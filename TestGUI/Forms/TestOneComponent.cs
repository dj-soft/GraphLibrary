﻿using Asol.Tools.WorkScheduler.Components;
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
            this.TagFilter.TagItems = this.CreateTagItems();
            this.TagFilter.SelectAllVisible = true;
            this.TagFilter.SelectionMode = GTagFilterSelectionMode.AnyItemsCount;
            this.TagFilter.ItemHeight = 22;
            this.TagFilter.RoundItemPercent = 40;
            this.TagFilter.ExpandHeightOnMouse = true;
            this.TagFilter.CheckedImage = Application.App.Resources.GetImage(Noris.LCS.Base.WorkScheduler.Resources.Images.Actions24.DialogOk2Png);
            this.TagFilterHeight = 2;
            this._Control.AddItem(this.TagFilter);
            this.ResizeTagBounds();
            this._Control.SizeChanged += _Control_SizeChanged;
            this._Control.BackColor = Color.LightGoldenrodYellow;
        }
        protected TagItem[] CreateTagItems()
        {
            List<TagItem> tagList = new List<TagItem>();
            tagList.Add("Třetí");
            tagList.Add("Desátý");
            tagList.Add("Zelená");
            tagList.Add("Borový");
            tagList.Add("Skleněné");
            tagList.Add("Kruhové");
            tagList.Add("Hrušková");
            tagList.Add("Nerezový");
            tagList.Add("Základní");

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