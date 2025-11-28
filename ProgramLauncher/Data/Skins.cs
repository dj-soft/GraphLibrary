using DjSoft.Tools.ProgramLauncher.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DjSoft.Tools.ProgramLauncher.App;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    #region class AppearanceInfo
    /// <summary>
    /// Sada definující jeden druh vzhledu (jeden "skin")
    /// </summary>
    public partial class AppearanceInfo : IMenuItem
    {
        #region Public properties
        /// <summary>
        /// Jméno sady
        /// </summary>
        public string Name { get { return __Name; } set { if (!__IsReadOnly) __Name = value; } } private string __Name;
        /// <summary>
        /// Ikona do menu, malá
        /// </summary>
        public Image ImageSmall { get { return __ImageSmall; } set { if (!__IsReadOnly) __ImageSmall = value; } } private Image __ImageSmall;
        /// <summary>
        /// Pořadí v nabídce
        /// </summary>
        private int SortOrder { get { return __SortOrder; } } private int __SortOrder;
        /// <summary>
        /// Barva statického pozadí pod všemi prvky = celé okno
        /// </summary>
        public Color WorkspaceColor { get { return __WorkspaceColor; } set { if (!__IsReadOnly) __WorkspaceColor = value; } } private Color __WorkspaceColor;
        /// <summary>
        /// Barva statického pozadí pod ToolStripy (Toolbar, Statusbar)
        /// </summary>
        public Color ToolStripColor { get { return __ToolStripColor; } set { if (!__IsReadOnly) __ToolStripColor = value; } } private Color __ToolStripColor;
        /// <summary>
        /// Barvy pozadí celé buňky. Pokud obsahuje null, nekreslí se.
        /// </summary>
        public ColorSet CellBackColor { get { return __CellBackColor; } set { if (!__IsReadOnly) __CellBackColor = value; } } private ColorSet __CellBackColor;
        /// <summary>
        /// Barvy aktivního prostoru. Nepoužívá se pro stav Enabled a Disabled, pouze MouseOn a MouseDown.
        /// </summary>
        public ColorSet ActiveContentColor { get { return __ActiveContentColor; } set { if (!__IsReadOnly) __ActiveContentColor = value; } } private ColorSet __ActiveContentColor;
        /// <summary>
        /// Sada barev pro čáru HeaderLine, kreslí se když její souřadnice jsou zadané.
        /// </summary>
        public ColorSet HeaderLineColors { get { return __HeaderLineColors; } set { if (!__IsReadOnly) __HeaderLineColors = value; } } private ColorSet __HeaderLineColors;
        /// <summary>
        /// Sada barev pro čáru Border, kreslí se když <see cref="BorderWidth"/> je kladné
        /// </summary>
        public ColorSet BorderLineColors { get { return __BorderLineColors; } set { if (!__IsReadOnly) __BorderLineColors = value; } } private ColorSet __BorderLineColors;
        /// <summary>
        /// Sada barev pro pozadí pod buttonem, ohraničený prostorem Border
        /// </summary>
        public ColorSet ButtonBackColors { get { return __ButtonBackColors; } set { if (!__IsReadOnly) __ButtonBackColors = value; } } private ColorSet __ButtonBackColors;
        /// <summary>
        /// Sada barev pro MainTitle
        /// </summary>
        public ColorSet MainTitleColors { get { return __MainTitleColors; } set { if (!__IsReadOnly) __MainTitleColors = value; } } private ColorSet __MainTitleColors;
        /// <summary>
        /// Sada barev pro SubTitle
        /// </summary>
        public ColorSet SubTitleColors { get { return __SubTitleColors; } set { if (!__IsReadOnly) __SubTitleColors = value; } } private ColorSet __SubTitleColors;
        /// <summary>
        /// Sada barev pro běžné texty
        /// </summary>
        public ColorSet StandardTextColors { get { return __TextStandardColors; } set { if (!__IsReadOnly) __TextStandardColors = value; } } private ColorSet __TextStandardColors;

        /// <summary>
        /// Hodnota průhlednosti pro kreslení přesouvaného prvku v režimu Mouse DragAndDrop.
        /// 0 = neviditelný / 1 = plně viditelný. Defaultní = 0.45
        /// </summary>
        public float MouseDragActiveCurrentAlpha { get { return __MouseDragActiveCurrentAlpha; } set { if (!__IsReadOnly) __MouseDragActiveCurrentAlpha = (value < 0f ? 0f : (value > 1f ? 1f : value)); } } private float __MouseDragActiveCurrentAlpha;

        /// <summary>
        /// Vzhled velkého titulku
        /// </summary>
        public TextAppearance MainTitleAppearance { get { return __MainTitleAppearance; } set { if (!__IsReadOnly) __MainTitleAppearance = value; } } private TextAppearance __MainTitleAppearance;
        /// <summary>
        /// Vzhled pod-titulku
        /// </summary>
        public TextAppearance SubTitleAppearance { get { return __SubTitleAppearance; } set { if (!__IsReadOnly) __SubTitleAppearance = value; } } private TextAppearance __SubTitleAppearance;
        /// <summary>
        /// Vzhled standardního textu
        /// </summary>
        public TextAppearance StandardTextAppearance { get { return __StandardTextAppearance; } set { if (!__IsReadOnly) __StandardTextAppearance = value; } } private TextAppearance __StandardTextAppearance;
        /// <summary>
        /// Data jsou ReadOnly?
        /// Lze setovat pouze tehdy, když dosavadní hodnota je false.
        /// Tzn.: lze vytvořit new instanci, následně naplnit její hodnoty a na závěr ji nastavit <see cref="IsReadOnly"/> = true. Poté již nelze setovat nic dalšího, bude ignorováno.
        /// </summary>
        public bool IsReadOnly { get { return __IsReadOnly; } set { if (!__IsReadOnly) __IsReadOnly = value; } } private bool __IsReadOnly;
        /// <summary>
        /// Konstruktor
        /// </summary>
        public AppearanceInfo()
        {
            __MouseDragActiveCurrentAlpha = 0.45f;
        }
        #endregion
        #region Dynamické získání ColorSet a TextAppearance podle PaletteColorPartType a AppearanceTextPartType
        /// <summary>
        /// Vrátí barevnou sadu daného typu
        /// </summary>
        public ColorSet GetColorSet(AppearanceColorPartType part)
        {
            switch (part)
            {
                case AppearanceColorPartType.ContentColor: return ActiveContentColor;
                case AppearanceColorPartType.BorderLineColors: return BorderLineColors;
                case AppearanceColorPartType.ButtonBackColors: return ButtonBackColors;
                case AppearanceColorPartType.MainTitleColors: return MainTitleColors;
                case AppearanceColorPartType.SubTitleColors: return SubTitleColors;
                case AppearanceColorPartType.StandardTextColors: return StandardTextColors;
            }
            return ActiveContentColor;
        }
        /// <summary>
        /// Vrátí definici vzhledu textu daného typu
        /// </summary>
        public TextAppearance GetTextAppearance(AppearanceTextPartType part)
        {
            switch (part)
            {
                case AppearanceTextPartType.MainTitle: return MainTitleAppearance;
                case AppearanceTextPartType.SubTitle: return SubTitleAppearance;
                case AppearanceTextPartType.StandardText: return StandardTextAppearance;
            }
            return StandardTextAppearance; ;
        }
        #endregion
        #region Statické konstruktory konkrétních stylů
        /// <summary>
        /// Kolekce všech standardních i přidaných definic
        /// </summary>
        public static AppearanceInfo[] Collection { get { return _Collection.Values.ToArray(); } }
        public static AppearanceInfo Default { get { return GetItem(_DefaultName); } }
        private const string _DefaultName = "Default";
        /// <summary>
        /// Vrátí prvek daného jména
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AppearanceInfo GetItem(string name, bool useDefault = false)
        {
            var collection = _Collection;
            if (!String.IsNullOrEmpty(name) && collection.TryGetValue(name, out var item)) return item;
            if (useDefault) return collection[_DefaultName];
            return null;
        }
        /// <summary>
        /// Vrátí true, pokud existuje prvek daného jména
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool Contains(string name) { return _Collection.ContainsKey(name); }
        /// <summary>
        /// List všech standardních i přidaných definic, autoinicializační, metodou <see cref="_CreateAllAppearances()"/>
        /// </summary>
        private static Dictionary<string, AppearanceInfo> _Collection
        {
            get
            {
                if (__Collection is null)
                    __Collection = _CreateAllAppearances();
                return __Collection;
            }
        }
        /// <summary>
        /// Dictionary všech standardních i přidaných definic, proměnná
        /// </summary>
        private static Dictionary<string, AppearanceInfo> __Collection;
        /// <summary>
        /// Vytvoří a vrátí List všech standardních i přidaných definic
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, AppearanceInfo> _CreateAllAppearances()
        {
            List<AppearanceInfo> list = new List<AppearanceInfo>();
            var methods = typeof(AppearanceInfo).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.NonPublic);
            foreach (var method in methods) 
            {
                if (!method.IsSpecialName && method.ReturnType == typeof(AppearanceInfo) && method.GetParameters().Length == 0)
                {
                    if (method.Invoke(null, new object[] {}) is AppearanceInfo info)
                        list.Add(info);
                }
            }
            list.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            var collection = list.CreateDictionary(i => i.Name, true);
            return collection;
        }
        /// <summary>
        /// Vytvoří new instanci palety "Default"
        /// </summary>
        /// <returns></returns>
        private static AppearanceInfo _CreateDefault()
        {
            var paletteSet = new AppearanceInfo();
            paletteSet.__Name = _DefaultName;
            paletteSet.__ImageSmall = Properties.Resources.btn_g2_20;
            paletteSet.__SortOrder = 100;
            paletteSet.__WorkspaceColor = Color.FromArgb(64, 68, 72);
            paletteSet.__ToolStripColor = Color.FromArgb(82, 86, 90);

            int a0 = 40;
            int a1 = 80;
            int a2 = 120;
            int a3 = 160;

            int b1 = 180;
            int b2 = 200;

            paletteSet.__CellBackColor = ColorSet.CreateAllColors(true, null, selectedColor: Color.FromArgb(a2, 160, 160, 160));
            paletteSet.__ActiveContentColor = ColorSet.CreateAllColors(true, null,
                downColor: Color.FromArgb(255, 240, 240, 190),
                mouseOnColor: Color.FromArgb(a0, 200, 200, 230),
                mouseDownColor: Color.FromArgb(a0, 180, 180, 210),
                mouseOn3DMorph: 0.15f,
                mouseDown3DMorph: -0.20f);
            paletteSet.__BorderLineColors = new ColorSet(true,
                Color.FromArgb(a1, b1, b1, b1),
                Color.FromArgb(a1, b1, b1, b1),
                Color.FromArgb(a1, b1, b1, b1),
                Color.FromArgb(a1, b2, b2, b2),
                Color.FromArgb(a1, b2, b2, b2),
                Color.FromArgb(a1, b2, b2, b2),
                Color.FromArgb(a1, b2, b2, b2));
            paletteSet.__ButtonBackColors = new ColorSet(true,
                Color.FromArgb(a1, 120, 120, 120),
                Color.FromArgb(a1, 216, 216, 216),
                Color.FromArgb(a1, 216, 216, 216),
                Color.FromArgb(a2, 200, 200, 230),
                Color.FromArgb(a2, 180, 180, 210),
                Color.FromArgb(a3, 180, 180, 240),
                Color.FromArgb(a2, 160, 160, 160));
            paletteSet.__MainTitleColors = ColorSet.CreateAllColors(true, Color.Black);
            paletteSet.__SubTitleColors = ColorSet.CreateAllColors(true, Color.Black);
            paletteSet.__TextStandardColors = ColorSet.CreateAllColors(true, Color.Black);

            paletteSet.__MainTitleAppearance = new TextAppearance(true,
                SystemFontType.CaptionFont,
                ContentAlignment.MiddleLeft,
                AppearanceColorPartType.MainTitleColors,
                null,
                new TextInteractiveStyle(true, Components.InteractiveState.Enabled, null, 1.2f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseOn, null, 1.3f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseDown, null, 1.3f, FontStyle.Bold)
                );

            paletteSet.__SubTitleAppearance = new TextAppearance(true,
                SystemFontType.CaptionFont,
                ContentAlignment.MiddleLeft,
                AppearanceColorPartType.MainTitleColors,
                null,
                new TextInteractiveStyle(true, Components.InteractiveState.Enabled, null, 1.1f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseOn, null, 1.2f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseDown, null, 1.2f, FontStyle.Bold)
                );

            paletteSet.__StandardTextAppearance = new TextAppearance(true,
                SystemFontType.CaptionFont,
                ContentAlignment.MiddleLeft,
                AppearanceColorPartType.MainTitleColors,
                null,
                new TextInteractiveStyle(true, Components.InteractiveState.Enabled, null, 1.0f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseOn, null, 1.1f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseDown, null, 1.1f, FontStyle.Bold)
                );

            paletteSet.__IsReadOnly = true;

            return paletteSet;
        }
        /// <summary>
        /// Vytvoří new instanci palety "DarkBlue"
        /// </summary>
        /// <returns></returns>
        private static AppearanceInfo _CreateDarkBlue()
        {
            var paletteSet = new AppearanceInfo();
            paletteSet.__Name = "DarkBlue";
            paletteSet.__ImageSmall = Properties.Resources.btn_09_20;
            paletteSet.__SortOrder = 500;
            paletteSet.__WorkspaceColor = Color.FromArgb(16, 22, 40);
            paletteSet.__ToolStripColor = Color.FromArgb(24, 28, 56);

            int a0 = 40;
            int a1 = 80;
            int a2 = 120;
            int a3 = 160;

            int b1 = 16;
            int b2 = 32;

            paletteSet.__CellBackColor = ColorSet.CreateAllColors(true, null, selectedColor: Color.FromArgb(a2, 90, 90, 180));
            paletteSet.__ActiveContentColor = ColorSet.CreateAllColors(true, null,
                downColor: Color.FromArgb(255, 48, 48, 96),
                mouseOnColor: Color.FromArgb(a0, 32, 32, 48),
                mouseDownColor: Color.FromArgb(a0, 40, 40, 64),
                mouseOn3DMorph: 0.15f,
                mouseDown3DMorph: -0.20f);
            paletteSet.__BorderLineColors = new ColorSet(true,
                Color.FromArgb(a1, b1, b1, b1),
                Color.FromArgb(a1, b1, b1, b1),
                Color.FromArgb(a1, b1, b1, b1),
                Color.FromArgb(a1, b2, b2, b2),
                Color.FromArgb(a1, b2, b2, b2),
                Color.FromArgb(a1, b2, b2, b2),
                Color.FromArgb(a1, b2, b2, b2));
            paletteSet.__ButtonBackColors = new ColorSet(true,
                Color.FromArgb(a1, 24, 24, 24),
                Color.FromArgb(a1, 0, 0, 32),
                Color.FromArgb(a1, 48, 48, 96),
                Color.FromArgb(a2, 32, 32, 64),
                Color.FromArgb(a2, 40, 40, 72),
                Color.FromArgb(a3, 40, 40, 96),
                Color.FromArgb(a2, 90, 90, 180));
            paletteSet.__MainTitleColors = ColorSet.CreateAllColors(true, Color.White);
            paletteSet.__SubTitleColors = ColorSet.CreateAllColors(true, Color.White);
            paletteSet.__TextStandardColors = ColorSet.CreateAllColors(true, Color.White);

            paletteSet.__MainTitleAppearance = new TextAppearance(true,
                SystemFontType.CaptionFont,
                ContentAlignment.MiddleLeft,
                AppearanceColorPartType.MainTitleColors,
                null,
                new TextInteractiveStyle(true, Components.InteractiveState.Enabled, null, 1.2f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseOn, null, 1.3f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseDown, null, 1.3f, FontStyle.Bold)
                );

            paletteSet.__SubTitleAppearance = new TextAppearance(true,
                SystemFontType.CaptionFont,
                ContentAlignment.MiddleLeft,
                AppearanceColorPartType.MainTitleColors,
                null,
                new TextInteractiveStyle(true, Components.InteractiveState.Enabled, null, 1.1f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseOn, null, 1.2f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseDown, null, 1.2f, FontStyle.Bold)
                );

            paletteSet.__StandardTextAppearance = new TextAppearance(true,
                SystemFontType.CaptionFont,
                ContentAlignment.MiddleLeft,
                AppearanceColorPartType.MainTitleColors,
                null,
                new TextInteractiveStyle(true, Components.InteractiveState.Enabled, null, 1.0f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseOn, null, 1.1f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseDown, null, 1.1f, FontStyle.Bold)
                );

            paletteSet.__IsReadOnly = true;
            return paletteSet;
        }
        /// <summary>
        /// Vytvoří new instanci palety "LightBlue"
        /// </summary>
        /// <returns></returns>
        private static AppearanceInfo _CreateLightBlue()
        {
            var paletteSet = new AppearanceInfo();
            paletteSet.__Name = "LightBlue";
            paletteSet.__ImageSmall = Properties.Resources.btn_05_20;
            paletteSet.__SortOrder = 500;
            paletteSet.__WorkspaceColor = Color.FromArgb(215, 220, 234);
            paletteSet.__ToolStripColor = Color.FromArgb(197, 206, 232);

            int a0 = 40;
            int a1 = 80;
            int a2 = 120;
            int a3 = 160;

            int b1 = 16;
            int b2 = 32;

            paletteSet.__CellBackColor = ColorSet.CreateAllColors(true, null, selectedColor: Color.FromArgb(a2, 117, 132, 168));
            paletteSet.__ActiveContentColor = ColorSet.CreateAllColors(true, null,
                downColor: Color.FromArgb(255, 48, 48, 96),
                mouseOnColor: Color.FromArgb(a0, 32, 32, 48),
                mouseDownColor: Color.FromArgb(a0, 40, 40, 64),
                mouseOn3DMorph: 0.15f,
                mouseDown3DMorph: -0.20f);
            paletteSet.__BorderLineColors = new ColorSet(true,
                Color.FromArgb(a1, b1, b1, b1),
                Color.FromArgb(a1, b1, b1, b1),
                Color.FromArgb(a1, b1, b1, b1),
                Color.FromArgb(a1, b2, b2, b2),
                Color.FromArgb(a1, b2, b2, b2),
                Color.FromArgb(a1, b2, b2, b2),
                Color.FromArgb(a1, b2, b2, b2));
            paletteSet.__ButtonBackColors = new ColorSet(true,
                Color.FromArgb(a1, 24, 24, 24),
                Color.FromArgb(a1, 0, 0, 32),
                Color.FromArgb(a1, 176, 191, 232),
                Color.FromArgb(a2, 162, 180, 232),
                Color.FromArgb(a2, 148, 170, 232),
                Color.FromArgb(a3, 120, 150, 232),
                Color.FromArgb(a2, 117, 132, 168));
            paletteSet.__MainTitleColors = ColorSet.CreateAllColors(true, Color.Black);
            paletteSet.__SubTitleColors = ColorSet.CreateAllColors(true, Color.Black);
            paletteSet.__TextStandardColors = ColorSet.CreateAllColors(true, Color.Black);

            paletteSet.__MainTitleAppearance = new TextAppearance(true,
                SystemFontType.CaptionFont,
                ContentAlignment.MiddleLeft,
                AppearanceColorPartType.MainTitleColors,
                null,
                new TextInteractiveStyle(true, Components.InteractiveState.Enabled, null, 1.2f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseOn, null, 1.3f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseDown, null, 1.3f, FontStyle.Bold)
                );

            paletteSet.__SubTitleAppearance = new TextAppearance(true,
                SystemFontType.CaptionFont,
                ContentAlignment.MiddleLeft,
                AppearanceColorPartType.MainTitleColors,
                null,
                new TextInteractiveStyle(true, Components.InteractiveState.Enabled, null, 1.1f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseOn, null, 1.2f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseDown, null, 1.2f, FontStyle.Bold)
                );

            paletteSet.__StandardTextAppearance = new TextAppearance(true,
                SystemFontType.CaptionFont,
                ContentAlignment.MiddleLeft,
                AppearanceColorPartType.MainTitleColors,
                null,
                new TextInteractiveStyle(true, Components.InteractiveState.Enabled, null, 1.0f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseOn, null, 1.1f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseDown, null, 1.1f, FontStyle.Bold)
                );

            paletteSet.__IsReadOnly = true;
            return paletteSet;
        }
        /// <summary>
        /// Vytvoří new instanci palety "LightBlue"
        /// </summary>
        /// <returns></returns>
        private static AppearanceInfo _CreateLightGreen()
        {
            var paletteSet = new AppearanceInfo();
            paletteSet.__Name = "LightGreen";
            paletteSet.__ImageSmall = Properties.Resources.btn_22_20;
            paletteSet.__SortOrder = 600;
            paletteSet.__WorkspaceColor = Color.FromArgb(206, 255, 206);
            paletteSet.__ToolStripColor = Color.FromArgb(181, 224, 181);

            int a0 = 40;
            int a1 = 80;
            int a2 = 120;
            int a3 = 160;

            int b1 = 16;
            int b2 = 32;

            paletteSet.__CellBackColor = ColorSet.CreateAllColors(true, null, selectedColor: Color.FromArgb(a2, 123, 175, 135));
            paletteSet.__ActiveContentColor = ColorSet.CreateAllColors(true, null,
                downColor: Color.FromArgb(255, 48, 48, 96),
                mouseOnColor: Color.FromArgb(a0, 32, 48, 32),
                mouseDownColor: Color.FromArgb(a0, 40, 64, 40),
                mouseOn3DMorph: 0.15f,
                mouseDown3DMorph: -0.20f);
            paletteSet.__BorderLineColors = new ColorSet(true,
                Color.FromArgb(a1, b1, b1, b1),
                Color.FromArgb(a1, b1, b1, b1),
                Color.FromArgb(a1, b1, b1, b1),
                Color.FromArgb(a1, b2, b2, b2),
                Color.FromArgb(a1, b2, b2, b2),
                Color.FromArgb(a1, b2, b2, b2),
                Color.FromArgb(a1, b2, b2, b2));
            paletteSet.__ButtonBackColors = new ColorSet(true,
                Color.FromArgb(a1, 24, 24, 24),
                Color.FromArgb(a1, 0, 0, 32),
                Color.FromArgb(a1, 176, 232, 191),
                Color.FromArgb(a2, 162, 232, 180),
                Color.FromArgb(a2, 148, 232, 170),
                Color.FromArgb(a3, 120, 232, 150),
                Color.FromArgb(a2, 123, 175, 135));
            paletteSet.__MainTitleColors = ColorSet.CreateAllColors(true, Color.Black);
            paletteSet.__SubTitleColors = ColorSet.CreateAllColors(true, Color.Black);
            paletteSet.__TextStandardColors = ColorSet.CreateAllColors(true, Color.Black);

            paletteSet.__MainTitleAppearance = new TextAppearance(true,
                SystemFontType.CaptionFont,
                ContentAlignment.MiddleLeft,
                AppearanceColorPartType.MainTitleColors,
                null,
                new TextInteractiveStyle(true, Components.InteractiveState.Enabled, null, 1.2f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseOn, null, 1.3f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseDown, null, 1.3f, FontStyle.Bold)
                );

            paletteSet.__SubTitleAppearance = new TextAppearance(true,
                SystemFontType.CaptionFont,
                ContentAlignment.MiddleLeft,
                AppearanceColorPartType.MainTitleColors,
                null,
                new TextInteractiveStyle(true, Components.InteractiveState.Enabled, null, 1.1f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseOn, null, 1.2f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseDown, null, 1.2f, FontStyle.Bold)
                );

            paletteSet.__StandardTextAppearance = new TextAppearance(true,
                SystemFontType.CaptionFont,
                ContentAlignment.MiddleLeft,
                AppearanceColorPartType.MainTitleColors,
                null,
                new TextInteractiveStyle(true, Components.InteractiveState.Enabled, null, 1.0f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseOn, null, 1.1f, null),
                new TextInteractiveStyle(true, Components.InteractiveState.MouseDown, null, 1.1f, FontStyle.Bold)
                );

            paletteSet.__IsReadOnly = true;
            return paletteSet;
        }
        #endregion
        #region Implementace IMenuItem
        string IMenuItem.Text { get { return this.Name; } }
        Image IMenuItem.Image { get { return this.ImageSmall; } }
        object IMenuItem.Code { get { return this.Name; } }
        string IMenuItem.ToolTip { get { return null; } }
        MenuItemType IMenuItem.ItemType { get { return MenuItemType.Button; } }
        bool IMenuItem.Enabled { get { return true; } }
        FontStyle? IMenuItem.FontStyle { get { return (Object.ReferenceEquals(this, App.CurrentAppearance) ? (FontStyle?)FontStyle.Bold : (FontStyle?)null); } }
        object IMenuItem.ToolItem { get; set; }
        object IMenuItem.UserData { get; set; }
        void IMenuItem.Process()
        {
            App.Settings.AppearanceName = this.Name;                 // Tato property zajistí i aktivaci this objektu do App objektu, není tedy třeba explicitně provádět: { App.CurrentAppearance = this; }
        }
        #endregion
    }
    /// <summary>
    /// Část barevné definice v paletě
    /// </summary>
    public enum AppearanceColorPartType
    {
        None,
        ContentColor,
        BorderLineColors,
        ButtonBackColors,
        MainTitleColors,
        SubTitleColors,
        StandardTextColors
    }
    /// <summary>
    /// Část definice vzhledu textu v paletě
    /// </summary>
    public enum AppearanceTextPartType
    {
        None,
        MainTitle,
        SubTitle,
        StandardText
    }
    #endregion
    #region class ColorSet
    /// <summary>
    /// Definice barev pro jednu oblast, liší se interaktivitou
    /// </summary>
    public class ColorSet
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ColorSet() { }
        /// <summary>
        /// Vytvoří a vrátí <see cref="ColorSet"/>, kdy lze zadat jen jednu barvu, která se aplikuje do všech hodnot (pro všechny interaktivní stavy),
        /// anebo lze zadat základní barvu a volitelně přepsat jen některé konkrétní stavy.
        /// </summary>
        /// <param name="allColors"></param>
        /// <param name="disabledColor"></param>
        /// <param name="enabledColor"></param>
        /// <param name="downColor"></param>
        /// <param name="mouseOnColor"></param>
        /// <param name="mouseDownColor"></param>
        /// <param name="mouseHighlightColor"></param>
        /// <returns></returns>
        public static ColorSet CreateAllColors(Color? allColors, 
            Color? disabledColor = null, Color? enabledColor = null, Color? downColor = null, Color? mouseOnColor = null, Color? mouseDownColor = null, Color? mouseHighlightColor = null, Color? selectedColor = null,
            float? enabled3DMorph = null, float? down3DMorph = null, float? mouseOn3DMorph = null, float? mouseDown3DMorph = null, float? selected3DMorph = null)
        {
            return new ColorSet(false,
                disabledColor ?? allColors, enabledColor ?? allColors, downColor ?? allColors, mouseOnColor ?? allColors, mouseDownColor ?? allColors, mouseHighlightColor ?? allColors, selectedColor ?? allColors,
                enabled3DMorph, down3DMorph, mouseOn3DMorph, mouseDown3DMorph, selected3DMorph);
        }
        /// <summary>
        /// Vytvoří a vrátí <see cref="ColorSet"/>, kdy lze zadat jen jednu barvu, která se aplikuje do všech hodnot (pro všechny interaktivní stavy),
        /// anebo lze zadat základní barvu a volitelně přepsat jen některé konkrétní stavy.
        /// </summary>
        /// <param name="isReadOnly"></param>
        /// <param name="allColors"></param>
        /// <param name="disabledColor"></param>
        /// <param name="enabledColor"></param>
        /// <param name="downColor"></param>
        /// <param name="mouseOnColor"></param>
        /// <param name="mouseDownColor"></param>
        /// <param name="mouseHighlightColor"></param>
        /// <returns></returns>
        public static ColorSet CreateAllColors(bool isReadOnly, Color? allColors, 
            Color? disabledColor = null, Color? enabledColor = null, Color? downColor = null, Color? mouseOnColor = null, Color? mouseDownColor = null, Color? mouseHighlightColor = null, Color? selectedColor = null,
            float? enabled3DMorph = null, float? down3DMorph = null, float? mouseOn3DMorph = null, float? mouseDown3DMorph = null, float? selected3DMorph = null)
        {
            return new ColorSet(isReadOnly, 
                disabledColor ?? allColors, enabledColor ?? allColors, downColor ?? allColors, mouseOnColor ?? allColors, mouseDownColor ?? allColors, mouseHighlightColor ?? allColors, selectedColor ?? allColors,
                enabled3DMorph, down3DMorph, mouseOn3DMorph, mouseDown3DMorph, selected3DMorph);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="disabledColor"></param>
        /// <param name="enabledColor"></param>
        /// <param name="mouseOnColor"></param>
        /// <param name="mouseDownColor"></param>
        /// <param name="mouseHighlightColor"></param>
        public ColorSet(Color? disabledColor, Color? enabledColor, Color? downColor, Color? mouseOnColor, Color? mouseDownColor, Color? mouseHighlightColor, Color? selectedColor)
        {
            this.__DisabledColor = disabledColor;
            this.__EnabledColor = enabledColor;
            this.__MouseOnColor = mouseOnColor;
            this.__MouseDownColor = mouseDownColor;
            this.__MouseHighlightColor = mouseHighlightColor;
            this.__DownColor = downColor;
            this.__SelectedColor = selectedColor;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="enabledColor"></param>
        /// <param name="disabledColor"></param>
        /// <param name="mouseOnColor"></param>
        /// <param name="mouseDownColor"></param>
        /// <param name="mouseHighlightColor"></param>
        public ColorSet(bool isReadOnly, 
            Color? disabledColor, Color? enabledColor, Color? downColor, Color? mouseOnColor, Color? mouseDownColor, Color? mouseHighlightColor, Color? selectedColor)
        {
            this.__DisabledColor = disabledColor;
            this.__EnabledColor = enabledColor;
            this.__MouseOnColor = mouseOnColor;
            this.__MouseDownColor = mouseDownColor;
            this.__MouseHighlightColor = mouseHighlightColor;
            this.__DownColor = downColor;
            this.__SelectedColor = selectedColor;
            this.__IsReadOnly = isReadOnly;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="enabledColor"></param>
        /// <param name="disabledColor"></param>
        /// <param name="mouseOnColor"></param>
        /// <param name="mouseDownColor"></param>
        /// <param name="mouseHighlightColor"></param>
        public ColorSet(bool isReadOnly, 
            Color? disabledColor, Color? enabledColor, Color? downColor, Color? mouseOnColor, Color? mouseDownColor, Color? mouseHighlightColor, Color? selectedColor,
            float? enabled3DMorph, float? down3DMorph, float? mouseOn3DMorph, float? mouseDown3DMorph, float? selected3DMorph)
        {
            this.__DisabledColor = disabledColor;
            this.__EnabledColor = enabledColor;
            this.__MouseOnColor = mouseOnColor;
            this.__MouseDownColor = mouseDownColor;
            this.__MouseHighlightColor = mouseHighlightColor;
            this.__DownColor = downColor;
            this.__SelectedColor = selectedColor;

            this.__Enabled3DMorph = enabled3DMorph;
            this.__Down3DMorph = down3DMorph;
            this.__MouseOn3DMorph = mouseOn3DMorph;
            this.__MouseDown3DMorph = mouseDown3DMorph;
            this.__Selected3DMorph = selected3DMorph;

            this.__IsReadOnly = isReadOnly;
        }

        /// <summary>
        /// Barva ve stavu Disabled = nedostupné
        /// </summary>
        public Color? DisabledColor { get { return __DisabledColor; } set { if (!__IsReadOnly) __DisabledColor = value; } } private Color? __DisabledColor;
        /// <summary>
        /// Barva ve stavu Enabled = bez myši, ale dostupné
        /// </summary>
        public Color? EnabledColor { get { return __EnabledColor; } set { if (!__IsReadOnly) __EnabledColor = value; } } private Color? __EnabledColor;
        /// <summary>
        /// 3D efekt pro vykreslení pozadí ve stavu Enabled; null = default = 0.00f = bez 3D efektu
        /// </summary>
        public float? Enabled3DMorph { get { return __Enabled3DMorph; } set { if (!__IsReadOnly) __Enabled3DMorph = value; } } private float? __Enabled3DMorph;
        /// <summary>
        /// Barva ve stavu MouseOn = myš je na prvku
        /// </summary>
        public Color? MouseOnColor { get { return __MouseOnColor; } set { if (!__IsReadOnly) __MouseOnColor = value; } } private Color? __MouseOnColor;
        /// <summary>
        /// 3D efekt pro vykreslení pozadí ve stavu MouseOn; null = default = 0.10f
        /// </summary>
        public float? MouseOn3DMorph { get { return __MouseOn3DMorph; } set { if (!__IsReadOnly) __MouseOn3DMorph = value; } } private float? __MouseOn3DMorph;
        /// <summary>
        /// Barva ve stavu MouseDown
        /// </summary>
        public Color? MouseDownColor { get { return __MouseDownColor; } set { if (!__IsReadOnly) __MouseDownColor = value; } } private Color? __MouseDownColor;
        /// <summary>
        /// 3D efekt pro vykreslení pozadí ve stavu MouseDown; null = default = -0.20f
        /// </summary>
        public float? MouseDown3DMorph { get { return __MouseDown3DMorph; } set { if (!__IsReadOnly) __MouseDown3DMorph = value; } } private float? __MouseDown3DMorph;
        /// <summary>
        /// Barva ve stavu <see cref="Components.InteractiveItem.IsDown"/> = trvale stisknutá (jako plošný CheckBox), je dáno stavem konkrétního prvku.
        /// </summary>
        public Color? DownColor { get { return __DownColor; } set { if (!__IsReadOnly) __DownColor = value; } } private Color? __DownColor;
        /// <summary>
        /// 3D efekt pro vykreslení pozadí ve stavu Down; null = default = 0f
        /// </summary>
        public float? Down3DMorph { get { return __Down3DMorph; } set { if (!__IsReadOnly) __Down3DMorph = value; } } private float? __Down3DMorph;
        /// <summary>
        /// Barva ve stavu Selected
        /// </summary>
        public Color? SelectedColor { get { return __SelectedColor; } set { if (!__IsReadOnly) __SelectedColor = value; } } private Color? __SelectedColor;
        /// <summary>
        /// 3D efekt pro vykreslení pozadí ve stavu Down; null = default = 0f
        /// </summary>
        public float? Selected3DMorph { get { return __Selected3DMorph; } set { if (!__IsReadOnly) __Selected3DMorph = value; } } private float? __Selected3DMorph;
        /// <summary>
        /// Barva zvýraznění prostoru myši (oválek pod kurzorem)
        /// </summary>
        public Color? MouseHighlightColor { get { return __MouseHighlightColor; } set { if (!__IsReadOnly) __MouseHighlightColor = value; } } private Color? __MouseHighlightColor;
        /// <summary>
        /// Data jsou ReadOnly?
        /// </summary>
        public bool IsReadOnly { get { return __IsReadOnly; } set { if (!__IsReadOnly) __IsReadOnly = value; } } private bool __IsReadOnly;
        /// <summary>
        /// Vrátí barvu pro daný stav
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public Color? GetColor(Components.InteractiveState state)
        {
            Color? color = null;

            var basicState = state & Components.InteractiveState.MaskBasicStates;
            switch (basicState)
            {
                case Components.InteractiveState.Disabled: color = this.DisabledColor; break;
                case Components.InteractiveState.Enabled: color = this.EnabledColor; break;
                case Components.InteractiveState.MouseOn: color = this.MouseOnColor; break;
                case Components.InteractiveState.MouseDown: color = this.MouseDownColor; break;
                case Components.InteractiveState.Dragged: color = this.MouseDownColor; break;
                default: color = this.EnabledColor; break;
            }

            if (state.HasFlag(Components.InteractiveState.AndDown)) color = color.Morph(this.DownColor);
            if (state.HasFlag(Components.InteractiveState.AndSelected)) color = color.Morph(this.SelectedColor);

            return color;
        }
    }
    #endregion
    #region class TextAppearance
    /// <summary>
    /// Vzhled textu - font, styl, velikost
    /// </summary>
    public class TextAppearance
    {
        /// <summary>
        /// Konstruktor pro editovatelnou instanci
        /// </summary>
        public TextAppearance() { }
        /// <summary>
        /// Konstruktor pro naplněnou instanci
        /// </summary>
        public TextAppearance(bool isReadOnly, SystemFontType? fontType, ContentAlignment textAlignment, AppearanceColorPartType textColorType, ColorSet textColors, params TextInteractiveStyle[] styles)
        {
            __FontType = fontType;
            __TextAlignment = textAlignment;
            __TextColorType = textColorType;
            __TextColors = textColors;
            __TextStyles = new TextInteractiveStyles(isReadOnly, styles);
            __IsReadOnly = isReadOnly;
        }
        /// <summary>
        /// Typ systémového fontu
        /// </summary>
        public SystemFontType? FontType { get { return __FontType; } set { if (!__IsReadOnly) __FontType = value; } } private SystemFontType? __FontType;
        /// <summary>
        /// Explicitně daná velikost, není ale nijak optimální definovat ji takto explicitně. 
        /// Lepší je definovat <see cref="SizeRatio"/>.
        /// </summary>
        public float? EmSize { get { return TextStyles[Components.InteractiveState.Enabled].EmSize; } set { if (!__IsReadOnly) TextStyles[Components.InteractiveState.Enabled].EmSize = value; } }
        /// <summary>
        /// Poměr velikosti aktuálního fontu ku fontu defaultnímu daného typu
        /// </summary>
        public float? SizeRatio { get { return TextStyles[Components.InteractiveState.Enabled].SizeRatio; } set { if (!__IsReadOnly) TextStyles[Components.InteractiveState.Enabled].SizeRatio = value; } }
        /// <summary>
        /// Styl fontu; default = dle systémového fontu
        /// </summary>
        public FontStyle? FontStyle { get { return TextStyles[Components.InteractiveState.Enabled].FontStyle; } set { if (!__IsReadOnly) TextStyles[Components.InteractiveState.Enabled].FontStyle = value; } }
        /// <summary>
        /// Umístění textu v jeho prostoru
        /// </summary>
        public ContentAlignment TextAlignment { get { return __TextAlignment; } set { if (!__IsReadOnly) __TextAlignment = value; } } private ContentAlignment __TextAlignment;
        /// <summary>
        /// Barvy písma - zdrojové místo v paletě
        /// </summary>
        public AppearanceColorPartType TextColorType { get { return __TextColorType; } set { if (!__IsReadOnly) __TextColorType = value; } } private AppearanceColorPartType __TextColorType;
        /// <summary>
        /// Barvy písma, lze zadat i explicitně
        /// </summary>
        public ColorSet TextColors
        {
            get 
            {
                var textColors = __TextColors;
                if (textColors is null)
                    textColors = App.CurrentAppearance.GetColorSet(this.TextColorType);
                return textColors;
            }
            set
            {
                if (!__IsReadOnly)
                    __TextColors = value;
            }
        }
        private ColorSet __TextColors;
        /// <summary>
        /// Data jsou ReadOnly?
        /// </summary>
        public bool IsReadOnly { get { return __IsReadOnly; } set { if (!__IsReadOnly) __IsReadOnly = value; } } private bool __IsReadOnly;
        /// <summary>
        /// Styl textu pro daný interaktivní stav (použij indexer s indexem type <see cref="InteractiveState"/>).
        /// Na výstupu není nikdy null - každý stav má svůj styl.
        /// </summary>
        public TextInteractiveStyles TextStyles 
        {
            get 
            { 
                if (__TextStyles is null) 
                    __TextStyles = new TextInteractiveStyles(); 
                return __TextStyles;
            }
        }
        private TextInteractiveStyles __TextStyles;
    }
    /// <summary>
    /// Sada interaktivních stylů
    /// </summary>
    public class TextInteractiveStyles
    {
        /// <summary>
        /// Konstruktor pro editovatelnou instanci
        /// </summary>
        public TextInteractiveStyles() { }
        /// <summary>
        /// Konstruktor pro naplněnou instanci
        /// </summary>
        public TextInteractiveStyles(bool isReadOnly, params TextInteractiveStyle[] styles)
        {
            var dict = _Styles;

            foreach ( var style in styles ) 
            {
                if (style != null && !dict.ContainsKey(style.InteractiveState))
                    dict.Add(style.InteractiveState, style);
            }

            __IsReadOnly = isReadOnly;
        }
        /// <summary>
        /// Data odpovídající danému stavu
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public TextInteractiveStyle this[Components.InteractiveState state]
        {
            get
            {
                state &= Components.InteractiveState.MaskBasicStates;
                var styles = _Styles;
                if (!styles.TryGetValue(state, out var style))
                {
                    if (__IsReadOnly)
                    {   // Jsme ReadOnly: pro neexistující stav nebudu nic přidávat, ale použiju prázdný readonly styl:
                        style = TextInteractiveStyle.Empty;
                    }
                    else
                    {   // Nejsme ReadOnly: mohu přidat novou editovatelnou položku pro daný stav:
                        style = new TextInteractiveStyle();
                        __Styles.Add(state, style);
                    }
                }
                return style;
            }
        }
        /// <summary>
        /// Dictionary obsahující styly
        /// </summary>
        private Dictionary<Components.InteractiveState, TextInteractiveStyle> _Styles
        {
            get
            {
                if (__Styles is null) __Styles = new Dictionary<Components.InteractiveState, TextInteractiveStyle>();
                return __Styles;
            }
        }
        private Dictionary<Components.InteractiveState, TextInteractiveStyle> __Styles;
        /// <summary>
        /// Data jsou ReadOnly?
        /// </summary>
        public bool IsReadOnly { get { return __IsReadOnly; } set { if (!__IsReadOnly) __IsReadOnly = value; } } private bool __IsReadOnly;
    }
    /// <summary>
    /// Modifikátor stylu písma pro konkrétní interaktivní stav
    /// </summary>
    public class TextInteractiveStyle
    {
        /// <summary>
        /// Konstruktor pro editovatelnou instanci
        /// </summary>
        public TextInteractiveStyle() { }
        /// <summary>
        /// Konstruktor pro naplněnou instanci
        /// </summary>
        public TextInteractiveStyle(bool isReadOnly, Components.InteractiveState interactiveState, float? emSize, float? sizeRatio, FontStyle? fontStyle)
        {
            __InteractiveState = interactiveState;
            __EmSize = emSize;
            __SizeRatio = sizeRatio;
            __FontStyle = fontStyle;
            __IsReadOnly = isReadOnly;
        }
        /// <summary>
        /// Pro tento stav je instance vytvořena
        /// </summary>
        public Components.InteractiveState InteractiveState { get { return __InteractiveState; } set { if (!__IsReadOnly) __InteractiveState = value; } } private Components.InteractiveState __InteractiveState;
        /// <summary>
        /// Explicitně daná velikost, není ale nijak optimální definovat ji takto explicitně. 
        /// Lepší je definovat <see cref="SizeRatio"/>.
        public float? EmSize { get { return __EmSize; } set { if (!__IsReadOnly) __EmSize = value; } } private float? __EmSize;
        /// <summary>
        /// Poměr velikosti aktuálního fontu ku fontu defaultnímu daného typu
        /// </summary>
        public float? SizeRatio { get { return __SizeRatio; } set { if (!__IsReadOnly) __SizeRatio = value; } } private float? __SizeRatio;
        /// <summary>
        /// Styl fontu; default = dle systémového fontu
        /// </summary>
        public FontStyle? FontStyle { get { return __FontStyle; } set { if (!__IsReadOnly) __FontStyle = value; } } private FontStyle? __FontStyle;
        /// <summary>
        /// Data jsou ReadOnly?
        /// </summary>
        public bool IsReadOnly { get { return __IsReadOnly; } set { if (!__IsReadOnly) __IsReadOnly = value; } } private bool __IsReadOnly;
        /// <summary>
        /// Obsahuje prázdnou ReadOnly instanci
        /// </summary>
        public static TextInteractiveStyle Empty
        {
            get
            {
                if (__Empty is null)
                    __Empty = new TextInteractiveStyle(true, Components.InteractiveState.Enabled, null, null, null);
                return __Empty;
            }
        }
        public static TextInteractiveStyle __Empty;
    }
    #endregion
}
