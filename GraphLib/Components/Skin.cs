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
        /// Barvy pro TextBox
        /// </summary>
        public static SkinTextboxSet TextBox { get { return Instance._TextBox; } }
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
        /// Ikony pro dialogy
        /// </summary>
        public static SkinIcons Icons { get { return Instance._Icons; } }
        /// <summary>
        /// Resetuje všechny hodnoty
        /// </summary>
        public static void Reset() { Instance.ResetValues(); }
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
            this._TextBox = new SkinTextboxSet(this, "TextBox");
            this._TabHeader = new SkinTabHeaderSet(this, "TabHeader");
            this._ScrollBar = new SkinScrollBarSet(this, "ScrollBar");
            this._TrackBar = new SkinTrackBarSet(this, "TrackBar");
            this._Progress = new SkinProgressSet(this, "Progress");
            this._Axis = new SkinAxisSet(this, "Axis");
            this._Grid = new SkinGridSet(this, "Grid");
            this._Graph = new SkinGraphSet(this, "Graph");
            this._Relation = new SkinRelationSet(this, "Relation");
            this._Icons = new SkinIcons(this, "Icons");
        }
        /// <summary>
        /// Add a new instance of SkinSet to Skin.
        /// Use pattern for "external" SkinSet:
        /// </summary>
        /// <param name="skinSet"></param>
        public static void Add(ISkinSet skinSet)
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
        private SkinTextboxSet _TextBox;
        private SkinButtonSet _Button;
        private SkinTabHeaderSet _TabHeader;
        private SkinScrollBarSet _ScrollBar;
        private SkinTrackBarSet _TrackBar;
        private SkinProgressSet _Progress;
        private SkinAxisSet _Axis;
        private SkinGridSet _Grid;
        private SkinGraphSet _Graph;
        private SkinRelationSet _Relation;
        private SkinIcons _Icons;
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
        /// <summary>
        /// Zahodí z úschovny všechny objekty
        /// </summary>
        internal void ResetValues()
        {
            this._ValueDict.Clear();
        }
        /// <summary>
        /// Zahodí z úschovny všechny objekty daného setu
        /// </summary>
        /// <param name="skinSetKey"></param>
        internal void ResetValues(string skinSetKey)
        {
            string keyPrefix = skinSetKey + ".";
            int length = keyPrefix.Length;
            var keys = this._ValueDict.Keys.Where(k => k.Substring(0, length) == keyPrefix).ToArray();
            this._ValueDict.RemoveKeys(keys);
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
        #region Public values
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
        /// <summary>
        /// Posun barvy pozadí na počátku
        /// </summary>
        public Color ShiftBackColorBegin { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorBegin", DefaultShiftBackColorBegin); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorBegin", value); } }
        /// <summary>
        /// Posun barvy pozadí na konci
        /// </summary>
        public Color ShiftBackColorEnd { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorEnd", DefaultShiftBackColorEnd); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorEnd", value); } }
        /// <summary>
        /// Posun barvy pozadí na počátku za stavu MouseHot
        /// </summary>
        public Color ShiftBackColorHotBegin { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorHotBegin", DefaultShiftBackColorHotBegin); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorHotBegin", value); } }
        /// <summary>
        /// Posun barvy pozadí na konci za stavu MouseHot
        /// </summary>
        public Color ShiftBackColorHotEnd { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorHotEnd", DefaultShiftBackColorHotEnd); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorHotEnd", value); } }
        /// <summary>
        /// Posun barvy pozadí na počátku za stavu MouseDown
        /// </summary>
        public Color ShiftBackColorDownBegin { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorDownBegin", DefaultShiftBackColorDownBegin); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorDownBegin", value); } }
        /// <summary>
        /// Posun barvy pozadí na konci za stavu MouseDown
        /// </summary>
        public Color ShiftBackColorDownEnd { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorDownEnd", DefaultShiftBackColorDownEnd); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorDownEnd", value); } }
        /// <summary>
        /// Posun barvy pozadí na počátku za stavu MouseDrag
        /// </summary>
        public Color ShiftBackColorDragBegin { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorDragBegin", DefaultShiftBackColorDragBegin); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorDragBegin", value); } }
        /// <summary>
        /// Posun barvy pozadí na konci za stavu MouseDrag
        /// </summary>
        public Color ShiftBackColorDragEnd { get { return this._Owner.GetValue(this._SkinSetKey, "ShiftBackColorDragEnd", DefaultShiftBackColorDragEnd); } set { this._Owner.SetValue(this._SkinSetKey, "ShiftBackColorDragEnd", value); } }
        /// <summary>
        /// Modifikátor barvy pozadí pro prvek, který je cílovým prvkem v procesu Drag and Drop
        /// </summary>
        public Color BackColorDropTargetItem { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorDropTargetItem", DefaultBackColorDropTargetItem); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorDropTargetItem", value); } }
        #endregion
        #region Default values
        // Modifier colors: Alpha value (0-255) represents Morphing value (0-1) !!!
        /// <summary>
        /// Defaultní barva: DefaultMouseMoveTracking
        /// </summary>
        protected virtual Color DefaultMouseMoveTracking { get { return Color.FromArgb(96, Color.LightYellow); } }
        /// <summary>
        /// Defaultní barva: DefaultMouseDragTracking
        /// </summary>
        protected virtual Color DefaultMouseDragTracking { get { return Color.FromArgb(64, Color.LightYellow); } }
        /// <summary>
        /// Defaultní barva: DefaultMouseHotColor
        /// </summary>
        protected virtual Color DefaultMouseHotColor { get { return Color.FromArgb(192, Color.LightYellow); } }
        /// <summary>
        /// Defaultní barva: DefaultBackColorBegin
        /// </summary>
        protected virtual Color DefaultBackColorBegin { get { return Color.FromArgb(16, Color.White); } }
        /// <summary>
        /// Defaultní barva: DefaultBackColorEnd
        /// </summary>
        protected virtual Color DefaultBackColorEnd { get { return Color.FromArgb(16, Color.Black); } }
        /// <summary>
        /// Defaultní barva: DefaultBackColorHotBegin
        /// </summary>
        protected virtual Color DefaultBackColorHotBegin { get { return Color.FromArgb(64, Color.White); } }
        /// <summary>
        /// Defaultní barva: DefaultBackColorHotEnd
        /// </summary>
        protected virtual Color DefaultBackColorHotEnd { get { return Color.FromArgb(64, Color.Black); } }
        /// <summary>
        /// Defaultní barva: DefaultBackColorDownBegin
        /// </summary>
        protected virtual Color DefaultBackColorDownBegin { get { return Color.FromArgb(64, Color.Black); } }
        /// <summary>
        /// Defaultní barva: DefaultBackColorDownEnd
        /// </summary>
        protected virtual Color DefaultBackColorDownEnd { get { return Color.FromArgb(48, Color.White); } }
        /// <summary>
        /// Defaultní barva: DefaultBackColorDragBegin
        /// </summary>
        protected virtual Color DefaultBackColorDragBegin { get { return Color.FromArgb(38, Color.Black); } }
        /// <summary>
        /// Defaultní barva: DefaultBackColorDragEnd
        /// </summary>
        protected virtual Color DefaultBackColorDragEnd { get { return Color.FromArgb(64, Color.White); } }
        /// <summary>
        /// Defaultní barva: DefaultBackColorDisable
        /// </summary>
        protected virtual Color DefaultBackColorDisable { get { return Color.FromArgb(96, Color.LightGray); } }
        /// <summary>
        /// Defaultní barva: DefaultBackColorShadow
        /// </summary>
        protected virtual Color DefaultBackColorShadow { get { return Color.FromArgb(96, Color.DimGray); } }
        /// <summary>
        /// Defaultní barva: DefaultTextColorHot
        /// </summary>
        protected virtual Color DefaultTextColorHot { get { return Color.FromArgb(64, Color.DarkViolet); } }
        /// <summary>
        /// Defaultní barva: DefaultTextColorDown
        /// </summary>
        protected virtual Color DefaultTextColorDown { get { return Color.FromArgb(96, Color.DarkViolet); } }
        /// <summary>
        /// Defaultní barva: DefaultTextColorDrag
        /// </summary>
        protected virtual Color DefaultTextColorDrag { get { return Color.FromArgb(96, Color.DarkViolet); } }
        /// <summary>
        /// Defaultní barva: DefaultTextColorDisable
        /// </summary>
        protected virtual Color DefaultTextColorDisable { get { return Color.FromArgb(160, Color.Gray); } }
        /// <summary>
        /// Defaultní barva: DefaultEffect3DDark
        /// </summary>
        protected virtual Color DefaultEffect3DDark { get { return Color.FromArgb(64, 64, 64); } }
        /// <summary>
        /// Defaultní barva: DefaultEffect3DLight
        /// </summary>
        protected virtual Color DefaultEffect3DLight { get { return Color.White; } }
        /// <summary>
        /// Defaultní barva: DefaultEffect3DBackgroundRatio
        /// </summary>
        protected virtual float DefaultEffect3DBackgroundRatio { get { return 0.10f; } }
        /// <summary>
        /// Defaultní barva: DefaultEffect3DRatio
        /// </summary>
        protected virtual float DefaultEffect3DRatio { get { return 0.25f; } }
        /// <summary>
        /// Defaultní barva: DefaultBackColorDropTargetItem
        /// </summary>
        protected virtual Color DefaultBackColorDropTargetItem { get { return Color.FromArgb(128, Color.Magenta); } }

        // Barvy pro provádění Shift = mají ve složká hodnotu posunu základní barvy, kde hodnota 128 = střed = 0:
        /// <summary>
        /// Defaultní barevný posun: DefaultShiftBackColorBegin
        /// </summary>
        protected virtual Color DefaultShiftBackColorBegin { get { return Color.FromArgb(0, 136, 136, 136); } }        //  +8
        /// <summary>
        /// Defaultní barevný posun: DefaultShiftBackColorEnd
        /// </summary>
        protected virtual Color DefaultShiftBackColorEnd { get { return Color.FromArgb(0, 120, 120, 120); } }          //  -8
        /// <summary>
        /// Defaultní barevný posun: DefaultShiftBackColorHotBegin
        /// </summary>
        protected virtual Color DefaultShiftBackColorHotBegin { get { return Color.FromArgb(0, 160, 160, 160); } }     // +32
        /// <summary>
        /// Defaultní barevný posun: DefaultShiftBackColorHotEnd
        /// </summary>
        protected virtual Color DefaultShiftBackColorHotEnd { get { return Color.FromArgb(0, 96, 96, 96); } }          // -32
        /// <summary>
        /// Defaultní barevný posun: DefaultShiftBackColorDownBegin
        /// </summary>
        protected virtual Color DefaultShiftBackColorDownBegin { get { return Color.FromArgb(0, 96, 96, 96); } }       // -32
        /// <summary>
        /// Defaultní barevný posun: DefaultShiftBackColorDownEnd
        /// </summary>
        protected virtual Color DefaultShiftBackColorDownEnd { get { return Color.FromArgb(0, 160, 160, 160); } }      // +32
        /// <summary>
        /// Defaultní barevný posun: DefaultShiftBackColorDragBegin
        /// </summary>
        protected virtual Color DefaultShiftBackColorDragBegin { get { return Color.FromArgb(0, 112, 112, 112); } }    // -16
        /// <summary>
        /// Defaultní barevný posun: DefaultShiftBackColorDragEnd
        /// </summary>
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
        /// <summary>
        /// Vrací barvu okraje modifikovanou na 3D Light
        /// </summary>
        /// <param name="borderColor"></param>
        /// <returns></returns>
        public Color GetColor3DBorderLight(Color borderColor)
        {
            return borderColor.Morph(this.Effect3DLight, this.Effect3DBorderRatio);
        }
        /// <summary>
        /// Vrací barvu okraje modifikovanou na 3D Light v daném poměru
        /// </summary>
        /// <param name="borderColor"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public Color GetColor3DBorderLight(Color borderColor, float ratio)
        {
            return borderColor.Morph(this.Effect3DLight, ratio);
        }
        /// <summary>
        /// Vrací barvu okraje modifikovanou na 3D Dark
        /// </summary>
        /// <param name="borderColor"></param>
        /// <returns></returns>
        public Color GetColor3DBorderDark(Color borderColor)
        {
            return borderColor.Morph(this.Effect3DDark, this.Effect3DBorderRatio);
        }
        /// <summary>
        /// Vrací barvu okraje modifikovanou na 3D Dark v daném poměru
        /// </summary>
        /// <param name="borderColor"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public Color GetColor3DBorderDark(Color borderColor, float ratio)
        {
            return borderColor.Morph(this.Effect3DDark, ratio);
        }
        /// <summary>
        /// Vrací barvu pozadí modifikovanou na 3D Light
        /// </summary>
        /// <param name="borderColor"></param>
        /// <returns></returns>
        public Color GetColor3DBackgroundLight(Color borderColor)
        {
            return borderColor.Morph(this.Effect3DLight, this.Effect3DBackgroundRatio);
        }
        /// <summary>
        /// Vrací barvu pozadí modifikovanou na 3D Light v daném poměru
        /// </summary>
        /// <param name="borderColor"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public Color GetColor3DBackgroundLight(Color borderColor, float ratio)
        {
            return borderColor.Morph(this.Effect3DLight, ratio);
        }
        /// <summary>
        /// Vrací barvu pozadí modifikovanou na 3D Dark
        /// </summary>
        /// <param name="borderColor"></param>
        /// <returns></returns>
        public Color GetColor3DBackgroundDark(Color borderColor)
        {
            return borderColor.Morph(this.Effect3DDark, this.Effect3DBackgroundRatio);
        }
        /// <summary>
        /// Vrací barvu pozadí modifikovanou na 3D Dark v daném poměru
        /// </summary>
        /// <param name="borderColor"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
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
        #region Public values
        /// <summary>
        /// Vnější barva
        /// </summary>
        public Color OuterColor { get { return this._Owner.GetValue(this._SkinSetKey, "OuterColor", DefaultOuterColor); } set { this._Owner.SetValue(this._SkinSetKey, "OuterColor", value); } }
        /// <summary>
        /// Vnitřní barva
        /// </summary>
        public Color InnerColor { get { return this._Owner.GetValue(this._SkinSetKey, "InnerColor", DefaultInnerColor); } set { this._Owner.SetValue(this._SkinSetKey, "InnerColor", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default Vnější barva
        /// </summary>
        protected virtual Color DefaultOuterColor { get { return Color.FromArgb(8, 192, 192, 192); } }
        /// <summary>
        /// Default Vnitřní barva
        /// </summary>
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
        #region Public values
        /// <summary>
        /// Barva pozadí Ambient
        /// </summary>
        public Color AmbientBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "AmbientBackColor", DefaultAmbientBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "AmbientBackColor", value); } }
        /// <summary>
        /// Barva pozadí Control
        /// </summary>
        public Color ControlBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ControlBackColor", DefaultControlBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ControlBackColor", value); } }
        /// <summary>
        /// Barva pozadí Focus = v době, kdy control má focus.
        /// Hodnota Alpha vyjadřuje Morph koeficient z barvy ControlBackColor / BackColor.
        /// </summary>
        public Color ControlFocusBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ControlFocusBackColor", DefaultControlFocusBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ControlFocusBackColor", value); } }
        /// <summary>
        /// Barva pozadí Active = v době, kdy control je aktivní (typicky aktivní záložka TabHeader).
        /// </summary>
        public Color ActiveBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ActiveBackColor", DefaultActiveBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ActiveBackColor", value); } }
        /// <summary>
        /// Barva textu Control
        /// </summary>
        public Color ControlTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextColor", DefaultTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextColor", value); } }
        /// <summary>
        /// Barva textu Control při Focusu
        /// </summary>
        public Color ControlTextFocusColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextFocusColor", DefaultTextFocusColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextFocusColor", value); } }
        /// <summary>
        /// Barva okraje
        /// </summary>
        public Color BorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "BorderColor", DefaultBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "BorderColor", value); } }
        /// <summary>
        /// Barva linky
        /// </summary>
        public Color LineColor { get { return this._Owner.GetValue(this._SkinSetKey, "LineColor", DefaultLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "LineColor", value); } }
        /// <summary>
        /// Intenzita 3D efektu pro Line
        /// </summary>
        public float LineEffect3D { get { return this._Owner.GetValue(this._SkinSetKey, "LineEffect3D", DefaultLineEffect3D); } set { this._Owner.SetValue(this._SkinSetKey, "LineEffect3D", value); } }
        /// <summary>
        /// Barva pozadí rámečku FrameSelect
        /// </summary>
        public Color FrameSelectBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "FrameSelectBackColor", DefaultFrameSelectBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "FrameSelectBackColor", value); } }
        /// <summary>
        /// Barva linky rámečku FrameSelect
        /// </summary>
        public Color FrameSelectLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "FrameSelectLineColor", DefaultFrameSelectLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "FrameSelectLineColor", value); } }
        /// <summary>
        /// Velikost přidaného okraje v AutoScroll containerech (vpravo a dole pod posledním prvkem)
        /// </summary>
        public int AutoScrollMargins { get { return this._Owner.GetValue(this._SkinSetKey, "AutoScrollMargins", DefaultAutoScrollMargins); } set { this._Owner.SetValue(this._SkinSetKey, "AutoScrollMargins", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default Barva pozadí Ambient
        /// </summary>
        protected virtual Color DefaultAmbientBackColor { get { return Color.Gray; } }
        /// <summary>
        /// Default Barva pozadí Control
        /// </summary>
        protected virtual Color DefaultControlBackColor { get { return Color.LightGray; } }
        /// <summary>
        /// Default Barva pozadí Focus = v době, kdy control má focus
        /// Hodnota Alpha vyjadřuje Morph koeficient z barvy ControlBackColor / BackColor.
        /// </summary>
        protected virtual Color DefaultControlFocusBackColor { get { return Color.FromArgb(64, 240, 240, 192); } }
        /// <summary>
        /// Default Barva pozadí Active
        /// </summary>
        protected virtual Color DefaultActiveBackColor { get { return Color.FromArgb(255, 216, 216, 216); } }
        /// <summary>
        /// Default Barva textu Control
        /// </summary>
        protected virtual Color DefaultTextColor { get { return Color.Black; } }
        /// <summary>
        /// Default Barva textu Control pro Focusu
        /// </summary>
        protected virtual Color DefaultTextFocusColor { get { return Color.FromArgb(0, 0, 64); } }
        /// <summary>
        /// Default Barva okraje
        /// </summary>
        protected virtual Color DefaultBorderColor { get { return Color.DimGray; } }
        /// <summary>
        /// Default Barva linky
        /// </summary>
        protected virtual Color DefaultLineColor { get { return Color.FromArgb(204, 204, 224); } }
        /// <summary>
        /// Default Intenzita 3D efektu pro Line
        /// </summary>
        protected virtual float DefaultLineEffect3D { get { return 0.25f; } }
        /// <summary>
        /// Default Barva pozadí rámečku FrameSelect
        /// </summary>
        protected virtual Color DefaultFrameSelectBackColor { get { return Color.FromArgb(48, Color.LightYellow); } }
        /// <summary>
        /// Default Barva linky rámečku FrameSelect
        /// </summary>
        protected virtual Color DefaultFrameSelectLineColor { get { return Color.DarkViolet; } }
        /// <summary>
        /// Default pro : Velikost přidaného okraje v AutoScroll containerech (vpravo a dole pod posledním prvkem)
        /// </summary>
        protected virtual int DefaultAutoScrollMargins { get { return 8; } }
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
        #region Public values
        /// <summary>
        /// Barva pro Splitter neaktivní
        /// </summary>
        public Color InactiveColor { get { return this._Owner.GetValue(this._SkinSetKey, "InactiveColor", DefaultInactiveColor); } set { this._Owner.SetValue(this._SkinSetKey, "InactiveColor", value); } }
        /// <summary>
        /// Velikost pro Splitter neaktivní
        /// </summary>
        public int InactiveSize { get { return this._Owner.GetValue(this._SkinSetKey, "InactiveSize", DefaultInactiveSize); } set { this._Owner.SetValue(this._SkinSetKey, "InactiveSize", value); } }
        /// <summary>
        /// Barva pro Splitter když je myš na parentovi
        /// </summary>
        public Color MouseOnParentColor { get { return this._Owner.GetValue(this._SkinSetKey, "MouseOnParentColor", DefaultMouseOnParentColor); } set { this._Owner.SetValue(this._SkinSetKey, "MouseOnParentColor", value); } }
        /// <summary>
        /// Velikost pro Splitter když je myš na parentovi
        /// </summary>
        public int MouseOnParentSize { get { return this._Owner.GetValue(this._SkinSetKey, "MouseOnParentSize", DefaultMouseOnParentSize); } set { this._Owner.SetValue(this._SkinSetKey, "MouseOnParentSize", value); } }
        /// <summary>
        /// Barva pro Splitter když je myš na splitteru
        /// </summary>
        public Color MouseOverColor { get { return this._Owner.GetValue(this._SkinSetKey, "MouseOverColor", DefaultMouseOverColor); } set { this._Owner.SetValue(this._SkinSetKey, "MouseOverColor", value); } }
        /// <summary>
        /// Velikost pro Splitter když je myš na splitteru
        /// </summary>
        public int MouseOverSize { get { return this._Owner.GetValue(this._SkinSetKey, "MouseOverSize", DefaultMouseOverSize); } set { this._Owner.SetValue(this._SkinSetKey, "MouseOverSize", value); } }
        /// <summary>
        /// Barva pro Splitter když je myš stisknutá (splitter se přetahuje)
        /// </summary>
        public Color MouseDownColor { get { return this._Owner.GetValue(this._SkinSetKey, "MouseDownColor", DefaultMouseDownColor); } set { this._Owner.SetValue(this._SkinSetKey, "MouseDownColor", value); } }
        /// <summary>
        /// Velikost pro Splitter když je myš stisknutá (splitter se přetahuje)
        /// </summary>
        public int MouseDownSize { get { return this._Owner.GetValue(this._SkinSetKey, "MouseDownSize", DefaultMouseDownSize); } set { this._Owner.SetValue(this._SkinSetKey, "MouseDownSize", value); } }
        /// <summary>
        /// Přesah aktivních okrajů splitteru
        /// </summary>
        public int InteractivePaddingSize { get { return this._Owner.GetValue(this._SkinSetKey, "InteractivePaddingSize", DefaultInteractivePaddingSize); } set { this._Owner.SetValue(this._SkinSetKey, "InteractivePaddingSize", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default pro: Barva pro Splitter neaktivní
        /// </summary>
        protected virtual Color DefaultInactiveColor { get { return Color.FromArgb(96, 96, 96, 64); } }
        /// <summary>
        /// Default pro: Velikost pro Splitter neaktivní
        /// </summary>
        protected virtual int DefaultInactiveSize { get { return 2; } }
        /// <summary>
        /// Default pro: Barva pro Splitter když je myš na parentovi
        /// </summary>
        protected virtual Color DefaultMouseOnParentColor { get { return Color.FromArgb(160, 96, 96, 64); } }
        /// <summary>
        /// Default pro: Velikost pro Splitter když je myš na parentovi
        /// </summary>
        protected virtual int DefaultMouseOnParentSize { get { return 3; } }
        /// <summary>
        /// Default pro: Barva pro Splitter když je myš na splitteru
        /// </summary>
        protected virtual Color DefaultMouseOverColor { get { return Color.FromArgb(220, 96, 96, 96); } }
        /// <summary>
        /// Default pro: Velikost pro Splitter když je myš na splitteru
        /// </summary>
        protected virtual int DefaultMouseOverSize { get { return 4; } }
        /// <summary>
        /// Default pro: Barva pro Splitter když je myš stisknutá (splitter se přetahuje)
        /// </summary>
        protected virtual Color DefaultMouseDownColor { get { return Color.FromArgb(255, 64, 64, 64); } }
        /// <summary>
        /// Default pro: Velikost pro Splitter když je myš stisknutá (splitter se přetahuje)
        /// </summary>
        protected virtual int DefaultMouseDownSize { get { return 4; } }
        /// <summary>
        /// Default pro: Přesah aktivních okrajů splitteru
        /// </summary>
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
        #region Public values
        /// <summary>
        /// Barva okolí = překryvná pro celý control
        /// </summary>
        public Color AreaColor { get { return this._Owner.GetValue(this._SkinSetKey, "AreaColor", DefaultAreaColor); } set { this._Owner.SetValue(this._SkinSetKey, "AreaColor", value); } }
        /// <summary>
        /// Barva pozadí pásu s textem
        /// </summary>
        public Color TextBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextBackColor", DefaultTextBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextBackColor", value); } }
        /// <summary>
        /// Barva písma titulku pásu s textem
        /// </summary>
        public Color TextTitleForeColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextTitleForeColor", DefaultTextTitleForeColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextTitleForeColor", value); } }
        /// <summary>
        /// Barva písma textu pásu s textem
        /// </summary>
        public Color TextInfoForeColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextInfoForeColor", DefaultTextInfoForeColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextInfoForeColor", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default pro: Barva okolí = překryvná pro celý control
        /// </summary>
        protected virtual Color DefaultAreaColor { get { return Color.FromArgb(144, 144, 144, 144); } }
        /// <summary>
        /// Default pro: Barva pozadí pásu s textem
        /// </summary>
        protected virtual Color DefaultTextBackColor { get { return Color.FromArgb(240, 196, 255, 255); } }
        /// <summary>
        /// Default pro: Barva písma titulku pásu s textem
        /// </summary>
        protected virtual Color DefaultTextTitleForeColor { get { return Color.Black; } }
        /// <summary>
        /// Default pro: Barva písma textu pásu s textem
        /// </summary>
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
        #region Public values
        /// <summary>
        /// Barva okraje
        /// </summary>
        public Color BorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "BorderColor", DefaultBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "BorderColor", value); } }
        /// <summary>
        /// Barva pozadí
        /// </summary>
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        /// <summary>
        /// Barva titulku
        /// </summary>
        public Color TitleColor { get { return this._Owner.GetValue(this._SkinSetKey, "TitleColor", DefaultTitleColor); } set { this._Owner.SetValue(this._SkinSetKey, "TitleColor", value); } }
        /// <summary>
        /// Barva infotextu
        /// </summary>
        public Color InfoColor { get { return this._Owner.GetValue(this._SkinSetKey, "InfoColor", DefaultInfoColor); } set { this._Owner.SetValue(this._SkinSetKey, "InfoColor", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default pro: Barva okraje
        /// </summary>
        protected virtual Color DefaultBorderColor { get { return Color.DimGray; } }
        /// <summary>
        /// Default pro: Barva pozadí
        /// </summary>
        protected virtual Color DefaultBackColor { get { return Color.FromArgb(255, 255, 240) /*Color.LightYellow*/; } }
        /// <summary>
        /// Default pro: Barva titulku
        /// </summary>
        protected virtual Color DefaultTitleColor { get { return Color.Black; } }
        /// <summary>
        /// Default pro: Barva infotextu
        /// </summary>
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
        #region Public values
        /// <summary>
        /// Barva okraje
        /// </summary>
        public Color BorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "BorderColor", DefaultBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "BorderColor", value); } }
        /// <summary>
        /// Barva pozadí
        /// </summary>
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        /// <summary>
        /// Barva písma
        /// </summary>
        public Color TextColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextColor", DefaultTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextColor", value); } }
        /// <summary>
        /// Barva pozadí prvku
        /// </summary>
        public Color ItemBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemBackColor", DefaultItemBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemBackColor", value); } }
        /// <summary>
        /// Barva pozadí označeného prvku
        /// </summary>
        public Color ItemSelectedBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemSelectedBackColor", DefaultItemSelectedBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemSelectedBackColor", value); } }
        /// <summary>
        /// Barva orámování označeného prvku
        /// </summary>
        public Color ItemSelectedLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemSelectedLineColor", DefaultItemSelectedLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemSelectedLineColor", value); } }
        /// <summary>
        /// Barva okraje běžného prvku
        /// </summary>
        public Color ItemBorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemBorderColor", DefaultItemBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemBorderColor", value); } }
        /// <summary>
        /// Barva pozadí titulku grupy
        /// </summary>
        public Color TitleBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "TitleBackColor", DefaultTitleBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "TitleBackColor", value); } }
        /// <summary>
        /// Barva oddělovače světlá
        /// </summary>
        public Color SeparatorLightColor { get { return this._Owner.GetValue(this._SkinSetKey, "SeparatorLightColor", DefaultSeparatorLightColor); } set { this._Owner.SetValue(this._SkinSetKey, "SeparatorLightColor", value); } }
        /// <summary>
        /// Barva oddělovače tmavá
        /// </summary>
        public Color SeparatorDarkColor { get { return this._Owner.GetValue(this._SkinSetKey, "SeparatorDarkColor", DefaultSeparatorDarkColor); } set { this._Owner.SetValue(this._SkinSetKey, "SeparatorDarkColor", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default pro: Barva okraje
        /// </summary>
        protected virtual Color DefaultBorderColor { get { return Color.DimGray; } }
        /// <summary>
        /// Default pro: Barva pozadí
        /// </summary>
        protected virtual Color DefaultBackColor { get { return Color.FromArgb(255, 250, 250, 250); } }
        /// <summary>
        /// Default pro: Barva písma
        /// </summary>
        protected virtual Color DefaultTextColor { get { return Color.Black; } }
        /// <summary>
        /// Default pro: Barva pozadí prvku
        /// </summary>
        protected virtual Color DefaultItemBackColor { get { return Color.FromArgb(255, 224, 224, 240); } }
        /// <summary>
        /// Default pro: Barva pozadí označeného prvku
        /// </summary>
        protected virtual Color DefaultItemSelectedBackColor { get { return Color.FromArgb(255, 240, 240, 160); } }
        /// <summary>
        /// Default pro: Barva orámování označeného prvku
        /// </summary>
        protected virtual Color DefaultItemSelectedLineColor { get { return Color.DimGray; } }
        /// <summary>
        /// Default pro: Barva okraje běžného prvku
        /// </summary>
        protected virtual Color DefaultItemBorderColor { get { return Color.DimGray; } }
        /// <summary>
        /// Default pro: Barva pozadí titulku grupy
        /// </summary>
        protected virtual Color DefaultTitleBackColor { get { return Color.FromArgb(128, 240, 224, 246); } }
        /// <summary>
        /// Default pro: Barva oddělovače světlá
        /// </summary>
        protected virtual Color DefaultSeparatorLightColor { get { return Color.LightGray; } }
        /// <summary>
        /// Default pro: Barva oddělovače tmavá
        /// </summary>
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
        #region Public values
        /// <summary>
        /// Barva pozadí
        /// </summary>
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        /// <summary>
        /// Barva pozadí prvku
        /// </summary>
        public Color ItemBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemBackColor", DefaultItemBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemBackColor", value); } }
        /// <summary>
        /// Barva pozadí vybraného prvku
        /// </summary>
        public Color ItemCheckedBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemCheckedBackColor", DefaultItemCheckedBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemCheckedBackColor", value); } }
        /// <summary>
        /// Barva pozadí prvku Vybrat vše
        /// </summary>
        public Color SelectAllItemBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "SelectAllItemBackColor", DefaultSelectAllItemBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "SelectAllItemBackColor", value); } }
        /// <summary>
        /// Barva pozadí prvku Vybrat vše pokud je vybraný
        /// </summary>
        public Color SelectAllItemCheckedBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "SelectAllItemCheckedBackColor", DefaultSelectAllItemCheckedBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "SelectAllItemCheckedBackColor", value); } }
        /// <summary>
        /// Barva okraje prvku
        /// </summary>
        public Color ItemBorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemBorderColor", DefaultItemBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemBorderColor", value); } }
        /// <summary>
        /// Barva textu prvku
        /// </summary>
        public Color ItemTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "ItemTextColor", DefaultItemTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "ItemTextColor", value); } }
        /// <summary>
        /// Prostor mezi prvky filtru
        /// </summary>
        public Size ItemSpacing { get { return this._Owner.GetValue(this._SkinSetKey, "ItemSpacing", DefaultItemSpacing); } set { this._Owner.SetValue(this._SkinSetKey, "ItemSpacing", value); } }
        /// <summary>
        /// Výška prvku
        /// </summary>
        public int ItemHeight { get { return this._Owner.GetValue(this._SkinSetKey, "ItemHeight", DefaultItemHeight); } set { this._Owner.SetValue(this._SkinSetKey, "ItemHeight", value); } }
        /// <summary>
        /// Obrázek u označeného prvku ("fajfka")
        /// </summary>
        public Image ItemSelectedImage { get { return this._Owner.GetValue(this._SkinSetKey, "ItemSelectedImage", DefaultItemSelectedImage); } set { this._Owner.SetValue(this._SkinSetKey, "ItemSelectedImage", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default pro: Barva pozadí
        /// </summary>
        protected virtual Color DefaultBackColor { get { return Color.FromArgb(255, 160, 160, 176); } }
        /// <summary>
        /// Default pro: Barva pozadí prvku
        /// </summary>
        protected virtual Color DefaultItemBackColor { get { return Color.FromArgb(255, 240, 240, 240); } }
        /// <summary>
        /// Default pro: Barva pozadí vybraného prvku
        /// </summary>
        protected virtual Color DefaultItemCheckedBackColor { get { return Color.FromArgb(255, 192, 255, 255); } }
        /// <summary>
        /// Default pro: Barva pozadí prvku Vybrat vše
        /// </summary>
        protected virtual Color DefaultSelectAllItemBackColor { get { return Color.FromArgb(255, 160, 232, 160); } }
        /// <summary>
        /// Default pro: Barva pozadí prvku Vybrat vše pokud je vybraný
        /// </summary>
        protected virtual Color DefaultSelectAllItemCheckedBackColor { get { return Color.FromArgb(255, 180, 255, 180); } }
        /// <summary>
        /// Default pro: Barva okraje prvku
        /// </summary>
        protected virtual Color DefaultItemBorderColor { get { return Color.DimGray; } }
        /// <summary>
        /// Default pro: Barva textu prvku
        /// </summary>
        protected virtual Color DefaultItemTextColor { get { return Color.Black; } }
        /// <summary>
        /// Default pro: Prostor mezi prvky filtru
        /// </summary>
        protected virtual Size DefaultItemSpacing { get { return new Size(3, 2); } }
        /// <summary>
        /// Default pro: Výška prvku
        /// </summary>
        protected virtual int DefaultItemHeight { get { return 24; } }
        /// <summary>
        /// Default pro: Obrázek u označeného prvku ("fajfka")
        /// </summary>
        protected virtual Image DefaultItemSelectedImage { get { return Application.App.ResourcesApp.GetImage(R.Images.Actions16.DialogOkApply2Png); } }
        #endregion
    }
    /// <summary>
    /// Skin set for Textbox.
    /// </summary>
    public class SkinTextboxSet : SkinSet
    {
        #region Internal and private
        internal SkinTextboxSet(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public values

        /// <summary>
        /// Barva pozadí textboxu ve stavu Disabled
        /// </summary>
        public Color BackColorDisabled { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorDisabled", DefaultBackColorDisabled); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorDisabled", value); } }
        /// <summary>
        /// Barva pozadí textboxu ve stavu Enabled
        /// </summary>
        public Color BackColorEnabled { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorEnabled", DefaultBackColorEnabled); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorEnabled", value); } }
        /// <summary>
        /// Barva pozadí textboxu ve stavu MouseOver
        /// </summary>
        public Color BackColorMouseOver { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorMouseOver", DefaultBackColorMouseOver); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorMouseOver", value); } }
        /// <summary>
        /// Barva pozadí textboxu ve stavu Active = máme focus
        /// </summary>
        public Color BackColorFocused { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorFocused", DefaultBackColorFocused); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorFocused", value); } }
        /// <summary>
        /// Barva pozadí textboxu, část SelectedText
        /// </summary>
        public Color BackColorSelectedText { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorSelectedText", DefaultBackColorSelectedText); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorSelectedText", value); } }

        /// <summary>
        /// Barva popředí (písma) textboxu ve stavu Disabled
        /// </summary>
        public Color TextColorDisabled { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorDisabled", DefaultTextColorDisabled); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorDisabled", value); } }
        /// <summary>
        /// Barva popředí (písma) textboxu ve stavu Enabled
        /// </summary>
        public Color TextColorEnabled { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorEnabled", DefaultTextColorEnabled); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorEnabled", value); } }
        /// <summary>
        /// Barva popředí (písma) textboxu ve stavu MouseOver
        /// </summary>
        public Color TextColorMouseOver { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorMouseOver", DefaultTextColorMouseOver); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorMouseOver", value); } }
        /// <summary>
        /// Barva popředí (písma) textboxu ve stavu Active = máme focus
        /// </summary>
        public Color TextColorFocused { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorFocused", DefaultTextColorFocused); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorFocused", value); } }
        /// <summary>
        /// Barva popředí (písma) textboxu, část SelectedText
        /// </summary>
        public Color TextColorSelectedText { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorSelectedText", DefaultTextColorSelectedText); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorSelectedText", value); } }

        /// <summary>
        /// Typ rámečku
        /// </summary>
        public BorderStyleType BorderStyle { get { return this._Owner.GetValue(this._SkinSetKey, "BorderStyle", DefaultBorderStyle); } set { this._Owner.SetValue(this._SkinSetKey, "BorderStyle", value); } }
        /// <summary>
        /// Vnitřní okraj mezi Border a Textem, počet pixelů.
        /// </summary>
        public int TextMargin { get { return this._Owner.GetValue(this._SkinSetKey, "TextMargin", DefaultTextMargin); } set { this._Owner.SetValue(this._SkinSetKey, "TextMargin", value); } }
        /// <summary>
        /// Barva okraje textboxu bez focusu (Enabled i Disabled, i MouseOver)
        /// </summary>
        public Color BorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "BorderColor", DefaultBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "BorderColor", value); } }
        /// <summary>
        /// Barva okraje textboxu s focusem (Focus, MouseDown)
        /// </summary>
        public Color BorderColorFocused { get { return this._Owner.GetValue(this._SkinSetKey, "BorderColorFocused", DefaultBorderColorFocused); } set { this._Owner.SetValue(this._SkinSetKey, "BorderColorFocused", value); } }
        /// <summary>
        /// Barva okraje textboxu Required. Hodnota Alpha kanálu určuje Morphing hodnotu z běžné barvy: 0=pro Required textbox se bude Border kreslit beze změny, 255=bude vždy použita čistá barva <see cref="BorderColorRequired"/>.
        /// Vhodná hodnota je 128 - 192, kdy Border částečně reaguje na Focus (přebírá například barvu <see cref="BorderColorFocused"/>).
        /// </summary>
        public Color BorderColorRequired { get { return this._Owner.GetValue(this._SkinSetKey, "BorderColorRequired", DefaultBorderColorRequired); } set { this._Owner.SetValue(this._SkinSetKey, "BorderColorRequired", value); } }
        /// <summary>
        /// Barva okraje textboxu v Soft verzi.. Hodnota Alpha kanálu určuje Morphing hodnotu z běžné barvy: 0=pro Required textbox se bude Border kreslit beze změny, 255=bude vždy použita čistá barva <see cref="BorderColorSoft"/>.
        /// Vhodná hodnota je 128 - 192, kdy Border částečně reaguje na Focus (přebírá například barvu <see cref="BorderColorFocused"/>).
        /// </summary>
        public Color BorderColorSoft { get { return this._Owner.GetValue(this._SkinSetKey, "BorderColorSoft", DefaultBorderColorSoft); } set { this._Owner.SetValue(this._SkinSetKey, "BorderColorSoft", value); } }
        /// <summary>
        /// Hodnota průhlednosti rámečku ve verzi Soft.
        /// </summary>
        public float BorderAlphaSoft { get { return this._Owner.GetValue(this._SkinSetKey, "BorderAlphaSoft ", DefaultBorderAlphaSoft); } set { this._Owner.SetValue(this._SkinSetKey, "BorderAlphaSoft ", value); } }
        /// <summary>
        /// Barva kurzoru
        /// </summary>
        public Color CursorColor { get { return this._Owner.GetValue(this._SkinSetKey, "CursorColor", DefaultCursorColor); } set { this._Owner.SetValue(this._SkinSetKey, "CursorColor", value); } }
        /// <summary>
        /// Ikona tlačítka vztahu na záznam
        /// </summary>
        public Image IconRelationRecord { get { return this._Owner.GetValue(this._SkinSetKey, "IconRelationRecord", DefaultIconRelationRecord); } set { this._Owner.SetValue(this._SkinSetKey, "IconRelationRecord", value); } }
        /// <summary>
        /// Ikona tlačítka vztahu na dokument
        /// </summary>
        public Image IconRelationDocument { get { return this._Owner.GetValue(this._SkinSetKey, "IconRelationDocument", DefaultIconRelationDocument); } set { this._Owner.SetValue(this._SkinSetKey, "IconRelationDocument", value); } }

        /// <summary>
        /// Ikona tlačítka Otevření složky
        /// </summary>
        public Image IconOpenFolder { get { return this._Owner.GetValue(this._SkinSetKey, "IconOpenFolder", DefaultIconOpenFolder); } set { this._Owner.SetValue(this._SkinSetKey, "IconOpenFolder", value); } }
        /// <summary>
        /// Ikona tlačítka Kalkulačka
        /// </summary>
        public Image IconCalculator { get { return this._Owner.GetValue(this._SkinSetKey, "IconCalculator", DefaultIconCalculator); } set { this._Owner.SetValue(this._SkinSetKey, "IconCalculator", value); } }
        /// <summary>
        /// Ikona tlačítka Kalendář
        /// </summary>
        public Image IconCalendar { get { return this._Owner.GetValue(this._SkinSetKey, "IconCalendar", DefaultIconCalendar); } set { this._Owner.SetValue(this._SkinSetKey, "IconCalendar", value); } }
        /// <summary>
        /// Ikona tlačítka DropDown
        /// </summary>
        public Image IconDropDown { get { return this._Owner.GetValue(this._SkinSetKey, "IconDropDown", DefaultIconDropDown); } set { this._Owner.SetValue(this._SkinSetKey, "IconDropDown", value); } }

        



        #endregion
        #region Default values
        /// <summary>
        /// Default pro: Barva pozadí textboxu ve stavu Disabled
        /// </summary>
        protected virtual Color DefaultBackColorDisabled { get { return Color.FromArgb(210, 210, 210); } }
        /// <summary>
        /// Default pro: Barva pozadí textboxu ve stavu Enabled
        /// </summary>
        protected virtual Color DefaultBackColorEnabled { get { return Color.FromArgb(240, 240, 240); } }
        /// <summary>
        /// Default pro: Barva pozadí textboxu ve stavu MouseOver
        /// </summary>
        protected virtual Color DefaultBackColorMouseOver { get { return Color.FromArgb(240, 240, 255); } }
        /// <summary>
        /// Default pro: Barva pozadí textboxu ve stavu Active = máme focus
        /// </summary>
        protected virtual Color DefaultBackColorFocused { get { return Color.FromArgb(255, 255, 212); } }
        /// <summary>
        /// Default pro: Barva pozadí textboxu, část SelectedText
        /// </summary>
        protected virtual Color DefaultBackColorSelectedText { get { return Color.FromArgb(0, 120, 215); } }
        /// <summary>
        /// Default pro: Barva popředí (písma) textboxu ve stavu Disabled
        /// </summary>
        protected virtual Color DefaultTextColorDisabled { get { return Color.Black; } }
        /// <summary>
        /// Default pro: Barva popředí (písma) textboxu ve stavu Enabled
        /// </summary>
        protected virtual Color DefaultTextColorEnabled { get { return Color.Black; } }
        /// <summary>
        /// Default pro: Barva popředí (písma) textboxu ve stavu MouseOver
        /// </summary>
        protected virtual Color DefaultTextColorMouseOver { get { return Color.Black; } }
        /// <summary>
        /// Default pro: Barva popředí (písma) textboxu ve stavu Active = máme focus
        /// </summary>
        protected virtual Color DefaultTextColorFocused { get { return Color.Black; } }
        /// <summary>
        /// Default pro: Barva popředí (písma) textboxu, část SelectedText
        /// </summary>
        protected virtual Color DefaultTextColorSelectedText { get { return Color.White; } }
        /// <summary>
        /// Default pro: Styl rámečku
        /// </summary>
        protected virtual BorderStyleType DefaultBorderStyle { get { return BorderStyleType.Effect3D; } }
        /// <summary>
        /// Default pro: Vnitřní okraj mezi Border a Textem, počet pixelů.
        /// </summary>
        protected virtual int DefaultTextMargin { get { return 2; } }
        /// <summary>
        /// Default pro: Barva okraje textboxu bez focusu (Enabled i Disabled, i MouseOver)
        /// </summary>
        protected virtual Color DefaultBorderColor { get { return Color.FromArgb(128, 128, 128); } }
        /// <summary>
        /// Default pro: Barva okraje textboxu s focusem (Focus, MouseDown)
        /// </summary>
        protected virtual Color DefaultBorderColorFocused { get { return Color.FromArgb(32, 32, 32); } }
        /// <summary>
        /// Default pro: Barva okraje textboxu Required
        /// </summary>
        protected virtual Color DefaultBorderColorRequired { get { return Color.FromArgb(192, 192, 64, 64); } }
        /// <summary>
        /// Default pro: Barva okraje textboxu Soft verze
        /// </summary>
        protected virtual Color DefaultBorderColorSoft { get { return Color.FromArgb(192, 150, 150, 150); } }
        /// <summary>
        /// Default pro: Hodnota průhlednosti rámečku ve verzi Soft.
        /// </summary>
        protected virtual float DefaultBorderAlphaSoft { get { return 0.75f; } }
        /// <summary>
        /// Default pro: Barva kurzoru
        /// </summary>
        protected virtual Color DefaultCursorColor { get { return Color.FromArgb(192, 32, 16, 0); } }
        /// <summary>
        /// Default pro: Ikona tlačítka vztahu na záznam
        /// </summary>
        protected virtual Image DefaultIconRelationRecord { get { return StandardIcons.RelationRecord;; } }
        /// <summary>
        /// Default pro: Ikona tlačítka vztahu na dokument
        /// </summary>
        protected virtual Image DefaultIconRelationDocument { get { return StandardIcons.RelationDocument; ; } }
        /// <summary>
        /// Default pro: Ikona tlačítka Otevření složky
        /// </summary>
        protected virtual Image DefaultIconOpenFolder { get { return StandardIcons.OpenFolder; ; } }
        /// <summary>
        /// Default pro: Ikona tlačítka Kalkulačka
        /// </summary>
        protected virtual Image DefaultIconCalculator { get { return StandardIcons.Calculator; ; } }
        /// <summary>
        /// Default pro: Ikona tlačítka Kalendář
        /// </summary>
        protected virtual Image DefaultIconCalendar { get { return StandardIcons.Calendar; ; } }
        /// <summary>
        /// Default pro: Ikona tlačítka DropDown
        /// </summary>
        protected virtual Image DefaultIconDropDown { get { return StandardIcons.DropDown; ; } }
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
        #region Public values
        /// <summary>
        /// Barva okraje buttonu
        /// </summary>
        public Color BorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "BorderColor", DefaultBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "BorderColor", value); } }
        /// <summary>
        /// Barva pozadí buttonu
        /// </summary>
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        /// <summary>
        /// Barva textu buttonu
        /// </summary>
        public Color TextColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextColor", DefaultTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextColor", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default pro: Barva okraje buttonu
        /// </summary>
        protected virtual Color DefaultBorderColor { get { return Color.DimGray; } }
        /// <summary>
        /// Default pro: Barva pozadí buttonu
        /// </summary>
        protected virtual Color DefaultBackColor { get { return Color.FromArgb(255, 216, 216, 216); } }
        /// <summary>
        /// Default pro: Barva textu buttonu
        /// </summary>
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
        #region Public values
        /// <summary>
        /// Výška Tabu
        /// </summary>
        public int HeaderHeight { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderHeight", DefaultHeaderHeight); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderHeight", value); } }
        /// <summary>
        /// Barva prázdného prostoru
        /// </summary>
        public Color SpaceColor { get { return this._Owner.GetValue(this._SkinSetKey, "SpaceColor", DefaultSpaceColor); } set { this._Owner.SetValue(this._SkinSetKey, "SpaceColor", value); } }
        /// <summary>
        /// Barva okrajů
        /// </summary>
        public Color BorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "BorderColor", DefaultBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "BorderColor", value); } }
        /// <summary>
        /// Barva pozadí Tabu
        /// </summary>
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        /// <summary>
        /// Barva textu
        /// </summary>
        public Color TextColor { get { return this._Owner.GetValue(this._SkinSetKey, "TextColor", DefaultTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextColor", value); } }
        /// <summary>
        /// Barva čáry podtržení u aktivního Tabu
        /// </summary>
        public Color LineColorActive { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorActive", DefaultLineColorActive); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorActive", value); } }
        /// <summary>
        /// Barva pozadí u aktivního Tabu
        /// </summary>
        public Color BackColorActive { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorActive", DefaultBackColorActive); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorActive", value); } }
        /// <summary>
        /// Barva linky u HotMouse Tabu
        /// </summary>
        public Color LineColorHot { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorHot", DefaultLineColorHot); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorHot", value); } }
        /// <summary>
        /// Barva textu u aktivního Tabu
        /// </summary>
        public Color TextColorActive { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorActive", DefaultTextColorActive); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorActive", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default pro: Výška Tabu
        /// </summary>
        protected virtual int DefaultHeaderHeight { get { return 28; } }
        /// <summary>
        /// Default pro: Barva prázdného prostoru
        /// </summary>
        protected virtual Color DefaultSpaceColor { get { return Skin.Control.AmbientBackColor; } }
        /// <summary>
        /// Barva okrajů
        /// </summary>
        protected virtual Color DefaultBorderColor { get { return Skin.Control.BorderColor; } }
        /// <summary>
        /// Default pro: Barva pozadí Tabu
        /// </summary>
        protected virtual Color DefaultBackColor { get { return Skin.Control.ControlBackColor; } }
        /// <summary>
        /// Default pro: Barva textu
        /// </summary>
        protected virtual Color DefaultTextColor { get { return Skin.Control.ControlTextColor; } }
        /// <summary>
        /// Default pro: Barva čáry podtržení u aktivního Tabu
        /// </summary>
        protected virtual Color DefaultLineColorActive { get { return Color.FromArgb(255, 255, 255, 128); } }
        /// <summary>
        /// Default pro: Barva pozadí u aktivního Tabu
        /// </summary>
        protected virtual Color DefaultBackColorActive { get { return Skin.Control.ActiveBackColor; } }
        /// <summary>
        /// Default pro: Barva čáry podtržení u aktivního Tabu
        /// </summary>
        protected virtual Color DefaultLineColorHot { get { return Color.FromArgb(255, 216, 216, 128); } }
        /// <summary>
        /// Default pro: Barva textu u aktivního Tabu
        /// </summary>
        protected virtual Color DefaultTextColorActive { get { return Skin.Control.ControlTextColor; } }
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
        #region Public values
        /// <summary>
        /// Šířka ScrollBaru (šířka u svislého, výška u vodorovného)
        /// </summary>
        public int ScrollThick { get { return this._Owner.GetValue(this._SkinSetKey, "ScrollThick", DefaultScrollThick); } set { this._Owner.SetValue(this._SkinSetKey, "ScrollThick", value); } }
        /// <summary>
        /// Malý krok na scrollbaru (standardní), poměrně k velikosti viditelné plochy
        /// </summary>
        public decimal SmallStepRatio { get { return this._Owner.GetValue(this._SkinSetKey, "SmallStepRatio", DefaultSmallStepRatio); } set { this._Owner.SetValue(this._SkinSetKey, "SmallStepRatio", value); } }
        /// <summary>
        /// Velký krok na scrollbaru (zvětšený vlivem Shift), poměrně k velikosti viditelné plochy
        /// </summary>
        public decimal BigStepRatio { get { return this._Owner.GetValue(this._SkinSetKey, "BigStepRatio", DefaultBigStepRatio); } set { this._Owner.SetValue(this._SkinSetKey, "BigStepRatio", value); } }
        /// <summary>
        /// Barva pozadí scrollbaru
        /// </summary>
        public Color BackColorArea { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorArea", DefaultBackColorArea); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorArea", value); } }
        /// <summary>
        /// Barva pozadí buttonů na scrollbaru - pasivní stav (bez myši)
        /// </summary>
        public Color BackColorButtonPassive { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorButtonPassive", DefaultBackColorButtonPassive); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorButtonPassive", value); } }
        /// <summary>
        /// Barva pozadí buttonů na scrollbaru - aktivní stav (MouseOver, MouseDown)
        /// </summary>
        public Color BackColorButtonActive { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorButtonActive", DefaultBackColorButtonActive); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorButtonActive", value); } }
        /// <summary>
        /// Barva textu - značky, ikonky na scrollbaru
        /// </summary>
        public Color TextColorButton { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorButton", DefaultTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorButton", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default pro: Šířka ScrollBaru (šířka u svislého, výška u vodorovného)
        /// </summary>
        protected virtual int DefaultScrollThick { get { return 14; } }
        /// <summary>
        /// Default pro: Malý krok na scrollbaru (standardní), poměrně k velikosti viditelné plochy
        /// </summary>
        protected virtual decimal DefaultSmallStepRatio { get { return 0.10m; } }
        /// <summary>
        /// Default pro: Velký krok na scrollbaru (zvětšený vlivem Shift), poměrně k velikosti viditelné plochy
        /// </summary>
        protected virtual decimal DefaultBigStepRatio { get { return 0.60m; } }
        /// <summary>
        /// Default pro: Barva pozadí scrollbaru
        /// </summary>
        protected virtual Color DefaultBackColorArea { get { return Color.FromArgb(255, 160, 160, 176); } }
        /// <summary>
        /// Default pro: Barva pozadí buttonů na scrollbaru - pasivní stav (bez myši)
        /// </summary>
        protected virtual Color DefaultBackColorButtonPassive { get { return Color.FromArgb(255, 192, 192, 192); } }
        /// <summary>
        /// Default pro: Barva pozadí buttonů na scrollbaru - aktivní stav (MouseOver, MouseDown)
        /// </summary>
        protected virtual Color DefaultBackColorButtonActive { get { return Color.FromArgb(255, 216, 216, 216); } }
        /// <summary>
        /// Default pro: Barva textu - značky, ikonky na scrollbaru
        /// </summary>
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
        #region Public values
        /// <summary>
        /// Barva pozadí
        /// </summary>
        public Color BackColorTrack { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorTrack", DefaultBackColorTrack); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorTrack", value); } }
        /// <summary>
        /// Barva linky Track = spojnice celé čáry
        /// </summary>
        public Color LineColorTrack { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorTrack", DefaultLineColorTrack); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorTrack", value); } }
        /// <summary>
        /// Barva ticku Track = značka vzdálenosti
        /// </summary>
        public Color LineColorTick { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorTick", DefaultLineColorTick); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorTick", value); } }
        /// <summary>
        /// Barva pozadí buttonu normální
        /// </summary>
        public Color BackColorButton { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorButton", DefaultBackColorButton); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorButton", value); } }
        /// <summary>
        /// Barva pozadí buttonu MouseHot
        /// </summary>
        public Color BackColorMouseOverButton { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorMouseOverButton", DefaultBackColorMouseOverButton); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorMouseOverButton", value); } }
        /// <summary>
        /// Barva pozadí buttonu MouseDown
        /// </summary>
        public Color BackColorMouseDownButton { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorMouseDownButton", DefaultBackColorMouseDownButton); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorMouseDownButton", value); } }
        /// <summary>
        /// Barva linek buttonu
        /// </summary>
        public Color LineColorButton { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorButton", DefaultLineColorButton); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorButton", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default pro: Barva pozadí
        /// </summary>
        protected virtual Color DefaultBackColorTrack { get { return Color.FromArgb(255, 180, 180, 192); } }
        /// <summary>
        /// Default pro: Barva linky Track = spojnice celé čáry
        /// </summary>
        protected virtual Color DefaultLineColorTrack { get { return Color.FromArgb(255, 64, 64, 64); } }
        /// <summary>
        /// Default pro: Barva ticku Track = značka vzdálenosti
        /// </summary>
        protected virtual Color DefaultLineColorTick { get { return Color.FromArgb(255, 160, 160, 168); } }
        /// <summary>
        /// Default pro: Barva pozadí buttonu normální
        /// </summary>
        protected virtual Color DefaultBackColorButton { get { return Color.FromArgb(255, 224, 224, 240); } }
        /// <summary>
        /// Default pro: Barva pozadí buttonu MouseHot
        /// </summary>
        protected virtual Color DefaultBackColorMouseOverButton { get { return Color.FromArgb(255, 232, 232, 224); } }
        /// <summary>
        /// Default pro: Barva pozadí buttonu MouseDown
        /// </summary>
        protected virtual Color DefaultBackColorMouseDownButton { get { return Color.FromArgb(255, 200, 200, 180); } }
        /// <summary>
        /// Default pro: Barva linek buttonu
        /// </summary>
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
        #region Public values
        /// <summary>
        /// Barva pozadí okna Progress
        /// </summary>
        public Color BackColorWindow { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorWindow", DefaultBackColorWindow); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorWindow", value); } }
        /// <summary>
        /// Barva textů okna Progress
        /// </summary>
        public Color TextColorWindow { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorWindow", DefaultTextColorWindow); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorWindow", value); } }
        /// <summary>
        /// Barva pozadí linky Progress
        /// </summary>
        public Color BackColorProgress { get { return this._Owner.GetValue(this._SkinSetKey, "BackColorProgress", DefaultBackColorProgress); } set { this._Owner.SetValue(this._SkinSetKey, "BackColorProgress", value); } }
        /// <summary>
        /// Barva dat (již zpracovaný poměr) linky Progress
        /// </summary>
        public Color DataColorProgress { get { return this._Owner.GetValue(this._SkinSetKey, "DataColorProgress", DefaultDataColorProgress); } set { this._Owner.SetValue(this._SkinSetKey, "DataColorProgress", value); } }
        /// <summary>
        /// Barva textu v lince Progress
        /// </summary>
        public Color TextColorProgress { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorProgress", DefaultTextColorProgress); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorProgress", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default pro: Barva pozadí okna Progress
        /// </summary>
        protected virtual Color DefaultBackColorWindow { get { return Color.FromArgb(255, 64, 128, 128); } }
        /// <summary>
        /// Default pro: Barva textů okna Progress
        /// </summary>
        protected virtual Color DefaultTextColorWindow { get { return Color.FromArgb(255, 255, 255, 255); } }
        /// <summary>
        /// Default pro: Barva pozadí linky Progress
        /// </summary>
        protected virtual Color DefaultBackColorProgress { get { return Color.FromArgb(255, 240, 240, 240); } }
        /// <summary>
        /// Default pro: Barva dat (již zpracovaný poměr) linky Progress
        /// </summary>
        protected virtual Color DefaultDataColorProgress { get { return Color.FromArgb(255, 160, 255, 160); } }
        /// <summary>
        /// Default pro: Barva textu v lince Progress
        /// </summary>
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
        #region Public values
        /// <summary>
        /// Barva pozadí osy
        /// </summary>
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        /// <summary>
        /// Barva textu pro label Big
        /// </summary>
        public Color TextColorLabelBig { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorLabelBig", DefaultTextColorLabelBig); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorLabelBig", value); } }
        /// <summary>
        /// Barva textu pro label Standard
        /// </summary>
        public Color TextColorLabelStandard { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorLabelStandard", DefaultTextColorLabelStandard); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorLabelStandard", value); } }
        /// <summary>
        /// Barva ticku pro label Big
        /// </summary>
        public Color LineColorTickBig { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorTickBig", DefaultLineColorTickBig); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorTickBig", value); } }
        /// <summary>
        /// Barva ticku pro label Standard
        /// </summary>
        public Color LineColorTickStandard { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorTickStandard", DefaultLineColorTickStandard); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorTickStandard", value); } }
        /// <summary>
        /// Barva textu pro popisek Arrangement (uprostřed, zobrazuje typ arrangementu osy)
        /// </summary>
        public Color TextColorArrangement { get { return this._Owner.GetValue(this._SkinSetKey, "TextColorArrangement", DefaultTextColorArrangement); } set { this._Owner.SetValue(this._SkinSetKey, "TextColorArrangement", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default pro: Barva pozadí osy
        /// </summary>
        protected virtual Color DefaultBackColor { get { return Color.FromArgb(192, Color.LightSkyBlue); } }
        /// <summary>
        /// Default pro: Barva textu pro label Big
        /// </summary>
        protected virtual Color DefaultTextColorLabelBig { get { return Color.Black; } }
        /// <summary>
        /// Default pro: Barva textu pro label Standard
        /// </summary>
        protected virtual Color DefaultTextColorLabelStandard { get { return Color.DimGray; } }
        /// <summary>
        /// Default pro: Barva ticku pro label Big
        /// </summary>
        protected virtual Color DefaultLineColorTickBig { get { return Color.Black; } }
        /// <summary>
        /// Default pro: Barva ticku pro label Standard
        /// </summary>
        protected virtual Color DefaultLineColorTickStandard { get { return Color.Gray; } }
        /// <summary>
        /// Default pro: Barva textu pro popisek Arrangement (uprostřed, zobrazuje typ arrangementu osy)
        /// </summary>
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
        #region Public values
        /// <summary>
        /// Grid, Záhlaví: barva pozadí
        /// </summary>
        public Color HeaderBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderBackColor", DefaultHeaderBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderBackColor", value); } }
        /// <summary>
        /// Grid, Záhlaví: barva textu
        /// </summary>
        public Color HeaderTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderTextColor", DefaultHeaderTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderTextColor", value); } }
        /// <summary>
        /// Grid, Záhlaví: barva linky
        /// </summary>
        public Color HeaderLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderLineColor", DefaultHeaderLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderLineColor", value); } }
        /// <summary>
        /// Grid, Záhlaví: barva levé svislé linky
        /// </summary>
        public Color HeaderLineLeftVerticalColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderLineLeftVerticalColor", DefaultHeaderLineLeftVerticalColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderLineLeftVerticalColor", value); } }
        /// <summary>
        /// Grid, Záhlaví: barva pravé svislé linky
        /// </summary>
        public Color HeaderLineRightVerticalColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderLineRightVerticalColor", DefaultHeaderLineRightVerticalColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderLineRightVerticalColor", value); } }
        /// <summary>
        /// Grid, Záhlaví: barva vodorovné linky
        /// </summary>
        public Color HeaderLineHorizontalColor { get { return this._Owner.GetValue(this._SkinSetKey, "HeaderLineHorizontalColor", DefaultHeaderLineHorizontalColor); } set { this._Owner.SetValue(this._SkinSetKey, "HeaderLineHorizontalColor", value); } }
        /// <summary>
        /// Grid: barva pozadí tabulky
        /// </summary>
        public Color TableBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "TableBackColor", DefaultTableBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "TableBackColor", value); } }
        /// <summary>
        /// Grid: barva pozadí řádku
        /// </summary>
        public Color RowBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "RowBackColor", DefaultRowBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "RowBackColor", value); } }
        /// <summary>
        /// Grid: barva pozadí Child řádku
        /// </summary>
        public Color RowChildBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "RowChildBackColor", DefaultRowChildBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "RowChildBackColor", value); } }
        /// <summary>
        /// Grid: barva textu v řádku
        /// </summary>
        public Color RowTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "RowTextColor", DefaultRowTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "RowTextColor", value); } }
        /// <summary>
        /// Grid: barva pozadí Selected řádku
        /// </summary>
        public Color SelectedRowBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "SelectedRowBackColor", DefaultSelectedRowBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "SelectedRowBackColor", value); } }
        /// <summary>
        /// Grid: barva pozadí Selected Child řádku
        /// </summary>
        public Color SelectedRowChildBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "SelectedRowChildBackColor", DefaultSelectedRowChildBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "SelectedRowChildBackColor", value); } }
        /// <summary>
        /// Grid: barva textu Selected řádku
        /// </summary>
        public Color SelectedRowTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "SelectedRowTextColor", DefaultSelectedRowTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "SelectedRowTextColor", value); } }
        /// <summary>
        /// Grid: barva pozadí Aktivní buňky
        /// </summary>
        public Color ActiveCellBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ActiveCellBackColor;", DefaultActiveCellBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ActiveCellBackColor;", value); } }
        /// <summary>
        /// Grid: barva textu Aktivní buňky
        /// </summary>
        public Color ActiveCellTextColor { get { return this._Owner.GetValue(this._SkinSetKey, "ActiveCellTextColor ", DefaultActiveCellTextColor); } set { this._Owner.SetValue(this._SkinSetKey, "ActiveCellTextColor ", value); } }
        /// <summary>
        /// Grid: barva border linky
        /// </summary>
        public Color BorderLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "BorderLineColor", DefaultBorderLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "BorderLineColor", value); } }
        /// <summary>
        /// Grid: ikona třídění ASC
        /// </summary>
        public Image SortAscendingImage { get { return this._Owner.GetValue(this._SkinSetKey, "SortAscendingImage", DefaultSortAscendingImage); } set { this._Owner.SetValue(this._SkinSetKey, "SortAscendingImage", value); } }
        /// <summary>
        /// Grid: ikona třídění DESC
        /// </summary>
        public Image SortDescendingImage { get { return this._Owner.GetValue(this._SkinSetKey, "SortDescendingImage", DefaultSortDescendingImage); } set { this._Owner.SetValue(this._SkinSetKey, "SortDescendingImage", value); } }
        /// <summary>
        /// Grid: ikona Checked v řádku
        /// </summary>
        public Image RowCheckedImage { get { return this._Owner.GetValue(this._SkinSetKey, "RowCheckedImage", DefaultRowCheckedImage); } set { this._Owner.SetValue(this._SkinSetKey, "RowCheckedImage", value); } }
        /// <summary>
        /// Grid: ikona NonChecked v řádku v tabulce, která podporue Checked
        /// </summary>
        public Image RowNotCheckedImage { get { return this._Owner.GetValue(this._SkinSetKey, "RowNotCheckedImage", DefaultRowNotCheckedImage); } set { this._Owner.SetValue(this._SkinSetKey, "RowNotCheckedImage", value); } }
        /// <summary>
        /// Grid: ikona DeselectAll v záhlaví tabulky
        /// </summary>
        public Image RowHeaderDeselectedAllImage { get { return this._Owner.GetValue(this._SkinSetKey, "RowHeaderDeselectedAllImage", DefaultRowHeaderDeselectedAllImage); } set { this._Owner.SetValue(this._SkinSetKey, "RowHeaderDeselectedAllImage", value); } }
        /// <summary>
        /// Grid: ikona ExpandAll v záhlaví tabulky
        /// </summary>
        public Image RowHeaderExpandAllImage { get { return this._Owner.GetValue(this._SkinSetKey, "RowHeaderExpandAllImage", DefaultRowHeaderExpandAllImage); } set { this._Owner.SetValue(this._SkinSetKey, "RowHeaderExpandAllImage", value); } }
        /// <summary>
        /// Grid: ikona CollapseAll v záhlaví tabulky
        /// </summary>
        public Image RowHeaderCollapseAllImage { get { return this._Owner.GetValue(this._SkinSetKey, "RowHeaderCollapseAllImage", DefaultRowHeaderCollapseAllImage); } set { this._Owner.SetValue(this._SkinSetKey, "RowHeaderCollapseAllImage", value); } }
        /// <summary>
        /// Grid: barva linke TreeView
        /// </summary>
        public Color TreeViewLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "TreeViewLineColor", DefaultTreeViewLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "TreeViewLineColor", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default pro: Grid, Záhlaví: barva pozadí
        /// </summary>
        protected virtual Color DefaultHeaderBackColor { get { return Color.LightSkyBlue.Morph(Color.White, 0.33f); } }
        /// <summary>
        /// Default pro: Grid, Záhlaví: barva textu
        /// </summary>
        protected virtual Color DefaultHeaderTextColor { get { return Color.Black; } }
        /// <summary>
        /// Default pro: Grid, Záhlaví: barva linky
        /// </summary>
        protected virtual Color DefaultHeaderLineColor { get { return Color.DimGray; } }
        /// <summary>
        /// Default pro: Grid, Záhlaví: barva levé svislé linky
        /// </summary>
        protected virtual Color DefaultHeaderLineLeftVerticalColor { get { return Color.Gray; } }
        /// <summary>
        /// Default pro: Grid, Záhlaví: barva pravé svislé linky
        /// </summary>
        protected virtual Color DefaultHeaderLineRightVerticalColor { get { return Color.LightGray; } }
        /// <summary>
        /// Default pro: Grid, Záhlaví: barva vodorovné linky
        /// </summary>
        protected virtual Color DefaultHeaderLineHorizontalColor { get { return Color.LightGray; } }
        /// <summary>
        /// Default pro: Grid: barva pozadí tabulky
        /// </summary>
        protected virtual Color DefaultTableBackColor { get { return Skin.Control.AmbientBackColor; } }
        /// <summary>
        /// Default pro: Grid: barva pozadí řádku
        /// </summary>
        protected virtual Color DefaultRowBackColor { get { return Color.White; } }
        /// <summary>
        /// Default pro: Grid: barva pozadí Child řádku
        /// </summary>
        protected virtual Color DefaultRowChildBackColor { get { return Color.FromArgb(215, 230, 220); } }
        /// <summary>
        /// Default pro: Grid: barva textu v řádku
        /// </summary>
        protected virtual Color DefaultRowTextColor { get { return Color.Black; } }
        /// <summary>
        /// Default pro: Grid: barva pozadí Selected řádku
        /// </summary>
        protected virtual Color DefaultSelectedRowBackColor { get { return Color.White.Morph(Color.CadetBlue, 0.25f); } }
        /// <summary>
        /// Default pro: Grid: barva pozadí Selected Child řádku
        /// </summary>
        protected virtual Color DefaultSelectedRowChildBackColor { get { return Color.FromArgb(185, 210, 195); } }
        /// <summary>
        /// Default pro: Grid: barva textu Selected řádku
        /// </summary>
        protected virtual Color DefaultSelectedRowTextColor { get { return Color.Black; } }
        /// <summary>
        /// Default pro: Grid: barva pozadí Aktivní buňky
        /// </summary>
        protected virtual Color DefaultActiveCellBackColor { get { return Color.White.Morph(Color.CadetBlue, 0.25f); } }
        /// <summary>
        /// Default pro: Grid: barva textu Aktivní buňky
        /// </summary>
        protected virtual Color DefaultActiveCellTextColor { get { return Color.Black; } }
        /// <summary>
        /// Default pro: Grid: barva border linky
        /// </summary>
        protected virtual Color DefaultBorderLineColor { get { return Color.Gray; } }
        /// <summary>
        /// Default pro: Grid: ikona třídění ASC
        /// </summary>
        protected virtual Image DefaultSortAscendingImage { get { return Application.App.ResourcesApp.GetImage(R.Images.Actions16.GoUp2Png); } }          // { get { return IconStandard.SortAsc; } }
        /// <summary>
        /// Default pro: Grid: ikona třídění DESC
        /// </summary>
        protected virtual Image DefaultSortDescendingImage { get { return Application.App.ResourcesApp.GetImage(R.Images.Actions16.GoDown2Png); } }       // { get { return IconStandard.SortDesc; } }
        /// <summary>
        /// Default pro: Grid: ikona Checked v řádku
        /// </summary>
        protected virtual Image DefaultRowCheckedImage { get { return Application.App.ResourcesApp.GetImage(R.Images.Actions16.DialogAccept2Png); } }    // { get { return IconStandard.RowSelected; } }
        /// <summary>
        /// Default pro: Grid: ikona NonChecked v řádku v tabulce, která podporue Checked
        /// </summary>
        protected virtual Image DefaultRowNotCheckedImage { get { return Application.App.ResourcesApp.GetImage(R.Images.Actions16.DialogNotAccept2Png); } }
        /// <summary>
        /// Default pro: Grid: ikona DeselectAll v záhlaví tabulky
        /// </summary>
        protected virtual Image DefaultRowHeaderDeselectedAllImage { get { return Application.App.ResourcesApp.GetImage(R.Images.Actions16.DialogClose2Png); } }
        /// <summary>
        /// Default pro: Grid: ikona ExpandAll v záhlaví tabulky
        /// </summary>
        protected virtual Image DefaultRowHeaderExpandAllImage { get { return Application.App.ResourcesApp.GetImage(R.Images.Actions16.ArrowRightDouble2Png); } }
        /// <summary>
        /// Default pro: Grid: ikona CollapseAll v záhlaví tabulky
        /// </summary>
        protected virtual Image DefaultRowHeaderCollapseAllImage { get { return Application.App.ResourcesApp.GetImage(R.Images.Actions16.ArrowLeftDouble2Png); } }
        /// <summary>
        /// Default pro: Grid: barva linke TreeView
        /// </summary>
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
        #region Public values
        /// <summary>
        /// Graf: výška linky
        /// </summary>
        public Int32 LineHeight { get { return this._Owner.GetValue(this._SkinSetKey, "LineHeight", DefaultLineHeight); } set { this._Owner.SetValue(this._SkinSetKey, "LineHeight", value); } }
        /// <summary>
        /// Graf: celková výška Min
        /// </summary>
        public Int32 TotalHeightMin { get { return this._Owner.GetValue(this._SkinSetKey, "TotalHeightMin", DefaultTotalHeightMin); } set { this._Owner.SetValue(this._SkinSetKey, "TotalHeightMin", value); } }
        /// <summary>
        /// Graf: celková výška Max
        /// </summary>
        public Int32 TotalHeightMax { get { return this._Owner.GetValue(this._SkinSetKey, "TotalHeightMax", DefaultTotalHeightMax); } set { this._Owner.SetValue(this._SkinSetKey, "TotalHeightMax", value); } }
        /// <summary>
        /// Graf: element - barva pozadí
        /// </summary>
        public Color ElementBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ElementBackColor", DefaultElementBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ElementBackColor", value); } }
        /// <summary>
        /// Graf: element - barva okraje
        /// </summary>
        public Color ElementBorderColor { get { return this._Owner.GetValue(this._SkinSetKey, "ElementBorderColor", DefaultElementBorderColor); } set { this._Owner.SetValue(this._SkinSetKey, "ElementBorderColor", value); } }
        /// <summary>
        /// Graf: Link - barva pozadí linky
        /// </summary>
        public Color ElementLinkBackColor { get { return this._Owner.GetValue(this._SkinSetKey, "ElementLinkBackColor", DefaultElementLinkBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "ElementLinkBackColor", value); } }
        /// <summary>
        /// Graf: element - barva Selected orámování
        /// </summary>
        public Color ElementSelectedLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "ElementSelectedLineColor", DefaultElementSelectedLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "ElementSelectedLineColor", value); } }
        /// <summary>
        /// Graf: Frame, barva linky
        /// </summary>
        public Color ElementFramedLineColor { get { return this._Owner.GetValue(this._SkinSetKey, "ElementFramedLineColor", DefaultElementFramedLineColor); } set { this._Owner.SetValue(this._SkinSetKey, "ElementFramedLineColor", value); } }
        /// <summary>
        /// Graf: barva pozadí
        /// </summary>
        public Color BackColor { get { return this._Owner.GetValue(this._SkinSetKey, "BackColor", DefaultBackColor); } set { this._Owner.SetValue(this._SkinSetKey, "BackColor", value); } }
        /// <summary>
        /// Graf: barva linek časové osy Main
        /// </summary>
        public Color TimeAxisTickMain { get { return this._Owner.GetValue(this._SkinSetKey, "TimeAxisTickMain", DefaultTimeAxisTickMain); } set { this._Owner.SetValue(this._SkinSetKey, "TimeAxisTickMain", value); } }
        /// <summary>
        /// Graf: barva linek časové osy Small
        /// </summary>
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
        #region Default values
        /// <summary>
        /// Graf: výška linky
        /// </summary>
        protected virtual Int32 DefaultLineHeight { get { return 18; } }
        /// <summary>
        /// Default pro : Graf: celková výška Min
        /// </summary>
        protected virtual Int32 DefaultTotalHeightMin { get { return 14; } }
        /// <summary>
        /// Default pro : Graf: celková výška Max
        /// </summary>
        protected virtual Int32 DefaultTotalHeightMax { get { return 480; } }
        /// <summary>
        /// Default pro : Graf: element - barva pozadí
        /// </summary>
        protected virtual Color DefaultElementBackColor { get { return Color.CornflowerBlue; } }
        /// <summary>
        /// Default pro : Graf: element - barva okraje
        /// </summary>
        protected virtual Color DefaultElementBorderColor { get { return Color.BlueViolet; } }
        /// <summary>
        /// Default pro : Graf: Link - barva pozadí linky
        /// </summary>
        protected virtual Color DefaultElementLinkBackColor { get { return Color.FromArgb(160, Color.DimGray); } }   // Barva linku obsahuje složku Alpha = 160 == úroveň Morphingu
        /// <summary>
        /// Default pro : Graf: element - barva Selected orámování
        /// </summary>
        protected virtual Color DefaultElementSelectedLineColor { get { return Color.OrangeRed; } }
        /// <summary>
        /// Default pro : Graf: Frame, barva linky
        /// </summary>
        protected virtual Color DefaultElementFramedLineColor { get { return Color.IndianRed; } }
        /// <summary>
        /// Default pro : Graf: barva pozadí
        /// </summary>
        protected virtual Color DefaultBackColor { get { return Color.DimGray; } }
        /// <summary>
        /// Default pro : Graf: barva linek časové osy Main
        /// </summary>
        protected virtual Color DefaultTimeAxisTickMain { get { return Color.FromArgb(216, 216, 216); } }
        /// <summary>
        /// Default pro : Graf: barva linek časové osy Small
        /// </summary>
        protected virtual Color DefaultTimeAxisTickSmall { get { return Color.FromArgb(216, 216, 216); } }
        /// <summary>
        /// Default pro : Barva linky základní.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je větší nebo rovno Prev.End, pak se použije <see cref="LinkColorStandard"/>.
        /// Další barvy viz <see cref="LinkColorWarning"/> a <see cref="LinkColorError"/>
        /// </summary>
        protected virtual Color DefaultLinkColorStandard { get { return Color.Green; } }
        /// <summary>
        /// Default pro : Barva linky varovná.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.End, ale Next.Begin je větší nebo rovno Prev.Begin, pak se použije <see cref="LinkColorWarning"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorError"/>
        /// </summary>
        protected virtual Color DefaultLinkColorWarning { get { return Color.Orange; } }
        /// <summary>
        /// Default pro : Barva linky chybová.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.Begin, pak se použije <see cref="LinkColorError"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorWarning"/>
        /// </summary>
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
        #region Public values
        /// <summary>
        /// Vztah : výška linky v Gridu
        /// </summary>
        public Int32 LineHeightInGrid { get { return this._Owner.GetValue(this._SkinSetKey, "LineHeightInGrid", DefaultLineHeightInGrid); } set { this._Owner.SetValue(this._SkinSetKey, "LineHeightInGrid", value); } }
        /// <summary>
        /// Vztah : výška linky v Formu
        /// </summary>
        public Int32 LineHeightInForm { get { return this._Owner.GetValue(this._SkinSetKey, "LineHeightInForm", DefaultLineHeightInForm); } set { this._Owner.SetValue(this._SkinSetKey, "LineHeightInForm", value); } }
        /// <summary>
        /// Vztah : barva linky pro vztažený Záznam v Formu
        /// </summary>
        public Color LineColorForRecordInForm { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorForRecordInForm", DefaultLineColorForRecordInForm); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorForRecordInForm", value); } }
        /// <summary>
        /// Vztah : barva linky pro vztažený Záznam v Gridu
        /// </summary>
        public Color LineColorForRecordInGrid { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorForRecordInGrid", DefaultLineColorForRecordInGrid); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorForRecordInGrid", value); } }
        /// <summary>
        /// Vztah : barva linky pro vztažený Dokument v Formu, horní část
        /// </summary>
        public Color LineColorForDocumentInForm1 { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorForDocumentInForm1", DefaultLineColorForDocumentInForm1); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorForDocumentInForm1", value); } }
        /// <summary>
        /// Vztah : barva linky pro vztažený Dokument v Formu, dolní část
        /// </summary>
        public Color LineColorForDocumentInForm2 { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorForDocumentInForm2", DefaultLineColorForDocumentInForm2); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorForDocumentInForm2", value); } }
        /// <summary>
        /// Vztah : barva linky pro vztažený Dokument v Gridu
        /// </summary>
        public Color LineColorForDocumentInGrid { get { return this._Owner.GetValue(this._SkinSetKey, "LineColorForDocumentInGrid", DefaultLineColorForDocumentInGrid); } set { this._Owner.SetValue(this._SkinSetKey, "LineColorForDocumentInGrid", value); } }
        /// <summary>
        /// Poměr slábnutí barvy
        /// </summary>
        public float LineFadingRatio { get { return this._Owner.GetValue(this._SkinSetKey, "LineFadingRatio", DefaultLineFadingRatio); } set { this._Owner.SetValue(this._SkinSetKey, "LineFadingRatio", value); } }
        #endregion
        #region Default values
        /// <summary>
        /// Default pro : Vztah : výška linky v Gridu
        /// </summary>
        protected virtual Int32 DefaultLineHeightInGrid { get { return 1; } }
        /// <summary>
        /// Default pro : Vztah : výška linky v Formu
        /// </summary>
        protected virtual Int32 DefaultLineHeightInForm { get { return 2; } }
        /// <summary>
        /// Default pro : Vztah : barva vztahu na Záznam v Gridu
        /// </summary>
        protected virtual Color DefaultLineColorForRecordInGrid { get { return Color.FromArgb(71, 130, 180); } }
        /// <summary>
        /// Default pro : Vztah : barva vztahu na Záznam v Formu
        /// </summary>
        protected virtual Color DefaultLineColorForRecordInForm { get { return Color.FromArgb(0, 116, 206); } }
        /// <summary>
        /// Default pro : Vztah : barva linky na Dokument v Formu, horní část
        /// </summary>
        protected virtual Color DefaultLineColorForDocumentInForm1 { get { return Color.FromArgb(214, 214, 1); } }
        /// <summary>
        /// Default pro : Vztah : barva linky na Dokument v Formu, dolní část
        /// </summary>
        protected virtual Color DefaultLineColorForDocumentInForm2 { get { return Color.FromArgb(255, 255, 0); } }
        /// <summary>
        /// Default pro : Vztah : barva linky na Dokument v Gridu
        /// </summary>
        protected virtual Color DefaultLineColorForDocumentInGrid { get { return Color.FromArgb(214, 214, 1); } }
        /// <summary>
        /// Default pro : Poměr slábnutí barvy
        /// </summary>
        protected virtual float DefaultLineFadingRatio { get { return 0.60f; } }
        #endregion
    }
    /// <summary>
    /// Sada standardních ikon
    /// </summary>
    public class SkinIcons : SkinSet
    {
        #region Internal and private
        internal SkinIcons(Skin owner, string skinSetKey)
            : base(owner, skinSetKey)
        { }
        #endregion
        #region Public prvky
        /// <summary>
        /// Ikona Info - velká - její název
        /// </summary>
        public string IconInfoNameBig { get { return this._Owner.GetValue(this._SkinSetKey, "IconInfoBig", DefaultIconInfoNameBig); } set { this._Owner.SetValue(this._SkinSetKey, "IconInfoBig", value); } }
        /// <summary>
        /// Ikona Info - malá - její název
        /// </summary>
        public string IconInfoNameSmall { get { return this._Owner.GetValue(this._SkinSetKey, "IconInfoSmall", DefaultIconInfoNameSmall); } set { this._Owner.SetValue(this._SkinSetKey, "IconInfoSmall", value); } }

        /// <summary>
        /// Ikona Warning - velká - její název
        /// </summary>
        public string IconWarningNameBig { get { return this._Owner.GetValue(this._SkinSetKey, "IconWarningBig", DefaultIconWarningNameBig); } set { this._Owner.SetValue(this._SkinSetKey, "IconWarningBig", value); } }
        /// <summary>
        /// Ikona Warning - malá - její název
        /// </summary>
        public string IconWarningNameSmall { get { return this._Owner.GetValue(this._SkinSetKey, "IconWarningSmall", DefaultIconWarningNameSmall); } set { this._Owner.SetValue(this._SkinSetKey, "IconWarningSmall", value); } }

        /// <summary>
        /// Ikona Error - velká - její název
        /// </summary>
        public string IconErrorNameBig { get { return this._Owner.GetValue(this._SkinSetKey, "IconErrorBig", DefaultIconErrorNameBig); } set { this._Owner.SetValue(this._SkinSetKey, "IconErrorBig", value); } }
        /// <summary>
        /// Ikona Error - malá - její název
        /// </summary>
        public string IconErrorNameSmall { get { return this._Owner.GetValue(this._SkinSetKey, "IconErrorSmall", DefaultIconErrorNameSmall); } set { this._Owner.SetValue(this._SkinSetKey, "IconErrorSmall", value); } }

        /// <summary>
        /// Ikona Stop - velká - její název
        /// </summary>
        public string IconStopNameBig { get { return this._Owner.GetValue(this._SkinSetKey, "IconStopBig", DefaultIconStopNameBig); } set { this._Owner.SetValue(this._SkinSetKey, "IconStopBig", value); } }
        /// <summary>
        /// Ikona Stop - malá - její název
        /// </summary>
        public string IconStopNameSmall { get { return this._Owner.GetValue(this._SkinSetKey, "IconStopSmall", DefaultIconStopNameSmall); } set { this._Owner.SetValue(this._SkinSetKey, "IconStopSmall", value); } }

        /// <summary>
        /// Ikona Question - velká - její název
        /// </summary>
        public string IconQuestionNameBig { get { return this._Owner.GetValue(this._SkinSetKey, "IconQuestionBig", DefaultIconQuestionNameBig); } set { this._Owner.SetValue(this._SkinSetKey, "IconQuestionBig", value); } }
        /// <summary>
        /// Ikona Question - malá - její název
        /// </summary>
        public string IconQuestionNameSmall { get { return this._Owner.GetValue(this._SkinSetKey, "IconQuestionSmall", DefaultIconQuestionNameSmall); } set { this._Owner.SetValue(this._SkinSetKey, "IconQuestionSmall", value); } }

        /// <summary>
        /// Ikona Tip - velká - její název
        /// </summary>
        public string IconTipNameBig { get { return this._Owner.GetValue(this._SkinSetKey, "IconTipBig", DefaultIconTipNameBig); } set { this._Owner.SetValue(this._SkinSetKey, "IconTipBig", value); } }
        /// <summary>
        /// Ikona Tip - malá - její název
        /// </summary>
        public string IconTipNameSmall { get { return this._Owner.GetValue(this._SkinSetKey, "IconTipSmall", DefaultIconTipNameSmall); } set { this._Owner.SetValue(this._SkinSetKey, "IconTipSmall", value); } }

        /// <summary>
        /// Ikona OK - velká - její název
        /// </summary>
        public string IconOKNameBig { get { return this._Owner.GetValue(this._SkinSetKey, "IconOKBig", DefaultIconOKNameBig); } set { this._Owner.SetValue(this._SkinSetKey, "IconOKBig", value); } }
        /// <summary>
        /// Ikona OK - malá - její název
        /// </summary>
        public string IconOKNameSmall { get { return this._Owner.GetValue(this._SkinSetKey, "IconOKSmall", DefaultIconOKNameSmall); } set { this._Owner.SetValue(this._SkinSetKey, "IconOKSmall", value); } }

        #endregion
        #region Public property pro přímé získání objektu ikony
        /// <summary>Ikona : Info, velká. Nesmí se použít v using patternu, nesmí se Disposovat, používá se opakovaně.</summary>
        public Image IconInfoBig { get { return this.GetIcon(IconImageType.Info, true); } }
        /// <summary>Ikona : Info, malá. Nesmí se použít v using patternu, nesmí se Disposovat, používá se opakovaně.</summary>
        public Image IconInfoSmall { get { return this.GetIcon(IconImageType.Info, false); } }
        /// <summary>Ikona : Warning, velká. Nesmí se použít v using patternu, nesmí se Disposovat, používá se opakovaně.</summary>
        public Image IconWarningBig { get { return this.GetIcon(IconImageType.Warning, true); } }
        /// <summary>Ikona : Warning, malá. Nesmí se použít v using patternu, nesmí se Disposovat, používá se opakovaně.</summary>
        public Image IconWarningSmall { get { return this.GetIcon(IconImageType.Warning, false); } }
        /// <summary>Ikona : Error, velká. Nesmí se použít v using patternu, nesmí se Disposovat, používá se opakovaně.</summary>
        public Image IconErrorBig { get { return this.GetIcon(IconImageType.Error, true); } }
        /// <summary>Ikona : Error, malá. Nesmí se použít v using patternu, nesmí se Disposovat, používá se opakovaně.</summary>
        public Image IconErrorSmall { get { return this.GetIcon(IconImageType.Error, false); } }
        /// <summary>Ikona : Stop, velká. Nesmí se použít v using patternu, nesmí se Disposovat, používá se opakovaně.</summary>
        public Image IconStopBig { get { return this.GetIcon(IconImageType.Stop, true); } }
        /// <summary>Ikona : Stop, malá. Nesmí se použít v using patternu, nesmí se Disposovat, používá se opakovaně.</summary>
        public Image IconStopSmall { get { return this.GetIcon(IconImageType.Stop, false); } }
        /// <summary>Ikona : Question, velká. Nesmí se použít v using patternu, nesmí se Disposovat, používá se opakovaně.</summary>
        public Image IconQuestionBig { get { return this.GetIcon(IconImageType.Question, true); } }
        /// <summary>Ikona : Question, malá. Nesmí se použít v using patternu, nesmí se Disposovat, používá se opakovaně.</summary>
        public Image IconQuestionSmall { get { return this.GetIcon(IconImageType.Question, false); } }
        /// <summary>Ikona : Tip, velká. Nesmí se použít v using patternu, nesmí se Disposovat, používá se opakovaně.</summary>
        public Image IconTipBig { get { return this.GetIcon(IconImageType.Tip, true); } }
        /// <summary>Ikona : Tip, malá. Nesmí se použít v using patternu, nesmí se Disposovat, používá se opakovaně.</summary>
        public Image IconTipSmall { get { return this.GetIcon(IconImageType.Tip, false); } }
        /// <summary>Ikona : OK, velká. Nesmí se použít v using patternu, nesmí se Disposovat, používá se opakovaně.</summary>
        public Image IconOKBig { get { return this.GetIcon(IconImageType.OK, true); } }
        /// <summary>Ikona : OK, malá. Nesmí se použít v using patternu, nesmí se Disposovat, používá se opakovaně.</summary>
        public Image IconOKSmall { get { return this.GetIcon(IconImageType.OK, false); } }
        #endregion
        #region Public získání ikony dle daného typu (enum IconImageType)
        /// <summary>
        /// Vrátí jméno ikony pro daný typ ikony a velikost
        /// </summary>
        /// <param name="imageType"></param>
        /// <param name="bigSize"></param>
        /// <returns></returns>
        public string GetName(IconImageType imageType, bool bigSize = true)
        {
            switch (imageType)
            {
                case IconImageType.None: return "";
                case IconImageType.Info: return (bigSize ? this.IconInfoNameBig : this.IconInfoNameSmall);
                case IconImageType.Warning: return (bigSize ? this.IconWarningNameBig : this.IconWarningNameSmall);
                case IconImageType.Error: return (bigSize ? this.IconErrorNameBig : this.IconErrorNameSmall);
                case IconImageType.Stop: return (bigSize ? this.IconStopNameBig : this.IconStopNameSmall);
                case IconImageType.Question: return (bigSize ? this.IconQuestionNameBig : this.IconQuestionNameSmall);
                case IconImageType.Tip: return (bigSize ? this.IconTipNameBig : this.IconTipNameSmall);
                case IconImageType.OK: return (bigSize ? this.IconOKNameBig : this.IconOKNameSmall);
            }
            return null;
        }
        /// <summary>
        /// Vrátí ikonu pro daný typ ikony a velikost.
        /// Ikona se nesmí Disposovat, používá se opakovaně.
        /// </summary>
        /// <param name="imageType"></param>
        /// <param name="bigSize"></param>
        /// <returns></returns>
        public Image GetIcon(IconImageType imageType, bool bigSize = true)
        {
            string name = this.GetName(imageType, bigSize);
            if (String.IsNullOrEmpty(name)) return null;
            return Application.App.ResourcesApp.GetImage(name);
        }
        #endregion
        #region Default jména = vazba na konkrétní obrázky
        /// <summary>
        /// Default pro : Ikona Info - velká - její název
        /// </summary>
        protected virtual string DefaultIconInfoNameBig { get { return R.Images.Status.DialogInformation3Png; } }
        /// <summary>
        /// Default pro : Ikona Info - malá - její název
        /// </summary>
        protected virtual string DefaultIconInfoNameSmall { get { return R.Images.Status16.DialogInformation3Png; } }

        /// <summary>
        /// Default pro : Ikona Warning - velká - její název
        /// </summary>
        protected virtual string DefaultIconWarningNameBig { get { return R.Images.Status.DialogWarning3Png; } }
        /// <summary>
        /// Default pro : Ikona Warning - malá - její název
        /// </summary>
        protected virtual string DefaultIconWarningNameSmall { get { return R.Images.Status16.DialogWarning3Png; } }

        /// <summary>
        /// Default pro : Ikona Error - velká - její název
        /// </summary>
        protected virtual string DefaultIconErrorNameBig { get { return R.Images.Status.DialogError3Png; } }
        /// <summary>
        /// Default pro : Ikona Error - malá - její název
        /// </summary>
        protected virtual string DefaultIconErrorNameSmall { get { return R.Images.Status16.DialogError3Png; } }

        /// <summary>
        /// Default pro : Ikona Stop - velká - její název
        /// </summary>
        protected virtual string DefaultIconStopNameBig { get { return R.Images.Status.DialogError5Png; } }
        /// <summary>
        /// Default pro : Ikona Stop - malá - její název
        /// </summary>
        protected virtual string DefaultIconStopNameSmall { get { return R.Images.Status16.DialogError5Png; } }

        /// <summary>
        /// Default pro : Ikona Question - velká - její název
        /// </summary>
        protected virtual string DefaultIconQuestionNameBig { get { return R.Images.Status.DialogQuestion2Png; } }
        /// <summary>
        /// Default pro : Ikona Question - malá - její název
        /// </summary>
        protected virtual string DefaultIconQuestionNameSmall { get { return R.Images.Status16.DialogQuestion2Png; } }

        /// <summary>
        /// Default pro : Ikona Tip - velká - její název
        /// </summary>
        protected virtual string DefaultIconTipNameBig { get { return R.Images.Status.DialogInformation2Png; } }
        /// <summary>
        /// Default pro : Ikona Tip - malá - její název
        /// </summary>
        protected virtual string DefaultIconTipNameSmall { get { return R.Images.Status16.DialogInformation2Png; } }

        /// <summary>
        /// Default pro : Ikona OK - velká - její název
        /// </summary>
        protected virtual string DefaultIconOKNameBig { get { return R.Images.Status.DialogCleanPng; } }
        /// <summary>
        /// Default pro : Ikona OK - malá - její název
        /// </summary>
        protected virtual string DefaultIconOKNameSmall { get { return R.Images.Status16.DialogCleanPng; } }
        #endregion
    }
    #region enum IconImageType
    /// <summary>
    /// Typ obrázku
    /// </summary>
    public enum IconImageType
    {
        /// <summary>
        /// Bez ikony
        /// </summary>
        None,
        /// <summary>
        /// Informace (i)
        /// </summary>
        Info,
        /// <summary>
        /// Varování (vykřičník)
        /// </summary>
        Warning,
        /// <summary>
        /// Chyba (zákaz vjezdu do protisměru)
        /// </summary>
        Error,
        /// <summary>
        /// Zastavení (křížek X)
        /// </summary>
        Stop,
        /// <summary>
        /// Otázka (otauník)
        /// </summary>
        Question,
        /// <summary>
        /// Tip (žárovka)
        /// </summary>
        Tip,
        /// <summary>
        /// OK (fajfka)
        /// </summary>
        OK
    }
    #endregion
    /// <summary>
    /// Skin set abstract base.
    /// </summary>
    public abstract class SkinSet : ISkinSet
    {
        #region Internal and protected
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="skinSetKey"></param>
        public SkinSet(string skinSetKey)
        {
            this._SkinSetKey = skinSetKey;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="skinSetKey"></param>
        public SkinSet(Skin owner, string skinSetKey)
        {
            this._Owner = owner;
            this._SkinSetKey = skinSetKey;
            this._FillPalette();
        }
        /// <summary>
        /// Vynuluje všechny proměnné = nastaví defaultní vzhled skinu
        /// </summary>
        public virtual void Reset()
        {
            this._Owner.ResetValues(this._SkinSetKey);
        }
        /// <summary>
        /// Vloží dodaného vlastníka
        /// </summary>
        /// <param name="owner"></param>
        void ISkinSet.SetOwner(Skin owner)
        {
            this._Owner = owner;
            this._FillPalette();
        }
        /// <summary>
        /// Evaluate all public instance properties from this type and its base classes, to fill all items into Skin.Palette dictionary.
        /// </summary>
        protected void _FillPalette()
        {
            List<System.Reflection.PropertyInfo> validProperties = new List<System.Reflection.PropertyInfo>();

            var properties = this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
            foreach (var property in properties)
            {   // Provedu načtení hodnoty ze všech public instančních property; tím se tyto property a jejich hodnoty dostanou do centrální cache:
                var getMethod = property.GetGetMethod();
                var setMethod = property.GetSetMethod();
                // Načítat budu hodnoty pouze z těch properties, které mají i set metodu. 
                // Proč? Abych vynechal ty property, které jsou například pomocné, a stejně je nepůjde nasetovat po deserializaci.
                // Jako příklad: Skin.Icons.IconInfoBig = má metodu get { }, nemá set { }, 
                //   ale get metoda provede načtení Skin.Icons.IconInfoNameBig a konverzi na typ Image.
                // Pro ověření odkomentuj řádek a debugni si jeho vnitřní kód:
                //   if (getMethod != null && setMethod == null) { }
                if (getMethod != null && setMethod != null)
                {
                    try { var value = property.GetValue(this, null); validProperties.Add(property); }
                    catch (Exception) { }
                }
            }

            _Properties = validProperties.ToArray();
        }
        private System.Reflection.PropertyInfo[] _Properties;
        /// <summary>
        /// Vlastník
        /// </summary>
        protected Skin _Owner;
        /// <summary>
        /// Název sady dat
        /// </summary>
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
        protected static T GetMatrix<T>(bool t0, bool t1, T value00, T value01, T value10, T value11)
        {
            return (t0 ? (t1 ? value11 : value10) : (t1 ? value01 : value00));
        }
        #endregion
    }
    /// <summary>
    /// Interface pro internal přístup
    /// </summary>
    public interface ISkinSet
    {
        /// <summary>
        /// Vloží dodaného vlastníka
        /// </summary>
        /// <param name="owner"></param>
        void SetOwner(Skin owner);
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
