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
        protected bool IsShown;
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

            DXE.SimpleButton button1 = new DXE.SimpleButton() { Text = "Uložit", Size = new Size(DefaultButtonWidth, DefaultButtonHeight) };
            button1.Click += ButtonSave_Click;
            this._ButtonPanel.Controls.Add(button1);
            _Button1 = button1;

            DXE.SimpleButton button2 = new DXE.SimpleButton() { Text = "Uložit jako nový", Size = new Size(DefaultButtonWidth, DefaultButtonHeight) };
            button2.Click += ButtonSaveAs_Click;
            this._ButtonPanel.Controls.Add(button2);
            _Button2 = button2;

            DXE.SimpleButton button3 = new DXE.SimpleButton() { Text = "Storno", Size = new Size(DefaultButtonWidth, DefaultButtonHeight) };
            button3.Click += ButtonCancel_Click;
            this._ButtonPanel.Controls.Add(button3);
            _Button3 = button3;

            // this.AcceptButton = _Button1;
            this.CancelButton = _Button3;

            _ButtonPanelLayout();
        }
        internal const int DefaultButtonPanelHeight = 40;
        internal const int DefaultButtonWidth = 150;
        internal const int DefaultButtonHeight = 34;
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
            if (_Button3 == null) return;

            Size size = _ButtonPanel.ClientSize;

            int bW = 140;
            int bH = size.Height - 12;
            int margin = 12;
            int space = 9;
            int bX = size.Width - (3 * bW + 2 * space + margin);
            int bY = 3;
            _Button1.Bounds = new Rectangle(bX, bY, bW, bH);
            bX += bW + space;
            _Button2.Bounds = new Rectangle(bX, bY, bW, bH);
            bX += bW + space;
            _Button3.Bounds = new Rectangle(bX, bY, bW, bH);
        }
        protected void InitData()
        {
            _CurrentGraphInfo = new GraphInfo();
            _GraphPanel = new GraphPanel() { Dock = DockStyle.Fill, ParentForm = this };
            _MainSplitContainer.Panel1.Controls.Add(_GraphPanel);
        }
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
        DXE.SimpleButton _Button1;
        DXE.SimpleButton _Button2;
        DXE.SimpleButton _Button3;
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
        public Database Database
        {
            get { return _Database; }
            set
            {
                _Database = value;
                if (_Database != null)
                    this._DataRefresh();
            }
        }
        private Database _Database;
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
            //_GraphHeaderDetailHost.Controls.Add(CreateControlForHeaderDetail());
            _GraphSeriesNewHost.Controls.Add(CreateControlForNewSeries());
            _GraphSeriesListHost.Controls.Add(CreateControlForSeriesList());

            this.SizeChanged += Frame_SizeChanged;

            /*
            InitForm();
            InitFrames();
            InitRibbons();
            InitList();
            InitGraph();
            */
        }
        private void InitStyles()
        {
            _TitleStyle = new DXE.StyleController();
            _TitleStyle.Appearance.FontSizeDelta = 2;
            _TitleStyle.Appearance.FontStyleDelta = FontStyle.Regular;
            _TitleStyle.Appearance.Options.UseBorderColor = false;
            _TitleStyle.Appearance.Options.UseBackColor = false;
            _TitleStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

        }
        DXE.StyleController _TitleStyle;
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
        #region Hlavička grafu

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
        #region Seznam existujících serií
        private WF.Control CreateControlForSeriesList()
        {
            _SeriesListPanel = new DXE.PanelControl() { Dock = DockStyle.Fill, BorderStyle = DXE.Controls.BorderStyles.NoBorder };


            _SeriesListData = new List<GraphSerieGridRow>();
            _SeriesListData.Add(new GraphSerieGridRow() { Entita = "Město 1", Hodnota = "Dnešní počet", Name = "Město 1, dnes" });
            _SeriesListData.Add(new GraphSerieGridRow() { Entita = "Město 2", Hodnota = "Dnešní počet", Name = "Město 2, dnes" });
            _SeriesListData.Add(new GraphSerieGridRow() { Entita = "Obec 3", Hodnota = "Včerejší počet", Name = "Obec 3, včera" });
            _SeriesListData.Add(new GraphSerieGridRow() { Entita = "Obec 4", Hodnota = "Včerejší počet", Name = "Obec 4, včera" });
            _SeriesListData.Add(new GraphSerieGridRow() { Entita = "Vesnička 5", Hodnota = "Zítřejší počet", Name = "Vesnička 5, zítra" });

            var _SeriesListGridView = new DevExpress.XtraGrid.Views.Grid.GridView();
            _SeriesListGridView.OptionsBehavior.AutoPopulateColumns = false;
            _SeriesListGridView.OptionsBehavior.Editable = false;
            _SeriesListGridView.Columns.Add(new DXG.Columns.GridColumn() { FieldName = nameof(GraphSerieGridRow.Name), Caption = "Název dat", Visible = true, Width = 120 });
            _SeriesListGridView.Columns.Add(new DXG.Columns.GridColumn() { FieldName = nameof(GraphSerieGridRow.Entita), Caption = "Obec/město/okres", Visible = true, Width = 250 });
            _SeriesListGridView.Columns.Add(new DXG.Columns.GridColumn() { FieldName = nameof(GraphSerieGridRow.Hodnota), Caption = "Zobrazená data", Visible = true, Width = 120 });
            _SeriesListGridView.OptionsView.BestFitMode = DXG.Views.Grid.GridBestFitMode.Fast;


            // _SeriesListGridView.OptionsBehavior.EditingMode = DXG.Views.Grid.GridEditingMode.
            _SeriesListGridView.OptionsBehavior.AllowAddRows = DevExpress.Utils.DefaultBoolean.False;
            _SeriesListGridView.OptionsBehavior.AllowDeleteRows = DevExpress.Utils.DefaultBoolean.False;
            _SeriesListGridView.OptionsBehavior.AllowIncrementalSearch = true;
            _SeriesListGridView.OptionsSelection.MultiSelectMode = DXG.Views.Grid.GridMultiSelectMode.RowSelect;
            _SeriesListGridView.Appearance.HeaderPanel.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;


            _SeriesListGrid = new DXG.GridControl() { Dock = DockStyle.Fill };
            _SeriesListGrid.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { _SeriesListGridView });
            _SeriesListGrid.MainView = _SeriesListGridView;
            _SeriesListGridView.GridControl = _SeriesListGrid;

            _SeriesListPanel.Controls.Add(_SeriesListGrid);


            _SeriesListGrid.DataSource = _SeriesListData; //  GraphSerieGridRow.GetDataGraph();
            var gwc = _SeriesListGridView.Columns;
            _SeriesListGridView.ColumnChanged += _SeriesListGridView_ColumnChanged;
            _SeriesListGridView.DataSourceChanged += _SeriesListGridView_DataSourceChanged;




            _SeriesListButtonPanel = new DXE.PanelControl() { Dock = DockStyle.Top, BorderStyle = DXE.Controls.BorderStyles.NoBorder, Height = GraphForm.DefaultButtonPanelHeight };
            _SeriesListPanel.Controls.Add(_SeriesListButtonPanel);

            _SeriesListAddButton = new DXE.SimpleButton() { Text = "Přidej řádky", Size = new Size(120, GraphForm.DefaultButtonHeight), Location = new Point(8, 3) };
            _SeriesListAddButton.Click += _SeriesListAddButton_Click;
            _SeriesListButtonPanel.Controls.Add(_SeriesListAddButton);
            _SeriesListRemoveButton = new DXE.SimpleButton() { Text = "Odeber řádky", Size = new Size(120, GraphForm.DefaultButtonHeight), Location = new Point(132, 3) };
            _SeriesListRemoveButton.Click += _SeriesListRemoveButton_Click;
            _SeriesListButtonPanel.Controls.Add(_SeriesListRemoveButton);

            return _SeriesListPanel;
        }

        private void _SeriesListGridView_DataSourceChanged(object sender, EventArgs e)
        {
            
        }

        private void _SeriesListGridView_ColumnChanged(object sender, EventArgs e)
        {
            
        }

        private void _SeriesListAddButton_Click(object sender, EventArgs e)
        {
            _SeriesListAddNewSeries();
        }
        private void _SeriesListRemoveButton_Click(object sender, EventArgs e)
        {

        }

        private void _SeriesListAddNewSeries()
        {
            var entityItems = _EntityListBox.SelectedItems.OfType<IEntity>().ToArray();
            var valueItems = _ValueTypeListBox.SelectedItems.OfType<DataValueTypeInfo>().ToArray();
            if (!_HasValidDataForNewSeries(entityItems, valueItems))
                return;

           


        }
        /// <summary>
        /// Ověří, zda aktuálně můžeme zadaná data přidat jako nové serie. Pokud ne, oznámí uživateli problém a vrátí false.
        /// </summary>
        /// <param name="entityItems"></param>
        /// <param name="valueItems"></param>
        /// <returns></returns>
        private bool _HasValidDataForNewSeries(IEntity[] entityItems, DataValueTypeInfo[] valueItems)
        {
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

                App.ShowWarning(this, text);
                return false;
            }
            return true;
        }


        private DXE.PanelControl _SeriesListPanel;
        private DXE.PanelControl _SeriesListButtonPanel;
        private DXG.GridControl _SeriesListGrid;
        private DXE.SimpleButton _SeriesListAddButton;
        private DXE.SimpleButton _SeriesListRemoveButton;
        private List<GraphSerieGridRow> _SeriesListData;
        #endregion
        #region Správa layoutu - splittery, uložení konfigurace
        /// <summary>
        /// Volá se těsně před prvním zobrazením. Layout formuláře (rozměry a splittery) je nastaven.
        /// Zdejší objekt si má z konfigurace načíst svůj layout a aplikovat jej do Controlů.
        /// </summary>
        internal void OnFirstShown()
        {
            RestoreSplitters();
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
        public Database Database { get { return ParentForm?.Database; } }
        public GraphInfo CurrentGraphInfo { get { return ParentForm?.CurrentGraphInfo; } }
        public void DataRefresh()
        {
            Database database = this.Database;
            GraphInfo graphInfo = this.CurrentGraphInfo;


        }
        #endregion
    }

    public class GraphSerieGridRow : INotifyPropertyChanged
    {
        [DisplayName("Název informace")]
        public string Name { get { return _Name; } set { _Set(ref _Name, value); } } private string _Name;
        [DisplayName("Obec (město, okres, kraj)")]
        public string Entita { get { return _Entita; } set { _Set(ref _Entita, value); } } private string _Entita;
        [DisplayName("Druh zobrazených dat")]
        public string Hodnota { get { return _Hodnota; } set { _Set(ref _Hodnota, value); } } private string _Hodnota;


        public static System.Data.DataTable GetDataTable()
        {
            System.Data.DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("popis") { Caption = "Uživatelský popis", DataType = typeof(string) });
            table.Columns.Add(new DataColumn("entita") { Caption = "Město", DataType = typeof(string) });
            table.Columns.Add(new DataColumn("hodnota") { Caption = "Hodnota dat", DataType = typeof(string) });

            table.Rows.Add("Chrudim, denní nové", "Chrudim", "Denní počet");
            table.Rows.Add("pardubice, denní nové", "pardubice", "Denní počet");
            table.Rows.Add("HraKra, denní nové", "Hradec", "Denní počet");

            return table;
        }

        public static BindingList<GraphSerieGridRow> GetDataGraph()
        {
            BindingList<GraphSerieGridRow> records = new BindingList<GraphSerieGridRow>();
            records.Add(new GraphSerieGridRow() { Name = "Praha, denní stav", Entita = "Praha (obec, 1M pražáků)", Hodnota = "Počet" });
            return records;
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
}
