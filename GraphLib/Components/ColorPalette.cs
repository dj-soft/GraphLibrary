using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Palette of standard application Colors and its Pens and Brushes.
    /// Palette is created for Skin = ApplicationBlue; Skin can be changed by setting an value to Skin or UserColors properties.
    /// </summary>
    public class ColorPalette
    {
        #region Singleton
        /// <summary>
        /// Singleton of ColorPalette instance.
        /// Singleton is created for Skin = ApplicationBlue, can be changed by setting Skin or UserColors properties.
        /// </summary>
        private static ColorPalette Current
        {
            get
            {
                if (__Current == null)
                {
                    lock (__Locker)
                    {
                        if (__Current == null)
                        {
                            __Current = new ColorPalette();
                            __Current._Initialise(DefaultSkin);
                        }
                    }
                }
                return __Current;
            }
        }
        private static ColorPalette __Current = null;
        private static object __Locker = new object();
        private ColorPalette()
        {
            this._PaletteItems = new Dictionary<PaletteItemType, PaletteItem>();
        }
        #endregion
        #region Private initialising, instantial members, reload, clear (=destroy)
        private void _Initialise()
        { 
            this._Initialise(DefaultSkin);
        }
        private void _Initialise(PaletteSkinType skin)
        {
            PaletteBaseColors baseColors = PaletteBaseColors.GetForSkin(skin);
            this._Initialise(skin, baseColors, null);
        }
        private void _Initialise(PaletteBaseColors baseColors)
        {
            this._Initialise(PaletteSkinType.UserDefined, baseColors, null);
        }
        private void _Initialise(Dictionary<PaletteItemType, Color> userTable)
        {
            PaletteBaseColors baseColors = PaletteBaseColors.GetForSkin(PaletteSkinType.CurrentWindows);
            this._Initialise(PaletteSkinType.UserDefined, baseColors, userTable);
        }
        private void _Initialise(PaletteSkinType skin, PaletteBaseColors baseColors, Dictionary<PaletteItemType, Color> userTable)
        {
            this._Clear();
            
            Array types = Enum.GetValues(typeof(PaletteItemType));
            foreach (object item in types)
            {
                PaletteItemType type = (PaletteItemType)item;
                Color color = _GetColorForType(type, baseColors, userTable);
                this._PaletteItems.Add(type, new PaletteItem(type, color));
            }
            this._Skin = skin;
            this._BaseColors = baseColors;
        }
        private static Color _GetColorForType(PaletteItemType type, PaletteBaseColors baseColors, Dictionary<PaletteItemType, Color> userTable)
        {
            Color color;
            if (userTable != null && userTable.TryGetValue(type, out color)) return color;
            if (baseColors != null) return baseColors[type];
            return Color.Gray;
        }
        /// <summary>
        /// Clear this _PaletteItems. _PaletteItems will be exists (not null), with zero items.
        /// </summary>
        private void _Clear()
        {
            if (this._PaletteItems == null)
                this._PaletteItems = new Dictionary<PaletteItemType, PaletteItem>();
            foreach (PaletteItem item in this._PaletteItems.Values)
            {
                if (item != null)
                    item.Clear();
            }
            this._PaletteItems.Clear();
        }
        private Dictionary<PaletteItemType, PaletteItem> _PaletteItems;
        /// <summary>
        /// One item in palette: ItemType, Color, Pen, Brush
        /// </summary>
        private class PaletteItem
        {
            public PaletteItem(PaletteItemType type, Color color)
            {
                this._ItemType = type;
                this._Color = color;
                this._Pen = null;
                this._Brush = null;
            }
            /// <summary>
            /// Type stored in this item
            /// </summary>
            public PaletteItemType ItemType { get { return this._ItemType; } } private PaletteItemType _ItemType;
            /// <summary>
            /// Color for this item
            /// </summary>
            public Color Color { get { return this._Color; } } private Color _Color;
            /// <summary>
            /// Pen for this item
            /// </summary>
            public Pen Pen { get { this._PenCheck(); return this._Pen; } } private Pen _Pen;
            /// <summary>
            /// Pen for this item
            /// </summary>
            public Brush Brush { get { this._BrushCheck(); return this._Brush; } } private Brush _Brush;
            /// <summary>
            /// Release (Dispose) inner existing object.
            /// </summary>
            public void Clear()
            { }
            private void _PenCheck()
            {
                if (this._Pen == null)
                    this._Pen = new Pen(this.Color);
            }
            private void _BrushCheck()
            {
                if (this._Brush == null)
                    this._Brush = new SolidBrush(this.Color);
            }
        }
        #endregion
        #region Palette Skins
        /// <summary>
        /// Currently used Skin for colors. Setting an value will reload palette. Setting UserDefined skin has no effect, you must setting UserColors property.
        /// </summary>
        public static PaletteSkinType Skin
        {
            get { return Current._Skin; }
            set { Current._SetSkin(value); }
        }
        /// <summary>
        /// Currently used Colors for ItemType.
        /// Getting a value create new Dictionary from all currently valid ItemTypes.
        /// Setting value refill palette with specified values, with default values (for missing keys) from CurrentWindows skin, and set Skin property to UserDefined. 
        /// Setting null value to UserColors has no effect.
        /// </summary>
        public static Dictionary<PaletteItemType, Color> UserColors
        {
            get { return Current._GetUserColors(); }
            set { Current._SetUserColors(value); }
        }
        public static PaletteBaseColors BaseColors
        {
            get { return Current._BaseColors.Clone; }
            set { Current._SetBaseColors(value); }
        }
        /// <summary>
        /// Default Skin for palette.
        /// Is PaletteSkinType.ApplicationBlue.
        /// </summary>
        public static PaletteSkinType DefaultSkin { get { return PaletteSkinType.ApplicationBlue; } }
        private void _SetSkin(PaletteSkinType skin)
        {
            if (skin == PaletteSkinType.UserDefined) return;
            if (skin == this._Skin) return;
            this._Initialise(skin);
        }
        private Dictionary<PaletteItemType, Color> _GetUserColors()
        {
            Dictionary<PaletteItemType, Color> result = new Dictionary<PaletteItemType, Color>();
            foreach (var kv in this._PaletteItems)
                result.Add(kv.Key, kv.Value.Color);
            return result;
        }
        private void _SetUserColors(Dictionary<PaletteItemType, Color> colorTable)
        {
            if (colorTable == null) return;
            this._Initialise(colorTable);
        }
        private void _SetBaseColors(PaletteBaseColors baseColors)
        {
            if (baseColors == null) return;
            this._Initialise(baseColors);
        }
        private PaletteSkinType _Skin;
        private PaletteBaseColors _BaseColors;
        #endregion
        #region Palette defititions for fixed palette skins


        #endregion
        #region Public Colors
        public static Color ColorFor(PaletteItemType itemType) { return Current._PaletteItems[itemType].Color; }

        public static Color ButtonBackDisableColor { get { return ColorFor(PaletteItemType.ButtonBackDisable); } }
        public static Color ButtonForeDisableColor { get { return ColorFor(PaletteItemType.ButtonForeDisable); } }
        public static Color ButtonBackEnableColor { get { return ColorFor(PaletteItemType.ButtonBackEnable); } }
        public static Color ButtonForeEnableColor { get { return ColorFor(PaletteItemType.ButtonForeEnable); } }
        public static Color ButtonBackHotColor { get { return ColorFor(PaletteItemType.ButtonBackHot); } }
        public static Color ButtonForeHotColor { get { return ColorFor(PaletteItemType.ButtonForeHot); } }
        public static Color ButtonBackDownColor { get { return ColorFor(PaletteItemType.ButtonBackDown); } }
        public static Color ButtonForeDownColor { get { return ColorFor(PaletteItemType.ButtonForeDown); } }

        public static Color ScrollBarBackDisableColor { get { return ColorFor(PaletteItemType.ScrollBarBackDisable); } }
        public static Color ScrollBarForeDisableColor { get { return ColorFor(PaletteItemType.ScrollBarForeDisable); } }
        public static Color ScrollBarBackEnableColor { get { return ColorFor(PaletteItemType.ScrollBarBackEnable); } }
        public static Color ScrollBarForeEnableColor { get { return ColorFor(PaletteItemType.ScrollBarForeEnable); } }
        public static Color ScrollBarBackHotColor { get { return ColorFor(PaletteItemType.ScrollBarBackHot); } }
        public static Color ScrollBarForeHotColor { get { return ColorFor(PaletteItemType.ScrollBarForeHot); } }
        public static Color ScrollBarBackDownColor { get { return ColorFor(PaletteItemType.ScrollBarBackDown); } }
        public static Color ScrollBarForeDownColor { get { return ColorFor(PaletteItemType.ScrollBarForeDown); } }

        // ...
        #endregion
        #region Public Pens
        public static Pen PenFor(PaletteItemType itemType) { return Current._PaletteItems[itemType].Pen; }

        public static Pen ButtonBackDisablePen { get { return PenFor(PaletteItemType.ButtonBackDisable); } }
        public static Pen ButtonForeDisablePen { get { return PenFor(PaletteItemType.ButtonForeDisable); } }
        public static Pen ButtonBackEnablePen { get { return PenFor(PaletteItemType.ButtonBackEnable); } }
        public static Pen ButtonForeEnablePen { get { return PenFor(PaletteItemType.ButtonForeEnable); } }
        public static Pen ButtonBackHotPen { get { return PenFor(PaletteItemType.ButtonBackHot); } }
        public static Pen ButtonForeHotPen { get { return PenFor(PaletteItemType.ButtonForeHot); } }
        public static Pen ButtonBackDownPen { get { return PenFor(PaletteItemType.ButtonBackDown); } }
        public static Pen ButtonForeDownPen { get { return PenFor(PaletteItemType.ButtonForeDown); } }

        public static Pen ScrollBarBackDisablePen { get { return PenFor(PaletteItemType.ScrollBarBackDisable); } }
        public static Pen ScrollBarForeDisablePen { get { return PenFor(PaletteItemType.ScrollBarForeDisable); } }
        public static Pen ScrollBarBackEnablePen { get { return PenFor(PaletteItemType.ScrollBarBackEnable); } }
        public static Pen ScrollBarForeEnablePen { get { return PenFor(PaletteItemType.ScrollBarForeEnable); } }
        public static Pen ScrollBarBackHotPen { get { return PenFor(PaletteItemType.ScrollBarBackHot); } }
        public static Pen ScrollBarForeHotPen { get { return PenFor(PaletteItemType.ScrollBarForeHot); } }
        public static Pen ScrollBarBackDownPen { get { return PenFor(PaletteItemType.ScrollBarBackDown); } }
        public static Pen ScrollBarForeDownPen { get { return PenFor(PaletteItemType.ScrollBarForeDown); } }

        // ...
        #endregion
        #region Public SolidBrushes
        public static Brush BrushFor(PaletteItemType itemType) { return Current._PaletteItems[itemType].Brush; }
        
        public static Brush ButtonBackDisableBrush { get { return BrushFor(PaletteItemType.ButtonBackDisable); } }
        public static Brush ButtonForeDisableBrush { get { return BrushFor(PaletteItemType.ButtonForeDisable); } }
        public static Brush ButtonBackEnableBrush { get { return BrushFor(PaletteItemType.ButtonBackEnable); } }
        public static Brush ButtonForeEnableBrush { get { return BrushFor(PaletteItemType.ButtonForeEnable); } }
        public static Brush ButtonBackHotBrush { get { return BrushFor(PaletteItemType.ButtonBackHot); } }
        public static Brush ButtonForeHotBrush { get { return BrushFor(PaletteItemType.ButtonForeHot); } }
        public static Brush ButtonBackDownBrush { get { return BrushFor(PaletteItemType.ButtonBackDown); } }
        public static Brush ButtonForeDownBrush { get { return BrushFor(PaletteItemType.ButtonForeDown); } }

        public static Brush ScrollBarBackDisableBrush { get { return BrushFor(PaletteItemType.ScrollBarBackDisable); } }
        public static Brush ScrollBarForeDisableBrush { get { return BrushFor(PaletteItemType.ScrollBarForeDisable); } }
        public static Brush ScrollBarBackEnableBrush { get { return BrushFor(PaletteItemType.ScrollBarBackEnable); } }
        public static Brush ScrollBarForeEnableBrush { get { return BrushFor(PaletteItemType.ScrollBarForeEnable); } }
        public static Brush ScrollBarBackHotBrush { get { return BrushFor(PaletteItemType.ScrollBarBackHot); } }
        public static Brush ScrollBarForeHotBrush { get { return BrushFor(PaletteItemType.ScrollBarForeHot); } }
        public static Brush ScrollBarBackDownBrush { get { return BrushFor(PaletteItemType.ScrollBarBackDown); } }
        public static Brush ScrollBarForeDownBrush { get { return BrushFor(PaletteItemType.ScrollBarForeDown); } }

        
        // ...
        #endregion
    }
    #region Enums PaletteSkinType and PaletteItemType
    /// <summary>
    /// Type of skin for current application
    /// </summary>
    public enum PaletteSkinType
    {
        /// <summary>
        /// Derived from current Windows colors
        /// </summary>
        CurrentWindows,
        /// <summary>
        /// Specified by application, preferred Blue color
        /// </summary>
        ApplicationBlue,
        /// <summary>
        /// Specified by application, preferred Green color
        /// </summary>
        ApplicationGreen,
        /// <summary>
        /// User defined, with missing colors in User definiton replaced by CurrentWindows colors.
        /// </summary>
        UserDefined
    }
    public enum PaletteItemType
    {
        WorkspaceBack,

        ButtonBackDisable,
        ButtonForeDisable,
        ButtonBackEnable,
        ButtonForeEnable,
        ButtonBackHot,
        ButtonForeHot,
        ButtonBackDown,
        ButtonForeDown,

        ScrollBarBackDisable,
        ScrollBarForeDisable,
        ScrollBarBackEnable,
        ScrollBarForeEnable,
        ScrollBarBackHot,
        ScrollBarForeHot,
        ScrollBarBackDown,
        ScrollBarForeDown,

        AxisBackDisable,
        AxisForeDisable,
        AxisBackEnable,
        AxisForeEnable,
        AxisBackHot,
        AxisForeHot,
        AxisBackDown,
        AxisForeDown,

        // Založení nové hodnoty tohoto enumu se musí promítnout přinejmenším do metod _ColorForWindows(), _ColorForAppBlue(), _ColorForAppGreen().
        // Je vhodné (ale ne nutné) přidat odpovídající Property do regionů Colors, Pens a SolidBrushes.

    }
    #endregion
    #region class PaletteBaseColors : BaseColors + MorphColors + MorphRatio = basic data for all derived colors
    /// <summary>
    /// PaletteBaseColors : BaseColors + MorphColors + MorphRatio = basic data for all derived colors
    /// </summary>
    public class PaletteBaseColors
    {
        public Color WorkspaceBack { get; set; }

        public Color ButtonBack { get; set; }
        public Color ButtonFore { get; set; }
        public Color ScrollBarBack { get; set; }
        public Color ScrollBarFore { get; set; }
        public Color AxisBack { get; set; }
        public Color AxisFore { get; set; }

        public Color MorphDisableBack { get; set; }
        public Color MorphDisableFore { get; set; }
        public float MorphDisableRatio { get; set; }
        public Color MorphHotBack { get; set; }
        public Color MorphHotFore { get; set; }
        public float MorphHotRatio { get; set; }
        public Color MorphDownBack { get; set; }
        public Color MorphDownFore { get; set; }
        public float MorphDownRatio { get; set; }
        /// <summary>
        /// Returns color for specified type, created by morphing Base color and Morph color and ratio for this type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Color this[PaletteItemType type]
        {
            get
            {
                switch (type)
                {
                    case PaletteItemType.WorkspaceBack: return this.WorkspaceBack;

                    case PaletteItemType.ButtonBackDisable: return this.ButtonBack.Morph(this.MorphDisableBack, this.MorphDisableRatio);
                    case PaletteItemType.ButtonForeDisable: return this.ButtonFore.Morph(this.MorphDisableFore, this.MorphDisableRatio);
                    case PaletteItemType.ButtonBackEnable: return this.ButtonBack;
                    case PaletteItemType.ButtonForeEnable: return this.ButtonFore;
                    case PaletteItemType.ButtonBackHot: return this.ButtonBack.Morph(this.MorphHotBack, this.MorphHotRatio);
                    case PaletteItemType.ButtonForeHot: return this.ButtonFore.Morph(this.MorphHotFore, this.MorphHotRatio);
                    case PaletteItemType.ButtonBackDown: return this.ButtonBack.Morph(this.MorphDownBack, this.MorphDownRatio);
                    case PaletteItemType.ButtonForeDown: return this.ButtonFore.Morph(this.MorphDownFore, this.MorphDownRatio);

                    case PaletteItemType.ScrollBarBackDisable: return this.ScrollBarBack.Morph(this.MorphDisableBack, this.MorphDisableRatio);
                    case PaletteItemType.ScrollBarForeDisable: return this.ScrollBarFore.Morph(this.MorphDisableFore, this.MorphDisableRatio);
                    case PaletteItemType.ScrollBarBackEnable: return this.ScrollBarBack;
                    case PaletteItemType.ScrollBarForeEnable: return this.ScrollBarFore;
                    case PaletteItemType.ScrollBarBackHot: return this.ScrollBarBack.Morph(this.MorphHotBack, this.MorphHotRatio);
                    case PaletteItemType.ScrollBarForeHot: return this.ScrollBarFore.Morph(this.MorphHotFore, this.MorphHotRatio);
                    case PaletteItemType.ScrollBarBackDown: return this.ScrollBarBack.Morph(this.MorphDownBack, this.MorphDownRatio);
                    case PaletteItemType.ScrollBarForeDown: return this.ScrollBarFore.Morph(this.MorphDownFore, this.MorphDownRatio);

                    case PaletteItemType.AxisBackDisable: return this.AxisBack.Morph(this.MorphDisableBack, this.MorphDisableRatio);
                    case PaletteItemType.AxisForeDisable: return this.AxisFore.Morph(this.MorphDisableFore, this.MorphDisableRatio);
                    case PaletteItemType.AxisBackEnable: return this.AxisBack;
                    case PaletteItemType.AxisForeEnable: return this.AxisFore;
                    case PaletteItemType.AxisBackHot: return this.AxisBack.Morph(this.MorphHotBack, this.MorphHotRatio);
                    case PaletteItemType.AxisForeHot: return this.AxisFore.Morph(this.MorphHotFore, this.MorphHotRatio);
                    case PaletteItemType.AxisBackDown: return this.AxisBack.Morph(this.MorphDownBack, this.MorphDownRatio);
                    case PaletteItemType.AxisForeDown: return this.AxisFore.Morph(this.MorphDownFore, this.MorphDownRatio);

                }
                return Color.Gray;
            }
        }
        /// <summary>
        /// Returns a new instance of PaletteBaseColors for specified Skin
        /// </summary>
        /// <param name="skin"></param>
        /// <returns></returns>
        public static PaletteBaseColors GetForSkin(PaletteSkinType skin)
        {
            PaletteBaseColors baseColors = new PaletteBaseColors();
            switch (skin)
            {
                case PaletteSkinType.ApplicationBlue:
                    baseColors.WorkspaceBack = Color.FromArgb(0, 0, 32);

                    baseColors.ButtonBack = Color.LightSkyBlue;
                    baseColors.ButtonFore = Color.Black;
                    baseColors.ScrollBarBack = Color.LightSteelBlue;
                    baseColors.ScrollBarFore = Color.Black;
                    baseColors.AxisBack = Color.LightBlue;
                    baseColors.AxisFore = Color.Black;

                    baseColors.MorphDisableBack = Color.Gray;
                    baseColors.MorphDisableFore = Color.Gray;
                    baseColors.MorphDisableRatio = 0.25f;
                    baseColors.MorphHotBack = Color.LightYellow;
                    baseColors.MorphHotFore = Color.DarkBlue;
                    baseColors.MorphHotRatio = 0.35f;
                    baseColors.MorphDownBack = Color.DarkBlue;
                    baseColors.MorphDownFore = Color.Gold;
                    baseColors.MorphDownRatio = 0.65f;

                    break;

                case PaletteSkinType.ApplicationGreen:
                    baseColors.WorkspaceBack = Color.FromArgb(0, 32, 0);

                    baseColors.ButtonBack = Color.LightGreen;
                    baseColors.ButtonFore = Color.Black;
                    baseColors.ScrollBarBack = Color.LawnGreen;
                    baseColors.ScrollBarFore = Color.Black;
                    baseColors.AxisBack = Color.GreenYellow;
                    baseColors.AxisFore = Color.Black;

                    baseColors.MorphDisableBack = Color.Gray;
                    baseColors.MorphDisableFore = Color.Gray;
                    baseColors.MorphDisableRatio = 0.25f;
                    baseColors.MorphHotBack = Color.Yellow;
                    baseColors.MorphHotFore = Color.DarkBlue;
                    baseColors.MorphHotRatio = 0.25f;
                    baseColors.MorphDownBack = Color.DarkCyan;
                    baseColors.MorphDownFore = Color.Goldenrod;
                    baseColors.MorphDownRatio = 0.65f;

                    break;
                case PaletteSkinType.CurrentWindows:
                default:


                    break;
            }
            return baseColors;
        }
        /// <summary>
        /// Gets clone of current object
        /// </summary>
        public PaletteBaseColors Clone { get { return (PaletteBaseColors)this.MemberwiseClone(); } }
    }
    #endregion
    #region class PaletteItem : solve Explicit/Implicit color of specified ItemType and ColorPalette.ColorFor(); classes Palette*Set
    /// <summary>
    /// PaletteItem : solve Explicit/Implicit color of specified ItemType and ColorPalette.ColorFor()
    /// </summary>
    public class PaletteItem
    {
        public PaletteItem(PaletteItemType itemType)
        {
            this.ItemType = itemType;
        }
        /// <summary>
        /// Type of this color item. 
        /// When Color property is null, then CurrentColor is taken from ColorPalette.ColorFor() for this ItemType.
        /// If Color property is explicitly assigned (HasValue), then CurrentColor is this explicit color, not from palette.
        /// </summary>
        public PaletteItemType ItemType { get; private set; }
        /// <summary>
        /// Explicit color.
        /// Can be null, then CurrentColor is default for this.ItemType (is taken from ColorPalette.ColorFor()).
        /// </summary>
        public Color? Color { get; set; }
        /// <summary>
        /// Current color: is this.Color.Value, when HasValue, or ColorPalette.ColorFor(this.ItemType).
        /// </summary>
        public Color CurrentColor { get { return (this.Color.HasValue ? this.Color.Value : ColorPalette.ColorFor(this.ItemType)); } }
        /// <summary>
        /// Brush for this palette item.
        /// Must be used in using() { } pattern, here is generated allways new instance (even for default color from ColorPalette system) !
        /// </summary>
        public Brush GetCurrentBrush() { return new SolidBrush(this.Color.HasValue ? this.Color.Value : ColorPalette.ColorFor(this.ItemType)); }
        /// <summary>
        /// Pen for this palette item.
        /// Must be used in using() { } pattern, here is generated allways new instance (even for default color from ColorPalette system) !
        /// </summary>
        public Pen GetCurrentPen() { return new Pen(this.Color.HasValue ? this.Color.Value : ColorPalette.ColorFor(this.ItemType)); }
    }
    public class PaletteSet
    {
        public PaletteSet(PaletteItemType backDisable, PaletteItemType foreDisable, PaletteItemType backEnable, PaletteItemType foreEnable, 
                          PaletteItemType backHot, PaletteItemType foreHot, PaletteItemType backDown, PaletteItemType foreDown)
        {
            this.BackDisable = new PaletteItem(backDisable);
            this.ForeDisable = new PaletteItem(foreDisable);
            this.BackEnable = new PaletteItem(backEnable);
            this.ForeEnable = new PaletteItem(foreEnable);
            this.BackHot = new PaletteItem(backHot);
            this.ForeHot = new PaletteItem(foreHot);
            this.BackDown = new PaletteItem(backDown);
            this.ForeDown = new PaletteItem(foreDown);
        }
        /// <summary>
        /// Back Color for Disabled item
        /// </summary>
        public PaletteItem BackDisable { get; private set; }
        /// <summary>
        /// Fore Color for Disabled item
        /// </summary>
        public PaletteItem ForeDisable { get; private set; }
        /// <summary>
        /// Back Color for Normal (None mouse) state, on Enabled item
        /// </summary>
        public PaletteItem BackEnable { get; private set; }
        /// <summary>
        /// Fore Color for Normal (None mouse) state, on Enabled item
        /// </summary>
        public PaletteItem ForeEnable { get; private set; }
        /// <summary>
        /// Back Color for MouseOver state
        /// </summary>
        public PaletteItem BackHot { get; private set; }
        /// <summary>
        /// Fore Color for MouseOver state
        /// </summary>
        public PaletteItem ForeHot { get; private set; }
        /// <summary>
        /// Back Color for MouseDown and MouseDrag state
        /// </summary>
        public PaletteItem BackDown { get; private set; }
        /// <summary>
        /// Fore Color for MouseDown and MouseDrag state
        /// </summary>
        public PaletteItem ForeDown { get; private set; }
        /// <summary>
        /// Returns CurrentColor from this set, from Back-Item for specified (state).
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public Color GetBackColorForState(GInteractiveState state)
        {
            return this.GetBackItemForState(state).CurrentColor;
        }
        /// <summary>
        /// Returns CurrentColor from this set, from Fore-Item for specified (state).
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public Color GetForeColorForState(GInteractiveState state)
        {
            return this.GetForeItemForState(state).CurrentColor;
        }
        /// <summary>
        /// Returns appropriate Back-Item from this set, for specified (state).
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public PaletteItem GetBackItemForState(GInteractiveState state)
        {
            switch (state)
            {
                case GInteractiveState.None: return this.BackEnable;
                case GInteractiveState.MouseOver: return this.BackHot;
                case GInteractiveState.LeftDown:
                case GInteractiveState.RightDown:
                case GInteractiveState.LeftFrame:
                case GInteractiveState.RightFrame:
                case GInteractiveState.LeftDrag:
                case GInteractiveState.RightDrag: return this.BackDown;
            }
            return this.BackDisable;
        }
        /// <summary>
        /// Returns appropriate Fore-Item from this set, for specified (state).
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public PaletteItem GetForeItemForState(GInteractiveState state)
        {
            switch (state)
            {
                case GInteractiveState.None: return this.ForeEnable;
                case GInteractiveState.MouseOver: return this.ForeHot;
                case GInteractiveState.LeftDown: 
                case GInteractiveState.RightDown:
                case GInteractiveState.LeftFrame:
                case GInteractiveState.RightFrame:
                case GInteractiveState.LeftDrag:
                case GInteractiveState.RightDrag: return this.ForeDown;
            }
            return this.ForeDisable;
        }
    }
    public class PaletteButtonSet : PaletteSet
    {
        public PaletteButtonSet()
            : base(PaletteItemType.ButtonBackDisable, PaletteItemType.ButtonForeDisable, PaletteItemType.ButtonBackEnable, PaletteItemType.ButtonForeEnable,
                   PaletteItemType.ButtonBackHot, PaletteItemType.ButtonForeHot, PaletteItemType.ButtonBackDown, PaletteItemType.ButtonForeDown)
        { }
    }
    public class PaletteScrollBarSet : PaletteSet
    {
        public PaletteScrollBarSet()
            : base(PaletteItemType.ScrollBarBackDisable, PaletteItemType.ScrollBarForeDisable, PaletteItemType.ScrollBarBackEnable, PaletteItemType.ScrollBarForeEnable,
                   PaletteItemType.ScrollBarBackHot, PaletteItemType.ScrollBarForeHot, PaletteItemType.ScrollBarBackDown, PaletteItemType.ScrollBarForeDown)
        { }
    }
    public class PaletteAxisSet : PaletteSet
    {
        public PaletteAxisSet()
            : base(PaletteItemType.AxisBackDisable, PaletteItemType.AxisForeDisable, PaletteItemType.AxisBackEnable, PaletteItemType.AxisForeEnable,
                   PaletteItemType.AxisBackHot, PaletteItemType.AxisForeHot, PaletteItemType.AxisBackDown, PaletteItemType.AxisForeDown)
        { }
    }
    #endregion
}
