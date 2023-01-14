using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace DjSoft.Games.Animated
{
    public static class Extensions
    {
        #region Point, Size, Rectangle
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="shiftByX"></param>
        /// <param name="shiftByY"></param>
        /// <returns></returns>
        public static Rectangle ShiftBy(this Rectangle bounds, int shiftByX, int shiftByY)
        {
            return new Rectangle(bounds.X + shiftByX, bounds.Y + shiftByY, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="shiftByX"></param>
        /// <param name="shiftByY"></param>
        /// <returns></returns>
        public static RectangleF ShiftBy(this RectangleF bounds, float shiftByX, float shiftByY)
        {
            return new RectangleF(bounds.X + shiftByX, bounds.Y + shiftByY, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="shiftBy"></param>
        /// <returns></returns>
        public static Rectangle ShiftBy(this Rectangle bounds, Point shiftBy)
        {
            return new Rectangle(bounds.X + shiftBy.X, bounds.Y + shiftBy.Y, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="shiftBy"></param>
        /// <returns></returns>
        public static RectangleF ShiftBy(this RectangleF bounds, PointF shiftBy)
        {
            return new RectangleF(bounds.X + shiftBy.X, bounds.Y + shiftBy.Y, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="shiftBy"></param>
        /// <returns></returns>
        public static Rectangle ShiftBy(this Rectangle bounds, Size shiftBy)
        {
            return new Rectangle(bounds.X + shiftBy.Width, bounds.Y + shiftBy.Height, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí this rectangle posunutý o dodané hodnoty
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="shiftBy"></param>
        /// <returns></returns>
        public static RectangleF ShiftBy(this RectangleF bounds, SizeF shiftBy)
        {
            return new RectangleF(bounds.X + shiftBy.Width, bounds.Y + shiftBy.Height, bounds.Width, bounds.Height);
        }
        /// <summary>
        /// Vrátí this point vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="point"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static Point Zoom(this Point point, float zoom)
        {
            return Point.Round(Zoom((PointF)point, zoom));
        }
        /// <summary>
        /// Vrátí this point vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="point"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static PointF Zoom(this PointF point, float zoom)
        {
            return new PointF(zoom * point.X, zoom * point.Y);
        }
        /// <summary>
        /// Vrátí this Size vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="size"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static Size Zoom(this Size size, float zoom)
        {
            return Size.Round(Zoom((SizeF)size, zoom));
        }
        /// <summary>
        /// Vrátí this SizeF vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="size"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static SizeF Zoom(this SizeF size, float zoom)
        {
            return new SizeF(zoom * size.Width, zoom * size.Height);
        }
        /// <summary>
        /// Vrátí this Rectangle vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static Rectangle Zoom(this Rectangle bounds, float zoom)
        {
            return Rectangle.Round(Zoom((RectangleF)bounds, zoom));
        }
        /// <summary>
        /// Vrátí this RectangleF vynásobený (ve všech hodnotách) daným Zoomem
        /// </summary>
        /// <param name="size"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static RectangleF Zoom(this RectangleF bounds, float zoom)
        {
            return new RectangleF(zoom * bounds.X, zoom * bounds.Y, zoom * bounds.Width, zoom * bounds.Height);
        }

        public static RectangleF AlignTo(this SizeF size, RectangleF bounds, ContentAlignment alignment, bool shrinkToFit = false)
        {
            float x = 0f;
            float y = 0f;
            float w = size.Width;
            float h = size.Height;
            if (shrinkToFit)
            {
                if (w > bounds.Width) w = bounds.Width;
                if (h > bounds.Height) h = bounds.Height;
            }
            float dw = bounds.Width - w;
            float dh = bounds.Height - h;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x = dw / 2f;
                    break;
                case ContentAlignment.TopRight:
                    x = dw;
                    break;
                case ContentAlignment.MiddleLeft:
                    y = dh / 2f;
                    break;
                case ContentAlignment.MiddleCenter:
                    x = dw / 2f;
                    y = dh / 2f;
                    break;
                case ContentAlignment.MiddleRight:
                    x = dw;
                    y = dh / 2f;
                    break;
                case ContentAlignment.BottomLeft:
                    y = dh;
                    break;
                case ContentAlignment.BottomCenter:
                    x = dw / 2f;
                    y = dh;
                    break;
                case ContentAlignment.BottomRight:
                    x = dw;
                    y = dh;
                    break;
            }
            return new RectangleF(bounds.X + x, bounds.Y + y, w, h);
        }
        #endregion
    }
}
