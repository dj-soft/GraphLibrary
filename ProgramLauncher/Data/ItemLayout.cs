using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DjSoft.Tools.ProgramLauncher.App;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    /// <summary>
    /// Sada layoutů: jedna sada obsahuje layouty <see cref="LayoutItemInfo"/> pro standardní prvky rozhraní zobrazené v jedné zvolené velikosti.
    /// Existují různé sady <see cref="LayoutSetInfo"/>, pro různé velikosti (které si volí uživatel).
    /// </summary>
    public class LayoutSetInfo : IMenuItem
    {
        #region Public instanční údaje = layouty pro jednotlivé prvky, pro stejnou velikost
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
        /// Layout prvků Page
        /// </summary>
        public LayoutItemInfo LayoutPage { get { return __LayoutPage; } set { if (!__IsReadOnly) __LayoutPage = value; } } private LayoutItemInfo __LayoutPage;
        /// <summary>
        /// Layout prvků Group
        /// </summary>
        public LayoutItemInfo LayoutGroup { get { return __LayoutGroup; } set { if (!__IsReadOnly) __LayoutGroup = value; } } private LayoutItemInfo __LayoutGroup;
        /// <summary>
        /// Layout prvků Application
        /// </summary>
        public LayoutItemInfo LayoutApplication { get { return __LayoutApplication; } set { if (!__IsReadOnly) __LayoutApplication = value; } } private LayoutItemInfo __LayoutApplication;
        /// <summary>
        /// Data jsou ReadOnly?
        /// </summary>
        public bool IsReadOnly { get { return __IsReadOnly; } set { if (!__IsReadOnly) __IsReadOnly = value; } } private bool __IsReadOnly;
        /// <summary>
        /// Metoda vrátí konkrétní layout daného druhu z této aktuální sady.
        /// </summary>
        /// <param name="layoutKind"></param>
        /// <returns></returns>
        public LayoutItemInfo GetLayout(DataLayoutKind layoutKind)
        {
            switch (layoutKind)
            {
                case DataLayoutKind.Pages: return LayoutPage;
                case DataLayoutKind.Groups: return LayoutGroup;
                case DataLayoutKind.Applications: return LayoutApplication;
            }
            return LayoutApplication;
        }

        #endregion
        #region Statické konstruktory konkrétních stylů, jejich kolekce a Default
        /// <summary>
        /// Kolekce všech standardních i přidaných definic
        /// </summary>
        public static LayoutSetInfo[] Collection { get { return _Collection.Values.ToArray(); } }
        /// <summary>
        /// Defaultní výchozí vzhled
        /// </summary>
        public static LayoutSetInfo Default { get { return GetItem(_DefaultName); } }
        private const string _DefaultName = "Default";
        /// <summary>
        /// Vrátí prvek daného jména
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static LayoutSetInfo GetItem(string name, bool useDefault = false)
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
        private static Dictionary<string, LayoutSetInfo> _Collection
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
        private static Dictionary<string, LayoutSetInfo> __Collection;
        /// <summary>
        /// Vytvoří a vrátí List všech standardních i přidaných definic
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, LayoutSetInfo> _CreateAllAppearances()
        {
            List<LayoutSetInfo> list = new List<LayoutSetInfo>();
            var methods = typeof(LayoutSetInfo).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                if (!method.IsSpecialName && method.ReturnType == typeof(LayoutSetInfo) && method.GetParameters().Length == 0)
                {
                    if (method.Invoke(null, new object[] { }) is LayoutSetInfo info)
                        list.Add(info);
                }
            }
            list.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            var collection = list.CreateDictionary(i => i.Name, true);
            return collection;
        }
        /// <summary>
        /// Vytvoří new instanci sady "Default"
        /// </summary>
        /// <returns></returns>
        private static LayoutSetInfo _CreateDefault()
        {
            var layoutSet = new LayoutSetInfo();
            layoutSet.__Name = _DefaultName;
            layoutSet.__ImageSmall = Properties.Resources.btn_g2_20;
            layoutSet.__SortOrder = 100;
            layoutSet.__LayoutPage = LayoutItemInfo.PageHeaderMedium;
            layoutSet.__LayoutGroup = LayoutItemInfo.GroupHeaderMedium;
            layoutSet.__LayoutApplication = LayoutItemInfo.ItemWideSmall;
            layoutSet.__IsReadOnly = true;
            return layoutSet;
        }
        /// <summary>
        /// Vytvoří new instanci palety "Small"
        /// </summary>
        /// <returns></returns>
        private static LayoutSetInfo _CreateSmall()
        {
            var layoutSet = new LayoutSetInfo();
            layoutSet.__Name = "Small";
            layoutSet.__ImageSmall = Properties.Resources.btn_09_20;
            layoutSet.__SortOrder = 500;
            layoutSet.__LayoutPage = LayoutItemInfo.PageHeaderSmall;
            layoutSet.__LayoutGroup = LayoutItemInfo.GroupHeaderMedium;
            layoutSet.__LayoutApplication = LayoutItemInfo.ItemWideSmall;
            layoutSet.__IsReadOnly = true;
            return layoutSet;
        }
        /// <summary>
        /// Vytvoří new instanci palety "Medium"
        /// </summary>
        /// <returns></returns>
        private static LayoutSetInfo _CreateMedium()
        {
            var layoutSet = new LayoutSetInfo();
            layoutSet.__Name = "Medium";
            layoutSet.__ImageSmall = Properties.Resources.btn_05_20;
            layoutSet.__SortOrder = 600;
            layoutSet.__LayoutPage = LayoutItemInfo.PageHeaderMedium;
            layoutSet.__LayoutGroup = LayoutItemInfo.GroupHeaderMedium;
            layoutSet.__LayoutApplication = LayoutItemInfo.ItemWideMedium;
            layoutSet.__IsReadOnly = true;
            return layoutSet;
        }
        /// <summary>
        /// Vytvoří new instanci palety "Big"
        /// </summary>
        /// <returns></returns>
        private static LayoutSetInfo _CreateBig()
        {
            var layoutSet = new LayoutSetInfo();
            layoutSet.__Name = "Big";
            layoutSet.__ImageSmall = Properties.Resources.btn_22_20;
            layoutSet.__SortOrder = 700;
            layoutSet.__LayoutPage = LayoutItemInfo.PageHeaderLarge;
            layoutSet.__LayoutGroup = LayoutItemInfo.GroupHeaderMedium;
            layoutSet.__LayoutApplication = LayoutItemInfo.ItemWideLarge;
            layoutSet.__IsReadOnly = true;
            return layoutSet;
        }
        #endregion
        #region IMenuItem
        string IMenuItem.Text { get { return Name; } }
        Image IMenuItem.Image { get { return ImageSmall; } }
        object IMenuItem.Code { get { return Name; } }
        string IMenuItem.ToolTip { get { return null; } }
        MenuItemType IMenuItem.ItemType { get { return MenuItemType.Button; } }
        bool IMenuItem.Enabled { get { return true; } }
        FontStyle? IMenuItem.FontStyle { get { return (Object.ReferenceEquals(this, App.CurrentLayoutSet) ? (FontStyle?)FontStyle.Bold : (FontStyle?)null); } }
        object IMenuItem.ToolItem { get; set; }
        object IMenuItem.UserData { get; set; }
        void IMenuItem.Process()
        {
            App.CurrentLayoutSet = this;
            App.Settings.LayoutSetName = this.Name;
        }
        #endregion
    }
    /// <summary>
    /// Layout prvku: rozmístění, velikost, styl písma
    /// </summary>
    public class LayoutItemInfo
    {
        #region Public properties
        /// <summary>
        /// Jméno layoutu
        /// </summary>
        public string Name { get { return __Name; } set { if (!__IsReadOnly) __Name = value; } } private string __Name;
        /// <summary>
        /// Velikost celé buňky.
        /// Tato velikost je základem pro tvorbu celého layoutu stránky = poskládání jednotlivých prvků do matice v controlu. Používá se společně s adresou buňky <see cref="InteractiveItem.Adress"/>.
        /// <para/>
        /// Může mít zápornou šířku, pak obsazuje disponibilní šířku v controlu ("Spring").
        /// V případě, že určitý řádek (prvky na stejné adrese X) obsahuje prvky, jejichž <see cref="CellSize"/>.Width je záporné, pak tyto prvky obsadí celou disponibilní šířku, 
        /// která je určena těmi okolními řádky, které neobsahují "Spring" prvky.
        /// (Pokud neexistují okolní řádky s pevnou šířkou, použije se defaultní velikost buňky).
        /// <para/>
        /// Nelze odvozovat šířku celého řádku od vizuálního controlu, vždy jen od fixních prvků.
        /// </summary>
        public Size CellSize { get { return __CellSize; } set { if (!__IsReadOnly) __CellSize = value; } } private Size __CellSize;
        /// <summary>
        /// Pokud má hodnotu, pak reprezentuje 3D efekt pro vykreslení celého pozadí prvku = i v NonMouseActive stavu.
        /// Používá se typicky pro GroupHeader.
        /// </summary>
        public float? BackAreaStatic3DRatio { get { return __BackAreaStatic3DRatio; } set { if (!__IsReadOnly) __BackAreaStatic3DRatio = value; } } private float? __BackAreaStatic3DRatio;
        /// <summary>
        /// Pokud má hodnotu, pak reprezentuje 3D efekt pro vykreslení celého pozadí prvku, pokud ten je Selected.
        /// Používá se typicky pro PageHeader.
        /// </summary>
        public float? BackAreaSelected3DRatio { get { return __BackAreaSelected3DRatio; } set { if (!__IsReadOnly) __BackAreaSelected3DRatio = value; } } private float? __BackAreaSelected3DRatio;
        /// <summary>
        /// Souřadnice aktivního prostoru pro data: v tomto prostoru je obsah myšo-aktivní.
        /// Vnější prostor okolo těchto souřadnic je prázdný a neaktivní, odděluje od sebe sousední buňky.
        /// <para/>
        /// V tomto prostoru se stínuje pozice myši barvou <see cref="ButtonBackColors"/> : <see cref="ColorSet.MouseHighlightColor"/>.
        /// </summary>
        public RectangleExt ActiveContentBounds { get { return __ContentBounds; } set { if (!__IsReadOnly) __ContentBounds = value; } } private RectangleExt __ContentBounds;
        /// <summary>
        /// Souřadnice prostoru s okrajem a vykresleným pozadím.
        /// V tomto prostoru je použita barva <see cref="BorderLineColors"/> a <see cref="ButtonBackColors"/>, 
        /// border má šířku <see cref="BorderWidth"/> a kulaté rohy <see cref="BorderRound"/>.
        /// <para/>
        /// Texty mohou být i mimo tento prostor.
        /// </summary>
        public RectangleExt BorderBounds { get { return __BorderBounds; } set { if (!__IsReadOnly) __BorderBounds = value; } } private RectangleExt __BorderBounds;
        /// <summary>
        /// Souřadnice prostoru, který tvoří typickou vodorovnou linku v prostoru GroupHeader.
        /// Pokud je Empty, pak se linka nekreslí.
        /// V tomto prostoru je použita barva <see cref="HeaderLineColors"/>. 
        /// </summary>
        public RectangleExt HeaderLineBounds { get { return __HeaderLineBounds; } set { if (!__IsReadOnly) __HeaderLineBounds = value; } } private RectangleExt __HeaderLineBounds;
        /// <summary>
        /// Zaoblení Borderu, 0 = ostře hranatý
        /// </summary>
        public int BorderRound { get { return __BorderRound; } set { if (!__IsReadOnly) __BorderRound = value; } } private int __BorderRound;
        /// <summary>
        /// Šířka linky Borderu, 0 = nekreslí se
        /// </summary>
        public float BorderWidth { get { return __BorderWidth; } set { if (!__IsReadOnly) __BorderWidth = value; } } private float __BorderWidth;
        /// <summary>
        /// Souřadnice prostoru pro ikonu
        /// </summary>
        public RectangleExt ImageBounds { get { return __ImageBounds; } set { if (!__IsReadOnly) __ImageBounds = value; } } private RectangleExt __ImageBounds;
        /// <summary>
        /// Velikost prostoru stínování myši, lze zakázat zadáním prázdného prostoru
        /// </summary>
        public Size MouseHighlightSize { get { return __MouseHighlightSize; } set { if (!__IsReadOnly) __MouseHighlightSize = value; } } private Size __MouseHighlightSize;
        /// <summary>
        /// Data jsou ReadOnly?
        /// </summary>
        public bool IsReadOnly { get { return __IsReadOnly; } set { if (!__IsReadOnly) __IsReadOnly = value; } } private bool __IsReadOnly;
        /// <summary>
        /// Souřadnice prostoru pro hlavní text
        /// </summary>
        public RectangleExt MainTitleBounds { get { return __MainTitleBounds; } set { if (!__IsReadOnly) __MainTitleBounds = value; } } private RectangleExt __MainTitleBounds;
        /// <summary>
        /// Umístění a zarovnání hlavního textu
        /// </summary>
        public ContentAlignment MainTitleAlignment { get { return __MainTitleAlignment; } set { if (!__IsReadOnly) __MainTitleAlignment = value; } } private ContentAlignment __MainTitleAlignment;
        /// <summary>
        /// Typ vzhledu hlavního titulku
        /// </summary>
        public AppearanceTextPartType? MainTitleAppearanceType { get { return __MainTitleAppearanceType; } set { if (!__IsReadOnly) __MainTitleAppearanceType = value; } } private AppearanceTextPartType? __MainTitleAppearanceType;
        /// <summary>
        /// Vzhled hlavního textu
        /// </summary>
        public TextAppearance MainTitleAppearance
        {
            get { return __MainTitleAppearance ?? App.CurrentAppearance.GetTextAppearance(MainTitleAppearanceType ?? AppearanceTextPartType.MainTitle); }
            set { if (!__IsReadOnly) __MainTitleAppearance = value; }
        }
        private TextAppearance __MainTitleAppearance;
        #endregion
        #region Statické konstruktory konkrétních stylů
        /// <summary>
        /// Záhlaví stránky - menší
        /// </summary>
        public static LayoutItemInfo PageHeaderSmall
        {
            get
            {
                LayoutItemInfo dataLayout = new LayoutItemInfo()
                {
                    Name = "Stránka malá",
                    CellSize = new Size(160, 40),
                    BackAreaSelected3DRatio = -0.25f,
                    ActiveContentBounds = new RectangleExt(2, null, 2, 2, null, 2),
                    BorderBounds = new RectangleExt(4, 32, null, 4, 32, null),
                    MouseHighlightSize = new Size(30, 20),
                    BorderRound = 3,
                    BorderWidth = 1f,
                    ImageBounds = new RectangleExt(8, 24, null, 8, 24, null),
                    MainTitleBounds = new RectangleExt(40, null, 6, 10, 20, null),
                    MainTitleAlignment = ContentAlignment.MiddleLeft,
                    MainTitleAppearanceType = AppearanceTextPartType.MainTitle,
                    IsReadOnly = true
                };
                return dataLayout;
            }
        }
        /// <summary>
        /// Záhlaví stránky - střední
        /// </summary>
        public static LayoutItemInfo PageHeaderMedium
        {
            get
            {
                LayoutItemInfo dataLayout = new LayoutItemInfo()
                {
                    Name = "Stránka malá",
                    CellSize = new Size(180, 48),
                    BackAreaSelected3DRatio = -0.25f,
                    ActiveContentBounds = new RectangleExt(2, null, 2, 2, null, 2),
                    BorderBounds = new RectangleExt(8, 32, null, 8, 32, null),
                    MouseHighlightSize = new Size(40, 24),
                    BorderRound = 4,
                    BorderWidth = 1f,
                    ImageBounds = new RectangleExt(12, 24, null, 12, 24, null),
                    MainTitleBounds = new RectangleExt(46, null, 8, 10, 20, null),
                    MainTitleAlignment = ContentAlignment.MiddleLeft,
                    MainTitleAppearanceType = AppearanceTextPartType.MainTitle,
                    IsReadOnly = true
                };
                return dataLayout;
            }
        }
        /// <summary>
        /// Záhlaví stránky - velký
        /// </summary>
        public static LayoutItemInfo PageHeaderLarge
        {
            get
            {
                LayoutItemInfo dataLayout = new LayoutItemInfo()
                {
                    Name = "Stránka malá",
                    CellSize = new Size(200, 60),
                    ActiveContentBounds = new RectangleExt(3, null, 3, 3, null, 3),
                    BackAreaSelected3DRatio = -0.25f,
                    BorderBounds = new RectangleExt(10, 40, null, 10, 40, null),
                    MouseHighlightSize = new Size(44, 26),
                    BorderRound = 5,
                    BorderWidth = 1f,
                    ImageBounds = new RectangleExt(12, 36, null, 12, 36, null),
                    MainTitleBounds = new RectangleExt(56, null, 8, 10, 20, null),
                    MainTitleAlignment = ContentAlignment.MiddleLeft,
                    MainTitleAppearanceType = AppearanceTextPartType.MainTitle,
                    IsReadOnly = true
                };
                return dataLayout;
            }
        }

        /// <summary>
        /// Středně velký titulek pro grupu
        /// </summary>
        public static LayoutItemInfo GroupHeaderMedium
        {
            get
            {
                LayoutItemInfo dataLayout = new LayoutItemInfo()
                {
                    Name = "Střední titulek",
                    CellSize = new Size(-1, 24),
                    BackAreaStatic3DRatio = 0.15f,
                    ActiveContentBounds = new RectangleExt(0, null, 0, 0, 24, null),
                    MainTitleBounds = new RectangleExt(12, null, 12, 2, null, 2),
                    MainTitleAlignment = ContentAlignment.MiddleLeft,
                    MainTitleAppearanceType = AppearanceTextPartType.MainTitle,
                    IsReadOnly = true
                };
                return dataLayout;
            }
        }

        /// <summary>
        /// Střední obdélník
        /// </summary>
        public static LayoutItemInfo ItemWideSmall
        {
            get
            {
                LayoutItemInfo dataLayout = new LayoutItemInfo()
                {
                    Name = "Menší cihla",
                    CellSize = new Size(160, 64),
                    ActiveContentBounds = new RectangleExt(2, null, 2, 2, null, 2),
                    BorderBounds = new RectangleExt(4, 56, null, 4, 56, null),
                    MouseHighlightSize = new Size(40, 24),
                    BorderRound = 4,
                    BorderWidth = 1f,
                    ImageBounds = new RectangleExt(8, 48, null, 8, 48, null),
                    MainTitleBounds = new RectangleExt(62, null, 6, 8, 48, null),
                    MainTitleAlignment = ContentAlignment.MiddleLeft,
                    MainTitleAppearanceType = AppearanceTextPartType.SubTitle,
                    IsReadOnly = true
                };
                return dataLayout;
            }
        }
        /// <summary>
        /// Středně velký obdélník
        /// </summary>
        public static LayoutItemInfo ItemWideMedium
        {
            get
            {
                LayoutItemInfo dataLayout = new LayoutItemInfo()
                {
                    Name = "Střední cihla",
                    CellSize = new Size(180, 92),
                    ActiveContentBounds = new RectangleExt(4, null, 4, 4, null, 4),
                    BorderBounds = new RectangleExt(14, 64, null, 14, 64, null),
                    MouseHighlightSize = new Size(48, 32),
                    BorderRound = 6,
                    BorderWidth = 1f,
                    ImageBounds = new RectangleExt(22, 48, null, 22, 48, null),
                    MainTitleBounds = new RectangleExt(82, null, 8, 22, 48, null),
                    MainTitleAlignment = ContentAlignment.MiddleLeft,
                    MainTitleAppearanceType = AppearanceTextPartType.MainTitle,
                    IsReadOnly = true
                };
                return dataLayout;
            }
        }
        /// <summary>
        /// Hodně velký obdélník
        /// </summary>
        public static LayoutItemInfo ItemWideLarge
        {
            get
            {
                LayoutItemInfo dataLayout = new LayoutItemInfo()
                {
                    Name = "Střední cihla",
                    CellSize = new Size(180, 92),
                    ActiveContentBounds = new RectangleExt(4, null, 4, 4, null, 4),
                    BorderBounds = new RectangleExt(14, 64, null, 14, 64, null),
                    MouseHighlightSize = new Size(48, 32),
                    BorderRound = 6,
                    BorderWidth = 1f,
                    ImageBounds = new RectangleExt(22, 48, null, 22, 48, null),
                    MainTitleBounds = new RectangleExt(82, null, 8, 22, 48, null),
                    MainTitleAlignment = ContentAlignment.MiddleLeft,
                    MainTitleAppearanceType = AppearanceTextPartType.MainTitle,
                    IsReadOnly = true
                };
                return dataLayout;
            }
        }
        #endregion
    }
    #region Enumy
    public enum DataLayoutKind
    {
        None,
        Pages,
        Groups,
        Applications
    }
    #endregion

}
