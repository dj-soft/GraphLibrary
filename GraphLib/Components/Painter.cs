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
    public class Painter
    {
        #region DrawRectangle
        /// <summary>
        /// Vyplní daný prostor danou barvou.
        /// Používá systémový štětec, nevytváří si nový.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="backColor"></param>
        public static void DrawRectangle(Graphics graphics, Rectangle bounds, Color backColor)
        {
            graphics.FillRectangle(Skin.Brush(backColor), bounds);
        }
        #endregion
        #region DrawBorder, DrawSoftBorder
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
            Color colorTop, colorRight, colorBottom, colorLeft;
            _ModifyColorByEffect3D(lineColor, effect3D, out colorTop, out colorRight, out colorBottom, out colorLeft);
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
            int w = bounds.Width;
            int h = bounds.Height;
            if (w <= 0 || h <= 0) return;
            int x0 = bounds.X;
            int y0 = bounds.Y;
            int x1 = x0 + w - 1;
            int y1 = y0 + h - 1;

            if (colorTop.HasValue && sides.HasFlag(RectangleSide.Top) && w > 0)
                graphics.DrawLine(Skin.Pen(colorTop.Value, dashStyle: dashStyle), x0, y0, x1, y0);

            if (colorRight.HasValue && sides.HasFlag(RectangleSide.Right) && h > 0)
                graphics.DrawLine(Skin.Pen(colorRight.Value, dashStyle: dashStyle), x1, y0, x1, y1);
            
            if (colorBottom.HasValue && sides.HasFlag(RectangleSide.Bottom) && w > 0)
                graphics.DrawLine(Skin.Pen(colorBottom.Value, dashStyle: dashStyle), x0, y1, x1, y1);
            
            if (colorLeft.HasValue && sides.HasFlag(RectangleSide.Left) && h > 0)
                graphics.DrawLine(Skin.Pen(colorLeft.Value, dashStyle: dashStyle), x0, y0, x0, y1);
        }
        /// <summary>
        /// Vykreslí Soft verzi okraje prvku.
        /// je vhodné, aby barva <paramref name="lineColor"/> měla hodnotu Alpha menší než 255, pak vypadá vnitřní růžek sytější.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="sides"></param>
        /// <param name="lineColor"></param>
        internal static void DrawSoftBorder(Graphics graphics, Rectangle bounds, RectangleSide sides, Color lineColor)
        {
            int w = bounds.Width;
            int h = bounds.Height;
            if (w <= 0 || h <= 0) return;
            int x0 = bounds.X;
            int y0 = bounds.Y;
            int x1 = x0 + w - 1;
            int y1 = y0 + h - 1;

            var brush = Skin.Brush(lineColor);

            if (sides.HasFlag(RectangleSide.Top) && w > 2)
                graphics.FillRectangle(brush, x0 + 1, y0, w - 2, 2);

            if (sides.HasFlag(RectangleSide.Right) && h > 2)
                graphics.FillRectangle(brush, x1 - 1, y0 + 1, 2, h - 2);

            if (sides.HasFlag(RectangleSide.Bottom) && w > 0)
                graphics.FillRectangle(brush, x0 + 1, y1 - 1, w - 2, 2);

            if (sides.HasFlag(RectangleSide.Left) && h > 0)
                graphics.FillRectangle(brush, x0, y0 + 1, 2, h - 2);
        }
        /// <summary>
        /// Provede modifikaci dané barvy pomocí dodaného 3D efektu, do výsledných barev
        /// </summary>
        /// <param name="color"></param>
        /// <param name="effect3D"></param>
        /// <param name="colorTop"></param>
        /// <param name="colorRight"></param>
        /// <param name="colorBottom"></param>
        /// <param name="colorLeft"></param>
        private static void _ModifyColorByEffect3D(Color color, float? effect3D, out Color colorTop, out Color colorRight, out Color colorBottom, out Color colorLeft)
        {
            colorTop = color;
            colorRight = color;
            colorBottom = color;
            colorLeft = color;
            if (effect3D.HasValue && effect3D.Value != 0f)
            {
                if (effect3D.Value > 0f)
                {   // Vlevo a nahoře bude barva světlejší, vpravo a dole tmavší:
                    colorTop = color.Morph(Skin.Modifiers.Effect3DLight, effect3D.Value);
                    colorRight = color.Morph(Skin.Modifiers.Effect3DDark, effect3D.Value);
                    colorBottom = colorRight;
                    colorLeft = colorTop;
                }
                else
                {   // Vlevo a nahoře bude barva tmavší, vpravo a dole světlejší:
                    colorTop = color.Morph(Skin.Modifiers.Effect3DDark, -effect3D.Value);
                    colorRight = color.Morph(Skin.Modifiers.Effect3DLight, -effect3D.Value);
                    colorBottom = colorRight;
                    colorLeft = colorTop;
                }
            }
        }
        /// <summary>
        /// Vrátí šířku linky borderu daného typu.
        /// Vrací hodnotu 0, 1, nebo 2
        /// </summary>
        /// <param name="borderType"></param>
        /// <returns></returns>
        internal static int GetBorderWidth(BorderType borderType)
        {
            if (borderType.HasFlag(BorderType.Single)) return 1;
            if (borderType.HasFlag(BorderType.Double)) return 2;
            return 0;
        }
        /// <summary>
        /// Vykreslí Border podle parametrů
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="borderColor"></param>
        /// <param name="borderType"></param>
        /// <param name="interactiveState"></param>
        /// <param name="drawSides"></param>
        /// <param name="dashStyle"></param>
        internal static void DrawBorder(Graphics graphics, Rectangle bounds, Color borderColor, BorderType borderType = BorderType.Single, GInteractiveState interactiveState = GInteractiveState.Enabled, RectangleSide drawSides = RectangleSide.All, DashStyle dashStyle = DashStyle.Solid)
        {
            if (drawSides == RectangleSide.None) return;

            // Malé rozměry nekreslíme:
            int w = bounds.Width;
            int h = bounds.Height;
            if (w <= 2 || h <= 2) return;

            int t = GetBorderWidth(borderType);
            if (t <= 0) return;

            // Pokud je zadána vazba barvy na interaktivní stav, vyřešíme ji podle stavu interaktivity: pro neinteraktivní stav buď skončíme, anebo dáme poloviční průhlednost):
            if (!ModifyBorderColorByState(ref borderColor, borderType, interactiveState)) return;

            // Pokud je dán 3D efekt, pak získáme barvu pro LT levý a horní okraj, a barvu RB pro pravý a dolní okraj:
            Color borderColorLT = borderColor;
            Color borderColorRB = borderColor;
            if (!ModifyBorderColorBy3DEffect(borderColor, borderType, interactiveState, out borderColorLT, out borderColorRB)) return;

            // Budeme tedy kreslit:
            int x0 = bounds.X;
            int y0 = bounds.Y;
            int x9 = x0 + w;
            int x8 = x0 + w - 1;
            int y9 = y0 + h;
            int y8 = y0 + h - 1;
            int x1 = x0 + 1;
            int x7 = x8 - 1;
            int x6 = x7 - 1;
            int y1 = y0 + 1;
            int y7 = y8 - 1;
            int y6 = y7 - 1;
            int w2 = w - 2;
            int h2 = h - 2;
            bool isDouble = (t > 1 && w >= 4 && h >= 4);
            if (!isDouble) t = 1;

            // Ostré okraje!
            using (GraphicsUseSharp(graphics))
            {
                if (dashStyle != DashStyle.Solid)
                {   // Nějaké tečkování:
                    if (drawSides.HasFlag(RectangleSide.Left) && h > 0)
                        graphics.DrawLine(Skin.Pen(borderColorLT, dashStyle: dashStyle), x0, y0, x0, y8);

                    if (drawSides.HasFlag(RectangleSide.Top) && w > 0)
                        graphics.DrawLine(Skin.Pen(borderColorLT, dashStyle: dashStyle), x0, y0, x8, y0);

                    if (drawSides.HasFlag(RectangleSide.Right) && h > 0)
                        graphics.DrawLine(Skin.Pen(borderColorRB, dashStyle: dashStyle), x8, y0, x8, y8);

                    if (drawSides.HasFlag(RectangleSide.Bottom) && w > 0)
                        graphics.DrawLine(Skin.Pen(borderColorRB, dashStyle: dashStyle), x0, y8, x8, y8);
                }
                else if (borderType.HasFlag(BorderType.Soft))
                {   // Měkký okraj = vynecháme kreslení do rohových pixelů:
                    if (drawSides.HasFlag(RectangleSide.Left) && h > 2)
                        graphics.FillRectangle(Skin.Brush(borderColorLT), x0, y1, t, h2);

                    if (drawSides.HasFlag(RectangleSide.Top) && w > 2)
                        graphics.FillRectangle(Skin.Brush(borderColorLT), x1, y0, w2, t);

                    if (drawSides.HasFlag(RectangleSide.Right) && h > 2)
                        graphics.FillRectangle(Skin.Brush(borderColorRB), x9 - t, y1, t, h2);

                    if (drawSides.HasFlag(RectangleSide.Bottom) && w > 2)
                        graphics.FillRectangle(Skin.Brush(borderColorRB), x1, y9 - t, w2, t);
                }
                else
                {   // Ostrý okraj = kreslíme i rohové pixely:
                    if (drawSides.HasFlag(RectangleSide.Left))
                    {
                        graphics.FillRectangle(Skin.Brush(borderColorLT), x0, y0, 1, h);
                        if (isDouble) graphics.FillRectangle(Skin.Brush(borderColorLT), x1, y1, 1, h2);
                    }

                    if (drawSides.HasFlag(RectangleSide.Top) && w > 2)
                    {
                        graphics.FillRectangle(Skin.Brush(borderColorLT), x0, y0, w, 1);
                        if (isDouble) graphics.FillRectangle(Skin.Brush(borderColorLT), x1, y1, w2, 1);
                    }

                    if (drawSides.HasFlag(RectangleSide.Right) && h > 2)
                    {
                        graphics.FillRectangle(Skin.Brush(borderColorRB), x8, y0, 1, h);
                        if (isDouble) graphics.FillRectangle(Skin.Brush(borderColorRB), x7, y1, 1, h2);
                    }

                    if (drawSides.HasFlag(RectangleSide.Bottom) && w > 2)
                    {
                        graphics.FillRectangle(Skin.Brush(borderColorRB), x0, y8, w, 1);
                        if (isDouble) graphics.FillRectangle(Skin.Brush(borderColorRB), x1, y7, w2, 1);
                    }
                }
            }
        }
        /// <summary>
        /// Modifikuje barvu okraje podle typu okraje a interaktivity, vrací false když se nemá kreslit.
        /// </summary>
        /// <param name="borderColor"></param>
        /// <param name="borderType"></param>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        private static bool ModifyBorderColorByState(ref Color borderColor, BorderType borderType, GInteractiveState interactiveState)
        {
            bool isInteractiveHalf = borderType.HasFlag(BorderType.InteractiveHalf);
            bool isInteractiveOnly = borderType.HasFlag(BorderType.InteractiveOnly);
            if (isInteractiveHalf || isInteractiveOnly)
            {
                bool isActive = (!interactiveState.HasFlag(GInteractiveState.Disabled) &&
                    (interactiveState.HasFlag(GInteractiveState.Focused) || interactiveState.HasFlag(GInteractiveState.MouseOver) ||
                     interactiveState.HasFlag(GInteractiveState.FlagDown) || interactiveState.HasFlag(GInteractiveState.FlagDrag)));
                if (!isActive)
                {
                    if (isInteractiveOnly) return false;
                    borderColor = borderColor.ApplyOpacity(0.5f);
                }
            }
            return true;
        }
        /// <summary>
        /// Vytvoří barvu <paramref name="borderColorLT"/> pro levý a horní okraj, a barvu <paramref name="borderColorRB"/> pro pravý a dolní okraj, podle výchozí barvy a podle typu borderu a stavu interaktivity.
        /// </summary>
        /// <param name="borderColor"></param>
        /// <param name="borderType"></param>
        /// <param name="interactiveState"></param>
        /// <param name="borderColorLT"></param>
        /// <param name="borderColorRB"></param>
        /// <returns></returns>
        private static bool ModifyBorderColorBy3DEffect(Color borderColor, BorderType borderType, GInteractiveState interactiveState, out Color borderColorLT, out Color borderColorRB)
        {
            borderColorLT = borderColor;
            borderColorRB = borderColor;

            bool is3DDown = borderType.HasFlag(BorderType.Effect3DDown);
            bool is3DUp = borderType.HasFlag(BorderType.Effect3DUp);
            bool is3DInteractive = borderType.HasFlag(BorderType.Effect3DInteractive);

            if (is3DDown || is3DUp || is3DInteractive)
            {
                bool isDown = is3DDown || !is3DUp;                   // Výchozí stav "Dolů" je tehdy, když je explicitně zadáno Down (Effect3DDown), anebo když není zadáno ani Up (Effect3DUp)
                if (is3DInteractive)                                 // Pokud je stav Interaktivně závislý:
                {
                    bool interactiveDown = (!interactiveState.HasFlag(GInteractiveState.Disabled) &&
                            (interactiveState.HasFlag(GInteractiveState.Focused) || /* tady pouhý stav MouseOver nereprezentuje "Down": interactiveState.HasFlag(GInteractiveState.MouseOver) || */
                             interactiveState.HasFlag(GInteractiveState.FlagDown) || interactiveState.HasFlag(GInteractiveState.FlagDrag)));
                    if (interactiveDown) isDown = !isDown;           // Pokud je Interaktivní stav Aktivní ("Zmáčknutý") => pak otočíme aktuální příznak
                }

                IModifierStyle modifier = Styles.Modifier;
                if (isDown)
                {   // Dolů: Levý a Horní je tmavší:
                    borderColorLT = borderColor.Morph(modifier.Border3DColorDark);
                    borderColorRB = borderColor.Morph(modifier.Border3DColorLight);
                }
                else
                {   // Nahoru: Levý a Horní je světlejší:
                    borderColorLT = borderColor.Morph(modifier.Border3DColorLight);
                    borderColorRB = borderColor.Morph(modifier.Border3DColorDark);
                }
            }
            return true;
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

            if (absoluteBounds.Width > 10240) absoluteBounds.Width = 10240;
            if (absoluteBounds.Height > 10240) absoluteBounds.Height = 10240;

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
        #region FillWithGradient
        /// <summary>
        /// Vyplní daný prostor barevným gradientem
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <param name="targetSide"></param>
        internal static void FillWithGradient(Graphics graphics, Rectangle bounds, Color color1, Color color2, RectangleSide targetSide)
        {
            using (var brush = CreateBrushForGradient(bounds, color1, color2, targetSide))
            {
                graphics.FillRectangle(brush, bounds);
            }
        }
        /// <summary>
        /// Vrátí new instanci <see cref="LinearGradientBrush"/> pro dané zadání. Interně řeší problém WinForms, kdy pro určité orientace / úhly gradientu dochází k posunu prostoru.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <param name="targetSide"></param>
        /// <returns></returns>
        internal static LinearGradientBrush CreateBrushForGradient(Rectangle bounds, Color color1, Color color2, RectangleSide targetSide)
        {
            switch (targetSide)
            {
                case RectangleSide.Bottom:
                case RectangleSide.BottomCenter:
                    bounds = bounds.Enlarge(0, 1, 0, 1);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds, color1, color2, LinearGradientMode.Vertical);
                case RectangleSide.Left:
                case RectangleSide.MiddleLeft:
                    bounds = bounds.Enlarge(1, 0, 1, 0);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds, color2, color1, LinearGradientMode.Horizontal);
                case RectangleSide.Top:
                case RectangleSide.TopCenter:
                    bounds = bounds.Enlarge(0, 1, 0, 1);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds, color2, color1, LinearGradientMode.Vertical);
                case RectangleSide.Right:
                case RectangleSide.MiddleRight:
                default:
                    bounds = bounds.Enlarge(1, 0, 1, 0);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds, color1, color2, LinearGradientMode.Horizontal);
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
        /// <summary>
        /// Vrátí danou barvu pozadí modifikovanou pro aktuální stav <see cref="GInteractiveState"/>.
        /// Volitelně je možno tuto modifikaci korigovat parametrem ratio.
        /// </summary>
        /// <param name="backColor"></param>
        /// <param name="interactiveState"></param>
        /// <param name="ratio">Korekce množství modifikace: 0=nemodifikovat vůbec, 0.25 = modifikovat na 25% standardu, 1 (=null) = modifikovat standardně</param>
        /// <returns></returns>
        internal static Color ModifyColorByInteractiveState(Color backColor, GInteractiveState interactiveState, float? ratio = null)
        {
            return Skin.Modifiers.GetBackColorModifiedByInteractiveState(backColor, interactiveState, ratio);
        }
        /// <summary>
        /// Vrátí barvu odpovídající interaktivnímu stavu
        /// </summary>
        /// <param name="interactiveState"></param>
        /// <param name="backColor"></param>
        /// <param name="mouseOverColor"></param>
        /// <param name="mouseDownColor"></param>
        /// <param name="mouseDragColor"></param>
        /// <param name="disabledColor"></param>
        /// <returns></returns>
        internal static Color GetColorByInteractiveState(GInteractiveState interactiveState, Color backColor, Color? mouseOverColor = null, Color? mouseDownColor = null, Color? mouseDragColor = null, Color? disabledColor = null)
        {
            Color? resultColor = null;

            switch (interactiveState)
            {
                case GInteractiveState.Enabled:
                    resultColor = backColor;
                    break;
                case GInteractiveState.Disabled:
                    resultColor = disabledColor;
                    break;
                case GInteractiveState.MouseOver:
                    resultColor = mouseOverColor;
                    break;
                default:
                    if (interactiveState.HasFlag(GInteractiveState.FlagDown))
                        resultColor = mouseDownColor;
                    else if (interactiveState.HasFlag(GInteractiveState.FlagDrag))
                        resultColor = mouseDragColor ?? mouseDownColor;
                    break;
            }
            return (resultColor ?? backColor);
        }
        #endregion
        #region DrawButton
        public static void DrawButtonBase(Graphics graphics, Rectangle bounds, DrawButtonArgs args)
        {
            _DrawButtonBase(graphics, bounds, args);
        }
        private static void _DrawButtonBase(Graphics graphics, Rectangle bounds, DrawButtonArgs args)
        {
            bounds = bounds.Enlarge(0, 0, -1, -1);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            int roundX = args.RoundCorner;
            int roundY = args.RoundCorner;

            using (Painter.GraphicsUseSmooth(graphics))
            {
                if (args.DrawBackground)
                {
                    using (GraphicsPath path = CreatePathRoundRectangle(bounds, roundX, roundY))
                    using (Brush brush = Skin.CreateBrushForBackground(bounds, args.Orientation, args.InteractiveState, true, args.BackColor, args.Opacity, args.MouseTrackPoint))
                    {
                        graphics.FillPath(brush, path);
                    }
                }

                if (args.DrawBorders)
                {
                    Color borderColor = (args.BorderColor.HasValue ? args.BorderColor.Value : Skin.Button.BorderColor);
                    Color borderColorBegin, borderColorEnd;
                    Skin.ModifyBackColorByState(Skin.Button.BorderColor, args.InteractiveState, true, out borderColorBegin, out borderColorEnd);

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
        #endregion
        #region DrawString, DrawStringMeasure, MeasureString, ConvertAlignment
        /// <summary>
        /// Vykreslí daný text do daného prostoru.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="text"></param>
        /// <param name="fontInfo"></param>
        /// <param name="bounds"></param>
        /// <param name="extAlignment"></param>
        /// <param name="outerBounds">Vnější souřadnice prostoru, pro umisťování textu vně daného prostoru (<see cref="ExtendedContentAlignment.PreferOuter"/> atd)</param>
        /// <param name="color"></param>
        /// <param name="brush"></param>
        /// <param name="transformation"></param>
        /// <param name="drawBackground"></param>
        /// <param name="stringFormatFlags"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        public static Rectangle DrawString(Graphics graphics, string text, FontInfo fontInfo, Rectangle bounds, ExtendedContentAlignment extAlignment, Rectangle? outerBounds, Color? color = null, Brush brush = null, MatrixTransformationType? transformation = null, Action<Rectangle> drawBackground = null, StringFormatFlags? stringFormatFlags = null, StringFormat stringFormat = null)
        {
            RectangleF[] positions;
            return _DrawString(graphics, bounds, text, brush, color, fontInfo, extAlignment, outerBounds, transformation, drawBackground, stringFormatFlags, stringFormat, false, out positions);
        }
        /// <summary>
        /// Vykreslí daný text do daného prostoru.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="text"></param>
        /// <param name="fontInfo"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="color"></param>
        /// <param name="brush"></param>
        /// <param name="transformation"></param>
        /// <param name="drawBackground"></param>
        /// <param name="stringFormatFlags"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        public static Rectangle DrawString(Graphics graphics, string text, FontInfo fontInfo, Rectangle bounds, ContentAlignment alignment, Color? color = null, Brush brush = null, MatrixTransformationType? transformation = null, Action<Rectangle> drawBackground = null, StringFormatFlags? stringFormatFlags = null, StringFormat stringFormat = null)
        {
            ExtendedContentAlignment extAlignment = ConvertAlignment(alignment);
            RectangleF[] positions;
            return _DrawString(graphics, bounds, text, brush, color, fontInfo, extAlignment, null, transformation, drawBackground, stringFormatFlags, stringFormat, false, out positions);
        }
        /// <summary>
        /// Vykreslí daný text do daného prostoru. Změří a vrátí souřadnice. Bohužel ve WinForm to není úplně přesné.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="text"></param>
        /// <param name="fontInfo"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="color"></param>
        /// <param name="brush"></param>
        /// <param name="transformation"></param>
        /// <param name="drawBackground"></param>
        /// <param name="stringFormatFlags"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        internal static RectangleF[] DrawStringMeasureChars(Graphics graphics, string text, FontInfo fontInfo, Rectangle bounds, ContentAlignment alignment, Color? color = null, Brush brush = null, MatrixTransformationType? transformation = null, Action<Rectangle> drawBackground = null, StringFormatFlags? stringFormatFlags = null, StringFormat stringFormat = null)
        {
            ExtendedContentAlignment extAlignment = ConvertAlignment(alignment);
            RectangleF[] positions;
            _DrawString(graphics, bounds, text, brush, color, fontInfo, extAlignment, null, transformation, drawBackground, stringFormatFlags, stringFormat, true, out positions);
            return positions;
        }
        /// <summary>
        /// Obsahuje formátovací příznaky pro psaní textu
        /// </summary>
        internal static StringFormatFlags DrawStringStandardFormatFlags { get { return FontManagerInfo.StandardStringFormatFlags; } }
        /// <summary>
        /// Vykreslí daný text do daného prostoru.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="text"></param>
        /// <param name="brush"></param>
        /// <param name="color"></param>
        /// <param name="fontInfo"></param>
        /// <param name="extAlignment"></param>
        /// <param name="outerBounds"></param>
        /// <param name="transformation"></param>
        /// <param name="drawBackground"></param>
        /// <param name="stringFormatFlags"></param>
        /// <param name="stringFormat"></param>
        /// <param name="measureChars"></param>
        /// <param name="positions"></param>
        private static Rectangle _DrawString(Graphics graphics, Rectangle bounds, string text, Brush brush, Color? color, FontInfo fontInfo, ExtendedContentAlignment extAlignment, Rectangle? outerBounds, MatrixTransformationType? transformation, Action<Rectangle> drawBackground, StringFormatFlags? stringFormatFlags, StringFormat stringFormat, bool measureChars, out RectangleF[] positions)
        {
            positions = null;

            Rectangle textArea = new Rectangle(bounds.X, bounds.Y, 0, 0);
            if (fontInfo == null || String.IsNullOrEmpty(text)) return textArea;
            if (bounds.Width <= 0 || bounds.Height <= 0) return textArea;

            if (stringFormat == null)
            {
                StringFormatFlags sff = stringFormatFlags ?? DrawStringStandardFormatFlags;
                stringFormat = FontManagerInfo.CreateNewStandardStringFormat(sff);
            }

            using (GraphicsUseText(graphics))    // Nedávej tady CLIP na grafiku pro bounds: ona grafika už touhle dobou je korektně clipnutá na správný prostor controlu. Clipnutím na bounds se může část textu vykreslit i mimo control !!!
            {
                bool isVertical = (transformation.HasValue && (transformation.Value == MatrixTransformationType.Rotate90 || transformation.Value == MatrixTransformationType.Rotate270));
                ExtendedContentAlignmentState alignState = new ExtendedContentAlignmentState(extAlignment);
                int boundsLength = _GetMaximalAlignLength(bounds, alignState, 1, outerBounds, isVertical);

                // Několik poznámek k měření velikosti textu pomocí Graphics:
                // 1. graphics.MeasureString() NEREAGUJE na hodnotu graphics.TextRenderingHint: ať nastavím jaký Hint chci, výsledek měření bude vždy stejný
                // 2. graphics.DrawString() REAGUJE na graphics.TextRenderingHint: 
                //   a) vzhledově ideální je ClearTypeGridFit nebo SystemDefault, ale velikost textu je jiná než bylo změřeno v graphics.MeasureString()
                //   b) přesnost je správná při AntiAlias, kdy je text vizuálně vykreslen přesně do prostoru, který byl změřen
                //   c) pokud chci použít ideální vzhled (SystemDefault), pak bych musel rozměr Width získaný v graphics.MeasureString() korigovat konstantou X, závislou na konkrétním fontu.

                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;         // Nebude přesně zarovnávat Align = RightToLeft, ale všechna písmena budou čitelnější.
                graphics.SmoothingMode = SmoothingMode.None;
                graphics.InterpolationMode = InterpolationMode.Default;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                Font font = fontInfo.Font;
                SizeF textSize = graphics.MeasureString(text, font, boundsLength, stringFormat);
                if (isVertical) textSize = textSize.Swap();               // Pro vertikální text převedu prostor textu "na výšku"

                textArea = _AlignContentToBounds(textSize, alignState, bounds, 1, true, outerBounds);


                // test only
                //      graphics.FillRectangle(Skin.Brush(Color.BlueViolet), textArea);



                if (drawBackground != null)
                    drawBackground(textArea);                             // UserDraw pozadí pod textem: v nativní orientaci

                Matrix matrixOld = null;
                if (isVertical)
                {
                    textArea = textArea.Swap();                           // Pro vertikální text převedu prostor textu "na šířku", protože otáčet "na výšku" ho bude Matrix aplikovaný do Graphics
                    matrixOld = graphics.Transform;
                    graphics.Transform = GetMatrix(transformation.Value, textArea);
                }

                if (brush != null)
                    graphics.DrawString(text, font, brush, textArea, stringFormat);
                else if (color.HasValue)
                    //graphics.DrawString(text, font, Skin.Brush(color.Value), textArea, stringFormat);
                    graphics.DrawString(text, font, Skin.Brush(color.Value), textArea.X, textArea.Y, stringFormat);
                else
                    graphics.DrawString(text, font, SystemBrushes.ControlText, textArea, stringFormat);

                if (measureChars)
                    positions = _DrawStringMeasurePositions(graphics, text, font, textArea, stringFormat);

                if (isVertical)
                    graphics.Transform = matrixOld;
            }

            return textArea;
        }
        /// <summary>
        /// Vrátí souřadnice, na kterých jsou vykresleny jednotlivé znaky při kreslení textu.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="textArea"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static RectangleF[] _DrawStringMeasurePositions(Graphics graphics, string text, Font font, Rectangle textArea, StringFormat stringFormat)
        {
            if (text == null || text.Length == 0) return new RectangleF[0];

            //  Někdo (C#? .NET? WinForms? Bill Gates?) nám nachystal lahůdku při určování souřadnic vykreslovaných znaků!
            // 1. Nelze snadno získat pole souřadnic pro jednotlivé znaky
            // 2. Lze získat pole souřadnic pro zadané rozsahy znaků (např. znaky 0-5 a 12-17 a 18-20),
            // 3. Lze tedy získat pole souřadnic i pro jednotlivé znaky (0-1, 1-2, 2-3, 3-4, ...), ale těch souřadnic NESMÍ BÝT 32 A VÍCE !!! v jednom chodu!
            // 4. Pokud tedy máme text delší než 32 znaků, musíme pole souřadnic získávat v oddělených dávkách po max 32 znacích!!!

            List<RectangleF> result = new List<RectangleF>();

            int length = text.Length;
            int begin = 0;
            int maxSize = 32;
            try
            {
                while (begin < length)
                {   // Cyklus přes jednotlivé dávky, kde jedna dávka má nejvýše (maxSize) prvků typu CharacterRange:
                    List<CharacterRange> characterRanges = new List<CharacterRange>();
                    for (int i = begin; (i < length && characterRanges.Count < maxSize); i++)
                        characterRanges.Add(new CharacterRange(i, 1));
                    using (StringFormat sFormat = stringFormat.Clone() as StringFormat)
                    {
                        sFormat.SetMeasurableCharacterRanges(characterRanges.ToArray());
                        Region[] charRanges = graphics.MeasureCharacterRanges(text, font, textArea, sFormat);
                        foreach (Region charRange in charRanges)
                            result.Add(charRange.GetBounds(graphics));
                    }
                    begin += characterRanges.Count;
                }
            }
            catch (Exception exc)
            {
                string msg = exc.Message + "\r\n" + exc.StackTrace;
                result.Clear();
            }
            return result.ToArray();
        }
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
            float width = font.Size * (float)text.Length + 6f;       // Přidávám 6px na šířku, protože toto měření opravdu není přesné...
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
            Size size = sizeF.Enlarge(4f, 3f).ToSize();              // Přidávám 4px na šířku, protože při kreslení (uvnitř metody _DrawString(), když volám _AlignContentToBounds()) přidávám 2 x 1px okraje! A další 2px jsou rezerva.
            return size;
        }
        #endregion
        #region Zarovnání obsahu do daného prostoru
        /// <summary>
        /// Metoda vrátí <see cref="ExtendedContentAlignment"/> pro daný <see cref="ContentAlignment"/>
        /// </summary>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static ExtendedContentAlignment ConvertAlignment(ContentAlignment alignment)
        {
            switch (alignment)
            {
                case ContentAlignment.TopLeft: return ExtendedContentAlignment.InnerLeftTop;
                case ContentAlignment.TopCenter: return ExtendedContentAlignment.InnerMiddleTop;
                case ContentAlignment.TopRight: return ExtendedContentAlignment.InnerRightTop;
                case ContentAlignment.MiddleLeft: return ExtendedContentAlignment.InnerLeft;
                case ContentAlignment.MiddleCenter: return ExtendedContentAlignment.Center;
                case ContentAlignment.MiddleRight: return ExtendedContentAlignment.InnerRight;
                case ContentAlignment.BottomLeft: return ExtendedContentAlignment.InnerLeftBottom;
                case ContentAlignment.BottomCenter: return ExtendedContentAlignment.InnerBottom;
                case ContentAlignment.BottomRight: return ExtendedContentAlignment.InnerRightBottom;
            }
            return ExtendedContentAlignment.Center;
        }
        /// <summary>
        /// Metoda vrátí <see cref="ExtendedContentAlignment"/> pro daný <see cref="Noris.LCS.Base.WorkScheduler.GuiTextPosition"/>
        /// </summary>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static ExtendedContentAlignment ConvertGuiAlignment(Noris.LCS.Base.WorkScheduler.GuiTextPosition alignment)
        {
            // Využíváme toho, že oba enumy mají identické číselné hodnoty na dolních 12 bitech:
            return (ExtendedContentAlignment)(((int)alignment) & 0x0FFF);
        }
        /// <summary>
        /// Vrátí souřadnice prostoru o dané velikosti <paramref name="size"/>, zarovnané ve stylu <paramref name="alignment"/> do daného prostoru <paramref name="bounds"/>.
        /// </summary>
        /// <param name="size">Velikost obsahu</param>
        /// <param name="alignment">Definice zarovnání obsahu</param>
        /// <param name="bounds">Souřadnice prostoru</param>
        /// <param name="margins">Okraj, počet pixelů, 0 nebo kladné číslo</param>
        /// <param name="cropSize">Zmenšit velikost do disponibilního prostoru</param>
        /// <param name="outerBounds">Vnější prostor pro umísťování obsahu vně daného prostoru</param>
        /// <returns></returns>
        public static Rectangle AlignContentToBounds(SizeF size, ExtendedContentAlignment alignment, Rectangle bounds, int margins = 0, bool cropSize = false, Rectangle? outerBounds = null)
        {
            ExtendedContentAlignmentState alignState = new ExtendedContentAlignmentState(alignment);
            return _AlignContentToBounds(Size.Ceiling(size), alignState, bounds, margins, cropSize, outerBounds);
        }
        /// <summary>
        /// Vrátí souřadnice prostoru o dané velikosti <paramref name="size"/>, zarovnané ve stylu <paramref name="alignment"/> do daného prostoru <paramref name="bounds"/>.
        /// </summary>
        /// <param name="size">Velikost obsahu</param>
        /// <param name="alignment">Definice zarovnání obsahu</param>
        /// <param name="bounds">Souřadnice prostoru</param>
        /// <param name="margins">Okraj, počet pixelů, 0 nebo kladné číslo</param>
        /// <param name="cropSize">Zmenšit velikost do disponibilního prostoru</param>
        /// <param name="outerBounds">Vnější prostor pro umísťování obsahu vně daného prostoru</param>
        /// <returns></returns>
        public static Rectangle AlignContentToBounds(Size size, ExtendedContentAlignment alignment, Rectangle bounds, int margins = 0, bool cropSize = false, Rectangle? outerBounds = null)
        {
            ExtendedContentAlignmentState alignState = new ExtendedContentAlignmentState(alignment);
            return _AlignContentToBounds(size, alignState, bounds, margins, cropSize, outerBounds);
        }
        /// <summary>
        /// Vrátí souřadnice prostoru o dané velikosti <paramref name="size"/>, zarovnané dovnitř ve stylu <paramref name="alignment"/> do daného prostoru <paramref name="bounds"/>.
        /// </summary>
        /// <param name="size">Velikost obsahu</param>
        /// <param name="alignment">Definice zarovnání obsahu</param>
        /// <param name="bounds">Souřadnice prostoru</param>
        /// <param name="margins">Okraj, počet pixelů, 0 nebo kladné číslo</param>
        /// <param name="cropSize">Zmenšit velikost do disponibilního prostoru</param>
        /// <returns></returns>
        public static Rectangle AlignContentToBoundsInner(Size size, ExtendedContentAlignment alignment, Rectangle bounds, int margins = 0, bool cropSize = false)
        {
            ExtendedContentAlignmentState alignState = new ExtendedContentAlignmentState(alignment);
            if (margins < 0) margins = 0;
            return _AlignContentToBoundsInner(size, alignState, bounds, margins, cropSize);
        }
        /// <summary>
        /// Vrátí souřadnice prostoru o dané velikosti <paramref name="size"/>, zarovnané dovnitř ve stylu <paramref name="alignState"/> do daného prostoru <paramref name="bounds"/>.
        /// </summary>
        /// <param name="size">Velikost obsahu</param>
        /// <param name="alignState">Definice zarovnání obsahu</param>
        /// <param name="bounds">Souřadnice prostoru</param>
        /// <param name="margins">Okraj, počet pixelů, 0 nebo kladné číslo</param>
        /// <param name="cropSize">Zmenšit velikost do disponibilního prostoru</param>
        /// <param name="outerBounds">Vnější prostor pro umísťování obsahu vně daného prostoru</param>
        /// <returns></returns>
        private static Rectangle _AlignContentToBounds(SizeF size, ExtendedContentAlignmentState alignState, Rectangle bounds, int margins, bool cropSize, Rectangle? outerBounds)
        {
            return _AlignContentToBounds(Size.Ceiling(size), alignState, bounds, margins, cropSize, outerBounds);
        }
        /// <summary>
        /// Vrátí souřadnice prostoru o dané velikosti <paramref name="size"/>, zarovnané dovnitř ve stylu <paramref name="alignState"/> do daného prostoru <paramref name="bounds"/>.
        /// </summary>
        /// <param name="size">Velikost obsahu</param>
        /// <param name="alignState">Definice zarovnání obsahu</param>
        /// <param name="bounds">Souřadnice prostoru</param>
        /// <param name="margins">Okraj, počet pixelů, 0 nebo kladné číslo</param>
        /// <param name="cropSize">Zmenšit velikost do disponibilního prostoru</param>
        /// <param name="outerBounds">Vnější prostor pro umísťování obsahu vně daného prostoru</param>
        /// <returns></returns>
        private static Rectangle _AlignContentToBounds(Size size, ExtendedContentAlignmentState alignState, Rectangle bounds, int margins, bool cropSize, Rectangle? outerBounds)
        {
            if (margins < 0) margins = 0;
            int margins2 = 2 * margins;

            // Vejde se daný obsah do daného prostoru?
            bool fitInside = (alignState.OnlyInner ? true :
                             (alignState.OuterHorizontal ? (size.Width <= (bounds.Width - margins2)) :
                             (alignState.OuterVertical ? (size.Height <= (bounds.Height - margins2)) : true)));

            // Pokud MUSÍME dát obsah dovnitř, anebo pokud jej MŮŽEME dát dovnitř a aktuálně je to možné, anebo pokud NENÍ definované žádné umístění venku, pak umístíme obsah DOVNITŘ:
            if (alignState.OnlyInner || (alignState.PreferInner && fitInside) || !alignState.OuterAny)
                return _AlignContentToBoundsInner(size, alignState, bounds, margins, cropSize);

            // Měli bychom obsah dát zvenku k danému prostoru (vnější pozice je definovaná - alignState.OuterAny je true), ale i ten vnější prostor může být omezen:
            // Pokud NENÍ vnější prostor omezen, anebo pokud MUSÍME dát obsah vně, anebo pokud MÁME PREFEROVAT umístění vně a obsah se vejde, pak umístíme obsah VNĚ:
            if (!outerBounds.HasValue || alignState.OnlyOuter || (alignState.PreferInner && !fitInside) || (alignState.PreferOuter && outerBounds.HasValue && _FitContentToOuterBounds(size, alignState, bounds, margins2, outerBounds.Value)))
                return _AlignContentToBoundsOuter(size, alignState, bounds, margins, cropSize);

            // Dáme obsah dovnit:
            return _AlignContentToBoundsInner(size, alignState, bounds, margins, cropSize);
        }
        /// <summary>
        /// Zjistí, zda se obsah s danou velikostí <paramref name="size"/> vejde vně daného prostoru <paramref name="bounds"/> = do meziprostoru ku <paramref name="outerBounds"/>.
        /// Volitelně může upravit režim umístění (prohodit OuterLeft a Right, nebo Top a Bottom), pokud je povoleno <see cref="ExtendedContentAlignment.CanSwapOuter"/>.
        /// </summary>
        /// <param name="size">Velikost obsahu</param>
        /// <param name="alignState">Definice zarovnání obsahu</param>
        /// <param name="bounds">Souřadnice prostoru</param>
        /// <param name="margins2">Okraje, dvojnásobek = prostor na začátku + konci, počet pixelů, 0 nebo kladné číslo</param>
        /// <param name="outerBounds">Vnější prostor pro umísťování obsahu vně daného prostoru</param>
        /// <returns></returns>
        private static bool _FitContentToOuterBounds(Size size, ExtendedContentAlignmentState alignState, Rectangle bounds, int margins2, Rectangle outerBounds)
        {
            if (alignState.OuterHorizontal)
            {
                int sizeL = (bounds.Left - outerBounds.Left - margins2);
                int sizeR = (outerBounds.Right - bounds.Right - margins2);
                if (alignState.OuterLeft)
                {
                    if (size.Width <= sizeL)
                        return true;
                    if (alignState.CanSwapOuter && size.Width <= sizeR)
                    {
                        alignState.OuterLeft = false;
                        alignState.OuterRight = true;
                        return true;
                    }
                }
                else if (alignState.OuterRight)
                {
                    if (size.Width <= sizeR)
                        return true;
                    if (alignState.CanSwapOuter && size.Width <= sizeL)
                    {
                        alignState.OuterRight = false;
                        alignState.OuterLeft = true;
                        return true;
                    }
                }
                return false;
            }

            if (alignState.OuterVertical)
            {
                int sizeT = (bounds.Top - outerBounds.Top - margins2);
                int sizeB = (outerBounds.Bottom - bounds.Bottom - margins2);
                if (alignState.OuterTop)
                {
                    if (size.Height <= sizeT)
                        return true;
                    if (alignState.CanSwapOuter && size.Height <= sizeB)
                    {
                        alignState.OuterTop = false;
                        alignState.OuterBottom = true;
                        return true;
                    }
                    return false;
                }

                if (alignState.OuterBottom)
                {
                    if (size.Height <= sizeB)
                        return true;
                    if (alignState.CanSwapOuter && size.Height <= sizeT)
                    {
                        alignState.OuterBottom = false;
                        alignState.OuterTop = true;
                        return true;
                    }
                    return false;
                }
            }

            return false;
        }
        /// <summary>
        /// Vrátí souřadnice prostoru o dané velikosti <paramref name="size"/>, zarovnané dovnitř ve stylu <paramref name="alignState"/> do daného prostoru <paramref name="bounds"/>.
        /// </summary>
        /// <param name="size">Velikost obsahu</param>
        /// <param name="alignState">Režim zarovnání</param>
        /// <param name="bounds">Souřadnice prostoru</param>
        /// <param name="margins">Okraj, počet pixelů, 0 nebo kladné číslo</param>
        /// <param name="cropSize">Zmenšit velikost do disponibilního prostoru</param>
        /// <returns></returns>
        private static Rectangle _AlignContentToBoundsInner(Size size, ExtendedContentAlignmentState alignState, Rectangle bounds, int margins, bool cropSize = false)
        {
            int margins2 = 2 * margins;
            int bw = bounds.Width - margins2;
            int bh = bounds.Height - margins2;
            int cw = (cropSize && size.Width > bw ? bw : size.Width);
            int ch = (cropSize && size.Height > bh ? bh : size.Height);
            int cx = _AlignContentInnerOne(bounds.X, (bw - cw), alignState.InnerLeft, alignState.InnerRight);
            int cy = _AlignContentInnerOne(bounds.Y, (bh - ch), alignState.InnerTop, alignState.InnerBottom);
            return new Rectangle(cx, cy, cw, ch);
        }
        /// <summary>
        /// Vrátí počáteční souřadnici prostoru dle počátku prostoru, volného prostru a zarovnání
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="space"></param>
        /// <param name="toBegin"></param>
        /// <param name="toEnd"></param>
        /// <returns></returns>
        private static int _AlignContentInnerOne(int begin, int space, bool toBegin, bool toEnd)
        {
            if (toBegin && !toEnd) return begin;                     // Left nebo Top: vracíme Begin = Left nebo Top
            if (!toBegin && toEnd) return begin + space;             // Right nebo Bottom: vracíme (Begin + Space) = tak, aby End = Right nebo Bottom
            return begin + space / 2;                                // Jinak je to Center: vracíme (Begin + 1/2 Space) = tak, aby Střed = Center
        }
        /// <summary>
        /// Vrátí souřadnice prostoru o dané velikosti <paramref name="size"/>, zarovnané vně daného prostoru <paramref name="bounds"/> ve stylu <paramref name="alignState"/>.
        /// </summary>
        /// <param name="size">Velikost obsahu</param>
        /// <param name="alignState">Režim zarovnání</param>
        /// <param name="bounds">Souřadnice prostoru</param>
        /// <param name="margins">Okraj, počet pixelů, 0 nebo kladné číslo</param>
        /// <param name="cropSize">Zmenšit velikost do disponibilního prostoru</param>
        /// <returns></returns>
        private static Rectangle _AlignContentToBoundsOuter(Size size, ExtendedContentAlignmentState alignState, Rectangle bounds, int margins, bool cropSize)
        {
            int margins2 = 2 * margins;
            int bx = bounds.X;
            int by = bounds.Y;
            int bw = bounds.Width;
            int bh = bounds.Height;

            int cx = bx;
            int cy = by;
            int cw = size.Width;
            int ch = size.Height;
            if (alignState.OuterHorizontal)
            {
                cy = by + margins + ((bh - ch) / 2);
                if (alignState.OuterLeft)
                    cx = bx - margins - cw;
                else if (alignState.OuterRight)
                    cx = bx + bw + margins;
            }
            else if (alignState.OuterVertical)
            {
                cx = bx + margins + ((bw - cw) / 2);
                if (alignState.OuterTop)
                    cy = by - margins - ch;
                else if (alignState.OuterBottom)
                    cy = by + bh + margins;
            }
            return new Rectangle(cx, cy, cw, ch);
        }
        /// <summary>
        /// Metoda se zorientuje v daných souřadnicích (daný prostor <paramref name="bounds"/> a režim zarovnání <paramref name="alignState"/> plus vnější prostor <paramref name="outerBounds"/>),
        /// najde a vrátí největší počet pixelů, na které je možno umístit text.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="alignState"></param>
        /// <param name="margins"></param>
        /// <param name="outerBounds"></param>
        /// <param name="isVertical"></param>
        /// <returns></returns>
        private static int _GetMaximalAlignLength(Rectangle bounds, ExtendedContentAlignmentState alignState, int margins, Rectangle? outerBounds, bool isVertical)
        {
            if (margins < 0) margins = 0;
            int margins2 = 2 * margins;

            int wi = bounds.Width - margins2;
            int hi = bounds.Height - margins2;

            // Pokud musím být uvnitř, anebo není kam jít ven, pak je to snadné = velikost určím jen podle bounds (šířka/výška) mínus okraje:
            if (alignState.OnlyInner || !alignState.OuterAny) return (!isVertical ? wi : hi);

            // Pokud tedy smím jít ven, a venkovní prostor je neomezený, pak je to Max:
            if (!outerBounds.HasValue) return 10240;

            // Tedy smím jít i ven z bounds, a vnější souřadnice jsou dány:
            if (alignState.OuterHorizontal)
            {   // Umístění i vně bounds vlevo nebo vpravo
                if (isVertical) return hi;                                               // Svislý text, ale umístěný i vně souřadnic vlevo/vpravo

                int wl = bounds.Left - outerBounds.Value.Left - margins2;                // Prostor vlevo
                int wr = outerBounds.Value.Right - bounds.Right - margins2;              // Prostor vpravo
                int wo = (wl > wr ? wl : wr);                                            // Vnější, větší

                if (alignState.OnlyOuter)
                {   // Pouze vnější prostor:
                    if (alignState.CanSwapOuter) return wo;
                    if (alignState.OuterLeft) return wl;
                    if (alignState.OuterRight) return wr;
                }
                // Mohu si vybrat vnitřní nebo vnější (protože OnlyInner bylo vyřízeno na začátku):
                if (alignState.CanSwapOuter) return (wo > wi ? wo : wi);
                if (alignState.OuterLeft) return (wl > wi ? wl : wi);
                if (alignState.OuterRight) return (wr > wi ? wr : wi);

                return wi;
            }
            if (alignState.OuterVertical && !isVertical)
            {   // Umístění i vně bounds nahoru nebo dolů, text normální v řádku:
                int wo = outerBounds.Value.Width - margins2;
                return (wi > wo ? wi : wo);
            }
            if (alignState.OuterVertical && isVertical)
            {   // Umístění i vně bounds nahoru nebo dolů, text svislý:
                int ht = bounds.Top - outerBounds.Value.Top - margins2;                  // Prostor nahoře
                int hb = outerBounds.Value.Bottom - bounds.Bottom - margins2;            // Prostor dole
                int ho = (ht > hb ? ht : hb);                                            // Vnější, větší

                if (alignState.OnlyOuter)
                {   // Pouze vnější prostor:
                    if (alignState.CanSwapOuter) return ho;
                    if (alignState.OuterTop) return ht;
                    if (alignState.OuterBottom) return hb;
                }
                // Mohu si vybrat vnitřní nebo vnější (protože OnlyInner bylo vyřízeno na začátku):
                if (alignState.CanSwapOuter) return (ho > hi ? ho : hi);
                if (alignState.OuterTop) return (ht > hi ? ht : hi);
                if (alignState.OuterBottom) return (hb > hi ? hb : hi);

                return hi;
            }

            return (!isVertical ? wi : hi);
        }
        /// <summary>
        /// Rozpad enumu <see cref="ExtendedContentAlignment"/>, aby se provedl pouze 1x
        /// </summary>
        private class ExtendedContentAlignmentState
        {
            public ExtendedContentAlignmentState(ExtendedContentAlignment alignment)
            {
                InnerLeft = ((alignment & ExtendedContentAlignment.InnerLeft) != 0);
                InnerRight = ((alignment & ExtendedContentAlignment.InnerRight) != 0);
                InnerTop = ((alignment & ExtendedContentAlignment.InnerTop) != 0);
                InnerBottom = ((alignment & ExtendedContentAlignment.InnerBottom) != 0);

                OuterLeft = ((alignment & ExtendedContentAlignment.OuterLeft) != 0);
                OuterRight = ((alignment & ExtendedContentAlignment.OuterRight) != 0);
                OuterTop = ((alignment & ExtendedContentAlignment.OuterTop) != 0);
                OuterBottom = ((alignment & ExtendedContentAlignment.OuterBottom) != 0);

                bool outerDefined = OuterAny;

                PreferInner = (outerDefined && (alignment & ExtendedContentAlignment.PreferInner) != 0);
                OnlyOuter = (outerDefined && (alignment & ExtendedContentAlignment.OnlyOuter) != 0);
                PreferOuter = (outerDefined && (alignment & ExtendedContentAlignment.PreferOuter) != 0);
                AllowedOuter = (PreferInner || OnlyOuter || PreferOuter);

                CanSwapOuter = ((alignment & ExtendedContentAlignment.CanSwapOuter) != 0);
            }
            /// <summary>
            /// K levému vnitřnímu okraji.
            /// </summary>
            public bool InnerLeft;
            /// <summary>
            /// K pravému vnitřnímu okraji.
            /// </summary>
            public bool InnerRight;
            /// <summary>
            /// K hornímu vnitřnímu okraji.
            /// </summary>
            public bool InnerTop;
            /// <summary>
            /// K dolnímu vnitřnímu okraji.
            /// </summary>
            public bool InnerBottom;

            /// <summary>
            /// K levému okraji zvenku.
            /// </summary>
            public bool OuterLeft;
            /// <summary>
            /// K pravému okraji zvenku.
            /// </summary>
            public bool OuterRight;
            /// <summary>
            /// K hornímu okraji zvenku = nad daný prostor
            /// </summary>
            public bool OuterTop;
            /// <summary>
            /// K dolnímu okraji zvenku = pod daný prostor.
            /// </summary>
            public bool OuterBottom;
            /// <summary>
            /// Vnější okraj vlevo nebo vpravo
            /// </summary>
            public bool OuterHorizontal { get { return OuterLeft || OuterRight; } }
            /// <summary>
            /// Vnější okraj nahoře nebo dole
            /// </summary>
            public bool OuterVertical { get { return OuterTop || OuterBottom; } }
            /// <summary>
            /// Vnější okraj kdekoli.
            /// True, pokud je zadán, pak má význam vůbec začít řešit umístění "vně"
            /// </summary>
            public bool OuterAny { get { return OuterLeft || OuterRight || OuterTop || OuterBottom; } }

            /// <summary>
            /// Pouze uvnitř prostoru, i kdyby se dovnitř nevešel.
            /// Tedy true, když <see cref="AllowedOuter"/> je false.
            /// </summary>
            public bool OnlyInner { get { return !AllowedOuter; } }
            /// <summary>
            /// Je možno nebo nutno jít vně daného prostoru (tzn. některá z hodnot <see cref="PreferInner"/> nebo <see cref="PreferOuter"/> nebo <see cref="OnlyOuter"/> je true)
            /// </summary>
            public bool AllowedOuter;
            /// <summary>
            /// Nejprve uvnitř prostoru, ale pokud se dovnitř nevejde pak je možno použít vnější umístění podle 
            /// <see cref="OuterLeft"/>, <see cref="OuterRight"/>, <see cref="OuterTop"/>, <see cref="OuterBottom"/>.
            /// Pokud ale nebude nic z toho specifikováno, nebude se text umísťovat Outer.
            /// </summary>
            public bool PreferInner;
            /// <summary>
            /// Pouze vně daného prostoru, nikdy ne dovnitř.
            /// Musí být zadáno něco z <see cref="OuterLeft"/>, <see cref="OuterRight"/>, <see cref="OuterTop"/>, <see cref="OuterBottom"/>.
            /// Pokud nebude nic z toho specifikováno, bude text umístěn Inner.
            /// </summary>
            public bool OnlyOuter;
            /// <summary>
            /// Neprve vně daného prostoru, podle hodnot <see cref="OuterLeft"/>, <see cref="OuterRight"/>, <see cref="OuterTop"/>, <see cref="OuterBottom"/>.
            /// Pokud se nevejde vně prostoru (který musí být něčím určen!), teprve pak se umisťuje uvnitř.
            /// </summary>
            public bool PreferOuter;

            /// <summary>
            /// Povolení k přemístění u vnějšího prostoru: pokud bude např. specifikována pozice <see cref="OuterLeft"/> a vnější prostor bude omezen tak, že obsah se nevejde doleva od daného prostoru,
            /// pak <see cref="CanSwapOuter"/> způsobí, že otestujeme pozici <see cref="OuterRight"/> (tedy místo vlevo od objektu dáme popisek doprava) a případně ji využijeme.
            /// Pokud ani vpravo nebude místo, pak můžeme přejít dovnitř (pokud bude dáno <see cref="PreferOuter"/> = nejprve vnější, a pak vnitřní pozice).
            /// </summary>
            public bool CanSwapOuter;
        }
        #endregion
        #region DrawTableText
        /// <summary>
        /// Metoda vykreslí danou textovou tabulku
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="tableText"></param>
        /// <param name="tableBackColor"></param>
        public static void DrawTableText(Graphics graphics, Rectangle boundsAbsolute, TableText tableText, Color? tableBackColor = null)
        {
            using (GraphicsClip(graphics, boundsAbsolute))
            {
                Color backColor = tableBackColor ?? tableText.BackColor ?? Skin.Control.ControlBackColor;
                Painter.DrawAreaBase(graphics, boundsAbsolute, backColor, System.Windows.Forms.Orientation.Horizontal, GInteractiveState.Enabled);

                if (tableText != null && tableText.Rows.Count > 0)
                {
                    if (tableText.NeedMeasure) tableText.TextMeasure(graphics);

                    List<Data.Int32Range> columns = tableText.ColumnRanges;
                    int count = columns.Count;
                    int y = 2;
                    foreach (var row in tableText.Rows)
                    {
                        int height = row.CurrentSize.Value.Height;
                        int c = row.Cells.Count;
                        if (c > count) c = count;
                        for (int i = 0; i < c; i++)
                        {
                            Data.Int32Range column = columns[i];
                            var cell = row.Cells[i];
                            Rectangle cellBounds = new Rectangle(column.Begin + 2, y, column.Size, height).Add(boundsAbsolute.Location);
                            DrawTableCell(graphics, cellBounds, cell);
                        }
                        y += height;
                    }
                }

                Brush brush = Skin.Brush(tableText.CurrentBorderColor);
                Rectangle[] borders = boundsAbsolute.GetBorders(2, RectangleSide.Left, RectangleSide.Top, RectangleSide.Right, RectangleSide.Bottom);
                foreach (Rectangle border in borders)
                    graphics.FillRectangle(brush, border);

            }
        }
        private static void DrawTableCell(Graphics graphics, Rectangle cellBounds, TableTextCell cell)
        {
            Color? backColor = cell.CurrentBackColor;
            if (backColor.HasValue)
                DrawEffect3D(graphics, cellBounds, backColor.Value, Orientation.Horizontal, cell.CurrentBackEffect3D);

            Rectangle textBounds = cell.GetTextBounds(cellBounds);
            DrawString(graphics, cell.Text, cell.CurrentFont, textBounds, cell.CurrentAlignment, cell.CurrentTextColor);

            Color? lineHColor = cell.CurrentLineHColor;
            Color? lineVColor = cell.CurrentLineVColor;
            if (lineHColor.HasValue || lineVColor.HasValue)
            {
                Rectangle[] borders = cellBounds.GetBorders(1, RectangleSide.Bottom, RectangleSide.Right);
                if (lineHColor.HasValue)
                    graphics.FillRectangle(Skin.Brush(lineHColor.Value), borders[0]);
                if (lineVColor.HasValue)
                    graphics.FillRectangle(Skin.Brush(lineVColor.Value), borders[1]);
            }
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
        /// Vykreslí záhlaví GridHeader = hlavička sloupce
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="side"></param>
        /// <param name="backColor"></param>
        /// <param name="draw3D"></param>
        /// <param name="lineColor"></param>
        /// <param name="state"></param>
        /// <param name="orientation"></param>
        /// <param name="relativePoint"></param>
        /// <param name="opacity"></param>
        internal static void DrawGridHeader(Graphics graphics, Rectangle bounds, RectangleSide side, Color backColor, bool draw3D, Color? lineColor, GInteractiveState state, Orientation orientation, Point? relativePoint, Int32? opacity)
        {
            _DrawGridHeader(graphics, bounds, side, backColor, draw3D, lineColor, state, orientation, relativePoint, opacity);
        }
        /// <summary>
        /// Vykreslí záhlaví GridHeader = hlavička sloupce
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="side"></param>
        /// <param name="backColor"></param>
        /// <param name="draw3D"></param>
        /// <param name="lineColor"></param>
        /// <param name="state"></param>
        /// <param name="orientation"></param>
        /// <param name="relativePoint"></param>
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
        #region DrawTrackBar
        /// <summary>
        /// Vykreslí TrackBar
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="paintData"></param>
        public static void DrawTrackBar(Graphics graphics, Rectangle bounds, ITrackBarPaintData paintData)
        {
            DrawTrackBarBackground(graphics, bounds, paintData);
            DrawTrackBarTicks(graphics, bounds, paintData);
            DrawTrackBarTrackLine(graphics, bounds, paintData);
            DrawTrackBarTrackData(graphics, bounds, paintData);
            DrawTrackBarTrackPoint(graphics, bounds, paintData);
            DrawTrackBarMousePoint(graphics, bounds, paintData);
        }
        /// <summary>
        /// Vykreslí pozadí.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="paintData"></param>
        private static void DrawTrackBarBackground(Graphics graphics, Rectangle bounds, ITrackBarPaintData paintData)
        {
            graphics.FillRectangle(Skin.Brush(paintData.BackColor), bounds);
            Rectangle activeBounds = paintData.ActiveBounds.Add(bounds.Location);
            Color activeColor = paintData.BackColor.Morph(Color.White, 0.08f);
            graphics.FillRectangle(Skin.Brush(activeColor), activeBounds);
        }
        /// <summary>
        /// Vykreslí všechny TickLines + Begin a End bar a TrackLine pokud se kreslí stylem Line.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="paintData"></param>
        /// <returns></returns>
        private static void DrawTrackBarTicks(Graphics graphics, Rectangle bounds, ITrackBarPaintData paintData)
        {
            if (paintData.TickType == TrackBarTickType.None) return;
            var ticks = (paintData.Orientation == Orientation.Horizontal ? DrawTrackBarTicksCalculateH(paintData) : DrawTrackBarTicksCalculateV(paintData));
            SolidBrush brush = Skin.Brush(Color.Empty);
            Point origin = bounds.Location;
            foreach (var tick in ticks)
            {
                brush.Color = tick.Item1;
                graphics.FillRectangle(brush, tick.Item2.Add(origin));
            }
        }
        /// <summary>
        /// Vygeneruje všechny TickLines + Begin a End bar a TrackLine pokud se kreslí stylem Line.
        /// Pro horizontální orientaci.
        /// </summary>
        /// <param name="paintData"></param>
        /// <returns></returns>
        private static List<Tuple<Color, Rectangle>> DrawTrackBarTicksCalculateH(ITrackBarPaintData paintData)
        {
            List<Tuple<Color, Rectangle>> result = new List<Tuple<Color, Rectangle>>();

            Color tickColor;

            // Koordináty:
            Rectangle bounds = paintData.TrackBounds;
            int l0 = bounds.X;                   // Délka = ve směru osy. l0 = Begin
            int l9 = bounds.Right;               // Délka = ve směru osy. l9 = End
            int t0 = bounds.Y;                   // Thick = kolmo na osu. t0 = Begin
            int t5 = bounds.Center().Y;          // Thick = kolmo na osu. t5 = Center
            int t9 = bounds.Bottom;              // Thick = kolmo na osu. t9 = End
            int l = bounds.Width;                // Délka = ve směru osy. l = Length
            int t = bounds.Height;               // Thick = kolmo na osu. t = Length

            // Ticky:
            if ((paintData.TickType & TrackBarTickType.WholeLine) != 0 && paintData.TickCount.HasValue && paintData.TickCount.Value > 1)
            {
                int trackHeight = paintData.TrackSize.Height;        // Velikost TrackPointu
                int th = trackHeight / 2;
                int a1 = t0 + 2;                                     // Počáteční souřadnice HalfBegin a WholeLine
                int a2 = t5 - th - 1;                                // Koncová souřadnice HalfBegin
                int at = a2 - a1;                                    // Velikost HalfBegin
                int b1 = t5 + th + 1;                                // Počáteční souřadnice HalfEnd
                int b2 = t9 - 2;                                     // Koncová souřadnice HalfEnd
                int bt = b2 - b1;                                    // Velikost HalfEnd
                int wt = b2 - a1;                                    // Velikost WholeLine

                bool addW = (((TrackBarTickType)(paintData.TickType & TrackBarTickType.WholeLine)) == TrackBarTickType.WholeLine);
                bool addA = (at >= 2 && !addW && ((paintData.TickType & TrackBarTickType.HalfBegin) != 0));
                bool addB = (bt >= 2 && !addW && ((paintData.TickType & TrackBarTickType.HalfEnd) != 0));

                tickColor = paintData.TickColor ?? Skin.TrackBar.LineColorTick;

                decimal length = l;
                decimal step = length / paintData.TickCount.Value;
                if (step < 3m) step = 3m;
                for (decimal tick = l0; tick < l9; tick += step)
                {
                    int s = (int)Math.Round(tick, 0);

                    if (addA)
                        result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(s, a1, 1, at)));
                    if (addB)
                        result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(s, b1, 1, bt)));
                    if (addW)
                        result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(s, a1, 1, wt)));
                }
            }

            // Krajní Bary:
            tickColor = paintData.EndBarColor ?? Skin.TrackBar.LineColorTick;
            if ((paintData.TickType & TrackBarTickType.BeginBar) != 0)
            {
                result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(l0, t0, 1, t)));
            }
            if ((paintData.TickType & TrackBarTickType.EndBar) != 0)
            {
                result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(l9, t0, 1, t)));
            }

            // TrackLine:
            if (paintData.TrackLineType == TrackBarLineType.Line)
            {
                tickColor = paintData.TrackLineColor ?? Skin.TrackBar.LineColorTrack;
                if ((paintData.TickType & TrackBarTickType.TrackLineDouble) != 0)
                {
                    result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(l0, t5 - 1, l, 1)));
                    result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(l0, t5 + 1, l, 1)));
                }
                else if ((paintData.TickType & TrackBarTickType.TrackLineSingle) != 0)
                {
                    result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(l0, t5, l, 1)));
                }
            }

            return result;
        }
        /// <summary>
        /// Vygeneruje všechny TickLines + Begin a End bar a TrackLine pokud se kreslí stylem Line.
        /// Pro vertikální orientaci.
        /// </summary>
        /// <param name="paintData"></param>
        /// <returns></returns>
        private static List<Tuple<Color, Rectangle>> DrawTrackBarTicksCalculateV(ITrackBarPaintData paintData)
        {
            List<Tuple<Color, Rectangle>> result = new List<Tuple<Color, Rectangle>>();

            Color tickColor;

            // Koordináty:
            Rectangle bounds = paintData.TrackBounds;
            int l0 = bounds.Bottom;              // Délka = ve směru osy. l0 = Begin (=dole)
            int l9 = bounds.Top;                 // Délka = ve směru osy. l9 = End   (=nahoře)
            int t0 = bounds.X;                   // Thick = kolmo na osu. t0 = Begin
            int t5 = bounds.Center().X;          // Thick = kolmo na osu. t5 = Center
            int t9 = bounds.Right;               // Thick = kolmo na osu. t9 = End
            int l = bounds.Height;               // Délka = ve směru osy. l = Length
            int t = bounds.Width;                // Thick = kolmo na osu. t = Length

            // Ticky:
            if ((paintData.TickType & TrackBarTickType.WholeLine) != 0 && paintData.TickCount.HasValue && paintData.TickCount.Value > 1)
            {
                int trackHeight = paintData.TrackSize.Width;         // Velikost TrackPointu
                int th = trackHeight / 2;
                int a1 = t0 + 2;                                     // Počáteční souřadnice HalfBegin a WholeLine
                int a2 = t5 - th - 1;                                // Koncová souřadnice HalfBegin
                int at = a2 - a1;                                    // Velikost HalfBegin
                int b1 = t5 + th + 1;                                // Počáteční souřadnice HalfEnd
                int b2 = t9 - 2;                                     // Koncová souřadnice HalfEnd
                int bt = b2 - b1;                                    // Velikost HalfEnd
                int wt = b2 - a1;                                    // Velikost WholeLine

                bool addW = (((TrackBarTickType)(paintData.TickType & TrackBarTickType.WholeLine)) == TrackBarTickType.WholeLine);
                bool addA = (at >= 2 && !addW && ((paintData.TickType & TrackBarTickType.HalfBegin) != 0));
                bool addB = (bt >= 2 && !addW && ((paintData.TickType & TrackBarTickType.HalfEnd) != 0));

                tickColor = paintData.TickColor ?? Skin.TrackBar.LineColorTick;

                decimal length = l;
                decimal step = length / paintData.TickCount.Value;
                if (step < 3m) step = 3m;
                for (decimal tick = l0; tick > l9; tick -= step)     // Ticky dáváme od spodní pozice Y, jdeme nahoru (k nižší hodnotě Y)
                {
                    int s = (int)Math.Round(tick, 0);

                    if (addA)
                        result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(a1, s, at, 1)));
                    if (addB)
                        result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(b1, s, bt, 1)));
                    if (addW)
                        result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(a1, s, wt, 1)));
                }
            }

            // Krajní Bary:
            tickColor = paintData.EndBarColor ?? Skin.TrackBar.LineColorTick;
            if ((paintData.TickType & TrackBarTickType.BeginBar) != 0)
            {
                result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(t0, l9, t, 1)));
            }
            if ((paintData.TickType & TrackBarTickType.EndBar) != 0)
            {
                result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(t0, l0, t, 1)));
            }

            // TrackLine:
            if (paintData.TrackLineType == TrackBarLineType.Line)
            {
                tickColor = paintData.TrackLineColor ?? Skin.TrackBar.LineColorTrack;
                if ((paintData.TickType & TrackBarTickType.TrackLineDouble) != 0)
                {
                    result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(t5 - 1, l9, 1, l)));
                    result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(t5 + 1, l9, 1, l)));
                }
                else if ((paintData.TickType & TrackBarTickType.TrackLineSingle) != 0)
                {
                    result.Add(new Tuple<Color, Rectangle>(tickColor, new Rectangle(t5, l9, 1, l)));
                }
            }

            return result;
        }
        /// <summary>
        /// Vykreslí linii, po které se posunuje TrackPoint
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="paintData"></param>
        private static void DrawTrackBarTrackLine(Graphics graphics, Rectangle bounds, ITrackBarPaintData paintData)
        {
            switch (paintData.TrackLineType)
            {
                case TrackBarLineType.Solid:
                    DrawTrackBarTrackLineSolid(graphics, bounds, paintData);
                    break;
                case TrackBarLineType.ColorBlendLine:
                    DrawTrackBarTrackLineColorBlend(graphics, bounds, paintData);
                    break;
            }
        }
        /// <summary>
        /// Vykreslí linii, po které se posunuje TrackPoint, typ linky = Solid
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="paintData"></param>
        private static void DrawTrackBarTrackLineSolid(Graphics graphics, Rectangle bounds, ITrackBarPaintData paintData)
        {
            Rectangle lineBounds = paintData.TrackLineBounds;
            List<Tuple<Color?, Rectangle>> lineParts = (paintData.Orientation == Orientation.Horizontal ?
                DrawTrackBarLineCalculateH(paintData, 5) : DrawTrackBarLineCalculateV(paintData, 5));

            Point origin = bounds.Location;
            for (int i = 0; i < lineParts.Count; i++)
            {
                if (lineParts[i].Item1.HasValue)
                {
                    Rectangle itemBounds = lineParts[i].Item2.Add(origin);
                    graphics.FillRectangle(Skin.Brush(lineParts[i].Item1.Value), itemBounds);
                }
            }
        }
        /// <summary>
        /// Vykreslí linii, po které se posunuje TrackPoint, typ linky = ColorBlendLine
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="paintData"></param>
        private static void DrawTrackBarTrackLineColorBlend(Graphics graphics, Rectangle bounds, ITrackBarPaintData paintData)
        {
            Rectangle lineBounds = paintData.TrackLineBounds;
            List<Tuple<Color?, Rectangle>> lineParts = (paintData.Orientation == Orientation.Horizontal ?
                DrawTrackBarLineCalculateH(paintData, 7) : DrawTrackBarLineCalculateV(paintData, 7));

            Point origin = bounds.Location;
            for (int i = 0; i < lineParts.Count; i++)
            {
                Rectangle itemBounds = lineParts[i].Item2.Add(origin);
                if (i == 0)
                {   // Fill:
                    using (var brush = DrawTrackBarTrackGetBrushColorBlend(paintData, itemBounds))
                        graphics.FillRectangle(brush, itemBounds);
                }
                else if (lineParts[i].Item1.HasValue)
                {   // Borders:
                    graphics.FillRectangle(Skin.Brush(lineParts[i].Item1.Value), itemBounds);
                }
            }
        }
        /// <summary>
        /// Vrátí souřadnice pro TrackLine typu Fill, orientace Horizontal
        /// </summary>
        /// <param name="paintData"></param>
        /// <param name="trackThick"></param>
        /// <returns></returns>
        private static List<Tuple<Color?, Rectangle>> DrawTrackBarLineCalculateH(ITrackBarPaintData paintData, int trackThick)
        {
            List<Tuple<Color?, Rectangle>> result = new List<Tuple<Color?, Rectangle>>();

            Rectangle bounds = paintData.TrackLineBounds;
            int tt = trackThick / 2;
            int td = 2 * tt;
            int l0 = bounds.X;
            int lv = paintData.TrackPoint.X;
            int l9 = bounds.Right + 1;
            int l8 = l9 - 1;
            int ls = l9 - l0;
            int t0 = bounds.Y - tt;
            int t1 = t0 + 1;
            int t8 = t0 + td;
            int t9 = t8 + 1;
            int ts = t9 - t0;

            Color backColor = paintData.TrackBackColor ?? Skin.TrackBar.BackColorTrack;
            Color lineColor = paintData.TrackLineColor ?? Skin.TrackBar.LineColorTrack;
            Color lineColorL = lineColor.Morph(Skin.Modifiers.Effect3DLight, 0.25f);
            Color lineColorD = lineColor.Morph(Skin.Modifiers.Effect3DDark, 0.25f);

            result.Add(new Tuple<Color?, Rectangle>(backColor, new Rectangle(l0, t0, ls, ts)));
            result.Add(new Tuple<Color?, Rectangle>(paintData.TrackActiveBackColor, new Rectangle(l0, t0, lv - l0, ts)));
            result.Add(new Tuple<Color?, Rectangle>(paintData.TrackInactiveBackColor, new Rectangle(lv, t0, l9 - lv, ts)));

            result.Add(new Tuple<Color?, Rectangle>(lineColorD, new Rectangle(l0, t0, ls, 1)));
            result.Add(new Tuple<Color?, Rectangle>(lineColorD, new Rectangle(l0, t1, 1, td)));
            result.Add(new Tuple<Color?, Rectangle>(lineColorL, new Rectangle(l0, t8, ls, 1)));
            result.Add(new Tuple<Color?, Rectangle>(lineColorL, new Rectangle(l8, t1, 1, td)));

            return result;
        }
        /// <summary>
        /// Vrátí souřadnice pro TrackLine typu Fill, orientace Vertical
        /// </summary>
        /// <param name="paintData"></param>
        /// <param name="trackThick"></param>
        /// <returns></returns>
        private static List<Tuple<Color?, Rectangle>> DrawTrackBarLineCalculateV(ITrackBarPaintData paintData, int trackThick)
        {
            List<Tuple<Color?, Rectangle>> result = new List<Tuple<Color?, Rectangle>>();

            Rectangle bounds = paintData.TrackLineBounds;
            int tt = trackThick / 2;
            int td = 2 * tt;
            int l0 = bounds.Y;
            int lv = paintData.TrackPoint.Y;
            int l9 = bounds.Bottom + 1;
            int l8 = l9 - 1;
            int ls = l9 - l0;
            int t0 = bounds.X - tt;
            int t1 = t0 + 1;
            int t8 = t0 + td;
            int t9 = t8 + 1;
            int ts = t9 - t0;

            Color backColor = paintData.TrackBackColor ?? Skin.TrackBar.BackColorTrack;
            Color lineColor = paintData.TrackLineColor ?? Skin.TrackBar.LineColorTrack;
            Color lineColorL = lineColor.Morph(Skin.Modifiers.Effect3DLight, 0.25f);
            Color lineColorD = lineColor.Morph(Skin.Modifiers.Effect3DDark, 0.25f);

            result.Add(new Tuple<Color?, Rectangle>(backColor, new Rectangle(t0, l0, ts, ls)));
            result.Add(new Tuple<Color?, Rectangle>(paintData.TrackActiveBackColor, new Rectangle(t0, lv, ts, l9 - lv)));
            result.Add(new Tuple<Color?, Rectangle>(paintData.TrackInactiveBackColor, new Rectangle(t0, l0, ts, lv - l0)));

            result.Add(new Tuple<Color?, Rectangle>(lineColorD, new Rectangle(t0, l0, 1, ls)));
            result.Add(new Tuple<Color?, Rectangle>(lineColorD, new Rectangle(t1, l0, td, 1)));
            result.Add(new Tuple<Color?, Rectangle>(lineColorL, new Rectangle(t8, l0, 1, ls)));
            result.Add(new Tuple<Color?, Rectangle>(lineColorL, new Rectangle(t1, l8, td, 1)));

            return result;
        }
        /// <summary>
        /// Vygeneruje a vrátí Brush pro TrackLine typu ColorBlendLine
        /// </summary>
        /// <param name="paintData"></param>
        /// <param name="itemBounds"></param>
        /// <returns></returns>
        private static System.Drawing.Drawing2D.LinearGradientBrush DrawTrackBarTrackGetBrushColorBlend(ITrackBarPaintData paintData, Rectangle itemBounds)
        {
            ColorBlend colorBlend = DrawTrackBarTrackGetColorBlend(paintData);
            Color color1 = colorBlend.Colors[0];
            Color color2 = colorBlend.Colors[colorBlend.Colors.Length - 1];
            LinearGradientBrush brush = new LinearGradientBrush(itemBounds, color1, color2, (paintData.Orientation == Orientation.Horizontal ? 0f : 270f));
            brush.InterpolationColors = colorBlend;
            return brush;
        }
        /// <summary>
        /// Vygeneruje a vrátí <see cref="ColorBlend"/> podle dat v <see cref="ITrackBarPaintData.ColorBlend"/>
        /// </summary>
        /// <param name="paintData"></param>
        /// <returns></returns>
        private static ColorBlend DrawTrackBarTrackGetColorBlend(ITrackBarPaintData paintData)
        {
            Tuple<float, Color>[] colors = ((paintData.ColorBlend != null) ? paintData.ColorBlend.ToArray() : null);
            if (colors == null || colors.Length < 2)
            {   // Defaultní:
                colors = new Tuple<float, Color>[]
                {
                    new Tuple<float, Color>(0.00f, Color.LightGreen),
                    new Tuple<float, Color>(0.20f, Color.LightGreen),
                    new Tuple<float, Color>(0.35f, Color.Yellow),
                    new Tuple<float, Color>(0.65f, Color.Yellow),
                    new Tuple<float, Color>(0.80f, Color.Red),
                    new Tuple<float, Color>(1.00f, Color.Red)
                };
            }

            ColorBlend colorBlend = new ColorBlend(colors.Length);
            colorBlend.Positions = colors.Select(c => c.Item1).ToArray();
            colorBlend.Colors = colors.Select(c => c.Item2).ToArray();

            return colorBlend;
        }
        /// <summary>
        /// Vykreslí TrackData = user draw
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="paintData"></param>
        private static void DrawTrackBarTrackData(Graphics graphics, Rectangle bounds, ITrackBarPaintData paintData)
        {
            paintData.PaintTextData(graphics, bounds);
        }
        /// <summary>
        /// Vykreslí TrackPoint = ovládací jezdec TrackBaru
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="paintData"></param>
        private static void DrawTrackBarTrackPoint(Graphics graphics, Rectangle bounds, ITrackBarPaintData paintData)
        {
            Point trackPoint = paintData.TrackPoint.Add(bounds.Location);
            Rectangle trackPointBounds = trackPoint.CreateRectangleFromCenter(paintData.TrackSize.Height - 1);
            Color trackBackColor = DrawTrackBarGetTrackBackColor(paintData);
            Color trackLineColor = paintData.TrackPointLineColor ?? Skin.TrackBar.LineColorButton;
            using (GraphicsUseSmooth(graphics))
            {
                if (paintData.CurrentMouseArea == TrackBarAreaType.Pointer)
                {
                    Rectangle trackWideBounds = new Rectangle(trackPointBounds.X - 4, trackPointBounds.Y - 4, trackPointBounds.Width + 8, trackPointBounds.Height + 8);
                    trackWideBounds = Rectangle.Intersect(bounds, trackWideBounds);
                    Color trackWideColor = Color.FromArgb(100, trackBackColor);
                    graphics.FillEllipse(Skin.Brush(trackWideColor), trackWideBounds);
                }
                graphics.FillEllipse(Skin.Brush(trackBackColor), trackPointBounds);
                graphics.DrawEllipse(Skin.Pen(trackLineColor), trackPointBounds);
            }
        }
        /// <summary>
        /// Vykreslí MouseOver point. Aktuálně nic nedělá
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="paintData"></param>
        private static void DrawTrackBarMousePoint(Graphics graphics, Rectangle bounds, ITrackBarPaintData paintData)
        { }
        /// <summary>
        /// Vrátí barvu BackColor pro TrackPoint podle dat v <see cref="ITrackBarPaintData"/> pro aktuální stav interaktivity
        /// </summary>
        /// <param name="paintData"></param>
        /// <returns></returns>
        private static Color DrawTrackBarGetTrackBackColor(ITrackBarPaintData paintData)
        {
            Color trackBackColor = paintData.TrackPointBackColor ?? Skin.TrackBar.BackColorButton;
            Color? mouseOverColor = paintData.TrackPointMouseOverBackColor ?? Skin.TrackBar.BackColorMouseOverButton;
            Color? mouseDownColor = paintData.TrackPointMouseDownBackColor ?? Skin.TrackBar.BackColorMouseDownButton;
            Color? mouseDragColor = mouseDownColor;
            Color? disabledColor = trackBackColor.Morph(Skin.Modifiers.BackColorDisable);
            return GetColorByInteractiveState(paintData.InteractiveState, trackBackColor, mouseOverColor, mouseDownColor, mouseDragColor, disabledColor);
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
            int x9 = bounds.Right - 0;
            int y0 = bounds.Y;
            int y1 = y0 + 2;
            int y5 = bounds.Center().Y;
            int y4 = y5 - 1;
            int y6 = y5 + 1;
            int y9 = bounds.Bottom - 0;
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
                using (GraphicsPath path = Painter.CreatePathTrackPointer(center, size, pointerType, pointerSide, GraphicsPathPart.FilledArea, out graphicSetting))
                {
                    if (path != null)
                    {
                        Rectangle bounds = center.CreateRectangleFromCenter(size);
                        using (Brush brush = Skin.CreateBrushForBackground(bounds, orientation, state, Skin.TrackBar.BackColorButton))
                        using (Painter.GraphicsUse(graphics, graphicSetting))
                        {
                            graphics.FillPath(brush, path);
                        }
                    }
                }
            }
            if (true)
            {
                using (GraphicsPath path = Painter.CreatePathTrackPointer(center, size, pointerType, pointerSide, GraphicsPathPart.LightBorder, out graphicSetting))
                {
                    Color lightBorder = Skin.Modifiers.GetColor3DBorderLight(Skin.TrackBar.LineColorButton);
                    using (Painter.GraphicsUse(graphics, graphicSetting))
                    {
                        graphics.DrawPath(Skin.Pen(lightBorder), path);
                    }
                }
            }
            if (true)
            {
                using (GraphicsPath path = Painter.CreatePathTrackPointer(center, size, pointerType, pointerSide, GraphicsPathPart.DarkBorder, out graphicSetting))
                {
                    Color darkBorder = Skin.Modifiers.GetColor3DBorderDark(Skin.TrackBar.LineColorButton);
                    using (Painter.GraphicsUse(graphics, graphicSetting))
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
        /// <param name="pathPart"></param>
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
                case TrackPointerType.OneSide:
                    return (isVertical ?
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
        /// <param name="alignment"></param>
        internal static void DrawImage(Graphics graphics, Rectangle bounds, Image image, ContentAlignment? alignment = null)
        {
            _DrawImage(graphics, bounds, image, null, alignment);
        }
        /// <summary>
        /// Vykreslí daný Image, pokud isEnabled je false, pak bude Image modifikovaný do šedé barvy
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="image"></param>
        /// <param name="isEnabled"></param>
        /// <param name="alignment"></param>
        internal static void DrawImage(Graphics graphics, Rectangle bounds, Image image, bool isEnabled, ContentAlignment? alignment = null)
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
        internal static void DrawImage(Graphics graphics, Rectangle bounds, Image image, GInteractiveState? state, ContentAlignment? alignment = null)
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
            Painter.DrawAreaBase(graphics, bounds, color, orientation, state, null, null, 0);
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
        #region DrawRelationLine
        /// <summary>
        /// Metoda vykreslí linku na spodním okraji daného prostoru, podle pravidel pro Grid
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="forDocument">Určení barvy: Zadejte false pro Záznam, true pro Dokument</param>
        /// <param name="forGrid">Určení šířky: Zadejte false pro Form, true pro Grid</param>
        /// <param name="color">Explicitní barva</param>
        /// <param name="lineWidth">Explicitní šířka linky (1 až 2 pixely, rozmezí je 0 - 6; přičemž 0 se nekreslí)</param>
        /// <param name="colorFading">Explicitní Fading = slábnutí barvy (barevný přechod), default = 0.60f</param>
        internal static void DrawRelationLine(Graphics graphics, Rectangle bounds, bool forDocument = false, bool forGrid = false, Color? color = null, int? lineWidth = null, float? colorFading = null)
        {
            int thick = (lineWidth.HasValue ? lineWidth.Value : (!forGrid ? Skin.Relation.LineHeightInForm : Skin.Relation.LineHeightInGrid));
            thick = (thick < 0 ? 0 : (thick > 6 ? 6 : thick));
            if (thick == 0) return;

            Color color1 = (color.HasValue ? color.Value : (!forDocument ? 
                (!forGrid ? Skin.Relation.LineColorForRecordInForm : Skin.Relation.LineColorForRecordInGrid) :
                (!forGrid ? Skin.Relation.LineColorForDocumentInForm1 : Skin.Relation.LineColorForDocumentInGrid)));
            Color? color2 = (color.HasValue ? color : (!forGrid && forDocument ? (Color?)Skin.Relation.LineColorForDocumentInForm2 : null));

            float fading = (colorFading.HasValue ? colorFading.Value : Skin.Relation.LineFadingRatio);
            fading = (fading < 0f ? 0f : (fading > 1f ? 1f : fading));

            Rectangle boundsLine = (!forGrid ?
                new Rectangle(bounds.X, bounds.Bottom - thick, bounds.Width, thick) :                   // Pro Dokument = těsně dole v daném prostoru
                new Rectangle(bounds.X + 1, bounds.Bottom - thick - 1, bounds.Width - 3, thick));       // Pro Grid = necháme kolem 1px
            if (boundsLine.Width <= 0) return;

            if (color2.HasValue && thick >= 2)
            {   // Barva color2 je zadána jen pro Dokument a Form; a pokud je tloušťka čáry více než 1 pixel máme trochu jinou úpravu:
                //  Horní polovina bude barvou color1, a dolní pak barvou color2:
                Rectangle bounds1 = new Rectangle(boundsLine.X, boundsLine.Y, boundsLine.Width, thick / 2);
                _DrawRelationLine(graphics, bounds1, color1, fading);
                Rectangle bounds2 = new Rectangle(boundsLine.X, bounds1.Bottom, boundsLine.Width, boundsLine.Height - bounds1.Height);
                _DrawRelationLine(graphics, bounds2, color2.Value, fading);
            }
            else
            {
                _DrawRelationLine(graphics, boundsLine, color1, fading);
            }
        }
        /// <summary>
        /// Vykreslí čáru do daného prostoru, v dané barvě a s danou hodnotou fading
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="boundsLine"></param>
        /// <param name="color"></param>
        /// <param name="fading"></param>
        private static void _DrawRelationLine(Graphics graphics, Rectangle boundsLine, Color color, float fading)
        {
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
        #region DrawGLine
        /// <summary>
        /// Vykreslí čáru GLine
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="line"></param>
        /// <param name="bounds"></param>
        internal static void DrawGLine(Graphics graphics, ILine3D line, Rectangle bounds)
        {
            if (line == null || !line.Visible) return;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            // 3D efekt na okraji:
            int border = 0;
            float effect3D = line.Effect3D;
            if (effect3D != 0f && line.Border3D > 0)
            {
                border = line.Border3D;
                border = (border < 0 ? 0 : (border > 32 ? 32 : border));
                int size = (bounds.Width < bounds.Height ? bounds.Width : bounds.Height);    // Menší rozměr
                int size2 = size / 2;
                if (border > size2) border = size2;
            }

            // Orientace:
            bool isHorizontal = (bounds.Width >= bounds.Height);

            // Oblasti pro kreslení = okraje + střed:
            Dictionary<RectangleSide, Rectangle> areas = new Dictionary<RectangleSide, Rectangle>();
            if (border > 0)
            {
                areas.Add(RectangleSide.Top, new Rectangle(bounds.X, bounds.Y, bounds.Width, border));
                areas.Add(RectangleSide.Left, new Rectangle(bounds.X, bounds.Y + border, border, bounds.Height - border));
                areas.Add(RectangleSide.Right, new Rectangle(bounds.Right - border, bounds.Y + border, border, bounds.Height - border - border));
                areas.Add(RectangleSide.Bottom, new Rectangle(bounds.X + border, bounds.Bottom - border, bounds.Width - border, border));
                bounds = bounds.Enlarge(-border);
            }
            if (bounds.Width > 0 && bounds.Height > 0)
            {
                areas.Add(RectangleSide.Center, bounds);
            }

            // Barvy: 3D efekt, Gradient:
            Color colorBegin = line.LineColor;
            Color? colorEnd = line.LineColorEnd;

            if (!colorEnd.HasValue)
            {   // Bez gradientu:
                if (border <= 0)
                {   // Bez gradientu a bez okrajů = jen střed v barvě colorBegin:
                    if (areas.TryGetValue(RectangleSide.Center, out bounds)) graphics.FillRectangle(Skin.Brush(colorBegin), bounds);
                }
                else
                {   // Bez gradientu s okraji 3D efekt:
                    Color colorBeginB, colorBeginA;
                    CreateEffect3DColors(colorBegin, effect3D, out colorBeginB, out colorBeginA);

                    if (areas.TryGetValue(RectangleSide.Left, out bounds)) graphics.FillRectangle(Skin.Brush(colorBeginB), bounds);
                    if (areas.TryGetValue(RectangleSide.Top, out bounds)) graphics.FillRectangle(Skin.Brush(colorBeginB), bounds);
                    if (areas.TryGetValue(RectangleSide.Right, out bounds)) graphics.FillRectangle(Skin.Brush(colorBeginA), bounds);
                    if (areas.TryGetValue(RectangleSide.Bottom, out bounds)) graphics.FillRectangle(Skin.Brush(colorBeginA), bounds);
                    if (areas.TryGetValue(RectangleSide.Center, out bounds)) graphics.FillRectangle(Skin.Brush(colorBegin), bounds);
                }
            }
            else if (isHorizontal)
            {   // S gradientem - Horizontálním:
                RectangleSide side = RectangleSide.Right;
                if (border <= 0)
                {   // Horizontální gradient, bez okrajů:
                    if (areas.TryGetValue(RectangleSide.Center, out bounds)) FillWithGradient(graphics, bounds, colorBegin, colorEnd.Value, side);
                }
                else
                {   // Horizontální gradient, s okraji 3D efekt:
                    Color colorBeginB, colorBeginA;
                    CreateEffect3DColors(colorBegin, effect3D, out colorBeginB, out colorBeginA);
                    Color colorEndB, colorEndA;
                    CreateEffect3DColors(colorEnd.Value, effect3D, out colorEndB, out colorEndA);

                    if (areas.TryGetValue(RectangleSide.Left, out bounds)) graphics.FillRectangle(Skin.Brush(colorBeginB), bounds);
                    if (areas.TryGetValue(RectangleSide.Top, out bounds)) FillWithGradient(graphics, bounds, colorBeginB, colorEndB, side);
                    if (areas.TryGetValue(RectangleSide.Right, out bounds)) graphics.FillRectangle(Skin.Brush(colorEndA), bounds);
                    if (areas.TryGetValue(RectangleSide.Bottom, out bounds)) FillWithGradient(graphics, bounds, colorBeginA, colorEndA, side);
                    if (areas.TryGetValue(RectangleSide.Center, out bounds)) FillWithGradient(graphics, bounds, colorBegin, colorEnd.Value, side);
                }
            }
            else
            {   // S gradientem - Vertikálním:
                RectangleSide side = RectangleSide.Bottom;
                if (border <= 0)
                {   // Vertikální gradient, bez okrajů:
                    if (areas.TryGetValue(RectangleSide.Center, out bounds)) FillWithGradient(graphics, bounds, colorBegin, colorEnd.Value, side);
                }
                else
                {   // Vertikální gradient, s okraji 3D efekt:
                    Color colorBeginB, colorBeginA;
                    CreateEffect3DColors(colorBegin, effect3D, out colorBeginB, out colorBeginA);
                    Color colorEndB, colorEndA;
                    CreateEffect3DColors(colorEnd.Value, effect3D, out colorEndB, out colorEndA);

                    if (areas.TryGetValue(RectangleSide.Left, out bounds)) FillWithGradient(graphics, bounds, colorBeginB, colorEndB, side); 
                    if (areas.TryGetValue(RectangleSide.Top, out bounds)) graphics.FillRectangle(Skin.Brush(colorBeginB), bounds);
                    if (areas.TryGetValue(RectangleSide.Right, out bounds)) FillWithGradient(graphics, bounds, colorBeginA, colorEndA, side);
                    if (areas.TryGetValue(RectangleSide.Bottom, out bounds)) graphics.FillRectangle(Skin.Brush(colorEndA), bounds);
                    if (areas.TryGetValue(RectangleSide.Center, out bounds)) FillWithGradient(graphics, bounds, colorBegin, colorEnd.Value, side);
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
            GInteractiveState summaryState = (scrollBar.MinButtonState | scrollBar.MinAreaState | scrollBar.ThumbButtonState | scrollBar.MaxAreaState | scrollBar.MaxButtonState);
            bool isScrollBarActive = summaryState.IsMouseActive();

            // Pozadí:
            _DrawScrollBarBack(graphics, absoluteBounds, orientation, isEnabled, isScrollBarActive, scrollBar.ScrollBarBackColor);

            // Prostor Data (mezi Min a Max buttonem, pod Thumbem), plus UserDataDraw method:
            _DrawScrollBarData(graphics, scrollBar.DataAreaBounds.Add(location), orientation, isEnabled, isScrollBarActive, scrollBar.ScrollBarBackColor, scrollBar.UserDataDraw);

            // Aktivní prostor Min/Max area (prostor pro kliknutí mezi Thumb a Min/Max Buttonem):
            if (scrollBar.MinAreaState.IsMouseActive())
                _DrawScrollBarActiveArea(graphics, scrollBar.MinAreaBounds.Add(location), orientation, isEnabled, isScrollBarActive, scrollBar.MinAreaState);
            else if (scrollBar.MaxAreaState.IsMouseActive())
                _DrawScrollBarActiveArea(graphics, scrollBar.MaxAreaBounds.Add(location), orientation, isEnabled, isScrollBarActive, scrollBar.MaxAreaState);

            // Buttony:
            _DrawScrollBarButton(graphics, scrollBar.MinButtonBounds.Add(location), orientation, isEnabled, isScrollBarActive, scrollBar.MinButtonState, false, LinearShapeType.LeftArrow, LinearShapeType.UpArrow);
            _DrawScrollBarButton(graphics, scrollBar.MaxButtonBounds.Add(location), orientation, isEnabled, isScrollBarActive, scrollBar.MaxButtonState, false, LinearShapeType.RightArrow, LinearShapeType.DownArrow);
            if (isEnabled)
                _DrawScrollBarButton(graphics, scrollBar.ThumbButtonBounds.Add(location), orientation, isEnabled, isScrollBarActive, scrollBar.ThumbButtonState, true, LinearShapeType.HorizontalLines, LinearShapeType.VerticalLines);

        }
        /// <summary>
        /// Vykreslí základní pozadí pod celý ScrollBar
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="orientation"></param>
        /// <param name="isEnabled"></param>
        /// <param name="isScrollBarActive"></param>
        /// <param name="backColor"></param>
        private static void _DrawScrollBarBack(Graphics graphics, Rectangle bounds, Orientation orientation, bool isEnabled, bool isScrollBarActive, Color backColor)
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
        /// <param name="isScrollBarActive"></param>
        /// <param name="backColor"></param>
        /// <param name="userDataDraw"></param>
        private static void _DrawScrollBarData(Graphics graphics, Rectangle bounds, Orientation orientation, bool isEnabled, bool isScrollBarActive, Color backColor, Action<Graphics, Rectangle> userDataDraw)
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
        /// <param name="isScrollBarActive"></param>
        /// <param name="itemState"></param>
        private static void _DrawScrollBarActiveArea(Graphics graphics, Rectangle bounds, Orientation orientation, bool isEnabled, bool isScrollBarActive, GInteractiveState itemState)
        {
            Painter.DrawAreaBase(graphics, bounds, Skin.ScrollBar.BackColorArea, orientation, itemState, null, 96);
        }
        /// <summary>
        /// Vykreslí button pro ScrollBar a do něj jeho grafiku
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="orientation"></param>
        /// <param name="isEnabled"></param>
        /// <param name="isScrollBarActive"></param>
        /// <param name="itemState"></param>
        /// <param name="drawAlways"></param>
        /// <param name="shapeHorizontal"></param>
        /// <param name="shapeVertical"></param>
        private static void _DrawScrollBarButton(Graphics graphics, Rectangle bounds, Orientation orientation, bool isEnabled, bool isScrollBarActive, GInteractiveState itemState, bool drawAlways, LinearShapeType shapeHorizontal, LinearShapeType shapeVertical)
        {
            bool isItemActive = itemState.IsMouseActive();

            if (isEnabled && (drawAlways || isItemActive))
            {   // Buttony kreslím jen pokud ScrollBar je Enabled, a (mám kreslit vždy = Thumb, anebo button je myšoaktivní = Min/Max):
                Color backColor = (isScrollBarActive ? Skin.ScrollBar.BackColorButtonActive : Skin.ScrollBar.BackColorButtonPassive);
                Painter.DrawAreaBase(graphics, bounds, backColor, orientation, itemState, null, null);
                // GPainter.DrawButtonBase(graphics, bounds, Skin.ScrollBar.BackColorButton, itemState, orientation, 0, null, null);
            }

            LinearShapeType shape = (orientation == Orientation.Vertical ? shapeVertical : shapeHorizontal);
            if (shape == LinearShapeType.None) return;

            Rectangle shapeBounds = bounds;
            if (itemState.IsMouseDown())
                shapeBounds = shapeBounds.Add(1, 1);
            GraphicSetting graphicSetting;
            GraphicsPath imagePath = Painter.CreatePathLinearShape(shape, shapeBounds, 2, out graphicSetting);
            if (imagePath != null)
            {
                GInteractiveState state = (isEnabled ? itemState : GInteractiveState.Disabled);
                Color foreColor = Skin.GetForeColor(Skin.ScrollBar.TextColorButton, state);
                using (Painter.GraphicsUse(graphics, graphicSetting))
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
            Painter.DrawString(graphics, tabHeader.Text, tabHeader.Font, textBounds, ContentAlignment.MiddleCenter, color: textColor, transformation: transformation);
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
        /// <summary>
        /// Vykreslí pozadí pro prvek grafu
        /// </summary>
        /// <param name="args"></param>
        /// <param name="boundsParts"></param>
        private static void GraphItemDrawBack(GraphItemArgs args, Rectangle[] boundsParts)
        {
            if (!args.BackColor.HasValue) return;
            switch (args.BackEffectStyle)
            {
                case Graphs.TimeGraphElementBackEffectStyle.Pipe:
                    Painter.GraphItemDrawBackPipe(args.Graphics, boundsParts[0], args.BackColor.Value, Orientation.Horizontal, args.InteractiveState, args.Effect3D, null);
                    break;
                case Graphs.TimeGraphElementBackEffectStyle.Flat:
                    Painter.GraphItemDrawBackFlat(args.Graphics, boundsParts[0], args.BackColor.Value, Orientation.Horizontal, args.InteractiveState, args.Effect3D, null);
                    break;
                case Graphs.TimeGraphElementBackEffectStyle.Simple:
                    Painter.GraphItemDrawBackFlat(args.Graphics, boundsParts[0], args.BackColor.Value, Orientation.Horizontal, args.InteractiveState, args.Effect3D, null);
                    break;
                case Graphs.TimeGraphElementBackEffectStyle.Default:
                default:
                    Painter.DrawEffect3D(args.Graphics, boundsParts[0], args.BackColor.Value, Orientation.Horizontal, args.Effect3D, null);
                    break;
            }
        }
        /// <summary>
        /// Vykreslí pozadí prvku v režimu <see cref="Graphs.TimeGraphElementBackEffectStyle.Pipe"/>
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="orientation"></param>
        /// <param name="interactiveState"></param>
        /// <param name="effect3D"></param>
        /// <param name="opacity"></param>
        private static void GraphItemDrawBackPipe(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, GInteractiveState interactiveState, float? effect3D, Int32? opacity = null)
        {
            float angle = (orientation == Orientation.Horizontal ? 90f : 0f);
            using (LinearGradientBrush brush = new LinearGradientBrush(bounds, color, color, angle))
            {
                brush.InterpolationColors = CreateColorBlendPipe(color, interactiveState, effect3D);
                graphics.FillRectangle(brush, bounds);
            }
        }
        /// <summary>
        /// Vrátí definici barevného přechodu
        /// </summary>
        /// <param name="color"></param>
        /// <param name="interactiveState"></param>
        /// <param name="effect3D"></param>
        /// <returns></returns>
        private static ColorBlend CreateColorBlendPipe(Color color, GInteractiveState interactiveState, float? effect3D)
        {
            if (_ColorBlendPipe == null) _ColorBlendPipe = new Dictionary<string, ColorBlend>();
            string key = "[" + color.A.ToString() + ":" + color.R.ToString() + ":" + color.G.ToString() + ":" + color.B.ToString() + "]:" + interactiveState.ToString();
            ColorBlend colorBlend;
            if (!_ColorBlendPipe.TryGetValue(key, out colorBlend))
            {   // Hledali jsme a nemáme... Možná budeme muset vytvořit, ale...
                lock (_ColorBlendPipe)
                {   // .. raději si Dictionary zamkneme, a podíváme se ještě jednou (možná v jiném threadu někdo právě tuto věc vytvořil)
                    if (!_ColorBlendPipe.TryGetValue(key, out colorBlend))
                    {   // Dictionary je zamčená, a klíč v neexistuje => vytvoříme ColorBlend a do Dictionary ji přidáme:
                        colorBlend = new ColorBlend(5);
                        Color dark = color;
                        Color light = color;
                        switch (interactiveState)
                        {
                            case GInteractiveState.Disabled:
                                dark = color.Morph(Color.DimGray, 0.75f);
                                light = color.Morph(Color.LightGray, 0.75f);
                                colorBlend.Positions = new float[] { 0f, 0.15f, 0.50f, 0.85f, 1f };
                                break;
                            case GInteractiveState.MouseOver:
                                dark = color.Morph(Color.DimGray, 0.75f);
                                light = color.Morph(Color.LightYellow, 0.75f);
                                colorBlend.Positions = new float[] { 0f, 0.15f, 0.30f, 0.75f, 1f };
                                break;
                            case GInteractiveState.LeftDown:
                            case GInteractiveState.RightDown:
                                dark = color.Morph(Color.Black, 0.70f);
                                light = color.Morph(Color.LightYellow, 0.75f);
                                colorBlend.Positions = new float[] { 0f, 0.30f, 0.65f, 0.85f, 1f };
                                break;
                            case GInteractiveState.Enabled:
                            default:
                                dark = color.Morph(Color.DimGray, 0.75f);
                                light = color.Morph(Color.White, 0.65f);
                                colorBlend.Positions = new float[] { 0f, 0.20f, 0.45f, 0.80f, 1f };
                                break;
                        }
                        colorBlend.Colors = new Color[] { dark, color, light, color, dark };
                        _ColorBlendPipe.Add(key, colorBlend);
                    }
                }
            }
            return colorBlend;
        }
        private static Dictionary<string, ColorBlend> _ColorBlendPipe;
        /// <summary>
        /// Vykreslí pozadí prvku v režimu <see cref="Graphs.TimeGraphElementBackEffectStyle.Flat"/>
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="orientation"></param>
        /// <param name="interactiveState"></param>
        /// <param name="effect3D"></param>
        /// <param name="opacity"></param>
        private static void GraphItemDrawBackFlat(Graphics graphics, Rectangle bounds, Color color, Orientation orientation, GInteractiveState interactiveState, float? effect3D, Int32? opacity = null)
        {
            if (effect3D.HasValue)
                color = color.Morph(Skin.Modifiers.Effect3DLight, 0.50f * effect3D.Value);
            if (opacity.HasValue)
                color = Color.FromArgb(opacity.Value, color);
            graphics.FillRectangle(Skin.Brush(color), bounds);
        }
        /// <summary>
        /// Vykreslí HatchBrush na prvek grafu
        /// </summary>
        /// <param name="args"></param>
        /// <param name="boundsParts"></param>
        private static void GraphItemDrawHatch(GraphItemArgs args, Rectangle[] boundsParts)
        {
            if (!args.HasHatchStyle) return;
            using (HatchBrush hatchBrush = new HatchBrush(args.HatchStyle.Value, args.HatchColor.Value, Color.Transparent))
            {
                args.Graphics.FillRectangle(hatchBrush, boundsParts[0]);
            }
        }
        /// <summary>
        /// Vykreslí části Ratio (=výplň a/nebo linka zobrazující poměr Ratio)
        /// </summary>
        /// <param name="args"></param>
        /// <param name="boundsParts"></param>
        private static void GraphItemDrawRatio(GraphItemArgs args, Rectangle[] boundsParts)
        {
            if (!args.HasRatio) return;
            switch (args.RatioStyle)
            {
                case Graphs.TimeGraphElementRatioStyle.VerticalFill:
                    GraphItemDrawRatioVertical(args, boundsParts, 0);
                    break;
                case Graphs.TimeGraphElementRatioStyle.HorizontalFill:
                    GraphItemDrawRatioHorizontal(args, boundsParts, 0);
                    break;
                case Graphs.TimeGraphElementRatioStyle.HorizontalInner:
                    GraphItemDrawRatioHorizontal(args, boundsParts, 3);
                    break;
            }
        }
        /// <summary>
        /// Vykreslí části Ratio (=výplň a/nebo linka zobrazující poměr Ratio) pro Horizontální orientaci
        /// </summary>
        /// <param name="args"></param>
        /// <param name="boundsParts"></param>
        /// <param name="offset"></param>
        private static void GraphItemDrawRatioHorizontal(GraphItemArgs args, Rectangle[] boundsParts, int offset)
        {
            Rectangle bounds = boundsParts[0];
            int q1, q2;
            GraphItemDrawRatioGetOffsetPoint(bounds.Y, bounds.Bottom, offset, out q1, out q2);
            // Body p1 a p2 jsou umístěny vpravo na souřadnici RatioBegin:
            Point p1 = GraphItemDrawRatioGetHorizontalPoint(args.RatioBegin.Value, bounds, q1);
            Point p2 = GraphItemDrawRatioGetHorizontalPoint(args.RatioBegin.Value, bounds, q2);   // Horizontální Ratio neřeší hodnotu v RatioEnd !!! Nedává to význam.

            if (args.RatioBeginBackColor.HasValue)
            {
                // Body p0 a p3 jsou umístěny vlevo na souřadnici bounds.X:
                Point p0 = new Point(bounds.X, q1);
                Point p3 = new Point(bounds.X, q2);
                GraphItemDrawRatioFill(bounds, p0, p1, p2, p3, args.RatioBeginBackColor.Value, null, args.Graphics);
            }

            if (args.RatioLineColor.HasValue)
            {
                GraphItemDrawRatioLine(p1, p2, args.RatioLineColor.Value, args.RatioLineWidth, args.Graphics);
            }
        }
        /// <summary>
        /// Vykreslí části Ratio (=výplň a/nebo linka zobrazující poměr Ratio) pro Vertikální orientaci
        /// </summary>
        /// <param name="args"></param>
        /// <param name="boundsParts"></param>
        /// <param name="offset"></param>
        private static void GraphItemDrawRatioVertical(GraphItemArgs args, Rectangle[] boundsParts, int offset)
        {
            Rectangle bounds = boundsParts[0];
            int q1, q2;
            GraphItemDrawRatioGetOffsetPoint(bounds.X, bounds.Right, offset, out q1, out q2);
            // Body p1 a p2 jsou umístěny nahoře na souřadnici RatioBegin a RatioEnd:
            Point p1 = GraphItemDrawRatioGetVerticalPoint(args.RatioBegin.Value, bounds, q1);
            Point p2 = GraphItemDrawRatioGetVerticalPoint((args.RatioEnd.HasValue ? args.RatioEnd.Value : args.RatioBegin.Value), bounds, q2);

            if (args.RatioBeginBackColor.HasValue)
            {
                // Body p0 a p3 jsou umístěny vlevo na souřadnici bounds.Bottom:
                Point p0 = new Point(q1, bounds.Bottom);
                Point p3 = new Point(q2, bounds.Bottom);
                GraphItemDrawRatioFill(bounds, p0, p1, p2, p3, args.RatioBeginBackColor.Value, null, args.Graphics);
            }

            if (args.RatioLineColor.HasValue)
            {
                GraphItemDrawRatioLine(p1, p2, args.RatioLineColor.Value, args.RatioLineWidth, args.Graphics);
            }
        }
        /// <summary>
        /// Vykreslí výplň části Ratio elementu podle dodaných souřadnic
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <param name="graphics"></param>
        private static void GraphItemDrawRatioFill(Rectangle bounds, Point p0, Point p1, Point p2, Point p3, Color color1, Color? color2, Graphics graphics)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddLine(p0, p1);
            path.AddLine(p1, p2);
            path.AddLine(p2, p3);
            path.AddLine(p3, p0);
            path.CloseFigure();

            if (color2.HasValue)
            {
                using (LinearGradientBrush lgb = new LinearGradientBrush(bounds, color1, color2.Value, 0f))
                    graphics.FillPath(lgb, path);
            }
            else
            {
                graphics.FillPath(Skin.Brush(color1), path);
            }
        }
        /// <summary>
        /// Vykreslí ukončovací linku části Ratio elementu podle dodaných souřadnic
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="color1"></param>
        /// <param name="width"></param>
        /// <param name="graphics"></param>
        private static void GraphItemDrawRatioLine(Point p1, Point p2, Color color1, int? width, Graphics graphics)
        {
            using (GraphicsUseSmooth(graphics))
            {
                Pen pen = Skin.Pen(color1);
                if (width.HasValue && width.Value > 0)
                    pen.Width = width.Value;
                graphics.DrawLine(pen, p1, p2);
            }
        }
        /// <summary>
        /// Určí souřadnice out q1 a q2 na základě b (begin) a e (end), k nimž přište dodaný offset.
        /// Vstupní souřadnice b (begin) má mít menší hodnotu než e (end).
        /// Zadaný offset zajistí odsunutí out q1 na pozici (begin + offset), a out q2 na (end - offset).
        /// Pokud by offset byl záporný, pak out q1 = begin a out q2 = end.
        /// Pokud by offset byl větší než vzdálenost ((end - begin) / 3), pak bude akceptováno jen tento největší povolený offset.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="e"></param>
        /// <param name="offset"></param>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        private static void GraphItemDrawRatioGetOffsetPoint(int b, int e, int offset, out int q1, out int q2)
        {
            q1 = b;
            q2 = e;
            int size = (e - b) / 3;
            if (size <= 0) return;
            offset = (offset < 0 ? 0 : (offset > size ? size : offset));
            if (offset <= 0) return;
            q1 = b + offset;
            q2 = e - offset;
        }
        /// <summary>
        /// Vrací souřadnici bodu pro Horizontal Ratio
        /// </summary>
        /// <param name="ratio"></param>
        /// <param name="bounds"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private static Point GraphItemDrawRatioGetHorizontalPoint(float ratio, Rectangle bounds, int y)
        {
            ratio = (ratio < 0f ? 0f : (ratio > 1f ? 1f : ratio));
            int x = bounds.Left + (int)(Math.Round((float)bounds.Width * ratio, 0));
            return new Point(x, y);
        }
        /// <summary>
        /// Vrací souřadnici bodu pro Vertical Ratio
        /// </summary>
        /// <param name="ratio"></param>
        /// <param name="bounds"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static Point GraphItemDrawRatioGetVerticalPoint(float ratio, Rectangle bounds, int x)
        {
            ratio = (ratio < 0f ? 0f : (ratio > 1f ? 1f : ratio));
            int y = bounds.Bottom - (int)(Math.Round((float)bounds.Height * ratio, 0));
            return new Point(x, y);
        }
        /// <summary>
        /// Vykreslí okraje prvku (Normální, Selected, Framed).
        /// </summary>
        /// <param name="args"></param>
        /// <param name="boundsParts"></param>
        private static void GraphItemDrawBorder(GraphItemArgs args, Rectangle[] boundsParts)
        {
            Color? borderColor = GraphItemGetBorderColor(args);
            if (borderColor.HasValue)
            {
                bool apply3DEffect = (args.BackEffectStyle != Graphs.TimeGraphElementBackEffectStyle.Simple);   // Styl Simple potlačuje 3D efekt
                Color colorTop = (apply3DEffect ? Skin.Modifiers.GetColor3DBorderLight(borderColor.Value, 0.50f) : borderColor.Value);
                Color colorBottom = (apply3DEffect ? Skin.Modifiers.GetColor3DBorderDark(borderColor.Value, 0.50f) : borderColor.Value);
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
        /// Vrátí barvu pro kreslení linky okolo prvku.
        /// Akceptuje příznaky <see cref="GraphItemArgs.IsSelected"/> a <see cref="GraphItemArgs.IsFramed"/>.
        /// Pro tvar prvku <see cref="GraphItemArgs.BackEffectStyle"/> == <see cref="Graphs.TimeGraphElementBackEffectStyle.Simple"/> vrací null = linka se nekreslí.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Color? GraphItemGetBorderColor(GraphItemArgs args)
        {
            if (args.IsSelected) return Skin.Graph.ElementSelectedLineColor;
            if (args.IsFramed) return Skin.Graph.ElementFramedLineColor;
            if (args.BackEffectStyle == Graphs.TimeGraphElementBackEffectStyle.Simple)
            {   // Simple prvek má barvu okraje pouze tehdy, když je explicitn definována v LineColor:
                if (args.LineColor.HasValue) return (args.LineColor.Value.IsEmpty ? (Color?)null : args.LineColor);    // Pokud LineColor = Empty, pak se nekreslí.
                // Pokud Simple prvek nemá definovánu barvu LineColor, pak se jeho okraj nekreslí:
                return null;
            }
            if (args.LineColor.HasValue) return (args.LineColor.Value.IsEmpty ? (Color?)null : args.LineColor);        // Pokud LineColor = Empty, pak se nekreslí.
            return args.BackColor;
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
            /// Explicitně zadaná barva linky okolo prvku.
            /// Pokud je null, odvodí se podle BarckColor.
            /// Pokud není null a je Empty, nebude linka kreslena.
            /// </summary>
            public Color? LineColor { get; set; }
            /// <summary>
            /// Interaktivní stav
            /// </summary>
            public GInteractiveState InteractiveState { get; set; }
            /// <summary>
            /// Styl efektu pozadí prvku
            /// </summary>
            public Components.Graphs.TimeGraphElementBackEffectStyle BackEffectStyle { get; set; }
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
            /// <para/>
            /// Aby bylo vykresleno Ratio, je nutno zadat přinejmenším <see cref="RatioBegin"/> a (<see cref="RatioBeginBackColor"/> nebo <see cref="RatioLineColor"/>).
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
            /// Styl kreslení Ratio: Vertical = odspodu nahoru, Horizontal = Zleva doprava
            /// </summary>
            public Components.Graphs.TimeGraphElementRatioStyle RatioStyle { get; set; }
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
        #region Práce se styly
        /// <summary>
        /// Vrátí výšku řádku textu, bez okrajů <see cref="TextBorderStyle.TextMargin"/> a bez borderu <see cref="TextBorderStyle.BorderType"/>, pro daný styl.
        /// </summary>
        /// <param name="style">Styl textboxu, buď konkrétní z <see cref="TextEdit.Style"/>, nebo základní <see cref="Styles.TextBox"/>.</param>
        /// <returns></returns>
        public static int GetOneTextLineHeight(ILabelStyle style)
        {
            FontInfo font = style.Font;
            return FontManagerInfo.GetFontHeight(font);
        }
        /// <summary>
        /// Optimální výška textboxu pro správné zobrazení jednořádkového textu, pro daný styl.
        /// Výška zahrnuje aktuální velikost okrajů dle <see cref="BorderStyle"/> plus vnitřní okraj <see cref="TextBorderStyle.TextMargin"/> plus výšku jednoho řádku textu.
        /// </summary>
        /// <param name="style">Styl textboxu, buď konkrétní z <see cref="TextEdit.Style"/>, nebo základní <see cref="Styles.TextBox"/>.</param>
        /// <returns></returns>
        public static int GetSingleLineOptimalHeight(ITextBoxStyle style)
        {
            BorderType borderType = style.BorderType;
            int borderWidth = Painter.GetBorderWidth(borderType);
            int textMargin = style.TextMargin;
            FontInfo font = style.Font;
            int fontHeight = FontManagerInfo.GetFontHeight(font);
            return (fontHeight + 2 * (textMargin + borderWidth));
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
        /// <param name="positions"></param>
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
        /// <param name="positions"></param>
        /// <param name="joinBroken"></param>
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
        /// <param name="partSelector"></param>
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
        /// <param name="partSelector"></param>
        /// <param name="joinBroken"></param>
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
        /// <param name="lineHandler"></param>
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
        /// <param name="partSelector"></param>
        /// <param name="lineHandler"></param>
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
        /// <param name="partSelector"></param>
        /// <param name="joinBroken"></param>
        /// <param name="lineHandler"></param>
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
        /// Metoda vrátí <see cref="GraphicsPath"/>, která reprezentuje spojovací linii dvou bodů, daného tvaru.
        /// </summary>
        /// <param name="lineType"></param>
        /// <param name="prevPoint"></param>
        /// <param name="nextPoint"></param>
        /// <param name="treshold">Prahová hodnota, pod kterou se místo čáry ZigZag bude kreslit Straight. 
        /// Pokud by vzdálenost prevPoint a nextPoint v klíčovém směru byla menší než treshold, 
        /// pak by lomená čára vykreslená pomocí Windows s nějakým větším LineCap byla ošklivá.
        /// Bylo to zjištěno pro typ linky <see cref="LinkLineType.ZigZagHorizontal"/>, pokud je větší šířka čáry, tvar zakončení je <see cref="LineCap.ArrowAnchor"/>,
        /// a vzdálenost bodů na ose Y je menší, pak při vykreslení (DrawLinkPath()) byl ignorován předposlední bod, a linka neměla tvar ZigZag (Svisle - Vodorovně - Svisle),
        /// protože poslední Svisle bylo menší než bylo potřeba pro vykreslení závěrečné šipky (ArrowAnchor), a tak se místo posledních dvou úseků (Vodorovně - Svisle)
        /// natáhla šikmá linka po úhlopříčce.
        /// A toto chování je navíc závislé i na šířce čáry, protože podle ní se odvozuje délka ArrowAnchor.
        /// Proto je pro čáry ZigZag vhodné zadávat treshold ve velikosti cca 2.5 * šířka čáry.
        /// </param>
        /// <returns></returns>
        internal static GraphicsPath CreatePathLink(LinkLineType lineType, Point? prevPoint, Point? nextPoint, float? treshold = null)
        {
            switch (lineType)
            {
                case LinkLineType.StraightLine: return CreatePathStraightLine(prevPoint, nextPoint);
                case LinkLineType.SCurveHorizontal: return CreatePathSCurveHorizontal(prevPoint, nextPoint);
                case LinkLineType.SCurveVertical: return CreatePathSCurveVertical(prevPoint, nextPoint);
                case LinkLineType.ZigZagHorizontal: return CreatePathLinkZigZagHorizontal(prevPoint, nextPoint, treshold);
                case LinkLineType.ZigZagVertical: return CreatePathLinkZigZagVertical(prevPoint, nextPoint, treshold);
                case LinkLineType.ZigZagOptimal: return CreatePathLinkZigZagOptimal(prevPoint, nextPoint, treshold);
            }
            return null;
        }
        /// <summary>
        /// Metoda vrátí <see cref="GraphicsPath"/>, která reprezentuje prostou přímou linku, která jde z bodu "prevPoint" do bodu "nextPoint".
        /// <para/>
        /// Kterýkoli z bodů "prevPoint" a "nextPoint" může být null. 
        /// Pokud jsou null oba, vrací se null.
        /// Pokud je null jeden, je vrácena krátká vodorovná linka ve směru zleva doprava z/do bodu, který není null.
        /// </summary>
        /// <param name="prevPoint">Bod počátku</param>
        /// <param name="nextPoint">Bod konce</param>
        /// <returns></returns>
        internal static GraphicsPath CreatePathStraightLine(Point? prevPoint, Point? nextPoint)
        {
            if (!prevPoint.HasValue && !nextPoint.HasValue) return null;
            if (!prevPoint.HasValue || !nextPoint.HasValue) return _CreatePathLinkHalf(prevPoint, nextPoint, 12);

            // Máme tedy oba body. Co mezi nimi budeme kreslit?
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int px, py, nx, ny;
            px = prevPoint.Value.X;
            py = prevPoint.Value.Y;
            nx = nextPoint.Value.X;
            ny = nextPoint.Value.Y;
            path.AddLine(px, py, nx, ny);
            return path;
        }
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
        /// Kterýkoli z bodů "prevPoint" a "nextPoint" může být null. 
        /// Pokud jsou null oba, vrací se null.
        /// Pokud je null jeden, je vrácena krátká vodorovná linka ve směru zleva doprava z/do bodu, který není null.
        /// </summary>
        /// <param name="prevPoint">Bod počátku</param>
        /// <param name="nextPoint">Bod konce</param>
        /// <param name="asSCurve">Tvar S-křivky</param>
        /// <returns></returns>
        internal static GraphicsPath CreatePathLinkLine(Point? prevPoint, Point? nextPoint, bool asSCurve)
        {
            if (!prevPoint.HasValue && !nextPoint.HasValue) return null;
            if (!prevPoint.HasValue || !nextPoint.HasValue) return _CreatePathLinkHalf(prevPoint, nextPoint, 12);

            // Máme tedy oba body. Co mezi nimi budeme kreslit?
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int px, py, nx, ny, dx, dy, bx, by;

            px = prevPoint.Value.X;
            py = prevPoint.Value.Y;
            nx = nextPoint.Value.X;
            ny = nextPoint.Value.Y;
            dx = nx - px;              // Vzdálenost (Next - Prev).X: kladná = jdeme doprava, záporná = jdeme doleva
            dy = ny - py;              // Vzdálenost (Next - Prev).Y: kladná = jdeme dolů,    záporná = jdeme nahoru
            bool sameY = (dy < 5 && dy > -5);
            if (sameY)
            {   // Prvky jsou na (skoro) stejné souřadnici Y, takže bez ohledu na požavek "asSCurve" to nebude klasická S-křivka:
                if (dx >= 0)
                {   // Next je (v nebo) za Prev, takže to bude přímka, jen ji možná trochu prodloužím:
                    int addx = (dx < 3 ? 2 : (dx < 5 ? 1 : 0));
                    px = px - addx;
                    nx = nx + addx;
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
        /// Kterýkoli z bodů "prevPoint" a "nextPoint" může být null. 
        /// Pokud jsou null oba, vrací se null.
        /// Pokud je null jeden, je vrácena krátká vodorovná linka ve směru zleva doprava z/do bodu, který není null.
        /// </summary>
        /// <param name="prevPoint">Bod počátku</param>
        /// <param name="nextPoint">Bod konce</param>
        /// <returns></returns>
        internal static GraphicsPath CreatePathSCurveHorizontal(Point? prevPoint, Point? nextPoint)
        {
            if (!prevPoint.HasValue && !nextPoint.HasValue) return null;
            if (!prevPoint.HasValue || !nextPoint.HasValue) return _CreatePathLinkHalf(prevPoint, nextPoint, 12);

            // Máme tedy oba body. Co mezi nimi budeme kreslit?
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int px, py, nx, ny, dx, dy, bx, by;

            px = prevPoint.Value.X;
            py = prevPoint.Value.Y;
            nx = nextPoint.Value.X;
            ny = nextPoint.Value.Y;
            dx = nx - px;              // Vzdálenost (Next - Prev).X: kladná = jdeme doprava, záporná = jdeme doleva
            dy = ny - py;              // Vzdálenost (Next - Prev).Y: kladná = jdeme dolů,    záporná = jdeme nahoru
            bool sameY = (dy < 5 && dy > -5);
            if (sameY)
            {   // Prvky jsou na (skoro) stejné souřadnici Y, takže bez ohledu na požavek "asSCurve" to nebude klasická S-křivka:
                if (dx >= 0)
                {   // Next je (v nebo) za Prev, takže to bude přímka, jen ji možná trochu prodloužím:
                    int addx = (dx < 3 ? 2 : (dx < 5 ? 1 : 0));
                    px = px - addx;
                    nx = nx + addx;
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
        /// Kterýkoli z bodů "prevPoint" a "nextPoint" může být null. 
        /// Pokud jsou null oba, vrací se null.
        /// Pokud je null jeden, je vrácena krátká vodorovná linka ve směru zleva doprava z/do bodu, který není null.
        /// </summary>
        /// <param name="prevPoint">Bod počátku</param>
        /// <param name="nextPoint">Bod konce</param>
        /// <returns></returns>
        internal static GraphicsPath CreatePathSCurveVertical(Point? prevPoint, Point? nextPoint)
        {
            if (!prevPoint.HasValue && !nextPoint.HasValue) return null;
            if (!prevPoint.HasValue || !nextPoint.HasValue) return _CreatePathLinkHalf(prevPoint, nextPoint, 12);

            // Máme tedy oba body. Co mezi nimi budeme kreslit?
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int px, py, nx, ny, dx, dy, bx, by;

            px = prevPoint.Value.X;
            py = prevPoint.Value.Y;
            nx = nextPoint.Value.X;
            ny = nextPoint.Value.Y;
            dx = nx - px;              // Vzdálenost (Next - Prev).X: kladná = jdeme doprava, záporná = jdeme doleva
            dy = ny - py;              // Vzdálenost (Next - Prev).Y: kladná = jdeme dolů,    záporná = jdeme nahoru

            // Bezierova křivka z Prev do Next - určíme hodnotu by (vzdálenost řídícího bodu na ose Y):
            by = dy / 4;                         // Výchozí hodnota je 1/4 vzdálenosti Prev a Next (kladné číslo)
            if (by >= 0 && by < 20) by = 20;     // Konstanta pro případ, kdy dy je příliš malé, aby se S-křivka projevila
            if (by < 0 && by > -20) by = -20;

            bx = (dx < 0 ? -dx : dx) / 4;        // Vliv vzdálenosti ve směru X
            if (bx > 40) bx = 40;
            if (by < bx) by = bx;                // Pokud jsou prvky Prev a Next od sebe ve směru Y daleko, zvětšíme křivku.

            path.AddBezier(px, py, px, py + by, nx, ny - by, nx, ny);
            return path;
        }
        /// <summary>
        /// Metoda vrátí <see cref="GraphicsPath"/>, která reprezentuje prostou lomenou linku, která jde z bodu "prevPoint" do bodu "nextPoint".
        /// Křivka vede z bodu <paramref name="prevPoint"/> vodorovně do poloviční vzdálenosti (ve směru X) k bodu <paramref name="nextPoint"/>,
        /// pak se zlomí do svislé, vede do souřadnice Y bodu <paramref name="nextPoint"/>, a pak běží vodorvně až do cíle.
        /// <para/>
        /// Kterýkoli z bodů "prevPoint" a "nextPoint" může být null. 
        /// Pokud jsou null oba, vrací se null.
        /// Pokud je null jeden, je vrácena krátká vodorovná linka ve směru zleva doprava z/do bodu, který není null.
        /// </summary>
        /// <param name="prevPoint">Bod počátku</param>
        /// <param name="nextPoint">Bod konce</param>
        /// <param name="treshold">Prahová hodnota, pod kterou se místo čáry ZigZag bude kreslit Straight. 
        /// Pokud by vzdálenost prevPoint a nextPoint v klíčovém směru byla menší než treshold, 
        /// pak by lomená čára vykreslená pomocí Windows s nějakým větším LineCap byla ošklivá.
        /// Bylo to zjištěno pro typ linky <see cref="LinkLineType.ZigZagHorizontal"/>, pokud je větší šířka čáry, tvar zakončení je <see cref="LineCap.ArrowAnchor"/>,
        /// a vzdálenost bodů na ose Y je menší, pak při vykreslení (DrawLinkPath()) byl ignorován předposlední bod, a linka neměla tvar ZigZag (Svisle - Vodorovně - Svisle),
        /// protože poslední Svisle bylo menší než bylo potřeba pro vykreslení závěrečné šipky (ArrowAnchor), a tak se místo posledních dvou úseků (Vodorovně - Svisle)
        /// natáhla šikmá linka po úhlopříčce.
        /// A toto chování je navíc závislé i na šířce čáry, protože podle ní se odvozuje délka ArrowAnchor.
        /// Proto je pro čáry ZigZag vhodné zadávat treshold ve velikosti cca 2.5 * šířka čáry.
        /// </param>
        /// <returns></returns>
        internal static GraphicsPath CreatePathLinkZigZagHorizontal(Point? prevPoint, Point? nextPoint, float? treshold = null)
        {
            if (!prevPoint.HasValue && !nextPoint.HasValue) return null;
            if (!prevPoint.HasValue || !nextPoint.HasValue) return _CreatePathLinkHalf(prevPoint, nextPoint, 12);

            // Máme tedy oba body. Co mezi nimi budeme kreslit?
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int px, py, nx, ny, dx, dy, hx;

            px = prevPoint.Value.X;
            py = prevPoint.Value.Y;
            nx = nextPoint.Value.X;
            ny = nextPoint.Value.Y;
            dx = nx - px;              // Vzdálenost (Next - Prev).X: kladná = jdeme doprava, záporná = jdeme doleva
            dy = ny - py;              // Vzdálenost (Next - Prev).Y: kladná = jdeme dolů,    záporná = jdeme nahoru
            hx = px + (dx / 2);        // Poloviční souřadnice X, kde se křivka lomí z vodorovné do svislé

            if (_EnableLineZigZag(dy, dx, treshold))
            {   // Vykreslíme ZigZag, protože máme dostatek prostoru:
                path.AddLine(px, py, hx, py);    // Vodorovně z Prev do půli cesty k Next
                path.AddLine(hx, py, hx, ny);    // Svisle k Next
                path.AddLine(hx, ny, nx, ny);    // Vodorovně do Next
            }
            else
            {   // Máme málo místa, vykreslíme Straight:
                path.AddLine(px, py, nx, ny);    // Přímo ze začátku do konce
            }
            return path;
        }
        /// <summary>
        /// Metoda vrátí <see cref="GraphicsPath"/>, která reprezentuje prostou lomenou linku, která jde z bodu "prevPoint" do bodu "nextPoint".
        /// Křivka vede z bodu <paramref name="prevPoint"/> svisle do poloviční vzdálenosti (ve směru Y) k bodu <paramref name="nextPoint"/>,
        /// pak se zlomí do vodorovné, vede do souřadnice X bodu <paramref name="nextPoint"/>, a pak běží vodorvně až do cíle.
        /// <para/>
        /// Kterýkoli z bodů "prevPoint" a "nextPoint" může být null. 
        /// Pokud jsou null oba, vrací se null.
        /// Pokud je null jeden, je vrácena krátká vodorovná linka ve směru zleva doprava z/do bodu, který není null.
        /// </summary>
        /// <param name="prevPoint">Bod počátku</param>
        /// <param name="nextPoint">Bod konce</param>
        /// <param name="treshold">Prahová hodnota, pod kterou se místo čáry ZigZag bude kreslit Straight. 
        /// Pokud by vzdálenost prevPoint a nextPoint v klíčovém směru byla menší než treshold, 
        /// pak by lomená čára vykreslená pomocí Windows s nějakým větším LineCap byla ošklivá.
        /// Bylo to zjištěno pro typ linky <see cref="LinkLineType.ZigZagHorizontal"/>, pokud je větší šířka čáry, tvar zakončení je <see cref="LineCap.ArrowAnchor"/>,
        /// a vzdálenost bodů na ose Y je menší, pak při vykreslení (DrawLinkPath()) byl ignorován předposlední bod, a linka neměla tvar ZigZag (Svisle - Vodorovně - Svisle),
        /// protože poslední Svisle bylo menší než bylo potřeba pro vykreslení závěrečné šipky (ArrowAnchor), a tak se místo posledních dvou úseků (Vodorovně - Svisle)
        /// natáhla šikmá linka po úhlopříčce.
        /// A toto chování je navíc závislé i na šířce čáry, protože podle ní se odvozuje délka ArrowAnchor.
        /// Proto je pro čáry ZigZag vhodné zadávat treshold ve velikosti cca 2.5 * šířka čáry.
        /// </param>
        /// <returns></returns>
        internal static GraphicsPath CreatePathLinkZigZagVertical(Point? prevPoint, Point? nextPoint, float? treshold = null)
        {
            if (!prevPoint.HasValue && !nextPoint.HasValue) return null;
            if (!prevPoint.HasValue || !nextPoint.HasValue) return _CreatePathLinkHalf(prevPoint, nextPoint, 12);

            // Máme tedy oba body. Co mezi nimi budeme kreslit?
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int px, py, nx, ny, dx, dy, hy;

            px = prevPoint.Value.X;
            py = prevPoint.Value.Y;
            nx = nextPoint.Value.X;
            ny = nextPoint.Value.Y;
            dx = nx - px;              // Vzdálenost (Next - Prev).X: kladná = jdeme doprava, záporná = jdeme doleva
            dy = ny - py;              // Vzdálenost (Next - Prev).Y: kladná = jdeme dolů,    záporná = jdeme nahoru
            hy = py + (dy / 2);        // Poloviční souřadnice Y, kde se křivka lomí z svislé do vodorovné

            if (_EnableLineZigZag(dx, dy, treshold))
            {   // Vykreslíme ZigZag, protože máme dostatek prostoru:
                path.AddLine(px, py, px, hy);    // Svisle z Prev do půli cesty k Next
                path.AddLine(px, hy, nx, hy);    // Vodorovně k Next
                path.AddLine(nx, hy, nx, ny);    // Svisle do Next
            }
            else
            {   // Máme málo místa, vykreslíme Straight:
                path.AddLine(px, py, nx, ny);    // Přímo ze začátku do konce
            }
            return path;
        }
        /// <summary>
        /// Metoda vrátí <see cref="GraphicsPath"/>, která reprezentuje čáru <see cref="LinkLineType.ZigZagOptimal"/> 
        /// </summary>
        /// <param name="prevPoint"></param>
        /// <param name="nextPoint"></param>
        /// <param name="treshold"></param>
        /// <returns></returns>
        internal static GraphicsPath CreatePathLinkZigZagOptimal(Point? prevPoint, Point? nextPoint, float? treshold = null)
        {
            if (!prevPoint.HasValue && !nextPoint.HasValue) return null;
            if (!prevPoint.HasValue || !nextPoint.HasValue) return _CreatePathLinkHalf(prevPoint, nextPoint, 12);

            int dx = nextPoint.Value.X - prevPoint.Value.X;
            int th = (treshold.HasValue && treshold.Value > 0f ? (int)(Math.Ceiling(2f * treshold.Value)) : 8);
            if (dx > th)
                return CreatePathLinkZigZagHorizontal(prevPoint, nextPoint, treshold);
            else
                return CreatePathLinkZigZagVertical(prevPoint, nextPoint, treshold);
        }
        /// <summary>
        /// Vrátí true, pokud pro danou vzdálenost bodů prevPoint a nextPoint na důležité ose (Horizontal: X; Vertical: Y) a daná práh (treshold)
        /// je vhodné vytvářet čáru ZigZag (vrací true) nebo dát Straight (když vrací false).
        /// </summary>
        /// <param name="length">Celá vzdálenost bodů Prev a Next v NEklíčové ose</param>
        /// <param name="diff">Celá vzdálenost bodů Prev a Next v klíčové ose</param>
        /// <param name="treshold">Prahová hodnota, pod kterou se místo čáry ZigZag bude kreslit Straight. 
        /// Pokud by vzdálenost prevPoint a nextPoint v klíčovém směru byla menší než treshold, 
        /// pak by lomená čára vykreslená pomocí Windows s nějakým větším LineCap byla ošklivá.
        /// Bylo to zjištěno pro typ linky <see cref="LinkLineType.ZigZagHorizontal"/>, pokud je větší šířka čáry, tvar zakončení je <see cref="LineCap.ArrowAnchor"/>,
        /// a vzdálenost bodů na ose Y je menší, pak při vykreslení (DrawLinkPath()) byl ignorován předposlední bod, a linka neměla tvar ZigZag (Svisle - Vodorovně - Svisle),
        /// protože poslední Svisle bylo menší než bylo potřeba pro vykreslení závěrečné šipky (ArrowAnchor), a tak se místo posledních dvou úseků (Vodorovně - Svisle)
        /// natáhla šikmá linka po úhlopříčce.
        /// A toto chování je navíc závislé i na šířce čáry, protože podle ní se odvozuje délka ArrowAnchor.
        /// Proto je pro čáry ZigZag vhodné zadávat treshold ve velikosti cca 2.5 * šířka čáry.
        /// </param>
        /// <returns></returns>
        private static bool _EnableLineZigZag(int length, int diff, float? treshold = null)
        {
            if (length == 0 || diff == 0) return false;    // Pokud v jedné nebo druhé ose je rozdíl bodů == 0, pak NEkreslíme ZigZag, ale Straight

            if (!treshold.HasValue || treshold.Value <= 0f) return true;       // Pokud není zadáno, pak dáme ZigZag bez omezení
            int half = diff / 2;                 // diff je vzdálenost bodů prev a next, ale ZigZag kreslí cílovou čáru v poloviční délce
            if (half < 0) half = -half;          // zajímá nás Abs(half)
            int tres = (int)Math.Ceiling(treshold.Value);
            return (half >= tres);               // ZigZag můžeme kreslit, když daný prostor je větší než treshold (Ceiling)
        }
        /// <summary>
        /// Vytvoří a vrátí Half-čáru, kdy pouze jeden z bodů je zadán...
        /// </summary>
        /// <param name="prevPoint"></param>
        /// <param name="nextPoint"></param>
        /// <param name="halfLength"></param>
        /// <returns></returns>
        private static GraphicsPath _CreatePathLinkHalf(Point? prevPoint, Point? nextPoint, int halfLength)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int px, py, nx, ny;

            // Mám jen prvek Prev: vykreslím linku "z Prev doprava":
            if (!nextPoint.HasValue)
            {
                px = prevPoint.Value.X;
                py = prevPoint.Value.Y;
                nx = px + halfLength;
                ny = py;
                path.AddLine(px, py, nx, ny);
                return path;
            }

            // Mám jen prvek Next: vykreslím linku "zleva do Next":
            if (!prevPoint.HasValue)
            {
                nx = nextPoint.Value.X;
                ny = nextPoint.Value.Y;
                px = nx - halfLength;
                py = ny;
                path.AddLine(px, py, nx, ny);
                return path;
            }

            return null;
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
        /// <param name="graphics">Grafika pro kreslení</param>
        /// <param name="graphicsPath">Tvar linky k vykreslení</param>
        /// <param name="colorLine">Barva linky. Pokud je null, nebude se kreslit vnitřní linka.</param>
        /// <param name="colorBack">Barva podkreslení, aby linka byla viditelná na každém podkladu; kreslí se tatáž linka ale s větší šířkou. Zadání null = automatická barva mezi barvou <paramref name="colorLine"/> a černou. Zadání Empty = nebude podkreslení.</param>
        /// <param name="width">Šířka linky, akceptuje se šířka 1 až 12 pixelů</param>
        /// <param name="startCap"></param>
        /// <param name="endCap"></param>
        /// <param name="opacityRatio">Průhlednost linky</param>
        /// <param name="setSmoothGraphics">Nastavit hladkou grafiku?</param>
        internal static void DrawLinkPath(Graphics graphics, System.Drawing.Drawing2D.GraphicsPath graphicsPath, Color? colorLine, Color? colorBack = null, int? width = null,
            System.Drawing.Drawing2D.LineCap startCap = LineCap.Round, System.Drawing.Drawing2D.LineCap endCap = LineCap.ArrowAnchor,
            float? opacityRatio = null, bool setSmoothGraphics = false)
        {
            if (graphicsPath == null) return;

            GraphicSetting? setting = (setSmoothGraphics ? (GraphicSetting?)GraphicSetting.Smooth : null);
            using (Painter.GraphicsUse(graphics, setting))
            {
                Pen pen;
                Color colorOut = (colorBack.HasValue ? colorBack.Value : (colorLine.HasValue ? colorLine.Value.Morph(Color.Black, 0.80f) : Color.Empty));
                float width1 = (width.HasValue ? (width.Value < 1 ? 1f : (width.Value > 12 ? 12f : (float)width.Value)) : 1f);

                if (!colorOut.IsEmpty)
                {
                    float width2 = (width1 <= 5f ? width1 + 2f : 1.4f * width1);             // Do šířky 5px včetně přidávám 2px, pro šířku větší přidávám 20% na každou stranu
                    pen = Skin.Pen(colorOut, width2, opacityRatio: opacityRatio, startCap: startCap, endCap: endCap);
                    graphics.DrawPath(pen, graphicsPath);
                }

                if (colorLine.HasValue)
                {
                    pen = Skin.Pen(colorLine.Value, width1, opacityRatio: opacityRatio, startCap: LineCap.Round, endCap: endCap);
                    graphics.DrawPath(pen, graphicsPath);
                }
            }
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
        #region CreateMenu
        /// <summary>
        /// Vytvoří a vrátí menu s danými vlastnostmi, volitelně obsahující položku pro titulek (plus separátor pod ním)
        /// </summary>
        /// <param name="dropShadowEnabled"></param>
        /// <param name="showCheckMargin"></param>
        /// <param name="showImageMargin"></param>
        /// <param name="showItemToolTips"></param>
        /// <param name="renderMode"></param>
        /// <param name="backColor"></param>
        /// <param name="opacity"></param>
        /// <param name="imageScalingSize"></param>
        /// <param name="title"></param>
        /// <param name="titleToolTip"></param>
        /// <returns></returns>
        public static ToolStripDropDownMenu CreateDropDownMenu(
            bool dropShadowEnabled = true,
            bool showCheckMargin = false,
            bool showImageMargin = true,
            bool showItemToolTips = true,
            ToolStripRenderMode renderMode = ToolStripRenderMode.Professional,
            Color? backColor = null,
            float? opacity = null,
            Size? imageScalingSize = null,
            string title = null,
            string titleToolTip = null)
        {
            ToolStripDropDownMenu menu = new ToolStripDropDownMenu();
            menu.DropShadowEnabled = dropShadowEnabled;
            menu.ShowCheckMargin = showCheckMargin;
            menu.ShowImageMargin = showImageMargin;
            menu.ShowItemToolTips = showItemToolTips;
            menu.RenderMode = renderMode;
            if (backColor.HasValue) menu.BackColor = backColor.Value;
            if (opacity.HasValue) menu.Opacity = opacity.Value;
            if (imageScalingSize.HasValue) menu.ImageScalingSize = imageScalingSize.Value;

            if (!String.IsNullOrEmpty(title))
            {
                ToolStripLabel titleItem = new ToolStripLabel(title);
                titleItem.ToolTipText = titleToolTip;
                if (imageScalingSize.HasValue)
                    titleItem.Size = new Size(100, imageScalingSize.Value.Height + 4);
                titleItem.Font = new Font(titleItem.Font, System.Drawing.FontStyle.Bold);
                titleItem.TextAlign = ContentAlignment.MiddleCenter;
                menu.Items.Add(titleItem);

                menu.Items.Add(new ToolStripSeparator());
            }

            return menu;
        }
        /// <summary>
        /// Vytvoří a vrátí položku menu
        /// </summary>
        /// <param name="text"></param>
        /// <param name="image"></param>
        /// <param name="toolTip"></param>
        /// <param name="isEnabled"></param>
        /// <param name="isCheckable"></param>
        /// <param name="isChecked"></param>
        /// <param name="backColor"></param>
        /// <param name="fontStyle"></param>
        /// <param name="name"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static ToolStripMenuItem CreateDropDownItem(
            string text,
            Image image = null,
            string toolTip = null,
            bool isEnabled = true,
            bool isCheckable = false,
            bool isChecked = false,
            Color? backColor = null,
            System.Drawing.FontStyle? fontStyle = null,
            string name = null,
            object tag = null
            )
        {
            System.Windows.Forms.ToolStripMenuItem item = new ToolStripMenuItem(text, image);
            item.Name = name;
            if (backColor.HasValue)
                item.BackColor = backColor.Value;

            if (!String.IsNullOrEmpty(toolTip))
            {
                item.ToolTipText = toolTip;
                item.AutoToolTip = true;
            }
            if (fontStyle.HasValue)
                item.Font = new Font(item.Font, fontStyle.Value);

            item.Enabled = isEnabled;
            item.CheckOnClick = isCheckable;
            item.Checked = isChecked;
            item.Tag = tag;

            return item;
        }
        /// <summary>
        /// Vrátí separátor do DropDown menu
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static ToolStripSeparator CreateDropDownSeparator(string name = null, object tag = null)
        {
            ToolStripSeparator item = new ToolStripSeparator();
            item.Name = name;
            item.Tag = tag;
            return item;
        }
        #endregion
        #region Set Graphics, restore previous state
        /// <summary>
        /// Prepare Graphics for Smooth / Text / Sharp drawing, by graphicSetting value.
        /// Return its original state, can be restored after drawing:  
        /// var state = Painter.GraphicsSet(graphics, GraphicSetting.Text); any drawing...;  graphics.Restore(state);
        /// Restore of state is not requierd.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="graphicSetting"></param>
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
        /// <param name="graphicSetting"></param>
        /// <returns></returns>
        internal static IDisposable GraphicsUse(Graphics graphics, GraphicSetting? graphicSetting)
        {
            if (!graphicSetting.HasValue) return null;
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
        /// <param name="setClip"></param>
        /// <param name="graphicSetting"></param>
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
        /// Set specified rectangle as Clip region to Graphics. On Dispose returns original Clip region.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="setClip"></param>
        /// <returns></returns>
        internal static IDisposable GraphicsUseSmooth(Graphics graphics, Rectangle? setClip = null)
        {
            IDisposable state = (setClip.HasValue ? new GraphicsStateRestore(graphics, setClip.Value) : new GraphicsStateRestore(graphics));
            if (setClip.HasValue)
                graphics.SetClip(setClip.Value);
            _GraphicsSetSmooth(graphics);
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
        /// <param name="setClip"></param>
        /// <param name="renderingHint"></param>
        /// <returns></returns>
        internal static IDisposable GraphicsUseText(Graphics graphics, Rectangle? setClip = null, System.Drawing.Text.TextRenderingHint renderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias)
        {
            IDisposable state = (setClip.HasValue ? new GraphicsStateRestore(graphics, setClip.Value) : new GraphicsStateRestore(graphics));
            if (setClip.HasValue)
                graphics.SetClip(setClip.Value);
            _GraphicsSetText(graphics, renderingHint);
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
        /// <param name="setClip"></param>
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
        /// <param name="opacity"></param>
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
        /// <param name="renderingHint"></param>
        private static void _GraphicsSetText(Graphics graphics, System.Drawing.Text.TextRenderingHint renderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias)
        {
            //graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;        // SmoothingMode.AntiAlias poskytuje ideální hladké kreslení grafiky
            //graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;  // Nemá vliv na vykreslování čehokoliv
            //graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;     // TextRenderingHint.AntiAlias je jediný, který garantuje korektní měření textu, což je nezbytné pro ContentAlignement;


            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;         // Nebude přesně zarovnávat Align = RightToLeft, ale všechna písmena budou čitelnější.
            graphics.SmoothingMode = SmoothingMode.None;
            graphics.InterpolationMode = InterpolationMode.Default;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;


            // graphics.SmoothingMode       nemá vliv na kvalitu písma
            // graphics.TextContrast = 6    nemá vliv
            // graphics.TextRenderingHint = Nejhezčí = nejčitelnější je SystemDefault. Ale AntiAlias dává nejpřesnější výsledky v měření fontu, což je klíčem pro správné zarovnání textu ContentAlignement, nejvíce je vidět v zarovnání doprava.
            //  vcelku OK :  AntiAliasGridFit, ClearTypeGridFit
            //  nic moc   :  AntiAlias, SystemDefault
            //  hrozný    :  SingleBitPerPixel, SingleBitPerPixelGridFit
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
    #region class DrawButtonArgs : třída argumentů pro kreslení Buttonu
    /// <summary>
    /// Třída pro kreslení buttonů
    /// </summary>
    public class DrawButtonArgs
    {
        /// <summary>
        /// Konsturktor
        /// </summary>
        public DrawButtonArgs()
        {
            this.BackColor = Skin.Button.BackColor;
            this.BorderColor = Skin.Button.BorderColor;
            this.InteractiveState = GInteractiveState.Enabled;
            this.Orientation = Orientation.Horizontal;
            this.RoundCorner = 0;
            this.MouseTrackPoint = null;
            this.Opacity = null;
            this.DrawBackground = true;
            this.DrawBorders = true;
        }
        /// <summary>
        /// Barva pozadí
        /// </summary>
        public Color BackColor { get; set; }
        /// <summary>
        /// Barva okraje
        /// </summary>
        public Color? BorderColor { get; set; }
        /// <summary>
        /// Interaktivní stav
        /// </summary>
        public GInteractiveState InteractiveState { get; set; }
        /// <summary>
        /// Orientace barvy pozadí
        /// </summary>
        public Orientation Orientation { get; set; }
        /// <summary>
        /// Kulaté rohy
        /// </summary>
        public int RoundCorner { get; set; }
        /// <summary>
        /// Souřadnice myši pro vykreslení TrackPointu
        /// </summary>
        public Point? MouseTrackPoint { get; set; }
        /// <summary>
        /// Průhlednost
        /// </summary>
        public Int32? Opacity { get; set; }
        /// <summary>
        /// Vykreslit background?
        /// </summary>
        public bool DrawBackground { get; set; }
        /// <summary>
        /// Vykreslit borders?
        /// </summary>
        public bool DrawBorders { get; set; }

    }
    #endregion
    #region interface IScrollBarPaintData : Interface pro vykreslení komplexní struktury Scrollbaru
    /// <summary>
    /// Interface pro vykreslení komplexní struktury Scrollbaru
    /// </summary>
    public interface IScrollBarPaintData
    {
        /// <summary>
        /// Orientace ScrollBaru
        /// </summary>
        Orientation Orientation { get; }
        /// <summary>
        /// Je Enabled?
        /// </summary>
        bool IsEnabled { get; }
        /// <summary>
        /// Souřadnice celková
        /// </summary>
        Rectangle ScrollBarBounds { get; }
        /// <summary>
        /// Barva pozadí
        /// </summary>
        Color ScrollBarBackColor { get; }
        /// <summary>
        /// Souřadnice buttonu Min (=vlevo nebo nahoře)
        /// </summary>
        Rectangle MinButtonBounds { get; }
        /// <summary>
        /// Stav buttonu Min (=vlevo nebo nahoře)
        /// </summary>
        GInteractiveState MinButtonState { get; }
        /// <summary>
        /// Souřadnice datového prostoru
        /// </summary>
        Rectangle DataAreaBounds { get; }
        /// <summary>
        /// Souřadnice datového prostoru před Thumbem
        /// </summary>
        Rectangle MinAreaBounds { get; }
        /// <summary>
        /// Stav datového prostoru před Thumbem
        /// </summary>
        GInteractiveState MinAreaState { get; }
        /// <summary>
        /// Souřadnice datového prostoru za Thumbem
        /// </summary>
        Rectangle MaxAreaBounds { get; }
        /// <summary>
        /// Stav datového prostoru za Thumbem
        /// </summary>
        GInteractiveState MaxAreaState { get; }
        /// <summary>
        /// Souřadnice buttonu Max (=vpravo nebo dole)
        /// </summary>
        Rectangle MaxButtonBounds { get; }
        /// <summary>
        /// Stav buttonu Max (=vpravo nebo dole)
        /// </summary>
        GInteractiveState MaxButtonState { get; }
        /// <summary>
        /// Souřadnice Thumbu
        /// </summary>
        Rectangle ThumbButtonBounds { get; }
        /// <summary>
        /// Stav Thumbu
        /// </summary>
        GInteractiveState ThumbButtonState { get; }
        /// <summary>
        /// Souřadnice pro ikonu v Thumbu
        /// </summary>
        Rectangle ThumbImageBounds { get; }
        /// <summary>
        /// Metoda, volaná uprostřed kreslení Scrollbaru pro vykreslení pozadí v datové oblasti
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        void UserDataDraw(Graphics graphics, Rectangle bounds);
    }
    #endregion
    #region interface ITabHeaderPaintData : Interface pro vykreslení komplexní struktury TabHeader
    /// <summary>
    /// Interface pro vykreslení komplexní struktury TabHeader
    /// </summary>
    public interface ITabHeaderItemPaintData
    {
        /// <summary>
        /// Pozice TabHeader proti datovému prostoru
        /// </summary>
        RectangleSide Position { get; }
        /// <summary>
        /// Je Enabled
        /// </summary>
        bool IsEnabled { get; }
        /// <summary>
        /// Barva pozadí
        /// </summary>
        Color? BackColor { get; }
        /// <summary>
        /// Je aktivní? (tzn. tato záložka je viditelná)
        /// </summary>
        bool IsActive { get; }
        /// <summary>
        /// Interaktivní stav
        /// </summary>
        GInteractiveState InteractiveState { get; }
        /// <summary>
        /// Písmo
        /// </summary>
        FontInfo Font { get; }
        /// <summary>
        /// Ikonka
        /// </summary>
        Image Image { get; }
        /// <summary>
        /// Souřadnice ikonky
        /// </summary>
        Rectangle ImageBounds { get; }
        /// <summary>
        /// Text
        /// </summary>
        string Text { get; }
        /// <summary>
        /// Souřadnice textu
        /// </summary>
        Rectangle TextBounds { get; }
        /// <summary>
        /// Zobrazit zavírací ikonku (button)
        /// </summary>
        bool CloseButtonVisible { get; }
        /// <summary>
        /// Souřadnice zavírací ikonky (button)
        /// </summary>
        Rectangle CloseButtonBounds { get; }
        /// <summary>
        /// Metoda volaná pro user kreslení na pozadí
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        void UserDataDraw(Graphics graphics, Rectangle bounds);
    }
    #endregion
    #region interface ITrackBarPaintData : Interface pro vykreslení komplexní struktury TrackBar
    /// <summary>
    /// interface ITrackBarPaintData : Interface pro vykreslení komplexní struktury TrackBar
    /// </summary>
    public interface ITrackBarPaintData
    {
        /// <summary>
        /// Orientace TrackBaru
        /// </summary>
        System.Windows.Forms.Orientation Orientation { get; }
        /// <summary>
        /// Interaktivní stav
        /// </summary>
        GInteractiveState InteractiveState { get; }
        /// <summary>
        /// Aktuální pozice myši nad trackbarem, konkrétní část
        /// </summary>
        TrackBarAreaType CurrentMouseArea { get; }
        /// <summary>
        /// Základní barva pozadí
        /// </summary>
        Color BackColor { get; }
        /// <summary>
        /// Souřadnice, kde je tracker aktivní na myš.
        /// Relativní k absolutní souřadnici dodané ke kreslení.
        /// </summary>
        Rectangle ActiveBounds { get; }
        /// <summary>
        /// Souřadnice, kde se vykreslují track Ticky a linky Begin, End a TrackLine.
        /// Relativní k absolutní souřadnici dodané ke kreslení.
        /// </summary>
        Rectangle TrackBounds { get; }
        /// <summary>
        /// Souřadnice, kde se nachází řídící linie trackeru (neaktivní rozměr má = 0).
        /// Relativní k absolutní souřadnici dodané ke kreslení.
        /// </summary>
        Rectangle TrackLineBounds { get; }
        /// <summary>
        /// Souřadnice středu TrackPointu, leží v souřadnicích <see cref="TrackLineBounds"/>.
        /// Relativní k absolutní souřadnici dodané ke kreslení.
        /// </summary>
        Point TrackPoint { get; }
        /// <summary>
        /// Velikost TrackPointu (ovládací knoflík TrackBaru).
        /// Velikost je udávána již orientovaná k aktuální <see cref="Orientation"/>, je tedy fyzická = Width je v ose X, Height je v ose Y.
        /// </summary>
        Size TrackSize { get; }
        /// <summary>
        /// Počet vykreslovaných úseků Ticků, null = 0 = žádný.
        /// Z hlediska logiky zadávání je počet linek ticků = (<see cref="TickCount"/> - 1).
        /// Jde o počet úseků mezi Ticky, přičemž je nutno uvažovat i krajní linky.
        /// Zadáme-li tedy <see cref="TickCount"/> = 10, bude vykresleno 10 úseků: linka vlevo, pak 9x úsek + tick, a na konci úsek + linka vpravo.
        /// </summary>
        int? TickCount { get; }
        /// <summary>
        /// Typ kreslených Ticků
        /// </summary>
        TrackBarTickType TickType { get; }
        /// <summary>
        /// Barva ticků
        /// </summary>
        Color? TickColor { get; }
        /// <summary>
        /// Barva krajních čar
        /// </summary>
        Color? EndBarColor { get; }
        /// <summary>
        /// Barva TrackLine = čáry okolo linie trackbaru
        /// </summary>
        Color? TrackLineColor { get; }
        /// <summary>
        /// Barva pozadí TrackLine, pokud je Solid
        /// </summary>
        Color? TrackBackColor { get; }
        /// <summary>
        /// Barva aktuální (=dosažené) hodnoty pozadí TrackLine, pokud je Solid
        /// </summary>
        Color? TrackActiveBackColor { get; }
        /// <summary>
        /// Barva aktuální (=od dosažené do nejvyšší) hodnoty pozadí TrackLine, pokud je Solid
        /// </summary>
        Color? TrackInactiveBackColor { get; }
        /// <summary>
        /// Barva pozadí TrackPointu = ukazatele
        /// </summary>
        Color? TrackPointBackColor { get; }
        /// <summary>
        /// Barva pozadí TrackPointu = ukazatele, ve stavu MouseOver
        /// </summary>
        Color? TrackPointMouseOverBackColor { get; }
        /// <summary>
        /// Barva pozadí TrackPointu = ukazatele, ve stavu MouseOver
        /// </summary>
        Color? TrackPointMouseDownBackColor { get; }
        /// <summary>
        /// Barva linky TrackPointu = ukazatele
        /// </summary>
        Color? TrackPointLineColor { get; }
        /// <summary>
        /// Typ track line
        /// </summary>
        TrackBarLineType TrackLineType { get; }
        /// <summary>
        /// Barvy použité pro TrackLine typu <see cref="TrackBarLineType.ColorBlendLine"/>.
        /// Pokud je null, pak takový TrackLine bude mít defaultní barevný přechod.
        /// </summary>
        IEnumerable<Tuple<float, Color>> ColorBlend { get; }
        /// <summary>
        /// Tuto metodu volá kreslící algoritmus po vykreslení backgroundu a ticků a TrackLine, před kreslením TrackPointu
        /// </summary>
        void PaintTextData(Graphics graphics, Rectangle absoluteBounds);
    }
    /// <summary>
    /// Kterou část ticků kreslíme
    /// </summary>
    [Flags]
    public enum TrackBarTickType
    {
        /// <summary>
        /// Žádný tick
        /// </summary>
        None = 0,
        /// <summary>
        /// Krátká část Ticku na začátku (podle orientace: Horizontal = nahoře, Vertical = vlevo)
        /// </summary>
        HalfBegin = 0x0001,
        /// <summary>
        /// Krátká část Ticku na konci (podle orientace: Horizontal = dole, Vertical = vpravo)
        /// </summary>
        HalfEnd = 0x0002,
        /// <summary>
        /// Obě krátké části Ticku 
        /// </summary>
        HalfBooth = 0x0003,
        /// <summary>
        /// Celá linie Ticku 
        /// </summary>
        WholeLine = 0x0007,
        /// <summary>
        /// Počáteční linie (kolmá u hodnoty Begin)
        /// </summary>
        BeginBar = 0x0010,
        /// <summary>
        /// Koncová linie (kolmá u hodnoty End)
        /// </summary>
        EndBar = 0x0020,
        /// <summary>
        /// Průběžná linka jednoduchá
        /// </summary>
        TrackLineSingle = 0x0100,
        /// <summary>
        /// Průběžná linka dvojitá
        /// </summary>
        TrackLineDouble = 0x0200,
        /// <summary>
        /// Standardní = HalfBooth | BeginBar | EndBar | TrackLineSingle
        /// </summary>
        Standard = HalfBooth | BeginBar | EndBar | TrackLineSingle,
        /// <summary>
        /// Standardní s dvojitou linií = HalfBooth | BeginBar | EndBar | TrackLineDouble
        /// </summary>
        StandardDouble = HalfBooth | BeginBar | EndBar | TrackLineDouble
    }
    /// <summary>
    /// Typ kreslení linie TrackLine
    /// </summary>
    public enum TrackBarLineType
    {
        /// <summary>
        /// Bez linie
        /// </summary>
        None = 0,
        /// <summary>
        /// Jednoduchá nebo dvojitá liniie bez výplně
        /// </summary>
        Line,
        /// <summary>
        /// Linka s barevnou výplní
        /// </summary>
        Solid,
        /// <summary>
        /// Linka s barevně proměnlivou výplní
        /// </summary>
        ColorBlendLine
    }
    /// <summary>
    /// Typ pozice v TrackBaru
    /// </summary>
    public enum TrackBarAreaType
    {
        /// <summary>
        /// Mimo
        /// </summary>
        None = 0,
        /// <summary>
        /// V neaktivní oblasti
        /// </summary>
        NonActive,
        /// <summary>
        /// V aktivní oblasti mimo Pointer
        /// </summary>
        Area,
        /// <summary>
        /// V pointeru
        /// </summary>
        Pointer
    }
    #endregion
    #region Enums
    /// <summary>
    /// Konfigurace grafiky pro různé vykreslované motivy
    /// </summary>
    public enum GraphicSetting
    {
        /// <summary>Nezadáno</summary>
        None,
        /// <summary>Konfigurace pro kreslení TEXTU</summary>
        Text,
        /// <summary>Konfigurace pro kreslení HLADKÝCH KŘIVEK</summary>
        Smooth,
        /// <summary>Konfigurace pro kreslení OSTRÝCH PRAVOÚHLÝCH ČAR</summary>
        Sharp
    }
    /// <summary>
    /// Relativní pozice dvou obdélníků
    /// </summary>
    [Flags]
    public enum RelativePosition 
    { 
        /// <summary>Nezadáno</summary>
        None        = 0x00000,
        /// <summary>Uvnitř</summary>
        Inside      = 0x00001,
        /// <summary>Nahoře vlevo</summary>
        TopLeft     = 0x00010,
        /// <summary>Nahoře</summary>
        Top         = 0x00020,
        /// <summary>Nahoře vpravo</summary>
        TopRight    = 0x00040,
        /// <summary>Vpravo nahoře</summary>
        RightTop    = 0x00100,
        /// <summary>Vpravo</summary>
        Right       = 0x00200,
        /// <summary>Vpravo dole</summary>
        RightBottom = 0x00400,
        /// <summary>Dole vpravo</summary>
        BottomRight = 0x01000,
        /// <summary>Dole</summary>
        Bottom      = 0x02000,
        /// <summary>Dole vlevo</summary>
        BottomLeft  = 0x04000,
        /// <summary>Vlevo dole</summary>
        LeftBottom  = 0x10000,
        /// <summary>Vlevo</summary>
        Left        = 0x20000,
        /// <summary>Vlevo nahoře</summary>
        LeftTop     = 0x40000,
        /// <summary>Kdekoli nahoře</summary>
        AllTop      = TopLeft | Top | TopRight,
        /// <summary>Kdekoli vpravo</summary>
        AllRight    = RightTop | Right | RightBottom,
        /// <summary>Kdekoli dole</summary>
        AllBottom   = BottomRight | Bottom | BottomLeft,
        /// <summary>Kdekoli vlevo</summary>
        AllLeft     = LeftBottom | Left | LeftTop
    }
    /// <summary>
    /// Tvary, které generuje metoda <see cref="Painter.CreatePathLinearShape(LinearShapeType, Rectangle, int)"/>
    /// </summary>
    public enum LinearShapeType
    {
        /// <summary>Nezadáno</summary>
        None,
        /// <summary>Šipka doleva</summary>
        LeftArrow,
        /// <summary>Šipka nahoru</summary>
        UpArrow,
        /// <summary>Šipka doprava</summary>
        RightArrow,
        /// <summary>Šipka dolů</summary>
        DownArrow,
        /// <summary>Vodorovné čáry</summary>
        HorizontalLines,
        /// <summary>Svislé čáry</summary>
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
    /// Typ spojovací čáry
    /// </summary>
    public enum LinkLineType : int
    {
        /// <summary>
        /// Žádná
        /// </summary>
        None = 0,
        /// <summary>
        /// Přímá linie
        /// </summary>
        StraightLine,
        /// <summary>
        /// S-křivka, kde počátek i konec jsou vodorovné
        /// </summary>
        SCurveHorizontal,
        /// <summary>
        /// S-křivka, kde počátek i konec jsou svislé
        /// </summary>
        SCurveVertical,
        /// <summary>
        /// Lomená čára, kde počátek i konec jsou vodorovné
        /// </summary>
        ZigZagHorizontal,
        /// <summary>
        /// Lomená čára, kde počátek i konec jsou svislé
        /// </summary>
        ZigZagVertical,
        /// <summary>
        /// Lomená čára optimální:
        /// Pokud konec má souřadnici X nižší než začátek (= časově zpětná), pak je vykreslena <see cref="ZigZagVertical"/> = nejprve dolů, pak zpátky, a pak zase dolů;
        /// Pokud konec má X rovno nebo vyšší, pak je vykreslena <see cref="ZigZagHorizontal"/> = nejprve doprava, pak nahoru/dolů, a pak doprava.
        /// </summary>
        ZigZagOptimal
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
    /// <summary>
    /// Umístění jednoho prostoru (typicky písmena) v rámci jiného prostoru (typicky control) včetně možnosti umístit obsah vně controlu (podle potřeby a podle definice).
    /// Pro konverzi existují metody <see cref="Painter.ConvertAlignment"/>.
    /// </summary>
    public enum ExtendedContentAlignment
    { // POZOR: AŽ BUDEŠ MĚNIT HODNOTY (přidávat nebo upravovat numerické bity), uprav i stejný enum GuiTextPosition v GraphLib\Shared\WorkSchedulerShared.cs !!!
        /// <summary>
        /// Nezadáno, použije se <see cref="Center"/>
        /// </summary>
        None = 0,
        /// <summary>
        /// Doprostřed
        /// </summary>
        Center = None,

        /// <summary>
        /// K levému vnitřnímu okraji.
        /// Pokud bude zadán současně <see cref="InnerLeft"/> i <see cref="InnerRight"/>, pak bude výsledek <see cref="Center"/>.
        /// </summary>
        InnerLeft = 0x01,
        /// <summary>
        /// K pravému vnitřnímu okraji.
        /// Pokud bude zadán současně <see cref="InnerLeft"/> i <see cref="InnerRight"/>, pak bude výsledek <see cref="Center"/>.
        /// </summary>
        InnerRight = 0x02,
        /// <summary>
        /// K hornímu vnitřnímu okraji.
        /// Pokud bude zadán současně <see cref="InnerTop"/> i <see cref="InnerBottom"/>, pak bude výsledek <see cref="Center"/>.
        /// </summary>
        InnerTop = 0x04,
        /// <summary>
        /// K dolnímu vnitřnímu okraji.
        /// Pokud bude zadán současně <see cref="InnerTop"/> i <see cref="InnerBottom"/>, pak bude výsledek <see cref="Center"/>.
        /// </summary>
        InnerBottom = 0x08,

        /// <summary>
        /// K levému okraji zvenku.
        /// Pokud bude zadán současně <see cref="OuterLeft"/> i <see cref="OuterRight"/>, pak bude výsledek <see cref="Center"/>.
        /// </summary>
        OuterLeft = 0x10,
        /// <summary>
        /// K pravému okraji zvenku.
        /// Pokud bude zadán současně <see cref="OuterLeft"/> i <see cref="OuterRight"/>, pak bude výsledek <see cref="Center"/>.
        /// </summary>
        OuterRight = 0x20,
        /// <summary>
        /// K hornímu okraji zvenku = nad daný prostor
        /// Pokud bude zadán současně <see cref="OuterTop"/> i <see cref="OuterBottom"/>, pak bude výsledek <see cref="Center"/>.
        /// </summary>
        OuterTop = 0x40,
        /// <summary>
        /// K dolnímu okraji zvenku = pod daný prostor.
        /// Pokud bude zadán současně <see cref="OuterTop"/> i <see cref="OuterBottom"/>, pak bude výsledek <see cref="Center"/>.
        /// </summary>
        OuterBottom = 0x80,

        /// <summary>
        /// Pouze uvnitř prostoru, i kdyby se dovnitř nevešel
        /// </summary>
        OnlyInner = 0x000,
        /// <summary>
        /// Nejprve uvnitř prostoru, ale pokud se dovnitř nevejde pak je možno použít vnější umístění podle 
        /// <see cref="OuterLeft"/>, <see cref="OuterRight"/>, <see cref="OuterTop"/>, <see cref="OuterBottom"/>.
        /// Pokud ale nebude nic z toho specifikováno, nebude se text umísťovat Outer.
        /// </summary>
        PreferInner = 0x100,
        /// <summary>
        /// Pouze vně daného prostoru, nikdy ne dovnitř.
        /// Musí být zadáno něco z <see cref="OuterLeft"/>, <see cref="OuterRight"/>, <see cref="OuterTop"/>, <see cref="OuterBottom"/>.
        /// Pokud nebude nic z toho specifikováno, bude text umístěn Inner.
        /// </summary>
        OnlyOuter = 0x200,
        /// <summary>
        /// Neprve vně daného prostoru, podle hodnot <see cref="OuterLeft"/>, <see cref="OuterRight"/>, <see cref="OuterTop"/>, <see cref="OuterBottom"/>.
        /// Pokud se nevejde vně prostoru (který musí být něčím určen!), teprve pak se umisťuje uvnitř.
        /// </summary>
        PreferOuter = 0x400,
        /// <summary>
        /// Povolení k přemístění u vnějšího prostoru: pokud bude např. specifikována pozice <see cref="OuterLeft"/> a vnější prostor bude omezen tak, že obsah se nevejde doleva od daného prostoru,
        /// pak <see cref="CanSwapOuter"/> způsobí, že otestujeme pozici <see cref="OuterRight"/> (tedy místo vlevo od objektu dáme popisek doprava) a případně ji využijeme.
        /// Pokud ani vpravo nebude místo, pak můžeme přejít dovnitř (pokud bude dáno <see cref="PreferOuter"/> = nejprve vnější, a pak vnitřní pozice).
        /// </summary>
        CanSwapOuter = 0x800,

        // POZOR: AŽ BUDEŠ MĚNIT HODNOTY (přidávat nebo upravovat numerické bity), uprav i stejný enum GuiTextPosition v GraphLib\Shared\WorkSchedulerShared.cs !!!

        /// <summary>
        /// K levému hornímu rohu
        /// </summary>
        InnerLeftTop = InnerLeft | InnerTop,
        /// <summary>
        /// Vodorovně: vlevo, svisle: uprostřed
        /// </summary>
        InnerLeftCenter = InnerLeft,
        /// <summary>
        /// K levému dolnímu rohu
        /// </summary>
        InnerLeftBottom = InnerLeft | InnerBottom,
        /// <summary>
        /// Vodorovně: uprostřed, svisle: nahoře
        /// </summary>
        InnerMiddleTop = InnerTop,
        /// <summary>
        /// Vodorovně: uprostřed, svisle: uprostřed
        /// </summary>
        InnerMiddleCenter = None,
        /// <summary>
        /// Vodorovně: uprostřed, svisle: dole
        /// </summary>
        InnerMiddleBottom = InnerBottom,
        /// <summary>
        /// K pravému hornímu rohu
        /// </summary>
        InnerRightTop = InnerRight | InnerTop,
        /// <summary>
        /// Vodorovně: vpravo, svisle: uprostřed
        /// </summary>
        InnerRightCenter = InnerRight,
        /// <summary>
        /// K pravému dolnímu rohu
        /// </summary>
        InnerRightBottom = InnerRight | InnerBottom

    }
    #endregion
}
