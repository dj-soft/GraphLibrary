// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using WinForm = System.Windows.Forms;
using DevExpress.Utils.Svg;
using DevExpress.Utils.Design;
using Noris.Clients.WinFormServices.Drawing;
using ASOL.Framework.Shared.Diagnostics;
using Noris.Clients.Common;

namespace Noris.Clients.Win.Components.AsolDX
{
    //  ÚČEL KÓDU: reprezentuje adapter mezi subsystémem AsolDX a okolním světem Nephrite.
    //  Komponenty AsolDX v případě potřeby dat/metod z okolního systému volají metody adapteru, shrnuté do statických metod třídy Noris.Clients.Win.Components.AsolDX.SystemAdapter,
    //  která v sobě hostuje instanci current adapteru = zdejší třída CurrentSystemAdapter, a s její pomocí pak se napojuje na zdejší metody a data.

    #region class CurrentSystemAdapter : adapter z AsolDX na svět Nephrite
    /// <summary>
    /// Adapter na systém Nephrite.
    /// Používají jej výhradně kódy z namespace AsolDX, nepoužívá jej ostatní kód z Win.Components.
    /// </summary>
    internal class CurrentSystemAdapter : ISystemAdapter
    {
        event EventHandler ISystemAdapter.InteractiveZoomChanged { add { ComponentConnector.Host.InteractiveZoomChanged += value; } remove { ComponentConnector.Host.InteractiveZoomChanged -= value; } }
        decimal ISystemAdapter.ZoomRatio { get { return ((decimal)Common.SupportScaling.GetScaledValue(100000)) / 100000m; } }
        string ISystemAdapter.GetMessage(MsgCode messageCode, params object[] parameters) { return AdapterSupport.GetMessage(messageCode, parameters); }
        StyleInfo ISystemAdapter.GetStyleInfo(string styleName, Color? exactAttributeColor) { return AdapterSupport.GetStyleInfo(styleName, exactAttributeColor); }
        bool ISystemAdapter.IsPreferredVectorImage { get { return ComponentConnector.GraphicsCache.Options.AllowEditionFormat; } }
        ResourceImageSizeType ISystemAdapter.ImageSizeStandard { get { return AdapterSupport.ImageSizeStandard; } }
        IEnumerable<IResourceItem> ISystemAdapter.GetResources() { return DataResources.GetResources(); }
        string ISystemAdapter.GetResourceItemKey(string name) { return DataResources.GetItemKey(name); }
        string ISystemAdapter.GetResourcePackKey(string name, out ResourceImageSizeType sizeType, out ResourceContentType contentType) { return DataResources.GetPackKey(name, out sizeType, out contentType); }
        byte[] ISystemAdapter.GetResourceContent(IResourceItem resourceItem) { return DataResources.GetResourceContent(resourceItem); }
        bool ISystemAdapter.CanRenderSvgImages { get { return false; } }
        Image ISystemAdapter.RenderSvgImage(SvgImage svgImage, Size size, ISvgPaletteProvider svgPalette) { return null; }
        System.ComponentModel.ISynchronizeInvoke ISystemAdapter.Host { get { return ComponentConnector.Host; } }
        /// <summary>
        /// Adresář na lokálním klientu, kam může aplikace volně zapisovat User data, aniž by byly často smazány. Včetně DLL souborů.
        /// </summary>
        string ISystemAdapter.LocalUserDataPath { get { return AdapterSupport.LocalUserDataPath; } }
        WinForm.Shortcut ISystemAdapter.GetShortcutKeys(string shortCut) { return WinFormServices.KeyboardHelper.GetShortcutFromServerHotKey(shortCut); }
        void ISystemAdapter.TraceText(TraceLevel level, Type type, string method, string keyword, params object[] arguments) { AdapterSupport.TraceText(level, type, method, keyword, arguments); }
        IDisposable ISystemAdapter.TraceTextScope(TraceLevel level, Type type, string method, string keyword, params object[] arguments) { return AdapterSupport.TraceTextScope(level, type, method, keyword, arguments); }
    }
    #endregion
    #region class AdapterSupport
    /// <summary>
    /// Obecný support pro adapter
    /// </summary>
    internal static class AdapterSupport
    {
        /// <summary>
        /// Vyrenderuje dodaný text jako náhradní ikonu
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        /// <returns></returns>
        public static Image CreateCaptionImage(string caption, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            string text = DxComponent.GetCaptionForIcon(caption);                    // Odeberu mezery a nepísmenové znaky
            if (text.Length == 0) return null;

            if (!sizeType.HasValue) sizeType = ResourceImageSizeType.Large;
            bool isDark = DxComponent.IsDarkTheme;
            Color backColor = (isDark ? SvgImageCustomize.DarkColor38 : SvgImageCustomize.LightColorFF);
            Color textColor = (isDark ? SvgImageCustomize.LightColorD4 : SvgImageCustomize.DarkColor38);
            Color lineColor = textColor;

            var realSize = imageSize ?? DxComponent.GetImageSize(sizeType.Value, true);
            Bitmap bitmap = new Bitmap(realSize.Width, realSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                RectangleF bounds = new RectangleF(0, 0, realSize.Width, realSize.Height);
                graphics.FillRectangle(DxComponent.PaintGetSolidBrush(backColor), bounds);

                Rectangle borderBounds = Rectangle.Truncate(bounds);
                borderBounds.Width--;
                borderBounds.Height--;
                graphics.DrawRectangle(DxComponent.PaintGetPen(lineColor), borderBounds);
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                //graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                //graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                var fontMenu = _RibbonFont;
                if (sizeType.Value != ResourceImageSizeType.Large)
                {   // Malé a střední ikony
                    if (text.Length > 2) text = text.Substring(0, 2);                // Radši bych tu viděl jen jedno písmenko, ale JD...
                    var font = fontMenu;
                    var textSize = graphics.MeasureString(text, font);
                    var textBounds = textSize.AlignTo(bounds, ContentAlignment.MiddleCenter);
                    graphics.DrawString(text, font, DxComponent.PaintGetSolidBrush(textColor), textBounds.Location);
                }
                else
                {   // Velké ikony
                    if (text.Length > 2) text = text.Substring(0, 2);
                    using (Font font = new Font(fontMenu.FontFamily, fontMenu.Size * 1.2f))
                    {
                        var textSize = graphics.MeasureString(text, font);
                        var textBounds = textSize.AlignTo(bounds, ContentAlignment.MiddleCenter);
                        graphics.DrawString(text, font, DxComponent.PaintGetSolidBrush(textColor), textBounds.Location);
                    }
                }
            }
            return bitmap;
        }
        /// <summary>
        /// Písmo použité v Ribbonu
        /// </summary>
        private static Font _RibbonFont
        {
            get
            {
                Font font = null;
                var skin = DxComponent.GetSkinInfo(SkinElementColor.RibbonSkins);
                if (skin != null)
                    font = skin[DevExpress.Skins.RibbonSkins.SkinButton]?.GetDefaultFont(DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveLookAndFeel);
                if (font == null)
                    font = SystemFonts.MenuFont;
                return font;
            }
        }
        /// <summary>
        /// Vrací lokalizovaný text požadované hlášky
        /// </summary>
        /// <param name="messageCode"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string GetMessage(MsgCode messageCode, params object[] parameters)
        {
            string code = messageCode.ToString();
            string text = ASOL.Framework.Shared.Localization.Message.GetMessage(code, parameters);
            if (String.Equals(text, code))
                text = _GetDefaultMessage(messageCode, parameters);
            return text;
        }
        #region Záložní lokalizace
        /// <summary>
        /// Vrátí defaultní lokalizovanou hlášku
        /// </summary>
        /// <param name="messageCode"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static string _GetDefaultMessage(MsgCode messageCode, IEnumerable<object> parameters)
        {
            string text = _GetDefaultMessageText(messageCode);
            if (text == null) return null;
            return _GetMessageWithParams(text, parameters);
        }
        /// <summary>
        /// Vrátí text s parametry doplněnými na místo %0
        /// </summary>
        /// <param name="text"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static string _GetMessageWithParams(string text, IEnumerable<object> parameters)
        {
            int i = 0;
            foreach (var param in parameters)
            {
                text = text.Replace($"%{i}", param?.ToString());
                i++;
            }
            return text;
        }
        /// <summary>
        /// Najde defaultní text daného kódu hlášky.
        /// Najde hlášku ve třídě <see cref="MsgCode"/>, vyhledá tam konstantu zadaného názvu, načte atribut dekorující danou konstantu, 
        /// atribut typu <see cref="DefaultMessageTextAttribute"/>, načte jeho hodnotu <see cref="DefaultMessageTextAttribute.DefaultText"/>, a vrátí ji.
        /// </summary>
        /// <param name="messageCode"></param>
        /// <returns></returns>
        private static string _GetDefaultMessageText(MsgCode messageCode)
        {
            if (__Messages == null) __Messages = new Dictionary<MsgCode, string>();

            string text = null;
            if (!__Messages.TryGetValue(messageCode, out text))
            {
                var msgField = typeof(MsgCode).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Where(f => f.Name == messageCode.ToString()).FirstOrDefault();
                if (msgField != null)
                {
                    var defTextAttr = msgField.GetCustomAttributes(typeof(DefaultMessageTextAttribute), true).Cast<DefaultMessageTextAttribute>().FirstOrDefault();
                    if (!(defTextAttr is null || String.IsNullOrEmpty(defTextAttr.DefaultText)))
                        text = defTextAttr.DefaultText;
                }

                // Pro daný kód si text uložím vždy, i když nebyl nalezen (=null) = abych jej příště jak osel nehledal znovu:
                lock (__Messages)
                {
                    if (!__Messages.ContainsKey(messageCode))
                        __Messages.Add(messageCode, text);
                }
            }
            return text;
        }
        /// <summary>
        /// Cache pro již lokalizované defaultní hlášky
        /// </summary>
        private static Dictionary<MsgCode, string> __Messages = null;
        #endregion
        /// <summary>
        /// Standardní velikost ikony
        /// </summary>
        public static ResourceImageSizeType ImageSizeStandard
        {
            get
            {
                var iconSize = Noris.Clients.Common.UserConfig.Design.IconSize;          //     1 = 16x16, 2 = 24x24, 3 = 32x32
                switch (iconSize)
                {
                    case 1: return ResourceImageSizeType.Small;
                    case 2: return ResourceImageSizeType.Medium;
                    case 3: return ResourceImageSizeType.Large;
                }
                return ResourceImageSizeType.None;
            }
        }
        /// <summary>
        /// Konverze velikosti z <see cref="UserGraphicsSize"/> na <see cref="ResourceImageSizeType"/>
        /// </summary>
        /// <param name="imageSize"></param>
        /// <returns></returns>
        internal static ResourceImageSizeType ConvertImageSize(UserGraphicsSize imageSize)
        {
            switch (imageSize)
            {
                case UserGraphicsSize.Small: return ResourceImageSizeType.Small;
                case UserGraphicsSize.Medium: return ResourceImageSizeType.Medium;
                case UserGraphicsSize.Large: return ResourceImageSizeType.Large;
            }
            return ResourceImageSizeType.Medium;
        }
        /// <summary>
        /// Konverze velikosti z <see cref="Noris.WS.DataContracts.DataTypes.DesktopState.IconSizeType"/> na <see cref="ResourceImageSizeType"/>
        /// </summary>
        /// <param name="iconSize"></param>
        /// <param name="defaultSize">defaultní velikost</param>
        /// <returns></returns>
        internal static ResourceImageSizeType ConvertImageSize(Noris.WS.DataContracts.DataTypes.DesktopState.IconSizeType iconSize, ResourceImageSizeType defaultSize)
        {
            switch (iconSize)
            {
                case WS.DataContracts.DataTypes.DesktopState.IconSizeType.None: return ResourceImageSizeType.None;
                case WS.DataContracts.DataTypes.DesktopState.IconSizeType.Default: return defaultSize;
                case WS.DataContracts.DataTypes.DesktopState.IconSizeType.Small: return ResourceImageSizeType.Small;
                case WS.DataContracts.DataTypes.DesktopState.IconSizeType.Medium: return ResourceImageSizeType.Medium;
                case WS.DataContracts.DataTypes.DesktopState.IconSizeType.Large: return ResourceImageSizeType.Large;
                case WS.DataContracts.DataTypes.DesktopState.IconSizeType.ExtraLarge: return ResourceImageSizeType.Large;
            }
            return defaultSize;
        }
        /// <summary>
        /// Konverze pozice z <see cref="Noris.WS.DataContracts.DataTypes.DesktopState.TabHeaderSecondIconPositionType"/> na <see cref="DxTabHeaderImagePainter.ImagePositionType"/>
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        internal static DxTabHeaderImagePainter.ImagePositionType ConvertImagePosition(Noris.WS.DataContracts.DataTypes.DesktopState.TabHeaderSecondIconPositionType position)
        {
            switch (position)
            {
                case WS.DataContracts.DataTypes.DesktopState.TabHeaderSecondIconPositionType.None: return DxTabHeaderImagePainter.ImagePositionType.None;
                case WS.DataContracts.DataTypes.DesktopState.TabHeaderSecondIconPositionType.Default: return DxTabHeaderImagePainter.ImagePositionType.Default;
                case WS.DataContracts.DataTypes.DesktopState.TabHeaderSecondIconPositionType.InsteadCloseButton: return DxTabHeaderImagePainter.ImagePositionType.InsteadCloseButton;
                case WS.DataContracts.DataTypes.DesktopState.TabHeaderSecondIconPositionType.InsteadPinButton: return DxTabHeaderImagePainter.ImagePositionType.InsteadPinButton;
                case WS.DataContracts.DataTypes.DesktopState.TabHeaderSecondIconPositionType.CenterControlArea: return DxTabHeaderImagePainter.ImagePositionType.CenterControlArea;
                case WS.DataContracts.DataTypes.DesktopState.TabHeaderSecondIconPositionType.InsteadStandardIcon: return DxTabHeaderImagePainter.ImagePositionType.InsteadStandardIcon;
                case WS.DataContracts.DataTypes.DesktopState.TabHeaderSecondIconPositionType.AfterStandardIcon: return DxTabHeaderImagePainter.ImagePositionType.AfterStandardIcon;
                case WS.DataContracts.DataTypes.DesktopState.TabHeaderSecondIconPositionType.AfterTextArea: return DxTabHeaderImagePainter.ImagePositionType.AfterTextArea;
            }
            return DxTabHeaderImagePainter.ImagePositionType.Default;
        }
        /// <summary>
        /// Konverze velikosti z <see cref="ResourceImageSizeType"/> na <see cref="UserGraphicsSize"/>
        /// </summary>
        /// <param name="imageSize"></param>
        /// <returns></returns>
        internal static UserGraphicsSize ConvertImageSize(ResourceImageSizeType imageSize)
        {
            switch (imageSize)
            {
                case ResourceImageSizeType.Small: return UserGraphicsSize.Small;
                case ResourceImageSizeType.Medium: return UserGraphicsSize.Medium;
                case ResourceImageSizeType.Large: return UserGraphicsSize.Large;
            }
            return UserGraphicsSize.Medium;
        }

