using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Nástroj pro kreslení
    /// </summary>
    public class GPainter
    {
        #region DrawRectangle
        /// <summary>
        /// Vyplní daný prostor danou barvou.
        /// Používá systémový štětec, nevytváří si nový.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="backColor"></param>
        internal static void DrawRectangle(Graphics graphics, Rectangle bounds, Color backColor)
        {
            graphics.FillRectangle(Skin.Brush(backColor), bounds);
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
        internal static void DrawBorder(Graphics graphics, Rectangle bounds, RectangleSide sides, DashStyle? dashStyle, Color lineColor, float? effect3D)
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
        /// <param name="colorTop"></param>
        /// <param name="colorRight"></param>
        /// <param name="colorBottom"></param>
        /// <param name="colorLeft"></param>
        internal static void DrawBorder(Graphics graphics, Rectangle bounds, RectangleSide sides, DashStyle? dashStyle, Color? colorTop, Color? colorRight, Color? colorBottom, Color? colorLeft)
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
        #region DrawAreaBase
        /// <summary>
        /// Vykreslí podkladovou vrstvu pod button nebo jiný 3D prvek, závislý na interaktivním stavu.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="color"></param>
        /// <param name="state"></param>
        /// <param name="orientation"></param>
        /// <param name="point"></param>
        /// <param name="roundCorner"></param>
        /// <param name="opacity"></param>
        internal static void DrawAreaBase(Graphics graphics, Rectangle absoluteBounds, Color color, Orientation orientation, GInteractiveState state = GInteractiveState.Enabled, Point? point = null, Int32? opacity = null, int? roundCorner = null)
        {
            if (absoluteBounds.Width <= 0 || absoluteBounds.Height <= 0) return;

            using (Brush brush = Skin.CreateBrushForBackground(absoluteBounds, orientation, state, true, color, opacity, point))
            {
                if (roundCorner.HasValue && roundCorner.Value > 0)
                {
                    using (GraphicsPath path = CreatePathRoundRectangle(absoluteBounds, roundCorner.Value, roundCorner.Value))
                    {
                        graphics.FillPath(brush, path);
                    }
                }
                else
                {
                    graphics.FillRectangle(brush, absoluteBounds);
                }
            }
        }
        #endregion
        #region DrawFrameSelect
        /// <summary>
        /// Vykreslí FrameSelect obdélník
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        internal static void DrawFrameSelect(Graphics graphics, Rectangle bounds)
        {
            graphics.FillRectangle(Skin.Brush(Skin.Control.FrameSelectBackColor), bounds);
            graphics.DrawRectangle(Skin.Pen(Skin.Control.FrameSelectLineColor, dashStyle: DashStyle.Dot), bounds);
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
        /// <param name="interactiveState">Interaktvní stav, je z něj odvozena hodnota hodnota 3D efektu (pomocí metody  ).</param>
        /// <param name="force3D">Vynutit 3D efekt i pro "klidový stav prvku" (<see cref="GInteractiveState.None"/> a <see cref="GInteractiveState.Enabled"/>)!</param>
        /// <param name="opacity"></param>
        internal static void DrawEffect3D(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, GInteractiveState? interactiveState, bool? force3D = false, Int32? opacity = null)
        {
            float? effect3D = (interactiveState.HasValue ? (float?)GetEffect3D(interactiveState.Value, force3D) : (float?)null);
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
        /// Hodnota 1.00 vytvoří bílou a černou barvu, hodnota 0.10f vytvoří lehký 3D efekt, 0.50f poměrně silný efekt.</param>  
        /// <param name="opacity"></param>
        internal static void DrawEffect3D(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, float? effect3D, Int32? opacity = null)
        {
            _DrawEffect3D(graphics, bounds, color, orientation, effect3D, opacity);
        }
        /// <summary>
        /// Metoda vrátí hodnotu effect3D pro konkrétní interaktivní stav
        /// </summary>
        /// <param name="interactiveState"></param>
        /// <param name="force3D">Vynutit 3D efekt i pro "klidový stav prvku" (<see cref="GInteractiveState.None"/> a <see cref="GInteractiveState.Enabled"/>)!</param>
        /// <returns></returns>
        internal static float? GetEffect3D(GInteractiveState interactiveState, bool? force3D = false)
        {
            switch (interactiveState)
            {
                case GInteractiveState.Disabled: return 0f;
                case GInteractiveState.None: return 0.25f;
                case GInteractiveState.LeftFrame:
                case GInteractiveState.RightFrame:
                case GInteractiveState.Enabled: return 0.25f;
                case GInteractiveState.MouseOver: return 0.50f;
                case GInteractiveState.LeftDown:
                case GInteractiveState.RightDown: return -0.35f;
                case GInteractiveState.LeftDrag:
                case GInteractiveState.RightDrag: return -0.15f;
            }
            return null;
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
        /// Hodnota 1.00 vytvoří bílou a černou barvu, hodnota 0.10f vytvoří lehký 3D efekt, 0.50f poměrně silný efekt.</param> 
        /// <param name="opacity"></param>
        private static void _DrawEffect3D(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, float? effect3D, Int32? opacity)
        {
            if (!bounds.HasPixels()) return;
            Color color1, color2;
            if (effect3D.HasValue && effect3D.Value != 0f && CreateEffect3DColors(color, effect3D, out color1, out color2))
            {   // 3D efekt:
                if (opacity.HasValue)
                {
                    color1 = Color.FromArgb(opacity.Value, color1);
                    color2 = Color.FromArgb(opacity.Value, color2);
                }
                using (Brush brush = Skin.CreateBrushForBackgroundGradient(bounds, orientation, color1, color2))
                {
                    graphics.FillRectangle(brush, bounds);
                }
            }
            else
            {   // Plná plochá barva:
                if (opacity.HasValue)
                    color = Color.FromArgb(opacity.Value, color);
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
        internal static bool CreateEffect3DColors(Color color, float? effect3D, out Color color1, out Color color2)
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
        internal static void DrawButtonBase(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, Point? point, Int32? opacity)
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
        internal static void DrawButtonBase(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, int roundCorner, Point? point, Int32? opacity)
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
        internal static void DrawButtonBase(Graphics graphics, Rectangle bounds, Color color, GInteractiveState state, Orientation orientation, Point? point, Int32? opacity)
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
        internal static void DrawButtonBase(Graphics graphics, Rectangle bounds, Color color, GInteractiveState state, Orientation orientation, int roundCorner, Point? point, Int32? opacity)
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
        internal static void DrawButtonBase(Graphics graphics, Rectangle bounds, Color color, GInteractiveState state, Orientation orientation, int roundCorner, Point? point, Int32? opacity, bool drawBackground, bool drawBorders)
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
        internal static void DrawButtonBase(Graphics graphics, Rectangle bounds, Color color, GInteractiveState state, Orientation orientation, int roundCorner, Point? point, Int32? opacity, bool drawBackground, bool drawBorders, Color colorBorder)
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
        internal static void DrawString(Graphics graphics, Rectangle bounds, string text, Color color, FontInfo fontInfo, ContentAlignment alignment)
        {
            Rectangle textArea;
            _DrawString(graphics, bounds, text, null, color, fontInfo, alignment, MatrixTransformationType.NoTransform, null, out textArea);
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
        /// <param name="transformation"></param>
        internal static void DrawString(Graphics graphics, Rectangle bounds, string text, Color color, FontInfo fontInfo, ContentAlignment alignment, MatrixTransformationType transformation)
        {
            Rectangle textArea;
            _DrawString(graphics, bounds, text, null, color, fontInfo, alignment, transformation, null, out textArea);
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
        internal static void DrawString(Graphics graphics, Rectangle bounds, string text, Color color, FontInfo fontInfo, ContentAlignment alignment, Action<Rectangle> drawBackground)
        {
            Rectangle textArea;
            _DrawString(graphics, bounds, text, null, color, fontInfo, alignment, MatrixTransformationType.NoTransform, drawBackground, out textArea);
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
        internal static void DrawString(Graphics graphics, Rectangle bounds, string text, Brush brush, FontInfo fontInfo, ContentAlignment alignment)
        {
            Rectangle textArea;
            _DrawString(graphics, bounds, text, brush, null, fontInfo, alignment, MatrixTransformationType.NoTransform, null, out textArea);
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
        internal static void DrawString(Graphics graphics, Rectangle bounds, string text, Brush brush, FontInfo fontInfo, ContentAlignment alignment, Action<Rectangle> drawBackground)
        {
            Rectangle textArea;
            _DrawString(graphics, bounds, text, brush, null, fontInfo, alignment, MatrixTransformationType.NoTransform, drawBackground, out textArea);
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
        internal static void DrawString(Graphics graphics, Rectangle bounds, string text, Color color, FontInfo fontInfo, ContentAlignment alignment, out Rectangle textArea)
        {
            _DrawString(graphics, bounds, text, null, color, fontInfo, alignment, MatrixTransformationType.NoTransform, null, out textArea);
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
        internal static void DrawString(Graphics graphics, Rectangle bounds, string text, Color color, FontInfo fontInfo, ContentAlignment alignment, Action<Rectangle> drawBackground, out Rectangle textArea)
        {
            _DrawString(graphics, bounds, text, null, color, fontInfo, alignment, MatrixTransformationType.NoTransform, drawBackground, out textArea);
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
        internal static void DrawString(Graphics graphics, Rectangle bounds, string text, Brush brush, FontInfo fontInfo, ContentAlignment alignment, out Rectangle textArea)
        {
            _DrawString(graphics, bounds, text, brush, null, fontInfo, alignment, MatrixTransformationType.NoTransform, null, out textArea);
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
        internal static void DrawString(Graphics graphics, Rectangle bounds, string text, Brush brush, FontInfo fontInfo, ContentAlignment alignment, Action<Rectangle> drawBackground, out Rectangle textArea)
        {
             _DrawString(graphics, bounds, text, brush, null, fontInfo, alignment, MatrixTransformationType.NoTransform, drawBackground, out textArea);
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
        private static void _DrawString(Graphics graphics, Rectangle bounds, string text, Brush brush, Color? color, FontInfo fontInfo, ContentAlignment alignment, MatrixTransformationType transformation, Action<Rectangle> drawBackground, out Rectangle textArea)
        {
            textArea = new Rectangle(bounds.X, bounds.Y, 0, 0);           // out parametr
            if (fontInfo == null || String.IsNullOrEmpty(text)) return;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            bool isVertical = (transformation == MatrixTransformationType.Rotate90 || transformation == MatrixTransformationType.Rotate270);
            int boundsLength = (isVertical ? bounds.Height : bounds.Width);

            using (GraphicsUseText(graphics))
            {
                Font font = fontInfo.Font;
                StringFormat sFormat = new StringFormat(StringFormatFlags.NoClip);
                // sFormat = new StringFormat(StringFormatFlags.NoClip);
                SizeF textSize = graphics.MeasureString(text, font, boundsLength, sFormat);
                if (isVertical) textSize = textSize.Swap();               // Pro vertikální text převedu prostor textu "na výšku"
                textArea = textSize.AlignTo(bounds, alignment, true);     // Zarovnám oblast textu do přiděleného prostoru dle zarovnání

                if (drawBackground != null)
                    drawBackground(textArea);                             // UserDraw pozadí pod textem: v nativní orientaci

                Matrix matrixOld = null;
                if (isVertical)
                {
                    textArea = textArea.Swap();                           // Pro vertikální text převedu prostor textu "na šířku", protože otáčet "na výšku" ho bude Matrix aplikovaný do Graphics
                    matrixOld = graphics.Transform;
                    graphics.Transform = GetMatrix(transformation, textArea);
                }

                if (brush != null)
                    graphics.DrawString(text, font, brush, textArea, sFormat);
                else if (color.HasValue)
                    graphics.DrawString(text, font, Skin.Brush(color.Value), textArea, sFormat);
                else
                    graphics.DrawString(text, font, SystemBrushes.ControlText, textArea, sFormat);

                if (isVertical)
                {
                    graphics.Transform = matrixOld;
                }
            }
        }
        #endregion
        #region MeasureString
        /// <summary>
        /// Metoda vrátí rozměry daného textu v daném fontu, rozměr odhadne jen podle vlastností fontu.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fontInfo"></param>
        /// <returns></returns>
        internal static Size MeasureString(string text, FontInfo fontInfo)
        {
            if (String.IsNullOrEmpty(text)) return new Size(0, 0);

            Font font = fontInfo.Font;
            int height = font.Height;
            float width = font.Size * (float)text.Length + 6f;
            return new Size((int)width, height);
        }
        /// <summary>
        /// Metoda změří daný text v daném fontu a v dané grafice.
        /// Pokud je grafika null, pak text změří jen odhadem.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="text"></param>
        /// <param name="fontInfo"></param>
        /// <returns></returns>
        internal static Size MeasureString(Graphics graphics, string text, FontInfo fontInfo)
        {
            if (String.IsNullOrEmpty(text)) return new Size(0, 0);
            if (graphics == null) return MeasureString(text, fontInfo);

            Font font = fontInfo.Font;
            SizeF sizeF = graphics.MeasureString(text, font);
            Size size = sizeF.Enlarge(1f, 3f).ToSize();
            return size;
        }
        #endregion
        #region DrawRadiance
        internal static void DrawRadiance(Graphics graphics, Point center, Color centerColor)
        {
            DrawRadiance(graphics, center, null, centerColor);
        }
        internal static void DrawRadiance(Graphics graphics, Point center, Rectangle? clipBounds, Color centerColor)
        {
            Rectangle bounds = center.CreateRectangleFromCenter(new Size(45, 30));
            DrawRadiance(graphics, bounds, clipBounds, centerColor);
        }
        internal static void DrawRadiance(Graphics graphics, Point center, Size size, Color centerColor)
        {
            DrawRadiance(graphics, center, size, null, centerColor);
        }
        internal static void DrawRadiance(Graphics graphics, Point center, Size size, Rectangle? clipBounds, Color centerColor)
        {
            Rectangle bounds = center.CreateRectangleFromCenter(size);
            DrawRadiance(graphics, bounds, clipBounds, centerColor);
        }
        internal static void DrawRadiance(Graphics graphics, Rectangle bounds, Color centerColor)
        {
            DrawRadiance(graphics, bounds, null, centerColor);
        }
        internal static void DrawRadiance(Graphics graphics, Rectangle bounds, Rectangle? clipBounds, Color centerColor)
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
        internal static void DrawGridHeader(Graphics graphics, Rectangle bounds, RectangleSide side, Color backColor, bool draw3D, Color? lineColor, GInteractiveState state, Orientation orientation, Point? relativePoint, Int32? opacity)
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
                float? effect3D = GetEffect3D(state, true);
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
        #endregion
        #region DrawWindow
        internal static void DrawWindow(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, Int32? opacity)
        {
            _DrawWindow(graphics, bounds, color, orientation, opacity, null, null);
        }
        internal static void DrawWindow(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, Int32? opacity, Int32? borderX, Int32? borderY)
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
        #region DrawTrackTicks
        internal static void DrawTrackTicks(Graphics graphics, Rectangle bounds, Orientation orientation, int? tickNumber = null)
        {
            switch (orientation)
            {
                case Orientation.Horizontal:
                    _DrawTrackTicksHorizontal(graphics, bounds, tickNumber);
                    break;
                case Orientation.Vertical:
                    _DrawTrackTicksVertical(graphics, bounds, tickNumber);
                    break;
            }
        }
        private static void _DrawTrackTicksHorizontal(Graphics graphics, Rectangle bounds, int? tickNumber)
        {
            Point trackCenter = bounds.Center();
            int x0 = bounds.X;
            int x9 = bounds.Right - 1;
            int y0 = bounds.Y;
            int y1 = y0 + 2;
            int y5 = bounds.Center().Y;
            int y4 = y5 - 1;
            int y6 = y5 + 1;
            int y9 = bounds.Bottom - 1;
            int y8 = y9 - 2;

            Color colorTrack = Skin.TrackBar.LineColorTrack;
            Color colorTick = Skin.TrackBar.LineColorTick;
            Pen pen = Skin.Pen(colorTick);
            if (tickNumber.HasValue && tickNumber.Value > 0)
            {
                decimal length = bounds.Width;
                decimal step = length / (int)tickNumber.Value;
                if (step < 3m) step = 3m;
                for (decimal tick = bounds.X; tick < x9; tick += step)
                {
                    int x = (int)Math.Round(tick, 0);
                    graphics.DrawLine(pen, x, y1, x, y8);
                }
            }

            pen = Skin.Pen(colorTick);
            graphics.DrawLine(pen, x0, y4, x9, y4);
            graphics.DrawLine(pen, x0, y6, x9, y6);

            pen = Skin.Pen(colorTrack);
            graphics.DrawLine(pen, x0, y0, x0, y9);
            graphics.DrawLine(pen, x9, y0, x9, y9);
            graphics.DrawLine(pen, x0, y5, x9, y5);
        }
        private static void _DrawTrackTicksVertical(Graphics graphics, Rectangle bounds, int? tickNumber = null)
        {

        }
        #endregion
        #region DrawTrackPointer
        internal static void DrawTrackPointer(Graphics graphics, Rectangle bounds, GInteractiveState state, TrackPointerType pointerType, RectangleSide pointerSide)
        {
            DrawTrackPointer(graphics, bounds.Center(), bounds.Size, state, pointerType, pointerSide);
        }
        internal static void DrawTrackPointer(Graphics graphics, Point center, Size size, GInteractiveState state, TrackPointerType pointerType, RectangleSide pointerSide, int? opacity = null)
        {
            Orientation orientation = ((pointerSide.HasFlag(RectangleSide.Top) || pointerSide.HasFlag(RectangleSide.Bottom)) ? Orientation.Horizontal : Orientation.Vertical);
            GraphicSetting graphicSetting;

            if (true)
            {
                using (GraphicsPath path = GPainter.CreatePathTrackPointer(center, size, pointerType, pointerSide, GraphicsPathPart.FilledArea, out graphicSetting))
                {
                    Rectangle bounds = center.CreateRectangleFromCenter(size);
                    using (Brush brush = Skin.CreateBrushForBackground(bounds, orientation, state, Skin.TrackBar.BackColorButton))
                    using (GPainter.GraphicsUse(graphics, graphicSetting))
                    {
                        graphics.FillPath(brush, path);
                    }
                }
            }
            if (true)
            {
                using (GraphicsPath path = GPainter.CreatePathTrackPointer(center, size, pointerType, pointerSide, GraphicsPathPart.LightBorder, out graphicSetting))
                {
                    Color lightBorder = Skin.Modifiers.GetColor3DBorderLight(Skin.TrackBar.LineColorButton);
                    using (GPainter.GraphicsUse(graphics, graphicSetting))
                    {
                        graphics.DrawPath(Skin.Pen(lightBorder), path);
                    }
                }
            }
            if (true)
            {
                using (GraphicsPath path = GPainter.CreatePathTrackPointer(center, size, pointerType, pointerSide, GraphicsPathPart.DarkBorder, out graphicSetting))
                {
                    Color darkBorder = Skin.Modifiers.GetColor3DBorderDark(Skin.TrackBar.LineColorButton);
                    using (GPainter.GraphicsUse(graphics, graphicSetting))
                    {
                        graphics.DrawPath(Skin.Pen(darkBorder), path);
                    }
                }
            }
        }
        /// <summary>
        /// Vrátí GraphicsPath pro definovaný tvar, k danému středu a velikosti.
        /// Vrátí null pro malé rozměry (menší než 8 px) anebo pro typ pointerType = <see cref="TrackPointerType.None"/>.
        /// </summary>
        /// <returns></returns>
        internal static GraphicsPath CreatePathTrackPointer(Point center, Size size, TrackPointerType pointerType, RectangleSide pointerSide, GraphicsPathPart pathPart)
        {
            GraphicSetting graphicSetting;
            return CreatePathTrackPointer(center, size, pointerType, pointerSide, pathPart, out graphicSetting);
        }
        /// <summary>
        /// Vrátí GraphicsPath pro definovaný tvar, k danému středu a velikosti.
        /// Vrátí null pro malé rozměry (menší než 8 px) anebo pro typ pointerType = <see cref="TrackPointerType.None"/>.
        /// Vloží optimální GraphicSetting pro kreslení tohoto tvaru do out parametru graphicSetting.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <param name="pointerType"></param>
        /// <param name="pointerSide"></param>
        /// <param name="graphicSetting"></param>
        /// <returns></returns>
        internal static GraphicsPath CreatePathTrackPointer(Point center, Size size, TrackPointerType pointerType, RectangleSide pointerSide, GraphicsPathPart pathPart, out GraphicSetting graphicSetting)
        {
            graphicSetting = GraphicSetting.None;
            if (pointerType == TrackPointerType.None) return null;
            if (pathPart == GraphicsPathPart.None) return null;
            if (size.Width < 8 || size.Height < 8) return null;
            bool isVertical = _CreatePathTrackPointerIsVertical(pointerSide, size);
            switch (pointerType)
            {
                case TrackPointerType.OneSide: return (isVertical ?
                        _CreatePathTrackPointerOneSideVertical(center, size, pointerSide, pathPart, out graphicSetting) :
                        _CreatePathTrackPointerOneSideHorizontal(center, size, pointerSide, pathPart, out graphicSetting));
                case TrackPointerType.DoubleSide:
                    return (isVertical ?
                        _CreatePathTrackPointerDoubleSideVertical(center, size, pointerSide, pathPart, out graphicSetting) :
                        _CreatePathTrackPointerDoubleSideHorizontal(center, size, pointerSide, pathPart, out graphicSetting));
                case TrackPointerType.HiFi:
                    return (isVertical ?
                        _CreatePathTrackPointerHiFiVertical(center, size, pointerSide, pathPart, out graphicSetting) :
                        _CreatePathTrackPointerHiFiHorizontal(center, size, pointerSide, pathPart, out graphicSetting));
            }
            return null;
        }
        /// <summary>
        /// Vrátí true, pokud orientace pointeru je svislá (tzn. jezdí zleva doprava, a ukazuje nahoru nebo dolů)
        /// </summary>
        /// <param name="pointerSide"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private static bool _CreatePathTrackPointerIsVertical(RectangleSide pointerSide, Size size)
        {
            if (pointerSide.HasFlag(RectangleSide.Top) || pointerSide.HasFlag(RectangleSide.Bottom)) return true;
            if (pointerSide.HasFlag(RectangleSide.Left) || pointerSide.HasFlag(RectangleSide.Right)) return false;
            return (size.Height > size.Width);
        }
        private static GraphicsPath _CreatePathTrackPointerOneSideVertical(Point center, Size size, RectangleSide pointerSide, GraphicsPathPart pathPart, out GraphicSetting graphicSetting)
        {
            bool addSimple = pathPart.HasFlag(GraphicsPathPart.SimpleBorder);
            bool addLight = pathPart.HasFlag(GraphicsPathPart.LightBorder);
            bool addDark = pathPart.HasFlag(GraphicsPathPart.DarkBorder);
            bool isFill = pathPart.HasFlag(GraphicsPathPart.FilledArea);
            bool sideTL = pointerSide.HasFlag(RectangleSide.Top);
            bool sideBR = pointerSide.HasFlag(RectangleSide.Bottom);

            int cx = center.X;
            int cy = center.Y;

            int lf = size.Height;                // Plná délka pointeru = výška daného prostoru
            int sf = size.Width;                 // Plná šířka pointeru = šířka daného prostoru
            int sm = lf / 2;                     // Nejvyšší použitelná šíře pointeru = 1/2 délky (výšky)
            if (sf > sm) sf = sm;

            int s3 = sf / 2;                     // Polo-šířka pointeru (vzdálenost boku od středu na ose S (vertical = X)
            int s2 = s3 - 1;

            int l3 = lf / 2;                     // Vzdálenost špičky pointeru od středu, na ose L (vertical = Y)
            int l2 = l3 - 1;
            int l1 = l3 - s3;                    // Pozice konce šipky

            GraphicsPath path = new GraphicsPath();

            path.StartFigure();
            if (addSimple || addLight || isFill)
            {   // Světlá část = vlevo a nahoře:
                path.AddLines(new Point[]
                {
                    new Point(cx - s3, cy + (sideBR ? l1 : l2)),
                    new Point(cx - s3, cy - (sideTL ? l1 : l2)),
                    new Point(cx - (sideTL ? 0 : s2), cy - l3),
                    new Point(cx + (sideTL ? 0 : s2), cy - l3),
                    new Point(cx + s3, cy - (sideTL ? l1 : l2))
                });
            }
            if (addSimple || addDark || isFill)
            {   // Tmavá část = vpravo a dole:
                path.AddLines(new Point[]
                {
                    new Point(cx + s3, cy - (sideTL ? l1 : l2)),
                    new Point(cx + s3, cy + (sideBR ? l1 : l2)),
                    new Point(cx + (sideBR ? 0 : s2), cy + l3),
                    new Point(cx - (sideBR ? 0 : s2), cy + l3),
                    new Point(cx - s3, cy + (sideBR ? l1 : l2))
                });
            }
            if (isFill)
                path.CloseFigure();

            if (!isFill)
            {
                int lt = cy - l1 + (sideTL ? 1 : 0);
                int lb = cy + l1 - (sideBR ? 1 : 0);
                bool isDouble = (s3 > 3);
                if (addSimple)
                {   // Jednoduché linky:
                    if (isDouble)
                        _CreatePathTrackPointerOneSideVerticalLines(path, lt, lb, cx - 1, cx + 1);
                    else
                        _CreatePathTrackPointerOneSideVerticalLines(path, lt, lb, cx);
                }
                if (addLight)
                {   // Pouze světlá část (tj. ne výplň) = svislé linky více vlevo:
                    if (isDouble)
                        _CreatePathTrackPointerOneSideVerticalLines(path, lt, lb, cx - 2, cx + 1);
                    // else
                    //     _CreatePathTrackPointerOneSideVerticalLines(path, lt, lb, cx - 1);
                }
                if (addDark)
                {   // Pouze tmavá část (tj. ne výplň) = tmavé linky více vpravo:
                    if (isDouble)
                        _CreatePathTrackPointerOneSideVerticalLines(path, lt, lb, cx - 1, cx + 2);
                    else
                        _CreatePathTrackPointerOneSideVerticalLines(path, lt, lb, cx + 0);
                }
            }

            graphicSetting = GraphicSetting.Sharp;
            return path;
        }
        private static void _CreatePathTrackPointerOneSideVerticalLines(GraphicsPath path, int yt, int yb, params int[] xs)
        {
            foreach (int x in xs)
            {
                path.StartFigure();
                path.AddLines(new Point[] { new Point(x, yt), new Point(x, yb) });
                path.CloseFigure();
            }
        }
        private static GraphicsPath _CreatePathTrackPointerOneSideHorizontal(Point center, Size size, RectangleSide pointerSide, GraphicsPathPart pathPart, out GraphicSetting graphicSetting)
        {
            GraphicsPath path = new GraphicsPath();
            graphicSetting = GraphicSetting.Smooth;
            return path;
        }
        private static GraphicsPath _CreatePathTrackPointerDoubleSideVertical(Point center, Size size, RectangleSide pointerSide, GraphicsPathPart pathPart, out GraphicSetting graphicSetting)
        {
            GraphicsPath path = new GraphicsPath();
            graphicSetting = GraphicSetting.Smooth;
            return path;
        }
        private static GraphicsPath _CreatePathTrackPointerDoubleSideHorizontal(Point center, Size size, RectangleSide pointerSide, GraphicsPathPart pathPart, out GraphicSetting graphicSetting)
        {
            GraphicsPath path = new GraphicsPath();
            graphicSetting = GraphicSetting.Smooth;
            return path;
        }
        private static GraphicsPath _CreatePathTrackPointerHiFiVertical(Point center, Size size, RectangleSide pointerSide, GraphicsPathPart pathPart, out GraphicSetting graphicSetting)
        {
            GraphicsPath path = new GraphicsPath();
            graphicSetting = GraphicSetting.Smooth;
            return path;

        }
        private static GraphicsPath _CreatePathTrackPointerHiFiHorizontal(Point center, Size size, RectangleSide pointerSide, GraphicsPathPart pathPart, out GraphicSetting graphicSetting)
        {
            GraphicsPath path = new GraphicsPath();
            graphicSetting = GraphicSetting.Smooth;
            return path;
            /*
            int cx = center.X;
            int cy = center.Y;
            int dx1 = 6;             // X pro pozice: 10 h, 8 h, 4 h, 2 h, 10 h
            int dx2 = 6;             // X pro pozice: 9 h, 3 h
            int dx3 = 3;             // X pro pozice: 7 h, 5 h, 1 h, 11 h
            int dy1 = 5;             // Y pro pozice: 10 h, 8 h, 4 h, 2 h, 10 h
            int dy2 = 7;             // Y pro pozice: 7 h, 5 h, 1 h, 11 h
            int dy3 = 6;             // Y pro pozice: 6 h, 12 h
            System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
            gp.AddLines(new Point[]
            {   // Souřadnic je 12 jako hodin na ciferníku :
                        new Point(cx - dx1, cy - dy1),         // 10 h
                        new Point(cx - dx2, cy),               //  9 h
                        new Point(cx - dx1, cy + dy1),         //  8 h
                        new Point(cx - dx3, cy + dy2),         //  7 h
                        new Point(cx, cy + dy3),               //  6 h
                        new Point(cx + dx3, cy + dy2),         //  5 h
                        new Point(cx + dx1, cy + dy1),         //  4 h
                        new Point(cx + dx2, cy),               //  3 h
                        new Point(cx + dx1, cy - dy1),         //  2 h
                        new Point(cx + dx3, cy - dy2),         //  1 h
                        new Point(cx, cy - dy3),               // 12 h
                        new Point(cx - dx3, cy - dy2),         // 11 h
                        new Point(cx - dx1, cy - dy1)          // 10 h
            });
            gp.CloseFigure();

            gp.AddLines(new Point[] { new Point(cx - 1, cy - 3), new Point(cx - 1, cy + 3) });
            gp.CloseFigure();

            gp.AddLines(new Point[] { new Point(cx + 1, cy - 3), new Point(cx + 1, cy + 3) });
            gp.CloseFigure();

            e.Graphics.FillPath(Skin.Brush(Color.LightBlue), gp);
            */
        }

        /*
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
        */
        #endregion
        #region DrawImage
        /// <summary>
        /// Vykreslí daný Image, pokud isEnabled je false, pak bude Image modifikovaný do šedé barvy
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="image"></param>
        /// <param name="isEnabled"></param>
        /// <param name="alignment"></param>
        internal static void DrawImage(Graphics graphics, Rectangle bounds, Image image, bool isEnabled = true, ContentAlignment? alignment = null)
        {
            System.Drawing.Imaging.ColorMatrix colorMatrix = (isEnabled ? null : CreateColorMatrixAlpha(0.45f));
            _DrawImage(graphics, bounds, image, colorMatrix, alignment);
        }
        /// <summary>
        /// Vykreslí daný Image s danou úrovní průhlednosti
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="image"></param>
        /// <param name="opacityRatio"></param>
        /// <param name="alignment"></param>
        internal static void DrawImage(Graphics graphics, Rectangle bounds, Image image, float opacityRatio, ContentAlignment? alignment = null)
        {
            System.Drawing.Imaging.ColorMatrix colorMatrix = CreateColorMatrixAlpha(opacityRatio);
            _DrawImage(graphics, bounds, image, colorMatrix, alignment);
        }
        /// <summary>
        /// Vykreslí daný Image s aplikací barevného posunu dle interaktivního stavu
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="image"></param>
        /// <param name="state"></param>
        /// <param name="alignment"></param>
        internal static void DrawImage(Graphics graphics, Rectangle bounds, Image image, GInteractiveState? state = null, ContentAlignment? alignment = null)
        {
            System.Drawing.Imaging.ColorMatrix colorMatrix = ((state.HasValue && state.Value != GInteractiveState.Enabled) ? CreateColorMatrixForState(state.Value) : null);
            _DrawImage(graphics, bounds, image, colorMatrix, alignment);
        }
        private static void _DrawImage(Graphics graphics, Rectangle bounds, Image image, System.Drawing.Imaging.ColorMatrix colorMatrix, ContentAlignment? alignment)
        {
            if (image == null || bounds.Width <= 0 || bounds.Height <= 0) return;

            Size imageSize = image.Size;
            Rectangle imageBounds = (alignment.HasValue ? imageSize.AlignTo(bounds, alignment.Value, true) : bounds);
            if (colorMatrix != null)
            {   // Vykreslení s barevnou transformací:
                using (System.Drawing.Imaging.ImageAttributes imageAttributes = new System.Drawing.Imaging.ImageAttributes())
                {
                    imageAttributes.SetColorMatrix(colorMatrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);
                    graphics.DrawImage(image, imageBounds, 0, 0, imageSize.Width, imageSize.Height, GraphicsUnit.Pixel, imageAttributes);
                }
            }
            else
            {   // Normální vykreslení, bez barevného posunu:
                graphics.DrawImage(image, imageBounds);
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
        internal static void DrawAxisBackground(Graphics graphics, Rectangle bounds, Orientation orientation, bool enabled, GInteractiveState state, Color color, float morph)
        {
            if (!enabled)
                state = GInteractiveState.Disabled;
            else if (state == GInteractiveState.LeftDrag)
                state = GInteractiveState.LeftDown;
            GPainter.DrawAreaBase(graphics, bounds, color, orientation, state, null, null, 0);
        }
        /// <summary>
        /// Zajistí vykreslení jednoho daného ticku
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="tickLevel"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="lineColorMain"></param>
        /// <param name="lineColorSmall"></param>
        /// <param name="showSmallSize"></param>
        internal static void DrawAxisTick(Graphics graphics, AxisTickType tickLevel, int x0, int y0, int x1, int y1, Color lineColorMain, Color lineColorSmall, bool showSmallSize)
        {
            Pen pen = null;
            bool std = !showSmallSize;
            switch (tickLevel)
            {
                case AxisTickType.OuterLabel:
                    break;
                case AxisTickType.BigLabel:
                    pen = (std ? Skin.Pen(lineColorMain, 2f, dashStyle: DashStyle.Solid) : Skin.Pen(lineColorMain, 1f, dashStyle: DashStyle.Solid));
                    break;
                case AxisTickType.StdLabel:
                    pen = (std ? Skin.Pen(lineColorMain, 1f, dashStyle: DashStyle.Solid) : Skin.Pen(lineColorSmall, 1f, dashStyle: DashStyle.Solid));
                    break;
                case AxisTickType.BigTick:
                    pen = (std ? Skin.Pen(lineColorSmall, 1f, dashStyle: DashStyle.Solid) : Skin.Pen(lineColorSmall, 1f, dashStyle: DashStyle.Dot));
                    break;
                case AxisTickType.StdTick:
                    pen = (std ? Skin.Pen(lineColorSmall, 1f, dashStyle: DashStyle.Dot) : Skin.Pen(lineColorSmall, 1f, dashStyle: DashStyle.Dot));
                    break;
                case AxisTickType.Pixel:
                    break;

            }
            if (pen != null)
                graphics.DrawLine(pen, x0, y0, x1, y1);
        }
        #endregion
        #region DrawRelationGrid
        /// <summary>
        /// Metoda vykreslí linku na spodním okraji daného prostoru, podle pravidel pro Grid
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="width"></param>
        /// <param name="fading"></param>
        internal static void DrawRelationGrid(Graphics graphics, Rectangle bounds, Color? color = null, int? width = null, float? fading = null)
        {
            int w = (width.HasValue ? width.Value : Skin.Relation.LineHeightInGrid);
            Color c = (color.HasValue ? color.Value : Skin.Relation.LineColorInGrid);
            float f = (fading.HasValue ? fading.Value : Skin.Relation.LineFadingRatio);
            bounds.Height = bounds.Height - 2;
            _DrawRelationGrid(graphics, bounds, c, w, f);
        }
        private static void _DrawRelationGrid(Graphics graphics, Rectangle bounds, Color color, int width, float fading)
        {
            width = (width < 0 ? 0 : (width > 6 ? 6 : width));
            if (width == 0) return;
            fading = (fading < 0f ? 0f : (fading > 1f ? 1f : fading));

            Rectangle boundsLine = new Rectangle(bounds.X + 1, bounds.Bottom - width, bounds.Width - 3, width);
            if (boundsLine.Width <= 0) return;

            if (fading == 0f)
            {
                graphics.FillRectangle(Skin.Brush(color), boundsLine);
            }
            else
            {
                int alpha = (int)(255f * (1f - fading));
                Color colorF = Color.FromArgb(alpha, color);
                Rectangle boundsBrush = boundsLine.Enlarge(1, 0, 0, 0);
                using (LinearGradientBrush lgb = new LinearGradientBrush(boundsBrush, color, colorF, 0f))
                {
                    graphics.FillRectangle(lgb, boundsLine);
                }

            }

        }
        #endregion
        #region DrawScrollBar
        /// <summary>
        /// Vykreslí ScrollBar
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="scrollBar"></param>
        internal static void DrawScrollBar(Graphics graphics, Rectangle absoluteBounds, IScrollBarPaintData scrollBar)
        {
            Point location = absoluteBounds.Location;
            Orientation orientation = scrollBar.Orientation;
            bool isEnabled = scrollBar.IsEnabled;

            // Pozadí:
            _DrawScrollBarBack(graphics, absoluteBounds, orientation, isEnabled, scrollBar.ScrollBarBackColor);

            // Prostor Data (mezi Min a Max buttonem, pod Thumbem), plus UserDataDraw method:
            _DrawScrollBarData(graphics, scrollBar.DataAreaBounds.Add(location), orientation, isEnabled, scrollBar.ScrollBarBackColor, scrollBar.UserDataDraw);

            // Aktivní prostor Min/Max area (prostor pro kliknutí mezi Thumb a Min/Max Buttonem):
            if (scrollBar.MinAreaState.IsMouseActive())
                _DrawScrollBarActiveArea(graphics, scrollBar.MinAreaBounds.Add(location), orientation, isEnabled, scrollBar.MinAreaState);
            else if (scrollBar.MaxAreaState.IsMouseActive())
                _DrawScrollBarActiveArea(graphics, scrollBar.MaxAreaBounds.Add(location), orientation, isEnabled, scrollBar.MaxAreaState);

            // Buttony:
            _DrawScrollBarButton(graphics, scrollBar.MinButtonBounds.Add(location), orientation, isEnabled, scrollBar.MinButtonState, true, LinearShapeType.LeftArrow, LinearShapeType.UpArrow);
            _DrawScrollBarButton(graphics, scrollBar.MaxButtonBounds.Add(location), orientation, isEnabled, scrollBar.MaxButtonState, true, LinearShapeType.RightArrow, LinearShapeType.DownArrow);
            if (isEnabled)
                _DrawScrollBarButton(graphics, scrollBar.ThumbButtonBounds.Add(location), orientation, isEnabled, scrollBar.ThumbButtonState, false, LinearShapeType.HorizontalLines, LinearShapeType.VerticalLines);

        }
        /// <summary>
        /// Vykreslí základní pozadí pod celý ScrollBar
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="orientation"></param>
        /// <param name="isEnabled"></param>
        private static void _DrawScrollBarBack(Graphics graphics, Rectangle bounds, Orientation orientation, bool isEnabled, Color backColor)
        {
            if (!isEnabled)
                backColor = backColor.Morph(Skin.Modifiers.BackColorDisable, 0.35f);

            graphics.FillRectangle(Skin.Brush(backColor), bounds);
        }
        /// <summary>
        /// Vykreslí prostor Data (mezi MinButton a MaxButton), nehledí na interaktivní stav
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="orientation"></param>
        /// <param name="isEnabled"></param>
        /// <param name="itemState"></param>
        private static void _DrawScrollBarData(Graphics graphics, Rectangle bounds, Orientation orientation, bool isEnabled, Color backColor, Action<Graphics, Rectangle> userDataDraw)
        {
            // GInteractiveState itemState = (isEnabled ? GInteractiveState.Enabled : GInteractiveState.Disabled);
            // GPainter.DrawAreaBase(graphics, bounds, Skin.ScrollBar.BackColorArea, itemState, orientation, null, null);

            if (!isEnabled)
                backColor = backColor.Morph(Skin.Modifiers.BackColorDisable, 0.35f);

            graphics.FillRectangle(Skin.Brush(backColor), bounds);
            if (userDataDraw != null)
                userDataDraw(graphics, bounds);
        }
        /// <summary>
        /// Vykreslí prostor MinArea / MaxArea
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="orientation"></param>
        /// <param name="isEnabled"></param>
        /// <param name="itemState"></param>
        private static void _DrawScrollBarActiveArea(Graphics graphics, Rectangle bounds, Orientation orientation, bool isEnabled, GInteractiveState itemState)
        {
            GPainter.DrawAreaBase(graphics, bounds, Skin.ScrollBar.BackColorArea, orientation, itemState, null, 96);
        }
        /// <summary>
        /// Vykreslí button pro ScrollBar a do něj jeho grafiku
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="orientation"></param>
        /// <param name="isEnabled"></param>
        /// <param name="itemState"></param>
        /// <param name="shapeHorizontal"></param>
        /// <param name="shapeVertical"></param>
        private static void _DrawScrollBarButton(Graphics graphics, Rectangle bounds, Orientation orientation, bool isEnabled, GInteractiveState itemState, bool drawOnlyMouseActive, LinearShapeType shapeHorizontal, LinearShapeType shapeVertical)
        {
            if (isEnabled && (!drawOnlyMouseActive || itemState.IsMouseActive()))
            {   // Buttony kreslím jen pokud ScrollBar je Enabled, a (mám kreslit i za stavu bez myši = Thumb, anebo button je myšoaktivní = Min/Max):
                GPainter.DrawAreaBase(graphics, bounds, Skin.ScrollBar.BackColorButton, orientation, itemState, null, null);
                // GPainter.DrawButtonBase(graphics, bounds, Skin.ScrollBar.BackColorButton, itemState, orientation, 0, null, null);
            }

            LinearShapeType shape = (orientation == Orientation.Vertical ? shapeVertical : shapeHorizontal);
            if (shape == LinearShapeType.None) return;

            Rectangle shapeBounds = bounds;
            if (itemState.IsMouseDown())
                shapeBounds = shapeBounds.Add(1, 1);
            GraphicSetting graphicSetting;
            GraphicsPath imagePath = GPainter.CreatePathLinearShape(shape, shapeBounds, 2, out graphicSetting);
            if (imagePath != null)
            {
                GInteractiveState state = (isEnabled ? itemState : GInteractiveState.Disabled);
                Color foreColor = Skin.GetForeColor(Skin.ScrollBar.TextColorButton, state);
                using (GPainter.GraphicsUse(graphics, graphicSetting))
                {
                    graphics.DrawPath(Skin.Pen(foreColor), imagePath);
                }
            }
        }
        #endregion
        #region DrawTabHeader
        internal static void DrawTabHeaderItem(Graphics graphics, Rectangle absoluteBounds, ITabHeaderItemPaintData tabHeader)
        {
            Rectangle? backArea, lineArea, lightArea, darkArea;
            _DrawTabHeaderItemGetArea(absoluteBounds, tabHeader, out backArea, out lineArea, out lightArea, out darkArea);
            MatrixTransformationType transformation = _DrawTabHeaderGetTransformation(tabHeader.Position);

            _DrawTabHeaderItemBackground(graphics, absoluteBounds, tabHeader, backArea, lineArea, lightArea, darkArea, transformation);
            _DrawTabHeaderItemUserDraw(graphics, absoluteBounds, tabHeader, backArea, lineArea, lightArea, darkArea, transformation);
            _DrawTabHeaderItemImage(graphics, absoluteBounds, tabHeader, backArea, lineArea, lightArea, darkArea, transformation);
            _DrawTabHeaderItemText(graphics, absoluteBounds, tabHeader, backArea, lineArea, lightArea, darkArea, transformation);
            _DrawTabHeaderItemCloseButton(graphics, absoluteBounds, tabHeader, backArea, lineArea, lightArea, darkArea, transformation);
            _DrawTabHeaderItemLines(graphics, absoluteBounds, tabHeader, backArea, lineArea, lightArea, darkArea, transformation);
        }
        /// <summary>
        /// Určí souřadnice jednotlivých prostor (pozadí a linky)
        /// </summary>
        /// <param name="absoluteBounds"></param>
        /// <param name="tabHeader"></param>
        /// <param name="backArea"></param>
        /// <param name="lineArea"></param>
        /// <param name="lightArea"></param>
        /// <param name="darkArea"></param>
        private static void _DrawTabHeaderItemGetArea(Rectangle absoluteBounds, ITabHeaderItemPaintData tabHeader, out Rectangle? backArea, out Rectangle? lineArea, out Rectangle? lightArea, out Rectangle? darkArea)
        {
            backArea = null;
            lineArea = null;
            lightArea = null;
            darkArea = null;

            int x = absoluteBounds.X;
            int y = absoluteBounds.Y;
            int w = absoluteBounds.Width;
            int h = absoluteBounds.Height;

            bool isActive = tabHeader.IsActive;                      // Aktivní záhlaví: má linku, prostor backArea jde až dolů, ale nemá light a dark area
            bool isHot = tabHeader.InteractiveState.IsMouseActive(); // Hot záhlaví: má linku, prostor backArea nejde až dolů, má light a dark area

            int al = (isActive || isHot ? 2 : 0);          // Šířka horní linky (kreslí se u záhlaví, které je aktivní nebo hot)
            int dl = (isActive ? 0 : 2);                   // Šířka dolní linky (nekreslí se u záhlaví, které je aktivní)
            int bl = (isActive ? 0 : 1);                   // Šířka bočních linek (lightArea a darkArea) (nekreslí se u záhlaví, které je aktivní)

            switch (tabHeader.Position)
            {
                case RectangleSide.Top:
                    backArea = new Rectangle(x + bl, y + al, w - bl - bl, h - al - dl);  // Plocha pozadí, bez Aktivní linky, bez Dolní linky, bez Dark a Light linky
                    lineArea = new Rectangle(x + bl, y, w - bl - bl, al);                // Plocha aktivní linky
                    lightArea = new Rectangle(x, y + al, bl, h - al - dl);               // Plocha světlé boční linky
                    darkArea = new Rectangle(x + w - dl, y + al, bl, h - al - dl);       // Plocha tmavé boční linky
                    break;
                case RectangleSide.Bottom:
                    backArea = new Rectangle(x + bl, y + dl, w - bl - bl, h - al - dl);
                    lineArea = new Rectangle(x + bl, y + h - al, w - bl - bl, al);
                    lightArea = new Rectangle(x, y + dl, bl, h - al - dl);
                    darkArea = new Rectangle(x + w - dl, y + dl, bl, h - al - dl);
                    break;
                case RectangleSide.Left:
                    backArea = new Rectangle(x + al, y + bl, w - al - dl, h - bl - bl);
                    lineArea = new Rectangle(x, y + bl, al, h - bl - bl);
                    lightArea = new Rectangle(x + al, y, w - al - dl, bl);
                    darkArea = new Rectangle(x + al, y + h - bl, w - al - dl, bl);
                    break;
                case RectangleSide.Right:
                    backArea = new Rectangle(x + dl, y + bl, w - al - dl, h - bl - bl);
                    lineArea = new Rectangle(x + w - al, y + bl, al, h - bl - bl);
                    lightArea = new Rectangle(x + dl, y, w - al - dl, bl);
                    darkArea = new Rectangle(x + dl, y + h - bl, w - al - dl, bl);
                    break;
            }
        }
        /// <summary>
        /// Vykreslí pozadí (nikoli linky okolo něj).
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="absoluteBounds">Souřadnice celého headeru</param>
        /// <param name="tabHeader">Data headeru</param>
        /// <param name="backArea">Souřadnice pouze pozadí</param>
        /// <param name="lineArea">Souřadnice linky naznačující aktivitu (pro pozici Top je to vodorovná linka nahoře)</param>
        /// <param name="lightArea">Souřadnice světlé boční linky na začátku headeru (pro pozici Top je to svislá linka vlevo)</param>
        /// <param name="darkArea">Souřadnice tmavé boční linky na konci headeru (pro pozici Top je to svislá linka vpravo)</param>
        /// <param name="transformation">Transformace ze standardní orientace (<see cref="RectangleSide.Top"/>) na aktuální stav (dle <see cref="ITabHeaderItemPaintData.Position"/>)</param>
        private static void _DrawTabHeaderItemBackground(Graphics graphics, Rectangle absoluteBounds, ITabHeaderItemPaintData tabHeader, Rectangle? backArea, Rectangle? lineArea, Rectangle? lightArea, Rectangle? darkArea, MatrixTransformationType transformation)
        {
            if (backArea.HasValue)
            {
                bool isActive = tabHeader.IsActive;
                bool hasBackColor = tabHeader.BackColor.HasValue;
                Color? backColor = (isActive ?
                      (hasBackColor ? tabHeader.BackColor.Value : Skin.TabHeader.BackColorActive) :
                      (hasBackColor ? tabHeader.BackColor.Value.Morph(Skin.TabHeader.BackColor, 0.25f) : Skin.TabHeader.BackColor));

                if (backColor.HasValue)
                    graphics.FillRectangle(Skin.Brush(backColor.Value), backArea.Value);
            }
        }
        /// <summary>
        /// Zavolá UserDataDraw
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="absoluteBounds">Souřadnice celého headeru</param>
        /// <param name="tabHeader">Data headeru</param>
        /// <param name="backArea">Souřadnice pouze pozadí</param>
        /// <param name="lineArea">Souřadnice linky naznačující aktivitu (pro pozici Top je to vodorovná linka nahoře)</param>
        /// <param name="lightArea">Souřadnice světlé boční linky na začátku headeru (pro pozici Top je to svislá linka vlevo)</param>
        /// <param name="darkArea">Souřadnice tmavé boční linky na konci headeru (pro pozici Top je to svislá linka vpravo)</param>
        /// <param name="transformation">Transformace ze standardní orientace (<see cref="RectangleSide.Top"/>) na aktuální stav (dle <see cref="ITabHeaderItemPaintData.Position"/>)</param>
        private static void _DrawTabHeaderItemUserDraw(Graphics graphics, Rectangle absoluteBounds, ITabHeaderItemPaintData tabHeader, Rectangle? backArea, Rectangle? lineArea, Rectangle? lightArea, Rectangle? darkArea, MatrixTransformationType transformation)
        {
            if (backArea.HasValue)
            {
                tabHeader.UserDataDraw(graphics, backArea.Value);
            }
        }
        /// <summary>
        /// Vykreslí Image (ikonka před textem)
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="absoluteBounds">Souřadnice celého headeru</param>
        /// <param name="tabHeader">Data headeru</param>
        /// <param name="backArea">Souřadnice pouze pozadí</param>
        /// <param name="lineArea">Souřadnice linky naznačující aktivitu (pro pozici Top je to vodorovná linka nahoře)</param>
        /// <param name="lightArea">Souřadnice světlé boční linky na začátku headeru (pro pozici Top je to svislá linka vlevo)</param>
        /// <param name="darkArea">Souřadnice tmavé boční linky na konci headeru (pro pozici Top je to svislá linka vpravo)</param>
        /// <param name="transformation">Transformace ze standardní orientace (<see cref="RectangleSide.Top"/>) na aktuální stav (dle <see cref="ITabHeaderItemPaintData.Position"/>)</param>
        private static void _DrawTabHeaderItemImage(Graphics graphics, Rectangle absoluteBounds, ITabHeaderItemPaintData tabHeader, Rectangle? backArea, Rectangle? lineArea, Rectangle? lightArea, Rectangle? darkArea, MatrixTransformationType transformation)
        {
            if (tabHeader.Image == null) return;
            Rectangle imageBounds = tabHeader.ImageBounds.Add(absoluteBounds.Location);
            bool isVertical = (tabHeader.Position == RectangleSide.Left || tabHeader.Position == RectangleSide.Right);
            if (!isVertical)
            {   // Vodorovná orientace = bez převracení:
                graphics.DrawImage(tabHeader.Image, imageBounds);
            }
            else
            {   // Svislá orientace = zajistíme vhodné otočení grafiky:
                imageBounds = imageBounds.Swap();               // Pro vertikální text převedu prostor textu "na šířku", protože otáčet "na výšku" ho bude Matrix aplikovaný do Graphics
                Matrix matrixOld = graphics.Transform;
                graphics.Transform = GetMatrix(transformation, imageBounds);
                graphics.DrawImage(tabHeader.Image, imageBounds);
                graphics.Transform = matrixOld;
            }
        }
        /// <summary>
        /// Vykreslí text záhlaví
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="absoluteBounds">Souřadnice celého headeru</param>
        /// <param name="tabHeader">Data headeru</param>
        /// <param name="backArea">Souřadnice pouze pozadí</param>
        /// <param name="lineArea">Souřadnice linky naznačující aktivitu (pro pozici Top je to vodorovná linka nahoře)</param>
        /// <param name="lightArea">Souřadnice světlé boční linky na začátku headeru (pro pozici Top je to svislá linka vlevo)</param>
        /// <param name="darkArea">Souřadnice tmavé boční linky na konci headeru (pro pozici Top je to svislá linka vpravo)</param>
        /// <param name="transformation">Transformace ze standardní orientace (<see cref="RectangleSide.Top"/>) na aktuální stav (dle <see cref="ITabHeaderItemPaintData.Position"/>)</param>
        private static void _DrawTabHeaderItemText(Graphics graphics, Rectangle absoluteBounds, ITabHeaderItemPaintData tabHeader, Rectangle? backArea, Rectangle? lineArea, Rectangle? lightArea, Rectangle? darkArea, MatrixTransformationType transformation)
        {
            Rectangle textBounds = tabHeader.TextBounds.Add(absoluteBounds.Location);
            bool isActive = tabHeader.IsActive;
            Color textColor = (isActive ? Skin.TabHeader.TextColorActive : Skin.TabHeader.TextColor);
            GPainter.DrawString(graphics, textBounds, tabHeader.Text, textColor, tabHeader.Font, ContentAlignment.MiddleCenter, transformation);
        }
        /// <summary>
        /// Vykreslí CloseButton, pokud má být viditelný
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="absoluteBounds">Souřadnice celého headeru</param>
        /// <param name="tabHeader">Data headeru</param>
        /// <param name="backArea">Souřadnice pouze pozadí</param>
        /// <param name="lineArea">Souřadnice linky naznačující aktivitu (pro pozici Top je to vodorovná linka nahoře)</param>
        /// <param name="lightArea">Souřadnice světlé boční linky na začátku headeru (pro pozici Top je to svislá linka vlevo)</param>
        /// <param name="darkArea">Souřadnice tmavé boční linky na konci headeru (pro pozici Top je to svislá linka vpravo)</param>
        /// <param name="transformation">Transformace ze standardní orientace (<see cref="RectangleSide.Top"/>) na aktuální stav (dle <see cref="ITabHeaderItemPaintData.Position"/>)</param>
        private static void _DrawTabHeaderItemCloseButton(Graphics graphics, Rectangle absoluteBounds, ITabHeaderItemPaintData tabHeader, Rectangle? backArea, Rectangle? lineArea, Rectangle? lightArea, Rectangle? darkArea, MatrixTransformationType transformation)
        { }
        /// <summary>
        /// Vykreslí všechny linky kolem kolem headeru
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="absoluteBounds">Souřadnice celého headeru</param>
        /// <param name="tabHeader">Data headeru</param>
        /// <param name="backArea">Souřadnice pouze pozadí</param>
        /// <param name="lineArea">Souřadnice linky naznačující aktivitu (pro pozici Top je to vodorovná linka nahoře)</param>
        /// <param name="lightArea">Souřadnice světlé boční linky na začátku headeru (pro pozici Top je to svislá linka vlevo)</param>
        /// <param name="darkArea">Souřadnice tmavé boční linky na konci headeru (pro pozici Top je to svislá linka vpravo)</param>
        /// <param name="transformation">Transformace ze standardní orientace (<see cref="RectangleSide.Top"/>) na aktuální stav (dle <see cref="ITabHeaderItemPaintData.Position"/>)</param>
        private static void _DrawTabHeaderItemLines(Graphics graphics, Rectangle absoluteBounds, ITabHeaderItemPaintData tabHeader, Rectangle? backArea, Rectangle? lineArea, Rectangle? lightArea, Rectangle? darkArea, MatrixTransformationType transformation)
        {
            bool isActive = tabHeader.IsActive;
            bool isHot = tabHeader.InteractiveState.IsMouseActive();
            bool hasBackColor = tabHeader.BackColor.HasValue;

            Color? backColor = (isActive ?
                  (hasBackColor ? tabHeader.BackColor.Value : Skin.TabHeader.BackColorActive) :
                  (hasBackColor ? tabHeader.BackColor.Value.Morph(Skin.TabHeader.BackColor, 0.25f) : Skin.TabHeader.BackColor));

            Color? lineColor = (isActive ? (Color?)Skin.TabHeader.LineColorActive : (isHot ? (Color?)Skin.TabHeader.LineColorHot : (Color?)null));
            Color? lightColor = (isActive ? (Color?)null : backColor.Value.Morph(Skin.Modifiers.Effect3DLight));  // Skin.TabHeader.BorderColor.Morph(Skin.Modifiers.Effect3DLight));
            Color? darkColor = (isActive ? (Color?)null : backColor.Value.Morph(Skin.Modifiers.Effect3DDark));    // Skin.TabHeader.BorderColor.Morph(Skin.Modifiers.Effect3DDark));

            if (lineArea.HasValue && lineColor.HasValue)
                graphics.FillRectangle(Skin.Brush(lineColor.Value), lineArea.Value);
            if (lightArea.HasValue && lightColor.HasValue)
                graphics.FillRectangle(Skin.Brush(lightColor.Value), lightArea.Value);
            if (darkArea.HasValue && darkColor.HasValue)
                graphics.FillRectangle(Skin.Brush(darkColor.Value), darkArea.Value);
        }
        /// <summary>
        /// Vrátí režim transformace prostoru pro zobrazení textu záhlaví TabHeader,
        /// pro danou hodnotu RectangleSide = <see cref="ITabHeaderItemPaintData.Position"/>
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        private static MatrixTransformationType _DrawTabHeaderGetTransformation(RectangleSide side)
        {
            switch (side)
            {
                case RectangleSide.Top: return MatrixTransformationType.NoTransform;
                case RectangleSide.Right: return MatrixTransformationType.Rotate90;
                case RectangleSide.Bottom: return MatrixTransformationType.NoTransform;
                case RectangleSide.Left: return MatrixTransformationType.Rotate270;
            }
            return MatrixTransformationType.NoTransform;
        }
        #endregion
        #region DrawShadow
        internal static void DrawShadow(Graphics graphics, Rectangle bounds)
        {
            _DrawShadow(graphics, bounds, 5, false);
        }
        internal static void DrawShadow(Graphics graphics, Rectangle bounds, int size)
        {
            _DrawShadow(graphics, bounds, size, false);
        }
        internal static void DrawShadow(Graphics graphics, Rectangle bounds, int size, bool inner)
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
        #region DrawGraphItem
        /// <summary>
        /// Provede kompletní vykreslení prvku grafu
        /// </summary>
        /// <param name="args"></param>
        public static void GraphItemDraw(GraphItemArgs args)
        {
            Rectangle[] boundsParts = GraphItemCreateBounds(args);

            GraphItemDrawBack(args, boundsParts);
            GraphItemDrawHatch(args, boundsParts);
            GraphItemDrawRatio(args, boundsParts);
            GraphItemDrawBorder(args, boundsParts);
        }
        private static void GraphItemDrawBack(GraphItemArgs args, Rectangle[] boundsParts)
        {
            if (!args.BackColor.HasValue) return;
            GPainter.DrawEffect3D(args.Graphics, boundsParts[0], args.BackColor.Value, Orientation.Horizontal, args.Effect3D, null);
        }
        private static void GraphItemDrawHatch(GraphItemArgs args, Rectangle[] boundsParts)
        {
            if (!args.HasHatchStyle) return;
            using (HatchBrush hatchBrush = new HatchBrush(args.HatchStyle.Value, args.HatchColor.Value, Color.Transparent))
            {
                args.Graphics.FillRectangle(hatchBrush, boundsParts[0]);
            }
        }
        private static void GraphItemDrawRatio(GraphItemArgs args, Rectangle[] boundsParts)
        {
            if (!args.HasRatio) return;
            Rectangle bounds = boundsParts[0];
            Point p1 = GraphItemDrawRatioGetPoint(args.RatioBegin.Value, bounds, bounds.X);
            Point p2 = GraphItemDrawRatioGetPoint((args.RatioEnd.HasValue ? args.RatioEnd.Value : args.RatioBegin.Value), bounds, bounds.Right);

            if (args.RatioBeginBackColor.HasValue)
            {
                Point p0 = new Point(bounds.X, bounds.Bottom);
                Point p3 = new Point(bounds.Right, bounds.Bottom);
                GraphicsPath path = new GraphicsPath();
                path.AddLine(p0, p1);
                path.AddLine(p1, p2);
                path.AddLine(p2, p3);
                path.AddLine(p3, p0);
                path.CloseFigure();

                if (args.RatioEndBackColor.HasValue)
                {
                    using (LinearGradientBrush lgb = new LinearGradientBrush(bounds, args.RatioBeginBackColor.Value, args.RatioEndBackColor.Value, 0f))
                        args.Graphics.FillPath(lgb, path);
                }
                else
                {
                    args.Graphics.FillPath(Skin.Brush(args.RatioBeginBackColor.Value), path);
                }
            }

            if (args.RatioLineColor.HasValue)
            {
                using (GraphicsUseSmooth(args.Graphics))
                {
                    Pen pen = Skin.Pen(args.RatioLineColor.Value);
                    if (args.RatioLineWidth.HasValue && args.RatioLineWidth.Value > 0)
                        pen.Width = args.RatioLineWidth.Value;
                    args.Graphics.DrawLine(pen, p1, p2);
                }
            }
        }
        private static Point GraphItemDrawRatioGetPoint(float ratio, Rectangle bounds, int x)
        {
            ratio = (ratio < 0f ? 0f : (ratio > 1f ? 1f : ratio));
            int y = bounds.Bottom - (int)(Math.Round((float)bounds.Height * ratio, 0));
            return new Point(x, y);
        }
        private static void GraphItemDrawBorder(GraphItemArgs args, Rectangle[] boundsParts)
        {
            Color? borderColor = (args.IsSelected ? Skin.Graph.ElementSelectedLineColor :
                                 (args.IsFramed ? Skin.Graph.ElementFramedLineColor : args.BackColor));
            if (borderColor.HasValue)
            {
                Color colorTop = Skin.Modifiers.GetColor3DBorderLight(borderColor.Value, 0.50f);
                Color colorBottom = Skin.Modifiers.GetColor3DBorderDark(borderColor.Value, 0.50f);
                args.Graphics.FillRectangle(Skin.Brush(colorTop), boundsParts[2]);
                args.Graphics.FillRectangle(Skin.Brush(colorBottom), boundsParts[4]);
                if (!args.HasBorder)
                {   // Běžné okraje prvku (3D efekt na krajních prvcích):
                    if (args.IsFirstItem)
                        args.Graphics.FillRectangle(Skin.Brush(colorTop), boundsParts[1]);
                    if (args.IsLastItem)
                        args.Graphics.FillRectangle(Skin.Brush(colorBottom), boundsParts[3]);
                }
                else
                {   // Zvýrazněné okraje prvku (Selected, Framed na krajních prvcích):
                    if (args.IsFirstItem)
                        args.Graphics.FillRectangle(Skin.Brush(borderColor.Value), boundsParts[1]);
                    if (args.IsLastItem)
                        args.Graphics.FillRectangle(Skin.Brush(borderColor.Value), boundsParts[3]);
                }
            }
        }
        /// <summary>
        /// Metoda vrátí sadu souřadnic vnitřních prvků položky grafu.
        /// </summary>
        /// <returns></returns>
        public static Rectangle[] GraphItemCreateBounds(GraphItemArgs args)
        {
            return GraphItemCreateBounds(args.BoundsAbsolute, args.IsGroup, args.IsFirstItem, args.IsLastItem, args.HasBorder);
        }
        /// <summary>
        /// Metoda vrátí sadu souřadnic vnitřních prvků položky grafu.
        /// </summary>
        /// <param name="boundsAbsolute"></param>
        /// <param name="forGroupItem"></param>
        /// <param name="isFirst"></param>
        /// <param name="isLast"></param>
        /// <param name="hasBorder"></param>
        /// <returns></returns>
        public static Rectangle[] GraphItemCreateBounds(Rectangle boundsAbsolute, bool forGroupItem, bool isFirst, bool isLast, bool hasBorder)
        {
            int x = boundsAbsolute.X;
            int y = boundsAbsolute.Y;
            int w = boundsAbsolute.Width;
            int h = boundsAbsolute.Height;
            int wb = (w <= 2 ? 0 : ((w < 5) ? 1 : (hasBorder ? 2 : 1)));
            int hb = (h <= 2 ? 0 : ((h < 10 || forGroupItem) ? 1 : (hasBorder ? 3 : 2)));    // Výška proužku "horní a dolní okraj"
            int hc = h - 2 * hb;

            Rectangle[] boundsParts = new Rectangle[5];
            if (forGroupItem)
                boundsParts[0] = new Rectangle(x, y + hb, w, hc);              // Střední prostor pro Group
            else
                boundsParts[0] = new Rectangle(x, y, w, h);                    // Střední prostor pro Item
            if (isFirst)
                boundsParts[1] = new Rectangle(x, y + hb, wb, hc + hb);        // Levý okraj
            boundsParts[2] = new Rectangle(x, y, w, hb);                       // Horní okraj
            if (isLast)
                boundsParts[3] = new Rectangle(x + w - wb, y, wb, hc + hb);    // Pravý okraj
            boundsParts[4] = new Rectangle(x, y + hb + hc, w, hb);             // Dolní okraj

            return boundsParts;
        }
        /// <summary>
        /// Třída pro přenos dat pro kreslení grafického prvku
        /// </summary>
        public class GraphItemArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="graphics"></param>
            /// <param name="boundsAbsolute"></param>
            public GraphItemArgs(Graphics graphics, Rectangle boundsAbsolute)
            {
                this.Graphics = graphics;
                this.BoundsAbsolute = boundsAbsolute;
            }
            /// <summary>
            /// Objekt grafiky, do které se kreslí
            /// </summary>
            public Graphics Graphics { get; private set; }
            /// <summary>
            /// Souřadnice, kde se prvek nachází
            /// </summary>
            public Rectangle BoundsAbsolute { get; private set; }
            /// <summary>
            /// Příznak: prvek je grupa?
            /// </summary>
            public bool IsGroup { get; set; }
            /// <summary>
            /// Příznak: prvek je první v grupě?
            /// </summary>
            public bool IsFirstItem { get; set; }
            /// <summary>
            /// Příznak: prvek je poslední v grupě?
            /// </summary>
            public bool IsLastItem { get; set; }
            /// <summary>
            /// Příznak: prvek je selectovaný?
            /// </summary>
            public bool IsSelected { get; set; }
            /// <summary>
            /// Příznak: prvek je framovaný?
            /// </summary>
            public bool IsFramed { get; set; }
            /// <summary>
            /// Příznak: prvek je aktivovaný?
            /// </summary>
            public bool IsActivated { get; set; }
            /// <summary>
            /// Příznak: prvek má výraznější Border?
            /// </summary>
            public bool HasBorder { get { return this.IsSelected || this.IsFramed; } }
            /// <summary>
            /// Barva plného pozadí prvku
            /// </summary>
            public Color? BackColor { get; set; }
            /// <summary>
            /// 3D effekt
            /// </summary>
            public float? Effect3D { get; set; }
            /// <summary>
            /// Styl překreslení prvku (vzorek)
            /// </summary>
            public HatchStyle? HatchStyle { get; set; }
            /// <summary>
            /// Barva pro kreslení <see cref="HatchStyle"/>
            /// </summary>
            public Color? HatchColor { get; set; }
            /// <summary>
            /// true pokud se má kreslit Hatch,
            /// tedy když <see cref="HatchStyle"/> i <see cref="HatchColor"/> mají hodnotu
            /// </summary>
            public bool HasHatchStyle { get { return (this.HatchStyle.HasValue && this.HatchColor.HasValue); } }
            /// <summary>
            /// Poměrná hodnota "nějakého" splnění v rámci prvku, na jeho počátku.
            /// Běžně se vykresluje jako poměrná část prvku, měřeno odspodu, která symbolizuje míru "naplnění" daného úseku.
            /// Část Ratio má tvar lichoběžníku, a spojuje body Begin = { Left, <see cref="RatioBegin"/> } a { Right, <see cref="RatioEnd"/> }.
            /// <para/>
            /// Pro zjednodušení zadávání: pokud je naplněno <see cref="RatioBegin"/>, ale v <see cref="RatioEnd"/> je null, 
            /// pak vykreslovací algoritmus předpokládá hodnotu End stejnou jako Begin. To znamená, že pro "obdélníkové" ratio stačí naplnit jen <see cref="RatioBegin"/>.
            /// Ale opačně to neplatí.
            /// </summary>
            public float? RatioBegin { get; set; }
            /// <summary>
            /// Poměrná hodnota "nějakého" splnění v rámci prvku, na jeho konci.
            /// Běžně se vykresluje jako poměrná část prvku, měřeno odspodu, která symbolizuje míru "naplnění" daného úseku.
            /// Část Ratio má tvar lichoběžníku, a spojuje body Begin = { Left, <see cref="RatioBegin"/> } a { Right, <see cref="RatioEnd"/> }.
            /// <para/>
            /// Pro zjednodušení zadávání: pokud je naplněno <see cref="RatioBegin"/>, ale v <see cref="RatioEnd"/> je null, 
            /// pak vykreslovací algoritmus předpokládá hodnotu End stejnou jako Begin. To znamená, že pro "obdélníkové" ratio stačí naplnit jen <see cref="RatioBegin"/>.
            /// Ale opačně to neplatí.
            /// </summary>
            public float? RatioEnd { get; set; }
            /// Barva pozadí prvku, kreslená v části Ratio, na straně času Begin.
            /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má hodnotu větší než 0f.
            /// Touto barvou je vykreslena dolní část prvku, která symbolizuje míru "naplnění" daného úseku.
            /// Tato část má tvar lichoběžníku, dolní okraj je na hodnotě 0, levý okraj má výšku <see cref="RatioBegin"/>, pravý okraj má výšku <see cref="RatioEnd"/>.
            /// Může sloužit k zobrazení vyčerpané pracovní kapacity, nebo jako lineární částečka grafu sloupcového nebo liniového.
            /// Tato barva se použije buď jako Solid color pro celý prvek v části Ratio, 
            /// anebo jako počáteční barva na souřadnici X = čas Begin při výplni Linear, 
            /// a to tehdy, pokud je zadána i barva <see cref="RatioEndBackColor"/> (ta reprezentuje barvu na souřadnici X = čas End).
            /// Z databáze se načítá ze sloupce: "ratio_begin_back_color", je NEPOVINNÝ.
            public Color? RatioBeginBackColor { get; set; }
            /// <summary>
            /// Barva pozadí prvku, kreslená v části Ratio, na straně času End.
            /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má hodnotu větší než 0f.
            /// Touto barvou je vykreslena dolní část prvku, která symbolizuje míru "naplnění" daného úseku.
            /// Tato část má tvar lichoběžníku, dolní okraj je na hodnotě 0, levý okraj má výšku <see cref="RatioBegin"/>, pravý okraj má výšku <see cref="RatioEnd"/>.
            /// Může sloužit k zobrazení vyčerpané pracovní kapacity, nebo jako lineární částečka grafu sloupcového nebo liniového.
            /// Tato barva se použije jako koncová barva (na souřadnici X = čas End) v lineární výplni prostoru Ratio,
            /// kde počáteční barva výplně (na souřadnici X = čas Begin) je dána v <see cref="RatioBeginBackColor"/>.
            /// Z databáze se načítá ze sloupce: "ratio_end_back_color", je NEPOVINNÝ.
            /// </summary>
            public Color? RatioEndBackColor { get; set; }
            /// <summary>
            /// Barva linky, kreslená v úrovni Ratio.
            /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má zadanou hodnotu v rozsahu 0 (včetně) a více.
            /// Touto barvou je vykreslena přímá linie, která symbolizuje míru "naplnění" daného úseku, 
            /// a spojuje body Begin = { Left, <see cref="RatioBegin"/> } a { Right, <see cref="RatioEnd"/> }.
            /// </summary>
            public Color? RatioLineColor { get; set; }
            /// <summary>
            /// Šířka linky, kreslená v úrovni Ratio.
            /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má zadanou hodnotu v rozsahu 0 (včetně) a více.
            /// Čárou této šířky je vykreslena přímá linie, která symbolizuje míru "naplnění" daného úseku, 
            /// a spojuje body Begin = { Left, <see cref="RatioBegin"/> } a { Right, <see cref="RatioEnd"/> }.
            /// </summary>
            public int? RatioLineWidth { get; set; }
            /// <summary>
            /// true pokud se má kreslit Ratio,
            /// tedy když <see cref="RatioBegin"/> a (<see cref="RatioBeginBackColor"/> nebo <see cref="RatioLineColor"/>) mají hodnotu
            /// </summary>
            public bool HasRatio { get { return (this.RatioBegin.HasValue && (this.RatioBeginBackColor.HasValue || this.RatioLineColor.HasValue)); } }
        }
        #endregion
        #region Drawing2D.Matrix = transformace vykreslovaných souřadnic
        /// <summary>
        /// Vrátí Matrix pro transformaci požadovaného typu,
        /// a to tak že střed transformace bude uprostřed daného prostoru.
        /// </summary>
        /// <param name="transformation">Typ transformace</param>
        /// <param name="area">Prostor, který se má transformovat okolo svého středu</param>
        /// <returns>Matrix, který zajistí transformaci</returns>
        internal static Matrix GetMatrix(MatrixTransformationType transformation, RectangleF area)
        {
            return GetMatrix(transformation, new PointF(area.X + area.Width / 2F, area.Y + area.Height / 2F));
        }
        /// <summary>
        /// Vrátí Matrix pro transformaci požadovaného typu,
        /// a to tak že střed transformace bude v daném bodě.
        /// </summary>
        /// <param name="transformation">Typ transformace</param>
        /// <param name="center">Bod, okolo kterého se bude tvar transformovat</param>
        /// <returns>Matrix, který zajistí transformaci</returns>
        internal static Matrix GetMatrix(MatrixTransformationType transformation, PointF center)
        {
            switch (transformation)
            {
                case MatrixTransformationType.Rotate90:
                    return _GetMatrixRotate90(center);
                case MatrixTransformationType.Rotate180:
                    return _GetMatrixRotate180(center);
                case MatrixTransformationType.Rotate270:
                    return _GetMatrixRotate270(center);
                case MatrixTransformationType.MirrorHorizontal:
                    return _GetMatrixMirrorHorizontal(center);
                case MatrixTransformationType.MirrorVertical:
                    return _GetMatrixMirrorVertical(center);
            }
            return new Matrix(1, 0, 0, 1, 0, 0);          // void Matrix, sice pracuje, ale dané souřadnice nijak netransformuje. Takové ((1 * 1) == 1)..
        }
        /// <summary>
        /// Vrátí Matrix, který provede rotaci tvaru (Path, Polygon) o 90° VE SMĚRU hodinových ručiček, 
        /// a to tak že střed rotace bude uprostřed daného prostoru.
        /// </summary>
        /// <param name="area">Prostor, který se má otáčet okolo svého středu</param>
        /// <returns>Matrix, který zajistí otáčení</returns>
        private static Matrix _GetMatrixRotate90(RectangleF area)
        {
            return _GetMatrixRotate90(area.Center());
        }
        /// <summary>
        /// Vrátí Matrix, který provede rotaci tvaru (Path, Polygon) o 90° VE SMĚRU hodinových ručiček, 
        /// a to tak že střed rotace bude v daném bodě.
        /// </summary>
        /// <param name="center">Bod, okolo kterého se bude tvar otáčet</param>
        /// <returns>Matrix, který zajistí otáčení</returns>
        private static Matrix _GetMatrixRotate90(PointF center)
        {
            float dx = center.X + center.Y;
            float dy = center.Y - center.X;
            return new Matrix(0F, 1F, -1F, 0F, dx, dy);
        }
        /// <summary>
        /// Vrátí Matrix, který provede rotaci tvaru (Path, Polygon) o 180°, což je stejné jako zrcadlení horizontální i vertikální, 
        /// a to tak že střed otáčení = zrcadlení bude uprostřed daného prostoru.
        /// </summary>
        /// <param name="area">Prostor, který se má otáčet okolo svého středu</param>
        /// <returns>Matrix, který zajistí zrcadlení</returns>
        private static Matrix _GetMatrixRotate180(RectangleF area)
        {
            return _GetMatrixRotate180(area.Center());
        }
        /// <summary>
        /// Vrátí Matrix, který provede rotaci tvaru (Path, Polygon) o 180°, což je stejné jako zrcadlení horizontální i vertikální, 
        /// a to tak že střed otáčení = zrcadlení bude v daném bodě.
        /// </summary>
        /// <param name="center">Bod, okolo kterého se bude tvar otáčet</param>
        /// <returns>Matrix, který zajistí zrcadlení</returns>
        private static Matrix _GetMatrixRotate180(PointF center)
        {
            float dx = center.X + center.X;
            float dy = center.Y + center.Y;
            return new Matrix(-1F, 0F, 0F, -1F, dx, dy);
        }
        /// <summary>
        /// Vrátí Matrix, který provede rotaci tvaru (Path, Polygon) o 90° PROTI SMĚRU hodinových ručiček, 
        /// a to tak že střed rotace bude uprostřed daného prostoru.
        /// </summary>
        /// <param name="area">Prostor, který se má otáčet okolo svého středu</param>
        /// <returns>Matrix, který zajistí otáčení</returns>
        private static Matrix _GetMatrixRotate270(RectangleF area)
        {
            return _GetMatrixRotate270(area.Center());
        }
        /// <summary>
        /// Vrátí Matrix, který provede rotaci tvaru (Path, Polygon) o 90° PROTI SMĚRU hodinových ručiček, 
        /// a to tak že střed rotace bude v daném bodě.
        /// </summary>
        /// <param name="center">Bod, okolo kterého se bude tvar otáčet</param>
        /// <returns>Matrix, který zajistí otáčení</returns>
        private static Matrix _GetMatrixRotate270(PointF center)
        {
            float dx = center.X - center.Y;
            float dy = center.Y + center.X;
            return new Matrix(0F, -1F, 1F, 0F, dx, dy);
        }
        /// <summary>
        /// Vrátí Matrix, který provede zrcadlení tvaru (Path, Polygon) vodorovně = zleva doprava, a zprava doleva,
        /// a to tak že střed zrcadlení bude uprostřed daného prostoru.
        /// </summary>
        /// <param name="area">Prostor, který se má zrcadlit okolo svého středu</param>
        /// <returns>Matrix, který zajistí zrcadlení</returns>
        private static Matrix _GetMatrixMirrorHorizontal(RectangleF area)
        {
            return _GetMatrixMirrorHorizontal(new PointF(area.X + area.Width / 2F, area.Y + area.Height / 2F));
        }
        /// <summary>
        /// Vrátí Matrix, který provede zrcadlení tvaru (Path, Polygon) vodorovně = zleva doprava, a zprava doleva,
        /// a to tak že střed zrcadlení bude v daném bodě.
        /// </summary>
        /// <param name="center">Bod, okolo kterého se bude tvar zrcadlit</param>
        /// <returns>Matrix, který zajistí zrcadlení</returns>
        private static Matrix _GetMatrixMirrorHorizontal(PointF center)
        {
            float dx = center.X + center.X;
            float dy = 0F;
            return new Matrix(-1F, 0F, 0F, 1F, dx, dy);
        }
        /// <summary>
        /// Vrátí Matrix, který provede zrcadlení tvaru (Path, Polygon) svisle = shora dolů, a sdola nahoru,
        /// a to tak že střed zrcadlení bude uprostřed daného prostoru.
        /// </summary>
        /// <param name="area">Prostor, který se má zrcadlit okolo svého středu</param>
        /// <returns>Matrix, který zajistí zrcadlení</returns>
        private static Matrix _GetMatrixMirrorVertical(RectangleF area)
        {
            return _GetMatrixMirrorVertical(new PointF(area.X + area.Width / 2F, area.Y + area.Height / 2F));
        }
        /// <summary>
        /// Vrátí Matrix, který provede zrcadlení tvaru (Path, Polygon) svisle = shora dolů, a sdola nahoru,
        /// a to tak že střed zrcadlení bude v daném bodě.
        /// </summary>
        /// <param name="center">Bod, okolo kterého se bude tvar zrcadlit</param>
        /// <returns>Matrix, který zajistí zrcadlení</returns>
        private static Matrix _GetMatrixMirrorVertical(PointF center)
        {
            float dx = 0F;
            float dy = center.Y + center.Y;
            return new Matrix(1F, 0F, 0F, -1F, dx, dy);
        }
        /// <summary>
        /// Transformuje prostor čtyřúhleníku s pomocí daného matrixu
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>
        internal static Rectangle Transform(Rectangle rectangle, Matrix matrix)
        {
            // Matrix umí transformovat sady bodů, tak mu dva připravíme, přetransformujeme, a z nich vytvoříme výsledný čtyřúhelník:
            Point[] data = new Point[] {
                new Point(rectangle.X, rectangle.Y),
                new Point(rectangle.Right - 1, rectangle.Bottom - 1)};   // Poznámka k bodu 2: souřadnice Right a Bottom jsou vždy ty "až za" rectanglem ! A my budeme transformovat poslední viditelné body rectanglu.
            matrix.TransformPoints(data);                                // Přetransformujeme body
            Rectangle result = RectangleFromTwoPoint(data[0], data[1]);  // Vytvoříme rectangle (takhle, protože nevím zda orientace bodů po transformaci je korektní)
            result.Width = result.Width + 1;                             // Zpátky přidáme 1 bod na šířku i na výšku, ten co jsme na začátku ukradli
            result.Height = result.Height + 1;
            return result;
        }
        /// <summary>
        /// Vrátí prostor (Rectangle), vytvořený ze dvou bodů. Vzájemná pozice dvou bodů je libovolná.
        /// Pokud by jeden rozměr (Width, Height) byl nulový, vrací Rectangle.Empty
        /// </summary>
        /// <param name="a">Jeden bod</param>
        /// <param name="b">Druhý bod</param>
        /// <returns>Prostor daný dvěma body</returns>
        internal static Rectangle RectangleFromTwoPoint(Point a, Point b)
        {
            if (a.X == b.X || a.Y == b.Y) return Rectangle.Empty;           // Nulová výška / šířka => Empty
            return Rectangle.FromLTRB(
                (a.X < b.X ? a.X : b.X),        // Left = menší X
                (a.Y < b.Y ? a.Y : b.Y),        // Top  = menší Y
                (a.X > b.X ? a.X : b.X),        // Right = větší X
                (a.Y > b.Y ? a.Y : b.Y));       // Bottom = větší Y
        }
        #endregion
        #region Imaging.ColorMatrix = změna barevnosti při vykreslování
        /// <summary>
        /// Vrací ColorMatrix, který aplikuje pouze danou průhlednost
        /// </summary>
        /// <param name="alpha"></param>
        /// <returns></returns>
        internal static System.Drawing.Imaging.ColorMatrix CreateColorMatrixAlpha(float alpha)
        {
            System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix();
            colorMatrix.Matrix33 = alpha;                       // Alpha channel: when input Alpha is 1 (full opacity, no transparent), then output Alpha will be (alpha) = input Alpha * (alpha)
            return colorMatrix;
        }
        /// <summary>
        /// Vrací ColorMatrix, který konvertuje barvy ve směru do šedivé a průhledné
        /// </summary>
        /// <param name="gray"></param>
        /// <param name="light"></param>
        /// <returns></returns>
        internal static System.Drawing.Imaging.ColorMatrix CreateColorMatrixGray(float gray, float light)
        {
            float gr = (gray < 0f ? 0f : (gray > 1f ? 1f : gray));   // gray v rozmezí 0 až 1
            float li = (light < 0f ? 0f : (light > 1f ? 1f : light));// light v rozmezí 0 až 1
            float c0 = 0f;                                           // 0 = konstanta 0
            float c1 = 1f;                                           // 1 = konstanta 1
            float sc = 1 - gr;                                       // Podíl původní barvy ve výsledné barvě (v téže složce) = poměr zachování barevnosti
            float oc = gray;                                         // Podíl zdrojové barvy do ostatních barev (ostatní složky) = přelévání barvy => odbarvení
            float al = 0.4f * gr;
            float[][] elements =
            {
                new float[] { sc, oc, oc, c0, c0 },                  // Tento řádek řídí distribuci hodnoty ze vstupního kanálu Red do výstupních kanálů R-G-B-A-?
                new float[] { oc, sc, oc, c0, c0 },                  // Ze vstupního kanálu Green do výstupních R-G-B-A-?
                new float[] { oc, oc, sc, c0, c0 },                  // Ze vstupního kanálu Blue do výstupních R-G-B-A-?
                new float[] { c0, c0, c0, al, c0 },                  // Ze vstupního kanálu Aplha do výstupních R-G-B-A-?     (Alpha: 0=průhledná, 1=Plná barva)
                new float[] { li, li, li, c0, c1 },                  // Fixní přídavek k výstupnímu kanálu
            };
            System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix(elements);
            return colorMatrix;
        }
        internal static System.Drawing.Imaging.ColorMatrix CreateColorMatrixForState(GInteractiveState state)
        {
            float z = 0f;
            float o = 1.0f;
            float g = 0f;
            float u = 1f;
            float[][] elements =
            {
                new float[] { o, z, z, z, g },                  // Red
                new float[] { z, o, z, z, g },                  // Green
                new float[] { z, z, o, z, g },                  // Blue
                new float[] { z, z, z, u, z },                  // Alpha
                new float[] { z, z, z, z, z },                  // Mix
            };
            System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix(elements);
            return colorMatrix;
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
        internal static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY)
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
        internal static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY, RelativePosition positions)
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
        internal static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY, RelativePosition positions, bool joinBroken)
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
        internal static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY, Func<RelativePosition, bool> partSelector)
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
        internal static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY, Func<RelativePosition, bool> partSelector, bool joinBroken)
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
        internal static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY, Action<GraphicsPath, RelativePosition, Point, Point> lineHandler)
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
        internal static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY, Func<RelativePosition, bool> partSelector, Action<GraphicsPath, RelativePosition, Point, Point> lineHandler)
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
        internal static GraphicsPath CreatePathRoundRectangle(Rectangle bounds, int roundX, int roundY, Func<RelativePosition, bool> partSelector, bool joinBroken, Action<GraphicsPath, RelativePosition, Point, Point> lineHandler)
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
        internal static GraphicsPath CreatePathRoundRectangleWithArrow(Rectangle bounds, int roundX, int roundY, Point target)
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
        internal static RelativePosition FindTargetRelativePosition(Point target, Rectangle bounds)
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
        internal static RelativePosition FindTargetRelativePosition(Point target, Rectangle bounds, int borderX, int borderY)
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
        internal static GraphicsPath CreatePathLinearShape(LinearShapeType shape, Rectangle area, int border)
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
        internal static GraphicsPath CreatePathLinearShape(LinearShapeType shape, Rectangle area, int border, out GraphicSetting graphicSetting)
        {
            graphicSetting = GraphicSetting.None;
            if (shape == LinearShapeType.None) return null;
            if ((area.Width - 2 * border) < 10 || (area.Height - 2 * border) < 10) return null;
            Point center = area.Center();
            int size = ((area.Width < area.Height ? area.Width : area.Height) / 2) - border;
            Rectangle bounds = center.CreateRectangleFromCenter(2 * size);
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
        #region LinkLine
        /// <summary>
        /// Metoda vrátí <see cref="GraphicsPath"/>, která reprezentuje linku, která jde z bodu "prevPoint" do bodu "nextPoint",
        /// a může to být linka nebo rovná čára podle parametru "asSCurve".
        /// Pokud jde o přímou čáru, není třeba vysvětlivek.
        /// <para/>
        /// Pokud jde o křivku:
        /// Z bodu "prevPoint" vychází křivka vždy ve směru vodorovně doprava, teprve pak se stáčí patřičným směrem.
        /// Do bodu "nextPoint" vstupuje křivka vždy vodorovně zleva.
        /// Mezi oběma body se křivka esovitě vine jako had.
        /// Pokud jsou body na stejné souřadnici Y: pokud bod "nextPoint" je vpravo od bodu "nextPoint", pak je vrácena přímá čára.
        /// Pokud je tomu naopak, pak je vrácena křivka částečné ležaté osmičky tak, 
        /// aby vycházela z bodu Prev (který je ale vpravo od Next) doprava, stáčí se pak dolů a doleva, nahoru stále doleva, rovně doleva, pak doleva dolů a nakonec doprava do bodu Prev.
        /// <para/>
        /// Kterýkoli z bodů "prevPoint" a "nextPoint" může být null. Pokud jsou null oba, vrací se null.
        /// Pokud je null jeden, je vrácena krátká vodorovná linka ve směru zleva doprava z/do bodu, který není null.
        /// </summary>
        /// <param name="prevPoint"></param>
        /// <param name="nextPoint"></param>
        /// <param name="asSCurve"></param>
        /// <returns></returns>
        internal static GraphicsPath CreatePathLinkLine(Point? prevPoint, Point? nextPoint, bool asSCurve)
        {
            if (!prevPoint.HasValue && !nextPoint.HasValue) return null;

            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int px, py, nx, ny, dx, dy, bx, by;

            // Mám jen prvek Prev: vykreslím linku "z Prev doprava":
            if (!nextPoint.HasValue)
            {
                px = prevPoint.Value.X;
                py = prevPoint.Value.Y;
                nx = px + 12;
                ny = py;
                path.AddLine(px, py, nx, ny);
                return path;
            }

            // Mám jen prvek Next: vykreslím linku "zleva do Next":
            if (!prevPoint.HasValue)
            {
                nx = nextPoint.Value.X;
                ny = nextPoint.Value.Y;
                px = nx - 12;
                py = ny;
                path.AddLine(px, py, nx, ny);
                return path;
            }

            // Máme tedy oba body. Co mezi nimi budeme kreslit?
            px = prevPoint.Value.X;
            py = prevPoint.Value.Y;
            nx = nextPoint.Value.X;
            ny = nextPoint.Value.Y;
            dx = nx - px;              // Vzdálenost (Next - Prev).X: kladná = jdeme doprava, záporná = jdeme doleva
            dy = ny - py;              // Vzdálenost (Next - Prev).Y: kladná = jdeme dolů,    záporná = jdeme nahoru
            if (dy == 0)
            {   // Prvky jsou na stejné souřadnici Y, takže bez ohledu na požavek "asSCurve" to nebude klasická S-křivka:
                if (dx >= 0)
                {   // Next je (v nebo) za Prev, takže to bude přímka, jen ji trochu prodloužím:
                    px = px - 2;
                    nx = nx + 2;
                    path.AddLine(px, py, nx, ny);
                    return path;
                }
                // Next je PŘED Prev, tedy opačné pořadí než je přirozené. 
                // Tady vykreslíme křivku, která vychází z Prev (tj. napravo), jde doprava dolů, stáčí se doleva zpátky,
                //  projde jako ležatá osmička doleva a nahoru nad prvek Next, a pak se stočí doleva dolů do úrovně souřadnice Y a vstoupí zleva do prvku Next (která je vpravo od Prev):

                // 1. část křivky vycházející z Prev, jde kousek doprava, dolů, zpátky doleva a končí na souřadnici Prev.X a Prev.Y + 12
                int p1y = py + 12;
                int tx = 25;
                path.AddBezier(px, py, px + tx, py, px + tx, p1y, px, p1y);

                // 2. část křivky, tvar S, spojující bod (Prev + Y) s bodem (Next - Y):
                int n1y = ny - 12;
                bx = (-dx) / 4;
                if (bx < 12) bx = 12;  // Vzdálenost řídícího bodu na ose X pro tuto část křivky
                path.AddBezier(px, p1y, px - bx, p1y, nx + bx, n1y, nx, n1y);

                // 3. část křivky vycházející z (Next - Y), jde kousek doleva, dolů, zpátky doprava a končí na souřadnici Next.X a Next.Y (zakončení, podobné části 1):
                path.AddBezier(nx, n1y, nx - tx, n1y, nx - tx, ny, nx, ny);

                return path;
            }

            // Prvky jsou na odlišné souřadnici Y, nyní se uplatní volba "asSCurve"
            if (!asSCurve)
            {   // Docela obyčejná přímka:
                path.AddLine(px, py, nx, ny);
                return path;
            }

            // Bezierova křivka z Prev do Next - určíme hodnotu bx (vzdálenost řídícího bodu na ose X):
            bx = dx / 4;                         // Výchozí hodnota je 1/4 vzdálenosti Prev a Next (kladné číslo)
            if (bx < 0) bx = 3 * (-bx);          // Pokud je ale posun záporný (Next je vlevo od Prev), pak musíme bx výrazně zvětšit, aby se zobrazila křivka jdoucí nejprve doprava, a pak zpátky
            if (bx < 16) bx = 16;                // Konstanta pro případ, kdy dx je malé, aby se S-křivka projevila

            by = (dy < 0 ? -dy : dy) / 4;        // Vliv vzdálenosti ve směru Y
            if (by > 40) by = 40;
            if (bx < by) bx = by;                // Pokud jsou prvky Prev a Next od sebe ve směru Y daleko, zvětšíme křivku.

            path.AddBezier(px, py, px + bx, py, nx - bx, ny, nx, ny);
            return path;
        }
        /// <summary>
        /// Vykreslí linku vztahu jako rovnou čáru z bodu pointStart do pointEnd.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="pointStart"></param>
        /// <param name="pointEnd"></param>
        /// <param name="color"></param>
        /// <param name="width"></param>
        /// <param name="startCap"></param>
        /// <param name="endCap"></param>
        /// <param name="ratio"></param>
        internal static void DrawLinkLine(Graphics graphics, Point pointStart, Point pointEnd, Color color, int? width = null,
            System.Drawing.Drawing2D.LineCap startCap = LineCap.Round, System.Drawing.Drawing2D.LineCap endCap = LineCap.ArrowAnchor,
            float? ratio = null)
        {
            Color colorB = color.Morph(Color.Black, 0.80f);

            float width1 = (width.HasValue ? (width.Value < 1 ? 1f : (width.Value > 6 ? 6f : (float)width.Value)) : 1f);

            Pen pen = Skin.Pen(colorB, width1 + 2f, opacityRatio: ratio, startCap: startCap, endCap: endCap);
            graphics.DrawLine(pen, pointStart, pointEnd);

            pen = Skin.Pen(color, width1, opacityRatio: ratio, endCap: endCap);
            graphics.DrawLine(pen, pointStart, pointEnd);
        }
        /// <summary>
        /// Vykreslí linku vztahu jako křivku z bodu pointStart do pointEnd.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="graphicsPath"></param>
        /// <param name="color"></param>
        /// <param name="width"></param>
        /// <param name="startCap"></param>
        /// <param name="endCap"></param>
        /// <param name="ratio"></param>
        internal static void DrawLinkPath(Graphics graphics, System.Drawing.Drawing2D.GraphicsPath graphicsPath, Color color, int? width = null,
            System.Drawing.Drawing2D.LineCap startCap = LineCap.Round, System.Drawing.Drawing2D.LineCap endCap = LineCap.ArrowAnchor,
            float? ratio = null)
        {
            if (graphicsPath == null) return;

            Color colorB = color.Morph(Color.Black, 0.80f);

            float width1 = (width.HasValue ? (width.Value < 1 ? 1f : (width.Value > 6 ? 6f : (float)width.Value)) : 1f);

            Pen pen = Skin.Pen(colorB, width1 + 2f, opacityRatio: ratio, startCap: startCap, endCap: endCap);
            graphics.DrawPath(pen, graphicsPath);

            pen = Skin.Pen(color, width1, opacityRatio: ratio, endCap: endCap);
            graphics.DrawPath(pen, graphicsPath);
        }

        #endregion
        #region Ellipse
        /// <summary>
        /// Returns a rectangle of the ellipse, which is circumscribed by the specified inner area, and distance from edge of inner area.
        /// </summary>
        /// <param name="innerArea"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        internal static Rectangle CreateEllipseAroundRectangle(Rectangle innerArea, int distance)
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
        internal static GraphicsState GraphicsSet(Graphics graphics, GraphicSetting graphicSetting)
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
        internal static GraphicsState GraphicsSetSmooth(Graphics graphics)
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
        internal static GraphicsState GraphicsSetText(Graphics graphics)
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
        internal static GraphicsState GraphicsSetSharp(Graphics graphics)
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
        internal static IDisposable GraphicsUse(Graphics graphics, GraphicSetting graphicSetting)
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
        internal static IDisposable GraphicsUse(Graphics graphics, Rectangle setClip, GraphicSetting graphicSetting)
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
        internal static IDisposable GraphicsUseSmooth(Graphics graphics)
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
        internal static IDisposable GraphicsUseSmooth(Graphics graphics, Rectangle setClip)
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
        internal static IDisposable GraphicsUseText(Graphics graphics)
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
        internal static IDisposable GraphicsUseText(Graphics graphics, Rectangle setClip)
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
        internal static IDisposable GraphicsUseSharp(Graphics graphics)
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
        internal static IDisposable GraphicsUseSharp(Graphics graphics, Rectangle setClip)
        {
            IDisposable state = new GraphicsStateRestore(graphics, setClip);
            graphics.SetClip(setClip);
            _GraphicsSetSharp(graphics);
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
        internal static IDisposable GraphicsUseOpacity(Graphics graphics, int opacity)
        {
            IDisposable state = new GraphicsStateRestore(graphics);
            _GraphicsSetOpacity(graphics, opacity);
            return state;
        }
        /// <summary>
        /// Zajistí oříznutí aktivní plochy v grafice na daný prostor.
        /// Po konci bloku using() bude plocha vrácena na předchozí nastavení.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="setClip"></param>
        /// <returns></returns>
        internal static IDisposable GraphicsClip(Graphics graphics, Rectangle setClip)
        {
            IDisposable state = new GraphicsStateRestore(graphics, setClip);
            graphics.SetClip(setClip);
            return state;
        }
        /// <summary>
        /// Nastaví Graphics tak, aby ideálně kreslil hladké čáry
        /// </summary>
        /// <param name="graphics"></param>
        private static void _GraphicsSetSmooth(Graphics graphics)
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;        // SmoothingMode.AntiAlias poskytuje ideální hladké kreslení grafiky
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;  // Nemá vliv na vykreslování čehokoliv
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;     // TextRenderingHint.AntiAlias vyhovuje pro všechny režimy, a neovlivňuje vykreslení jiné grafiky
        }
        /// <summary>
        /// Nastaví Graphics tak, aby ideálně kreslil text
        /// </summary>
        /// <param name="graphics"></param>
        private static void _GraphicsSetText(Graphics graphics)
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;        // SmoothingMode.AntiAlias poskytuje ideální hladké kreslení grafiky
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;  // Nemá vliv na vykreslování čehokoliv
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;     // TextRenderingHint.AntiAlias je jediný, který garantuje korektní měření textu, což je nezbytné pro ContentAlignement;
        }
        /// <summary>
        /// Nastaví Graphics tak, aby ideálně kreslil ostré linie
        /// </summary>
        /// <param name="graphics"></param>
        private static void _GraphicsSetSharp(Graphics graphics)
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;             // Kreslí jednotlivé pixely
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;     // TextRenderingHint.AntiAlias vyhovuje pro všechny režimy, a neovlivňuje vykreslení jiné grafiky
        }
        /// <summary>
        /// Nastaví Graphics tak, aby pro veškeré následující kreslení použil danou průhlednost
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="opacity"></param>
        private static void _GraphicsSetOpacity(Graphics graphics, int opacity)
        {
            /*  Bohužel tohle WinForm Graphics neumí  */
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
        internal static Brush InteractiveClipBrushForState(GInteractiveState state)
        {
            switch (state)
            {
                case GInteractiveState.None: return InteractiveClipStandardBrush;
                case GInteractiveState.LeftFrame:
                case GInteractiveState.RightFrame:
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
        internal static Brush InteractiveClipStandardBrush
        {
            get
            {
                if (_InteractiveClipStandardBrush == null)
                    _InteractiveClipStandardBrush = new SolidBrush(InteractiveClipStandardColor);
                return _InteractiveClipStandardBrush;
            }
        } private static Brush _InteractiveClipStandardBrush;
        internal static Brush InteractiveClipDisabledBrush
        {
            get
            {
                if (_InteractiveClipDisabledBrush == null)
                    _InteractiveClipDisabledBrush = new SolidBrush(InteractiveClipDisabledColor);
                return _InteractiveClipDisabledBrush;
            }
        } private static Brush _InteractiveClipDisabledBrush;
        internal static Brush InteractiveClipMouseBrush
        {
            get
            {
                if (_InteractiveClipMouseBrush == null)
                    _InteractiveClipMouseBrush = new SolidBrush(InteractiveClipMouseColor);
                return _InteractiveClipMouseBrush;
            }
        } private static Brush _InteractiveClipMouseBrush;
        internal static Brush InteractiveClipDownBrush
        {
            get
            {
                if (_InteractiveClipDownBrush == null)
                    _InteractiveClipDownBrush = new SolidBrush(InteractiveClipDownColor);
                return _InteractiveClipDownBrush;
            }
        } private static Brush _InteractiveClipDownBrush;
        internal static Brush InteractiveClipDragBrush
        {
            get
            {
                if (_InteractiveClipDragBrush == null)
                    _InteractiveClipDragBrush = new SolidBrush(InteractiveClipDragColor);
                return _InteractiveClipDragBrush;
            }
        } private static Brush _InteractiveClipDragBrush;
        internal static Color InteractiveClipStandardColor { get { return Color.DimGray; } }
        internal static Color InteractiveClipDisabledColor { get { return Color.DimGray; } }
        internal static Color InteractiveClipMouseColor { get { return Color.BlueViolet; } }
        internal static Color InteractiveClipDownColor { get { return Color.Black; } }
        internal static Color InteractiveClipDragColor { get { return Color.Blue; } }
        #endregion
    }
    #region interface IScrollBarPaintData : Interface pro vykreslení komplexní struktury Scrollbaru
    /// <summary>
    /// Interface pro vykreslení komplexní struktury Scrollbaru
    /// </summary>
    public interface IScrollBarPaintData
    {
        Orientation Orientation { get; }
        bool IsEnabled { get; }
        Rectangle ScrollBarBounds { get; }
        Color ScrollBarBackColor { get; }
        Rectangle MinButtonBounds { get; }
        GInteractiveState MinButtonState { get; }
        Rectangle DataAreaBounds { get; }
        Rectangle MinAreaBounds { get; }
        GInteractiveState MinAreaState { get; }
        Rectangle MaxAreaBounds { get; }
        GInteractiveState MaxAreaState { get; }
        Rectangle MaxButtonBounds { get; }
        GInteractiveState MaxButtonState { get; }
        Rectangle ThumbButtonBounds { get; }
        GInteractiveState ThumbButtonState { get; }
        Rectangle ThumbImageBounds { get; }
        void UserDataDraw(Graphics graphics, Rectangle bounds);
    }
    #endregion
    #region interface ITabHeaderPaintData : Interface pro vykreslení komplexní struktury TabHeader
    /// <summary>
    /// Interface pro vykreslení komplexní struktury TabHeader
    /// </summary>
    public interface ITabHeaderItemPaintData
    {
        RectangleSide Position { get; }
        bool IsEnabled { get; }
        Color? BackColor { get; }
        bool IsActive { get; }
        GInteractiveState InteractiveState { get; }
        FontInfo Font { get; }
        Image Image { get; }
        Rectangle ImageBounds { get; }
        string Text { get; }
        Rectangle TextBounds { get; }
        bool CloseButtonVisible { get; }
        Rectangle CloseButtonBounds { get; }
        void UserDataDraw(Graphics graphics, Rectangle bounds);
    }
    #endregion
    #region Enums
    /// <summary>
    /// Konfigurace grafiky pro různé vykreslované motivy
    /// </summary>
    public enum GraphicSetting
    {
        None,
        Text,
        Smooth,
        Sharp
    }
    /// <summary>
    /// Relativní pozice dvou obdélníků
    /// </summary>
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
    /// <summary>
    /// Tvary, které generuje metoda <see cref="GPainter.CreatePathLinearShape(LinearShapeType, Rectangle, int)"/>
    /// </summary>
    public enum LinearShapeType
    {
        None,
        LeftArrow,
        UpArrow,
        RightArrow,
        DownArrow,
        HorizontalLines,
        VerticalLines
    }
    internal enum TrackPointerType
    {
        None,
        OneSide,
        DoubleSide,
        HiFi
    }
    [Flags]
    internal enum GraphicsPathPart : int
    {
        None = 0,
        SimpleBorder = 1,
        LightBorder = 2,
        DarkBorder = 4,
        FilledArea = 8
    }
    /// <summary>
    /// Druhy transformací, pro které lze vygenerovat matrix v metodě GetMatrix(MatrixBasicTransformType).
    /// Udává jeden z několika základních druhů transformací.
    /// </summary>
    public enum MatrixTransformationType
    {
        /// <summary>Žádná transformace</summary>
        NoTransform = 0,
        /// <summary>Otočení o 90° = o 90° ve směru hodinových ručiček</summary>
        Rotate90,
        /// <summary>Otočení o 180° = stejné jako dvojité zrcadlení</summary>
        Rotate180,
        /// <summary>Otočení o 270° = o 90° proti směru hodinových ručiček</summary>
        Rotate270,
        /// <summary>Zrcadlení horizontální = zprava doleva a zleva doprava</summary>
        MirrorHorizontal,
        /// <summary>Zrcadlení vertikální = shora dolů, a sdola nahoru</summary>
        MirrorVertical
    }
    #endregion
}
