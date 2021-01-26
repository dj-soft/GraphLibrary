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
            this.Controls.Add(InitFrames());

            _GraphSeriesNewHost.Controls.Add(InitSeriesNewControl());
            _GraphSeriesListHost.Controls.Add(InitSeriesListControl());

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
        #region Rozvržení layoutu
        private WF.Control InitFrames()
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
        private WF.Control InitSeriesNewControl()
        {
            _SeriesNewPanel = new DXE.PanelControl() { Dock = DockStyle.Fill };

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

            _SeriesNewSplitContainer.Panel1.Controls.Add(InitPanelObce());
            _SeriesNewSplitContainer.Panel2.Controls.Add(InitPanelValueType());


            _SeriesNewButtonsPanel = new DXE.PanelControl() { Dock = DockStyle.Bottom, Height = GraphForm.DefaultButtonPanelHeight };
            _SeriesNewAddButton = new DXE.SimpleButton() { Text = "Přidej jako další serie", Size = new Size(210, GraphForm.DefaultButtonHeight) };
            _SeriesNewButtonsPanel.Controls.Add(_SeriesNewAddButton);
            _SeriesNewButtonsPanel.SizeChanged += _SeriesNewButtonsPanel_SizeChanged;

            _SeriesNewPanel.Controls.Add(_SeriesNewButtonsPanel);

            return _SeriesNewPanel;
        }

        private void _SeriesNewSplitContainer_SizeChanged(object sender, EventArgs e)
        {
        }

        private void _SeriesNewButtonsPanel_SizeChanged(object sender, EventArgs e)
        {
            Size buttonSize = _SeriesNewAddButton.Size;
            Size panelSize = _SeriesNewButtonsPanel.ClientSize;
            Point location = new Point((panelSize.Width - buttonSize.Width) / 2, (panelSize.Height - buttonSize.Height) / 2);
            _SeriesNewAddButton.Bounds = new Rectangle(location, buttonSize);
        }

        private DXE.PanelControl _SeriesNewPanel;
        private DXE.SplitContainerControl _SeriesNewSplitContainer;
        private DXE.PanelControl _SeriesNewButtonsPanel;
        private DXE.SimpleButton _SeriesNewAddButton;
        #endregion
        #region Seznam s obcemi = zdroj entit
        private WF.Control InitPanelObce()
        {
            _ObcePanel = new DXE.PanelControl() { Dock = DockStyle.Fill };
            this.Controls.Add(_ObcePanel);

            _ObceSearchLabel = new DXE.LabelControl() { Bounds = new Rectangle(3, 3, 200, 25), Text = "Vyhledat obec:" };
            _ObcePanel.Controls.Add(_ObceSearchLabel);

            _ObceSearchText = new DXE.TextEdit() { Bounds = new Rectangle(3, 22, 200, 25) };
            _ObceSearchText.SuperTip = new DevExpress.Utils.SuperToolTip();
            _ObceSearchText.SuperTip.Items.AddTitle("Vyhledat obec:");
            _ObceSearchText.SuperTip.Items.Add(@"Zadejte počátek názvu, budou nabídnuty všechny obce s tímto začátkem.
Zadejte hvězdičku a část názvu, budou nalezeny všechny obce obsahující ve jménu daný text.
Zadejte na začátek textu výraz kraj: (nebo okres: nebo město: nebo obec:), a budou vypsány pouze odpovídající jednotky.
Po zadání tohoto prefixu nemusíte psát další text, budou vypsány všechny kraje (okresy, města, obce).

Následně si vyberete pouze patřičné obce ze seznamu.");
            _ObceSearchText.KeyUp += _ObceSearchText_KeyUp;
            _ObcePanel.Controls.Add(_ObceSearchText);
            _ObceLastSearchText = "";

            _ObceSearchButton = new DXE.SimpleButton() { Bounds = new Rectangle(206, 22, 50, 25), Text = "Vyhledat" };
            _ObceSearchButton.SuperTip = new DevExpress.Utils.SuperToolTip();
            _ObceSearchButton.SuperTip.Items.AddTitle("Vyhledat v databázi");
            _ObceSearchButton.SuperTip.Items.Add("Po zadání textu vlevo stiskněte toto tlačítko.");
            _ObceSearchButton.Click += _ObceSearchButton_Click;
            _ObceSearchButton.Visible = false;
            _ObcePanel.Controls.Add(_ObceSearchButton);

            _ObceListBox = new DXE.ListBoxControl()
            {
                Bounds = new Rectangle(3, 50, 240, 300),
                MultiColumn = false,
                SelectionMode = SelectionMode.MultiExtended,
                Dock = DockStyle.None
            };
            _ObceListBox.Appearance.FontSizeDelta = 1;
            _ObceListBox.SelectedIndex = 0;
            _ObcePanel.Controls.Add(_ObceListBox);

            this._ObcePanelLayout();

            this._ObcePanel.SizeChanged += _ObcePanel_SizeChanged;

            return _ObcePanel;
        }
        private void _ObcePanel_SizeChanged(object sender, EventArgs e)
        {
            this._ObcePanelLayout();
        }
        private void _ObceSearchText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Enter)
                _ObceListBox.Focus();
            else if (e.KeyCode == Keys.Home || e.KeyCode == Keys.End || e.KeyCode == Keys.Up /* || e.KeyCode == Keys.Down */ || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Tab || e.KeyCode == Keys.Escape)
            { }
            else if (e.Modifiers == Keys.Control)
            { }
            else
                ObceRunSearch();
        }
        private void _ObceSearchButton_Click(object sender, EventArgs e)
        {
            ObceRunSearch();
            this._ObceSearchText.Focus();
        }
        private void _ObcePanelLayout()
        {
            if (_ObceListBox == null) return;

            int mx = 3;
            int my = 3;
            int sx = 2;
            int sy = 2;
            Size size = _ObcePanel.ClientSize;
            bool btnVisible = _ObceSearchButton.Visible;
            int btnWidth = (btnVisible ? _ObceSearchButton.GetPreferredSize(new Size(200, 80)).Width + 16 : 0);
            int inpWidth = size.Width - mx - btnWidth - mx - (btnVisible ? sx : 0);
            _ObceSearchLabel.Bounds = new Rectangle(mx, my, inpWidth, 20);
            _ObceSearchText.Bounds = new Rectangle(mx, _ObceSearchLabel.Bounds.Bottom + sy, inpWidth, 25);
            int inpBottom = _ObceSearchText.Bounds.Bottom;
            if (btnVisible)
            {
                int btnX = _ObceSearchText.Right + sx;
                int btnY = my + 4;
                _ObceSearchButton.Bounds = new Rectangle(btnX, btnY, size.Width - mx - btnX, inpBottom - btnY);
            }

            _ObceListBox.Bounds = new Rectangle(mx, inpBottom + sy, size.Width - mx - mx, size.Height - my - inpBottom - sy);
        }
        private void ObceRunSearch()
        {
            if (this.Database == null) return;

            string newText = _ObceSearchText.Text.Trim();
            string oldText = _ObceLastSearchText;
            if (String.Equals(newText, oldText, StringComparison.CurrentCultureIgnoreCase)) return;
            _ObceLastSearchText = newText;

            this._ObceListBox.Items.Clear();
            if (newText.Length < 2) return;

            var entites = this.Database.SearchEntities(newText);
            if (entites.Length > 0)
            {
                this._ObceListBox.Items.AddRange(entites);
                if (!this._ObceListBox.Enabled)
                    this._ObceListBox.Enabled = true;
            }
            else
            {
                this._ObceListBox.Items.Add("Nenalezeno");
                if (this._ObceListBox.Enabled)
                    this._ObceListBox.Enabled = false;
            }
        }
        private string _ObceLastSearchText;
        private DXE.PanelControl _ObcePanel;
        private DXE.LabelControl _ObceSearchLabel;
        private DXE.SimpleButton _ObceSearchButton;
        private DXE.TextEdit _ObceSearchText;
        private DXE.ListBoxControl _ObceListBox;
        #endregion
        #region Seznam s datovými typy
        private WF.Control InitPanelValueType()
        {
            _ValueTypePanel = new DXE.PanelControl() { Dock = DockStyle.Fill };
            _ValueTypePanel.SizeChanged += _ValueTypePanel_SizeChanged;

            _ValueTypeLabel = new DXE.LabelControl() { Text = "Označte jeden nebo více typů dat:", BorderStyle = DXE.Controls.BorderStyles.NoBorder };
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

            _ValueTypePanel.Controls.Add(_ValueTypeListBox);


            _ValueTypeInfo = new DXE.LabelControl() { Text = "", AutoSizeMode = DXE.LabelAutoSizeMode.Vertical, BorderStyle = DXE.Controls.BorderStyles.NoBorder };
            _ValueTypeInfo.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            _ValueTypeInfo.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
            _ValueTypeInfo.Appearance.Options.UseTextOptions = true;
            _ValueTypePanel.Controls.Add(_ValueTypeListBox);

            _ValueTypeLayout();
            return _ValueTypePanel;
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
            int x = mx;
            int w = size.Width - mx - mx;

            int y = my;
            _ValueTypeLabel.Bounds = new Rectangle(x, y, w, 20); y = _ValueTypeLabel.Bounds.Bottom + sy;

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
        private WF.Control InitSeriesListControl()
        {
            _SeriesListPanel = new DXE.PanelControl() { Dock = DockStyle.Fill };

            _SeriesListGrid = new DXG.GridControl() { Dock = DockStyle.Fill };
            // _SeriesListGrid.DataSource
            _SeriesListPanel.Controls.Add(_SeriesListGrid);

            _SeriesListButtonPanel = new DXE.PanelControl() { Dock = DockStyle.Top, Height = GraphForm.DefaultButtonPanelHeight };
            _SeriesListPanel.Controls.Add(_SeriesListButtonPanel);

            _SeriesListRemoveButton = new DXE.SimpleButton() { Text = "Odeber řádek", Size = new Size(120, GraphForm.DefaultButtonHeight), Location = new Point(8, 3) };
            _SeriesListButtonPanel.Controls.Add(_SeriesListRemoveButton);

            return _SeriesListPanel;
        }
        private DXE.PanelControl _SeriesListPanel;
        private DXE.PanelControl _SeriesListButtonPanel;
        private DXG.GridControl _SeriesListGrid;
        private DXE.SimpleButton _SeriesListRemoveButton;
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
}
