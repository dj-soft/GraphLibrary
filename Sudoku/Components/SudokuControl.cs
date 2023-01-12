using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace DjSoft.Games.Sudoku.Components
{
    /// <summary>
    /// Control pro zobrazení Sudoku
    /// </summary>
    public class SudokuControl : AnimatedControl
    {
        #region Konstruktor a řízení inicializace
        public SudokuControl()
        {
            this.UseBackgroundLayer = true;
            this.UseStandardLayer = true;

            _InitGame();
            _InitTheme();
            _InitCoordinates();
            _InitInteractivity();

            __ComponentsReady = true;
            _LinkComponents();
        }
        private void _LinkComponents()
        {
            if (__ComponentsReady)
            {
                using (__Coordinates.SuspendCoordinates())
                {
                    __Coordinates.Theme = Theme;
                    __Coordinates.Configuration = Configuration;
                    __Coordinates.SudokuGame = SudokuGame;
                }
                _RefreshCoordinates();
            }
        }
        private bool __ComponentsReady = false;
        #endregion
        #region Hra = datová instance a interakce s ní, a konfigurace
        private void _InitGame()
        {
            __SudokuGame = new Data.SudokuGame();
            __Configuration = SudokuConfiguration.Default;
        }
        public Data.SudokuGame SudokuGame { get { return __SudokuGame; } set { __SudokuGame = value; _LinkComponents(); } } private Data.SudokuGame __SudokuGame;
        public SudokuConfiguration Configuration { get { return __Configuration; } set { __Configuration = value; _LinkComponents(); } } private SudokuConfiguration __Configuration;
        #endregion
        #region Vizuální kabát = Theme
        private void _InitTheme()
        {
            Theme = SudokuSkinTheme.LightGray;
        }
        public SudokuSkinTheme Theme { get { return __Theme; } set { __Theme = value; _LinkComponents(); } } private SudokuSkinTheme __Theme;
        #endregion
        #region Mapa prostoru = kde co je, v závislosti na velikosti controlu
        private void _InitCoordinates()
        {
            __Coordinates = new SudokuCoordinates(this);
            ClientSizeChanged += _ClientSizeChanged;
        }
        /// <summary>
        /// Po změně rozměru controlu se upraví souřadnice v <see cref="SudokuCoordinates"/>;
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ClientSizeChanged(object sender, EventArgs e)
        {
            _RefreshCoordinates();
        }
        private void _RefreshCoordinates()
        {
            if (__Coordinates != null)
            {
                __Coordinates.ResizeTo(this.ClientSize);
                LayerBackgroundValid = false;
                LayerStandardValid = false;
            }
        }
        private SudokuCoordinates __Coordinates;
        #endregion
        #region Fyzická interaktivita od uživatele do controlu a do hry
        private void _InitInteractivity()
        {
            this.MouseMove += _MouseMove;
            this.MouseDown += _MouseDown;
            this.MouseUp += _MouseUp;
        }
        private void _MouseMove(object sender, MouseEventArgs e)
        {
        }
        private void _MouseDown(object sender, MouseEventArgs e)
        {
        }
        private void _MouseUp(object sender, MouseEventArgs e)
        {
        }
        #endregion
        #region Kreslení = Paint do vrstev
        protected override void DoPaintBackground(LayeredPaintEventArgs args)
        {
            base.DoPaintBackground(args, Theme.BackColor);
        }
        protected override void DoPaintStandard(LayeredPaintEventArgs args)
        {
            var theme = Theme;
            var coords = __Coordinates;
            args.Graphics.FillRectangle(_Brush(theme.GameBackColor), coords.GameBounds);
            args.Graphics.FillRectangle(_Brush(theme.ControlBackColor), coords.ControlBounds);
        }
        private SolidBrush _Brush(Color color)
        {
            if (__SolidBrush is null) __SolidBrush = new SolidBrush(color);
            else __SolidBrush.Color = color;
            return __SolidBrush;
        }
        private SolidBrush __SolidBrush;
        #endregion
    }
    #region class SudokuCoordinates : Souřadnice prvků v Sudoku
    /// <summary>
    /// Souřadnice prvků v Sudoku
    /// </summary>
    public class SudokuCoordinates
    {
        /// <summary>
        /// Konstruktor a public data
        /// </summary>
        /// <param name="sudokuGame"></param>
        public SudokuCoordinates(SudokuControl owner)
        {
            __Owner = owner;
            _CreateSudokuItems();
        }
        /// <summary>
        /// Owner control
        /// </summary>
        private SudokuControl __Owner;
        /// <summary>
        /// Aktuální hra
        /// </summary>
        public Data.SudokuGame SudokuGame { get { return __SudokuGame; } set { _SetSudokuGame(value); } } private Data.SudokuGame __SudokuGame;
        /// <summary>
        /// Vloží dodanou hru.
        /// </summary>
        /// <param name="sudokuGame"></param>
        private void _SetSudokuGame(Data.SudokuGame sudokuGame)
        {
            __SudokuGame = sudokuGame;
            __SudokuGameIsChanged = true;
            if (!__IsSuspended)
                _LinkSudokuGame();
        }
        /// <summary>
        /// Konfigurace. Ovlivňuje vzhled i chování.
        /// </summary>
        public SudokuConfiguration Configuration { get { return __Configuration; } set { __Configuration = value; _RecalcBounds(); } } private SudokuConfiguration __Configuration;
        /// <summary>
        /// Vizuální skin
        /// </summary>
        public SudokuSkinTheme Theme { get { return __Theme; } set { __Theme = value; _RecalcBounds(); } } private SudokuSkinTheme __Theme;
        /// <summary>
        /// Velikost controlu, pro kterou byly naposledy počítány souřadnice
        /// </summary>
        public SizeF ControlSize { get { return __ControlSize; } set { __ControlSize = value; _RecalcBounds(); } } private SizeF __ControlSize;
        /// <summary>
        /// Po změně rozměru controlu se upraví souřadnice v <see cref="SudokuCoordinates"/>;
        /// </summary>
        /// <param name="size"></param>
        public void ResizeTo(Size size)
        {
            __ControlSize = size;
            _SetAllBounds();
        }
        /// <summary>
        /// Pokusí se vyvolat přepočet souřadnic, pokud aktuálně není Suspend
        /// </summary>
        private void _RecalcBounds()
        {
            __SudokuBoundsChanged = true;
            if (!__IsSuspended)
                _SetAllBounds();
        }
        #region Prvky SudokuItem - jejich prvotní vytvoření
        /// <summary>
        /// Vizuální a interaktivní prvky
        /// </summary>
        public IReadOnlyList<SudokuItem> Items { get { return __Items; } }
        /// <summary>
        /// Vytvoří pole prvků a naplní jej, jedenkrát v konstrktoru
        /// </summary>
        private void _CreateSudokuItems()
        {
            __Items = new List<SudokuItem>();

            // Vygeneruje definice pro všechny linky, ve správném pořadí odspodu:
            addLines(1, SudokuItemType.PartCellLine);
            addLines(2, SudokuItemType.PartCellLine);
            addLines(4, SudokuItemType.PartCellLine);
            addLines(5, SudokuItemType.PartCellLine);
            addLines(7, SudokuItemType.PartCellLine);
            addLines(8, SudokuItemType.PartCellLine);
            addLines(3, SudokuItemType.PartGroupLine);
            addLines(6, SudokuItemType.PartGroupLine);
            addLines(0, SudokuItemType.PartOuterLine);
            addLines(9, SudokuItemType.PartOuterLine);

            // Vygeneruje definice pro všechny grupy:
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                    addItem(row, col, SudokuItemType.SudokuGroup);

            // Vygeneruje definice pro všechny buňky včetně SubValue:
            for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                {
                    addItem(row, col, SudokuItemType.SudokuCell);
                    for (int sub = 1; sub <= 9; sub++)
                        addItem(row, col, SudokuItemType.SudokuSubCell, sub);
                }

            void addLines(int pos, SudokuItemType lineType)
            {
                addItem(pos, 0, SudokuItemType.PartSudoku | lineType | SudokuItemType.PartHorizontalLine);
                addItem(0, pos, SudokuItemType.PartSudoku | lineType | SudokuItemType.PartVerticalLine);
            }

            void addItem(int row, int col, SudokuItemType itemType, int? itemSubValue = null)
            {
                SudokuItem item = new SudokuItem(this.__Owner, new Data.Position((UInt16)row, (UInt16)col), itemType, itemSubValue);
                __Items.Add(item);
            }
        }
        /// <summary>Vizuální a interaktivní prvky</summary>
        private List<SudokuItem> __Items;
        #endregion
        #region Napojení na hru
        private void _LinkSudokuGame()
        {
            __SudokuGameIsChanged = false;
        }
        /// <summary>Obsahuje true po změně objektu v <see cref="__SudokuGame"/>, řeší se v <see cref="_ResumeCoordinates"/></summary>
        private bool __SudokuGameIsChanged;
        #endregion
        #region Souřadnice a jejich výpočet
        /// <summary>
        /// Vypočte všechny souřadnice, podle aktuálního rozměru.
        /// </summary>
        private void _SetAllBounds()
        {
            __SudokuBoundsChanged = false;

            var theme = this.Theme;
            string oldThemeSizeHash = __ThemeSizeHashCurrent;
            string newThemeSizeHash = theme?.SizeHash;
            if (!String.Equals(newThemeSizeHash, oldThemeSizeHash))
            {
                _SetRelativeBounds(theme);
                __ThemeSizeHashCurrent = newThemeSizeHash;
            }

            SizeF size = this.ControlSize;
            if (size.Width < 50f || size.Height < 70f) return;

            string oldControlSizeHash = __ControlSizeHashCurrent;
            string newControlSizeHash = $"{size.Width}|{size.Height}";
            if (!String.Equals(oldControlSizeHash, newControlSizeHash))
            {
                _SetAbsoluteBounds(size);
                __ControlSizeHashCurrent = newControlSizeHash;
            }
        }
        #region Relativní souřadnice hry: podle hodnot v Theme vypočte relativní souřadnice a vloží je do všech prvků
        /// <summary>
        /// Metoda určí kompletní relativní souřadnice všech prvků
        /// </summary>
        /// <param name="theme"></param>
        private void _SetRelativeBounds(SudokuSkinTheme theme)
        {
            // Rozpočet velikostí
            __ControlSizeHashCurrent = null;
        }
        #endregion
        #region Absolutní souřadnice: rozdělení prostoru na Game / Control, a následné přepočty relativních souřadnic do těchto souřadnic absolutních
        /// <summary>
        /// Určí absolutní souřadnice segmentů Game a Control, a poté do nich přepočte relativní souřadnice prvků
        /// </summary>
        /// <param name="size"></param>
        private void _SetAbsoluteBounds(SizeF size)
        {
            _SetBasicBounds(size);
            _SetAbsoluteGameBounds();
            _SetAbsoluteControlBounds();
        }
        /// <summary>
        /// Rozmístí souřadnice <see cref="GameBounds"/> a <see cref="ControlBounds"/> do daného prostoru. 
        /// Následující metody už umísťují své prvky do těchto souřadnic.
        /// </summary>
        /// <param name="size"></param>
        private void _SetBasicBounds(SizeF size)
        {
            float gameRatio = 0.80f;                       // Horních 80% obsazuje hra Sudoku
            float gameBorder = 6f;                         // Prázdný okraj okolo Sudoku
            float controlBorder = 6f;                      // Prázdný okraj okolo Controls
            float controlRatio = 5f;                       // Počet prvků v Controls = poměr Šířka / Výška

            float w = size.Width;
            float h = size.Height;
            float yb = gameRatio * h;                      // Nahoře bude hra Sudoku
            float bh = h - yb;                             // Dole bude prostor Controls

            float gw = w - gameBorder - gameBorder;
            float gh = yb - gameBorder - gameBorder;
            float gs = (gw < gh ? gw : gh);
            float gx = (w - gs) / 2f;
            float gy = (yb - gs) / 2f;
            GameBounds = new RectangleF(gx, gy, gs, gs);

            float cw = (w - controlBorder - controlBorder) / controlRatio;
            float ch = h - yb - controlBorder;
            float cs = (cw < ch ? cw : ch);
            float cx = (w - (controlRatio * cs)) / 2f;
            float cy = yb + ((bh - cs) / 2f);
            ControlBounds = new RectangleF(cx, cy, controlRatio * cs, cs);
        }
        private void _SetAbsoluteGameBounds() { }
        private void _SetAbsoluteControlBounds() { }

        public RectangleF GameBounds { get; private set; }
        public RectangleF ControlBounds { get; private set; }
        #endregion
        /// <summary>Obsahuje hash získaný z Theme <see cref="SudokuSkinTheme.SizeHash"/>, pro který je aktuálně spočítaný relativní souřadný systém</summary>
        private string __ThemeSizeHashCurrent;
        /// <summary>Obsahuje hash získaný z <see cref="ControlSize"/> controlu, pro který je aktuálně spočítaný absolutní souřadný systém</summary>
        private string __ControlSizeHashCurrent;
        /// <summary>Obsahuje true po změně objektů, které mají vliv na souřadnice, řeší se v <see cref="_ResumeCoordinates"/></summary>
        private bool __SudokuBoundsChanged;
        #endregion
        #region Suspend / Resume coordinates
        /// <summary>
        /// Pozastaví akce v <see cref="SudokuCoordinates"/>, které jsou běžně vyvolány po setování hodnot do <see cref="SudokuGame"/>, <see cref="Configuration"/>,
        /// <see cref="Theme"/> a <see cref="ControlBounds"/>. Pokud nyní dojde k jejich setování, akce neproběhnou, ale proběhnou až na konci usingu při Dispose vráceného objektu.
        /// </summary>
        /// <returns></returns>
        public IDisposable SuspendCoordinates()
        {
            return new SuspendCoordinatesToken(this);
        }
        /// <summary>
        /// IDisposable instance zajišťující Suspend (v konstruktoru) + Resume (v Dispose).
        /// </summary>
        private class SuspendCoordinatesToken : IDisposable
        {
            /// <summary>
            /// Konstuktor: do předaného ownera nastaví Suspend = true (předtím si zapamatuje výchozí hodnotu).
            /// V Dispose ji do ownera vrátí.
            /// </summary>
            /// <param name="owner"></param>
            public SuspendCoordinatesToken(SudokuCoordinates owner)
            {
                __Owner = owner;
                __PreviousIsSuspended = owner.__IsSuspended;
                owner.__IsSuspended = true;
            }
            /// <summary>
            /// Owner = vlastník tohoto Suspend bloku
            /// </summary>
            private SudokuCoordinates __Owner;
            /// <summary>
            /// Originální hodnota Suspend z Ownera
            /// </summary>
            private bool __PreviousIsSuspended;
            /// <summary>
            /// Dispose vrátí <see cref="__PreviousIsSuspended"/> do Suspendu Ownera. Vyvolá <see cref="_ResumeCoordinates"/> v Owneru. Pak zahodí referenci na něj.
            /// </summary>
            public void Dispose()
            {
                __Owner.__IsSuspended = __PreviousIsSuspended;
                __Owner._ResumeCoordinates();
                __Owner = null;
            }
        }
        /// <summary>
        /// Provede akce, které by měly být provedeny, ale reálně byly suspendovány
        /// </summary>
        private void _ResumeCoordinates()
        {
            if (__SudokuGameIsChanged)
                _LinkSudokuGame();
            if (__SudokuBoundsChanged)
                _SetAllBounds();
        }
        /// <summary>
        /// Aktuálně jsou pozastaveny akce <see cref="_LinkSudokuGame()"/> a <see cref="_SetAllBounds()"/>?
        /// </summary>
        private bool __IsSuspended;
        #endregion

    }
    #endregion
    #region class SudokuConfiguration : Konfigurace chování Sudoku
    /// <summary>
    /// Konfigurace chování Sudoku
    /// </summary>
    public class SudokuConfiguration
    {
        /// <summary>
        /// Výchozí chování dle DJsoft
        /// </summary>
        public static SudokuConfiguration Default
        {
            get
            {
                SudokuConfiguration cfg = new SudokuConfiguration();

                return cfg;
            }
        }
        private SudokuConfiguration()
        {
        }

    }
    #endregion
    #region class SudokuSkinTheme : Vizuální schemata pro Sudoku
    /// <summary>
    /// Vizuální schemata pro Sudoku
    /// </summary>
    public class SudokuSkinTheme
    {
        #region Tvorba jedotlivých schemat
        /// <summary>
        /// Obsahuje schema v základní barvě světle šedé
        /// </summary>
        public static SudokuSkinTheme LightGray
        {
            get
            {
                SudokuSkinTheme scs = new SudokuSkinTheme();
                scs.BackColor = Color.FromArgb(255, 240, 240, 245);
                scs.GameBackColor = Color.FromArgb(255, 245, 245, 250);

                scs.OuterLineColor = Color.FromArgb(255, 100, 100, 100);
                scs.OuterLineSize = 3f;
                scs.GroupLineColor = Color.FromArgb(255, 160, 160, 160);
                scs.GroupLineSize = 2f;
                scs.CellLineColor = Color.FromArgb(255, 160, 160, 160);
                scs.CellLineSize = 1f;

                scs.CellMargin = 2f;

                scs.EmptyCellBackColor = Color.FromArgb(255, 245, 245, 250);
                scs.EmptyCellMouseOnBackColor = Color.FromArgb(255, 250, 250, 180);
                scs.EmptyCellInActiveGroupBackColor = Color.FromArgb(255, 250, 250, 220);

                scs.FixedCellBackColor = Color.FromArgb(255, 220, 220, 230);
                scs.FixedCellMouseOnBackColor = Color.FromArgb(255, 220, 220, 230);
                scs.FixedCellInActiveGroupBackColor = Color.FromArgb(255, 240, 240, 210);
                scs.FixedCellTextColor = Color.FromArgb(255, 0, 0, 0);

                scs.ControlBackColor = Color.FromArgb(255, 235, 235, 245);
                return scs;
            }
        }
        /// <summary>
        /// Konstruktor je privátní. Instance se generují z konkrétních static properties pro konkrétní témata...
        /// </summary>
        private SudokuSkinTheme() { }
        #endregion
        #region Jednotlivé barvy a rozměry
        /// <summary>
        /// Základní barva pozadí celého panelu = mimo hru a Controls
        /// </summary>
        public Color BackColor { get; private set; }
        /// <summary>
        /// Základní barva pozadí pod vlastní hrou (z ní bude vykukovat jen prázdný prostor <see cref="CellMargin"/>, 
        /// a případně bude prosvítat pod poloprůhlednými prvky. Kterákoli barva může mít Alpha kanál menší než 255.
        /// </summary>
        public Color GameBackColor { get; private set; }

        /// <summary>
        /// Barva linky okolo celé plochy (9x9)
        /// </summary>
        public Color OuterLineColor { get; private set; }
        /// <summary>
        /// Šířka linky okolo celé plochy (9x9); uvádí se relativně k buňce o velikosti 90 x 90
        /// </summary>
        public float OuterLineSize { get; private set; }
        /// <summary>
        /// Barva linky okolo jednotlivé grupy (3x3)
        /// </summary>
        public Color GroupLineColor { get; private set; }
        /// <summary>
        /// Šířka linky okolo jednotlivé grupy (3x3); uvádí se relativně k buňce o velikosti 90 x 90
        /// </summary>
        public float GroupLineSize { get; private set; }
        /// <summary>
        /// Barva linky okolo jednotlivé buňky
        /// </summary>
        public Color CellLineColor { get; private set; }
        /// <summary>
        /// Šířka linky okolo jednotlivé buňky; uvádí se relativně k buňce o velikosti 90 x 90
        /// </summary>
        public float CellLineSize { get; private set; }
        /// <summary>
        /// Okraj okolo jedné buňky k nejbližší lince; uvádí se relativně k buňce o velikosti 90 x 90
        /// </summary>
        public float CellMargin { get; private set; }

        /// <summary>
        /// Barva pozadí grupy A (1.1 + 1.3 + 2.2 + 3.1 + 3.3)
        /// </summary>
        public Color GroupABackColor { get; private set; }
        /// <summary>
        /// Barva pozadí grupy B (1.2 + 2.1 + 2.3 + 3.2)
        /// </summary>
        public Color GroupBBackColor { get; private set; }

        public Color EmptyCellBackColor { get; private set; }
        public Color EmptyCellMouseOnBackColor { get; private set; }
        public Color EmptyCellInActiveGroupBackColor { get; private set; }

        public Color FixedCellBackColor { get; private set; }
        public Color FixedCellMouseOnBackColor { get; private set; }
        public Color FixedCellInActiveGroupBackColor { get; private set; }
        public Color FixedCellTextColor { get; private set; }

        public Color ControlBackColor { get; private set; }

        /// <summary>
        /// Obsahuje string, který zahrnuje všechny rozměrové hodnoty (šířky linek a mezer).
        /// Zajistí že po změně těchto hodnot (změna Theme) dojde k přepočtu relativního souřadného systému.
        /// </summary>
        public string SizeHash { get { return $"{OuterLineSize:F1}|{GroupLineSize:F1}|{CellLineSize:F1}|{CellMargin:F1}"; } }
        #endregion
        #region Získání hodnoty pro daný typ prvku
        public Color GetBackColor(SudokuItemType itemType)
        {
            if (itemType.HasFlag(SudokuItemType.PartSudoku))
            {
                // Následující prvky mají jen jednu barvu = nereagují na interaktivní stav:
                if (itemType.HasFlag(SudokuItemType.PartBackgroundArea)) return this.GameBackColor;
                if (itemType.HasFlag(SudokuItemType.PartOuterLine)) return this.OuterLineColor;
                if (itemType.HasFlag(SudokuItemType.PartGroupLine)) return this.GroupLineColor;
                if (itemType.HasFlag(SudokuItemType.PartCellLine)) return this.CellLineColor;

            }

            if (itemType.HasFlag(SudokuItemType.PartControl))
            { }

            return this.BackColor;
        }
        #endregion
    }
    #endregion
    #region class SudokuItem : Jednotlivý prvek GUI Sudoku (fixed, game, config)
    /// <summary>
    /// Jednotlivý prvek GUI Sudoku (fixed, game, config)
    /// </summary>
    public class SudokuItem
    {
        public SudokuItem(SudokuControl owner, Data.Position itemPosition, SudokuItemType itemType, int? itemSubValue)
        {
            __Owner = owner;
            __ItemPosition = itemPosition;
            __ItemType = itemType;
            __ItemSubValue = itemSubValue;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = $"ItemType: {ItemType}; Position: {ItemPosition};";
            if (ItemSubValue.HasValue) text += $" SubValue: {ItemSubValue}";
            return text;
        }
        private readonly SudokuControl __Owner;
        /// <summary>
        /// Typ prvku
        /// </summary>
        public SudokuItemType ItemType { get { return __ItemType; } } private readonly SudokuItemType __ItemType;
        /// <summary>
        /// Pozice prvku = adresa (buňky, grupy, linie)
        /// </summary>
        public Data.Position ItemPosition { get { return __ItemPosition; } } private readonly Data.Position __ItemPosition;
        /// <summary>
        /// Hodnota SubValue = hint
        /// </summary>
        public int? ItemSubValue { get { return __ItemSubValue; } } private readonly int? __ItemSubValue;
        /// <summary>
        /// Pozice relativní v rastru, kde jedna buňka má rozměr 100 x 100.
        /// Je vypočtena po změně rozměrových hodnot v <see cref="SudokuSkinTheme"/>.
        /// Nemění se po změně rozměrů ani po změnách barvy.
        /// </summary>
        public RectangleF BoundsRelative { get; set; }
        /// <summary>
        /// Pozice absolutní koordinátu controlu
        /// </summary>
        public RectangleF BoundsAbsolute { get; set; }
        public bool IsBackground { get; set; }
        public bool IsInteractive { get; set; }
        public bool IsVisible { get; set; }
    }
    #endregion
    #region enumy SudokuItemType
    [Flags]
    public enum SudokuItemType : int
    {
        None = 0,
        /// <summary>
        /// Patří do Sudoku
        /// </summary>
        PartSudoku = 0x0001,
        /// <summary>
        /// Patří do Controls
        /// </summary>
        PartControl = 0x0002,
        /// <summary>
        /// Prostor celého pozadí
        /// </summary>
        PartBackgroundArea = 0x0008,
        /// <summary>
        /// Vnější linka
        /// </summary>
        PartOuterLine = 0x0010,
        /// <summary>
        /// Vnitřní linka mezi grupami (3x3)
        /// </summary>
        PartGroupLine = 0x0020,
        /// <summary>
        /// Vnitřní linka mezi buňkami (1x1)
        /// </summary>
        PartCellLine = 0x0040,
        /// <summary>
        /// Vodorovná linka
        /// </summary>
        PartHorizontalLine = 0x0100,
        /// <summary>
        /// Svislá linka
        /// </summary>
        PartVerticalLine = 0x0200,
        /// <summary>
        /// Jedna grupa 3x3
        /// </summary>
        PartGroup = 0x1000,
        /// <summary>
        /// Jedna buňka
        /// </summary>
        PartCell = 0x2000,
        /// <summary>
        /// Jedna sub-buňka (1/9 v buňce)
        /// </summary>
        PartSubCell = 0x4000,
        /// <summary>
        /// Label v controlech
        /// </summary>
        PartLabel = 0x00010000,
        /// <summary>
        /// Button v controlech
        /// </summary>
        PartButton = 0x00020000,

        SudokuOuterHorizontalLine = PartSudoku | PartHorizontalLine | PartOuterLine,
        SudokuOuterVerticalLine = PartSudoku | PartVerticalLine | PartOuterLine,
        SudokuGroupHorizontalLine = PartSudoku | PartHorizontalLine | PartGroupLine,
        SudokuGroupVerticalLine = PartSudoku | PartVerticalLine | PartGroupLine,
        SudokuCellHorizontalLine = PartSudoku | PartHorizontalLine | PartCellLine,
        SudokuCellVerticalLine = PartSudoku | PartVerticalLine | PartCellLine,
        SudokuGroup = PartSudoku | PartGroup,
        SudokuCell = PartSudoku | PartCell,
        SudokuSubCell = PartSudoku | PartSubCell

    }
    #endregion
}
