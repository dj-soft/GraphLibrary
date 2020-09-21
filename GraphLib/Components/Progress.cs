using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Třída pro zobrazení okna postupu operace
    /// </summary>
    public class ProgressItem : InteractiveContainer
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="host"></param>
        public ProgressItem(GInteractiveControl host)
        {
            this._ProgressData = new ProgressData();
            this.Parent = host;
            this.Is.Visible = false;
        }
        private ProgressData _ProgressData;

        #region Data for progress
        /// <summary>
        /// Data for progress
        /// </summary>
        public ProgressData ProgressData
        {
            get { return this._ProgressData; }
            set { this._ProgressData = value; }
        }
        /// <summary>
        /// true = máme data
        /// </summary>
        protected bool HasData { get { return (this._ProgressData != null); } }
        /// <summary>
        /// Stav postupu
        /// </summary>
        protected float DataRatio { get { return (this.HasData ? this._ProgressData.Ratio : 0f); } }
        /// <summary>
        /// Lze dát Storno?
        /// </summary>
        protected bool DataCanCancel { get { return (this.HasData ? this._ProgressData.CanCancel : false); } }
        /// <summary>
        /// Bylo dáno Storno?
        /// </summary>
        protected bool DataIsCancelled { get { return (this.HasData ? this._ProgressData.IsCancelled : false); } }
        /// <summary>
        /// Průhlednost
        /// </summary>
        protected Int32? DataOpacity { get { return (this.HasData ? this._ProgressData.Opacity : null); } }
        /// <summary>
        /// Barva pozadí
        /// </summary>
        protected Color DataBackColor { get { return (this.HasData && this._ProgressData.BackColor.HasValue ? this._ProgressData.BackColor.Value : this.BackColorDefault).SetOpacity(this.DataOpacity); } }
        /// <summary>
        /// Barva popředí
        /// </summary>
        protected Color DataForeColor { get { return (this.HasData && this._ProgressData.ForeColor.HasValue ? this._ProgressData.ForeColor.Value : this.DefaultForeColor); } }
        /// <summary>
        /// Barva pozadí progresu
        /// </summary>
        protected Color DataProgressBackColor { get { return (this.HasData && this._ProgressData.ProgressBackColor.HasValue ? this._ProgressData.ProgressBackColor.Value : this.DefaultProgressBackColor).SetOpacity(this.DataOpacity); } }
        /// <summary>
        /// Barva popředí progresu
        /// </summary>
        protected Color DataProgressForeColor { get { return (this.HasData && this._ProgressData.ProgressForeColor.HasValue ? this._ProgressData.ProgressForeColor.Value : this.DefaultProgressForeColor); } }
        /// <summary>
        /// Text v progresu
        /// </summary>
        protected string DataInfoCurrent { get { return (this.HasData ? this._ProgressData.InfoCurrent : null); } }
        /// <summary>
        /// Řada předešlých textů
        /// </summary>
        protected List<string> DataInfoPrevious { get { return (this.HasData ? this._ProgressData.InfoPrevious : null); } }
        /// <summary>
        /// Výchozí barva pozadí
        /// </summary>
        protected override Color BackColorDefault { get { return Color.DarkOrchid; } }
        /// <summary>
        /// Výchozí barva popředí
        /// </summary>
        protected Color DefaultForeColor { get { return this.DataBackColor.Contrast(); } }
        /// <summary>
        /// Výchozí barva pozadí progresu
        /// </summary>
        protected Color DefaultProgressBackColor { get { return Color.LightGray; } }
        /// <summary>
        /// Výchozí barva popředí progresu
        /// </summary>
        protected Color DefaultProgressForeColor { get { return Color.MediumBlue; } }
        #endregion
        /// <summary>
        /// Set position (Bounds) for this item, by ClientBounds of host object.
        /// </summary>
        internal void SetPosition()
        {
            GInteractiveControl host = this.Host;
            if (host == null) return;

            Rectangle hostBounds = host.ClientItemsRectangle;

            Size maxSize = new Size(hostBounds.Width - 12, hostBounds.Height - 12);
            Size size = new Size(420, 200);
            if (size.Width > maxSize.Width) size.Width = maxSize.Width;
            if (size.Height > maxSize.Height) size.Height = maxSize.Height;

            Rectangle bounds = hostBounds.Center().CreateRectangleFromCenter(size);
            this.Bounds = bounds;
        }
        /// <summary>
        /// Po změně stavu
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChanged(e);
        }
        /// <summary>
        /// true when progress window has been drawed
        /// </summary>
        public bool NeedDraw { get { return this.Is.Visible; } }
        internal void Draw(System.Drawing.Graphics graphics)
        {
            GInteractiveControl host = this.Host;
            if (host == null) return;

            Rectangle bounds = this.BoundsAbsolute;
            int x = bounds.X;
            int y = bounds.Y;
            int w = bounds.Width;
            int h = bounds.Height;
            int b = bounds.Bottom - 1;

            GPainter.DrawWindow(graphics, bounds, this.DataBackColor, System.Windows.Forms.Orientation.Vertical, this.DataOpacity, 2, 4);

            int progressHeight = Application.App.Zoom.ZoomDistance(21);
            Rectangle progBounds = new Rectangle(x + 15, b - progressHeight - 10, w - 30, progressHeight);
            GPainter.DrawButtonBase(graphics, progBounds, new DrawButtonArgs() { BackColor = this.DataProgressBackColor });

            int progressWidth = this.GetDataProgressWidth(progBounds.Width - 4);
            Rectangle dataBounds = new Rectangle(progBounds.X + 2, progBounds.Y + 2, progressWidth, progBounds.Height - 4);
            GPainter.DrawRectangle(graphics, dataBounds, this.DataProgressForeColor);

            string progressText = this.GetDataProgressText();
            GPainter.DrawString(graphics, progressText, FontInfo.Caption, progBounds, ContentAlignment.MiddleCenter, color: Color.Black);

            string infoCurrent = this.DataInfoCurrent;
            if (!String.IsNullOrEmpty(infoCurrent))
            {
                Rectangle infoBounds = new Rectangle(x + 15, y + 12, w - 32, h - 59);
                FontInfo fontInfo = FontInfo.Caption;
                fontInfo.Bold = true;
                fontInfo.RelativeSize = 120;
                Rectangle realBounds;
                realBounds = GPainter.DrawString(graphics, infoCurrent, fontInfo, infoBounds, ContentAlignment.TopLeft, color: this.DataForeColor);
            }
        }
        /// <summary>
        /// Returns width of Data on Progress Bar (in pixel) for current DataRatio and specified Width (for 100%).
        /// </summary>
        /// <param name="width"></param>
        /// <returns></returns>
        private int GetDataProgressWidth(int width)
        {
            float ratio = this.DataRatio;
            int w = (int)(Math.Round(ratio * (float)(width), 0));
            return (w < 0 ? 0 : (w > width ? width : w));
        }
        /// <summary>
        /// Returns current percent text for progress, in form "4.2 %"
        /// </summary>
        /// <returns></returns>
        private string GetDataProgressText()
        {
            double percent = Math.Round(100d * (double)this.DataRatio, 1);
            return percent.ToString("##0.0").Trim() + " %";
        }
    }
    /// <summary>
    /// Data progresu = nevizuální čistý datový objekt
    /// </summary>
    public class ProgressData
    {
        #region Private variables
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ProgressData()
        {
            this.Reset();
        }
        private bool _IsVisible;
        private bool _CanCancel;
        private bool _IsCancelled;
        private float _Ratio;
        private Int32? _Opacity;
        private Color? _BackColor;
        private Color? _ForeColor;
        private Color? _ProgressBackColor;
        private Color? _ProgressForeColor;
        private string _InfoCurrent;
        private List<string> _InfoPrevious;
        #endregion
        /// <summary>
        /// Reset all variables to their initial values
        /// </summary>
        internal void Reset()
        {
            this._CanCancel = false;
            this._IsCancelled = false;
            this._Ratio = 0f;
            this._Opacity = null;
            this._BackColor = null;
            this._ForeColor = null;
            this._ProgressBackColor = null;
            this._ProgressForeColor = null;
            this._InfoCurrent = "";
            this._InfoPrevious = new List<string>();
        }
        /// <summary>
        /// Have be this Progress visible?
        /// </summary>
        public bool IsVisible
        {
            get { return this._IsVisible; }
            set { this._IsVisible = value; }
        }
        /// <summary>
        /// Current value for progress ratio, in range from 0 to 1 (include booth values).
        /// </summary>
        public float Ratio
        {
            get { return this._Ratio; }
            set { this._Ratio = (value < 0f ? 0f : (value > 1f ? 1f : value)); }
        }
        /// <summary>
        /// Can user cancel this progress?
        /// </summary>
        public bool CanCancel
        {
            get { return this._CanCancel; }
            set { if (!this._IsCancelled) this._CanCancel = value; }
        }
        /// <summary>
        /// Was user cancelled this progress?
        /// </summary>
        public bool IsCancelled
        {
            get { return this._IsCancelled; }
        }
        /// <summary>
        /// Opacity for progress window
        /// </summary>
        public Int32? Opacity
        {
            get { return this._Opacity; }
            set { this._Opacity = value; }
        }
        /// <summary>
        /// Back color for progress window
        /// </summary>
        public Color? BackColor
        {
            get { return this._BackColor; }
            set { this._BackColor = value; }
        }
        /// <summary>
        /// Foreground (=text) color for progress window
        /// </summary>
        public Color? ForeColor
        {
            get { return this._ForeColor; }
            set { this._ForeColor = value; }
        }
        /// <summary>
        /// Back color for progress area
        /// </summary>
        public Color? ProgressBackColor
        {
            get { return this._ProgressBackColor; }
            set { this._ProgressBackColor = value; }
        }
        /// <summary>
        /// Foreground (=data) color for progress area
        /// </summary>
        public Color? ProgressForeColor
        {
            get { return this._ProgressForeColor; }
            set { this._ProgressForeColor = value; }
        }
        /// <summary>
        /// Current informations row = main text
        /// </summary>
        public string InfoCurrent
        {
            get { return this._InfoCurrent; }
            set { this._InfoCurrent = value; }
        }
        /// <summary>
        /// Current informations row = main text
        /// </summary>
        public List<string> InfoPrevious
        {
            get { return this._InfoPrevious; }
            set { this._InfoPrevious = value; }
        }
    }
}
