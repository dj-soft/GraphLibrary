using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Windows.Forms;
using static DjSoft.Tools.ProgramLauncher.App;
using DjSoft.Tools.ProgramLauncher.Data;

namespace DjSoft.Tools.ProgramLauncher
{
    /// <summary>
    /// Main singleton aplikace
    /// </summary>
    public class App
    {
        #region Singleton
        /// <summary>
        /// Singleton celé aplikace s dostupnými daty a službami
        /// </summary>
        public static App Current
        {
            get
            {
                if (__Current is null)
                {
                    lock (__Lock)
                    {
                        if (__Current is null)
                            _CreateCurrent();
                    }
                }
                return __Current;
            }
        }
        /// <summary>
        /// Vytvoří a vrátí new instanci
        /// </summary>
        private static void _CreateCurrent()
        {
            __Current = new App();
            __Current._Initialize();
        }
        private static App __Current;
        private static object __Lock = new object();
        /// <summary>
        /// Konstruktor: první fáze inicializace, nesmí používat <see cref="Current"/>
        /// </summary>
        private App()
        { }
        /// <summary>
        /// Inicializace: druhá fáze inicializace, v omezené míře smí používat <see cref="Current"/>
        /// </summary>
        private void _Initialize()
        {
            _InitGraphics();
            _InitFonts();
            // Co přidáš sem, přidej i do _Exit() !!!
        }
        #endregion
        #region Řízený start a konec aplikace
        public static void Start(string[] arguments)
        {
            Current._Start(arguments);
        }
        public static void Exit()
        {
            if (__Current != null)
            {
                __Current._Exit();
                __Current = null;
            }
        }
        private void _Start(string[] arguments)
        {
            __Arguments = arguments;
        }
        private void _Exit()
        {
            _DisposeGraphics();
            _DisposeFonts();
        }
        private string[] __Arguments;
        #endregion
        #region Grafické prvky
        /// <summary>
        /// Vrátí připravené fungující pero správně namočené do inkoustu dané barvy, šířka pera 1.
        /// Nesmí se Disposovat, jde o obecně používané půjčovací pero.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen GetPen(Color color)
        {
            var pen = Current.__Pen;
            pen.Color = color;
            pen.Width = 1f;
            return pen;
        }
        /// <summary>
        /// Vrátí připravené fungující pero dané šířky a správně namočené do inkoustu dané barvy.
        /// Nesmí se Disposovat, jde o obecně používané půjčovací pero.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen GetPen(Color color, float width)
        {
            var pen = Current.__Pen;
            pen.Color = color;
            pen.Width = width;
            return pen;
        }
        /// <summary>
        /// Vrátí připravený fungující štětec namočený do plechovky dané barvy.
        /// Nesmí se Disposovat, jde o obecně používaný půjčovací štětec.
        /// </summary>
        /// <param name="colorSet"></param>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        public static Pen GetPen(ColorSet colorSet, InteractiveState interactiveState = InteractiveState.None, float? width = null)
        {
            var pen = Current.__Pen;
            pen.Color = colorSet.GetColor(interactiveState);
            pen.Width = width ?? 1f;
            return pen;
        }
        /// <summary>
        /// Vrátí připravený fungující štětec namočený do plechovky dané barvy.
        /// Nesmí se Disposovat, jde o obecně používaný půjčovací štětec.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Brush GetBrush(Color color)
        {
            var brush = Current.__Brush;
            brush.Color = color;
            return brush;
        }
        /// <summary>
        /// Vrátí připravený fungující štětec namočený do plechovky dané barvy.
        /// Nesmí se Disposovat, jde o obecně používaný půjčovací štětec.
        /// </summary>
        /// <param name="colorSet"></param>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        public static Brush GetBrush(ColorSet colorSet, InteractiveState interactiveState = InteractiveState.None)
        {
            var brush = Current.__Brush;
            brush.Color = colorSet.GetColor(interactiveState);
            return brush;
        }
        /// <summary>
        /// Inicializace grafických prvků
        /// </summary>
        private void _InitGraphics()
        {
            __Pen = new Pen(Color.White, 1f);
            __Brush = new SolidBrush(Color.White);
        }
        /// <summary>
        /// Dispose grafických prvků
        /// </summary>
        private void _DisposeGraphics()
        {
            __Pen?.Dispose();
            __Pen = null;

            __Brush?.Dispose();
            __Brush = null;
        }
        private Pen __Pen;
        private SolidBrush __Brush;
        #endregion
        #region FontLibrary
        /// <summary>
        /// Najde a vrátí Font pro dané požadavky. Vrácený Font se nesmí Dispose, protože je opakovaně používán!
        /// </summary>
        /// <param name="fontType"></param>
        /// <param name="emSize"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        public static Font GetFont(FontType? fontType = null, float? emSize = null, FontStyle? fontStyle = null)
        {
            return Current._GetFont(fontType, emSize, fontStyle);
        }
        /// <summary>
        /// Najde a vrátí Font pro dané požadavky. Vrácený Font se nesmí Dispose, protože je opakovaně používán!
        /// </summary>
        /// <param name="textAppearance"></param>
        /// <returns></returns>
        public static Font GetFont(TextAppearance textAppearance)
        {
            FontType fontType = textAppearance.FontType ?? FontType.DefaultFont;
            float emSize = textAppearance.EmSize ?? GetSystemFont(fontType).Size;
            if (textAppearance.SizeRatio.HasValue) emSize = emSize * textAppearance.SizeRatio.Value;

            return Current._GetFont(fontType, emSize, textAppearance.FontStyle);
        }
        /// <summary>
        /// Najde nebo vytvoří a vrátí požadovaný font
        /// </summary>
        /// <param name="fontType"></param>
        /// <param name="emSize"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        private Font _GetFont(FontType? fontType, float? emSize, FontStyle? fontStyle)
        {
            if (!fontType.HasValue) fontType = FontType.DefaultFont;
            if (!emSize.HasValue) emSize = GetSystemFont(fontType.Value).Size;
            if (!fontStyle.HasValue) fontStyle = GetSystemFont(fontType.Value).Style;

            string key = _GetFontKey(fontType.Value, emSize.Value, fontStyle.Value);
            if (!__Fonts.TryGetValue(key, out var font))
            {
                font = new Font(GetSystemFont(fontType.Value).FontFamily, emSize.Value, fontStyle.Value);
                __Fonts.Add(key, font);
            }
            return font;
        }
        /// <summary>
        /// Vrátí string klíč pro danou definici fontu. Pod klíčem bude font uložen do <see cref="__Fonts"/>.
        /// </summary>
        /// <param name="fontType"></param>
        /// <param name="emSize"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        private static string _GetFontKey(FontType fontType, float emSize, FontStyle fontStyle)
        {
            int size = (int)Math.Round(5f * emSize, 0);
            string style = "S" +
                (fontStyle.HasFlag(FontStyle.Bold) ? "B" : "") +
                (fontStyle.HasFlag(FontStyle.Italic) ? "I" : "") +
                (fontStyle.HasFlag(FontStyle.Underline) ? "U" : "") +
                (fontStyle.HasFlag(FontStyle.Strikeout) ? "S" : "");
            return $"T.{fontType}.{size}.{style}";
        }
        /// <summary>
        /// Vrátí systémový font, např. <see cref="SystemFonts.DefaultFont"/> pro daný typ <paramref name="fontType"/>
        /// </summary>
        /// <param name="fontType"></param>
        /// <returns></returns>
        public static Font GetSystemFont(FontType fontType)
        {
            switch (fontType)
            {
                case FontType.DefaultFont: return SystemFonts.DefaultFont;
                case FontType.DialogFont: return SystemFonts.DialogFont;
                case FontType.MessageBoxFont: return SystemFonts.MessageBoxFont;
                case FontType.CaptionFont: return SystemFonts.CaptionFont;
                case FontType.SmallCaptionFont: return SystemFonts.SmallCaptionFont;
                case FontType.MenuFont: return SystemFonts.MenuFont;
                case FontType.StatusFont: return SystemFonts.StatusFont;
                case FontType.IconTitleFont: return SystemFonts.IconTitleFont;
            }
            return SystemFonts.DefaultFont;
        }
        /// <summary>
        /// Systémové typy fontů
        /// </summary>
        public enum FontType
        {
            DefaultFont,
            DialogFont,
            MessageBoxFont,
            CaptionFont,
            SmallCaptionFont,
            MenuFont,
            StatusFont,
            IconTitleFont
        }
        /// <summary>
        /// Inicializace fontů
        /// </summary>
        private void _InitFonts()
        {
            __Fonts = new Dictionary<string, Font>();
        }
        /// <summary>
        /// Dispose fontů
        /// </summary>
        private void _DisposeFonts()
        {
            __Fonts.Values.ForEachExec(f => f?.Dispose());
            __Fonts = null;
        }
        private Dictionary<string, Font> __Fonts;
        #endregion


        public static void ShowError(string error) { }
    }
}
