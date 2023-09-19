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
            _InitImages();
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
            _DisposeImages();
        }
        private string[] __Arguments;
        #endregion
        #region Grafické prvky
        /// <summary>
        /// Vrátí připravené fungující pero správně namočené do inkoustu dané barvy, šířka pera 1.
        /// Nesmí se Disposovat, jde o obecně používané půjčovací pero.
        /// <para/>
        /// Pozor, může vrátit null, pokud <paramref name="color"/> je null
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen GetPen(Color? color)
        {
            if (!color.HasValue) return null;
            var pen = Current.__Pen;
            pen.Color = color.Value;
            pen.Width = 1f;
            return pen;
        }
        /// <summary>
        /// Vrátí připravené fungující pero dané šířky a správně namočené do inkoustu dané barvy.
        /// Nesmí se Disposovat, jde o obecně používané půjčovací pero.
        /// <para/>
        /// Pozor, může vrátit null, pokud <paramref name="color"/> je null
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen GetPen(Color? color, float width)
        {
            if (!color.HasValue) return null;
            var pen = Current.__Pen;
            pen.Color = color.Value;
            pen.Width = width;
            return pen;
        }
        /// <summary>
        /// Vrátí připravený fungující štětec namočený do plechovky dané barvy.
        /// Nesmí se Disposovat, jde o obecně používaný půjčovací štětec.
        /// <para/>
        /// Pozor, může vrátit null, pokud <paramref name="colorSet"/> je null nebo vrátí null pro daný stav = pak se nemá nic kreslit.
        /// </summary>
        /// <param name="colorSet"></param>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        public static Pen GetPen(ColorSet colorSet, InteractiveState interactiveState = InteractiveState.Default, float? width = null)
        {
            var color = colorSet?.GetColor(interactiveState);
            if (color == null) return null;
            var pen = Current.__Pen;
            pen.Color = color.Value;
            pen.Width = width ?? 1f;
            return pen;
        }
        /// <summary>
        /// Vrátí připravený fungující štětec namočený do plechovky dané barvy.
        /// Nesmí se Disposovat, jde o obecně používaný půjčovací štětec.
        /// <para/>
        /// Pozor, může vrátit null, pokud <paramref name="color"/> je null
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Brush GetBrush(Color? color)
        {
            if (!color.HasValue) return null;
            var brush = Current.__Brush;
            brush.Color = color.Value;
            return brush;
        }
        /// <summary>
        /// Vrátí připravený fungující štětec namočený do plechovky dané barvy.
        /// Nesmí se Disposovat, jde o obecně používaný půjčovací štětec.
        /// <para/>
        /// Pozor, může vrátit null, pokud <paramref name="colorSet"/> je null nebo vrátí null pro daný stav = pak se nemá nic kreslit.
        /// </summary>
        /// <param name="colorSet"></param>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        public static Brush GetBrush(ColorSet colorSet, InteractiveState interactiveState = InteractiveState.Default)
        {
            var color = colorSet?.GetColor(interactiveState);
            if (color == null) return null;
            var brush = Current.__Brush;
            brush.Color = color.Value;
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
            __Pen.TryDispose();
            __Pen = null;

            __Brush.TryDispose();
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
        /// Pokud je dodán parametr <paramref name="interactiveState"/>, vyhledá i odpovídající variantu stylu.
        /// </summary>
        /// <param name="textAppearance"></param>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        public static Font GetFont(TextAppearance textAppearance, InteractiveState interactiveState = InteractiveState.Default)
        {
            FontType fontType = textAppearance.FontType ?? FontType.DefaultFont;
            float emSize = textAppearance.TextStyles[interactiveState].EmSize ?? textAppearance.TextStyles[InteractiveState.Default].EmSize ?? GetSystemFont(fontType).Size;
            var sizeRatio = textAppearance.TextStyles[interactiveState].SizeRatio ?? textAppearance.TextStyles[InteractiveState.Default].SizeRatio;
            var fontStyle = textAppearance.TextStyles[interactiveState].FontStyle ?? textAppearance.TextStyles[InteractiveState.Default].FontStyle;

            if (sizeRatio.HasValue) emSize = emSize * sizeRatio.Value;

            return Current._GetFont(fontType, emSize, fontStyle);
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
            __Fonts.Values.ForEachExec(f => f.TryDispose());
            __Fonts = null;
        }
        private Dictionary<string, Font> __Fonts;
        #endregion
        #region ImageLibrary
        /// <summary>
        /// Najde a vrátí Image načtený z dodaného souboru.
        /// Image se nesmí měnit ani Disposovat, používá se opakovaně.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Image GetImage(string fileName)
        {
            return Current._GetImage(fileName, null);
        }
        /// <summary>
        /// Najde a vrátí Image načtený z dodaného obsahu.
        /// Image se nesmí měnit ani Disposovat, používá se opakovaně.
        /// <para/>
        /// Dodaný <paramref name="imageName"/> nesmí být prázdný - používá se jako jednoznačný klíč pro Image, pod ním je uložen v interní paměti aplikace!
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static Image GetImage(string imageName, byte[] content)
        {
            return Current._GetImage(imageName, content);
        }
        /// <summary>
        /// Najde / vytvoří a vrátí Image z dané definice.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        private Image _GetImage(string fileName, byte[] content)
        {
            if (String.IsNullOrEmpty(fileName)) return null;
            string type = (content is null ? "File" : "Data");
            string key = _GetImageKey(type, fileName);
            if (!__Images.TryGetValue(key, out Image image))
            {
                try
                {
                    if (content != null)
                    {   // Z obsahu:
                        using (var stream = new System.IO.MemoryStream(content))
                            image = Image.FromStream(stream);
                    }
                    else if (System.IO.File.Exists(fileName))
                    {   // Ze souboru:
                        image = Image.FromFile(fileName);
                    }
                }
                catch (Exception) { image = null; }
                __Images.Add(key, image);
            }
            return image;
        }
        /// <summary>
        /// Vrátí klíč pro Image
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private string _GetImageKey(string type, string name)
        {
            name = name.Trim().ToLower().Replace("\\", "/");
            return $"{type}>{name}";
        }
        /// <summary>
        /// Inicializace Images
        /// </summary>
        private void _InitImages()
        {
            __Images = new Dictionary<string, Image>();
        }
        /// <summary>
        /// Dispose Images
        /// </summary>
        private void _DisposeImages()
        {
            __Images.Values.ForEachExec(f => f.TryDispose());
            __Images = null;
        }
        private Dictionary<string, Image> __Images;
        #endregion
        #region Vzhled
        /// <summary>
        /// Aktuální vzhled (skin = barevná paleta); lze změnit, po změně dojde eventu <see cref="CurrentAppearanceChanged"/>
        /// </summary>
        public static AppearanceSet CurrentAppearance
        {
            get { return Current._CurrentAppearance; }
            set { Current._CurrentAppearance = value; }
        }
        private AppearanceSet _CurrentAppearance
        {
            get 
            {
                if (__CurrentAppearance is null)
                    __CurrentAppearance = AppearanceSet.Default;
                return __CurrentAppearance;
            }
            set 
            {
                if (value is null) return;
                bool isChange = (__CurrentAppearance is null || !Object.ReferenceEquals(value, __CurrentAppearance));
                __CurrentAppearance = value;
                if (isChange) __CurrentAppearanceChanged?.Invoke(null, EventArgs.Empty);
            }
        }
        private AppearanceSet __CurrentAppearance;
        /// <summary>
        /// Událost volaná po změně skinu
        /// </summary>
        public static event EventHandler CurrentAppearanceChanged { add { Current.__CurrentAppearanceChanged += value; } remove { Current.__CurrentAppearanceChanged -= value; } }
        /// <summary>
        /// Událost volaná po změně skinu
        /// </summary>
        private event EventHandler __CurrentAppearanceChanged;
        #endregion


        public static void ShowError(string error) { }
    }
}
