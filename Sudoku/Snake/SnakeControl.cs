﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using DjSoft.Games.Animated.Components;
using DjSoft.Games.Sudoku.Data;

namespace DjSoft.Games.Animated.Snake
{
    public class SnakeControl : AnimatedControl
    {
        public SnakeControl()
        {
            this.UseBackgroundLayer = true;
            this.UseStandardLayer = true;

            Random rand = new Random();

            _BgrAnimator = new BackColorHSVAnimator(this, Color.FromArgb(100, 200, 250));
            _BgrAnimator.InitAnimation(500, this.Animator, rand);

            _Snake1 = new SnakeItem(this, Color.FromArgb(255, 90, 6, 16), 30);
            _Snake1.InitAnimation(352, 218, this.Animator, rand);
            _Snake2 = new SnakeItem(this, Color.FromArgb(255, 6, 60, 12), 45);
            _Snake2.InitAnimation(412, 315, this.Animator, rand);
            _Snake3 = new SnakeItem(this, Color.FromArgb(255, 6, 6, 70), 55);
            _Snake3.InitAnimation(528, 372, this.Animator, rand);
        }
        private BackColorHSVAnimator _BgrAnimator;
        private SnakeItem _Snake1;
        private SnakeItem _Snake2;
        private SnakeItem _Snake3;

