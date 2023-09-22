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
        #region Řízený start a konec aplikace, Argumenty
        /// <summary>
        /// Zahájení běhu aplikace, předání parametrů
        /// </summary>
        /// <param name="arguments"></param>
        public static void Start(string[] arguments)
        {
            Current._Start(arguments);
        }
        /// <summary>
        /// Ukončení běhu aplikace
        /// </summary>
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
            __Arguments = arguments ?? new string[0];
        }
        private void _Exit()
        {
            _DisposeSettings();
            _DisposeGraphics();
            _DisposeFonts();
            _DisposeImages();
            __MainForm = null;
        }
        /// <summary>
        /// Argumenty předané při startu.
        /// Operační systém je rozdělil v místě mezery.
        /// Pokud byl přítomen parametr s uvozovkami, pak jsou odstraněny a zde je obsah bez uvozovek.
        /// <para/>
        /// Pokud byla aplikace spuštěna s příkazovým řádkem: 'Aplikace.exe reset maximized config="c:\data.cfg" ', 
        /// pak zde jsou tři argumenty jako tři stringy: { reset , maximized , config=c:\data.cfg }         (kde string  config=c:\data.cfg  je třetí argument)
        /// <para/>
        /// Pokud byla aplikace spuštěna s příkazovým řádkem: 'Aplikace.exe reset maximized config = "c:\data aplikací.cfg" '    (mezery okolo rovnítka),
        /// pak zde je pět argumentů: { reset , maximized , config , = , c:\data aplikací.cfg }           (kde čárka odděluje jednotlivé stringy, a celý string  c:\data aplikací.cfg  je pátý argument)
        /// </summary>
        public static string[] Arguments { get { return Current.__Arguments.ToArray(); } } private string[] __Arguments;
        /// <summary>
        /// Vrátí true, pokud argumenty předané při startu aplikace obsahují daný text
        /// </summary>
        /// <param name="text"></param>
        /// <param name="caseSensitive"></param>
        /// <returns></returns>
        public static bool HasArgument(string text, bool caseSensitive = false)
        {
            if (String.IsNullOrEmpty(text)) return false;
            var arguments = Arguments;
            if (arguments.Length == 0) return false;
            StringComparison comparison = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return arguments.Any(a => String.Equals(a, text, comparison));
        }
        /// <summary>
        /// Zkusí najít argument, který začíná daným textem.
        /// </summary>
        /// <param name="textBegin"></param>
        /// <param name="foundArgument"></param>
        /// <param name="caseSensitive"></param>
        /// <returns></returns>
        public static bool TryGetArgument(string textBegin, out string foundArgument, bool caseSensitive = false)
        {
            foundArgument = null;
            if (String.IsNullOrEmpty(textBegin)) return false;
            var arguments = Arguments;
            if (arguments.Length == 0) return false;
            StringComparison comparison = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return arguments.TryFindFirst(a => a.StartsWith(textBegin, comparison), out foundArgument);
        }
        #endregion
        #region Settings
        #region Přístup na Settings
        /// <summary>
        /// Konfigurace aplikace
        /// </summary>
        public static Settings Settings { get { return Current._Settings; } }
        /// <summary>
        /// Instance Konfigurace aplikace, OnDemand
        /// </summary>
        private Settings _Settings
        {
            get
            {
                if (__Settings is null)
                    __Settings = Settings.Create();
                return __Settings;
            }
        }
        /// <summary>
        /// Proměnná pro Konfigurace aplikace
        /// </summary>
        private Settings __Settings;
        /// <summary>
        /// Ukončení Konfigurace aplikace = její Save
        /// </summary>
        private void _DisposeSettings()
        {
            __Settings?.SaveNow();
            __Settings = null;
        }
        internal const string Company = "DjSoft";
        internal const string ProductName = "ProgramLauncher";
        internal static string ProductTitle { get { return "Nabídka aplikací"; } }
        #endregion
        #region Využívání Settings pro práci s jeho daty pomocí App
        #endregion
        #endregion
        #region Monitory, zarovnání souřadnic do monitoru, ukládání souřadnic oken
        #endregion
        #region Menu

        public static bool TrySelectFromMenu(IEnumerable<IMenuItem> menuItems, out IMenuItem selectedItem, Point? pointOnScreen)
        {

            ToolStripDropDownMenu menu = new ToolStripDropDownMenu();

            string currentName = App.CurrentAppearance.Name;
            foreach (var appearance in AppearanceInfo.Collection)
            {
                bool isCurrent = (appearance.Name == currentName);
                Image image = appearance.ImageSmall;
                var item = new ToolStripMenuItem(appearance.Name, image) { Tag = appearance };
                if (isCurrent)
                    item.Font = App.GetFont(item.Font, null, FontStyle.Bold);
                menu.Items.Add(item);
            }
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Další vzhled");

            menu.DropShadowEnabled = true;
            menu.RenderMode = ToolStripRenderMode.Professional;
            menu.ShowCheckMargin = false;
            menu.ShowImageMargin = true;
            menu.ItemClicked += menuItemClicked;
            menu.Show(pointOnScreen.Value);


            void menuItemClicked(object sender, ToolStripItemClickedEventArgs e)
            { }


        }
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
        public static Pen GetPen(ColorSet colorSet, Components.InteractiveState interactiveState = Components.InteractiveState.Default, float? width = null)
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
        public static Brush GetBrush(ColorSet colorSet, Components.InteractiveState interactiveState = Components.InteractiveState.Default)
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
        public static Font GetFont(TextAppearance textAppearance, Components.InteractiveState interactiveState = Components.InteractiveState.Default)
        {
            FontType fontType = textAppearance.FontType ?? FontType.DefaultFont;
            float emSize = textAppearance.TextStyles[interactiveState].EmSize ?? textAppearance.TextStyles[Components.InteractiveState.Default].EmSize ?? GetSystemFont(fontType).Size;
            var sizeRatio = textAppearance.TextStyles[interactiveState].SizeRatio ?? textAppearance.TextStyles[Components.InteractiveState.Default].SizeRatio;
            var fontStyle = textAppearance.TextStyles[interactiveState].FontStyle ?? textAppearance.TextStyles[Components.InteractiveState.Default].FontStyle;

            if (sizeRatio.HasValue) emSize = emSize * sizeRatio.Value;

            return Current._GetFont(fontType, emSize, fontStyle);
        }
        /// <summary>
        /// Najde a vrátí Font pro dané požadavky. Vrácený Font se nesmí Dispose, protože je opakovaně používán!
        /// </summary>
        /// <param name="original">Originální font</param>
        /// <param name="sizeRatio">Poměrná změna velikosti</param>
        /// <param name="fontStyle">Explicitní styl fontu, null = bezez změny</param>
        /// <returns></returns>
        public static Font GetFont(Font original, float? sizeRatio, FontStyle? fontStyle = null)
        {
            string familyName = original.FontFamily.Name;
            float emSize = original.Size;
            if (sizeRatio.HasValue) emSize = emSize * sizeRatio.Value;
            if (!fontStyle.HasValue) fontStyle = original.Style;

            return Current._GetFont(familyName, emSize, fontStyle.Value);
        }
        /// <summary>
        /// Najde a vrátí Font pro dané požadavky. Vrácený Font se nesmí Dispose, protože je opakovaně používán!
        /// </summary>
        /// <param name="familyName"></param>
        /// <param name="emSize"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        public Font _GetFont(string familyName, float emSize, FontStyle fontStyle)
        {
            string key = _GetFontKey(familyName, emSize, fontStyle);
            if (!__Fonts.TryGetValue(key, out var font))
            {
                font = new Font(familyName, emSize, fontStyle);
                __Fonts.Add(key, font);
            }
            return font;
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
        /// Vrátí string klíč pro danou definici fontu. Pod klíčem bude font uložen do <see cref="__Fonts"/>.
        /// </summary>
        /// <param name="familyName"></param>
        /// <param name="emSize"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        private static string _GetFontKey(string familyName, float emSize, FontStyle fontStyle)
        {
            int size = (int)Math.Round(5f * emSize, 0);
            string style = "S" +
                (fontStyle.HasFlag(FontStyle.Bold) ? "B" : "") +
                (fontStyle.HasFlag(FontStyle.Italic) ? "I" : "") +
                (fontStyle.HasFlag(FontStyle.Underline) ? "U" : "") +
                (fontStyle.HasFlag(FontStyle.Strikeout) ? "S" : "");
            return $"N.{familyName}.{size}.{style}";
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
        /// Aktuální vzhled (skin = barevná paleta); lze změnit, po změně dojde eventu <see cref="CurrentAppearanceChanged"/>.
        /// Pozor, tato property není propojena s <see cref="Settings.AppearanceName"/>, toto propojení musí zajistit aplikace.
        /// Důvodem je vhodné načasování zaháčkování a provedení eventu po změně / inicializaci.
        /// </summary>
        public static AppearanceInfo CurrentAppearance
        {
            get { return Current._CurrentAppearance; }
            set { Current._CurrentAppearance = value; }
        }
        private AppearanceInfo _CurrentAppearance
        {
            get 
            {
                if (__CurrentAppearance is null)
                    __CurrentAppearance = AppearanceInfo.Default;
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
        private AppearanceInfo __CurrentAppearance;
        /// <summary>
        /// Událost volaná po změně skinu
        /// </summary>
        public static event EventHandler CurrentAppearanceChanged { add { Current.__CurrentAppearanceChanged += value; } remove { Current.__CurrentAppearanceChanged -= value; } }
        /// <summary>
        /// Událost volaná po změně skinu
        /// </summary>
        private event EventHandler __CurrentAppearanceChanged;
        #endregion
        #region MainForm a Messages
        /// <summary>
        /// Main okno aplikace. Slouží jako Owner pro Dialog okna a další Child okna.
        /// </summary>
        public static Form MainForm { get { return Current.__MainForm; } set { Current.__MainForm = value; } } private Form __MainForm;
        /// <summary>
        /// Zobrazí standardní Message. Zadáním ikony lze definovat titulek okna.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="icon"></param>
        /// <param name="title"></param>
        public static void ShowMessage(string text, MessageBoxIcon icon = MessageBoxIcon.Information, string title = null)
        {
            if (title is null) title = _GetMessageBoxTitle(icon);
            System.Windows.Forms.MessageBox.Show(MainForm, text, title, MessageBoxButtons.OK, icon);
        }
        /// <summary>
        /// Vrátí defaultní titulek MessageBox okna podle dané ikony.
        /// Pro ikonu typu <see cref="MessageBoxIcon.Question"/> přihlédne k parametru <paramref name="isQuestion"/>.
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="isQuestion"></param>
        /// <returns></returns>
        private static string _GetMessageBoxTitle(MessageBoxIcon icon, bool isQuestion = false)
        {
            switch (icon)
            {
                case MessageBoxIcon.None: return "Poznámka";
                case MessageBoxIcon.Stop: return "Chyba";
                case MessageBoxIcon.Question: return (isQuestion ? "Dotaz" : "Podivné");
                case MessageBoxIcon.Exclamation: return "Varování";
                case MessageBoxIcon.Asterisk: return "Informace";
            }
            return "Zpráva";
        }
        #endregion
    }
}
