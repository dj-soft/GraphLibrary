﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;
using System.Drawing;
using TestDevExpress.Components;
using Noris.Clients.Win.Components;
using DevExpress.XtraRichEdit.Layout;
using DevExpress.PivotGrid.OLAP.Mdx;
using DevExpress.Utils.DirectXPaint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DevExpress.XtraBars.Docking2010.DragEngine;

namespace TestDevExpress.Forms
{
    [RunTestForm(groupText: "Testovací okna", buttonText: "AD HOC", buttonOrder: 999, buttonImage: "svgimages/business%20objects/bo_statemachine.svg", buttonToolTip: "Otevře okno pro občasné testování čehokoli", tabViewToolTip: "Okno pro občasné testování čehokoli")]
    internal class AdHocForm : DxRibbonForm
    {
        protected override void DxMainContentPrepare()
        {
            this.Text = "AdHoc test TreeListu a ikon";

            base.DxMainContentPrepare();

            PrepareTreeList();
        }
        protected override void DxMainContentDoLayout(bool isSizeChanged)
        {
            DoLayoutTreeList();
        }

        #region TreeList
        protected void PrepareTreeList()
        {
            TreeList = new DevExpress.XtraTreeList.TreeList() { Bounds = new Rectangle(12, 12, 350, 480) };
            PrepareTreeListProperties();
            PrepareTreeListNodes();
        }
        protected void PrepareTreeListProperties()
        {
            TreeList.OptionsView.ShowTreeLines = DevExpress.Utils.DefaultBoolean.True;
            TreeList.OptionsView.TreeLineStyle = DevExpress.XtraTreeList.LineStyle.Percent50;
            TreeList.OptionsView.ShowHorzLines = false;
            TreeList.OptionsView.ShowVertLines = false;
            TreeList.OptionsView.ShowFirstLines = false;
            TreeList.OptionsView.ShowRoot = true;

            TreeList.ViewStyle = DevExpress.XtraTreeList.TreeListViewStyle.TreeView;        // TreeList    TreeView

            TreeList.TreeLevelWidth = 34;
            FixSetTreeList();

//          string optionsDump = DxTreeList.CreateOptionsDump(TreeList);
        }

