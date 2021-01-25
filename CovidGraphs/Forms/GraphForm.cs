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
            panel.Height = 40;
            _ButtonPanel = panel;
            this.Controls.Add(panel);

            DXE.SimpleButton button1 = new DXE.SimpleButton() { Text = "Uložit", Size = new Size(140, 33) };
            button1.Click += ButtonSave_Click;
            this._ButtonPanel.Controls.Add(button1);
            _Button1 = button1;

            DXE.SimpleButton button2 = new DXE.SimpleButton() { Text = "Uložit jako nový", Size = new Size(140, 33) };
            button2.Click += ButtonSaveAs_Click;
            this._ButtonPanel.Controls.Add(button2);
            _Button2 = button2;

            DXE.SimpleButton button3 = new DXE.SimpleButton() { Text = "Storno", Size = new Size(140, 33) };
            button3.Click += ButtonCancel_Click;
            this._ButtonPanel.Controls.Add(button3);
            _Button3 = button3;

            this.AcceptButton = _Button1;
            this.CancelButton = _Button3;

            _ButtonPanelLayout();
        }
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
            _GraphPanel = new GraphPanel() { Dock = DockStyle.Fill, CurrentGraphInfo = _CurrentGraphInfo };
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
            InitListObce();
            /*
            InitForm();
            InitFrames();
            InitRibbons();
            InitList();
            InitGraph();
            */
        }
        private void InitListObce()
        {
            _ObceListBox = new DXE.ListBoxControl()
            {
                MultiColumn = false,
                SelectionMode = SelectionMode.One,
                Dock = DockStyle.Fill

            };
            _ObceListBox.Appearance.FontSizeDelta = 1;

        }

        private DXE.ListBoxControl _ObceListBox;

        #region Data, Refresh, Store

        public void DataRefresh()
        { }
        public GraphInfo CurrentGraphInfo
        {
            get { return _CurrentGraphInfo; }
            set { _CurrentGraphInfo = value; }
        }
        private GraphInfo _CurrentGraphInfo;
        #endregion
    }
}