        /// <summary>
        /// Adresář na lokálním klientu, kam může aplikace volně zapisovat User data, aniž by byly často smazány. Včetně DLL souborů.
        /// </summary>
        internal static string LocalUserDataPath
        {
            get
            {
                if (String.IsNullOrEmpty(__LocalUserDataPath))
                    __LocalUserDataPath = _GetLocalUserDataPath();
                return __LocalUserDataPath;
            }
        }
        /// <summary>
        /// Úložiště pro <see cref="LocalUserDataPath"/>
        /// </summary>
        private static string __LocalUserDataPath;
        /// <summary>
        /// Najde a vrátí hodnotu <see cref="LocalUserDataPath"/>
        /// </summary>
        /// <returns></returns>
        private static string _GetLocalUserDataPath()
        {
            // Pokud bych vrátil hodnotu:
            //   Noris.Clients.Controllers.ControllerConnector.Config.CachePath
            // pak bych měl jednu izolovanou cache pro instanci WebView pro každého klienta Nephrite.
            // My ale chceme mít jeden adresář pro tuto cache společný pro všechny klienty Nephrite, takže použiju standardní Windows Application aplikační adresář:

            //   "V:\\InetPub\\wwwroot\\Noris99\\Noris\\ClientImages\\ClientWinForms\\Bin\\Cache\\"
            //     nebo
            //   "C:\\Users\\david.janacek\\AppData\\Roaming\\Asseco Solutions\\HELIOS Green WinKlient\\"
            // var cachePath = Noris.Clients.Controllers.ControllerConnector.Config.CachePath;

            //   "C:\\Users\\david.janacek\\AppData\\Roaming\\Asseco Solutions\\HELIOS Green WinKlient\\50.0.0.0"
            //  a je automaticky vytvořen na disku:
            // var appWorkingPath = System.Windows.Forms.Application.UserAppDataPath;

            //   "C:\\Users\\david.janacek\\AppData\\Roaming"
            // var pathAppData = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            //   "C:\\Users\\david.janacek\\AppData\\Local"
            // var pathLocAppData = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            //   "C:\\Windows\\resources"
            // var pathResData = System.Environment.GetFolderPath(Environment.SpecialFolder.Resources);

            //   "C:\\Users\\david.janacek"
            // var pathUserProfile = System.Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);


            // Takže si cestu složíme sami:
            string appDataRoot = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);     // "C:\\Users\\david.janacek\\AppData\\Roaming"   
            string companyName = System.Windows.Forms.Application.CompanyName;                                    // "Asseco Solutions"
            string prodName = System.Windows.Forms.Application.ProductName;                                       // "HELIOS Green WinKlient"
            string subPath1 = "ASOL";
            string subPath2 = "~WebView.tmp";
            string nephritesPath = System.IO.Path.Combine(appDataRoot, companyName, prodName);                    // "C:\\Users\\david.janacek\\AppData\\Roaming\\Asseco Solutions\\HELIOS Green WinKlient" 
            string localUserDataPath = System.IO.Path.Combine(nephritesPath, subPath1, subPath2);                 // "C:\\Users\\david.janacek\\AppData\\Roaming\\Asseco Solutions\\HELIOS Green WinKlient\\ASOL\\~WebView.tmp" 

