using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using DjSoft.Games.Animated;
using DjSoft.Games.Animated.Components;

namespace DjSoft.Games.Animated.Gadgets
{
    public class SpinnerControl : AnimatedControl
    {
        public SpinnerControl()
        {
            this.UseBackgroundLayer = true;
            this.UseStandardLayer = true;

            Random rand = new Random();

            _BgrAnimator = new BackColorHSVAnimator(this, Color.FromArgb(100, 200, 250));
            _BgrAnimator.InitAnimation(2000, this.Animator, rand);

            __SpinnerItem = new SpinnerItem(this, Color.Red);
            __SpinnerItem.LineCount = 12;
            __SpinnerItem.LinesColor = Color.FromArgb(64, 32, 32, 32);
            __SpinnerItem.InitAnimation(200, this.Animator, rand);

        }
        private BackColorHSVAnimator _BgrAnimator;
        private SpinnerItem __SpinnerItem;
        protected override void DoPaintBackground(LayeredPaintEventArgs args)
        {
            _BgrAnimator.Paint(args);
        }
        protected override void DoPaintStandard(LayeredPaintEventArgs args)
        {
            RectangleF spinnerBounds = GetSpinnerBounds();
            PaintSpinnerBase(args, spinnerBounds);
            __SpinnerItem.Paint(args, spinnerBounds);
        }
        protected RectangleF GetSpinnerBounds()
        {
            RectangleF clientBounds = this.ClientRectangle;
            var center = clientBounds.GetCenter();
            float s = 0.94f * (clientBounds.Width < clientBounds.Height ? clientBounds.Width : clientBounds.Height);
            RectangleF spinnerBounds = center.GetBoundsFromCenter(s, s);
            return spinnerBounds;
        }
        protected void PaintSpinnerBase(LayeredPaintEventArgs args, RectangleF bounds)
        {
            args.PrepareGraphicsFor(GraphicsTargetType.Splines);
            args.Graphics.FillEllipse(args.GetBrush(Color.FromArgb(64, Color.Wheat)), bounds);
            args.Graphics.DrawEllipse(args.GetPen(Color.FromArgb(64, Color.Gray), 2f), bounds);
        }
    }

    public class SpinnerItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="color"></param>
        /// <param name="length"></param>
        public SpinnerItem(SpinnerControl owner, Color color)
        {
            __Owner = owner;
            __Color = color;
            __Brush = new SolidBrush(color);
            __Lines = new List<SpinnerLine>();
        }
        /// <summary>
        /// Nastartuje animaci
        /// </summary>
        /// <param name="cycle"></param>
        /// <param name="animator"></param>
        /// <param name="rand"></param>
        public void InitAnimation(int cycle, Animator animator, Random rand = null)
        {
            if (rand != null)
                cycle = (int)((0.90d + (0.2d * rand.NextDouble())) * (double)cycle);

            animator.AddMotion(cycle, Animator.TimeMode.LinearCycling, 0d, _AnimeSpinner, 0f, 360f, null);
        }
        /// <summary>
        /// Vykreslí Spinner
        /// </summary>
        /// <param name="args"></param>
        public void Paint(LayeredPaintEventArgs args, RectangleF spinnerBounds)
        {
            foreach (var line in __Lines)
                line.Paint(args, spinnerBounds, __AnimatedValue);
        }
        private SpinnerControl __Owner;
        private Color __Color;
        private SolidBrush __Brush;
        private double __AnimatedValue;
        private List<SpinnerLine> __Lines;
        private void _AnimeSpinner(Animator.Motion motion)
        {
            __AnimatedValue = (double)((float)motion.CurrentValue);  // motion.CurrentValue je object, v němž je float. Nemohu přetypovat object přímo na double - musím jít přes nativní float...
            __Owner.LayerStandardValid = false;
        }
        public int LineCount
        {
            get { return __Lines.Count; }
            set
            {
                var count = (value < 0 ? 0 : (value > 36 ? 36 : value));
                List<SpinnerLine> lines = new List<SpinnerLine>();
                double angle0 = 180d / (double)count;
                for (int l = 0; l < count; l++)
                {
                    double angle = (double)l * angle0;
                    double phase = angle;
                    lines.Add(new SpinnerLine(this, angle, phase));
                }
                __Lines = lines;
                __Owner.LayerStandardValid = false;
            }
        }
        /// <summary>
        /// Barva linky spinneru
        /// </summary>
        public Color? LinesColor { get; set; }
        private class SpinnerLine
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="angle">Úhel ve stupních 0-360</param>
            /// <param name="phase"></param>
            public SpinnerLine(SpinnerItem owner, double angle, double phase)
            {
                __Owner = owner;
                Angle = angle;
                Phase = phase;
                double rad = GetRad(angle);
                _AngleX = (float)Math.Sin(rad);
                _AngleY = (float)Math.Cos(rad);
            }
            private SpinnerItem __Owner;
            private float _AngleX;
            private float _AngleY;
            /// <summary>
            /// Úhel pod kterým je tato linka
            /// </summary>
            public double Angle { get; private set; }
            /// <summary>
            /// Posun fáze této linky
            /// </summary>
            public double Phase { get; private set; }
            /// <summary>
            /// Barva puntíku
            /// </summary>
            public Color? PointColor { get; private set; }
            /// <summary>
            /// Vykreslí linii Spinneru
            /// </summary>
            /// <param name="args"></param>
            /// <param name="spinnerBounds"></param>
            /// <param name="animatedValue"></param>
            public void Paint(LayeredPaintEventArgs args, RectangleF spinnerBounds, double animatedValue)
            {
                CalculatePoints(spinnerBounds, animatedValue, out PointF begin, out PointF end, out PointF current);
                var lineColor = __Owner.LinesColor;
                if (lineColor.HasValue)
                    args.Graphics.DrawLine(args.GetPen(lineColor.Value, 1f), begin, end);

                float pointSize = 0.03f * (spinnerBounds.Width < spinnerBounds.Height ? spinnerBounds.Width : spinnerBounds.Height);
                var pointBounds = current.GetBoundsFromCenter(pointSize, pointSize);
                args.Graphics.FillEllipse(args.GetBrush(__Owner.__Color), pointBounds);
            }
            private void CalculatePoints(RectangleF spinnerBounds, double animatedValue, out PointF begin, out PointF end, out PointF current)
            {
                var center = spinnerBounds.GetCenter();
                var sizeX = spinnerBounds.Width / 2f;
                var sizeY = spinnerBounds.Height / 2f;
                var dx = _AngleX * sizeX;
                var dy = _AngleY * sizeY;
                begin = center.ShiftBy(-dx, -dy);
                end = center.ShiftBy(dx, dy);

                // animatedValue je aktuální úhel v rozsahu 0 - 360°;
                // Phase je konstantní posun tohoto úhlu v rozsahu 0 - 360°;
                // Určím sinus výsledného úhlu:
                double rad = GetRad(animatedValue + Phase);
                float sin = (float)Math.Sin(rad);
                var cx = dx * sin;
                var cy = dy * sin;
                current = center.ShiftBy(cx, cy);
            }
            /// <summary>
            /// Vrací radiány (0 - 2*Pi) ze stupňů (0 - 360°)
            /// </summary>
            /// <param name="degree"></param>
            /// <returns></returns>
            private static double GetRad(double degree)
            {
                return degree * 2d * Math.PI / 360d;
            }
        }
    }
}
