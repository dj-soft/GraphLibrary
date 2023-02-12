using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using DjSoft.Games.Animated;
using DjSoft.Games.Animated.Components;
using DjSoft.Games.Sudoku.Data;

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
            _BgrAnimator.InitAnimation(4000, this.Animator, rand);

            __SpinnerItem = new SpinnerItem(this);
            __SpinnerItem.LineCount = 6;
            __SpinnerItem.Tempo = 0.60f;
            __SpinnerItem.SpinnerDots = true;
            __SpinnerItem.LinesColor = Color.FromArgb(64, 32, 32, 32);
            __SpinnerItem.PointColor = ColorHSV.FromArgb(216, 16, 128, 16);
            __SpinnerItem.PointColorRotate = true;
            __SpinnerItem.InitAnimation(this.Animator, rand);

            _BgrAnimator.Active = false;
            _BgrAnimator.CurrentColor = Color.FromArgb(60, 60, 60);
            __SpinnerItem.PointColor = ColorHSV.FromArgb(216, 255, 255, 30);

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
    }

    public class SpinnerItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        public SpinnerItem(SpinnerControl owner)
        {
            __Owner = owner;
            __Lines = new List<SpinnerLine>();
            LinesColor = Color.FromArgb(64, 48, 48, 48);
            PointColor = ColorHSV.FromArgb(192, 216, 255, 216);
            Tempo = 1f;
        }
        /// <summary>
        /// Nastartuje animaci
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="rand"></param>
        public void InitAnimation(Animator animator, Random rand = null)
        {
            animator.AddMotion(_AnimeSpinner, null);
        }
        /// <summary>
        /// Vykreslí Spinner
        /// </summary>
        /// <param name="args"></param>
        public void Paint(LayeredPaintEventArgs args, RectangleF spinnerBounds)
        {
            args.PrepareGraphicsFor(GraphicsTargetType.Splines);
            PaintSpinnerBase(args, spinnerBounds);
            foreach (var line in __Lines)
                line.Paint(args, spinnerBounds, __CurrentAngle);
        }
        protected void PaintSpinnerBase(LayeredPaintEventArgs args, RectangleF bounds)
        {
            if (this.SpinnerDots)
                PaintSpinnerBaseDots(args, bounds);
            else
                PaintSpinnerBaseCircle(args, bounds);
        }
        protected void PaintSpinnerBaseDots(LayeredPaintEventArgs args, RectangleF bounds)
        {
        }
        protected void PaintSpinnerBaseCircle(LayeredPaintEventArgs args, RectangleF bounds)
        {
            args.Graphics.FillEllipse(args.GetBrush(Color.FromArgb(64, Color.Wheat)), bounds);
            args.Graphics.DrawEllipse(args.GetPen(Color.FromArgb(64, Color.Gray), 2f), bounds);
        }

        /// <summary>
        /// Vlastník
        /// </summary>
        private SpinnerControl __Owner;
        /// <summary>
        /// Animovaný úhel 0 - 360°
        /// </summary>
        private double __CurrentAngle;
        /// <summary>
        /// Čas (Tick) při poslední animaci. Od této časové hodnoty se počítá posun úhlu.
        /// </summary>
        private long __LastTick;

        private List<SpinnerLine> __Lines;
        /// <summary>
        /// Jeden krok animace
        /// </summary>
        /// <param name="motion"></param>
        private void _AnimeSpinner(Animator.Motion motion)
        {
            _CalculateCurrentAngle(motion);
            __Owner.LayerStandardValid = false;
        }
        /// <summary>
        /// Počet linek
        /// </summary>
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
                __LineAngle = angle0;
                __Owner.LayerStandardValid = false;
            }
        }
        /// <summary>
        /// Úhel mezi dvěma linkami
        /// </summary>
        public double LineAngle { get { return __LineAngle; } } private double __LineAngle;
        /// <summary>
        /// Rychlost otáčení v otáčkách za sekundu.
        /// Platný rozsah je 0.01 až 50 (ot/sec).
        /// </summary>
        public float Tempo
        {
            get { return __Tempo; }
            set { __Tempo = (value < 0.01f ? 0.01f : (value > 50f ? 50f : value)); }
        }
        private float __Tempo;
        /// <summary>
        /// Určí a nastaví aktuální úhel do <see cref="__CurrentAngle"/> podle času a rychlosti...
        /// </summary>
        /// <param name="motion"></param>
        private void _CalculateCurrentAngle(Animator.Motion motion)
        {
            var currentTick = motion.Animator.CurrentTick;
            var startTick = __LastTick;
            if (startTick != 0L)
            {
                // Je známý čas (v milisekundách) od posledního animačního kroku:
                double timeMiliSecs = motion.Animator.GetElapsedMiliSeconds(startTick);
                // Je známá požadovaná rychlost otáčení v otáčkách/sec:
                double cyclePerSec = (double)Tempo;
                // Z toho snadno určíme úhel, o který se za uplynulý čas máme pootočit:
                double angleDiff = cyclePerSec * 0.360d * timeMiliSecs;       // Otáčky za sec = počet 360° úhlů za sekundu, a my máme čas v milisekundách... proto (cyclePerSec * 0.360d) = počet stupňů za 1 milisekundu
                // A o takto určený úhel pootočíme, a zarovnáme do rozsahu 0 - 360:
                __CurrentAngle = (__CurrentAngle + angleDiff) % 360d;
            }
            __LastTick = currentTick;
        }
        /// <summary>
        /// Barva linky spinneru; null = bez vykreslení linky
        /// </summary>
        public Color? LinesColor { get; set; }
        /// <summary>
        /// Barva tečky spinneru; může být cyklována
        /// </summary>
        public ColorHSV PointColor { get; set; }
        /// <summary>
        /// Barva <see cref="PointColor"/> se má pro jednotlivé tečky posouvat na spektru odstínů
        /// </summary>
        public bool PointColorRotate { get; set; }
        /// <summary>
        /// Varianta obrazce
        /// </summary>
        public bool SpinnerDots { get; set; }
        /// <summary>
        /// Jedna linka Spinneru
        /// </summary>
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
            /// Posun fáze této linky odpovídá úhlu 0 - 180°
            /// </summary>
            public double Phase { get; private set; }
            /// <summary>
            /// Vykreslí linii Spinneru
            /// </summary>
            /// <param name="args"></param>
            /// <param name="spinnerBounds"></param>
            /// <param name="currentAngle">Aktuální úhel v rozsahu 0 - 360°;</param>
            public void Paint(LayeredPaintEventArgs args, RectangleF spinnerBounds, double currentAngle)
            {
                if (__Owner.SpinnerDots)
                    PaintDots(args, spinnerBounds, currentAngle);
                else
                    PaintCircle(args, spinnerBounds, currentAngle);
            }
            private void PaintDots(LayeredPaintEventArgs args, RectangleF spinnerBounds, double currentAngle)
            {
                float pointSize = 0.01f * (spinnerBounds.Width < spinnerBounds.Height ? spinnerBounds.Width : spinnerBounds.Height);
                RectangleF pointBounds;
                var pointColorHsv = __Owner.PointColor;
                PointF point;
                float alpha;
                
                currentAngle = 360f - currentAngle;

                CalculateDotsPoint1(spinnerBounds, currentAngle, 1f, out point, out alpha);
                pointBounds = point.GetBoundsFromCenter(pointSize, pointSize);
                pointColorHsv.Alpha = alpha;
                args.Graphics.FillEllipse(args.GetBrush(pointColorHsv.Color), pointBounds);

                CalculateDotsPoint1(spinnerBounds, currentAngle, -1f, out point, out alpha);
                pointBounds = point.GetBoundsFromCenter(pointSize, pointSize);
                pointColorHsv.Alpha = alpha;
                args.Graphics.FillEllipse(args.GetBrush(pointColorHsv.Color), pointBounds);

            }
            private void CalculateDotsPoint1(RectangleF spinnerBounds, double currentAngle, float side, out PointF point, out float alpha)
            {
                var center = spinnerBounds.GetCenter();
                var sizeX = spinnerBounds.Width / 2f;
                var sizeY = spinnerBounds.Height / 2f;
                var dx = _AngleX * sizeX;
                var dy = _AngleY * sizeY;

                // Pozice bodu je dána koeficientem ratio = 0.50 (tečky jsou okolo středu prostoru, nikoli na obvodu):
                float ratio = 0.20f;
                var cx = side * ratio * dx;
                var cy = side * ratio * dy;
                point = center.ShiftBy(cx, cy);

                // Určujeme hodnotu alpha = poměr viditelnosti naší tečky.
                alpha = 1f;
                // "Naše tečky" jsou dvě: jedna na úhlu "Angle" a druhá na úhlu "Angle + 180°" (protože Angle je v rozsahu 0-180).
                // Viditelnost (alpha) je běžně == 1.0f, ale když přes náš úhel přechází animovaný 'currentAngle', pak naše viditelnost klesá k 0 (sinusovkou od 1 k 0 a pak zase k 1).
                // Pokles je v rozsahu úhlů tak, aby plynule přecházel mezi sousedními úhly, tedy __Owner.LineAngle.
                double thisAngle = this.Angle + (side < 0f ? 180d : 0d);            // Náš úhel (první nebo komplementární +180°)
                double lineAngle = 1.5f * __Owner.LineAngle;                               // Úhel mezi dvěma sousedními liniemi
                double angle1 = thisAngle - 0.5d * lineAngle;                       // Úhel, od kterého začíná stmívání viditelnosti
                double angle2 = angle1 + lineAngle;                                 // Úhel, do kterého trvá stmívání viditelnosti
                if (currentAngle < angle1 || currentAngle >= angle2) return;        // Naše tečka je běžně viditelná

                // Naše tečka se bude sinusově stmívat podle úhlu:
                double angleDif = Math.PI * (currentAngle - angle1) / lineAngle;    // Hodnota 0 až Pi odpovídající pozici currentAngle vůči intervalu (angle1 .. angle2)
                double sin = Math.Sin(angleDif);                                    // Hodnota 0 až 1 odpovídající "setmění"
                alpha = 1f - (float)sin;
            }
            private void PaintCircle(LayeredPaintEventArgs args, RectangleF spinnerBounds, double currentAngle)
            { 
                CalculateCirclePoints(spinnerBounds, currentAngle, out PointF begin, out PointF end, out PointF current);

                var lineColor = __Owner.LinesColor;
                if (lineColor.HasValue)
                    args.Graphics.DrawLine(args.GetPen(lineColor.Value, 1f), begin, end);

                float pointSize = 0.03f * (spinnerBounds.Width < spinnerBounds.Height ? spinnerBounds.Width : spinnerBounds.Height);
                var pointBounds = current.GetBoundsFromCenter(pointSize, pointSize);
                var pointColorHsv = __Owner.PointColor;
                if (__Owner.PointColorRotate)
                    pointColorHsv.Hue += this.Phase;       // pointColorHsv je struct, takže změna Hue v lokální proměnné se nepromítá do __Owner.PointColor !!!
                args.Graphics.FillEllipse(args.GetBrush(pointColorHsv.Color), pointBounds);
            }
            /// <summary>
            /// Vypočítá potřebné souřadnice
            /// </summary>
            /// <param name="spinnerBounds"></param>
            /// <param name="currentAngle">Aktuální úhel v rozsahu 0 - 360°;</param>
            /// <param name="begin"></param>
            /// <param name="end"></param>
            /// <param name="current"></param>
            private void CalculateCirclePoints(RectangleF spinnerBounds, double currentAngle, out PointF begin, out PointF end, out PointF current)
            {
                var center = spinnerBounds.GetCenter();
                var sizeX = spinnerBounds.Width / 2f;
                var sizeY = spinnerBounds.Height / 2f;
                var dx = _AngleX * sizeX;
                var dy = _AngleY * sizeY;
                begin = center.ShiftBy(-dx, -dy);
                end = center.ShiftBy(dx, dy);

                // animatedValue je aktuální úhel v rozsahu 0 - 360°;
                // Phase je konstantní posun tohoto úhlu v rozsahu 0 - 180°;
                // Určím sinus výsledného úhlu:
                double rad = GetRad(currentAngle + Phase);
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
