// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using System.Windows.Forms;
using System.Drawing;

using DevExpress.Utils;
using System.Drawing.Drawing2D;
using DevExpress.Pdf.Native;
using DevExpress.XtraPdfViewer;
using DevExpress.XtraEditors;
using DevExpress.XtraRichEdit.Layout;
using WSXmlSerializer = Noris.WS.Parser.XmlSerializer;
using System.Diagnostics;
using DevExpress.Utils.Svg;
using DevExpress.Utils.Design;

// using BAR = DevExpress.XtraBars;
// using EDI = DevExpress.XtraEditors;
// using TAB = DevExpress.XtraTab;
// using GRD = DevExpress.XtraGrid;
// using CHT = DevExpress.XtraCharts;
// using RIB = DevExpress.XtraBars.Ribbon;
// using NOD = DevExpress.XtraTreeList.Nodes;

namespace Noris.Clients.Win.Components.AsolDX
{
    #region class DxComponent : centrální knihovna pro podporu komponent DevExpress a AsolDx
    /// <summary>
    /// <see cref="DxComponent"/> : centrální knihovna pro podporu komponent DevExpress a AsolDx
    /// </summary>
    public sealed partial class DxComponent
    {
        #region Singleton
        /// <summary>
        /// Soukromý přístup k singletonu
        /// </summary>
        private static DxComponent Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_InstanceLock)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new DxComponent();
                        }
                    }
                }
                return _Instance;
            }
        }
        private DxComponent()
        {
            this._InitCore();
            this._InitLog();
            this._InitStyles();
            this._InitZoom();
            this._InitFontCache();
            this._InitDrawing();
            this._InitListeners();
            this._InitSvgConvertor();
            this._InitClipboard();
            this._InitAppEvents();
        }
        private static bool __IsInitialized = false;
        private static DxComponent _Instance;
        private static object _InstanceLock = new object();
        #endregion
        #region InitCore, Init a Done
        /// <summary>
        /// Inicializace základní
        /// </summary>
        private void _InitCore()
        {
            _Rand = new Random();
            _AppFile = Application.ExecutablePath;
        }
        /// <summary>
        /// Náhoda
        /// </summary>
        private Random _Rand;
        /// <summary>
        /// Main soubor aplikace (adresář/jméno.exe)
        /// </summary>
        private string _AppFile;
        /// <summary>
        /// Inicializace subsystému: pojmenuje CurrentThread, registruje a povoluje DevEpxress skiny, nastavuje animace a výchozí skin
        /// </summary>
        public static void Init() { if (!__IsInitialized) Instance._Init(); }
        /// <summary>
        /// Inicializace subsystému
        /// </summary>
        private void _Init()
        {
            if (__IsInitialized) return;                   // mezivláknový konflikt při startu nehrozí, aplikace je spouštěna z jednoho vlákna. Tohle je ochrana proti opakované inicializaci.

            __IsInitialized = true;
            _ApplicationStartTime = System.Diagnostics.Process.GetCurrentProcess().StartTime;

            System.Threading.Thread.CurrentThread.Name = "GUI thread";
            DevExpress.UserSkins.BonusSkins.Register();
            DevExpress.Skins.SkinManager.EnableFormSkins();
            DevExpress.Skins.SkinManager.EnableMdiFormSkins();
            DevExpress.XtraEditors.WindowsFormsSettings.AnimationMode = DevExpress.XtraEditors.AnimationMode.EnableAll;
            DevExpress.XtraEditors.WindowsFormsSettings.AllowHoverAnimation = DevExpress.Utils.DefaultBoolean.True;
            DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = "iMaginary";
        }
        /// <summary>
        /// Je voláno při ukončení aplikace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            _Done();
        }
        /// <summary>
        /// Volá se při ukončení celé aplikace
        /// </summary>
        public static void Done() { Instance._Done(); }
        /// <summary>
        /// Při ukončení celé aplikace
        /// </summary>
        private void _Done()
        {
            this._DoneAppEvents();
            this._DisposeFontCache();
            this._ResetControlColors();
        }
        /// <summary>
        /// Zapojí aplikační eventy
        /// </summary>
        private void _InitAppEvents()
        {
            System.Windows.Forms.Application.ApplicationExit += Application_ApplicationExit;
            System.Windows.Forms.Application.Idle += Application_Idle;
        }
        /// <summary>
        /// Odpojí aplikační eventy
        /// </summary>
        private void _DoneAppEvents()
        {
            System.Windows.Forms.Application.Idle -= Application_Idle;
            System.Windows.Forms.Application.ApplicationExit -= Application_ApplicationExit;
        }
        /// <summary>
        /// Když GUI nemá do čeho píchnout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_Idle(object sender, EventArgs e)
        {
            if (!_ApplicationReadyTime.HasValue)
                _ApplicationReadyTime = DateTime.Now;

            CallListeners<IListenerApplicationIdle>();
        }
        /// <summary>
        /// Touto cestou si může nějaká komponenta vyžádat vyvolání určité své metody v GUI threadu v situaci, kdy GUI thread má volný čas.
        /// Nejvhodnější je použití pro Lazy inicializaci té části komponenty, která nemusí být inicializována hned při tvorbě, ale až když bude volný čas.
        /// </summary>
        public static void RunOnceOnGuiIdle()
        {

        }
        /// <summary>
        /// Doba trvání startu aplikace od spuštění procesu do prvního okamžiku, kdy je aplikace ready
        /// </summary>
        public static TimeSpan? ApplicationStartUpTime { get { return Instance._ApplicationStartUpTime; } }
        private TimeSpan? _ApplicationStartUpTime
        {
            get
            {
                var startTime = _ApplicationStartTime;
                var readyTime = _ApplicationReadyTime;
                if (startTime.HasValue && readyTime.HasValue) return (readyTime.Value - startTime.Value);
                return null;
            }
        }
        /// <summary>
        /// Čas, kdy byla aplikace spuštěna
        /// </summary>
        public DateTime? ApplicationStartTime { get { return _ApplicationStartTime; } }
        private DateTime? _ApplicationStartTime;
        /// <summary>
        /// Čas, od kdy je aplikace ready (=čas první události <see cref="Application.Idle"/>)
        /// </summary>
        public DateTime? ApplicationReadyTime { get { return _ApplicationReadyTime; } }
        private DateTime? _ApplicationReadyTime;
        #endregion
        #region Application - start, restart, MainForm
        /// <summary>
        /// Main soubor aplikace (plné jméno souboru = adresář/jméno.exe)
        /// </summary>
        public static string ApplicationFile { get { return Instance._AppFile; } }
        /// <summary>
        /// Main soubor aplikace (holé jméno souboru = jméno.exe)
        /// </summary>
        public static string ApplicationName { get { return System.IO.Path.GetFileName(Instance._AppFile); } }
        /// <summary>
        /// Main adresář aplikace (adresář kde je umístěn Main spuštěný soubor)
        /// </summary>
        public static string ApplicationPath { get { return System.IO.Path.GetDirectoryName(Instance._AppFile); } }
        /// <summary>
        /// Hlavní okno aplikace, pokud byla aplikace spuštěna pomocí <see cref="ApplicationStart(Type, Image)"/>
        /// </summary>
        public static Form MainForm { get { return Instance._MainForm; } }
        public static void ApplicationStart(Type mainFormType, Image splashImage) { Instance._ApplicationStart(mainFormType, splashImage); }
        public static void ApplicationRestart() { Instance._ApplicationRestart(); }
        private void _ApplicationStart(Type mainFormType, Image splashImage)
        {
            while (true)
            {
                _ApplicationDoRestart = false;

                _SplashShow("Testovací aplikace Helios Nephrite", "DJ soft & ASOL", "Copyright © 1995 - 2021 DJ soft" + Environment.NewLine + "All Rights reserved.", "Začínáme...",
                    null, splashImage, null,
                    DevExpress.XtraSplashScreen.FluentLoadingIndicatorType.Dots, null, null, true, true);

                _MainForm = System.Activator.CreateInstance(mainFormType) as Form;
                _MainForm.Shown += MainForm_Shown;
                ApplicationContext context = new ApplicationContext();
                context.MainForm = _MainForm;

                _SplashUpdate(subTitle: "Už to bude...");

                Application.VisualStyleState = System.Windows.Forms.VisualStyles.VisualStyleState.ClientAndNonClientAreasEnabled;
                Application.EnableVisualStyles();

                Application.Run(context);
                _MainForm = null;
                if (!_ApplicationDoRestart) break;

                Application.Restart();
                break;
            }
        }
        /// <summary>
        /// Při zobrazení MainFormu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (sender is Form mainForm)
                mainForm.Shown -= MainForm_Shown;
            _SplashHide();
        }
        private void _ApplicationRestart()
        {
            List<Form> forms = new List<Form>();
            foreach (Form form in Application.OpenForms)
                forms.Add(form);
            forms.Reverse();
            _ApplicationDoRestart = true;
            foreach (Form form in forms)
                form.Close();
            _MainForm = null;
        }
        private bool _ApplicationDoRestart;
        /// <summary>Hlavní okno aplikace, pokud byla aplikace spuštěna pomocí <see cref="ApplicationStart(Type, Image)"/></summary>
        private Form _MainForm;
        #endregion
        #region Splash Screen
        /// <summary>
        /// Zobrazí SplashScreen
        /// </summary>
        /// <param name="title"></param>
        /// <param name="subTitle"></param>
        /// <param name="leftFooter"></param>
        /// <param name="rightFooter"></param>
        /// <param name="owner"></param>
        /// <param name="image"></param>
        /// <param name="svgImage"></param>
        /// <param name="indicator"></param>
        /// <param name="opacityColor"></param>
        /// <param name="opacity"></param>
        /// <param name="useFadeIn"></param>
        /// <param name="useFadeOut"></param>
        public static void SplashShow(string title, string subTitle = null, string leftFooter = null, string rightFooter = null,
            Form owner = null, Image image = null, DevExpress.Utils.Svg.SvgImage svgImage = null,
            DevExpress.XtraSplashScreen.FluentLoadingIndicatorType? indicator = null, Color? opacityColor = null, int? opacity = null,
            bool useFadeIn = true, bool useFadeOut = true)
        { Instance._SplashShow(title, subTitle, leftFooter, rightFooter,
            owner, image, svgImage,
            indicator, opacityColor, opacity,
            useFadeIn, useFadeOut); }
        /// <summary>
        /// Aktualizuje zobrazený SplashScreen
        /// </summary>
        /// <param name="title"></param>
        /// <param name="subTitle"></param>
        /// <param name="leftFooter"></param>
        /// <param name="rightFooter"></param>
        /// <param name="opacityColor"></param>
        /// <param name="opacity"></param>
        public static void SplashUpdate(string title = null, string subTitle = null, string leftFooter = null, string rightFooter = null,
            Color? opacityColor = null, int? opacity = null)
        {
            Instance._SplashUpdate(title, subTitle, leftFooter, rightFooter, opacityColor, opacity);
        }
        /// <summary>
        /// Zavře zobrazený SplashScreen
        /// </summary>
        public static void SplashHide() { Instance._SplashHide(); }
        /// <summary>
        /// Zobrazí SplashScreen
        /// </summary>
        /// <param name="title"></param>
        /// <param name="subTitle"></param>
        /// <param name="leftFooter"></param>
        /// <param name="rightFooter"></param>
        /// <param name="owner"></param>
        /// <param name="image"></param>
        /// <param name="svgImage"></param>
        /// <param name="indicator"></param>
        /// <param name="opacityColor"></param>
        /// <param name="opacity"></param>
        /// <param name="useFadeIn"></param>
        /// <param name="useFadeOut"></param>
        private void _SplashShow(string title, string subTitle, string leftFooter, string rightFooter,
            Form owner, Image image, DevExpress.Utils.Svg.SvgImage svgImage,
            DevExpress.XtraSplashScreen.FluentLoadingIndicatorType? indicator, Color? opacityColor, int? opacity,
            bool useFadeIn, bool useFadeOut)
        {
            if (_SplashOptions != null) return;                      // Nějaký SplashScreen už svítí => nebudeme jej otevírat znovu!

            DevExpress.XtraSplashScreen.FluentSplashScreenOptions options = new DevExpress.XtraSplashScreen.FluentSplashScreenOptions();
            options.Title = title;
            options.Subtitle = subTitle ?? "Asseco Solutions";
            options.LeftFooter = leftFooter ?? "Copyright © 1995 - 2021" + Environment.NewLine + "All Rights reserved.";
            options.RightFooter = rightFooter ?? "starting...";
            options.LoadingIndicatorType = indicator ?? DevExpress.XtraSplashScreen.FluentLoadingIndicatorType.Dots;
            options.OpacityColor = opacityColor ?? Color.DarkBlue;
            options.Opacity = opacity ?? 40;

            if (svgImage != null)
            {
                options.LogoImageOptions.SvgImage = svgImage;
                options.LogoImageOptions.SvgImageSize = new Size(80, 80);
            }
            else if (image != null)
            {
                options.LogoImageOptions.Image = image;
            }

            DevExpress.XtraSplashScreen.SplashScreenManager.ShowFluentSplashScreen(
                options,
                parentForm: owner,
                startPos: DevExpress.XtraSplashScreen.SplashFormStartPosition.CenterScreen,
                useFadeIn: useFadeIn,
                useFadeOut: useFadeOut
            );

            _SplashOptions = options;
        }
        /// <summary>
        /// Aktualizuje zobrazený SplashScreen
        /// </summary>
        /// <param name="title"></param>
        /// <param name="subTitle"></param>
        /// <param name="leftFooter"></param>
        /// <param name="rightFooter"></param>
        /// <param name="opacityColor"></param>
        /// <param name="opacity"></param>
        private void _SplashUpdate(string title = null, string subTitle = null, string leftFooter = null, string rightFooter = null,
            Color? opacityColor = null, int? opacity = null)
        {
            var currentOptions = _SplashOptions;
            if (currentOptions == null) return;

            DevExpress.XtraSplashScreen.FluentSplashScreenOptions options = new DevExpress.XtraSplashScreen.FluentSplashScreenOptions();
            options.Assign(currentOptions);
            if (title != null) options.Title = title;
            if (subTitle != null) options.Subtitle = subTitle;
            if (leftFooter != null) options.LeftFooter = leftFooter;
            if (rightFooter != null) options.RightFooter = rightFooter;
            if (opacityColor != null) options.OpacityColor = opacityColor.Value;
            if (opacity != null) options.Opacity = opacity.Value;

            DevExpress.XtraSplashScreen.SplashScreenManager.Default?.SendCommand(DevExpress.XtraSplashScreen.FluentSplashScreenCommand.UpdateOptions, options);

            _SplashOptions = options;
        }
        /// <summary>
        /// Zavře zobrazený SplashScreen
        /// </summary>
        private void _SplashHide()
        {
            if (_SplashOptions != null)
            {
                DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
                _SplashOptions = null;
            }
        }
        /// <summary>
        /// Vlastnosti zobrazeného SplashScreen, nebo null když není
        /// </summary>
        private DevExpress.XtraSplashScreen.FluentSplashScreenOptions _SplashOptions;
        #endregion
        #region Základní styly (Appearance) pro zobrazování komponent
        /// <summary>
        /// Provede inicializaci standardních stylů
        /// </summary>
        private void _InitStyles()
        {
            var mainTitleStyle = new DevExpress.XtraEditors.StyleController();
            mainTitleStyle.Appearance.FontSizeDelta = 3;
            mainTitleStyle.Appearance.FontStyleDelta = FontStyle.Bold;
            mainTitleStyle.Appearance.Options.UseBorderColor = false;
            mainTitleStyle.Appearance.Options.UseBackColor = false;
            mainTitleStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            var subTitleStyle = new DevExpress.XtraEditors.StyleController();
            subTitleStyle.Appearance.FontSizeDelta = 2;
            subTitleStyle.Appearance.FontStyleDelta = FontStyle.Bold;
            subTitleStyle.Appearance.Options.UseBorderColor = false;
            subTitleStyle.Appearance.Options.UseBackColor = false;
            subTitleStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            var labelStyle = new DevExpress.XtraEditors.StyleController();
            labelStyle.Appearance.FontSizeDelta = 0;
            labelStyle.Appearance.FontStyleDelta = FontStyle.Italic;
            labelStyle.Appearance.Options.UseBorderColor = false;
            labelStyle.Appearance.Options.UseBackColor = false;
            labelStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            var infoStyle = new DevExpress.XtraEditors.StyleController();
            infoStyle.Appearance.FontSizeDelta = 0;
            infoStyle.Appearance.FontStyleDelta = FontStyle.Italic;
            infoStyle.Appearance.Options.UseBorderColor = false;
            infoStyle.Appearance.Options.UseBackColor = false;
            infoStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            var inputStyle = new DevExpress.XtraEditors.StyleController();
            inputStyle.Appearance.FontSizeDelta = 0;
            inputStyle.Appearance.FontStyleDelta = FontStyle.Regular;
            inputStyle.Appearance.Options.UseBorderColor = false;
            inputStyle.Appearance.Options.UseBackColor = false;
            inputStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            _MainTitleStyle = mainTitleStyle;
            _SubTitleStyle = subTitleStyle;
            _LabelStyle = labelStyle;
            _InfoStyle = infoStyle;
            _InputStyle = inputStyle;

            _DetailXLabel = 16;
            _DetailXText = 12;
            _DetailYFirst = 9;
            _DetailYHeightLabel = 19;
            _DetailYHeightText = 22;
            _DetailYOffsetLabelText = 5;
            _DetailYSpaceLabel = 2;
            _DetailYSpaceText = 3;
            _DetailXMargin = 6;
            _DetailYMargin = 4;

            _DefaultButtonPanelHeight = 40;
            _DefaultButtonWidth = 150;
            _DefaultButtonHeight = 32;
            _DefaultButtonXSpace = 6;

            _Zoom = 1m;
            _DesignDpi = 96;
            _RecalcZoomDpi();

            _DefaultBarManager = new DevExpress.XtraBars.BarManager();
            _DefaultToolTipController = CreateNewToolTipController();
        }
        /// <summary>
        /// Vrátí styl labelu podle požadovaného typu
        /// </summary>
        /// <param name="styleType"></param>
        /// <returns></returns>
        public static DevExpress.XtraEditors.IStyleController GetLabelStyle(LabelStyleType? styleType) { return Instance._GetLabelStyle(styleType); }
        /// <summary>
        /// Vrátí styl labelu podle požadovaného typu
        /// </summary>
        /// <param name="styleType"></param>
        /// <returns></returns>
        private DevExpress.XtraEditors.IStyleController _GetLabelStyle(LabelStyleType? styleType)
        {
            switch (styleType)
            {
                case LabelStyleType.MainTitle: return _MainTitleStyle;
                case LabelStyleType.SubTitle: return _SubTitleStyle;
                case LabelStyleType.Default: return _LabelStyle;
                case LabelStyleType.Info: return _InfoStyle;
            }
            return _LabelStyle;
        }
        /// <summary>
        /// Styl pro labely zobrazené jako <see cref="LabelStyleType.MainTitle"/>
        /// </summary>
        public static DevExpress.XtraEditors.StyleController MainTitleStyle { get { return Instance._MainTitleStyle; } }
        /// <summary>
        /// Styl pro labely zobrazené jako <see cref="LabelStyleType.SubTitle"/>
        /// </summary>
        public static DevExpress.XtraEditors.StyleController TitleStyle { get { return Instance._SubTitleStyle; } }
        /// <summary>
        /// Styl pro labely zobrazené jako <see cref="LabelStyleType.Default"/>
        /// </summary>
        public static DevExpress.XtraEditors.StyleController LabelStyle { get { return Instance._LabelStyle; } }
        /// <summary>
        /// Styl pro labely zobrazené jako <see cref="LabelStyleType.Info"/>
        /// </summary>
        public static DevExpress.XtraEditors.StyleController InfoStyle { get { return Instance._InfoStyle; } }
        /// <summary>
        /// Styl pro Input prvky
        /// </summary>
        public static DevExpress.XtraEditors.StyleController InputStyle { get { return Instance._InputStyle; } }
        /// <summary>
        /// Odsazení labelu od levého okraje X
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDetailXLabel(int targetDpi) { return ZoomToGui(Instance._DetailXLabel, targetDpi); }
        /// <summary>
        /// Odsazení textu od levého okraje X
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDetailXText(int targetDpi) { return ZoomToGui(Instance._DetailXText, targetDpi); }
        /// <summary>
        /// Odsazení prvního prvku od horního okraje Y
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDetailYFirst(int targetDpi) { return ZoomToGui(Instance._DetailYFirst, targetDpi); }
        /// <summary>
        /// Vnitřní okraje, defaultní hodnota, vychází z <see cref="GetDetailXMargin(int)"/> a <see cref="GetDetailYMargin(int)"/>
        /// </summary>
        /// <returns></returns>
        public static Padding DefaultInnerMargins { get { int x = Instance._DetailXMargin; int y = Instance._DetailYMargin; return new Padding(x, y, x, y); } }
        /// <summary>
        /// Vnitřní okraje, defaultní hodnota, vychází z <see cref="GetDetailXMargin(int)"/> a <see cref="GetDetailYMargin(int)"/>
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        public static Padding GetDefaultInnerMargins(int targetDpi) { return ZoomToGui(DefaultInnerMargins, targetDpi); }
        /// <summary>
        /// Výchozí hodnota výšky labelu
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDetailYHeightLabel(int targetDpi) { return ZoomToGui(Instance._DetailYHeightLabel, targetDpi); }
        /// <summary>
        /// Výchozí hodnota výšky textu
        /// </summary>
        public static int GetDetailYHeightText(int targetDpi) { return ZoomToGui(Instance._DetailYHeightText, targetDpi); }
        /// <summary>
        /// Posun labelu vůči textu v ose Y pro zarovnané úpatí textu
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDetailYOffsetLabelText(int targetDpi) { return ZoomToGui(Instance._DetailYOffsetLabelText, targetDpi); }
        /// <summary>
        /// Odsazení labelu dalšího řádku od předešlého textu
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDetailYSpaceLabel(int targetDpi) { return ZoomToGui(Instance._DetailYSpaceLabel, targetDpi); }
        /// <summary>
        /// Odsazení textu řádku od předešlého labelu
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDetailYSpaceText(int targetDpi) { return ZoomToGui(Instance._DetailYSpaceText, targetDpi); }
        /// <summary>
        /// Okraj v ose X
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDetailXMargin(int targetDpi) { return ZoomToGui(Instance._DetailXMargin, targetDpi); }
        /// <summary>
        /// Okraj v ose Y
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDetailYMargin(int targetDpi) { return ZoomToGui(Instance._DetailYMargin, targetDpi); }
        /// <summary>
        /// Defaultní výška panelu s buttony, designová hodnota
        /// </summary>
        public static int DefaultButtonPanelHeight { get { return Instance._DefaultButtonPanelHeight; } }
        /// <summary>
        /// Defaultní výška panelu s buttony
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDefaultButtonPanelHeight(int targetDpi) { return ZoomToGui(Instance._DefaultButtonPanelHeight, targetDpi); }
        /// <summary>
        /// Defaultní šířka buttonu
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDefaultButtonWidth(int targetDpi) { return ZoomToGui(Instance._DefaultButtonWidth, targetDpi); }
        /// <summary>
        /// Defaultní výška buttonu, designová hodnota
        /// </summary>
        public static int DefaultButtonHeight { get { return Instance._DefaultButtonHeight; } }
        /// <summary>
        /// Defaultní výška buttonu
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDefaultButtonHeight(int targetDpi) { return ZoomToGui(Instance._DefaultButtonHeight, targetDpi); }
        /// <summary>
        /// Defaultní mezera mezi buttony, designová hodnota
        /// </summary>
        public static int DefaultButtonXSpace { get { return Instance._DefaultButtonXSpace; } }
        /// <summary>
        /// Defaultní mezera mezi buttony
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDefaultButtonXSpace(int targetDpi) { return ZoomToGui(Instance._DefaultButtonXSpace, targetDpi); }
        /// <summary>
        /// Vrásí souřadnici, na které má začínat obsah velký <paramref name="contentSize"/> uvnitř daného prostoru <paramref name="totalSize"/>,
        /// tak aby obsah byl zarovnán na začátek / na střed / na konec podle <paramref name="alignment"/>.
        /// Pokud daný prostor <paramref name="totalSize"/> je menší než potřebný prostor <paramref name="contentSize"/>, pak bude vrácena 0.
        /// </summary>
        /// <param name="totalSize"></param>
        /// <param name="contentSize"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static int CalculateAlignedBegin(int totalSize, int contentSize, AlignContentToSide alignment)
        {
            int freeSize = totalSize - contentSize;
            if (freeSize < 0) return 0;
            switch (alignment)
            {
                case AlignContentToSide.Center: return freeSize / 2;
                case AlignContentToSide.End: return freeSize;
            }
            return 0;
        }
        /// <summary>
        /// Defaultní BarManager pro obecné použití
        /// </summary>
        public static DevExpress.XtraBars.BarManager DefaultBarManager { get { return Instance._DefaultBarManager; } }
        /// <summary>
        /// Defaultní ToolTipController pro obecné použití
        /// </summary>
        public static ToolTipController DefaultToolTipController { get { return Instance._DefaultToolTipController; } }
        /// <summary>
        /// Vytvoří a vrátí new instanci ToolTipController, standardně deklarovanou
        /// </summary>
        /// <returns></returns>
        public static ToolTipController CreateNewToolTipController()
        {
            var ttc = new ToolTipController()
            {
                Active = true,
                InitialDelay = 400,
                AutoPopDelay = 10000,
                ReshowDelay = 1000,
                KeepWhileHovered = true,
                Rounded = true,
                RoundRadius = 20,
                ShowShadow = true,
                ToolTipAnchor = DevExpress.Utils.ToolTipAnchor.Object,
                ToolTipLocation = DevExpress.Utils.ToolTipLocation.RightBottom,
                ToolTipStyle = DevExpress.Utils.ToolTipStyle.Windows7,
                ToolTipType = DevExpress.Utils.ToolTipType.SuperTip,       // Standard   Flyout   SuperTip;
                IconSize = DevExpress.Utils.ToolTipIconSize.Large,
                CloseOnClick = DevExpress.Utils.DefaultBoolean.True,
                ShowBeak = true
            };

            ttc.Appearance.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;

            return ttc;
        }
        /// <summary>
        /// Převede string obsahující písmena B,I,U,S na odpovídající <see cref="FontStyle"/>.
        /// Pokud je na vstupu null nebo prázdný string, vrací null.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public static FontStyle? ConvertFontStyle(string style)
        {
            if (String.IsNullOrEmpty(style)) return null;
            FontStyle fontStyle = FontStyle.Regular;
            style = style.ToUpper();
            if (style.Contains("B")) fontStyle |= FontStyle.Bold;
            if (style.Contains("I")) fontStyle |= FontStyle.Italic;
            if (style.Contains("U")) fontStyle |= FontStyle.Underline;
            if (style.Contains("S")) fontStyle |= FontStyle.Strikeout;
            return fontStyle;
        }
        /// <summary>
        /// Převede boolean hodnoty na odpovídající <see cref="FontStyle"/>.
        /// </summary>
        /// <returns></returns>
        public static FontStyle? ConvertFontStyle(bool bold, bool italic, bool underline, bool strikeOut)
        {
            FontStyle fontStyle = FontStyle.Regular;
            if (bold) fontStyle |= FontStyle.Bold;
            if (italic) fontStyle |= FontStyle.Italic;
            if (underline) fontStyle |= FontStyle.Underline;
            if (strikeOut) fontStyle |= FontStyle.Strikeout;
            return fontStyle;
        }
        private DevExpress.XtraEditors.StyleController _MainTitleStyle;
        private DevExpress.XtraEditors.StyleController _SubTitleStyle;
        private DevExpress.XtraEditors.StyleController _LabelStyle;
        private DevExpress.XtraEditors.StyleController _InfoStyle;
        private DevExpress.XtraEditors.StyleController _InputStyle;
        private int _DetailXLabel;
        private int _DetailXText;
        private int _DetailYFirst;
        private int _DetailYHeightLabel;
        private int _DetailYHeightText;
        private int _DetailYOffsetLabelText;
        private int _DetailYSpaceLabel;
        private int _DetailYSpaceText;
        private int _DetailXMargin;
        private int _DetailYMargin;
        private int _DefaultButtonPanelHeight;
        private int _DefaultButtonWidth;
        private int _DefaultButtonHeight;
        private int _DefaultButtonXSpace;
        private DevExpress.XtraBars.BarManager _DefaultBarManager;
        private ToolTipController _DefaultToolTipController;
        #endregion
        #region Rozhraní na Zoom
        /// <summary>
        /// Inicializace Zoomu
        /// </summary>
        private void _InitZoom()
        {
            _Zoom = 1m;
            SystemAdapter.InteractiveZoomChanged += Host_InteractiveZoomChanged;
            _ReloadZoom();
        }
        /// <summary>
        /// Po interaktivní změně Zoomu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Host_InteractiveZoomChanged(object sender, EventArgs e)
        {
            _ReloadZoom();
            _CallListeners<IListenerZoomChange>();
        }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static int ZoomToGui(int value) { decimal zoom = Instance._Zoom; return _ZoomToGui(value, zoom); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static int ZoomToGui(int value, int targetDpi) { decimal zoomDpi = Instance._ZoomDpi; return _ZoomDpiToGui(value, zoomDpi, targetDpi); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static float ZoomToGui(float value, int targetDpi) { decimal zoomDpi = Instance._ZoomDpi; return _ZoomDpiToGui(value, zoomDpi, targetDpi); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Int32Range ZoomToGui(Int32Range value, int targetDpi) { if (value == null) return null; decimal zoomDpi = Instance._ZoomDpi; return new Int32Range(_ZoomDpiToGui(value.Begin, zoomDpi, targetDpi), _ZoomDpiToGui(value.End, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Point ZoomToGui(Point value) { decimal zoom = Instance._Zoom; return new Point(_ZoomToGui(value.X, zoom), _ZoomToGui(value.Y, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Point? ZoomToGui(Point? value) { if (!value.HasValue) return null; decimal zoom = Instance._Zoom; var v = value.Value; return new Point(_ZoomToGui(v.X, zoom), _ZoomToGui(v.Y, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Point ZoomToGui(Point value, int targetDpi) { decimal zoomDpi = Instance._ZoomDpi; return new Point(_ZoomDpiToGui(value.X, zoomDpi, targetDpi), _ZoomDpiToGui(value.Y, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Size ZoomToGui(Size value) { decimal zoom = Instance._Zoom; return new Size(_ZoomToGui(value.Width, zoom), _ZoomToGui(value.Height, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Size? ZoomToGui(Size? value) { if (!value.HasValue) return null; decimal zoom = Instance._Zoom; var v = value.Value; return new Size(_ZoomToGui(v.Width, zoom), _ZoomToGui(v.Height, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Size ZoomToGui(Size value, int targetDpi) { decimal zoomDpi = Instance._ZoomDpi; return new Size(_ZoomDpiToGui(value.Width, zoomDpi, targetDpi), _ZoomDpiToGui(value.Height, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty.
        /// <para/>
        /// Rectangle je vytvářen z přepočtených souřadnic (Left, Top, Right, Bottom), 
        /// tím je zaručeno, že pravý a dolní okraj výsledných Rectangle bude zarovnán stejně jako v Designu, 
        /// tedy že nebude "rozházený" vlivem zaokrouhlení, kdy se může stát, že (Round(X,0) + Round(Width,0)) != Round(Right,0) !!!
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Rectangle ZoomToGui(Rectangle value) { decimal zoom = Instance._Zoom; return Rectangle.FromLTRB(_ZoomToGui(value.Left, zoom), _ZoomToGui(value.Top, zoom), _ZoomToGui(value.Right, zoom), _ZoomToGui(value.Bottom, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty.
        /// <para/>
        /// Rectangle je vytvářen z přepočtených souřadnic (Left, Top, Right, Bottom), 
        /// tím je zaručeno, že pravý a dolní okraj výsledných Rectangle bude zarovnán stejně jako v Designu, 
        /// tedy že nebude "rozházený" vlivem zaokrouhlení, kdy se může stát, že (Round(X,0) + Round(Width,0)) != Round(Right,0) !!!
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Rectangle? ZoomToGui(Rectangle? value) { if (!value.HasValue) return null; decimal zoom = Instance._Zoom; var v = value.Value; return Rectangle.FromLTRB(_ZoomToGui(v.Left, zoom), _ZoomToGui(v.Top, zoom), _ZoomToGui(v.Right, zoom), _ZoomToGui(v.Bottom, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty.
        /// <para/>
        /// Rectangle je vytvářen z přepočtených souřadnic (Left, Top, Right, Bottom), 
        /// tím je zaručeno, že pravý a dolní okraj výsledných Rectangle bude zarovnán stejně jako v Designu, 
        /// tedy že nebude "rozházený" vlivem zaokrouhlení, kdy se může stát, že (Round(X,0) + Round(Width,0)) != Round(Right,0) !!!
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Rectangle ZoomToGui(Rectangle value, int targetDpi) { decimal zoomDpi = Instance._ZoomDpi; return Rectangle.FromLTRB(_ZoomDpiToGui(value.Left, zoomDpi, targetDpi), _ZoomDpiToGui(value.Top, zoomDpi, targetDpi), _ZoomDpiToGui(value.Right, zoomDpi, targetDpi), _ZoomDpiToGui(value.Bottom, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Padding ZoomToGui(Padding value) { decimal zoom = Instance._Zoom; return new Padding(_ZoomToGui(value.Left, zoom), _ZoomToGui(value.Top, zoom), _ZoomToGui(value.Right, zoom), _ZoomToGui(value.Bottom, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Padding? ZoomToGui(Padding? value) { if (!value.HasValue) return null; decimal zoom = Instance._Zoom; var v = value.Value; return new Padding(_ZoomToGui(v.Left, zoom), _ZoomToGui(v.Top, zoom), _ZoomToGui(v.Right, zoom), _ZoomToGui(v.Bottom, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Padding ZoomToGui(Padding value, int targetDpi) { decimal zoomDpi = Instance._ZoomDpi; return new Padding(_ZoomDpiToGui(value.Left, zoomDpi, targetDpi), _ZoomDpiToGui(value.Top, zoomDpi, targetDpi), _ZoomDpiToGui(value.Right, zoomDpi, targetDpi), _ZoomDpiToGui(value.Bottom, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí daný font přepočtený dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Font ZoomToGui(Font value, int targetDpi)
        {
            var instance = Instance;
            decimal zoomDpi = instance._ZoomDpi;
            float emSize = _ZoomDpiToGui(value.Size, Instance._ZoomDpi, targetDpi);
            return instance._GetFont(null, value.FontFamily, null, emSize, value.Style);
        }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle daného Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        private static int _ZoomToGui(int value, decimal zoom) { return (int)Math.Round((decimal)value * zoom, 0); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle daného (Zoom / DesignDpi) a dané TargetDpi do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota</param>
        /// <param name="zoomDpi"></param>
        /// <param name="targetDpi"></param>
        /// <returns></returns>
        private static int _ZoomDpiToGui(int value, decimal zoomDpi, decimal targetDpi) { return (int)Math.Round((decimal)value * zoomDpi * targetDpi, 0); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle daného (Zoom / DesignDpi) a dané TargetDpi do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota</param>
        /// <param name="zoomDpi"></param>
        /// <param name="targetDpi"></param>
        /// <returns></returns>
        private static float _ZoomDpiToGui(float value, decimal zoomDpi, decimal targetDpi) { return (float)((decimal)value * zoomDpi * targetDpi); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle daného (Zoom / DesignDpi) a dané TargetDpi do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota</param>
        /// <param name="zoomDpi"></param>
        /// <param name="targetDpi"></param>
        /// <returns></returns>
        private static decimal _ZoomDpiToGui(decimal value, decimal zoomDpi, decimal targetDpi) { return (value * zoomDpi * targetDpi); }
        /// <summary>
        /// Aktuální hodnota Zoomu
        /// </summary>
        internal static decimal Zoom { get { return Instance._Zoom; } }
        /// <summary>
        /// Hodnota DPI, ke které se vztahují velikosti prvků zadávané jako DesignBounds.
        /// Reálná velikost prvků se pak konvertuje na cílové DPI monitoru.
        /// </summary>
        public static int DesignDpi { get { return Instance._DesignDpi; } set { Instance._SetDesignDpi(value); } }

        /// <summary>
        /// Aktuální hodnota Zoomu a SourceDpi
        /// </summary>
        internal static decimal ZoomDpi { get { return Instance._Zoom; } }
        /// <summary>
        /// Reload hodnoty Zoomu
        /// </summary>
        internal static void ReloadZoom() { Instance._ReloadZoom(); }
        /// <summary>
        /// Reload hodnoty Zoomu uvnitř instance
        /// </summary>
        private void _ReloadZoom()
        {
            _Zoom = SystemAdapter.ZoomRatio;
            _RecalcZoomDpi();
        }
        /// <summary>
        /// Uloží hodnotu DesignDpi a přepočte další...
        /// </summary>
        /// <param name="designDpi"></param>
        private void _SetDesignDpi(int designDpi)
        {
            _DesignDpi = (designDpi < 72 ? 72 : (designDpi > 600 ? 600 : designDpi));
            _RecalcZoomDpi();
        }
        private void _RecalcZoomDpi()
        {
            // Hodnota _ZoomDpi slouží k rychlému přepočtu Designové hodnoty (int)
            // s pomocí Zoomu (kde 1.5 = 150%) a konverze rozměru pomocí DPI Design - Current (kde Design = 96, a UHD má Target = 144)
            // na cílovou Current hodnotu (int):
            // Current = (Design * _ZoomDpi * TargetDpi)

            _ZoomDpi = _Zoom / (decimal)_DesignDpi;
        }
        private decimal _Zoom;
        private int _DesignDpi;
        private decimal _ZoomDpi;
        #endregion
        #region FontCache
        /// <summary>
        /// Vrátí požadovaný font z cache. 
        /// Nedávejme na něm Dispose(), tedy nepoužívejme jej v using() patternu!!!
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public static Font GetFont(Font prototype, FontStyle? style = null) { return Instance._GetFont(prototype, null, null, prototype.Size, style); }
        /// <summary>
        /// Vrátí požadovaný font z cache. 
        /// Nedávejme na něm Dispose(), tedy nepoužívejme jej v using() patternu!!!
        /// </summary>
        /// <param name="family"></param>
        /// <param name="emSize"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public static Font GetFont(FontFamily family, float emSize, FontStyle? style = null) { return Instance._GetFont(null, family, null, emSize, style); }
        /// <summary>
        /// Vrátí požadovaný font z cache. 
        /// Nedávejme na něm Dispose(), tedy nepoužívejme jej v using() patternu!!!
        /// </summary>
        /// <param name="familyName"></param>
        /// <param name="emSize"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public static Font GetFont(string familyName, float emSize, FontStyle? style = null) { return Instance._GetFont(null, null, familyName, emSize, style); }
        /// <summary>
        /// Vrátí požadovaný font z cache. 
        /// Nedávejme na něm Dispose(), tedy nepoužívejme jej v using() patternu!!!
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="family"></param>
        /// <param name="familyName"></param>
        /// <param name="emSize"></param>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        private Font _GetFont(Font prototype, FontFamily family, string familyName, float emSize, FontStyle? fontStyle = null)
        {
            string name = (prototype != null ? prototype.Name : (family != null ? family.Name : familyName));
            float size = (float)Math.Round((decimal)emSize, 2);
            FontStyle style = fontStyle ?? FontStyle.Regular;
            string styleKey = (style.HasFlag(FontStyle.Bold) ? "B" : "") + (style.HasFlag(FontStyle.Italic) ? "I" : "") + (style.HasFlag(FontStyle.Underline) ? "U" : "") + (style.HasFlag(FontStyle.Strikeout) ? "S" : "");
            string key = $"'{name}':{size:###0.00}:{styleKey }";
            Font font;
            if (!_FontCache.TryGetValue(key, out font))
            {
                if (prototype != null)
                    font = new Font(prototype, style);
                else if (family != null)
                    font = new Font(family, size, style);
                else if (!String.IsNullOrEmpty(familyName))
                    font = new Font(familyName, size, style);
                else
                    throw new ArgumentException($"Nelze vytvořit Font bez zadání jeho druhu.");
                lock (_FontCache)
                {
                    if (!_FontCache.ContainsKey(key))
                        _FontCache.Add(key, font);
                }
            }
            return font;
        }
        /// <summary>
        /// Inicializace cache pro fonty
        /// </summary>
        private void _InitFontCache()
        {
            _FontCache = new Dictionary<string, Font>();
        }
        /// <summary>
        /// Dispose cache pro fonty
        /// </summary>
        private void _DisposeFontCache()
        {
            if (_FontCache != null)
                _FontCache.Values.ForEachExec(f => { if (f != null) f.Dispose(); });
            _FontCache.Clear();
        }
        /// <summary>
        /// Cache fontů. Klíče si instance tvoří sama.
        /// </summary>
        private Dictionary<string, Font> _FontCache;
        #endregion
        #region Listenery
        /// <summary>
        /// Jakýkoli objekt se může touto metodou přihlásit k odběru zpráv o událostech systému.
        /// Když v systému dojde k obecné události, například "Změna Zoomu" nebo "Změna Skinu", pak zdroj této události zavolá systém <see cref="DxComponent"/>,
        /// ten vyhledá odpovídající listenery (které jsou naživu) a vyvolá jejich odpovídající metodu.
        /// <para/>
        /// Typy událostí jsou určeny tím, který konkrétní interface (potomek <see cref="IListener"/>) daný posluchač implementuje.
        /// Na příkladu Změny Zoomu: tam, kde dojde ke změně Zoomu (Desktop) bude vyvolaná metoda <see cref="DxComponent.CallListeners{T}()"/>,
        /// tato metoda vyhledá ve svém seznamu Listenerů ty, které implementují <see cref="IListenerZoomChange"/>, a vyvolá jejich výkonnou metodu.
        /// </summary>
        /// <param name="listener"></param>
        public static void RegisterListener(IListener listener) { Instance._RegisterListener(listener); }
        /// <summary>
        /// Zavolá Listenery daného typu a předá jim daný argumen
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void CallListeners<T>() where T : IListener { Instance._CallListeners<T>(); }
        /// <summary>
        /// Zavolá Listenery daného typu a předá jim daný argumen
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args"></param>
        public static void CallListeners<T>(object args) where T : IListener { Instance._CallListeners<T>(args); }
        /// <summary>
        /// Odregistruje daný objekt z příjmu zpráv systému.
        /// Typicky se volá při Dispose.
        /// Dokud se objekt neodregistruje a pokud je naživu, bude dostávat zprávy.
        /// Zde se udržuje pouze WeakReference na Listener, takže nebráníme uvolnění Listeneru z paměti.
        /// </summary>
        /// <param name="listener"></param>
        public static void UnregisterListener(IListener listener) { Instance._UnregisterListener(listener); }
        /// <summary>
        /// Inicializace subsystému Listeners
        /// </summary>
        private void _InitListeners()
        {
            __Listeners = new List<_ListenerInstance>();
            __ListenersLastClean = DateTime.Now;
            DevExpress.LookAndFeel.UserLookAndFeel.Default.StyleChanged += DevExpress_StyleChanged;
        }
        /// <summary>
        /// Handler události, kdy DevExpress změní styl (skin)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DevExpress_StyleChanged(object sender, EventArgs e)
        {
            _ResetControlColors();                         // Tady se nuluje příznak __IsDarkTheme
            bool nowIsDark = _IsDarkTheme();               // Tady se napočte příznak __IsDarkTheme
            _CallListeners<IListenerStyleChanged>();
            bool isChange = (!__WasDarkTheme.HasValue || (__WasDarkTheme.HasValue && __WasDarkTheme.Value != nowIsDark));
            __WasDarkTheme = nowIsDark;
            if (isChange)
            {
                _CallListeners<IListenerLightDarkChanged>();
            }
        }
        /// <summary>
        /// Paměť předchozího tmavého skinu, pro vyhodnocení změny Světlý/Tmavý
        /// </summary>
        private bool? __WasDarkTheme = null;
        /// <summary>
        /// Zaregistruje dodanou instanci, která chce být posluchačem systémových událostí
        /// </summary>
        /// <param name="listener"></param>
        private void _RegisterListener(IListener listener)
        {
            if (listener == null) return;

            _ClearDeadListeners();
            lock (__Listeners)
                __Listeners.Add(new _ListenerInstance(listener));
        }
        /// <summary>
        /// Odešle systémovou událost všem zvědavým a živým posluchačům
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private void _CallListeners<T>() where T : IListener
        {
            _CallFixedListeners(typeof(T));

            var listeners = _GetListeners<T>();
            if (listeners.Length == 0) return;
            var method = _GetListenerMethod(typeof(T), 0);
            foreach (var listener in listeners)
                method.Invoke(listener, null);
        }
        /// <summary>
        /// Zavolá fixní listenery, kteří nemusí být registrovaní
        /// </summary>
        /// <param name="listenerType"></param>
        private void _CallFixedListeners(Type listenerType)
        {
        }
        /// <summary>
        /// Odešle systémovou událost všem zvědavým a živým posluchačům
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args"></param>
        private void _CallListeners<T>(object args) where T : IListener
        {
            var listeners = _GetListeners<T>();
            if (listeners.Length == 0) return;
            var method = _GetListenerMethod(typeof(T), 1);
            object[] parameters = new object[] { args };
            foreach (var listener in listeners)
                method.Invoke(listener, parameters);
        }
        /// <summary>
        /// Metoda najde jedinou metodu daného typu, a ověří že má přesně daný počet parametrů.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parameterCount"></param>
        /// <returns></returns>
        private System.Reflection.MethodInfo _GetListenerMethod(Type type, int parameterCount)
        {
            var methods = type.GetMethods();
            if (methods.Length != 1)
                throw new InvalidOperationException($"Interface '{type.Name}' is not valid IListener interface, must have exact one method.");
            var method = methods[0];
            var parameters = method.GetParameters();
            if (parameters.Length != parameterCount)
                throw new InvalidOperationException($"Interface '{type.Name}' is not valid IListener interface for call with {parameterCount} parameters, has {parameters.Length} parameters.");
            return method;
        }
        /// <summary>
        /// Odebere daného posluchače ze seznamu
        /// </summary>
        /// <param name="listener"></param>
        private void _UnregisterListener(IListener listener)
        {
            lock (__Listeners)
                __Listeners.RemoveAll(s => !s.IsAlive || s.ContainsListener(listener));
            __ListenersLastClean = DateTime.Now;
        }
        /// <summary>
        /// Vrátí pole živých Listenerů daného typu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private T[] _GetListeners<T>() where T : IListener
        {
            _ClearDeadListeners();
            T[] listeners = null;
            lock (__Listeners)
                listeners = __Listeners.Where(l => l.IsAlive).Select(l => l.Listener).OfType<T>().ToArray();
            return listeners;
        }
        /// <summary>
        /// Odebere mrtvé Listenery z pole <see cref="__Listeners"/>.
        /// Bez parametru <paramref name="force"/> se reálně provede až po 30 sekundách od posledního reálného provedení úklidu.
        /// </summary>
        /// <param name="force"></param>
        private void _ClearDeadListeners(bool force = false)
        {
            if (!force && ((TimeSpan)(DateTime.Now - __ListenersLastClean)).TotalSeconds < 30d) return;  // Když to není nutné, nebudeme to řešit
            lock (__Listeners)
                __Listeners.RemoveAll(s => !s.IsAlive);
            __ListenersLastClean = DateTime.Now;
        }
        /// <summary>
        /// Pole všech zaregistrovaných posluchačů systémových událostí
        /// </summary>
        private List<_ListenerInstance> __Listeners;
        /// <summary>
        /// Datum a čas, kdy se naposledy odklízeli zesnulí posluchači
        /// </summary>
        private DateTime __ListenersLastClean;
        /// <summary>
        /// Evidence jednoho listenera (posluchače zpráv), obsahuje WeakReferenci na něj
        /// </summary>
        private class _ListenerInstance
        {
            public _ListenerInstance(IListener listener)
            {
                __Listener = new WeakTarget<IListener>(listener);
            }
            private WeakTarget<IListener> __Listener;
            /// <summary>
            /// true pokud je objekt použitelný
            /// </summary>
            public bool IsAlive { get { return __Listener.IsAlive; } }
            /// <summary>
            /// Reference na instanci
            /// </summary>
            public IListener Listener { get { return __Listener?.Target; } }
            /// <summary>
            /// Vrátí true, pokud this instance drží odkaz na daného subscribera.
            /// </summary>
            public bool ContainsListener(IListener testListener)
            {
                IListener mySubscriber = __Listener.Target;
                return (mySubscriber != null && Object.ReferenceEquals(mySubscriber, testListener));
            }
        }
        #endregion
        #region Factory metody pro jednořádkovou tvorbu běžných komponent
        public static DxPanelControl CreateDxPanel(Control parent = null,
            DockStyle? dock = null, DevExpress.XtraEditors.Controls.BorderStyles? borderStyles = null,
            int? width = null, int? height = null,
            bool? visible = null)
        {
            var inst = Instance;

            var panel = new DxPanelControl();
            if (parent != null) parent.Controls.Add(panel);
            if (dock.HasValue) panel.Dock = dock.Value;
            if (width.HasValue) panel.Width = width.Value;
            if (height.HasValue) panel.Height = height.Value;
            if (borderStyles.HasValue) panel.BorderStyle = borderStyles.Value;
            if (visible.HasValue) panel.Visible = visible.Value;
            return panel;
        }
        public static DxSplitContainerControl CreateDxSplitContainer(Control parent = null, EventHandler splitterPositionChanged = null, DockStyle? dock = null,
            Orientation splitLineOrientation = Orientation.Horizontal, DevExpress.XtraEditors.SplitFixedPanel? fixedPanel = null,
            int? splitPosition = null, DevExpress.XtraEditors.SplitPanelVisibility? panelVisibility = null,
            bool? showSplitGlyph = null, DevExpress.XtraEditors.Controls.BorderStyles? borderStyles = null)
        {
            var inst = Instance;

            var container = new DxSplitContainerControl() { Horizontal = (splitLineOrientation == Orientation.Vertical) };
            if (parent != null) parent.Controls.Add(container);
            if (dock.HasValue) container.Dock = dock.Value;
            container.FixedPanel = (fixedPanel ?? DevExpress.XtraEditors.SplitFixedPanel.None);
            container.SplitterPosition = (splitPosition ?? 200);
            container.PanelVisibility = (panelVisibility ?? DevExpress.XtraEditors.SplitPanelVisibility.Both);
            if (borderStyles.HasValue) container.BorderStyle = borderStyles.Value;
            container.ShowSplitGlyph = (showSplitGlyph.HasValue ? (showSplitGlyph.Value ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False) : DevExpress.Utils.DefaultBoolean.Default);

            if (splitterPositionChanged != null) container.SplitterMoved += splitterPositionChanged;

            return container;
        }
        public static DxLabelControl CreateDxLabel(int x, int y, int w, Control parent, string text,
            LabelStyleType? styleType = null, DevExpress.Utils.WordWrap? wordWrap = null, DevExpress.XtraEditors.LabelAutoSizeMode? autoSizeMode = null, DevExpress.Utils.HorzAlignment? hAlignment = null,
            bool? visible = null, bool useLabelTextOffset = false)
        {
            return CreateDxLabel(x, ref y, w, parent, text,
                styleType, wordWrap, autoSizeMode, hAlignment,
                visible, useLabelTextOffset, false);
        }
        public static DxLabelControl CreateDxLabel(int x, ref int y, int w, Control parent, string text,
            LabelStyleType? styleType = null, DevExpress.Utils.WordWrap? wordWrap = null, DevExpress.XtraEditors.LabelAutoSizeMode? autoSizeMode = null, DevExpress.Utils.HorzAlignment? hAlignment = null,
            bool? visible = null, bool useLabelTextOffset = false, bool shiftY = false)
        {
            var inst = Instance;

            int yOffset = (useLabelTextOffset ? inst._DetailYOffsetLabelText : 0);
            var label = new DxLabelControl() { Bounds = new Rectangle(x, y + yOffset, w, inst._DetailYHeightLabel), Text = text };
            label.StyleController = inst._GetLabelStyle(styleType);
            if (wordWrap.HasValue) label.Appearance.TextOptions.WordWrap = wordWrap.Value;
            if (autoSizeMode.HasValue) label.AutoSizeMode = autoSizeMode.Value;
            if (hAlignment.HasValue) label.Appearance.TextOptions.HAlignment = hAlignment.Value;

            if (wordWrap.HasValue || hAlignment.HasValue) label.Appearance.Options.UseTextOptions = true;

            if (visible.HasValue) label.Visible = visible.Value;

            if (parent != null) parent.Controls.Add(label);
            if (shiftY) y = label.Bounds.Bottom + inst._DetailYSpaceLabel;

            return label;
        }
        public static DxTextEdit CreateDxTextEdit(int x, int y, int w, Control parent, EventHandler textChanged = null,
            DevExpress.XtraEditors.Mask.MaskType? maskType = null, string editMask = null, bool? useMaskAsDisplayFormat = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null)
        {
            return CreateDxTextEdit(x, ref y, w, parent, textChanged,
                maskType, editMask, useMaskAsDisplayFormat,
                toolTipTitle, toolTipText,
                visible, readOnly, tabStop, false);
        }
        public static DxTextEdit CreateDxTextEdit(int x, ref int y, int w, Control parent, EventHandler textChanged = null,
            DevExpress.XtraEditors.Mask.MaskType? maskType = null, string editMask = null, bool? useMaskAsDisplayFormat = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var textEdit = new DxTextEdit() { Bounds = new Rectangle(x, y, w, inst._DetailYHeightText) };
            textEdit.StyleController = inst._InputStyle;
            if (visible.HasValue) textEdit.Visible = visible.Value;
            if (readOnly.HasValue) textEdit.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) textEdit.TabStop = tabStop.Value;

            if (maskType.HasValue) textEdit.Properties.Mask.MaskType = maskType.Value;
            if (editMask != null) textEdit.Properties.Mask.EditMask = editMask;
            if (useMaskAsDisplayFormat.HasValue) textEdit.Properties.Mask.UseMaskAsDisplayFormat = useMaskAsDisplayFormat.Value;

            textEdit.SetToolTip(toolTipTitle, toolTipText);

            if (textChanged != null) textEdit.TextChanged += textChanged;
            if (parent != null) parent.Controls.Add(textEdit);
            if (shiftY) y = y + textEdit.Height + inst._DetailYSpaceText;
            return textEdit;
        }
        public static DxMemoEdit CreateDxMemoEdit(Control parent = null, DockStyle? dock = null, int? width = null, int? height = null, EventHandler textChanged = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null)
        {
            int x = 0;
            int y = 0;
            int w = width ?? 100;
            int h = height ?? 100;
            var memoEdit = CreateDxMemoEdit(x, ref y, w, h, parent, textChanged,
                toolTipTitle, toolTipText,
                visible, readOnly, tabStop, false);
            if (dock.HasValue) memoEdit.Dock = dock.Value;
            return memoEdit;
        }
        public static DxMemoEdit CreateDxMemoEdit(int x, int y, int w, int h, Control parent, EventHandler textChanged = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null)
        {
            return CreateDxMemoEdit(x, ref y, w, h, parent, textChanged,
                toolTipTitle, toolTipText,
                visible, readOnly, tabStop, false);
        }
        public static DxMemoEdit CreateDxMemoEdit(int x, ref int y, int w, int h, Control parent, EventHandler textChanged = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var memoEdit = new DxMemoEdit() { Bounds = new Rectangle(x, y, w, h) };
            memoEdit.StyleController = inst._InputStyle;
            if (visible.HasValue) memoEdit.Visible = visible.Value;
            if (readOnly.HasValue) memoEdit.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) memoEdit.TabStop = tabStop.Value;

            memoEdit.SetToolTip(toolTipTitle, toolTipText);

            if (textChanged != null) memoEdit.TextChanged += textChanged;
            if (parent != null) parent.Controls.Add(memoEdit);
            if (shiftY) y = y + memoEdit.Height + inst._DetailYSpaceText;

            return memoEdit;
        }
        public static DxImageComboBoxEdit CreateDxImageComboBox(int x, int y, int w, Control parent, EventHandler selectedIndexChanged = null, string itemsTabbed = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null)
        {
            return CreateDxImageComboBox(x, ref y, w, parent, selectedIndexChanged, itemsTabbed,
                toolTipTitle, toolTipText,
                visible, readOnly, tabStop, false);
        }
        public static DxImageComboBoxEdit CreateDxImageComboBox(int x, ref int y, int w, Control parent, EventHandler selectedIndexChanged = null, string itemsTabbed = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var comboBox = new DxImageComboBoxEdit() { Bounds = new Rectangle(x, y, w, inst._DetailYHeightText) };
            comboBox.StyleController = inst._InputStyle;
            if (visible.HasValue) comboBox.Visible = visible.Value;
            if (readOnly.HasValue) comboBox.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) comboBox.TabStop = tabStop.Value;

            if (itemsTabbed != null)
            {
                string[] items = itemsTabbed.Split('\t');
                for (int i = 0; i < items.Length; i++)
                    comboBox.Properties.Items.Add(items[i], i, 0);
            }

            comboBox.SetToolTip(toolTipTitle, toolTipText);

            if (selectedIndexChanged != null) comboBox.SelectedIndexChanged += selectedIndexChanged;
            if (parent != null) parent.Controls.Add(comboBox);
            if (shiftY) y = y + comboBox.Height + inst._DetailYSpaceText;
            return comboBox;
        }
        public static DxSpinEdit CreateDxSpinEdit(int x, int y, int w, Control parent, EventHandler valueChanged = null,
            decimal? minValue = null, decimal? maxValue = null, decimal? increment = null, string mask = null, DevExpress.XtraEditors.Controls.SpinStyles? spinStyles = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null)
        {
            return CreateDxSpinEdit(x, ref y, w, parent, valueChanged,
                minValue, maxValue, increment, mask, spinStyles,
                toolTipTitle, toolTipText,
                visible, readOnly, tabStop, false);
        }
        public static DxSpinEdit CreateDxSpinEdit(int x, ref int y, int w, Control parent, EventHandler valueChanged = null,
            decimal? minValue = null, decimal? maxValue = null, decimal? increment = null, string mask = null, DevExpress.XtraEditors.Controls.SpinStyles? spinStyles = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var spinEdit = new DxSpinEdit() { Bounds = new Rectangle(x, y, w, inst._DetailYHeightText) };
            spinEdit.StyleController = inst._InputStyle;
            if (visible.HasValue) spinEdit.Visible = visible.Value;
            if (readOnly.HasValue) spinEdit.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) spinEdit.TabStop = tabStop.Value;

            if (minValue.HasValue) spinEdit.Properties.MinValue = minValue.Value;
            if (maxValue.HasValue) spinEdit.Properties.MaxValue = maxValue.Value;
            if (increment.HasValue) spinEdit.Properties.Increment = increment.Value;
            if (mask != null)
            {
                spinEdit.Properties.EditMask = mask;
                spinEdit.Properties.DisplayFormat.FormatString = mask;
                spinEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                spinEdit.Properties.EditFormat.FormatString = mask;
            }
            if (spinStyles.HasValue) spinEdit.Properties.SpinStyle = spinStyles.Value;

            spinEdit.SetToolTip(toolTipTitle, toolTipText);

            if (valueChanged != null) spinEdit.ValueChanged += valueChanged;
            if (parent != null) parent.Controls.Add(spinEdit);
            if (shiftY) y = y + spinEdit.Height + inst._DetailYSpaceText;
            return spinEdit;
        }
        public static DxCheckEdit CreateDxCheckEdit(int x, int y, int w, Control parent, string text, EventHandler checkedChanged = null,
            DevExpress.XtraEditors.Controls.CheckBoxStyle? checkBoxStyle = null, DevExpress.XtraEditors.Controls.BorderStyles? borderStyles = null, HorzAlignment? hAlignment = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null)
        {
            return CreateDxCheckEdit(x, ref y, w, parent, text, checkedChanged,
                checkBoxStyle, borderStyles, hAlignment,
                toolTipTitle, toolTipText,
                visible, readOnly, tabStop, false);
        }
        public static DxCheckEdit CreateDxCheckEdit(int x, ref int y, int w, Control parent, string text, EventHandler checkedChanged = null,
            DevExpress.XtraEditors.Controls.CheckBoxStyle? checkBoxStyle = null, DevExpress.XtraEditors.Controls.BorderStyles? borderStyles = null, HorzAlignment? hAlignment = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var checkEdit = new DxCheckEdit() { Bounds = new Rectangle(x, y, w, inst._DetailYHeightText), Text = text };
            checkEdit.StyleController = inst._InputStyle;
            if (visible.HasValue) checkEdit.Visible = visible.Value;
            if (readOnly.HasValue) checkEdit.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) checkEdit.TabStop = tabStop.Value;

            if (checkBoxStyle.HasValue) checkEdit.Properties.CheckBoxOptions.Style = checkBoxStyle.Value;
            if (hAlignment.HasValue)
            {
                checkEdit.Properties.GlyphAlignment = hAlignment.Value;                       // Kde bude ikonka?
                checkEdit.Properties.Appearance.TextOptions.HAlignment = hAlignment.Value;    // Kde bude text?
                checkEdit.Properties.Appearance.Options.UseTextOptions = true;                // Použít zarovnání textu!
                // checkEdit.Properties.GlyphVAlignment = VertAlignment.Top;              // Vertikální pozice ikony...
            }
            if (borderStyles.HasValue) checkEdit.BorderStyle = borderStyles.Value;

            checkEdit.SetToolTip(toolTipTitle, toolTipText, text);

            if (checkedChanged != null) checkEdit.CheckedChanged += checkedChanged;
            if (parent != null) parent.Controls.Add(checkEdit);
            if (shiftY) y = y + checkEdit.Height + inst._DetailYSpaceText;

            return checkEdit;
        }
        public static DxListBoxControl CreateDxListBox(DockStyle? dock = null, int? width = null, int? height = null, Control parent = null, EventHandler selectedIndexChanged = null,
            bool? multiColumn = null, SelectionMode? selectionMode = null, int? itemHeight = null, int? itemHeightPadding = null,
            KeyActionType? enabledKeyActions = null, DxDragDropActionType? dragDropActions = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? tabStop = null)
        {
            int y = 0;
            int w = width ?? 0;
            int h = height ?? 0;
            return CreateDxListBox(0, ref y, w, h, parent, selectedIndexChanged,
                multiColumn, selectionMode, itemHeight, itemHeightPadding,
                enabledKeyActions, dragDropActions,
                toolTipTitle, toolTipText,
                dock, visible, tabStop, false);
        }
        public static DxListBoxControl CreateDxListBox(int x, int y, int w, int h, Control parent = null, EventHandler selectedIndexChanged = null,
            bool? multiColumn = null, SelectionMode? selectionMode = null, int? itemHeight = null, int? itemHeightPadding = null,
            KeyActionType? enabledKeyActions = null, DxDragDropActionType? dragDropActions = null,
            string toolTipTitle = null, string toolTipText = null,
            DockStyle? dock = null, bool? visible = null, bool? tabStop = null)
        {
            return CreateDxListBox(x, ref y, w, h, parent, selectedIndexChanged,
                multiColumn, selectionMode, itemHeight, itemHeightPadding,
                enabledKeyActions, dragDropActions,
                toolTipTitle, toolTipText,
                dock, visible, tabStop, false);
        }
        public static DxListBoxControl CreateDxListBox(int x, ref int y, int w, int h, Control parent = null, EventHandler selectedIndexChanged = null,
            bool? multiColumn = null, SelectionMode? selectionMode = null, int? itemHeight = null, int? itemHeightPadding = null,
            KeyActionType? enabledKeyActions = null, DxDragDropActionType? dragDropActions = null,
            string toolTipTitle = null, string toolTipText = null,
            DockStyle? dock = null, bool? visible = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            DxListBoxControl listBox = new DxListBoxControl() { Bounds = new Rectangle(x, y, w, h) };
            listBox.StyleController = inst._InputStyle;
            if (dock.HasValue) listBox.Dock = dock.Value;
            if (multiColumn.HasValue) listBox.MultiColumn = multiColumn.Value;
            if (selectionMode.HasValue) listBox.SelectionMode = selectionMode.Value;
            if (itemHeight.HasValue) listBox.ItemHeight = itemHeight.Value;
            if (itemHeightPadding.HasValue) listBox.ItemHeightPadding = itemHeightPadding.Value;

            if (enabledKeyActions.HasValue) listBox.EnabledKeyActions = enabledKeyActions.Value;
            if (dragDropActions.HasValue) listBox.DragDropActions = dragDropActions.Value;

            if (visible.HasValue) listBox.Visible = visible.Value;
            if (tabStop.HasValue) listBox.TabStop = tabStop.Value;

            listBox.SetToolTip(toolTipTitle, toolTipText);

            if (selectedIndexChanged != null) listBox.SelectedIndexChanged += selectedIndexChanged;
            if (parent != null) parent.Controls.Add(listBox);
            if (shiftY) y = y + listBox.Height + inst._DetailYSpaceText;

            return listBox;
        }
        public static DxCheckButton CreateDxCheckButton(int x, int y, int w, int h, Control parent, string text, EventHandler click = null,
            DevExpress.XtraEditors.Controls.PaintStyles? paintStyles = null,
            bool isChecked = false,
            Image image = null, string resourceName = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null, Keys? hotKey = null)
        {
            return CreateDxCheckButton(x, ref y, w, h, parent, text, click,
                paintStyles,
                isChecked,
                image, resourceName,
                toolTipTitle, toolTipText,
                visible, enabled, tabStop, hotKey, false);
        }
        public static DxCheckButton CreateDxCheckButton(int x, ref int y, int w, int h, Control parent, string text, EventHandler click = null,
            DevExpress.XtraEditors.Controls.PaintStyles? paintStyles = null,
            bool isChecked = false,
            Image image = null, string resourceName = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null, Keys? hotKey = null, bool shiftY = false)
        {
            var inst = Instance;

            var checkButton = new DxCheckButton() { Bounds = new Rectangle(x, y, w, h) };
            checkButton.StyleController = inst._InputStyle;
            checkButton.Text = text;
            checkButton.Checked = isChecked;
            if (visible.HasValue) checkButton.Visible = visible.Value;
            if (enabled.HasValue) checkButton.Enabled = enabled.Value;
            if (tabStop.HasValue) checkButton.TabStop = tabStop.Value;
            if (hotKey.HasValue) checkButton.HotKey = hotKey.Value;
            if (paintStyles.HasValue) checkButton.PaintStyle = paintStyles.Value;

            int s = (w < h ? w : h) - 10;
            DxComponent.ApplyImage(checkButton.ImageOptions, resourceName, image, null, new Size(s, s), true);
            checkButton.ImageOptions.ImageToTextAlignment = ImageAlignToText.LeftCenter;
            checkButton.ImageOptions.ImageToTextIndent = 3;
            checkButton.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Default;

            checkButton.SetToolTip(toolTipTitle, toolTipText, text);

            if (click != null) checkButton.Click += click;
            if (parent != null) parent.Controls.Add(checkButton);
            if (shiftY) y = y + checkButton.Height + inst._DetailYSpaceText;

            return checkButton;
        }
        public static DxSimpleButton CreateDxSimpleButton(int x, int y, int w, int h, Control parent, IMenuItem iButton)
        {
            if (iButton is null) return null;
            var button = CreateDxSimpleButton(x, y, w, h, parent, iButton.Text, null,
                DevExpress.XtraEditors.Controls.PaintStyles.Default, iButton.Image, iButton.ImageName,
                iButton.ToolTipTitle, iButton.ToolTipText, iButton.Visible, iButton.Enabled, true, iButton.HotKeys);
            if (iButton.ClickAction != null)
                button.Click += _DxMenuItemClickHandler;
            button.Tag = iButton;
            return button;
        }
        /// <summary>
        /// Handler kliknutí na <see cref="DxSimpleButton"/>, vytvořený z <see cref="IMenuItem"/>, který má za úkol vyvolat akci <see cref="IMenuItem.ClickAction"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void _DxMenuItemClickHandler(object sender, EventArgs args)
        {
            if (sender is Control control && control.Tag is IMenuItem iMenuItem)
            {
                if (iMenuItem != null)
                    iMenuItem.ClickAction(iMenuItem);
            }
        }
        public static DxSimpleButton CreateDxSimpleButton(int x, int y, int w, int h, Control parent, string text, EventHandler click = null,
            DevExpress.XtraEditors.Controls.PaintStyles? paintStyles = null,
            Image image = null, string resourceName = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null, Keys? hotKey = null)
        {
            return CreateDxSimpleButton(x, ref y, w, h, parent, text, click,
                paintStyles,
                image, resourceName,
                toolTipTitle, toolTipText,
                visible, enabled, tabStop, hotKey, false);
        }
        public static DxSimpleButton CreateDxSimpleButton(int x, ref int y, int w, int h, Control parent, string text, EventHandler click = null,
            DevExpress.XtraEditors.Controls.PaintStyles? paintStyles = null,
            Image image = null, string resourceName = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null, Keys? hotKey = null, bool shiftY = false)
        {
            var inst = Instance;

            var simpleButton = new DxSimpleButton() { Bounds = new Rectangle(x, y, w, h) };
            simpleButton.StyleController = inst._InputStyle;
            simpleButton.Text = text;
            if (visible.HasValue) simpleButton.Visible = visible.Value;
            if (enabled.HasValue) simpleButton.Enabled = enabled.Value;
            if (tabStop.HasValue) simpleButton.TabStop = tabStop.Value;
            if (hotKey.HasValue) simpleButton.HotKey = hotKey.Value;
            if (paintStyles.HasValue) simpleButton.PaintStyle = paintStyles.Value;

            DxComponent.ApplyImage(simpleButton.ImageOptions, resourceName, image, null, null, true);
            simpleButton.ImageOptions.ImageToTextAlignment = ImageAlignToText.LeftCenter;
            simpleButton.ImageOptions.ImageToTextIndent = 3;
            simpleButton.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Default;
            simpleButton.PrepareSizeSvgImage(true);

            simpleButton.SetToolTip(toolTipTitle, toolTipText, text);

            if (click != null) simpleButton.Click += click;
            if (parent != null) parent.Controls.Add(simpleButton);
            if (shiftY) y = y + simpleButton.Height + inst._DetailYSpaceText;

            return simpleButton;
        }
        public static DxSimpleButton CreateDxMiniButton(int x, int y, int w, int h, Control parent, EventHandler click = null,
            Image image = null, string resourceName = null,
            Image hotImage = null, string hotResourceName = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null, Keys? hotKey = null, bool? allowFocus = null,
            object tag = null)
        {
            var inst = Instance;

            var miniButton = new DxSimpleButton() { Bounds = new Rectangle(x, y, w, h) };
            miniButton.StyleController = inst._InputStyle;
            miniButton.Text = "";
            miniButton.Tag = tag;

            if (visible.HasValue) miniButton.Visible = visible.Value;
            if (enabled.HasValue) miniButton.Enabled = enabled.Value;
            miniButton.TabStop = tabStop ?? false;
            if (hotKey.HasValue) miniButton.HotKey = hotKey.Value;
            if (allowFocus.HasValue) miniButton.AllowFocus = allowFocus.Value;
            miniButton.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light;

            DxComponent.ApplyImage(miniButton.ImageOptions, resourceName, image, null, new Size(w - 4, h - 4), true);
            miniButton.Padding = new Padding(0);
            miniButton.Margin = new Padding(0);
            miniButton.PrepareSizeSvgImage(true);

            miniButton.SetToolTip(toolTipTitle, toolTipText);

            if (click != null) miniButton.Click += click;
            if (parent != null) parent.Controls.Add(miniButton);

            return miniButton;
        }
        public static DxRibbonStatusBar CreateDxStatusBar(Control parent, DockStyle? dock = null)
        {
            DxRibbonStatusBar statusBar = new DxRibbonStatusBar();
            if (dock.HasValue) statusBar.Dock = dock.Value;
            if (parent != null)
                parent.Controls.Add(statusBar);
            return statusBar;
        }
        public static DxBarStaticItem CreateDxStatusLabel(DevExpress.XtraBars.Ribbon.RibbonStatusBar statusBar = null, string text = null, DevExpress.XtraBars.BarStaticItemSize? autoSize = null,
            bool? visible = null, int? fontSizeDelta = null)
        {
            DxBarStaticItem statusLabel = new DxBarStaticItem();
            if (text != null) statusLabel.Caption = text;
            if (autoSize.HasValue) statusLabel.AutoSize = autoSize.Value;
            if (visible.HasValue) statusLabel.Visibility = (visible.Value ? DevExpress.XtraBars.BarItemVisibility.Always : DevExpress.XtraBars.BarItemVisibility.Never);
            if (fontSizeDelta.HasValue) statusLabel.ItemAppearance.Normal.FontSizeDelta = fontSizeDelta.Value;

            if (statusBar != null) statusBar.ItemLinks.Add(statusLabel);

            return statusLabel;
        }
        public static DxBarButtonItem CreateDxStatusButton(DevExpress.XtraBars.Ribbon.RibbonStatusBar statusBar = null, string text = null,
            int? width = null, int? height = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, int? fontSizeDelta = null,
            DevExpress.XtraBars.ItemClickEventHandler clickHandler = null)
        {
            DxBarButtonItem button = new DxBarButtonItem();
            if (text != null) button.Caption = text;
            button.SetToolTip(toolTipTitle, toolTipText, text);
            if (visible.HasValue) button.Visibility = (visible.Value ? DevExpress.XtraBars.BarItemVisibility.Always : DevExpress.XtraBars.BarItemVisibility.Never);
            if (fontSizeDelta.HasValue)
            {
                button.ItemAppearance.Normal.FontSizeDelta = fontSizeDelta.Value;
                button.ItemAppearance.Hovered.FontSizeDelta = fontSizeDelta.Value;
                button.ItemAppearance.Pressed.FontSizeDelta = fontSizeDelta.Value;
            }
            if (clickHandler != null) button.ItemClick += clickHandler;

            if (statusBar != null) statusBar.ItemLinks.Add(button);

            return button;
        }
        public static DxBarCheckItem CreateDxStatusCheckButton(DevExpress.XtraBars.Ribbon.RibbonStatusBar statusBar = null, string text = null,
            int? width = null, int? height = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, int? fontSizeDelta = null,
            DevExpress.XtraBars.ItemClickEventHandler clickHandler = null)
        {
            DxBarCheckItem checkButton = new DxBarCheckItem();
            if (text != null) checkButton.Caption = text;
            checkButton.SetToolTip(toolTipTitle, toolTipText, text);
            if (visible.HasValue) checkButton.Visibility = (visible.Value ? DevExpress.XtraBars.BarItemVisibility.Always : DevExpress.XtraBars.BarItemVisibility.Never);
            if (fontSizeDelta.HasValue)
            {
                checkButton.ItemAppearance.Normal.FontSizeDelta = fontSizeDelta.Value;
                checkButton.ItemAppearance.Hovered.FontSizeDelta = fontSizeDelta.Value;
                checkButton.ItemAppearance.Pressed.FontSizeDelta = fontSizeDelta.Value;
            }
            if (clickHandler != null) checkButton.ItemClick += clickHandler;

            if (statusBar != null) statusBar.ItemLinks.Add(checkButton);

            return checkButton;
        }
        public static DxDropDownButton CreateDxDropDownButton(int x, int y, int w, int h, Control parent, string text,
            EventHandler click = null, EventHandler<TEventArgs<IMenuItem>> itemClick = null,
            DevExpress.Utils.Menu.IDXDropDownControl dropDownControl = null, string subItemsText = null, IEnumerable<IMenuItem> subItems = null,
            DevExpress.XtraEditors.Controls.PaintStyles? paintStyles = null,
            Image image = null, string resourceName = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null)
        {
            return CreateDxDropDownButton(x, ref y, w, h, parent, text,
                click, itemClick,
                dropDownControl, subItemsText, subItems,
                paintStyles,
                image, resourceName,
                toolTipTitle, toolTipText,
                visible, enabled, tabStop, false);
        }
        public static DxDropDownButton CreateDxDropDownButton(int x, ref int y, int w, int h, Control parent, string text,
            EventHandler click = null, EventHandler<TEventArgs<IMenuItem>> itemClick = null,
            DevExpress.Utils.Menu.IDXDropDownControl dropDownControl = null, string subItemsText = null, IEnumerable<IMenuItem> subItems = null,
            DevExpress.XtraEditors.Controls.PaintStyles? paintStyles = null,
            Image image = null, string resourceName = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var dropDownButton = new DxDropDownButton() { Bounds = new Rectangle(x, y, w, h) };
            dropDownButton.StyleController = inst._InputStyle;
            dropDownButton.Text = text;
            if (visible.HasValue) dropDownButton.Visible = visible.Value;
            if (enabled.HasValue) dropDownButton.Enabled = enabled.Value;
            if (tabStop.HasValue) dropDownButton.TabStop = tabStop.Value;
            if (paintStyles.HasValue) dropDownButton.PaintStyle = paintStyles.Value;

            int s = (w < h ? w : h) - 10;
            DxComponent.ApplyImage(dropDownButton.ImageOptions, resourceName, image, null, new Size(s, s), true);
            dropDownButton.ImageOptions.ImageToTextAlignment = ImageAlignToText.LeftCenter;
            dropDownButton.ImageOptions.ImageToTextIndent = 3;
            dropDownButton.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Default;

            dropDownButton.SetToolTip(toolTipTitle, toolTipText, text);

            dropDownButton.DropDownControl = CreateDxDropDownControl(dropDownControl, subItemsText, subItems, itemClick);

            if (click != null) dropDownButton.Click += click;
            if (parent != null) parent.Controls.Add(dropDownButton);
            if (shiftY) y = y + dropDownButton.Height + inst._DetailYSpaceText;

            return dropDownButton;
        }
        public static DevExpress.Utils.Menu.IDXDropDownControl CreateDxDropDownControl(
            DevExpress.Utils.Menu.IDXDropDownControl dropDownControl = null, string subItemsText = null, IEnumerable<IMenuItem> subItems = null,
            EventHandler<TEventArgs<IMenuItem>> itemClick = null)
        {
            if (dropDownControl != null) return dropDownControl;

            // Tvorba položek menu z textu:
            if (subItems == null && !String.IsNullOrEmpty(subItemsText)) subItems = CreateIMenuItems(subItemsText);
            if (subItems == null) return null;

            // var popupMenu = CreateXBPopupMenu(subItems);
            var popupMenu = CreateDXPopupMenu(subItems, itemClick: itemClick);

            return popupMenu;
        }
        /// <summary>
        /// Metoda vytvoří a vrátí PopupMenu pro dané položky [s daným titulkem, daného typu].
        /// Je možno předat odkaz na metodu, která bude volána po kliknutí na položku menu. Tato metoda dostane argument, jehož Item je položka menu, na kterou se kliklo.
        /// </summary>
        /// <param name="menuItems"></param>
        /// <param name="caption"></param>
        /// <param name="menuType"></param>
        /// <param name="showCheckedAsBold"></param>
        /// <param name="itemClick">Akce volaná po kliknutí na kterýkoli prvek menu. Smí být null. Je možno definovat akci přímo v položce, v <see cref="IMenuItem.ClickAction"/>.</param>
        /// <returns></returns>
        public static DevExpress.Utils.Menu.DXPopupMenu CreateDXPopupMenu(IEnumerable<IMenuItem> menuItems,
            string caption = null, DevExpress.Utils.Menu.MenuViewType? menuType = null, bool showCheckedAsBold = false,
            EventHandler<TEventArgs<IMenuItem>> itemClick = null)
        {
            var dxMenu = new DevExpress.Utils.Menu.DXPopupMenu();
            // dxMenu.MenuViewType = DevExpress.Utils.Menu.MenuViewType.RibbonMiniToolbar;         // sada tlačítek v panelu
            // dxMenu.MenuViewType = DevExpress.Utils.Menu.MenuViewType.Toolbar;                   // sada tlačítek v řádce
            dxMenu.MenuViewType = menuType ?? DevExpress.Utils.Menu.MenuViewType.Menu;             // default = normální menu
            dxMenu.Caption = caption;
            dxMenu.ShowCaption = !String.IsNullOrEmpty(caption);
            dxMenu.ShowItemToolTips = true;
            dxMenu.ItemClick += DxPopupMenu_ItemClick;

            if (menuItems != null)
                menuItems.ForEachExec(i => dxMenu.Items.Add(CreateDXPopupMenuItem(i, showCheckedAsBold)));

            if (itemClick != null)
                dxMenu.Tag = itemClick;

            return dxMenu;
        }
        /// <summary>
        /// Vytvoří a vrátí položky menu z definice menu, rekurzivně včetně subItems
        /// </summary>
        /// <param name="menuItems"></param>
        /// <param name="showCheckedAsBold"></param>
        /// <returns></returns>
        public static List<DevExpress.Utils.Menu.DXMenuItem> CreateDXPopupMenuItems(IEnumerable<IMenuItem> menuItems, bool showCheckedAsBold)
        {
            if (menuItems is null) return null;
            return menuItems.Select(i => CreateDXPopupMenuItem(i, showCheckedAsBold)).ToList();
        }
        /// <summary>
        /// Vytvoří a vrátí položku menu z definice menu, rekurzivně včetně subItems
        /// </summary>
        /// <param name="menuItem"></param>
        /// <param name="showCheckedAsBold"></param>
        /// <returns></returns>
        public static DevExpress.Utils.Menu.DXMenuItem CreateDXPopupMenuItem(IMenuItem menuItem, bool showCheckedAsBold)
        {
            if (menuItem == null) return null;

            DevExpress.Utils.Menu.DXMenuItem dxItem;
            string itemImage = menuItem.ImageName;
            var itemType = menuItem.ItemType;
            if (itemType == MenuItemType.CheckBox)
            {   // Prvek menu s možností CheckBox:
                bool isChecked = menuItem.Checked ?? false;
                var dxCheckItem = new DevExpress.Utils.Menu.DXMenuCheckItem();
                dxCheckItem.Checked = isChecked;
                if (isChecked)
                {   // Je zaškrtnutý:
                    if (showCheckedAsBold)
                        dxCheckItem.Appearance.FontStyleDelta = FontStyle.Bold;
                    if (menuItem.ImageNameChecked != null)
                        itemImage = menuItem.ImageNameChecked;
                }
                else
                {   // Není zaškrtnutý:
                    if (menuItem.ImageNameUnChecked != null)
                        itemImage = menuItem.ImageNameUnChecked;
                }
                dxItem = dxCheckItem;
            }
            else
            {   // Prvek bez CheckBoxu:
                dxItem = new DevExpress.Utils.Menu.DXMenuItem();
            }
            dxItem.BeginGroup = menuItem.ItemIsFirstInGroup;
            dxItem.Enabled = menuItem.Enabled;
            dxItem.Caption = menuItem.Text;
            dxItem.SuperTip = CreateDxSuperTip(menuItem);
            ApplyImage(dxItem.ImageOptions, imageName: itemImage, image: menuItem.Image);
            dxItem.Tag = menuItem;

            // SubMenu:
            if (menuItem.SubItems != null)
                menuItem.SubItems.ForEachExec(i => dxItem.Collection.Add(CreateDXPopupMenuItem(i, showCheckedAsBold)));

            return dxItem;
        }
        /// <summary>
        /// Po kliknutí na prvek menu můžeme provést definovanou akci
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DxPopupMenu_ItemClick(object sender, DevExpress.Utils.Menu.DXMenuItemEventArgs e)
        {
            // Item by v Tagu mělo obsahovat definici IMenuItem:
            if (!(e.Item?.Tag is IMenuItem iMenuItem)) return;

            // IMenuItem může obsahovat vlastní klikací akci:
            if (iMenuItem.ClickAction != null)
                iMenuItem.ClickAction(iMenuItem);

            // sender by měl být DevExpress.Utils.Menu.DXPopupMenu,
            //  jehož Tag může obsahovat eventhandler typu : EventHandler<TEventArgs<IMenuItem>>
            EventHandler<TEventArgs<IMenuItem>> itemClick = (sender as DevExpress.Utils.Menu.DXMenuItem)?.Tag as EventHandler<TEventArgs<IMenuItem>>;
            if (itemClick != null)
            {
                // Předáme řízení handleru akce ItemClick:
                itemClick(sender, new TEventArgs<IMenuItem>(iMenuItem));
            }
        }
        public static DevExpress.XtraBars.PopupMenu CreateXBPopupMenu(IEnumerable<IMenuItem> menuItems)
        {
            // Má řadu nectostí, například: nezhasne když kliknu mimo

            var barManager = DxComponent.DefaultBarManager;
            var xbMenu = new DevExpress.XtraBars.PopupMenu(barManager);
            DevExpress.XtraBars.BarItem[] barItems = CreateXBPopupMenuItems(barManager, menuItems);
            xbMenu.AddItems(barItems);
            xbMenu.DrawMenuSideStrip = DefaultBoolean.True;
            return xbMenu;
        }
        private static DevExpress.XtraBars.BarItem[] CreateXBPopupMenuItems(DevExpress.XtraBars.BarManager barManager, IEnumerable<IMenuItem> menuItems)
        {
            List<DevExpress.XtraBars.BarItem> barItems = new List<DevExpress.XtraBars.BarItem>();
            if (menuItems != null)
            {
                foreach (var menuItem in menuItems)
                {
                    DevExpress.XtraBars.BarButtonItem button = new DevExpress.XtraBars.BarButtonItem(barManager, menuItem.Text)
                    {
                        Name = menuItem.ItemId,
                        Hint = menuItem.ToolTipText,
                        SuperTip = CreateDxSuperTip(menuItem)
                    };
                    barItems.Add(button);
                }
            }
            return barItems.ToArray();
        }
        /// <summary>
        /// Z daného stringu sestaví a vrátí pole <see cref="IMenuItem"/>, z něhož lze např. sestavit SubItems v Ribbonu, nebo DropDownButton.
        /// String má formát: řádky oddělené znakem Alt+Num4; Prvky oddělené znakem Alt+Num7;
        /// Řádky = jednotlivé položky menu
        /// Prvky v pořadí: Text; Tooltip; ImageName; Options
        /// Options může obsahovat znaky: C CheckBox; A Checked; - Začátek grupy; / Disabled
        /// </summary>
        /// <param name="itemsText"></param>
        /// <returns></returns>
        public static List<IMenuItem> CreateIMenuItems(string itemsText)
        {
            List<IMenuItem> menuItems = new List<IMenuItem>();

            if (itemsText != null)
            {   // Ukázka: "Nabídka•Toto je nabídka funkcí•image.svg•CD♦Nabídka•Toto je nabídka funkcí•image.svg•CD♦Nabídka•Toto je nabídka funkcí•image.svg•CD♦";
                char sepLines = MenuItemsSeparatorLines;            // Klávesa ALT a Num 4
                char sepItems = MenuItemsSeparatorItems;            // Klávesa ALT a Num 7
                char codChBox = MenuItemsCodeCheckBox;
                char codChecked = MenuItemsCodeChecked;
                char codGroup = MenuItemsCodeBeginGroup;
                char codDisable = MenuItemsCodeDisable;
                var lines = itemsText.Split(sepLines);
                int id = 0;
                foreach (var line in lines)
                {
                    var items = line.Split(sepItems);
                    int count = items.Length;
                    string itemId = "Item" + (id++).ToString();
                    string text = (count > 0 ? items[0].Trim() : "");
                    string toolTip = (count > 1 ? items[1].Trim() : "");
                    string imageName = (count > 2 ? items[2].Trim() : "");
                    string options = (count > 3 ? items[3].Trim().ToUpper() : "");
                    if (!String.IsNullOrEmpty(text))
                    {
                        DataMenuItem menuItem = new DataMenuItem() { ItemId = itemId, Text = text, ToolTipText = toolTip, ToolTipTitle = text, ImageName = imageName };
                        menuItem.ItemType = (options.Contains(codChBox) ? MenuItemType.CheckBox : MenuItemType.MenuItem);
                        if (menuItem.ItemType == MenuItemType.CheckBox) menuItem.Checked = options.Contains(codChecked);
                        if (options.Contains(codGroup)) menuItem.ItemIsFirstInGroup = true;
                        if (options.Contains(codDisable)) menuItem.Enabled = false;
                        menuItems.Add(menuItem);
                    }
                }
            }

            return menuItems;
        }
        /// <summary>
        /// Deklarace prvků menu : Oddělovač řádků v textu. '♦'
        /// Lze jej zadat jako Alt + Num 4.
        /// <para/>
        /// Používá se v metodách 
        /// <see cref="CreateIMenuItems(string)"/>, 
        /// <see cref="CreateDxDropDownControl(DevExpress.Utils.Menu.IDXDropDownControl, string, IEnumerable{IMenuItem}, EventHandler{TEventArgs{IMenuItem}})"/>
        /// <see cref="CreateDxDropDownButton(int, int, int, int, Control, string, EventHandler, EventHandler{TEventArgs{IMenuItem}}, DevExpress.Utils.Menu.IDXDropDownControl, string, IEnumerable{IMenuItem}, DevExpress.XtraEditors.Controls.PaintStyles?, Image, string, string, string, bool?, bool?, bool?)"/>
        /// jako parametr 'subItemsText'
        /// </summary>
        public static char MenuItemsSeparatorLines { get { return '♦'; } }
        /// <summary>
        /// Oddělovač prvků v jednom řádku. '•'
        /// Lze jej zadat jako Alt + Num 7.
        /// <para/>
        /// Používá se v metodách 
        /// <see cref="CreateIMenuItems(string)"/>, 
        /// <see cref="CreateDxDropDownControl(DevExpress.Utils.Menu.IDXDropDownControl, string, IEnumerable{IMenuItem}, EventHandler{TEventArgs{IMenuItem}})"/>
        /// <see cref="CreateDxDropDownButton(int, int, int, int, Control, string, EventHandler, EventHandler{TEventArgs{IMenuItem}}, DevExpress.Utils.Menu.IDXDropDownControl, string, IEnumerable{IMenuItem}, DevExpress.XtraEditors.Controls.PaintStyles?, Image, string, string, string, bool?, bool?, bool?)"/>
        /// jako parametr 'subItemsText'
        /// </summary>
        public static char MenuItemsSeparatorItems { get { return '•'; } }
        /// <summary>
        /// Značka pro typ prvku CheckBox = 'C'.
        /// <para/>
        /// Používá se v metodách 
        /// <see cref="CreateIMenuItems(string)"/>, 
        /// <see cref="CreateDxDropDownControl(DevExpress.Utils.Menu.IDXDropDownControl, string, IEnumerable{IMenuItem}, EventHandler{TEventArgs{IMenuItem}})"/>
        /// <see cref="CreateDxDropDownButton(int, int, int, int, Control, string, EventHandler, EventHandler{TEventArgs{IMenuItem}}, DevExpress.Utils.Menu.IDXDropDownControl, string, IEnumerable{IMenuItem}, DevExpress.XtraEditors.Controls.PaintStyles?, Image, string, string, string, bool?, bool?, bool?)"/>
        /// jako parametr 'subItemsText'
        /// </summary>
        public static char MenuItemsCodeCheckBox { get { return 'C'; } }
        /// <summary>
        /// Značka pro hodnotu Checked v prvku CheckBox = 'A'.
        /// <para/>
        /// Používá se v metodách 
        /// <see cref="CreateIMenuItems(string)"/>, 
        /// <see cref="CreateDxDropDownControl(DevExpress.Utils.Menu.IDXDropDownControl, string, IEnumerable{IMenuItem}, EventHandler{TEventArgs{IMenuItem}})"/>
        /// <see cref="CreateDxDropDownButton(int, int, int, int, Control, string, EventHandler, EventHandler{TEventArgs{IMenuItem}}, DevExpress.Utils.Menu.IDXDropDownControl, string, IEnumerable{IMenuItem}, DevExpress.XtraEditors.Controls.PaintStyles?, Image, string, string, string, bool?, bool?, bool?)"/>
        /// jako parametr 'subItemsText'
        /// </summary>
        public static char MenuItemsCodeChecked { get { return 'A'; } }
        /// <summary>
        /// Značka pro zahájení nové skupiny (nad tímto prvkem bude vodorovný oddělovač) = '-'.
        /// <para/>
        /// Používá se v metodách 
        /// <see cref="CreateIMenuItems(string)"/>, 
        /// <see cref="CreateDxDropDownControl(DevExpress.Utils.Menu.IDXDropDownControl, string, IEnumerable{IMenuItem}, EventHandler{TEventArgs{IMenuItem}})"/>
        /// <see cref="CreateDxDropDownButton(int, int, int, int, Control, string, EventHandler, EventHandler{TEventArgs{IMenuItem}}, DevExpress.Utils.Menu.IDXDropDownControl, string, IEnumerable{IMenuItem}, DevExpress.XtraEditors.Controls.PaintStyles?, Image, string, string, string, bool?, bool?, bool?)"/>
        /// jako parametr 'subItemsText'
        /// </summary>
        public static char MenuItemsCodeBeginGroup { get { return '_'; } }
        /// <summary>
        /// Značka pro Disable na položce menu = '/'
        /// <para/>
        /// Používá se v metodách 
        /// <see cref="CreateIMenuItems(string)"/>, 
        /// <see cref="CreateDxDropDownControl(DevExpress.Utils.Menu.IDXDropDownControl, string, IEnumerable{IMenuItem}, EventHandler{TEventArgs{IMenuItem}})"/>
        /// <see cref="CreateDxDropDownButton(int, int, int, int, Control, string, EventHandler, EventHandler{TEventArgs{IMenuItem}}, DevExpress.Utils.Menu.IDXDropDownControl, string, IEnumerable{IMenuItem}, DevExpress.XtraEditors.Controls.PaintStyles?, Image, string, string, string, bool?, bool?, bool?)"/>
        /// jako parametr 'subItemsText'
        /// </summary>
        public static char MenuItemsCodeDisable { get { return '/'; } }
        /// <summary>
        /// Vytvoří a vrátí standardní SuperToolTip pro daný titulek a text
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <param name="defaultTitle"></param>
        /// <param name="toolTipIcon"></param>
        /// <returns></returns>
        public static DxSuperToolTip CreateDxSuperTip(string title, string text, string defaultTitle = null, string toolTipIcon = null)
        {
            return DxSuperToolTip.CreateDxSuperTip(title, text, defaultTitle, toolTipIcon);
        }
        /// <summary>
        /// Vytvoří a vrátí SuperTooltip
        /// </summary>
        /// <param name="textItem"></param>
        /// <returns></returns>
        public static SuperToolTip CreateDxSuperTip(IMenuItem textItem)
        {
            return DxSuperToolTip.CreateDxSuperTip(textItem);
        }
        /// <summary>
        /// Vytvoří a vrátí SuperTooltip
        /// </summary>
        /// <param name="toolTipItem"></param>
        /// <returns></returns>
        public static SuperToolTip CreateDxSuperTip(IToolTipItem toolTipItem)
        {
            return DxSuperToolTip.CreateDxSuperTip(toolTipItem);
        }
        #endregion
        #region TryRun
        /// <summary>
        /// Provede danou akci v bloku try - catch, volitelně s potlačením chybové hlášky
        /// </summary>
        /// <param name="action"></param>
        /// <param name="hideException"></param>
        public static void TryRun(Action action, bool hideException = false)
        {
            try { action(); }
            catch (Exception exc)
            {
                if (!hideException)
                {
                    DialogForm.ShowDialog(DialogArgs.CreateForException(exc));
                }
            }
        }
        #endregion
        #region LogText - logování
        /// <summary>
        /// Obsahuje true, pokud log je aktivní.
        /// Není dobré to testovat před voláním běžných metod, ty to testují uvnitř.
        /// Je vhodné to testovat jen tehdy, když bychom pro Log měli v aplikaci chystat složitá data, protože v případě neaktivního logu to je zbytečná práce.
        /// <para/>
        /// Pokud je Log neaktivní, pak <see cref="LogText"/> je null, <see cref="LogTimeCurrent"/> je vždy 0
        /// </summary>
        public static bool LogActive { get { return Instance._LogActive; } set { Instance._LogActive = value; } }
        /// <summary>
        /// Aktuální obsah Log textu.
        /// Lze zaregistrovat eventhandler <see cref="LogTextChanged"/> pro hlídání všech změn
        /// </summary>
        public static string LogText { get { return Instance._LogText; } }
        /// <summary>
        /// Smaže dosavadní obsah logu
        /// </summary>
        public static void LogClear() { Instance._LogClear(); }
        /// <summary>
        /// Událost po každé změně obsahu textu <see cref="LogText"/>
        /// </summary>
        public static event EventHandler LogTextChanged { add { Instance._LogTextChanged += value; } remove { Instance._LogTextChanged -= value; } }
        /// <summary>
        /// Obsahuje přesný aktuální čas jako Int64. 
        /// Lze ho následně použít jako parametr 'long startTime' v metodě <see cref="LogAddLineTime(string, long?)"/> pro zápis uplynulého času.
        /// </summary>
        /// <returns></returns>
        public static long LogTimeCurrent { get { return Instance._LogTimeCurrent; } }
        /// <summary>
        /// Vrátí dobu času, která uplynula do teď od času <paramref name="startTime"/>, v jednotkách dle <paramref name="logTokenTime"/>, default v mikrosekundách.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="logTokenTime"></param>
        /// <returns></returns>
        public static decimal LogGetTimeElapsed(long startTime, string logTokenTime = null) { return Instance._LogGetTimeElapsed(startTime, logTokenTime); }
        /// <summary>
        /// Přidá titulek (mezera + daný text ohraničený znaky ===)
        /// </summary>
        /// <param name="title"></param>
        /// <param name="liner"></param>
        public static void LogAddTitle(string title, char? liner = null) { Instance._LogAddTitle(title, liner); }
        /// <summary>
        /// Přidá dodaný řádek do logu. Umožní do textu vložit uplynulý čas:
        /// na místo tokenu z property <see cref="DxComponent.LogTokenTimeSec"/> vloží počet uplynulých sekund ve formě "25,651 sec";
        /// na místo tokenu z property <see cref="DxComponent.LogTokenTimeMilisec"/> vloží počet uplynulých milisekund ve formě "25,651 milisec";
        /// na místo tokenu z property <see cref="DxComponent.LogTokenTimeMicrosec"/> vloží počet uplynulých mikrosekund ve formě "98 354,25 mikrosec";
        /// </summary>
        /// <param name="line"></param>
        /// <param name="startTime"></param>
        public static void LogAddLineTime(string line, long? startTime) { Instance._LogAddLineTime(line, startTime); }
        /// <summary>
        /// Přidá dodaný řádek do logu. 
        /// Nepřidává se nic víc.
        /// </summary>
        /// <param name="line"></param>
        public static void LogAddLine(string line) { Instance._LogAddLine(line, false); }
        /// <summary>
        /// Poslední řádek v logu
        /// </summary>
        public static string LogLastLine { get { return Instance._LogLastLine; } }
        /// <summary>
        /// Token, který se očekává v textu v metodě <see cref="LogAddLineTime(string, long?)"/>, za který se dosaví uplynulý čas v sekundách
        /// </summary>
        public static string LogTokenTimeSec { get { return _LogTokenTimeSec; } }
        private const string _LogTokenTimeSec = "{SEC}";
        /// <summary>
        /// Token, který se očekává v textu v metodě <see cref="LogAddLineTime(string, long?)"/>, za který se dosaví uplynulý čas v milisekundách
        /// </summary>
        public static string LogTokenTimeMilisec { get { return _LogTokenTimeMilisec; } }
        private const string _LogTokenTimeMilisec = "{MILISEC}";
        /// <summary>
        /// Token, který se očekává v textu v metodě <see cref="LogAddLineTime(string, long?)"/>, za který se dosaví uplynulý čas v mikrosekundách
        /// </summary>
        public static string LogTokenTimeMicrosec { get { return _LogTokenTimeMicrosec; } }
        private const string _LogTokenTimeMicrosec = "{MICROSEC}";
        /// <summary>
        /// Zaloguje výjimku
        /// </summary>
        /// <param name="exc"></param>
        public static void LogAddException(Exception exc) { Instance._LogAddLine(exc.Message, false); }
        /// <summary>
        /// Init systému Log
        /// </summary>
        private void _InitLog()
        {
            _LogActive = System.Diagnostics.Debugger.IsAttached;
        }
        /// <summary>
        /// Aktivita logu
        /// </summary>
        private bool _LogActive
        {
            get { return __LogActive; }
            set
            {
                if (value && (_LogWatch == null || _LogSB == null))
                {   // Inicializace:
                    _LogWatch = new System.Diagnostics.Stopwatch();
                    _LogFrequencyLong = System.Diagnostics.Stopwatch.Frequency;
                    _LogFrequency = _LogFrequencyLong;
                    _LogTimeSpanForEmptyRow = System.Diagnostics.Stopwatch.Frequency / 10L;   // Pokud mezi dvěma zápisy do logu bude časová pauza 1/10 sekundy a víc, vložím EmptyRow
                    _LogSB = new StringBuilder();
                }
                if (value && !__LogActive)
                {   // Restart:
                    _LogWatch.Start();
                    _LogStartTicks = _LogWatch.ElapsedTicks;
                    _LogLastWriteTicks = _LogStartTicks;
                }
                if (!value && __LogActive)
                {   // Stop:
                    if (_LogWatch != null)
                        _LogWatch.Stop();
                    if (_LogSB != null)
                        _LogSB.Clear();
                    // Instance nechávám existovat, ale klidné a prázdné.
                }

                __LogActive = value;
            }
        }
        /// <summary>
        /// Aktuální obsah Log textu.
        /// </summary>
        private string _LogText
        {
            get
            {
                if (!_LogActive) return null;

                string text = "";
                lock (_LogSB)
                    text = _LogSB.ToString();
                return text;
            }
        }
        /// <summary>
        /// Smaže dosavadní obsah logu
        /// </summary>
        private void _LogClear()
        {
            if (!_LogActive) return;

            lock (_LogSB)
            {
                _LogSB.Clear();
                _LogLastLine = null;
            }

            RunLogTextChanged();
        }
        /// <summary>
        /// Obsahuje aktuální čas jako ElapsedTicks
        /// </summary>
        /// <returns></returns>
        private long _LogTimeCurrent { get { return (_LogActive ? _LogWatch.ElapsedTicks : 0L); } }
        /// <summary>
        /// Přidá titulek (mezera + daný text ohraničený znaky ===)
        /// </summary>
        /// <param name="title"></param>
        /// <param name="liner"></param>
        private void _LogAddTitle(string title, char? liner)
        {
            if (!_LogActive) return;

            string margins = "".PadRight(15, (liner ?? '='));
            string line = $"{margins}  {title}  {margins}";
            _LogAddLine(line, true);
        }
        /// <summary>
        /// Vrátí dobu času, která uplynula do teď od času <paramref name="startTime"/>, v jednotkách dle <paramref name="logTokenTime"/>, default v mikrosekundách.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="logTokenTime"></param>
        /// <returns></returns>
        private decimal _LogGetTimeElapsed(long startTime, string logTokenTime)
        {
            if (!_LogActive) return 0m;

            long nowTime = _LogWatch.ElapsedTicks;
            decimal seconds = ((decimal)(nowTime - startTime)) / _LogFrequency;     // Počet sekund
            string token = logTokenTime ?? LogTokenTimeMicrosec;
            switch (token)
            {
                case _LogTokenTimeSec:
                    return Math.Round(seconds, 3);
                case _LogTokenTimeMilisec:
                    return Math.Round((seconds * 1000m), 3);
                default:
                    return Math.Round((seconds * 1000000m), 3);
            }
        }
        /// <summary>
        /// Přidá dodaný řádek do logu. Umožní do textu vložit uplynulý čas:
        /// na místo tokenu {S} vloží počet uplynulých sekund ve formě "25,651 sec";
        /// na místo tokenu {MS} vloží počet uplynulých milisekund ve formě "25,651 milisec";
        /// na místo tokenu {US} vloží počet uplynulých mikrosekund ve formě "98 354,25 microsec";
        /// </summary>
        /// <param name="line"></param>
        /// <param name="startTime"></param>
        private void _LogAddLineTime(string line, long? startTime)
        {
            if (!_LogActive) return;

            long nowTime = _LogWatch.ElapsedTicks;
            decimal seconds = (startTime.HasValue ? ((decimal)(nowTime - startTime.Value)) / _LogFrequency : 0m);     // Počet sekund
            if (line.Contains(LogTokenTimeSec))
            {
                string info = Math.Round(seconds, 3).ToString("### ### ### ##0.000").Trim() + " sec";
                line = line.Replace(LogTokenTimeSec, info);
            }
            if (line.Contains(LogTokenTimeMilisec))
            {
                string info = Math.Round((seconds * 1000m), 3).ToString("### ### ### ##0.000").Trim() + " milisec";
                line = line.Replace(LogTokenTimeMilisec, info);
            }
            if (line.Contains(LogTokenTimeMicrosec))
            {
                string info = Math.Round((seconds * 1000000m), 3).ToString("### ### ### ##0.000").Trim() + " microsec";
                line = line.Replace(LogTokenTimeMicrosec, info);
            }
            _LogAddLine(line, false, startTime, nowTime);
        }
        /// <summary>
        /// Přidá daný text jako další řádek
        /// </summary>
        /// <param name="line"></param>
        /// <param name="forceEmptyRow"></param>
        /// <param name="startTime"></param>
        /// <param name="nowTime"></param>
        private void _LogAddLine(string line, bool forceEmptyRow, long? startTime = null, long? nowTime = null)
        {
            if (!_LogActive) return;

            // | mikrosekund od startu | mikrosekund od posledně | mikrosekund od starTime | Thread | ...
            long nowTick = nowTime ?? _LogWatch.ElapsedTicks;
            string totalUs = _LogGetMicroseconds(_LogStartTicks, nowTick).ToString();              // mikrosekund od startu
            string stepUs = _LogGetMicroseconds(_LogLastWriteTicks, nowTick).ToString();           // mikrosekund od posledního logu
            string timeUs = (startTime.HasValue ? _LogGetMicroseconds(startTime.Value, nowTick).ToString() : "");    // mikrosekund od daného času
            string thread = System.Threading.Thread.CurrentThread.Name;
            string tab = "\t";
            string logLine = totalUs + tab + stepUs + tab + timeUs + tab + thread + tab + line;
            lock (_LogSB)
            {
                if (forceEmptyRow || ((nowTick - _LogLastWriteTicks) > _LogTimeSpanForEmptyRow))
                    _LogSB.AppendLine();
                _LogSB.AppendLine(logLine);
                _LogLastLine = logLine;
            }
            _LogLastWriteTicks = _LogWatch.ElapsedTicks;
            RunLogTextChanged();
        }
        /// <summary>
        /// Poslední řádek v logu
        /// </summary>
        private string _LogLastLine = null;
        /// <summary>
        /// Vrací počet mikrosekund od času <paramref name="start"/> do <paramref name="stop"/>
        /// </summary>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        private long _LogGetMicroseconds(long start, long stop)
        {
            return (1000000L * (stop - start)) / _LogFrequencyLong;
        }
        /// <summary>
        /// Vyvolá event <see cref="_LogTextChanged"/>.
        /// </summary>
        private void RunLogTextChanged()
        {
            // Tady nemá smysl řešit standardní metodu : protected virtual void OnLogTextChanged(), protože tahle třída je sealed a singleton
            _LogTextChanged?.Invoke(null, EventArgs.Empty);
        }
        private bool __LogActive;
        private System.Diagnostics.Stopwatch _LogWatch;
        private decimal _LogFrequency;
        private long _LogFrequencyLong;
        private StringBuilder _LogSB;
        private event EventHandler _LogTextChanged;
        private long _LogStartTicks;
        private long _LogLastWriteTicks;
        private long _LogTimeSpanForEmptyRow;
        #endregion
        #region Draw metody
        /// <summary>
        /// Vykreslí linku, která může být i gradientem kde <paramref name="color2"/> je barva u pravého okraje
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        public static void PaintDrawLine(Graphics graphics, Rectangle bounds, Color color1, Color? color2)
        {
            if (!color2.HasValue)
            {
                graphics.FillRectangle(Instance._GetSolidBrush(color1), bounds);
            }
            else
            {
                using (var brush = PaintCreateBrushForGradient(bounds, color1, color2.Value, RectangleSide.Right))
                {
                    graphics.FillRectangle(brush, bounds);
                }
            }
        }

        /// <summary>
        /// Vrátí new instanci <see cref="LinearGradientBrush"/> pro dané zadání. 
        /// Interně řeší problém WinForms, kdy pro určité orientace / úhly gradientu dochází k posunu prostoru.
        /// <para/>
        /// Může vrátit SolidBrush, pokud <paramref name="effectType"/> je <see cref="Gradient3DEffectType.None"/>, i pak ale vrátí new instanci, kterou je nutno Disposovat.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="orientation"></param>
        /// <param name="effectType"></param>
        /// <returns></returns>
        public static Brush PaintCreateBrushForGradient(Rectangle bounds, Color color, Orientation orientation, Gradient3DEffectType effectType)
        {
            float effectRatio = (effectType == Gradient3DEffectType.Inset ? -0.25f :
                                (effectType == Gradient3DEffectType.Outward ? +0.25f : 0f));
            return PaintCreateBrushForGradient(bounds, color, orientation, effectRatio);
        }
        /// <summary>
        /// Vrátí new instanci <see cref="LinearGradientBrush"/> pro dané zadání. 
        /// Interně řeší problém WinForms, kdy pro určité orientace / úhly gradientu dochází k posunu prostoru.
        /// <para/>
        /// Může vrátit SolidBrush, pokud <paramref name="effectRatio"/> je 0f, i pak ale vrátí new instanci, kterou je nutno Disposovat.
        /// </summary>
        /// <param name="bounds">Souřadnice "položené trubky", na kterou chystáme nátěr</param>
        /// <param name="color">Základní barva uprostřed "položené trubky"</param>
        /// <param name="orientation">Směr "položené trubky" na které předvádíme 3D efekt</param>
        /// <param name="effectRatio">Hodnota 3D efektu: 
        /// kladná vytváří "nahoru zvednutý povrch" (tj. color1 = nahoře/vlevo je světlejší, color2 = dole/vpravo je tmavší),
        /// kdežto záporná hodnota vytváří "dolů promáčknutý povrch".
        /// Hodnota 1.00 vytvoří bílou a černou barvu, hodnota 0.10f vytvoří lehký 3D efekt, 0.50f poměrně silný efekt.
        /// </param>
        /// <returns></returns>
        public static Brush PaintCreateBrushForGradient(Rectangle bounds, Color color, Orientation orientation, float effectRatio)
        {
            if (effectRatio == 0f) return new SolidBrush(color);
            CreateColor3DEffect(color, effectRatio, out Color colorBegin, out Color colorEnd);
            GradientStyleType? gradientStyle = (orientation == Orientation.Horizontal ? GradientStyleType.Downward : GradientStyleType.ToRight);
            return PaintCreateBrushForGradient(bounds, colorBegin, colorEnd, gradientStyle);
        }
        /// <summary>
        /// Metoda vygeneruje pár barev out color1 a color2 pro danou barvu výchozí a daný 3D efekt.
        /// Metoda vrací true = barvy pro 3D efekt jsou vytvořeny / false = daná hodnota efektu není 3D, prostor se má vybarvit plnou barvou.
        /// Barvy světla a stínu se přebírají z hodnot Skin.Control.Effect3DLight a Skin.Control.Effect3DDark.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="effectRatio">Hodnota 3D efektu: 
        /// kladná vytváří "nahoru zvednutý povrch" (tj. color1 = nahoře/vlevo je světlejší, color2 = dole/vpravo je tmavší),
        /// kdežto záporná hodnota vytváří "dolů promáčknutý povrch".
        /// Hodnota 1.00 vytvoří bílou a černou barvu, hodnota 0.10f vytvoří lehký 3D efekt, 0.50f poměrně silný efekt.
        /// </param>
        /// <param name="colorBegin">Barva nahoře/vlevo</param>
        /// <param name="colorEnd">Barva dole/vpravo</param>
        /// <returns></returns>
        public static bool CreateColor3DEffect(Color color, float? effectRatio, out Color colorBegin, out Color colorEnd)
        {
            colorBegin = color;
            colorEnd = color;
            if (!effectRatio.HasValue || effectRatio.Value == 0f) return false;

            float ratio = effectRatio.Value;
            int l = 250;
            int d = 16;
            if (ratio > 0f)
            {   // Nahoru = barva 1 je světlejší, barva 2 je tmavší:
                colorBegin = color.Morph(Color.FromArgb(l, l, l), ratio);
                colorEnd = color.Morph(Color.FromArgb(d, d, d), ratio);
            }
            else
            {   // Dolů = barva 1 je tmavší, barva 2 je světlejší:
                ratio = -ratio;
                colorBegin = color.Morph(Color.FromArgb(d, d, d), ratio);
                colorEnd = color.Morph(Color.FromArgb(l, l, l), ratio);
            }
            return true;
        }
        /// <summary>
        /// Vrátí new instanci <see cref="LinearGradientBrush"/> pro dané zadání. 
        /// Interně řeší problém WinForms, kdy pro určité orientace / úhly gradientu dochází k posunu prostoru.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <param name="targetSide"></param>
        /// <para/>
        /// Může vrátit null, pokud <paramref name="color1"/> nemá hodnotu.
        /// Může vrátit SolidBrush, pokud <paramref name="color2"/> nemá hodnotu.
        /// <returns></returns>
        public static Brush PaintCreateBrushForGradient(Rectangle bounds, Color? color1, Color? color2, RectangleSide targetSide)
        {
            GradientStyleType gradientStyle = ConvertRectangleSideToGradientStyle(targetSide);
            return PaintCreateBrushForGradient(bounds, color1, color2, gradientStyle);
        }
        /// <summary>
        /// Vrátí new instanci <see cref="LinearGradientBrush"/> pro dané zadání. 
        /// Interně řeší problém WinForms, kdy pro určité orientace / úhly gradientu dochází k posunu prostoru.
        /// <para/>
        /// Může vrátit null, pokud <paramref name="color1"/> nemá hodnotu.
        /// Může vrátit SolidBrush, pokud <paramref name="color2"/> nemá hodnotu nebo <paramref name="gradientStyle"/> je None.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <param name="gradientStyle"></param>
        /// <returns></returns>
        public static Brush PaintCreateBrushForGradient(Rectangle bounds, Color? color1, Color? color2, GradientStyleType? gradientStyle)
        {
            if (!color1.HasValue) return null;
            if (!color2.HasValue || color2.Value == color1.Value || !gradientStyle.HasValue || gradientStyle.Value == GradientStyleType.None) return new SolidBrush(color1.Value);

            switch (gradientStyle)
            {
                case GradientStyleType.Downward:
                    bounds = bounds.Enlarge(0, 1, 0, 1);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds, color1.Value, color2.Value, LinearGradientMode.Vertical);
                case GradientStyleType.ToLeft:
                    bounds = bounds.Enlarge(1, 0, 1, 0);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds, color2.Value, color1.Value, LinearGradientMode.Horizontal);
                case GradientStyleType.Upward:
                    bounds = bounds.Enlarge(0, 1, 0, 1);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds, color2.Value, color1.Value, LinearGradientMode.Vertical);
                case GradientStyleType.ToRight:
                    bounds = bounds.Enlarge(1, 0, 1, 0);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds, color1.Value, color2.Value, LinearGradientMode.Horizontal);
            }
            return new SolidBrush(color1.Value);
        }
        /// <summary>
        /// Konvertuje <see cref="RectangleSide"/> na <see cref="GradientStyleType"/>
        /// </summary>
        /// <param name="targetSide"></param>
        /// <returns></returns>
        private static GradientStyleType ConvertRectangleSideToGradientStyle(RectangleSide targetSide)
        {
            switch (targetSide)
            {
                case RectangleSide.Bottom:
                case RectangleSide.BottomCenter: return GradientStyleType.Downward;
                case RectangleSide.Left:
                case RectangleSide.MiddleLeft: return GradientStyleType.ToLeft;
                case RectangleSide.Top:
                case RectangleSide.TopCenter: return GradientStyleType.Upward;
                case RectangleSide.Right:
                case RectangleSide.MiddleRight: return GradientStyleType.ToRight;
                default: return GradientStyleType.None;
            }
        }
        /// <summary>
        /// Okamžitě vrací SolidBrush pro kreslení danou barvou. 
        /// Vrácený objekt Nesmí být Disposován!
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static SolidBrush PaintGetSolidBrush(Color color) { return Instance._GetSolidBrush(color); }
        /// <summary>
        /// Okamžitě vrací SolidBrush pro kreslení danou barvou.
        /// Explicitně je dána hodnota alpha kanálu: 0 = zcela průhledná barva ... 255 = zcela plná barva.
        /// Vrácený objekt Nesmí být Disposován!
        /// </summary>
        /// <param name="color"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        public static SolidBrush PaintGetSolidBrush(Color color, int alpha) { return Instance._GetSolidBrush(Color.FromArgb(alpha, color)); }
        /// <summary>
        /// Okamžitě vrací Pen pro kreslení danou barvou. 
        /// Vrácený objekt Nesmí být Disposován!
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen PaintGetPen(Color color) { return Instance._GetPen(color); }
        /// <summary>
        /// Okamžitě vrací Pen pro kreslení danou barvou. 
        /// Explicitně je dána hodnota alpha kanálu: 0 = zcela průhledná barva ... 255 = zcela plná barva
        /// Vrácený objekt Nesmí být Disposován!
        /// </summary>
        /// <param name="color"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        public static Pen PaintGetPen(Color color, int alpha) { return Instance._GetPen(Color.FromArgb(alpha, color)); }
        /// <summary>
        /// Okamžitě vrací SolidBrush dané barvy.
        /// Vrácený objekt Nesmí být Disposován!
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private SolidBrush _GetSolidBrush(Color color)
        {
            _SolidBrush.Color = color;
            return _SolidBrush;
        }
        /// <summary>
        /// Okamžitě vrací Pen dané barvy.
        /// Vrácený objekt Nesmí být Disposován!
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private Pen _GetPen(Color color)
        {
            _Pen.Color = color;
            return _Pen;
        }
        /// <summary>
        /// Inicializace objektů pro kreslení
        /// </summary>
        private void _InitDrawing()
        {
            _SolidBrush = new SolidBrush(Color.White);
            _Pen = new Pen(Color.Black);
        }
        private SolidBrush _SolidBrush;
        private Pen _Pen;
        #endregion
        #region Lokalizace - můstek do systému
        /// <summary>
        /// Vrátí lokalizovaný string
        /// </summary>
        /// <param name="messageCode"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string Localize(MsgCode messageCode, params object[] parameters)
        {
            return SystemAdapter.GetMessage(messageCode, parameters);
        }
        /// <summary>
        /// Vrátí lokalizovaný string.
        /// Pokud tato metoda nevrátí string, pak bude vrácen daný default.
        /// </summary>
        /// <param name="messageCode"></param>
        /// <param name="messageText"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string LocalizeDef(MsgCode messageCode, string messageText, params object[] parameters)
        {
            string message = SystemAdapter.GetMessage(messageCode);
            if (message == null) message = messageText;
            if (parameters != null && parameters.Length > 0) message = String.Format(message, parameters);
            return message;
        }
        #endregion
        #region MessageBox
        /// <summary>
        /// Zobrazí informaci
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public static void ShowMessageInfo(string text, string title = null) { _ShowMessage(title, text, MessageBoxButtons.OK, DialogSystemIcon.Information); }
        /// <summary>
        /// Zobrazí varování
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public static void ShowMessageWarning(string text, string title = null) { _ShowMessage(title, text, MessageBoxButtons.OK, DialogSystemIcon.Exclamation); }
        /// <summary>
        /// Zobrazí chybu
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public static void ShowMessageError(string text, string title = null) { _ShowMessage(title, text, MessageBoxButtons.OK, DialogSystemIcon.Error); }
        /// <summary>
        /// Zobrazí exception
        /// </summary>
        /// <param name="exc"></param>
        /// <param name="preText"></param>
        /// <param name="title"></param>
        public static void ShowMessageException(Exception exc, string preText = null, string title = null) { _ShowMessage(title, preText, MessageBoxButtons.OK, DialogSystemIcon.Error, exc); }
        /// <summary>
        /// Zobrazí text / chybu
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <param name="buttons"></param>
        /// <param name="icon"></param>
        /// <param name="exc"></param>
        /// <param name="defaultButton"></param>
        private static void _ShowMessage(string title, string text, MessageBoxButtons buttons, DialogSystemIcon? icon = null, Exception exc = null, MessageBoxDefaultButton? defaultButton = null)
        {
            DialogArgs args;
            if (exc != null)
            {
                args = DialogArgs.CreateForException(exc);
                if (icon != null) args.SystemIcon = icon;
                if (title != null) args.Title = title;
                if (text != null) args.MessageText = text + args.MessageText;
            }
            else
            {
                args = new DialogArgs();
                args.Title = title;
                args.MessageText = text;
                args.ButtonPanelDock = DockStyle.Bottom;
                args.ButtonsAlignment = AlignContentToSide.Center;
                args.SystemIcon = icon ?? DialogSystemIcon.Information;
                args.PrepareButtons(buttons, defaultButton);
            }
            DialogForm.ShowDialog(args);
        }
        #endregion
        #region SkinSupport a Colors, GetSkinColor, IsDarkTheme
        /// <summary>
        /// Vrátí aktuálně platnou barvu dle skinu.
        /// Vstupní jména by měly pocházet z prvků třídy <see cref="SkinElementColor"/>.
        /// Například barva textu v labelu je pod jménem <see cref="SkinElementColor.CommonSkins_WindowText"/>
        /// </summary>
        /// <param name="name">Typ prvku</param>
        /// <returns></returns>
        public static Color? GetSkinColor(string name) { return Instance._GetSkinColor(name); }
        /// <summary>
        /// Vrátí prvek skinu dané komponenty. Název komponenty berme z hodnot v <see cref="SkinElementColor"/>, například <see cref="SkinElementColor.RibbonSkins"/> pro skin Ribbonu.
        /// </summary>
        /// <param name="skinPartName"></param>
        /// <returns></returns>
        public static DevExpress.Skins.Skin GetSkinInfo(string skinPartName) { return Instance._GetSkinByName(skinPartName); }
        private void _OnSkinChanged()
        { }
        private Color? _GetSkinColor(string name)
        {
            if (String.IsNullOrEmpty(name) || !name.Contains(".")) return null;
            var parts = name.Split('.');
            if (parts.Length != 2) return null;
            string colorSource = parts[0];
            string colorEntity = parts[1];
            if (colorSource == "Control")
                return _GetControlColor(colorEntity);

            var skin = _GetSkinByName(colorSource);
            if (skin == null) return null;
            if (skin.Colors.Contains(colorEntity)) return skin.Colors[colorEntity];

            if (_TryGetSkinSystemColor(skin, colorEntity, out var systemColor)) return systemColor.Value;

            return null;
        }
        /// <summary>
        /// Vrátí požadovanou část definice aktuálního skinu (oblast, family).
        /// Pro zadaný text "CommonSkins" vrací DevExpress.Skins.CommonSkins.GetSkin(DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveLookAndFeel), atd.
        /// Lze zadat hodnotu např. <see cref="SkinElementColor.RibbonSkins"/>; ta má sice na konci tečku, ale zdejší metoda si s tím poradí...
        /// </summary>
        /// <param name="skinPartName"></param>
        /// <returns></returns>
        private DevExpress.Skins.Skin _GetSkinByName(string skinPartName)
        {
            var alaf = DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveLookAndFeel;
            skinPartName = skinPartName.Replace(".", "");           // Aby bylo možno použít hodnotu např. SkinElementColor.RibbonSkins (=s tečkou na konci) jako parametr této metody
            switch (skinPartName)
            {
                case "CommonSkins": return DevExpress.Skins.CommonSkins.GetSkin(alaf);
                case "EditorsSkins": return DevExpress.Skins.EditorsSkins.GetSkin(alaf);
                case "BarSkins": return DevExpress.Skins.BarSkins.GetSkin(alaf);
                case "ChartSkins": return DevExpress.Skins.ChartSkins.GetSkin(alaf);
                case "DashboardSkins": return DevExpress.Skins.DashboardSkins.GetSkin(alaf);
                case "DockingSkins": return DevExpress.Skins.DockingSkins.GetSkin(alaf);
                case "FormSkins": return DevExpress.Skins.FormSkins.GetSkin(alaf);
                case "GridSkins": return DevExpress.Skins.GridSkins.GetSkin(alaf);
                case "NavBarSkins": return DevExpress.Skins.NavBarSkins.GetSkin(alaf);
                case "NavPaneSkins": return DevExpress.Skins.NavPaneSkins.GetSkin(alaf);
                case "RibbonSkins": return DevExpress.Skins.RibbonSkins.GetSkin(alaf);
                case "SpreadsheetSkins": return DevExpress.Skins.SpreadsheetSkins.GetSkin(alaf);
                case "TabSkins": return DevExpress.Skins.TabSkins.GetSkin(alaf);
            }
            return null;
        }
        /// <summary>
        /// Vrátí barvu z reálného controlu
        /// </summary>
        /// <param name="controlPartName"></param>
        /// <returns></returns>
        private Color? _GetControlColor(string controlPartName)
        {
            DxLabelControl dxLabelControl;
            DxTextEdit dxTextEdit;
            DxPanelControl dxPanelControl;
            switch (controlPartName)
            {
                case "LabelForeColor":
                    dxLabelControl = _GetControlFromCache("Label") as DxLabelControl;
                    return dxLabelControl?.ForeColor;
                case "TextBoxForeColor":
                    dxTextEdit = _GetControlFromCache("TextBox") as DxTextEdit;
                    return dxTextEdit?.ForeColor;
                case "TextBoxBackColor":
                    dxTextEdit = _GetControlFromCache("TextBox") as DxTextEdit;
                    return dxTextEdit?.BackColor;
                case "PanelBackColor":
                    dxPanelControl = _GetControlFromCache("Panel") as DxPanelControl;
                    return dxPanelControl?.BackColor;
            }
            return null;
        }
        /// <summary>
        /// Vrátí daný control z cache / ytvoří nový a umístí tam a vrátí
        /// </summary>
        /// <param name="controlName"></param>
        /// <returns></returns>
        private Control _GetControlFromCache(string controlName)
        {
            Control control;
            if (_ControlColors == null) _ControlColors = new Dictionary<string, Control>();
            if (_ControlColors.TryGetValue(controlName, out control)) return control;

            switch (controlName)
            {
                case "Label":
                    control = new DxLabelControl();
                    break;
                case "TextBox":
                    control = new DxTextEdit();
                    break;
                case "Panel":
                    control = new DxPanelControl();
                    break;
                case "Button":
                    control = new DxSimpleButton();
                    break;
            }
            _ControlColors.Add(controlName, control);
            return control;
        }
        /// <summary>
        /// Zkusí najít a vrátit systémovou barvu v daném skinu
        /// </summary>
        /// <param name="skin"></param>
        /// <param name="colorEntity"></param>
        /// <param name="systemColor"></param>
        /// <returns></returns>
        private bool _TryGetSkinSystemColor(DevExpress.Skins.Skin skin, string colorEntity, out Color? systemColor)
        {
            systemColor = _GetSkinSystemColor(skin, colorEntity);
            return systemColor.HasValue;
        }
        /// <summary>
        /// Zkusí najít a vrátit systémovou barvu v daném skinu
        /// </summary>
        /// <param name="skin"></param>
        /// <param name="colorEntity"></param>
        /// <returns></returns>
        private Color? _GetSkinSystemColor(DevExpress.Skins.Skin skin, string colorEntity)
        {
            if (skin is null || String.IsNullOrEmpty(colorEntity)) return null;
            // Původně jsem tu chtěl psát switch { case ... }, jenže hodnoty nejsou konstanty ale static get (a to do switche nejde), i proto, že jejich zdroje (např. SystemColors.ButtonFace.Name) jsou instanční get.
            if (colorEntity == SkinElementColor.SystemColorActiveBorder) return skin.GetSystemColor(SystemColors.ActiveBorder);
            if (colorEntity == SkinElementColor.SystemColorActiveCaption) return skin.GetSystemColor(SystemColors.ActiveCaption);
            if (colorEntity == SkinElementColor.SystemColorActiveCaptionText) return skin.GetSystemColor(SystemColors.ActiveCaptionText);
            if (colorEntity == SkinElementColor.SystemColorAppWorkspace) return skin.GetSystemColor(SystemColors.AppWorkspace);
            if (colorEntity == SkinElementColor.SystemColorButtonFace) return skin.GetSystemColor(SystemColors.ButtonFace);
            if (colorEntity == SkinElementColor.SystemColorButtonHighlight) return skin.GetSystemColor(SystemColors.ButtonHighlight);
            if (colorEntity == SkinElementColor.SystemColorButtonShadow) return skin.GetSystemColor(SystemColors.ButtonShadow);
            if (colorEntity == SkinElementColor.SystemColorControl) return skin.GetSystemColor(SystemColors.Control);
            if (colorEntity == SkinElementColor.SystemColorControlDark) return skin.GetSystemColor(SystemColors.ControlDark);
            if (colorEntity == SkinElementColor.SystemColorControlDarkDark) return skin.GetSystemColor(SystemColors.ControlDarkDark);
            if (colorEntity == SkinElementColor.SystemColorControlLight) return skin.GetSystemColor(SystemColors.ControlLight);
            if (colorEntity == SkinElementColor.SystemColorControlLightLight) return skin.GetSystemColor(SystemColors.ControlLightLight);
            if (colorEntity == SkinElementColor.SystemColorControlText) return skin.GetSystemColor(SystemColors.ControlText);
            if (colorEntity == SkinElementColor.SystemColorDesktop) return skin.GetSystemColor(SystemColors.Desktop);
            if (colorEntity == SkinElementColor.SystemColorGradientActiveCaption) return skin.GetSystemColor(SystemColors.GradientActiveCaption);
            if (colorEntity == SkinElementColor.SystemColorGradientInactiveCaption) return skin.GetSystemColor(SystemColors.GradientInactiveCaption);
            if (colorEntity == SkinElementColor.SystemColorGrayText) return skin.GetSystemColor(SystemColors.GrayText);
            if (colorEntity == SkinElementColor.SystemColorHighlight) return skin.GetSystemColor(SystemColors.Highlight);
            if (colorEntity == SkinElementColor.SystemColorHighlightText) return skin.GetSystemColor(SystemColors.HighlightText);
            if (colorEntity == SkinElementColor.SystemColorHotTrack) return skin.GetSystemColor(SystemColors.HotTrack);
            if (colorEntity == SkinElementColor.SystemColorInactiveBorder) return skin.GetSystemColor(SystemColors.InactiveBorder);
            if (colorEntity == SkinElementColor.SystemColorInactiveCaption) return skin.GetSystemColor(SystemColors.InactiveCaption);
            if (colorEntity == SkinElementColor.SystemColorInactiveCaptionText) return skin.GetSystemColor(SystemColors.InactiveCaptionText);
            if (colorEntity == SkinElementColor.SystemColorInfo) return skin.GetSystemColor(SystemColors.Info);
            if (colorEntity == SkinElementColor.SystemColorInfoText) return skin.GetSystemColor(SystemColors.InfoText);
            if (colorEntity == SkinElementColor.SystemColorMenu) return skin.GetSystemColor(SystemColors.Menu);
            if (colorEntity == SkinElementColor.SystemColorMenuBar) return skin.GetSystemColor(SystemColors.MenuBar);
            if (colorEntity == SkinElementColor.SystemColorMenuHighlight) return skin.GetSystemColor(SystemColors.MenuHighlight);
            if (colorEntity == SkinElementColor.SystemColorMenuText) return skin.GetSystemColor(SystemColors.MenuText);
            if (colorEntity == SkinElementColor.SystemColorScrollBar) return skin.GetSystemColor(SystemColors.ScrollBar);
            if (colorEntity == SkinElementColor.SystemColorWindow) return skin.GetSystemColor(SystemColors.Window);
            if (colorEntity == SkinElementColor.SystemColorWindowFrame) return skin.GetSystemColor(SystemColors.WindowFrame);
            if (colorEntity == SkinElementColor.SystemColorWindowText) return skin.GetSystemColor(SystemColors.WindowText);
            return null;
        }
        /// <summary>
        /// Resetuje objekty v cache controlů pro získávání nativních barev
        /// </summary>
        private void _ResetControlColors()
        {
            if (_ControlColors != null)
            {
                _ControlColors.Values.ForEachExec(c => c.Dispose());
                _ControlColors.Clear();
            }
            __IsDarkTheme = null;
        }
        /// <summary>
        /// cache controlů pro získávání nativních barev
        /// </summary>
        private Dictionary<string, Control> _ControlColors;
        /// <summary>
        /// Vrátí náhodnou barvu určitého typu.
        /// </summary>
        /// <param name="colorFamily">Typ barvy (Pastelové, Plné, Tmavé)</param>
        /// <returns></returns>
        public static Color GetRandomColor(ColorFamilyType colorFamily = ColorFamilyType.Full)
        {
            var colorIndex = Instance._Rand.Next();
            return GetColor(colorIndex, colorFamily);
        }
        /// <summary>
        /// Vrátí barvu odpovídající danému indexu, daného typu.
        /// Index barvy může mít libovolnou hodnotou (bez omezení Min - Max).
        /// Pokud bude na vstupu zadán shodný index, na výstupu bude shodná barva, nejde o náhodné číslo.
        /// </summary>
        /// <param name="colorIndex">Index barvy, libovolný rozsah hodnot v rámci Int32. Reálná barva bude vybrána z množiny pevných barev, na základě daného indexu modulo počet barev.</param>
        /// <param name="colorFamily">Typ barvy (Pastelové, Plné, Tmavé)</param>
        /// <returns></returns>
        public static Color GetColor(int colorIndex, ColorFamilyType colorFamily = ColorFamilyType.Full)
        {
            int modulo = 14;
            int index = colorIndex % modulo;
            if (index < 0) index += modulo;
            Color color;
            switch (colorFamily)
            {
                case ColorFamilyType.Pastel:
                    switch (index)
                    {
                        case 0: color = Color.FromArgb(255, 127, 127); break;
                        case 1: color = Color.FromArgb(255, 178, 127); break;
                        case 2: color = Color.FromArgb(255, 233, 127); break;
                        case 3: color = Color.FromArgb(218, 255, 127); break;
                        case 4: color = Color.FromArgb(165, 255, 127); break;
                        case 5: color = Color.FromArgb(127, 255, 142); break;
                        case 6: color = Color.FromArgb(127, 255, 197); break;
                        case 7: color = Color.FromArgb(127, 255, 255); break;
                        case 8: color = Color.FromArgb(127, 201, 255); break;
                        case 9: color = Color.FromArgb(127, 146, 255); break;
                        case 10: color = Color.FromArgb(161, 127, 255); break;
                        case 11: color = Color.FromArgb(214, 127, 255); break;
                        case 12: color = Color.FromArgb(255, 127, 237); break;
                        case 13: color = Color.FromArgb(255, 127, 182); break;
                        default: color = Color.FromArgb(160, 160, 160); break;
                    }
                    break;
                case ColorFamilyType.Dark:
                    switch (index)
                    {
                        case 0: color = Color.FromArgb(127,0,0); break;
                        case 1: color = Color.FromArgb(127,51,0); break;
                        case 2: color = Color.FromArgb(127,106,0); break;
                        case 3: color = Color.FromArgb(91,127,0); break;
                        case 4: color = Color.FromArgb(38,127,0); break;
                        case 5: color = Color.FromArgb(0,127,14); break;
                        case 6: color = Color.FromArgb(0,127,70); break;
                        case 7: color = Color.FromArgb(0,127,127); break;
                        case 8: color = Color.FromArgb(0,74,127); break;
                        case 9: color = Color.FromArgb(0,19,127); break;
                        case 10: color = Color.FromArgb(33,0,127); break;
                        case 11: color = Color.FromArgb(87,0,127); break;
                        case 12: color = Color.FromArgb(127,0,110); break;
                        case 13: color = Color.FromArgb(127,0,55); break;
                        default: color = Color.FromArgb(64,64,64); break;
                    }
                    break;
                case ColorFamilyType.Full:
                default:
                    switch (index)
                    {
                        case 0: color = Color.FromArgb(255,0,0); break;
                        case 1: color = Color.FromArgb(255,106,0); break;
                        case 2: color = Color.FromArgb(255,216,0); break;
                        case 3: color = Color.FromArgb(182,255,0); break;
                        case 4: color = Color.FromArgb(76,255,0); break;
                        case 5: color = Color.FromArgb(0,255,33); break;
                        case 6: color = Color.FromArgb(0,255,144); break;
                        case 7: color = Color.FromArgb(0,255,255); break;
                        case 8: color = Color.FromArgb(0,148,255); break;
                        case 9: color = Color.FromArgb(0,38,255); break;
                        case 10: color = Color.FromArgb(72,0,255); break;
                        case 11: color = Color.FromArgb(178,0,255); break;
                        case 12: color = Color.FromArgb(255,0,220); break;
                        case 13: color = Color.FromArgb(255,0,110); break;
                        default: color = Color.FromArgb(128,128,128); break;
                    }
                    break;
            }
            return color;
        }
        /// <summary>
        /// Obsahuje true, pokud je aktuálně použitý tmavý skin.
        /// Kdo chce dostávat informace o změně typu skinu (světlý / tmavý), nechť si implementuje interface <see cref="IListenerLightDarkChanged"/>
        /// a zaregistruje se jako listener do <see cref="DxComponent.RegisterListener(IListener)"/>, 
        /// a bude dostávat událost <see cref="IListenerLightDarkChanged.LightDarkChanged"/>
        /// </summary>
        public static bool IsDarkTheme { get { return Instance._IsDarkTheme(); } }
        /// <summary>
        /// Vrátí (nebo nejprve zjistí) zda aktuálně je použit tmavý skin
        /// </summary>
        /// <returns></returns>
        private bool _IsDarkTheme()
        {
            if (!__IsDarkTheme.HasValue)
                __IsDarkTheme = _IsCurrentDarkSkin();
            return __IsDarkTheme.Value;
        }
        /// <summary>
        /// Vrací true, pokud je aktuální skin Tmavý = odhaduje to podle barvy písma u tlačítek v Ribbonu
        /// </summary>
        /// <returns></returns>
        private bool _IsCurrentDarkSkin()
        {
            Color? textColor = null;

            // a) Najdu element "Button" skinu "RibbonSkins", najdu jeho barvu "ForeColor:
            // POZOR, pro některé skiny sice hlásí barvu, ale špatnou!!!
            //DevExpress.Skins.Skin skin = _GetSkinByName(SkinElementColor.RibbonSkins);
            //if (skin != null)
            //{
            //    DevExpress.Skins.SkinElement[] elements = new DevExpress.Skins.SkinElement[skin.Elements.Values.Count];
            //    skin.Elements.Values.CopyTo(elements, 0);
            //    if (elements.TryGetFirst(e => e.ElementName == "Button", out var buttonElement) && !buttonElement.Color.ForeColor.IsEmpty)
            //        textColor = buttonElement.Color.ForeColor;
            //}


            // b) Získám aktivní skin Ribbonu, a přečtu jeho systémovou barvu pro text menu = odpovídá barvě písma pro tlačítka v Ribbonu:
            // POZOR, sice hlásí někdy nepřesnou barvu, ale víceméně podobnou, jakou reálně používá:
            if (!textColor.HasValue)
                textColor = _GetSkinColor(SkinElementColor.RibbonSkins + SkinElementColor.SystemColorMenuText);    // = DevExpress.Skins.RibbonSkins.GetSkin(DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveLookAndFeel).GetSystemColor(SystemColors.MenuText);


            // A pokud je barva písmen světlá, pak je skin tmavý:
            return (textColor.HasValue && textColor.Value.GetBrightness() >= 0.5f);
        }
        private bool? __IsDarkTheme;
        /// <summary>
        /// Vrátí náhodný prvek dodaného seznamu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static T GetRandomItem<T>(IList<T> collection)
        {
            if (collection != null && collection.Count > 0)
            {
                int index = Instance._Rand.Next(collection.Count);
                return collection[index];
            }
            return default;
        }
        #endregion
        #region Static helpers
        /// <summary>
        /// Vrátí <see cref="DefaultBoolean"/> z hodnoty nullable <see cref="Boolean"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DefaultBoolean Convert(bool? value)
        {
            return (value.HasValue ? (value.Value ? DefaultBoolean.True : DefaultBoolean.False) : DefaultBoolean.Default);
        }
        /// <summary>
        /// Vrátí nullable <see cref="Boolean"/> z hodnoty <see cref="DefaultBoolean"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool? Convert(DefaultBoolean value)
        {
            return (value == DefaultBoolean.True ? (bool?)true :
                   (value == DefaultBoolean.False ? (bool?)false : (bool?)null));
        }
        #endregion
        #region DxClipboard : obálka nad systémovým clipboardem
        /// <summary>
        /// Inicializac clipboardu
        /// </summary>
        private void _InitClipboard()
        {
            _ClipboardApplicationId = Guid.NewGuid().ToString();
        }
        /// <summary>
        /// ID aplikace = odlišuje typicky dvě různé aplikace otevřené v jeden okamžik
        /// </summary>
        public static string ClipboardApplicationId { get { return Instance._ClipboardApplicationId; } set { Instance._ClipboardApplicationId = value; } }
        /// <summary>
        /// Vloží do Clipboardu daný text
        /// </summary>
        /// <param name="text"></param>
        public static void ClipboardInsert(string text) { Instance._ClipboardInsert(null, text, DataFormats.Text); }
        /// <summary>
        /// Vloží do Clipboardu daná aplikační data a další údaj, typicky text
        /// </summary>
        /// <param name="applicationData"></param>
        /// <param name="windowsData"></param>
        /// <param name="windowsFormat"></param>
        public static void ClipboardInsert(object applicationData, object windowsData, string windowsFormat = null) { Instance._ClipboardInsert(applicationData, windowsData, windowsFormat); }
        /// <summary>
        /// Zkusí z Clipboardu vytáhnout nějaká aplikační data
        /// </summary>
        /// <param name="applicationData"></param>
        /// <returns></returns>
        public static bool ClipboardTryGetApplicationData(out object applicationData) { return Instance._ClipboardTryGetApplicationData(out applicationData, out string applicationId); }
        /// <summary>
        /// Zkusí z Clipboardu vytáhnout nějaká aplikační data a ID aplikace Nephrite, která je tam vložila
        /// </summary>
        /// <param name="applicationData"></param>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        public static bool ClipboardTryGetApplicationData(out object applicationData, out string applicationId) { return Instance._ClipboardTryGetApplicationData(out applicationData, out applicationId); }
        /// <summary>
        /// ID aktuální aplikace, přidává se do Clipboardu.
        /// Výchozí hodnota je unikátní Guid.
        /// </summary>
        private string _ClipboardApplicationId = null;
        /// <summary>
        /// Vloží data do Clipboardu
        /// </summary>
        /// <param name="applicationData"></param>
        /// <param name="windowsData"></param>
        /// <param name="windowsFormat"></param>
        private void _ClipboardInsert(object applicationData, object windowsData, string windowsFormat)
        {
            DataObject dataObject = new DataObject();
            int count = 0;
            string errorMsg = "";
            if (applicationData != null)
            {
                ClipboardContainer container = new ClipboardContainer() { ApplicationId = _ClipboardApplicationId, Data = applicationData };
                try
                {
                    string containerXml = WSXmlSerializer.Persist.Serialize(container, WSXmlSerializer.PersistArgs.Default);
                    dataObject.SetData(ClipboardAppDataId, containerXml);
                    count++;
                }
                catch (Exception exc)
                {
                    errorMsg += exc.Message + Environment.NewLine;
                }
            }
            if (windowsData != null)
            {
                if (windowsFormat == null) windowsFormat = DataFormats.Text;
                dataObject.SetData(windowsFormat, windowsData);
                count++;
            }
            if (count > 0)
            {
                try { System.Windows.Forms.Clipboard.SetDataObject(dataObject, true); }
                catch (Exception exc)
                {
                    errorMsg += exc.Message + Environment.NewLine;
                }
            }
            if (errorMsg.Length > 0)
            { }
        }
        /// <summary>
        /// Zkusí z Clipboardu vytáhnout nějaká aplikační data a ID aplikace Nephrite, která je tam vložila
        /// </summary>
        /// <param name="applicationData"></param>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        private bool _ClipboardTryGetApplicationData(out object applicationData, out string applicationId)
        {
            applicationData = null;
            applicationId = null;
            IDataObject dataObject = null;
            try { dataObject = System.Windows.Forms.Clipboard.GetDataObject(); }
            catch { }
            if (dataObject == null) return false;
            if (!dataObject.GetDataPresent(ClipboardAppDataId)) return false;
            string containerXml = dataObject.GetData(ClipboardAppDataId) as string;
            if (containerXml == null) return false;
            ClipboardContainer container = WSXmlSerializer.Persist.Deserialize(containerXml) as ClipboardContainer;
            if (container == null) return false;
            applicationId = container.ApplicationId;
            applicationData = container.Data;
            return true;
        }
        /// <summary>
        /// Typ dat ukládaných v Clipboardu pro aplikační data
        /// </summary>
        private const string ClipboardAppDataId = "DxAppData";

        /// <summary>
        /// Obsah clipboardu = ID plus Data
        /// </summary>
        [Serializable]
        private class ClipboardContainer
        {
            /// <summary>
            /// ID zdroje dat
            /// </summary>
            public string ApplicationId { get; set; }
            /// <summary>
            /// Vlastní data
            /// </summary>
            public object Data { get; set; }
        }


        /*
        private void _ClipboardInsert(object applicationData, object windowsData, string windowsFormat)
        {
            ClipboardContainer container = new ClipboardContainer() { ApplicationId = _ClipboardApplicationId, Data = Persist.Serialize(applicationData, PersistArgs.Default) };
            DataObject dataObject = new DataObject();
            string applicationXml = Persist.Serialize(applicationData, PersistArgs.Default);
            dataObject.SetData(ClipboardAppDataId, applicationXml);
            if (windowsData != null)
            {
                if (windowsFormat == null) windowsFormat = DataFormats.Text;
                dataObject.SetData(windowsFormat, windowsData);
            }
            try { System.Windows.Forms.Clipboard.SetDataObject(dataObject, true); }
            catch { }
        }
        private bool _ClipboardTryGetApplicationData(out object applicationData)
        {
            applicationData = null;
            IDataObject dataObject = null;
            try { dataObject = System.Windows.Forms.Clipboard.GetDataObject(); }
            catch { }
            if (dataObject == null) return false;
            bool containsText = dataObject.GetDataPresent(DataFormats.Text);
            bool containsNephrite = dataObject.GetDataPresent("Nephrite");
            if (!dataObject.GetDataPresent("Nephrite")) return false;
            string nephriteXml = dataObject.GetData("Nephrite") as string;
            applicationData = Persist.Deserialize(nephriteXml);
            return true;
        }
        */
        /*
        private void _ClipboardCopy(object nephriteData, object windowsData, string windowsFormat)
        {
            NephriteDataObject container = new NephriteDataObject();
            container.SetData("Nephrite", nephriteData);
            container.NephriteData = nephriteData;
            if (windowsData != null)
            {
                if (windowsFormat == null) windowsFormat = DataFormats.Text;
                container.SetData(windowsFormat, windowsData);
            }
            try { System.Windows.Forms.Clipboard.SetDataObject(container, true); }
            catch { }
        }
        private bool _ClipboardTryGetNephriteData(out object nephriteData)
        {
            nephriteData = null;
            IDataObject dataObject = null;
            try { dataObject = System.Windows.Forms.Clipboard.GetDataObject(); }
            catch { }
            if (dataObject == null) return false;
            if (!(dataObject is NephriteDataObject ndo)) return false;
            if (!ndo.HasNephriteData) return false;
            nephriteData = ndo.NephriteData;
            return true;
        }

        private class NephriteDataObject : DataObject
        {
            public object NephriteData 
            { 
                get;
                set;
            }
            public bool HasNephriteData { get; set; }
        }
        */
        #endregion
        #region Win32Api informace a další metody
        [DllImport("User32")]
        private extern static int GetGuiResources(IntPtr hProcess, int uiFlags);
        /// <summary>
        /// Získá a vrátí informace o využití zdrojů operačního systému
        /// </summary>
        /// <returns></returns>
        public static WinProcessInfo GetWinProcessInfo()
        {
            return WinProcessInfo.GetCurent();
        }
        /// <summary>
        /// Informace o využití zdrojů operačního systému
        /// </summary>
        public class WinProcessInfo
        {
            /// <summary>
            /// Získá a vrátí informace o využití zdrojů operačního systému
            /// </summary>
            /// <returns></returns>
            public static WinProcessInfo GetCurent()
            {
                using (var process = System.Diagnostics.Process.GetCurrentProcess())
                {
                    long privateMemory = process.PrivateMemorySize64;
                    long workingSet64 = process.WorkingSet64;
                    int gDIHandleCount = GetGuiResources(process.Handle, 0);
                    int userHandleCount = GetGuiResources(process.Handle, 1);
                    return new WinProcessInfo(privateMemory, workingSet64, gDIHandleCount, userHandleCount);
                }
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="privateMemory"></param>
            /// <param name="workingSet"></param>
            /// <param name="gDIHandleCount"></param>
            /// <param name="userHandleCount"></param>
            public WinProcessInfo(long privateMemory, long workingSet, int gDIHandleCount, int userHandleCount)
            {
                this.PrivateMemory = privateMemory;
                this.WorkingSet = workingSet;
                this.GDIHandleCount = gDIHandleCount;
                this.UserHandleCount = userHandleCount;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString() { return Text4; }
            /// <summary>
            /// Jednořádkový text, 2 údaje. Popisek je v <see cref="Text2Info"/>
            /// </summary>
            public string Text2
            {
                get
                {
                    return $"   GDI: {GDIHandleCount};  User: {UserHandleCount}   ";
                }
            }
            /// <summary>
            /// Popisek hodnot uvedených v <see cref="Text2"/>, vhodný do ToolTipu
            /// </summary>
            public static string Text2Info { get { return "\r\nGDI: počet GDI Handles z WinAPI\r\nUser: počet User Handles z WinAPI"; } }

            /// <summary>
            /// Jednořádkový text, 4 údaje. Popisek je v <see cref="Text4Info"/>
            /// </summary>
            public string Text4
            {
                get
                {
                    string privMemoryKb = ((int)(this.PrivateMemory / 1024L)).ToString("### ### ##0").Trim();
                    string workSetKb = ((int)(this.WorkingSet / 1024L)).ToString("### ### ##0").Trim();
                    return $"   Priv: {privMemoryKb} KB;  Work: {workSetKb} KB;  GDI: {GDIHandleCount};  User: {UserHandleCount}   ";
                }
            }
            /// <summary>
            /// Plný text, 4 údaje na 4 řádcích, do ToolTipu.
            /// </summary>
            public string Text4Full
            {
                get
                {
                    string eol = Environment.NewLine;
                    string privMemoryKb = ((int)(this.PrivateMemory / 1024L)).ToString("### ### ##0").Trim();
                    string workSetKb = ((int)(this.WorkingSet / 1024L)).ToString("### ### ##0").Trim();
                    return $"Private memory {privMemoryKb} KB{eol}Working set: {workSetKb} KB{eol}GDI Handles: {GDIHandleCount}{eol}USER Handles: {UserHandleCount}";
                }
            }
            /// <summary>
            /// Popisek hodnot uvedených v <see cref="Text4"/>, vhodný do ToolTipu
            /// </summary>
            public static string Text4Info { get { return "\r\nPriv: spotřeba paměti 'Private KB'\r\nWork: spotřeba paměti 'WorkingSet KB'\r\nGDI: počet GDI Handles z WinAPI\r\nUser: počet User Handles z WinAPI"; } }
            /// <summary>
            /// <see cref="System.Diagnostics.Process.PrivateMemorySize64"/>
            /// </summary>
            public long PrivateMemory { get; private set; }
            /// <summary>
            /// <see cref="System.Diagnostics.Process.WorkingSet64"/>
            /// </summary>
            public long WorkingSet { get; private set; }
            /// <summary>
            /// Počet využitých objektů GDI Handle z WinAPI
            /// </summary>
            public int GDIHandleCount { get; private set; }
            /// <summary>
            /// Počet využitých objektů USER Handle z WinAPI
            /// </summary>
            public int UserHandleCount { get; private set; }
            /// <summary>
            /// Součet hodnot dvou instancí
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            public static WinProcessInfo operator +(WinProcessInfo a, WinProcessInfo b)
            {
                if (a is null || b is null) return null;
                return new WinProcessInfo(a.PrivateMemory + b.PrivateMemory, a.WorkingSet + b.WorkingSet, a.GDIHandleCount + b.GDIHandleCount, a.UserHandleCount + b.UserHandleCount);
            }
            /// <summary>
            /// Rozdíl hodnot dvou instancí
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            public static WinProcessInfo operator -(WinProcessInfo a, WinProcessInfo b)
            {
                if (a is null || b is null) return null;
                return new WinProcessInfo(a.PrivateMemory - b.PrivateMemory, a.WorkingSet - b.WorkingSet, a.GDIHandleCount - b.GDIHandleCount, a.UserHandleCount - b.UserHandleCount);
            }
        }
        /// <summary>
        /// Obsahuje jméno frameworku, na kterém aktuálně běžíme.
        /// </summary>
        public static string FrameworkName { get { return Instance._FrameworkName; } }
        private string _FrameworkName
        {
            get
            {
                if (__FrameworkName == null)
                    __FrameworkName = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
                return __FrameworkName;
            }
        }
        private string __FrameworkName;
        /// <summary>
        /// Aplikace má podporu pro UHD monitory (PerMonitorDPI)
        /// </summary>
        public static bool UhdPaintEnabled { get { return Instance._UhdPaintEnabled; } set { Instance._SetUhdPaint(value); } }

        private void _SetUhdPaint(bool uhdEnable)
        {
            if (uhdEnable)
            {
                DevExpress.XtraEditors.WindowsFormsSettings.AllowDpiScale = false;
                DevExpress.XtraEditors.WindowsFormsSettings.ForceDirectXPaint();
                DevExpress.XtraEditors.WindowsFormsSettings.SetPerMonitorDpiAware();
                _UhdPaintEnabled = true;
            }
            else
            {
                DevExpress.XtraEditors.WindowsFormsSettings.AllowAutoScale = DevExpress.Utils.DefaultBoolean.True;
                DevExpress.XtraEditors.WindowsFormsSettings.AllowDpiScale = true;
                DevExpress.XtraEditors.WindowsFormsSettings.ForceGDIPlusPaint();
                _UhdPaintEnabled = false;
            }
        }
        private bool _UhdPaintEnabled;
        #endregion
        #region Win32Api Block input a DoEventsBlockingInput
        [DllImport("user32.dll", EntryPoint = "BlockInput")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);
        /// <summary>
        /// Zakáže provádění uživatelských akcí (klávesnice, myš do WinForm controlů)
        /// </summary>
        public static void WinApiHoldUser()
        {
            BlockInput(true);
        }
        /// <summary>
        /// Povolí provádění uživatelských akcí (klávesnice, myš do WinForm controlů)
        /// </summary>
        public static void WinApiReleaseUser()
        {
            BlockInput(false);
        }
        /// <summary>
        /// Provede <see cref="Application.DoEvents()"/>, ale s vyloučením (blokováním) uživatelských vstupů.
        /// </summary>
        public static void DoEventsWithBlockingInput()
        {
            WinApiHoldUser();
            Application.DoEvents();
            WinApiReleaseUser();
        }
        #endregion
        #region Překlad WinMSG kódů
        /// <summary>
        /// Vrátí string odpovídající dané Win Message. Obsahuje název události a její hodnotu.
        /// Metoda může vrátit NULL pro zprávy, které se mají ignorovat (viz parametr <paramref name="forceAll"/>).
        /// </summary>
        /// <param name="m"></param>
        /// <param name="forceAll">true = Povinně zpracovat všechny zprávy. Default = false: negenerují se zprávy pro GETTEXTLENGTH(0x000E) a GETTEXT(0x000D).</param>
        /// <returns></returns>
        public static string GetWinMessage(Message m, bool forceAll = false)
        {
            int code = m.Msg;
            if (!forceAll && (code == WM.GETTEXTLENGTH || code == WM.GETTEXT)) return null;

            var wmDict = WmDict;
            string name = (wmDict.TryGetValue(m.Msg, out string value) ? value : "???");
            string message = $"{name} (0x{(m.Msg.ToString("X4"))})";
            return message;
        }
        /// <summary>
        /// Dictionary známých zpráv WinMsg
        /// </summary>
        private static Dictionary<int, string> WmDict { get { if (_WmDict is null) _WmDict = GetWmDict(); return _WmDict; } }
        private static Dictionary<int, string> _WmDict;
        /// <summary>
        /// Vygeneruje a vrátí Dictionary obsahující známé WinMSG kódy a jejich názvy.
        /// Používá konstanty v třídě <see cref="WM"/>.
        /// </summary>
        /// <returns></returns>
        internal static Dictionary<int, string> GetWmDict()
        {
            Dictionary<int, string> wmDict = new Dictionary<int, string>();
            var fields = typeof(WM).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            foreach (var field in fields)
            {
                string name = field.Name;
                object value = field.GetValue(null);
                if ((value is int key) && !wmDict.ContainsKey(key))
                    wmDict.Add(key, name);
            }
            return wmDict;
        }
        #endregion
    }
    #region Enumy
    /// <summary>
    /// Typ barvy
    /// </summary>
    public enum ColorFamilyType
    {
        /// <summary>
        /// Plné barvy
        /// </summary>
        Full = 0,
        /// <summary>
        /// Pastelové barvy
        /// </summary>
        Pastel,
        /// <summary>
        /// Temné barvy
        /// </summary>
        Dark
    }
    /// <summary>
    /// Názvy barev, skinů a elementů, pro které lze získat barvz pomocí metody <see cref="DxComponent.GetSkinColor(string)"/>
    /// </summary>
    public class SkinElementColor
    {
        public static string Control { get { return "Control."; } }
        public static string Control_LabelForeColor { get { return Control + "LabelForeColor"; } }
        public static string Control_TextBoxForeColor { get { return Control + "TextBoxForeColor"; } }
        public static string Control_TextBoxBackColor { get { return Control + "TextBoxBackColor"; } }
        public static string Control_PanelBackColor { get { return Control + "PanelBackColor"; } }

        public static string CommonSkins { get { return "CommonSkins."; } }
        public static string CommonSkins_WindowText { get { return CommonSkins + "WindowText"; } }
        public static string CommonSkins_ReadOnly { get { return CommonSkins + "ReadOnly"; } }
        public static string CommonSkins_Info { get { return CommonSkins + "Info"; } }
        public static string CommonSkins_Success { get { return CommonSkins + "Success"; } }
        public static string CommonSkins_HotTrackedForeColor { get { return CommonSkins + "HotTrackedForeColor"; } }
        public static string CommonSkins_Danger { get { return CommonSkins + "Danger"; } }
        public static string CommonSkins_Control { get { return CommonSkins + "Control"; } }
        public static string CommonSkins_DisabledText { get { return CommonSkins + "DisabledText"; } }
        public static string CommonSkins_Highlight { get { return CommonSkins + "Highlight"; } }
        public static string CommonSkins_Question { get { return CommonSkins + "Question"; } }
        public static string CommonSkins_Primary { get { return CommonSkins + "Primary"; } }
        public static string CommonSkins_HighlightAlternate { get { return CommonSkins + "HighlightAlternate"; } }
        public static string CommonSkins_WarningFill { get { return CommonSkins + "WarningFill"; } }
        public static string CommonSkins_InfoText { get { return CommonSkins + "InfoText"; } }
        public static string CommonSkins_HotTrackedColor { get { return CommonSkins + "HotTrackedColor"; } }
        public static string CommonSkins_DisabledControl { get { return CommonSkins + "DisabledControl"; } }
        public static string CommonSkins_Information { get { return CommonSkins + "Information"; } }
        public static string CommonSkins_HighlightText { get { return CommonSkins + "HighlightText"; } }
        public static string CommonSkins_ControlText { get { return CommonSkins + "ControlText"; } }
        public static string CommonSkins_QuestionFill { get { return CommonSkins + "QuestionFill"; } }
        public static string CommonSkins_Warning { get { return CommonSkins + "Warning"; } }
        public static string CommonSkins_InactiveCaptionText { get { return CommonSkins + "InactiveCaptionText"; } }
        public static string CommonSkins_Window { get { return CommonSkins + "Window"; } }
        public static string CommonSkins_HideSelection { get { return CommonSkins + "HideSelection"; } }
        public static string CommonSkins_Menu { get { return CommonSkins + "Menu"; } }
        public static string CommonSkins_MenuText { get { return CommonSkins + "MenuText"; } }
        public static string CommonSkins_Critical { get { return CommonSkins + "Critical"; } }

        public static string EditorsSkins { get { return "EditorsSkins."; } }
        public static string EditorsSkins_ProgressBarEmptyTextColor { get { return EditorsSkins + "ProgressBarEmptyTextColor"; } }
        public static string EditorsSkins_FluentCalendarWeekDayForeColor { get { return EditorsSkins + "FluentCalendarWeekDayForeColor"; } }
        public static string EditorsSkins_CalendarSelectedCellColor { get { return EditorsSkins + "CalendarSelectedCellColor"; } }
        public static string EditorsSkins_FilterControlValueTextColor { get { return EditorsSkins + "FilterControlValueTextColor"; } }
        public static string EditorsSkins_FilterControlGroupOperatorTextColor { get { return EditorsSkins + "FilterControlGroupOperatorTextColor"; } }
        public static string EditorsSkins_BeakFormBorderColor { get { return EditorsSkins + "BeakFormBorderColor"; } }
        public static string EditorsSkins_HyperLinkTextColor { get { return EditorsSkins + "HyperLinkTextColor"; } }
        public static string EditorsSkins_FilterControlEmptyValueTextColor { get { return EditorsSkins + "FilterControlEmptyValueTextColor"; } }
        public static string EditorsSkins_FluentCalendarSeparatorColor { get { return EditorsSkins + "FluentCalendarSeparatorColor"; } }
        public static string EditorsSkins_ProgressBarFilledTextColor { get { return EditorsSkins + "ProgressBarFilledTextColor"; } }
        public static string EditorsSkins_FluentCalendarBackColor { get { return EditorsSkins + "FluentCalendarBackColor"; } }
        public static string EditorsSkins_FluentCalendarWeekNumberForeColor { get { return EditorsSkins + "FluentCalendarWeekNumberForeColor"; } }
        public static string EditorsSkins_FilterControlFieldNameTextColor { get { return EditorsSkins + "FilterControlFieldNameTextColor"; } }
        public static string EditorsSkins_CalcEditOperationTextColor { get { return EditorsSkins + "CalcEditOperationTextColor"; } }
        public static string EditorsSkins_FilterControlOperatorTextColor { get { return EditorsSkins + "FilterControlOperatorTextColor"; } }
        public static string EditorsSkins_FluentCalendarHolidayCellColor { get { return EditorsSkins + "FluentCalendarHolidayCellColor"; } }
        public static string EditorsSkins_CalcEditDigitTextColor { get { return EditorsSkins + "CalcEditDigitTextColor"; } }
        public static string EditorsSkins_CalendarTodayCellColor { get { return EditorsSkins + "CalendarTodayCellColor"; } }
        public static string EditorsSkins_CalendarInactiveCellColor { get { return EditorsSkins + "CalendarInactiveCellColor"; } }
        public static string EditorsSkins_CalendarNormalCellColor { get { return EditorsSkins + "CalendarNormalCellColor"; } }
        public static string EditorsSkins_CalendarHolidayCellColor { get { return EditorsSkins + "CalendarHolidayCellColor"; } }

        public static string BarSkins { get { return "BarSkins."; } }
        public static string BarSkins_ColorLinkDisabledForeColor { get { return BarSkins + "ColorLinkDisabledForeColor"; } }

        public static string ChartSkins { get { return "ChartSkins."; } }
        public static string ChartSkins_ColorLine3DMarker { get { return ChartSkins + "ColorLine3DMarker"; } }
        public static string ChartSkins_ColorConstantLineTitle { get { return ChartSkins + "ColorConstantLineTitle"; } }
        public static string ChartSkins_ColorArea3DMarker { get { return ChartSkins + "ColorArea3DMarker"; } }
        public static string ChartSkins_ColorConstantLine { get { return ChartSkins + "ColorConstantLine"; } }
        public static string ChartSkins_ColorChartTitle { get { return ChartSkins + "ColorChartTitle"; } }

        public static string DashboardSkins { get { return "DashboardSkins."; } }
        public static string DashboardSkins_ChartPaneRemoveButton { get { return DashboardSkins + "ChartPaneRemoveButton"; } }
        public static string DashboardSkins_BarAxisColor { get { return DashboardSkins + "BarAxisColor"; } }

        public static string DockingSkins { get { return "DockingSkins."; } }
        public static string DockingSkins_DocumentGroupHeaderTextColor { get { return DockingSkins + "DocumentGroupHeaderTextColor"; } }
        public static string DockingSkins_DocumentGroupHeaderTextColorDisabled { get { return DockingSkins + "DocumentGroupHeaderTextColorDisabled"; } }
        public static string DockingSkins_DocumentGroupHeaderTextColorHot { get { return DockingSkins + "DocumentGroupHeaderTextColorHot"; } }
        public static string DockingSkins_TabHeaderTextColorActive { get { return DockingSkins + "TabHeaderTextColorActive"; } }
        public static string DockingSkins_TabHeaderTextColorDisabled { get { return DockingSkins + "TabHeaderTextColorDisabled"; } }
        public static string DockingSkins_TabHeaderTextColorHot { get { return DockingSkins + "TabHeaderTextColorHot"; } }
        public static string DockingSkins_DocumentGroupHeaderTextColorActive { get { return DockingSkins + "DocumentGroupHeaderTextColorActive"; } }
        public static string DockingSkins_TabHeaderTextColor { get { return DockingSkins + "TabHeaderTextColor"; } }
        public static string DockingSkins_DocumentGroupHeaderTextColorGroupInactive { get { return DockingSkins + "DocumentGroupHeaderTextColorGroupInactive"; } }

        public static string FormSkins { get { return "FormSkins."; } }
        public static string FormSkins_TextShadowColor { get { return FormSkins + "TextShadowColor"; } }
        public static string FormSkins_InactiveColor { get { return FormSkins + "InactiveColor"; } }

        public static string RibbonSkins { get { return "RibbonSkins."; } }
        public static string RibbonSkins_ForeColorDisabledInCaptionQuickAccessToolbar { get { return RibbonSkins + "ForeColorDisabledInCaptionQuickAccessToolbar"; } }
        public static string RibbonSkins_ForeColorDisabledInBottomQuickAccessToolbar { get { return RibbonSkins + "ForeColorDisabledInBottomQuickAccessToolbar"; } }
        public static string RibbonSkins_ForeColorDisabledInCaptionQuickAccessToolbarInActive2010 { get { return RibbonSkins + "ForeColorDisabledInCaptionQuickAccessToolbarInActive2010"; } }
        public static string RibbonSkins_ButtonDisabled { get { return RibbonSkins + "ButtonDisabled"; } }
        public static string RibbonSkins_ForeColorInBackstageViewTitle { get { return RibbonSkins + "ForeColorInBackstageViewTitle"; } }
        public static string RibbonSkins_ForeColorDisabledInTopQuickAccessToolbar { get { return RibbonSkins + "ForeColorDisabledInTopQuickAccessToolbar"; } }
        public static string RibbonSkins_ForeColorDisabledInCaptionQuickAccessToolbar2010 { get { return RibbonSkins + "ForeColorDisabledInCaptionQuickAccessToolbar2010"; } }
        public static string RibbonSkins_EditorBackground { get { return RibbonSkins + "EditorBackground"; } }
        public static string RibbonSkins_RadialMenuColor { get { return RibbonSkins + "RadialMenuColor"; } }
        public static string RibbonSkins_ForeColorDisabledInPageHeader { get { return RibbonSkins + "ForeColorDisabledInPageHeader"; } }
        public static string RibbonSkins_ForeColorInCaptionQuickAccessToolbar2010 { get { return RibbonSkins + "ForeColorInCaptionQuickAccessToolbar2010"; } }

        public static string TabSkins { get { return "TabHeaderTextColorActive"; } }
        public static string TabSkins_TabHeaderTextColorActive { get { return TabSkins + "TabHeaderTextColorActive"; } }
        public static string TabSkins_TabHeaderButtonTextColorHot { get { return TabSkins + "TabHeaderButtonTextColorHot"; } }
        public static string TabSkins_TabHeaderButtonTextColor { get { return TabSkins + "TabHeaderButtonTextColor"; } }
        public static string TabSkins_TabHeaderTextColorDisabled { get { return TabSkins + "TabHeaderTextColorDisabled"; } }
        public static string TabSkins_TabHeaderTextColor { get { return TabSkins + "TabHeaderTextColor"; } }
        public static string TabSkins_TabHeaderTextColorHot { get { return TabSkins + "TabHeaderTextColorHot"; } }

        public static string SystemColorActiveBorder { get { return SystemColors.ActiveBorder.Name; } }
        public static string SystemColorActiveCaption { get { return SystemColors.ActiveCaption.Name; } }
        public static string SystemColorActiveCaptionText { get { return SystemColors.ActiveCaptionText.Name; } }
        public static string SystemColorAppWorkspace { get { return SystemColors.AppWorkspace.Name; } }
        public static string SystemColorButtonFace { get { return SystemColors.ButtonFace.Name; } }
        public static string SystemColorButtonHighlight { get { return SystemColors.ButtonHighlight.Name; } }
        public static string SystemColorButtonShadow { get { return SystemColors.ButtonShadow.Name; } }
        public static string SystemColorControl { get { return SystemColors.Control.Name; } }
        public static string SystemColorControlDark { get { return SystemColors.ControlDark.Name; } }
        public static string SystemColorControlDarkDark { get { return SystemColors.ControlDarkDark.Name; } }
        public static string SystemColorControlLight { get { return SystemColors.ControlLight.Name; } }
        public static string SystemColorControlLightLight { get { return SystemColors.ControlLightLight.Name; } }
        public static string SystemColorControlText { get { return SystemColors.ControlText.Name; } }
        public static string SystemColorDesktop { get { return SystemColors.Desktop.Name; } }
        public static string SystemColorGradientActiveCaption { get { return SystemColors.GradientActiveCaption.Name; } }
        public static string SystemColorGradientInactiveCaption { get { return SystemColors.GradientInactiveCaption.Name; } }
        public static string SystemColorGrayText { get { return SystemColors.GrayText.Name; } }
        public static string SystemColorHighlight { get { return SystemColors.Highlight.Name; } }
        public static string SystemColorHighlightText { get { return SystemColors.HighlightText.Name; } }
        public static string SystemColorHotTrack { get { return SystemColors.HotTrack.Name; } }
        public static string SystemColorInactiveBorder { get { return SystemColors.InactiveBorder.Name; } }
        public static string SystemColorInactiveCaption { get { return SystemColors.InactiveCaption.Name; } }
        public static string SystemColorInactiveCaptionText { get { return SystemColors.InactiveCaptionText.Name; } }
        public static string SystemColorInfo { get { return SystemColors.Info.Name; } }
        public static string SystemColorInfoText { get { return SystemColors.InfoText.Name; } }
        public static string SystemColorMenu { get { return SystemColors.Menu.Name; } }
        public static string SystemColorMenuBar { get { return SystemColors.MenuBar.Name; } }
        public static string SystemColorMenuHighlight { get { return SystemColors.MenuHighlight.Name; } }
        public static string SystemColorMenuText { get { return SystemColors.MenuText.Name; } }
        public static string SystemColorScrollBar { get { return SystemColors.ScrollBar.Name; } }
        public static string SystemColorWindow { get { return SystemColors.Window.Name; } }
        public static string SystemColorWindowFrame { get { return SystemColors.WindowFrame.Name; } }
        public static string SystemColorWindowText { get { return SystemColors.WindowText.Name; } }

    }
    #endregion
    #endregion
    #region MsgCode
    /// <summary>
    /// Knihovna standardních hlášek k lokalizaci
    /// </summary>
    public enum MsgCode
    {
        None = 0,
        [DefaultMessageText("SYSTÉM")]
        RibbonAppHomeText,
        [DefaultMessageText("Systémový prvek, nelze jej odebrat")]
        RibbonDirectQatItem,
        [DefaultMessageText("Přidat na panel nástrojů Rychlý přístup")]
        RibbonAddToQat,
        [DefaultMessageText("Odebrat z panelu nástrojů Rychlý přístup")]
        RibbonRemoveFromQat,
        [DefaultMessageText("Zobrazit panel nástrojů Rychlý přístup nad pásem karet")]
        RibbonShowQatTop,
        [DefaultMessageText("Zobrazit panel nástrojů Rychlý přístup pod pásem karet")]
        RibbonShowQatDown,
        [DefaultMessageText("Upravit obsah panelu nástrojů Rychlý přístup")]
        RibbonShowManager,
        [DefaultMessageText("Zmenšit")]
        RibbonMinimizeQat,
        [DefaultMessageText("Nastavení oblíbených položek")]
        RibbonQatManagerTitle,

        [DefaultMessageText("Chyba")]
        DialogFormTitleError,
        [DefaultMessageText("Došlo k chybě")]
        DialogFormTitlePrefix,
        [DefaultMessageText("Ctrl+C: zkopírovat")]
        DialogFormCtrlCText,
        [DefaultMessageText("Ctrl+C: zkopíruje všechny informace z okna do schránky Windows")]
        DialogFormCtrlCToolTip,
        [DefaultMessageText("Text zkopírován")]
        DialogFormCtrlCInfo,
        [DefaultMessageText("Více informací")]
        DialogFormAltMsgButtonText,
        [DefaultMessageText("Zobrazí větší okno s více informacemi")]
        DialogFormAltMsgButtonToolTip,
        [DefaultMessageText("Méně informací")]
        DialogFormStdMsgButtonText,
        [DefaultMessageText("Zobrazí základní okno s méně informacemi")]
        DialogFormStdMsgButtonToolTip,

        [DefaultMessageText("prefix")]
        DialogFormResultPrefix,
        [DefaultMessageText("OK")]
        DialogFormResultOk,
        [DefaultMessageText("Ano")]
        DialogFormResultYes,
        [DefaultMessageText("Ne")]
        DialogFormResultNo,
        [DefaultMessageText("Zrušit")]
        DialogFormResultAbort,
        [DefaultMessageText("Znovu")]
        DialogFormResultRetry,
        [DefaultMessageText("Ignoruj")]
        DialogFormResultIgnore,
        [DefaultMessageText("Storno")]
        DialogFormResultCancel,

        [DefaultMessageText("Orientace")]
        LayoutPanelContextMenuTitle,
        [DefaultMessageText("Vedle sebe")]
        LayoutPanelHorizontalText,
        [DefaultMessageText("Panely vlevo a vpravo, oddělovač je svislý")]
        LayoutPanelHorizontalToolTip,
        [DefaultMessageText("Pod sebou")]
        LayoutPanelVerticalText,
        [DefaultMessageText("Panely nahoře a dole, oddělovač je vodorovný")]
        LayoutPanelVerticalToolTip,
        [DefaultMessageText("Zavřít")]
        MenuCloseText,
        [DefaultMessageText("Zavře nabídku bez provedení akce")]
        MenuCloseToolTip,

        [DefaultMessageText("Smazat")]
        DxFilterBoxClearTipTitle,
        [DefaultMessageText("Zruší zadaný filtr")]
        DxFilterBoxClearTipText,
        [DefaultMessageText("Obsahuje")]
        DxFilterOperatorContainsText,
        [DefaultMessageText("Vybere ty položky, které obsahují zadaný text")]
        DxFilterOperatorContainsTip,
        [DefaultMessageText("Neobsahuje")]
        DxFilterOperatorDoesNotContainText,
        [DefaultMessageText("Vybere ty položky, které neobsahují zadaný text")]
        DxFilterOperatorDoesNotContainTip,
        [DefaultMessageText("Začíná")]
        DxFilterOperatorStartWithText,
        [DefaultMessageText("Vybere ty položky, jejichž text začíná zadaným textem")]
        DxFilterOperatorStartWithTip,
        [DefaultMessageText("Nezačíná")]
        DxFilterOperatorDoesNotStartWithText,
        [DefaultMessageText("Vybere ty položky, jejichž text začíná jinak, než je zadáno")]
        DxFilterOperatorDoesNotStartWithTip,
        [DefaultMessageText("Končí")]
        DxFilterOperatorEndWithText,
        [DefaultMessageText("Vybere ty položky, jejichž text končí zadaným textem")]
        DxFilterOperatorEndWithTip,
        [DefaultMessageText("Nekončí")]
        DxFilterOperatorDoesNotEndWithText,
        [DefaultMessageText("Vybere ty položky, jejichž text končí jinak, než je zadáno")]
        DxFilterOperatorDoesNotEndWithTip,

        [DefaultMessageText("Podobá se")]
        DxFilterOperatorLikeText,
        [DefaultMessageText("?")]
        DxFilterOperatorLikeTip,
        [DefaultMessageText("Nepodobá se")]
        DxFilterOperatorNotLikeText,
        [DefaultMessageText("?")]
        DxFilterOperatorNotLikeTip,
        [DefaultMessageText("Odpovídá")]
        DxFilterOperatorMatchText,
        [DefaultMessageText("?")]
        DxFilterOperatorMatchTip,
        [DefaultMessageText("Neodpovídá")]
        DxFilterOperatorDoesNotMatchText,
        [DefaultMessageText("?")]
        DxFilterOperatorDoesNotMatchTip,

        [DefaultMessageText("Menší než")]
        DxFilterOperatorLessThanText,
        [DefaultMessageText("Hodnoty menší než zadaná hodnota")]
        DxFilterOperatorLessThanTip,
        [DefaultMessageText("Menší nebo rovno")]
        DxFilterOperatorLessThanOrEqualToText,
        [DefaultMessageText("Hodnoty menší nebo rovny jako zadaná hodnota")]
        DxFilterOperatorLessThanOrEqualToTip,
        [DefaultMessageText("Rovno")]
        DxFilterOperatorEqualsText,
        [DefaultMessageText("Hodnoty rovné dané hodnotě")]
        DxFilterOperatorEqualsTip,
        [DefaultMessageText("Nerovno")]
        DxFilterOperatorNotEqualsText,
        [DefaultMessageText("Hodnoty jiné než je daná hodnota")]
        DxFilterOperatorNotEqualsTip,
        [DefaultMessageText("Větší nebo rovno")]
        DxFilterOperatorGreaterThanOrEqualToText,
        [DefaultMessageText("Hodnoty větší nebo rovny jako zadaná hodnota")]
        DxFilterOperatorGreaterThanOrEqualToTip,
        [DefaultMessageText("Větší než")]
        DxFilterOperatorGreaterThanText,
        [DefaultMessageText("Hodnoty větší než zadaná hodnota")]
        DxFilterOperatorGreaterThanTip
       

        // Nové kódy přidej do Messages.xml v klientu!!!     Do AdapterTest.cs není nutno, tam se načítá hodnota atributu DefaultMessageText() !
    }
    /// <summary>
    /// Defaultní text této hlášky ve výchozím jazyce
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DefaultMessageTextAttribute : Attribute
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="defaultText"></param>
        public DefaultMessageTextAttribute(string defaultText)
        {
            DefaultText = defaultText;
        }
        /// <summary>
        /// Defaultní text této hlášky ve výchozím jazyce
        /// </summary>
        public string DefaultText { get; set; }
    }
    #endregion
    #region interface IListener... : předpis pro odběratele systémových události
    /// <summary>
    /// Formální interface, předek pro konkrétní listenery
    /// </summary>
    public interface IListener
    { }
    /// <summary>
    /// Interface pro listener události Změna Zoomu
    /// </summary>
    public interface IListenerZoomChange : IListener
    {
        /// <summary>
        /// Metoda je volaná po změně Zoomu do všech instancí, které se zaregistrovaly pomocí <see cref="DxComponent.RegisterListener"/>
        /// </summary>
        void ZoomChanged();
    }
    /// <summary>
    /// Interface pro listener události Změna Skinu/Stylu
    /// Tato událost se volá před událostí <see cref="IListenerLightDarkChanged.LightDarkChanged"/>.
    /// </summary>
    public interface IListenerStyleChanged : IListener
    {
        /// <summary>
        /// Metoda je volaná po změně stylu do všech instancí, které se zaregistrovaly pomocí <see cref="DxComponent.RegisterListener"/>
        /// Tato událost se volá před událostí <see cref="IListenerLightDarkChanged.LightDarkChanged"/>.
        /// </summary>
        void StyleChanged();
    }
    /// <summary>
    /// Interface pro listener události Změna světlého/tmavého Skinu/Stylu.
    /// Nebude dostávat událost vždycky po změně skinu, ale jen když se změní tmavý za světý skin.
    /// Tato událost se volá až po proběhnutí události <see cref="IListenerStyleChanged.StyleChanged"/>.
    /// </summary>
    public interface IListenerLightDarkChanged : IListener
    {
        /// <summary>
        /// Metoda je volaná po změně světlého/tmavého stylu do všech instancí, které se zaregistrovaly pomocí <see cref="DxComponent.RegisterListener"/>
        /// Tato událost se volá až po proběhnutí události <see cref="IListenerStyleChanged.StyleChanged"/>.
        /// </summary>
        void LightDarkChanged();
    }
    /// <summary>
    /// Interface pro listener události <see cref="Application.Idle"/>
    /// </summary>
    public interface IListenerApplicationIdle : IListener
    {
        /// <summary>
        /// Metoda je volaná v situaci, kdy aplikační kontext nemá co dělat
        /// </summary>
        void ApplicationIdle();
    }
    #endregion
    #region SystemAdapter - přístupový bod k adapteru na lokální systém
    /// <summary>
    /// Tato třída reprezentuje adapter na systémové metody.
    /// Prvky této třídy jsou volány z různých míst komponent AsolDX a jejich úkolem je převolat odpovídající metody konkrétního systému.
    /// K tomu účelu si zdejší třída vytvoří interní instanci konkrétního systému (zde NephriteAdapter).
    /// <para/>
    /// Komponenty volají statické metody <see cref="SystemAdapter"/>, ty uvnitř převolají odpovídající svoje instanční abstraktní metody, 
    /// které jsou implementované v konkrétním potomku adapteru.
    /// </summary>
    internal static class SystemAdapter
    {
        #region Zásuvný modul pro konkrétní systém
        /// <summary>
        /// Aktuální adapter
        /// </summary>
        private static ISystemAdapter Current
        {
            get
            {
                if (__Current == null)
                {
                    lock (__Lock)
                    {
                        if (__Current == null)
                            __Current = __CreateAdapter();
                    }
                }
                return __Current;
            }
        }
        /// <summary>
        /// Tato metoda vygeneruje a vrátí konkrétní adapter
        /// </summary>
        /// <returns></returns>
        private static ISystemAdapter __CreateAdapter() { return new CurrentSystemAdapter(); }
        private static ISystemAdapter __Current;
        private static object __Lock = new object();
        #endregion
        #region Služby pro komponenty, předpis abstraktních metod pro konkrétního potomka
        /// <summary>
        /// Událost, kdy systém změní Zoom
        /// </summary>
        public static event EventHandler InteractiveZoomChanged { add { Current.InteractiveZoomChanged += value; } remove { Current.InteractiveZoomChanged -= value; } }
        /// <summary>
        /// Aktuálně platný Zoom, kde 1.0 = 100%; 1.25 = 125% atd
        /// </summary>
        public static decimal ZoomRatio { get { return Current.ZoomRatio; } }
        /// <summary>
        /// Lokalizace daného stringu a parametrů
        /// </summary>
        /// <param name="messageCode"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string GetMessage(MsgCode messageCode, params object[] parameters) { return Current.GetMessage(messageCode, parameters); }
        /// <summary>
        /// Obsahuje true, pokud jsou preferovány vektorové ikony
        /// </summary>
        public static bool IsPreferredVectorImage { get { return Current.IsPreferredVectorImage; } }
        /// <summary>
        /// Standardní velikost ikony
        /// </summary>
        public static ResourceImageSizeType ImageSizeStandard { get { return Current.ImageSizeStandard; } }
        /// <summary>
        /// Volá se jedenkrát, vrátí kompletní seznam všech zdrojů (Resource).
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IResourceItem> GetResources() { return Current.GetResources(); }
        /// <summary>
        /// Vrátí konkrétní jméno zdroje z dodaného plného jména zdroje (ponechá velikost i typ souboru, ale upraví Trim a Case)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetResourceItemKey(string name) { return Current.GetResourceItemKey(name); }
        /// <summary>
        /// Vrátí obecné jméno zdroje z dodaného plného jména zdroje (oddělí velikost a typ souboru podle suffixu a přípony)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sizeType"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static string GetResourcePackKey(string name, out ResourceImageSizeType sizeType, out ResourceContentType contentType) { return Current.GetResourcePackKey(name, out sizeType, out contentType); }
        /// <summary>
        /// Volá se v případě potřeby pro získání obsahu daného Resource.
        /// </summary>
        /// <param name="resourceItem"></param>
        /// <returns></returns>
        public static byte[] GetResourceContent(IResourceItem resourceItem) { return Current.GetResourceContent(resourceItem); }
        /// <summary>
        /// Nějaký control, který slouží pouze pro přístup do GUI threadu. Typicky je to main okno aplikace.
        /// </summary>
        public static System.ComponentModel.ISynchronizeInvoke Host { get { return Current.Host; } }
        /// <summary>
        /// Vrací klávesovou zkratku pro daný string, typicky na vstupu je "Ctrl+C", na výstupu je <see cref="System.Windows.Forms.Keys.Control"/> | <see cref="System.Windows.Forms.Keys.C"/>
        /// </summary>
        /// <param name="shortCut"></param>
        /// <returns></returns>
        public static System.Windows.Forms.Shortcut GetShortcutKeys(string shortCut) { return Current.GetShortcutKeys(shortCut); }
        /// <summary>
        /// Umí aktuální adapter renderovat SVG image do bitmapy?
        /// </summary>
        public static bool CanRenderSvgImages { get { return Current.CanRenderSvgImages; } }
        /// <summary>
        /// Vyrenderuje dodaný SVG obrázek do formátu bitmapy
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="size"></param>
        /// <param name="svgPalette"></param>
        /// <returns></returns>
        internal static Image RenderSvgImage(SvgImage svgImage, Size size, ISvgPaletteProvider svgPalette) { return Current.RenderSvgImage(svgImage, size, svgPalette); }
        #endregion
    }
    #region interface ISystemAdapter : Požadavky na adapter na support systém
    /// <summary>
    /// Požadavky na adapter na support systém
    /// </summary>
    internal interface ISystemAdapter
    {
        /// <summary>
        /// Událost, kdy systém změní Zoom
        /// </summary>
        event EventHandler InteractiveZoomChanged;
        /// <summary>
        /// Aktuálně platný Zoom, kde 1.0 = 100%; 1.25 = 125% atd
        /// </summary>
        decimal ZoomRatio { get; }
        /// <summary>
        /// Lokalizace daného stringu a parametrů
        /// </summary>
        /// <param name="messageCode"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        string GetMessage(MsgCode messageCode, params object[] parameters);
        /// <summary>
        /// Obsahuje true, pokud na klientu máme preferovat Vektorové ikony.
        /// </summary>
        bool IsPreferredVectorImage { get; }
        /// <summary>
        /// Standardní velikost ikony
        /// </summary>
        ResourceImageSizeType ImageSizeStandard { get; }
        /// <summary>
        /// Volá se jedenkrát, vrátí kompletní seznam všech zdrojů (Resource).
        /// </summary>
        /// <returns></returns>
        IEnumerable<IResourceItem> GetResources();
        /// <summary>
        /// Vrátí konkrétní jméno zdroje z dodaného plného jména zdroje (ponechá velikost i typ souboru, ale upraví Trim a Case)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string GetResourceItemKey(string name);
        /// <summary>
        /// Vrátí obecné jméno zdroje z dodaného plného jména zdroje (oddělí velikost a typ souboru podle suffixu a přípony)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sizeType"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        string GetResourcePackKey(string name, out ResourceImageSizeType sizeType, out ResourceContentType contentType);
        /// <summary>
        /// Volá se v případě potřeby pro získání obsahu daného Resource.
        /// </summary>
        /// <param name="resourceItem"></param>
        /// <returns></returns>
        byte[] GetResourceContent(IResourceItem resourceItem);
        /// <summary>
        /// Umí aktuální adapter renderovat SVG image do bitmapy?
        /// </summary>
        bool CanRenderSvgImages { get; }
        /// <summary>
        /// Vyrenedrovat dodaný SVG Image jako Bitmapu
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="size"></param>
        /// <param name="svgPalette"></param>
        /// <returns></returns>
        Image RenderSvgImage(SvgImage svgImage, Size size, ISvgPaletteProvider svgPalette);
        /// <summary>
        /// Nějaký control, který slouží pouze pro přístup do GUI threadu. Typicky je to main okno aplikace.
        /// </summary>
        System.ComponentModel.ISynchronizeInvoke Host { get; }
        /// <summary>
        /// Vrací klávesovou zkratku pro daný string, typicky na vstupu je "Ctrl+C", na výstupu je <see cref="System.Windows.Forms.Keys.Control"/> | <see cref="System.Windows.Forms.Keys.C"/>
        /// </summary>
        /// <param name="shortCut"></param>
        /// <returns></returns>
        Shortcut GetShortcutKeys(string shortCut);
    }
    /// <summary>
    /// Popis jednoho prvku resource (odpovídá typicky jednomu souboru, který obsahuje jednu verzi obrázku).
    /// Podle klíčů je ukládán a vyhledán a sdružen do balíček příbuzných souborů.
    /// Obsah resource <see cref="byte"/>[] vrátí metoda <see cref="ISystemAdapter.GetResourceContent(IResourceItem)"/> v aktuálním adapteru.
    /// </summary>
    public interface IResourceItem
    {
        /// <summary>
        /// Klíč používaný v aplikaci, exaktní, unikátní pro jeden soubor (obsahuje označení velikosti i příponu), typicky lowercase
        /// </summary>
        string ItemKey { get; }
        /// <summary>
        /// Klíč používaný v aplikaci, společný pro několik příbuzných souborů stejného významu ale různé velikosti (bez velikosti a bez přípony), typicky lowercase
        /// </summary>
        string PackKey { get; }
        /// <summary>
        /// Typ obsahu, běžně je odvozen od přípony souboru
        /// </summary>
        ResourceContentType ContentType { get; }
        /// <summary>
        /// Velikost obsahu, typicky bitmapy
        /// </summary>
        ResourceImageSizeType SizeType { get; }
    }
    #endregion
    #region Enumy
    /// <summary>
    /// Velikost obrázku typu Bitmapa
    /// </summary>
    public enum ResourceImageSizeType
    {
        /// <summary>None</summary>
        None = 0,
        /// <summary>Small (typicky 16 x 16)</summary>
        Small = 1,
        /// <summary>Medium (typicky 24 x 24)</summary>
        Medium = 2,
        /// <summary>Large (typicky 32 x 32)</summary>
        Large = 3
    }
    /// <summary>
    /// Druh obsahu resource
    /// </summary>
    public enum ResourceContentType
    {
        /// <summary>
        /// Nejde o platný Resource (neznámá přípona)
        /// </summary>
        None = 0,
        /// <summary>
        /// Bitmapa (BMP, JPG, PNG, PCX, GIF, TIF)
        /// </summary>
        Bitmap,
        /// <summary>
        /// Vektor (SVG)
        /// </summary>
        Vector,
        /// <summary>
        /// Video (MP4, AVI, MPEG)
        /// </summary>
        Video,
        /// <summary>
        /// Audio (MP3, WAV, FLAC)
        /// </summary>
        Audio,
        /// <summary>
        /// Ikona (ICO)
        /// </summary>
        Icon,
        /// <summary>
        /// Kurzor (CUR)
        /// </summary>
        Cursor,
        /// <summary>
        /// Xml (XML, HTML)
        /// </summary>
        Xml
    }
    #endregion
    #endregion
    #region class Algebra
    public class Algebra
    {
        /// <summary>
        /// Vrátí instanci lineární rovnice, která vypočítá výsledek Y = fn(X) podle vzorce Y = a + b*X.
        /// Na vstupu jsou zadané dva standardní parametry a, b které budou použity v rovnici.
        /// Tuto definici rovnice nelze z principu použít na rovnici popisující svislou funkci (kde X je konstanta). Tam je třeba použít definici se dvěma body.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static LinearEquation GetLinearEquation(double a, double b) { return LinearEquation.CreateEquation(a, b); }
        /// <summary>
        /// Vrátí instanci lineární rovnice, která vypočítá výsledek Y = fn(X) podle vzorce Y = a + b*X.
        /// Na vstupu jsou zadané dva body, kterými prochází přímka lineární rovnice. Zadané body nesmí být identické, pak dojde k chybě.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static LinearEquation GetLinearEquation(double x1, double y1, double x2, double y2) { return LinearEquation.CreateEquation(x1, y1, x2, y2); }
        #region LinearEquation
        /// <summary>
        /// Lineární rovnice = pro dané X najdi Y.
        /// Zadání rovnice: parametrické nebo dvojbodové.
        /// </summary>
        public class LinearEquation
        {
            /// <summary>
            /// Vrátí instanci lineární rovnice, která vypočítá výsledek Y = fn(X) podle vzorce Y = a + b*X.
            /// Na vstupu jsou zadané dva standardní parametry a, b které budou použity v rovnici.
            /// Tuto definici rovnice nelze z principu použít na rovnici popisující svislou funkci (kde X je konstanta). Tam je třeba použít definici se dvěma body.
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            public static LinearEquation CreateEquation(double a, double b)
            {
                double? x = null;
                double? y = (b == 0d ? (double?)a : (double?)null);
                return new LinearEquation(a, b, x, y);
            }
            /// <summary>
            /// Vrátí instanci lineární rovnice, která vypočítá výsledek Y = fn(X) podle vzorce Y = a + b*X.
            /// Na vstupu jsou zadané dva body, kterými prochází přímka lineární rovnice. Zadané body nesmí být identické, pak dojde k chybě.
            /// </summary>
            /// <param name="x1"></param>
            /// <param name="y1"></param>
            /// <param name="x2"></param>
            /// <param name="y2"></param>
            /// <returns></returns>
            public static LinearEquation CreateEquation(double x1, double y1, double x2, double y2)
            {

                double dx = x2 - x1;
                double dy = y2 - y1;
                bool zx = (dx == 0d);
                bool zy = (dy == 0d);
                if (zx && zy)
                    throw new ArgumentException("A linear equation cannot be defined by two identical points.");
                if (zx)
                {   // dx = 0: body 1 a 2 jsou nad sebou, X je konstantní:
                    return new LinearEquation(0d, 0d, x1, null);     // Rovnice s konstantním X
                }
                else if (zy)
                {   // dy = 0: body 1 a 2 jsou vedle sebe, Y je konstantní:
                    return new LinearEquation(0d, 0d, null, y1);     // Rovnice s konstantním Y
                }
                // Funkce má standardní průběh (ani svislý, ani vodorovný : dx i dy jsou nenulové):
                double b = dy / dx;                                  // Poměr přírůstku Y ku poměru přírůstku X = směrník přímky
                double a = y1 - (b * x1);                            // Hodnota Y v souřadnici X = 0
                return new LinearEquation(a, b, null, null);         // Rovnice dle vzorce Y = A + B * X
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            private LinearEquation(double a, double b, double? x, double? y)
            {
                _A = a;
                _B = b;
                _X = x;
                _Y = y;
            }
            private double _A;
            private double _B;
            /// <summary>
            /// Konstantní hodnota X pro všechna Y = svislý průběh funkce, nezávislý na hodnotě Y.
            /// Pokud je null, pak X není konstantní = pak lze použít funkci <see cref="GetY(double)"/> pro získání hodnoty X pro určitou hodnotu Y.
            /// </summary>
            private double? _X;
            /// <summary>
            /// Konstantní hodnota Y pro všechna X = vodorovný průběh funkce, nezávislý na hodnotě X.
            /// Pokud je null, pak Y není konstantní = pak lze použít funkci <see cref="GetX(double)"/> pro získání hodnoty Y pro určitou hodnotu X.
            /// </summary>
            private double? _Y;
            /// <summary>
            /// Konstantní hodnota X, má hodnotu když funkce je svislá = hodnota X se nemění při změně hodnoty Y.
            /// </summary>
            public double? ConstantX { get { return _X; } }
            /// <summary>
            /// Obsahuje true, když hodnota X je konstantní (svislý průběh funkce). Pak nelze použít funkci <see cref="GetY(double)"/>.
            /// </summary>
            public bool IsConstantX { get { return _X.HasValue; } }
            /// <summary>
            /// Konstantní hodnota Y, má hodnotu když funkce je vodorovná = hodnota Y se nemění při změně hodnoty X.
            /// </summary>
            public double? ConstantY { get { return _Y; } }
            /// <summary>
            /// Obsahuje true, když hodnota Y je konstantní (vodorovný průběh funkce). Pak nelze použít funkci <see cref="GetX(double)"/>.
            /// </summary>
            public bool IsConstantY { get { return _Y.HasValue; } }
            /// <summary>
            /// Vrátí X pro tuto lineární rovnici pro zadané Y.
            /// Pokud je použito na rovnici, která je vodorovná (má <see cref="IsConstantY"/> == true), pak dojde k chybě.
            /// </summary>
            /// <param name="y"></param>
            /// <returns></returns>
            public double GetX(double y)
            {
                if (IsConstantY)
                    throw new InvalidOperationException("The linear equation cannot determine X for the given Y, the function has a constant Y.");
                if (IsConstantX)
                    return ConstantX.Value;
                return (y - _A) / _B;
            }
            /// <summary>
            /// Vrátí Y pro tuto lineární rovnici pro zadané X.
            /// Pokud je použito na rovnici, která je svislá (má <see cref="IsConstantX"/> == true), pak dojde k chybě.
            /// </summary>
            /// <param name="x"></param>
            /// <returns></returns>
            public double GetY(double x)
            {
                if (IsConstantX)
                    throw new InvalidOperationException("The linear equation cannot determine Y for the given X, the function has a constant X.");
                if (IsConstantY)
                    return ConstantY.Value;
                return _A + _B * x;
            }
        }
        #endregion
    }
    #endregion
    #region class ConvertFormat : konverze textu / RTF / HTML
    /// <summary>
    /// Třída pro konverze mezi formáty TXT - HTML - RTF
    /// </summary>
    public static class ConvertFormat
    {
        /// <summary>
        /// Vrátí PlainText z daného HTML textu
        /// </summary>
        /// <param name="textHtml"></param>
        /// <param name="acceptEolAsBr"></param>
        /// <returns></returns>
        public static string HtmlToText(string textHtml, bool acceptEolAsBr = false)
        {
            if (String.IsNullOrEmpty(textHtml)) return "";
            if (acceptEolAsBr || (textHtml.Contains("\r\n") && !textHtml.Contains("<br>")))
                textHtml = textHtml.Replace("\r\n", "<br>");

            string result = null;
            using (var textServer = new DevExpress.XtraRichEdit.RichEditDocumentServer())
            {
                textServer.HtmlText = textHtml;
                result = textServer.Text;
            }

            return result;
        }
        /// <summary>
        /// Vrátí RTF text z daného HTML textu
        /// </summary>
        /// <param name="textHtml"></param>
        /// <param name="acceptEolAsBr"></param>
        /// <returns></returns>
        public static string HtmlToRtf(string textHtml, bool acceptEolAsBr = false)
        {
            if (String.IsNullOrEmpty(textHtml)) return "";
            if (acceptEolAsBr || (textHtml.Contains("\r\n") && !textHtml.Contains("<br>")))
                textHtml = textHtml.Replace("\r\n", "<br>");

            string result = null;
            using (var textServer = new DevExpress.XtraRichEdit.RichEditDocumentServer())
            {
                textServer.HtmlText = textHtml;
                result = textServer.RtfText;
            }

            return result;
        }
        /// <summary>
        /// Vrátí PlainText z daného RTF textu
        /// </summary>
        /// <param name="textRtf"></param>
        /// <returns></returns>
        public static string RtfToText(string textRtf)
        {
            if (String.IsNullOrEmpty(textRtf)) return "";

            string result = null;
            using (var textServer = new DevExpress.XtraRichEdit.RichEditDocumentServer())
            {
                textServer.RtfText = textRtf;
                result = textServer.Text;
            }

            return result;
        }
        /// <summary>
        /// Vrátí HTML text z daného RTF textu
        /// </summary>
        /// <param name="textRtf"></param>
        /// <returns></returns>
        public static string RtfToHtml(string textRtf)
        {
            if (String.IsNullOrEmpty(textRtf)) return "";

            string result = null;
            using (var textServer = new DevExpress.XtraRichEdit.RichEditDocumentServer())
            {
                textServer.RtfText = textRtf;
                result = textServer.HtmlText;
            }

            return result;
        }
    }
    #endregion
    #region class ActionScope : Jednoduchý scope, který provede při vytvoření akci OnBegin, a při Dispose akci OnEnd
    /// <summary>
    /// <see cref="ActionScope"/> : Jednoduchý scope, který provede při vytvoření akci OnBegin, a při Dispose akci OnEnd.
    /// </summary>
    internal class ActionScope : IDisposable
    {
        /// <summary>
        /// Jednoduchý scope, který provede při vytvoření akci OnBegin, a při Dispose akci OnEnd.
        /// </summary>
        /// <param name="onBegin">Jako parametr je předán this scope, lze v něm použít property <see cref="UserData"/> pro uložení dat, budou k dispozici v akci <paramref name="onEnd"/></param>
        /// <param name="onEnd">Jako parametr je předán this scope, lze v něm použít property <see cref="UserData"/> pro čtení dat uložených v akci <paramref name="onBegin"/></param>
        /// <param name="userData">Volitelně UserData</param>
        public ActionScope(Action<ActionScope> onBegin, Action<ActionScope> onEnd, object userData = null)
        {
            this.UserData = userData;
            _OnEnd = onEnd;
            onBegin?.Invoke(this);
        }
        private Action<ActionScope> _OnEnd;
        void IDisposable.Dispose()
        {
            _OnEnd?.Invoke(this);
            _OnEnd = null;
        }
        /// <summary>
        /// Libovolná data.
        /// Typicky jsou vloženy v akci OnBegin, a v akci OnEnd jsou načteny. Výchozí hodnota je null.
        /// </summary>
        public object UserData { get; set; }
    }
    #endregion
    #region class RegexSupport : Třída pro podporu konverze Wildcard patternu na "Regex"
    /// <summary>
    /// Třída pro podporu konverze Wildcard patternu na <see cref="Regex"/>
    /// </summary>
    public static class RegexSupport
    {
        /// <summary>
        /// Metoda vrátí true, pokud hodnota value vyhovuje vzorci pattern.
        /// Pro opakované používání stejného patternu je vhodnější získat <see cref="Regex"/> metodou <see cref="CreateWildcardsRegex(string, bool, bool)"/>,
        /// a ten pak používat pro testování různých hodnot opakovaně.
        /// </summary>
        /// <param name="value">Hodnota, například "Abcdef ghij"</param>
        /// <param name="pattern">Vzorec, například "Abc??f *"</param>
        /// <param name="matchCase">Kontrolovat shodu velikosti písmen, default = false: "abc" == "ABc"</param>
        /// <param name="checkIllegalFileCharacters">Vyhodit chybu, pokud pattern obsahuje zakázané znaky pro jména souborů, default = false: nekontrolovat</param>
        /// <returns></returns>
        public static bool IsMatchWildcards(string value, string pattern, bool matchCase = false, bool checkIllegalFileCharacters = false)
        {
            if (!IsWildcardsValid(pattern, checkIllegalFileCharacters)) return false;
            Regex regex = CreateWildcardsRegex(pattern, matchCase, checkIllegalFileCharacters);
            return regex.IsMatch(value);
        }
        /// <summary>
        /// Metoda vrátí true, pokud daný pattern je formálně správný a může být použit v metodě <see cref="CreateWildcardsRegex(string, bool, bool)"/>.
        /// </summary>
        /// <param name="pattern">Pattern s užitím standardních Wildcards * a ?</param>
        /// <param name="checkIllegalFileCharacters">Vyhodit chybu, pokud pattern obsahuje zakázané znaky pro jména souborů, default = false: nekontrolovat</param>
        /// <returns></returns>
        public static bool IsWildcardsValid(string pattern, bool checkIllegalFileCharacters = false)
        {
            if (pattern == null) return false;
            pattern = pattern.Trim();
            if (pattern.Length == 0) return false;
            if (checkIllegalFileCharacters && IllegalCharactersRegex.IsMatch(pattern)) return false;
            return true;
        }
        /// <summary>
        /// Metoda vrátí pole <see cref="Regex"/>, které dovolují porovnávat konkrétní texty se standardní Wildcards notací.
        /// Z dodané sady wildcard masek (odděleny středníkem) vrátí pole Regex výrazů pro jejich filtrování.
        /// Pokud je na vstupu Empty, vrací prázdné pole.
        /// Typický vstup: "*.tmp; *.js; *thumb*.*; *.htm*;" atd
        /// Tedy: text "Abcdefg" vyhovuje patternu "Ab??e*".
        /// Volitelně lze požádat (<paramref name="matchCase"/>), aby <see cref="Regex"/> měl vypnutou/zapnutou option <see cref="RegexOptions.IgnoreCase"/>: 
        /// false = ignoruje velikost znaků, true = neignoruje, porovnává exaktně
        /// </summary>
        /// <param name="patterns">Patterny s užitím standardních Wildcards * a ?, oddělené středníkem (nebo čárkami)</param>
        /// <param name="matchCase">Kontrolovat shodu velikosti písmen, default = false: "abc" == "ABc"</param>
        /// <param name="checkIllegalFileCharacters">Vyhodit chybu, pokud pattern obsahuje zakázané znaky pro jména souborů, default = false: nekontrolovat</param>
        /// <returns></returns>
        public static Regex[] CreateWildcardsRegexes(string patterns, bool matchCase = false, bool checkIllegalFileCharacters = false)
        {
            string[] masks = patterns.Trim().Split(';');         // Prioritní oddělovač je středník
            if (masks.Length == 1 && patterns.Contains(','))     // Pokud není přítomen středník, ale je přítomna čárka...
                masks = patterns.Trim().Split(',');              //  ... pak i čárka může hrát roli oddělovače

            return CreateWildcardsRegexes(masks, matchCase, checkIllegalFileCharacters);
        }
        /// <summary>
        /// Metoda vrátí pole <see cref="Regex"/>, které dovolují porovnávat konkrétní texty se standardní Wildcards notací.
        /// Z dodané sady wildcard masek (odděleny středníkem) vrátí pole Regex výrazů pro jejich filtrování.
        /// Pokud je na vstupu Empty, vrací prázdné pole.
        /// Typický vstup: "*.tmp; *.js; *thumb*.*; *.htm*;" atd
        /// Tedy: text "Abcdefg" vyhovuje patternu "Ab??e*".
        /// Volitelně lze požádat (<paramref name="matchCase"/>), aby <see cref="Regex"/> měl vypnutou/zapnutou option <see cref="RegexOptions.IgnoreCase"/>: 
        /// false = ignoruje velikost znaků, true = neignoruje, porovnává exaktně
        /// </summary>
        /// <param name="patterns">Patterny s užitím standardních Wildcards * a ?, zde již rozdělené do pole jednotlivých patternů</param>
        /// <param name="matchCase">Kontrolovat shodu velikosti písmen, default = false: "abc" == "ABc"</param>
        /// <param name="checkIllegalFileCharacters">Vyhodit chybu, pokud pattern obsahuje zakázané znaky pro jména souborů</param>
        /// <returns></returns>
        public static Regex[] CreateWildcardsRegexes(string[] patterns, bool matchCase = false, bool checkIllegalFileCharacters = false)
        {
            List<Regex> regexes = new List<Regex>();
            if (patterns != null && patterns.Length > 0)
            {
                foreach (string pattern in patterns)
                {
                    if (!String.IsNullOrEmpty(pattern))
                    {
                        Regex regex = CreateWildcardsRegex(pattern.Trim(), matchCase, checkIllegalFileCharacters);
                        if (regex != null)
                            regexes.Add(regex);
                    }
                }
            }

            return regexes.ToArray();
        }
        /// <summary>
        /// Vrátí dodanou kolekci textů filtrovanou podle dané kolekce regulárních výrazů.
        /// Kolekce <paramref name="data"/> typicky obsahuje seznam souborů nebo názvů;
        /// kolekce <paramref name="regexes"/> obsahuje výstup zdejší metody <see cref="CreateWildcardsRegexes(string, bool, bool)"/>;
        /// výstup zdejší metody pak obsahuje jen vyhovující soubory.
        /// <para/>
        /// Pokud <paramref name="data"/> je null, výstupem je null.
        /// Pokud <paramref name="regexes"/> je null nebo prázdná kolekce, pak výstupem je vstupní kolekce <paramref name="data"/>.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="regexes"></param>
        /// <returns></returns>
        public static IEnumerable<string> FilterByRegexes(IEnumerable<string> data, IEnumerable<Regex> regexes)
        {
            if (data == null) return null;
            if (regexes == null || regexes.Count() == 0) return data;
            return data.Where(t => IsTextMatchToAny(t, regexes));
        }
        /// <summary>
        /// Vrátí true, pokud daný text vyhovuje některé masce
        /// </summary>
        /// <param name="text"></param>
        /// <param name="regexes"></param>
        /// <returns></returns>
        public static bool IsTextMatchToAny(string text, IEnumerable<Regex> regexes)
        {
            if (text == null) return false;
            if (regexes == null) return false;
            return regexes.Any(mask => mask.IsMatch(text));
        }
        /// <summary>
        /// Metoda vrátí <see cref="Regex"/>, který dovoluje porovnávat texty se standardní Wildcards notací.
        /// Tedy: text "Abcdefg" vyhovuje patternu "Ab??e*".
        /// Volitelně lze požádat (<paramref name="matchCase"/>), aby <see cref="Regex"/> měl vypnutou/zapnutou option <see cref="RegexOptions.IgnoreCase"/>: 
        /// false = ignoruje velikost znaků, true = neignoruje, porovnává exaktně
        /// </summary>
        /// <param name="pattern">Pattern s užitím standardních Wildcards * a ?</param>
        /// <param name="matchCase">Kontrolovat shodu velikosti písmen, default = false: "abc" == "ABc"</param>
        /// <param name="checkIllegalFileCharacters">Vyhodit chybu, pokud pattern obsahuje zakázané znaky pro jména souborů</param>
        /// <returns></returns>
        public static Regex CreateWildcardsRegex(string pattern, bool matchCase = false, bool checkIllegalFileCharacters = false)
        {
            if (pattern == null) throw new ArgumentNullException();

            pattern = pattern.Trim();
            if (pattern.Length == 0) throw new ArgumentException("Pattern is empty.");

            if (checkIllegalFileCharacters && IllegalCharactersRegex.IsMatch(pattern)) throw new ArgumentException("Pattern contains illegal characters.");

            bool hasExtension = CatchExtentionRegex.IsMatch(pattern);
            bool matchExact = false;
            if (HasQuestionMarkRegEx.IsMatch(pattern))
                matchExact = true;
            else if (hasExtension)
                matchExact = CatchExtentionRegex.Match(pattern).Groups[1].Length != 3;

            string regexString = Regex.Escape(pattern);
            regexString = "^" + Regex.Replace(regexString, @"\\\*", ".*");
            regexString = Regex.Replace(regexString, @"\\\?", ".");
            if (!matchExact && hasExtension)
            {
                regexString += NonDotCharacters;
            }
            regexString += "$";
            RegexOptions regexOptions = (matchCase ? RegexOptions.Compiled : RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Regex regex = new Regex(regexString, regexOptions);
            return regex;
        }
        /// <summary>
        /// Pokud výraz obsahuje tzv. "Green" wildcards (% nebo _) a přitom neobsahuje standardní wildcards (* a ?), pak je nahradí standardními (* a ?)
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static string ReplaceGreenWildcards(string filter)
        {
            if (String.IsNullOrEmpty(filter)) return "";
            bool containsGreen = (filter.Contains("%") || filter.Contains("_"));
            bool containsStandard = (filter.Contains("*") || filter.Contains("?"));

            if (containsGreen && !containsStandard)
                filter = filter
                    .Replace('%', '*')
                    .Replace('_', '?');

            return filter;
        }
        private static Regex HasQuestionMarkRegEx = new Regex(@"\?", RegexOptions.Compiled);
        private static Regex IllegalCharactersRegex = new Regex("[" + @"\/:<>|" + "\"]", RegexOptions.Compiled);
        private static Regex CatchExtentionRegex = new Regex(@"^\s*.+\.([^\.]+)\s*$", RegexOptions.Compiled);
        private static string NonDotCharacters = @"[^.]*";
    }
    #endregion
    #region class WeakTarget<T> : Typová obálka nad WeakReference
    /// <summary>
    /// Typová obálka nad <see cref="WeakReference"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WeakTarget<T> where T : class
    {
        /// <summary>
        /// Konstruktor. Lze použít i implicitní konverzi:
        /// <see cref="WeakTarget{T}"/> wt = (T)target; a následně: T target2 = wt.Target;
        /// </summary>
        /// <param name="target"></param>
        public WeakTarget(T target)
        {
            this._Wref = (target != null ? new WeakReference(target) : null);
        }
        private readonly WeakReference _Wref;
        /// <summary>
        /// Obsahuje true pokud cíl je nyní dostupný a je správného typu
        /// </summary>
        public bool IsAlive { get { return ((this._Wref?.IsAlive ?? false) ? (this._Wref.Target is T) : false); } }
        /// <summary>
        /// Cíl daného typu
        /// </summary>
        public T Target { get { return ((this.IsAlive) ? this._Wref.Target as T : null); } }
        /// <summary>
        /// Implicitní konverze z typového WeakTargetu na originální objekt daného typu
        /// </summary>
        /// <param name="target"></param>
        public static implicit operator T(WeakTarget<T> target) { return target?.Target; }
        /// <summary>
        /// Implicitní konverze z originálního objektu daného typu na typový WeakTarget
        /// </summary>
        /// <param name="source"></param>
        public static implicit operator WeakTarget<T>(T source) { return (source is null ? null : new WeakTarget<T>(source)); }
    }
    #endregion
    #region class TEventArgs<T> : Třída argumentů obsahující jeden prvek generického typu Item
    /// <summary>
    /// Třída argumentů obsahující jeden prvek <see cref="Item"/> generického typu <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Libovolný typ, třída na něj nemá žádné požadavky</typeparam>
    public class TEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="item"></param>
        public TEventArgs(T item) { Item = item; }
        /// <summary>
        /// Konkrétní datový prvek
        /// </summary>
        public T Item { get; private set; }
    }
    /// <summary>
    /// Třída argumentů obsahující jeden prvek <see cref="Item"/> generického typu <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Libovolný typ, třída na něj nemá žádné požadavky</typeparam>
    public class TEventCancelArgs<T> : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="item"></param>
        public TEventCancelArgs(T item) { Item = item; Cancel = false; }
        /// <summary>
        /// Konkrétní datový prvek
        /// </summary>
        public T Item { get; private set; }
        /// <summary>
        /// Požadavek true na zrušení akce, default = false = akce proběhne (není zakázaná)
        /// </summary>
        public bool Cancel { get; set; }
    }
    #endregion
    #region class TEventValueChangeArgs<T> : Třída argumentů obsahující dva prvky generického typu OldValue a NewValue
    /// <summary>
    /// Třída argumentů obsahující dva prvky generického typu <typeparamref name="T"/> s charakterem 
    /// Původní hodnota <see cref="OldValue"/> a Nová hodnota <see cref="NewValue"/>.
    /// Novou hodnotu <see cref="NewValue"/> lze upravit (setovat), a zdroj eventu ji možná převezme.
    /// Lze nastavit <see cref="Cancel"/> = true, a zdroj eventu možná akci zruší.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TEventValueChangeArgs<T> : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="source">Zdroj události</param>
        /// <param name="oldValue">Původní hodnota</param>
        /// <param name="newValue">Nová hodnota</param>
        public TEventValueChangeArgs(EventSource source, T oldValue, T newValue) { Source = source; OldValue = oldValue; _NewValue = newValue; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Change from: {OldValue}, to: {NewValue}, source: {Source}";
        }
        /// <summary>
        /// Zdroj události
        /// </summary>
        public EventSource Source { get; private set; }
        /// <summary>
        /// Původní hodnota. Nelze změnit.
        /// </summary>
        public T OldValue { get; private set; }
        /// <summary>
        /// Nová hodnota. 
        /// Hodnotu lze změnit, a zdroj eventu ji možná převezme.
        /// Vložením hodnoty dojde k nastavení <see cref="Changed"/> na true.
        /// </summary>
        public T NewValue { get { return _NewValue; } set { Changed = true; _NewValue = value; } }
        private T _NewValue;
        /// <summary>
        /// Zrušit událost? default = false, lze nastavit.
        /// </summary>
        public bool Cancel { get; set; } = false;
        /// <summary>
        /// Bude nastaveno na true poté, kdy aplikace vloží novou hodnotu do <see cref="NewValue"/>.
        /// A to bez ohledu na změnu hodnoty.
        /// </summary>
        public bool Changed { get; private set; } = false;
    }
    /// <summary>
    /// Zdroj eventu
    /// </summary>
    public enum EventSource
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Zásah kódu
        /// </summary>
        Code,
        /// <summary>
        /// Interaktivní akce uživatele
        /// </summary>
        User
    }
    /// <summary>
    /// Směr gradientu
    /// </summary>
    public enum GradientStyleType
    {
        /// <summary>
        /// Není gradient
        /// </summary>
        None = 0,
        /// <summary>
        /// Dolů
        /// </summary>
        Downward,
        /// <summary>
        /// Nahoru
        /// </summary>
        Upward,
        /// <summary>
        /// Doprava
        /// </summary>
        ToRight,
        /// <summary>
        /// Doleva
        /// </summary>
        ToLeft
    }
    /// <summary>
    /// 3D efekt gradientu
    /// </summary>
    public enum Gradient3DEffectType
    {
        /// <summary>
        /// Plochý
        /// </summary>
        None,
        /// <summary>
        /// Dovnitř (tmavá je vlevo/nahoře, světlá je vpravo/dole)
        /// </summary>
        Inset,
        /// <summary>
        /// Vně (světlá je vlevo/nahoře, tmavá je vpravo/dole)
        /// </summary>
        Outward
    }
    #endregion
}
