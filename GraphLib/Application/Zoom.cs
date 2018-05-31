using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.Application
{
    /// <summary>
    /// Třída, která zajišťuje služby Zoomování aplikace
    /// </summary>
    public class Zoom
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Zoom()
        {
            this._Value = 1f;
        }
        /// <summary>
        /// Hodnota Zoomu pro GUI. Její změna změní velikost všech grafických elementů.
        /// Default = 1, pak GUI zobrazuje prvky v originální velikosti.
        /// </summary>
        public float Value
        {
            get { return this._Value; }
            set { this._ValueSet(value); }
        }
        /// <summary>
        /// Uloží novou hodnotu Zoom, zarovná ji do rozmezí 0.2 - 5.0.
        /// Pokud dojde ke změně, resetuje cache fontů (ty mají aktuální Zoom už v sobě)
        /// </summary>
        /// <param name="value"></param>
        private void _ValueSet(float value)
        {
            float oldZoom = this._Value;
            float newZoom = (value < 0.2f ? 0.2f : (value > 5.0f ? 5.0f : value));
            if (newZoom != oldZoom)
            {
                this._Value = newZoom;
                Asol.Tools.WorkScheduler.Components.FontInfo.ResetFonts();
            }
        }
        private float _Value;
        /// <summary>
        /// Vrací souřadnice přepočtené aktuálním Zoomem
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public Rectangle ZoomBounds(Rectangle bounds)
        {
            float zoom = this._Value;
            int l = _ZoomValue(zoom, bounds.Left);
            int t = _ZoomValue(zoom, bounds.Top);
            int r = _ZoomValue(zoom, bounds.Right);
            int b = _ZoomValue(zoom, bounds.Bottom);
            return Rectangle.FromLTRB(l, t, r, b);
        }
        /// <summary>
        /// Vrací souřadnice daného bodu přepočtené aktuálním Zoomem
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Point ZoomPoint(Point point)
        {
            float zoom = this._Value;
            return new Point(_ZoomValue(zoom, point.X), _ZoomValue(zoom, point.Y));
        }
        /// <summary>
        /// Vrací velikost přepočtenou aktuálním Zoomem
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public Size ZoomSize(Size size)
        {
            float zoom = this._Value;
            return new Size(_ZoomValue(zoom, size.Width), _ZoomValue(zoom, size.Height));
        }
        /// <summary>
        /// Vrací vzdálenost přepočtenou aktuálním Zoomem
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public int ZoomDistance(int distance)
        {
            float zoom = this._Value;
            return _ZoomValue(zoom, distance);
        }
        /// <summary>
        /// Vrací přepočet distance * zoom
        /// </summary>
        /// <param name="zoom"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private static int _ZoomValue(float zoom, int distance)
        {
            return (int)(Math.Round((double)(zoom * (float)distance), 0));
        }
    }
}
