using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Djs.Common.Data;
using Djs.Common.Components;

namespace Djs.Common.TestGUI
{
    public partial class TestFormGrid : Form
    {
        public TestFormGrid()
        {
            using (var scope = Application.App.TraceScope(Application.TracePriority.Lowest_2, "TestFormGrid", "Constructor", "Start"))
            {
                using (Application.App.TraceScope("TestFormGrid", "InitializeComponent", ""))
                {
                    InitializeComponent();
                }
                this.StartPosition = FormStartPosition.CenterScreen;
                this.InitGControl();
                this.SizeChanged += new EventHandler(TestFormGrid_SizeChanged);
                this.FormClosed += new FormClosedEventHandler(TestFormGrid_FormClosed);
                scope.Result = "OK";
            }
        }

        void TestFormGrid_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.App.End();
        }
        private void InitGControl()
        {
            this._PrepareSources();

            // Application.App.Zoom = 1.12f;

            using (var scope = Application.App.TraceScope("TestFormGrid", "InitGControl", "Start"))
            {
                this._Toolbar = new GToolbar();
                this._Toolbar.FillFunctionGlobals();
                this._Toolbar.ToolbarSizeChanged += new GPropertyChanged<Services.ComponentSize>(_Toolbar_ToolbarSizeChanged);

                this.Rand = new Random((int)DateTime.Now.Ticks % 0x0FFFFFFF);

                this._Table1 = this._PrepareTableW(48);
                this._Table2 = this._PrepareTableW(128);
                this._TableZ = this._PrepareTableZ(18);

                using (var scope2 = Application.App.TraceScope("TestFormGrid", "InitGControl", "CreateGrid"))
                {
                    this._GridW = new GGrid() { Bounds = new Rectangle(10, 10, 750, 550) };
                    this._GridW.AddTable(this._Table1, "", null, 200);
                    this._GridW.AddTable(this._Table2, "", null, 150);

                    this._Table1.AddRow(new TestItem() { Klic = 60, DatumOd = DateTime.Now.Add(TimeSpan.FromMinutes(50)), DatumDo = DateTime.Now.Add(TimeSpan.FromMinutes(55)) });

                    object[] items = this._Table1.Rows[2].Items;

                    this._SplitterWZ = new GSplitter() { SplitterVisibleWidth = 4, SplitterActiveOverlap = 2, Orientation = Orientation.Vertical, Value = 400, BoundsNonActive = new Int32Range(0, 200) };
                    this._SplitterWZ.ValueChanged += new GPropertyChanged<int>(_SplitterWZ_ValueChanged);
                    this._SplitterWZ.ValueChanging += new GPropertyChanged<int>(_SplitterWZ_ValueChanging);
                    this._GridZ = new GGrid();
                    this._GridZ.AddTable(this._TableZ, "", null, 850);
                }

                using (var scope3 = Application.App.TraceScope("TestFormGrid", "InitGControl", "GControl.AddItem(Grid)"))
                {
                    this.GControl.AddItem(this._Toolbar);
                    this.GControl.AddItem(this._GridW);
                    this.GControl.AddItem(this._GridZ);
                    this.GControl.AddItem(this._SplitterWZ);
                }

                this.ControlsPosition();

                this._Table1.AddRow(new TestItem() { Klic = 70, DatumOd = DateTime.Now.Add(TimeSpan.FromMinutes(60)), DatumDo = DateTime.Now.Add(TimeSpan.FromMinutes(80)) });
                Application.App.TraceInfo("TestFormGrid", "InitGControl", "AddRow done");

                scope.Result = "OK";
            }

            this._LoadDataFromSource();
        }

        void _Toolbar_ToolbarSizeChanged(object sender, GPropertyChangeArgs<Services.ComponentSize> e)
        {
            this.ControlsPosition();
        }

