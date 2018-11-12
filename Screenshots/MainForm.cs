using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Djs.Tools.Screenshots.Components;

namespace Djs.Tools.Screenshots
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
            this.BackColor = Color.FromArgb(0, 0, 64);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            this.AllowTransparency = true;
            this.TransparencyKey = Color.Magenta;
            this._ScreenPanel.BackColor = Color.Magenta;
            this._HelpBox.BackColor = this.BackColor;

            this.StartPosition = FormStartPosition.Manual;

            this._Fms = new FormMoveSupport(this, this._ImagePanel, this._SnapOneBtn, this._SnapRecBtn, this._SnapStopBtn, this._FrequencyLabel);
            this._Config = Config.LoadConfig();

            this.SetTopLevel(true);
            this.TopMost = true;

            bool visibleHelp = !this._Config.HideHelp;
            this._SetButtons(true, true, false, visibleHelp);

            this._ThreadInit();
            this._LoadConfig();
        }
        private Config _Config;
        private string _SubPath;
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this._ReadSnapBounds();
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this._ReadSnapBounds();
        }
        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            this._ReadSnapBounds();
        }
        protected override void OnClosed(EventArgs e)
        {
            this._ThreadStop();
            this._SaveConfig();
            base.OnClosed(e);
        }
        private void _LoadConfig()
        {
            this.Bounds = this._Config.FormBounds;
            this._ReadSnapBounds();

            int min = 0;
            int max = 18;
            int val = this._Config.FrequencyIndex;
            val = (val < min ? min : (val > max ? max : val));
            this._FrequencyTrack.Minimum = min;
            this._FrequencyTrack.Maximum = max;
            this._FrequencyTrack.Value = val;
            this._ShowFrequencyTick();

            this._HelpHideChk.Checked = this._Config.HideHelp;

            this._StatusText.Text = this._Config.TargetPath;

            this._SubPath = null;
        }
        private void _SaveConfig()
        {
            this._Config.FormBounds = this.Bounds;
            this._Config.FrequencyIndex = this._FrequencyTrack.Value;
            this._Config.HideHelp = this._HelpHideChk.Checked;
            this._Config.Save();
        }
        FormMoveSupport _Fms;
        private void _FrequencyTrack_Scroll(object sender, EventArgs e)
        {
            this._ShowFrequencyTick();
        }
        private void _ShowFrequencyTick()
        {
            int frequencyIndex = ((this._FrequencyTrack.Maximum - this._FrequencyTrack.Minimum) / 2) - this._FrequencyTrack.Value;
            TimeSpan time;
            string text;
            _FrequencyToValues(frequencyIndex, out time, out text);
            this._FrequencyLabel.Text = text;
            this._ThreadTime = time;
            this._Semaphore.Set();
        }
        private static void _FrequencyToValues(int frequencyIndex, out TimeSpan time, out string text)
        {
            int fValue = frequencyIndex;
            if (fValue < 0)
            {   // -1 => 2 snímky / sec:
                int n = -fValue + 1;
                time = TimeSpan.FromSeconds(1d / (double)n);
                text = n.ToString() + " s / sec";
            }
            else if (fValue == 0)
            {   // 0 => 1 / sec
                time = TimeSpan.FromSeconds(1d);
                text = "1 s / sec";
            }
            else
            {   // 1 => 2 / sec
                int n = fValue + 1;
                time = TimeSpan.FromSeconds((double)n);
                text = "1 s / " + n.ToString() + " sec";
            }
        }
        private Bitmap _SnapBitmap()
        {
            Bitmap bitmap = null;
            if (this._SnapBoundsValid)
            {
                Rectangle bounds = this._SnapBounds;
                using (Bitmap bmpScreenCapture = new Bitmap(bounds.Width, bounds.Height))
                using (Graphics g = Graphics.FromImage(bmpScreenCapture))
                {
                    g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                    bitmap = bmpScreenCapture.Clone() as Bitmap;
                }
            }
            return bitmap;
        }
        private void _ReadSnapBounds()
        {
            if (this.WindowState == FormWindowState.Normal && !this._SnapRunning)
            {
                Size size = this._ScreenPanel.Size;
                Point point = this._ScreenPanel.PointToScreen(new Point(0, 0));
                this._SnapBounds = new Rectangle(point, size);
                this._SnapBoundsValid = true;
            }
        }
        private Rectangle _SnapBounds;
        private bool _SnapBoundsValid;
        private void SaveBitmap(Bitmap bitmap)
        {
            if (bitmap == null) return;
            DateTime now = DateTime.UtcNow;
            string path = this._Config.TargetPath;
            string targetPath;

            string subPath = this._SubPath;
            if (subPath == null)
            {
                subPath = now.ToString("yyyyMMdd_HHmmss");
                this._SubPath = subPath;
            }
            targetPath = Path.Combine(path, subPath);
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            string targetFile = Path.Combine(targetPath, "Sc_" + now.ToString("yyyyMMdd_HHmmssfff") + ".jpg");
            bitmap.Save(targetFile, System.Drawing.Imaging.ImageFormat.Jpeg);
        }
        private void _SetButtons(bool snap, bool run, bool stop, bool help)
        {
            this._SnapOneBtn.Visible = snap;
            this._HelpShowBtn.Visible = snap;
            this._SnapRecBtn.Visible = run;
            this._SnapStopBtn.Visible = stop;
            this._HelpBox.Visible = help;
        }
        private void _SnapOneBtn_Click(object sender, EventArgs e)
        {
            this._SetButtons(false, false, false, false);
            this._CreateSnapShotGui();
            this._SetButtons(true, true, false, false);
        }
        private void _CreateSnapShot()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(_CreateSnapShotGui));
            else
                this._CreateSnapShotGui();
        }
        private void _CreateSnapShotGui()
        {
            Bitmap bitmap = _SnapBitmap();
            SaveBitmap(bitmap);
            this._ImagePanel.Image = bitmap;

            if (_LastBitMap != null)
                this._LastBitMap.Dispose();
            this._LastBitMap = bitmap;
        }
        private Bitmap _LastBitMap;

        #region Back thread
        private void _ThreadInit()
        {
            this._Semaphore = new System.Threading.AutoResetEvent(false);
            this._Thread = new System.Threading.Thread(_ThreadRun);
            this._Thread.Name = "BackThread";
            this._Thread.IsBackground = true;
            this._Thread.Start();
        }
        private void _ThreadRun()
        {
            this._ThreadRunning = true;
            while (this._ThreadRunning)
            {
                if (this._ThreadRunning && this._SnapRunning)
                    this._ThreadSnapOne();
                if (!this._ThreadRunning)
                    break;
                this._Semaphore.WaitOne(this._ThreadTime);
            }
        }
        private void _ThreadSnapOne()
        {
            try
            {
                this._CreateSnapShot();
            }
            catch { }
        }
        
        private void _ThreadStop()
        {
            this._ThreadRunning = false;
            this._Semaphore.Set();
        }
        private System.Threading.Thread _Thread;
        private System.Threading.AutoResetEvent _Semaphore;
        private TimeSpan _ThreadTime;
        private bool _SnapRunning;
        private bool _ThreadRunning;

        private void _SnapRecBtn_Click(object sender, EventArgs e)
        {
            this._SetButtons(false, false, true, false);
            this._SubPath = null;                 // Nové video si otevře nový adresář
            this._SnapRunning = true;
            this._Semaphore.Set();
        }

        private void _SnapStopBtn_Click(object sender, EventArgs e)
        {
            this._SnapRunning = false;
            this._SetButtons(true, true, false, false);
        }
        #endregion

        private void _HelpOkBtn_Click(object sender, EventArgs e)
        {
            this._SetButtons(true, true, false, false);
        }

        private void _HelpShowBtn_Click(object sender, EventArgs e)
        {
            this._SetButtons(true, true, false, true);
        }
    }
}
