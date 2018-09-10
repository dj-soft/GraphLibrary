using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using Asol.Tools.WorkScheduler.Application;
using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Manufacturing.WorkScheduler;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class LayoutEngine, LayoutEngineArgs, LayoutEngineResultRow
    /// <summary>
    /// Engine for placing any items in specified area
    /// </summary>
    public class LayoutEngine : IDisposable
    {
        /// <summary>
        /// Assign Bounds into items by arguments and item.Hint.
        /// Return number of processed items.
        /// </summary>
        /// <param name="itemList"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static int CreateLayout(List<ILayoutItem> itemList, LayoutEngineArgs args)
        {
            int result = 0;
            using (LayoutEngine engine = new LayoutEngine(itemList, args))
            {
                result = engine._ProcessLayout();
            }
            return result;
        }
        #region Private process
        private LayoutEngine(List<ILayoutItem> itemList, LayoutEngineArgs args)
        {
            this._ItemList = itemList;
            this._Args = args;
            this._ProcessIndex = args.ProcessStartIndex;
            this._ProcessedItemCount = 0;
            this._ProcessedItemWidth = 0;
            this._ResultRowList = new List<LayoutEngineResultRow>();
        }
        private int _ProcessLayout()
        {
            while (this._HasNextItem)
            {
                ILayoutItem item = this._CurrentItem;
                if (!this._CanAddItem(item))
                {
                    if (!this._CanAddRow(item))
                        break;
                    this._AddNewRow();
                }

                this._AddItem(item);

                if (_ItemHasHint(item, LayoutHint.NextItemSkipToNextTable))
                    break;
                if (_ItemHasHint(item, LayoutHint.NextItemSkipToNextRow))
                    this._FinaliseRow();
            }

            // Write Bounds to all items:
            this._ProcessResult();

            return this._ProcessedItemCount;
        }
        /// <summary>
        /// Process result info
        /// </summary>
        private void _ProcessResult()
        {
            int y = this._Args.ProcessStartLocationY;
            int widthMax = 0;
            foreach (LayoutEngineResultRow row in this._ResultRowList)
            {
                int x = this._Args.ProcessStartLocationX;
                int w = 0;
                foreach (ILayoutItem item in row.Items)
                {
                    Point location = new Point(x, y);
                    Size size = item.ItemSize;
                    item.ItemBounds = new Rectangle(location, size);
                    x += size.Width;
                    w += size.Width;
                }
                if (widthMax < w) widthMax = w;
                y += row.Size.Height;
            }

            this._Args.StoreResults(this._ProcessedItemCount, widthMax, this._ResultRowList);

            // Remove processed items:
            if (this._Args.RemoveProcessedItems)
                this._ItemList.RemoveRange(this._Args.ProcessStartIndex, this._ProcessedItemCount);
        }
        /// <summary>
        /// true when exists another item to process
        /// </summary>
        private bool _HasNextItem { get { return (this._ProcessIndex < this._ItemList.Count); } }
        /// <summary>
        /// Current item to process
        /// </summary>
        private ILayoutItem _CurrentItem { get { return (this._HasNextItem ? this._ItemList[this._ProcessIndex] : null); } }
        /// <summary>
        /// Can add new row with this item?
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool _CanAddRow(ILayoutItem item)
        {
            List<LayoutEngineResultRow> rowInfoList = this._ResultRowList;
            if (rowInfoList.Count == 0) return true;                                               // First row is allways enabled
            if (_ItemHasHint(item, LayoutHint.ThisItemSkipToNextTable)) return false;              // New item must be first in next table
            LayoutEngineResultRow lastRow = this._LastRow;                                         // Last row is not same as Work row (last row can be finalised - and finalised row can not be accessed in WorkRow property)
            ILayoutItem lastItem = lastRow.LastItem;                                               // Last item from Last row
            if (_ItemHasHint(lastItem, LayoutHint.NextItemSkipToNextTable)) return false;          // Last item must be last in current table
            int height = lastRow.Bounds.Bottom + item.ItemSize.Height;                             // Theoretical Height of all rows + new row
            if (height > this._Args.HeightTarget) return false;
            return true;
        }
        /// <summary>
        /// Add new layout row
        /// </summary>
        private void _AddNewRow()
        {
            int rowIndex = this._ResultRowList.Count;
            int x = this._Args.ProcessStartLocationX;
            int y = (rowIndex == 0 ? 0 : this._LastRow.Bounds.Bottom);
            Point location = new Point(x, y);
            LayoutEngineResultRow rowInfo = new LayoutEngineResultRow(rowIndex, location);
            this._ResultRowList.Add(rowInfo);
        }
        /// <summary>
        /// Return true when another item can be added to current WorkRow.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool _CanAddItem(ILayoutItem item)
        {
            LayoutEngineResultRow workRow = this._WorkRow;
            if (workRow == null) return false;
            if (workRow.ItemCount == 0) return true;
            if (item.ItemSize.Height != workRow.Size.Height) return false;
            
            if (_ItemHasHint(item, LayoutHint.ThisItemSkipToNextRow)) return false;
            if (_ItemHasHint(item, LayoutHint.ThisItemSkipToNextTable)) return false;

            ILayoutItem lastItem = workRow.LastItem;
            if (_ItemHasHint(lastItem, LayoutHint.NextItemSkipToNextRow)) return false;
            if (_ItemHasHint(lastItem, LayoutHint.NextItemSkipToNextTable)) return false;

            if (_ItemHasHint(item, LayoutHint.ThisItemOnSameRow)) return true;
            if (_ItemHasHint(lastItem, LayoutHint.NextItemOnSameRow)) return true;

            int width = workRow.Size.Width + item.ItemSize.Width;
            if (width <= this._Args.WidthOptimal) return true;
            return false;
        }
        /// <summary>
        /// Add next item to current WorkRow
        /// </summary>
        /// <param name="item"></param>
        /// <param name="args"></param>
        /// <param name="index"></param>
        private void _AddItem(ILayoutItem item)
        {
            this._WorkRow.AddItem(item, this._ProcessIndex);
            this._ProcessIndex++;
            this._ProcessedItemCount++;
        }
        /// <summary>
        /// Finalise current WorkRow.
        /// </summary>
        private void _FinaliseRow()
        {
            this._WorkRow.IsFinalised = true;
        }
        /// <summary>
        /// return true when item is not null as has specified Hint
        /// </summary>
        /// <param name="item"></param>
        /// <param name="hint"></param>
        /// <returns></returns>
        private bool _ItemHasHint(ILayoutItem item, LayoutHint hint)
        {
            if (item == null) return false;
            return item.Hint.HasFlag(hint);
        }

        /// <summary>
        /// Current processed item index
        /// </summary>
        private int _ProcessIndex;
        /// <summary>
        /// Current processed item count
        /// </summary>
        private int _ProcessedItemCount;
        private int _ProcessedItemWidth;
        /// <summary>
        /// Last added row.
        /// </summary>
        private LayoutEngineResultRow _LastRow { get { return this._ResultRowList.LastOrDefaultInList(); } }
        /// <summary>
        /// true when exists _WorkRow
        /// </summary>
        private bool _HasWorkRow { get { var r = this._LastRow; return (r != null && !r.IsFinalised); } }
        /// <summary>
        /// Current layout row, into which will be new item inserted. If is finalised, returns null.
        /// </summary>
        private LayoutEngineResultRow _WorkRow { get { var r = this._LastRow; return (r == null || r.IsFinalised ? null : r); } }
        /// <summary>
        /// Result array containing processed items in one resulting table  
        /// </summary>
        private List<LayoutEngineResultRow> _ResultRowList;
        /// <summary>
        /// Input item list
        /// </summary>
        private List<ILayoutItem> _ItemList;
        /// <summary>
        /// Input/Output arguments
        /// </summary>
        private LayoutEngineArgs _Args;

        void IDisposable.Dispose()
        { 
            _ItemList = null;
            _Args = null;
        }
        #endregion
    }
    /// <summary>
    /// Arguments for Layout engine, input and result data
    /// </summary>
    public class LayoutEngineArgs
    {
        /// <summary>
        /// Optimal width
        /// </summary>
        public int WidthOptimal { get; set; }
        /// <summary>
        /// Maximal width
        /// </summary>
        public int WidthMaximal { get; set; }
        /// <summary>
        /// Target height
        /// </summary>
        public int HeightTarget { get; set; }
        /// <summary>
        /// Index of first item in array for process 
        /// </summary>
        public int ProcessStartIndex { get; set; }
        /// <summary>
        /// Start X position for layout items
        /// </summary>
        public int ProcessStartLocationX { get; set; }
        /// <summary>
        /// Start X position for layout items
        /// </summary>
        public int ProcessStartLocationY { get; set; }
        /// <summary>
        /// Remove processes items from input list?
        /// </summary>
        public bool RemoveProcessedItems { get; set; }
        /// <summary>
        /// Number of processed items in current process  
        /// </summary>
        public int ResultProcessedItemCount { get; private set; }
        /// <summary>
        /// Max Width from all Rows processed in current run (ResultRows)
        /// </summary>
        public int ResultProcessedItemWidth { get; private set; }
        /// <summary>
        /// Result array containing processed items in one resulting table  
        /// </summary>
        public LayoutEngineResultRow[] ResultRows { get; private set; }
        /// <summary>
        /// Store result of layout processing into this args
        /// </summary>
        /// <param name="processedCount"></param>
        /// <param name="processedWidth"></param>
        /// <param name="processedRows"></param>
        internal void StoreResults(int processedCount, int processedWidth, List<LayoutEngineResultRow> processedRows)
        {
            this.ResultProcessedItemCount = processedCount;
            this.ResultProcessedItemWidth = processedWidth;
            this.ResultRows = processedRows.ToArray();
        }
        /// <summary>
        /// Prepare inner data for next process:
        /// ProcessStartIndex = (ProcessStartIndex + ResultProcessedItemCount);
        /// ProcessStartLocationX = (ProcessStartLocationX + ResultProcessedItemWidth);
        /// ResultProcessedItemCount = 0;
        /// ResultProcessedItemWidth = 0;
        /// ResultRows = null;
        /// </summary>
        public void PrepareNextProcess(bool shiftX, bool shiftY)
        {
            this.ProcessStartIndex += this.ResultProcessedItemCount;
            if (shiftX) this.ProcessStartLocationX += this.ResultProcessedItemWidth;
            if (shiftY) this.ProcessStartLocationY += this.HeightTarget;
            this.ResultProcessedItemCount = 0;
            this.ResultProcessedItemWidth = 0;
            this.ResultRows = null;
        }
    }
    /// <summary>
    /// One row with items in created layout
    /// </summary>
    public class LayoutEngineResultRow
    {
        public LayoutEngineResultRow(int rowIndex, Point location)
        {
            this.RowIndex = rowIndex;
            this.Location = location;
            this.Size = new Size(0, 0);
            this.Items = new List<ILayoutItem>();
            this.IsFinalised = false;
        }
        /// <summary>
        /// Location of begin this row
        /// </summary>
        public Point Location { get; private set; }
        /// <summary>
        /// Size of this row = (Sum(Items[].Size.Width), Max(Items[].Size.Height))
        /// </summary>
        public Size Size { get; private set; }
        /// <summary>
        /// Bounds of this row
        /// </summary>
        public Rectangle Bounds { get { return new Rectangle(this.Location, this.Size); } }
        /// <summary>
        /// Index of this row
        /// </summary>
        public int RowIndex { get; private set; }
        /// <summary>
        /// Items in this row
        /// </summary>
        public List<ILayoutItem> Items { get; private set; }
        /// <summary>
        /// Index of first item in this row
        /// </summary>
        public int FirstItemIndex { get; private set; }
        /// <summary>
        /// true when row is finalised
        /// </summary>
        public bool IsFinalised { get; set; }
        /// <summary>
        /// Number of items
        /// </summary>
        public int ItemCount { get { return this.Items.Count; } }
        /// <summary>
        /// Last item in this row
        /// </summary>
        internal ILayoutItem LastItem { get { return this.Items.LastOrDefaultInList(); } }
        /// <summary>
        /// Add next item to this row
        /// </summary>
        /// <param name="item"></param>
        /// <param name="args"></param>
        /// <param name="index"></param>
        internal void AddItem(ILayoutItem item, int index)
        {
            if (this.ItemCount == 0)
                this.FirstItemIndex = index;
            this.Items.Add(item);
            int iw = item.ItemSize.Width;
            int ih = item.ItemSize.Height;
            int rw = this.Size.Width;
            int rh = this.Size.Height;
            this.Size = new Size(rw + iw, (rh < ih ? ih : rh));
        }
    }
    #endregion
    #region interface ILayoutItem, enum LayoutHint
    /// <summary>
    /// Interface for any item for processing in LayoutManager
    /// </summary>
    public interface ILayoutItem
    {
        /// <summary>
        /// Velikost tohoto prvku (vstupní)
        /// </summary>
        Size ItemSize { get; }
        /// <summary>
        /// Nápověda ke zpracování layoutu této položky
        /// </summary>
        LayoutHint Hint { get; }
        /// <summary>
        /// Explicitně požadovaná šířka prvku v počtu modulů
        /// </summary>
        int? ModuleWidth { get; }
        /// <summary>
        /// Pozice prvku v layoutu (=Location + this.ItemSize) po zpracování layoutu (výstupní)
        /// </summary>
        Rectangle? ItemBounds { get; set; }
    }
    #endregion
    #region Tests
    /// <summary>
    /// Tests for LayoutEngine
    /// </summary>
    public class LayoutTest : ITest
    {
        #region TestLayoutEngine()
        protected void TestLayoutEngine(TestArgs testArgs)
        {
            List<ILayoutItem> items = new List<ILayoutItem>();

            // Content: A,B,C = Rows (Y coordinate);   7,8,9,10,11 ... = Columns (X coordinate)
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "A7" });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "A8" });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "A9" });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "B7" });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "B8" });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "B9" });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "C7" });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "C8" });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "C9" });
            items.Add(new LayoutTestItem() { ItemSize = new Size(2, 2), Content = "AB1011" });
            items.Add(new LayoutTestItem() { ItemSize = new Size(2, 1), Content = "C1011", Hint = LayoutHint.NextItemSkipToNextRow });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "A12" });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 2), Content = "BC12" });
            items.Add(new LayoutTestItem() { ItemSize = new Size(3, 3), Content = "ABC131415" });

            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "A16", Hint = LayoutHint.ThisItemOnSameRow });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "A17", Hint = LayoutHint.ThisItemOnSameRow });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "A18", Hint = LayoutHint.ThisItemOnSameRow });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "A19", Hint = LayoutHint.ThisItemOnSameRow });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "A20", Hint = LayoutHint.ThisItemOnSameRow });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "A21", Hint = LayoutHint.ThisItemOnSameRow });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "B16", Hint = LayoutHint.ThisItemSkipToNextRow });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "C16", Hint = LayoutHint.ThisItemSkipToNextRow | LayoutHint.NextItemSkipToNextTable });
            items.Add(new LayoutTestItem() { ItemSize = new Size(1, 1), Content = "A22" });


            LayoutEngineArgs layoutArgs = new LayoutEngineArgs()
            {
                WidthOptimal = 3,
                WidthMaximal = 5,
                ProcessStartIndex = 0,
                ProcessStartLocationX = 7,
                HeightTarget = 3,
                RemoveProcessedItems = false
            };

            while (true)
            {
                layoutArgs.PrepareNextProcess(true, false);
                int processed = LayoutEngine.CreateLayout(items, layoutArgs);
                if (processed == 0) break;
            }

            // Test results:
            Compare(items, "AB1011", 10, 0, 2, 2, testArgs);
            Compare(items, "A12", 12, 0, 1, 1, testArgs);
            Compare(items, "BC12", 12, 1, 1, 2, testArgs);
            Compare(items, "ABC131415", 13, 0, 3, 3, testArgs);
            Compare(items, "A21", 21, 0, 1, 1, testArgs);
            Compare(items, "C16", 16, 2, 1, 1, testArgs);
            Compare(items, "A22", 22, 0, 1, 1, testArgs);

        }

        protected void Compare(List<ILayoutItem> items, string content, int x, int y, int w, int h, TestArgs testArgs)
        {
            LayoutTestItem item = Find(items, content);
            Rectangle expected = new Rectangle(x, y, w, h);
            if (!item.ItemBounds.HasValue)
                testArgs.AddResult(TestResultType.TestError, "ItemBounds for " + content + " is null.");
            else if (item.ItemBounds.Value != expected)
                testArgs.AddResult(TestResultType.TestError, "ItemBounds for " + content + " is wrong, expected: " + expected.ToString() + "; real: " + item.ItemBounds.Value.ToString());
        }
        protected static LayoutTestItem Find(IEnumerable<ILayoutItem> items, string content)
        {
            return items.FirstOrDefault(i => (i as LayoutTestItem).Content == content) as LayoutTestItem;
        }
        protected class LayoutTestItem : ILayoutItem
        {
            public override string ToString()
            {
                return this.Content + "; " + (this.ItemBounds == null ? "null" : this.ItemBounds.Value.ToString());
            }
            public Size ItemSize { get; set; }
            public Rectangle? ItemBounds { get; set; }
            public LayoutHint Hint { get; set; }
            public int? ModuleWidth { get; set; }
            public string Content { get; set; }
        }
        #endregion
        #region ITest interface members
        PluginActivity IPlugin.Activity { get { return PluginActivity.Standard; } }
        TestType ITest.TestType { get { return TestType.Essential | TestType.AtStartup; } }
        void ITest.RunTest(TestArgs testArgs) { this.TestLayoutEngine(testArgs); }
        #endregion
    }

    #endregion
}
