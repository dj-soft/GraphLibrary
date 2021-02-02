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
        #region Konstruktor, vnitřní proměnné
        /// <summary>
        /// Konstruktor pro definici grafu
        /// </summary>
        public GraphInfo()
        {
            this.Id = NextId++;
            Init();
            this.Clear();
        }
        /// <summary>
        /// Vizualizce
        /// </summary>
        /// <returns></returns>
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
        /// Mají se zobrazit společné časové pruhy?
        /// </summary>
        public bool EnableCommonTimeStripes { get; set; }
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
                string pattern = "*." + FileExtension;
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
        #region Načtení ze souboru a uložení do souboru, serializace
        /// <summary>
        /// Uloží tento graf do jeho souboru na disk, pro budoucí načtení.
        /// </summary>
        /// <param name="fileName"></param>
        public void SaveToFile(string fileName = null)
        {
            string data = this.Serial;
            if (fileName == null) fileName = this.CurrentFileName;
            File.WriteAllText(fileName, data, Encoding.UTF8);
            _FileName = fileName;
        }
        /// <summary>
        /// Pokud this graf je uložen v souboru, smaže tento soubor.
        /// Volá se v procesu smazání grafu.
        /// </summary>
        public void DeleteGraphFile()
        {
            string fileName = this.GraphFileName;
            if (String.IsNullOrEmpty(fileName) || !File.Exists(fileName)) return;
            File.Decrypt(fileName);
        }
        /// <summary>
        /// Z dodaného textu (Serial) vytvoří a vrátí new instanci.
        /// Vrácená instance nemá vztah k podkladovému souboru, z něhož byla načtena data (serializovaná data neobsahují fyzický souor na disku).
        /// </summary>
        /// <param name="serial"></param>
        /// <returns></returns>
        public static GraphInfo LoadFromSerial(string serial)
        {
            if (String.IsNullOrEmpty(serial)) return null;

            GraphInfo graphInfo = new GraphInfo();
            graphInfo.Serial = serial;
            graphInfo._FileName = null;
            return graphInfo;
        }
        /// <summary>
        /// Z daného souboru načte a vytvoří celou definici grafu
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static GraphInfo LoadFromFile(string fileName)
        {
            if (String.IsNullOrEmpty(fileName) || !System.IO.File.Exists(fileName)) return null;
            string data = File.ReadAllText(fileName, Encoding.UTF8);
            if (String.IsNullOrEmpty(data)) return null;

            GraphInfo graphInfo = new GraphInfo();
            graphInfo.Serial = data;
            graphInfo._FileName = fileName;
            return graphInfo;
        }
        /// <summary>
        /// Serializovaný obraz celé this instance
        /// </summary>
        public string Serial
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                using (StringWriter stream = new StringWriter(sb))
                {
                    _SaveToStream(stream);
                }
                return sb.ToString();
            }
            set
            {
                this.Clear();
                string serial = value;
                if (!String.IsNullOrEmpty(serial))
                {
                    using (StringReader stream = new StringReader(serial))
                    {
                        _LoadFromStream(stream);
                    }
                }
            }
        }
        /// <summary>
        /// Vymaže všechna data
        /// </summary>
        public void Clear()
        {
            this.Title = "";
            this.Order = 0;
            this.Description = "";
            this.TimeRangeLastDays = null;
            this.TimeRangeLastMonths = null;
            this.TimeRangeBegin = null;
            this.TimeRangeEnd = null;
            this.ChartEnableTimeZoom = true;
            this.ChartAxisYRight = false;
            this.EnableCommonTimeStripes = true;

            _Series.Clear();
            _ChartLayout = null;
            _FileName = null;
        }
        /// <summary>
        /// Jméno souboru grafu, z něhož je načten. Pokud dosud není uložen, je null. I v takovém případě bude možno jej uložit, vygeneruje si svoje jméno souboru.
        /// </summary>
        public string GraphFileName { get { return _FileName; } }
        #region Privátní Load
        /// <summary>
        /// Do this instance načte plná data z daného streamu
        /// </summary>
        /// <param name="stream"></param>
        private void _LoadFromStream(StringReader stream)
        {
            FileVersion fileVersion = FileVersion.None;
            string line = null;
            while (stream.Peek() > 0)
            {
                if (line == null) line = stream.ReadLine();
                if (line == null) break;

                if (fileVersion == FileVersion.None)
                {   // Hledám header s označením verze:
                    if (line == ChartHeaderFileV1) fileVersion = FileVersion.Version1;
                    line = null;
                }
                else if (fileVersion == FileVersion.Version1)
                {   // Zpracovávám verzi 1:
                    if (line == ChartHeaderLayout)
                    {   // Jde o poslední prvek = následuje kompletní layout grafu:
                        this.LoadFromStreamLayout(stream, fileVersion, ref line);
                    }
                    else if (line.StartsWith(ChartSeriesPrefix))
                    {   // Řádek začíná prefixem pro jednotlivou serii => zpracujeme serii:
                        this.LoadFromStreamSerie(stream, fileVersion, ref line);
                    }
                    else
                    {   // Jinak to nejspíš bude jednotlivý řádek obsahující property do hlavičky grafu:
                        this.LoadFromStreamHeaderLine(stream, fileVersion, ref line);
                    }
                }
            }
        }
        /// <summary>
        /// Do this instance načte data hlavičky z daného streamu
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fileVersion"></param>
        /// <param name="line"></param>
        private void LoadFromStreamHeaderLine(StringReader stream, FileVersion fileVersion, ref string line)
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
                case ChartHeaderEnableCommonTimeStripes:
                    this.EnableCommonTimeStripes = GetValue(text, false);
                    break;
                    // Nové prvky přidávej i do Clear() !
            }

            line = null;
        }
        /// <summary>
        /// Do this instance načte všechny položky Series z daného streamu
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fileVersion"></param>
        /// <param name="line"></param>
        private void LoadFromStreamSerie(StringReader stream, FileVersion fileVersion, ref string line)
        {
            this._Series.Clear();
            while (stream.Peek() > 0)
            {
                GraphSerieInfo serie = GraphSerieInfo.LoadFromStream(stream, fileVersion, ref line);
                if (serie == null) break;
                this.AddSerie(serie);
            }
        }
        /// <summary>
        /// Do this instance načte data layoutu grafu z daného streamu
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fileVersion"></param>
        /// <param name="line"></param>
        private void LoadFromStreamLayout(StringReader stream, FileVersion fileVersion, ref string line)
        {
            this.ChartLayout = stream.ReadToEnd();
            line = null;
        }
        #endregion
        #region Privátní Save
        /// <summary>
        /// Uloží celý tento graf do daného streamu.
        /// </summary>
        private void _SaveToStream(StringWriter stream)
        {
            string fileName = CurrentFileName;

            stream.WriteLine(ChartHeaderFileV1);
            SaveToFileHeader(stream);
            SaveToFileSeries(stream);
            SaveToFileLayout(stream);

            _FileName = fileName;
        }
        /// <summary>
        /// Do daného streamu vepíše vlastnosti hlavičky grafu. Nikoli položky, a nikoli layout.
        /// </summary>
        /// <param name="stream"></param>
        private void SaveToFileHeader(StringWriter stream)
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
            if (this.EnableCommonTimeStripes)
                stream.WriteLine(CreateLine(ChartHeaderEnableCommonTimeStripes, GetSerial(true)));

        }
        /// <summary>
        /// Do daného streamu vepíše vlastnosti všech položek grafu. Nikoli hlavičku, a nikoli layout.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="serie"></param>
        private void SaveToFileSeries(StringWriter stream)
        {
            foreach (var serie in Series)
                serie.SaveToStream(stream);
        }
        /// <summary>
        /// Do daného streamu vepíše layout grafu, pokud není defaultní.
        /// </summary>
        /// <param name="stream"></param>
        private void SaveToFileLayout(StringWriter stream)
        {
            string layout = ChartLayout;
            if (!String.IsNullOrEmpty(layout))
            {
                stream.WriteLine(ChartHeaderLayout);
                stream.Write(layout);
            }
        }
        #endregion
        /// <summary>
        /// Jméno souboru grafu aktuální = buď reálné, nebo nově vygenerované pro ukládání.
        /// </summary>
        protected string CurrentFileName
        {
            get
            {
                string fileName = _FileName;
                if (!String.IsNullOrEmpty(fileName)) return fileName;
                string name = CreateValidFileName(this.Title, "_");
                string path = App.ConfigPath;
                return System.IO.Path.Combine(path, name + "." + FileExtension);
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
        private const string ChartHeaderEnableCommonTimeStripes = "EnableCommonTimeStripes";
        private const string ChartHeaderLayout = "LayoutXml";
        internal const string ChartSeriesPrefix = "Serie.";
        private const string FileExtension = "chart";
        #endregion
        #region Načtení dat grafu z databáze
        public GraphData LoadData(DatabaseInfo database)
        {
            if (database == null)
                throw new InvalidOperationException($"Nelze získat data grafu v metodě GraphInfo.LoadData(), protože není k dispozici databáze.");
            if (!database.HasData)
                throw new InvalidOperationException($"Nelze získat data grafu v metodě GraphInfo.LoadData(), protože databáze dosud nemá načtena data.");

            GraphData graphData = new GraphData(this);

            PrepareTimeRange(graphData);
            foreach (var serie in _Series)
                serie.LoadData(database, graphData);

            graphData.FinaliseLoading();

            return graphData;
        }
        /// <summary>
        /// Připraví reálný datový rozsah pro graf: naplní hodnoty <see cref="GraphData.DateBegin"/>, <see cref="GraphData.DateEnd"/>.
        /// </summary>
        /// <param name="graphData"></param>
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
        /// Vzhled grafu. Ve výchozím stavu je null, pak si aplikace má vyžádat defaultní layout z metody <see cref="CreateDefaultLayout(GraphData)"/>.
        /// Aplikace může tento layout použít, následně editovat, a pak uložit sem do <see cref="ChartLayout"/>, odkud bude uložen na disk a příště bude použit již editovaný layout.
        /// Aplikace může vždy vyžádat aktuální defaultní layout, vhodné například po změně dat.
        /// </summary>
        public string ChartLayout { get { return _ChartLayout; } set { _ChartLayout = value; } }
        private string _ChartLayout;
        /// <summary>
        /// Vygeneruje defaultní layout. Potřebuje k tomu plná data grafu, nejen definici. Definice serií grafu je uložena ve sloupcích dat.
        /// </summary>
        /// <param name="graphData"></param>
        /// <returns></returns>
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

            string currStrips = "";
            if (this.EnableCommonTimeStripes)
            {
            }

            string currDiagram = DefaultLayoutDiagram;
            currDiagram = currDiagram.Replace("{{AXIS_INTERACTIVITY}}", (this.ChartEnableTimeZoom ? "EnableAxisXScrolling=\"true\" EnableAxisXZooming=\"true\" " : ""));
            currDiagram = currDiagram.Replace("{{STRIPS}}", currStrips);
            currDiagram = currDiagram.Replace("{{AXISY_ALIGNMENT}}", this.ChartAxisYRight ? "Alignment=\"Far\" " : "");

            string currLayout = DefaultLayoutMain;
            currLayout = currLayout.Replace("{{SERIES}}", currSeries);
            currLayout = currLayout.Replace("{{LEGEND}}", currLegend);
            currLayout = currLayout.Replace("{{TITLES}}", currTitles);
            currLayout = currLayout.Replace("{{DIAGRAM}}", currDiagram);

            return currLayout;
        }
        /// <summary>
        /// Vytvoří a vrátí definici layoutu za jednu serii = jeden sloupec dat grafu
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        protected static string CreateDefaultLayoutSerie(GraphColumn column)
        {
            var graphSerieInfo = column.GraphSerie;
            string currSerie = DefaultLayoutSerie;
            currSerie = currSerie.Replace("{{ITEMNAME}}", "Item" + (column.Index + 1).ToString());
            currSerie = currSerie.Replace("{{TITLE}}", column.Title);
            currSerie = currSerie.Replace("{{COLUMNNAME}}", column.ColumnNameData);
            currSerie = currSerie.Replace("{{MARKER_VISIBILITY_ELEMENT}}", CreateElement("MarkerVisibility", false));
            currSerie = currSerie.Replace("{{LINECOLOR_ELEMENT}}", CreateElement("Color", graphSerieInfo.LineColor));
            currSerie = currSerie.Replace("{{ENABLE_ANTIALIASING_ELEMENT}}", CreateElement("EnableAntialiasing", true));
            currSerie = currSerie.Replace("{{LINE_JOIN_ELEMENT}}", CreateElement("LineJoin", "Bevel"));            // Druh vykreslení zalomení čáry: Mitter = ostrý (neuvádí se), Round = kulatý roh, Bevel = Zkosení (ostrý oříznutý), MiterClipped = Hodně ostrý
            currSerie = currSerie.Replace("{{LINE_THICK_ELEMENT}}", CreateElement("Thickness", graphSerieInfo.LineThickness));
            currSerie = currSerie.Replace("{{LINE_DASHSTYLE_ELEMENT}}", CreateElement("DashStyle", graphSerieInfo.LineDashStyleName));
            return currSerie;
        }
        protected static string CreateElement(string elementName, string value)
        {
            if (value == null) return "";
            string xmlString = System.Web.HttpUtility.HtmlEncode(value);
            return $"{elementName}=\"{xmlString}\" ";
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
            <LineStyle {{LINE_JOIN_ELEMENT}}{{LINE_THICK_ELEMENT}}{{LINE_DASHSTYLE_ELEMENT}} />
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
    }
    #endregion
    #region GraphSerieInfo : definice jedné řady v grafu (zdroj dat, typ dat, další vlastnosti)
    /// <summary>
    /// Definice jedné serie dat grafu = jedna čára s daty
    /// </summary>
    public class GraphSerieInfo : DataSerializable
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
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
        /// Plný kód entity = definice místa, jehož data se načítají.
        /// <para/>
        /// Funguje podobně jako plná cesta k adresáři, může obsahovat např. jen "CZ", pak určuje celou Českou republiku; nebo "CZ.CZ053.CZ0531", pak určuje okres Chrudim,
        /// anebo "CZ.CZ053.CZ0531.5304.53043.571164", pouze samotná Chrudim na nejnižší úrovni = bez okolních obcí.
        /// </summary>
        public string DataEntityCode { get; set; }
        /// <summary>
        /// Druh dat, která se budou zobrazovat. Určuje zdroj dat (počet nových případů za den, aktuální počet nemocných) i jejich agregaci (průměr za 7 dní, poměr k počtu obyvatel).
        /// </summary>
        public DataValueType ValueType { get; set; }
        /// <summary>
        /// Detaily o aktuálním typu hodnoty <see cref="ValueType"/>
        /// </summary>
        public DataValueTypeInfo ValueTypeInfo { get { return DataValueTypeInfo.CreateFor(this.ValueType); } }
        /// <summary>
        /// Filtovat data podle velikosti obcí: načítat jen z obcí, jejichž Počet obyvatel je rovný nebo větší této hodnotě
        /// </summary>
        public int? FiltrPocetObyvatelOd { get; set; }
        /// <summary>
        /// Filtovat data podle velikosti obcí: načítat jen z obcí, jejichž Počet obyvatel je menší než tato hodnota
        /// </summary>
        public int? FiltrPocetObyvatelDo { get; set; }
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
                if (style.Value == LineDashStyleType.Solid) return null;                       // Pro plnou čáru se hodnota do XML nedává, ta je default
                return style.Value.ToString();
            }
        }
        #region Načtení ze souboru a uložení definice této serie do souboru
        public static GraphSerieInfo LoadFromStream(StringReader stream, GraphInfo.FileVersion fileVersion, ref string line)
        {
            if (stream == null || stream.Peek() == 0) return null;

            GraphSerieInfo serieInfo = null;

            bool hasTitle = false;
            while (stream.Peek() != 0)
            {
                if (line == null) line = stream.ReadLine();
                if (line == null) break;

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
        private void LoadFromStreamHeaderLine(StringReader stream, GraphInfo.FileVersion fileVersion, ref string line)
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
                case ChartSeriesFiltrPocetOd:
                    this.FiltrPocetObyvatelOd = GetValue(text, (int?)0);
                    break;
                case ChartSeriesFiltrPocetDo:
                    this.FiltrPocetObyvatelDo = GetValue(text, (int?)0);
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
        internal void SaveToStream(StringWriter stream)
        {
            stream.WriteLine(CreateLine(ChartSeriesTitle, GetSerial(this.Title)));
            stream.WriteLine(CreateLine(ChartSeriesEntityCode, GetSerial(this.DataEntityCode)));
            stream.WriteLine(CreateLine(ChartSeriesValueType, GetSerialEnum(this.ValueType)));
            if (this.FiltrPocetObyvatelOd.HasValue)
                stream.WriteLine(CreateLine(ChartSeriesFiltrPocetOd, GetSerial(this.FiltrPocetObyvatelOd)));
            if (this.FiltrPocetObyvatelDo.HasValue)
                stream.WriteLine(CreateLine(ChartSeriesFiltrPocetDo, GetSerial(this.FiltrPocetObyvatelDo)));
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
        private const string ChartSeriesFiltrPocetOd = GraphInfo.ChartSeriesPrefix + "FiltrPocetOd";
        private const string ChartSeriesFiltrPocetDo = GraphInfo.ChartSeriesPrefix + "FiltrPocetDo";
        private const string ChartSeriesLineColor = GraphInfo.ChartSeriesPrefix + "LineColor";
        private const string ChartSeriesLineThickness = GraphInfo.ChartSeriesPrefix + "LineThickness";
        private const string ChartSeriesLineDashStyle = GraphInfo.ChartSeriesPrefix + "LineDashStyle";
        #endregion
        #region Načtení dat grafu pro tuto sérii z databáze
        /// <summary>
        /// Načte data z databáze <see cref="DatabaseInfo"/> podle definice v této serii a uloží je do nového sloupce v dodaném objektu <paramref name="graphData"/> pro data grafu
        /// </summary>
        /// <param name="database"></param>
        /// <param name="graphData"></param>
        public void LoadData(DatabaseInfo database, GraphData graphData)
        {
            string fullCode = this.DataEntityCode;
            var entity = database.GetEntity(fullCode);
            if (entity is null) return;

            var result = database.GetResult(entity, this.ValueType, this.ValueTypeInfo, graphData.DateBegin, graphData.DateEnd, this.FiltrPocetObyvatelOd, this.FiltrPocetObyvatelDo);
            var column = graphData.AddColumn(this, entity);
            if (result != null)
            {
                foreach (var item in result.Results)
                    graphData.AddCell(item.Date, column, item.Value);
                graphData.AddCount(result.ScanRecordCount, result.LoadRecordCount, result.ShowRecordCount);
            }
        }
        #endregion
    }
    #endregion
    #region DataValueType, DataValueTypeInfo : Typy dat - druhy informací
    /// <summary>
    /// Informace o jednom typu dat v grafu
    /// </summary>
    public class DataValueTypeInfo : DataVisualInfo
    {
        /// <summary>
        /// Vrátí pole informací o všech typech
        /// </summary>
        /// <returns></returns>
        public static DataValueTypeInfo[] CreateAll()
        {
            List<DataValueTypeInfo> result = new List<DataValueTypeInfo>();
            result.Add(DataValueTypeInfo.CreateFor(DataValueType.NewCount));
            result.Add(DataValueTypeInfo.CreateFor(DataValueType.NewCountAvg));
            result.Add(DataValueTypeInfo.CreateFor(DataValueType.NewCountRelative));
            result.Add(DataValueTypeInfo.CreateFor(DataValueType.NewCountRelativeAvg));
            result.Add(DataValueTypeInfo.CreateFor(DataValueType.NewCount7DaySum));
            result.Add(DataValueTypeInfo.CreateFor(DataValueType.NewCount7DaySumAvg));
            result.Add(DataValueTypeInfo.CreateFor(DataValueType.NewCount7DaySumRelative));
            result.Add(DataValueTypeInfo.CreateFor(DataValueType.NewCount7DaySumRelativeAvg));
            result.Add(DataValueTypeInfo.CreateFor(DataValueType.CurrentCount));
            result.Add(DataValueTypeInfo.CreateFor(DataValueType.CurrentCountAvg));
            result.Add(DataValueTypeInfo.CreateFor(DataValueType.CurrentCountRelative));
            result.Add(DataValueTypeInfo.CreateFor(DataValueType.CurrentCountRelativeAvg));
            result.Add(DataValueTypeInfo.CreateFor(DataValueType.RZero));
            result.Add(DataValueTypeInfo.CreateFor(DataValueType.RZeroAvg));

            return result.ToArray();
        }
        /// <summary>
        /// Statický konstruktor
        /// </summary>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static DataValueTypeInfo CreateFor(DataValueType valueType)
        {
            if (TryCreateFor(valueType, out DataValueTypeInfo result))
                return result;

            throw new KeyNotFoundException($"Nelze vytvořit data {nameof(DataValueTypeInfo)} pro valueType = {valueType}, není vytvořen segment kódu.");
        }
        /// <summary>
        /// Statický konstruktor
        /// </summary>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static bool TryCreateFor(DataValueType valueType, out DataValueTypeInfo result)
        {
            switch (valueType)
            {
                case DataValueType.NewCount:
                    result = new DataValueTypeInfo(valueType, "Denní počet nových případů", "Neupravený počet nově nalezených případů", 
                    GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Dot,
                    0, 0);
                    return true;
                case DataValueType.NewCountAvg:
                    result = new DataValueTypeInfo(valueType, "Průměrný denní přírůstek", "Počet nově nalezených případů, zprůměrovaný za okolních 7 dní (-3  +3 dny)", 
                    GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Solid,
                     -4, +4);
                    return true;
                case DataValueType.NewCountRelative:
                    result = new DataValueTypeInfo(valueType, "Relativní přírůstek na 100tis obyvatel", "Počet nově nalezených případů, přepočtený na 100 000 obyvatel, vhodné k porovnání různých regionů", 
                    GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Dash,
                    0, 0);
                    return true;
                case DataValueType.NewCountRelativeAvg:
                    result = new DataValueTypeInfo(valueType, "Relativní přírůstek na 100t, zprůměrovaný", "Počet nově nalezených případů, přepočtený na 100 000 obyvatel, zprůměrovaný za okolních 7 dní (-3  +3 dny)", 
                    GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Solid,
                    -4, +4);
                    return true;

                case DataValueType.NewCount7DaySum:
                    result = new DataValueTypeInfo(valueType, "Součet za posledních 7 dní", "Počet nově nalezených případů, sečtený za posledních 7 dní", 
                    GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Dot,
                    -7, 0);
                    return true;
                case DataValueType.NewCount7DaySumAvg:
                    result = 
                        new DataValueTypeInfo(valueType, "Součet za posledních 7 dní, průměrovaný", "Počet nově nalezených případů, sečtený za posledních 7 dní, průměrovaný", 
                    GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Solid,
                    -11, +4);
                    return true;
                case DataValueType.NewCount7DaySumRelative:
                    result = new DataValueTypeInfo(valueType, "Součet za týden na 100tis obyvatel", "Počet nově nalezených případů, sečtený za posledních 7 dní, přepočtený na 100 000 obyvatel, vhodné k porovnání různých regionů", 
                    GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Dash,
                    -7, 0);
                    return true;
                case DataValueType.NewCount7DaySumRelativeAvg:
                    result = new DataValueTypeInfo(valueType, "Součet za týden na 100tis obyvatel, průměrovaný", "Počet nově nalezených případů, sečtený za posledních 7 dní, průměrovaný, přepočtený na 100 000 obyvatel, vhodné k porovnání různých regionů", 
                    GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Solid,
                    -11, +4);
                    return true;

                case DataValueType.CurrentCount:
                    result = new DataValueTypeInfo(valueType, "Aktuální stav případů", "Aktuální počet pozitivních osob", 
                    GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Dot,
                    0, 0);
                    return true;
                case DataValueType.CurrentCountAvg:
                    result = new DataValueTypeInfo(valueType, "Aktuální stav, průměr za 7 dní", "Aktuální počet pozitivních osob, průměr za 7 dní", 
                    GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Solid,
                     -4, +4);
                    return true;
                case DataValueType.CurrentCountRelative:
                    result = new DataValueTypeInfo(valueType, "Aktuální stav případů na 100tis obyvatel", "Aktuální počet pozitivních osob, přepočtený na 100 000 obyvatel, vhodné k porovnání různých regionů", 
                    GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Dash,
                    0, 0);
                    return true;
                case DataValueType.CurrentCountRelativeAvg:
                    result = new DataValueTypeInfo(valueType, "Aktuální stav případů na 100tis obyvatel, průměr za 7 dní", "Aktuální počet pozitivních osob, průměr za 7 dní, přepočtený na 100 000 obyvatel, vhodné k porovnání různých regionů", 
                    GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Solid,
                    -4, +4);
                    return true;

                case DataValueType.RZero:
                    result = new DataValueTypeInfo(valueType, "Reprodukční číslo R0", "Reprodukční číslo = poměr počtu nových případů (průměrný za 7 dní) vůči počtu nových případů (průměrnému) před pěti dny", 
                    GraphSerieAxisType.SmallValuesLinear, EntityType.Vesnice, LineDashStyleType.Dot,
                    -6, 0);
                    return true;
                case DataValueType.RZeroAvg:
                    result = new DataValueTypeInfo(valueType, "Reprodukční číslo R0, průměr za 7dní", "Reprodukční číslo = poměr počtu nových případů (průměrný za 7 dní) vůči počtu nových případů (průměrnému) před pěti dny, kdy výsledek je zprůměrovaný za 7 dní", 
                    GraphSerieAxisType.SmallValuesLinear, EntityType.Vesnice, LineDashStyleType.Solid,
                    -10, +4);
                    return true;
            }
            result = null;
            return false;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="text"></param>
        /// <param name="toolTip"></param>
        /// <param name="axisType"></param>
        /// <param name="entityType"></param>
        /// <param name="suggestedDashStyle"></param>
        /// <param name="dateOffsetBefore"></param>
        /// <param name="dateOffsetAfter"></param>
        /// <param name="icon"></param>
        private DataValueTypeInfo(DataValueType value, string text, string toolTip, 
            GraphSerieAxisType axisType, EntityType entityType, LineDashStyleType suggestedDashStyle,
            int? dateOffsetBefore = null, int? dateOffsetAfter = null, System.Drawing.Image icon = null)
            : base(value, text, toolTip, icon)
        {
            this.Value = value;
            this.AxisType = axisType;
            this.EntityType = entityType;
            this.SuggestedDashStyle = suggestedDashStyle;
            this.DateOffsetBefore = dateOffsetBefore;
            this.DateOffsetAfter = dateOffsetAfter;
        }
        /// <summary>
        /// Datový typ grafu
        /// </summary>
        public new DataValueType Value { get; protected set; }
        /// <summary>
        /// Vhodný typ osy
        /// </summary>
        public GraphSerieAxisType AxisType { get; protected set; }
        /// <summary>
        /// Druh entity, kde jsou fyzicky data uložena.
        /// Data tohoto typu lze číst i z vyšších entit, ale ne z nižších: pokud např. určitý údaj je jen za okresy, nelze jej číst z obcí.
        /// </summary>
        public EntityType EntityType { get; protected set; }
        /// <summary>
        /// Vhodný styl čáry, použije se ale jen když se v jednom grafu sejde více stylů.
        /// Pokud v jednom grafu bude jen jeden typ, pak se použije Full.
        /// Má význam: pokud máme data Average i Raw, pak výrazně zobrazím Average, a tečkovaně a tenčí čarou zobrazím Raw data.
        /// </summary>
        public LineDashStyleType SuggestedDashStyle { get; protected set; }
        /// <summary>
        /// Předstih RAW dat načítaných z databáze proti uživatelskému datu počátku grafu - potřebný pro to, aby byl správně napočten agregát k prvnímu uživatelskému dni i z dní předešlých.
        /// Typicky součet za posledních 7 dní je třeba pro 10.1.2021 vypočítat za dny {4, 5, 6, 7, 8, 9, 10}, 
        /// proto musím z databáze získat data od 4.1.2021, napočíst agregát a pak ponechat k zobrazení jen data od 10.1.2021.
        /// V tom případě zde bude ZÁPORNÁ hodnota -6: pro Begin = 10.1.2021 bude SourceBegin = 4.1.2021
        /// </summary>
        public int? DateOffsetBefore { get; protected set; }
        /// <summary>
        /// Přesah RAW dat načítaných z databáze proti uživatelskému datu konce grafu - potřebný pro to, aby byl správně napočten agregát k prvnímu uživatelskému dni i z dní předešlých.
        /// Typicky pro plovoucí průměr (za okolních 7 dní: pro 10.1.2021 budou napočteny dny { 7, 8, 9, 10, 11, 12, 13 }
        /// </summary>
        public int? DateOffsetAfter { get; protected set; }
    }

    /// <summary>
    /// Druh dat získaných z databáze pro jednu serii
    /// </summary>
    [Flags]
    public enum DataValueType : int
    {
        None = 0,

        // Orientační poznámka: procesy probíhají v pořadí jednotlivých bitů, tedy tak jako jsou zde uvedeny jednotlivé základní hodnoty.

        /// <summary>
        /// Zdroj: NewCount
        /// </summary>
        SourceNewCount = 0x00000001,
        /// <summary>
        /// Zdroj: CurrentCount
        /// </summary>
        SourceCurrentCount = 0x00000002,

        // Případný další zdroj přidej i dole do CommonSources!

        /// <summary>
        /// Aggregate: Průměr minulých 7 dní = za minulých 6 dní plus aktuální den (průměr: součet reálných dnů děleno počtem reálných dnů).
        /// Tento agregát se aplikuje jako přípravný = jako první v řadě, ještě před <see cref="AggrCoefficient5Days"/> nebo <see cref="AggrCoefficient7Days"/>.
        /// </summary>
        AggrPrepareLast7DayAverage = 0x00000100,

        /// <summary>
        /// Aggregate: Poměr hodnoty daného dne ku hodnotě před 5 dny (Den[X] / Den[X-5]) = standardně uváděné číslo R0.
        /// Tuto hodnotu je nutno povinně kombinovat s <see cref="AggrPrepareLast7DayAverage"/>, aby výsledky byly korektní. Samotná hodnota <see cref="AggrCoefficient5Days"/> ale <see cref="AggrPrepareLast7DayAverage"/> neprovádí.
        /// <para/>
        /// Tento agregát nelze kombinovat s <see cref="AggrCoefficient7Days"/>, při zkombinování se použije pouze <see cref="AggrCoefficient5Days"/>.
        /// </summary>
        AggrCoefficient5Days = 0x00001000,
        /// <summary>
        /// Aggregate: Poměr hodnoty daného dne ku hodnotě před 7 dny (Den[X] / Den[X-7]).
        /// Tuto hodnotu je nutno povinně kombinovat s <see cref="AggrPrepareLast7DayAverage"/>, aby výsledky byly korektní. Samotná hodnota <see cref="AggrCoefficient7Days"/> ale <see cref="AggrPrepareLast7DayAverage"/> neprovádí.
        /// <para/>
        /// Tento agregát nelze kombinovat s <see cref="AggrCoefficient5Days"/>, při zkombinování se použije pouze <see cref="AggrCoefficient5Days"/>.
        /// </summary>
        AggrCoefficient7Days = 0x00002000,

        /// <summary>
        /// Aggregate: Součet minulých 7 dní = za minulých 6 dní plus aktuální den
        /// </summary>
        AggrLast7DaySum = 0x00010000,
        /// <summary>
        /// Aggregate: Průměr 7 dní dle konfigurace (buď Flow = plovoucí, anebo Last = minulé dny).
        /// <para/>
        /// Tento agregát nelze kombinovat s <see cref="AggrLast7DayAverage"/>, při zkombinování se použije pouze <see cref="AggrLast7DayAverage"/>.
        /// </summary>
        AggrStd7DayAverage = 0x00020000,
        /// <summary>
        /// Aggregate: Průměr minuých 7 dní = za minulých 6 dní plus aktuální den (průměr: součet reálných dnů děleno počtem reálných dnů).
        /// Tento agregát se aplikuje jako standardní, po případné aplikaci <see cref="AggrCoefficient5Days"/> nebo <see cref="AggrCoefficient7Days"/>.
        /// <para/>
        /// Tento agregát nelze kombinovat s <see cref="AggrFlow7DayAverage"/>, při zkombinování se použije pouze <see cref="AggrLast7DayAverage"/>.
        /// </summary>
        AggrLast7DayAverage = 0x00040000,
        /// <summary>
        /// Aggregate: Průměr plovoucích 7 dní = za minulé 3 dny plus aktuální den plus následující 3 dny (průměr: součet reálných dnů děleno počtem reálných dnů)
        /// <para/>
        /// Tento agregát nelze kombinovat s <see cref="AggrLast7DayAverage"/>, při zkombinování se použije pouze <see cref="AggrLast7DayAverage"/>.
        /// </summary>
        AggrFlow7DayAverage = 0x00080000,

        /// <summary>
        /// Aggregate: Přepočet dle počtu obyvatel, na počet případů na 100 000 osob
        /// <para/>
        /// Tento agregát nelze kombinovat s <see cref="AggrRelativeTo1M"/>, při zkombinování se použije pouze <see cref="AggrRelativeTo100K"/>.
        /// </summary>
        AggrRelativeTo100K = 0x00100000,
        /// <summary>
        /// Aggregate: Přepočet dle počtu obyvatel, na počet případů na 1 000 000 osob
        /// <para/>
        /// Tento agregát nelze kombinovat s <see cref="AggrRelativeTo100K"/>, při zkombinování se použije pouze <see cref="AggrRelativeTo100K"/>.
        /// </summary>
        AggrRelativeTo1M = 0x00200000,

        /// <summary>
        /// Round: na 0 míst (na celá čísla)
        /// <para/>
        /// Tuto nelze kombinovat s <see cref="Round1D"/> ani s <see cref="Round2D"/>, při zkombinování se použije nejnižší hodnota (0, 1).
        /// </summary>
        Round0D = 0x01000000,
        /// <summary>
        /// Round: na 1 desetinné místo
        /// <para/>
        /// Tuto nelze kombinovat s <see cref="Round0D"/> ani s <see cref="Round2D"/>, při zkombinování se použije nejnižší hodnota (0, 1).
        /// </summary>
        Round1D = 0x02000000,
        /// <summary>
        /// Round: na 2 desetinná místa (typicky pro Coefficient)
        /// <para/>
        /// Tuto nelze kombinovat s <see cref="Round0D"/> ani s <see cref="Round1D"/>, při zkombinování se použije nejnižší hodnota (0, 1).
        /// </summary>
        Round2D = 0x04000000,

        /// <summary>
        /// Maska všech zdrojů
        /// </summary>
        CommonSources = SourceNewCount | SourceCurrentCount,

        // Orientační poznámka: procesy probíhají v pořadí jednotlivých bitů, tedy tak jako jsou zde uvedeny jednotlivé základní hodnoty.
        // Při skládání finálních hodnot (následující segment) se bere ohled na fyzické pořadí bitů;
        //  a nelze brát ohled na to, v jakém pořadí jsou zde jednotlivé bity vkládány do výsledné hodnoty!!!

        // Pořadí akcí fyzicky realizuje metoda Database.ProcessResultValue() !

        NewCount = SourceNewCount | Round0D,
        NewCountAvg = SourceNewCount | AggrStd7DayAverage | Round0D,
        NewCountRelative = SourceNewCount | AggrRelativeTo100K | Round0D,
        NewCountRelativeAvg = SourceNewCount | AggrStd7DayAverage | AggrRelativeTo100K | Round0D,
        NewCount7DaySum = SourceNewCount | AggrLast7DaySum | Round0D,
        NewCount7DaySumAvg = SourceNewCount | AggrLast7DaySum | AggrStd7DayAverage | Round0D,
        NewCount7DaySumRelative = SourceNewCount | AggrLast7DaySum | AggrRelativeTo100K | Round0D,
        NewCount7DaySumRelativeAvg = SourceNewCount | AggrLast7DaySum | AggrStd7DayAverage | AggrRelativeTo100K | Round0D,
        CurrentCount = SourceCurrentCount | Round0D,
        CurrentCountAvg = SourceCurrentCount | AggrStd7DayAverage | Round0D,
        CurrentCountRelative = SourceCurrentCount | AggrRelativeTo100K | Round0D,
        CurrentCountRelativeAvg = SourceCurrentCount | AggrStd7DayAverage | AggrRelativeTo100K | Round0D,
        RZero = SourceNewCount | AggrPrepareLast7DayAverage | AggrCoefficient5Days | Round2D,
        RZeroAvg = SourceNewCount | AggrPrepareLast7DayAverage | AggrCoefficient5Days | AggrStd7DayAverage | Round2D

        // Po přidání další finální hodnoty ji přidej i do metod:
        //  public static DataValueTypeInfo[] CreateAll()
        //  public static bool TryCreateFor(DataValueType valueType, out DataValueTypeInfo result)
        // Protože tam se generují uživatelské nabídky hodnot a jejich detaily.
    }
    /// <summary>
    /// Vhodný typ osy Y
    /// </summary>
    public enum GraphSerieAxisType
    {
        None,
        SmallValuesLinear,
        BigValuesLinear,
        SmallValuesLogarithmic,
        BigValuesLogarithmic
    }
    /// <summary>
    /// Styl čáry v grafu
    /// </summary>
    public enum LineDashStyleType
    {
        None,
        Solid,
        Dot,
        Dash,
        DashDot,
        DashDotDot
    }
    #endregion
    #region GraphStripInfo : Definice pruhů v grafu = časové intervaly
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
    #region GraphData : Načtená aktuální data o jednom celém grafu (data grafu, sloupce za jednotlivé serie, řádky za jednotlivé datumy)
    /// <summary>
    /// Data pro jeden graf shrnutá z definice grafu <see cref="GraphInfo"/>, ze všech jeho serií <see cref="GraphSerieInfo"/>, načtená z databáze <see cref="DatabaseInfo"/>.
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
            this.StatisticInit();
        }
        private int ColumnId;
        /// <summary>
        /// Definice grafu, podle které jsou načtena data
        /// </summary>
        public GraphInfo GraphInfo { get; private set; }
        /// <summary>
        /// První zobrazené datum
        /// </summary>
        public DateTime? DateBegin { get; set; }
        /// <summary>
        /// Poslední zobrazené datum
        /// </summary>
        public DateTime? DateEnd { get; set; }
        public GraphColumn AddColumn(GraphSerieInfo serie, DatabaseInfo.EntityInfo entity)
        {
            int index = ColumnId++;
            string columnNameData = "column" + index.ToString();
            GraphColumn column = new GraphColumn(this, index, columnNameData, serie, entity);
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
        #region Vytváření obecné DataTable
        /// <summary>
        /// Tabulka s daty sloužící jako DataSource pro graf
        /// </summary>
        public DataTable DataTable { get { return CreateDataTable(); } }
        /// <summary>
        /// Vygeneruje a vrátí DataTable pro komponentu grafu
        /// </summary>
        /// <returns></returns>
        private DataTable CreateDataTable()
        {
            System.Data.DataTable dataTable = new System.Data.DataTable("GraphData");

            System.Data.DataColumn dataColumn = CreateDataColumn("date", "Datum", typeof(DateTime));
            dataTable.Columns.Add(dataColumn);
            foreach (var column in this.Columns)
            {
                dataColumn = CreateDataColumn(column.ColumnNameData, column.Title, typeof(decimal));
                dataTable.Columns.Add(dataColumn);
            }

            foreach (var row in Rows)
            {
                dataTable.Rows.Add(row.Cells);
            }

            return dataTable;
        }
        /// <summary>
        /// Vygeneruje a vrátí DataColumn
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="caption"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        private DataColumn CreateDataColumn(string columnName, string caption, Type dataType)
        {
            DataColumn dataColumn = new DataColumn(columnName);
            dataColumn.AllowDBNull = false;
            dataColumn.Caption = caption;
            dataColumn.DataType = dataType;
            return dataColumn;
        }
        #endregion
        #region Statistika
        /// <summary>
        /// Inicializace statistiky
        /// </summary>
        private void StatisticInit()
        {
            this.LoadTimeBegin = DateTime.Now;
            this.LoadTimeEnd = null;
            this.ShowTimeEnd = null;
            this.ScanRecordCount = 0;
            this.LoadRecordCount = 0;
            this.ShowRecordCount = 0;
        }
        /// <summary>
        /// Čas zahájení načítání dat
        /// </summary>
        public DateTime LoadTimeBegin { get; private set; }
        /// <summary>
        /// Čas dokončení načítání dat
        /// </summary>
        public DateTime? LoadTimeEnd { get; private set; }
        /// <summary>
        /// Čas dokončení zobrazení grafu
        /// </summary>
        public DateTime? ShowTimeEnd { get; private set; }
        /// <summary>
        /// Počet sekund načítání dat, textem
        /// </summary>
        public string LoadSecondsText { get { return (LoadTimeEnd.HasValue ? ((TimeSpan)(LoadTimeEnd.Value - LoadTimeBegin)).TotalSeconds.ToString("### ##0.000").Trim() + " sec": "???"); } }
        /// <summary>
        /// Počet záznamů prověřovaných, zda vyhoví filtru, textem
        /// </summary>
        public string ScanRecordCountText { get { return ScanRecordCount.ToString("### ### ### ##0").Trim(); } }
        /// <summary>
        /// Počet záznamů prověřovaných, zda vyhoví filtru
        /// </summary>
        public int ScanRecordCount { get; private set; }
        /// <summary>
        /// Počet záznamů načtených podle filtru, textem
        /// </summary>
        public string LoadRecordCountText { get { return LoadRecordCount.ToString("### ### ### ##0").Trim(); } }
        /// <summary>
        /// Počet záznamů načtených podle filtru
        /// </summary>
        public int LoadRecordCount { get; private set; }
        /// <summary>
        /// Počet záznamů určených k zobrazení v grafu, textem
        /// </summary>
        public string ShowRecordCountText { get { return ShowRecordCount.ToString("### ### ### ##0").Trim(); } }
        /// <summary>
        /// Počet záznamů určených k zobrazení v grafu
        /// </summary>
        public int ShowRecordCount { get; private set; }
        /// <summary>
        /// Přidá další počty načtených dat
        /// </summary>
        /// <param name="scanCount"></param>
        /// <param name="loadCount"></param>
        /// <param name="showCount"></param>
        public void AddCount(int scanCount, int loadCount, int showCount)
        {
            if (scanCount > 0) ScanRecordCount += scanCount;
            if (loadCount > 0) LoadRecordCount += loadCount;
            if (showCount > 0) ShowRecordCount += showCount;
        }
        /// <summary>
        /// Volá se po dokončení načítání dat grafu, jen kvůli měření času
        /// </summary>
        public void FinaliseLoading()
        {
            if (this.LoadTimeEnd.HasValue)
                throw new InvalidOperationException("Nelze opakovaně provádět akci GraphData.FinaliseLoading !");
            this.LoadTimeEnd = DateTime.Now;
        }
        /// <summary>
        /// Volá se po dokončení zobrazení grafu, jen kvůli měření času
        /// </summary>
        public void FinaliseShowGraph()
        {
            if (this.ShowTimeEnd.HasValue)
                throw new InvalidOperationException("Nelze opakovaně provádět akci GraphData.FinaliseShowGraph !");
            this.ShowTimeEnd = DateTime.Now;
        }
        #endregion
    }
    /// <summary>
    /// Jeden sloupec, reprezentuje jednu serii grafu
    /// </summary>
    public class GraphColumn
    {
        public GraphColumn(GraphData graphData, int columnIndex, string columnNameData, GraphSerieInfo graphSerie, DatabaseInfo.EntityInfo dataEntity)
        {
            this.GraphData = graphData;
            Index = columnIndex;
            ColumnNameData = columnNameData;
            this.GraphSerie = graphSerie;
            this.DataEntity = dataEntity;
        }
        public override string ToString()
        {
            return $"Column[{Index}]: {ColumnNameData} = \"{Title}\"";
        }
        public GraphData GraphData { get; private set; }
        public int Index { get; private set; }
        /// <summary>
        /// Jméno sloupce s daty (ColumnName) v obecné DataTable
        /// </summary>
        public string ColumnNameData { get; set; }
        /// <summary>
        /// Reference na serii s daty
        /// </summary>
        public GraphSerieInfo GraphSerie { get; private set; }
        /// <summary>
        /// Reference na entitu (okres, obec, atd)
        /// </summary>
        public DatabaseInfo.EntityInfo DataEntity { get; private set; }
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