        protected override void DoPaintBackground(LayeredPaintEventArgs args)
        {
            _BgrAnimator.Paint(args);
        }
        protected override void DoPaintStandard(LayeredPaintEventArgs args)
        {
            _Snake1.Paint(args);
            _Snake2.Paint(args);
            _Snake3.Paint(args);
        }
    }
    #region class BackColorHSVAnimator : Animátor barvy BackColor HSV
    /// <summary>
    /// Animátor barvy BackColor.
    /// Pracuje s jednou instancí barvy <see cref="ColorHSV"/>, ve které se animuje složka <see cref="ColorHSV.Hue"/> = odstín.
    /// Tím se plynule mění barva přes celé barevné spektrum, se zachování sytosti <see cref="ColorHSV.Saturation"/> a světlosti <see cref="ColorHSV.Value"/>.
    /// </summary>
    public class BackColorHSVAnimator
    {
        /// <summary>
        /// Konstruktur
        /// </summary>
        /// <param name="owner"></param>
        public BackColorHSVAnimator(AnimatedControl owner, Color baseColor)
        {
            __Owner = owner;
            __ColorHSV = ColorHSV.FromColor(baseColor);
            __ColorHue = __ColorHSV.Hue;
            owner.BackColor = __ColorHSV.Color;
        }
        /// <summary>
        /// Nastartuje animaci
        /// </summary>
        /// <param name="cycle"></param>
        /// <param name="animator"></param>
        /// <param name="rand"></param>
        public void InitAnimation(int cycle, Animator animator, Random rand = null)
        {
            int start = 0;
            if (rand != null)
            {
                cycle = (int)((0.95d + (0.1d * rand.NextDouble())) * (double)cycle);
                start = (int)(rand.NextDouble() * (double)cycle);
            }
            animator.AddMotion(cycle, start, Animator.TimeMode.LinearCycling, 0d, _AnimeBgr, 0d, 360d, null);
        }
        /// <summary>
        /// Jeden animační krok
        /// </summary>
        /// <param name="motion"></param>
        private void _AnimeBgr(Animator.Motion motion)
        {
            double hueDelta = (double)motion.CurrentValue;
            double hueCurrent = (__ColorHue + hueDelta) % 360d;
            __ColorHSV.Hue = hueCurrent;
            Color backColorNew = __ColorHSV.Color;
            Color backColorOld = __Owner.BackColor;
            if (!ValueSupport.IsEqualColors(backColorOld, backColorNew))
                __Owner.BackColor = backColorNew;
        }
        /// <summary>
        /// Vykreslí pozadí
        /// </summary>
        /// <param name="e"></param>
        public void Paint(LayeredPaintEventArgs e)
        {
            double shift = 15d;
            ColorHSV colorHSV = __ColorHSV;
            Color color1 = colorHSV.Color;
            colorHSV.Hue -= shift;
            Color color2 = colorHSV.Color;
            Rectangle bounds = this.__Owner.ClientRectangle;
            Point point1 = new Point(0, 0);
            // Point point2 = new Point(0, bounds.Height);
            Point point2 = new Point(bounds.Width, 0);
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(point1, point2, color1, color2))
                e.Graphics.FillRectangle(brush, bounds);
        }
        private AnimatedControl __Owner;
        private ColorHSV __ColorHSV;
        private double __ColorHue;
    }
    #endregion
    #region class BackColorRGBAnimator : Animátor barvy BackColor RGB
    /// <summary>
    /// Animátor barvy BackColor.
    /// Pracuje se sadou barev RGB v sadě <see cref="AnimatedValueSet"/> = sekvence hodnot (barva) na číselné ose, 
    /// přes kterou probíhá animace a interpolace hodnot.
    /// </summary>
    public class BackColorRGBAnimator
    {
        /// <summary>
        /// Konstruktur
        /// </summary>
        /// <param name="owner"></param>
        public BackColorRGBAnimator(AnimatedControl owner)
        {
            __Owner = owner;
            __BgrValueSet = CreateBgrColorSet(out var backColor);
            __IsDiagnosticActive = AppService.IsDiagnosticActive;
            __AnimeBgrTick = 0;
            __AnimeBgrTime = 0d;
            __AnimeBgrLog = "";
            owner.BackColor = backColor;
        }
        /// <summary>
        /// Nastartuje animaci
        /// </summary>
        /// <param name="cycle"></param>
        /// <param name="animator"></param>
        /// <param name="rand"></param>
        public void InitAnimation(int cycle, Animator animator, Random rand = null)
        {
            int start = 0;
            if (rand != null)
            {
                cycle = (int)((0.95d + (0.1d * rand.NextDouble())) * (double)cycle);
                start = (int)(rand.NextDouble() * (double)cycle);
            }
            animator.AddMotion(cycle, start, Animator.TimeMode.SinusCycling, 0d, _AnimeBgr, __BgrValueSet, null);
        }
        /// <summary>
        /// Vytvoří a vrátí sadu barev k animaci
        /// </summary>
        /// <param name="firstColor"></param>
        /// <returns></returns>
        private AnimatedValueSet CreateBgrColorSet(out Color firstColor)
        {
            byte l = 200;
            byte h = 250;
            firstColor = Color.FromArgb(l, l, h);
            AnimatedValueSet bgrSet = new AnimatedValueSet();
            bgrSet.Add(0d, Color.FromArgb(l, l, h));
            bgrSet.Add(15d, Color.FromArgb(l, h, l));
            bgrSet.Add(35d, Color.FromArgb(h, l, l));
            bgrSet.Add(60d, Color.FromArgb(h, h, l));
            bgrSet.Add(85d, Color.FromArgb(l, h, h));
            bgrSet.Add(105d, Color.FromArgb(h, l, h));
            bgrSet.Add(120d, Color.FromArgb(l, l, l));
            return bgrSet;
        }
        private void _AnimeBgr(Animator.Motion motion)
        {
            Color color = (Color)motion.CurrentValue;
            if (motion.IsCurrentValueChanged)
                __Owner.BackColor = color;

            if (__IsDiagnosticActive)
            {
                __AnimeBgrTick++;
                var time = __Owner.StopwatchExt.ElapsedMilisecs;
                var delay = time - __AnimeBgrTime;
                if (String.IsNullOrEmpty(__AnimeBgrLog))
                    __AnimeBgrLog = "Tick\tTime\tDelay\tIsChanged\tA\tR\tG\tB\r\n";
                __AnimeBgrLog += $"{__AnimeBgrTick}\t{__Owner.StopwatchExt.ElapsedMilisecs:F3}\t{delay:F3}\t{(motion.IsCurrentValueChanged ? "1" : "0")}\t{color.A}\t{color.R}\t{color.G}\t{color.B}\r\n";
                __AnimeBgrTime = time;
                if (motion.IsDone || ((__AnimeBgrTick % 200) == 0))
                {
                    var text = __AnimeBgrLog;
                    __AnimeBgrLog = "";
                }
            }
        }
        /// <summary>
        /// Vykreslí pozadí
        /// </summary>
        /// <param name="e"></param>
        public void Paint(LayeredPaintEventArgs e)
        {

        }
        private AnimatedControl __Owner;
        private AnimatedValueSet __BgrValueSet;
        private bool __IsDiagnosticActive;
        private int __AnimeBgrTick;
        private double __AnimeBgrTime;
        private string __AnimeBgrLog;
    }
    #endregion
    #region class SnakeItem : Jeden pohybující se had
    /// <summary>
    /// Jeden pohybující se had
    /// </summary>
    public class SnakeItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="color"></param>
        /// <param name="length"></param>
        public SnakeItem(AnimatedControl owner, Color color, int length)
        {
            __Owner = owner;
            __Color = color;
            __Brush = new SolidBrush(color);
            __Points = new PointF?[length];
        }
        /// <summary>
        /// Nastartuje animaci
        /// </summary>
        /// <param name="cycleX"></param>
        /// <param name="cycleY"></param>
        /// <param name="animator"></param>
        /// <param name="rand"></param>
        public void InitAnimation(int cycleX, int cycleY, Animator animator, Random rand = null)
        {
            int startX = 0;
            int startY = 0;
            if (rand != null)
            {
                cycleX = (int)((0.90d + (0.2d * rand.NextDouble())) * (double)cycleX);
                cycleY = (int)((0.90d + (0.2d * rand.NextDouble())) * (double)cycleY);
                startX = (int)(rand.NextDouble() * (double)cycleX);
                startY = (int)(rand.NextDouble() * (double)cycleY);
            }
            animator.AddMotion(cycleX, startX, Animator.TimeMode.SinusCycling, 0d, _AnimePointX, 0f, 5000f, null);
            animator.AddMotion(cycleY, startY, Animator.TimeMode.SinusCycling, 0d, _AnimePointY, 0f, 5000f, null);
        }
        private AnimatedControl __Owner;
        private Color __Color;
        private SolidBrush __Brush;
        private float? __PointX = null;
        private float? __PointY = null;
        private PointF?[] __Points;
        /// <summary>
        /// Animace bodu X
        /// </summary>
        /// <param name="motion"></param>
        private void _AnimePointX(Animator.Motion motion)
        {
            if (!this.__PointX.HasValue || motion.IsCurrentValueChanged)
                this.__PointX = (float)motion.CurrentValue;
        }
        /// <summary>
        /// Animace bodu Y
        /// </summary>
        /// <param name="motion"></param>
        private void _AnimePointY(Animator.Motion motion)
        {
            if (!this.__PointY.HasValue || motion.IsCurrentValueChanged)
                this.__PointY = (float)motion.CurrentValue;
            AddPoint();
        }
        /// <summary>
        /// Přidá jeden bod
        /// </summary>
        protected void AddPoint()
        {
            if (__PointX.HasValue && __PointY.HasValue)
            {
                PointF? point = new PointF(__PointX.Value, __PointY.Value);
                for (int i = 0; i < __Points.Length; i++)
                {
                    var shift = __Points[i];
                    __Points[i] = point;
                    point = shift;
                }
                __Owner.LayerStandardValid = false;
            }
        }
        /// <summary>
        /// Vykreslí hada
        /// </summary>
        /// <param name="e"></param>
        public void Paint(LayeredPaintEventArgs e)
        {
            SizeF size = __Owner.ClientSize;
            float sizeW = size.Width - 10f;
            float sizeH = size.Height - 10f;
            Color startColor = __Color;
            Color backColor = __Owner.BackColor;
            Color endColor = Color.FromArgb(16, backColor.R, backColor.G, backColor.B);
            int length = __Points.Length;
            for (int i = length - 1; i >= 0; i--)
            {
                var relPoint = __Points[i];
                if (!relPoint.HasValue) continue;
                float x = 5f + (sizeW * relPoint.Value.X / 5000f);
                float y = 5f + (sizeH * relPoint.Value.Y / 5000f);
                double morphRatio = ((double)i / (double)length);
                __Brush.Color = ValueSupport.MorphValueColor(startColor, morphRatio, endColor);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(__Brush, new RectangleF(x - 3f, y - 3f, 6f, 6f));
            }
        }
    }
    #endregion
}