using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WF = System.Windows.Forms;

using DXB = DevExpress.XtraBars;
using DXE = DevExpress.XtraEditors;
using DC = DevExpress.XtraCharts;
using DevExpress.XtraBars.Ribbon;

namespace Djs.Tools.CovidGraphs
{
    public partial class MainForm : DXB.Ribbon.RibbonForm
    {
        #region Tvorba formuláře, proměnné pro controly
        public MainForm()
        {
            Data.App.DefaultApplicationCode = "BestInCovid";         // Ještě před inicializací singletonu
            InitializeComponent();
            InitDevExpressComponents();
        }
        #region WinForm designer
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Text = "Best in Covid";
        }
        #endregion
        #endregion
        private void InitDevExpressComponents()
        {
            InitForm();
            InitFrames();
            InitRibbons();
            InitList();
            InitGraph();
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.LoadDatabase();
            Data.App.Config.SaveEnabled = true;
        }
        private void InitForm()
        {
            this.WindowState = FormWindowState.Maximized;
        }
        private void InitRibbons()
        {
            this.Ribbon = new DXB.Ribbon.RibbonControl();
            this.Ribbon.Items.Clear();

            this.StatusBar = new DXB.Ribbon.RibbonStatusBar();
            this.StatusBar.Ribbon = this.Ribbon;

            RibbonFillItems();
            StatusFillItems();

            this.Controls.Add(this.Ribbon);
            this.Controls.Add(this.StatusBar);

        }
        private void InitFrames()
        {
            int splitterPosition = Data.App.Config.SplitterPosition;
            _MainSplitContainer = new DXE.SplitContainerControl()
            {
                FixedPanel = DXE.SplitFixedPanel.Panel1,
                Horizontal = true,
                PanelVisibility = DXE.SplitPanelVisibility.Both,
                SplitterPosition = splitterPosition,
                Dock = DockStyle.Fill
            };
            _MainSplitContainer.Panel1.BackColor = Color.LightBlue;
            _MainSplitContainer.Panel2.BackColor = Color.LightCoral;
            _MainSplitContainer.ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True;
            _MainSplitContainer.SplitterPositionChanged += _SplitterPositionChanged;

            this.Controls.Add(_MainSplitContainer);
        }

        private void _SplitterPositionChanged(object sender, EventArgs e)
        {
            Data.App.Config.SplitterPosition = _MainSplitContainer.SplitterPosition;
        }

        private void InitList()
        {
            _GraphListBox = new DXE.ListBoxControl()
            {
                MultiColumn = false,
                SelectionMode = SelectionMode.One,
                Dock = DockStyle.Fill
                
            };
            _GraphListBox.Appearance.FontSizeDelta = 1;

            /*
            DXE.TableLayout.ItemTemplateBase template = new DXE.TableLayout.ItemTemplateBase();  // DXE.TableLayout.ItemTemplateBase();

            template.Name = "TemplateD";

            template.Rows.Add(new DXE.TableLayout.TableRowDefinition() { AutoHeight = false, Length = new DXE.TableLayout.TableDefinitionLength() { Type = DXE.TableLayout.TableDefinitionLengthType.Pixel, Value = 18 } });
            template.Rows.Add(new DXE.TableLayout.TableRowDefinition() { AutoHeight = true, Length = new DXE.TableLayout.TableDefinitionLength() { Type = DXE.TableLayout.TableDefinitionLengthType.Pixel, Value = 32 } });
            template.Columns.Add(new DXE.TableLayout.TableColumnDefinition() { Length = new DXE.TableLayout.TableDefinitionLength() { Type = DXE.TableLayout.TableDefinitionLengthType.Star, Value = 1000 } });
            template.Columns.Add(new DXE.TableLayout.TableColumnDefinition());

            DXE.TableLayout.TemplatedItemElement e0 = new DXE.TableLayout.TemplatedItemElement() { RowIndex = 0, ColumnIndex = 0, FieldName = "Title", Height = 18, TextLocation = new Point(8, 2), StretchHorizontal = true };
            e0.StretchHorizontal = true;
            e0.StretchVertical = true;
            e0.Appearance.Normal.FontSizeDelta = 1;
            e0.Appearance.Normal.FontStyleDelta = FontStyle.Bold;

            DXE.TableLayout.TemplatedItemElement e1 = new DXE.TableLayout.TemplatedItemElement() { RowIndex = 1, ColumnIndex = 0, FieldName = "Description", Height = 32, TextLocation = new Point(8,2), StretchHorizontal = true };
            e1.Appearance.Normal.FontStyleDelta = FontStyle.Italic;


            template.Elements.Add(e0);
            template.Elements.Add(e1);

            _GraphListBox.Templates.Add(template);
            _GraphListBox.ItemAutoHeight = true;
            _GraphListBox.ItemHeight = 50;
            */

            LoadGraphs();

            _GraphListBox.DataSource = _Graphs;

            // _GraphListBox.Items.AddRange(_Graphs.ToArray());
            _GraphListBox.SelectedIndex = 0;
            _GraphListBox.SelectedIndexChanged += _GraphListBox_SelectedIndexChanged;
            _MainSplitContainer.Panel1.Controls.Add(_GraphListBox);
        }

