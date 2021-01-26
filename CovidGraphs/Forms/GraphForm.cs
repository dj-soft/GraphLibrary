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

            Rectangle? configBounds = Data.App.Config.EditFormBounds;
            if (!configBounds.HasValue)
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
            else
            {
                if (configBounds.Value.Width <= 0 || configBounds.Value.Height <= 0)
                    this.WindowState = FormWindowState.Maximized;
                else
                {
                    this.WindowState = FormWindowState.Normal;
                    this.StartPosition = FormStartPosition.Manual;
                    if (parentBounds.HasValue)
                        this.Bounds = configBounds.Value.AlignTo(parentBounds.Value);
                    else
                        this.Bounds = configBounds.Value;
                }
            }
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!IsShown)
                OnFirstShown();
            this.IsShown = true;
        }
        protected void OnFirstShown()
        {
            _MainSplitContainer.SplitterPosition = Data.App.Config.EditFormMainSplitter;
            _MainSplitContainer.SplitterPositionChanged += _MainSplitContainer_SplitterPositionChanged;

            _GraphPanel.OnFirstShown();
        }
        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            StoreFormBoundsToConfig();
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            StoreFormBoundsToConfig();
        }
        protected void StoreFormBoundsToConfig()
        {
            if (IsShown)
            {
                switch (this.WindowState)
                {
                    case FormWindowState.Maximized:
                        Data.App.Config.EditFormBounds = Rectangle.Empty;
                        break;
                    case FormWindowState.Normal:
                        Data.App.Config.EditFormBounds = this.Bounds;
                        break;
                }
            }
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
        /// Po přesunutí Splitteru uložím jeho pozici do Configu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MainSplitContainer_SplitterPositionChanged(object sender, EventArgs e)
        {
            Data.App.Config.EditFormMainSplitter = _MainSplitContainer.SplitterPosition;
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
        /// <summary>
        /// Volá se těsně před prvním zobrazením. Layout formuláře (rozměry a splittery) je nastaven.
        /// Zdejší objekt si má z konfigurace načíst svůj layout a aplikovat jej do Controlů.
        /// </summary>
        internal void OnFirstShown()
        {
            var configLayout = Data.App.Config.EditFormGraphPanelLayout;

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
        #region Rozvržení layoutu
        private WF.Control CreateControlForFrames()
        {
            // Main splitter
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

            // Horní panel obsadí TabPage se záložkami pro hlavičku a pro nové položky:
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

            // Dolní panel obsadí splitter, obsahující v Panel1 = Seznam položek, a v Panel2 = detail jedné položky:
            _SeriesSplitContainer = new DXE.SplitContainerControl()
            {
                FixedPanel = DXE.SplitFixedPanel.Panel2,
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
            _SeriesListData.Add(new GraphSerieGridRow() { Entita = "Město 1", Hodnota = "Dnešní počet" });
            _SeriesListData.Add(new GraphSerieGridRow() { Entita = "Město 2", Hodnota = "Dnešní počet" });
            _SeriesListData.Add(new GraphSerieGridRow() { Entita = "Obec 3", Hodnota = "Včerejší počet" });
            _SeriesListData.Add(new GraphSerieGridRow() { Entita = "Obec 4", Hodnota = "Včerejší počet" });
            _SeriesListData.Add(new GraphSerieGridRow() { Entita = "Vesnička 1", Hodnota = "Zítřejší počet" });

            _SeriesListGrid = new DXG.GridControl() { Dock = DockStyle.Fill };
            _SeriesListGrid.DataSource = _SeriesListData;
            _SeriesListGrid.RefreshDataSource();
            _SeriesListGrid.Refresh();
            _SeriesListGrid.MainView = _SeriesListGrid.AvailableViews[1].CreateView(_SeriesListGrid); // .vie .CreateView(_SeriesListGrid).SourceView;
            var view = _SeriesListGrid.MainView as DXG.Views.Grid.GridView;

            var columns = view.Columns;

            view.PopulateColumns();
            view.BorderStyle = DXE.Controls.BorderStyles.NoBorder;
            view.DetailTabHeaderLocation = DXT.TabHeaderLocation.Left;
            // _SeriesListGrid.DataMember = "Entita";

            /*
            string layout = "";
            using (System.IO.MemoryStream sw = new System.IO.MemoryStream())
            {
                view.Appearance.SaveLayoutToStream(sw);
                var bytes = sw.ToArray();
                layout = Encoding.UTF8.GetString(bytes);
            }
            */

            // _SeriesListGrid.MainView.RefreshData();
            // _SeriesListGrid.MainView.PopulateColumns();
            // _SeriesListGrid.MainView



            _SeriesListPanel.Controls.Add(_SeriesListGrid);

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

        private void _SeriesListAddButton_Click(object sender, EventArgs e)
        {
            _SeriesListAddNewSeries();
        }
        private void _SeriesListAddNewSeries()
        {
            int requestPage = 1;
            if (_TabContainer.SelectedTabPageIndex != requestPage)
            {
                _TabContainer.SelectedTabPageIndex = requestPage;
                string text = $@"Na stránce '{_TabContainer.TabPages[requestPage].Text}' najděte místa (obce) a vyberte data pro zobrazení, a pak teprve stiskněte tlačítko '{_SeriesListAddButton.Text}'.";
                App.ShowWarning(this, text);
                return;
            }

            var entityItems = _EntityListBox.SelectedItems.OfType<IEntity>().ToArray();
            bool hasEntities = (entityItems != null && entityItems.Length > 0);
            var valueItems = _ValueTypeListBox.SelectedItems.OfType<DataValueTypeInfo>().ToArray();
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
                return;
            }


        }

        private void _SeriesListRemoveButton_Click(object sender, EventArgs e)
        {
            
        }

        private DXE.PanelControl _SeriesListPanel;
        private DXE.PanelControl _SeriesListButtonPanel;
        private DXG.GridControl _SeriesListGrid;
        private DXE.SimpleButton _SeriesListAddButton;
        private DXE.SimpleButton _SeriesListRemoveButton;
        private List<GraphSerieGridRow> _SeriesListData;
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

    public class GraphSerieGridRow
    {
        public string Entita { get; set; }
        public string Hodnota { get; set; }
    }
}
