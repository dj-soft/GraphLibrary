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
using Djs.Tools.CovidGraphs.Data;

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
            InitDevExpress();
            InitForm();
            InitFrames();
            InitRibbons();
            InitList();
            InitChart();
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.LoadDatabase();
            Data.App.Config.SaveEnabled = true;
        }
        /// <summary>
        /// Inicializace vlastností DevExpress, inciace skinu podle konfigurace
        /// </summary>
        private void InitDevExpress()
        {
            DevExpress.UserSkins.BonusSkins.Register();
            DevExpress.Skins.SkinManager.EnableFormSkins();
            DevExpress.Skins.SkinManager.EnableMdiFormSkins();

            // DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = "Stardust";
            if (Data.App.Config.HasSkin)
                DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle(Data.App.Config.ActiveSkinName, Data.App.Config.ActiveSkinPalette);

            DevExpress.LookAndFeel.UserLookAndFeel.Default.StyleChanged += DevExpress_StyleChanged;

            Data.Localization.Enabled = true;
        }
        /// <summary>
        /// Po změně SKinu uživatelem se uloží do konfigurace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DevExpress_StyleChanged(object sender, EventArgs e)
        {
            string skinName = DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveSkinName;
            string paletteName = DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveSvgPaletteName;
            Data.App.Config.SetSkin(skinName, paletteName);
        }
        /// <summary>
        /// Inicializace vlastností formuláře
        /// </summary>
        private void InitForm()
        {
            this.IconOptions.Icon = Properties.Resources.aeskulap;
            this.WindowState = FormWindowState.Maximized;
            this.Text = "Best in Covid! [ČR]";
        }
        /// <summary>
        /// Inicializace vnitřního layoutu - splitpanel
        /// </summary>
        private void InitFrames()
        {
            int splitterPosition = Data.App.Config.MainSplitterPosition;
            _MainSplitContainer = new DXE.SplitContainerControl()
            {
                FixedPanel = DXE.SplitFixedPanel.Panel1,
                Horizontal = true,
                PanelVisibility = DXE.SplitPanelVisibility.Both,
                SplitterPosition = splitterPosition,
                Dock = DockStyle.Fill
            };
            _MainSplitContainer.ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True;
            _MainSplitContainer.SplitterPositionChanged += _SplitterPositionChanged;

            this.Controls.Add(_MainSplitContainer);
        }
        /// <summary>
        /// Inicializace objektu Ribbon a Statusbar
        /// </summary>
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
        /// <summary>
        /// Po změně pozice splitteru v hlavním splitpanelu se uloží pozice splitteru do konfigurace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SplitterPositionChanged(object sender, EventArgs e)
        {
            Data.App.Config.MainSplitterPosition = _MainSplitContainer.SplitterPosition;
        }
        /// <summary>
        /// Inicializace objektu seznamu grafů, součástí je i načtení seznamu grafů z disku
        /// </summary>
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
        /// <summary>
        /// Po změně vybraného prvku v seznamu grafů se vyvolá načtení grafu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _GraphListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowCurrentGraph();
        }
        /// <summary>
        /// Inicializace objektu ChartControl = objekt zobrazující graf
        /// </summary>
        private void InitChart()
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
        /// <summary>
        /// Naplní prvky do Ribbonu, zaháčkuje eventhandlery
        /// </summary>
        private void RibbonFillItems()
        {
            this.Ribbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;

            DXB.Ribbon.RibbonPage page0 = new DXB.Ribbon.RibbonPage("GRAF");
            this.Ribbon.Pages.Add(page0);

            DXB.Ribbon.RibbonPageGroup group01 = new DXB.Ribbon.RibbonPageGroup("SPRÁVA GRAFŮ");
            page0.Groups.Add(group01);

            DXB.Ribbon.RibbonPageGroup group02 = new DXB.Ribbon.RibbonPageGroup("SPRÁVA DAT");
            page0.Groups.Add(group02);

            RibbonAddButton(group01, "Přidej", "Přidá nový graf", Properties.Resources.document_new_6_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickNewGraph);
            RibbonAddButton(group01, "Uprav", "Umožní změnit datové zdroje grafu: výběr obcí, výběr dat", Properties.Resources.document_properties_2_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickEditDataGraph);
            RibbonAddButton(group01, "Vzhled grafu", "Otevře editor vzhledu grafu", Properties.Resources.document_page_setup_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickEditLayoutGraph);
            RibbonAddButton(group01, "Smaž", "Smaže aktuální graf", Properties.Resources.document_close_4_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickDeleteGraph);
            RibbonAddButton(group02, "Aktualizuj data", "Zajistí aktualizaci dat z internetu", Properties.Resources.download_3_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickWebUpdate);
            RibbonAddButton(group02, "Značky", "Značky jsou společné, jsou vkládány do všech grafů, označují význačné časové úseky", Properties.Resources.system_switch_user_2_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickEditStrip);
            RibbonAddButton(group02, "Ulož", "Uloží všechna aktuální data do datových souborů (Structure, Data, i kompaktního DataPack, ten lze např odeslat mailem).", Properties.Resources.document_save_as_6_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickSaveData);
            RibbonAddButton(group02, "Config", "Nastavení projektu", Properties.Resources.run_build_configure_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickRunConfig);


            DXB.Ribbon.RibbonPage page1 = new DXB.Ribbon.RibbonPage("VZHLED APLIKACE");
            this.Ribbon.Pages.Add(page1);

            DXB.Ribbon.RibbonPageGroup group10 = new DXB.Ribbon.RibbonPageGroup("VZHLED");
            page1.Groups.Add(group10);

            group10.ItemLinks.Add(new DXB.SkinDropDownButtonItem());
            group10.ItemLinks.Add(new DXB.SkinPaletteRibbonGalleryBarItem());
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
            Data.App.TryRun(TryShowGraphFormNew);
        }
        private void RibbonClickEditDataGraph(object sender, DXB.ItemClickEventArgs e)
        {
            Data.App.TryRun(TryShowGraphFormCurrent);
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
        private void RibbonClickSaveData(object sender, DXB.ItemClickEventArgs e)
        {
            Data.App.TryRun(TrySaveData);
        }
        private void RibbonClickRunConfig(object sender, DXB.ItemClickEventArgs e)
        {
            Data.App.TryRun(TryRunConfig);
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

            this._Database = new Data.DatabaseInfo();
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
        private void TrySaveData()
        {
            if (this._Database == null)
                throw new InvalidOperationException("Nelze uložit databázi, dosud není vytvořena. Počkejte několik sekund...");
            if (!this._Database.IsReady)
                throw new InvalidOperationException("Nelze uložit databázi, dosud není připravena. Počkejte několik sekund...");

            this._Database.SaveStandardData(true, DatabaseShowProgress);
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
        private Data.DatabaseInfo _Database;
        #endregion
        #region Správce grafů
        /// <summary>
        /// Připraví a naplní pole pro definované grafy <see cref="_Graphs"/>
        /// </summary>
        private void LoadGraphs()
        {
            var graphs = Data.GraphInfo.LoadFromPath();
            if (graphs.Count == 0)
                graphs = Data.GraphInfo.CreateSamples(true);

            _Graphs = graphs;
        }
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
                CurrentGraph.WorkingChartLayout = _GetChartControlLayout();
            }

            // Přepneme na nový layout:
            CurrentGraph = graph;
            CurrentGraphData = graph.LoadData(database);
            CurrentGraphDataSource = CurrentGraphData.DataTable;

            // Layout:
            string layout = graph.WorkingChartLayout;
            if (String.IsNullOrEmpty(layout)) layout = graph.ChartLayout;
            if (String.IsNullOrEmpty(layout)) layout = graph.CreateDefaultLayout(CurrentGraphData);

            // Tato sekvence je důležitá, jinak dojde k chybám:
            _SetChartControlLayout("");
            _ChartControl.DataSource = CurrentGraphDataSource;
            _SetChartControlLayout(layout);

            CurrentGraphData.FinaliseShowGraph();
            this.ShowLoadGraphDataResult(CurrentGraphData);
        }
        /// <summary>
        /// Do Statusbaru vloží výsledky o načítání dat grafu z databáze
        /// </summary>
        /// <param name="graphData"></param>
        private void ShowLoadGraphDataResult(GraphData graphData)
        {
            string text = $"Načtena data grafu: analyzováno {graphData.ScanRecordCountText} vět, získáno {graphData.LoadRecordCountText} položek, zobrazeno {graphData.ShowRecordCountText} hodnot, za {graphData.LoadSecondsText}.";
            StatusInfoText = text;
        }
         /// <summary>
        /// Otevře formulář pro definici dat nového grafu
        /// </summary>
        private void TryShowGraphFormNew()
        {
            ShowGraphFormEditor(null);
        }
        /// <summary>
        /// Otevře formulář pro definici dat aktuálního grafu
        /// </summary>
        private void TryShowGraphFormCurrent()
        {
            ShowGraphFormEditor(this.CurrentGraph);
        }
        /// <summary>
        /// Otevře formulář pro definici dat grafu
        /// </summary>
        private void ShowGraphFormEditor(GraphInfo graphInfo)
        {
            if (this._Database == null || !this._Database.HasData) throw new InvalidOperationException("Dosud nejsou načtena a připravena data, počkejte malou chvilku...");

            bool isNewGraph = (graphInfo == null);
            if (graphInfo == null) graphInfo = new GraphInfo();
            using (var graphForm = new GraphForm(this.VisibleBounds))
            {
                graphForm.Database = this._Database;
                graphForm.ShowSaveAsNewButton = !isNewGraph;                   // Zobrazit tlačítko "Uložit jako nový graf" jen tehdy, když editujeme nějaký stávající graf
                graphForm.CurrentGraphInfo = graphInfo;
                var result = graphForm.ShowDialog(this);

                if ((isNewGraph && result == DialogResult.OK) || (!isNewGraph && result == DialogResult.Yes))
                {   // Uložit nový graf:
                    graphInfo = graphForm.CurrentGraphInfo;                    // Převezmeme si novou instanci z editoru.
                    graphInfo.ResetId();
                    graphInfo.SaveToFile();
                    _Graphs.Add(graphInfo);
                    _GraphListBox.Refresh();
                    _GraphListBox.SelectedItem = graphInfo;                    // Nový graf se bude aktivovat (a tím se i zobrazí) přes event: _GraphListBox.SelectedIndexChanged
                }
                else if (!isNewGraph && result == DialogResult.OK)
                {   // Uložit stávající graf:
                    graphInfo.Serial = graphForm.CurrentGraphInfoSerial;       // Ponecháme si svoji instanci grafu, ale vepíšeme do ní data z editoru
                    graphInfo.SaveToFile();
                    ShowCurrentGraph();                                        // Protože se nemění selectovaný prvek v _GraphListBox, musím vynutit zobrazení grafu
                }
            }
        }

        private void TryRunConfig() { }
        public Rectangle VisibleBounds
        {
            get
            {
                Rectangle bounds = this.Bounds;
                if (this.WindowState == FormWindowState.Maximized)
                    bounds = new Rectangle(bounds.X + 8, bounds.Y + 8, bounds.Width - 16, bounds.Height - 16);
                return bounds;
            }
        }
        /// <summary>
        /// Otevře Wizard pro editaci layoutu aktuálního grafu
        /// </summary>
        private void TryEditChartLayout()
        {
            var graph = CurrentGraph;
            if (graph == null)
                throw new InvalidOperationException($"V tuto chvíli nelze editovat graf, dosud není načten nebo není definován.");

            string oldLayout = _GetChartControlLayout();

            DC.Designer.ChartDesigner designer = new DC.Designer.ChartDesigner(_ChartControl)
            {
                Caption = "Upravte graf...",
                ShowActualData = true
            };
            var response = designer.ShowDialog(false);

            // Wizard pracuje nad naším controlem, veškeré změny ve Wizardu provedené se ihned promítají do našeho grafu.
            // Pokud uživatel dal OK, chceme změny uložit i do příště,
            // pokud ale dal Cancel, pak změny chceme zahodit a vracíme se k původnímu layoutu:
            bool result = (response == WF.DialogResult.OK);
            if (result)
            {
                graph.ChartLayout = _GetChartControlLayout();
                graph.WorkingChartLayout = null;
                graph.SaveToFile();
            }
            else
            {
                _SetChartControlLayout(oldLayout);
            }
        }
        /// <summary>
        /// Načte a vrátí Layout z aktuálního grafu
        /// </summary>
        /// <returns></returns>
        private string _GetChartControlLayout()
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
        private void _SetChartControlLayout(string layout)
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
        /// <summary>
        /// Pole grafů, základ pro nabídku grafů v hlavním okně
        /// </summary>
        private List<Data.GraphInfo> _Graphs;
        #endregion
    }
}
