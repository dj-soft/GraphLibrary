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
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        /// <summary>
        /// Po kliknutí na "Uložit jako nový"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSaveAs_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Yes;
            this.Close();
        }
        /// <summary>
        /// Po kliknutí na "Storno"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
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
            _GraphPanel.DataChanged += _GraphPanel_DataChanged;
            _MainSplitContainer.Panel1.Controls.Add(_GraphPanel);
        }
        /// <summary>
        /// Poté, kdy uživatel změnil data v definici grafu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _GraphPanel_DataChanged(object sender, EventArgs e)
        {
            RefreshButtons();
        }
        /// <summary>
        /// Panel s daty obsahuje změny?
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
        #region Rozvržení layoutu
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

            // Horní panel Main SplitContaineru obsadí TabPage se záložkami pro hlavičku a pro nové položky:
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

            // Dolní panel Main SplitContaineru obsadí sekundární SplitContainer, obsahující v Panel1 = Grid = Seznam položek, a v Panel2 = detail jedné položky:
            _SeriesSplitContainer = new DXE.SplitContainerControl()
            {
                FixedPanel = DXE.SplitFixedPanel.Panel2,
                IsSplitterFixed = true,
                Horizontal = false,
                PanelVisibility = DXE.SplitPanelVisibility.Both,
                SplitterPosition = 200,
                Dock = DockStyle.Fill,
                ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True
            };
            _SeriesSplitContainer.Panel1.MinSize = 150;
            _SeriesSplitContainer.Panel2.MinSize = 100;

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
        #region Hlavička grafu (Panel)
        private WF.Control CreateControlForHeaderDetail()
        {
            _HeaderDetailPanel = new DXE.PanelControl() { Dock = DockStyle.Fill, BorderStyle = DXE.Controls.BorderStyles.NoBorder };

            int y = DetailYFirst;
            _HeaderDetailTitleLabel = new DXE.LabelControl() { Bounds = new Rectangle(DetailXLabel, y, 320, DetailYHeightLabel), Text = "Název celého grafu" };
            _HeaderDetailTitleLabel.StyleController = _LabelStyle;
            _HeaderDetailPanel.Controls.Add(_HeaderDetailTitleLabel);

            _HeaderDetailDescriptionLabel = new DXE.LabelControl() { Bounds = new Rectangle(DetailXLabel + 390, y, 320, DetailYHeightLabel), Text = "Název celého grafu" };
            _HeaderDetailDescriptionLabel.StyleController = _LabelStyle;
            _HeaderDetailPanel.Controls.Add(_HeaderDetailDescriptionLabel);

            y = _HeaderDetailTitleLabel.Bounds.Bottom + DetailYSpaceLabel;

            _HeaderDetailTitleText = new DXE.TextEdit() { Bounds = new Rectangle(DetailXText, y, 375, DetailYHeightText) };
            _HeaderDetailTitleText.StyleController = _InputStyle;
            _HeaderDetailTitleText.TextChanged += _HeaderValueChanged;
            _HeaderDetailPanel.Controls.Add(_HeaderDetailTitleText);

            _HeaderDetailDescriptionText = new DXE.MemoEdit() { Bounds = new Rectangle(DetailXText + 390, y, 375, 135) };
            _HeaderDetailDescriptionText.StyleController = _InputStyle;
            _HeaderDetailDescriptionText.TextChanged += _HeaderValueChanged;
            _HeaderDetailPanel.Controls.Add(_HeaderDetailDescriptionText);

            y = _HeaderDetailTitleText.Bounds.Bottom + DetailYSpaceText;

            _HeaderDetailTimeTypeLabel = new DXE.LabelControl() { Bounds = new Rectangle(DetailXLabel, y, 320, DetailYHeightLabel), Text = "Časové omezení dat grafu" };
            _HeaderDetailTimeTypeLabel.StyleController = _LabelStyle;
            _HeaderDetailPanel.Controls.Add(_HeaderDetailTimeTypeLabel);

            y = _HeaderDetailTimeTypeLabel.Bounds.Bottom + DetailYSpaceLabel;

            _HeaderDetailTimeTypeCombo = new DXE.ImageComboBoxEdit() { Bounds = new Rectangle(DetailXText, y, 200, DetailYHeightText) };
            _HeaderDetailTimeTypeCombo.Properties.Items.Add("Bez omezení", 0, 0);
            _HeaderDetailTimeTypeCombo.Properties.Items.Add("Posledních několik měsíců", 1, 0);
            _HeaderDetailTimeTypeCombo.Properties.Items.Add("Posledních několik dnů", 2, 0);
            _HeaderDetailTimeTypeCombo.Properties.Items.Add("Přesně daný interval Od-Do", 3, 0);
            _HeaderDetailTimeTypeCombo.StyleController = _InputStyle;
            _HeaderDetailTimeTypeCombo.SelectedIndexChanged += _HeaderDetailTimeTypeCombo_SelectedIndexChanged;
            _HeaderDetailPanel.Controls.Add(_HeaderDetailTimeTypeCombo);

            _HeaderDetailTimeLastMonthsText = new DXE.TextEdit() { Bounds = new Rectangle(DetailXText + 210, y, 45, DetailYHeightText), Visible = false };
            _HeaderDetailTimeLastMonthsText.StyleController = _InputStyle;
            _HeaderDetailTimeLastMonthsText.Properties.Mask.MaskType = DXE.Mask.MaskType.Numeric;
            _HeaderDetailTimeLastMonthsText.Properties.Mask.EditMask = "###0";
            _HeaderDetailTimeLastMonthsText.Properties.Mask.UseMaskAsDisplayFormat = true;
            _HeaderDetailTimeLastMonthsText.TextChanged += _HeaderValueChanged;
            _HeaderDetailPanel.Controls.Add(_HeaderDetailTimeLastMonthsText);

            _HeaderDetailTimeLastDaysText = new DXE.TextEdit() { Bounds = new Rectangle(DetailXText + 210, y, 45, DetailYHeightText), Visible = false };
            _HeaderDetailTimeLastDaysText.StyleController = _InputStyle;
            _HeaderDetailTimeLastDaysText.Properties.Mask.MaskType = DXE.Mask.MaskType.Numeric;
            _HeaderDetailTimeLastDaysText.Properties.Mask.EditMask = "###0";
            _HeaderDetailTimeLastDaysText.Properties.Mask.UseMaskAsDisplayFormat = true;
            _HeaderDetailTimeLastDaysText.TextChanged += _HeaderValueChanged;
            _HeaderDetailPanel.Controls.Add(_HeaderDetailTimeLastDaysText);

            _HeaderDetailTimeRangeBeginText = new DXE.TextEdit() { Bounds = new Rectangle(DetailXText + 210, y, 80, DetailYHeightText), Visible = false };
            _HeaderDetailTimeRangeBeginText.StyleController = _InputStyle;
            _HeaderDetailTimeRangeBeginText.Properties.Mask.MaskType = DXE.Mask.MaskType.DateTimeAdvancingCaret;
            _HeaderDetailTimeRangeBeginText.Properties.Mask.EditMask = "d";
            _HeaderDetailTimeRangeBeginText.Properties.Mask.UseMaskAsDisplayFormat = true;
            _HeaderDetailTimeRangeBeginText.TextChanged += _HeaderValueChanged;
            _HeaderDetailPanel.Controls.Add(_HeaderDetailTimeRangeBeginText);

            _HeaderDetailTimeRangeEndText = new DXE.TextEdit() { Bounds = new Rectangle(DetailXText + 295, y, 80, DetailYHeightText), Visible = false };
            _HeaderDetailTimeRangeEndText.StyleController = _InputStyle;
            _HeaderDetailTimeRangeEndText.Properties.Mask.MaskType = DXE.Mask.MaskType.DateTimeAdvancingCaret;
            _HeaderDetailTimeRangeEndText.Properties.Mask.EditMask = "d";
            _HeaderDetailTimeRangeEndText.Properties.Mask.UseMaskAsDisplayFormat = true;
            _HeaderDetailTimeRangeEndText.TextChanged += _HeaderValueChanged;
            _HeaderDetailPanel.Controls.Add(_HeaderDetailTimeRangeEndText);

            y = _HeaderDetailTimeTypeCombo.Bounds.Bottom + DetailYSpaceText;

            int checkWidth = 350;
            int checkX = DetailXText;
            _HeaderDetailTimeStripesCheck = new DXE.CheckEdit() { Bounds = new Rectangle(checkX, y, checkWidth, DetailYHeightText), Text = "Zobrazovat význačné časové intervaly" }; y = _HeaderDetailTimeStripesCheck.Bounds.Bottom + DetailYSpaceText;
            _HeaderDetailTimeStripesCheck.StyleController = _InputStyle;
            _HeaderDetailTimeStripesCheck.BorderStyle = DXE.Controls.BorderStyles.NoBorder;
            _HeaderDetailTimeStripesCheck.Properties.CheckBoxOptions.Style = DXE.Controls.CheckBoxStyle.SvgToggle1;
            _HeaderDetailTimeStripesCheck.CheckedChanged += _HeaderValueChanged;
            _HeaderDetailPanel.Controls.Add(_HeaderDetailTimeStripesCheck);

            _HeaderDetailTimeZoomCheck = new DXE.CheckEdit() { Bounds = new Rectangle(checkX, y, checkWidth, DetailYHeightText), Text = "Povolit zoom na časové ose" }; y = _HeaderDetailTimeZoomCheck.Bounds.Bottom + DetailYSpaceText;
            _HeaderDetailTimeZoomCheck.StyleController = _InputStyle;
            _HeaderDetailTimeZoomCheck.BorderStyle = DXE.Controls.BorderStyles.NoBorder;
            _HeaderDetailTimeZoomCheck.Properties.CheckBoxOptions.Style = DXE.Controls.CheckBoxStyle.SvgToggle1;
            _HeaderDetailTimeZoomCheck.CheckedChanged += _HeaderValueChanged;
            _HeaderDetailPanel.Controls.Add(_HeaderDetailTimeZoomCheck);

            _HeaderDetailAxisOnRightCheck = new DXE.CheckEdit() { Bounds = new Rectangle(checkX, y, checkWidth, DetailYHeightText), Text = "Svislá osa vpravo" }; y = _HeaderDetailAxisOnRightCheck.Bounds.Bottom + DetailYSpaceText;
            _HeaderDetailAxisOnRightCheck.StyleController = _InputStyle;
            _HeaderDetailAxisOnRightCheck.BorderStyle = DXE.Controls.BorderStyles.NoBorder;
            _HeaderDetailAxisOnRightCheck.Properties.CheckBoxOptions.Style = DXE.Controls.CheckBoxStyle.SvgToggle1;
            _HeaderDetailAxisOnRightCheck.CheckedChanged += _HeaderValueChanged;
            _HeaderDetailPanel.Controls.Add(_HeaderDetailAxisOnRightCheck);

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
            graph.TimeRangeLastMonths = (timeIndex == 1 ? GetValue<int>(_HeaderDetailTimeLastMonthsText.EditValue) : (int?)null);
            graph.TimeRangeLastDays = (timeIndex == 2 ? GetValue<int>(_HeaderDetailTimeLastDaysText.EditValue) : (int?)null);
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
        DXE.TextEdit _HeaderDetailTimeLastMonthsText;
        DXE.TextEdit _HeaderDetailTimeLastDaysText;
        DXE.TextEdit _HeaderDetailTimeRangeBeginText;
        DXE.TextEdit _HeaderDetailTimeRangeEndText;
        DXE.CheckEdit _HeaderDetailTimeStripesCheck;
        DXE.CheckEdit _HeaderDetailTimeZoomCheck;
        DXE.CheckEdit _HeaderDetailAxisOnRightCheck;
        #endregion
        #region Panel pro zadání nových serií
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
        #region Seznam s obcemi = zdroj entit
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

            _EntitySearchButton = new DXE.SimpleButton() { Bounds = new Rectangle(206, 22, 50, 25), Text = "Vyhledat" };
            _EntitySearchButton.SuperTip = new DevExpress.Utils.SuperToolTip();
            _EntitySearchButton.SuperTip.Items.AddTitle("Vyhledat v databázi");
            _EntitySearchButton.SuperTip.Items.Add("Po zadání textu vlevo stiskněte toto tlačítko.");
            _EntitySearchButton.Click += _EntitySearchButton_Click;
            _EntitySearchButton.Visible = false;
            _EntityPanel.Controls.Add(_EntitySearchButton);

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
                entityCodes = this.CurrentGraphInfo.Series.Select(s => s.DataEntityCode).Distinct().ToArray();
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
        private void _EntitySearchButton_Click(object sender, EventArgs e)
        {
            EntitySearchRun();
            this._EntitySearchText.Focus();
        }
        private void _EntityPanelLayout()
        {
            if (_EntityListBox == null) return;

            int mx = 3;
            int my = 3;
            int sx = 2;
            int sy = 2;
            int ty = 4;
            Size size = _EntityPanel.ClientSize;
            bool btnVisible = _EntitySearchButton.Visible;
            int btnWidth = (btnVisible ? _EntitySearchButton.GetPreferredSize(new Size(200, 80)).Width + 16 : 0);
            int inpWidth = size.Width - mx - btnWidth - mx - (btnVisible ? sx : 0);
            _EntitySearchLabel.Bounds = new Rectangle(mx, my, inpWidth, 20);
            _EntitySearchText.Bounds = new Rectangle(mx, _EntitySearchLabel.Bounds.Bottom + ty, inpWidth, 25);
            int inpBottom = _EntitySearchText.Bounds.Bottom;
            if (btnVisible)
            {
                int btnX = _EntitySearchText.Right + sx;
                int btnY = my + 4;
                _EntitySearchButton.Bounds = new Rectangle(btnX, btnY, size.Width - mx - btnX, inpBottom - btnY);
            }

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
        private DXE.SimpleButton _EntitySearchButton;
        private DXE.TextEdit _EntitySearchText;
        private DXE.ListBoxControl _EntityListBox;
        #endregion
        #region Seznam s datovými typy = zdroj dat
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

            SeriesListRefreshData(false);

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
        /// Do gridu se seriemi <see cref="_SeriesListGrid"/> načte aktuální obsah definice grafu <see cref="CurrentGraphInfo"/>.
        /// </summary>
        protected void SeriesListRefreshData(bool refreshGui)
        {
            if (_SeriesListData.Count > 0) _SeriesListData.Clear();
            if (this.Database == null || this.CurrentGraphInfo == null) return;

            foreach (var serie in this.CurrentGraphInfo.Series)
                AddSerieToSeriesList(serie);

            if (refreshGui)
                RefreshSeriesListGrid();
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
        private void RefreshSeriesListGrid()
        {
            _SeriesListGridView.RefreshData();
            _SeriesListGrid.RefreshDataSource();
            _SeriesListGrid.Refresh();

       }
        private void _SeriesListAddButton_Click(object sender, EventArgs e)
        {
            App.TryRun(_SeriesListAddNewSeries);
        }
        private void _SeriesListRemoveButton_Click(object sender, EventArgs e)
        {

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
            int newSeriesCount = newSerieKeys.Count;
            if (newSeriesCount == 0)
            {
                App.ShowWarning(this.FindForm(), "Všechny zvolené obce a datové hodnoty jsou v grafu již obsaženy.");
                return;
            }

            int currentCount = this.CurrentGraphInfo.Series.Length;
            int sumCount = currentCount + newSeriesCount;
            if (sumCount > GraphInfo.MaxSeriesCount)
            {
                string eol = Environment.NewLine;
                string message = $"Pro rozumnou přehlednost grafu není možno do něj vložit více než {GraphInfo.MaxSeriesCount} datových řad.{eol}{eol}";
                if (currentCount > 0)
                {
                    string currentSeriesText = GetCountText(currentCount, "není v grafu žádná datová řada", "je v grafu jedna datová řada", "jsou v grafu {0} datové řady", "je v grafu {0} datových řad");
                    string newSeriesText = GetCountText(newSeriesCount, "žádné řady", "další jedné řady", "dalších {0} řad", "dalších {0} řad");
                    message += $"Aktuálně {currentSeriesText}, a při požadavku na přidání {newSeriesText} by byl překročen maximální povolený počet.";
                }
                else
                {
                    string valueText = GetCountText(valueItems.Length, "žádnou typ dat", "jednoho typu dat", "{0} typů dat", "{0} typů dat");
                    string entitytext = GetCountText(entityItems.Length, "žádnou obec", "jednu obec", "{0} obce", "{1} obcí");
                    string newSeriesText = GetCountText(newSeriesCount, "žádnou datovou řadu", "jednu datovou řadu", "{0} datové řady", "{0} datových řad");
                    message += $"Zvolili jste přidání {valueText} pro {entitytext}, celkem tedy {newSeriesText}. Zmenšete jejich počet.";
                }
                message += $"{eol}{eol}Zvolte méně dat a zkuste je přidat poté.";
                App.ShowWarning(this.FindForm(), message);
                return;
            }

            _AddNewSeriesToGraph(newSerieKeys);
            RefreshSeriesListGrid();
            ContainChanges = true;
        }
        /// <summary>
        /// Vrací text podle počtu: 
        /// pro počet (<paramref name="count"/>) = 0 vrací String.Format(<paramref name="text0"/>, count);
        /// pro počet 1 vrací String.Format(<paramref name="text1"/>, count);
        /// pro počet 2,3,4 vrací String.Format(<paramref name="text234"/>, count);
        /// a pro ostatní vrací String.Format(<paramref name="text5plus"/>, count);
        /// <para/> Pro definici textu tedy lze využít vložení daného počtu do potřebného místa textu, pomocí {0}, anebo nemusí být použit vůbec.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="text0"></param>
        /// <param name="text1"></param>
        /// <param name="text234"></param>
        /// <param name="text5plus"></param>
        /// <returns></returns>
        private string GetCountText(int count, string text0, string text1, string text234, string text5plus)
        {
            switch (count)
            {
                case 0: return String.Format(text0, count);
                case 1: return String.Format(text1, count);
                case 2:
                case 3:
                case 4: return String.Format(text234, count);
            }
            return String.Format(text5plus, count);
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
                    bool exists = series.Any(s => s.DataEntityCode == entityCode && s.ValueType == valueItem.Value);
                    if (!exists)
                        result.Add(new Tuple<DatabaseInfo.EntityInfo, DataValueTypeInfo>(entityItem, valueItem));
                }
            }
            return result;
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
                GraphSerieInfo serie = new GraphSerieInfo() { Title = title, DataEntityCode = entity.FullCode, ValueType = valueType.Value };
                this.CurrentGraphInfo.AddSerie(serie);
                AddSerieToSeriesList(serie);
            }
        }
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
        #region Detaily jednoho řádku (Panel)
        private WF.Control CreateControlForSeriesDetail()
        {
            _SeriesDetailPanel = new DXE.PanelControl() { Dock = DockStyle.Fill, BorderStyle = DXE.Controls.BorderStyles.NoBorder };

            int y = DetailYFirst;
            _SeriesDetailTitleLabel = new DXE.LabelControl() { Bounds = new Rectangle(DetailXLabel, y, 520, DetailYHeightLabel), Text = "Název datové řady" };
            _SeriesDetailTitleLabel.StyleController = _LabelStyle;
            _SeriesDetailPanel.Controls.Add(_SeriesDetailTitleLabel);

            y = _SeriesDetailTitleLabel.Bounds.Bottom + DetailYSpaceLabel;

            _SeriesDetailTitleText = new DXE.TextEdit() { Bounds = new Rectangle(DetailXText, y, 520, DetailYHeightText) }; 
            _SeriesDetailTitleText.StyleController = _InputStyle;
            _SeriesDetailTitleText.TextChanged += _SeriesValueChanged;
            _SeriesDetailPanel.Controls.Add(_SeriesDetailTitleText);

            y = _SeriesDetailTitleText.Bounds.Bottom + DetailYSpaceText;


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
        }
        /// <summary>
        /// Načte data aktuálně zobrazené serie z datového objektu do GUI
        /// </summary>
        private void SeriesDetailRefresh()
        {
            bool oldDRR = _DataRefreshRunning;
            _DataRefreshRunning = true;

            var row = CurrentSerieGridRow;
            var serie = row?.Serie;
            CurrentSerieInfo = serie;
            _SeriesDetailTitleText.Text = serie?.Title ?? "";

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
        DXE.LabelControl _SeriesDetailPocetOdDoLabel;
        DXE.TextEdit _SeriesDetailPocetOdText;
        DXE.TextEdit _SeriesDetailPocetDoText;
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
        /// Obsahuje změny?
        /// </summary>
        public bool ContainChanges
        {
            get { return _ContainChanges; }
            protected set
            {
                if (!_DataRefreshRunning && value && !_ContainChanges)
                {
                    _ContainChanges = true;
                    OnDataChanged();
                }
            }
        }
        private bool _ContainChanges;
        private bool _DataRefreshRunning;
        /// <summary>
        /// Je voláno po první změně, která je provedena do "čistého" objektu
        /// </summary>
        protected virtual void OnDataChanged()
        {
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Je voláno po první změně, která je provedena do "čistého" objektu
        /// </summary>
        public event EventHandler DataChanged;
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
            SeriesListRefreshData(true);
            SeriesDetailRefresh();

            _ContainChanges = false;
            _DataRefreshRunning = false;
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
        public string PocetOd { get { return _PocetOd; } } private string _PocetOd;
        [DisplayName("Počet obyvatel DO")]
        public string PocetDo { get { return _PocetDo; } } private string _PocetDo;

        public GraphSerieInfo Serie { get; set; }
        protected DatabaseInfo Database { get; set; }

        internal static GraphSerieGridRow Create(GraphSerieInfo serie, DatabaseInfo database)
        {
            if (serie == null || database == null) return null;
            GraphSerieGridRow gridRow = new GraphSerieGridRow();
            gridRow.Serie = serie;
            gridRow.Database = database;
            gridRow._EntityText = (database.GetEntity(serie.DataEntityCode)?.Text ?? "");
            gridRow._ValueTypeName = serie.ValueTypeInfo?.Text;
            gridRow._PocetOd = _GetPocet(serie.FiltrPocetObyvatelOd);
            gridRow._PocetDo = _GetPocet(serie.FiltrPocetObyvatelDo);
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
