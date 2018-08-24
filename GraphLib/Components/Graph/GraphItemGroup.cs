using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;

namespace Asol.Tools.WorkScheduler.Components.Graph
{
    /// <summary>
    /// GTimeGraphGroup : skupina jednoho nebo více prvků ITimeGraphItem, obsahující sumární čas Time a Max(Height) z položek
    /// </summary>
    public class GTimeGraphGroup : ITimeGraphItem
    {
        #region Konstruktory; řízená tvorba GTimeGraphItem pro GTimeGraphGroup i pro jednotlivé položky ITimeGraphItem
        /// <summary>
        /// Konstruktor
        /// </summary>
        private GTimeGraphGroup(GTimeGraph parent)
        {
            this._ParentGraph = parent;
            this._ItemId = Application.App.GetNextId(typeof(ITimeGraphItem));
            this._FirstItem = null;
            this._PrepareGControlGroup(parent);                           // Připravím GUI prvek pro sebe = pro grupu, jeho parentem je vlastní graf
        }
        /// <summary>
        /// Konstruktor s předáním jediné položky
        /// </summary>
        /// <param name="items"></param>
        public GTimeGraphGroup(GTimeGraph parent, ITimeGraphItem item)
            : this(parent)
        {
            this._PrepareGControlItem(item);                              // Připravím GUI prvek pro jednotlivý prvek grafu, jeho parentem bude grafický prvek této grupy (=this.GControl)
            this._FirstItem = item;
            this._Items = new ITimeGraphItem[] { item };
            this._Store(item.Time.Begin, item.Time.End, item.Height);
        }
        /// <summary>
        /// Konstruktor s předáním skupiny položek, s výpočtem jejich sumárního časového intervalu a výšky
        /// </summary>
        /// <param name="items"></param>
        public GTimeGraphGroup(GTimeGraph parent, IEnumerable<ITimeGraphItem> items)
            : this(parent)
        {
            this._Items = items.ToArray();
            DateTime? begin = null;
            DateTime? end = null;
            float height = 0f;
            foreach (ITimeGraphItem item in this.Items)
            {
                this._PrepareGControlItem(item);                          // Připravím GUI prvek pro jednotlivý prvek grafu, jeho parentem bude grafický prvek této grupy (=this.GControl)
                if (this._FirstItem == null) this._FirstItem = item;
                if (item.Height > height) height = item.Height;
                if (item.Time.Begin.HasValue && (!begin.HasValue || item.Time.Begin.Value < begin.Value)) begin = item.Time.Begin;
                if (item.Time.End.HasValue && (!end.HasValue || item.Time.End.Value > end.Value)) end = item.Time.End;
                item.OwnerGraph = parent;
            }
            this._Store(begin, end, height);
        }
        /// <summary>
        /// Metoda vytvoří grafický control třídy <see cref="GTimeGraphItem"/> (<see cref="ITimeGraphItem.GControl"/>) pro this grupu.
        /// </summary>
        /// <param name="parent">Parent prvku, graf (neboť this je <see cref="GTimeGraphGroup"/>, pak jeho přímý Parent je <see cref="GTimeGraph"/>).</param>
        private void _PrepareGControlGroup(GTimeGraph parent)
        {
            if (this.GControl != null) return;
            this.GControl = new GTimeGraphItem(this, parent, this, GGraphControlPosition.Group);          // GUI prvek (GTimeGraphItem) dostává data (=this) a dostává vizuálního parenta (parent)
        }
        /// <summary>
        /// Metoda vytvoří grafický control třídy <see cref="GTimeGraphItem"/> (<see cref="ITimeGraphItem.GControl"/>) pro daný datový grafický prvek (item).
        /// </summary>
        /// <param name="item">Datový prvek grafu</param>
        private void _PrepareGControlItem(ITimeGraphItem item)
        {
            item.GControl = new GTimeGraphItem(item, this.GControl, this, GGraphControlPosition.Item);    // GUI prvek (GTimeGraphItem) dostává data (=item) a dostává vizuálního parenta (this.GControl)
            this.GControl.AddItem(item.GControl);                         // Náš hlavní GUI prvek (ten od grupy) si přidá další svůj Child prvek
        }
        /// <summary>
        /// Zadané údaje vloží do <see cref="Time"/> a <see cref="Height"/>, vypočte hodnotu <see cref="IsValidRealTime"/>.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="height"></param>
        private void _Store(DateTime? begin, DateTime? end, float height)
        {
            this._Time = new TimeRange(begin, end);
            this._Height = height;
            this._IsValidRealTime = ((height > 0f) && (begin.HasValue && end.HasValue && end.Value > begin.Value));
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Time: " + this.Time.ToString() +
                "; Height: " + this.Height.ToString() +
                "; UseSpace: " + (this.CoordinateYLogical == null ? "none" : this.CoordinateYLogical.ToString());
        }
        /// <summary>
        /// Parent této grupy položek = graf
        /// </summary>
        private GTimeGraph _ParentGraph;
        #endregion
        #region Privátní proměnné
        private ITimeInteractiveGraph _OwnerGraph;
        private int _ItemId;
        private ITimeGraphItem _FirstItem;
        private ITimeGraphItem[] _Items;
        private float _Height;
        private TimeRange _Time;
        private bool _IsValidRealTime;
        #endregion
        #region Souřadnice prvku na ose X i Y (logické, virtuální, reálné)
        /// <summary>
        /// Metoda připraví souřadnice this grupy na ose X, včetně jejích grafických items
        /// </summary>
        /// <param name="timeConvert"></param>
        /// <param name="coordinateYVirtual"></param>
        /// <param name="itemsCount"></param>
        public void PrepareCoordinateX(Func<TimeRange, Int32Range> timeConvert, ref int itemsCount)
        {
            Int32Range groupX = timeConvert(this.Time);
            this.GControl.CoordinateX = groupX;
            foreach (ITimeGraphItem item in this.Items)
            {
                itemsCount++;
                Int32Range absX = timeConvert(item.Time);                                // Vrací souřadnici X v koordinátech grafu
                int relBegin = absX.Begin - groupX.Begin;
                Int32Range relX = Int32Range.CreateFromBeginSize(relBegin, absX.Size);   // Získáme souřadnici X relativní k prvku Group, který je Parentem daného item
                item.GControl.CoordinateX = relX;
            }
            this.InvalidateBounds();
        }
        /// <summary>
        /// Metoda připraví reálné souřadnice Bounds do this grupy a jejích grafických items.
        /// Metoda může být volána opakovaně, sama si určí kdy je třeba něco měnit.
        /// </summary>
        public void PrepareBounds()
        {
            if (this.IsValidBounds) return;

            Int32Range groupY = this.CoordinateYVisual; // CoordinateYReal;
            this.GControl.Bounds = Int32Range.GetRectangle(this.GControl.CoordinateX, groupY);

            // Child prvky mají svoje souřadnice (Bounds) relativní k this prvku (který je jejich parentem), proto mají Y souřadnici { 0 až this.Y.Size }:
            Int32Range itemY = new Int32Range(0, groupY.Size);
            foreach (ITimeGraphItem item in this.Items)
                item.GControl.Bounds = Int32Range.GetRectangle(item.GControl.CoordinateX, itemY);

            this._IsValidBounds = true;
        }
        protected Int32Range CoordinateYVisual
        {
            get
            {
                Int32Range yReal = this.CoordinateYReal;
                int yDMax = (yReal.Size / 3);
                int yDiff = this._FirstItem.Layer;
                yDiff = ((yDiff < 0) ? 0 : (yDiff > yDMax ? yDMax : yDiff));
                if (yDiff != 0)
                    yReal = new Int32Range(yReal.Begin + 1 + yDiff, yReal.End - yDiff);
                return yReal;
            }
        }

