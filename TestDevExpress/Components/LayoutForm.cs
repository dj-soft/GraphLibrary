using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using WF = System.Windows.Forms;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Components
{
    public class LayoutForm : DevExpress.XtraEditors.XtraForm
    {
        public LayoutForm() : this(false)
        { }
        public LayoutForm(bool useDevExpress)
        {
            _LayoutPanel = new DxLayoutPanel() 
            {
                Dock = System.Windows.Forms.DockStyle.Fill, 
                SplitterContextMenuEnabled = true, 
                DockButtonVisibility = ControlVisibility.OnMouse,
                CloseButtonVisibility = ControlVisibility.OnNonPrimaryPanelAllways,
                UseSvgIcons = false
            };
            _LayoutPanel.LastControlRemoved += _LayoutPanel_LastControlRemoved;
            _LayoutPanel.SplitterPositionChanged += _LayoutPanel_SplitterPositionChanged;
            _LayoutPanel.LayoutPanelChanged += _LayoutPanel_LayoutPanelChanged;

            this.Controls.Add(_LayoutPanel);

            Rectangle monitorBounds = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            Rectangle formBounds = new Rectangle(monitorBounds.X + monitorBounds.Width * 1 / 10, monitorBounds.Y + monitorBounds.Height * 1 / 10, monitorBounds.Width * 8 / 10, monitorBounds.Height * 8 / 10);

            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Bounds = formBounds;

            this._Timer = new Timer() { Interval = 1800 };
            this._Timer.Tick += _Timer_Tick;
            this._Timer.Enabled = false;
        }
        /// <summary>
        /// Po změně layoutu (pozice prvků)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LayoutPanel_LayoutPanelChanged(object sender, DxLayoutPanelSplitterChangedArgs e)
        {
            _ShowLayout(e);
        }
        /// <summary>
        /// Po změně pozice splitteru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LayoutPanel_SplitterPositionChanged(object sender, DxLayoutPanelSplitterChangedArgs e)
        {
            _ShowLayout(e);
        }
        private void _ShowLayout(DxLayoutPanelSplitterChangedArgs e)
        {
            LayoutTestPanel panel1 = e.Control1 as LayoutTestPanel;
            LayoutTestPanel panel2 = e.Control2 as LayoutTestPanel;
            var orientation = e.SplitterOrientation;
            var position = e.SplitterPosition;
        }
        /// <summary>
        /// Občas změním titulek některého panelu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Timer_Tick(object sender, EventArgs e)
        {
            this._Timer.Enabled = false;

            // Najdeme si náhodně nějaký panel:
            var controls = _LayoutPanel.AllControls;
            int count = controls.Length;
            if (count > 0)
            {
                Random rand = new Random();
                LayoutTestPanel testPanel = controls[rand.Next(count)] as LayoutTestPanel;
                if (testPanel != null)
                {

                    string title = testPanel.TitleText;
                    if (rand.Next(10) > 3)
                    {
                        string[] suffix = "A;B;C;D;E;F;G;H;I;J;K;L;M;N;O;P;Q".Split(';');
                        title = title + " [" + suffix[rand.Next(suffix.Length)] + rand.Next(10, 100).ToString() + "]";
                    }

                    _LayoutPanel.UpdateTitle(testPanel, title);

                    this._Timer.Interval = rand.Next(700, 4500);
                }
            }

            this._Timer.Enabled = true;
        }
        /// <summary>
        /// Chtěl bych zavřít formulář
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            this._LayoutPanel.RemoveAllControls();
            if (this._LayoutPanel.ControlCount > 0)
                e.Cancel = true;
        }
        /// <summary>
        /// Někdo zavřel poslední panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LayoutPanel_LastControlRemoved(object sender, EventArgs e)
        {
            this.Close();
        }

        public void AddControl(WF.Control control)
        {
            _LayoutPanel.AddControl(control);
        }
        private DxLayoutPanel _LayoutPanel;
        /// <summary>
        /// Panel layoutu
        /// </summary>
        public DxLayoutPanel LayoutPanel { get { return _LayoutPanel; } }

        System.Windows.Forms.Timer _Timer;
    }

    public class LayoutTestPanel : System.Windows.Forms.Panel, ILayoutUserControl // DevExpress.XtraEditors.PanelControl
    {
        public LayoutTestPanel()
        {
            this.Dock = System.Windows.Forms.DockStyle.Fill;

            Size buttonSize = ButtonSize;

            Id = ++LastPanelId;
            this.TitleText = "Panel číslo " + Id.ToString();

            _AddRightButton = CreateDxButton("Otevřít další VPRAVO", LayoutPosition.Right);
            _AddBottomButton = CreateDxButton("Otevřít další DOLE", LayoutPosition.Bottom);
            _AddLeftButton = CreateDxButton("Otevřít další VLEVO", LayoutPosition.Left);
            _AddTopButton = CreateDxButton("Otevřít další NAHOŘE", LayoutPosition.Top);

            ShowVisibleButton();

            Random rand = new Random();
            int r = rand.Next(160, 240);
            int g = rand.Next(160, 240);
            int b = rand.Next(160, 240);
            this.MyColor1 = Color.FromArgb(r, g, b);
            this.MyColor2 = Color.FromArgb(r + 16, g + 16, b + 16);
            this.BackColor = this.MyColor1;
            //this.Appearance.BackColor = this.MyColor1;
            //this.Appearance.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
            //this.Appearance.BackColor2 = this.MyColor2;
            //this.Appearance.Options.UseBackColor = true;
            //this.LookAndFeel.UseDefaultLookAndFeel = false;

        }
        public string TitleText 
        {
            get { return _TitleText; }
            set { _TitleText = value; TitleTextChanged?.Invoke(this, EventArgs.Empty); }
        }
        public event EventHandler TitleTextChanged;
        private string _TitleText;
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.SetVisibleButtons(true);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            DetectVisibleButtons();
        }
        protected void DetectVisibleButtons()
        {
            Point mousePoint = this.PointToClient(WF.Control.MousePosition);
            bool isMouseOnPanel = this.ClientRectangle.Contains(mousePoint);             // MouseLeave: myš mohla odejít i na naše Child controly, ale pak se nejedná o opuštění našeho controlu.
            this.SetVisibleButtons(isMouseOnPanel);
        }
        protected void SetVisibleButtons(bool visible)
        {
            this._HasMouse = visible;
            ShowVisibleButton();
        }
        protected void ShowVisibleButton()
        {
            bool isVisible = this._HasMouse;
            _AddRightButton.Visible = isVisible;
            _AddBottomButton.Visible = isVisible;
            _AddLeftButton.Visible = isVisible;
            _AddTopButton.Visible = isVisible;
        }
        private bool _HasMouse = false;

        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"TestPanel Id: {Id}";
        }
        protected int Id { get; private set; }
        protected static int LastPanelId = 0;

        protected Color MyColor1;
        protected Color MyColor2;

        // DevExpress.XtraEditors.LabelControl _Title;
        // DevExpress.XtraEditors.SimpleButton _CloseButton;
        DevExpress.XtraEditors.SimpleButton _AddRightButton;
        DevExpress.XtraEditors.SimpleButton _AddBottomButton;
        DevExpress.XtraEditors.SimpleButton _AddLeftButton;
        DevExpress.XtraEditors.SimpleButton _AddTopButton;
        
        private DevExpress.XtraEditors.SimpleButton CreateDxButton(string text, LayoutPosition position)
        {
            var button = new DevExpress.XtraEditors.SimpleButton() { Text = text, Size = ButtonSize, Tag = position };
            button.Click += _AnyButton_Click;
            this.Controls.Add(button);
            return button;
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            var cs = this.ClientSize;
            int cw = cs.Width;
            int ch = cs.Height;

            var bs = ButtonSize;
            int bw = bs.Width;
            int bh = bs.Height;

            int mx = 12;
            int my = 9;
            int dw = cw - bw;
            int dh = ch - bh;

            _AddRightButton.Location = new Point(dw - mx, dh / 2);             // Vpravo, svisle uprostřed
            _AddBottomButton.Location = new Point(dw / 2, dh - my);            // Vodorovně uprostřed, dole
            _AddLeftButton.Location = new Point(mx, dh / 2);                   // Vlevo, svisle uprostřed
            _AddTopButton.Location = new Point(dw / 2, my);                    // Vodorovně uprostřed, nahoře
        }
        /// <summary>
        /// Obsahuje (najde) control, který řídí layout a vkládání nových prvků a odebírání existujících
        /// </summary>
        protected DxLayoutPanel LayoutPanel { get { return DxLayoutPanel.SearchParentLayoutPanel(this); } }
        /// <summary>
        /// Doporučená velikost buttonů
        /// </summary>
        protected Size ButtonSize { get { return new Size(120, 32); } }


        private void _AnyButton_Click(object sender, EventArgs e)
        {
            if (!(sender is DevExpress.XtraEditors.SimpleButton button)) return;
            if (!(button.Tag is LayoutPosition)) return;
            LayoutPosition position = (LayoutPosition)button.Tag;
            if (position == LayoutPosition.Left || position == LayoutPosition.Top || position == LayoutPosition.Bottom || position == LayoutPosition.Right)
            {
                int size = ((position == LayoutPosition.Left || position == LayoutPosition.Right) ? ButtonSize.Width * 4 : ButtonSize.Height * 6);
                LayoutTestPanel newPanel = new LayoutTestPanel();

                float ratio = 0.4f;
                LayoutPanel.AddControl(newPanel, this, position, currentSizeRatio: ratio);          // currentSize: size);
                this.DetectVisibleButtons();
                newPanel.DetectVisibleButtons();
            }
        }
        #region ILayoutUserControl implementace
        string ILayoutUserControl.TitleText { get { return this.TitleText; } }
        ControlVisibility ILayoutUserControl.CloseButtonVisibility { get { return ControlVisibility.Allways; } }
        event EventHandler ILayoutUserControl.TitleTextChanged { add { this.TitleTextChanged += value; } remove { this.TitleTextChanged -= value; } }
        #endregion
    }
}
