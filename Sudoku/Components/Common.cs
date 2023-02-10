using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using DjSoft.Games.Sudoku.Data;

namespace DjSoft.Games.Animated.Components
{
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
}