        private void _PrepareSources()
        {
            this._DataSourceList = new List<Services.IDataSource>();
            var plugins = Application.App.GetPlugins(typeof(Services.IDataSource));
            foreach (object plugin in plugins)
            {
                Services.IDataSource source = plugin as Services.IDataSource;
                if (source != null)
                    this._DataSourceList.Add(source);
            }
        }
        private void _LoadDataFromSource()
        {
            Services.IDataSource source = this._DataSourceList.FirstOrDefault();
            if (source == null) return;

            using (Application.App.TraceScope(Application.TracePriority.ElementaryTimeDebug_1, "TestFormGrid", "LoadDataFromSource", "WorkerThread"))
            {

                Services.DataSourceGetDataRequest request = new Services.DataSourceGetDataRequest(null);

                Application.App.ProcessRequestOnbackground<Services.DataSourceGetDataRequest, Services.DataSourceResponse>(source.ProcessRequest, request, this._ProcessResponseData);
            }
        }
        private void _ProcessResponseData(Services.DataSourceGetDataRequest request, Services.DataSourceResponse response)
        {
            if (this.InvokeRequired)
            {
                Application.App.TraceInfo(Application.TracePriority.ElementaryTimeDebug_1, "TestFormGrid", "ProcessResponseData", "WorkerThread", "InvokeGUI");
                this.BeginInvoke(new Action<Services.DataSourceGetDataRequest, Services.DataSourceResponse>(this._ProcessResponseData), request, response);
            }
            else
            {
                Application.App.TraceInfo(Application.TracePriority.ElementaryTimeDebug_1, "TestFormGrid", "ProcessResponseData", "WorkerThread", "Native in GUI");
            }
        }
        private List<Services.IDataSource> _DataSourceList;

        void _SplitterWZ_ValueChanging(object sender, GPropertyChangeArgs<int> e)
        {
            // this.ControlsPosition();
        }

        void _SplitterWZ_ValueChanged(object sender, GPropertyChangeArgs<int> e)
        {
            this.ControlsPosition();
        }

