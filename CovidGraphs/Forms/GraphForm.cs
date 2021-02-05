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
using DXT = DevExpress.XtraTab;
using DXG = DevExpress.XtraGrid;
using DC = DevExpress.XtraCharts;
using DevExpress.XtraBars.Ribbon;

using Djs.Tools.CovidGraphs.Data;

namespace Djs.Tools.CovidGraphs
{
    /// <summary>
    /// Formulář pro zadání grafů
    /// </summary>
    public class GraphForm : DXE.XtraForm
    {
        #region Konstrukce, tvorba okna
        public GraphForm()
        {
            InitDevExpressComponents();
        }
        public GraphForm(Rectangle? parentBounds = null)
        {
            InitDevExpressComponents(parentBounds);
        }
        private void InitDevExpressComponents(Rectangle? parentBounds = null)
        {
            InitForm(parentBounds);
            InitFrames();
            InitButtons();
            InitData();
            InitChart();
            /*
            InitForm();
            InitFrames();
            InitRibbons();
            InitList();
            InitGraph();
            */
        }
        /// <summary>
        /// Inicializace vlastností formuláře
        /// </summary>
        protected void InitForm(Rectangle? parentBounds)
        {
            this.Text = "Upravit data grafu...";
            this.IconOptions.Icon = Properties.Resources.aeskulap;
            this.ShowInTaskbar = false;
            this.IsShown = false;
            this.MinimizeBox = false;
            this.DialogResult = DialogResult.None;
            this.MinimumSize = new Size(800, 600);

            var position = Data.App.Config.EditFormPosition;
            if (position != null && position.Length >= 5)
                RestoreFormBoundsToConfig(position, parentBounds);
            else
            {
                Size size = Size.Empty;
                if (parentBounds.HasValue && parentBounds.Value.Width > 100)
                    size = parentBounds.Value.Size;
                else
                    size = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Size;

                int w = size.Width * 8 / 10;
                int h = size.Height * 7 / 10;
                this.WindowState = FormWindowState.Normal;
                this.StartPosition = FormStartPosition.CenterParent;
                this.Size = new Size(w, h);
            }
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!IsShown)
                OnFirstShown();
        }
        protected void OnFirstShown()
        {
            this.IsShown = true;

            RestoreSplitters();

            _GraphPanel.OnFirstShown();
        }
        /// <summary>
        /// Po změně pozice formuláře - uložíme Position do configu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            StoreFormBoundsToConfig();
        }
        /// <summary>
        /// Po změně velikosti formuláře - uložíme Position do configu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            StoreFormBoundsToConfig();
        }
        /// <summary>
        /// Obsahuje true od chvíle, kdy začíná první zobrazení formuláře. Obsahuje false od konstruktoru do prvného Show().
        /// </summary>
        public bool IsShown { get; protected set; }
        /// <summary>
        /// Bude zobrazen button "Uložit jako nový" ?
        /// </summary>
        public bool ShowSaveAsNewButton { get { return _ShowSaveAsNewButton; } set { _ShowSaveAsNewButton = value; RefreshButtons(); } } private bool _ShowSaveAsNewButton;
        /// <summary>
        /// Inicializace vnitřního layoutu - splitpanel
        /// </summary>
        private void InitFrames()
        {
            _MainSplitContainer = new DXE.SplitContainerControl()
            {
                FixedPanel = DXE.SplitFixedPanel.Panel2,
                Horizontal = true,
                PanelVisibility = DXE.SplitPanelVisibility.Both,
                SplitterPosition = 500,
                Dock = DockStyle.Fill,
                ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True
            };

            this.Controls.Add(_MainSplitContainer);
        }
        /// <summary>
        /// Vytvoří panel pro buttony, vytvoří buttony, naváže handlery a postaví layout
        /// </summary>
        protected void InitButtons()
        {
            var panel = new DevExpress.XtraEditors.PanelControl() { BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, TabStop = false, Dock = DockStyle.Bottom };
            panel.Appearance.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
            panel.Visible = true;
            panel.SizeChanged += _ButtonPanel_SizeChanged;
            panel.Height = DefaultButtonPanelHeight;
            _ButtonPanel = panel;
            this.Controls.Add(panel);

            _ButtonSave = new DXE.SimpleButton() { Text = "Uložit", Size = new Size(DefaultButtonWidth, DefaultButtonHeight), Enabled = false };
            _ButtonSave.Click += ButtonSave_Click;
            this._ButtonPanel.Controls.Add(_ButtonSave);
          
            _ButtonSaveAs = new DXE.SimpleButton() { Text = "Uložit jako nový", Size = new Size(DefaultButtonWidth, DefaultButtonHeight) };
            _ButtonSaveAs.Click += ButtonSaveAs_Click;
            this._ButtonPanel.Controls.Add(_ButtonSaveAs);
          
            _ButtonCancel = new DXE.SimpleButton() { Text = "Storno", Size = new Size(DefaultButtonWidth, DefaultButtonHeight) };
            _ButtonCancel.Click += ButtonCancel_Click;
            this._ButtonPanel.Controls.Add(_ButtonCancel);
            
            // this.AcceptButton = _Button1;
            this.CancelButton = _ButtonCancel;

            _ButtonPanelLayout();
        }
        /// <summary>
        /// Aktualizuje Enabled na buttonech podle stavu dat
        /// </summary>
        protected void RefreshButtons()
        {
            _ButtonSave.Enabled = this.ContainChanges;
            _ButtonSaveAs.Enabled = true;
            _ButtonCancel.Enabled = true;

            bool showSaveAsNewButton = this._ShowSaveAsNewButton;
            if (_ButtonSaveAs.Visible != showSaveAsNewButton)
            {   // Změna viditelnosti?
                _ButtonSaveAs.Visible = false;
                _ButtonPanelLayout();
                _ButtonSaveAs.Visible = showSaveAsNewButton;
            }
        }
        internal const int DefaultButtonPanelHeight = 40;
        internal const int DefaultButtonWidth = 150;
        internal const int DefaultButtonHeight = 31;
        /// <summary>
        /// Po kliknutí na "Uložit"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSave_Click(object sender, EventArgs e)
        {
            if (_GraphPanel.IsValidData("Graf nelze uložit, obsahuje chyby:\r\n\r\n"))
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        /// <summary>
        /// Po kliknutí na "Uložit jako nový"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSaveAs_Click(object sender, EventArgs e)
        {
            if (_GraphPanel.IsValidData("Graf nelze uložit, obsahuje chyby:\r\n\r\n"))
            {
                this.DialogResult = DialogResult.Yes;
                this.Close();
            }
        }
        /// <summary>
        /// Po kliknutí na "Storno"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
            this.Close();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            if (CanCloseForm(e))
                base.OnClosing(e);
            else
                e.Cancel = true;
        }
        /// <summary>
        /// Zjistí, zda se může provést zavření okna. Vrátí true = lze zavřít, false = zastavit zavření okna.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected bool CanCloseForm(CancelEventArgs e)
        {
            if (!this.ContainChanges) return true;
            if (this.DialogResult == DialogResult.OK || this.DialogResult == DialogResult.Yes) return true;     // true = Okno zavřeme, data budou uložena

            // Máme změny dat, ale nemáme požadavek na jejich uložení:
            // Podle obsahu this.DialogResult poznáme, zda se okno zavírá tlačítkem "Storno" (pak je zde DialogResult.No, a to i po Escape), anebo křížkem:
            // Pokud tlačítkem nebo escape, pak defaultní odpověď na následující dotaz bude přednastavena: "Uložit změny? NE", pokud křížkem, pak "STORNO":
            bool isCancelButton = (this.DialogResult == DialogResult.No);
            MessageBoxDefaultButton defaultButton = (isCancelButton ? MessageBoxDefaultButton.Button2 : MessageBoxDefaultButton.Button3);
            var result = App.ShowMessage(this, text: "V definici grafu jsou provedeny nějaké změny.\r\nPřejete si je uložit?", buttons: MessageBoxButtons.YesNoCancel, icon: MessageBoxIcon.Question, defaultButton: defaultButton);
            switch (result)
            {
                case DialogResult.Yes:
                    if (_GraphPanel.IsValidData("Graf nelze uložit, obsahuje chyby:\r\n\r\n"))
                    {   // Pokud jsou data OK, povolíme je uložit a zavřeme okno:
                        this.DialogResult = DialogResult.OK;
                        return true;
                    }
                    // Data nejsou OK: false = okno nezavíráme:
                    return false;
                case DialogResult.No:
                    // Neukládat data: výsledek je Cancel, a true = okno zavřeme:
                    this.DialogResult = DialogResult.Cancel;
                    return true;
                default:
                    // Vrátím false = nepovolíme zavírání okna:
                    // Zruším případný výsledek od tlačítka Cancel:
                    this.DialogResult = DialogResult.None;
                    return false;
            }
        }
        /// <summary>
        /// Po resize panelu Buttonů vyvolá Layout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonPanel_SizeChanged(object sender, EventArgs e)
        {
            _ButtonPanelLayout();
        }
        /// <summary>
        /// Nastaví souřadnice buttonů podle rozměru panelu buttonů
        /// </summary>
        private void _ButtonPanelLayout()
        {
            if (_ButtonCancel == null) return;

            Size size = _ButtonPanel.ClientSize;
            bool showSaveAsNewButton = this._ShowSaveAsNewButton;

            int bW = DefaultButtonWidth;
            int bH = DefaultButtonHeight;
            int margin = 12;
            int space = 9;

            int bX = size.Width - (2 * bW + space + margin);
            if (showSaveAsNewButton) bX -= (bW + space);
            int bY = 3;
            _ButtonSave.Bounds = new Rectangle(bX, bY, bW, bH);
            bX += bW + space;
            if (showSaveAsNewButton)
            {
                _ButtonSaveAs.Bounds = new Rectangle(bX, bY, bW, bH);
                bX += bW + space;
            }
            _ButtonCancel.Bounds = new Rectangle(bX, bY, bW, bH);
        }
        /// <summary>
        /// Inicializace datového panelu
        /// </summary>
        protected void InitData()
        {
            _CurrentGraphInfo = new GraphInfo();
            _GraphPanel = new GraphPanel() { Dock = DockStyle.Fill, ParentForm = this, CurrentGraphInfo = _CurrentGraphInfo };
            _GraphPanel.FirstDataChanged += _GraphPanel_FirstDataChanged;
            _MainSplitContainer.Panel1.Controls.Add(_GraphPanel);
        }
        /// <summary>
        /// Poté, kdy uživatel změnil data v definici grafu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _GraphPanel_FirstDataChanged(object sender, EventArgs e)
        {
            RefreshButtons();
        }
        /// <summary>
        /// Panel s daty obsahuje jakékoli změny?
        /// </summary>
        public bool ContainChanges { get { return _GraphPanel?.ContainChanges ?? false; } }
        protected void InitChart()
        {
            _ChartControl = new DevExpress.XtraCharts.ChartControl()
            {
                Dock = DockStyle.Fill
            };
            _MainSplitContainer.Panel2.Controls.Add(_ChartControl);
        }
        DXE.SplitContainerControl _MainSplitContainer;
        DXE.PanelControl _ButtonPanel;
        private DXE.SimpleButton _ButtonSave;
        private DXE.SimpleButton _ButtonSaveAs;
        private DXE.SimpleButton _ButtonCancel;
        GraphPanel _GraphPanel;
        DevExpress.XtraCharts.ChartControl _ChartControl;
        #endregion
        #region Persistence rozměrů formuláře a layoutu splitterů
        /// <summary>
        /// Obnoví rozměry formuláře z konfigurace
        /// </summary>
        /// <param name="position"></param>
        /// <param name="parentBounds"></param>
        private void RestoreFormBoundsToConfig(int[] position, Rectangle? parentBounds)
        {
            this.SetFormPosition(position);
            if (this.WindowState != FormWindowState.Maximized && parentBounds.HasValue)
                this.Bounds = this.Bounds.AlignTo(parentBounds.Value);
        }
        /// <summary>
        /// Po změně umístění nebo velikosti okna - uložíme Position do configu
        /// </summary>
        protected void StoreFormBoundsToConfig()
        {
            if (!IsShown) return;
            Data.App.Config.EditFormPosition = this.GetFormPosition();
        }
        /// <summary>
        /// Z konfigurace načte pozice splitterů a vepíše je do nich
        /// </summary>
        private void RestoreSplitters()
        {
            // Pole Int32 hodnot, které reprezentují pozice splitterů:
            var configLayout = Data.App.Config.EditFormLayout;

            // 1. Main SplitContainer
            _MainSplitContainer.SplitterPosition = GetSplitterPosition(configLayout, 0, 200, _MainSplitContainer.Width * 3 / 10);
            _MainSplitContainer.SplitterPositionChanged += _AnySplitterPositionChanged;
        }
        /// <summary>
        /// Vrátí požadovanou hodnotu z pole konfigurace (pokud existuje), nejméně minValue, nebo defaultValue
        /// </summary>
        /// <param name="configLayout"></param>
        /// <param name="index"></param>
        /// <param name="minValue"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private int GetSplitterPosition(int[] configLayout, int index, int minValue, int defaultValue)
        {
            if (configLayout != null && index < configLayout.Length && configLayout[index] > minValue) return configLayout[index];
            return defaultValue;
        }
        /// <summary>
        /// Po změně pozice kteréhokoli splitteru se uloží layout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _AnySplitterPositionChanged(object sender, EventArgs e)
        {
            StoreSplitters();
        }
        /// <summary>
        /// Posbírá layout (pozice splitterů) a uloží je do konfigurace
        /// </summary>
        private void StoreSplitters()
        {
            List<int> configLayout = new List<int>();

            configLayout.Add(_MainSplitContainer.SplitterPosition);

            Data.App.Config.EditFormLayout = configLayout.ToArray();
        }
        #endregion
        #region Data grafu
        /// <summary>
        /// 
        /// </summary>
        public DatabaseInfo Database
        {
            get { return _Database; }
            set
            {
                _Database = value;
                if (_Database != null)
                    this._DataRefresh();
            }
        }
        private DatabaseInfo _Database;
        /// <summary>
        /// Definice grafu.
        /// Lze setovat, lze číst.
        /// Setováním instance do této property se její data obrazí v editoru.
        /// Vždy je vrácena plně izolovaná instance. Nemá význam v ní něco měnit, v editoru se to neprojeví.
        /// Pro přenos vlastního obsahu definice grafu se používá property <see cref="GraphInfo.Serial"/>.
        /// </summary>
        public GraphInfo CurrentGraphInfo
        {
            get { return GraphInfo.LoadFromSerial(_CurrentGraphInfo.Serial); }
            set
            {
                _CurrentGraphInfo.Serial = value?.Serial ?? "";
                this._DataRefresh();
            }
        }
        /// <summary>
        /// Serializovaná data grafu
        /// </summary>
        public string CurrentGraphInfoSerial
        {
            get { return _CurrentGraphInfo.Serial; }
            set
            {
                _CurrentGraphInfo.Serial = value ?? "";
                this._DataRefresh();
            }
        }
        private GraphInfo _CurrentGraphInfo;
        private void _DataRefresh()
        {
            _GraphPanel.DataRefresh();
        }
        #endregion
    }
    /// <summary>
    /// Panel pro zobrazení definice grafu a jeho úpravu
    /// </summary>
    public class GraphPanel : DXE.PanelControl
    {
        #region Konstruktor
        public GraphPanel()
        {
            InitDevExpressComponents();
        }
        private void InitDevExpressComponents()
        {
            this.AutoSize = true;
            this.InitStyles();

            this.Controls.Add(CreateControlForFrames());
            _GraphHeaderDetailHost.Controls.Add(CreateControlForHeaderDetail());
            _GraphSeriesNewHost.Controls.Add(CreateControlForNewSeries());
            _GraphSeriesListHost.Controls.Add(CreateControlForSeriesList());
            _GraphSeriesDetailHost.Controls.Add(CreateControlForSeriesDetail());

            this.SizeChanged += Frame_SizeChanged;
        }
        private void InitStyles()
        {
            _TitleStyle = new DXE.StyleController();
            _TitleStyle.Appearance.FontSizeDelta = 2;
            _TitleStyle.Appearance.FontStyleDelta = FontStyle.Regular;
            _TitleStyle.Appearance.Options.UseBorderColor = false;
            _TitleStyle.Appearance.Options.UseBackColor = false;
            _TitleStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            _InputStyle = new DXE.StyleController();
            _InputStyle.Appearance.FontSizeDelta = 1;
            _InputStyle.Appearance.FontStyleDelta = FontStyle.Bold;
            _InputStyle.Appearance.Options.UseBorderColor = false;
            _InputStyle.Appearance.Options.UseBackColor = false;
            _InputStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            _LabelStyle = new DXE.StyleController();
            _LabelStyle.Appearance.FontSizeDelta = 1;
            _LabelStyle.Appearance.FontStyleDelta = FontStyle.Italic;
            _LabelStyle.Appearance.Options.UseBorderColor = false;
            _LabelStyle.Appearance.Options.UseBackColor = false;
            _LabelStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;
        }
        DXE.StyleController _TitleStyle;
        DXE.StyleController _InputStyle;
        DXE.StyleController _LabelStyle;
        const int DetailXLabel = 16;
        const int DetailXText = 12;
        const int DetailYFirst = 9;
        const int DetailYHeightLabel = 19;
        const int DetailYHeightText = 22;
        const int DetailYSpaceLabel = 2;
        const int DetailYSpaceText = 3;
        #endregion
        #region Rozvržení layoutu (SplitContainery a TabContainer)
        private WF.Control CreateControlForFrames()
        {
            // Main SplitContainer, dva panely nad sebou:
            _GraphSplitContainer = new DXE.SplitContainerControl()
            {
                FixedPanel = DXE.SplitFixedPanel.Panel1,
                Horizontal = false,
                PanelVisibility = DXE.SplitPanelVisibility.Both,
                Size = new Size(600, 400),
                SplitterPosition = 200,
                Dock = DockStyle.Fill,
                ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True
            };
            _GraphSplitContainer.Panel1.MinSize = 150;
            _GraphSplitContainer.Panel2.MinSize = 250;

            // Horní panel Main SplitContaineru obsadí TabPage se záložkou pro hlavičku a druhou záložkou pro nové položky (obce a datové typy):
            _TabContainer = new DXT.XtraTabControl()
            {
                BorderStyle = DXE.Controls.BorderStyles.NoBorder,
                Dock = DockStyle.Fill
            };
            _TabContainer.AppearancePage.Header.FontSizeDelta = 2;
            _TabPage1 = new DXT.XtraTabPage() { Text = "Společná data grafu" };
            _TabContainer.TabPages.Add(_TabPage1);
            _TabPage2 = new DXT.XtraTabPage() { Text = "Zadání nových datových zdrojů" };
            _TabContainer.TabPages.Add(_TabPage2);

            _GraphSplitContainer.Panel1.Controls.Add(_TabContainer);

            // Dolní panel Main SplitContaineru obsadí sekundární SplitContainer, obsahující nahoře v Panel1 = Grid (Seznam položek), a v Panel2 = Detail jedné položky:
            _SeriesSplitContainer = new DXE.SplitContainerControl()
            {
                FixedPanel = DXE.SplitFixedPanel.Panel2,
                IsSplitterFixed = true,
                Horizontal = false,
                PanelVisibility = DXE.SplitPanelVisibility.Both,
                SplitterPosition = 250,
                Dock = DockStyle.Fill,
                ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True
            };
            _SeriesSplitContainer.ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.False;
            _SeriesSplitContainer.Panel1.MinSize = 150;
            _SeriesSplitContainer.Panel2.MinSize = 200;

            _GraphSplitContainer.Panel2.Controls.Add(_SeriesSplitContainer);

            return _GraphSplitContainer;
        }
        private void Frame_SizeChanged(object sender, EventArgs e)
        {
            Size size = this.ClientSize;
            _TabContainer.Bounds = new Rectangle(0, 0, size.Width, size.Height);
        }
        DXE.SplitContainerControl _GraphSplitContainer;
        DXT.XtraTabControl _TabContainer;
        DXT.XtraTabPage _TabPage1;
        DXT.XtraTabPage _TabPage2;
        DXE.SplitContainerControl _SeriesSplitContainer;
        WF.Control _GraphHeaderDetailHost { get { return _TabPage1; } }
        WF.Control _GraphSeriesNewHost { get { return _TabPage2; } }
        WF.Control _GraphSeriesListHost { get { return _SeriesSplitContainer.Panel1; } }
        WF.Control _GraphSeriesDetailHost { get { return _SeriesSplitContainer.Panel2; } }
        int _GraphSeriesDetailHeight { get { return _SeriesSplitContainer.SplitterPosition; } set { _SeriesSplitContainer.SplitterPosition = value; } }
        #endregion
        #region Hlavička grafu (Panel uvnitř TabPage0, obsahuje Texty a Checkboxy)
        private WF.Control CreateControlForHeaderDetail()
        {
            _HeaderDetailPanel = new DXE.PanelControl() { Dock = DockStyle.Fill, BorderStyle = DXE.Controls.BorderStyles.NoBorder };

            int y = DetailYFirst;
            _HeaderDetailTitleLabel = CreateDxeLabel(DetailXLabel, ref y, 320, _HeaderDetailPanel, "Název celého grafu");
            _HeaderDetailDescriptionLabel = CreateDxeLabel(DetailXLabel + 390, ref y, 320, _HeaderDetailPanel, "Detailní popisek obsahu grafu", shiftY: true);

            _HeaderDetailTitleText = CreateDxeTextEdit(DetailXText, ref y, 375, _HeaderDetailPanel, _HeaderValueChanged);
            _HeaderDetailDescriptionText = CreateDxeMemoEdit(DetailXText + 390, ref y, 375, 135, _HeaderDetailPanel, _HeaderValueChanged);
            y = _HeaderDetailTitleText.Bounds.Bottom + DetailYSpaceText;

            _HeaderDetailTimeTypeLabel = CreateDxeLabel(DetailXLabel, ref y, 320, _HeaderDetailPanel, Text = "Časové omezení dat grafu", shiftY: true);
            _HeaderDetailTimeTypeCombo = CreateDxeImageComboBox(DetailXText, ref y, 200, _HeaderDetailPanel, _HeaderDetailTimeTypeCombo_SelectedIndexChanged, "Bez omezení\tPosledních několik měsíců\tPosledních několik dnů\tPřesně daný interval Od-Do");

            _HeaderDetailTimeLastMonthsText = CreateDxeSpinEdit(DetailXText + 210, ref y, 65, _HeaderDetailPanel, _HeaderValueChanged, 1m, 120m, 1m, "##0", DXE.Controls.SpinStyles.Vertical, visible: false);
            _HeaderDetailTimeLastDaysText = CreateDxeSpinEdit(DetailXText + 210, ref y, 65, _HeaderDetailPanel, _HeaderValueChanged, 1m, 3650m, 7m, "# ##0", DXE.Controls.SpinStyles.Vertical, visible: false);
            _HeaderDetailTimeRangeBeginText = CreateDxeTextEdit(DetailXText + 210, ref y, 80, _HeaderDetailPanel, _HeaderValueChanged, maskType: DXE.Mask.MaskType.DateTimeAdvancingCaret, editMask: "d", useMaskAsDisplayFormat: true, visible: false);
            _HeaderDetailTimeRangeEndText = CreateDxeTextEdit(DetailXText + 295, ref y, 80, _HeaderDetailPanel, _HeaderValueChanged, maskType: DXE.Mask.MaskType.DateTimeAdvancingCaret, editMask: "d", useMaskAsDisplayFormat: true, visible: false, shiftY: true);

            _HeaderDetailTimeStripesCheck = CreateDxeCheckEdit(DetailXText, ref y, 350, _HeaderDetailPanel, "Zobrazovat význačné časové intervaly", _HeaderValueChanged, checkBoxStyle: DXE.Controls.CheckBoxStyle.SvgToggle1, borderStyles: DXE.Controls.BorderStyles.NoBorder, shiftY: true);
            _HeaderDetailTimeZoomCheck = CreateDxeCheckEdit(DetailXText, ref y, 350, _HeaderDetailPanel, "Povolit zoom na časové ose", _HeaderValueChanged, checkBoxStyle: DXE.Controls.CheckBoxStyle.SvgToggle1, borderStyles: DXE.Controls.BorderStyles.NoBorder, shiftY: true);
            _HeaderDetailAxisOnRightCheck = CreateDxeCheckEdit(DetailXText, ref y, 350, _HeaderDetailPanel, "Svislá osa vpravo", _HeaderValueChanged, checkBoxStyle: DXE.Controls.CheckBoxStyle.SvgToggle1, borderStyles: DXE.Controls.BorderStyles.NoBorder, shiftY: true);

            return _HeaderDetailPanel;
        }
        /// <summary>
        /// Po změně předvolby v ComboBoxu "Časové omezení dat grafu" se nastaví odpovídající viditelnost
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _HeaderDetailTimeTypeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = _HeaderDetailTimeTypeCombo.SelectedIndex;
            _HeaderDetailTimeLastMonthsText.Visible = (index == 1);
            _HeaderDetailTimeLastDaysText.Visible = (index == 2);
            _HeaderDetailTimeRangeBeginText.Visible = (index == 3);
            _HeaderDetailTimeRangeEndText.Visible = (index == 3);
        }
        /// <summary>
        /// Po změně jakéhokoli údaje v hlavičce se uloží data z GUI do datového objektu grafu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _HeaderValueChanged(object sender, EventArgs e)
        {
            HeaderDetailStore();
            ContainChanges = true;
        }
        /// <summary>
        /// Načte data hlavičky z datového objektu do GUI
        /// </summary>
        private void HeaderDetailRefresh()
        {
            bool oldDRR = _DataRefreshRunning;
            _DataRefreshRunning = true;

            var graph = this.CurrentGraphInfo;
            _HeaderDetailTitleText.Text = graph.Title;
            _HeaderDetailDescriptionText.Text = graph.Description;

            int timeIndex = (graph.TimeRangeLastMonths.HasValue ? 1 :
                    (graph.TimeRangeLastDays.HasValue ? 2 :
                    ((graph.TimeRangeBegin.HasValue || graph.TimeRangeEnd.HasValue) ? 3 : 0)));
            _HeaderDetailTimeTypeCombo.SelectedIndex = timeIndex;
            _HeaderDetailTimeLastMonthsText.EditValue = (graph.TimeRangeLastMonths.HasValue ? graph.TimeRangeLastMonths.Value : (int?)null);
            _HeaderDetailTimeLastDaysText.EditValue = (graph.TimeRangeLastDays.HasValue ? graph.TimeRangeLastDays.Value : (int?)null);
            _HeaderDetailTimeLastMonthsText.Value = (graph.TimeRangeLastMonths.HasValue ? graph.TimeRangeLastMonths.Value : 0);
            _HeaderDetailTimeLastDaysText.Value = (graph.TimeRangeLastDays.HasValue ? graph.TimeRangeLastDays.Value : 0);
            // textboxy:  _HeaderDetailTimeLastMonthsText.EditValue = (graph.TimeRangeLastMonths.HasValue ? graph.TimeRangeLastMonths.Value : (int?)null);
            // textboxy:  _HeaderDetailTimeLastDaysText.EditValue = (graph.TimeRangeLastDays.HasValue ? graph.TimeRangeLastDays.Value : (int?)null);
            _HeaderDetailTimeRangeBeginText.EditValue = (graph.TimeRangeBegin.HasValue ? graph.TimeRangeBegin.Value : (DateTime?)null);
            _HeaderDetailTimeRangeEndText.EditValue = (graph.TimeRangeEnd.HasValue ? graph.TimeRangeEnd.Value : (DateTime?)null);

            _HeaderDetailTimeStripesCheck.Checked = graph.EnableCommonTimeStripes;
            _HeaderDetailTimeZoomCheck.Checked = graph.ChartEnableTimeZoom;
            _HeaderDetailAxisOnRightCheck.Checked = graph.ChartAxisYRight;

            _DataRefreshRunning = oldDRR;
        }
        /// <summary>
        /// Uloží data hlavičky z GUI do datového objektu
        /// </summary>
        private void HeaderDetailStore()
        {
            if (_DataRefreshRunning) return;               // Probíhá HeaderDetailRefresh(), mění se hodnoty v UI, volají se eventy Changed => nesmím provádět Store, nemá to smysl !!!

            var graph = this.CurrentGraphInfo;
            graph.Title = _HeaderDetailTitleText.Text;
            graph.Description = _HeaderDetailDescriptionText.Text;

            int timeIndex = _HeaderDetailTimeTypeCombo.SelectedIndex;
            graph.TimeRangeLastMonths = ((timeIndex == 1 && _HeaderDetailTimeLastMonthsText.Value > 0m) ? (int?)_HeaderDetailTimeLastMonthsText.Value : (int?)null);
            graph.TimeRangeLastDays = ((timeIndex == 2 && _HeaderDetailTimeLastDaysText.Value > 0m) ? (int?)_HeaderDetailTimeLastDaysText.Value : (int?)null);
            // textboxy:  graph.TimeRangeLastMonths = (timeIndex == 1 ? GetValue<int>(_HeaderDetailTimeLastMonthsText.EditValue) : (int?)null);
            // textboxy:  graph.TimeRangeLastDays = (timeIndex == 2 ? GetValue<int>(_HeaderDetailTimeLastDaysText.EditValue) : (int?)null);
            graph.TimeRangeBegin = (timeIndex == 3 ? GetValue<DateTime>(_HeaderDetailTimeRangeBeginText.EditValue) : (DateTime?)null);
            graph.TimeRangeEnd = (timeIndex == 3 ? GetValue<DateTime>(_HeaderDetailTimeRangeEndText.EditValue) : (DateTime?)null);

            graph.EnableCommonTimeStripes = _HeaderDetailTimeStripesCheck.Checked;
            graph.ChartEnableTimeZoom = _HeaderDetailTimeZoomCheck.Checked;
            graph.ChartAxisYRight = _HeaderDetailAxisOnRightCheck.Checked;
        }
        /// <summary>
        /// Vrátí hodnotu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="editValue"></param>
        /// <returns></returns>
        private static T? GetValue<T>(object editValue) where T : struct
        {
            if (editValue == null) return null;
            if (!(editValue is T)) return null;
            return (T)editValue;
        }
        DXE.PanelControl _HeaderDetailPanel;
        DXE.LabelControl _HeaderDetailTitleLabel;
        DXE.LabelControl _HeaderDetailDescriptionLabel;
        DXE.TextEdit _HeaderDetailTitleText;
        DXE.TextEdit _HeaderDetailDescriptionText;
        DXE.LabelControl _HeaderDetailTimeTypeLabel;
        DXE.ImageComboBoxEdit _HeaderDetailTimeTypeCombo;
        DXE.SpinEdit _HeaderDetailTimeLastMonthsText;
        DXE.SpinEdit _HeaderDetailTimeLastDaysText;
        // DXE.TextEdit _HeaderDetailTimeLastMonthsText;
        // DXE.TextEdit _HeaderDetailTimeLastDaysText;
        DXE.TextEdit _HeaderDetailTimeRangeBeginText;
        DXE.TextEdit _HeaderDetailTimeRangeEndText;
        DXE.CheckEdit _HeaderDetailTimeStripesCheck;
        DXE.CheckEdit _HeaderDetailTimeZoomCheck;
        DXE.CheckEdit _HeaderDetailAxisOnRightCheck;
        #endregion
        #region Panel pro zadání nových serií (Panel uvnitř TabPage1, obsahuje Entity + ValueType)
        private WF.Control CreateControlForNewSeries()
        {
            _SeriesNewPanel = new DXE.PanelControl() { Dock = DockStyle.Fill, BorderStyle = DXE.Controls.BorderStyles.NoBorder };

            _SeriesNewSplitContainer = new DXE.SplitContainerControl()
            {
                FixedPanel = DXE.SplitFixedPanel.None,
                Horizontal = true,
                PanelVisibility = DXE.SplitPanelVisibility.Both,
                Size = new Size(600, 300),
                SplitterPosition = 300,
                Dock = DockStyle.Fill,
                ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True
            };
            _SeriesNewSplitContainer.Panel1.MinSize = 150;
            _SeriesNewSplitContainer.Panel2.MinSize = 100;
            _SeriesNewSplitContainer.SizeChanged += _SeriesNewSplitContainer_SizeChanged;
            _SeriesNewPanel.Controls.Add(_SeriesNewSplitContainer);

            _SeriesNewSplitContainer.Panel1.Controls.Add(CreateControlForEntities());
            _SeriesNewSplitContainer.Panel2.Controls.Add(CreateControlForValueTypes());

            return _SeriesNewPanel;
        }
        private void _SeriesNewSplitContainer_SizeChanged(object sender, EventArgs e)
        {
        }
        /// <summary>
        /// V rámci Refreshe dat (=načtení z dat do GUI) označíme ty prvky, které jsou ve stávajícím grafu obsaženy
        /// </summary>
        protected void SeriesNewRefreshData()
        {
            SeriesNewEntityRefreshData();
            SeriesNewValueTypeRefreshData();
        }
        private DXE.PanelControl _SeriesNewPanel;
        private DXE.SplitContainerControl _SeriesNewSplitContainer;
        #endregion
        #region Seznam s obcemi = zdroj entit (Text + List)
        private WF.Control CreateControlForEntities()
        {
            _EntityPanel = new DXE.PanelControl() { Dock = DockStyle.Fill, BorderStyle = DXE.Controls.BorderStyles.NoBorder };
            this.Controls.Add(_EntityPanel);

            _EntitySearchLabel = new DXE.LabelControl() { Text = "Vyhledat obec:" };
            _EntitySearchLabel.StyleController = _TitleStyle;
            _EntityPanel.Controls.Add(_EntitySearchLabel);

            _EntitySearchText = new DXE.TextEdit() { EnterMoveNextControl = true };
            _EntitySearchText.SuperTip = new DevExpress.Utils.SuperToolTip();
            _EntitySearchText.SuperTip.Items.AddTitle("Vyhledat obec:");
            _EntitySearchText.SuperTip.Items.Add(@"Zadejte počátek názvu, budou nabídnuty všechny obce s tímto začátkem.
Zadejte hvězdičku a část názvu, budou nalezeny všechny obce obsahující ve jménu daný text.
Zadejte na začátek textu výraz kraj: (nebo okres: nebo město: nebo obec:), a budou vypsány pouze odpovídající jednotky.
Po zadání tohoto prefixu nemusíte psát další text, budou vypsány všechny kraje (okresy, města, obce).

Následně si vyberete pouze patřičné obce ze seznamu.");
            _EntitySearchText.KeyUp += _EntitySearchText_KeyUp;
            _EntityPanel.Controls.Add(_EntitySearchText);
            _EntityLastSearchText = "";

            _EntityListBox = new DXE.ListBoxControl()
            {
                Bounds = new Rectangle(3, 50, 240, 300),
                MultiColumn = false,
                SelectionMode = SelectionMode.MultiExtended,
                Dock = DockStyle.None
            };
            _EntityListBox.Appearance.FontSizeDelta = 1;
            _EntityListBox.SelectedIndex = 0;
            _EntityPanel.Controls.Add(_EntityListBox);

            int fontheight = _EntityListBox.Appearance.GetFont().Height;
            _EntityListBox.ItemAutoHeight = false;
            _EntityListBox.ItemHeight = fontheight + 4;

            this._EntityPanelLayout();

            this._EntityPanel.SizeChanged += _EntityPanel_SizeChanged;

            return _EntityPanel;
        }
        /// <summary>
        /// V rámci Refreshe dat (=načtení z dat do GUI) zobrazíme ty entity, které jsou ve stávajícím grafu obsaženy
        /// </summary>
        protected void SeriesNewEntityRefreshData()
        {
            string[] entityCodes = null;
            if (this.CurrentGraphInfo != null && this.CurrentGraphInfo.Series != null)
                entityCodes = this.CurrentGraphInfo.Series.Select(s => s.EntityFullCode).Distinct().ToArray();
            if (entityCodes == null || entityCodes.Length == 0) return;

            var entites = this.Database.GetEntities(entityCodes);
            if (entites == null || entites.Length == 0) return;

            this._EntityListBox.Items.AddRange(entites);
            if (!this._EntityListBox.Enabled)
                this._EntityListBox.Enabled = true;
            this._EntityListBox.SelectAll();
        }
        private void _EntityPanel_SizeChanged(object sender, EventArgs e)
        {
            this._EntityPanelLayout();
        }
        private void _EntitySearchText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Enter)
                _EntityListBox.Focus();
            else if (e.KeyCode == Keys.Home || e.KeyCode == Keys.End || e.KeyCode == Keys.Up /* || e.KeyCode == Keys.Down */ || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Tab || e.KeyCode == Keys.Escape)
            { }
            else if (e.Modifiers == Keys.Control)
            { }
            else
                EntitySearchRun();
        }
        private void _EntityPanelLayout()
        {
            if (_EntityListBox == null) return;

            int mx = 3;
            int my = 3;
            int sy = 2;
            int ty = 4;
            Size size = _EntityPanel.ClientSize;
            int inpWidth = size.Width - mx - mx;
            _EntitySearchLabel.Bounds = new Rectangle(mx, my, inpWidth, 20);
            _EntitySearchText.Bounds = new Rectangle(mx, _EntitySearchLabel.Bounds.Bottom + ty, inpWidth, 25);
            int inpBottom = _EntitySearchText.Bounds.Bottom;

            _EntityListBox.Bounds = new Rectangle(mx, inpBottom + sy, size.Width - mx - mx, size.Height - my - inpBottom - sy);
        }
        private void EntitySearchRun()
        {
            if (this.Database == null) return;

            string newText = _EntitySearchText.Text.Trim();
            string oldText = _EntityLastSearchText;
            if (String.Equals(newText, oldText, StringComparison.CurrentCultureIgnoreCase)) return;
            _EntityLastSearchText = newText;

            this._EntityListBox.Items.Clear();
            if (newText.Length < 2) return;

            var entites = this.Database.SearchEntities(newText);
            if (entites.Length > 0)
            {
                this._EntityListBox.Items.AddRange(entites);
                if (!this._EntityListBox.Enabled)
                    this._EntityListBox.Enabled = true;
            }
            else
            {
                this._EntityListBox.Items.Add("Nenalezeno");
                if (this._EntityListBox.Enabled)
                    this._EntityListBox.Enabled = false;
            }
        }
        private string _EntityLastSearchText;
        private DXE.PanelControl _EntityPanel;
        private DXE.LabelControl _EntitySearchLabel;
        private DXE.TextEdit _EntitySearchText;
        private DXE.ListBoxControl _EntityListBox;
        #endregion
        #region Seznam s datovými typy = zdroj dat (List + Info)
        private WF.Control CreateControlForValueTypes()
        {
            _ValueTypePanel = new DXE.PanelControl() { Dock = DockStyle.Fill, BorderStyle = DXE.Controls.BorderStyles.NoBorder };
            _ValueTypePanel.SizeChanged += _ValueTypePanel_SizeChanged;

            _ValueTypeLabel = new DXE.LabelControl() { Text = "Označte jeden nebo více typů dat:", BorderStyle = DXE.Controls.BorderStyles.NoBorder };
            _ValueTypeLabel.StyleController = _TitleStyle;
            _ValueTypePanel.Controls.Add(_ValueTypeLabel);

            _ValueTypeInfos = DataValueTypeInfo.CreateAll();

            _ValueTypeListBox = new DXE.ListBoxControl()
            {
                MultiColumn = false,
                SelectionMode = SelectionMode.MultiExtended,
                Dock = DockStyle.None
            };
            _ValueTypeListBox.Appearance.FontSizeDelta = 1;
            _ValueTypeListBox.SelectedIndex = 0;
            _ValueTypeListBox.DataSource = _ValueTypeInfos;
            _ValueTypeListBox.SelectedValueChanged += _ValueTypeListBox_SelectedValueChanged;

            _ValueTypePanel.Controls.Add(_ValueTypeListBox);

            int fontheight = _ValueTypeListBox.Appearance.GetFont().Height;
            _ValueTypeListBox.ItemAutoHeight = false;
            _ValueTypeListBox.ItemHeight = fontheight + 4;

            _ValueTypeInfo = new DXE.LabelControl() { Text = "", AutoSizeMode = DXE.LabelAutoSizeMode.Vertical, BorderStyle = DXE.Controls.BorderStyles.NoBorder };
            _ValueTypeInfo.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            _ValueTypeInfo.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
            _ValueTypeInfo.Appearance.Options.UseTextOptions = true;
            _ValueTypePanel.Controls.Add(_ValueTypeInfo);

            _ValueTypeLayout();
            return _ValueTypePanel;
        }
        /// <summary>
        /// V rámci Refreshe dat (=načtení z dat do GUI) označíme ty datové řady, které jsou ve stávajícím grafu obsaženy
        /// </summary>
        protected void SeriesNewValueTypeRefreshData()
        {
            if (_ValueTypeListBox == null) return;

            DataValueType[] usedValueTypes = null;
            if (this.CurrentGraphInfo != null && this.CurrentGraphInfo.Series != null)
                usedValueTypes = this.CurrentGraphInfo.Series.Select(s => s.ValueType).Distinct().ToArray();

            // _ValueTypeListBox nepoužívá Items, ale DataSource = _ValueTypeInfos:
            int count = _ValueTypeInfos.Length;
            for (int i = 0; i < count; i++)
            {
                DataValueTypeInfo info = _ValueTypeInfos[i];
                bool isSelected = usedValueTypes.Any(t => t == info.Value);
                _ValueTypeListBox.SetSelected(i, isSelected);
            }

            var selectedItems = _ValueTypeListBox.SelectedItems;
            var valueItems = _ValueTypeListBox.SelectedItems.OfType<DataValueTypeInfo>().ToArray();

            _ValueTypeListBox.Refresh();
        }
        private void _ValueTypeListBox_SelectedValueChanged(object sender, EventArgs e)
        {
            string text = "";
            if (_ValueTypeListBox.SelectedItem is DataValueTypeInfo info)
                text = info.ToolTip;
            _ValueTypeInfo.Text = text;
        }
        private void _ValueTypePanel_SizeChanged(object sender, EventArgs e)
        {
            _ValueTypeLayout();
        }
        private void _ValueTypeLayout()
        {
            if (_ValueTypeInfo == null) return;

            Size size = _ValueTypePanel.ClientSize;
            int mx = 3;
            int my = 3;
            int sx = 2;
            int sy = 2;
            int ty = 4;
            int x = mx;
            int w = size.Width - mx - mx;

            int y = my;
            _ValueTypeLabel.Bounds = new Rectangle(x, y, w, 20); y = _ValueTypeLabel.Bounds.Bottom + ty;

            int ih = (int)(3f * _ValueTypeInfo.Font.GetHeight());
            int iy = size.Height - my - ih;
            _ValueTypeInfo.Bounds = new Rectangle(x, iy, w, ih);

            int lh = iy - sy - y;
            _ValueTypeListBox.Bounds = new Rectangle(x, y, w, lh);
        }
        private DXE.PanelControl _ValueTypePanel;
        private DXE.LabelControl _ValueTypeLabel;
        private DXE.ListBoxControl _ValueTypeListBox;
        private DXE.LabelControl _ValueTypeInfo;
        private DataValueTypeInfo[] _ValueTypeInfos;
        #endregion
        #region Seznam existujících serií (Grid)
        private WF.Control CreateControlForSeriesList()
        {
            _SeriesListPanel = new DXE.PanelControl() { Dock = DockStyle.Fill, BorderStyle = DXE.Controls.BorderStyles.NoBorder };

            SeriesListInitViewData();

            _SeriesListGrid = new DXG.GridControl() { Dock = DockStyle.Fill };
            _SeriesListGrid.ViewCollection.AddRange(new DXG.Views.Base.BaseView[] { _SeriesListGridView });
            _SeriesListGrid.MainView = _SeriesListGridView;
            _SeriesListGridView.GridControl = _SeriesListGrid;
            _SeriesListGridView.FocusedRowChanged += _SeriesListGridView_FocusedRowChanged;

            _SeriesListPanel.Controls.Add(_SeriesListGrid);


            _SeriesListGrid.DataSource = _SeriesListData; //  GraphSerieGridRow.GetDataGraph();


            _SeriesListButtonPanel = new DXE.PanelControl() { Dock = DockStyle.Top, BorderStyle = DXE.Controls.BorderStyles.NoBorder, Height = GraphForm.DefaultButtonPanelHeight };
            _SeriesListPanel.Controls.Add(_SeriesListButtonPanel);

            _SeriesListAddButton = new DXE.SimpleButton() { Text = "Přidej řádky", Size = new Size(GraphForm.DefaultButtonWidth, GraphForm.DefaultButtonHeight), Location = new Point(8, 3) };
            _SeriesListAddButton.Click += _SeriesListAddButton_Click;
            _SeriesListButtonPanel.Controls.Add(_SeriesListAddButton);
            _SeriesListRemoveButton = new DXE.SimpleButton() { Text = "Odeber řádky", Size = new Size(GraphForm.DefaultButtonWidth, GraphForm.DefaultButtonHeight), Location = new Point(_SeriesListAddButton.Bounds.Right + 6, 3) };
            _SeriesListRemoveButton.Click += _SeriesListRemoveButton_Click;
            _SeriesListButtonPanel.Controls.Add(_SeriesListRemoveButton);

            SeriesListReloadDataFromGraph(false);

            return _SeriesListPanel;
        }
        /// <summary>
        /// Připraví GridView tak, aby následně správně zobrazovalo data
        /// </summary>
        protected void SeriesListInitViewData()
        {
            _SeriesListData = new List<GraphSerieGridRow>();

            _SeriesListGridView = new DXG.Views.Grid.GridView();
            _SeriesListGridView.OptionsBehavior.AutoPopulateColumns = false;
            _SeriesListGridView.OptionsBehavior.Editable = false;
            _SeriesListGridView.Columns.Add(new DXG.Columns.GridColumn() { FieldName = nameof(GraphSerieGridRow.Title), Caption = "Název dat", Visible = true, Width = 200 });
            _SeriesListGridView.Columns.Add(new DXG.Columns.GridColumn() { FieldName = nameof(GraphSerieGridRow.EntityText), Caption = "Obec/město/okres", Visible = true, Width = 170 });
            _SeriesListGridView.Columns.Add(new DXG.Columns.GridColumn() { FieldName = nameof(GraphSerieGridRow.ValueTypeName), Caption = "Zobrazená data", Visible = true, Width = 150 });
            _SeriesListGridView.Columns.Add(new DXG.Columns.GridColumn() { FieldName = nameof(GraphSerieGridRow.PocetOd), Caption = "Obyvatel OD", Visible = true, Width = 60 });
            _SeriesListGridView.Columns.Add(new DXG.Columns.GridColumn() { FieldName = nameof(GraphSerieGridRow.PocetDo), Caption = "Obyvatel DO", Visible = true, Width = 60 });
            _SeriesListGridView.OptionsView.BestFitMode = DXG.Views.Grid.GridBestFitMode.Fast;


            // _SeriesListGridView.OptionsBehavior.EditingMode = DXG.Views.Grid.GridEditingMode.
            _SeriesListGridView.OptionsBehavior.AllowAddRows = DevExpress.Utils.DefaultBoolean.False;
            _SeriesListGridView.OptionsBehavior.AllowDeleteRows = DevExpress.Utils.DefaultBoolean.False;
            _SeriesListGridView.OptionsBehavior.AllowIncrementalSearch = true;
            _SeriesListGridView.OptionsSelection.MultiSelectMode = DXG.Views.Grid.GridMultiSelectMode.RowSelect;
            _SeriesListGridView.Appearance.HeaderPanel.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;

        }
        /// <summary>
        /// Po změně focusovaného řádku zajistíme zobrazení detailů aktuální serie
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SeriesListGridView_FocusedRowChanged(object sender, DXG.Views.Base.FocusedRowChangedEventArgs e)
        {
            SeriesDetailRefresh();
        }
        /// <summary>
        /// Do lokálního Listu se seriemi <see cref="_SeriesListGrid"/> načte aktuální obsah definice grafu <see cref="CurrentGraphInfo"/>.
        /// Tento List slouží jako datový zdroj pro Grid.
        /// Volitelně vyvolá refresh Gridu.
        /// </summary>
        protected void SeriesListReloadDataFromGraph(bool refreshGui)
        {
            if (_SeriesListData.Count > 0) _SeriesListData.Clear();
            if (this.Database == null || this.CurrentGraphInfo == null) return;

            foreach (var serie in this.CurrentGraphInfo.Series)
                AddSerieToSeriesList(serie);

            if (refreshGui)
                SeriesListRefreshGrid();
        }
        /// <summary>
        /// Danou serii (data z Grafu) přidá do this listu, který je zobrazován v Gridu
        /// </summary>
        /// <param name="serie"></param>
        private void AddSerieToSeriesList(GraphSerieInfo serie)
        {
            GraphSerieGridRow gridRow = GraphSerieGridRow.Create(serie, this.Database);
            if (gridRow != null)
                _SeriesListData.Add(gridRow);
        }
        /// <summary>
        /// Provede vizuální refresh gridu s daty serií
        /// </summary>
        private void SeriesListRefreshGrid()
        {
            _SeriesListGridView.RefreshData();
            _SeriesListGrid.RefreshDataSource();
            _SeriesListGrid.Refresh();
        }
        /// <summary>
        /// Po kliknutí na button Přidej serie
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SeriesListAddButton_Click(object sender, EventArgs e)
        {
            App.TryRun(_SeriesListAddNewSeries);
        }
        /// <summary>
        /// Po kliknutí na button Odeber serii
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SeriesListRemoveButton_Click(object sender, EventArgs e)
        {
            App.TryRun(_SeriesListRemoveSeries);
        }
        /// <summary>
        /// Přidat nové serie do aktuální definice, serie jsou zadány v GUI (<see cref="_EntityListBox"/> a <see cref="_ValueTypeListBox"/>)
        /// </summary>
        private void _SeriesListAddNewSeries()
        {
            var entityItems = _EntityListBox.SelectedItems.OfType<DatabaseInfo.EntityInfo>().ToArray();
            var valueItems = _ValueTypeListBox.SelectedItems.OfType<DataValueTypeInfo>().ToArray();
            if (!_HasValidDataForNewSeries(entityItems, valueItems))
                return;

            var newSerieKeys = _CreateKeysForNewSeries(entityItems, valueItems);
            if (!_HasValidCountForNewSeries(entityItems.Length, valueItems.Length, newSerieKeys.Count))
                return;

            _AddNewSeriesToGraph(newSerieKeys);

            SeriesListRefreshGrid();
            ContainChanges = true;
            ContainSerieChanges = true;

            CheckChartLayoutOnSeriesChange(newSerieKeys.Count);
        }
        /// <summary>
        /// Ověří, zda aktuálně můžeme zadaná data přidat jako nové serie. Vrátí true pokud je vše OK.
        /// Ověří stávající počet datových řad v grafu, ověří aktuálně vybranou záložu v okně (aby uživatel viděl, která data se přidávají),
        /// a ověří zda jsou vybrané entity a datové typy. 
        /// Pokud nejsou data OK, pak uživateli zobrazí varování s informacemi, a vrátí false.
        /// </summary>
        /// <param name="entityItems"></param>
        /// <param name="valueItems"></param>
        /// <returns></returns>
        private bool _HasValidDataForNewSeries(DatabaseInfo.EntityInfo[] entityItems, DataValueTypeInfo[] valueItems)
        {
            int currentCount = this.CurrentGraphInfo.Series.Length;
            if (currentCount >= GraphInfo.MaxSeriesCount)
            {
                string text = $@"Aktuální graf již obsahuje {currentCount} datových řad'. Nelze přidat další řadu, graf by nebyl přehledný.";
                App.ShowWarning(this, text);
                return false;
            }

            int requestPage = 1;
            if (_TabContainer.SelectedTabPageIndex != requestPage)
            {
                _TabContainer.SelectedTabPageIndex = requestPage;
                string text = $@"Na stránce '{_TabContainer.TabPages[requestPage].Text}' najděte místa (obce) a vyberte data pro zobrazení, a pak teprve stiskněte tlačítko '{_SeriesListAddButton.Text}'.";
                App.ShowWarning(this, text);
                return false;
            }

            bool hasEntities = (entityItems != null && entityItems.Length > 0);
            bool hasValues = (valueItems != null && valueItems.Length > 0);
            if (!hasEntities || !hasValues)
            {
                string text = "";
                if (!hasEntities && !hasValues)
                    text = $@"Pro přidání nových prvků do grafu nejprve vlevo nahoře vyberte místa (obce), pro které chcete graf zobrazit. 
Vepište část názvu, zobrazí se seznam odpovídajících míst, označte jedno nebo více míst (klikáním myší s klávesou Ctrl).

Pak v pravé části podobně vyberte jeden nebo více druhů dat, které chcete zobrazit.

Teprve pak klikněte na tlačítko '{_SeriesListAddButton.Text}', budou přidány kombinace všech označených míst a všech označených typů dat.";
                else if (!hasEntities)
                    text = $@"Pro přidání nových prvků do grafu nejprve vlevo nahoře vyberte místa (obce), pro které chcete graf zobrazit. 
Vepište část názvu, zobrazí se seznam odpovídajících míst, označte jedno nebo více míst (klikáním myší s klávesou Ctrl).

Teprve pak klikněte na tlačítko '{_SeriesListAddButton.Text}', budou přidány kombinace všech označených míst a všech označených typů dat.";
                else if (!hasValues)
                    text = $@"Pro přidání nových prvků do grafu (po výběru míst) je třeba vybrat druhy dat pro zobrazení v grafu.
V pravé části vyberte jeden nebo více druhů dat, které chcete zobrazit.

Teprve pak klikněte na tlačítko '{_SeriesListAddButton.Text}', budou přidány kombinace všech označených míst a všech označených typů dat.";

                App.ShowWarning(this.FindForm(), text);
                return false;
            }
            return true;
        }
        /// <summary>
        /// Vytvoří a vrátí všechny nové kombinace vstupních hodnot (entita a typ hodnoty), které dosud nejsou přítomny v aktuálním grafu.
        /// </summary>
        /// <param name="entityItems"></param>
        /// <param name="valueItems"></param>
        /// <returns></returns>
        private IList<Tuple<DatabaseInfo.EntityInfo, DataValueTypeInfo>> _CreateKeysForNewSeries(DatabaseInfo.EntityInfo[] entityItems, DataValueTypeInfo[] valueItems)
        {
            var result = new List<Tuple<DatabaseInfo.EntityInfo, DataValueTypeInfo>>();
            var series = this.CurrentGraphInfo.Series;
            foreach (var entityItem in entityItems)
            {
                string entityCode = entityItem.FullCode;
                foreach (var valueItem in valueItems)
                {   // Máme zde kombinaci požadovaných hodnot entityItem (a její entityCode) plus typ hodnoty valueItem:
                    // Zjistím, zda takovou kombinaci v grafu již máme:
                    bool exists = series.Any(s => s.EntityFullCode == entityCode && s.ValueType == valueItem.Value);
                    if (!exists)
                        result.Add(new Tuple<DatabaseInfo.EntityInfo, DataValueTypeInfo>(entityItem, valueItem));
                }
            }
            return result;
        }
        /// <summary>
        /// Ověří, zda do grafu je možno přidat další serie z hlediska maximálního počtu
        /// </summary>
        /// <param name="entityItems"></param>
        /// <param name="valueItems"></param>
        /// <param name="newSeriesCount"></param>
        /// <returns></returns>
        private bool _HasValidCountForNewSeries(int entityCount, int valueCount, int newSeriesCount)
        {
            if (newSeriesCount == 0)
            {
                App.ShowWarning(this.FindForm(), "Všechny zvolené obce a datové hodnoty jsou v grafu již obsaženy.");
                return false;
            }

            int currentCount = this.CurrentGraphInfo.Series.Length;
            int sumCount = currentCount + newSeriesCount;
            if (sumCount > GraphInfo.MaxSeriesCount)
            {
                string eol = Environment.NewLine;
                string message = $"Pro rozumnou přehlednost grafu není možno do něj vložit více než {GraphInfo.MaxSeriesCount} datových řad.{eol}{eol}";
                if (currentCount > 0)
                {
                    string currentSeriesText = App.GetCountText(currentCount, "není v grafu žádná datová řada", "je v grafu jedna datová řada", "jsou v grafu {0} datové řady", "je v grafu {0} datových řad");
                    string newSeriesText = App.GetCountText(newSeriesCount, "žádné řady", "další jedné řady", "dalších {0} řad", "dalších {0} řad");
                    message += $"Aktuálně {currentSeriesText}, a při požadavku na přidání {newSeriesText} by byl překročen maximální povolený počet.";
                }
                else
                {
                    string valueText = App.GetCountText(valueCount, "žádnou typ dat", "jednoho typu dat", "{0} typů dat", "{0} typů dat");
                    string entitytext = App.GetCountText(entityCount, "žádnou obec", "jednu obec", "{0} obce", "{1} obcí");
                    string newSeriesText = App.GetCountText(newSeriesCount, "žádnou datovou řadu", "jednu datovou řadu", "{0} datové řady", "{0} datových řad");
                    message += $"Zvolili jste přidání {valueText} pro {entitytext}, celkem tedy {newSeriesText}. Zmenšete jejich počet.";
                }
                message += $"{eol}{eol}Zvolte méně dat a zkuste je přidat poté.";
                App.ShowWarning(this.FindForm(), message);
                return false;
            }
            return true;
        }
        /// <summary>
        /// Do aktuálního grafu přidá nové serie podle zadaných klíčů. Neprovádí refresh Gridu.
        /// </summary>
        /// <param name="newSerieKeys"></param>
        private void _AddNewSeriesToGraph(IEnumerable<Tuple<DatabaseInfo.EntityInfo, DataValueTypeInfo>> newSerieKeys)
        {
            foreach (var newSerieKey in newSerieKeys)
            {
                var entity = newSerieKey.Item1;
                var valueType = newSerieKey.Item2;
                string title = entity.Nazev + ", " + valueType.ShortText;
                GraphSerieInfo serie = new GraphSerieInfo() { Title = title, EntityFullCode = entity.FullCode, ValueType = valueType.Value };
                this.CurrentGraphInfo.AddSerie(serie);
                AddSerieToSeriesList(serie);
            }
        }
        /// <summary>
        /// Odebere aktuální serii
        /// </summary>
        private void _SeriesListRemoveSeries()
        {
            var row = CurrentSerieGridRow;
            var serie = row?.Serie;
            if (serie == null) return;

            if (!App.ShowQuestionYN(this.FindForm(), $"Odebrat řádek '{serie.Title}' ?")) return;

            int rowIndex = _SeriesListData.FindIndex(r => Object.ReferenceEquals(r, row));
            if (rowIndex >= 0)
                _SeriesListData.RemoveAt(rowIndex);

            var graph = CurrentGraphInfo;
            graph.RemoveSerie(serie);
            ContainChanges = true;
            ContainSerieChanges = true;

            SeriesListRefreshGrid();
            SeriesDetailRefresh();

            CheckChartLayoutOnSeriesChange(-1);
        }
        /// <summary>
        /// Metoda je volaná po přidání / odebrání datových řad z grafu.
        /// Pokud má graf uložený layout, nabídne se uživateli jeho odstranění včetně důvodu. Pouze při první změně.
        /// </summary>
        /// <param name="seriesDiff"></param>
        private void CheckChartLayoutOnSeriesChange(int seriesDiff)
        {
            if (seriesDiff == 0) return;

            var graph = CurrentGraphInfo;
            string xmlLayout = graph?.ChartLayout;
            if (String.IsNullOrEmpty(xmlLayout)) return;                       // Graf NEMÁ speciální úpravený layout

            if (seriesDiff > 0 && ChartLayoutAddSeriesWarningShown) return;    // Graf sice má upravený vzhled, ale my už jsme uživatele po PŘIDÁNÍ nových series varovali...
            if (seriesDiff < 0 && ChartLayoutRemoveSeriesWarningShown) return; //  dtto pro případ Odebrání series

            string info1 = (seriesDiff > 0 ? App.GetCountText(seriesDiff, "", "přidali datovou řadu", "přidali {0} datové řady", "přidali {0} datových řad") :
                           (seriesDiff < 0 ? App.GetCountText(-seriesDiff, "", "odebrali datovou řadu", "odebrali {0} datové řady", "odebrali {0} datových řad") : ""));
            string info2 = (seriesDiff > 0 ? "Přidané datové řady nebudou ve Vámi upraveném grafu zobrazeny, budete muset graf opět ručně upravit." :
                                             "Odebrání datových řad způsobí chybu při zobrazení grafu podle původní definice (ta očekává původní datové řady).");
            string text = $@"Tento graf má speciálně upravený vzhled (pomocí tlačítka 'Vzhled grafu').

Nyní jste {info1}.
{info2}

Bylo by vhodné zrušit Vaše úpravy vzhledu grafu a použít výchozí vzhled. Ten pak můžete znovu upravit, až budou datové řady definitivní.

Zrušit úpravy vzhledu?";
            bool yes = App.ShowQuestionYN(this.FindForm(), text);

            // Nyní jsme tedy uživatele varovali:
            if (seriesDiff > 0) ChartLayoutAddSeriesWarningShown = true;
            if (seriesDiff < 0) ChartLayoutRemoveSeriesWarningShown = true;

            // Uživatel souhlasí s odebráním Layoutu:
            if (yes)
            {
                graph.ChartLayout = null;
                ContainChanges = true;
            }
        }
        /// <summary>
        /// Obsahuje true poté, kdy bylo uživateli vydáno varování o vhodnosti změny layoutu po přidání Series.
        /// Výchozí je false.
        /// </summary>
        private bool ChartLayoutAddSeriesWarningShown = false;
        /// <summary>
        /// Obsahuje true poté, kdy bylo uživateli vydáno varování o vhodnosti změny layoutu po přidání Series.
        /// Výchozí je false.
        /// </summary>
        private bool ChartLayoutRemoveSeriesWarningShown = false;
        /// <summary>
        /// Obsahuje aktuálně focusovaný řádek, obsahující serii grafu
        /// </summary>
        private GraphSerieGridRow CurrentSerieGridRow { get { return (_SeriesListGridView.GetFocusedRow() as GraphSerieGridRow); } }

        private DXE.PanelControl _SeriesListPanel;
        private DXE.PanelControl _SeriesListButtonPanel;
        private DXG.Views.Grid.GridView _SeriesListGridView;
        private DXG.GridControl _SeriesListGrid;
        private DXE.SimpleButton _SeriesListAddButton;
        private DXE.SimpleButton _SeriesListRemoveButton;
        private List<GraphSerieGridRow> _SeriesListData;
        #endregion
        #region Detaily jednoho řádku (Panel, obsahuje Texty a Comboboxy)
        private WF.Control CreateControlForSeriesDetail()
        {
            _SeriesDetailPanel = new DXE.PanelControl() { Dock = DockStyle.Fill, BorderStyle = DXE.Controls.BorderStyles.NoBorder };

            int y = DetailYFirst;
            _SeriesDetailTitleLabel = CreateDxeLabel(DetailXLabel, ref y, 600, _SeriesDetailPanel, "Název datové řady");
            _SeriesDetailValueInfoLabel = CreateDxeLabel(DetailXLabel + 610, ref y, 250, _SeriesDetailPanel, "Informace o datech", shiftY: true);

            _SeriesDetailTitleText = CreateDxeTextEdit(DetailXLabel, ref y, 600, _SeriesDetailPanel, _SeriesValueChanged);
            _SeriesDetailValueInfoText = CreateDxeMemoEdit(DetailXLabel + 610, ref y, 250, 50, _SeriesDetailPanel, readOnly: true, tabStop: false);
                
            y = _SeriesDetailTitleText.Bounds.Bottom + DetailYSpaceText;

            _SeriesDetailEntityLabel = CreateDxeLabel(DetailXLabel, ref y, 295, _SeriesDetailPanel, "Okres/město");
            _SeriesDetailValueTypeLabel = CreateDxeLabel(DetailXLabel + 305, ref y, 295, _SeriesDetailPanel, "Druh zobrazených dat", shiftY: true);

            _SeriesDetailEntityText = CreateDxeTextEdit(DetailXLabel, ref y, 295, _SeriesDetailPanel, readOnly: true, tabStop: false);
            _SeriesDetailValueTypeText = CreateDxeTextEdit(DetailXLabel + 305, ref y, 295, _SeriesDetailPanel, readOnly: true, tabStop: false);

            y = _SeriesDetailValueTypeText.Bottom;
            _SeriesDetailValueInfoText.Height = (y - _SeriesDetailValueInfoText.Top);
            y += DetailYSpaceText;

            _SeriesDetailPocetOdDoLabel = CreateDxeLabel(DetailXLabel, ref y, 295, _SeriesDetailPanel, "Výběr obcí dle počtu obyvatel OD-DO:", shiftY: true);


            _SeriesDetailPocetOdText = CreateDxeSpinEdit(DetailXLabel, ref y, 110, _SeriesDetailPanel, _SeriesValueChanged, minValue: 0m, maxValue: 10000000000m, increment: 100m, mask: "### ### ### ###", spinStyles: DXE.Controls.SpinStyles.Vertical);
            _SeriesDetailPocetDoText = CreateDxeSpinEdit(DetailXLabel + 120, ref y, 110, _SeriesDetailPanel, _SeriesValueChanged, minValue: 0m, maxValue: 10000000000m, increment: 100m, mask: "### ### ### ###", spinStyles: DXE.Controls.SpinStyles.Vertical);


            _GraphSeriesDetailHeight = y + 12;


            return _SeriesDetailPanel;
        }
        /// <summary>
        /// Po změně jakéhokoli údaje v hlavičce se uloží data z GUI do datového objektu grafu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SeriesValueChanged(object sender, EventArgs e)
        {
            SeriesDetailStore();
            ContainChanges = true;
            ContainSerieChanges = true;
        }
        /// <summary>
        /// Načte detailní data z aktuálně zobrazené serie z datového objektu grafu do GUI SeriesDetail
        /// </summary>
        private void SeriesDetailRefresh()
        {
            bool oldDRR = _DataRefreshRunning;
            _DataRefreshRunning = true;

            var row = CurrentSerieGridRow;
            var serie = row?.Serie;
            CurrentSerieInfo = serie;
            _SeriesDetailTitleText.Text = serie?.Title ?? "";
            _SeriesDetailEntityText.Text = row?.EntityText ?? "";
            _SeriesDetailValueTypeText.Text = row?.ValueTypeName ?? "";
            _SeriesDetailValueInfoText.Text = serie?.ValueTypeInfo.ToolTip ?? "";
            _SeriesDetailPocetOdText.Value = serie?.FiltrPocetObyvatelOd ?? 0;
            _SeriesDetailPocetDoText.Value = serie?.FiltrPocetObyvatelDo ?? 0;


            _DataRefreshRunning = oldDRR;
        }
        /// <summary>
        /// Uloží data aktuální položky z GUI do datového objektu
        /// </summary>
        private void SeriesDetailStore()
        {
            if (_DataRefreshRunning) return;               // Probíhá SeriesDetailRefresh(), mění se hodnoty v UI, volají se eventy Changed => nesmím provádět Store, nemá to smysl !!!

            var serie = CurrentSerieInfo;
            if (serie == null) return;

            serie.Title = _SeriesDetailTitleText.Text;
            serie.FiltrPocetObyvatelOd = (_SeriesDetailPocetOdText.Value > 0m ? (int?)_SeriesDetailPocetOdText.Value : (int?)null);
            serie.FiltrPocetObyvatelDo = (_SeriesDetailPocetDoText.Value > 0m ? (int?)_SeriesDetailPocetDoText.Value : (int?)null);



            _SeriesListGridView.RefreshData();
//             _SeriesListGrid.RefreshDataSource();

        }
        /// <summary>
        /// Serie grafu, aktuálně zobrazení v Gridu, její data jsou zobrazena v poli SeriesDetail.
        /// Do této serie grafu se budou vkládat data po editaci.
        /// Teoreticky to může být null.
        /// </summary>
        private GraphSerieInfo CurrentSerieInfo;
        DXE.PanelControl _SeriesDetailPanel;
        DXE.LabelControl _SeriesDetailTitleLabel;
        DXE.TextEdit _SeriesDetailTitleText;
        DXE.LabelControl _SeriesDetailEntityLabel;
        DXE.TextEdit _SeriesDetailEntityText;
        DXE.LabelControl _SeriesDetailValueTypeLabel;
        DXE.TextEdit _SeriesDetailValueTypeText;
        DXE.LabelControl _SeriesDetailValueInfoLabel;
        DXE.MemoEdit _SeriesDetailValueInfoText;
        DXE.LabelControl _SeriesDetailPocetOdDoLabel;
        DXE.SpinEdit _SeriesDetailPocetOdText;
        DXE.SpinEdit _SeriesDetailPocetDoText;
        #endregion
        #region Generátory controlů
        private DXE.LabelControl CreateDxeLabel(int x, ref int y, int w, Control parent, string text,
            bool? visible = null, bool shiftY = false)
        {
            DXE.LabelControl label = new DXE.LabelControl() { Bounds = new Rectangle(x, y, w, DetailYHeightLabel), Text = text };
            label.StyleController = _LabelStyle;
            if (visible.HasValue) label.Visible = visible.Value;
            parent.Controls.Add(label);
            if (shiftY) y = y + label.Height + DetailYSpaceLabel;
            return label;
        }
        private DXE.TextEdit CreateDxeTextEdit(int x, ref int y, int w, Control parent, EventHandler textChanged = null,
            DXE.Mask.MaskType? maskType = null, string editMask = null, bool? useMaskAsDisplayFormat = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            DXE.TextEdit textEdit = new DXE.TextEdit() { Bounds = new Rectangle(x, y, w, DetailYHeightText) };
            textEdit.StyleController = _InputStyle;
            if (visible.HasValue) textEdit.Visible = visible.Value;
            if (readOnly.HasValue) textEdit.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) textEdit.TabStop = tabStop.Value;

            if (maskType.HasValue) textEdit.Properties.Mask.MaskType = maskType.Value;
            if (editMask != null) textEdit.Properties.Mask.EditMask = editMask;
            if (useMaskAsDisplayFormat.HasValue) textEdit.Properties.Mask.UseMaskAsDisplayFormat = useMaskAsDisplayFormat.Value;

            if (textChanged != null) textEdit.TextChanged += textChanged;
            parent.Controls.Add(textEdit);
            if (shiftY) y = y + textEdit.Height + DetailYSpaceText;
            return textEdit;
        }
        private DXE.MemoEdit CreateDxeMemoEdit(int x, ref int y, int w, int h, Control parent, EventHandler textChanged = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            DXE.MemoEdit memoEdit = new DXE.MemoEdit() { Bounds = new Rectangle(x, y, w, h) };
            memoEdit.StyleController = _InputStyle;
            if (visible.HasValue) memoEdit.Visible = visible.Value;
            if (readOnly.HasValue) memoEdit.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) memoEdit.TabStop = tabStop.Value;

            if (textChanged != null) memoEdit.TextChanged += textChanged;
            parent.Controls.Add(memoEdit);
            if (shiftY) y = y + memoEdit.Height + DetailYSpaceText;
            return memoEdit;
        }
        private DXE.ImageComboBoxEdit CreateDxeImageComboBox(int x, ref int y, int w, Control parent, EventHandler selectedIndexChanged = null, string itemsTabbed = null, 
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            DXE.ImageComboBoxEdit comboBox = new DXE.ImageComboBoxEdit() { Bounds = new Rectangle(x, y, w, DetailYHeightText) };
            comboBox.StyleController = _InputStyle;
            if (visible.HasValue) comboBox.Visible = visible.Value;
            if (readOnly.HasValue) comboBox.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) comboBox.TabStop = tabStop.Value;

            if (itemsTabbed != null)
            {
                string[] items = itemsTabbed.Split('\t');
                for (int i = 0; i < items.Length; i++)
                    comboBox.Properties.Items.Add(items[i], i, 0);
            }
            if (selectedIndexChanged != null) comboBox.SelectedIndexChanged += selectedIndexChanged;
            parent.Controls.Add(comboBox);
            if (shiftY) y = y + comboBox.Height + DetailYSpaceText;
            return comboBox;
        }
        private DXE.SpinEdit CreateDxeSpinEdit(int x, ref int y, int w, Control parent, EventHandler valueChanged = null,
            decimal? minValue = null, decimal? maxValue = null, decimal? increment = null, string mask = null, DXE.Controls.SpinStyles? spinStyles = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            DXE.SpinEdit spinEdit = new DXE.SpinEdit() { Bounds = new Rectangle(x, y, w, DetailYHeightText) };
            spinEdit.StyleController = _InputStyle;
            if (visible.HasValue) spinEdit.Visible = visible.Value;
            if (readOnly.HasValue) spinEdit.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) spinEdit.TabStop = tabStop.Value;

            if (minValue.HasValue) spinEdit.Properties.MinValue = minValue.Value;
            if (maxValue.HasValue) spinEdit.Properties.MaxValue = maxValue.Value;
            if (increment.HasValue) spinEdit.Properties.Increment = increment.Value;
            if (mask != null)
            {
                spinEdit.Properties.EditMask = mask;
                spinEdit.Properties.DisplayFormat.FormatString = mask;
                spinEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                spinEdit.Properties.EditFormat.FormatString = mask;
            }
            if (spinStyles.HasValue) spinEdit.Properties.SpinStyle = spinStyles.Value;

            if (valueChanged != null) spinEdit.ValueChanged += valueChanged;
            parent.Controls.Add(spinEdit);
            if (shiftY) y = y + spinEdit.Height + DetailYSpaceText;
            return spinEdit;
        }
        private DXE.CheckEdit CreateDxeCheckEdit(int x, ref int y, int w, Control parent, string text, EventHandler checkedChanged = null,
            DXE.Controls.CheckBoxStyle? checkBoxStyle = null, DXE.Controls.BorderStyles? borderStyles = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            DXE.CheckEdit checkEdit = new DXE.CheckEdit() { Bounds = new Rectangle(x, y, w, DetailYHeightText), Text = text };
            checkEdit.StyleController = _InputStyle;
            if (visible.HasValue) checkEdit.Visible = visible.Value;
            if (readOnly.HasValue) checkEdit.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) checkEdit.TabStop = tabStop.Value;

            if (checkBoxStyle.HasValue) checkEdit.Properties.CheckBoxOptions.Style = checkBoxStyle.Value;
            if (borderStyles.HasValue) checkEdit.BorderStyle = borderStyles.Value;

            if (checkedChanged != null) checkEdit.CheckedChanged += checkedChanged;
            parent.Controls.Add(checkEdit);
            if (shiftY) y = y + checkEdit.Height + DetailYSpaceText;
            return checkEdit;
        }

        #endregion
        #region Správa layoutu - splittery, uložení konfigurace
        /// <summary>
        /// Volá se těsně před prvním zobrazením. Layout formuláře (rozměry a splittery) je nastaven.
        /// Zdejší objekt si má z konfigurace načíst svůj layout a aplikovat jej do Controlů.
        /// </summary>
        internal void OnFirstShown()
        {
            RestoreSplitters();
            this.DataRefresh();
        }
        /// <summary>
        /// Z konfigurace načte pozice splitterů a vepíše je do nich
        /// </summary>
        private void RestoreSplitters()
        {
            // Pole Int32 hodnot, které reprezentují pozice splitterů:
            var configLayout = Data.App.Config.EditFormGraphPanelLayout;

            // 1. Main SplitContainer
            _GraphSplitContainer.SplitterPosition = GetSplitterPosition(configLayout, 0, 150, _GraphSplitContainer.Height * 4 / 10);
            _GraphSplitContainer.SplitterPositionChanged += _AnySplitterPositionChanged;

            // 2. SplitContainer v TabContaineru na stránce [1]:
            _SeriesNewSplitContainer.SplitterPosition = GetSplitterPosition(configLayout, 1, 250, _SeriesNewSplitContainer.Width * 5 / 10);
            _SeriesNewSplitContainer.SplitterPositionChanged += _AnySplitterPositionChanged;

            // 3. SplitContainer v dolní části, ten je fixní:
            _SeriesSplitContainer.SplitterPosition = 120;
        }

        /// <summary>
        /// Vrátí požadovanou hodnotu z pole konfigurace (pokud existuje), nejméně minValue, nebo defaultValue
        /// </summary>
        /// <param name="configLayout"></param>
        /// <param name="index"></param>
        /// <param name="minValue"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private int GetSplitterPosition(int[] configLayout, int index, int minValue, int defaultValue)
        {
            if (configLayout != null && index < configLayout.Length && configLayout[index] > minValue) return configLayout[index];
            return defaultValue;
        }
        /// <summary>
        /// Po změně pozice kteréhokoli splitteru se uloží layout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _AnySplitterPositionChanged(object sender, EventArgs e)
        {
            StoreSplitters();
        }
        /// <summary>
        /// Posbírá layout (pozice splitterů) a uloží je do konfigurace
        /// </summary>
        private void StoreSplitters()
        {
            List<int> configLayout = new List<int>();

            configLayout.Add(_GraphSplitContainer.SplitterPosition);
            configLayout.Add(_SeriesNewSplitContainer.SplitterPosition);

            Data.App.Config.EditFormGraphPanelLayout = configLayout.ToArray();
        }
        #endregion
        #region Data, Refresh, Store
        /// <summary>
        /// Formulář, který obsahuje referenci na definici grafu i na databázi
        /// </summary>
        public GraphForm ParentForm { get; set; }
        /// <summary>
        /// Databáze
        /// </summary>
        public DatabaseInfo Database { get { return ParentForm?.Database; } }
        /// <summary>
        /// Objekt s definicí grafu, do něj přímo ukládáme data. Za běžné situace není null, pouze krátký čas při inicializaci.
        /// Po vnější změně obsahu dat (setování) je vyvolána zdejší metoda <see cref="DataRefresh"/>.
        /// Jde o referenci na soukromou pracovní instanci, která je uložena ve formuláři.
        /// </summary>
        public GraphInfo CurrentGraphInfo { get; set; }
        /// <summary>
        /// Obsahuje jakékoli změny?
        /// </summary>
        public bool ContainChanges
        {
            get { return _ContainChanges; }
            protected set
            {
                if (!_DataRefreshRunning && value)
                {
                    if (!_ContainChanges)
                    {
                        _ContainChanges = true;
                        OnFirstDataChanged();
                    }
                    OnAnyDataChanged();
                }
            }
        }
        private bool _ContainChanges;
        /// <summary>
        /// Obsahuje změny serií (přidané, odebrané, přejmenované)?
        /// </summary>
        public bool ContainSerieChanges
        {
            get { return _ContainSerieChanges; }
            protected set
            {
                if (!_DataRefreshRunning && value)
                {
                    if (!_ContainSerieChanges)
                    {
                        _ContainSerieChanges = true;
                        OnFirstSerieDataChanged();
                    }
                    OnAnyDataChanged();
                }
            }
        }
        private bool _ContainSerieChanges;
        /// <summary>
        /// true = Právě probíhá Refresh dat, v té situaci ignorujeme hlášení změn
        /// </summary>
        private bool _DataRefreshRunning;
        /// <summary>
        /// Je voláno po první změně kterýchkoli dat, která je provedena do "čistého" objektu
        /// </summary>
        protected virtual void OnFirstDataChanged()
        {
            FirstDataChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Je voláno po první změně kterýchkoli dat, která je provedena do "čistého" objektu
        /// </summary>
        public event EventHandler FirstDataChanged;
        /// <summary>
        /// Je voláno po první změně dat serie, která je provedena do "čistého" objektu
        /// </summary>
        protected virtual void OnFirstSerieDataChanged()
        {
            FirstSerieDataChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Je voláno po první změně dat serie, která je provedena do "čistého" objektu
        /// </summary>
        public event EventHandler FirstSerieDataChanged;


        /// <summary>
        /// Je voláno po každé změně dat
        /// </summary>
        protected virtual void OnAnyDataChanged()
        {
            AnyDataChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Je voláno po každé změně dat
        /// </summary>
        public event EventHandler AnyDataChanged;



        /// <summary>
        /// Metoda je volaná po změně dat v objektu <see cref="CurrentGraphInfo"/>.
        /// Změnu dat provádí vnjší logika v procesu editace. Zdejší objekt má aktualizovat data ve svých objektech.
        /// </summary>
        public void DataRefresh()
        {
            if (this.ParentForm == null || !this.ParentForm.IsShown) return;        // DataRefresh() je volán i v procesu FirstShown, a teprve tehdy má smysl jej provádět

            _DataRefreshRunning = true;

            HeaderDetailRefresh();
            SeriesNewRefreshData();
            SeriesListReloadDataFromGraph(true);
            SeriesDetailRefresh();

            _ContainChanges = false;
            _DataRefreshRunning = false;
        }
        /// <summary>
        /// Prověří, zda zadaná definice je platná. Pokud není, může zobrazit varování uživateli.
        /// Vrací true = data jsou OK
        /// </summary>
        /// <param name="showErrorsPrefix"></param>
        /// <returns></returns>
        internal bool IsValidData(string showErrorsPrefix)
        {
            string errors = this.CurrentGraphInfo?.Errors;
            if (String.IsNullOrEmpty(errors)) return true;
            if (showErrorsPrefix != null)
                App.ShowWarning(this.FindForm(), showErrorsPrefix + errors);
            return false;
        }
        #endregion
    }

    #region class GraphSerieGridRow : Třída pro zobrazení dat serií
    /// <summary>
    /// <see cref="GraphSerieGridRow"/> : Třída pro zobrazení dat serií
    /// </summary>
    public class GraphSerieGridRow : INotifyPropertyChanged
    {
        [DisplayName("Název informace")]
        public string Title { get { return Serie.Title; } }
        [DisplayName("Obec (město, okres, kraj)")]
        public string EntityText { get { return _EntityText; } } private string _EntityText;
        [DisplayName("Druh zobrazených dat")]
        public string ValueTypeName { get { return _ValueTypeName; } } private string _ValueTypeName;
        [DisplayName("Počet obyvatel OD")]
        public string PocetOd { get { return _GetPocet(Serie.FiltrPocetObyvatelOd); } }
        [DisplayName("Počet obyvatel DO")]
        public string PocetDo { get { return _GetPocet(Serie.FiltrPocetObyvatelDo); } }

        public GraphSerieInfo Serie { get; set; }
        protected DatabaseInfo Database { get; set; }

        internal static GraphSerieGridRow Create(GraphSerieInfo serie, DatabaseInfo database)
        {
            if (serie == null || database == null) return null;
            GraphSerieGridRow gridRow = new GraphSerieGridRow();
            gridRow.Serie = serie;
            gridRow.Database = database;
            gridRow._EntityText = (database.GetEntity(serie.EntityFullCode)?.Text ?? "");
            gridRow._ValueTypeName = serie.ValueTypeInfo?.Text;
            return gridRow;
        }
        protected static string _GetPocet(int? pocet)
        {
            return (pocet.HasValue ? pocet.Value.ToString("### ### ##0").Trim() : "");
        }
        #region INotifyPropertyChanged
        private void _Set<T>(ref T variable, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "") where T : IComparable
        {
            if (value.CompareTo(variable) != 0)
            {
                variable = value;
                OnPropertyChanged(propertyName);
            }
        }
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (_PropertyChanged != null)
                _PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { _PropertyChanged += value; }
            remove { _PropertyChanged -= value; }
        }
        event PropertyChangedEventHandler _PropertyChanged;
        #endregion
    }
    #endregion
}