        /// <summary>
        /// Invaliduje platnost souřadnic Bounds
        /// </summary>
        protected void InvalidateBounds()
        {
            this._IsValidBounds = false;
        }
        /// <summary>
        /// true pokud Bounds tohoto prvku i vnořených prvků jsou platné.
        /// </summary>
        protected bool IsValidBounds { get { return _IsValidBounds; } }
        private bool _IsValidBounds;
        /// <summary>
        /// Logická souřadnice tohoto prvku na ose Y. Souřadnice 0 odpovídá hodnotě 0 na ose Y, kladná čísla jsou fyzicky nahoru, záporná jsou povolená a jdou dolů.
        /// Jednotka je logická (nikoli pixely): prvek s výškou 1 je standardně vysoký.
        /// Vedle toho existují souřadné systémy Virtual (v pixelech, odspodu) a Real (v pixelech, od horního okraje).
        /// </summary>
        public Interval<float> CoordinateYLogical { get { return this._CoordinateYLogical; } set { this._CoordinateYLogical = value; this.InvalidateBounds(); } }
        private Interval<float> _CoordinateYLogical;
        /// <summary>
        /// Virtuální souřadnice na ose Y. Jednotkou jsou pixely.
        /// Hodnota 0 odpovídá pixelu úplně dole na grafu, tj. jako na matematické ose: Y jde odspodu nahoru.
        /// </summary>
        public Int32Range CoordinateYVirtual { get { return this._CoordinateYVirtual; } set { this._CoordinateYVirtual = value; this.InvalidateBounds(); } }
        private Int32Range _CoordinateYVirtual;
        /// <summary>
        /// Reálné souřadnice na ose Y. Jednotkou jsou pixely.
        /// Hodnota 0 odpovídá pixelu na souřadnici Bounds.Top, tj. jako ve Windows.Forms: Y jde odshora dolů.
        /// </summary>
        public Int32Range CoordinateYReal { get { return this._CoordinateYReal; } set { this._CoordinateYReal = value; this.InvalidateBounds(); } }
        private Int32Range _CoordinateYReal;
        #endregion
        #region Public prvky
        /// <summary>
        /// Graf, do něhož tento prvek (grupa) patří
        /// </summary>
        public GTimeGraph Graph { get { return this._ParentGraph; } }
        /// <summary>
        /// Pole všech základních prvků <see cref="ITimeGraphItem"/> zahrnutých v tomto objektu.
        /// Pole má vždy nejméně jeden prvek.
        /// První prvek tohoto pole <see cref="_FirstItem"/> je nositelem některých klíčových informací.
        /// </summary>
        public ITimeGraphItem[] Items { get { return this._Items; } }
        /// <summary>
        /// Počet prvků pole <see cref="Items"/>
        /// </summary>
        public int ItemCount { get { return this._Items.Length; } }
        /// <summary>
        /// Relativní výška tohoto prvku. Standardní hodnota = 1.0F. Fyzická výška (v pixelech) jednoho prvku je dána součinem 
        /// <see cref="Height"/> * <see cref="GTimeGraph.GraphParameters"/>: <see cref="TimeGraphProperties.OneLineHeight"/>
        /// Prvky s výškou 0 a menší nebudou vykresleny.
        /// </summary>
        public float Height { get { return this._Height; } }
        /// <summary>
        /// Režim editovatelnosti položky grafu
        /// </summary>
        public GraphItemBehaviorMode BehaviorMode { get { return this._FirstItem.BehaviorMode; } }
        /// <summary>
        /// Barva pozadí prvku.
        /// </summary>
        public Color? BackColor { get { return this._FirstItem.BackColor; } }
        /// <summary>
        /// Styl vzorku kresleného v pozadí.
        /// null = Solid.
        /// </summary>
        public System.Drawing.Drawing2D.HatchStyle? BackStyle { get { return this._FirstItem.BackStyle; } }
        /// <summary>
        /// Barva spojovací linky mezi prvky jedné skupiny.
        /// Default = null = kreslí se barvou <see cref="BackColor"/>, která je morfována na 50% do barvy DimGray a zprůhledněna na 50%.
        /// </summary>
        public Color? LinkBackColor { get { return this._FirstItem.LinkBackColor; } }
        /// <summary>
        /// Barva okraje (ohraničení) prvku.
        /// </summary>
        public Color? BorderColor { get { return this._FirstItem.BorderColor; } }
        /// <summary>
        /// Souhrnný čas všech prvků v této skupině. Je vypočten při vytvoření prvku.
        /// Pouze prvek, jehož čas je kladný (End je vyšší než Begin) je zobrazován.
        /// </summary>
        public TimeRange Time { get { return this._Time; } }
        /// <summary>
        /// Obsahuje true, když tento prvek je vhodné zobrazovat (má kladný čas i výšku).
        /// </summary>
        internal bool IsValidRealTime { get { return this._IsValidRealTime; } }
        /// <summary>
        /// Vizuální prvek, který v sobě zahrnuje jak podporu pro vykreslování, tak podporu interaktivity.
        /// A přitom to nevyžaduje od třídy, která fyzicky implementuje <see cref="ITimeGraphItem"/>.
        /// Aplikační kód (respektive implementační objekt <see cref="ITimeGraphItem"/>) se o tuto property nemusí starat, řídící mechanismus sem vloží v případě potřeby new instanci.
        /// Implementátor pouze poskytuje úložiště pro tuto instanci.
        /// </summary>
        public GTimeGraphItem GControl { get; set; }
        #endregion
        #region Childs, Interaktivita, Draw()
        /// <summary>
        /// Vykreslí tuto grupu. Kreslí pouze pokud obsahuje více než 1 prvek, a pokud vrstva <see cref="ITimeGraphItem.Layer"/> je nula nebo kladná (pro záporné vrstvy se nekreslí).
        /// </summary>
        /// <param name="e">Standardní data pro kreslení</param>
        /// <param name="boundsAbsolute">Absolutní souřadnice tohoto prvku</param>
        /// <param name="drawMode">Režim kreslení (má význam pro akce Drag & Drop)</param>
        public void Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            if (!this.IsValidRealTime || this._FirstItem.Layer < 0 || this.ItemCount <= 1) return;

