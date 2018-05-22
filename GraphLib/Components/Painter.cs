using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Djs.Common.Components
{
    public class GPainter
    {
        #region DrawRadiance
        public static void DrawRadiance(Graphics graphics, Point center, Color centerColor)
        {
            DrawRadiance(graphics, center, null, centerColor);
        }
        public static void DrawRadiance(Graphics graphics, Point center, Rectangle? clipBounds, Color centerColor)
        {
            Rectangle bounds = center.CreateRectangleFromCenter(new Size(45, 30));
            DrawRadiance(graphics, bounds, clipBounds, centerColor);
        }
        public static void DrawRadiance(Graphics graphics, Point center, Size size, Color centerColor)
        {
            DrawRadiance(graphics, center, size, null, centerColor);
        }
        public static void DrawRadiance(Graphics graphics, Point center, Size size, Rectangle? clipBounds, Color centerColor)
        {
            Rectangle bounds = center.CreateRectangleFromCenter(size);
            DrawRadiance(graphics, bounds, clipBounds, centerColor);
        }
        public static void DrawRadiance(Graphics graphics, Rectangle bounds, Color centerColor)
        {
            DrawRadiance(graphics, bounds, null, centerColor);
        }
        public static void DrawRadiance(Graphics graphics, Rectangle bounds, Rectangle? clipBounds, Color centerColor)
        {
            using (System.Drawing.Drawing2D.GraphicsPath p = new System.Drawing.Drawing2D.GraphicsPath())
            {
                p.AddEllipse(bounds);
                using (System.Drawing.Drawing2D.PathGradientBrush b = new System.Drawing.Drawing2D.PathGradientBrush(p))
                {
                    b.CenterColor = centerColor;
                    b.CenterPoint = bounds.Center();
                    b.SurroundColors = new Color[] { Color.Transparent };
                    if (clipBounds.HasValue)
                    {
                        Region clip = graphics.Clip;
                        graphics.SetClip(clipBounds.Value);
                        graphics.FillEllipse(b, bounds);
                        graphics.Clip = clip;
                    }
                    else
                    {
                        graphics.FillEllipse(b, bounds);
                    }
                }
            }
        }
        #endregion
        #region DrawGridHeader
        /// <summary>
        /// Draw button base (background and border, by state)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="state"></param>
        /// <param name="opacity"></param>
        public static void DrawGridHeader(Graphics graphics, Rectangle bounds, RectangleSide side, Color backColor, bool draw3D, Color? lineColor, GInteractiveState state, Orientation orientation, Point? relativePoint, Int32? opacity)
        {
            _DrawGridHeader(graphics, bounds, side, backColor, draw3D, lineColor, state, orientation, relativePoint, opacity);
        }
        /// <summary>
        /// Draw button base (background and border, by state)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="state"></param>
        /// <param name="opacity"></param>
        private static void _DrawGridHeader(Graphics graphics, Rectangle bounds, RectangleSide side, Color backColor, bool draw3D, Color? lineColor, GInteractiveState state, Orientation orientation, Point? relativePoint, Int32? opacity)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            // Pozadí:
            using (Brush brush = Skin.CreateBrushForBackground(bounds, orientation, state, true, backColor, opacity, relativePoint))
            {
                graphics.FillRectangle(brush, bounds);
            }

            // 3D efekt na okrajích:
            draw3D = false;
            if (draw3D)
            {   // 3D okraje NEJSOU kresleny na poslední pixel vpravo a dole (ten je vyhrazen pro linku barvy lineColor), 
                // 3D okraje jsou o 1 pixel před tím:
                Rectangle boundsBorder = (lineColor.HasValue ? bounds.Enlarge(0, 0, -1, -1) : bounds);
                RectangleSide borderSides = _GetHeaderBorderSides(side);
                float? effect3D = _GetHeadersEffect3D(state);
                DrawBorder(graphics, boundsBorder, borderSides, null, backColor, effect3D);
            }

            // Linky vpravo a dole:
            if (lineColor.HasValue)
            {
                DrawBorder(graphics, bounds, RectangleSide.Right | RectangleSide.Bottom, null, null, lineColor, lineColor, null);
            }
        }
        /// <summary>
        /// Metoda vrátí souhrn stran, na kterých se má vykreslit Border, při vykreslování Headeru na dané straně objektu.
        /// Tedy: pokud headerSide == Top, pak se Border kreslí na stranách Left, Bottom, Right.
        /// Pokud headerSide == Left, pak se Border kreslí na stranách Top, Right, Bottom. Atd.
        /// </summary>
        /// <param name="headerSide"></param>
        /// <returns></returns>
        private static RectangleSide _GetHeaderBorderSides(RectangleSide headerSide)
        {
            switch (headerSide)
            {
                case RectangleSide.Top: return RectangleSide.Left | RectangleSide.Bottom | RectangleSide.Right;
                case RectangleSide.Left: return RectangleSide.Top | RectangleSide.Right | RectangleSide.Bottom;
                case RectangleSide.Bottom: return RectangleSide.Left | RectangleSide.Top | RectangleSide.Right;
                case RectangleSide.Right: return RectangleSide.Top | RectangleSide.Left | RectangleSide.Bottom;
                case RectangleSide.None: return RectangleSide.None;
            }
            return RectangleSide.All;
        }
        /// <summary>
        /// Metoda vrátí hodnotu effect3D pro konkrétní interaktivní stav
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private static float? _GetHeadersEffect3D(GInteractiveState state)
        {
            switch (state)
            {
                case GInteractiveState.Disabled: return 0f;
                case GInteractiveState.None: return 0.25f;
                case GInteractiveState.Enabled: return 0.25f;
                case GInteractiveState.MouseOver: return 0.50f;
                case GInteractiveState.LeftDown:
                case GInteractiveState.RightDown: return -0.35f;
                case GInteractiveState.LeftDrag:
                case GInteractiveState.RightDrag: return -0.15f;
            }
            return null;
        }
        #endregion
        #region DrawBorder
        /// <summary>
        /// Vykreslí okraje kolem daného prostoru. 
        /// Kreslí i pravou a dolní hranu na okraj daného rozměru.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="sides"></param>
        /// <param name="dashStyle"></param>
        /// <param name="lineColor"></param>
        /// <param name="effect3D">Hodnota 3D efektu: 
        /// kladná vytváří "nahoru zvednutý povrch" (tj. color1 = nahoře/vlevo je světlejší, color2 = dole/vpravo je tmavší),
        /// kdežto záporná hodnota vytváří "dolů promáčknutý povrch".
        /// Hodnota 1.00 vytvoří bílou a černou barvu, hodnota 0.10f vytvoří lehký 3D efekt, 0.50f poměrně silný efekt.
        /// </param>
        public static void DrawBorder(Graphics graphics, Rectangle bounds, RectangleSide sides, DashStyle? dashStyle, Color lineColor, float? effect3D)
        {
            Color? colorTop = lineColor;
            Color? colorRight = lineColor;
            Color? colorBottom = lineColor;
            Color? colorLeft = lineColor;
            if (effect3D.HasValue && effect3D.Value != 0f)
            {
                if (effect3D.Value > 0f)
                {   // Vlevo a nahoře bude barva světlejší, vpravo a dole tmavší:
                    colorTop = lineColor.Morph(Skin.Modifiers.Effect3DLight, effect3D.Value);
                    colorRight = lineColor.Morph(Skin.Modifiers.Effect3DDark, effect3D.Value);
                    colorBottom = colorRight;
                    colorLeft = colorTop;
                }
                else
                {   // Vlevo a nahoře bude barva tmavší, vpravo a dole světlejší:
                    colorTop = lineColor.Morph(Skin.Modifiers.Effect3DDark, -effect3D.Value);
                    colorRight = lineColor.Morph(Skin.Modifiers.Effect3DLight, -effect3D.Value);
                    colorBottom = colorRight;
                    colorLeft = colorTop;
                }
            }

            DrawBorder(graphics, bounds, sides, dashStyle, colorTop, colorRight, colorBottom, colorLeft);
        }
        /// <summary>
        /// Vykreslí okraje kolem daného prostoru. 
        /// Kreslí i pravou a dolní hranu na okraj daného rozměru.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="sides"></param>
        /// <param name="dashStyle"></param>
        public static void DrawBorder(Graphics graphics, Rectangle bounds, RectangleSide sides, DashStyle? dashStyle, Color? colorTop, Color? colorRight, Color? colorBottom, Color? colorLeft)
        {
            int x0 = bounds.X;
            int y0 = bounds.Y;
            int x1 = x0 + bounds.Width - 1;
            int y1 = y0 + bounds.Height - 1;
            using (Pen pen = new Pen(Color.Black))
            {
                pen.DashStyle = (dashStyle.HasValue ? dashStyle.Value : DashStyle.Solid);

                if (colorTop.HasValue && (sides & RectangleSide.Top) != 0)
                {
                    pen.Color = colorTop.Value;
                    graphics.DrawLine(pen, x0, y0, x1, y0);
                }
                if (colorRight.HasValue && (sides & RectangleSide.Right) != 0)
                {
                    pen.Color = colorRight.Value;
                    graphics.DrawLine(pen, x1, y0, x1, y1);
                }
                if (colorBottom.HasValue && (sides & RectangleSide.Bottom) != 0)
                {
                    pen.Color = colorBottom.Value;
                    graphics.DrawLine(pen, x0, y1, x1, y1);
                }
                if (colorLeft.HasValue && (sides & RectangleSide.Left) != 0)
                {
                    pen.Color = colorLeft.Value;
                    graphics.DrawLine(pen, x0, y0, x0, y1);
                }
            }
        }
        #endregion
        #region DrawArea
        /// <summary>
        /// Draw button base (background and border, by state)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="opacity"></param>
        public static void DrawAreaBase(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, Point? point, Int32? opacity)
        {
            DrawAreaBase(graphics, bounds, color, GInteractiveState.Enabled, orientation, point, opacity, 0);
        }
        /// <summary>
        /// Draw button base (background and border, by state)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="opacity"></param>
        public static void DrawAreaBase(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, Point? point, Int32? opacity, int roundCorner)
        {
            DrawAreaBase(graphics, bounds, color, GInteractiveState.Enabled, orientation, point, opacity, roundCorner);
        }
        /// <summary>
        /// Draw button base (background and border, by state)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="state"></param>
        /// <param name="opacity"></param>
        public static void DrawAreaBase(Graphics graphics, Rectangle bounds, Color color, GInteractiveState state, Orientation orientation, Point? point, Int32? opacity)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            DrawAreaBase(graphics, bounds, color, state, orientation, point, opacity, 0);
        }
        /// <summary>
        /// Draw button base (background and border, by state)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="state"></param>
        /// <param name="opacity"></param>
        public static void DrawAreaBase(Graphics graphics, Rectangle bounds, Color color, GInteractiveState state, Orientation orientation, Point? point, Int32? opacity, int roundCorner)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            int roundX = roundCorner;
            int roundY = roundCorner;
            using (GraphicsPath path = CreatePathRoundRectangle(bounds, roundX, roundY))
            using (Brush brush = Skin.CreateBrushForBackground(bounds, orientation, state, true, color, opacity, point))
            {
                graphics.FillPath(brush, path);
            }
        }
        #endregion
        #region DrawEffect3D
        /// <summary>
        /// Vykreslí rectangle danou barvou, s 3D efektem v dané intenzitě a orientaci
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="orientation"></param>
        /// <param name="effect3D">Hodnota 3D efektu: 
        /// kladná vytváří "nahoru zvednutý povrch" (tj. color1 = nahoře/vlevo je světlejší, color2 = dole/vpravo je tmavší),
        /// kdežto záporná hodnota vytváří "dolů promáčknutý povrch".
        /// Hodnota 1.00 vytvoří bílou a černou barvu, hodnota 0.10f vytvoří lehký 3D efekt, 0.50f poměrně silný efekt.
        /// </param>  /// <param name="opacity"></param>
        public static void DrawEffect3D(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, float? effect3D)
        {
            _DrawEffect3D(graphics, bounds, color, orientation, effect3D, null);
        }
        /// <summary>
        /// Vykreslí rectangle danou barvou, s 3D efektem v dané intenzitě a orientaci
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="orientation"></param>
        /// <param name="effect3D">Hodnota 3D efektu: 
        /// kladná vytváří "nahoru zvednutý povrch" (tj. color1 = nahoře/vlevo je světlejší, color2 = dole/vpravo je tmavší),
        /// kdežto záporná hodnota vytváří "dolů promáčknutý povrch".
        /// Hodnota 1.00 vytvoří bílou a černou barvu, hodnota 0.10f vytvoří lehký 3D efekt, 0.50f poměrně silný efekt.
        /// </param>  /// <param name="opacity"></param>
        public static void DrawEffect3D(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, float? effect3D, Int32? opacity)
        {
            _DrawEffect3D(graphics, bounds, color, orientation, effect3D, opacity);
        }
        /// <summary>
        /// Vykreslí rectangle danou barvou, s 3D efektem v dané intenzitě a orientaci
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="orientation"></param>
        /// <param name="effect3D">Hodnota 3D efektu: 
        /// kladná vytváří "nahoru zvednutý povrch" (tj. color1 = nahoře/vlevo je světlejší, color2 = dole/vpravo je tmavší),
        /// kdežto záporná hodnota vytváří "dolů promáčknutý povrch".
        /// Hodnota 1.00 vytvoří bílou a černou barvu, hodnota 0.10f vytvoří lehký 3D efekt, 0.50f poměrně silný efekt.
        /// </param> /// <param name="opacity"></param>
        private static void _DrawEffect3D(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, float? effect3D, Int32? opacity)
        {
            Color color1, color2;
            if (CreateEffect3DColors(color, effect3D, out color1, out color2))
            {   // 3D efekt:
                using (Brush brush = Skin.CreateBrushForBackgroundGradient(bounds, orientation, color1, color2))
                {
                    graphics.FillRectangle(brush, bounds);
                }
            }
            else
            {   // Plná plochá barva:
                graphics.FillRectangle(Skin.Brush(color), bounds);
            }
        }
        /// <summary>
        /// Metoda vygeneruje pár barev out color1 a color2 pro danou barvu výchozí a daný 3D efekt.
        /// Metoda vrací true = barvy pro 3D efekt jsou vytvořeny / false = daná hodnota efektu není 3D, prostor se má vybarvit plnou barvou.
        /// Barvy světla a stínu se přebírají z hodnot Skin.Control.Effect3DLight a Skin.Control.Effect3DDark.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="effect3D">Hodnota 3D efektu: 
        /// kladná vytváří "nahoru zvednutý povrch" (tj. color1 = nahoře/vlevo je světlejší, color2 = dole/vpravo je tmavší),
        /// kdežto záporná hodnota vytváří "dolů promáčknutý povrch".
        /// Hodnota 1.00 vytvoří bílou a černou barvu, hodnota 0.10f vytvoří lehký 3D efekt, 0.50f poměrně silný efekt.
        /// </param>
        /// <param name="color1">Barva nahoře/vlevo</param>
        /// <param name="color2">Barva dole/vpravo</param>
        /// <returns></returns>
        public static bool CreateEffect3DColors(Color color, float? effect3D, out Color color1, out Color color2)
        {
            color1 = color;
            color2 = color;
            if (!effect3D.HasValue || effect3D.Value == 0f) return false;

            float ratio = effect3D.Value;
            if (ratio > 0f)
            {   // Nahoru = barva 1 je světlejší, barva 2 je tmavší:
                color1 = color.Morph(Skin.Modifiers.Effect3DLight, ratio);
                color2 = color.Morph(Skin.Modifiers.Effect3DDark, ratio);
            }
            else
            {   // Dolů = barva 1 je tmavší, barva 2 je světlejší:
                ratio = -ratio;
                color1 = color.Morph(Skin.Modifiers.Effect3DDark, ratio);
                color2 = color.Morph(Skin.Modifiers.Effect3DLight, ratio);
            }
            return true;
        }
        #endregion
        #region DrawButton
        /// <summary>
        /// Draw button base (background and border, by state)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="opacity"></param>
        public static void DrawButtonBase(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, Point? point, Int32? opacity)
        {
            _DrawButtonBase(graphics, bounds, color, GInteractiveState.Enabled, orientation, 2, point, opacity, true, true, null);
        }
        /// <summary>
        /// Draw button base (background and border, by state)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="opacity"></param>
        public static void DrawButtonBase(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, int roundCorner, Point? point, Int32? opacity)
        {
            _DrawButtonBase(graphics, bounds, color, GInteractiveState.Enabled, orientation, roundCorner, point, opacity, true, true, null);
        }
        /// <summary>
        /// Draw button base (background and border, by state)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="state"></param>
        /// <param name="opacity"></param>
        public static void DrawButtonBase(Graphics graphics, Rectangle bounds, Color color, GInteractiveState state, Orientation orientation, Point? point, Int32? opacity)
        {
            _DrawButtonBase(graphics, bounds, color, state, orientation, 2, point, opacity, true, true, null);
        }
        /// <summary>
        /// Draw button base (background and border, by state)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="state"></param>
        /// <param name="opacity"></param>
        public static void DrawButtonBase(Graphics graphics, Rectangle bounds, Color color, GInteractiveState state, Orientation orientation, int roundCorner, Point? point, Int32? opacity)
        {
            _DrawButtonBase(graphics, bounds, color, state, orientation, roundCorner, point, opacity, true, true, null);
        }
        /// <summary>
        /// Draw button base (background and border, by state)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="state"></param>
        /// <param name="opacity"></param>
        public static void DrawButtonBase(Graphics graphics, Rectangle bounds, Color color, GInteractiveState state, Orientation orientation, int roundCorner, Point? point, Int32? opacity, bool drawBackground, bool drawBorders)
        {
            _DrawButtonBase(graphics, bounds, color, state, orientation, roundCorner, point, opacity, drawBackground, drawBorders, null);
        }
        /// <summary>
        /// Draw button base (background and border, by state)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="state"></param>
        /// <param name="opacity"></param>
        public static void DrawButtonBase(Graphics graphics, Rectangle bounds, Color color, GInteractiveState state, Orientation orientation, int roundCorner, Point? point, Int32? opacity, bool drawBackground, bool drawBorders, Color colorBorder)
        {
            _DrawButtonBase(graphics, bounds, color, state, orientation, roundCorner, point, opacity, drawBackground, drawBorders, colorBorder);
        }
        private static void _DrawButtonBase(Graphics graphics, Rectangle bounds, Color color, GInteractiveState state, Orientation orientation, int roundCorner, Point? point, Int32? opacity, bool drawBackground, bool drawBorders, Color? colorBorder)
        {
            bounds = bounds.Enlarge(0, 0, -1, -1);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            int roundX = roundCorner;
            int roundY = roundCorner;

            try
            {

                using (GPainter.GraphicsUseSmooth(graphics))
                {
                    if (drawBackground)
                    {
                        using (GraphicsPath path = CreatePathRoundRectangle(bounds, roundX, roundY))
                        using (Brush brush = Skin.CreateBrushForBackground(bounds, orientation, state, true, color, opacity, point))
                        {
                            graphics.FillPath(brush, path);
                        }
                    }

                    if (drawBorders)
                    {
                        Color borderColor = (colorBorder.HasValue ? colorBorder.Value : Skin.Button.BorderColor);
                        Color borderColorBegin, borderColorEnd;
                        Skin.ModifyBackColorByState(Skin.Button.BorderColor, state, true, out borderColorBegin, out borderColorEnd);

                        using (GraphicsPath pathBegin = CreatePathRoundRectangle(bounds, roundX, roundY, p => (p == RelativePosition.BottomLeft || p == RelativePosition.Left || p == RelativePosition.LeftTop || p == RelativePosition.Top)))
                        {
                            graphics.DrawPath(Skin.Pen(borderColorBegin), pathBegin);
                        }
                        using (GraphicsPath pathEnd = CreatePathRoundRectangle(bounds, roundX, roundY, p => (p == RelativePosition.TopRight || p == RelativePosition.Right || p == RelativePosition.RightBottom || p == RelativePosition.Bottom)))
                        {
                            graphics.DrawPath(Skin.Pen(borderColorEnd), pathEnd);
                        }
                    }
                }
            }

            catch (Exception)
            {   // Only for debug...
                throw;
            }
        }
        #endregion
        #region DrawString
        /// <summary>
        /// Draw a text to specified area
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="fontInfo"></param>
        /// <param name="alignment"></param>
        public static void DrawString(Graphics graphics, Rectangle bounds, string text, Color color, FontInfo fontInfo, ContentAlignment alignment)
        {
            Rectangle textArea;
            _DrawString(graphics, bounds, text, null, color, fontInfo, alignment, null, out textArea);
        }
        /// <summary>
        /// Draw a text to specified area
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="fontInfo"></param>
        /// <param name="alignment"></param>
        public static void DrawString(Graphics graphics, Rectangle bounds, string text, Color color, FontInfo fontInfo, ContentAlignment alignment, Action<Rectangle> drawBackground)
        {
            Rectangle textArea;
            _DrawString(graphics, bounds, text, null, color, fontInfo, alignment, drawBackground, out textArea);
        }
        /// <summary>
        /// Draw a text to specified area
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="fontInfo"></param>
        /// <param name="alignment"></param>
        public static void DrawString(Graphics graphics, Rectangle bounds, string text, Brush brush, FontInfo fontInfo, ContentAlignment alignment)
        {
            Rectangle textArea;
            _DrawString(graphics, bounds, text, brush, null, fontInfo, alignment, null, out textArea);
        }
        /// <summary>
        /// Draw a text to specified area
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="fontInfo"></param>
        /// <param name="alignment"></param>
        public static void DrawString(Graphics graphics, Rectangle bounds, string text, Brush brush, FontInfo fontInfo, ContentAlignment alignment, Action<Rectangle> drawBackground)
        {
            Rectangle textArea;
            _DrawString(graphics, bounds, text, brush, null, fontInfo, alignment, drawBackground, out textArea);
        }
        /// <summary>
        /// Draw a text to specified area
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="fontInfo"></param>
        /// <param name="alignment"></param>
        public static void DrawString(Graphics graphics, Rectangle bounds, string text, Color color, FontInfo fontInfo, ContentAlignment alignment, out Rectangle textArea)
        {
            _DrawString(graphics, bounds, text, null, color, fontInfo, alignment, null, out textArea);
        }
        /// <summary>
        /// Draw a text to specified area
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="fontInfo"></param>
        /// <param name="alignment"></param>
        public static void DrawString(Graphics graphics, Rectangle bounds, string text, Color color, FontInfo fontInfo, ContentAlignment alignment, Action<Rectangle> drawBackground, out Rectangle textArea)
        {
            _DrawString(graphics, bounds, text, null, color, fontInfo, alignment, drawBackground, out textArea);
        }
        /// <summary>
        /// Draw a text to specified area
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="fontInfo"></param>
        /// <param name="alignment"></param>
        public static void DrawString(Graphics graphics, Rectangle bounds, string text, Brush brush, FontInfo fontInfo, ContentAlignment alignment, out Rectangle textArea)
        {
            _DrawString(graphics, bounds, text, brush, null, fontInfo, alignment, null, out textArea);
        }
        /// <summary>
        /// Draw a text to specified area
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="fontInfo"></param>
        /// <param name="alignment"></param>
        public static void DrawString(Graphics graphics, Rectangle bounds, string text, Brush brush, FontInfo fontInfo, ContentAlignment alignment, Action<Rectangle> drawBackground, out Rectangle textArea)
        {
             _DrawString(graphics, bounds, text, brush, null, fontInfo, alignment, drawBackground, out textArea);
        }
        /// <summary>
        /// Draw a text to specified area
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="text"></param>
        /// <param name="brush"></param>
        /// <param name="color"></param>
        /// <param name="fontInfo"></param>
        /// <param name="alignment"></param>
        /// <param name="drawBackground"></param>
        /// <param name="textArea"></param>
        private static void _DrawString(Graphics graphics, Rectangle bounds, string text, Brush brush, Color? color, FontInfo fontInfo, ContentAlignment alignment, Action<Rectangle> drawBackground, out Rectangle textArea)
        {
            textArea = new Rectangle(bounds.X, bounds.Y, 0, 0);
            if (String.IsNullOrEmpty(text)) return;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            StringFormat sf = new StringFormat(StringFormatFlags.LineLimit);

            using (GraphicsUseText(graphics))
            {
                // graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                Font font = fontInfo.Font;
                SizeF textSize = graphics.MeasureString(text, font, bounds.Width, sf);
                textArea = textSize.AlignTo(bounds, alignment, true);
                if (drawBackground != null)
                    drawBackground(textArea);

                if (brush != null)
                    graphics.DrawString(text, font, brush, textArea, sf);
                else if (color.HasValue)
                    graphics.DrawString(text, font, Skin.Brush(color.Value), textArea, sf);
                else
                    graphics.DrawString(text, font, SystemBrushes.ControlText, textArea, sf);
            }
        }
        #endregion
        #region MeasureString
        public static Size MeasureString(string text, FontInfo fontInfo)
        {
            if (String.IsNullOrEmpty(text)) return new Size(0, 0);

            Font font = fontInfo.Font;
            int height = font.Height;
            float width = font.Size * (float)text.Length + 6f;
            return new Size((int)width, height);
        }

        public static Size MeasureString(Graphics graphics, string text, FontInfo fontInfo)
        {
            if (String.IsNullOrEmpty(text)) return new Size(0, 0);

            Font font = fontInfo.Font;
            SizeF sizeF = graphics.MeasureString(text, font);
            Size size = sizeF.Enlarge(1f, 3f).ToSize();
            return size;
        }
        #endregion
        #region DrawWindow
        public static void DrawWindow(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, Int32? opacity)
        {
            _DrawWindow(graphics, bounds, color, orientation, opacity, null, null);
        }
        public static void DrawWindow(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, Int32? opacity, Int32? borderX, Int32? borderY)
        {
            _DrawWindow(graphics, bounds, color, orientation, opacity, borderX, borderY);
        }
        private static void _DrawWindow(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, Int32? opacity, Int32? borderX, Int32? borderY)
        {
            int dx = (borderX.HasValue ? borderX.Value : 5);
            dx = (dx < 0 ? 0 : (dx > 14 ? 1 : dx));
            int dy = (borderY.HasValue ? borderY.Value : 2);
            dy = (dy < 0 ? 0 : (dy > 14 ? 1 : dy));

            int x = bounds.X;
            int y = bounds.Y;
            int w = bounds.Width;
            int h = bounds.Height;
            int r = bounds.Right - 1;
            int b = bounds.Bottom - 1;
            int q = h / 3;

            Color color0 = color.Morph(Color.White, 0.850f).SetOpacity(opacity);
            Color color1 = color.Morph(Color.Black, 0.350f).SetOpacity(opacity);
            Color color2 = color.Morph(Color.White, 0.650f).SetOpacity(opacity);
            Color color3 = color.Morph(Color.Black, 0.850f).SetOpacity(opacity);
            
            Rectangle boundsF = new Rectangle(x, y, w, h);
         
            using (SolidBrush brush = new SolidBrush(color0))
            using (LinearGradientBrush lgb = new LinearGradientBrush(boundsF, color1, color2, 90f))
            using (GraphicsPath path = new GraphicsPath())
            {
                graphics.FillRectangle(lgb, boundsF);
                if (dy > 0)
                {
                    path.AddLine(x, y, r, y);
                    path.AddLine(r, y, r - dy, y + dy);
                    path.AddLine(r - dy, y + dy, x + dy, y + dy);
                    path.AddLine(x + dy, y + dy, x, y); ;
                    path.CloseFigure();
                    brush.Color = color0;
                    graphics.FillPath(brush, path);
                    path.Reset();

                    path.AddLine(x, b, r, b);
                    path.AddLine(r, b, r - dy, b - dy);
                    path.AddLine(r - dy, b - dy, x + dy, b - dy);
                    path.AddLine(x + dy, b - dy, x, y); ;
                    path.CloseFigure();
                    brush.Color = color3;
                    graphics.FillPath(brush, path);
                    path.Reset();
                }

                if (dx > 0)
                {
                    using (GraphicsUseSmooth(graphics))
                    {
                        path.AddLine(x, y, x + dy, y + dy);
                        if (dx == dy)
                            path.AddLine(x + dy, y + dy, x + dy, b - dy);
                        else
                            path.AddBezier(x + dy, y + dy, x + dx, y + q, x + dx, b - q, x + dy, b - dy);
                        path.AddLine(x + dy, b - dy, x, b);
                        path.AddLine(x, b, x, y);
                        path.CloseFigure();
                        brush.Color = color0;
                        graphics.FillPath(brush, path);
                        path.Reset();

                        path.AddLine(r, y, r - dy, y + dy);
                        if (dx == dy)
                            path.AddLine(r - dy, y + dy, r - dy, b - dy);
                        else
                            path.AddBezier(r - dy, y + dy, r - dx, y + q, r - dx, b - q, r - dy, b - dy);
                        path.AddLine(r - dy, b - dy, r, b);
                        path.AddLine(r, b, r, y);
                        path.CloseFigure();
                        brush.Color = color3;
                        graphics.FillPath(brush, path);
                        path.Reset();
                    }
                }
            }
        }
        #endregion
        #region DrawInsertMark
        /// <summary>
        /// Vykreslí prosvícení v dané části prostoru, které signalizuje aktivní část prostoru - například při Drag and Drop, nebo MouseHot stav
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="contentAlignment"></param>
        internal static void DrawInsertMark(Graphics graphics, Rectangle bounds, Color color, ContentAlignment contentAlignment)
        {
            DrawInsertMark(graphics, bounds, color, contentAlignment, false, null);
        }
        /// <summary>
        /// Vykreslí prosvícení v dané části prostoru, které signalizuje aktivní část prostoru - například při Drag and Drop, nebo MouseHot stav
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="contentAlignment"></param>
        /// <param name="asArc"></param>
        /// <param name="opacity"></param>
        internal static void DrawInsertMark(Graphics graphics, Rectangle bounds, Color color, ContentAlignment contentAlignment, bool asArc, int? opacity)
        {
            Color color1 = Color.FromArgb((opacity.HasValue ? opacity.Value : 160), color);
            Color color2 = Color.Transparent;
            switch (contentAlignment)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.TopCenter:
                case ContentAlignment.TopRight:
                    break;
                case ContentAlignment.MiddleLeft:
                    // Mark at left edge:
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        if (asArc)
                        {   // Half-ellipse with left side vertical:
                            path.AddPie(bounds.X - bounds.Width, bounds.Y, bounds.Width + bounds.Width, bounds.Height, 270f, 180f);
                            path.AddLine(bounds.X, bounds.Bottom, bounds.X, bounds.Y);
                            path.CloseFigure();
                        }
                        else
                        {   // Rectangle with Gradient
                            path.AddRectangle(bounds);
                        }
                        Rectangle boundsBrush = bounds.Enlarge(2, 0, 1, 0);        // .NET has error in LinearGradientBrush when angle == 180°, first column on left edge of rectangle has filled with full color from right side.
                        using (LinearGradientBrush brush = new LinearGradientBrush(boundsBrush, color1, color2, 0f))
                        {
                            graphics.FillPath(brush, path);
                        }
                    }
                    break;
                case ContentAlignment.MiddleCenter:
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        if (asArc)
                        {   // Half-ellipse with left side vertical:
                            path.AddEllipse(bounds);
                        }
                        else
                        {   // Rectangle with Gradient
                            path.AddRectangle(bounds);
                        }
                        using (PathGradientBrush brush = new PathGradientBrush(path))
                        {
                            brush.CenterPoint = bounds.Center();
                            brush.CenterColor = color1;
                            brush.SurroundColors = new Color[] { color2 };

                            graphics.FillPath(brush, path);
                        }
                    }
                    break;
                case ContentAlignment.MiddleRight:
                    // Mark at right edge:
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        if (asArc)
                        {   // Half-ellipse with left side vertical:
                            path.AddPie(bounds.X, bounds.Y, bounds.Width + bounds.Width, bounds.Height, 90f, 180f);
                            path.AddLine(bounds.X, bounds.Y, bounds.X, bounds.Bottom);
                            path.CloseFigure();
                        }
                        else
                        {   // Rectangle with Gradient
                            path.AddRectangle(bounds);
                        }
                        Rectangle boundsBrush = bounds.Enlarge(2, 0, 1, 0);        // .NET has error in LinearGradientBrush when angle == 180°, first column on left edge of rectangle has filled with full color from right side.
                        using (LinearGradientBrush brush = new LinearGradientBrush(boundsBrush, color1, color2, 180f))
                        {
                            graphics.FillPath(brush, path);
                        }
                    }
                    break;
                case ContentAlignment.BottomLeft:
                case ContentAlignment.BottomCenter:
                case ContentAlignment.BottomRight:
                    break;
            }
        }
        #endregion
        #region DrawAxis
        /// <summary>
        /// Paint axis background
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="orientation"></param>
        /// <param name="enabled"></param>
        /// <param name="state"></param>
        /// <param name="color"></param>
        /// <param name="morph"></param>
        public static void DrawAxisBackground(Graphics graphics, Rectangle bounds, Orientation orientation, bool enabled, GInteractiveState state, Color color, float morph)
        {
            if (!enabled)
                state = GInteractiveState.Disabled;
            else if (state == GInteractiveState.LeftDrag)
                state = GInteractiveState.LeftDown;
            GPainter.DrawAreaBase(graphics, bounds, color, state, orientation, null, null, 0);
        }
        #endregion
        #region DrawShadow
        public static void DrawShadow(Graphics graphics, Rectangle bounds)
        {
            _DrawShadow(graphics, bounds, 5, false);
        }
        public static void DrawShadow(Graphics graphics, Rectangle bounds, int size)
        {
            _DrawShadow(graphics, bounds, size, false);
        }
        public static void DrawShadow(Graphics graphics, Rectangle bounds, int size, bool inner)
        {
            _DrawShadow(graphics, bounds, size, inner);
        }
        private static void _DrawShadow(Graphics graphics, Rectangle bounds, int size, bool inner)
        {
            int s = (size > 10 ? 10 : (size < 2 ? 2 : size));
            int s2 = s + s;
            Rectangle area = bounds;
            if (inner && bounds.Width > s2 && bounds.Height > s2)
                area = new Rectangle(bounds.X + s, bounds.Y + s, bounds.Width - s2 - 1, bounds.Height - s2 - 1);

            // Modify area and size for shadow:
            area = _DrawShadowModify(area, ref s);
            s2 = s + s;

            int l = area.Left;
            int t = area.Top;
            int w = area.Width;
            int h = area.Height;
            int r = area.Right;
            int b = area.Bottom;

            int ls = l - s;
            int ts = t - s;
            int rs = r + s;
            int bs = b + s;

            _DrawShadowInn(graphics, l, t, w, h);                    // Inner
            _DrawShadowGrd(graphics, 0f, ls + 1, t, s, h - 1, 1);    // Left      0 +1px
            _DrawShadowPie(graphics, ls, ts, s, 180f);               // LeftTop
            _DrawShadowGrd(graphics, 90f, l, ts + 1, w - 1, s, 1);   // Top      90 +1px
            _DrawShadowPie(graphics, r - s - 1, ts, s, 270f);        // TopRight
            _DrawShadowGrd(graphics, 180f, r - 1, t, s, h - 1, 0);   // Right   180
            _DrawShadowPie(graphics, r - s - 1, b - s - 1, s, 0f);   // RightBottom
            _DrawShadowGrd(graphics, 270f, l, b - 1, w - 1, s, 0);   // Bottom  270
            _DrawShadowPie(graphics, ls, b - s - 1, s, 90f);         // BottomLeft
        }

        private static Rectangle _DrawShadowModify(Rectangle area, ref int s)
        {
            int ds = 2 * s / 3;
            int x = area.X + ds;
            int y = area.Y + ds;
            int w = area.Width - ds - 1;
            int h = area.Height - ds - 1;

            s = s + 1;
            return new Rectangle(x, y, w, h);

            /*
            // Modify:
            int modX = 1;
            int modY = 1;
            int modD = 2;
            area.Inflate(-modD, -modD);
            area.Width += modX;
            area.Height += modY;
            s += modD;
            // end.
            */


        }
        private static void _DrawShadowInn(Graphics graphics, int x, int y, int width, int height)
        {
            graphics.FillRectangle(Skin.Brush(Skin.Shadow.InnerColor), x, y, width, height);
        }
        private static void _DrawShadowGrd(Graphics graphics, float angle, int x, int y, int width, int height, int brushOffset)
        {
            Rectangle brushBounds = new Rectangle(x - brushOffset, y - brushOffset, width, height);
            using (LinearGradientBrush lgb = new LinearGradientBrush(brushBounds, Skin.Shadow.OuterColor, Skin.Shadow.InnerColor, angle))
            {
                Rectangle bounds = new Rectangle(x, y, width, height);
                graphics.FillRectangle(lgb, bounds);
            }
        }
        private static void _DrawShadowPie(Graphics graphics, int x, int y, int size, float angle)
        {
            int brushSize = 2 * size;
            Rectangle brushBounds = new Rectangle(x, y, brushSize, brushSize);
            using (System.Drawing.Drawing2D.GraphicsPath p = new System.Drawing.Drawing2D.GraphicsPath())
            {
                p.AddEllipse(brushBounds);
                using (System.Drawing.Drawing2D.PathGradientBrush b = new System.Drawing.Drawing2D.PathGradientBrush(p))
                {
                    b.CenterColor = Skin.Shadow.InnerColor;
                    b.CenterPoint = brushBounds.Center();
                    b.SurroundColors = new Color[] { Skin.Shadow.OuterColor };

                    graphics.FillPie(b, brushBounds, angle, 90f);
                }
            }
        }
        #endregion
        #region CreatePath
        #region RoundRectangle
        /// <summary>
        /// Create GraphicsPath containing rounded rectangle.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="roundX"></param>
        /// <param name="roundY"></param>
        /// <returns></returns>
        public static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY)
        {
            return CreatePathRoundRectangle(bounds, roundX, roundY, null, false, null);
        }
        /// <summary>
        /// Create GraphicsPath containing rounded rectangle.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="roundX"></param>
        /// <param name="roundY"></param>
        /// <returns></returns>
        public static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY, RelativePosition positions)
        {
            return CreatePathRoundRectangle(bounds, roundX, roundY, p => ((p & positions) != 0), false, null);
        }
        /// <summary>
        /// Create GraphicsPath containing rounded rectangle.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="roundX"></param>
        /// <param name="roundY"></param>
        /// <returns></returns>
        public static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY, RelativePosition positions, bool joinBroken)
        {
            return CreatePathRoundRectangle(bounds, roundX, roundY, p => ((p & positions) != 0), joinBroken, null);
        }
        /// <summary>
        /// Create GraphicsPath containing rounded rectangle.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="roundX"></param>
        /// <param name="roundY"></param>
        /// <returns></returns>
        public static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY, Func<RelativePosition, bool> partSelector)
        {
            return CreatePathRoundRectangle(bounds, roundX, roundY, partSelector, false, null);
        }
        /// <summary>
        /// Create GraphicsPath containing rounded rectangle.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="roundX"></param>
        /// <param name="roundY"></param>
        /// <returns></returns>
        public static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY, Func<RelativePosition, bool> partSelector, bool joinBroken)
        {
            return CreatePathRoundRectangle(bounds, roundX, roundY, partSelector, joinBroken, null);
        }
        /// <summary>
        /// Create GraphicsPath containing rounded rectangle.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="roundX"></param>
        /// <param name="roundY"></param>
        /// <returns></returns>
        public static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY, Action<GraphicsPath, RelativePosition, Point, Point> lineHandler)
        {
            return CreatePathRoundRectangle(bounds, roundX, roundY, null, false, lineHandler);
        }
        /// <summary>
        /// Create GraphicsPath containing rounded rectangle.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="roundX"></param>
        /// <param name="roundY"></param>
        /// <returns></returns>
        public static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY, Func<RelativePosition, bool> partSelector, Action<GraphicsPath, RelativePosition, Point, Point> lineHandler)
        {
            return CreatePathRoundRectangle(bounds, roundX, roundY, partSelector, false, lineHandler);
        }
        /// <summary>
        /// Create GraphicsPath containing rounded rectangle.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="roundX"></param>
        /// <param name="roundY"></param>
        /// <returns></returns>
        public static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY, Func<RelativePosition, bool> partSelector, bool joinBroken, Action<GraphicsPath, RelativePosition, Point, Point> lineHandler)
        {
            GraphicsPath gp = new GraphicsPath();

            int rxr = roundX;
            int rxd = 2 * rxr;
            int ryr = roundY;
            int ryd = 2 * ryr;
            int total = 8;
            bool rounded = (rxd > 0 && ryd > 0 && bounds.Width > rxd && bounds.Height > ryd);
            if (!rounded)
            {
                rxr = 0;
                rxd = 0;
                ryr = 0;
                ryd = 0;
                total = 4;
            }

            int sbx = 1;
            int sby = 1;
            int px0 = bounds.X;
            int px1 = bounds.X + rxr;
            int px2 = bounds.Right - sbx - rxr;
            int px3 = bounds.Right - sbx;
            int py0 = bounds.Y;
            int py1 = bounds.Y + ryr;
            int py2 = bounds.Bottom - sby - ryr;
            int py3 = bounds.Bottom - sby;

            bool isBroken = false;
            int partCount = 0;

            if (rounded && _CreatePathRoundRectangleIsSelected(gp, partSelector, joinBroken, ref isBroken, ref partCount, RelativePosition.LeftTop, RelativePosition.TopLeft))
                gp.AddArc(px0, py0, rxd, ryd, 180f, 90f);

            if (_CreatePathRoundRectangleIsSelected(gp, partSelector, joinBroken, ref isBroken, ref partCount, RelativePosition.Top))
                _CreatePathRoundRectangleAddLine(gp, RelativePosition.Top, px1, py0, px2, py0, lineHandler);

            if (rounded && _CreatePathRoundRectangleIsSelected(gp, partSelector, joinBroken, ref isBroken, ref partCount, RelativePosition.TopRight, RelativePosition.RightTop))
                gp.AddArc(px3 - rxd, py0, rxd, ryd, 270f, 90f);

            if (_CreatePathRoundRectangleIsSelected(gp, partSelector, joinBroken, ref isBroken, ref partCount, RelativePosition.Right))
                _CreatePathRoundRectangleAddLine(gp, RelativePosition.Right, px3, py1, px3, py2, lineHandler);

            if (rounded && _CreatePathRoundRectangleIsSelected(gp, partSelector, joinBroken, ref isBroken, ref partCount, RelativePosition.RightBottom, RelativePosition.BottomRight))
                gp.AddArc(px3 - rxd, py3 - ryd, rxd, ryd, 0f, 90f);

            if (_CreatePathRoundRectangleIsSelected(gp, partSelector, joinBroken, ref isBroken, ref partCount, RelativePosition.Bottom))
                _CreatePathRoundRectangleAddLine(gp, RelativePosition.Bottom, px2, py3, px1, py3, lineHandler);

            if (rounded && _CreatePathRoundRectangleIsSelected(gp, partSelector, joinBroken, ref isBroken, ref partCount, RelativePosition.BottomLeft, RelativePosition.LeftBottom))
                gp.AddArc(px0, py3 - ryd, rxd, ryd, 90f, 90f);

            if (_CreatePathRoundRectangleIsSelected(gp, partSelector, joinBroken, ref isBroken, ref partCount, RelativePosition.Left))
                _CreatePathRoundRectangleAddLine(gp, RelativePosition.Left, px0, py2, px0, py1, lineHandler);

            if (partCount == total)
                gp.CloseFigure();

            return gp;
        }
        private static bool _CreatePathRoundRectangleIsSelected(GraphicsPath gp, Func<RelativePosition, bool> partSelector, bool joinBroken, ref bool isBroken, ref int partCount, params RelativePosition[] positions)
        {
            bool selected = false;
            if (partSelector != null)
            {
                foreach (RelativePosition position in positions)
                {
                    if (partSelector(position))
                    {
                        selected = true;
                        break;
                    }
                }
            }
            else
            {
                selected = true;
            }

            if (selected)
            {
                partCount++;
                if (isBroken)
                {
                    gp.StartFigure();
                    isBroken = false;
                }
            }
            else if (!joinBroken)
            {
                isBroken = true;
            }

            return selected;
        }
        private static void _CreatePathRoundRectangleAddLine(GraphicsPath gp, RelativePosition position, int x0, int y0, int x1, int y1, Action<GraphicsPath, RelativePosition, Point, Point> lineHandler)
        {
            if (lineHandler != null)
                lineHandler(gp, position, new Point(x0, y0), new Point(x1, y1));
            else
                gp.AddLine(x0, y0, x1, y1);
        }
    	#endregion
    	#region RoundRectangleWithArrow
        /// <summary>
        /// Create GraphicsPath containing rounded rectangle with arrow to specific point (tooltip, label, comics, etc).
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="roundX"></param>
        /// <param name="roundY"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static GraphicsPath CreatePathRoundRectangleWithArrow(Rectangle bounds, int roundX, int roundY, Point target)
        {
            RelativePosition position = FindTargetRelativePosition(target, bounds, roundX + 7, roundY + 7);
            return CreatePathRoundRectangle(bounds, roundX, roundY, (gp, lp, p0, p1) => _AddLineWithArrow(gp, lp, p0, p1, position));
        }
        private static void _AddLineWithArrow(GraphicsPath gp, RelativePosition linePosition, Point point0, Point point1, RelativePosition arrowPosition)
        {
            gp.AddLine(point0, point1);
        }
        /// <summary>
        /// Detect target point position relative to specified bounds.
        /// Position is Inside (target is inside bounds), or one from nine positions.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public static RelativePosition FindTargetRelativePosition(Point target, Rectangle bounds)
        {
            return FindTargetRelativePosition(target, bounds, 0, 0);
        }
        /// <summary>
        /// Detect target point position relative to specified bounds, which can be reduced by specified border (x,y).
        /// Position is Inside (target is inside bounds), or one from nine positions.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="bounds"></param>
        /// <param name="borderX"></param>
        /// <param name="borderY"></param>
        /// <returns></returns>
        public static RelativePosition FindTargetRelativePosition(Point target, Rectangle bounds, int borderX, int borderY)
        {
            if (bounds.Contains(target)) return RelativePosition.Inside;
            
            // Coordinates of X (left, right) and Y (top, bottom), of bounds + borders:
            int xl = bounds.Left + borderX;
            int xr = bounds.Right - 1 - borderX;
            if (xl > xr)
            {
                xl = bounds.Left + bounds.Width / 2;
                xr = xl;
            }
            int yt = bounds.Top + borderY;
            int yb = bounds.Bottom - 1 - borderY;
            if (yt > yb)
            {
                yt = bounds.Top + bounds.Height / 2;
                yb = yt;
            }

            // Cordinates of point, distances point from borders:
            int x = target.X;
            int y = target.Y;
            int dxl = xl - x;
            int dxr = x - xr;
            int dyt = yt - y;
            int dyb = y - yb;
            if (dxl > 0)
            {   // On Left:
                if (dyt > 0) return ((dxl >= dyt) ? RelativePosition.LeftTop : RelativePosition.TopLeft);                // Left, Top:    when dx[Left] >= dy[Top], then LeftTop, otherwise TopLeft
                if (dyb > 0) return ((dxl >= dyb) ? RelativePosition.LeftBottom : RelativePosition.BottomLeft);          // Left, Bottom: when dx[Left] >= dy[Bottom], then LeftBottom, otherwise BottomLeft
                return RelativePosition.Left;
            }

            if (dxr > 0)
            {   // On Right:
                if (dyt > 0) return ((dxr >= dyt) ? RelativePosition.RightTop : RelativePosition.TopRight);              // Right, Top:    when dx >= dy[Top], then RightTop, otherwise TopRight
                if (dyb > 0) return ((dxr >= dyb) ? RelativePosition.RightBottom : RelativePosition.BottomRight);        // Right, Bottom: when dx >= dy[Bottom], then RightBottom, otherwise BottomRight
                return RelativePosition.Right;
            }

            if (dyt > 0) return RelativePosition.Top;        // On Top (only Top; because other is solved: LeftTop,TopLeft,TopRight,RightTop)
            if (dyb > 0) return RelativePosition.Bottom;     // On Bottom (dtto)
            return RelativePosition.None;
        }
    	#endregion
        #region LinearShapeType
        /// <summary>
        /// Returns an GraphicsPath with specified shape, in specified area and border.
        /// Returns null for shape == None or area with small dimensions (smaller than 10px).
        /// Path have been drawed as Line (graphics.DrawPath()), not filled as area (graphics.FillPath()).
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="area"></param>
        /// <param name="border"></param>
        /// <returns></returns>
        public static GraphicsPath CreatePathLinearShape(LinearShapeType shape, Rectangle area, int border)
        {
            GraphicSetting graphicSetting;
            return CreatePathLinearShape(shape, area, border, out graphicSetting);
        }
        /// <summary>
        /// Returns an GraphicsPath with specified shape, in specified area and border.
        /// Returns null for shape == None or area with small dimensions (smaller than 10px).
        /// Path have been drawed as Line (graphics.DrawPath()), not filled as area (graphics.FillPath()).
        /// Store optimal GraphicSetting for draw this shape.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="area"></param>
        /// <param name="border"></param>
        /// <param name="graphicSetting"></param>
        /// <returns></returns>
        public static GraphicsPath CreatePathLinearShape(LinearShapeType shape, Rectangle area, int border, out GraphicSetting graphicSetting)
        {
            graphicSetting = GraphicSetting.None;
            if (shape == LinearShapeType.None) return null;
            if ((area.Width - 2 * border) < 10 || (area.Height - 2 * border) < 10) return null;
            Point center = area.Center();
            int size = ((area.Width < area.Height ? area.Width : area.Height) / 2) - border;
            Rectangle bounds = center.CreateRectangleFromCenter(2 * size);
            bounds = bounds.ShiftBy(1, 1);
            switch (shape)
            {
                case LinearShapeType.LeftArrow: return _CreatePathLinearShapeLeftArrow(bounds, out graphicSetting);
                case LinearShapeType.UpArrow: return _CreatePathLinearShapeUpArrow(bounds, out graphicSetting);
                case LinearShapeType.RightArrow: return _CreatePathLinearShapeRightArrow(bounds, out graphicSetting);
                case LinearShapeType.DownArrow: return _CreatePathLinearShapeDownArrow(bounds, out graphicSetting);
                case LinearShapeType.HorizontalLines: return _CreatePathLinearShapeHorizontalLines(bounds, out graphicSetting);
                case LinearShapeType.VerticalLines: return _CreatePathLinearShapeVerticalLines(bounds, out graphicSetting);
            }
            return null;
        }
        private static GraphicsPath _CreatePathLinearShapeLeftArrow(Rectangle bounds, out GraphicSetting graphicSetting)
        {
            int h = (((bounds.Height + 1) / 2) - 1);        // Y pixels/2     4       5       9
            int c = ((bounds.Width + 1) / 2);               // X center       5       6      10
            int x = c - ((2 * h) / 3);                      // X left         3
            GraphicsPath path = new GraphicsPath();         // Examples for: 10px    11px    19px
            path.AddLine(x + h, 0, x, h);
            path.AddLine(x, h, x + h, h + h);
            _ShiftPath(path, bounds.Location);
            graphicSetting = GraphicSetting.Smooth;
            return path;
        }
        private static GraphicsPath _CreatePathLinearShapeUpArrow(Rectangle bounds, out GraphicSetting graphicSetting) 
        {
            int w = (((bounds.Width + 1) / 2) - 1);         // X pixels/2     4       5       9
            int c = (((bounds.Height + 1) / 2) - 1);        // Y center       5       6      10
            int y = c - ((2 * w) / 3);                      // Y top          3
            GraphicsPath path = new GraphicsPath();         // Examples for: 10px    11px    19px
            path.AddLine(0, y + w, w, y);
            path.AddLine(w, y, w + w, y + w);
            _ShiftPath(path, bounds.Location);
            graphicSetting = GraphicSetting.Smooth;
            return path;
        }
        private static GraphicsPath _CreatePathLinearShapeRightArrow(Rectangle bounds, out GraphicSetting graphicSetting) 
        {
            int h = (((bounds.Height + 1) / 2) - 1);        // Y pixels/2     4       5       9
            int c = ((bounds.Width + 1) / 2);               // X center       5       6      10
            int x = c + ((2 * h) / 3);                      // X right        3
            GraphicsPath path = new GraphicsPath();         // Examples for: 10px    11px    19px
            path.AddLine(x - h, 0, x, h);
            path.AddLine(x, h, x - h, h + h);
            _ShiftPath(path, bounds.Location);
            graphicSetting = GraphicSetting.Smooth;
            return path;
        }
        private static GraphicsPath _CreatePathLinearShapeDownArrow(Rectangle bounds, out GraphicSetting graphicSetting)
        {
            int w = (((bounds.Width + 1) / 2) - 1);         // X pixels/2     4       5       9
            int c = ((bounds.Height + 1) / 2);              // Y center       5       6      10
            int y = c + ((2 * w) / 3);                      // Y bottom       3
            GraphicsPath path = new GraphicsPath();         // Examples for: 10px    11px    19px
            path.AddLine(0, y - w, w, y);
            path.AddLine(w, y, w + w, y - w);
            _ShiftPath(path, bounds.Location);
            graphicSetting = GraphicSetting.Smooth;
            return path;
        }
        private static GraphicsPath _CreatePathLinearShapeHorizontalLines(Rectangle bounds, out GraphicSetting graphicSetting)
        {
            int ch = ((bounds.Height + 1) / 2);
            int h = (ch - 1);
            int cl = ((bounds.Width + 1) / 2);
            GraphicsPath path = new GraphicsPath();         // Examples for: 10px    11px    19px
            path.AddLine(cl - 4, ch - h + 1, cl - 4, ch + h - 2);
            path.CloseFigure();
            path.AddLine(cl - 2, ch - h, cl - 2, ch + h - 1);
            path.CloseFigure();
            path.AddLine(cl + 0, ch - h, cl + 0, ch + h - 1);
            path.CloseFigure();
            path.AddLine(cl + 2, ch - h, cl + 2, ch + h - 1);
            path.CloseFigure();
            path.AddLine(cl + 4, ch - h + 1, cl + 4, ch + h - 2);
            path.CloseFigure();
            _ShiftPath(path, bounds.Location);
            graphicSetting = GraphicSetting.Sharp;
            return path;
        }
        private static GraphicsPath _CreatePathLinearShapeVerticalLines(Rectangle bounds, out GraphicSetting graphicSetting) 
        {
            int ch = ((bounds.Width + 1) / 2);
            int h = (ch - 1);
            int cl = ((bounds.Height + 1) / 2);
            GraphicsPath path = new GraphicsPath();         // Examples for: 10px    11px    19px
            path.AddLine(ch - h + 1, cl - 4, ch + h - 2, cl - 4);
            path.CloseFigure();
            path.AddLine(ch - h, cl - 2, ch + h - 1, cl - 2);
            path.CloseFigure();
            path.AddLine(ch - h, cl + 0, ch + h - 1, cl + 0);
            path.CloseFigure();
            path.AddLine(ch - h, cl + 2, ch + h - 1, cl + 2);
            path.CloseFigure();
            path.AddLine(ch - h + 1, cl + 4, ch + h - 2, cl + 4);
            path.CloseFigure();
            _ShiftPath(path, bounds.Location);
            graphicSetting = GraphicSetting.Sharp;
            return path;
        }

        private static void _ShiftPath(GraphicsPath path, Point point)
        {
            Matrix matrix = new Matrix();
            matrix.Translate(point.X, point.Y);
            path.Transform(matrix);
        }
        #endregion
        #region Ellipse
        /// <summary>
        /// Returns a rectangle of the ellipse, which is circumscribed by the specified inner area, and distance from edge of inner area.
        /// </summary>
        /// <param name="innerArea"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Rectangle CreateEllipseAroundRectangle(Rectangle innerArea, int distance)
        {
            double ew = (double)innerArea.Width;
            if (ew < 10d) ew = 10d;
            double eh = (double)innerArea.Height;
            if (eh < 10d) eh = 10d;
            double a = (double)(ew > eh ? ew : eh);                  // Max (Width, Height): create a square above the larger of the two dimensions,
            double d = Math.Sqrt(2d * a * a) + (double)distance;     //  which creates a diagonal diameter of the circle circumscribed over a square. Increase diameter by 6px.
            double ex = d * ew / a;                                  // Dimension of ellipse on X axis, Y axis:
            double ey = d * eh / a;
            return innerArea.Center().CreateRectangleFromCenter((int)ex, (int)ey);
        }
        #endregion
        #endregion
        #region Set Graphics, restore previous state
        /// <summary>
        /// Prepare Graphics for Smooth / Text / Sharp drawing, by graphicSetting value.
        /// Return its original state, can be restored after drawing:  
        /// var state = Painter.GraphicsSet(graphics, GraphicSetting.Text); any drawing...;  graphics.Restore(state);
        /// Restore of state is not requierd.
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static GraphicsState GraphicsSet(Graphics graphics, GraphicSetting graphicSetting)
        {
            switch (graphicSetting)
            {
                case GraphicSetting.None: return graphics.Save();
                case GraphicSetting.Smooth: return GraphicsSetSmooth(graphics);
                case GraphicSetting.Text: return GraphicsSetText(graphics);
                case GraphicSetting.Sharp: return GraphicsSetSharp(graphics);
            }
            return graphics.Save();
        }
        /// <summary>
        /// Prepare Graphics for smooth drawing (curve, text), quality smooth rendering.
        /// Return its original state, can be restored after drawing:  
        /// var state = Painter.GraphicsSetSmooth(graphics); any drawing...;  graphics.Restore(state);
        /// Restore of state is not requierd.
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static GraphicsState GraphicsSetSmooth(Graphics graphics)
        {
            GraphicsState state = graphics.Save();
            _GraphicsSetSmooth(graphics);
            return state;
        }
        /// <summary>
        /// Prepare Graphics for text drawing (lines, text), fast rendering lines and quality rendering of texts.
        /// Return its original state, can be restored after drawing:  
        /// var state = Painter.GraphicsSetSmooth(graphics); any drawing...;  graphics.Restore(state);
        /// Restore of state is not requierd.
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static GraphicsState GraphicsSetText(Graphics graphics)
        {
            GraphicsState state = graphics.Save();
            _GraphicsSetText(graphics);
            return state;
        }
        /// <summary>
        /// Prepare Graphics for sharp sharp (rectangles), fast rendering.
        /// Return its original state, can be restored after drawing:  
        /// var state = Painter.GraphicsSetSharp(graphics); any drawing...;  graphics.Restore(state);
        /// Restore of state is not requierd.
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static GraphicsState GraphicsSetSharp(Graphics graphics)
        {
            GraphicsState state = graphics.Save();
            _GraphicsSetSharp(graphics);
            return state;
        }
        /// <summary>
        /// Prepare Graphics for Smooth / Text / Sharp drawing, by graphicSetting value.
        /// Return a disposable object, which at its Dispose returns graphics to the original state.
        /// You must use this method in using pattern:
        /// using(Painter.GraphicsUseSmooth(graphics) { any drawing... }.
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static IDisposable GraphicsUse(Graphics graphics, GraphicSetting graphicSetting)
        {
            switch (graphicSetting)
            {
                case GraphicSetting.None: return GraphicsUseText(graphics);
                case GraphicSetting.Smooth: return GraphicsUseSmooth(graphics);
                case GraphicSetting.Text: return GraphicsUseText(graphics);
                case GraphicSetting.Sharp: return GraphicsUseSharp(graphics);
            }
            return GraphicsUseText(graphics);
        }
        /// <summary>
        /// Prepare Graphics for Smooth / Text / Sharp drawing, by graphicSetting value.
        /// Return a disposable object, which at its Dispose returns graphics to the original state.
        /// You must use this method in using pattern:
        /// using(Painter.GraphicsUseSmooth(graphics) { any drawing... }.
        /// Set specified rectangle as Clip region to Graphics. On Dispose returns original Clip region.
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static IDisposable GraphicsUse(Graphics graphics, Rectangle setClip, GraphicSetting graphicSetting)
        {
            switch (graphicSetting)
            {
                case GraphicSetting.None: return GraphicsUseText(graphics, setClip);
                case GraphicSetting.Smooth: return GraphicsUseSmooth(graphics, setClip);
                case GraphicSetting.Text: return GraphicsUseText(graphics, setClip);
                case GraphicSetting.Sharp: return GraphicsUseSharp(graphics, setClip);
            }
            return GraphicsUseText(graphics, setClip);
        }
        /// <summary>
        /// Prepare Graphics for smooth drawing (curve, text), quality smooth rendering.
        /// Return a disposable object, which at its Dispose returns graphics to the original state.
        /// You must use this method in using pattern:
        /// using(Painter.GraphicsUseSmooth(graphics) { any drawing... }.
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static IDisposable GraphicsUseSmooth(Graphics graphics)
        {
            IDisposable state = new GraphicsStateRestore(graphics);
            _GraphicsSetSmooth(graphics);
            return state;
        }
        /// <summary>
        /// Prepare Graphics for smooth drawing (curve, text), quality smooth rendering.
        /// Return a disposable object, which at its Dispose returns graphics to the original state.
        /// You must use this method in using pattern:
        /// using(Painter.GraphicsUseSmooth(graphics) { any drawing... }.
        /// Set specified rectangle as Clip region to Graphics. On Dispose returns original Clip region.
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static IDisposable GraphicsUseSmooth(Graphics graphics, Rectangle setClip)
        {
            IDisposable state = new GraphicsStateRestore(graphics, setClip);
            graphics.SetClip(setClip);
            _GraphicsSetSmooth(graphics);
            return state;
        }
        /// <summary>
        /// Prepare Graphics for text drawing (lines, text), fast rendering lines and quality rendering of texts.
        /// Return a disposable object, which at its Dispose returns graphics to the original state.
        /// You must use this method in using pattern:
        /// using(Painter.GraphicsUseSmooth(graphics) { any drawing... }.
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static IDisposable GraphicsUseText(Graphics graphics)
        {
            IDisposable state = new GraphicsStateRestore(graphics);
            _GraphicsSetText(graphics);
            return state;
        }
        /// <summary>
        /// Prepare Graphics for text drawing (lines, text), fast rendering lines and quality rendering of texts.
        /// Return a disposable object, which at its Dispose returns graphics to the original state.
        /// You must use this method in using pattern:
        /// using(Painter.GraphicsUseSmooth(graphics) { any drawing... }.
        /// Set specified rectangle as Clip region to Graphics. On Dispose returns original Clip region.
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static IDisposable GraphicsUseText(Graphics graphics, Rectangle setClip)
        {
            IDisposable state = new GraphicsStateRestore(graphics, setClip);
            graphics.SetClip(setClip);
            _GraphicsSetText(graphics);
            return state;
        }
        /// <summary>
        /// Prepare Graphics for sharp sharp (rectangles), fast rendering.
        /// Return a disposable object, which at its Dispose returns graphics to the original state.
        /// You must use this method in using pattern:
        /// using(Painter.GraphicsUseSharp(graphics) { any drawing... }.
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static IDisposable GraphicsUseSharp(Graphics graphics)
        {
            IDisposable state = new GraphicsStateRestore(graphics);
            _GraphicsSetSharp(graphics);
            return state;
        }
        /// <summary>
        /// Prepare Graphics for sharp sharp (rectangles), fast rendering.
        /// Return a disposable object, which at its Dispose returns graphics to the original state.
        /// You must use this method in using pattern:
        /// using(Painter.GraphicsUseSharp(graphics) { any drawing... }.
        /// Set specified rectangle as Clip region to Graphics. On Dispose returns original Clip region.
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static IDisposable GraphicsUseSharp(Graphics graphics, Rectangle setClip)
        {
            IDisposable state = new GraphicsStateRestore(graphics, setClip);
            graphics.SetClip(setClip);
            _GraphicsSetSharp(graphics);
            return state;
        }
        /// <summary>
        /// Set graphic to smooth mode
        /// </summary>
        /// <param name="graphics"></param>
        private static void _GraphicsSetSmooth(Graphics graphics)
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        }
        /// <summary>
        /// Set graphic to text mode
        /// </summary>
        /// <param name="graphics"></param>
        private static void _GraphicsSetText(Graphics graphics)
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;        // None;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear; // Default;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;     // AntiAliasGridFit;
        }
        /// <summary>
        /// Set graphic to sharp mode
        /// </summary>
        /// <param name="graphics"></param>
        private static void _GraphicsSetSharp(Graphics graphics)
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
        }
        #region class GraphicsStateRestore : Disposable pattern for graphics.Save(), Set, use and Restore on Dispose
        /// <summary>
        /// GraphicsStateRestore : Disposable pattern for graphics.Save(), set, use and Restore on Dispose
        /// </summary>
        private class GraphicsStateRestore : IDisposable
        {
            public GraphicsStateRestore(Graphics graphics)
            {
                this._Graphics = graphics;
                this._State = graphics.Save();
            }
            public GraphicsStateRestore(Graphics graphics, Rectangle setClip)
            {
                this._Graphics = graphics;
                this._State = graphics.Save();
                this._Clip = setClip;
                this._OriginClip = graphics.Clip;
            }
            /// <summary>
            /// Graphics (via WeakReference)
            /// </summary>
            private Graphics _Graphics
            {
                set
                {
                    this.__Graphics = (value == null ? null : new WeakReference(value));
                }
                get
                {
                    if (!this._HasGraphics) return null;
                    return this.__Graphics.Target as Graphics;
                }
            }
            /// <summary>
            /// true when WeakReference to Graphics is valid
            /// </summary>
            private bool _HasGraphics
            {
                get { return (this.__Graphics != null && this.__Graphics.IsAlive && this.__Graphics.Target is Graphics); }
            }
            private WeakReference __Graphics;
            private GraphicsState _State;
            private Rectangle? _Clip;
            private Region _OriginClip;
            void IDisposable.Dispose()
            {
                if (this._State != null && this._HasGraphics)
                {
                    Graphics graphics = this._Graphics;
                    graphics.Restore(this._State);
                    if (this._Clip.HasValue)
                    {
                        if (this._OriginClip != null)
                            graphics.SetClip(this._OriginClip, CombineMode.Replace);
                        else
                            graphics.ResetClip();
                    }
                }
                this._Graphics = null;
                this._State = null;
                this._OriginClip = null;
                this._Clip = null;
            }
        }
        #endregion
        #endregion
        #region Static property, containing standardised graphic tools
        public static Brush InteractiveClipBrushForState(GInteractiveState state)
        {
            switch (state)
            {
                case GInteractiveState.None: return InteractiveClipStandardBrush;
                case GInteractiveState.Enabled: return InteractiveClipStandardBrush;
                case GInteractiveState.Disabled: return InteractiveClipDisabledBrush;
                case GInteractiveState.MouseOver: return InteractiveClipMouseBrush;
                case GInteractiveState.LeftDown: return InteractiveClipDownBrush;
                case GInteractiveState.RightDown: return InteractiveClipDownBrush;
                case GInteractiveState.LeftDrag: return InteractiveClipDragBrush;
                case GInteractiveState.RightDrag: return InteractiveClipDragBrush;
            }
            return InteractiveClipStandardBrush;
        }
        public static Brush InteractiveClipStandardBrush
        {
            get
            {
                if (_InteractiveClipStandardBrush == null)
                    _InteractiveClipStandardBrush = new SolidBrush(InteractiveClipStandardColor);
                return _InteractiveClipStandardBrush;
            }
        } private static Brush _InteractiveClipStandardBrush;
        public static Brush InteractiveClipDisabledBrush
        {
            get
            {
                if (_InteractiveClipDisabledBrush == null)
                    _InteractiveClipDisabledBrush = new SolidBrush(InteractiveClipDisabledColor);
                return _InteractiveClipDisabledBrush;
            }
        } private static Brush _InteractiveClipDisabledBrush;
        public static Brush InteractiveClipMouseBrush
        {
            get
            {
                if (_InteractiveClipMouseBrush == null)
                    _InteractiveClipMouseBrush = new SolidBrush(InteractiveClipMouseColor);
                return _InteractiveClipMouseBrush;
            }
        } private static Brush _InteractiveClipMouseBrush;
        public static Brush InteractiveClipDownBrush
        {
            get
            {
                if (_InteractiveClipDownBrush == null)
                    _InteractiveClipDownBrush = new SolidBrush(InteractiveClipDownColor);
                return _InteractiveClipDownBrush;
            }
        } private static Brush _InteractiveClipDownBrush;
        public static Brush InteractiveClipDragBrush
        {
            get
            {
                if (_InteractiveClipDragBrush == null)
                    _InteractiveClipDragBrush = new SolidBrush(InteractiveClipDragColor);
                return _InteractiveClipDragBrush;
            }
        } private static Brush _InteractiveClipDragBrush;
        public static Color InteractiveClipStandardColor { get { return Color.DimGray; } }
        public static Color InteractiveClipDisabledColor { get { return Color.DimGray; } }
        public static Color InteractiveClipMouseColor { get { return Color.BlueViolet; } }
        public static Color InteractiveClipDownColor { get { return Color.Black; } }
        public static Color InteractiveClipDragColor { get { return Color.Blue; } }
        #endregion
    }
    #region Enums
    public enum GraphicSetting { None, Text, Smooth, Sharp }
    [Flags]
    public enum RelativePosition 
    { 
        None        = 0x00000, 
        Inside      = 0x00001, 
        TopLeft     = 0x00010, 
        Top         = 0x00020, 
        TopRight    = 0x00040, 
        RightTop    = 0x00100, 
        Right       = 0x00200, 
        RightBottom = 0x00400, 
        BottomRight = 0x01000, 
        Bottom      = 0x02000, 
        BottomLeft  = 0x04000, 
        LeftBottom  = 0x10000, 
        Left        = 0x20000, 
        LeftTop     = 0x40000,
        AllTop = TopLeft | Top | TopRight,
        AllRight = RightTop | Right | RightBottom,
        AllBottom = BottomRight | Bottom | BottomLeft,
        AllLeft = LeftBottom | Left | LeftTop
    }
    public enum LinearShapeType { None, LeftArrow, UpArrow, RightArrow, DownArrow, HorizontalLines, VerticalLines }
    #endregion
}