            var dirInfo = new System.IO.DirectoryInfo(localUserDataPath);
            bool exists = dirInfo.Exists;
            if (!exists)
            {
                try
                {
                    dirInfo.Create();
                    dirInfo.Refresh();
                    exists = dirInfo.Exists;
                }
                catch { }
            }

            if (!exists)
            {

            }

            return dirInfo.FullName;
        }
        /// <summary>
        /// Zapíše do trace dané informace
        /// </summary>
        /// <param name="level"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="keyword"></param>
        /// <param name="arguments"></param>
        internal static void TraceText(TraceLevel level, Type type, string method, string keyword, params object[] arguments)
        {
            switch (level)
            {
                case TraceLevel.Info:
                    Trace.Info(type, method, keyword, arguments);
                    break;
                case TraceLevel.Warning:
                    Trace.Warning(type, method, keyword, arguments);
                    break;
                case TraceLevel.Error:
                case TraceLevel.SysError:
                    Trace.Error(type, method, keyword, arguments);
                    break;
            }
        }
        /// <summary>
        /// Zapíše do trace dané informace a vrátí using blok, který na svém konci v Dispose zapíše konec bloku (Begin - End)
        /// </summary>
        /// <param name="level"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="keyword"></param>
        /// <param name="arguments"></param>
        internal static IDisposable TraceTextScope(TraceLevel level, Type type, string method, string keyword, params object[] arguments)
        {
            return Trace.InfoScope(type, method, keyword, arguments);
        }
        #region Styly <=> Kalíšky
        /// <summary>
        /// Vrátí definici daného stylu. Pokud je exactAttributeColor not null, tak se styleName ignoruje a generuje se podle exactAttributeColor.
        /// </summary>
        /// <param name="styleName"></param>
        /// <param name="exactAttributeColor"></param>
        /// <returns></returns>
        public static StyleInfo GetStyleInfo(string styleName, Color? exactAttributeColor)
        {
            var styles = _Styles;
            if (styles is null)
            {
                styles = new Dictionary<string, StyleInfo>();
                _Styles = styles;
            }
            bool useExactColor = exactAttributeColor != null;
            if (useExactColor) { styleName = _CreateStyleNameByExactColor(exactAttributeColor); }

            bool isDarkTheme = DxComponent.IsDarkTheme;
            var key = _GetStyleKey(styleName, isDarkTheme);
            if (key == null) return null;
            if (styles.TryGetValue(key, out var styleInfo)) return styleInfo;
            styleInfo = useExactColor ? _CreateStyleByExactValues(styleName, exactAttributeColor) : _CreateStyle(styleName, isDarkTheme);
            lock (styles)
            {   // Vložím i styleinfo = null, abych ho příště nehledal znovu:
                if (!styles.ContainsKey(key))
                    styles.Add(key, styleInfo);
            }
            return styleInfo;
        }
        /// <summary>
        /// Získá data daného stylu a vrátí je
        /// </summary>
        /// <param name="styleName"></param>
        /// <param name="isDarkTheme"></param>
        /// <returns></returns>
        private static StyleInfo _CreateStyle(string styleName, bool isDarkTheme)
        {
            NrsFontColor nrsStyle;
            if (!Noris.Clients.Common.Styles.TryGetStyle(styleName, out nrsStyle)
                && !Noris.Clients.Common.PredefinedStyles.TryGetStyleByName(styleName, out nrsStyle)) return null;  //RMC 0077320 29.01.2025; přidán pokus získa předdefinovaný styl (není v skin.xml

            //attribute
            FontStyle? attributeFontStyle = null;
            string attributeFontFamily = string.Empty;
            float? attributeFontSize = null;
            Color? attributeBgColor = null;
            Color? attributeColor = null;
            //label
            FontStyle? labelFontStyle = null;
            string labelFontFamily = string.Empty;
            float? labelFontSize = null;
            Color? labelBgColor = null;
            Color? labelColor = null;

            if (!nrsStyle.AttrFontFamilyUseFromSkin) attributeFontFamily = nrsStyle.AttrFontFamily;
            if (!nrsStyle.AttrFontSizeUseFromSkin) attributeFontSize = nrsStyle.AttrFontSize;
            if (!nrsStyle.AttrBgColorUseFromSkinByTheme(isDarkTheme)) attributeBgColor = nrsStyle.AttrBgColorByTheme(isDarkTheme);
            if (!nrsStyle.AttrColorUseFromSkinByTheme(isDarkTheme)) attributeColor = nrsStyle.AttrColorByTheme(isDarkTheme);
            attributeFontStyle = DxComponent.ConvertFontStyle(
                    nrsStyle.AttrBoldUseFromSkin == false ? nrsStyle.AttrBold : false,
                    nrsStyle.AttrItalicUseFromSkin == false ? nrsStyle.AttrItalic : false,
                    nrsStyle.AttrUnderlineUseFromSkin == false ? nrsStyle.AttrUnderline : false,
                    false);


            if (!nrsStyle.LabelFontFamilyUseFromSkin) labelFontFamily = nrsStyle.LabelFontFamily;
            if (!nrsStyle.LabelFontSizeUseFromSkin) labelFontSize = nrsStyle.LabelFontSize;
            if (!nrsStyle.LabelBgColorUseFromSkinByTheme(isDarkTheme)) labelBgColor = nrsStyle.LabelBgColorByTheme(isDarkTheme);
            if (!nrsStyle.LabelColorUseFromSkinByTheme(isDarkTheme)) labelColor = nrsStyle.LabelColorByTheme(isDarkTheme);
            labelFontStyle = DxComponent.ConvertFontStyle(
                   nrsStyle.LabelBoldUseFromSkin == false ? nrsStyle.LabelBold : false,
                   nrsStyle.LabelItalicUseFromSkin == false ? nrsStyle.LabelItalic : false,
                   nrsStyle.LabelUnderlineUseFromSkin == false ? nrsStyle.LabelUnderline : false,
                   false);

            StyleInfo styleInfo = new StyleInfo(styleName, isDarkTheme,
                attributeFontStyle, attributeFontFamily, attributeFontSize, attributeBgColor, attributeColor,
                labelFontStyle, labelFontFamily, labelFontSize, labelBgColor, labelColor);

            return styleInfo;
        }

