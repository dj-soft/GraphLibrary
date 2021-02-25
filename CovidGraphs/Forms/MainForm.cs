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
            this.FormAppTitle = "Best in Covid";
            this.FormAppTitleEmpty = "Best in Covid! [ČR]";
        }
        /// <summary>
        /// Inicializace vnitřního layoutu - splitpanel
        /// </summary>
        private void InitFrames()
        {
            int splitterPosition = Data.App.Config.MainSplitterPosition;
            _MainSplitContainer = DxComponent.CreateDxSplitContainer(this, _SplitterPositionChanged,
                DockStyle.Fill, Orientation.Vertical, DXE.SplitFixedPanel.Panel1,
                splitterPosition, DXE.SplitPanelVisibility.Both, true);
        }
        /// <summary>
        /// Inicializace objektu Ribbon a Statusbar
        /// </summary>
        private void InitRibbons()
        {
            _DxRibbonControl = new DxRibbonControl();
            _DxRibbonControl.PaintImageRightBefore += Ribbon_PaintImageRightBefore;
            _DxRibbonControl.Items.Clear();
            this.Ribbon = _DxRibbonControl;
            _RibbonLastChangeTime = DateTime.Now.AddDays(-1);
            _RibbonCurrentImageIndex = -1;
            _RibbonImages = new Image[] { Properties.Resources.Covid19a_64, Properties.Resources.Covid19b_64, Properties.Resources.Covid19c_64, Properties.Resources.Covid19d_64 };

            this.StatusBar = new DXB.Ribbon.RibbonStatusBar();
            this.StatusBar.Ribbon = this.Ribbon;

            RibbonFillItems();
            StatusFillItems();

            this.Controls.Add(this.Ribbon);
            this.Controls.Add(this.StatusBar);
        }
        /// <summary>
        /// Před vykreslením obrázku v Ribbonu vpravo. Zde je možnost obrázek změnit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Ribbon_PaintImageRightBefore(object sender, PaintEventArgs e)
        {
            TimeSpan ribbonTime = DateTime.Now - _RibbonLastChangeTime;
            if (_DxRibbonControl.ImageRightFull != null && ribbonTime.TotalMinutes < 5) return;
            int count = (_RibbonImages != null ? _RibbonImages.Length : -1);
            if (count > 0)
            {
                if (_RibbonCurrentImageIndex < 0)
                    _RibbonCurrentImageIndex = ((new Random()).Next(0, count));
                else
                    _RibbonCurrentImageIndex = ((_RibbonCurrentImageIndex + 1) % count);

                _DxRibbonControl.ImageRightFull = _RibbonImages[_RibbonCurrentImageIndex];
            }
            _RibbonLastChangeTime = DateTime.Now;
        }
        int _RibbonCurrentImageIndex;
        DateTime _RibbonLastChangeTime;
        Image[] _RibbonImages;
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
            LoadGraphs();

            _GraphListBox = DxComponent.CreateDxListBox(DockStyle.Fill, parent: _MainSplitContainer.Panel1, selectedIndexChanged: _GraphListBox_SelectedIndexChanged, multiColumn: false, selectionMode: SelectionMode.One, itemHeightPadding: 3, reorderByDragEnabled: true);
            _GraphListBox.DataSource = _Graphs;
            _GraphListBox.SelectedIndex = 0;


            // var mcx = DXE.MultiColumnListBoxCreator.CreateMultiColumnListBox();



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
        DxRibbonControl _DxRibbonControl;
        DXB.BarStaticItem _StatusInfoTextItem;
        DXE.Repository.RepositoryItemProgressBar _StatusProgressBar;
        DXB.BarEditItem _StatusProgressEdit;
        DxSplitContainerControl _MainSplitContainer;
        DxListBoxControl _GraphListBox;
        DevExpress.XtraCharts.ChartControl _ChartControl;
        /// <summary>
        /// Titulek okna, část Aplikace
        /// </summary>
        public string FormAppTitle { get { return _FormAppTitle; } set { _FormAppTitle = value; RefreshTitle(); } }
        private string _FormAppTitle;
        /// <summary>
        /// Titulek okna, Aplikace bez dokumentu
        /// </summary>
        public string FormAppTitleEmpty { get { return _FormAppTitleEmpty; } set { _FormAppTitleEmpty = value; RefreshTitle(); } }
        private string _FormAppTitleEmpty;
        /// <summary>
        /// Titulek okna, část Dokument
        /// </summary>
        public string FormDocumentTitle { get { return _FormDocumentTitle; } set { _FormDocumentTitle = value; RefreshTitle(); } }
        private string _FormDocumentTitle;
        /// <summary>
        /// Aktualizuje titulek okna z podkladů <see cref="FormAppTitle"/> a <see cref="FormDocumentTitle"/>
        /// </summary>
        protected void RefreshTitle()
        {
            string text = "";
            string document = _FormDocumentTitle;
            if (String.IsNullOrEmpty(document))
                text = _FormAppTitleEmpty;
            else
                text = $"{_FormAppTitle}: {(document.Trim())}";
            this.Text = text;
        }
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

            DXB.Ribbon.RibbonPageGroup group00 = new DXB.Ribbon.RibbonPageGroup("SPRÁVA GRAFŮ");
            page0.Groups.Add(group00);

            DXB.Ribbon.RibbonPageGroup group01 = new DXB.Ribbon.RibbonPageGroup("SPRÁVA DAT");
            page0.Groups.Add(group01);

            RibbonAddButton(group00, "Přidej", "Přidá nový graf", Properties.Resources.document_new_6_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickNewGraph);
            RibbonAddButton(group00, "Uprav", "Umožní změnit datové zdroje grafu: výběr obcí, výběr dat", Properties.Resources.document_properties_2_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickEditDataGraph);
            RibbonAddButton(group00, "Vzhled grafu", "Otevře editor vzhledu grafu", Properties.Resources.document_page_setup_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickEditLayoutGraph);
            RibbonAddButton(group00, "Smaž", "Smaže aktuální graf", Properties.Resources.document_close_4_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickDeleteGraph);
            RibbonAddButton(group01, "Aktualizuj data", "Zajistí aktualizaci dat z internetu", Properties.Resources.download_3_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickWebUpdate);
            RibbonAddButton(group01, "Značky", "Značky jsou společné, jsou vkládány do všech grafů, označují význačné časové úseky", Properties.Resources.system_switch_user_2_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickEditStrip);
            RibbonAddButton(group01, "Ulož", "Uloží všechna aktuální data do datových souborů (Structure, Data, i kompaktního DataPack, ten lze např odeslat mailem).", Properties.Resources.document_save_as_6_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickSaveData);
            RibbonAddButton(group01, "Config", "Nastavení projektu", Properties.Resources.run_build_configure_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickRunConfig);


            DXB.Ribbon.RibbonPage page1 = new DXB.Ribbon.RibbonPage("VZHLED APLIKACE");
            this.Ribbon.Pages.Add(page1);

            DXB.Ribbon.RibbonPageGroup group10 = new DXB.Ribbon.RibbonPageGroup("VZHLED");
            page1.Groups.Add(group10);

            group10.ItemLinks.Add(new DXB.SkinDropDownButtonItem());
            group10.ItemLinks.Add(new DXB.SkinPaletteRibbonGalleryBarItem());


            if (App.IsRunningInVisualStudio)
            {
                DXB.Ribbon.RibbonPage page2 = new DXB.Ribbon.RibbonPage("JINÉ TESTY");
                this.Ribbon.Pages.Add(page2);

                DXB.Ribbon.RibbonPageGroup group20 = new DXB.Ribbon.RibbonPageGroup("KOMPONENTY");
                page2.Groups.Add(group20);

                RibbonAddButton(group20, "TreeView", "Otevře okno pro testování TreeView", Properties.Resources.code_class_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickTestTreeView);
                RibbonAddButton(group20, "Analýza dat", "V databázi vyhledá mezní hodnoty pomocí analýzy", Properties.Resources.straw_32, DXB.Ribbon.RibbonItemStyles.Large, RibbonClickAnalytics);
            }
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
            Data.App.TryRun(TryDeleteGraph);
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

        private void RibbonClickAnalytics(object sender, DXB.ItemClickEventArgs e)
        {
            if (this._Database == null) return;

            DatabaseInfo.EntityInfo entityCz = this._Database.GetEntity("CZ");
            DateTime now = DateTime.Now.Date;
            DateTime analyseBegin = now.AddDays(-6d);
            DateTime analyseEnd = now.AddDays(1d);

            DateTime start = DateTime.Now;
            var result = this._Database.GetResultsAnalytic(entityCz, DataValueType.NewCount7DaySumRelativeAvg, EntityType.Obec,
                4, true, 2, analyseBegin, analyseEnd);
            DateTime end = DateTime.Now;
            var time = end - start;
            var seconds = time.TotalSeconds;
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

            this._Database.SaveStandardData(true, true, DatabaseShowProgress);
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

            if (args.IsDone && this._Database != null && args.ProcessState == Data.ProcessFileState.Loaded && args.ContentHasData)
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
            this.FormDocumentTitle = "";

            if (!(_GraphListBox.SelectedItem is Data.GraphInfo graph)) return;

            var database = _Database;
            if (database == null) return;
            if (!database.HasData) return;

            if (CurrentGraph != null && !Object.ReferenceEquals(CurrentGraph, graph))
            {   // Změna grafu z existujícího na nový: načtu aktuální Layout a uložím si jej do dat grafu, protože Layout může obsahovat změnu měřítka:
                CurrentGraph.WorkingChartLayout = _GetChartControlLayout();
            }

            // Přepneme se na nový layout:
            CurrentGraph = graph;
            CurrentGraphData = graph.LoadData(database);
            CurrentGraphDataSource = CurrentGraphData.DataTable;
            this.FormDocumentTitle = graph.Title;

            // Zobrazíme graf s určitým layoutem:
            bool isOk = TryShowChartLayoutBySource(ShowChartSourceType.Working);
            if (!isOk) isOk = TryShowChartLayoutBySource(ShowChartSourceType.User);
            if (!isOk) isOk = TryShowChartLayoutBySource(ShowChartSourceType.Default);

            CurrentGraphData.FinaliseShowGraph();
            this.ShowLoadGraphDataResult(CurrentGraphData);

            string name = graph.ScreenshotFileName + ".png";
            _ChartControl.ExportToImage(name, System.Drawing.Imaging.ImageFormat.Png);
        }
        private bool TryShowChartLayoutBySource(ShowChartSourceType source)
        {
            var graph = CurrentGraph;
            string layout = (source == ShowChartSourceType.Working ? graph.WorkingChartLayout :
                            (source == ShowChartSourceType.User ? graph.ChartLayout :
                            (source == ShowChartSourceType.Default ? graph.CreateDefaultLayout(CurrentGraphData) : null)));
            if (String.IsNullOrEmpty(layout)) return false;

            // Tato sekvence je důležitá, jinak dojde k chybám:
            bool result = false;
            try
            {
                _SetChartControlLayout("");
                _ChartControl.DataSource = CurrentGraphDataSource;
                _SetChartControlLayout(layout);
                result = true;
            }
            catch (Exception exc)
            {
                string text;
                string eol = Environment.NewLine;
                switch (source)
                {
                    case ShowChartSourceType.Working:
                        result = false;
                        break;
                    case ShowChartSourceType.User:
                        text = $"V uložené definici grafu je chyba: '{exc.Message}'. {eol}{eol}K tomu může dojít, pokud existuje graf s upraveným vzhledem, a následně je z něj odebrána datová řada. {eol}{eol}Přejete si definici zahodit a zobrazit graf ve výchozím vzhledu?";
                        if (App.ShowQuestionYN(this, text))
                        {
                            graph.ChartLayout = null;
                            graph.SaveToFile();
                            result = false;
                        }
                        else
                        {
                            result = true;
                        }
                        break;
                    case ShowChartSourceType.Default:
                        text = $"V základní definici grafu je chyba: '{exc.Message}'. {eol}{eol}Bohužel, tuto chybu musí opravit programátor. Zkuste upravit vzhled grafu a vytvořit jej ručně.";
                        App.ShowWarning(this, text);
                        result = false;
                        break;
                }
            }
            return result;
        }
        /// <summary>
        /// Druh zdroje pro layout grafu
        /// </summary>
        private enum ShowChartSourceType { None, Working, User, Default }
        /// <summary>
        /// Do Statusbaru vloží výsledky o načítání dat grafu z databáze
        /// </summary>
        /// <param name="graphData"></param>
        private void ShowLoadGraphDataResult(GraphData graphData)
        {
            var counts = graphData.GraphScanCounts;
            string text = $"Načtena data grafu: analyzováno {counts.ScanRecordCountText} vět, získáno {counts.LoadRecordCountText} položek, zobrazeno {counts.ShowRecordCountText} hodnot, za {graphData.LoadSecondsText}.";
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
        private void TryDeleteGraph()
        {
            var graph = CurrentGraph;
            if (graph == null)
                throw new InvalidOperationException($"V tuto chvíli nelze odstranit graf, dosud není načten nebo není definován.");

            if (!App.ShowQuestionYN(this, $"Přejete si odstranit graf '{graph.Title}' ?")) return;

            int index = _Graphs.FindIndex(g => Object.ReferenceEquals(g, graph));
            if (index >= 0)
            {
                _Graphs.RemoveAt(index);
                _GraphListBox.Refresh();
                ShowCurrentGraph();
            }

            graph.DeleteGraphFile(true);
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
                    var buffer = ms.ToArray();
                    layout = Encoding.UTF8.GetString(buffer);
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
            if (_IsChartExists)
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
        /// Obsahuje true, pokud graf existuje
        /// </summary>
        private bool _IsChartExists { get { return (_ChartControl != null && !_ChartControl.IsDisposed); } }
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
        #region Testy
        private void RibbonClickTestTreeView(object sender, DXB.ItemClickEventArgs e)
        {
            Data.App.TryRun(TryTestTreeView);
        }
        private void TryTestTreeView()
        {
            DxTestTreeForm.Run();
        }
        #endregion
    }
}
