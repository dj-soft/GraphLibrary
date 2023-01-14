using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using DjSoft.Games.Animated.Components;

namespace DjSoft.Games.Animated.Sudoku
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

            _FillRandomGame();
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            ((IDisposable)__Coordinates)?.Dispose();
        }
        private void _LinkComponents()
        {
            if (__ComponentsReady)
            {
                using (__Coordinates.SuspendCoordinates())
                {
                    __Coordinates.Theme = Theme;
                    __Coordinates.Configuration = Configuration;
                    __Coordinates.Game = Game;
                }
                _RefreshCoordinates();
            }
        }
        private bool __ComponentsReady = false;
        #endregion
        #region Hra = datová instance a interakce s ní, a konfigurace
        private void _InitGame()
        {
            __Game = new SudokuGame();
            __Configuration = SudokuConfiguration.Default;
        }
        private void _FillRandomGame()
        {
            __Game.Import(SudokuGame.CreateSample());
        }
        public SudokuGame Game { get { return __Game; } set { __Game = value; _LinkComponents(); } } private SudokuGame __Game;
        public SudokuConfiguration Configuration { get { return __Configuration; } set { __Configuration = value; _LinkComponents(); } } private SudokuConfiguration __Configuration;
        #endregion
        #region Vizuální kabát = Theme
        private void _InitTheme()
        {
            Theme = SudokuSkinTheme.Default;
        }
        /// <summary>
        /// Skin / Theme
        /// </summary>
        public SudokuSkinTheme Theme { get { return __Theme; } set { __Theme = value; _LinkComponents(); } } private SudokuSkinTheme __Theme;
        #endregion
        #region Mapa prostoru = kde co je, v závislosti na velikosti controlu
        /// <summary>
        /// Sada všech prvků a jejich souřadnic
        /// </summary>
        public SudokuCoordinates Coordinates { get { return __Coordinates; } }
        /// <summary>
        /// Inicializace <see cref="Coordinates"/>
        /// </summary>
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

            PrepareGraphicsMode(args);

            var coords = __Coordinates;

            // Game Background:
            coords.PaintBackground(args);
            foreach (var item in coords.BackgroundItems)
                item.PaintItem(args);

        }
        protected override void DoPaintStandard(LayeredPaintEventArgs args)
        {
            PrepareGraphicsMode(args);

            var coords = __Coordinates;
            foreach (var item in coords.StandardItems)
                item.PaintItem(args);
        }
        protected virtual void PrepareGraphicsMode(LayeredPaintEventArgs args)
        {
            args.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            args.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        }
        #endregion
    }
    #region class SudokuCoordinates : Souřadnice prvků v Sudoku
    /// <summary>
    /// Souřadnice prvků v Sudoku
    /// </summary>
    public class SudokuCoordinates : IDisposable
    {
        #region Konstruktor a základní vlastnosti a chování
        /// <summary>
        /// Konstruktor a public data
        /// </summary>
        /// <param name="sudokuGame"></param>
        public SudokuCoordinates(SudokuControl owner)
        {
            __Owner = owner;
            _CreateSudokuItems();
        }
        void IDisposable.Dispose()
        {
            _DisposeFonts();
        }
        /// <summary>
        /// Owner control
        /// </summary>
        private SudokuControl __Owner;
        /// <summary>
        /// Aktuální hra
        /// </summary>
        public SudokuGame Game { get { return __Game; } set { _SetSudokuGame(value); } } private SudokuGame __Game;
        /// <summary>
        /// Vloží dodanou hru.
        /// </summary>
        /// <param name="sudokuGame"></param>
        private void _SetSudokuGame(SudokuGame sudokuGame)
        {
            __Game = sudokuGame;
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
        /// <summary>
        /// Zajistí překreslení daných vrstev
        /// </summary>
        /// <param name="repaintBackgroundLayer"></param>
        /// <param name="repaintStardardLayer"></param>
        /// <param name="repaintOverlayLayer"></param>
        private void _RepaintOwner(bool repaintBackgroundLayer, bool repaintStardardLayer, bool repaintOverlayLayer = false)
        {
            if (repaintBackgroundLayer) __Owner.LayerBackgroundValid = false;
            if (repaintStardardLayer) __Owner.LayerBackgroundValid = false;
            if (repaintOverlayLayer) __Owner.LayerOverlayValid = false;
        }
        #endregion
        #region Napojení na hru
        private void _LinkSudokuGame()
        {
            __SudokuGameIsChanged = false;
        }
        /// <summary>Obsahuje true po změně objektu v <see cref="__Game"/>, řeší se v <see cref="_ResumeCoordinates"/></summary>
        private bool __SudokuGameIsChanged;
        #endregion
        #region Prvky SudokuItem - jejich prvotní vytvoření
        /// <summary>
        /// Prvky kreslené na pozadí - pole prvků kreslených na pozadí, nemají vnitřní Childs, jejich vzhled se interaktivně nijak často nemění (rámečky)
        /// </summary>
        public IReadOnlyList<SudokuItem> BackgroundItems { get { return __BackgroundItems; } }
        /// <summary>
        /// Prvky kreslené na standardní vrstvu - lineární pole pro snadné kreslení, netřeba procházet rekurzivně Childs
        /// </summary>
        public IReadOnlyList<SudokuItem> StandardItems { get { return __StandardItems; } }
        /// <summary>
        /// Interaktivní prvky - obsahuje pouze Root prvky, které mají navazující prvky ve svém poli Childs, a které mohou být interaktivní (prvky hry a controly)
        /// </summary>
        public IReadOnlyList<SudokuItem> InteractiveItems { get { return __InteractiveItems; } }
        /// <summary>
        /// Vytvoří pole prvků a naplní jej, jedenkrát v konstruktoru
        /// </summary>
        private void _CreateSudokuItems()
        {
            __BackgroundItems = new List<SudokuItem>();
            __StandardItems = new List<SudokuItem>();
            __InteractiveItems = new List<SudokuItem>();
            __AllItems = new List<SudokuItem>();

            _AddSudokuGameItems();
            _AddSudokuControlItems();
        }
        /// <summary>
        /// Do pole prvků vloží prvky typu Game
        /// </summary>
        private void _AddSudokuGameItemsV1()
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

            // Vygeneruje definice pro všechny grupy + cell + subCell, hierarchicky:
            for (int gRow = 0; gRow < 3; gRow++)
                for (int gCol = 0; gCol < 3; gCol++)
                {
                    var group = _AddOneItem(gRow, gCol, SudokuItemType.GameGroup, false);                        // Group nemá parenta = je Root
                    // Buňky jedné grupy (3x3):
                    for (int cr = 0; cr < 3; cr++)
                        for (int cc = 0; cc < 3; cc++)
                        {
                            int cRow = 3 * gRow + cr;
                            int cCol = 3 * gCol + cc;
                            var cell = _AddOneItem(cRow, cCol, SudokuItemType.GameCell, false, null, group);     // Cell se ukládá do parenta = Group
                            for (int sub = 1; sub <= 9; sub++)
                                _AddOneItem(cRow, cCol, SudokuItemType.GameSubCell, false, sub, cell);           // SubCell se ukládá do parenta = Cell
                        }
                }

            // Přidá prvky (PartHorizontalLine a PartVerticalLine) pro linku na dané pozici a daného typu
            void addLines(int pos, SudokuItemType lineType)
            {
                _AddOneItem(pos, 0, SudokuItemType.PartGame | lineType | SudokuItemType.PartHorizontalLine, true);
                _AddOneItem(0, pos, SudokuItemType.PartGame | lineType | SudokuItemType.PartVerticalLine, true);
            }
        }
        /// <summary>
        /// Do pole prvků vloží prvky typu Game
        /// </summary>
        private void _AddSudokuGameItems()
        {
            // Grupy dáme pod linky:
            for (int gRow = 0; gRow < 3; gRow++)
                for (int gCol = 0; gCol < 3; gCol++)
                    _AddOneItem(gRow, gCol, SudokuItemType.GameGroup, true);           // Group na pozadí

            // Vygeneruje definice pro všechny linky, ve správném pořadí odspodu, ale nad pozadí Groups:
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

            // Vygeneruje definice pro všechny Cell + subCell, hierarchicky:
            for (int cRow = 0; cRow < 9; cRow++)
                for (int cCol = 0; cCol < 9; cCol++)
                {
                    var cell = _AddOneItem(cRow, cCol, SudokuItemType.GameCell, false, null, null);      // Cell je interaktvní a je Root
                    for (int sub = 1; sub <= 9; sub++)
                        _AddOneItem(cRow, cCol, SudokuItemType.GameSubCell, false, sub, cell);           // SubCell se ukládá do parenta = Cell
                }

            // Přidá prvky (PartHorizontalLine a PartVerticalLine) pro linku na dané pozici a daného typu
            void addLines(int pos, SudokuItemType lineType)
            {
                _AddOneItem(pos, 0, SudokuItemType.PartGame | lineType | SudokuItemType.PartHorizontalLine, true);
                _AddOneItem(0, pos, SudokuItemType.PartGame | lineType | SudokuItemType.PartVerticalLine, true);
            }
        }
        /// <summary>
        /// Do pole prvků vloží prvky typu Menu
        /// </summary>
        private void _AddSudokuControlItems()
        {

        }
        /// <summary>
        /// Přidá prvek na dané pozici, daného typu a subValue
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="itemType"></param>
        /// <param name="isBackground"></param>
        /// <param name="itemSubValue"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private SudokuItem _AddOneItem(int row, int col, SudokuItemType itemType, bool isBackground, int? itemSubValue = null, SudokuItem parent = null)
        {
            SudokuItem item = new SudokuItem(this.__Owner, new Position((UInt16)row, (UInt16)col), itemType, itemSubValue);

            // Všechny prvky vložím do __AllItems, pro jejich jednoduché procházení při výpočtu souřadnic:
            __AllItems.Add(item);

            // Tříděno podle vrstvy = pro kreslení:
            if (isBackground)
                __BackgroundItems.Add(item);
            else
                __StandardItems.Add(item);

            // Zařadit do interaktivní hierarchie:
            if (parent is null)
            {   // Prvek nemá parenta => patří do Interactive:
                if (!isBackground)
                    __InteractiveItems.Add(item);
            }
            else
            {   // Child do Parenta:
                parent.AddChild(item);
            }

            return item;
        }
        /// <summary>Prvky kreslené na pozadí - pole prvků kreslených na pozadí, nemají vnitřní Childs, jejich vzhled se interaktivně nijak často nemění (rámečky)</summary>
        private List<SudokuItem> __BackgroundItems;
        /// <summary>Prvky kreslené na vrstvu Standard, lineární pole všech prvků, zde jsou přítomny i Child prvky = není třeba procházet Child prvky rekurzivně.</summary>
        private List<SudokuItem> __StandardItems;
        /// <summary>Interaktivní prvky - obsahuje pouze Root prvky, které mají navazující prvky ve svém poli Childs, a které mohou být interaktivní (prvky hry a controly)</summary>
        private List<SudokuItem> __InteractiveItems;
        /// <summary>Lineární pole obsahující všechny prvky (<see cref="__InteractiveItems"/> + všechy jejich Child prvky) + <see cref="__BackgroundItems"/>, v jedné úrovni, aby nebylo nutno procházet rekurzivně</summary>
        private List<SudokuItem> __AllItems;
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
        /// <summary>
        /// Velikost hry v relativních souřadnicích = do tohoto prostoru jsou umístěny prvky Items a jejich <see cref="SudokuItem.BoundsRelative"/>.
        /// </summary>
        private SizeF __RelativeGameSize;
        /// <summary>
        /// Velikost jedné buňky v relativních souřadnicích
        /// </summary>
        private SizeF __RelativeCellSize;
        /// <summary>
        /// Velikost jedné buňky v absolutních souřadnicích = pixely
        /// </summary>
        private SizeF __AbsoluteCellSize;
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
            var groupIndexes = theme.GroupSizeIndexes;
            var cellIndexes = theme.CellSizeIndexes;
            var lineIndexes = theme.LineSizeIndexes;
            var subCellBounds = theme.RelativeSubCellBounds;

            foreach (var i in this.__AllItems)
                setRelativeBoundsItem(i);

            // Uložíme si souřadnici posledního bodu pole velikostí (Begin + Size) jako relativní velikost hry:
            var last = sizes[sizes.Length - 1];
            float gameSize = last.Item1 + last.Item2;
            __RelativeGameSize = new SizeF(gameSize, gameSize);
            float cellSize = theme.CellSize;
            __RelativeCellSize = new SizeF(cellSize, cellSize);

            void setRelativeBoundsItem(SudokuItem item)
            {
                var itemType = item.ItemType;
                if (itemType.HasFlag(SudokuItemType.PartHorizontalLine))
                    setRelativeBoundsHLine(item);
                else if (itemType.HasFlag(SudokuItemType.PartVerticalLine))
                    setRelativeBoundsVLine(item);
                else if (itemType.HasFlag(SudokuItemType.PartGroup))
                    setRelativeBoundsGroup(item);
                else if (itemType.HasFlag(SudokuItemType.PartCell))
                    setRelativeBoundsCell(item, null);
                else if (itemType.HasFlag(SudokuItemType.PartSubCell))
                    setRelativeBoundsCell(item, item.ItemSubValue);
            }
            void setRelativeBoundsHLine(SudokuItem item)
            {   // Horizontální = vodorovná linka:
                var begin = sizes[lineIndexes[0]];         // Na této souřadnici VLEVO začínají všechny linky (vnější na začátku, vnitřní na konci)
                var end = sizes[lineIndexes[9]];           // Na této souřadnici VPRAVO končí všechny linky   (vnější na konci, vnitřní na začátku)
                var idx = item.ItemPosition.Row;           // Index řádky
                var line = sizes[lineIndexes[idx]];        // Pozice řádky, ve směru Y
                bool isOuter = item.ItemType.HasFlag(SudokuItemType.PartOuterLine);
                var boundsRelative = (isOuter ?
                    new RectangleF(begin.Item1, line.Item1, end.Item1 + end.Item2, line.Item2) :
                    new RectangleF(begin.Item1 + begin.Item2, line.Item1, end.Item1 - (begin.Item1 + begin.Item2), line.Item2));
                item.BoundsRelative = boundsRelative;
            }
            void setRelativeBoundsVLine(SudokuItem item)
            {   // Vertikální = svislá linka:
                var begin = sizes[lineIndexes[0]];         // Na této souřadnici NAHOŘE začínají všechny linky (vnější na začátku, vnitřní na konci)
                var end = sizes[lineIndexes[9]];           // Na této souřadnici DOLE končí všechny linky      (vnější na konci, vnitřní na začátku)
                var idx = item.ItemPosition.Col;           // Index sloupce
                var line = sizes[lineIndexes[idx]];        // Pozice sloupce, ve směru X
                bool isOuter = item.ItemType.HasFlag(SudokuItemType.PartOuterLine);
                var boundsRelative = (isOuter ?
                    new RectangleF(line.Item1, begin.Item1, line.Item2, end.Item1 + end.Item2) :
                    new RectangleF(line.Item1, begin.Item1 + begin.Item2, line.Item2, end.Item1 - (begin.Item1 + begin.Item2)));
                item.BoundsRelative = boundsRelative;
            }
            void setRelativeBoundsGroup(SudokuItem item)
            {
                var row = item.ItemPosition.Row;
                var col = item.ItemPosition.Col;
                var cx = sizes[groupIndexes[col]];
                var cy = sizes[groupIndexes[row]];
                var boundsRelative = new RectangleF(cx.Item1, cy.Item1, cx.Item2, cy.Item2);
                item.BoundsRelative = boundsRelative;
            }
            void setRelativeBoundsCell(SudokuItem item, int? subValue)
            {
                var row = item.ItemPosition.Row;
                var col = item.ItemPosition.Col;
                var cx = sizes[cellIndexes[col]];          // col obsahuje číslo sloupce 0-8; pole cellIndexes obsahuje 9 prvků, kde odpovídající prvek obsahuje index do pole sizes, ...
                var cy = sizes[cellIndexes[row]];          // row stejně tak :                   ... kde jsou konkrétní souřadnice daného prvku (Cell daného čísla)
                var boundsRelative = new RectangleF(cx.Item1, cy.Item1, cx.Item2, cy.Item2);      // Relativní souřadnice

                if (subValue.HasValue)
                    boundsRelative = subCellBounds[subValue.Value - 1].ShiftBy(boundsRelative.Location);

                item.BoundsRelative = boundsRelative;
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
            _PrepareFonts();
            _RepaintOwner(true, true);
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
        /// <summary>
        /// Umístí všechny controly patřící k Game do fyzického prostoru = nastaví jejich <see cref="SudokuItem.BoundsAbsolute"/>
        /// </summary>
        private void _SetAbsoluteGameBounds()
        {
            var virtualSize = this.__RelativeGameSize.Width;
            if (virtualSize <= 10f) return;
            var targetBounds = this.GameBounds;
            var origin = (PointF)targetBounds.Location;
            var zoom = (float)targetBounds.Width / virtualSize;

            foreach (var i in this.__AllItems.Where(i => i.ItemType.HasFlag(SudokuItemType.PartGame)))
                setAbsoluteBoundsItem(i);

            __AbsoluteCellSize = __RelativeCellSize.Zoom(zoom);

            void setAbsoluteBoundsItem(SudokuItem item)
            {
                item.BoundsAbsolute = item.BoundsRelative.Zoom(zoom).ShiftBy(origin);
            }
        }
        private void _SetAbsoluteControlBounds() { }
        /// <summary>
        /// Prostor, kde je kreslena Game. Jeho šířka == výška = vždy jde o čtverec.
        /// </summary>
        public RectangleF GameBounds { get; private set; }
        /// <summary>
        /// Prostor kde jsou kresleny Controls
        /// </summary>
        public RectangleF ControlBounds { get; private set; }
        #endregion
        #endregion
        #region Fonty písma
        /// <summary>
        /// Existují platné fonty pro buňku?
        /// </summary>
        public bool HasValidFonts { get { return __HasValidCellFonts; } }
        /// <summary>
        /// Existují platné fonty pro sub-buňku?
        /// </summary>
        public bool HasValidSubCellFonts { get { return __HasValidSubCellFonts; } }
        /// <summary>
        /// Font pro buňku s fixní hodnotou
        /// </summary>
        public Font CellFixedFont { get { return (__HasValidCellFonts ? __CellFixedFont : null); } }
        /// <summary>
        /// Font pro buňku s nefixní zadanou hodnotou
        /// </summary>
        public Font CellFilledFont { get { return (__HasValidCellFonts ? __CellFilledFont : null); } }
        /// <summary>
        /// Font pro sub-buňku hodnotou
        /// </summary>
        public Font SubCellFont { get { return (__HasValidSubCellFonts ? __SubCellFont : null); } }
        /// <summary>
        /// Připraví fonty pro aktuální velikost buňky <see cref="__AbsoluteCellSize"/>
        /// </summary>
        private void _PrepareFonts()
        {
            __HasValidCellFonts = false;
            __HasValidSubCellFonts = false;
            var height = __AbsoluteCellSize.Height;

            var emSize = (float)(Math.Round(0.48f * height, 1));
            if (emSize < 6.5f) return;                  // Jsme moc malinký...

            if (emSize != __FontCellEmSize || __CellFixedFont is null || __CellFilledFont is null || __SubCellFont is null)
            {
                _DisposeFonts();
                __CellFixedFont = new Font(FontFamily.GenericSansSerif, emSize, FontStyle.Bold);
                __CellFilledFont = new Font(FontFamily.GenericSansSerif, emSize, FontStyle.Regular);
                var emSubSize = emSize / 3f;
                if (emSubSize >= 6f)
                    __SubCellFont = new Font(FontFamily.GenericSansSerif, emSubSize, FontStyle.Regular);
                __FontCellEmSize = emSize;
            }
            __HasValidCellFonts = true;
            __HasValidSubCellFonts = (__SubCellFont != null);
        }
        private void _DisposeFonts()
        {
            __HasValidCellFonts = false;
            __CellFixedFont?.Dispose();
            __CellFixedFont = null;
            __CellFilledFont?.Dispose();
            __CellFilledFont = null;
            __SubCellFont?.Dispose();
            __SubCellFont = null;
        }
        private Font __CellFixedFont;
        private Font __CellFilledFont;
        private Font __SubCellFont;
        private float __FontCellEmSize;
        private bool __HasValidCellFonts;
        private bool __HasValidSubCellFonts;
        #endregion
        #region Suspend / Resume coordinates
        /// <summary>
        /// Pozastaví akce v <see cref="SudokuCoordinates"/>, které jsou běžně vyvolány po setování hodnot do <see cref="Game"/>, <see cref="Configuration"/>,
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
        #region Kreslení
        /// <summary>
        /// Vykreslí pozadí = pouze základní plochy pro Game a Control, nikoli prvky
        /// </summary>
        /// <param name="args"></param>
        public void PaintBackground(LayeredPaintEventArgs args)
        {
            var theme = Theme;
            args.Graphics.FillRectangle(args.GetBrush(theme.GameBackColor), this.GameBounds);
            args.Graphics.FillRectangle(args.GetBrush(theme.ControlBackColor), this.ControlBounds);
        }
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
                scs.OuterLineSize = 6f;
                scs.GroupLineColor = Color.FromArgb(255, 160, 160, 160);
                scs.GroupLineSize = 4f;
                scs.CellLineColor = Color.FromArgb(255, 160, 160, 160);
                scs.CellLineSize = 2f;
                scs.CellMargin = 4f;

                scs.GroupABackColor = Color.FromArgb(255, 235, 230, 235);
                scs.GroupBBackColor = Color.FromArgb(255, 240, 240, 245);

                scs.EmptyCellBackColor = Color.FromArgb(0, 245, 245, 250);
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
            this.GroupsInRowCount = 3;
            this.GroupsInColCount = 3;
            this.CellsInGroupRowCount = 3;
            this.CellsInGroupColCount = 3;

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
        #region Počet buněk a skupin v řadách a sloupcích - zatím nepoužíváme !!!   ( jedeme konstantně (3 x 3)  x  (3 x 3)  =  9 x 9 )
        /// <summary>
        /// Počet skupin vedle sebe = v jednom řádku (ve směru X), typicky 3
        /// </summary>
        public int GroupsInRowCount { get; private set; }
        /// <summary>
        /// Počet skupin pod sebou = v jednom sloupci (ve směru Y), typicky 3
        /// </summary>
        public int GroupsInColCount { get; private set; }
        /// <summary>
        /// Počet buněk v jedné grupě v řádku (ve směru X), typicky 3
        /// </summary>
        public int CellsInGroupRowCount { get; private set; }
        /// <summary>
        /// Počet buněk v jedné grupě v sloupci (ve směru Y), typicky 3
        /// </summary>
        public int CellsInGroupColCount { get; private set; }
        /// <summary>
        /// Počet buněk v jednom řádku celkem (ve směru X) = (<see cref="GroupsInRowCount"/> * <see cref="CellsInGroupRowCount"/>), typicky 9
        /// </summary>
        public int CellsInRowCount { get { return (GroupsInRowCount * CellsInGroupRowCount); } }
        /// <summary>
        /// Počet buněk v jednom sloupci celkem (ve směru Y) = (<see cref="GroupsInColCount"/> * <see cref="CellsInGroupColCount"/>), typicky 9
        /// </summary>
        public int CellsInColCount { get { return (GroupsInColCount * CellsInGroupColCount); } }
        #endregion
        #region Souřadný systém v relativních = designových hodnotách
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
        /// Obsahuje indexy buněk pole <see cref="RelativeSizes"/>, na kterých se nachází grupy Group[0] ÷ Group[2]. 
        /// Pole tedy má 3 prvky a slouží jako ukazatel do pole <see cref="RelativeSizes"/>.
        /// Tedy: souřadnice grupy 1 najdeme v <see cref="RelativeSizes"/> na indexu <see cref="GroupSizeIndexes"/>[1].
        /// </summary>
        public int[] GroupSizeIndexes
        {
            get
            {
                List<int> result = new List<int>();
                result.Add(01);
                result.Add(08);
                result.Add(15);
                return result.ToArray();
            }
        }
        /// <summary>
        /// Obsahuje indexy buněk pole <see cref="RelativeSizes"/>, na kterých se nachází buňky Cell[0] ÷ Cell[8].
        /// Pole tedy má 9 prvků a slouží jako ukazatel do pole <see cref="RelativeSizes"/>.
        /// Tedy: souřadnice buňky v řádku 3 najdeme v <see cref="RelativeSizes"/> na indexu <see cref="CellSizeIndexes"/>[3].
        /// </summary>
        public int[] CellSizeIndexes
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
        /// Obsahuje indexy buněk pole <see cref="RelativeSizes"/>, na kterých se nachází souřadnice linek v pořadí Outer - Cell - Cell - Group - Cell - Cell - Group - Cell - Cell - Outer.
        /// Pole tedy má 10 prvků a slouží jako ukazatel do pole <see cref="RelativeSizes"/>.
        /// Tedy: souřadnice linky mezi grupou 1 a 2 v <see cref="RelativeSizes"/> na indexu <see cref="LineSizeIndexes"/>[6].
        /// </summary>
        public int[] LineSizeIndexes
        {
            get
            {
                List<int> result = new List<int>();
                result.Add(00);                  // Outer
                result.Add(03);                  // Cell
                result.Add(05);                  // Cell
                result.Add(07);                  // Group
                result.Add(10);                  // Cell
                result.Add(12);                  // Cell
                result.Add(14);                  // Group
                result.Add(17);                  // Cell
                result.Add(19);                  // Cell
                result.Add(21);                  // Outer
                return result.ToArray();
            }
        }
        /// <summary>
        /// Relativní souřadnice jednotlivých SubCells.
        /// Pole má 9 prvků na indexech 0-8, pro hodnotu SubValue 1-9.
        /// Hodnota je relativní souřadnice SubCell v rámci Cell, je třeba ji tedy do konkrétní SubCell přemístit o počátek konkrétní buňky.
        /// </summary>
        public RectangleF[] RelativeSubCellBounds
        {
            get
            {
                List<RectangleF> result = new List<RectangleF>();
                var subSize = CellSize / 3f;
                addResult(0f, 0f);               // Pozice hodnoty 1
                addResult(1f, 0f);               // Pozice hodnoty 2
                addResult(2f, 0f);               // Pozice hodnoty 3
                addResult(0f, 1f);               // Pozice hodnoty 4
                addResult(1f, 1f);               // Pozice hodnoty 5
                addResult(2f, 1f);               // Pozice hodnoty 6
                addResult(0f, 2f);               // Pozice hodnoty 7
                addResult(1f, 2f);               // Pozice hodnoty 8
                addResult(2f, 2f);               // Pozice hodnoty 9
                return result.ToArray();

                void addResult(float rx, float ry)
                {
                    result.Add(new RectangleF(rx * subSize, ry * subSize, subSize, subSize));
                }
            }
        }
        #endregion
        #region Získání hodnoty pro daný typ prvku
        public Color GetBackColor(SudokuItem item)
        {
            SudokuItemType itemType = item.ItemType;
            if (itemType.HasFlag(SudokuItemType.PartGame))
            {
                // Následující prvky mají jen jednu barvu = nereagují na interaktivní stav:
                if (itemType.HasFlag(SudokuItemType.PartBackgroundArea)) return this.GameBackColor;
                if (itemType.HasFlag(SudokuItemType.PartOuterLine)) return this.OuterLineColor;
                if (itemType.HasFlag(SudokuItemType.PartGroupLine)) return this.GroupLineColor;
                if (itemType.HasFlag(SudokuItemType.PartCellLine)) return this.CellLineColor;

                if (itemType.HasFlag(SudokuItemType.PartGroup))
                {
                    int mod = (item.ItemPosition.Row + item.ItemPosition.Col) % 2;          // 1 nebo 0, střídavě
                    return (mod == 0 ? this.GroupABackColor : this.GroupBBackColor);
                }

                if (itemType.HasFlag(SudokuItemType.PartCell))
                {
                    return this.EmptyCellBackColor;
                }
                if (itemType.HasFlag(SudokuItemType.PartSubCell))
                {
                    return Color.FromArgb(0, 0, 0, 0);
                }

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
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="itemPosition"></param>
        /// <param name="itemType"></param>
        /// <param name="itemSubValue"></param>
        public SudokuItem(SudokuControl owner, Position itemPosition, SudokuItemType itemType, int? itemSubValue)
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
            string text = $"ItemType: {ItemType}; Position: {ItemPosition}";
            if (ItemSubValue.HasValue) text += $"; SubValue: {ItemSubValue}";
            if (HasChilds) text += $"; Childs: {__Childs.Count}";
            text += $"; BoundsRelative: {BoundsRelative}";
            return text;
        }
        private readonly SudokuControl __Owner;
        /// <summary>
        /// Skin / Theme
        /// </summary>
        public SudokuSkinTheme Theme { get { return __Owner.Theme; } }
        /// <summary>
        /// Data hry
        /// </summary>
        public SudokuGame Game { get { return __Owner.Game; } }
        /// <summary>
        /// Sada všech prvků a jejich souřadnic
        /// </summary>
        public SudokuCoordinates Coordinates { get { return __Owner.Coordinates; } }
        /// <summary>
        /// Typ prvku
        /// </summary>
        public SudokuItemType ItemType { get { return __ItemType; } } private readonly SudokuItemType __ItemType;
        /// <summary>
        /// Pozice prvku = adresa (buňky, grupy, linie)
        /// </summary>
        public Position ItemPosition { get { return __ItemPosition; } } private readonly Position __ItemPosition;
        /// <summary>
        /// Hodnota SubValue = hint
        /// </summary>
        public int? ItemSubValue { get { return __ItemSubValue; } } private readonly int? __ItemSubValue;
        /// <summary>
        /// Prvky, nacházející se uvnitř prostoru this prvku.
        /// Jde tedy výhradně o řetěz: Grupa -- Cell -- SubCell.
        /// Prvky se přidávají metodou <see cref="AddChild(SudokuItem)"/>.
        /// Dokud nebude přidán první Child, je zde null. Lze testovat existenci Child v property <see cref="HasChilds"/>.
        /// <para/>
        /// <u>Zásadní upozornění:</u><br/>
        /// <list type="bullet">
        /// <item>Souřadnice Child prvku <see cref="BoundsRelative"/> i <see cref="BoundsAbsolute"/> jsou v koordinátech controlu, nikoli parenta. <br/>
        /// Protože se hledá i kreslí v souřadnicích Controlu.</item>
        /// <item>Child prvek neobsahuje referenci na Parent prvek.<br/>
        /// Protože ji k ničemu nepotřebuje.</item>
        /// </list>
        /// </summary>
        public SudokuItem[] Childs { get { return __Childs?.ToArray(); } } private List<SudokuItem> __Childs;
        /// <summary>
        /// Obsahuje true, pokud this prvek má nejméně jedno <see cref="Childs"/>
        /// </summary>
        public bool HasChilds { get { return (__Childs != null && __Childs.Count > 0); } }
        /// <summary>
        /// Přidá daný prvek
        /// </summary>
        /// <param name="child"></param>
        public void AddChild(SudokuItem child)
        {
            if (child is null) return;
            if (__Childs is null) __Childs = new List<SudokuItem>();
            __Childs.Add(child);
        }
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

        #region Kreslení
        /// <summary>
        /// Vykreslí prvek
        /// </summary>
        /// <param name="args"></param>
        public void PaintItem(LayeredPaintEventArgs args)
        {
            var itemType = this.ItemType;
            if (itemType.HasFlag(SudokuItemType.PartGroup))
                PaintItemGroup(args);
            else if (itemType.HasFlag(SudokuItemType.PartCell))
                PaintItemCell(args);
            else if (itemType.HasFlag(SudokuItemType.PartSubCell))
                PaintItemSubCell(args);
            else
                PaintItemOther(args);
        }
        protected virtual void PaintItemGroup(LayeredPaintEventArgs args)
        {
            var theme = Theme;
            var color = theme.GetBackColor(this);
            if (color.A > 0)
                args.Graphics.FillRectangle(args.GetBrush(color), this.BoundsAbsolute);
        }
        protected virtual void PaintItemCell(LayeredPaintEventArgs args)
        {
            var theme = Theme;
            var color = theme.GetBackColor(this);
            if (color.A > 0)
                args.Graphics.FillRectangle(args.GetBrush(color), this.BoundsAbsolute);

            var cell = this.Game[this.ItemPosition];
            if (cell.Value > 0)
            {
                var bounds = this.BoundsAbsolute.ShiftBy(0f, 1f);
                var image = GetImageForValue(cell.Value);
                if (image != null) args.Graphics.DrawImage(image, bounds);
                DrawString(args, cell.Value.ToString(), this.Coordinates.CellFixedFont, bounds);
            }
        }
        protected virtual void PaintItemSubCell(LayeredPaintEventArgs args)
        { }
        protected virtual void PaintItemOther(LayeredPaintEventArgs args)
        {
            var theme = Theme;
            var color = theme.GetBackColor(this);
            if (color.A > 0)
                args.Graphics.FillRectangle(args.GetBrush(color), this.BoundsAbsolute);
        }
        protected Image GetImageForValue(int value)
        {
            switch (value)
            {
                case 1: return Properties.Resources.Aqua41;
                case 2: return Properties.Resources.Aqua42;
                case 3: return Properties.Resources.Aqua43;
                case 4: return Properties.Resources.Aqua44;
                case 5: return Properties.Resources.Aqua45;
                case 6: return Properties.Resources.Aqua46;
                case 7: return Properties.Resources.Aqua47;
                case 8: return Properties.Resources.Aqua48;
                case 9: return Properties.Resources.Aqua49;
            }
            return null;
        }
        protected virtual void DrawString(LayeredPaintEventArgs args, string text, Font font, RectangleF bounds)
        {
            if (font is null || String.IsNullOrEmpty(text)) return;
            var textSize = args.Graphics.MeasureString(text, font);
            var textBounds = textSize.AlignTo(bounds, ContentAlignment.MiddleCenter);

            args.Graphics.DrawString(text, this.Coordinates.CellFixedFont, args.GetBrush(Color.Black), textBounds.Location);
        }
        #endregion

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
