using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Components.Graph;
using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.TestGUI
{
    public partial class TestFormGrid : Form
    {
        public TestFormGrid()
        {
            Application.App.TracePriority = Application.TracePriority.Priority1_ElementaryTimeDebug;
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority2_Lowest, "TestFormGrid", "Constructor", "Start"))
            {
                using (Application.App.Trace.Scope("TestFormGrid", "InitializeComponent", ""))
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

            using (var scope = Application.App.Trace.Scope("TestFormGrid", "InitGControl", "Start"))
            {
                this._Toolbar = new GToolBar();
                this._Toolbar.FillFunctionGlobals();
                this._Toolbar.ToolbarSizeChanged += new GPropertyChangedHandler<Services.ComponentSize>(_Toolbar_ToolbarSizeChanged);

                this.Rand = new Random((int)DateTime.Now.Ticks % 0x0FFFFFFF);

                this._Table1 = this._PrepareTableW("stroje", 48);
                this._Table2 = this._PrepareTableW("směny", 128);
                this._TableZ = this._PrepareTableZ("lidi", 18);
                this._TimeSynchronizer = new ValueTimeRangeSynchronizer();

                using (var scope2 = Application.App.Trace.Scope("TestFormGrid", "InitGControl", "CreateGrid"))
                {
                    this._GridW = new GGrid() { Bounds = new Rectangle(10, 10, 750, 550) };
                    this._GridW.AddTable(this._Table1);
                    this._GridW.AddTable(this._Table2);

                    // this._Table1.AddRow(new TestItem() { Klic = 60, DatumOd = DateTime.Now.Add(TimeSpan.FromMinutes(50)), DatumDo = DateTime.Now.Add(TimeSpan.FromMinutes(55)) });

                    Cell[] items = this._Table1.Rows[2].Cells;

                    this._SplitterWZ = new GSplitter() { SplitterVisibleWidth = 4, SplitterActiveOverlap = 2, Orientation = Orientation.Vertical, Value = 400, BoundsNonActive = new Int32NRange(0, 200) };
                    this._SplitterWZ.ValueChanged += new GPropertyChangedHandler<int>(_SplitterWZ_ValueChanged);
                    this._SplitterWZ.ValueChanging += new GPropertyChangedHandler<int>(_SplitterWZ_ValueChanging);
                    this._GridZ = new GGrid();
                    this._GridZ.AddTable(this._TableZ);
                    this._GridZ.SynchronizedTime = this._TimeSynchronizer;
                    this._GridW.SynchronizedTime = this._TimeSynchronizer;
                }

                using (var scope3 = Application.App.Trace.Scope("TestFormGrid", "InitGControl", "GControl.AddItem(Grid)"))
                {
                    this.GControl.AddItem(this._Toolbar);
                    this.GControl.AddItem(this._GridW);
                    this.GControl.AddItem(this._GridZ);
                    this.GControl.AddItem(this._SplitterWZ);
                }

                this.ControlsPosition();

                // this._Table1.AddRow(new TestItem() { Klic = 70, DatumOd = DateTime.Now.Add(TimeSpan.FromMinutes(60)), DatumDo = DateTime.Now.Add(TimeSpan.FromMinutes(80)) });
                Application.App.Trace.Info("TestFormGrid", "InitGControl", "AddRow done");

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
            /*
            using (Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "TestFormGrid", "LoadDataFromSource", "WorkerThread"))
            {

                Services.DataSourceGetDataRequest request = new Services.DataSourceGetDataRequest(null);

                Application.App.ProcessRequestOnbackground<Services.DataSourceGetDataRequest, Services.DataSourceResponse>(source.ProcessRequest, request, this._ProcessResponseData);
            }
            */
        }
        /*
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
        */
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
            using (Application.App.Trace.Scope("TestFormGrid", "_PrepareTable", "Start", "RowCount: " + rowCount.ToString()))
            {

                table.AllowRowSelectByClick = false;
                table.AllowRowReorder = false;

                table.AddColumns
                    (
                        new Column("recordid", columnContent: ColumnContentType.RecordId, recordClassNumber: 1188),
                        new Column("key", "Reference", "Reference záznamu v tomto sloupci", "0000000", 75),
                        new Column("date_from", "Datum OD", "Počáteční datum směny", "yyyy-MM-dd HH:mm", 80),
                        new Column("date_to", "Datum DO", "Koncové datum směny", "yyyy-MM-dd HH:mm:ss", 80),
                        new Column("graph1", "graf", "Graf vytížení", null, 180, columnContent: ColumnContentType.TimeGraphSynchronized, autoWidth: true, sortingEnabled: false, widthMininum: 160),
                        new Column("price1", "Cena jednotky", "Jednotková cena.\r\nJe zde jen pro informaci.", "### ##0.00", 80),
                        new Column("image", "Fotografie", "Zobrazení", null, 60, sortingEnabled: false)
                    );

                DateTime now = DateTime.Now.Date.AddHours(8);
                for (int r = 0; r < rowCount; r++)
                {
                    int klic = 10 * (r + 1);
                    DateTime datumOd = now.AddMinutes(15 * r);
                    DateTime datumDo = now.AddMinutes((15 * r) + 5);
                    GTimeGraph graph1 = this._PrepareGraphW(now);
                    double price = Math.Round((this.Rand.NextDouble() * 100000d), 2);
                    Image image = images[imgPointer];
                    int height = ((this.Rand.Next(0, 100) > 80) ? 65 : 25);

                    Row row = new Row(10001 + r, klic, datumOd, datumDo, graph1, price, image);

                    if (this.Rand.Next(0, 100) > 80)
                        row.Height = 75;
                    else
                        row.Height = 35;

                    table.AddRow(row);

                    Cell cim = row["image"];
                    cim.UseImageAsToolTip = true;

                    Cell cellKey = row["key"];
                    cellKey.VisualStyle = new VisualStyle();
                    cellKey.VisualStyle.Font = new FontInfo();
                    cellKey.VisualStyle.Font.RelativeSize = 120;
                    if ((r % 5) == 0)
                    {
                        cellKey.VisualStyle.Font.Bold = true;
                        cellKey.VisualStyle.BackColor = Color.LightYellow;
                    }


                    imgPointer = (++imgPointer) % imgCount;
                }
            }
            return table;
        }
        private Table _PrepareTableZ(string name, int rowCount)
        {
            Table table = new Table(name);
            using (Application.App.Trace.Scope("TestFormGrid", "_PrepareTable", "Start", "RowCount: " + rowCount.ToString()))
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

                table.AllowRowResize = false;

                table.AddColumns
                    (
                        new Column("id", columnContent: ColumnContentType.RecordId, recordClassNumber: 1364),
                        new Column("profesion_id", columnContent: ColumnContentType.RelationRecordId, recordClassNumber: 1190),
                        new Column("photo", "Fotografie", sortingEnabled: false, width: 45, widthMininum: 10, widthMaximum: 60),
                        new Column("nazev", "Jméno", "Jméno zaměstnance", width: 200, widthMininum: 50, widthMaximum: 300),
                        new Column("profesion", "Profese", "Hlavní profese zaměstnance", columnContent: ColumnContentType.RelationRecordData, width: 150),
                        new Column("gender", "Rod", sortingEnabled: false, width: 35, allowColumnResize: false, widthMininum: 35, widthMaximum: 35)
                    );

                table.Columns["profesion"].ColumnProperties.RelatedRecordColumnName = "profesion_id";

                Image[] images = _LoadImages();

                DateTime now = DateTime.Now.Date.AddHours(8);
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

                    Row row = new Row(20001 + r, 50001 + r, image, tc, prof, mf);

                    Cell imageCell = row[1];
                    imageCell.ToolTip = nazev;
                    imageCell.UseImageAsToolTip = true;

                    Cell nameCell = row[2];
                    nameCell.ToolTip = nazev;
                    nameCell.ToolTipImage = image;

                    row.BackgroundValue = this._PrepareGraphZ(now, false, 4);

                    table.AddRow(row);
                }

                table.GraphParameters.TimeAxisMode = TimeGraphTimeAxisMode.LogarithmicScale;
                table.GraphParameters.OneLineHeight = 18;
                table.GraphParameters.TotalHeightRange = new Int32NRange(35, 480);
                table.GraphParameters.TimeAxisMode = TimeGraphTimeAxisMode.LogarithmicScale;
                table.GraphParameters.LogarithmicRatio = 0.70f;
                table.GraphParameters.LogarithmicGraphDrawOuterShadow = 0.30f;

                table.VisualStyle.BorderLines = BorderLinesType.HorizontalSolid;


            }
            return table;
        }
        private Image[] _LoadImages()
        {
            List<Image> images = new List<Image>();
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.DocumentSave);
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.EditCopy);
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.EditCut);
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.EditPaste);
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.EditUndo);
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.EditRedo);
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.GoTop);
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.GoUp);
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.GoDown);
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.GoBottom);
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.GoHome);
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.GoLeft);
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.GoRight);
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.GoEnd);
            images.Add(Asol.Tools.WorkScheduler.Components.IconStandard.Refresh);
            return images.ToArray();
        }
        private GTimeGraph _PrepareGraphW(DateTime now)
        {
            GTimeGraph graph = new GTimeGraph();

            graph.GraphParameters = TimeGraphProperties.Default;
            graph.GraphParameters.OneLineHeight = 18;
            graph.GraphParameters.TotalHeightRange = new Int32NRange(35, 480);

            DateTime begin, end;
            TestGraphItem item;

            // Layer -1 = time frame:
            int workLayer = -1;
            DateTime workBegin = now.Date.AddDays(-1d);
            DateTime workEnd = workBegin.AddDays(62d);
            while (workBegin < workEnd)
            {
                begin = workBegin.AddHours(6d);
                end = workBegin.AddHours(14d);

                item = new TestGraphItem();
                item.Layer = workLayer;
                item.Level = 0;
                item.GroupId = item.ItemId;
                item.Time = new TimeRange(begin, end);
                item.Height = 5f;
                item.ToolTip = "Ranní směna";
                item.BackColor = Color.FromArgb(240, 240, 255);
                item.LineColor = Color.Green;
                graph.AddGraphItem(item);

                item = new TestGraphItem();
                item.Layer = workLayer;
                item.Level = -1;
                item.GroupId = item.ItemId;
                item.Time = new TimeRange(begin, end);
                item.Height = 0.40f;
                item.ToolTip = "Ranní směna";
                item.BackColor = Color.FromArgb(240, 240, 255);
                item.LineColor = Color.Green;
                graph.AddGraphItem(item);



                begin = workBegin.AddHours(14d);
                end = workBegin.AddHours(22d);

                item = new TestGraphItem();
                item.Layer = workLayer;
                item.Level = 0;
                item.GroupId = item.ItemId;
                item.Time = new TimeRange(begin, end);
                item.Height = 5f;
                item.ToolTip = "Odpolední směna";
                item.BackColor = Color.FromArgb(240, 255, 240);
                item.LineColor = Color.Blue;
                graph.AddGraphItem(item);

                item = new TestGraphItem();
                item.Layer = workLayer;
                item.Level = -1;
                item.GroupId = item.ItemId;
                item.Time = new TimeRange(begin, end);
                item.Height = 0.40f;
                item.ToolTip = "Odpolední směna";
                item.BackColor = Color.FromArgb(240, 255, 240);
                item.LineColor = Color.Blue;
                graph.AddGraphItem(item);

                for (int t = 0; t < 7; t++)
                {
                    workBegin = workBegin.AddDays(1d);
                    if (workBegin.DayOfWeek == DayOfWeek.Saturday || workBegin.DayOfWeek == DayOfWeek.Saturday) continue;
                    break;
                }
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

                    item = new TestGraphItem();
                    item.Layer = layer;
                    item.Level = level;
                    item.Order = order;
                    item.GroupId = groupId;
                    item.Time = new TimeRange(begin, end);
                    item.Height = height;
                    item.ToolTip = tooltip;
                    item.BackColor = backColor;
                    item.LineColor = Color.Black;

                    graph.AddGraphItem(item);

                    start = end + TimeSpan.FromMinutes(15 * Rand.Next(0, 1 * 24 * 4));
                }
            }

            return graph;
        }
        private GTimeGraph _PrepareGraphZ(DateTime now, bool withShift, int taskCount)
        {
            GTimeGraph graph = new GTimeGraph();

            /*
            graph.GraphParameters = TimeGraphParameters.Default;
            graph.GraphParameters.OneLineHeight = 18;
            graph.GraphParameters.TotalHeightRange = new Int32NRange(35, 480);
            graph.GraphParameters.TimeAxisMode = TimeGraphTimeAxisMode.LogarithmicScale;
            graph.GraphParameters.LogarithmicRatio = 0.50f;
            */

            DateTime begin, end;
            TestGraphItem item;

            // Směny:
            int workLayer = 0;
            DateTime workBegin = now.Date.AddDays(-1d);
            DateTime workEnd = workBegin.AddDays(62d);
            if (withShift)
            {
                while (workBegin < workEnd)
                {
                    begin = workBegin.AddHours(6d);
                    end = workBegin.AddHours(14d);

                    item = new TestGraphItem();
                    item.Layer = workLayer;
                    item.Level = 0;
                    item.GroupId = item.ItemId;
                    item.Time = new TimeRange(begin, end);
                    item.Height = 1f;
                    item.ToolTip = "Ranní směna";
                    item.BackColor = Color.FromArgb(192, 255, 192);
                    item.LineColor = Color.FromArgb(128, 160, 128);
                    graph.AddGraphItem(item);

                    for (int t = 0; t < 7; t++)
                    {
                        workBegin = workBegin.AddDays(1d);
                        if (workBegin.DayOfWeek == DayOfWeek.Saturday || workBegin.DayOfWeek == DayOfWeek.Saturday) continue;
                        break;
                    }
                }
            }

            // Nějaký ten pracovní úkol:
            DateTime start = now.AddMinutes(15 * Rand.Next(0, 14 * 24 * 4));
            for (int t = 0; t < taskCount; t++)
            {
                begin = start.AddMinutes(15 * Rand.Next(0, 14 * 24 * 4));
                end = begin.AddMinutes(15 * Rand.Next(16, 5 * 24 * 4));

                item = new TestGraphItem();
                item.Layer = 1;
                item.Level = 0;
                item.Order = 0;
                item.GroupId = t;
                item.Time = new TimeRange(begin, end);
                item.Height = 0.5f;
                item.ToolTip = "Přiřazen k operaci " + (10 * (t + 1)).ToString();
                item.BackColor = Color.FromArgb(230, 216, 255);
                item.LineColor = Color.FromArgb(180, 160, 192);

                graph.AddGraphItem(item);

                start = end.AddMinutes(15 * Rand.Next(4, 1 * 24 * 4));
            }

            return graph;
        }
        private Random Rand;
        private Table _Table1;
        private Table _Table2;
        private Table _TableZ;
        private GToolBar _Toolbar;
        private GGrid _GridW;
        private GGrid _GridZ;
        private GSplitter _SplitterWZ;
        private ValueTimeRangeSynchronizer _TimeSynchronizer;
        private void TestFormGrid_SizeChanged(object sender, EventArgs e)
        {
            this.ControlsPosition();
        }
        protected void ControlsPosition()
        {
            using (Application.App.Trace.Scope("TestFormGrid", "ControlsPosition", "Start"))
            {
                Size size = this.GControl.ClientSize;
                int y = 0;
                if (this._Toolbar != null)
                {
                    this._Toolbar.Bounds = new Rectangle(0, 0, size.Width, 110);
                    y = this._Toolbar.Bounds.Bottom + 1;
                }

                y += 3;
                int h = size.Height - y - 0;
                if (this._SplitterWZ != null)
                {
                    int split = this._SplitterWZ.Value;
                    if (this._GridW != null)
                    {
                        this._GridW.Bounds = new Rectangle(0, y, split - 2, h);
                        this._GridW.Refresh();
                    }
                    if (this._GridZ != null)
                    {
                        this._GridZ.Bounds = new Rectangle(split + 2, y, size.Width - 0 - split - 2, h);
                        this._GridZ.Refresh();
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
    #region class TestGraphItem : Třída reprezentující jednu položku grafů. Jde o jednoduchou a funkční implementaci rozhraní ITimeGraphItem.
    /// <summary>
    /// TestGraphItem : Třída reprezentující jednu položku grafů. Jde o jednoduchou a funkční implementaci rozhraní ITimeGraphItem.
    /// </summary>
    public class TestGraphItem : ITimeGraphItem
    {
        #region Public members
        public TestGraphItem()
        {
            this.ItemId = Application.App.GetNextId(typeof(ITimeGraphItem));
        }
        private ITimeInteractiveGraph _OwnerGraph;
        /// <summary>
        /// Jednoznačný identifikátor prvku
        /// </summary>
        public Int32 ItemId { get; private set; }
        /// <summary>
        /// GroupId: číslo skupiny. Prvky se shodným GroupId budou vykreslovány do společného "rámce", 
        /// a pokud mezi jednotlivými prvky <see cref="ITimeGraphItem"/> se shodným <see cref="GroupId"/> bude na ose X nějaké volné místo,
        /// nebude mezi nimi vykreslován žádný "cizí" prvek.
        /// </summary>
        public Int32 GroupId { get; set; }
        /// <summary>
        /// Časový interval tohoto prvku
        /// </summary>
        public virtual TimeRange Time { get; set; }
        /// <summary>
        /// Layer: Vizuální vrstva. Prvky z různých vrstev jsou kresleny "přes sebe" = mohou se překrývat.
        /// Nižší hodnota je kreslena dříve.
        /// Například: záporná hodnota Layer reprezentuje "podklad" který se needituje.
        /// </summary>
        public Int32 Layer { get; set; }
        /// <summary>
        /// Level: Vizuální hladina. Prvky v jedné hladině jsou kresleny do společného vodorovného pásu, 
        /// další prvky ve vyšší hladině jsou všechny zase vykresleny ve svém odděleném pásu (nad tímto nižším pásem). 
        /// Nespadnou do prvků nižšího pásu i když by v něm bylo volné místo.
        /// </summary>
        public Int32 Level { get; set; }
        /// <summary>
        /// Order: pořadí prvku při výpočtech souřadnic Y před vykreslováním. 
        /// Prvky se stejným Order budou tříděny vzestupně podle data počátku <see cref="Time"/>.Begin.
        /// </summary>
        public Int32 Order { get; set; }
        /// <summary>
        /// Relativní výška tohoto prvku. Standardní hodnota = 1.0F. Fyzická výška (v pixelech) jednoho prvku je dána součinem 
        /// <see cref="Height"/> * <see cref="GTimeGraph.GraphParameters"/>: <see cref="TimeGraphProperties.OneLineHeight"/>
        /// Prvky s výškou 0 a menší nebudou vykresleny.
        /// </summary>
        public float Height { get; set; }
        public string Text { get; set; }
        public string ToolTip { get; set; }
        /// <summary>
        /// Barva pozadí prvku.
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Barva spojovací linky mezi prvky jedné skupiny.
        /// Default = null = kreslí se barvou <see cref="BackColor"/>, která je morfována na 50% do barvy DimGray a zprůhledněna na 50%.
        /// </summary>
        public Color? LineColor { get; set; }
        /// <summary>
        /// Režim editovatelnosti položky grafu
        /// </summary>
        public GraphItemBehaviorMode BehaviorMode { get; set; }
        /// <summary>
        /// Vizuální prvek, který v sobě zahrnuje jak podporu pro vykreslování, tak podporu interaktivity.
        /// A přitom to nevyžaduje od třídy, která fyzicky implementuje <see cref="ITimeGraphItem"/>.
        /// Aplikační kód (implementační objekt <see cref="ITimeGraphItem"/> se o tuto property nemusí starat, řídící mechanismus sem vloží v případě potřeby new instanci.
        /// Implementátor pouze poskytuje úložiště pro tuto instanci.
        /// </summary>
        public WorkScheduler.Components.Graph.GTimeGraphItem GControl { get; set; }
        #endregion
        #region explicit ITimeGraphItem members
        ITimeInteractiveGraph ITimeGraphItem.OwnerGraph { get { return this._OwnerGraph; } set { this._OwnerGraph = value; } }
        int ITimeGraphItem.ItemId { get { return this.ItemId; } }
        int ITimeGraphItem.GroupId { get { return this.GroupId; } }
        TimeRange ITimeGraphItem.Time { get { return this.Time; } set { this.Time = value; } }
        int ITimeGraphItem.Layer { get { return this.Layer; } }
        int ITimeGraphItem.Level { get { return this.Level; } }
        int ITimeGraphItem.Order { get { return this.Order; } }
        float ITimeGraphItem.Height { get { return this.Height; } }
        string ITimeGraphItem.Text { get { return this.Text; } }
        string ITimeGraphItem.ToolTip { get { return this.ToolTip; } }
        Color? ITimeGraphItem.BackColor { get { return this.BackColor; } }
        Color? ITimeGraphItem.LineColor { get { return this.LineColor; } }
        System.Drawing.Drawing2D.HatchStyle? ITimeGraphItem.BackStyle { get { return null; } }
        float? ITimeGraphItem.RatioBegin { get { return null; } }
        float? ITimeGraphItem.RatioEnd { get { return null; } }
        Color? ITimeGraphItem.RatioBeginBackColor { get { return null; } }
        Color? ITimeGraphItem.RatioEndBackColor { get { return null; } }
        Color? ITimeGraphItem.RatioLineColor { get { return null; } }
        int? ITimeGraphItem.RatioLineWidth { get { return null; } }
        GraphItemBehaviorMode ITimeGraphItem.BehaviorMode { get { return this.BehaviorMode; } }
        WorkScheduler.Components.Graph.GTimeGraphItem ITimeGraphItem.GControl { get { return this.GControl; } set { this.GControl = value; } }
        void ITimeGraphItem.Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode) { this.GControl.DrawItem(e, boundsAbsolute, drawMode); }
        #endregion
    }
    #endregion
}
