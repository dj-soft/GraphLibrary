using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class ToolTipItem : řídící objekt pro zobrazení ToolTipu na základě dat v ToolTipData
    /// <summary>
    /// ToolTipItem : for drawing tooltip informations
    /// </summary>
    public class ToolTipItem
    {
        #region Public members
        /// <summary>
        /// Store new active point for TooTip.
        /// </summary>
        /// <param name="point">Target point of ToolTip, in coordinates of Control (=AbsoluteVisible)</param>
        public void ToolTipSet(Point? point)
        {
            this.Invalidate();
            if (point.HasValue && this._Data != null)
            {
                this.Point = point;
            }
            else
            {
                this.Point = null;
                this.Data = null;
            }
        }
        /// <summary>
        /// Store new active point and Data for TooTip.
        /// </summary>
        /// <param name="point">Target point of ToolTip, in coordinates of Control (=AbsoluteVisible)</param>
        /// <param name="data">Data for Tooltip</param>
        public void ToolTipSet(Point? point, ToolTipData data)
        {
            this.Invalidate();
            if (point.HasValue && data != null)
            {
                this.Point = point;
                this.Data = data.Clone;
            }
            else
            {
                this.Point = null;
                this.Data = null;
            }
        }
        /// <summary>
        /// Reset (=Hide) this Tooltip
        /// </summary>
        public void ToolTipClear()
        {
            this.Point = null;
            this.Data = null;
        }
        /// <summary>
        /// ToolTip has data for drawing (have Point, have Data and have Title or Info text plus font)?
        /// </summary>
        public bool NeedDraw { get { return (this.Point.HasValue && this.DataIsValid && this.IsVisible && this.ToolTipExist); } }
        #endregion
        #region Properties for ToolTip
        /// <summary>
        /// Current mouse location. Can be changed everywhile. Cause Fade-Out of toooltip, or restart for Wait for ToolTip.
        /// </summary>
        public Point? MouseLocationCurrent { get { return this._MouseLocationCurrent; } set { this._MouseLocationCurrent = value; this.AnimationRefreshMouse(); } } private Point? _MouseLocationCurrent;
        /// <summary>
        /// Target point of ToolTip, in coordinates of Control (=AbsoluteVisible)
        /// </summary>
        protected Point? Point { get { return this._Point; } set { this.InvalidateLocation(); this._Point = value; } }
        /// <summary>
        /// Data for ToolTip
        /// </summary>
        protected ToolTipData Data { get { return this._Data; } set { this.Invalidate(); this._Data = value; this.AnimationRefresh(); } }
        /// <summary>
        /// true when data is valid (Point and Data is not null)
        /// </summary>
        protected bool DataIsValid { get { return (this._Data != null && this._Data.IsValid); } }
        /// <summary>
        /// Is tooltip Visible? setting false = hide tooltip.
        /// </summary>
        protected bool IsVisible { get { return (this._Data != null && this._Data.IsVisible); } }
        /// <summary>
        /// Shape type of ToolTip
        /// </summary>
        protected TooltipShapeType ShapeType { get { return (this.DataIsValid && this._Data.ShapeType.HasValue ? this._Data.ShapeType.Value : DefaultShapeType); } }
        /// <summary>
        /// Color for Border line
        /// </summary>
        protected Color BorderColor { get { return (this.DataIsValid && this._Data.BorderColor.HasValue ? this._Data.BorderColor.Value : DefaultBorderColor); } }
        /// <summary>
        /// Title to ToolTip
        /// </summary>
        protected string TitleText { get { return (this.DataIsValid ? this._Data.TitleText : null); } }
        /// <summary>
        /// Back color for Title text
        /// </summary>
        protected Color TitleBackColor { get { return (this.DataIsValid && this._Data.TitleBackColor.HasValue ? this._Data.TitleBackColor.Value : DefaultBackColor); } }
        /// <summary>
        /// Font color for Title text
        /// </summary>
        protected Color TitleFontColor { get { return (this.DataIsValid && this._Data.TitleFontColor.HasValue ? this._Data.TitleFontColor.Value : DefaultTitleColor); } }
        /// <summary>
        /// Font for Title text
        /// </summary>
        protected FontInfo TitleFont { get { return (this.DataIsValid && this._Data.TitleFont != null ? this._Data.TitleFont : DefaultTitleFont); } }
        /// <summary>
        /// Info Text to ToolTip
        /// </summary>
        protected string InfoText { get { return (this.DataIsValid ? this._Data.InfoText : null); } }
        /// <summary>
        /// Icon before Info Text to ToolTip
        /// </summary>
        protected Image Icon { get { return (this.DataIsValid ? this._Data.Icon : null); } }
        /// <summary>
        /// Image namísto hlavního textu
        /// </summary>
        protected Image Image { get { return (this.DataIsValid ? this._Data.Image : null); } }
        /// <summary>
        /// Layout of Icon - Title - Info in Tooltip
        /// </summary>
        protected ToolTipLayoutType ToolTipLayout { get { return (this.DataIsValid ? this._Data.ToolTipLayout : ToolTipLayoutType.IconBeforeTitle); } }
        /// <summary>
        /// Back color for Info text
        /// </summary>
        protected Color InfoBackColor { get { return (this.DataIsValid && this._Data.InfoBackColor.HasValue ? this._Data.InfoBackColor.Value : DefaultBackColor); } }
        /// <summary>
        /// Opacity for Info background
        /// </summary>
        protected Int32? InfoBackOpacity { get { return (this.DataIsValid && this._Data.Opacity.HasValue ? this._Data.Opacity : this.DefaultOpacity); } }
        /// <summary>
        /// Default opacity, when InfoBackOpacity is null
        /// </summary>
        protected Int32? DefaultOpacity { get { return 216; } }
        /// <summary>
        /// Font color for Info text
        /// </summary>
        protected Color InfoFontColor { get { return (this.DataIsValid && this._Data.InfoFontColor.HasValue ? this._Data.InfoFontColor.Value : DefaultInfoColor); } }
        /// <summary>
        /// Font for Info text
        /// </summary>
        protected FontInfo InfoFont { get { return (this.DataIsValid && this._Data.InfoFont != null ? this._Data.InfoFont : DefaultInfoFont); } }
        /// <summary>
        /// Shape type of ToolTip
        /// </summary>
        protected static TooltipShapeType DefaultShapeType { get { return TooltipShapeType.Rectangle; } }
        /// Color for Border line
        /// </summary>
        protected static Color DefaultBorderColor { get { return Skin.ToolTip.BorderColor; } }
        /// <summary>
        /// Back color for Title and Info text
        /// </summary>
        protected static Color DefaultBackColor { get { return Skin.ToolTip.BackColor; } }
        /// <summary>
        /// Font color for Title text
        /// </summary>
        protected static Color DefaultTitleColor { get { return Skin.ToolTip.TitleColor; } }
        /// <summary>
        /// Font for Title text
        /// </summary>
        protected static FontInfo DefaultTitleFont { get { return FontInfo.IconTitleBold; } }
        /// <summary>
        /// Font color for Info text
        /// </summary>
        protected static Color DefaultInfoColor { get { return Skin.ToolTip.InfoColor; } }
        /// <summary>
        /// Font for Info text
        /// </summary>
        protected static FontInfo DefaultInfoFont { get { return FontInfo.IconTitle; } }
        #endregion
        #region Animation (fade-in, wait, fade-out)
        /// <summary>
        /// ToolTip need animation (=repeatedly calling AnimateStep() method)
        /// </summary>
        public bool NeedAnimation { get { return (this.NeedDraw && this.AnimationActive); } }
        private void AnimationInit()
        {
            this._AnimationDict = new Dictionary<AnimationPhase, AnimationState>();
            this._AnimationDict.Add(AnimationPhase.None, new AnimationState(AnimationPhase.None, 0, 1, 1, AnimationPhase.None));
            this._AnimationDict.Add(AnimationPhase.Wait, new AnimationState(AnimationPhase.Wait, 1, 0, 0, AnimationPhase.FadeIn));
            this._AnimationDict.Add(AnimationPhase.RepeatedWait, new AnimationState(AnimationPhase.RepeatedWait, 1, 0, 0, AnimationPhase.FadeIn));
            this._AnimationDict.Add(AnimationPhase.FadeIn, new AnimationState(AnimationPhase.FadeIn, 1, 0, 1, AnimationPhase.Show));
            this._AnimationDict.Add(AnimationPhase.Show, new AnimationState(AnimationPhase.Show, 1, 1, 1, AnimationPhase.FadeOut));
            this._AnimationDict.Add(AnimationPhase.FadeOut, new AnimationState(AnimationPhase.FadeOut, 1, 1, 0, AnimationPhase.End));
            this._AnimationDict.Add(AnimationPhase.End, new AnimationState(AnimationPhase.End, 0, 0, 0, AnimationPhase.End));
            this.AnimationCurrentPhase = AnimationPhase.None;
        }
        /// <summary>
        /// Refresh inner data for animation.
        /// Is called after to this.Data is stored new value.
        /// </summary>
        /// <remarks>This method is allways called from GUI thread, as result of any interactive actions in Host Control</remarks>
        private void AnimationRefresh()
        {
            ToolTipData data = null;
            lock (this._AnimationDict)
            {
                this.AnimationCurrentPhase = AnimationPhase.None;

                data = this._Data;

                if (data == null)
                    Application.App.Trace.Info(Application.TracePriority.Priority2_Lowest, "ToolTipItem", "AnimationRefresh", "Test", "Data: NULL");
                else
                    Application.App.Trace.Info(Application.TracePriority.Priority2_Lowest, "ToolTipItem", "AnimationRefresh", "Test", "Data: Exists", "Text: " + data.InfoText, "AnimationExists: " + (data.AnimationExists ? "True" : "False"));


                if (data == null || !data.AnimationExists)
                    return;

                this._AnimationDict[AnimationPhase.Wait].Prepare(data.AnimationWaitBeforeTime);
                this._AnimationDict[AnimationPhase.RepeatedWait].Prepare(data.AnimationWaitRepeatedBeforeTime);
                this._AnimationDict[AnimationPhase.FadeIn].Prepare(data.AnimationFadeInTime);
                this._AnimationDict[AnimationPhase.Show].Prepare(data.AnimationShowTime);
                this._AnimationDict[AnimationPhase.FadeOut].Prepare(data.AnimationFadeOutTime);

                this.AnimationCurrentPhase = AnimationPhase.Wait;
            }

            Application.App.Trace.Info(Application.TracePriority.Priority2_Lowest, "ToolTipItem", "AnimationRefresh", "Result", "Text: " + data.InfoText, "AnimationCurrentPhase: " + (this.AnimationCurrentPhase.ToString()));
        }
        /// <summary>
        /// This is called after each MouseMove.
        /// We will react only in some phases...
        /// </summary>
        private void AnimationRefreshMouse()
        {
            AnimationPhase currentPhase = this.AnimationCurrentPhase;
            if (currentPhase == AnimationPhase.None || currentPhase == AnimationPhase.FadeOut) return;
            
            bool isCtrl = (Control.ModifierKeys == Keys.Control);
            if (isCtrl) return;

            lock (this._AnimationDict)
            {
                currentPhase = this.AnimationCurrentPhase;
                switch (currentPhase)
                {
                    case AnimationPhase.Wait:
                    case AnimationPhase.RepeatedWait:
                        this.AnimationCurrentState.Restart();
                        break;
                    case AnimationPhase.FadeIn:
                        float currentAlpha = this.AnimationCurrentState.AlphaCurrent;
                        this.AnimationCurrentPhase = AnimationPhase.FadeOut;
                        this.AnimationCurrentState.PrepareForAlpha(currentAlpha);
                        break;
                    case AnimationPhase.Show:
                        this.GoToNextPhase(false);
                        break;
                    case AnimationPhase.End:
                        this.AnimationCurrentPhase = AnimationPhase.RepeatedWait;
                        break;
                }
            }
        }
        /// <summary>
        /// Perform one step in animation.
        /// Return true when need Draw, false when is not visual change.
        /// </summary>
        /// <remarks>This method is allways called from background thread, as result of Background tick animations in Host Control</remarks>
        public bool AnimateTick()
        {
            string text = null;
            float alphaOld = 0f;
            AnimationPhase phaseOld = AnimationPhase.None;
            float alphaNew = 0f;
            AnimationPhase phaseNew = AnimationPhase.None;
            int timeRemaining = 0;
            bool resultDraw = false;

            lock (this._AnimationDict)
            {
                text = (this._Data != null ? this._Data.InfoText : "null");
                if (!this.AnimationActive)
                {
                    Application.App.Trace.Info(Application.TracePriority.Priority2_Lowest, "ToolTipItem", "AnimateStep", "Skip", "Text: " + text, "AnimationActive: False");
                    return false;
                }

                alphaOld = this.AnimationAlpha;
                phaseOld = this.AnimationCurrentPhase;
                
                bool isPhaseDone = this.AnimationCurrentState.Tick();
                if (isPhaseDone)
                    this.GoToNextPhase(false);

                alphaNew = this.AnimationAlpha;
                phaseNew = this.AnimationCurrentPhase;
                timeRemaining = this.AnimationCurrentState.TimeRemaining;
                resultDraw = (alphaNew != alphaOld);
            }

            Application.App.Trace.Info(Application.TracePriority.Priority2_Lowest, "ToolTipItem", "AnimateStep", "Tick", "Text: " + text, 
                "AlphaOld: " + alphaOld.ToString(),
                "PhaseOld: " + phaseOld.ToString(),
                "AlphaNew: " + alphaNew.ToString(),
                "PhaseNew: " + phaseOld.ToString(),
                "TimeRemaining: " + timeRemaining.ToString(),
                "ResultDraw: " + (resultDraw ? "True" : "False"));

            return resultDraw;
        }
        /// <summary>
        /// Go to next phase.
        /// When parameter "skipEmptyPhases" is true, then skip all next phases, which time is zero.
        /// </summary>
        /// <param name="skipEmptyPhases"></param>
        protected void GoToNextPhase(bool skipEmptyPhases)
        {
            AnimationPhase oldPhase = this.AnimationCurrentPhase;
            for (int tx = 0; tx < 5; tx++)
            {   // tx is only time-out for this loop:
                this.AnimationCurrentPhase = this.AnimationCurrentState.NextPhase;
                bool isZero = this.AnimationCurrentState.Restart();

                if (!isZero) break;              // Next phase is not empty (its Time is positive): break => next phase is valid.
                if (!skipEmptyPhases) break;     // Next phase is empty. When does not skip empty phases, break => next phase (although empty) is valid.
                // We will perform next loop = current phase = NextPhase:
            }

            if (oldPhase == AnimationPhase.Wait || oldPhase == AnimationPhase.RepeatedWait)
                this.AnimateReloadPoint();
        }
        /// <summary>
        /// Reload current mouse point (MouseLocationCurrent) as origin point for this tooltip (this.Point)
        /// </summary>
        private void AnimateReloadPoint()
        {
            this.Point = this._MouseLocationCurrent;
        }
        protected bool AnimationActive { get { return this.AnimationCurrentState.IsActive; } }
        /// <summary>
        /// Data for current animation state
        /// </summary>
        protected AnimationState AnimationCurrentState { get { return this._AnimationDict[this.AnimationCurrentPhase]; } }
        /// <summary>
        /// Alpha channel (=opacity) for animated ToolTip (wait, fade-in, show, fade-out)
        /// </summary>
        protected float AnimationAlpha { get { return this.AnimationCurrentState.AlphaCurrent; } }
        /// <summary>
        /// Current animation phase.
        /// Setting a new value will change object in AnimationCurrentState to appropriate state.
        /// </summary>
        protected AnimationPhase AnimationCurrentPhase { get { return this._AnimationCurrentPhase; } set { this._AnimationCurrentPhase = value; } } private AnimationPhase _AnimationCurrentPhase;
        /// <summary>
        /// Dictionary with all animation states
        /// </summary>
        protected Dictionary<AnimationPhase, AnimationState> _AnimationDict;
        protected class AnimationState
        {
            public AnimationState(AnimationPhase phase, int tickStep, float alphaBegin, float alphaEnd, AnimationPhase nextPhase)
            {
                this.Phase = phase;
                this.TickStep = tickStep;
                this.AlphaBegin = alphaBegin;
                this.AlphaEnd = alphaEnd;
                this.AlphaCurrent = alphaBegin;
                this.NextPhase = nextPhase;
            }
            /// <summary>
            /// Prepare this phase for explicit specified time.
            /// Calculate inner values (TimeTotal and TimeRemaining) by this time for ticks called every 40 miliseconds (25/sec).
            /// </summary>
            /// <param name="time"></param>
            public void Prepare(TimeSpan time)
            {
                int ticks = (int)(Math.Round(time.TotalSeconds * 25d, 0));
                this.TimeTotal = ticks;
                this.TimeRemaining = ticks;
                this.AlphaCurrent = this.AlphaBegin;
                this.IsDone = (ticks <= 0);
            }
            /// <summary>
            /// Restart values to begin this phase:
            /// TimeRemaining = TimeTotal; AlphaCurrent = AlphaBegin; IsDone = (TimeRemaining is zero or negative);
            /// Return true when this phase is done (this is: remaining time is zero).
            /// </summary>
            public bool Restart()
            {
                this.TimeRemaining = this.TimeTotal;
                this.AlphaCurrent = this.AlphaBegin;
                this.IsDone = (this.TimeRemaining <= 0);
                return this.IsDone;
            }
            /// <summary>
            /// Go to End values
            /// </summary>
            public void GoEnd()
            {
                this.TimeRemaining = 0;
                this.AlphaCurrent = this.AlphaEnd;
                this.IsDone = true;
            }
            /// <summary>
            /// Calculate this.AlphaCurrent valid after this time tick.
            /// Return true when this phase is done (this is: remaining time is zero).
            /// </summary>
            /// <returns></returns>
            public bool Tick()
            {
                if (this.TickStep <= 0) return false;

                // Phase Show is infinite during Ctrl key is pressed:
                bool isInfinite = (this.Phase == AnimationPhase.Show && (Control.ModifierKeys == Keys.Control));

                if (!isInfinite)
                {
                    if (this.TimeRemaining > 0)
                    {
                        this.TimeRemaining -= this.TickStep;
                        this.AlphaCurrent = this.AlphaValue;
                    }
                    if (this.TimeRemaining == 0)
                        this.IsDone = true;
                }

                return this.IsDone;
            }
            /// <summary>
            /// Prepare inner values (TimeRemaining and AlphaCurrent) that next Tick() calling will continue from specified current alpha value.
            /// </summary>
            /// <param name="alpha"></param>
            public void PrepareForAlpha(float alpha)
            {
                float distance = this.AlphaDistance;
                if (distance == 0f)
                {   // this phase is "constant":
                    this.Restart();
                }
                else if (distance > 0f)
                {   // Increasing phase:
                    if (alpha <= this.AlphaBegin)
                        this.Restart();
                    else if (alpha >= this.AlphaEnd)
                        this.GoEnd();
                    else
                    {   // Increasing phase, go to alpha between Begin and End alpha:
                        float ratio = (alpha - this.AlphaBegin) / distance;              // Ratio from alpha value and distance
                        float time = (float)this.TimeTotal * ratio;                      // Time appropriate this alpha ratio
                        this.TimeRemaining = ((int)(time)) - 1;
                        this.AlphaCurrent = this.AlphaValue;
                    }
                }
                else
                {   // Decreasing phase:
                    if (alpha >= this.AlphaBegin)
                        this.Restart();
                    else if (alpha <= this.AlphaEnd)
                        this.GoEnd();
                    else
                    {   // Decreasing phase, go to alpha between Begin and End alpha:
                        float ratio = 1f - ((alpha - this.AlphaBegin) / distance);       // Ratio from alpha value and distance
                        float time = (float)this.TimeTotal * ratio;                      // Time appropriate this alpha ratio
                        this.TimeRemaining = ((int)(time)) - 1;
                        this.AlphaCurrent = this.AlphaValue;
                    }
                }
            }
            /// <summary>
            /// Current TimeRatio:
            /// 0 = before First Tick;
            /// 1 = after Last Tick;
            /// for example 0.25f = on 25% time from start to last time
            /// </summary>
            protected float TimeRatio { get { return (1f - (((float)this.TimeRemaining) / ((float)this.TimeTotal))); } }
            /// <summary>
            /// Distance from AlphaBegin to AlphaEnd:
            /// 0 = no change, positive value = increase alpha during time, negative value = decrease alpha during time.
            /// </summary>
            protected float AlphaDistance { get { return (float)(this.AlphaEnd - this.AlphaBegin); } }
            /// <summary>
            /// Calculated Alpha value for current TimeTotal, TimeRemaining, AlphaBegin, AlphaEnd (with using TimeRatio and AlphaDistance).
            /// </summary>
            protected float AlphaValue
            {
                get
                {
                    if (this.TimeRemaining >= this.TimeTotal) return this.AlphaBegin;
                    if (this.TimeRemaining <= 0) return this.AlphaEnd;
                    return this.AlphaBegin + (this.TimeRatio * this.AlphaDistance);    // for ratio = 0: alpha = AlphaBegin; for ratio = 1: alpha = AlphaEnd
                }
            }
            /// <summary>
            /// This phase
            /// </summary>
            public AnimationPhase Phase { get; private set; }
            /// <summary>
            /// Next phase
            /// </summary>
            public AnimationPhase NextPhase { get; private set; }
            /// <summary>
            /// Time step after one time tick. +1 for animated phases, 0 for final phase.
            /// </summary>
            public int TickStep { get; private set; }
            /// <summary>
            /// Alpha chanell on Begin
            /// </summary>
            public float AlphaBegin { get; private set; }
            /// <summary>
            /// Alpha chanell on End
            /// </summary>
            public float AlphaEnd { get; private set; }
            /// <summary>
            /// Total time for this phase (in ticks)
            /// </summary>
            public int TimeTotal { get; private set; }
            /// <summary>
            /// Remaining time for this phase (in ticks)
            /// </summary>
            public int TimeRemaining { get; private set; }
            /// <summary>
            /// Current value of Alpha chanell
            /// </summary>
            public float AlphaCurrent { get; private set; }
            /// <summary>
            /// true when this phase is done, and we need go to next phase
            /// </summary>
            public bool IsDone { get; private set; }
            /// <summary>
            /// true when this phase need another animation
            /// </summary>
            public bool IsActive { get { return (this.TickStep > 0 && this.TimeRemaining > 0); } }
        }
        protected enum AnimationPhase { None, Wait, RepeatedWait, FadeIn, Show, FadeOut, End }
        #endregion
        #region Constructor, protected and private members
        internal ToolTipItem(Control owner)
        {
            this._Owner = owner;
            this.AnimationInit();
            this.ColorMatrixInit();
            this.Invalidate();
        }
        private Control _Owner;
        private Point? _Point;
        private ToolTipData _Data;
        /// <summary>
        /// true when ShapeType is other than None, and exists Title or Info (not empty text + not null font)
        /// </summary>
        protected bool ToolTipExist { get { return (this.ShapeType != TooltipShapeType.None && (this.TitleExist || this.InfoExist || this.ImageExist)); } }
        /// <summary>
        /// true when exists Title (not empty text + not null font)
        /// </summary>
        protected bool TitleExist { get { return (!String.IsNullOrEmpty(this.TitleText) && this.TitleFont != null); } }
        /// <summary>
        /// true when exists Info ((not empty text + not null font) or explicit request for ShowImage)
        /// </summary>
        protected bool InfoExist { get { return (!String.IsNullOrEmpty(this.InfoText) && this.InfoFont != null); } }
        /// <summary>
        /// true when exists Icon (is not null)
        /// </summary>
        protected bool IconExist { get { return (this.Icon != null); } }
        /// <summary>
        /// true pokud je zadán velký obrázek (Image)
        /// </summary>
        protected bool ImageExist { get { return (this.Image != null); } }
        #endregion
        #region Draw
        /// <summary>
        /// Draw current tooltip
        /// </summary>
        /// <param name="graphics"></param>
        internal void Draw(Graphics graphics)
        {
            if (!this.NeedDraw) return;
            if (!this.ToolTipExist) return;
            this.CheckValid(graphics);

            float alpha = this.AnimationAlpha;
            
            string text = (this._Data != null ? this._Data.InfoText : "null");
            Application.App.Trace.Info(Application.TracePriority.Priority2_Lowest, "ToolTipItem", "Draw", "Run", "Text: " + text, "AnimationAlpha: " + alpha.ToString());

            if (this._Image != null && this._TargetBounds.HasValue && alpha > 0f)
            {
                Rectangle bounds = this._TargetBounds.Value;
                
                if (alpha >= 1f)
                {   // Full opacity => Draw image (this._Image, cached content of ToolTip) without any alpha channel modification:
                    graphics.DrawImage(this._Image, bounds);
                }
                else
                {   // Draw tooltip from Image, with modification of alpha channel:
                    using (System.Drawing.Imaging.ImageAttributes imageAttributes = new System.Drawing.Imaging.ImageAttributes())
                    {
                        System.Drawing.Imaging.ColorMatrix colorMatrix = this.ColorMatrixForAlpha(alpha, true);
                        imageAttributes.SetColorMatrix(colorMatrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);

                        Rectangle srcRect = new Rectangle(0, 0, bounds.Width, bounds.Height);
                        graphics.DrawImage(this._Image, bounds, 0, 0, bounds.Width, bounds.Height, GraphicsUnit.Pixel, imageAttributes);
                    }
                }

            }
        }
        #endregion
        #region TimerDraw
        /// <summary>
        /// This event is called when ToolTip want be drawed by its own initiative.
        /// </summary>
        public event EventHandler TimerDrawRequest;
        #endregion
        #region ToolTip as Rectangle (Calculations and Draw)
        protected void CalculateRectangle()
        {
            this._CalculateTotalBoundsRelative();

            Rectangle pathBounds = this._TotalBounds.Value;
            int round = 2;
            // pathBounds.Width++;
            // pathBounds.Height++;
            this._TotalPath = GPainter.CreatePathRoundRectangle(pathBounds, round, round);
            
            this._TitlePath = new GraphicsPath();
            if (this._LineBounds.HasValue)
            {
                Rectangle lineBounds = this._LineBounds.Value;
                this._TitlePath.AddLine(lineBounds.X + 1, lineBounds.Y, lineBounds.Right - 5, lineBounds.Y);
            }
        }
        private void DrawRectangle(Graphics graphics)
        {
            using (GPainter.GraphicsUseSmooth(graphics))
            {
                Rectangle totalBackBounds = this._TotalBounds.Value; //  Rectangle.Truncate(this._TotalPath.GetBounds());
                using (Brush brush = Skin.CreateBrushForBackground(totalBackBounds, Orientation.Horizontal, GInteractiveState.Enabled /* .MouseOver*/, true, this.InfoBackColor, this.InfoBackOpacity))
                {
                    graphics.FillPath(brush, this._TotalPath);
                }

                if (this.TitleExist)
                {
                    graphics.DrawString(this.TitleText, this.TitleFont.Font, Skin.Brush(this.TitleFontColor), this._TitleBounds.Value);
                    graphics.DrawPath(Skin.Pen(this.BorderColor), this._TitlePath);
                }
                if (this.IconExist)
                {
                    graphics.DrawImage(this.Icon, this._IconBounds.Value);
                }
                if (this.InfoExist)
                {
                    graphics.DrawString(this.InfoText, this.InfoFont.Font, Skin.Brush(this.InfoFontColor), this._InfoBounds.Value);
                }
                if (this.ImageExist)
                {
                    graphics.DrawImage(this.Image, this._ImageBounds.Value);
                }

                graphics.DrawPath(Skin.Pen(this.BorderColor), this._TotalPath);
            }
        }
        #endregion
        #region ToolTip as RoundRectangle (Calculations and Draw)
        protected void CalculateRoundRectangle()
        {
            this._CalculateTotalBoundsRelative();

            Rectangle pathBounds = this._TotalBounds.Value;
            int round = 5;
            // pathBounds.Inflate(round, round);
            this._TotalPath = GPainter.CreatePathRoundRectangle(pathBounds, round, round);

            this._TitlePath = new GraphicsPath();
            if (this._LineBounds.HasValue)
            {
                Rectangle lineBounds = this._LineBounds.Value;
                this._TitlePath.AddLine(lineBounds.X + 1, lineBounds.Y, lineBounds.Right - 5, lineBounds.Y);
            }
        }
        private void DrawRoundRectangle(Graphics graphics)
        {
            using (GPainter.GraphicsUseSmooth(graphics))
            {
                Rectangle totalBackBounds = Rectangle.Ceiling(this._TotalPath.GetBounds());
                using (Brush brush = Skin.CreateBrushForBackground(totalBackBounds, Orientation.Horizontal, GInteractiveState.MouseOver, true, this.InfoBackColor, this.InfoBackOpacity))
                {
                    graphics.FillPath(brush, this._TotalPath);
                }

                if (this.TitleExist)
                {
                    graphics.DrawString(this.TitleText, this.TitleFont.Font, Skin.Brush(this.TitleFontColor), this._TitleBounds.Value);
                    graphics.DrawPath(Skin.Pen(this.BorderColor), this._TitlePath);
                }
                if (this.IconExist)
                {
                    graphics.DrawImage(this.Icon, this._IconBounds.Value);
                }
                if (this.InfoExist)
                {
                    graphics.DrawString(this.InfoText, this.InfoFont.Font, Skin.Brush(this.InfoFontColor), this._InfoBounds.Value);
                }
                if (this.ImageExist)
                {
                    graphics.DrawImage(this.Image, this._ImageBounds.Value);
                }

                graphics.DrawPath(Skin.Pen(this.BorderColor), this._TotalPath);
            }
        }
        #endregion
        #region ToolTip as Ellipse (Calculations and Draw)
        protected void CalculateEllipse()
        { }
        private void DrawEllipse(Graphics graphics)
        {
        }
        #endregion
        #region ToolTip as Window (Calculations and Draw)
        protected void CalculateWindow()
        { }
        private void DrawWindow(Graphics graphics)
        {
        }
        #endregion
        #region Invalidate, CheckValid - common calculation methods and properties
        /// <summary>
        /// Reset all private data (CachePointClear() + CacheDataClear())
        /// Does not reset other data (text sizes, fonts, pen brushes)
        /// </summary>
        protected void Invalidate()
        {
            this.InvalidateSize();
            this.InvalidateBounds();
            this.InvalidateImage();
        }
        protected void CheckValid(Graphics graphics)
        {
            if (!this.ToolTipExist) return;

            this.CheckValidSize(graphics);
            this.CheckValidBounds(graphics);
            this.CheckValidImage(graphics);
            this.CheckValidLocation(graphics);
        }
        #region Size for Title, Text, Icon, Total
        /// <summary>
        /// Invalidate sizes
        /// </summary>
        protected void InvalidateSize()
        {
            this._ShadowSize = null;
            this._TitleSize = null;
            this._TextSize = null;
            this._IconSize = null;
            this._ImageSize = null;
            this._TotalSize = null;

            this.InvalidateBounds();
        }
        /// <summary>
        /// Check and calculate _TitleSize and _InfoSize from (this.TitleText, this.TitleFont) and (this.InfoText, this.InfoFont).
        /// </summary>
        protected void CheckValidSize(Graphics graphics)
        {
            if (!this.ToolTipExist) return;
            if (this._ShadowSize.HasValue && this._TitleSize.HasValue && this._TextSize.HasValue && this._IconSize.HasValue && this._ImageSize.HasValue && this._TotalSize.HasValue) return;

            this.CalculateSize(graphics);
        }
        /// <summary>
        /// Calculate all Size informations
        /// </summary>
        /// <param name="graphics"></param>
        protected void CalculateSize(Graphics graphics)
        {
            SizeF maxSize = this.ToolTipMaxSize;
            Size titleSize = _CalculateOneTextSize(graphics, this.TitleText, this.TitleFont, maxSize, 0.15f);
            Size textSize = _CalculateOneTextSize(graphics, this.InfoText, this.InfoFont, maxSize, 0.85f);
            Size iconSize = _CalculateIconSize(this.Icon, IconMaxSize);
            Size imageSize = _CalculateIconSize(this.Image, ImageMaxSize);
            int titleWidth = (titleSize.Height > 0 ? titleSize.Width + 20 : 0);
            int infoWidth = (textSize.Height > 0 ? textSize.Width : 0);
            int infoHeight = textSize.Height;
            if (imageSize.Width > 0 && imageSize.Height > 0)
            {   // Pokud máme Image, zvětší se prostor Info:
                infoWidth = GetMax(infoWidth, imageSize.Width);
                infoHeight = infoHeight + GetSpace(infoHeight, imageSize.Height, InfoImageSpace) + imageSize.Height;
            }
            if (iconSize.Width > 0 && iconSize.Height > 0)
            {
                infoWidth = iconSize.Width + IconInfoSpace + infoWidth;
                infoHeight = GetMax(infoHeight, iconSize.Height);
            }
            int width = ((titleWidth > infoWidth) ? titleWidth : infoWidth);
            int height = titleSize.Height + GetSpace(titleSize.Height, infoHeight, TitleInfoSpace) + infoHeight;

            this._ShadowSize = 4;
            this._TitleSize = titleSize;
            this._TextSize = textSize;
            this._IconSize = iconSize;
            this._ImageSize = imageSize;
            this._TotalSize = new Size(2 * InnerMargin.Width + width, 2 * InnerMargin.Height + height);
        }
        /// <summary>
        /// Returns Size (Ceiling) of specified text with font on current Graphics.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <returns></returns>
        protected Size _CalculateOneTextSize(Graphics graphics, string text, FontInfo font, SizeF maxSize, float maxHeight)
        {
            if (String.IsNullOrEmpty(text) || font == null) return Size.Empty;
            SizeF area = maxSize.Multiply(1f, maxHeight);
            SizeF size = graphics.MeasureString(text, font.Font, area);
            return Size.Ceiling(size);
        }
        /// <summary>
        /// Calculate size for icon, with maxSize
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        protected Size _CalculateIconSize(Image image, Size maxSize)
        {
            Size size = Size.Empty;
            if (image != null)
                size = image.Size.ShrinkTo(maxSize);
            return size;
        }
        /// <summary>Size of Shadow border</summary>
        protected Int32? _ShadowSize;
        /// <summary>Size of Title text</summary>
        protected Size? _TitleSize;
        /// <summary>Size of Info text</summary>
        protected Size? _TextSize;
        /// <summary>Size of Icon</summary>
        protected Size? _IconSize;
        /// <summary>Size of Image</summary>
        protected Size? _ImageSize;
        /// <summary>Sum of size _TitleSize + _InfoSize</summary>
        protected Size? _TotalSize;
        /// <summary>
        /// Vrátí větší z hodnot
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        protected static int GetMax(int value1, int value2)
        {
            return ((value1 > value2) ? value1 : value2);
        }
        /// <summary>
        /// Pokud size1 i size2 jsou větší než 0, vrátí space. Jinak vrátí 0.
        /// </summary>
        /// <param name="size1"></param>
        /// <param name="size2"></param>
        /// <param name="space"></param>
        /// <returns></returns>
        protected static int GetSpace(int size1, int size2, int space)
        {
            return (size1 > 0 && size2 > 0 ? space : 0);
        }
        #endregion
        #region Paths for Title, Text, Total - for relative bounds (Location = 0, 0)
        /// <summary>
        /// Invalidate paths and bounds
        /// </summary>
        protected void InvalidateBounds()
        {
            if (this._TitlePath != null) this._TitlePath.Dispose();
            this._TitlePath = null;
            this._TitleBounds = null;
            this._IconBounds = null;
            if (this._InfoPath != null) this._InfoPath.Dispose();
            this._InfoPath = null;
            this._InfoBounds = null;
            if (this._TotalPath != null) this._TotalPath.Dispose();
            this._ImageBounds = null;
            this._TotalPath = null;
            this._TotalBounds = null;

            this.InvalidateImage();
            this.InvalidateLocation();
        }
        /// <summary>
        /// Check and calculate paths and bounds.
        /// </summary>
        protected void CheckValidBounds(Graphics graphics)
        {
            if (this._TitleBounds.HasValue &&
                this._IconBounds.HasValue &&
                this._InfoBounds.HasValue &&
                this._ImageBounds.HasValue &&
                this._TotalBounds.HasValue)
                return;

            this.CalculateBounds(graphics);
        }
        /// <summary>
        /// Calculate valid paths and bounds.
        /// </summary>
        /// <param name="graphics"></param>
        protected void CalculateBounds(Graphics graphics)
        {
            switch (this.ShapeType)
            {
                case TooltipShapeType.Rectangle:
                    this.CalculateRectangle();
                    break;
                case TooltipShapeType.RoundRectangle:
                    this.CalculateRoundRectangle();
                    break;
                case TooltipShapeType.Ellipse:
                    this.CalculateEllipse();
                    break;
                case TooltipShapeType.Window:
                    this.CalculateWindow();
                    break;
            }
        }
        /// <summary>
        /// Calculate TotalBounds, TitleBounds and InfoBounds for current sizes, relative to point 0,0
        /// </summary>
        protected void _CalculateTotalBoundsRelative()
        {
            switch (this.ToolTipLayout)
            {
                case ToolTipLayoutType.IconBeforeTitle:
                    this._CalculateTotalBoundsRelativeIBT();
                    break;
                case ToolTipLayoutType.TitleBeforeIcon:
                default:
                    this._CalculateTotalBoundsRelativeTBI();
                    break;
            }
        }
        protected void _CalculateTotalBoundsRelativeIBT()
        {
            int shadowSize = (this._ShadowSize.HasValue ? this._ShadowSize.Value : 0);
            Size titleSize = (this.TitleExist && this._TitleSize.HasValue ? this._TitleSize.Value : Size.Empty);
            Size iconSize = (this.IconExist && this._IconSize.HasValue ? this._IconSize.Value : Size.Empty);
            Size infoSize = (this.InfoExist && this._TextSize.HasValue ? this._TextSize.Value : Size.Empty);
            Size imageSize = (this.ImageExist && this._ImageSize.HasValue ? this._ImageSize.Value : Size.Empty);

            Rectangle titleBounds = Rectangle.Empty;
            Rectangle iconBounds = Rectangle.Empty;
            Rectangle infoBounds = Rectangle.Empty;
            Rectangle imageBounds = Rectangle.Empty;
            Rectangle? lineBounds = null;

            int mx = InnerMargin.Width;
            int my = InnerMargin.Height;
            int l = shadowSize + mx;
            int t = shadowSize + my;

            // Icon:
            if (iconSize.Width > 0)
            {
                iconBounds = new Rectangle(l, t, iconSize.Width, iconSize.Height);
                l = iconBounds.Right + IconInfoSpace;
            }
            else
            {
                iconBounds = new Rectangle(l, t, 0, 0);
            }

            // Title:
            if (titleSize.Height > 0)
            {
                titleBounds = new Rectangle(l, t, titleSize.Width, titleSize.Height);
                t = titleBounds.Bottom + TitleInfoSpace;
            }
            else
            {
                titleBounds = new Rectangle(l, t, 0, 0);
            }

            // Info:
            if (infoSize.Width > 0)
            {
                infoBounds = new Rectangle(l, t, infoSize.Width, infoSize.Height);
                t = infoBounds.Bottom + InfoImageSpace;
            }
            else
            {
                infoBounds = new Rectangle(l, t, 0, 0);
            }

            // Image:
            if (imageSize.Width > 0)
            {
                imageBounds = new Rectangle(l, t, imageSize.Width, imageSize.Height);
                t = imageBounds.Bottom + InfoImageSpace;
            }
            else
            {
                imageBounds = new Rectangle(l, t, 0, 0);
            }

            // Line between Title and Info:
            if (titleSize.Height > 0)
                lineBounds = new Rectangle(titleBounds.X, titleBounds.Bottom, Math.Max(titleBounds.Width, infoBounds.Width), 0);

            // Total:
            Rectangle totalBounds = DrawingExtensions.SummaryVisibleRectangle(iconBounds, titleBounds, infoBounds, imageBounds);
            totalBounds = totalBounds.Enlarge(mx, my, mx, my);
            Rectangle outerBounds = totalBounds.Enlarge(shadowSize);

            this._TitleBounds = titleBounds;
            this._IconBounds = iconBounds;
            this._InfoBounds = infoBounds;
            this._ImageBounds = imageBounds;
            this._LineBounds = lineBounds;
            this._TotalBounds = totalBounds;
            this._OuterBounds = outerBounds;
        }
        protected void _CalculateTotalBoundsRelativeTBI()
        {
            int shadowSize = (this._ShadowSize.HasValue ? this._ShadowSize.Value : 0);
            Size titleSize = (this.TitleExist && this._TitleSize.HasValue ? this._TitleSize.Value : Size.Empty);
            Size iconSize = (this.IconExist && this._IconSize.HasValue ? this._IconSize.Value : Size.Empty);
            Size infoSize = (this.InfoExist && this._TextSize.HasValue ? this._TextSize.Value : Size.Empty);
            Size imageSize = (this.ImageExist && this._ImageSize.HasValue ? this._ImageSize.Value : Size.Empty);

            Rectangle titleBounds = Rectangle.Empty;
            Rectangle iconBounds = Rectangle.Empty;
            Rectangle infoBounds = Rectangle.Empty;
            Rectangle imageBounds = Rectangle.Empty;
            Rectangle? lineBounds = null;

            int mx = InnerMargin.Width;
            int my = InnerMargin.Height;
            int l = shadowSize + mx;
            int t = shadowSize + my;

            // Title:
            if (titleSize.Height > 0)
            {
                titleBounds = new Rectangle(l + 10, t, titleSize.Width, titleSize.Height);
                t = titleBounds.Bottom + TitleInfoSpace;
            }
            else
            {
                titleBounds = new Rectangle(l, t, 0, 0);
                t = titleBounds.Bottom;
            }

            // Icon:
            if (iconSize.Width > 0)
            {
                iconBounds = new Rectangle(l, t, iconSize.Width, iconSize.Height);
                l = iconBounds.Right + IconInfoSpace;
            }
            else
            {
                iconBounds = new Rectangle(l, t, 0, 0);
                l = iconBounds.Right;
            }

            // Info:
            if (infoSize.Width > 0)
            {
                infoBounds = new Rectangle(l, t, infoSize.Width, infoSize.Height);
                t = infoBounds.Bottom + InfoImageSpace;
            }
            else
            {
                infoBounds = new Rectangle(l, t, 0, 0);
            }

            // Image:
            if (imageSize.Width > 0)
            {
                imageBounds = new Rectangle(l, t, imageSize.Width, imageSize.Height);
                t = imageBounds.Bottom + InfoImageSpace;
            }
            else
            {
                imageBounds = new Rectangle(l, t, 0, 0);
            }

            // Line between Title and Info:
            if (titleSize.Height > 0)
                lineBounds = new Rectangle(titleBounds.X, titleBounds.Bottom, Math.Max(titleBounds.Width, infoBounds.Width), 0);

            // Total:
            Rectangle totalBounds = DrawingExtensions.SummaryVisibleRectangle(iconBounds, titleBounds, infoBounds, imageBounds);
            totalBounds = totalBounds.Enlarge(mx, my, mx, my);
            Rectangle outerBounds = totalBounds.Enlarge(shadowSize);

            this._TitleBounds = titleBounds;
            this._IconBounds = iconBounds;
            this._InfoBounds = infoBounds;
            this._ImageBounds = imageBounds;
            this._LineBounds = lineBounds;
            this._TotalBounds = totalBounds;
            this._OuterBounds = outerBounds;
        }
        /// <summary>Shape of title</summary>
        protected GraphicsPath _TitlePath;
        /// <summary>Bounds for Title text</summary>
        protected Rectangle? _TitleBounds;
        /// <summary>Bounds for Line between Title and Info text</summary>
        protected Rectangle? _LineBounds;
        /// <summary>Bounds for Icon</summary>
        protected Rectangle? _IconBounds;
        /// <summary>Shape of Info text</summary>
        protected GraphicsPath _InfoPath;
        /// <summary>Bounds for Info text</summary>
        protected Rectangle? _InfoBounds;
        /// <summary>Bounds for Image</summary>
        protected Rectangle? _ImageBounds;
        /// <summary>Shape of whole tooltip</summary>
        protected GraphicsPath _TotalPath;
        /// <summary>Bounds for inner area of tooltip (border, background) = exclude shadow</summary>
        protected Rectangle? _TotalBounds;
        /// <summary>Bounds for whole tooltip = include shadow</summary>
        protected Rectangle? _OuterBounds;
        #endregion
        #region Image created for current tooltip
        protected void InvalidateImage()
        {
            if (this._Image != null) this._Image.Dispose();
            this._Image = null;
        }
        protected void CheckValidImage(Graphics graphics)
        {
            if (this._Image != null) return;

            this.CalculateImage(graphics);
        }
        protected void CalculateImage(Graphics graphics)
        {
            Size size = this._OuterBounds.Value.Size;
            
            this._Image = new Bitmap(size.Width, size.Height, graphics);
            using (Graphics imgGraphics = Graphics.FromImage(this._Image))
            {
                this.DrawShadow(imgGraphics, size);
                switch (this.ShapeType)
                {
                    case TooltipShapeType.Rectangle:
                        this.DrawRectangle(imgGraphics);
                        break;
                    case TooltipShapeType.RoundRectangle:
                        this.DrawRoundRectangle(imgGraphics);
                        break;
                    case TooltipShapeType.Ellipse:
                        this.DrawEllipse(imgGraphics);
                        break;
                    case TooltipShapeType.Window:
                        this.DrawWindow(imgGraphics);
                        break;
                }
            }
        }

        private void DrawShadow(Graphics imgGraphics, Size size)
        {
            int shadowSize = (this._ShadowSize.HasValue ? this._ShadowSize.Value : 0);
            if (shadowSize <= 0) return;

            Rectangle bounds = this._TotalBounds.Value;
            GPainter.DrawShadow(imgGraphics, bounds, shadowSize, false);
        }
        protected Image _Image; // Bitmap
        #endregion
        #region Absolute location for ToolTip
        protected void InvalidateLocation()
        {
            this._TargetBounds = null;
        }
        protected void CheckValidLocation(Graphics graphics)
        {
            if (this._TargetBounds.HasValue) return;

            this.CalculateLocation(graphics);
        }
        protected void CalculateLocation(Graphics graphics)
        {
            if (!this._Point.HasValue || !this._TotalBounds.HasValue) return;

            Point mousePoint = this._Point.Value;
            Size totalSize = this._OuterBounds.Value.Size;
            Size hostSize = this._Owner.ClientSize;

            // Standard offset for TargetBounds.Location from MousePoint is: X-18, Y+14:
            Point point = mousePoint.Add(-12, 18);
            Rectangle bounds = new Rectangle(point, totalSize);

            // If bounds is outside host on X axis, then shift it:
            if (bounds.Right > (hostSize.Width - 2))
                bounds.X = hostSize.Width - bounds.Width - 2;
            if (bounds.X < 2)
                bounds.X = 2;

            // If bounds is outside host on Y axis, then draw it above point:
            if (bounds.Bottom > (hostSize.Height - 2))
                bounds.Y = mousePoint.Y - 4 - bounds.Height;
            if (bounds.Y < 2)
                bounds.Y = 2;

            this._TargetBounds = bounds;
        }
        /// <summary>Absolute point to draw ToolTip origin</summary>
        protected Rectangle? _TargetBounds;
        #endregion
        #region Color matrix for draw image with alpha channel (opacity) controlled by animations
        private void ColorMatrixInit()
        {
            // Identity matrix :
            float[][] colorMatrixElements = 
            { 
               new float[] {1, 0, 0, 0, 0},      // Red input distribution to RGBA channels...
               new float[] {0, 1, 0, 0, 0},      // Green input distribution to RGBA channels...
               new float[] {0, 0, 1, 0, 0},      // Blue input distribution to RGBA channels...
               new float[] {0, 0, 0, 1, 0},      // Alpha input distribution to RGBA channels...
               new float[] {0, 0, 0, 0, 1}       // Add to RGBA channels...
            };

            this._ColorMatrix = new System.Drawing.Imaging.ColorMatrix(colorMatrixElements);
        }
        /// <summary>
        /// Returns color matrix (for modifications of colors on image) in DrawImage process.
        /// </summary>
        /// <param name="alpha"></param>
        /// <returns></returns>
        private System.Drawing.Imaging.ColorMatrix ColorMatrixForAlpha(float alpha)
        {
            return this.ColorMatrixForAlpha(alpha, false);
        }
        /// <summary>
        /// Returns color matrix (for modifications of colors on image) in DrawImage process.
        /// </summary>
        /// <param name="alpha"></param>
        /// <returns></returns>
        private System.Drawing.Imaging.ColorMatrix ColorMatrixForAlpha(float alpha, bool asGray)
        {
            System.Drawing.Imaging.ColorMatrix colorMatrix = this._ColorMatrix;
            /*
            asGray = false;
            if (asGray && alpha < 1f)
            {
                float gray = 1f - alpha;
                colorMatrix.Matrix00 = alpha;
                colorMatrix.Matrix01 = gray;
                colorMatrix.Matrix02 = gray;

                colorMatrix.Matrix10 = gray;
                colorMatrix.Matrix11 = alpha;
                colorMatrix.Matrix12 = gray;

                colorMatrix.Matrix20 = gray;
                colorMatrix.Matrix21 = gray;
                colorMatrix.Matrix22 = alpha;
            }
            else
            {
                colorMatrix.Matrix00 = 1f;
                colorMatrix.Matrix01 = 0f;
                colorMatrix.Matrix02 = 0f;

                colorMatrix.Matrix10 = 0f;
                colorMatrix.Matrix11 = 1f;
                colorMatrix.Matrix12 = 0f;

                colorMatrix.Matrix20 = 0f;
                colorMatrix.Matrix21 = 0f;
                colorMatrix.Matrix22 = 1f;
            }
            */
            colorMatrix.Matrix33 = alpha;        // Alpha channel: when input Alpha is 1 (full opacity, no transparent), then output Alpha will be (alpha) = input Alpha * (alpha)
            return colorMatrix;
        }
        System.Drawing.Imaging.ColorMatrix _ColorMatrix;

        #endregion
        /// <summary>
        /// Returns a Minimal Size for ToolTip, as constant (Width = 50px, Height = 17px)
        /// </summary>
        protected Size ToolTipMinSize { get { return new Size(50, 17); } }
        /// <summary>
        /// Returns a Maximal Size for ToolTip, by _Owner.ClientRectangle.Size, as 80% x 50% of original Size.
        /// </summary>
        protected Size ToolTipMaxSize
        {
            get
            {
                Rectangle space = this._Owner.ClientRectangle;
                // MaxSize is 80% of Width x 50% of Height:
                SizeF size = new SizeF(space.Width, space.Height);
                return Size.Ceiling(size.Multiply(0.8f, 0.5f));
            }
        }
        /// <summary>
        /// Maximal size for Icon
        /// </summary>
        protected static Size IconMaxSize { get { return new Size(64, 64); } }
        /// <summary>
        /// Maximal size for Image
        /// </summary>
        protected static Size ImageMaxSize { get { return new Size(512, 512); } }
        /// <summary>
        /// Space between Icon and Info text area
        /// </summary>
        protected static int IconInfoSpace { get { return 5; } }
        /// <summary>
        /// Space between Title and Info text area
        /// </summary>
        protected static int TitleInfoSpace { get { return 2; } }
        /// <summary>
        /// Space between Info and Image text area
        /// </summary>
        protected static int InfoImageSpace { get { return 6; } }
        protected static Point TextShift { get { return new Point(3, 1); } }
        /// <summary>
        /// Margin between ToolTip and its Inner parts (Icon, texts)
        /// </summary>
        protected static Size InnerMargin { get { return new Size(4, 3); } }
        /// <summary>
        /// Margin between ToolTip and ClientArea of Owner (control)
        /// </summary>
        protected static Size OuterMargin { get { return new Size(6, 6); } }
        protected ToolTipPosition _Position;
        protected enum ToolTipPosition { None, Above, Under }
        #region Cache for calculated and prepared data
        /// <summary>
        /// Reset all private data non-dependent on position of tooltip (text sizes, fonts, pen brushes),
        /// Does not reset other data (background brushes and pathes).
        /// </summary>
        protected void InvalidateData()
        {
            this._ShadowSize = null;
            this._TitleSize = null;
            this._IconSize = null;
            this._TextSize = null;

            if (this._Data != null) ((IDisposable)this._Data).Dispose();
            this._Data = null;
        }
        #endregion
        #endregion
    }
    #endregion
    #region class ToolTipData : pouze data tooltip
    /// <summary>
    /// ToolTipData : Data for Tooltip object
    /// </summary>
    public class ToolTipData : IDisposable
    {
        public ToolTipData()
        {
            this.AnimationType = TooltipAnimationType.DefaultFade;
            this.ShapeType = TooltipShapeType.Rectangle;
            this.Icon = IconStandard.IconInfo;
            this.ToolTipLayout = ToolTipLayoutType.IconBeforeTitle;
        }
        /// <summary>
        /// Is tooltip Visible? setting false = hide tooltip.
        /// </summary>
        public bool IsVisible { get { return !this._Hidden; } set { this._Hidden = !value; } } private bool _Hidden;
        /// <summary>
        /// Shape type of ToolTip
        /// </summary>
        public TooltipShapeType? ShapeType { get { return this._ShapeType; } set { this._ShapeType = value; } } private TooltipShapeType? _ShapeType;
        /// <summary>
        /// Animation type for ToolTip (fade-in, wait, fade-out).
        /// Default value = DefaultFade (Wait: 250; FadeIn: 250; Show: 6000; FadeOut: 250 miliseconds)
        /// </summary>
        public TooltipAnimationType AnimationType { get { return this._AnimationType; } set { this._AnimationType = value; this._AnimationPrepare(true); } } private TooltipAnimationType _AnimationType;
        /// <summary>
        /// Color for Border line
        /// </summary>
        public Color? BorderColor { get { return this._BorderColor; } set { this._BorderColor = value; } } private Color? _BorderColor;
        /// <summary>
        /// Title to ToolTip
        /// </summary>
        public Asol.Tools.WorkScheduler.Localizable.TextLoc TitleText { get { return this._TitleText; } set { this._TitleText = value; } } private string _TitleText;
        /// <summary>
        /// Back color for Title text
        /// </summary>
        public Color? TitleBackColor { get { return this._TitleBackColor; } set { this._TitleBackColor = value; } } private Color? _TitleBackColor;
        /// <summary>
        /// Font color for Title text
        /// </summary>
        public Color? TitleFontColor { get { return this._TitleFontColor; } set { this._TitleFontColor = value; } } private Color? _TitleFontColor;
        /// <summary>
        /// Font for Title text
        /// </summary>
        public FontInfo TitleFont { get { return this._TitleFont; } set { this._TitleFont = value; } } private FontInfo _TitleFont;
        /// <summary>
        /// Info Text to ToolTip
        /// </summary>
        public string InfoText { get { return this._InfoText; } set { this._InfoText = value; } } private string _InfoText;
        /// <summary>
        /// Icon before Info Text to ToolTip
        /// </summary>
        public Image Icon { get { return this._Icon; } set { this._Icon = value; } } private Image _Icon;
        /// <summary>
        /// Image namísto hlavního textu
        /// </summary>
        public Image Image { get { return this._Image; } set { this._Image = value; } } private Image _Image;
        /// <summary>
        /// Layout of Icon - Title - Info in Tooltip
        /// </summary>
        public ToolTipLayoutType ToolTipLayout { get { return this._ToolTipLayout; } set { this._ToolTipLayout = value; } } private ToolTipLayoutType _ToolTipLayout;
        /// <summary>
        /// Back color for Info text
        /// </summary>
        public Color? InfoBackColor { get { return this._InfoBackColor; } set { this._InfoBackColor = value; } } private Color? _InfoBackColor;
        /// <summary>
        /// Opacity for ToolTip layer (background).
        /// 0 = transparent, 255 = opaque (no transparent).
        /// Default opacity by system = 216 (is used, when Opacity = null).
        /// </summary>
        public Int32? Opacity { get { return this._Opacity; } set { this._Opacity = value; } } private Int32? _Opacity;
        /// <summary>
        /// Font color for Info text
        /// </summary>
        public Color? InfoFontColor { get { return this._InfoFontColor; } set { this._InfoFontColor = value; } } private Color? _InfoFontColor;
        /// <summary>
        /// Font for Info text
        /// </summary>
        public FontInfo InfoFont { get { return this._InfoFont; } set { this._InfoFont = value; } } private FontInfo _InfoFont;
        /// <summary>
        /// true when this contain valid data for draw tooltip
        /// </summary>
        public bool IsValid { get { return (!String.IsNullOrEmpty(this._InfoText) || this.Image != null); } }
        /// <summary>
        /// Clone of all data
        /// </summary>
        internal ToolTipData Clone { get { return (ToolTipData)this.MemberwiseClone(); } }
        #region Animations
        /// <summary>
        /// true when exists animation
        /// </summary>
        public bool AnimationExists { get { this._AnimationPrepare(false); return this._AnimationExists.Value; } set { this._AnimationExists = value; } } private bool? _AnimationExists;
        /// <summary>
        /// Animation: time before Fade-In (wait before show begins)
        /// </summary>
        public TimeSpan AnimationWaitBeforeTime { get { this._AnimationPrepare(false); return this._AnimationWaitBeforeTime.Value; } set { this._AnimationWaitBeforeTime = value; } } private TimeSpan? _AnimationWaitBeforeTime;
        /// <summary>
        /// Animation: time before repeated Fade-In (wait before show repeated begins)
        /// </summary>
        public TimeSpan AnimationWaitRepeatedBeforeTime { get { this._AnimationPrepare(false); return this._AnimationWaitRepeatedBeforeTime.Value; } set { this._AnimationWaitRepeatedBeforeTime = value; } } private TimeSpan? _AnimationWaitRepeatedBeforeTime;
        /// <summary>
        /// Animation: time of Fade-In
        /// </summary>
        public TimeSpan AnimationFadeInTime { get { this._AnimationPrepare(false); return this._AnimationFadeInTime.Value; } set { this._AnimationFadeInTime = value; } }  private TimeSpan? _AnimationFadeInTime;
        /// <summary>
        /// Animation: time of standard visibility
        /// </summary>
        public TimeSpan AnimationShowTime { get { this._AnimationPrepare(false); return this._AnimationShowTime.Value; } set { this._AnimationShowTime = value; } }  private TimeSpan? _AnimationShowTime;
        /// <summary>
        /// Animation: time of Fade-Out
        /// </summary>
        public TimeSpan AnimationFadeOutTime { get { this._AnimationPrepare(false); return this._AnimationFadeOutTime.Value; } set { this._AnimationFadeOutTime = value; } }  private TimeSpan? _AnimationFadeOutTime;
        /// <summary>
        /// Prepare values into AnimationExists, AnimationWaitBeforeTime, AnimationFadeInTime, AnimationShowTime, AnimationFadeOutTime
        /// </summary>
        /// <param name="force"></param>
        private void _AnimationPrepare(bool force)
        {
            TooltipAnimationType animationType = this._AnimationType;
            switch (animationType)
            {
                case TooltipAnimationType.DefaultFade:
                    if (force || !_AnimationExists.HasValue) _AnimationExists = true;
                    if (force || !_AnimationWaitBeforeTime.HasValue) _AnimationWaitBeforeTime = DefaultAnimationWaitBeforeTime;
                    if (force || !_AnimationWaitRepeatedBeforeTime.HasValue) _AnimationWaitRepeatedBeforeTime = DefaultAnimationWaitRepeatedBeforeTime;
                    if (force || !_AnimationFadeInTime.HasValue) _AnimationFadeInTime = DefaultAnimationFadeInTime;
                    if (force || !_AnimationShowTime.HasValue) _AnimationShowTime = DefaultAnimationShowTime;
                    if (force || !_AnimationFadeOutTime.HasValue) _AnimationFadeOutTime = DefaultAnimationFadeOutTime;
                    break;
                case TooltipAnimationType.SlowFade:
                    if (force || !_AnimationExists.HasValue) _AnimationExists = true;
                    if (force || !_AnimationWaitBeforeTime.HasValue) _AnimationWaitBeforeTime = TimeSpan.FromMilliseconds(350d);
                    if (force || !_AnimationWaitRepeatedBeforeTime.HasValue) _AnimationWaitRepeatedBeforeTime = TimeSpan.FromMilliseconds(10000d);
                    if (force || !_AnimationFadeInTime.HasValue) _AnimationFadeInTime = TimeSpan.FromMilliseconds(450d);
                    if (force || !_AnimationShowTime.HasValue) _AnimationShowTime = TimeSpan.FromMilliseconds(15000d);
                    if (force || !_AnimationFadeOutTime.HasValue) _AnimationFadeOutTime = TimeSpan.FromMilliseconds(750d);
                    break;
                case TooltipAnimationType.FastFadeInSlowFadeOut:
                    if (force || !_AnimationExists.HasValue) _AnimationExists = true;
                    if (force || !_AnimationWaitBeforeTime.HasValue) _AnimationWaitBeforeTime = TimeSpan.FromMilliseconds(100d);
                    if (force || !_AnimationWaitRepeatedBeforeTime.HasValue) _AnimationWaitRepeatedBeforeTime = TimeSpan.FromMilliseconds(10000d);
                    if (force || !_AnimationFadeInTime.HasValue) _AnimationFadeInTime = TimeSpan.FromMilliseconds(100d);
                    if (force || !_AnimationShowTime.HasValue) _AnimationShowTime = TimeSpan.FromMilliseconds(15000d);
                    if (force || !_AnimationFadeOutTime.HasValue) _AnimationFadeOutTime = TimeSpan.FromMilliseconds(850d);
                    break;
                case TooltipAnimationType.Instant:
                    if (force || !_AnimationExists.HasValue) _AnimationExists = false;
                    if (force || !_AnimationWaitBeforeTime.HasValue) _AnimationWaitBeforeTime = TimeSpan.FromMilliseconds(0d);
                    if (force || !_AnimationWaitRepeatedBeforeTime.HasValue) _AnimationWaitRepeatedBeforeTime = TimeSpan.FromMilliseconds(10000d);
                    if (force || !_AnimationFadeInTime.HasValue) _AnimationFadeInTime = TimeSpan.FromMilliseconds(0d);
                    if (force || !_AnimationShowTime.HasValue) _AnimationShowTime = TimeSpan.FromMilliseconds(300000d);
                    if (force || !_AnimationFadeOutTime.HasValue) _AnimationFadeOutTime = TimeSpan.FromMilliseconds(450d);
                    break;
            }
        }
        internal static TimeSpan DefaultAnimationWaitBeforeTime { get { return TimeSpan.FromMilliseconds(300d); } }
        internal static TimeSpan DefaultAnimationWaitRepeatedBeforeTime { get { return TimeSpan.FromMilliseconds(5000); } }
        internal static TimeSpan DefaultAnimationFadeInTime { get { return TimeSpan.FromMilliseconds(250d); } }
        internal static TimeSpan DefaultAnimationShowTime { get { return TimeSpan.FromMilliseconds(6000d); } }
        internal static TimeSpan DefaultAnimationFadeOutTime { get { return TimeSpan.FromMilliseconds(500d); } }
        #endregion
        void IDisposable.Dispose()
        {
        }
    }
    #endregion
    #region Enumy : TooltipAnimationType, TooltipShapeType, ToolTipLayoutType
    /// <summary>
    /// Animation of ToolTip
    /// </summary>
    public enum TooltipAnimationType
    {
        /// <summary>
        /// Default (Wait: 250; FadeIn: 250; Show: 6000; FadeOut: 250 miliseconds)
        /// </summary>
        DefaultFade,
        /// <summary>
        /// Slow (Wait: 350; FadeIn: 450; Show: 15000; FadeOut: 750 miliseconds)
        /// </summary>
        SlowFade,
        /// <summary>
        /// FastFadeInSlowFadeOut (Wait: 100; FadeIn: 100; Show: 15000; FadeOut: 850 miliseconds)
        /// </summary>
        FastFadeInSlowFadeOut,
        /// <summary>
        /// Instant = no animation (immediatelly and indefinitely Show, no Wait, no FadeIn, no FadeOut)
        /// </summary>
        Instant
    }
    public enum TooltipShapeType
    {
        None,
        Rectangle,
        RoundRectangle,
        Ellipse,
        Window
    }
    /// <summary>
    /// Layout of Icon - Title - Info in Tooltip
    /// </summary>
    public enum ToolTipLayoutType
    {
        /// <summary>
        /// Icon on the left, Title on right from icon, Info below Title.
        /// Icon has separate column, Title and Info is in one column.
        /// </summary>
        IconBeforeTitle,
        /// <summary>
        /// Title on the top, Icon and Info is in one row below Title.
        /// </summary>
        TitleBeforeIcon
    }
    #endregion
}