            Color? backColor = this.LinkBackColor;
            if (!backColor.HasValue)
                // Nemáme explicitně danou barvu linky => odvodíme ji z barvy pozadí prvku + morphing:
                backColor = (this.BackColor.HasValue ? this.BackColor.Value : Skin.Graph.ElementBackColor).Morph(Skin.Graph.ElementLinkBackColor);
            backColor = Color.FromArgb(128, backColor.Value);
            Color? borderColor = backColor;

            Rectangle boundsLink = boundsAbsolute.Enlarge(-1, -2, -1, -2);
            GPainter.DrawEffect3D(e.Graphics, boundsLink, backColor.Value, System.Windows.Forms.Orientation.Horizontal, this.GControl.InteractiveState, force3D: false);
        }
        /// <summary>
        /// Metoda volaná pro vykreslování obsahu "Přes Child prvky".
        /// </summary>
        /// <param name="e"></param>
        public void DrawOverChilds(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            if (!this.DrawTextInCurrentState) return;
            Rectangle boundsVisibleAbsolute = Rectangle.Intersect(e.AbsoluteVisibleClip, boundsAbsolute);

            // Text pro aktuální velikost je pravděpodobně uložený v paměti, a pokud není tak jej explicitně zjistíme a do paměti uložíme:
            string text = this.GetCaptionForSize(boundsAbsolute.Size);
            FontInfo fontInfo = FontInfo.CaptionBold;
            if (text == null)
            {   // Získáme text Caption z datového zdroje grafu:
                text = this._ParentGraph.GraphItemGetCaptionText(e, fontInfo, this, this._FirstItem, GGraphControlPosition.Group, boundsAbsolute, boundsVisibleAbsolute);
                this.SetCaptionForSize(boundsAbsolute.Size, text);
            }

            // Kreslit text:
            if (!String.IsNullOrEmpty(text))
            {
                Color foreColor = this.GControl.BackColor.Contrast();
                GPainter.DrawString(e.Graphics, boundsAbsolute, text, foreColor, fontInfo, ContentAlignment.MiddleCenter);
            }
        }
        /// <summary>
        /// Vrací true, pokud se v aktuálním stavu objektu má kreslit text.
        /// To záleží na interaktivním stavu (MouseOver, Drag, Selected...) a na nastavení vlastností prvku grafu.
        /// </summary>
        protected bool DrawTextInCurrentState
        {
            get { return GTimeGraph.IsCaptionVisible(this._FirstItem.BehaviorMode, this.GControl.InteractiveState, (this.GControl.IsSelected || this._Items.Any(i => i.GControl.IsSelected))); }
        }
        /// <summary>
        /// Vrátí text pro danou velikost textu, pokud si ji pro tuto velikost pamatujeme.
        /// Vrací null, pokud pro danou velikost nemáme informaci.
        /// Vrátí prázdný string, pokud si pamatujeme, že text nebyl vygenerován (aby se nemusel generovat znovu).
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        protected string GetCaptionForSize(Size size)
        {
            return ((this._CaptionForSizeSize.HasValue && this._CaptionForSizeSize.Value == size) ? this._CaptionForSizeText : null);
        }
        /// <summary>
        /// Uloží si danou velikost a jí odpovídající text (namísto null ukládá prázdný string).
        /// </summary>
        /// <param name="size"></param>
        /// <param name="text"></param>
        protected void SetCaptionForSize(Size size, string text)
        {
            this._CaptionForSizeSize = size;
            this._CaptionForSizeText = (text != null ? text : "");
        }
        /// <summary>
        /// Velikost prvku, pro kterou máme uložený text Caption v <see cref="_CaptionForSizeText"/>
        /// </summary>
        private Size? _CaptionForSizeSize;
        /// <summary>
        /// Text prvku, který je platný pro jeho velikost <see cref="_CaptionForSizeSize"/>
        /// </summary>
        private string _CaptionForSizeText;
        /// <summary>
        /// Porovná dvě instance <see cref="GTimeGraphGroup"/> podle <see cref="ITimeGraphItem.Order"/> ASC, <see cref="ITimeGraphItem.Time"/> ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareOrderTimeAsc(GTimeGraphGroup a, GTimeGraphGroup b)
        {
            int cmp = a._FirstItem.Order.CompareTo(b._FirstItem.Order);
            if (cmp == 0)
                cmp = TimeRange.CompareByBeginAsc(a.Time, b.Time);
            return cmp;
        }
        #endregion
        #region Podpora pro Drag and Drop
        /// <summary>
        /// Vrací true, pokud daný prvek může být přemísťován.
        /// Rozhoduje o tom Group, protože jednotlivé Items přemisťovat nelze.
        /// </summary>
        internal bool IsDragEnabled
        {
            get { return this.BehaviorMode.HasAnyFlag(GraphItemBehaviorMode.AnyMove); }
        }
        #endregion
        #region explicit ITimeGraphItem members
        ITimeInteractiveGraph ITimeGraphItem.OwnerGraph { get { return this._OwnerGraph; } set { this._OwnerGraph = value; } }
        int ITimeGraphItem.ItemId { get { return this._ItemId; } }
        int ITimeGraphItem.Layer { get { return this._FirstItem.Layer; } }
        int ITimeGraphItem.Level { get { return this._FirstItem.Level; } }
        int ITimeGraphItem.Order { get { return this._FirstItem.Order; } }
        int ITimeGraphItem.GroupId { get { return this._FirstItem.GroupId; } }
        TimeRange ITimeGraphItem.Time { get { return this.Time; } }
        float ITimeGraphItem.Height { get { return this.Height; } }
        GraphItemBehaviorMode ITimeGraphItem.BehaviorMode { get { return this.BehaviorMode; } }
        Color? ITimeGraphItem.BackColor { get { return this.BackColor; } }
        System.Drawing.Drawing2D.HatchStyle? ITimeGraphItem.BackStyle { get { return this.BackStyle; } }
        Color? ITimeGraphItem.LinkBackColor { get { return this.LinkBackColor; } }
        Color? ITimeGraphItem.BorderColor { get { return this.BorderColor; } }
        GTimeGraphItem ITimeGraphItem.GControl { get { return this.GControl; } set { this.GControl = value; } }
        void ITimeGraphItem.Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode) { this.Draw(e, boundsAbsolute, drawMode); }
        #endregion
    }
}
