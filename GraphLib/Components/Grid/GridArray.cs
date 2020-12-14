using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Components.Grid;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// GridArray : objekt, který v sobě obsahuje pole Gridů (v počtu 0 až +nnn) a k tomuto poli odpovídající TabHeader, kde každá jedna záložka reprezentuje jeden Grid.
    /// </summary>
    public class GridArray : InteractiveContainer, IInteractiveItem
    {
        #region Konstrukce, inicializace, proměnné
        /// <summary>
        /// Konstruktor s parentem
        /// </summary>
        /// <param name="parent"></param>
        public GridArray(IInteractiveParent parent) : this() { this.Parent = parent; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GridArray()
        {
            this._Initialise();
        }

        private void _Initialise()
        {
            this._TabHeaderHeight = Application.App.Zoom.ZoomDistance(Skin.TabHeader.HeaderHeight);
            this._TabHeader = new GTabHeader(this) { Position = RectangleSide.Top };

            this._GridList = new List<GGrid>();
        }
        private int _TabHeaderHeight;
        private GTabHeader _TabHeader;
        private List<GGrid> _GridList;
        private int GridCount { get { return this._GridList.Count; } }

        #endregion
        #region Přidávání tabulky / Gridu
        /// <summary>
        /// Přidá tabulku
        /// </summary>
        /// <param name="table"></param>
        public void AddTable(Table table)
        {
            GGrid grid = new GGrid(this);
            grid.AddTable(table);
            this._AddGrid(grid);
        }
        /// <summary>
        /// Přidá grid
        /// </summary>
        /// <param name="grid"></param>
        public void AddGrid(GGrid grid)
        {
            this._AddGrid(grid);
        }
        private void _AddGrid(GGrid grid)
        {
            if (grid == null) return;

            Table table = grid.DataTables.FirstOrDefault();
            if (table == null) return;

            ((IInteractiveItem)grid).Parent = this;
            this._GridList.Add(grid);

            this._TabHeader.AddHeader(table.Title, table.Image, linkItem: grid);
        }
        #endregion
        #region Přepínání TabHeader, aktivní Grid
        /// <summary>
        /// Zobrazit tabulku i pro jeden Grid
        /// </summary>
        public bool ShowHeaderForOnlyTable;
        #endregion
        #region Draw, Interactivity, Childs
        /// <summary>
        /// Potomstvo
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { return this.GetChilds(); } }
        /// <summary>
        /// Vrátí potomky
        /// </summary>
        /// <returns></returns>
        protected IInteractiveItem[] GetChilds()
        {
            if (this._ChildItems == null)
            {

            }
            return this._ChildItems;
        }
        /// <summary>
        /// Invaliduje potomky
        /// </summary>
        protected void InvalidateChilds()
        {
            this._ChildItems = null;
        }
        /// <summary>
        /// Potomci
        /// </summary>
        protected IInteractiveItem[] _ChildItems;
        #endregion
    }
}
