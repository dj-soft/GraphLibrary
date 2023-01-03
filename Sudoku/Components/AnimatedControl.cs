using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

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
            __Animator = new Animator(this);
            __StopwatchExt = new Data.StopwatchExt(true);
            __LayeredGraphics = new LayeredGraphicStandard(this);
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
        public Data.StopwatchExt StopwatchExt { get { return __StopwatchExt; } } private Data.StopwatchExt __StopwatchExt;
        #endregion
        #region Layered graphics
        /// <summary>
        /// Správce vrstev bufferované grafiky
        /// </summary>
        protected LayeredGraphicStandard LayeredGraphics { get { return __LayeredGraphics; } } private LayeredGraphicStandard __LayeredGraphics;
        protected override void OnPaintBackground(PaintEventArgs args)
        {
            _PaintBgrCount++;
            _PaintBgrStart = StopwatchExt.ElapsedTicks;
            this.DoPaintBackground(args);
        }
        protected override void OnPaint(PaintEventArgs args)
        {
            _PaintStdCount++;
            _PaintStdStart = StopwatchExt.ElapsedTicks;
            this.DoPaintStandard(args);
            var paintStdEnd = StopwatchExt.ElapsedTicks;
            var bgrTime = StopwatchExt.GetMilisecsRound(_PaintBgrStart, _PaintStdStart, 3);
            var stdTime = StopwatchExt.GetMilisecsRound(_PaintStdStart, paintStdEnd, 3);
            string info = $"BgrCount: {_PaintBgrCount}; BgrTime: {bgrTime} milisec; StdCount: {_PaintStdCount}; StdTime: {stdTime} milisec";
            args.Graphics.DrawString(info, SystemFonts.StatusFont, Brushes.Black, new PointF(20f, 80f));
        }
        int _PaintBgrCount;
        long _PaintBgrStart;
        int _PaintStdCount;
        long _PaintStdStart;

        protected virtual void DoPaintBackground(PaintEventArgs args) 
        {
            args.Graphics.Clear(this.BackColor);
        }
        protected virtual void DoPaintStandard(PaintEventArgs args)
        { }
        #endregion
    }
}
