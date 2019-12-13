using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// GTextEdit (=TextBox)
    /// </summary>
    public class GTextEdit : GTextObject
    {
        #region Konstruktor, privátní život
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GTextEdit()
        {
            this.BackgroundMode = DrawBackgroundMode.Solid;
            this.Is.Set(InteractiveProperties.Bit.DefaultMouseOverProperties
                      | InteractiveProperties.Bit.KeyboardInput);
        }
        #endregion
        #region Myš
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedLeftClick(e);
            this.MouseClickPoint = e.MouseAbsolutePoint;
        }
        protected Point? MouseClickPoint;
        #endregion
        #region Klávesnice
        #endregion
        #region Vykreslení obsahu
        /// <summary>
        /// Zajistí krslení TextBoxu
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            Rectangle innerBounds = this.GetInnerBounds(absoluteBounds);
            this.DrawBackground(e, absoluteBounds, absoluteVisibleBounds, innerBounds, drawMode);     // Background
            this.DrawRelation(e, absoluteBounds, absoluteVisibleBounds, innerBounds, drawMode);       // Podtržení od vztahu
            this.DrawText(e, absoluteBounds, absoluteVisibleBounds, innerBounds, drawMode);           // Text
            this.DrawBorder(e, absoluteBounds, absoluteVisibleBounds, innerBounds, drawMode);         // Rámeček
        }
        /// <summary>
        /// Vykreslí pozadí
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="innerBounds"></param>
        /// <param name="drawMode"></param>
        protected virtual void DrawBackground(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, Rectangle innerBounds, DrawItemMode drawMode)
        {
            this.DrawBackground(e, innerBounds, absoluteVisibleBounds, drawMode, this.CurrentBackColor);
        }
        /// <summary>
        /// Vykreslí linku podtržení vztahu podle <see cref="RelationType"/>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="innerBounds"></param>
        /// <param name="drawMode"></param>
        protected virtual void DrawRelation(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, Rectangle innerBounds, DrawItemMode drawMode)
        {
            var relationType = this.RelationType;
            if (relationType == TextRelationType.ToRecord || relationType == TextRelationType.ToDocument)
                GPainter.DrawRelationLine(e.Graphics, innerBounds, false, (relationType == TextRelationType.ToDocument));
        }
        /// <summary>
        /// Vykreslí obsah (text)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="innerBounds"></param>
        /// <param name="drawMode"></param>
        protected virtual void DrawText(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, Rectangle innerBounds, DrawItemMode drawMode)
        {
            string text = this.Text;
            if (!String.IsNullOrEmpty(text))
            {
                if (this.HasFocus)
                {
                    RectangleF[] charPoss = GPainter.DrawStringMeasureChars(e.Graphics, text, this.Font, innerBounds, this.Alignment, color: this.ForeColor);

                    Color[] overColors = new Color[] { Color.FromArgb(64, 220, 255, 220), Color.FromArgb(64, 255, 220, 220), Color.FromArgb(64, 220, 220, 255) };
                    int alpha = 128;
                    int loval = 160;
                    int hival = 220;
                    overColors = new Color[] { Color.FromArgb(alpha, loval, hival, loval), Color.FromArgb(alpha, hival, loval, loval), Color.FromArgb(alpha, loval, loval, hival) };
                    int idx = 0;
                    foreach (var charPos in charPoss)
                    {
                        Color color = overColors[idx % 3];
                        e.Graphics.FillRectangle(Skin.Brush(color), charPos);
                        idx++;
                    }

                    if (this.MouseClickPoint.HasValue)
                    {
                        RectangleF? cursor = FindCursorByPoint(this.MouseClickPoint.Value, charPoss);
                        if (cursor.HasValue)
                            e.Graphics.FillRectangle(Brushes.Black, cursor.Value);
                        Host.AnimationStart(this.ShowCursorTick);
                    }
                }
                else
                    GPainter.DrawString(e.Graphics, text, this.Font, innerBounds, this.Alignment, color: this.ForeColor);
            }
        }
        protected AnimationResult ShowCursorTick()
        {


            return AnimationResult.Stop;
        }
        protected RectangleF? FindCursorByPoint(Point point, RectangleF[] charPoss)
        {
            int count = charPoss.Length;
            float x = point.X;
            float y = point.Y;
            for (int i = 0; i < count; i++)
            {
                RectangleF pos = charPoss[i];
                if (x >= pos.X && x < pos.Right)
                {
                    if (x < (pos.X + 0.667f * pos.Width))
                        return new RectangleF(pos.X, pos.Y, 1, pos.Height);
                    return new RectangleF(pos.Right, pos.Y, 1, pos.Height);
                }
            }
            return null;
        }
        /// <summary>
        /// Vykreslí border
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="innerBounds"></param>
        /// <param name="drawMode"></param>
        protected virtual void DrawBorder(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, Rectangle innerBounds, DrawItemMode drawMode)
        {
            switch (this.BorderStyle.Value)
            {
                case BorderStyleType.Flat:
                    GPainter.DrawBorder(e.Graphics, absoluteBounds, RectangleSide.All, null, this.CurrentBorderColor, 0f);
                    break;
                case BorderStyleType.Effect3D:
                    GPainter.DrawBorder(e.Graphics, absoluteBounds, RectangleSide.All, null, this.CurrentBorderColor, this.CurrentBorder3DEffect);
                    break;
                case BorderStyleType.Soft:
                    GPainter.DrawSoftBorder(e.Graphics, absoluteBounds, RectangleSide.All, this.CurrentSoftBorderColor);
                    break;
            }
        }
        #region Support pro kreslení obsahu - aktuální barvy
        /// <summary>
        /// Vrací souřadnice vnitřního prostoru po odečtení prostoru pro Border (0-1-2 pixely)
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        protected Rectangle GetInnerBounds(Rectangle bounds)
        {
            switch (this.BorderStyle.Value)
            {
                case BorderStyleType.None: return bounds;
                case BorderStyleType.Flat:
                case BorderStyleType.Effect3D: return bounds.Enlarge(-1);
                case BorderStyleType.Soft: return bounds.Enlarge(-2);
            }
            return bounds;
        }
        /// <summary>
        /// Aktuální barva okrajů
        /// </summary>
        protected Color CurrentBorderColor
        {
            get
            {
                var state = this.InteractiveState;
                if (state.HasFlag(GInteractiveState.Disabled)) return Skin.TextBox.PassiveBorderColor;
                if (this.HasFocus) return Skin.TextBox.ActiveBorderColor;
                if (state.HasFlag(GInteractiveState.FlagOver)) return Skin.TextBox.ActiveBorderColor;
                return Skin.TextBox.PassiveBorderColor;
            }
        }
        /// <summary>
        /// Aktuální barva okrajů
        /// </summary>
        protected Color CurrentSoftBorderColor
        {
            get
            {
                var state = this.InteractiveState;
                if (state.HasFlag(GInteractiveState.Disabled)) return Skin.TextBox.PassiveSoftBorderColor;
                if (this.HasFocus) return Skin.TextBox.ActiveSoftBorderColor;
                if (state.HasFlag(GInteractiveState.FlagOver)) return Skin.TextBox.ActiveSoftBorderColor;
                return Skin.TextBox.PassiveSoftBorderColor;
            }
        }
        /// <summary>
        /// Aktuální barva pozadí
        /// </summary>
        protected Color CurrentBackColor
        {
            get
            {
                var state = this.InteractiveState;
                if (state.HasFlag(GInteractiveState.Disabled)) return Skin.TextBox.DisabledBackColor;
                if (this.HasFocus) return Skin.TextBox.ActiveBackColor;
                if (state.HasFlag(GInteractiveState.FlagOver)) return Skin.TextBox.MouseOverBackColor;
                return Skin.TextBox.EnabledBackColor;
            }
        }
        /// <summary>
        /// Aktuální barva písma
        /// </summary>
        protected Color CurrentTextColor
        {
            get
            {
                var state = this.InteractiveState;
                if (state.HasFlag(GInteractiveState.Disabled)) return Skin.TextBox.DisabledForeColor;
                if (this.HasFocus) return Skin.TextBox.ActiveForeColor;
                if (state.HasFlag(GInteractiveState.FlagOver)) return Skin.TextBox.MouseOverForeColor;
                return Skin.TextBox.EnabledForeColor;
            }
        }
        /// <summary>
        /// Aktuální hodnota 3D efektu pro Border
        /// </summary>
        protected float CurrentBorder3DEffect
        {
            get
            {
                var state = this.InteractiveState;
                if (state.HasFlag(GInteractiveState.Disabled)) return 0f;
                if (this.HasFocus) return -0.45f;
                if (state.HasFlag(GInteractiveState.FlagOver)) return -0.25f;
                return -0.15f;
            }
        }
        #endregion
        #endregion
        #region Public členové
        /// <summary>
        /// Typ vztahu - pro správné vykreslování (linka podtržení)
        /// </summary>
        public TextRelationType RelationType { get; set; }
        /// <summary>
        /// Typ borderu
        /// </summary>
        public BorderStyleType xBorderStyle { get; set; }


        /// <summary>
        /// Aktuální typ rámečku textboxu. Při čtení má vždy hodnotu (nikdy není null).
        /// Dokud není explicitně nastavena hodnota, vrací se hodnota <see cref="BorderStyleDefault"/>.
        /// Lze setovat konkrétní explicitní hodnotu, anebo hodnotu null = tím se resetuje na barvu defaultní <see cref="BorderStyleDefault"/>.
        /// </summary>
        public BorderStyleType? BorderStyle
        {
            get { return this.__BorderStyle ?? this.BorderStyleDefault; }
            set { this.__BorderStyle = value; }
        }
        private BorderStyleType? __BorderStyle;
        /// <summary>
        /// Defaultní písmo
        /// </summary>
        public virtual BorderStyleType BorderStyleDefault { get { return Skin.TextBox.BorderStyle; } }

        #endregion
    }
    #region enum TextRelationType
    /// <summary>
    /// Typ vztahu - pro správné vykreslování (linka podtržení)
    /// </summary>
    public enum TextRelationType
    {
        /// <summary>
        /// Není vztah
        /// </summary>
        None = 0,
        /// <summary>
        /// Vztah na záznam (modrý)
        /// </summary>
        ToRecord,
        /// <summary>
        /// Vztah na dokument (žlutý)
        /// </summary>
        ToDocument
    }
    #endregion
}
