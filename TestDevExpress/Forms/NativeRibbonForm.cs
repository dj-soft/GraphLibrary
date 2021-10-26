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

namespace TestDevExpress.Forms
{
    public class NativeRibbonForm : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        public NativeRibbonForm() : this(CreateMode.Native) { }
        public NativeRibbonForm(CreateMode createMode)
        {
            this.SvgImages = DxComponent.GetResourceKeys(false, true);
            InitRibbon(createMode);
        }
        private void InitRibbon(CreateMode createMode)
        {
            switch (createMode)
            {
                case CreateMode.Native:
                    this._Ribbon = new DevExpress.XtraBars.Ribbon.RibbonControl();
                    this._SlaveRibbon = new DevExpress.XtraBars.Ribbon.RibbonControl();
                    this.Ribbon = this._Ribbon;
                    this.SlaveRibbon = this._SlaveRibbon;
                    break;
                case CreateMode.UseClasses:
                    this._DxRibbon = new DxRibbonControl();
                    this._DxRibbon.InitUserProperties(true);
                    this._SlaveDxRibbon = new DxRibbonControl();
                    this._SlaveDxRibbon.InitUserProperties(false);
                    this.Ribbon = this._DxRibbon;
                    this.SlaveRibbon = this._SlaveDxRibbon;
                    break;
            }

            this.SlaveRibbon.Visible = false;

            _Page0 = CreatePage(createMode, 0);
            _Page0.Groups.AddRange(this.CreateGroups(createMode, 12, 6));
            this.SlaveRibbon.Pages.Add(_Page0);

            _Page1 = CreatePage(createMode, 1);
            _Page1.Groups.AddRange(this.CreateGroups(createMode, 6, 4));
            this.SlaveRibbon.Pages.Add(_Page1);

            this.Ribbon.Dock = System.Windows.Forms.DockStyle.Top;
            this.Controls.Add(this.Ribbon);

            this.Ribbon.Visible = true;
            this.Ribbon.MergeRibbon(this.SlaveRibbon);
        }
        private RibbonPage CreatePage(CreateMode createMode, int pageIndex)
        {
            switch (createMode)
            {
                case CreateMode.Native:
                    return new RibbonPage("NATIVE " + (pageIndex + 1).ToString());
                case CreateMode.UseClasses:
                    return new DxRibbonPage(this._DxRibbon, "ASOL " + (pageIndex + 1).ToString());
                case CreateMode.UseMethods:
                    return null;
            }
            return null;
        }
        private RibbonPageGroup[] CreateGroups(CreateMode createMode, int groupCount, int itemCount)
        {
            List<RibbonPageGroup> groups = new List<RibbonPageGroup>();
            for (int g = 0; g < groupCount; g++)
            {
                RibbonPageGroup group = CreateGroup(createMode, g);
                group.ItemLinks.AddRange(this.CreateItems(createMode, itemCount));
                groups.Add(group);
            }
            return groups.ToArray();
        }
        private RibbonPageGroup CreateGroup(CreateMode createMode, int groupIndex)
        {
            switch (createMode)
            {
                case CreateMode.Native:
                    RibbonPageGroup group = new RibbonPageGroup("Grupa NATIVE " + (groupIndex + 1).ToString());
                    group.State = RibbonPageGroupState.Auto;
                    return group;
                case CreateMode.UseClasses:
                    DxRibbonGroup dxGroup = new DxRibbonGroup("Grupa ASOL " + (groupIndex + 1).ToString());
                    dxGroup.State = RibbonPageGroupState.Auto;
                    return dxGroup;
                case CreateMode.UseMethods:
                    return null;
            }
            return null;
        }
        private BarItem[] CreateItems(CreateMode createMode, int itemCount)
        {
            List<BarItem> items = new List<BarItem>();
            for (int i = 0; i < itemCount; i++)
            {
                BarItem item = CreateItem(createMode, i);
                items.Add(item);
            }

            return items.ToArray();
        }
        private BarItem CreateItem(CreateMode createMode, int itemIndex)
        {
            string svgImage = Random.GetItem(SvgImages);
            string text = "Button " + (this.Ribbon.Items.Count + 1).ToString();
            BarItem item = this.Ribbon.Items.CreateButton(text);
            item.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            item.ImageOptions.SvgImage = DxComponent.GetSvgImage(svgImage);
            return item;
        }
        private string[] SvgImages;
        private DevExpress.XtraBars.Ribbon.RibbonControl _Ribbon;
        private DevExpress.XtraBars.Ribbon.RibbonControl _SlaveRibbon;
        private DxRibbonControl _DxRibbon;
        private DxRibbonControl _SlaveDxRibbon;
        private DevExpress.XtraBars.Ribbon.RibbonPage _Page0;
        private DevExpress.XtraBars.Ribbon.RibbonPage _Page1;
        private DevExpress.XtraBars.Ribbon.RibbonControl SlaveRibbon;
        /// <summary>
        /// Režim vytváření pvků Ribbonu
        /// </summary>
        public enum CreateMode { Native, UseClasses, UseMethods }
    }
}
