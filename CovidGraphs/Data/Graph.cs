using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Djs.Tools.CovidGraphs.Data
{
    #region GraphInfo : Definice jednoho grafu = záhlaví, datumy, souhrn serií, volitelně i layout
    /// <summary>
    /// <see cref="GraphInfo"/> : Definice jednoho grafu = záhlaví, datumy, souhrn serií, volitelně i layout
    /// </summary>
    public class GraphInfo : DataSerializable
    {
        #region Konstruktor, proměnné

        public GraphInfo()
        {
            this.Id = NextId++;
            Init();
        }
        public override string ToString()
        {
            return this.Title;
        }
        private List<GraphSerieInfo> _Series;
        private void Init()
        {
            _Series = new List<GraphSerieInfo>();
        }
        private static int NextId = 0;
        #endregion
        #region Data definující záhlaví grafu
        public int Id { get; private set; }
        /// <summary>
        /// Titulek grafu: zobrazuje se v seznamu grafů a v záhlaví grafu
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Pořadí grafu v seznamu
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// Popisek grafu: zobrazuje se jako Tooltip grafu
        /// </summary>
        public string Description { get; set; }
        public System.Drawing.Image Icon { get; set; }
        /// <summary>
        /// Zobrazit posledních NNN dnů
        /// </summary>
        public int? TimeRangeLastDays { get; set; }
        /// <summary>
        /// Zobrazit posledních NNN měsíců: zobrazí aktuální měsíc + tento počet celých měsíců předchozích
        /// </summary>
        public int? TimeRangeLastMonths { get; set; }
        /// <summary>
        /// Exaktní počátek časového úseku
        /// </summary>
        public DateTime? TimeRangeBegin { get; set; }
        /// <summary>
        /// Exaktní konec časového úseku
        /// </summary>
        public DateTime? TimeRangeEnd { get; set; }
        /// <summary>
        /// Graf umožňuje změnu měřítka?
        /// </summary>
        public bool ChartEnableTimeZoom { get; set; }
        /// <summary>
        /// Osa Y bude vpravo?
        /// </summary>
        public bool ChartAxisYRight { get; set; }
        /// <summary>
        /// Jednotlivé serie grafu
        /// </summary>
        public GraphSerieInfo[] Series { get { return _Series.ToArray(); } }
        /// <summary>
        /// Do this grafu přidá danou serii
        /// </summary>
        /// <param name="serie"></param>
        public void AddSerie(GraphSerieInfo serie)
        {
            serie.Parent = this;
            _Series.Add(serie);
        }
        #endregion
        #region Static načtení celého seznamu grafů z config adresáře
        public static List<GraphInfo> LoadFromPath(string path = null)
        {
            List<GraphInfo> graphs = new List<GraphInfo>();

            if (path == null) path = App.ConfigPath;
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists)
            {
                string pattern = "*." + _Extension;
                var fileInfos = dirInfo.GetFiles(pattern);
                foreach (var fileInfo in fileInfos)
                {
                    GraphInfo graph = GraphInfo.LoadFromFile(fileInfo.FullName);
                    if (graph != null)
                        graphs.Add(graph);
                }
            }

            return graphs;
        }
        #endregion
        #region Načtení ze souboru a uložení do souboru
        public static GraphInfo LoadFromFile(string fileName)
        {
            if (String.IsNullOrEmpty(fileName) || !System.IO.File.Exists(fileName)) return null;

            GraphInfo graphInfo = new GraphInfo();
            using (var stream = CreateReadStream(fileName))
            {
                FileVersion fileVersion = FileVersion.None;
                string line = null;
                while (!stream.EndOfStream)
                {
                    if (line == null) line = stream.ReadLine();

                    if (fileVersion == FileVersion.None)
                    {   // Hledám header s označením verze:
                        if (line == ChartHeaderFileV1) fileVersion = FileVersion.Version1;
                        line = null;
                    }
                    else if (fileVersion == FileVersion.Version1)
                    {   // Zpracovávám verzi 1:
                        if (line == ChartHeaderLayout)
                        {   // Jde o poslední prvek = následuje kompletní layout grafu:
                            graphInfo.LoadFromStreamLayout(stream, fileVersion, ref line);
                        }
                        else if (line.StartsWith(ChartSeriesPrefix))
                        {   // Řádek začíná prefixem pro jednotlivou serii => zpracujeme serii:
                            graphInfo.LoadFromStreamSerie(stream, fileVersion, ref line);
                        }
                        else
                        {   // Jinak to nejspíš bude jednotlivý řádek obsahující property do hlavičky grafu:
                            graphInfo.LoadFromStreamHeaderLine(stream, fileVersion, ref line);
                        }
                    }
                }
            }
            graphInfo._FileName = fileName;

            return graphInfo;
        }

        private void LoadFromStreamHeaderLine(StreamReader stream, FileVersion fileVersion, ref string line)
        {
            string name = LoadNameValueFromString(line, out string text);

            switch (name)
            {
                case ChartHeaderTitle:
                    this.Title = GetValue(text, "");
                    break;
                case ChartHeaderOrder:
                    this.Order = GetValue(text, 0);
                    break;
                case ChartHeaderDescription:
                    this.Description = GetValue(text, "");
                    break;
                case ChartHeaderLastDays:
                    this.TimeRangeLastDays = GetValue(text, (int?)null);
                    break;
                case ChartHeaderLastMonths:
                    this.TimeRangeLastMonths = GetValue(text, (int?)null);
                    break;
                case ChartHeaderTimeBegin:
                    this.TimeRangeBegin = GetValue(text, (DateTime?)null);
                    break;
                case ChartHeaderTimeEnd:
                    this.TimeRangeEnd = GetValue(text, (DateTime?)null);
                    break;
                case ChartHeaderEnableTimeZoom:
                    this.ChartEnableTimeZoom = GetValue(text, false);
                    break;
                case ChartHeaderAxisYRight:
                    this.ChartAxisYRight = GetValue(text, false);
                    break;
            }

            line = null;
        }

        private void LoadFromStreamSerie(StreamReader stream, FileVersion fileVersion, ref string line)
        {
            while (!stream.EndOfStream)
            {
                GraphSerieInfo serie = GraphSerieInfo.LoadFromStream(stream, fileVersion, ref line);
                if (serie == null) break;
                this.AddSerie(serie);
            }
        }

        private void LoadFromStreamLayout(StreamReader stream, FileVersion fileVersion, ref string line)
        {
            this.ChartLayout = stream.ReadToEnd();
            line = null;
        }

        public void SaveToFile()
        {
            string fileName = FileName;

            using (var stream = CreateWriteStream(fileName))
            {
                stream.WriteLine(ChartHeaderFileV1);
                SaveToFileHeader(stream);
                foreach (var serie in Series)
                    SaveToFileSerie(stream, serie);
                SaveToFileLayout(stream);
            }

            _FileName = fileName;
        }

        private void SaveToFileHeader(StreamWriter stream)
        {
            stream.WriteLine(CreateLine(ChartHeaderTitle, GetSerial(this.Title)));
            stream.WriteLine(CreateLine(ChartHeaderOrder, GetSerial(this.Order)));
            if (!String.IsNullOrEmpty(this.Description))
                stream.WriteLine(CreateLine(ChartHeaderDescription, GetSerial(this.Description)));
            if (this.TimeRangeLastDays.HasValue)
                stream.WriteLine(CreateLine(ChartHeaderLastDays, GetSerial(this.TimeRangeLastDays)));
            else if (this.TimeRangeLastMonths.HasValue)
                stream.WriteLine(CreateLine(ChartHeaderLastMonths, GetSerial(this.TimeRangeLastMonths)));
            else
            {
                if (this.TimeRangeBegin.HasValue)
                    stream.WriteLine(CreateLine(ChartHeaderTimeBegin, GetSerial(this.TimeRangeBegin)));
                else if (this.TimeRangeEnd.HasValue)
                    stream.WriteLine(CreateLine(ChartHeaderTimeEnd, GetSerial(this.TimeRangeEnd)));
            }
            if (this.ChartEnableTimeZoom)
                stream.WriteLine(CreateLine(ChartHeaderEnableTimeZoom, GetSerial(true)));
            if (this.ChartAxisYRight)
                stream.WriteLine(CreateLine(ChartHeaderAxisYRight, GetSerial(true)));

        }

        private void SaveToFileSerie(StreamWriter stream, GraphSerieInfo serie)
        {
            serie.SaveToStream(stream);
        }

        private void SaveToFileLayout(StreamWriter stream)
        {
            string layout = ChartLayout;
            if (!String.IsNullOrEmpty(layout))
            {
                stream.WriteLine(ChartHeaderLayout);
                stream.Write(layout);
            }
        }

        public string FileName
        {
            get
            {
                string fileName = _FileName;
                if (!String.IsNullOrEmpty(fileName)) return fileName;
                string name = CreateValidFileName(this.Title, "_");
                string path = App.ConfigPath;
                return System.IO.Path.Combine(path, name + "." + _Extension);
            }
        }
        private string _FileName;
        public enum FileVersion { None, Version1 }
        private const string ChartHeaderFileV1 = "== BestInCovid v1.0 chart ==";
        private const string ChartHeaderTitle = "Title";
        private const string ChartHeaderOrder = "Order";
        private const string ChartHeaderDescription = "Description";
        private const string ChartHeaderLastDays = "LastDays";
        private const string ChartHeaderLastMonths = "LastMonths";
        private const string ChartHeaderTimeBegin = "TimeBegin";
        private const string ChartHeaderTimeEnd = "TimeEnd";
        private const string ChartHeaderEnableTimeZoom = "EnableTimeZoom";
        private const string ChartHeaderAxisYRight = "AxisYRight";
        private const string ChartHeaderLayout = "LayoutXml";
        private const string _Extension = "chart";
        internal const string ChartSeriesPrefix = "Serie.";
        #endregion
        #region Načtení dat grafu z databáze
        public GraphData LoadData(Database database)
        {
            if (database == null)
                throw new InvalidOperationException($"Nelze získat data grafu v metodě GraphInfo.LoadData(), protože není k dispozici databáze.");
            if (!database.HasData)
                throw new InvalidOperationException($"Nelze získat data grafu v metodě GraphInfo.LoadData(), protože databáze dosud nemá načtena data.");

            GraphData graphData = new GraphData(this);

            PrepareTimeRange(graphData);
            foreach (var serie in _Series)
                serie.LoadData(database, graphData);

            return graphData;
        }
        private void PrepareTimeRange(GraphData graphData)
        {
            graphData.DateBegin = null;
            graphData.DateEnd = null;

            if (TimeRangeBegin.HasValue || TimeRangeEnd.HasValue)
            {
                graphData.DateBegin = TimeRangeBegin;
                graphData.DateEnd = TimeRangeEnd;
            }
            else if (TimeRangeLastDays.HasValue && TimeRangeLastDays.Value > 0)
            {
                DateTime end = DateTime.Now.Date.AddDays(1d);
                DateTime begin = end.AddDays(-TimeRangeLastDays.Value);
                graphData.DateBegin = begin;
                graphData.DateEnd = end;
            }
            else if (TimeRangeLastMonths.HasValue && TimeRangeLastMonths.Value > 0)
            {
                DateTime end = DateTime.Now.Date.AddDays(1d);
                DateTime begin = end.AddMonths(-TimeRangeLastMonths.Value);
                begin = new DateTime(begin.Year, begin.Month, 1);
                graphData.DateBegin = begin;
                graphData.DateEnd = end;
            }
        }
        #endregion
        #region Layout grafu, generátor defaultního layoutu
        /// <summary>
        /// Vzhled grafu. Ve výchozím stavu generuje vhodný, po setování nenull udržuje setovaný layout. Po setování null vrací opět defaultní.
        /// </summary>
        public string ChartLayout { get { return _ChartLayout; } set { _ChartLayout = value; } }
        private string _ChartLayout;
        public string CreateDefaultLayout(GraphData graphData)
        {


            string sumSerie = "";
            foreach (var column in graphData.Columns)
            {
                string currSerie = CreateDefaultLayoutSerie(column);
                sumSerie += currSerie;
            }
            string currSeries = DefaultLayoutSeries;
            currSeries = currSeries.Replace("{{SERIES}}", sumSerie);

            string currLegend = DefaultLayoutLegend;

            string currTitles = DefaultLayoutTitles;
            currTitles = currTitles.Replace("{{TITLETEXT}}", this.Title);

            string currDiagram = DefaultLayoutDiagram;
            currDiagram = currDiagram.Replace("{{AXIS_INTERACTIVITY}}", (this.ChartEnableTimeZoom ? "EnableAxisXScrolling=\"true\" EnableAxisXZooming=\"true\" " : ""));
            currDiagram = currDiagram.Replace("{{STRIPS}}", "");
            currDiagram = currDiagram.Replace("{{AXISY_ALIGNMENT}}", this.ChartAxisYRight ? "Alignment=\"Far\" " : "");

            string currLayout = DefaultLayoutMain;
            currLayout = currLayout.Replace("{{SERIES}}", currSeries);
            currLayout = currLayout.Replace("{{LEGEND}}", currLegend);
            currLayout = currLayout.Replace("{{TITLES}}", currTitles);
            currLayout = currLayout.Replace("{{DIAGRAM}}", currDiagram);

            return currLayout;
        }
        protected static string CreateDefaultLayoutSerie(GraphColumn column)
        {
            var graphSerieInfo = column.GraphSerie;
            string currSerie = DefaultLayoutSerie;
            currSerie = currSerie.Replace("{{ITEMNAME}}", "Item" + (column.Index + 1).ToString());
            currSerie = currSerie.Replace("{{TITLE}}", column.Title);
            currSerie = currSerie.Replace("{{COLUMNNAME}}", column.Name);
            currSerie = currSerie.Replace("{{MARKER_VISIBILITY_ELEMENT}}", CreateElement("MarkerVisibility", false));
            currSerie = currSerie.Replace("{{LINECOLOR_ELEMENT}}", CreateElement("Color", graphSerieInfo.LineColor));
            currSerie = currSerie.Replace("{{ENABLE_ANTIALIASING_ELEMENT}}", CreateElement("EnableAntialiasing", true));
            currSerie = currSerie.Replace("{{LINE_THICK_ELEMENT}}", CreateElement("Thickness", graphSerieInfo.LineThickness));
            currSerie = currSerie.Replace("{{LINE_DASHSTYLE_ELEMENT}}", CreateElement("DashStyle", graphSerieInfo.LineDashStyleName));
            return currSerie;
        }
        protected static string CreateElement(string elementName, Boolean? value)
        {
            if (!value.HasValue) return "";
            return $"{elementName}=\"{(value.Value ? "True" : "False")}\" ";
        }
        protected static string CreateElement(string elementName, int? value)
        {
            if (!value.HasValue) return "";
            return $"{elementName}=\"{value.Value}\" ";
        }
        protected static string CreateElement(string elementName, string value)
        {
            if (value == null) return "";
            return $"{elementName}=\"{value}\" ";
        }
        protected static string CreateElement(string elementName, System.Drawing.Color? value)
        {
            if (!value.HasValue) return "";
            return $"{elementName}=\"{value.Value.R}, {value.Value.G}, {value.Value.B}\" ";
        }
        protected static string DefaultLayoutMain
        {
            get
            {
                string settings = @"﻿<?xml version='1.0' encoding='utf-8'?>
<ChartXmlSerializer version='20.1.4.0'>
  <Chart AppearanceNameSerializable='Default' SelectionMode='None' SeriesSelectionMode='Series'>
    <DataContainer ValidateDataMembers='true' BoundSeriesSorting='None'>
{{SERIES}}
      <SeriesTemplate CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText='' />
    </DataContainer>
{{LEGEND}}
{{TITLES}}
{{DIAGRAM}}
  </Chart>
</ChartXmlSerializer>";
                settings = settings.Replace("'", "\"");
                return settings;
            }
        }
        protected static string DefaultLayoutSeries
        {
            get
            {
                string settings = @"﻿      <SeriesSerializable>
{{SERIES}}
      </SeriesSerializable>
";
                settings = settings.Replace("'", "\"");
                return settings;
            }
        }
        protected static string DefaultLayoutSerie
        {
            get
            {
                string settings = @"﻿        <{{ITEMNAME}} Name='{{TITLE}}' DataSourceSorted='false' ArgumentDataMember='date' ValueDataMembersSerializable='{{COLUMNNAME}}' CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText=''>
          <View {{MARKER_VISIBILITY_ELEMENT}}{{ENABLE_ANTIALIASING_ELEMENT}}{{LINECOLOR_ELEMENT}}TypeNameSerializable='LineSeriesView'>
            <LineStyle {{LINE_THICK_ELEMENT}}{{LINE_DASHSTYLE_ELEMENT}} />
            <SeriesPointAnimation TypeNameSerializable='XYMarkerWidenAnimation' />
            <FirstPoint MarkerDisplayMode='Default' LabelDisplayMode='Default' TypeNameSerializable='SidePointMarker'>
              <Label Angle='180' TypeNameSerializable='PointSeriesLabel' TextOrientation='Horizontal' />
            </FirstPoint>
            <LastPoint MarkerDisplayMode='Default' LabelDisplayMode='Default' TypeNameSerializable='SidePointMarker'>
              <Label Angle='0' TypeNameSerializable='PointSeriesLabel' TextOrientation='Horizontal' />
            </LastPoint>
          </View>
        </{{ITEMNAME}}>
";
                settings = settings.Replace("'", "\"");
                return settings;
            }
        }
        protected static string DefaultLayoutLegend
        {
            get
            {
                string settings = @"﻿    <Legend HorizontalIndent='40' AlignmentHorizontal='Center' Direction='LeftToRight' CrosshairContentOffset='4' MarkerSize='@2,Width=45@2,Height=16' MarkerMode='CheckBox' MaxCrosshairContentWidth='50' MaxCrosshairContentHeight='0' Name='Default Legend' />
";
                settings = settings.Replace("'", "\"");
                return settings;
            }
        }
        protected static string DefaultLayoutTitles
        {
            get
            {
                string settings = @"    <Titles>
        <Item1 Text='{{TITLETEXT}}' Font='Tahoma, 18pt' TextColor='' Antialiasing='true' EnableAntialiasing='Default' />
    </Titles> ";
                settings = settings.Replace("'", "\"");
                return settings;
            }
        }
        protected static string DefaultLayoutDiagram
        {
            get
            {
                string settings = @"    <Diagram {{AXIS_INTERACTIVITY}}RuntimePaneCollapse='true' RuntimePaneResize='false' PaneLayoutDirection='Vertical' TypeNameSerializable='XYDiagram'>
      <AxisX StickToEnd='false' VisibleInPanesSerializable='-1' ShowBehind='false'>
{{STRIPS}}
        <WholeRange StartSideMargin='21.466666666666665' EndSideMargin='21.466666666666665' SideMarginSizeUnit='AxisUnit' />
      </AxisX>
      <AxisY {{AXISY_ALIGNMENT}}VisibleInPanesSerializable='-1' ShowBehind='false'>
        <WholeRange StartSideMargin='232.5' EndSideMargin='232.5' SideMarginSizeUnit='AxisUnit' />
      </AxisY>
      <SelectionOptions />
    </Diagram>";
                settings = settings.Replace("'", "\"");
                return settings;
            }
        }
        protected static string DefaultLayoutStrips
        {
            get
            {
                string settings = @"        <Strips>
{{STRIP}}
        </Strips>";
                settings = settings.Replace("'", "\"");
                return settings;
            }
        }
        protected static string DefaultLayoutStrip
        {
            get
            {
                string settings = @"          <Item1 Color='251, 213, 181' LegendText='Prázdniny' Name='Prázdniny'>
            <MinLimit AxisValueSerializable='01.07.2020 00:00:00.000' />
            <MaxLimit AxisValueSerializable='01.09.2020 00:00:00.000' />
            <FillStyle FillMode='Gradient'>
              <Options GradientMode='BottomToTop' Color2='242, 242, 242' TypeNameSerializable='RectangleGradientFillOptions' />
            </FillStyle>
          </Item1>
";
                settings = settings.Replace("'", "\"");
                return settings;
            }
        }
        #endregion
        #region DataValueType - vlastnosti, názvy
        public static DataVisualInfo GetValueTypeInfo(DataValueType valueType)
        {
            switch (valueType)
            {
                case DataValueType.NewCount: return new DataVisualInfo(valueType, "Denní počet nových případů", "Neupravený počet nově nalezených případů", null, GraphSerieAxisType.BigValuesLinear);
                case DataValueType.NewCountAvg: return new DataVisualInfo(valueType, "Průměrný denní přírůstek", "Počet nově nalezených případů, zprůměrovaný za okolních 7 dní (-3  +3 dny)", null, GraphSerieAxisType.BigValuesLinear);
                case DataValueType.NewCountRelative: return new DataVisualInfo(valueType, "Relativní přírůstek na 100tis obyvatel", "Počet nově nalezených případů, přepočtený na 100 000 obyvatel, vhodné k porovnání", null, GraphSerieAxisType.BigValuesLinear);
                case DataValueType.NewCountRelativeAvg: return new DataVisualInfo(valueType, "Relativní přírůstek na 100t, zprůměrovaný", "Počet nově nalezených případů, přepočtený na 100 000 obyvatel, zprůměrovaný za okolních 7 dní (-3  +3 dny)", null, GraphSerieAxisType.BigValuesLinear);
                case DataValueType.NewCount7DaySum: return new DataVisualInfo(valueType, "Součet za posledních 7 dní", "", null, GraphSerieAxisType.BigValuesLinear);
                case DataValueType.NewCount7DaySumAvg: return new DataVisualInfo(valueType, "Součet za posledních 7 dní, průměrovaný", "", null, GraphSerieAxisType.BigValuesLinear);
                case DataValueType.NewCount7DaySumRelative: return new DataVisualInfo(valueType, "Součet za týden na 100tis obyvatel", "", null, GraphSerieAxisType.BigValuesLinear);
                case DataValueType.NewCount7DaySumRelativeAvg: return new DataVisualInfo(valueType, "Součet za týden na 100tis obyvatel, průměrovaný", "", null, GraphSerieAxisType.BigValuesLinear);
                case DataValueType.CurrentCount: return new DataVisualInfo(valueType, "Aktuální stav případů", "", null, GraphSerieAxisType.BigValuesLinear);
                case DataValueType.CurrentCountAvg: return new DataVisualInfo(valueType, "Aktuální stav, průměr za 7 dní", "", null, GraphSerieAxisType.BigValuesLinear);
                case DataValueType.CurrentCountRelative: return new DataVisualInfo(valueType, "Aktuální stav případů na 100tis obyvatel", "", null, GraphSerieAxisType.BigValuesLinear);
                case DataValueType.CurrentCountRelativeAvg: return new DataVisualInfo(valueType, "Aktuální stav případů na 100tis obyvatel, průměr za 7 dní", "", null, GraphSerieAxisType.BigValuesLinear);
                case DataValueType.RZero: return new DataVisualInfo(valueType, "Reprodukční číslo R0", "", null, GraphSerieAxisType.SmallValuesLinear);
                case DataValueType.RZeroAvg: return new DataVisualInfo(valueType, "Reprodukční číslo R0, průměr za 7dní", "", null, GraphSerieAxisType.SmallValuesLinear);
                default: return new DataVisualInfo(valueType, valueType.ToString(), "", null, GraphSerieAxisType.BigValuesLinear);
            }
        }
        public enum GraphSerieAxisType
        {
            None,
            SmallValuesLinear,
            BigValuesLinear,
            SmallValuesLogarithmic,
            BigValuesLogarithmic
        }
        #endregion
    }
    /// <summary>
    /// Definice jedné serie dat grafu = jedna čára s daty
    /// </summary>
    public class GraphSerieInfo : DataSerializable
    {
        public GraphSerieInfo()
        {
            ValueType = DataValueType.CurrentCount;
        }
        /// <summary>
        /// Parent graf
        /// </summary>
        public GraphInfo Parent { get; set; }
        /// <summary>
        /// Název serie
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Plný kód entity
        /// </summary>
        public string DataEntityCode { get; set; }
        /// <summary>
        /// Druh dat
        /// </summary>
        public DataValueType ValueType { get; set; }
        /// <summary>
        /// Detaily o aktuálním typu hodnoty <see cref="ValueType"/>
        /// </summary>
        public DataVisualInfo ValueTypeInfo { get { return GraphInfo.GetValueTypeInfo(this.ValueType); } }
        /// <summary>
        /// Barva čáry explicitně zadaná
        /// </summary>
        public System.Drawing.Color? LineColor { get; set; }
        /// <summary>
        /// Síla čáry explicitně zadaná
        /// </summary>
        public int? LineThickness { get; set; }
        /// <summary>
        /// Styl tečkování čáry
        /// </summary>
        public LineDashStyleType? LineDashStyle { get; set; }
        /// <summary>
        /// Styl tečkování čáry, název do grafu
        /// </summary>
        public string LineDashStyleName
        {
            get
            {
                var style = LineDashStyle;
                if (!style.HasValue || style.Value == LineDashStyleType.None) return null;
                if (style.Value == LineDashStyleType.Full) return null;                       // Pro plnou čáru se hodnota do XML nedává, ta je default
                return style.Value.ToString();
            }
        }
        #region Načtení ze souboru a uložení definice této serie do souboru

        public static GraphSerieInfo LoadFromStream(StreamReader stream, GraphInfo.FileVersion fileVersion, ref string line)
        {
            if (stream == null || stream.EndOfStream) return null;

            GraphSerieInfo serieInfo = null;

            bool hasTitle = false;
            while (!stream.EndOfStream)
            {
                if (line == null) line = stream.ReadLine();

                // Pokud najdu řádek, který nemá prefix Serie, pak jej nebudu zpracovávat = jde o něco za seriemi - a skončíme:
                if (!line.StartsWith(GraphInfo.ChartSeriesPrefix)) break;

                // Pokud najdu řádek s titulkem serie v situaci, kdy už náš titulek máme, pak jde o titulek následující serie - a zdejší instance skončí:
                bool isTitle = (line.StartsWith(ChartSeriesTitle));
                if (hasTitle && isTitle) break;

                // Je to řádek pro serii, a tedy pro zdejší instanci:
                if (!hasTitle && isTitle) hasTitle = true;

                // Mám data pro Serie, ale nemám cílový objekt => vytvořím new instanci:
                if (serieInfo == null) serieInfo = new GraphSerieInfo();
                serieInfo.LoadFromStreamHeaderLine(stream, fileVersion, ref line);
            }

            return serieInfo;
        }
        private void LoadFromStreamHeaderLine(StreamReader stream, GraphInfo.FileVersion fileVersion, ref string line)
        {
            string name = LoadNameValueFromString(line, out string text);
            switch (name)
            {
                case ChartSeriesTitle:
                    this.Title = GetValue(text, "");
                    break;
                case ChartSeriesEntityCode:
                    this.DataEntityCode = GetValue(text, "");
                    break;
                case ChartSeriesValueType:
                    this.ValueType = GetValueEnum<DataValueType>(text, DataValueType.None);
                    break;
                case ChartSeriesLineColor:
                    this.LineColor = GetValue(text, (System.Drawing.Color?)null);
                    break;
                case ChartSeriesLineThickness:
                    this.LineThickness = GetValue(text, (int?)null);
                    break;
                case ChartSeriesLineDashStyle:
                    this.LineDashStyle = GetValueEnum<LineDashStyleType>(text, (LineDashStyleType?)null);
                    break;
            }
            line = null;
        }
        internal void SaveToStream(StreamWriter stream)
        {
            stream.WriteLine(CreateLine(ChartSeriesTitle, GetSerial(this.Title)));
            stream.WriteLine(CreateLine(ChartSeriesEntityCode, GetSerial(this.DataEntityCode)));
            stream.WriteLine(CreateLine(ChartSeriesValueType, GetSerialEnum(this.ValueType)));
            if (this.LineColor.HasValue)
                stream.WriteLine(CreateLine(ChartSeriesLineColor, GetSerial(this.LineColor)));
            if (this.LineThickness.HasValue)
                stream.WriteLine(CreateLine(ChartSeriesLineThickness, GetSerial(this.LineThickness)));
            if (this.LineDashStyle.HasValue)
                stream.WriteLine(CreateLine(ChartSeriesLineDashStyle, GetSerialEnum(this.LineDashStyle)));
        }
        private const string ChartSeriesTitle = GraphInfo.ChartSeriesPrefix + "Title";
        private const string ChartSeriesEntityCode = GraphInfo.ChartSeriesPrefix + "EntityCode";
        private const string ChartSeriesValueType = GraphInfo.ChartSeriesPrefix + "ValueType";
        private const string ChartSeriesLineColor = GraphInfo.ChartSeriesPrefix + "LineColor";
        private const string ChartSeriesLineThickness = GraphInfo.ChartSeriesPrefix + "LineThickness";
        private const string ChartSeriesLineDashStyle = GraphInfo.ChartSeriesPrefix + "LineDashStyle";
        #endregion
        #region Načtení dat grafu pro tuto sérii z databáze
        /// <summary>
        /// Načte data z databáze <see cref="Database"/> podle definice v této serii a uloží je do nového sloupce v dodaném objektu <paramref name="graphData"/> pro data grafu
        /// </summary>
        /// <param name="database"></param>
        /// <param name="graphData"></param>
        public void LoadData(Database database, GraphData graphData)
        {
            string fullCode = this.DataEntityCode;
            var entity = database.GetEntity(fullCode);
            if (entity is null) return;

            var column = graphData.AddColumn(this, entity);

            var items = database.GetResult(entity, this.ValueType, graphData.DateBegin, graphData.DateEnd);
            foreach (var item in items)
                graphData.AddCell(item.Date, column, item.Value);
        }
        #endregion
    }
    public enum LineDashStyleType
    {
        None,
        Full,
        Dot,
        Dash,
        DashDot,
        DashDotDot
    }
    #endregion
    #region GraphStripInfo
    /// <summary>
    /// Data o jednom pruhu v rámci grafu. Pruh vyznačuje určité pásmo - časové období, nebo rozsah vhodných hodnot.
    /// </summary>
    /// <typeparam name="T">Datový typ hodnoty: datum, decimal...</typeparam>
    public class GraphStripInfo<T>
    {
        /// <summary>
        /// Název pruhu
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Dolní hodnota = kde pruh začíná
        /// </summary>
        public T MinLimit { get; set; }
        /// <summary>
        /// Horní hodnota = kde pruh končí
        /// </summary>
        public T MaxLimit { get; set; }
        /// <summary>
        /// Barva celého pruhu, nebo barva počátku u Gradientu
        /// </summary>
        public System.Drawing.Color Color1 { get; set; }
        /// <summary>
        /// Barva konce pruhu - u Gradientu
        /// </summary>
        public System.Drawing.Color? Color2 { get; set; }
    }
    #endregion
    #region GraphData : Načtená aktuální data o jednom celém garfu (data grafu, sloupce za jednotlivé serie, řádky za jednotlivé datumy)
    /// <summary>
    /// Data pro jeden graf shrnutá z definice grafu <see cref="GraphInfo"/>, ze všech jeho serií <see cref="GraphSerieInfo"/>, načtená z databáze <see cref="Database"/>.
    /// Obsahuje sloupce <see cref="Columns"/> za jednotlivé serie grafu, obsahuje řádky <see cref="Rows"/> za jednotlvié datumy, obsahující konkrétní hodnoty.
    /// Property <see cref="DataTable"/> vygeneruje new tabulku pro zobrazení v grafu.
    /// </summary>
    public class GraphData
    {
        public GraphData(GraphInfo graphInfo)
        {
            this.GraphInfo = graphInfo;
            this._Columns = new Dictionary<int, GraphColumn>();
            this._Rows = new Dictionary<int, GraphRow>();
            this.ColumnId = 0;
        }
        private int ColumnId;
        /// <summary>
        /// Definice grafu, podle které jsou načtena data
        /// </summary>
        public GraphInfo GraphInfo { get; private set; }
        public DateTime? DateBegin { get; set; }
        public DateTime? DateEnd { get; set; }
        public System.Data.DataTable DataTable { get { return CreateDataTable(); } }
        public GraphColumn AddColumn(GraphSerieInfo serie, Database.IEntity entity)
        {
            int index = ColumnId++;
            string columnName = "column" + index.ToString();
            GraphColumn column = new GraphColumn(this, index, columnName, serie, entity);
            this._Columns.AddOrUpdate(index, column);
            return column;
        }
        public void AddCell(DateTime date, GraphColumn column, object value)
        {
            int dateKey = date.GetDateKey();
            GraphRow row = _Rows.AddOrCreate(dateKey, () => new GraphRow(this, date));
            row.AddCell(column, value);
        }
        public GraphColumn[] Columns { get { return _Columns.GetSortedValues((a,b) => a.Index.CompareTo(b.Index)); } }
        public GraphRow[] Rows { get { return _Rows.GetSortedValues((a, b) => a.Date.CompareTo(b.Date)); } }
        private Dictionary<int, GraphColumn> _Columns;
        private Dictionary<int, GraphRow> _Rows;
        private System.Data.DataTable CreateDataTable()
        {
            System.Data.DataTable dataTable = new System.Data.DataTable("GraphData");

            System.Data.DataColumn dataColumn = CreateDataColumn("date", "Datum", typeof(DateTime));
            dataTable.Columns.Add(dataColumn);
            foreach (var column in this.Columns)
            {
                dataColumn = CreateDataColumn(column.Name, column.Title, typeof(decimal));
                dataTable.Columns.Add(dataColumn);
            }

            foreach (var row in Rows)
            {
                dataTable.Rows.Add(row.Cells);
            }

            return dataTable;
        }

        private DataColumn CreateDataColumn(string columnName, string caption, Type dataType)
        {
            DataColumn dataColumn = new DataColumn(columnName);
            dataColumn.AllowDBNull = false;
            dataColumn.Caption = caption;
            dataColumn.DataType = dataType;
            return dataColumn;
        }
    }
    /// <summary>
    /// Jeden sloupec, reprezentuje jednu serii grafu
    /// </summary>
    public class GraphColumn
    {
        public GraphColumn(GraphData graphData, int columnIndex, string columnName, GraphSerieInfo graphSerie, Database.IEntity dataEntity)
        {
            this.GraphData = graphData;
            Index = columnIndex;
            Name = columnName;
            this.GraphSerie = graphSerie;
            this.DataEntity = dataEntity;
        }
        public override string ToString()
        {
            return $"Column[{Index}]: {Name} = \"{Title}\"";
        }
        public GraphData GraphData { get; private set; }
        public int Index { get; private set; }
        public string Name { get; set; }
        public GraphSerieInfo GraphSerie { get; private set; }
        public Database.IEntity DataEntity { get; private set; }
        public string Title { get { return this.GraphSerie.Title; } }
        public string Description { get; set; }
        public string EntityFullCode { get { return this.DataEntity.FullCode; } }
        public string EntityName { get { return this.DataEntity.Nazev; } }
        public string EntityInfo { get { return this.DataEntity.Text; } }
    }
    /// <summary>
    /// Jeden řádek s daty grafu
    /// </summary>
    public class GraphRow
    {
        public GraphRow(GraphData graphData, DateTime date)
        {
            this.GraphData = graphData;
            this.Date = date;
            this._Cells = new Dictionary<int, object>();
        }
        public override string ToString()
        {
            string text = this.Date.ToString("yyyy-MM-dd");
            var cells = Cells;
            foreach (var cell in cells)
                text += " | " + (cell == null ? "NULL" : cell.ToString());
            return text;
        }
        public GraphData GraphData { get; private set; }
        public DateTime Date { get; private set; }
        public int DateKey { get { return Date.GetDateKey(); } }
        public object[] Cells
        {
            get
            {
                List<object> cells = new List<object>();
                cells.Add(this.Date);
                var columns = this.GraphData.Columns;
                foreach (var column in columns)
                {
                    int index = column.Index;
                    if (_Cells.TryGetValue(index, out object value)) cells.Add(value);
                    else cells.Add(null);
                }
                return cells.ToArray();
            }
        }
        private Dictionary<int, object> _Cells;
        public void AddCell(GraphColumn column, object value)
        {
            _Cells.AddOrUpdate(column.Index, value);
        }
    }
    #endregion
}
