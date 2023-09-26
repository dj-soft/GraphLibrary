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
    /// Sada layoutů: jedna sada obsahuje layouty <see cref="ItemLayoutInfo"/> pro standardní prvky rozhraní zobrazené v jedné zvolené velikosti.
    /// Existují různé sady <see cref="ItemLayoutSet"/>, pro různé velikosti (které si volí uživatel).
    /// </summary>
    public class ItemLayoutSet : IMenuItem
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
        public ItemLayoutInfo LayoutPage { get { return __LayoutPage; } set { if (!__IsReadOnly) __LayoutPage = value; } } private ItemLayoutInfo __LayoutPage;
        /// <summary>
        /// Layout prvků Group
        /// </summary>
        public ItemLayoutInfo LayoutGroup { get { return __LayoutGroup; } set { if (!__IsReadOnly) __LayoutGroup = value; } } private ItemLayoutInfo __LayoutGroup;
        /// <summary>
        /// Layout prvků Application
        /// </summary>
        public ItemLayoutInfo LayoutApplication { get { return __LayoutApplication; } set { if (!__IsReadOnly) __LayoutApplication = value; } } private ItemLayoutInfo __LayoutApplication;
        /// <summary>
        /// Data jsou ReadOnly?
        /// </summary>
        public bool IsReadOnly { get { return __IsReadOnly; } } private bool __IsReadOnly;
        /// <summary>
        /// Metoda vrátí konkrétní layout daného druhu, z této sady.
        /// </summary>
        /// <param name="layoutKind"></param>
        /// <returns></returns>
        public ItemLayoutInfo GetLayout(DataLayoutKind layoutKind)
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
        #region Statické konstruktory konkrétních stylů
        /// <summary>
        /// Kolekce všech standardních i přidaných definic
        /// </summary>
        public static ItemLayoutSet[] Collection { get { return _Collection.Values.ToArray(); } }
        public static ItemLayoutSet Default { get { return GetItem(_DefaultName); } }
        private const string _DefaultName = "Default";
        /// <summary>
        /// Vrátí prvek daného jména
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ItemLayoutSet GetItem(string name, bool useDefault = false)
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
        private static Dictionary<string, ItemLayoutSet> _Collection
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
        private static Dictionary<string, ItemLayoutSet> __Collection;
        /// <summary>
        /// Vytvoří a vrátí List všech standardních i přidaných definic
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, ItemLayoutSet> _CreateAllAppearances()
        {
            List<ItemLayoutSet> list = new List<ItemLayoutSet>();
            var methods = typeof(ItemLayoutSet).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                if (!method.IsSpecialName && method.ReturnType == typeof(ItemLayoutSet) && method.GetParameters().Length == 0)
                {
                    if (method.Invoke(null, new object[] { }) is ItemLayoutSet info)
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
        private static ItemLayoutSet _CreateDefault()
        {
            var layoutSet = new ItemLayoutSet();
            layoutSet.__Name = _DefaultName;
            layoutSet.__ImageSmall = Properties.Resources.btn_g2_20;
            layoutSet.__SortOrder = 100;
            layoutSet.__LayoutPage = ItemLayoutInfo.SetSmallBrick;
            layoutSet.__LayoutGroup = ItemLayoutInfo.SetMidiBrick;
            layoutSet.__LayoutApplication = ItemLayoutInfo.SetMediumBrick;
            layoutSet.__IsReadOnly = true;

            return layoutSet;
        }
        /// <summary>
        /// Vytvoří new instanci palety "Small"
        /// </summary>
        /// <returns></returns>
        private static ItemLayoutSet _CreateSmall()
        {
            var layoutSet = new ItemLayoutSet();
            layoutSet.__Name = "Small";
            layoutSet.__ImageSmall = Properties.Resources.btn_09_20;
            layoutSet.__SortOrder = 500;
            layoutSet.__LayoutPage = ItemLayoutInfo.SetSmallBrick;
            layoutSet.__LayoutGroup = ItemLayoutInfo.SetMidiBrick;
            layoutSet.__LayoutApplication = ItemLayoutInfo.SetMediumBrick;
            layoutSet.__IsReadOnly = true;
            return layoutSet;
        }
        /// <summary>
        /// Vytvoří new instanci palety "Medium"
        /// </summary>
        /// <returns></returns>
        private static ItemLayoutSet _CreateMedium()
        {
            var layoutSet = new ItemLayoutSet();
            layoutSet.__Name = "Medium";
            layoutSet.__ImageSmall = Properties.Resources.btn_05_20;
            layoutSet.__SortOrder = 600;
            layoutSet.__LayoutPage = ItemLayoutInfo.SetSmallBrick;
            layoutSet.__LayoutGroup = ItemLayoutInfo.SetMidiBrick;
            layoutSet.__LayoutApplication = ItemLayoutInfo.SetMediumBrick;
            layoutSet.__IsReadOnly = true;
            return layoutSet;
        }
        /// <summary>
        /// Vytvoří new instanci palety "Big"
        /// </summary>
        /// <returns></returns>
        private static ItemLayoutSet _CreateBig()
        {
            var layoutSet = new ItemLayoutSet();
            layoutSet.__Name = "Big";
            layoutSet.__ImageSmall = Properties.Resources.btn_22_20;
            layoutSet.__SortOrder = 700;
            layoutSet.__LayoutPage = ItemLayoutInfo.SetSmallBrick;
            layoutSet.__LayoutGroup = ItemLayoutInfo.SetMidiBrick;
            layoutSet.__LayoutApplication = ItemLayoutInfo.SetMediumBrick;
            layoutSet.__IsReadOnly = true;
            return layoutSet;
        }
        #endregion
        #region IMenuItem
        string IMenuItem.Text { get { return Name; } }
        string IMenuItem.ToolTip { get { return null; } }
        MenuItemType IMenuItem.ItemType { get { return MenuItemType.Button; } }
        Image IMenuItem.Image { get { return ImageSmall; } }
        bool IMenuItem.Enabled { get { return true; } }
        FontStyle? IMenuItem.FontStyle { get { return (Object.ReferenceEquals(this, App.CurrentLayoutSet) ? (FontStyle?)FontStyle.Bold : (FontStyle?)null); } }
        object IMenuItem.UserData { get; set; }
        #endregion
    }
    /// <summary>
    /// Layout prvku: rozmístění, velikost, styl písma
    /// </summary>
    public class ItemLayoutInfo
    {
        #region Public properties
        /// <summary>
        /// Jméno stylu
        /// </summary>
        public string Name { get { return __Name; } set { if (!__IsReadOnly) __Name = value; } } private string __Name;
        /// <summary>
        /// Velikost celé buňky.
        /// Základ pro tvorbu layoutu = poskládání jednotlivých prvků do matice v controlu. Používá se společně s adresou buňky <see cref="InteractiveItem.Adress"/>.
        /// <para/>
        /// Může mít zápornou šířku, pak obsazuje disponibilní šířku v controlu ("Spring").
        /// V případě, že určitý řádek (prvky na stejné adrese X) obsahuje prvky, jejichž <see cref="CellSize"/>.Width je záporné, pak tyto prvky obsadí celou šířku, 
        /// která je určena těmi řádky, které neobshaují "Spring" prvky.
        /// <para/>
        /// Nelze odvozovat šířku celého řádku od vizuálního controlu, vždy jen od fixních prvků.
        /// </summary>
        public Size CellSize { get { return __CellSize; } set { if (!__IsReadOnly) __CellSize = value; } }
        private Size __CellSize;
        /// <summary>
        /// Souřadnice aktivního prostoru pro data: v tomto prostoru je obsah myšo-aktivní.
        /// Vnější prostor okolo těchto souřadnic je prázdný a neaktivní, odděluje od sebe sousední buňky.
        /// <para/>
        /// V tomto prostoru se stínuje pozice myši barvou <see cref="ButtonBackColors"/> : <see cref="ColorSet.MouseHighlightColor"/>.
        /// </summary>
        public Rectangle ContentBounds { get { return __ContentBounds; } set { if (!__IsReadOnly) __ContentBounds = value; } }
        private Rectangle __ContentBounds;
        /// <summary>
        /// Souřadnice prostoru s okrajem a vykresleným pozadím.
        /// V tomto prostoru je použita barva <see cref="BorderLineColors"/> a <see cref="ButtonBackColors"/>, 
        /// border má šířku <see cref="BorderWidth"/> a kulaté rohy <see cref="BorderRound"/>.
        /// <para/>
        /// Texty mohou být i mimo tento prostor.
        /// </summary>
        public Rectangle BorderBounds { get { return __BorderBounds; } set { if (!__IsReadOnly) __BorderBounds = value; } }
        private Rectangle __BorderBounds;
        /// <summary>
        /// Zaoblení Borderu, 0 = ostře hranatý
        /// </summary>
        public int BorderRound { get { return __BorderRound; } set { if (!__IsReadOnly) __BorderRound = value; } }
        private int __BorderRound;
        /// <summary>
        /// Šířka linky Borderu, 0 = nekreslí se
        /// </summary>
        public float BorderWidth { get { return __BorderWidth; } set { if (!__IsReadOnly) __BorderWidth = value; } }
        private float __BorderWidth;
        /// <summary>
        /// Souřadnice prostoru pro ikonu
        /// </summary>
        public Rectangle ImageBounds { get { return __ImageBounds; } set { if (!__IsReadOnly) __ImageBounds = value; } }
        private Rectangle __ImageBounds;
        /// <summary>
        /// Velikost prostoru stínování myši, lze zakázat zadáním prázdného prostoru
        /// </summary>
        public Size MouseHighlightSize { get { return __MouseHighlightSize; } set { if (!__IsReadOnly) __MouseHighlightSize = value; } }
        private Size __MouseHighlightSize;
        /// <summary>
        /// Data jsou ReadOnly?
        /// </summary>
        public bool IsReadOnly { get { return __IsReadOnly; } }
        private bool __IsReadOnly;
        /// <summary>
        /// Souřadnice prostoru pro hlavní text
        /// </summary>
        public Rectangle MainTitleBounds { get { return __MainTitleBounds; } set { if (!__IsReadOnly) __MainTitleBounds = value; } }
        private Rectangle __MainTitleBounds;
        /// <summary>
        /// Typ vzhledu hlavního titulku
        /// </summary>
        public AppearanceTextPartType? MainTitleAppearanceType { get { return __MainTitleAppearanceType; } set { if (!__IsReadOnly) __MainTitleAppearanceType = value; } }
        private AppearanceTextPartType? __MainTitleAppearanceType;
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
        /// Menší obdélník
        /// </summary>
        public static ItemLayoutInfo SetSmallBrick
        {
            get
            {
                ItemLayoutInfo dataLayout = new ItemLayoutInfo()
                {
                    Name = "Menší cihla",
                    CellSize = new Size(160, 48),
                    ContentBounds = new Rectangle(2, 2, 156, 44),
                    BorderBounds = new Rectangle(6, 6, 36, 36),
                    MouseHighlightSize = new Size(40, 24),
                    BorderRound = 4,
                    BorderWidth = 1f,
                    ImageBounds = new Rectangle(12, 12, 24, 24),
                    MainTitleBounds = new Rectangle(46, 14, 95, 20),
                    MainTitleAppearanceType = AppearanceTextPartType.MainTitle
                };
                return dataLayout;
            }
        }
        /// <summary>
        /// Střední obdélník
        /// </summary>
        public static ItemLayoutInfo SetMidiBrick
        {
            get
            {
                ItemLayoutInfo dataLayout = new ItemLayoutInfo()
                {
                    Name = "Menší cihla",
                    CellSize = new Size(160, 64),
                    ContentBounds = new Rectangle(2, 2, 156, 60),
                    BorderBounds = new Rectangle(4, 4, 56, 56),
                    MouseHighlightSize = new Size(40, 24),
                    BorderRound = 4,
                    BorderWidth = 1f,
                    ImageBounds = new Rectangle(8, 8, 48, 48),
                    MainTitleBounds = new Rectangle(62, 24, 95, 20),
                    MainTitleAppearanceType = AppearanceTextPartType.SubTitle
                };
                return dataLayout;
            }
        }
        /// <summary>
        /// Středně velký obdélník
        /// </summary>
        public static ItemLayoutInfo SetMediumBrick
        {
            get
            {
                ItemLayoutInfo dataLayout = new ItemLayoutInfo()
                {
                    Name = "Střední cihla",
                    CellSize = new Size(180, 92),
                    ContentBounds = new Rectangle(4, 4, 173, 85),
                    BorderBounds = new Rectangle(14, 14, 64, 64),
                    MouseHighlightSize = new Size(48, 32),
                    BorderRound = 6,
                    BorderWidth = 1f,
                    ImageBounds = new Rectangle(22, 22, 48, 48),
                    MainTitleBounds = new Rectangle(82, 18, 95, 20),
                    MainTitleAppearanceType = AppearanceTextPartType.MainTitle
                };
                return dataLayout;
            }
        }
        /// <summary>
        /// Středně velký titulek
        /// </summary>
        public static ItemLayoutInfo SetTitle
        {
            get
            {
                ItemLayoutInfo dataLayout = new ItemLayoutInfo()
                {
                    Name = "Střední titulek",
                    CellSize = new Size(-1, 24),
                    ContentBounds = new Rectangle(0, 0, 200, 24),
                    MainTitleBounds = new Rectangle(0, 0, 200, 24),
                    MainTitleAppearanceType = AppearanceTextPartType.MainTitle
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
