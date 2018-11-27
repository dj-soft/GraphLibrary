using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;

using Asol.Tools.WorkScheduler.Components;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    #region ConfigSnapPanel : Panel pro zobrazení přichytávání
    /// <summary>
    /// ConfigSnapPanel : Panel pro zobrazení přichytávání
    /// </summary>
    public class ConfigSnapPanel : WinHorizontalLine
    {
        #region Vnitřní život: inicializace
        /// <summary>
        /// Inicializace.
        /// Pozor, jde o virtuální metodu volanou z konstruktoru.
        /// Tyto controly obecně nemají používat konstruktor, ale tuto inicializaci (kde lze řídit místo výkonu base metody)
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            this.SnapCheck = new CheckBox() { Bounds = new Rectangle(14, 35, 174, 24), Text = "Přichytávat, na vzdálenost:", TabIndex = 0 };
            this.DistTrack = new TrackBar() { Bounds = new Rectangle(50, 61, 188, 45), Minimum = 0, Maximum = 32, TickFrequency = 2, TickStyle = TickStyle.TopLeft, TabIndex = 1 };
            this.PixelLabel = new Label() { Bounds = new Rectangle(194, 35, 59, 23), AutoSize = false, Text = "", TextAlign = ContentAlignment.MiddleLeft, TabIndex = 2 };
            this.SamplePanel = new ConfigSnapSamplePanel() { Bounds = new Rectangle(240, 29, 150, 90) };

            this.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DistTrack)).BeginInit();

            this.SnapCheck.CheckedChanged += SnapCheck_CheckedChanged;
            this.DistTrack.ValueChanged += DistTrack_ValueChanged;

            this.Controls.Add(this.SamplePanel);
            this.Controls.Add(this.DistTrack);
            this.Controls.Add(this.SnapCheck);
            this.Controls.Add(this.PixelLabel);

            this.OnlyOneLine = false;
            this.Size = new Size(400, 128);
            this.MinimumSize = this.Size;
            this.MaximumSize = this.Size;

            this.ResumeLayout(false);
            this.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DistTrack)).EndInit();

            this.SnapCheck.Checked = true;
            this.DistTrack.Value = 16;
        }
        /// <summary>
        /// CheckBox 
        /// </summary>
        protected CheckBox SnapCheck;
        /// <summary>
        /// TrackBar 
        /// </summary>
        protected TrackBar DistTrack;
        /// <summary>
        /// Label 
        /// </summary>
        protected Label PixelLabel;
        /// <summary>
        /// ConfigSnapSamplePanel 
        /// </summary>
        protected ConfigSnapSamplePanel SamplePanel;
        #endregion
        #region privátní eventhandlery
        /// <summary>
        /// Změna hodnoty SnapActive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SnapCheck_CheckedChanged(object sender, EventArgs e)
        {
            this.CallSnapActiveChanged();
        }
        /// <summary>
        /// Akce po změně hodnoty <see cref="SnapActive"/>
        /// </summary>
        private void CallSnapActiveChanged()
        {
            bool value = this.SnapCheck.Checked;
            this.SamplePanel.SnapActive = value;
            this.DistTrack.Enabled = value;
            this.OnSnapActiveChanged();
            if (this.SnapActiveChanged != null)
                this.SnapActiveChanged(this, new EventArgs());
        }
        /// <summary>
        /// Háček po změně hodnoty <see cref="SnapActive"/>
        /// </summary>
        protected virtual void OnSnapActiveChanged() { }
        /// <summary>
        /// Event po změně hodnoty <see cref="SnapActive"/>
        /// </summary>
        public event EventHandler SnapActiveChanged;
        /// <summary>
        /// Změna hodnoty SnapDistance
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DistTrack_ValueChanged(object sender, EventArgs e)
        {
            this.CallSnapDistanceChanged();
        }
        /// <summary>
        /// Akce po změně hodnoty <see cref="SnapDistance"/>
        /// </summary>
        private void CallSnapDistanceChanged()
        {
            int value = this.DistTrack.Value;
            this.SamplePanel.SnapDistance = value;
            this.PixelLabel.Text = value.ToString() + " pixel";
            this.OnSnapDistanceChanged();
            if (this.SnapDistanceChanged != null)
                this.SnapDistanceChanged(this, new EventArgs());
        }
        /// <summary>
        /// Háček po změně hodnoty <see cref="SnapDistance"/>
        /// </summary>
        protected virtual void OnSnapDistanceChanged() { }
        /// <summary>
        /// Event po změně hodnoty <see cref="SnapDistance"/>
        /// </summary>
        public event EventHandler SnapDistanceChanged;
        #endregion
        #region Public properties
        /// <summary>
        /// Aktivita tohoto konkrétního přichycení
        /// </summary>
        [Description("Aktivita tohoto konkrétního přichycení")]
        [Category(WinConstants.DesignCategory)]
        public bool SnapActive
        {
            get { return this.SnapCheck.Checked; }
            set { this.SnapCheck.Checked = value; }
        }
        /// <summary>
        /// Vzdálenost přichycení v pixelech
        /// </summary>
        [Description("Vzdálenost přichycení v pixelech")]
        [Category(WinConstants.DesignCategory)]
        public int SnapDistance
        {
            get { return this.DistTrack.Value; }
            set { this.DistTrack.Value = value; this.SamplePanel.SnapDistance = this.DistTrack.Value; }
        }
        /// <summary>
        /// Druh ilustračního obrázku
        /// </summary>
        [Description("Druh ilustračního obrázku")]
        [Category(WinConstants.DesignCategory)]
        public ConfigSnapImageType ImageType
        {
            get { return this.SamplePanel.ImageType; }
            set { this.SamplePanel.ImageType = value; }
        }
        #endregion
    }
    #endregion
    #region ConfigSnapSamplePanel : Panel pro zobrazení přichytávání
    /// <summary>
    /// ConfigSnapSamplePanel : Panel pro zobrazení přichytávání
    /// </summary>
    public class ConfigSnapSamplePanel : WinPanel
    {
        #region Vnitřní život: inicializace
        /// <summary>
        /// Inicializace.
        /// Pozor, jde o virtuální metodu volanou z konstruktoru.
        /// Tyto controly obecně nemají používat konstruktor, ale tuto inicializaci (kde lze řídit místo výkonu base metody)
        /// </summary>
        protected override void Initialize()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            base.Initialize();

            this.Size = new Size(150, 90);
            this.MinimumSize = this.Size;
            this.MaximumSize = this.Size;

            this._SnapActive = true;
            this._ImageType = ConfigSnapImageType.Sequence;
            this._SnapDistance = 20;

            this._SampleBackColor = Color.LightGray;
            this._SampleFixColor = Color.DarkGray;
            this._SampleMovedColor = Color.DarkSeaGreen;
            this._SampleMagnetLinkColor = Color.DarkViolet;
        }
        /// <summary>
        /// Vykreslení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(this.SampleBackColor);
            switch (this.ImageType)
            {
                case ConfigSnapImageType.None:
                    this.PaintNone(e);
                    break;
                case ConfigSnapImageType.Sequence:
                    this.PaintSequence(e);
                    break;
                case ConfigSnapImageType.InnerItem:
                    this.PaintInnerItem(e);
                    break;
                case ConfigSnapImageType.OriginalTime:
                    this.PaintOriginalTime(e);
                    break;
                case ConfigSnapImageType.GridSnap:
                    this.PaintGridSnap(e);
                    break;
            }
            /*
            Rectangle bounds;
            int w = this.Width;
            int h = this.Height;

            bounds = new Rectangle(0, this.FontLineHeight - 2, w, 1);
            using (LinearGradientBrush lgb = new LinearGradientBrush(bounds, this._LineColorTop, Color.Transparent, 0f))
                e.Graphics.FillRectangle(lgb, bounds);

            bounds = new Rectangle(0, this.FontLineHeight - 1, w, 1);
            using (LinearGradientBrush lgb = new LinearGradientBrush(bounds, this._LineColorBottom, Color.Transparent, 0f))
                e.Graphics.FillRectangle(lgb, bounds);

            bounds = new Rectangle(2, 1, w - 4, this.FontLineHeight - 2);
            GPainter.DrawString(e.Graphics, bounds, this._Caption, Skin.Brush(this._TextColor), this._FontInfo, ContentAlignment.MiddleLeft);
            */
        }
        /// <summary>
        /// Vykreslí sample typu: None
        /// </summary>
        /// <param name="e"></param>
        protected void PaintNone(PaintEventArgs e)
        {
        }
        /// <summary>
        /// Vykreslí sample typu: Sequence
        /// </summary>
        /// <param name="e"></param>
        protected void PaintSequence(PaintEventArgs e)
        {
            Color color;

            int y = 30;
            Rectangle bounds1 = new Rectangle(10, y, 51, 25);
            Rectangle bounds2 = new Rectangle(60 + (this.SnapActive ? this.SnapDistance : 32), y, 51, 25);

            if (this.SnapActive)
                this.PaintMagnetLine(e, bounds2.GetPoint(ContentAlignment.MiddleLeft).Value, bounds1.GetPoint(ContentAlignment.MiddleRight).Value, true);

            color = this.SampleFixColor;
            GPainter.DrawButtonBase(e.Graphics, bounds1, color, GInteractiveState.MouseOver, Orientation.Horizontal, 0, null, null);

            color = this.SnapActive ? this.SampleMovedColor : this.SampleFixColor;
            GPainter.DrawButtonBase(e.Graphics, bounds2, color, GInteractiveState.MouseOver, Orientation.Horizontal, 0, null, null);
        }
        /// <summary>
        /// Vykreslí sample typu: InnerItem
        /// </summary>
        /// <param name="e"></param>
        protected void PaintInnerItem(PaintEventArgs e)
        {
            Color color;

            int y = 35;
            Rectangle bounds1 = new Rectangle(10, 5, 130, 78);
            Rectangle bounds2 = new Rectangle(10 + (this.SnapActive ? this.SnapDistance : 32), y, 51, 25);

            color = this.SampleFixColor;
            GPainter.DrawButtonBase(e.Graphics, bounds1, color, GInteractiveState.MouseOver, Orientation.Horizontal, 0, null, null);

            if (this.SnapActive)
                this.PaintMagnetLine(e, bounds2.GetPoint(ContentAlignment.MiddleLeft).Value, bounds1.GetPoint(ContentAlignment.MiddleLeft).Value, true);

            color = this.SnapActive ? this.SampleMovedColor : this.SampleFixColor;
            GPainter.DrawButtonBase(e.Graphics, bounds2, color, GInteractiveState.MouseOver, Orientation.Horizontal, 0, null, null);
        }
        /// <summary>
        /// Vykreslí sample typu: OriginalTime
        /// </summary>
        /// <param name="e"></param>
        protected void PaintOriginalTime(PaintEventArgs e)
        { }
        /// <summary>
        /// Vykreslí sample typu: GridSnap
        /// </summary>
        /// <param name="e"></param>
        protected void PaintGridSnap(PaintEventArgs e)
        { }
        /// <summary>
        /// Vykreslí spojovací magnetovou linku
        /// </summary>
        /// <param name="e"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="sameY"></param>
        protected void PaintMagnetLine(PaintEventArgs e, Point source, Point target, bool sameY = false)
        {
            if (sameY) target.Y = source.Y;
            using (GPainter.GraphicsUseSmooth(e.Graphics))
            {
                Pen pen = Skin.Pen(this.SampleMagnetLinkColor, width: 3, endCap: LineCap.ArrowAnchor);
                e.Graphics.DrawLine(pen, source, target);
            }
        }
        #endregion
        #region Public properties
        /// <summary>
        /// Aktivita tohoto konkrétního přichycení
        /// </summary>
        [Description("Aktivita tohoto konkrétního přichycení")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(true)]
        public bool SnapActive
        {
            get { return this._SnapActive; }
            set { this._SnapActive = value; this.Refresh(); }
        }
        private bool _SnapActive;
        /// <summary>
        /// Druh ilustračního obrázku
        /// </summary>
        [Description("Druh ilustračního obrázku")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(ConfigSnapImageType), "Sequence")]
        public ConfigSnapImageType ImageType
        {
            get { return this._ImageType; }
            set { this._ImageType = value; this.Refresh(); }
        }
        private ConfigSnapImageType _ImageType;
        /// <summary>
        /// Vzdálenost přichycení v pixelech
        /// </summary>
        [Description("Vzdálenost přichycení v pixelech")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(16)]
        public int SnapDistance
        {
            get { return this._SnapDistance; }
            set { this._SnapDistance = (value < 0 ? 0 : (value > 50 ? 50 : value)); this.Refresh(); }
        }
        private int _SnapDistance;
        /// <summary>
        /// Barva pozadí pod vzorky
        /// </summary>
        [Description("Barva pozadí pod vzorky")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "LightGray")]
        public Color SampleBackColor
        {
            get { return this._SampleBackColor; }
            set { this._SampleBackColor = value; this.Refresh(); }
        }
        private Color _SampleBackColor;
        /// <summary>
        /// Barva prvku, který je pevný
        /// </summary>
        [Description("Barva prvku, který je pevný")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "DarkGray")]
        public Color SampleFixColor
        {
            get { return this._SampleFixColor; }
            set { this._SampleFixColor = value; this.Refresh(); }
        }
        private Color _SampleFixColor;
        /// <summary>
        /// Barva prvku, který se pohybuje
        /// </summary>
        [Description("Barva prvku, který se pohybuje")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "DarkSeaGreen")]
        public Color SampleMovedColor
        {
            get { return this._SampleMovedColor; }
            set { this._SampleMovedColor = value; this.Refresh(); }
        }
        private Color _SampleMovedColor;

        /// <summary>
        /// Barva spojovací linky magnetu
        /// </summary>
        [Description("Barva spojovací linky magnetu")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "DarkViolet")]
        public Color SampleMagnetLinkColor
        {
            get { return this._SampleMagnetLinkColor; }
            set { this._SampleMagnetLinkColor = value; this.Refresh(); }
        }
        private Color _SampleMagnetLinkColor;
        #endregion
    }
    /// <summary>
    /// Typ ilustračního obrázku
    /// </summary>
    public enum ConfigSnapImageType
    {
        /// <summary>
        /// Bez přichytávání
        /// </summary>
        None,
        /// <summary>
        /// Sekvence = navazující prvky, za sebou jdoucí operace
        /// </summary>
        Sequence,
        /// <summary>
        /// Vnitřní prvek ve vnějším prvku, začátek operace k začátku směny
        /// </summary>
        InnerItem,
        /// <summary>
        /// Přidržet se původního času
        /// </summary>
        OriginalTime,
        /// <summary>
        /// Přichytávat k rastru
        /// </summary>
        GridSnap
    }
    #endregion
}