        private void _GraphListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowCurrentGraph();
        }

        private void InitGraph()
        {
            _ChartControl = new DevExpress.XtraCharts.ChartControl()
            {
                Dock = DockStyle.Fill
            };
            _MainSplitContainer.Panel2.Controls.Add(_ChartControl);
        }
        DXB.BarStaticItem _StatusInfoTextItem;
        DXE.Repository.RepositoryItemProgressBar _StatusProgressBar;
        DXB.BarEditItem _StatusProgressEdit;
        DXE.SplitContainerControl _MainSplitContainer;
        DXE.ListBoxControl _GraphListBox;
        DevExpress.XtraCharts.ChartControl _ChartControl;
        #endregion
        #region Ribbon a StatusBar
        private void RibbonFillItems()
        {
            this.Ribbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;

            DXB.Ribbon.RibbonPage page0 = new DXB.Ribbon.RibbonPage("GRAF");
            this.Ribbon.Pages.Add(page0);
            DXB.Ribbon.RibbonPageGroup group00 = new DXB.Ribbon.RibbonPageGroup("SPRÁVA GRAFŮ");
            page0.Groups.Add(group00);

            DXB.Ribbon.RibbonPageGroup group01 = new DXB.Ribbon.RibbonPageGroup("SPRÁVA DAT");
            page0.Groups.Add(group01);

            /*
            var provider = DevExpress.Utils.DxImageAssemblyUtil.ImageProvider;

            var baseImages = provider.GetBaseImages();
            var baseImage0 = provider.GetImage(baseImages[0]);

            var file0 = provider.GetFile(baseImages[0]);


            var svgImages = provider.GetSvgImages().ToArray();
            var svgImage0 = provider.GetSvgImage(svgImages[0]);
            var svgImage1 = provider.GetSvgImage(svgImages[1]);
            var svgImage2 = provider.GetSvgImage(svgImages[2]);

            */


            /*
            var allImages = provider.GetAllImages();


            var allImage0 = allImages[0];
            var allImage0Uri = allImage0.MakeUri();
            var allImage0Img = provider.GetImageByPath(allImage0Uri);

            var files = provider.GetFiles("CostAnalysis_16x16.png").ToArray();
            var files0Img = provider.GetImageByPath(files[0]);

            var name = allImages[25].MakeUri();
            */

            RibbonAddButton(group00, "Přidej", "Přidá nový graf", Properties.Resources.document_new_6_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickNewGraph);
            RibbonAddButton(group00, "Uprav", "Umožní změnit datové zdroje grafu: výběr obcí, výběr dat", Properties.Resources.document_properties_2_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickEditDataGraph);
            RibbonAddButton(group00, "Vzhled grafu", "Otevře editor vzhledu grafu", Properties.Resources.document_page_setup_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickEditLayoutGraph);
            RibbonAddButton(group00, "Smaž", "Smaže aktuální graf", Properties.Resources.document_close_4_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickDeleteGraph);
            RibbonAddButton(group01, "Aktualizuj data", "Zajistí aktualizaci dat z internetu", Properties.Resources.download_3_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickWebUpdate);
            RibbonAddButton(group01, "Značky", "Značky jsou společné, jsou vkládány do všech grafů, označují význačné časové úseky", Properties.Resources.system_switch_user_2_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickEditStrip);
            RibbonAddButton(group01, "Ulož", "Uloží všechna aktuální data do jednoho datového souboru DataPack, ten lze např odeslat mailem.", Properties.Resources.document_save_as_6_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickSaveDataPack);
        }
        /// <summary>
        /// Do dané grupy přidá nový button podle definice
        /// </summary>
        /// <param name="group"></param>
        /// <param name="text"></param>
        /// <param name="tooltip"></param>
        /// <param name="image"></param>
        /// <param name="style"></param>
        /// <param name="itemClick"></param>
        private void RibbonAddButton(RibbonPageGroup group, string text, string tooltip, Bitmap image, RibbonItemStyles style, DXB.ItemClickEventHandler itemClick)
        {
            DXB.BarButtonItem button = new DXB.BarButtonItem(this.Ribbon.Manager, text) { RibbonStyle = style };
            button.ImageOptions.Image = image;
            if (tooltip != null)
            {
                button.SuperTip = new DevExpress.Utils.SuperToolTip();
                button.SuperTip.Items.AddTitle(text);
                button.SuperTip.Items.Add(tooltip);
            }
            group.ItemLinks.Add(button);
            if (itemClick != null)
                button.ItemClick += itemClick;
        }
        private void RibbonClickNewGraph(object sender, DXB.ItemClickEventArgs e)
        {
        }
        private void RibbonClickEditDataGraph(object sender, DXB.ItemClickEventArgs e)
        {
            Data.App.TryRun(TryShowGraphForm);
        }
        private void RibbonClickEditLayoutGraph(object sender, DXB.ItemClickEventArgs e)
        {
            Data.App.TryRun(TryEditChartLayout);
        }
        private void RibbonClickDeleteGraph(object sender, DXB.ItemClickEventArgs e)
        {
        }
        private void RibbonClickWebUpdate(object sender, DXB.ItemClickEventArgs e)
        {
            Data.App.TryRun(TryUpdateDatabaseWeb);
        }
        private void RibbonClickEditStrip(object sender, DXB.ItemClickEventArgs e)
        {
        }
        private void RibbonClickSaveDataPack(object sender, DXB.ItemClickEventArgs e)
        {
            Data.App.TryRun(TrySaveDatabasePack);
        }
        private void StatusFillItems()
        {
            _StatusInfoTextItem = new DXB.BarStaticItem() { Caption = "Stavový řádek" };
            this.StatusBar.ItemLinks.Add(_StatusInfoTextItem);

            _StatusProgressBar = new DXE.Repository.RepositoryItemProgressBar();
            _StatusProgressBar.AutoHeight = true;
            _StatusProgressBar.BorderStyle = DXE.Controls.BorderStyles.Office2003;
            _StatusProgressBar.Minimum = 0;
            _StatusProgressBar.Maximum = 200;
            _StatusProgressBar.BestFitWidth = 200;
            _StatusProgressBar.FlowAnimationEnabled = true;
            _StatusProgressBar.FlowAnimationDuration = 2500;
            _StatusProgressBar.FlowAnimationDelay = 1000;
            _StatusProgressBar.ProgressViewStyle = DXE.Controls.ProgressViewStyle.Solid;
            _StatusProgressBar.ProgressKind = DXE.Controls.ProgressKind.Horizontal;
            _StatusProgressBar.ProgressPadding = new Padding(3);
            this.Ribbon.RepositoryItems.Add(_StatusProgressBar);

            _StatusProgressEdit = new DXB.BarEditItem();
            this.Ribbon.Items.Add(_StatusProgressEdit);
            _StatusProgressEdit.Edit = _StatusProgressBar;
            _StatusProgressEdit.Alignment = DXB.BarItemLinkAlignment.Right;
            _StatusProgressEdit.Size = new Size(200, 18);
            _StatusProgressEdit.EditHeight = 12;
            _StatusProgressEdit.EditValue = 0;
            _StatusProgressEdit.EditWidth = 200;
            _StatusProgressEdit.Visibility = DXB.BarItemVisibility.Never;
            this.StatusBar.ItemLinks.Add(_StatusProgressEdit);
        }
        #endregion
        #region Property pro přístup k datům controlů, invokace
        /// <summary>
        /// Text ve statusbaru - velký informativní
        /// </summary>
        protected string StatusInfoText
        {
            get
            {
                return _StatusInfoTextItem.Caption;
            }
            set
            {
                if (this.InvokeRequired)
                    this.BeginInvoke(new Action<string>((v) => this.StatusInfoText = v), value);
                else
                {
                    _StatusInfoTextItem.Caption = value;
                }
            }
        }
        /// <summary>
        /// Hodnota zobrazená v progresu ve statusbaru, v rozmezí 0 - 1.
        /// Setování hodnoty zajistí <see cref="StatusProgressVisible"/> = true; pokud bylo false.
        /// </summary>
        protected float StatusProgressValue
        {
            get
            {
                float min = _StatusProgressBar.Minimum;
                float max = _StatusProgressBar.Maximum;
                float val = (int)_StatusProgressEdit.EditValue;
                return (val - min) / (max - min);
            }
            set
            {
                if (this.InvokeRequired)
                    this.BeginInvoke(new Action<float>((v) => this.StatusProgressValue = v), value);
                else
                {
                    float min = _StatusProgressBar.Minimum;
                    float max = _StatusProgressBar.Maximum;
                    float val = min + (value * (max - min));
                    _StatusProgressEdit.EditValue = (int)val;
                    if (!StatusProgressVisible) StatusProgressVisible = true;
                }
            }
        }
        /// <summary>
        /// Viditelnost progresu ve statusbaru
        /// </summary>
        protected bool StatusProgressVisible
        {
            get
            {
                return (_StatusProgressEdit.Visibility == DXB.BarItemVisibility.Always);
            }
            set
            {
                if (this.InvokeRequired)
                    this.BeginInvoke(new Action<bool>((v) => this.StatusProgressVisible = v), value);
                else
                {
                    _StatusProgressEdit.Visibility = (value ? DXB.BarItemVisibility.Always : DXB.BarItemVisibility.Never);
                    _StatusBarRefresh();
                }
            }
        }
        /// <summary>
        /// Refresh statusbaru po jeho aktualizaci, je nutno volat po změně viditelnosti prvku, nikoli po změně obsahu
        /// </summary>
        protected void _StatusBarRefresh()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(_StatusBarRefresh));
            else
            {
                this.StatusBar.Refresh();
                _StatusInfoTextItem.Refresh();
                _StatusProgressEdit.Refresh();
            }
        }
        #endregion
        #region Databáze
        /// <summary>
        /// Vyvolá prvotní načtení databáze
        /// </summary>
        private void LoadDatabase()
        {
            StatusProgressValue = 0f;
            StatusInfoText = "Načítáme data...";

            this._Database = new Data.Database();
            this._Database.LoadStandardDataAsync(DatabaseShowProgress);
        }
        /// <summary>
        /// Zkusí aktualizovat databázi
        /// </summary>
        private void TryUpdateDatabaseWeb()
        {
            if (this._Database == null)
                throw new InvalidOperationException("Nelze aktualizovat databázi, dosud není vytvořena. Počkejte několik sekund...");
            if (!this._Database.IsReady)
                throw new InvalidOperationException("Nelze aktualizovat databázi, dosud není připravena. Počkejte několik sekund...");

            this._Database.WebUpdateAsync(DatabaseShowProgress);
        }
        /// <summary>
        /// Zkusí uložit databázi do DataPacku
        /// </summary>
        private void TrySaveDatabasePack()
        {
            if (this._Database == null)
                throw new InvalidOperationException("Nelze uložit databázi, dosud není vytvořena. Počkejte několik sekund...");
            if (!this._Database.IsReady)
                throw new InvalidOperationException("Nelze uložit databázi, dosud není připravena. Počkejte několik sekund...");

            this._Database.SaveDataPackData(DatabaseShowProgress);
        }
        /// <summary>
        /// Progress práce s databází
        /// </summary>
        /// <param name="args"></param>
        private void DatabaseShowProgress(Data.ProgressArgs args)
        {
            string text = "";
            if (!args.IsDone)
            {
                string ratio = Math.Round(100m * args.Ratio, 1).ToString("###0.0").Trim();
                int mb = (int)(args.DataLength / 1048576);
                string length = mb.ToString("### ##0").Trim();
                text = $"{args.Description}, zpracováno {ratio}% z : {length} MB";
                StatusProgressValue = (float)args.Ratio;
            }
            else
            {
                string count = args.RecordCount.ToString("### ### ##0").Trim();
                string time = args.ProcessTime.TotalSeconds.ToString("##0.000");
                text = $"{args.Description} {count} záznamů, za : {time} sekund";
                StatusProgressVisible = false;
            }
            StatusInfoText = text;

            if (args.IsDone && this._Database != null && args.ProcessState == Data.ProcessFileState.Loaded && (args.ContentType == Data.FileContentType.Data || args.ContentType == Data.FileContentType.DataPack))
            {
                ShowCurrentGraph();
            }
        }
        /// <summary>
        /// Úložiště databáze
        /// </summary>
        private Data.Database _Database;
        #endregion
        #region Správce grafů
        /// <summary>
        /// Připraví a naplní pole pro definované grafy <see cref="_Graphs"/>
        /// </summary>
        private void LoadGraphs()
        {
            var database = _Database;


            var graphs = Data.GraphInfo.LoadFromPath();


            _Graphs = new List<Data.GraphInfo>();

            Data.GraphInfo graph;

            graph = new Data.GraphInfo() { Title = "ČR: Denní přírůstky poslední měsíc+", Description = "Počty nově nakažených za den - přesně, a průměrně", TimeRangeLastMonths = 1, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ", Title = "Česká republika, denní přírůstky", ValueType = Data.DataValueType.NewCount, LineThickness = 1, LineColor = Color.DarkViolet, LineDashStyle = Data.LineDashStyleType.Dot });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ", Title = "Česká republika, denní přírůstky průměrně", ValueType = Data.DataValueType.NewCountAvg, LineThickness = 3, LineColor = Color.DarkViolet, LineDashStyle = Data.LineDashStyleType.Solid }); 
            _Graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "CR+PC+HK obce, relativně", Description = "Stav ve trojměstí za celou dobu", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0531.5304.53043.571164", Title = "Chrudim, aktuálně", ValueType = Data.DataValueType.CurrentCountRelativeAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0532.5309.53092.555134", Title = "Pardubice, aktuálně", ValueType = Data.DataValueType.CurrentCountRelativeAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ052.CZ0521.5205.52051.569810", Title = "Hradec, aktuálně", ValueType = Data.DataValueType.CurrentCountRelativeAvg });
            _Graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "Chrudim", Description = "Stav v Chrudimi za poslední 4 měsíce", TimeRangeLastMonths = 4, ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0531.5304.53043.571164", Title = "Chrudim, aktuálně", ValueType = Data.DataValueType.CurrentCount });
            _Graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "Pardubice", Description = "Stav v Pardubicích za posledních 7 měsíců", TimeRangeLastMonths = 7, ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0532.5309.53092.555134", Title = "Pardubice, aktuálně", ValueType = Data.DataValueType.CurrentCount });
            _Graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "Krucemburk", Description = "Stav v Krucborku a Ždírci za celou dobu", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ063.CZ0631.6304.63041.568945", Title = "Krucbork, aktuálně", ValueType = Data.DataValueType.CurrentCount });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ063.CZ0631.6304.63041.569780", Title = "Ždírec, aktuálně", ValueType = Data.DataValueType.CurrentCount });
            _Graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "CR+PC+HK obce", Description = "Stav ve trojměstí za celou dobu", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0531.5304.53043.571164", Title = "Chrudim, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0532.5309.53092.555134", Title = "Pardubice, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ052.CZ0521.5205.52051.569810", Title = "Hradec, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            _Graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "CR+PC+HK obce poslední 3 měsíce", Description = "Stav ve trojměstí za poslední 3 měsíce", TimeRangeLastMonths = 3, ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0531.5304.53043.571164", Title = "Chrudim, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0532.5309.53092.555134", Title = "Pardubice, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ052.CZ0521.5205.52051.569810", Title = "Hradec, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            _Graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "CR+PC+HK okresy", Description = "Stav okresů za celou dobu", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0531", Title = "Chrudim, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0532", Title = "Pardubice, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ052.CZ0521", Title = "Hradec, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            _Graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "CR+PC+HK obce, číslo R", Description = "Stav ve trojměstí za celou dobu", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0531.5304.53043.571164", Title = "Chrudim, aktuálně", ValueType = Data.DataValueType.RZeroAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0532.5309.53092.555134", Title = "Pardubice, aktuálně", ValueType = Data.DataValueType.RZeroAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ052.CZ0521.5205.52051.569810", Title = "Hradec, aktuálně", ValueType = Data.DataValueType.RZeroAvg });
            _Graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "ČR + kraje PC+HK, přírůstky 7dní", Description = "Stav celkový za celou dobu", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ", Title = "ČR, aktuálně", ValueType = Data.DataValueType.NewCount7DaySumAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053", Title = "Kraj Pardubice", ValueType = Data.DataValueType.NewCount7DaySumAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ052", Title = "Kraj HK", ValueType = Data.DataValueType.NewCount7DaySumAvg });
            _Graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "ČR + kraje PC+HK, průměrný stav", Description = "Stav celkový za celou dobu", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ", Title = "ČR, aktuálně", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053", Title = "Kraj Pardubice", ValueType = Data.DataValueType.CurrentCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ052", Title = "Kraj HK", ValueType = Data.DataValueType.CurrentCountAvg });
            _Graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "ČR + kraje PC+HK, číslo R avg", Description = "Stav celkový za celou dobu", TimeRangeLastMonths = 3, ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ", Title = "ČR, aktuálně", ValueType = Data.DataValueType.RZeroAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053", Title = "Kraj Pardubice", ValueType = Data.DataValueType.RZeroAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ052", Title = "Kraj HK", ValueType = Data.DataValueType.RZeroAvg });
            _Graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "ČR + kraje PC+HK, číslo R raw", Description = "Stav celkový za celou dobu", TimeRangeLastMonths = 3, ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ", Title = "ČR, aktuálně", ValueType = Data.DataValueType.RZero });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053", Title = "Kraj Pardubice", ValueType = Data.DataValueType.RZero });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ052", Title = "Kraj HK", ValueType = Data.DataValueType.RZero });
            _Graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "CR+PC+HK Přírůstek/7 dní relativně", Description = "Počty nových případů za posledních 7 dní poměrně k počtu obyvatel", ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0531", Title = "Chrudim, aktuálně", ValueType = Data.DataValueType.NewCount7DaySumRelative });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0532", Title = "Pardubice, aktuálně", ValueType = Data.DataValueType.NewCount7DaySumRelative });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ052.CZ0521", Title = "Hradec, aktuálně", ValueType = Data.DataValueType.NewCount7DaySumRelative });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0531", Title = "Chrudim, číslo R", ValueType = Data.DataValueType.RZeroAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0532", Title = "Pardubice, číslo R", ValueType = Data.DataValueType.RZeroAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ052.CZ0521", Title = "Hradec, číslo R", ValueType = Data.DataValueType.RZeroAvg });
            _Graphs.Add(graph);

            graph = new Data.GraphInfo() { Title = "CR+PC+HK+RK+CH+HL Přírůstek průměr", Description = "Počty nových případů, zprůměrované", TimeRangeLastMonths = 3, ChartEnableTimeZoom = true, ChartAxisYRight = true };
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0531", Title = "Chrudim, aktuálně", ValueType = Data.DataValueType.NewCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0532", Title = "Pardubice, aktuálně", ValueType = Data.DataValueType.NewCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ052.CZ0521", Title = "Hradec, aktuálně", ValueType = Data.DataValueType.NewCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ052.CZ0524", Title = "okres Rychnov n/K, aktuálně", ValueType = Data.DataValueType.NewCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ052.CZ0524.5213.52132.576069", Title = "obec Rychnov n/K, aktuálně", ValueType = Data.DataValueType.NewCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ063.CZ0631.6304.63041.568759", Title = "obec Chotěboř", ValueType = Data.DataValueType.NewCountAvg });
            graph.AddSerie(new Data.GraphSerieInfo() { DataEntityCode = "CZ.CZ053.CZ0531.5302.53021.571393", Title = "obec Hlinsko", ValueType = Data.DataValueType.NewCountAvg });
            _Graphs.Add(graph);

        }
        private List<Data.GraphInfo> _Graphs;
        /// <summary>
        /// Zajistí zobrazení aktuálně vybraného grafu.
        /// Volá se po změně vybraného grafu a po dokončení načítání dat.
        /// </summary>
        private void ShowCurrentGraph()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(ShowCurrentGraph));
            else
            {
                if (this._Database == null) return;
                if (!(_GraphListBox.SelectedItem is Data.GraphInfo graph)) return;

                Data.App.TryRun(TryShowCurrentGraph);
            }
        }
        /// <summary>
        /// Zkusí zobrazit aktuálně vybraný graf
        /// </summary>
        private void TryShowCurrentGraph()
        {
            if (!(_GraphListBox.SelectedItem is Data.GraphInfo graph)) return;

            var database = _Database;
            if (database == null) return;
            if (!database.HasData) return;

            if (CurrentGraph != null && !Object.ReferenceEquals(CurrentGraph, graph))
            {   // Změna grafu z existujícího na nový: načtu aktuální Layout a uložím si jej do dat grafu, protože Layout může obsahovat změnu měřítka:
                CurrentGraph.ChartLayout = _GetLayout();
            }

            // Přepneme na nový layout:
            CurrentGraph = graph;
            CurrentGraphData = graph.LoadData(database);
            CurrentGraphDataSource = CurrentGraphData.DataTable;

            // Tato sekvence je důležitá, jinak dojde k chybám:
            _SetLayout("");

            _ChartControl.DataSource = CurrentGraphDataSource;
            string layout = graph.ChartLayout;
            if (String.IsNullOrEmpty(layout))
                layout = graph.CreateDefaultLayout(CurrentGraphData);

            _SetLayout(layout);


            //if (!_SettingsApplied)
            //{
            //    _SetLayout(_GetSettings());
            //    _SettingsApplied = true;
            //}

            var modifierKeys = Control.ModifierKeys;
            if (modifierKeys != Keys.None)
            {
                DC.Designer.ChartDesigner designer = new DC.Designer.ChartDesigner(_ChartControl)
                {
                    Caption = "Upravte graf...",
                    ShowActualData = true
                };
                var response = designer.ShowDialog(true);
                bool result = (response == WF.DialogResult.OK);

                if (result)
                {
                    string setting = _GetLayout();
                }
            }
        }

        private string _GetSettings()
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



        /// <summary>
        /// Načte a vrátí Layout z aktuálního grafu
        /// </summary>
        /// <returns></returns>
        private string _GetLayout()
        {
            string layout = null;
            if (_IsChartValid)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    _ChartControl.SaveToStream(ms);
                    layout = Encoding.UTF8.GetString(ms.GetBuffer());
                }
            }
            return layout;
        }
        /// <summary>
        /// Vloží daný string jako Layout do grafu. 
        /// Tato metoda nevolá žádné eventy. 
        /// Nemění ani Tools.Combo.SelectedItem ani jeho Items.
        /// </summary>
        /// <param name="layout"></param>
        private void _SetLayout(string layout)
        {
            if (_IsChartValid)
            {
                try
                {
                    byte[] buffer = (!String.IsNullOrEmpty(layout) ? Encoding.UTF8.GetBytes(layout) : new byte[0]);
                    if (buffer.Length > 0)
                    {
                        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(buffer))
                            _ChartControl.LoadFromStream(ms);     // Pozor, zahodí data !!!
                    }
                    else
                    {
                        _ChartControl.Series.Clear();
                        _ChartControl.Legends.Clear();
                        _ChartControl.Titles.Clear();
                    }
                }
                catch (DevExpress.XtraCharts.LayoutStreamException) { }
                finally
                {
                    _ChartControl.DataSource = CurrentGraphDataSource;       // Vrátíme data do grafu
                }
            }
        }
        /// <summary>
        /// Obsahuje true, pokud graf je platný (není null, má data a není Disposed)
        /// </summary>
        private bool _IsChartValid { get { return (CurrentGraphDataSource != null && _ChartControl != null && _ChartControl.DataSource != null && !_ChartControl.IsDisposed); } }
        /// <summary>
        /// Otevře Wizard pro editaci layoutu aktuálního grafu
        /// </summary>
        private void TryEditChartLayout()
        {
            if (CurrentGraph == null)
                throw new InvalidOperationException($"V tuto chvíli nelze editovat graf, dosud není načten nebo není definován.");

            string oldLayout = _GetLayout();

            DC.Designer.ChartDesigner designer = new DC.Designer.ChartDesigner(_ChartControl)
            {
                Caption = "Upravte graf...",
                ShowActualData = true
            };
            var response = designer.ShowDialog(true);

            // Wizard pracuje nad naším controlem, veškeré změny ve Wizardu provedené se ihned promítají do našeho grafu.
            // Pokud uživatel dal OK, chceme změny uložit i do příště,
            // pokud ale dal Cancel, pak změny chceme zahodit a vracíme se k původnímu layoutu:
            bool result = (response == WF.DialogResult.OK);
            if (result)
            {
                CurrentGraph.ChartLayout = _GetLayout();
                CurrentGraph.SaveToFile();
            }
            else
            {
                _SetLayout(oldLayout);
            }
        }
        /// <summary>
        /// Otevře formulář pro definici dat grafu
        /// </summary>
        private void TryShowGraphForm()
        {
            using (var graphForm = new GraphForm())
            {
                graphForm.ShowDialog(this);
            }
        }
        /// <summary>
        /// Aktuálně zobrazovaný graf. Dokud nejsou načtena data, je zde null.
        /// Toto je definice grafu, nikoli jeho data.
        /// </summary>
        internal Data.GraphInfo CurrentGraph { get; private set; }
        /// <summary>
        /// Reálná data aktuálního grafu.
        /// </summary>
        internal Data.GraphData CurrentGraphData { get; private set; }
        /// <summary>
        /// Tabulka s daty aktuálního grafu
        /// </summary>
        internal System.Data.DataTable CurrentGraphDataSource { get; private set; }
        #region Editace grafu
        /*
        private void _MenuActionNewEmpty()
        {
            string name = "Nový graf";
            if (_EditChartName(ref name))
            {
                string layout = "";
                if (_EditChartLayout(ref layout))
                {
                    ChartSetting setting = new ChartSetting(name, layout);
                    _ChartChanged(ChartChangeType.NewSettings, setting);
                    _SetSetting(setting, true);
                    _ChartChanged(ChartChangeType.SelectSetting, setting);
                }
            }
        }
        private void _MenuActionNewCopy()
        {
            string name = (_CurrentSetting != null ? (_CurrentSetting.Name + " 2") : "Nový graf");
            if (_EditChartName(ref name))
            {
                string layout = _GetLayout();
                if (_EditChartLayout(ref layout))
                {
                    ChartSetting setting = new ChartSetting(name, layout);
                    _ChartChanged(ChartChangeType.NewSettings, setting);
                    _SetSetting(setting, true);
                    _ChartChanged(ChartChangeType.SelectSetting, setting);
                }
            }
        }
        private void _MenuActionPasteFromClipboard()
        {
            string definition = null;
            if (WF.Clipboard.ContainsText(WF.TextDataFormat.UnicodeText))
                definition = WF.Clipboard.GetText(WF.TextDataFormat.UnicodeText);
            else if (WF.Clipboard.ContainsText(WF.TextDataFormat.Text))
                definition = WF.Clipboard.GetText(WF.TextDataFormat.Text);
            if (String.IsNullOrEmpty(definition))
                return;

            ChartSetting setting = ChartSetting.CreateFromDefinition(definition);
            if (setting is null)
                return;

            string layout = setting.Layout;
            if (_EditChartLayout(ref layout))
            {
                setting.Layout = layout;
                _ChartChanged(ChartChangeType.NewSettings, setting);
                _SetSetting(setting, true);
                _ChartChanged(ChartChangeType.SelectSetting, setting);
            }
        }
        private void _MenuActionRename()
        {
            var setting = _CurrentSetting;
            if (setting is null) return;
            string name = setting.Name;
            if (_EditChartName(ref name, true))
            {
                setting.Name = name;
                _SettingsCombo.Text = name;      // Poznámka: nic jiného nefunguje, například Refresh();  RefreshEditValue();  SelectedItem = _CurrentSettings; => nic nepromítne aktuální text do comba!!!
                _ChartChanged(ChartChangeType.ChangeName, setting);
            }
        }
        /// <summary>
        /// Akce: edituj vzhled grafu
        /// </summary>
        /// <returns></returns>
        private bool _MenuActionEdit()
        {
            var setting = _CurrentSetting;
            if (setting is null) return false;
            string layout = null;
            bool result = _EditChartLayout(ref layout);
            if (result)
            {
                setting.Layout = layout;
                _ChartChanged(ChartChangeType.ChangeLayout, setting);
            }
            return result;
        }
        private void _MenuActionCopyToClipboard()
        {
            var setting = _CurrentSetting;
            if (setting is null) return;
            WF.Clipboard.SetText(setting.Definition);
        }
        private void _MenuActionDelete()
        {
            var setting = _CurrentSetting;
            if (setting is null) return;
            int index = _ChartSettings.IndexOf(setting);
            if (index < 0) return;
            _ChartSettings.RemoveAt(index);
            _ChartChanged(ChartChangeType.Delete, setting);
            int count = _ChartSettings.Count;
            setting = (index < count ? _ChartSettings[index] : (count > 0 ? _ChartSettings[count - 1] : null));
            _ComboItemsReload(setting);
            CurrentSettings = setting;
        }
        private void _MenuActionExportPdf()
        {
            string fileName = null;
            using (var dialog = new WF.SaveFileDialog())
            {
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.DefaultExt = ".pdf";
                dialog.Filter = "PDF dokument|*.pdf|SVG grafika|*.svg";   // Řetězce pro různé volby filtrování rovněž musí být odděleny svislou čarou. Příklad: Textové soubory (*.txt)|*.txt|Všechny soubory (*.*)|*.*'

                var result = dialog.ShowDialog(this.FindForm());
                if (result == WF.DialogResult.OK)
                    fileName = dialog.FileName;
            }
            if (fileName is null) return;

            byte[] content = null;
            using (SIO.MemoryStream ms = new SIO.MemoryStream())
            {
                var options = new DevExpress.XtraPrinting.PdfExportOptions()
                {
                    ConvertImagesToJpeg = false,
                    ImageQuality = DevExpress.XtraPrinting.PdfJpegImageQuality.Highest,
                    PdfACompatibility = DevExpress.XtraPrinting.PdfACompatibility.PdfA1b,
                    RasterizationResolution = 300,
                    ShowPrintDialogOnOpen = false
                };
                // _Chart.ExportToDocx
                _Chart.ExportToPdf(ms, options);
                content = ms.ToArray();
            }

            if (fileName != null)
            {
                try
                {
                    SIO.File.WriteAllBytes(fileName, content);
                    var result = WF.MessageBox.Show(this.FindForm(), "Graf je uložen. Přejete si jej otevřít?", "Export grafu...", WF.MessageBoxButtons.YesNo, WF.MessageBoxIcon.Question);
                    if (result == WF.DialogResult.Yes)
                        System.Diagnostics.Process.Start(fileName);
                }
                catch (Exception exc)
                {
                    WF.MessageBox.Show(this.FindForm(), "Došlo k chybě: " + exc.Message, "Export grafu...", WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Exclamation);
                }
            }
        }
        /// <summary>
        /// Výkonná procedura pro změnu názvu (=vyvolání okna), ale nepracuje s <see cref="_CurrentSetting"/> ani nevolá eventy.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="detectChange"></param>
        /// <returns></returns>
        private bool _EditChartName(ref string name, bool detectChange = false)
        {
            string oldName = name;
            string newName = TestDevExpress.Forms.InputForm.InputDialogShow(this.FindForm(), null, "Název grafu:", oldName);
            if (String.IsNullOrEmpty(newName) || (detectChange && String.Equals(oldName, newName))) return false;
            name = newName;
            return true;
        }
        /// <summary>
        /// Výkonná procedura pro změnu vzhledu grafu (=vyvolání Designeru), ale nepracuje s <see cref="_CurrentSetting"/> ani nevolá eventy.
        /// <para/>
        /// Zajistí editaci vzhledu grafu pomocí designeru <see cref="DC.Designer.ChartDesigner"/>.
        /// Do grafu může předem vložit daný Layout z parametru <paramref name="layout"/> (pokud není NULL).
        /// Pokud v Designeru bude potvrzen zadaný Layout tlačítkem OK, pak získá Layout editovaného grafu a vloží jej do ref parametru <paramref name="layout"/>, a vrátí true.
        /// Layout neukládá do žádného <see cref="ChartSetting"/>.
        /// </summary>
        /// <param name="layout"></param>
        /// <returns></returns>
        private bool _EditChartLayout(ref string layout)
        {
            bool result = false;

            string oldLayout = null;
            if (layout != null)
            {
                oldLayout = _GetLayout();
                _SetLayout(layout);
            }

            DC.Designer.ChartDesigner designer = new DC.Designer.ChartDesigner(_Chart)
            {
                Caption = "Upravte graf...",
                ShowActualData = true
            };

            var response = designer.ShowDialog();
            result = (response == WF.DialogResult.OK);

            if (result)
                layout = _GetLayout();
            else if (oldLayout != null)
                // Storno v Designeru, a my máme uschován původní layout grafu => vrátíme jej:
                _SetLayout(oldLayout);

            return result;
        }
        /// <summary>
        /// Vyvolá event <see cref="ChartChanged"/> s danými hodnotami
        /// </summary>
        /// <param name="changeType"></param>
        /// <param name="setting"></param>
        private void _ChartChanged(ChartChangeType changeType, ChartSetting setting)
        {
            if (ChartChanged == null) return;
            ChartChangedArgs args = new ChartChangedArgs(changeType, setting);
            ChartChanged(this, args);
        }
        */
        #endregion
        #endregion
    }
}
