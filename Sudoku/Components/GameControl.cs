using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace DjSoft.Games.Sudoku.Components
{
    public class GameControl : AnimatedControl
    {
        public GameControl()
        {
            this.LayeredGraphics.UseBackgroundLayer = true;
            this.LayeredGraphics.UseStandardLayer = true;

            var bgrSet = CreateBgrColorSet(out var backColor);
            this.BackColor = backColor;
            this.Animator.Fps = 60;
            this.Animator.AddMotion(500, Animator.TimeMode.Cycling, 0d, _AnimeBgr, bgrSet, null);
            _AnimeBgrTick = 0;
            _AnimeBgrTime = 0d;
            _AnimeBgrLog = "Tick\tTime\tDelay\tIsChanged\tA\tR\tG\tB\r\n";

            Random rand = new Random();
            _Snake1 = new AnimeSnake(this, Color.FromArgb(255, 90, 6, 16), 30);
            _Snake1.InitAnimation(352, 218, this.Animator, rand);
            _Snake2 = new AnimeSnake(this, Color.FromArgb(255, 6, 60, 12), 45);
            _Snake2.InitAnimation(412, 315, this.Animator, rand);
            _Snake3 = new AnimeSnake(this, Color.FromArgb(255, 6, 6, 70), 55);
            _Snake3.InitAnimation(528, 372, this.Animator, rand);
        }
        private void _AnimeBgr(Animator.Motion motion)
        {
            Color color = (Color)motion.CurrentValue;
            if (motion.IsCurrentValueChanged)
                this.BackColor = color;
            _AnimeBgrTick++;
            var time = StopwatchExt.ElapsedMilisecs;
            var delay = time - _AnimeBgrTime;
            _AnimeBgrLog += $"{_AnimeBgrTick}\t{StopwatchExt.ElapsedMilisecs:F3}\t{delay:F3}\t{(motion.IsCurrentValueChanged ? "1" : "0")}\t{color.A}\t{color.R}\t{color.G}\t{color.B}\r\n";
            _AnimeBgrTime = time;
            if (motion.IsDone || ((_AnimeBgrTick % 200) == 0))
            {
                var text = _AnimeBgrLog;
            }
        }
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
        private int _AnimeBgrTick;
        private double _AnimeBgrTime;
        private string _AnimeBgrLog;


        private AnimeSnake _Snake1;
        private AnimeSnake _Snake2;
        private AnimeSnake _Snake3;

        protected override void DoPaintStandard(LayeredPaintEventArgs e)
        {
            _Snake1.Paint(e);
            _Snake2.Paint(e);
            _Snake3.Paint(e);
        }
      
    }
    public class AnimeSnake
    {
        public AnimeSnake(AnimatedControl owner, Color color, int length)
        {
            __Owner = owner;
            __Color = color;
            __Brush = new SolidBrush(color);
            __Points = new PointF?[length];
        }
        public void InitAnimation(int cycleX, int cycleY, Animator animator, Random rand = null)
        {
            int startX = 0;
            int startY = 0;
            if (rand != null)
            {
                cycleX = (int)((0.95d + (0.1d * rand.NextDouble())) * (double)cycleX);
                cycleY = (int)((0.95d + (0.1d * rand.NextDouble())) * (double)cycleY);
                startX = (int)(rand.NextDouble() * (double)cycleX);
                startY = (int)(rand.NextDouble() * (double)cycleY);
            }
            animator.AddMotion(cycleX, startX, Animator.TimeMode.Cycling, 0d, _AnimePointX, 0f, 5000f, null);
            animator.AddMotion(cycleY, startY, Animator.TimeMode.Cycling, 0d, _AnimePointY, 0f, 5000f, null);
        }
        private AnimatedControl __Owner;
        private Color __Color;
        private SolidBrush __Brush;
        private float? _PointX = null;
        private float? _PointY = null;
        private PointF?[] __Points;
        private void _AnimePointX(Animator.Motion motion)
        {
            if (!this._PointX.HasValue || motion.IsCurrentValueChanged)
                this._PointX = (float)motion.CurrentValue;
        }
        private void _AnimePointY(Animator.Motion motion)
        {
            if (!this._PointY.HasValue || motion.IsCurrentValueChanged)
                this._PointY = (float)motion.CurrentValue;
            AddPoint();
        }
        protected void AddPoint()
        {
            if (_PointX.HasValue && _PointY.HasValue)
            {
                PointF? point = new PointF(_PointX.Value, _PointY.Value);
                for (int i = 0; i < __Points.Length; i++)
                {
                    var shift = __Points[i];
                    __Points[i] = point;
                    point = shift;
                }
                __Owner.LayerStandardValid = false;
            }
        }
        public void Paint(LayeredPaintEventArgs e)
        {
            SizeF size = __Owner.ClientSize;
            float sizeW = size.Width - 10f;
            float sizeH = size.Height - 30f;
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
}
