using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        #region MenuItems, DataTable, Icons, Images, Myco
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
        /// <summary>
        /// Vrátí datovou tabulku s danou strukturou<br/>
        /// Struktura se deklaruje jedním stringem ve formě: <c>"colname1:type; colname2:type; colname3:type..."</c> - kde <c>type</c> určuje jednak konkrétní datový typ (string, int, atd), a i vygenerovaný obsah sloupce.
        /// <list type="bullet">
        /// <item>id: typ int, generuje se postupné číslo od 0</item>
        /// <item>int: typ int, generuje se náhodné číslo v plném rozsahu</item>
        /// <item>number: typ int, generuje se náhodné číslo v rozsahu 100 - 1000000</item>
        /// <item>label: typ string, prázdné (null)</item>
        /// <item>char: typ string, jeden znak 32 + 128</item>
        /// <item>word: typ string, jedno náhodné slovo</item>
        /// <item>sentence: typ string, věta o 2 - 5 slovech</item>
        /// <item>text: typ string, věta o 2 - 5 slovech</item>
        /// <item>idtext: typ string, pořadové číslo od 1, tečka, věta o 2 - 5 slovech</item>
        /// <item>note: typ string, delší text se 3 - 7 větami</item>
        /// <item>string: typ string, několik slov bez tečky</item>
        /// <item>varchar: typ string, několik slov bez tečky</item>
        /// <item>nvarchar: typ string, několik slov bez tečky</item>
        /// <item>decimal: typ decimal, náhodné číslo 0 až 1000000d</item>
        /// <item>numeric: typ decimal, náhodné číslo 0 až 1000000d</item>
        /// <item>date: typ DateTime, náhodné datum v rozmezí před půl rokem do dneška</item>
        /// <item>datetime: typ DateTime, náhodné datum v rozmezí před půl rokem do dneška</item>
        /// <item>imagename: typ string, jméno ikony typu SVG</item>
        /// <item>imagenamesvg: typ string, jméno ikony typu SVG</item>
        /// <item>imagenamepng: typ string, jméno ikony typu PNG, velká</item>
        /// <item>imagenamepngsmall: typ string, jméno ikony typu PNG, malá</item>
        /// <item>imagenamepngfull: typ string, jméno ikony typu PNG, velká</item>
        /// <item>thumb: typ byte[], obsahuje data fotografie malé</item>
        /// <item>bytes: typ byte[], obsahuje data fotografie malé</item>
        /// <item>photo: typ byte[], obsahuje data fotografie velké</item>
        /// <item>image: typ byte[], obsahuje data fotografie velké</item>
        /// </list>
        /// </summary>
        /// <param name="minCount">Počet řádků nejnižší</param>
        /// <param name="maxCount">Počet řádků nejvyšší</param>
        /// <param name="structure">Struktura: sloupce oddělené středníkem; uvnitř sloupce je "název:typ; ". Typ je: "id, int, number, decimal, text, note, date, datetime, imagename, iconname a další"</param>
        /// <returns></returns>
        public static DataTable GetDataTable(int minCount, int maxCount, string structure)
        {
            if (String.IsNullOrEmpty(structure)) return null;
            int count = Rand.Next(minCount, maxCount);
            return GetDataTable(count, structure);
        }
        /// <summary>
        /// Vrátí datovou tabulku s danou strukturou.<br/>
        /// Struktura se deklaruje jedním stringem ve formě: <c>"colname1:type; colname2:type; colname3:type..."</c> - kde <c>type</c> určuje jednak konkrétní datový typ (string, int, atd), a i vygenerovaný obsah sloupce.
        /// <list type="bullet">
        /// <item>id: typ int, generuje se postupné číslo od 0</item>
        /// <item>int: typ int, generuje se náhodné číslo v plném rozsahu</item>
        /// <item>number: typ int, generuje se náhodné číslo v rozsahu 100 - 1000000</item>
        /// <item>label: typ string, prázdné (null)</item>
        /// <item>char: typ string, jeden znak 32 + 128</item>
        /// <item>word: typ string, jedno náhodné slovo</item>
        /// <item>sentence: typ string, věta o 2 - 5 slovech</item>
        /// <item>text: typ string, věta o 2 - 5 slovech</item>
        /// <item>idtext: typ string, pořadové číslo od 1, tečka, věta o 2 - 5 slovech</item>
        /// <item>note: typ string, delší text se 3 - 7 větami</item>
        /// <item>string: typ string, několik slov bez tečky</item>
        /// <item>varchar: typ string, několik slov bez tečky</item>
        /// <item>nvarchar: typ string, několik slov bez tečky</item>
        /// <item>decimal: typ decimal, náhodné číslo 0 až 1000000d</item>
        /// <item>numeric: typ decimal, náhodné číslo 0 až 1000000d</item>
        /// <item>date: typ DateTime, náhodné datum v rozmezí před půl rokem do dneška</item>
        /// <item>datetime: typ DateTime, náhodné datum v rozmezí před půl rokem do dneška</item>
        /// <item>imagename: typ string, jméno ikony typu SVG</item>
        /// <item>imagenamesvg: typ string, jméno ikony typu SVG</item>
        /// <item>imagenamepng: typ string, jméno ikony typu PNG, velká</item>
        /// <item>imagenamepngsmall: typ string, jméno ikony typu PNG, malá</item>
        /// <item>imagenamepngfull: typ string, jméno ikony typu PNG, velká</item>
        /// <item>thumb: typ byte[], obsahuje data fotografie malé</item>
        /// <item>bytes: typ byte[], obsahuje data fotografie malé</item>
        /// <item>photo: typ byte[], obsahuje data fotografie velké</item>
        /// <item>image: typ byte[], obsahuje data fotografie velké</item>
        /// </list>
        /// </summary>
        /// <param name="count">Počet řádků</param>
        /// <param name="structure">Struktura: sloupce oddělené středníkem; uvnitř sloupce je "název:typ; ". Typ je: "id, int, number, decimal, text, note, date, datetime, imagename, iconname a další"</param>
        /// <returns></returns>
        public static DataTable GetDataTable(int count, string structure)
        {
            if (String.IsNullOrEmpty(structure)) return null;
            var columns = ColumnInfo.Parse(structure);
            var dataTable = ColumnInfo.CreateTable(columns);

            for (int i = 0; i < count; i++)
                dataTable.Rows.Add(ColumnInfo.CreateRowItems(i, columns));

            return dataTable;
        }
        #region class ColumnInfo : jeden sloupec random tabulky: typ, druh hodnot
        private class ColumnInfo
        {
            private ColumnInfo() { }
            internal static ColumnInfo[] Parse(string structure)
            {
                List<ColumnInfo> result = new List<ColumnInfo>();
                var columns = structure.Split(';');
                foreach (var column in columns)
                {
                    if (!String.IsNullOrEmpty(column))
                    {
                        var parts = column.Split(':');
                        int count = parts.Length;
                        string name = parts[0];
                        if (!String.IsNullOrEmpty(name))
                        {
                            name = name.Trim();
                            string typeName = (count > 1 ? parts[1] : null);

                            Type type = typeof(string);
                            if (!String.IsNullOrEmpty(typeName))
                            {
                                typeName = typeName.Trim().ToLower();
                                switch (typeName)
                                {
                                    case "id":
                                    case "int":
                                        type = typeof(int);
                                        break;
                                    case "number":
                                        type = typeof(int);
                                        break;
                                    case "label":
                                    case "char":
                                    case "word":
                                    case "sentence":
                                    case "idtext":
                                    case "text":
                                    case "note":
                                    case "string":
                                    case "varchar":
                                    case "nvarchar":
                                        type = typeof(string);
                                        break;
                                    case "decimal":
                                    case "numeric":
                                        type = typeof(decimal);
                                        break;
                                    case "date":
                                    case "datetime":
                                        type = typeof(DateTime);
                                        break;
                                    case "imagename":
                                    case "imagenamesvg":
                                    case "imagenamepng":
                                    case "imagenamepngsmall":
                                    case "imagenamepngfull":
                                        type = typeof(string);
                                        break;
                                    case "thumb":
                                    case "image":
                                    case "photo":
                                    case "bytes":
                                        type = typeof(byte[]);
                                        break;
                                    default:
                                        type = typeof(string);
                                        typeName = "text";
                                        break;
                                }
                            }
                            else
                            {
                                type = typeof(string);
                                typeName = "text";
                            }

                            ColumnInfo col = new ColumnInfo() { ColumnName = name, ColumnTypeName = typeName, ColumnType = type};
                            result.Add(col);
                        }
                    }
                }

                return result.ToArray();
            }
            internal static DataTable CreateTable(ColumnInfo[] columns)
            {
                DataTable table = new DataTable();
                foreach (ColumnInfo col in columns) 
                    table.Columns.Add(col.ColumnName, col.ColumnType);
                return table;
            }
            internal static object[] CreateRowItems(int id, ColumnInfo[] columns)
            {
                object[] result = new object[columns.Length];
                for (int c = 0; c < columns.Length; c++)
                {
                    var col = columns[c];
                    string typeName = col.ColumnTypeName;
                    switch (typeName)
                    {
                        case "id":
                            result[c] = id;
                            break;
                        case "int":
                            result[c] = Rand.Next();
                            break;
                        case "number":
                            result[c] = Rand.Next(100, 1000000);
                            break;
                        case "label":
                            result[c] = null;
                            break;
                        case "char":
                            result[c] = ((char)(Rand.Next(32, 128))).ToString();
                            break;
                        case "word":
                            result[c] = GetWord(true);
                            break;
                        case "sentence":
                        case "text":
                            result[c] = GetSentence(2, 6, true);
                            break;
                        case "idtext":
                            result[c] = (id + 1).ToString() + ". " + GetSentence(2, 6, true);
                            break;
                        case "note":
                            result[c] = GetSentences(4, 9, 3, 8);
                            break;
                        case "string":
                        case "varchar":
                        case "nvarchar":
                            result[c] = GetSentence(1, 4, false);
                            break;
                        case "decimal":
                        case "numeric":
                            result[c] = (decimal)(Math.Round(1000000d * Rand.NextDouble(), 2));
                            break;
                        case "date":
                        case "datetime":
                            result[c] = DateTime.Now.AddMinutes(-259200d * Rand.NextDouble());           // 259200 minut = 180 dní, záporné = od teď do minulosti o půl roku.
                            break;

                        case "imagename":
                        case "imagenamesvg":
                            result[c] = GetIconName(ImageResourceType.Svg);
                            break;
                        case "imagenamepng":
                            result[c] = GetIconName(ImageResourceType.PngFull);
                            break;
                        case "imagenamepngsmall":
                            result[c] = GetIconName(ImageResourceType.PngSmall);
                            break;
                        case "imagenamepngfull":
                            result[c] = GetIconName(ImageResourceType.PngFull);
                            break;
                        case "thumb":
                        case "bytes":
                            result[c] = GetImageFileContent(RandomImageType.Thumb);
                            break;
                        case "photo":
                        case "image":
                            result[c] = GetImageFileContent(RandomImageType.Photo);
                            break;
                    }
                }

                return result;
            }

            internal string ColumnName { get; private set; }
            internal string ColumnTypeName { get; private set; }
            internal Type ColumnType { get; private set; }

            public override string ToString()
            {
                return $"{ColumnName} ({ColumnTypeName})";
            }
        }
        #endregion
        /// <summary>
        /// Vrátí jednu náhodnou ikonu daného typu
        /// </summary>
        /// <param name="imageType"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vrátí jednu náhodnou ikonu typu SVG
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Vrátí jednu náhodnou ikonu typu PNG small
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Vrátí jednu náhodnou ikonu typu PNG full
        /// </summary>
        /// <returns></returns>
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
        /// Vrátí jméno souboru na lokálním disku, který obsahuje obrázek
        /// </summary>
        /// <returns></returns>
        public static string GetImageFileName(RandomImageType type)
        {
            if (type == RandomImageType.None) return null;

            if (__ImageFileNames is null) __ImageFileNames = new Dictionary<RandomImageType, string[]>();
            if (!__ImageFileNames.TryGetValue(type, out var images))
            { 
                switch (type)
                {
                    case RandomImageType.Thumb:
                        images = _LoadImageFileNamesPaths(@"c:\DavidPrac\Photos\Small", @"c:\DavidPrac\Images\Small");
                        break;
                    case RandomImageType.Photo:
                        images = _LoadImageFileNamesPaths(@"c:\DavidPrac\Photos\Full", @"c:\DavidPrac\Images\Full");
                        break;
                }
                __ImageFileNames.Add(type, images);
            }
            return GetItem(images);
        }
        private static Dictionary<RandomImageType, string[]> __ImageFileNames;
        /// <summary>
        /// Vrátí obsah ze souboru na lokálním disku, který obsahuje obrázek
        /// </summary>
        /// <returns></returns>
        public static byte[] GetImageFileContent(RandomImageType type)
        {
            string imageName = GetImageFileName(type);
            if (String.IsNullOrEmpty(imageName)) return null;
            return System.IO.File.ReadAllBytes(imageName);
        }
        /// <summary>
        /// Druh obrázku
        /// </summary>
        public enum RandomImageType { None, Thumb, Photo }
        /// <summary>
        /// Vrátí pole názvů souborů z prvního z dodaných adresářů, kde nějaký existuje
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        private static string[] _LoadImageFileNamesPaths(params string[] paths)
        {
            foreach (var path in paths) 
            {
                if (String.IsNullOrEmpty(path)) continue;
                if (!System.IO.Directory.Exists(path)) continue;
                var files = System.IO.Directory.GetFiles(path, "*.*", System.IO.SearchOption.AllDirectories)
                    .Where(n => isImage(n))
                    .ToArray();
                if (files.Length > 0) return files;
            }
            return new string[0];

            // Je to Image?
            bool isImage(string name)
            {
                string extn = System.IO.Path.GetExtension(name).ToLower();
                return (extn == ".jpg" || extn == ".jpeg" || extn == ".png" || extn == ".bmp" || extn == ".pcx" || extn == ".tif" || extn == ".gif");
            }
        }
        /// <summary>
        /// Typ zdroje ikony
        /// </summary>
        public enum ImageResourceType
        {
            /// <summary>
            /// Žádná ikona
            /// </summary>
            None,
            /// <summary>
            /// Vektorová ikona
            /// </summary>
            Svg,
            /// <summary>
            /// Bitmapa 16px
            /// </summary>
            PngSmall,
            /// <summary>
            /// Bitmapa 32px
            /// </summary>
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
        private static WordBookType _ActiveWordBook = WordBookType.TriMuziVeClunu;
        /// <summary>
        /// Zdroj slovní zásoby
        /// </summary>
        public enum WordBookType
        {
            /// <summary>
            /// Jerome Klapka Jerome: Tři muži ve člunu, o psu nemluvě
            /// </summary>
            TriMuziVeClunu,
            /// <summary>
            /// Jerome Klapka Jerome: Tři muži na toulkách
            /// </summary>
            TriMuziNaToulkach,
            /// <summary>
            /// Tábor Svatých
            /// </summary>
            TaborSvatych, 
            /// <summary>
            /// Camp Of Saints
            /// </summary>
            CampOfSaints  
        }
        /// <summary>
        /// Vrátí pole náhodných slov
        /// </summary>
        /// <returns></returns>
        private static string[] _GetWordBook()
        {
            string text = (_ActiveWordBook == WordBookType.TriMuziVeClunu ? Text_TriMuziVeClunu :
                          (_ActiveWordBook == WordBookType.TriMuziNaToulkach ? Text_TriMuziNaToulkach : 
                          (_ActiveWordBook == WordBookType.TaborSvatych ? Text_TaborSvatych :
                          (_ActiveWordBook == WordBookType.CampOfSaints ? Text_CampOfSaints :
                          Text_TriMuziNaToulkach))));

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
        /// Text "Tři muži ve člunu"
        /// </summary>
        public static string Text_TriMuziVeClunu
        {
            get
            {
                return @"Byli jsme čtyři - George, William Samuel Harris, já a Montmorency. Seděli jsme u mě v pokoji, kouřili jsme a každý z nás vykládal, jak je špatný - špatný z hlediska zdravotního, pochopitelně. Ani jeden jsme se necítili ve své kůži, a to nám šlo na nervy. Harris říkal, že chvílemi se s ním všechno začne tak prapodivně motat, že si stěží uvědomuje, co dělá; a pak říkal George, že s ním se taky chvílemi všechno motá a že i on si stěží uvědomuje, co dělá. A já, já zas měl v nepořádku játra. Věděl jsem, že jsou to játra, to, co mám v nepořádku, protože jsem si zrovna přečetl prospekt na zázrační jaterní pilulky, kde byly podrobně popsány příznaky, podle nichž člověk pozná, že má v nepořádku játra. Já měl ty příznaky všechny. Prapodivná věc, ale já když si čtu reklamní oznámení na nějakou zázračnou medicínu, tak vždycky dojdu k závěru, že tou dotyčnou nemocí, o které se tam pojednává, trpím v té nejvirulentnější podobě. Pokaždé odpovídá diagnóza přesně všem pocitům, které mám odjakživa já. Vzpomínám si, jak jsem si jednoho dne zašel do knihovny Britského muzea, abych se poučil, co má člověk podnikati proti jakési nepatrné chorůbce, která se o mě pokoušela - mám dojem, že to byla senná rýma. Vyhledal jsem příslušnou publikaci a prostudoval jsem si všechno, co jsem si prostudovat přišel; a potom jsem tak nějak bezmyšlenkovitě a bezplánovitě obracel listy a naprosto lhostejně si začal číst o jiných nemocech, jen tak povšechně. Už si nepamatuji, co to bylo za chorobu, do které jsem se poprvé víc zahloubal - vím jenom, že to byla nějaká strašlivá, zhoubná metla lidstva - ale ještě jsem ani zpolovičky nepřelétl očima výčet „varovných symptomů“, a už na mě dolehlo poznání, že tu chorobu mám. Na chvilku jsem úplně zkameněl hrůzou; a pak jsem, už v naprosté apatii zoufalství, zase obrátil pár stránek. narazil jsem na tyfus - pročetl jsem si příznaky - objevil jsem, že mám i tyfus, a už ho mám zřejmě řadu měsíců a že to vůbec nevím - a ptal jsem se v duchu, co mám ještě; nalistoval jsem tanec svatého Víta - zjistil jsem, jak jsem očekával, že ten mám taky - začal jsem se o svůj případ zajímat a rozhodl jsem se prozkoumat ho důkladně, a tak jsem se do toho dal podle abecedy. Prostudoval jsem Addisonovu chorobu a dověděl se, že na ni stůňu a že se do čtrnácti dnů může vystupňovat v addisonskou krizi. O Brightově nemoci jsem se ke své úlevě dočetl, že ji mám jen ve formě zcela mírné, takže v mém případě se s ní snad dá ještě nějaký čas žít. Zato cukrovku jsem měl s vážnými komplikacemi a s cholerou jsem se zřejmě už narodil. Svědomitě jsem probádal všechna písmena abecedy a pouze o jediném neduhu jsem mohl s jistotou usoudit, že jím netrpím, a to o sklonu k samovolným potratům. V první chvíli mě to dost zamrzelo; připadalo mi to jako urážlivé přezírání. Jak to, že netrpím sklonem k samovolným potratům? Jak to, že zrovna já mám být takto omezován? Po nějaké chvíli však ve mně převládly pocity méně chamtivé. uvážil jsem, že mám všechny ostatní ve farmakologii známé nemoci, začal jsem se na celou tu věc dívat méně sobecky a došel jsem k rozhodnutí, že se bez sklonu k samovolným potratům obejdu. Záškrt, jak se ukázalo, mě zachvátil, aniž jsem si toho povšiml, rovnou ve svém nejzavilejším stádiu a žloutenkou infekční jsem se očividně nakazil už ve věku jinošském. Po žloutence už tam žádné další choroby nebyly, usoudil jsem tudíž, že ani ve mně už nic jiného nehlodá. Seděl jsem a dumal. Přemítal jsem, jaký to musím být z lékařského hlediska zajímavý případ a jak cennou akvizicí bych byl pro kandidáty veškerého lékařství. Kdyby medici měli mne, nemuseli by dělat žádnou klinickou praxi. Já sám vydám za celou kliniku. Úplně by jim stačilo párkrát si mě obejít, a hned by si mohli doběhnout pro diplom. Pak mi v mysli vyvstal problém, kolik asi mi ještě zbývá let života. Zkoušel jsem sám sebe vyšetřit. Chtěl jsem si spočítat puls. Nejdřív jsem si vůbec žádný puls nenahmatal. Potom se zčistajasna roztepal. Vytáhl jsem hodinky a odpočítával. Napočítal jsem sto čtyřicet sedm tepů za minutu. Pak jsem chtěl vědět, jak mi tluče srdce. Ale ani srdce jsem si nenahmatal. Úplně se mi zastavilo. Od té doby jsem už byl sice přinucen přiklonit se k názoru, že na svém místě být muselo a zastavit se nemohlo, ale vysvětlení mi chybí pořád. Proklepal jsem si celé své průčelí, od míst, kterým říkám pás, až k hlavě, vzal jsem to i kousíček do stran a kousíček přes ramena na záda, ale na nic podezřelého jsem nenarazil a nic podezřelého jsem neslyšel. Ještě jsem se pokusil kouknout na jazyk. Vyplázl jsem ho, jak nejdál to šlo, zavřel jsem jedno oko a tím druhým jsem se snažil jazyk prohlédnout. Viděl jsem mu jenom na špičku a jediné, co mi ta námaha vynesla, bylo ještě důkladnější ujištění, že mám spálu. Vešel jsem do té čítárny jako šťastný, zdravím kypící člověk. Ven jsem se vybelhal jako zchátralá troska. Odebral jsem se k svému lékaři. Je to můj dobrý, starý kamarád, a když si vezmu do hlavy, že stůňu, tak mi vždycky sáhne na puls, mrkne mi na jazyk a popovídá si se mnou o počasí - a to všechno zadarmo; tak jsem si řekl, že tentokrát mu prokážu znamenitou službu, když k němu zajdu. „Doktor nesmí vyjít ze cviku,“ povídám si. „Ať má tedy mne! Na mně se pocvičí mnohem víc než na sedmnácti stech obyčejných, běžných pacientů s jednou, nanejvýš dvěma nemocemi.“ A tak jsem zašel rovnou k němu a on povídá: „Tak co tě trápí?“ „Líčením, co mě trápí,“ řekl jsem, „nebudu mařit tvůj čas, milý příteli. Život je krátký, takže bys mohl vydechnout naposled, než bych s tím líčením byl hotov. Povím ti raději, co mě netrápí. netrápí mě sklon k samovolným potratům. Proč mě sklon k samovolným potratům netrápí, to ti říci nemohu; spokoj se s faktem, že mě netrápí. Zato mě trápí všechno ostatní.“ A pověděl jsem mu, jak jsem na to všechno přišel. On si mě otevřel tam, kde mám ústa, nakoukl do mě, chňapl mi po zápěstí, pak mě, zrovna když jsem to nečekal, praštil přes prsa - tomu říkám zbabělost - a hned vzápětí mi do prsou trkl skrání. Načež se posadil, napsal recept, složil ho a podal mi ho a já si ho strčil do kapsy a šel jsem pryč. Ani jsem ten recept nerozložil. Šel jsem s ním do nejbližší lékárny a tam jsem se s ním vytasil. Lékárník si ho přečetl a vrátil mi ho. Řekl, tohle že on nevede. „Jste přece lékárník?“ zeptal jsem se. „Ano, jsem lékárník,“ řekl on. „Kdybych byl potravní konzum a k tomu ještě rodinný penzión, pak bych vám snad mohl být k službám. Co mi v tomto případě hází klacky pod nohy, je skutečnost, že jsem pouze lékárník.“ Přečetl jsem si ten recept. Stálo v něm: 1 půlkilový biftek, k tomu 1 půllitr piva pravidelně po 6 hodinách, 1 patnáctikilometrová procházka každé ráno, 1 postel přesně v 11 každý večer. A neláduj si do hlavy věci, kterým nerozumíš. Řídil jsem se podle těchto pokynů, což mělo ten šťastný výsledek - mluvím jenom za sebe - že můj život byl zachován a běží dokonce dodnes. Tentokrát, abych se vrátil k tomu prospektu na jaterní pilulky, jsem však měl všechny příznaky zcela nepochybně, především ten hlavní, totiž „naprostou nechuť k jakékoli práci.“ Co já si v tomhle směru vytrpím, žádný jazyk není s to vypovědět. To je u mě už od nejútlejšího dětství učiněné mučednictví. Když jsem byl větší chlapec, nebylo snad jediného dne, aby mě ta nemoc nechala na pokoji. Lékařská věda nebyla tenkrát ještě tak pokročilá jako dnes, a tak to naši napořád přičítali lenosti. „Tak necháš už konečně toho válení, ty kluku líná, ulejvácká, a začneš dělat něco pořádného, aby sis zasloužil byt a stravu?“ křičeli na mě, nevědouce samozřejmě, že jsem nemocný. A nedávali mi pilulky; dávali mi pohlavky. Ale ty pohlavky, ať to zní sebeneuvěřitelněji, mě často vyléčily - na nějakou chvíli. Teď už dovedu posoudit, že jeden takový pohlavek měl mnohem zdárnější účinek na má játra a mnohem hbitěji povzbudil mou chuť skočit sem nebo tam a bez dalšího otálení udělat, co se po mně chtělo, než dneska celá krabička pilulek. A to je častý zjev, poslechněte - tyhle obyčejnské, staromódní medicíny jsou kolikrát účinnější než všechny serepetičky z lékáren. A tak jsme seděli aspoň půl hodiny a popisovali si navzájem svoje nemoci. Já jsem líčil Georgeovi a Williamu Harrisovi, jak mi je, když ráno vstávám, a William Harris nám vykládal, jak mu je, když jde večer spát; a George stál na rohožce před krbem a vtipně a působivě nám mimicky předváděl, jak jemu je v noci. George si ovšem jenom namlouvá, že je nemocný; ve skutečnosti mu - mezi námi - nechybí vůbec nic. V tom okamžiku nám zaklepala na dveře paní Poppetsová; chtěla vědět, jestli už máme chuť na večeři. Smutně jsme se jeden na druhého pousmáli a odpověděli jsme, že bychom se snad měli pokusit vpravit do sebe ždibec potravy. Harris řekl, že nějaké to soustečko v žaludku často průběh choroby poněkud zmírní. A tak paní Poppetsová přinesla podnos a my jsme se dovlekli ke stolu a nimrali se ve filátkách na cibulce a v několika kouscích reveňového koláče. Musel jsem být v té době velice zesláblý; vzpomínám si totiž, že asi po půlhodině nebo tak nějak jsem ztratil všechnu chuť k jídlu - případ u mě neobvyklý - a že jsem už nechtěl ani sýr. Když jsme tu povinnost měli konečně s krku, naplnili jsme si znova sklenice, zapálili dýmky a opět jsme se rozhovořili o svém zdravotním stavu. Co to s námi doopravdy je, to nemohl s jistotou říci ani jeden z nás; ale jednomyslně jsme došli k náhledu, že ať už máme cokoli, máme to z přepracování. „Potřebujeme si zkrátka odpočinout,“ prohlásil Harris. „Odpočinout si a důkladně změnit způsob života,“ dodal George. „Přílišné mozkové vypětí ochromilo celou naši tělesnou soustavu. Když změníme prostředí a nebudeme nuceni neustále myslet, zrestaurujeme opět svou duševní rovnováhu.“ George má bratrance, který je v trestním rejstříku veden jako studující medicíny, a po něm se pochopitelně vyjadřuje tak trochu jako rodinný lékař. Já jsem s Georgem souhlasil a navrhoval jsem, abychom si vyhledali nějaké odlehlé, starosvětské místečko, daleko od běsnícího davu, a abychom tam prosnili slunečný týden v ospalých alejích - nějaký polozapomenutý koutek, který si někde mimo dosah hlučného světa schovaly víly - nějaké dravčí hnízdo, důmyslně přilepené na útesech času, kde by převalující se vlnobití devatenáctého století bylo slyšet jen slabounce a z velikých dálav. Harris řekl, že to by podle jeho názoru byla pěkná otrava. Zná prý taková místa, jaká mám na mysli; kdekdo tam chodí spat už v osm, ani za peníze, ani za dobré slovo tam člověk nesežene Milovníka sportu, a pro kuřivo aby jeden chodil patnáct kilometrů. „Kdepak,“ řekl Harris, „když chcete odpočinek a změnu, tak není nad výlet po moři. Já jsem proti výletu po moři vehementně protestoval. Výlet po moři vám udělá dobře, když na něj máte pár měsíců, ale na jeden týden je to nepředloženost. Vyplujete v pondělí, v nitru pevně zakořeněnou představu, jak si užijete. S lehkým srdcem zamáváte na rozloučenou mládencům na břehu, zapálíte si svou nej mohutnější dýmku a chvástavě si vykračujete po palubě, jako kdybyste byl kapitán Cook, sir Francis Drake a Krištof Kolumbus v jedné osobě. V úterý si říkáte, že jste se nikam plavit neměl. Ve středu, ve čtvrtek a v pátek si říkáte, že by vám bylo líp, kdyby bylo po vás. V sobotu jste už schopen pozřít kapku čistého hovězího bujónu, posadit se na palubu a s mírným, malátným úsměvem hlesnout v odpověď, když se vás dobrosrdeční lidé zeptají, jak se cítíte dneska. V neděli už zase můžete udělat pár kroků a sníst trochu hutné stravy. A v pondělí ráno, když s kufrem a deštníkem v ruce stojíte na boku lodi u zábradlí a máte už vystoupit na břeh, začíná se vám to ohromně líbit. Vzpomínám si, jak můj švagr se jednou vypravil na krátkou cestu po moři, aby si upevnil zdraví. Zajistil si kabinu z Londýna do Liverpoolu a zpět; a když dorazil do Liverpoolu, měl jen jedinou snahu: prodat ten zpáteční lístek. Nabízel ho prý s ohromnou slevou po celém městě a nakonec ho za osmnáct pencí prodal nějakému mládenci, který vypadal, jako kdyby měl žloutenku, a jemuž lékaři doporučili, aby jel k moři a hodně se pohyboval. „Moře!“ zvolal můj švagr, lýskyplně mu tiskna lístek do dlaně. „Jéje, toho si takhle užijete, že vám to vystačí na celý život. A co se pohybu týče? Když na téhle lodi budete jenom sedět, tak budete mít víc pohybu, než kdybyste na souši dělal kotrmelce.“ On sám - můj švagr totiž - jel zpátky vlakem. Prohlásil, že pro nějo je až dost zdravá severozápadní dráha. Jiný člověk, kterého jsem znal, se zase vydal na týdenní plavbu podél pobřeží, a než odrazili od břehu, přišel se ho zeptat stevard, jestli bude platit po každém jídle zvlášť, nebo jestli si přeje celé stravování rovnou předplatit. Stevard doporučoval to druhé řešení, přijde prý o mnoho laciněji. Za celý týden to prý bude dělat dvě libry pět šilinků. K snídani prý bývá ryba a pak nějaké rožněné maso. Oběd je v jednu a skládá se ze čtyř chodů. Večeře v šest - polévka, ryba, předkrm, porce masa nebo drůbeže, salát, sladkosti, sýr a ovoce. A v deset je lehká masitá večeře na noc. Můj přítel usoudil, že by se měl rozhodnout pro to předplatné (on je pořádný jedlík), a taky se pro ně rozhodl. Oběd se podával, sotva odpluli z Sheernessu. Můj přítel neměl takový hlad, jaký myslel, že mít bude, a tak se spokojil s kousíčkem vařeného hovězího a několika jahůdkami se šlehačkou. Odpoledne byl značně zadumaný; chvílemi měl pocit, jako by už dlouhé týdny nejedl nic jiného než vařené hovězí, a chvílemi si zase říkal, že se určitě už dlouhá léta živí jedině jahodami se šlehačkou. A ani to hovězí ani ty jahody se šlehačkou nedělaly spokojený dojem. Chovaly se spíš rozmrzele. V šest přišli tomu mému příteli oznámit, že se podává večeře. Ta zpráva v něm nevzbudila pažádné nadšení, uvědomoval si však, že by bylo záhodno odjíst něco z těch dvou liber a pěti šilinků, a tak se přidržoval lan a všeho možného a sestoupil do podpalubí. Na nejspodnějším schůdku ho uvítala lahodná vůně cibule a horké šunky, smíšená s vůní smažených ryb a zeleniny; a tu už k němu přistoupil stevard a se servilním úsměvem se zeptal: „Co mohu pánovi nabídnout?“ „Nabídněte mi rámě a vyveďte mě odtud,“ zněla mdlá odpověď. A tak s ním honem vyběhli nahoru, na závětrné straně ho zaklesli do zábradlí a tam ho nechali. Celé příští čtyři dny vedl prostý a bohulibý život pouze ve společnosti lodních sucharů (myslím tím opravdové suchary, nikoli členy lodní posádky, kteří měli všichni veliký smysl pro humor) a sodovky. Ale takhle k sobotě mu zase narozsl hřebínek a zašel si do jídelny na slabý čaj s toastem bez másla a v pondělí se už přecpal řídkým kuřecím vývarem. V úterý vystoupil z lodi, a když se plnou parou vzdalovala od přístavního můstku, zíral na ní pln lítosti. „To se jí to pluje,“ říkal si. „To se jí to pluje, když si odváží za dvě libry jídla, které patří mně a z kterého jsem nic neměl!“ Tvrdil, že kdyby mu byli přidali jeden jediný den, byl by si ten dluh zinkasoval. A proto já jsem byl proti výletu po moři. Nikoli kvůli sobě, to jsem řekl rovnou. Mně nanic nebývá. Ale měl jsem starost o Geortge. George zas tvrdil, že jemu by se zle neudělalo, a že by se mu to dokonce líbilo, ale mně a Harrisovi že to nedoporučuje, my dva že bychom to určitě odnesli. A Harris prohlásil, že co se jeho týče, pro něho je to odjakživa učiněná záhada, jak se někomu může podařit dostat mořskou nemoc - on prý je přesvědčen, že to lidi schválně předstírají, aby vypadali zajímavě - on prý si už mockrát přál mořskou nemoc dostat, ale nikdy to nedokázal. A potom nám vyprávěl historky, jak se plavil přes Kanál, když se moře tak divoce vztekalo, že cestující museli být připoutáni k lůžkům, a jak on a kapitán byli jediné dvě živé bytosti na palubě, kterým nic nebylo. Pak zas to byl on a druhý kormidelník, kterým nic nebylo. Prostě vždycky to byl on a ještě někdo. A když to nebylo on a ještě někdo, tak to byl on sám. To je stejně divná věc, že nikdo nedostane mořksou nemoc - na suché zemi. Na moři, tam vidíte spousty lidí, kterým je hrozně zle, tam jsou jich plné lodě; ale na suché zemi jsem ještě nepotkal člověka, který by věděl, co je to mít mořskou nemoc. Kde se ty tisíce a tisíce lidí, co nesnášejí plavbu po moři a co se jimi každá loď zrovna hemží, schovávají, když jsou na suché zemi, to je namouduši záhada. Kdyby se jich většina podobala tomu chlapíkovi, kterého jsem jednou viděl na lodi do Yarmouthu, pak bych tu zdánlivou šarádu dovedl rozlousknout docela snadno. Jen jsme odrazili od southendské přístavní hráze, už se ten člověk nebezpečně vykláněl z jednoho okénka na boku. Běžím k němu, abych ho zachránil. „Hej, nevyklánějte se tolik!“ povídám a tahám ho zpátky za rameno. „Sletíte do vody!“ „Ach bóže, už abych tam radši byl!“ To byla jediná odpověď, kterou jsem z něho dostal. A tak jsem ho nechal být. Za tři neděle jsem ho uviděl v kavárně jednoho hotelu v Bathu. Vyprávěl o svých cestách a nadšeně vykládal, jak miluje moře. „Mně prostě to houpání vůbec nic nedělá,“ pravil zrovna v odpověď na závistivý dotaz jakéhosi dobře vychovaného mladíka. „Pravda ovšem je, že jednou, jedinkrát mě bylo kapánek divně, to přiznávám. U Hornova mysu. To ráno potom naše loď ztroskotala.“ „A nebylo vám jednou trošku nanic u southendského mola?“ zeptal jsem se ho. „Tam jste si přece přál, aby vás radši hodili do moře.“ „U southendského mola?“ opáčil a tvářil se, jako by nevěděl, oč jde. „Ano. Při plavbě do Yarmouthu. V pátek to byly tři neděle.“ „Á! No ovšem,“ zvolal v šťastném osvícení. „Už si vzpomínám. To mě tlačil žaludek, tenkrát odpoledne. To po těch kyselých okurkách, poslechněte. Na žádné slušné lodi jsem jakživ nejedl tak mizerné kyselé okurky. Vy jste si je taky dal?“ Já jsem si proti mořské nemoci vymyslel sám pro sebe znamenitý preventivní prostředek: já se kolébám. To se postavíte doprostřed paluby, a jak se loď kymácí a houpe, pohybujete tělem tak, abyste pořád stáli svisle. Když se zvedá příď, nakláníte se celým tělem dopředu, až se nosem skoro dotknete paluby; a když jde nahoru záď, tak se nakláníte dozadu. Hodinu nebo dvě to jde moc dobře, celý týden se ovšem takhle kolébat nemůžete. Najednou povídá Jiří: „Tak pojeďme na řeku.“ Tam bychom měli čistý vzduch, povídá, pohyb i klid; ustavičná změna scenérie by zaujala našeho ducha (i to, co místo toho má Harris) a po té tělesné námaze bychom s chutí jedli a výtečně spali. Harris na to řekl, že podle jeho mínění by George neměl dělat nic takového, co by v něm probouzelo ještě větší chuť spát, než jakou má v jednom kuse, jelikož to by už pro něho bylo nebezpečné. Nedovede si prý dobře představit, jak by George dokázal prospat ještě víc času, než prospí teď, když přece den má pouze čtyřiadvacet hodin, a to jak v létě tak v zimě. Ale kdyby to přece jen dokázal, pak už by to prý bylo totéž, jako kdyby byl mrtvý, a tím by aspoň ušetřil za byt a za stravu. Pak ale Harris dodal, že přesto přese všecko by mu řeka seděla. Já sice nevím, jak by řeka mohla sedět, ledaže by seděla modelem, a ani potom mi není jasné, na čem by seděla, ale mně řeka seděla taky, a tak jsme oba, Harris i j á, řekli, že to je od George výborný nápad, a řekli j sme to tónem, z kterého byl dost j asně znát náš údiv nad tím, že se z George najednou vyklubal člověk tak rozumný. Jediný, komu ten návrh nepadl do noty, byl Montmorency. Jemu se ovšem řeka nikdy moc nezamlouvala, jemu opravdu ne. „Copak vy, mládenci, vy si tam na své přijdete,“ povídá, „vy máte řeku rádi, ale já ne. Co já tam budu dělat? Scenérie, to pro mě není nic a kuřák nejsem. A když uvidím myš, tak mi nezastavíte; a když se mně bude chtít spát, tak budete s lodí všelijak blbnout a shodíte mě do vody. Jestli se ptáte na moje mínění, tak já to považuju za úplnou pitomost.“ Ale byli jsme tři proti jednomu, a tak byl ten návrh přijat.

Vytáhli jsme mapy a rozvíjely plány. Dohodli jsme se, že vyrazíme příští sobotu z Kongstonu. Harris a já tam pojedeme hned ráno a loď dopravíme do Chertsey a George, který se může utrhnout ze zaměstnání až odpoledne (chodí denně od deseti do čtyř spát do jedné banky, denně s výjimkou soboty, kdy ho vždycky probudí a vyšoupnou ven už ve dvě), se s námi sejde až tam. Máme „tábořit pod širým nebem“, nebo spát v hostincích? George a já jsme hlasovali pro táboření pod širým nebem. Říkali jsme, že by to bylo takové romantické a nespoutané, takové skoro patriarchální. Ze srdcí chladných, smutných mraků zvolna vyprchává zlatá vzpomínka na zemřelé slunce. Ptáci už nezpívají, ztichli jako truchlící děti a jen plačtivý křik vodní slípky a chraptivé vrzání chřástala polního čeří posvátnou hrůzou strnulou tišinu nad vodním lůžkem, na němž umírající den dýchá z posledního. Z potemnělých lesů na obou březích se bezhlučným, plíživým krokem vynořuje stradšidelná armáda noci, ty šedé stíny, které mají zahnat stále ještě otálející zadní voj světla; tichýma, neviditelnýma nohama postupují nad vlnící se trávou při řece a skrze vzdychající rákosí; a noc na chmurném trůnu rozkládá své černé perutě nad hasnoucím světlem a ze svého přízračného paláce, ozářeného sinalými hvězdami, kraluje v říši ticha. A my zajíždíme s lodičkou do nějaké poklidné zátočinky, rozbíjíme tábor a vaříme a jíme střídmou večeři. Pak si nacpáváme a zapalujeme statné dýmky a melodickým polohlasem rozpřádáme příjemný potlach; a do zámlk v našem hovoru nám řeka, dovádějící kolem lodi, šplouchá podivuhodné starobylé zkazky a starobylá tajemství a tichounce zpívá tu starobylou dětskou písničku, kterou už zpívá tolik tisíc let - a kterou tolik tisíc let ještě zpívat bude, než jí vysokým věkem ochraptí hlas -, tu písničku, o níž si my, kteří jsme se naučili milovat na řece její proměnlivou tvář, my, kteří tak často hledáme útočiště na jejích pružných ňadrech, myslíme, že jí rozumíme, i když bychom nedokázali vyprávět pouhými slovy obsah toho, čemu nasloucháme. A tak tam sedíme, nad lemem té řeky, a měsíc, který ji má taky rád, sestupuje níž, aby jí dal bratrské políbení a objal ji svýma stříbrnýma pažema a přitulil se k ní. A my se na ni dáváme, jak plyne, věčně ševelící, věčně rozezpívaná, na schůzku se svým králem, s mořem - až se naše hlasy ztratí v mlčení a až nám vyhasnou dýmky - až i my, mládenci tak všední, tak tuctoví, máme najednou neuvěřitelný pocit, že jsme plni myšlenek, napůl rozteskňujících, napůl rozjařujících, a nechceme už a ani nepotřebujeme mluvit - až se zasmějeme, vstaneme, vyklepáme z vyhaslých dýmek popel, řekneme si „dobrou noc“ a ukolébáni šuměním vody a šelestěním stromů usneme pod velkolepými tichými hvězdami a máme sen, že země je zase mladá - mladá a líbezná, jaká bývala, než jí staletí rozhořčení a starostí zbrázdila sličnou tvář, než jí to milující slunce zestárlo nad hříchy a zpozdilostmi jejích dětí - líbezná, jako by v těch dávných dnech, kdy jako nezkušená matka nás, své dítky, živila z hlubokosti svých prsů - než nás svody nalíčené civilizace odlákaly z její schovívavé náruče a než jsme se kvůli jedovatým posměškům a afektovanosti začali stydět za ten prostý život, co jsme vedli u ní, a za ten prostý, ale důstojný domov, v němž se lidstvo před tolika tisíciletími zrodilo. „A co když bude pršet?“ povídá Harris. Harrise jakživi ničím nedojmete. V Harrisovi není kouska poetičnosti - ani ždibec divého prahnutí po nedosažitelnu. Harris jakživ „nepláče, nevěda proč“ Má-li Harris v očích slzy, můžete se vsadit, že buď zrovna snědl syrovou cibuli, nebo si nakapal na kotletu příliš mnoho worcesterské omáčky. Kdybyste s Harrisem stanuli v noci na mořském břehu a zašeptali: „Zmlkni! Což neslyšíš? To nemůže být nic jiného než mořské panny pějící v hlubinách těch zvlněných vod. Či to truchlící duše lkají žalozpěvy nad svými umrlými těly, zachycenými ve spleti chaluh?“ Harris by vás vzal pod paží a řekl: „Já ti povím, co to je, kamaráde. Leze na tebe rýma. Pojď hezky se mnou, já znám tadyhle za rohem podniček, kde dostaneš nejlepší skotskou whisky, jakou jsi kdy ochutnal, a ta tě v cuku letu postaví na nohy.“ Harris totiž vždycky ví o podničku za rohem, kde člověk dostane něco vynikajícího z oblasti nápojů. Jsem přesvědčen, že kdybyste Harrise potkali v ráji (za předpokladu, že jeho byste tam potkat mohli), hned by vás vítal slovy: „To jsem rád, kamaráde, že jsi tady. Já tadyhle za rohem našel rozkošný podniček, kde dostaneš nektar - no doopravdy prvotřídní.“ V tom případě, o němž je řeč, když totiž běželo o to táboření pod širým nebem, byl však Harrisův praktický názor velmi vhodnou připomínkou. Tábořit pod širým nebem za deštivého počasí, to není žádná radost. Je večer. Jste promoklí na kůži, v lodi je dobrých pět centimetrů vody a všecky vaše věci jsou vlhké. Spatříte na břehu místo, kde je o něco málo míň kaluží než na ostatních místech, která jste ohledali, a tak přistanete a vylovíte z lodi stan a dva z vás se ho pokoušejí postavit. Je prosáklý vodou a těžký a plácá sebou a hroutí se a lepí se vám na hlavu a šíleně vás rozčiluje. A přitom nepřetržitě leje. Postavit stan, to je i za sucha dost obtížné; ale když prší, tak je to práce herkulovská. A ten druhý chlapík, místo aby vám pomáhal, si z vás očividně dělá blázny. Sotva se vám podařilo tu vaši stranu jakžtakž vypnout, on vám s ní ze svého konce cukne, a zas je všecko na draka. „No tak! Co to vyvádíš?“ voláte na něj. „Co ty to vyvádíš?“ odpovídá on. „Pusť to!“ „Ty tam za to netahej! Vždyť ty se v tom vůbec nevyznáš, ty trumbero!“ křičíte. Já se v tom vyznám,“ ječí on. „Ty to tam máš pustit!“ „Povídám ti, že se v tom nevyznáš,“ řvete a nejraději byste po něm skočil; škubnete lanem a vytáhnete mu ze země všecky kolíky. „Ách, ten pitomec pitomá!“ slyšíte ho cedit mezi zuby. A vzápětí následuje prudké trhnutí - a vyletí kolíky na vaší straně. Odložíte palici a vykročíte kolem stanu, abyste tomu nemehlu pověděl, co si o tom všem myslíte, ale současně i on vykročí, stejným směrem jako vy, aby vám vyložil, jak on se na to dívá. A tak běháte za sebou, pořád dokolečka, a nadáváte si, až se stan sesuje na hromadu a vy se přes jeho trosky kouknete jeden na druhého a oba jedním dechem rozhořčeně zařvete: „Tady to máš! Co jsem ti říkal?“ A tu váš třetí kamarád, který zatím vybíral z lodi vodu a lil si ji do rukávů a posledních deset minut nepřetržitě klel, začne být zvědav, proč si u všech ďasů takhle hračičkaříte a proč jste ještě nepostavili ten zatracený stan. Nakonec ho - tak či onak - přece jen postavíte a vynesete z lodi svéí věci. Chtít si rozdělat oheň z dříví, to by byl pokus naprosto beznadějný, a tak si zapálíte lihový vařič a sesednete se kolem toho. Převládající složkou večeře je dešťová voda. Chléb, to je dešťová voda ze dvou třetin, nesmírně bohaté na dešťovou vodu jsou i taštičky se sekaným hovězím, a máslo, džem, sůl a káva daly dohromady s dešťovou vodou polévku. Po večeři zjistíte, že máte vlhký tabák, a že si nemůžete zakouřit. Naštěstí máte s sebou láhev nápoje, který požit v náležitém množství rozveseluje a rozjařuje, a ten ve vás natolik obnoví zájem o život, že vás přiměje jít spat. Načež se vám zdá, že si vám zčistajasna sedl na prsa slon a současně že vybuchla sopka a svrhla vás na dno moře - i s tím slonem, který vám dál klidně spí na prsou. Probudíte se a okamžitě pochopíte, že se vskutku stalo něco strašlivého. V prvním okamžiku máte dojem, že nastal konec světa; pak si uvědomíte, že to se stát nemohlo, ale že se na vás vrhli zloději a vrahové nebo že vypukl požár, a toto mínění vyjadřujete obvyklým způsobem. Pomoc však odnikud nepřichází a vy víte jen jedno: že po vás šlapou tisíce lidí a že se co nevidět udusíte. Nejste však zřejmě sám, kdo je na tom bledě. Slyšíte přidušené výkřiky, ozývající se zpod vašeho lůžka. Rozhodnete se, že ať se děje co se děje, prodáte svůj život draho, a začnete se zuřivě rvát, rozdávaje doprava i doleva rány a kopance a v jednom kuse přitom divoce ječíte, až konečně někde něco povolí a vy máte najednou hlavu na četstvém vzduchu. Ve vzdálenosti asi půl metru od sebe matně rozeznáte nějakého polooblečeného halamu, který se vás chystá zavraždit, připravujete se na zápas na život a na smrt, ale vtom se vám v mozku rozbřeskne, že je to Jim. „Á ták to jsi ty!“ povídá, neboť vás v tom okamžiku taky poznal. „No!“ odvětíte a protíráte si oči. „Co se stalo?“ „Ale ten zatracený stan se zřítil,“ povídá Jim. „Kde je Bill?“ Pak oba hlasitě hulákáte „Bille!“ a pod vámi se začne vzdouvat a zmítat půda a z těch zřícenin se ozývá ten přiškrcený hlas, který jste už před chviličkou slyšeli: „Přestaň se mi, ksakru, válet po hlavě!“ A ven se vyškrábe Bill, zablácený, pošlapaný lidský vrak, a zcela bezdůvodně má útočnou náladu - zjevně se domnívá, že to všechno jste udělali jemu naschvál. Ráno ani jednomu z vás tří není valně do řeči, neboť jste se v noci strašně nachladili; kvůli každé maličkosti se hned hádáte a po celou snídani jeden na druhého sípavým chrapotem nadáváte. Proto jsme se rozhodli, že venku budeme spát jen za pěkných nocí; a když bude pršet nebo když zatoužíme po změně, přespíme jako pořádní lidé v hotelu, v hostinci nebo v krčmě. Montmorency přivítal tento kompromis s velikým uspokojením. On si v romantické samotě nelibuje. On horuje pro místa hlučná; a je-li to tam kapánek obhroublé, tím víc se mu tam líbí. Kdybyste Montmorencyho viděli, určitě byste měli dojem, že je to andílek, který z nějakého člověčenstvu utajeného důvodu byl seslán na zem v podobě malého foxteriéra. Montmorency má takový ten výraz, říkající „Ach, ten svět je tak zkažený a já bych si tolik přál ho zlepšit a zušlechtit,“ takový ten výraz, jaký dovede vehnat slzy do očí zbožných starých dam a pánů. Když se ke mne přistěhoval, aby žil na mé útraty, nevěřil jsem, žře se mi podaří uchovat ho při životě. Sedával jsem u něho a pozoroval ho, jak leží na dece a vzhlíží ke mně, a říkával jsem si: „Ach jé, tenhle pejsek tady dlouho nebude. Toho mi jednou unesou v nebeské káře k janým výšinám, nic jiného ho nečeká.“ Ale když jsem pak zaplatil dobrého půl tuctu kuřat, která roztrhal; když jsem ho vrčícího a rafajícího vyvlekl za zátylek ze sto čtrnácti pouličních rvaček; když mi jedna rozběsněná ženská přinesla ukázat mrtvou kočku a říkala mi vrahu; když mě soused, co bydlí ob dům, pohnal před soud, poněvadž prý nechávám volně pobíhat krvelačného psa, který ho za chladné noci zahnal do jeho vlastní kůlny a přes dvě hodiny mu nedovolil vystrčit ze dveří nos; a když jsem se dověděl, že náš zahradník se vsadil, že Montmorendy zakousne ve stanovené době tolik a tolik krys, a vyhrál třicet šilinků, začal jsem věřit, že ten můj pejsánek bude přece jen ponechán na světě o něco déle. Potloukat se kolem stájí, shromažďovat hordy těch nejvykřičenějších hafanů, jaké lze ve městě sehnat, pochodovat v jejich čele po perifériích a vodit je tam do boj ů s dalšími vykřičenými hafany - task si Montmorency představuje „život“. A proto, jak jsem prve poznamenal, schválil tak emfaticky ten návrh týkající se krčem, hostinců a hotelů. Když jsme takto k spokojenosti všech nás čtyř rozřešili problém noclehů, zbývalo už pouze projednat, co všechno si vezmeme s sebou; i zahájili jsme o tom rozepři, když tu Harris prohlásil, že on se už toho na jeden večer nařečnil až dost, a navrhl, abychom si někam vyšli a rozjasnili tváře; objevil prý kousek za náměstím podniček, kde si můžeme dát kapku irské, která stojí za ochutnání. George řekl, že on žízeň má (co ho znám, tak jaktěživ neřekl, že žízeň nemá), a ježto já měl takovou předtuchu, že trocha whisky, pěkně teplé, s plátečkem citrónu, mi značně uleví v mé nevolnosti, byla debata za všeobecného souhlasu odročena na příští večer a celé shromáždění si nasadilo klobouky a vyšlo z domu. 

A tak jsme se příští večer opět sešli, abychom prohovořili a projednali své plány. Harris řekl: „V první řadě se musíme dohodnout, co si vezmeme s sebou. Ty si vem kousek papíru, Jerome, a piš, ty Georgi, ty si vem k ruce katalog z toho obchodu se smíšeným zbožím a mně někdo půjčte tužku a já potom sestavím seznam.“ To je celý Harris - ochotně vezme sám na sebe bířmě veškeré práce a vloží je na bedra těch druhých. Vždycky mi připomene chudáka strýce Podgera. V životě jste neviděli takový blázinec po celém domě, jako když se můj strýc Podger pustí do nějaké práce. Od rámaře přijde obraz, stojí opřen o stěnu v jídelně a čeká, až ho někdo pověsí. Teta Podgerová se zeptá, co s ním, a strýc Podger řekne: „Á, to nechte na mně. S tím si nikdo z vás nedělejte starosti. To všechno zařídím sám.“ A sundá si sako a dá se do toho. Služku pošle koupit za šest pencí hřebíků, a vzápětí za ní žene jednoho z podomků, aby jí řekl, jak ty hřebíky mají být dlouhé. A tak postupně zaměstná a uvede v chod celý dům. „Ty jdi pro kladivo, Wille,“ volá, „a ty mi přines pravítko, Tome; a pak budu potřebovat štafle a taky bych tu měl mít nějakou kuchyňskou stoličku; a Jime! ty skoč k panu Gogglesovi a řekni mu: ,Tatínek se dává pěkně poroučet a doufá, že s tou nohou to už máte lepší, a půjčil byste mu laskavě libelu?‘ A ty tady zůstaň, Marie, někdo mně bude muset posvítit. A až se vrátí to děvče, tak musí ještě doběhnout pro kus pořádného špagátu. A Tome! - kde je Tom? - pojď sem, Tome, ty mi ten obraz podáš.“ Potom obraz zvedne, upustí ho, obraz vyletí z rámu, strýc se snaží zachránit sklo a pořeže se; načež poskakuje po pokoji a hledá svůj kapesník. Ale nemůže ten kapesník najít, protože ho má v kapse saka, které si sundal, a nemá ponětí, kam si to sako dal, a tak celá domácnost musí nechat shánění nářadí a dát se do shánění strýcova saka, a strýc zatím tancuje po celém pokoji a kdekomu se plete pod nohy. „Copak v celém baráku není ani jediná živá duše, která by věděla, kde to moje sako je? V životě jsem neviděl takovou hromadu budižkničemů, na mou duši, že ne. Je vás šest! - a nejste s to najít sako, které jsem si sundal ani ne před pěti minutami! To vám teda řeknu, že ze všech...“ Zvedne se, zjistí, že si na tom saku seděl, a křičí: „No, teď už hledat nemusíte, už jsem to sako našel sám. Od vás tak chtít, abyste mi něco našli! To bych to rovnou mohl chtít na naší kočce! “ A když se po půlhodině, věnované obvazování strýcova prstu, sežene nové sklo a snesou se všechny nástroje a štafle a stolička a svíčka, a celá rodina i se služkou a s posluhovačkou stojí v půlkruhu připravena pomáhat, strýc se znovu pustí do díla. Dva mu musejí držet stoličku, třetí mu na ni pomůže vylézt a dává mu tam záchranu, čtvrtý mu podává hřebík, pátý k němu zvedá kladivo a strýc sáhne po hřebíku a upustí ho. „No prosím,“ řekne dotčeným tónem, „a hřebík je v tahu!“ A my musíme všichni na kolena a plazit se a hledat, zatímco on stojí na stoličce a remcá a přeje si vědět, jestli ho tam míníme nechat stát celý večer. Hřebík je konečně na světě, ale strýc zase ztratil kladivo. „Kde mám kladivo? Kam já j sem to kladivo dal? Kristepane! Čučí vás na mě sedum a ani j eden z vás neví, kam jstem dal kladivo! “ Vypátráme kladivo, ale on zase najednou ne a ne najít na stěně znamínko, kam se má zatlouci hřebík, a my všichni musíme jeden po druhém vylézt k němu na stoličku a snažit se to jeho znamínko objevit. Každý je vidíme někde jinde a on nám postupně všem vynadá, že jsme pitomci a ať radši slezeme dolů. A pak se chopí pravítka a stěnu přeměří a zjistí, že od rohu to má dělat polovinu ze sedmdesáti sedmi centimetrů a devíti a půl milimetru, a pokouší se to vypočítat z hlavy, a to ho dožene div ne k nepříčetnosti. Pak se to i my pokoušíme vypočítat z hlavy, každému vyjde něco docela jiného a jeden druhému se pošklebujeme. Načež ve všeobecné vřavě upadne v zapomenutí původní číslo a strýc Podger musí měřit znova. Tentokrát si na to vezme kus provázku a v kritickém okamžiku, když se z té své stoličky vyklání v pětačtyřicetistupňovém úhlu do strany a snaží se dosáhnout bodu ležícího o sem a půl centimetru dále, než kam vůbec může dosáhnout, provázek se mu vysmekne z prstů a ten blázen stará uklouzne a zřítí se na otevřené piáno, a tím, jak znenadání třískne hlavou i tělem do všech kláves současně, vyloudí vskutku pěkný hudební efekt. A teta Marie prohlásí, že nedovolí, aby děti poslouchaly takové výrazy. Posléze si strýc Podger znovu označí příslušné místo, levou rukou na ně nasadí špičku hřebíku a pravou rukou se chopí kladiva. Hned prvním úderem si rozmačká palec a s vřískotem upustí kladivo na prsty něčí nohy. Teta Marie vysloví mírným hlasem naději, že příště jí snad strýc Podger zavčas oznámí, kdy zas bude zatloukat do zdi hřebík, aby se mohla domluvit s matkou a strávila ten týden u ní. „Ách, vy ženské! Vás všecko hned vyvede z míry!“ odvětí strýc Podger, sbíraje se na nohy. „To já, já tyhle drobné domácí práce dělám rád.“ A poté zahájí další pokus; při druhém úderu proletí hřebík skrze celou omítku a půlka kladiva za ním a strýc Podger naletí tak prudce na zeď, že si málem rozplácne nos. I musíme opět vyhledat pravítko a provázek a ve zdi vznikne nová díra; obraz visí až někdy k půlnoci - značně nakřivo a nespolehlivě - stěna na metry kolem dokola vypadá, jako kdyby po ní byl někdo jezdil hráběmi, a kdekdo je k smrti utahaný a umlácený - jen strýc Podger ne. „No - a je to!“ praví, těžce sestoupí se stoličky na kuří oko naší posluhovačky a obhlíží spoušť, kterou natropil, s očividnou pýchou. „A to prosím existujou lidi, kteří by si na takovou maličkost někoho zjednali!“ A Harris, to já vím, bude zrovna taková, až bude starší. Však jsem mu to taky řekl. A prohlásil jsem, že nepřipustím, aby si sám nabral tolik těžké práce. „Ne, ne! Ty si vezmeš papír a tužku a ten katalog, George bude zapisovat a tu hlavní práci udělám já.“ První seznam, který jsme sestavili, jsme museli zahodit. Bylo nám jasné, že horním tokem Temže by neproplulo plavidlo tak velikánské, aby mohlo pobrat všechny věci, které jsme uvedli jako nepostradatelné; a tak jsme ten seznam roztrhali a podívali j sme se jeden na druhého. A George povídá: „Poslouchejte, my na to jdeme úplně špatně. Nesmíme myslet na věci, které s sebou mít chceme, ale jenom na věci, které s sebou mít musíme “ Čas od času se George projeví docela rozumně. Což člověka překvapí. A tomuhle říkám přímo moudrost, nejen s ohledem na ten tehdejší případ, ale i se zřetelem na celou naši plavbu po řece života. Kolik lidí si na tuhle cestu přetíží loď, že se s nimi div nepotopí pod nákladem hloupostí, o nichž si leckdo myslí, že jsou pro zábavu a pohodlí na tom výletu nepostradatelné, které však ve skutečnosti nejsou nic jiného než bezúčelné haraburdí! Až po vršek stěžně přecpou ten ubohý korábek skvostnými obleky a velikými domy; zbytečným služebnictvem a davem nastrojených přátel, kteří o ně ani za mák nestojí a o které oni sami nestojí ani za máček; nákladnými radovánkami, při nichž se nikdo nebaví, okázalostmi a obřadnostmi, konvencemi a předstíráním a hlavně - a to jsou ty nejtěžší, nejnesmyslnější kusy toho haraburdí! - strachem, co si pomyslí soused, přepychem, který se už přejídá, zábavičkami, které už nudí, nicotnou parádou, pod níž - jako pod tou železnou korunou nasazovanou za dávných časů zločincům - obolavěná hlava krvácí a klesá do bezvědomí. To je haraburdí, člověče, samé haraburdí! Hoď to všecko přes palubu! S tím je loď tak těžká, že u vesel div neomdlíš. S tím je tak nemotorná, její řízení tě přivádí do tolika nebezpečí, že si ani na okamžik neoddychneš od úzkostí a starostí, ani na okamžik si nemůžeš odpočinout v lenivém zasnění - nemáš kdy se zahledět na prchavé stíny, ve vánku lehce těkající nad mělčinami, na sluneční paprsky, jiskřivě prosakující mezi vlnkami, na vznešené stromy na břehu, shlížející na své vlastní podoby, na lesy, které jsou samá zeleň a samé zlato, na lekníny a stulíky, na zádumčivě se vlnící rákosí, na houštiny ostřice, na vstavače, na modré pomněnky. To haraburdí hoď přes palubu, člověče! Ať loď tvého života je lehká, naložená jenom tím, co nutně potřebuješ - domovem, kde jsi doopravdy doma, prostými radostmi, jedním nebo dvěma přáteli, hodnými toho označení, někým, koho můžeš milovat a kdo může milovat tebe, kočkou, psem, dýmkou - nebo i dvěma -, dostatečnou zásobou jídla a dostatečnou zásobou šatstva - a o něco víc než dostatečnou zásobou pití; neboť žízeň je věc nebezpečná. Na takové lodi se ti pak bude snadněji veslovat, ta se s tebou hned tak nepřevrhne, a když se převrhne, tak to nebude taková pohroma; dobrému, solidnímu zboží voda moc neuškodí. A budeš mít čas na přemýšlení, zrovna tak jako na práci. Čas nalokat se slunečního jasu života - čas zaposlouchat se do té eolské hudby, kterou boží vítr vyluzuje ze strun lidských srdcí kolem nás - čas... Jé, moc se omlouvám. Dočista jsem zapomněl. No tak j sme ten seznam přehráli na George, a on ho začal sepisovat. „Stan si s sebou brát nebudeme,“ prohlásil, „opatříme si na loď natahovací střechu. To bude o moc jednodušší a taky pohodlnější.“ Ten nápad se nám zamlouval a byli jsme pro. Nevím, jestli jste to, o čem teď mluvím, už někdy viděli. To překlenete loď železnými žebry, zahutými do oblouků, přes ně napnete velikánskou plachtu, tu dole připevníte k oběma bokům po celé jejich délce, a tím vlastně loď proměnte v takový domeček, kde je krásně útulno, i když kapánek dusno. Jenomže všechno na světě má svou stinnou stránku, jak poznamenal ten chlapík, co mu umřela tchýně, když na něj přišli, aby zaplatil útraty na její pohřeb. George řekl, že v tom případě musíme mít s sebou tři deky, lampu, nějaké mýdlo, kartáč a hřeben (společný), kartáček na zuby (každý svůj), zubní pasty, holicí náčiní (to je, jako když se učíte slovíčka na hodinu francouzšitny, viďte?) a dvě veliké osušky ke koupání. Často si všímám, jaké se dělají gigantické přípravy na koupání, když lidi jednou někam k řece, ale jak se potom, když tam jsou, moc nekoupou. To je zrovna tak, jako když jedete k moři. Já si pokaždé umiňuju - když o tom přemýšlím ještě v Lonýdně -, že si denně ráno přivstanu a půjdu se namočit do moře ještě před snídaní, a vždycky si zbožně zapakuju plavky a osušku. Kupuju si výhradně červené plavky. Červené plavky se mi líbí nejvíc. Výtečně mi jdou k pleti. Ale když potom k tomu moři přijedu, tak můj pocit, že tu časnou ranní koupel tolik potřebuju, není už najednou tak výrazný, jako byl v Londýně. Naopak, spíš mám pocit, že potřebuju zůstat do posledního okamžiku v posteli a pak rovnou sejít dolů ke snídani. Jednou nebo dvakrát ve mně zvítězí čest, vstanu před šestou, trochu se přiobléknu, seberu plavky a ručník a ponuře doškobrtám k moři. Ale rozkoš mi to nepůsobí žádnou. Zřejmě tam chovají zvlášť řezavý východní vítr, který mají nachystaný speciálně pro mě, až se časně ráno půjdu koupat; k té příležitosti taky seberou kdejaký trojhranný kámen, všechny položí hezky navrch, zašpičatí skaliska a ty špice zasypou trochou písku, abych je neviděl, a navíc vezmou moře a posunou je o tři kilometry dál, takže musím, schoulený do svých vlastních zkřížených paží a drkotaje zimou, přebrotit lán patnácticentometrové hloubky. A když konečně dorazím k moři, to moře se chová neurvale a vysloveně mě uráží. Drapne mě obrovská vlna, složí si mě, že vypadám jako vsedě, a pak nejsurovější silou, jaké je schopna, se mnou práskne na skalisko, které tam dali schválně kvůli mně. A dřív než stačím zařvat „Au! Júúú!“ a zjistit, co mám pryč, už je ta vlna zpátky a smýkne se mnou doprostřed oceánu. Zběsile sebou plácám směrem k pobřeží, pochybuju, že ještě někdy uvidím domov a přátele, a vyčítám si, proč jsem nebyl hodnější na svou sestřičku v klukovských letech (když jsem já byl v klukovských letech, pochopitelně, nikoli ona). A přesně v okamžiku, kdy jsem se vzdal veškeré nadsěje, vlna se dá náhle na ústup a nechá mě rozcabeného jako mořskou hvězdici na mokrém písku; vyškrábu se na nohy, ohlédnu se s vidím, že jsem plaval o život v sotva půlmetrové hloubce. Po jedné noze doskáču na břeh, obléknu se a dopotácím se domů, a tam musím předstírat, že se mi to líbilo. I v tomto případě jsme všichni vedli řeči, jako kdybychom se chystali každé ráno si pořádně zaplavat. Georte pravil, že není nad to, probudit se za čiperného rána v lodi a hned se ponořit do průhledné řeky. Harris pravil, že nic tak nezvýší chuť k jídlu, jako když si člověk před snídaní zaplave. U něho prý to vždycky zvýší chuť k jídlu. Na to řekl George, že jestli po koupeli spořádá Harris ještě víc jídla než obvykle, pak on, George, musí být ostře proti tomu, aby se Harris koupal. I tak to prý bude perná dřina, táhnout proti proudu tolik jídla, aby to stačilo pro Harrise. Já jsem však George přiměl k zamyšlení, oč bude pro nás příjemnější mít v dodi Harrise čistého a svěžího, i kdybychom vážně museli s sebou vzít o pár metráků proviantu víc; a George musel to mé hledisko uznat a svou námitku proti Harrisovým koupelím vzal zpátky. Posléze jsme se dohodli, že osušky si s sebou vezmeme tři, aby jeden na druhého nemusel čekat. Pokud šlo o věci na sebe, mínil George, že nám úplně postačí dva flanelové obleky, protože ty si můžeme sami vyprat v řece, až je ušpiníme. Ptali jsme se ho, jestli už někdy zkoušel vyprat v řece flanelový oblek, a Georte odvětil, že on sám to sice nezkoušel, ale že zná pár mládenců, kteří to zkusili, a že to prý byla hračka. A já s Harrisem j sme si bláhově mysleli, že George ví, o čem mluví, a že si tři úctyhodní mladí muži bez vliveného postavení a bez jakýchkoli zkušeností s praním mohou vskutku kouskem mýdla vyprat vlastní košile a kalhoty v řece Temži. Teprve ve dnech, které byly před námi, v době, kdy už bylo příliš pozdě, jsme se měli dovědět, že George je ničemný podvodník, který o té věci neví zřejmě vůbec nic. Kdybyste ty šaty byli viděli, když jsme je - leč nepředbíhejme, jak se to říká v šestákových krvácích. George nám rovněž kladl na srdce, abychom si vzali dost rezervního spodního prádla a spoustu ponožek pro případ, že bychom se převrhli a potřebovali se převléknout; a taky spoustu kapesníků, ty že se budou hodit jako utěrky, a kromě lehkých veslařských střevíců ještě jedny vysoké botky z pořádné kůže, ty že taky budeme potřebovat, kdybychom se převrhli.

Pak jsme vzali na přetřes otázku stravování. George povídá: „Začneme snídaní.“ (George je velice praktický.) „Tak tedy k snídani budeme potřebovat pánev,“ (Harris řekl, ta že je těžko stravitelná; ale my jsme ho prostě vybídli, aby neblbnul, a George pokračoval) „konvici na čaj, kotlík a lihový vařič.“ „Jen ne petrolej!“ dodal s významným pohledme George a Harris i já jsme přikývli. Jednou jsme si s sebou vzali vařič petrolejový, ale to ,již nikdy více!“ Ten týden jsme měli dojem, jako bychom žili v krámě s petrolejem. Plechovka s tím petrolejem totiž tekla. A když odněkud teče petrolej, to si teda nepřejte. My jsme tu plechovku měli v samé špici lodi, a ten petrolej tekl odtamtud až ke kormidlu a cestou se vsákl do celé lodi a do všeho, co v ní bylo, a natekl do řeky a prostoupil celou scenérii a zamořil ovzduší. Někdy foukal západní petrolejový vítr, jindy východní petrolejový vítr; ale ať už přicházel od sněhů Arktidy nebo se zrodil v nehostinné písečné poušti, k nám vždycky doletěl stejně prosycený vůní petroleje. A náš petrolej tekl dál a poničil i západy slunce; dokonce i měsíční paprsky určitě už zaváněly petrolejem. V Marlow jsme se mu pokusili utéci. Loď jsme nechali u mostu a přes město jsme se vydali pěšky, abychom tomu petroleji unikli; táhl se však za námi. Za chvilku ho bylo plné město. Pustili jsme se přes hřbitov, ale tam zřejmě nebožtíky pochovávali v petroleji. I Hlavní třída smrděla petrolejem; divli jsme se, jak tam lidi dokážou bydlet. A tak jsme ušli kilometry a kilometry po silnici k Birminghamu; ale ani to nebylo nic platné, i ten venkov se koupal v petroleji. Na konci tohohle výletu jsme se o půlnoci všichni sešli na opuštěné pláni pod takovým olysalým dubem a zakleli jsme se strašlivou přísahou (kleli jsme kvůli tomu neřádu samozřejmě celý týden, ale jenom tak normálně, středostavovsky, kdežto teď to byl úkon slavnostní) - zakleli jsme se tedy strašlivou přísahou, že si už jakživi nevezmeme na loď petrolej. Proto jsme i tentokrát dali přednost lihu. Ono i s tím je to dost zlé. To máte denaturátovou sekanou a denaturátové pečivo. Jenomže denaturovaný líh, když ho tělu poskytnete větší množství, je o něco výživnější než petrolej. Jako další pomůcky k snídani navrhoval George vejce a slaninu, což se dá lehce připravit, studená masa, čaj, chléb, máslo a džem. K obědům bychom si prý mohli brát studené pečínky se suchary, chléb s máslem a džemem - nikoli však sýr. Sýr, zrovna tak jako petrolej, se všude cpe do popředí. Chce celou loď jenom pro sebe. Prolézá celým košem s jídlem a všemu ostatnímu v tom koši dává sýrovou příchuť. Těžko uhodnete, jestli jíte jablkový závin nebo párek nebo jahody se šlehačkou. Všecko to připomíná sýr. Sýr má totiž příliš mocný odér. Vzpomínám si, jak jeden můj přítel zakoupil v Liverpoolu páreček sýrů. Byly to výtečné sýry, vyzrále a uležené, s vůní o síle dvou set koní, na niž bylo možno vystavit záruku, že dostřelí do vzdálenosti čtyř a půl kilometru a na vzdálenost dvou set metrů srazí k zemi člověka. Já byl tehdy taky v Liverpoolu a ten můj přítel se mne přišel zeptat, jestli bych byl tak hodný a vzal mu ty sýry s sebou do Londýna, protože on se tam dostane až za den nebo dva, a myslí si, že ty sýry by se už neměly déle skladovat. „Ale milerád, kamaráde,“ odpověděl jsem, „milerád.“ Zašel jsem pro ty sýry k němu a odvážel jsem je drožkou. Byla to taková rachotina na rozsypání, tažená dýchavičným náměsíčníkem s nohama do X, o němž se jeho majitel v okamžiku nadšení během konverzace vyjádřil jako o koni. Sýry jsem položil na střechu kabiny a drožka se rozdrkotala tempem, které by sloužilo ke cti nejsvižnějšímu dosud sestrojenému parnímu válci, a tak jsme jeli, vesele jako s umíráčkem, dokud jsme nezabočili za roh. Tam vítr zanesl aróma sýrů přímo k našemu komoni. To ho probudilo; zděšeně zafrkal a vyrazil vpřed rychlostí pěti kilometrů za hodinu. Vítr stále vanul směrem k němu, a než jsme dorazili na konec ulice, dostal už ze sebe rychlost téměř šestikilometrovou, takže invalidy a tělnaté staré dámy nechával za sebou prostě v nedohlednu. Před nádražím měli co dělat dva nosiči, aby společně s kočím koně zastavili a udrželi na místě; a patrně by se jim to vůbec nebylo podařilo, kdyby jeden z nich nebyl měl tolik duchapřítomnosti, že tomu splašenci zakryl kapesníkem nozdry a honem před ním zapálil kus balicího papíru. Koupil jsme si jízdenku a hrdě jsem se svými sýry kráčel po nástupišti a lidé se přede mnou uctivlě rozestupovali na obě strany. Vlak byl přeplněn a já se musel vecpat do oddělení, kde už bylo sedm cestujících. Jakýsi nerudný starý pán protestoval, ale já jsem se tam přece jen vecpal; sýry jsem dal nahoru do sítě, s vlídným pousmáním jsem se vtěsnal na lavici a prohodil jsem, že je dnes teploučko. Za několik okamžiků začal ten starý pán nervózně poposedat. „Je tu nějak dusno,“ řekl. „Přímo k zalknutí,“ přitakal jeho soused. Pak oba začali čenichat, při třetím začenichání uhodili hřebík na hlavičku, zvedli se a bez dalších slov odešli. Pak vstala jakási korpulentní dáma, prohlásila, že takovéto týrání počestné vdané ženy považuje prostě za hanebnost, sebrala kufr a osm balíků a rovněž odešla. Zbývající čtyři cestující poseděli až do chvíle, kdy slavnostně vypadající muž, sedící v koutě a oblečením a celkovým vzezřením vzbuzující dojem, že je příslušníkem kasty majitelů pohřebních ústavů, poznamenal, že mu to jaksi připomíná zesnulé batole; pak se ti tři ostatní cestující pokusili, všichni současně, vyrazit ze dveří a vzájemně se zranili. Usmál jsem se na toho černého pána a poznamenal jsem, že teď zřejmě budeme mít celé kupé pro sebe; a on se bodře zasmál a řekl, že někteří lidé nadělají spousty cavyků kvůli úplným maličkostem. Ale jen jsme vyjeli, začala i na něho padat podivná sklíčenost, a tak když jsme dorazili do Crewe, pozval jsem ho, aby se se mnou šel napít. Přijal, probojovali jsme se tudíž k bufetu, a tam jsme čtvrt hodiny pokřikovali a dupali a mávali deštníky, načež přišla nějaká slečna a otázala se nás, jestli snad něco nechceme. „Co vy si dáte?“ zeptal jsem se, obraceje se k svému příteli. „Velký koňak, slečno. A bez sodovky, prosím,“ odvětil. A když ho vypil, klidně odešel a nastoupil do jiného vagónu, což mi připadalo nevychované. Od Crewe jsem měl celé kupé pro sebe, přestože vlak byl stále přeplněn. Na každém nádraží, kde jsme zastavili, si lidé toho prázdného oddělení všimli a hnali se k němu. „No vidíš, Marie! Pojď, tadyhle je místa dost.“ „Dobrá, Tome, nastoupíme sem,“ volali na sebe. A přiběhli s těžkými kufry a přede dveřmi se vždycky prali o to, kdo vleze dovnitř první. A pak vždycky jeden ty dveře otevřel, vystoupil po schůdkách a vyvrávoral pozpátku do náruče toho, co byl za ním. A tak vstupovali jeden po druhém každý začenichal a odpotácel se ven a vtěsnal se do jiného oddělení nebo si doplatil na první třídu. Z eustonského nádraží jsem sýry dopravil do domu svého přítele. Jeho žena, když vstoupila do pokoje, chviličku čichala a pak řekla: „Co to je? Nešetřete mě, povězte mi rovnou to nejhorší.“ „Sýry jsou to,“ odvětil jsem. „Tom si je koupil v Liverpoolu a požádal mě, abych mu je sem zavezl.“ A dodal jsem, že ona, jak doufám pochopí, že já jsem v tom nevinně, a ona řekla, že tím je si jista, ale až se vrátí Tom, že si s ním pohovoří. Tom se musle v Liverpoolu zdržet déle, než očekával, a když se ani třetího dne ještě nevrátil domů, přišla jeho žena ke mně. „Co Tom o těch sýrech říkal?“ zeptala se. Odvětil jsem, že nařídil, aby byly uchovány někde ve vlhku a aby na ně nikdo nesahal. „Nikoho ani nenapadne, aby na ně sahal,“ řekla. „Přivoněl si k nim vůbec?“ Pravil jsem, že pravděpodobně ano, a dodal jsem, že mu na nich zřejmě nesmírně záleželo. „Takže myslíte, že by ho mrzelo,“ vyzvídala, „kdybych někomu dala zlatku, aby je někam odnesl a zakopal?“ Odpověděl jsem, že by se pravděpodobně už nikdy neusmál. Tu dostala nápad. A řekla: „A co kdybyste mu je vzal do opatrování vy? Dovolte, abych je poslala k vám.“ „Madam,“ odvětil jsem, ,já osobně mám vůni sýrů rád a na tok jak jsme s nimi tuhle cestoval z Liverpoolu, budu vždycky vzpomínat jako na šťastné zakončení příjemné dovolené. Leč na tomto světě musíme brát ohled na ostatní. Dáma, pod jejíž střechou mám čest přebývat, je vdova, a pokud je mi známo, patrně též sirota. Ta dáma má mocný, řekl bych až výmluvný odpor k tomu, aby ji někdo, jak to ona sama formuluje, »dožíral«. Přítomnost sýrů vašeho manžela ve svém bytě by určitě, to instinktivně cítím, považovala za »dožírání«. A o mně se bohdá nikdy neřekne, že jsem dožíral vdovu a sirotu.“ „No dobrá,“ řekla žena mého přítele povstávajíc, „pak mohu říci pouze tolik, že seberu děti a budeme bydlet v hotelu, dokud ty sýry někdo nesní. Žít společně s nimi pod jednou střechou, to prostě odmítám.“ Dodržela slovo a domácnost přenechala v péči své posluhovačky, která na otázku, jestli ten zápach přežije, odpověděla „Jakej zápach?“ a která, když ji přivedli až těsně k těm sýrům a řekli jí, aby si k nim pořádně přičichla, prohlásila, že rozeznává slabounkou vůni melounů. Z toho bylo možno usoudit, že této ženě ovzduší bytu nijak zvlášť neublíží, a byla tam tedy ponechána. Účet za hotel činil patnáct guinejí, a když můj přítel spočítal všechno dohromady, zjistil, že jeho ty sýry přišly na osm a půl šilinku za půl kila. Pravil, že s nesmírnou chutí sní kousek sýra, tohle že je však nad jeho prostředky. A tak se rozhodl, že se těch dvou sýrů zbaví, a hodil je do jednoho průplavu u doků; ale musle je zase vylovit, ježto chlapi z nákladních bárek si stěžovali; prý jim z nich bylo na omdlení. Potom je jedné temné noci odnesl na místní hřbitov a nechal je tam v márnici. Ale přišel na ně ohledač mrtvol a ztropil pekelný randál. Křičel, že to je úklad, jehož cílem je vzkřísit mrtvoly a jeho tak připravit o živobytí. Nakonec se můj přítel těch sýrů zbavil tím, že je odvezl do jednoho přímožského města a tam je zakopal na pobřeží. A tím tomu místu zajistil značnou proslulost. Návštěvníci se najednou začali divit, proč si už dřív nepovšimli, jak silný je tam vzuch, a ještě řadu let potom se tam houfně hrnuli souchotináři a vůbec lidé slabí na prsa. A proto jsem i já, ačkoli sýry velice rád, dal Georgeovi za pravdu, když je odmítl vzít s sebou. „Taky si odpustíme odpolední svačinu,“ dodal George (načež Harris protáhl obličej), „ale zato si pravidelně v sedm dáme denně pořádnou, vydatnou baštu - prostě svačinu, večeři a sousto na noc v jednom.“ Harris hned vypadal veseleji. George navrhoval masové a jablečné záviny, šunku, studené pečínky, rajčata a hlávkový a okurkový salát a ovoce. K pití jsme si s sebou brali jakousi báječnou lepkavou míchanici podle Harrisova receptu, které, když se rozředí vodou, se dá říkat limonáda, spousty čaje a láhev whisky, pro případ, jak pravil George, že bychom se převrhli.

Podle mého názoru se George k té představě, že se můžeme převrhnout, vracel zbytečně často. To přece nebyl ten pravý duch, jaký měl vládnout našim cestovním přípravám. Ale že jsme s sebou měli tu whisky, to jsem dodnes rád. Pivo ani víno jsme si nebrali. To není dobré na řeku. Pak jste ospalí a máte těžké nohy. Takhle navečer, když se jen tak poflakujete po městě a koukáte po holkách, to je nějaká ta sklenička na místě; ale když vám na hlavu praží slunce a čeká vás tvrdá dřina, tak nepijte. Sepisovali jsme seznam všeho, co se má vzít, a byl z toho seznam hezky dlouhý, než jsme se toho večera rozešli. Nazítří, to byl pátek, jsme to všechno nanosili ke mně a večer jsme se sešli, abychom to zapakovali. Na šatstvo jsme měli obrovský kožený kufr a na poživatiny a kuchařské náčiní dva koše. Stůl jsme odstrčili k oknu, všecky ty věci jsme snesli na jednu hromadu doprostřed pokoje, sedli jsme si kolem té hromady a dívali se na ni. Já jsem řekl, že to zapakuju sám. Na to, jak umím pakovat, jsem dost pyšný. Pakování je jedna z těch mnoha věcí, v nichž se vyznám líp než kterýkoli žijící člověk. (Někdy mě samotného překvapí, jak mnoho těch věcí je.) Důtklivě jsem George i Harrise na tuto skutečnost upozornil a radil jsem jim, aby to všechno nechali na mně. Skočili na ten návrh s ochotou, která se mi hned nějak nezamlouvala. George si nacpal dýmku a rozvalil se v lenošce a Harris si dal nohy na stůl a zapálil si doutník. Ttakhle jsem to ovšem nemínil. Představoval jsem si to - samozřejmě - tak, že já budu při té práci vrchním dohlížitelem a Harris a George že se budou podle mých direktiv motat sem a tam a já že je každou chvíli odstrčím s takovým tím „Jdi ty...!“ nebo „Pusť, já to udělám sám,“ nebo „No prosím, vždyť to není nic těžkého!“ - prostě že je budu tak říkajíc učit. Že si to vyložili tak, jak si to vyložili, to mě popudilo. Nic mě totiž tolik nepopudí, jako když si ti druzí jenom sedí a nic nedělají, zatímco já pracuju. Jednou jsem bydlel s člověkem, který mě tímhle způsobem doháněl skoro k šílenství. Vždycky když jsem něco dělal, on se povaloval na pohovce a koukal na mě; celé hodiny mě vydržel sledovat očima, ať jsem se v tom pokoji kamkoli pohnul. Říkal, že mu dělá úžasně dobře, když se může dívat, jak pilně markýruju práci. Prý mu to dokazuje, že život není jen jalové snění, určené k prozevlování a prozívání, ale vznešené poslání, plné povinností a tvrdé práce. Často už prý si kladl otázku, jak mohl vůbec existovat, než se seznámil se mnou, když neměl nikoho, koho by mohl pozorovat při práci. Tak takovýhle já nejsem. Já nevydržím klidně sedět, když vidím, jak se někdo jiný otrocky lopotí. To musím vstát a dozírat na něj, obcházet ho s rukama v kapsách a radit mu, jak na to. To dělá ta moje činorodá povaha. Tomu se prostě neubráním. Přesto jsem neřekl ani slovo a dal jsem se do toho pakování. Dalo to víc práce, než jsem předpokládal, ale posléze jsem byl hotov s kufrem, sedl jsem si na něj a stáhl jsem ho řemeny. „A co boty? Ty tam nedáš?“ zeptal se Harris. Rozhlédl jsem se a zjistil, že na boty jsem zapomněl. To je celý Harris. Ani necekne samozřejmě, dokud kufr nezavřu a nestáhnu ho řemeny. A George se smál - tím svým popuzujícím, bezdůvodným, přiblblým chechotem, při kterém mu huba div nevyletí z pantů a který mě pokaždé rozzuří. Otevřel jsem kufr a přibalil boty; a potom, právě když jsem se chystal kufr zase zavřít, napadla mě strašlivá myšlenka. Dal jsem tam svůj kartáček na zuby? Nevím, čím to je, ale já vážně nikdy nevím, jestli jsem si dal do kufru kartáček na zuby. Můj kartáček na zuby, to je věc, která mě při každém cestování v jednom kuse straší a proměňuje mi život v peklo. V noci se mi zdá, že jsem si ho nezapakoval, probudím se zborcen studeným potem, vyskočím z postele a sháním se po kartáčku. A ráno ho zapakuju dřív, než si vyčistím zuby, takže ho zase musím vypakovat, a vždycky ho najdu, až když vytahám z kufru všecko ostatní; pak musím zapakovat znova a na kartáček zapomenu a v posledním okamžiku pro něj pádím nahoru a na nádraží ho vezu v kapse, zabalený do kapesníku. I teď jsem samozřejmě musel všecko do poslední mrtě zase vypakovat, ale kartáček, samozřejmě, ne a ne najít. Celý obsah kufru jsem vyházel a tak důkladně zpřeházel, že to u mne vypadalo přibližně jako před stvořením světa, když ještě vládl chaos. Georgeův a Harrisův kartáček jsem měl v ruce, samozřejmě! aspoň osmnáctkrát, ale ten svůj jsem nenašel. Tak jsem všecky ty věci házel zpátky do kufru, jednu po druhé, každou jsem zvedl a protřepal. A ten kartáček j sem našel v j edné botě. A pakoval j sem znova. Když jsem s tím byl hotov, zeptal se George, jestli je tam mýdlo. Řekl jsem mu, na tom že mi pendrek záleží, jestli tam je nebo není mýdlo, kufr jsem zabouchl a stáhl řemeny a zjistil jsem, že jsem si do něho dal pytlík s tabákem a musel jsem ho nanovo otevřít. Definitvně byl zavřen v 10,05 a ještě zbývalo zapakovat koše. Harris poznamenal, že budeme chtít vyrazit už za necelých dvanáct hodin, a že by tedy snad bylo lepší, kdyby si to ostatní vzal na starost on s Georgem; s tím jsem souhlasil, posadil jsem se a do práce se dali oni. Začali s veselou myslí očividně v úmyslu předvést mi, jak se to dělá. Já se zdržel poznámek; prostě jsem čekal. Až milého George pověsí, nej horším pakovačem na tomto světě bude Harris. Díval jsem se na ty stohy talířů a šálků a konvic a lahví a sklenic a konzerv a vařičů a pečiva a rajčat, atd., a tušil jsem, že brzy to bude vzrušující. A bylo. Zahájili to tím, že rozbili jeden šálek. To bylo první, co udělali. A udělali to jenom proto, aby ukázali, co všechno by udělat dovedli, a aby vzbudili zájem. Pak Harris postavil sklenici s jahodovým džemem rovnou na jedno rajče a rozmačkal je, takže je museli z koše vybírat lžičkou. Pak přišel na řadu George, a ten dupl na máslo. Já nic neřekl, ale přistoupil jsem blíž, sedl si na kraj stolu a pozoroval je. To je popouzelo mnohem víc než všechno, co bych byl mohl říci. To jsem vycítil. Byli ze mne nervózní a rozčilení, na všecko šlapali, všecko, co vzali do ruky, hned někam zašantročili, a když to pak potřebovali, tak to nemohli najít; a nákyp dali do koše na dno a těžké předměty navrch, takže z nákypu udělali kaši. Sůl, tu rozsypali prostě do všeho - a to máslo? V životě jsem si nedovedl představit, že by se dva chlapi dovedli tolik vydovádět s kouskem másla za jeden šilink a dvě pence. Když si je George odlepil z pantofle, chtěli je nacpat do konvičky na mléko. Tam samozřejmě nešlo, a to, co tam z něho přece jen šlo, to zase nešlo ven. Nakonec je kupodivu vyškrábali a odložili je na židli, a tam si na ně sedl Harris a máslo se k němu přilíplo a oba šmejdili po celém pokoji a pátrali, kam se podělo. „Já bych přísahal, že jsem ho dal tuhle na židli,“ pravil George, zíraje na prázdné sedadlo. „No jo, já sám jsem viděl, jak ho tam dáváš, to není ani minuta,“ přikyvoval Harris. A znova se vydali na obhlídku celého pokoje a znova se sešli uprostřed a civěli jeden na druhého. „To teda je ta nejfantastičtější věc, jakou jsem kdy zažil,“ prohlásil George. „Úplná záhada!“ dodal Harris. Pak se najednou George octl Harrisovi za zády a to máslo uviděl. „Prosím tě, vždyť jeho celou tu dobu máš tadyhle!“ zvolal rozhořčeně. „Kde?“ zařval Harris a začal se točit dokolečka. „Copak ty nedovedeš chviličku klidně postát?“ hřměl George, lítaje za ním. Nakonec to máslo seškrábali a nandali je do čajové konvice. Všeho toho se, přirozeně, zúčastnil i Montmorency. Montmorencyho životní ctižádostí je totiž plést se lidem pod nohy a provokovat je k nadávkám. Když se mu podaří vklouznout někam, kde je obzvlášť nežádoucí, kde příšerně překáží, kde lidi rozzuří do té míry, že po něm házejí vším, co je po ruce, pak teprve má pocit, že nepromarnil den. Jeho nejvyšší metou a touhou je docílit, aby se přes něj někdo přerazil a pak ho nepřetržitě po celou hodinu proklínal; to když se mu povede, pak je jeho domýšlivost naprosto nesnesitelná. A tak se vždycky běžel posadit na to, co George a Harris chtěli zrovna zapakovat, a řídil se utkvělou představou, že kdykoli ti dva vztahujou po něčem ruku, vztahujou ji po jeho studeném, vlhkém čumáku. Tlapky strkal do džemů, sápal se na lžičky a pak si začal hrát, že citróny jsou myši, skočil do jednoho koše a tři ty myši zakousl, dřív než se Harrisovi podažilo uzemnit ho železnou pánví. Harris tvrdil, že já jsem ho k tomu všemu podněcoval. Nikoli, nepodněcoval jsem ho. Takový pes žádné podněcování nepotřebuje. Aby takhle jančil, k tomu ho podněcuje vrozený prvotní hřích, s kterým už přišel na svět. Pakování skončilo ve 12.50. Harris seděl na tom větším koši a vyslovil naději, že v něm nic nebude rozbité. A George řekl, že jestli v něm něco rozbité bude, tak to je rozbité už teď, a tato úvaha ho zjevně uklidnila. A dodal, že je zralý pro postel. Harris měl té noci spát u nás, a tak jsme šli všichni nahoru do ložnice. Házeli jsme šilinkem, kdo s kým bude sdílet lože, a Harrisovi vyšlo, že má spát se mnou. „Chceš spát radši u zdi nebo dál ode zdi?“ zeptal se mne. Odpověděl jsem, to že je mi jedno, jen když to bude v posteli. A Harris řekl, že to je fousaté. „A kdy vás mám vzbudit, mládenci?“ tázal se George. Haris řekl: „V sedm.“ A já řekl „Ne - v šest!“, protože jsem ještě chtěl napsat pár dopisů. Kvůli tomu j sme se já a Harris chvilku hádali, ale nakonec j sme si vyšli na půl cesty vstříc a dohodli j sme se na půl sedmé. „Vzbuď nás v šest třicet, Georgi,“ řekli jsme. George nám na to neodpověděl, a když jsme k němu přistoupili, zjistili jsme, že už spí; a tak jsme mu k posteli postavili vaničku s vodou, aby do ní skočil, až bude ráno vstávat, a šli jsme taky spat.

Ale byla to paní Poppetsová, kdo mě ráno vzbudil. „Jestlipak víte, pane, že už je skoro devět hodin?“ volala. „Devět čeho?“ vykřikl jsem, vyskakuje z postele. „Devět hodin,“ odvětila skrze klíčovou dírku. „Říkám si, že jste asi zaspali.“ Vzbudil jsem Harrise a řekl jsem mu, co se stalo. „Já myslel, že tys chtěl vstávat v šest,“ pravil Harris. „Taky že chtěl,“ odpověděl jsem. „Proč jsi mě nevzbudil?“ „Jak jsem tě mohl vzbudit, když jsi ty nevzbudil mě,“ odsekl. „A teď se už stejně nedostaneme na vodu dřív než někdy po dvanácté. Tak nechápu, proč vůbec lezeš z postele.“ „Ty buď rád, že z ní lezu,“ odvětil jsem. „Kdybych tě neprobudil, tak tu budeš takhle ležet celých těch čtrnáct dní.“ Tímto tónem jsme na sebe ňafali už pět minut, když nás přerušilo vyzývavé chrápání Georgeovo. Tím jsme si poprvé od okamžiku, kdy jsme byli vyburcováni ze spánku, uvědomili jeho existenci. A on si tam ležel - ten chlap, který chtěl vědět, v kolik hodin nás má vzbudit - na zádech, s hubou dokořán a s koleny nahoru. Nevím, čím to je, to na mou duši nevím, ale pohled na někoho jiného, jak si spí v posteli, zatímco já jsem na nohou, mě vždycky rozzuří. To na mě působí přímo otřesně, když musím přihlížet, jak někdo dokáže drahocenné hodiny života - ty okamžiky k nezaplacení, které se mu nikdy nenavrátí - promarňovat jako nějaké hovado jen a jen spánkem. No prosím! - a takhle tam George v odporném lenošení zahazoval ten neocenitelný dar času; cenný život, z jehož každé vteřiny bude jednou muset skládat počet, mu ubíhal nevyužit. Mohl se cpát slaninou s vejci, pošťuchovat psa nebo koketovat s naší služtičkou, místo aby se takto povaloval, zapadlý do duchamorného nevědomí. To bylo strašlivé pomyšlení. A zasáhlo zřejmě Harrise i mne současně. Rozhodli jsme se, že ho zachráníme, a pro toto ušlechilé předsevzetí jsme rázem zapomněli i na svou vlastní rozepři. Vrhli jsme se k Georgeovi, servali s něho deku, Harris mu jednu přišil pantoflem a já mu zařval do ucha a George se probudil. „Coseděé?“ pravil a posadil se. „Vstávej, ty kládo jedna slabomyslná!“ zaburácel Harris. „Je čtvrt na deset!“ „Co?“ zavřeštěl George a vyskočil z postele do vaničky. - „Kdo to sem ksakru postavil?“ Řekli jsme mu, že musel pořádně zblbnout, když už nevidí ani vaničku. Dooblékli jsme se, a když došlo na konečnou úpravu zevnějšku, vzpomněli jsme si, že jsme si už zapakovali kartáčky na zuby, kartáč na vlasy a hřeben (ten můj kartáček na zuby, to bude jednou moje smrt, to já vím), a museli jsme jít dolů všecko to z kufru vylovit. A když jsme to konečně dokázali, chtěl George holicí náčiní. Řekli jsme mu, že dneska se musí objeít bez holení, jelikož kvůli němu ani kvůli komukoli jinému už ten kufr přepakovávat nebudeme. „Mějte rozum,“ povídá. „Copak můžu jít do banky takhle?“ To nesporně byla vůči celé bankovní čtvrti surovost, co nám však bylo do útrap lidstva? Bankovní čtvrť, jak to svým běžným drsným způsobem vyjádřil Harris, to bude muset spolknout. Sešli jsme dolů k snídani. Montmorendy si pozval dva jiné psy, aby se s ním přišli rozloučit, a ti si zrovna krátili čas tím, že se rvali před domovními dveřmi. Uklidnili jsme je deštníkem a zasedli jsme ke kotletám a studenému hovězímu. „Pořádně se nasnídat, to je věc velice důležitá,“ pravil Harris a začal s dvěma kotletami; ty je prý nutno jíst, dokud jsou teplé, hovězí, to prý počká. George se zmocnil novin a předčítal nám zprávy o neštěstích na řece a předpověď počasí, kterážto předpověď prorokovala „zamračeno, chladno, proměnlivo až deštivo,“ (prostě ještě příšernější počasí, než bývá obvykle), „občasné místní bouřky, východní vítr, celková deprese nad hrabstvími střední Anglie (Londýn a Kanál); barometr klesá.“ Já si přece jen myslím, že ze všech těch pitomých, popuzujících šaškáren, jimiž jsme sužováni, jde tenhle podvod s „předpovídáním počasí“ na nervy nejvíc. Vždycky „předpovídá“ přesně to, co bylo včera nebo předevčírem, a přesně opak toho, co se chystá dnes. Vzpomínám si, jak jsme si jednou na sklonku podzimu úplně zkazili dovolenou tím, že jsme se řídili povětrnostními zprávami v místních novinách. „Dnes jest očekávati vydatné přeháňky a bouřky,“ stálo tam třeba v pondělí, a tak jsme se vzdali plánovaného pikniku v přírodě a celý den jsme trčeli doma a čekali, kdy začne pršet. Kolem našeho dou proudili výletníci, v kočárech i omnibusech, všichni v té nejlepší, nejveselejší náladě neboť svítilo sluníčko a nikde nebylo vidět mráček. „Á jé,“ říkali jsme si, koukajíce na ně z okna, „ti přijedou domů promočení!“ Chichtali jsme se při pomyšlení, jak zmoknou, odstoupili jsme od okna, prohrábli oheň a vzali si knížky a přerovnávali sbírečku mořských chaluh a mušliček. K poledni, když jsme div nepadli horkem, jak nám do pokoje pražilo slunce, jsme si kladli otázku, kdy asi spustí ty prudké přeháňky a občasné bouřky. „Všecko to přijde odpoledne, uvidíte,“ říkali jsme si. „A ti tak promoknou, ti lidé, to bude taková legrace!“ V jednu hodinu se nás naše domácí přišla zeptat, jestli nepůjdeme ven, když je tak překrásný den. „Kdepak, kdepak,“ odvětili jsme s mazaným uchichtnutím. „My promoknout nechceme. Kdepak.“ A když se odpoledne chýlilo ke konci a pořád to ještě nevypadalo na déšť, snažili jsme se zvednout si náladu představou, že to spadne zčistajasna, až lidi už budou na cestě domů a nebudou se mít kam schovat, takže na nich nezůstane suchá ani nitka. Leč nespadla ani kapička a celý den se vydařil a přišla po něm líbezná noc. Nazítří jsme si přečetli, že bude „suché, pěkné, ustalující se počasí, značné horko“; vzali jsme si na sebe své nejlehčí hadříky a vyrazili jsme do přírody a za půl hodiny se přihnal hustý liják, zvedl se hnusně studený vítr a to obojí trvalo bez ustání celý den a my jsme se vrátili domů s rýmou a s revmatismem v celém těle a hned jsme museli do postelí. Počasí, to je prostě něco, nač vůbec nestačím. Jaktěživ se v něm nevyznám. A barometr je k ničemu; to je zrovna taková šalba jako ty novinářské předpovědi. Jeden visel v jistém oxfordském hotelu, kde jsem byl ubytován loni na jaře, a když jsem tam přišel, tak ukazoval na „ustáleně pěkně“. A venku pršelo, jen se lilo; celý den; nedovedl jsem si to vysvětlit. Zaťukal jsem na ten barometr a ručička poskočila a ukázala na „velmi sucho“. Zrovna šel kolem kluk, co tam čistil boty, zastavil se u mě a povídá, že to asi znamená, jak bude zítra. Já spíš soudil, že to ukazuje, jak bylo předminulý týden, ale ten kluk povídá, to že asi ne. Příští ráno jsem na ten barometr zaťukal znova a ručička vyletěla ještě výš a venku cedilo jako snad nikdy. Ve středu jsem do něj šel šťouchnut zase a ručička běžela přes „ustáleně pěkně “, „velmi sucho“ a „vedro “, až se zarazila o takový ten čudlík a dál už nemohla. Dělala, co mohla, ale ten aparát byl sestrojen tak, že důrazněji už pěkné počasí prorokovat nemohl, to už by se byl pochroumal. Ručička očividně chtěla běžet dál a udělat prognózu na katastrofální sucha a vypaření všeho vodstva a sluneční úžehy a písečné vichřice a podobné věci, ale ten čudlík jí v tom zabránil, takže se musela spokojit s tím, že ukázala na to prosté, obyčejné „vedro“. A venku zatím vytrvale lilo jako z konve a níže položená část města byla pod vodou, neboť se rozvodnila řeka. Čistič obuvi viděl v chování barometru zjevný příslib, že někdy později se nám nádherné počasí udrží po velmi dlouhou dobu, a nahlas mi přečetl veršovánku vytištěnou nad tím orákulem, něco jako: Dlouho slibované trvá dlouze; narychlo oznámené přeletí pouze. Toho léta se pěkné počasí nedostavilo vůbec. Počítám, že ten přístroj se zmiňoval až o jaru příštího roku. Pak jsou ty nové barometry, ty dlouhé, jako tyčky. Z těch už jsem teda úplný jelen. Tam se na jedné straně ukazuje stav v deset hodin dopoledne včera a na druhé straně stav v deset hodin dopoledne dnes; jenže přivstat si tak, abyste to šel kontrolovat zrovna v deset, se člověku nepodaří, to dá rozum. Klesá a stoupá to tam na déšť nebo na pěkně, na silný vítr nebo na slabý vítr, na jednom konci je MIN a na druhém MAX (nemám ponětí, který Max to má být), a když na to zaťukáte, tak vám to vůbec neodpoví. A ještě si to musíte přepočítat na příslušnou výšku nad mořem a převést na Fahrenheita, a já ani potom nevím, co mě vlastně čeká. Ale kdo vlastně touží po tom, aby se mu předpovídalo počasí? Když ten nečas přijde, tak je to protivné až dost, a aspoň jsme z toho neměli mizernou náladu už napřed. Prorok, jakého máme rádi, to je ten stařeček, který se toho obzvlášť ponurého rána, kdy si obzvlášť vroucně přejeme, aby byl krásný den, rozhlédne obzvlášť znaleckým zrakem po obzoru a řekne: „Ale kdepák, pane, já počítám, že se to vybere. To se určitě protrhá, pane.“ „Ten se v tom hlot vyzná,“ říkáme si, popřejeme mu dobré jitro a vyrážíme. „Ohromná věc, jak tomu tihle staroušci rozumějí.“ A pociťujeme k tomu člověku náklonnost, kterou nikterak neoslabí fakt, že se to nevybralo a že celý den nepřetržitě leje. „Inu,“ myslíme si, „aspoň se snažil.“ Zatímco vůči člověku, který nám prorokoval špatné počasí, chováme jen myšlenky trpké a pomstychtivé. „Vybere se to, co myslíte?“ voláme bodře, když jdeme kolem něho. „Ne ne, pane. Dneska to bohužel celej den nebude stát za nic,“ kroutí hlavou ten člověk. „Dědek jeden pitomá!“ bručíme. Co ten o tom může vědět?“ A když na jeho zlověstnou předtuchu dojde, vracíme se s pocity ještě větší zloby vůči němu a s takovým mlhavým dojmem, že to nějak spískal on. Toho dotyčného rána bylo příliš jasno a slunečno, aby nás George mohl nějak znepokojit, když nám tónem, při němž měla stydnout krev, předčítal, jak „barometr klesá“, jak „atmosférické proruchy postupují nad jižní Evropou šikmo k severu,“ a jak se „přibližuje oblast nízkého tlaku“. Když tedy zjistil, že nás nemůže uvrhnout v zoufalství a že zbytečně maří čas, šlohnul mi cigaretu, kteru jsem si zrovna pečlivě ukroutil, a šel do banky. Harris a já jsme dojedli to málo, co George nechal na stole, a pak jsme vyvlekli před dům svá zavazadla a vyhlíželi jsme drožku. Těch zavazadel bylo dost, když jsme je snesli na jednu hromadu. Měli jsme ten veliký kufr a jeden menší kufřík, příruční, a ty dva koše a velikánskou roli dek a asi tak čtvero nebo patero svrchníků a pršáků a několik deštníků a pak meloun, který měl jednu tašku jenom pro sebe, protože ho byl takový kus, že se nikam jinam nevešel, a ještě pár kilo hroznů v další tašce a takové japonské papírové paraple a pánev, která byla tak dlouhá, že jsme ji nemohli k ničemu připakovat, a tak jsme ji jen tak zabalili do papíru. No, byla toho pořádná kupa a Harris a já jsme se za ni začali jaksi stydět, i když teď nechápu, proč vlastně. Drožka se neobjevovala, zato se objevovali četní uličníci, kterým jsme zřejmě skýtali zajímavou podívanou, a tak se stavěli kolem nás. První se přiloudal ten kluk od Biggse. Biggs je náš zelinář a má zvláštní talent zaměstnávat ty nej sprostší a nejzpustlejší učedníky, jaké civilizace dosud zplodila. Když se v našem okolí vyskytne něco až neobvykle zlotřilého v klukovském provedení, hned víme, žte je to Biggsův nejnovější učedník. Po té vraždě v Great Coram Street došla prý naše ulice okamžitě k závěru, že v tom má prsty Biggsův učedník (ten tehdejší), a kdyby se mu při přísném křížovém výslechu, kterému ho podrobilo číslo 1 9, k němuž ráno po tom zločinu zaskočil pro objednávku (číslu 1 9 asistovalo číslo 21 , protože náhodou stálo zrovna před domem), kdyby se mu tedy bylo nepodařilo prokázat při tom výslechu dokonalé alibi, bylo by to s ním dopadlo moc špatně. Já jsem toho tehdejšího Biggsova učedníka neznal, ale soudě podle toho, co vím o všech těch dalších, sám bych byl tomu alibi velký význam nepřikládal. Jak už jsem tedy řekl, za rohem se ukázal Biggsův učedník. Když se prvně zjevil v dohledu, měl očividně veliký spěch, ale jakmile si všiml Harrise a mne a Montmorencyho a našich věcí, zpomalil svůj běh a vypoulil oči. Harris i já jsme se na něho zaškaredili. Jen poněkud citlivé povahy by se to bylo dotklo, avšak Biggsovi učedníci nejsou obyčejně žádné netykavky. Ani ne metr od našich schůdků se ten kluk zastavil, opře se o železnou tyč v plotě, pečlivě si vybral stéblo trávy, které by se dalo cucat, a začal nás upřeně pozorovat. Zřejmě se mínil dožít toho, jak to s námi dopadne. Za chvilku šel na protějším chodníku učedník hokynářův. Biggsův kluk na něj zavolal: „Ahoj! Přízemí z dvaačtyřicítky se stěhuje.“ Hokynářův učedník přešel přes ulici a zaujal postavení na druhé straně našich schůdků. Pak se u nás zastavil mladý pán od obchodníka s obuví a připojil se k učedníkovi od Biggse, zatímco vrchní dohlížitel na čistotu vyprzádněných pohárů od „Modrých sloupů“ zaujal postavení zcela samostatné, a to na obrubě chodníku. „Hlad teda mít nebudou, co?“ řekl pán z krámu s obuví. „Hele, ty by sis taky vzal s sebou jednu nebo dvě věci,“ namítly Modré sloupy, „dyby ses v malej lodičce chystal přes Atlantickej oceán.“ „Ty se neplavěj přes oceán,“ vložil se do toho kluk od Biggse, „ty jedou hledat Stanleye.“ Tou dobou se už shromážidl slušný hlouček a lidi se jeden druhého ptali, co se to děje. Jedna skupina (ta mladší a frivolnější část hloučku) trvala na tom, že je to svatba, a za ženicha označovali Harrise; zatímco ti starší a uvážlivější v tom lidu se přikláněli k názoru, že jde o pohřeb a já že jsem patrně bratr mrtvoly. Konečně se vynořila prázdná drožka (je to ulice, kterou zpravidla - když je nikdo nepotřebuje - projíždějí tři prázné drožky za minutu, postávají a pletou se vám do cesty), i naskládali jsme do ní sebe i své příslušenství, vyhodili jsme z ní pár Montmorencyho kamarádů, kteří se mu zřejmě zapřisáhli, že ho nikdy neopustí, a odjížděli jsme; dav nám provolával slávu a Biggsův učedník po nás pro štěstí hodil mrkví. Na nádraží Waterloo jsme dorazili v jedenáct a vyptávali jsme se, odkud vyjíždí vlak v 11,05. To pochopitelně nikdo nevěděl; na tomhle nádraží nikdy nikdo neví, odkud který vlak vyjíždí, nebo kam který vlak, když už odněkud vyjíždí, dojíždí, no tam prostě nikdy nikdo neví nic. Nosič, který se ujal našich zavazadel, měl za to, že náš vlak jede z nástupiště číslo dvě, avšak jiný nosič, s kterým jsme o tom problému rovněž diskutovali, prý někde zaslechl cosi o nástupišti číslo jedna. Přednosta stanice byl však naproti tomu přesvědčen, že ten vlak vyjede z nástupiště pro vnitrolondýnskou dopravu. Abychom se to dověděli s konečnou platností, šli jsme nahoru a zeptali jsme se hlavního výpravčího, a ten nám řekl, že zrovna potkal nějakého člověka, který se zmínil, že ten vlak viděl na nástupišti číslo tři. Odebrali jsme se na nástupiště číslo tři, ale tam se příslušní činitelé domnívali, že ten jejich vlak je expres do Southamptonu nebo taky možná lokálka do Windsoru. Ale že to není vlak do Kongstonu, to věděli naprosto jistě, i když nevěděli jistě, proč to vědí tak jistě. Pak řekl náš nosič, že náš vlak bude nejspíš ten vlak na zvýšeném nástupišti; prý ho zná, ten náš vlak. Tak jsme šli na zvýšené nástupiště a ptali jsme se přímo strojvedoucího, jestli jede do Kongstonu. Pravil, že s jistotou to pochopitelně tvrdit nemůže, ale že si myslí, že tam jede. A i kdyby ten jeho vlak nebyl ten v 11.05 do Kingstonu, tak prý dost pevně věří, že je to vlak v 9,32 do Virginia Water, nebo rychlík v 10.00 na ostrov Wight, anebo prostě někam tím směrem, což prý bezpečně poznáme, až tam dojedeme. Strčili jsme mu do dlaně půlkorunu a prosili jsme ho, ať z toho udělá ten v 11.05 do Kingstonu. „Na téhle trati stejně nikdo neví, co jste za vlak a kam jedete,“ řekli jsme mu. „Cestu jistě znáte, tak odtud potichounku vyklouzněte a vemte to na Kingston.“ „No, já teda nevím, páni,“ odvětil ten šlechetný muž, „ale předpokládám, že nějakej vlak do Kingstonu jet musí, tak tam teda pojedu já. Tu půlkorunu mi klidně nechte.“ Takto jsme se Londýnskou a Jihozápadní dráhou dostali do Kingstonu. Později jsme se dověděli, že ten vlak, co jsme s ním jeli, byl ve skutečnosti poštovní vlak do Exeteru a že ho na nádraží Waterloo hodiny a hodiny hledali a nikdo nevěděl, co se s ním stalo. Naše loď na nás čekala v Kingstonu přímo pod mostem, k ní j sme tedy zameřili své kroky, na ni j sme naskládali svá zavazadla a do ní jsme posléze vstoupili. „Nechybí vám něco, páni?“ ptal se muž, co mu loď patřila. „Ne, nic tu nechybí,“ odpověděli jsme a pak jsme, Harris u vesel, já u kormidla a Montmorency, nešťastný a hluboce nedůvěřivý, na přídi, odrazili na vody, jež měly být čtrnáct dní naším domovem. ";
            }
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
„Kdybyste se v tom kapánek líp vyznal, pane,“ pravil, „sám byste pochopil, že to není možný. Vždyť vítr fouká
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

        /// <summary>
        /// Obsahuje houby (latinsky, česky, latinsky + info o původu)
        /// </summary>
        public static Tuple<string, string, string>[] Mycelias
        {
            get
            {
                if (__Mycelias is null)
                    __Mycelias = _GetMycelias().ToArray();
                return __Mycelias;
            }
        }
        /// <summary>
        /// Houby
        /// </summary>
        private static Tuple<string, string, string>[] __Mycelias;
        /// <summary>
        /// Vrátí názvy hub: Latinský - Český - Latinský extend.
        /// </summary>
        /// <returns></returns>
        private static List<Tuple<string, string, string>> _GetMycelias()
        {
            // Data obsahují řádky ve fixním pořadí: LatName, CzName, LatName2
            // Zdroj: https://www.mykoweb.cz/atlas-hub#limit=250
            // Zkopírovat do prostého textu a soupis pouze hub zkopírovat sem:
            // POZOR: někdy ve zdroji chybí první z latinských slov. Doplnil jsem je ručně (ze třetího prvku), ale je to vopruz.
            //  Asi nebude potřeba to aktualizovat :-) !!!
            string data = @"Gloeophyllum odoratum
anýzovník vonný
Gloeophyllum odoratum (Wulfen) Imazeki 1943

Balsamia polysperma
balzamovka mnohovýtrusá
Balsamia polysperma Vittad. (1831)

Sarcosphaera coronaria
baňka velkokališná
Sarcosphaera coronaria (Jacq.) J. Schröt. 1893

Battarrea phalloides
battarovka pochvatá (Stevenova)
Battarrea phalloides (Dicks.) Pers. 1801

Lepiota boudieri
bedla Boudierova
Lepiota boudieri Bres. 1881

Cystolepiota bucknallii
bedla Bucknallova
Cystolepiota bucknallii (Berk. & Broome) Singer & Clémençon 1972

Leucocoprinus birnbaumii
bedla cibulkotřenná
Leucocoprinus birnbaumii (Corda) Singer 1962

Lepiota felina
bedla černošupinná
Lepiota felina (Pers.) P. Karst. 1879

Chlorophyllum rachodes
bedla červenající
Chlorophyllum rachodes (Vittad.) Vellinga 2002

Leucoagaricus nympharum
bedla dívčí
Leucoagaricus nympharum (Kalchbr.) Bon 1977

Lepiota grangei
bedla Grangeova
Lepiota grangei (Eyre) Kühner 1934

Leucocoprinus heinemannii
bedla Heinemannova
Leucocoprinus heinemannii Migl. 1987

Cystolepiota hetieri
bedla Hetierova
Cystolepiota hetieri (Boud.) Singer 1973

Lepiota fuscovinacea
bedla hnědovínová
Lepiota fuscovinacea F.H. Møller & J.E. Lange 1940

Lepiota cristata
bedla hřebenitá
Lepiota cristata (Bolton) P. Kumm. 1871

Lepiota castanea
bedla kaštanová
Lepiota castanea Quél. 1881

Lepiota pseudolilacea
bedla klamavá
Lepiota pseudolilacea Huijsman 1947

Macrolepiota konradii
bedla Konradova
Macrolepiota konradii (Huijsman ex P.D. Orton) M.M. Moser 1967

Melanophyllum haematospermum
bedla krvavá
Melanophyllum haematospermum (Bull.) Kreisel 1984

Lepiota subincarnata
bedla namasovělá
Lepiota subincarnata J.E. Lange 1940

Lepiota magnispora
bedla nažloutlá
Lepiota magnispora Murrill 1912

Macrolepiota excoriata
bedla odřená
Macrolepiota excoriata (Schaeff.) Wasser 1978

Lepiota ignivolvata
bedla ohňopochvá
Lepiota ignivolvata Bousset & Joss. ex Joss. 1990

Chamaemyces fracidus
bedla orosená
Chamaemyces fracidus (Fr.) Donk 1962

Echinoderma asperum
bedla ostrošupinná
Echinoderma asperum (Pers.) Bon 1991

Lepiota tomentella
bedla plstnatá
Lepiota tomentella J.E. Lange 1923

Cystolepiota seminuda
bedla polonahá
Cystolepiota seminuda (Lasch) Bon 1976

Cystolepiota pulverulenta
bedla poprášená
Cystolepiota pulverulenta (Huijsman) Vellinga 1992

Leucoagaricus croceovelutinus
bedla šafránová
Leucoagaricus croceovelutinus (Bon & Boiffard) Bon 1976

Chlorophyllum olivieri
bedla šedohnědá
Chlorophyllum olivieri (Barla) Vellinga 2002

Lepiota oreadiformis
bedla špičkovitá
Lepiota oreadiformis Velen. 1920

Lepiota echinella
bedla štětinkatá
Lepiota echinella Quél. & G.E. Bernard 1888

Macrolepiota mastoidea
bedla útlá
Macrolepiota mastoidea (Fr.) Singer 1951

Lepiota clypeolaria
bedla vlnatá
Lepiota clypeolaria (Bull.) P. Kumm. 1871

Macrolepiota procera
bedla vysoká
Macrolepiota procera (Scop.) Singer 1948

Chlorophyllum brunneum
bedla zahradní
Chlorophyllum brunneum (Farl. & Burt) Vellinga 2002

Leucoagaricus leucothites
bedla zardělá
Leucoagaricus leucothites (Vittad.) Wasser 1977

Leucopaxillus gentianeus
běločechratka hořká
Leucopaxillus gentianeus (Quél.) Kotl. 1966

Leucopaxillus giganteus
běločechratka obrovská
Leucopaxillus giganteus (Sowerby) Singer 1939

Pseudoclitopilus rhodoleucus
běločechratka zardělá
Pseudoclitopilus rhodoleucus (Sacc.) Kühner 1926

Postia balsamea
bělochoroš cystidonosný
Postia balsamea (Peck) Jülich 1982

Postia sericeomollis
bělochoroš hedvábitý
Postia sericeomollis (Romell) Jülich 1982

Postia stiptica
bělochoroš hořký
Postia stiptica (Pers.) Jülich 1982

Aurantiporus fissilis
bělochoroš jabloňový
Aurantiporus fissilis (Berk. & M.A. Curtis) H. Jahn 1973

Tyromyces kmetii
bělochoroš Kmeťův
Tyromyces kmetii (Bres.) Bondartsev & Singer 1941

Postia fragilis
bělochoroš křehký
Postia fragilis (Fr.) Jülich 1982

Postia subcaesia
bělochoroš lužní
Postia subcaesia (A. David) Jülich 1982

Postia caesia
bělochoroš modravý
Postia caesia (Schrad.) Gilb. & Ryvarden 1985

Postia rennyi
bělochoroš prašnatý
Postia rennyi (Berk. & Broome) Rajchenb.

Postia ptychogaster
bělochoroš pýchavkovitý
Postia ptychogaster (F. Ludw.) Vesterh. 1996

Postia guttulata
bělochoroš slzící
Postia guttulata (Sacc.) Jülich 1982

Tyromyces chioneus
bělochoroš sněhobílý
Tyromyces chioneus (Fr.) P. Karst. 1881

Skeletocutis nivea
bělochoroš (kostrovka) polokloboukatý
Skeletocutis nivea (Jungh.) Jean Keller 1979

Trichophaea hemisphaerioides
bělokosmatka miskovitá
Trichophaea hemisphaerioides (Mouton) Graddon 1960

Trichophaea woolhopeia
bělokosmatka osmahlá
Trichophaea woolhopeia (Cooke & W. Phillips)

Humaria hemisphaerica
bělokosmatka polokulovitá
Humaria hemisphaerica (F.H. Wigg.) Fuckel 1870

Trichophaea gregaria
bělokosmatka pospolitá
Trichophaea gregaria (Rehm) Boud. (1907)

Choiromyces venosus
bělolanýž obecný
Choiromyces venosus (Fr.) Th. Fr. 1909

Leucocortinarius bulbiger
bělopavučinec hlíznatý
Leucocortinarius bulbiger (Alb. & Schwein.) Singer 1945

Bankera violascens
bělozub nafialovělý
Bankera violascens (Alb. & Schwein.) Pouzar 1955

Phellodon fuligineoalbus
bělozub osmahlý
Phellodon fuligineoalbus (J.C. Schmidt) Baird 2013

Pachyella violaceonigra
bochníček fialovočerný
Pachyella violaceonigra (Rehm) Pfister 1974

Pachyella babingtonii
bochníček potoční
Pachyella babingtonii (Berk. & Broome) Boud. 1907

Camarops tubulina
bolinka černohnědá
Camarops tubulina (Alb. & Schwein.) Shear 1938

Camarops polysperma
bolinka mnohovýtrusá
Camarops polysperma (Mont.) J.H. Mill. 1930

Auricularia mesenterica
boltcovitka mozkovitá
Auricularia mesenterica (Dicks.) Pers. 1822

Auricularia auricula-judae
boltcovitka ucho Jidášovo
Auricularia auricula-judae (Bull.) Quél. 1886

Bondarzewia mesenterica
bondarcevka horská
Bondarzewia mesenterica (Schaeff.) Kreisel 1984

Eutypella sorbi
bradavkatka jeřábová
Eutypella sorbi (Alb. & Schwein.) Sacc. 1882

Eutypella alnifraga
bradavkatka olšová
Eutypella alnifraga (Wahlenb.) Sacc. 1882

Eutypa spinosa
bradavkatka ostnitá
Eutypa spinosa (Pers.) Tul. & C. Tul.

Peroneutypa scoparia
bradavkatka různoostná
Peroneutypa scoparia (Schwein.) Carmarán & A.I. Romero 2006

Trichaptum biforme
bránovitec dvoutvarý
Trichaptum biforme (Fr.) Ryvarden 1972

Trichaptum fuscoviolaceum
bránovitec hnědofialový
Trichaptum fuscoviolaceum (Ehrenb.) Ryvarden 1972

Trichaptum abietinum
bránovitec jedlový
Trichaptum abietinum (Dicks.) Ryvarden 1972

Irpex lacteus
bránovitka mléčná
Irpex lacteus (Fr.) Fr. 1828

Steccherinum oreophilum
bránovitka přezkatá
Steccherinum oreophilum Lindsey & Gilb. 1977

Lachnellula calyciformis
brvenka číškovitá
Lachnellula calyciformis (Fr.) Dharne (1965)

Lachnellula subtilissima
brvenka drobná
Lachnellula subtilissima (Cooke) Dennis

Lachnellula gallica
brvenka francouzská
Lachnellula gallica (P. Karst. & Har.) Dennis (1962)

Lachnellula occidentalis
brvenka Hahnova
Lachnellula occidentalis (G.G. Hahn & Ayers) Dharne 1965

Lasiobolus macrotrichus
Lasiobolus macrotrichus
Lasiobolus macrotrichus Rea 1917

Lasiobolus intermedius
Lasiobolus intermedius
Lasiobolus intermedius J.L. Bezerra & Kimbr. (1975)

Piptoporus betulinus
březovník obecný
Piptoporus betulinus (Bull.) P. Karst. 1881

Macrocystidia cucumis
cystidovka rybovonná
Macrocystidia cucumis (Pers.) Joss. 1934

Mitrula paludosa
čapulka bahenní
Mitrula paludosa Fr. 1816

Tapinella atrotomentosa
čechratice černohuňatá
Tapinella atrotomentosa (Batsch) Šutara 1992

Tapinella panuoides
čechratice sklepní
Tapinella panuoides (Fr.) E.-J. Gilbert 1931

Paxillus rubicundulus
čechratka olšová
Paxillus rubicundulus P.D. Orton 1969

Paxillus involutus
čechratka podvinutá
Paxillus involutus (Batsch) Fr. 1838

Galerina atkinsoniana
čepičatka Atkinsonova
Galerina atkinsoniana A.H. Sm. 1953

Phaeogalera stagnina
čepičatka bažinná
Phaeogalera stagnina (Fr.) Pegler & T.W.K. Young 1975

Galerina marginata
čepičatka jehličnanová
Galerina marginata (Batsch) Kühner 1935

Galerina triscopa
čepičatka kmenová
Galerina triscopa (Fr.) Kühner 1935

Galerina mairei
čepičatka kosťovitá
Galerina mairei Boutev. & P.-A. Moreau 2005

Exidia thuretiana
černorosol bělavý
Exidia thuretiana (Lév.) Fr. 1874

Myxarium nucleatum
černorosol bezbarvý
Myxarium nucleatum Wallr. 1833

Exidia saccharina
černorosol borový
Exidia saccharina Fr. 1822

Exidia glandulosa
černorosol bukový
Exidia glandulosa (Bull.) Fr. 1822

Exidia cartilaginea
černorosol chrupavčitý
Exidia cartilaginea S. Lundell & Neuhoff 1935

Exidia pithya
černorosol smrkový
Exidia pithya (Alb. & Schwein.) Fr. 1822

Exidia recisa
černorosol terčovitý
Exidia recisa (Ditmar) Fr. 1822

Exidia badioumbrina
černorosol umbrový
Exidia badioumbrina (Bres.) Neuhoff 1936

Exidia truncata
černorosol uťatý
Exidia truncata Fr. 1822

Melanogaster broomeanus
černoušek Broomeův
Melanogaster broomeanus Berk. 1843

Ascocoryne sarcoides
čihovitka masová
Ascocoryne sarcoides (Jacq.) J.W. Groves & D.E. Wilson 1967

Ascocoryne cylichnium
čihovitka větší
Ascocoryne cylichnium (Tul.) Korf 1971

Dermoloma josserandii
čirůvečka Josserandova
Dermoloma josserandii Dennis & P.D. Orton 1960

Dermoloma cuneifolium
čirůvečka klínolupenná
Dermoloma cuneifolium (Fr.) Singer ex Bon 1986

Dermoloma pseudocuneifolium
čirůvečka trávníková
Dermoloma pseudocuneifolium Herink ex Bon 1986

Tricholoma albobrunneum
čirůvka bělohnědá
Tricholoma albobrunneum (Pers.) P. Kumm. 1871

Tricholoma stiparophyllum
čirůvka běložlutavá
Tricholoma stiparophyllum (N. Lund) P. Karst. 1879

Tricholoma album
čirůvka bílá
Tricholoma album (Schaeff.) P. Kumm. 1871

Tricholoma sciodes
čirůvka buková
Tricholoma sciodes (Pers.) C. Martín 1919

Tricholoma apium
čirůvka celerová
Tricholoma apium Jul. Schäff. 1925

Porpoloma metapodium
čirůvka černavá
Porpoloma metapodium (Fr.) Singer 1973

Tricholoma atrosquamosum
čirůvka černošupinná
Tricholoma atrosquamosum Sacc. 1887

Lepista personata
čirůvka dvoubarvá
Lepista personata (Fr.) Cooke 1871

Lepista nuda
čirůvka fialová
Lepista nuda (Bull.) Cooke 1871

Tricholoma portentosum
čirůvka havelka
Tricholoma portentosum (Fr.) Quél. 1873

Tricholoma columbetta
čirůvka holubičí
Tricholoma columbetta (Fr.) P. Kumm. 1871

Tricholoma acerbum
čirůvka hořká
Tricholoma acerbum (Bull.) Quél. 1872

Tricholoma vaccinum
čirůvka kravská
Tricholoma vaccinum (Schaeff.) P. Kumm. 1871

Tricholoma cingulatum
čirůvka kroužkatá
Tricholoma cingulatum (Almfelt ex Fr.) Jacobashch 1890

Tricholoma pseudonictitans
čirůvka lesklá (jehličnanová)
Tricholoma pseudonictitans Bon 1983

Tricholoma focale
čirůvka límcovitá
Tricholoma focale (Fr.) Ricken 1914

Calocybe gambosa
čirůvka májovka
Calocybe gambosa (Fr.) Donk 1962

Tricholoma pessundatum
čirůvka masitá
Tricholoma pessundatum (Fr.) Quél. (1872)

Rugosomyces carneus
čirůvka masová
Rugosomyces carneus (Bull.) Bon 1991

Tricholoma psammopus
čirůvka modřínová
Tricholoma psammopus (Kalchbr.) Quél. 1875

Tricholoma saponaceum
čirůvka mýdlová
Tricholoma saponaceum (Fr.) P. Kumm. 1871

Tricholoma inamoenum
čirůvka nevonná
Tricholoma inamoenum (Fr.) Gillet 1874

Tricholoma colossus
čirůvka obrovská
Tricholoma colossus (Fr.) Quél. 1872

Tricholoma sejunctum
čirůvka odlišná
Tricholoma sejunctum (Sowerby) Quél. 1872

Tricholoma viridilutescens
čirůvka olivově hnědá
Tricholoma viridilutescens M.M. Moser 1978

Tricholoma ustaloides
čirůvka opálená
Tricholoma ustaloides Romagn. 1954

Tricholoma aurantium
čirůvka oranžová
Tricholoma aurantium (Schaeff.) Ricken 1914

Tricholoma frondosae
čirůvka osiková
Tricholoma frondosae Kalamees & Shchukin 2001

Tricholoma ustale
čirůvka osmahlá
Tricholoma ustale (Fr.) P. Kumm. 1871

Tricholoma fucatum
čirůvka peřestá
Tricholoma fucatum (Fr.) P. Kumm. 1871

Tricholoma stans
čirůvka pochybná
Tricholoma stans (Fr.) Sacc. 1887

Tricholoma batschii
čirůvka prstenitá
Tricholoma batschii Gulden 1969

Tricholoma arvernense
čirůvka příbuzná
Tricholoma arvernense Bon 1976

čirůvka růžovolupenná
Tricholoma orirubens Quél. 1872
Tricholoma basirubens

Tricholoma basirubens
čirůvka růžovotřenná
Tricholoma basirubens (Bon) A. Riva & Bon 1988

Tricholoma aestuans
čirůvka sálající
Tricholoma aestuans (Fr.) Gillet 1874

Tricholoma sulphureum
čirůvka sírožlutá
Tricholoma sulphureum (Bull.) P. Kumm. 1871

Tricholoma lascivum
čirůvka smrdutá
Tricholoma lascivum (Fr.) Gillet 1874

Tricholoma imbricatum
čirůvka střechovitá
Tricholoma imbricatum (Fr.) P. Kumm. 1871

Tricholoma scalpturatum
čirůvka šedožemlová
Tricholoma scalpturatum (Fr.) Quél. 1872

Lepista sordida
čirůvka špinavá
Lepista sordida (Schumach.) Singer 1951

Tricholoma populinum
čirůvka topolová
Tricholoma populinum J.E. Lange 1933

Tricholoma pardinum
čirůvka tygrovaná
Tricholoma pardinum (Pers.) Quél. 1873

Tricholoma matsutake
čirůvka větší
Tricholoma matsutake (S. Ito & S. Imai) Singer 1943

Rugosomyces ionides
čirůvka violková
Rugosomyces ionides (Bull.) Bon 1991

Tricholoma filamentosum
čirůvka vláknitá
Tricholoma filamentosum (Alessio) Alessio 1988

Lepista luscina
čirůvka zamlžená
Lepista luscina (Fr.) Singer 1951

Tricholoma equestre
čirůvka zelánka
Tricholoma equestre (L.) P. Kumm. 1871

Tricholoma terreum
čirůvka zemní
Tricholoma terreum (Schaeff.) P. Kumm. 1871

Tricholoma bufonium
čirůvka žabí
Tricholoma bufonium (Pers.) Gillet 1874

Tricholoma virgatum
čirůvka žíhaná
Tricholoma virgatum (Fr.) P. Kumm. 1871

Tricholoma fulvum
čirůvka žlutohnědá
Tricholoma fulvum (DC.) Bigeard & H. Guill. 1909

Tricholoma distantifoliaceum
Tricholoma distantifoliaceum
Tricholoma distantifoliaceum E. Ludw. & H. Willer 2012

Cyathus olla
číšenka hrnečková
Cyathus olla (Batsch) Pers. 1801

Cyathus striatus
číšenka rýhovaná
Cyathus striatus (Huds.) Willd. 178

Cyathus stercoreus
číšenka výkalová
Cyathus stercoreus (Schwein.) De Toni 1888

Lachnella alboviolascens
číšovec bělofialový
Lachnella alboviolascens (Alb. & Schwein.) Fr. 1849

Cyphella digitalis
číšovec náprstkovitý
Cyphella digitalis (Alb. & Schwein.) Fr. 1822

Woldmaria filicina
číšovec pérovníkový
Woldmaria filicina (Peck) Knudsen 1996

Calyptella capula
číšoveček kápovitý
Calyptella capula (Holmsk.) Quél. 1888

Discina ancilis
destice chřapáčová
Discina ancilis (Pers.) Sacc. 1889

Gyromitra parma
destice okrouhlá
Gyromitra parma (J. Breitenb. & Maas Geest.) Kotl. & Pouzar 1974

Cyathicula coronata
dlouhobrvka zdobená
Cyathicula coronata (Bull.) Rehm 1893

Xylaria carpophila
dřevnatka číškomilná
Xylaria carpophila (Pers.) Fr. 1849

Xylaria longipes
dřevnatka dlouhonohá
Xylaria longipes Nitschke 1867

Xylaria polymorpha
dřevnatka kyjovitá
Xylaria polymorpha (Pers.) Grev. 1824

Xylaria filiformis
dřevnatka niťovitá
Xylaria filiformis (Alb. & Schwein.) Fr. 1849

Xylaria hypoxylon
dřevnatka parohatá
Xylaria hypoxylon (L.) Grev. 1824

Byssomerulius corium
dřevokaz kožový
Byssomerulius corium (Pers.) Parmasto 1967

Phlebia tremellosa
dřevokaz rosolovitý
Phlebia tremellosa (Schrad.) Nakasone & Burds. 1984

Hypoxylon fragiforme
dřevomor červený
Hypoxylon fragiforme (Pers.) J. Kickx f. 1835

Hypoxylon fuscum
dřevomor hnědý
Hypoxylon fuscum (Pers.) Fr. 1849

Hypoxylon howeanum
dřevomor Howeův
Hypoxylon howeanum Peck 1872

Annulohypoxylon multiforme
dřevomor mnohotvarý
Annulohypoxylon multiforme (Fr.) Y.M. Ju, J.D. Rogers & H.M. Hsieh

Nemania serpens
dřevomor plazivý
Nemania serpens (Pers.) Gray 1821

Entoleuca mammata
dřevomor prsnatý
Entoleuca mammata (Wahlenb.) J.D. Rogers & Y.M. Ju 1996

Jackrogersella cohaerens
dřevomor ranový
Jackrogersella cohaerens (Pers.) L. Wendt, Kuhnert & M. Stadler 2017

Hypoxylon rubiginosum
dřevomor rezavý
Hypoxylon rubiginosum (Pers.) Fr. 1849

Serpula lacrymans
dřevomorka domácí
Serpula lacrymans (Wulfen) J. Schröt. 1885

Serpula himantioides
dřevomorka lesní
Serpula himantioides (Fr.) P. Karst. 1884

Leucogyrophana mollusca
dřevomorka meruňková
Leucogyrophana mollusca (Fr.) Pouzar 1958

Pseudomerulius aureus
dřevomorka zlatá
Pseudomerulius aureus (Fr.) Jülich 1979

Onnia triqueter
ďubkatec borový
Onnia triqueter (Pers.) Imazeki 1955

Coltricia perennis
ďubkatec pohárkovitý
Coltricia perennis (L.) Murrill 1903

Pachykytospora tuberculosa
dubovnice střevovitá
Pachykytospora tuberculosa (Fr.) Kotl. & Pouzar 1963

Fayodia bisphaerigera
fajodka osténkatá
Fayodia bisphaerigera (J.E. Lange) Singer 1936

Gamundia striatula
fajodka zimní
Gamundia striatula (Kühner) Raithelh. 1983

Phallus impudicus
hadovka smrdutá
Phallus impudicus L. 1753

Phallus hadriani
hadovka valčická
Phallus hadriani Vent. 1798

Mycena belliae
helmovka Bellové
Mycena belliae (Johnst.) P.D. Orton 1960

Mycena bulbosa
helmovka cibulkatá
Mycena bulbosa (Cejp) Kühner 1938

Mycena atropapillata
helmovka (černobradavková)
Mycena atropapillata Kühner & Maire 1938

Mycena rubromarginata
helmovka červenobřitá
Mycena rubromarginata (Fr.) P. Kumm. 1871

Mycena stylobates
helmovka diskovitá
Mycena stylobates (Pers.) P. Kumm. 1871

Mycena diosma
helmovka dvojvonná
Mycena diosma Krieglst. & Schwöbel 1982

Mycena laevigata
helmovka hladká
Mycena laevigata (Lasch) Gillet (1876)

Mycena purpureofusca
helmovka hnědopurpurová
Mycena purpureofusca (Peck) Sacc. 1887

Mycena acicula
helmovka jehličková
Mycena acicula (Schaeff.) P. Kumm. 1871

Atheniella adonis
helmovka jitřenková
Atheniella adonis (Bull.) Redhead, Moncalvo, Vilgalys, Desjardin & B.A. Perry 2012

Mycena pterigena
helmovka kapradinová
Mycena pterigena (Fr.) P. Kumm. 1871

Mycena pseudocorticola
helmovka koromilná
Mycena pseudocorticola Kühner (1938)

Mycena sanguinolenta
helmovka krvavá
Mycena sanguinolenta (Alb. & Schwein.) P. Kumm. 1871

Mycena haematopus
helmovka krvonohá
Mycena haematopus (Pers.) P. Kumm. 1871

Mycena metata
helmovka kuželovitá
Mycena metata (Secr. ex Fr.) P. Kumm. (1871)

Mycena nucicola
helmovka lísková
Mycena nucicola Huijsman (1958)

Mycena stipata
helmovka louhová
Mycena stipata Maas Geest. & Schwöbel 1987

Mycena renati
helmovka medonohá
Mycena renati Quél. (1886)

Mycena galopus
helmovka mléčná
Mycena galopus (Pers.) P. Kumm. 1871

Mycena amicta
helmovka modravá
Mycena amicta (Fr.) Quél. 1872

Mycena rosea
helmovka narůžovělá
Mycena rosea Gramberg 1912

Mycena flavescens
helmovka nažloutlá
Mycena flavescens Velen. 1920

Mycena vulgaris
helmovka obecná
Mycena vulgaris (Pers.) P. Kumm. 1871

Mycena tintinnabulum
helmovka pařezová
Mycena tintinnabulum (Paulet) Quél. 1872

Mycena abramsii
helmovka raná
Mycena abramsii (Murrill) Murrill 1916

Mycena romagnesiana
helmovka Romagnesiho
Mycena romagnesiana Maas Geest. 1991

Mycena rosella
helmovka růžová
Mycena rosella (Fr.) P. Kumm. 1871

Mycena polygramma
helmovka rýhonohá
Mycena polygramma (Bull.) Gray 1821

Mycena pura
helmovka ředkvičková
Mycena pura (Pers.) P. Kumm. 1871

Hemimycena cucullata
helmovka sádrová
Hemimycena cucullata (Pers.) Singer 1961

Mycena maculata
helmovka skvrnitá
Mycena maculata P. Karst. 1890,

Mycena epipterygia
helmovka slizká
Mycena epipterygia (Scop.) Gray 1821

Mycena silvae-nigrae
helmovka smrková
Mycena silvae-nigrae Maas Geest. & Schwöbel 1987

Mycena niveipes
helmovka sněhonohá
Mycena niveipes (Murrill) Murrill 1916

Mycena flos-nivium
helmovka sněžná
Mycena flos-nivium Kühner 1952

Mycena crocata
helmovka šafránová
Mycena crocata (Schrad.) P. Kumm. 1871

Mycena cinerella
helmovka šedá
Mycena cinerella (P. Karst.) P. Karst. 1879

Mycena latifolia
helmovka širokolupenná
Mycena latifolia (Peck) A.H. Sm. 1935

Mycena strobilicola
helmovka šiškomilná
Mycena strobilicola J. Favre & Kühner 1938

Mycena galericulata
helmovka tuhonohá
Mycena galericulata (Scop.) Gray 1821

Mycena viridimarginata
helmovka zelenobřitá
Mycena viridimarginata P. Karst. 1892

Mycena hiemalis
helmovka zimní
Mycena hiemalis (Osbeck) Quél. 1872

Mycena aurantiomarginata
helmovka zlatobřitá
Mycena aurantiomarginata (Fr.) Quél. 1872

Mycena pelianthina
helmovka zoubkatá
Mycena pelianthina (Fr.) Quél. 1872

Mycena flavoalba
helmovka žlutobílá
Mycena flavoalba (Fr.) Quél. 1872

Mycena agrestis
Mycena agrestis
Mycena agrestis Aronsen & Maas Geest. 1997

Hapalopilus nidulans
hlinák červenající
Hapalopilus nidulans (Fr.) P. Karst. 1881

Hapalopilus ochraceolateritius
hlinák (okrovocihlový)
Hapalopilus ochraceolateritius (Bondartsev) Bondartsev & Singer 1941

Pleurotus calyptratus
hlíva čepičkatá
Pleurotus calyptratus (Lindblad ex Fr.) Sacc. 1887

Tectella patellaris
hlíva číškovitá
Tectella patellaris (Fr.) Murrill 1915

Pleurotus dryinus
hlíva dubová
Pleurotus dryinus (Pers.) P. Kumm. 1871

Panus conchatus
hlíva fialová
Panus conchatus (Bull.) Fr. 1838

Phyllotopsis nidulans
hlíva hnízdovitá
Phyllotopsis nidulans (Pers.) Singer 1936

Panus lecomtei
hlíva chlupatá
Panus lecomtei (Fr.) Corner 1981

Omphalotus illudens
hlíva klamná
Omphalotus illudens Bresinsky & Besl 1979

Hohenbuehelia petaloides
hlíva plátkovitá (zemní)
Hohenbuehelia petaloides (Bull.) Schulzer 1866

Pleurotus pulmonarius
hlíva plicní
Pleurotus pulmonarius (Fr.) Quél. 1872

Pleurotus ostreatus
hlíva ústřičná
Pleurotus ostreatus (Jacq.) P. Kumm. 1871

Pleurocybella porrigens
hlíva ušatá
Pleurocybella porrigens (Pers.) Singer 1947

Resupinatus striatulus
hlívečník Kavinův
Resupinatus striatulus (Pers.) Murrill

Resupinatus applicatus
hlívečník připjatý
Resupinatus applicatus (Batsch) Gray 1821

Hohenbuehelia unguicularis
hlívička ???
Hohenbuehelia unguicularis (Fr.) O.K. Mill. 1986

Hohenbuehelia cyphelliformis
hlívička číšovcovitá
Hohenbuehelia cyphelliformis (Berk.) O.K. Mill. 1986

Hohenbuehelia auriscalpium
hlívička stopkatá
Hohenbuehelia auriscalpium (Maire) Singer 1951

Rhodotus palmatus
hlívovec ostnovýtrusý
Rhodotus palmatus (Bull.) Maire 1926

Hymenogaster niveus
hlíza bělostná
Hymenogaster niveus Vittad. (1831)

Hymenogaster bulliardii
hlíza Bulliardova
Hymenogaster bulliardii Vittad. 1831

Hymenogaster olivaceus
hlíza olivová
Hymenogaster olivaceus Vittad. (1831)

Hymenogaster decorus
hlíza zdobná
Hymenogaster decorus Tul. & C. Tul. (1843)

Hymenogaster luteus
hlíza žlutá
Hymenogaster luteus Vittad. (1831)

Monilinia jonsonii
hlízenka hlohová
Monilinia jonsonii (Ellis & Everh.) Honey 1936

Sclerotinia ficariae
hlízenka orsejová
Sclerotinia ficariae Rehm 1893

Dumontinia tuberosa
hlízenka sasanková
Dumontinia tuberosa (Bull.) L.M. Kohn 1979

Phaeolus schweinitzi
hnědák Schweinitzův
Phaeolus schweinitzi (Fr.) Pat. 1900

Nidularia deformis
hnízdovka nacpaná
Nidularia deformis (Willd.) Fr. 1817

Coprinellus domesticus
hnojník domácí
Coprinellus domesticus (Bolton) Vilgalys, Hopple & Jacq. Johnson 2001

Coprinopsis acuminata
hnojník hrotitý
Coprinopsis acuminata (Romagn.) Redhead, Vilgalys & Moncalvo 2001

Coprinopsis atramentaria
hnojník inkoustový
Coprinopsis atramentaria (Bull.) Redhead, Vilgalys & Moncalvo 2001

Coprinopsis cinerea
hnojník mrvní
Coprinopsis cinerea (Schaeff.) Redhead, Vilgalys & Moncalvo 2001

Coprinus disseminatus
hnojník nasetý
Coprinus disseminatus (Pers.) Gray 1821

Coprinus comatus
hnojník obecný
Coprinus comatus (O.F. Müll.) Pers. 1797

Parasola plicatilis
hnojník řasnatý
Parasola plicatilis (Curtis) Redhead, Vilgalys & Hopple 2001

Coprinus picaceus
hnojník strakatý
Coprinus picaceus (Bull.) Gray 1821

Coprinellus micaceus
hnojník třpytivý
Coprinellus micaceus (Bull.) Vilgalys, Hopple & Jacq. Johnson 2001

Coprinopsis lagopus
hnojník zaječí
Coprinopsis lagopus (Fr.) Redhead, Vilgalys & Moncalvo 2001

Russula chloroides
holubinka akvamarínová
Russula chloroides (Krombh.) Bres. 1900

Russula delica
holubinka bílá
Russula delica Fr. 1838

Russula badia
holubinka brunátná
Russula badia Quél. 1881

Russula betularum
holubinka březová
Russula betularum Hora 1960

Russula faginea
holubinka buková
Russula faginea Romagn. 1967

Russula heterophylla
holubinka bukovka
Russula heterophylla (Fr.) Fr. 1838

Russula integra
holubinka celokrajná
Russula integra (L.) Fr. 1838

Russula luteotacta
holubinka citlivá
Russula luteotacta Rea 1922

Russula nigricans
holubinka černající
Russula nigricans Fr. 1838

Russula albonigra
holubinka černobílá
Russula albonigra (Krombh.) Fr. 1874

Russula atropurpurea
holubinka černonachová
Russula atropurpurea (Krombh.) Britzelm.

Russula grisea
holubinka doupňáková (sivá)
Russula grisea Fr. 1838

Russula pumila
holubinka drobná
Russula pumila Rouzeau & F. Massart 1970

Russula cavipes
holubinka dutonohá
Russula cavipes Britzelm. 1893

Russula violeipes
holubinka fialovonohá
Russula violeipes Quél. 1898

Russula ionochlora
holubinka fialovozelená
Russula ionochlora Romagn. 1952

Russula ochroleuca
holubinka hlínožlutá
Russula ochroleuca Fr. 1838

Russula grata
holubinka hořkomandlová
Russula grata Britzelm. 1898

Russula amoenolens
holubinka hřebílkatá
Russula amoenolens Romagn. 1952

Russula pectinatoides
holubinka hřebínkatá
Russula pectinatoides Peck 1907

Russula densifolia
holubinka hustolupenná (hustolistá)
Russula densifolia Secr. ex Gillet 1876

Russula claroflava
holubinka chromová
Russula claroflava Grove 1888

Russula paludosa
holubinka jahodová
Russula paludosa Britzelm. 1891

Russula sardonia
holubinka jízlivá
Russula sardonia Fr. 1838

Russula anatina
holubinka kachní
Russula anatina Romagn. 1967

Russula mustelina
holubinka kolčaví
Russula mustelina Fr. 1838

Russula curtipes
holubinka krátkonohá
Russula curtipes F.H. Møller & Jul. Schäff. 1935

Russula fragilis
holubinka křehká
Russula fragilis Fr. 1838

Russula lundellii
holubinka Lundellova
Russula lundellii Singer 1951

Russula vesca
holubinka mandlová
Russula vesca Fr. 1836

Russula risigallina
holubinka měnlivá
Russula risigallina (Batsch) Sacc. 1915

Russula subrubens
holubinka mokřadní
Russula subrubens (J.E. Lange) Bon 1972

Russula cyanoxantha
holubinka namodralá
Russula cyanoxantha (Schaeff.) Fr. 1863

Russula virescens
holubinka nazelenalá
Russula virescens (Schaeff.) Fr. 1836

Russula decolorans
holubinka odbarvená
Russula decolorans (Fr.) Fr. 1838

Russula olivacea
holubinka olivová
Russula olivacea (Schaeff.) Fr. 1838

Russula alnetorum
holubinka olšinná
Russula alnetorum Romagn. 1956

Russula adusta
holubinka osmahlá
Russula adusta (Pers.) Fr. 1838

Russula subfoetens
holubinka páchnoucí (zápašná)
Russula subfoetens W.G. Sm. 1873

Russula acrifolia
holubinka palčivolupenná (ostrá)
Russula acrifolia Romagn. 1997

Russula farinipes
holubinka pružná
Russula farinipes Romell 1893

Russula queletii
holubinka Quéletova
Russula queletii Fr. 1872

Russula nauseosa
holubinka raná
Russula nauseosa (Pers.) Fr. 1838

Russula helodes
holubinka rašelinná
Russula helodes Melzer 1929

Russula xerampelina
holubinka révová
Russula xerampelina (Schaeff.) Fr. 1838

Russula rhodopus
holubinka rudonohá
Russula rhodopus Zvára 1927

Russula pseudointegra
holubinka ruměnná
Russula pseudointegra Arnould & Goris 1907

Russula roseipes
holubinka růžovonohá
Russula roseipes Secr. ex Bres. 1881

Russula sororia
holubinka sesterská
Russula sororia (Fr.) Romell 1891

Russula maculata
holubinka skvrnitá
Russula maculata Quél. 1878

Russula graveolens
holubinka slanečková
Russula graveolens Romell 1885

Russula rosea
holubinka sličná
Russula rosea Pers. 1796

Russula solaris
holubinka sluneční
Russula solaris Ferd. & Winge 1924

Russula foetens
holubinka smrdutá
Russula foetens Pers. 1796

Russula vinosa
holubinka tečkovaná
Russula vinosa Lindblad 1901

Russula illota
holubinka tmavolemá
Russula illota Romagn. 1954

Russula aeruginea
holubinka trávozelená
Russula aeruginea Lindblad ex Fr.

Russula emetica
holubinka vrhavka
Russula emetica (Schaeff.) Pers. 1796

Russula aurea
holubinka zlatá
Russula aurea Pers. 1796

Russula fellea
holubinka žlučová
Russula fellea (Fr.) Fr. 1838

Ophiocordyceps gracilis
housenec menší
Ophiocordyceps gracilis (Grev.) G.H. Sung, J.M. Sung, Hywel-Jones & Spatafora 2007

Ophiocordyceps myrmecophila
housenec mravenčí
Ophiocordyceps myrmecophila (Ces.) G.H. Sung, J.M. Sung, Hywel-Jones & Spatafora 2007

Tolypocladium ophioglossoides
housenice cizopasná
Tolypocladium ophioglossoides (J.F. Gmel.) Quandt, Kepler & Spatafora 2014

Cordyceps militaris
housenice červená
Cordyceps militaris (L.) Fr. 1818

Ophiocordyceps sinensis
housenice čínská
Ophiocordyceps sinensis (Berk.) G.H. Sung, J.M. Sung, Hywel-Jones & Spatafora 2007

Tolypocladium rouxii
housenice Rouxova
Tolypocladium rouxii (Cand.) Quandt, Kepler & Spatafora 2014

Neolentinus adhaerens
houževnatec přivázlý
Neolentinus adhaerens (Alb. & Schwein.) Redhead & Ginns 1985

Neolentinus lepideus
houževnatec šupinatý
Neolentinus lepideus (Fr.) Redhead & Ginns 1985

Lentinus suavissimus
houževnatec vonný
Lentinus suavissimus Fr. 1836

Lentinellus castoreus
houžovec bobří
Lentinellus castoreus (Fr.) Kühner & Maire 1934

Lentinellus cochleatus
houžovec hlemýžďovitý
Lentinellus cochleatus (Pers.) P. Karst. 1879

Lentinellus ursinus
houžovec medvědí
Lentinellus ursinus (Fr.) Kühner 1926

Ascobolus furfuraceus
hovník otrubičnatý
Ascobolus furfuraceus Pers. 1794

Ascobolus carbonarius
hovník spáleništní
Ascobolus carbonarius P.Karst. 1870

Sphaerobolus stellatus
hrachovec hvězdovitý
Sphaerobolus stellatus Tode 1790

Boletopsis leucomelaena
hrbolatka černobílá
Boletopsis leucomelaena (Pers.) Fayod 1889

Cystostereum murray
hrbolatník vonný
Cystostereum murray (Berk. & M.A. Curtis) Pouzar 1959

Geopora foliacea
hrobenka listovitá
Geopora foliacea (Schaeff.) S. Ahmad 1978

Geopora arenosa
hrobenka písečná
Geopora arenosa (Fuckel) S. Ahmad 1978

Geopora arenicola
hrobenka pískomilná
Geopora arenicola (Lév.) Kers 1974

Mycoacia uda
hrotnatečka žlutá
Mycoacia uda (Fr.) Donk 1931

Sarcodontia crocea
hrotnatka zápašná
Sarcodontia crocea (Schwein.) Kotl. 1953

Boletus pinophilus
hřib borový
Boletus pinophilus Pilát & Dermek 1973

Boletus aereus
hřib bronzový
Boletus aereus Bull. 1789

Xerocomellus rubellus
hřib červený
Xerocomellus rubellus (Krombh.) Šutara 2008

Buchwaldoboletus lignicola
hřib dřevožijný
Buchwaldoboletus lignicola (Kallenb.) Pilát 1969

Boletus reticulatus
hřib dubový
Boletus reticulatus Schaeff 1774

Boletus dupainii
hřib Dupainův
Boletus dupainii Boud. 1902

Suillus cavipes
hřib dutonohý
Suillus cavipes (Opat.) A.H. Sm. & Thiers 1964

Xerocomellus engelii
hřib Engelův
Xerocomellus engelii (Hlaváček) Šutara (2008)

Boletus fechtneri
hřib Fechtnerův
Boletus fechtneri Velen. 1922

Boletus badius
hřib hnědý
Boletus badius (Fr.) Fr.

Boletus subappendiculatus
hřib horský
Boletus subappendiculatus Dermek, Lazebn. & J. Veselský 1979

Boletus kluzakii
hřib Kluzákův
Boletus kluzakii Šutara & Špinar 2006

Boletus luridus
hřib koloděj
Boletus luridus Schaeff. 1774

Boletus erythropus
hřib kovář
Boletus erythropus Pers. 1796

Boletus luridiformis var. discolor
hřib kovář odbarvený
Boletus luridiformis var. discolor (Quél.) Krieglst. 1991

Boletus junquilleus
hřib kovář žlutý
Boletus junquilleus (Quél.) Boud. 1906

Boletus regius
hřib královský
Boletus regius Krombh. 1832

Boletus calopus
hřib kříšť
Boletus calopus Pers.

Boletus legaliae
hřib Le Galové
Boletus legaliae (Pilát & Dermek) Della Maggiora & Trassin 2015

Boletus legaliae f. spinarii
hřib Le Galové Špinarův
Boletus legaliae f. spinarii (Hlaváček) Janda 2009

Xerocomellus marekii
hřib Markův
Xerocomellus marekii (Šutara & Skála) Šutara 2008

Boletus radicans
hřib medotrpký
Boletus radicans Pers.

Xerocomellus armeniacus
hřib meruňkový
Xerocomellus armeniacus (Quél.) Šutara 2008

Boletus pulverulentus
hřib modračka
Boletus pulverulentus Opat. (1836)

Xerocomellus ripariellus
hřib mokřadní
Xerocomellus ripariellus (Redeuilh) Šutara 2008

Aureoboletus moravicus
hřib moravský
Aureoboletus moravicus (Vacek) Klofac 2010

Boletus rubrosanguineus
hřib Moserův
Boletus rubrosanguineus Cheype 1983

Boletus rhodoxanthus
hřib nachový
Boletus rhodoxanthus (Krombh.) Kallenb. 1925

Tylopilus porphyrosporus
hřib nachovýtrusý
Tylopilus porphyrosporus (Fr. & Hök) A.H. Sm. & Thiers 1971

Boletus ferrugineus
hřib osmahlý
Boletus ferrugineus Schaeff 1774

Xerocomellus bubalinus
hřib parkový (lindový)
Xerocomellus bubalinus (Oolbekk. & Duin) Mikšík 2014

Chalciporus piperatus
hřib peprný
Chalciporus piperatus (Bull.) Bataille 1908

Hemileccinum impolitum
hřib plavý
Hemileccinum impolitum (Fr.) Šutara 2008

Boletus subtomentosus
hřib plstnatý
Boletus subtomentosus L. 1753
";

            List<Tuple<string, string, string>> mycelias = new List<Tuple<string, string, string>>();
            var lines = data.Replace("\n", "").Split('\r');
            int length = lines.Length;
            for (int l = 0; l < length;)
            {
                if (!String.IsNullOrEmpty(lines[l]))
                {   // Neprázdný řádek: musím mít ještě další dva:
                    if (l + 2 >= length) break;
                    var latName = lines[l++];              // Aureoboletus moravicus
                    var czName = lines[l++];               // hřib moravský
                    var latName2 = lines[l++];             // Aureoboletus moravicus (Vacek) Klofac 2010
                    mycelias.Add(new Tuple<string, string, string>(latName, czName, latName2));
                }
                else
                    // Prázdný řádek mezi prvky přeskočím:
                    l++;
            }
            return mycelias;
        }
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
