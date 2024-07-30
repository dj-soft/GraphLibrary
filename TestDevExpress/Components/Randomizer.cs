using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Noris.Clients.Win.Components;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress
{
    /// <summary>
    /// Generátor náhodných textů
    /// </summary>
    public class Randomizer
    {
        #region Náhodné slovo, věta, odstavec
        /// <summary>
        /// Vrať náhodné slovo
        /// </summary>
        /// <param name="firstUpper"></param>
        /// <returns></returns>
        public static string GetWord(bool firstUpper = false)
        {
            string word = WordBook[Rand.Next(WordBook.Length)];
            if (firstUpper) word = word.Substring(0, 1).ToUpper() + word.Substring(1);
            return word;
        }
        /// <summary>
        /// Vrať náhodnou sadu vět
        /// </summary>
        /// <param name="minWordCount"></param>
        /// <param name="maxWordCount"></param>
        /// <param name="minSentenceCount"></param>
        /// <param name="maxSentenceCount"></param>
        /// <returns></returns>
        public static string GetSentences(int minWordCount, int maxWordCount, int minSentenceCount, int maxSentenceCount)
        {
            string sentences = "";
            int sentenceCount = Rand.Next(minSentenceCount, maxSentenceCount);
            string eol = Environment.NewLine;
            for (int s = 0; s < sentenceCount; s++)
            {
                string sentence = GetSentence(minWordCount, maxWordCount, true);
                if (sentences.Length > 0)
                {
                    if (Rand.Next(3) == 0) sentences += eol;
                    else sentences += " ";
                }
                sentences += sentence;
            }
            return sentences;
        }
        /// <summary>
        /// Vrať pole náhodných vět
        /// </summary>
        /// <param name="minWordCount"></param>
        /// <param name="maxWordCount"></param>
        /// <param name="minSentenceCount"></param>
        /// <param name="maxSentenceCount"></param>
        /// <param name="addDot"></param>
        /// <returns></returns>
        public static string[] GetSentencesArray(int minWordCount, int maxWordCount, int minSentenceCount, int maxSentenceCount, bool addDot = false)
        {
            List<string> sentences = new List<string>();
            int sentenceCount = Rand.Next(minSentenceCount, maxSentenceCount);
            string eol = Environment.NewLine;
            for (int s = 0; s < sentenceCount; s++)
            {
                string sentence = GetSentence(minWordCount, maxWordCount, addDot);
                sentences.Add(sentence);
            }
            return sentences.ToArray();
        }
        /// <summary>
        /// Vrať náhodnou větu
        /// </summary>
        /// <param name="minCount"></param>
        /// <param name="maxCount"></param>
        /// <param name="addDot"></param>
        /// <returns></returns>
        public static string GetSentence(int minCount, int maxCount, bool addDot = false)
        {
            int count = Rand.Next(minCount, maxCount);
            return GetSentence(count, addDot);
        }
        /// <summary>
        /// Vrať náhodnou větu
        /// </summary>
        /// <param name="count"></param>
        /// <param name="addDot"></param>
        /// <returns></returns>
        public static string GetSentence(int count, bool addDot = false)
        {
            string sentence = "";
            for (int w = 0; w < count; w++)
                sentence += (sentence.Length > 0 ? ((Rand.Next(12) < 1) ? ", " : " ") : "") + GetWord((w == 0));
            if (addDot)
                sentence += GetItem(SentenceDots);
            return sentence;
        }
        #endregion
        #region MenuItems, Icons
        /// <summary>
        /// Vytvoří a vrátí pole jednoduchých prvků menu
        /// </summary>
        /// <param name="minCount"></param>
        /// <param name="maxCount"></param>
        /// <param name="imageType"></param>
        /// <param name="withNumberedItems"></param>
        /// <param name="withToolTip"></param>
        /// <returns></returns>
        public static IMenuItem[] GetMenuItems(int minCount, int maxCount, ImageResourceType imageType = ImageResourceType.PngFull, bool withNumberedItems = false, bool withToolTip = true)
        {
            int count = Rand.Next(minCount, maxCount);
            return GetMenuItems(count, imageType, withNumberedItems, withToolTip);
        }
        /// <summary>
        /// Vytvoří a vrátí pole jednoduchých prvků menu
        /// </summary>
        /// <param name="count"></param>
        /// <param name="imageType"></param>
        /// <param name="withNumberedItems"></param>
        /// <param name="withToolTip"></param>
        /// <returns></returns>
        public static IMenuItem[] GetMenuItems(int count, ImageResourceType imageType = ImageResourceType.PngFull, bool withNumberedItems = false, bool withToolTip = true)
        {
            
            List<IMenuItem> items = new List<IMenuItem>();
            for (int i = 0; i < count; i++)
                items.Add(createItem(i));
            return items.ToArray();

            IMenuItem createItem(int index)
            {
                DataMenuItem item = new DataMenuItem()
                {
                    ItemId = "Item" + index.ToString(),
                    Text = GetSentence(1, 6, false)
                };
                item.ImageName = GetIconName(imageType);
                if (withToolTip)
                {
                    item.ToolTipTitle = item.Text;
                    item.ToolTipText = GetSentences(3, 8, 2, 10);
                }
                if (withNumberedItems) item.Text = (index + 1).ToString() + ". " + item.Text;
                return item;
            }
        }
        public static string GetIconName(ImageResourceType imageType = ImageResourceType.PngFull)
        {
            switch (imageType)
            {
                case ImageResourceType.None: return null;
                case ImageResourceType.Svg: return GetIconNameSvg();
                case ImageResourceType.PngSmall: return GetIconNamePngSmall();
                case ImageResourceType.PngFull: return GetIconNamePngFull();
            }
            return null;
        }
        public static string GetIconNameSvg()
        {
            string[] resourcesSvg = new string[]
            {
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
                "svgimages/reports/changecharttype.svg"
            };
            return GetItem(resourcesSvg);
        }
        public static string GetIconNamePngSmall()
        {
            string[] resourcesPng = new string[]
                {
                "images/chart/3dclusteredcolumn_16x16.png",
                "images/chart/3dcolumn_16x16.png",
                "images/chart/3dcylinder_16x16.png",
                "images/chart/3dfullstackedarea_16x16.png",
                "images/chart/3dline_16x16.png",
                "images/chart/3dstackedarea_16x16.png",
                "images/chart/addchartpane_16x16.png",
                "images/chart/area_16x16.png",
                "images/chart/area2_16x16.png",
                "images/chart/area3_16x16.png",
                "images/chart/area3d_16x16.png",
                "images/chart/axes_16x16.png",
                "images/chart/axistitles_16x16.png",
                "images/chart/bar_16x16.png",
                "images/chart/bar2_16x16.png",
                "images/chart/barofpie_16x16.png",
                "images/chart/bottomcenterhorizontalinside_16x16.png",
                "images/chart/bottomcenterhorizontaloutside_16x16.png",
                "images/chart/bottomcenterverticalinside_16x16.png",
                "images/chart/bottomcenterverticaloutside_16x16.png",
                "images/chart/bottomlefthorizontalinside_16x16.png",
                "images/chart/bottomlefthorizontaloutside_16x16.png",
                "images/chart/bottomleftverticalinside_16x16.png",
                "images/chart/bottomleftverticaloutside_16x16.png",
                "images/chart/bottomrighthorizontalinside_16x16.png",
                "images/chart/bottomrighthorizontaloutside_16x16.png",
                "images/chart/bottomrightverticalinside_16x16.png",
                "images/chart/bottomrightverticaloutside_16x16.png",
                "images/chart/boxandwhisker_16x16.png",
                "images/chart/bubble_16x16.png",
                "images/chart/bubble2_16x16.png",
                "images/chart/bubble3d_16x16.png",
                "images/chart/clusteredbar_16x16.png",
                "images/chart/clusteredbar3d_16x16.png",
                "images/chart/clusteredcolumn_16x16.png",
                "images/chart/clusteredcone_16x16.png",
                "images/chart/clusteredcylinder_16x16.png",
                "images/chart/clusteredhorizontalcone_16x16.png",
                "images/chart/clusteredhorizontalcylinder_16x16.png",
                "images/chart/clusteredhorizontalpyramid_16x16.png",
                "images/chart/clusteredpyramid_16x16.png",
                "images/chart/colorlegend_16x16.png",
                "images/chart/column_16x16.png",
                "images/chart/column2_16x16.png",
                "images/chart/cone_16x16.png",
                "images/chart/doughnut_16x16.png",
                "images/chart/drilldown_16x16.png",
                "images/chart/drilldownonarguments_chart_16x16.png",
                "images/chart/drilldownonarguments_pie_16x16.png",
                "images/chart/drilldownonseries_chart_16x16.png",
                "images/chart/drilldownonseries_pie_16x16.png",
                "images/chart/explodeddoughnut_16x16.png",
                "images/chart/explodedpie_16x16.png",
                "images/chart/explodedpie3d_16x16.png",
                "images/chart/filledradarwithoutmarkers_16x16.png",
                "images/chart/fullstackedarea_16x16.png",
                "images/chart/fullstackedarea2_16x16.png",
                "images/chart/fullstackedbar_16x16.png",
                "images/chart/fullstackedbar2_16x16.png",
                "images/chart/fullstackedbar3d_16x16.png",
                "images/chart/fullstackedcolumn_16x16.png",
                "images/chart/fullstackedcolumn3d_16x16.png",
                "images/chart/fullstackedcone_16x16.png",
                "images/chart/fullstackedcylinder_16x16.png",
                "images/chart/fullstackedhorizontalcone_16x16.png",
                "images/chart/fullstackedhorizontalcylinder_16x16.png",
                "images/chart/fullstackedhorizontalpyramid_16x16.png",
                "images/chart/fullstackedline_16x16.png",
                "images/chart/fullstackedlinewithmarkers_16x16.png",
                "images/chart/fullstackedlinewithoutmarkers_16x16.png",
                "images/chart/fullstackedpyramid_16x16.png",
                "images/chart/fullstackedsplinearea_16x16.png",
                "images/chart/funnel_16x16.png",
                "images/chart/heatmapchart_16x16.png",
                "images/chart/highlowclose_16x16.png",
                "images/chart/highlowclose2_16x16.png",
                "images/chart/histogram_16x16.png",
                "images/chart/horizontalaxisbillions_16x16.png",
                "images/chart/horizontalaxisdefault_16x16.png",
                "images/chart/horizontalaxislefttoright_16x16.png",
                "images/chart/horizontalaxislogscale_16x16.png",
                "images/chart/horizontalaxismillions_16x16.png",
                "images/chart/horizontalaxisnone_16x16.png",
                "images/chart/horizontalaxisrighttoleft_16x16.png",
                "images/chart/horizontalaxisthousands_16x16.png",
                "images/chart/horizontalaxistitle_16x16.png",
                "images/chart/horizontalaxistitle_none_16x16.png",
                "images/chart/horizontalaxistopdown_16x16.png",
                "images/chart/horizontalaxiswithoutlabeling_16x16.png",
                "images/chart/changechartlegendalignment_16x16.png",
                "images/chart/changechartseriestype_16x16.png",
                "images/chart/chart_16x16.png",
                "images/chart/chartchangelayout_16x16.png",
                "images/chart/chartchangestyle_16x16.png",
                "images/chart/chartshowcaptions_16x16.png",
                "images/chart/chartsrotate_16x16.png",
                "images/chart/chartsshowlegend_16x16.png",
                "images/chart/charttitlesabovechart_16x16.png",
                "images/chart/charttitlescenteredoverlaytitle_16x16.png",
                "images/chart/charttitlesnone_16x16.png",
                "images/chart/chartxaxissettings_16x16.png",
                "images/chart/chartyaxissettings_16x16.png",
                "images/chart/chartyaxissettings2_16x16.png",
                "images/chart/kpi_16x16.png",
                "images/chart/labelsabove_16x16.png",
                "images/chart/labelsbelow_16x16.png",
                "images/chart/labelscenter_16x16.png",
                "images/chart/labelsinsidebase_16x16.png",
                "images/chart/labelsinsidecenter_16x16.png",
                "images/chart/labelsinsideend_16x16.png",
                "images/chart/labelsleft_16x16.png",
                "images/chart/labelsnone_16x16.png",
                "images/chart/labelsnone2_16x16.png",
                "images/chart/labelsoutsideend_16x16.png",
                "images/chart/labelsright_16x16.png",
                "images/chart/legendbottom_16x16.png",
                "images/chart/legendleft_16x16.png",
                "images/chart/legendleftoverlay_16x16.png",
                "images/chart/legendnone_16x16.png",
                "images/chart/legendright_16x16.png",
                "images/chart/legendrightoverlay_16x16.png",
                "images/chart/legendtop_16x16.png",
                "images/chart/line_16x16.png",
                "images/chart/line2_16x16.png",
                "images/chart/linewithmarkers_16x16.png",
                "images/chart/linewithoutmarkers_16x16.png",
                "images/chart/openhighlowclosecandlestick_16x16.png",
                "images/chart/openhighlowclosecandlestick2_16x16.png",
                "images/chart/openhighlowclosestock_16x16.png",
                "images/chart/othercharts_16x16.png",
                "images/chart/pareto_16x16.png",
                "images/chart/pie_16x16.png",
                "images/chart/pie2_16x16.png",
                "images/chart/pie3_16x16.png",
                "images/chart/pie3d_16x16.png",
                "images/chart/pielabelsdatalabels_16x16.png",
                "images/chart/pielabelstooltips_16x16.png",
                "images/chart/pieofpie_16x16.png",
                "images/chart/piestyledonut_16x16.png",
                "images/chart/piestylepie_16x16.png",
                "images/chart/point_16x16.png",
                "images/chart/previewchart_16x16.png",
                "images/chart/pyramid_16x16.png",
                "images/chart/radarwithmarkers_16x16.png",
                "images/chart/radarwithoutmarkers_16x16.png",
                "images/chart/rangearea_16x16.png",
                "images/chart/rangebar_16x16.png",
                "images/chart/sankeydiagram_16x16.png",
                "images/chart/scatter_16x16.png",
                "images/chart/scatterwithonlymarkers_16x16.png",
                "images/chart/scatterwithsmoothlines_16x16.png",
                "images/chart/scatterwithsmoothlinesandmarkers_16x16.png",
                "images/chart/scatterwithstraightlines_16x16.png",
                "images/chart/scatterwithstraightlinesandmarkers_16x16.png",
                "images/chart/sidebysiderangebar_16x16.png",
                "images/chart/spline_16x16.png",
                "images/chart/splinearea_16x16.png",
                "images/chart/stackedarea_16x16.png",
                "images/chart/stackedarea2_16x16.png",
                "images/chart/stackedbar_16x16.png",
                "images/chart/stackedbar2_16x16.png",
                "images/chart/stackedbar3d_16x16.png",
                "images/chart/stackedcolumn_16x16.png",
                "images/chart/stackedcolumn3d_16x16.png",
                "images/chart/stackedcone_16x16.png",
                "images/chart/stackedcylinder_16x16.png",
                "images/chart/stackedhorizontalcone_16x16.png",
                "images/chart/stackedhorizontalcylinder_16x16.png",
                "images/chart/stackedhorizontalpyramid_16x16.png",
                "images/chart/stackedline_16x16.png",
                "images/chart/stackedlinewithmarkers_16x16.png",
                "images/chart/stackedlinewithoutmarkers_16x16.png",
                "images/chart/stackedpyramid_16x16.png",
                "images/chart/stackedsplinearea_16x16.png",
                "images/chart/steparea_16x16.png",
                "images/chart/stepline_16x16.png",
                "images/chart/sunburst_16x16.png",
                "images/chart/topcenterhorizontalinside_16x16.png",
                "images/chart/topcenterhorizontaloutside_16x16.png",
                "images/chart/topcenterverticalinside_16x16.png",
                "images/chart/topcenterverticaloutside_16x16.png",
                "images/chart/toplefthorizontalinside_16x16.png",
                "images/chart/toplefthorizontaloutside_16x16.png",
                "images/chart/topleftverticalinside_16x16.png",
                "images/chart/topleftverticaloutside_16x16.png",
                "images/chart/toprighthorizontalinside_16x16.png",
                "images/chart/toprighthorizontaloutside_16x16.png",
                "images/chart/toprightverticalinside_16x16.png",
                "images/chart/toprightverticaloutside_16x16.png",
                "images/chart/treemap_16x16.png",
                "images/chart/verticalaxisbillions_16x16.png",
                "images/chart/verticalaxisbottomup_16x16.png",
                "images/chart/verticalaxisdefault_16x16.png",
                "images/chart/verticalaxislogscale_16x16.png",
                "images/chart/verticalaxismillions_16x16.png",
                "images/chart/verticalaxisnone_16x16.png",
                "images/chart/verticalaxisthousands_16x16.png",
                "images/chart/verticalaxistitles_16x16.png",
                "images/chart/verticalaxistitles_horizonlaltext_16x16.png",
                "images/chart/verticalaxistitles_none_16x16.png",
                "images/chart/verticalaxistitles_rotatedtext_16x16.png",
                "images/chart/verticalaxistitles_verticaltext_16x16.png",
                "images/chart/verticallaxiswithoutlabeling_16x16.png",
                "images/chart/waterfall_16x16.png"
                };
            return GetItem(resourcesPng);
        }
        public static string GetIconNamePngFull()
        {
            string[] resourcesPng = new string[]
            {
                "images/chart/3dclusteredcolumn_32x32.png",
                "images/chart/3dcolumn_32x32.png",
                "images/chart/3dcylinder_32x32.png",
                "images/chart/3dfullstackedarea_32x32.png",
                "images/chart/3dline_32x32.png",
                "images/chart/3dstackedarea_32x32.png",
                "images/chart/addchartpane_32x32.png",
                "images/chart/area_32x32.png",
                "images/chart/area2_32x32.png",
                "images/chart/area3_32x32.png",
                "images/chart/area3d_32x32.png",
                "images/chart/axes_32x32.png",
                "images/chart/axistitles_32x32.png",
                "images/chart/bar_32x32.png",
                "images/chart/bar2_32x32.png",
                "images/chart/barofpie_32x32.png",
                "images/chart/bottomcenterhorizontalinside_32x32.png",
                "images/chart/bottomcenterhorizontaloutside_32x32.png",
                "images/chart/bottomcenterverticalinside_32x32.png",
                "images/chart/bottomcenterverticaloutside_32x32.png",
                "images/chart/bottomlefthorizontalinside_32x32.png",
                "images/chart/bottomlefthorizontaloutside_32x32.png",
                "images/chart/bottomleftverticalinside_32x32.png",
                "images/chart/bottomleftverticaloutside_32x32.png",
                "images/chart/bottomrighthorizontalinside_32x32.png",
                "images/chart/bottomrighthorizontaloutside_32x32.png",
                "images/chart/bottomrightverticalinside_32x32.png",
                "images/chart/bottomrightverticaloutside_32x32.png",
                "images/chart/boxandwhisker_32x32.png",
                "images/chart/bubble_32x32.png",
                "images/chart/bubble2_32x32.png",
                "images/chart/bubble3d_32x32.png",
                "images/chart/clusteredbar_32x32.png",
                "images/chart/clusteredbar3d_32x32.png",
                "images/chart/clusteredcolumn_32x32.png",
                "images/chart/clusteredcone_32x32.png",
                "images/chart/clusteredcylinder_32x32.png",
                "images/chart/clusteredhorizontalcone_32x32.png",
                "images/chart/clusteredhorizontalcylinder_32x32.png",
                "images/chart/clusteredhorizontalpyramid_32x32.png",
                "images/chart/clusteredpyramid_32x32.png",
                "images/chart/colorlegend_32x32.png",
                "images/chart/column_32x32.png",
                "images/chart/column2_32x32.png",
                "images/chart/cone_32x32.png",
                "images/chart/doughnut_32x32.png",
                "images/chart/drilldown_32x32.png",
                "images/chart/drilldownonarguments_chart_32x32.png",
                "images/chart/drilldownonarguments_pie_32x32.png",
                "images/chart/drilldownonseries_chart_32x32.png",
                "images/chart/drilldownonseries_pie_32x32.png",
                "images/chart/explodeddoughnut_32x32.png",
                "images/chart/explodedpie_32x32.png",
                "images/chart/explodedpie3d_32x32.png",
                "images/chart/filledradarwithoutmarkers_32x32.png",
                "images/chart/fullstackedarea_32x32.png",
                "images/chart/fullstackedarea2_32x32.png",
                "images/chart/fullstackedbar_32x32.png",
                "images/chart/fullstackedbar2_32x32.png",
                "images/chart/fullstackedbar3d_32x32.png",
                "images/chart/fullstackedcolumn_32x32.png",
                "images/chart/fullstackedcolumn3d_32x32.png",
                "images/chart/fullstackedcone_32x32.png",
                "images/chart/fullstackedcylinder_32x32.png",
                "images/chart/fullstackedhorizontalcone_32x32.png",
                "images/chart/fullstackedhorizontalcylinder_32x32.png",
                "images/chart/fullstackedhorizontalpyramid_32x32.png",
                "images/chart/fullstackedline_32x32.png",
                "images/chart/fullstackedlinewithmarkers_32x32.png",
                "images/chart/fullstackedlinewithoutmarkers_32x32.png",
                "images/chart/fullstackedpyramid_32x32.png",
                "images/chart/fullstackedsplinearea_32x32.png",
                "images/chart/funnel_32x32.png",
                "images/chart/heatmapchart_32x32.png",
                "images/chart/highlowclose_32x32.png",
                "images/chart/highlowclose2_32x32.png",
                "images/chart/histogram_32x32.png",
                "images/chart/horizontalaxisbillions_32x32.png",
                "images/chart/horizontalaxisdefault_32x32.png",
                "images/chart/horizontalaxislefttoright_32x32.png",
                "images/chart/horizontalaxislogscale_32x32.png",
                "images/chart/horizontalaxismillions_32x32.png",
                "images/chart/horizontalaxisnone_32x32.png",
                "images/chart/horizontalaxisrighttoleft_32x32.png",
                "images/chart/horizontalaxisthousands_32x32.png",
                "images/chart/horizontalaxistitle_32x32.png",
                "images/chart/horizontalaxistitle_none_32x32.png",
                "images/chart/horizontalaxistopdown_32x32.png",
                "images/chart/horizontalaxiswithoutlabeling_32x32.png",
                "images/chart/changechartlegendalignment_32x32.png",
                "images/chart/changechartseriestype_32x32.png",
                "images/chart/chart_32x32.png",
                "images/chart/chartchangelayout_32x32.png",
                "images/chart/chartchangestyle_32x32.png",
                "images/chart/chartshowcaptions_32x32.png",
                "images/chart/chartsrotate_32x32.png",
                "images/chart/chartsshowlegend_32x32.png",
                "images/chart/charttitlesabovechart_32x32.png",
                "images/chart/charttitlescenteredoverlaytitle_32x32.png",
                "images/chart/charttitlesnone_32x32.png",
                "images/chart/chartxaxissettings_32x32.png",
                "images/chart/chartyaxissettings_32x32.png",
                "images/chart/chartyaxissettings2_32x32.png",
                "images/chart/kpi_32x32.png",
                "images/chart/labelsabove_32x32.png",
                "images/chart/labelsbelow_32x32.png",
                "images/chart/labelscenter_32x32.png",
                "images/chart/labelsinsidebase_32x32.png",
                "images/chart/labelsinsidecenter_32x32.png",
                "images/chart/labelsinsideend_32x32.png",
                "images/chart/labelsleft_32x32.png",
                "images/chart/labelsnone_32x32.png",
                "images/chart/labelsnone2_32x32.png",
                "images/chart/labelsoutsideend_32x32.png",
                "images/chart/labelsright_32x32.png",
                "images/chart/legendbottom_32x32.png",
                "images/chart/legendleft_32x32.png",
                "images/chart/legendleftoverlay_32x32.png",
                "images/chart/legendnone_32x32.png",
                "images/chart/legendright_32x32.png",
                "images/chart/legendrightoverlay_32x32.png",
                "images/chart/legendtop_32x32.png",
                "images/chart/line_32x32.png",
                "images/chart/line2_32x32.png",
                "images/chart/linewithmarkers_32x32.png",
                "images/chart/linewithoutmarkers_32x32.png",
                "images/chart/openhighlowclosecandlestick_32x32.png",
                "images/chart/openhighlowclosecandlestick2_32x32.png",
                "images/chart/openhighlowclosestock_32x32.png",
                "images/chart/othercharts_32x32.png",
                "images/chart/pareto_32x32.png",
                "images/chart/pie_32x32.png",
                "images/chart/pie2_32x32.png",
                "images/chart/pie3_32x32.png",
                "images/chart/pie3d_32x32.png",
                "images/chart/pielabelsdatalabels_32x32.png",
                "images/chart/pielabelstooltips_32x32.png",
                "images/chart/pieofpie_32x32.png",
                "images/chart/piestyledonut_32x32.png",
                "images/chart/piestylepie_32x32.png",
                "images/chart/point_32x32.png",
                "images/chart/previewchart_32x32.png",
                "images/chart/pyramid_32x32.png",
                "images/chart/radarwithmarkers_32x32.png",
                "images/chart/radarwithoutmarkers_32x32.png",
                "images/chart/rangearea_32x32.png",
                "images/chart/rangebar_32x32.png",
                "images/chart/sankeydiagram_32x32.png",
                "images/chart/scatter_32x32.png",
                "images/chart/scatterwithonlymarkers_32x32.png",
                "images/chart/scatterwithsmoothlines_32x32.png",
                "images/chart/scatterwithsmoothlinesandmarkers_32x32.png",
                "images/chart/scatterwithstraightlines_32x32.png",
                "images/chart/scatterwithstraightlinesandmarkers_32x32.png",
                "images/chart/scatterwithstraightlinesandmarkersx23_32x32.png",
                "images/chart/sidebysiderangebar_32x32.png",
                "images/chart/spline_32x32.png",
                "images/chart/splinearea_32x32.png",
                "images/chart/stackedarea_32x32.png",
                "images/chart/stackedarea2_32x32.png",
                "images/chart/stackedbar_32x32.png",
                "images/chart/stackedbar2_32x32.png",
                "images/chart/stackedbar3d_32x32.png",
                "images/chart/stackedcolumn_32x32.png",
                "images/chart/stackedcolumn3d_32x32.png",
                "images/chart/stackedcone_32x32.png",
                "images/chart/stackedcylinder_32x32.png",
                "images/chart/stackedhorizontalcone_32x32.png",
                "images/chart/stackedhorizontalcylinder_32x32.png",
                "images/chart/stackedhorizontalpyramid_32x32.png",
                "images/chart/stackedline_32x32.png",
                "images/chart/stackedlinewithmarkers_32x32.png",
                "images/chart/stackedlinewithoutmarkers_32x32.png",
                "images/chart/stackedpyramid_32x32.png",
                "images/chart/stackedsplinearea_32x32.png",
                "images/chart/steparea_32x32.png",
                "images/chart/stepline_32x32.png",
                "images/chart/sunburst_32x32.png",
                "images/chart/topcenterhorizontalinside_32x32.png",
                "images/chart/topcenterhorizontaloutside_32x32.png",
                "images/chart/topcenterverticalinside_32x32.png",
                "images/chart/topcenterverticaloutside_32x32.png",
                "images/chart/toplefthorizontalinside_32x32.png",
                "images/chart/toplefthorizontaloutside_32x32.png",
                "images/chart/topleftverticalinside_32x32.png",
                "images/chart/topleftverticaloutside_32x32.png",
                "images/chart/toprighthorizontalinside_32x32.png",
                "images/chart/toprighthorizontaloutside_32x32.png",
                "images/chart/toprightverticalinside_32x32.png",
                "images/chart/toprightverticaloutside_32x32.png",
                "images/chart/treemap_32x32.png",
                "images/chart/verticalaxisbillions_32x32.png",
                "images/chart/verticalaxisbottomup_32x32.png",
                "images/chart/verticalaxisdefault_32x32.png",
                "images/chart/verticalaxislogscale_32x32.png",
                "images/chart/verticalaxismillions_32x32.png",
                "images/chart/verticalaxisnone_32x32.png",
                "images/chart/verticalaxisthousands_32x32.png",
                "images/chart/verticalaxistitles_32x32.png",
                "images/chart/verticalaxistitles_horizonlaltext_32x32.png",
                "images/chart/verticalaxistitles_none_32x32.png",
                "images/chart/verticalaxistitles_rotatedtext_32x32.png",
                "images/chart/verticalaxistitles_verticaltext_32x32.png",
                "images/chart/verticallaxiswithoutlabeling_32x32.png",
                "images/chart/waterfall_32x32.png"
            };
            return GetItem(resourcesPng);
        }
        /// <summary>
        /// Typ zdroje ikony
        /// </summary>
        public enum ImageResourceType
        {
            None,
            Svg,
            PngSmall,
            PngFull
        }
        #endregion
        #region Zdroje slov
        /// <summary>
        /// Náhodná slova
        /// </summary>
        public static string[] WordBook { get { if (_WordBook is null) _WordBook = _GetWordBook(); return _WordBook; } }
        private static string[] _WordBook;
        /// <summary>
        /// Aktivní slovní zásoba
        /// </summary>
        public static WordBookType ActiveWordBook { get { return _ActiveWordBook; } set { _ActiveWordBook = value; _WordBook = null; } }
        private static WordBookType _ActiveWordBook = WordBookType.TriMuziNaToulkach;
        /// <summary>
        /// Zdroj slovní zásoby
        /// </summary>
        public enum WordBookType { TriMuziNaToulkach, TaborSvatych, CampOfSaints  }
        /// <summary>
        /// Vrátí pole náhodných slov
        /// </summary>
        /// <returns></returns>
        private static string[] _GetWordBook()
        {
            string text = (_ActiveWordBook == WordBookType.TriMuziNaToulkach ? Text_TriMuziNaToulkach : 
                          (_ActiveWordBook == WordBookType.TaborSvatych ? Text_TaborSvatych :
                          (_ActiveWordBook == WordBookType.CampOfSaints ? Text_CampOfSaints :
                          Text_TriMuziNaToulkach)));

            // Některé znaky odstraníme, text rozdělíme na slova, a z nich vybereme pouze slova se 3 znaky a více:
            text = text.Replace("„", " ");
            text = text.Replace("“", " ");
            text = text.Replace(".", " ");
            text = text.Replace(",", " ");
            text = text.Replace(";", " ");
            text = text.Replace(":", " ");
            text = text.Replace("?", " ");
            text = text.Replace("!", " ");
            text = text.Replace(",", " ");
            text = text.Replace("«", " ");
            text = text.Replace("»", " ");
            text = text.Replace("(", " ");
            text = text.Replace(")", " ");
            text = text.Replace("[", " ");
            text = text.Replace("]", " ");
            text = text.Replace("{", " ");
            text = text.Replace("}", " ");
            text = text.Replace("<", " ");
            text = text.Replace(">", " ");
            text = text.Replace("0", " ");
            text = text.Replace("1", " ");
            text = text.Replace("2", " ");
            text = text.Replace("3", " ");
            text = text.Replace("4", " ");
            text = text.Replace("5", " ");
            text = text.Replace("6", " ");
            text = text.Replace("7", " ");
            text = text.Replace("8", " ");
            text = text.Replace("9", " ");
            text = text.Replace("+", " ");
            text = text.Replace("=", " ");
            text = text.ToLower();
            var words = text.Split(" \r\n\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            return words.Where(w => w.Length >= 3).ToArray();
        }
        /// <summary>
        /// Text "Tři muži na toulkách"
        /// </summary>
        public static string Text_TriMuziNaToulkach
        {
            get
            {
                return @"„Potřebujeme změnit způsob života,“ řekl Harris.
V tom okamžiku se otevřely dveře a nakoukla k nám paní Harrisová; že prý ji posílá Ethelberta, aby mi
připomněla, že kvůli Clarencovi nesmíme přijít domů moc pozdě. Já si teda myslím, že Ethleberta si o naše děti
dělá zbytečné starosti. Tomu klukovi vlastně vůbec nic nebylo. Dopoledne byl venku s tetou; a když se roztouženě
zakouká do výkladní skříně u cukráře, ta teta ho vezme do krámu a tak dlouho mu kupuje trubičky se šlehačkou a
mandlové dorty, dokud kluk neprohlásí, že už nemůže, a zdvořile, leč rezolutně cokoli dalšího sníst neodmítne. U
oběda pak pochopitelně nechce druhou porci nákypu a Ethelberta si hned myslí, že je to příznak nějaké nemoci.
Paní Harrisová dále dodala, že bychom vůbec měli přijít co nejdřív nahoru, pro své vlastní blaho, jinak že
zmeškáme výstup slečny Muriel, která přednese Bláznivou svačinu z Alenky v kraji divů. Muriel je ta Harrisova
starší, osmiletá; dítě bystré a inteligentní; ale já za svou osobu ji poslouchám raději, když recituje něco vážného.
Řekli jsme paní Harrisové, že jen dokouříme cigarety a přijdeme co nevidět; a prosili jsme ji, aby Muriel
nedovolila začít, dokud tam nebudeme. Slíbila, že se vynasnaží udržet to dítko na uzdě co nejdéle, a odešla. A
jakmile se za ní zavřely dveře, Harris se vrátil k větě, při níž ho prve přerušila.
„Důkladně změnit způsob života,“ řekl. „No však vy víte, jak to myslím.“
Problém byl jenom v tom, jak toho dosáhnout.
George navrhoval „úřední záležitost“. To bylo pro George typické, takový návrh. Svobodný mládenec se
domnívá, že vdaná žena nemá ponětí, jak se vyhnout parnímu válci. Znal jsem kdysi jednoho mládence, inženýra,
který si usmyslil, že si „v úřední záležitosti“ vyjede do Vídně. Jeho žena si přála vědět, v jaké úřední záležitosti.
Řekl jí tedy, že dostal za úkol navštívit všechny doly v okolí rakouského hlavního města a podat o nich hlášení.
Manželka prohlásila, že pojede s ním; taková to byla manželka. Pokoušel se jí to rozmluvit; vykládal jí, že důl není
vhodné prostředí pro krásnou ženu. Odvětila, to že instinktivně vycítila sama, a že s ním tedy nebude fárat dolů do
šachet; jen ho k nim každé ráno doprovodí a pak se až do jeho návratu na zem bude bavit po svém; bude se dívat
po vídeňských obchodech a sem tam si koupí pár věciček, které se jí třeba budou hodit. Protože s tím programem
přišel sám, nevěděl, chudák, jak se z něho vyvléci; a tak deset dlouhých letních dní skutečně trávil v dolech v okolí
Vídně a po večerech o nich psal hlášení a jeho žena je sama odesílala jeho firmě, která o ně vůbec neměla zájem.
Ne že bych si myslel, že Ethelberta nebo paní Harrisová patří k téhle sortě žen, ale s „úřední záležitostí“ se
prostě nemá přehánět - tu si má člověk schovávat jen pro případ potřeby zcela naléhavé.
„Ne, ne,“ pravil jsem tedy, „na to se musí jít zpříma a mužně. Já řeknu Ethelbertě, že jsem dospěl k závěru, že
manžel nikdy nepozná pravou cenu štěstí, když se mu těší neustále. Řeknu jí, že se chci naučit vážit si výhod, jichž
se mi dostává, vážit si jich tak, jak si to zasluhují, a z toho důvodu že se nejméně na tři týdny hodlám násilím
odtrhnout od ní a od dětí. A řeknu jí,“ dodal jsem obraceje se k Harrisovi, „žes to byl ty, kdo mě na mé povinnosti
v tomto směru upozornil; tobě že vděčíme...“
Harris tak nějak zbrkle postavil na stůl svou sklenici.
„Jestli tě smím o něco prosit, člověče,“ přerušil mě, „tak nic takového radši neříkej. Ethelberta by se o tom
určitě zmínila mé ženě a... no, já si zkrátka nechci přisvojovat zásluhy, které mi nepatří.“
„Jak to, že ti nepatří?“ pravil jsem. „To byl přece tvůj nápad!“
„Ale tys mě na něj přivedl,“ znovu mě přerušil Harris. „Říkal jsi přece, že to je chyba, když člověk zapadne do
vyježděných kolejí, a nepřetržitý život v kruhu rodinném že otupuje ducha.“
„To bylo míněno všeobecně,“ vysvětloval jsem.
„Mně to připadalo velice výstižné,“ pravil Harris, „a říkal jsem si, že to budu citovat Claře; Clara si o tobě
myslí, že jsi člověk velice rozumný, vím, že má o tobě vysoké mínění. A tak jsem přesvědčen, že...“
Teď jsem zase já přerušil jeho: „Hele, radši nebudeme nic riskovat. Tohle jsou choulostivé věci. A já už vím,
jak na to. Řekneme, že s tím nápadem přišel George.“
Georgeovi, jak si často s rozhořčením všímám, naprosto chybí přívětivá ochota podat někomu pomocnou ruku.
Řekli byste, že příležitost vysvobodit dva staré kamarády z těžkého dilematu přímo uvítá; ale on místo toho velice
zprotivněl.
„Jen si to zkuste,“ pravil, „a já jim oběma řeknu, že můj původní návrh zněl, abychom jeli společně, s dětmi a s
celými rodinami; já že bych byl s sebou vzal svou tetu a že jsme si mohli najmout rozkošný starý zámeček v
Normandii, o kterém dobře vím a který stojí hned u moře, kde je podnebí speciálně vhodné pro choulostivé dětičky
a kde mají mléko, jaké se v Anglii nesežene. A ještě jim řeknu, že vy jste ten návrh úplně rozmetali kategorickou
námitkou, že nám bude mnohem líp, když pojedeme sami.“
S člověkem, jako je George, nemá smysl jednat vlídně; na takového platí jen pevná rozhodnost.
„Jen si to zkus,“ pravil Harris, „a já, co mě se týče, tu tvou nabídku okamžitě přijmu. A ten zámek si najmeme.
Ty s sebou vezmeš tetu - o to se postarám - a protáhneme si to na celý měsíc. Naše děcka se v tobě vidí; Jerome a
já nebudeme nikdy k dosažení. Slíbil jsi, že Edgara naučíš chytat ryby; a hrát si na divokou zvěř, to taky zůstane na
Tři muži na toulkách
174
tobě. Dick a Muriel beztak od minulé neděle nemluví o ničem jiném než o tom, jak jsi jim dělal hrocha. Budeme v
hájích pořádat společné pikniky - jenom nás jedenáct - a večer co večer bude na programu hudba a recitace.
Muriel, jak víš, umí zpaměti už šest básniček; a ostatní děti se učí ohromně rychle.“
A tak se George podvolil - on nemá moc pevné zásady -, i když ne zrovna ochotně. Co mu prý zbývá, když jsme
takoví neřádi a zbabělci a falešníci, že bychom se dokázali snížit k tak mrzkým úkladům? A jestli prý nemám v
úmyslu vypít celou láhev toho claretu sám, tak ať mu laskavě naleju aspoň jednu skleničku. A ještě dodal, poněkud
nelogicky, že je to ostatně úplně jedno, neboť jak Ethelberta tak paní Harrisová jsou ženy velice prozíravé a mají o
něm mnohem lepší mínění, než aby byť jenom na okamžik uvěřily, že by takový návrh mohl opravdu vyjít od něho.
Tento nedůležitý bod byl tedy projednán a zbývala otázka, jak máme způsob života změnit.
Harris byl, jako obvykle, pro moře. Ví prý o jedné plachetnici - pro nás jako stvořené -, kterou bychom dokázali
zvládnout sami, bez té hordy ulejváků, kteří se jen flákají a přitom stojí spoustu peněz a zbavují plavbu veškeré
romantiky. Když prý bude mít k ruce jednoho šikovného plavčíka, může ji řídit úplně sám. Ale my jsme tu
plachetnici znali a hned jsme mu to připomněli; už jsme si na ní jednou s Harrisem vyjeli. Ta loď páchne ztuchlou
vodou, které má plné dno, a zkaženou zeleninou a spoustou všelijakých dalších smradů, vedle nichž žádný
normální mořský vzduch nemá nejmenší naději se prosadit. A pokud by si měl přijít na své jenom čich, pak přece
můžeme strávit jeden týden někde v rybí tržnici. Kromě toho není na té lodi místečko, kam by se člověk mohl
schovat před deštěm; kajuta má rozměry tři a půl metru krát jeden a půl metru a polovinu toho prostoru zabírají
kamna, která se rozpadávají, jak jen se chystáte v nich zatopit. Koupat se musíte na palubě a ručník uletí do moře,
zrovna když lezete z kádě. Harris a plavčík obstarávají všechnu práci, která může člověka bavit - napínají a
podkasávají plachty, kýlují loď a pouštějí si ji volně po větru a tak podobně - zatímco na George a mne zbývá
loupání brambor a umývání nádobí.
„No prosím,“ řekl Harris, „tak si teda opatříme pořádnou jachtu s kapitánem a vyjeďme si se vší parádou!“
Ale já jsem i proti tomuto řešení protestoval. Tyhle kapitány moc dobře znám; výlet po moři, to pro ně znamená
kotvit v dosahu souše, aby měli pár kroků k ženě a k rodině, o zamilované hospodě ani nemluvě.
Před lety, když jsem byl ještě mladý a nezkušený, jsem si jednou sám najal plachetnici. Tři okolnosti se spojily,
aby mě vehnaly do toho nerozumu: potkalo mě neočekávané štěstí; Ethelberta projevila touhu po mořském vánku;
a zrovna příští ráno jsem v klubu čirou náhodou vzal do ruky jedno číslo Sportovce a přišel tam na tento inzerát:
MILOVNÍKŮM PLACHTĚNÍ. - Jedinečná příležitost - Ferina, jola o 28 tunách. - Vlastník, odvolaný náhle v
obchodní záležitosti do ciziny, ochotně pronajme tohoto luxusně vybaveného „mořského chrta“ na jakoukoli kratší
i delší dobu. - Dvě kajuty a salón; pianino značky Woffenkoff; nový kotel na vyvářku. - 10 guinejí týdně. - Bližší
informace u firmy Pertwee a spol., Bucklersbury 3A.
To mi připadalo jako vyslyšená modlitba. „Nový kotel na vyvářku“ mě sice nikterak nezajímal; těch pár věcí,
které bude zapotřebí přeprat, klidně počká až domů, říkal jsem si. „Pianino značky Woffenkoff“, to však znělo
lákavě. Představil jsem si Ethelbertu, jak večer hraje - něco s refrénem, který po několika zkouškách může s námi
sborově pět posádka -, zatímco náš pohyblivý domov uhání „jako chrt“ po stříbrných vlnách.
Vzal jsem si drožku a na udanou adresu jsem si rovnou zajel. Pan Pertwee byl pán nenápadného vzhledu a měl
neokázalou kancelář ve třetím poschodí. Ukázal mi akvarel Feriny, letícího po větru. Paluba byla skloněna k
oceánu v úhlu 95 stupňů. Na palubě nebyla na tom obrázku ani živá duše; všecky patrně sklouzly do moře. Já taky
nechápu, jak by se tam někdo byl mohl udržet, ledaže by byl přitlučen hřebíky. Poukázal jsem na tuto nevýhodu,
ale agent mi vyložil, že ten obrázek představuje Ferinu v okamžiku, kdy se za onoho svého pověstného vítězného
závodu o medwayský pohár obrací o 180 stupňů a obeplouvá, nebím už co. Pan Pertwee předpokládal, že o té
události je mi všechno známo, tak jsem se radši na nic neptal. Dvě skvrnky až u samého rámu, které jsem v prní
chvíli pokládal za moly, zobrazovaly, jak se ukázalo, druhého a třetího vítěze v té proslulé regatě. Fotografie
Feriny kotvícího u Gravesendu, byla už méně impozantní, zato slibovala větší stabilitu. A jelikož všechny
odpovědi na mé dotazy zněly uspokojivě, najal jsem si tu loď na čtrnáct dní. Pan Pertwee pravil, že to je šťastná
náhoda, že ji chci pouze na čtrnáct dní - později jsem mu dal za pravdu -, protože ta doba přesně navazuje na další
pronájem. Kdybych prý Ferinu žádal na tři týdny, nemohl by mi vůbec vyhovět.
Když jsme se takto dohodli, pan Pertwee se mě zeptal, jestli jsem si už vyhlédl nějakého kapitána. Že jsem si
žádného nevyhlédl, to byla další šťastná náhoda - štěstí mi zřejmě přálo ve všem všudy - ježto pan Pertwee byl
přesvědčen, že nemohu udělat nic lepšího, než se přidržet pana Goylese, v jehož péči se loď v současné době
nalézá; je to znamenitý kapitán, ujišťoval mě pan Pertwee, námořník, který zná moře jako manžel vlastní manželku
a pod jehož velením nikdy nikdo nepřišel o život.
Pořád ještě bylo časné dopoledne a plachetnice kotvila u Harwiche. Chytil jsem vlak v deset pětačtyřicet z
nádraží Liverpool Street a v jednu hodinu jsem už rozmlouval s panem Goylesem přímo na palubě. Pan Goyles byl
obtloustlý chlapík, který měl v sobě něco otcovského. Vyložil jsem mu, jak si to představuji, že bych totiž rád
obeplul ty ostrovy nad Holandskem a pak to vzal nahoru k Norsku. Kapitán řekl „Výborně, pane,“ a zatvářil se,
jako kdyby ho vyhlídka na tu cestu nadchla; sám prý z ní bude mít požitek. Přešli jsme k otázce potravinových
zásob a pan Goyles projevil nadšení ještě větší. Přiznávám se ovšem, že množství potravin, které navrhoval, mě
překvapilo. Být to v dobách kapitána Drakea a španělského panství v karibské oblasti, byl bych pojal obavy, že se
chystá k něčemu nezákonnému. Ale on se tím svým otcovským způsobem zasmál a ujistil mě, že to nikterak
nepřeháníme. A když něco zbude, tak si to rozdělí a vezme domů lodní posádka - tak to bylo zřejmě zvykem. Já
měl sice dojem, že tu posádku zásobím na celou zimu, ale nechtěl jsem vypadat jako držgrešle, a tak jsem už nic
neříkal. Požadované množství nápojů mě překvapilo rovněž. Já jsem naplánoval tolik, kolik jsme podle mého
Tři muži na toulkách
175
odhadu mohli spotřebovat my sami, a pak pak Goyles zahovořil jménem posádky. K jeho cti musím říci, že o to
své mužstvo vskutku pečoval.
„Neradi bychom zažili nějaké orgie, pane Goylesi,“ namítl jsem.
„Orgie!“ zvolal pan Goyles. „Tahle kapička jim stačí tak akorát do čaje!“
A vysvětlil mi, že se řídí heslem: „Opatři si dobrý chlapy a dobře se o ně starej!“
„Pak vám odvedou lepší práci,“ dodal pan Goyles, „a rádi přijdou zas.“
Já za svou osobu jsem si ani moc nepřál, aby přišli zas. Začínal jsem jich mít až po krk, a to jsem je ještě ani
neviděl; pro mě to byla posádka chamtivá a nenažraná. Ale pan Goyles stál tak bodře na svém a já byl tak
nezkušený, že jsem mu opět nechal volnou ruku. A on slíbil, že osobně dohlédne, aby ani v této kategorii nepřišlo
nic nazmar.
I výběr členů posádky jsem nechal na něm. Říkal, že kvůli mně to všechno může zastat, a taky zastane, s dvěma
lodníky a jedním plavčíkem. Jestli narážel na likvidaci zásob jídla a pití, pak na to taková posádka rozhodně
nemohla stačit; ale snad měl na mysli obsluhu plachetnice.
Na zpáteční cestě jsem se stavil u svého krejčího a objednal jsm si úbor na jachtu a bílý klobouk, a krejčí slíbil,
že sebou hodí a že to všechno ušije včas. A pak jsem se vrátil domů a řekl jsem Ethelbertě, co všechno jsem
zařídil. Její radost kalila jen jediná obava - jestli jí švadleny budou moci včas ušít úbor na jachtu. To jsou ty
ženské!
Svatební cestu, na kterou jsme si vyjeli teprve před nedávnem, jsme museli předčasně ukončit, a tak jsme se
rozhodli, že tentokrát s sebou nikoho nepozveme a necháme si celou plachetnici jenom pro sebe. A že jsme se
takto rozhodli, za to dodnes děkuji nebesům. V pondělí jsme se nastrojili do samých nových věcí a vyrazili jsme.
Co měla na sobě Ethelberta, to už si nepamatuji, vím jenom, že jí to ohromně slušelo. Já měl oblek tmavomodrý,
lemovaný úzkou bílou paspulkou, což bylo myslím velice efektní.
Pan Goyles nás už čekal na palubě a oznámil nám, že je prostřeno k obědu. Kuchaře, to musím uznat, opatřil
znamenitého. Schopnosti ostatních členů posádky jsem neměl příležitost posoudit. Ale podle toho, jak vypadali za
odpočinku, mohu říci, že dělali dojem mužstva sympatického.
Představoval jsem si, že až se i posádka naobědvá, ihned zvedneme kotvu a že se budu opírat o zábradlí, s
doutníkem v ústech a s Ethelbertou po boku, a budu se dívat, jak se bílé útesy mé otčiny pomaloučku noří za obzor.
Ethelberta i já jsme se svých rolí v tomto představení řádně ujali a čekali jsme, majíce celou palubu sami pro sebe.
„Dávají si načas,“ prohodila Ethelberta.
„Nu,“ odvětil jsem já, „jestli mají ve čtrnácti dnech sníst aspoň polovičku toho, co je v této lodi uskladněno,
budou potřebovat na každé jídlo hezky slušnou dobu. Radši na ně nespěchejme, nebo z toho všeho nespořádají ani
čtvrtinu.“
„Zřejmě šli už spat,“ řekla Ethelberta o něco později. „Vždyť je pomalu čas na svačinu.“
Chovali se vskutku velice tiše. Šel jsem na příď a zavolal jsem dolů pod schůdky na kapitána Goylese. Zavolal
jsem na něj třikrát, a teprve potom se pomalu vyškrábal nahoru. Připadal mi nemotornější a starší, než když jsem
ho viděl naposled. Z pusy mu trčel vyhaslý doutník.
„Až budete se vším hotov, pane kapitáne, tak vyplujeme,“ řekl jsem.
Kapitán Goyles vyňal z pusy ten doutník.
„Dneska ne, pane, když dovolíte,“ odvětil.
„Ale! Copak se vám na dnešku nelíbí?“ zeptal jsem se. Vím, že námořníci jsou cháska pověrčivá, a tak jsem si
myslel, že pondělek třeba považují za den nešťastný.
„Den ten by nevadil,“ odpověděl kapitán Goyles, „ale vítr mi dělá starosti. A nevypadá na to, že by se chtěl
změnit.“
„Copak potřebujeme, aby se změnil?“ divil jsem se. „Podle mého je přesně takový, jaký má být - měli bychom
ho v zádech.“
„No právě, právě,“ přitakal kapitán Goyles, „v zádech, to je ten správnej výraz. V těch zádech bysme totiž měli
smrt, kdybysme, nedej Pámbu, museli v tomhle vyplout. Abyste rozuměl, pane,“ vysvětloval v odpověď na můj
udivený pohled, „tohle je vítr, kterýmu my říkáme »soušák«, poněvadž fouká přímo od souše.“
Musel jsem uznat, že ten člověk má pravdu: vítr skutečně foukal přímo od souše.
„V noci se možná obrátí,“ pravil kapitán Goyles už trochu nadějněji. „Naštěstí není prudkej, a tahle loď sedí
pevně.“
Pak si dal dutník zpátky dopusy a já se vrátil na záď a vyložil jsem Ethelbertě, proč se náš odjezd odkládá.
Ethelberta už neměla tak jásavou náladu, jako když jsme se nalodili, a přála si vědět, proč nemůžeme vyplout,
když vítr fouká od souše.
„Kdyby nevanul od souše,“ prohlásila, „tak by vanul od moře a hnal by nás zpátky ke břehu. Já si myslím, že
tohle je zrovna ten vítr, jaký potřebujeme.“
„To z tebe mluví tvá nezkušenost, miláčku,“ poučoval jsem ji. „To se jen tak zdá, že tohle je zrovna ten vítr,
jaký potřebujeme, ale není to ten vítr. Tomuhle větru my říkáme soušák a soušák je vždycky velice nebezpečný.
Ethelberta chtěla vědět, proč je soušák velice nebezpečný.
Ta její neústupnost mně už začínala jít na nervy; ale asi jsem sám nebyl v nejlepší kondici; monotónní houpání
zakotvené plachetničky působí na činorodého ducha depresívně.
„Podrobně ti to vysvětlit nemůžu,“ odvětil jsem podle pravdy, „vím jenom, že vyplout za tohoto větru by byl
vrchol hazardérství a mně na tobě příliš záleží, má drahá, než abych tě zbytečně vystavoval nebezpečí.“
Tři muži na toulkách
176
To jsem považoval za dost obratné uzavření debaty, ale Ethelberta na to podotkla, že za těchto okolností jsme
se mohli klidně nalodit až v úterý, a sešla do podpalubí.
Nazítří se vítr otočil k severu; byl jsem vzhůru ož od časného jitra a hned jsem na tu změnu upozornil kapitána
Goylese.
„No práve, právě, pane,“ pravil. „Je to smůla, ale to se nedá nic dělat.“
„Tak vy máte za to, že dnes vyplout nemůžeme?“ odvážil jsem se zeptat.
Kapitán se na mně nerozhněval. Jenom se zasmál.
„Inu, pane,“ povídá, „kdybyste chtěl jet do Ipswiche, tak bysme lepší vítr mít nemohli, jenomže my máme
namířeno k holandskýmu pobřeží a v tom pádě - no co vám mám povídat?“
Sdělil jsem tuto novinu Ethelbertě a dohodli jsme se, že ten den strávíme na břehu. Harwich není moc veselé
město, k večeru je tam dokonce dost velká nuda. „U doverského dvora“ jsme si dali čaj a pár chlebíčků s
řeřichovým salátem a pak jsme se vrátili na nábřeží, abychom se podívali, co dělá loď a kapitán Golyes. Na
kapitána jsme čekali celou hodinu. Když přišel, byl v mnohem lepší náladě než my; kdyby mi sám nebyl řekl, že
než jde na kutě, jakživ nevypije víc než jednu sklenici horkého grogu, byl bych si myslel, že je opilý.
Nazítří ráno vanul vítr k jihu, což kapitána Goylese nemálo zneklidnilo; ukázalo se totiž, že je stejně
nebezpečné vyplout, jako zůstat tam, kde jsme; zbývala nám jediná naděje: že se vítr změní, dřív než se nám něco
stane. To už Ethelberta pojala k té plachetnici značnou nechuť; prohlásila, že by mnohem raději strávila týden v
takové té lázeňské kabině na kolečkách, neboť lázeňská kabina na kolečkách se aspoň dá pevně postavit.
Strávili jsme další den v Harwichi a noc po něm - i tu příští - jsme přespali „U královy hlavy“, jelikož vítr vanul
neustále k jihu. V pátek foukal vítr přesně od východu. Kapitána Goylese jsem potkal na nábřeží a zmínil jsem se
mu, že za těchto okolností bychom se snad mohli vydat na cestu. Má umíněnost ho zjevně rozčílila.
„Kdybyste se v tom kapánel líp vyznal, pane,“ pravil, „sám byste pochopil, že to není možný. Vždyť vítr fouká
rovnou od moře.“
„Pane kapitáne,“ řekl jsem, „povězte mi laskavě, co jsem si to vlastně najal. Je to plachetnice nebo hausbót?“
Vypadal, jako kdyby ho ten dotaz překvapil.“
„Je to jola,“ povídá.
„Abyste totiž rozuměl, oč mi jde,“ na to já. „Může se s tou věcí vůbec pohnout? Nebo je tady pevně
přimontovaná? Jestli je přimontovaná,“ dodal jsem, „upřímně mi to prozraďte, a my si seženeme pár truhlíků s
břečťanem, dáme si je pod okénka z kajuty, palubu si osázíme dalšími kytičkami a natáhneme si přes ni plátěnou
střechu a prostě si to tady pěkně zvelebíme. Jestli se ale ta věc může hýbat...“
„Hejbat!“ skočil mi do řeči kapitán Goyles. „Když má Ferina za sebou ten správnej vítr...“
„A jaký je to vítr, ten správný?“ ptám se.
To kapitánu Goylesovi očividně zamotalo hlavu.
„V tomto týdnu,“ pravil jsem dále, „jsme už měli vítr od severu, od jihu, od východu, od západu - a to s
různými modifikacemi. Jestli víte o nějakém dalším bodu na větrné růžici, z něhož může vítr dout, řekněte mi o
něm, a já ještě počkám. Jestli o žádném nevíte a jestli vaše kotva už nezarostla do dna oceánu, tak ji dneska
zvedneme a uvidíme, co to bude dělat.“
Z toho vyrozuměl, že jsem odhodlán ke všemu.
„No prosím!“ řekl. „Vy jste pán, já jsem kmán. Mám, zaplať Pámbu, jenom jedno děcko, který je na mě závislý,
a vykonavatelé vaší poslední vůle budou nepochybně vědět, jaký povinnosti je čekaj vůči mý starý.“
Jeho slavnostně vážný tón na mě zapůsobil.
„Pane Goylesi,“ řekl jsem, „jednejte se mnou na rovinu. Existuje nějaká naděje, nebo nějaké počasí, jež nám
umožní dostat se z tohohle pitomého hnízda?“
V kapitánu Goylesovi opět ožila ta jeho bodrá vlídnost.
„Víte, pane,“ povídá, „tohle je moc divný pobřeží. Jak už je člověk jednou venku na moři, tak je všechno v
pořádku, ale dostat se tam v takový skořápce, jak je tahle - víte, pane, upřímně řečeno, to je pěkná fuška.“
Rozloučili jsme se, když mě kapitán Goyles ujistil, že bude bdít nad počasím jako matka nad svým spícím
robátkem; to bylo jeho vlastní přirovnání a mě dost dojalo. Pak jsem ho zas viděl v poledne; bděl nad počasím za
oknem hospody „U řetězu a kotvy“.
Toho odpoledne v pět hodin se na mě usmálo štěstí; v prostředku hlavní třídy jsem potkal dva své kamarády,
plachtaře, kteří se v Harwichi museli zdržet, protože se jim polámalo kormidlo. Vyprávěl jsem jim, co mě potkalo,
a je to zjevně spíš pobavilo než překvapilo. kapitán Goyles a jeho lodníci pořád ještě bděli nad počasím. Běžel
jsem tedy ke „Králově hlavě“ a zburcoval jsem Ethelbertu. Pak jsme se všichni čtyři tiše přikradli na nábřeží a ke
své lodi. Na palubě byl jenom plavčík; moji dva přátelé se ujali velení a v šest hodin už jsme vesele klouzali podél
pobřeží k severu.
Tu noc jsme zakotvili v Aldoborough a příštího dne jsme dorazili do Yarmouthu. Tam se moji přátelé s námi
museli rozloučit, a tak jsme se orzhodl plachetnici opustit. Zásoby jsme hned zrána prodali na pláži v dražbě.
Prodělal jsem na tom, ale hřálo mě vědomí, že jsem doběhl kapitána Goylese. Ferinu jsem svěřil jednomu
místnímu námořníkovi, který ji za pár zlaťáků slíbil dopravit zpátky do Harwiche; a do Londýna jsme se vrátili
vlakem. Možná že všechny plachetnice nejsou jako Ferina a všichni kapitáni jako pan Goyles, ale já jsem po téhle
zkušenosti jak proti plachetnicím, tak proti jejich kapitánům zaujatý.
I George byl toho mínění, že plachetnice by znamenala spoustu odpovědnosti, a tak jsme tento nápad zavrhli.
„A co řeka?“ nadhodil Harris. „Na řece jsme zažili moc pěkné časy.“
Tři muži na toulkách
177
George mlčky zabafal ze svého doutníku a já rozlouskl další ořech.
„Řeka, to už není to, co to bývalo,“ řekl jsem. „Bůhví, co to v tom říčním vzduchu teď je - snad taková vlhkost
nějaká - že z toho vždycky dostanu hexnšús.“
„Já taky,“ pravil George. „Bůhví, čím to je, ale já si už nemůžu dovolit spát někde poblíž řeky. Na jaře jsem byl
týden u Joea a každou noc jsem se tam probudil už v sedm a pak jsem už nezavřel oka.“
„No to byl jen takový návrh,“ poznamenal Harris. „Mně osobně řeka taky nesvědčí; vždycky mi rozbouří to
moje revma.“
„Co mně dělá dobře,“ řekl jsem, „to jsou hory. Co byste říkali pěší tůře po Skotsku?“
„Ve Skotsku pořád prší,“ namítl George. „Já tam byl předloni tři neděle a v jednom kuse jsem byl zlitý - v tom
původním slova smyslu teda.“
„Moc hezky je ve Švýcarsku,“ podotkl Harris.
„Jenže do Švýcarska bychom nemohli jet sami, to by ženské nesnesly,“ namítl jsem. „Víš, jak to dopadlo
posledně. My musíme někam, kde by to delikátně vypiplané dámy nebo děti prostě nevydržely; někam, kde jsou
mizerné hotely a kde se cestuje nepohodlně; někam, kde nám to dá zabrat, kde se nadřeme a třeba budem mít
vysoko do žlabu...“
„Tak to pozor!“ přerušil mě George. „Pozor, kamaráde! Nezapomeň, že jedu taky já!“
„Já už to mám!“ zvolal Harris. „Vyjedeme si na kolech!“
George se zatvářil nerozhodně.
„To musíš každou chvíli do kopce,“ pravil. „A máš proti sobě vítr.“
„Ale pak zas jedeš s kopce a máš vítr v zádech,“ řekl Harris.
„To jsem nikdy nepozoroval,“ namítl George.
„Na nic lepšího, než je výlet na kolech, nepřijdeš,“ trval na svém Harris.
Já jsem s ním chtě nechtě musel souhlasit.
„A řeknu ti, kam si vyjedeme,“ dodal Harris. „Do Černého lesa.“
„No dovol! Tam je to pořád jenom do kopce,“ namítl George.
„Pořád ne,“ odvětil Harris. „Tak ze dvou třetin. Ale existuje jedna vymoženost, na kterou jsi zapomněl.“
Opatrně se rozhlédl a snížil hlas až do šepotu.
„Na ty kopce jezdí nahoru takové malinké vláčky, takové miniaturky s ozubenými kolečky, které...“
Vtom se otevřely dveře a objevila se paní Harrisová. Ethelberta si prý už nasazuje klobouk a Muriel po marném
čekání zarecitovala Bláznivou svačinu bez nás.
„Zítra ve čtyři v klubu,“ zašeptal mi Harris, když se zvedal, a já, když jsme šli nahoru, jsem to přihrál
Georgeovi.";
            }
        }
        /// <summary>
        /// Text "Tábor svatých"
        /// </summary>
        public static string Text_TaborSvatych
        {
            get
            {
                return @"Starý profesor uvažoval všedně. Příliš mnoho četl, příliš mnoho přemýšlel, také příliš mnoho napsal na to, aby
se odvážil vyslovit dokonce i jen sám k sobě za okolností tak dokonale anormálních něco jiného, než banalitu
hodnou kompozice žáka ze sexty. Bylo krásně. Bylo horko, ale ne příliš, neboť čerstvý jarní vítr pomalu a bez
hluku přebíhal po kryté terase domu, jednoho z posledních směrem k vršku kopce, zavěšeného na úbočí skály jako
předsunutá stráž staré hnědé vesnice, která vévodila celé oblasti až k městu turistů dole, až k přepychové třídě na
břehu u vody, na niž se daly vytušit vrcholky zelených palem a bílé rezidence, až k samému moři, klidnému a
modrému, moři bohatých, z jehož povrchu byl náhle sloupnut lak opulence, který je obvykle pokrýval –
chromované jachty, svalnatí lyžaři, opálené dívky, těžká břicha rozvalená na palubě velkých obezřelých plachetnic
– nu a na tomto prázdném moři, neuvěřitelná rezavá flotila přibyvší z druhé strany zeměkoule, uvázlá padesát
metrů od břehu a kterou starý profesor od rána pozoroval. Příšerný puch latrín, který objevení této flotily
předcházel, jako hrom předchází bouři, se nyní úplně rozptýlil.
Oddaluje oko od dalekohledu na trojnožce, v němž se neuvěřitelná invaze hemžila tak blízko, že se zdálo, že
již přelezla svahy kopce a vrhla do domu, starý muž si protřel unavené víčko a poté zcela přirozeně obrátil pohled
ke dveřím svého domu. Byly to dveře z mohutného dubu, něco jako nesmrtelná hmota skloubená s veřejemi
pevnosti, v níž bylo vidět do tmavého dřeva vyryté rodové jméno starého pána a rok, který spatřil dostavění domu
předkem v přímé linii: 1673. Dveře spojovaly na téže úrovni terasu a hlavní místnost, která byla současně salónem,
knihovnou a pracovnou. Byly to jediné dveře v domě, neboť terasa vedla přímo do uličky malým schodištěm o
pěti schodech bez jakéhokoli plotu a po němž mohl každý kolemjdoucí po libosti vystoupit podle zvyku ve vesnici
panujícím, pokud dostal chuť pozdravit majitele. Každý den od úsvitu do noci zůstávaly tyto dveře otevřené a dnes
večer byly rovněž. Právě toho si starý muž všimnul poprvé. Tak pronesl těchto několik slov, jejichž úžasná
banálnost vyvolala na jeho rtech jakýsi okouzlený úsměv: „Sám se ptám, řekl si, zda v tomto případě, je nutno, aby
byly dveře otevřené nebo zavřené?…“
Poté se znovu ujal stráže, oko u dalekohledu využívaje toho, že zapadající slunce osvětlovalo naposledy před
příchodem noci neuvěřitelnou podívanou. Kolik jich tam bylo, na palubě všech těch uvázlých trosek? Pokud bylo
možno věřit děsivému počtu oznámenému v lakonických informačních sděleních, která dnes od rána následovala
jedno za druhým, možná byli napěchovaní po lidských vrstvách v podpalubí a na palubách, v chumlech tyčících se
až k můstkům a komínům, spodní mrtvé vrstvy nesoucí ty, které ještě žily, jako ony kolony mravenců na pochodu,
jejichž viditelná část je hemžením života a základna jakousi mravenčí cestou dlážděnou milióny mrtvol?
Starý profesor, jmenoval se Calgués – zaměřil dalekohled na jedno z plavidel nejlépe osvětlených sluncem.
Poté jej rozvážně zreguloval až k nejdokonalejší ostrosti jako badatel u svého mikroskopu, když v živné půdě objeví
kolonii mikrobů, jejíž existenci předvídal. Toto plavidlo byl parník více než šedesátník, jehož pět kolmých komínů
trubkové formy hlásalo velmi vysoký věk. Čtyři z nich byly v různé výši uříznuty časem, rzí, absencí údržby, ranami
osudu, jedním slovem bídou.
Uvázlá před pláží, ležela loď ve sklonu nějakých deseti stupňů. Jako na všech ostatních plavidlech této
strašidelné flotily, zatímco se stmívalo, nebylo vidět jediného světla ani nejmenšího záblesku.
Navigační světla, kotle, dynama, všechno muselo rázem zhasnout při záměrném ztroskotání, nebo možná pro
nedostatek topiva spočítaného co nejpřesněji na jednu a jedinou cestu, nebo také protože nikdo na palubě už
nepovažoval za nutné o cokoli se starat, exodus byv skočen u bran nového ráje.
Starý pán Calgués to vše pečlivě zaznamenával, detail po detailu, aniž by u sebe pozoroval sebemenší projev
emoce. Prostě, před avantgardou protisvěta, který se konečně odhodlal přijít osobně zaklepat na dveře hojnosti,
pociťoval nesmírný zájem.
Okem připoutaným k dalekohledu uviděl nejprve paže. Vypočítal, že kruh jím vykrojený na palubě lodi mohl
mít průměr kolem deseti metrů. Poté počítal dál, klidně, ale bylo to stejně obtížné, jako počítat stromy v lese.
Protože všechny tyto paže byly vztyčeny. Klátily se společně, nakláněly se k blízkému břehu, hubené černé a hnědé
větve, oživené větrem naděje. Paže byly nahé. Vynořovaly se z kusů bílého plátna, které měly být tunikami, tógami,
sarimi poutníků: byly to vychrtlé paže Gándhího. Dospěv k číslu dvou set, profesor přestal počítat, neboť dosáhl
hranic kruhu. Poté se pustil do rychlého výpočtu. Vezme-li se v úvahu délka a šířka paluby lodi, mohlo se stanovit,
že stejný obvod byl vedle sebe položen více než třicetkrát a že mezi každým z těchto třiceti bodově se dotýkajících
se kruhů se uložily dva prostory ve formě trojúhelníku stýkající se vrcholem a jejichž plocha se rovnala přibližně
třetině obvodu, tedy: 30 + 10 = 40 obvodů × 200 paží = 8 000 paží. Čtyři tisíce lidí. Na jediné palubě lodi!
Připustíme-li existenci vrstev překrývajících jedna druhou, nebo přinejmenším pravděpodobně tutéž hustotu na
každé palubě, mezipalubě a podpalubí, bylo třeba násobit nejméně osmi číslo už tak překvapující. Celkově: třicet
tisíc lidí na jediném plavidle! Aniž bychom počítali mrtvé, kteří plavali kolem boků lodi ve svých bílých cárech
táhnoucích se po povrchu vody, které živí hned zrána hodili přes palubu. Pro toto podivné gesto, které se nezdálo
být inspirováno hygienou – jinak, proč vyčkávat až konec cesty? - profesor nalezl, jak si myslel, jediné možné
vysvětlení. Calgués věřil v Boha. Věřil ve vše, v život věčný, ve vykoupení, v milosrdenství Boží, víru, naději. Věřil
také, a velmi pevně, že mrtvoly vyhozené na pobřeží Francie konečně dosáhly, i ony, ráje, že v něm dokonce
bloudily bez zábran a navždy, takto se těšící větší přízni než živí, kteří tím, že svoje mrtvé hodili do vody, jim naráz
dopřáli vysvobození, blaženost a věčnost. Toto gesto se nazývalo láska a profesor to chápal.
Nastala noc, aniž by den naposledy neosvítil rudými záblesky uvázlou flotilu. Bylo v ní více než sto plavidel,
všechna zrezivělá, nepoužitelná a všechna dosvědčující zázrak, který je vedl a chránil už z druhé strany světa, s
výjimkou jednoho, ztraceného ztroskotáním v blízkosti Ceylonu. Jedno za druhým, téměř způsobně seřazena podle
toho jak připlula, byla zapíchnuta ve skalách nebo v písku, s přídí obrácenou ke břehu a pozvednutou v posledním
vzepětí. Kolem dokola plavaly tisíce mrtvých v bílém, které poslední vlny dne začínaly pozvolna přinášet na
pevninu, pokládajíce je na břeh a poté opadávaly, aby odešly pro další. Sto plavidel! Starý profesor v sobě cítil zrod
jakéhosi záchvěvu pokory smíšené s vytržením, které člověk občas pocítí, když svoji mysl velmi silně zaměstnává
pojmy nekonečna nebo věčnosti. Navečer této neděle Velikonoční obléhalo osm set tisíc živých a tisíce mrtvých
mírumilovně hranici Západu. Nazítří bude po všem. Od břehu stoupaly až do kopců, k vesnici, až k terase domu
na poslech velmi příjemné zpěvy, ale navzdory jejich mírnosti, plné nesmírné síly, jako melopej
(říkanka)prozpěvovaná sborem osmi set tisíců hlasů. Křižáci kdysi v předvečer závěrečného útoku obcházeli
zpívajíce Jeruzalém. Po sedmém zaznění trub se bez boje zhroutily hradby Jericha. A když by melopej uprázdnila
místo tichu, budou snad vyvolené národy zase podrobeny nepřízni boží? Bylo rovněž slyšet burácení stovek
nákladních aut: od rána také armáda zaujímala pozice na břehu Středozemního moře. V nastavší noci se terasa
otevírala jen k nebi a ke hvězdám.
V domě bylo chladno, ale vcházeje, rozhodl se profesor nechat dveře otevřené. Copak dveře, byť zázrak
třistaleté řemeslné práce v západní nanejvýš úctyhodné dubovině, mohou ochránit svět, který se již přežil? Elektřina
nefungovala. Nepochybně také i techničtí pracovníci elektráren v pobřežní oblasti uprchli na sever, následujíce
veškerý zděšený lid, který se obracel zády a vytrácel v tichosti, aby neviděl, aby nic neviděl a tím také nic nechápal,
nebo přesněji nic chápat nechtěl.
Profesor zažehl petrolejové lampy, které měl pro případ poruchy vždy připravené a vhodil zápalku do krbu,
v němž pečlivě nachystaný oheň ihned vzplál, zahučel, zapraskal, šíře teplo a světlo. Poté zapnul tranzistor. Pop
muzika, rock, zpěvačky, plytcí žvanilové, negerští saxofonisté, guruové, sebevědomé hvězdy, moderátoři, poradci
ohledně zdraví, srdečních záležitostí a sexu, ti všichni opustili éter, náhle považováni za nevkusné, jakoby si
ohrožený Západ obzvláště hleděl svého posledního zvukového obrazu. Bylo slyšet Mozarta, stejný program na
všech stanicích: „Malá noční hudba“, docela prostě.
Starý profesor přátelsky pomyslel na programového pracovníka v pařížském studiu. Aniž by věděl či viděl,
tento muž pochopil. Na melopej osmi set tisíc hlasů, kterou zatím nemohl slyšet, našel instinktivně nejlepší
odpověď. Co na světě bylo západnější, civilizovanější, dokonalejší než Mozart? Je nemožné pobrukovat Mozarta
osmi sty tisíci hlasy. Mozart nikdy neskládal pro podněcování davů, ale aby bylo dojato srdce každého v jeho
osobnosti. Západ ve svojí jediné pravdě… Hlas zpravodaje vytrhl profesora z úvah:
„Vláda shromážděná kolem prezidenta republiky zasedala celý den v Elysejském paláci.";
            }
        }
        /// <summary>
        /// Text "Camp Of Saints"
        /// </summary>
        public static string Text_CampOfSaints
        {
            get
            {
                return @"The old professor had a rather simple thought. Given the wholly abnormal conditions, he had read, and reasoned, and even written
too much—versed as he was in the workings of the mind—to dare propose anything, even to himself, but the most banal of reflections,
worthy of a schoolboy’s theme. It was a lovely day, warm but not hot, with a cool spring breeze rolling gently and noiselessly over the
covered terrace outside the house. His was one of the last houses up toward the crest of the hill, perched on the rocky slope like an
outpost guarding the old brown-hued village that stood out above the landscape, towering over it all, as far as the tourist resort down
below; as far as the sumptuous boulevard along the water, with its green palms, tips barely visible, and its fine white homes; as far as the
sea itself, calm and blue, the rich man’s sea, now suddenly stripped of all the opulent veneer that usually overspread its surface—the
chrome-covered yachts, the muscle-bulging skiers, the gold-skinned girls, the fat bellies lining the decks of sailboats, large but
discreet—and now, stretching over that empty sea, aground some fifty yards out, the incredible fleet from the other side of the globe, the
rusty, creaking fleet that the old professor had been eyeing since morning. The stench had faded away at last, the terrible stench of
latrines, that had heralded the fleet’s arrival, like thunder before a storm. The old man took his eye from the spyglass, moved back from
the tripod. The amazing invasion had loomed up so close that it already seemed to be swarming over the hill and into his house. He
rubbed his weary eye, looked toward the door. It was a door of solid oak, like some deathless mass, jointed with fortress hinges. The
ancestral name was carved in somber wood, and the year that one of the old man’s forebears, in uninterrupted line, had completed the
house: 1673. The door opened out on the terrace from the large main room that served as his library, parlor, and study, all in one. There
was no other door in the house. The terrace, in fact, ran right to the road, down five little steps, with nothing like a gate to close them off,
open to any and every passerby who felt like walking up and saying hello, the way they did so often in the village. Each day, from dawn
to dusk, that door stood open. And on this particular evening, as the sun was beginning to sink down to its daily demise, it was open as
well—a fact that seemed to strike the old man for the very first time. It was then that he had this fleeting thought, whose utter banality
brought a kind of rapturous smile to his lips: “I wonder,” he said to himself, “if, under the circumstances, the proverb is right, and if a
door really has to be open or shut ...”
Then he took up his watch again, eye to glass, to make the most of the sun’s last, low-skimming rays, as they lit the unlikely sight
one more time before dark. How many of them were there, out on those grounded wrecks? If the figures could be believed—the
horrendous figures that each terse news bulletin had announced through the day, one after another—then the decks and holds must be
piled high with layer on layer of human bodies, clustered in heaps around smokestacks and gangways, with the dead underneath
supporting the living, like one of those columns of ants on the march, teeming with life on top, exposed to view, and below, a kind of
ant-paved path, with millions of trampled cadavers. The old professor—Calgues by name—aimed his glass at one of the ships still lit by
the sun, then patiently focused the lens until the image was as sharp as he could make it, like a scientist over his microscope, peering in
to find his culture swarming with the microbes that he knew all the time must be there. The ship was a steamer, a good sixty years old.
Her five stacks, straight up, like pipes, showed how very old she was. Four of them were lopped off at different levels, by time, by rust,
by lack of care, by chance—in short, by gradual decay. She had run aground just off the beach, and lay there, listing at some ten degrees.
Like all the ships in this phantom fleet, there wasn’t a light to be seen on her once it was dark, not even a glimmer. Everything must
have gone dead—boilers, generators, everything, all at once—as she ran to meet her self-imposed disaster. Perhaps there had been just
fuel enough for this one and only voyage. Or perhaps there was no one on board anymore who felt the need to take care of such things—
or of anything else—now that the exodus had finally led to the gates of the newfound paradise. Old Monsieur Calguès took careful note
of all he saw, of each and every detail, unaware of the slightest emotion within him. Except, that is, for his interest; a prodigious interest
in this vanguard of an antiworld bent on coming in the flesh to knock, at long last, at the gates of abundance. He pressed his eye to the
glass, and the first things he saw were arms. As best he could tell, his range of vision described a circle on deck ten yards or so in
diameter. Then he started to count. Calm and unhurried. But it was like trying to count all the trees in the forest, those arms raised high
in the air, waving and shaking together, all outstretched toward the nearby shore. Scraggy branches, brown and black, quickened by a
breath of hope. All bare, those fleshless Gandhi-arms. And they rose up out of scraps of cloth, white cloth that must have been tunics
once, and togas, and pilgrims’ saris. The professor reached two hundred, then stopped. He had counted as far as he could within the
bounds of the circle. Then he did some rapid calculation. Given the length and breadth of the deck, it was likely that more than thirty
such circles could be laid out side by side, and that between every pair of tangent circumferences there would be two spaces, more or
less triangular in shape, opposite one another, vertex to vertex, each with an area roughly equal to one-third of a circle, which would
give a total of 30 + 10 = 40 circles, 40 x 200 arms = 8,000 arms. Or four thousand bodies! On this one deck alone! Now, assuming that
they might be several layers thick, or at least no less thick on each of the decks—and between decks and belowdecks too—then the
figure, astounding enough as it was, would have to be multiplied by eight. Or thirty thousand creatures on a single ship! Not to mention
the dead, floating here and there around the hull, trailing their white rags over the water, corpses that the living had been throwing
overboard since morning. A curious act, all in all, and one not inspired by reasons of hygiene, to be sure. Otherwise, why wait for the
end of the voyage? But Monsieur Calgues felt certain he had hit on the one explanation. He believed in God. He believed in all the rest:
eternal life, redemption, heavenly mercy, hope and faith. He believed as well, with firm conviction, that the corpses thrown out on the
shores of France had reached their paradise too to waft their way through it, unconstrained, forevermore. Even more blessed than the
living themselves, who, throwing them into the sea, had offered their dead, then and there, the gift of salvation, joy, and all eternity.
Such an act was called love. At least that was how the old professor understood it.
And so night settled in, but not until daylight had glimmered its last red rays once more on the grounded fleet. There were better
than a hundred ships in all, each one caked with rust, unfit for the sea, and each one proof of the miracle that had somehow guided them,
safe and sound, from the other side of the earth. All but one, that is, wrecked off the coast of Ceylon. They had lined up in almost
mannerly fashion, one after the other, stuck in the sand or in among the rocks, bows upraised in one final yearning thrust toward shore.
And all around, thousands of floating, white-clad corpses, that daylight’s last waves were beginning to wash aground, laying them gently
down on the beach, then rolling back to sea to look for more. A hundred ships! The old professor felt a shudder well up within him, that
quiver of exaltation and humility combined, the feeling we sometimes get when we turn our minds, hard as we can, to notions of the
infinite and the eternal. On this Easter Sunday evening, eight hundred thousand living beings, and thousands of dead ones, were making
their peaceful assault on the Western World. Tomorrow it would all be over. And now, rising up from the coast to the hills, to the
village, to the house and its terrace, a gentle chanting, yet so very strong for all its gentleness, like a kind of singsong, droned by a
chorus of eight hundred thousand voices. Long, long ago, the Crusaders had sung as they circled Jerusalem, on the eve of their last
attack. And Jericho’s walls had crumbled without a fight when the trumpets sounded for the seventh time. Perhaps when all was silent,
when the chanting was finally stilled, the chosen people too would feel the force of divine displeasure. ... There were other sounds as
well. The roar of hundreds of trucks. Since morning, the army had taken up positions on the Mediterranean beaches. But there in the
darkness there was nothing beyond the terrace but sky and stars.
It was cool in the house when the professor went inside, but he left the door open all the same. Can a door protect a world that has
lived too long? Even a marvel of workmanship, three hundred years old, and one carved out of such utterly respectable Western oak? ...
There was no electricity. Obviously, the technicians from the power plants along the coast had fled north too, with all the others, the
petrified mob, turning tail and running off without a word, so as not to have to look, not see a thing, which meant they wouldn’t have to
understand, or even try. The professor lit the oil lamps that he always kept on hand in case the lights went out. He threw one of the
matches into the fireplace. The kindling, carefully arranged, flashed up with a roar, crackled, and spread its light and warmth over the
room. Then he turned on his transistor, tuned all day long to the national chain. Gone now the pop and the jazz, the crooning ladies and
the vapid babblers, the black saxophonists, the gurus, the smug stars of stage and screen, the experts on health and love and sex. All
gone from the airwaves, all suddenly judged indecent, as if the threatened West were concerned with the last acoustic image it presented
of itself. Nothing but Mozart, the same on every station. Eine kleine Nachtmusik, no less. And the old professor had a kindly thought for
the program director, there in his studio in Paris. He couldn’t possibly see or know, and yet he had understood. For those eight hundred
thousand singsong voices that he couldn’t even hear, he had found, instinctively, the most fitting reply. What was there in the world
more Western than Mozart, more civilized, more perfect? No eight hundred thousand voices could drone their chant to Mozart’s notes.
Mozart had never written to stir the masses, but to touch the heart of each single human being, in his private self. What a lovely symbol,
really! The Western World summed up in its ultimate truth ... An announcer’s voice roused the old professor from his musings:
“The President of the Republic has been meeting all day at the Élysée Palace with government leaders. Also present, in view of the
gravity of the situation, are the chiefs of staff of the three branches of the armed forces, as well as the heads of the local and state police,
the prefects of the departments of Var and Alpes-Maritimes, and, in a strictly advisory capacity, His Eminence the Cardinal Archbishop
of Paris, the papal nuncio, and most of the Western ambassadors currently stationed in the capital. At present the meeting is still in
progress. A government spokesman, however, has just announced that this evening, at about midnight, the President of the Republic will
go on the air with an address of utmost importance to the nation. According to reports reaching us from the south, all still seems quiet on
board the ships of the refugee fleet. A communiqué from army headquarters confirms that two divisions have been deployed along the
coast in the face ... in the face of ...” (The announcer hesitated. And who could blame him? Just what should one call that numberless,
miserable mass? The enemy? The horde? The invasion? The Third World on the march?) “... in the face of this unprecedented incursion
(There! Not too bad at all!) “... and that three divisions of reinforcements are heading south at this moment, despite considerable
difficulty of movement. In another communiqué, issued not more than five minutes ago, army chief of staff Colonel Dragasès has
reported that troops under his command have begun setting fire to some twenty immense wooden piles along the shore, in order to ...
(Another hesitation. The announcer seemed to gasp. The old professor even thought he heard him mutter “My God!”) “... in order to
burn the thousands of dead bodies thrown overboard from all the ships ...”
And that was all. A moment later, with hardly a break, Mozart was back, replacing those three divisions hurtling southward, and the
score of funeral pyres that must have begun to crackle by now in the crisp air down by the coast. The West doesn’t like to burn its dead.
It tucks away its cremation urns, hides them out in the hinterlands of its cemeteries. The Seine, the Rhine, the Loire, the Rhône, the
Thames are no Ganges or Indus. Not even the Guadalquivir and the Tiber. Their shores never stank with the stench of roasting corpses.
Yes, they have flowed with blood, their waters have run red, and many a peasant has crossed himself as he used his pitchfork to push
aside the human carcasses floating downstream. But in Western times, on their bridges and banks, people danced and drank their wine
and beer, men tickled the fresh, young laughing lasses, and everyone laughed at the wretch on the rack, laughed in his face, and the
wretch on the gallows, tongue dangling, and the wretch on the block, neck severed—because, indeed, the Western World, staid as it was,
knew how to laugh as well as cry—and then, as their belfreys called them to prayer, they would all go partake of their fleshly god,
secure in the knowledge that their dead were there, protecting them, safe as could be, laid out in rows beneath their timeless slabs and
crosses, in graveyards nestled against the hills, since burning, after all, was only for devilish fiends, or wizards, or poor souls with the
plague. ... The professor stepped out on the terrace. Down below, the shoreline was lit with a score of reddish glows, ringed round with
billows of smoke. He opened his binoculars and trained them on the highest of the piles, flaming neatly along like a wooden tower,
loaded with corpses from bottom to top. The soldiers had stacked it with care, first a layer of wood, then a layer of flesh, and so on all
the way up. At least some trace of respect for death seemed to show in its tidy construction. Then all at once, down it crashed, still
burning, nothing now but a loathsome mass, like a heap of smoking rubble along the public way. And no one troubled to build the nice
neat tower again. Bulldozers rolled up, driven by men in diving suits, then other machines fitted with great jointed claws and shovels,
pushing the bodies together into soft, slimy mounds, scooping a load in the air and pouring it onto the fire, as arms and legs and heads,
and even whole cadavers overflowed around them and fell to the ground. It was then that the professor saw the first soldier turn and run,
calling to mind yet another cliché, arms and legs flapping like a puppet on a string, in perfect pantomime of unbridled panic. The young
man had dropped the corpse he was dragging. He had wildly thrown down his helmet and mask, ripped off his safety gloves. Then,
hands clutched to temples, he dashed off, zigzag, like a terrified jackrabbit, into the ring of darkness beyond the burning pile. Five
minutes more, and ten other soldiers had done the same. The professor closed his binoculars. He understood. That scorn of a people for
other races, the knowledge that one’s own is best, the triumphant joy at feeling oneself to be part of humanity’s finest—none of that had
ever filled these youngsters’ addled brains, or at least so little that the monstrous cancer implanted in the Western conscience had
quashed it in no time at all. In their case it wasn’t a matter of tender heart, but a morbid, contagious excess of sentiment, most interesting
to find in the flesh and observe, at last, in action. The real men of heart would be toiling that night, and nobody else. Just a moment
before, as the nice young man was running away, old Calguès had turned his glasses briefly on a figure that looked like some uniformed
giant, standing at the foot of the burning pile, legs spread, and hurling up each corpse passed over to him, one by one, with a powerful,
rhythmic fling, like a stoker of yesteryear deep belowdecks, feeding his boiler with shovelfuls of coal. Perhaps he too was pained at the
sight, but if so, his pain didn’t leave much room for pity. In fact, he probably didn’t think of it at all, convinced that now, finally, the
human race no longer formed one great fraternal whole—as the popes, philosophers, intellects, politicos, and priests of the West had
been claiming for much too long. Unless, that is, the old professor, watching “the stoker” and his calm resolve—the one he called “the
stoker” was really Colonel Dragases, the chief of staff, up front to set his men an example—was simply ascribing to him his own ideas.
... That night, love too was not of one mind. Man never has really loved humanity all of a piece—all its races, its peoples, its religions—
but only those creatures he feels are his kin, a part of his clan, no matter how vast. As far as the rest are concerned, he forces himself,
and lets the world force him. And then, when he does, when the damage is done, he himself falls apart. In this curious war taking shape,
those who loved themselves best were the ones who would triumph. How many would they be, next morning, still joyously standing
their ground on the beach, as the hideous army slipped down by the thousands, down into the water, for the onslaught by the living, in
the wake of their dead? Joyously! That was what mattered the most. A moment before, as he watched “the stoker,” the professor had
thought he could see him move his lips, wide open, as if he were singing. Yes, by God, singing! If even just the two of them could stand
there and sing, perhaps they could wake up the rest from their deathly sleep. ... But no other sound came rising from the shore, no sound
but the soft, foreboding chant welling up out of eight hundred thousand throats.
“Pretty cool, man, huh!” exclaimed a voice in the shadows.";
            }
        }

        /// <summary>
        /// Různé varianty tečky za větou.
        /// Nejčastější tečka za větou je tečka (7x častější než ostatní), ale může tam být trojtečka nebo otazník nebo vykřičník
        /// </summary>
        protected static string[] SentenceDots 
        {
            get
            {
                if (_SentenceDots is null)
                    // Nejčastější tečka za větou je tečka (7x častější než ostatní), ale může tam být trojtečka nebo otazník nebo vykřičník
                    _SentenceDots = ".×.×.×.×.×.×.×...×?×!".Split('×');
                return _SentenceDots;
            }
        }
        private static string[] _SentenceDots = null;
        #endregion
        #region Generátor náhodné pravděpodobnosti, čísla, barvy...
        /// <summary>
        /// Vrátí true s danou pravděpodobností:
        /// 0 = nikdy není true; 100 = vždy je true; mezi tím: 10 = vrátí true v 10 případech ze 100 volání
        /// </summary>
        /// <param name="probability"></param>
        public static bool IsTrue(int probability = 50)
        {
            probability = _ToRange(probability, 0, 100);
            int value = Rand.Next(0, 100);                 // číslo 0-99
            return (value < probability);                  // Pokud je probability = 0, pak value nikdy není < 0, vždy vrátím false. Pokud probability = 100, pak value je vždy < 100. ....
        }
        /// <summary>
        /// Vrátí náhodnou barvu, volitelně v daném rozmezí 0 až 256, volitelně s náhodnou hodntou Alpha
        /// </summary>
        /// <param name="low"></param>
        /// <param name="high"></param>
        /// <param name="alpha">Hodnota Alpha</param>
        /// <param name="isRandomAlpha">Použít náhodný Alpha kanál v rozmezí 16 - 240? false = ne, Alpha bude 255</param>
        /// <returns></returns>
        public static Color GetColor(int low = 0, int high = 256, int? alpha = null, bool isRandomAlpha = false)
        {
            low = _ToRange(low, 0, 255);
            high = _ToRange(high, low, 256);
            var rand = Rand;
            int a = (alpha.HasValue ? _ToRange(alpha.Value, 0, 256) : (isRandomAlpha ? rand.Next(16, 240) : 255));
            int r = rand.Next(low, high);
            int g = rand.Next(low, high);
            int b = rand.Next(low, high);
            return Color.FromArgb(a, r, g, b);
        }
        /// <summary>
        /// Vrátí náhodný prvek z pole
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public static T GetItem<T>(params T[] items)
        {
            return GetItem((IList<T>)items);
        }
        /// <summary>
        /// Vrátí náhodný prvek z pole
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public static T GetItem<T>(IList<T> items)
        {
            if (items == null) return default(T);
            int count = items.Count;
            if (count == 0) return default(T);
            return items[Rand.Next(count)];
        }
        /// <summary>
        /// Vrátí daný počet náhodně vybraných prvků z pole
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="count"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static T[] GetItems<T>(int count, params T[] items)
        {
            return GetItems(count, (IList<T>)items);
        }
        /// <summary>
        /// Vrátí náhodný prvek z pole
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public static T[] GetItems<T>(int count, IList<T> items)
        {
            if (items == null) return null;
            int itemsCount = items.Count;
            List<T> result = new List<T>();
            if (count > 0)
            {   // Něco chtěli?
                if (count < itemsCount)
                {   // Chtěli méně prvků, než nám dali: vybereme si náhodně daný počet:
                    List<T> values = items.ToList();
                    for (int i = 0; i < count; i++)
                    {
                        if (values.Count == 0) break;                // Pojistka
                        int index = Rand.Next(values.Count);         // Náhodná pozice prvku ve zmenšujícím se Listu hodnot
                        result.Add(values[index]);                   // Do výsledku přidám prvek na náhodné pozici
                        values.RemoveAt(index);                      // A tentýž prvek z Listu odeberu, abych ho do výsledku nedával duplicitně...
                    }
                }
                else
                {   // Chtěli by víc prvků, než nám dali: vrátíme jen to co máme:
                    result.AddRange(items);
                }
            }
            return result.ToArray();
        }
        /// <summary>
        /// Vrátí danou hodnotu zarovnanou do min - max, obě meze jsou včetně
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private static int _ToRange(int value, int min, int max)
        {
            return (value < min ? min : (value > max ? max : value));
        }
        #endregion
        #region Náhoda
        /// <summary>
        /// Random generátor
        /// </summary>
        public static System.Random Rand { get { if (_Rand is null) _Rand = new System.Random(); return _Rand; } }
        private static System.Random _Rand;
        #endregion
    }
}
