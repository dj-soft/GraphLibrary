using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Windows.Forms;
using DjSoft.Tools.ProgramLauncher.Data;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

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
        #region Popup Menu
        /// <summary>
        /// Z dodaných prvků vytvoří kontextové Popup menu, zobrazí jej na dané souřadnici (nebo na aktuální souřadnici myši) a nabídne uživateli.
        /// Až si uživatel vybere (asynchronní), pak předá řízení do dané metody <paramref name="onSelectItem"/> a předá do ní vybranou položku menu.
        /// Volající si pak sám provede odpovídající akci. 
        /// Může k tomu využít prostor <see cref="IMenuItem.UserData"/> v prvku menu, kde si uchová kontext pro tuto akci.
        /// </summary>
        /// <param name="menuItems"></param>
        /// <param name="onSelectItem"></param>
        /// <param name="pointOnScreen"></param>
        public static void SelectFromMenu(IEnumerable<IMenuItem> menuItems, Action<IMenuItem> onSelectItem, Point? pointOnScreen)
        {
            if (!pointOnScreen.HasValue) pointOnScreen = Control.MousePosition;

            ToolStripDropDownMenu menu = new ToolStripDropDownMenu();
            foreach (var menuItem in menuItems)
                menu.Items.Add(_CreateToolStripItem(menuItem, onSelectItem));

            menu.DropShadowEnabled = true;
            menu.RenderMode = ToolStripRenderMode.Professional;
            menu.ShowCheckMargin = false;
            menu.ShowImageMargin = true;
            menu.ItemClicked += _OnMenuItemClicked;
            menu.Show(pointOnScreen.Value);
        }
        /// <summary>
        /// Vygeneruje a vrátí <see cref="ToolStripMenuItem"/> z dané datové položky
        /// </summary>
        /// <param name="menuItem"></param>
        /// <returns></returns>
        private static ToolStripItem _CreateToolStripItem(IMenuItem menuItem, Action<IMenuItem> onSelectItem)
        {
            ToolStripItem item;
            switch (menuItem.ItemType)
            {
                case MenuItemType.Separator:
                    var separatorItem = new ToolStripSeparator();
                    item = separatorItem;
                    break;
                case MenuItemType.Header:
                    var headerItem = new ToolStripLabel(menuItem.Text, menuItem.Image);
                    item = headerItem;
                    break;
                case MenuItemType.Button:
                case MenuItemType.Default:
                default:
                    var buttonItem = new ToolStripMenuItem(menuItem.Text, menuItem.Image) { Tag = new Tuple<IMenuItem, Action<IMenuItem>>(menuItem, onSelectItem) };
                    buttonItem.Enabled = menuItem.Enabled;
                    item = buttonItem;
                    break;
            }
            item.Tag = new Tuple<IMenuItem, Action<IMenuItem>>(menuItem, onSelectItem);
            item.ToolTipText = menuItem.ToolTip;

            var fontStyle = menuItem.FontStyle;
            if (fontStyle.HasValue)
                item.Font = App.GetFont(item.Font, null, fontStyle.Value);

            return item;
        }
        /// <summary>
        /// Provede se po kliknutí na prvek menu. Najde data o prvku i cílovou metodu, a vyvolá ji.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void _OnMenuItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Tag is Tuple<IMenuItem, Action<IMenuItem>> selectedInfo && selectedInfo.Item2 != null)
                selectedInfo.Item2(selectedInfo.Item1);
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
        public static Pen GetPen(Color? color, float? alpha = null)
        {
            if (!color.HasValue) return null;
            var pen = Current.__Pen;
            pen.Color = color.Value.GetAlpha(alpha);
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
        public static Pen GetPen(Color? color, float width, float? alpha = null)
        {
            if (!color.HasValue) return null;
            var pen = Current.__Pen;
            pen.Color = color.Value.GetAlpha(alpha);
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
        public static Pen GetPen(ColorSet colorSet, Components.InteractiveState interactiveState = Components.InteractiveState.Default, float? width = null, float? alpha = null)
        {
            var color = colorSet?.GetColor(interactiveState);
            if (color == null) return null;
            var pen = Current.__Pen;
            pen.Color = color.Value.GetAlpha(alpha);
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
        public static Brush GetBrush(Color? color, float? alpha = null)
        {
            if (!color.HasValue) return null;
            var brush = Current.__Brush;
            brush.Color = color.Value.GetAlpha(alpha);
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
        public static Brush GetBrush(ColorSet colorSet, Components.InteractiveState interactiveState = Components.InteractiveState.Default, float? alpha = null)
        {
            var color = colorSet?.GetColor(interactiveState);
            if (color == null) return null;
            var brush = Current.__Brush;
            brush.Color = color.Value.GetAlpha(alpha);
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
        /// <summary>
        /// Vrátí kurzor daného typu
        /// </summary>
        /// <param name="cursorTypes"></param>
        /// <returns></returns>
        public static Cursor GetCursor(CursorTypes cursorTypes)
        {
            switch (cursorTypes)
            {
                case CursorTypes.Default: return Cursors.Default;
                case CursorTypes.Hand: return Cursors.Hand;
                case CursorTypes.Arrow: return Cursors.Arrow;
                case CursorTypes.Cross: return Cursors.Cross;
                case CursorTypes.IBeam: return Cursors.IBeam;
                case CursorTypes.Help: return Cursors.Help;
                case CursorTypes.AppStarting: return Cursors.AppStarting;
                case CursorTypes.UpArrow: return Cursors.UpArrow;
                case CursorTypes.WaitCursor: return Cursors.WaitCursor;
                case CursorTypes.HSplit: return Cursors.HSplit;
                case CursorTypes.VSplit: return Cursors.VSplit;
                case CursorTypes.NoMove2D: return Cursors.NoMove2D;
                case CursorTypes.NoMoveHoriz: return Cursors.NoMoveHoriz;
                case CursorTypes.NoMoveVert: return Cursors.NoMoveVert;
                case CursorTypes.SizeAll: return Cursors.SizeAll;
                case CursorTypes.SizeNESW: return Cursors.SizeNESW;
                case CursorTypes.SizeNS: return Cursors.SizeNS;
                case CursorTypes.SizeNWSE: return Cursors.SizeNWSE;
                case CursorTypes.SizeWE: return Cursors.SizeWE;
                case CursorTypes.PanEast: return Cursors.PanEast;
                case CursorTypes.PanNE: return Cursors.PanNE;
                case CursorTypes.PanNorth: return Cursors.PanNorth;
                case CursorTypes.PanNW: return Cursors.PanNW;
                case CursorTypes.PanSE: return Cursors.PanSE;
                case CursorTypes.PanSouth: return Cursors.PanSouth;
                case CursorTypes.PanSW: return Cursors.PanSW;
                case CursorTypes.PanWest: return Cursors.PanWest;
                case CursorTypes.No: return Cursors.No;
            }
            return Cursors.Default;
        }
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
        #region Vzhled a Layout
        /// <summary>
        /// Aktuální vzhled (skin = barevná paleta); lze změnit, po změně dojde eventu <see cref="CurrentAppearanceChanged"/>.
        /// Pozor, tato property není propojena s <see cref="Settings.AppearanceName"/>, toto propojení musí zajistit aplikace.
        /// Důvodem je vhodné načasování zaháčkování a provedení eventu po změně / inicializaci.
        /// </summary>
        public static AppearanceInfo CurrentAppearance { get { return Current._CurrentAppearance; } set { Current._CurrentAppearance = value; } }
        /// <summary>
        /// Aktuální vzhled (skin = barevná paleta); řeší autoinicializaci i hlídání změny a vyvolání eventu
        /// </summary>
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
        /// <summary>
        /// Aktuální vzhled (skin = barevná paleta), proměnná
        /// </summary>
        private AppearanceInfo __CurrentAppearance;
        /// <summary>
        /// Událost volaná po změně skinu
        /// </summary>
        public static event EventHandler CurrentAppearanceChanged { add { Current.__CurrentAppearanceChanged += value; } remove { Current.__CurrentAppearanceChanged -= value; } }
        /// <summary>
        /// Událost volaná po změně skinu
        /// </summary>
        private event EventHandler __CurrentAppearanceChanged;


        /// <summary>
        /// Aktuální sada definující layout; lze změnit, po změně dojde eventu <see cref="CurrentLayoutSetChanged"/>.
        /// Pozor, tato property není propojena s <see cref="Settings.LayoutSetName"/>, toto propojení musí zajistit aplikace.
        /// Důvodem je vhodné načasování zaháčkování a provedení eventu po změně / inicializaci.
        /// </summary>
        public static ItemLayoutSet CurrentLayoutSet { get { return Current._CurrentLayoutSet; } set { Current._CurrentLayoutSet = value; } }
        /// <summary>
        /// Aktuální sada definující layout; řeší autoinicializaci i hlídání změny a vyvolání eventu
        /// </summary>
        private ItemLayoutSet _CurrentLayoutSet
        {
            get
            {
                if (__CurrentLayoutSet is null)
                    __CurrentLayoutSet = ItemLayoutSet.Default;
                return __CurrentLayoutSet;
            }
            set
            {
                if (value is null) return;
                bool isChange = (__CurrentLayoutSet is null || !Object.ReferenceEquals(value, __CurrentLayoutSet));
                __CurrentLayoutSet = value;
                if (isChange) __CurrentLayoutSetChanged?.Invoke(null, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Aktuální sada definující layout, proměnná
        /// </summary>
        private ItemLayoutSet __CurrentLayoutSet;
        /// <summary>
        /// Událost volaná po změně skinu
        /// </summary>
        public static event EventHandler CurrentLayoutSetChanged { add { Current.__CurrentLayoutSetChanged += value; } remove { Current.__CurrentLayoutSetChanged -= value; } }
        /// <summary>
        /// Událost volaná po změně skinu
        /// </summary>
        private event EventHandler __CurrentLayoutSetChanged;
        #endregion
        #region MainForm a StatusBar a StatusImage
        /// <summary>
        /// Main okno aplikace. Slouží jako Owner pro Dialog okna a další Child okna.
        /// </summary>
        public static MainForm MainForm { get { return Current.__MainForm; } set { Current.__MainForm = value; } } private MainForm __MainForm;
        /// <summary>
        /// Vrátí image daného typu do PopupMenu nebo do StatusBar
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static Image GetStatusImage(ImageKindType value)
        {
            switch (value)
            {
                case ImageKindType.None: return null;
                case ImageKindType.Other: return null;
                case ImageKindType.Delete: return Properties.Resources.delete_22;
                case ImageKindType.DocumentNew: return Properties.Resources.document_new_3_22;
                case ImageKindType.DocumentPreview: return Properties.Resources.document_preview_22;
                case ImageKindType.DocumentProperties: return Properties.Resources.document_properties_22;
                case ImageKindType.DocumentEdit: return Properties.Resources.edit_3_22;
                case ImageKindType.EditCopy: return Properties.Resources.edit_copy_3_22;
                case ImageKindType.EditCut: return Properties.Resources.edit_cut_3_22;
                case ImageKindType.EditPaste: return Properties.Resources.edit_paste_3_22;
                case ImageKindType.EditRemove: return Properties.Resources.edit_remove_3_22;
                case ImageKindType.EditSelect: return Properties.Resources.edit_select_22;
                case ImageKindType.EditRows: return Properties.Resources.edit_select_all_3_22;
                case ImageKindType.FormatJustify: return Properties.Resources.format_justify_left_4_22;
                case ImageKindType.Home: return Properties.Resources.go_home_4_22;
                case ImageKindType.Help: return Properties.Resources.help_3_22;
                case ImageKindType.Hint: return Properties.Resources.help_hint_22;
                case ImageKindType.MediaPlay: return Properties.Resources.media_playback_start_3_22;
                case ImageKindType.MediaForward: return Properties.Resources.media_seek_forward_3_22;
            }
            return null;
        }
        #endregion
        #region Messages a texty
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
        /// <summary>
        /// Vrátí text odpovídající počtu
        /// </summary>
        /// <param name="count"></param>
        /// <param name="textZero"></param>
        /// <param name="textOne"></param>
        /// <param name="textSmall"></param>
        /// <param name="textMany"></param>
        /// <returns></returns>
        public static string GetCountText(int count, string textZero, string textOne, string textSmall, string textMany)
        {
            if (count <= 0) return textZero;
            if (count == 1) return count.ToString() + textOne;
            if (count <= 4) return count.ToString() + textSmall;
            return count.ToString() + textMany;
        }
        #endregion
    }

    #region Podpůrné třídy (StatusInfo) a enumy
    /// <summary>
    /// Třída obsahující logická data reprezentovaná v jednom prvku Status labelu
    /// </summary>
    public class StatusInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="statusLabel"></param>
        public StatusInfo(ToolStripStatusLabel statusLabel)
        {
            __StatusLabel = statusLabel;
        }
        /// <summary>
        /// Instance controlu <see cref="ToolStripStatusLabel"/>
        /// </summary>
        private ToolStripStatusLabel __StatusLabel;
        /// <summary>
        /// Typ obrázku
        /// </summary>
        private ImageKindType __ImageKind;
        /// <summary>
        /// Text ve Statusbaru
        /// </summary>
        public string Text { get { return __StatusLabel.Text; } set { __StatusLabel.Text = value; } }
        /// <summary>
        /// Typ obrázku ve Statusbaru
        /// </summary>
        public ImageKindType ImageKind { get { return __ImageKind; } set { __ImageKind = value; __StatusLabel.Image = App.GetStatusImage(value); } }
        /// <summary>
        /// Fyzický obrázek ve Statusbaru
        /// </summary>
        public Image Image { get { return __StatusLabel.Image; } set { __ImageKind = (value is null ? ImageKindType.None : ImageKindType.Other); __StatusLabel.Image = value; } }
    }
    /// <summary>
    /// Typ ikony
    /// </summary>
    public enum ImageKindType
    {
        None,
        Other,
        Delete,
        DocumentNew,
        DocumentPreview,
        DocumentProperties,
        DocumentEdit,
        EditCopy,
        EditCut,
        EditPaste,
        EditRemove,
        EditSelect,
        EditRows,
        FormatJustify,
        Home,
        Help,
        Hint,
        MediaPlay,
        MediaForward
    }
    /// <summary>
    /// Typ kurzoru. 
    /// Fyzický kurzor pro konkrétní typ vrátí <see cref="App.GetCursor(CursorTypes)"/>.
    /// </summary>
    public enum CursorTypes
    {
        Default,
        Hand,
        Arrow,
        Cross,
        IBeam,
        Help,
        AppStarting,
        UpArrow,
        WaitCursor,
        HSplit,
        VSplit,
        NoMove2D,
        NoMoveHoriz,
        NoMoveVert,
        SizeAll,
        SizeNESW,
        SizeNS,
        SizeNWSE,
        SizeWE,
        PanEast,
        PanNE,
        PanNorth,
        PanNW,
        PanSE,
        PanSouth,
        PanSW,
        PanWest,
        No
    }
    #endregion
}
