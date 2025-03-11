using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XBars = DevExpress.XtraBars;
using XRibbon = DevExpress.XtraBars.Ribbon;
using XEditors = DevExpress.XtraEditors;
using XUtils = DevExpress.Utils;
using System.Drawing;

namespace DjSoft.App.iCollect.Components.Ribbon
{
    internal class DjRibbonControl : XRibbon.RibbonControl
    {
        public DjRibbonControl()
        {
            this.Visible = true;
            this.Dock = System.Windows.Forms.DockStyle.Top;
            this.CommandLayout = XRibbon.CommandLayout.Simplified;
            this.ButtonGroupsLayout = DevExpress.XtraBars.ButtonGroupsLayout.TwoRows;
            this.AllowMdiChildButtons = false;
            this.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            this.ShowDisplayOptionsMenuButton = DevExpress.Utils.DefaultBoolean.False;
            this.OptionsExpandCollapseMenu.EnableExpandCollapseMenu = DevExpress.Utils.DefaultBoolean.False;
            this.OptionsCustomizationForm.AllowToolbarCustomization = false;
            this.ToolbarLocation = XRibbon.RibbonQuickAccessToolbarLocation.Hidden;
            this.RibbonDisplayOptionsMenu.AllowRibbonQATMenu = false;
            


        }

        public DjRibbonPage AddPage(string name, string text)
        {
            DjRibbonPage page = new DjRibbonPage() { Name = name, Text = text };
            this.Pages.Add(page);
            page.DjRibbon = this;
            return page;
        }
    }

    internal class DjRibbonPage : XRibbon.RibbonPage
    {
        public DjRibbonGroup AddGroup(string name, string text)
        {
            DjRibbonGroup group = new DjRibbonGroup() { Name = name, Text = text };
            this.Groups.Add(group);
            return group;
        }
        public DjRibbonControl DjRibbon { get; set; }
    }
    internal class DjRibbonGroup : XRibbon.RibbonPageGroup
    {
        public IDjRibbonItem AddItem(DjRibbonItemType itemType)
        {
            return AddItem(itemType, null, null, null, null, null);
        }
        public IDjRibbonItem AddItem(DjRibbonItemType itemType, string name, string text, string toolTipTitle, string toolTipText, Image image)
        {
            IDjRibbonItem item = null;
            switch (itemType)
            {
                case DjRibbonItemType.Button:
                    var button = new DjRibbonButton();
                    button.Name = name;
                    button.Caption = text;
                    button.SuperTip = DjSuperToolTip.Create(toolTipTitle, toolTipText, text);
                    this.ItemLinks.Add(button);
                    item = button; 
                    break;

                case DjRibbonItemType.SkinDropDownButton:
                    var skinDropDownButton = new DjRibbonSkinDropDownButton();
                    this.ItemLinks.Add(skinDropDownButton);
                    item = skinDropDownButton;
                    break;

                case DjRibbonItemType.SkinPaletteDropDownButton:
                    var skinPaletteDropDownButton = new DjRibbonSkinPaletteDropDownButton();
                    this.ItemLinks.Add(skinPaletteDropDownButton);
                    item = skinPaletteDropDownButton;
                    break;

            }
            return item;
        }
        public DjRibbonPage DjPage { get; set; }
        public DjRibbonControl DjRibbon { get { return this.DjPage?.DjRibbon; } }

    }
    internal class DjRibbonButton : XBars.BarButtonItem, IDjRibbonItem
    {
        public DjRibbonButton()
        { }
        public DjRibbonItemType ItemType { get { return DjRibbonItemType.Button; } }
    }
    internal class DjRibbonSkinDropDownButton : XBars.SkinDropDownButtonItem, IDjRibbonItem
    {
        public DjRibbonSkinDropDownButton()
        {
            this.PaintStyle = XBars.BarItemPaintStyle.CaptionGlyph;
            this.RibbonStyle = XRibbon.RibbonItemStyles.Large;
        }
        public DjRibbonItemType ItemType { get { return DjRibbonItemType.SkinDropDownButton; } }
    }
    internal class DjRibbonSkinPaletteDropDownButton : XBars.SkinPaletteDropDownButtonItem, IDjRibbonItem
    {
        public DjRibbonSkinPaletteDropDownButton()
        {
            this.PaintStyle = XBars.BarItemPaintStyle.CaptionGlyph;
            this.RibbonStyle = XRibbon.RibbonItemStyles.Large;
        }
        public DjRibbonItemType ItemType { get { return DjRibbonItemType.SkinPaletteDropDownButton; } }
    }
    internal interface IDjRibbonItem
    {
        DjRibbonItemType ItemType { get; }
    }
    internal enum DjRibbonItemType
    {
        None,
        Button,

        SkinDropDownButton,
        SkinPaletteDropDownButton
    }

    internal class DjSuperToolTip : XUtils.SuperToolTip
    {
        internal static DjSuperToolTip Create(string toolTipTitle, string toolTipText, string itemCaption = null)
        {
            if (String.IsNullOrEmpty(toolTipTitle) && String.IsNullOrEmpty(toolTipText)) return null;
            if (String.IsNullOrEmpty(toolTipTitle) && !String.IsNullOrEmpty(itemCaption)) toolTipTitle = itemCaption;
            return new DjSuperToolTip(toolTipTitle, toolTipText);
        }
        private DjSuperToolTip(string toolTipTitle, string toolTipText)
        {
            bool hasTitle = String.IsNullOrEmpty(toolTipTitle);
            bool hasText = String.IsNullOrEmpty(toolTipText);

            if (hasTitle)
                this.Items.Add(new XUtils.ToolTipTitleItem() { Text = toolTipTitle });

            if (hasTitle && hasText)
                this.Items.Add(new XUtils.ToolTipSeparatorItem());

            if (hasText)
                this.Items.Add(new XUtils.ToolTipItem() { Text = toolTipText });
        }
    }
}
