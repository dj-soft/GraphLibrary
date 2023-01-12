using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Games.Sudoku.Data
{
    /// <summary>
    /// Celá hra
    /// </summary>
    public class SudokuGame
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public SudokuGame()
        {
            __Cells = new Dictionary<Position, Cell>();
            __Groups = new Dictionary<Position, Group>();
            __Rand = new Random();
            _InitCells();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return Text; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        public string Text 
        { 
            get 
            {
                StringBuilder sb = new StringBuilder();
                var cells = __Cells;
                /*
 ===========  ===========  =========== 
| 1 | 2 | 3 || 4 | 5 | 6 || 7 | 8 | 9 |
| 1 | 2 | 3 || 4 | 5 | 6 || 7 | 8 | 9 |
| 1 | 2 | 3 || 4 | 5 | 6 || 7 | 8 | 9 |
 -----------  -----------  ----------- 
| 1 | 2 | 3 || 4 | 5 | 6 || 7 | 8 | 9 |
| 1 | 2 | 3 || 4 | 5 | 6 || 7 | 8 | 9 |
| 1 | 2 | 3 || 4 | 5 | 6 || 7 | 8 | 9 |
 -----------  -----------  ----------- 
| 1 | 2 | 3 || 4 | 5 | 6 || 7 | 8 | 9 |
| 1 | 2 | 3 || 4 | 5 | 6 || 7 | 8 | 9 |
| 1 | 2 | 3 || 4 | 5 | 6 || 7 | 8 | 9 |
 ===========  ===========  =========== 
                */
                string line0 = " ===========  ===========  =========== ";
                string line1 = " -----------  -----------  ----------- ";
                string column1 = "|";
                string column2 = "||";
                sb.AppendLine(line0);
                for (int row = 0; row < 9; row++)
                {
                    sb.Append(column1);
                    for (int col = 0; col < 9; col++)
                    {
                        sb.Append(this[row, col].TextValue);
                        sb.Append((col == 2 || col == 5) ? column2 : column1);
                    }
                    sb.AppendLine();
                    if (row == 2 || row == 5) sb.AppendLine(line1);
                    else if (row == 8) sb.AppendLine(line0);
                }
                return sb.ToString();
            }
        }
        /// <summary>
        /// Buňky = mapa plochy.
        /// První index = řádek Row 0-8; Druhý index = sloupec Col 0-8;
        /// </summary>
        public Dictionary<Position, Cell> Cells { get { return __Cells; } } private Dictionary<Position, Cell> __Cells;
        /// <summary>
        /// Buňka na dané adrese
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public Cell this[int row, int col] { get { return __Cells[new Position((UInt16)row, (UInt16)col)]; } }
        /// <summary>
        /// Grupy (řádky, sloupce, čtverce).
        /// </summary>
        public Dictionary<Position, Group> Groups { get { return __Groups; } } private Dictionary<Position, Group> __Groups;
        /// <summary>
        /// Random
        /// </summary>
        private Random __Rand;
        /// <summary>
        /// Naplní pole <see cref="__Cells"/>
        /// </summary>
        private void _InitCells()
        {
            for (ushort row = 0; row < 9; row++)
                for (ushort col = 0; col < 9; col++)
                {
                    var cell = new Cell(this, row, col);
                    __Cells.Add(cell.CellPosition, cell);
                }
            _InitGroups();
        }
        /// <summary>
        /// Naplní pole <see cref="__Groups"/>
        /// </summary>
        private void _InitGroups()
        {
        }
        /// <summary>
        /// Vrátí adresu čtvercové grupy obsahující danou buňku
        /// </summary>
        /// <param name="cellPosition"></param>
        /// <returns></returns>
        internal Position GetGroupPosition(Position cellPosition)
        {
            return new Position(((UInt16)(cellPosition.Row / 3)), ((UInt16)(cellPosition.Col / 3)));
        }
        #region Vyhodnocování
        internal bool IsValidCell(Cell cell)
        {
            return true;
        }

        #endregion
        #region Text samples
        /// <summary>
        /// Vytvoří new <see cref="SudokuGame"/>, volitelně pro dodaná data vzorku
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static SudokuGame CreateGame(string sample = null)
        {
            SudokuGame game = new SudokuGame();
            if (sample is null) sample = _GetSample();
            game.Import(sample);
            return game;
        }
        /// <summary>
        /// Obsahuje definiční data aktuální hry, pouze fixní (zadané výchozí) hodnoty
        /// </summary>
        public string DataFixed { get { return _GetData(true); } }
        /// <summary>
        /// Obsahuje definiční data aktuální hry, aktuální (zadané výchozí + uživatelem doplněné) hodnoty
        /// </summary>
        public string DataCurrent { get { return _GetData(false); } }
        /// <summary>
        /// Vrátí definiční data aktuální hry
        /// </summary>
        /// <param name="onlyFixed"></param>
        /// <returns></returns>
        private string _GetData(bool onlyFixed)
        {
            StringBuilder sb = new StringBuilder();

            for (int row = 0; row < 9; row++)
            {
                sb.Append(":");
                for (int col = 0; col < 9; col++)
                {
                    var cell = this[row, col];
                    var state = cell.State;
                    bool isFixed = (state == CellState.Fixed);
                    int value = cell.Value;
                    bool storeValue = (value > 0 && (!onlyFixed || (onlyFixed && isFixed)));
                    string text = (storeValue ? value.ToString().Substring(0, 1) : ".");
                    sb.Append(text);
                }
                sb.Append(":");
            }

            return sb.ToString();
        }
        /// <summary>
        /// Importuje dodaná data.
        /// Data musí obsahovat přesně 9x9 = 81 znaků 0 až 9, povolena je tečka nebo mezera na místě bez hodnoty, ostatní znaky jsou ignorovány.
        /// Znaky jsou vkládány postupně do řádků zleva doprava, a pak další řádek, tak jak se v civilizovaných zemích píše.
        /// Pokud text obsahuje tečky, pak jsou tečky chápány jako prázdné políčko; pokud neobsahuje tečky, pak jsou jako prázdné chápány mezery. Prázdné políčko může být zadáno jako znak 0.
        /// Ostatní znaky jsou ignorovány (písmena, EOL, atd).
        /// Pokud výsledných hodnot není přesně 81, dojde k chybě.
        /// </summary>
        /// <param name="data"></param>
        public void Import(string data)
        {
            if (String.IsNullOrEmpty(data)) throw new ArgumentNullException("Game.Import(): Data musí být zadána.");
            bool containsDot = (data.Contains("."));
            List<int> values = new List<int>();
            foreach (var c in data)
            {
                int value = -1;
                if (Char.IsDigit(c)) value = (int)c - 48;            // '0' = 48d
                else if ((containsDot && c == '.') || (!containsDot && c == ' ')) value = 0;
                if (value >= 0 && value <= 9) values.Add(value);
                if (values.Count > 81) break;
            }
            if (values.Count != 81) throw new ArgumentNullException("Game.Import(): Data musí obsahovat přesně 9x9 hodnot 0 až 9, povolena je tečka nebo mezera na místě bez hodnoty, ostatní znaky jsou ignorovány.");

            int index = 0;
            for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                    this[row, col].SetFixedValue(values[index++]);
        }
        /// <summary>
        /// Vrať sample k testování
        /// </summary>
        /// <returns></returns>
        private static string _GetSample()
        {
            int count = 4;
            int id = DateTime.Now.Millisecond % count;
            switch (id)
            {
                case 00: return ":.6..8...2::2.54....8::....6.37.::1...94.3.::.8.7.32..::9...1..57::.29...7..::..13.65..::3....9..4:";
                case 01: return ":159......::...571...::..2...85.::5..8.2..9::963......::.....516.::6...5.2.8::.48.26...::....4..93:";
                case 02: return ":8.4....2.::....794..::6..94....::..9...7.4::7..19.8..::.8...2..9::.4..7..3.::.3......1::..68.3...:";
                case 03: return ":...5....2::27.9.4...::..3.6.15.::.6..48...::.3....248::4.1.2..9.::3..47....::..6....84::1....65..:";
                    // Až zadáš další samply, nezapomeň nahoře upravit count !!!  Jinak se nové samply nikdy neuplatní.
                case 99: return ":.........::.........::.........::.........::.........::.........::.........::.........::.........:";
            }
            // Defaultní:
            return ":.6..8...2::2.54....8::....6.37.::1...94.3.::.8.7.32..::9...1..57::.29...7..::..13.65..::3....9..4:";
            // https://sudokukingdom.com/
        }
        #endregion
    }
    /// <summary>
    /// Jedna skupina buněk = jedna řada nebo sloupec nebo čtverec
    /// </summary>
    public class Group
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Group()
        {
            __Cells = new Cell[9];
        }
        /// <summary>
        /// Buňky v grupě. Vždy je jich 9.
        /// </summary>
        public Cell[] Cells { get { return __Cells; } } private Cell[] __Cells;
    }
    /// <summary>
    /// Jedna buňka
    /// </summary>
    public class Cell
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public Cell(SudokuGame game, UInt16 row, UInt16 col)
        {
            __Game = game;
            __CellPosition = new Position(row, col);
            __GroupPosition = game.GetGroupPosition(__CellPosition);
            __Value = 0;
        }
        private SudokuGame __Game;
        private Position __CellPosition;
        private Position __GroupPosition;
        /// <summary>
        /// Pozice buňky
        /// </summary>
        public Position CellPosition { get { return __CellPosition; } }
        /// <summary>
        /// Pozice čtvercové grupy
        /// </summary>
        public Position GroupPosition { get { return __GroupPosition; } }
        /// <summary>
        /// Číslo řádku 0-8
        /// </summary>
        public int Row { get { return __CellPosition.Row; } }
        /// <summary>
        /// Číslo sloupce 0-8
        /// </summary>
        public int Col { get { return __CellPosition.Col; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return TextFull; }
        /// <summary>
        /// Vizualizace kompletní: "[Row,Col] = | Value |"
        /// </summary>
        public string TextFull { get { return $"[{Row},{Col}] = | {TextChar} |"; } }
        /// <summary>
        /// Vizualizace hodnoty: " Value "
        /// </summary>
        public string TextValue { get { return $" {TextChar} "; } }
        /// <summary>
        /// Výsledná viditelná hodnota: délka 1 znak (=číslice, mezera, vykřičník ...)
        /// </summary>
        public string TextChar { get { return ((__Value > 0 && __Value <= 9) ? __Value.ToString() : " "); } }
        /// <summary>
        /// Aktuální hodnota buňky, nebo 0 když není jistá.
        /// </summary>
        public int Value 
        { 
            get { return __Value; } 
            set 
            {
                if (__IsFixedValue) throw new InvalidOperationException($"Nelze setovat hodnotu 'Value' do buňky 'Cell[{Row},{Col}]', která je 'IsFixedValue'.");
                if (value < 0 || value > 9) throw new ArgumentException($"Nelze setovat hodnotu 'Value = {value}' do buňky 'Cell[{Row},{Col}]', hodnota není platná.");
                __Value = value;
            }
        } private int __Value;
        /// <summary>
        /// Stav buňky
        /// </summary>
        public CellState State
        {
            get
            {
                if (__IsFixedValue) return CellState.Fixed;
                if (__Value > 0) return (__Game.IsValidCell(this) ? CellState.Filled : CellState.Error);


                return CellState.Empty;
            }
        }
        /// <summary>
        /// Hodnota této buňky je Fixní = daná do startu?
        /// </summary>
        private bool __IsFixedValue;
        /// <summary>
        /// Vloží dodanou hodnotu jako fixní = zadanou
        /// </summary>
        /// <param name="value"></param>
        public void SetFixedValue(int value)
        {
            __Value = value;
            __IsFixedValue = (value > 0);
        }
    }
    public class Position
    {
        public Position(UInt16 row, UInt16 col)
        {
            __Position = ((row & 0x00FF) | ((col & 0x00FF) << 8));
        }
        private readonly Int32 __Position;
        public UInt16 Row { get { return (UInt16)(__Position & 0x00FF); } }
        public UInt16 Col { get { return (UInt16)((__Position & 0xFF00) >> 8); } }
        public override string ToString()
        {
            return $"Row: {Row}; Col: {Col}";
        }
        public override bool Equals(object obj)
        {
            if (obj is Position position) return position.__Position == this.__Position;
            return false;
        }
        public override int GetHashCode()
        {
            return __Position.GetHashCode();
        }
    }
    /// <summary>
    /// Stav buňky Sudoku
    /// </summary>
    public enum CellState
    {
        /// <summary>
        /// Neuvedeno
        /// </summary>
        None,
        /// <summary>
        /// Prázdná
        /// </summary>
        Empty,
        /// <summary>
        /// Vyplněná fixně
        /// </summary>
        Fixed,
        /// <summary>
        /// Obsahuje nějaké tipy
        /// </summary>
        WithTips,
        /// <summary>
        /// Vyplněná uživatelem
        /// </summary>
        Filled,
        /// <summary>
        /// Vyplněná s chybou
        /// </summary>
        Error
    }
}
