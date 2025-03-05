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
using DevExpress.XtraEditors;
using WSXmlSerializer = Noris.WS.Parser.XmlSerializer;
using DevExpress.Utils.Svg;
using DevExpress.Utils.Design;
using System.Globalization;


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
                            _Instance._Prepare();
                        }
                    }
                }
                return _Instance;
            }
        }
        /// <summary>
        /// Konstruktor. V době jeho provádění se nesmí volat <see cref="DxComponent.Instance"/>!
        /// </summary>
        private DxComponent()
        {
            this._InitCore();
            this._InitLog();
            this._InitStyles();
            this._InitZoom();
            this._InitFontCache();
            this._InitSkinStaticInfo();
            this._InitDrawing();
            this._InitListeners();
            this._InitSvgConvertor();
            this._InitClipboard();
            this._InitAppEvents();
        }
        /// <summary>
        /// Příprava. V době provádění už se smí volat <see cref="DxComponent.Instance"/>
        /// </summary>
        private void _Prepare()
        {
            this._ReadSkinColorSet();
            this._PrepareImageLists();
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
            _AppArguments = Environment.GetCommandLineArgs();
            _IsTerminalServer = SystemInformation.TerminalServerSession;
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
        /// Obsahuje true pokud jsme aktuálně na terminal serveru.
        /// </summary>
        private bool _IsTerminalServer;
        /// <summary>
        /// Argumenty spuštění aplikace
        /// </summary>
        private string[] _AppArguments;
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
            this._TempDirectoryDone();
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
        /// Vrátí true, pokud mezi argumenty je některý s uvedeným textem
        /// </summary>
        /// <param name="argumentName"></param>
        /// <param name="caseSensitive"></param>
        /// <returns></returns>
        private bool _AppArgumentsContains(string argumentName, bool caseSensitive = false)
        {
            if (String.IsNullOrEmpty(argumentName)) return false;
            if (this._AppArguments is null || this._AppArguments.Length == 0) return false;
            var comparison = (caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
            return this._AppArguments.Any(a => a.IndexOf(argumentName, comparison) >= 0);
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
        /// Argumenty aplikace.
        /// An array of string where each element contains a command-line argument. 
        /// The first element is the executable file name, and the following zero or more elements contain the remaining command-line arguments.
        /// </summary>
        public static string[] ApplicationArguments { get { return Instance._AppArguments; } }
        /// <summary>
        /// Vrátí true, pokud mezi argumenty je některý s uvedeným textem
        /// </summary>
        /// <param name="argumentName"></param>
        /// <param name="caseSensitive"></param>
        /// <returns></returns>
        public static bool ApplicationArgumentsContains(string argumentName, bool caseSensitive = false) { return Instance._AppArgumentsContains(argumentName, caseSensitive); }
        /// <summary>
        /// Obsahuje true jen tehdy, když je aplikace v režimu ladění s debuggerem VisualStudia
        /// </summary>
        public static bool IsDebuggerActive { get { return System.Diagnostics.Debugger.IsAttached; } }
        /// <summary>
        /// Obsahuje true pokud jsme aktuálně na terminal serveru.
        /// Pak je vhodnější omezit grafické lahůdky, protože se musí přenášet po síti na klienta.
        /// </summary>
        public static bool IsTerminalServer { get { return Instance._IsTerminalServer; } }
        /// <summary>
        /// Hlavní okno aplikace, pokud byla aplikace spuštěna pomocí <see cref="ApplicationStart(Type, Image)"/>
        /// </summary>
        public static Form MainForm { get { return Instance._MainForm; } }
        /// <summary>
        /// Do daného Controlu nastaví daný Cursor. Zde se provádí konverze z hodnoty Enum na objekt Cursor, na defaultní systémové kurzory z <see cref="Cursors"/>.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="cursorType"></param>
        public static void SetCursorToControl(Control control, DxCursorType? cursorType)
        {
            if (control != null && control.IsHandleCreated && !control.Disposing && !control.IsDisposed)
            {
                if (cursorType.HasValue)
                {
                    switch (cursorType.Value)
                    {
                        case DxCursorType.Default:
                            control.Cursor = Cursors.Default;
                            break;
                        case DxCursorType.AppStarting:
                            control.Cursor = Cursors.AppStarting;
                            break;
                        case DxCursorType.PanSW:
                            control.Cursor = Cursors.PanSW;
                            break;
                        case DxCursorType.PanSouth:
                            control.Cursor = Cursors.PanSouth;
                            break;
                        case DxCursorType.PanSE:
                            control.Cursor = Cursors.PanSE;
                            break;
                        case DxCursorType.PanNW:
                            control.Cursor = Cursors.PanNW;
                            break;
                        case DxCursorType.PanNorth:
                            control.Cursor = Cursors.PanNorth;
                            break;
                        case DxCursorType.PanNE:
                            control.Cursor = Cursors.PanNE;
                            break;
                        case DxCursorType.PanEast:
                            control.Cursor = Cursors.PanEast;
                            break;
                        case DxCursorType.NoMoveVert:
                            control.Cursor = Cursors.NoMoveVert;
                            break;
                        case DxCursorType.NoMoveHoriz:
                            control.Cursor = Cursors.NoMoveHoriz;
                            break;
                        case DxCursorType.NoMove2D:
                            control.Cursor = Cursors.NoMove2D;
                            break;
                        case DxCursorType.VSplit:
                            control.Cursor = Cursors.VSplit;
                            break;
                        case DxCursorType.HSplit:
                            control.Cursor = Cursors.HSplit;
                            break;
                        case DxCursorType.Help:
                            control.Cursor = Cursors.Help;
                            break;
                        case DxCursorType.WaitCursor:
                            control.Cursor = Cursors.WaitCursor;
                            break;
                        case DxCursorType.UpArrow:
                            control.Cursor = Cursors.UpArrow;
                            break;
                        case DxCursorType.SizeWE:
                            control.Cursor = Cursors.SizeWE;
                            break;
                        case DxCursorType.SizeNWSE:
                            control.Cursor = Cursors.SizeNWSE;
                            break;
                        case DxCursorType.SizeNS:
                            control.Cursor = Cursors.SizeNS;
                            break;
                        case DxCursorType.SizeNESW:
                            control.Cursor = Cursors.SizeNESW;
                            break;
                        case DxCursorType.SizeAll:
                            control.Cursor = Cursors.SizeAll;
                            break;
                        case DxCursorType.No:
                            control.Cursor = Cursors.No;
                            break;
                        case DxCursorType.IBeam:
                            control.Cursor = Cursors.IBeam;
                            break;
                        case DxCursorType.Cross:
                            control.Cursor = Cursors.Cross;
                            break;
                        case DxCursorType.Arrow:
                            control.Cursor = Cursors.Arrow;
                            break;
                        case DxCursorType.PanWest:
                            control.Cursor = Cursors.PanWest;
                            break;
                        case DxCursorType.Hand:
                            control.Cursor = Cursors.Hand;
                            break;
                    }
                }
                else
                    control.Cursor = Cursors.Default;
            }
        }
        /// <summary>
        /// Zajistí provedení dodané akce v GUI threadu
        /// </summary>
        /// <param name="action">Požadovaná akce; null = nic</param>
        /// <param name="control">Optional Control, který řeší invokaci GUI</param>
        /// <param name="asyncInvoke">Požadavek true = na asynchronní invokování (BeginInvoke) / false = synchronní (Invoke)</param>
        public static void InvokeGuiThread(Action action, Control control = null, bool asyncInvoke = false) { Instance._InvokeGuiThread(action, control, asyncInvoke); }
        /// <summary>
        /// Start aplikace, nepoužívat z Nephrite
        /// </summary>
        /// <param name="mainFormType"></param>
        /// <param name="splashImage"></param>
        internal static void ApplicationStart(Type mainFormType, Image splashImage) { Instance._ApplicationStart(mainFormType, splashImage); }
        /// <summary>
        /// ReStart aplikace, nepoužívat z Nephrite
        /// </summary>
        internal static void ApplicationRestart() { Instance._ApplicationRestart(); }
        /// <summary>
        /// Start aplikace, nepoužívat z Nephrite
        /// </summary>
        /// <param name="mainFormType"></param>
        /// <param name="splashImage"></param>
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
        /// <summary>
        /// ReStart aplikace, nepoužívat z Nephrite
        /// </summary>
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
        /// <summary>
        /// Zajistí provedení dodané akce v GUI threadu
        /// </summary>
        /// <param name="action">Požadovaná akce; null = nic</param>
        /// <param name="control">Optional Control, který řeší invokaci GUI</param>
        /// <param name="asyncInvoke">Požadavek true = na asynchronní invokování (BeginInvoke) / false = synchronní (Invoke)</param>
        private void _InvokeGuiThread(Action action, Control control = null, bool asyncInvoke = false)
        {
            if (action is null) return;

            if (control is null) control = _MainForm;
            if (control != null && control.IsHandleCreated && !control.Disposing && !control.IsDisposed && control.InvokeRequired)
            {
                if (asyncInvoke)
                    control.BeginInvoke(new Action(action));
                else
                    control.Invoke(new Action(action));
            }
            else
                action();
        }
        /// <summary>
        /// Aplikace provádí restart
        /// </summary>
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
            _DefaultInnerMarginX = 6;
            _DefaultInnerMarginY = 4;
            _DefaultInnerSpacingW = 3;
            _DefaultInnerSpacingH = 3;

            _DefaultButtonPanelHeight = 38;
            _DefaultButtonWidth = 150;
            _DefaultButtonHeight = 30;
            _DefaultButtonXSpace = 6;
            _DefaultButtonYSpace = 6;

            __Zoom = 1m;
            __DesignDpi = 96;
            _RecalcZoomDpi();

            _DefaultBarManager = new DevExpress.XtraBars.BarManager();
            _DefaultToolTipController = CreateNewToolTipController();
            _DefaultRibbonToolTipController = CreateNewToolTipController(ToolTipAnchor.Cursor, ToolTipLocation.BottomRight);
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
        /// Vnitřní okraje, defaultní hodnota, obsahuje hodnoty <see cref="_DefaultInnerMarginX"/> a <see cref="_DefaultInnerMarginY"/>
        /// </summary>
        /// <returns></returns>
        public static Padding DefaultInnerMargins { get { return Instance._DefaultInnerMargins; } }
        /// <summary>
        /// Vnitřní rozestupy mezi prvky, defaultní hodnota
        /// </summary>
        public static Size DefaultInnerSpacing { get { return Instance._DefaultInnerSpacing; } }
        /// <summary>
        /// Vnitřní okraje, obsahuje hodnoty <see cref="_DefaultInnerMarginX"/> a <see cref="_DefaultInnerMarginY"/>
        /// </summary>
        private Padding _DefaultInnerMargins { get { int x = _DefaultInnerMarginX; int y = _DefaultInnerMarginY; return new Padding(x, y, x, y); } }
        /// <summary>
        /// Vnitřní rozestupy mezi prvky, defaultní hodnota
        /// </summary>
        private Size _DefaultInnerSpacing { get { int w = _DefaultInnerSpacingW; int h = _DefaultInnerSpacingH; return new Size(w, h); } }
        /// <summary>
        /// Vnitřní okraje, defaultní hodnota, vychází z <see cref="GetDefaultInnerMarginX(int)"/> a <see cref="GetDefaultInnerMarginY(int)"/>
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        public static Padding GetDefaultInnerMargins(int targetDpi) { return ZoomToGui(DefaultInnerMargins, targetDpi); }
        /// <summary>
        /// Vnitřní rozestupy mezi prvky, hodnota pro dané DPI
        /// </summary>
        /// <param name="targetDpi"></param>
        /// <returns></returns>
        public static Size GetDefaultInnerSpacing(int targetDpi) { return ZoomToGui(DefaultInnerSpacing, targetDpi); }
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
        /// Okraj v ose X, mezi vnitřním okrajem labelu a prvním prvkem
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDefaultInnerMarginX(int targetDpi) { return ZoomToGui(Instance._DefaultInnerMarginX, targetDpi); }
        /// <summary>
        /// Okraj v ose Y, mezi vnitřním okrajem labelu a prvním prvkem
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDefaultInnerMarginY(int targetDpi) { return ZoomToGui(Instance._DefaultInnerMarginY, targetDpi); }
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
        /// Defaultní mezera X mezi buttony, designová hodnota
        /// </summary>
        public static int DefaultButtonXSpace { get { return Instance._DefaultButtonXSpace; } }
        /// <summary>
        /// Defaultní mezera Y mezi buttony, designová hodnota
        /// </summary>
        public static int DefaultButtonYSpace { get { return Instance._DefaultButtonYSpace; } }
        /// <summary>
        /// Defaultní mezera Y mezi buttony pro dané DPI
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDefaultButtonXSpace(int targetDpi) { return ZoomToGui(Instance._DefaultButtonXSpace, targetDpi); }
        /// <summary>
        /// Defaultní mezera Y mezi buttony pro dané DPI
        /// </summary>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        public static int GetDefaultButtonYSpace(int targetDpi) { return ZoomToGui(Instance._DefaultButtonYSpace, targetDpi); }
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
        public static DxToolTipController DefaultToolTipController { get { return Instance._DefaultToolTipController; } }
        /// <summary>
        /// Defaultní ToolTipController pro Ribbony
        /// </summary>
        public static DxToolTipController DefaultRibbonToolTipController { get { return Instance._DefaultRibbonToolTipController; } }
        /// <summary>
        /// Vytvoří a vrátí new instanci ToolTipController, standardně deklarovanou
        /// </summary>
        /// <param name="toolTipAnchor">Ukotvení ToolTipu se odvozuje od ...</param>
        /// <param name="toolTipLocation">Pozice ToolTipu je ... od ukotvení</param>
        /// <returns></returns>
        public static DxToolTipController CreateNewToolTipController(ToolTipAnchor toolTipAnchor = ToolTipAnchor.Object, ToolTipLocation toolTipLocation = ToolTipLocation.RightBottom)
        {
            return new DxToolTipController(toolTipAnchor, toolTipLocation);
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
        private int _DefaultInnerMarginX;
        private int _DefaultInnerMarginY;
        private int _DefaultInnerSpacingW;
        private int _DefaultInnerSpacingH;
        private int _DefaultButtonPanelHeight;
        private int _DefaultButtonWidth;
        private int _DefaultButtonHeight;
        private int _DefaultButtonXSpace;
        private int _DefaultButtonYSpace;
        private DevExpress.XtraBars.BarManager _DefaultBarManager;
        private DxToolTipController _DefaultToolTipController;
        private DxToolTipController _DefaultRibbonToolTipController;

        #region Rozmístění prvků typu ControlItemLayoutInfo do daného vnitřního prostoru
        /// <summary>
        /// Určí rozmístění prvků typu ControlItemLayoutInfo do daného vnitřního prostoru.
        /// <para/>
        /// Máme vnitřní prostor nějakého panelu (parametr <paramref name="innerBounds"/>, 
        /// a dovnitř do něj budeme umisťovat sadu controlů <paramref name="items"/> (typicky malá tlačítka).
        /// Tato metoda určí souřadnice těchto controlů podle daného pozicování <paramref name="position"/>.
        /// Pokud je málo prostoru, tato metoda neprovádí Wrapping = controly, které se nevejdou do prostoru,
        /// nepřeskočí na nový řádek, ale jsou v neviditelné pozici.
        /// </summary>
        /// <param name="innerBounds"></param>
        /// <param name="items"></param>
        /// <param name="position"></param>
        /// <param name="margins"></param>
        /// <param name="itemSpace"></param>
        /// <returns></returns>
        internal static Rectangle CalculateControlItemsLayout(Rectangle innerBounds, ControlItemLayoutInfo[] items, ToolbarPosition position, Padding? margins = null, Size? itemSpace = null)
        { return Instance._CalculateControlItemsLayout(innerBounds, items, position, out var _, margins, itemSpace); }
        /// <summary>
        /// Určí rozmístění prvků typu ControlItemLayoutInfo do daného vnitřního prostoru.
        /// <para/>
        /// Máme vnitřní prostor nějakého panelu (parametr <paramref name="innerBounds"/>, 
        /// a dovnitř do něj budeme umisťovat sadu controlů <paramref name="items"/> (typicky malá tlačítka).
        /// Tato metoda určí souřadnice těchto controlů podle daného pozicování <paramref name="position"/>.
        /// Pokud je málo prostoru, tato metoda neprovádí Wrapping = controly, které se nevejdou do prostoru,
        /// nepřeskočí na nový řádek, ale jsou v neviditelné pozici.
        /// </summary>
        /// <param name="innerBounds"></param>
        /// <param name="items"></param>
        /// <param name="position"></param>
        /// <param name="buttonsBounds"></param>
        /// <param name="margins"></param>
        /// <param name="itemSpace"></param>
        /// <returns></returns>
        internal static Rectangle CalculateControlItemsLayout(Rectangle innerBounds, ControlItemLayoutInfo[] items, ToolbarPosition position, out Rectangle buttonsBounds, Padding? margins = null, Size? itemSpace = null)
        { return Instance._CalculateControlItemsLayout(innerBounds, items, position, out buttonsBounds, margins, itemSpace); }
        private Rectangle _CalculateControlItemsLayout(Rectangle innerBounds, ControlItemLayoutInfo[] items, ToolbarPosition position, out Rectangle buttonsBounds, Padding? margins, Size? itemSpace)
        {
            Rectangle contentBounds = innerBounds;
            buttonsBounds = Rectangle.Empty;
            int count = items?.Length ?? 0;
            if (count == 0) return contentBounds;

            Padding margin = margins ?? _DefaultInnerMargins;
            Size space = itemSpace ?? _DefaultInnerSpacing;
            int spaceX = space.Width;
            int spaceY = space.Height;

            bool isLeftSide = DataExtensions.IsAnyOf(position, ToolbarPosition.LeftSideTop, ToolbarPosition.LeftSideCenter, ToolbarPosition.LeftSideBottom);
            bool isTopSide = DataExtensions.IsAnyOf(position, ToolbarPosition.TopSideLeft, ToolbarPosition.TopSideCenter, ToolbarPosition.TopSideRight);
            bool isRightSide = DataExtensions.IsAnyOf(position, ToolbarPosition.RightSideTop, ToolbarPosition.RightSideCenter, ToolbarPosition.RightSideBottom);
            bool isBottomSide = DataExtensions.IsAnyOf(position, ToolbarPosition.BottomSideLeft, ToolbarPosition.BottomSideCenter, ToolbarPosition.BottomSideRight);

            bool isAlignedBegin = DataExtensions.IsAnyOf(position, ToolbarPosition.LeftSideTop, ToolbarPosition.TopSideLeft, ToolbarPosition.RightSideTop, ToolbarPosition.BottomSideLeft);
            bool isAlignedCenter = DataExtensions.IsAnyOf(position, ToolbarPosition.LeftSideCenter, ToolbarPosition.TopSideCenter, ToolbarPosition.RightSideCenter, ToolbarPosition.BottomSideCenter);
            bool isAlignedEnd = DataExtensions.IsAnyOf(position, ToolbarPosition.LeftSideBottom, ToolbarPosition.TopSideRight, ToolbarPosition.RightSideBottom, ToolbarPosition.BottomSideRight);
            AlignContentToSide alignment = (isAlignedEnd ? AlignContentToSide.End : (isAlignedCenter ? AlignContentToSide.Center : AlignContentToSide.Begin));

            if (isLeftSide || isRightSide)
            {   // Svisle uspořádané:
                // 1. Jednotlivé itemy:
                int height = items.Sum(i => i.Size.Height) + ((count - 1) * spaceY);
                int maxWidth = items.Max(i => i.Size.Width);
                int y = innerBounds.Y + CalculateAlignedBegin(innerBounds.Height, height, alignment);
                int b = isLeftSide ? innerBounds.Left : innerBounds.Right;     // Tady je Left buttonu, pokud jsou LeftSide, anebo Right buttonu, pokud jsou RightSide
                int e = b;                                                     // Tady je Max(Right) buttonu pro LeftSide, anebo Min(Left) Buttonu pro RightSide
                foreach (var item in items)
                {
                    Size size = item.Size;                                     // Požadovaná velikost prvku
                    Point location = new Point((isLeftSide ? b : b - size.Width), y);
                    Rectangle bounds = new Rectangle(location, size);
                    item.Bounds = bounds;                                      // Setování souřadnice se promítne i do vloženého Controlu (pokud tam je)
                    y += size.Height + spaceY;

                    // Střádám Max(Right) pro LeftSide:
                    if (isLeftSide && bounds.Right > e)
                        e = bounds.Right;
                    // .. anebo Min(Left) pro RightSide:
                    else if (isRightSide && bounds.Left < e)
                        e = bounds.Left;
                }
                // 2. Prostor pro content je vedle buttonů (doprava / doleva) při zachování Margins (Left / Right) odstupu od okraje buttonů (e):
                if (isLeftSide)
                {
                    buttonsBounds = Rectangle.FromLTRB(b, innerBounds.Top, e, innerBounds.Bottom);
                    contentBounds = Rectangle.FromLTRB(e + margin.Left, innerBounds.Top, innerBounds.Right, innerBounds.Bottom);
                }
                else if (isRightSide)
                {
                    buttonsBounds = Rectangle.FromLTRB(e, innerBounds.Top, b, innerBounds.Bottom);
                    contentBounds = Rectangle.FromLTRB(innerBounds.Left, innerBounds.Top, e - margin.Right, innerBounds.Bottom);
                }
            }
            else if (isTopSide || isBottomSide)
            {   // Vodorovně uspořádané:
                int width = items.Sum(i => i.Size.Width) + ((count - 1) * spaceX);
                int maxHeight = items.Max(i => i.Size.Height);
                int x = innerBounds.X + CalculateAlignedBegin(innerBounds.Width, width, alignment);
                int b = isTopSide ? innerBounds.Top : innerBounds.Bottom;      // Tady je Top buttonu, pokud jsou TopSide, anebo Bottom buttonu, pokud jsou BottomSide
                int e = b;                                                     // Tady je Max(Bottom) buttonu pro TopSide, anebo Min(Top) Buttonu pro BottomSide
                foreach (var item in items)
                {
                    Size size = item.Size;                                     // Požadovaná velikost prvku
                    Point location = new Point(x, (isTopSide ? b : b - size.Height));
                    Rectangle bounds = new Rectangle(location, size);
                    item.Bounds = bounds;                                      // Setování souřadnice se promítne i do vloženého Controlu (pokud tam je)
                    x += size.Width + spaceX;

                    // Střádám Max(Bottom) pro TopSide:
                    if (isTopSide && bounds.Bottom > e)
                        e = bounds.Bottom;
                    // .. anebo Min(Left) pro RightSide:
                    else if (isBottomSide && bounds.Top < e)
                        e = bounds.Top;
                }
                // 2. Prostor pro content je vedle buttonů (dolů / nahoru) při zachování Margins (Bottom / Top) odstupu od okraje buttonů (e):
                if (isTopSide)
                {
                    buttonsBounds = Rectangle.FromLTRB(innerBounds.Left, b, innerBounds.Right, e);
                    contentBounds = Rectangle.FromLTRB(innerBounds.Left, e + margin.Top, innerBounds.Right, innerBounds.Bottom);
                }
                else if (isBottomSide)
                {
                    buttonsBounds = Rectangle.FromLTRB(innerBounds.Left, e, innerBounds.Right, b);
                    contentBounds = Rectangle.FromLTRB(innerBounds.Left, innerBounds.Top, innerBounds.Right, e - margin.Bottom);
                }
            }

            return contentBounds;
        }
        #endregion
        #endregion
        #region Rozhraní na Zoom
        /// <summary>
        /// Inicializace Zoomu
        /// </summary>
        private void _InitZoom()
        {
            __Zoom = 1m;
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
        internal static int ZoomToGui(int value) { decimal zoom = Instance.__Zoom; return _ZoomToGui(value, zoom); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static int ZoomToGui(int value, int targetDpi) { decimal zoomDpi = Instance.__ZoomDpi; return _ZoomDpiToGui(value, zoomDpi, targetDpi); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static float ZoomToGui(float value, int targetDpi) { decimal zoomDpi = Instance.__ZoomDpi; return _ZoomDpiToGui(value, zoomDpi, targetDpi); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static int? ZoomToGui(int? value) { if (value.HasValue) { decimal zoom = Instance.__Zoom; return _ZoomToGui(value.Value, zoom); } return null; }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static int? ZoomToGui(int? value, int targetDpi) { if (value.HasValue) { decimal zoomDpi = Instance.__ZoomDpi; return _ZoomDpiToGui(value.Value, zoomDpi, targetDpi); } return null; }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static float? ZoomToGui(float? value, int targetDpi) { if (value.HasValue) { decimal zoomDpi = Instance.__ZoomDpi; return _ZoomDpiToGui(value.Value, zoomDpi, targetDpi); } return null; }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Int32Range ZoomToGui(Int32Range value, int targetDpi) { if (value == null) return null; decimal zoomDpi = Instance.__ZoomDpi; return new Int32Range(_ZoomDpiToGui(value.Begin, zoomDpi, targetDpi), _ZoomDpiToGui(value.End, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Point ZoomToGui(Point value) { decimal zoom = Instance.__Zoom; return new Point(_ZoomToGui(value.X, zoom), _ZoomToGui(value.Y, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Point? ZoomToGui(Point? value) { if (!value.HasValue) return null; decimal zoom = Instance.__Zoom; var v = value.Value; return new Point(_ZoomToGui(v.X, zoom), _ZoomToGui(v.Y, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Point ZoomToGui(Point value, int targetDpi) { decimal zoomDpi = Instance.__ZoomDpi; return new Point(_ZoomDpiToGui(value.X, zoomDpi, targetDpi), _ZoomDpiToGui(value.Y, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Size ZoomToGui(Size value) { decimal zoom = Instance.__Zoom; return new Size(_ZoomToGui(value.Width, zoom), _ZoomToGui(value.Height, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Size? ZoomToGui(Size? value) { if (!value.HasValue) return null; decimal zoom = Instance.__Zoom; var v = value.Value; return new Size(_ZoomToGui(v.Width, zoom), _ZoomToGui(v.Height, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Size ZoomToGui(Size value, int targetDpi) { decimal zoomDpi = Instance.__ZoomDpi; return new Size(_ZoomDpiToGui(value.Width, zoomDpi, targetDpi), _ZoomDpiToGui(value.Height, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty.
        /// <para/>
        /// Rectangle je vytvářen z přepočtených souřadnic (Left, Top, Right, Bottom), 
        /// tím je zaručeno, že pravý a dolní okraj výsledných Rectangle bude zarovnán stejně jako v Designu, 
        /// tedy že nebude "rozházený" vlivem zaokrouhlení, kdy se může stát, že (Round(X,0) + Round(Width,0)) != Round(Right,0) !!!
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Rectangle ZoomToGui(Rectangle value) { decimal zoom = Instance.__Zoom; return Rectangle.FromLTRB(_ZoomToGui(value.Left, zoom), _ZoomToGui(value.Top, zoom), _ZoomToGui(value.Right, zoom), _ZoomToGui(value.Bottom, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty.
        /// <para/>
        /// Rectangle je vytvářen z přepočtených souřadnic (Left, Top, Right, Bottom), 
        /// tím je zaručeno, že pravý a dolní okraj výsledných Rectangle bude zarovnán stejně jako v Designu, 
        /// tedy že nebude "rozházený" vlivem zaokrouhlení, kdy se může stát, že (Round(X,0) + Round(Width,0)) != Round(Right,0) !!!
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Rectangle? ZoomToGui(Rectangle? value) { if (!value.HasValue) return null; decimal zoom = Instance.__Zoom; var v = value.Value; return Rectangle.FromLTRB(_ZoomToGui(v.Left, zoom), _ZoomToGui(v.Top, zoom), _ZoomToGui(v.Right, zoom), _ZoomToGui(v.Bottom, zoom)); }
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
        internal static Rectangle ZoomToGui(Rectangle value, int targetDpi) { decimal zoomDpi = Instance.__ZoomDpi; return Rectangle.FromLTRB(_ZoomDpiToGui(value.Left, zoomDpi, targetDpi), _ZoomDpiToGui(value.Top, zoomDpi, targetDpi), _ZoomDpiToGui(value.Right, zoomDpi, targetDpi), _ZoomDpiToGui(value.Bottom, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Padding ZoomToGui(Padding value) { decimal zoom = Instance.__Zoom; return new Padding(_ZoomToGui(value.Left, zoom), _ZoomToGui(value.Top, zoom), _ZoomToGui(value.Right, zoom), _ZoomToGui(value.Bottom, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Padding? ZoomToGui(Padding? value) { if (!value.HasValue) return null; decimal zoom = Instance.__Zoom; var v = value.Value; return new Padding(_ZoomToGui(v.Left, zoom), _ZoomToGui(v.Top, zoom), _ZoomToGui(v.Right, zoom), _ZoomToGui(v.Bottom, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Padding ZoomToGui(Padding value, int targetDpi) { decimal zoomDpi = Instance.__ZoomDpi; return new Padding(_ZoomDpiToGui(value.Left, zoomDpi, targetDpi), _ZoomDpiToGui(value.Top, zoomDpi, targetDpi), _ZoomDpiToGui(value.Right, zoomDpi, targetDpi), _ZoomDpiToGui(value.Bottom, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí daný font přepočtený dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Font ZoomToGui(Font value, int targetDpi)
        {
            var instance = Instance;
            decimal zoomDpi = instance.__ZoomDpi;
            float emSize = _ZoomDpiToGui(value.Size, Instance.__ZoomDpi, targetDpi);
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
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static int ZoomToDesign(int value) { decimal zoom = Instance.__Zoom; return _ZoomToDesign(value, zoom); }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static int ZoomToDesign(int value, int targetDpi) { decimal zoomDpi = Instance.__ZoomDpi; return _ZoomDpiToDesign(value, zoomDpi, targetDpi); }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static float ZoomToDesign(float value, int targetDpi) { decimal zoomDpi = Instance.__ZoomDpi; return _ZoomDpiToDesign(value, zoomDpi, targetDpi); }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static int? ZoomToDesign(int? value) { if (value.HasValue) { decimal zoom = Instance.__Zoom; return _ZoomToDesign(value.Value, zoom); } return null; }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static int? ZoomToDesign(int? value, int targetDpi) { if (value.HasValue) { decimal zoomDpi = Instance.__ZoomDpi; return _ZoomDpiToDesign(value.Value, zoomDpi, targetDpi); } return null; }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static float? ZoomToDesign(float? value, int targetDpi) { if (value.HasValue) { decimal zoomDpi = Instance.__ZoomDpi; return _ZoomDpiToDesign(value.Value, zoomDpi, targetDpi); } return null; }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Int32Range ZoomToDesign(Int32Range value, int targetDpi) { if (value == null) return null; decimal zoomDpi = Instance.__ZoomDpi; return new Int32Range(_ZoomDpiToDesign(value.Begin, zoomDpi, targetDpi), _ZoomDpiToDesign(value.End, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Point ZoomToDesign(Point value) { decimal zoom = Instance.__Zoom; return new Point(_ZoomToDesign(value.X, zoom), _ZoomToDesign(value.Y, zoom)); }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Point? ZoomToDesign(Point? value) { if (!value.HasValue) return null; decimal zoom = Instance.__Zoom; var v = value.Value; return new Point(_ZoomToDesign(v.X, zoom), _ZoomToDesign(v.Y, zoom)); }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Point ZoomToDesign(Point value, int targetDpi) { decimal zoomDpi = Instance.__ZoomDpi; return new Point(_ZoomDpiToDesign(value.X, zoomDpi, targetDpi), _ZoomDpiToDesign(value.Y, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Size ZoomToDesign(Size value) { decimal zoom = Instance.__Zoom; return new Size(_ZoomToDesign(value.Width, zoom), _ZoomToDesign(value.Height, zoom)); }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Size? ZoomToDesign(Size? value) { if (!value.HasValue) return null; decimal zoom = Instance.__Zoom; var v = value.Value; return new Size(_ZoomToDesign(v.Width, zoom), _ZoomToDesign(v.Height, zoom)); }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Size ZoomToDesign(Size value, int targetDpi) { decimal zoomDpi = Instance.__ZoomDpi; return new Size(_ZoomDpiToDesign(value.Width, zoomDpi, targetDpi), _ZoomDpiToDesign(value.Height, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty.
        /// <para/>
        /// Rectangle je vytvářen z přepočtených souřadnic (Left, Top, Right, Bottom), 
        /// tím je zaručeno, že pravý a dolní okraj výsledných Rectangle bude zarovnán stejně jako v Designu, 
        /// tedy že nebude "rozházený" vlivem zaokrouhlení, kdy se může stát, že (Round(X,0) + Round(Width,0)) != Round(Right,0) !!!
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Rectangle ZoomToDesign(Rectangle value) { decimal zoom = Instance.__Zoom; return Rectangle.FromLTRB(_ZoomToDesign(value.Left, zoom), _ZoomToDesign(value.Top, zoom), _ZoomToDesign(value.Right, zoom), _ZoomToDesign(value.Bottom, zoom)); }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty.
        /// <para/>
        /// Rectangle je vytvářen z přepočtených souřadnic (Left, Top, Right, Bottom), 
        /// tím je zaručeno, že pravý a dolní okraj výsledných Rectangle bude zarovnán stejně jako v Designu, 
        /// tedy že nebude "rozházený" vlivem zaokrouhlení, kdy se může stát, že (Round(X,0) + Round(Width,0)) != Round(Right,0) !!!
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Rectangle? ZoomToDesign(Rectangle? value) { if (!value.HasValue) return null; decimal zoom = Instance.__Zoom; var v = value.Value; return Rectangle.FromLTRB(_ZoomToDesign(v.Left, zoom), _ZoomToDesign(v.Top, zoom), _ZoomToDesign(v.Right, zoom), _ZoomToDesign(v.Bottom, zoom)); }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty.
        /// <para/>
        /// Rectangle je vytvářen z přepočtených souřadnic (Left, Top, Right, Bottom), 
        /// tím je zaručeno, že pravý a dolní okraj výsledných Rectangle bude zarovnán stejně jako v Designu, 
        /// tedy že nebude "rozházený" vlivem zaokrouhlení, kdy se může stát, že (Round(X,0) + Round(Width,0)) != Round(Right,0) !!!
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Rectangle ZoomToDesign(Rectangle value, int targetDpi) { decimal zoomDpi = Instance.__ZoomDpi; return Rectangle.FromLTRB(_ZoomDpiToDesign(value.Left, zoomDpi, targetDpi), _ZoomDpiToDesign(value.Top, zoomDpi, targetDpi), _ZoomDpiToDesign(value.Right, zoomDpi, targetDpi), _ZoomDpiToDesign(value.Bottom, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Padding ZoomToDesign(Padding value) { decimal zoom = Instance.__Zoom; return new Padding(_ZoomToDesign(value.Left, zoom), _ZoomToDesign(value.Top, zoom), _ZoomToDesign(value.Right, zoom), _ZoomToDesign(value.Bottom, zoom)); }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Padding? ZoomToDesign(Padding? value) { if (!value.HasValue) return null; decimal zoom = Instance.__Zoom; var v = value.Value; return new Padding(_ZoomToDesign(v.Left, zoom), _ZoomToDesign(v.Top, zoom), _ZoomToDesign(v.Right, zoom), _ZoomToDesign(v.Bottom, zoom)); }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Padding ZoomToDesign(Padding value, int targetDpi) { decimal zoomDpi = Instance.__ZoomDpi; return new Padding(_ZoomDpiToDesign(value.Left, zoomDpi, targetDpi), _ZoomDpiToDesign(value.Top, zoomDpi, targetDpi), _ZoomDpiToDesign(value.Right, zoomDpi, targetDpi), _ZoomDpiToDesign(value.Bottom, zoomDpi, targetDpi)); }

        /// <summary>
        /// Vrátí daný font přepočtený dle aktuálního Zoomu do designové hodnoty
        /// </summary>
        /// <param name="value">Designová hodnota (96DPI, 100%)</param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        internal static Font ZoomToDesign(Font value, int targetDpi)
        {
            var instance = Instance;
            decimal zoomDpi = instance.__ZoomDpi;
            float emSize = _ZoomDpiToDesign(value.Size, Instance.__ZoomDpi, targetDpi);
            return instance._GetFont(null, value.FontFamily, null, emSize, value.Style);
        }

        //-----

        private static int _ZoomToDesign(int value, decimal zoom) { return (int)Math.Round((decimal)value / zoom, 0); }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle daného (Zoom / DesignDpi) a dané TargetDpi do designové hodnoty
        /// </summary>
        /// <param name="value">Vizuální hodnota</param>
        /// <param name="zoomDpi"></param>
        /// <param name="targetDpi"></param>
        /// <returns></returns>
        private static int _ZoomDpiToDesign(int value, decimal zoomDpi, decimal targetDpi) { return (int)Math.Round((decimal)value / zoomDpi / targetDpi, 0); }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle daného (Zoom / DesignDpi) a dané TargetDpi do designové hodnoty
        /// </summary>
        /// <param name="value">Vizuální hodnota</param>
        /// <param name="zoomDpi"></param>
        /// <param name="targetDpi"></param>
        /// <returns></returns>
        private static float _ZoomDpiToDesign(float value, decimal zoomDpi, decimal targetDpi) { return (float)((decimal)value / zoomDpi / targetDpi); }
        /// <summary>
        /// Vrátí danou vizuální hodnotu přepočtenou dle daného (Zoom / DesignDpi) a dané TargetDpi do designové hodnoty
        /// </summary>
        /// <param name="value">Vizuální hodnota</param>
        /// <param name="zoomDpi"></param>
        /// <param name="targetDpi"></param>
        /// <returns></returns>
        private static decimal _ZoomDpiToDesign(decimal value, decimal zoomDpi, decimal targetDpi) { return (value / zoomDpi / targetDpi); }
        /// <summary>
        /// Aktuální hodnota Zoomu. Defaultní hodnota = 1.00, což odpovídá 100%.
        /// </summary>
        internal static decimal Zoom { get { return Instance.__Zoom; } }
        /// <summary>
        /// Hodnota DPI, ke které se vztahují velikosti prvků zadávané jako DesignBounds.
        /// Reálná velikost prvků se pak konvertuje na cílové DPI monitoru.
        /// Defaultní hodnota je 96 DPI, povolený rozsah je 72 až 600 DPI.
        /// </summary>
        public static int DesignDpi { get { return Instance.__DesignDpi; } set { Instance._SetDesignDpi(value); } }

        /// <summary>
        /// Aktuální hodnota (<see cref="Zoom"/> / <see cref="DesignDpi"/>), slouží k rychlému přepočtu Design souřadnic na cílové souřadnice v aktuálním Zoomu a TargetDPI.
        /// </summary>
        internal static decimal ZoomDpi { get { return Instance.__ZoomDpi; } }
        /// <summary>
        /// Reload hodnoty Zoomu
        /// </summary>
        internal static void ReloadZoom() { Instance._ReloadZoom(); }
        /// <summary>
        /// Reload hodnoty Zoomu uvnitř instance
        /// </summary>
        private void _ReloadZoom()
        {
            __Zoom = SystemAdapter.ZoomRatio;
            _RecalcZoomDpi();
        }
        /// <summary>
        /// Uloží hodnotu DesignDpi a přepočte další...
        /// </summary>
        /// <param name="designDpi"></param>
        private void _SetDesignDpi(int designDpi)
        {
            __DesignDpi = (designDpi < 72 ? 72 : (designDpi > 600 ? 600 : designDpi));
            _RecalcZoomDpi();
        }
        private void _RecalcZoomDpi()
        {
            // Hodnota _ZoomDpi slouží k rychlému přepočtu Designové hodnoty (int)
            // s pomocí Zoomu (kde 1.5 = 150%) a konverze rozměru pomocí DPI Design - Current (kde Design = 96, a UHD má Target = 144)
            // na cílovou Current hodnotu (int):
            // Current = (Design * _ZoomDpi * TargetDpi)

            __ZoomDpi = __Zoom / (decimal)__DesignDpi;
        }
        /// <summary>
        /// Aktuální hodnota Zoomu. Defaultní hodnota = 1.00, což odpovídá 100%.
        /// </summary>
        private decimal __Zoom;
        /// <summary>
        /// Hodnota DPI, ke které se vztahují velikosti prvků zadávané jako DesignBounds.
        /// Reálná velikost prvků se pak konvertuje na cílové DPI monitoru.
        /// Defaultní hodnota je 96 DPI, povolený rozsah je 72 až 600 DPI.
        /// </summary>
        private int __DesignDpi;
        /// <summary>
        /// Aktuální hodnota (<see cref="Zoom"/> / <see cref="DesignDpi"/>), slouží k rychlému přepočtu Design souřadnic na cílové souřadnice v aktuálním Zoomu a TargetDPI.
        /// </summary>
        private decimal __ZoomDpi;
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
            bool nowIsDark = _IsDarkTheme();               // Tady se napočte příznak __IsDarkTheme, a současně se načte SkinColorSet
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
            {
                try { method.Invoke(listener, null); }
                catch(Exception exc)
                {
                    try { DxComponent.LogAddException(exc); }
                    catch { }
                }
            }
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
            {
                try { method.Invoke(listener, parameters); }
                catch (Exception exc)
                {
                    try { DxComponent.LogAddException(exc); }
                    catch { }
                }
            }
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
                __Listeners.RemoveWhere(s => (!s.IsAlive || s.ContainsListener(listener)), a => a.ClearListener());

            __ListenersLastClean = DateTime.Now;
        }
        /// <summary>
        /// Metoda najde a vrátí všechny živé listenery daného typu.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] GetListeners<T>() where T : IListener
        { return Instance._GetListeners<T>(); }
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
                __Listeners.RemoveWhere(s => !s.IsAlive, a => a.ClearListener());
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
            public bool IsAlive { get { return __Listener?.IsAlive ?? false; } }
            /// <summary>
            /// Reference na instanci
            /// </summary>
            public IListener Listener { get { return __Listener?.Target; } }
            /// <summary>
            /// Vrátí true, pokud this instance drží odkaz na daného subscribera.
            /// </summary>
            public bool ContainsListener(IListener testListener)
            {
                IListener myListener = __Listener.Target;
                return (myListener != null && Object.ReferenceEquals(myListener, testListener));
            }
            /// <summary>
            /// Zahodí a uvolní instanci v this instanci <see cref="_ListenerInstance"/>
            /// </summary>
            public void ClearListener()
            {
                __Listener = null;
            }
        }
        #endregion
        #region Factory metody pro jednořádkovou tvorbu běžných komponent
        /// <summary>
        /// Vytvoří a vrátí DxPanelControl s danými parametry
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="dock"></param>
        /// <param name="borderStyles"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="visible"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxSplitContainerControl s danými parametry
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="splitterPositionChanged"></param>
        /// <param name="dock"></param>
        /// <param name="splitLineOrientation"></param>
        /// <param name="fixedPanel"></param>
        /// <param name="splitPosition"></param>
        /// <param name="panelVisibility"></param>
        /// <param name="showSplitGlyph"></param>
        /// <param name="borderStyles"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxLabelControl s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="parent"></param>
        /// <param name="text"></param>
        /// <param name="styleType"></param>
        /// <param name="wordWrap"></param>
        /// <param name="autoSizeMode"></param>
        /// <param name="hAlignment"></param>
        /// <param name="visible"></param>
        /// <param name="useLabelTextOffset"></param>
        /// <returns></returns>
        public static DxLabelControl CreateDxLabel(int x, int y, int w, Control parent, string text,
            LabelStyleType? styleType = null, DevExpress.Utils.WordWrap? wordWrap = null, DevExpress.XtraEditors.LabelAutoSizeMode? autoSizeMode = null, DevExpress.Utils.HorzAlignment? hAlignment = null,
            bool? visible = null, bool useLabelTextOffset = false)
        {
            return CreateDxLabel(x, ref y, w, parent, text,
                styleType, wordWrap, autoSizeMode, hAlignment,
                visible, useLabelTextOffset, false);
        }
        /// <summary>
        /// Vytvoří a vrátí DxLabelControl s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="parent"></param>
        /// <param name="text"></param>
        /// <param name="styleType"></param>
        /// <param name="wordWrap"></param>
        /// <param name="autoSizeMode"></param>
        /// <param name="hAlignment"></param>
        /// <param name="visible"></param>
        /// <param name="useLabelTextOffset"></param>
        /// <param name="shiftY"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxTextEdit s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="parent"></param>
        /// <param name="textChanged"></param>
        /// <param name="maskType"></param>
        /// <param name="editMask"></param>
        /// <param name="useMaskAsDisplayFormat"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="readOnly"></param>
        /// <param name="tabStop"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxTextEdit s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="parent"></param>
        /// <param name="textChanged"></param>
        /// <param name="maskType"></param>
        /// <param name="editMask"></param>
        /// <param name="useMaskAsDisplayFormat"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="readOnly"></param>
        /// <param name="tabStop"></param>
        /// <param name="shiftY"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxMemoEdit s danými parametry
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="dock"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="textChanged"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="readOnly"></param>
        /// <param name="tabStop"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxMemoEdit s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="parent"></param>
        /// <param name="textChanged"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="readOnly"></param>
        /// <param name="tabStop"></param>
        /// <returns></returns>
        public static DxMemoEdit CreateDxMemoEdit(int x, int y, int w, int h, Control parent, EventHandler textChanged = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null)
        {
            return CreateDxMemoEdit(x, ref y, w, h, parent, textChanged,
                toolTipTitle, toolTipText,
                visible, readOnly, tabStop, false);
        }
        /// <summary>
        /// Vytvoří a vrátí DxMemoEdit s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="parent"></param>
        /// <param name="textChanged"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="readOnly"></param>
        /// <param name="tabStop"></param>
        /// <param name="shiftY"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxButtonEdit s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="parent"></param>
        /// <param name="textChanged"></param>
        /// <param name="maskType"></param>
        /// <param name="editMask"></param>
        /// <param name="useMaskAsDisplayFormat"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="readOnly"></param>
        /// <param name="tabStop"></param>
        /// <returns></returns>
        public static DxButtonEdit CreateDxButtonEdit(int x, int y, int w, Control parent, EventHandler textChanged = null,
            DevExpress.XtraEditors.Mask.MaskType? maskType = null, string editMask = null, bool? useMaskAsDisplayFormat = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null)
        {
            return CreateDxButtonEdit(x, ref y, w, parent, textChanged,
                maskType, editMask, useMaskAsDisplayFormat,
                toolTipTitle, toolTipText,
                visible, readOnly, tabStop, false);
        }
        /// <summary>
        /// Vytvoří a vrátí DxButtonEdit s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="parent"></param>
        /// <param name="textChanged"></param>
        /// <param name="maskType"></param>
        /// <param name="editMask"></param>
        /// <param name="useMaskAsDisplayFormat"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="readOnly"></param>
        /// <param name="tabStop"></param>
        /// <param name="shiftY"></param>
        /// <returns></returns>
        public static DxButtonEdit CreateDxButtonEdit(int x, ref int y, int w, Control parent, EventHandler textChanged = null,
            DevExpress.XtraEditors.Mask.MaskType? maskType = null, string editMask = null, bool? useMaskAsDisplayFormat = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var buttonEdit = new DxButtonEdit() { Bounds = new Rectangle(x, y, w, inst._DetailYHeightText) };
            buttonEdit.StyleController = inst._InputStyle;
            if (visible.HasValue) buttonEdit.Visible = visible.Value;
            if (readOnly.HasValue) buttonEdit.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) buttonEdit.TabStop = tabStop.Value;

            if (maskType.HasValue) buttonEdit.Properties.Mask.MaskType = maskType.Value;
            if (editMask != null) buttonEdit.Properties.Mask.EditMask = editMask;
            if (useMaskAsDisplayFormat.HasValue) buttonEdit.Properties.Mask.UseMaskAsDisplayFormat = useMaskAsDisplayFormat.Value;

            buttonEdit.SetToolTip(toolTipTitle, toolTipText);

            if (textChanged != null) buttonEdit.TextChanged += textChanged;
            if (parent != null) parent.Controls.Add(buttonEdit);
            if (shiftY) y = y + buttonEdit.Height + inst._DetailYSpaceText;
            return buttonEdit;
        }
        /// <summary>
        /// Vytvoří a vrátí DxImageComboBoxEdit s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="parent"></param>
        /// <param name="selectedIndexChanged"></param>
        /// <param name="itemsTabbed"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="readOnly"></param>
        /// <param name="tabStop"></param>
        /// <returns></returns>
        public static DxImageComboBoxEdit CreateDxImageComboBox(int x, int y, int w, Control parent, EventHandler selectedIndexChanged = null, string itemsTabbed = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null)
        {
            return CreateDxImageComboBox(x, ref y, w, parent, selectedIndexChanged, itemsTabbed,
                toolTipTitle, toolTipText,
                visible, readOnly, tabStop, false);
        }
        /// <summary>
        /// Vytvoří a vrátí DxImageComboBoxEdit s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="parent"></param>
        /// <param name="selectedIndexChanged"></param>
        /// <param name="itemsTabbed"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="readOnly"></param>
        /// <param name="tabStop"></param>
        /// <param name="shiftY"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxSpinEdit s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="parent"></param>
        /// <param name="valueChanged"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="increment"></param>
        /// <param name="mask"></param>
        /// <param name="spinStyles"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="readOnly"></param>
        /// <param name="tabStop"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxSpinEdit s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="parent"></param>
        /// <param name="valueChanged"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="increment"></param>
        /// <param name="mask"></param>
        /// <param name="spinStyles"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="readOnly"></param>
        /// <param name="tabStop"></param>
        /// <param name="shiftY"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxCheckEdit s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="parent"></param>
        /// <param name="text"></param>
        /// <param name="checkedChanged"></param>
        /// <param name="checkBoxStyle"></param>
        /// <param name="borderStyles"></param>
        /// <param name="hAlignment"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="readOnly"></param>
        /// <param name="tabStop"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxCheckEdit s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="parent"></param>
        /// <param name="text"></param>
        /// <param name="checkedChanged"></param>
        /// <param name="checkBoxStyle"></param>
        /// <param name="borderStyles"></param>
        /// <param name="hAlignment"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="readOnly"></param>
        /// <param name="tabStop"></param>
        /// <param name="shiftY"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxListBoxControl s danými parametry
        /// </summary>
        /// <param name="dock"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="parent"></param>
        /// <param name="selectedIndexChanged"></param>
        /// <param name="multiColumn"></param>
        /// <param name="selectionMode"></param>
        /// <param name="itemHeight"></param>
        /// <param name="itemHeightPadding"></param>
        /// <param name="enabledKeyActions"></param>
        /// <param name="dragDropActions"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="tabStop"></param>
        /// <returns></returns>
        public static DxListBoxControl CreateDxListBox(DockStyle? dock = null, int? width = null, int? height = null, Control parent = null, EventHandler selectedIndexChanged = null,
            bool? multiColumn = null, SelectionMode? selectionMode = null, int? itemHeight = null, int? itemHeightPadding = null,
            ControlKeyActionType? enabledKeyActions = null, DxDragDropActionType? dragDropActions = null,
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
        /// <summary>
        /// Vytvoří a vrátí DxListBoxControl s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="parent"></param>
        /// <param name="selectedIndexChanged"></param>
        /// <param name="multiColumn"></param>
        /// <param name="selectionMode"></param>
        /// <param name="itemHeight"></param>
        /// <param name="itemHeightPadding"></param>
        /// <param name="enabledKeyActions"></param>
        /// <param name="dragDropActions"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="dock"></param>
        /// <param name="visible"></param>
        /// <param name="tabStop"></param>
        /// <returns></returns>
        public static DxListBoxControl CreateDxListBox(int x, int y, int w, int h, Control parent = null, EventHandler selectedIndexChanged = null,
            bool? multiColumn = null, SelectionMode? selectionMode = null, int? itemHeight = null, int? itemHeightPadding = null,
            ControlKeyActionType? enabledKeyActions = null, DxDragDropActionType? dragDropActions = null,
            string toolTipTitle = null, string toolTipText = null,
            DockStyle? dock = null, bool? visible = null, bool? tabStop = null)
        {
            return CreateDxListBox(x, ref y, w, h, parent, selectedIndexChanged,
                multiColumn, selectionMode, itemHeight, itemHeightPadding,
                enabledKeyActions, dragDropActions,
                toolTipTitle, toolTipText,
                dock, visible, tabStop, false);
        }
        /// <summary>
        /// Vytvoří a vrátí DxListBoxControl s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="parent"></param>
        /// <param name="selectedIndexChanged"></param>
        /// <param name="multiColumn"></param>
        /// <param name="selectionMode"></param>
        /// <param name="itemHeight"></param>
        /// <param name="itemHeightPadding"></param>
        /// <param name="enabledKeyActions"></param>
        /// <param name="dragDropActions"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="dock"></param>
        /// <param name="visible"></param>
        /// <param name="tabStop"></param>
        /// <param name="shiftY"></param>
        /// <returns></returns>
        public static DxListBoxControl CreateDxListBox(int x, ref int y, int w, int h, Control parent = null, EventHandler selectedIndexChanged = null,
            bool? multiColumn = null, SelectionMode? selectionMode = null, int? itemHeight = null, int? itemHeightPadding = null,
            ControlKeyActionType? enabledKeyActions = null, DxDragDropActionType? dragDropActions = null,
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

            if (selectedIndexChanged != null) listBox.SelectedIndexChanged += selectedIndexChanged;
            if (parent != null) parent.Controls.Add(listBox);
            if (shiftY) y = y + listBox.Height + inst._DetailYSpaceText;

            return listBox;
        }
        /// <summary>
        /// Vytvoří a vrátí DxCheckButton s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="parent"></param>
        /// <param name="text"></param>
        /// <param name="click"></param>
        /// <param name="paintStyles"></param>
        /// <param name="isChecked"></param>
        /// <param name="image"></param>
        /// <param name="resourceName"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="enabled"></param>
        /// <param name="tabStop"></param>
        /// <param name="hotKey"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxCheckButton s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="parent"></param>
        /// <param name="text"></param>
        /// <param name="click"></param>
        /// <param name="paintStyles"></param>
        /// <param name="isChecked"></param>
        /// <param name="image"></param>
        /// <param name="resourceName"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="enabled"></param>
        /// <param name="tabStop"></param>
        /// <param name="hotKey"></param>
        /// <param name="shiftY"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxSimpleButton s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="parent"></param>
        /// <param name="iButton"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxSimpleButton s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="parent"></param>
        /// <param name="text"></param>
        /// <param name="click"></param>
        /// <param name="paintStyle"></param>
        /// <param name="image"></param>
        /// <param name="resourceName"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="enabled"></param>
        /// <param name="tabStop"></param>
        /// <param name="hotKey"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static DxSimpleButton CreateDxSimpleButton(int x, int y, int w, int h, Control parent, string text, EventHandler click = null,
            DevExpress.XtraEditors.Controls.PaintStyles? paintStyle = null,
            Image image = null, string resourceName = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null, Keys? hotKey = null, object tag = null)
        {
            return CreateDxSimpleButton(x, ref y, w, h, parent, text, click,
                paintStyle,
                image, resourceName,
                toolTipTitle, toolTipText,
                visible, enabled, tabStop, hotKey, tag, false);
        }
        /// <summary>
        /// Vytvoří a vrátí DxSimpleButton s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="parent"></param>
        /// <param name="text"></param>
        /// <param name="click"></param>
        /// <param name="paintStyle"></param>
        /// <param name="image"></param>
        /// <param name="resourceName"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="enabled"></param>
        /// <param name="tabStop"></param>
        /// <param name="hotKey"></param>
        /// <param name="tag"></param>
        /// <param name="shiftY"></param>
        /// <returns></returns>
        public static DxSimpleButton CreateDxSimpleButton(int x, ref int y, int w, int h, Control parent, string text, EventHandler click = null,
            DevExpress.XtraEditors.Controls.PaintStyles? paintStyle = null,
            Image image = null, string resourceName = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null, Keys? hotKey = null, object tag = null, bool shiftY = false)
        {
            var inst = Instance;

            var simpleButton = new DxSimpleButton() { Bounds = new Rectangle(x, y, w, h) };
            simpleButton.StyleController = inst._InputStyle;
            simpleButton.Text = text;
            if (visible.HasValue) simpleButton.Visible = visible.Value;
            if (enabled.HasValue) simpleButton.Enabled = enabled.Value;
            if (tabStop.HasValue) simpleButton.TabStop = tabStop.Value;
            if (hotKey.HasValue) simpleButton.HotKey = hotKey.Value;
            if (paintStyle.HasValue) simpleButton.PaintStyle = paintStyle.Value;

            if (!String.IsNullOrEmpty(resourceName))
                simpleButton.ImageName = resourceName;
            else if (image != null)
                simpleButton.Image = image;

            if (!String.IsNullOrEmpty(text))
            {
                simpleButton.ImageOptions.ImageToTextAlignment = ImageAlignToText.LeftCenter;
                simpleButton.ImageOptions.ImageToTextIndent = 3;
            }
            else
            {
                simpleButton.ImageOptions.ImageToTextAlignment = ImageAlignToText.LeftCenter;
                simpleButton.ImageOptions.ImageToTextIndent = 0;
            }
            simpleButton.PrepareSizeSvgImage(true);

            simpleButton.SetToolTip(toolTipTitle, toolTipText, text);

            if (click != null) simpleButton.Click += click;
            if (parent != null) parent.Controls.Add(simpleButton);
            if (shiftY) y = y + simpleButton.Height + inst._DetailYSpaceText;

            simpleButton.Tag = tag;

            return simpleButton;
        }
        /// <summary>
        /// Vytvoří a vrátí DxSimpleButton s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="parent"></param>
        /// <param name="click"></param>
        /// <param name="image"></param>
        /// <param name="resourceName"></param>
        /// <param name="hotImage"></param>
        /// <param name="hotResourceName"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="enabled"></param>
        /// <param name="tabStop"></param>
        /// <param name="hotKey"></param>
        /// <param name="allowFocus"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxRibbonStatusBar s danými parametry
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="dock"></param>
        /// <returns></returns>
        public static DxRibbonStatusBar CreateDxStatusBar(Control parent, DockStyle? dock = null)
        {
            DxRibbonStatusBar statusBar = new DxRibbonStatusBar();
            if (dock.HasValue) statusBar.Dock = dock.Value;
            if (parent != null)
                parent.Controls.Add(statusBar);
            return statusBar;
        }
        /// <summary>
        /// Vytvoří a vrátí DxBarStaticItem s danými parametry
        /// </summary>
        /// <param name="statusBar"></param>
        /// <param name="text"></param>
        /// <param name="autoSize"></param>
        /// <param name="visible"></param>
        /// <param name="fontSizeDelta"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxBarButtonItem s danými parametry
        /// </summary>
        /// <param name="statusBar"></param>
        /// <param name="text"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="borderStyles"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="fontSizeDelta"></param>
        /// <param name="clickHandler"></param>
        /// <returns></returns>
        public static DxBarButtonItem CreateDxStatusButton(DevExpress.XtraBars.Ribbon.RibbonStatusBar statusBar = null, string text = null,
            int? width = null, int? height = null,
            DevExpress.XtraEditors.Controls.BorderStyles? borderStyles = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, int? fontSizeDelta = null,
            DevExpress.XtraBars.ItemClickEventHandler clickHandler = null)
        {
            DxBarButtonItem button = new DxBarButtonItem();
            button.ButtonStyle = DevExpress.XtraBars.BarButtonStyle.Default;
            if (text != null) button.Caption = text;
            if (borderStyles.HasValue) button.Border = borderStyles.Value;
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
        /// <summary>
        /// Vytvoří a vrátí DxBarCheckItem s danými parametry
        /// </summary>
        /// <param name="statusBar"></param>
        /// <param name="text"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="borderStyles"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="fontSizeDelta"></param>
        /// <param name="clickHandler"></param>
        /// <returns></returns>
        public static DxBarCheckItem CreateDxStatusCheckButton(DevExpress.XtraBars.Ribbon.RibbonStatusBar statusBar = null, string text = null,
            int? width = null, int? height = null,
            DevExpress.XtraEditors.Controls.BorderStyles? borderStyles = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, int? fontSizeDelta = null,
            DevExpress.XtraBars.ItemClickEventHandler clickHandler = null)
        {
            DxBarCheckItem checkButton = new DxBarCheckItem();
            if (text != null) checkButton.Caption = text;
            if (borderStyles.HasValue) checkButton.Border = borderStyles.Value;
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
        /// <summary>
        /// Vytvoří a vrátí DxDropDownButton s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="parent"></param>
        /// <param name="text"></param>
        /// <param name="click"></param>
        /// <param name="itemClick"></param>
        /// <param name="dropDownControl"></param>
        /// <param name="subItemsText"></param>
        /// <param name="subItems"></param>
        /// <param name="paintStyles"></param>
        /// <param name="image"></param>
        /// <param name="resourceName"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="enabled"></param>
        /// <param name="tabStop"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Vytvoří a vrátí DxDropDownButton s danými parametry
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="parent"></param>
        /// <param name="text"></param>
        /// <param name="click"></param>
        /// <param name="itemClick"></param>
        /// <param name="dropDownControl"></param>
        /// <param name="subItemsText"></param>
        /// <param name="subItems"></param>
        /// <param name="paintStyles"></param>
        /// <param name="image"></param>
        /// <param name="resourceName"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="visible"></param>
        /// <param name="enabled"></param>
        /// <param name="tabStop"></param>
        /// <param name="shiftY"></param>
        /// <returns></returns>
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

            var dropMenu = CreateDxDropDownControl(dropDownControl, subItemsText, subItems, itemClick);
            // dropMenu.
            dropDownButton.DropDownControl = dropMenu;

            if (click != null) dropDownButton.Click += click;
            if (parent != null) parent.Controls.Add(dropDownButton);
            if (shiftY) y = y + dropDownButton.Height + inst._DetailYSpaceText;

            return dropDownButton;
        }
        /// <summary>
        /// Vytvoří a vrátí IDXDropDownControl s danými parametry
        /// </summary>
        /// <param name="dropDownControl"></param>
        /// <param name="subItemsText"></param>
        /// <param name="subItems"></param>
        /// <param name="itemClick"></param>
        /// <returns></returns>
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
        /// Metoda vytvoří a vrátí DXPopupMenu pro dané položky [s daným titulkem, daného typu].
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
        #region DevExpress.XtraBars.PopupMenu : tvorba, řízení OnDemand
        /// <summary>
        /// Vytvoří a vrátí PopupMenu s danými parametry
        /// </summary>
        /// <param name="menuItems">Prvky definující menu</param>
        /// <param name="menuItemClick">Handler události MenuItemClick</param>
        /// <param name="onDemandLoad">Handler události OnDemandLoad</param>
        /// <param name="form">Formulář na kterém bude menu zobrazeno</param>
        /// <param name="explicitManager">Explicitně dodaný BarManager</param>
        /// <returns></returns>
        public static DxPopupMenu CreateXBPopupMenu(IEnumerable<IMenuItem> menuItems, EventHandler<MenuItemClickArgs> menuItemClick = null, EventHandler<DxPopupMenu.OnDemandLoadArgs> onDemandLoad = null, System.Windows.Forms.Form form = null, DevExpress.XtraBars.BarManager explicitManager = null)
        {
            DevExpress.XtraBars.BarManager barManager = explicitManager;
            if (barManager is null && form != null) barManager = new DevExpress.XtraBars.BarManager() { Form = form };
            if (barManager is null) barManager = new DevExpress.XtraBars.BarManager();

            return DxPopupMenu.CreateDxPopupMenu(menuItems, menuItemClick, onDemandLoad, barManager);
        }
        #endregion
        #region Tvorba pole IMenuItem z jednoho stringu
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
        #endregion
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
        public static DxSuperToolTip CreateDxSuperTip(IMenuItem textItem)
        {
            return DxSuperToolTip.CreateDxSuperTip(textItem);
        }
        /// <summary>
        /// Vytvoří a vrátí SuperTooltip
        /// </summary>
        /// <param name="toolTipItem"></param>
        /// <returns></returns>
        public static DxSuperToolTip CreateDxSuperTip(IToolTipItem toolTipItem)
        {
            return DxSuperToolTip.CreateDxSuperTip(toolTipItem);
        }
        /// <summary>
        /// Vytvoří a vrátí instanci <see cref="ToolTipControlInfo"/>, která se používá jako ToolTip pro TreeList a GridList.
        /// </summary>
        /// <param name="title">Titulek ToolTipu = výraznější písmo</param>
        /// <param name="text">Text ToolTipu = větší informace, může obsahovat HTML</param>
        /// <param name="defaultTitle">Defaultní titulek, když <paramref name="title"/> je prázdné</param>
        /// <param name="toolTipIcon">Typ ikony, NULL = žádná</param>
        /// <param name="allowHtmlText">Povolit HTML kódy v textu</param>
        /// <returns></returns>
        public static DevExpress.Utils.ToolTipControlInfo CreateDxToolTipControlInfo(string title, string text, string defaultTitle = null, string toolTipIcon = null, bool allowHtmlText = false)
        {
            return CreateDxToolTipControlInfo(title, text, null, null, defaultTitle, toolTipIcon, allowHtmlText);
        }
        /// <summary>
        /// Vytvoří a vrátí instanci <see cref="ToolTipControlInfo"/>, která se používá jako ToolTip pro TreeList a GridList.
        /// </summary>
        /// <param name="title">Titulek ToolTipu = výraznější písmo</param>
        /// <param name="text">Text ToolTipu = větší informace, může obsahovat HTML</param>
        /// <param name="control">Objekt umístěný do ToolTip controlleru jako Object</param>
        /// <param name="target">Objekt umístěný do ToolTip controlleru jako Info, typicky <see cref="DevExpress.XtraTreeList.ViewInfo.TreeListCellToolTipInfo"/> atd</param>
        /// <param name="defaultTitle">Defaultní titulek, když <paramref name="title"/> je prázdné</param>
        /// <param name="toolTipIcon">Typ ikony, NULL = žádná</param>
        /// <param name="allowHtmlText">Povolit HTML kódy v textu</param>
        /// <returns></returns>
        public static DevExpress.Utils.ToolTipControlInfo CreateDxToolTipControlInfo(string title, string text, object control, object target, string defaultTitle = null, string toolTipIcon = null, bool allowHtmlText = false)
        {
            if (!PrepareToolTipTexts(title, text, defaultTitle, out string toolTipTitle, out string toolTipText)) return null;

            DevExpress.Utils.ToolTipControlInfo ttci = null;

            // Následující nastavení generuje ToolTip shodný s jinými částmi systému = standardní SuperTip (Titulek + Oddělovač + Text):
            ToolTipType toolTipType = ToolTipType.SuperTip;
            bool superTipExplicit = true;

            switch (toolTipType)
            {
                case ToolTipType.SuperTip:
                    if (superTipExplicit)
                    {
                        var dxSuperTip = CreateDxSuperTip(toolTipTitle, toolTipText, toolTipIcon: toolTipIcon);
                        if (dxSuperTip != null)
                        {
                            dxSuperTip.ToolTipAllowHtmlText = allowHtmlText;
                            ttci = new DevExpress.Utils.ToolTipControlInfo(target, "");
                            ttci.Object = control;
                            ttci.ToolTipType = ToolTipType.SuperTip;
                            ttci.SuperTip = dxSuperTip;
                        }
                    }
                    else
                    {
                        ttci = new DevExpress.Utils.ToolTipControlInfo(target, "");
                        ttci.Object = control;
                        ttci.ToolTipType = ToolTipType.SuperTip;
                        ttci.Title = toolTipTitle;
                        ttci.Text = toolTipText;
                    }
                    break;

                case ToolTipType.Standard:
                case ToolTipType.Default:
                case ToolTipType.Flyout:
                    ttci = new DevExpress.Utils.ToolTipControlInfo(target, "");
                    ttci.Object = control;
                    ttci.ToolTipType = toolTipType;
                    ttci.Title = toolTipTitle;
                    ttci.Text = toolTipText;
                    ttci.AllowHtmlText = DxComponent.ConvertBool(allowHtmlText);
                    break;
            }

            return ttci;
        }
        /// <summary>
        /// Metoda ze vstupních dat vybere Titulek a Text pro ToolTip a vrátí true = máme data pro jeho zobrazení.
        /// Zde se tedy určuje, co bude zobrazeno a zda vůbec.
        /// <para/>
        /// Pokud není naplněn <paramref name="title"/> ani <paramref name="text"/>, pak se ToolTip nebude generovat = vrací se false.<br/>
        /// Pokud je naplněn <paramref name="text"/> ale není <paramref name="title"/>, pak se jako titulek použije <paramref name="defaultTitle"/> a vrátí se true.<br/>
        /// Pokud je naplněn <paramref name="title"/> a není dán <paramref name="text"/>, pak bude ToolTip bez textu (titulek stačí) a vrátí se true.
        /// <para/>
        /// Pokud je zadán text v <paramref name="defaultTitle"/>, a k němu je dán jen jeden z <paramref name="title"/> anebo <paramref name="text"/>, a ten je shodný,
        /// pak se ToolTip negeneruje = obsahoval by totéž, co už je uvedeno v prvku.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <param name="defaultTitle"></param>
        /// <param name="toolTipTitle">Výstup titulku ToolTipu. Pokud nemá být, je zde null.</param>
        /// <param name="toolTipText">Výstup textu ToolTipu. Pokud nemá být, je zde null.</param>
        /// <returns></returns>
        public static bool PrepareToolTipTexts(string title, string text, string defaultTitle, out string toolTipTitle, out string toolTipText)
        {
            toolTipTitle = null;
            toolTipText = null;
            bool isTitle = !String.IsNullOrEmpty(title);
            bool isText = !String.IsNullOrEmpty(text);
            bool isDefaultTitle = !String.IsNullOrEmpty(defaultTitle);
            if (isTitle && isText && String.Equals(title, text))
            {   // Pokud máme dán Title i Text a oba jsou shodné,
                // pak Title zahodím (a případně použiju DefaultTitle jako titulek):
                title = null;
                isTitle = false;
            }
            if (!isTitle && !isText) return false;                       // Pokud není Title ani Text, pak Default Title sám o sobě neznamená existenci ToolTipu.

            if (isTitle && isText)
            {   // Máme titulek i text a ty jsou navzájem odlišné (to se řešilo před chvilkou):
                // : Jde o jednoznačnou věc:
                toolTipTitle = title;
                toolTipText = text;
                return true;
            }

            if (isDefaultTitle && (isTitle != isText))
            {   // Mám defaultní titulek (= text ve viditelném prvku - Ribbon BarItem, Menu Item),
                // a k tomu mám jen Titulek nebo jen Text (tedy XOR: jeden ano, a druhý ne),
                // Pak pokud ten zadaný 'Title' anebo 'Text' je stejný jako 'DefaultTitle', pak ToolTip nebude:
                if (isTitle)
                {   // Máme DefaultTitle a Title, a nemáme Text:
                    // : pokud Title == DefaultTitle, pak nemá význam zobrazit ToolTip (bylo by tam jen to, co už je vidět):
                    if (String.Equals(title, defaultTitle)) return false;
                    // : Jinak dám DefaultTitle => toolTipTitle a Title => toolTipText:
                    toolTipTitle = defaultTitle;
                    toolTipText = title;
                    return true;
                }

                if (isText)
                {   // Máme DefaultTitle a Text, a nemáme Title:
                    // : pokud Text == DefaultTitle, pak nemá význam zobrazit ToolTip (bylo by tam jen to, co už je vidět):
                    if (String.Equals(text, defaultTitle)) return false;
                    // : Jinak dám DefaultTitle => toolTipTitle a Text => toolTipText:
                    toolTipTitle = defaultTitle;
                    toolTipText = text;
                    return true;
                }

                return false;
            }

            // Defaultní chování:
            toolTipTitle = (isTitle ? title : (isDefaultTitle ? defaultTitle : null));
            toolTipText = (isText ? text : null);
            return true;
        }
        #endregion
        #region Tvorba prvků Toolbaru (=Ribbonu), static, bez Ribbonu
        /// <summary>
        /// Vytvoří pole BarItemů.
        /// Pokud určitý prvek má mít SubItems, pak jsou do něj vytvořeny. A to rekurzivně.
        /// Pokud některý takový prvek je LazyLoad (tzn. nemá připravené položky), pak je vytvořené SubMenu s eventem Popup (volá se v okamžiku rozbalení tohoto OnDemand submenu): <paramref name="onDemandMenuPopup"/>.
        /// </summary>
        /// <param name="ribbonItems"></param>
        /// <param name="barManager"></param>
        /// <param name="onDemandMenuPopup"></param>
        /// <param name="barLinks"></param>
        /// <returns></returns>
        public static DevExpress.XtraBars.BarItem[] CreateBarItems(IEnumerable<IRibbonItem> ribbonItems, DevExpress.XtraBars.BarManager barManager = null, Action<IRibbonItem> onDemandMenuPopup = null, DevExpress.XtraBars.BarItemLinkCollection barLinks = null)
        {
            return _CreateBarItems(ribbonItems, barManager, onDemandMenuPopup, 0, barLinks);
        }
        /// <summary>
        /// Vytvoří prvek BarItem podle zadání.
        /// Hodnota <paramref name="level"/> slouží k detailnímu nastavení při aplikování vlastností, 0 = přímo viditelný prvek (Button v Ribonu, první úroveň samostatného Popup).
        /// <para/>
        /// Ttao metoda nevytváří SubItems pro prvky, které je mají mít (Menu, dropDowm, atd). To musí zajistit volající metoda.
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barManager"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static DevExpress.XtraBars.BarItem CreateBarItem(IRibbonItem ribbonItem, DevExpress.XtraBars.BarManager barManager = null, int level = 0)
        {
            return _CreateBarItem(ribbonItem, barManager, level);
        }
        /// <summary>
        /// Naplní veškeré patřičné hodnoty do BarItemu { Caption, Visible, Tagy, Tooltip;  Styl písma;  Velikost, barvy, alignment;  Image;  HotKeys }
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="ribbonItem"></param>
        /// <param name="level"></param>
        internal static void FillBarItemFrom(DevExpress.XtraBars.BarItem barItem, IRibbonItem ribbonItem, int level = 0)
        {
            // Systémové prvky Ribbonu nebudu modifikovat:
            bool isSystemItem = (ribbonItem.ItemType == RibbonItemType.SkinSetDropDown || ribbonItem.ItemType == RibbonItemType.SkinPaletteDropDown || ribbonItem.ItemType == RibbonItemType.SkinPaletteGallery);
            if (isSystemItem)
            {
                _FillBarItemSystem(ribbonItem, barItem, level);          // Style
            }
            else
            {
                _FillBarItemCommon(ribbonItem, barItem, level);          // Caption, Visible, Tagy, Tooltip
                _FillBarItemFontStyle(ribbonItem, barItem, level);       // Styl písma
                _FillBarItemSizeColors(ribbonItem, barItem, level);      // Velikost, barvy, alignment
                _FillBarItemImage(ribbonItem, barItem, level);           // Image můžu řešit až po vložení velikosti, protože Image se řídí i podle velikosti prvku 
                _FillBarItemHotKey(ribbonItem, barItem, level);          // HotKeys
                _FillBarItemReload(ribbonItem, barItem, level);          // Speciální itemy mají svůj Reload (ComboBox)
            }
        }
        /// <summary>
        /// Připraví do prvku Ribbonu obrázek (ikonu) podle aktuálního stavu a dodané definice, pro button typu CheckButton nebo CheckBox
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barButton"></param>
        /// <param name="level"></param>
        public static void FillBarItemImageChecked(IRibbonItem ribbonItem, DevExpress.XtraBars.BarBaseButtonItem barButton, int? level = null)
        {
            _FillBarItemImageChecked(ribbonItem, barButton, level);
        }
        /// <summary>
        /// Do daného prvku <see cref="DevExpress.XtraBars.BarItem"/> vepíše vše pro jeho HotKey
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="menuItem"></param>
        public static void FillBarItemHotKey(DevExpress.XtraBars.BarItem barItem, IMenuItem menuItem)
        {
            if (menuItem.HotKeys.HasValue)
                barItem.ItemShortcut = new DevExpress.XtraBars.BarShortcut(menuItem.HotKeys.Value);
            else if (menuItem.Shortcut.HasValue)
                barItem.ItemShortcut = new DevExpress.XtraBars.BarShortcut(menuItem.Shortcut.Value);
            else if (!String.IsNullOrEmpty(menuItem.HotKey) && !(barItem is DevExpress.XtraBars.BarSubItem))
                barItem.ItemShortcut = new DevExpress.XtraBars.BarShortcut(SystemAdapter.GetShortcutKeys(menuItem.HotKey));

            if (!String.IsNullOrEmpty(menuItem.ShortcutText))
                barItem.ShortcutKeyDisplayString = menuItem.ShortcutText;
        }
        /// <summary>
        /// Vytvoří pole BarItemů.
        /// Pokud určitý prvek má mít SubItems, pak jsou do něj vytvořeny. A to rekurzivně.
        /// Pokud některý takový prvek je LazyLoad (tzn. nemá připravené položky), pak je vytvořené SubMenu s eventem Popup (volá se v okamžiku rozbalení tohoto OnDemand submenu): <paramref name="onDemandMenuPopup"/>.
        /// </summary>
        /// <param name="ribbonItems"></param>
        /// <param name="barManager"></param>
        /// <param name="onDemandMenuPopup"></param>
        /// <param name="level"></param>
        /// <param name="barLinks"></param>
        /// <returns></returns>
        private static DevExpress.XtraBars.BarItem[] _CreateBarItems(IEnumerable<IRibbonItem> ribbonItems, DevExpress.XtraBars.BarManager barManager, Action<IRibbonItem> onDemandMenuPopup, int level, DevExpress.XtraBars.BarItemLinkCollection barLinks)
        {
            if (ribbonItems is null) return null;
            List<DevExpress.XtraBars.BarItem> barItems = new List<DevExpress.XtraBars.BarItem>();
            bool prevHeader = false;                       // Předchozí prvek byl Header (po Headeru se nenastavuje BeginGroup, to vypadá blbě)
            bool isHeader = false;                         // Až vytvořím prvek v metodě _CreateBarItem(), pak zde bude příznak toho, že jde o BarHeaderItem
            bool isOpenGroup = false;
            foreach (var ribbonItem in ribbonItems)
            {
                var barItem = _CreateBarItem(ribbonItem, barManager, level, ref isHeader);
                if (barItem != null)
                {
                    // Pokud poslední prvek byla inlinovaná grupa, a nynější prvek není záhlaví (Header), tak musím vložit implicitní záhlaví - aby nepokračovala grupa (MultiColumn):
                    prepareVoidHeader();

                    // Přidat konkrétní prvek:
                    addItem(ribbonItem, barItem);

                    // SubMenu klasické (vnořené):
                    if (_IsBarItemSubItems(ribbonItem))
                        _FillBarSubItems(barItem, ribbonItem, barManager, onDemandMenuPopup, level);

                    // Inlinované SubPoložky ("Rychlá volba") = grupa prvků následujících za tímto záhlavím:
                    else if (_IsBarItemGroupItems(ribbonItem))
                    {
                        foreach (var subRibbonItem in ribbonItem.SubItems)
                        {
                            var barSubItem = _CreateBarItem(subRibbonItem, barManager, level, ref isHeader);
                            if (_IsBarItemSubItems(subRibbonItem))
                                _FillBarSubItems(barSubItem, subRibbonItem, barManager, onDemandMenuPopup, level);
                            addItem(subRibbonItem, barSubItem);
                        }
                        isOpenGroup = true;
                    }
                }
            }
            return barItems.ToArray();


            // Do pole prvků (barItems) přidá Header bez textu, tím ukončí právě otevřenou grupu MultiColumn - pokud je toho zapotřebí
            void prepareVoidHeader()
            {
                if (isHeader) isOpenGroup = false;                   // Pokud nynější prvek je reálný Header, pak nyní nemáme otevřenou grupu a nebude ani potřeba ji zavírat.
                if (!(isOpenGroup && !isHeader)) return;             // Pokud NENÍ (zůstala nám otevřená grupa, a aktuální prvek NENÍ záhlaví => to bychom museli přidat prázdné záhlaví pro oddělení grupy od dalších řádků mimo grupu) => není co řešit.

                // Do aktuálního soupisu přidám nový defaultní Header (bez MultiColumn, bez textu), a tím zajistím, že následující prvky budou zobrazeny jako běžné řádky:
                var rTitle = new DataRibbonItem() { ItemType = RibbonItemType.Header, RibbonStyle = RibbonItemStyles.SmallWithText };
                var bTitle = _CreateBarItem(rTitle, barManager, level, ref isHeader);
                addItem(rTitle, bTitle);

                // A tím je inlinovaná grupa uzavřena:
                isOpenGroup = false;
            }

            // Přidá další prvek do výstupní kolekce barItems i do Linků v barLinks
            void addItem(IRibbonItem rItem, DevExpress.XtraBars.BarItem bItem)
            {
                barItems.Add(bItem);
                if (barLinks != null)
                {
                    var bLink = barLinks.Add(bItem);
                    if (rItem.ItemIsFirstInGroup && !prevHeader && !isHeader) bLink.BeginGroup = true;   // Separátor se dělá takhle!
                }
                prevHeader = isHeader;
            }
        }
        /// <summary>
        /// Metoda do daného prvku <paramref name="barItem"/> vloží SubMenu z prvků dané definice menu <paramref name="menuRibbonItem"/>.
        /// Pokud prvek je OnDemand, pak zajistí vytvoření odpovídajícího menu do odpovídajícího eventu.
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="menuRibbonItem"></param>
        /// <param name="barManager"></param>
        /// <param name="onDemandMenuPopup"></param>
        /// <param name="level"></param>
        private static void _FillBarSubItems(DevExpress.XtraBars.BarItem barItem, IRibbonItem menuRibbonItem, DevExpress.XtraBars.BarManager barManager, Action<IRibbonItem> onDemandMenuPopup, int level)
        {
            if (barItem is DevExpress.XtraBars.BarSubItem barSubItem)
            {
                bool isOnDemand = (menuRibbonItem.SubItemsContentMode != RibbonContentMode.Static);
                if (isOnDemand)
                {   // OnDemand definované menu:
                    //  barSubItem.GetItemData += BarSubItem_GetItemData;             // Event je vyvolán okamžitě při vykreslení řádku obsahujícího Menu
                    barSubItem.Popup += _OnBarSubItem_OnDemand_Popup;             // Event je volán předtím, než se začne kreslit SubMenu, před PaintMenuBar
                    //  barSubItem.PaintMenuBar += BarSubItem_PaintMenuBar;           // Event je vyvolán v okamžiku, kdy komponenta začíná vykreslovat Child popup = SubItems tohoto menu

                    barSubItem.Tag = new DataRibbonItemLazy(menuRibbonItem, menuRibbonItem.SubItemsContentMode, onDemandMenuPopup);
                }

                // Pokud máme položky menu, tak je vložíme (i kdyby to bylo OnDemand menu):
                var subItems = menuRibbonItem.SubItems;
                if (subItems != null && subItems.Any())
                    _CreateBarItems(subItems, barManager, onDemandMenuPopup, level + 1, barSubItem.ItemLinks);
            }
        }

        // sada neúspěšných pokusů, jak zobrazit SubMenu až poté, kdy dostaneme platná data...:
        /*
          Možná by šlo:
             neřešit event Popup, 
             ale odchytit PaintMenuBar 
             a v něm zajistit vyžádání dat ze serveru : dataLazy.OnDemandMenuPopupAction?.Invoke(dataLazy.MenuRibbonItem);
             a hned poté nastavit e.Handled = true = potlačit vykreslení Popup
          Ale muselo by se zajistit reálné vykreslení po dodání dat
             a to i v režimu RibbonContentMode.OnDemandLoadEveryTime:
          Aktuálně nemám čas to zkoumat a testovat.

        */

        //private static void BarSubItem_PaintMenuBar(object sender, DevExpress.XtraBars.BarCustomDrawEventArgs e)
        //{
        //    if (sender is DevExpress.XtraBars.BarSubItem barSubItem && barSubItem.Tag is DataRibbonItemLazy dataLazy)
        //    {
        //        bool isOnDemand = (dataLazy.MenuRibbonItem.SubItemsContentMode != RibbonContentMode.Static);
        //        if (isOnDemand)
        //        {
        //            if (!dataLazy.IsActionCalled)
        //                e.Handled = true;
        //        }
        //    }
        //}

        //private static void BarSubItem_GetItemData(object sender, EventArgs e)
        //{

        //}

        /// <summary>
        /// Uživatel rozbaluje Submenu (BarSubItem), které má příznak <see cref="IRibbonItem.SubItemsContentMode"/> jiný než Static 
        /// (tedy <see cref="RibbonContentMode.OnDemandLoadOnce"/> nebo <see cref="RibbonContentMode.OnDemandLoadEveryTime"/>).
        /// Zdejší handler zajistí vyvolání akce, kterou předal volající jako "onDemandMenuPopup", a akci předá právě ten datový prvek SubMenu, který deklaruje toto OnDemand menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void _OnBarSubItem_OnDemand_Popup(object sender, EventArgs e)
        {
            if (sender is DevExpress.XtraBars.BarSubItem barSubItem && barSubItem.Tag is DataRibbonItemLazy dataLazy)
            {
                switch (dataLazy.ContentMode)
                {
                    case RibbonContentMode.OnDemandLoadOnce:
                        if (!dataLazy.IsActionCalled)
                        {
                            dataLazy.IsActionCalled = true;
                            dataLazy.OnDemandMenuPopupAction?.Invoke(dataLazy.MenuRibbonItem);
                        }
                        break;
                    case RibbonContentMode.OnDemandLoadEveryTime:
                        dataLazy.IsActionCalled = true;
                        dataLazy.OnDemandMenuPopupAction?.Invoke(dataLazy.MenuRibbonItem);
                        break;
                }
            }
        }
        /// <summary>
        /// Data připravená pro OnDemand submenu
        /// </summary>
        private class DataRibbonItemLazy
        {
            public DataRibbonItemLazy(IRibbonItem menuRibbonItem, RibbonContentMode contentMode, Action<IRibbonItem> onDemandMenuPopup)
            {
                MenuRibbonItem = menuRibbonItem;
                ContentMode = contentMode;
                OnDemandMenuPopupAction = onDemandMenuPopup;
                IsActionCalled = false;
            }
            public IRibbonItem MenuRibbonItem { get; private set; }
            internal RibbonContentMode ContentMode { get; private set; }
            public Action<IRibbonItem> OnDemandMenuPopupAction { get; private set; }
            public bool IsActionCalled { get; set; }
        }
        /// <summary>
        /// Vrátí true, pokud dodaný prvek reprezentuje hlavičku od SubMenu, 
        /// a je tedy třeba do BarItemu tohoto prvku vložit SubItemy...
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <returns></returns>
        private static bool _IsBarItemSubItems(IRibbonItem ribbonItem)
        {
            var itemType = ribbonItem.ItemType;
            return (itemType == RibbonItemType.ComboListBox || itemType == RibbonItemType.InRibbonGallery || itemType == RibbonItemType.Menu || itemType == RibbonItemType.PopupMenuDropDown);
        }
        /// <summary>
        /// Vrátí true, pokud dodaný prvek reprezentuje hlavičku od Inlinované grupy prvků (Rychlá volba), 
        /// a je tedy třeba přímo za daný BarItem tohoto prvku vložit jeho SubItemy jako následující prvky...
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <returns></returns>
        private static bool _IsBarItemGroupItems(IRibbonItem ribbonItem)
        {
            var itemType = ribbonItem.ItemType;
            return (itemType == RibbonItemType.ButtonGroup);
        }
        /// <summary>
        /// Vytvoří BarItem a naplní jeho vlastnosti. Neřeší SubItems.
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barManager"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private static DevExpress.XtraBars.BarItem _CreateBarItem(IRibbonItem ribbonItem, DevExpress.XtraBars.BarManager barManager, int level)
        {
            bool isHeader = false;
            return _CreateBarItem(ribbonItem, barManager, level, ref isHeader);
        }
        /// <summary>
        /// Vytvoří BarItem a naplní jeho vlastnosti. Neřeší SubItems.
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barManager"></param>
        /// <param name="level"></param>
        /// <param name="isHeader"></param>
        /// <returns></returns>
        private static DevExpress.XtraBars.BarItem _CreateBarItem(IRibbonItem ribbonItem, DevExpress.XtraBars.BarManager barManager, int level, ref bool isHeader)
        {
            if (ribbonItem is null) return null;
            DevExpress.XtraBars.BarItem barItem = null;
            isHeader = false;
            switch (ribbonItem.ItemType)
            {
                case RibbonItemType.Static:
                case RibbonItemType.Label:
                    var barStaticButton = new DevExpress.XtraBars.BarStaticItem();
                    barItem = barStaticButton;
                    break;
                case RibbonItemType.Button:
                    var barButton = new DevExpress.XtraBars.BarButtonItem();
                    barItem = barButton;
                    break;
                case RibbonItemType.CheckButton:
                case RibbonItemType.CheckButtonPassive:
                    var barCheckButton = new DevExpress.XtraBars.BarButtonItem();
                    barCheckButton.ButtonStyle = DevExpress.XtraBars.BarButtonStyle.Check;
                    barCheckButton.Down = (ribbonItem.Checked.HasValue && ribbonItem.Checked.Value);
                    barItem = barCheckButton;
                    break;
                case RibbonItemType.CheckBoxStandard:
                case RibbonItemType.CheckBoxPasive:
                    var barBoxButton = new DevExpress.XtraBars.BarButtonItem();        // CheckBox z něj udělá až metoda _FillBarItemProperties
                    barItem = barBoxButton;
                    break;
                case RibbonItemType.ButtonGroup:
                    var buttonGroup = _CreateBarItemButtonGroup(ribbonItem, barManager, level);
                    barItem = buttonGroup;
                    isHeader = true;
                    break;
                case RibbonItemType.Menu:
                case RibbonItemType.PopupMenuDropDown:
                    var barSubItem = new DevExpress.XtraBars.BarSubItem();
                    barItem = barSubItem;
                    break;
                case RibbonItemType.SkinSetDropDown:
                    var barSkin = new DevExpress.XtraBars.SkinDropDownButtonItem();
                    barItem = barSkin;
                    break;
                case RibbonItemType.SkinPaletteDropDown:
                case RibbonItemType.SkinPaletteGallery:
                    var barPalette = new DevExpress.XtraBars.SkinPaletteDropDownButtonItem();
                    barItem = barPalette;
                    break;
                case RibbonItemType.Header:
                    var barHeader = _CreateBarItemHeader(ribbonItem, barManager, level);
                    barHeader.MultiColumn = DefaultBoolean.False;
                    barItem = barHeader;
                    isHeader = true;
                    break;
                default:
                    barItem = new DevExpress.XtraBars.BarButtonItem();
                    break;

            }
            if (barManager != null) barItem.Manager = barManager;
            _FillBarItemProperties(ribbonItem, barItem, level);
            return barItem;
        }
        /// <summary>
        /// Vytvoří záhlaví obecné a nastaví jeho vlastnosti
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barManager"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private static DevExpress.XtraBars.BarHeaderItem _CreateBarItemHeader(IRibbonItem ribbonItem, DevExpress.XtraBars.BarManager barManager, int level)
        {
            var barHeader = new DevExpress.XtraBars.BarHeaderItem();
            barHeader.MultiColumn = DefaultBoolean.False;

            barHeader.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText;
            barHeader.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionInMenu;
            barHeader.ContentHorizontalAlignment = DevExpress.XtraBars.BarItemContentAlignment.Near;
            barHeader.ImageToTextAlignment = DevExpress.XtraBars.BarItemImageToTextAlignment.BeforeText;

            return barHeader;
        }
        /// <summary>
        /// Vytvoří záhlaví pro ButtonGroup a nastaví jeho vlastnosti
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barManager"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private static DevExpress.XtraBars.BarHeaderItem _CreateBarItemButtonGroup(IRibbonItem ribbonItem, DevExpress.XtraBars.BarManager barManager, int level)
        {
            bool useLargeImages = (ribbonItem.RibbonStyle.HasFlag(RibbonItemStyles.Large) || !(ribbonItem.RibbonStyle.HasFlag(RibbonItemStyles.SmallWithText) || ribbonItem.RibbonStyle.HasFlag(RibbonItemStyles.SmallWithoutText)));
            var buttonGroup = new DevExpress.XtraBars.BarHeaderItem();
            buttonGroup.MultiColumn = DefaultBoolean.True;
            buttonGroup.OptionsMultiColumn.ColumnCount = ribbonItem.ButtonGroupColumnCount ?? 7;
            buttonGroup.OptionsMultiColumn.ImageHorizontalAlignment = DevExpress.Utils.Drawing.ItemHorizontalAlignment.Left;
            buttonGroup.OptionsMultiColumn.ImageVerticalAlignment = DevExpress.Utils.Drawing.ItemVerticalAlignment.Top;
            buttonGroup.OptionsMultiColumn.LargeImages = (useLargeImages ? DefaultBoolean.True : DefaultBoolean.False);
            buttonGroup.OptionsMultiColumn.UseMaxItemWidth = DevExpress.Utils.DefaultBoolean.False;

            buttonGroup.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText;
            // buttonGroup.RibbonStyle = (useLargeImages ? DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large : DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText);
            buttonGroup.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionInMenu;
            buttonGroup.ContentHorizontalAlignment = DevExpress.XtraBars.BarItemContentAlignment.Near;
            buttonGroup.ImageToTextAlignment = DevExpress.XtraBars.BarItemImageToTextAlignment.BeforeText;

            return buttonGroup;
        }
        /// <summary>
        /// Z datového prvku <paramref name="ribbonItem"/> opíše veškeré hodnoty do vizuálního prvku <paramref name="barItem"/>. 
        /// Prováže je pomocí Tagu a RibbonItem.
        /// Hodnota <paramref name="level"/> slouží k detailnímu nastavení při aplikování vlastností, 0 = přímo viditelný prvek (Button v Ribonu, první úroveň samostatného Popup)
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barItem"></param>
        /// <param name="level"></param>
        private static void _FillBarItemProperties(IRibbonItem ribbonItem, DevExpress.XtraBars.BarItem barItem, int level)
        {
            FillBarItemFrom(barItem, ribbonItem, level);

            // Vzájemné provázání:
            barItem.Tag = ribbonItem;            // Vizuální prvek zná data
            ribbonItem.RibbonItem = barItem;     // Datový prvek má Weakreferenci na vizuál
        }
        /// <summary>
        /// Nastaví základní běžné hodnoty do do daného prvku = systémový prvek, jen některé věci
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barItem"></param>
        /// <param name="level"></param>
        private static void _FillBarItemSystem(IRibbonItem ribbonItem, DevExpress.XtraBars.BarItem barItem, int level)
        {
            barItem.Enabled = ribbonItem.Enabled;
            barItem.Visibility = ribbonItem.Visible ? DevExpress.XtraBars.BarItemVisibility.Always : DevExpress.XtraBars.BarItemVisibility.Never;
            barItem.VisibleInSearchMenu = ribbonItem.VisibleInSearchMenu;
            barItem.RibbonStyle = (ribbonItem.RibbonStyle == RibbonItemStyles.Default ? DevExpress.XtraBars.Ribbon.RibbonItemStyles.All : DxRibbonControl.Convert(ribbonItem.RibbonStyle));
        }
        /// <summary>
        /// Nastaví základní běžné hodnoty do do daného prvku
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barItem"></param>
        /// <param name="level"></param>
        private static void _FillBarItemCommon(IRibbonItem ribbonItem, DevExpress.XtraBars.BarItem barItem, int level)
        {
            barItem.Caption = ribbonItem.Text ?? "";
            barItem.Enabled = ribbonItem.Enabled;
            barItem.Visibility = ribbonItem.Visible ? DevExpress.XtraBars.BarItemVisibility.Always : DevExpress.XtraBars.BarItemVisibility.Never;
            barItem.VisibleInSearchMenu = ribbonItem.VisibleInSearchMenu;
            barItem.SearchTags = ribbonItem.SearchTags;
            barItem.PaintStyle = DxRibbonControl.ConvertPaintStyle(ribbonItem, level);

            // Header má styl jinak:
            bool isHeader = (ribbonItem.ItemType == RibbonItemType.Header || ribbonItem.ItemType == RibbonItemType.ButtonGroup);
            barItem.RibbonStyle = isHeader ? 
                    DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText : 
                    (ribbonItem.RibbonStyle == RibbonItemStyles.Default ? DevExpress.XtraBars.Ribbon.RibbonItemStyles.All : DxRibbonControl.Convert(ribbonItem.RibbonStyle));

            barItem.SuperTip = DxComponent.CreateDxSuperTip(ribbonItem);
        }
        /// <summary>
        /// Nastaví dodaný styl písma do daného prvku, do jeho odpovídající Appearance, do všech stavů
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barItem"></param>
        /// <param name="level"></param>
        private static void _FillBarItemFontStyle(IRibbonItem ribbonItem, DevExpress.XtraBars.BarItem barItem, int level)
        {
            if (ribbonItem.FontStyle.HasValue)
            {
                var fontStyle = ribbonItem.FontStyle.Value;
                var appearance = ((level == 0) ? barItem.ItemAppearance : barItem.ItemInMenuAppearance);
                appearance.Normal.FontStyleDelta = fontStyle;
                appearance.Hovered.FontStyleDelta = fontStyle;
                appearance.Pressed.FontStyleDelta = fontStyle;
                appearance.Disabled.FontStyleDelta = fontStyle;
            }
        }
        /// <summary>
        /// Nastaví barvy a velikost do daného prvku, do jeho odpovídající Appearance, do všech stavů
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barItem"></param>
        /// <param name="level"></param>
        private static void _FillBarItemSizeColors(IRibbonItem ribbonItem, DevExpress.XtraBars.BarItem barItem, int level)
        {
            if (ribbonItem.Size.HasValue)
            {
                barItem.Size = ribbonItem.Size.Value;
            }

            // Barvy: pokud na vstupu je null (=barva není určena), i pak je třeba zajistit její promítnutí do prvku! 
            // Proč? Jde třeba o Refresh, který má vrátit barvu prvku z výrazné (explicitně dané) do neutrální (jen namísto NULL vložím Empty)!
            Color? backColor = null;
            Color? textColor = null;
            if (ribbonItem.BackColor.HasValue || ribbonItem.TextColor.HasValue)
            {   // Explicitní barvy z deklarace prvku:
                backColor = ribbonItem.BackColor;
                textColor = ribbonItem.TextColor;
            }
            else if (!String.IsNullOrEmpty(ribbonItem.StyleName))
            {   // Barvy definované v kalíšku:
                StyleInfo style = GetStyleInfo(ribbonItem.StyleName);
                backColor = style?.LabelBgColor;
                textColor = style?.LabelColor;
            }

            // Barvu vepíšu vždy (pro Refresh, viz nahoře):
            var appearance = ((level == 0) ? barItem.ItemAppearance : barItem.ItemInMenuAppearance);
            Color backCol = backColor ?? Color.Empty;
            if (appearance.Normal.BackColor != backCol) appearance.Normal.BackColor = backCol;
            if (appearance.Hovered.BackColor != backCol) appearance.Hovered.BackColor = backCol;
            if (appearance.Pressed.BackColor != backCol) appearance.Pressed.BackColor = backCol;
            if (appearance.Disabled.BackColor != backCol) appearance.Disabled.BackColor = backCol;

            Color textCol = textColor ?? Color.Empty;
            if (appearance.Normal.ForeColor != textCol) appearance.Normal.ForeColor = textCol;
            if (appearance.Hovered.ForeColor != textCol) appearance.Hovered.ForeColor = textCol;
            if (appearance.Pressed.ForeColor != textCol) appearance.Pressed.ForeColor = textCol;
            if (appearance.Disabled.ForeColor != textCol) appearance.Disabled.ForeColor = textCol;

            // Zarovnání dle požadavku:
            if (ribbonItem.Alignment.HasValue)
            {
                barItem.Alignment = (ribbonItem.Alignment.Value == BarItemAlignment.Default ? DevExpress.XtraBars.BarItemLinkAlignment.Default :
                                    (ribbonItem.Alignment.Value == BarItemAlignment.Left ? DevExpress.XtraBars.BarItemLinkAlignment.Left :
                                    (ribbonItem.Alignment.Value == BarItemAlignment.Right ? DevExpress.XtraBars.BarItemLinkAlignment.Right : DevExpress.XtraBars.BarItemLinkAlignment.Default)));
            }
        }
        /// <summary>
        /// Nastaví dodaný styl písma do daného prvku, do jeho odpovídající Appearance, do všech stavů
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barItem"></param>
        /// <param name="level"></param>
        private static void _FillBarItemImage(IRibbonItem ribbonItem, DevExpress.XtraBars.BarItem barItem, int level)
        {
            var itemType = ribbonItem.ItemType;
            if ((itemType == RibbonItemType.CheckButton || itemType == RibbonItemType.CheckButtonPassive || itemType == RibbonItemType.CheckBoxStandard || itemType == RibbonItemType.CheckBoxPasive || itemType == RibbonItemType.RadioItem) && barItem is DevExpress.XtraBars.BarBaseButtonItem barButton)
                _FillBarItemImageChecked(ribbonItem, barButton, level);    // S možností volby podle Checked
            else
                _FillBarItemImageStandard(ribbonItem, barItem, level);
        }
        /// <summary>
        /// Připraví do prvku Ribbonu obrázek (ikonu) podle aktuálního stavu a dodané definice, pro standardní button
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barItem"></param>
        /// <param name="level"></param>
        private static void _FillBarItemImageStandard(IRibbonItem ribbonItem, DevExpress.XtraBars.BarItem barItem, int? level = null)
        {
            // Určíme, zda prvek je přímo v Ribbonu nebo až jako subpoložka:
            //  Hodnota level se předává v procesu prvotní tvorby, pak Root prvek má level == 0;
            //  Pokud hodnota level není předána, pak jsme volání z obsluhy kliknutí na prvek, a tam se spolehneme na hodnotu IRibbonItem.ParentItem.
            bool isRootItem = (level.HasValue ? (level.Value == 0) : (ribbonItem.ParentItem is null));

            // Velikost obrázku: pro RootItem (vlastní prvky v Ribbonu) ve stylu Large nebo Default dáme obrázky Large, jinak dáme Small (pro malé prvky Ribbonu a pro položky menu, ty mají Level 1 a vyšší):
            bool isLargeIcon = (isRootItem && (ribbonItem.RibbonStyle.HasFlag(RibbonItemStyles.Large) || ribbonItem.RibbonStyle == RibbonItemStyles.Default));
            ResourceImageSizeType sizeType = (isLargeIcon ? ResourceImageSizeType.Large : ResourceImageSizeType.Small);

            // Náhradní ikonka (pro nezadané nebo neexistující ImageName) budeme generovat jen pro level = 0 = Ribbon, a ne pro Menu!
            string imageCaption = GetCaptionForRibbonImage(ribbonItem, level);
            DxComponent.ApplyImage(barItem.ImageOptions, ribbonItem.ImageName, ribbonItem.Image, sizeType, caption: imageCaption, prepareDisabledImage: ribbonItem.PrepareDisabledImage, imageListMode: ribbonItem.ImageListMode);

            // Pokud nemám Image ani imageCaption, pak je třeba upravit PaintStyle tak, aby BarStatic prvek nezobrazoval přeškrtnutý Image:
            barItem.PaintStyle = DxRibbonControl.ConvertPaintStyle(ribbonItem, out bool hasImage, level);
            if (!hasImage)
                barItem.ImageOptions.Reset();
        }
        /// <summary>
        /// Připraví do prvku Ribbonu obrázek (ikonu) podle aktuálního stavu a dodané definice, pro button typu CheckButton nebo CheckBox
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barButton"></param>
        /// <param name="level"></param>
        private static void _FillBarItemImageChecked(IRibbonItem ribbonItem, DevExpress.XtraBars.BarBaseButtonItem barButton, int? level = null)
        {
            // Určíme, zda prvek je přímo v Ribbonu nebo až jako subpoložka:
            //  Hodnota level se předává v procesu prvotní tvorby, pak Root prvek má level == 0;
            //  Pokud hodnota level není předána, pak jsme volání z obsluhy kliknutí na prvek, a tam se spolehneme na hodnotu IRibbonItem.ParentItem.
            bool isRootItem = (level.HasValue ? (level.Value == 0) : (ribbonItem.ParentItem is null));

            // Velikost obrázku: pro RootItem (vlastní prvky v Ribbonu) ve stylu Large nebo Default dáme obrázky Large, jinak dáme Small (pro malé prvky Ribbonu a pro položky menu, ty mají Level 1 a vyšší):
            bool isLargeIcon = (isRootItem && (ribbonItem.RibbonStyle.HasFlag(RibbonItemStyles.Large) || ribbonItem.RibbonStyle == RibbonItemStyles.Default));
            ResourceImageSizeType sizeType = (isLargeIcon ? ResourceImageSizeType.Large : ResourceImageSizeType.Small);

            // Náhradní ikonka (pro nezadané nebo neexistující ImageName) budeme generovat jen pro level = 0 = Ribbon, a ne pro Menu!
            string imageCaption = GetCaptionForRibbonImage(ribbonItem, level);

            // Zvolíme aktuálně platný obrázek - podle hodnoty iRibbonItem.Checked a pro zadáné obrázky:
            string imageName = (!ribbonItem.Checked.HasValue ? ribbonItem.ImageName :                                                                     // Pro hodnotu NULL
                   ((ribbonItem.Checked.HasValue && !ribbonItem.Checked.Value) ? _GetDefinedImage(ribbonItem.ImageNameUnChecked, ribbonItem.ImageName) :  // pro False
                   ((ribbonItem.Checked.HasValue && ribbonItem.Checked.Value) ? _GetDefinedImage(ribbonItem.ImageNameChecked, ribbonItem.ImageName) :     // pro True
                   null)));
            var image = ribbonItem.Image;

            // Pozor, pro DevExpress platí:
            // Máme-li prvek na SubPoložce v menu, a prvek je Checked, pak nesmí mít Image - protože DevExpress nezobrazuje vedle sebe Image a CheckIcon, ale zobrazí prioritně Image a tím skryje CheckIcon:
            if (!isRootItem && ribbonItem.Checked.HasValue && ribbonItem.Checked.Value)
            {
                imageName = null;
                image = null;
            }
            DxComponent.ApplyImage(barButton.ImageOptions, imageName, ribbonItem.Image, sizeType, caption: imageCaption, prepareDisabledImage: ribbonItem.PrepareDisabledImage, imageListMode: ribbonItem.ImageListMode);

            // Pokud nemám Image ani imageCaption, pak je třeba upravit PaintStyle tak, aby BarStatic prvek nezobrazoval přeškrtnutý Image:
            barButton.PaintStyle = DxRibbonControl.ConvertPaintStyle(ribbonItem, level);
        }
        /// <summary>
        /// Metoda vrátí text, z něhož bude možno vytvořit náhradní Image pro daný prvek Ribbonu <paramref name="ribbonItem"/>.
        /// Metoda akceptuje nastavení prvku, danou úroveň zanoření prvku i jeho text.
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        internal static string GetCaptionForRibbonImage(IRibbonItem ribbonItem, int? level)
        {
            // Určíme, zda prvek je přímo v Ribbonu nebo až jako subpoložka:
            //  Hodnota level se předává v procesu prvotní tvorby, pak Root prvek má level == 0;
            //  Pokud hodnota level není předána, pak jsme volání z obsluhy kliknutí na prvek, a tam se spolehneme na hodnotu IRibbonItem.ParentItem.
            bool isRootItem = (level.HasValue ? (level.Value == 0) : (ribbonItem.ParentItem is null));
            var mode = ribbonItem.ImageFromCaptionMode;
            bool enableImageFromCaption =
                    (mode == ImageFromCaptionType.Disabled ? false :
                    (mode == ImageFromCaptionType.OnlyForRootMenuLevel ? isRootItem :
                    (mode == ImageFromCaptionType.Enabled ? true : false)));
            string imageCaption = (enableImageFromCaption ? ribbonItem.Text : null);
            return imageCaption;
        }
        /// <summary>
        /// Vrátí první neprázdný obrázek
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        private static string _GetDefinedImage(params string[] images)
        {
            return images.FirstOrDefault(i => !String.IsNullOrEmpty(i));
        }
        /// <summary>
        /// Do daného prvku Ribbonu vepíše vše pro jeho HotKey
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barItem"></param>
        /// <param name="level"></param>
        private static void _FillBarItemHotKey(IRibbonItem ribbonItem, DevExpress.XtraBars.BarItem barItem, int level)
        {
            if (ribbonItem.HotKeys.HasValue)
                barItem.ItemShortcut = new DevExpress.XtraBars.BarShortcut(ribbonItem.HotKeys.Value);
            else if (ribbonItem.Shortcut.HasValue)
                barItem.ItemShortcut = new DevExpress.XtraBars.BarShortcut(ribbonItem.Shortcut.Value);
            else if (!string.IsNullOrEmpty(ribbonItem.HotKey) && !(barItem is DevExpress.XtraBars.BarSubItem))
                barItem.ItemShortcut = new DevExpress.XtraBars.BarShortcut(SystemAdapter.GetShortcutKeys(ribbonItem.HotKey));
        }
        /// <summary>
        /// Pro daný prvek Ribbonu, pokud implementuje <see cref="IReloadable"/>, zavolá jeho metodu <see cref="IReloadable.Reload"/>
        /// </summary>
        /// <param name="ribbonItem"></param>
        /// <param name="barItem"></param>
        /// <param name="level"></param>
        private static void _FillBarItemReload(IRibbonItem ribbonItem, DevExpress.XtraBars.BarItem barItem, int level)
        {
            if (barItem is IReloadable reloadable)
                reloadable.ReloadFrom(ribbonItem);
        }
        #endregion
        #region Pozice okna, a multimonitor
        /// <summary>
        /// Vrátí pozici daného okna. Následně se okno na tuto pozici může umístit voláním metody <see cref="FormPositionSet(Form, string, bool)"/>.
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public static string FormPositionGet(Form form)
        {
            if (form is null) return "";
            var state = form.WindowState;
            var bounds = (state == FormWindowState.Normal ? form.Bounds : form.RestoreBounds);
            string s = (state == FormWindowState.Maximized ? "X" : (state == FormWindowState.Normal ? "B" : "N"));
            return $"{s},{bounds.X},{bounds.Y},{bounds.Width},{bounds.Height}";
        }
        /// <summary>
        /// Do daného okna nastaví pozici a souřadnice dle daného stringu, který byl vygenerován metodou <see cref="FormPositionGet(Form)"/>.
        /// Může/nemusí souřadnice okna zarovnat do viditelných monitorů.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="position"></param>
        /// <param name="skipAlign">Pokud bude false (default), pak se dodané souřadnice zarovnají do aktuálně připojených monitorů. To je lepší pro to, že nově otevřené okno se neotevře na nějaké neviditelné pozici.
        /// Hodnota true tuto akci přeskočí, okno bude umístěno přesně tam, kde je zadáno, i když nebude vidět.</param>
        public static bool FormPositionSet(Form form, string position, bool skipAlign = false)
        {
            if (form is null || position is null || position.Length < 9) return false;
            var items = position.Split(',');
            if (items.Length < 5) return false;
            if ((items[0] == "X" || items[0] == "B" || items[0] == "N") &&
                (Int32.TryParse(items[1], out int x)) &&
                (Int32.TryParse(items[2], out int y)) &&
                (Int32.TryParse(items[3], out int w) && w > 0) &&
                (Int32.TryParse(items[4], out int h) && h > 0))
            {
                FormWindowState state = (items[0] == "X" ? FormWindowState.Maximized : (items[0] == "B" ? FormWindowState.Normal : FormWindowState.Minimized));
                Rectangle bounds = new Rectangle(x, y, w, h);
                if (!skipAlign)
                    bounds = bounds.FitIntoMonitors();
                form.WindowState = FormWindowState.Normal;
                if (state == FormWindowState.Normal)
                    form.StartPosition = FormStartPosition.Manual;
                form.Bounds = bounds;
                form.WindowState = state;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Aktuální klíč konfigurace všech monitorů.
        /// <para/>
        /// String popisuje všechny aktuálně přítomné monitory, jejich příznak Primární, a jejich souřadnice.<br/>
        /// String lze použít jako klíč: obsahuje pouze písmena a číslice, nic více.<br/>
        /// Ze stringu nelze rozumně sestavit souřadnice monitorů, to ale není nutné. Cílem je krátký klíč.<br/>
        /// String slouží jako suffix klíče pro ukládání souřadnic uživatelských oken, aby bylo možno je ukládat pro jednotlivé konfigurace monitorů.
        /// Je vhodné ukládat souřadnice pracovních oken pro jejich restorování s klíčem aktuální konfigurace monitorů: 
        /// uživatel používající různé konfigurace monitorů očekává, že konkrétní okno se mu po otevření zobrazí na konkrétním místě v závislosti na tom, které monitory právě používá.
        /// </summary>
        public static string CurrentMonitorsKey { get { return _GetCurrentMonitorsKey(); } }
        /// <summary>
        /// Určí a vrátí Aktuální klíč konfigurace všech monitorů.
        /// </summary>
        /// <returns></returns>
        private static string _GetCurrentMonitorsKey()
        {
            string key = "";
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                key += getOneKey(screen);

            // key = "PaapjSzwz1371rm"
            return key;

            string getOneKey(System.Windows.Forms.Screen scr)
            {
                // one = "Paapj", anebo se zápornými čísly a s číselnými hodnotami: "Szwz1371rm"
                var bounds = scr.Bounds;
                string one = (scr.Primary ? "P" : "S") + getSiz(bounds.X) + getSiz(bounds.Y) + getSiz(bounds.Width) + getSiz(bounds.Height);
                return one;
            }

            string getSiz(int siz)
            {
                string prefix = (siz < 0 ? "z" : "");
                if (siz < 0) siz = -siz;
                switch (siz)
                {   // Standardní rozměry převedu na jednopísmeno:
                    case 0000: return prefix + "a";
                    case 0320: return prefix + "b";
                    case 0480: return prefix + "c";
                    case 0540: return prefix + "d";
                    case 0640: return prefix + "e";
                    case 0720: return prefix + "f";
                    case 0800: return prefix + "g";
                    case 0960: return prefix + "h";
                    case 1024: return prefix + "i";
                    case 1080: return prefix + "j";
                    case 1280: return prefix + "k";
                    case 1366: return prefix + "l";
                    case 1440: return prefix + "m";
                    case 1536: return prefix + "n";
                    case 1728: return prefix + "o";
                    case 1920: return prefix + "p";
                    case 2160: return prefix + "q";
                    case 2560: return prefix + "r";
                    case 2880: return prefix + "s";
                    case 3072: return prefix + "t";
                    case 3200: return prefix + "u";
                    case 3440: return prefix + "v";
                    case 3840: return prefix + "w";
                    case 4320: return prefix + "x";
                    case 4880: return prefix + "y";
                }
                // Nestandardní rozměry a pozice oken (X, Y) mimo std hodnoty budou numericky:
                return prefix + siz.ToString();
            }

        }
        #endregion
        #region Práce s klávesovými kódy - KeyArgs
        /// <summary>
        /// Vrátí true, pokjud daná klávesa je pouze modifikátor bez další významné klávesy.
        /// </summary>
        /// <param name="keyArgs"></param>
        /// <returns></returns>
        public static bool KeyIsOnlyModifier(KeyEventArgs keyArgs) { return KeyIsOnlyModifier(keyArgs.KeyCode); }
        /// <summary>
        /// Vrátí true, pokjud daná klávesa je pouze modifikátor bez další významné klávesy.
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        public static bool KeyIsOnlyModifier(Keys keyCode)
        {
            return (keyCode == Keys.Capital ||                                 // Caps lock
                    keyCode == Keys.ShiftKey ||
                    keyCode == Keys.ControlKey ||
                    keyCode == Keys.LWin ||                                    // Wokno
                    keyCode == Keys.Apps ||                                    // Kontextové menu
                    keyCode == Keys.Menu);
        }
        /// <summary>
        /// Vrátí Char odpovídající daným datům klávesnice.
        /// Pokud daná klávesa není znak, ale nějaký ovládací prvek, pak vrátí null (= typicky modifikátory, pohyby kurzoru, Ctrl+něco, Fnn, aplikační klávesy).
        /// </summary>
        /// <param name="keyArgs"></param>
        /// <param name="useLocalizedKeys"></param>
        /// <returns></returns>
        public static char? KeyConvertToChar(KeyEventArgs keyArgs, bool useLocalizedKeys)
        {
            if (KeyIsOnlyModifier(keyArgs)) return null;                       // Samotné modifikační klávesy nejsou znak

            // Modifikátory:
            bool isCtrlMod = keyArgs.KeyData.HasFlag(Keys.Control);            // Je zmáčknutý 'Ctrl'
            bool isShiftMod = (keyArgs.KeyData.HasFlag(Keys.Shift));           // Je zmáčknutý 'Shift'
            bool isAltMod = (keyArgs.KeyData.HasFlag(Keys.Alt));               // Je zmáčknutý 'Alt'
            bool isAltGr = isCtrlMod && isAltMod;                              // klávesa      'AltGr' se projevuje tak, že v KeyData je Control + Alt

            if (isCtrlMod && !isAltMod) return null;                           // Ctrl bez Alt je např. Ctrl+C, Ctrl+A (clipboard, editor) nebo Ctrl+S (klávesová zkratka). Ale ne 'Znak'.
            if (isShiftMod && isAltGr) return null;                            // 'Shift' + 'AltGr' nic nepíše

            bool isCapsLock = Console.CapsLock;

            // Index znaku v řetězci, který odpovídá aktuálním modifikátorům:
            int index = (isAltGr ? 5 : (isCapsLock ? 2 : 0) + (isShiftMod ? 1 : 0));

            // Jednotlivé klávesy a jejich znaky s různými modifikátory:
            if (useLocalizedKeys)
            {   // CZ klávesnice:
                switch (keyArgs.KeyCode)                // KeyCode obsahuje klávesu, pouze klávesu očištěnou o modifikátory
                {
                    // Esc a Fn:
                    case Keys.Escape: return null;
                    case Keys.F1: return null;
                    case Keys.F2: return null;
                    case Keys.F3: return null;
                    case Keys.F4: return null;
                    case Keys.F5: return null;
                    case Keys.F6: return null;
                    case Keys.F7: return null;
                    case Keys.F8: return null;
                    case Keys.F9: return null;
                    case Keys.F10: return null;
                    case Keys.F11: return null;
                    case Keys.F12: return null;


                    // Horní řada
                    case Keys.Oemtilde: return getChar(";°;°  ");
                    case Keys.D1: return getChar("+1+1 ~ ");
                    case Keys.D2: return getChar("ě2Ěě ˇ ");
                    case Keys.D3: return getChar("š3Š3 ^ ");
                    case Keys.D4: return getChar("č4Č4 ˘ ");
                    case Keys.D5: return getChar("ř5Ř5 ° ");
                    case Keys.D6: return getChar("ž6Ž6 ˛ ");
                    case Keys.D7: return getChar("ý7Ý7 ` ");
                    case Keys.D8: return getChar("á8Á8 · ");
                    case Keys.D9: return getChar("í9Í9 ´ ");
                    case Keys.D0: return getChar("é0É0 ˝ ");
                    case Keys.Oemplus: return getChar("=%=% ¨ ");
                    case Keys.OemQuestion: return getChar("´ˇ´ˇ ¸ ");
                    case Keys.Back: return null;

                    // QWER
                    case Keys.Tab: return null;
                    case Keys.Q: return getChar("qQQq \\ ");
                    case Keys.W: return getChar("wWwW | ");
                    case Keys.E: return getChar("eEEe € ");
                    case Keys.R: return getChar("rRRr   ");
                    case Keys.T: return getChar("tTTt   ");
                    case Keys.Z: return getChar("zZZz   ");
                    case Keys.U: return getChar("uUUu   ");
                    case Keys.I: return getChar("iIIi   ");
                    case Keys.O: return getChar("oOOo   ");
                    case Keys.P: return getChar("pPPp   ");
                    case Keys.OemOpenBrackets: return getChar("ú/Ú/ ÷ ");
                    case Keys.Oem6: return getChar(")()( × ");
                    case Keys.Oem5: return getChar("¨'¨' ¨ ");

                    // ASDF
                    case Keys.Capital: return null;
                    case Keys.A: return getChar("aAAa   ");
                    case Keys.S: return getChar("sSSs đ ");
                    case Keys.D: return getChar("dDDd Đ ");
                    case Keys.F: return getChar("fFFf [ ");
                    case Keys.G: return getChar("gGGg ] ");
                    case Keys.H: return getChar("hHHh   ");
                    case Keys.J: return getChar("jJJj   ");
                    case Keys.K: return getChar("kKKk ł ");
                    case Keys.L: return getChar("lLLl Ł ");
                    case Keys.Oem1: return getChar("ů\"Ů\" $ ");
                    case Keys.Oem7: return getChar("§!§! ß ");
                    case Keys.Return: return null;

                    // YXCV
                    case Keys.Y: return getChar("yYYy   ");
                    case Keys.X: return getChar("xXXx # ");
                    case Keys.C: return getChar("cCCc & ");
                    case Keys.V: return getChar("vVVv @ ");
                    case Keys.B: return getChar("bBBb { ");
                    case Keys.N: return getChar("nNNn } ");
                    case Keys.M: return getChar("mMMm   ");
                    case Keys.Oemcomma: return getChar(",?,? < ");
                    case Keys.OemPeriod: return getChar(".:.: > ");
                    case Keys.OemMinus: return getChar("-_-_ * ");

                    // Mezera:
                    case Keys.Space: return getChar("       ");


                    // Střední blok:
                    case Keys.PrintScreen: return null;
                    case Keys.Scroll: return null;
                    case Keys.Pause: return null;

                    case Keys.Insert: return null;
                    case Keys.Home: return null;
                    case Keys.PageUp: return null;

                    case Keys.Delete: return null;
                    case Keys.End: return null;
                    case Keys.PageDown: return null;

                    case Keys.Up: return null;
                    case Keys.Left: return null;
                    case Keys.Down: return null;
                    case Keys.Right: return null;


                    // Numerická:
                    case Keys.NumLock: return null;
                    case Keys.Divide: return getChar("////   ");
                    case Keys.Multiply: return getChar("****   ");
                    case Keys.Subtract: return getChar("----   ");

                    case Keys.NumPad7: return getChar("7\07\0 • ");
                    case Keys.NumPad8: return getChar("7\07\0 ◘ ");
                    case Keys.NumPad9: return getChar("7\07\0 ○ ");
                    case Keys.Add: return getChar("++++  ");

                    case Keys.NumPad4: return getChar("4\04\0 ♦ ");
                    case Keys.NumPad5: return getChar("5\05\0 ♣ ");
                    case Keys.NumPad6: return getChar("6\06\0 ♠ ");

                    case Keys.NumPad1: return getChar("1\01\0 ♦ ");
                    case Keys.NumPad2: return getChar("2\02\0 ☻ ");
                    case Keys.NumPad3: return getChar("3\03\0 ♥ ");

                    case Keys.NumPad0: return getChar("0\00\0   ");
                    case Keys.Decimal: return getChar(",\0,\0   ");
                }
            }
            else
            {   // EN klávesnice:
                switch (keyArgs.KeyCode)                // KeyCode obsahuje klávesu, pouze klávesu očištěnou o modifikátory
                {
                    // Esc a Fn:
                    case Keys.Escape: return null;
                    case Keys.F1: return null;
                    case Keys.F2: return null;
                    case Keys.F3: return null;
                    case Keys.F4: return null;
                    case Keys.F5: return null;
                    case Keys.F6: return null;
                    case Keys.F7: return null;
                    case Keys.F8: return null;
                    case Keys.F9: return null;
                    case Keys.F10: return null;
                    case Keys.F11: return null;
                    case Keys.F12: return null;


                    // Horní řada
                    case Keys.Oemtilde: return getChar(";°;°  ");
                    case Keys.D1: return getChar("+1+1 ~ ");
                    case Keys.D2: return getChar("ě2Ěě ˇ ");
                    case Keys.D3: return getChar("š3Š3 ^ ");
                    case Keys.D4: return getChar("č4Č4 ˘ ");
                    case Keys.D5: return getChar("ř5Ř5 ° ");
                    case Keys.D6: return getChar("ž6Ž6 ˛ ");
                    case Keys.D7: return getChar("ý7Ý7 ` ");
                    case Keys.D8: return getChar("á8Á8 · ");
                    case Keys.D9: return getChar("í9Í9 ´ ");
                    case Keys.D0: return getChar("é0É0 ˝ ");
                    case Keys.Oemplus: return getChar("=%=% ¨ ");
                    case Keys.OemQuestion: return getChar("´ˇ´ˇ ¸ ");
                    case Keys.Back: return null;

                    // QWER
                    case Keys.Tab: return null;
                    case Keys.Q: return getChar("qQQq \\ ");
                    case Keys.W: return getChar("wWwW | ");
                    case Keys.E: return getChar("eEEe € ");
                    case Keys.R: return getChar("rRRr   ");
                    case Keys.T: return getChar("tTTt   ");
                    case Keys.Z: return getChar("zZZz   ");
                    case Keys.U: return getChar("uUUu   ");
                    case Keys.I: return getChar("iIIi   ");
                    case Keys.O: return getChar("oOOo   ");
                    case Keys.P: return getChar("pPPp   ");
                    case Keys.OemOpenBrackets: return getChar("ú/Ú/ ÷ ");
                    case Keys.Oem6: return getChar(")()( × ");
                    case Keys.Oem5: return getChar("¨'¨' ¨ ");

                    // ASDF
                    case Keys.Capital: return null;
                    case Keys.A: return getChar("aAAa   ");
                    case Keys.S: return getChar("sSSs đ ");
                    case Keys.D: return getChar("dDDd Đ ");
                    case Keys.F: return getChar("fFFf [ ");
                    case Keys.G: return getChar("gGGg ] ");
                    case Keys.H: return getChar("hHHh   ");
                    case Keys.J: return getChar("jJJj   ");
                    case Keys.K: return getChar("kKKk ł ");
                    case Keys.L: return getChar("lLLl Ł ");
                    case Keys.Oem1: return getChar("ů\"Ů\" $ ");
                    case Keys.Oem7: return getChar("§!§! ß ");
                    case Keys.Return: return null;

                    // YXCV
                    case Keys.Y: return getChar("yYYy   ");
                    case Keys.X: return getChar("xXXx # ");
                    case Keys.C: return getChar("cCCc & ");
                    case Keys.V: return getChar("vVVv @ ");
                    case Keys.B: return getChar("bBBb { ");
                    case Keys.N: return getChar("nNNn } ");
                    case Keys.M: return getChar("mMMm   ");
                    case Keys.Oemcomma: return getChar(",?,? < ");
                    case Keys.OemPeriod: return getChar(".:.: > ");
                    case Keys.OemMinus: return getChar("-_-_ * ");

                    // Mezera:
                    case Keys.Space: return getChar("       ");


                    // Střední blok:
                    case Keys.PrintScreen: return null;
                    case Keys.Scroll: return null;
                    case Keys.Pause: return null;

                    case Keys.Insert: return null;
                    case Keys.Home: return null;
                    case Keys.PageUp: return null;

                    case Keys.Delete: return null;
                    case Keys.End: return null;
                    case Keys.PageDown: return null;

                    case Keys.Up: return null;
                    case Keys.Left: return null;
                    case Keys.Down: return null;
                    case Keys.Right: return null;


                    // Numerická:
                    case Keys.NumLock: return null;
                    case Keys.Divide: return getChar("////   ");
                    case Keys.Multiply: return getChar("****   ");
                    case Keys.Subtract: return getChar("----   ");

                    case Keys.NumPad7: return getChar("7\07\0 • ");
                    case Keys.NumPad8: return getChar("8\08\0 ◘ ");
                    case Keys.NumPad9: return getChar("9\09\0 ○ ");
                    case Keys.Add: return getChar("++++  ");

                    case Keys.NumPad4: return getChar("4\04\0 ♦ ");
                    case Keys.NumPad5: return getChar("5\05\0 ♣ ");
                    case Keys.NumPad6: return getChar("6\06\0 ♠ ");

                    case Keys.NumPad1: return getChar("1\01\0 ♦ ");
                    case Keys.NumPad2: return getChar("2\02\0 ☻ ");
                    case Keys.NumPad3: return getChar("3\03\0 ♥ ");

                    case Keys.NumPad0: return getChar("0\00\0   ");
                    case Keys.Decimal: return getChar(",\0,\0   ");
                }
            }

            return null;

            // Vrátí znak z daného řetězce, kde jsou znaky v pořadí:
            //  0: Bez CapsLock a bez Shiftu         CZ  klávesnice
            //  1: Bez CapsLock a včetně Shiftu      CZ  klávesnice
            //  2: Včetně CapsLock a bez Shiftu      CZ  klávesnice
            //  3: Včetně CapsLock a včetně Shiftu   CZ  klávesnice
            //  4:   mezera pro přehlednost
            //  5: AltGr  (nebo mezera)              CZ  klávesnice
            //  6:   mezera pro přehlednost
            //  7: Bez CapsLock a bez Shiftu         ENG klávesnice
            //  8: Bez CapsLock a včetně Shiftu      ENG klávesnice
            //  9: Včetně CapsLock a bez Shiftu      ENG klávesnice
            // 10: Včetně CapsLock a včetně Shiftu   ENG klávesnice
            // 11:   mezera pro přehlednost
            //  5: AltGr  (nebo mezera)              ENG klávesnice
            char? getChar(string text)
            {
                if (text.Length < index) return null;
                char c = text[index];
                if (isAltGr && c == ' ') return null;
                if (c == '\0') return null;
                return c;
            }
        }
        #endregion
        #region Temp Directory
        /// <summary>
        /// Aplikace zde najde fullname adresáře Temp, v podobě: "C:\User\Jméno\...\".
        /// Před prvním použitím se určí, otestuje Write a Delete, případně se použije jiný.
        /// Pokud je dostupný, sustí se v threadu na pozadí jeho úklid a ihned se vrátí jeho jméno.
        /// Úklid se provádí i na konci běhu klienta, pokud neproběhl zde.
        /// </summary>
        public static string TempDirectoryName { get { return Instance._TempDirectoryName; } }
        /// <summary>
        /// Aplikace zde najde fullname adresáře Temp, v podobě: "C:\User\Jméno\...\".
        /// </summary>
        private string _TempDirectoryName
        {
            get
            {
                string directoryName = __TempDirectoryName;          // Tato zajímavá konstrukce ušetří duplicitní kontrolu existence adresáře tehdy, ...
                if (directoryName is null) _TempDirectoryPrepare();
                else __TempDirectoryExists(directoryName);           //   ... když při prvním čtení je připravován (_TempDirectoryPrepare), protože přitom je adresář testován a je zajištěna jeho existence.
                return __TempDirectoryName;                          // Nemohu vracet directoryName, to může být prázdné, a přitom metoda _TempDirectoryPrepare() jméno vytvořila.
            }
        }
        /// <summary>
        /// Temp adresář
        /// </summary>
        private string __TempDirectoryName;
        /// <summary>
        /// Aplikace zde zadává suffix Temp adresáře.
        /// </summary>
        public static string TempDirectorySuffix { get { return Instance._TempDirectorySuffix; } set { Instance._TempDirectorySuffix = value; } }
        /// <summary>
        /// Aplikace zde zadává suffix Temp adresáře.
        /// </summary>
        private string _TempDirectorySuffix
        {
            get { return __TempDirectorySuffix; }
            set 
            {
                if (!String.Equals(value, __TempDirectorySuffix))
                {
                    __TempDirectorySuffix = value;
                    __TempDirectoryReset();
                }
            }
        }
        /// <summary>
        /// Temp adresář
        /// </summary>
        private string __TempDirectorySuffix;
        /// <summary>
        /// Zámek přípravy Temp adresáře
        /// </summary>
        private object __TempDirectoryLock = new object();
        /// <summary>
        /// Určí, otestuje a vrátí Temp adresář.
        /// </summary>
        /// <returns></returns>
        private void _TempDirectoryPrepare()
        {
            // Pokud by přišly dva požadavky na _TempDirectoryPrepare() ze dvou různých threadů při startu aplikace,
            //  pak nechci provádět hledání a předběžný úklid ze dvou threadů najednou => druhý počká, až první dokončí:
            lock (__TempDirectoryLock)
            {
                if (__TempDirectoryName is null)
                    _TempDirectorySearch();
            }
        }
        /// <summary>
        /// Fyzicky určí jméno Temp adresáře.
        /// </summary>
        private void _TempDirectorySearch()
        {
            string directoryName = null;

            // Temp adresář, typicky "C:\Users\david.janacek\AppData\Local" + "Asseco Solutions\Aplikace\Temp"
            string addSuffix = _TempDirectorySuffix;
            if (!String.IsNullOrEmpty(addSuffix)) searchForTempDirectoryOne(addSuffix);

            // Za stavu nouze zkusím bez suffixu?
            if (directoryName is null) searchForTempDirectoryOne(null);

            // Prvotní úklid adresáře bude asynchronní:
            if (!(directoryName is null)) _ = _TempDirectoryCleanAsync(directoryName);

            __TempDirectoryName = directoryName;

            // Vyhledá Temp adresář se zadaným suffixem:
            void searchForTempDirectoryOne(string suffix)
            {
                if (directoryName is null) directoryName = _TempDirectoryTest(System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), suffix);     // C:\Users\David\AppData\Local              C:\Users\david.janacek\AppData\Local                     // Cílový prostor
                if (directoryName is null) directoryName = _TempDirectoryTest(System.Environment.GetEnvironmentVariable("TEMP"), suffix);         // C:\Users\David\AppData\Local\Temp         C:\Users\DAVID~1.JAN\AppData\Local\Temp                      // Dosavadní Temp
                if (directoryName is null) directoryName = _TempDirectoryTest(System.Environment.GetEnvironmentVariable("TMP"), suffix);          // C:\Users\David\AppData\Local\Temp         C:\Users\DAVID~1.JAN\AppData\Local\Temp
                if (directoryName is null) directoryName = _TempDirectoryTest(System.IO.Path.GetTempPath(), suffix);                              // C:\Users\David\AppData\Local\Temp\        C:\Users\david.janacek\AppData\Local\Temp\
            }
        }
        /// <summary>
        /// Otestuje, zda daný adresář <paramref name="directoryName"/> není prázdný, volitelně k němu přidá dodaný suffix <paramref name="suffix"/> a otestuje jej a vrátí výsledné jméno.
        /// Může vrátit null, pak adresář nelze používat.
        /// Nemělo by dojít k chybě.
        /// Vrácený adresář nemá na konci lomítko.
        /// </summary>
        /// <param name="directoryName"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        private string _TempDirectoryTest(string directoryName, string suffix)
        {
            if (String.IsNullOrEmpty(directoryName)) return null;

            directoryName = directoryName.Trim();
            while (directoryName.Length > 1 && directoryName.EndsWith("\\")) directoryName = directoryName.Substring(0, directoryName.Length - 1);

            if (!String.IsNullOrEmpty(suffix))
                directoryName = System.IO.Path.Combine(directoryName, suffix);

            try
            {   // Vytvořím adresář, pokud neexistuje. Pokud dojde k chybě, odchytím ji a vrátím null => daný adresář nelze použít, nemáme na něj právo...
                if (!System.IO.Directory.Exists(directoryName))
                    System.IO.Directory.CreateDirectory(directoryName);

                // Do vytvořeného adresáře zkusím zapsat (a poté smazat) soubor, abych měl jistotu, že jej mohu používat:
                string name = "~t" + (new Random()).Next(100000000, 1000000000).ToString() + DateTime.Now.ToString("mmssfff") + ".tmp";
                string testFileName = System.IO.Path.Combine(directoryName, name);
                if (System.IO.File.Exists(testFileName))
                    System.IO.File.Delete(testFileName);
                System.IO.File.WriteAllText(testFileName, "Helios");

                // Pokud testovací soubor existuje, je to OK; pokud ale neexistuje, pak adresář nemohu používat:
                if (System.IO.File.Exists(testFileName))
                    System.IO.File.Delete(testFileName);
                else
                    directoryName = null;
            }
            catch
            {   // Chyba znamená, že adresář nebudeme používat:
                directoryName = null;
            }

            return directoryName;
        }
        /// <summary>
        /// Zajistí, že daný adresář (pokud není prázdný) bude existovat (pokud neexistuje, vytvoří jej)
        /// </summary>
        /// <param name="directoryName"></param>
        private void __TempDirectoryExists(string directoryName)
        {
            if (!String.IsNullOrEmpty(directoryName) && !System.IO.Directory.Exists(directoryName))
                System.IO.Directory.CreateDirectory(directoryName);
        }
        /// <summary>
        /// Resetuje aktuální Temp adresář, ale ne jeho Suffix.
        /// </summary>
        private void __TempDirectoryReset()
        {
            __TempDirectoryName = null;
        }
        /// <summary>
        /// Volá se při ukončení aplikace. Má za úkol uklidit v Temp adresáři, pokud úklid neproběhl před prvním použitím.
        /// </summary>
        /// <returns></returns>
        private void _TempDirectoryDone()
        {
            string directoryName = __TempDirectoryName;
            if (directoryName != null)
                _ = _TempDirectoryCleanAsync(directoryName);
        }
        /// <summary>
        /// Uklidí zastaralé soubory v daném Temp adresáři.
        /// </summary>
        /// <param name="tempDirectoryName"></param>
        private async Task _TempDirectoryCleanAsync(string tempDirectoryName)
        {
            if (String.IsNullOrEmpty(tempDirectoryName)) return;

            var task = Task.Factory.StartNew(() => _TempDirectoryCleanSync(tempDirectoryName));
            await task;
            return;
        }
        /// <summary>
        /// Úklid daného Temp adresáře na pozadí
        /// </summary>
        /// <param name="tempDirectoryName"></param>
        private void _TempDirectoryCleanSync(string tempDirectoryName)
        {
            if (String.IsNullOrEmpty(tempDirectoryName)) return;
            if (tempDirectoryName.IndexOf(@"\AppData\Local", StringComparison.InvariantCultureIgnoreCase) < 0) return;   // Bezpečností pojistka, abych nesmazal nějaký jiný adresář...

            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(tempDirectoryName);
            if (!dirInfo.Exists) return;
            double days = 31d;
            var dateDelete = DateTime.UtcNow.AddDays(-days);
            _TempDirectoryDirectoryCleanOne(dirInfo, dateDelete, false);
        }
        /// <summary>
        /// Smaže prvky (soubory a podadresáře) z daného adresáře, pokud je smazat lze.
        /// Pokud je požadováno <paramref name="include"/> a adresář je na konci prázdný, pak smaže i sebe.
        /// </summary>
        /// <param name="dirInfo"></param>
        /// <param name="dateDelete">Smazat soubory, jejicž datum (LastWriteTimeUtc) je menší než toto datum</param>
        /// <param name="include"></param>
        private static void _TempDirectoryDirectoryCleanOne(System.IO.DirectoryInfo dirInfo, DateTime dateDelete, bool include)
        {
            if (dirInfo is null || !dirInfo.Exists) return;

            var fsInfos = dirInfo.GetFileSystemInfos("*.*", System.IO.SearchOption.TopDirectoryOnly);
            foreach (var fsInfo in fsInfos)
            {
                if (fsInfo.Attributes.HasFlag(System.IO.FileAttributes.Directory))
                {   // Adresář => rekurzivně:
                    _TempDirectoryDirectoryCleanOne(fsInfo as System.IO.DirectoryInfo, dateDelete, true);
                }
                else
                {   // Soubor => podle data smazat:
                    if (fsInfo.LastWriteTimeUtc < dateDelete)
                        _TempDirectoryCleanDeleteOne(fsInfo);
                }
            }

            if (include)
            {   // Včetně adresáře, pokud je prázdný:
                dirInfo.Refresh();
                fsInfos = dirInfo.GetFileSystemInfos("*.*", System.IO.SearchOption.TopDirectoryOnly);
                if (fsInfos.Length == 0)
                    _TempDirectoryCleanDeleteOne(dirInfo);
            }
        }
        /// <summary>
        /// Smaže jeden prvek z adresáře
        /// </summary>
        /// <param name="fsInfo"></param>
        private static void _TempDirectoryCleanDeleteOne(System.IO.FileSystemInfo fsInfo)
        {
            try
            {
                if (fsInfo != null)
                {
                    if (fsInfo.Attributes.HasFlag(System.IO.FileAttributes.ReadOnly) || fsInfo.Attributes.HasFlag(System.IO.FileAttributes.Hidden))
                        fsInfo.Attributes = System.IO.FileAttributes.Normal;
                    fsInfo.Delete();
                }
            }
            catch (Exception)
            { }
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
        /// Které aktivity se mají logovat?
        /// </summary>
        public static LogActivityKind LogActivities { get { return Instance.__LogActivities; } set { Instance.__LogActivities = value; } }
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
        public static event EventHandler LogTextChanged { add { Instance.__LogTextChanged += value; } remove { Instance.__LogTextChanged -= value; } }
        /// <summary>
        /// Obsahuje přesný aktuální čas jako Int64. 
        /// Lze ho následně použít jako parametr 'long startTime' v metodě <see cref="LogAddLineTime(LogActivityKind, string, long?)"/> pro zápis uplynulého času.
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
        /// Vrátí true, pokud v uplynulých (<paramref name="miliseconds"/>) milisekundách byl testován daný event.
        /// Slouží k tomu, aby se do logu nevypisovaly stále stejné eventy, pokud nám u nich stačí jeden zápis za určitý počet milisekund.
        /// <para/>
        /// Typické použití je u často po sobě jdoucích eventech typu "Změnil se text do statusbaru" nebo "přišla další webová response".
        /// Typický kód:
        /// <code>
        /// if (!<see cref="DxComponent.LogIsRepeated"/>(50, "WebStatusBarChanged"))
        ///     <see cref="DxComponent.LogAddLine"/>(<see cref="LogActivityKind.DevExpressEvents"/>, "Událost WebStatusBarChanged");
        /// </code>
        /// A má význam: Pokud v posledních 50ms byl evidován test "WebStatusBarChanged", pak vrátí true a NEprovede se zápis do logu.
        /// </summary>
        /// <param name="miliseconds"></param>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static bool LogIsRepeated(int miliseconds, string eventName) { return Instance._LogIsRepeated(miliseconds, eventName); }
        /// <summary>
        /// Přidá titulek (mezera + daný text ohraničený znaky ===)
        /// </summary>
        /// <param name="kind">Zdroj, filtruje se podle <see cref="LogActivities"/></param>
        /// <param name="title"></param>
        /// <param name="liner"></param>
        public static void LogAddTitle(LogActivityKind kind, string title, char? liner = null) { Instance._LogAddTitle(kind, title, liner); }
        /// <summary>
        /// Přidá dodaný řádek do logu. Umožní do textu vložit uplynulý čas:
        /// na místo tokenu z property <see cref="DxComponent.LogTokenTimeSec"/> vloží počet uplynulých sekund ve formě "25,651 sec";
        /// na místo tokenu z property <see cref="DxComponent.LogTokenTimeMilisec"/> vloží počet uplynulých milisekund ve formě "25,651 milisec";
        /// na místo tokenu z property <see cref="DxComponent.LogTokenTimeMicrosec"/> vloží počet uplynulých mikrosekund ve formě "98 354,25 mikrosec";
        /// </summary>
        /// <param name="kind">Zdroj, filtruje se podle <see cref="LogActivities"/></param>
        /// <param name="line"></param>
        /// <param name="startTime"></param>
        public static void LogAddLineTime(LogActivityKind kind, string line, long? startTime) { Instance._LogAddLineTime(kind, line, startTime); }
        /// <summary>
        /// Do logu vepíše danou Message z daného controlu
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="control"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        public static void LogAddMessage(Message msg, Control control = null, string prefix = null, string suffix = null) { Instance._LogAddMessage(msg, control, prefix, suffix); }
        /// <summary>
        /// Přidá dodaný řádek do logu. 
        /// Nepřidává se nic víc.
        /// </summary>
        /// <param name="kind">Zdroj, filtruje se podle <see cref="LogActivities"/></param>
        /// <param name="line"></param>
        public static void LogAddLine(LogActivityKind kind, string line) { Instance._LogAddLine(kind, line, false); }
        /// <summary>
        /// Poslední řádek v logu
        /// </summary>
        public static string LogLastLine { get { return Instance._LogLastLine; } }
        /// <summary>
        /// Token, který se očekává v textu v metodě <see cref="LogAddLineTime(LogActivityKind, string, long?)"/>, za který se dosadí uplynulý čas v sekundách, včetně jednotky " sec"
        /// </summary>
        public static string LogTokenTimeSec { get { return _LogTokenTimeSec; } } private const string _LogTokenTimeSec = "{SEC}";
        /// <summary>
        /// Token, který se očekává v textu v metodě <see cref="LogAddLineTime(LogActivityKind, string, long?)"/>, za který se dosadí uplynulý čas v milisekundách, včetně jednotky " ms"
        /// </summary>
        public static string LogTokenTimeMilisec { get { return _LogTokenTimeMilisec; } } private const string _LogTokenTimeMilisec = "{MILISEC}";
        /// <summary>
        /// Token, který se očekává v textu v metodě <see cref="LogAddLineTime(LogActivityKind, string, long?)"/>, za který se dosadí uplynulý čas v mikrosekundách, včetně jednotky " μs"
        /// </summary>
        public static string LogTokenTimeMicrosec { get { return _LogTokenTimeMicrosec; } } private const string _LogTokenTimeMicrosec = "{MICROSEC}";
        /// <summary>
        /// Zaloguje výjimku
        /// </summary>
        /// <param name="exc"></param>
        public static void LogAddException(Exception exc) { Instance._LogAddLine(exc.Message, false); }
        /// <summary>
        /// Zobrazí menu s výběrem logovaných aktivit
        /// </summary>
        /// <param name="control"></param>
        /// <param name="location"></param>
        public static void AppLogShowSettingsMenu(Control control, Point location) { Instance._AppLogShowSettingsMenu(control, location); }
        /// <summary>
        /// Zobrazí menu s výběrem logovaných aktivit
        /// </summary>
        /// <param name="control"></param>
        /// <param name="location"></param>
        private void _AppLogShowSettingsMenu(Control control, Point location)
        {
            var currentKind = DxComponent.LogActivities;
            var menuItems = new List<IMenuItem>();
            var fields = typeof(LogActivityKind).GetFields();
            foreach (var field in fields)
            {
                if (Enum.TryParse(field.Name, out LogActivityKind kind))
                {
                    bool isActive = (kind == LogActivityKind.None ? currentKind == LogActivityKind.None :
                                    (kind == LogActivityKind.Default ? currentKind == LogActivityKind.Default :
                                    (kind == LogActivityKind.All ? currentKind == LogActivityKind.All :
                                    ((currentKind & kind) != 0))));
                    menuItems.Add(new DataMenuItem() { ItemId = field.Name, Text = field.Name, ItemType = MenuItemType.CheckBox, Checked = isActive, Tag = kind });
                }
            }

            var menu = DxComponent.CreateDXPopupMenu(menuItems, "Settings", DevExpress.Utils.Menu.MenuViewType.Menu, true);
            menu.ItemClick += _AppLogShowSettingsMenuClick;
            menu.ShowPopup(control, location);
        }
        /// <summary>
        /// Po výběru logovaných aktivit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _AppLogShowSettingsMenuClick(object sender, DevExpress.Utils.Menu.DXMenuItemEventArgs e)
        {
            if (e.Item != null && e.Item.Tag is DataMenuItem menuItem && menuItem.Tag is LogActivityKind kind)
            {
                LogActivityKind oldKind = DxComponent.LogActivities;
                LogActivityKind newKind = LogActivityKind.None;
                switch (kind)
                {
                    case LogActivityKind.None:
                    case LogActivityKind.Default:
                    case LogActivityKind.All:
                        newKind = kind;
                        break;
                    default:
                        if (e.Item is DevExpress.Utils.Menu.DXMenuCheckItem checkItem)
                        {
                            bool isChecked = checkItem.Checked;
                            if (isChecked)
                                newKind = oldKind | kind;
                            else
                            {
                                LogActivityKind maskKind = LogActivityKind.All ^ kind;
                                newKind = oldKind & maskKind;
                            }
                        }
                        break;
                }

                if (newKind != oldKind)
                {
                    DxComponent.LogActivities = newKind;
#if Compile_TestDevExpress
                    DxComponent.Settings.SetRawValue("Components", DxComponent.LogActivitiesKindCfgName, ((Int64)DxComponent.LogActivities).ToString());
#endif
                }
            }
        }
        /// <summary>
        /// Init systému Log
        /// </summary>
        private void _InitLog()
        {
            bool hasDebugger = System.Diagnostics.Debugger.IsAttached;
            string userName = (hasDebugger ? System.Environment.UserName?.ToLower() : "");
            bool isDeveloper = (userName == "david.janacek" || userName == "david");
            _LogActive = hasDebugger && isDeveloper;
            __LogRunningWatch = new System.Diagnostics.Stopwatch();
            __LogRunningWatch.Start();
            __LogFrequencyLong = System.Diagnostics.Stopwatch.Frequency;
        }
        /// <summary>
        /// Aktivita logu
        /// </summary>
        private bool _LogActive
        {
            get { return __LogActive; }
            set
            {
                if (value && (__LogWatch == null || __LogSB == null))
                {   // Inicializace:
                    __LogWatch = new System.Diagnostics.Stopwatch();
                    __LogFrequency = __LogFrequencyLong;
                    __LogTimeSpanForEmptyRow = System.Diagnostics.Stopwatch.Frequency / 10L;   // Pokud mezi dvěma zápisy do logu bude časová pauza 1/10 sekundy a víc, vložím EmptyRow
                    __LogSB = new StringBuilder();
                }
                if (value && !__LogActive)
                {   // Restart:
                    __LogWatch.Start();
                    __LogStartTicks = __LogWatch.ElapsedTicks;
                    __LogLastWriteTicks = __LogStartTicks;
                }
                if (!value && __LogActive)
                {   // Stop:
                    if (__LogWatch != null)
                        __LogWatch.Stop();
                    // if (_LogSB != null)
                    //    _LogSB.Clear();
                    // Instance nechávám existovat včetně obsahu. Clear obsahu lze řešit explicitně.
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
                string text = "";
                if (__LogSB != null)
                {
                    lock (__LogSB)
                        text = __LogSB?.ToString();
                }
                return text;
            }
        }
        /// <summary>
        /// Smaže dosavadní obsah logu
        /// </summary>
        private void _LogClear()
        {
            if (!_LogActive) return;

            if (__LogSB != null)
            {
                lock (__LogSB)
                {
                    __LogSB.Clear();
                    _LogLastLine = null;
                }
            }

            _RunLogTextChanged();
        }
        /// <summary>
        /// Obsahuje aktuální čas jako ElapsedTicks
        /// </summary>
        /// <returns></returns>
        private long _LogTimeCurrent { get { return (_LogActive ? __LogWatch.ElapsedTicks : 0L); } }
        /// <summary>
        /// Přidá titulek (mezera + daný text ohraničený znaky ===)
        /// </summary>
        /// <param name="kind">Zdroj, filtruje se podle <see cref="LogActivities"/></param>
        /// <param name="title"></param>
        /// <param name="liner"></param>
        private void _LogAddTitle(LogActivityKind kind, string title, char? liner)
        {
            if (!_LogActive) return;
            if (!_DoLog(kind)) return;

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

            long nowTime = __LogWatch.ElapsedTicks;
            decimal seconds = ((decimal)(nowTime - startTime)) / __LogFrequency;     // Počet sekund
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
        /// Vrátí true, pokud v uplynulých (<paramref name="miliseconds"/>) milisekundách byl testován daný event.
        /// Slouží k tomu, aby se do logu nevypisovaly stále stejné eventy, pokud nám u nich stačí jeden zápis za určitý počet milisekund.
        /// <para/>
        /// Typické použití je u často po sobě jdoucích eventech typu "Změnil se text do statusbaru" nebo "přišla další webová response".
        /// Typický kód:
        /// <code>
        /// if (!<see cref="DxComponent.LogIsRepeated"/>(50, "WebStatusBarChanged"))
        ///     <see cref="DxComponent.LogAddLine"/>(<see cref="LogActivityKind.DevExpressEvents"/>, "Událost WebStatusBarChanged");
        /// </code>
        /// A má význam: Pokud v posledních 50ms byl evidován test "WebStatusBarChanged", pak vrátí true a NEprovede se zápis do logu.
        /// </summary>
        /// <param name="miliseconds"></param>
        /// <param name="eventName"></param>
        /// <returns></returns>
        private bool _LogIsRepeated(int miliseconds, string eventName)
        {
            if (String.IsNullOrEmpty(eventName)) return false;                 // Bez jména eventu vrátím false = není to opakování.

            // Volání je opakované tehdy, když máme minulé jméno eventu a nynější event je tentýž, a máme minulý čas dotazu a aktuálně uplynulý čas není větší:
            bool isRepeated = (!String.IsNullOrEmpty(__LogLastRepeatedEventName)
                             && String.Equals(eventName, __LogLastRepeatedEventName)
                             && __LogLastRepeatedTime > 0L
                             && _LogGetMiliseconds(__LogLastRepeatedTime, _LogRunningWatchCurrentTicks) <= miliseconds);

            // Uložíme si aktuální čas jako čas LastRepated, a zapíšu si eventname i kdyby byl shodný...
            __LogLastRepeatedTime = _LogRunningWatchCurrentTicks;
            __LogLastRepeatedEventName = eventName;
            return isRepeated;
        }
        /// <summary>
        /// Přidá dodaný řádek do logu. Umožní do textu vložit uplynulý čas:
        /// na místo tokenu {S} vloží počet uplynulých sekund ve formě "25,651 sec";
        /// na místo tokenu {MS} vloží počet uplynulých milisekund ve formě "25,651 milisec";
        /// na místo tokenu {US} vloží počet uplynulých mikrosekund ve formě "98 354,25 microsec";
        /// </summary>
        /// <param name="kind">Zdroj, filtruje se podle <see cref="LogActivities"/></param>
        /// <param name="line"></param>
        /// <param name="startTime"></param>
        private void _LogAddLineTime(LogActivityKind kind, string line, long? startTime)
        {
            if (!_LogActive) return;
            if (!_DoLog(kind)) return;

            long nowTime = __LogWatch.ElapsedTicks;
            decimal seconds = (startTime.HasValue ? ((decimal)(nowTime - startTime.Value)) / __LogFrequency : 0m);     // Počet sekund
            if (line.Contains(LogTokenTimeSec))
            {
                string info = Math.Round(seconds, 3).ToString("### ### ### ##0.000").Trim() + " sec";
                line = line.Replace(LogTokenTimeSec, info);
            }
            if (line.Contains(LogTokenTimeMilisec))
            {
                string info = Math.Round((seconds * 1000m), 3).ToString("### ### ### ##0.000").Trim() + " ms";
                line = line.Replace(LogTokenTimeMilisec, info);
            }
            if (line.Contains(LogTokenTimeMicrosec))
            {
                string info = Math.Round((seconds * 1000000m), 0).ToString("### ### ### ##0").Trim() + " μs";
                line = line.Replace(LogTokenTimeMicrosec, info);
            }
            _LogAddLine(line, false, startTime, nowTime);
        }
        /// <summary>
        /// Do logu vepíše danou Message z daného controlu
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="control"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        private void _LogAddMessage(Message msg, Control control, string prefix, string suffix)
        {
            if (!_LogActive) return;
            if (!_DoLog(LogActivityKind.WinMessage)) return;

            long nowTime = __LogWatch.ElapsedTicks;

            var msgName = _GetWinMessage(msg, false);
            if (msgName != null)
            {
                var wParam = msg.WParam.ToInt64().ToString("X16");
                var lParam = msg.LParam.ToInt64().ToString("X16");
                string line = (prefix ?? "") + $"Message: {msgName}; WParam: {wParam}; LParam: {lParam}";
                if (control != null)
                    line += $"; Control: {control.GetType().Name};";       // Ne abys přidal Control.Text, to se zacyklíš (protože čtení property Text vyvolá zprávu GETTEXT)
                if (suffix != null)
                    line += suffix;

                _LogAddLine(line, false, null, nowTime);
            }
        }
        /// <summary>
        /// Přidá daný text jako další řádek
        /// </summary>
        /// <param name="kind">Zdroj, filtruje se podle <see cref="LogActivities"/></param>
        /// <param name="line"></param>
        /// <param name="forceEmptyRow"></param>
        /// <param name="startTime"></param>
        /// <param name="nowTime"></param>
        private void _LogAddLine(LogActivityKind kind, string line, bool forceEmptyRow, long? startTime = null, long? nowTime = null)
        {
            if (!_LogActive) return;
            if (!_DoLog(kind)) return;

            _LogAddLine(line, forceEmptyRow, startTime, nowTime);
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
            long nowTick = nowTime ?? __LogWatch.ElapsedTicks;
            string totalUs = _LogGetMicroseconds(__LogStartTicks, nowTick).ToString();              // mikrosekund od startu
            string stepUs = _LogGetMicroseconds(__LogLastWriteTicks, nowTick).ToString();           // mikrosekund od posledního logu
            string timeUs = (startTime.HasValue ? _LogGetMicroseconds(startTime.Value, nowTick).ToString() : "");    // mikrosekund od daného času
            string thread = System.Threading.Thread.CurrentThread.Name;
            string tab = "\t";
            string logLine = totalUs + tab + stepUs + tab + timeUs + tab + thread + tab + line;
            lock (__LogSB)
            {
                if (forceEmptyRow || ((nowTick - __LogLastWriteTicks) > __LogTimeSpanForEmptyRow))
                    __LogSB.AppendLine();
                __LogSB.AppendLine(logLine);
                _LogLastLine = logLine;
            }
            __LogLastWriteTicks = __LogWatch.ElapsedTicks;
            _RunLogTextChanged();
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
            return (1000000L * (stop - start)) / __LogFrequencyLong;
        }
        private long _LogGetMiliseconds(long start, long stop)
        {
            return (1000L * (stop - start)) / __LogFrequencyLong;
        }
        /// <summary>
        /// Vyvolá event <see cref="__LogTextChanged"/>.
        /// </summary>
        private void _RunLogTextChanged()
        {
            // Tady nemá smysl řešit standardní metodu : protected virtual void OnLogTextChanged(), protože tahle třída je sealed a singleton
            __LogTextChanged?.Invoke(null, EventArgs.Empty);
        }
        /// <summary>
        /// Vrátí true, pokud se log daného druhu má zapisovat
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        private bool _DoLog(LogActivityKind kind)
        {
            return ((kind & __LogActivities) != 0);
        }
        private bool __LogActive;
        private LogActivityKind __LogActivities;
        private System.Diagnostics.Stopwatch __LogWatch;
        private long _LogWatchCurrentTicks { get { return __LogWatch.ElapsedTicks; } }
        private decimal __LogFrequency;
        private long __LogFrequencyLong;
        private StringBuilder __LogSB;
        private event EventHandler __LogTextChanged;
        private long __LogStartTicks;
        private long __LogLastWriteTicks;
        private long __LogTimeSpanForEmptyRow;
        /// <summary>
        /// Stopky stáleběžící, běží i když log není aktivní
        /// </summary>
        private System.Diagnostics.Stopwatch __LogRunningWatch;
        /// <summary>
        /// Aktuální čas stopek stáleběžících <see cref="__LogRunningWatch"/>
        /// </summary>
        private long _LogRunningWatchCurrentTicks { get { return __LogRunningWatch.ElapsedTicks; } }
        /// <summary>
        /// Poslední čas dotazu <see cref="_LogIsRepeated(int, string)"/>
        /// </summary>
        private long __LogLastRepeatedTime;
        /// <summary>
        /// Poslední eventName dotazu <see cref="_LogIsRepeated(int, string)"/>
        /// </summary>
        private string __LogLastRepeatedEventName;
        /// <summary>
        /// Jméno položky v konfiguraci pro <see cref="DxComponent.LogActive"/>
        /// </summary>
        internal const string LogActiveCfgName = "AppLogActive";
        /// <summary>
        /// Jméno položky v konfiguraci pro <see cref="DxComponent.LogActivities"/>
        /// </summary>
        internal const string LogActivitiesKindCfgName = "AppLogActivitiesKind";
        #endregion
        #region NotCaptureWindows - znemožní zachycení obsahu oken v Capture aplikacích
        /// <summary>
        /// <see cref="ExcludeFromCaptureContent"/> - znemožní zachycení obsahu oken v Capture aplikacích (sdílení obrazovky v Teams atd, nahrávání videa, PrintScreen).<br/>
        /// Hodnota false (default) = obsah okna bude normálně zobrazen při nahrávání / sdílení / zachycení. <br/>
        /// Hodnota true = obsah okna nebude zobrazen při nahrávání / sdílení / zachycení.
        /// </summary>
        public static bool ExcludeFromCaptureContent { get { return Instance._ExcludeFromCaptureContent; } set { Instance._ExcludeFromCaptureContent = value; } }
        /// <summary>
        /// Znemožní zachycení obsahu oken v Capture aplikacích.
        /// Změna hodnoty je odeslána jako událost do všech Listenerů
        /// </summary>
        private bool _ExcludeFromCaptureContent
        {
            get { return __ExcludeFromCaptureContent; }
            set
            {
                if (value != __ExcludeFromCaptureContent)
                {   // Po změně hodnoty tuto novou hodnotu uložím, a zavolám všechny stále živé posluchače události IListenerNotCaptureWindows,
                    //  ti si (většinou jde o Formuláře) aktualizují svoji odpovídající vlastnost:
                    __ExcludeFromCaptureContent = value;
                    this._CallListeners<IListenerExcludeFromCaptureContentChanged>();
                }
            }
        }
        /// <summary>
        /// Proměnná pro <see cref="_ExcludeFromCaptureContent"/>
        /// </summary>
        private bool __ExcludeFromCaptureContent;
        /// <summary>
        /// Pro dodaný control <paramref name="control"/> (typicky Form) nastaví jeho vlastnost <c>WindowDisplayAffinity</c> podle <paramref name="excludeFromCapture"/>:<br/>
        /// Pro true nastaví <c>WDA_EXCLUDEFROMCAPTURE</c> = The window is displayed only on a monitor. Everywhere else, the window does not appear at all. 
        /// One use for this affinity is for windows that show video recording controls, so that the controls are not included in the capture.<br/>
        /// Pro false nastaví <c>WDA_NONE</c> = Imposes no restrictions on where the window can be displayed.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="excludeFromCapture"></param>
        public static void SetWindowDisplayAffinity(Control control, bool excludeFromCapture)
        {
            if (control is null) return;
            if (!control.IsHandleCreated) return;

            try
            {
                if (excludeFromCapture)
                    // Vyloučit ze zachycení obsahu:
                    SetWindowDisplayAffinity(control.Handle, WDA_MONITOR);   // WDA_EXCLUDEFROMCAPTURE: Skryje obsah na lokálním monitoru před PrintScreenem, ale na RDP se obsah ukazuje
                else
                    // Běžné zobrazení:
                    SetWindowDisplayAffinity(control.Handle, WDA_NONE);
            }
            catch { }
        }
        /// <summary>
        /// Vrátí true, pokud dané okno <paramref name="control"/> má zakázané zobrazení obsahu pro ScreenRecordery.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static bool? GetWindowDisplayAffinity(Control control)
        {
            if (control is null) return null;
            if (!control.IsHandleCreated) return null;

            try
            {
                GetWindowDisplayAffinity(control.Handle, out uint dwAffinity);
                return (dwAffinity == WDA_NONE ? false : true);
            }
            catch { }
            return null;
        }
        /// <summary>
        /// Jméno položky v konfiguraci pro <see cref="DxComponent.ExcludeFromCaptureContent"/>
        /// </summary>
        internal const string ExcludeFromCaptureContentCfgName = "AppExcludeFromCaptureContent";
        [DllImport("user32.dll")]
        private static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
        [DllImport("user32.dll")]
        private static extern uint GetWindowDisplayAffinity(IntPtr hwnd, out uint dwAffinity);
        /// <summary>
        /// Chování okna při PrintScreen, VideoCapture, Teams sdílení...:<br/>
        /// Okno je vždy normálně viditelné, na lokálním PC i na RDP.
        /// </summary>
        private const uint WDA_NONE = 0x00000000;
        /// <summary>
        /// Chování okna při PrintScreen, VideoCapture, Teams sdílení...:<br/>
        /// Na lokálním PC: <br/>
        /// Na RDP: Okno má vidět rám, ale obsah je černý.
        /// </summary>
        private const uint WDA_MONITOR = 0x00000001;
        /// <summary>
        /// Chování okna při PrintScreen, VideoCapture, Teams sdílení...:<br/>
        /// Na lokálním PC: Okno není vidět, ani jeho rám. Pouze pod DevExpress okny je vystínovaný Border.<br/>
        /// Na RDP: Okno je viditelné i s čitelným obsahem.
        /// </summary>
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
        #endregion
        #region Draw metody a instance
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

                case GradientStyleType.UpLeft:
                    bounds = bounds.Enlarge(1, 1, 1, 1);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds.GetPoint(ContentAlignment.BottomRight).Value, bounds.GetPoint(ContentAlignment.TopLeft).Value, color1.Value, color2.Value);
                case GradientStyleType.UpRight:
                    bounds = bounds.Enlarge(1, 1, 1, 1);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds.GetPoint(ContentAlignment.BottomLeft).Value, bounds.GetPoint(ContentAlignment.TopRight).Value, color1.Value, color2.Value);
                case GradientStyleType.DownRight:
                    bounds = bounds.Enlarge(1, 1, 1, 1);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds.GetPoint(ContentAlignment.TopLeft).Value, bounds.GetPoint(ContentAlignment.BottomRight).Value, color1.Value, color2.Value);
                case GradientStyleType.DownLeft:
                    bounds = bounds.Enlarge(1, 1, 1, 1);             // Problém .NET a WinForm...
                    return new LinearGradientBrush(bounds.GetPoint(ContentAlignment.TopRight).Value, bounds.GetPoint(ContentAlignment.BottomLeft).Value, color1.Value, color2.Value);
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
        /// Metoda vykreslí do dané grafiky daný obrázek do cílového prostoru, v daném režimu a zarovnání.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="imageName"></param>
        /// <param name="bounds"></param>
        /// <param name="fillMode"></param>
        /// <param name="alignment"></param>
        public static void PaintImage(Graphics graphics, string imageName, Rectangle bounds, ImageFillMode fillMode, ContentAlignment alignment)
        {
            if (String.IsNullOrEmpty(imageName) || fillMode == ImageFillMode.None) return;
            var image = DxComponent.GetBitmapImage(imageName, ResourceImageSizeType.Original);
            PaintImage(graphics, image, bounds, fillMode, alignment);
        }
        /// <summary>
        /// Metoda vykreslí do dané grafiky daný obrázek do cílového prostoru, v daném režimu a zarovnání.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="image"></param>
        /// <param name="bounds"></param>
        /// <param name="fillMode"></param>
        /// <param name="alignment"></param>
        public static void PaintImage(Graphics graphics, Image image, Rectangle bounds, ImageFillMode fillMode, ContentAlignment alignment)
        {
            if (image is null || fillMode == ImageFillMode.None) return;
            var clip = graphics.Clip;
            try
            {
                var imageSize = image.Size;
                Rectangle imageBounds;
                graphics.SetClip(bounds, CombineMode.Intersect);
                switch (fillMode)
                {
                    case ImageFillMode.Clip:
                        imageBounds = imageSize.AlignTo(bounds, alignment, false);
                        graphics.DrawImageUnscaledAndClipped(image, imageBounds);
                        break;
                    case ImageFillMode.Shrink:
                        bool isBigger = (imageSize.Width > bounds.Width || imageSize.Height > bounds.Height);
                        imageBounds = (isBigger ? imageSize.FitTo(bounds, alignment) : imageSize.AlignTo(bounds, alignment));
                        graphics.DrawImage(image, imageBounds);
                        break;
                    case ImageFillMode.Resize:
                        imageBounds = imageSize.FitTo(bounds, alignment);
                        graphics.DrawImage(image, imageBounds);
                        break;
                    case ImageFillMode.Fill:
                        imageBounds = bounds;
                        graphics.DrawImage(image, imageBounds);
                        break;
                    case ImageFillMode.Tile:



                        break;
                }
            }
            catch (Exception) { }
            finally
            {
                graphics.Clip = clip;
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
        /// Okamžitě vrací SolidBrush dané barvy.
        /// Vrácený objekt Nesmí být Disposován!
        /// </summary>
        /// <param name="color"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        private SolidBrush _GetSolidBrush(Color color, int? alpha)
        {
            _SolidBrush.Color = (alpha.HasValue ? Color.FromArgb(alpha.Value, color) : color);
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
        /// Okamžitě vrací Pen dané barvy.
        /// Vrácený objekt Nesmí být Disposován!
        /// </summary>
        /// <param name="color"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        private Pen _GetPen(Color color, int? alpha)
        {
            _Pen.Color = (alpha.HasValue ? Color.FromArgb(alpha.Value, color) : color);
            _Pen.Width = 1f;
            _Pen.DashStyle = DashStyle.Solid;
            return _Pen;
        }
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
        #region DrawString a Graphics
        /// <summary>
        /// Vykreslí string
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="aligment"></param>
        /// <param name="alpha"></param>
        public static void DrawText(Graphics graphics, string text, Font font, Rectangle bounds, Color color, ContentAlignment? aligment = null, int? alpha = null)
        {
            Instance._DrawText(graphics, text, font, bounds, color, aligment, alpha);
        }
        /// <summary>
        /// Vykreslí string.
        /// Písmo je vhodné získat metodou <see cref="GetFont(Font, FontStyle?, int?)"/>.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="aligment"></param>
        /// <param name="alpha"></param>
        private void _DrawText(Graphics graphics, string text, Font font, Rectangle bounds, Color color, ContentAlignment? aligment = null, int? alpha = null)
        {
            if (String.IsNullOrEmpty(text)) return;

            var brush = _GetSolidBrush(color, alpha);
            var stringFormat = _GetStringFormatFor(aligment ?? ContentAlignment.MiddleLeft);
            GraphicsSetForText(graphics);
            graphics.DrawString(text, font, brush, bounds, stringFormat);
        }
        /// <summary>
        /// Nastaví vlastnosti dané Graphics pro kreslení textu
        /// </summary>
        /// <param name="graphics"></param>
        public static void GraphicsSetForText(Graphics graphics)
        {
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;             // UHD: OK
            graphics.SmoothingMode = SmoothingMode.AntiAlias;                                                // UHD: OK
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }
        /// <summary>
        /// Nastaví vlastnosti dané Graphics pro kreslení gridů = základní kolmé čáry, nikoli šikmé
        /// </summary>
        /// <param name="graphics"></param>
        public static void GraphicsSetForGrids(Graphics graphics)
        {
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;             // UHD: OK
            graphics.SmoothingMode = SmoothingMode.None;
            graphics.InterpolationMode = InterpolationMode.Default;
        }
        /// <summary>
        /// Nastaví vlastnosti dané Graphics pro kreslení hladkých zakřivených čar
        /// </summary>
        /// <param name="graphics"></param>
        public static void GraphicsSetForSmooth(Graphics graphics)
        {
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;             // UHD: OK
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }
        #endregion
        #region FontCache
        /// <summary>
        /// Vrátí požadovaný font z cache. 
        /// Nedávejme na něm Dispose(), tedy nepoužívejme jej v using() patternu!!!
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="style"></param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        public static Font GetFont(Font prototype, FontStyle? style = null, int? targetDpi = null) { return Instance._GetFont(prototype, null, null, prototype.Size, style, targetDpi); }
        /// <summary>
        /// Vrátí požadovaný font z cache. 
        /// Nedávejme na něm Dispose(), tedy nepoužívejme jej v using() patternu!!!
        /// </summary>
        /// <param name="family"></param>
        /// <param name="emSize"></param>
        /// <param name="style"></param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        public static Font GetFont(FontFamily family, float emSize, FontStyle? style = null, int? targetDpi = null) { return Instance._GetFont(null, family, null, emSize, style, targetDpi); }
        /// <summary>
        /// Vrátí požadovaný font z cache. 
        /// Nedávejme na něm Dispose(), tedy nepoužívejme jej v using() patternu!!!
        /// </summary>
        /// <param name="familyName"></param>
        /// <param name="emSize"></param>
        /// <param name="style"></param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        public static Font GetFont(string familyName, float emSize, FontStyle? style = null, int? targetDpi = null) { return Instance._GetFont(null, null, familyName, emSize, style, targetDpi); }
        /// <summary>
        /// Vrátí defaultní font, s případnými odchylkami od standardu. Neprovádět Dispose!!!
        /// </summary>
        /// <param name="fontType"></param>
        /// <param name="sizeRatio"></param>
        /// <param name="fontStyle"></param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        public static Font GetFontDefault(SystemFontType? fontType = null, float? sizeRatio = null, FontStyle? fontStyle = null, int? targetDpi = null) { return Instance._GetFontDefault(fontType, sizeRatio, fontStyle, targetDpi); }
        /// <summary>
        /// Vrátí defaultní font, s případnými odchylkami od standardu. Neprovádět Dispose!!!
        /// </summary>
        /// <param name="fontType"></param>
        /// <param name="sizeRatio"></param>
        /// <param name="fontStyle"></param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        private Font _GetFontDefault(SystemFontType? fontType = null, float? sizeRatio = null, FontStyle? fontStyle = null, int? targetDpi = null)
        {
            var result = GetSystemFont(fontType ?? SystemFontType.DefaultFont);
            if (sizeRatio.HasValue || fontStyle.HasValue || targetDpi.HasValue)
            {
                float emSize = (sizeRatio.HasValue ? result.Size * sizeRatio.Value : result.Size);
                result = _GetFont(result, null, null, emSize, fontStyle, targetDpi);
            }
            return result;
        }
        /// <summary>
        /// Vrátí požadovaný font z cache. 
        /// Nedávejme na něm Dispose(), tedy nepoužívejme jej v using() patternu!!!
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="family"></param>
        /// <param name="familyName"></param>
        /// <param name="emSize"></param>
        /// <param name="fontStyle"></param>
        /// <param name="targetDpi">Cílové DPI. Typicky se má použít hodnota <see cref="DxPanelControl.CurrentDpi"/> (nebo <see cref="DxStdForm.CurrentDpi"/>)</param>
        /// <returns></returns>
        private Font _GetFont(Font prototype, FontFamily family, string familyName, float emSize, FontStyle? fontStyle = null, int? targetDpi = null)
        {
            string name = (prototype != null ? prototype.Name : (family != null ? family.Name : familyName));
            if (targetDpi.HasValue) emSize = ZoomToGui(emSize, targetDpi.Value);
            float size = (float)Math.Round((decimal)emSize, 2);
            FontStyle style = fontStyle ?? FontStyle.Regular;
            string styleKey = (style.HasFlag(FontStyle.Bold) ? "B" : "") + (style.HasFlag(FontStyle.Italic) ? "I" : "") + (style.HasFlag(FontStyle.Underline) ? "U" : "") + (style.HasFlag(FontStyle.Strikeout) ? "S" : "");
            string key = $"'{name}':{size:###0.00}:{styleKey}";
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
        /// <summary>
        /// Vrátí systémový font, např. <see cref="SystemFonts.DefaultFont"/> pro daný typ <paramref name="fontType"/>
        /// </summary>
        /// <param name="fontType"></param>
        /// <returns></returns>
        public static Font GetSystemFont(SystemFontType fontType)
        {
            switch (fontType)
            {
                case SystemFontType.DefaultFont: return SystemFonts.DefaultFont;
                case SystemFontType.DialogFont: return SystemFonts.DialogFont;
                case SystemFontType.MessageBoxFont: return SystemFonts.MessageBoxFont;
                case SystemFontType.CaptionFont: return SystemFonts.CaptionFont;
                case SystemFontType.SmallCaptionFont: return SystemFonts.SmallCaptionFont;
                case SystemFontType.MenuFont: return SystemFonts.MenuFont;
                case SystemFontType.StatusFont: return SystemFonts.StatusFont;
                case SystemFontType.IconTitleFont: return SystemFonts.IconTitleFont;
            }
            return SystemFonts.DefaultFont;
        }
        /// <summary>
        /// Systémové typy fontů
        /// </summary>
        public enum SystemFontType
        {
            /// <summary>
            /// <see cref="SystemFonts.DefaultFont"/>
            /// </summary>
            DefaultFont,
            /// <summary>
            /// <see cref="SystemFonts.DialogFont"/>
            /// </summary>
            DialogFont,
            /// <summary>
            /// <see cref="SystemFonts.MessageBoxFont"/>
            /// </summary>
            MessageBoxFont,
            /// <summary>
            /// <see cref="SystemFonts.CaptionFont"/>
            /// </summary>
            CaptionFont,
            /// <summary>
            /// <see cref="SystemFonts.SmallCaptionFont"/>
            /// </summary>
            SmallCaptionFont,
            /// <summary>
            /// <see cref="SystemFonts.MenuFont"/>
            /// </summary>
            MenuFont,
            /// <summary>
            /// <see cref="SystemFonts.StatusFont"/>
            /// </summary>
            StatusFont,
            /// <summary>
            /// <see cref="SystemFonts.IconTitleFont"/>
            /// </summary>
            IconTitleFont
        }
        #endregion
        #region StringFormaty
        /// <summary>
        /// Vrátí formátovací předpis pro daný alignment textu
        /// </summary>
        /// <param name="contentAlignment"></param>
        /// <returns></returns>
        public static StringFormat GetStringFormatFor(ContentAlignment? contentAlignment) { return Instance._GetStringFormatFor(contentAlignment); }
        /// <summary>
        /// Vrátí formátovací předpis pro daný alignment textu
        /// </summary>
        /// <param name="contentAlignment"></param>
        /// <returns></returns>
        private StringFormat _GetStringFormatFor(ContentAlignment? contentAlignment)
        {
            if (__StringFormats is null) __StringFormats = new Dictionary<ContentAlignment, StringFormat>();
            var alignment = contentAlignment ?? ContentAlignment.TopLeft;
            if (!__StringFormats.TryGetValue(alignment, out var stringFormat))
            {
                stringFormat = new StringFormat();
                stringFormat.FormatFlags = StringFormatFlags.LineLimit;
                switch (alignment)
                {
                    case ContentAlignment.TopLeft:
                        stringFormat.Alignment = StringAlignment.Near;
                        stringFormat.LineAlignment = StringAlignment.Near;
                        break;
                    case ContentAlignment.TopCenter:
                        stringFormat.Alignment = StringAlignment.Center;
                        stringFormat.LineAlignment = StringAlignment.Near;
                        break;
                    case ContentAlignment.TopRight:
                        stringFormat.Alignment = StringAlignment.Far;
                        stringFormat.LineAlignment = StringAlignment.Near;
                        break;
                    case ContentAlignment.MiddleLeft:
                        stringFormat.Alignment = StringAlignment.Near;
                        stringFormat.LineAlignment = StringAlignment.Center;
                        break;
                    case ContentAlignment.MiddleCenter:
                        stringFormat.Alignment = StringAlignment.Center;
                        stringFormat.LineAlignment = StringAlignment.Center;
                        break;
                    case ContentAlignment.MiddleRight:
                        stringFormat.Alignment = StringAlignment.Far;
                        stringFormat.LineAlignment = StringAlignment.Center;
                        break;
                    case ContentAlignment.BottomLeft:
                        stringFormat.Alignment = StringAlignment.Near;
                        stringFormat.LineAlignment = StringAlignment.Far;
                        break;
                    case ContentAlignment.BottomCenter:
                        stringFormat.Alignment = StringAlignment.Center;
                        stringFormat.LineAlignment = StringAlignment.Far;
                        break;
                    case ContentAlignment.BottomRight:
                        stringFormat.Alignment = StringAlignment.Far;
                        stringFormat.LineAlignment = StringAlignment.Far;
                        break;
                }
                __StringFormats.Add(alignment, stringFormat);
            }
            return stringFormat;
        }
        /// <summary>
        /// Formáty pro různé zarovnání textu
        /// </summary>
        private Dictionary<ContentAlignment, StringFormat> __StringFormats;
        /// <summary>
        /// <see cref="StringFormat"/> zarovnaný doleva nahoru, s povoleným dělením slov
        /// </summary>
        public static StringFormat StringFormatWrap { get { return Instance._StringFormatWrap; } }
        private StringFormat _StringFormatWrap
        {
            get
            {
                if (__StringFormatWrap is null)
                    __StringFormatWrap = new StringFormat(StringFormatFlags.NoClip) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisWord };
                return __StringFormatWrap;
            }
        }
        private StringFormat __StringFormatWrap;
        #endregion
        #region GraphicsPath
        public static System.Drawing.Drawing2D.GraphicsPath CreateGraphicsPathOval(Rectangle bounds, float innerMargin, float ovalRatio)
        {
            ovalRatio = (ovalRatio < 0f ? 0f : (ovalRatio > 1f ? 1f : ovalRatio));
            createDim(bounds.Left, bounds.Right, innerMargin, ovalRatio, out var x1, out var x2, out var x3, out var x4, out var x5, out var x6, out var x7, out var xv);
            createDim(bounds.Top, bounds.Bottom, innerMargin, ovalRatio, out var y1, out var y2, out var y3, out var y4, out var y5, out var y6, out var y7, out var yv);

            System.Drawing.Drawing2D.GraphicsPath path = new GraphicsPath();
            switch (xv)
            {
                case 0:
                    // Obdélník:
                    path.AddLine(new PointF(x3, y1), new PointF(x5, y1));                                              // Horní linie doprava
                    path.AddLine(new PointF(x7, y3), new PointF(x7, y5));                                              // Pravá linie dolů
                    path.AddLine(new PointF(x5, y7), new PointF(x3, y7));                                              // Dolní linie doleva
                    path.AddLine(new PointF(x1, y5), new PointF(x1, y3));                                              // Levá linie nahoru
                    path.CloseFigure();
                    break;
                case 1:
                    // Vejce:
                    path.AddBezier(new PointF(x5, y1), new PointF(x6, y1), new PointF(x7, y2), new PointF(x7, y3));    // Horní pravý oblouk
                    path.AddBezier(new PointF(x7, y5), new PointF(x7, y6), new PointF(x6, y7), new PointF(x5, y7));    // Pravý dolní oblouk
                    path.AddBezier(new PointF(x3, y7), new PointF(x2, y7), new PointF(x1, y6), new PointF(x1, y5));    // Dolní levý oblouk
                    path.AddBezier(new PointF(x1, y3), new PointF(x1, y2), new PointF(x2, y1), new PointF(x3, y1));    // Levý horní oblouk
                    path.CloseFigure();
                    break;
                case 2:
                    // Kulaté obdélníky:
                    path.AddLine(new PointF(x3, y1), new PointF(x5, y1));                                              // Horní linie doprava
                    path.AddBezier(new PointF(x5, y1), new PointF(x6, y1), new PointF(x7, y2), new PointF(x7, y3));    // Horní pravý oblouk
                    path.AddLine(new PointF(x7, y3), new PointF(x7, y5));                                              // Pravá linie dolů
                    path.AddBezier(new PointF(x7, y5), new PointF(x7, y6), new PointF(x6, y7), new PointF(x5, y7));    // Pravý dolní oblouk
                    path.AddLine(new PointF(x5, y7), new PointF(x3, y7));                                              // Dolní linie doleva
                    path.AddBezier(new PointF(x3, y7), new PointF(x2, y7), new PointF(x1, y6), new PointF(x1, y5));    // Dolní levý oblouk
                    path.AddLine(new PointF(x1, y5), new PointF(x1, y3));                                              // Levá linie nahoru
                    path.AddBezier(new PointF(x1, y3), new PointF(x1, y2), new PointF(x2, y1), new PointF(x3, y1));    // Levý horní oblouk
                    path.CloseFigure();
                    break;
            }

            return path;

            // Určí rozměry v jedné dimenzi
            void createDim(float b, float e, float margin, float ratio, out float d1, out float d2, out float d3, out float d4, out float d5, out float d6, out float d7, out int variation)
            {
                d1 = b + margin;                 // Počátek (Left / Top)
                d7 = e - margin;                 // Konec (Right / Bottom)
                float s = d7 - d1;               // Velikost od počátku do konce
                d4 = d1 + (s / 2f);              // Střed

                // ratio říká, jak moc to bude čistý ovál = bez rovných úseků na středu: 1 = vejce / 0 = obdélník / 0.25 = čtvrtina rozměru (v rohu) bude kulatá, a prostřední prostor bude přímka
                if (ratio <= 0f)
                {   // Obdélník:
                    d2 = d1;
                    d3 = d1;
                    d5 = d7;
                    d6 = d7;
                    variation = 0;
                }
                else if (ratio >= 1f)
                {   // Vejce:
                    float v = s / 4f;
                    d2 = d1 + v;
                    d3 = d4;
                    d5 = d4;
                    d6 = d7 - v;
                    variation = 1;
                }
                else
                {   // Rovná linka okolo středu, a kulaté rohy:
                    float l = (1f - ratio) * (s / 2f);  // Půl délka rovné linky, kolem středu d4
                    d3 = d4 - l;
                    d5 = d4 + l;
                    float v = (d3 - d1) / 2f;
                    d2 = d1 + v;
                    d6 = d7 - v;
                    variation = 2;
                }
            }
        }
        #endregion
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
        #region Vyhledání Typů v Assembly
        /// <summary>
        /// Načte a vrátí typy z dodané Assembly, vyhovující určitému filtru.
        /// Může vrátit null pokud ododaná assembly je null nebo ji nelze vůbec načíst.
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static Type[] GetTypes(System.Reflection.Assembly assembly, Func<Type, bool> filter)
        {
            if (assembly is null) return null;

            var types = _GetTypes(assembly);
            if (types != null && filter != null)
                types = types.Where(t => filter(t)).ToArray();
            return types;
        }
        /// <summary>
        /// Načte a vrátí typy z dodané Assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private static Type[] _GetTypes(System.Reflection.Assembly assembly)
        {
            Type[] types = null;
            try
            {
                types = assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException rtle)
            {
                types = GetTypesFromException(rtle, $"Assembly {assembly.FullName}", out string message);
                // ShowError(message);
            }
            catch (Exception)
            {
                // ShowError(exc);
            }
            return types;
        }
        /// <summary>
        /// Načte a vrátí typy z výjimky <see cref="System.Reflection.ReflectionTypeLoadException"/> a současně vytvoří zprávu o chybě
        /// </summary>
        /// <param name="rtle"></param>
        /// <param name="resourceName"></param>
        /// <param name="messageText"></param>
        /// <returns></returns>
        private static Type[] GetTypesFromException(System.Reflection.ReflectionTypeLoadException rtle, string resourceName, out string messageText)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ReflectionTypeLoadException: Nelze načíst jeden nebo více typů uložených v {resourceName}.");
            sb.AppendLine($"======================================================================================================.");

            var messages = rtle.LoaderExceptions.Select(le => GetMissingComponentText(le.Message)).GroupBy(m => m).ToList();
            int errorCount = messages.Count;
            messages.Sort((a, b) => String.Compare(a.Key, b.Key));

            sb.AppendLine($"Celkem: {errorCount} chyb:");
            int lineCount = 0;
            foreach (var message in messages)
            {
                lineCount++;
                int typeCount = message.Count();
                string itemsText = (typeCount == 1 ? "prvek" : typeCount < 5 ? "prvky" : "prvků");
                sb.AppendLine($"{lineCount}. {message.Key} (pro {typeCount} {itemsText});");
                if (lineCount > 60)
                {
                    sb.AppendLine($"... a dalších {errorCount - lineCount} problémů.");
                    break;
                }
            }
            messageText = sb.ToString();

            return rtle.Types;
        }
        /// <summary>
        /// Vrátí text o chybějící knihovně
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static string GetMissingComponentText(string message)
        {
            if (String.IsNullOrEmpty(message)) return "";
            int indexEnd = -1;
            if (indexEnd < 0) indexEnd = message.IndexOf(", Version", StringComparison.InvariantCultureIgnoreCase);
            if (indexEnd < 0) indexEnd = message.IndexOf(", Verze", StringComparison.InvariantCultureIgnoreCase);
            if (indexEnd > 5) message = message.Substring(0, indexEnd);
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
        /// <summary>
        /// Vrátí true, má být akceptováno HTML formátování v daném textu / false pokud ne.<br/>
        /// Pokud je předán parametr <paramref name="allowHtml"/> s dodanou hodnotou true nebo false, pak se vrací tato hodnota - bez zkoumání obsahu textu.
        /// Pokud <paramref name="allowHtml"/> je null : provede se autodetect Light-HTML kódů podle obsahu textu.
        /// Výstupem je true / false. 
        /// Konverzi do DevExpress hodnoty typu <see cref="DefaultBoolean"/> provádí metoda <see cref="DxComponent.ConvertBool(bool?)"/>.
        /// (Light-HTML kódy viz DevExpress: https://docs.devexpress.com/WindowsForms/4874/common-features/html-text-formatting).
        /// </summary>
        /// <param name="text"></param>
        /// <param name="allowHtml"></param>
        /// <returns></returns>
        public static bool AllowHtmlText(string text, bool? allowHtml = null)
        {
            if (allowHtml.HasValue) return allowHtml.Value;

            if (String.IsNullOrEmpty(text)) return false;

            string test = text.Trim().ToLower();
            if (test.Contains("<b>") && test.Contains("</b>")) return true;
            if (test.Contains("<i>") && test.Contains("</i>")) return true;
            if (test.Contains("<s>") && test.Contains("</s>")) return true;
            if (test.Contains("<u>") && test.Contains("</u>")) return true;

            if (test.Contains("<sub>") && test.Contains("</sub>")) return true;
            if (test.Contains("<sup>") && test.Contains("</sup>")) return true;

            if (test.Contains("<size") && test.Contains("</size>")) return true;
            if (test.Contains("<font") && test.Contains("</font>")) return true;
            if (test.Contains("<backcolor") && test.Contains("</backcolor>")) return true;
            if (test.Contains("<color") && test.Contains("</color>")) return true;

            if (test.Contains("<p") && test.Contains("</p>")) return true;
            if (test.Contains("<p>") || test.Contains("<br>")) return true;

            if (test.Contains("<href") && test.Contains(">")) return true;

            return false;
        }
        #endregion
        #region SleepUntil - pozastavit thread do splnění podmínky
        /// <summary>
        /// Tato metoda pozastaví aktuální thread do té doby, než bude splněna podmínka <paramref name="wakeUpCondition"/> pro probuzení.
        /// Pozastavený thread nebrání činnosti na pozadí, thread není úplně zablokován (používá se <see cref="System.Threading.Thread.Sleep(TimeSpan)"/>).
        /// <para/>
        /// Zadaná metoda s podmínkou <paramref name="wakeUpCondition"/> bude volaná v pravidelném intervalu <paramref name="wakeUpCheckIntervalMs"/> [milisekund], to je doba jednoho dílu spánku.
        /// Pokud metoda <paramref name="wakeUpCondition"/> vrátí true, pak bude spánek ukončen a tato metoda vrací řízení.<br/>
        /// Metoda nemůže být null, v takovém případě použijme <see cref="System.Threading.Thread.Sleep(TimeSpan)"/> s timeoutem.
        /// <para/>
        /// Tato metoda vrátí true, pokud byla ukončena protože jsme se dočkali správného stavu (metoda <paramref name="wakeUpCondition"/> vrátila true), anebo vrací false, když byla ukončena timeoutem <paramref name="maxSleepTime"/>.
        /// </summary>
        /// <param name="wakeUpCondition">Pravidelně volaná metoda, která testuje zda už je splněna podmínka pro probuzení. Metoda je volaná v intervalu <paramref name="wakeUpCheckIntervalMs"/> [milisekund]. Když vrátí true, spánek končí a metoda vrací řízení.</param>
        /// <param name="maxSleepTime">Timeout spánku, po kterém bude metoda ukončena i při nesplnění podmínky. Default = 15 sekund.</param>
        /// <param name="wakeUpCheckIntervalMs">Interval, v kterém bude opakovaně volána metoda <paramref name="wakeUpCondition"/>, v milisekundách. Default = 100 ms. Povolené rozmezí 25 - 2000.</param>
        internal static bool SleepUntil(Func<bool> wakeUpCondition, TimeSpan? maxSleepTime = null, int? wakeUpCheckIntervalMs = null)
        {
            if (wakeUpCondition is null) throw new ArgumentNullException($"DxComponent.SleepUntil() : wakeUpCondition is null.");

            TimeSpan timeOut = maxSleepTime ?? TimeSpan.FromSeconds(15d);
            if (timeOut.TotalSeconds < 1d) timeOut = TimeSpan.FromSeconds(1d);
            else if (timeOut.TotalMinutes > 60d) timeOut = TimeSpan.FromMinutes(60d);

            int interval = wakeUpCheckIntervalMs ?? 100;
            if (interval < 25) interval = 25;
            else if (interval > 2000) interval = 2000;

            var timeEnd = DateTime.Now.Add(timeOut);
            while (true)
            {
                Application.DoEvents();
                if (wakeUpCondition()) return true;                  // OK

                System.Threading.Thread.Sleep(100);                  // Počkám, tak abych neblokoval práci v current threadu.

                Application.DoEvents();
                if (wakeUpCondition()) return true;                  // OK

                if (DateTime.Now >= timeEnd) return false;           // TimeOut = false
            }
        }
        #endregion
        #region SystemSounds
        /// <summary>
        /// Přehraje daný systémový zvuk <paramref name="soundType"/>.
        /// Pokud v uplynulých 800 milisekundách byl jiný požadavek, pak aktuální se bude ignorovat.
        /// Parametrem <paramref name="force"/> lze interval zmenšit na 150 milisekund, ale nijak nelze vynutit interval kratší.
        /// <para/>
        /// Konkrétní zvuky jsou dané zvoleným tématem Windows. U mě osobně jsou zvuky Asterisk a Beep a Exclamation a Question stejné, odlišný je Hand.
        /// </summary>
        /// <param name="soundType"></param>
        /// <param name="force"></param>
        public static void SystemSoundPlay(SystemSoundType soundType, bool force = false) { Instance._SystemSoundPlay(soundType, force); }
        /// <summary>
        /// Přehraje daný systémový zvuk.
        /// </summary>
        /// <param name="soundType"></param>
        /// <param name="force"></param>
        private void _SystemSoundPlay(SystemSoundType soundType, bool force)
        {
            if (_IsTimeForNextPlay(force))
            {
                try
                {
                    switch (soundType)
                    {
                        case SystemSoundType.Asterisk:
                            System.Media.SystemSounds.Asterisk.Play();
                            break;
                        case SystemSoundType.Beep:
                            System.Media.SystemSounds.Beep.Play();
                            break;
                        case SystemSoundType.Exclamation:
                            System.Media.SystemSounds.Exclamation.Play();
                            break;
                        case SystemSoundType.Hand:
                            System.Media.SystemSounds.Hand.Play();
                            break;
                        case SystemSoundType.Question:
                            System.Media.SystemSounds.Question.Play();
                            break;
                    }
                }
                catch { }
            }
        }
        /// <summary>
        /// Přehraje daný systémový zvuk <paramref name="sound"/>.
        /// Zvuky a jejich názvy najdeme v property <see cref="DxComponent.SystemEventSounds"/>.
        /// Touto cestou lze přehrát všechny zvuky, které jsou k dispozici v Control panelu: "mmsys.cpl" (nebo "control mmsys.cpl") - záložka Zvuky.
        /// <para/>
        /// Pokud v uplynulých 800 milisekundách byl jiný požadavek, pak aktuální se bude ignorovat.
        /// Parametrem <paramref name="force"/> lze interval zmenšit na 150 milisekund, ale nijak nelze vynutit interval kratší.
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="force"></param>
        /// <param name="command"></param>
        public static void SystemSoundPlay(SystemEventSound sound, bool force = false, SystemEventSound.PlayCommands command = SystemEventSound.PlayCommands.Default) { Instance._SystemSoundPlay(sound, force, command); }
        /// <summary>
        /// Přehraje daný systémový zvuk.
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="force"></param>
        /// <param name="command"></param>
        private void _SystemSoundPlay(SystemEventSound sound, bool force, SystemEventSound.PlayCommands command = SystemEventSound.PlayCommands.Default)
        {
            if (sound != null && _IsTimeForNextPlay(force))
                sound.Play(command);
        }
        /// <summary>
        /// Přehraje daný systémový zvuk <paramref name="soundEventName"/>.
        /// Zvuky a jejich názvy najdeme v property <see cref="DxComponent.SystemEventSounds"/>.
        /// Touto cestou lze přehrát všechny zvuky, které jsou k dispozici v Control panelu: "mmsys.cpl" (nebo "control mmsys.cpl") - záložka Zvuky.
        /// <para/>
        /// Pokud v uplynulých 800 milisekundách byl jiný požadavek, pak aktuální se bude ignorovat.
        /// Parametrem <paramref name="force"/> lze interval zmenšit na 150 milisekund, ale nijak nelze vynutit interval kratší.
        /// </summary>
        /// <param name="soundEventName"></param>
        /// <param name="force"></param>
        /// <param name="command"></param>
        public static void SystemSoundPlay(string soundEventName, bool force = false, SystemEventSound.PlayCommands command = SystemEventSound.PlayCommands.Default) { Instance._SystemSoundPlay(soundEventName, force, command); }
        /// <summary>
        /// Přehraje daný systémový zvuk.
        /// </summary>
        /// <param name="soundEventName"></param>
        /// <param name="force"></param>
        /// <param name="command"></param>
        private void _SystemSoundPlay(string soundEventName, bool force, SystemEventSound.PlayCommands command = SystemEventSound.PlayCommands.Default)
        {
            if (!String.IsNullOrEmpty(soundEventName) && _IsTimeForNextPlay(force))
                SystemEventSound.PlaySystemEvent(soundEventName, command);
        }
        /// <summary>
        /// Přehraje daný soubor WAV <paramref name="fileNameWav"/>.
        /// <para/>
        /// Pokud v uplynulých 800 milisekundách byl jiný požadavek, pak aktuální se bude ignorovat.
        /// Parametrem <paramref name="force"/> lze interval zmenšit na 150 milisekund, ale nijak nelze vynutit interval kratší.
        /// </summary>
        /// <param name="fileNameWav"></param>
        /// <param name="force"></param>
        /// <param name="command"></param>
        public static void AudioSoundWavPlay(string fileNameWav, bool force = false, SystemEventSound.PlayCommands command = SystemEventSound.PlayCommands.Default) { Instance._AudioSoundWavPlay(fileNameWav, force, command); }
        /// <summary>
        /// Přehraje daný soubor WAV <paramref name="fileNameWav"/>.
        /// </summary>
        /// <param name="fileNameWav"></param>
        /// <param name="force"></param>
        /// <param name="command"></param>
        private void _AudioSoundWavPlay(string fileNameWav, bool force, SystemEventSound.PlayCommands command = SystemEventSound.PlayCommands.Default)
        {
            if (!String.IsNullOrEmpty(fileNameWav) && _IsTimeForNextPlay(force))
                SystemEventSound.PlayFileWav(fileNameWav, command);
        }
        /// <summary>
        /// Zvuky definované v systému Windows pro jednotlivé události.
        /// Obsahuje zvuky a události, které jsou k dispozici v Control panelu: "mmsys.cpl" (nebo "control mmsys.cpl") - záložka Zvuky.
        /// </summary>
        public static SystemEventSound[] SystemEventSounds { get { return Instance._SystemEventSounds; } }
        /// <summary>Zvuky definované v systému Windows pro jednotlivé události</summary>
        private SystemEventSound[] _SystemEventSounds { get { if (__SystemEventSounds is null) __SystemEventSounds = SystemEventSound.ReadEventSounds(); return __SystemEventSounds; } }
        private SystemEventSound[] __SystemEventSounds;
        /// <summary>
        /// Metoda zjistí, zda už je možno přehrát další zvuk s ohledem na čas předešlého zvuku.
        /// Pokud ano, tak si uloží aktuální čas pro hlídání dalšího požadavku.
        /// <para/>
        /// Vrátí true, pokud jsme dosud nehráli, anebo jsme už hráli, ale čas nyní mínus čas posledního hraní je větší než požadovaná pauza v sekundách.
        /// </summary>
        /// <param name="force"></param>
        /// <returns></returns>
        private bool _IsTimeForNextPlay(bool force)
        {
            bool result = false;
            var last = _LastSystemSoundTime;
            var now = DateTime.Now;
            double pause = force ? 0.15d : 0.80d;          // Pauza mezi dvěma melodiemi: vynucené hraní po 150ms, běžné hraní po 800ms
            if (!last.HasValue || (last.HasValue && (((TimeSpan)(now - last.Value)).TotalSeconds >= pause)))
            {
                result = true;
                _LastSystemSoundTime = now;
            }
            return result;
        }
        /// <summary>
        /// Čas posledního systémového zvuku, to abychom nezahltili zvukovody DDOS útokem
        /// </summary>
        private DateTime? _LastSystemSoundTime;
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
        /// <summary>
        /// Vrátí aktuálně platnou barvu dle skinu.
        /// Vstupní jména by měly pocházet z prvků třídy <see cref="SkinElementColor"/>.
        /// Například barva textu v labelu je pod jménem <see cref="SkinElementColor.CommonSkins_WindowText"/>
        /// </summary>
        /// <param name="name">Typ prvku</param>
        /// <returns></returns>
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
        /// Vrátí daný control z cache / vytvoří nový a umístí tam a vrátí.
        /// Control se používá pro určení jeho appearance a barevnosti.
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

            // c) A pokud je barva písmen světlá, pak je skin tmavý:
            bool djIsDarkSkin = (textColor.HasValue && textColor.Value.GetBrightness() >= 0.5f);

            // d) DevExpess nabízí přímou funkci v Helperu:
            bool dxIsDarkSkin = DevExpress.Utils.Frames.FrameHelper.IsDarkSkin(DevExpress.LookAndFeel.UserLookAndFeel.Default);

            if (djIsDarkSkin != dxIsDarkSkin)
            {
                // jen pro breakpoint...
            }

            bool isPanelDarkSkin = SkinColorSet.IsPanelDark;
            bool isInputDarkSkin = SkinColorSet.IsEditorDark;

            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"IsDarkSkin: By Ribbon.MenuText: {djIsDarkSkin}");
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"IsDarkSkin: By FrameHelper.IsDarkSkin: {dxIsDarkSkin}");
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"IsDarkSkin: By SkinColorSet.IsPanelDark: {isPanelDarkSkin}");
            DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"IsDarkSkin: By SkinColorSet.IsInputDark: {isInputDarkSkin}");

            return djIsDarkSkin;
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
        /// <summary>
        /// Vrátí definici daného stylu
        /// </summary>
        /// <param name="styleName"></param>
        /// <param name="exactAttributeColor"></param>
        /// <returns></returns>
        public static StyleInfo GetStyleInfo(string styleName, Color? exactAttributeColor = null) { return SystemAdapter.GetStyleInfo(styleName, exactAttributeColor); }
        #endregion
        #region SkinStaticInfo
        /// <summary>
        /// Připraví statickou kolekci informací o všech Skinech.
        /// Tato metoda nepotřebuje přístup k funkční instanci <see cref="DxComponent"/>.
        /// </summary>
        private void _InitSkinStaticInfo()
        {
            __SkinStaticInfos = SkinStaticInfo.CreateDictionary();
        }
        /// <summary>
        /// Vrátí statickou informaci o skinu daného jména.
        /// Jméno skinu <paramref name="skinNameCompact"/> generuje <see cref="DxSkinColorSet.CurrentSkinNameCompact"/> nebo <see cref="DxSkinColorSet.GetSkinNameCompact(string, bool)"/>.
        /// Nebo je k dispozici v <see cref="DxSkinColorSet.SkinNameCompact"/>.
        /// </summary>
        /// <param name="skinNameCompact"></param>
        /// <param name="skinStaticInfo"></param>
        /// <returns></returns>
        internal static bool TryFindStaticSkinInfo(string skinNameCompact, out SkinStaticInfo skinStaticInfo) { return Instance._TryFindStaticSkinInfo(skinNameCompact, out skinStaticInfo); }
        /// <summary>
        /// Statická informace o aktuálním skinu
        /// </summary>
        internal static SkinStaticInfo CurrentStaticSkinInfo { get { return Instance._GetCurrentStaticSkinInfo(); } }
        /// <summary>
        /// Vrátí statickou informaci o aktuálním skinu <see cref="DxSkinColorSet.CurrentSkinNameCompact"/>.
        /// </summary>
        /// <returns></returns>
        private SkinStaticInfo _GetCurrentStaticSkinInfo()
        {
            var skinNameCompact = DxSkinColorSet.CurrentSkinNameCompact;
            if (_TryFindStaticSkinInfo(skinNameCompact, out SkinStaticInfo skinInfo)) return skinInfo;
            return null;
        }
        /// <summary>
        /// Vrátí statickou informaci o skinu daného jména.
        /// Jméno skinu <paramref name="skinNameCompact"/> generuje <see cref="DxSkinColorSet.CurrentSkinNameCompact"/> nebo <see cref="DxSkinColorSet.GetSkinNameCompact(string, bool)"/>.
        /// Nebo je k dispozici v <see cref="DxSkinColorSet.SkinNameCompact"/>.
        /// </summary>
        /// <param name="skinNameCompact"></param>
        /// <param name="skinStaticInfo"></param>
        /// <returns></returns>
        private bool _TryFindStaticSkinInfo(string skinNameCompact, out SkinStaticInfo skinStaticInfo)
        {
            if (!String.IsNullOrEmpty(skinNameCompact) && __SkinStaticInfos.TryGetValue(skinNameCompact, out skinStaticInfo)) return true;      // OK: našli jsme

            SystemAdapter.TraceText(TraceLevel.Warning, this.GetType(), "_GetCurrentStaticSkinInfo", "Skin", $"Current skin {skinNameCompact} not found in Static Skin List.");

            skinStaticInfo = null;
            return false;
        }
        private Dictionary<string, SkinStaticInfo> __SkinStaticInfos;
        #endregion
        #region DxSkinColorSet
        /// <summary>
        /// Standardizovaný set barev a dalších hodnot aktuálního skinu a palety.
        /// Po změně skinu/palety je zde (OnDemand) připraven aktuální objekt.
        /// <para/>
        /// Vývojář může třídu <see cref="DxSkinColorSet"/> libovolně rozšiřovat a zajistit si svoje načtení hodnot ze skinu, třída je 'partial',
        /// a načítání dat může zajistit libovolná metoda třídy označená atributem <see cref="InitializerAttribute"/>.
        /// </summary>
        public static DxSkinColorSet SkinColorSet { get { return Instance._SkinColorSet; } }
        /// <summary>Standardizovaný set barev aktuálního skinu a palety</summary>
        private DxSkinColorSet _SkinColorSet { get { _CheckSkinColorSet(); return __SkinColorSet; } }
        /// <summary>
        /// Metoda načte data do <see cref="__SkinColorSet"/>:
        /// buď bezpodmínečně (když <paramref name="force"/> je true);
        /// anebo (když <paramref name="force"/> je false) tak jen pokud je aktivní Debugger (pro ladění)...
        /// <para/>
        /// Typicky se volá při startu a po změně skinu / palety, a s parametrem <paramref name="force"/> = false.
        /// Tím je zajištěno načtení skinu jen pro vývojáře.
        /// Běžný uživatel si skin/paletu načte OnDemand, až si požádá o <see cref="SkinColorSet"/> (tam bude vyhodnocen stav a aktuálnost v metodě <see cref="_CheckSkinColorSet(bool)"/>).
        /// </summary>
        /// <param name="force"></param>
        private void _ReadSkinColorSet(bool force = false)
        {
            if (force || IsDebuggerActive)
                _CheckSkinColorSet(true);
        }
        /// <summary>
        /// Metoda zajistí, že v <see cref="__SkinColorSet"/> bude instance platná pro aktuální skin a paletu.
        /// </summary>
        /// <param name="force"></param>
        private void _CheckSkinColorSet(bool force = false)
        {
            var skinColorSet = __SkinColorSet;
            if (force || skinColorSet is null || !skinColorSet.IsCurrent)
                __SkinColorSet = DxSkinColorSet.CreateCurrent();
        }
        /// <summary>
        /// Úložiště poslední verifikované instance <see cref="DxSkinColorSet"/>
        /// </summary>
        private DxSkinColorSet __SkinColorSet;
        #endregion
        #region Static helpers
        /// <summary>
        /// Vytvoří a vrátí string obsahující new Guid
        /// </summary>
        /// <returns></returns>
        public static string CreateGuid() { return Guid.NewGuid().ToString(); }
        /// <summary>
        /// Vrátí <see cref="DefaultBoolean"/> z hodnoty nullable <see cref="Boolean"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Obsolete("Lepší bude metoda DxComponent.ConvertBool() se stejným významem", true)]
        public static DefaultBoolean Convert(bool? value)
        {
            return (value.HasValue ? (value.Value ? DefaultBoolean.True : DefaultBoolean.False) : DefaultBoolean.Default);
        }
        /// <summary>
        /// Vrátí nullable <see cref="Boolean"/> z hodnoty <see cref="DefaultBoolean"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Obsolete("Lepší bude metoda DxComponent.ConvertBool() se stejným významem", true)]
        public static bool? Convert(DefaultBoolean value)
        {
            return (value == DefaultBoolean.True ? (bool?)true :
                   (value == DefaultBoolean.False ? (bool?)false : (bool?)null));
        }

        /// <summary>
        /// Vrátí <see cref="DefaultBoolean"/> z hodnoty nullable <see cref="Boolean"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DefaultBoolean ConvertBool(bool? value)
        {
            return (value.HasValue ? (value.Value ? DefaultBoolean.True : DefaultBoolean.False) : DefaultBoolean.Default);
        }
        /// <summary>
        /// Vrátí nullable <see cref="Boolean"/> z hodnoty <see cref="DefaultBoolean"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool? ConvertBool(DefaultBoolean value)
        {
            return (value == DefaultBoolean.True ? (bool?)true :
                   (value == DefaultBoolean.False ? (bool?)false : (bool?)null));
        }

        /// <summary>
        /// Metoda rozdělí dodaný string na "řádky" obsahující "Klíč a Hodnotu".
        /// Jsou zadány dva oddělovače: řádkový a hodnotový.
        /// Neprobíhá plnohodnotné parsování, proto nejsou detekovány stringy v uvozovkách ani komentáře.
        /// Oddělovače se tedy smí vyskytovat pouze jako oddělovač, nikoli v místě hodnoty (uzavřené v uvozovkách...).
        /// </summary>
        /// <param name="content"></param>
        /// <param name="itemDelimiter">Oddělovač prvků (řádků)</param>
        /// <param name="keyValueDelimiter">Oddělovač klíče od hodnoty</param>
        /// <param name="trimKeys">Odstranit krajní mezery okolo klíče</param>
        /// <param name="trimValues">Odstranit krajní mezery okolo hodnoty</param>
        /// <returns></returns>
        public static KeyValuePair<string, string>[] SplitToKeyValues(string content, string itemDelimiter, string keyValueDelimiter, bool trimKeys = false, bool trimValues = false)
        {
            if (content is null) return null;
            if (String.IsNullOrEmpty(content)) return new KeyValuePair<string, string>[0];

            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            var items = content.Split(new string[] { itemDelimiter }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                var kvp = SplitToKeyValue(item, keyValueDelimiter, trimKeys, trimValues);
                if (kvp.HasValue)
                    result.Add(kvp.Value);
            }
            return result.ToArray();
        }
        /// <summary>
        /// Metoda rozdělí dodaný string na "Klíč" a "Hodnotu" v místě oddělovače <paramref name="keyValueDelimiter"/>.
        /// <para/>
        /// Příklady pro různé umístění oddělovače, v ukázce znak "="<br/>
        /// Text <c>"W"</c> rozdělí na klíč: <c>"W"</c> a hodnotu <c>null</c><br/>
        /// Text <c>"="</c> rozdělí na klíč: <c>""</c> a hodnotu <c>null</c><br/>
        /// Text <c>"=250"</c> rozdělí na klíč: <c>""</c> a hodnotu <c>"250"</c><br/>
        /// Text <c>"W=250"</c> rozdělí na klíč: <c>"W"</c> a hodnotu <c>"250"</c><br/>
        /// Text <c>"W="</c> rozdělí na klíč: <c>"W"</c> a hodnotu <c>""</c><br/>
        /// </summary>
        /// <param name="content"></param>
        /// <param name="keyValueDelimiter"></param>
        /// <param name="trimKey"></param>
        /// <param name="trimValue"></param>
        /// <returns></returns>
        public static KeyValuePair<string, string>? SplitToKeyValue(string content, string keyValueDelimiter, bool trimKey = false, bool trimValue = false)
        {
            if (content == null || String.IsNullOrEmpty(keyValueDelimiter)) return null;
            int length = content.Length;
            if (length > 0)
            {
                int index = content.IndexOf(keyValueDelimiter);
                int delLen = keyValueDelimiter.Length;

                string key, value;

                if (index < 0)
                {   // "Klíč"               : bez oddělovače
                    key = content;
                    value = null;
                }
                else if (index == 0 && length == delLen)
                {   // "="                  : pouze oddělovač
                    key = "";
                    value = null;
                }
                else if (index == 0 && length > delLen)
                {   // "=abcd"              : pouze hodnota
                    key = "";
                    value = content.Substring(delLen);
                }
                else if (index > 0 && index < (length - delLen))
                {   // "x=15.7967442"       : standardní Key=Value
                    key = content.Substring(0, index);
                    value = content.Substring(index + delLen);
                }
                else if (index > 0 && index == (length - delLen))
                {   // "Width="             : Klíč=prázdná hodnota (ale je tam náznak hodnoty)
                    key = content.Substring(0, index);
                    value = "";
                }
                else
                {   // "nevímco"
                    key = content;
                    value = null;
                }
                if (key != null && trimKey) key = key.Trim();
                if (value != null && trimValue) value = value.Trim();
                return new KeyValuePair<string, string>(key, value);
            }
            return null;
        }
        #endregion
        #region DxClipboard : obálka nad systémovým clipboardem plus support pro DataExchangeContainer
        /// <summary>
        /// Inicializac clipboardu
        /// </summary>
        private void _InitClipboard()
        {
            _ApplicationGuid = CreateGuid();
            _ClipboardApplicationId = _ApplicationGuid;
        }
        /// <summary>
        /// Guid aplikace = náhodný string, po dobu života aplikace konstantní, unikátní
        /// </summary>
        public static string ApplicationGuid { get { return Instance._ApplicationGuid; } }
        /// <summary>
        /// ID aplikace = odlišuje typicky dvě různé aplikace otevřené v jeden okamžik
        /// </summary>
        public static string ClipboardApplicationId { get { return Instance._ClipboardApplicationId; } set { Instance._ClipboardApplicationId = value; } }
        /// <summary>
        /// Vloží do Clipboardu daný text
        /// </summary>
        /// <param name="text">Uživatelská data. Pokud bude null, nebudou se vkládat. Jejich formát je <see cref="DataFormats.Text"/>.</param>
        public static void ClipboardInsert(string text) { Instance._ClipboardInsert(null, text, DataFormats.Text); }
        /// <summary>
        /// Vloží do Clipboardu daná aplikační data a další běžně zpracovatelný údaj, typicky jde o prostý text (může to být i RTF nebo HTML text, obrázek, atd)
        /// </summary>
        /// <param name="appDataId">Identifikátor zdroje konkrétních dat, dovoluje cílovému objektu řídit, zda tato data bude/nebude akceptovat</param>
        /// <param name="appData">Vlastní aplikační data</param>
        /// <param name="windowsData">Uživatelská data. Pokud bude null, nebudou se vkládat. Jejich formát určuje <paramref name="windowsFormat"/>, default je <see cref="DataFormats.Text"/>.</param>
        /// <param name="windowsFormat">Formát dat v <paramref name="windowsData"/></param>
        public static void ClipboardInsert(string appDataId, object appData, object windowsData, string windowsFormat = null) { Instance._ClipboardInsert(new DataExchangeContainer(appDataId, appData), windowsData, windowsFormat); }
        /// <summary>
        /// Vloží do Clipboardu daná aplikační data a další běžně zpracovatelný údaj, typicky jde o prostý text (může to být i RTF nebo HTML text, obrázek, atd)
        /// </summary>
        /// <param name="appDataContainer">Komunikační data, která mohou být načtena a strojově zpracována touto nebo i jinou aplikací</param>
        /// <param name="windowsData">Uživatelská data. Pokud bude null, nebudou se vkládat. Jejich formát určuje <paramref name="windowsFormat"/>, default je <see cref="DataFormats.Text"/>.</param>
        /// <param name="windowsFormat">Formát dat v <paramref name="windowsData"/></param>
        public static void ClipboardInsert(DataExchangeContainer appDataContainer, object windowsData, string windowsFormat = null) { Instance._ClipboardInsert(appDataContainer, windowsData, windowsFormat); }
        /// <summary>
        /// Zkusí z Clipboardu vytáhnout aplikační data ve standardním containeru <see cref="DataExchangeContainer"/>, vrací true = data jsou k dispozici
        /// </summary>
        /// <param name="appDataContainer"></param>
        /// <returns></returns>
        public static bool ClipboardTryGetApplicationData(out DataExchangeContainer appDataContainer) { return Instance._ClipboardTryGetApplicationData(out appDataContainer); }
        /// <summary>
        /// Vrátí true, pokud v dodaném balíčku s daty jsou data pocházející ze zdroje, který je podle daných parametrů akceptovatelný.
        /// </summary>
        /// <param name="appDataContainer">Container s daty</param>
        /// <param name="targetControlId">ID cílového controlu</param>
        /// <param name="crossType">Režim výměny dat: co akceptuje cílový control</param>
        /// <param name="enabledSources">Povolené zdroje pro cílový control</param>
        /// <returns></returns>
        public static bool CanAcceptExchangeData(DataExchangeContainer appDataContainer, string targetControlId, DataExchangeCrossType crossType, string enabledSources) { return Instance._CanAcceptExchangeData(appDataContainer, targetControlId, crossType, enabledSources); }
        /// <summary>
        /// Guid aplikace = náhodný string, po dobu života aplikace konstantní, unikátní
        /// </summary>
        private string _ApplicationGuid; 
        /// <summary>
        /// ID aktuální aplikace, přidává se do Clipboardu.
        /// Výchozí hodnota je unikátní Guid.
        /// </summary>
        private string _ClipboardApplicationId = null;
        /// <summary>
        /// Vloží data do Clipboardu
        /// </summary>
        /// <param name="appDataContainer">Komunikační data, která mohou být načtena a strojově zpracována touto nebo i jinou aplikací</param>
        /// <param name="windowsData">Uživatelská data. Pokud bude null, nebudou se vkládat. Jejich formát určuje <paramref name="windowsFormat"/>, default je <see cref="DataFormats.Text"/>.</param>
        /// <param name="windowsFormat">Formát dat v <paramref name="windowsData"/></param>
        private void _ClipboardInsert(DataExchangeContainer appDataContainer, object windowsData, string windowsFormat)
        {
            // Do Clipboardu vložím instanci WinForms:DataObject, která v sobě může najednou obsahovat více formátů dat
            //   (např. ve Wordu používá se pro vložení formátu RTF i TXT),
            //   my tam vložíme naše aplikační data (ClipboardContainer) + relativně čitelná data, která může poskytnout volající jako windowsData + windowsFormat:
            DataObject dataObject = new DataObject();
            int count = 0;
            string errorMsg = "";
            if (appDataContainer != null)
            {   // Strojová data - která může zpracovat tentýž nebo jiný klient Nephrite:
                try
                {
                    string containerXml = WSXmlSerializer.Persist.Serialize(appDataContainer, WSXmlSerializer.PersistArgs.Default);
                    dataObject.SetData(ClipboardAppDataId, containerXml);
                    count++;
                }
                catch (Exception exc)
                {
                    errorMsg += exc.Message + Environment.NewLine;
                }
            }
            if (windowsData != null)
            {   // Otevřeně čitelná data:
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
        /// <param name="appDataContainer">Výstup nalezených dat</param>
        /// <returns></returns>
        private bool _ClipboardTryGetApplicationData(out DataExchangeContainer appDataContainer)
        {
            appDataContainer = null;
            IDataObject dataObject = null;
            try { dataObject = System.Windows.Forms.Clipboard.GetDataObject(); }
            catch { }
            if (dataObject == null) return false;
            if (!dataObject.GetDataPresent(ClipboardAppDataId)) return false;
            string containerXml = dataObject.GetData(ClipboardAppDataId) as string;
            if (containerXml == null) return false;
            DataExchangeContainer container = WSXmlSerializer.Persist.Deserialize(containerXml) as DataExchangeContainer;
            if (container == null) return false;
            appDataContainer = container;
            return true;
        }
        /// <summary>
        /// Vrátí true, pokud v dodaném balíčku s daty jsou data pocházející ze zdroje, který je podle daných parametrů akceptovatelný.
        /// </summary>
        /// <param name="appDataContainer">Container s daty</param>
        /// <param name="targetControlId">ID cílového controlu</param>
        /// <param name="crossType">Režim výměny dat: co akceptuje cílový control</param>
        /// <param name="enabledSources">Povolené zdroje pro cílový control</param>
        /// <returns></returns>
        private bool _CanAcceptExchangeData(DataExchangeContainer appDataContainer, string targetControlId, DataExchangeCrossType crossType, string enabledSources)
        {
            if (appDataContainer is null || appDataContainer.Data is null || crossType == DataExchangeCrossType.None) return false;

            // Zdrojem dat je naše aktuální aplikace? A řešení předvoleb:
            bool isEqualApplication = String.Equals(appDataContainer.ApplicationGuid, DxComponent.ApplicationGuid, StringComparison.Ordinal);
            if (isEqualApplication && !crossType.HasFlag(DataExchangeCrossType.CurrentApplication)) return false;
            if (!isEqualApplication && !crossType.HasFlag(DataExchangeCrossType.AnyOtherApplications)) return false;

            // Zdrojem dat je aktuální control?
            bool isEqualControl = (isEqualApplication && String.Equals(appDataContainer.DataSourceId, targetControlId, StringComparison.Ordinal));
            if (isEqualControl) return crossType.HasFlag(DataExchangeCrossType.OwnControl);

            // Pokud je zdrojem dat jiný než aktuální control, a je specifikováno že lze akceptovat kterýkoli jiný:
            if (!isEqualControl && crossType.HasFlag(DataExchangeCrossType.AnyOtherControls)) return true;

            // Pokud máme kontrolovat jiné zdroje jmenovitě, provedeme to nyní:
            if (crossType.HasFlag(DataExchangeCrossType.OtherSelectedControls)) return _CanAcceptExchangeDataSources(appDataContainer.DataSourceId, enabledSources);

            return false;
        }
        /// <summary>
        /// Vrátí true, pokud zdroj dat <paramref name="dataSourceId"/> lze akceptovat pro zadané povolené zdroje <paramref name="enabledSources"/>.
        /// Povolené zdroje mohou obsahovat více položek, oddělených CrLf.
        /// Každý jednotlivý povolený zdroj v <paramref name="enabledSources"/> může obsahovat Wildcard ve stylu FileName (např. "Page10001*ListBox*"), 
        /// pak se vyhodnocuje jako WildCard Pattern.
        /// </summary>
        /// <param name="dataSourceId"></param>
        /// <param name="enabledSources"></param>
        /// <returns></returns>
        private bool _CanAcceptExchangeDataSources(string dataSourceId, string enabledSources)
        {
            if (String.IsNullOrEmpty(enabledSources)) return false;
            var enabledItems = enabledSources.Split('\r', '\n');
            foreach (var enabledItem in enabledItems)
            {
                if (String.IsNullOrEmpty(enabledItem)) continue;
                if (String.Equals(dataSourceId, enabledItem, StringComparison.Ordinal)) return true;
                if ((enabledItem.Contains("*") || enabledItem.Contains("?")) && RegexSupport.IsMatchWildcards(dataSourceId, enabledItem)) return true;
            }
            return false;
        }
        /// <summary>
        /// Typ dat ukládaných v Clipboardu pro aplikační data
        /// </summary>
        private const string ClipboardAppDataId = "DxApplicationData";
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
        /// <summary>
        /// Nastaví požadovaný režim UHD
        /// </summary>
        /// <param name="uhdEnable"></param>
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
        /// <summary>
        /// Aktuální hodnota UHD Paint
        /// </summary>
        private bool _UhdPaintEnabled;
        /// <summary>
        /// Jméno položky v konfiguraci pro <see cref="DxComponent.UhdPaintEnabled"/>
        /// </summary>
        internal const string UhdPaintEnabledCfgName = "UhdPaintEnabled";
        /// <summary>
        /// Nastartuje proces systému Windows na klientu
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param>
        public static void StartProcess(string fileName, string arguments = null)
        {
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(fileName, arguments);
            var process = System.Diagnostics.Process.Start(psi);
        }
        #region class WinProcessInfo : Informace o využití zdrojů operačního systému
        /// <summary>
        /// <see cref="WinProcessInfo"/> : Informace o využití zdrojů operačního systému
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
                    return GetInfoForProcess(process);
                }
            }
            /// <summary>
            /// Získá a vrátí informace o využití zdrojů operačního systému pro explicitně daný proces
            /// </summary>
            /// <param name="process"></param>
            /// <returns></returns>
            public static WinProcessInfo GetInfoForProcess(System.Diagnostics.Process process)
            {

                long privateMemory = 0L;
                long workingSet64 = 0L;
                int gDIHandleCount = 0;
                int userHandleCount = 0;
                try { privateMemory = process.PrivateMemorySize64; } catch { }
                try { workingSet64 = process.WorkingSet64; } catch { }
                try { gDIHandleCount = GetGuiResources(process.Handle, 0); } catch { }
                try { userHandleCount = GetGuiResources(process.Handle, 1); } catch { }
                return new WinProcessInfo(privateMemory, workingSet64, gDIHandleCount, userHandleCount);
            }
            /// <summary>
            /// Obsahuje ID aktuálního procesu.
            /// </summary>
            public static int CurrentProcessId
            {
                get
                {
                    using (var process = System.Diagnostics.Process.GetCurrentProcess())
                    {
                        return (process?.Id ?? 0);
                    }
                }
            }
            /// <summary>
            /// Vrátí true, pokud dané dvě instance obsahují shodná data (neporovnává se čas)
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            public static bool EqualsContent(WinProcessInfo a, WinProcessInfo b)
            {
                bool an = a is null;
                bool bn = a is null;
                if (an && bn) return true;
                if (an || bn) return false;
                return (a.PrivateMemory == b.PrivateMemory &&
                        a.WorkingSet == b.WorkingSet &&
                        a.GDIHandleCount == b.GDIHandleCount &&
                        a.UserHandleCount == b.PrivateMemory);
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
                this.Time = DateTime.Now;
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
            /// Čas získání hodnot
            /// </summary>
            public DateTime Time { get; private set; }
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
        #endregion
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
        { return Instance._GetWinMessage(m, forceAll); }
        /// <summary>
        /// Vrátí string odpovídající dané Win Message. Obsahuje název události a její hodnotu.
        /// Metoda může vrátit NULL pro zprávy, které se mají ignorovat (viz parametr <paramref name="forceAll"/>).
        /// </summary>
        /// <param name="m"></param>
        /// <param name="forceAll">true = Povinně zpracovat všechny zprávy. Default = false: negenerují se zprávy pro GETTEXTLENGTH(0x000E) a GETTEXT(0x000D).</param>
        /// <returns></returns>
        private string _GetWinMessage(Message m, bool forceAll = false)
        {
            int code = m.Msg;
            if (!forceAll && (code == DxWin32.WM.GETTEXTLENGTH || code == DxWin32.WM.GETTEXT)) return null;

            var wmDict = _WmDict;
            string name = (wmDict.TryGetValue(m.Msg, out string value) ? value : "???");
            string message = $"{name} (0x{(m.Msg.ToString("X4"))})";
            return message;
        }
        /// <summary>
        /// Dictionary známých zpráv WinMsg, autoinicializační instanční property
        /// </summary>
        private Dictionary<int, string> _WmDict { get { if (__WmDict is null) __WmDict = _GetWmDict(); return __WmDict; } }
        private Dictionary<int, string> __WmDict;
        /// <summary>
        /// Vygeneruje a vrátí Dictionary obsahující známé WinMSG kódy a jejich názvy.
        /// Používá konstanty v třídě <see cref="DxWin32.WM"/>.
        /// </summary>
        /// <returns></returns>
        private static Dictionary<int, string> _GetWmDict()
        {
            Dictionary<int, string> wmDict = new Dictionary<int, string>();
            var fields = typeof(DxWin32.WM).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
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
    #region class DxSkinColorSet a SkinStaticInfo
    /// <summary>
    /// Standardizovaný set barev aktuálního skinu a palety.
    /// Existuje jediná instance, je dostupná přes <see cref="DxComponent.SkinColorSet"/> a je zajištěno, že bude vrácena vždy instance odpovídající aktuálnímu skinu a paletě.
    /// Po změně skinu/palety je tedy vygenerována a vrácena nová instance.
    /// <para/>
    /// Třída je partial, a kdokoliv ji může rozšířit bez toho, aby modifikoval její bázovou část.
    /// Rozšíření se provádí tak, že se nadeklarují potřebné property (barvy, velikost, obrázky, ...),
    /// a vytvoří se nová inicializační metoda (instanční, private) pro naplnění těchto dat. Tato metoda musí mít atribut <see cref="InitializerAttribute"/>.
    /// Konstruktor třídy tyto metody pomocí reflexe najde a postupně je všechny zavolá.
    /// Výsledkem je plně načtená instance!
    /// <para/>
    /// Aplikační kód tedy vždy může získat instanci této třídy a číst hodnotu z libovolných properties, a spolehne se na to, že pochází z aktuálního skinu.
    /// </summary>
    public partial class DxSkinColorSet
    {
        #region Konstruktor, skin, paleta, aktuálnost, řízení načítání
        /// <summary>
        /// Konstruktor
        /// </summary>
        private DxSkinColorSet(DevExpress.Skins.Skin skin, string skinName, bool isCompact, string paletteName)
        {
            this.Skin = skin;
            this.SkinName = skinName;
            this.IsCompact = isCompact;
            this.PaletteName = paletteName;
            this.__SvgColors = new Dictionary<string, Color>();
            this.__ControlColors = new Dictionary<string, Color>();
        }
        /// <summary>
        /// Vizualizace: obsahuje Název skinu, příznak Compact, a jméno palety
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SkinNameCompactPalette;
        }
        /// <summary>
        /// Suffix " Compact"
        /// </summary>
        private static string _SuffixCompact { get { return "; Compact"; } }
        /// <summary>
        /// Instance Skinu
        /// </summary>
        public DevExpress.Skins.Skin Skin { get; private set; }
        /// <summary>
        /// Jméno skinu
        /// </summary>
        public string SkinName { get; private set; }
        /// <summary>
        /// Skin je "Kompaktní" (=zmenšený prostor), tak se odlišuje WXI ... WXI Compact
        /// </summary>
        public bool IsCompact { get; private set; }
        /// <summary>
        /// Jméno palety
        /// </summary>
        public string PaletteName { get; private set; }
        /// <summary>
        /// Text obsahující název skinu [a suffix; Compact]
        /// </summary>
        public string SkinNameCompact { get { return GetSkinNameCompact(this.SkinName, this.IsCompact); } }
        /// <summary>
        /// Text obsahující název skinu [a suffix; Compact]
        /// </summary>
        public string SkinNameCompactPalette { get { return GetSkinNameCompactPalette(this.SkinName, this.IsCompact, this.PaletteName); } }
        /// <summary>
        /// Obsahuje true, pokud zdejší instance je platná pro aktuální skin a paletu.
        /// </summary>
        public bool IsCurrent
        {
            get
            {
                ReadCurrentSkinPalette(out var _, out string skinName, out bool isCompact, out string paletteName);
                bool isCurrent = String.Equals(SkinName, skinName) && String.Equals(PaletteName, paletteName) && (IsCompact == isCompact);
                if (!isCurrent)
                    SystemAdapter.TraceText(TraceLevel.Info, typeof(DxSkinColorSet), "IsCurrent", "Skin", "SkinChange detect", $"OldSkin: {SkinName}; NewSkin: {skinName}; OldPalette: {PaletteName}; NewPalette: {paletteName}; OldCompact: {IsCompact}; NewCompact: {isCompact}.");
                return isCurrent;
            }
        }
        #endregion
        #region Static support - načtení aktuálního skinu, formátování textu
        /// <summary>
        /// Obsahuje jméno a suffix compact aktuálního skinu, např. "High Contrast Classic" nebo "WXI; Compact": aktuální = nyní platný (proto je static),
        /// nikoliv zde uložený skin, pro který jsme vytvořeni (to je k dispozici v instanční <see cref="SkinNameCompact"/>.
        /// </summary>
        public static string CurrentSkinNameCompact
        {
            get
            {
                ReadCurrentSkinPalette(out var _, out string skinName, out bool isCompact, out string paletteName);
                return GetSkinNameCompact(skinName, isCompact);
            }
        }
        /// <summary>
        /// Obsahuje jméno a suffix compact aktuálního skinu a jméno palety
        /// </summary>
        public static string CurrentSkinNameCompactPalette
        {
            get
            {
                ReadCurrentSkinPalette(out var _, out string skinName, out bool isCompact, out string paletteName);
                return GetSkinNameCompactPalette(skinName, isCompact, paletteName);
            }
        }
        /// <summary>
        /// Vrátí jméno a suffix ; Compact zadaného skinu, např. "High Contrast Classic" nebo "WXI; Compact"
        /// </summary>
        public static string GetSkinNameCompact(string skinName, bool isCompact)
        {
            return skinName + (isCompact ? _SuffixCompact : "");
        }
        /// <summary>
        /// Vrátí jméno a suffix ; Compact a jméno palety zadaného skinu, např. "High Contrast Classic" nebo "Basic : Pine Light"
        /// </summary>
        public static string GetSkinNameCompactPalette(string skinName, bool isCompact, string paletteName)
        {
            return skinName + (isCompact ? _SuffixCompact : "") + (!String.IsNullOrEmpty(paletteName) ? " : " + paletteName : "");
        }
        /// <summary>
        /// Načte a vrátí aktuální jméno skinu a SVG palety
        /// </summary>
        /// <param name="skinName"></param>
        /// <param name="isCompact"></param>
        /// <param name="paletteName"></param>
        internal static void ReadCurrentSkinPalette(out string skinName, out bool isCompact, out string paletteName)
        {
            ReadCurrentSkinPalette(out var _, out skinName, out isCompact, out paletteName);
        }
        /// <summary>
        /// Načte a vrátí aktuální jméno skinu a SVG palety
        /// </summary>
        /// <param name="skin"></param>
        /// <param name="skinName"></param>
        /// <param name="isCompact"></param>
        /// <param name="paletteName"></param>
        internal static void ReadCurrentSkinPalette(out DevExpress.Skins.Skin skin, out string skinName, out bool isCompact, out string paletteName)
        {
            skin = null;
            skinName = null;
            isCompact = false;
            paletteName = null;
            try
            {
                var ulaf = DevExpress.LookAndFeel.UserLookAndFeel.Default;
                skin = DevExpress.Skins.CommonSkins.GetSkin(ulaf);
                skinName = (ulaf.ActiveSkinName ?? "").Trim();
                isCompact = ulaf.CompactUIModeForced;
                paletteName = (ulaf.ActiveSvgPaletteName ?? "").Trim();
            }
            catch (Exception exc)
            {
                SystemAdapter.TraceText(TraceLevel.SysError, typeof(DxSkinColorSet), "ReadSkinPalette", "Skin", "Exception", $"Type: {exc.GetType().FullName}; Message: {exc.Message}.");
            }
        }
        /// <summary>
        /// Vytvoří, naplní a vrátí instanci obsahující data aktuálního skinu a palety
        /// </summary>
        /// <returns></returns>
        internal static DxSkinColorSet Current
        {
            get { return CreateCurrent(); }
        }
        /// <summary>
        /// Vytvoří, naplní a vrátí instanci obsahující data aktuálního skinu a palety
        /// </summary>
        /// <returns></returns>
        internal static DxSkinColorSet CreateCurrent()
        {
            DxSkinColorSet dxSkinColorSet = null;
            try
            {
                SystemAdapter.TraceText(TraceLevel.Info, typeof(DxSkinColorSet), "CreateCurrent", "Skin", "Begin");
                dxSkinColorSet = _CreateCurrent();
                SystemAdapter.TraceText(TraceLevel.Info, typeof(DxSkinColorSet), "CreateCurrent", "Skin", "Done", $"SkinName: {dxSkinColorSet.SkinName}; SkinPalette: {dxSkinColorSet.PaletteName}");
            }
            catch (Exception exc)
            {
                SystemAdapter.TraceText(TraceLevel.SysError, typeof(DxSkinColorSet), "CreateCurrent", "Skin", "Exception", $"Type: {exc.GetType().FullName}; Message: {exc.Message}.");
            }

            return dxSkinColorSet;
        }
        /// <summary>
        /// Vytvoří, naplní a vrátí instanci obsahující data aktuálního skinu a palety
        /// </summary>
        /// <returns></returns>
        private static DxSkinColorSet _CreateCurrent()
        {
            ReadCurrentSkinPalette(out var skin, out var skinName, out bool isCompact, out var paletteName);
            DxSkinColorSet colorSet = new DxSkinColorSet(skin, skinName, isCompact, paletteName);

            // Zajistíme spuštění všech metod, které mají atribut Initializer:
            var initializers = InitializerAttribute.SearchInitializerMethods(colorSet.GetType());

            /*
            var initializers = colorSet.GetType()
                .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .Where(m => m.CustomAttributes.Any(a => a.AttributeType == typeof(InitializerAttribute)))
                .ToList();

            if (initializers.Count > 1) initializers.Sort(compareByPriority);
            */

            foreach (var initializer in initializers)
                initializer.Invoke(colorSet, null);

            return colorSet;

            /*
            int compareByPriority(System.Reflection.MethodInfo a, System.Reflection.MethodInfo b)
            {
                int ap = getPriority(a);
                int bp = getPriority(b);
                return ap.CompareTo(bp);
            }
            int getPriority(System.Reflection.MethodInfo method)
            {
                var initializer = method.GetCustomAttributes(typeof(InitializerAttribute), false).FirstOrDefault() as InitializerAttribute;
                return initializer?.Priority ?? 0;
            }
            */
        }
        #endregion
        #region Pojmenované barvy
        private Dictionary<string, Color> __SvgColors;
        private void _AddSvgColors(Dictionary<string, Color> svgColors)
        {
            _AddColorsTo(this.__SvgColors, svgColors);
        }
        private Dictionary<string, Color> __ControlColors;
        private void _AddControlColors(Dictionary<string, Color> controlColors)
        {
            _AddColorsTo(this.__ControlColors, controlColors);
        }
        private static void _AddColorsTo(Dictionary<string, Color> target, Dictionary<string, Color> source)
        {
            if (source is null || source.Count == 0) return;
            foreach (var pair in source)
            {
                if (!target.ContainsKey(pair.Key))
                    target.Add(pair.Key, pair.Value);
            }
        }
        #endregion
        #region Support metody
        /// <summary>
        /// Vrátí Dictionary obsahující jména barev a přímo barvy z dodané SVG palety.
        /// </summary>
        /// <param name="svgPalette"></param>
        private static Dictionary<string, Color> GetColorsDictionary(SvgPalette svgPalette)
        {
            return svgPalette.Colors.CreateDictionary(sc => sc.Name, sc => sc.Value, true);
        }
        /// <summary>
        /// Vrátí Dictionary obsahující jména barev a přímo barvy z dodané Skin palety.
        /// </summary>
        /// <param name="skinColors"></param>
        /// <returns></returns>
        private static Dictionary<string, Color> GetColorsDictionary(DevExpress.Skins.SkinColors skinColors)
        {
            Dictionary<string, Color> dictionary = new Dictionary<string, Color>();
            var properties = skinColors.GetProperties();
            foreach (System.Collections.DictionaryEntry property in properties)
            {
                if (property.Key is string && property.Value is Color)
                {
                    string key = property.Key as string;
                    Color color = (Color)property.Value;
                    if (key != null && !dictionary.ContainsKey(key))
                        dictionary.Add(key, color);
                }
            }
            return dictionary;
        }
        /// <summary>
        /// Vrátí true, pokud dodaná paleta obsahuje všechny zadané názvy barev.
        /// </summary>
        /// <param name="svgDict"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        private static bool ContainAllNames(Dictionary<string, Color> svgDict, params string[] names)
        {
            if (svgDict is null || svgDict.Count == 0) return false;           // Pokud Dictionary je null nebo prázdná, pak neobsahuje nic
            if (names.Any(n => !svgDict.ContainsKey(n))) return false;         // Pokud najdu jediné names, které NEEXISTUJE v Dictionary, pak vracím false = neobsahuje všechny klíče.
            return true;
        }
        #endregion
        #region Základní data a jejich načtení
        /// <summary>
        /// Z aktuálního skinu a palety načte základní barvy
        /// </summary>
        [Initializer(-10)]
        private void _ColorsRead()
        {
            var skin = this.Skin;

            var customPalettes = skin.CustomSvgPalettes;
            if (!String.IsNullOrEmpty(this.PaletteName) && customPalettes.Count > 0)
            {
                var customPalette = customPalettes[this.PaletteName];
                if (customPalette != null)
                    readFromSvgPalette(customPalette);
            }

            var svgPalettes = skin.SvgPalettes;
            var defaultPalette = (svgPalettes.Count > 0 ? svgPalettes["DefaultSkinPalette"] : null);
            if (defaultPalette != null)
                readFromSvgPalette(defaultPalette);

            var skinColors = skin.Container.GetSkin(DevExpress.Skins.SkinProductId.Common).Colors;
            if (skinColors != null)
                readFromSkinCommonColors(skinColors);

            detectDarkSkin();

            // Načte barvy Basic z dodané SVG palety; načítá jen hodnoty, dosud nenačtené
            void readFromSvgPalette(SvgPalette svgPalette)
            {
                var svgDict = GetColorsDictionary(svgPalette);
                _AddSvgColors(svgDict);

                // Palety jsou vícero druhů: starší obsahují názvy barev: "Paint", "Paint High", "Paint Shadow", atd...:
                /*
Paint			        Pozadí - Panel
Paint High		        Pozadí - TextBox
Paint Shadow		    Pozadí - ColumnHeader a Form Footer
Paint Deep Shadow
Brush			        Písmo v Panelu a ColumnHeader a ve Footeru
Brush Light		        Písmo Disabled v Panelu a ColumnHeader a ve Footeru
Brush High		        Písmo v TextBoxu
Brush Major		        Border v Editoru
Brush Minor		        GridLines v Editoru / Gridu, linky v Panelu


Accent Paint		    Zvýrazněné písmo v Panelu, ve Footeru, Pozadí Selected Row
Accent Paint Light	    Pozadí v Hot Row
Accent Brush		    Písmo v Selected Row
Accent Brush Light	    ?
Key Paint		        Pozadí - Titulek okna
Key Brush		        Písmo - Titulek okna
Key Brush Light		    Písmo - Titulek okna šedivý


Accent Paint		    Pozadí - Titulek okna
Accent Paint Dark	    Pozadí - Zmáčknutý button na okně
Accent Paint Light	    Pozadí - Selected row v Gridu
Accent Paint Lighter	Pozadí - Hot row v Gridu
Accent Brush		    Písmo v titulku okna
Accent Brush Light	    Písmo v titulku okna šedivé


Red
Green
Blue
Yellow
Black
Gray
White



                */
                // Následující mapování (tzn. jméno barvy => její použití na Controlech) bylo odvozeno pomocí SkinEditoru pro různé skiny a palety...

                if (ContainAllNames(svgDict, "Accent Paint", "Accent Paint Light", "Accent Brush", "Accent Brush Light", "Key Paint", "Key Brush", "Key Brush Light"))
                {   // Zvýrazněné barvy varianta A:
                    this.AccentPaint = applyColor(this.AccentPaint, svgDict, "Accent Paint");
                    this.LabelForeColorAccent = applyColor(this.LabelForeColorAccent, svgDict, "Accent Paint");
                    this.EditorBackColorHot = applyColor(this.EditorBackColorHot, svgDict, "Accent Paint Light");
                    this.EditorForeColorSelected = applyColor(this.EditorForeColorSelected, svgDict, "Accent Brush");
                    this.AccentBrushLight = applyColor(this.AccentBrushLight, svgDict, "Accent Brush Light");
                    this.WindowTitleBackColor = applyColor(this.WindowTitleBackColor, svgDict, "Key Paint");
                    this.WindowTitleForeColor = applyColor(this.WindowTitleForeColor, svgDict, "Key Brush");
                    this.WindowTitleForeColorDisabled = applyColor(this.WindowTitleForeColorDisabled, svgDict, "Key Brush Light");
                }

                if (ContainAllNames(svgDict, "Accent Paint", "Accent Paint Dark", "Accent Paint Light", "Accent Paint Lighter", "Accent Brush", "Accent Brush Light"))
                {   // Zvýrazněné barvy varianta B:
                    this.AccentPaint = applyColor(this.AccentPaint, svgDict, "Accent Paint");
                    this.WindowTitleBackColor = applyColor(this.WindowTitleBackColor, svgDict, "Accent Paint");
                    this.WindowTitleBackColorActive = applyColor(this.WindowTitleBackColorActive, svgDict, "Accent Paint Dark");
                    this.EditorBackColorSelected = applyColor(this.EditorBackColorSelected, svgDict, "Accent Paint Light");
                    this.EditorBackColorHot = applyColor(this.EditorBackColorHot, svgDict, "Accent Paint Lighter");
                    this.WindowTitleForeColor = applyColor(this.WindowTitleForeColor, svgDict, "Accent Brush");
                    this.WindowTitleForeColorDisabled = applyColor(this.WindowTitleForeColorDisabled, svgDict, "Accent Brush Light");
                }

                if (ContainAllNames(svgDict, "Paint", "Paint High", "Paint Shadow", "Paint Deep Shadow", "Brush", "Brush Light", "Brush High", "Brush Major", "Brush Minor"))
                {   // Základní názvy barev klasické:
                    this.PanelBackColor = applyColor(this.PanelBackColor, svgDict, "Paint");
                    this.EditorBackColor = applyColor(this.EditorBackColor, svgDict, "Paint High");
                    this.HeaderFooterBackColor = applyColor(this.HeaderFooterBackColor, svgDict, "Paint Shadow");
                    this.HeaderFooterSecondBackColor = applyColor(this.HeaderFooterSecondBackColor, svgDict, "Paint Deep Shadow");
                    this.LabelForeColor = applyColor(this.LabelForeColor, svgDict, "Brush");
                    this.EditorForeColor = applyColor(this.EditorForeColor, svgDict, "Brush Light");
                    this.EditorForeColorDisabled = applyColor(this.EditorForeColorDisabled, svgDict, "Brush High");
                    this.PanelForeColor = applyColor(this.PanelForeColor, svgDict, "Brush High");
                    this.EditorBorderColor = applyColor(this.EditorBorderColor, svgDict, "Brush Major");
                    this.EditorGridLineColor = applyColor(this.EditorGridLineColor, svgDict, "Brush Minor");
                }

                if (ContainAllNames(svgDict, "Background 0", "Edit Background 0", "Background 200", "Foreground 100", "Edit Foreground 100", "Edit Foreground 25", "Line 50", "Line 25"))
                {   // Základní názvy barev pro moderní skiny:
                    this.PanelBackColor = applyColor(this.PanelBackColor, svgDict, "Background 0");
                    this.EditorBackColor = applyColor(this.EditorBackColor, svgDict, "Edit Background 0");
                    this.HeaderFooterBackColor = applyColor(this.HeaderFooterBackColor, svgDict, "Background 200");
                    this.LabelForeColor = applyColor(this.LabelForeColor, svgDict, "Foreground 100");
                    this.EditorForeColor = applyColor(this.EditorForeColor, svgDict, "Edit Foreground 100");
                    this.EditorForeColorDisabled = applyColor(this.EditorForeColorDisabled, svgDict, "Edit Foreground 25");
                    this.PanelForeColor = applyColor(this.PanelForeColor, svgDict, "Foreground 100");
                    this.EditorBorderColor = applyColor(this.EditorBorderColor, svgDict, "Line 50");
                    this.EditorGridLineColor = applyColor(this.EditorGridLineColor, svgDict, "Line 25");
                }

                if (ContainAllNames(svgDict, "Primary Background 0", "Secondary Background 0", "Secondary Background -100", "Edit Background -100", "Edit Background -200", "Secondary Foreground 100", "Secondary Foreground 25"))
                {   // Zvýrazněné názvy barev pro moderní skiny:
                    this.AccentPaint = applyColor(this.AccentPaint, svgDict, "Primary Background 0");
                    this.WindowTitleBackColor = applyColor(this.WindowTitleBackColor, svgDict, "Secondary Background 0");
                    this.WindowTitleBackColorActive = applyColor(this.WindowTitleBackColorActive, svgDict, "Secondary Background -100");
                    this.EditorBackColorSelected = applyColor(this.EditorBackColorSelected, svgDict, "Edit Background -100");
                    this.EditorBackColorHot = applyColor(this.EditorBackColorHot, svgDict, "Edit Background -200");
                    this.WindowTitleForeColor = applyColor(this.WindowTitleForeColor, svgDict, "Secondary Foreground 100");
                    this.WindowTitleForeColorDisabled = applyColor(this.WindowTitleForeColorDisabled, svgDict, "Secondary Foreground 25");
                }

                if (ContainAllNames(svgDict, "Red", "Green", "Blue", "Yellow", "Black", "Gray", "White"))
                {   // Primární barvy modifikované pro skin:
                    this.BasicColorRed = applyColor(this.BasicColorRed, svgDict, "Red");
                    this.BasicColorGreen = applyColor(this.BasicColorGreen, svgDict, "Green");
                    this.BasicColorBlue = applyColor(this.BasicColorBlue, svgDict, "Blue");
                    this.BasicColorYellow = applyColor(this.BasicColorYellow, svgDict, "Yellow");
                    this.BasicColorBlack = applyColor(this.BasicColorBlack, svgDict, "Black");
                    this.BasicColorGray = applyColor(this.BasicColorGray, svgDict, "Gray");
                    this.BasicColorWhite = applyColor(this.BasicColorWhite, svgDict, "White");
                }
            }

            // Načte barvy Basic z dodané sady barev skinu; načítá jen hodnoty, dosud nenačtené
            void readFromSkinCommonColors(DevExpress.Skins.SkinColors commonColors)
            {
                // Vstupní barvy: Key = systémový název barvy (např. "DisabledText"), Value = barva:
                var commonDict = GetColorsDictionary(commonColors);

                // Pozor: jméno barvy (Color.Name) může mít formu "@SvgColorName",
                //        pak vepsaná konkrétní barva je bezvýznamná a je třeba získat barvu definovanou pro SVG = ze zdejší Dictionary this.SvgColors !!
                var svgColors = this.__SvgColors;
                var colorDict = new Dictionary<string, Color>();
                foreach (var kvp in commonDict)
                {
                    string itemName = kvp.Key;                       // Jméno položky, např. "DisabledText"
                    if (!colorDict.ContainsKey(itemName))
                    {
                        Color itemColor = kvp.Value;                 // Barva položky, např. "{Name=@Foreground 25, ARGB=(255, 240, 240, 240)}"
                        string colorName = itemColor.Name;           // Otestujeme její jméno, např. "@Foreground 25" - v paletě SVG barev:
                        if (colorName != null && colorName.Length > 2 && colorName[0] == '@' && svgColors.TryGetValue(colorName.Substring(1), out var svgColor))
                            itemColor = svgColor;                    // Reálná barva pro účely "DisabledText", záskaná z SVG palety pro jméno "Foreground 25": RGB="189; 189; 189"
                        colorDict.Add(itemName, itemColor);
                    }
                }

                // Konkrétní barvy vložíme do zdejší instanční Dictionary:
                _AddControlColors(colorDict);

                // debug barev:
                //string colors1 = commonDict.Select(i => $"\"{i.Key}\" = {i.Value}").ToOneString();
                //string colors2 = colorDict.Select(i => $"\"{i.Key}\" = {i.Value}").ToOneString();
                //string command1 = colorDict.Select(i => $"this.xxxxxxx = applyColor(this.xxxxxxx, colorDict, \"{i.Key}\");").ToOneString();
                //string command2 = colorDict.Select(i => $"this.Named{i.Key} = applyColor(this.Named{i.Key}, colorDict, \"{i.Key}\");").ToOneString();
                //string command3 = colorDict.Select(i => $"/// <summary>\r\n/// Pojmenovaná systémová barva '{i.Key}'\r\n/// </summary>\r\npublic Color? Named{i.Key} " + "{ get; private set; }").ToOneString();

                // Namapujeme barvy do našich property:
                this.LabelForeColor = applyColor(this.LabelForeColor, colorDict, "WindowText");
                this.EditorForeColorDisabled = applyColor(this.EditorForeColorDisabled, colorDict, "ReadOnly");
                this.HeaderFooterBackColor = applyColor(this.HeaderFooterBackColor, colorDict, "Info");
                this.BasicColorGreen = applyColor(this.BasicColorGreen, colorDict, "Success");
                this.BasicColorRed = applyColor(this.BasicColorRed, colorDict, "Danger");
                this.PanelBackColor = applyColor(this.PanelBackColor, colorDict, "Control");
                this.EditorForeColorDisabled = applyColor(this.EditorForeColorDisabled, colorDict, "DisabledText");
                this.EditorBackColorSelected = applyColor(this.EditorBackColorSelected, colorDict, "Highlight");
                this.BasicColorBlue = applyColor(this.BasicColorBlue, colorDict, "Question");
                this.AccentPaint = applyColor(this.AccentPaint, colorDict, "Primary");
                this.BasicColorYellow = applyColor(this.BasicColorYellow, colorDict, "WarningFill");
                this.LabelForeColor = applyColor(this.LabelForeColor, colorDict, "InfoText");
                this.EditorBackColorHot = applyColor(this.EditorBackColorHot, colorDict, "HotTrackedColor");
                this.EditorForeColorDisabled = applyColor(this.EditorForeColorDisabled, colorDict, "DisabledControl");
                this.BasicColorGreen = applyColor(this.BasicColorGreen, colorDict, "Information");
                this.EditorForeColor = applyColor(this.EditorForeColor, colorDict, "HighlightText");
                this.PanelForeColor = applyColor(this.PanelForeColor, colorDict, "ControlText");
                this.BasicColorBlue = applyColor(this.BasicColorBlue, colorDict, "QuestionFill");
                this.BasicColorYellow = applyColor(this.BasicColorYellow, colorDict, "Warning");
                this.EditorForeColor = applyColor(this.EditorForeColor, colorDict, "InactiveCaptionText");
                this.EditorBackColor = applyColor(this.EditorBackColor, colorDict, "Window");
                this.WindowTitleBackColorActive = applyColor(this.WindowTitleBackColorActive, colorDict, "HideSelection");
                this.HeaderFooterBackColor = applyColor(this.HeaderFooterBackColor, colorDict, "Menu");
                this.PanelForeColor = applyColor(this.PanelForeColor, colorDict, "MenuText");
                this.BasicColorRed = applyColor(this.BasicColorRed, colorDict, "Critical");

                // Uložíme pojmenované barvy:
                this.NamedWindowText = applyColor(this.NamedWindowText, colorDict, "WindowText");
                this.NamedReadOnly = applyColor(this.NamedReadOnly, colorDict, "ReadOnly");
                this.NamedInfo = applyColor(this.NamedInfo, colorDict, "Info");
                this.NamedSuccess = applyColor(this.NamedSuccess, colorDict, "Success");
                this.NamedHotTrackedForeColor = applyColor(this.NamedHotTrackedForeColor, colorDict, "HotTrackedForeColor");
                this.NamedDanger = applyColor(this.NamedDanger, colorDict, "Danger");
                this.NamedControl = applyColor(this.NamedControl, colorDict, "Control");
                this.NamedDisabledText = applyColor(this.NamedDisabledText, colorDict, "DisabledText");
                this.NamedHighlight = applyColor(this.NamedHighlight, colorDict, "Highlight");
                this.NamedQuestion = applyColor(this.NamedQuestion, colorDict, "Question");
                this.NamedPrimary = applyColor(this.NamedPrimary, colorDict, "Primary");
                this.NamedHighlightAlternate = applyColor(this.NamedHighlightAlternate, colorDict, "HighlightAlternate");
                this.NamedWarningFill = applyColor(this.NamedWarningFill, colorDict, "WarningFill");
                this.NamedInfoText = applyColor(this.NamedInfoText, colorDict, "InfoText");
                this.NamedHotTrackedColor = applyColor(this.NamedHotTrackedColor, colorDict, "HotTrackedColor");
                this.NamedDisabledControl = applyColor(this.NamedDisabledControl, colorDict, "DisabledControl");
                this.NamedInformation = applyColor(this.NamedInformation, colorDict, "Information");
                this.NamedHighlightText = applyColor(this.NamedHighlightText, colorDict, "HighlightText");
                this.NamedControlText = applyColor(this.NamedControlText, colorDict, "ControlText");
                this.NamedQuestionFill = applyColor(this.NamedQuestionFill, colorDict, "QuestionFill");
                this.NamedWarning = applyColor(this.NamedWarning, colorDict, "Warning");
                this.NamedInactiveCaptionText = applyColor(this.NamedInactiveCaptionText, colorDict, "InactiveCaptionText");
                this.NamedWindow = applyColor(this.NamedWindow, colorDict, "Window");
                this.NamedHideSelection = applyColor(this.NamedHideSelection, colorDict, "HideSelection");
                this.NamedMenu = applyColor(this.NamedMenu, colorDict, "Menu");
                this.NamedMenuText = applyColor(this.NamedMenuText, colorDict, "MenuText");
                this.NamedCritical = applyColor(this.NamedCritical, colorDict, "Critical");
            }

            // Pokusí se najít barvu. Pokud na vstupu už barva je, vrátí ji a nehledá. Pokud barva na vstupu není, a v Dictionary najde daný klíč, vrátí jeho barvu.
            Color? applyColor(Color? currentValue, Dictionary<string, Color> colorDict, string name)
            {
                if (currentValue.HasValue || name is null) return currentValue;
                if (colorDict.TryGetValue(name, out var color)) return color;
                return null;
            }

            // Určí hodnoty IsPanelDark a IsEditorDark podle nalezených barev PanelBackColor a EditorBackColor
            void detectDarkSkin()
            {
                this.IsPanelDark = isColorDark(this.PanelBackColor);
                this.IsEditorDark = isColorDark(this.EditorBackColor);
            }

            // Vrátí true, pokud daná barva je tmavá
            bool isColorDark(Color? color)
            {
                if (!color.HasValue) return false;
                return (color.Value.GetBrightness() < 0.5f);
            }
        }
        /// <summary>
        /// Aktuální skin má tmavé pozadí panelů (<see cref="PanelBackColor"/>
        /// </summary>
        public bool IsPanelDark { get; private set; }
        /// <summary>
        /// Aktuální skin má tmavé pozadí vstupních editor prvků (<see cref="EditorBackColor"/>
        /// </summary>
        public bool IsEditorDark { get; private set; }
        /// <summary>
        /// Barva panelu (Container), pozadí
        /// </summary>
        public Color? PanelBackColor { get; private set; }
        /// <summary>
        /// Barva editoru (TextEdit), pozadí
        /// </summary>
        public Color? EditorBackColor { get; private set; }
        /// <summary>
        /// Barva titulku (Header) nebo zápatí (Footer), pozadí
        /// </summary>
        public Color? HeaderFooterBackColor { get; private set; }
        /// <summary>
        /// Podobná jako <see cref="HeaderFooterBackColor"/>
        /// </summary>
        public Color? HeaderFooterSecondBackColor { get; private set; }
        /// <summary>
        /// Barva písma použitá na pozadí <see cref="PanelBackColor"/> a <see cref="HeaderFooterBackColor"/>
        /// </summary>
        public Color? LabelForeColor { get; private set; }
        /// <summary>
        /// Barva písma použitá na editoru (TextEdit, Combo, atd), použitá na pozadí <see cref="EditorBackColor"/>
        /// </summary>
        public Color? EditorForeColor { get; private set; }
        /// <summary>
        /// Barva písma použitá na Disabled editoru (TextEdit, Combo, atd), použitá na pozadí <see cref="EditorBackColor"/>
        /// </summary>
        public Color? EditorForeColorDisabled { get; private set; }
        /// <summary>
        /// Barva písma použitá na panelu (Container) i na Title+Footer <see cref="PanelBackColor"/> a <see cref="HeaderFooterBackColor"/>
        /// </summary>
        public Color? PanelForeColor { get; private set; }
        /// <summary>
        /// Barva borderu editoru
        /// </summary>
        public Color? EditorBorderColor { get; private set; }
        /// <summary>
        /// Barva linky (mezi řádky / sloupci) uvnitř editoru - GridLines v Gridu atd
        /// </summary>
        public Color? EditorGridLineColor { get; private set; }

        /// <summary>
        /// Barva pozadí editoru ve stavu Hot = pod myší (řádek gridu)
        /// </summary>
        public Color? EditorBackColorHot { get; private set; }
        /// <summary>
        /// Barva pozadí editoru ve stavu Selected = s kurzorem
        /// </summary>
        public Color? EditorBackColorSelected { get; private set; }
        /// <summary>
        /// Barva písma  editoru ve stavu Selected = s kurzorem
        /// </summary>
        public Color? EditorForeColorSelected { get; private set; }
        /// <summary>
        /// Barva zvýraznění, použitá na pozadí header okna, a na zvýrazněné písmo na <see cref="HeaderFooterBackColor"/>
        /// </summary>
        public Color? LabelForeColorAccent { get; private set; }

        /// <summary>
        /// Barva zvýrazněného písma nebo grafiky na pozadí panelu
        /// </summary>
        public Color? AccentPaint { get; private set; }
        /// <summary>
        /// Barva pozadí titulkového řádku okna
        /// </summary>
        public Color? WindowTitleBackColor { get; private set; }
        /// <summary>
        /// Barva pozadí stisknutého buttonu v titulkovém řádku okna (tlačítko Minimize)
        /// </summary>
        public Color? WindowTitleBackColorActive { get; private set; }
        /// <summary>
        /// Barva písma titulkového řádku okna
        /// </summary>
        public Color? WindowTitleForeColor { get; private set; }
        /// <summary>
        /// Barva písma titulkového řádku okna ve stavu Disabled
        /// </summary>
        public Color? WindowTitleForeColorDisabled { get; private set; }
        /// <summary>
        /// Barva zvýraznění
        /// </summary>
        public Color? AccentBrushLight { get; private set; }

        /// <summary>
        /// Základní standardní barva Red
        /// </summary>
        public Color? BasicColorRed { get; private set; }
        /// <summary>
        /// Základní standardní barva Green
        /// </summary>
        public Color? BasicColorGreen { get; private set; }
        /// <summary>
        /// Základní standardní barva Blue
        /// </summary>
        public Color? BasicColorBlue { get; private set; }
        /// <summary>
        /// Základní standardní barva Yellow
        /// </summary>
        public Color? BasicColorYellow { get; private set; }
        /// <summary>
        /// Základní standardní barva Black
        /// </summary>
        public Color? BasicColorBlack { get; private set; }
        /// <summary>
        /// Základní standardní barva Gray
        /// </summary>
        public Color? BasicColorGray { get; private set; }
        /// <summary>
        /// Základní standardní barva White
        /// </summary>
        public Color? BasicColorWhite { get; private set; }

        /// <summary>
        /// Pojmenovaná systémová barva 'WindowText'
        /// </summary>
        public Color? NamedWindowText { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'ReadOnly'
        /// </summary>
        public Color? NamedReadOnly { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'Info'
        /// </summary>
        public Color? NamedInfo { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'Success'
        /// </summary>
        public Color? NamedSuccess { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'HotTrackedForeColor'
        /// </summary>
        public Color? NamedHotTrackedForeColor { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'Danger'
        /// </summary>
        public Color? NamedDanger { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'Control'
        /// </summary>
        public Color? NamedControl { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'DisabledText'
        /// </summary>
        public Color? NamedDisabledText { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'Highlight'
        /// </summary>
        public Color? NamedHighlight { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'Question'
        /// </summary>
        public Color? NamedQuestion { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'Primary'
        /// </summary>
        public Color? NamedPrimary { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'HighlightAlternate'
        /// </summary>
        public Color? NamedHighlightAlternate { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'WarningFill'
        /// </summary>
        public Color? NamedWarningFill { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'InfoText'
        /// </summary>
        public Color? NamedInfoText { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'HotTrackedColor'
        /// </summary>
        public Color? NamedHotTrackedColor { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'DisabledControl'
        /// </summary>
        public Color? NamedDisabledControl { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'Information'
        /// </summary>
        public Color? NamedInformation { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'HighlightText'
        /// </summary>
        public Color? NamedHighlightText { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'ControlText'
        /// </summary>
        public Color? NamedControlText { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'QuestionFill'
        /// </summary>
        public Color? NamedQuestionFill { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'Warning'
        /// </summary>
        public Color? NamedWarning { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'InactiveCaptionText'
        /// </summary>
        public Color? NamedInactiveCaptionText { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'Window'
        /// </summary>
        public Color? NamedWindow { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'HideSelection'
        /// </summary>
        public Color? NamedHideSelection { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'Menu'
        /// </summary>
        public Color? NamedMenu { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'MenuText'
        /// </summary>
        public Color? NamedMenuText { get; private set; }
        /// <summary>
        /// Pojmenovaná systémová barva 'Critical'
        /// </summary>
        public Color? NamedCritical { get; private set; }

        #endregion
        #region Rozměrová data a jejich načtení
        /// <summary>
        /// Z aktuálního skinu a palety načte základní rozměry
        /// </summary>
        [Initializer(-9)]
        private void _SizesRead()
        {
            var skin = this.Skin;

            DxComponent.TryFindStaticSkinInfo(this.SkinNameCompact, out var staticInfo);
            this.TabHeaderHeight = staticInfo?.TabHeaderHeight ?? 33;



            //  TAKHLE JSEM NAČÍTAL VLASTNOSTI SKINU:
            // Tady jsem si jen získal SkinNameCompact do Clipboardu, a následně na živé aplikaci změřil skin, a výsledky opisuji do SkinStaticInfo.CreateDictionary() ...

            //var skinElements = skin.GetElements();
            //var tabElements = skin.Container.GetSkin(DevExpress.Skins.SkinProductId.Tab).GetElements();

            //if (SumSkinNames is null) SumSkinNames = new List<string>();
            //string skinText = this.SkinNameCompact;
            //DxComponent.ClipboardInsert(skinText);


            //string skinName = $"\"{skinText}\"";
            //if (!SumSkinNames.Contains(skinName))
            //{
            //    SumSkinNames.Add(skinName);
            //    SumSkinNames.Sort();
            //}
            //string skinNames = String.Join(", ", SumSkinNames);
            //DxComponent.ClipboardInsert(skinNames);

            // MultiMonitor, Windows Zoom, UHD

            /*
            b) NonUHD, WinZoom 100%, prvek výšky 22px je vykreslen na 22px a je čistý       DPI controlu je  96
            b) NonUHD, WinZoom 150%, prvek výšky 22px je vykreslen na 35px a je rozmazaný   DPI controlu je  96  (resize na 150% si řeší DevExpess a Windows)
            b) UHD,    WinZoom 150%, prvek výšky 22px je vykreslen na 35px a je čistý       DPI controlu je 144  (měli bychom zadávat reálnou výšku controlu = to řeší sekvence: var formDpi = this.FindForm()?.DeviceDpi ?? 96;   var currentHeight = DxComponent.ZoomToGui(tabSkinHeight, formDpi);  )






            */
        }
        // private static List<string> SumSkinNames;


        /// <summary>
        /// Výška záložky na TabHeaderu, Pro neznámé skiny je zde 33.
        /// </summary>
        public int TabHeaderHeight { get; private set; }

        #endregion
    }
    /// <summary>
    /// Atribut deklarující metodu, která je rozpoznána a vyvolána v procesu generické inicializace třídy.
    /// Konstruktor třídy najde metody s tímto atributem a vyvolá je.
    /// Lze zadat pořadí = priorita volání, bude se volat podle priority vzestupně.
    /// Metoda sama tedy vypadá, jako by ji nikdo nevolal, protože je volána pomocí reflexe.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class InitializerAttribute : Attribute
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public InitializerAttribute() { Priority = 0; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public InitializerAttribute(int priority) { Priority = priority; }
        /// <summary>
        /// Priorita zpracování, metoda s nižším číslem bude volána dříve
        /// </summary>
        public int Priority { get; private set; }
        /// <summary>
        /// V daném typu najde instanční metody, které mají atribut <see cref="InitializerAttribute"/>, seřadí je podle jejich priority <see cref="Priority"/>, a výsledné pole vrátí.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static System.Reflection.MethodInfo[] SearchInitializerMethods(Type type)           // List of: Method, Priority
        {
            var infos = new List<Tuple<System.Reflection.MethodInfo, int>>();
            var methods = type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.FlattenHierarchy);
            foreach (var method in methods)
            {
                var attributes = System.Reflection.CustomAttributeExtensions.GetCustomAttributes<InitializerAttribute>(method, true);
                if (attributes != null && attributes.Any())
                {
                    var attribute = attributes.First();
                    infos.Add(new Tuple<System.Reflection.MethodInfo, int>(method, attribute.Priority));
                }
            }
            if (infos.Count > 1) infos.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));             // ORDER BY Priority ASC
            return infos.Select(t => t.Item1).ToArray();
        }
    }
    /// <summary>
    /// Statické informace o skinu, vlastnosti jsou získané měřením, nikoli dynamicky detekcí vlastností skinu.
    /// </summary>
    internal class SkinStaticInfo
    {
        #region Konstruktor a public vlastnosti
        /// <summary>
        /// Konstruktor je privátní
        /// </summary>
        /// <param name="skinName"></param>
        /// <param name="tabHeaderHeight"></param>
        /// <param name="hasTwoDarkState"></param>
        private SkinStaticInfo(string skinName, int tabHeaderHeight, bool hasTwoDarkState)
        {
            this.SkinName = skinName;
            this.TabHeaderHeight = tabHeaderHeight;
            this.HasTwoDarkState = hasTwoDarkState;
        }
        /// <summary>
        /// Jméno skinu odpovídající kódu
        /// </summary>
        public string SkinName { get; private set; }
        /// <summary>
        /// Výška prostoru TabHeader
        /// </summary>
        public int TabHeaderHeight { get; private set; }
        /// <summary>
        /// Má dva stavy IsDark: liší se světlost Panelu od světlosti TextBoxu
        /// </summary>
        public bool HasTwoDarkState { get; private set; }
        #endregion
        #region Static tvorba Dictionary
        /// <summary>
        /// Vygeneruje a vrátí statickou Dictionary, obsahující fixní výčet skinů a jejich vlastnosti získané měřením, nikoli dynamicky detekcí vlastností skinu.
        /// Tato metoda nepotřebuje přístup k funkční instanci <see cref="DxComponent"/>.
        /// </summary>
        /// <returns></returns>
        internal static Dictionary<string, SkinStaticInfo> CreateDictionary()
        {
            Dictionary<string, SkinStaticInfo> result = new Dictionary<string, SkinStaticInfo>();

            addInfo("Basic", 33, false);
            addInfo("Bezier", 27, false);
            addInfo("WXI", 41, false);
            addInfo("WXI; Compact", 32, false);
            addInfo("Office 2019 Colorful", 37, false);
            addInfo("Office 2019 Black", 37, false);
            addInfo("Office 2019 White", 37, false);
            addInfo("Office 2019 Dark Gray", 37, false);
            addInfo("High Contrast", 27, false);
            addInfo("DevExpress Style", 32, false);
            addInfo("DevExpress Dark Style", 32, false);
            addInfo("Office 2016 Colorful", 27, false);
            addInfo("Office 2016 Dark", 27, true);
            addInfo("Office 2016 Black", 27, false);
            addInfo("Office 2013", 27, false);
            addInfo("Office 2013 Dark Gray", 27, false);
            addInfo("Office 2013 Light Gray", 27, false);
            addInfo("Visual Studio 2013 Blue", 27, false);
            addInfo("Visual Studio 2013 Dark", 27, false);
            addInfo("Visual Studio 2013 Light", 27, false);
            addInfo("VS2010", 28, false);
            addInfo("Office 2010 Blue", 29, false);
            addInfo("Office 2010 Black", 29, true);
            addInfo("Office 2010 Silver", 29, false);
            addInfo("Office 2007 Black", 23, false);
            addInfo("Office 2007 Blue", 23, false);
            addInfo("Office 2007 Green", 23, false);
            addInfo("Office 2007 Pink", 23, false);
            addInfo("Office 2007 Silver", 23, false);
            addInfo("Blueprint", 24, false);
            addInfo("High Contrast Classic", 21, true);
            addInfo("Metropolis", 21, false);
            addInfo("Metropolis Dark", 21, false);
            addInfo("Whiteprint", 24, false);
            addInfo("Pumpkin", 22, true);
            addInfo("Springtime", 22, false);
            addInfo("Summer 2008", 22, false);
            addInfo("Valentine", 22, false);
            addInfo("Xmas 2008 Blue", 22, false);
            addInfo("McSkin", 22, false);
            addInfo("Seven Classic", 31, false);
            addInfo("Black", 22, false);
            addInfo("Blue", 22, false);
            addInfo("Darkroom", 21, true);
            addInfo("Money Twins", 22, false);
            addInfo("Seven", 23, false);
            addInfo("Foggy", 22, false);
            addInfo("Sharp", 22, true);
            addInfo("Sharp Plus", 23, true);
            addInfo("Caramel", 22, false);
            addInfo("Coffee", 22, false);
            addInfo("Dark Side", 22, true);
            addInfo("Glass Oceans", 22, false);
            addInfo("iMaginary", 23, false);
            addInfo("Lilian", 22, false);
            addInfo("Liquid Sky", 22, false);
            addInfo("London Liquid Sky", 22, false);
            addInfo("Stardust", 22, false);
            addInfo("The Asphalt World", 22, false);

            addInfo("HELIOS Nephrite Black", 37, false);
            addInfo("HELIOS Nephrite Colorful", 37, false);

            return result;

            void addInfo(string skinName, int tabHeaderHeight, bool hasTwoDarkState)
            {
                result.Add(skinName, new SkinStaticInfo(skinName, tabHeaderHeight, hasTwoDarkState));
            }
        }
        #endregion
    }
    #endregion
    #region Enumy
    /// <summary>
    /// Typ systémového zvuku.
    /// Konkrétní zvuky jsou dané zvoleným tématem Windows. U mě osobně jsou zvuky Asterisk a Beep a Exclamation a Question stejné, odlišný je Hand.
    /// </summary>
    public enum SystemSoundType
    {
        /// <summary>
        /// Není definován
        /// </summary>
        None,
        /// <summary>
        /// Asterisk
        /// </summary>
        Asterisk,
        /// <summary>
        /// Beep
        /// </summary>
        Beep,
        /// <summary>
        /// Exclamation
        /// </summary>
        Exclamation,
        /// <summary>
        /// Hand
        /// </summary>
        Hand,
        /// <summary>
        /// Question
        /// </summary>
        Question
    }
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
    /// Určuje, z jakého typu zdroje může určitý cíl akceptovat data.
    /// <para/>
    /// Máme k dispozici výměnu dat, realizovanou jednak pomocí Clipboardu a druhak pomocí DragAndDrop procesu. 
    /// Oba způsoby mohou teoreticky vzít libovolná data z jedné této aplikace a přemístit je do jiné aplikace, 
    /// anebo je přemístit v rámci jedné aplikace mezi různými okny a controly.
    /// Potřebujeme ale řídit, zda do cílového controlu (ListBox, TreeList, Grid, DataForm, atd) lze nebo nelze vložit data z libovolného zdroje.
    /// <para/>
    /// Zdroj dat (určitý zdrojový control) sestavuje balík konkrétních dat určitého typu (řádky ListBoxu, nody TreeView, RecordId v Gridu atd),
    /// a tento balík vkládá (pomocí Ctrl+C do clipboardu nebo při zahájení DragAndDrop) do new instance třídy <see cref="DataExchangeContainer"/>.
    /// Při tom je do <see cref="DataExchangeContainer"/> vepsána informace o zdroji (konkrétní aplikace - ID procesu) a ID zdrojového controlu.
    /// Toto ID je controlu přiděleno při jeho vytvoření, typicky jde o ID stránky a ID controlu.
    /// <para/>
    /// Cíl dat, tedy control, kam jsou data vkládána (Ctrl+V nebo Drop při DragAndDrop) si pak zjistí, zda existují Exchange data,
    /// a následně se optá, zda tato data může do sebe akceptovat.
    /// K tomu slouží metoda <see cref="DxComponent.CanAcceptExchangeData(DataExchangeContainer, string, DataExchangeCrossType, string)"/>.
    /// Metodě předává získaná Exchange data a parametry deklarované v cílovém controlu - které slouží pro volbu akceptování dat z Exchange.
    /// </summary>
    [Flags]
    public enum DataExchangeCrossType
    {
        /// <summary>
        /// Nezadáno, výchozí stav, control neůže akceptovat žádná vstupující data, ani svoje vlastní.
        /// </summary>
        None = 0,
        /// <summary>
        /// Lze akceptovat data pocházející z vlastního controlu
        /// </summary>
        OwnControl = 0x0001,
        /// <summary>
        /// Lze akceptovat data pocházející z jiných controlů, jejich výběr (DataSourceId) je uveden v parametru 'enabledSources' 
        /// metody <see cref="DxComponent.CanAcceptExchangeData(DataExchangeContainer, string, DataExchangeCrossType, string)"/>
        /// </summary>
        OtherSelectedControls = 0x0002,
        /// <summary>
        /// Lze akceptovat data pocházející z kteréhokoli jiného controlů, bez omezení;
        /// ale akceptování zdroje dat z vlastního controlu stále ovládá příznak <see cref="OwnControl"/>;
        /// </summary>
        AnyOtherControls = 0x0004,

        /// <summary>
        /// Lze akceptovat data pocházející z aktuální aplikace.
        /// Pozor, toto NENÍ default: tuto volbu je třeba explicitně nastavit (anebo vybrat volbu kombinovanou, která už tuto hodnotu obsahuje).
        /// Pokud nebude nastaven tento příznak, nebudou akceptovány zdroje z aktuální aplikace - a to ani zdroj <see cref="OwnControl"/>!
        /// </summary>
        CurrentApplication = 0x0010,
        /// <summary>
        /// Lze akceptovat data pocházející z jakékoli jiné aplikace;
        /// ale akceptování zdroje dat z aktuální aplikace stále ovládá příznak <see cref="CurrentApplication"/>;
        /// </summary>
        AnyOtherApplications = 0x0020,

        /// <summary>
        /// Pouze data z vlastního controlu a vlastní aplikace. 
        /// Pak není nutno určovat povolené zdrojové controly.
        /// </summary>
        OwnControlOnly = OwnControl | CurrentApplication,
        /// <summary>
        /// Data z jakéhokoli jiného controlu (kromě vlastního controlu) z aktuální aplikace
        /// </summary>
        AnyOtherControlsInCurrentApplication = AnyOtherControls | CurrentApplication,
        /// <summary>
        /// Data z jakéhokoli controlu (včetně vlastního controlu) z aktuální aplikace
        /// </summary>
        AllControlsInCurrentApplication = OwnControl | AnyOtherControls | CurrentApplication,
    }
    /// <summary>
    /// Názvy barev, skinů a elementů, pro které lze získat barvy pomocí metody <see cref="DxComponent.GetSkinColor(string)"/>
    /// </summary>
    public class SkinElementColor
    {
        /// <summary>Jméno celého skinu</summary>
        public static string Control { get { return "Control."; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string Control_LabelForeColor { get { return Control + "LabelForeColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string Control_TextBoxForeColor { get { return Control + "TextBoxForeColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string Control_TextBoxBackColor { get { return Control + "TextBoxBackColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string Control_PanelBackColor { get { return Control + "PanelBackColor"; } }

        /// <summary>Jméno celého skinu</summary>
        public static string CommonSkins { get { return "CommonSkins."; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_WindowText { get { return CommonSkins + "WindowText"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_ReadOnly { get { return CommonSkins + "ReadOnly"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_Info { get { return CommonSkins + "Info"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_Success { get { return CommonSkins + "Success"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_HotTrackedForeColor { get { return CommonSkins + "HotTrackedForeColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_Danger { get { return CommonSkins + "Danger"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_Control { get { return CommonSkins + "Control"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_DisabledText { get { return CommonSkins + "DisabledText"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_Highlight { get { return CommonSkins + "Highlight"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_Question { get { return CommonSkins + "Question"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_Primary { get { return CommonSkins + "Primary"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_HighlightAlternate { get { return CommonSkins + "HighlightAlternate"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_WarningFill { get { return CommonSkins + "WarningFill"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_InfoText { get { return CommonSkins + "InfoText"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_HotTrackedColor { get { return CommonSkins + "HotTrackedColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_DisabledControl { get { return CommonSkins + "DisabledControl"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_Information { get { return CommonSkins + "Information"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_HighlightText { get { return CommonSkins + "HighlightText"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_ControlText { get { return CommonSkins + "ControlText"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_QuestionFill { get { return CommonSkins + "QuestionFill"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_Warning { get { return CommonSkins + "Warning"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_InactiveCaptionText { get { return CommonSkins + "InactiveCaptionText"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_Window { get { return CommonSkins + "Window"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_HideSelection { get { return CommonSkins + "HideSelection"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_Menu { get { return CommonSkins + "Menu"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_MenuText { get { return CommonSkins + "MenuText"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string CommonSkins_Critical { get { return CommonSkins + "Critical"; } }

        /// <summary>Jméno celého skinu</summary>
        public static string EditorsSkins { get { return "EditorsSkins."; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_ProgressBarEmptyTextColor { get { return EditorsSkins + "ProgressBarEmptyTextColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_FluentCalendarWeekDayForeColor { get { return EditorsSkins + "FluentCalendarWeekDayForeColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_CalendarSelectedCellColor { get { return EditorsSkins + "CalendarSelectedCellColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_FilterControlValueTextColor { get { return EditorsSkins + "FilterControlValueTextColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_FilterControlGroupOperatorTextColor { get { return EditorsSkins + "FilterControlGroupOperatorTextColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_BeakFormBorderColor { get { return EditorsSkins + "BeakFormBorderColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_HyperLinkTextColor { get { return EditorsSkins + "HyperLinkTextColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_FilterControlEmptyValueTextColor { get { return EditorsSkins + "FilterControlEmptyValueTextColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_FluentCalendarSeparatorColor { get { return EditorsSkins + "FluentCalendarSeparatorColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_ProgressBarFilledTextColor { get { return EditorsSkins + "ProgressBarFilledTextColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_FluentCalendarBackColor { get { return EditorsSkins + "FluentCalendarBackColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_FluentCalendarWeekNumberForeColor { get { return EditorsSkins + "FluentCalendarWeekNumberForeColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_FilterControlFieldNameTextColor { get { return EditorsSkins + "FilterControlFieldNameTextColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_CalcEditOperationTextColor { get { return EditorsSkins + "CalcEditOperationTextColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_FilterControlOperatorTextColor { get { return EditorsSkins + "FilterControlOperatorTextColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_FluentCalendarHolidayCellColor { get { return EditorsSkins + "FluentCalendarHolidayCellColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_CalcEditDigitTextColor { get { return EditorsSkins + "CalcEditDigitTextColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_CalendarTodayCellColor { get { return EditorsSkins + "CalendarTodayCellColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_CalendarInactiveCellColor { get { return EditorsSkins + "CalendarInactiveCellColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_CalendarNormalCellColor { get { return EditorsSkins + "CalendarNormalCellColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string EditorsSkins_CalendarHolidayCellColor { get { return EditorsSkins + "CalendarHolidayCellColor"; } }

        /// <summary>Jméno celého skinu</summary>
        public static string BarSkins { get { return "BarSkins."; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string BarSkins_ColorLinkDisabledForeColor { get { return BarSkins + "ColorLinkDisabledForeColor"; } }

        /// <summary>Jméno celého skinu</summary>
        public static string ChartSkins { get { return "ChartSkins."; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string ChartSkins_ColorLine3DMarker { get { return ChartSkins + "ColorLine3DMarker"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string ChartSkins_ColorConstantLineTitle { get { return ChartSkins + "ColorConstantLineTitle"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string ChartSkins_ColorArea3DMarker { get { return ChartSkins + "ColorArea3DMarker"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string ChartSkins_ColorConstantLine { get { return ChartSkins + "ColorConstantLine"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string ChartSkins_ColorChartTitle { get { return ChartSkins + "ColorChartTitle"; } }

        /// <summary>Jméno celého skinu</summary>
        public static string DashboardSkins { get { return "DashboardSkins."; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string DashboardSkins_ChartPaneRemoveButton { get { return DashboardSkins + "ChartPaneRemoveButton"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string DashboardSkins_BarAxisColor { get { return DashboardSkins + "BarAxisColor"; } }

        /// <summary>Jméno celého skinu</summary>
        public static string DockingSkins { get { return "DockingSkins."; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string DockingSkins_DocumentGroupHeaderTextColor { get { return DockingSkins + "DocumentGroupHeaderTextColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string DockingSkins_DocumentGroupHeaderTextColorDisabled { get { return DockingSkins + "DocumentGroupHeaderTextColorDisabled"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string DockingSkins_DocumentGroupHeaderTextColorHot { get { return DockingSkins + "DocumentGroupHeaderTextColorHot"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string DockingSkins_TabHeaderTextColorActive { get { return DockingSkins + "TabHeaderTextColorActive"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string DockingSkins_TabHeaderTextColorDisabled { get { return DockingSkins + "TabHeaderTextColorDisabled"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string DockingSkins_TabHeaderTextColorHot { get { return DockingSkins + "TabHeaderTextColorHot"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string DockingSkins_DocumentGroupHeaderTextColorActive { get { return DockingSkins + "DocumentGroupHeaderTextColorActive"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string DockingSkins_TabHeaderTextColor { get { return DockingSkins + "TabHeaderTextColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string DockingSkins_DocumentGroupHeaderTextColorGroupInactive { get { return DockingSkins + "DocumentGroupHeaderTextColorGroupInactive"; } }

        /// <summary>Jméno celého skinu</summary>
        public static string FormSkins { get { return "FormSkins."; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string FormSkins_TextShadowColor { get { return FormSkins + "TextShadowColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string FormSkins_InactiveColor { get { return FormSkins + "InactiveColor"; } }

        /// <summary>Jméno celého skinu</summary>
        public static string RibbonSkins { get { return "RibbonSkins."; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string RibbonSkins_ForeColorDisabledInCaptionQuickAccessToolbar { get { return RibbonSkins + "ForeColorDisabledInCaptionQuickAccessToolbar"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string RibbonSkins_ForeColorDisabledInBottomQuickAccessToolbar { get { return RibbonSkins + "ForeColorDisabledInBottomQuickAccessToolbar"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string RibbonSkins_ForeColorDisabledInCaptionQuickAccessToolbarInActive2010 { get { return RibbonSkins + "ForeColorDisabledInCaptionQuickAccessToolbarInActive2010"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string RibbonSkins_ButtonDisabled { get { return RibbonSkins + "ButtonDisabled"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string RibbonSkins_ForeColorInBackstageViewTitle { get { return RibbonSkins + "ForeColorInBackstageViewTitle"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string RibbonSkins_ForeColorDisabledInTopQuickAccessToolbar { get { return RibbonSkins + "ForeColorDisabledInTopQuickAccessToolbar"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string RibbonSkins_ForeColorDisabledInCaptionQuickAccessToolbar2010 { get { return RibbonSkins + "ForeColorDisabledInCaptionQuickAccessToolbar2010"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string RibbonSkins_EditorBackground { get { return RibbonSkins + "EditorBackground"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string RibbonSkins_RadialMenuColor { get { return RibbonSkins + "RadialMenuColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string RibbonSkins_ForeColorDisabledInPageHeader { get { return RibbonSkins + "ForeColorDisabledInPageHeader"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string RibbonSkins_ForeColorInCaptionQuickAccessToolbar2010 { get { return RibbonSkins + "ForeColorInCaptionQuickAccessToolbar2010"; } }

        /// <summary>Jméno celého skinu</summary>
        public static string TabSkins { get { return "TabSkins."; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string TabSkins_TabHeaderTextColorActive { get { return TabSkins + "TabHeaderTextColorActive"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string TabSkins_TabHeaderButtonTextColorHot { get { return TabSkins + "TabHeaderButtonTextColorHot"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string TabSkins_TabHeaderButtonTextColor { get { return TabSkins + "TabHeaderButtonTextColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string TabSkins_TabHeaderTextColorDisabled { get { return TabSkins + "TabHeaderTextColorDisabled"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string TabSkins_TabHeaderTextColor { get { return TabSkins + "TabHeaderTextColor"; } }
        /// <summary>Jméno konkrétní barvy konkrétního skinu</summary>
        public static string TabSkins_TabHeaderTextColorHot { get { return TabSkins + "TabHeaderTextColorHot"; } }

        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorActiveBorder { get { return SystemColors.ActiveBorder.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorActiveCaption { get { return SystemColors.ActiveCaption.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorActiveCaptionText { get { return SystemColors.ActiveCaptionText.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorAppWorkspace { get { return SystemColors.AppWorkspace.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorButtonFace { get { return SystemColors.ButtonFace.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorButtonHighlight { get { return SystemColors.ButtonHighlight.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorButtonShadow { get { return SystemColors.ButtonShadow.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorControl { get { return SystemColors.Control.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorControlDark { get { return SystemColors.ControlDark.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorControlDarkDark { get { return SystemColors.ControlDarkDark.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorControlLight { get { return SystemColors.ControlLight.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorControlLightLight { get { return SystemColors.ControlLightLight.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorControlText { get { return SystemColors.ControlText.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorDesktop { get { return SystemColors.Desktop.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorGradientActiveCaption { get { return SystemColors.GradientActiveCaption.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorGradientInactiveCaption { get { return SystemColors.GradientInactiveCaption.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorGrayText { get { return SystemColors.GrayText.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorHighlight { get { return SystemColors.Highlight.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorHighlightText { get { return SystemColors.HighlightText.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorHotTrack { get { return SystemColors.HotTrack.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorInactiveBorder { get { return SystemColors.InactiveBorder.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorInactiveCaption { get { return SystemColors.InactiveCaption.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorInactiveCaptionText { get { return SystemColors.InactiveCaptionText.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorInfo { get { return SystemColors.Info.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorInfoText { get { return SystemColors.InfoText.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorMenu { get { return SystemColors.Menu.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorMenuBar { get { return SystemColors.MenuBar.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorMenuHighlight { get { return SystemColors.MenuHighlight.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorMenuText { get { return SystemColors.MenuText.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorScrollBar { get { return SystemColors.ScrollBar.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorWindow { get { return SystemColors.Window.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorWindowFrame { get { return SystemColors.WindowFrame.Name; } }
        /// <summary>Jméno konkrétní systémové barvy</summary>
        public static string SystemColorWindowText { get { return SystemColors.WindowText.Name; } }
    }
    /// <summary>
    /// Druhy aktivit, které lze logovat
    /// </summary>
    [Flags]
    public enum LogActivityKind : Int64
    {
        /// <summary>
        /// Nic
        /// </summary>
        None = 0x00000000,
        /// <summary>
        /// Zprávy od WinApi
        /// </summary>
        WinMessage = 0x00000001,
        /// <summary>
        /// Zprávy od DevExpress komponent
        /// </summary>
        DevExpressEvents = 0x00000002,
        /// <summary>
        /// Zprávy od interaktivních přesunů prvků
        /// </summary>
        InteractiveMove = 0x00000010,
        /// <summary>
        /// Zprávy od interaktivních clicků
        /// </summary>
        InteractiveClick = 0x00000020,
        /// <summary>
        /// Zprávy od interaktivních DragAndDrop
        /// </summary>
        InteractiveDrag = 0x00000040,
        /// <summary>
        /// Zprávy o Scrool obsahu
        /// </summary>
        Scroll = 0x00000100,
        /// <summary>
        /// Zprávy o změnách virtuálních containerů
        /// </summary>
        VirtualChanges = 0x00000200,
        /// <summary>
        /// Zprávy o Paint v DataFormu
        /// </summary>
        Paint = 0x00000800,
        /// <summary>
        /// Zprávy o práci s Repository v DataFormu
        /// </summary>
        DataFormRepository = 0x00001000,
        /// <summary>
        /// Zprávy o událostech v DataFormu
        /// </summary>
        DataFormEvents = 0x00002000,
        /// <summary>
        /// Zprávy o událostech v Ribbonu
        /// </summary>
        Ribbon = 0x00004000,
        /// <summary>
        /// Zprávy o událostech v LayoutPanelu
        /// </summary>
        LayoutPanel = 0x00008000,
        /// <summary>
        /// Zprávy o událostech v DocumentManageru
        /// </summary>
        DocumentManager = 0x00010000,
        /// <summary>
        /// Zprávy o událostech v ThreadManageru
        /// </summary>
        ThreadManager = 0x00020000,

        /// <summary>
        /// Default
        /// </summary>
        Default = Scroll | VirtualChanges | DataFormRepository | DataFormEvents | Ribbon | LayoutPanel | DocumentManager,
        /// <summary>
        /// Vše
        /// </summary>
        All = 0x7FFFFFFF
    }
    #endregion
    #endregion
    #region enum MsgCode : kódy textů pro lokalizaci
    /// <summary>
    /// Knihovna standardních hlášek k lokalizaci
    /// </summary>
    public enum MsgCode
    {
        /// <summary>Žádná hláška / nedefinovaná</summary>
        None = 0,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("SYSTÉM")]
        RibbonAppHomeText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Systémový prvek, nelze jej odebrat")]
        RibbonDirectQatItem,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Tento prvek není možno přidat na panel nástrojů Rychlý přístup")]
        RibbonDisabledForQatItem,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Přidat na panel nástrojů Rychlý přístup")]
        RibbonAddToQat,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Odebrat z panelu nástrojů Rychlý přístup")]
        RibbonRemoveFromQat,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Zobrazit panel nástrojů Rychlý přístup nad pásem karet")]
        RibbonShowQatTop,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Zobrazit panel nástrojů Rychlý přístup pod pásem karet")]
        RibbonShowQatDown,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Upravit obsah panelu nástrojů Rychlý přístup")]
        RibbonShowManager,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Zmenšit")]
        RibbonMinimizeQat,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Nastavení oblíbených položek")]
        RibbonQatManagerTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Výsledky hledání")]
        RibbonSearchMenuGroupGeneralCaption,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Ostatní")]
        RibbonSearchMenuGroupOtherCaption,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Příliš mnoho výsledků")]
        RibbonSearchMenuGroupTooManyMatchesCaption,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Upřesněte hledaný text")]
        RibbonSearchMenuItemTooManyMatchesCaption,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Žádný výsledek")]
        RibbonSearchMenuGroupNoMatchesCaption,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Upřesněte hledaný text")]
        RibbonSearchMenuItemNoMatchesCaption,

        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Chyba")]
        DialogFormTitleError,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Došlo k chybě")]
        DialogFormTitlePrefix,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Zkopírovat (Ctrl+C)")]
        DialogFormCtrlCText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Ctrl+C: zkopíruje všechny informace z okna do schránky Windows")]
        DialogFormCtrlCToolTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Text zkopírován")]
        DialogFormCtrlCInfo,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Více informací")]
        DialogFormAltMsgButtonText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Zobrazí větší okno s více informacemi")]
        DialogFormAltMsgButtonToolTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Méně informací")]
        DialogFormStdMsgButtonText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Zobrazí základní okno s méně informacemi")]
        DialogFormStdMsgButtonToolTip,

        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("prefix")]
        DialogFormResultPrefix,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("OK")]
        DialogFormResultOk,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Ano")]
        DialogFormResultYes,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Ne")]
        DialogFormResultNo,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Zrušit")]
        DialogFormResultAbort,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Znovu")]
        DialogFormResultRetry,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Ignoruj")]
        DialogFormResultIgnore,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Storno")]
        DialogFormResultCancel,

        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Orientace")]
        LayoutPanelContextMenuTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Vedle sebe")]
        LayoutPanelHorizontalText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Panely vlevo a vpravo, oddělovač je svislý")]
        LayoutPanelHorizontalToolTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Pod sebou")]
        LayoutPanelVerticalText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Panely nahoře a dole, oddělovač je vodorovný")]
        LayoutPanelVerticalToolTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Zavřít")]
        MenuCloseText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Zavře nabídku bez provedení akce")]
        MenuCloseToolTip,

        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Smazat")]
        DxFilterBoxClearTipTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Zruší zadaný filtr")]
        DxFilterBoxClearTipText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Hledat")]
        DxFilterBoxNullValuePrompt,

        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Obsahuje")]
        DxFilterOperatorContainsText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Vybere ty položky, které obsahují zadaný text")]
        DxFilterOperatorContainsTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Neobsahuje")]
        DxFilterOperatorDoesNotContainText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Vybere ty položky, které neobsahují zadaný text")]
        DxFilterOperatorDoesNotContainTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Začíná")]
        DxFilterOperatorStartWithText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Vybere ty položky, jejichž text začíná zadaným textem")]
        DxFilterOperatorStartWithTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Nezačíná")]
        DxFilterOperatorDoesNotStartWithText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Vybere ty položky, jejichž text začíná jinak, než je zadáno")]
        DxFilterOperatorDoesNotStartWithTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Končí")]
        DxFilterOperatorEndWithText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Vybere ty položky, jejichž text končí zadaným textem")]
        DxFilterOperatorEndWithTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Nekončí")]
        DxFilterOperatorDoesNotEndWithText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Vybere ty položky, jejichž text končí jinak, než je zadáno")]
        DxFilterOperatorDoesNotEndWithTip,

        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Podobá se")]
        DxFilterOperatorLikeText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("?")]
        DxFilterOperatorLikeTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Nepodobá se")]
        DxFilterOperatorNotLikeText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("?")]
        DxFilterOperatorNotLikeTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Odpovídá")]
        DxFilterOperatorMatchText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("?")]
        DxFilterOperatorMatchTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Neodpovídá")]
        DxFilterOperatorDoesNotMatchText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("?")]
        DxFilterOperatorDoesNotMatchTip,

        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Menší než")]
        DxFilterOperatorLessThanText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Hodnoty menší než zadaná hodnota")]
        DxFilterOperatorLessThanTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Menší nebo rovno")]
        DxFilterOperatorLessThanOrEqualToText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Hodnoty menší nebo rovny jako zadaná hodnota")]
        DxFilterOperatorLessThanOrEqualToTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Rovno")]
        DxFilterOperatorEqualsText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Hodnoty rovné dané hodnotě")]
        DxFilterOperatorEqualsTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Nerovno")]
        DxFilterOperatorNotEqualsText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Hodnoty jiné než je daná hodnota")]
        DxFilterOperatorNotEqualsTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Větší nebo rovno")]
        DxFilterOperatorGreaterThanOrEqualToText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Hodnoty větší nebo rovny jako zadaná hodnota")]
        DxFilterOperatorGreaterThanOrEqualToTip,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Větší než")]
        DxFilterOperatorGreaterThanText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Hodnoty větší než zadaná hodnota")]
        DxFilterOperatorGreaterThanTip,

        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Alt+Home")]
        DxKeyActionMoveTopTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Přesunout na začátek")]
        DxKeyActionMoveTopText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Alt+Nahoru")]
        DxKeyActionMoveUpTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Přesunout o řádek nahoru")]
        DxKeyActionMoveUpText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Alt+Dolů")]
        DxKeyActionMoveDownTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Přesunout o řádek dolů")]
        DxKeyActionMoveDownText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Alt+End")]
        DxKeyActionMoveBottomTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Přesunout na konec")]
        DxKeyActionMoveBottomText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Ctrl+R")]
        DxKeyActionRefreshTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Aktualizovat")]
        DxKeyActionRefreshText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Ctrl+A")]
        DxKeyActionSelectAllTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Označit vše")]
        DxKeyActionSelectAllText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Ctrl+C")]
        DxKeyActionClipCopyTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Zkopírovat do schránky")]
        DxKeyActionClipCopyText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Ctrl+X")]
        DxKeyActionClipCutTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Přenést do schránky")]
        DxKeyActionClipCutText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Ctrl+V")]
        DxKeyActionClipPasteTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Vložit ze schránky")]
        DxKeyActionClipPasteText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Delete")]
        DxKeyActionDeleteTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Smazat")]
        DxKeyActionDeleteText,

        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Ctrl+Y")]
        DxKeyActionUndoTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("O krok zpět v editaci")]
        DxKeyActionUndoText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Ctrl+Z")]
        DxKeyActionRedoTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("O krok vpřed. Znovu provede krok editace, který byl zrušen krokem zpět")]
        DxKeyActionRedoText,


        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Vytvořte nový graf...")]
        DxChartEditorTitleWizard,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Upravte graf...")]
        DxChartEditorTitleDesigner,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("V tuto chvíli nelze editovat graf, dosud není načten nebo není definován.")]
        DxChartEditorNotPrepared,

        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("V seznamu (TreeList) není možno zobrazit ikonu %0, protože je typu %1, ale seznam očekává ikony typu %2.")]
        TreeListImageTypeMismatch,

        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Systém HELIOS")]
        ApplicationName,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Obsah buňky ze sloupce «%0» vložen do schránky.")]
        CopyBrowseCell,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Rok může být pouze číslo v rozmezí od 1753 do 9999.")]
        TxtCalendarYearOutOfRange,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Zadané datum od je větší než datum do.")]
        TxtCalendarStartDateIsHigherThanEndDate,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Nesprávně zadané datum.")]
        TxtCalendarDateNotValid,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Zadané hodnota od je větší než hodnota do.")]
        TxtNumberFirstNumberIsHigherThanSecond,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Nesprávně zadané číslo.")]
        TxtNumberValueNotValid,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Pro zadanou skupinu ještě nejsou načtena všechna data.")]
        ClientBrowse_Grouping_DataNotLoaded,

        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Otevřít v prohlížeči")]
        DxMapOpenExternalBrowserTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Aktuální mapu otevře v internetovém prohlížeči. Změny mapy v prohlížeči nelze načíst zpět do formuláře.")]
        DxMapOpenExternalBrowserText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Najdi adresu")]
        DxMapSearchCoordinatesTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Získá adresu z formuláře a vyhledá ji na mapě.")]
        DxMapSearchCoordinatesText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Souřadnice")]
        DxMapCoordinatesTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Souřadnice zadané do formuláře. Tlačítky po stranách lze změnit jejich formát. Tlačítko vpravo znovu vyhledá zadané souřadnice.")]
        DxMapCoordinatesText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Změna formátu zobrazené souřadnice")]
        DxMapCoordinatesButtonTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Změní formát zobrazené souřadnice, ale nezmění její hodnotu (pozici na mapě).")]
        DxMapCoordinatesButtonText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Ukaž na mapě")]
        DxMapReloadMapTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Nastaví mapu tak, aby znovu zobrazila zadanou souřadnici. Použijte po posunutí mapy nebo změně označené souřadnice, pro návrat na zadanou souřadnici.")]
        DxMapReloadMapText,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Převezmi z mapy")]
        DxMapAcceptCoordinatesTitle,
        /// <summary>Název a text konkrétní hlášky k lokalizaci</summary>
        [DefaultMessageText("Pozici (bod) aktuálně označenou na mapě převezme do formuláře. Změní tedy souřadnici aktuálního záznamu.")]
        DxMapAcceptCoordinatesText


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
    /// <summary>
    /// Interface pro listener události Změna hodnoty <see cref="DxComponent.ExcludeFromCaptureContent"/>.
    /// <para/>
    /// Listenerem je typicky Formulář, který na základě této hodnoty nastaví svůj příznak <c>WinApi.SetWindowDisplayAffinity</c>.<br/>
    /// Takový formulář si uvedený příznak podle této hodnoty nastavuje i v konstruktoru (=tím platí pro nově vytvořená okna), a pomocí Listeneru si tento příznak změní i za svého života.
    /// </summary>
    public interface IListenerExcludeFromCaptureContentChanged : IListener
    {
        /// <summary>
        /// Metoda je volaná v situaci, kdy došlo ke změně hodnoty <see cref="DxComponent.ExcludeFromCaptureContent"/>.
        /// </summary>
        void ExcludeFromCaptureContentChanged();
    }
    #endregion
    #region class SystemAdapter - přístupový bod k adapteru na aktuální aplikační systém
    /// <summary>
    /// Tato třída reprezentuje adapter na systémové metody aktuálního aplikačního systému.
    /// Prvky této třídy jsou volány z různých míst komponent AsolDX a jejich úkolem je převolat odpovídající metody konkrétního systému.
    /// K tomu účelu si zdejší třída vytvoří interní instanci konkrétního systému (zde NephriteAdapter / TestAdapter).
    /// <para/>
    /// Komponenty AsolDX tedy volají statické metody této třídy <see cref="SystemAdapter"/>, 
    /// ty potom uvnitř převolají odpovídající metody, které jsou fyzicky realizované 
    /// v konkrétním adapteru implementujícím <see cref="ISystemAdapter"/>.
    /// </summary>
    internal static class SystemAdapter
    {
        #region Zásuvný modul pro konkrétní systém = instance implementující ISystemAdapter
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
        /// Vrátí definici daného stylu
        /// </summary>
        /// <param name="styleName"></param>
        /// <param name="exactAttributeColor"></param>
        /// <returns></returns>
        public static StyleInfo GetStyleInfo(string styleName, Color? exactAttributeColor) { return Current.GetStyleInfo(styleName, exactAttributeColor); }
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
        /// Adresář na lokálním klientu, kam může aplikace volně zapisovat User data, aniž by byly často smazány. Včetně DLL souborů.
        /// </summary>
        public static string LocalUserDataPath { get { return Current.LocalUserDataPath; } }
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
        /// <summary>
        /// Zapíše do trace dané informace
        /// </summary>
        /// <param name="level"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="keyword"></param>
        /// <param name="arguments"></param>
        internal static void TraceText(TraceLevel level, Type type, string method, string keyword, params object[] arguments) { Current.TraceText(level, type, method, keyword, arguments); }
        /// <summary>
        /// Zapíše do trace dané informace a vrátí using blok, který na svém konci v Dispose zapíše konec bloku (Begin - End)
        /// </summary>
        /// <param name="level"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="keyword"></param>
        /// <param name="arguments"></param>
        internal static IDisposable TraceTextScope(TraceLevel level, Type type, string method, string keyword, params object[] arguments) { return Current.TraceTextScope(level, type, method, keyword, arguments); }
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
        /// Vrátí definici daného stylu
        /// </summary>
        /// <param name="styleName"></param>
        /// <param name="exactAttributeColor"></param>
        /// <returns></returns>
        StyleInfo GetStyleInfo(string styleName, Color? exactAttributeColor);
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
        /// Adresář na lokálním klientu, kam může aplikace volně zapisovat User data, aniž by byly často smazány. Včetně DLL souborů.
        /// </summary>
        string LocalUserDataPath { get; }
        /// <summary>
        /// Vrací klávesovou zkratku pro daný string, typicky na vstupu je "Ctrl+C", na výstupu je <see cref="System.Windows.Forms.Keys.Control"/> | <see cref="System.Windows.Forms.Keys.C"/>
        /// </summary>
        /// <param name="shortCut"></param>
        /// <returns></returns>
        Shortcut GetShortcutKeys(string shortCut);
        /// <summary>
        /// Zapíše do trace dané informace
        /// </summary>
        /// <param name="level"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="keyword"></param>
        /// <param name="arguments"></param>
        void TraceText(TraceLevel level, Type type, string method, string keyword, params object[] arguments);
        /// <summary>
        /// Zapíše do trace dané informace a vrátí using blok, který na svém konci v Dispose zapíše konec bloku (Begin - End)
        /// </summary>
        /// <param name="level"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="keyword"></param>
        /// <param name="arguments"></param>
        IDisposable TraceTextScope(TraceLevel level, Type type, string method, string keyword, params object[] arguments);
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
        Large = 3,
        /// <summary>Originální velikost, pouze pro získání bitmapy - nikoli pro ImageListy.</summary>
        Original
    }
    /// <summary>
    /// Druh obsahu resource
    /// </summary>
    [Flags]
    public enum ResourceContentType
    {
        /// <summary>
        /// Nejde o platný Resource (neznámá přípona)
        /// </summary>
        None = 0,
        /// <summary>
        /// Vektor (SVG)
        /// </summary>
        Vector = 0x0001,
        /// <summary>
        /// Bitmapa (BMP, JPG, PNG, PCX, GIF, TIF)
        /// </summary>
        Bitmap = 0x0002,
        /// <summary>
        /// Ikona (ICO)
        /// </summary>
        Icon = 0x0004,
        /// <summary>
        /// Kurzor (CUR)
        /// </summary>
        Cursor = 0x0008,
        /// <summary>
        /// Audio (MP3, WAV, FLAC)
        /// </summary>
        Audio = 0x0010,
        /// <summary>
        /// Video (MP4, AVI, MPEG)
        /// </summary>
        Video = 0x0020,
        /// <summary>
        /// Txt
        /// </summary>
        Txt = 0x0100,
        /// <summary>
        /// Xml (XML, HTML)
        /// </summary>
        Xml = 0x0200,

        /// <summary>
        /// Základní obrázky (<see cref="Bitmap"/> a <see cref="Vector"/>).
        /// Prioritu obrázků určuje hodnota <see cref="DxComponent.IsPreferredVectorImage"/>.
        /// </summary>
        BasicImage = Bitmap | Vector,
        /// <summary>
        /// Běžně konvertibilní obrázky (mimo <see cref="Cursor"/>)
        /// </summary>
        StandardImage = Bitmap | Vector | Icon,
        /// <summary>
        /// Všechny obrázky (včetně <see cref="Cursor"/>)
        /// </summary>
        AnyImage = Bitmap | Vector | Icon | Cursor,
        /// <summary>
        /// Jakákoli multimedia
        /// </summary>
        AnyMultimedia = Audio | Video,
        /// <summary>
        /// Jakýkoli text
        /// </summary>
        AnyText = Txt | Xml,
        /// <summary>
        /// Cokoli
        /// </summary>
        All = AnyImage | AnyMultimedia | AnyText
    }
    /// <summary>
    /// Level zápisu do Trace
    /// </summary>
    public enum TraceLevel
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Informace
        /// </summary>
        Info,
        /// <summary>
        /// Varování
        /// </summary>
        Warning,
        /// <summary>
        /// Chyba
        /// </summary>
        Error,
        /// <summary>
        /// Systémová chyba
        /// </summary>
        SysError
    }
    /// <summary>
    /// Definice vizuálního stylu daná názvem (kalíšek)
    /// </summary>
    public class StyleInfo
    {
        /// <summary>
        /// Konstruktor s daty
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isForDarkTheme"></param>
        /// <param name="attributeFontStyle"></param>
        /// <param name="attributeFontFamily"></param>
        /// <param name="attributeFontSize"></param>
        /// <param name="attributeBgColor"></param>
        /// <param name="attributeColor"></param>
        /// <param name="labelFontStyle"></param>
        /// <param name="labelFontFamily"></param>
        /// <param name="labelFontSize"></param>
        /// <param name="labelBgColor"></param>
        /// <param name="labelColor"></param>
        public StyleInfo(string name, bool? isForDarkTheme, 
            FontStyle? attributeFontStyle =null, string attributeFontFamily = "", float? attributeFontSize = null, Color? attributeBgColor = null, Color? attributeColor = null,
            FontStyle? labelFontStyle = null, string labelFontFamily = "", float? labelFontSize = null, Color? labelBgColor = null, Color? labelColor = null)
        {
            Name = name;
            IsForDarkTheme = isForDarkTheme;

            AttributeFontStyle = attributeFontStyle;
            AttributeFontFamily = attributeFontFamily;
            AttributeFontSize = attributeFontSize;
            AttributeBgColor = attributeBgColor;
            AttributeColor = attributeColor;

            LabelFontStyle = labelFontStyle;
            LabelFontFamily = labelFontFamily;
            LabelFontSize = labelFontSize;
            LabelBgColor = labelBgColor;
            LabelColor = labelColor;
        }
        /// <summary>
        /// Jméno stylu
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Určeno pro tmavý skin. Pokud je null, tak je pro oba režimy.
        /// </summary>
        public bool? IsForDarkTheme { get; private set; }

        /// <summary>
        /// FontStyle: AttrBold + AttrUnderline + AttrItalic
        /// </summary>
        public FontStyle? AttributeFontStyle { get; private set; }
        /// <summary>
        /// String: AttrFontFamily 
        /// </summary>
        public string AttributeFontFamily { get; private set; }
        /// <summary>
        /// Float: AttrFontSize
        /// </summary>
        public float? AttributeFontSize { get; private set; }
        /// <summary>
        /// Barva pozadí controlu (AttrBgColor / AttrBgColorDark)
        /// </summary>
        public Color? AttributeBgColor { get; private set; }
        /// <summary>
        /// Barva písma controlu (AttrColor / AttrColorDark)
        /// </summary>
        public Color? AttributeColor { get; private set; }

        /// <summary>
        /// FontStyle: LabelBold + LabelUnderline + LabelItalic
        /// </summary>
        public FontStyle? LabelFontStyle { get; private set; }
        /// <summary>
        /// String: LabelFontFamily 
        /// </summary>
        public string LabelFontFamily { get; private set; }
        /// <summary>
        /// Float: LabelFontSize
        /// </summary>
        public float? LabelFontSize { get; private set; }
        /// <summary>
        /// Barva pozadí labelu (LabelBgColor / LabelBgColorDark)
        /// </summary>
        public Color? LabelBgColor { get; private set; }
        /// <summary>
        /// Barva písma labelu (LabelColor / LabelColorDark)
        /// </summary>
        public Color? LabelColor { get; private set; }
    }
    #endregion
    #endregion
    #region class SystemEventSound : Systémové zvuky
    /// <summary>
    /// Systémové zvuky: načtení z registru, evidence, přehrání zvuku
    /// </summary>
    public class SystemEventSound
    {
        #region Konstruktor a public data, private subclass ApplicationInfo
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="description"></param>
        /// <param name="soundSource"></param>
        private SystemEventSound(string eventName, string description, string soundSource)
        {
            EventName = eventName ?? "";
            SoundSource = soundSource;
            Description = description;
            _Applications = new List<ApplicationInfo>();
        }
        private List<ApplicationInfo> _Applications;
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"EventName: {EventName}; SoundSource: {SoundSource}";
        }
        /// <summary>
        /// Klíčový název události
        /// </summary>
        public string EventName { get; private set; }
        /// <summary>
        /// Uživatelský popis události
        /// </summary>
        public string Description { get; private set; }
        /// <summary>
        /// Zdroj zvuku
        /// </summary>
        public string SoundSource { get; private set; }
        /// <summary>
        /// Má tato událost definován zdroj v <see cref="SoundSource"/> ?
        /// </summary>
        public bool HasSoundSource { get { return !String.IsNullOrWhiteSpace(SoundSource); } }
        /// <summary>
        /// Aplikace používající tuto událost
        /// </summary>
        public string[] Applications { get { return _Applications.Select(a => a.ApplicationName).ToArray(); } }
        /// <summary>
        /// Existuje nějaká aplikace, která definuje konkrétní zvuk Current?
        /// </summary>
        public bool ExistsCurrentSource { get { return _Applications.Any(a => a.ExistsCurrentSource); } }
        /// <summary>
        /// Existuje nějaká aplikace, která definuje konkrétní zvuk Default?
        /// </summary>
        public bool ExistsDefaultSource { get { return _Applications.Any(a => a.ExistsDefaultSource); } }
        /// <summary>
        /// Do this eventu přidá další aplikaci, která jej používá
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="applicationDescription"></param>
        /// <param name="currentSource"></param>
        /// <param name="defaultSource"></param>
        private void AddApplication(string applicationName, string applicationDescription, string currentSource, string defaultSource)
        {
            _Applications.Add(new ApplicationInfo(this, applicationName, applicationDescription, currentSource, defaultSource));
        }
        /// <summary>
        /// Info o jedné aplikaci
        /// </summary>
        private class ApplicationInfo
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="applicationName"></param>
            /// <param name="applicationDescription"></param>
            /// <param name="currentSource"></param>
            /// <param name="defaultSource"></param>
            public ApplicationInfo(SystemEventSound parent, string applicationName, string applicationDescription, string currentSource, string defaultSource)
            {
                Parent = parent;
                ApplicationName = applicationName;
                ApplicationDescription = applicationDescription;
                CurrentSource = currentSource;
                DefaultSource = defaultSource;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Application: {ApplicationDescription}; CurrentSource: {CurrentSource}; DefaultSource: {DefaultSource}";
            }
            /// <summary>
            /// Parent = zvukový event
            /// </summary>
            public SystemEventSound Parent { get; private set; }
            /// <summary>
            /// Klíčové jméno aplikace (např ".Default")
            /// </summary>
            public string ApplicationName { get; private set; }
            /// <summary>
            /// Uživatelské jméno aplikace (např "Windows")
            /// </summary>
            public string ApplicationDescription { get; private set; }
            /// <summary>
            /// Aktuální zdroj zvuku
            /// </summary>
            public string CurrentSource { get; private set; }
            /// <summary>
            /// Defaultní zdroj zvuku
            /// </summary>
            public string DefaultSource { get; private set; }
            /// <summary>
            /// Má tato aplikace definován konkrétní zvuk <see cref="CurrentSource"/>?
            /// </summary>
            public bool ExistsCurrentSource { get { return !String.IsNullOrEmpty(CurrentSource); } }
            /// <summary>
            /// Má tato aplikace definován konkrétní zvuk <see cref="DefaultSource"/>?
            /// </summary>
            public bool ExistsDefaultSource { get { return !String.IsNullOrEmpty(DefaultSource); } }
        }
        #endregion
        #region Přehrání systémových zvuků a souborů WAV
        /// <summary>
        /// Přehraje this zvuk
        /// </summary>
        /// <param name="playCommands"></param>
        public void Play(PlayCommands playCommands = PlayCommands.Default)
        {
            PlaySystemEvent(this.EventName, playCommands);
        }
        /// <summary>
        /// Přehraje daný systémový zvuk. Jméno má pocházet z <see cref="SystemEventSound.EventName"/>
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="playCommands"></param>
        public static void PlaySystemEvent(string eventName, PlayCommands playCommands = PlayCommands.Default)
        {
            if (String.IsNullOrEmpty(eventName)) return;

            SND command = DefaultCommand | SND.SND_ALIAS;
            if (!playCommands.HasFlag(PlayCommands.Synchronously)) command |= SND.SND_ASYNC;
            if (!playCommands.HasFlag(PlayCommands.StopCurrentSound)) command |= SND.SND_NOSTOP;

            _PlaySound(eventName, command);
        }
        /// <summary>
        /// Přehraje daný WAV soubor.
        /// Pokud soubor není zadán, nebo pokud nemá příponu WAV, nebo pokud neexistuje, nic nedělá.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="playCommands"></param>
        public static void PlayFileWav(string fileName, PlayCommands playCommands = PlayCommands.Default)
        {
            if (String.IsNullOrEmpty(fileName)) return;
            fileName = fileName.Trim();
            if (System.IO.Path.GetExtension(fileName).Trim().ToLower() != ".wav") return;
            if (!System.IO.File.Exists(fileName)) return;

            SND command = DefaultCommand | SND.SND_FILENAME;
            if (!playCommands.HasFlag(PlayCommands.Synchronously)) command |= SND.SND_ASYNC;
            if (!playCommands.HasFlag(PlayCommands.StopCurrentSound)) command |= SND.SND_NOSTOP;

            _PlaySound(fileName, command);
        }
        private static SND DefaultCommand { get { return SND.SND_NODEFAULT | SND.SND_NOWAIT; } }
        private static void _PlaySound(string sound, SND command)
        {
            PlaySound(sound, 0, (int)command);
        }
        /// <summary>
        /// Pokyny k přehrání zvuku v metodách <see cref="SystemEventSound.Play(PlayCommands)"/>
        /// </summary>
        public enum PlayCommands
        {
            /// <summary>
            /// Nic speciálního
            /// </summary>
            None = 0,
            /// <summary>
            /// Synchronní běh = řízení se vrátí až po dokončení zadaného zvuku
            /// </summary>
            Synchronously = 0x0001,
            /// <summary>
            /// Asynchronní běh = řízení se vrátí ihned, nečeká se na dokončení zadaného zvuku
            /// </summary>
            Asynchronously = 0x0002,
            /// <summary>
            /// Pokud aktuálně zní nějaký zvuk, zruš jej a pusť tam daný zvuk (bez tohoto příznaku: nechá aktuální zvuk doznít)
            /// </summary>
            StopCurrentSound = 0x0004,

            /// <summary>
            /// Default
            /// </summary>
            Default = Asynchronously
        }
        /// <summary>
        /// API Parameter Flags for PlaySound method
        /// </summary>
        private enum SND
        {
            /// <summary>play synchronously (default)</summary>
            SND_SYNC =         0x0000,
            /// <summary>play asynchronously</summary>
            SND_ASYNC =        0x0001,
            /// <summary>silence (!default) if sound not found</summary>
            SND_NODEFAULT =    0x0002,
            /// <summary>pszSound points to a memory file</summary>
            SND_MEMORY =       0x0004,
            /// <summary>loop the sound until next sndPlaySound</summary>
            SND_LOOP =         0x0008,
            /// <summary>don't stop any currently playing sound</summary>
            SND_NOSTOP =       0x0010,
            /// <summary>don't wait if the driver is busy</summary>
            SND_NOWAIT =   0x00002000,
            /// <summary>name is a registry alias</summary>
            SND_ALIAS =    0x00010000,
            /// <summary>alias is a pre d ID</summary>
            SND_ALIAS_ID = 0x00110000,
            /// <summary>name is file name</summary>
            SND_FILENAME = 0x00020000,
            /// <summary>name is resource name or atom</summary>
            SND_RESOURCE = 0x00040004,
            /// <summary>purge non-static events for task</summary>
            SND_PURGE =        0x0040,
            /// <summary>look for application specific association</summary>
            SND_APPLICATION =  0x0080 
        }
        [DllImport("winmm.dll", EntryPoint = "PlaySound", CharSet = CharSet.Auto)]
        private static extern int PlaySound(String pszSound, int hmod, int falgs);
        #endregion
        #region Načtení seznamu systémových zvuků ze systému (WinRegistry) 
        /// <summary>
        /// Načte ze systému (WinRegistry) seznam systémových zvuků, jejich zdroje a jejich využití
        /// </summary>
        /// <returns></returns>
        public static SystemEventSound[] ReadEventSounds()
        {
            Dictionary<string, SystemEventSound> soundDict = new Dictionary<string, SystemEventSound>();

            try
            {
                using (Microsoft.Win32.RegistryKey userKey = Microsoft.Win32.Registry.CurrentUser)
                {
                    // a) Načtu jména událostí a jejich zvuky:
                    using (Microsoft.Win32.RegistryKey eventKeys = userKey.OpenSubKey(@"AppEvents\EventLabels"))
                    {
                        string[] eventNames = eventKeys.GetSubKeyNames();                // .Default; BlockedPopup; DeleteObject; Minimize; ...
                        eventKeys.Close();
                        foreach (string eventName in eventNames)
                        {
                            try
                            {
                                using (Microsoft.Win32.RegistryKey eventKey = userKey.OpenSubKey(@"AppEvents\EventLabels\" + eventName))
                                {
                                    string description = eventKey.GetValue("", "") as string;
                                    if (String.IsNullOrEmpty(description)) description = eventName;
                                    string soundSource = eventKey.GetValue("DispFileName", "") as string;
                                    eventKey.Close();
                                    soundDict.Add(eventName, new SystemEventSound(eventName, description, soundSource));
                                }
                            }
                            catch { }
                        }
                    }

                    // b) Načtu aplikace a jejich události:
                    using (Microsoft.Win32.RegistryKey appsKey = userKey.OpenSubKey(@"AppEvents\Schemes\Apps"))
                    {
                        string[] appNames = appsKey.GetSubKeyNames();                    // .Default; Explorer; WinHTTrack; ...
                        appsKey.Close();
                        foreach (string appName in appNames)
                        {   // Každá jedna aplikace:
                            try
                            {
                                using (Microsoft.Win32.RegistryKey appKey = userKey.OpenSubKey(@"AppEvents\Schemes\Apps\" + appName))
                                {   // Její klíč v registru:
                                    string appDescription = appKey.GetValue("", "") as string;     // např pro appKey = "Explorer" je appDescription = "File Explorer"
                                    string[] eventNames = appKey.GetSubKeyNames();       // Close; CriticalBatteryAlarm; PrintComplete; ...
                                    appKey.Close();
                                    foreach (string eventName in eventNames)
                                    {
                                        try
                                        {
                                            string currentValue = null;
                                            string defaultValue = null;
                                            using (Microsoft.Win32.RegistryKey currentKey = userKey.OpenSubKey(@"AppEvents\Schemes\Apps\" + appName + @"\" + eventName + @"\.Current"))
                                            {
                                                if (currentKey != null)
                                                {
                                                    currentValue = currentKey.GetValue("", "") as string;
                                                    currentKey.Close();
                                                }
                                            }
                                            using (Microsoft.Win32.RegistryKey defaultKey = userKey.OpenSubKey(@"AppEvents\Schemes\Apps\" + appName + @"\" + eventName + @"\.Default"))
                                            {
                                                if (defaultKey != null)
                                                {
                                                    defaultValue = defaultKey.GetValue("", "") as string;
                                                    defaultKey.Close();
                                                }
                                            }

                                            if (!soundDict.TryGetValue(eventName, out var sound))
                                            {
                                                sound = new SystemEventSound(eventName, null, null);
                                                soundDict.Add(eventName, sound);
                                            }
                                            sound.AddApplication(appName, appDescription, currentValue, defaultValue);

                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    userKey.Close();
                }
            }
            catch { }

            var soundList = soundDict.Values.ToList();
            soundList.Sort((a, b) => String.Compare(a.Description, b.Description, StringComparison.CurrentCultureIgnoreCase));

            return soundList.ToArray();
        }
        #endregion
    }
    #endregion
    #region class Compressor
    /// <summary>
    /// Podpora pro jednoduchou komprimaci dat
    /// </summary>
    public static class Compressor
    {
        /// <summary>
        /// Metoda zajistí komprimaci dat dodaných jako pole byte do výstupního komprimovaného pole.
        /// </summary>
        /// <param name="data">Čitelná data</param>
        /// <returns></returns>
        public static byte[] Compress(byte[] data)
        {
            byte[] zip;
            using (var inputData = new System.IO.MemoryStream(data)) // Vstupní "čitelná data"
            using (var outputZip = new System.IO.MemoryStream())     // Výstupní komprimovaná data
            using (var compressor = new System.IO.Compression.GZipStream(outputZip, System.IO.Compression.CompressionMode.Compress))
            {
                inputData.CopyTo(compressor);                        // Vstupní data natlačím do kompresoru
                compressor.Close();                                  // Z kompresoru vymačkám to, co tam zůstalo rozpracované
                zip = outputZip.ToArray();                           // Z komprimovaného streamu získám buffer a je to
            }
            return zip;
        }
        /// <summary>
        ///  Metoda zajistí dekomprimaci dat dodaných jako pole byte do výstupního čitelného pole.
        /// </summary>
        /// <param name="zip">Komprimovaná data</param>
        /// <returns></returns>
        public static byte[] DeCompress(byte[] zip)
        {
            byte[] data;
            using (var inputZip = new System.IO.MemoryStream(zip))   // Vstupní komprimovaná data
            using (var outputData = new System.IO.MemoryStream())    // Výstupní "čitelná data"
            using (var compressor = new System.IO.Compression.GZipStream(inputZip, System.IO.Compression.CompressionMode.Decompress))
            {
                compressor.CopyTo(outputData);                       // Kompresor je napojený na vstupní ZIP data, dekomprimujeme je do výstupního čitelého streamu
                compressor.Close();                                  // Z kompresoru vymačkám to, co tam zůstalo rozpracované
                data = outputData.ToArray();                         // Přečtu čitelná data do bufferu a je to
            }
            return data;
        }
        /// <summary>
        /// Metoda zajistí komprimaci dat dodaných jako pole byte do výstupního komprimovaného pole.
        /// Kompresní poměr u hustého textu je cca 45%, u HTML kódu je cca 20%.
        /// Rychlost je cca 10-15 KB textu / 1 milisec, u velkých textů a volby Fast je rychlost cca 50 KB textu / 1 milisec.
        /// Binární data (JPEG, PNG, DOCX) jsem neměřil.
        /// </summary>
        /// <param name="data">Čitelná data</param>
        /// <param name="mode">Režim komprimace. 
        /// U velkých dat má režim Fast opravdu smysl (250KB zrychlí z 25ms na 5ms).
        /// Rozdíl ve velikosti mezi Zip a Deflate i mezi Fast a Optimal u textu není nijak výrazný (Ratio se mění o 7%).</param>
        /// <returns></returns>
        public static byte[] Compress(byte[] data, CompressionMode mode)  //  = CompressionMode.Default
        {
            if (data is null) return null;
            if (data.Length == 0) return new byte[0];

            byte[] zip;
            System.IO.Compression.CompressionLevel level;
            using (var inputData = new System.IO.MemoryStream(data)) // Vstupní "čitelná data"
            using (var outputZip = new System.IO.MemoryStream())     // Výstupní komprimovaná data
            {
                switch (mode)
                {
                    case CompressionMode.DeflateOptimal:
                    case CompressionMode.DeflateFast:
                        level = (mode == CompressionMode.DeflateFast ? System.IO.Compression.CompressionLevel.Fastest : System.IO.Compression.CompressionLevel.Optimal);
                        using (var deflator = new System.IO.Compression.DeflateStream(outputZip, level))
                        {
                            inputData.CopyTo(deflator);              // Vstupní data natlačím do kompresoru
                            deflator.Close();                        // Z kompresoru vymačkám to, co tam zůstalo rozpracované
                        }
                        break;

                    case CompressionMode.ZipStreamOptimal:
                    case CompressionMode.ZipStreamFast:
                    case CompressionMode.Default:
                    default:
                        level = (mode == CompressionMode.ZipStreamFast ? System.IO.Compression.CompressionLevel.Fastest : System.IO.Compression.CompressionLevel.Optimal);
                        using (var compressor = new System.IO.Compression.GZipStream(outputZip, level))
                        {
                            inputData.CopyTo(compressor);            // Vstupní data natlačím do kompresoru
                            compressor.Close();                      // Z kompresoru vymačkám to, co tam zůstalo rozpracované
                        }
                        break;
                }
                zip = outputZip.ToArray();                           // Z komprimovaného streamu získám buffer a je to
            }
            return zip;
        }
        /// <summary>
        /// Metoda zajistí dekomprimaci dat dodaných jako pole byte do výstupního čitelného pole.
        /// </summary>
        /// <param name="zip">Komprimovaná data</param>
        /// <param name="mode">Explicitně daný režim, Nezadávejte nic (nebo Default) když nevíte, čím bylo komprimováno; použije se autodetekce.</param>
        /// <returns></returns>
        public static byte[] DeCompress(byte[] zip, CompressionMode mode)  //  = CompressionMode.Default
        {
            if (zip is null) return null;
            if (zip.Length == 0) return new byte[0];
            if (mode == CompressionMode.Default) mode = _AutodetectCompressionMode(zip);

            byte[] data;
            using (var inputZip = new System.IO.MemoryStream(zip))   // Vstupní komprimovaná data
            using (var outputData = new System.IO.MemoryStream())    // Výstupní "čitelná data"
            {
                switch (mode)
                {
                    case CompressionMode.DeflateOptimal:
                    case CompressionMode.DeflateFast:
                        using (var deflator = new System.IO.Compression.DeflateStream(inputZip, System.IO.Compression.CompressionMode.Decompress))
                        {
                            deflator.CopyTo(outputData);             // Kompresor je napojený na vstupní ZIP data, dekomprimujeme je do výstupního čitelého streamu
                            deflator.Close();                        // Z kompresoru vymačkám to, co tam zůstalo rozpracované
                        }
                        break;
                    case CompressionMode.ZipStreamOptimal:
                    case CompressionMode.ZipStreamFast:
                    default:
                        using (var compressor = new System.IO.Compression.GZipStream(inputZip, System.IO.Compression.CompressionMode.Decompress))
                        {
                            compressor.CopyTo(outputData);           // Kompresor je napojený na vstupní ZIP data, dekomprimujeme je do výstupního čitelého streamu
                            compressor.Close();                      // Z kompresoru vymačkám to, co tam zůstalo rozpracované
                        }
                        break;
                }
                data = outputData.ToArray();                         // Přečtu čitelná data do bufferu a je to
            }
            return data;
        }
        /// <summary>
        /// Metoda se pokusí určit režim komprimace dat podle jejich obsahu. Analyzuje úvodní 4 znaky, a pokud odpovídají signatuře ZIP, vrátí ZIP; jinak Deflate.
        /// </summary>
        /// <param name="zip"></param>
        /// <returns></returns>
        private static CompressionMode _AutodetectCompressionMode(byte[] zip)
        {
            if (zip is null || zip.Length < 4) return CompressionMode.ZipStreamOptimal;

            // Signatura ZIP má formát: 1F-8B-08-00-00-00-00-00-xx-xx-...
            if (zip[0] == 0x1F && zip[1] == 0x8B && zip[2] == 0x08 && zip[3] == 0x00) return CompressionMode.ZipStreamOptimal;
            return CompressionMode.DeflateOptimal;
        }
    }
    /// <summary>
    /// Režim komprimace
    /// </summary>
    public enum CompressionMode
    {
        /// <summary>
        /// Default
        /// </summary>
        Default = 0,
        /// <summary>
        /// ZipStreamOptimal
        /// </summary>
        ZipStreamOptimal,
        /// <summary>
        /// ZipStreamFast
        /// </summary>
        ZipStreamFast,
        /// <summary>
        /// DeflateOptimal
        /// </summary>
        DeflateOptimal,
        /// <summary>
        /// DeflateFast
        /// </summary>
        DeflateFast
    }
    #endregion
    #region class Algebra
    /// <summary>
    /// Algebraické třídy, rovnice
    /// </summary>
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
    #region class TaskProgress : umožní hlavnímu oknu aplikace zobrazovat progres / stav procesu v liště úkolů aplikace Windows
    /// <summary>
    /// Třída, která umožní hlavnímu oknu aplikace prezentovat progress v taskbaru Windows.
    /// <para/>
    /// Její instance se vytváří s předáním reference na MainForm aplikace, uloží si WeakReferenci.<br/>
    /// MainForm aplikace pak musí volat zdejší metodu <see cref="FormWndProc(ref Message)"/>, jinak progres nebued komunikovat.<br/>
    /// Následně může aplikace setovat stav progresu <see cref="ProgressState"/> a jeho hodnoty <see cref="ProgressValue"/> a <see cref="ProgressMaximum"/>, tím budou zobrazovány požadované hodnoty.
    /// </summary>
    public class TaskProgress
    {
        #region Inicializace, proměnné
        /// <summary>
        /// Konstruktor. Je třeba předat referenci na Main Form aplikace, progress si uloží WeakReference na okno.
        /// MainForm aplikace pak musí ze své metody <see cref="Form.WndProc(ref Message)"/> volat zdejší metodu <see cref="FormWndProc(ref Message)"/>, jinak progres nebude komunikovat.<br/>
        /// Následně může aplikace setovat stav progresu <see cref="ProgressState"/> a jeho hodnoty <see cref="ProgressValue"/> a <see cref="ProgressMaximum"/>, tím budou zobrazovány požadované hodnoty.
        /// </summary>
        /// <param name="mainForm"></param>
        public TaskProgress(Form mainForm)
        {
            _Init(mainForm);
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        /// <param name="mainForm"></param>
        private void _Init(Form mainForm)
        {
            __IsTaskbarButtonCreated = -1;
            __ProgressState = ThumbnailProgressState.NoProgress;
            __ProgressValue = 0;
            __ProgressMaximum = 100;
            Version osVersion = Environment.OSVersion.Version;
            __IsValid = ((osVersion.Major == 6 && osVersion.Minor > 0) || (osVersion.Major > 6));
            if (__IsValid)
            {
                _MainForm = mainForm;
                __IsTaskbarButtonCreated = RegisterWindowMessage(@"TaskbarButtonCreated");
            }
        }
        /// <summary>
        /// (Weakreference) na owner Form
        /// </summary>
        private Form _MainForm
        {
            get { return (__MainForm != null && __MainForm.TryGetTarget(out var form) ? form : null); }
            set { __MainForm = (value != null ? new WeakReference<Form>(value) : null); }

        }
        /// <summary>
        /// OnDemand instance <see cref="ITaskbarList3"/>
        /// </summary>
        private ITaskbarList3 _CTaskbarList
        {
            get
            {
                if (__CTaskbarList == null)
                {
                    lock (this)
                    {
                        if (__CTaskbarList == null)
                        {
                            __CTaskbarList = (ITaskbarList3)new CTaskbarList();
                            __CTaskbarList.HrInit();
                        }
                    }
                }
                return __CTaskbarList;
            }
        }
        private WeakReference<Form> __MainForm;
        private ITaskbarList3 __CTaskbarList = null;
        private bool __IsValid;
        private int __IsTaskbarButtonCreated;
        private ThumbnailProgressState __ProgressState;
        private int __ProgressValue;
        private int __ProgressMaximum;
        #endregion
        #region Public rozhraní
        /// <summary>
        /// Obsahuje true, když prvek je platný.
        /// V tom případě do něj musí volající Form předávat svoji událost WndProc do metody <see cref="FormWndProc(ref Message)"/>.
        /// </summary>
        public bool IsValid { get { return __IsValid; } }
        /// <summary>
        /// Tuto metodu musí vyvolat OwnerForm ze své metody <see cref="Form.WndProc(ref Message)"/>
        /// </summary>
        /// <param name="m"></param>
        public void FormWndProc(ref Message m)
        {
            if (__IsValid && m.Msg == __IsTaskbarButtonCreated)
                // if taskbar button created or recreated, update progress status
                _RefreshProgress();
        }
        /// <summary>
        /// Status progresu = odpovídá barvě
        /// </summary>
        public ThumbnailProgressState ProgressState
        {
            get { return __ProgressState; }
            set
            {
                if (value != __ProgressState)
                {
                    __ProgressState = value;
                    _RefreshProgress(true, true);
                }
            }
        }
        /// <summary>
        /// Hodnota progresu.
        /// Musí být v rozsahu 0 až <see cref="ProgressMaximum"/>.
        /// </summary>
        public int ProgressValue
        {
            get { return __ProgressValue; }
            set
            {
                int progressValue = (value < 0 ? 0 : (value > __ProgressMaximum ? __ProgressMaximum : value));
                if (progressValue != __ProgressValue)
                {
                    __ProgressValue = progressValue;
                    _RefreshProgress(false, true);
                }
            }
        }
        /// <summary>
        /// Hodnota progresu.
        /// Musí být v rozsahu 0 až 1.
        /// Pokud bude setována hodnota nižší, než je aktuální <see cref="ProgressValue"/>, tak bude <see cref="ProgressValue"/> snížena na toto nově zadané maximum.
        /// </summary>
        public int ProgressMaximum
        {
            get { return __ProgressMaximum; }
            set
            {
                int progressMaximum = (value < 1 ? 1 : value);
                if (progressMaximum != __ProgressMaximum)
                {
                    if (__ProgressValue > progressMaximum)
                        __ProgressValue = progressMaximum;
                    __ProgressMaximum = progressMaximum;
                    _RefreshProgress(true, false);
                }
            }
        }
        #endregion
        #region Privátní komunikace na Progress, DLL importy
        /// <summary>
        /// Zajistí vepsání hodnot <see cref="__ProgressState"/>, <see cref="__ProgressValue"/> a <see cref="__ProgressMaximum"/> do <see cref="_CTaskbarList"/>
        /// </summary>
        /// <param name="setState"></param>
        /// <param name="setValue"></param>
        private void _RefreshProgress(bool setState = true, bool setValue = true)
        {
            if (__IsValid)
            {
                var form = _MainForm;
                var cTaskbarList = _CTaskbarList;
                if (form != null && cTaskbarList != null)
                {
                    if (setState)
                        cTaskbarList.SetProgressState(form.Handle, __ProgressState);
                    if (setValue && (__ProgressState == ThumbnailProgressState.Normal || __ProgressState == ThumbnailProgressState.Paused || __ProgressState == ThumbnailProgressState.Error))
                        cTaskbarList.SetProgressValue(form.Handle, (ulong)__ProgressValue, (ulong)__ProgressMaximum);
                }
            }
        }
        [DllImport("user32.dll")]
        internal static extern int RegisterWindowMessage(string message);

        [ComImportAttribute()]
        [GuidAttribute("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface ITaskbarList3
        {
            // ITaskbarList
            [PreserveSig]
            void HrInit();
            [PreserveSig]
            void AddTab(IntPtr hwnd);
            [PreserveSig]
            void DeleteTab(IntPtr hwnd);
            [PreserveSig]
            void ActivateTab(IntPtr hwnd);
            [PreserveSig]
            void SetActiveAlt(IntPtr hwnd);

            // ITaskbarList2
            [PreserveSig]
            void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

            // ITaskbarList3
            void SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal);
            void SetProgressState(IntPtr hwnd, ThumbnailProgressState tbpFlags);
        }
        #endregion
    }

    [GuidAttribute("56FDF344-FD6D-11d0-958A-006097C9A090")]
    [ClassInterfaceAttribute(ClassInterfaceType.None)]
    [ComImportAttribute()]
    internal class CTaskbarList { }

    /// <summary>
    /// Stavy progresu v taskbaru Windows
    /// </summary>
    public enum ThumbnailProgressState
    {
        /// <summary>
        /// No progress is displayed.<br>
        /// yourFormName.Value is ignored.</br> </summary>
        NoProgress = 0,
        /// <summary>
        /// Normal progress is displayed.<br>
        /// The bar is GREEN.</br> </summary>
        Normal = 0x2,
        /// <summary>
        /// The operation is paused.<br>
        /// The bar is YELLOW.</br></summary>
        Paused = 0x8,
        /// <summary>
        /// An error occurred.<br>
        /// The bar is RED.</br> </summary>
        Error = 0x4,
        /// <summary>
        /// The progress is indeterminate.<br>
        /// Marquee style bar (constant scroll).</br> </summary>
        Indeterminate = 0x1
    }
    #endregion
    #region class ClipboardContainer : Obsah clipboardu = ID aplikace, ID zdroje dat plus vlastní Data
    /// <summary>
    /// Obsah clipboardu = ID aplikace, ID zdroje dat plus vlastní Data
    /// </summary>
    [Serializable]
    public class DataExchangeContainer
    {
        /// <summary>
        /// Kvůli XmlPersist
        /// </summary>
        private DataExchangeContainer() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataId"></param>
        /// <param name="data"></param>
        public DataExchangeContainer(string dataId, object data)
        {
            this.ApplicationGuid = DxComponent.ApplicationGuid;
            this.ApplicationId = DxComponent.ClipboardApplicationId;
            this.DataSourceId = dataId;
            this.Data = data;
        }
        /// <summary>
        /// Guid aplikace, která je zdrojem dat
        /// </summary>
        public string ApplicationGuid { get; private set; }
        /// <summary>
        /// ID aplikace, která je zdrojem dat
        /// </summary>
        public string ApplicationId { get; private set; }
        /// <summary>
        /// ID zdroje dat v rámci jedné aplikace. Identifikuje zdroj dat, který tato data připravil a do balíčku vložil, typicky při Ctrl+C.
        /// To je informace pro cíl, aby se moho rozhodnout, zda tato data chce / nechce přijmout.
        /// </summary>
        public string DataSourceId { get; private set; }
        /// <summary>
        /// Vlastní data, jejich konkrétní datový typ je dán zdrojem dat
        /// </summary>
        public object Data { get; set; }
    }
    #endregion
    #region class MessageBuffer : střadač zpráv umožňující střádání jednotlivých zpráv a poté hromadné oznámení
    /// <summary>
    /// <see cref="MessageBuffer"/> : střadač zpráv umožňující střádání jednotlivých zpráv a poté hromadné oznámení
    /// </summary>
    public class MessageBuffer
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="permanentCodes">Permanentní kódy = viz </param>
        public MessageBuffer(bool permanentCodes = false)
        {
            __Items = new List<Item>();
            __Codes = new Dictionary<string, object>();
            __PermanentCodes = permanentCodes;
            __MessagesReady = false;
        }
        /// <summary>
        /// Připraví buffer pro přijímání zpráv. Následovat bude sada volání metody <see cref="Add(string, DxMessageLevel, string)"/> a zakončeno bude metodou <see cref="Show"/>.
        /// </summary>
        public void Reset()
        {
            this.Reset(false);
        }
        /// <summary>
        /// Připraví buffer pro přijímání zpráv. Následovat bude sada volání metody <see cref="Add(string, DxMessageLevel, string)"/> a zakončeno bude metodou <see cref="Show"/>.
        /// </summary>
        /// <param name="withResetCodes">true = resetovat i ohlášené kódy, bez ohledu na nastavení <see cref="PermanentCodes"/></param>
        public void Reset(bool withResetCodes)
        {
            __Items.Clear();
            if (withResetCodes || !PermanentCodes)
                __Codes.Clear();
            __MessagesReady = true;
        }
        /// <summary>
        /// Resetuje pouze ohlášené kódy. Následující přidávání zpráv je bude evidovat od nuly.
        /// Detaily viz <see cref="PermanentCodes"/>.
        /// Tato metoda neresetuje buffer zpráv!
        /// </summary>
        public void ResetCodes()
        {
            __Codes.Clear();
        }
        /// <summary>
        /// Přidá zprávu.<br/>
        /// Pokud je objekt resetován (<see cref="Reset()"/>), pak je v <see cref="MessagesReady"/> = true a zpráva se přidá do Bufferu.<br/>
        /// Pokud není resetován, bude zpráva ohlášena okamžitě podle své závažnosti (a nebude přidána do Bufferu).
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        public void Add(DxMessageLevel level, string message)
        {
            Add(null, level, message);
        }
        /// <summary>
        /// Přidá zprávu.<br/>
        /// Pokud je objekt resetován (<see cref="Reset()"/>), pak je v <see cref="MessagesReady"/> = true a zpráva se přidá do Bufferu.<br/>
        /// Pokud není resetován, bude zpráva ohlášena okamžitě podle své závažnosti (a nebude přidána do Bufferu).
        /// <para/>
        /// Pokud je dán kód <paramref name="code"/> a takový kód již máme v evidenci, opakovaně tuto zprávu nepřidá.
        /// Netestuje se pak ani <paramref name="level"/> ani obsah <paramref name="message"/>.
        /// <para/>
        /// Kódy zpráv jsou resetovány (nulovány) buď v metodě <see cref="Reset()"/> pokud <see cref="PermanentCodes"/> je false;
        /// nebo v metodě <see cref="Reset(bool)"/> s parametrem true;
        /// nebo v metodě <see cref="ResetCodes()"/> vždy.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="level"></param>
        /// <param name="message"></param>
        public void Add(string code, DxMessageLevel level, string message)
        {
            if (__MessagesReady)
                _AddItem(code, level, message);
            else
                _ShowMessage(level, message, null);
        }
        /// <summary>
        /// Zobrazí nastřádané zprávy
        /// </summary>
        /// <param name="title"></param>
        public void Show(string title = null)
        {
            if (__MessagesReady && __Items.Count > 0)
                _ShowMessage(title);

            __Items.Clear();
            if (!PermanentCodes)
                __Codes.Clear();
            __MessagesReady = false;
        }
        /// <summary>
        /// Přidá zprávu do bufferu.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="level"></param>
        /// <param name="message"></param>
        private void _AddItem(string code, DxMessageLevel level, string message)
        {
            if (!String.IsNullOrEmpty(code))
            {
                if (__Codes.ContainsKey(code)) return;
                __Codes.Add(code, null);
            }
            __Items.Add(new Item(level, message));
        }
        /// <summary>
        /// Zajistí zobrazení výsledné bufferované zprávy pro uživatele.
        /// </summary>
        private void _ShowMessage(string title)
        {
            DxMessageLevel level = DxMessageLevel.None;
            StringBuilder sb = new StringBuilder();
            foreach (var item in __Items)
            {
                // Střádám Max(Level) a Sum(Message):
                if (((int)item.Level) > ((int)level)) level = item.Level;
                sb.AppendLine(item.Message);
            }

            _ShowMessage(level, sb.ToString(), title);
        }
        /// <summary>
        /// Zobrazí dodanou zprávu
        /// </summary>
        private void _ShowMessage(DxMessageLevel level, string message, string title)
        {
            if (String.IsNullOrEmpty(message)) return;

            if (String.IsNullOrEmpty(title)) title = DxComponent.Localize(MsgCode.ApplicationName);
            switch (level)
            {
                case DxMessageLevel.None:
                case DxMessageLevel.Info:
                    DxComponent.ShowMessageInfo(message, title);
                    break;
                case DxMessageLevel.Warning:
                    DxComponent.ShowMessageWarning(message, title);
                    break;
                case DxMessageLevel.UserError:
                case DxMessageLevel.SystemError:
                    DxComponent.ShowMessageError(message, title);
                    break;
            }
        }
        /// <summary>
        /// Kódy chyb (=hlášení jen unikátních kódů) jsou permanentní?
        /// <para/>
        /// true = chybu s jedním kódem ohlásíme pouze jedenkrát za celý život <see cref="MessageBuffer"/>. 
        /// Vhodné pro přehledovou šablonu.
        /// U některých komponent (navigační strom) to je celý život klienta, pak by hodnota true znemožnila zobrazit opakované hlášky.
        /// Je možno volat <see cref="Reset(bool)"/> s hodnotou true = resetují se i kódy bez ohledu na příznak <see cref="PermanentCodes"/>.
        /// <para/>
        /// false = zadané kódy chyb jsou po ohlášení resetovány, a pokud se objeví daný kód chyby při příští dávce znovu, budou hlášeny opakovaně.
        /// Vhodné pro Navigační strom.
        /// </summary>
        public bool PermanentCodes { get { return __PermanentCodes; } }
        /// <summary>
        /// Systém zpráv je připraven přijímat zprávy a ukládat je? Je nastaveno po <see cref="Reset()"/>, na konci musí být volána metoda <see cref="Show(string)"/>!
        /// false = každá zpráva bude přímo ohlášena.
        /// </summary>
        public bool MessagesReady { get { return __MessagesReady; } }
        private bool __PermanentCodes;
        private bool __MessagesReady;
        private List<Item> __Items;
        private Dictionary<string, object> __Codes;
        private class Item
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="level"></param>
            /// <param name="message"></param>
            public Item(DxMessageLevel level, string message)
            {
                this.Level = level;
                this.Message = message;
            }
            /// <summary>
            /// Úroveň informace
            /// </summary>
            public DxMessageLevel Level { get; private set; }
            /// <summary>
            /// Text zprávy
            /// </summary>
            public string Message { get; private set; }
        }
    }
    /// <summary>
    /// Úroveň závažnosti informace
    /// </summary>
    public enum DxMessageLevel : int
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None = 0,
        /// <summary>
        /// Informace, stav je OK
        /// </summary>
        Info,
        /// <summary>
        /// Varování, něco není OK ale pokračujeme
        /// </summary>
        Warning,
        /// <summary>
        /// Chyba na straně uživatele (nevyplněný údaj, chybná volba, nepovolená kombinace, atd)
        /// </summary>
        UserError,
        /// <summary>
        /// Chyba na straně aplikace (nečekaná NULL hodnota, volání metody před inicializací nebo po Dispose, atd)
        /// </summary>
        SystemError
    }
    #endregion
    #region class DxStyleToConfigListener : spojovací prvek mezi stylem (skin + paleta) z DevExpress a (abstract) konfigurací
    /// <summary>
    /// <see cref="DxStyleToConfigListener"/> : spojovací prvek mezi stylem (skin + paleta) z DevExpress a (abstract) konfigurací.
    /// <para/>
    /// Potomek musí implementovat dvě property: <see cref="SkinName"/> a <see cref="PaletteName"/>, a nasměrovat je na svoji konfiguraci.
    /// Zdejší třída pak zajišťuje jejich napojení na GUI DevExpress.
    /// </summary>
    public abstract class DxStyleToConfigListener : IListenerStyleChanged
    {
        /// <summary>
        /// Konstruktor s provedením automatické inicializace.
        /// Tedy v rámci konstruktoru jsou načteny hodnoty z konfigurace as aplikovány do GUI.
        /// </summary>
        public DxStyleToConfigListener()
            : this(true)
        { }
        /// <summary>
        /// Konstruktor s možností automatické inicializace (<paramref name="withInitialize"/> = true).
        /// Pokud v době konstruktoru je již funkční konfigurace, může se předat true a bude rovnou nastaven i skin.
        /// Pokud není konfigurace k dispozici, předá se false a explicitně se pak vyvolá metoda <see cref="ActivateConfigStyle"/>.
        /// </summary>
        /// <param name="withInitialize">Požadavek true na provedení inicializace</param>
        public DxStyleToConfigListener(bool withInitialize)
        {
            __IsInitialized = false;
            __IsSupressEvent = false;
            __LastSkinName = null;
            __LastSkinCompact = null;
            __LastPaletteName = null;

            if (withInitialize)
                ActivateConfigStyle();
            Noris.Clients.Win.Components.AsolDX.DxComponent.RegisterListener(this);
        }
        /// <summary>
        /// Nastaví se na true po inicializaci. Dokud je false, nebudeme řešit změny skinu.
        /// </summary>
        private bool __IsInitialized;
        /// <summary>
        /// Pokud obsahuje true, pak nebudeme reagovat na změny skinu v <see cref="IListenerStyleChanged.StyleChanged()"/> = provádíme je my sami!
        /// </summary>
        private bool __IsSupressEvent;
        /// <summary>
        /// Posledně uložený / načtený název skinu, pro detekci reálné změny v GUI.
        /// GUI občas pošle událost <see cref="IListenerStyleChanged.StyleChanged()"/> i když k reálné změně nedochází.
        /// </summary>
        private string __LastSkinName;
        /// <summary>
        /// Posledně uložený / načtený příznak Compact skinu, pro detekci reálné změny v GUI.
        /// GUI občas pošle událost <see cref="IListenerStyleChanged.StyleChanged()"/> i když k reálné změně nedochází.
        /// </summary>
        private bool? __LastSkinCompact;
        /// <summary>
        /// Posledně uložený / načtený název palety, pro detekci reálné změny v GUI.
        /// GUI občas pošle událost <see cref="IListenerStyleChanged.StyleChanged()"/> i když k reálné změně nedochází.
        /// </summary>
        private string __LastPaletteName;

        /// <summary>
        /// Jméno Skinu.
        /// Potomek má v metodě 'get' přečíst hodnotu ze své konfigurace a vrátit ji, a v metodě 'set' má předanou hodnotu do své konfigurace vepsat.
        /// <para/>
        /// Setování hodnoty nezmění GUI. Změnu GUI provede metoda <see cref="ActivateConfigStyle"/>, která si načítá hodnoty <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="PaletteName"/>.
        /// </summary>
        public abstract string SkinName { get; set; }
        /// <summary>
        /// Příznak kompaktního Skinu.
        /// Potomek má v metodě 'get' přečíst hodnotu ze své konfigurace a vrátit ji, a v metodě 'set' má předanou hodnotu do své konfigurace vepsat.
        /// <para/>
        /// Setování hodnoty nezmění GUI. Změnu GUI provede metoda <see cref="ActivateConfigStyle"/>, která si načítá hodnoty <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="PaletteName"/>.
        /// </summary>
        public abstract bool SkinCompact { get; set; }
        /// <summary>
        /// Jméno palety.
        /// Potomek má v metodě 'get' přečíst hodnotu ze své konfigurace a vrátit ji, a v metodě 'set' má předanou hodnotu do své konfigurace vepsat.
        /// <para/>
        /// Setování hodnoty nezmění GUI. Změnu GUI provede metoda <see cref="ActivateConfigStyle"/>, která si načítá hodnoty <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="PaletteName"/>.
        /// </summary>
        public abstract string PaletteName { get; set; }
        /// <summary>
        /// Aktivuje v GUI rozhraní skin daný <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="PaletteName"/>.
        /// </summary>
        public void ActivateConfigStyle()
        {
            __IsInitialized = true;

            string skinName = SkinName;
            bool skinCompact = SkinCompact;
            string paletteName = PaletteName;
            _ActivateStyle(skinName, skinCompact, paletteName, false);
            __LastSkinName = skinName;
            __LastSkinCompact = skinCompact;
            __LastPaletteName = paletteName;
        }
        /// <summary>
        /// Aktivuje v GUI rozhraní explicitně daný Skin.
        /// Uloží jej i do konfigurace (do properties <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="PaletteName"/>), jako by jej vybral uživatel.
        /// </summary>
        /// <param name="skinName"></param>
        /// <param name="skinCompact"></param>
        /// <param name="paletteName"></param>
        public void ActivateStyle(string skinName, bool skinCompact, string paletteName)
        {
            _ActivateStyle(skinName, skinCompact, paletteName, true);
        }
        /// <summary>
        /// Aktivuje v GUI rozhraní explicitně daný Skin.
        /// Podle hodnoty <paramref name="storeToConfig"/> jej uloží i do konfigurace (do properties <see cref="SkinName"/>, <see cref="SkinCompact"/> a <see cref="PaletteName"/>), jako by jej vybral uživatel.
        /// </summary>
        /// <param name="skinName"></param>
        /// <param name="skinCompact"></param>
        /// <param name="paletteName"></param>
        /// <param name="storeToConfig"></param>
        private void _ActivateStyle(string skinName, bool skinCompact, string paletteName, bool storeToConfig)
        {
            bool isActivated = false;
            try
            {
                __IsSupressEvent = true;

                if (!String.IsNullOrEmpty(skinName))
                {   // https://docs.devexpress.com/WindowsForms/2399/build-an-application/skins?utm_source=SupportCenter&utm_medium=website&utm_campaign=docs-feedback&utm_content=T1093158#how-to-re-apply-the-last-active-skin-when-an-application-restarts
                    DevExpress.LookAndFeel.UserLookAndFeel.ForceCompactUIMode(skinCompact, false);
                    if (String.IsNullOrEmpty(paletteName)) paletteName = "DefaultSkinPalette";
                    DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle(skinName, paletteName);
                }

                /*
                DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = skinName;
                if (!String.IsNullOrEmpty(paletteName))
                {   // https://supportcenter.devexpress.com/ticket/details/t827424/save-and-restore-svg-palette-name
                    var skin = DevExpress.Skins.CommonSkins.GetSkin(DevExpress.LookAndFeel.UserLookAndFeel.Default);
                    if (skin.CustomSvgPalettes.Count > 0)
                    {
                        var palette = skin.CustomSvgPalettes[paletteName];               // Když není nalezena, vrátí se null, nikoli Exception
                        if (palette != null)
                            skin.SvgPalettes[DevExpress.Skins.Skin.DefaultSkinPaletteName].SetCustomPalette(palette);
                    }
                }
                */

                isActivated = true;
            }
            catch { /* Pokud by na vstupu byly nepřijatelné hodnoty, neshodím kvůli tomu aplikaci... */ }
            finally
            {
                __IsSupressEvent = false;
            }

            if (storeToConfig && isActivated)
                _StoreToConfig(skinName, skinCompact, paletteName);
        }
        /// <summary>
        /// Uloží jméno skinu a palety do konfigurace = do <see cref="SkinName"/> a <see cref="PaletteName"/>.
        /// </summary>
        /// <param name="skinName"></param>
        /// <param name="skinCompact"></param>
        /// <param name="paletteName"></param>
        private void _StoreToConfig(string skinName, bool skinCompact, string paletteName)
        {
            SkinName = skinName;
            SkinCompact = skinCompact;
            PaletteName = paletteName;
            __LastSkinName = skinName;
            __LastSkinCompact = skinCompact;
            __LastPaletteName = paletteName;
        }
        /// <summary>
        /// Po změně skinu / palety v GUI
        /// </summary>
        void IListenerStyleChanged.StyleChanged()
        {
            if (__IsInitialized && !__IsSupressEvent)
            {
                DxSkinColorSet.ReadCurrentSkinPalette(out string skinName, out bool isCompact, out string paletteName);
                if (!String.Equals(skinName, __LastSkinName) || (((bool?)isCompact) != __LastSkinCompact) || !String.Equals(paletteName, __LastPaletteName))
                    _StoreToConfig(skinName, isCompact, paletteName);
            }
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
    #region class BitStorage32 a BitStorage64 : úložiště Boolean hodnot
    /// <summary>
    /// Úložiště až 32 hodnot typu Boolean.
    /// použití:
    /// <code>
    /// var bs32 = new BitStorage32();
    /// bs32[4] = true;
    /// bs32[5] = false;
    /// if (bs32[4]) 
    /// { /* hodnota bitu 4 je true */ }
    /// </code>
    /// </summary>
    public class BitStorage32
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public BitStorage32()
        {
            __Bits = 0U;
        }
        private UInt32 __Bits;
        /// <summary>
        /// Vizualizace hodnoty
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.__Bits.ToString("X8");
        }
        /// <summary>
        /// Hodnota daného bitu. Index smí mít hodnotu 0 až 31.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool this[byte index]
        {
            get
            {
                return ((__Bits & _Get1(index)) != 0);
            }
            set
            {
                if (value)
                    __Bits |= _Get1(index);
                else
                    __Bits &= _Get0(index);
            }
        }
        /// <summary>
        /// Metoda vrátí masku obsahující bit s hodnotou 1 na daném indexu, ostatní bity mají hodnotu 0.
        /// Pro vstup <paramref name="index"/> = 0 vrací 0x0001 = 0b0000 0000 0000 0001;
        /// Pro vstup <paramref name="index"/> = 5 vrací 0x0020 = 0b0000 0000 0010 0000;
        /// Pro vstup <paramref name="index"/> = 7 vrací 0x0080 = 0b0000 0000 1000 0000;
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private static UInt32 _Get1(byte index)
        {
            if (index >= 32) throw new ArgumentException($"BitStorage32 error: index value {index} is out of range 0 ÷ 31.");
            UInt32 mask = (1U << index);
            return mask;
        }
        /// <summary>
        /// Metoda vrátí masku obsahující bit s hodnotou 0 na daném indexu, ostatní bity mají hodnotu 1.
        /// Pro vstup <paramref name="index"/> = 0 vrací 0xFFFE = 0b1111 1111 1111 1111;
        /// Pro vstup <paramref name="index"/> = 5 vrací 0xFFDF = 0b1111 1111 1101 1111;
        /// Pro vstup <paramref name="index"/> = 7 vrací 0xFF7F = 0b1111 1111 0111 1111;
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private static UInt32 _Get0(byte index)
        {
            if (index >= 32) throw new ArgumentException($"BitStorage32 error: index value {index} is out of range 0 ÷ 31.");
            UInt32 mask = UInt32.MaxValue ^ (1U << index);
            return mask;
        }
    }
    /// <summary>
    /// Úložiště až 32 hodnot typu Boolean.
    /// použití:
    /// <code>
    /// var bs64 = new BitStorage64();
    /// bs64[54] = true;
    /// bs64[55] = false;
    /// if (bs64[54]) 
    /// { /* hodnota bitu 54 je true */ }
    /// </code>
    /// </summary>
    public class BitStorage64
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public BitStorage64()
        {
            __Bits = 0UL;
        }
        private UInt64 __Bits;
        /// <summary>
        /// Vizualizace hodnoty
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.__Bits.ToString("X16");
        }
        /// <summary>
        /// Hodnota daného bitu. Index smí mít hodnotu 0 až 31.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool this[byte index]
        {
            get
            {
                return ((__Bits & _Get1(index)) != 0);
            }
            set
            {
                if (value)
                    __Bits |= _Get1(index);
                else
                    __Bits &= _Get0(index);
            }
        }
        /// <summary>
        /// Metoda vrátí masku obsahující bit s hodnotou 1 na daném indexu, ostatní bity mají hodnotu 0.
        /// Pro vstup <paramref name="index"/> = 0 vrací 0x0001 = 0b0000 0000 0000 0001;
        /// Pro vstup <paramref name="index"/> = 5 vrací 0x0020 = 0b0000 0000 0010 0000;
        /// Pro vstup <paramref name="index"/> = 7 vrací 0x0080 = 0b0000 0000 1000 0000;
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private static UInt64 _Get1(byte index)
        {
            if (index >= 64) throw new ArgumentException($"BitStorage64 error: index value {index} is out of range 0 ÷ 63.");
            UInt64 mask = (1UL << index);
            return mask;
        }
        /// <summary>
        /// Metoda vrátí masku obsahující bit s hodnotou 0 na daném indexu, ostatní bity mají hodnotu 1.
        /// Pro vstup <paramref name="index"/> = 0 vrací 0xFFFE = 0b1111 1111 1111 1111;
        /// Pro vstup <paramref name="index"/> = 5 vrací 0xFFDF = 0b1111 1111 1101 1111;
        /// Pro vstup <paramref name="index"/> = 7 vrací 0xFF7F = 0b1111 1111 0111 1111;
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private static UInt64 _Get0(byte index)
        {
            if (index >= 64) throw new ArgumentException($"BitStorage64 error: index value {index} is out of range 0 ÷ 63.");
            UInt64 mask = UInt64.MaxValue ^ (1UL << index);
            return mask;
        }
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
    #region class ListExt<T> : extended List
    /// <summary>
    /// Represents a strongly typed list of objects that can be accessed by index. Provides methods to search, sort, and manipulate lists.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ListExt<T> : List<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListExt{T}"/> class that is empty and has the default initial capacity.
        /// </summary>
        public ListExt() : base() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ListExt{T}"/> class that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        public ListExt(int capacity) : base(capacity) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ListExt{T}"/> class that contains elements copied from the specified collection and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        public ListExt(IEnumerable<T> collection) : base(collection) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ListExt{T}"/> class that contains elements copied from the specified collection and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="items"></param>
        public ListExt(params T[] items) : base(items) { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"List<{typeof(T).Name}>: Count= {Count}";
        }
        /// <summary>
        /// Do this Listu přidá sadu dodaných prvků.
        /// </summary>
        /// <param name="items"></param>
        public void AddItems(params T[] items)
        {
            this.AddRange(items);
        }
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
    /// Třída argumentů obsahující dva prvky generického typu <typeparamref name="T"/> s charakterem 
    /// Původní hodnota <see cref="OldValue"/> a Nová hodnota <see cref="NewValue"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TEventValueChangedArgs<T> : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="source">Zdroj události</param>
        /// <param name="oldValue">Původní hodnota</param>
        /// <param name="newValue">Nová hodnota</param>
        public TEventValueChangedArgs(EventSource source, T oldValue, T newValue) { Source = source; OldValue = oldValue; NewValue = newValue; }
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
        /// Nová hodnota. Nelze změnit.
        /// </summary>
        public T NewValue { get; private set; }
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
    #endregion
    #region Obecné enumy
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
        ToLeft,
        /// <summary>
        /// Nahoru doleva 45° nebo podle rozměrů obdélníku
        /// </summary>
        UpLeft,
        /// <summary>
        /// Nahoru doprava 45° nebo podle rozměrů obdélníku
        /// </summary>
        UpRight,
        /// <summary>
        /// Dolů doprava 45° nebo podle rozměrů obdélníku
        /// </summary>
        DownRight,
        /// <summary>
        /// Dolů doleva 45° nebo podle rozměrů obdélníku
        /// </summary>
        DownLeft
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
    /// <summary>
    /// Režim práce s obrázkem při jeho vkládání do cílového prostoru
    /// </summary>
    public enum ImageFillMode
    {
        /// <summary>
        /// Nekreslit, jako by nebyl obrázek zadán
        /// </summary>
        None,
        /// <summary>
        /// Velikost neupravovat, nechat 1:1, umístit a oříznout
        /// </summary>
        Clip,
        /// <summary>
        /// Pokud je obrázek větší než cíl, pak jej zmenšit, ale pokud je menší pak nezvětšovat
        /// </summary>
        Shrink,
        /// <summary>
        /// Velikost obrázku přizpůsobit cíli (zmenšit nebo zvětšit), ale zachovat poměr stran
        /// </summary>
        Resize,
        /// <summary>
        /// Obrázek deformovat do rozměrů cílového prostoru tak, aby byl v obou směrech vyplněn na 100%
        /// </summary>
        Fill,
        /// <summary>
        /// Obrázek ponechat v původní velikosti, umístit v levém horním rohu a opakovat jako dlaždice po celé ploše
        /// </summary>
        Tile
    }
    /// <summary>
    /// Režim práce s Image a ImageList
    /// </summary>
    public enum DxImageListMode
    {
        /// <summary>
        /// Defaultní = podle možností (používat ImageList, definovat obě velikosti)
        /// </summary>
        Default = 0,
        /// <summary>
        /// Používat ImageList, ale vkládat jen požadovanou velikost
        /// </summary>
        UseOnlyOneSize,
        /// <summary>
        /// Nepoužívat ImageList, ale vkládat přímo Image /ImageSvg do objektu
        /// </summary>
        UseDirectImage
    }
    /// <summary>
    /// Typ kurzoru. 
    /// Fyzický kurzor pro konkrétní typ vrátí <see cref="DxComponent.GetCursor(CursorTypes)"/>.
    /// </summary>
    public enum CursorTypes
    {
        /// <summary>
        /// Default
        /// </summary>
        Default,
        /// <summary>
        /// Hand
        /// </summary>
        Hand,
        /// <summary>
        /// Arrow
        /// </summary>
        Arrow,
        /// <summary>
        /// Cross
        /// </summary>
        Cross,
        /// <summary>
        /// IBeam
        /// </summary>
        IBeam,
        /// <summary>
        /// Help
        /// </summary>
        Help,
        /// <summary>
        /// AppStarting
        /// </summary>
        AppStarting,
        /// <summary>
        /// UpArrow
        /// </summary>
        UpArrow,
        /// <summary>
        /// WaitCursor
        /// </summary>
        WaitCursor,
        /// <summary>
        /// HSplit
        /// </summary>
        HSplit,
        /// <summary>
        /// VSplit
        /// </summary>
        VSplit,
        /// <summary>
        /// NoMove2D
        /// </summary>
        NoMove2D,
        /// <summary>
        /// NoMoveHoriz
        /// </summary>
        NoMoveHoriz,
        /// <summary>
        /// NoMoveVert
        /// </summary>
        NoMoveVert,
        /// <summary>
        /// SizeAll
        /// </summary>
        SizeAll,
        /// <summary>
        /// SizeNESW
        /// </summary>
        SizeNESW,
        /// <summary>
        /// SizeNS
        /// </summary>
        SizeNS,
        /// <summary>
        /// SizeNWSE
        /// </summary>
        SizeNWSE,
        /// <summary>
        /// SizeWE
        /// </summary>
        SizeWE,
        /// <summary>
        /// PanEast
        /// </summary>
        PanEast,
        /// <summary>
        /// PanNE
        /// </summary>
        PanNE,
        /// <summary>
        /// PanNorth
        /// </summary>
        PanNorth,
        /// <summary>
        /// PanNW
        /// </summary>
        PanNW,
        /// <summary>
        /// PanSE
        /// </summary>
        PanSE,
        /// <summary>
        /// PanSouth
        /// </summary>
        PanSouth,
        /// <summary>
        /// PanSW
        /// </summary>
        PanSW,
        /// <summary>
        /// PanWest
        /// </summary>
        PanWest,
        /// <summary>
        /// No
        /// </summary>
        No
    }
    #endregion
    #region class Convertor : Knihovna statických konverzních metod mezi simple typy a stringem
    /// <summary>
    /// Convertor : Knihovna statických konverzních metod mezi simple typy a stringem
    /// </summary>
    public static class Convertor
    {
        #region Sada krátkých metod pro serializaci a deserializaci Simple typů (jsou vyjmenované v TypeLibrary._SimpleTypePrepare())
        #region System types
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string BooleanToString(object value)
        {
            return ((Boolean)value ? "true" : "false");
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="textTrue"></param>
        /// <param name="textFalse"></param>
        /// <returns></returns>
        public static string BooleanToString(object value, string textTrue, string textFalse)
        {
            return ((Boolean)value ? textTrue : textFalse);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToBoolean(string text)
        {
            return (!String.IsNullOrEmpty(text) && (text.ToLower() == "true" || text == "1" || text.ToLower() == "a" || text.ToLower() == "y"));
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ByteToString(object value)
        {
            return ((Byte)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToByte(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Byte)0;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Byte)0;
            Byte b = (Byte)(value & 0x00FF);
            return b;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SByteToString(object value)
        {
            return ((SByte)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToSByte(string text)
        {
            if (String.IsNullOrEmpty(text)) return (SByte)0;
            SByte value;
            if (!SByte.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (SByte)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Int16ToString(object value)
        {
            return ((Int16)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToInt16(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int16)0;
            NumberStyles style = _StringToHexStyle(ref text);
            Int16 value;
            if (!Int16.TryParse(text, style, _Nmfi, out value)) return (Int16)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Int32ToString(object value)
        {
            return ((Int32)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToInt32(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int32)0;
            NumberStyles style = _StringToHexStyle(ref text);
            Int32 value;
            if (!Int32.TryParse(text, style, _Nmfi, out value)) return (Int32)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Int64ToString(object value)
        {
            return ((Int64)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToInt64(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int64)0;
            NumberStyles style = _StringToHexStyle(ref text);
            Int64 value;
            if (!Int64.TryParse(text, style, _Nmfi, out value)) return (Int64)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string IntPtrToString(object value)
        {
            return ((IntPtr)value).ToInt64().ToString("G");
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToIntPtr(string text)
        {
            if (String.IsNullOrEmpty(text)) return (IntPtr)0;
            NumberStyles style = _StringToHexStyle(ref text);
            Int64 int64;
            if (!Int64.TryParse(text, style, _Nmfi, out int64)) return (IntPtr)0;
            return new IntPtr(int64);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UInt16ToString(object value)
        {
            return ((UInt16)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToUInt16(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UInt16)0;
            NumberStyles style = _StringToHexStyle(ref text);
            UInt16 value;
            if (!UInt16.TryParse(text, style, _Nmfi, out value)) return (UInt16)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UInt32ToString(object value)
        {
            return ((UInt32)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToUInt32(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UInt32)0;
            NumberStyles style = _StringToHexStyle(ref text);
            UInt32 value;
            if (!UInt32.TryParse(text, style, _Nmfi, out value)) return (UInt32)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UInt64ToString(object value)
        {
            return ((UInt64)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToUInt64(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UInt64)0;
            NumberStyles style = _StringToHexStyle(ref text);
            UInt64 value;
            if (!UInt64.TryParse(text, style, _Nmfi, out value)) return (UInt64)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UIntPtrToString(object value)
        {
            return ((UIntPtr)value).ToUInt64().ToString("G");
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToUIntPtr(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UIntPtr)0;
            NumberStyles style = _StringToHexStyle(ref text);
            UInt64 uint64;
            if (!UInt64.TryParse(text, style, _Nmfi, out uint64)) return (UIntPtr)0;
            return new UIntPtr(uint64);
        }
        /// <summary>
        /// Vrátí styl pro konverzi textu na číslo, detekuje a řeší HEX prefixy 0x a &amp;h.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static NumberStyles _StringToHexStyle(ref string text)
        {
            NumberStyles style = NumberStyles.Any;
            if (text.Length > 2)
            {
                string prefix = text.Substring(0, 2).ToLower();
                if (prefix == "0x" || prefix == "&h")
                {
                    text = text.Substring(2);
                    style = NumberStyles.HexNumber;
                }
            }
            return style;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SingleToString(object value)
        {
            Single number = (Single)value;
            string text = number.ToString("N", _Nmfi);
            if ((number % 1f) == 0f)
            {
                int dot = text.IndexOf('.');
                if (dot > 0)
                    text = text.Substring(0, dot);
            }
            return text;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToSingle(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Single)0;
            Single value;
            if (!Single.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Single)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DoubleToString(object value)
        {
            Double number = (Double)value;
            string text = number.ToString("N", _Nmfi);
            if ((number % 1d) == 0d)
            {
                int dot = text.IndexOf('.');
                if (dot > 0)
                    text = text.Substring(0, dot);
            }
            return text;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToDouble(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Double)0;
            Double value;
            if (!Double.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Double)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DecimalToString(object value)
        {
            Decimal number = (Decimal)value;
            string text = number.ToString("N", _Nmfi);
            if ((number % 1m) == 0m)
            {
                int dot = text.IndexOf('.');
                if (dot > 0)
                    text = text.Substring(0, dot);
            }
            return text;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToDecimal(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Decimal)0;
            Decimal value;
            if (!Decimal.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Decimal)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GuidToString(object value)
        {
            return ((Guid)value).ToString("N", _Nmfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToGuid(string text)
        {
            if (String.IsNullOrEmpty(text)) return Guid.Empty;
            Guid value;
            if (!Guid.TryParse(text, out value)) return Guid.Empty;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string CharToString(object value)
        {
            return ((Char)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToChar(string text)
        {
            if (String.IsNullOrEmpty(text)) return Char.MinValue;
            Char value;
            if (!Char.TryParse(text, out value)) return Char.MinValue;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string StringToString(object value)
        {
            if (value == null) return null;
            return value as string;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToString(string text)
        {
            if (text == null) return null;
            return text;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DateTimeToString(object value)
        {
            DateTime dateTime = (DateTime)value;
            if (dateTime.Millisecond == 0 && dateTime.Second == 0)
                return dateTime.ToString("D", _Dtfi);
            return dateTime.ToString("F", _Dtfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToDateTime(string text)
        {
            if (String.IsNullOrEmpty(text)) return DateTime.MinValue;
            DateTime value;
            if (DateTime.TryParseExact(text, "D", _Dtfi, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault, out value))
                return value;
            if (DateTime.TryParseExact(text, "F", _Dtfi, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault, out value))
                return value;
            if (DateTime.TryParse(text, _Dtfi, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault, out value))
                return value;

            return DateTime.MinValue;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DateTimeToSerial(object value)
        {
            DateTime dateTime = (DateTime)value;
            string u = (dateTime.Kind == DateTimeKind.Utc ? "U" : (dateTime.Kind == DateTimeKind.Local ? "L" : ""));
            bool h = (dateTime.TimeOfDay.Ticks > 0L);
            bool s = (h && dateTime.Second > 0);
            bool f = (h && dateTime.Millisecond > 0);
            string format = (f ? "yyyyMMddHHmmssfff" : (s ? "yyyyMMddHHmmss" : (h ? "yyyyMMddHHmm" : "yyyyMMdd")));
            return u + dateTime.ToString(format);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object SerialToDateTime(string text)
        {
            if (String.IsNullOrEmpty(text)) return DateTime.MinValue;
            string serial = text.Trim();
            if (serial.Length < 8) return DateTime.MinValue;
            DateTimeKind kind = _GetDateTimeKind(ref serial);
            int length = serial.Length;
            if (length < 8) return DateTime.MinValue;
            int dy = _GetInt(serial, 0, 4, 100);
            int dm = _GetInt(serial, 4, 2, 1);
            int dd = _GetInt(serial, 6, 2, 1);
            if (dm < 1 || dm > 12 || dd < 1 || dd > 31) return DateTime.MinValue;

            try
            {
                if (length == 8) return new DateTime(dy, dm, dd, 0, 0, 0, kind);

                int th = _GetInt(serial, 8, 2, 0);
                int tm = _GetInt(serial, 10, 2, 0);
                int ts = _GetInt(serial, 12, 2, 0);
                if (th < 0 || th > 23 || tm < 0 || tm > 59 || ts < 0 || ts > 59) return new DateTime(dy, dm, dd, 0, 0, 0, kind);

                int tf = _GetInt(serial, 14, 3, 0);
                return new DateTime(dy, dm, dd, th, tm, ts, tf, kind);
            }
            catch { }
            return DateTime.MinValue;
        }
        /// <summary>
        /// Vrátí typ času podle prefixu
        /// </summary>
        /// <param name="serial"></param>
        /// <returns></returns>
        private static DateTimeKind _GetDateTimeKind(ref string serial)
        {
            switch (serial[0])
            {
                case 'U':
                    serial = serial.Substring(1);
                    return DateTimeKind.Utc;
                case 'L':
                    serial = serial.Substring(1);
                    return DateTimeKind.Local;
            }
            return DateTimeKind.Unspecified;
        }
        /// <summary>
        /// Vrátí číslo ze substringu
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="begin"></param>
        /// <param name="length"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        private static int _GetInt(string serial, int begin, int length, int defValue)
        {
            if (serial.Length < (begin + length)) return defValue;
            int value;
            if (!Int32.TryParse(serial.Substring(begin, length), out value)) return defValue;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DateTimeOffsetToString(object value)
        {
            DateTimeOffset dateTimeOffset = (DateTimeOffset)value;
            return dateTimeOffset.ToString("F", _Dtfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToDateTimeOffset(string text)
        {
            if (String.IsNullOrEmpty(text)) return DateTimeOffset.MinValue;
            DateTimeOffset value;
            if (!DateTimeOffset.TryParseExact(text, "D", _Dtfi, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault, out value)) return DateTimeOffset.MinValue;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string TimeSpanToString(object value)
        {
            return ((TimeSpan)value).ToString("G", _Dtfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToTimeSpan(string text)
        {
            if (String.IsNullOrEmpty(text)) return TimeSpan.Zero;
            TimeSpan value;
            if (!TimeSpan.TryParse(text, _Dtfi, out value)) return TimeSpan.Zero;
            return value;
        }
        #endregion
        #region Object to/from, Type
        /// <summary>
        /// Z objektu detekuje jeho typ a pak podle tohoto typu převede hodnotu na string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ObjectToString(object value)
        {
            if (value == null) return "";
            string typeName = value.GetType().FullName;
            switch (typeName)
            {
                case "System.Boolean": return BooleanToString(value);
                case "System.Byte": return ByteToString(value);
                case "System.SByte": return SByteToString(value);
                case "System.Int16": return Int16ToString(value);
                case "System.Int32": return Int32ToString(value);
                case "System.Int64": return Int64ToString(value);
                case "System.UInt16": return UInt16ToString(value);
                case "System.UInt32": return UInt32ToString(value);
                case "System.UInt64": return UInt64ToString(value);
                case "System.Single": return SingleToString(value);
                case "System.Double": return DoubleToString(value);
                case "System.Decimal": return DecimalToString(value);
                case "System.DateTime": return DateTimeToString(value);
                case "System.TimeSpan": return TimeSpanToString(value);
                case "System.Char": return CharToString(value);
                case "System.DateTimeOffset": return DateTimeOffsetToString(value);
                case "System.Guid": return GuidToString(value);
                case "System.Drawing.Color": return ColorToString(value);
                case "System.Drawing.Point": return PointToString(value);
                case "System.Drawing.PointF": return PointFToString(value);
                case "System.Drawing.Rectangle": return RectangleToString(value);
                case "System.Drawing.RectangleF": return RectangleFToString(value);
                case "System.Drawing.Size": return SizeToString(value);
                case "System.Drawing.SizeF": return SizeFToString(value);
            }
            return value.ToString();
        }
        /// <summary>
        /// Daný string převede na hodnotu požadovaného typu. Pokud není zadán typ, vrátí null.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object StringToObject(string text, Type type)
        {
            if (type == null) return null;
            string typeName = type.FullName;
            switch (typeName)
            {
                case "System.Boolean": return StringToBoolean(text);
                case "System.Byte": return StringToByte(text);
                case "System.SByte": return StringToSByte(text);
                case "System.Int16": return StringToInt16(text);
                case "System.Int32": return StringToInt32(text);
                case "System.Int64": return StringToInt64(text);
                case "System.UInt16": return StringToUInt16(text);
                case "System.UInt32": return StringToUInt32(text);
                case "System.UInt64": return StringToUInt64(text);
                case "System.Single": return StringToSingle(text);
                case "System.Double": return StringToDouble(text);
                case "System.Decimal": return StringToDecimal(text);
                case "System.DateTime": return StringToDateTime(text);
                case "System.TimeSpan": return StringToTimeSpan(text);
                case "System.Char": return StringToChar(text);
                case "System.DateTimeOffset": return StringToDateTimeOffset(text);
                case "System.Guid": return StringToGuid(text);
                case "System.Drawing.Color": return StringToColor(text);
                case "System.Drawing.Point": return StringToPoint(text);
                case "System.Drawing.PointF": return StringToPointF(text);
                case "System.Drawing.Rectangle": return StringToRectangle(text);
                case "System.Drawing.RectangleF": return StringToRectangleF(text);
                case "System.Drawing.Size": return StringToSize(text);
                case "System.Drawing.SizeF": return StringToSizeF(text);
            }
            return null;
        }
        /// <summary>
        /// Vrátí String pro daný Type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string TypeToString(Type type)
        {
            string name = type.FullName;
            if (name.StartsWith("System."))
            {
                string sysName = name.Substring(7);
                if (sysName.IndexOf(".") < 0) return sysName;        // Z typů "System.DateTime" vrátím jen "DateTime"
            }
            if (name.StartsWith("System.Drawing."))
            {
                string sysName = name.Substring(15);
                if (sysName.IndexOf(".") < 0) return sysName;        // Z typů "System.Drawing.RectangleF" vrátím jen "RectangleF"
            }
            return name;
        }
        /// <summary>
        /// Převede text na Type. Pokud nelze určit Type, vrátí null, ale ne chybu.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Type StringToType(string text)
        {
            if (String.IsNullOrEmpty(text)) return null;
            text = _NormalizeTypeName(text);
            if (text.IndexOf(".") < 0)
                text = "System." + text;
            Type type = null;
            try { type = Type.GetType(text, false, true); }
            catch { type = null; }
            return type;
        }
        /// <summary>
        /// Vrátí plný StringName pro daný zjednodušený název typu
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string _NormalizeTypeName(string text)
        {
            text = text.Trim();
            string key = text.ToLower();
            switch (key)
            {   // Tady řeším "zjednodušené názvy typů" a vracím ".NET názvy typů"; nemusím prefixovat "System." :
                case "bool": return "System.Boolean";
                case "guid": return "System.Guid";
                case "short": return "System.Int16";
                case "int": return "System.Int32";
                case "long": return "System.Int64";
                case "ushort": return "System.UInt16";
                case "uint": return "System.UInt32";
                case "ulong": return "System.UInt64";
                case "numeric": return "System.Decimal";
                case "float": return "System.Single";
                case "double": return "System.Double";                 // Tenhle a další nejsou nutné, protože to řeší parametr "ignoreCase" v Type.GetType()
                case "decimal": return "System.Decimal";
                case "number": return "System.Decimal";
                case "text": return "System.String";
                case "varchar": return "System.String";
                case "char": return "System.String";                   // Toto je změna typu !!!
                case "date": return "System.DateTime";
                case "time": return "System.DateTime";
                case "color": return "System.Drawing.Color";
                case "point": return "System.Drawing.Point";
                case "pointf": return "System.Drawing.PointF";
                case "rectangle": return "System.Drawing.Rectangle";
                case "rectanglef": return "System.Drawing.RectangleF";
                case "size": return "System.Drawing.Size";
                case "sizef": return "System.Drawing.SizeF";
            }
            if (key.StartsWith("numeric_")) return "Decimal";   // Jakékoli "numeric_19_6" => Decimal
            return text;
        }
        #endregion
        #region Nullable types
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Int32NToString(object value)
        {
            Int32? v = (Int32?)value;
            return (v.HasValue ? v.Value.ToString() : "null");
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToInt32N(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int32?)null;
            if (text.ToLower().Trim() == "null") return (Int32?)null;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Int32?)null;
            return (Int32?)value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SingleNToString(object value)
        {
            Single? v = (Single?)value;
            return (v.HasValue ? SingleToString(v.Value) : "null");
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToSingleN(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Single?)null;
            if (text.ToLower().Trim() == "null") return (Single?)null;
            Single value;
            if (!Single.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Single?)null;
            return (Single?)value;
        }
        #endregion
        #region Sql Types (removed)
        /*
        public static string SqlBinaryToString(object value)
        {
            if (value == null) return null;
            SqlBinary data = (SqlBinary)value;
            if (data.IsNull) return null;
            return Convert.ToBase64String(data.Value);
        }
        public static object StringToSqlBinary(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlBinary.Null;
            return new SqlBinary(Convert.FromBase64String(text));
        }
        public static string SqlBooleanToString(object value)
        {
            if (value == null) return null;
            SqlBoolean data = (SqlBoolean)value;
            if (data.IsNull) return null;
            return (data.Value ? "true" : "false");
        }
        public static object StringToSqlBoolean(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlBoolean.Null;
            string data = text.Trim().ToLower();
            return (text == "true" ? SqlBoolean.True :
                   (text == "false" ? SqlBoolean.False : SqlBoolean.Null));
        }
        public static string SqlByteToString(object value)
        {
            if (value == null) return null;
            SqlByte data = (SqlByte)value;
            if (data.IsNull) return null;
            return data.Value.ToString();
        }
        public static object StringToSqlByte(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlByte.Null;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlByte.Null;
            SqlByte b = new SqlByte((Byte)(value & 0x00FF));
            return b;
        }
        public static string SqlDateTimeToString(object value)
        {
            if (value == null) return null;
            SqlDateTime data = (SqlDateTime)value;
            if (data.IsNull) return null;
            return data.Value.ToString("F", _Dtfi);
        }
        public static object StringToSqlDateTime(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlDateTime.Null;
            DateTime value;
            if (!DateTime.TryParseExact(text, "D", _Dtfi, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault, out value)) return SqlDateTime.Null;
            return new SqlDateTime(value);
        }
        public static string SqlDecimalToString(object value)
        {
            if (value == null) return null;
            SqlDecimal data = (SqlDecimal)value;
            if (data.IsNull) return null;
            return data.Value.ToString("N", _Nmfi);
        }
        public static object StringToSqlDecimal(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlDecimal.Null;
            Decimal value;
            if (!Decimal.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlDecimal.Null;
            return new SqlDecimal(value);
        }
        public static string SqlDoubleToString(object value)
        {
            if (value == null) return null;
            SqlDouble data = (SqlDouble)value;
            if (data.IsNull) return null;
            return data.Value.ToString("N", _Nmfi);
        }
        public static object StringToSqlDouble(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlDouble.Null;
            Double value;
            if (!Double.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlDouble.Null;
            return new SqlDouble(value);
        }
        public static string SqlGuidToString(object value)
        {
            if (value == null) return null;
            SqlGuid data = (SqlGuid)value;
            if (data.IsNull) return null;
            return data.Value.ToString("N", _Nmfi);
        }
        public static object StringToSqlGuid(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlGuid.Null;
            Guid value;
            if (!Guid.TryParse(text, out value)) return SqlGuid.Null;
            return new SqlGuid(value);
        }
        public static string SqlInt16ToString(object value)
        {
            if (value == null) return null;
            SqlInt16 data = (SqlInt16)value;
            if (data.IsNull) return null;
            return data.Value.ToString();
        }
        public static object StringToSqlInt16(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlInt16.Null;
            Int16 value;
            if (!Int16.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlInt16.Null;
            return new SqlInt16(value);
        }
        public static string SqlInt32ToString(object value)
        {
            if (value == null) return null;
            SqlInt32 data = (SqlInt32)value;
            if (data.IsNull) return null;
            return data.Value.ToString();
        }
        public static object StringToSqlInt32(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlInt32.Null;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlInt32.Null;
            return new SqlInt32(value);
        }
        public static string SqlInt64ToString(object value)
        {
            if (value == null) return null;
            SqlInt64 data = (SqlInt64)value;
            if (data.IsNull) return null;
            return data.Value.ToString();
        }
        public static object StringToSqlInt64(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlInt64.Null;
            Int64 value;
            if (!Int64.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlInt64.Null;
            return new SqlInt64(value);
        }
        public static string SqlMoneyToString(object value)
        {
            if (value == null) return null;
            SqlMoney data = (SqlMoney)value;
            if (data.IsNull) return null;
            return data.Value.ToString();
        }
        public static object StringToSqlMoney(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlMoney.Null;
            Decimal value;
            if (!Decimal.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlMoney.Null;
            return new SqlMoney(value);
        }
        public static string SqlSingleToString(object value)
        {
            if (value == null) return null;
            SqlSingle data = (SqlSingle)value;
            if (data.IsNull) return null;
            return data.Value.ToString();
        }
        public static object StringToSqlSingle(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlSingle.Null;
            Single value;
            if (!Single.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlSingle.Null;
            return new SqlSingle(value);
        }
        public static string SqlStringToString(object value)
        {
            if (value == null) return null;
            SqlString data = (SqlString)value;
            if (data.IsNull) return null;
            return data.Value;
        }
        public static object StringToSqlString(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlString.Null;
            return text;
        }
        */
        #endregion
        #region Drawing Types
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ColorToString(object value)
        {
            if (value is KnownColor)
            {
                KnownColor knownColor = (KnownColor)value;
                return System.Enum.GetName(typeof(KnownColor), knownColor);
            }
            if (!(value is Color))
                return "";
            Color data = (Color)value;
            if (data.IsKnownColor)
                return System.Enum.GetName(typeof(KnownColor), data.ToKnownColor());
            if (data.IsNamedColor)
                return data.Name;
            if (data.IsSystemColor)
                return "System." + data.ToString();
            if (data.A < 255)
                return ("#" + data.A.ToString("X2") + data.R.ToString("X2") + data.G.ToString("X2") + data.B.ToString("X2")).ToUpper();
            return ("#" + data.R.ToString("X2") + data.G.ToString("X2") + data.B.ToString("X2")).ToUpper();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToColor(string text)
        {
            if (String.IsNullOrEmpty(text)) return Color.Empty;
            string t = text.Trim();                      // Jméno "Orchid", nebo hexa #806040 (RGB), nebo 0xD02000 (RGB), nebo hexa "#FF808040" (ARGB) nebo 0x40C0C0FF (ARGB).
            if (t.Length == 7 && t[0] == '#' && ContainOnlyHexadecimals(t.Substring(1, 6)))
                return StringRgbToColor(t.Substring(1, 6));
            if (t.Length == 8 && t.Substring(0, 2).ToLower() == "0x" && ContainOnlyHexadecimals(t.Substring(2, 6)))
                return StringRgbToColor(t.Substring(2, 6));
            if (t.Length == 9 && t[0] == '#' && ContainOnlyHexadecimals(t.Substring(1, 8)))
                return StringARgbToColor(t.Substring(1, 8));
            if (t.Length == 10 && t.Substring(0, 2).ToLower() == "0x" && ContainOnlyHexadecimals(t.Substring(2, 8)))
                return StringARgbToColor(t.Substring(2, 8));
            return StringNameToColor(t);
        }
        /// <summary>
        /// Z dodané barvy vrátí hexadecimální formát ve formě "#RRGGBB".
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ColorToXmlString(Color data)
        {
            if (data.A < 255)
                return ("#" + data.A.ToString("X2") + data.R.ToString("X2") + data.G.ToString("X2") + data.B.ToString("X2")).ToUpper();
            return ("#" + data.R.ToString("X2") + data.G.ToString("X2") + data.B.ToString("X2")).ToUpper();
        }
        /// <summary>
        /// Z deklarace barvy ve formě "#RRGGBB" v hexadecimálním formátu vrátí odpovídající barvu.
        /// Barva bude mít hodnotu Alpha = 255.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Color XmlStringToColor(string text)
        {
            if (String.IsNullOrEmpty(text)) return Color.Empty;
            string t = text.Trim();                      // Jméno "Orchid", nebo hexa #806040 (RGB), nebo 0xD02000 (RGB), nebo hexa "#FF808040" (ARGB) nebo 0x40C0C0FF (ARGB).
            if (t.Length == 7 && t[0] == '#' && ContainOnlyHexadecimals(t.Substring(1, 6)))
                return StringRgbToColor(t.Substring(1, 6));
            if (t.Length == 8 && t.Substring(0, 2).ToLower() == "0x" && ContainOnlyHexadecimals(t.Substring(2, 6)))
                return StringRgbToColor(t.Substring(2, 6));
            if (t.Length == 9 && t[0] == '#' && ContainOnlyHexadecimals(t.Substring(1, 8)))
                return StringARgbToColor(t.Substring(1, 8));
            if (t.Length == 10 && t.Substring(0, 2).ToLower() == "0x" && ContainOnlyHexadecimals(t.Substring(2, 8)))
                return StringARgbToColor(t.Substring(2, 8));
            return StringNameToColor(t);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Color StringNameToColor(string name)
        {
            KnownColor known;
            if (System.Enum.TryParse<KnownColor>(name, out known))
                return Color.FromKnownColor(known);

            try
            {
                return Color.FromName(name);
            }
            catch
            { }
            return Color.Empty;
        }
        /// <summary>
        /// Konkrétní konvertor z hodnoty "RRGGBB" na Color, kde RR, GG, BB je hexadecimální číslo
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static Color StringRgbToColor(string t)
        {
            int r = HexadecimalToInt32(t.Substring(0, 2));
            int g = HexadecimalToInt32(t.Substring(2, 2));
            int b = HexadecimalToInt32(t.Substring(4, 2));
            return Color.FromArgb(r, g, b);
        }
        /// <summary>
        /// Konkrétní konvertor z hodnoty "AARRGGBB" na Color, kde aa, RR, GG, BB je hexadecimální číslo
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static Color StringARgbToColor(string t)
        {
            int a = HexadecimalToInt32(t.Substring(0, 2));
            int r = HexadecimalToInt32(t.Substring(2, 2));
            int g = HexadecimalToInt32(t.Substring(4, 2));
            int b = HexadecimalToInt32(t.Substring(6, 2));
            return Color.FromArgb(a, r, g, b);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static string PointToString(object value, char delimiter = ';')
        {
            Point data = (Point)value;
            return $"{data.X}{delimiter}{data.Y}";
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static object StringToPoint(string text, char delimiter = ';')
        {
            if (String.IsNullOrEmpty(text)) return Point.Empty;
            string[] items = text.Split(delimiter);
            if (items.Length != 2) return Point.Empty;
            int x = StringInt32(items[0]);
            int y = StringInt32(items[1]);
            return new Point(x, y);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static string PointFToString(object value, char delimiter = ';')
        {
            PointF data = (PointF)value;
            return $"{data.X.ToString("N", _Nmfi)}{delimiter}{data.Y.ToString("N", _Nmfi)}";
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static object StringToPointF(string text, char delimiter = ';')
        {
            if (String.IsNullOrEmpty(text)) return PointF.Empty;
            string[] items = text.Split(delimiter);
            if (items.Length != 2) return PointF.Empty;
            Single x = StringSingle(items[0]);
            Single y = StringSingle(items[1]);
            return new PointF(x, y);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static string RectangleToString(object value, char delimiter = ';')
        {
            Rectangle data = (Rectangle)value;
            return $"{data.X}{delimiter}{data.Y}{delimiter}{data.Width}{delimiter}{data.Height}";
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static object StringToRectangle(string text, char delimiter = ';')
        {
            if (String.IsNullOrEmpty(text)) return Rectangle.Empty;
            string[] items = text.Split(delimiter);
            if (items.Length != 4) return Rectangle.Empty;
            int x = StringInt32(items[0]);
            int y = StringInt32(items[1]);
            int w = StringInt32(items[2]);
            int h = StringInt32(items[3]);
            return new Rectangle(x, y, w, h);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static string RectangleFToString(object value, char delimiter = ';')
        {
            RectangleF data = (RectangleF)value;
            return $"{data.X.ToString("N", _Nmfi)}{delimiter}{data.Y.ToString("N", _Nmfi)}{delimiter}{data.Width.ToString("N", _Nmfi)}{delimiter}{data.Height.ToString("N", _Nmfi)}";
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static object StringToRectangleF(string text, char delimiter = ';')
        {
            if (String.IsNullOrEmpty(text)) return RectangleF.Empty;
            string[] items = text.Split(delimiter);
            if (items.Length != 4) return RectangleF.Empty;
            Single x = StringSingle(items[0]);
            Single y = StringSingle(items[1]);
            Single w = StringSingle(items[2]);
            Single h = StringSingle(items[3]);
            return new RectangleF(x, y, w, h);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static string SizeToString(object value, char delimiter = ';')
        {
            Size data = (Size)value;
            return $"{data.Width}{delimiter}{data.Height}";
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static object StringToSize(string text, char delimiter = ';')
        {
            if (String.IsNullOrEmpty(text)) return Size.Empty;
            string[] items = text.Split(delimiter);
            if (items.Length != 2) return Size.Empty;
            int w = StringInt32(items[0]);
            int h = StringInt32(items[1]);
            return new Size(w, h);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static string SizeFToString(object value, char delimiter = ';')
        {
            SizeF data = (SizeF)value;
            return $"{data.Width.ToString("N", _Nmfi)}{delimiter}{data.Height.ToString("N", _Nmfi)}";
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter">Oddělovač hodnot, default ";"</param>
        /// <returns></returns>
        public static object StringToSizeF(string text, char delimiter = ';')
        {
            if (String.IsNullOrEmpty(text)) return SizeF.Empty;
            string[] items = text.Split(';');
            if (items.Length != 2) return SizeF.Empty;
            Single w = StringSingle(items[0]);
            Single h = StringSingle(items[1]);
            return new SizeF(w, h);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string FontStyleToString(object value)
        {
            FontStyle fontStyle = (FontStyle)value;
            bool b = ((fontStyle & FontStyle.Bold) != 0);
            bool i = ((fontStyle & FontStyle.Italic) != 0);
            bool s = ((fontStyle & FontStyle.Strikeout) != 0);
            bool u = ((fontStyle & FontStyle.Underline) != 0);
            string result = (b ? "B" : "") + (i ? "I" : "") + (s ? "S" : "") + (u ? "U" : "");
            if (result.Length > 0) return result;
            return "R";
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToFontStyle(string text)
        {
            if (String.IsNullOrEmpty(text)) return FontStyle.Regular;
            FontStyle result = (text.Contains("B") ? FontStyle.Bold : FontStyle.Regular) |
                               (text.Contains("I") ? FontStyle.Italic : FontStyle.Regular) |
                               (text.Contains("S") ? FontStyle.Strikeout : FontStyle.Regular) |
                               (text.Contains("U") ? FontStyle.Underline : FontStyle.Regular);
            return result;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string FontToString(object value)
        {
            if (value == null) return "";
            Font font = (Font)value;
            return font.Name + ";" + SingleToString(font.SizeInPoints) + ";" + FontStyleToString(font.Style) + ";" + ByteToString(font.GdiCharSet);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToFont(string text)
        {
            if (String.IsNullOrEmpty(text)) return null;
            string[] items = text.Split(';');
            if (items.Length != 4) return null;
            float emSize = (float)StringToSingle(items[1]);
            FontStyle fontStyle = (FontStyle)StringToFontStyle(items[2]);
            byte gdiCharSet = (byte)StringToByte(items[3]);
            Font result = new Font(items[0], emSize, fontStyle, GraphicsUnit.Point, gdiCharSet);
            return result;
        }
        #endregion
        #region User types : je vhodnější persistovat je pomocí interface IXmlSerializer (pomocí property string IXmlSerializer.XmlSerialData { get; set; } )
        #endregion
        #region Enum types
        /// <summary>
        /// Vrátí název dané hodnoty enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string EnumToString<T>(T value)
        {
            return Enum.GetName(typeof(T), value);
        }
        /// <summary>
        /// Vrátí hodnotu enumu daného typu z daného stringu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text"></param>
        /// <returns></returns>
        public static T StringToEnum<T>(string text) where T : struct
        {
            T value;
            if (Enum.TryParse<T>(text, out value))
                return value;
            return default(T);
        }
        /// <summary>
        /// Vrátí hodnotu enumu daného typu z daného stringu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text"></param>
        /// <param name="defaultValue">Defaultní hodnota</param>
        /// <returns></returns>
        public static T StringToEnum<T>(string text, T defaultValue) where T : struct
        {
            T value;
            if (Enum.TryParse<T>(text, out value))
                return value;
            return defaultValue;
        }
        #endregion
        #region Helpers
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Int32 StringInt32(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int32)0;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Int32)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Single StringSingle(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Single)0;
            Single value;
            if (!Single.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Single)0;
            return value;
        }
        /// <summary>
        /// Vrátí Int32 ekvivalent daného hexadecimálního čísla.
        /// Hexadecimální číslo nesmí obsahovat prefix ani mezery, pouze hexadecimální znaky ("0123456789abcdefABCDEF").
        /// Délka textu je relativně libovolná (v rozsahu Int32, jinak dojde k přetečení).
        /// </summary>
        /// <param name="hexa"></param>
        /// <returns></returns>
        public static Int32 HexadecimalToInt32(string hexa)
        {
            Int64 value = HexadecimalToInt64(hexa);
            if (value > (Int64)(Int32.MaxValue) || value < (Int64)(Int32.MinValue))
                throw new OverflowException("Hexadecimal value " + hexa + " exceeding range for Int32 number.");
            return (Int32)value;
        }
        /// <summary>
        /// Vrátí Int64 ekvivalent daného hexadecimálního čísla.
        /// Hexadecimální číslo nesmí obsahovat prefix ani mezery, pouze hexadecimální znaky ("0123456789abcdefABCDEF").
        /// Délka textu je relativně libovolná (v rozsahu Int64, jinak dojde k přetečení).
        /// </summary>
        /// <param name="hexa"></param>
        /// <returns></returns>
        public static Int64 HexadecimalToInt64(string hexa)
        {
            int result = 0;
            if (hexa == null || hexa.Length == 0 || !ContainOnlyHexadecimals(hexa)) return result;
            int len = hexa.Length;
            int cfc = 1;
            for (int u = (len - 1); u >= 0; u--)
            {
                char c = hexa[u];
                switch (c)
                {
                    case '0':
                        break;
                    case '1':
                        result += cfc;
                        break;
                    case '2':
                        result += 2 * cfc;
                        break;
                    case '3':
                        result += 3 * cfc;
                        break;
                    case '4':
                        result += 4 * cfc;
                        break;
                    case '5':
                        result += 5 * cfc;
                        break;
                    case '6':
                        result += 6 * cfc;
                        break;
                    case '7':
                        result += 7 * cfc;
                        break;
                    case '8':
                        result += 8 * cfc;
                        break;
                    case '9':
                        result += 9 * cfc;
                        break;
                    case 'a':
                    case 'A':
                        result += 10 * cfc;
                        break;
                    case 'b':
                    case 'B':
                        result += 11 * cfc;
                        break;
                    case 'c':
                    case 'C':
                        result += 12 * cfc;
                        break;
                    case 'd':
                    case 'D':
                        result += 13 * cfc;
                        break;
                    case 'e':
                    case 'E':
                        result += 14 * cfc;
                        break;
                    case 'f':
                    case 'F':
                        result += 15 * cfc;
                        break;
                }
                cfc = cfc * 16;
            }
            return result;
        }
        /// <summary>
        /// Vrací true, když text obsahuje pouze hexadecimální znaky
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool ContainOnlyHexadecimals(string text)
        {
            return ContainOnlyChars(text, "0123456789abcdefABCDEF");
        }
        /// <summary>
        /// Vrací true, když text obsahuje pouze povolené znaky ze seznamu (chars)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static bool ContainOnlyChars(string text, string chars)
        {
            if (text == null) return false;
            foreach (char c in text)
            {
                // Pokud písmeno c (ze vstupního textu) není obsaženo v seznamu povolených písmen, pak vrátíme false (text obsahuje jiné znaky než dané):
                if (!chars.Contains(c)) return false;
            }
            return true;
        }
        /// <summary>
        /// Z daného řetězce (text) odkrojí a vrátí část, která se nachází před delimiterem.
        /// Dany text (ref) zkrátí, bude obsahovat část za delimiterem.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static string StringCutOff(ref string text, string delimiter)
        {
            if (text == null) return null;
            if (text.Length == 0) return "";
            string result;
            if (String.IsNullOrEmpty(delimiter))
                throw new ArgumentNullException("delimiter", "Parametr metody Convertor.StringCutOff(«delimiter») nemůže být prázdný.");
            int len = delimiter.Length;
            int at = text.IndexOf(delimiter);
            if (at < 0)
            {
                result = text;
                text = "";
            }
            else if (at == 0)
            {
                result = "";
                text = (at + len >= text.Length ? "" : text.Substring(at + len));
            }
            else
            {
                result = text.Substring(0, at);
                text = (at + len >= text.Length ? "" : text.Substring(at + len));
            }
            return result;
        }
        #endregion
        #endregion
        #region Konverze enumů na základě názvu hodnoty (Enum přes String na jiný Enum)
        /// <summary>
        /// Vstupní hodnotu enumu konvertuje do výstupní odpovídající stejnojmenné hodnoty.
        /// Konvertuje přes string, nikoli přes numerickou hodnotu. Enumy tedy musí mít shodné názvy prvků.
        /// </summary>
        /// <typeparam name="TInp"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="inp"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TOut ConvertEnum<TInp, TOut>(TInp inp, TOut defaultValue)
            where TInp : struct
            where TOut : struct
        {
            string name = inp.ToString();
            if (Enum.TryParse<TOut>(name, out TOut value1)) return value1;
            if (Enum.TryParse<TOut>(name, true, out TOut value2)) return value2;
            return defaultValue;
        }
        /// <summary>
        /// Vstupní hodnotu enumu konvertuje do výstupní odpovídající stejnojmenné hodnoty.
        /// Konvertuje přes string, nikoli přes numerickou hodnotu. Enumy tedy musí mít shodné názvy prvků.
        /// <para/>
        /// Ošetřena NULL hodnota na vstupu: pokud je na vstupu NULL, pak výstup je automaticky NULL. Nikoli <paramref name="defaultValue"/>.
        /// Pokud vstupní hodnota nebude nalezena ve výstupním Enumu, pak výstupem bude <paramref name="defaultValue"/>. Ta může být předána jako null, pak i tehdy bude výstupem null.
        /// </summary>
        /// <typeparam name="TInp"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="inp"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TOut? ConvertEnum<TInp, TOut>(TInp? inp, TOut? defaultValue)
            where TInp : struct
            where TOut : struct
        {
            if (!inp.HasValue) return null;

            string name = inp.Value.ToString();
            if (Enum.TryParse<TOut>(name, out TOut value1)) return value1;
            if (Enum.TryParse<TOut>(name, true, out TOut value2)) return value2;
            return defaultValue;
        }
        /// <summary>
        /// Vstupní hodnotu danou stringem konvertuje do hodnoty enumu <typeparamref name="TOut"/> odpovídající stejnojmenné hodnoty.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="text"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TOut ConvertEnum<TOut>(string text, TOut defaultValue)
            where TOut : struct
        {
            if (!String.IsNullOrEmpty(text))
            {
                string name = text.Trim();
                if (Enum.TryParse<TOut>(name, out TOut value1)) return value1;
                if (Enum.TryParse<TOut>(name, true, out TOut value2)) return value2;
            }
            return defaultValue;
        }
        #endregion
        #region Static konstruktor
        static Convertor()
        { _PrepareFormats(); }
        #endregion
        #region FormatInfo
        static void _PrepareFormats()
        {
            _Dtfi = new System.Globalization.DateTimeFormatInfo();
            _Dtfi.LongDatePattern = "yyyy-MM-dd HH:mm";                   // Pattern pro formátování písmenem D, musí být nastaveno před nastavením patternu FullDateTimePattern
            _Dtfi.FullDateTimePattern = "yyyy-MM-dd HH:mm:ss.fff";        // Pattern pro formátování písmenem F

            _Nmfi = new System.Globalization.NumberFormatInfo();
            _Nmfi.NumberDecimalDigits = 4;
            _Nmfi.NumberDecimalSeparator = ".";
            _Nmfi.NumberGroupSeparator = "";
        }
        static System.Globalization.DateTimeFormatInfo _Dtfi;
        static System.Globalization.NumberFormatInfo _Nmfi;
        #endregion
    }
    #endregion
}
