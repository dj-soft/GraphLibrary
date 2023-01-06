using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace DjSoft.Games.Sudoku.Components
{
    public class SudokuControl : AnimatedControl
    {
        public SudokuControl()
        {
            this.UseBackgroundLayer = true;
            this.UseStandardLayer = true;

            _InitCoordinates();
            _InitTheme();
            _InitGame();
        }

        #region Hra
        private void _InitGame()
        {

        }

        #endregion
        #region Kreslení
        protected override void DoPaintBackground(LayeredPaintEventArgs args)
        {
            base.DoPaintBackground(args, Theme.BackColor);
        }
        protected override void DoPaintStandard(LayeredPaintEventArgs args)
        {
        }
        #endregion
        #region Vizuální kabát
        private void _InitTheme()
        {
            Theme = SudokuSkinTheme.LightGray;
        }
        public SudokuSkinTheme Theme { get { return __Theme; } set { __Theme = value; _RefreshCoordinates(); } }
        private SudokuSkinTheme __Theme;
        #endregion
        #region Mapa prostoru = kde co je, v závislosti na velikosti controlu
        private void _InitCoordinates()
        {
            __Coordinates = new SudokuCoordinates();
            this.ClientSizeChanged += _ClientSizeChanged;
            _RefreshCoordinates();
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
            if (__Coordinates != null && Theme != null)
                __Coordinates.ResizeTo(this.ClientSize, Theme);
        }
        private SudokuCoordinates __Coordinates;
        #endregion
    }
    /// <summary>
    /// Souřadnice prvků v Sudoku
    /// </summary>
    public class SudokuCoordinates
    {
        public SudokuCoordinates()
        { }
        /// <summary>
        /// Po změně rozměru controlu se upraví souřadnice v <see cref="SudokuCoordinates"/>;
        /// </summary>
        /// <param name="size"></param>
        public void ResizeTo(Size size, SudokuSkinTheme theme)
        { }
    }
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
                scs.BackColor = Color.FromArgb(255, 240, 240, 250);
                scs.InnerLineColor = Color.FromArgb(255, 160, 160, 160);
                scs.InnerLineSize = 1f;
                scs.OuterLineColor = Color.FromArgb(255, 100, 100, 100);
                scs.OuterLineSize = 3f;
                return scs;
            }
        }
        private SudokuSkinTheme() { }
        #endregion
        #region Jednotlivé barvy
        public Color BackColor { get; private set; }
        public Color InnerLineColor { get; private set; }
        public float InnerLineSize { get; private set; }
        public Color OuterLineColor { get; private set; }
        public float OuterLineSize { get; private set; }

        #endregion

    }
    #endregion
}
