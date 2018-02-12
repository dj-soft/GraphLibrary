using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Djs.Common.Application;
using System.Drawing;
using Djs.Common.Components;

namespace Djs.Common.Services
{
    internal class TestGlobalFunctions : IFunctionGlobal
    {
        /// <summary>
        /// Activity of this implementation
        /// </summary>
        PluginActivity IPlugin.Activity { get { return PluginActivity.OnlyDebug; } }
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
            group.Title = "Data k ukládání";
            group.Order = "A1";
            group.ToolTipTitle = "PRÁCE S DATY";

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Image, Size = FunctionGlobalItemSize.Whole, Image = Components.IconStandard.Dragon, Text = "DJ soft", UserData = "Rtf" });

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = Components.IconStandard.DocumentSave, Text = "Uložit", ToolTip = "Uloží dokument jako by se nechumelilo", LayoutHint = LayoutHint.NextItemOnSameRow });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = Components.IconStandard.DocumentSaveAs, Text = "Založit jako...", ToolTip = "Založí někam dokument tak, že nebude k nalezení", LayoutHint = LayoutHint.NextItemSkipToNextRow });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.DocumentExport, Text = "Exportovat dokument", ToolTip = "Exportuje dokument tak, aby jej všichni viděli", LayoutHint = LayoutHint.NextItemSkipToNextRow });

            group.ItemAction += new FunctionItemEventHandler(_ItemClick);
            
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
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = Components.IconStandard.EditCopy, Text = "Copy", ToolTip = "", LayoutHint = LayoutHint.NextItemSkipToNextRow });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = Components.IconStandard.EditCut, Text = "Cut", ToolTip = "", LayoutHint = LayoutHint.NextItemSkipToNextRow });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = Components.IconStandard.EditPaste, Text = "Paste", ToolTip = "", LayoutHint = LayoutHint.NextItemSkipToNextRow });

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Micro, Image = Components.IconStandard.EditPaste, ToolTip = "Napastuje dokument" });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Micro, Image = Components.IconStandard.EditUndo, ToolTip = "Undoluje obsah" });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Micro, Image = Components.IconStandard.EditRedo, ToolTip = "Redohodí to co jste nečekali" });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Micro, Image = Components.IconStandard.Refresh, ToolTip = "Přenačte něco málo z databáze" });

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Large, Image = Components.IconStandard.GoLeft, Text = "", ToolTip = "Jděte vlevo" });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Large, Image = Components.IconStandard.GoRight, Text = "", ToolTip = "Jděte vpravo", LayoutHint = LayoutHint.ThisItemOnSameRow });

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Micro, Image = Components.IconStandard.EditRedo, ToolTip = "Zase to redo" });

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Separator, Size = FunctionGlobalItemSize.Whole });

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.EditUndo, Text = "UNDO", LayoutHint = LayoutHint.ThisItemSkipToNextTable });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.EditRedo, Text = "REDO", LayoutHint = LayoutHint.ThisItemSkipToNextTable, IsEnabled = false });

            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Separator, Size = FunctionGlobalItemSize.Whole });
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Label, Size = FunctionGlobalItemSize.Small, Text = "Databáze:" });
            this._DbCombo = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.ComboBox, Size = FunctionGlobalItemSize.Half };
            this._DbCombo.SubItemsEnumerateBefore += new FunctionItemEventHandler(_DbCombo_SubItemsEnumerateBefore);
            group.Items.Add(this._DbCombo);
            
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Separator, Size = FunctionGlobalItemSize.Whole });

            group.ItemAction += new FunctionItemEventHandler(_ItemClick);

            return group;
        }

        void _DbCombo_SubItemsEnumerateBefore(object sender, FunctionItemEventArgs e)
        {
            e.Item.SubItems.Clear();
            e.Item.SubItems.Add(new FunctionItem(this) { Text = "První řádek", Image = Components.IconStandard.BulletBlue16, ToolTip = "Výběr databáze z prvního řádku" });
            e.Item.SubItems.Add(new FunctionItem(this) { Text = "Druhý řádek", Image = Components.IconStandard.BulletGreen16, ToolTip = "Výběr databáze z druhého řádku" });
            e.Item.SubItems.Add(new FunctionItem(this) { Text = "Třetí řádek", Image = Components.IconStandard.BulletRed16, ToolTip = "Výběr databáze z třetího řádku" });
            e.Item.SubItems.Add(new FunctionItem(this) { Text = null });
            e.Item.SubItems.Add(new FunctionItem(this) { Text = "Další řádek", Image = Components.IconStandard.BulletPurple16, ToolTip = "Výběr databáze z dalšího řádku" });

            if (e.Item.Value == null)
                e.Item.Value = e.Item.SubItems[0];
        }
        private FunctionGlobalItem _DbCombo;
        void _ItemClick(object sender, FunctionItemEventArgs e)
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
