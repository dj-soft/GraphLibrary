using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using DjSoft.Games.Sudoku.Data;

namespace DjSoft.Games.Sudoku.Components
{
    public class AnimatedControl : Control
    {
        #region Konstruktor, Animátor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public AnimatedControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            __IsPainted = false;
            __Animator = new Animator(this);
            __StopwatchExt = new Data.StopwatchExt(true);
            __LayeredGraphics = new LayeredGraphicStandard(this);
            __IsActiveDiagnostic = AppService.IsDiagnosticActive;
        }
        protected override void Dispose(bool disposing)
        {
            __Animator.AnimatorTimerStop = true;
            __Animator = null;
            base.Dispose(disposing);
        }
        /// <summary>
        /// Animátor
        /// </summary>
        public Animator Animator { get { return __Animator; } } private Animator __Animator;
        /// <summary>
        /// Časomíra pro měření práce controlu.
        /// Není dobrý nápad resetovat ji.
        /// Je možné odečítat aktuální časy, nebo si poznamenat počáteční čas akce a na konci akce si určit uplynulý čas 
        /// metodou <see cref="Data.StopwatchExt.GetMilisecs(long)"/>
        /// </summary>
        public Data.StopwatchExt StopwatchExt { get { return __StopwatchExt; } } private Data.StopwatchExt __StopwatchExt;
        #endregion
        #region Podpora pro Paint obecná
        /// <summary>
        /// Detekuje první reálné vykreslení controlu.
        /// Detekuje a nastavuje <see cref="__IsPainted"/>, startuje <see cref="Animator"/>, vyvolá <see cref="OnFirstPaint()"/>.
        /// </summary>
        private void _CheckFirstPaint()
        {
            if (!__IsPainted && CanReallyDraw)
            {
                __IsPainted = true;
                __Animator.Running = true;
                OnFirstPaint();
            }
        }
        /// <summary>
        /// Vyvolá se před prvním reálným kreslením
        /// </summary>
        protected virtual void OnFirstPaint() { }
        /// <summary>
        /// Obsahuje false po konstruktoru, změní se na true při prvním reálném kreslení
        /// </summary>
        private bool __IsPainted;
        /// <summary>
        /// Obsahuje true, pokud může být reálně kresleno = máme kladné rozměry, a máme Parent form, a ten není Minimized.
        /// </summary>
        protected bool CanReallyDraw
        {
            get
            {
                if (this.Parent is null) return false;
                var size = this.ClientSize;
                if (size.Width <= 0 || size.Height <= 0) return false;
                var parentForm = this.FindForm();
                if (parentForm is null || parentForm.WindowState == FormWindowState.Minimized) return false;
                return true;
            }
        }
        #endregion
        #region Layered graphics
        /// <summary>
        /// Správce vrstev bufferované grafiky
        /// </summary>
        protected LayeredGraphicStandard LayeredGraphics { get { return __LayeredGraphics; } } private LayeredGraphicStandard __LayeredGraphics;
        /// <summary>
        /// Systémová metoda kreslení pozadí.
        /// Potomstvo nemá overridovat tuto metodu, ale <see cref="DoPaintBackground(PaintEventArgs)"/>.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnPaintBackground(PaintEventArgs args)
        {
            _CheckFirstPaint();
            bool isActiveDiagnostic = IsActiveDiagnostic;
            if (isActiveDiagnostic) { __PaintBackgroundStart = StopwatchExt.ElapsedTicks; }

            if (!this.LayeredGraphics.UseLayeredGraphic)
                this.RunPaintBackgroundNative(args);
            else
                this.RunPaintBackgroundLayered(args);

            if (isActiveDiagnostic) { __PaintBackgroundEnd = StopwatchExt.ElapsedTicks; }
        }
        /// <summary>
        /// Systémová metoda kreslení obsahu.
        /// Potomstvo nemá overridovat tuto metodu, ale <see cref="DoPaintStandard(PaintEventArgs)"/>.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnPaint(PaintEventArgs args)
        {
            _CheckFirstPaint();
            bool isActiveDiagnostic = IsActiveDiagnostic;
            if (isActiveDiagnostic) { __PaintStandardStart = StopwatchExt.ElapsedTicks; }

            if (!this.LayeredGraphics.UseLayeredGraphic)
                this.RunPaintStandardNative(args);
            else
                this.RunPaintStandardLayered(args);
        
            if (isActiveDiagnostic) { __PaintStandardEnd = StopwatchExt.ElapsedTicks; _ShowPaintTime(args); }
        }
        /// <summary>
        /// Zde bude vykresleno pozadí controlu.
        /// Bázová metoda vyplní dodanou grafiku barvou pozadí.
        /// Potomek může přepsat a vykreslit i další 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void DoPaintBackground(LayeredPaintEventArgs args)
        {
            args.Graphics.Clear(this.BackColor);
        }
        protected virtual void DoPaintStandard(LayeredPaintEventArgs args) { }
        protected virtual void DoPaintOverlay(LayeredPaintEventArgs args) { }
        protected virtual void DoPaintToolTip(LayeredPaintEventArgs args) { }