        protected void FixSetTreeList()
        {
            TreeList.OptionsView.ShowIndentAsRowStyle = true;
            TreeList.OptionsView.ShowHierarchyIndentationLines = DevExpress.Utils.DefaultBoolean.Default;
            TreeList.OptionsView.ShowIndentAsRowStyle = false;
        }
        protected void PrepareTreeListNodes()
        {
            ResourceImageSizeType size = ResourceImageSizeType.Small;
            NodeImageSetType setMain = NodeImageSetType.Formats;
            NodeImageSetType setSuffix = NodeImageSetType.Suffix;

            // Varianty:
            //  1 = používám oba ImageListy (Select + State). Do Select (vlevo) dávám Suffix ikonu, do State (vpravo) dávám Main ikonu.
            //  2 = používám oba ImageListy (Select + State). Do Select (vlevo) dávám Main ikonu, do State (vpravo) dávám Suffix ikonu.
            //  3 = používám pouze Select ImageList (vlevo), ale nemám State ImageList (vpravo). Do Select (vlevo) dávám Suffix ikonu, do State (vpravo) dávám Main ikonu (ale nebude vidět).
            //  4 = používám pouze Select ImageList (vlevo), ale nemám State ImageList (vpravo). Do Select (vlevo) dávám Main ikonu, do State (vpravo) dávám Suffix ikonu (ale nebude vidět).
            //  5 = používám pouze State ImageList (vpravo), ale nemám Select ImageList (vlevo). Do Select (vlevo) dávám Suffix ikonu (ale nebude vidět), do State (vpravo) dávám Main ikonu.
            //  6 = používám pouze State ImageList (vpravo), ale nemám Select ImageList (vlevo). Do Select (vlevo) dávám Main ikonu (ale nebude vidět), do State (vpravo) dávám Suffix ikonu.
            //  7 = Nepoužívám žádný ImageList. Do Select (vlevo) dávám Suffix ikonu, do State (vpravo) dávám Main ikonu. Nebude vidět ani jedna.
            //  8 = Nepoužívám žádný ImageList. Do Select (vlevo) dávám Main ikonu, do State (vpravo) dávám Suffix ikonu. Nebude vidět ani jedna.
            // Nephrite:
            //  Vlevo je "dodatková ikona" (zámek), vpravo "Main ikona" (typ dokladu).
            //  Tedy varianta 1.
            int varianta = 1;

            if (varianta == 1 || varianta == 2 || varianta == 3 || varianta == 4)
                TreeList.SelectImageList = DxComponent.GetVectorImageList(size);             // Ikony vlevo
            if (varianta == 1 || varianta == 2 || varianta == 5 || varianta == 6)
                TreeList.StateImageList = DxComponent.GetVectorImageList(size);              // Ikony vpravo

            TreeList.Columns.Clear();
            TreeList.Columns.Add(new DevExpress.XtraTreeList.Columns.TreeListColumn() { Caption = "Test sloupec", Width = 4000, UnboundDataType = typeof(string), Visible = true });

            TreeList.Nodes.Clear();

            object[] nodeData;

            int cnt0 = Randomizer.Rand.Next(10, 40);
            for (int i0 = 0; i0 < cnt0; i0++)
            {
                var node0 = prepareNode(null);
                if (Randomizer.IsTrue(60))
                {
                    int cnt1 = Randomizer.Rand.Next(3, 10);
                    for (int i1 = 0; i1 < cnt1; i1++)
                    {
                        var node1 = prepareNode(node0);
                    }
                    if (Randomizer.IsTrue(60))
                        node0.Expand();
                }
            }

            this.DxMainPanel.Controls.Add(TreeList);

            FixSetTreeList();

            DevExpress.XtraTreeList.Nodes.TreeListNode prepareNode(DevExpress.XtraTreeList.Nodes.TreeListNode parent)
            {
                nodeData = new object[] { Randomizer.GetSentence(6, true) };
                var node = TreeList.AppendNode(nodeData, parent);

                int mainIndex = (Randomizer.IsTrue(65) ? DxComponent.GetVectorImageIndex(GetImageName(setMain), size) : -1);
                int suffixIndex = (Randomizer.IsTrue(45) ? DxComponent.GetVectorImageIndex(GetImageName(setSuffix), size) : -1);

                switch (varianta)
                {
                    case 1:
                    case 3:
                    case 5:
                    case 7:
                        // Suffix ikona je vlevo, a pokud není pak je tam díra;
                        // Main ikona je vždy vpravo, a pokud není pak je tam díra;

                        // Ikona vlevo = Suffix:
                        node.ImageIndex = suffixIndex;
                        node.SelectImageIndex = suffixIndex;

                        // Ikona vpravo = Main:
                        node.StateImageIndex = mainIndex;

                        break;

                    case 2:
                    case 4:
                    case 6:
                    case 8:
                        // Suffix ikona je vpravo, a pokud není pak je tam díra;
                        // Main ikona je vždy vlevo, a pokud není pak je tam díra;

                        // Ikona vlevo = Main:
                        node.ImageIndex = mainIndex;
                        node.SelectImageIndex = mainIndex;

                        // Ikona vpravo = Suffix:
                        node.StateImageIndex = suffixIndex;

                        break;

                }

                return node;
            }
        }
        private void DoLayoutTreeList()
        {
            if (TreeList != null)
                TreeList.Bounds = new Rectangle(12, 12, 350, this.DxMainPanel.ClientSize.Height - 24);
        }
        DevExpress.XtraTreeList.TreeList TreeList;
        #endregion

