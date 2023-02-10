using System;
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
