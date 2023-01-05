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

            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;

            /*    OptimizedDoubleBuffer     DoubleBuffered     TestPaint milisec
             *           false                  false               4,50
             *           true                   false               5,20
             *           false                  true                5,40
             *           true                   true                    
            */

            __IsPainted = false;
            __Animator = new Animator(this);
            __StopwatchExt = new Data.StopwatchExt(true);
            __LayeredGraphics = new LayeredGraphicStandard(this);
            __IsActiveDiagnostic = false; // AppService.IsDiagnosticActive;
            __DiagnosticData = new CycleBuffer<DiagnosticItem>(2);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
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
        #region Podpora pro potomky - řízení použitosti a aktivity a platnosti jednotlivých vrstev
        /// <summary>
        /// Obsahuje true, pokud se používá vrstva Background. Výchozí je false.
        /// Je vhodné používat vrstvu pro Background, kde bude podkladová barva, protože se urychlí zahájení kreslení.
        /// </summary>
        protected bool UseBackgroundLayer { get { return this.LayeredGraphics.UseBackgroundLayer; } set { this.LayeredGraphics.UseBackgroundLayer = value; } }
        /// <summary>
        /// Obsahuje true, pokud se používá vrstva Standard. Výchozí je false.
        /// Tato vrstva typicky obsahuje základní motiv.
        /// </summary>
        protected bool UseStandardLayer { get { return this.LayeredGraphics.UseStandardLayer; } set { this.LayeredGraphics.UseStandardLayer = value; } }
        /// <summary>
        /// Obsahuje true, pokud se v rámci controlu někdy používá vrstva Overlay. Výchozí je false.
        /// Tato vrstva typicky obsahuje překryv základního motiv = něco, co se pohybuje nad základním obrazcem.
        /// Vrstva Overlay může být deaktivována hodnotou <see cref="LayerOverlayActive"/>.
        /// </summary>
        protected bool UseOverlayLayer { get { return this.LayeredGraphics.UseOverlayLayer; } set { this.LayeredGraphics.UseOverlayLayer = value; } }
         /// <summary>
        /// Obsahuje true, pokud se v rámci controlu někdy používá vrstva ToolTip. Výchozí je false.
        /// Tato vrstva typicky obsahuje "okno" nad celým motivem (ToolTip), zobrazený ještě nad Overlayem.
        /// Vrstva Tooltip může být deaktivována hodnotou <see cref="LayerToolTipActive"/>.
        /// </summary>
        protected bool UseToolTipLayer { get { return this.LayeredGraphics.UseToolTipLayer; } set { this.LayeredGraphics.UseToolTipLayer = value; } }
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
        /// Aktuální stav vrstvy Overlay - má být aktuálně nějak řešena?
        /// Pokud control nemá nic, co by v této vrstvě vykresloval, pak sem nastaví false a ušetří se čas.
        /// Pokud control bude nějaký Overlay kreslit, dá sem true a pak řídí hodnotu <see cref="LayerOverlayValid"/>: nastaví tam false po změnách dat, které je potřeba zobrazit.
        /// </summary>
        public virtual bool LayerOverlayActive { get; set; }
        /// <summary>
        /// Platnost dat vykreslených do vrstvy Overlay.
        /// Pokud aplikační kód chce znovu vykreslit motiv na vrstvě Overlay, pak stačí nastavit sem false. 
        /// Následující kreslení (po Invalidaci) zajistí vyvolání metody <see cref="AnimatedControl.DoPaintOverlay(PaintEventArgs)"/> s vhodnou vrstvou.
        /// </summary>
        public virtual bool LayerOverlayValid { get; set; }
        /// <summary>
        /// Aktuální stav vrstvy ToolTip - má být aktuálně nějak řešena?
        /// Pokud control nemá nic, co by v této vrstvě vykresloval, pak sem nastaví false a ušetří se čas.
        /// Pokud control bude nějaký ToolTip kreslit, dá sem true a pak řídí hodnotu <see cref="LayerToolTipValid"/>: nastaví tam false po změnách dat, které je potřeba zobrazit.
        /// </summary>
        public virtual bool LayerToolTipActive { get; set; }
        /// <summary>
        /// Platnost dat vykreslených do vrstvy ToolTip.
        /// Pokud aplikační kód chce znovu vykreslit motiv na vrstvě ToolTip, pak stačí nastavit sem false. 
        /// Následující kreslení (po Invalidaci) zajistí vyvolání metody <see cref="AnimatedControl.DoPaintToolTip(PaintEventArgs)"/> s vhodnou vrstvou.
        /// </summary>
        public virtual bool LayerToolTipValid { get; set; }
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
                this._RunPaintBackgroundNative(args);
            else
                this._RunPaintBackgroundLayered(args);

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
                this._RunPaintStandardNative(args);
            else
                this._RunPaintStandardLayered(args);
        
            if (isActiveDiagnostic) { __PaintStandardEnd = StopwatchExt.ElapsedTicks; _ShowPaintTime(args); }
        }
        /// <summary>
        /// Zde bude vykresleno pozadí controlu.
        /// Bázová metoda vyplní dodanou grafiku aktuální barvou pozadí.
        /// Potomek může tuto metodu přepsat a vykreslit v ní i další motivy, které se nebudou téměř nikdy měnit = ušetří se časy repaintu standardní vrstvy!
        /// </summary>
        /// <param name="args"></param>
        protected virtual void DoPaintBackground(LayeredPaintEventArgs args)
        {
            args.Graphics.Clear(this.BackColor);
        }
        /// <summary>
        /// Zde potomek vykreslí obsah standardní vrstvy.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void DoPaintStandard(LayeredPaintEventArgs args) { }
        /// <summary>
        /// Zde potomek vykreslí obsah vrstvy Overlay.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void DoPaintOverlay(LayeredPaintEventArgs args) { }
        /// <summary>
        /// Zde potomek vykreslí obsah vrstvy ToolTip.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void DoPaintToolTip(LayeredPaintEventArgs args) { }
        #region Kreslení nativní
        /// <summary>
        /// Obsluha kreslení Background bez využití vrstev
        /// </summary>
        /// <param name="args"></param>
        private void _RunPaintBackgroundNative(PaintEventArgs args)
        {
            __PaintBackgroundCount++;
            using (LayeredPaintEventArgs largs = new LayeredPaintEventArgs(args))
                DoPaintBackground(largs);
        }
        /// <summary>
        /// Obsluha kreslení obsahu bez využití vrstev
        /// </summary>
        /// <param name="args"></param>
        private void _RunPaintStandardNative(PaintEventArgs args)
        {
            __PaintStandardCount++;
            using (LayeredPaintEventArgs largs = new LayeredPaintEventArgs(args))
                DoPaintStandard(largs);
        }
        #endregion
        #region Kreslení s využitím vrstev
        /// <summary>
        /// Obsluha kreslení Background s využitím vrstev
        /// </summary>
        /// <param name="args"></param>
        private void _RunPaintBackgroundLayered(PaintEventArgs args)
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
        /// <summary>
        /// Obsluha kreslení obsahu s využitím vrstev
        /// </summary>
        /// <param name="args"></param>
        private void _RunPaintStandardLayered(PaintEventArgs args)
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
                bool isActive = LayerOverlayActive;
                layer.IsActive = isActive;
                if (isActive)
                {   // Vrstva Overlay může být deaktivována!
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
            }

            if (this.LayeredGraphics.UseToolTipLayer)
            {
                var layer = this.LayeredGraphics.ToolTipLayer;
                bool isActive = LayerOverlayActive;
                layer.IsActive = isActive;
                if (isActive)
                {   // Vrstva ToolTip může být deaktivována!
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
            }

            var topLayer = this.LayeredGraphics.CurrentLayer;
            if (topLayer != null && topLayer.ContainData)
                topLayer.RenderTo(args.Graphics);
        }
        /// <summary>
        /// Barva pozadí Controlu.
        /// Setování nové (odlišné) barvy nastaví do <see cref="AnimatedControl.LayerBackgroundValid"/> false, tím vyžádá repaint vrstvy Background.
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
            var backgroundTime = StopwatchExt.GetMilisecsRound(__PaintBackgroundStart, __PaintBackgroundEnd, 3);
            var standardTime = StopwatchExt.GetMilisecsRound(__PaintStandardStart, __PaintStandardEnd, 3);
            __DiagnosticData.Add(new DiagnosticItem(backgroundTime, standardTime));
            DiagnosticItem averageTime = DiagnosticItem.CreateAverage(__DiagnosticData.Items);
            string info = $"BgrCount: {__PaintBackgroundCount}; BgrTime: {averageTime.BackgroundTime:F3} milisec; StdCount: {__PaintStandardCount}; StdTime: {averageTime.StandardTime:F3} milisec";
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
        CycleBuffer<DiagnosticItem> __DiagnosticData;
        private class DiagnosticItem
        {
            public DiagnosticItem(double backgroundTime, double standardTime)
            {
                BackgroundTime = backgroundTime;
                StandardTime = standardTime;
            }
            private DiagnosticItem()
            {
                BackgroundTime = 0d;
                StandardTime = 0d;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"BackgroundTime: {BackgroundTime:F3} ms; StandardTime: {StandardTime:F3} ms";
            }
            public static DiagnosticItem CreateAverage(IEnumerable<DiagnosticItem> items)
            {
                var summary = new DiagnosticItem();
                int count = 0;
                foreach (var item in items)
                {
                    summary.Add(item);
                    count++;
                }
                if (count > 0)
                    summary.Divide(count);

                return summary;
            }
            private void Add(DiagnosticItem item)
            {
                BackgroundTime += item.BackgroundTime;
                StandardTime += item.StandardTime;
            }
            private void Divide(int divider)
            {
                BackgroundTime /= (double)divider;
                StandardTime /= (double)divider;
            }
            public double BackgroundTime { get; set; }
            public double StandardTime { get; set; }
        }
        #endregion
        #endregion
    }
}
