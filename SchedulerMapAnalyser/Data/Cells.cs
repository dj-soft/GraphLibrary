using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.SchedulerMap.Analyser
{
    /// <summary>
    /// Obsahuje pole buněk = adresy (souřadnice) pro zobrazované prvky.
    /// Umožňuje uspořádávat prvky do pravoúhlé sítě, kdy Next prvky jsou na souřadnici X+1 proti svému Parentu, a Prev prvky na souřadnici X-1 proti Parentu.
    /// Sousedící prvky jsou na shodné souřadnici X, na těsně sousedícím bloku souřadnic Y.
    /// </summary>
    public class Cells
    {
        public Cells()
        {
            _CellsX = new SortedList<int, SortedList<int, IVisualItem>>();
        }
        private SortedList<int, SortedList<int, IVisualItem>> _CellsX;

        public void Add() { }
    }
}
