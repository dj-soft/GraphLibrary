using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestMaxControl
{
    public partial class TestMaxControlsForm : Form
    {
        public TestMaxControlsForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.NumCtrlLbl.Text = this.NumCtrlTrack.Value.ToString();
        }

        private void NumCtrlTrack_ValueChanged(object sender, EventArgs e)
        {
            this.NumCtrlLbl.Text = this.NumCtrlTrack.Value.ToString();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            this.DisposeTestPanel();
        }

        private void RunCreateBtn_Click(object sender, EventArgs e)
        {
            this.DisposeTestPanel();

            this.SplitContainer.Panel2.SuspendLayout();

            int option = (this.OpgWin.Checked ? 1 : this.OpgDaj.Checked ? 2 : 0);
            switch (option)
            {
                case 1:
                    WinPanel winPanel = new WinPanel();
                    winPanel.CreateInnerControls(this.NumCtrlTrack.Value);
                    this.SplitContainer.Panel2.Controls.Add(winPanel);
                    winPanel.Dock = DockStyle.Fill;
                    this.TestPanel = winPanel;
                    break;
                case 2:
                    Panel host = new Panel();
                    host.AutoScroll = true;
                    AplPanel aplPanel = new AplPanel();
                    host.Controls.Add(aplPanel);
                    aplPanel.Location = new Point(0, 0);
                    aplPanel.CreateInnerControls(this.NumCtrlTrack.Value);
                    this.SplitContainer.Panel2.Controls.Add(host);
                    host.Dock = DockStyle.Fill;
                    this.TestPanel = host;
                    break;
            }

            this.SplitContainer.Panel2.ResumeLayout(true);
        }
        protected void DisposeTestPanel()
        {
            if (this.TestPanel != null)
            {
                this.TestPanel.Dispose();
                this.TestPanel = null;
            }
        }
        private IDisposable TestPanel;
    }

    #region WinControls
    public class WinPanel : Panel, ITestPanel
    {
        public WinPanel()
        {
            this.WinControls = new List<WinControl>();
            this.AutoScroll = true;
            this.VScroll = true;
            this.HScroll = true;
        }
        public void CreateInnerControls(int count)
        {
            this.DisposeControls();

            count = count / WinControl.ControlsCount;

            int firstX = 8;
            int firstY = 8;
            int currX = firstX;
            int currY = firstY;
            int rowNumb = 1;
            int colNumb = 1;
            int itmNumb = 1;
            int colCount = 7;
            for (int n = 0; n < count; n++)
            {
                WinControl winControl = new WinControl() { Location = new Point(currX, currY) };
                winControl.WinLabel = "R" + rowNumb + "; C" + colNumb;
                winControl.WinLabel = "I." + itmNumb;
                this.Controls.Add(winControl);

                if (colNumb < colCount)
                {
                    colNumb++;
                    currX = winControl.Bounds.Right + 3;
                }
                else
                {
                    rowNumb++;
                    colNumb = 1;
                    currX = firstX;
                    currY = winControl.Bounds.Bottom + 3;
                }
                itmNumb++;

                this.WinControls.Add(winControl);
            }
        }
        protected override void Dispose(bool disposing)
        {
            this.DisposeControls();
            base.Dispose(disposing);
        }
        protected void DisposeControls()
        {
            foreach (WinControl control in WinControls)
                control.Dispose();
            this.Controls.Clear();
        }
        protected List<WinControl> WinControls;
    }
    public class WinControl : Panel
    {
        public WinControl()
        {
            this.Label = new Label()
            {
                Text = "Label",
                Bounds = new Rectangle(4, 4, 60, 20),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(this.Label);

            this.TextBox = new TextBox()
            {
                Bounds = new Rectangle(66, 4, 120, 20)
            };
            this.Controls.Add(this.TextBox);

            this.Button = new Button()
            {
                Text = "...",
                Bounds = new Rectangle(188, 4, 25, 20),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Button.Click += Button_Click;
            this.Controls.Add(this.Button);

            this.Size = new Size(218, 28);
            this.MinimumSize = this.Size;
            this.MaximumSize = this.Size;
            this.BackColor = Color.FromArgb(190, 198, 198);
        }
        private void Button_Click(object sender, EventArgs e)
        {
            if (this.WinClick != null)
                this.WinClick(this, e);
        }
        public string WinLabel { get { return this.Label.Text; } set { this.Label.Text = value; } }
        public string WinText { get { return this.TextBox.Text; } set { this.TextBox.Text = value; } }
        public string WinButton { get { return this.Button.Text; } set { this.Button.Text = value; } }
        public event EventHandler WinClick;
        protected override void Dispose(bool disposing)
        {
            this.DisposeControls();
            base.Dispose(disposing);
        }
        protected void DisposeControls()
        {
            if (this.Label != null)
            {
                this.Label.Dispose();
                this.Label = null;
            }
            if (this.TextBox != null)
            {
                this.TextBox.Dispose();
                this.TextBox = null;
            }
            if (this.Button != null)
            {
                this.Button.Dispose();
                this.Button = null;
            }
        }
        protected Label Label;
        protected TextBox TextBox;
        protected Button Button;
        /// <summary>
        /// Počet controlů v tomto controlu, včetně this controlu
        /// </summary>
        internal static int ControlsCount { get { return 4; } }
    }
    #endregion
    #region AplControls
    public class AplPanel : Panel, ITestPanel
    {
        public AplPanel()
        {
            this.AplControls = new List<AplControl>();
            this.AutoScroll = true;
            this.VScroll = true;
            this.HScroll = true;
            this.Brush = new SolidBrush(Color.Wheat);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            foreach (var aplControl in this.AplControls)
                aplControl.Paint(e);
        }
        public void CreateInnerControls(int count)
        {
            this.DisposeControls();

            count = count / WinControl.ControlsCount;

            Color[] colors = new Color[] 
            {
                Color.FromArgb(192, 216, 192),
                Color.FromArgb(216, 192, 192),
                Color.FromArgb(192, 192, 216),
                Color.FromArgb(216, 216, 192),
                Color.FromArgb(192, 216, 216),
                Color.FromArgb(216, 192, 216)
            };
            int firstX = 8;
            int firstY = 8;
            int currX = firstX;
            int currY = firstY;
            int maxX = 0;
            int maxY = 0;
            int rowNumb = 1;
            int colNumb = 1;
            int itmNumb = 1;
            int colCount = 7;
            for (int n = 0; n < count; n++)
            {
                AplControl aplControl = new AplControl() { Location = new Point(currX, currY) };
                aplControl.Parent = this;
                aplControl.BackColor = colors[n % colors.Length];
                if (aplControl.Bounds.Right > maxX) maxX = aplControl.Bounds.Right;
                if (aplControl.Bounds.Bottom > maxY) maxY = aplControl.Bounds.Bottom;

                aplControl.AplLabel = "I." + itmNumb;

                this.AplControls.Add(aplControl);

                if (colNumb < colCount)
                {
                    colNumb++;
                    currX = aplControl.Bounds.Right + 3;
                }
                else
                {
                    rowNumb++;
                    colNumb = 1;
                    currX = firstX;
                    currY = aplControl.Bounds.Bottom + 3;
                }
                itmNumb++;

                this.AplControls.Add(aplControl);
            }

            this.Size = new Size(maxX + 6, maxY + 6);
            this.MinimumSize = this.Size;
        }
        public SolidBrush Brush { get; set; }
        protected override void Dispose(bool disposing)
        {
            this.DisposeControls();
            this.Brush.Dispose();
            this.Brush = null;
            base.Dispose(disposing);
        }
        protected void DisposeControls()
        {
            foreach (AplControl control in AplControls)
                ((IDisposable)control).Dispose();
            this.Controls.Clear();
        }
        protected List<AplControl> AplControls;
    }
    public class AplControl : IDisposable
    {
        public AplControl()
        {
            this.Bounds = new Rectangle(0, 0, 218, 28);
        }
        public AplPanel Parent { get; set; }
        public Point Location { get { return this.Bounds.Location; } set { this.Bounds = new Rectangle(value, this.Bounds.Size); } }
        public string AplLabel { get; set; }
        public string AplText { get; set; }
        public string AplButton { get; set; }
        public Rectangle Bounds { get; set; }
        public Color BackColor { get; set; }
        public void Paint(PaintEventArgs e)
        {
            SolidBrush brush = this.Parent.Brush;
            brush.Color = this.BackColor;
            e.Graphics.FillRectangle(brush, this.Bounds);
        }
        void IDisposable.Dispose()
        { }
    }
    #endregion
    #region Interfaces
    public interface ITestPanel : IDisposable
    {
        void CreateInnerControls(int count);
    }
    #endregion
}
