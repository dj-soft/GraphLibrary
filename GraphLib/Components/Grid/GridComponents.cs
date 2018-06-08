using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components.Grid
{
    #region Třída GComponent : abstraktní předek pro vizuální třídy zobrazující záhlaví i buňku
    /// <summary>
    /// GComponent : abstraktní předek pro vizuální třídy zobrazující záhlaví i buňku
    /// </summary>
    public abstract class GComponent : InteractiveDragObject, IInteractiveItem
    {
        #region Konstruktor, data
        /// <summary>
        /// Konstruktor
        /// </summary>
        protected GComponent()
        { }
        /// <summary>
        /// Souřadnice headeru.
        /// Vložením nové hodnoty do souřadnic dojde i k správnému umístění Splitteru, který je součástí tohoto headeru, a případně i časové osy (pokud je součástí ColumnHeaderu).
        /// </summary>
        public override Rectangle Bounds { get { return base.Bounds; } set { base.Bounds = value; } }        // tahle property je tu jen kvůli XML komentáři, který je odlišný od base třídy :-)
        protected override void OnBoundsChanged(GPropertyChangeArgs<Rectangle> args)
        {
            base.OnBoundsChanged(args);
            this.SetChildBounds(args.NewValue);
        }
        /// <summary>
        /// Potomek by měl nastavit souřadnice svých Childs objektů, a/nebo svého splitteru, na základě nových souřadnic this headeru.
        /// </summary>
        /// <param name="newBounds"></param>
        protected virtual void SetChildBounds(Rectangle newBounds) { }
        #endregion
        #region Reference na objekty Owner
        /// <summary>
        /// Grid (grafický), do kterého patří zdejší tabulka
        /// </summary>
        public GGrid OwnerGGrid { get { return ((this.OwnerTable != null && this.OwnerTable.HasGTable) ? this.OwnerTable.GTable.Grid : null); } }
        /// <summary>
        /// Tabulka (grafická), do které patří toto záhlaví
        /// </summary>
        public GTable OwnerGTable { get { return ((this.OwnerTable != null && this.OwnerTable.HasGTable) ? this.OwnerTable.GTable : null); } }
        /// <summary>
        /// Datová tabulka, do které this záhlaví patří.
        /// Je k dispozici pro všechny tři typy záhlaví (Table, Column, Row).
        /// </summary>
        public abstract Table OwnerTable { get; }
        /// <summary>
        /// Typ záhlaví. Potomek musí přepsat na správnou hodnotu.
        /// </summary>
        protected abstract TableAreaType ComponentType { get; }
        #endregion
        #region Podpora kreslení
        /// <summary>
        /// true pokud tento prvek může být přetahován myší jinam
        /// </summary>
        protected override bool CanDrag { get { return false; } }
        protected virtual bool NeedDebug { get { return false; } }
        /// <summary>
        /// Kreslí prvek standardně (včetně kompletního obsahu).
        /// Může to být do vrstvy Standard i do jiné vrstvy, záleží na nastavení režimu Drag.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected override void DrawStandard(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            Application.App.Trace.Info(Application.TracePriority.Priority1_ElementaryTimeDebug, this.GetType().Name, "Draw", "Component", this.ToString(), "BoundsAbsolute: " + boundsAbsolute.ToString());

            if (!this.GraphicClip(e, boundsAbsolute)) return;                  // Není kam kreslit (oříznutí souřadnic vrátilo nulu)

            int? opacity = (e.DrawLayer == GInteractiveDrawLayer.Standard ? (int?)null : (int?)128);
            this.DrawContent(e, boundsAbsolute, false, opacity);
        }
        /// <summary>
        /// Kreslí prvek jako ducha (může být jen naznačený obsah), při přetahování myší.
        /// Může to být do vrstvy Standard i do jiné vrstvy, záleží na nastavení režimu Drag.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected override void DrawAsGhost(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            if (!this.GraphicClip(e, boundsAbsolute)) return;                  // Není kam kreslit (oříznutí souřadnic vrátilo nulu)

            this.DrawContent(e, boundsAbsolute, true, 128);
        }
        /// <summary>
        /// Vykreslí podklad prostoru pro záhlaví.
        /// Bázová třída GComponent vykreslí pouze pozadí, a to jen pokud se kreslí jako ghost do vrstvy Standard.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="drawAsGhost"></param>
        /// <param name="opacity"></param>
        protected virtual void DrawContent(GInteractiveDrawArgs e, Rectangle boundsAbsolute, bool drawAsGhost, int? opacity)
        {
            if (drawAsGhost && e.DrawLayer == GInteractiveDrawLayer.Standard)
                e.Graphics.FillRectangle(Brushes.DarkGray, boundsAbsolute);
        }
        /// <summary>
        /// Metoda zajistí oříznutí aktuální grafiky tak, aby prvek kreslil jen do přiměřeného prostoru.
        /// Oříznutí se provádí jen pro vrstvu Standard (protože v jiných vrstvách se provádí přetahování myší, a to je bez ořezávání).
        /// Oříznutí se provádí jako Permanent = až do následujícího resetu clipu, což prvek běžně nedělá.
        /// Clip() mi zajistí, že při pixelovém posunu záhlaví (sloupce, řádky, buňka) bude obsah vykreslena jen do příslušné části vymezeného prostoru pro danou oblast.
        /// Grafická organizace GTable není členěna nijak výrazně strukturovaně = GTable obsahuje jako Child jednotlivé prvky (GHeader, GColumn),
        ///  které mají svoje souřadnice relativní k GTable, ale mají se zobrazovat "oříznuté" do patřičných oblastí v GTable.
        /// Metoda vrací true = po oříznutí je nějaký důvod kreslit / false = neá význam něco kreslit, souřadnice prvku jsou mimo rozsah viditelných pixelů
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        protected bool GraphicClip(GInteractiveDrawArgs e, Rectangle boundsAbsolute)
        {
            // Ořezáváme jen při kreslení do vrstvy Standard, jinak ne:
            if (e.DrawLayer != GInteractiveDrawLayer.Standard) return true;

            // Prostor pro oblast (ColumnHeaders, RowHeaders, atd), se zohledněním souřadnic určených pro prostor tabulek v rámci Gridu:
            Rectangle areaAbsoluteBounds = this.OwnerGTable.GetAbsoluteBoundsForArea(this.ComponentType, true);

            // Prostor pro aktuální prvek = intersect se souřadnicemi prvku:
            Rectangle controlBounds = Rectangle.Intersect(areaAbsoluteBounds, boundsAbsolute);
            if (!controlBounds.HasPixels()) return false;

            // Prostor po oříznutí s aktuálním Clipem v grafice:
            //  Aktuální Clip v grafice obsahuje prostor, daný pro tento prvek v rámci jeho parentů:
            Rectangle clipBounds = Rectangle.Intersect(controlBounds, e.AbsoluteVisibleClip);
            e.GraphicsClipWith(clipBounds, true);

            // Pokud aktuální Clip je viditelný, pak jeho hodnota určuje souřadnice, kde je prvek interaktivní:
            bool isVisible = !e.IsVisibleClipEmpty;
            this.AbsoluteInteractiveBounds = (isVisible ? (Rectangle?)e.AbsoluteVisibleClip : (Rectangle?)null);

            return isVisible;
        }
        /// <summary>
        /// Pokud běží aplikace v IsDebug režimu, vykreslí rozpoznatelný rámeček okolo this prvku tak, aby bylo možno poznat jak to kreslíme... :-)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="opacity"></param>
        protected void DrawDebugBorder(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int? opacity)
        {
            /*
            if (Application.App.IsDebugMode)
                GPainter.DrawBorder(e.Graphics, boundsAbsolute, RectangleSide.All, 
                    System.Drawing.Drawing2D.DashStyle.Dot,
                    Color.Yellow, Color.LightGreen, Color.Red, Color.DarkBlue);
            */
        }
        #endregion
        #region Interaktivita
        #endregion
        #region Drag - podpora pro přesunutí this headeru na jinou pozici
        /// <summary>
        /// Procento zvýraznění začátku tohoto sloupce v procesu přetahování jiného sloupce.
        /// Pokud je větší než 0, zvýrazní se část počátku, protože se před tento sloupec přetáhne nějaký jiný.
        /// </summary>
        protected int DrawInsertMarkAtBegin { get; set; }
        /// <summary>
        /// Procento zvýraznění konce tohoto sloupce v procesu přetahování jiného sloupce.
        /// Pokud je větší než 0, zvýrazní se část konce, protože se za tento sloupec přetáhne nějaký jiný.
        /// </summary>
        protected int DrawInsertMarkAtEnd { get; set; }
        #endregion
    }
    #endregion
    #region Třída GTableHeader : třída zobrazující záhlaví tabulky (vlevo nahoře, vpravo nahoře)
    /// <summary>
    /// GTableHeader : třída zobrazující záhlaví tabulky (vlevo nahoře, vpravo nahoře)
    /// </summary>
    public class GTableHeader : GComponent
    {
        #region Konstruktor, data
        /// <summary>
        /// Konstruktor pro záhlaví, s odkazem na tabulku
        /// </summary>
        /// <param name="table"></param>
        public GTableHeader(Table table)
        {
            this._OwnerTable = table;
            this._ColumnSplitterInit();
        }
        private Table _OwnerTable;
        /// <summary>
        /// Umožní nastavit souřadnice pro Child objekty
        /// </summary>
        /// <param name="newBounds"></param>
        protected override void SetChildBounds(Rectangle newBounds)
        {
            this.SetSplitterBounds(newBounds);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "TableHeader in " + this._OwnerTable.ToString();
        }
        #endregion
        #region Reference na objekty Owner
        /// <summary>
        /// Tabulka, do které patří toto záhlaví
        /// </summary>
        public override Table OwnerTable { get { return this._OwnerTable; } }
        /// <summary>
        /// Typ záhlaví.
        /// </summary>
        protected override TableAreaType ComponentType { get { return TableAreaType.TableHeader; } }
        #endregion
        #region Public rozhraní
        /// <summary>
        /// Souřadnice na ose X, v pixelech, v koordinátech GTable, kde je tento sloupec právě zobrazen.
        /// </summary>
        public Int32Range VisualRangeX { get; set; }
        /// <summary>
        /// Souřadnice na ose Y, v pixelech, v koordinátech GTable, kde je tento sloupec právě zobrazen.
        /// </summary>
        public Int32Range VisualRangeY { get; set; }
        #endregion
        #region ColumnSplitter
        /// <summary>
        /// Svislý Splitter za tímto sloupcem (který představuje táhlaví řádků), řídí šířku tohoto sloupce (a tím všech sloupců shodného ColumnId v celém Gridu)
        /// </summary>
        public GSplitter ColumnSplitter { get { return this._ColumnSplitter; } }
        /// <summary>
        /// true pokud má být zobrazen splitter za tímto sloupcem, závisí na (OwnerTable.AllowRowHeaderWidthResize)
        /// </summary>
        public bool ColumnSplitterVisible { get { return (this.OwnerTable.AllowRowHeaderWidthResize); } }
        /// <summary>
        /// Připraví ColumnSplitter.
        /// Splitter je připraven vždy, i když se aktuálně nepoužívá.
        /// To proto, že uživatel (tj. aplikační kód) může změnit názor, a pak bude pozdě provádět inicializaci.
        /// </summary>
        protected void _ColumnSplitterInit()
        {
            this._ColumnSplitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Vertical, SplitterVisibleWidth = 0, SplitterActiveOverlap = 4 };
            this._ColumnSplitter.ValueSilent = this.Bounds.Right;
            this._ColumnSplitter.ValueChanging += new GPropertyChangedHandler<int>(_ColumnSplitter_LocationChange);
            this._ColumnSplitter.ValueChanged += new GPropertyChangedHandler<int>(_ColumnSplitter_LocationChange);
        }
        /// <summary>
        /// Eventhandler pro událost _ColumnSplitter.ValueChanging a ValueChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ColumnSplitter_LocationChange(object sender, GPropertyChangeArgs<int> e)
        {
            int left = this.Bounds.Left;
            int location = this.ColumnSplitter.Value;
            int width = location - left;
            this.OwnerGGrid.ColumnRowHeaderResizeTo(ref width);
            e.CorrectValue = left + width;
        }
        /// <summary>
        /// Nastaví souřadnice splitteru, po změně souřadnic this headeru.
        /// Splitter má být vždy umístěn na pravém okraji this záhlaví.
        /// </summary>
        /// <param name="newBounds"></param>
        protected void SetSplitterBounds(Rectangle newBounds)
        {
            this.ColumnSplitter.LoadFrom(newBounds, RectangleSide.Right, true);
        }
        /// <summary>
        /// ColumnSplitter
        /// </summary>
        protected GSplitter _ColumnSplitter;
        #endregion
        #region Interaktivita
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            this.OwnerGTable.TableHeaderClick(e);
        }
        #endregion
        #region Draw - kreslení záhlaví tabulky
        /// <summary>
        /// Vykreslí podklad prostoru pro záhlaví.
        /// Bázová třída GHeader vykreslí pouze pozadí, pomocí metody GPainter.DrawColumnHeader()
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="drawAsGhost"></param>
        /// <param name="opacity"></param>
        protected override void DrawContent(GInteractiveDrawArgs e, Rectangle boundsAbsolute, bool drawAsGhost, int? opacity)
        {
            base.DrawContent(e, boundsAbsolute, drawAsGhost, opacity);
            this.DrawGridHeader(e, boundsAbsolute, drawAsGhost, opacity);
            this.DrawDebugBorder(e, boundsAbsolute, opacity);
        }
        /// <summary>
        /// Vykreslí jen pozadí
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="drawAsGhost"></param>
        /// <param name="opacity"></param>
        protected void DrawGridHeader(GInteractiveDrawArgs e, Rectangle boundsAbsolute, bool drawAsGhost, int? opacity)
        {
            GPainter.DrawGridHeader(e.Graphics, boundsAbsolute, RectangleSide.Top, Skin.Grid.HeaderBackColor, true, Skin.Grid.HeaderLineColor, this.CurrentState, System.Windows.Forms.Orientation.Horizontal, null, opacity);
        }
        #endregion
    }
    #endregion
    #region Třída GColumnHeader : vizuální třída pro zobrazování záhlaví sloupce
    /// <summary>
    /// GColumnHeader : vizuální třída pro zobrazování záhlaví sloupce
    /// </summary>
    public class GColumnHeader : GComponent
    {
        #region Konstruktor, data
        public GColumnHeader(Column column)
            : base()
        {
            this._OwnerColumn = column;
            this._ColumnSplitterInit();
            this._TimeAxisInit();
        }
        private Column _OwnerColumn;
        /// <summary>
        /// Umožní nastavit souřadnice pro Child objekty
        /// </summary>
        /// <param name="newBounds"></param>
        protected override void SetChildBounds(Rectangle newBounds)
        {
            this.SetSplitterBounds(newBounds);
            this.SetTimeAxisBounds(newBounds);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "ColumnHeader in " + this._OwnerColumn.ToString();
        }
        #endregion
        #region Reference na objekty Owner
        /// <summary>
        /// Tabulka (datová), do které patří toto záhlaví
        /// </summary>
        public override Table OwnerTable { get { return this._OwnerColumn.Table; } }
        /// <summary>
        /// Sloupec, do kterého patří toto záhlaví
        /// </summary>
        public virtual Column OwnerColumn { get { return this._OwnerColumn; } }
        /// <summary>
        /// Typ záhlaví.
        /// </summary>
        protected override TableAreaType ComponentType { get { return TableAreaType.ColumnHeaders; } }
        #endregion
        #region Public rozhraní
        /// <summary>
        /// Souřadnice na ose X, v pixelech, v koordinátech GTable, kde je tento sloupec právě zobrazen.
        /// Může být null pro sloupce mimo zobrazovaný prostor.
        /// </summary>
        public Int32Range VisualRange { get; set; }
        #endregion
        #region ColumnSplitter
        /// <summary>
        /// Svislý Splitter za tímto sloupcem, řídí šířku tohoto sloupce (a tím všech sloupců shodného ColumnId v celém Gridu)
        /// </summary>
        public GSplitter ColumnSplitter { get { return this._ColumnSplitter; } }
        /// <summary>
        /// true pokud má být zobrazen splitter za tímto sloupcem, závisí na (OwnerTable.AllowColumnResize && OwnerColumn.AllowColumnResize)
        /// </summary>
        public bool ColumnSplitterVisible { get { return (this.OwnerTable.AllowColumnResize && this.OwnerColumn.AllowColumnResize); } }
        /// <summary>
        /// Připraví ColumnSplitter.
        /// Splitter je připraven vždy, i když se aktuálně nepoužívá.
        /// To proto, že uživatel (tj. aplikační kód) může změnit názor, a pak bude pozdě provádět inicializaci.
        /// </summary>
        protected void _ColumnSplitterInit()
        {
            this._ColumnSplitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Vertical, SplitterVisibleWidth = 0, SplitterActiveOverlap = 4 };
            this._ColumnSplitter.ValueSilent = this.Bounds.Right;
            this._ColumnSplitter.ValueChanging += new GPropertyChangedHandler<int>(_ColumnSplitter_LocationChange);
            this._ColumnSplitter.ValueChanged += new GPropertyChangedHandler<int>(_ColumnSplitter_LocationChange);
        }
        /// <summary>
        /// Eventhandler pro událost _ColumnSplitter.ValueChanging a ValueChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ColumnSplitter_LocationChange(object sender, GPropertyChangeArgs<int> e)
        {
            int left = this.Bounds.Left;
            int location = this.ColumnSplitter.Value;
            int width = location - left;
            this.OwnerGGrid.ColumnResizeTo(this.OwnerColumn, ref width);
            e.CorrectValue = left + width;
        }
        /// <summary>
        /// Nastaví souřadnice splitteru, po změně souřadnic this headeru.
        /// Splitter má být vždy umístěn na pravém okraji this záhlaví.
        /// </summary>
        /// <param name="newBounds"></param>
        protected void SetSplitterBounds(Rectangle newBounds)
        {
            this.ColumnSplitter.LoadFrom(newBounds, RectangleSide.Right, true);
        }
        /// <summary>
        /// ColumnSplitter
        /// </summary>
        protected GSplitter _ColumnSplitter;
        #endregion
        #region TimeAxis
        /// <summary>
        /// true pokud se pro sloupec má zobrazit časová osa v záhlaví
        /// </summary>
        public bool UseTimeAxis { get { return this.OwnerColumn.UseTimeAxis; } }
        /// <summary>
        /// Objekt, který provádí konverze časových údajů a pixelů, jde o vizuální časovou osu.
        /// Může být null, pokud this.UseTimeAxis je false.
        /// </summary>
        public ITimeConvertor TimeConvertor { get { return (this.UseTimeAxis ? this.TimeAxis : null); } }
        /// <summary>
        /// Objekt, který provádí konverze časových údajů a pixelů, jde o vizuální časovou osu.
        /// Může být null, pokud this.UseTimeAxis je false.
        /// </summary>
        public GTimeAxis TimeAxis
        {
            get
            {
                if (!this.OwnerColumn.UseTimeAxis) return null;
                this._TimeAxisCheck();
                return this._TimeAxis;
            }
        }
        /// <summary>
        /// Inicializace časové osy - nic neprovede, protože TimeAxis se vytváří On-Demand podle nastavení OwnerColumnu, v TimeAxis.get()
        /// </summary>
        protected void _TimeAxisInit() { /* TimeAxis je On-Demand, netřeba řešit inicializaci */ }
        private void _TimeAxisCheck()
        {
            if (this._TimeAxis != null) return;

            this._TimeAxis = new GTimeAxis();
            this._TimeAxis.ResizeContentMode = AxisResizeContentMode.ChangeScale;

            ((IInteractiveItem)this._TimeAxis).Parent = this;
            this._TimeAxis.ValueChanging += _TimeAxis_ValueChange;
            this._TimeAxis.ValueChanged += _TimeAxis_ValueChange;
        }
        /// <summary>
        /// Eventhandler události při/po změně ValueChanging nebo ValueChanged.
        /// Handler vyvolá metodu OnChangeTimeAxis() na OwnerGTable, 
        /// která zajistí synchronizaci této změny do ostatních tabulek (změna projde do metody this.RefreshTimeAxis, ale v jiných instancích této třídy).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TimeAxis_ValueChange(object sender, GPropertyChangeArgs<TimeRange> e)
        {
            this.OwnerGTable.OnChangeTimeAxis(this.OwnerColumn, e);
        }
        /// <summary>
        /// Je voláno z GGrid, po změně hodnoty Value na některé TimeAxis na sloupci columnId (v this.Columns), ale na jiné tabulce než je this tabulka.
        /// Tato tabulka je tedy Slave, a má si změnit svoji hodnotu bez toho, aby vyvolala další event o změně hodnoty.
        /// Metoda se volá po jakékoli změně hodnot na časové ose daného sloupce v JINÉ tabulce.
        /// </summary>
        /// <param name="e"></param>
        internal void RefreshTimeAxis(GPropertyChangeArgs<TimeRange> e)
        {
            if (!this.UseTimeAxis) return;
            this.TimeAxis.ValueSilent = e.NewValue;
            this.Repaint();
        }
        /// <summary>
        /// Umístí časovou osu do odpovídajícího prostoru v this objektu (nastaví TimeAxis.Bounds).
        /// </summary>
        /// <param name="newBounds"></param>
        protected void SetTimeAxisBounds(Rectangle newBounds)
        {
            if (this.UseTimeAxis)
            {   // Časovou osu kreslíme v ose X o 1px menší z obou stran, a v ose Y necháme nahoře 5 pixelů (pro Drag sloupce), dole necháme 1 pixel (pro strýčka Příhodu):
                Rectangle bounds = this.Bounds;
                this.TimeAxis.BoundsSilent = new Rectangle(1, 5, bounds.Width - 2, bounds.Height - 6);
            }
        }
        /// <summary>
        /// Časová osa, fyzické úložiště
        /// </summary>
        private GTimeAxis _TimeAxis;
        #endregion
        #region Childs items : záhlaví sloupce může obsahovat TimeAxis
        protected override IEnumerable<IInteractiveItem> Childs { get { this._ChildArrayCheck(); return this._ChildList; } }
        private void _ChildArrayCheck()
        {
            bool useTimeAxis = this.OwnerColumn.UseTimeAxis;
            if (useTimeAxis)
            {
                if (this._ChildList == null)
                {
                    GTimeAxis timeAxis = this.TimeAxis;
                    this._ChildList = new List<IInteractiveItem>();
                    this._ChildList.Add(timeAxis);
                }
            }
            else
            {
                if (this._ChildList != null)
                {
                    this._ChildList = null;
                }
            }
        }
        private List<IInteractiveItem> _ChildList;
        #endregion
        #region Interaktivita
        /// <summary>
        /// Metoda je volána v události MouseEnter, a jejím úkolem je přpravit data pro ToolTip.
        /// Zobrazení ToolTipu zajišťuje jádro.
        /// </summary>
        /// <param name="e"></param>
        protected override void PrepareToolTip(GInteractiveChangeStateArgs e)
        {
            Asol.Tools.WorkScheduler.Localizable.TextLoc toolTip = this.OwnerColumn.ToolTip;
            if (toolTip != null && !String.IsNullOrEmpty(toolTip.Text))
            {
                e.ToolTipData.TitleText = "Column info";
                e.ToolTipData.InfoText = toolTip.Text;
                e.ToolTipData.ShapeType = TooltipShapeType.Rectangle;
                e.ToolTipData.Opacity = 240;
            }
        }
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            this.OwnerGTable.ColumnHeaderClick(e, this.OwnerColumn);
        }
        #endregion
        #region Drag - Proces přesouvání sloupce
        /// <summary>
        /// Můžeme tento sloupec přemístit jinam? Závisí na OwnerTable.AllowColumnReorder
        /// </summary>
        protected override bool CanDrag { get { return this.OwnerTable.AllowColumnReorder; } }
        /// <summary>
        /// Volá se v procesu přesouvání. Zarovná souřadnice do povoleného rozmezí a najde sloupce, kam by se měl přesun provést.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="targetRelativeBounds"></param>
        protected override void DragThisOverBounds(GDragActionArgs e, Rectangle targetRelativeBounds)
        {
            // base třída je ochotná přesunout this objekt do libovolného místa (to je ostatně její velké pozitivum).
            // Ale ColumnHeader má mít prostor pro posun omezen jen na vhodná místa mezi ostatními sloupci:
            Rectangle allowedBounds = this.OwnerGTable.GetRelativeBoundsForArea(TableAreaType.ColumnHeaders);    // Souřadnice prostoru ColumnHeader, relativně k Table
            allowedBounds.Y = allowedBounds.Y + 5;                   // Prostor ColumnHeader omezím: dolů o 5px,
            allowedBounds.Height = 2 * allowedBounds.Height - 10;    //  a dolní okraj tak, aby byl o něco menší než 2x výšky.
            Rectangle modifiedBounds = targetRelativeBounds.FitInto(allowedBounds, false);         // Souřadnice "Drag" musí být uvnitř vymezeného prostoru

            // V této chvíli si base třída zapracuje "upravené" souřadnice (bounds) do this objektu,
            //  takže this záhlaví se bude vykreslovat "jako duch" v tomto omezeném prostoru:
            base.DragThisOverBounds(e, modifiedBounds);

            // Vyhledáme okolní sloupce, mezi které bychom rádi vložili this sloupec:
            Column prevColumn, nextColumn;
            int prevMark, nextMark;
            this._DragThisSearchHeaders(e, modifiedBounds, out prevColumn, out prevMark, out nextColumn, out nextMark);
            this._DragThisMarkHeaders(prevColumn, prevMark, nextColumn, nextMark);
        }
        /// <summary>
        /// Je vyvoláno po skončení přetahování (=při uvolnění myši nad cílovým prostorem). Je voláno na objektu, který je přetahován, nikoli na objektu kam bylo přetaženo.
        /// Bázová třída (InteractiveDragObject) vložila dané souřadnice do this.Bounds (přičemž ProcessAction = DragValueActions; a EventSourceType = (InteractiveChanged | BoundsChange).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsTarget"></param>
        protected override void DragThisDropToBounds(GDragActionArgs e, Rectangle boundsTarget)
        {
            if (this.DragThisToColumnOrder.HasValue)
                this.OwnerGGrid.ColumnMoveTo(this.OwnerColumn, this.DragThisToColumnOrder.Value);
        }
        /// <summary>
        /// Je voláno po skončení přetahování, ať už skončilo OK (=Drop) nebo Escape (=Cancel).
        /// Účelem je provést úklid po skončení přetahování.
        /// </summary>
        /// <param name="e"></param>
        protected override void DragThisOverEnd(GDragActionArgs e)
        {
            base.DragThisOverEnd(e);
            this.OwnerGTable.Columns.ForEachItem(c => c.ColumnHeader.ResetInsertMark());
            this.DragThisToColumnOrder = null;
        }
        /// <summary>
        /// Nuluje proměnné, které byly použity při přetahování nějakého jiného ColumnHeader přes this ColumnHeader.
        /// Zhasíná se tím prosvícení Drag-Target označení.
        /// </summary>
        protected void ResetInsertMark()
        {
            if (this.DrawInsertMarkAtBegin != 0 || this.DrawInsertMarkAtEnd != 0)
            {
                this.DrawInsertMarkAtBegin = 0;
                this.DrawInsertMarkAtEnd = 0;
                this.Repaint();
            }
        }
        /// <summary>
        /// Najde sloupce ležící před a za místem, kam bychom rádi vložili this sloupec v procesu přetahování.
        /// </summary>
        /// <param name="boundsAbsolute"></param>
        /// <param name="prevColumn"></param>
        /// <param name="nextColumn"></param>
        private void _DragThisSearchHeaders(GDragActionArgs e, Rectangle boundsAbsolute, out Column prevColumn, out int prevMark, out Column nextColumn, out int nextMark)
        {
            prevColumn = null;
            prevMark = 0;
            nextColumn = null;
            nextMark = 0;

            // Získám soupis sloupců, které jsou viditelné, vyjma this sloupec (podle ColumnId),
            //  tyto sloupce mají korektně vyplněny souřadnice 
            List<Column> columns = this.OwnerGTable.VisibleColumns
                .Where(c => (c.ColumnId != this.OwnerColumn.ColumnId))
                .ToList();
            int count = columns.Count;
            if (count == 0) return;

            // Určím souřadnici myši ve směru X, relativně k tabulce (protože relativně k tabulce jsou určeny souřadnice sloupců):
            int mouseX = this.OwnerGTable.GetRelativePoint(e.MouseCurrentAbsolutePoint).Value.X;

            // Najdu sloupec, nad kterým se aktuálně pohybuje myš v ose X:
            int index = -1;
            int lastIndex = count - 1;
            bool setDragToOrder = true;
            if (mouseX < columns[0].ColumnHeader.Bounds.X)
            {   // Myš je PŘED PRVNÍM ze sloupců:
                index = 0;
                nextColumn = columns[0];
                nextMark = 100;
            }
            else if (mouseX >= columns[lastIndex].ColumnHeader.Bounds.Right)
            {   // Myš je ZA POSLEDNÍM ze sloupců:
                index = lastIndex;
                prevColumn = columns[lastIndex];
                prevMark = 100;
            }
            else
            {   // Bude to složitější: myš je někde uvnitř, nad nějakým sloupcem:
                // Zkusím najít sloupec, nad kterým se nachází myš (na souřadnici X):
                index = columns.FindIndex(c => (mouseX >= c.ColumnHeader.Bounds.X && mouseX < c.ColumnHeader.Bounds.Right));
                // Může být, že sloupec nenajdu, protože v poli "columns" není obsažen prvek this, a nad ním může stále být myš umístěna!
                if (index >= 0)
                {   // Myš je nad nějakým sloupcem [index], zjistíme zda náš sloupec (this) budeme dávet před něj nebo za něj:
                    Rectangle targetBounds = columns[index].ColumnHeader.Bounds;
                    int targetCenterX = targetBounds.Center().X;
                    if (mouseX < targetCenterX)
                    {   // Myš je v levé polovině sloupce => přetáhneme nás PŘED ten sloupec:
                        nextColumn = columns[index];
                        nextMark = _DragThisGetMark(mouseX - targetBounds.X, targetBounds.Width);
                        prevColumn = (index > 0 ? columns[index - 1] : null);
                        prevMark = (index > 0 ? nextMark : 0);
                    }
                    else
                    {   // Myš je v pravé polovině sloupce => přetáhneme nás ZA ten sloupec:
                        prevColumn = columns[index];
                        prevMark = _DragThisGetMark(targetBounds.Right - mouseX, targetBounds.Width);
                        nextColumn = (index < lastIndex ? columns[index + 1] : null);
                        nextMark = (index < lastIndex ? prevMark : 0);
                    }
                }
                else
                {   // Myš je stále nad naším sloupcem, najdeme sloupce před a za námi:
                    int prevIndex = columns.FindLastIndex(c => (c.ColumnHeader.Bounds.Right < mouseX));
                    prevColumn = (prevIndex >= 0 ? columns[prevIndex] : null);
                    prevMark = (prevIndex >= 0 ? 100 : 0);
                    int nextIndex = columns.FindIndex(c => (c.ColumnHeader.Bounds.X >= mouseX));
                    nextColumn = (nextIndex >= 0 ? columns[nextIndex] : null);
                    nextMark = (nextIndex >= 0 ? 100 : 0);
                    this.DragThisToColumnOrder = null;
                    // V tomto případě nebudeme nastavovat _DragThisToColumnOrder:
                    setDragToOrder = false;
                }
            }

            if (setDragToOrder)
            {   // Nastavíme _DragThisToColumnOrder na hodnotu toho sloupce, před kterým chceme být umístěni:
                if (nextColumn != null)
                    this.DragThisToColumnOrder = nextColumn.ColumnOrder;
                else
                    // Pokud máme být umístěni za poslední sloupec, dáme hodnotu posledního sloupce + 1:
                    this.DragThisToColumnOrder = columns[lastIndex].ColumnOrder + 1;
            }
        }
        /// <summary>
        /// Vrací procentuální hodnotu (15 - 100), která reprezentuje vizuální přesnost zacílení při přesouvání sloupce myší.
        /// 15 = slabé, myš je někde uprostřed; 100 = přesné, myš je přesně na hraně cílového prvku.
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        private int _DragThisGetMark(int distance, int width)
        {
            int half = width / 2;
            if (distance < 0) return 100;
            if (distance >= half) return 15;
            return (int)(Math.Round(15f + 85f * (float)(half - distance) / (float)half, 0));
        }
        /// <summary>
        /// Mark specified columns as "Drag into after" and "Drag into before".
        /// All other columns mark as "no drag".
        /// Where is change, there will set DrawToLayer...
        /// </summary>
        /// <param name="prevColumn"></param>
        /// <param name="nextColumn"></param>
        private void _DragThisMarkHeaders(Column prevColumn, int prevMark, Column nextColumn, int nextMark)
        {
            int prevId = (prevColumn != null ? prevColumn.ColumnId : -1);
            int nextId = (nextColumn != null ? nextColumn.ColumnId : -1);
            foreach (Column column in this.OwnerTable.Columns)
            {
                var header = column.ColumnHeader;
                int markBegin = ((column.ColumnId == nextId) ? nextMark : 0);
                if (header.DrawInsertMarkAtBegin != markBegin)
                {
                    header.DrawInsertMarkAtBegin = markBegin;
                    header.Repaint();
                }

                int markEnd = ((column.ColumnId == prevId) ? prevMark : 0);
                if (header.DrawInsertMarkAtEnd != markEnd)
                {
                    header.DrawInsertMarkAtEnd = markEnd;
                    header.Repaint();
                }
            }
        }
        /// <summary>
        /// Cílové pořadí pro this sloupec v procesu přetahování tohoto sloupce na jiné místo.
        /// </summary>
        protected Int32? DragThisToColumnOrder { get; set; }
        #endregion
        #region Draw - kreslení záhlaví sloupce : ikona, text, značky při procesu Drag
        protected override void DrawContent(GInteractiveDrawArgs e, Rectangle boundsAbsolute, bool drawAsGhost, int? opacity)
        {
            base.DrawContent(e, boundsAbsolute, drawAsGhost, opacity);
            this.DrawGridHeader(e, boundsAbsolute, drawAsGhost, opacity);
            this.DrawInsertMarks(e, boundsAbsolute, opacity);
            this.DrawColumnHeader(e, boundsAbsolute, opacity);
            this.DrawDebugBorder(e, boundsAbsolute, opacity);
        }
        /// <summary>
        /// Vykreslí jen pozadí
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="drawAsGhost"></param>
        /// <param name="opacity"></param>
        protected void DrawGridHeader(GInteractiveDrawArgs e, Rectangle boundsAbsolute, bool drawAsGhost, int? opacity)
        {
            GPainter.DrawGridHeader(e.Graphics, boundsAbsolute, RectangleSide.Top, Skin.Grid.HeaderBackColor, true, Skin.Grid.HeaderLineColor, this.CurrentState, System.Windows.Forms.Orientation.Horizontal, null, opacity);
        }
        /// <summary>
        /// Do this záhlaví vykreslí ikonu třídění a titulkový text
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="opacity"></param>
        protected void DrawColumnHeader(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int? opacity)
        {
            Column column = this.OwnerColumn;
            string text = column.Title;
            Rectangle textArea = Rectangle.Empty;
            if (!String.IsNullOrEmpty(text) && !column.UseTimeAxis)
            {   // Sloupec má zadaný titulek, a nepoužívá časovou osu (pak nebudeme kreslit titulek, bude tam jen osa):
                FontInfo fontInfo = FontInfo.Caption;
                fontInfo.Bold = (column.SortCurrent == ItemSortType.Ascending || column.SortCurrent == ItemSortType.Descending);
                Color textColor = Skin.Grid.HeaderTextColor.SetOpacity(opacity);
                GPainter.DrawString(e.Graphics, boundsAbsolute, text, textColor, fontInfo, ContentAlignment.MiddleCenter, out textArea);

                // Obrázek odpovídající aktuálnímu třídění sloupce:
                Image sortImage = this.SortCurrentImage;
                if (sortImage != null)
                {
                    int x = textArea.X - sortImage.Width - 2;
                    int y = textArea.Center().Y - sortImage.Height / 2;
                    Rectangle sortBounds = new Rectangle(x, y, sortImage.Width, sortImage.Height);
                    e.Graphics.DrawImage(sortImage, sortBounds);
                }
            }
        }
        /// <summary>
        /// Do this záhlaví vykreslí značky, označující cíl při procesu Drag. Kreslí značky Begin i End, podle jejich hodnoty.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="opacity"></param>
        protected void DrawInsertMarks(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int? opacity)
        {
            int mark;

            mark = this.DrawInsertMarkAtBegin;
            if (mark > 0)
            {
                int m = (mark <= 100 ? mark : 100);
                int w = boundsAbsolute.Width * m / 300;
                Rectangle boundsMark = new Rectangle(boundsAbsolute.X + 1, boundsAbsolute.Y, w, boundsAbsolute.Height);
                GPainter.DrawInsertMark(e.Graphics, boundsMark, Skin.Modifiers.MouseDragTracking, System.Drawing.ContentAlignment.MiddleLeft);
            }

            mark = this.DrawInsertMarkAtEnd;
            if (mark > 0)
            {
                int m = (mark <= 100 ? mark : 100);
                int w = boundsAbsolute.Width * m / 300;
                Rectangle boundsMark = new Rectangle(boundsAbsolute.Right - w - 1, boundsAbsolute.Y, w, boundsAbsolute.Height);
                GPainter.DrawInsertMark(e.Graphics, boundsMark, Skin.Modifiers.MouseDragTracking, System.Drawing.ContentAlignment.MiddleRight);
            }
        }
        /// <summary>
        /// Image odvozený podle this.OwnerColumn.SortCurrent
        /// </summary>
        protected Image SortCurrentImage
        {
            get
            {
                switch (this.OwnerColumn.SortCurrent)
                {
                    case ItemSortType.Ascending: return Skin.Grid.SortAscendingImage;
                    case ItemSortType.Descending: return Skin.Grid.SortDescendingImage;
                }
                return null;
            }
        }
        #endregion
    }
    #endregion
    #region Třída GRow : vizuální třída pro zobrazování podkladu řádku (základní barva nebo podkladový graf nebo jiná data)
    public class GRow : GComponent
    {
        #region Konstruktor, data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="row"></param>
        public GRow(Row row)
        {
            this._OwnerRow = row;
        }
        private Row _OwnerRow;
        /// <summary>
        /// Umožní nastavit souřadnice pro Child objekty
        /// </summary>
        /// <param name="newBounds"></param>
        protected override void SetChildBounds(Rectangle newBounds)
        {
            // Pokud bych měl (já jako GRow) nějaké ChildItems, tak tady jim můžu nastavit Bounds, podle mých rozměrů.
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "GRow in " + this._OwnerRow.ToString();
        }
        #endregion
        #region Reference na objekty Owner
        /// <summary>
        /// Tabulka (datová), do které patří pozadí řádku
        /// </summary>
        public override Table OwnerTable { get { return this._OwnerRow.Table; } }
        /// <summary>
        /// Řádek, do kterého patří toto pozadí řádku
        /// </summary>
        public virtual Row OwnerRow { get { return this._OwnerRow; } }
        /// <summary>
        /// Typ prvku.
        /// </summary>
        protected override TableAreaType ComponentType { get { return TableAreaType.Row; } }
        #endregion
        #region Public rozhraní
        /// <summary>
        /// Souřadnice na ose Y, v pixelech, v koordinátech GTable, kde je tento řádek právě zobrazen.
        /// Může být null pro řádky mimo zobrazovaný prostor.
        /// Řádky, které jsou viditelné plně nebo i jen zčásti mají <see cref="VisualRange"/> naplněno, s tím že hodnota výšky odpovídá plné výšce řádku i když řádek je vidět jen zčásti.
        /// </summary>
        public Int32Range VisualRange { get; set; }
        #endregion
        #region Draw, příprava Childs
        /// <summary>
        /// Vykreslení obsahu
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="drawAsGhost"></param>
        /// <param name="opacity"></param>
        protected override void DrawContent(GInteractiveDrawArgs e, Rectangle boundsAbsolute, bool drawAsGhost, int? opacity)
        {
            base.DrawContent(e, boundsAbsolute, drawAsGhost, opacity);
            if (this.OwnerRow.BackgroundValueType == TableValueType.ITimeInteractiveGraph)
            { }
            this.OwnerGTable.DrawValue(e, boundsAbsolute, this.OwnerRow.BackgroundValue, this.OwnerRow.BackgroundValueType, this.OwnerRow, null);
        }
        /// <summary>
        /// Metoda připraví patřičné prvky tohoto řádku do svého seznam Childs prvků.
        /// Tyto prvky pak budou v řádku zobrazovány a budou interaktivní.
        /// </summary>
        /// <param name="rowHeadersBounds"></param>
        /// <param name="rowDataBounds"></param>
        /// <param name="visibleColumns"></param>
        public void PrepareChilds(Row row, Rectangle rowHeadersBounds, Rectangle rowDataBounds, Column[] visibleColumns)
        {
            Int32Range rowHeaderXRange = rowHeadersBounds.GetVisualRange(Orientation.Horizontal);  // Pozice RowHeader na ose X  (záhlaví řádků)
            Int32Range rowAreaXRange = rowDataBounds.GetVisualRange(Orientation.Horizontal);       // Pozice RowArea na ose X    (prostor pro data řádků, začíná za RowHeader, ale končí těsně před ScrollBarem)
            Int32Range rowChildsYRange = new Int32Range(0, this.VisualRange.Size - 0);             // Pozice všech Child items na ose Y (ta je relativní vzhledem k this řádku, proto začíná 0, a je o 1 pixel menší = o dolní GridLine)

            List<IInteractiveItem> childList = new List<IInteractiveItem>();

            int left = rowHeaderXRange.Begin;
            int right = left;

            // Viditelné buňky:
            foreach (Column column in visibleColumns)
            {
                GCell gCell = row[column.ColumnId].Control;
                this.PrepareChildOne(gCell, column.ColumnHeader.VisualRange, rowChildsYRange, childList, ref right);
            }

            // Záhlaví aktuálního řádku přidám až po buňkách, bude "navrchu" z hlediska kreslení i interaktivity:
            this.PrepareChildOne(row.RowHeader, rowHeaderXRange, rowChildsYRange, childList, ref right);
            row.RowHeader.PrepareSplitterBounds();


            // Souřadnice this řádku v rámci tabulky:
            Int32Range rowXRange = new Int32Range(left, right);
            Rectangle rowBounds = Int32Range.GetRectangle(rowXRange, this.VisualRange);
            if (this.Bounds != rowBounds) this.Bounds = rowBounds;

            // Soupis Child prvků:
            this._ChildItems = childList.ToArray();
        }
        /// <summary>
        /// Provede veškerou přípravu dat jedné Child buňky v aktuálním řádku
        /// </summary>
        /// <param name="item">Prvek (GCell nebo GRowHeader)</param>
        /// <param name="xRange">Pozice na ose X relativně k řádku</param>
        /// <param name="yRange">Pozice na ose Y relativně k řádku</param>
        /// <param name="childList">Soupis Child items, tam se prvek přidá</param>
        /// <param name="right">Průběžně střádaná Max() souřadnice Right</param>
        private void PrepareChildOne(IInteractiveItem item, Int32Range xRange, Int32Range yRange, List<IInteractiveItem> childList, ref int right)
        {
            if (item.Parent == null)
                item.Parent = this;
            Rectangle itemBounds = Int32Range.GetRectangle(xRange, yRange);
            if (item.Bounds != itemBounds) item.Bounds = itemBounds;
            if (right < itemBounds.Right) right = itemBounds.Right;
            childList.Add(item);
        }
        /// <summary>
        /// Aktuální Child prvky tohoto řádku
        /// </summary>
        private IInteractiveItem[] _ChildItems;
        /// <summary>
        /// Child prvky <see cref="IInteractiveItem.Childs"/>
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { return this._ChildItems; } }
        #endregion
    }
    #endregion
    #region Třída GRowHeader : vizuální třída pro zobrazování záhlaví řádku
    /// <summary>
    /// GRowHeader : vizuální třída pro zobrazování záhlaví řádku
    /// </summary>
    public class GRowHeader : GComponent
    {
        #region Konstruktor, data
        public GRowHeader(Row row)
            : base()
        {
            this._OwnerRow = row;
            this._RowSplitterInit();
        }
        private Row _OwnerRow;
        /// <summary>
        /// Umožní nastavit souřadnice pro Child objekty
        /// </summary>
        /// <param name="newBounds"></param>
        protected override void SetChildBounds(Rectangle newBounds)
        {
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "RowHeader in " + this._OwnerRow.ToString();
        }
        #endregion
        #region Reference na objekty Owner
        /// <summary>
        /// Tabulka (datová), do které patří toto záhlaví
        /// </summary>
        public override Table OwnerTable { get { return this._OwnerRow.Table; } }
        /// <summary>
        /// Řádek, do kterého patří toto záhlaví
        /// </summary>
        public virtual Row OwnerRow { get { return this._OwnerRow; } }
        /// <summary>
        /// Typ záhlaví.
        /// </summary>
        protected override TableAreaType ComponentType { get { return TableAreaType.RowHeaders; } }
        #endregion
        #region RowSplitter
        /// <summary>
        /// Vodorovný Splitter pod tímto řádkem, řídí výšku tohoto řádku.
        /// Jeho Bounds a tedy i <see cref="GSplitter.Value"/> jsou relativní k <see cref="GTable"/>.
        /// </summary>
        public GSplitter RowSplitter { get { return this._RowSplitter; } }
        /// <summary>
        /// Metoda je volána po nastavení souřadnic řádku, jejím úkolem je nastavit souřadnice splitteru.
        /// Splitter je child prvkem <see cref="GTable"/>, není child prvkem svého řádku <see cref="GRow"/> - to proto, že jeho pohyb a vykreslení má být volné v rámci téměř celé tabulky.
        /// </summary>
        public void PrepareSplitterBounds()
        {
            GTable gTable = this.OwnerGTable;
            if (this.Parent == null)
                this.Parent = gTable;

            GRow gRow = this.OwnerRow.Control;
            Rectangle rowHeadersBounds = gTable.GetRelativeBoundsForArea(TableAreaType.RowHeaders);          // Celý prostor všech RowHeaders, relativně k GTable, nás bude zajímat jeho pozice X
            Rectangle rowHeaderBounds = Int32Range.GetRectangle(rowHeadersBounds, gRow.VisualRange);         // Souřadnice RowHeader relativně k GTable (pozice na ose Y je převzata z řádku: VisualRange)
            this.RowSplitter.LoadFrom(rowHeaderBounds, RectangleSide.Bottom, true);                          // Splitter umístíme na dolní hranu prostoru RowHeader, relativně k GTable
        }
        /// <summary>
        /// true pokud má být zobrazen splitter za tímto řádkem, závisí na (OwnerTable.AllowRowResize)
        /// </summary>
        public bool RowSplitterVisible { get { return (this.OwnerTable.AllowRowResize); } }
        /// <summary>
        /// Připraví ColumnSplitter.
        /// Splitter je připraven vždy, i když se aktuálně nepoužívá.
        /// To proto, že uživatel (tj. aplikační kód) může změnit názor, a pak bude pozdě provádět inicializaci.
        /// </summary>
        protected void _RowSplitterInit()
        {
            this._RowSplitter = new GSplitter() { Orientation = System.Windows.Forms.Orientation.Horizontal, SplitterVisibleWidth = 0, SplitterActiveOverlap = 4 };
            this._RowSplitter.ValueSilent = this.Bounds.Right;
            this._RowSplitter.ValueChanging += new GPropertyChangedHandler<int>(_RowSplitter_LocationChange);
            this._RowSplitter.ValueChanged += new GPropertyChangedHandler<int>(_RowSplitter_LocationChange);
        }
        /// <summary>
        /// Eventhandler pro událost _RowSplitter.ValueChanging a ValueChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _RowSplitter_LocationChange(object sender, GPropertyChangeArgs<int> e)
        {
            int top = this.OwnerRow.Control.VisualRange.Begin;       // Souřadnice Y, kde začíná Row, relativně k GTable
            int value = this.RowSplitter.Value;                      // Aktuálně platná souřadnice Splitteru = nový Bottom souřadnic řádku
            int height = value - top;
            this.OwnerGTable.RowResizeTo(this.OwnerRow, ref height);
            e.CorrectValue = top + height;
        }
        /// <summary>
        /// RowSplitter
        /// </summary>
        protected GSplitter _RowSplitter;
        #endregion
        #region Interaktivita
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChanged(e);

            switch (e.ChangeState)
            {
                case GInteractiveChangeState.WheelUp:
                    this.OwnerGTable.ProcessRowAction(InteractivePositionAction.WheelUp);
                    break;
                case GInteractiveChangeState.WheelDown:
                    this.OwnerGTable.ProcessRowAction(InteractivePositionAction.WheelDown);
                    break;
            }
        }
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            this.OwnerGTable.RowHeaderClick(e, this.OwnerRow);
        }
        protected override bool CanDrag { get { return this.OwnerTable.AllowRowReorder; } }
        #endregion
        #region Draw - kreslení záhlaví řádku
        protected override void DrawContent(GInteractiveDrawArgs e, Rectangle boundsAbsolute, bool drawAsGhost, int? opacity)
        {
            base.DrawContent(e, boundsAbsolute, drawAsGhost, opacity);
            this.DrawGridHeader(e, boundsAbsolute, drawAsGhost, opacity);
            this.DrawMouseHot(e, boundsAbsolute, opacity);
            this.DrawSelectedRow(e, boundsAbsolute, opacity);
            this.DrawDebugBorder(e, boundsAbsolute, opacity);
        }
        /// <summary>
        /// Vykreslí jen pozadí
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="drawAsGhost"></param>
        /// <param name="opacity"></param>
        protected void DrawGridHeader(GInteractiveDrawArgs e, Rectangle boundsAbsolute, bool drawAsGhost, int? opacity)
        {
            GPainter.DrawGridHeader(e.Graphics, boundsAbsolute, RectangleSide.Left, Skin.Grid.HeaderBackColor, true, Skin.Grid.HeaderLineColor, this.CurrentState, System.Windows.Forms.Orientation.Horizontal, null, opacity);
        }
        /// <summary>
        /// Do this záhlaví podbarvení v situaci, kdy tento řádek je MouseHot
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="opacity"></param>
        protected void DrawMouseHot(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int? opacity)
        {
            if (!this.OwnerRow.IsMouseHot) return;

            Rectangle bounds = new Rectangle(boundsAbsolute.Right - 7, boundsAbsolute.Y + 1, 7, boundsAbsolute.Height - 2);
            GPainter.DrawInsertMark(e.Graphics, bounds, Skin.Modifiers.MouseHotColor, ContentAlignment.MiddleRight, false, 255);
        }
        /// <summary>
        /// Do this záhlaví vykreslí ikonu pro RowHeaderImage (typicky pro SelectedRow).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="opacity"></param>
        protected void DrawSelectedRow(GInteractiveDrawArgs e, Rectangle boundsAbsolute, int? opacity)
        {
            Image image = this.RowHeaderImage;
            if (image == null) return;

            Rectangle bounds = boundsAbsolute.Enlarge(-1, -1, -1, -1);
            bounds = image.Size.AlignTo(bounds, ContentAlignment.MiddleCenter, true);
            e.Graphics.DrawImage(image, bounds);
        }
        /// <summary>
        /// Image vhodný do záhlaví this řádku
        /// </summary>
        protected Image RowHeaderImage
        {
            get
            {
                Row row = this.OwnerRow;
                if (row.IsSelected) return Skin.Grid.RowSelectedImage;
                // Případné další ikonky mohou být zde...
                return null;
            }
        }
        /// <summary>
        /// Volba, zda metoda <see cref="Repaint()"/> způsobí i vyvolání metody <see cref="Parent"/>.<see cref="IInteractiveParent.Repaint"/>.
        /// </summary>
        protected override RepaintParentMode RepaintParent { get { return RepaintParentMode.Always; } }
        #endregion
    }
    #endregion
    #region Třída GCell : vizuální třída pro zobrazení obsahu buňky tabulky
    /// <summary>
    /// GCell : vizuální třída pro zobrazení obsahu buňky tabulky
    /// </summary>
    public class GCell : GComponent
    {
        #region Konstruktor, data
        public GCell(Cell cell)
        {
            this._Cell = cell;
        }
        private Cell _Cell;
        /// <summary>
        /// Umožní nastavit souřadnice pro Child objekty
        /// </summary>
        /// <param name="newBounds"></param>
        protected override void SetChildBounds(Rectangle newBounds)
        {
            // Pokud bych měl (já jako GCell) nějaké ChildItems, tak tady jim můžu nastavit Bounds, podle mých rozměrů.
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "GCell in " + this._Cell.ToString();
        }
        #endregion
        #region Reference na objekty Owner
        /// <summary>
        /// Tabulka (datová), do které patří tato buňka
        /// </summary>
        public override Table OwnerTable { get { return this._Cell.Table; } }
        /// <summary>
        /// Řádek, do kterého patří tato vizuální buňka
        /// </summary>
        public Row OwnerRow { get { return this._Cell.Row; } }
        /// <summary>
        /// Záhlaví řádku kam patří tato buňka, grafický prvek
        /// </summary>
        public GRowHeader RowHeader { get { return this._Cell.Row.RowHeader; } }
        /// <summary>
        /// Sloupec, do kterého patří tato vizuální buňka
        /// </summary>
        public Column OwnerColumn { get { return this._Cell.Column; } }
        /// <summary>
        /// Záhlaví sloupce, kam patří tato buňka, grafický prvek
        /// </summary>
        public GColumnHeader ColumnHeader { get { return this._Cell.Column.ColumnHeader; } }
        /// <summary>
        /// Datová buňka, do které patří tato vizuální buňka
        /// </summary>
        public Cell OwnerCell { get { return this._Cell; } }
        /// <summary>
        /// Typ oblasti tabulky.
        /// </summary>
        protected override TableAreaType ComponentType { get { return TableAreaType.Cell; } }
        #endregion
        #region Interaktivita
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChanged(e);

            switch (e.ChangeState)
            {
                case GInteractiveChangeState.KeyboardPreviewKeyDown:           // Sem chodí i klávesy Kurzor, Tab
                    this.KeyboardPreviewKeyDown(e);
                    break;
                case GInteractiveChangeState.KeyboardKeyPress:                 // Sem nechodí "kurzorové" klávesy, zatím nás event nezajímá. Mohl by aktivovat řádkový filtr...
                    break;
                case GInteractiveChangeState.KeyboardKeyDown:                  // Sem chodí PageUp, PageDown a písmena
                    break;
                case GInteractiveChangeState.WheelUp:
                    this.OwnerGTable.ProcessRowAction(InteractivePositionAction.WheelUp);
                    break;
                case GInteractiveChangeState.WheelDown:
                    this.OwnerGTable.ProcessRowAction(InteractivePositionAction.WheelDown);
                    break;
            }
        }
        /// <summary>
        /// Reaguje na klávesy typu kurzor, posune seznam řádků nahoru / dolů
        /// </summary>
        /// <param name="e"></param>
        private void KeyboardPreviewKeyDown(GInteractiveChangeStateArgs e)
        {
            InteractivePositionAction action = e.KeyboardPreviewArgs.GetInteractiveAction();
            if (action != InteractivePositionAction.None)
                e.KeyboardPreviewArgs.IsInputKey = this.OwnerGTable.ProcessRowAction(action);

            /*
            var code = e.KeyboardPreviewArgs.KeyCode;
            var data = e.KeyboardPreviewArgs.KeyData;
            int x = e.KeyboardPreviewArgs.KeyValue;
            
            e.ToolTipData.TitleText = "KeyboardPreviewKeyDown";
            e.ToolTipData.InfoText = "KeyCode: " + code.ToString() + "; KeyData: " + data.ToString() + "; Action = " + action.ToString();
            */
        }
        /// <summary>
        /// Myš vstoupila nad tuto buňku
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseEnter(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseEnter(e);
            this.OwnerGTable.CellMouseEnter(e, this.OwnerCell);
        }
        /// <summary>
        /// Myš odešla z této buňky
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseLeave(e);
            this.OwnerGTable.CellMouseLeave(e, this.OwnerCell);
        }
        /// <summary>
        /// Uživatel klikl do této buňky
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedLeftClick(e);
            this.OwnerGTable.CellClick(e, this.OwnerCell);
        }
        /// <summary>
        /// Metoda je volána v události MouseEnter, a jejím úkolem je přpravit data pro ToolTip.
        /// Zobrazení ToolTipu zajišťuje jádro.
        /// </summary>
        /// <param name="e"></param>
        protected override void PrepareToolTip(GInteractiveChangeStateArgs e)
        {
            Cell cell = this.OwnerCell;
            Asol.Tools.WorkScheduler.Localizable.TextLoc toolTip = this.OwnerCell.ToolTip;
            bool setStdTip = false;
            if (cell.ValueType == TableValueType.Image && cell.UseImageAsToolTip)
            {
                e.ToolTipData.TitleText = (toolTip != null ? toolTip.Text : null);
                e.ToolTipData.Icon = null;
                e.ToolTipData.Image = cell.Value as Image;
                setStdTip = true;
            }
            else if (cell.ToolTipImage != null)
            {
                if (toolTip != null && !String.IsNullOrEmpty(toolTip.Text))
                    e.ToolTipData.TitleText = toolTip.Text;
                else
                    e.ToolTipData.TitleText = "Data info";
                e.ToolTipData.Image = cell.ToolTipImage;
                setStdTip = true;
            }
            else if (toolTip != null && !String.IsNullOrEmpty(toolTip.Text))
            {
                e.ToolTipData.TitleText = "Data info";
                e.ToolTipData.InfoText = toolTip.Text;
                e.ToolTipData.Image = cell.ToolTipImage;
                setStdTip = true;
            }

            if (setStdTip)
            {
                e.ToolTipData.ShapeType = TooltipShapeType.Rectangle;
                e.ToolTipData.Opacity = 240;
            }
        }
        #endregion
        #region Draw
        /// <summary>
        /// Vykreslení obsahu
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="drawAsGhost"></param>
        /// <param name="opacity"></param>
        protected override void DrawContent(GInteractiveDrawArgs e, Rectangle boundsAbsolute, bool drawAsGhost, int? opacity)
        {
            base.DrawContent(e, boundsAbsolute, drawAsGhost, opacity);
            this.OwnerGTable.DrawValue(e, boundsAbsolute, this.OwnerCell.Value, this.OwnerCell.ValueType, this.OwnerCell.Row, this.OwnerCell);
            this.OwnerGTable.DrawRowGridLines(e, this.OwnerCell, boundsAbsolute);
            this.DrawDebugBorder(e, boundsAbsolute, opacity);
        }
        /// <summary>
        /// Volba, zda metoda <see cref="Repaint()"/> způsobí i vyvolání metody <see cref="Parent"/>.<see cref="IInteractiveParent.Repaint"/>.
        /// </summary>
        protected override RepaintParentMode RepaintParent { get { return RepaintParentMode.Always; } }
        #endregion
    }
    #endregion
    #region enum TableAreaType
    /// <summary>
    /// Typ prostoru v tabulce
    /// </summary>
    public enum TableAreaType
    {
        None,
        /// <summary>
        /// Prostor všech tabulek
        /// </summary>
        AllTables,
        /// <summary>
        /// Prostor celé tabulky
        /// </summary>
        Table,
        /// <summary>
        /// Záhlaví tabulky (pak jde o header vlevo nahoře, v křížení sloupce RowHeader a řádku ColumnHeader)
        /// </summary>
        TableHeader,
        /// <summary>
        /// Záhlaví sloupce
        /// </summary>
        ColumnHeaders,
        /// <summary>
        /// Prostor pro řádky: (záhlaví + data)
        /// </summary>
        Row,
        /// <summary>
        /// Záhlaví řádku
        /// </summary>
        RowHeaders,
        /// <summary>
        /// Prostor řádku vizuální (za koncem záhlaví, pod všemi buňkami)
        /// </summary>
        RowData,
        /// <summary>
        /// Prostor jedné buňky v tabulce (křížení řádku a sloupce)
        /// </summary>
        Cell,
        /// <summary>
        /// Svislý scrollbar vpravo
        /// </summary>
        VerticalScrollBar,
        /// <summary>
        /// Vodorovný scrollbar dole
        /// </summary>
        HorizontalScrollBar
    }
    #endregion
    #region interfaces pro podporu grafické tabulky : IGTableMember
    /// <summary>
    /// Člen grafické tabulky GTable, do kterého je možno vložit i odebrat referenci na danou GTable
    /// </summary>
    public interface IGTableMember
    {
        /// <summary>
        /// Reference na GTable, umístěná v datovém prvku
        /// </summary>
        GTable GTable { get; set; }
    }
    #endregion
}