        private DTypeTable<TestItem> _PrepareTableW(int rowCount)
        {
            Image[] images = _LoadImages();
            int imgPointer = 0;
            int imgCount = images.Length;

            DTypeTable<TestItem> table = new DTypeTable<TestItem>();
            using (Application.App.TraceScope("TestFormGrid", "_PrepareTable", "Start", "RowCount: " + rowCount.ToString()))
            {
                DateTime now = DateTime.Now.Date.AddHours(8);
                for (int r = 0; r < rowCount; r++)
                {
                    TestItem row = new TestItem();
                    row.Klic = 10 * (r + 1);
                    row.DatumOd = now.AddMinutes(15 * r);
                    row.DatumDo = now.AddMinutes((15 * r) + 5);
                    row.Graf1 = this._PrepareGraph(now);
                    row.Graf2 = "Obsah grafu 2 je neznámý";
                    row.Price1 = (decimal)(48 + ((12 - r) % 12));
                    row.Photo = images[imgPointer];
                    row.RowHeight = (r % 3 == 0 ? null : (int?)(20 + ((4 * r) % 60)));

                    table.AddRow(row);

                    imgPointer = (++imgPointer) % imgCount;
                }
            }
            return table;
        }
        private DTable _PrepareTableZ(int rowCount)
        {
            DTable table = new DTable();
            using (Application.App.TraceScope("TestFormGrid", "_PrepareTable", "Start", "RowCount: " + rowCount.ToString()))
            {

                #region Jména a příjmení

                // Jména a příjmení jsou ze stránek na internetu, ze seznamů nejčastějších jmen a příjmení (stránky ministerstva vnitra):
                string namesM = "Jiří;Jan;Petr;Josef;Pavel;Jaroslav;Martin;Miroslav;Tomáš;František;Zdeněk;Václav;Karel;Milan;Michal;Vladimír;Lukáš;David;Ladislav;Jakub;Stanislav;Roman;Ondřej;Antonín;Radek;Marek;Daniel;Miloslav;" +
                                "Vojtěch;Jaromír;Filip;Ivan;Aleš;Libor;Oldřich;Rudolf;Vlastimil;Jindřich;Miloš;Adam;Lubomír;Patrik;Bohumil;Luboš;Robert;Matěj;Dominik;Radim;Richard;Ivo;Rostislav;Dušan;Luděk;Vladislav;Bohuslav;Alois;" +
                                "Vit;Vít;Štěpán;Kamil;Ján;Jozef;Zbyněk;Štefan;Viktor;Emil;Michael;Eduard;Vítězslav;Ludvík;René;Marcel;Peter;Dalibor;Radomír;Otakar;Bedřich;Šimon;Břetislav;Vilém;Vratislav;Matyáš;Radovan;Leoš;Marian;" +
                                "Igor;Přemysl;Bohumir;Bohumír;Alexandr;Kryštof;Otto;Arnošt;Svatopluk;Denis;Adolf;Hynek;Erik;Bronislav;Alexander";
                string namesF = "Marie;Jana;Eva;Anna;Hana;Věra;Lenka;Alena;Kateřina;Petra;Lucie;Jaroslava;Ludmila;Helena;Jitka;Martina;Zdeňka;Veronika;Jarmila;Michaela;Ivana;Jiřina;Monika;Tereza;Božena;Zuzana;Vlasta;Markéta;Marcela;" +
                                "Dagmar;Dana;Libuše;Marta;Irena;Miroslava;Barbora;Pavla;Eliška;Růžena;Olga;Kristýna;Andrea;Iveta;Šárka;Pavlína;Blanka;Milada;Zdenka;Klára;Renata;Nikola;Gabriela;Adéla;Radka;Simona;Milena;Miloslava;" +
                                "Iva;Daniela;Miluše;Denisa;Karolína;Romana;Aneta;Ilona;Stanislava;Květoslava;Emilie;Anežka;Naděžda;Soňa;Vladimíra;Kamila;Drahomíra;Danuše;Jindřiška;Natálie;Františka;Renáta;Mária;Alžběta;Vendula;Štěpánka;" +
                                "Bohumila;Ladislava;Magdalena;Dominika;Blažena;Žaneta;Květa;Sabina;Julie;Antonie;Alice;Kristina;Karla;Hedvika;Květuše;Alexandra;Silvie";
                string prijmM = "NOVÁK;SVOBODA;NOVOTNÝ;DVOŘÁK;ČERNÝ;PROCHÁZKA;KUČERA;VESELÝ;KREJČÍ;HORÁK;NĚMEC;MAREK;POSPÍŠIL;POKORNÝ;HÁJEK;KRÁL;JELÍNEK;RŮŽIČKA;BENEŠ;FIALA;SEDLÁČEK;DOLEŽAL;ZEMAN;KOLÁŘ;NAVRÁTIL;ČERMÁK;VANĚK;URBAN;BLAŽEK;" +
                                "KŘÍŽ;KOVÁŘ;KRATOCHVÍL;BARTOŠ;VLČEK;POLÁK;MUSIL;KOPECKÝ;ŠIMEK;KONEČNÝ;MALÝ;HOLUB;ČECH;STANĚK;KADLEC;ŠTĚPÁNEK;DOSTÁL;SOUKUP;ŠŤASTNÝ;MAREŠ;MORAVEC;SÝKORA;TICHÝ;VALENTA;VÁVRA;MATOUŠEK;ŘÍHA;BLÁHA;BUREŠ;ŠEVČÍK;" +
                                "HRUŠKA;MAŠEK;DUŠEK;PAVLÍK;HAVLÍČEK;JANDA;HRUBÝ;MACH;LIŠKA;BEDNÁŘ;MACHÁČEK;VÍTEK;BERAN";
                string prijmF = "NOVÁKOVÁ;SVOBODOVÁ;NOVOTNÁ;DVOŘÁKOVÁ;ČERNÁ;PROCHÁZKOVÁ;KUČEROVÁ;VESELÁ;HORÁKOVÁ;NĚMCOVÁ;MARKOVÁ;POKORNÁ;POSPÍŠILOVÁ;HÁJKOVÁ;KRÁLOVÁ;JELÍNKOVÁ;RŮŽIČKOVÁ;BENEŠOVÁ;FIALOVÁ;SEDLÁČKOVÁ;DOLEŽALOVÁ;ZEMANOVÁ;KOLÁŘOVÁ;" +
                                "NAVRÁTILOVÁ;ČERMÁKOVÁ;VAŇKOVÁ;URBANOVÁ;KRATOCHVÍLOVÁ;ŠIMKOVÁ;BLAŽKOVÁ;KŘÍŽOVÁ;KOPECKÁ;KOVÁŘOVÁ;BARTOŠOVÁ;VLČKOVÁ;POLÁKOVÁ;KONEČNÁ;MUSILOVÁ;ČECHOVÁ;MALÁ;STAŇKOVÁ;ŠTĚPÁNKOVÁ;HOLUBOVÁ;ŠŤASTNÁ;KADLECOVÁ;DOSTÁLOVÁ;" +
                                "SOUKUPOVÁ;MAREŠOVÁ;SÝKOROVÁ;VALENTOVÁ;MORAVCOVÁ;VÁVROVÁ;TICHÁ;MATOUŠKOVÁ;BLÁHOVÁ;ŘÍHOVÁ;MACHOVÁ;MAŠKOVÁ;ŠEVČÍKOVÁ;BUREŠOVÁ;ŠMÍDOVÁ;DUŠKOVÁ;PAVLÍKOVÁ;KREJČOVÁ;JANDOVÁ;HRUŠKOVÁ;HAVLÍČKOVÁ;HRUBÁ;BERANOVÁ;LIŠKOVÁ;BEDNÁŘOVÁ;TOMANOVÁ";

                string[] arrayM1 = namesM.Split(';');
                string[] arrayM2 = prijmM.Split(';');
                string[] arrayF1 = namesF.Split(';');
                string[] arrayF2 = prijmF.Split(';');

                // Profese jsou náhoda:
                string[] arrayP = "Konstruktér;Technolog;Mistr;Svářeč;Elektro;Montér;Obráběč;Slévač;Formíř;Šponař;Jeřábník;Kuchař;Řidič;Skladník;Účetní;Analytik;Programátor;Recepční;Prodavač;Učitel;Klempíř;Pokrývač;Lékař;Úředník".Split(';');

                #endregion

                table.AddColumn(new DColumn("GID", "Ident") { IsVisible = false, SortingEnabled = false });
                table.AddColumn(new DColumn("image", "Foto") { IsVisible = true, SortingEnabled = false, Width = 70 });
                table.AddColumn(new DColumn("name", "Jméno") { IsVisible = true, Width = 200, ToolTip = "Jméno pracovníka" });
                table.AddColumn(new DColumn("profese", "Profese") { IsVisible = true, Width = 120 });
                table.AddColumn(new DColumn("mf", "Kdo je") { IsVisible = false, Width = 50 });

                Image[] images = _LoadImages();

                for (int r = 0; r < rowCount; r++)
                {
                    string mf = (Rand.NextDouble() < 0.333d) ? "F" : "M";
                    Image image = images[Rand.Next(0, images.Length)];
                    string t1 = (mf == "M" ? arrayM1[Rand.Next(0, arrayM1.Length)] : arrayF1[Rand.Next(0, arrayF1.Length)]);
                    string t2 = (mf == "M" ? arrayM2[Rand.Next(0, arrayM2.Length)] : arrayF2[Rand.Next(0, arrayF2.Length)]);
                    string name = t1 + " " + t2;
                    string value = t2 + " " + t1;
                    TextComparable tc = new TextComparable(name, value);

                    string prof = arrayP[Rand.Next(0, arrayP.Length)];

                    table.AddRow(new DRow(r, image, tc, prof, mf));
                }

            }
            return table;
        }
        private Image[] _LoadImages()
        {
            List<Image> images = new List<Image>();
            images.Add(Djs.Common.Components.IconStandard.DocumentSave);
            images.Add(Djs.Common.Components.IconStandard.EditCopy);
            images.Add(Djs.Common.Components.IconStandard.EditCut);
            images.Add(Djs.Common.Components.IconStandard.EditPaste);
            images.Add(Djs.Common.Components.IconStandard.EditUndo);
            images.Add(Djs.Common.Components.IconStandard.EditRedo);
            images.Add(Djs.Common.Components.IconStandard.GoTop);
            images.Add(Djs.Common.Components.IconStandard.GoUp);
            images.Add(Djs.Common.Components.IconStandard.GoDown);
            images.Add(Djs.Common.Components.IconStandard.GoBottom);
            images.Add(Djs.Common.Components.IconStandard.GoHome);
            images.Add(Djs.Common.Components.IconStandard.GoLeft);
            images.Add(Djs.Common.Components.IconStandard.GoRight);
            images.Add(Djs.Common.Components.IconStandard.GoEnd);
            images.Add(Djs.Common.Components.IconStandard.Refresh);
            return images.ToArray();
        }
        private GTimeGraph _PrepareGraph(DateTime now)
        {
            GTimeGraph graph = new GTimeGraph();

            graph.GraphDefaultHeight = 120;
            graph.LineUnitHeight = 18;
            graph.GraphHeightRange = new Int32Range(35, 480);

            DateTime begin, end;
            GTimeGraphItem item;

            // Layer -1 = time frame:
            int workLayer = -1;
            DateTime workBegin = now.Date.AddDays(-1d);
            DateTime workEnd = workBegin.AddDays(62d);
            while (workBegin < workEnd)
            {
                begin = workBegin.AddHours(6d);
                end = workBegin.AddHours(14d);

                item = new GTimeGraphItem();
                item.Layer = workLayer;
                item.Level = 0;
                item.GroupId = item.ItemId;
                item.Time = new TimeRange(begin, end);
                item.Height = 5f;
                item.ToolTip = "Ranní směna";
                item.BackColor = Color.FromArgb(240, 240, 255);
                item.BorderColor = Color.Green;
                graph.ItemList.Add(item);

                item = new GTimeGraphItem();
                item.Layer = workLayer;
                item.Level = -1;
                item.GroupId = item.ItemId;
                item.Time = new TimeRange(begin, end);
                item.Height = 0.40f;
                item.ToolTip = "Ranní směna";
                item.BackColor = Color.FromArgb(240, 240, 255);
                item.BorderColor = Color.Green;
                graph.ItemList.Add(item);



                begin = workBegin.AddHours(14d);
                end = workBegin.AddHours(22d);

                item = new GTimeGraphItem();
                item.Layer = workLayer;
                item.Level = 0;
                item.GroupId = item.ItemId;
                item.Time = new TimeRange(begin, end);
                item.Height = 5f;
                item.ToolTip = "Odpolední směna";
                item.BackColor = Color.FromArgb(240, 255, 240);
                item.BorderColor = Color.Blue;
                graph.ItemList.Add(item);

                item = new GTimeGraphItem();
                item.Layer = workLayer;
                item.Level = -1;
                item.GroupId = item.ItemId;
                item.Time = new TimeRange(begin, end);
                item.Height = 0.40f;
                item.ToolTip = "Odpolední směna";
                item.BackColor = Color.FromArgb(240, 255, 240);
                item.BorderColor = Color.Blue;
                graph.ItemList.Add(item);

                workBegin = workBegin.AddDays(1d);
            }

            List<Color> colors = new List<Color>();
            colors.Add(Color.FromArgb(192, 64, 160 + Rand.Next(0, 32), 64));
            colors.Add(Color.FromArgb(192, 64, 224 + Rand.Next(0, 32), 64));
            colors.Add(Color.FromArgb(192, 64, 64, 160 + Rand.Next(0, 32)));
            colors.Add(Color.FromArgb(192, 64, 64, 224 + Rand.Next(0, 32)));

            int groupId = 0;
            int cnt = Rand.Next(12, 60);
            for (int i = 0; i < cnt; i++)
            {
                groupId++;
                int layer = 1;
                int level = Rand.Next(0, 2);
                int order = Rand.Next(0, 2);
                bool halfling = (Rand.Next(0, 100) < 5);
                float height = (halfling ? 0.5f : 1f);
                int colIdx = 2 * level + order;
                Color backColor = colors[colIdx];
                DateTime start = now.AddMinutes(15 * Rand.Next(0, 28 * 24 * 4));
                int count = Rand.Next(1, 8);
                for (int c = 0; c < count; c++)
                {
                    begin = start;
                    end = begin + TimeSpan.FromMinutes(15 * Rand.Next(2, 1 * 24 * 4));

                    string tooltip = "Událost " + i.ToString();

                    item = new GTimeGraphItem();
                    item.Layer = layer;
                    item.Level = level;
                    item.Order = order;
                    item.GroupId = groupId;
                    item.Time = new TimeRange(begin, end);
                    item.Height = height;
                    item.ToolTip = tooltip;
                    item.BackColor = backColor;
                    item.BorderColor = Color.Black;

                    graph.ItemList.Add(item);

                    start = end + TimeSpan.FromMinutes(15 * Rand.Next(0, 1 * 24 * 4));
                }
            }

            return graph;
        }
        private Random Rand;
        private DTypeTable<TestItem> _Table1;
        private DTypeTable<TestItem> _Table2;
        private DTable _TableZ;
        private GToolbar _Toolbar;
        private GGrid _GridW;
        private GGrid _GridZ;
        private GSplitter _SplitterWZ;
        private void TestFormGrid_SizeChanged(object sender, EventArgs e)
        {
            this.ControlsPosition();
        }
        protected void ControlsPosition()
        {
            using (Application.App.TraceScope("TestFormGrid", "ControlsPosition", "Start"))
            {
                Size size = this.GControl.ClientSize;
                int y = 0;
                if (this._Toolbar != null)
                {
                    this._Toolbar.Bounds = new Rectangle(0, 0, size.Width, 110);
                    y = this._Toolbar.Bounds.Bottom + 1;
                }
                int h = size.Height - y - 5;
                if (this._SplitterWZ != null)
                {
                    int split = this._SplitterWZ.Value;
                    if (this._GridW != null)
                    {
                        this._GridW.Bounds = new Rectangle(5, y, split - 7, h);
                        this._GridW.Refresh();
                    }
                    if (this._GridZ != null)
                    {
                        this._GridZ.Bounds = new Rectangle(split + 2, y, size.Width - 5 - split - 2, h);
                        this._GridW.Refresh();
                    }
                    this._SplitterWZ.BoundsNonActive = new Int32Range(y, y + h);
                    this._SplitterWZ.Refresh();
                    // this.Refresh();
                }
            }
        }
        private void CloseButtonClick(object sender, EventArgs e)
        {
            this.Close();
        }
    }
    public class TextComparable : IComparable
    {
        public TextComparable() { }
        public TextComparable(string text) { this.Text = text; this.Value = text; }
        public TextComparable(string text, IComparable value) { this.Text = text; this.Value = value; }

        public override string ToString()
        {
            return (this.Text == null ? "" : this.Text);
        }
        public string Text { get; set; }
        public IComparable Value { get; set; }

        
        // Summary:
        //     Compares the current instance with another object of the same type and returns
        //     an integer that indicates whether the current instance precedes, follows,
        //     or occurs in the same position in the sort order as the other object.
        //
        // Parameters:
        //   obj:
        //     An object to compare with this instance.
        //
        // Returns:
        //     A value that indicates the relative order of the objects being compared.
        //     The return value has these meanings: Value Meaning Less than zero This instance
        //     precedes obj in the sort order. Zero This instance occurs in the same position
        //     in the sort order as obj. Greater than zero This instance follows obj in
        //     the sort order.
        //
        // Exceptions:
        //   System.ArgumentException:
        //     obj is not the same type as this instance.
        int IComparable.CompareTo(object obj)
        {
            TextComparable other = obj as TextComparable;
            if (other == null) return 1;
            return this.Value.CompareTo(other.Value);
        }
    }
    public class TestItem : IVisualRow
    {
        [ColumnInfo(columnIndex: 0, text: "Klíč záznamu", toolTip: "Klíč popisuje klíčovou vlastnost.", width: 75, widthMin: 60, widthMax: 120, visible: false)]
        public int Klic { get; set; }
        [ColumnInfo(columnIndex: 1, text: "Datum OD", toolTip: "Počátek události.", width: 110)]
        public DateTime DatumOd { get; set; }
        [ColumnInfo(columnIndex: 2, text: "Datum DO", toolTip: "Konec události.", width: 110)]
        public DateTime DatumDo { get; set; }
        [ColumnInfo(columnIndex: 3, text: "Graf1", toolTip: "Graf 1", useTimeAxis: true, width: 850)]
        public GTimeGraph Graf1 { get; set; }
        [ColumnInfo(columnIndex: 4, text: "Graf2", toolTip: "Graf 2", width: 250, visible: false)]
        public string Graf2 { get; set; }
        [ColumnInfo(columnIndex: 5, text: "Cena", toolTip: "Cena 1 položky", width: 65, widthMin: 45, widthMax: 95, visible: false)]
        public decimal Price1 { get; set; }
        [ColumnInfo(columnIndex: 6, text: "Fotografie", toolTip: "Fotografie osoby", width: 65, widthMin: 45, widthMax: 95, visible: false)]
        public Image Photo { get; set; }

        public Int32? RowHeight { get; set; }
        public Int32Range RowHeightRange { get { return null; } }
        public Color? RowBackColor { get; set; }
    }
}
