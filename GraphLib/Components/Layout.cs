using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using Asol.Tools.WorkScheduler.Application;
using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Base.WorkScheduler;

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
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="location"></param>
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
    #region class SequenceLayout : řízení pozice vizuálního prvku ve vizuální sekvenci, na základě jeho velikosti ItemSize
    /// <summary>
    /// <see cref="SequenceLayout"/> : řízení pozice vizuálního prvku ve vizuální sekvenci, na základě jeho velikosti ItemSize.
    /// Třída, která v sobě uchovává odkaz na velikost elementu v jednom směru <see cref="ItemSizeInt"/>,
    /// současně dokáže tuto hodnotu akceptovat z datové i vizuální vrstvy, dokáže hlídat minimální hodnotu a zadaný rozsah hodnot, a obsahuje i defaultní hodnotu.
    /// Navíc podporuje interface ISequenceLayout, které se používá pro sekvenční řazené prvků za sebe.
    /// </summary>
    public class SequenceLayout : ISequenceLayout
    {
        #region Konstruktor, proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="itemSize"></param>
        public SequenceLayout(ItemSizeInt itemSize)
        {
            this._ItemSize = itemSize;
            this._Visible = true;
        }
        private ItemSizeInt _ItemSize;
        private Int32? _Size;
        private bool _Visible;
        private int _ISequenceLayoutBegin;
        #endregion
        #region Základní property
        /// <summary>
        /// Velikost. Lze vložit hodnotu null, a hodnota null může být vrácena z property. Pak není velikost tohoto prvku dána explicitně.
        /// Pokud bylo vloženo null, pak příští get čte hodnotu z ParentLayout (i rekurzivně).
        /// </summary>
        public Int32? Size
        {
            get
            {
                int size = this._ItemSize.Size ?? this._Size ?? 0;
                return size;
                //return this._ItemSize.Size;
                //Int32? size = this._ItemSize?.Size;
                //if (!size.HasValue)
                //return (this._Size.HasValue ? this._ItemSize.AlignSize(this._Size.Value) : this._ItemSize.Size.Value);
            }
            set { this._Size = value; if (value.HasValue) this._ItemSize.Size = value; }
        }
        /// <summary>
        /// true pokud prvek je viditelný (dáno kódem, nikoli fitry atd). Default = true.
        /// POZOR: zde je součin viditelnosti z hodnot: <see cref="ItemSize{T}.Visible"/> AND <see cref="SequenceLayout.Visible"/> !
        /// Tzn. pokud je setováno true, pak get může vrátit false = z podkladové datové vrstvy.
        /// </summary>
        public bool Visible { get { return this._Visible && this._ItemSize.Visible; } set { this._Visible = value; } }
        /// <summary>
        /// true pokud tento prvek má být použit jako "guma" při změně rozměru hostitelského prvku tak, aby kolekce prvků obsadila celý prostor.
        /// Na true se nastavuje typicky u "hlavního" sloupce grafové tabulky.
        /// </summary>
        public bool AutoSize { get { return this._ItemSize.AutoSize.Value; } set { this._ItemSize.AutoSize = value; } }
        /// <summary>
        /// true pokud uživatel může změnit velikost tohoto prvku. Default = true
        /// </summary>
        public bool ResizeEnabled { get { return this._ItemSize.ResizeEnabled.Value; } set { this._ItemSize.ResizeEnabled = value; } }
        #endregion
        #region Implementace ISequenceLayout = pořadí, počáteční pixel, velikost, následující pixel. Podpůrné metody GetLayoutSize() a SetLayoutSize().
        /// <summary>
        /// Pořadí tohoto prvku v kolekci; -1 pro prvky neviditelné
        /// </summary>
        int ISequenceLayout.Order { get { return this._ItemSize.Order; } set { this._ItemSize.Order = value; } }
        /// <summary>
        /// Pozice, kde prvek začíná.
        /// Interface ISequenceLayout tuto hodnotu setuje v případě, kdy se layout těchto prvků změní (změna prvků nebo jejich velikosti).
        /// </summary>
        int ISequenceLayout.Begin { get { return _ISequenceLayoutBegin; } set { _ISequenceLayoutBegin = value; } }
        /// <summary>
        /// Velikost prvku v pixelech (šířka sloupce, výška řádku, výška tabulky). 
        /// Lze ji setovat, protože prvky lze pomocí splitterů zvětšovat / zmenšovat.
        /// Pokud ale prvek nemá povoleno Resize (ResizeEnabled je false), pak setování hodnoty je ignorováno.
        /// Aplikační logika prvku musí zabránit vložení neplatné hodnoty (reálně se uloží hodnota platná).
        /// </summary>
        int ISequenceLayout.Size { get { return this.Size.Value; } set { if (this.ResizeEnabled) this.Size = value; } }
        /// <summary>
        /// Pozice, kde za tímto prvkem začíná následující prvek. 
        /// Velikost prvku = (End - Begin) = počet pixelů, na které se zobrazuje tento prvek.
        /// Interface ISequenceLayout tuto hodnotu nesetuje, pouze ji čte.
        /// </summary>
        int ISequenceLayout.End { get { return this._ISequenceLayoutBegin + (this.Visible ? this.Size.Value : 0); } }
        /// <summary>
        /// true, pokud velikost tohoto objektu (<see cref="Size"/>) bude změněna při změně velikosti celého prvku.
        /// Typicky: sloupec tabulky s nastavením (<see cref="AutoSize"/> == true) bude rozšířen nebo zúžen při změně šířky tabulky.
        /// Při této změně bude vložena nová Size, přitom tento objekt může aplikovat svoje restrikce (Min a Max).
        /// </summary>
        bool ISequenceLayout.AutoSize { get { return this.AutoSize; } }
        #endregion
        #region Public static podpora pro instance ISequenceLayout (Nápočet Begin, AutoSize, FilterVisible)
        #region Nápočet pozice ISequenceLayout.Begin pro všechny prvky v dané kolekci
        /// <summary>
        /// Do všech položek ISequenceLayout dodané kolekce vepíše hodnotu Begin postupně od 0.
        /// Lze zadat mezeru mezi prvky = vzdálenost Begin prvku [N+1] od End prvku [N].
        /// Vrací hodnotu End posledního viditelného prvku (toho, který má Size větší enž 0).
        /// </summary>
        /// <param name="items">Kolekce položek typu ISequenceLayout, jejich Begin a End se bude nastavovat</param>
        /// <param name="spacing">Mezera mezi prvky = hodnota, o kterou bude Begin následující položky navýšen proti End položky předešlé.</param>
        public static int SequenceLayoutCalculate(IEnumerable<ISequenceLayout> items, int spacing = 0)
        {
            int order = 0;
            int begin = 0;
            return SequenceLayoutCalculate(items, ref order, ref begin, spacing);
        }
        /// <summary>
        /// Do všech položek ISequenceLayout dodané kolekce vepíše hodnotu Begin postupně od hodnoty begin.
        /// Lze zadat mezeru mezi prvky = vzdálenost Begin prvku [N+1] od End prvku [N].
        /// Vrací hodnotu End posledního viditelného prvku (toho, který má Size větší enž 0).
        /// Parametr ref begin po skončení metody obsahuje hodnotu, kde by začínal další prvek za posledním prvkem této kolekce (akceptujíc spacing).
        /// </summary>
        /// <param name="items">Kolekce položek typu ISequenceLayout, jejich Begin a End se bude nastavovat</param>
        /// <param name="order">Hodnota Order do první položky, která bude mít Size kladné</param>
        /// <param name="begin">Hodnota Begin do první položky</param>
        /// <param name="spacing">Mezera mezi prvky = hodnota, o kterou bude Begin následující položky navýšen proti End položky předešlé.</param>
        public static int SequenceLayoutCalculate(IEnumerable<ISequenceLayout> items, ref int order, ref int begin, int spacing)
        {
            int end = begin;
            foreach (ISequenceLayout item in items)
                _SequenceLayoutCalculate(item, ref order, ref begin, ref end, spacing);
            return end;
        }
        /// <summary>
        /// Do položky ISequenceLayout vepíše Begin = begin.
        /// Pokud item.Size je kladné, pak:
        /// K hodnotě begin přičte item.Size, to vloží do ref parametru end, 
        /// a do ref parametru begin vloží end (plus spacing, pokud je hodnota spacing větší než nula).
        /// Pokud Size není kladné, pak begin bude nezměněno, a end bude rovněž nezměněno, bez ohledu na spacing. Jde o neviditelný prvek.
        /// Význam: begin určuje počátek tohoto prvku, a následně i prvku následujícího. Akceptuje spacing, pokud to má smysl.
        /// Hodnota end obsahuje konec posledního viditelného prvku, nenavýšený o spacing, přeskakuje neviditelné prvky.
        /// end obsahuje vždy konec 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="order">Hodnota Order do první položky, která bude mít Size kladné</param>
        /// <param name="begin">Hodnota Begin do první položky</param>
        /// <param name="end"></param>
        /// <param name="spacing"></param>
        private static void _SequenceLayoutCalculate(ISequenceLayout item, ref int order, ref int begin, ref int end, int spacing)
        {
            item.Order = -1;
            item.Begin = begin;
            int size = item.Size;
            if (size > 0)
            {
                item.Order = order++;
                end = begin + size;
                begin = ((spacing > 0) ? end + spacing : end);
            }
        }
        #endregion
        #region ISequenceLayout podpora - výpočty AutoSize pro prvky kolekce ISequenceLayout
        /// <summary>
        /// Metoda najde explicitně zadané prvky s <see cref="ISequenceLayout.AutoSize"/> == true, anebo najde první nebo poslední (podle parametru implicitAutoSize),
        /// a upraví jejich velikost (<see cref="ISequenceLayout.Size"/>) tak, aby součet velikostí prvků kolekce měl 
        /// včetně započtení mezery mezi prvky (podle parametru spacing) hodnotu dle parametru visualSize.
        /// Pokud dojde k reálné změně velikosti některého prvku, provede se pro kolekci prvků metoda <see cref="SequenceLayout.SequenceLayoutCalculate(IEnumerable{ISequenceLayout}, int)"/> a vrací se true.
        /// </summary>
        /// <param name="items">Prvky v kolekci</param>
        /// <param name="visualSize">Cílová sumární velikost</param>
        /// <param name="spacing">Mezera mezi prvky</param>
        /// <param name="implicitAutoSize">Režim <see cref="ImplicitAutoSizeType"/>, uplatněný pokud se mezi prvky kolekce nenajde žádný s (<see cref="ISequenceLayout.AutoSize"/> == true)</param>
        /// <param name="fixedItem">Prvek, který by se měl resizovat až když nebude jiná možnost (například proto, že tento prvek právě resizuje uživatel interaktivně)</param>
        /// <param name="variableItem">Prvek, který by se měl resizovat jako první</param>
        /// <returns></returns>
        public static bool AutoSizeLayoutCalculate(IEnumerable<ISequenceLayout> items, int visualSize, int spacing = 0, ImplicitAutoSizeType implicitAutoSize = ImplicitAutoSizeType.None, 
            ISequenceLayout fixedItem = null, ISequenceLayout variableItem = null)
        {
            return _AutoSizeLayoutCalculate(items, visualSize, spacing, implicitAutoSize, fixedItem, variableItem);
        }
        /// <summary>
        /// Metoda najde explicitně zadané prvky s <see cref="ISequenceLayout.AutoSize"/> == true, anebo najde první nebo poslední (podle parametru implicitAutoSize),
        /// a upraví jejich velikost (<see cref="ISequenceLayout.Size"/>) tak, aby součet velikostí prvků kolekce měl 
        /// včetně započtení mezery mezi prvky (podle parametru spacing) hodnotu dle parametru visualSize.
        /// Pokud dojde k reálné změně velikosti některého prvku, provede se pro kolekci prvků metoda <see cref="SequenceLayout.SequenceLayoutCalculate(IEnumerable{ISequenceLayout}, int)"/> a vrací se true.
        /// </summary>
        /// <param name="items">Prvky v kolekci</param>
        /// <param name="visualSize">Cílová sumární velikost</param>
        /// <param name="spacing">Mezera mezi prvky</param>
        /// <param name="implicitAutoSize">Režim <see cref="ImplicitAutoSizeType"/>, uplatněný pokud se mezi prvky kolekce nenajde žádný s (<see cref="ISequenceLayout.AutoSize"/> == true)</param>
        /// <param name="fixedItem">Prvek, který by se měl resizovat až když nebude jiná možnost (například proto, že tento prvek právě resizuje uživatel interaktivně)</param>
        /// <param name="variableItem">Prvek, který by se měl resizovat jako první</param>
        /// <returns></returns>
        private static bool _AutoSizeLayoutCalculate(IEnumerable<ISequenceLayout> items, int visualSize, int spacing, ImplicitAutoSizeType implicitAutoSize, 
            ISequenceLayout fixedItem, ISequenceLayout variableItem = null)
        {
            if (items == null) return false;

            ISequenceLayout[] array = items.ToArray();
            int count = array.Length;
            if (count == 0) return false;

            // Explicitně dodané prvky:
            ISequenceLayout currentFixedItem = null;
            bool hasFixedItem = false;
            bool isFixedItemInVarList = false;
            ISequenceLayout currentVariableItem = null;
            bool hasVariableItem = false;

            // Nejprve projdu všechny vstupní prvky, oddělím prvky s AutoSize = true, sečtu jejich Size zvlášť za Fixní a zvlášť za Variabilní:
            List<ISequenceLayout> variableItems = new List<ISequenceLayout>();
            int fixedSize = 0;
            int currentSize = 0;
            int spaceSizeOne = (spacing < 0 ? 0 : spacing);
            int spaceSize = 0;
            foreach (ISequenceLayout item in items)
            {
                bool isFixedItem = (!hasFixedItem && fixedItem != null && currentFixedItem == null && Object.ReferenceEquals(item, fixedItem));
                if (isFixedItem) { currentFixedItem = item; hasFixedItem = true; }

                bool isVariableItem = (!hasVariableItem && variableItem != null && currentVariableItem == null && Object.ReferenceEquals(item, variableItem));
                if (isVariableItem) { currentVariableItem = item; hasVariableItem = true; }

                if (item.AutoSize || isVariableItem)
                {   // a) Variable prvek (z paranmetru) se bere jako AutoSize i kdyby to na sobě neměl uvedené:
                    // b) Fixed prvek (z paranmetru) dáme do pole (itemsVarList) podle jeho AutoSize, ale jeho velikost budeme měnit "až jako poslední":
                    currentSize += item.Size;
                    if (isFixedItem) isFixedItemInVarList = true;              // To abych prvky v (itemsVarList) nejprve resizoval bez prvku fixedItem
                    variableItems.Add(item);
                }
                else
                {
                    fixedSize += item.Size;
                }
                spaceSize += spaceSizeOne;
            }
            if (spaceSize > 0) spaceSize -= spaceSizeOne;                      // Odečtu spacing, který byl přičtený za posledním prvkem pole, protože tam se space již nezapočítává!

            // Žádný prvek není AutoSize => ke slovu přichází řešení pomocí ImplicitAutoSizeType:
            if (variableItems.Count == 0)
            {   // Pole (array) má vždy přinejmenším jeden prvek, podmínka je na začátku metody...
                ISequenceLayout implicitItem = null;
                switch (implicitAutoSize)
                {
                    case ImplicitAutoSizeType.FirstItem:
                        implicitItem = array[0];
                        break;
                    case ImplicitAutoSizeType.LastItem:
                        implicitItem = array[count - 1];
                        break;
                    default:
                        return false;                                          // Není zde žádný variabilní prvek
                }

                // Jako AutoSize vezmeme prvek implicitItem:
                currentSize += implicitItem.Size;
                fixedSize -= implicitItem.Size;
                variableItems.Add(implicitItem);
            }

            // Přepočet Size u prvků AutoSize:
            bool isChanged = false;
            int targetSize = visualSize - (fixedSize + spaceSize);             // Nová sumární šířka variabilních prvků = daný prostor mínus (pevné prvky + mezery)
            if (targetSize == currentSize) return false;

            // 1. Pokud máme zadán Variable prvek, pak prioritně upravím jeho velikost:
            if (hasVariableItem)
                _AutoSizeLayoutCalculateAbsolute(currentVariableItem, ref currentSize, targetSize, ref isChanged);

            // 2. máme nějaký rozdíl mezi tím, co má být a co je, provedem přepočet variabilních prvků pomocí Ratio:
            if (targetSize != currentSize)
            {
                if (isFixedItemInVarList)
                {
                    // 2a. Pokud je přítomen Fixní prvek, a ten je uložen v itemsVarList, pak resizuji pouze proměnné prvky (v itemsVarList) bez fixního prvku:
                    var varItems = variableItems.Where(i => !Object.ReferenceEquals(i, currentFixedItem)).ToList();
                    _AutoSizeLayoutCalculateRatio(varItems, ref currentSize, targetSize, ref isChanged);
                }
                else
                {
                    // 2b. Pokud není přítomen Fixní prvek, pak pomocí Ratio resizuji všechny proměnné prvky:
                    _AutoSizeLayoutCalculateRatio(variableItems, ref currentSize, targetSize, ref isChanged);
                }
            }

            // 3. Pokud dosud není srovnáno, pak rozdíl promítnu do jakéhokoli ochotného prvku, počínaje prvním, včetně fixního:
            if (targetSize != currentSize)
                _AutoSizeLayoutCalculateAbsolute(variableItems, ref currentSize, targetSize, ref isChanged);

            // 4. Pokud stále není srovnáno, pak mohu zkusit rozdíl promítnout do FixedItem (pokud je) = protože to je prvek, který za nesrovnalost může (byl resizován na novou ale nevyhovující velikost):
            if (targetSize != currentSize && hasFixedItem)
                _AutoSizeLayoutCalculateAbsolute(currentFixedItem, ref currentSize, targetSize, ref isChanged);

            // 5. Pokud stále není srovnáno, pak rozdíl dám do prvního z fixních prvků (fixních = proto, že variabilní to nedokážou akceptovat):
            if (targetSize != currentSize)
                _AutoSizeLayoutCalculateAbsolute(items.Where(i => !i.AutoSize), ref currentSize, targetSize, ref isChanged);

            // 6. Po nějaké změně: Zajistím provedení nápočtu pozic (ISequenceLayout.Begin, End):
            if (isChanged)
                SequenceLayout.SequenceLayoutCalculate(items, spaceSizeOne);

            return isChanged;
        }
        /// <summary>
        /// Zajistí proporcionální úpravu velikosti všech daných prvků tak, aby sumární velikost odpovídala danému cíli.
        /// </summary>
        /// <param name="variableItems"></param>
        /// <param name="currentSize"></param>
        /// <param name="targetSize"></param>
        /// <param name="isChanged"></param>
        private static void _AutoSizeLayoutCalculateRatio(List<ISequenceLayout> variableItems, ref int currentSize, int targetSize, ref bool isChanged)
        {
            decimal ratio = (decimal)targetSize / (decimal)currentSize;
            foreach (ISequenceLayout item in variableItems)
            {
                int sizeOneOld = item.Size;
                int sizeOneNew = (int)(Math.Round(ratio * (decimal)sizeOneOld, 0));
                _AutoSizeLayoutCalculateOne(item, sizeOneNew, ref currentSize, ref isChanged);
            }
        }
        /// <summary>
        /// Pokusí se upravit velikost všech daných prvků o tolik, kolik zbývá z aktuální sumární velikosti <paramref name="currentSize"/> do cílové sumární velikosti <paramref name="targetSize"/>.
        /// Tedy do hodnoty Size v poli prvků daného prvku přičte rozdíl (<paramref name="targetSize"/> - <paramref name="currentSize"/>).
        /// Zpětně pak vyhodnotí reálnou změnu na daném prvku (prvek sám může korigovat svoji Size), a metoda pak upraví ref <paramref name="currentSize"/> o reálně provedenou změnu prvku.
        /// Pokouší se tedy rozdíl umístit do kteréhokoli ochotného prvku v poli, počínaje prvním.
        /// <para/>
        /// Jinými slovy: metoda se pokusí s pomocí daných prvků dosáhnout dané cílové velikosti. 
        /// Výsledky této činnosti promítá do ref parametrů <paramref name="currentSize"/> a <paramref name="isChanged"/>.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="currentSize"></param>
        /// <param name="targetSize"></param>
        /// <param name="isChanged"></param>
        private static void _AutoSizeLayoutCalculateAbsolute(IEnumerable<ISequenceLayout> items, ref int currentSize, int targetSize, ref bool isChanged)
        {
            foreach (var item in items)
            {
                if (currentSize == targetSize) break;                          // Je hotovo?
                _AutoSizeLayoutCalculateAbsolute(item, ref currentSize, targetSize, ref isChanged);
            }
        }
        /// <summary>
        /// Pokusí se upravit velikost jednoho daného prvku o tolik, kolik zbývá z aktuální sumární velikosti <paramref name="currentSize"/> do cílové sumární velikosti <paramref name="targetSize"/>.
        /// Tedy do hodnoty Size daného prvku přičte rozdíl (<paramref name="targetSize"/> - <paramref name="currentSize"/>).
        /// Zpětně pak vyhodnotí reálnou změnu na daném prvku (prvek sám může korigovat svoji Size), a metoda pak upraví ref <paramref name="currentSize"/> o reálně provedenou změnu prvku.
        /// <para/>
        /// Jinými slovy: metoda se pokusí s pomocí daného prvku dosáhnout dané cílové velikosti. 
        /// Výsledky této činnosti promítá do ref parametrů <paramref name="currentSize"/> a <paramref name="isChanged"/>.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="currentSize"></param>
        /// <param name="targetSize"></param>
        /// <param name="isChanged"></param>
        private static void _AutoSizeLayoutCalculateAbsolute(ISequenceLayout item, ref int currentSize, int targetSize, ref bool isChanged)
        {
            if (currentSize == targetSize) return;                             // Zkratka, pokud není co řešit

            int sizeOneOld = item.Size;
            int sizeOneNew = sizeOneOld + (targetSize - currentSize);          // Zkusíme daný prvek celý resizovat tak, aby prvek absorboval celou požadovanou změnu
            _AutoSizeLayoutCalculateOne(item, sizeOneNew, ref currentSize, ref isChanged);
        }
        /// <summary>
        /// Pokusí se upravit velikost jednoho daného prvku tak, aby měl danou velikost.
        /// Zpětně pak vyhodnotí reálnou změnu na daném prvku (prvek sám může korigovat svoji Size), a metoda pak upraví ref <paramref name="currentSize"/> o reálně provedenou změnu prvku.
        /// <para/>
        /// Výsledky této činnosti promítá do ref parametrů <paramref name="currentSize"/> a <paramref name="isChanged"/>.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="targetItemSize"></param>
        /// <param name="currentSize"></param>
        /// <param name="isChanged"></param>
        private static void _AutoSizeLayoutCalculateOne(ISequenceLayout item, int targetItemSize, ref int currentSize, ref bool isChanged)
        {
            int sizeOneOld = item.Size;
            item.Size = targetItemSize;
            int sizeOneNew = item.Size;                                        // Prvek mohl aplikovat svoje Min-Max limity, proto si přečteme jeho korigovanou velikost
            if (sizeOneNew != sizeOneOld)
            {   // Došlo ke změně: upravíme sumární velikost proměnných prvků tak, aby odpovídala aktuální velikosti:
                isChanged = true;
                currentSize = currentSize + (sizeOneNew - sizeOneOld);
            }
        }
        #endregion
        #region ISequenceLayout podpora - filtrování prvků typu ISequenceLayout podle viditelné oblasti
        /// <summary>
        /// Vrátí true, pokud daný prvek (item) se svojí pozicí (Begin, End) bude viditelný v aktuálním datovém prostoru
        /// </summary>
        /// <param name="item">Datová položka</param>
        /// <param name="dataBegin">První viditelný datový pixel</param>
        /// <param name="dataEnd">První datový pixel za viditelnou oblastí</param>
        /// <returns></returns>
        public static bool IsItemVisible(ISequenceLayout item, int dataBegin, int dataEnd)
        {
            return _FilterVisibleItem(item, dataBegin, dataEnd);
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek (item) se svojí pozicí (Begin, End) bude viditelný v aktuálním datovém prostoru
        /// </summary>
        /// <param name="item">Datová položka</param>
        /// <param name="dataRange">Rozsah viditelných dat</param>
        /// <returns></returns>
        public static bool IsItemVisible(ISequenceLayout item, Int32NRange dataRange)
        {
            return _FilterVisibleItem(item, dataRange);
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek (item) se svojí pozicí (Begin, End) bude viditelný v aktuálním datovém prostoru
        /// </summary>
        /// <param name="item">Datová položka</param>
        /// <param name="dataRange">Rozsah viditelných dat</param>
        /// <returns></returns>
        public static bool IsItemVisible(ISequenceLayout item, Int32Range dataRange)
        {
            return _FilterVisibleItem(item, dataRange);
        }
        /// <summary>
        /// Vrátí danou kolekci, zafiltrovanou na pouze viditelné prvky
        /// </summary>
        /// <typeparam name="T">Typ datových položek</typeparam>
        /// <param name="items">Kolekce datových položek</param>
        /// <param name="dataBegin">První viditelný datový pixel</param>
        /// <param name="dataEnd">První datový pixel za viditelnou oblastí</param>
        /// <returns></returns>
        public static IEnumerable<T> FilterVisibleItems<T>(IEnumerable<T> items, int dataBegin, int dataEnd) where T : ISequenceLayout
        {
            return items.Where(i => _FilterVisibleItem(i, dataBegin, dataEnd));
        }
        /// <summary>
        /// Vrátí danou kolekci, zafiltrovanou na pouze viditelné prvky
        /// </summary>
        /// <typeparam name="T">Typ datových položek</typeparam>
        /// <param name="items">Kolekce datových položek</param>
        /// <param name="dataRange">Rozsah viditelných dat</param>
        /// <returns></returns>
        public static IEnumerable<T> FilterVisibleItems<T>(IEnumerable<T> items, Int32NRange dataRange) where T : ISequenceLayout
        {
            return items.Where(i => _FilterVisibleItem(i, dataRange));
        }
        /// <summary>
        /// Vrátí danou kolekci, zafiltrovanou na pouze viditelné prvky
        /// </summary>
        /// <typeparam name="T">Typ datových položek</typeparam>
        /// <param name="items">Kolekce datových položek</param>
        /// <param name="dataRange">Rozsah viditelných dat</param>
        /// <returns></returns>
        public static IEnumerable<T> FilterVisibleItems<T>(IEnumerable<T> items, Int32Range dataRange) where T : ISequenceLayout
        {
            return items.Where(i => _FilterVisibleItem(i, dataRange));
        }
        /// <summary>
        /// Vrátí true, pokud daná položka je alespoň částečně viditelná v daném rozsahu
        /// </summary>
        /// <param name="item">Datová položka</param>
        /// <param name="dataBegin">První viditelný datový pixel</param>
        /// <param name="dataEnd">První datový pixel za viditelnou oblastí</param>
        /// <returns></returns>
        private static bool _FilterVisibleItem(ISequenceLayout item, int dataBegin, int dataEnd)
        {
            return (item.Size > 0
                && item.Begin < dataEnd
                && item.End > dataBegin);
        }
        /// <summary>
        /// Vrátí true, pokud daná položka je alespoň částečně viditelná v daném rozsahu
        /// </summary>
        /// <param name="item">Datová položka</param>
        /// <param name="dataRange">Rozsah viditelných dat</param>
        /// <returns></returns>
        private static bool _FilterVisibleItem(ISequenceLayout item, Int32NRange dataRange)
        {
            return (item.Size > 0
                && (!dataRange.HasEnd || item.Begin < dataRange.End.Value)
                && (!dataRange.HasBegin || item.End > dataRange.Begin.Value));
        }
        /// <summary>
        /// Vrátí true, pokud daná položka je alespoň částečně viditelná v daném rozsahu
        /// </summary>
        /// <param name="item">Datová položka</param>
        /// <param name="dataRange">Rozsah viditelných dat</param>
        /// <returns></returns>
        private static bool _FilterVisibleItem(ISequenceLayout item, Int32Range dataRange)
        {
            return (item.Size > 0
                && item.Begin < dataRange.End
                && item.End > dataRange.Begin);
        }
        #endregion
        #endregion
    }
    /// <summary>
    /// Prvek podporující sekvenční layout (řádek nebo sloupec umístěný v kolekci podobných řádků/sloupců).
    /// Má svůj Begin a End. Pokud Begin = End, pak prvek nebude zobrazován.
    /// </summary>
    public interface ISequenceLayout
    {
        /// <summary>
        /// Pořadí tohoto prvku v kolekci.
        /// První prvek má <see cref="Order"/> = 0, následující 1,2, ...
        /// Pořadí je napočítáváno společně s Begin.
        /// </summary>
        int Order { get; set; }
        /// <summary>
        /// Pozice, kde prvek začíná.
        /// Interface ISequenceLayout tuto hodnotu setuje v případě, kdy se layout těchto prvků změní (změna prvků nebo jejich velikosti).
        /// </summary>
        int Begin { get; set; }
        /// <summary>
        /// Velikost prvku v pixelech (šířka sloupce, výška řádku, výška tabulky). 
        /// Lze ji setovat, protože prvky lze pomocí splitterů zvětšovat / zmenšovat.
        /// Aplikační logika prvku musí zabránit vložení neplatné hodnoty (reálně se uloží hodnota platná).
        /// Setování této hodnoty nesmí vyvolat event SizeChanged, protože by mohlo dojít k zacyklení eventů.
        /// </summary>
        int Size { get; set; }
        /// <summary>
        /// Pozice, kde za tímto prvkem začíná následující prvek. 
        /// Velikost prvku = (End - Begin) = počet pixelů, na které se zobrazuje tento prvek.
        /// Interface ISequenceLayout tuto hodnotu nesetuje, pouze ji čte.
        /// </summary>
        int End { get; }
        /// <summary>
        /// true, pokud velikost tohoto objektu (<see cref="Size"/>) bude změněna při změně velikosti celého prvku.
        /// Typicky: sloupec tabulky s nastavením (<see cref="AutoSize"/> == true) bude rozšířen nebo zúžen při změně šířky tabulky.
        /// Při této změně bude vložena nová Size, přitom tento objekt může aplikovat svoje restrikce (Min a Max).
        /// </summary>
        bool AutoSize { get; }
    }
    /// <summary>
    /// Implicitní určování prvku AutoSize, aplikuje se pokud v kolekci prvků <see cref="ISequenceLayout"/> neexistuje žádný, který by měl <see cref="ISequenceLayout.AutoSize"/> == true.
    /// Pokud je zadáno <see cref="ImplicitAutoSizeType.None"/>, pak kolekce nebude mít chování AutoSize.
    /// </summary>
    public enum ImplicitAutoSizeType
    {
        /// <summary>
        /// Žádný z prvků nebude mít chování AutoSize
        /// </summary>
        None = 0,
        /// <summary>
        /// Pokud žádný z prvků kolekce <see cref="ISequenceLayout"/> nebude mít nastaveno <see cref="ISequenceLayout.AutoSize"/> = true, pak se za takový prvek bude považovat PRVNÍ prvek kolekce.
        /// </summary>
        FirstItem,
        /// <summary>
        /// Pokud žádný z prvků kolekce <see cref="ISequenceLayout"/> nebude mít nastaveno <see cref="ISequenceLayout.AutoSize"/> = true, pak se za takový prvek bude považovat POSLEDNÍ prvek kolekce.
        /// </summary>
        LastItem
    }
    #endregion
    #region Tests
    /// <summary>
    /// Tests for LayoutEngine
    /// </summary>
    public class LayoutTest : ITest
    {
        #region TestLayoutEngine()
        /// <summary>
        /// Test layoutu
        /// </summary>
        /// <param name="testArgs"></param>
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
        /// <summary>
        /// Porovná výsledky s očekáváním
        /// </summary>
        /// <param name="items"></param>
        /// <param name="content"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="testArgs"></param>
        protected void Compare(List<ILayoutItem> items, string content, int x, int y, int w, int h, TestArgs testArgs)
        {
            LayoutTestItem item = Find(items, content);
            Rectangle expected = new Rectangle(x, y, w, h);
            if (!item.ItemBounds.HasValue)
                testArgs.AddResult(TestResultType.TestError, "ItemBounds for " + content + " is null.");
            else if (item.ItemBounds.Value != expected)
                testArgs.AddResult(TestResultType.TestError, "ItemBounds for " + content + " is wrong, expected: " + expected.ToString() + "; real: " + item.ItemBounds.Value.ToString());
        }
        /// <summary>
        /// Najde prvek
        /// </summary>
        /// <param name="items"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        protected static LayoutTestItem Find(IEnumerable<ILayoutItem> items, string content)
        {
            return items.FirstOrDefault(i => (i as LayoutTestItem).Content == content) as LayoutTestItem;
        }
        /// <summary>
        /// Testovací třída pro layout
        /// </summary>
        protected class LayoutTestItem : ILayoutItem
        {
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.Content + "; " + (this.ItemBounds == null ? "null" : this.ItemBounds.Value.ToString());
            }
            /// <summary>
            /// Velikost
            /// </summary>
            public Size ItemSize { get; set; }
            /// <summary>
            /// Souřadnice
            /// </summary>
            public Rectangle? ItemBounds { get; set; }
            /// <summary>
            /// Hint
            /// </summary>
            public LayoutHint Hint { get; set; }
            /// <summary>
            /// Šířka
            /// </summary>
            public int? ModuleWidth { get; set; }
            /// <summary>
            /// Textový obsah
            /// </summary>
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
