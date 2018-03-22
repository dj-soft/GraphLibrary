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
            Application.App.TracePriority = Application.TracePriority.Priority1_ElementaryTimeDebug;
            using (var scope = Application.App.TraceScope(Application.TracePriority.Priority2_Lowest, "TestFormGrid", "Constructor", "Start"))
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

                this._Table1 = this._PrepareTableW("stroje", 48);
                this._Table2 = this._PrepareTableW("směny", 128);
                this._TableZ = this._PrepareTableZ("lidi", 18);

                using (var scope2 = Application.App.TraceScope("TestFormGrid", "InitGControl", "CreateGrid"))
                {
                    this._GridW = new GGrid() { Bounds = new Rectangle(10, 10, 750, 550) };
                    this._GridW.AddTable(this._Table1);
                    this._GridW.AddTable(this._Table2);

                    // this._Table1.AddRow(new TestItem() { Klic = 60, DatumOd = DateTime.Now.Add(TimeSpan.FromMinutes(50)), DatumDo = DateTime.Now.Add(TimeSpan.FromMinutes(55)) });

                    Cell[] items = this._Table1.Rows[2].Cells;

                    this._SplitterWZ = new GSplitter() { SplitterVisibleWidth = 4, SplitterActiveOverlap = 2, Orientation = Orientation.Vertical, Value = 400, BoundsNonActive = new Int32NRange(0, 200) };
                    this._SplitterWZ.ValueChanged += new GPropertyChanged<int>(_SplitterWZ_ValueChanged);
                    this._SplitterWZ.ValueChanging += new GPropertyChanged<int>(_SplitterWZ_ValueChanging);
                    this._GridZ = new GGrid();
                    this._GridZ.AddTable(this._TableZ);
                }

                using (var scope3 = Application.App.TraceScope("TestFormGrid", "InitGControl", "GControl.AddItem(Grid)"))
                {
                    this.GControl.AddItem(this._Toolbar);
                    this.GControl.AddItem(this._GridW);
                    this.GControl.AddItem(this._GridZ);
                    this.GControl.AddItem(this._SplitterWZ);
                }

                this.ControlsPosition();

                // this._Table1.AddRow(new TestItem() { Klic = 70, DatumOd = DateTime.Now.Add(TimeSpan.FromMinutes(60)), DatumDo = DateTime.Now.Add(TimeSpan.FromMinutes(80)) });
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

            using (Application.App.TraceScope(Application.TracePriority.Priority1_ElementaryTimeDebug, "TestFormGrid", "LoadDataFromSource", "WorkerThread"))
            {

                Services.DataSourceGetDataRequest request = new Services.DataSourceGetDataRequest(null);

                Application.App.ProcessRequestOnbackground<Services.DataSourceGetDataRequest, Services.DataSourceResponse>(source.ProcessRequest, request, this._ProcessResponseData);
            }
        }
        private void _ProcessResponseData(Services.DataSourceGetDataRequest request, Services.DataSourceResponse response)
        {
            if (this.InvokeRequired)
            {
                Application.App.TraceInfo(Application.TracePriority.Priority1_ElementaryTimeDebug, "TestFormGrid", "ProcessResponseData", "WorkerThread", "InvokeGUI");
                this.BeginInvoke(new Action<Services.DataSourceGetDataRequest, Services.DataSourceResponse>(this._ProcessResponseData), request, response);
            }
            else
            {
                Application.App.TraceInfo(Application.TracePriority.Priority1_ElementaryTimeDebug, "TestFormGrid", "ProcessResponseData", "WorkerThread", "Native in GUI");
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
        private Table _PrepareTableW(string name, int rowCount)
        {
            Image[] images = _LoadImages();
            int imgPointer = 0;
            int imgCount = images.Length;

            Table table = new Table(name);
            using (Application.App.TraceScope("TestFormGrid", "_PrepareTable", "Start", "RowCount: " + rowCount.ToString()))
            {
                table.AddColumns
                    (
                        new Column() { Title = "Klíč", ToolTip = "Klíč záznamu v tomto sloupci", Name = "key", FormatString = "0000000", Width = 60 },
                        new Column() { Title = "Datum OD", ToolTip = "Počáteční datum směny", Name = "date_from", FormatString = "yyyy-MM-dd HH:mm", Width = 80 },
                        new Column() { Title = "Datum DO", ToolTip = "Koncové datum směny", Name = "date_to", FormatString = "yyyy-MM-dd HH:mm:ss", Width = 80 },
                        new Column() { Title = "graf", ToolTip = "Graf vytížení", Name = "graph1", UseTimeAxis = true, Width = 180, AutoWidth = true, SortingEnabled = false },
                        new Column() { Title = "Cena jednotky", ToolTip = "Jednotková cena.\r\nJe zde jen pro informaci.", Name = "price1", FormatString = "### ##0.00", Width = 80 },
                        new Column() { Title = "Fotografie", ToolTip = "Zobrazení", Name = "image", Width = 60, SortingEnabled = false }
                    );

                DateTime now = DateTime.Now.Date.AddHours(8);
                for (int r = 0; r < rowCount; r++)
                {
                    int klic = 10 * (r + 1);
                    DateTime datumOd = now.AddMinutes(15 * r);
                    DateTime datumDo = now.AddMinutes((15 * r) + 5);
                    GTimeGraph graph1 = this._PrepareGraph(now);
                    double price = Math.Round((this.Rand.NextDouble() * 100000d), 2);
                    Image image = images[imgPointer];
                    int height = ((this.Rand.Next(0, 100) > 80) ? 65 : 25);

                    Row row = new Row(klic, datumOd, datumDo, graph1, price, image);

                    if (this.Rand.Next(0, 100) > 80)
                        row.Height = 65;

                    table.AddRow(row);

                    Cell cim = row["image"];
                    cim.UseImageAsToolTip = true;

                    imgPointer = (++imgPointer) % imgCount;
                }
            }
            return table;
        }
        private Table _PrepareTableZ(string name, int rowCount)
        {
            Table table = new Table(name);
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

                table.AddColumns
                    (
                        new Column() { Name = "id", Title = "ID", IsVisible = false },
                        new Column() { Name = "photo", Title = "Fotografie", SortingEnabled = false, Width = 45 },
                        new Column() { Name = "nazev", Title = "Jméno", SortingEnabled = true, Width = 200 },
                        new Column() { Name = "prof", Title = "Profese", SortingEnabled = true, Width = 150 },
                        new Column() { Name = "gender", Title = "Rod", SortingEnabled = false, Width = 35 }
                    );

                Image[] images = _LoadImages();

                for (int r = 0; r < rowCount; r++)
                {
                    string mf = (Rand.NextDouble() < 0.333d) ? "F" : "M";
                    Image image = images[Rand.Next(0, images.Length)];
                    string t1 = (mf == "M" ? arrayM1[Rand.Next(0, arrayM1.Length)] : arrayF1[Rand.Next(0, arrayF1.Length)]);
                    string t2 = (mf == "M" ? arrayM2[Rand.Next(0, arrayM2.Length)] : arrayF2[Rand.Next(0, arrayF2.Length)]);
                    string nazev = t1 + " " + t2;
                    string value = t2 + " " + t1;

                    // TextComparable obsahuje Text = "Jméno Příjmení" (čitelné pro uživatelů), a porovnávací hodnotu = "Příjmení Jméno" (vhodné pro třídění):
                    TextComparable tc = new TextComparable(nazev, value);

                    string prof = arrayP[Rand.Next(0, arrayP.Length)];

                    Row row = new Row(r, image, tc, prof, mf);

                    Cell imageCell = row[1];
                    imageCell.ToolTip = nazev;
                    imageCell.UseImageAsToolTip = true;

                    Cell nameCell = row[2];
                    nameCell.ToolTip = nazev;
                    nameCell.ToolTipImage = image;

                    table.AddRow(row);
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
            graph.GraphHeightRange = new Int32NRange(35, 480);

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
        private Table _Table1;
        private Table _Table2;
        private Table _TableZ;
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

                y += 3;
                int h = size.Height - y - 6;
                if (this._SplitterWZ != null)
                {
                    int split = this._SplitterWZ.Value;
                    if (this._GridW != null)
                    {
                        this._GridW.Bounds = new Rectangle(5, y, split - 8, h);
                        this._GridW.Refresh();
                    }
                    if (this._GridZ != null)
                    {
                        this._GridZ.Bounds = new Rectangle(split + 3, y, size.Width - 5 - split - 6, h);
                        this._GridW.Refresh();
                    }
                    this._SplitterWZ.BoundsNonActive = new Int32NRange(y, y + h);
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
        int IComparable.CompareTo(object obj)
        {
            TextComparable other = obj as TextComparable;
            if (other == null) return 1;
            return this.Value.CompareTo(other.Value);
        }
    }
}