        /// <summary>
        /// Vytvoří styl na základě exactních barev
        /// </summary>
        /// <param name="styleName"></param>
        /// <param name="exactAttributeColor"></param>
        /// <returns></returns>
        private static StyleInfo _CreateStyleByExactValues(string styleName, Color? exactAttributeColor)
        {
            if (string.IsNullOrEmpty(styleName) || exactAttributeColor == null) return null;

            //attribute
            FontStyle? attributeFontStyle = null;
            string attributeFontFamily = string.Empty;
            float? attributeFontSize = null;
            Color? attributeBgColor = null;
            Color? attributeColor = exactAttributeColor;    //Pokud je definovaná exaktní barva, tak se použije ta. Možná se časem rozšíří i o další vlastnosti.
            //label
            FontStyle? labelFontStyle = null;
            string labelFontFamily = string.Empty;
            float? labelFontSize = null;
            Color? labelBgColor = null;
            Color? labelColor = null;

            StyleInfo styleInfo = new StyleInfo(styleName, null,
                attributeFontStyle, attributeFontFamily, attributeFontSize, attributeBgColor, attributeColor,
                labelFontStyle, labelFontFamily, labelFontSize, labelBgColor, labelColor);

            return styleInfo;
        }
        private static Color? _GetStyleColor(bool isDarkTheme) { return null; }
        /// <summary>
        /// Resetuje cache stylů
        /// </summary>
        public static void ResetStyles()
        {
            _Styles = null;
        }
        /// <summary>
        /// Přidá definici stylu, pro testovací účely. V běžném provotu se styly generují OnDemand ze systému.
        /// </summary>
        /// <param name="styleInfo"></param>
        public static void AddStyleInfo(StyleInfo styleInfo)
        {
            var key = _GetStyleKey(styleInfo.Name, styleInfo.IsForDarkTheme);
            if (key == null) return;
            if (_Styles is null) _Styles = new Dictionary<string, StyleInfo>();
            if (_Styles.ContainsKey(key))
                _Styles[key] = styleInfo;
            else
                _Styles.Add(key, styleInfo);
        }
        private static string _GetStyleKey(string styleName, bool? isDark)
        {
            if (styleName is null) return null;
            string sufix;
            if (isDark == null) sufix = "~D&L";
            else sufix = isDark.Value ? "~D" : "~L";
            return styleName + sufix;
        }

