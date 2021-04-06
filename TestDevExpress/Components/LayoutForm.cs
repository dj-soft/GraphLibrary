using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using WF = System.Windows.Forms;


namespace TestDevExpress.Components
{
    public class LayoutForm : DevExpress.XtraEditors.XtraForm
    {
        public LayoutForm() : this(false)
        { }
        public LayoutForm(bool useDevExpress)
        {
            _LayoutPanel = new DxLayoutPanel() { Dock = System.Windows.Forms.DockStyle.Fill };
            _LayoutPanel.LastControlRemoved += _LayoutPanel_LastControlRemoved;
            this.Controls.Add(_LayoutPanel);

            Rectangle monitorBounds = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            Rectangle formBounds = new Rectangle(monitorBounds.X + monitorBounds.Width * 1 / 10, monitorBounds.Y + monitorBounds.Height * 1 / 10, monitorBounds.Width * 8 / 10, monitorBounds.Height * 8 / 10);

            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Bounds = formBounds;
        }

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
    }

    public class LayoutTestPanel : System.Windows.Forms.Panel // DevExpress.XtraEditors.PanelControl
    {
        public LayoutTestPanel()
        {
            this.Dock = System.Windows.Forms.DockStyle.Fill;

            Size buttonSize = ButtonSize;

            Id = ++LastPanelId;
            string title = "Panel číslo " + Id.ToString();
            _Title = new DevExpress.XtraEditors.LabelControl() { Text = title, Size = new Size(140, 25) };
            _Title.Appearance.FontSizeDelta = 2;
            _Title.Appearance.FontStyleDelta = FontStyle.Bold;
            _Title.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            _Title.Appearance.Options.UseTextOptions = true;
            this.Controls.Add(_Title);

            _CloseButton = CreateDxButton("ZAVŘÍT tento panel", LayoutPosition.None);
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
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.SetVisibleButtons(false, true);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            Point mousePoint = this.PointToClient(WF.Control.MousePosition);
            bool isMouseOnPanel = this.ClientRectangle.Contains(mousePoint);             // MouseLeave: myš mohla odejít i na naše Child controly, ale pak se nejedná o opuštění našeho controlu.
            this.SetVisibleButtons(false, isMouseOnPanel);
        }
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            this.SetVisibleButtons(true, true);
        }
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            this.SetVisibleButtons(true, false);
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            this.SetVisibleButtons(true, true);
        }
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            this.SetVisibleButtons(true, false);
        }
        protected void ResetVisibleButtons()
        {
            this._HasFocus = false;
            this._HasMouse = false;
            ShowVisibleButton();
        }
        protected void SetVisibleButtons(bool keyboardFocus, bool visible)
        {
            if (keyboardFocus)
                this._HasFocus = visible;
            else
                this._HasMouse = visible;
            ShowVisibleButton();
        }
        protected void ShowVisibleButton()
        {
            bool isVisible = this._HasFocus || this._HasMouse;
            _CloseButton.Visible = isVisible;
            _AddRightButton.Visible = isVisible;
            _AddBottomButton.Visible = isVisible;
            _AddLeftButton.Visible = isVisible;
            _AddTopButton.Visible = isVisible;
        }
        private bool _HasFocus = false;
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

        DevExpress.XtraEditors.LabelControl _Title;
        DevExpress.XtraEditors.SimpleButton _CloseButton;
        DevExpress.XtraEditors.SimpleButton _AddRightButton;
        DevExpress.XtraEditors.SimpleButton _AddBottomButton;
        DevExpress.XtraEditors.SimpleButton _AddLeftButton;
        DevExpress.XtraEditors.SimpleButton _AddTopButton;
        /// <summary>
        /// Viditelnost buttonu Close
        /// </summary>
        public bool CloseButtonVisible { get { return _CloseButton?.Visible ?? false; } set { if (_CloseButton != null) _CloseButton.Visible = value; } }

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

            _Title.Location = new Point((cw - _Title.Width) / 2, (dh / 2) - 35);
            _CloseButton.Location = new Point(dw / 2, dh / 2);                 // Uprostřed
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
            if (position == LayoutPosition.None)
                LayoutPanel?.RemoveControl(this);
            else
            {
                int size = ((position == LayoutPosition.Left || position == LayoutPosition.Right) ? ButtonSize.Width * 4 : ButtonSize.Height * 6);
                LayoutTestPanel newPanel = new LayoutTestPanel();

                float ratio = 0.4f;
                LayoutPanel.AddControl(newPanel, this, position, currentSizeRatio: ratio);          // currentSize: size);
                this.ResetVisibleButtons();
                newPanel.SetVisibleButtons(true, true);
                newPanel._CloseButton.Focus();
                newPanel.SetVisibleButtons(true, true);
            }
        }
    }
}
