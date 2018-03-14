using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Djs.Common.Components
{
    /// <summary>
    /// Skin : Set of skins for individual items in GUI.
    /// <para/>
    /// Skin is partial class and can be extended by any "external" SkinSets, using this process:
    /// 1) class Skin is "partial", thus another code can write next part of Skin class;
    /// 2) in this definition ( partial class Skin { } ) can create new property and variable for new SkinSet type;
    /// 3) in this definition can create method for initialise this variable, for example: private void _MyControlSkinInit() { this._MyControlSkin = new MyControlSkinSet(this, "MyControl"); };
    /// 4) mark this method with attribute [Initialiser];
    /// 5) Skin init method will reflect own code, search for (Public | NonPublic | Instance) methods with attribute Initialiser;
    /// 6) Run this methods
    /// </summary>
    public partial class Skin
    {
        #region Public static SkinSets
        /// <summary>
        /// Color items for Modifier (common colors for all GUI items, dependent on Mouse state, and so on)
        /// </summary>
        public static SkinModifierSet Modifiers { get { return Instance._Modifiers; } }
        /// <summary>
        /// Color items for shadow colors
        /// </summary>
        public static SkinShadowSet Shadow { get { return Instance._Shadow; } }
        /// <summary>
        /// Color items for common control (BackColor, TextColor)
        /// </summary>
        public static SkinControlSet Control { get { return Instance._Control; } }
        /// <summary>
        /// Color items for ToolTip (BorderColor, BackColor, TitleColor, InfoColor)
        /// </summary>
        public static SkinToolTipSet ToolTip { get { return Instance._ToolTip; } }
        /// <summary>
        /// Color items for ToolBar
        /// </summary>
        public static SkinToolBarSet ToolBar { get { return Instance._ToolBar; } }
        /// <summary>
        /// Color items for button (BackColor, TextColor)
        /// </summary>
        public static SkinButtonSet Button { get { return Instance._Button; } }
        /// <summary>
        /// Color items for scrollbar (BackColorArea, BackColorButton, TextColorButton)
        /// </summary>
        public static SkinScrollBarSet ScrollBar { get { return Instance._ScrollBar; } }
        /// <summary>
        /// Color items for progress (BackColorWindow, TextColorWindow, BackColorProgress, DataColorProgress, TextColorProgress)
        /// </summary>
        public static SkinProgressSet Progress { get { return Instance._Progress; } }
        /// <summary>
        /// Color items for Axis (BackColor, TextColorLabelBig, TextColorLabelStandard, LineColorTickBig, LineColorTickStandard)
        /// </summary>
        public static SkinAxisSet Axis { get { return Instance._Axis; } }
        /// <summary>
        /// Color items for Grid (BackColor, TextColorLabelBig, TextColorLabelStandard, LineColorTickBig, LineColorTickStandard, ...)
        /// </summary>
        public static SkinGridSet Grid { get { return Instance._Grid; } }
        /// <summary>
        /// All items, for configuration
        /// </summary>
        public static IEnumerable<KeyValuePair<string, object>> AllSkinItems { get { return Instance._ValueDict.ToArray(); } }
        #endregion
        #region Create Skin and SkinSets
        private Skin()
        {
            this._ValueDict = new Dictionary<string, object>();

            var methods = this.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var method in methods)
            {
                var initialisers = method.GetCustomAttributes(typeof(InitialiserAttribute), false);
                if (initialisers.Length > 0)
                    method.Invoke(this, null);
            }
        }
        [Initialiser]
        private void _StandardSkinInit()
        {
            this._Modifiers = new SkinModifierSet(this, "Modifiers");
            this._Shadow = new SkinShadowSet(this, "Shadow");
            this._Control = new SkinControlSet(this, "Control");
            this._ToolTip = new SkinToolTipSet(this, "ToolTip");
            this._ToolBar = new SkinToolBarSet(this, "ToolBar");
            this._Button = new SkinButtonSet(this, "Button");
            this._ScrollBar = new SkinScrollBarSet(this, "ScrollBar");
            this._Progress = new SkinProgressSet(this, "Progress");
            this._Axis = new SkinAxisSet(this, "Axis");
            this._Grid = new SkinGridSet(this, "Grid");
        }
        /// <summary>
        /// Add a new instance of SkinSet to Skin.
        /// Use pattern for "external" SkinSet:
        /// </summary>
        /// <param name="skinSet"></param>
        public static void Add(SkinSet skinSet)
        {
            skinSet.SetOwner(Instance);
        }
        private SkinModifierSet _Modifiers;
        private SkinShadowSet _Shadow;
        private SkinControlSet _Control;
        private SkinToolTipSet _ToolTip;
        private SkinToolBarSet _ToolBar;
        private SkinButtonSet _Button;
        private SkinScrollBarSet _ScrollBar;
        private SkinProgressSet _Progress;
        private SkinAxisSet _Axis;
        private SkinGridSet _Grid;
        private Dictionary<string, object> _ValueDict;
        #endregion
        #region Get and Set Values
        /// <summary>
        /// Vrátí objekt typu T z úschovny
        /// </summary>
        /// <param name="skinSetKey"></param>
        /// <param name="valueKey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        internal T GetValue<T>(string skinSetKey, string valueKey, T defaultValue)
        {
            string key = skinSetKey + "." + valueKey;
            object value;
            if (!this._ValueDict.TryGetValue(key, out value))
            {
                value = defaultValue;
                this._ValueDict.Add(key, value);
            }
            return (value is T ? (T)value : defaultValue);
        }
        /// <summary>
        /// Vloží objekt typu T do úschovny
        /// </summary>
        /// <param name="skinSetKey"></param>
        /// <param name="valueKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal void SetValue<T>(string skinSetKey, string valueKey, T value)
        {
            string key = skinSetKey + "." + valueKey;
            if (!this._ValueDict.ContainsKey(key))
                this._ValueDict.Add(key, value);
            else
                this._ValueDict[key] = value;
        }
        #endregion
        #region Get modified color by InteraciveState
        #endregion
        #region Brush and Pen
        /// <summary>
        /// Returns re-usable SolidBrush.
        /// Do not dispose it !!!
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static SolidBrush Brush(Color color)
        {
            return Instance._GetBrush(color, null);
        }
        /// <summary>
        /// Returns re-usable SolidBrush.
        /// Do not dispose it !!!
        /// </summary>
        /// <param name="color"></param>
        /// <param name="opacity"></param>
        /// <returns></returns>
        public static SolidBrush Brush(Color color, Int32? opacity)
        {
            return Instance._GetBrush(color, opacity);
        }
        /// <summary>
        /// Returns re-usable Pen.
        /// Do not dispose it !!!
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen Pen(Color color)
        {
            return Instance._GetPen(color, null, null, null);
        }
        /// <summary>
        /// Returns re-usable Pen.
        /// Do not dispose it !!!
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen Pen(Color color, int? opacity)
        {
            return Instance._GetPen(color, opacity, null, null);
        }
        /// <summary>
        /// Returns re-usable Pen.
        /// Do not dispose it !!!
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen Pen(Color color, float width)
        {
            return Instance._GetPen(color, null, width, null);
        }
        /// <summary>
        /// Returns re-usable Pen.
        /// Do not dispose it !!!
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen Pen(Color color, DashStyle dashStyle)
        {
            return Instance._GetPen(color, null, null, dashStyle);
        }
        /// <summary>
        /// Returns re-usable Pen.
        /// Do not dispose it !!!
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen Pen(Color color, float width, DashStyle dashStyle)
        {
            return Instance._GetPen(color, null, width, dashStyle);
        }
        /// <summary>
        /// Returns re-usable Pen.
        /// Do not dispose it !!!
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen Pen(Color color, int? opacity, float width, DashStyle dashStyle)
        {
            return Instance._GetPen(color, opacity, width, dashStyle);
        }
        private SolidBrush _GetBrush(Color color, Int32? opacity)
        {
            if (opacity.HasValue)
                color = color.SetOpacity(opacity);

            if (this.__Brush == null)
                this.__Brush = new SolidBrush(color);
            else
                this.__Brush.Color = color;
            return this.__Brush;
        }
        private Pen _GetPen(Color color, Int32? opacity, float? width, DashStyle? dashStyle)
        {
            if (opacity.HasValue)
                color = color.SetOpacity(opacity);

            if (this.__Pen == null)
                this.__Pen = new Pen(color);
            else
                this.__Pen.Color = color;

            this.__Pen.Width = ((width.HasValue) ? width.Value : 1f);
            this.__Pen.DashStyle = (dashStyle.HasValue ? dashStyle.Value : DashStyle.Solid);

            return this.__Pen;
        }
        private SolidBrush __Brush;
        private Pen __Pen;
        #endregion
        #region BackgroundBrush by interactive state
        /// <summary>
        /// Create and return Brush for Background for specified data.
        /// This Brush must be used in using {} pattern (Disposed at end of using).
        /// </summary>
        /// <param name="bounds">Absolute bounds for brush (LinearGradient, or PathGradientBrush)</param>
        /// <param name="orientation">Orientation for LinearGradient brush</param>
        /// <param name="state">Interactive state</param>
        /// <param name="color">Base color for background</param>
        /// <returns></returns>
        public static Brush CreateBrushForBackground(Rectangle bounds, Orientation orientation, GInteractiveState state, Color color)
        {
            return _CreateBrushForBackground(bounds, orientation, state, true, color, null, null);
        }
        /// <summary>
        /// Create and return Brush for Background for specified data.
        /// This Brush must be used in using {} pattern (Disposed at end of using).
        /// </summary>
        /// <param name="bounds">Absolute bounds for brush (LinearGradient, or PathGradientBrush)</param>
        /// <param name="orientation">Orientation for LinearGradient brush</param>
        /// <param name="state">Interactive state</param>
        /// <param name="modifyStateNone">Create Linear gradient even for state = None</param>
        /// <param name="color">Base color for background</param>
        /// <returns></returns>
        public static Brush CreateBrushForBackground(Rectangle bounds, Orientation orientation, GInteractiveState state, bool modifyStateNone, Color color)
        {
            return _CreateBrushForBackground(bounds, orientation, state, modifyStateNone, color, null, null);
        }
        /// <summary>
        /// Create and return Brush for Background for specified data.
        /// This Brush must be used in using {} pattern (Disposed at end of using).
        /// </summary>
        /// <param name="bounds">Absolute bounds for brush (LinearGradient, or PathGradientBrush)</param>
        /// <param name="orientation">Orientation for LinearGradient brush</param>
        /// <param name="state">Interactive state</param>
        /// <param name="modifyStateNone">Create Linear gradient even for state = None</param>
        /// <param name="color">Base color for background</param>
        /// <param name="opacity">Explicit opacity for color</param>
        /// <returns></returns>
        public static Brush CreateBrushForBackground(Rectangle bounds, Orientation orientation, GInteractiveState state, bool modifyStateNone, Color color, int? opacity)
        {
            return _CreateBrushForBackground(bounds, orientation, state, modifyStateNone, color, opacity, null);
        }
        /// <summary>
        /// Create and return Brush for Background for specified data.
        /// This Brush must be used in using {} pattern (Disposed at end of using).
        /// </summary>
        /// <param name="bounds">Absolute bounds for brush (LinearGradient, or PathGradientBrush)</param>
        /// <param name="orientation">Orientation for LinearGradient brush</param>
        /// <param name="state">Interactive state</param>
        /// <param name="modifyStateNone">Create Linear gradient even for state = None</param>
        /// <param name="color">Base color for background</param>
        /// <param name="opacity">Explicit opacity for color</param>
        /// <param name="relativePoint">Relative point (relative to bounds) of center for PathGradientBrush.</param>
        /// <returns></returns>
        public static Brush CreateBrushForBackground(Rectangle bounds, Orientation orientation, GInteractiveState state, bool modifyStateNone, Color color, int? opacity, Point? relativePoint)
        {
            return _CreateBrushForBackground(bounds, orientation, state, modifyStateNone, color, opacity, relativePoint);
        }
        /// <summary>
        /// Create and return Brush for Background for specified data.
        /// This Brush must be used in using {} pattern (Disposed at end of using).
        /// </summary>
        /// <param name="bounds">Absolute bounds for brush (LinearGradient, or PathGradientBrush)</param>
        /// <param name="orientation">Orientation for LinearGradient brush</param>
        /// <param name="state">Interactive state</param>
        /// <param name="modifyStateNone">Create Linear gradient even for state = None</param>
        /// <param name="color">Base color for background</param>
        /// <param name="opacity">Explicit opacity for color</param>
        /// <param name="relativePoint">Relative point (relative to bounds) of center for PathGradientBrush.</param>
        /// <returns></returns>
        private static Brush _CreateBrushForBackground(Rectangle bounds, Orientation orientation, GInteractiveState state, bool modifyStateNone, Color color, int? opacity, Point? relativePoint)
        {
            Color color1, color2;
            bool asGradient;
            _ModifyBackColorByState(color, state, modifyStateNone, opacity, out color1, out color2, out asGradient);
            if (!asGradient) return new SolidBrush(color1);          // Must be "new" instance, because is used in "using" pattern. If is not "asGradient", then is returned new LinearGradientBrush.

            if (relativePoint.HasValue && (state == GInteractiveState.MouseOver || state == GInteractiveState.LeftDown || state == GInteractiveState.LeftDrag || state == GInteractiveState.RightDown || state == GInteractiveState.RightDrag))
                return _CreateBrushForBackgroundPoint(bounds, orientation, color1, color2, relativePoint.Value);
            else
                return _CreateBrushForBackgroundGradient(bounds, orientation, color1, color2);
        }
        /// <summary>
        /// Create and return LinearGradientBrush for Background 
        /// This Brush must be used in using {} pattern (Disposed at end of using).
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="orientation"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <returns></returns>
        private static Brush _CreateBrushForBackgroundGradient(Rectangle bounds, Orientation orientation, Color color1, Color color2)
        {
            float angle = (orientation == Orientation.Horizontal ? 90f : 0f);
            return new LinearGradientBrush(bounds, color1, color2, angle);
        }
        /// <summary>
        /// Create and return PathGradientBrush for Background and relative center point
        /// This Brush must be used in using {} pattern (Disposed at end of using).
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="orientation"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <param name="relativePoint"></param>
        /// <returns></returns>
        private static System.Drawing.Brush _CreateBrushForBackgroundPoint(Rectangle bounds, Orientation orientation, Color color1, Color color2, Point relativePoint)
        {
            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(GPainter.CreateEllipseAroundRectangle(bounds, 4));
            PathGradientBrush gb = new PathGradientBrush(gp);
            gb.CenterColor = color1;
            gb.CenterPoint = bounds.Location.Add(relativePoint);
            gb.SurroundColors = new Color[] { color2 };
            return gb;
        }
        #endregion
        #region ModifyColorByState
        /// <summary>
        /// Returns specified color (as Back color) modified by enabled and mouse state, using Skin.Modifiers
        /// </summary>
        /// <param name="color"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static Color GetBackColor(Color color, GInteractiveState state)
        {
            Color color1, color2;
            return ModifyBackColorByState(color, state, false, out color1, out color2);
        }
        /// <summary>
        /// Modify background colors by current interactive state.
        /// </summary>
        /// <param name="color">Base color</param>
        /// <param name="state">Interactive state</param>
        /// <param name="color1">Result Begin color for LinearGradient</param>
        /// <param name="color2">Result End color for LinearGradient</param>
        public static Color ModifyBackColorByState(Color color, GInteractiveState state, out Color color1, out Color color2)
        {
            bool asGradient;
            _ModifyBackColorByState(color, state, true, null, out color1, out color2, out asGradient);
            return color1.Morph(color2, 0.5f);
        }
        /// <summary>
        /// Modify background colors by current interactive state.
        /// </summary>
        /// <param name="color">Base color</param>
        /// <param name="state">Interactive state</param>
        /// <param name="modifyStateNone">true for modify when Interactive state == None</param>
        /// <param name="color1">Result Begin color for LinearGradient</param>
        /// <param name="color2">Result End color for LinearGradient</param>
        public static Color ModifyBackColorByState(Color color, GInteractiveState state, bool modifyStateNone, out Color color1, out Color color2)
        {
            bool asGradient;
            _ModifyBackColorByState(color, state, modifyStateNone, null, out color1, out color2, out asGradient);
            return color1.Morph(color2, 0.5f);
        }
        /// <summary>
        /// Modify background colors by current interactive state.
        /// </summary>
        /// <param name="color">Base color</param>
        /// <param name="state">Interactive state</param>
        /// <param name="modifyStateNone">true for modify when Interactive state == None</param>
        /// <param name="opacity">Change opacity to this value</param>
        /// <param name="color1">Result Begin color for LinearGradient</param>
        /// <param name="color2">Result End color for LinearGradient</param>
        /// <param name="asGradient">true when result is LinearGradient, false for SolidBrush (then color1 == color2)</param>
        public static Color ModifyBackColorByState(Color color, GInteractiveState state, bool modifyStateNone, Int32? opacity, out Color color1, out Color color2, out bool asGradient)
        {
            _ModifyBackColorByState(color, state, modifyStateNone, opacity, out color1, out color2, out asGradient);
            return color1.Morph(color2, 0.5f);
        }
        /// <summary>
        /// Returns specified color (as Fore color) modified by enabled and mouse state, using Skin.Modifiers
        /// </summary>
        /// <param name="color"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static Color GetForeColor(Color color, GInteractiveState state)
        {
            return ModifyForeColorByState(color, state);
        }
        /// <summary>
        /// Modify foreground colors by current interactive state.
        /// </summary>
        /// <param name="color">Base color</param>
        /// <param name="state">Interactive state</param>
        public static Color ModifyForeColorByState(Color color, GInteractiveState state)
        {
            Color colorF;
            _ModifyForeColorByState(color, state, null, out colorF);
            return colorF;
        }
        /// <summary>
        /// Modify foreground colors by current interactive state.
        /// </summary>
        /// <param name="color">Base color</param>
        /// <param name="state">Interactive state</param>
        /// <param name="opacity">Change opacity to this value</param>
        public static Color ModifyForeColorByState(Color color, GInteractiveState state, Int32? opacity)
        {
            Color colorF;
            _ModifyForeColorByState(color, state, opacity, out colorF);
            return colorF;
        }
        /// <summary>
        /// Modify foreground colors by current interactive state.
        /// </summary>
        /// <param name="color">Base color</param>
        /// <param name="state">Interactive state</param>
        /// <param name="opacity">Change opacity to this value</param>
        /// <param name="colorF">Result color</param>
        private static void _ModifyForeColorByState(Color color, GInteractiveState state, Int32? opacity, out Color colorF)
        {
            colorF = color;

            SkinModifierSet modifiers = Skin.Modifiers;
            switch (state)
            {
                case GInteractiveState.None:
                case GInteractiveState.Enabled:
                    colorF = color;
                    break;
                case GInteractiveState.Disabled:
                    colorF = color.Morph(modifiers.TextColorDisable);
                    break;
                case GInteractiveState.MouseOver:
                    colorF = color.Morph(modifiers.TextColorHot);
                    break;
                case GInteractiveState.LeftDown:
                case GInteractiveState.RightDown:
                    colorF = color.Morph(modifiers.TextColorDown);
                    break;
                case GInteractiveState.LeftDrag:
                case GInteractiveState.RightDrag:
                    colorF = color.Morph(modifiers.TextColorDrag);
                    break;
            }

            if (opacity.HasValue)
                _ModifyColorsByOpacity(opacity, ref colorF);
        }
        /// <summary>
        /// Modify background colors by current interactive state.
        /// </summary>
        /// <param name="color">Base color</param>
        /// <param name="state">Interactive state</param>
        /// <param name="modifyStateNone">true for modify when Interactive state == None</param>
        /// <param name="opacity">Change opacity to this value</param>
        /// <param name="color1">Result Begin color for LinearGradient</param>
        /// <param name="color2">Result End color for LinearGradient</param>
        /// <param name="asGradient">true when result is LinearGradient, false for SolidBrush (then color1 == color2)</param>
        private static void _ModifyBackColorByState(Color color, GInteractiveState state, bool modifyStateNone, Int32? opacity, out Color color1, out Color color2, out bool asGradient)
        {
            color1 = color;
            color2 = color;
            asGradient = false;

            SkinModifierSet modifiers = Skin.Modifiers;
            switch (state)
            {
                case GInteractiveState.None:
                case GInteractiveState.Enabled:
                    color1 = color;
                    color2 = color;
                    if (modifyStateNone)
                    {
                        color1 = color.Morph(modifiers.BackColorBegin);
                        color2 = color.Morph(modifiers.BackColorEnd);
                        asGradient = true;
                    }
                    break;
                case GInteractiveState.Disabled:
                    color1 = color.Morph(modifiers.BackColorDisable);
                    color2 = color1;
                    asGradient = false;
                    break;
                case GInteractiveState.MouseOver:
                    color1 = color.Morph(modifiers.BackColorHotBegin);
                    color2 = color.Morph(modifiers.BackColorHotEnd);
                    asGradient = true;
                    break;
                case GInteractiveState.LeftDown:
                case GInteractiveState.RightDown:
                    color1 = color.Morph(modifiers.BackColorDownBegin);
                    color2 = color.Morph(modifiers.BackColorDownEnd);
                    asGradient = true;
                    break;
                case GInteractiveState.LeftDrag:
                case GInteractiveState.RightDrag:
                    color1 = color.Morph(modifiers.BackColorDragBegin);
                    color2 = color.Morph(modifiers.BackColorDragEnd);
                    asGradient = true;
                    break;
            }

            if (opacity.HasValue)
                _ModifyColorsByOpacity(opacity, ref color1, ref color2);
        }
        /// <summary>
        /// Modify colors by specified opacity
        /// </summary>
        /// <param name="opacity"></param>
        /// <param name="color1"></param>
        private static void _ModifyColorsByOpacity(int? opacity, ref Color color1)
        {
            if (opacity.HasValue)
            {
                color1 = Color.FromArgb(opacity.Value, color1);
            }
        }
        /// <summary>
        /// Modify colors by specified opacity
        /// </summary>
        /// <param name="opacity"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        private static void _ModifyColorsByOpacity(int? opacity, ref Color color1, ref Color color2)
        {
            if (opacity.HasValue)
            {
                color1 = Color.FromArgb(opacity.Value, color1);
                color2 = Color.FromArgb(opacity.Value, color2);
            }
        }
        /// <summary>
        /// Modify colors by specified opacity
        /// </summary>
        /// <param name="opacity"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <param name="color3"></param>
        private static void _ModifyColorsByOpacity(int? opacity, ref Color color1, ref Color color2, ref Color color3)
        {
            if (opacity.HasValue)
            {
                color1 = Color.FromArgb(opacity.Value, color1);
                color2 = Color.FromArgb(opacity.Value, color2);
                color3 = Color.FromArgb(opacity.Value, color3);
            }
        }
        #endregion
        #region Singleton
        private static Skin Instance
        {
            get
            {
                if (__Instance == null)
                {
                    lock (__Lock)
                    {
                        if (__Instance == null)
                        {
                            __Instance = new Skin();
                        }
                    }
                }
                return __Instance;
            }
        }
        private static Skin __Instance;
        private static object __Lock = new object();
        #endregion
    }
    /// <summary>
    /// Skin set for Modifiers: by Mouse state (Hot, Down) and for Disable state.
    /// Has colors: BackColor; TextColor; TextColorDisable;
    /// </summary>
    public class SkinModifierSet : SkinSet
    {
        #region Internal and private
        internal SkinModifierSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Color MouseMoveTracking { get { return this._Owner.GetValue(this._SkinSetKey, "MouseMoveTracking", DefaultMouseMoveTracking); } set { this._Owner.SetValue(this._SkinSetKey, "MouseMoveTracking", value); } }
        public Color MouseDragTracking { get { return this._Owner.GetValue(this._SkinSetKey, "MouseDragTracking", DefaultMouseDragTracking); } set { this._Owner.SetValue(this._SkinSetKey, "MouseDragTracking", value); } }
        public Color BackColorBegin { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorBegin", DefaultBackColorBegin); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorBegin", value); } }
        public Color BackColorEnd { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorEnd", DefaultBackColorEnd); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorEnd", value); } }
        public Color BackColorHotBegin { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorHotBegin", DefaultBackColorHotBegin); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorHotBegin", value); } }
        public Color BackColorHotEnd { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorHotEnd", DefaultBackColorHotEnd); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorHotEnd", value); } }
        public Color BackColorDownBegin { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorDownBegin", DefaultBackColorDownBegin); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorDownBegin", value); } }
        public Color BackColorDownEnd { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorDownEnd", DefaultBackColorDownEnd); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorDownEnd", value); } }
        public Color BackColorDragBegin { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorDragBegin", DefaultBackColorDragBegin); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorDragBegin", value); } }
        public Color BackColorDragEnd { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorDragEnd", DefaultBackColorDragEnd); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorDragEnd", value); } }
        public Color BackColorDisable { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorDisable", DefaultBackColorDisable); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorDisable", value); } }
        public Color BackColorShadow { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorShadow", DefaultBackColorShadow); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorShadow", value); } }
        public Color TextColorHot { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorHot", DefaultTextColorHot); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorHot", value); } }
        public Color TextColorDown { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorDown", DefaultTextColorDown); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorDown", value); } }
        public Color TextColorDrag { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorDrag", DefaultTextColorDrag); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorDrag", value); } }
        public Color TextColorDisable { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorDisable", DefaultTextColorDisable); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorDisable", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultMouseMoveTracking { get { return Color.FromArgb(96, Color.LightYellow); } }
        protected virtual Color DefaultMouseDragTracking { get { return Color.FromArgb(64, Color.Violet); } }
        // Modifier colors: Alpha value (0-255) represents Morphing value (0-1) !!!
        protected virtual Color DefaultBackColorBegin { get { return Color.FromArgb(38, Color.White); } }
        protected virtual Color DefaultBackColorEnd { get { return Color.FromArgb(38, Color.Black); } }
        protected virtual Color DefaultBackColorHotBegin { get { return Color.FromArgb(64, Color.White); } }
        protected virtual Color DefaultBackColorHotEnd { get { return Color.FromArgb(64, Color.Black); } }
        protected virtual Color DefaultBackColorDownBegin { get { return Color.FromArgb(64, Color.Black); } }
        protected virtual Color DefaultBackColorDownEnd { get { return Color.FromArgb(48, Color.White); } }
        protected virtual Color DefaultBackColorDragBegin { get { return Color.FromArgb(38, Color.Black); } }
        protected virtual Color DefaultBackColorDragEnd { get { return Color.FromArgb(64, Color.White); } }
        protected virtual Color DefaultBackColorDisable { get { return Color.FromArgb(96, Color.LightGray); } }
        protected virtual Color DefaultBackColorShadow { get { return Color.FromArgb(96, Color.DimGray); } }
        protected virtual Color DefaultTextColorHot { get { return Color.FromArgb(64, Color.DarkViolet); } }
        protected virtual Color DefaultTextColorDown { get { return Color.FromArgb(96, Color.DarkViolet); } }
        protected virtual Color DefaultTextColorDrag { get { return Color.FromArgb(96, Color.DarkViolet); } }
        protected virtual Color DefaultTextColorDisable { get { return Color.FromArgb(64, Color.DimGray); } }
        #endregion
    }
    /// <summary>
    /// Skin set for Shadow.
    /// Has colors: BackColor; TextColor;
    /// </summary>
    public class SkinShadowSet : SkinSet
    {
        #region Internal and private
        internal SkinShadowSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Color OuterColor { get { return this._Owner.GetValue(this._SkinSetKey, "OuterColor", DefaultOuterColor); } set { this._Owner.SetValue(this._SkinSetKey, "OuterColor", value); } }
        public Color InnerColor { get { return this._Owner.GetValue(this._SkinSetKey, "InnerColor", DefaultInnerColor); } set { this._Owner.SetValue(this._SkinSetKey, "InnerColor", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultOuterColor { get { return Color.FromArgb(8, 192, 192, 192); } }
        protected virtual Color DefaultInnerColor { get { return Color.FromArgb(192, 32, 32, 32); } }
        #endregion
    }
    /// <summary>
    /// Skin set for Controls.
    /// Has colors: BackColor; TextColor;
    /// </summary>
    public class SkinControlSet : SkinSet
    {
        #region Internal and private
        internal SkinControlSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        public Color TextColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextColor", DefaultTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextColor", value); } }
        public Color Effect3DDark { get { return this._Owner.GetValue(this._SkinSetKey, "Effect3DDark", DefaultEffect3DDark); } set { this._Owner.SetValue(this._SkinSetKey, "Effect3DDark", value); } }
        public Color Effect3DLight { get { return this._Owner.GetValue(this._SkinSetKey, "Effect3DLight", DefaultEffect3DLight); } set { this._Owner.SetValue(this._SkinSetKey, "Effect3DLight", value); } }
        public float Effect3DRatio { get { return this._Owner.GetValue(this._SkinSetKey, "Effect3DRatio", DefaultEffect3DRatio); } set { this._Owner.SetValue(this._SkinSetKey, "Effect3DRatio", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultBackColor { get { return Color.LightGray; } }
        protected virtual Color DefaultTextColor { get { return Color.Black; } }
        protected virtual Color DefaultEffect3DDark { get { return Color.DarkGray; } }
        protected virtual Color DefaultEffect3DLight { get { return Color.White; } }
        protected virtual float DefaultEffect3DRatio { get { return 0.25f; } }
        #endregion
    }
    /// <summary>
    /// Skin set for ToolTip.
    /// Has colors: BorderColor, BackColor, TitleColor, InfoColor
    /// </summary>
    public class SkinToolTipSet : SkinSet
    {
        #region Internal and private
        internal SkinToolTipSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Color BorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "BorderColor", DefaultBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "BorderColor", value); } }
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        public Color TitleColor { get { return this._Owner.GetValue(this._SkinSetKey, "TitleColor", DefaultTitleColor); } set { this._Owner.SetValue(this._SkinSetKey, "TitleColor", value); } }
        public Color InfoColor { get { return this._Owner.GetValue(this._SkinSetKey, "InfoColor", DefaultInfoColor); } set { this._Owner.SetValue(this._SkinSetKey, "InfoColor", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultBorderColor { get { return Color.DimGray; } }
        protected virtual Color DefaultBackColor { get { return Color.LightYellow; } }
        protected virtual Color DefaultTitleColor { get { return Color.Black; } }
        protected virtual Color DefaultInfoColor { get { return Color.Black; } }
        #endregion
    }
    /// <summary>
    /// Skin set for ToolBar.
    /// Has colors: BorderColor, BackColor, TextColor
    /// </summary>
    public class SkinToolBarSet : SkinSet
    {
        #region Internal and private
        internal SkinToolBarSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Color BorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "BorderColor", DefaultBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "BorderColor", value); } }
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        public Color TextColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextColor", DefaultTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextColor", value); } }
        public Color ItemBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemBackColor", DefaultItemBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemBackColor", value); } }
        public Color ItemBorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemBorderColor", DefaultItemBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemBorderColor", value); } }
        public Color TitleBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "TitleBackColor", DefaultTitleBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "TitleBackColor", value); } }
        public Color SeparatorLightColor { get { return this._Owner.GetValue(this._SkinSetKey, "SeparatorLightColor", DefaultSeparatorLightColor); } set { this._Owner.SetValue(this._SkinSetKey, "SeparatorLightColor", value); } }
        public Color SeparatorDarkColor { get { return this._Owner.GetValue(this._SkinSetKey, "SeparatorDarkColor", DefaultSeparatorDarkColor); } set { this._Owner.SetValue(this._SkinSetKey, "SeparatorDarkColor", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultBorderColor { get { return Color.DimGray; } }
        protected virtual Color DefaultBackColor { get { return Color.FromArgb(255, 240, 240, 240); } }
        protected virtual Color DefaultTextColor { get { return Color.Black; } }
        protected virtual Color DefaultItemBackColor { get { return Color.FromArgb(255, 224, 224, 240); } }
        protected virtual Color DefaultItemBorderColor { get { return Color.DimGray; } }
        protected virtual Color DefaultTitleBackColor { get { return Color.FromArgb(128, 240, 224, 246); } }
        protected virtual Color DefaultSeparatorLightColor { get { return Color.LightGray; } }
        protected virtual Color DefaultSeparatorDarkColor { get { return Color.DimGray; } }
        #endregion
    }
    /// <summary>
    /// Skin set for Buttons.
    /// Has colors: BackColor; TextColor;
    /// </summary>
    public class SkinButtonSet : SkinSet
    {
        #region Internal and private
        internal SkinButtonSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Color BorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "BorderColor", DefaultBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "BorderColor", value); } }
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        public Color TextColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextColor", DefaultTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextColor", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultBorderColor { get { return Color.DimGray; } }
        protected virtual Color DefaultBackColor { get { return Color.FromArgb(255, 216, 216, 216); } }
        protected virtual Color DefaultTextColor { get { return Color.Black; } }
        #endregion
    }
    /// <summary>
    /// Skin set for Scrollbar.
    /// Has colors: BackColorArea; BackColorButton; TextColorButton;
    /// </summary>
    public class SkinScrollBarSet : SkinSet
    {
        #region Internal and private
        internal SkinScrollBarSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Color BackColorArea { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorArea", DefaultBackColorArea); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorArea", value); } }
        public Color BackColorButton { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorButton", DefaultBackColorButton); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorButton", value); } }
        public Color TextColorButton { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorButton", DefaultTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorButton", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultBackColorArea { get { return Color.FromArgb(255, 160, 160, 160); } }
        protected virtual Color DefaultBackColorButton { get { return Color.FromArgb(255, 216, 216, 216); } }
        protected virtual Color DefaultTextColor { get { return Color.Black; } }
        #endregion
    }
    /// <summary>
    /// Skin set for Progress window.
    /// Has colors: BackColorWindow, TextColorWindow, BackColorProgress, DataColorProgress, TextColorProgress
    /// </summary>
    public class SkinProgressSet : SkinSet
    {
        #region Internal and private
        internal SkinProgressSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Color BackColorWindow { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorWindow", DefaultBackColorWindow); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorWindow", value); } }
        public Color TextColorWindow { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorWindow", DefaultTextColorWindow); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorWindow", value); } }
        public Color BackColorProgress { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorProgress", DefaultBackColorProgress); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorProgress", value); } }
        public Color DataColorProgress { get { return this._Owner.GetValue(this._SkinSetKey, "DataColorProgress", DefaultDataColorProgress); } set { this._Owner.SetValue(this._SkinSetKey, "DataColorProgress", value); } }
        public Color TextColorProgress { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorProgress", DefaultTextColorProgress); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorProgress", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultBackColorWindow { get { return Color.FromArgb(255, 64, 128, 128); } }
        protected virtual Color DefaultTextColorWindow { get { return Color.FromArgb(255, 255, 255, 255); } }
        protected virtual Color DefaultBackColorProgress { get { return Color.FromArgb(255, 240, 240, 240); } }
        protected virtual Color DefaultDataColorProgress { get { return Color.FromArgb(255, 160, 255, 160); } }
        protected virtual Color DefaultTextColorProgress { get { return Color.Black; } }
        #endregion
    }
    /// <summary>
    /// Skin set for Axis control.
    /// Has colors: BackColor, TextColorLabelBig, TextColorLabelStandard, LineColorTickBig, LineColorTickStandard
    /// </summary>
    public class SkinAxisSet : SkinSet
    {
        #region Internal and private
        internal SkinAxisSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        public Color TextColorLabelBig { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorLabelBig", DefaultTextColorLabelBig); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorLabelBig", value); } }
        public Color TextColorLabelStandard { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorLabelStandard", DefaultTextColorLabelStandard); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorLabelStandard", value); } }
        public Color LineColorTickBig { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorTickBig", DefaultLineColorTickBig); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorTickBig", value); } }
        public Color LineColorTickStandard { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorTickStandard", DefaultLineColorTickStandard); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorTickStandard", value); } }
        public Color TextColorArrangement { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorArrangement", DefaultTextColorArrangement); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorArrangement", value); } }
        
        #endregion
        #region Default colors
        protected virtual Color DefaultBackColor { get { return Color.FromArgb(192, Color.LightSkyBlue); } }
        protected virtual Color DefaultTextColorLabelBig { get { return Color.Black; } }
        protected virtual Color DefaultTextColorLabelStandard { get { return Color.DimGray; } }
        protected virtual Color DefaultLineColorTickBig { get { return Color.Black; } }
        protected virtual Color DefaultLineColorTickStandard { get { return Color.Gray; } }
        protected virtual Color DefaultTextColorArrangement { get { return Color.FromArgb(128, Color.Black); } }
        #endregion
    }
    /// <summary>
    /// Skin set for Grid control.
    /// Has colors: BackColor, TextColorLabelBig, TextColorLabelStandard, LineColorTickBig, LineColorTickStandard
    /// </summary>
    public class SkinGridSet : SkinSet
    {
        #region Internal and private
        internal SkinGridSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Color HeaderBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderBackColor", DefaultHeaderBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderBackColor", value); } }
        public Color HeaderTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderTextColor", DefaultHeaderTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderTextColor", value); } }
        public Color HeaderLineLeftVerticalColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderLineLeftVerticalColor", DefaultHeaderLineLeftVerticalColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderLineLeftVerticalColor", value); } }
        public Color HeaderLineRightVerticalColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderLineRightVerticalColor", DefaultHeaderLineRightVerticalColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderLineRightVerticalColor", value); } }
        public Color HeaderLineHorizontalColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderLineHorizontalColor", DefaultHeaderLineHorizontalColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderLineHorizontalColor", value); } }
        public Color TableBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "TableBackColor", DefaultTableBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "TableBackColor", value); } }
        public Color RowBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "RowBackColor", DefaultRowBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "RowBackColor", value); } }
        public Color RowTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "RowTextColor", DefaultRowTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "RowTextColor", value); } }
        public Color SelectedRowBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "SelectedRowBackColor", DefaultSelectedRowBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "SelectedRowBackColor", value); } }
        public Color SelectedRowTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "SelectedRowTextColor", DefaultSelectedRowTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "SelectedRowTextColor", value); } }
        public Color ActiveCellBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ActiveCellBackColor;", DefaultActiveCellBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ActiveCellBackColor;", value); } }
        public Color ActiveCellTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "ActiveCellTextColor ", DefaultActiveCellTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "ActiveCellTextColor ", value); } }
        public Color BorderLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "BorderLineColor", DefaultBorderLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "BorderLineColor", value); } }
        public BorderLinesType BorderLineType { get { return this._Owner.GetValue(this._SkinSetKey, "BorderLineType", DefaultBorderLineType); } set { this._Owner.SetValue(this._SkinSetKey, "BorderLineType", value); } }
        public Image SortAscendingImage { get { return this._Owner.GetValue(this._SkinSetKey, "SortAscendingImage", DefaultSortAscendingImage); } set { this._Owner.SetValue(this._SkinSetKey, "SortAscendingImage", value); } }
        public Image SortDescendingImage { get { return this._Owner.GetValue(this._SkinSetKey, "SortDescendingImage", DefaultSortDescendingImage); } set { this._Owner.SetValue(this._SkinSetKey, "SortDescendingImage", value); } }
        public Image RowSelectedImage { get { return this._Owner.GetValue(this._SkinSetKey, "RowSelectedImage", DefaultRowSelectedImage); } set { this._Owner.SetValue(this._SkinSetKey, "RowSelectedImage", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultHeaderBackColor { get { return Color.LightSkyBlue; } }
        protected virtual Color DefaultHeaderTextColor { get { return Color.Black; } }
        protected virtual Color DefaultHeaderLineLeftVerticalColor { get { return Color.Gray; } }
        protected virtual Color DefaultHeaderLineRightVerticalColor { get { return Color.LightGray; } }
        protected virtual Color DefaultHeaderLineHorizontalColor { get { return Color.LightGray; } }
        protected virtual Color DefaultTableBackColor { get { return Color.DimGray; } }
        protected virtual Color DefaultRowBackColor { get { return Color.White; } }
        protected virtual Color DefaultRowTextColor { get { return Color.Black; } }
        protected virtual Color DefaultSelectedRowBackColor { get { return Color.White.Morph(Color.LightBlue, 0.65f); } }
        protected virtual Color DefaultSelectedRowTextColor { get { return Color.Black; } }
        protected virtual Color DefaultActiveCellBackColor { get { return Color.White.Morph(Color.CadetBlue, 0.95f); } }
        protected virtual Color DefaultActiveCellTextColor { get { return Color.White; } }
        protected virtual Color DefaultBorderLineColor { get { return Color.Gray; } }
        protected virtual BorderLinesType DefaultBorderLineType { get { return BorderLinesType.Horizontal3DSunken | BorderLinesType.VerticalSolid; } }
        protected virtual Image DefaultSortAscendingImage { get { return IconStandard.SortAsc; } }
        protected virtual Image DefaultSortDescendingImage { get { return IconStandard.SortDesc; } }
        protected virtual Image DefaultRowSelectedImage { get { return IconStandard.RowSelected; } }
        #endregion
    }
    /// <summary>
    /// Skin set abstract base.
    /// </summary>
    public abstract class SkinSet
    {
        #region Internal and protected
        public SkinSet(string skinSetKey)
        {
            this._SkinSetKey = skinSetKey;
        }
        public SkinSet(Skin owner, string skinSetKey)
        {
            this._Owner = owner;
            this._SkinSetKey = skinSetKey;
            this._FillPalette();
        }
        public void SetOwner(Skin owner)
        {
            this._Owner = owner;
            this._FillPalette();
        }
        /// <summary>
        /// Evaluate all public instance properties from this type and its base classes, to fill all items into Skin.Palette dictionary.
        /// </summary>
        protected void _FillPalette()
        {
            var properties = this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
            foreach (var property in properties)
            {
                var getMethod = property.GetGetMethod();
                if (getMethod != null)
                {
                    try { var value = property.GetValue(this, null); }
                    catch (Exception) { }
                }
            }
        }
        protected Skin _Owner;
        protected string _SkinSetKey;
        #endregion
    }
    #region class InitialiserAttribute
    /// <summary>
    /// Attribut Initialiser označuje metodu, která se má provést v rámci inicializace this objektu.
    /// Je vhodno používat v partial třídách.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class InitialiserAttribute : Attribute
    {
        /// <summary>
        /// Attribut Initialiser označuje metodu, která se má provést v rámci inicializace this objektu.
        /// Je vhodno používat v partial třídách.
        /// </summary>
        public InitialiserAttribute()
        {

        }
    }
    #endregion
}
