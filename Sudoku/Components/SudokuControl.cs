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
            Theme = SudokuSkinTheme.Default;
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
        /// Vytvoří pole prvků a naplní jej, jedenkrát v konstruktoru
        /// </summary>
        private void _CreateSudokuItems()
        {
            __Items = new List<SudokuItem>();
            _AddSudokuGameItems();
            _AddSudokuControlItems();
        }
        /// <summary>
        /// Do pole prvků vloží prvky typu Game
        /// </summary>
        private void _AddSudokuGameItems()
        {
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
                    addItem(row, col, SudokuItemType.GameGroup);

            // Vygeneruje definice pro všechny buňky včetně SubValue:
            for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                {
                    addItem(row, col, SudokuItemType.GameCell);
                    for (int sub = 1; sub <= 9; sub++)
                        addItem(row, col, SudokuItemType.GameSubCell, sub);
                }

            // Přidá prvky (PartHorizontalLine a PartVerticalLine) pro linku na dané pozici a daného typu
            void addLines(int pos, SudokuItemType lineType)
            {
                addItem(pos, 0, SudokuItemType.PartGame | lineType | SudokuItemType.PartHorizontalLine);
                addItem(0, pos, SudokuItemType.PartGame | lineType | SudokuItemType.PartVerticalLine);
            }

            // Přidá prvek na dané souřadnici, daného typu a subValue
            void addItem(int row, int col, SudokuItemType itemType, int? itemSubValue = null)
            {
                SudokuItem item = new SudokuItem(this.__Owner, new Data.Position((UInt16)row, (UInt16)col), itemType, itemSubValue);
                __Items.Add(item);
            }
        }
        /// <summary>
        /// Do pole prvků vloží prvky typu Menu
        /// </summary>
        private void _AddSudokuControlItems()
        {

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

            var theme = this.Theme ?? SudokuSkinTheme.Default;
            string oldThemeSizeHash = __ThemeSizeHashCurrent;
            string newThemeSizeHash = theme.SizeHash;
            if (!String.Equals(newThemeSizeHash, oldThemeSizeHash))
            {
                _SetRelativeBounds(theme);
                __ThemeSizeHashCurrent = newThemeSizeHash;
                __ControlSizeHashCurrent = null;           // Zajistí přepočet souřadnic Absolute
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
        /// <summary>Obsahuje hash získaný z Theme <see cref="SudokuSkinTheme.SizeHash"/>, pro který je aktuálně spočítaný relativní souřadný systém</summary>
        private string __ThemeSizeHashCurrent;
        /// <summary>Obsahuje hash získaný z <see cref="ControlSize"/> controlu, pro který je aktuálně spočítaný absolutní souřadný systém</summary>
        private string __ControlSizeHashCurrent;
        /// <summary>Obsahuje true po změně objektů, které mají vliv na souřadnice, řeší se v <see cref="_ResumeCoordinates"/></summary>
        private bool __SudokuBoundsChanged;
        #region Relativní souřadnice hry: podle hodnot v Theme vypočte relativní souřadnice a vloží je do všech prvků
        /// <summary>
        /// Metoda určí kompletní relativní souřadnice všech prvků
        /// </summary>
        /// <param name="theme"></param>
        private void _SetRelativeBounds(SudokuSkinTheme theme)
        {
            // Rozpočet velikostí vychází z hodnot v theme:
            var sizes = theme.RelativeSizes;
            var cellPositions = theme.CellSizePositions;
            var subSizes = theme.RelativeSubSizes;

            foreach (var item in this.__Items)
                item.BoundsRelative = getRelativeBounds(item);

            RectangleF getRelativeBounds(SudokuItem item)
            {
                var itemType = item.ItemType;
                if (itemType.HasFlag(SudokuItemType.PartCell))
                    getRelativeBoundsCell(item, null);
                if (itemType.HasFlag(SudokuItemType.PartSubCell))
                    getRelativeBoundsCell(item, item.ItemSubValue);

                return RectangleF.Empty;
            }
            RectangleF getRelativeBoundsCell(SudokuItem item, int? subValue)
            {
                var row = item.ItemPosition.Row;
                var col = item.ItemPosition.Col;
                var cx = sizes[cellPositions[col]];
                var cy = sizes[cellPositions[row]];
                RectangleF cellBounds = new RectangleF(cx.Item1, cy.Item1, cx.Item2, cy.Item2);

                return cellBounds;
            }
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
        /// Defaultní schema
        /// </summary>
        public static SudokuSkinTheme Default { get { return LightGray; } }
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
        private SudokuSkinTheme()
        {
            this.CellSize = 90f;
        }
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
        /// Šířka linky okolo celé plochy (9x9);
        /// uvádí se relativně k velikosti buňky <see cref="CellSize"/>, což je defaultně 90.
        /// </summary>
        public float OuterLineSize { get; private set; }
        /// <summary>
        /// Barva linky okolo jednotlivé grupy (3x3)
        /// </summary>
        public Color GroupLineColor { get; private set; }
        /// <summary>
        /// Šířka linky okolo jednotlivé grupy (3x3);
        /// uvádí se relativně k velikosti buňky <see cref="CellSize"/>, což je defaultně 90.
        /// </summary>
        public float GroupLineSize { get; private set; }
        /// <summary>
        /// Barva linky okolo jednotlivé buňky
        /// </summary>
        public Color CellLineColor { get; private set; }
        /// <summary>
        /// Šířka linky okolo jednotlivé buňky;
        /// uvádí se relativně k velikosti buňky <see cref="CellSize"/>, což je defaultně 90.
        /// </summary>
        public float CellLineSize { get; private set; }
        /// <summary>
        /// Okraj okolo jedné buňky k nejbližší lince; uvádí se relativně k velikosti buňky <see cref="CellSize"/>, což je defaultně 90
        /// </summary>
        public float CellMargin { get; private set; }
        /// <summary>
        /// Velikost jedné buňky. Hodnota je 90 proto, aby bylo možno dělit na 3x3 SubCell.
        /// </summary>
        public float CellSize { get; private set; }

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
        /// <summary>
        /// Souřadnice jednotlivých prvků, shodné pro osu X i Y.
        /// Každý prvek tohoto pole obsahuje Tuple obsahující Item1 = počátek a Item2 = velikost.
        /// Pořadí prvků je logické a fixní: 
        /// [00] = OuterLine; 
        /// [01] = Group0; 
        /// [02] = Cell0;  
        /// [03] = CellLine;  
        /// [04] = Cell1;  
        /// [05] = CellLine;  
        /// [06] = Cell2;  
        /// [07] = GroupLine; 
        /// [08] = Group1;  
        /// [09] = Cell3;  
        /// [10] = CellLine;  
        /// [11] = Cell4;  
        /// [12] = CellLine;  
        /// [13] = Cell5;  
        /// [14] = GroupLine; 
        /// [15] = Group2;  
        /// [16] = Cell6;  
        /// [17] = CellLine;  
        /// [18] = Cell7;  
        /// [19] = CellLine;  
        /// [20] = Cell8;  
        /// [21] = GroupLine;
        /// OuterLine; 
        /// </summary>
        public Tuple<float, float>[] RelativeSizes
        {
            get
            {
                List<Tuple<float, float>> result = new List<Tuple<float, float>>();

                var ol = OuterLineSize;
                var gl = GroupLineSize;
                var cl = CellLineSize;
                var cm = CellMargin;
                var cs = CellSize;
                var gs = 6f * cm + 3f * cs + 2f * cl;
                float position = 0f;

                add(ref position, ol, ol);                 // [00] = OuterLine 0

                add(ref position, gs, cm);                 // [01] = Grupa 0 navazuje hned na OuterLine a zahrnuje 3 buňky + 2x3 margins + 2x InnerLine, ale position se posune jen o 1 margin
                add(ref position, cs, cs + cm);            // [02] = Cell 0 + 1x margin za ní
                add(ref position, cl, cl + cm);            // [03] = CellLine 0-1, + 1x margin za ní
                add(ref position, cs, cs + cm);            // [04] = Cell 1 + 1x margin za ní
                add(ref position, cl, cl + cm);            // [05] = CellLine 1-2, + 1x margin za ní
                add(ref position, cs, cs + cm);            // [06] = Cell 2 + 1x margin za ní

                add(ref position, gl, gl);                 // [07] = GroupLine 2-3, bez posunu navíc

                add(ref position, gs, cm);                 // [08] = Grupa 1 navazuje hned na GroupLine a zahrnuje 3 buňky + 2x3 margins + 2x InnerLine, ale position se posune jen o 1 margin
                add(ref position, cs, cs + cm);            // [09] = Cell 3 + 1x margin za ní
                add(ref position, cl, cl + cm);            // [10] = CellLine 3-4, + 1x margin za ní
                add(ref position, cs, cs + cm);            // [11] = Cell 4 + 1x margin za ní
                add(ref position, cl, cl + cm);            // [12] = CellLine 4-5, + 1x margin za ní
                add(ref position, cs, cs + cm);            // [13] = Cell 5 + 1x margin za ní

                add(ref position, gl, gl);                 // [14] = GroupLine 5-6, bez posunu navíc

                add(ref position, gs, cm);                 // [15] = Grupa 2 navazuje hned na OuterLine a zahrnuje 3 buňky + 2x3 margins + 2x InnerLine, ale position se posune jen o 1 margin
                add(ref position, cs, cs + cm);            // [16] = Cell 6 + 1x margin za ní
                add(ref position, cl, cl + cm);            // [17] = CellLine 6-7, + 1x margin za ní
                add(ref position, cs, cs + cm);            // [18] = Cell 7 + 1x margin za ní
                add(ref position, cl, cl + cm);            // [19] = CellLine 7-8, + 1x margin za ní
                add(ref position, cs, cs + cm);            // [20] = Cell 8 + 1x margin za ní

                add(ref position, ol, ol);                 // [21] = OuterLine 1

                return result.ToArray();

                // Do pole 'result' přidá nový prvek, kde Item1 = ref 'p' a Item2 = 'size', a poté k 'p' přičte 'shift'.
                void add(ref float p, float size, float shift)
                {
                    result.Add(new Tuple<float, float>(p, size));
                    p += shift;
                }
            }
        }
        /// <summary>
        /// Obsahuje indexy buněk pole <see cref="RelativeSizes"/>, na kterých se nachází buňky Cell[0] ÷ Cell[8]. Pole tedy má 9 prvků.
        /// Tedy: souřadnice buňky v řádku 3 najdeme v <see cref="RelativeSizes"/> na indexu <see cref="CellSizePositions"/>[3].
        /// </summary>
        public int[] CellSizePositions
        {
            get
            {
                List<int> result = new List<int>();
                result.Add(02);
                result.Add(04);
                result.Add(06);
                result.Add(09);
                result.Add(11);
                result.Add(13);
                result.Add(16);
                result.Add(18);
                result.Add(20);
                return result.ToArray();
            }
        }
        /// <summary>
        /// Relativní souřadnice jednotlivých SubCells
        /// </summary>
        public Tuple<float, float>[] RelativeSubSizes
        {
            get
            {
                List<Tuple<float, float>> result = new List<Tuple<float, float>>();

                var cs = CellSize;
                var ss = cs / 3f;
                float position = 0f;

                add(ref position, ss, ss);                 // [00] = SubCell 0
                add(ref position, ss, ss);                 // [00] = SubCell 1
                add(ref position, ss, ss);                 // [00] = SubCell 2

                return result.ToArray();

                // Do pole 'result' přidá nový prvek, kde Item1 = ref 'p' a Item2 = 'size', a poté k 'p' přičte 'shift'.
                void add(ref float p, float size, float shift)
                {
                    result.Add(new Tuple<float, float>(p, size));
                    p += shift;
                }
            }
        }
        public Color GetBackColor(SudokuItemType itemType)
        {
            if (itemType.HasFlag(SudokuItemType.PartGame))
            {
                // Následující prvky mají jen jednu barvu = nereagují na interaktivní stav:
                if (itemType.HasFlag(SudokuItemType.PartBackgroundArea)) return this.GameBackColor;
                if (itemType.HasFlag(SudokuItemType.PartOuterLine)) return this.OuterLineColor;
                if (itemType.HasFlag(SudokuItemType.PartGroupLine)) return this.GroupLineColor;
                if (itemType.HasFlag(SudokuItemType.PartCellLine)) return this.CellLineColor;

            }

            if (itemType.HasFlag(SudokuItemType.PartMenu))
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
        PartGame = 0x0001,
        /// <summary>
        /// Patří do Controls
        /// </summary>
        PartMenu = 0x0002,
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

        GameOuterHorizontalLine = PartGame | PartHorizontalLine | PartOuterLine,
        GameOuterVerticalLine = PartGame | PartVerticalLine | PartOuterLine,
        GameGroupHorizontalLine = PartGame | PartHorizontalLine | PartGroupLine,
        GameuGroupVerticalLine = PartGame | PartVerticalLine | PartGroupLine,
        GameCellHorizontalLine = PartGame | PartHorizontalLine | PartCellLine,
        GameCellVerticalLine = PartGame | PartVerticalLine | PartCellLine,
        GameGroup = PartGame | PartGroup,
        GameCell = PartGame | PartCell,
        GameSubCell = PartGame | PartSubCell

    }
    #endregion
}