        private static string _CreateStyleNameByExactColor(Color? exactAttributeColor)
        {
            string result = "styleByExactColor";
            result += exactAttributeColor == null ? "_null" : "_" + exactAttributeColor.ToString();
            return result;
        }

        private static Dictionary<string, StyleInfo> _Styles;
        #endregion
    }
    #endregion
    #region class DataResources
    /// <summary>
    /// <see cref="DataResources"/> : systém lokálních zdrojů (typicky obrázky), načtené ze souborů z adresářů
    /// </summary>
    internal static class DataResources
    {
        #region Načtení zdrojů
        /// <summary>
        /// Volá se jedenkrát, vrátí kompletní seznam všech zdrojů (Resource).
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IResourceItem> GetResources()
        {
            if (Noris.Clients.Common.ServerResources.State != Common.ServerResources.ResourceState.Ready) return null;                  // Zdroje (základní) ještě nejsou načteny, nebudeme je řešit. Naše knihovna zdrojů se s tím vypořádá.

            List<IResourceItem> resourceList = new List<IResourceItem>();
            AddResourcesFromResourcesBin(resourceList);
            return resourceList;
        }
        #endregion
        #region Načtení Resource.bin
        /// <summary>
        /// Do daného seznamu načte jednotlivé zdroje ze souboru ServerResources.bin
        /// </summary>
        /// <returns></returns>
        private static void AddResourcesFromResourcesBin(List<IResourceItem> resourceList)
        {
            string resourceFile = Noris.Clients.Common.ServerResources.ResourceFileName;
            string resourcePath = DxComponent.ApplicationPath;
            LoadFromResourcesBinFile(resourceFile, resourceList);                                  // Ze souboru, který je použit i jinde

            if (resourceList.Count == 0)
                LoadFromResourcesBinInDirectory(resourcePath, 0, "", resourceList);                // Tady je umístěn soubor "ServerResources.bin" v uživatelském běhu
            if (resourceList.Count == 0)
                LoadFromResourcesBinInDirectory(resourcePath, 3, "Download", resourceList);        // Tady je umístěn v běhu na vývojovém serveru = adresář aplikačního serveru
        }
        /// <summary>
        /// Načte obsah souboru "ServerResources.bin" z určeného adresáře (počet úrovní nahoru nahoru od aktuálního, a daný subdir)
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="upDirs"></param>
        /// <param name="subDir"></param>
        /// <param name="resourceList"></param>
        private static void LoadFromResourcesBinInDirectory(string resourcePath, int upDirs, string subDir, List<IResourceItem> resourceList)
        {
            string path = resourcePath;
            for (int i = 0; i < upDirs && !String.IsNullOrEmpty(path); i++)
                path = System.IO.Path.GetDirectoryName(path);
            if (String.IsNullOrEmpty(path)) return;
            if (!String.IsNullOrEmpty(subDir)) path = System.IO.Path.Combine(path, subDir);
            string fileName = System.IO.Path.Combine(path, "ServerResources.bin");
            LoadFromResourcesBinFile(fileName, resourceList);
        }
        /// <summary>
        /// Načte obsah z daného souboru "ServerResources.bin"
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="resourceList"></param>
        private static void LoadFromResourcesBinFile(string fileName, List<IResourceItem> resourceList)
        {
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(fileName);
            if (!fileInfo.Exists) return;
            int fileLengthMB = (int)(fileInfo.Length / 1000000L);
            if (fileLengthMB > 150) return;

            try
            {
                // Jednoduché načtení někdy skončí chybou Null object reference, asi konflikt o soubor:
                //   byte[] content = System.IO.File.ReadAllBytes(fileInfo.FullName);
                // Načteme soubor trochu jinak:
                byte[] content = _ReadContentOfFile(fileInfo.FullName);
                LoadFromResourcesBinInArray(content, resourceList);
            }
            catch (Exception exc)
            {
                string text = $"Error on loading resources from file {fileInfo.FullName}.\r\n{exc.Message}\r\n{exc.StackTrace}";
                WinForm.MessageBox.Show(text, "ERROR", WinForm.MessageBoxButtons.OK, WinForm.MessageBoxIcon.Warning);
            }
        }
        /// <summary>
        /// Načte obsah dodaného souboru. Snaží se řešit problémy.
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        private static byte[] _ReadContentOfFile(string fullName)
        {
            byte[] content = null;
            int lastAttempt = 3;
            for (int t = 1; t <= lastAttempt; t++)
            {   // Dáme tři pokusy:
                // Pokud soubor zmizí, hlásíme chybu ihned (nečekám, že se v dalším pokusu o čtení soubor objeví):
                if (!System.IO.File.Exists(fullName))
                    throw new System.IO.FileNotFoundException($"File '{fullName}' not found, the file has disappeared! Attempt: {t}.");

                try
                {   // Zkusíme otevřít soubor a jeho obsah zkopírovat do MemoryStream, a ten pak uložíme do byte[] content:
                    // Proč takhle? Protože tady mám možnost specifikovat FileAccess a FileShare:
                    using (var fs = System.IO.File.Open(fullName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                    using (var ms = new System.IO.MemoryStream())
                    {
                        fs.CopyTo(ms);
                        content = ms.ToArray();
                    }
                    return content;
                }
                catch (Exception exc)
                {   // Chyby: při posledním pokusu danou chybu pošlu nahoru,
                    // při prvních pokusech ji utopím a počkám,
                    // třeba se nám soubor zpřístupní...
                    content = null;
                    if (t == lastAttempt)
                        throw;
                    System.Threading.Thread.Sleep(1000);
                }
            }
            return null;                  // Nedostupný kód, ale překladač o tom neví :-)
        }
        /// <summary>
        /// Načte zdroje z dodaného binárního pole ve formátu "ServerResources.bin".
        /// Je zajištěno, že velikost souboru je rozumná (do 150MB)
        /// </summary>
        /// <param name="content"></param>
        /// <param name="resourceList"></param>
        private static void LoadFromResourcesBinInArray(byte[] content, List<IResourceItem> resourceList)
        {
            StringBuilder sb = new StringBuilder();

            string validSignature2 = "ASOL$ApplicationServerResources$2.0";
            int signature2Length = validSignature2.Length;
            int signatureLength = signature2Length;
            string signature = ReadContentString(content, 0, signatureLength, sb);
            bool isValidSignature = (signature.Substring(0, signature2Length) == validSignature2);
            if (!isValidSignature)
                throw new FormatException($"Bad signature of ServerResource.bin file: '{signature}'");

            var length = content.Length;
            int headerEnd = length - 8;
            var headerBegin = ReadContentInt(content, headerEnd);
            var position = headerBegin;
            while (position < headerEnd)
            {
                // Načteme data z headeru:
                long fileBegin = ReadContentLong(content, ref position);
                int fileLength = ReadContentInt(content, ref position);
                int fileNameLength = ReadContentInt(content, ref position);
                string fileName = ReadContentString(content, ref position, fileNameLength, sb);
                if (fileLength >= 0 && fileBegin >= signatureLength && (fileBegin + fileLength) <= headerBegin)
                {
                    string itemKey = GetItemKey(fileName);
                    string packKey = GetPackKey(fileName, out var sizeType, out var contentType);
                    resourceList.Add(new DataResourceItem(content, fileBegin, fileLength, itemKey, packKey, sizeType, contentType));
                }
                else
                {
                    throw new FormatException($"Bad format of ServerResource.bin file: FileBegin ({fileBegin}) or FileLength ({fileLength}) is outside data area ({signatureLength}-{headerBegin}).");
                }
            }
        }
        /// <summary>
        /// Načte Int32 z dané pozice bufferu
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private static int ReadContentInt(byte[] content, int position)
        {
            return ReadContentInt(content, ref position);
        }
        /// <summary>
        /// Načte Int32 z dané pozice bufferu, pozici posune
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private static int ReadContentInt(byte[] content, ref int position)
        {
            return content[position++] | (content[position++] << 8) | (content[position++] << 16) | (content[position++] << 24);
        }
        /// <summary>
        /// Načte Int64 z dané pozice bufferu
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private static long ReadContentLong(byte[] content, int position)
        {
            return ReadContentLong(content, ref position);
        }
        /// <summary>
        /// Načte Int64 z dané pozice bufferu, pozici posune
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private static long ReadContentLong(byte[] content, ref int position)
        {
            long l = ReadContentInt(content, ref position);
            long h = ReadContentInt(content, ref position);
            return l | (h << 32);
        }
        /// <summary>
        /// Načte String z dané pozice bufferu, načítá pouze ASCII znaky (0-255)
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <param name="length"></param>
        /// <param name="sb"></param>
        /// <returns></returns>
        private static string ReadContentString(byte[] content, int position, int length, StringBuilder sb = null)
        {
            return ReadContentString(content, ref position, length, sb ?? new StringBuilder());
        }
        /// <summary>
        /// Načte String z dané pozice bufferu, načítá pouze ASCII znaky (0-255), pozici posune
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <param name="length"></param>
        /// <param name="sb"></param>
        /// <returns></returns>
        private static string ReadContentString(byte[] content, ref int position, int length, StringBuilder sb)
        {
            sb.Clear();
            for (int i = 0; i < length; i++)
                sb.Append((char)(content[position++]));
            return sb.ToString();
        }
        #endregion
        #region Konverze, detekce velikosti a typu
        /// <summary>
        /// Vrátí korektně formátovaný klíč resource (provede Trim, ToLower, a náhradu zpětných lomítek a odstranění úvodních lomítek
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetItemKey(string name)
        {
            string key = (name ?? "").Trim().ToLower().Replace("\\", "/");
            while (key.Length > 0 && key[0] == '/') key = key.Substring(1).Trim();
            return key;
        }
        /// <summary>
        /// Vrátí obecné jméno zdroje z dodaného plného jména zdroje (oddělí velikost a typ souboru podle suffixu a přípony).
        /// Pro vstupní text např. "Noris/pic/AddressDelete-32x32.png" vrátí "Noris/pic/AddressDelete"
        /// a nastaví <paramref name="sizeType"/> = <see cref="ResourceImageSizeType.Large"/>;
        /// a <paramref name="contentType"/> = <see cref="ResourceContentType.Bitmap"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sizeType"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static string GetPackKey(string name, out ResourceImageSizeType sizeType, out ResourceContentType contentType)
        {
            string packKey = GetItemKey(name);
            sizeType = ResourceImageSizeType.None;
            contentType = ResourceContentType.None;
            if (!String.IsNullOrEmpty(packKey))
                if (RemoveContentTypeByExtension(ref packKey, out contentType) && ContentTypeSupportSize(contentType))
                    RemoveSizeTypeBySuffix(ref packKey, out sizeType);
            return packKey;
        }
        /// <summary>
        /// Vrací true, pokud dodaný typ obsahu podporuje uvádění velikosti v názvu zdroje (souboru). Typicky jde o obrázky.
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private static bool ContentTypeSupportSize(ResourceContentType contentType)
        {
            switch (contentType)
            {
                case ResourceContentType.Bitmap:
                case ResourceContentType.Vector:
                case ResourceContentType.Icon:
                case ResourceContentType.Cursor:
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Z dodaného jména souboru určí suffix, a podle něj detekuje velikost obrázku (dá do out parametru) a detekovaný suffix odřízne (celý).
        /// Vrátí true, pokud nějakou velikost detekoval a odřízl (tedy <paramref name="sizeType"/> je jiný než None). 
        /// Vrátí false, když je vstup prázdný, nebo bez suffixu nebo s neznámým suffixem, pak suffix neodřízne.
        /// <para/>
        /// Například pro vstup: "C:/Images/Button-24x24" detekuje <paramref name="sizeType"/> = <see cref="ResourceImageSizeType.Medium"/>, 
        /// a v ref parametru <paramref name="name"/> ponechá: "C:/Images/Button".
        /// <para/>
        /// Tato metoda se typicky volá až po metodě <see cref="RemoveContentTypeByExtension(ref string, out ResourceContentType)"/>, protože tam se řeší a odřízne přípona, a následně se zde řeší suffix jména souboru.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private static bool RemoveSizeTypeBySuffix(ref string name, out ResourceImageSizeType sizeType)
        {
            sizeType = ResourceImageSizeType.None;
            if (String.IsNullOrEmpty(name)) return false;
            name = name.TrimEnd();
            int index = name.LastIndexOf("-");
            if (index <= 0) return false;
            string suffix = name.Substring(index).ToLower();
            switch (suffix)
            {
                case "-16x16":
                case "-small":
                    sizeType = ResourceImageSizeType.Small;
                    break;
                case "-24x24":
                    sizeType = ResourceImageSizeType.Medium;
                    break;
                case "-32x32":
                case "-large":
                    sizeType = ResourceImageSizeType.Large;
                    break;
            }
            if (sizeType != ResourceImageSizeType.None)
                name = name.Substring(0, name.Length - suffix.Length);
            return (sizeType != ResourceImageSizeType.None);
        }
        /// <summary>
        /// Z dodaného jména souboru určí příponu, podle ní detekuje typ obsahu (dá do out parametru) a detekovanou příponu odřízne (včetně tečky).
        /// Vrátí true, pokud nějakou příponu detekoval a odřízl (tedy <paramref name="contentType"/> je jiný než None). 
        /// Vrátí false, když je vstup prázdný, nebo bez přípony nebo s neznámou příponou, pak příponu neodřízne.
        /// <para/>
        /// Například pro vstup: "C:/Images/Button-24x24.png" detekuje <paramref name="contentType"/> = <see cref="ResourceContentType.Bitmap"/>, 
        /// a v ref parametru <paramref name="name"/> ponechá: "C:/Images/Button-24x24".
        /// <para/>
        /// Tato metoda se typicky volá před metodou <see cref="RemoveSizeTypeBySuffix(ref string, out ResourceImageSizeType)"/>, protože tady se řeší a odřízne přípona, a následně se tam řeší suffix jména souboru.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="contentType"></param>
        private static bool RemoveContentTypeByExtension(ref string name, out ResourceContentType contentType)
        {
            contentType = ResourceContentType.None;
            if (String.IsNullOrEmpty(name)) return false;
            name = name.TrimEnd();
            if (name.StartsWith("@")) return false;               // POZOR = tady může name obsahovat nevalidní znaky ( "@text|A|#000066|sans-serif|B|2|#222288|#CCCCFF" ) => neměl bych zhavarovat
            string extension = System.IO.Path.GetExtension(name).ToLower();
            contentType = DxComponent.GetContentTypeFromExtension(extension);
            if (contentType != ResourceContentType.None)
                name = name.Substring(0, name.Length - extension.Length);
            return (contentType != ResourceContentType.None);
        }
        /// <summary>
        /// Vrátí obsah daného zdroje.
        /// </summary>
        /// <param name="resourceItem"></param>
        public static byte[] GetResourceContent(IResourceItem resourceItem)
        {
            return ((resourceItem is DataResourceItem dataItem) ? dataItem.Content : null);
        }
        #endregion
    }
    #endregion
    #region class DataResourceItem
    /// <summary>
    /// Třída obsahující reálně jeden prvek resource - klíče, jméno souboru, OnDemand načtený obsah
    /// </summary>
    internal class DataResourceItem : IResourceItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="itemKey"></param>
        /// <param name="packKey"></param>
        /// <param name="sizeType"></param>
        /// <param name="contentType"></param>
        internal DataResourceItem(string fileName, string itemKey, string packKey, ResourceImageSizeType sizeType, ResourceContentType contentType)
        {
            FileName = fileName;
            ItemKey = itemKey;
            PackKey = packKey;
            ContentType = contentType;
            SizeType = sizeType;
            _Content = null;
            _ContentLoaded = false;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="content"></param>
        /// <param name="fileBegin"></param>
        /// <param name="fileLength"></param>
        /// <param name="itemKey"></param>
        /// <param name="packKey"></param>
        /// <param name="sizeType"></param>
        /// <param name="contentType"></param>
        public DataResourceItem(byte[] content, long fileBegin, int fileLength, string itemKey, string packKey, ResourceImageSizeType sizeType, ResourceContentType contentType)
        {
            FileName = null;
            ItemKey = itemKey;
            PackKey = packKey;
            SizeType = sizeType;
            ContentType = contentType;
            _Content = new byte[fileLength];
            if (fileLength > 0)
                Array.Copy(content, fileBegin, _Content, 0, fileLength);
            _ContentLoaded = true;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ItemKey: {ItemKey}; Size: {SizeType}; Content: {ContentType}";
        }
        /// <summary>
        /// Plné jméno souboru
        /// </summary>
        public string FileName { get; private set; }
        /// <summary>
        /// Klíč prvku (bez root adresáře, včetně velikosti a včetně přípony)
        /// </summary>
        public string ItemKey { get; private set; }
        /// <summary>
        /// Klíč skupiny (bez root adresáře, bez velikosti a bez přípony)
        /// </summary>
        public string PackKey { get; private set; }
        /// <summary>
        /// Typ obsahu
        /// </summary>
        public ResourceContentType ContentType { get; private set; }
        /// <summary>
        /// Typ velikosti
        /// </summary>
        public ResourceImageSizeType SizeType { get; private set; }
        /// <summary>
        /// Obsah zdroje = načtený z odpovídajícího souboru
        /// </summary>
        public byte[] Content { get { return _GetContent(); } }
        /// <summary>
        /// Vrátí obsah zdroje, buď dříve již načtený, nebo jej načte nyní. 
        /// </summary>
        /// <returns></returns>
        private byte[] _GetContent()
        {
            if (!_ContentLoaded)
            {
                _ContentLoaded = true;
                if (!String.IsNullOrEmpty(this.FileName) && System.IO.File.Exists(this.FileName))
                {
                    try { _Content = System.IO.File.ReadAllBytes(this.FileName); }
                    catch { }
                }
            }
            return _Content;
        }
        private byte[] _Content;
        private bool _ContentLoaded;
    }
    #endregion
}
