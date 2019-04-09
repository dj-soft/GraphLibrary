using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using Asol.Tools.WorkScheduler.Data;
using R = Noris.LCS.Base.WorkScheduler.Resources;

namespace Asol.Tools.WorkScheduler.Components
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
        /// Barvy pro Splitter a Resizer
        /// </summary>
        public static SkinSplitterSet Splitter { get { return Instance._Splitter; } }
        /// <summary>
        /// Barvy pro zobrazení Blokovaného GUI
        /// </summary>
        public static SkinBlockedGuiSet BlockedGui { get { return Instance._BlockedGui; } }
        /// <summary>
        /// Color items for ToolTip (BorderColor, BackColor, TitleColor, InfoColor)
        /// </summary>
        public static SkinToolTipSet ToolTip { get { return Instance._ToolTip; } }
        /// <summary>
        /// Color items for ToolBar
        /// </summary>
        public static SkinToolBarSet ToolBar { get { return Instance._ToolBar; } }
        /// <summary>
        /// Color items for TagFilter
        /// </summary>
        public static SkinTagFilterSet TagFilter { get { return Instance._TagFilter; } }
        /// <summary>
        /// Color items for button (BackColor, TextColor)
        /// </summary>
        public static SkinButtonSet Button { get { return Instance._Button; } }
        /// <summary>
        /// Color items for TabHeader (BackColor, TextColor, BackColorActive, TextColorActive)
        /// </summary>
        public static SkinTabHeaderSet TabHeader { get { return Instance._TabHeader; } }
        /// <summary>
        /// Color items for scrollbar (BackColorArea, BackColorButton, TextColorButton)
        /// </summary>
        public static SkinScrollBarSet ScrollBar { get { return Instance._ScrollBar; } }
        /// <summary>
        /// Barvy pro TrackBar
        /// </summary>
        public static SkinTrackBarSet TrackBar { get { return Instance._TrackBar; } }
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
        /// Grafické prvky pro kreslení objektů typu Graph
        /// </summary>
        public static SkinGraphSet Graph { get { return Instance._Graph; } }
        /// <summary>
        /// Grafické prvky pro kreslení objektů typu Relation
        /// </summary>
        public static SkinRelationSet Relation { get { return Instance._Relation; } }
        /// <summary>
        /// All items, for configuration
        /// </summary>
        public static IEnumerable<KeyValuePair<string, object>> AllSkinItems { get { return Instance._ValueDict.ToArray(); } }
        #endregion
        #region Create Skin and SkinSets
        private Skin()
        {
            this._ValueDict = new Dictionary<string, object>();
        }
        /// <summary>
        /// Provede inicializaci instance, volat po konstruktoru
        /// </summary>
        private void _Init()
        {
            // Standardní inicializátor nejdřív:
            this._StandardSkinInit();

            // Další incializátory následně:
            var methods = this.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var method in methods)
            {
                var initialisers = method.GetCustomAttributes(typeof(InitialiserAttribute), false);
                if (initialisers.Length > 0)
                    method.Invoke(this, null);
            }
        }
        /// <summary>
        /// Inicializer
        /// </summary>
        private void _StandardSkinInit()
        {
            // Na pořadí záleží; některé SkinSety mohou využívat hodnot z "nižších" SkinSetů:
            this._Modifiers = new SkinModifierSet(this, "Modifiers");
            this._Control = new SkinControlSet(this, "Control");
            this._Splitter = new SkinSplitterSet(this, "Splitter");
            this._Shadow = new SkinShadowSet(this, "Shadow");
            this._BlockedGui = new SkinBlockedGuiSet(this, "BlockedGui");
            this._ToolTip = new SkinToolTipSet(this, "ToolTip");
            this._ToolBar = new SkinToolBarSet(this, "ToolBar");
            this._Button = new SkinButtonSet(this, "Button");
            this._TagFilter = new SkinTagFilterSet(this, "TagFilter");
            this._TabHeader = new SkinTabHeaderSet(this, "TabHeader");
            this._ScrollBar = new SkinScrollBarSet(this, "ScrollBar");
            this._TrackBar = new SkinTrackBarSet(this, "TrackBar");
            this._Progress = new SkinProgressSet(this, "Progress");
            this._Axis = new SkinAxisSet(this, "Axis");
            this._Grid = new SkinGridSet(this, "Grid");
            this._Graph = new SkinGraphSet(this, "Graph");
            this._Relation = new SkinRelationSet(this, "Relation");
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
        private SkinSplitterSet _Splitter;
        private SkinBlockedGuiSet _BlockedGui;
        private SkinToolTipSet _ToolTip;
        private SkinToolBarSet _ToolBar;
        private SkinTagFilterSet _TagFilter;
        private SkinButtonSet _Button;
        private SkinTabHeaderSet _TabHeader;
        private SkinScrollBarSet _ScrollBar;
        private SkinTrackBarSet _TrackBar;
        private SkinProgressSet _Progress;
        private SkinAxisSet _Axis;
        private SkinGridSet _Grid;
        private SkinGraphSet _Graph;
        private SkinRelationSet _Relation;
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
        /*
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
            return Instance._GetPen(color, null, opacity, null);
        }
        /// <summary>
        /// Returns re-usable Pen.
        /// Do not dispose it !!!
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Pen Pen(Color color, float width)
        {
            return Instance._GetPen(color, width, null, null);
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
            return Instance._GetPen(color, width, null, dashStyle);
        }
        */
        /// <summary>
        /// Vrací znovupoužitelné pero.
        /// Nepoužívejte ho v using { } patternu!!!
        /// </summary>
        /// <param name="color"></param>
        /// <param name="width"></param>
        /// <param name="opacity">Průhlednost v hodnotě 0-255 (nebo null = neměnit)</param>
        /// <param name="opacityRatio">Průhlednost v hodnotě ratio 0.0 ÷1.0 (nebo null = neměnit)</param>
        /// <param name="dashStyle"></param>
        /// <param name="startCap"></param>
        /// <param name="endCap"></param>
        /// <returns></returns>
        public static Pen Pen(Color color, float width = 1f, int? opacity = null, float? opacityRatio = null, DashStyle? dashStyle = null, LineCap? startCap = null, LineCap? endCap = null)
        {
            return Instance._GetPen(color, width, opacity, opacityRatio, dashStyle, startCap, endCap);
        }
        private Pen _GetPen(Color color, float? width, Int32? opacity, float? opacityRatio, DashStyle? dashStyle, LineCap? startCap = null, LineCap? endCap = null)
        {
            if (opacity.HasValue)
                color = color.SetOpacity(opacity);
            else if (opacityRatio.HasValue)
                color = color.SetOpacity(opacityRatio);

            if (this.__Pen == null)
                this.__Pen = new Pen(color);
            else
                this.__Pen.Color = color;

            this.__Pen.Width = ((width.HasValue) ? width.Value : 1f);
            this.__Pen.DashStyle = (dashStyle.HasValue ? dashStyle.Value : DashStyle.Solid);
            this.__Pen.StartCap = (startCap.HasValue ? startCap.Value : LineCap.Flat);
            this.__Pen.EndCap = (endCap.HasValue ? endCap.Value : LineCap.Flat);

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
                return CreateBrushForBackgroundGradient(bounds, orientation, color1, color2);
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
        public static Brush CreateBrushForBackgroundGradient(Rectangle bounds, Orientation orientation, Color color1, Color color2)
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
                case GInteractiveState.LeftFrame:
                case GInteractiveState.RightFrame:
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
            _ModifyBackColorByStateShift(color, state, modifyStateNone, opacity, out color1, out color2, out asGradient);
            // _ModifyBackColorByStateMorph(color, state, modifyStateNone, opacity, out color1, out color2, out asGradient);
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
        private static void _ModifyBackColorByStateShift(Color color, GInteractiveState state, bool modifyStateNone, Int32? opacity, out Color color1, out Color color2, out bool asGradient)
        {
            color1 = color;
            color2 = color;
            asGradient = false;

            SkinModifierSet modifiers = Skin.Modifiers;
            switch (state)
            {
                case GInteractiveState.None:
                case GInteractiveState.Enabled:
                case GInteractiveState.LeftFrame:
                case GInteractiveState.RightFrame:
                    color1 = color;
                    color2 = color;
                    if (modifyStateNone)
                    {
                        color1 = color.Shift(modifiers.ShiftBackColorBegin);
                        color2 = color.Shift(modifiers.ShiftBackColorEnd);
                        asGradient = true;
                    }
                    break;
                case GInteractiveState.Disabled:
                    color1 = color.Morph(modifiers.BackColorDisable);
                    color2 = color1;
                    asGradient = false;
                    break;
                case GInteractiveState.MouseOver:
                    color1 = color.Shift(modifiers.ShiftBackColorHotBegin);
                    color2 = color.Shift(modifiers.ShiftBackColorHotEnd);
                    asGradient = true;
                    break;
                case GInteractiveState.LeftDown:
                case GInteractiveState.RightDown:
                    color1 = color.Shift(modifiers.ShiftBackColorDownBegin);
                    color2 = color.Shift(modifiers.ShiftBackColorDownEnd);
                    asGradient = true;
                    break;
                case GInteractiveState.LeftDrag:
                case GInteractiveState.RightDrag:
                    color1 = color.Shift(modifiers.ShiftBackColorDragBegin);
                    color2 = color.Shift(modifiers.ShiftBackColorDragEnd);
                    asGradient = true;
                    break;
            }

            if (opacity.HasValue)
                _ModifyColorsByOpacity(opacity, ref color1, ref color2);
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
        private static void _ModifyBackColorByStateMorph(Color color, GInteractiveState state, bool modifyStateNone, Int32? opacity, out Color color1, out Color color2, out bool asGradient)
        {
            color1 = color;
            color2 = color;
            asGradient = false;

            SkinModifierSet modifiers = Skin.Modifiers;
            switch (state)
            {
                case GInteractiveState.None:
                case GInteractiveState.Enabled:
                case GInteractiveState.LeftFrame:
                case GInteractiveState.RightFrame:
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
                            // Tato sekvence zajistí částečnou dostupnost instance již v průběhu inicializace jednotlivých properties:
                            __Instance = new Skin();
                            __Instance._Init();
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
    /// Sada předvoleb pro Modifikátory (podle stavu myši, podle Enabled/Disabled).
    /// </summary>
    public class SkinModifierSet : SkinSet
    {
        #region Internal and private
        internal SkinModifierSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        /// <summary>
        /// Barva použitá pro zvýraznění prostoru, kde se pohybuje myš (použito např. na časové ose)
        /// </summary>
        public Color MouseMoveTracking { get { return this._Owner.GetValue(this._SkinSetKey, "MouseMoveTracking", DefaultMouseMoveTracking); } set { this._Owner.SetValue(this._SkinSetKey, "MouseMoveTracking", value); } }
        /// <summary>
        /// Barva použitá pro zvýraznění prostoru, kde se něco přetahuje myší
        /// </summary>
        public Color MouseDragTracking { get { return this._Owner.GetValue(this._SkinSetKey, "MouseDragTracking", DefaultMouseDragTracking); } set { this._Owner.SetValue(this._SkinSetKey, "MouseDragTracking", value); } }
        /// <summary>
        /// Barva zvýrazňující okolí myši při jejím pohybu
        /// </summary>
        public Color MouseHotColor { get { return this._Owner.GetValue(this._SkinSetKey, "MouseHotColor", DefaultMouseHotColor); } set { this._Owner.SetValue(this._SkinSetKey, "MouseHotColor", value); } }
        /// <summary>
        /// Modifikátor barvy pozadí na počátku
        /// </summary>
        public Color BackColorBegin { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorBegin", DefaultBackColorBegin); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorBegin", value); } }
        /// <summary>
        /// Modifikátor barvy pozadí na konci
        /// </summary>
        public Color BackColorEnd { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorEnd", DefaultBackColorEnd); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorEnd", value); } }
        /// <summary>
        /// Modifikátor barvy pozadí na počátku, stav MouseHot
        /// </summary>
        public Color BackColorHotBegin { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorHotBegin", DefaultBackColorHotBegin); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorHotBegin", value); } }
        /// <summary>
        /// Modifikátor barvy pozadí na konci, stav MouseHot
        /// </summary>
        public Color BackColorHotEnd { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorHotEnd", DefaultBackColorHotEnd); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorHotEnd", value); } }
        /// <summary>
        /// Modifikátor barvy pozadí na počátku, stav MouseDown
        /// </summary>
        public Color BackColorDownBegin { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorDownBegin", DefaultBackColorDownBegin); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorDownBegin", value); } }
        /// <summary>
        /// Modifikátor barvy pozadí na konci, stav MouseDown
        /// </summary>
        public Color BackColorDownEnd { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorDownEnd", DefaultBackColorDownEnd); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorDownEnd", value); } }
        /// <summary>
        /// Modifikátor barvy pozadí na počátku, stav Drag and Drop
        /// </summary>
        public Color BackColorDragBegin { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorDragBegin", DefaultBackColorDragBegin); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorDragBegin", value); } }
        /// <summary>
        /// Modifikátor barvy pozadí na konci, stav Drag and Drop
        /// </summary>
        public Color BackColorDragEnd { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorDragEnd", DefaultBackColorDragEnd); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorDragEnd", value); } }
        /// <summary>
        /// Modifikátor barvy pozadí, stav NotEnabled
        /// </summary>
        public Color BackColorDisable { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorDisable", DefaultBackColorDisable); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorDisable", value); } }
        /// <summary>
        /// Modifikátor barvy pozadí, stav Stín
        /// </summary>
        public Color BackColorShadow { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorShadow", DefaultBackColorShadow); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorShadow", value); } }
        /// <summary>
        /// Modifikátor barvy textu, stav MouseHot
        /// </summary>
        public Color TextColorHot { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorHot", DefaultTextColorHot); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorHot", value); } }
        /// <summary>
        /// Modifikátor barvy textu, stav MouseDown
        /// </summary>
        public Color TextColorDown { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorDown", DefaultTextColorDown); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorDown", value); } }
        /// <summary>
        /// Modifikátor barvy textu, stav Drag and Drop
        /// </summary>
        public Color TextColorDrag { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorDrag", DefaultTextColorDrag); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorDrag", value); } }
        /// <summary>
        /// Modifikátor barvy textu, stav Not Enabled
        /// </summary>
        public Color TextColorDisable { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorDisable", DefaultTextColorDisable); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorDisable", value); } }
        /// <summary>
        /// Modifikátor barvy pozadí pro 3D efekt, tmavší část
        /// </summary>
        public Color Effect3DDark { get { return this._Owner.GetValue(this._SkinSetKey, "Effect3DDark", DefaultEffect3DDark); } set { this._Owner.SetValue(this._SkinSetKey, "Effect3DDark", value); } }
        /// <summary>
        /// Modifikátor barvy pozadí pro 3D efekt, světlejší část
        /// </summary>
        public Color Effect3DLight { get { return this._Owner.GetValue(this._SkinSetKey, "Effect3DLight", DefaultEffect3DLight); } set { this._Owner.SetValue(this._SkinSetKey, "Effect3DLight", value); } }
        /// <summary>
        /// Poměr 3D efektu pro pozadí
        /// </summary>
        public float Effect3DBackgroundRatio { get { return this._Owner.GetValue(this._SkinSetKey, "Effect3DBackgroundRatio", DefaultEffect3DBackgroundRatio); } set { this._Owner.SetValue(this._SkinSetKey, "Effect3DBackgroundRatio", value); } }
        /// <summary>
        /// Poměr 3D efektu pro okraje
        /// </summary>
        public float Effect3DBorderRatio { get { return this._Owner.GetValue(this._SkinSetKey, "Effect3DRatio", DefaultEffect3DRatio); } set { this._Owner.SetValue(this._SkinSetKey, "Effect3DRatio", value); } }
        public Color ShiftBackColorBegin { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorBegin", DefaultShiftBackColorBegin); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorBegin", value); } }
        public Color ShiftBackColorEnd { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorEnd", DefaultShiftBackColorEnd); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorEnd", value); } }
        public Color ShiftBackColorHotBegin { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorHotBegin", DefaultShiftBackColorHotBegin); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorHotBegin", value); } }
        public Color ShiftBackColorHotEnd { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorHotEnd", DefaultShiftBackColorHotEnd); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorHotEnd", value); } }
        public Color ShiftBackColorDownBegin { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorDownBegin", DefaultShiftBackColorDownBegin); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorDownBegin", value); } }
        public Color ShiftBackColorDownEnd { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorDownEnd", DefaultShiftBackColorDownEnd); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorDownEnd", value); } }
        public Color ShiftBackColorDragBegin { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorDragBegin", DefaultShiftBackColorDragBegin); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorDragBegin", value); } }
        public Color ShiftBackColorDragEnd { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorDragEnd", DefaultShiftBackColorDragEnd); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorDragEnd", value); } }
        /// <summary>
        /// Modifikátor barvy pozadí pro prvek, který je cílovým prvkem v procesu Drag and Drop
        /// </summary>
        public Color BackColorDropTargetItem { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorDropTargetItem", DefaultBackColorDropTargetItem); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorDropTargetItem", value); } }
        #endregion
        #region Default colors
        // Modifier colors: Alpha value (0-255) represents Morphing value (0-1) !!!
        protected virtual Color DefaultMouseMoveTracking { get { return Color.FromArgb(96, Color.LightYellow); } }
        protected virtual Color DefaultMouseDragTracking { get { return Color.FromArgb(64, Color.LightYellow); } }
        protected virtual Color DefaultMouseHotColor { get { return Color.FromArgb(192, Color.LightYellow); } }
        protected virtual Color DefaultBackColorBegin { get { return Color.FromArgb(16, Color.White); } }
        protected virtual Color DefaultBackColorEnd { get { return Color.FromArgb(16, Color.Black); } }
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
        protected virtual Color DefaultTextColorDisable { get { return Color.FromArgb(160, Color.Gray); } }
        protected virtual Color DefaultEffect3DDark { get { return Color.FromArgb(64, 64, 64); } }
        protected virtual Color DefaultEffect3DLight { get { return Color.White; } }
        protected virtual float DefaultEffect3DBackgroundRatio { get { return 0.10f; } }
        protected virtual float DefaultEffect3DRatio { get { return 0.25f; } }
        protected virtual Color DefaultBackColorDropTargetItem { get { return Color.FromArgb(128, Color.Magenta); } }

        // Barvy pro provádění Shift = mají ve složká hodnotu posunu základní barvy, kde hodnota 128 = střed = 0:
        protected virtual Color DefaultShiftBackColorBegin { get { return Color.FromArgb(0, 136, 136, 136); } }        //  +8
        protected virtual Color DefaultShiftBackColorEnd { get { return Color.FromArgb(0, 120, 120, 120); } }          //  -8
        protected virtual Color DefaultShiftBackColorHotBegin { get { return Color.FromArgb(0, 160, 160, 160); } }     // +32
        protected virtual Color DefaultShiftBackColorHotEnd { get { return Color.FromArgb(0, 96, 96, 96); } }          // -32
        protected virtual Color DefaultShiftBackColorDownBegin { get { return Color.FromArgb(0, 96, 96, 96); } }       // -32
        protected virtual Color DefaultShiftBackColorDownEnd { get { return Color.FromArgb(0, 160, 160, 160); } }      // +32
        protected virtual Color DefaultShiftBackColorDragBegin { get { return Color.FromArgb(0, 112, 112, 112); } }    // -16
        protected virtual Color DefaultShiftBackColorDragEnd { get { return Color.FromArgb(0, 144, 144, 144); } }      // +16

        #endregion
        #region Servis
        /// <summary>
        /// Vrátí danou barvu pozadí modifikovanou pro aktuální stav <see cref="GInteractiveState"/>.
        /// Volitelně je možno tuto modifikaci korigovat parametrem ratio.
        /// </summary>
        /// <param name="backColor"></param>
        /// <param name="interactiveState"></param>
        /// <param name="ratio">Korekce množství modifikace: 0=nemodifikovat vůbec, 0.25 = modifikovat na 25% standardu, 1 (=null) = modifikovat standardně</param>
        /// <returns></returns>
        public Color GetBackColorModifiedByInteractiveState(Color backColor, GInteractiveState interactiveState, float? ratio = null)
        {
            Color? modifierColor = null;
            switch (interactiveState)
            {
                case GInteractiveState.Enabled:
                    return backColor;
                case GInteractiveState.Disabled:
                    modifierColor = BackColorDisable;
                    break;
                case GInteractiveState.MouseOver:
                    modifierColor = BackColorHotBegin;
                    break;
                default:
                    if (interactiveState.HasFlag(GInteractiveState.FlagDown))
                        modifierColor = BackColorDownBegin;
                    else if (interactiveState.HasFlag(GInteractiveState.FlagDrag))
                        modifierColor = BackColorDragBegin;
                    break;
            }
            if (modifierColor == null) return backColor;
            float modifierRatio = ((float)modifierColor.Value.A / 255f);
            if (ratio.HasValue)
                modifierRatio = ratio.Value * modifierRatio;

            backColor = backColor.Morph(modifierColor.Value, modifierRatio);
            return backColor;
        }
        public Color GetColor3DBorderLight(Color borderColor)
        {
            return borderColor.Morph(this.Effect3DLight, this.Effect3DBorderRatio);
        }
        public Color GetColor3DBorderLight(Color borderColor, float ratio)
        {
            return borderColor.Morph(this.Effect3DLight, ratio);
        }
        public Color GetColor3DBorderDark(Color borderColor)
        {
            return borderColor.Morph(this.Effect3DDark, this.Effect3DBorderRatio);
        }
        public Color GetColor3DBorderDark(Color borderColor, float ratio)
        {
            return borderColor.Morph(this.Effect3DDark, ratio);
        }
        public Color GetColor3DBackgroundLight(Color borderColor)
        {
            return borderColor.Morph(this.Effect3DLight, this.Effect3DBackgroundRatio);
        }
        public Color GetColor3DBackgroundLight(Color borderColor, float ratio)
        {
            return borderColor.Morph(this.Effect3DLight, ratio);
        }
        public Color GetColor3DBackgroundDark(Color borderColor)
        {
            return borderColor.Morph(this.Effect3DDark, this.Effect3DBackgroundRatio);
        }
        public Color GetColor3DBackgroundDark(Color borderColor, float ratio)
        {
            return borderColor.Morph(this.Effect3DDark, ratio);
        }

        #endregion
    }
    /// <summary>
    /// Skin pro stínování.
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
        public Color AmbientBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "AmbientBackColor", DefaultAmbientBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "AmbientBackColor", value); } }
        public Color ControlBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ControlBackColor", DefaultControlBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ControlBackColor", value); } }
        public Color ActiveBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ActiveBackColor", DefaultActiveBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ActiveBackColor", value); } }
        public Color ControlTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextColor", DefaultTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextColor", value); } }
        public Color BorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "BorderColor", DefaultBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "BorderColor", value); } }
        public Color FrameSelectBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "FrameSelectBackColor", DefaultFrameSelectBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "FrameSelectBackColor", value); } }
        public Color FrameSelectLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "FrameSelectLineColor", DefaultFrameSelectLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "FrameSelectLineColor", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultAmbientBackColor { get { return Color.Gray; } }
        protected virtual Color DefaultControlBackColor { get { return Color.LightGray; } }
        protected virtual Color DefaultActiveBackColor { get { return Color.FromArgb(255, 216, 216, 216); } }
        protected virtual Color DefaultTextColor { get { return Color.Black; } }
        protected virtual Color DefaultBorderColor { get { return Color.DimGray; } }
        protected virtual Color DefaultFrameSelectBackColor { get { return Color.FromArgb(48, Color.LightYellow); } }
        protected virtual Color DefaultFrameSelectLineColor { get { return Color.DarkViolet; } }
        #endregion
    }
    /// <summary>
    /// Skin set pro Splitter a Resizer.
    /// </summary>
    public class SkinSplitterSet : SkinSet
    {
        #region Internal and private
        internal SkinSplitterSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Color InactiveColor { get { return this._Owner.GetValue(this._SkinSetKey, "InactiveColor", DefaultInactiveColor); } set { this._Owner.SetValue(this._SkinSetKey, "InactiveColor", value); } }
        public int InactiveSize { get { return this._Owner.GetValue(this._SkinSetKey, "InactiveSize", DefaultInactiveSize); } set { this._Owner.SetValue(this._SkinSetKey, "InactiveSize", value); } }
        public Color MouseOnParentColor { get { return this._Owner.GetValue(this._SkinSetKey, "MouseOnParentColor", DefaultMouseOnParentColor); } set { this._Owner.SetValue(this._SkinSetKey, "MouseOnParentColor", value); } }
        public int MouseOnParentSize { get { return this._Owner.GetValue(this._SkinSetKey, "MouseOnParentSize", DefaultMouseOnParentSize); } set { this._Owner.SetValue(this._SkinSetKey, "MouseOnParentSize", value); } }
        public Color MouseOverColor { get { return this._Owner.GetValue(this._SkinSetKey, "MouseOverColor", DefaultMouseOverColor); } set { this._Owner.SetValue(this._SkinSetKey, "MouseOverColor", value); } }
        public int MouseOverSize { get { return this._Owner.GetValue(this._SkinSetKey, "MouseOverSize", DefaultMouseOverSize); } set { this._Owner.SetValue(this._SkinSetKey, "MouseOverSize", value); } }
        public Color MouseDownColor { get { return this._Owner.GetValue(this._SkinSetKey, "MouseDownColor", DefaultMouseDownColor); } set { this._Owner.SetValue(this._SkinSetKey, "MouseDownColor", value); } }
        public int MouseDownSize { get { return this._Owner.GetValue(this._SkinSetKey, "MouseDownSize", DefaultMouseDownSize); } set { this._Owner.SetValue(this._SkinSetKey, "MouseDownSize", value); } }
        public int InteractivePaddingSize { get { return this._Owner.GetValue(this._SkinSetKey, "InteractivePaddingSize", DefaultInteractivePaddingSize); } set { this._Owner.SetValue(this._SkinSetKey, "InteractivePaddingSize", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultInactiveColor { get { return Color.FromArgb(96, 96, 96, 64); } }
        protected virtual int DefaultInactiveSize { get { return 2; } }
        protected virtual Color DefaultMouseOnParentColor { get { return Color.FromArgb(160, 96, 96, 64); } }
        protected virtual int DefaultMouseOnParentSize { get { return 3; } }
        protected virtual Color DefaultMouseOverColor { get { return Color.FromArgb(220, 96, 96, 96); } }
        protected virtual int DefaultMouseOverSize { get { return 4; } }
        protected virtual Color DefaultMouseDownColor { get { return Color.FromArgb(255, 64, 64, 64); } }
        protected virtual int DefaultMouseDownSize { get { return 4; } }
        protected virtual int DefaultInteractivePaddingSize { get { return 3; } }
        #endregion
    }
    /// <summary>
    /// Skin set for BlockedGui.
    /// </summary>
    public class SkinBlockedGuiSet : SkinSet
    {
        #region Internal and private
        internal SkinBlockedGuiSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Color AreaColor { get { return this._Owner.GetValue(this._SkinSetKey, "AreaColor", DefaultAreaColor); } set { this._Owner.SetValue(this._SkinSetKey, "AreaColor", value); } }
        public Color TextBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextBackColor", DefaultTextBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextBackColor", value); } }
        public Color TextTitleForeColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextTitleForeColor", DefaultTextTitleForeColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextTitleForeColor", value); } }
        public Color TextInfoForeColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextInfoForeColor", DefaultTextInfoForeColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextInfoForeColor", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultAreaColor { get { return Color.FromArgb(144, 144, 144, 144); } }
        protected virtual Color DefaultTextBackColor { get { return Color.FromArgb(240, 196, 255, 255); } }
        protected virtual Color DefaultTextTitleForeColor { get { return Color.Black; } }
        protected virtual Color DefaultTextInfoForeColor { get { return Color.Black; } }
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
        protected virtual Color DefaultBackColor { get { return Color.FromArgb(255, 255, 240) /*Color.LightYellow*/; } }
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
        public Color ItemSelectedBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemSelectedBackColor", DefaultItemSelectedBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemSelectedBackColor", value); } }
        public Color ItemSelectedLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemSelectedLineColor", DefaultItemSelectedLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemSelectedLineColor", value); } }

        public Color ItemBorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemBorderColor", DefaultItemBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemBorderColor", value); } }
        public Color TitleBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "TitleBackColor", DefaultTitleBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "TitleBackColor", value); } }
        public Color SeparatorLightColor { get { return this._Owner.GetValue(this._SkinSetKey, "SeparatorLightColor", DefaultSeparatorLightColor); } set { this._Owner.SetValue(this._SkinSetKey, "SeparatorLightColor", value); } }
        public Color SeparatorDarkColor { get { return this._Owner.GetValue(this._SkinSetKey, "SeparatorDarkColor", DefaultSeparatorDarkColor); } set { this._Owner.SetValue(this._SkinSetKey, "SeparatorDarkColor", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultBorderColor { get { return Color.DimGray; } }
        protected virtual Color DefaultBackColor { get { return Color.FromArgb(255, 250, 250, 250); } }
        protected virtual Color DefaultTextColor { get { return Color.Black; } }
        protected virtual Color DefaultItemBackColor { get { return Color.FromArgb(255, 224, 224, 240); } }
        protected virtual Color DefaultItemSelectedBackColor { get { return Color.FromArgb(255, 240, 240, 160); } }
        protected virtual Color DefaultItemSelectedLineColor { get { return Color.DimGray; } }
        protected virtual Color DefaultItemBorderColor { get { return Color.DimGray; } }
        protected virtual Color DefaultTitleBackColor { get { return Color.FromArgb(128, 240, 224, 246); } }
        protected virtual Color DefaultSeparatorLightColor { get { return Color.LightGray; } }
        protected virtual Color DefaultSeparatorDarkColor { get { return Color.DimGray; } }
        #endregion
    }
    /// <summary>
    /// Skin set for TagFilter.
    /// Has colors: BackColor; TextColor;
    /// </summary>
    public class SkinTagFilterSet : SkinSet
    {
        #region Internal and private
        internal SkinTagFilterSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        public Color ItemBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemBackColor", DefaultItemBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemBackColor", value); } }
        public Color ItemCheckedBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemCheckedBackColor", DefaultItemCheckedBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemCheckedBackColor", value); } }
        public Color SelectAllItemBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "SelectAllItemBackColor", DefaultSelectAllItemBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "SelectAllItemBackColor", value); } }
        public Color SelectAllItemCheckedBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "SelectAllItemCheckedBackColor", DefaultSelectAllItemCheckedBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "SelectAllItemCheckedBackColor", value); } }
        public Color ItemBorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemBorderColor", DefaultItemBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemBorderColor", value); } }
        public Color ItemTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemTextColor", DefaultItemTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemTextColor", value); } }
        public Size ItemSpacing { get { return this._Owner.GetValue(this._SkinSetKey, "ItemSpacing", DefaultItemSpacing); } set { this._Owner.SetValue(this._SkinSetKey, "ItemSpacing", value); } }
        public int ItemHeight { get { return this._Owner.GetValue(this._SkinSetKey, "ItemHeight", DefaultItemHeight); } set { this._Owner.SetValue(this._SkinSetKey, "ItemHeight", value); } }
        public Image ItemSelectedImage { get { return this._Owner.GetValue(this._SkinSetKey, "ItemSelectedImage", DefaultItemSelectedImage); } set { this._Owner.SetValue(this._SkinSetKey, "ItemSelectedImage", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultBackColor { get { return Color.FromArgb(255, 160, 160, 176); } }
        protected virtual Color DefaultItemBackColor { get { return Color.FromArgb(255, 240, 240, 240); } }
        protected virtual Color DefaultItemCheckedBackColor { get { return Color.FromArgb(255, 192, 255, 255); } }
        protected virtual Color DefaultSelectAllItemBackColor { get { return Color.FromArgb(255, 160, 232, 160); } }
        protected virtual Color DefaultSelectAllItemCheckedBackColor { get { return Color.FromArgb(255, 180, 255, 180); } }
        protected virtual Color DefaultItemBorderColor { get { return Color.DimGray; } }
        protected virtual Color DefaultItemTextColor { get { return Color.Black; } }
        protected virtual Size DefaultItemSpacing { get { return new Size(3, 2); } }
        protected virtual int DefaultItemHeight { get { return 24; } }
        protected virtual Image DefaultItemSelectedImage { get { return Application.App.Resources.GetImage(R.Images.Actions16.DialogOkApply2Png); } }
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
    /// Skin set for TabHeader.
    /// Has colors: BorderColor, BackColor; TextColor, BackColorActive, TextColorActive;
    /// </summary>
    public class SkinTabHeaderSet : SkinSet
    {
        #region Internal and private
        internal SkinTabHeaderSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public int HeaderHeight { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderHeight", DefaultHeaderHeight); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderHeight", value); } }
        public Color SpaceColor { get { return this._Owner.GetValue(this._SkinSetKey, "SpaceColor", DefaultSpaceColor); } set { this._Owner.SetValue(this._SkinSetKey, "SpaceColor", value); } }
        public Color BorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "BorderColor", DefaultBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "BorderColor", value); } }
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        public Color TextColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextColor", DefaultTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextColor", value); } }
        public Color LineColorActive { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorActive", DefaultLineColorActive); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorActive", value); } }
        public Color BackColorActive { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorActive", DefaultBackColorActive); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorActive", value); } }
        public Color LineColorHot { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorHot", DefaultLineColorHot); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorHot", value); } }
        public Color TextColorActive { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorActive", DefaultTextColorActive); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorActive", value); } }
        #endregion
        #region Default colors
        protected virtual int DefaultHeaderHeight { get { return 28; } }
        protected virtual Color DefaultSpaceColor { get { return Skin.Control.AmbientBackColor; } }
        protected virtual Color DefaultBorderColor { get { return Skin.Control.BorderColor; } }
        protected virtual Color DefaultBackColor { get { return Skin.Control.ControlBackColor; } }
        protected virtual Color DefaultTextColor { get { return Skin.Control.ControlTextColor; } }
        protected virtual Color DefaultBackColorActive { get { return Skin.Control.ActiveBackColor; } }
        protected virtual Color DefaultTextColorActive { get { return Skin.Control.ControlTextColor; } }
        protected virtual Color DefaultLineColorHot { get { return Color.FromArgb(255, 216, 216, 128); } }
        protected virtual Color DefaultLineColorActive { get { return Color.FromArgb(255, 255, 255, 128); } }
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
        protected virtual Color DefaultBackColorArea { get { return Color.FromArgb(255, 160, 160, 176); } }
        protected virtual Color DefaultBackColorButton { get { return Color.FromArgb(255, 216, 216, 216); } }
        protected virtual Color DefaultTextColor { get { return Color.Black; } }
        #endregion
    }
    /// <summary>
    /// Skin set for TrackBar.
    /// Has colors: BackColorTrack; LineColorTrack; BackColorButton, LineColorButton;
    /// </summary>
    public class SkinTrackBarSet : SkinSet
    {
        #region Internal and private
        internal SkinTrackBarSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Color BackColorTrack { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorTrack", DefaultBackColorTrack); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorTrack", value); } }
        public Color LineColorTrack { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorTrack", DefaultLineColorTrack); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorTrack", value); } }
        public Color LineColorTick { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorTick", DefaultLineColorTick); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorTick", value); } }
        public Color BackColorButton { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorButton", DefaultBackColorButton); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorButton", value); } }
        public Color BackColorMouseOverButton { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorMouseOverButton", DefaultBackColorMouseOverButton); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorMouseOverButton", value); } }
        public Color BackColorMouseDownButton { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorMouseDownButton", DefaultBackColorMouseDownButton); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorMouseDownButton", value); } }
        public Color LineColorButton { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorButton", DefaultLineColorButton); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorButton", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultBackColorTrack { get { return Color.FromArgb(255, 180, 180, 192); } }
        protected virtual Color DefaultLineColorTrack { get { return Color.FromArgb(255, 64, 64, 64); } }
        protected virtual Color DefaultLineColorTick { get { return Color.FromArgb(255, 160, 160, 168); } }
        protected virtual Color DefaultBackColorButton { get { return Color.FromArgb(255, 224, 224, 240); } }
        protected virtual Color DefaultBackColorMouseOverButton { get { return Color.FromArgb(255, 232, 232, 224); } }
        protected virtual Color DefaultBackColorMouseDownButton { get { return Color.FromArgb(255, 200, 200, 180); } }
        protected virtual Color DefaultLineColorButton { get { return Color.FromArgb(255, 80, 80, 96); } }
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
        public Color HeaderLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderLineColor", DefaultHeaderLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderLineColor", value); } }
        public Color HeaderLineLeftVerticalColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderLineLeftVerticalColor", DefaultHeaderLineLeftVerticalColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderLineLeftVerticalColor", value); } }
        public Color HeaderLineRightVerticalColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderLineRightVerticalColor", DefaultHeaderLineRightVerticalColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderLineRightVerticalColor", value); } }
        public Color HeaderLineHorizontalColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderLineHorizontalColor", DefaultHeaderLineHorizontalColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderLineHorizontalColor", value); } }
        public Color TableBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "TableBackColor", DefaultTableBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "TableBackColor", value); } }
        public Color RowBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "RowBackColor", DefaultRowBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "RowBackColor", value); } }
        public Color RowChildBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "RowChildBackColor", DefaultRowChildBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "RowChildBackColor", value); } }
        public Color RowTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "RowTextColor", DefaultRowTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "RowTextColor", value); } }
        public Color SelectedRowBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "SelectedRowBackColor", DefaultSelectedRowBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "SelectedRowBackColor", value); } }
        public Color SelectedRowChildBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "SelectedRowChildBackColor", DefaultSelectedRowChildBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "SelectedRowChildBackColor", value); } }
        public Color SelectedRowTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "SelectedRowTextColor", DefaultSelectedRowTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "SelectedRowTextColor", value); } }
        public Color ActiveCellBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ActiveCellBackColor;", DefaultActiveCellBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ActiveCellBackColor;", value); } }
        public Color ActiveCellTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "ActiveCellTextColor ", DefaultActiveCellTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "ActiveCellTextColor ", value); } }
        public Color BorderLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "BorderLineColor", DefaultBorderLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "BorderLineColor", value); } }
        public Image SortAscendingImage { get { return this._Owner.GetValue(this._SkinSetKey, "SortAscendingImage", DefaultSortAscendingImage); } set { this._Owner.SetValue(this._SkinSetKey, "SortAscendingImage", value); } }
        public Image SortDescendingImage { get { return this._Owner.GetValue(this._SkinSetKey, "SortDescendingImage", DefaultSortDescendingImage); } set { this._Owner.SetValue(this._SkinSetKey, "SortDescendingImage", value); } }
        public Image RowSelectedImage { get { return this._Owner.GetValue(this._SkinSetKey, "RowSelectedImage", DefaultRowSelectedImage); } set { this._Owner.SetValue(this._SkinSetKey, "RowSelectedImage", value); } }
        public Image RowHeaderDeselectedAllImage { get { return this._Owner.GetValue(this._SkinSetKey, "RowHeaderDeselectedAllImage", DefaultRowHeaderDeselectedAllImage); } set { this._Owner.SetValue(this._SkinSetKey, "RowHeaderDeselectedAllImage", value); } }
        public Image RowHeaderExpandAllImage { get { return this._Owner.GetValue(this._SkinSetKey, "RowHeaderExpandAllImage", DefaultRowHeaderExpandAllImage); } set { this._Owner.SetValue(this._SkinSetKey, "RowHeaderExpandAllImage", value); } }
        public Image RowHeaderCollapseAllImage { get { return this._Owner.GetValue(this._SkinSetKey, "RowHeaderCollapseAllImage", DefaultRowHeaderCollapseAllImage); } set { this._Owner.SetValue(this._SkinSetKey, "RowHeaderCollapseAllImage", value); } }
        public Color TreeViewLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "TreeViewLineColor", DefaultTreeViewLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "TreeViewLineColor", value); } }
        #endregion
        #region Default colors
        protected virtual Color DefaultHeaderBackColor { get { return Color.LightSkyBlue.Morph(Color.White, 0.33f); } }
        protected virtual Color DefaultHeaderTextColor { get { return Color.Black; } }
        protected virtual Color DefaultHeaderLineColor { get { return Color.DimGray; } }
        protected virtual Color DefaultHeaderLineLeftVerticalColor { get { return Color.Gray; } }
        protected virtual Color DefaultHeaderLineRightVerticalColor { get { return Color.LightGray; } }
        protected virtual Color DefaultHeaderLineHorizontalColor { get { return Color.LightGray; } }
        protected virtual Color DefaultTableBackColor { get { return Skin.Control.AmbientBackColor; } }
        protected virtual Color DefaultRowBackColor { get { return Color.White; } }
        protected virtual Color DefaultRowChildBackColor { get { return Color.FromArgb(215, 230, 220); } }
        protected virtual Color DefaultRowTextColor { get { return Color.Black; } }
        protected virtual Color DefaultSelectedRowBackColor { get { return Color.White.Morph(Color.CadetBlue, 0.25f); } }
        protected virtual Color DefaultSelectedRowChildBackColor { get { return Color.FromArgb(185, 210, 195); } }
        protected virtual Color DefaultSelectedRowTextColor { get { return Color.Black; } }
        protected virtual Color DefaultActiveCellBackColor { get { return Color.White.Morph(Color.CadetBlue, 0.25f); } }
        protected virtual Color DefaultActiveCellTextColor { get { return Color.Black; } }
        protected virtual Color DefaultBorderLineColor { get { return Color.Gray; } }
        protected virtual Image DefaultSortAscendingImage { get { return Application.App.Resources.GetImage(R.Images.Actions16.GoUp2Png); } }          // { get { return IconStandard.SortAsc; } }
        protected virtual Image DefaultSortDescendingImage { get { return Application.App.Resources.GetImage(R.Images.Actions16.GoDown2Png); } }       // { get { return IconStandard.SortDesc; } }
        protected virtual Image DefaultRowSelectedImage { get { return Application.App.Resources.GetImage(R.Images.Actions16.DialogAccept2Png); } }    // { get { return IconStandard.RowSelected; } }
        protected virtual Image DefaultRowHeaderDeselectedAllImage { get { return Application.App.Resources.GetImage(R.Images.Actions16.DialogClose2Png); } }
        protected virtual Image DefaultRowHeaderExpandAllImage { get { return Application.App.Resources.GetImage(R.Images.Actions16.ArrowRightDouble2Png); } }
        protected virtual Image DefaultRowHeaderCollapseAllImage { get { return Application.App.Resources.GetImage(R.Images.Actions16.ArrowLeftDouble2Png); } }
        protected virtual Color DefaultTreeViewLineColor { get { return Color.DimGray; } }
        #endregion
        #region Další metody
        /// <summary>
        /// Vrací barvu pozadí řádků (buněk) pro řádek root/child, normal/selected
        /// </summary>
        /// <param name="isRoot"></param>
        /// <param name="isChecked"></param>
        /// <returns></returns>
        public Color GetBackColor(bool isRoot, bool isChecked)
        {
            return GetMatrix(isRoot, isChecked, this.RowChildBackColor, this.SelectedRowChildBackColor, this.RowBackColor, this.SelectedRowBackColor);
        }
        #endregion
    }
    /// <summary>
    /// Sada grafických prvků pro typ Graph.
    /// </summary>
    public class SkinGraphSet : SkinSet
    {
        #region Internal and private
        internal SkinGraphSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Int32 LineHeight { get { return this._Owner.GetValue(this._SkinSetKey, "LineHeight", DefaultLineHeight); } set { this._Owner.SetValue(this._SkinSetKey, "LineHeight", value); } }
        public Int32 TotalHeightMin { get { return this._Owner.GetValue(this._SkinSetKey, "TotalHeightMin", DefaultTotalHeightMin); } set { this._Owner.SetValue(this._SkinSetKey, "TotalHeightMin", value); } }
        public Int32 TotalHeightMax { get { return this._Owner.GetValue(this._SkinSetKey, "TotalHeightMax", DefaultTotalHeightMax); } set { this._Owner.SetValue(this._SkinSetKey, "TotalHeightMax", value); } }
        public Color ElementBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ElementBackColor", DefaultElementBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ElementBackColor", value); } }
        public Color ElementBorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "ElementBorderColor", DefaultElementBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "ElementBorderColor", value); } }
        public Color ElementLinkBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ElementLinkBackColor", DefaultElementLinkBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ElementLinkBackColor", value); } }
        public Color ElementSelectedLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "ElementSelectedLineColor", DefaultElementSelectedLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "ElementSelectedLineColor", value); } }
        public Color ElementFramedLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "ElementFramedLineColor", DefaultElementFramedLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "ElementFramedLineColor", value); } }
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        public Color TimeAxisTickMain { get { return this._Owner.GetValue(this._SkinSetKey, "TimeAxisTickMain", DefaultTimeAxisTickMain); } set { this._Owner.SetValue(this._SkinSetKey, "TimeAxisTickMain", value); } }
        public Color TimeAxisTickSmall { get { return this._Owner.GetValue(this._SkinSetKey, "TimeAxisTickSmall", DefaultTimeAxisTickSmall); } set { this._Owner.SetValue(this._SkinSetKey, "TimeAxisTickSmall", value); } }
        /// <summary>
        /// Barva linky základní.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je větší nebo rovno Prev.End, pak se použije <see cref="LinkColorStandard"/>.
        /// Další barvy viz <see cref="LinkColorWarning"/> a <see cref="LinkColorError"/>
        /// </summary>
        public Color LinkColorStandard { get { return this._Owner.GetValue(this._SkinSetKey, "LinkColorStandard", DefaultLinkColorStandard); } set { this._Owner.SetValue(this._SkinSetKey, "LinkColorStandard", value); } }
        /// <summary>
        /// Barva linky varovná.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.End, ale Next.Begin je větší nebo rovno Prev.Begin, pak se použije <see cref="LinkColorWarning"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorError"/>
        /// </summary>
        public Color LinkColorWarning { get { return this._Owner.GetValue(this._SkinSetKey, "LinkColorWarning", DefaultLinkColorWarning); } set { this._Owner.SetValue(this._SkinSetKey, "LinkColorWarning", value); } }
        /// <summary>
        /// Barva linky chybová.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.Begin, pak se použije <see cref="LinkColorError"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorWarning"/>
        /// </summary>
        public Color LinkColorError { get { return this._Owner.GetValue(this._SkinSetKey, "LinkColorError", DefaultLinkColorError); } set { this._Owner.SetValue(this._SkinSetKey, "LinkColorError", value); } }


        #endregion
        #region Default colors
        protected virtual Int32 DefaultLineHeight { get { return 18; } }
        protected virtual Int32 DefaultTotalHeightMin { get { return 14; } }
        protected virtual Int32 DefaultTotalHeightMax { get { return 480; } }
        protected virtual Color DefaultElementBackColor { get { return Color.CornflowerBlue; } }
        protected virtual Color DefaultElementBorderColor { get { return Color.BlueViolet; } }
        protected virtual Color DefaultElementLinkBackColor { get { return Color.FromArgb(160, Color.DimGray); } }   // Barva linku obsahuje složku Alpha = 160 == úroveň Morphingu
        protected virtual Color DefaultElementSelectedLineColor { get { return Color.OrangeRed; } }
        protected virtual Color DefaultElementFramedLineColor { get { return Color.IndianRed; } }
        protected virtual Color DefaultBackColor { get { return Color.DimGray; } }
        protected virtual Color DefaultTimeAxisTickMain { get { return Color.FromArgb(216, 216, 216); } }
        protected virtual Color DefaultTimeAxisTickSmall { get { return Color.FromArgb(216, 216, 216); } }
        protected virtual Color DefaultLinkColorStandard { get { return Color.Green; } }
        protected virtual Color DefaultLinkColorWarning { get { return Color.Orange; } }
        protected virtual Color DefaultLinkColorError { get { return Color.Red; } }
        #endregion
    }
    /// <summary>
    /// Sada grafických prvků pro typ Relation.
    /// </summary>
    public class SkinRelationSet : SkinSet
    {
        #region Internal and private
        internal SkinRelationSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public colors
        public Int32 LineHeightInGrid { get { return this._Owner.GetValue(this._SkinSetKey, "LineHeightInGrid", DefaultLineHeightInGrid); } set { this._Owner.SetValue(this._SkinSetKey, "LineHeightInGrid", value); } }
        public Int32 LineHeightInForm { get { return this._Owner.GetValue(this._SkinSetKey, "LineHeightInForm", DefaultLineHeightInForm); } set { this._Owner.SetValue(this._SkinSetKey, "LineHeightInForm", value); } }
        public Color LineColorInGrid { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorInGrid", DefaultLineColorInGrid); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorInGrid", value); } }
        public Color LineColorInForm { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorInForm", DefaultLineColorInForm); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorInForm", value); } }
        public float LineFadingRatio { get { return this._Owner.GetValue(this._SkinSetKey, "LineFadingRatio", DefaultLineFadingRatio); } set { this._Owner.SetValue(this._SkinSetKey, "LineFadingRatio", value); } }
        #endregion
        #region Default colors
        protected virtual Int32 DefaultLineHeightInGrid { get { return 1; } }
        protected virtual Int32 DefaultLineHeightInForm { get { return 2; } }
        protected virtual Color DefaultLineColorInGrid { get { return Color.BlueViolet; } }
        protected virtual Color DefaultLineColorInForm { get { return Color.BlueViolet; } }
        protected virtual float DefaultLineFadingRatio { get { return 0.60f; } }
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
            {   // Provedu načtení hodnoty ze všech public instančních property; tím se tyto property a jejich hodnoty dostanou do centrální cache:
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
        /// <summary>
        /// Vrací výsledek matrice 2 x 2
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t0">Vstup 0</param>
        /// <param name="t1">Vstup 1</param>
        /// <param name="value00">Výstup pro 0 = false; 1 = false</param>
        /// <param name="value01">Výstup pro 0 = false; 1 = true</param>
        /// <param name="value10">Výstup pro 0 = true; 1 = false</param>
        /// <param name="value11">Výstup pro 0 = true; 1 = true</param>
        /// <returns></returns>
        public static T GetMatrix<T>(bool t0, bool t1, T value00, T value01, T value10, T value11)
        {
            return (t0 ? (t1 ? value11 : value10) : (t1 ? value01 : value00));
        }
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
