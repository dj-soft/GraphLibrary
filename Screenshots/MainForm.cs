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
        #region Konstruktor a inicializace
        public TestForm()
        {
            InitializeComponent();
            this.InitForm();
        }
        private void InitForm()
        {
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.AllowTransparency = true;
            this.TransparencyKey = Color.Magenta;

            this.BackColor = Color.FromArgb(0, 0, 64);
            this._ScreenPanel.BackColor = Color.Magenta;
            this._HelpBox.BackColor = this.BackColor;

            this._Fms = new FormMoveSupport(this, this._ImagePanel, this._SnapOneBtn, this._SnapRecBtn, this._SnapStopBtn, this._FrequencyLabel);

            this.SetTopLevel(true);
            this.TopMost = true;

            this._LoadConfig();
            this._ShowConfig();

            this._ThreadInit();
        }
        FormMoveSupport _Fms;
        #endregion
        #region Overrides formu

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
        /// <summary>
        /// Řízení viditelnosti ovládacích jednotlivých prvků GUI
        /// </summary>
        /// <param name="snap"></param>
        /// <param name="run"></param>
        /// <param name="stop"></param>
        /// <param name="help"></param>
        private void _SetButtons(bool snap, bool run, bool stop, bool help)
        {
            this._SnapOneBtn.Visible = snap;
            this._HelpShowBtn.Visible = snap;
            this._SnapRecBtn.Visible = run;
            this._SnapStopBtn.Visible = stop;
            this._HelpBox.Visible = help;
        }
        #endregion
        #region TickFrequency
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
            if (this._ThreadRunning && this._Semaphore != null)
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
        #endregion
        #region Práce s konfigurací - načtení, zobrazení, uložení
        private void _LoadConfig()
        {
            this._Config = Config.LoadConfig();
        }
        private void _ShowConfig()
        {
            bool visibleHelp = !this._Config.HideHelp;
            this._SetButtons(true, true, false, visibleHelp);
            this._HelpHideChk.Checked = !visibleHelp;

            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = this._Config.FormBounds;

            int min = 0;
            int max = 18;
            int val = this._Config.FrequencyIndex;
            val = (val < min ? min : (val > max ? max : val));
            this._FrequencyTrack.Minimum = min;
            this._FrequencyTrack.Maximum = max;
            this._FrequencyTrack.Value = val;
            this._ShowFrequencyTick();

            this._ShowStatusText();

            this._SubPath = null;
        }
        /// <summary>
        /// Cílový adresář
        /// </summary>
        private string _TargetPath
        {
            get { return this._Config.TargetPath; }
            set { this._Config.TargetPath = value; this._SaveConfig(); }
        }
        private void _SaveConfig()
        {
            this._Config.FormBounds = this.Bounds;
            this._Config.FrequencyIndex = this._FrequencyTrack.Value;
            this._Config.HideHelp = this._HelpHideChk.Checked;
            this._Config.Save();
        }
        private Config _Config;
        #endregion
        #region Sejmutí obrázku - akce a obsluha buttonu
        /// <summary>
        /// Kompletní získání screenshotu, metoda volaná z threadu na pozadí (převolává GUI thread)
        /// </summary>
        private void _CreateSnapShot()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(_CreateSnapShotGui));
            else
                this._CreateSnapShotGui();
        }
        /// <summary>
        /// Řízení celé aktivity sejmutí jednoho obrázku v GUI threadu: získání bitmapy <see cref="_SnapBitmap"/>, 
        /// uložení do souboru a prosvícení v <see cref="_ImagePanel"/>.
        /// </summary>
        private void _CreateSnapShotGui()
        {
            Bitmap bitmap = _SnapBitmap();
            SaveBitmap(bitmap);
            this._ImagePanel.Image = bitmap;

            if (_LastBitMap != null)
                this._LastBitMap.Dispose();
            this._LastBitMap = bitmap;
        }
        /// <summary>
        /// Fyzické získání screenshotu z obrazovky
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Určení souřadnic Screenshotu
        /// </summary>
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
        /// <summary>
        /// Uloží screenshot do souboru
        /// </summary>
        /// <param name="bitmap"></param>
        private void SaveBitmap(Bitmap bitmap)
        {
            if (bitmap == null) return;
            DateTime now = DateTime.UtcNow;
            string path = this._TargetPath;
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

        /// <summary>
        /// Eventhandler pro tlačítko Sejmi jeden obrázek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SnapOneBtn_Click(object sender, EventArgs e)
        {
            this._SetButtons(false, false, false, false);
            this._CreateSnapShotGui();
            this._SetButtons(true, true, false, false);
        }
        /// <summary>
        /// Podadresář pro ukládání aktuální serie screenshotů
        /// </summary>
        private string _SubPath;
        /// <summary>
        /// Souřadnice Screenshotu
        /// </summary>
        private Rectangle _SnapBounds;
        /// <summary>
        /// true pokud Souřadnice Screenshotu jsou platné
        /// </summary>
        private bool _SnapBoundsValid;
        /// <summary>
        /// Posledně získaná bitmapa, svítí v okně vpravo nahoře v <see cref="_ImagePanel"/>
        /// </summary>
        private Bitmap _LastBitMap;
        #endregion
        #region Back thread
        /// <summary>
        /// Inicializace threadu na pozadí
        /// </summary>
        private void _ThreadInit()
        {
            this._Semaphore = new System.Threading.AutoResetEvent(false);
            this._Thread = new System.Threading.Thread(_ThreadRun);
            this._Thread.Name = "BackThread";
            this._Thread.IsBackground = true;
            this._Thread.Start();
        }
        /// <summary>
        /// Main smyčka threadu na pozadí
        /// </summary>
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
        /// <summary>
        /// Kontinuální snímání: sejmi jeden screenshot
        /// </summary>
        private void _ThreadSnapOne()
        {
            try
            {
                this._CreateSnapShot();
            }
            catch { }
        }
        /// <summary>
        /// Definitivně zastaví běh threadu na pozadí, při zavírání okna
        /// </summary>
        private void _ThreadStop()
        {
            this._ThreadRunning = false;
            this._Semaphore.Set();
        }
        /// <summary>
        /// Thread na pozadí
        /// </summary>
        private System.Threading.Thread _Thread;
        /// <summary>
        /// Budíček pro thread na pozadí, když je potřeba přerušit jeho čekání
        /// </summary>
        private System.Threading.AutoResetEvent _Semaphore;
        /// <summary>
        /// Interval mezi snímky na pozadí
        /// </summary>
        private TimeSpan _ThreadTime;
        /// <summary>
        /// true když se provádí kontinuální snímání screenshotů v threadu na pozadí (Run), false když ne (Stop)
        /// </summary>
        private bool _SnapRunning;
        /// <summary>
        /// true po dobu života vlákna na pozadí, false pro jeho skončení
        /// </summary>
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
        #region Help okno
        private void _HelpOkBtn_Click(object sender, EventArgs e)
        {
            this._SetButtons(true, true, false, false);
        }

        private void _HelpShowBtn_Click(object sender, EventArgs e)
        {
            this._SetButtons(true, true, false, true);
        }

        #endregion
        #region Status bar
        private void _ShowStatusText()
        {
            this._StatusText.Text = this._TargetPath;
        }
        /// <summary>
        /// Eventhandler tlačítka Vyber výstupní složku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _StatusSettingBtn_Click(object sender, EventArgs e)
        {
            string path = this._TargetPath;
            using (System.Windows.Forms.FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Složka pro ukládání screenshotů";
                // fbd.RootFolder = Environment.SpecialFolder.MyComputer;
                fbd.SelectedPath = path;
                fbd.ShowNewFolderButton = true;
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    this._TargetPath = fbd.SelectedPath;
                    this._ShowConfig();
                }
            }
        }
        /// <summary>
        /// Eventhandler tlačítka Otevři výstupní složku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _StatusOpenBtn_Click(object sender, EventArgs e)
        {
            string path = this._TargetPath;
            System.Diagnostics.Process.Start(path);
        }
        #endregion
    }
}