        #region Kreslení nativní
        private void RunPaintBackgroundNative(PaintEventArgs args)
        {
            __PaintBackgroundCount++;
            using (LayeredPaintEventArgs largs = new LayeredPaintEventArgs(args))
                DoPaintBackground(largs);
        }
        private void RunPaintStandardNative(PaintEventArgs args)
        {
            __PaintStandardCount++;
            using (LayeredPaintEventArgs largs = new LayeredPaintEventArgs(args))
                DoPaintStandard(largs);
        }
        #endregion
        #region Kreslení s využitím vrstev
        private void RunPaintBackgroundLayered(PaintEventArgs args)
        {
            if (this.LayeredGraphics.UseBackgroundLayer)
            {
                var layer = this.LayeredGraphics.BackgroundLayer;
                if (!layer.ContainData || !LayerBackgroundValid)
                {
                    __PaintBackgroundCount++;
                    using (LayeredPaintEventArgs largs = new LayeredPaintEventArgs(layer.Graphics, args.ClipRectangle))
                    {
                        DoPaintBackground(largs);
                        layer.ContainData = largs.IsGraphicsUsed;
                    }
                }
                LayerBackgroundValid = true;
            }
        }
        private void RunPaintStandardLayered(PaintEventArgs args)
        {
            if (this.LayeredGraphics.UseStandardLayer)
            {
                var layer = this.LayeredGraphics.StandardLayer;
                if (!layer.ContainData || !LayerStandardValid)
                {
                    __PaintStandardCount++;
                    layer.PrepareFromSubLayer();
                    using (LayeredPaintEventArgs largs = new LayeredPaintEventArgs(layer.Graphics, args.ClipRectangle))
                    {
                        DoPaintStandard(largs);
                        layer.ContainData = largs.IsGraphicsUsed;
                    }
                }
                LayerStandardValid = true;
            }

            if (this.LayeredGraphics.UseOverlayLayer)
            {
                var layer = this.LayeredGraphics.OverlayLayer;
                if (!layer.ContainData || !LayerOverlayValid)
                {
                    layer.PrepareFromSubLayer();
                    using (LayeredPaintEventArgs largs = new LayeredPaintEventArgs(layer.Graphics, args.ClipRectangle))
                    {
                        DoPaintOverlay(largs);
                        layer.ContainData = largs.IsGraphicsUsed;
                    }
                }
                LayerOverlayValid = true;
            }

            if (this.LayeredGraphics.UseToolTipLayer)
            {
                var layer = this.LayeredGraphics.ToolTipLayer;
                if (!layer.ContainData || !LayerToolTipValid)
                {
                    layer.PrepareFromSubLayer();
                    using (LayeredPaintEventArgs largs = new LayeredPaintEventArgs(layer.Graphics, args.ClipRectangle))
                    {
                        DoPaintToolTip(largs);
                        layer.ContainData = largs.IsGraphicsUsed;
                    }
                }
                LayerToolTipValid = true;
            }

            var topLayer = this.LayeredGraphics.CurrentLayer;
            if (topLayer != null && topLayer.ContainData)
                topLayer.RenderTo(args.Graphics);
        }
        /// <summary>
        /// Platnost dat vykreslených do vrstvy Background. Běžně se o hodnotu stará třída <see cref="AnimatedControl"/>.
        /// Pokud aplikační kód chce znovu vykreslit motiv na vrstvě Background, pak stačí nastavit sem false. 
        /// Následující kreslení (po Invalidaci) zajistí vyvolání metody <see cref="AnimatedControl.DoPaintBackground(PaintEventArgs)"/> s vhodnou vrstvou.
        /// </summary>
        public virtual bool LayerBackgroundValid { get; set; }
        /// <summary>
        /// Platnost dat vykreslených do vrstvy Standard.
        /// Pokud aplikační kód chce znovu vykreslit motiv na vrstvě Standard, pak stačí nastavit sem false. 
        /// Následující kreslení (po Invalidaci) zajistí vyvolání metody <see cref="AnimatedControl.DoPaintStandard(PaintEventArgs)"/> s vhodnou vrstvou.
        /// </summary>
        public virtual bool LayerStandardValid { get; set; }
        /// <summary>
        /// Platnost dat vykreslených do vrstvy Overlay.
        /// Pokud aplikační kód chce znovu vykreslit motiv na vrstvě Overlay, pak stačí nastavit sem false. 
        /// Následující kreslení (po Invalidaci) zajistí vyvolání metody <see cref="AnimatedControl.DoPaintOverlay(PaintEventArgs)"/> s vhodnou vrstvou.
        /// </summary>
        public virtual bool LayerOverlayValid { get; set; }
        /// <summary>
        /// Platnost dat vykreslených do vrstvy ToolTip.
        /// Pokud aplikační kód chce znovu vykreslit motiv na vrstvě ToolTip, pak stačí nastavit sem false. 
        /// Následující kreslení (po Invalidaci) zajistí vyvolání metody <see cref="AnimatedControl.DoPaintToolTip(PaintEventArgs)"/> s vhodnou vrstvou.
        /// </summary>
        public virtual bool LayerToolTipValid { get; set; }
        /// <summary>
        /// Gets or sets the background color for the control.
        /// </summary>
        public override Color BackColor 
        {
            get { return base.BackColor; } 
            set 
            {
                bool isEqualColor = ValueSupport.IsEqualColors(base.BackColor, value);
                base.BackColor = value; 
                if (!isEqualColor) LayerBackgroundValid = false; 
            }
        }
        #endregion
        #region Diagnostika - měření časů pokud je připojen debugger
        /// <summary>
        /// Vypíše časy kreslení
        /// </summary>
        /// <param name="args"></param>
        private void _ShowPaintTime(PaintEventArgs args)
        {
            var bgrTime = StopwatchExt.GetMilisecsRound(__PaintBackgroundStart, __PaintBackgroundEnd, 3);
            var stdTime = StopwatchExt.GetMilisecsRound(__PaintStandardStart, __PaintStandardEnd, 3);
            string info = $"BgrCount: {__PaintBackgroundCount}; BgrTime: {bgrTime:F3} milisec; StdCount: {__PaintStandardCount}; StdTime: {stdTime:F3} milisec";
            var textSize = args.Graphics.MeasureString(info, SystemFonts.StatusFont);
            var ctrlSize = this.ClientSize;
            var textPoint = new PointF(12f, ctrlSize.Height - textSize.Height - 2f);
            args.Graphics.DrawString(info, SystemFonts.StatusFont, Brushes.Red, textPoint);
        }
        /// <summary>
        /// Je aktivní diagnostika?
        /// </summary>
        public bool IsActiveDiagnostic { get { return __IsActiveDiagnostic; } set { __IsActiveDiagnostic = value; } }
        long __PaintBackgroundCount;
        long __PaintBackgroundStart;
        long __PaintBackgroundEnd;
        long __PaintStandardCount;
        long __PaintStandardStart;
        long __PaintStandardEnd;
        bool __IsActiveDiagnostic;
        #endregion
        #endregion
    }
}
