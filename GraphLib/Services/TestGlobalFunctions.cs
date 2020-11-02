using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Application;
using RES = Noris.LCS.Base.WorkScheduler.Resources;
using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.Services
{
    internal class TestGlobalFunctions : IFunctionGlobal
    {
        /// <summary>
        /// Activity of this implementation
        /// </summary>
        PluginActivity IPlugin.Activity { get { return PluginActivity.None; } }
        /// <summary>
        /// Create and return items for Toolbar GUI from current service
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        FunctionGlobalPrepareResponse IFunctionGlobal.PrepareGui(FunctionGlobalPrepareGuiRequest request)
        {
            List<FunctionGlobalGroup> groups = new List<FunctionGlobalGroup>();
            groups.Add(_CreateGroupData());
            groups.Add(_CreateGroupEdit());

            FunctionGlobalPrepareResponse response = new FunctionGlobalPrepareResponse();
            response.Items = groups.ToArray();
            return response;
        }
        /// <summary>
        /// Check all items for Toolbar GUI created from all services.
        /// Any service can set FunctionGlobalGroup.IsVisible to false, or FunctionGlobalGroup.Items[].IsVisible or IsEnabled to false, to hide any function from other service.
        /// </summary>
        /// <param name="request"></param>
        void IFunctionGlobal.CheckGui(FunctionGlobalCheckGuiRequest request) { }
        private FunctionGlobalGroup _CreateGroupData()
        {
            FunctionGlobalGroup group = new FunctionGlobalGroup(this);
            group.Title = "Data k ukládání 2018";
            group.Order = "A1";
            group.ToolTipTitle = "PRÁCE S DATY (c)2018";

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Image, Size = FunctionGlobalItemSize.Whole, Image = App.ResourcesApp.GetImage(RES.Images.Actions24.DashboardShowPng), Text = "DJ soft", UserData = "Rtf" });

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = App.ResourcesApp.GetImage(RES.Images.Actions24.DocumentSave2Png), Text = "Uložit", ToolTip = "Uloží dokument jako by se nechumelilo", LayoutHint = LayoutHint.NextItemOnSameRow });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = App.ResourcesApp.GetImage(RES.Images.Actions24.DocumentSaveAs2Png), Text = "Založit jako...", ToolTip = "Založí někam dokument tak, že nebude k nalezení", LayoutHint = LayoutHint.NextItemSkipToNextRow });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = App.ResourcesApp.GetImage(RES.Images.Actions24.DocumentExport2Png), Text = "Exportovat dokument", ToolTip = "Exportuje dokument tak, aby jej všichni viděli", LayoutHint = LayoutHint.NextItemSkipToNextRow });

            group.ItemClicked += new FunctionItemEventHandler(_ItemInGroupClick);
            
            return group;
        }

        private FunctionGlobalGroup _CreateGroupEdit()
        {
            FunctionGlobalGroup group = new FunctionGlobalGroup(this);
            group.Title = "Editace položek";
            group.Order = "A2";
            group.ToolTipTitle = "EDITACE POLOŽEK";
            group.LayoutWidth = 16;

            List<FunctionGlobalItem> items = new List<FunctionGlobalItem>();
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = App.ResourcesApp.GetImage(RES.Images.Actions24.EditCopy2Png), Text = "Copy", ToolTip = "", LayoutHint = LayoutHint.NextItemSkipToNextRow });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = App.ResourcesApp.GetImage(RES.Images.Actions24.EditCut2Png), Text = "Cut", ToolTip = "", LayoutHint = LayoutHint.NextItemSkipToNextRow });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = App.ResourcesApp.GetImage(RES.Images.Actions24.EditPaste2Png), Text = "Paste", ToolTip = "", LayoutHint = LayoutHint.NextItemSkipToNextRow });

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Micro, Image = App.ResourcesApp.GetImage(RES.Images.Actions16.EditPaste2Png), ToolTip = "Napastuje dokument" });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Micro, Image = App.ResourcesApp.GetImage(RES.Images.Actions16.EditUndo2Png), ToolTip = "Undoluje obsah" });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Micro, Image = App.ResourcesApp.GetImage(RES.Images.Actions16.EditRedo2Png), ToolTip = "Redohodí to co jste nečekali" });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Micro, Image = App.ResourcesApp.GetImage(RES.Images.Actions16.ViewRefresh2Png), ToolTip = "Přenačte něco málo z databáze" });

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Large, Image = App.ResourcesApp.GetImage(RES.Images.Actions24.ArrowLeft2Png), Text = "", ToolTip = "Jděte vlevo" });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Large, Image = App.ResourcesApp.GetImage(RES.Images.Actions24.ArrowRight2Png), Text = "", ToolTip = "Jděte vpravo", LayoutHint = LayoutHint.ThisItemOnSameRow });

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Micro, Image = App.ResourcesApp.GetImage(RES.Images.Actions16.EditRedo4Png), ToolTip = "Zase to redo" });

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Separator, Size = FunctionGlobalItemSize.Whole });

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = App.ResourcesApp.GetImage(RES.Images.Actions16.EditUndo6Png), Text = "UNDO", LayoutHint = LayoutHint.ThisItemSkipToNextTable });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = App.ResourcesApp.GetImage(RES.Images.Actions16.EditRedo6Png), Text = "REDO", LayoutHint = LayoutHint.ThisItemSkipToNextTable, IsEnabled = false });

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Separator, Size = FunctionGlobalItemSize.Whole });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Label, Size = FunctionGlobalItemSize.Small, Text = "Databáze:" });
            this._DbCombo = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.ComboBox, Size = FunctionGlobalItemSize.Half };
            this._DbCombo.SubItemsEnumerateBefore += new FunctionItemEventHandler(_DbCombo_SubItemsEnumerateBefore);
            group.Items.Add(this._DbCombo);
            
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Separator, Size = FunctionGlobalItemSize.Whole });

            FunctionGlobalItem fItem = group.Items[0];

            group.SubItemsEnumerateBefore += _ItemInGroup_SubItemsEnumerateBefore;
            group.ItemClicked += _ItemInGroupClick;

            return group;
        }


        void _DbCombo_SubItemsEnumerateBefore(object sender, FunctionItemEventArgs e)
        {
            e.Item.SubItems.Clear();
            e.Item.SubItems.Add(new FunctionItem(this) { Text = "První řádek", Image = App.ResourcesApp.GetImage(RES.Images.Small16.BulletBluePng), ToolTip = "Výběr databáze z prvního řádku" });
            e.Item.SubItems.Add(new FunctionItem(this) { Text = "Druhý řádek", Image = App.ResourcesApp.GetImage(RES.Images.Small16.BulletGreenPng), ToolTip = "Výběr databáze z druhého řádku" });
            e.Item.SubItems.Add(new FunctionItem(this) { Text = "Třetí řádek", Image = App.ResourcesApp.GetImage(RES.Images.Small16.BulletRedPng), ToolTip = "Výběr databáze z třetího řádku" });
            e.Item.SubItems.Add(new FunctionItem(this) { Text = null });
            e.Item.SubItems.Add(new FunctionItem(this) { Text = "Další řádek", Image = App.ResourcesApp.GetImage(RES.Images.Small16.BulletPurplePng), ToolTip = "Výběr databáze z dalšího řádku" });

            if (e.Item.Value == null)
                e.Item.Value = e.Item.SubItems[0];
        }
        private FunctionGlobalItem _DbCombo;


        private void _ItemInGroup_SubItemsEnumerateBefore(object sender, FunctionItemEventArgs e)
        {
        }
        void _ItemInGroupClick(object sender, FunctionItemEventArgs e)
        {
            if (e.Item.UserData is string && (string)e.Item.UserData == "Rtf")
            {
                Rtf.RtfDocument doc = new Rtf.RtfDocument();
                doc.RtfText = Rtf.RtfDocument.TestRtfText2;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Clicked on: " + e.Item.ToString());
            }
        }
    }
}
