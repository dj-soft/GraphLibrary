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
            this.UseOverlayLayer = true;

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
            if (e.Button == MouseButtons.None)
                _MouseMoveNone(e.Location);
        }
        private void _MouseDown(object sender, MouseEventArgs e)
        {
        }
        private void _MouseUp(object sender, MouseEventArgs e)
        {
        }
        private void _MouseMoveNone(Point location)
        {
            __ItemsOnMouse = _GetItemsOnPoint(location, true);
        }
        private SudokuItem[] _GetItemsOnPoint(PointF mousePoint, bool callMouseChangeToItems)
        {
            // Najdu aktuální prvky na dané pozici:
            List<SudokuItem> mouseItems = new List<SudokuItem>();
            foreach (var item in __Coordinates.InteractiveItems)
                item.ScanItemsOnPoint(mousePoint, mouseItems);

            // Zavolám změny myši, pokud je to požadováno:
            if (callMouseChangeToItems)
            {
                var pairs = __ItemsOnMouse.GetPairs(mouseItems, useReferenceEquals: true);
                foreach (var pair in pairs)
                {
                    if (pair.HasItem1 && !pair.HasItem2)
                        pair.Item1.MouseState = InteractiveMouseState.MouseLeave;
                    else if (!pair.HasItem1 && pair.HasItem2)
                        pair.Item2.MouseState = InteractiveMouseState.MouseEnter;
                }
            }

            return mouseItems.ToArray();
        }
        /// <summary>
        /// Prvky pod myší, aktuálně
        /// </summary>
        private SudokuItem[] __ItemsOnMouse;
        #endregion
        #region Kreslení = Paint do vrstev
        /// <summary>
        /// Zajistí překreslení daných vrstev. Jde tedy o vznesení požadavku na RePaint, nikoli o jeho provádění!
        /// </summary>
        /// <param name="repaintBackgroundLayer"></param>
        /// <param name="repaintStardardLayer"></param>
        /// <param name="repaintOverlayLayer"></param>
        public void Repaint(bool repaintBackgroundLayer, bool repaintStardardLayer, bool repaintOverlayLayer = false)
        {
            if (repaintBackgroundLayer) this.LayerBackgroundValid = false;
            if (repaintStardardLayer) this.LayerBackgroundValid = false;
            if (repaintOverlayLayer) this.LayerOverlayValid = false;
        }
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
        protected override void DoPaintOverlay(LayeredPaintEventArgs args)
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

        internal void PaintBack(LayeredPaintEventArgs args, RectangleF bounds, SudokuSkinTheme.BackProperties backProperties)
        {
            if (backProperties is null) return;
            if (backProperties.Image != null)
                args.Graphics.DrawImage(backProperties.Image, bounds);
            if (backProperties.Color.HasValue && backProperties.Color.Value.A > 0)
                args.Graphics.FillRectangle(args.GetBrush(backProperties.Color.Value), bounds);
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
            for (int v = 1; v <= 9; v++)
                _AddOneButton(SudokuItemType.PartMenu | SudokuItemType.PartButton, SudokuButtonType.Value, v.ToString(), true, v);

            _AddOneButton(SudokuItemType.PartMenu | SudokuItemType.PartButton, SudokuButtonType.ResetGame, "", true);
            _AddOneButton(SudokuItemType.PartMenu | SudokuItemType.PartButton, SudokuButtonType.NewGame, "", true);
            _AddOneButton(SudokuItemType.PartMenu | SudokuItemType.PartButton, SudokuButtonType.Hint, "", true);
            _AddOneButton(SudokuItemType.PartMenu | SudokuItemType.PartButton, SudokuButtonType.NewGameLight, "", false);
            _AddOneButton(SudokuItemType.PartMenu | SudokuItemType.PartButton, SudokuButtonType.NewGameMedium, "", false);
            _AddOneButton(SudokuItemType.PartMenu | SudokuItemType.PartButton, SudokuButtonType.NewGameHard, "", false);
            _AddOneButton(SudokuItemType.PartMenu | SudokuItemType.PartButton, SudokuButtonType.Back, "", false);
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
            Position position = new Position((UInt16)row, (UInt16)col);
            SudokuItem item = new SudokuItem(this.__Owner, itemType, position, itemSubValue);
            return _AddOneItem(item, isBackground, parent);
        }
        /// <summary>
        /// Přidá prvek na dané pozici, daného typu a subValue
        /// </summary>
        /// <param name="itemType"></param>
        /// <param name="buttonType"></param>
        /// <param name="visible"></param>
        /// <param name="itemSubValue"></param>
        /// <returns></returns>
        private SudokuItem _AddOneButton(SudokuItemType itemType, SudokuButtonType buttonType, string buttonText, bool visible = true, int? itemSubValue = null)
        {
            SudokuItem item = new SudokuItem(this.__Owner, itemType, buttonType, buttonText, itemSubValue);
            return _AddOneItem(item, false, null);
        }
        /// <summary>
        /// Přidá daný prvek do evidence
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isBackground"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private SudokuItem _AddOneItem(SudokuItem item, bool isBackground, SudokuItem parent = null)
        {
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
        /// Relativní velikost Menu, na tuto velikost je kalkulvána hodnota <see cref="SudokuItem.BoundsRelative"/>
        /// </summary>
        private SizeF __RelativeMenuSize;
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
            var menuSize = new SizeF(400f, 100f);

            foreach (var i in this.__AllItems)
                setRelativeBoundsItem(i);

            // Uložíme si souřadnici posledního bodu pole velikostí (Begin + Size) jako relativní velikost hry:
            var last = sizes[sizes.Length - 1];
            float gameSize = last.Item1 + last.Item2;
            __RelativeGameSize = new SizeF(gameSize, gameSize);
            float cellSize = theme.CellSize;
            __RelativeCellSize = new SizeF(cellSize, cellSize);
            __RelativeMenuSize = menuSize;

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
                else if (itemType.HasFlag(SudokuItemType.PartButton))
                    setRelativeBoundsButton(item);
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
            // Určuje relativní souřadnici prvku Button
            void setRelativeBoundsButton(SudokuItem item)
            {
                if (item.ButtonType == SudokuButtonType.Value && item.ItemSubValue.HasValue && item.ItemSubValue.Value > 0)
                {   // Tlačítko pro hodnotu 1-9:
                    float v = (float)(item.ItemSubValue.Value - 1);
                    float w = (menuSize.Width / 9f);
                    float x = v * w;
                    float y = 0f;
                    float h = (0.45f * menuSize.Height);
                    item.BoundsRelative = new RectangleF(x, y, w, h);
                }
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
            _SetAbsoluteItemBounds();
            _PrepareFonts();
            _RepaintOwner(true, true);
        }
        /// <summary>
        /// Rozmístí souřadnice <see cref="GameBounds"/> a <see cref="MenuBounds"/> do daného prostoru. 
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
            MenuBounds = new RectangleF(cx, cy, controlRatio * cs, cs);
        }
        /// <summary>
        /// Umístí všechny prvky patřící k Game i Menu do fyzického prostoru = nastaví jejich <see cref="SudokuItem.BoundsAbsolute"/> podle jejich <see cref="SudokuItem.BoundsRelative"/> tak,
        /// aby se správně umístily do <see cref="GameBounds"/>.
        /// </summary>
        private void _SetAbsoluteItemBounds()
        {
            // Přepočty pro prvky Game:
            var relativeGameSize = this.__RelativeGameSize;
            if (relativeGameSize.Width <= 90f) return;
            var gameBounds = this.GameBounds;
            var gameOrigin = (PointF)gameBounds.Location;
            var gameZoom = (float)gameBounds.Width / relativeGameSize.Width;
            __AbsoluteCellSize = __RelativeCellSize.Zoom(gameZoom);

            // Přepočty pro prvky Menu:
            var relativeMenuSize = this.__RelativeMenuSize;
            if (relativeMenuSize.Height <= 20f) return;
            var menuBounds = this.MenuBounds;
            var menuOrigin = (PointF)menuBounds.Location;
            var menuZoomX = (float)menuBounds.Width / relativeMenuSize.Width;
            var menuZoomY = (float)menuBounds.Height / relativeMenuSize.Height;

            // Všechny prvky:
            foreach (var i in this.__AllItems)
            {
                if (i.ItemType.HasFlag(SudokuItemType.PartGame))
                    setAbsoluteGameBoundsItem(i);
                else if (i.ItemType.HasFlag(SudokuItemType.PartButton))
                    setAbsoluteMenuBoundsItem(i);
            }

            void setAbsoluteGameBoundsItem(SudokuItem item)
            {
                item.BoundsAbsolute = item.BoundsRelative.Zoom(gameZoom).ShiftBy(gameOrigin);
            }
            void setAbsoluteMenuBoundsItem(SudokuItem item)
            {
                item.BoundsAbsolute = item.BoundsRelative.Zoom(menuZoomX, menuZoomY).ShiftBy(menuOrigin);
            }
        }
        /// <summary>
        /// Prostor, kde je kreslena Game. Jeho šířka == výška = vždy jde o čtverec.
        /// </summary>
        public RectangleF GameBounds { get; private set; }
        /// <summary>
        /// Prostor kde je kresleno Menu
        /// </summary>
        public RectangleF MenuBounds { get; private set; }
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
        /// Font pro buttony
        /// </summary>
        public Font ButtonFont { get { return (__HasValidButtonFonts ? __ButtonFont : null); } }
        /// <summary>
        /// Připraví fonty pro aktuální velikost buňky <see cref="__AbsoluteCellSize"/>
        /// </summary>
        private void _PrepareFonts()
        {
            __HasValidCellFonts = false;
            __HasValidSubCellFonts = false;
            __HasValidButtonFonts = false;
            var height = __AbsoluteCellSize.Height;

            var emSize = (float)(Math.Round(0.48f * height, 1));     // Optimální konstanta mezi prostorem pro buňku a velikostí fontu
            if (emSize < 6.5f) return;                               // Jsme moc malinký...

            var theme = this.Theme;
            var fixedFontStyle = theme?.FixedCellLook.FontStyle ?? FontStyle.Bold;
            var filledFontStyle = theme?.FilledCellLook.FontStyle ?? FontStyle.Regular;
            if (emSize != __FontCellEmSize || __CellFixedFont is null || __CellFilledFont is null || __SubCellFont is null)
            {
                _DisposeFonts();
                __CellFixedFont = new Font(FontFamily.GenericSansSerif, emSize, fixedFontStyle);
                __CellFilledFont = new Font(FontFamily.GenericSansSerif, emSize, filledFontStyle);

                var emButtonSize = emSize;
                __ButtonFont = new Font(FontFamily.GenericSansSerif, emButtonSize, filledFontStyle);

                var emSubSize = emSize / 3f;
                if (emSubSize >= 6f)
                    __SubCellFont = new Font(FontFamily.GenericSansSerif, emSubSize, FontStyle.Regular);
                __FontCellEmSize = emSize;
            }
            __HasValidCellFonts = true;
            __HasValidSubCellFonts = (__SubCellFont != null);
            __HasValidButtonFonts = true;
        }
        private void _DisposeFonts()
        {
            __HasValidCellFonts = false;
            __CellFixedFont?.Dispose();
            __CellFixedFont = null;
            __CellFilledFont?.Dispose();
            __CellFilledFont = null;

            __HasValidSubCellFonts = false;
            __SubCellFont?.Dispose();
            __SubCellFont = null;

            __HasValidButtonFonts = false;
            __ButtonFont?.Dispose();
            __ButtonFont = null;
        }
        private Font __CellFixedFont;
        private Font __CellFilledFont;
        private Font __SubCellFont;
        private Font __ButtonFont;
        private float __FontCellEmSize;
        private bool __HasValidCellFonts;
        private bool __HasValidSubCellFonts;
        private bool __HasValidButtonFonts;
        #endregion
        #region Suspend / Resume coordinates
        /// <summary>
        /// Pozastaví akce v <see cref="SudokuCoordinates"/>, které jsou běžně vyvolány po setování hodnot do <see cref="Game"/>, <see cref="Configuration"/>,
        /// <see cref="Theme"/> a <see cref="MenuBounds"/>. Pokud nyní dojde k jejich setování, akce neproběhnou, ale proběhnou až na konci usingu při Dispose vráceného objektu.
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
        /// Zajistí překreslení daných vrstev. Jde tedy o vznesení požadavku na RePaint, nikoli o jeho provádění!
        /// </summary>
        /// <param name="repaintBackgroundLayer"></param>
        /// <param name="repaintStardardLayer"></param>
        /// <param name="repaintOverlayLayer"></param>
        private void _RepaintOwner(bool repaintBackgroundLayer, bool repaintStardardLayer, bool repaintOverlayLayer = false)
        {
            __Owner.Repaint(repaintBackgroundLayer, repaintStardardLayer, repaintOverlayLayer);
        }
        /// <summary>
        /// Vykreslí pozadí = pouze základní plochy pro Game a Control, nikoli prvky
        /// </summary>
        /// <param name="args"></param>
        public void PaintBackground(LayeredPaintEventArgs args)
        {
            var theme = Theme;
            __Owner.PaintBack(args, this.GameBounds, theme.GameBackLook);
            __Owner.PaintBack(args, this.MenuBounds, theme.MenuBackLook);
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
                scs.GameBackLook.Color = Color.FromArgb(255, 245, 245, 250);
                scs.MenuBackLook.Color = Color.FromArgb(255, 235, 235, 245);

                scs.OuterLineLook.Color = Color.FromArgb(255, 100, 100, 100);
                scs.OuterLineLook.Width = 6f;
                scs.GroupLineLook.Color = Color.FromArgb(255, 160, 160, 160);
                scs.GroupLineLook.Width = 4f;
                scs.CellLineLook.Color = Color.FromArgb(255, 160, 160, 160);
                scs.CellLineLook.Width = 2f;

                scs.CellMargin = 4f;

                scs.GroupABackLook.Color = Color.FromArgb(255, 235, 230, 235);
                scs.GroupBBackLook.Color = Color.FromArgb(255, 240, 240, 245);

                scs.EmptyCellLook.Color = Color.FromArgb(0, 245, 245, 250);
                scs.EmptyCellLook.ColorMouseOn = Color.FromArgb(255, 250, 250, 180);

                scs.FixedCellLook.Color = Color.FromArgb(255, 220, 220, 230);
                scs.FixedCellLook.ColorMouseOn = Color.FromArgb(255, 220, 220, 230);
                scs.FixedCellLook.TextColor = Color.FromArgb(255, 0, 0, 0);
                scs.FixedCellLook.FontStyle = FontStyle.Bold;

                scs.FilledCellLook.Color = Color.FromArgb(255, 220, 220, 230);
                scs.FilledCellLook.ColorMouseOn = Color.FromArgb(255, 220, 220, 230);
                scs.FilledCellLook.TextColor = Color.FromArgb(255, 0, 0, 0);
                scs.FilledCellLook.FontStyle = FontStyle.Regular;

                scs.ButtonLook.Color = Color.FromArgb(255, 220, 220, 230);
                scs.ButtonLook.ColorMouseOn = Color.FromArgb(255, 220, 220, 230);
                scs.ButtonLook.TextColor = Color.FromArgb(255, 0, 0, 0);
                scs.ButtonLook.FontStyle = FontStyle.Regular;

                scs.ValueImages[0] = Properties.Resources.Aqua41;
                scs.ValueImages[1] = Properties.Resources.Aqua42;
                scs.ValueImages[2] = Properties.Resources.Aqua43;
                scs.ValueImages[3] = Properties.Resources.Aqua44;
                scs.ValueImages[4] = Properties.Resources.Aqua45;
                scs.ValueImages[5] = Properties.Resources.Aqua46;
                scs.ValueImages[6] = Properties.Resources.Aqua47;
                scs.ValueImages[7] = Properties.Resources.Aqua48;
                scs.ValueImages[8] = Properties.Resources.Aqua49;

                scs.ButtonImages.Add(SudokuButtonType.ResetGame, Properties.Resources.actualiser);
                scs.ButtonImages.Add(SudokuButtonType.NewGame, Properties.Resources.sudoku3);
                scs.ButtonImages.Add(SudokuButtonType.Hint, Properties.Resources.Jarovka_0);
                scs.ButtonImages.Add(SudokuButtonType.NewGameLight, Properties.Resources.sudoku6);
                scs.ButtonImages.Add(SudokuButtonType.NewGameMedium, Properties.Resources.sudoku4);
                scs.ButtonImages.Add(SudokuButtonType.NewGameHard, Properties.Resources.sudoku2);
                scs.ButtonImages.Add(SudokuButtonType.Back, Properties.Resources.ArrowLeft2);
               
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
            this.GameBackLook = new BackProperties();
            this.MenuBackLook = new BackProperties();
            this.GroupABackLook = new BackProperties();
            this.GroupBBackLook = new BackProperties();
            this.OuterLineLook = new LineProperties();
            this.GroupLineLook = new LineProperties();
            this.CellLineLook = new LineProperties();
            this.EmptyCellLook = new InteractiveProperties();
            this.FixedCellLook = new InteractiveProperties();
            this.FilledCellLook = new InteractiveProperties();
            this.ErrorCellLook = new InteractiveProperties();
            this.ButtonLook = new InteractiveProperties();
            this.ValueImages = new Image[9];
            this.ButtonImages = new Dictionary<SudokuButtonType, Image>();
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
        public BackProperties GameBackLook { get; private set; }
        /// <summary>
        /// Pozadí pod Menu
        /// </summary>
        public BackProperties MenuBackLook { get; private set; }

        /// <summary>
        /// Barva pozadí grupy A (1.1 + 1.3 + 2.2 + 3.1 + 3.3)
        /// </summary>
        public BackProperties GroupABackLook { get; private set; }
        /// <summary>
        /// Barva pozadí grupy B (1.2 + 2.1 + 2.3 + 3.2)
        /// </summary>
        public BackProperties GroupBBackLook { get; private set; }

        /// <summary>
        /// Barva a šířka linky okolo celé plochy (9x9)
        /// </summary>
        public LineProperties OuterLineLook { get; private set; }
        /// <summary>
        /// Šířka a šířka linky okolo celé plochy (9x9);
        /// uvádí se relativně k velikosti buňky <see cref="CellSize"/>, což je defaultně 90.
        /// </summary>
        public LineProperties GroupLineLook { get; private set; }
        /// <summary>
        /// Barva a šířka linky okolo jednotlivé buňky
        /// </summary>
        public LineProperties CellLineLook { get; private set; }
        /// <summary>
        /// Okraj okolo jedné buňky k nejbližší lince; uvádí se relativně k velikosti buňky <see cref="CellSize"/>, což je defaultně 90
        /// </summary>
        public float CellMargin { get; private set; }
        /// <summary>
        /// Velikost jedné buňky. Hodnota je 90 proto, aby bylo možno dělit na 3x3 SubCell.
        /// </summary>
        public float CellSize { get; private set; }

        public InteractiveProperties EmptyCellLook { get; private set; }
        public InteractiveProperties FixedCellLook { get; private set; }
        public InteractiveProperties FilledCellLook { get; private set; }
        public InteractiveProperties ErrorCellLook { get; private set; }

        public InteractiveProperties ButtonLook { get; private set; }

        /// <summary>
        /// Images [0] ÷ [8] odpovídající hodnotám [1] ÷ [9] : 9 prvků, kde prvek [0] představuje Background image pro buňku s hodnotou 1, atd.
        /// Pole má tedy 9 prvků, ale je možné že obsahují NULL.
        /// </summary>
        public Image[] ValueImages { get; private set; }
        /// <summary>
        /// Images pro jednotlivé Buttony
        /// </summary>
        public Dictionary<SudokuButtonType, Image> ButtonImages { get; private set; }


        /// <summary>
        /// Obsahuje string, který zahrnuje všechny rozměrové hodnoty (šířky linek a mezer).
        /// Zajistí že po změně těchto hodnot (změna Theme) dojde k přepočtu relativního souřadného systému.
        /// </summary>
        public string SizeHash { get { return $"{OuterLineLook.Width:F1}|{GroupLineLook.Width:F1}|{CellLineLook.Width:F1}|{CellMargin:F1}|{CellSize:F1}"; } }
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

                var ol = OuterLineLook.Width;
                var gl = GroupLineLook.Width;
                var cl = CellLineLook.Width;
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
        public BackProperties GetBackInfo(SudokuItem item)
        {
            SudokuItemType itemType = item.ItemType;
            if (itemType.HasFlag(SudokuItemType.PartGame))
            {
                // Následující prvky mají jen jednu barvu = nereagují na interaktivní stav:
                if (itemType.HasFlag(SudokuItemType.PartBackgroundArea)) return this.GameBackLook;
                if (itemType.HasFlag(SudokuItemType.PartOuterLine)) return new BackProperties(this.OuterLineLook);
                if (itemType.HasFlag(SudokuItemType.PartGroupLine)) return new BackProperties(this.GroupLineLook);
                if (itemType.HasFlag(SudokuItemType.PartCellLine)) return new BackProperties(this.OuterLineLook);

                if (itemType.HasFlag(SudokuItemType.PartGroup))
                {
                    int mod = (item.ItemPosition.Row + item.ItemPosition.Col) % 2;          // 1 nebo 0, střídavě
                    var groupLook = (mod == 0 ? this.GroupABackLook : this.GroupBBackLook);
                    return groupLook;
                }

                if (itemType.HasFlag(SudokuItemType.PartCell))
                {   // Buňka má různé styly podle jejího vyplnění:
                    var cellLook = GetCellLook(item);
                    return new BackProperties(cellLook, item.MouseState);
                }

                if (itemType.HasFlag(SudokuItemType.PartSubCell))
                {
                    return null;
                }
            }

            if (itemType.HasFlag(SudokuItemType.PartMenu))
            {
                if (itemType.HasFlag(SudokuItemType.PartButton))
                {
                    var buttonLook = this.ButtonLook;
                    return new BackProperties(buttonLook, item.MouseState);
                }
            }

            return new BackProperties(null, this.BackColor);
        }
        public InteractiveProperties GetTextInfo(SudokuItem item)
        {
            SudokuItemType itemType = item.ItemType;
            if (itemType.HasFlag(SudokuItemType.PartGame))
            {
                if (itemType.HasFlag(SudokuItemType.PartCell))
                {   // Buňka má různé styly podle jejího vyplnění:
                    return GetCellLook(item);
                }

                if (itemType.HasFlag(SudokuItemType.PartSubCell))
                {
                    return GetCellLook(item);
                }
            }

            if (itemType.HasFlag(SudokuItemType.PartMenu))
            {
                if (itemType.HasFlag(SudokuItemType.PartButton))
                {
                    return this.ButtonLook;
                }
            }

            return null;
        }
        /// <summary>
        /// Vrátí vzhled buňky <see cref="InteractiveProperties"/> pro buňku v daném prvku, podle jejího stavu <see cref="Cell.State"/>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private InteractiveProperties GetCellLook(SudokuItem item)
        {
            var cell = item.Cell;
            if (cell is null) return this.EmptyCellLook;
            switch (cell.State)
            {
                case CellState.Empty: return this.EmptyCellLook;
                case CellState.WithTips: return this.EmptyCellLook;
                case CellState.Fixed: return this.FixedCellLook;
                case CellState.Filled: return this.FilledCellLook;
                case CellState.Error: return this.ErrorCellLook;
                default: return this.EmptyCellLook;
            }
        }
        #endregion
        #region SubClasses
        /// <summary>
        /// Grafické vlastnosti linky prvku (vnější, mezi grupami, mezi prvky)
        /// </summary>
        public class LineProperties
        {
            /// <summary>
            /// Barva linky
            /// </summary>
            public Color? Color { get; set; }
            /// <summary>
            /// Šířka linky - relativní vůči ostatním prvkům v layoutu
            /// </summary>
            public float Width { get; set; }
        }
        /// <summary>
        /// Grafické vlastnosti pozadí prvku (hra, group, cell), neaktivní prvek
        /// </summary>
        public class BackProperties
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            public BackProperties() { }
            /// <summary>
            /// Konstruktor
            /// </summary>
            public BackProperties(Image image, Color? color)
            {
                this.Image = image;
                this.Color = color;
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            public BackProperties(LineProperties lineProperties)
            {
                this.Image = null;
                this.Color = lineProperties.Color;
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            public BackProperties(InteractiveProperties interactiveProperties, InteractiveMouseState mouseState)
            {
                this.Image = interactiveProperties.Image;
                this.Color = interactiveProperties.GetColorByMouseState(mouseState);
            }
            /// <summary>
            /// Obrázek na pozadí
            /// </summary>
            public Image Image { get; set; }
            /// <summary>
            /// Barva pozadí, neaktivní
            /// </summary>
            public Color? Color { get; set; }
        }
        /// <summary>
        /// Grafické vlastnosti pozadí prvku (hra, group, cell), interaktivní prvek
        /// </summary>
        public class InteractiveProperties : BackProperties
        {
            /// <summary>
            /// Barva pozadí pokud je prvek Disabled
            /// </summary>
            public Color? ColorDisabled { get; set; }
            /// <summary>
            /// Barva pozadí pokud je prvek pod myší
            /// </summary>
            public Color? ColorMouseOn { get; set; }
            /// <summary>
            /// Barva pozadí pokud je prvek se stisknutou myší
            /// </summary>
            public Color? ColorMouseDown { get; set; }
            /// <summary>
            /// Barva písma
            /// </summary>
            public Color? TextColor { get; set; }
            /// <summary>
            /// Styl písma
            /// </summary>
            public FontStyle FontStyle { get; set; }

            /// <summary>
            /// Vrátí barvu z this <see cref="InteractiveProperties"/> pro daný stav myši
            /// </summary>
            /// <param name="mouseState"></param>
            /// <returns></returns>
            public Color? GetColorByMouseState(InteractiveMouseState mouseState)
            {
                switch (mouseState)
                {
                    case InteractiveMouseState.None: return this.Color;
                    case InteractiveMouseState.MouseEnter: return this.ColorMouseOn;
                    case InteractiveMouseState.MouseEnterAnimating: return this.ColorMouseOn;
                    case InteractiveMouseState.MouseOn: return this.ColorMouseOn;
                    case InteractiveMouseState.MouseLeftDown: return this.ColorMouseDown;
                    case InteractiveMouseState.MouseRightDown: return this.ColorMouseDown;
                    case InteractiveMouseState.MouseLeave: return this.Color;
                    case InteractiveMouseState.MouseLeaveAnimating: return this.Color;
                    case InteractiveMouseState.MouseOff: return this.Color;
                    default: return this.Color;
                }
            }
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
        #region Konstruktor a public data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="itemType"></param>
        /// <param name="itemPosition"></param>
        /// <param name="itemSubValue"></param>
        public SudokuItem(SudokuControl owner, SudokuItemType itemType, Position itemPosition, int? itemSubValue)
        {
            __Owner = owner;
            __ItemType = itemType;
            __ItemPosition = itemPosition;
            __ItemSubValue = itemSubValue;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="itemType"></param>
        /// <param name="buttonType"></param>
        public SudokuItem(SudokuControl owner, SudokuItemType itemType, SudokuButtonType buttonType, string buttonText, int? itemSubValue)
        {
            __Owner = owner;
            __ItemType = itemType;
            __ButtonType = buttonType;
            __ButtonText = buttonText;
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
        protected SudokuSkinTheme Theme { get { return __Owner.Theme; } }
        /// <summary>
        /// Data hry
        /// </summary>
        protected SudokuGame Game { get { return __Owner.Game; } }
        /// <summary>
        /// Sada všech prvků a jejich souřadnic
        /// </summary>
        protected SudokuCoordinates Coordinates { get { return __Owner.Coordinates; } }
        /// <summary>
        /// Animátor
        /// </summary>
        protected Animator Animator { get { return __Owner.Animator; } }
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
        /// Typ buttonu, uvádí se pouze u Controlů (buttony)
        /// </summary>
        public SudokuButtonType ButtonType { get { return __ButtonType; } } private SudokuButtonType __ButtonType;
        /// <summary>
        /// Text buttonu
        /// </summary>
        public string ButtonText { get { return __ButtonText; } } private string __ButtonText;
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
        /// <summary>
        /// Data buňky, pokud this prvek odpovídá buňce
        /// </summary>
        public Cell Cell 
        {
            get
            {
                if (this.ItemType.HasFlag(SudokuItemType.PartCell) || this.ItemType.HasFlag(SudokuItemType.GameSubCell)) return this.Game[this.ItemPosition];
                return null;
            }
        }

        public bool IsVisible { get; set; }

        #endregion
        #region Interaktivita
        /// <summary>
        /// Do dodaného soupisu přidá sebe a své Childs, pokud jejich souřadnice obsahuje daný bod (myši).
        /// Pokud bod leží ve zdejších souřadnicích <see cref="BoundsAbsolute"/>, přidá se a tutéž metopdu spustí pro svoje Childs.
        /// </summary>
        /// <param name="mousePoint"></param>
        /// <param name="mouseItems"></param>
        internal void ScanItemsOnPoint(PointF mousePoint, List<SudokuItem> mouseItems)
        {
            if (!IsInteractive) return;
            if (!BoundsAbsolute.Contains(mousePoint)) return;

            mouseItems.Add(this);

            if (!this.HasChilds) return;
            foreach (var child in this.__Childs)
                child.ScanItemsOnPoint(mousePoint, mouseItems);
        }
        protected virtual bool IsInteractive
        {
            get
            {
                var itemType = this.ItemType;
                if (itemType.HasFlag(SudokuItemType.PartCell)) return true;
                if (itemType.HasFlag(SudokuItemType.PartSubCell)) return false;
                if (itemType.HasFlag(SudokuItemType.PartButton)) return true;
                return false;
            }
        }
        public InteractiveMouseState MouseState
        {
            get { return __MouseState; }
            set { _SetMouseState(value); }
        }
        private void _SetMouseState(InteractiveMouseState mouseState)
        {
            lock (this)
            {
                if (mouseState != __MouseState)
                {
                    if (mouseState == InteractiveMouseState.MouseEnter)
                        _MouseEnterAnimationStart(ref mouseState);
                    else if (mouseState == InteractiveMouseState.MouseLeave)
                        _MouseLeaveAnimationStart(ref mouseState);
                    __MouseState = mouseState;
                    this._RepaintOwner(false, true, false);
                 }
            }
        }
        private InteractiveMouseState _MouseState { get { return __MouseState; } set { __MouseState = value; } }
        private InteractiveMouseState __MouseState = InteractiveMouseState.None;
        /// <summary>
        /// Má být zobrazen obrázek na pozadí?
        /// </summary>
        protected bool IsVisibleValueImage
        {
            get
            {
                var mouseState = this.MouseState;
                return (mouseState == InteractiveMouseState.MouseEnter ||
                        mouseState == InteractiveMouseState.MouseEnterAnimating ||
                        mouseState == InteractiveMouseState.MouseOn ||
                        mouseState == InteractiveMouseState.MouseLeftDown ||
                        mouseState == InteractiveMouseState.MouseRightDown);
                  // || mouseState == InteractiveMouseState.MouseLeaveAnimating);
            }
        }
        #endregion
        #region Animace
        /// <summary>
        /// Zahájí animaci akce MouseEnter
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseEnterAnimationStart(ref InteractiveMouseState mouseState)
        {
            _CurrentAnimationDone();
            __CurrentAnimation = this.Animator.AddMotion(12, Animator.TimeMode.FastStartSlowEnd, 0d, _MouseEnterAnimationStep, __MouseOnOverlayOpacity, 100f, null);
            mouseState = InteractiveMouseState.MouseEnterAnimating;
        }
        /// <summary>
        /// Provede jeden krok animace MouseEnter
        /// </summary>
        /// <param name="motion"></param>
        private void _MouseEnterAnimationStep(Animator.Motion motion)
        {
            if (__MouseState != InteractiveMouseState.MouseEnterAnimating)
            {   // Pokud z nějakého důvodu už nejsme ve stavu MouseEnterAnimating, pak tento animační pohyb ukončíme:
                motion.IsDone = true;
            }
            else
            {
                bool isDone = motion.IsDone;
                __MouseOnOverlayOpacity = (isDone ? 100f : (float)motion.CurrentValue);
                if (isDone) _MouseState = InteractiveMouseState.MouseOn;
                _RepaintOverlay(isDone);
            }
            if (motion.IsDone) __CurrentAnimation = null;
        }
        /// <summary>
        /// Zahájí animaci akce MouseLeave
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseLeaveAnimationStart(ref InteractiveMouseState mouseState)
        {
            _CurrentAnimationDone();
            __CurrentAnimation = this.Animator.AddMotion(45, Animator.TimeMode.FastStartSlowEnd, 0d, _MouseLeaveAnimationStep, __MouseOnOverlayOpacity, 0f, null);
            mouseState = InteractiveMouseState.MouseLeaveAnimating;
        }
        /// <summary>
        /// Provede jeden krok animace MouseLeave
        /// </summary>
        /// <param name="motion"></param>
        private void _MouseLeaveAnimationStep(Animator.Motion motion)
        {
            if (__MouseState != InteractiveMouseState.MouseLeaveAnimating)
            {   // Pokud z nějakého důvodu už nejsme ve stavu MouseLeaveAnimating, pak tento animační pohyb ukončíme:
                motion.IsDone = true;
            }
            else
            {
                bool isDone = motion.IsDone;
                __MouseOnOverlayOpacity = (isDone ? 0f : (float)motion.CurrentValue);
                if (isDone) _MouseState = InteractiveMouseState.MouseOff;
                _RepaintOverlay(isDone);
            }
            if (motion.IsDone) __CurrentAnimation = null;
        }
        private void _RepaintOverlay(bool isDone)
        {
            bool isOverlay = (__MouseOnOverlayOpacity > 0f);
            __Owner.LayerOverlayActive = true;
            _RepaintOwner(false, isDone, !isDone);
        }
        private void _PaintOverlayAnimation(LayeredPaintEventArgs args)
        {
            var ratio = __MouseOnOverlayOpacity;
            if (ratio > 0f)
            {
                byte alpha = (byte)(160f * ratio / 100f);
                Color color = Color.FromArgb(alpha, 255, 255, 180);
                args.Graphics.FillRectangle(args.GetBrush(color), this.BoundsAbsolute);
                PaintItemCellContent(args);

            }
        }
        /// <summary>
        /// Viditelnost barvy MouseOn ve vrstvě Overlay pro this buňku.
        /// Hodnota 0 = barva MouseOn není zobrazena, hodnoty 1-99 = postupná animace (probíhající) do plného zobrazení barvy, hodnota 100 = plně viditelná barva.
        /// Upozornění: barva sama smí mít Alha kanál menší než 255, pak i při plně viditelné barvě smí být částečně průhledná.
        /// </summary>
        private float __MouseOnOverlayOpacity;
        /// <summary>
        /// Pokud aktuálně běží nějaká animace, pak ji ukončí.
        /// </summary>
        private void _CurrentAnimationDone()
        {
            var motion = __CurrentAnimation;
            if (motion != null)
            {
                motion.IsDone = true;
                __CurrentAnimation = null;
            }
        }
        /// <summary>
        /// Aktuálně probíhající animace. Jedna buňka má vždy aktivní nejvýše jednu animaci.
        /// </summary>
        private Animator.Motion __CurrentAnimation;
        #endregion
        #region Kreslení
        /// <summary>
        /// Zajistí překreslení daných vrstev. Jde tedy o vznesení požadavku na RePaint, nikoli o jeho provádění!
        /// </summary>
        /// <param name="repaintBackgroundLayer"></param>
        /// <param name="repaintStardardLayer"></param>
        /// <param name="repaintOverlayLayer"></param>
        private void _RepaintOwner(bool repaintBackgroundLayer, bool repaintStardardLayer, bool repaintOverlayLayer = false)
        {
            __Owner.Repaint(repaintBackgroundLayer, repaintStardardLayer, repaintOverlayLayer);
        }
        /// <summary>
        /// Vykreslí prvek
        /// </summary>
        /// <param name="args"></param>
        public void PaintItem(LayeredPaintEventArgs args)
        {
            var itemType = this.ItemType;
            if (itemType.HasFlag(SudokuItemType.PartHorizontalLine))
                PaintItemHorizontalLine(args);
            if (itemType.HasFlag(SudokuItemType.PartVerticalLine))
                PaintItemVerticalLine(args);
            if (itemType.HasFlag(SudokuItemType.PartGroup))
                PaintItemGroup(args);
            else if (itemType.HasFlag(SudokuItemType.PartCell))
                PaintItemCell(args);
            else if (itemType.HasFlag(SudokuItemType.PartSubCell))
                PaintItemSubCell(args);
            else if (itemType.HasFlag(SudokuItemType.PartButton))
                PaintItemButton(args);
            else
                PaintItemOther(args);
        }
        protected virtual void PaintItemHorizontalLine(LayeredPaintEventArgs args)
        {
            if (args.Layer != LayerType.Background) return;

            var bounds = this.BoundsAbsolute;
            float h = bounds.Height;
            if (h < 2f)
            {
                var colorInfo = Theme.GetBackInfo(this);
                if (colorInfo != null && colorInfo.Color.HasValue)
                {
                    var y = bounds.Y + h / 2f;
                    var pen = args.GetPen(colorInfo.Color.Value, h);
                    args.PrepareGraphicsFor(GraphicsTargetType.Splines);
                    args.Graphics.DrawLine(pen, bounds.X, y, bounds.Right, y);
                }
            }
            else
            {
                PaintItemOther(args);
            }
        }
        protected virtual void PaintItemVerticalLine(LayeredPaintEventArgs args)
        {
            if (args.Layer != LayerType.Background) return;

            var bounds = this.BoundsAbsolute;
            float w = bounds.Width;
            if (w < 2f)
            {
                var colorInfo = Theme.GetBackInfo(this);
                if (colorInfo != null && colorInfo.Color.HasValue)
                {
                    var x = bounds.X + w / 2f;
                    var pen = args.GetPen(colorInfo.Color.Value, w);
                    args.PrepareGraphicsFor(GraphicsTargetType.Splines);
                    args.Graphics.DrawLine(pen, x, bounds.Y, x, bounds.Bottom);
                }
            }
            else
            {
                PaintItemOther(args);
            }
        }
        protected virtual void PaintItemGroup(LayeredPaintEventArgs args)
        {
            if (args.Layer != LayerType.Background) return;

            var theme = Theme;
            var colorInfo = theme.GetBackInfo(this);
            this.PaintBackground(args, colorInfo);
        }
        protected virtual void PaintItemCell(LayeredPaintEventArgs args)
        {
            switch (args.Layer)
            {
                case LayerType.Standard:
                    PaintItemCellStandard(args);
                    break;
                case LayerType.Overlay:
                    PaintItemCellOverlay(args);
                    break;
            }
        }
        protected virtual void PaintItemCellStandard(LayeredPaintEventArgs args)
        {
            var theme = Theme;
            var colorInfo = theme.GetBackInfo(this);
            PaintBackground(args, colorInfo);
            PaintItemCellContent(args);
        }
        protected virtual void PaintItemCellContent(LayeredPaintEventArgs args)
        {
            var cell = Cell;
            if (cell.Value > 0)
            {
                var bounds = this.BoundsAbsolute.ShiftBy(0f, 1f);
                var image = GetImageForCell(cell);
                PaintIcon(args, image, ContentAlignment.MiddleCenter, null, null, new PointF(0f, 1f));

                string text = cell.Value.ToString();
                var textInfo = Theme.GetTextInfo(this);
                DrawString(args, text, this.Coordinates.ButtonFont, textInfo?.TextColor, bounds, ContentAlignment.MiddleCenter, new PointF(1f, 0f));
            }
        }
        protected virtual void PaintItemCellOverlay(LayeredPaintEventArgs args)
        {
            _PaintOverlayAnimation(args);
        }
        protected virtual void PaintItemSubCell(LayeredPaintEventArgs args)
        { }
        protected virtual void PaintItemButton(LayeredPaintEventArgs args)
        {
            var theme = Theme;
            var colorInfo = theme.GetBackInfo(this);
            var bounds = this.BoundsAbsolute;

            bounds = bounds.ZoomCenter(0.9f);
            if (false)
                this.PaintBackground(args, colorInfo, bounds);
            
            var image = GetImageForButton();
            PaintIcon(args, image, ContentAlignment.MiddleCenter, null, 0.95f);

            var text = this.ButtonText;
            if (!String.IsNullOrEmpty(text))
            {
                var textInfo = theme.GetTextInfo(this);
                DrawString(args, text, this.Coordinates.CellFixedFont, textInfo?.TextColor, bounds, ContentAlignment.MiddleCenter, new PointF(1f, 0f));
            }
        }
        protected virtual void PaintItemOther(LayeredPaintEventArgs args)
        {
            var theme = Theme;
            var colorInfo = theme.GetBackInfo(this);
            this.PaintBackground(args, colorInfo);
        }
        protected Image GetImageForCell(Cell cell)
        {
            int value = cell.Value;
            if (value >= 1 && value <= 9 && IsVisibleValueImage) return Theme.ValueImages[value - 1];
            return null;
        }
        protected Image GetImageForButton()
        {
            var buttonType = this.ButtonType;
            if (buttonType == SudokuButtonType.Value && this.ItemSubValue.HasValue) return Theme.ValueImages[this.ItemSubValue.Value - 1];
            if (Theme.ButtonImages.TryGetValue(buttonType, out var image)) return image;
            return null;
        }
        /// <summary>
        /// Vykreslí pozadí: Image nebo Color, do daného prostoru nebo do <see cref="BoundsAbsolute"/>.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="colorInfo"></param>
        /// <param name="explicitBounds"></param>
        protected void PaintBackground(LayeredPaintEventArgs args, SudokuSkinTheme.BackProperties colorInfo, RectangleF? explicitBounds = null)
        {
            if (colorInfo != null)
            {
                RectangleF bounds = explicitBounds ?? this.BoundsAbsolute;
                if (colorInfo.Image != null)
                {
                    args.PrepareGraphicsFor(GraphicsTargetType.Images);
                    args.Graphics.DrawImage(colorInfo.Image, bounds);
                }
                else if (colorInfo.Color.HasValue && colorInfo.Color.Value.A > 0)
                {
                    args.PrepareGraphicsFor(GraphicsTargetType.Rectangles);
                    args.Graphics.FillRectangle(args.GetBrush(colorInfo.Color.Value), bounds);
                }
            }
        }
        /// <summary>
        /// Vykreslí danou ikonu do daného prostoru
        /// </summary>
        /// <param name="args"></param>
        /// <param name="image"></param>
        /// <param name="alignment"></param>
        /// <param name="explicitBounds"></param>
        /// <param name="sizeRatio"></param>
        protected void PaintIcon(LayeredPaintEventArgs args, Image image, ContentAlignment alignment, RectangleF? explicitBounds = null, float? sizeRatio = null, PointF? offset = null)
        {
            if (image is null) return;
            SizeF imageSize = image.Size;
            RectangleF bounds = explicitBounds ?? this.BoundsAbsolute;
            if (sizeRatio.HasValue) bounds = bounds.ZoomCenter(sizeRatio.Value);
            RectangleF imageBounds = imageSize.AlignTo(bounds, alignment, ShrinkSizeMode.ShrinkWithPreserveRatio);
            if (offset.HasValue) imageBounds = imageBounds.ShiftBy(offset.Value);
            args.PrepareGraphicsFor(GraphicsTargetType.Images);
            args.Graphics.DrawImage(image, imageBounds);
        }
        /// <summary>
        /// Vykreslí text do daného prostoru s daným zarovnáním.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="color"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        protected virtual void DrawString(LayeredPaintEventArgs args, string text, Font font, Color? color, RectangleF bounds, ContentAlignment alignment, PointF? offset = null)
        {
            if (font is null || String.IsNullOrEmpty(text) || !color.HasValue) return;
            args.PrepareGraphicsFor(GraphicsTargetType.Text);
            var textSize = args.Graphics.MeasureString(text, font);
            var textBounds = textSize.AlignTo(bounds, alignment);
            if (offset.HasValue) textBounds = textBounds.ShiftBy(offset.Value);
            args.Graphics.DrawString(text, font, args.GetBrush(color.Value), textBounds.Location); //
        }


        #endregion
    }
    #endregion
    #region enumy SudokuItemType, SudokuButtonType, InteractiveMouseState
    /// <summary>
    /// Typ prvku
    /// </summary>
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
    /// <summary>
    /// Typyp buttonů
    /// </summary>
    public enum SudokuButtonType
    {
        /// <summary>
        /// Není button
        /// </summary>
        None,
        /// <summary>
        /// Button pro konkrétní hodnotu, která je v <see cref="SudokuItem.ItemSubValue"/>
        /// </summary>
        Value,
        /// <summary>
        /// Reset hry = zpátky na fixní hodnoty
        /// </summary>
        ResetGame,
        /// <summary>
        /// Nová hra, rozsvítí 3 možnosti + Back
        /// </summary>
        NewGame,
        /// <summary>
        /// Dej mi tip
        /// </summary>
        Hint,
        /// <summary>
        /// Nová hra lehká
        /// </summary>
        NewGameLight,
        /// <summary>
        /// Nová hra střední
        /// </summary>
        NewGameMedium,
        /// <summary>
        /// Nová hra těžká
        /// </summary>
        NewGameHard,
        /// <summary>
        /// Zpátky
        /// </summary>
        Back
    }
    /// <summary>
    /// Stav interaktivity
    /// </summary>
    public enum InteractiveMouseState
    {
        None,
        MouseEnter,
        MouseEnterAnimating,
        MouseOn,
        MouseLeftDown,
        MouseRightDown,
        MouseLeave,
        MouseLeaveAnimating,
        MouseOff

    }
    #endregion
}
