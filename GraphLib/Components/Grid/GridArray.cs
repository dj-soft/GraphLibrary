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
            this._TabHeader = new TabHeader(this) { Position = RectangleSide.Top };

            this._GridList = new List<GGrid>();
        }
        private int _TabHeaderHeight;
        private TabHeader _TabHeader;
        private List<GGrid> _GridList;
        private int GridCount { get { return this._GridList.Count; } }

        #endregion
        #region Přidávání tabulky / Gridu
        public void AddTable(Table table)
        {
            GGrid grid = new GGrid(this);
            grid.AddTable(table);
            this._AddGrid(grid);
        }
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

        public bool ShowHeaderForOnlyTable;

        #endregion
        #region Draw, Interactivity, Childs

        protected override IEnumerable<IInteractiveItem> Childs { get { return this.GetChilds(); } }

        protected IInteractiveItem[] GetChilds()
        {
            if (this._ChildItems == null)
            {

            }
            return this._ChildItems;
        }
        protected void InvalidateChilds()
        {
            this._ChildItems = null;
        }
        protected IInteractiveItem[] _ChildItems;
        #endregion
    }
    /// <summary>
    /// Režim zobrazování záložek <see cref="TabHeader"/> při zobrazování pole prvků
    /// </summary>
    [Flags]
    public enum ShowTabHeaderMode
    {
        /// <summary>
        /// Zobrazovat TabHeader jen tehdy, když pole prvků obsahuje dvě nebo více položek (pak má výběr zobrazené položky logický význam)
        /// </summary>
        Default = 0,
        /// <summary>
        /// Zobrazovat TabHeader vždy, tedy i když pole prvků obsahuje jen jeden prvek (pak výběr zobrazené položky nemá logický význam, 
        /// ale <see cref="TabHeader"/> pak hraje roli titulku = zobrazuje totiž Image a Text)
        /// </summary>
        Always = 1,
        /// <summary>
        /// Nikdy nezobrazovat TabHeader, ani když je v poli přítomno více položek (přepínání položek se řeší kódem)
        /// </summary>
        NoTabHeader = 2,
        /// <summary>
        /// Přidat TabHeader, který "zhasne" = nezobrazí se žádný prvek pole, zůstane jen TabHeader
        /// </summary>
        AddEmptyHeader
    }
}