        #region Ikony: druhy ikon, seznam názvů podle druhů, generátor ikony, barvy, stylu
        /// <summary>
        /// Vrátí náhodný Main obrázek z dané sady <paramref name="imageSet"/>.
        /// </summary>
        /// <returns></returns>
        protected virtual string GetImageName(NodeImageSetType imageSet)
        {
            var images = _GetImageNames(imageSet);
            if (images != null && images.Length > 0)
                return Randomizer.GetItem(images);
            return null;
        }
        /// <summary>
        /// Vrať náhodnou světlou barvu pro BackColor
        /// </summary>
        /// <returns></returns>
        protected virtual Color GetRandomBackColor()
        {
            if (__BackColors is null)
            {
                int h = 240;
                int l = 210;
                __BackColors = new Color[]
                {
                    Color.FromArgb(h,h,h),
                    Color.FromArgb(h,h,l),
                    Color.FromArgb(h,l,h),
                    Color.FromArgb(l,h,h),
                    Color.FromArgb(l,h,l),
                    Color.FromArgb(l,l,h),
                    Color.FromArgb(h,l,l),
                    Color.FromArgb(l,l,l)
                };
            }
            return Randomizer.GetItem(__BackColors);
        }
        /// <summary>
        /// Vrať náhodnou tmavou barvu pro ForeColor
        /// </summary>
        /// <returns></returns>
        protected virtual Color GetRandomForeColor()
        {
            if (__ForeColors is null)
            {
                int h = 80;
                int l = 24;
                __ForeColors = new Color[]
                {
                    Color.FromArgb(h,h,h),
                    Color.FromArgb(h,h,l),
                    Color.FromArgb(h,l,h),
                    Color.FromArgb(l,h,h),
                    Color.FromArgb(l,h,l),
                    Color.FromArgb(l,l,h),
                    Color.FromArgb(h,l,l),
                    Color.FromArgb(l,l,l)
                };
            }
            return Randomizer.GetItem(__ForeColors);
        }
        protected virtual string GetRandomStyleName()
        {
            if (__StyleNames is null)
                __StyleNames = new string[]
                {   // Nebudu dávat všechny styly, jen vybrané:
                    AdapterSupport.StyleDefault,
                    AdapterSupport.StyleOK,
                    AdapterSupport.StyleWarning,
                    AdapterSupport.StyleWarning,
                    AdapterSupport.StyleImportant,
                    AdapterSupport.StyleNote,
                    AdapterSupport.StyleHeader1

                };
            return Randomizer.GetItem(__StyleNames);
        }
        /// <summary>
        /// Vrátí set požadovaných obrázků. Autoinicializační.
        /// </summary>
        /// <param name="imageSet"></param>
        /// <returns></returns>
        private string[] _GetImageNames(NodeImageSetType imageSet)
        {
            switch (imageSet)
            {
                case NodeImageSetType.Documents:
                    if (__ImagesDocuments is null)
                        __ImagesDocuments = new string[]
{
    "svgimages/reports/alignmentbottomcenter.svg",
    "svgimages/reports/alignmentbottomleft.svg",
    "svgimages/reports/alignmentbottomright.svg",
    "svgimages/reports/alignmentcentercenter.svg",
    "svgimages/reports/alignmentcenterleft.svg",
    "svgimages/reports/alignmentcenterright.svg",
    "svgimages/reports/alignmenttopcenter.svg",
    "svgimages/reports/alignmenttopleft.svg",
    "svgimages/reports/alignmenttopright.svg",
    "svgimages/richedit/alignbottomcenter.svg",
    "svgimages/richedit/alignbottomcenterrotated.svg",
    "svgimages/richedit/alignbottomleft.svg",
    "svgimages/richedit/alignbottomleftrotated.svg",
    "svgimages/richedit/alignbottomright.svg",
    "svgimages/richedit/alignbottomrightrotated.svg",
    "svgimages/richedit/alignfloatingobjectbottomcenter.svg",
    "svgimages/richedit/alignfloatingobjectbottomleft.svg",
    "svgimages/richedit/alignfloatingobjectbottomright.svg",
    "svgimages/richedit/alignfloatingobjectmiddlecenter.svg",
    "svgimages/richedit/alignfloatingobjectmiddleleft.svg",
    "svgimages/richedit/alignfloatingobjectmiddleright.svg",
    "svgimages/richedit/alignfloatingobjecttopcenter.svg",
    "svgimages/richedit/alignfloatingobjecttopleft.svg",
    "svgimages/richedit/alignfloatingobjecttopright.svg",
    "svgimages/richedit/alignmiddlecenter.svg",
    "svgimages/richedit/alignmiddlecenterrotated.svg",
    "svgimages/richedit/alignmiddleleft.svg",
    "svgimages/richedit/alignmiddleleftrotated.svg",
    "svgimages/richedit/alignmiddleright.svg",
    "svgimages/richedit/alignmiddlerightrotated.svg",
    "svgimages/richedit/alignright.svg",
    "svgimages/richedit/aligntopcenter.svg",
    "svgimages/richedit/aligntopcenterrotated.svg",
    "svgimages/richedit/aligntopleft.svg",
    "svgimages/richedit/aligntopleftrotated.svg",
    "svgimages/richedit/aligntopright.svg",
    "svgimages/richedit/aligntoprightrotated.svg",
    "svgimages/richedit/borderbottom.svg",
    "svgimages/richedit/borderinsidehorizontal.svg",
    "svgimages/richedit/borderinsidevertical.svg",
    "svgimages/richedit/borderleft.svg",
    "svgimages/richedit/bordernone.svg",
    "svgimages/richedit/borderright.svg",
    "svgimages/richedit/bordersall.svg",
    "svgimages/richedit/bordersandshading.svg",
    "svgimages/richedit/bordersbox.svg",
    "svgimages/richedit/borderscustom.svg",
    "svgimages/richedit/bordersgrid.svg",
    "svgimages/richedit/bordersinside.svg",
    "svgimages/richedit/bordersoutside.svg",
    "svgimages/richedit/bordertop.svg"
};
                    return __ImagesDocuments;
                case NodeImageSetType.Actions:
                    if (__ImagesActions is null)
                        __ImagesActions = new string[]
{
    "svgimages/icon%20builder/actions_add.svg",
    "svgimages/icon%20builder/actions_addcircled.svg",
    "svgimages/icon%20builder/actions_arrow1down.svg",
    "svgimages/icon%20builder/actions_arrow1left.svg",
    "svgimages/icon%20builder/actions_arrow1leftdown.svg",
    "svgimages/icon%20builder/actions_arrow1leftup.svg",
    "svgimages/icon%20builder/actions_arrow1right.svg",
    "svgimages/icon%20builder/actions_arrow1rightdown.svg",
    "svgimages/icon%20builder/actions_arrow1rightup.svg",
    "svgimages/icon%20builder/actions_arrow1up.svg",
    "svgimages/icon%20builder/actions_arrow2down.svg",
    "svgimages/icon%20builder/actions_arrow2left.svg",
    "svgimages/icon%20builder/actions_arrow2leftdown.svg",
    "svgimages/icon%20builder/actions_arrow2leftup.svg",
    "svgimages/icon%20builder/actions_arrow2right.svg",
    "svgimages/icon%20builder/actions_arrow2rightdown.svg",
    "svgimages/icon%20builder/actions_arrow2rightup.svg",
    "svgimages/icon%20builder/actions_arrow2up.svg",
    "svgimages/icon%20builder/actions_arrow3down.svg",
    "svgimages/icon%20builder/actions_arrow3left.svg",
    "svgimages/icon%20builder/actions_arrow3right.svg",
    "svgimages/icon%20builder/actions_arrow3up.svg",
    "svgimages/icon%20builder/actions_arrow4down.svg",
    "svgimages/icon%20builder/actions_arrow4left.svg",
    "svgimages/icon%20builder/actions_arrow4leftdown.svg",
    "svgimages/icon%20builder/actions_arrow4leftup.svg",
    "svgimages/icon%20builder/actions_arrow4right.svg",
    "svgimages/icon%20builder/actions_arrow4rightdown.svg",
    "svgimages/icon%20builder/actions_arrow4rightup.svg",
    "svgimages/icon%20builder/actions_arrow4up.svg",
    "svgimages/icon%20builder/actions_arrow5downleft.svg",
    "svgimages/icon%20builder/actions_arrow5downright.svg",
    "svgimages/icon%20builder/actions_arrow5leftdown.svg",
    "svgimages/icon%20builder/actions_arrow5leftup.svg",
    "svgimages/icon%20builder/actions_arrow5rightdown.svg",
    "svgimages/icon%20builder/actions_arrow5rightup.svg",
    "svgimages/icon%20builder/actions_arrow5upleft.svg",
    "svgimages/icon%20builder/actions_arrow5upright.svg"
};
                    return __ImagesActions;
                case NodeImageSetType.Formats:
                    if (__ImagesFormats is null)
                        __ImagesFormats = new string[]
{
    "svgimages/export/exporttocsv.svg",
    "svgimages/export/exporttodoc.svg",
    "svgimages/export/exporttodocx.svg",
    "svgimages/export/exporttoepub.svg",
    "svgimages/export/exporttohtml.svg",
    "svgimages/export/exporttoimg.svg",
    "svgimages/export/exporttomht.svg",
    "svgimages/export/exporttoodt.svg",
    "svgimages/export/exporttopdf.svg",
    "svgimages/export/exporttortf.svg",
    "svgimages/export/exporttotxt.svg",
    "svgimages/export/exporttoxls.svg",
    "svgimages/export/exporttoxlsx.svg",
    "svgimages/export/exporttoxml.svg",
    "svgimages/export/exporttoxps.svg"
};
                    return __ImagesFormats;
                case NodeImageSetType.Charts:
                    if (__ImagesCharts is null)
                        __ImagesCharts = new string[]
{
    "svgimages/chart/chart.svg",
    "svgimages/chart/charttype_area.svg",
    "svgimages/chart/charttype_area3d.svg",
    "svgimages/chart/charttype_area3dstacked.svg",
    "svgimages/chart/charttype_area3dstacked100.svg",
    "svgimages/chart/charttype_areastacked.svg",
    "svgimages/chart/charttype_areastacked100.svg",
    "svgimages/chart/charttype_areastepstacked.svg",
    "svgimages/chart/charttype_areastepstacked100.svg",
    "svgimages/chart/charttype_bar.svg",
    "svgimages/chart/charttype_bar3d.svg",
    "svgimages/chart/charttype_bar3dstacked.svg",
    "svgimages/chart/charttype_bar3dstacked100.svg",
    "svgimages/chart/charttype_barstacked.svg",
    "svgimages/chart/charttype_barstacked100.svg",
    "svgimages/chart/charttype_boxplot.svg",
    "svgimages/chart/charttype_bubble.svg",
    "svgimages/chart/charttype_bubble3d.svg",
    "svgimages/chart/charttype_candlestick.svg",
    "svgimages/chart/charttype_doughnut.svg",
    "svgimages/chart/charttype_doughnut3d.svg",
    "svgimages/chart/charttype_funnel.svg",
    "svgimages/chart/charttype_funnel3d.svg",
    "svgimages/chart/charttype_gantt.svg",
    "svgimages/chart/charttype_histogram.svg",
    "svgimages/chart/charttype_line.svg",
    "svgimages/chart/charttype_line3d.svg",
    "svgimages/chart/charttype_line3dstacked.svg",
    "svgimages/chart/charttype_line3dstacked100.svg",
    "svgimages/chart/charttype_linestacked.svg",
    "svgimages/chart/charttype_linestacked100.svg",
    "svgimages/chart/charttype_manhattanbar.svg",
    "svgimages/chart/charttype_nesteddoughnut.svg",
    "svgimages/chart/charttype_pareto.svg",
    "svgimages/chart/charttype_pie.svg",
    "svgimages/chart/charttype_pie3d.svg",
    "svgimages/chart/charttype_point.svg",
    "svgimages/chart/charttype_point3d.svg",
    "svgimages/chart/charttype_polararea.svg",
    "svgimages/chart/charttype_polarline.svg",
    "svgimages/chart/charttype_polarpoint.svg",
    "svgimages/chart/charttype_polarrangearea.svg",
    "svgimages/chart/charttype_radararea.svg",
    "svgimages/chart/charttype_radarline.svg",
    "svgimages/chart/charttype_radarpoint.svg",
    "svgimages/chart/charttype_radarrangearea.svg",
    "svgimages/chart/charttype_rangearea.svg",
    "svgimages/chart/charttype_rangearea3d.svg",
    "svgimages/chart/charttype_rangebar.svg",
    "svgimages/chart/charttype_scatterline.svg",
    "svgimages/chart/charttype_scatterpolarline.svg",
    "svgimages/chart/charttype_scatterradarline.svg",
    "svgimages/chart/charttype_sidebysidebar3dstacked.svg",
    "svgimages/chart/charttype_sidebysidebar3dstacked100.svg",
    "svgimages/chart/charttype_sidebysidebarstacked.svg",
    "svgimages/chart/charttype_sidebysidebarstacked100.svg",
    "svgimages/chart/charttype_sidebysidegantt.svg",
    "svgimages/chart/charttype_sidebysiderangebar.svg",
    "svgimages/chart/charttype_spline.svg",
    "svgimages/chart/charttype_spline3d.svg",
    "svgimages/chart/charttype_splinearea.svg",
    "svgimages/chart/charttype_splinearea3d.svg",
    "svgimages/chart/charttype_splinearea3dstacked.svg",
    "svgimages/chart/charttype_splinearea3dstacked100.svg",
    "svgimages/chart/charttype_splineareastacked.svg",
    "svgimages/chart/charttype_splineareastacked100.svg",
    "svgimages/chart/charttype_steparea.svg",
    "svgimages/chart/charttype_steparea3d.svg",
    "svgimages/chart/charttype_stepline.svg",
    "svgimages/chart/charttype_stepline3d.svg",
    "svgimages/chart/charttype_stock.svg",
    "svgimages/chart/charttype_sunburst.svg",
    "svgimages/chart/charttype_swiftplot.svg",
    "svgimages/chart/charttype_waterfall.svg",
    "svgimages/chart/sankey.svg",
    "svgimages/chart/treemap.svg"
};
                    return __ImagesCharts;
                case NodeImageSetType.Spreadsheet:
                    if (__ImagesSpreadsheet is null)
                        __ImagesSpreadsheet = new string[]
                    {
                            "svgimages/spreadsheet/createarea3dchart.svg",
    "svgimages/spreadsheet/createareachart.svg",
    "svgimages/spreadsheet/createbar3dchart.svg",
    "svgimages/spreadsheet/createbarchart.svg",
    "svgimages/spreadsheet/createbubble3dchart.svg",
    "svgimages/spreadsheet/createbubblechart.svg",
    "svgimages/spreadsheet/createconebar3dchart.svg",
    "svgimages/spreadsheet/createconefullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createconemanhattanbarchart.svg",
    "svgimages/spreadsheet/createconestackedbar3dchart.svg",
    "svgimages/spreadsheet/createcylinderbar3dchart.svg",
    "svgimages/spreadsheet/createcylinderfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createcylindermanhattanbarchart.svg",
    "svgimages/spreadsheet/createcylinderstackedbar3dchart.svg",
    "svgimages/spreadsheet/createdoughnutchart.svg",
    "svgimages/spreadsheet/createexplodeddoughnutchart.svg",
    "svgimages/spreadsheet/createexplodedpie3dchart.svg",
    "svgimages/spreadsheet/createexplodedpiechart.svg",
    "svgimages/spreadsheet/createfullstackedarea3dchart.svg",
    "svgimages/spreadsheet/createfullstackedareachart.svg",
    "svgimages/spreadsheet/createfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createfullstackedbarchart.svg",
    "svgimages/spreadsheet/createfullstackedlinechart.svg",
    "svgimages/spreadsheet/createline3dchart.svg",
    "svgimages/spreadsheet/createlinechart.svg",
    "svgimages/spreadsheet/createmanhattanbarchart.svg",
    "svgimages/spreadsheet/createpie3dchart.svg",
    "svgimages/spreadsheet/createpiechart.svg",
    "svgimages/spreadsheet/createpyramidbar3dchart.svg",
    "svgimages/spreadsheet/createpyramidfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createpyramidmanhattanbarchart.svg",
    "svgimages/spreadsheet/createpyramidstackedbar3dchart.svg",
    "svgimages/spreadsheet/createradarlinechart.svg",
    "svgimages/spreadsheet/createrotatedbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedconebar3dchart.svg",
    "svgimages/spreadsheet/createrotatedcylinderbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedbarchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedconebar3dchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedcylinderbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedfullstackedpyramidbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedpyramidbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedstackedbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedstackedbarchart.svg",
    "svgimages/spreadsheet/createrotatedstackedconebar3dchart.svg",
    "svgimages/spreadsheet/createrotatedstackedcylinderbar3dchart.svg",
    "svgimages/spreadsheet/createrotatedstackedpyramidbar3dchart.svg",
    "svgimages/spreadsheet/createstackedarea3dchart.svg",
    "svgimages/spreadsheet/createstackedareachart.svg",
    "svgimages/spreadsheet/createstackedbar3dchart.svg",
    "svgimages/spreadsheet/createstackedbarchart.svg",
    "svgimages/spreadsheet/createstackedlinechart.svg"
};
                    return __ImagesSpreadsheet;
                case NodeImageSetType.Suffix:
                    if (__ImagesSuffix is null)
                        __ImagesSuffix = new string[]
{
    "svgimages/icon%20builder/actions_addcircled.svg",
    "svgimages/icon%20builder/actions_deletecircled.svg",
    "svgimages/icon%20builder/actions_checkcircled.svg",
    "svgimages/icon%20builder/actions_removecircled.svg"
};
                    return __ImagesSuffix;
            }
            return null;
        }
        private string[] __ImagesDocuments;
        private string[] __ImagesActions;
        private string[] __ImagesFormats;
        private string[] __ImagesCharts;
        private string[] __ImagesSpreadsheet;
        private string[] __ImagesSuffix;
        private Color[] __BackColors;
        private Color[] __ForeColors;
        private string[] __StyleNames;
        internal enum NodeImageSetType
        {
            None,
            Documents,
            Actions,
            Formats,
            Charts,
            Spreadsheet,
            Suffix
        }

        protected NewNodePositionType __NewNodePosition = NewNodePositionType.None;
        protected enum NewNodePositionType { None, First, Last }
        #endregion
    }
}
