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
// using BAR = DevExpress.XtraBars;
// using EDI = DevExpress.XtraEditors;
// using TAB = DevExpress.XtraTab;
// using GRD = DevExpress.XtraGrid;
// using CHT = DevExpress.XtraCharts;
// using RIB = DevExpress.XtraBars.Ribbon;
// using NOD = DevExpress.XtraTreeList.Nodes;

namespace Noris.Clients.Win.Components.AsolDX
{
    #region class DxComponent : Factory pro tvorbu DevExpress komponent
    /// <summary>
    /// <see cref="DxComponent"/> : Factory pro tvorbu DevExpress komponent
    /// </summary>
    public sealed class DxComponent
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
            this._InitLog();
            this._InitStyles();
            this._InitZoom();
            this._InitDrawing();
            this._InitListeners();
        }
        private static DxComponent _Instance;
        private static object _InstanceLock = new object();
        #endregion
        #region Init a Done
        public static void Init() { Instance._Init(); }
        private void _Init()
        {
            System.Threading.Thread.CurrentThread.Name = "GUI thread";
            DevExpress.UserSkins.BonusSkins.Register();
            DevExpress.Skins.SkinManager.EnableFormSkins();
            DevExpress.Skins.SkinManager.EnableMdiFormSkins();
            DevExpress.XtraEditors.WindowsFormsSettings.AnimationMode = DevExpress.XtraEditors.AnimationMode.EnableAll;
            DevExpress.XtraEditors.WindowsFormsSettings.AllowHoverAnimation = DevExpress.Utils.DefaultBoolean.True;
            DevExpress.LookAndFeel.UserLookAndFeel.Default.SkinName = "iMaginary";
        }
        public static void Done() { Instance._Done(); }
        private void _Done()
        { }
        #endregion
        #region Splash Screen
        public static void SplashShow(string title, string subTitle = null, string leftFooter = null, string rightFooter = null,
            Form owner = null, Image image = null, DevExpress.Utils.Svg.SvgImage svgImage = null,
            DevExpress.XtraSplashScreen.FluentLoadingIndicatorType? indicator = null, Color? opacityColor = null, int? opacity = null,
            bool useFadeIn = true, bool useFadeOut = true)
        { Instance._SplashShow(title, subTitle, leftFooter, rightFooter,
            owner, image, svgImage,
            indicator, opacityColor, opacity,
            useFadeIn, useFadeOut); }
        public static void SplashUpdate(string title = null, string subTitle = null, string leftFooter = null, string rightFooter = null,
            Color? opacityColor = null, int? opacity = null)
        {
            Instance._SplashUpdate(title, subTitle, leftFooter, rightFooter, opacityColor, opacity);
        }
        public static void SplashHide() { Instance._SplashHide(); }

        private void _SplashShow(string title, string subTitle, string leftFooter, string rightFooter,
            Form owner, Image image, DevExpress.Utils.Svg.SvgImage svgImage,
            DevExpress.XtraSplashScreen.FluentLoadingIndicatorType? indicator, Color? opacityColor, int? opacity,
            bool useFadeIn, bool useFadeOut)
        {
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
        private void _SplashHide()
        {
            if (_SplashOptions != null)
            {
                DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
                _SplashOptions = null;
            }
        }
        private DevExpress.XtraSplashScreen.FluentSplashScreenOptions _SplashOptions;
        #endregion
        #region Styly
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
        public static int DetailXLabel { get { return ZoomToGuiInt(Instance._DetailXLabel); } }
        /// <summary>
        /// Odsazení textu od levého okraje X
        /// </summary>
        public static int DetailXText { get { return ZoomToGuiInt(Instance._DetailXText); } }
        /// <summary>
        /// Odsazení prvního prvku od horního okraje Y
        /// </summary>
        public static int DetailYFirst { get { return ZoomToGuiInt(Instance._DetailYFirst); } }
        /// <summary>
        /// Výchozí hodnota výšky labelu
        /// </summary>
        public static int DetailYHeightLabel { get { return ZoomToGuiInt(Instance._DetailYHeightLabel); } }
        /// <summary>
        /// Výchozí hodnota výšky textu
        /// </summary>
        public static int DetailYHeightText { get { return ZoomToGuiInt(Instance._DetailYHeightText); } }
        /// <summary>
        /// Posun labelu vůči textu v ose Y pro zarovnané úpatí textu
        /// </summary>
        public static int DetailYOffsetLabelText { get { return ZoomToGuiInt(Instance._DetailYOffsetLabelText); } }
        /// <summary>
        /// Odsazení labelu dalšího řádku od předešlého textu
        /// </summary>
        public static int DetailYSpaceLabel { get { return ZoomToGuiInt(Instance._DetailYSpaceLabel); } }
        /// <summary>
        /// Odsazení textu řádku od předešlého labelu
        /// </summary>
        public static int DetailYSpaceText { get { return ZoomToGuiInt(Instance._DetailYSpaceText); } }
        /// <summary>
        /// Okraj v ose X
        /// </summary>
        public static int DetailXMargin { get { return ZoomToGuiInt(Instance._DetailXMargin); } }
        /// <summary>
        /// Okraj v ose Y
        /// </summary>
        public static int DetailYMargin { get { return ZoomToGuiInt(Instance._DetailYMargin); } }
        /// <summary>
        /// Defaultní výška panelu s buttony
        /// </summary>
        public static int DefaultButtonPanelHeight { get { return ZoomToGuiInt(Instance._DefaultButtonPanelHeight); } }
        /// <summary>
        /// Defaultní šířka buttonu
        /// </summary>
        public static int DefaultButtonWidth { get { return ZoomToGuiInt(Instance._DefaultButtonWidth); } }
        /// <summary>
        /// Defaultní výška buttonu
        /// </summary>
        public static int DefaultButtonHeight { get { return ZoomToGuiInt(Instance._DefaultButtonHeight); } }

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
        #endregion
        #region Rozhraní na Zoom
        /// <summary>
        /// Inicializace Zoomu
        /// </summary>
        private void _InitZoom()
        {
            _Zoom = 1m;
            _SubscribersToZoomChange = new List<_SubscriberToZoomChange>();
            _SubscribersToZoomLastClean = DateTime.Now;
            ComponentConnector.Host.InteractiveZoomChanged += Host_InteractiveZoomChanged;
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
            _CallSubscriberToZoomChange();
        }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static int ZoomToGuiInt(int value) { decimal zoom = Instance._Zoom; return _ZoomToGuiInt(value, zoom); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Point ZoomToGuiInt(Point value) { decimal zoom = Instance._Zoom; return new Point(_ZoomToGuiInt(value.X, zoom), _ZoomToGuiInt(value.Y, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Point? ZoomToGuiInt(Point? value) { if (!value.HasValue) return null; decimal zoom = Instance._Zoom; return new Point(_ZoomToGuiInt(value.Value.X, zoom), _ZoomToGuiInt(value.Value.Y, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Size ZoomToGuiInt(Size value) { decimal zoom = Instance._Zoom; return new Size(_ZoomToGuiInt(value.Width, zoom), _ZoomToGuiInt(value.Height, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle aktuálního Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static Size? ZoomToGuiInt(Size? value) { if (!value.HasValue) return null; decimal zoom = Instance._Zoom; return new Size(_ZoomToGuiInt(value.Value.Width, zoom), _ZoomToGuiInt(value.Value.Height, zoom)); }
        /// <summary>
        /// Vrátí danou designovou hodnotu přepočtenou dle daného Zoomu do vizuální hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        private static int _ZoomToGuiInt(int value, decimal zoom) { return (int)Math.Round((decimal)value * zoom, 0); }
        /// <summary>
        /// Aktuální hodnota Zoomu
        /// </summary>
        internal static decimal Zoom { get { return Instance._Zoom; } }
        /// <summary>
        /// Reload hodnoty Zoomu
        /// </summary>
        internal static void ReloadZoom() { Instance._ReloadZoom(); }
        /// <summary>
        /// Reload hodnoty Zoomu uvnitř instance
        /// </summary>
        private void _ReloadZoom() { _Zoom = ((decimal)Common.SupportScaling.GetScaledValue(100000)) / 100000m; }
        private decimal _Zoom;
        #endregion
        #region Listenery
        /// <summary>
        /// Jakýkoli objekt se může touto metodou přihlásit k odběru zpráv o událostech systému.
        /// Když v systému dojde k obecné události, například "Změna Zoomu" nebo "Změna Skinu", pak zdroj této události zavolá systém <see cref="DxComponent"/>,
        /// ten vyhledá odpovídající listenery (které jsou naživu) a vyvolá jejich odpovídající metodu.
        /// <para/>
        /// Typy událostí jsou určeny tím, který konkrétní interface (potomek <see cref="IListener"/>) daný posluchač implementuje.
        /// Na příkladu Změny Zoomu: tam, kde dojde ke změně Zoomu (Desktop) bude vyvolaná metoda <see cref="DxComponent.CallListeners{T}"/>,
        /// tato metoda vyhledá ve svém seznamu Listenerů ty, které implementují <see cref="IListenerZoomChange"/>, a vyvolá jejich výkonnou metodu.
        /// </summary>
        /// <param name="listener"></param>
        public static void RegisterListener(IListener listener) { Instance._RegisterListener(listener); }
        /// <summary>
        /// Zavolá Listenery daného typu a předá jim daný argumen
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args"></param>
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

        private void DevExpress_StyleChanged(object sender, EventArgs e)
        {
            _CallListeners<IListenerStyleChanged>();
        }

        private void _RegisterListener(IListener listener)
        {
            if (listener == null) return;

            _ClearDeadListeners();
            lock (__Listeners)
                __Listeners.Add(new _ListenerInstance(listener));
        }
        private void _CallListeners<T>() where T : IListener
        {
            var listeners = _GetListeners<T>();
            if (listeners.Length == 0) return;
            var method = _GetListenerMethod(typeof(T), 0);
            foreach (var listener in listeners)
                method.Invoke(listener, null);
        }
        private void _CallListeners<T>(object args) where T : IListener
        {
            var listeners = _GetListeners<T>();
            if (listeners.Length == 0) return;
            var method = _GetListenerMethod(typeof(T), 1);
            foreach (var listener in listeners)
                method.Invoke(listener, null);
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
        private List<_ListenerInstance> __Listeners;
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
        #region SubscriberToZoomChange
        /// <summary>
        /// Zaeviduje si dalšího žadatele o volání metody <see cref="ISubscriberToZoomChange.ZoomChanged()"/> po změně Zoomu
        /// </summary>
        /// <param name="subscriber"></param>
        public static void SubscribeToZoomChange(ISubscriberToZoomChange subscriber)
        {
            Instance._AddSubscriberToZoomChange(subscriber);
        }
        /// <summary>
        /// Odebere daného žadatele o volání metody <see cref="ISubscriberToZoomChange.ZoomChanged()"/> ze seznamu.
        /// </summary>
        /// <param name="subscriber"></param>
        public static void UnSubscribeToZoomChange(ISubscriberToZoomChange subscriber)
        {
            Instance._RemoveSubscriberToZoomChange(subscriber);
        }
        /// <summary>
        /// Zaeviduje si dalšího žadatele o volání metody <see cref="ISubscriberToZoomChange.ZoomChanged()"/> po změně Zoomu
        /// </summary>
        /// <param name="subscriber"></param>
        private void _AddSubscriberToZoomChange(ISubscriberToZoomChange subscriber)
        {
            if (subscriber == null) return;

            _ClearDeadSubscriberToZoomChange();
            lock (_SubscribersToZoomChange)
                _SubscribersToZoomChange.Add(new _SubscriberToZoomChange(subscriber));
        }
        /// <summary>
        /// Odebere daného žadatele o volání metody <see cref="ISubscriberToZoomChange.ZoomChanged()"/> ze seznamu.
        /// </summary>
        /// <param name="subscriber"></param>
        private void _RemoveSubscriberToZoomChange(ISubscriberToZoomChange subscriber)
        {
            lock (_SubscribersToZoomChange)
                _SubscribersToZoomChange.RemoveAll(s => !s.IsAlive || s.ContainsSubscriber(subscriber));
            _SubscribersToZoomLastClean = DateTime.Now;
        }
        /// <summary>
        /// Odebere mrtvé Subscribery z pole <see cref="_SubscribersToZoomChange"/>
        /// </summary>
        /// <param name="force"></param>
        private void _ClearDeadSubscriberToZoomChange(bool force = false)
        {
            if (!force && ((TimeSpan)(DateTime.Now - _SubscribersToZoomLastClean)).TotalSeconds < 30d) return;  // Když to není nutné, nebudeme to řešit
            lock (_SubscribersToZoomChange)
                _SubscribersToZoomChange.RemoveAll(s => !s.IsAlive);
            _SubscribersToZoomLastClean = DateTime.Now;
        }
        /// <summary>
        /// Zavolá všechny živé subscribery o změně Zoomu
        /// </summary>
        private void _CallSubscriberToZoomChange()
        {
            _ClearDeadSubscriberToZoomChange();
            
            List<_SubscriberToZoomChange> activeSubscribers = null;
            lock (_SubscribersToZoomChange)
                activeSubscribers = _SubscribersToZoomChange.ToList();

            activeSubscribers.ForEach(s => s.CallSubscribe());
        }
        private List<_SubscriberToZoomChange> _SubscribersToZoomChange;
        private DateTime _SubscribersToZoomLastClean;
        /// <summary>
        /// Evidence jednoho subscribera
        /// </summary>
        private class _SubscriberToZoomChange
        {
            public _SubscriberToZoomChange(ISubscriberToZoomChange subscriber)
            {
                __Subscriber = new WeakTarget<ISubscriberToZoomChange>(subscriber);
            }
            private WeakTarget<ISubscriberToZoomChange> __Subscriber;
            /// <summary>
            /// true pokud je objekt použitelný
            /// </summary>
            public bool IsAlive { get { return __Subscriber.IsAlive; } }
            /// <summary>
            /// Vrátí true, pokud this instance drží odkaz na daného subscribera.
            /// </summary>
            public bool ContainsSubscriber(ISubscriberToZoomChange testSubscriber)
            {
                ISubscriberToZoomChange mySubscriber = __Subscriber.Target;
                return (mySubscriber != null && Object.ReferenceEquals(mySubscriber, testSubscriber));
            }
            /// <summary>
            /// Zavolá metodu <see cref="ISubscriberToZoomChange.ZoomChanged()"/> pro zdejší cíl
            /// </summary>
            public void CallSubscribe()
            {
                ISubscriberToZoomChange subscriber = __Subscriber.Target;
                if (subscriber != null)
                    subscriber.ZoomChanged();
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

            checkEdit.SetToolTip(toolTipTitle ?? text, toolTipText);

            if (checkedChanged != null) checkEdit.CheckedChanged += checkedChanged;
            if (parent != null) parent.Controls.Add(checkEdit);
            if (shiftY) y = y + checkEdit.Height + inst._DetailYSpaceText;

            return checkEdit;
        }
        public static DxListBoxControl CreateDxListBox(DockStyle? dock = null, int? width = null, int? height = null, Control parent = null, EventHandler selectedIndexChanged = null,
            bool? multiColumn = null, SelectionMode? selectionMode = null, int? itemHeight = null, int? itemHeightPadding = null, bool? reorderByDragEnabled = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? tabStop = null)
        {
            int y = 0;
            int w = width ?? 0;
            int h = height ?? 0;
            return CreateDxListBox(0, ref y, w, h, parent, selectedIndexChanged,
                multiColumn, selectionMode, itemHeight, itemHeightPadding, reorderByDragEnabled,
                toolTipTitle, toolTipText,
                dock, visible, tabStop, false);
        }
        public static DxListBoxControl CreateDxListBox(int x, int y, int w, int h, Control parent = null, EventHandler selectedIndexChanged = null,
            bool? multiColumn = null, SelectionMode? selectionMode = null, int? itemHeight = null, int? itemHeightPadding = null, bool? reorderByDragEnabled = null,
            string toolTipTitle = null, string toolTipText = null,
            DockStyle? dock = null, bool? visible = null, bool? tabStop = null)
        {
            return CreateDxListBox(x, ref y, w, h, parent, selectedIndexChanged,
                multiColumn, selectionMode, itemHeight, itemHeightPadding, reorderByDragEnabled,
                toolTipTitle, toolTipText,
                dock, visible, tabStop, false);
        }
        public static DxListBoxControl CreateDxListBox(int x, ref int y, int w, int h, Control parent = null, EventHandler selectedIndexChanged = null,
            bool? multiColumn = null, SelectionMode? selectionMode = null, int? itemHeight = null, int? itemHeightPadding = null, bool? reorderByDragEnabled = null,
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

            if (reorderByDragEnabled.HasValue) listBox.ReorderByDragEnabled = reorderByDragEnabled.Value;
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
            bool? visible = null, bool? enabled = null, bool? tabStop = null)
        {
            return CreateDxCheckButton(x, ref y, w, h, parent, text, click,
                paintStyles,
                isChecked,
                image, resourceName,
                toolTipTitle, toolTipText,
                visible, enabled, tabStop, false);
        }
        public static DxCheckButton CreateDxCheckButton(int x, ref int y, int w, int h, Control parent, string text, EventHandler click = null,
            DevExpress.XtraEditors.Controls.PaintStyles? paintStyles = null,
            bool isChecked = false,
            Image image = null, string resourceName = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var checkButton = new DxCheckButton() { Bounds = new Rectangle(x, y, w, h) };
            checkButton.StyleController = inst._InputStyle;
            checkButton.Text = text;
            checkButton.Checked = isChecked;
            if (visible.HasValue) checkButton.Visible = visible.Value;
            if (enabled.HasValue) checkButton.Enabled = enabled.Value;
            if (tabStop.HasValue) checkButton.TabStop = tabStop.Value;
            if (paintStyles.HasValue) checkButton.PaintStyle = paintStyles.Value;

            int s = (w < h ? w : h) - 10;
            DxComponent.ApplyImage(checkButton.ImageOptions, image, resourceName, new Size(s, s), true);
            checkButton.ImageOptions.ImageToTextAlignment = ImageAlignToText.LeftCenter;
            checkButton.ImageOptions.ImageToTextIndent = 3;
            checkButton.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Default;

            checkButton.SetToolTip(toolTipTitle ?? text, toolTipText);

            if (click != null) checkButton.Click += click;
            if (parent != null) parent.Controls.Add(checkButton);
            if (shiftY) y = y + checkButton.Height + inst._DetailYSpaceText;

            return checkButton;
        }
        public static DxSimpleButton CreateDxSimpleButton(int x, int y, int w, int h, Control parent, string text, EventHandler click = null,
            DevExpress.XtraEditors.Controls.PaintStyles? paintStyles = null,
            Image image = null, string resourceName = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null)
        {
            return CreateDxSimpleButton(x, ref y, w, h, parent, text, click,
                paintStyles,
                image, resourceName,
                toolTipTitle, toolTipText,
                visible, enabled, tabStop, false);
        }
        public static DxSimpleButton CreateDxSimpleButton(int x, ref int y, int w, int h, Control parent, string text, EventHandler click = null,
            DevExpress.XtraEditors.Controls.PaintStyles? paintStyles = null,
            Image image = null, string resourceName = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var simpleButton = new DxSimpleButton() { Bounds = new Rectangle(x, y, w, h) };
            simpleButton.StyleController = inst._InputStyle;
            simpleButton.Text = text;
            if (visible.HasValue) simpleButton.Visible = visible.Value;
            if (enabled.HasValue) simpleButton.Enabled = enabled.Value;
            if (tabStop.HasValue) simpleButton.TabStop = tabStop.Value;
            if (paintStyles.HasValue) simpleButton.PaintStyle = paintStyles.Value;

            int s = (w < h ? w : h) - 10;
            DxComponent.ApplyImage(simpleButton.ImageOptions, image, resourceName, new Size(s, s), true);
            simpleButton.ImageOptions.ImageToTextAlignment = ImageAlignToText.LeftCenter;
            simpleButton.ImageOptions.ImageToTextIndent = 3;
            simpleButton.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Default;

            simpleButton.SetToolTip(toolTipTitle ?? text, toolTipText);

            if (click != null) simpleButton.Click += click;
            if (parent != null) parent.Controls.Add(simpleButton);
            if (shiftY) y = y + simpleButton.Height + inst._DetailYSpaceText;

            return simpleButton;
        }
        public static DxSimpleButton CreateDxMiniButton(int x, int y, int w, int h, Control parent, EventHandler click = null,
            Image image = null, string resourceName = null,
            Image hotImage = null, string hotResourceName = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null,
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
            miniButton.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light;

            DxComponent.ApplyImage(miniButton.ImageOptions, image, resourceName, new Size(w - 4, h - 4), true);

            miniButton.Padding = new Padding(0);
            miniButton.Margin = new Padding(0);

            miniButton.SetToolTip(toolTipTitle, toolTipText);

            if (click != null) miniButton.Click += click;
            if (parent != null) parent.Controls.Add(miniButton);

            return miniButton;
        }
        /// <summary>
        /// Vytvoří a vrátí standardní ToolTipController
        /// </summary>
        /// <returns></returns>
        public static ToolTipController CreateToolTipController()
        {
            ToolTipController toolTipController = new ToolTipController();

            return toolTipController;
        }
        /// <summary>
        /// Vytvoří a vrátí standardní SuperToolTip pro daný titulek a text
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static SuperToolTip CreateDxSuperTip(string title, string text)
        {
            if (String.IsNullOrEmpty(title) && String.IsNullOrEmpty(text)) return null;

            var superTip = new DevExpress.Utils.SuperToolTip();
            if (title != null) superTip.Items.AddTitle(title);
            superTip.Items.Add(text);

            return superTip;
        }
        #endregion
        #region Factory metody pro tvorbu komponent DataFormu
        #region Public static rozhraní
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormControl(IDataFormItem dataFormItem) { return Instance._CreateDataFormControl(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu Label pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormLabel(IDataFormItem dataFormItem) { return Instance._CreateDataFormLabel(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu TextBox pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormTextBox(IDataFormItem dataFormItem) { return Instance._CreateDataFormTextBox(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu EditBox pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormEditBox(IDataFormItem dataFormItem) { return Instance._CreateDataFormEditBox(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu SpinnerBox pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormSpinnerBox(IDataFormItem dataFormItem) { return Instance._CreateDataFormSpinnerBox(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu CheckBox pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormCheckBox(IDataFormItem dataFormItem) { return Instance._CreateDataFormCheckBox(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu BreadCrumb pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormBreadCrumb(IDataFormItem dataFormItem) { return Instance._CreateDataFormBreadCrumb(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu ComboBoxList pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormComboBoxList(IDataFormItem dataFormItem) { return Instance._CreateDataFormComboBoxList(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu ComboBoxEdit pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormComboBoxEdit(IDataFormItem dataFormItem) { return Instance._CreateDataFormComboBoxEdit(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu ListView pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormListView(IDataFormItem dataFormItem) { return Instance._CreateDataFormListView(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu TreeView pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormTreeView(IDataFormItem dataFormItem) { return Instance._CreateDataFormTreeView(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu RadioButtonBox pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormRadioButtonBox(IDataFormItem dataFormItem) { return Instance._CreateDataFormRadioButtonBox(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu Button pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormButton(IDataFormItem dataFormItem) { return Instance._CreateDataFormButton(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu CheckButton pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormCheckButton(IDataFormItem dataFormItem) { return Instance._CreateDataFormCheckButton(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu DropDownButton pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormDropDownButton(IDataFormItem dataFormItem) { return Instance._CreateDataFormDropDownButton(dataFormItem); }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu Image pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        public static Control CreateDataFormImage(IDataFormItem dataFormItem) { return Instance._CreateDataFormImage(dataFormItem); }
        #endregion
        #region private rozcestník a výkonné metody
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormControl(IDataFormItem dataFormItem) 
        {
            switch (dataFormItem.ItemType)
            {
                case DataFormItemType.Label: return _CreateDataFormLabel(dataFormItem);
                case DataFormItemType.TextBox: return _CreateDataFormTextBox(dataFormItem);
                case DataFormItemType.EditBox: return _CreateDataFormEditBox(dataFormItem);
                case DataFormItemType.SpinnerBox: return _CreateDataFormSpinnerBox(dataFormItem);
                case DataFormItemType.CheckBox: return _CreateDataFormCheckBox(dataFormItem);
                case DataFormItemType.BreadCrumb: return _CreateDataFormBreadCrumb(dataFormItem);
                case DataFormItemType.ComboBoxList: return _CreateDataFormComboBoxList(dataFormItem);
                case DataFormItemType.ComboBoxEdit: return _CreateDataFormComboBoxEdit(dataFormItem);
                case DataFormItemType.ListView: return _CreateDataFormListView(dataFormItem);
                case DataFormItemType.TreeView: return _CreateDataFormTreeView(dataFormItem);
                case DataFormItemType.RadioButtonBox: return _CreateDataFormRadioButtonBox(dataFormItem);
                case DataFormItemType.Button: return _CreateDataFormButton(dataFormItem);
                case DataFormItemType.CheckButton: return _CreateDataFormCheckButton(dataFormItem);
                case DataFormItemType.DropDownButton: return _CreateDataFormDropDownButton(dataFormItem);
                case DataFormItemType.Image: return _CreateDataFormImage(dataFormItem);
            }
            throw new ArgumentException();

        }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu Label pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormLabel(IDataFormItem dataFormItem)
        {
            var bounds = dataFormItem.Bounds;
            var label = CreateDxLabel(bounds.X, bounds.Y, bounds.Width, null, dataFormItem.Text,
                dataFormItem.LabelStyle, dataFormItem.LabelWordWrap, dataFormItem.LabelAutoSize, dataFormItem.LabelHAlignment, 
                dataFormItem.Visible);
            return label;
        }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu TextBox pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormTextBox(IDataFormItem dataFormItem) 
        {
            var bounds = dataFormItem.Bounds;
            var textEdit = CreateDxTextEdit(bounds.X, bounds.Y, bounds.Width, null, null,
                dataFormItem.TextMaskType, dataFormItem.TextEditMask, dataFormItem.TextUseMaskAsDisplayFormat,
                dataFormItem.ToolTipTitle, dataFormItem.ToolTipText, dataFormItem.Visible, dataFormItem.ReadOnly, dataFormItem.TabStop);
            return textEdit;
        }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu EditBox pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormEditBox(IDataFormItem dataFormItem) 
        {
            var bounds = dataFormItem.Bounds;
            var memoEdit = CreateDxMemoEdit(bounds.X, bounds.Y, bounds.Width, bounds.Height, null, null,
                dataFormItem.ToolTipTitle, dataFormItem.ToolTipText, dataFormItem.Visible, dataFormItem.ReadOnly, dataFormItem.TabStop);
            return memoEdit;
        }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu SpinnerBox pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormSpinnerBox(IDataFormItem dataFormItem)
        {
            var bounds = dataFormItem.Bounds;
            var checkBox = CreateDxSpinEdit(bounds.X, bounds.Y, bounds.Width, null, null,
                dataFormItem.SpinMinValue, dataFormItem.SpinMaxValue, dataFormItem.SpinIncrement, dataFormItem.TextEditMask, dataFormItem.SpinStyle,
                dataFormItem.ToolTipTitle, dataFormItem.ToolTipText, dataFormItem.Visible, dataFormItem.ReadOnly, dataFormItem.TabStop);
            return checkBox;
        }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu CheckBox pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormCheckBox(IDataFormItem dataFormItem)
        {
            var bounds = dataFormItem.Bounds;
            var checkBox = CreateDxCheckEdit(bounds.X, bounds.Y, bounds.Width, null, dataFormItem.Text, null,
                dataFormItem.CheckBoxStyle, dataFormItem.BorderStyle, dataFormItem.LabelHAlignment,
                dataFormItem.ToolTipTitle, dataFormItem.ToolTipText, dataFormItem.Visible, dataFormItem.ReadOnly, dataFormItem.TabStop);
            return checkBox;
        }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu BreadCrumb pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormBreadCrumb(IDataFormItem dataFormItem) { return null; }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu ComboBoxList pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormComboBoxList(IDataFormItem dataFormItem) { return null; }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu ComboBoxEdit pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormComboBoxEdit(IDataFormItem dataFormItem) { return null; }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu ListView pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormListView(IDataFormItem dataFormItem) { return null; }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu TreeView pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormTreeView(IDataFormItem dataFormItem) { return null; }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu RadioButtonBox pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormRadioButtonBox(IDataFormItem dataFormItem) { return null; }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu Button pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormButton(IDataFormItem dataFormItem) 
        {
            var bounds = dataFormItem.Bounds;
            var checkBox = CreateDxSimpleButton(bounds.X, bounds.Y, bounds.Width, bounds.Height, null, dataFormItem.Text, null,
                DevExpress.XtraEditors.Controls.PaintStyles.Default,
                null, dataFormItem.ButtonImageName,
                dataFormItem.ToolTipTitle, dataFormItem.ToolTipText, dataFormItem.Visible, dataFormItem.Enabled, dataFormItem.TabStop);
            return checkBox;
        }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu CheckButton pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormCheckButton(IDataFormItem dataFormItem)
        {
            var bounds = dataFormItem.Bounds;
            var checkBox = CreateDxCheckButton(bounds.X, bounds.Y, bounds.Width, bounds.Height, null, dataFormItem.Text, null,
                DevExpress.XtraEditors.Controls.PaintStyles.Default,
                false,
                null, dataFormItem.ButtonImageName,
                dataFormItem.ToolTipTitle, dataFormItem.ToolTipText, dataFormItem.Visible, dataFormItem.Enabled, dataFormItem.TabStop);
            return checkBox;
        }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu DropDownButton pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormDropDownButton(IDataFormItem dataFormItem) { return null; }
        /// <summary>
        /// Vygeneruje a vrátí vizuální Control typu Image pro danou definici prvku DataForm
        /// </summary>
        /// <param name="dataFormItem"></param>
        /// <returns></returns>
        private Control _CreateDataFormImage(IDataFormItem dataFormItem) { return null; }
        #endregion
        #endregion
        #region LogText_ logování
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
        /// Token, který se očekává v textu v metodě <see cref="LogAddLineTime(string, long)"/>, za který se dosaví uplynulý čas v sekundách
        /// </summary>
        public static string LogTokenTimeSec { get { return "{SEC}"; } }
        /// <summary>
        /// Token, který se očekává v textu v metodě <see cref="LogAddLineTime(string, long)"/>, za který se dosaví uplynulý čas v milisekundách
        /// </summary>
        public static string LogTokenTimeMilisec { get { return "{MILISEC}"; } }
        /// <summary>
        /// Token, který se očekává v textu v metodě <see cref="LogAddLineTime(string, long)"/>, za který se dosaví uplynulý čas v mikrosekundách
        /// </summary>
        public static string LogTokenTimeMicrosec { get { return "{MICROSEC}"; } }
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
                _LogSB.Clear();
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
            }
            _LogLastWriteTicks = _LogWatch.ElapsedTicks;
            RunLogTextChanged();
        }
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
        /// Vrátí new instanci <see cref="LinearGradientBrush"/> pro dané zadání. Interně řeší problém WinForms, kdy pro určité orientace / úhly gradientu dochází k posunu prostoru.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <param name="targetSide"></param>
        /// <returns></returns>
        public static LinearGradientBrush PaintCreateBrushForGradient(Rectangle bounds, Color color1, Color color2, RectangleSide targetSide)
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
        /// <summary>
        /// Okamžitě vrací SolidBrush pro kreslení danou barvou. Nesmí být Disposován!
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static SolidBrush PaintGetSolidBrush(Color color) { return Instance._GetSolidBrush(color); }
        /// <summary>
        /// Okamžitě vrací Pen pro kreslení danou barvou. Nesmí být Disposován!
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen PaintGetPen(Color color) { return Instance._GetPen(color); }

        private SolidBrush _GetSolidBrush(Color color)
        {
            _SolidBrush.Color = color;
            return _SolidBrush;
        }
        private Pen _GetPen(Color color)
        {
            _Pen.Color = color;
            return _Pen;
        }
        private void _InitDrawing()
        {
            _SolidBrush = new SolidBrush(Color.White);
            _Pen = new Pen(Color.Black);
        }
        private SolidBrush _SolidBrush;
        private Pen _Pen;
        #endregion
        #region ImageResource
        /// <summary>
        /// Vrací setříděný seznam DevExpress resources
        /// </summary>
        /// <param name="addPng">Akceptovat bitmapy (PNG)</param>
        /// <param name="addSvg">Akceptovat vektory (SVG)</param>
        /// <returns></returns>
        public static string[] GetResourceKeys(bool addPng = true, bool addSvg = true)
        {
            return Instance._GetResourceKeys(addPng, addSvg);
        }
        /// <summary>
        /// Zkusí najít daný zdroj v <see cref="_ImageResourceDictionary"/> (seznam systémových zdrojů = ikon) a určit jeho příponu. Vrací true = nalezeno.
        /// Přípona je trim(), lower() a bez tečky na začátku, například: "png", "svg" atd.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static bool TryGetResourceExtension(string resourceName, out string extension)
        {
            return Instance._TryGetResourceExtension(resourceName, out extension);
        }
        /// <summary>
        /// Vrací setříděný seznam DevExpress resources
        /// </summary>
        /// <param name="addPng">Akceptovat bitmapy (PNG)</param>
        /// <param name="addSvg">Akceptovat vektory (SVG)</param>
        /// <returns></returns>
        private string[] _GetResourceKeys(bool addPng, bool addSvg)
        {
            if (!addPng && !addSvg) return new string[0];

            var keyList = _ImageResourceCache.GetAllResourceKeys()
                .Where(k => (addPng && k.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) || (addSvg && k.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            keyList.Sort();

            return keyList.ToArray();
        }
        /// <summary>
        /// Vrátí Image (bitmapu) pro daný název DevExpress zdroje.
        /// Vstupem může být SVG i PNG zdroj.
        /// <para/>
        /// Pro SVG obrázek je vhodné:
        /// 1. určit <paramref name="optimalSvgSize"/>, pak bude Image renderován exaktně na zadaný rozměr.
        /// 2. předat i paletu <paramref name="svgPalette"/>, tím bude SVG obrázek přizpůsoben danému skinu a stavu.
        /// 3. Pokud nebude předána paleta, lze zadat alespoň stav objektu <paramref name="svgState"/> (default = Normal), pak bude použit aktuální skin a daný stav objektu.
        /// </summary>
        /// <param name="resourceName">Název zdroje, zdroje jsou k dispozici v <see cref="GetResourceKeys(bool, bool)"/></param>
        /// <param name="maxSize"></param>
        /// <param name="optimalSvgSize">Cílová velikost, použije se pouze pro vykreslení SVG Image</param>
        /// <param name="svgPalette">Paleta pro vykreslení SVG Image</param>
        /// <param name="svgState">Stav objektu pro vykreslení SVG Image, implicitní je <see cref="DevExpress.Utils.Drawing.ObjectState.Normal"/></param>
        /// <returns></returns>
        public static Image GetImageFromResource(string resourceName, 
            Size? maxSize = null, Size? optimalSvgSize = null, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            return GetImageFromResource(resourceName, out Size size,
                maxSize, optimalSvgSize, svgPalette, svgState);
        }
        /// <summary>
        /// Vrátí Image (bitmapu) pro daný název DevExpress zdroje.
        /// Vstupem může být SVG i PNG zdroj.
        /// <para/>
        /// Pro SVG obrázek je vhodné:
        /// 1. určit <paramref name="optimalSvgSize"/>, pak bude Image renderován exaktně na zadaný rozměr.
        /// 2. předat i paletu <paramref name="svgPalette"/>, tím bude SVG obrázek přizpůsoben danému skinu a stavu.
        /// 3. Pokud nebude předána paleta, lze zadat alespoň stav objektu <paramref name="svgState"/> (default = Normal), pak bude použit aktuální skin a daný stav objektu.
        /// </summary>
        /// <param name="resourceName">Název zdroje, zdroje jsou k dispozici v <see cref="GetResourceKeys(bool, bool)"/></param>
        /// <param name="size">Výstup konkrétní velikosti, odráží velikost bitmapy, nebo <paramref name="optimalSvgSize"/> pro SVG, je oříznuto na <paramref name="maxSize"/></param>
        /// <param name="maxSize"></param>
        /// <param name="optimalSvgSize">Cílová velikost, použije se pouze pro vykreslení SVG Image</param>
        /// <param name="svgPalette">Paleta pro vykreslení SVG Image</param>
        /// <param name="svgState">Stav objektu pro vykreslení SVG Image, implicitní je <see cref="DevExpress.Utils.Drawing.ObjectState.Normal"/></param>
        /// <returns></returns>
        public static Image GetImageFromResource(string resourceName, out Size size,
            Size? maxSize = null, Size? optimalSvgSize = null, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            return Instance._GetImageFromResource(resourceName, out size,
                maxSize, optimalSvgSize, svgPalette, svgState);
        }
        /// <summary>
        /// Vrátí Image (bitmapu) pro daný název DevExpress zdroje.
        /// Vstupem může být SVG i PNG zdroj.
        /// <para/>
        /// Pro SVG obrázek je vhodné:
        /// 1. určit <paramref name="optimalSvgSize"/>, pak bude Image renderován exaktně na zadaný rozměr.
        /// 2. předat i paletu <paramref name="svgPalette"/>, tím bude SVG obrázek přizpůsoben danému skinu a stavu.
        /// 3. Pokud nebude předána paleta, lze zadat alespoň stav objektu <paramref name="svgState"/> (default = Normal), pak bude použit aktuální skin a daný stav objektu.
        /// </summary>
        /// <param name="resourceName">Název zdroje, zdroje jsou k dispozici v <see cref="GetResourceKeys(bool, bool)"/></param>
        /// <param name="size">Výstup konkrétní velikosti, odráží velikost bitmapy, nebo <paramref name="optimalSvgSize"/> pro SVG, je oříznuto na <paramref name="maxSize"/></param>
        /// <param name="maxSize"></param>
        /// <param name="optimalSvgSize">Cílová velikost, použije se pouze pro vykreslení SVG Image</param>
        /// <param name="svgPalette">Paleta pro vykreslení SVG Image</param>
        /// <param name="svgState">Stav objektu pro vykreslení SVG Image, implicitní je <see cref="DevExpress.Utils.Drawing.ObjectState.Normal"/></param>
        /// <returns></returns>
        private Image _GetImageFromResource(string resourceName, out Size size,
            Size? maxSize, Size? optimalSvgSize, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette, DevExpress.Utils.Drawing.ObjectState? svgState)
        {
            System.Drawing.Image image = null;
            size = new Size(32, 32);
            if (!String.IsNullOrEmpty(resourceName))
            {
                try
                {
                    if (_IsImageNameSvg(resourceName))
                    {
                        if (svgPalette == null)
                            svgPalette = GetSvgPalette(DevExpress.LookAndFeel.UserLookAndFeel.Default, svgState);
                        if (optimalSvgSize.HasValue)
                            size = optimalSvgSize.Value;
                        else if (maxSize.HasValue)
                            size = maxSize.Value;
                        _ImageResourceRewindStream(resourceName);
                        image = _ImageResourceCache.GetSvgImage(resourceName, svgPalette, size);
                    }
                    else
                    {
                        image = _ImageResourceCache.GetImage(resourceName);
                        size = image.Size;
                        if (maxSize.HasValue)
                        {
                            if (maxSize.Value.Width > 0 && size.Width > maxSize.Value.Width) size.Width = maxSize.Value.Width;
                            if (maxSize.Value.Height > 0 && size.Height > maxSize.Value.Height) size.Height = maxSize.Value.Height;
                        }
                    }
                }
                catch (Exception exc)
                {
                    image = null;
                }
            }
            return image;
        }
        /// <summary>
        /// Vrátí SVG paletu [volitelně pro daný skin a pro daný stav objektu]
        /// </summary>
        /// <param name="skinProvider">Cílový skin, implicitně bude použit <see cref="DevExpress.LookAndFeel.UserLookAndFeel.Default"/></param>
        /// <param name="svgState">Stav objektu, implicitní je <see cref="DevExpress.Utils.Drawing.ObjectState.Normal"/></param>
        /// <returns></returns>
        public static DevExpress.Utils.Design.ISvgPaletteProvider GetSvgPalette(DevExpress.Skins.ISkinProvider skinProvider = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            if (skinProvider == null) skinProvider = DevExpress.LookAndFeel.UserLookAndFeel.Default;
            if (!svgState.HasValue) svgState = DevExpress.Utils.Drawing.ObjectState.Normal;
            return DevExpress.Utils.Svg.SvgPaletteHelper.GetSvgPalette(skinProvider, svgState.Value);
        }
        public static DevExpress.Utils.SvgImageCollection SvgImageCollection { get { return Instance._SvgImageCollection; } }
        public static DevExpress.Utils.Svg.SvgImage GetSvgImage(string key) { return Instance._GetSvgImage(key); }
        public static void ApplyImage(ImageCollectionImageOptions imageOptions, Image image = null, string resourceName = null, Size? imageSize = null) { Instance._ApplyImage(imageOptions, image, resourceName, imageSize); }
        private void _ApplyImage(ImageCollectionImageOptions imageOptions, Image image = null, string resourceName = null, Size? imageSize = null)
        {
            if (image != null)
            {
                imageOptions.Image = image;
            }
            else if (!String.IsNullOrEmpty(resourceName))
            {
                try
                {
                    if (_IsImageNameSvg(resourceName))
                    {
                        _ImageResourceRewindStream(resourceName);
                        imageOptions.SvgImage = _ImageResourceCache.GetSvgImage(resourceName);
                        if (imageSize.HasValue) imageOptions.SvgImageSize = imageSize.Value;
                    }
                    else
                    {
                        imageOptions.Image = _ImageResourceCache.GetImage(resourceName);
                    }
                }
                catch { }
            }
        }
        public static void ApplyImage(DevExpress.XtraEditors.SimpleButtonImageOptions imageOptions, Image image = null, string resourceName = null, Size? imageSize = null, bool smallButton = false) { Instance._ApplyImage(imageOptions, image, resourceName, imageSize, smallButton); }
        private void _ApplyImage(DevExpress.XtraEditors.SimpleButtonImageOptions imageOptions, Image image = null, string resourceName = null, Size? imageSize = null, bool smallButton = false)
        {
            if (image != null)
            {
                imageOptions.Image = image;
            }
            else if (!String.IsNullOrEmpty(resourceName))
            {
                try
                {
                    if (_IsImageNameSvg(resourceName))
                    {
                        _ImageResourceRewindStream(resourceName);
                        imageOptions.SvgImage = _ImageResourceCache.GetSvgImage(resourceName);
                        if (imageSize.HasValue) imageOptions.SvgImageSize = imageSize.Value;
                    }
                    else
                    {
                        imageOptions.Image = _ImageResourceCache.GetImage(resourceName);
                    }
                }
                catch { }
            }
            if (smallButton)
            {
                imageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
//                imageOptions.ImageToTextIndent = 0;
  //              imageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.BottomCenter;
            }
        }
        /// <summary>
        /// Vrátí true, pokud dané jméno zdroje končí příponou ".svg"
        /// </summary>
        /// <param name="imageUri"></param>
        /// <returns></returns>
        private static bool _IsImageNameSvg(string imageUri)
        {
            return (!String.IsNullOrEmpty(imageUri) && imageUri.EndsWith(".svg", StringComparison.OrdinalIgnoreCase));
        }
        /// <summary>
        /// Napravuje chybu DevExpress, kdy v <see cref="DevExpress.Images.ImageResourceCache"/> pro SVG zdroje po jejich použití je jejich zdrojový stream na konci, a další použití je tak znemožněno.
        /// </summary>
        /// <param name="resourceName"></param>
        private void _ImageResourceRewindStream(string resourceName)
        {
            if (String.IsNullOrEmpty(resourceName)) return;

            var imageResourceCache = _ImageResourceCache;
            var dictonaryField = imageResourceCache.GetType().GetField("resources", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (dictonaryField == null) return;
            object dictonaryValue = dictonaryField.GetValue(imageResourceCache);

            if (!(dictonaryValue is Dictionary<string, System.IO.Stream> dictionary)) return;
            if (!dictionary.TryGetValue(resourceName, out System.IO.Stream stream)) return;

            var position = stream.Position;
            if (stream.Position > 0L && stream.CanSeek)
                stream.Seek(0L, System.IO.SeekOrigin.Begin);
        }
        private DevExpress.Utils.Svg.SvgImage _GetSvgImage(string key)
        {
            if (String.IsNullOrEmpty(key)) return null;

            if (!key.StartsWith(ImageUriPrefix, StringComparison.OrdinalIgnoreCase)) key = ImageUriPrefix + key;
            var svgImageCollection = _SvgImageCollection;
            if (!svgImageCollection.ContainsKey(key))
                svgImageCollection.Add(key, key);

            return svgImageCollection[key];
        }
        /// <summary>
        /// Prefix pro ImageUri: "image://"
        /// </summary>
        private static string ImageUriPrefix { get { return "image://"; } }
        /// <summary>
        /// Cache systémových image resources
        /// </summary>
        private DevExpress.Images.ImageResourceCache _ImageResourceCache
        {
            get
            {
                if (__ImageResourceCache == null)
                    __ImageResourceCache = DevExpress.Images.ImageResourceCache.Default;
                return __ImageResourceCache;
            }
        }
        private DevExpress.Images.ImageResourceCache __ImageResourceCache;
        /// <summary>
        /// Zkusí najít daný zdroj v <see cref="_ImageResourceDictionary"/> (seznam systémových zdrojů = ikon) a určit jeho příponu. Vrací true = nalezeno.
        /// Přípona je trim(), lower() a bez tečky na začátku, například: "png", "svg" atd.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        private bool _TryGetResourceExtension(string resourceName, out string extension)
        {
            extension = null;
            if (String.IsNullOrEmpty(resourceName)) return false;
            var dictionary = _ImageResourceDictionary;
            var key = resourceName.Trim().ToLower();
            return dictionary.TryGetValue(key, out extension);
        }
        /// <summary>
        /// Dictionary obsahující všechny systémové zdroje (jako Key) 
        /// a jejich normalizovanou příponu (jako Value) ve formě "png", "svg" atd (bez tečky, lower, trim)
        /// </summary>
        private Dictionary<string, string> _ImageResourceDictionary
        {
            get
            {
                if (__ImageResourceDictionary == null)
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    var names = _ImageResourceCache.GetAllResourceKeys();
                    foreach (var name in names)
                    {
                        if (!String.IsNullOrEmpty(name))
                        {
                            string key = name.Trim().ToLower();
                            if (!dict.ContainsKey(key))
                            {
                                string ext = System.IO.Path.GetExtension(key).Trim();
                                if (ext.Length > 0 && ext[0] == '.') ext = ext.Substring(1);
                                dict.Add(key, ext);
                            }
                        }
                    }
                    __ImageResourceDictionary = dict;
                }
                return __ImageResourceDictionary;
            }
        }
        private Dictionary<string, string> __ImageResourceDictionary;
        private DevExpress.Utils.SvgImageCollection _SvgImageCollection 
        { 
            get 
            {
                if (__SvgImageCollection == null)
                    __SvgImageCollection = new SvgImageCollection();
                return __SvgImageCollection;
            }
        }
        private DevExpress.Utils.SvgImageCollection __SvgImageCollection;
        #endregion
        #region Win32Api informace
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
        #endregion
    }
    #endregion
    #region interface
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
    /// </summary>
    public interface IListenerStyleChanged : IListener
    {
        /// <summary>
        /// Metoda je volaná po změně stylu do všech instancí, které se zaregistrovaly pomocí <see cref="DxComponent.RegisterListener"/>
        /// </summary>
        void StyleChanged();
    }

    /// <summary>
    /// Objekt, který chce být informován o změně Zoomu.
    /// Při své iniciaizaci má objekt zavolat <see cref="DxComponent.SubscribeToZoomChange(ISubscriberToZoomChange)"/>.
    /// Pak po změně Zoomu dostane řízení do své metody <see cref="ISubscriberToZoomChange.ZoomChanged"/>.
    /// Objekt se může odregistrovat (typicky v Dispose()) metodou <see cref="DxComponent.UnSubscribeToZoomChange(ISubscriberToZoomChange)"/>, ale není to povinné.
    /// </summary>
    public interface ISubscriberToZoomChange
    {
        /// <summary>
        /// Došlo ke změně Zoomu
        /// </summary>
        void ZoomChanged();
    }
    #endregion
    #region class DrawingExtensions : Extensions metody pro grafické třídy (z namespace System.Drawing)
    /// <summary>
    /// Extensions metody pro grafické třídy (z namespace System.Drawing)
    /// </summary>
    public static class DrawingExtensions
    {
        #region Control
        /// <summary>
        /// Vrátí IDisposable blok, který na svém počátku (při vyvolání této metody) provede control?.Parent.SuspendLayout(), 
        /// a na konci bloku (při Dispose) provede control?.Parent.ResumeLayout(false)
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static IDisposable ScopeSuspendParentLayout(this Control control)
        {
            return new UsingScope(
            (s) =>
            {   // OnBegin (Constructor):
                Control parent = control?.Parent;
                if (parent != null && !parent.IsDisposed)
                {
                    parent.SuspendLayout();
                }
                s.UserData = parent;
            },
            (s) =>
            {   // OnEnd (Dispose):
                Control parent = s.UserData as Control;
                if (parent != null && !parent.IsDisposed)
                {
                    parent.ResumeLayout(false);
                    parent.PerformLayout();
                }
                s.UserData = null;
            }
            );
        }
        /// <summary>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato metoda <see cref="IsSetVisible(Control)"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static bool IsSetVisible(this Control control)
        {
            if (control is null) return false;
            var getState = control.GetType().GetMethod("GetState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic);
            if (getState is null) return false;
            object visible = getState.Invoke(control, new object[] { (int)0x02  /*STATE_VISIBLE*/  });
            return (visible is bool ? (bool)visible : false);
        }
        /// <summary>
        /// Vrátí nejbližšího Parenta požadovaného typu pro this control.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="control"></param>
        /// <returns></returns>
        public static T SearchForParentOfType<T>(this Control control) where T : Control
        {
            Control item = control?.Parent;                // Tímhle řádkem zajistím, že nebudu vracet vstupní objekt, i kdyby byl požadovaného typu = protože hledám Parenta, nikoli sebe.
            while (item != null)
            {
                if (item is T result) return result;
                item = item.Parent;
            }
            return null;
        }
        /// <summary>
        /// Korektně disposuje všechny Child prvky.
        /// </summary>
        /// <param name="control"></param>
        public static void DisposeContent(this Control control)
        {
            if (control == null || control.IsDisposed || control.Disposing) return;

            var childs = control.Controls.OfType<System.Windows.Forms.Control>().ToArray();
            foreach (var child in childs)
            {
                if (child == null || child.IsDisposed || child.Disposing) continue;

                if (child is DevExpress.XtraEditors.XtraScrollableControl xsc)
                {
                    xsc.AutoScroll = false;
                }
                if (child is System.Windows.Forms.ScrollableControl wsc)
                {
                    wsc.AutoScroll = false;
                }

                control.Controls.Remove(child);

                try { child.Dispose(); }
                catch { }
            }
        }
        #endregion
        #region Invoke to GUI: run, get, set
        /// <summary>
        /// Metoda provede danou akci v GUI threadu.
        /// Pokud aktuální thread je GUI thread (tedy this control nepotřebuje invokaci), pak se akce provede nativně v Current threadu.
        /// Jinak se použije synchronní Invoke().
        /// </summary>
        /// <param name="control"></param>
        /// <param name="action"></param>
        public static void RunInGui(this Control control, Action action)
        {
            if (control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        }
        /// <summary>
        /// Metoda vrátí hodnotu z GUI prvku, zajistí si invokaci GUI threadu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="control"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T GetGuiValue<T>(this Control control, Func<T> reader)
        {
            if (control.InvokeRequired)
                return (T)control.Invoke(reader);
            else
                return reader();
        }
        /// <summary>
        /// Metoda vloží do GUI prvku danou hodnotu, zajistí si invokaci GUI threadu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="control"></param>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        public static void SetGuiValue<T>(this Control control, Action<T> writer, T value)
        {
            if (control.InvokeRequired)
                control.Invoke(writer, value);
            else
                writer(value);
        }
        #endregion
        #region Color: Shift
        /// <summary>
        /// Vrací danou barvu s daným posunutím
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="shift">Posunutí barvy</param>
        /// <returns></returns>
        public static Color Shift(this Color root, float shift)
        {
            float r = (float)root.R + shift;
            float g = (float)root.G + shift;
            float b = (float)root.B + shift;
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Vrací danou barvu s daným posunutím
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="shiftR">Posunutí barvy pro složku R</param>
        /// <param name="shiftG">Posunutí barvy pro složku G</param>
        /// <param name="shiftB">Posunutí barvy pro složku B</param>
        /// <returns></returns>
        public static Color Shift(this Color root, float shiftR, float shiftG, float shiftB)
        {
            float r = (float)root.R + shiftR;
            float g = (float)root.G + shiftG;
            float b = (float)root.B + shiftB;
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Vrací danou barvu s daným posunutím
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="shiftA">Posunutí barvy pro složku A</param>
        /// <param name="shiftR">Posunutí barvy pro složku R</param>
        /// <param name="shiftG">Posunutí barvy pro složku G</param>
        /// <param name="shiftB">Posunutí barvy pro složku B</param>
        /// <returns></returns>
        public static Color Shift(this Color root, float shiftA, float shiftR, float shiftG, float shiftB)
        {
            float a = (float)root.A + shiftA;
            float r = (float)root.R + shiftR;
            float g = (float)root.G + shiftG;
            float b = (float)root.B + shiftB;
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrací danou barvu s daným posunutím
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="shift">Posunutí barvy ve struktuře Color: jednotlivé složky nesou offset, kde hodnota 128 odpovídá posunu 0</param>
        /// <returns></returns>
        public static Color Shift(this Color root, Color shift)
        {
            float r = (float)(root.R + shift.R - 128);
            float g = (float)(root.G + shift.G - 128);
            float b = (float)(root.B + shift.B - 128);
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Vrací barvu dle daných složek, přičemž složky (a,r,g,b) omezuje do rozsahu 0 - 255.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static Color GetColor(float a, float r, float g, float b)
        {
            int ac = (a < 0f ? 0 : (a > 255f ? 255 : (int)a));
            int rc = (r < 0f ? 0 : (r > 255f ? 255 : (int)r));
            int gc = (g < 0f ? 0 : (g > 255f ? 255 : (int)g));
            int bc = (b < 0f ? 0 : (b > 255f ? 255 : (int)b));
            return Color.FromArgb(ac, rc, gc, bc);
        }
        #endregion
        #region Color: Change
        /// <summary>
        /// Změní barvu.
        /// Změna (Change) není posun (Shift): shift přičítá / odečítá hodnotu, ale změna hodnotu mění koeficientem.
        /// Pokud je hodnota složky například 170 a koeficient změny 0.25, pak výsledná hodnota je +25% od výchozí hodnoty směrem k maximu (255): 170 + 0.25 * (255 - 170).
        /// Obdobně změna dolů -70% z hodnoty 170 dá výsledek 170 - 0.7 * (170).
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="change">Změna složek</param>
        /// <returns></returns>
        public static Color ChangeColor(this Color root, float change)
        {
            float r = ChangeCC(root.R, change);
            float g = ChangeCC(root.G, change);
            float b = ChangeCC(root.B, change);
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Změní barvu.
        /// Změna (Change) není posun (Shift): shift přičítá / odečítá hodnotu, ale změna hodnotu mění koeficientem.
        /// Pokud je hodnota složky například 170 a koeficient změny 0.25, pak výsledná hodnota je +25% od výchozí hodnoty směrem k maximu (255): 170 + 0.25 * (255 - 170).
        /// Obdobně změna dolů -70% z hodnoty 170 dá výsledek 170 - 0.7 * (170).
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="changeR">Změna složky R</param>
        /// <param name="changeG">Změna složky R</param>
        /// <param name="changeB">Změna složky R</param>
        /// <returns></returns>
        public static Color ChangeColor(this Color root, float changeR, float changeG, float changeB)
        {
            float r = ChangeCC(root.R, changeR);
            float g = ChangeCC(root.G, changeG);
            float b = ChangeCC(root.B, changeB);
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Vrátí složku změněnou koeficientem.
        /// </summary>
        /// <param name="colorComponent"></param>
        /// <param name="change"></param>
        /// <returns></returns>
        private static float ChangeCC(int colorComponent, float change)
        {
            float result = (float)colorComponent;
            if (change > 0f)
            {
                result = result + (change * (255f - result));
            }
            else if (change < 0f)
            {
                result = result - (-change * result);
            }
            return result;
        }
        #endregion
        #region Color: Morph
        /// <summary>
        /// Vrací barvu, která je výsledkem interpolace mezi barvou this a barvou other, 
        /// přičemž od barvy this se liší poměrem morph.
        /// Poměr (morph): 0=vrací se výchozí barva (this).
        /// Poměr (morph): 1=vrací se barva cílová (other).
        /// Poměr může být i větší než 1 (pak je výsledek ještě za cílovou barvou other),
        /// anebo může být záporný (pak výsledkem je barva na opačné straně než je other).
        /// Hodnota Alpha (=opacity = průhlednost) kanálu se přebírá z this barvy a Morphingem se nemění.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="other">Cílová barva</param>
        /// <param name="morph">Poměr morph (0=vrátí this, 1=vrátí other, hodnota může být záporná i větší než 1f)</param>
        /// <returns></returns>
        public static Color Morph(this Color root, Color other, float morph)
        {
            if (morph == 0f) return root;
            float a = root.A;
            float r = GetMorph(root.R, other.R, morph);
            float g = GetMorph(root.G, other.G, morph);
            float b = GetMorph(root.B, other.B, morph);
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrací barvu, která je výsledkem interpolace mezi barvou this a barvou other, 
        /// přičemž od barvy this se liší poměrem morph.
        /// Poměr morph zde není zadán explicitně, ale je dán hodnotou Alpha kanálu v barvě other (kde 0 odpovídá morph = 0, a 255 odpovídá 1).
        /// Jinými slovy, barva this se transformuje do barvy other natolik výrazně, jak výrazně je barva other viditelná (neprůhledná).
        /// Nelze tedy provádět Morph na opačnou stranu (morph nebude nikdy záporné) ani s přesahem za cílovou barvu (morph nebude nikdy vyšší než 1).
        /// Poměr (Alpha kanál barvy other): 0=vrací se výchozí barva (this).
        /// Poměr (Alpha kanál barvy other): 255=vrací se barva cílová (other).
        /// Poměr tedy nemůže být menší než 0 nebo větší než 1 (255).
        /// Hodnota Alpha výsledné barvy (=opacity = průhlednost) se přebírá z Alpha kanálu this barvy, a tímto Morphingem se nijak nemění.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="other">Cílová barva</param>
        /// <returns></returns>
        public static Color Morph(this Color root, Color other)
        {
            if (other.A == 0) return root;
            float morph = ((float)other.A) / 255f;
            float a = root.A;
            float r = GetMorph(root.R, other.R, morph);
            float g = GetMorph(root.G, other.G, morph);
            float b = GetMorph(root.B, other.B, morph);
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrátí složku barvy vzniklou morphingem = interpolací.
        /// </summary>
        /// <param name="root">Výchozí složka</param>
        /// <param name="other">Cílová složka</param>
        /// <param name="morph">Poměr morph (0=vrátí this, 1=vrátí other, hodnota může být záporná i větší než 1f)</param>
        /// <returns></returns>
        private static float GetMorph(float root, float other, float morph)
        {
            float dist = other - root;
            return root + morph * dist;
        }
        #endregion
        #region Color: Contrast
        /// <summary>
        /// Vrátí kontrastní barvu černou nebo bílou k barvě this.
        /// Tato metoda vrací barvu černou nebo bílou, která je dobře viditelná na pozadí dané barvy (this).
        /// Tato metoda pracuje s fyziologickým jasem každé složky barvy zvlášť (například složka G se jeví jasnější než B, složka R má svůj jas někde mezi nimi).
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <returns></returns>
        public static Color Contrast(this Color root)
        {
            // Vypočítám souhrnný jas všech složek se zohledněním koeficientu jejich barevného jasu:
            float rgb = 1.0f * (float)root.R +
                        1.4f * (float)root.G +
                        0.7f * (float)root.B;
            return (rgb >= 395 ? Color.Black : Color.White);      // Součet složek je 0 až 790.5, střed kontrastu je 1/2 = 395
        }
        /// <summary>
        /// Vrátí barvu, která je kontrastní vůči barvě this.
        /// Kontrastní barva leží o dané množství barvy směrem k protilehlé barvě (vždy na opačnou stranu od hodnoty 128), v každé složce zvlášť.
        /// Například ke složce s hodnotou 160 je kontrastní barvou o 32 hodnota (160-32) = 128, k hodnotě 100 o 32 je kontrastní (100+32) = 132.
        /// Tedy kontrastní barva k barvě ((rgb: 64,96,255), contrast=32) je barva: rgb(96,128,223) = (64+32, 96+32, 255-32).
        /// Složka A se nemění.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="contrast">Míra kontrastu</param>
        /// <returns></returns>
        public static Color Contrast(this Color root, int contrast)
        {
            float a = root.A;
            float r = GetContrast(root.R, contrast);
            float g = GetContrast(root.G, contrast);
            float b = GetContrast(root.B, contrast);
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrátí barvu, která je kontrastní vůči barvě this.
        /// Kontrastní barva leží o dané množství barvy směrem k protilehlé barvě (vždy na opačnou stranu od hodnoty 128), v každé složce zvlášť.
        /// Například ke složce s hodnotou 160 je kontrastní barvou o 32 hodnota (160-32) = 128, k hodnotě 100 o 32 je kontrastní (100+32) = 132.
        /// Tedy kontrastní barva k barvě ((rgb: 64,96,255), contrast=32) je barva: rgb(96,128,223) = (64+32, 96+32, 255-32).
        /// Složka A se nemění.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="contrastR">Míra kontrastu ve složce R</param>
        /// <param name="contrastG">Míra kontrastu ve složce G</param>
        /// <param name="contrastB">Míra kontrastu ve složce B</param>
        /// <returns></returns>
        public static Color Contrast(this Color root, int contrastR, int contrastG, int contrastB)
        {
            float a = root.A;
            float r = GetContrast(root.R, contrastR);
            float g = GetContrast(root.G, contrastG);
            float b = GetContrast(root.B, contrastB);
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrací kontrastní složku
        /// </summary>
        /// <param name="root"></param>
        /// <param name="contrast"></param>
        /// <returns></returns>
        private static float GetContrast(int root, int contrast)
        {
            return (root <= 128 ? root + contrast : root - contrast);
        }
        #endregion
        #region Color: GrayScale
        /// <summary>
        /// Vrátí danou barvu odbarvenou do černo-šedo-bílé stupnice.
        /// Hodnotu Alpha ponechává.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <returns></returns>
        public static Color GrayScale(this Color root)
        {
            // Vypočítám souhrnný jas všech složek se zohledněním koeficientu jejich barevného jasu:
            float rgb = 1.0f * (float)root.R +
                        1.4f * (float)root.G +
                        0.7f * (float)root.B;              // Součet složek je 0 až 790.5;
            int g = (int)(Math.Round((255f * (rgb / 790.5f)), 0));
            return Color.FromArgb(root.A, g, g, g);
        }
        /// <summary>
        /// Vrátí danou barvu odbarvenou do černo-šedo-bílé stupnice s daným poměrem odbarvení.
        /// Hodnotu Alpha ponechává.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="ratio">Poměr odbarvení</param>
        /// <returns></returns>
        public static Color GrayScale(this Color root, float ratio)
        {
            Color gray = root.GrayScale();
            return root.Morph(gray, ratio);
        }
        #endregion
        #region Color: Opacity
        /// <summary>
        /// Do dané barvy (this) vloží danou hodnotu Alpha (parametr opacity), výsledek vrátí.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="opacity">Průhlednost v hodnotě 0-255 (nebo null = neměnit)</param>
        /// <returns></returns>
        public static Color SetOpacity(this Color root, Int32? opacity)
        {
            if (!opacity.HasValue) return root;
            int alpha = (opacity.Value < 0 ? 0 : (opacity.Value > 255 ? 255 : opacity.Value));
            return Color.FromArgb(alpha, root);
        }
        /// <summary>
        /// Do dané barvy (this) vloží danou hodnotu Alpha (parametr opacityRatio), výsledek vrátí.
        /// Hodnota opacityRatio : Průhlednost v hodnotě ratio 0.0 ÷1.0 (nebo null = neměnit)
        /// </summary>
        /// <param name="root"></param>
        /// <param name="opacityRatio">Průhlednost v hodnotě ratio 0.0 ÷1.0 (nebo null = neměnit)</param>
        /// <returns></returns>
        public static Color SetOpacity(this Color root, float? opacityRatio)
        {
            if (!opacityRatio.HasValue) return root;
            return SetOpacity(root, (int)(255f * opacityRatio.Value));
        }
        /// <summary>
        /// Na danou barvu aplikuje všechny dodané hodnoty průhlednosti, při akceptování i původní průhlednosti.
        /// Aplikování se provádní vzájemným násobením hodnoty (opacity/255), což je poměr (ratio) průhlednosti.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="opacities"></param>
        /// <returns></returns>
        public static Color ApplyOpacity(this Color root, params Int32?[] opacities)
        {
            float alpha = _GetColorRatio(root.A);
            foreach (Int32? opacity in opacities)
            {
                if (opacity.HasValue)
                    alpha = alpha * _GetColorRatio(opacity.Value);
            }
            return SetOpacity(root, (int)(Math.Round(255f * alpha, 0)));
        }
        /// <summary>
        /// Vrací ratio { 0.00 až 1.00 } z hodnoty { 0 až 255 }.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static float _GetColorRatio(int value)
        {
            if (value < 0) return 0f;
            if (value >= 255) return 1f;
            return (float)value / 255f;
        }
        /// <summary>
        /// Na danou barvu aplikuje všechny dodané hodnoty průhlednosti, při akceptování i původní průhlednosti.
        /// Aplikování se provádní vzájemným násobením hodnoty (ratio), což je poměr (ratio) průhlednosti.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="ratios"></param>
        /// <returns></returns>
        public static Color ApplyOpacity(this Color root, params float?[] ratios)
        {
            float alpha = _GetColorRatio(root.A);
            foreach (float? ratio in ratios)
            {
                if (ratio.HasValue)
                    alpha = alpha * _GetColorRatio(ratio.Value);
            }
            return SetOpacity(root, (int)(Math.Round(255f * alpha, 0)));
        }
        /// <summary>
        /// Zarovná dané ratio do rozmezí { 0.00 až 1.00 }.
        /// </summary>
        /// <param name="ratio"></param>
        /// <returns></returns>
        private static float _GetColorRatio(float ratio)
        {
            return (ratio < 0f ? 0f : (ratio > 1f ? 1f : ratio));
        }
        /// <summary>
        /// Metoda vrátí novou instanci barvy this, kde její Alpha je nastavena na daný poměr (transparent) původní hodnoty.
        /// Tedy zadáním například: <see cref="Color.BlueViolet"/>.<see cref="CreateTransparent(Color, float)"/>(0.75f) 
        /// dojde k vytvoření a vrácení barvy s hodnotou Alpha = 75% = 192, od barvy BlueViolet (která je #FF8A2BE2), tedy výsledek bude #C08A2BE2.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        public static Color CreateTransparent(this Color root, float alpha)
        {
            int a = (int)(((float)root.A) * alpha);
            a = (a < 0 ? 0 : (a > 255 ? 255 : a));
            return Color.FromArgb(a, root.R, root.G, root.B);
        }
        #endregion
        #region Point, PointF: Add/Sub
        /// <summary>
        /// Returns a point = basePoint + addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="addPoint"></param>
        /// <returns></returns>
        public static Point Add(this Point basePoint, Point addPoint)
        {
            return new Point(basePoint.X + addPoint.X, basePoint.Y + addPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint + (addpoint X, Y)
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="addX"></param>
        /// <param name="addY"></param>
        /// <returns></returns>
        public static Point Add(this Point basePoint, int addX, int addY)
        {
            return new Point(basePoint.X + addX, basePoint.Y + addY);
        }
        /// <summary>
        /// Returns a point = basePoint + addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="addPoint"></param>
        /// <returns></returns>
        public static PointF Add(this PointF basePoint, PointF addPoint)
        {
            return new PointF(basePoint.X + addPoint.X, basePoint.Y + addPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint + addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="addPoint"></param>
        /// <returns></returns>
        public static PointF Add(this Point basePoint, PointF addPoint)
        {
            return new PointF((float)basePoint.X + addPoint.X, (float)basePoint.Y + addPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint - addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="subPoint"></param>
        /// <returns></returns>
        public static Point Sub(this Point basePoint, Point subPoint)
        {
            return new Point(basePoint.X - subPoint.X, basePoint.Y - subPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint - (subpoint X, Y)
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="subX"></param>
        /// <param name="subY"></param>
        /// <returns></returns>
        public static Point Sub(this Point basePoint, int subX, int subY)
        {
            return new Point(basePoint.X - subX, basePoint.Y - subY);
        }
        /// <summary>
        /// Returns a point = basePoint - addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="subPoint"></param>
        /// <returns></returns>
        public static PointF Sub(this PointF basePoint, PointF subPoint)
        {
            return new PointF(basePoint.X - subPoint.X, basePoint.Y - subPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint - addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="subPoint"></param>
        /// <returns></returns>
        public static PointF Sub(this Point basePoint, PointF subPoint)
        {
            return new PointF((float)basePoint.X - subPoint.X, (float)basePoint.Y - subPoint.Y);
        }
        #endregion
        #region souřadnice: IsVisible()
        /// <summary>
        /// Vrátí true pokud this objekt může být svou velikostí viditelný (šířka a výška je větší než 0)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsVisible(this Size value) { return (value.Width > 0 && value.Height > 0); }
        /// <summary>
        /// Vrátí true pokud this objekt může být svou velikostí viditelný (šířka a výška je větší než 0)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsVisible(this SizeF value) { return (value.Width > 0f && value.Height > 0f); }
        /// <summary>
        /// Vrátí true pokud this objekt může být svou velikostí viditelný (šířka a výška je větší než 0)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsVisible(this Rectangle value) { return (value.Width > 0 && value.Height > 0); }
        /// <summary>
        /// Vrátí true pokud this objekt může být svou velikostí viditelný (šířka a výška je větší než 0)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsVisible(this RectangleF value) { return (value.Width > 0f && value.Height > 0f); }
        #endregion
        #region Size, SizeF, Rectangle, RectangleF: zooming
        /// <summary>
        /// Zvětší danou velikost o daný rozměr na každé straně = velikost se zvětší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="enlarge">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Enlarge(this SizeF size, float enlarge)
        { return new SizeF(size.Width + 2f * enlarge, size.Height + 2f * enlarge); }
        /// <summary>
        /// Zvětší danou velikost o daný rozměr na každé straně = velikost se zvětší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="enlargeWidth">Coefficient X.</param>
        /// <param name="enlargeHeight">Coefficient Y.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Enlarge(this SizeF size, float enlargeWidth, float enlargeHeight)
        { return new SizeF(size.Width + 2f * enlargeWidth, size.Height + 2f * enlargeHeight); }
        /// <summary>
        /// Zvětší danou velikost o daný rozměr na každé straně = velikost se zvětší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="enlarge">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Enlarge(this SizeF size, SizeF enlarge)
        { return new SizeF(size.Width + 2f * enlarge.Width, size.Height + 2f * enlarge.Height); }
        /// <summary>
        /// Zmenší danou velikost o daný rozměr na každé straně = velikost se zmenší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="reduce">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Reduce(this SizeF size, float reduce)
        { return new SizeF(size.Width - 2f * reduce, size.Height - 2f * reduce); }
        /// <summary>
        /// Zmenší danou velikost o daný rozměr na každé straně = velikost se zmenší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="reduceWidth">Coefficient X.</param>
        /// <param name="reduceHeight">Coefficient Y.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Reduce(this SizeF size, float reduceWidth, float reduceHeight)
        { return new SizeF(size.Width - 2f * reduceWidth, size.Height - 2f * reduceHeight); }
        /// <summary>
        /// Zmenší danou velikost o daný rozměr na každé straně = velikost se zmenší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="reduce">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Reduce(this SizeF size, SizeF reduce)
        { return new SizeF(size.Width - 2f * reduce.Width, size.Height - 2f * reduce.Height); }

        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoom">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, decimal zoom)
        { return (new SizeF(size.Width * (float)zoom, size.Height * (float)zoom)); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoom">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, float zoom)
        { return new SizeF(size.Width * zoom, size.Height * zoom); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoom">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, double zoom)
        { return new SizeF(size.Width * (float)zoom, size.Height * (float)zoom); }
        /// <summary>
        /// Zmenší danou velikost daným poměrem.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="ratio">Ratio.</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF Divide(this SizeF size, decimal ratio)
        { return new SizeF(size.Width / (float)ratio, size.Height / (float)ratio); }
        /// <summary>
        /// Zmenší danou velikost daným poměrem.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="ratio">Ratio.</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF Divide(this SizeF size, float ratio)
        { return new SizeF(size.Width / ratio, size.Height / ratio); }
        /// <summary>
        /// Zmenší danou velikost daným poměrem.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="ratio">Ratio.</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF Divide(this SizeF size, double ratio)
        { return new SizeF(size.Width / (float)ratio, size.Height / (float)ratio); }

        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoomX">Coefficient.</param>
        /// <param name="zoomY">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, decimal zoomX, decimal zoomY)
        { return (new SizeF(size.Width * (float)zoomX, size.Height * (float)zoomX)); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoomX">Coefficient.</param>
        /// <param name="zoomY">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, float zoomX, float zoomY)
        { return new SizeF(size.Width * zoomX, size.Height * zoomY); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoomX">Coefficient.</param>
        /// <param name="zoomY">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, double zoomX, double zoomY)
        { return new SizeF(size.Width * (float)zoomX, size.Height * (float)zoomY); }

        /// <summary>
        /// Zmenší velikost this tak, aby se vešla do dané velikosti.
        /// Pokud je velikost menší, pak ji nezvětšuje.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="shrinkTo">Cílová velikost</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF ShrinkTo(this SizeF size, SizeF shrinkTo)
        { return new SizeF(size.Width < shrinkTo.Width ? size.Width : shrinkTo.Width, size.Height < shrinkTo.Height ? size.Height : shrinkTo.Height); }
        /// <summary>
        /// Zmenší velikost this tak, aby se vešla do dané velikosti a současně zachovala svůj původní poměr stran.
        /// Pokud je velikost menší, pak ji nezvětšuje.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="shrinkTo">Cílová velikost</param>
        /// <param name="preserveRatio">Zachovat poměr stran</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF ShrinkTo(this SizeF size, SizeF shrinkTo, bool preserveRatio)
        {
            if (size.Width <= shrinkTo.Width && size.Height <= shrinkTo.Height)
                return size;

            if (size.Width == 0 || size.Height == 0)
                preserveRatio = false;

            if (!preserveRatio)
                return new SizeF(size.Width < shrinkTo.Width ? size.Width : shrinkTo.Width, size.Height < shrinkTo.Height ? size.Height : shrinkTo.Height);

            if (shrinkTo.Width <= 0 || shrinkTo.Height <= 0)
                return SizeF.Empty;

            decimal shrinkX = (decimal)shrinkTo.Width / (decimal)size.Width;
            decimal shrinkY = (decimal)shrinkTo.Height / (decimal)size.Height;
            decimal shrink = (shrinkX < shrinkY ? shrinkX : shrinkY);

            return new SizeF((float)((decimal)size.Width * shrink), (float)((decimal)size.Height * shrink));
        }
        /// <summary>
        /// Zmenší velikost this tak, aby se vešla do dané velikosti.
        /// Pokud je velikost menší, pak ji nezvětšuje.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="shrinkTo">Cílová velikost</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static Size ShrinkTo(this Size size, Size shrinkTo)
        { return new Size(size.Width < shrinkTo.Width ? size.Width : shrinkTo.Width, size.Height < shrinkTo.Height ? size.Height : shrinkTo.Height); }
        /// <summary>
        /// Zmenší velikost this tak, aby se vešla do dané velikosti a současně zachovala svůj původní poměr stran.
        /// Pokud je velikost menší, pak ji nezvětšuje.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="shrinkTo">Cílová velikost</param>
        /// <param name="preserveRatio">Zachovat poměr stran</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static Size ShrinkTo(this Size size, Size shrinkTo, bool preserveRatio)
        {
            if (size.Width <= shrinkTo.Width && size.Height <= shrinkTo.Height)
                return size;

            if (size.Width == 0 || size.Height == 0)
                preserveRatio = false;

            if (!preserveRatio)
                return new Size(size.Width < shrinkTo.Width ? size.Width : shrinkTo.Width, size.Height < shrinkTo.Height ? size.Height : shrinkTo.Height);

            if (shrinkTo.Width <= 0 || shrinkTo.Height <= 0)
                return Size.Empty;

            decimal shrinkX = (decimal)shrinkTo.Width / (decimal)size.Width;
            decimal shrinkY = (decimal)shrinkTo.Height / (decimal)size.Height;
            decimal shrink = (shrinkX < shrinkY ? shrinkX : shrinkY);

            return new Size((int)((decimal)size.Width * shrink), (int)((decimal)size.Height * shrink));
        }
        /// <summary>
        /// Vytvoří a vrátí nový Rectangle, jehož velikost je do všech stran zvětšená o daný počet pixelů.
        /// Záporné číslo velikost zmenší.
        /// Například this Rectangle {50, 10, 30, 20} .Enlarge(1) vrátí hodnotu: {49, 9, 32, 22}.
        /// Například this Rectangle {50, 10, 30, 20} .Enlarge(-1) vrátí hodnotu: {51, 11, 28, 18}.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="all">Změna aplikovaná na všechny strany</param>
        public static Rectangle Enlarge(this Rectangle r, int all)
        {
            return r.Enlarge(all, all, all, all);
        }
        /// <summary>
        /// Vytvoří a vrátí nový Rectangle, jehož velikost je do všech stran zvětšená o daný počet pixelů.
        /// Záporné číslo velikost zmenší.
        /// Například this Rectangle {50, 10, 30, 20} .Enlarge(1, 1, 1, 1) vrátí hodnotu: {49, 9, 32, 22}.
        /// Například this Rectangle {50, 10, 30, 20} .Enlarge(0, 0, -1, -1) vrátí hodnotu: {50, 10, 29, 19}.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="left">Zvětšení doleva (zmenší X a zvětší Width)</param>
        /// <param name="top">Zvětšení nahoru (zmenší Y a zvětší Height)</param>
        /// <param name="right">Zvětšení doprava (zvětší Width)</param>
        /// <param name="bottom">Zvětšení dolů (zvětší Height)</param>
        public static Rectangle Enlarge(this Rectangle r, int left, int top, int right, int bottom)
        {
            int x = r.X - left;
            int y = r.Y - top;
            int w = r.Width + left + right;
            int h = r.Height + top + bottom;
            return new Rectangle(x, y, w, h);
        }
        /// <summary>
        /// Vytvoří a vrátí nový Rectangle, jehož velikost je do všech stran zvětšená o daný počet pixelů.
        /// Záporné číslo velikost zmenší.
        /// Například this RectangleF {50, 10, 30, 20} .Enlarge(1) vrátí hodnotu: {49, 9, 32, 22}.
        /// Například this RectangleF {50, 10, 30, 20} .Enlarge(-1) vrátí hodnotu: {51, 11, 28, 18}.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="all">Změna aplikovaná na všechny strany</param>
        public static RectangleF Enlarge(this RectangleF r, int all)
        {
            return r.Enlarge(all, all, all, all);
        }
        /// <summary>
        /// Create a new RectangleF, which is current rectangle enlarged by size specified for each side.
        /// For example: this RectangleF {50, 10, 30, 20} .Enlarge(1, 1, 1, 1) will be after: {49, 9, 32, 22}.
        /// For example: this RectangleF {50, 10, 30, 20} .Enlarge(0, 0, -1, -1) will be after: {50, 10, 29, 19}.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public static RectangleF Enlarge(this RectangleF r, float left, float top, float right, float bottom)
        {
            float x = r.X - left;
            float y = r.Y - top;
            float w = r.Width + left + right;
            float h = r.Height + top + bottom;
            return new RectangleF(x, y, w, h);
        }
        /// <summary>
        /// Vrátí Size, jejíž Width i Height jsou ta menší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static Size Min(this Size one, Size other)
        {
            return new Size((one.Width < other.Width ? one.Width : other.Width),
                             (one.Height < other.Height ? one.Height : other.Height));
        }
        /// <summary>
        /// Vrátí Size, jejíž Width i Height jsou ta menší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="otherWidth"></param>
        /// <param name="otherHeight"></param>
        /// <returns></returns>
        public static Size Min(this Size one, int otherWidth, int otherHeight)
        {
            return new Size((one.Width < otherWidth ? one.Width : otherWidth),
                             (one.Height < otherHeight ? one.Height : otherHeight));
        }
        /// <summary>
        /// Vrátí Size, jejíž Width i Height jsou ta větší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static Size Max(this Size one, Size other)
        {
            return new Size((one.Width > other.Width ? one.Width : other.Width),
                             (one.Height > other.Height ? one.Height : other.Height));
        }
        /// <summary>
        /// Vrátí Size, jejíž Width i Height jsou ta větší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="otherWidth"></param>
        /// <param name="otherHeight"></param>
        /// <returns></returns>
        public static Size Max(this Size one, int otherWidth, int otherHeight)
        {
            return new Size((one.Width > otherWidth ? one.Width : otherWidth),
                             (one.Height > otherHeight ? one.Height : otherHeight));
        }
        /// <summary>
        /// Vrátí Size, jejíž Width i Height jsou zarovnány do mezí Min - Max.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Size MinMax(this Size one, Size min, Size max)
        {
            return new Size((one.Width < min.Width ? min.Width : (one.Width > max.Width ? max.Width : one.Width)),
                             (one.Height < min.Height ? min.Height : (one.Height > max.Height ? max.Height : one.Height)));
        }

        /// <summary>
        /// Vrátí SizeF, jejíž Width i Height jsou ta menší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static SizeF Min(this SizeF one, SizeF other)
        {
            return new SizeF((one.Width < other.Width ? one.Width : other.Width),
                             (one.Height < other.Height ? one.Height : other.Height));
        }
        /// <summary>
        /// Vrátí SizeF, jejíž Width i Height jsou ta větší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static SizeF Max(this SizeF one, SizeF other)
        {
            return new SizeF((one.Width > other.Width ? one.Width : other.Width),
                             (one.Height > other.Height ? one.Height : other.Height));
        }
        /// <summary>
        /// Vrátí SizeF, jejíž Width i Height jsou zarovnány do mezí Min - Max.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static SizeF MinMax(this SizeF one, SizeF min, SizeF max)
        {
            return new SizeF((one.Width < min.Width ? min.Width : (one.Width > max.Width ? max.Width : one.Width)),
                             (one.Height < min.Height ? min.Height : (one.Height > max.Height ? max.Height : one.Height)));
        }
        #endregion
        #region Size: AlignTo
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Rectangle bounds, ContentAlignment alignment, bool cropSize)
        {
            SizeF realSize = sizeF;
            if (cropSize)
                realSize = sizeF.ShrinkTo(bounds.Size, false);
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <param name="preserveRatio"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Rectangle bounds, ContentAlignment alignment, bool cropSize, bool preserveRatio)
        {
            SizeF realSize = sizeF;
            if (cropSize)
                realSize = sizeF.ShrinkTo(bounds.Size, preserveRatio);
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Rectangle bounds, ContentAlignment alignment, bool cropSize)
        {
            Size realSize = size;
            if (cropSize)
                realSize = realSize.ShrinkTo(bounds.Size, false);
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <param name="preserveRatio"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Rectangle bounds, ContentAlignment alignment, bool cropSize, bool preserveRatio)
        {
            Size realSize = size;
            if (cropSize)
                realSize = realSize.ShrinkTo(bounds.Size, preserveRatio);
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Rectangle bounds, ContentAlignment alignment)
        {
            Size size = Size.Ceiling(sizeF);
            return size.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Rectangle bounds, ContentAlignment alignment)
        {
            int x = bounds.X;
            int y = bounds.Y;
            int w = bounds.Width - size.Width;
            int h = bounds.Height - size.Height;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x += w / 2;
                    break;
                case ContentAlignment.TopRight:
                    x += w;
                    break;
                case ContentAlignment.MiddleLeft:
                    y += h / 2;
                    break;
                case ContentAlignment.MiddleCenter:
                    x += w / 2;
                    y += h / 2;
                    break;
                case ContentAlignment.MiddleRight:
                    x += w;
                    y += h / 2;
                    break;
                case ContentAlignment.BottomLeft:
                    y += h;
                    break;
                case ContentAlignment.BottomCenter:
                    x += w / 2;
                    y += h;
                    break;
                case ContentAlignment.BottomRight:
                    x += w;
                    y += h;
                    break;
            }
            return new Rectangle(new Point(x, y), size);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <param name="addSize"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Point pivot, ContentAlignment alignment, Size addSize)
        {
            Rectangle bounds = AlignTo(sizeF, pivot, alignment);
            return new Rectangle(bounds.X, bounds.Y, bounds.Width + addSize.Width, bounds.Height + addSize.Height);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <param name="addSize"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Point pivot, ContentAlignment alignment, Size addSize)
        {
            Rectangle bounds = AlignTo(size, pivot, alignment);
            return new Rectangle(bounds.X, bounds.Y, bounds.Width + addSize.Width, bounds.Height + addSize.Height);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Point pivot, ContentAlignment alignment)
        {
            Size size = Size.Ceiling(sizeF);
            return size.AlignTo(pivot, alignment);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Point pivot, ContentAlignment alignment)
        {
            int x = pivot.X;
            int y = pivot.Y;
            int w = size.Width;
            int h = size.Height;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x -= w / 2;
                    break;
                case ContentAlignment.TopRight:
                    x -= w;
                    break;
                case ContentAlignment.MiddleLeft:
                    y -= h / 2;
                    break;
                case ContentAlignment.MiddleCenter:
                    x -= w / 2;
                    y -= h / 2;
                    break;
                case ContentAlignment.MiddleRight:
                    x -= w;
                    y -= h / 2;
                    break;
                case ContentAlignment.BottomLeft:
                    y -= h;
                    break;
                case ContentAlignment.BottomCenter:
                    x -= w / 2;
                    y -= h;
                    break;
                case ContentAlignment.BottomRight:
                    x -= w;
                    y -= h;
                    break;
            }
            return new Rectangle(new Point(x, y), size);
        }
        /// <summary>
        /// Vrátí new Size, která bude mít shodný poměr jako výchozí, a bude mít danou Width
        /// </summary>
        /// <param name="size"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static Size ZoomToWidth(this Size size, int width)
        {
            if (size.Width <= 0) return size;
            double ratio = (double)width / (double)size.Width;
            return ZoomByRatio(size, ratio);
        }
        /// <summary>
        /// Vrátí new Size, která bude mít shodný poměr jako výchozí, a bude mít danou Height
        /// </summary>
        /// <param name="size"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Size ZoomToHeight(this Size size, int height)
        {
            if (size.Height <= 0) return size;
            double ratio = (double)height / (double)size.Height;
            return ZoomByRatio(size, ratio);
        }
        /// <summary>
        /// Vrátí new Size, která bude mít shodný poměr jako výchozí, a bude mít danou Height
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public static Size ZoomByRatio(this Size size, double ratio)
        {
            int width = (int)(Math.Round(ratio * (double)size.Width, 0));
            int height = (int)(Math.Round(ratio * (double)size.Height, 0));
            return new Size(width, height);
        }
        /// <summary>
        /// Vrátí new Size, která bude mít shodný poměr jako výchozí, a bude mít danou Height
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ratioWidth"></param>
        /// <param name="ratioHeight"></param>
        /// <returns></returns>
        public static Size ZoomByRatio(this Size size, double ratioWidth, double ratioHeight)
        {
            int width = (int)(Math.Round(ratioWidth * (double)size.Width, 0));
            int height = (int)(Math.Round(ratioHeight * (double)size.Height, 0));
            return new Size(width, height);
        }
        #endregion
        #region SizeF: AlignTo
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <returns></returns>
        public static RectangleF AlignTo(this SizeF size, RectangleF bounds, ContentAlignment alignment, bool cropSize)
        {
            SizeF realSize = size;
            if (cropSize)
            {
                if (realSize.Width > bounds.Width)
                    realSize.Width = bounds.Width;
                if (realSize.Height > bounds.Height)
                    realSize.Height = bounds.Height;
            }
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static RectangleF AlignTo(this SizeF size, RectangleF bounds, ContentAlignment alignment)
        {
            float x = bounds.X;
            float y = bounds.Y;
            float w = bounds.Width - size.Width;
            float h = bounds.Height - size.Height;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x += w / 2f;
                    break;
                case ContentAlignment.TopRight:
                    x += w;
                    break;
                case ContentAlignment.MiddleLeft:
                    y += h / 2f;
                    break;
                case ContentAlignment.MiddleCenter:
                    x += w / 2f;
                    y += h / 2f;
                    break;
                case ContentAlignment.MiddleRight:
                    x += w;
                    y += h / 2f;
                    break;
                case ContentAlignment.BottomLeft:
                    y += h;
                    break;
                case ContentAlignment.BottomCenter:
                    x += w / 2f;
                    y += h;
                    break;
                case ContentAlignment.BottomRight:
                    x += w;
                    y += h;
                    break;
            }
            return new RectangleF(new PointF(x, y), size);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <param name="addSize"></param>
        /// <returns></returns>
        public static RectangleF AlignTo(this SizeF size, PointF pivot, ContentAlignment alignment, SizeF addSize)
        {
            RectangleF bounds = AlignTo(size, pivot, alignment);
            return new RectangleF(bounds.X, bounds.Y, bounds.Width + addSize.Width, bounds.Height + addSize.Height);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static RectangleF AlignTo(this SizeF size, PointF pivot, ContentAlignment alignment)
        {
            float x = pivot.X;
            float y = pivot.Y;
            float w = size.Width;
            float h = size.Height;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x -= w / 2f;
                    break;
                case ContentAlignment.TopRight:
                    x -= w;
                    break;
                case ContentAlignment.MiddleLeft:
                    y -= h / 2f;
                    break;
                case ContentAlignment.MiddleCenter:
                    x -= w / 2f;
                    y -= h / 2f;
                    break;
                case ContentAlignment.MiddleRight:
                    x -= w;
                    y -= h / 2f;
                    break;
                case ContentAlignment.BottomLeft:
                    y -= h;
                    break;
                case ContentAlignment.BottomCenter:
                    x -= w / 2f;
                    y -= h;
                    break;
                case ContentAlignment.BottomRight:
                    x -= w;
                    y -= h;
                    break;
            }
            return new RectangleF(new PointF(x, y), size);
        }
        #endregion
        #region Rectangle: FromPoints, FromDim, FromCenter, End, GetVisualRange, GetSide, GetPoint
        /// <summary>
        /// Vrátí Rectangle, který je natažený mezi dvěma body, přičemž vzájemná pozice oněch dvou bodů může být libovolná.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static Rectangle FromPoints(this Point point1, Point point2)
        {
            int l = (point1.X < point2.X ? point1.X : point2.X);
            int t = (point1.Y < point2.Y ? point1.Y : point2.Y);
            int r = (point1.X > point2.X ? point1.X : point2.X);
            int b = (point1.Y > point2.Y ? point1.Y : point2.Y);
            return Rectangle.FromLTRB(l, t, r + 1, b + 1);
        }
        /// <summary>
        /// Vrátí Rectangle, který je nakreslený mezi souřadnicemi x1÷x2 a y1÷y2.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static Rectangle FromDim(int x1, int x2, int y1, int y2)
        {
            int l = (x1 < x2 ? x1 : x2);
            int t = (y1 < y2 ? y1 : y2);
            int r = (x1 > x2 ? x1 : x2);
            int b = (y1 > y2 ? y1 : y2);
            return Rectangle.FromLTRB(l, t, r + 1, b + 1);
        }
        /// <summary>
        /// Vrátí Rectangle, který je nakreslený mezi souřadnicemi x1÷x2 a y1÷y2.
        /// Pokud je vzdálenost mezi x1÷x2 nebo y1÷y2 menší než minDist, pak zachová vzdálenost minDist,
        /// a to tak, že v odpovídajícím směru upraví souřadnici x2 nebo y2. 
        /// Jako x2/y2 by tedy měla být zadána ta "pohyblivější".
        /// </summary>
        /// <returns></returns>
        public static Rectangle FromDim(int x1, int x2, int y1, int y2, int minDist)
        {
            // Úprava souřadnic minDist (kladné číslo) a x2,y2:
            if (minDist < 0) minDist = -minDist;
            if (x2 >= x1 && x2 - x1 < minDist)
                x2 = x1 + minDist;
            else if (x2 < x1 && x1 - x2 < minDist)
                x2 = x1 - minDist;
            if (y2 >= y1 && y2 - y1 < minDist)
                y2 = y1 + minDist;
            else if (y2 < y1 && y1 - y2 < minDist)
                y2 = y1 - minDist;

            int l = (x1 < x2 ? x1 : x2);
            int t = (y1 < y2 ? y1 : y2);
            int r = (x1 > x2 ? x1 : x2);
            int b = (y1 > y2 ? y1 : y2);
            return Rectangle.FromLTRB(l, t, r + 1, b + 1);
        }
        /// <summary>
        /// Vrátí Rectangle postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Rectangle CreateRectangleFromCenter(this Point point, Size size)
        {
            Point location = new Point((point.X - size.Width / 2), (point.Y - size.Height / 2));
            return new Rectangle(location, size);
        }
        /// <summary>
        /// Vrátí Rectangle postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Rectangle CreateRectangleFromCenter(this Point point, int width, int height)
        {
            Point location = new Point((point.X - width / 2), (point.Y - height / 2));
            return new Rectangle(location, new Size(width, height));
        }
        /// <summary>
        /// Vrátí Rectangle postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Rectangle CreateRectangleFromCenter(this Point point, int size)
        {
            Point location = new Point((point.X - size / 2), (point.Y - size / 2));
            return new Rectangle(location, new Size(size, size));
        }
        /// <summary>
        /// Vrátí Rectangle ve velikosti (Size) this, postavený okolo daného středu
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Rectangle CreateRectangleFromCenter(this Size size, Point point)
        {
            Point location = new Point((point.X - size.Width / 2), (point.Y - size.Height / 2));
            return new Rectangle(location, size);
        }
        /// <summary>
        /// Vrátí bod uprostřed this Rectangle
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static Point Center(this Rectangle rectangle)
        {
            return new Point(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2);
        }
        /// <summary>
        /// Vrátí bod na konci this Rectangle (opak Location) : (X + Width - 1, Y + Height - 1)
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static Point End(this Rectangle rectangle)
        {
            return new Point(rectangle.X + rectangle.Width - 1, rectangle.Y + rectangle.Height - 1);
        }
        /// <summary>
        /// Vrátí souřadnici z this rectangle dle požadované strany.
        /// Pokud je zadána hodnota Top, Right, Bottom nebo Left, pak vrací příslušnou souřadnici.
        /// Pokud je zadána hodnota None nebo nějaký součet stran, pak vrací null.
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static Int32? GetSide(this Rectangle rectangle, RectangleSide edge)
        {
            switch (edge)
            {
                case RectangleSide.Top:
                    return rectangle.Top;
                case RectangleSide.Right:
                    return rectangle.Right;
                case RectangleSide.Bottom:
                    return rectangle.Bottom;
                case RectangleSide.Left:
                    return rectangle.Left;
            }
            return null;
        }
        /// <summary>
        /// Metoda vrátí určitý bod v daném Rectangle.
        /// Při nesprávném zadání strany může vrátit null.
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Point? GetPoint(this Rectangle rectangle, ContentAlignment alignment)
        {
            switch (alignment)
            {
                case ContentAlignment.TopLeft: return GetPoint(rectangle, RectangleSide.TopLeft);
                case ContentAlignment.TopCenter: return GetPoint(rectangle, RectangleSide.TopCenter);
                case ContentAlignment.TopRight: return GetPoint(rectangle, RectangleSide.TopRight);

                case ContentAlignment.MiddleLeft: return GetPoint(rectangle, RectangleSide.MiddleLeft);
                case ContentAlignment.MiddleCenter: return GetPoint(rectangle, RectangleSide.MiddleCenter);
                case ContentAlignment.MiddleRight: return GetPoint(rectangle, RectangleSide.MiddleRight);

                case ContentAlignment.BottomLeft: return GetPoint(rectangle, RectangleSide.BottomLeft);
                case ContentAlignment.BottomCenter: return GetPoint(rectangle, RectangleSide.BottomCenter);
                case ContentAlignment.BottomRight: return GetPoint(rectangle, RectangleSide.BottomRight);
            }
            return null;
        }
        /// <summary>
        /// Metoda vrátí určitý bod v daném Rectangle.
        /// Při nesprávném zadání strany může vrátit null.
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Point? GetPoint(this Rectangle rectangle, RectangleSide position)
        {
            int? x = (position.HasFlag(RectangleSide.Left) ? rectangle.X :
                     (position.HasFlag(RectangleSide.Right) ? rectangle.Right :
                     (position.HasFlag(RectangleSide.CenterX) ? (rectangle.X + rectangle.Width / 2) : (int?)null)));
            int? y = (position.HasFlag(RectangleSide.Top) ? rectangle.Y :
                     (position.HasFlag(RectangleSide.Bottom) ? rectangle.Bottom :
                     (position.HasFlag(RectangleSide.CenterY) ? (rectangle.Y + rectangle.Height / 2) : (int?)null)));
            if (!(x.HasValue && y.HasValue)) return null;
            return new Point(x.Value, y.Value);
        }
        ///// <summary>
        ///// Vrátí rozsah { Begin, End } z this rectangle na požadované ose (orientaci).
        ///// Pokud je zadána hodnota axis = <see cref="Orientation.Horizontal"/>, pak je vrácen <see cref="Int32Range"/> s hodnotami X, Width, Right.
        ///// Pokud je zadána hodnota axis = <see cref="Orientation.Vertical"/>, pak je vrácen <see cref="Int32Range"/> s hodnotami Y, Height, Bottom.
        ///// Jinak se vrací null.
        ///// </summary>
        ///// <param name="rectangle"></param>
        ///// <param name="axis"></param>
        ///// <returns></returns>
        //public static Int32Range GetVisualRange(this Rectangle rectangle, Orientation axis)
        //{
        //    switch (axis)
        //    {
        //        case Orientation.Horizontal: return new Int32Range(rectangle.X, rectangle.Right);
        //        case Orientation.Vertical: return new Int32Range(rectangle.Y, rectangle.Bottom);
        //    }
        //    return null;
        //}
        #endregion
        #region RectangleF: FromPoints, FromDim, FromCenter
        /// <summary>
        /// Vrátí RectangleF, který je natažený mezi dvěma body, přičemž vzájemná pozice oněch dvou bodů může být libovolná.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static RectangleF FromPoints(this PointF point1, PointF point2)
        {
            float l = (point1.X < point2.X ? point1.X : point2.X);
            float t = (point1.Y < point2.Y ? point1.Y : point2.Y);
            float r = (point1.X > point2.X ? point1.X : point2.X);
            float b = (point1.Y > point2.Y ? point1.Y : point2.Y);
            return RectangleF.FromLTRB(l, t, r + 1f, b + 1f);
        }
        /// <summary>
        /// Vrátí RectangleF, který je nakreslený mezi souřadnicemi x1÷x2 a y1÷y2.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static RectangleF FromDim(float x1, float x2, float y1, float y2)
        {
            float l = (x1 < x2 ? x1 : x2);
            float t = (y1 < y2 ? y1 : y2);
            float r = (x1 > x2 ? x1 : x2);
            float b = (y1 > y2 ? y1 : y2);
            return RectangleF.FromLTRB(l, t, r + 1f, b + 1f);
        }
        /// <summary>
        /// Vrátí RectangleF, který je nakreslený mezi souřadnicemi x1÷x2 a y1÷y2.
        /// Pokud je vzdálenost mezi x1÷x2 nebo y1÷y2 menší než minDist, pak zachová vzdálenost minDist,
        /// a to tak, že v odpovídajícím směru upraví souřadnici x2 nebo y2. 
        /// Jako x2/y2 by tedy měla být zadána ta "pohyblivější".
        /// </summary>
        /// <returns></returns>
        public static RectangleF FromDim(float x1, float x2, float y1, float y2, float minDist)
        {
            // Úprava souřadnic minDist (kladné číslo) a x2,y2:
            if (minDist < 0f) minDist = -minDist;
            if (x2 >= x1 && x2 - x1 < minDist)
                x2 = x1 + minDist;
            else if (x2 < x1 && x1 - x2 < minDist)
                x2 = x1 - minDist;
            if (y2 >= y1 && y2 - y1 < minDist)
                y2 = y1 + minDist;
            else if (y2 < y1 && y1 - y2 < minDist)
                y2 = y1 - minDist;

            float l = (x1 < x2 ? x1 : x2);
            float t = (y1 < y2 ? y1 : y2);
            float r = (x1 > x2 ? x1 : x2);
            float b = (y1 > y2 ? y1 : y2);
            return RectangleF.FromLTRB(l, t, r + 1f, b + 1f);
        }
        /// <summary>
        /// Vrátí RectangleF postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static RectangleF CreateRectangleFromCenter(this PointF point, SizeF size)
        {
            PointF location = new PointF((point.X - size.Width / 2f), (point.Y - size.Height / 2f));
            return new RectangleF(location, size);
        }
        /// <summary>
        /// Vrátí RectangleF postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static RectangleF CreateRectangleFromCenter(this PointF point, float width, float height)
        {
            PointF location = new PointF((point.X - width / 2f), (point.Y - height / 2f));
            return new RectangleF(location, new SizeF(width, height));
        }
        /// <summary>
        /// Vrátí RectangleF postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static RectangleF CreateRectangleFromCenter(this PointF point, float size)
        {
            PointF location = new PointF((point.X - size / 2f), (point.Y - size / 2f));
            return new RectangleF(location, new SizeF(size, size));
        }
        /// <summary>
        /// Vrátí RectangleF ve velikosti (Size) this, postavený okolo daného středu
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static RectangleF CreateRectangleFromCenter(this SizeF size, PointF point)
        {
            PointF location = new PointF((point.X - size.Width / 2f), (point.Y - size.Height / 2f));
            return new RectangleF(location, size);
        }
        /// <summary>
        /// Vrátí bod uprostřed this RectangleF
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <returns></returns>
        public static PointF Center(this RectangleF rectangleF)
        {
            return new PointF(rectangleF.X + rectangleF.Width / 2f, rectangleF.Y + rectangleF.Height / 2f);
        }
        /// <summary>
        /// Vrátí bod na konci this RectangleF (opak Location)
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <returns></returns>
        public static PointF End(this RectangleF rectangleF)
        {
            return new PointF(rectangleF.X + rectangleF.Width - 1f, rectangleF.Y + rectangleF.Height - 1f);
        }
        #endregion
        #region RectangleF: RelativePoint, AbsolutePoint
        /// <summary>
        /// Vrátí relativní pozici daného absolutního bodu (absolutePoint) vzhledem k this (RectangleF).
        /// Relativní pozice je v rozmezí 0 (na souřadnici Left nebo Top) až 1 (na souřadnici Right nebo Bottom).
        /// Relativní pozice může být menší než 0 (vlevo nebo nad this), nebo větší než 1 (vpravo nebo pod this).
        /// Tedy hodnoty 0 a 1 jsou na hraně this, hodnoty mezi 0 a 1 jsou uvnitř this, a hodnoty mimo jsou mimo this.
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        public static PointF GetPointFRelative(this RectangleF rectangleF, PointF absolutePoint)
        {
            return new PointF(
                (float)_GetRelative(rectangleF.X, rectangleF.Right, absolutePoint.X),
                (float)_GetRelative(rectangleF.Y, rectangleF.Bottom, absolutePoint.Y));
        }
        /// <summary>
        /// Vrátí relativní pozici daného absolutního bodu 
        /// vzhledem k bodům begin (relativní pozice = 0) a end (relativní pozice = 1).
        /// Pokud mezi body begin a end je vzdálenost 0, pak vrací 0.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="absolute"></param>
        /// <returns></returns>
        private static decimal _GetRelative(float begin, float end, float absolute)
        {
            decimal offset = (decimal)(absolute - begin);
            decimal size = (decimal)(end - begin);
            if (size == 0m) return 0m;
            return offset / size;
        }
        /// <summary>
        /// Vrátí souřadnice bodu, který v this rectangle odpovídá dané relativní souřadnici.
        /// Relativní souřadnice vyjadřuje pozici bodu: hodnota 0=na pozici Left nebo Top, hodnota 1=na pozici Right nebo Bottom.
        /// Vrácený bod je vyjádřen v reálných (absolutních) hodnotách odpovídajících rectanglu this.
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="relativePoint"></param>
        /// <returns></returns>
        public static PointF GetPointFAbsolute(this RectangleF rectangleF, PointF relativePoint)
        {
            return new PointF(
                (float)_GetAbsolute(rectangleF.X, rectangleF.Right, (decimal)relativePoint.X),
                (float)_GetAbsolute(rectangleF.Y, rectangleF.Bottom, (decimal)relativePoint.Y));
        }
        /// <summary>
        /// Vrátí absolutní pozici daného relativního bodu 
        /// vzhledem k bodům begin (relativní pozice = 0) a end (relativní pozice = 1).
        /// Pokud mezi body begin a end je vzdálenost 0, pak vrací begin.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="relative"></param>
        /// <returns></returns>
        private static float _GetAbsolute(float begin, float end, decimal relative)
        {
            decimal size = (decimal)(end - begin);
            if (size == 0m) return begin;
            return begin + (float)(relative * size);
        }

        private static float _GetBeginFromRelative(float fix, float size, decimal relative)
        {
            return fix - (float)((decimal)size * relative);
        }
        #endregion
        #region RectangleF: MoveEdge, MovePoint
        /// <summary>
        /// Vrátí RectangleF, který vytvoří z this RectangleF, když jeho hranu (edge) posune na novou souřadnici
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="side"></param>
        /// <param name="dimension"></param>
        /// <returns></returns>
        public static RectangleF MoveEdge(this RectangleF rectangleF, RectangleSide side, float dimension)
        {
            float x1 = rectangleF.X;
            float x2 = rectangleF.Right;
            float y1 = rectangleF.Y;
            float y2 = rectangleF.Bottom;
            switch (side)
            {
                case RectangleSide.Top:
                    return FromDim(x1, x2, dimension, y2);
                case RectangleSide.Right:
                    return FromDim(x1, dimension, y1, y2);
                case RectangleSide.Bottom:
                    return FromDim(x1, x2, y1, dimension);
                case RectangleSide.Left:
                    return FromDim(dimension, x2, y1, y2);
            }
            return rectangleF;
        }
        /// <summary>
        /// Vrátí PointF, který leží na daném rohu this RectangleF
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="corner"></param>
        /// <returns></returns>
        public static PointF GetPoint(this RectangleF rectangleF, RectangleCorner corner)
        {
            float x1 = rectangleF.X;
            float x2 = rectangleF.Right;
            float y1 = rectangleF.Y;
            float y2 = rectangleF.Bottom;
            switch (corner)
            {
                case RectangleCorner.LeftTop:
                    return new PointF(x1, y1);
                case RectangleCorner.TopRight:
                    return new PointF(x2, y1);
                case RectangleCorner.RightBottom:
                    return new PointF(x2, y2);
                case RectangleCorner.BottomLeft:
                    return new PointF(x1, y2);
            }
            return PointF.Empty;
        }
        /// <summary>
        /// Vrátí RectangleF, který vytvoří z this RectangleF, když jeho bod (corner) posune na nové souřadnice
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="corner"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static RectangleF MovePoint(this RectangleF rectangleF, RectangleCorner corner, PointF point)
        {
            switch (corner)
            {
                case RectangleCorner.LeftTop:
                    return FromPoints(rectangleF.GetPoint(RectangleCorner.RightBottom), point);
                case RectangleCorner.TopRight:
                    return FromPoints(rectangleF.GetPoint(RectangleCorner.BottomLeft), point);
                case RectangleCorner.RightBottom:
                    return FromPoints(rectangleF.GetPoint(RectangleCorner.LeftTop), point);
                case RectangleCorner.BottomLeft:
                    return FromPoints(rectangleF.GetPoint(RectangleCorner.TopRight), point);
            }
            return rectangleF;
        }
        #endregion
        #region Rectangle, RectangleF: GetArea(), SummaryRectangle(), ShiftBy()
        /// <summary>
        /// Vrací true, pokud this Rectangle má obě velikosti (Width i Height) kladné, a tedy obsahuje nějaký reálný pixel ke kreslení.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static bool HasPixels(this Rectangle r)
        {
            return (r.Width > 0 && r.Height > 0);
        }
        /// <summary>
        /// Vrací plochu daného Rectangle
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static int GetArea(this Rectangle r)
        {
            return r.Width * r.Height;
        }
        /// <summary>
        /// Vrací plochu daného Rectangle
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static float GetArea(this RectangleF r)
        {
            return r.Width * r.Height;
        }
        /// <summary>
        /// Vrátí orientaci tohoto prostoru podle poměru šířky a výšky. Pokud šířka == výšce, pak vrací Horizontal.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Orientation GetOrientation(this Rectangle r)
        {
            return (r.Width >= r.Height ? Orientation.Horizontal : Orientation.Vertical);
        }
        /// <summary>
        /// Returns a Orientation of this Rectangle. When Width is equal or greater than Height, then returns Horizontal. Otherwise returns Vertica orientation.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Orientation GetOrientation(this RectangleF r)
        {
            return (r.Width >= r.Height ? Orientation.Horizontal : Orientation.Vertical);
        }
        /// <summary>
        /// Metoda vrátí vzdálenost daného bodu od nejbližšího bodu daného rectangle.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static int GetOuterDistance(this Rectangle bounds, Point point)
        {
            int x = point.X;
            int y = point.Y;
            int l = bounds.X;
            int t = bounds.Y;
            int r = bounds.Right;
            int b = bounds.Bottom;

            string q = ((x < l) ? "0" : ((x < r) ? "1" : "2")) + ((y < t) ? "0" : ((y < b) ? "1" : "2"));        // Kvadrant "00" = vlevo nad, "11" = uvnitř, "02" = vlevo pod, atd...
            int dx = 0;
            int dy = 0;
            switch (q)
            {
                case "00":        // Vlevo, Nad
                    dx = l - x;
                    dy = t - y;
                    break;
                case "01":        // Vlevo, Uvnitř
                    dx = l - x;
                    break;
                case "02":        // Vlevo, Pod
                    dx = l - x;
                    dy = y - b;
                    break;
                case "10":        // Uvnitř, Nad
                    dy = t - y;
                    break;
                case "11":        // Uvnitř, Uvnitř
                    break;
                case "12":        // Uvnitř, Pod
                    dy = y - b;
                    break;
                case "20":        // Vpravo, Nad
                    dx = x - r;
                    dy = t - y;
                    break;
                case "21":        // Vpravo, Uvnitř
                    dx = x - r;
                    break;
                case "22":        // Vpravo, Pod
                    dx = x - r;
                    dy = y - b;
                    break;
            }
            if (dy == 0) return dx;
            if (dx == 0) return dy;
            int d = (int)Math.Ceiling(Math.Sqrt((double)(dx * dx + dy * dy)));
            return d;
        }
        /// <summary>
        /// Vrací Rectangle, který je souhrnem všech zadaných Rectangle.
        /// Akceptuje i neviditelné Rectangle (který má Width nebo Height nula nebo záporné), i z nich střádá jejich souřadnice.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle SummaryRectangle(params Rectangle[] items)
        {
            return _SummaryRectangle(items as IEnumerable<Rectangle>, false);
        }
        /// <summary>
        /// Vrací Rectangle, který je souhrnem viditelných Rectangle.
        /// Viditelný = ten který má Width a Height kladné.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle SummaryVisibleRectangle(params Rectangle[] items)
        {
            return _SummaryRectangle(items as IEnumerable<Rectangle>, true);
        }
        /// <summary>
        /// Vrací RectangleF, který je souhrnem všech zadaných Rectangle.
        /// Akceptuje i neviditelné Rectangle (který má Width nebo Height nula nebo záporné), i z nich střádá jejich souřadnice.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle SummaryRectangle(IEnumerable<Rectangle> items)
        {
            return _SummaryRectangle(items as IEnumerable<Rectangle>, false);
        }
        /// <summary>
        /// Vrací RectangleF, který je souhrnem viditelných Rectangle.
        /// Viditelný = ten který má Width a Height kladné.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle SummaryVisibleRectangle(IEnumerable<Rectangle> items)
        {
            return _SummaryRectangle(items as IEnumerable<Rectangle>, true);
        }
        /// <summary>
        /// Vrací RectangleF, který je souhrnem zadaných Rectangle.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="onlyVisible"></param>
        /// <returns></returns>
        private static Rectangle _SummaryRectangle(IEnumerable<Rectangle> items, bool onlyVisible)
        {
            int l = 0;
            int t = 0;
            int r = 0;
            int b = 0;
            bool empty = true;
            foreach (Rectangle item in items)
            {
                if (onlyVisible && (item.Width <= 0 || item.Height <= 0)) continue;

                if (empty)
                {
                    l = item.Left;
                    t = item.Top;
                    r = item.Right;
                    b = item.Bottom;
                    empty = false;
                }
                else
                {
                    if (l > item.Left) l = item.Left;
                    if (t > item.Top) t = item.Top;
                    if (r < item.Right) r = item.Right;
                    if (b < item.Bottom) b = item.Bottom;
                }
            }
            return Rectangle.FromLTRB(l, t, r, b);
        }
        /// <summary>
        /// Vrací Rectangle, který je souhrnem všech zadaných Rectangle.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle? SummaryRectangle(params Rectangle?[] items)
        {
            int l = 0;
            int t = 0;
            int r = 0;
            int b = 0;
            bool empty = true;
            foreach (Rectangle? itemN in items)
            {
                if (itemN.HasValue)
                {
                    Rectangle item = itemN.Value;
                    if (empty)
                    {
                        l = item.Left;
                        t = item.Top;
                        r = item.Right;
                        b = item.Bottom;
                        empty = false;
                    }
                    else
                    {
                        if (l > item.Left) l = item.Left;
                        if (t > item.Top) t = item.Top;
                        if (r < item.Right) r = item.Right;
                        if (b < item.Bottom) b = item.Bottom;
                    }
                }
            }
            return (!empty ? (Rectangle?)Rectangle.FromLTRB(l, t, r, b) : (Rectangle?)null);
        }
        /// <summary>
        /// Vrací RectangleF, který je souhrnem všech zadaných Rectangle.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static RectangleF SummaryRectangle(IEnumerable<RectangleF> items)
        {
            float l = 0f;
            float t = 0f;
            float r = 0f;
            float b = 0f;
            bool empty = true;
            foreach (RectangleF item in items)
            {
                if (empty)
                {
                    l = item.Left;
                    t = item.Top;
                    r = item.Right;
                    b = item.Bottom;
                    empty = false;
                }
                else
                {
                    if (l > item.Left) l = item.Left;
                    if (t > item.Top) t = item.Top;
                    if (r < item.Right) r = item.Right;
                    if (b < item.Bottom) b = item.Bottom;
                }
            }
            return RectangleF.FromLTRB(l, t, r, b);
        }
        /// <summary>
        /// Vrací RectangleF, který je souhrnem všech zadaných Rectangle.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static RectangleF? SummaryRectangle(params RectangleF?[] items)
        {
            float l = 0f;
            float t = 0f;
            float r = 0f;
            float b = 0f;
            bool empty = true;
            foreach (RectangleF? itemN in items)
            {
                if (itemN.HasValue)
                {
                    RectangleF item = itemN.Value;
                    if (empty)
                    {
                        l = item.Left;
                        t = item.Top;
                        r = item.Right;
                        b = item.Bottom;
                        empty = false;
                    }
                    else
                    {
                        if (l > item.Left) l = item.Left;
                        if (t > item.Top) t = item.Top;
                        if (r < item.Right) r = item.Right;
                        if (b < item.Bottom) b = item.Bottom;
                    }
                }
            }
            return (!empty ? (RectangleF?)RectangleF.FromLTRB(l, t, r, b) : (RectangleF?)null);
        }
        /// <summary>
        /// Vrátí Rectangle, který vznikne posunutím this o souřadnice (X,Y) daného bodu
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rectangle ShiftBy(this Rectangle r, Point point)
        {
            return new Rectangle(r.Location.Add(point), r.Size);
        }
        /// <summary>
        /// Vrátí Rectangle, který vznikne posunutím this o souřadnice (X,Y) daného bodu
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle ShiftBy(this Rectangle r, int x, int y)
        {
            return new Rectangle(r.X + x, r.Y + y, r.Width, r.Height);
        }
        /// <summary>
        /// Vrátí Rectangle, který vznikne posunutím this o souřadnice (X,Y) daného bodu
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static RectangleF ShiftBy(this RectangleF r, PointF point)
        {
            return new RectangleF(r.Location.Add(point), r.Size);
        }
        /// <summary>
        /// Vrátí RectangleF, který vznikne posunutím this o souřadnice (X,Y) daného bodu
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static RectangleF ShiftBy(this RectangleF r, float x, float y)
        {
            return new RectangleF(r.X + x, r.Y + y, r.Width, r.Height);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle plus point (=new Rectangle(this.X + point.X, this.Y + point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rectangle Add(this Rectangle r, Point point)
        {
            return new Rectangle(r.Location.Add(point), r.Size);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle plus point (=new Rectangle(this.X + point.X, this.Y + point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle Add(this Rectangle r, int x, int y)
        {
            return new Rectangle(r.X + x, r.Y + y, r.Width, r.Height);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle minus point (=new Rectangle(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rectangle Sub(this Rectangle r, Point point)
        {
            return new Rectangle(r.Location.Sub(point), r.Size);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle minus point (=new Rectangle(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle Sub(this Rectangle r, int x, int y)
        {
            return new Rectangle(r.X - x, r.Y - y, r.Width, r.Height);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle plus point (=new Rectangle(this.X + point.X, this.Y + point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rectangle? Add(this Rectangle? r, Point? point)
        {
            return (r.HasValue && point.HasValue ? (Rectangle?)(new Rectangle(r.Value.Location.Add(point.Value), r.Value.Size)) : (Rectangle?)null);
        }
        /// <summary>
        /// Returns a Rectangle?, which is this rectangle plus point (=new Rectangle?(this.X + point.X, this.Y + point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle? Add(this Rectangle? r, int x, int y)
        {
            return (r.HasValue ? (Rectangle?)(new Rectangle(r.Value.X + x, r.Value.Y + y, r.Value.Width, r.Value.Height)) : (Rectangle?)null);
        }
        /// <summary>
        /// Returns a Rectangle?, which is this rectangle minus point (=new Rectangle?(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rectangle? Sub(this Rectangle? r, Point? point)
        {
            return (r.HasValue && point.HasValue ? (Rectangle?)(new Rectangle(r.Value.Location.Sub(point.Value), r.Value.Size)) : (Rectangle?)null);
        }
        /// <summary>
        /// Returns a Rectangle?, which is this rectangle minus point (=new Rectangle?(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle? Sub(this Rectangle? r, int x, int y)
        {
            return (r.HasValue ? (Rectangle?)(new Rectangle(r.Value.X - x, r.Value.Y - y, r.Value.Width, r.Value.Height)) : (Rectangle?)null);
        }
        /// <summary>
        /// Returns a RectangleF, which is this rectangle plus point (=new RectangleF(this.X + point.X, this.Y + point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static RectangleF Add(this RectangleF r, PointF point)
        {
            return new RectangleF(r.Location.Add(point), r.Size);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle plus point (=new Rectangle(this.X + point.X, this.Y + point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static RectangleF Add(this RectangleF r, float x, float y)
        {
            return new RectangleF(r.X + x, r.Y + y, r.Width, r.Height);
        }
        /// <summary>
        /// Returns a RectangleF, which is this rectangle minus point (=new RectangleF(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static RectangleF Sub(this RectangleF r, PointF point)
        {
            return new RectangleF(r.Location.Sub(point), r.Size);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle minus point (=new Rectangle(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static RectangleF Sub(this RectangleF r, float x, float y)
        {
            return new RectangleF(r.X - x, r.Y - y, r.Width, r.Height);
        }
        /// <summary>
        /// Vrátí nový Rectangle, který má stejnou pozici středu (Center), ale je otočený o 90°: z výšky na šířku.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Rectangle Swap(this Rectangle r)
        {
            Point center = Center(r);
            Size size = Swap(r.Size);
            return center.CreateRectangleFromCenter(size);
        }
        /// <summary>
        /// Vrátí danou velikost otočenou o 90°: z výšky na šířku.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Size Swap(this Size size)
        {
            return new Size(size.Height, size.Width);
        }

        /// <summary>
        /// Vrátí nový Rectangle, který má stejnou pozici středu (Center), ale je otočený o 90°: z výšky na šířku.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static RectangleF Swap(this RectangleF r)
        {
            PointF center = Center(r);
            SizeF size = Swap(r.Size);
            return center.CreateRectangleFromCenter(size);
        }
        /// <summary>
        /// Vrátí danou velikost otočenou o 90°: z výšky na šířku.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static SizeF Swap(this SizeF size)
        {
            return new SizeF(size.Height, size.Width);
        }
        #endregion
        #region Rectangle a Point: FitInto()
        /// <summary>
        /// Zajistí, že this souřadnice budou umístěny do daného prostoru (disponibleBounds).
        /// Pokud daný prostor je menší, než velikost this, pak velikost this může být zmenšena, anebo this může přesahovat doprava/dolů, podle parametru shrinkToFit
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="disponibleBounds">Souřadnice prostoru, do něhož má být this souřadnice posunuta</param>
        /// <param name="shrinkToFit">true = pokud this má větší velikost než disponibleBounds, pak this bude zmenšeno / false = pak this bude přečnívat doprava / dolů.</param>
        /// <returns></returns>
        public static Rectangle FitInto(this Rectangle bounds, Rectangle disponibleBounds, bool shrinkToFit)
        {
            int dx = disponibleBounds.X;
            int dy = disponibleBounds.Y;
            int dw = disponibleBounds.Width;
            int dh = disponibleBounds.Height;
            int dr = dx + dw;
            int db = dy + dh;

            int x = bounds.X;
            int y = bounds.Y;
            int w = bounds.Width;
            int h = bounds.Height;

            if (x < dx) x = dx;
            if ((x + w) > dr)
            {
                x = dr - w;
                if (x < dx)
                {
                    x = dx;
                    if (shrinkToFit)
                        w = dw;
                }
            }

            if (y < dy) y = dy;
            if ((y + h) > db)
            {
                y = db - h;
                if (y < dy)
                {
                    y = dy;
                    if (shrinkToFit)
                        h = dh;
                }
            }

            return new Rectangle(x, y, w, h);
        }
        /// <summary>
        /// Zajistí, že this souřadnice budou umístěny do daného prostoru (disponibleBounds).
        /// Pokud daný prostor je menší, než velikost this, pak velikost this může být zmenšena, anebo this může přesahovat doprava/dolů, podle parametru shrinkToFit
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="disponibleBounds">Souřadnice prostoru, do něhož má být this souřadnice posunuta</param>
        /// <param name="shrinkToFit">true = pokud this má větší velikost než disponibleBounds, pak this bude zmenšeno / false = pak this bude přečnívat doprava / dolů.</param>
        /// <returns></returns>
        public static RectangleF FitInto(this RectangleF bounds, RectangleF disponibleBounds, bool shrinkToFit)
        {
            float dx = disponibleBounds.X;
            float dy = disponibleBounds.Y;
            float dw = disponibleBounds.Width;
            float dh = disponibleBounds.Height;
            float dr = dx + dw;
            float db = dy + dh;

            float x = bounds.X;
            float y = bounds.Y;
            float w = bounds.Width;
            float h = bounds.Height;

            if (x < dx) x = dx;
            if ((x + w) > dr)
            {
                x = dr - w;
                if (x < dx)
                {
                    x = dx;
                    if (shrinkToFit)
                        w = dw;
                }
            }

            if (y < dy) y = dy;
            if ((y + h) > db)
            {
                y = db - h;
                if (y < dy)
                {
                    y = dy;
                    if (shrinkToFit)
                        h = dh;
                }
            }

            return new RectangleF(x, y, w, h);
        }
        /// <summary>
        /// Zajistí, že this souřadnice bodu bude umístěny do daného prostoru (disponibleBounds).
        /// Vrácený bod tedy bude nejblíže výchozímu bodu, v daném prostoru.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="disponibleBounds">Souřadnice prostoru, do něhož má být this souřadnice posunuta</param>
        /// <returns></returns>
        public static Point FitInto(this Point point, Rectangle disponibleBounds)
        {
            int dx = disponibleBounds.X;
            int dy = disponibleBounds.Y;
            int dw = disponibleBounds.Width;
            int dh = disponibleBounds.Height;
            int dr = dx + dw;
            int db = dy + dh;

            int x = point.X;
            int y = point.Y;

            if (x > dr) x = dr;
            if (x < dx) x = dx;
            if (y > db) y = db;
            if (y < dy) y = dy;

            return new Point(x, y);
        }
        /// <summary>
        /// Zajistí, že this souřadnice bodu bude umístěny do daného prostoru (disponibleBounds).
        /// Vrácený bod tedy bude nejblíže výchozímu bodu, v daném prostoru.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="disponibleBounds">Souřadnice prostoru, do něhož má být this souřadnice posunuta</param>
        /// <returns></returns>
        public static PointF FitInto(this PointF point, RectangleF disponibleBounds)
        {
            float dx = disponibleBounds.X;
            float dy = disponibleBounds.Y;
            float dw = disponibleBounds.Width;
            float dh = disponibleBounds.Height;
            float dr = dx + dw;
            float db = dy + dh;

            float x = point.X;
            float y = point.Y;

            if (x > dr) x = dr;
            if (x < dx) x = dx;
            if (y > db) y = db;
            if (y < dy) y = dy;

            return new PointF(x, y);
        }
        #endregion
        #region Rectangle: GetBorders
        /// <summary>
        /// Metoda vrací souřadnice okrajů daného Rectangle.
        /// Tyto souřadnice lze poté vyplnit (Fill), a budou uvnitř daného Rectangle.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="thick"></param>
        /// <param name="sides"></param>
        /// <returns></returns>
        public static Rectangle[] GetBorders(this Rectangle r, int thick, params RectangleSide[] sides)
        {
            int count = sides.Length;
            Rectangle[] borders = new Rectangle[count];

            int x0 = r.X;
            int x1 = r.Right;
            int w = r.Width;
            int y0 = r.Y;
            int y1 = r.Bottom;
            int h = r.Height;
            int t = (thick >= 0 ? thick : 0);
            int tx = (t < w ? t : w);
            int ty = (t < h ? t : h);

            for (int i = 0; i < count; i++)
            {
                switch (sides[i])
                {
                    case RectangleSide.Left:
                        borders[i] = new Rectangle(x0, y0, tx, h);
                        break;
                    case RectangleSide.Top:
                        borders[i] = new Rectangle(x0, y0, w, ty);
                        break;
                    case RectangleSide.Right:
                        borders[i] = new Rectangle(x1 - tx, y0, tx, h);
                        break;
                    case RectangleSide.Bottom:
                        borders[i] = new Rectangle(x0, y1 - ty, w, ty);
                        break;

                    case RectangleSide.TopLeft:
                        borders[i] = new Rectangle(x0, y0, tx, ty);
                        break;
                    case RectangleSide.TopRight:
                        borders[i] = new Rectangle(x1 - tx, y0, tx, ty);
                        break;
                    case RectangleSide.BottomRight:
                        borders[i] = new Rectangle(x1 - tx, y1 - ty, tx, ty);
                        break;
                    case RectangleSide.BottomLeft:
                        borders[i] = new Rectangle(x0, y1 - ty, tx, ty);
                        break;
                }
            }

            return borders;
        }
        #endregion


    }
    #endregion
    #region class EnumerableExtensions
    /// <summary>
    /// Extension metody pro IEnumerable
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Pro každý prvek this kolekce proede danou akci
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="action"></param>
        public static void ForEachExec<T>(this IEnumerable<T> items, Action<T> action)
        {
            if (items == null || action == null) return;
            foreach (T item in items)
                action(item);
        }
    }
    #endregion
    #region class UsingScope : Jednoduchý scope, který provede při vytvoření akci OnBegin, a při Dispose akci OnEnd
    /// <summary>
    /// Jednoduchý scope, který provede při vytvoření akci OnBegin, a při Dispose akci OnEnd.
    /// </summary>
    internal class UsingScope : IDisposable
    {
        /// <summary>
        /// Jednoduchý scope, který provede při vytvoření akci OnBegin, a při Dispose akci OnEnd.
        /// </summary>
        /// <param name="onBegin">Jako parametr je předán this scope, lze v něm použít property <see cref="UserData"/> pro uložení dat, budou k dispozici v akci <paramref name="onEnd"/></param>
        /// <param name="onEnd">Jako parametr je předán this scope, lze v něm použít property <see cref="UserData"/> pro čtení dat uložených v akci <paramref name="onBegin"/></param>
        /// <param name="userData">Volitelně UserData</param>
        public UsingScope(Action<UsingScope> onBegin, Action<UsingScope> onEnd, object userData = null)
        {
            this.UserData = userData;
            _OnEnd = onEnd;
            onBegin?.Invoke(this);
        }
        private Action<UsingScope> _OnEnd;
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
    #region class RegexSupport
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
    internal class WeakTarget<T> where T : class
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
    #endregion
    #region Enumy
    /// <summary>
    /// Styl použitý pro Label
    /// </summary>
    public enum LabelStyleType
    {
        /// <summary>
        /// Běžný label u jednotlivých input prvků
        /// </summary>
        Default,
        /// <summary>
        /// Titulkový label menší, typicky grupa
        /// </summary>
        SubTitle,
        /// <summary>
        /// Titulkový label větší, jeden na formuláři
        /// </summary>
        MainTitle,
        /// <summary>
        /// Dodatková informace
        /// </summary>
        Info
    }
    /// <summary>
    /// Vyjádření názvu hrany na objektu Rectangle (Horní, Vpravo, Dolní, Vlevo).
    /// Enum povoluje sčítání hodnot, ale různé funkce nemusejí sečtené hodnoty akceptovat (z důvodu jejich logiky).
    /// </summary>
    [Flags]
    public enum RectangleSide
    {
        /// <summary>Neurčeno</summary>
        None = 0,
        /// <summary>Vlevo svislá na ose X</summary>
        Left = 0x01,
        /// <summary>Střed na ose X</summary>
        CenterX = 0x02,
        /// <summary>Vpravo svislá na ose X</summary>
        Right = 0x04,
        /// <summary>Horní vodorovná na ose Y</summary>
        Top = 0x10,
        /// <summary>Střed na ose Y</summary>
        CenterY = 0x20,
        /// <summary>Dolní vodorovná na ose Y</summary>
        Bottom = 0x40,
        /// <summary>Vodorovné = Top + Bottom</summary>
        Horizontal = Top | Bottom,
        /// <summary>Svislé = Left + Right</summary>
        Vertical = Left | Right,
        /// <summary>
        /// Prostřední bod
        /// </summary>
        Center = CenterX | CenterY,
        /// <summary>
        /// Horní levý bod
        /// </summary>
        TopLeft = Top | Left,
        /// <summary>
        /// Horní prostřední bod
        /// </summary>
        TopCenter = Top | CenterX,
        /// <summary>
        /// Horní pravý bod
        /// </summary>
        TopRight = Top | Right,
        /// <summary>
        /// Střední levý bod
        /// </summary>
        MiddleLeft = CenterY | Left,
        /// <summary>
        /// Úplně střední bod (X i Y)
        /// </summary>
        MiddleCenter = CenterY | CenterX,
        /// <summary>
        /// Střední pravý bod
        /// </summary>
        MiddleRight = CenterY | Right,
        /// <summary>
        /// Dolní levý bod
        /// </summary>
        BottomLeft = Bottom | Left,
        /// <summary>
        /// Dolní prostřední bod
        /// </summary>
        BottomCenter = Bottom | CenterX,
        /// <summary>
        /// Dolní pravý roh
        /// </summary>
        BottomRight = Bottom | Right,
        /// <summary>Všechny</summary>
        All = Left | Top | Right | Bottom
    }
    /// <summary>
    /// Vyjádření názvu rohu na objektu Rectangle (Vlevo nahoře, Vpravo nahoře, ...)
    /// </summary>
    public enum RectangleCorner
    {
        /// <summary>Neurčeno</summary>
        None = 0,
        /// <summary>Vlevo nahoře</summary>
        LeftTop,
        /// <summary>Vpravo nahoře</summary>
        TopRight,
        /// <summary>Vpravo dole</summary>
        RightBottom,
        /// <summary>Vlevo dole</summary>
        BottomLeft
    }

    #endregion
    #region DxStdForm
    public class DxStdForm : DevExpress.XtraEditors.XtraForm
    { }
    #endregion
    #region DxRibbonForm
    /// <summary>
    /// Formulář s ribbonem
    /// </summary>
    public class DxRibbonForm : DevExpress.XtraBars.Ribbon.RibbonForm
    { }
    #endregion
    #region DxAutoScrollPanelControl
    /// <summary>
    /// DxAutoScrollPanelControl : Panel s podporou AutoScroll a s podporou události při změně VisibleBounds
    /// </summary>
    public class DxAutoScrollPanelControl : DxPanelControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxAutoScrollPanelControl()
        {
            this.AutoScroll = true;
            this.SetAutoScrollMargin(40, 6);
            this.Padding = new Padding(10);
            this.SetStyle(ControlStyles.UserPaint, true);
        }
        #region VisibleBounds
        /// <summary>
        /// Souřadnice Child prvků, které jsou nyní vidět (=logické koordináty).
        /// Pokud tedy mám Child prvek s Bounds = { 0, 0, 600, 2000 } 
        /// a this container má velikost { 500, 300 } a je odscrollovaný trochu dolů (o 100 pixelů),
        /// pak VisibleBounds obsahuje právě to "viditelné okno" v Child controlu = { 100, 0, 500, 300 }
        /// </summary>
        public Rectangle VisibleBounds { get { return __CurrentVisibleBounds; } }
        /// <summary>
        /// Je provedeno po změně <see cref="DxAutoScrollPanelControl.VisibleBounds"/>
        /// </summary>
        protected virtual void OnVisibleBoundsChanged() { }
        /// <summary>
        /// Událost je vyvolaná po každé změně <see cref="VisibleBounds"/>
        /// </summary>
        public event EventHandler VisibleBoundsChanged;
        /// <summary>
        /// Zkontroluje, zda aktuální viditelná oblast je shodná/jiná než dosavadní, a pokud je jiná pak ji upraví a vyvolá události.
        /// </summary>
        private void _CheckVisibleBoundsChange()
        {
            Rectangle last = __CurrentVisibleBounds;
            Rectangle current = _GetVisibleBounds();
            if (current == last) return;
            __CurrentVisibleBounds = current;
            OnVisibleBoundsChanged();
            VisibleBoundsChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Vrátí aktuálně viditelnou oblast vypočtenou pro AutoScrollPosition a ClientSize
        /// </summary>
        /// <returns></returns>
        private Rectangle _GetVisibleBounds()
        {
            Point autoScrollPoint = this.AutoScrollPosition;
            Point origin = new Point(-autoScrollPoint.X, -autoScrollPoint.Y);
            Size size = this.ClientSize;
            return new Rectangle(origin, size);
        }
        private Rectangle __CurrentVisibleBounds;
        /// <summary>
        /// Volá se při kreslení pozadí.
        /// Potomci zde mohou detekovat nové <see cref="VisibleBounds"/> a podle nich zobrazit potřebné controly.
        /// V této metodě budou controly zobrazeny bez blikání = ještě dříve, než se Panel naroluje na novou souřadnici
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            this._CheckVisibleBoundsChange();
            base.OnPaintBackground(e);
        }
        //   TATO METODA SE VOLÁ AŽ PO OnPaintBackground() A NENÍ TEDY NUTNO JI ŘEŠIT:
        //protected override void OnPaint(PaintEventArgs e)
        //{
        //    this._CheckVisibleBoundsChange();
        //    base.OnPaint(e);
        //}
        /// <summary>
        /// Tato metoda je jako jediná vyvolaná při posunu obsahu pomocí kolečka myší a některých dalších akcích (pohyb po controlech, resize), 
        /// ale není volaná při manipulaci se Scrollbary.
        /// </summary>
        protected override void SyncScrollbars()
        {
            base.SyncScrollbars();
            this._CheckVisibleBoundsChange();
        }
        /// <summary>
        /// Tato metoda je vyvolaná při manipulaci se Scrollbary.
        /// Při té se ale nevolá SyncScrollbars().
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnScroll(object sender, XtraScrollEventArgs e)
        {
            base.OnScroll(sender, e);
            this._CheckVisibleBoundsChange();
        }
        /// <summary>
        /// Volá se po změně velikosti tohoto controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this._CheckVisibleBoundsChange();
        }
        #endregion
    }
    #endregion
    #region DxPanelControl
    /// <summary>
    /// PanelControl
    /// </summary>
    public class DxPanelControl : DevExpress.XtraEditors.PanelControl, IListenerZoomChange, IListenerStyleChanged
    {
        #region Konstruktor a základní vlastnosti
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxPanelControl()
        {
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.Margin = new Padding(0);
            this.Padding = new Padding(0);
            DxComponent.RegisterListener(this);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DxComponent.UnregisterListener(this);
        }
        /// <summary>
        /// Barva pozadí uživatelská, má přednost před skinem, aplikuje se na hotový skin, může obsahovat Alpha kanál = pak skrz tuto barvu prosvítá podkladový skin
        /// </summary>
        public Color? BackColorUser { get { return _BackColorUser; } set { _BackColorUser = value; Invalidate(); } }
        private Color? _BackColorUser;
        #endregion
        #region Paint
        /// <summary>
        /// Základní kreslení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this.PaintBackColorUser(e);
        }
        /// <summary>
        /// Overlay kreslení BackColorUser
        /// </summary>
        /// <param name="e"></param>
        protected void PaintBackColorUser(PaintEventArgs e)
        {
            var backColorUser = BackColorUser;
            if (!backColorUser.HasValue) return;
            e.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(backColorUser.Value), this.ClientRectangle);
        }
        #endregion
        #region Style & Zoom Changed
        void IListenerZoomChange.ZoomChanged()
        { }
        void IListenerStyleChanged.StyleChanged()
        { }
        #endregion
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
    }
    #endregion
    #region DxSplitContainerControl
    /// <summary>
    /// SplitContainerControl
    /// </summary>
    public class DxSplitContainerControl : DevExpress.XtraEditors.SplitContainerControl
    { }
    #endregion
    #region DxTabPane : Control se záložkami = DevExpress.XtraBars.Navigation.TabPane
    /// <summary>
    /// Control se záložkami = <see cref="DevExpress.XtraBars.Navigation.TabPane"/>
    /// </summary>
    public class DxTabPane : DevExpress.XtraBars.Navigation.TabPane
    {
        #region Konstruktor a zjednodušené přidání záložky
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTabPane()
        {
            InitProperties();
            InitEvents();
            
        }
        /// <summary>
        /// Přidá novou stránku (záložku) do this containeru
        /// </summary>
        /// <param name="pageName"></param>
        /// <param name="pageText"></param>
        /// <param name="pageToolTip"></param>
        /// <param name="pageImageName"></param>
        /// <returns></returns>
        public DevExpress.XtraBars.Navigation.TabNavigationPage AddNewPage(string pageName, string pageText, string pageToolTip = null, string pageImageName = null)
        {
            string text = pageText;
            var page = this.CreateNewPage() as DevExpress.XtraBars.Navigation.TabNavigationPage;
            page.Name = pageName;
            page.Caption = text;
            page.PageText = text;
            page.ToolTip = pageToolTip;
            page.ImageOptions.Image = null; // pageImageName tabHeaderItem.Image;
            page.Properties.ShowMode = DevExpress.XtraBars.Navigation.ItemShowMode.ImageAndText;

            this.Pages.Add(page);

            return page;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.RemoveEvents();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Nastaví defaultní vlastnosti
        /// </summary>
        private void InitProperties()
        {
            this.TabAlignment = DevExpress.XtraEditors.Alignment.Near;           // Near = doleva, Far = doprava, Center = uprostřed
            this.PageProperties.AllowBorderColorBlending = true;
            this.PageProperties.ShowMode = DevExpress.XtraBars.Navigation.ItemShowMode.ImageAndText;
            this.AllowCollapse = DevExpress.Utils.DefaultBoolean.False;          // Nedovolí uživateli skrýt headery

            this.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Style3D;
            this.LookAndFeel.UseWindowsXPTheme = true;
            this.OverlayResizeZoneThickness = 20;
            this.ItemOrientation = Orientation.Horizontal;                       // Vertical = kreslí řadu záhlaví vodorovně, ale obsah jednotlivého buttonu svisle :-(

            this.TransitionType = DxTabPaneTransitionType.FadeFast;

            // Požadavky designu na vzhled buttonů:
            this.AppearanceButton.Normal.FontSizeDelta = 2;
            this.AppearanceButton.Normal.FontStyleDelta = FontStyle.Regular;
            this.AppearanceButton.Normal.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.AppearanceButton.Hovered.FontSizeDelta = 2;
            this.AppearanceButton.Hovered.FontStyleDelta = FontStyle.Underline;
            this.AppearanceButton.Hovered.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.AppearanceButton.Pressed.FontSizeDelta = 2;
            this.AppearanceButton.Pressed.FontStyleDelta = FontStyle.Bold;
            this.AppearanceButton.Pressed.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        }
        /// <summary>
        /// Typ přechodového efektu
        /// </summary>
        public new DxTabPaneTransitionType TransitionType
        {
            get { return _TransitionType; }
            set
            {
                _TransitionType = value;
                DxTabPaneTransitionType type = value & DxTabPaneTransitionType.AllTypes;
                if (type == DxTabPaneTransitionType.None)
                {
                    this.AllowTransitionAnimation = DevExpress.Utils.DefaultBoolean.False;
                }
                else
                {
                    this.AllowTransitionAnimation = DevExpress.Utils.DefaultBoolean.True;
                    this.TransitionManager.UseDirectXPaint = DefaultBoolean.True;
                    switch (type)
                    {
                        case DxTabPaneTransitionType.Fade:
                            base.TransitionType = DevExpress.Utils.Animation.Transitions.Fade;
                            break;
                        case DxTabPaneTransitionType.Slide:
                            base.TransitionType = DevExpress.Utils.Animation.Transitions.SlideFade;
                            break;
                        case DxTabPaneTransitionType.Push:
                            base.TransitionType = DevExpress.Utils.Animation.Transitions.Push;
                            break;
                        case DxTabPaneTransitionType.Shape:
                            base.TransitionType = DevExpress.Utils.Animation.Transitions.Shape;
                            break;
                        default:
                            base.TransitionType = DevExpress.Utils.Animation.Transitions.Fade;
                            break;
                    }

                    DxTabPaneTransitionType time = value & DxTabPaneTransitionType.AllTimes;
                    switch (time)
                    {
                        case DxTabPaneTransitionType.Fast:
                            this.TransitionAnimationProperties.FrameCount = 50;                  // Celkový čas = interval * count
                            this.TransitionAnimationProperties.FrameInterval = 2 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
                            break;
                        case DxTabPaneTransitionType.Medium:
                            this.TransitionAnimationProperties.FrameCount = 100;                 // Celkový čas = interval * count
                            this.TransitionAnimationProperties.FrameInterval = 2 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
                            break;
                        case DxTabPaneTransitionType.Slow:
                            this.TransitionAnimationProperties.FrameCount = 250;                 // Celkový čas = interval * count
                            this.TransitionAnimationProperties.FrameInterval = 2 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
                            break;
                        case DxTabPaneTransitionType.VerySlow:
                            this.TransitionAnimationProperties.FrameCount = 500;                 // Celkový čas = interval * count
                            this.TransitionAnimationProperties.FrameInterval = 2 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
                            break;
                        default:
                            this.TransitionAnimationProperties.FrameCount = 100;                 // Celkový čas = interval * count
                            this.TransitionAnimationProperties.FrameInterval = 2 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
                            break;
                    }
                }
            }
        }
        private DxTabPaneTransitionType _TransitionType;
        /// <summary>
        /// Aktivuje vlastní eventy
        /// </summary>
        private void InitEvents()
        {
            this.TransitionManager.BeforeTransitionStarts += TransitionManager_BeforeTransitionStarts;
            this.TransitionManager.AfterTransitionEnds += TransitionManager_AfterTransitionEnds;
            this.SelectedPageChanging += DxTabPane_SelectedPageChanging;
            this.SelectedPageChanged += DxTabPane_SelectedPageChanged;
        }
        /// <summary>
        /// Deaktivuje vlastní eventy
        /// </summary>
        private void RemoveEvents()
        {
            this.TransitionManager.BeforeTransitionStarts -= TransitionManager_BeforeTransitionStarts;
            this.TransitionManager.AfterTransitionEnds -= TransitionManager_AfterTransitionEnds;
            this.SelectedPageChanging -= DxTabPane_SelectedPageChanging;
            this.SelectedPageChanged -= DxTabPane_SelectedPageChanged;
        }
        #endregion
        #region Přepínání záložek a volání událostí pro podporu deaktivace a aktivace správné stránky
        /// <summary>
        /// Obsahuje true, pokud jsme v procesu přepínání záložek, false v běžném stavu
        /// </summary>
        public bool PageChangingIsRunning { get; private set; }
        /// <summary>
        /// Obsahuje dřívější aktivní stránku před procesem přepínání záložek
        /// </summary>
        public DevExpress.XtraBars.Navigation.TabNavigationPage PageChangingPageOld { get; private set; }
        /// <summary>
        /// Obsahuje novou aktivní stránku po procesu přepínání záložek
        /// </summary>
        public DevExpress.XtraBars.Navigation.TabNavigationPage PageChangingPageNew { get; private set; }

        /// <summary>
        /// Zajistí události pro přípravu obsahu nové stránky před přepnutím na ni (stránka dosud není vidět)
        /// </summary>
        /// <param name="pageNew"></param>
        private void RunPageChangingPrepare(DevExpress.XtraBars.Navigation.TabNavigationPage pageNew)
        {
            TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args = new TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>(pageNew);
            OnPageChangingPrepare(args);
            PageChangingPrepare?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se pro přípravu obsahu nové stránky před přepnutím na ni (stránka dosud není vidět)
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnPageChangingPrepare(TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args) { }
        /// <summary>
        /// Volá se při přípravu obsahu nové stránky před přepnutím na ni (stránka dosud není vidět)
        /// </summary>
        public event EventHandler<TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>> PageChangingPrepare;

        /// <summary>
        /// Zajistí události pro aktivaci obsahu nové stránky po přepnutím na ni (stránka už je vidět)
        /// </summary>
        /// <param name="pageNew"></param>
        private void RunPageChangingActivate(DevExpress.XtraBars.Navigation.TabNavigationPage pageNew)
        {
            TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args = new TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>(pageNew);
            OnPageChangingActivate(args);
            PageChangingActivate?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se pro aktivaci obsahu nové stránky po přepnutím na ni (stránka už je vidět)
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnPageChangingActivate(TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args) { }
        /// <summary>
        /// Volá se při aktivaci obsahu nové stránky po přepnutím na ni (stránka už je vidět)
        /// </summary>
        public event EventHandler<TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>> PageChangingActivate;

        /// <summary>
        /// Zajistí události pro deaktivaci obsahu staré stránky před přepnutím z ní (stránka je dosud vidět)
        /// </summary>
        /// <param name="pageOld"></param>
        private void RunPageChangingDeactivate(DevExpress.XtraBars.Navigation.TabNavigationPage pageOld)
        {
            TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args = new TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>(pageOld);
            OnPageChangingDeactivate(args);
            PageChangingDeactivate?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se pro deaktivaci obsahu staré stránky před přepnutím z ní (stránka je dosud vidět)
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnPageChangingDeactivate(TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args) { }
        /// <summary>
        /// Volá se při deaktivaci obsahu staré stránky před přepnutím z ní (stránka je dosud vidět)
        /// </summary>
        public event EventHandler<TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>> PageChangingDeactivate;

        /// <summary>
        /// Zajistí události pro uvolnění obsahu staré stránky před přepnutím z ní (stránka už není vidět)
        /// </summary>
        /// <param name="pageOld"></param>
        private void RunPageChangingRelease(DevExpress.XtraBars.Navigation.TabNavigationPage pageOld)
        {
            TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args = new TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>(pageOld);
            OnPageChangingRelease(args);
            PageChangingRelease?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se pro uvolnění obsahu staré stránky před přepnutím z ní (stránka už není vidět)
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnPageChangingRelease(TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> args) { }
        /// <summary>
        /// Volá se při uvolnění obsahu staré stránky před přepnutím z ní (stránka už není vidět)
        /// </summary>
        public event EventHandler<TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage>> PageChangingRelease;

        // Pořadí eventů v konstuktoru:
        //     DxTabPane_SelectedPageChanging;
        //     DxTabPane_SelectedPageChanged;
        // Pořadí eventů při přepínání stránky s transicí:        SelectedPageIndex
        //     DxTabPane_SelectedPageChanging;                           old           mám k dispozici old i new page v argumentu
        //     TransitionManager_BeforeTransitionStarts;                 old
        //     DxTabPane_SelectedPageChanged;                            new
        //     TransitionManager_AfterTransitionEnds;                    new

        private void TransitionManager_BeforeTransitionStarts(DevExpress.Utils.Animation.ITransition transition, System.ComponentModel.CancelEventArgs e)
        {
        }
        private void TransitionManager_AfterTransitionEnds(DevExpress.Utils.Animation.ITransition transition, EventArgs e)
        {
        }
        /// <summary>
        /// Na začátku přepínání záložek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DxTabPane_SelectedPageChanging(object sender, DevExpress.XtraBars.Navigation.SelectedPageChangingEventArgs e)
        {
            this.PageChangingPageOld = e.OldPage as DevExpress.XtraBars.Navigation.TabNavigationPage;
            this.PageChangingPageNew = e.Page as DevExpress.XtraBars.Navigation.TabNavigationPage;
            this.PageChangingIsRunning = true;
            this.RunPageChangingDeactivate(this.PageChangingPageOld);
            this.RunPageChangingPrepare(this.PageChangingPageNew);
        }
        /// <summary>
        /// Na konci přepínání záložek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DxTabPane_SelectedPageChanged(object sender, DevExpress.XtraBars.Navigation.SelectedPageChangedEventArgs e)
        {
            this.RunPageChangingActivate(this.PageChangingPageNew);
            this.RunPageChangingRelease(this.PageChangingPageOld);
            this.PageChangingIsRunning = false;

        }
        #endregion
    }
    #region enum DxTabPaneTransitionType
    /// <summary>
    /// Typ přechodového efektu v <see cref="DxTabPane"/>
    /// </summary>
    [Flags]
    public enum DxTabPaneTransitionType 
    {
        None = 0,
        
        Fast = 0x0001,
        Medium = 0x0002,
        Slow = 0x0004,
        VerySlow = 0x0008,

        Fade = 0x0100,
        Slide = 0x0200,
        Push = 0x0400,
        Shape = 0x0800,

        FadeFast = Fade | Fast,
        FadeMedium = Fade | Medium,
        FadeSlow = Fade | Slow,

        SlideFast = Slide | Fast,
        SlideMedium = Slide | Medium,
        SlideSlow = Slide | Slow,

        PushFast = Push | Fast,
        PushMedium = Push | Medium,
        PushSlow = Push | Slow,

        ShapeFast = Shape | Fast,
        ShapeMedium = Shape | Medium,
        ShapeSlow = Shape | Slow,

        Default = FadeFast,

        AllTimes = Fast | Medium | Slow | VerySlow,
        AllTypes = Fade | Slide | Push | Shape
    }
    #endregion
    #endregion
    #region DxLabelControl
    /// <summary>
    /// LabelControl
    /// </summary>
    public class DxLabelControl : DevExpress.XtraEditors.LabelControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxLabelControl()
        {
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        }
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
    }
    #endregion
    #region DxLabelControl
    /// <summary>
    /// TextEdit
    /// </summary>
    public class DxTextEdit : DevExpress.XtraEditors.TextEdit
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTextEdit()
        {
            EnterMoveNextControl = true;
        }
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
    }
    #endregion
    #region DxMemoEdit
    /// <summary>
    /// MemoEdit
    /// </summary>
    public class DxMemoEdit : DevExpress.XtraEditors.MemoEdit
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
    }
    #endregion
    #region DxImageComboBoxEdit
    /// <summary>
    /// ImageComboBoxEdit
    /// </summary>
    public class DxImageComboBoxEdit : DevExpress.XtraEditors.ImageComboBoxEdit
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
    }
    #endregion
    #region DxSpinEdit
    /// <summary>
    /// SpinEdit
    /// </summary>
    public class DxSpinEdit : DevExpress.XtraEditors.SpinEdit
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
    }
    #endregion
    #region DxCheckEdit
    /// <summary>
    /// CheckEdit
    /// </summary>
    public class DxCheckEdit : DevExpress.XtraEditors.CheckEdit
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
    }
    #endregion
    #region DxListBoxControl
    /// <summary>
    /// ListBoxControl
    /// </summary>
    public class DxListBoxControl : DevExpress.XtraEditors.ListBoxControl
    {
        #region Public členy
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxListBoxControl()
        {
            ReorderByDragEnabled = false;
            ReorderIconColor = Color.FromArgb(192, 116, 116, 96);
            ReorderIconColorHot = Color.FromArgb(220, 160, 160, 122);
        }
        /// <summary>
        /// Událost volaná po vykreslení základu Listu, před vykreslením Reorder ikony
        /// </summary>
        public event PaintEventHandler PaintList;
        /// <summary>
        /// Pole, obsahující informace o právě viditelných prvcích ListBoxu a jejich aktuální souřadnice
        /// </summary>
        public Tuple<int, object, Rectangle>[] VisibleItems
        {
            get
            {
                List<Tuple<int, object, Rectangle>> visibleItems = new List<Tuple<int, object, Rectangle>>();
                int topIndex = this.TopIndex;
                int index = (topIndex > 0 ? topIndex - 1 : topIndex);
                int count = this.ItemCount;
                while (index < count)
                {
                    Rectangle? bounds = GetItemBounds(index);
                    if (bounds.HasValue)
                        visibleItems.Add(new Tuple<int, object, Rectangle>(index, this.Items[index], bounds.Value));
                    else if (index > topIndex)
                        break;
                    index++;
                }
                return visibleItems.ToArray();
            }
        }
        /// <summary>
        /// Pole, obsahující informace o právě selectovaných prvcích ListBoxu a jejich aktuální souřadnice
        /// </summary>
        public Tuple<int, object, Rectangle?>[] SelectedItemsInfo
        {
            get
            {
                List<Tuple<int, object, Rectangle?>> selectedItems = new List<Tuple<int, object, Rectangle?>>();
                foreach (var index in this.SelectedIndices)
                {
                    Rectangle? bounds = GetItemBounds(index);
                    selectedItems.Add(new Tuple<int, object, Rectangle?>(index, this.Items[index], bounds));
                }
                return selectedItems.ToArray();
            }
        }

        /// <summary>
        /// Přídavek k výšce jednoho řádku ListBoxu v pixelech.
        /// Hodnota 0 a záporná: bude nastaveno <see cref="DevExpress.XtraEditors.BaseListBoxControl.ItemAutoHeight"/> = true.
        /// Kladná hodnota přidá daný počet pixelů nad a pod text = zvýší výšku řádku o 2x <see cref="ItemHeightPadding"/>.
        /// Hodnota vyšší než 10 se akceptuje jako 10.
        /// </summary>
        public int ItemHeightPadding
        {
            get { return _ItemHeightPadding; }
            set
            {
                if (value > 0)
                {
                    int padding = (value > 10 ? 10 : value);
                    int fontheight = this.Appearance.GetFont().Height;
                    this.ItemAutoHeight = false;
                    this.ItemHeight = fontheight + (2 * padding);
                    _ItemHeightPadding = padding;
                }
                else
                {
                    this.ItemAutoHeight = true;
                    _ItemHeightPadding = 0;
                }
            }
        }
        private int _ItemHeightPadding = 0;
        #endregion
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region Overrides
        /// <summary>
        /// Při vykreslování
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this.PaintList?.Invoke(this, e);
            this.PaintOnMouseItem(e);
        }
        /// <summary>
        /// Po stisku klávesy
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            OnMouseItemIndex = -1;
        }
        /// <summary>
        /// Po vstupu myši: určíme myšoaktivní prvek listu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.DetectOnMouseItemAbsolute(Control.MousePosition);
        }
        /// <summary>
        /// Po pohybu myši: určíme myšoaktivní prvek listu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            this.DetectOnMouseItem(e.Location);
        }
        /// <summary>
        /// Po odchodu myši: zrušíme myšoaktivní prvek listu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.DetectOnMouseItem(null);
        }
        #endregion
        #region Přesouvání prvků pomocí myši
        /// <summary>
        /// Povolení pro reorder prvků pomocí myši (pak se zobrazí ikona pro drag)
        /// </summary>
        public bool ReorderByDragEnabled { get; set; }
        /// <summary>
        /// Barva ikonky pro reorder <see cref="ReorderByDragEnabled"/>, v běžném stavu.
        /// Průhlednost je podporována.
        /// </summary>
        public Color ReorderIconColor { get; set; }
        /// <summary>
        /// Barva ikonky pro reorder <see cref="ReorderByDragEnabled"/>, v běžném stavu.
        /// Průhlednost je podporována.
        /// </summary>
        public Color ReorderIconColorHot { get; set; }
        /// <summary>
        /// Určí a uloží si index myšoaktivního prvku na dané absolutní souřadnici
        /// </summary>
        /// <param name="mouseAbsolutePosition"></param>
        protected void DetectOnMouseItemAbsolute(Point mouseAbsolutePosition)
        {
            Point relativePoint = this.PointToClient(mouseAbsolutePosition);
            DetectOnMouseItem(relativePoint);
        }
        /// <summary>
        /// Určí a uloží si index myšoaktivního prvku na dané relativní souřadnici
        /// </summary>
        /// <param name="mouseRelativePosition"></param>
        protected void DetectOnMouseItem(Point? mouseRelativePosition)
        {
            int index = -1;
            float ratio = 0f;
            if (mouseRelativePosition.HasValue)
            {
                index = this.IndexFromPoint(mouseRelativePosition.Value);
                float ratioX = (float)mouseRelativePosition.Value.X / (float)this.Width; // Relativní pozice myši na ose X v rámci celého controlu v rozmezí 0.0 až 1.0
                ratio = 0.30f + (ratioX < 0.5f ? 0f : 1.40f * (ratioX - 0.5f));          // 0.30 je pevná dolní hodnota. Ta platí pro pozici myši 0.0 až 0.5. Pak se ratio zvyšuje od 0.30 do 1.0.
            }
            OnMouseItemIndex = index;
            OnMouseItemAlphaRatio = ratio;
            this.DetectOnMouseCursor(mouseRelativePosition);
        }
        /// <summary>
        /// Určí a uloží si druh kurzoru podle dané relativní souřadnice
        /// </summary>
        /// <param name="mouseRelativePosition"></param>
        protected void DetectOnMouseCursor(Point? mouseRelativePosition)
        {
            bool isCursorActive = false;
            if (mouseRelativePosition.HasValue)
            {
                var iconBounds = OnMouseIconBounds;
                isCursorActive = (iconBounds.HasValue && iconBounds.Value.Contains(mouseRelativePosition.Value));
            }

            if (isCursorActive != _IsMouseOverReorderIcon)
            {
                _IsMouseOverReorderIcon = isCursorActive;
                if (ReorderByDragEnabled)
                {
                    this.Cursor = (isCursorActive ? Cursors.SizeNS : Cursors.Default);
                    this.Invalidate();                         // Překreslení => jiná barva ikony
                }
            }
        }
        /// <summary>
        /// Zajistí vykreslení ikony pro ReorderByDrag a případně i přemisťovaného prvku
        /// </summary>
        /// <param name="e"></param>
        private void PaintOnMouseItem(PaintEventArgs e)
        {
            if (!ReorderByDragEnabled) return;

            Rectangle? iconBounds = OnMouseIconBounds;
            if (!iconBounds.HasValue) return;

            int wb = iconBounds.Value.Width;
            int x0 = iconBounds.Value.X;
            int yc = iconBounds.Value.Y + iconBounds.Value.Height / 2;
            Color iconColor = (_IsMouseOverReorderIcon ? ReorderIconColorHot : ReorderIconColor);
            float alphaRatio = OnMouseItemAlphaRatio;
            int alpha = (int)((float)iconColor.A * alphaRatio);
            iconColor = Color.FromArgb(alpha, iconColor);
            using (SolidBrush brush = new SolidBrush(iconColor))
            {
                e.Graphics.FillRectangle(brush, x0, yc - 6, wb, 2);
                e.Graphics.FillRectangle(brush, x0, yc - 1, wb, 2);
                e.Graphics.FillRectangle(brush, x0, yc + 4, wb, 2);
            }
        }
        /// <summary>
        /// Příznak, že myš je nad ikonou pro přesouvání prvku, a kurzor je tedy upraven na šipku nahoru/dolů.
        /// Pak tedy MouseDown a MouseMove bude provádět Reorder prvku.
        /// </summary>
        private bool _IsMouseOverReorderIcon = false;
        /// <summary>
        /// Index prvku, nad kterým se pohybuje myš
        /// </summary>
        public int OnMouseItemIndex
        {
            get 
            {
                if (_OnMouseItemIndex >= this.ItemCount)
                    _OnMouseItemIndex = -1;
                return _OnMouseItemIndex;
            }
            protected set
            {
                if (value != _OnMouseItemIndex)
                {
                    _OnMouseItemIndex = value;
                    this.Invalidate();
                }
            }
        }
        private int _OnMouseItemIndex = -1;
        /// <summary>
        /// Blízkost myši k ikoně na ose X v poměru: 0.15 = úplně daleko, 1.00 = přímo na ikoně.
        /// Ovlivní průhlednost barvy (kanál Alpha).
        /// </summary>
        public float OnMouseItemAlphaRatio
        {
            get { return _OnMouseItemAlphaRatio; }
            protected set
            {
                float ratio = (value < 0.15f ? 0.15f : (value > 1.0f ? 1.0f : value));
                float diff = ratio - _OnMouseItemAlphaRatio;
                if (diff <= -0.04f || diff >= 0.04f)
                {
                    _OnMouseItemAlphaRatio = ratio;
                    this.Invalidate();
                }
            }
        }
        private float _OnMouseItemAlphaRatio = 0.15f;
        /// <summary>
        /// Souřadnice prostoru pro myší ikonu vpravo v myšoaktivním řádku
        /// </summary>
        protected Rectangle? OnMouseIconBounds
        {
            get
            {
                if (_OnMouseIconBoundsIndex != _OnMouseItemIndex)
                {
                    _OnMouseIconBounds = GetOnMouseIconBounds(_OnMouseItemIndex);
                    _OnMouseIconBoundsIndex = _OnMouseItemIndex;
                }
                return _OnMouseIconBounds;
            }
        }
        /// <summary>
        /// Souřadnice prostoru ikony vpravo pro myš
        /// </summary>
        private Rectangle? _OnMouseIconBounds = null;
        /// <summary>
        /// Index prvku, pro který je vypočtena souřadnice a uložena v <see cref="_OnMouseIconBounds"/>
        /// </summary>
        private int _OnMouseIconBoundsIndex = -1;
        /// <summary>
        /// Vrátí souřadnice prvku na daném indexu. Volitelně může provést kontrolu na to, zda daný prvek je ve viditelné oblasti Listu. Pokud prvek neexistuje nebo není vidět, vrací null.
        /// Vrácené souřadnice jsou relativní v prostoru this ListBoxu.
        /// </summary>
        /// <param name="itemIndex"></param>
        /// <param name="onlyVisible"></param>
        /// <returns></returns>
        public Rectangle? GetItemBounds(int itemIndex, bool onlyVisible = true)
        {
            if (itemIndex < 0 || itemIndex >= this.ItemCount) return null;

            Rectangle itemBounds = this.GetItemRectangle(itemIndex);
            if (onlyVisible)
            {   // Pokud chceme souřadnice pouze viditelného prvku, pak prověříme souřadnice prvku proti souřadnici prostoru v ListBoxu:
                Rectangle listBounds = this.ClientRectangle;
                if (itemBounds.Right <= listBounds.X || itemBounds.X >= listBounds.Right || itemBounds.Bottom <= listBounds.Y || itemBounds.Y >= listBounds.Bottom) 
                    return null;   // Prvek není vidět
            }

            return itemBounds;
        }
        /// <summary>
        /// Vrátí souřadnici prostoru pro myší ikonu
        /// </summary>
        /// <param name="onMouseItemIndex"></param>
        /// <returns></returns>
        protected Rectangle? GetOnMouseIconBounds(int onMouseItemIndex)
        {
            Rectangle? itemBounds = this.GetItemBounds(onMouseItemIndex, true);
            if (!itemBounds.HasValue || itemBounds.Value.Width < 35) return null;        // Pokud prvek neexistuje, nebo není vidět, nebo je příliš úzký => vrátíme null

            int wb = 14;
            int x0 = itemBounds.Value.Right - wb - 6;
            int yc = itemBounds.Value.Y + itemBounds.Value.Height / 2;
            Rectangle iconBounds = new Rectangle(x0 - 1, itemBounds.Value.Y, wb + 1, itemBounds.Value.Height);
            return iconBounds;
        }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
    }
    #endregion
    #region DxSimpleButton
    /// <summary>
    /// SimpleButton
    /// </summary>
    public class DxSimpleButton : DevExpress.XtraEditors.SimpleButton
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
        #region Resize a SvgImage

        #endregion
    }
    #endregion
    #region DxCheckButton
    /// <summary>
    /// CheckButton
    /// </summary>
    public class DxCheckButton : DevExpress.XtraEditors.CheckButton
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
        #region Resize a SvgImage

        #endregion
    }
    #endregion
    #region DxImagePickerListBox
    /// <summary>
    /// ListBox nabízející DevExpress Resources
    /// </summary>
    public class DxImagePickerListBox : DxPanelControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxImagePickerListBox()
        {
            this.Initialize();
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        protected void Initialize()
        {
            _ClipboardCopyIndex = 0;

            _FilterClearButton = DxComponent.CreateDxMiniButton(0, 0, 20, 20, this, _FilterClearButtonClick, 
                resourceName: "svgimages/spreadsheet/clearfilter.svg",
                toolTipTitle: "Zrušit filtr", toolTipText: "Zruší filtr, budou zobrazeny všechny dostupné zdroje.");
            _FilterClearButton.MouseEnter += _AnyControlEnter;
            _FilterClearButton.Enter += _AnyControlEnter;

            _FilterText = DxComponent.CreateDxTextEdit(27, 0, 200, this,
                toolTipTitle: "Filtr Resources", toolTipText: "Vepište část názvu zdroje.\r\nLze použít filtrační znaky * a ?.\r\nLze zadat víc filtrů, oddělených středníkem nebo čárkou.\r\n\r\nNapříklad: 'add' zobrazí všechny položky obsahující 'add',\r\n'*close*.svg' zobrazí něco obsahující 'close' s příponou '.svg',\r\n'*close*.svg;*delete*' zobrazí prvky close nebo delete");
            _FilterText.MouseEnter += _AnyControlEnter;
            _FilterText.Enter += _AnyControlEnter;
            _FilterText.KeyUp += _FilterText_KeyUp;

            _ListCopyButton = DxComponent.CreateDxMiniButton(230, 0, 20, 20, this, _ListCopyButtonClick, 
                resourceName: "svgimages/xaf/action_copy.svg", hotResourceName: "svgimages/xaf/action_modeldifferences_copy.svg",
                toolTipTitle: "Zkopírovat", toolTipText: "Označené řádky v seznamu zdrojů vloží do schránky, jako Ctrl+C.");
            _ListCopyButton.MouseEnter += _AnyControlEnter;
            _ListCopyButton.Enter += _AnyControlEnter;

            _ListBox = DxComponent.CreateDxListBox(DockStyle.None, parent: this, selectionMode: SelectionMode.MultiExtended, itemHeight: 32,
                toolTipTitle: "Seznam Resources", toolTipText: "Označte jeden nebo více řádků, klávesou Ctrl+C zkopírujete názvy Resources jako kód C#.");
            _ListBox.MouseEnter += _AnyControlEnter;
            _ListBox.Enter += _AnyControlEnter;
            _ListBox.KeyUp += _ListBox_KeyUp;
            _ListBox.PaintList += _ListBox_PaintList;
            _ListBox.SelectedIndexChanged += _ListBox_SelectedIndexChanged;

            _ResourceNames = DxComponent.GetResourceKeys();
            _ResourceFilter = "";
            _StatusText = "";

            _FillListByFilter();
        }
        /// <summary>
        /// Po vstupu do jakéhokoli controlu nastavím výchozí status text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _AnyControlEnter(object sender, EventArgs e)
        {
            _ResetStatusText();
        }
        /// <summary>
        /// Po změně velikosti provedu rozmístění prvků
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this.DoLayout();
        }
        /// <summary>
        /// Správně rozmístí prvky
        /// </summary>
        protected void DoLayout()
        {
            var size = this.ClientSize;
            int mx = 3;
            int my = 3;
            int x = mx;
            int y = my;
            int sx = 2;
            int sy = 2;
            int r = size.Width - mx;
            int w = size.Width - mx - mx;
            int h = size.Height - my - my;
            int txs = _FilterText.Height;
            int bts = txs + sx;
            
            _FilterClearButton.Bounds = new Rectangle(x, y, txs, txs);
            _FilterText.Bounds = new Rectangle(x + bts, y, w - bts - bts, txs);
            _ListCopyButton.Bounds = new Rectangle(r - txs, y, txs, txs);

            y = _FilterText.Bounds.Bottom + sy;
            _ListBox.Bounds = new Rectangle(x, y, w, h - y);
        }
        /// <summary>
        /// Clear filter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FilterClearButtonClick(object sender, EventArgs e) 
        {
            _ResourceFilter = "";
            _FillListByFilter();
            _FilterText.Focus();
        }
        /// <summary>
        /// FilterText: Po klávese ve filtru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FilterText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Enter)
                _ListBox.Focus();
            else if (e.KeyCode == Keys.Home || e.KeyCode == Keys.End || e.KeyCode == Keys.Up /* || e.KeyCode == Keys.Down */ || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Tab || e.KeyCode == Keys.Escape)
            { }
            else if (e.Modifiers == Keys.Control)
            { }
            else
                _FillListByFilter();
        }
        /// <summary>
        /// Copy selected items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListCopyButtonClick(object sender, EventArgs e) 
        {
            _FilterText.Focus();
            _DoCopyClipboard();
        }
        /// <summary>
        /// ListBox: Obsluha kláves: detekce Ctrl+C
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.C)) _DoCopyClipboard();
        }
        /// <summary>
        /// Po změně řádku v ListBoxu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedItems = _ListBox.SelectedItemsInfo;
            StatusText = "Označeny řádky: " + selectedItems.Length.ToString();
        }
        /// <summary>
        /// Vykreslí obrázky do ListBoxu do viditelných prvků
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_PaintList(object sender, PaintEventArgs e)
        {
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = DxComponent.GetSvgPalette();
            var visibleItems = _ListBox.VisibleItems;
            foreach (var visibleItem in visibleItems)
            {
                string resourceName = visibleItem.Item2 as string;
                Rectangle itemBounds = visibleItem.Item3;
                var image = DxComponent.GetImageFromResource(resourceName, out Size size, maxSize: new Size(32, 32), optimalSvgSize: new Size(32, 32), svgPalette: svgPalette);
                if (image != null)
                {
                    Point imagePoint = new Point((itemBounds.Right - 24 - size.Width / 2), itemBounds.Top + ((itemBounds.Height - size.Height) / 2));
                    Rectangle imageBounds = new Rectangle(imagePoint, size);
                    e.Graphics.DrawImage(image, imageBounds);
                }
            }
        }
        DxSimpleButton _FilterClearButton;
        DxTextEdit _FilterText;
        DxSimpleButton _ListCopyButton;
        DxListBoxControl _ListBox;
        #region Seznam resources - získání, filtrování, tvorba Image, CopyToClipboard
        /// <summary>
        /// Do seznamu ListBox vloží zdroje odpovídající aktuálnímu filtru
        /// </summary>
        private void _FillListByFilter()
        {
            string[] resources = _GetResourcesByFilter();
            _FilteredItemsCount = resources.Length;

            _ListBox.SuspendLayout();
            _ListBox.Items.Clear();
            _ListBox.Items.AddRange(resources);
            _ListBox.ResumeLayout(false);
            _ListBox.PerformLayout();

            _ResetStatusText();
        }
        /// <summary>
        /// Vrátí pole zdrojů vyhovujících aktuálnímu filtru
        /// </summary>
        /// <returns></returns>
        private string[] _GetResourcesByFilter()
        {
            if (_ResourceNames == null || _ResourceNames.Length == 0) return new string[0];

            var filter = _ResourceFilter;
            if (String.IsNullOrEmpty(filter)) return _ResourceNames;

            filter = RegexSupport.ReplaceGreenWildcards(filter);

            string[] result = _GetResourcesByFilter(filter);
            if (result.Length == 0)
                result = _GetResourcesByFilter("*" + filter + "*");

            return result;
        }
        /// <summary>
        /// Vrátí pole zdrojů vyhovujících danému filtru
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string[] _GetResourcesByFilter(string filter)
        {
            var regexes = RegexSupport.CreateWildcardsRegexes(filter);
            var result = RegexSupport.FilterByRegexes(_ResourceNames, regexes);
            return result.ToArray();
        }
        /// <summary>
        /// Vloží do Clipboardu kód obsahující aktuálně vybrané texty
        /// </summary>
        private void _DoCopyClipboard()
        {
            var selectedItems = _ListBox.SelectedItemsInfo;
            int rowCount = selectedItems.Length;
            if (rowCount > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var selectedItem in selectedItems)
                {
                    _ClipboardCopyIndex++;
                    string resourceName = selectedItem.Item2 as string;
                    sb.AppendLine($"  string resource{_ClipboardCopyIndex} = \"{resourceName}\";");
                }
                if (sb.Length > 0)
                {
                    Clipboard.Clear();
                    Clipboard.SetText(sb.ToString());
                }

                StatusText = "Položky zkopírovány do schránky: " + rowCount.ToString();
            }
            else
            {
                StatusText = $"Nejsou označeny žádné položky.";
            }
        }
        /// <summary>
        /// Filtrační text z textboxu
        /// </summary>
        private string _ResourceFilter { get { return this._FilterText.Text.Trim(); } set { this._FilterText.Text = (value ?? ""); } }
        /// <summary>
        /// Jména všech zdrojů
        /// </summary>
        private string[] _ResourceNames;
        /// <summary>
        /// Číslo pro číslování proměnných do Clipboardu
        /// </summary>
        private int _ClipboardCopyIndex;
        /// <summary>
        /// Počet aktuálně filtrovaných položek
        /// </summary>
        private int _FilteredItemsCount;
        #endregion
        #region Podpora pro StatusBar
        /// <summary>
        /// Text vhodný pro zobrazení ve statusbaru. Setování nové hodnoty vyvolá event <see cref="StatusTextChanged"/>
        /// </summary>
        public string StatusText
        {
            get { return _StatusText; }
            protected set
            {
                bool isChanged = !String.Equals(value, _StatusText);
                _StatusText = value;
                if (isChanged)
                    StatusTextChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private string _StatusText;
        private void _ResetStatusText()
        {
            int allItemsCount = _ResourceNames.Length;
            int filteredItemsCount = _FilteredItemsCount;
            bool hasFilter = !String.IsNullOrEmpty(_ResourceFilter);
            string filter = (hasFilter ? "'" + _ResourceFilter + "'" : "");
            if (allItemsCount == 0)
                StatusText = $"Neexistují žádné položky";
            else if (!hasFilter)
                StatusText = $"Zobrazeny všechny položky: {allItemsCount}";
            else if (filteredItemsCount == 0)
                StatusText = $"Zadanému filtru {filter} nevyhovuje žádná položka";
            else if (filteredItemsCount == allItemsCount)
                StatusText = $"Zadanému filtru {filter} vyhovují všechny položky: {filteredItemsCount}";
            else
                StatusText = $"Zadanému filtru {filter} vyhovují zobrazené položky: {filteredItemsCount}";
        }
        /// <summary>
        /// Událost vyvolaná po změně textu v <see cref="StatusText"/>
        /// </summary>
        public event EventHandler StatusTextChanged;
        #endregion
    }
    #endregion
    #region DxChartControl
    /// <summary>
    /// Přímý potomek <see cref="DevExpress.XtraCharts.ChartControl"/> pro použití v ASOL.Nephrite
    /// </summary>
    internal class DxChartControl : DevExpress.XtraCharts.ChartControl
    {
        #region Support pro práci s grafem
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxChartControl()
        {
            this.HardwareAcceleration = true;
        }
        /// <summary>
        /// XML definice vzhledu grafu (Layout), aktuálně načtený z komponenty
        /// </summary>
        public string ChartXmlLayout
        {
            get { return GetGuiValue<string>(() => this._GetChartXmlLayout()); }
            set { SetGuiValue<string>(v => this._SetChartXmlLayout(v), value); }
        }
        /// <summary>
        /// Reference na data zobrazená v grafu
        /// </summary>
        public object ChartDataSource
        {
            get { return GetGuiValue<object>(() => this._GetChartDataSource()); }
            set { SetGuiValue<object>(v => this._SetChartDataSource(v), value); }
        }
        /// <summary>
        /// Vloží do grafu layout, a současně data, v jednom kroku
        /// </summary>
        /// <param name="xmlLayout">XML definice vzhledu grafu</param>
        /// <param name="dataSource">Tabulka s daty, zdroj dat grafu</param>
        public void SetChartXmlLayoutAndDataSource(string xmlLayout, object dataSource)
        {
            _ValidChartXmlLayout = xmlLayout;              // Uloží se požadovaný text definující layout
            _ChartDataSource = dataSource;                 // Uloží se WeakReference
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Zajistí editaci grafu pomocí Wizarda DevExpress
        /// </summary>
        public void ShowChartWizard()
        {
            if (!IsChartWorking)
                throw new InvalidOperationException($"V tuto chvíli nelze editovat graf, dosud není načten nebo není definován.");

            bool valid = DxChartDesigner.DesignChart(this, "Upravte graf...", true, false);

            // Wizard pracuje nad naším controlem, veškeré změny ve Wizardu provedené se ihned promítají do našeho grafu.
            // Pokud uživatel dal OK, chceme změny uložit i do příště,
            // pokud ale dal Cancel, pak změny chceme zahodit a vracíme se k původnímu layoutu:
            if (valid)
            {
                string newLayout = _GetLayoutFromControl();
                _ValidChartXmlLayout = newLayout;
                OnChartXmlLayoutEdited();
            }
            else
            {
                _RestoreChartXmlLayout();
            }
        }
        /// <summary>
        /// Je voláno po editaci layoutu, vyvolá event <see cref="ChartXmlLayoutEdited"/>
        /// </summary>
        protected virtual void OnChartXmlLayoutEdited()
        {
            ChartXmlLayoutEdited?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Událost, kdy uživatel změnil data (vyvoláno metodou <see cref="ShowChartWizard"/>), a v designeru uložil změnu dat.
        /// V tuto chvíli je již nový layout k dispozici v <see cref="ChartXmlLayout"/>.
        /// </summary>
        public event EventHandler ChartXmlLayoutEdited;
        #endregion
        #region private práce s layoutem a daty
        /// <summary>
        /// Vloží do grafu layout, ponechává data
        /// </summary>
        /// <param name="xmlLayout">XML definice vzhledu grafu</param>
        private void _SetChartXmlLayout(string xmlLayout)
        {
            _ValidChartXmlLayout = xmlLayout;              // Uloží se požadovaný text definující layout
            var dataSource = _ChartDataSource;             // Načteme z WeakReference
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Vrátí XML layout načtený přímo z grafu
        /// </summary>
        /// <returns></returns>
        private string _GetChartXmlLayout()
        {
            return _GetLayoutFromControl();
        }
        /// <summary>
        /// Vloží do grafu dříve platný layout (uložený v metodě <see cref="_SetChartXmlLayout(string)"/>), ponechává data.
        /// </summary>
        private void _RestoreChartXmlLayout()
        {
            var dataSource = _ChartDataSource;             // Tabulka z WeakReference
            string xmlLayout = _ValidChartXmlLayout;       // Uložený layout
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Vloží do grafu data, ponechává layout (může dojít k chybě)
        /// </summary>
        /// <param name="dataSource">Tabulka s daty, zdroj dat grafu</param>
        private void _SetChartDataSource(object dataSource)
        {
            _ChartDataSource = dataSource;                 // Uloží se WeakReference
            string xmlLayout = _ValidChartXmlLayout;       // Uložený layout
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Vrátí data grafu
        /// </summary>
        /// <returns></returns>
        private object _GetChartDataSource()
        {
            return _ChartDataSource;
        }
        /// <summary>
        /// Do grafu korektně vloží data i layout, ve správném pořadí.
        /// Pozor: tato metoda neukládá dodané objekty do lokálních proměnných <see cref="_ValidChartXmlLayout"/> a <see cref="_ChartDataSource"/>, pouze do controlu grafu!
        /// </summary>
        /// <param name="dataSource"></param>
        /// <param name="xmlLayout"></param>
        private void _SetChartXmlLayoutAndDataSource(string xmlLayout, object dataSource)
        {
            // Tato sekvence je důležitá, jinak dojde ke zbytečným chybám:
            try
            {
                this._SetLayoutToControl(xmlLayout);
            }
            finally
            {   // Datový zdroj do this uložím i po chybě, abych mohl i po vložení chybného layoutu otevřít editor:
                this.DataSource = dataSource;
            }
        }
        /// <summary>
        /// Fyzicky načte a vrátí Layout z aktuálního grafu
        /// </summary>
        /// <returns></returns>
        private string _GetLayoutFromControl()
        {
            string layout = null;
            if (IsChartValid)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    this.SaveToStream(ms);
                    layout = Encoding.UTF8.GetString(ms.GetBuffer());
                }
            }
            return layout;
        }
        /// <summary>
        /// Vloží daný string jako Layout do grafu. 
        /// Neřeší try catch, to má řešit volající včetně ošetření chyby.
        /// POZOR: tato metoda odpojí datový zdroj, proto je třeba po jejím skončení znovu vložit zdroj do _ChartControl.DataSource !
        /// </summary>
        /// <param name="layout"></param>
        private void _SetLayoutToControl(string layout)
        {
            if (IsChartValid)
            {
                try
                {
                    this.BeginInit();
                    this.Reset();
                    _SetLayoutToControlDirect(layout);
                }
                finally
                {
                    this.EndInit();
                }
            }
        }
        /// <summary>
        /// Vynuluje layout
        /// </summary>
        public void Reset()
        {
            this.DataSource = null;
            string emptyLayout = @"<?xml version='1.0' encoding='utf-8'?>
<ChartXmlSerializer version='20.1.6.0'>
  <Chart AppearanceNameSerializable='Default' SelectionMode='None' SeriesSelectionMode='Series'>
    <DataContainer ValidateDataMembers='true' BoundSeriesSorting='None'>
      <SeriesTemplate CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText='' />
    </DataContainer>
  </Chart>
</ChartXmlSerializer>";
            emptyLayout = emptyLayout.Replace("'", "\"");
            _SetLayoutToControlDirect(emptyLayout);

            this.ResetLegendTextPattern();
                    this.Series.Clear();
                    this.Legends.Clear();
                    this.Titles.Clear();
            this.AutoLayout = false;
            this.CalculatedFields.Clear();
            this.ClearCache();
            this.Diagram = null;
                }
        /// <summary>
        /// Fyzicky vloží daný string do layoutu grafu
        /// </summary>
        /// <param name="layout"></param>
        private void _SetLayoutToControlDirect(string layout)
        {
            byte[] buffer = (!String.IsNullOrEmpty(layout) ? Encoding.UTF8.GetBytes(layout) : new byte[0]);
            if (buffer.Length > 0)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(buffer))
                    this.LoadFromStream(ms);     // Pozor, zahodí data !!!
            }
        }
        /// <summary>
        /// Obsahuje true, pokud graf je platný (není null a není Disposed) 
        /// </summary>
        public bool IsChartValid { get { return (!this.IsDisposed); } }
        /// <summary>
        /// Obsahuje true, pokud graf je platný (není null a není Disposed) a obsahuje data (má datový zdroj)
        /// </summary>
        public bool IsChartWorking { get { return (IsChartValid && this.DataSource != null); } }
        /// <summary>
        /// Offline uložený layout grafu, který byl setovaný zvenku; používá se při refreshi dat pro nové vložení stávajícího layoutu do grafu. 
        /// Při public čtení layoutu se nevrací tento string, ale fyzicky se načte aktuálně použitý layout z grafu.
        /// </summary>
        private string _ValidChartXmlLayout;
        /// <summary>
        /// Reference na tabulku s daty grafu, nebo null
        /// </summary>
        private object _ChartDataSource
        {
            get
            {
                var wr = _ChartDataSourceWR;
                return ((wr != null && wr.TryGetTarget(out var table)) ? table : null);
            }
            set
            {
                _ChartDataSourceWR = ((value != null) ? new WeakReference<object>(value) : null);
            }
        }
        /// <summary>
        /// WeakReference na tabulku s daty grafu
        /// </summary>
        private WeakReference<object> _ChartDataSourceWR;
        #endregion
        #region ASOL standardní rozšíření
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region Invoke to GUI: run, get, set
        /// <summary>
        /// Metoda provede danou akci v GUI threadu
        /// </summary>
        /// <param name="action"></param>
        protected void RunInGui(Action action)
        {
            if (this.InvokeRequired)
                this.Invoke(action);
            else
                action();
        }
        /// <summary>
        /// Metoda vrátí hodnotu z GUI prvku, zajistí si invokaci GUI threadu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        protected T GetGuiValue<T>(Func<T> reader)
        {
            if (this.InvokeRequired)
                return (T)this.Invoke(reader);
            else
                return reader();
        }
        /// <summary>
        /// Metoda vloží do GUI prvku danou hodnotu, zajistí si invokaci GUI threadu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        protected void SetGuiValue<T>(Action<T> writer, T value)
        {
            if (this.InvokeRequired)
                this.Invoke(writer, value);
            else
                writer(value);
        }
        #endregion
    }
    #endregion
    #region DxChartDesigner
    /// <summary>
    /// Přímý potomek <see cref="DevExpress.XtraCharts.Designer.ChartDesigner"/> pro editaci definice grafu
    /// </summary>
    internal class DxChartDesigner : DevExpress.XtraCharts.Designer.ChartDesigner, Noris.Clients.Win.Components.IEscapeHandler
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="chart"></param>
        public DxChartDesigner(object chart) : base(chart) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="designerHost"></param>
        public DxChartDesigner(object chart, System.ComponentModel.Design.IDesignerHost designerHost) : base(chart, designerHost) { }
        /// <summary>
        /// Zobrazí designer pro daný graf, a vrátí true = OK
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="caption"></param>
        /// <param name="showActualData"></param>
        /// <param name="topMost"></param>
        /// <returns></returns>
        public static bool DesignChart(object chart, string caption, bool showActualData, bool topMost)
        {
            DxChartDesigner chartDesigner = new DxChartDesigner(chart);
            chartDesigner.Caption = caption;
            chartDesigner.ShowActualData = showActualData;
            var result = chartDesigner.ShowDialog(topMost);
            return (result == DialogResult.OK);
        }
        /// <summary>
        /// Desktop okno hlídá klávesu Escape: 
        /// c:\inetpub\wwwroot\Noris46\Noris\ClientImages\ClientWinForms\WinForms.Host\Windows\WDesktop.cs
        /// metoda escapeKeyFilter_EscapeKeyDown()
        /// Když dostane Escape, najde OnTop okno které implementuje IEscapeHandler, a zavolá zdejší metodu.
        /// My vrátíme true = OK, aby desktop dál ten Escape neřešil, a my se v klidu zavřeme nativním zpracováním klávesy Escape ve WinFormu.
        /// </summary>
        /// <returns></returns>
        bool Noris.Clients.Win.Components.IEscapeHandler.HandleEscapeKey()
        {
            return true;
        }
    }
    #endregion
}
