using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;
using Noris.LCS.Base.WorkScheduler;

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
        /// <param name="parent"></param>
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
        /// <param name="parent"></param>
        /// <param name="item"></param>
        public GTimeGraphGroup(GTimeGraph parent, ITimeGraphItem item)
            : this(parent)
        {
            this._PrepareGControlItem(item);                              // Připravím GUI prvek pro jednotlivý prvek grafu, jeho parentem bude grafický prvek této grupy (=this.GControl)
            this._FirstItem = item;
            this._Items = new ITimeGraphItem[] { item };
            bool canResize = item.BehaviorMode.HasFlag(GraphItemBehaviorMode.ResizeTime);
            this._Store(item.Time.Begin, item.Time.End, item.Height, canResize);
        }
        /// <summary>
        /// Konstruktor s předáním skupiny položek, s výpočtem jejich sumárního časového intervalu a výšky
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="items"></param>
        public GTimeGraphGroup(GTimeGraph parent, IEnumerable<ITimeGraphItem> items)
            : this(parent)
        {
            // Vstupní prvky (items) mohou být (ze vstupních dat) nesetříděné podle jejich času, 
            //  ale pro korektní fungování grupy musí být prvky v jedné grupě setříděny.
            List<ITimeGraphItem> itemList = items.ToList();
            if (itemList.Count > 1)
                itemList.Sort((a,b) => DateTime.Compare(a.Time.Begin.Value, b.Time.Begin.Value));
            this._Items = itemList.ToArray();
            DateTime? begin = null;
            DateTime? end = null;
            float height = 0f;
            bool canResize = false;
            foreach (ITimeGraphItem item in this.Items)
            {
                this._PrepareGControlItem(item);                          // Připravím GUI prvek pro jednotlivý prvek grafu, jeho parentem bude grafický prvek této grupy (=this.GControl)
                if (this._FirstItem == null) this._FirstItem = item;
                if (item.Height > height) height = item.Height;
                if (item.Time.Begin.HasValue && (!begin.HasValue || item.Time.Begin.Value < begin.Value)) begin = item.Time.Begin;
                if (item.Time.End.HasValue && (!end.HasValue || item.Time.End.Value > end.Value)) end = item.Time.End;
                if (!canResize && item.BehaviorMode.HasFlag(GraphItemBehaviorMode.ResizeTime)) canResize = true;
                item.OwnerGraph = parent;
            }
            this._Store(begin, end, height, canResize);
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
            this.GControl.AddGraphItem(item.GControl);                         // Náš hlavní GUI prvek (ten od grupy) si přidá další svůj Child prvek
        }
        /// <summary>
        /// Zadané údaje vloží do <see cref="Time"/> a <see cref="Height"/>, vypočte hodnotu <see cref="IsValidRealTime"/>.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="height"></param>
        /// <param name="canResize"></param>
        private void _Store(DateTime? begin, DateTime? end, float height, bool canResize)
        {
            this._Time = new TimeRange(begin, end);
            this._Height = height;
            this._IsValidRealTime = ((height > 0f) && (begin.HasValue && end.HasValue && end.Value > begin.Value));
            this.GControl.CanResize = canResize;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = this._CaptionForSizeText;
            string toolTip = this._FirstItem.ToolTip;
            return "Time: " + this.Time.ToString() +
                (!String.IsNullOrEmpty(text) ? "; Text: " + text : "") +
                (!String.IsNullOrEmpty(toolTip) ? "; ToolTip: " + toolTip : "") +
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
        /// <param name="offsetX"></param>
        /// <param name="itemsCount"></param>
        public void PrepareCoordinateX(Func<TimeRange, DoubleRange> timeConvert, int offsetX, ref int itemsCount)
        {
            Int32Range coordX;
            DoubleRange groupX = timeConvert(this.Time);             // Vrací souřadnici X v koordinátech grafu
            coordX = groupX.Int32RoundEnd;                           // Zaokrouhlím Begin i End
            int groupB = coordX.Begin;                               // Offset z absolutní do relativní souřadnice pro jednotlivé prvky
            if (offsetX != 0d) coordX = coordX.ShiftBy(offsetX);     // Posun celé grupy vlivem offsetu grafu vůči časové ose
            this.GControl.CoordinateX = coordX;                      // Relativní souřadnice grupy v rámci grafu
            foreach (ITimeGraphItem item in this.Items)
            {
                itemsCount++;
                DoubleRange itemX = timeConvert(item.Time);          // Vrací souřadnici X v koordinátech grafu
                coordX = itemX.Int32RoundEnd;                        // Zaokrouhlím Begin i End
                item.GControl.CoordinateX = coordX.ShiftBy(-groupB); // Posunu prvek na relativní souřadnici vzhledem ke grupě
            }
            this.InvalidateBounds();
        }
        /// <summary>
        /// Metoda připraví reálné souřadnice Bounds do this grupy a jejích grafických items.
        /// Metoda může být volána opakovaně, sama si určí kdy je třeba něco měnit.
        /// </summary>
        public void PrepareBounds()
        {
            if (!this.IsValidBounds)
                this.CalculateBounds();
        }
        /// <summary>
        /// Metoda vypočte reálné souřadnice Bounds do this grupy a jejích grafických items.
        /// Metoda při opakovaném volání skutečně přepočítá hodnoty.
        /// </summary>
        public void CalculateBounds()
        {
            Int32Range groupY = this.CoordinateYVisual;
            if (this.GControl.CoordinateX == null)
            {
                this.Graph.CheckValidCoordinateX();
                return;
            }
            this.GControl.Bounds = Int32Range.GetRectangle(this.GControl.CoordinateX, groupY);

            // Child prvky mají svoje souřadnice (Bounds) relativní k this prvku (který je jejich parentem), proto mají Y souřadnici { 0 až this.Y.Size }:
            Int32Range itemY = new Int32Range(0, groupY.Size);
            foreach (ITimeGraphItem item in this.Items)
                item.GControl.Bounds = Int32Range.GetRectangle(item.GControl.CoordinateX, itemY);

            this._IsValidBounds = true;
        }
        /// <summary>
        /// Souřadnice na ose S, vizuální (tzn. v pixelech)
        /// </summary>
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

            this.GControl.InvalidateBounds();
            foreach (ITimeGraphItem item in this.Items)
                item.GControl.InvalidateBounds();
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
        /// Prvek je viditelný?
        /// </summary>
        public bool IsVisible { get { return this._IsVisible; } set { this._IsVisible = value; } } private bool _IsVisible = true;
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
        /// Souhrnný čas všech prvků v této skupině. Je vypočten při vytvoření prvku.
        /// Pouze prvek, jehož čas je kladný (End je vyšší než Begin) je zobrazován.
        /// </summary>
        public TimeRange Time { get { return this._Time; } private set { this._Time = value; } }
        /// <summary>
        /// Relativní výška tohoto prvku. Standardní hodnota = 1.0F. Fyzická výška (v pixelech) jednoho prvku je dána součinem 
        /// <see cref="Height"/> * <see cref="GTimeGraph.CurrentGraphProperties"/>: <see cref="TimeGraphProperties.OneLineHeight"/> nebo <see cref="TimeGraphProperties.OneLinePartialHeight"/>, 
        /// podle toho zda graf obsahuje jen celočíselné výšky, nebo i zlomkové výšky.
        /// Prvky s výškou 0 a menší nebudou vykresleny.
        /// </summary>
        public float Height { get { return this._Height; } }
        /// <summary>
        /// GroupId: číslo skupiny. Prvky se shodným GroupId budou vykreslovány do společného "rámce", 
        /// a pokud mezi jednotlivými prvky <see cref="ITimeGraphItem"/> se shodným <see cref="GroupId"/> bude na ose X nějaké volné místo,
        /// nebude mezi nimi vykreslován žádný "cizí" prvek.
        /// </summary>
        public int GroupId { get { return this._FirstItem.GroupId; } }
        /// <summary>
        /// Level: Vizuální hladina. Prvky v jedné hladině jsou kresleny do společného vodorovného pásu, 
        /// další prvky ve vyšší hladině jsou všechny zase vykresleny ve svém odděleném pásu (nad tímto nižším pásem). 
        /// Nespadnou do prvků nižšího pásu i když by v něm bylo volné místo.
        /// </summary>
        public int Level { get { return this._FirstItem.Level; } }
        /// <summary>
        /// Layer: Vizuální vrstva. Prvky z různých vrstev jsou kresleny "přes sebe" = mohou se překrývat.
        /// Nižší hodnota je kreslena dříve.
        /// Například: záporná hodnota Layer reprezentuje "podklad" který se needituje.
        /// </summary>
        public int Layer { get { return this._FirstItem.Layer; } }
        /// <summary>
        /// Barva pozadí prvku.
        /// </summary>
        public Color? BackColor { get { return this._FirstItem.BackColor; } }
        /// <summary>
        /// Barva linek ohraničení prvku.
        /// Pokud je null, pak prvek nemá ohraničení pomocí linky (Border).
        /// </summary>
        public Color? LineColor { get { return this._FirstItem.LineColor; } }
        /// <summary>
        /// Režim editovatelnosti položky grafu
        /// </summary>
        public GraphItemBehaviorMode BehaviorMode { get { return this._FirstItem.BehaviorMode; } }
        /// <summary>
        /// true pokud this grupu je možno resizovat.
        /// Hodnota je platná až po doběhnutí konstruktoru, nikoli v době tvorby controlu <see cref="GControl"/>.
        /// </summary>
        public bool CanResize { get { return this.BehaviorMode.HasFlag(GraphItemBehaviorMode.ResizeTime); } }
        /// <summary>
        /// Obsahuje true, když tento prvek je vhodné zobrazovat (má kladný čas i výšku).
        /// </summary>
        internal bool IsValidRealTime { get { return this._IsValidRealTime; } }
        /// <summary>
        /// Obsahuje průhlednost pro vykreslení tohoto prvku do vrstvy Interactive.
        /// Vkládá se sem v procesu Drag and Drop, čte se v procesu Draw na prvku třídy <see cref="GTimeGraphItem"/>
        /// </summary>
        internal int? DragDropDrawInteractiveOpacity { get; set; }
        /// <summary>
        /// Obrázek vykreslený 1x za jednu grupu na souřadnici jejího začátku.
        /// Obrázek může být umístěn do kteréhokoli jednoho prvku v rámci grupy, akceptován bude první ve směru času.
        /// </summary>
        internal Image ImageBegin
        {
            get
            {
                ITimeGraphItem item = this._Items.FirstOrDefault(i => i.ImageBegin != null);
                return item?.ImageBegin;
            }
        }
        /// <summary>
        /// Obrázek vykreslený 1x za jednu grupu na souřadnici jejího konce.
        /// Obrázek může být umístěn do kteréhokoli jednoho prvku v rámci grupy, akceptován bude poslední ve směru času.
        /// </summary>
        internal Image ImageEnd
        {
            get
            {
                ITimeGraphItem item = this._Items.LastOrDefault(i => i.ImageEnd != null);
                return item?.ImageEnd;
            }
        }
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
        /// Vykreslí tuto grupu. 
        /// Grupa se kreslí pouze tehdy, pokud obsahuje více než 1 prvek, a pokud vrstva <see cref="ITimeGraphItem.Layer"/> je nula nebo kladná (pro záporné vrstvy se nekreslí).
        /// </summary>
        /// <param name="e">Standardní data pro kreslení</param>
        /// <param name="boundsAbsolute">Absolutní souřadnice tohoto prvku</param>
        /// <param name="drawMode">Režim kreslení (má význam pro akce Drag and Drop)</param>
        public void Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            if (!this.IsValidRealTime || this.Layer < 0 || this.ItemCount <= 1) return;
            this.GControl.DrawItem(e, boundsAbsolute, drawMode);
        }
        /// <summary>
        /// Metoda volaná pro vykreslování obsahu "Přes Child prvky".
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="drawMode"></param>
        public void DrawOverChilds(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            Rectangle boundsVisibleAbsolute = boundsAbsolute;
            if (e.DrawLayer == GInteractiveDrawLayer.Standard)
                boundsVisibleAbsolute = e.GetClip(boundsAbsolute);

            this.GControl.DrawImages(e, ref boundsAbsolute, boundsVisibleAbsolute, drawMode);

            if (!this.DrawTextInCurrentState) return;

            if (e.DrawLayer == GInteractiveDrawLayer.Standard)
                boundsVisibleAbsolute = e.GetClip(boundsAbsolute);

            FontInfo fontInfo = FontInfo.CaptionBold;
            string text = this.GetCaption(e, boundsAbsolute, boundsVisibleAbsolute, fontInfo);

            if (String.IsNullOrEmpty(text)) return;
            this.GControl.DrawText(e, boundsVisibleAbsolute, text, fontInfo);
        }
        /// <summary>
        /// Metoda vrátí text, který má být zobrazen v grafickém prvku
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="boundsVisibleAbsolute"></param>
        /// <param name="fontInfo"></param>
        /// <returns></returns>
        protected string GetCaption(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Rectangle boundsVisibleAbsolute, FontInfo fontInfo)
        {
            // 1. Pokud je text v prvku uveden explicitně, pak jej použijeme:
            string text = this._FirstItem.Text;
            if (!String.IsNullOrEmpty(text)) return text;

            // 2. Text pro aktuální velikost je pravděpodobně uložený v paměti (od posledně):
            Size size = boundsAbsolute.Size;
            text = this.GetCaptionForSize(size);
            if (text != null) return text;

            // 3. Pro danou velikost ještě text není zapamatován => Získáme text Caption z datového zdroje grafu a zapamatujeme si ho:
            CreateTextArgs args = new CreateTextArgs(this._ParentGraph, e, fontInfo, this, this._FirstItem, GGraphControlPosition.Group, boundsAbsolute, boundsVisibleAbsolute);
            text = this._ParentGraph.GraphItemGetCaptionText(args);
            this.SetCaptionForSize(size, text);

            return text;
        }
        /// <summary>
        /// Vrátí danou barvu, s modifikovanou průhledností Opacity (<see cref="Color.A"/>).
        /// Zadaná Opacity je respektována, ale pokud celý graf má deklarovaou svoji průhlednost v <see cref="GTimeGraph.GraphOpacity"/>, pak je akceptována rovněž.
        /// A dále, pokud se aktuálně kreslí do vrstvy <see cref="GInteractiveDrawLayer.Interactive"/>, pak je akceptována i hodnota <see cref="DragDropDrawInteractiveOpacity"/>.
        /// </summary>
        /// <param name="baseColor">Výchozí barva, typicky BackColor nebo ForeColor prvku grafu.</param>
        /// <param name="drawLayer">Vykreslovaná vrstva grafiky</param>
        /// <param name="drawMode">Režim kreslení předaný systémem</param>
        /// <param name="forGroup">true = pro barvu grupy / false = pro barvu prvku</param>
        /// <param name="forBackColor">true = pro barvu pozadí / false = pro barvu okrajů, čar a textů</param>
        /// <returns></returns>
        public Color? GetColorWithOpacity(Color? baseColor, GInteractiveDrawLayer drawLayer, DrawItemMode drawMode, bool forGroup, bool forBackColor)
        {
            if (!baseColor.HasValue) return null;
            return this.GetColorWithOpacity(baseColor.Value, drawLayer, drawMode, forGroup, forBackColor);
        }
        /// <summary>
        /// Vrátí danou barvu, s modifikovanou průhledností Opacity (<see cref="Color.A"/>).
        /// Zadaná Opacity je respektována, ale pokud celý graf má deklarovaou svoji průhlednost v <see cref="GTimeGraph.GraphOpacity"/>, pak je akceptována rovněž.
        /// A dále, pokud se aktuálně kreslí do vrstvy <see cref="GInteractiveDrawLayer.Interactive"/>, pak je akceptována i hodnota <see cref="DragDropDrawInteractiveOpacity"/>.
        /// </summary>
        /// <param name="baseColor">Výchozí barva, typicky BackColor nebo ForeColor prvku grafu.</param>
        /// <param name="drawLayer">Vykreslovaná vrstva grafiky</param>
        /// <param name="drawMode">Režim kreslení předaný systémem</param>
        /// <param name="forGroup">true = pro barvu grupy / false = pro barvu prvku</param>
        /// <param name="forBackColor">true = pro barvu pozadí / false = pro barvu okrajů, čar a textů</param>
        /// <returns></returns>
        public Color GetColorWithOpacity(Color baseColor, GInteractiveDrawLayer drawLayer, DrawItemMode drawMode, bool forGroup, bool forBackColor)
        {
            if (!this.IsDragged)
            {   // Běžný stav:
                return this._GetColorWithOpacityStandard(baseColor, forGroup, forBackColor, null);
            }
            else
            {   // Drag and Drop:
                if (drawLayer == GInteractiveDrawLayer.Standard)
                    return this._GetColorWithOpacityDragOriginal(baseColor, forGroup, forBackColor);
                else
                    return this._GetColorWithOpacityDragTarget(baseColor, forGroup, forBackColor);
            }
        }
        /// <summary>
        /// Vrátí danou barvu v režimu bez Drag and Drop
        /// </summary>
        /// <param name="baseColor"></param>
        /// <param name="interactiveOpacity"></param>
        /// <param name="forGroup"></param>
        /// <param name="forBackColor"></param>
        /// <returns></returns>
        public Color _GetColorWithOpacityStandard(Color baseColor, bool forGroup, bool forBackColor, int? interactiveOpacity)
        {
            int? groupOpacity = ((forGroup && forBackColor) ? (int?)170 : (int?)null);
            int? graphOpacity = this.Graph.GraphOpacity;
            if (!groupOpacity.HasValue && !graphOpacity.HasValue && !interactiveOpacity.HasValue) return baseColor;    // Zkratka - bez úprav
            return baseColor.ApplyOpacity(groupOpacity, graphOpacity, interactiveOpacity);
        }
        /// <summary>
        /// Vrátí danou barvu v režimu Drag and Drop, pozice Original
        /// </summary>
        /// <param name="baseColor"></param>
        /// <param name="forGroup"></param>
        /// <param name="forBackColor"></param>
        /// <returns></returns>
        public Color _GetColorWithOpacityDragOriginal(Color baseColor, bool forGroup, bool forBackColor)
        {
            Color color = baseColor.GrayScale().Morph(Color.White, 0.40f);
            return _GetColorWithOpacityStandard(color, forGroup, forBackColor, 64);
        }
        /// <summary>
        /// Vrátí danou barvu v režimu Drag and Drop, pozice Target
        /// </summary>
        /// <param name="baseColor"></param>
        /// <param name="forGroup"></param>
        /// <param name="forBackColor"></param>
        /// <returns></returns>
        public Color _GetColorWithOpacityDragTarget(Color baseColor, bool forGroup, bool forBackColor)
        {
            Color color = baseColor.Morph(Color.White, 0.15f);
            return _GetColorWithOpacityStandard(color, forGroup, forBackColor, 160);
        }
        /// <summary>
        /// Vrací true, pokud se v aktuálním stavu objektu má kreslit text.
        /// To záleží na interaktivním stavu (MouseOver, Drag, Selected...) a na nastavení vlastností prvku grafu.
        /// </summary>
        protected bool DrawTextInCurrentState
        {
            get
            {
                GraphItemBehaviorMode mode = this.BehaviorMode;
                GInteractiveState state = this.GControl.InteractiveState;
                bool isActive = this.GControl.IsSelected || this.GControl.IsFramed || this.GControl.IsActivated || this.IsDragged;
                return GTimeGraph.IsCaptionVisible(mode, state, isActive);
            }
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
        #region Podpora pro Drag and Drop a Resize
        /// <summary>
        /// Vrací true, pokud daný prvek může být přemísťován.
        /// Rozhoduje o tom Group, protože jednotlivé Items přemisťovat nelze.
        /// </summary>
        internal bool IsDragEnabled
        {
            get { return this.BehaviorMode.HasAnyFlag(GraphItemBehaviorMode.AnyMove); }
        }
        /// <summary>
        /// Obsahuje true, pokud this grupa je nyní přemisťována akcí DragMove.
        /// </summary>
        internal bool IsDragged { get { return this.GControl.InteractiveState.HasFlag(GInteractiveState.FlagDrag); } }
        /// <summary>
        /// Metoda do sebe vloží nově zadaný čas.
        /// Musí jej vložit i do konkrétních Items, proto aby po jakékoli vizuální invalidaci tento čas byl zachován.
        /// V podstatě jde o jiné rozmístění časů v Items tak, aby respektovaly původní velikost a rozmístění v celkovém čase, a vešly se do nového času.
        /// Nutným výsledkem je přesné zarovnání času Begin u prvního prvku a End u posledního, z toho se odvozuje při rekalkulacích čas grupy (který je cílem této metody).
        /// </summary>
        /// <param name="timeNew"></param>
        internal void SetTime(TimeRange timeNew)
        {
            if (timeNew == null) return;
            TimeRange timeOld = this.Time;
            if (timeNew == timeOld) return;                                         // Beze změny hodnoty

            // Mít prvky v Items je téměř povinnost. Ale pojistka je jistota :-) :
            if (this._Items != null && this._Items.Length > 0)
            {
                DateTime beginNew = timeNew.Begin.Value;
                DateTime endNew = timeNew.End.Value;
                if (this._Items.Length == 1)
                {   // Pokud máme jen jeden prvek v grupě, pak jednoduše změníme jeho čas na čas požadovaný:
                    this._Items[0].Time = new TimeRange(beginNew, endNew);
                }
                else
                {   // Pokud máme více než jeden prvek, musíme je do daného času rozprostřít a pokud možno zachovat jejich vlastní délky:
                    DateTime beginOld = timeOld.Begin.Value;
                    DateTime endOld = timeOld.End.Value;
                    long tickNew = timeNew.Size.Value.Ticks;
                    long tickOld = timeOld.Size.Value.Ticks;

                    // Varianty kombinací:
                    if (tickNew == tickOld) this._SetTimeShift(timeNew);            // Shift = přesun intervalu na jiné místo beze změny jeho délky
                    else if (tickNew > tickOld)                                     // Prodloužení času
                    {
                        if (beginNew == beginOld) this._SetTimeAddEnd(timeNew);     // Begin se nemění, End se zvyšuje
                        else if (endNew == endOld) this._SetTimeAddBegin(timeNew);  // End se nemění, Begin se snižuje (délka se zvyšuje)
                        else this._SetTimeAddBooth(timeNew);                        // Mění se Begin i End, délka se zvyšuje
                    }
                    else                                                            // Zkrácení času
                    {
                        if (beginNew == beginOld) this._SetTimeSubEnd(timeNew);     // Begin se nemění, End se snižuje (délka se snižuje)
                        else if (endNew == endOld) this._SetTimeSubBegin(timeNew);  // End se nemění, Begin se zvyšuje (délka se snižuje)
                        else this._SetTimeSubBooth(timeNew);                        // Mění se Begin i End, délka se snižuje
                    }
                }
            }
            this._Time = timeNew;
            this.Graph.Invalidate(GTimeGraph.InvalidateItems.AllGroups);
        }
        /// <summary>
        /// Změna intervalu: posun času všech prvků o stejnou vzdálenost
        /// </summary>
        /// <param name="timeNew"></param>
        private void _SetTimeShift(TimeRange timeNew)
        {
            TimeSpan shift = timeNew.Begin.Value - this.Time.Begin.Value;      // Hodnota posunu, může být kladný nebo záporný
            this._SetTimeShiftBy(shift);                                       //  aplikuje se na všechny časy
        }
        /// <summary>
        /// Změna intervalu: prodloužení celkové doby - zvýšení hodnoty End, beze změny Begin
        /// </summary>
        /// <param name="timeNew"></param>
        private void _SetTimeAddEnd(TimeRange timeNew)
        {
            TimeSpan shift = timeNew.End.Value - this.Time.End.Value;          // Hodnota posunu hodnoty End, shift bude kladný
            int last = this._Items.Length - 1;
            var item = this._Items[last];                                      //  aplikuje se pouze na poslední prvek
            item.Time = item.Time.ShiftByTime(shift);
        }
        /// <summary>
        /// Změna intervalu: prodloužení celkové doby - snížení hodnoty Begin, beze změny End
        /// </summary>
        /// <param name="timeNew"></param>
        private void _SetTimeAddBegin(TimeRange timeNew)
        {
            TimeSpan shift = timeNew.Begin.Value - this.Time.Begin.Value;      // Hodnota posunu hodnoty Begin, shift bude záporný
            var item = this._Items[0];                                         //  aplikuje se pouze na první prvek
            item.Time = item.Time.ShiftByTime(shift);
        }
        /// <summary>
        /// Změna intervalu: prodloužení celkové doby - změna hodnoty Begin i End
        /// </summary>
        /// <param name="timeNew"></param>
        private void _SetTimeAddBooth(TimeRange timeNew)
        {
            TimeSpan shift = timeNew.Begin.Value - this.Time.Begin.Value;      // Hodnota posunu hodnoty Begin, shift může být kladný nebo záporný
            this._SetTimeShiftBy(shift);                                       //  aplikuje se na všechny časy
            int last = this._Items.Length - 1;                                 //  a poslední prvek bude mít upravený i End
            var item = this._Items[last];
            item.Time = TimeRange.CreateFromSizeEnd(item.Time.Size.Value, timeNew.End.Value);
        }
        /// <summary>
        /// Změna intervalu: zkrácení celkové doby - snížení hodnoty End, beze změny Begin
        /// </summary>
        /// <param name="timeNew"></param>
        private void _SetTimeSubEnd(TimeRange timeNew)
        {
            this._SetTimeShrinkTo(timeNew);
        }
        /// <summary>
        /// Změna intervalu: zkrácení celkové doby - zvýšení hodnoty Begin, beze změny End
        /// </summary>
        /// <param name="timeNew"></param>
        private void _SetTimeSubBegin(TimeRange timeNew)
        {
            this._SetTimeShrinkTo(timeNew);
        }
        /// <summary>
        /// Změna intervalu: zkrácení celkové doby - změna hodnoty Begin i End
        /// </summary>
        /// <param name="timeNew"></param>
        private void _SetTimeSubBooth(TimeRange timeNew)
        {
            TimeSpan shift = timeNew.Begin.Value - this.Time.Begin.Value;      // Nejprve všechny prvky posunu o daný posun
            this._SetTimeShiftBy(shift);                                       //  aplikuje se na všechny časy
            this._SetTimeShrinkTo(timeNew);                                    // a po posunutí je namačkám do daného času.
        }
        /// <summary>
        /// Změna intervalu: posun o daný časový úsek, pro všechny prvky
        /// </summary>
        /// <param name="shift"></param>
        private void _SetTimeShiftBy(TimeSpan shift)
        {
            foreach (var item in this._Items)
                item.Time = item.Time.ShiftByTime(shift);
        }

        /// <summary>
        /// Změna intervalu: zmenšení intervalu, zajištění že všechny prvky budou mít čas posunutý nebo zmenšený do daného nového intervalu
        /// </summary>
        /// <param name="timeNew"></param>
        private void _SetTimeShrinkTo(TimeRange timeNew)
        {
            foreach (var item in this._Items)
                item.Time = _SetTimeShrink(item.Time, timeNew);
        }
        /// <summary>
        /// Metoda vrátí daný čas prvku "itemTime" upravený tak, aby se vešel do mezního času "shrinkTime".
        /// Pokud možno čas prvku neupravuje (když se vejde beze změn).
        /// Když už jej musí upravit, snaží se zachovat jeho délku (když je menší než délka mezního času).
        /// Pokud ale délka času prvku je stejnáý nebo větší než mezní čas, pak vrací mezní čas.
        /// <para/>
        /// Povinný předpoklad: oba časy jsou plně vyplněné a nezáporné.
        /// </summary>
        /// <param name="itemTime"></param>
        /// <param name="shrinkTime"></param>
        /// <returns></returns>
        private static TimeRange _SetTimeShrink(TimeRange itemTime, TimeRange shrinkTime)
        {
            TimeSpan itemSize = itemTime.Size.Value;
            TimeSpan shrinkSize = shrinkTime.Size.Value;

            // Pokud shrinkTime mám menší (nebo stejnou) délku než čas prvku, pak nejde čas prvku upravit jinak než na čas shrink:
            if (shrinkSize.Ticks < itemSize.Ticks) return shrinkTime.Clone;

            DateTime itemBegin = itemTime.Begin.Value;
            DateTime itemEnd = itemTime.End.Value;
            DateTime shrinkBegin = shrinkTime.Begin.Value;
            DateTime shrinkEnd = shrinkTime.End.Value;

            if (itemBegin < shrinkBegin)
            {
                itemBegin = shrinkBegin;
                itemEnd = itemBegin + itemSize;
                if (itemEnd > shrinkEnd)
                    itemEnd = shrinkEnd;
            }
            else if (itemEnd > shrinkEnd)
            {
                itemEnd = shrinkEnd;
                itemBegin = itemEnd - itemSize;
                if (itemBegin < shrinkBegin)
                    itemBegin = shrinkBegin;
            }
            return new TimeRange(itemBegin, itemEnd);
        }
        #endregion
        #region ICloneable members
        object ICloneable.Clone()
        {
            GTimeGraphGroup gTimeGroup = new GTimeGraphGroup(this._ParentGraph);

            return gTimeGroup;

            // GTimeGraph clone = (GTimeGraph)this.MemberwiseClone();
            // return clone;
        }
        #endregion
        #region explicit ITimeGraphItem members
        ITimeInteractiveGraph ITimeGraphItem.OwnerGraph { get { return this._OwnerGraph; } set { this._OwnerGraph = value; } }
        bool ITimeGraphItem.IsVisible { get { return this.IsVisible; } set { this.IsVisible = value; } }
        int ITimeGraphItem.ItemId { get { return this._ItemId; } }
        int ITimeGraphItem.GroupId { get { return this._FirstItem.GroupId; } }
        TimeRange ITimeGraphItem.Time { get { return this._Time; } set { this._Time = value; } }
        int ITimeGraphItem.Layer { get { return this._FirstItem.Layer; } }
        int ITimeGraphItem.Level { get { return this._FirstItem.Level; } }
        int ITimeGraphItem.Order { get { return this._FirstItem.Order; } }
        float ITimeGraphItem.Height { get { return this.Height; } }
        string ITimeGraphItem.Text { get { return this._FirstItem.Text; } }
        string ITimeGraphItem.ToolTip { get { return this._FirstItem.ToolTip; } }
        Color? ITimeGraphItem.BackColor { get { return this.BackColor; } }
        Color? ITimeGraphItem.HatchColor { get { return this._FirstItem.HatchColor; } }
        Color? ITimeGraphItem.LineColor { get { return this.LineColor; } }
        System.Drawing.Drawing2D.HatchStyle? ITimeGraphItem.BackStyle { get { return this._FirstItem.BackStyle; } }
        float? ITimeGraphItem.RatioBegin { get { return null; } }
        float? ITimeGraphItem.RatioEnd { get { return null; } }
        Color? ITimeGraphItem.RatioBeginBackColor { get { return null; } }
        Color? ITimeGraphItem.RatioEndBackColor { get { return null; } }
        Color? ITimeGraphItem.RatioLineColor { get { return null; } }
        int? ITimeGraphItem.RatioLineWidth { get { return null; } }
        Image ITimeGraphItem.ImageBegin { get { return this.ImageBegin; } }
        Image ITimeGraphItem.ImageEnd { get { return this.ImageEnd; } }
        GraphItemBehaviorMode ITimeGraphItem.BehaviorMode { get { return this.BehaviorMode; } }
        GTimeGraphItem ITimeGraphItem.GControl { get { return this.GControl; } set { this.GControl = value; } }
        void ITimeGraphItem.Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode) { this.Draw(e, boundsAbsolute, drawMode); }
        #endregion
    }
}
