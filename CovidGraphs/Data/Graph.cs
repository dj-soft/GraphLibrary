using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
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
            this.Id = 0;
            this.Init();
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
        #endregion
        #region Data definující záhlaví grafu
        /// <summary>
        /// ID grafu. Je součástí jména souboru. Výchozí je 0 = pro dosud neuložený záznam.
        /// Prvním uložením na disk se ID přidělí.
        /// <see cref="Id"/> je načítáno z disku, je ukládáno na disk, ale nepřenáší se přenosem hodnoty <see cref="Serial"/>.
        /// </summary>
        public int Id { get; private set; }
        /// <summary>
        /// Jméno souboru grafu, z něhož je načten. Pokud dosud není uložen, je null. 
        /// I v takovém případě bude možno jej uložit, vygeneruje si svoje jméno souboru.
        /// <see cref="FileName"/> není serializováno.
        /// </summary>
        public string FileName { get; private set; }
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
        /// <summary>
        /// Z grafu odebere danou serii.
        /// </summary>
        /// <param name="serie"></param>
        public void RemoveSerie(GraphSerieInfo serie)
        {
            if (serie == null) return;
            int index = this._Series.FindIndex(s => Object.ReferenceEquals(s, serie));
            if (index < 0) return;
            this._Series.RemoveAt(index);
        }
        #endregion
        #region Podpora pro exporty
        /// <summary>
        /// Obsahuje vhodné jméno pro export grafu.
        /// Obsahuje adresář (Screenshot), ten existuje. Obsahuje jméno grafu (platné pro jméno souboru) a suffix = dnešní den.
        /// Neobsahuje tečku ani příponu, tu dodá exportér.
        /// </summary>
        public string ScreenshotFileName
        {
            get
            {
                DateTime now = DateTime.Now;
                string suffix = now.ToString("yyyy-MM-dd");
                string name = CreateValidFileName(this.Title, "") + "~" + suffix;
                string path = PathScreenshots;
                if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
                return Path.Combine(path, name);
            }
        }
        #endregion
        #region Static načtení celého seznamu grafů z config adresáře, tvorba sample grafů
        /// <summary>
        /// Načte data grafů z daného adresáře
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<GraphInfo> LoadFromPath(string path = null)
        {
            List<GraphInfo> graphs = new List<GraphInfo>();
            FileInfo[] fileInfos = _SearchForGraphFiles(path);
            int lastId = 0;
            if (fileInfos != null && fileInfos.Length > 0)
            { 
                foreach (var fileInfo in fileInfos)
                {
                    GraphInfo graph = GraphInfo.LoadFromFile(fileInfo.FullName);
                    if (graph != null)
                    {
                        graphs.Add(graph);
                        if (graph.Id > lastId) lastId = graph.Id;
                    }
                }
            }
            App.Config.LastSaveGraphId = lastId;
            return graphs;
        }
        /// <summary>
        /// Metoda najde a vrátí soubory na dané cestě, anebo je zkusí najít ve standardní datové cestě nebo v adresáři aplikace.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static FileInfo[] _SearchForGraphFiles(string path = null)
        {
            FileInfo[] fileInfos = null;
            string pattern = "*." + FileExtension;
            DirectoryInfo dirInfo = null;
            if (!String.IsNullOrEmpty(path))
            {   // Povinně z dané cesty
                dirInfo = new DirectoryInfo(path);
                if (dirInfo.Exists)
                    fileInfos = dirInfo.GetFiles(pattern);
            }
            else
            {   // Hledat ve standardní cestě:
                dirInfo = new DirectoryInfo(PathData);               // Tady jsou grafy ukládané docela standardně
                if (dirInfo.Exists)
                    fileInfos = dirInfo.GetFiles(pattern);

                // Hledat v aplikační cestě:
                if (fileInfos == null || fileInfos.Length == 0)
                {
                    dirInfo = new DirectoryInfo(App.AppDataPath);    // Tady jsou grafy distribuované spolu s aplikací, ale sem se neukládají
                    if (dirInfo.Exists)
                        fileInfos = dirInfo.GetFiles(pattern);
                }

                // Vygenerovat výchozí grafy standardně z kódu pomocí deserializace:
#warning Vygenerovat výchozí grafy standardně z kódu pomocí deserializace:
            }
            return fileInfos;
        }
        /// <summary>
        /// Vygeneruje a vrátí sadu ukázkových grafů
        /// </summary>
        /// <param name="saveToFiles"></param>
        /// <returns></returns>
        public static List<GraphInfo> CreateSamples(bool saveToFiles = false)
        {
            List<GraphInfo> graphs = new List<GraphInfo>();
            GraphInfo graph;

            graph = new Data.GraphInfo() { Title = "ČR: Denní přírůstky poslední měsíc+", Description = "Počty nově nakažených za den - přesně, a průměrně", TimeRangeLastMonths = 1, ChartAxisYRight = true };
            graph.AddSerie(new GraphSerieInfo() { EntityFullCode = "CZ", Title = "Česká republika, denní přírůstky", ValueType = DataValueType.NewCount, LineThickness = 1, LineColor = Color.DarkViolet, LineDashStyle = LineDashStyleType.Dot });
            graph.AddSerie(new GraphSerieInfo() { EntityFullCode = "CZ", Title = "Česká republika, denní přírůstky průměrně", ValueType = DataValueType.NewCountAvg, LineThickness = 3, LineColor = Color.DarkViolet, LineDashStyle = LineDashStyleType.Solid });
            graphs.Add(graph);

            graph = new GraphInfo() { Title = "CR+PC+HK obce, relativně", Description = "Stav ve trojměstí za celou dobu", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0531.5304.53043.571164", Title = "Chrudim, aktuálně", ValueType = DataValueType.CurrentCountRelativeAvg });
            graph.AddSerie(new GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0532.5309.53092.555134", Title = "Pardubice, aktuálně", ValueType = DataValueType.CurrentCountRelativeAvg });
            graph.AddSerie(new GraphSerieInfo() { EntityFullCode = "CZ.CZ052.CZ0521.5205.52051.569810", Title = "Hradec, aktuálně", ValueType = DataValueType.CurrentCountRelativeAvg });
            graphs.Add(graph);

            graph = new GraphInfo() { Title = "Chrudim", Description = "Stav v Chrudimi za poslední 4 měsíce", TimeRangeLastMonths = 4, ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0531.5304.53043.571164", Title = "Chrudim, aktuálně", ValueType = DataValueType.CurrentCount });
            graphs.Add(graph);

            graph = new GraphInfo() { Title = "Pardubice", Description = "Stav v Pardubicích za posledních 7 měsíců", TimeRangeLastMonths = 7, ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0532.5309.53092.555134", Title = "Pardubice, aktuálně", ValueType = DataValueType.CurrentCount });
            graphs.Add(graph);

            graph = new GraphInfo() { Title = "Krucemburk", Description = "Stav v Krucborku a Ždírci za celou dobu", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new GraphSerieInfo() { EntityFullCode = "CZ.CZ063.CZ0631.6304.63041.568945", Title = "Krucbork, aktuálně", ValueType = DataValueType.CurrentCount });
            graph.AddSerie(new GraphSerieInfo() { EntityFullCode = "CZ.CZ063.CZ0631.6304.63041.569780", Title = "Ždírec, aktuálně", ValueType = DataValueType.CurrentCount });
            graphs.Add(graph);

            graph = new GraphInfo() { Title = "CR+PC+HK obce", Description = "Stav ve trojměstí za celou dobu", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0531.5304.53043.571164", Title = "Chrudim, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0532.5309.53092.555134", Title = "Pardubice, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ052.CZ0521.5205.52051.569810", Title = "Hradec, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "CR+PC+HK obce poslední 3 měsíce", Description = "Stav ve trojměstí za poslední 3 měsíce", TimeRangeLastMonths = 3, ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0531.5304.53043.571164", Title = "Chrudim, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0532.5309.53092.555134", Title = "Pardubice, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ052.CZ0521.5205.52051.569810", Title = "Hradec, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "CR+PC+HK okresy", Description = "Stav okresů za celou dobu", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0531", Title = "Chrudim, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0532", Title = "Pardubice, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ052.CZ0521", Title = "Hradec, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "CR+PC+HK obce, číslo R", Description = "Stav ve trojměstí za celou dobu", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0531.5304.53043.571164", Title = "Chrudim, aktuálně", ValueType = Data.DataValueType.RZeroAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0532.5309.53092.555134", Title = "Pardubice, aktuálně", ValueType = Data.DataValueType.RZeroAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ052.CZ0521.5205.52051.569810", Title = "Hradec, aktuálně", ValueType = Data.DataValueType.RZeroAvg });
            graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "ČR + kraje PC+HK, přírůstky 7dní", Description = "Stav celkový za celou dobu", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, aktuálně", ValueType = Data.DataValueType.NewCount7DaySumAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053", Title = "Kraj Pardubice", ValueType = Data.DataValueType.NewCount7DaySumAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ052", Title = "Kraj HK", ValueType = Data.DataValueType.NewCount7DaySumAvg });
            graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "ČR + kraje PC+HK, aktuální stav průměr", Description = "Aktuální stav, průměrovaný, celá doba", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053", Title = "Kraj Pardubice", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ052", Title = "Kraj HK", ValueType = Data.DataValueType.CurrentCountAvg });
            graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "ČR + kraje PC+HK, číslo R avg", Description = "Stav celkový za celou dobu", TimeRangeLastMonths = 3, ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, aktuálně", ValueType = Data.DataValueType.RZeroAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053", Title = "Kraj Pardubice", ValueType = Data.DataValueType.RZeroAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ052", Title = "Kraj HK", ValueType = Data.DataValueType.RZeroAvg });
            graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "ČR + kraje PC+HK, číslo R raw", Description = "Stav celkový za celou dobu", TimeRangeLastMonths = 3, ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, aktuálně", ValueType = Data.DataValueType.RZero });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053", Title = "Kraj Pardubice", ValueType = Data.DataValueType.RZero });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ052", Title = "Kraj HK", ValueType = Data.DataValueType.RZero });
            graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "CR+PC+HK Přírůstek/7 dní relativně", Description = "Počty nových případů za posledních 7 dní poměrně k počtu obyvatel", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0531", Title = "Chrudim, aktuálně", ValueType = Data.DataValueType.NewCount7DaySumRelative });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0532", Title = "Pardubice, aktuálně", ValueType = Data.DataValueType.NewCount7DaySumRelative });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ052.CZ0521", Title = "Hradec, aktuálně", ValueType = Data.DataValueType.NewCount7DaySumRelative });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0531", Title = "Chrudim, číslo R", ValueType = Data.DataValueType.RZeroAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0532", Title = "Pardubice, číslo R", ValueType = Data.DataValueType.RZeroAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ052.CZ0521", Title = "Hradec, číslo R", ValueType = Data.DataValueType.RZeroAvg });
            graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "CR+PC+HK+RK+CH+HL Přírůstek průměr", Description = "Počty nových případů, zprůměrované", TimeRangeLastMonths = 3, ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0531", Title = "Chrudim, aktuálně", ValueType = Data.DataValueType.NewCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0532", Title = "Pardubice, aktuálně", ValueType = Data.DataValueType.NewCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ052.CZ0521", Title = "Hradec, aktuálně", ValueType = Data.DataValueType.NewCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ052.CZ0524", Title = "okres Rychnov n/K, aktuálně", ValueType = Data.DataValueType.NewCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ052.CZ0524.5213.52132.576069", Title = "obec Rychnov n/K, aktuálně", ValueType = Data.DataValueType.NewCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ063.CZ0631.6304.63041.568759", Title = "obec Chotěboř", ValueType = Data.DataValueType.NewCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ.CZ053.CZ0531.5302.53021.571393", Title = "obec Hlinsko", ValueType = Data.DataValueType.NewCountAvg });
            graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "ČR, podle velikosti obce, relativně, aktuální stav průměr", Description = "Aktuální stav, průměrovaný, celá doba", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, obce pod 300 osob", ValueType = Data.DataValueType.CurrentCountRelativeAvg, LineColor = Color.FromArgb(64, 0, 0), FiltrPocetObyvatelDo = 300 });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, obce 300 - 2000 osob", ValueType = Data.DataValueType.CurrentCountRelativeAvg, LineColor = Color.FromArgb(160, 64, 0), FiltrPocetObyvatelOd = 300, FiltrPocetObyvatelDo = 2000 });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, obce 2000 - 12000 osob", ValueType = Data.DataValueType.CurrentCountRelativeAvg, LineColor = Color.FromArgb(0, 192, 128), FiltrPocetObyvatelOd = 2000, FiltrPocetObyvatelDo = 12000 });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, obce 12000 - 60000 osob", ValueType = Data.DataValueType.CurrentCountRelativeAvg, LineColor = Color.FromArgb(64, 96, 0), FiltrPocetObyvatelOd = 12000, FiltrPocetObyvatelDo = 60000 });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, obce 60000 - 350000 osob", ValueType = Data.DataValueType.CurrentCountRelativeAvg, LineColor = Color.FromArgb(32, 32, 192), FiltrPocetObyvatelOd = 60000, FiltrPocetObyvatelDo = 350000 });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, Praha", ValueType = Data.DataValueType.CurrentCountRelativeAvg, LineColor = Color.FromArgb(96, 96, 224), FiltrPocetObyvatelOd = 350000 });
            graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "ČR, podle velikosti obce, relativně, týdenní přírůstky, průměr", Description = "Aktuální stav, průměrovaný, celá doba", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, obce pod 300 osob", ValueType = Data.DataValueType.NewCount7DaySumRelativeAvg, LineColor = Color.FromArgb(64, 0, 0), FiltrPocetObyvatelDo = 300 });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, obce 300 - 2000 osob", ValueType = Data.DataValueType.NewCount7DaySumRelativeAvg, LineColor = Color.FromArgb(160, 64, 0), FiltrPocetObyvatelOd = 300, FiltrPocetObyvatelDo = 2000 });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, obce 2000 - 12000 osob", ValueType = Data.DataValueType.NewCount7DaySumRelativeAvg, LineColor = Color.FromArgb(0, 192, 128), FiltrPocetObyvatelOd = 2000, FiltrPocetObyvatelDo = 12000 });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, obce 12000 - 60000 osob", ValueType = Data.DataValueType.NewCount7DaySumRelativeAvg, LineColor = Color.FromArgb(64, 96, 0), FiltrPocetObyvatelOd = 12000, FiltrPocetObyvatelDo = 60000 });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, obce 60000 - 350000 osob", ValueType = Data.DataValueType.NewCount7DaySumRelativeAvg, LineColor = Color.FromArgb(32, 32, 192), FiltrPocetObyvatelOd = 60000, FiltrPocetObyvatelDo = 350000 });
            graph.AddSerie(new Data.GraphSerieInfo() { EntityFullCode = "CZ", Title = "ČR, Praha", ValueType = Data.DataValueType.NewCount7DaySumRelativeAvg, LineColor = Color.FromArgb(96, 96, 224), FiltrPocetObyvatelOd = 350000 });
            graphs.Add(graph);

            if (saveToFiles)
                graphs.ForEachExec(g => g.SaveToFile());

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
            bool isExplicitFile = !String.IsNullOrEmpty(fileName);
            string targetFile = (isExplicitFile ? fileName : GetFileName());             // Metoda GetFileName() může vygenerovat nové ID, to chci uložit - takže GetFileName() musí být dříve než získání Serial:
            string data = this.Serial;
            File.WriteAllText(targetFile, data, Encoding.UTF8);
            if (!isExplicitFile) this.SetFileName(targetFile);
        }
        /// <summary>
        /// Pokud this graf je uložen v souboru, smaže tento soubor.
        /// Volá se v procesu smazání grafu.
        /// </summary>
        public void DeleteGraphFile(bool renameOnly = false)
        {
            string fileName = this.FileName;
            if (!String.IsNullOrEmpty(fileName))
            {
                fileName = fileName.Trim();
                if (File.Exists(fileName))
                {
                    if (renameOnly)
                    {
                        string renameName = Path.ChangeExtension(fileName, "delChart");
                        if (File.Exists(renameName))
                            App.TryRun(() => File.Delete(renameName));
                        App.TryRun(() => File.Move(fileName, renameName));
                    }
                    else
                    {
                        App.TryRun(() => File.Delete(fileName));
                    }
                }
            }
            ResetId();
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
            graphInfo.ResetId();
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
            graphInfo.FileName = fileName;
            return graphInfo;
        }
        /// <summary>
        /// Serializovaný obraz celé this instance.
        /// Neobsahuje <see cref="Id"/> ani <see cref="FileName"/>
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
        /// Resetuje <see cref="Id"/> a vynuluje jméno souboru <see cref="FileName"/>.
        /// Pokud bude následně prováděno Save, pak se do grafu vygeneruje nové ID a soubor bude uložen jako nový soubor.
        /// Voláním této metody se instance grafu odpojí od zdrojového souboru, volá se typicky při změně garfu a jeho uložení grafu jako nový graf.
        /// </summary>
        public void ResetId()
        {
            this.Id = 0;
            this.FileName = null;
        }
        /// <summary>
        /// Vymaže všechna data.
        /// Nesmaže <see cref="Id"/> ani <see cref="FileName"/>.
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
            _WorkingChartLayout = null;
        }
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
                case ChartHeaderId:
                    if (this.Id == 0)
                        this.Id = GetValue(text, 0);
                    break;
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
            stream.WriteLine(ChartHeaderFileV1);
            SaveToFileHeader(stream);
            SaveToFileSeries(stream);
            SaveToFileLayout(stream);
        }
        /// <summary>
        /// Do daného streamu vepíše vlastnosti hlavičky grafu. Nikoli položky, a nikoli layout.
        /// </summary>
        /// <param name="stream"></param>
        private void SaveToFileHeader(StringWriter stream)
        {
            stream.WriteLine(CreateLine(ChartHeaderId, GetSerial(this.Id)));
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
        /// Metoda vrátí plné jméno pro soubor. Používá se těsně před Save do souboru.
        /// Tato metoda v případě potřeby vygeneruje nové jméno pro soubor a vygeneruje i nové ID.
        /// Po uložení souboru se má jméno tohoto souboru vložit do <see cref="FileName"/> pomocí metody <see cref="SetFileName(string)"/>.
        /// </summary>
        protected string GetFileName()
        {
            if (this.Id == 0)
                this.Id = App.Config.GetNextGraphId();

            string name = "Chart" + this.Id.ToString("00000") + "-" + CreateValidFileName(this.Title, "") + "." + FileExtension;
            string path = PathData;
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
            return Path.Combine(path, name);
        }
        /// <summary>
        /// Adresář pro standardní načítání a ukládání pracovních dat
        /// </summary>
        protected static string PathData { get { return System.IO.Path.Combine(App.ConfigPath, "Charts"); } }
        /// <summary>
        /// Adresář pro standardní export screenshotů
        /// </summary>
        protected static string PathScreenshots { get { return System.IO.Path.Combine(App.ConfigPath, "Screenshots"); } }

        /// <summary>
        /// Metoda zajistí uložení jména souboru do this instance.
        /// Pokud dosud jméno existuje a je shodné, nic nedělá.
        /// Pokud se ličí, pak smaže soubor původního jména.
        /// </summary>
        /// <param name="fileName"></param>
        protected void SetFileName(string fileName)
        {
            if (String.IsNullOrEmpty(fileName)) return;

            string newFile = fileName.Trim();
            string oldFile = this.FileName;
            if (!String.IsNullOrEmpty(oldFile))
            {
                oldFile = oldFile.Trim();
                bool isEqual = String.Equals(oldFile, newFile, StringComparison.OrdinalIgnoreCase);
                if (!isEqual && File.Exists(oldFile))
                    App.TryRun(() => File.Delete(oldFile));
            }
            this.FileName = newFile;
        }
        public enum FileVersion { None, Version1 }
        private const string ChartHeaderFileV1 = "== BestInCovid v1.0 chart ==";
        private const string ChartHeaderId = "ChartId";
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
        /// <summary>
        /// Maximální počet datových řad v jednom vgrafu, kvůli přehlednosti
        /// </summary>
        internal const int MaxSeriesCount = 24;
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
        #region Kontroly
        /// <summary>
        /// Vrací text všech chyb. Pokud nejsou chyby, vrací prázdný string (ne null).
        /// </summary>
        public string Errors
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                if (String.IsNullOrEmpty(this.Title)) sb.AppendLine("- Titulek grafu není zadán");
                if (this._Series.Count == 0) sb.AppendLine("- Graf neobsahuje žádná data");
                foreach (var serie in this._Series)
                    serie.AddErrors(sb);

                return sb.ToString();
            }
        }
        #endregion
        #region Layout grafu, generátor defaultního layoutu
        /// <summary>
        /// Vzhled grafu. Ve výchozím stavu je null, pak si aplikace má vyžádat defaultní layout z metody <see cref="CreateDefaultLayout(GraphData)"/>.
        /// Aplikace může tento layout použít, následně editovat, a pak uložit sem do <see cref="ChartLayout"/>, odkud bude uložen na disk a příště bude použit již editovaný layout.
        /// Aplikace může vždy vyžádat aktuální defaultní layout, vhodné například po změně dat.
        /// </summary>
        public string ChartLayout { get { return _ChartLayout; } set { _ChartLayout = value; } } private string _ChartLayout;
        /// <summary>
        /// Provozní vzhled grafu. Používá se, když <see cref="ChartLayout"/> není definován. Neserializuje se do souboru.
        /// Při editaci grafu se neakceptuje, po editaci se nuluje.
        /// </summary>
        public string WorkingChartLayout { get { return _WorkingChartLayout; } set { _WorkingChartLayout = value; } } private string _WorkingChartLayout;
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
                string settings = @"﻿    <Legend HorizontalIndent='40' AlignmentHorizontal='Left' AlignmentVertical='Top' Direction='TopToBottom' CrosshairContentOffset='4' MarkerSize='@2,Width=30@2,Height=16' MarkerMode='CheckBoxAndMarker' MaxCrosshairContentWidth='50' MaxCrosshairContentHeight='0' Name='Základní legenda'>
        <Title WordWrap='false' MaxLineCount='0' Alignment='Center' Text='Jednotlivé řady' Visible='true' Font='Tahoma, 12pt' TextColor='' Antialiasing='false' />
    </Legend>
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



        private string SAMPLES_LAYOUT()
        {
            #region Ukázky
            /*

﻿<?xml version="1.0" encoding="utf-8"?>
<ChartXmlSerializer version="20.1.4.0">
  <Chart AppearanceNameSerializable="Default" SelectionMode="None" SeriesSelectionMode="Series">
    <DataContainer ValidateDataMembers="true" BoundSeriesSorting="None">
      <SeriesSerializable>
        <Item1 Name="Series 1" DataSourceSorted="false" ArgumentDataMember="date" ValueDataMembersSerializable="column0" CrosshairContentShowMode="Default" CrosshairEmptyValueLegendText="">
          <View TypeNameSerializable="LineSeriesView">
            <SeriesPointAnimation TypeNameSerializable="XYMarkerWidenAnimation" />
            <FirstPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="180" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </FirstPoint>
            <LastPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="0" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </LastPoint>
          </View>
        </Item1>
        <Item2 Name="Series 2" DataSourceSorted="false" ArgumentDataMember="date" ValueDataMembersSerializable="column1" CrosshairContentShowMode="Default" CrosshairEmptyValueLegendText="">
          <View TypeNameSerializable="LineSeriesView">
            <SeriesPointAnimation TypeNameSerializable="XYMarkerWidenAnimation" />
            <FirstPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="180" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </FirstPoint>
            <LastPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="0" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </LastPoint>
          </View>
        </Item2>
        <Item3 Name="Series 3" DataSourceSorted="false" ArgumentDataMember="date" ValueDataMembersSerializable="column2" CrosshairContentShowMode="Default" CrosshairEmptyValueLegendText="">
          <View TypeNameSerializable="LineSeriesView">
            <SeriesPointAnimation TypeNameSerializable="XYMarkerWidenAnimation" />
            <FirstPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="180" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </FirstPoint>
            <LastPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="0" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </LastPoint>
          </View>
        </Item3>
      </SeriesSerializable>
      <SeriesTemplate CrosshairContentShowMode="Default" CrosshairEmptyValueLegendText="" />
    </DataContainer>
    <Legend VerticalIndent="25" AlignmentHorizontal="Center" Direction="LeftToRight" CrosshairContentOffset="4" BackColor="183, 221, 232" Font="Tahoma, 11.25pt, style=Bold" MaxCrosshairContentWidth="50" MaxCrosshairContentHeight="0" Name="Default Legend" />
    <Titles>
      <Item1 Text="Best in Covid, ČR" Font="Tahoma, 18pt" TextColor="" Antialiasing="true" EnableAntialiasing="Default" />
    </Titles>
    <Diagram RuntimePaneCollapse="true" RuntimePaneResize="false" PaneLayoutDirection="Vertical" TypeNameSerializable="XYDiagram">
      <AxisX StickToEnd="false" VisibleInPanesSerializable="-1" ShowBehind="false">
        <WholeRange StartSideMargin="21.2" EndSideMargin="21.2" SideMarginSizeUnit="AxisUnit" />
        <DateTimeScaleOptions GridAlignment="Month" AutoGrid="false">
          <IntervalOptions />
        </DateTimeScaleOptions>
      </AxisX>
      <AxisY VisibleInPanesSerializable="-1" ShowBehind="false">
        <WholeRange StartSideMargin="238.6" EndSideMargin="238.6" SideMarginSizeUnit="AxisUnit" />
        <GridLines Color="128, 100, 162" />
      </AxisY>
      <SelectionOptions />
    </Diagram>
  </Chart>
</ChartXmlSerializer>
















ZAŠKRTÁVACÍ LEGENDA NAHOŘE UPROSTŘED

﻿<?xml version="1.0" encoding="utf-8"?>
<ChartXmlSerializer version="20.1.4.0">
  <Chart AppearanceNameSerializable="Default" SelectionMode="None" SeriesSelectionMode="Series">
    <DataContainer ValidateDataMembers="true" BoundSeriesSorting="None">
      <SeriesSerializable>
        <Item1 Name="Series 1" DataSourceSorted="false" ArgumentDataMember="date" ValueDataMembersSerializable="column0" CrosshairContentShowMode="Default" CrosshairEmptyValueLegendText="">
          <View TypeNameSerializable="LineSeriesView">
            <SeriesPointAnimation TypeNameSerializable="XYMarkerWidenAnimation" />
            <FirstPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="180" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </FirstPoint>
            <LastPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="0" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </LastPoint>
          </View>
        </Item1>
        <Item2 Name="Series 2" DataSourceSorted="false" ArgumentDataMember="date" ValueDataMembersSerializable="column1" CrosshairContentShowMode="Default" CrosshairEmptyValueLegendText="">
          <View TypeNameSerializable="LineSeriesView">
            <SeriesPointAnimation TypeNameSerializable="XYMarkerWidenAnimation" />
            <FirstPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="180" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </FirstPoint>
            <LastPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="0" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </LastPoint>
          </View>
        </Item2>
        <Item3 Name="Series 3" DataSourceSorted="false" ArgumentDataMember="date" ValueDataMembersSerializable="column2" CrosshairContentShowMode="Default" CrosshairEmptyValueLegendText="">
          <View TypeNameSerializable="LineSeriesView">
            <SeriesPointAnimation TypeNameSerializable="XYMarkerWidenAnimation" />
            <FirstPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="180" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </FirstPoint>
            <LastPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="0" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </LastPoint>
          </View>
        </Item3>
      </SeriesSerializable>
      <SeriesTemplate CrosshairContentShowMode="Default" CrosshairEmptyValueLegendText="" />
    </DataContainer>
    <Legend HorizontalIndent="40" AlignmentHorizontal="Center" Direction="LeftToRight" CrosshairContentOffset="4" MarkerSize="@2,Width=45@2,Height=16" MarkerMode="CheckBox" MaxCrosshairContentWidth="50" MaxCrosshairContentHeight="0" Name="Default Legend" />
    <Diagram RuntimePaneCollapse="true" RuntimePaneResize="false" PaneLayoutDirection="Vertical" TypeNameSerializable="XYDiagram">
      <AxisX StickToEnd="false" VisibleInPanesSerializable="-1" ShowBehind="false">
        <WholeRange StartSideMargin="21.466666666666665" EndSideMargin="21.466666666666665" SideMarginSizeUnit="AxisUnit" />
      </AxisX>
      <AxisY VisibleInPanesSerializable="-1" ShowBehind="false">
        <WholeRange StartSideMargin="1.4" EndSideMargin="1.4" SideMarginSizeUnit="AxisUnit" />
      </AxisY>
      <SelectionOptions />
    </Diagram>
  </Chart>
</ChartXmlSerializer>









DTTO + DVA ČASOVÉ PRUHY Prázdniny a Vánoce

﻿<?xml version="1.0" encoding="utf-8"?>
<ChartXmlSerializer version="20.1.4.0">
  <Chart AppearanceNameSerializable="Default" SelectionMode="None" SeriesSelectionMode="Series">
    <DataContainer ValidateDataMembers="true" BoundSeriesSorting="None">
      <SeriesSerializable>
        <Item1 Name="Series 1" DataSourceSorted="false" ArgumentDataMember="date" ValueDataMembersSerializable="column0" CrosshairContentShowMode="Default" CrosshairEmptyValueLegendText="">
          <View TypeNameSerializable="LineSeriesView">
            <SeriesPointAnimation TypeNameSerializable="XYMarkerWidenAnimation" />
            <FirstPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="180" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </FirstPoint>
            <LastPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="0" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </LastPoint>
          </View>
        </Item1>
        <Item2 Name="Series 2" DataSourceSorted="false" ArgumentDataMember="date" ValueDataMembersSerializable="column1" CrosshairContentShowMode="Default" CrosshairEmptyValueLegendText="">
          <View TypeNameSerializable="LineSeriesView">
            <SeriesPointAnimation TypeNameSerializable="XYMarkerWidenAnimation" />
            <FirstPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="180" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </FirstPoint>
            <LastPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="0" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </LastPoint>
          </View>
        </Item2>
        <Item3 Name="Series 3" DataSourceSorted="false" ArgumentDataMember="date" ValueDataMembersSerializable="column2" CrosshairContentShowMode="Default" CrosshairEmptyValueLegendText="">
          <View TypeNameSerializable="LineSeriesView">
            <SeriesPointAnimation TypeNameSerializable="XYMarkerWidenAnimation" />
            <FirstPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="180" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </FirstPoint>
            <LastPoint MarkerDisplayMode="Default" LabelDisplayMode="Default" TypeNameSerializable="SidePointMarker">
              <Label Angle="0" TypeNameSerializable="PointSeriesLabel" TextOrientation="Horizontal" />
            </LastPoint>
          </View>
        </Item3>
      </SeriesSerializable>
      <SeriesTemplate CrosshairContentShowMode="Default" CrosshairEmptyValueLegendText="" />
    </DataContainer>
    <Legend HorizontalIndent="40" AlignmentHorizontal="Center" Direction="LeftToRight" CrosshairContentOffset="4" MarkerSize="@2,Width=45@2,Height=16" MarkerMode="CheckBox" MaxCrosshairContentWidth="50" MaxCrosshairContentHeight="0" Name="Default Legend" />
    <Diagram RuntimePaneCollapse="true" RuntimePaneResize="false" PaneLayoutDirection="Vertical" TypeNameSerializable="XYDiagram">
      <AxisX StickToEnd="false" VisibleInPanesSerializable="-1" ShowBehind="false">
        <Strips>
          <Item1 Color="251, 213, 181" LegendText="Prázdniny" Name="Prázdniny">
            <MinLimit AxisValueSerializable="07/01/2020 00:00:00.000" />
            <MaxLimit AxisValueSerializable="09/01/2020 00:00:00.000" />
            <FillStyle FillMode="Gradient">
              <Options GradientMode="BottomToTop" Color2="242, 242, 242" TypeNameSerializable="RectangleGradientFillOptions" />
            </FillStyle>
          </Item1>
          <Item2 Color="183, 221, 232" LegendText="Vánoce" Name="Vánoce">
            <MinLimit AxisValueSerializable="12/23/2020 00:00:00.000" />
            <MaxLimit AxisValueSerializable="12/31/2020 00:00:00.000" />
            <FillStyle FillMode="Gradient">
              <Options GradientMode="BottomToTop" Color2="242, 242, 242" TypeNameSerializable="RectangleGradientFillOptions" />
            </FillStyle>
          </Item2>
        </Strips>
        <WholeRange StartSideMargin="21.466666666666665" EndSideMargin="21.466666666666665" SideMarginSizeUnit="AxisUnit" />
      </AxisX>
      <AxisY VisibleInPanesSerializable="-1" ShowBehind="false">
        <WholeRange StartSideMargin="232.5" EndSideMargin="232.5" SideMarginSizeUnit="AxisUnit" />
      </AxisY>
      <SelectionOptions />
    </Diagram>
  </Chart>
</ChartXmlSerializer>













Legenda:


    <Legend HorizontalIndent="40" AlignmentHorizontal="Left" CrosshairContentOffset="4" MarkerSize="@2,Width=45@2,Height=16" MarkerMode="CheckBox" MaxCrosshairContentWidth="50" MaxCrosshairContentHeight="0" Name="Default Legend" />

    <Legend HorizontalIndent="40" AlignmentHorizontal="Left" Direction="BottomToTop" CrosshairContentOffset="4" MarkerSize="@2,Width=45@2,Height=16" MarkerMode="CheckBox" MaxCrosshairContentWidth="50" MaxCrosshairContentHeight="0" Name="Default Legend" />
    <Legend HorizontalIndent="40" AlignmentHorizontal="Left" AlignmentVertical="Center" Direction="BottomToTop" CrosshairContentOffset="4" MarkerSize="@2,Width=45@2,Height=16" MarkerMode="CheckBox" MaxCrosshairContentWidth="50" MaxCrosshairContentHeight="0" Name="Default Legend" />
    <Legend HorizontalIndent="40" AlignmentHorizontal="LeftOutside" AlignmentVertical="Center" Direction="BottomToTop" CrosshairContentOffset="4" MarkerSize="@2,Width=45@2,Height=16" MarkerMode="CheckBox" MaxCrosshairContentWidth="50" MaxCrosshairContentHeight="0" Name="Default Legend" />
    <Legend HorizontalIndent="40" AlignmentHorizontal="LeftOutside" AlignmentVertical="BottomOutside" Direction="BottomToTop" CrosshairContentOffset="4" MarkerSize="@2,Width=45@2,Height=16" MarkerMode="CheckBox" MaxCrosshairContentWidth="50" MaxCrosshairContentHeight="0" Name="Default Legend" />
    <Legend HorizontalIndent="40" AlignmentHorizontal="Left" AlignmentVertical="TopOutside" Direction="BottomToTop" CrosshairContentOffset="4" MarkerSize="@2,Width=45@2,Height=16" MarkerMode="CheckBox" MaxCrosshairContentWidth="50" MaxCrosshairContentHeight="0" Name="Default Legend" />


    <Legend HorizontalIndent="40" AlignmentHorizontal="Left" Direction="LeftToRight" CrosshairContentOffset="4" MarkerSize="@2,Width=45@2,Height=16" MarkerMode="CheckBox" MaxCrosshairContentWidth="50" MaxCrosshairContentHeight="0" Name="Default Legend" />




MarkerSize="@2,Width=45@2,Height=16"  = prostor a border checkboxu




    <Legend HorizontalIndent="40" AlignmentHorizontal="Left" Direction="BottomToTop" CrosshairContentOffset="4" MarkerSize="@2,Width=30@2,Height=16" MarkerMode="CheckBox" MaxCrosshairContentWidth="50" MaxCrosshairContentHeight="0" Name="Default Legend">
      <Title WordWrap="false" MaxLineCount="0" Alignment="Center" Text="Jednotlivé řady" Visible="true" Font="Tahoma, 12pt" TextColor="" Antialiasing="false" />
    </Legend>

má titulek




    <Legend HorizontalIndent="40" AlignmentHorizontal="Left" Direction="BottomToTop" TextOffset="15" CrosshairContentOffset="4" MarkerSize="@2,Width=30@2,Height=16" MarkerMode="CheckBox" MaxCrosshairContentWidth="50" MaxCrosshairContentHeight="0" Name="Default Legend">
      <Title WordWrap="false" MaxLineCount="0" Alignment="Center" Text="Jednotlivé řady" Visible="true" Font="Tahoma, 12pt" TextColor="" Antialiasing="false" />
    </Legend>
text offset = mezera mezi checkboxem a textem




    <Legend HorizontalIndent="40" AlignmentHorizontal="Left" Direction="BottomToTop" TextOffset="15" CrosshairContentOffset="4" MarkerSize="@2,Width=30@2,Height=16" MarkerMode="CheckBoxAndMarker" MaxCrosshairContentWidth="50" MaxCrosshairContentHeight="0" Name="Default Legend">
      <Title WordWrap="false" MaxLineCount="0" Alignment="Center" Text="Jednotlivé řady" Visible="true" Font="Tahoma, 12pt" TextColor="" Antialiasing="false" />
    </Legend>

MarkerMode = checkbox, čára, text





STRIPES V OSE Y = pro číslo R :


      <AxisY Alignment="Far" VisibleInPanesSerializable="-1" ShowBehind="false">
        <Strips>
          <Item1 Color="118, 146, 60" ShowInLegend="false" Name="Strip 1">
            <MinLimit AxisValueSerializable="0.75" />
            <MaxLimit AxisValueSerializable="0.85" />
          </Item1>
          <Item2 Color="146, 205, 220" ShowInLegend="false" Name="Strip 2">
            <MinLimit AxisValueSerializable="0.85" />
            <MaxLimit AxisValueSerializable="1" />
            <FillStyle FillMode="Solid" />
          </Item2>
          <Item3 Color="217, 150, 148" ShowInLegend="false" Name="Strip 3">
            <MinLimit AxisValueSerializable="1" />
            <MaxLimit AxisValueSerializable="1.1" />
          </Item3>
          <Item4 Color="149, 55, 52" ShowInLegend="false" Name="Strip 4">
            <MinLimit AxisValueSerializable="1.1" />
            <MaxLimit AxisValueSerializable="1.3" />
          </Item4>
        </Strips>
        <WholeRange StartSideMargin="0.16599999999999998" EndSideMargin="0.16599999999999998" SideMarginSizeUnit="AxisUnit" />
      </AxisY>



            */
            #endregion



            string settings = @"﻿<?xml version='1.0' encoding='utf-8'?>
<ChartXmlSerializer version='20.1.4.0'>
  <Chart AppearanceNameSerializable='Default' SelectionMode='None' SeriesSelectionMode='Series'>
    <DataContainer ValidateDataMembers='true' BoundSeriesSorting='None'>
      <SeriesSerializable>
        <Item1 Name='Series 1' DataSourceSorted='false' ArgumentDataMember='date' ValueDataMembersSerializable='column0' CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText=''>
          <View TypeNameSerializable='LineSeriesView'>
            <SeriesPointAnimation TypeNameSerializable='XYMarkerWidenAnimation' />
            <FirstPoint MarkerDisplayMode='Default' LabelDisplayMode='Default' TypeNameSerializable='SidePointMarker'>
              <Label Angle='180' TypeNameSerializable='PointSeriesLabel' TextOrientation='Horizontal' />
            </FirstPoint>
            <LastPoint MarkerDisplayMode='Default' LabelDisplayMode='Default' TypeNameSerializable='SidePointMarker'>
              <Label Angle='0' TypeNameSerializable='PointSeriesLabel' TextOrientation='Horizontal' />
            </LastPoint>
          </View>
        </Item1>
        <Item2 Name='Series 2' DataSourceSorted='false' ArgumentDataMember='date' ValueDataMembersSerializable='column1' CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText=''>
          <View TypeNameSerializable='LineSeriesView'>
            <SeriesPointAnimation TypeNameSerializable='XYMarkerWidenAnimation' />
            <FirstPoint MarkerDisplayMode='Default' LabelDisplayMode='Default' TypeNameSerializable='SidePointMarker'>
              <Label Angle='180' TypeNameSerializable='PointSeriesLabel' TextOrientation='Horizontal' />
            </FirstPoint>
            <LastPoint MarkerDisplayMode='Default' LabelDisplayMode='Default' TypeNameSerializable='SidePointMarker'>
              <Label Angle='0' TypeNameSerializable='PointSeriesLabel' TextOrientation='Horizontal' />
            </LastPoint>
          </View>
        </Item2>
        <Item3 Name='Series 3' DataSourceSorted='false' ArgumentDataMember='date' ValueDataMembersSerializable='column2' CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText=''>
          <View TypeNameSerializable='LineSeriesView'>
            <SeriesPointAnimation TypeNameSerializable='XYMarkerWidenAnimation' />
            <FirstPoint MarkerDisplayMode='Default' LabelDisplayMode='Default' TypeNameSerializable='SidePointMarker'>
              <Label Angle='180' TypeNameSerializable='PointSeriesLabel' TextOrientation='Horizontal' />
            </FirstPoint>
            <LastPoint MarkerDisplayMode='Default' LabelDisplayMode='Default' TypeNameSerializable='SidePointMarker'>
              <Label Angle='0' TypeNameSerializable='PointSeriesLabel' TextOrientation='Horizontal' />
            </LastPoint>
          </View>
        </Item3>
      </SeriesSerializable>
      <SeriesTemplate CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText='' />
    </DataContainer>
    <Legend HorizontalIndent='40' AlignmentHorizontal='Center' Direction='LeftToRight' CrosshairContentOffset='4' MarkerSize='@2,Width=45@2,Height=16' MarkerMode='CheckBox' MaxCrosshairContentWidth='50' MaxCrosshairContentHeight='0' Name='Default Legend' />
    <Diagram RuntimePaneCollapse='true' RuntimePaneResize='false' PaneLayoutDirection='Vertical' TypeNameSerializable='XYDiagram'>
      <AxisX StickToEnd='false' VisibleInPanesSerializable='-1' ShowBehind='false'>
        <Strips>
          <Item1 Color='251, 213, 181' LegendText='Prázdniny' Name='Prázdniny'>
            <MinLimit AxisValueSerializable='01.07.2020 00:00:00.000' />
            <MaxLimit AxisValueSerializable='01.09.2020 00:00:00.000' />
            <FillStyle FillMode='Gradient'>
              <Options GradientMode='BottomToTop' Color2='242, 242, 242' TypeNameSerializable='RectangleGradientFillOptions' />
            </FillStyle>
          </Item1>
          <Item2 Color='183, 221, 232' LegendText='Vánoce' Name='Vánoce'>
            <MinLimit AxisValueSerializable='23.12.2020 00:00:00.000' />
            <MaxLimit AxisValueSerializable='01.01.2021 00:00:00.000' />
            <FillStyle FillMode='Gradient'>
              <Options GradientMode='BottomToTop' Color2='242, 242, 242' TypeNameSerializable='RectangleGradientFillOptions' />
            </FillStyle>
          </Item2>
        </Strips>
        <WholeRange StartSideMargin='21.466666666666665' EndSideMargin='21.466666666666665' SideMarginSizeUnit='AxisUnit' />
      </AxisX>
      <AxisY VisibleInPanesSerializable='-1' ShowBehind='false'>
        <WholeRange StartSideMargin='232.5' EndSideMargin='232.5' SideMarginSizeUnit='AxisUnit' />
      </AxisY>
      <SelectionOptions />
    </Diagram>
  </Chart>
</ChartXmlSerializer>";
            settings = settings.Replace("'", "\"");
            return settings;
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
            this.FillDefaultValues();
        }
        protected void FillDefaultValues()
        {
            ValueType = DataValueType.CurrentCount;
            this.IsAnalyticSerie = false;
            this.AnalyseEntityLevel = EntityType.Mesto;
            this.AnalyseLowestCount = 2;
            this.AnalyseHighestCount = 4;
            this.AnalyseAddRootResult = false;
            this.AnalyseTimeLastDays = 14;
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
        public string EntityFullCode { get; set; }
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
        /// true pokud this položka je analytická, viz properties "Analytic..."
        /// </summary>
        public bool IsAnalyticSerie { get; set; }
        /// <summary>
        /// Analyzovat data určité úrovně
        /// </summary>
        public EntityType AnalyseEntityLevel { get; set; }
        /// <summary>
        /// Do výsledku zahrnout tento počet nejnižších hodnot
        /// </summary>
        public int AnalyseLowestCount { get; set; }
        /// <summary>
        /// Do výsledku zahrnout tento počet nejvyšších hodnot
        /// </summary>
        public int AnalyseHighestCount { get; set; }
        /// <summary>
        /// Do výsledku zahrnout výsledky výchozí entity (jakožto střed)
        /// </summary>
        public bool AnalyseAddRootResult { get; set; }
        /// <summary>
        /// Analyzovat tento počet dní ode dneška do minulosti
        /// </summary>
        public int AnalyseTimeLastDays { get; set; }
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
                    this.EntityFullCode = GetValue(text, "");
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

                case ChartSeriesIsAnalyticSerie:
                    this.IsAnalyticSerie = GetValue(text, false);
                    break;
                case ChartSeriesAnalyseEntityLevel:
                    this.AnalyseEntityLevel = GetValueEnum<EntityType>(text, EntityType.Mesto);
                    break;
                case ChartSeriesAnalyseLowestCount:
                    this.AnalyseLowestCount = GetValue(text, 2);
                    break;
                case ChartSeriesAnalyseHighestCount:
                    this.AnalyseHighestCount = GetValue(text, 4);
                    break;
                case ChartSeriesAnalyseAddRootResult:
                    this.AnalyseAddRootResult = GetValue(text, false);
                    break;
                case ChartSeriesAnalyseTimeLastDays:
                    this.AnalyseTimeLastDays = GetValue(text, 14);
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
            stream.WriteLine(CreateLine(ChartSeriesEntityCode, GetSerial(this.EntityFullCode)));
            stream.WriteLine(CreateLine(ChartSeriesValueType, GetSerialEnum(this.ValueType)));

            if (this.FiltrPocetObyvatelOd.HasValue)
                stream.WriteLine(CreateLine(ChartSeriesFiltrPocetOd, GetSerial(this.FiltrPocetObyvatelOd)));
            if (this.FiltrPocetObyvatelDo.HasValue)
                stream.WriteLine(CreateLine(ChartSeriesFiltrPocetDo, GetSerial(this.FiltrPocetObyvatelDo)));

            if (this.IsAnalyticSerie)
            {
                stream.WriteLine(CreateLine(ChartSeriesIsAnalyticSerie, GetSerial(this.IsAnalyticSerie)));
                stream.WriteLine(CreateLine(ChartSeriesAnalyseEntityLevel, GetSerialEnum<EntityType>(this.AnalyseEntityLevel)));
                stream.WriteLine(CreateLine(ChartSeriesAnalyseLowestCount, GetSerial(this.AnalyseLowestCount)));
                stream.WriteLine(CreateLine(ChartSeriesAnalyseHighestCount, GetSerial(this.AnalyseHighestCount)));
                stream.WriteLine(CreateLine(ChartSeriesAnalyseAddRootResult, GetSerial(this.AnalyseAddRootResult)));
                stream.WriteLine(CreateLine(ChartSeriesAnalyseTimeLastDays, GetSerial(this.AnalyseTimeLastDays)));
            }

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

        private const string ChartSeriesIsAnalyticSerie = GraphInfo.ChartSeriesPrefix + "IsAnalyticSerie";
        private const string ChartSeriesAnalyseEntityLevel = GraphInfo.ChartSeriesPrefix + "AnalyseEntityLevel";
        private const string ChartSeriesAnalyseLowestCount = GraphInfo.ChartSeriesPrefix + "AnalyseLowestCount";
        private const string ChartSeriesAnalyseHighestCount = GraphInfo.ChartSeriesPrefix + "AnalyseHighestCount";
        private const string ChartSeriesAnalyseAddRootResult = GraphInfo.ChartSeriesPrefix + "AnalyseAddRootResult";
        private const string ChartSeriesAnalyseTimeLastDays = GraphInfo.ChartSeriesPrefix + "AnalyseTimeLastDays";

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
            string fullCode = this.EntityFullCode;
            var entity = database.GetEntity(fullCode);
            if (entity is null) return;

            if (!this.IsAnalyticSerie)
                this.LoadDataSimple(entity, database, graphData);
            else
                this.LoadDataAnalytic(entity, database, graphData);
        }
        /// <summary>
        /// Načte data Simple = nikoli analytická
        /// </summary>
        /// <param name="database"></param>
        /// <param name="graphData"></param>
        private void LoadDataSimple(DatabaseInfo.EntityInfo entity, DatabaseInfo database, GraphData graphData)
        {
            var result = database.GetResultSimple(entity, this.ValueType, this.ValueTypeInfo, graphData.DateBegin, graphData.DateEnd, this.FiltrPocetObyvatelOd, this.FiltrPocetObyvatelDo);
            this.AddDataResult(result, graphData, false);
        }
        /// <summary>
        /// Načte analytická data = více serií
        /// </summary>
        /// <param name="database"></param>
        /// <param name="graphData"></param>
        private void LoadDataAnalytic(DatabaseInfo.EntityInfo entity, DatabaseInfo database, GraphData graphData)
        {
            EntityType analyseEntityLevel = this.AnalyseEntityLevel;
            int lowestCount = this.AnalyseLowestCount;
            bool addRootResult = this.AnalyseAddRootResult;
            int highestCount = this.AnalyseHighestCount;
            int lastDays = this.AnalyseTimeLastDays;
            if (lastDays < 3) lastDays = 3;
            if (lastDays > 100) lastDays = 100;
            DateTime analyseEnd = graphData.DateEnd ?? DateTime.Now.Date.AddDays(+1);
            DateTime analyseBegin = analyseEnd.AddDays(-lastDays);

            var results = database.GetResultsAnalytic(entity, this.ValueType, analyseEntityLevel,
                highestCount, addRootResult, lowestCount, analyseBegin, analyseEnd,
                graphData.DateBegin, graphData.DateEnd, this.FiltrPocetObyvatelOd, this.FiltrPocetObyvatelDo);

            // Výsledná data grafů:
            foreach (var result in results.Item1)
                this.AddDataResult(result, graphData, true);

            // Výsledné nápočty:
            graphData.AddCounts(results.Item2);
        }
        /// <summary>
        /// Do výsledných dat grafu vloží jednu serii z daného výsledku
        /// </summary>
        /// <param name="result"></param>
        /// <param name="graphData"></param>
        /// <param name="isAnalytic"></param>
        private void AddDataResult(ResultSetInfo result, GraphData graphData, bool isAnalytic)
        {
            var column = graphData.AddColumn(this, result.Entity, isAnalytic);
            if (result != null)
            {
                foreach (var item in result.Results)
                    graphData.AddCell(item.Date, column, item.Value);
                if (!isAnalytic)
                    graphData.AddCounts(result);
            }
        }
        /// <summary>
        /// Vrátí defaultní titulek pro danou entitu a typ hodnoty
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        internal static string GetDefaultTitle(DatabaseInfo.EntityInfo entity, DataValueType valueType)
        {
            DataValueTypeInfo valueTypeInfo = DataValueTypeInfo.CreateFor(valueType);
            return GetDefaultTitle(entity, valueTypeInfo);
        }
        /// <summary>
        /// Vrátí defaultní titulek pro danou entitu a typ hodnoty
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="valueTypeInfo"></param>
        /// <returns></returns>
        internal static string GetDefaultTitle(DatabaseInfo.EntityInfo entity, DataValueTypeInfo valueTypeInfo)
        {
            return $"{entity.Nazev}, {valueTypeInfo.ShortText}";
        }
        #endregion
        #region Kontroly
        /// <summary>
        /// přidá text všech svých chyb
        /// </summary>
        public void AddErrors(StringBuilder sb)
        {
            if (String.IsNullOrEmpty(this.Title)) sb.AppendLine("- Titulek datové řady není zadán");
            if (((int)this.ValueType) == 0) sb.AppendLine($"- Typ dat v datové řadě {Title} není zadán");
            if (String.IsNullOrEmpty(this.EntityFullCode)) sb.AppendLine($"- Místo (okres, obec) v datové řadě {Title} není zadáno");
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
                    result = new DataValueTypeInfo(valueType, "Denní počet nových případů", "nové za den",
                        "Neupravený počet nově nalezených případů", 
                        GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Dot,
                        0, 0);
                    return true;
                case DataValueType.NewCountAvg:
                    result = new DataValueTypeInfo(valueType, "Průměrný denní přírůstek", "nové za den, průměr",
                        "Počet nově nalezených případů, týdenní průměr", 
                        GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Solid,
                        -8, +4);
                    return true;
                case DataValueType.NewCountRelative:
                    result = new DataValueTypeInfo(valueType, "Relativní přírůstek na 100tis obyvatel", "nové za den / 100T",
                        "Počet nově nalezených případů, přepočtený na 100 000 obyvatel, vhodné k porovnání různých regionů", 
                        GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Dash,
                        0, 0);
                    return true;
                case DataValueType.NewCountRelativeAvg:
                    result = new DataValueTypeInfo(valueType, "Relativní přírůstek na 100t, zprůměrovaný", "nové za den, průměr / 100T",
                        "Počet nově nalezených případů, přepočtený na 100 000 obyvatel, týdenní průměr", 
                        GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Solid,
                        -8, +4);
                    return true;

                case DataValueType.NewCount7DaySum:
                    result = new DataValueTypeInfo(valueType, "Součet za posledních 7 dní", "nové za týden",
                        "Počet nově nalezených případů, sečtený za posledních 7 dní", 
                        GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Dot,
                        -7, 0);
                    return true;
                case DataValueType.NewCount7DaySumAvg:
                    result = 
                        new DataValueTypeInfo(valueType, "Součet za posledních 7 dní, průměrovaný", "nové za týden, průměr",
                        "Počet nově nalezených případů, sečtený za posledních 7 dní, průměrovaný", 
                        GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Solid,
                        -14, +4);
                    return true;
                case DataValueType.NewCount7DaySumRelative:
                    result = new DataValueTypeInfo(valueType, "Součet za týden na 100tis obyvatel", "nové za týden / 100T",
                        "Počet nově nalezených případů, sečtený za posledních 7 dní, přepočtený na 100 000 obyvatel, vhodné k porovnání různých regionů", 
                        GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Dash,
                        -7, 0);
                    return true;
                case DataValueType.NewCount7DaySumRelativeAvg:
                    result = new DataValueTypeInfo(valueType, "Součet za týden na 100tis obyvatel, průměrovaný", "nové za týden, průměr / 100T",
                        "Počet nově nalezených případů, sečtený za posledních 7 dní, průměrovaný, přepočtený na 100 000 obyvatel, vhodné k porovnání různých regionů", 
                        GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Solid,
                        -14, +4);
                    return true;

                case DataValueType.CurrentCount:
                    result = new DataValueTypeInfo(valueType, "Aktuální stav případů", "aktuální stav",
                        "Aktuální počet pozitivních osob", 
                        GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Dot,
                        0, 0);
                    return true;
                case DataValueType.CurrentCountAvg:
                    result = new DataValueTypeInfo(valueType, "Aktuální stav, průměr za 7 dní", "aktuální stav, průměr",
                        "Aktuální počet pozitivních osob, průměr za 7 dní", 
                        GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Solid,
                         -7, +4);
                    return true;
                case DataValueType.CurrentCountRelative:
                    result = new DataValueTypeInfo(valueType, "Aktuální stav případů na 100tis obyvatel", "aktuální stav / 100T",
                        "Aktuální počet pozitivních osob, přepočtený na 100 000 obyvatel, vhodné k porovnání různých regionů", 
                        GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Dash,
                        0, 0);
                    return true;
                case DataValueType.CurrentCountRelativeAvg:
                    result = new DataValueTypeInfo(valueType, "Aktuální stav případů na 100tis obyvatel, průměr za 7 dní", "aktuální stav, průměr / 100T", 
                        "Aktuální počet pozitivních osob, průměr za 7 dní, přepočtený na 100 000 obyvatel, vhodné k porovnání různých regionů", 
                        GraphSerieAxisType.BigValuesLinear, EntityType.Vesnice, LineDashStyleType.Solid,
                        -7, +4);
                    return true;

                case DataValueType.RZero:
                    result = new DataValueTypeInfo(valueType, "Reprodukční číslo R0", "číslo R0", 
                        "Reprodukční číslo = poměr počtu nových případů (průměrný za 7 dní) vůči počtu nových případů (průměrnému) před pěti dny", 
                        GraphSerieAxisType.SmallValuesLinear, EntityType.Vesnice, LineDashStyleType.Dot,
                        -6, 0);
                    return true;
                case DataValueType.RZeroAvg:
                    result = new DataValueTypeInfo(valueType, "Reprodukční číslo R0, průměr za 7dní", "číslo R0, průměr",
                        "Reprodukční číslo = poměr počtu nových případů (průměrný za 7 dní) vůči počtu nových případů (průměrnému) před pěti dny, kdy výsledek je zprůměrovaný za 7 dní", 
                        GraphSerieAxisType.SmallValuesLinear, EntityType.Vesnice, LineDashStyleType.Solid,
                        -14, +4);
                    return true;
            }
            result = null;
            return false;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="text">Krátký text, do titulku grafu</param>
        /// <param name="shortText"></param>
        /// <param name="toolTip"></param>
        /// <param name="axisType"></param>
        /// <param name="entityType"></param>
        /// <param name="suggestedDashStyle"></param>
        /// <param name="dateOffsetBefore"></param>
        /// <param name="dateOffsetAfter"></param>
        /// <param name="icon"></param>
        private DataValueTypeInfo(DataValueType value, string text, string shortText, string toolTip, 
            GraphSerieAxisType axisType, EntityType entityType, LineDashStyleType suggestedDashStyle,
            int? dateOffsetBefore = null, int? dateOffsetAfter = null, System.Drawing.Image icon = null)
            : base(value, text, toolTip, icon)
        {
            this.ShortText = shortText;
            this.Value = value;
            this.AxisType = axisType;
            this.EntityType = entityType;
            this.SuggestedDashStyle = suggestedDashStyle;
            this.DateOffsetBefore = dateOffsetBefore;
            this.DateOffsetAfter = dateOffsetAfter;
        }
        /// <summary>
        /// Krátký text, do titulku grafu
        /// </summary>
        public string ShortText { get; protected set; }
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
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="graphInfo"></param>
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
        public GraphColumn AddColumn(GraphSerieInfo serie, DatabaseInfo.EntityInfo entity, bool isAnalytic)
        {
            int index = ColumnId++;
            string columnNameData = "column" + index.ToString();
            GraphColumn column = new GraphColumn(this, index, columnNameData, serie, entity, isAnalytic);
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
            this.GraphScanCounts = new GraphScanCountsInfo();
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
        /// Nápočty dat z analýzy
        /// </summary>
        public GraphScanCountsInfo GraphScanCounts { get; private set; }
        /// <summary>
        /// Počet sekund načítání dat, textem
        /// </summary>
        public string LoadSecondsText { get { return (LoadTimeEnd.HasValue ? ((TimeSpan)(LoadTimeEnd.Value - LoadTimeBegin)).TotalSeconds.ToString("### ##0.000").Trim() + " sec": "???"); } }
        /// <summary>
        /// Přidá další počty načtených dat
        /// </summary>
        /// <param name="counts"></param>
        public void AddCounts(GraphScanCountsInfo counts)
        {
            this.GraphScanCounts.Add(counts);
        }
        /// <summary>
        /// Přidá další počty načtených dat
        /// </summary>
        /// <param name="result"></param>
        public void AddCounts(ResultSetInfo result)
        {
            this.GraphScanCounts.Add(result);
        }
        /// <summary>
        /// Přidá další počty načtených dat
        /// </summary>
        /// <param name="scanCount"></param>
        /// <param name="loadCount"></param>
        /// <param name="showCount"></param>
        public void AddCounts(int scanCount, int loadCount, int showCount)
        {
            this.GraphScanCounts.Add(scanCount, loadCount, showCount);
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
    /// Nápočty množství dat
    /// </summary>
    public class GraphScanCountsInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GraphScanCountsInfo()
        {
            Clear();
        }
        public void Clear()
        {
            this.ScanRecordCount = 0;
            this.LoadRecordCount = 0;
            this.ShowRecordCount = 0;
        }
        public void Add(GraphScanCountsInfo counts)
        {
            if (counts != null) this.Add(counts.ScanRecordCount, counts.LoadRecordCount, counts.ShowRecordCount);
        }
        public void Add(ResultSetInfo result)
        {
            if (result != null) this.Add(result.ScanRecordCount, result.LoadRecordCount, result.ShowRecordCount);
        }
        public void Add(int scanCount, int loadCount, int showCount)
        {
            if (scanCount > 0) ScanRecordCount += scanCount;
            if (loadCount > 0) LoadRecordCount += loadCount;
            if (showCount > 0) ShowRecordCount += showCount;
        }
        /// <summary>
        /// Počet záznamů prověřovaných, zda vyhoví filtru, textem
        /// </summary>
        public string ScanRecordCountText { get { return ScanRecordCount.ToString("### ### ### ##0").Trim(); } }
        /// <summary>
        /// Počet záznamů prověřovaných, zda vyhoví filtru
        /// </summary>
        public int ScanRecordCount { get; set; }
        /// <summary>
        /// Počet záznamů načtených podle filtru, textem
        /// </summary>
        public string LoadRecordCountText { get { return LoadRecordCount.ToString("### ### ### ##0").Trim(); } }
        /// <summary>
        /// Počet záznamů načtených podle filtru
        /// </summary>
        public int LoadRecordCount { get; set; }
        /// <summary>
        /// Počet záznamů určených k zobrazení v grafu, textem
        /// </summary>
        public string ShowRecordCountText { get { return ShowRecordCount.ToString("### ### ### ##0").Trim(); } }
        /// <summary>
        /// Počet záznamů určených k zobrazení v grafu
        /// </summary>
        public int ShowRecordCount { get; set; }
    }
    /// <summary>
    /// Jeden sloupec, reprezentuje jednu serii grafu
    /// </summary>
    public class GraphColumn
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="graphData"></param>
        /// <param name="columnIndex"></param>
        /// <param name="columnNameData"></param>
        /// <param name="graphSerie"></param>
        /// <param name="dataEntity"></param>
        /// <param name="isAnalytic"></param>
        public GraphColumn(GraphData graphData, int columnIndex, string columnNameData, GraphSerieInfo graphSerie, DatabaseInfo.EntityInfo dataEntity, bool isAnalytic)
        {
            this.GraphData = graphData;
            this.Index = columnIndex;
            this.ColumnNameData = columnNameData;
            this.IsAnalyticColumn = isAnalytic;
            this.GraphSerie = graphSerie;
            this.DataEntity = dataEntity;
            this.Title = (isAnalytic ? GetAnalyticTitle(graphSerie, dataEntity) : this.GraphSerie.Title);
        }
        /// <summary>
        /// Vrátí titulek analytické serie grafu
        /// </summary>
        /// <param name="graphSerie"></param>
        /// <param name="dataEntity"></param>
        /// <returns></returns>
        private string GetAnalyticTitle(GraphSerieInfo graphSerie, DatabaseInfo.EntityInfo dataEntity)
        {
            return GraphSerieInfo.GetDefaultTitle(dataEntity, graphSerie.ValueTypeInfo);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
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
        public bool IsAnalyticColumn { get; private set; }
        public string Title { get; private set; }
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
