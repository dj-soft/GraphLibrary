using Asol.Tools.WorkScheduler.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Asol.Tools.WorkScheduler.TestGUI.Forms
{
    public partial class TestOneComponent : Form
    {
        public TestOneComponent()
        {
            InitializeComponent();
            this.InitGComp();
        }

        private void _CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        
        protected void InitGComp()
        {
            this.TagFilter = new GTagFilter();
            this.TagFilter.Bounds = new Rectangle(10, 5, 250, 120);
            // this.TagFilter.BackColor = Color.DarkBlue;
            this.TagFilter.SelectAllVisible = true;
            this.TagFilter.SelectionMode = GTagFilterSelectionMode.AnyItemsCount;
            this.TagFilter.ItemHeight = 29;
            this.TagFilter.RoundItemPercent = 0;
            this.TagFilter.DrawItemBorder = false;
            this.TagFilter.ExpandHeightOnMouse = true;
            this.TagFilter.CheckedImage = Application.App.Resources.GetImage(Noris.LCS.Base.WorkScheduler.Resources.Images.Actions24.Checkbox2Png);
            this.TagFilter.FilterChanged += TagFilter_FilterChanged;
            this.TagFilter.TagItems = this.CreateTagItems();
            this.TagFilterHeight = 2;
            this._Control.AddItem(this.TagFilter);
            this.ResizeTagBounds();
            this._Control.SizeChanged += _Control_SizeChanged;
            this._Control.BackColor = Color.LightGoldenrodYellow;
        }

        private void TagFilter_FilterChanged(object sender, EventArgs e)
        {
            string text = "";
            var filters = this.TagFilter.FilteredItems;
            foreach (var filter in filters)
                text += (text.Length == 0 ? "" : "; ") + filter.Text;
            this._FilterLabel.Text = text;
        }

        protected TagItem[] CreateTagItems()
        {
            List<TagItem> tagList = new List<TagItem>();
            tagList.Add("0.Třetí");
            tagList.Add("1.Desátý");
            tagList.Add("2.Zelená");
            tagList.Add("3.Borový");
            tagList.Add("4.Skleněné");
            tagList.Add("5.Kruhové");
            tagList.Add("6.Hrušková");
            tagList.Add("7.Nerezový");
            tagList.Add("8.Základní");

            Color? backColor = Color.FromArgb(255, 220, 192, 192);
            Color? checkColor = null; // Color.FromArgb(255, 250, 220, 220);
            tagList[3].BackColor = backColor;
            tagList[3].CheckedBackColor = checkColor;
            tagList[4].BackColor = backColor;
            tagList[4].CheckedBackColor = checkColor;
            tagList[5].BackColor = backColor;
            tagList[5].CheckedBackColor = checkColor;
            tagList[6].BackColor = backColor;
            tagList[6].CheckedBackColor = checkColor;
            tagList[7].Visible = false;

            /*
            tagList[2].Checked = true;
            tagList[4].Checked = true;
            tagList[6].Checked = true;
            */
            return tagList.ToArray();
        }
        private void _Control_SizeChanged(object sender, EventArgs e)
        {
            this.ResizeTagBounds();
        }
        protected void ResizeTagBounds()
        {
            int height = (this.TagFilterHeight == 1 ? this.TagFilter.OptimalHeightOneRow : this.TagFilter.OptimalHeightAllRows);
            Size clientSize = this._Control.ClientSize;
            this.TagFilter.Bounds = new Rectangle(10, 5, clientSize.Width - 20, height);
        }

        protected GTagFilter TagFilter;
        protected int TagFilterHeight;
        private void _OneRowButton_Click(object sender, EventArgs e)
        {
            this.TagFilterHeight = 1;
            this.ResizeTagBounds();
            this._Control.Refresh();
        }

        private void _AllRowsButton_Click(object sender, EventArgs e)
        {
            this.TagFilterHeight = 2;
            this.ResizeTagBounds();
            this._Control.Refresh();
        }
    }
}
