﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;
using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.Components.Graphs
{
    /// <summary>
    /// <see cref="TimeGraphGroup"/> : skupina jednoho nebo více prvků ITimeGraphItem, obsahující sumární čas Time a Max(Height) z položek
    /// </summary>
    public class TimeGraphGroup : ITimeGraphItem
    {
        #region Konstruktory; řízená tvorba GTimeGraphItem pro GTimeGraphGroup i pro jednotlivé položky ITimeGraphItem
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        private TimeGraphGroup(TimeGraph parent)
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
        /// <param name="acceptZeroTime">Požadavek true = jako platný prvek lze akceptovat i prvek, jehož čas End == čas Begin; false = čas End musí být větší než Begin.</param>
        /// <param name="item"></param>
        public TimeGraphGroup(TimeGraph parent, bool acceptZeroTime, ITimeGraphItem item)
            : this(parent)
        {
            this._PrepareGControlItem(item);                              // Připravím GUI prvek pro jednotlivý prvek grafu za item, jeho parentem bude grafický prvek této grupy (=this.ControlBuffered)
            this._FirstItem = item;
            this._Items = new ITimeGraphItem[] { item };
            bool canResize = item.BehaviorMode.HasFlag(GraphItemBehaviorMode.ResizeTime);
            if (!acceptZeroTime && (item.ImageBegin != null || item.ImageEnd != null)) acceptZeroTime = true;
            this._Store(item.Time.Begin, item.Time.End, acceptZeroTime, item.Height, canResize);
        }
        /// <summary>
        /// Konstruktor s předáním skupiny položek, s výpočtem jejich sumárního časového intervalu a výšky
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="acceptZeroTime">Požadavek true = jako platný prvek lze akceptovat i prvek, jehož čas End == čas Begin; false = čas End musí být větší než Begin.</param>
        /// <param name="items"></param>
        public TimeGraphGroup(TimeGraph parent, bool acceptZeroTime, IEnumerable<ITimeGraphItem> items)
            : this(parent)
        {
            // Vstupní prvky (items) mohou být (ze vstupních dat) nesetříděné podle jejich času, 
            //  ale pro korektní fungování grupy musí být prvky v jedné grupě setříděny.
            List<ITimeGraphItem> itemList = items.ToList();
            if (itemList.Count > 1)
                itemList.Sort((a, b) => TimeRange.CompareByBeginAsc(a.Time, b.Time));
            this._Items = itemList.ToArray();
            DateTime? begin = null;
            DateTime? end = null;
            float? height = null;
            bool canResize = false;
            foreach (ITimeGraphItem item in this.Items)
            {
                this._PrepareGControlItem(item);                          // Připravím GUI prvek pro jednotlivý prvek grafu za item, jeho parentem bude grafický prvek této grupy (=this.ControlBuffered)
                if (this._FirstItem == null) this._FirstItem = item;
                if (item.Height.HasValue && (!height.HasValue || item.Height.Value > height.Value)) height = item.Height;
                if (item.Time.Begin.HasValue && (!begin.HasValue || item.Time.Begin.Value < begin.Value)) begin = item.Time.Begin;
                if (item.Time.End.HasValue && (!end.HasValue || item.Time.End.Value > end.Value)) end = item.Time.End;
                if (!canResize && item.BehaviorMode.HasFlag(GraphItemBehaviorMode.ResizeTime)) canResize = true;
                if (!acceptZeroTime && (item.ImageBegin != null || item.ImageEnd != null)) acceptZeroTime = true;
                item.OwnerGraph = parent;
            }
            this._Store(begin, end, acceptZeroTime, height, canResize);
        }
        /// <summary>
        /// Metoda vytvoří grafický control třídy <see cref="TimeGraphItem"/> (<see cref="ITimeGraphItem.VisualControl"/>) pro this grupu.
        /// </summary>
        /// <param name="parent">Parent prvku, graf (neboť this je <see cref="TimeGraphGroup"/>, pak jeho přímý Parent je <see cref="TimeGraph"/>).</param>
        private void _PrepareGControlGroup(TimeGraph parent)
        {
            if (this.ControlBuffered != null) return;
            this.ControlBuffered = new TimeGraphItem(this, parent, this, GraphControlPosition.Group);          // GUI prvek (GTimeGraphItem) dostává data (=this) a dostává vizuálního parenta (parent)
        }
        /// <summary>
        /// Metoda vytvoří grafický control třídy <see cref="TimeGraphItem"/> (<see cref="ITimeGraphItem.VisualControl"/>) pro daný datový grafický prvek (item).
        /// </summary>
        /// <param name="item">Datový prvek grafu</param>
        private void _PrepareGControlItem(ITimeGraphItem item)
        {
            item.VisualControl = new TimeGraphItem(item, this.ControlBuffered, this, GraphControlPosition.Item);    // GUI prvek (GTimeGraphItem) dostává data (=item) a dostává vizuálního parenta (this.ControlBuffered)
            this.ControlBuffered.AddGraphItem(item.VisualControl);                         // Náš hlavní GUI prvek (ten od grupy) si přidá další svůj Child prvek
        }
        /// <summary>
        /// Zadané údaje vloží do <see cref="Time"/> a <see cref="Height"/>, vypočte hodnotu <see cref="IsValidRealTime"/>.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="acceptZeroTime">Požadavek true = jako platný prvek lze akceptovat i prvek, jehož čas End == čas Begin; false = čas End musí být větší než Begin.</param>
        /// <param name="height"></param>
        /// <param name="canResize"></param>
        private void _Store(DateTime? begin, DateTime? end, bool acceptZeroTime, float? height, bool canResize)
        {
            this._Time = new TimeRange(begin, end);
            this._Height = height;
            this._IsValidRealTime = ((!height.HasValue || (height.HasValue && height.Value > 0f)) && (begin.HasValue && end.HasValue && (acceptZeroTime ? end.Value >= begin.Value : end.Value > begin.Value)));
            this.ControlBuffered.CanResize = canResize;
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
        private TimeGraph _ParentGraph;
        #endregion
        #region Privátní proměnné
        private ITimeInteractiveGraph _OwnerGraph;
        private int _ItemId;
        private ITimeGraphItem _FirstItem;
        private ITimeGraphItem[] _Items;
        private float? _Height;
        private TimeRange _Time;
        private bool _IsValidRealTime;
        #endregion
        #region Souřadnice prvku na ose X i Y (logické, virtuální, reálné)
        /// <summary>
        /// Metoda připraví souřadnice this grupy na ose X, včetně jejích grafických items
        /// </summary>
        /// <param name="timeConvert"></param>
        /// <param name="offsetX"></param>
        /// <param name="minWidth"></param>
        /// <param name="isSubMinWidth">Out: šířka prvku by byla menší než <paramref name="minWidth"/> a byla tedy navýšena</param>
        /// <param name="itemsCount"></param>
        public void PrepareCoordinateX(Func<TimeRange, DoubleRange> timeConvert, int offsetX, double minWidth, out bool isSubMinWidth, ref int itemsCount)
        {
            Int32Range coordX;
            DoubleRange groupX = timeConvert(this.Time);             // Vrací souřadnici X v koordinátech grafu
            isSubMinWidth = (groupX.Size < minWidth);
            coordX = ResizeRangeToMinWidth(groupX, minWidth);        // Zaokrouhlím Begin i End
            int groupB = coordX.Begin;                               // Offset z absolutní do relativní souřadnice pro jednotlivé prvky
            if (offsetX != 0d) coordX = coordX.ShiftBy(offsetX);     // Posun celé grupy vlivem offsetu grafu vůči časové ose
            this.ControlBuffered.CoordinateX = coordX;                      // Relativní souřadnice grupy v rámci grafu
            foreach (ITimeGraphItem item in this.Items)
            {
                itemsCount++;
                DoubleRange itemX = timeConvert(item.Time);          // Vrací souřadnici X v koordinátech grafu
                coordX = ResizeRangeToMinWidth(itemX, minWidth);     // Zaokrouhlím Begin i End
                item.VisualControl.CoordinateX = coordX.ShiftBy(-groupB); // Posunu prvek na relativní souřadnici vzhledem ke grupě
            }
            this.InvalidateBounds();
        }
        /// <summary>
        /// Metoda zajistí velikost MinWidth a vrátí zaokrouhlený interval
        /// </summary>
        /// <param name="range"></param>
        /// <param name="minWidth"></param>
        /// <returns></returns>
        protected static Int32Range ResizeRangeToMinWidth(DoubleRange range, double minWidth)
        {
            if (minWidth > 0 && range.Size < minWidth)
                range = range.ZoomToSize(range.Center, minWidth);
            return range.Int32RoundEnd;
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
            if (this.ControlBuffered.CoordinateX == null)
            {
                this.Graph.CheckValidCoordinateX();
                return;
            }
            this.ControlBuffered.Bounds = Int32Range.GetRectangle(this.ControlBuffered.CoordinateX, groupY);

            // Child prvky mají svoje souřadnice (Bounds) relativní k this prvku (který je jejich parentem), proto mají Y souřadnici { 0 až this.Y.Size }:
            Int32Range itemY = new Int32Range(0, groupY.Size);
            foreach (ITimeGraphItem item in this.Items)
                item.VisualControl.Bounds = Int32Range.GetRectangle(item.VisualControl.CoordinateX, itemY);

            this._IsValidBounds = true;
        }
        /// <summary>
        /// Souřadnice na ose Y, vizuální (tzn. v pixelech)
        /// </summary>
        protected Int32Range CoordinateYVisual
        {
            get
            {
                Int32Range yReal = this.CoordinateYReal;
                if (yReal == null) return new Int32Range();
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

            this.ControlBuffered.InvalidateBounds();
            foreach (ITimeGraphItem item in this.Items)
                item.VisualControl.InvalidateBounds();
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
        public TimeGraph Graph { get { return this._ParentGraph; } }
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
        /// <see cref="Height"/> * <see cref="TimeGraph.CurrentGraphProperties"/>: <see cref="TimeGraphProperties.OneLineHeight"/> nebo <see cref="TimeGraphProperties.OneLinePartialHeight"/>, 
        /// podle toho zda graf obsahuje jen celočíselné výšky, nebo i zlomkové výšky.
        /// Prvky s výškou 0 a menší nebudou vykresleny.
        /// </summary>
        public float? Height { get { return this._Height; } }
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
        /// Barva textu (písma).
        /// </summary>
        public Color? TextColor { get { return this._FirstItem.TextColor; } }
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
        /// Efekt pro vykreslení prvku, pokud je Editovatelný
        /// </summary>
        public TimeGraphElementBackEffectStyle BackEffectEditable { get { return this._FirstItem.BackEffectEditable; } }
        /// <summary>
        /// Efekt pro vykreslení prvku, pokud je Needitovatelný
        /// </summary>
        public TimeGraphElementBackEffectStyle BackEffectNonEditable { get { return this._FirstItem.BackEffectNonEditable; } }
        /// <summary>
        /// true pokud this grupu je možno resizovat.
        /// Hodnota je platná až po doběhnutí konstruktoru, nikoli v době tvorby controlu <see cref="ControlBuffered"/>.
        /// </summary>
        public bool CanResize { get { return this.BehaviorMode.HasFlag(GraphItemBehaviorMode.ResizeTime); } }
        /// <summary>
        /// Obsahuje true, když tento prvek je vhodné zobrazovat (má kladný čas i výšku).
        /// Výška se považuje za vhodnou i tehdy, když je NULL (protože pak výška prvku je dána výškou celého grafu).
        /// </summary>
        internal bool IsValidRealTime { get { return this._IsValidRealTime; } }
        /// <summary>
        /// Viditelnost prvku z důvodu velikosti a časové osy
        /// </summary>
        internal TimeGraphElementSizeState CurrentSizeState { get; set; }
        /// <summary>
        /// Obsahuje průhlednost pro vykreslení tohoto prvku do vrstvy Interactive.
        /// Vkládá se sem v procesu Drag and Drop, čte se v procesu Draw na prvku třídy <see cref="TimeGraphItem"/>
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
        /// Zarovnání textu v prvku grafu
        /// </summary>
        internal ExtendedContentAlignment TextPosition { get { return this._FirstItem.TextPosition; } }
        /// <summary>
        /// Vrací true, pokud pro daný prvek mohou být vyhledávány a zobrazovány Linky, podle jeho <see cref="BehaviorMode"/>.
        /// Rozhoduje o tom Group, protože jednotlivé Items nemají svoje privátní Linky.
        /// </summary>
        internal bool IsShowLinks { get { return this.BehaviorMode.HasAnyFlag(GraphItemBehaviorMode.ShowLinks); } }
        /// <summary>
        /// Vizuální prvek, který v sobě zahrnuje jak podporu pro vykreslování, tak podporu interaktivity.
        /// A přitom to nevyžaduje od třídy, která fyzicky implementuje <see cref="ITimeGraphItem"/>.
        /// Aplikační kód (respektive implementační objekt <see cref="ITimeGraphItem"/>) se o tuto property nemusí starat, řídící mechanismus sem vloží v případě potřeby new instanci.
        /// Implementátor pouze poskytuje úložiště pro tuto instanci.
        /// </summary>
        public TimeGraphItem ControlBuffered { get; set; }
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
            this.ControlBuffered.DrawItem(e, boundsAbsolute, drawMode);
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

            // 1. Obrázky:
            // Pokud máme hodně prostoru, tak vypisovaný text posuneme mimo ikony (true = pokud chceme zmenšit "boundsAbsolute" o prostor ikony / false = neměnit)
            bool cropBoundsAbsolute = (boundsAbsolute.Width > 65);
            this.ControlBuffered.DrawImages(e, ref boundsAbsolute, boundsVisibleAbsolute, drawMode, cropBoundsAbsolute);

            // 2. Textový popisek do prvku:
            if (!this.DrawTextInCurrentState) return;

            if (e.DrawLayer == GInteractiveDrawLayer.Standard)
                boundsVisibleAbsolute = e.GetClip(boundsAbsolute);

            FontInfo fontInfo = FontInfo.CaptionBold;
            ExtendedContentAlignment textPosition = this.TextPosition;
            string text = this.GetCaption(e, boundsAbsolute, boundsVisibleAbsolute, fontInfo, textPosition);

            if (String.IsNullOrEmpty(text)) return;
            this.ControlBuffered.DrawText(e, boundsVisibleAbsolute, text, fontInfo, textPosition);
        }
        /// <summary>
        /// Metoda vrátí text, který má být zobrazen v grafickém prvku
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="boundsVisibleAbsolute"></param>
        /// <param name="fontInfo"></param>
        /// <param name="textPosition"></param>
        /// <returns></returns>
        protected string GetCaption(GInteractiveDrawArgs e, Rectangle boundsAbsolute, Rectangle boundsVisibleAbsolute, FontInfo fontInfo, ExtendedContentAlignment textPosition)
        {
            // 1. Pokud je text v prvku uveden explicitně, pak jej použijeme:
            string text = this._FirstItem.Text;
            if (text != null) return text;

            // 2. Text pro aktuální velikost je pravděpodobně uložený v paměti (od posledně):
            Size size = boundsAbsolute.Size;
            text = this.GetCaptionForSize(size);
            if (text != null) return text;

            // 3. Pro danou velikost ještě text není zapamatován => Získáme text Caption z datového zdroje grafu a zapamatujeme si ho:
            CreateTextArgs args = new CreateTextArgs(this._ParentGraph, e, fontInfo, this, this._FirstItem, GraphControlPosition.Group, boundsAbsolute, boundsVisibleAbsolute);
            text = this._ParentGraph.GraphItemGetCaptionText(args);
            this.SetCaptionForSize(size, text);

            return text;
        }
        /// <summary>
        /// Vrátí danou barvu, s modifikovanou průhledností Opacity (<see cref="Color.A"/>).
        /// Zadaná Opacity je respektována, ale pokud celý graf má deklarovaou svoji průhlednost v <see cref="TimeGraph.GraphOpacity"/>, pak je akceptována rovněž.
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
        /// Zadaná Opacity je respektována, ale pokud celý graf má deklarovaou svoji průhlednost v <see cref="TimeGraph.GraphOpacity"/>, pak je akceptována rovněž.
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
            {   // Běžný stav prvku, kdy tento pvek není přetahován jinam:
                return this._GetColorWithOpacityStandard(baseColor, forGroup, forBackColor, null);
            }
            else
            {   // Drag and Drop tohoto prvku, kdy prvek je přemisťován jinam:
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
            if (this.AnyItemIsActiveTarget)
                baseColor = baseColor.Morph(Skin.Modifiers.BackColorDropTargetItem);
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
                GInteractiveState state = this.ControlBuffered.InteractiveState;
                bool isActive = this.ControlBuffered.IsSelected || this.ControlBuffered.IsFramed || this.ControlBuffered.IsActivated || this.IsDragged;
                return TimeGraph.IsCaptionVisible(mode, state, isActive);
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
        /// Porovná dvě instance <see cref="TimeGraphGroup"/> podle <see cref="ITimeGraphItem.Order"/> ASC, <see cref="ITimeGraphItem.Time"/> ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareOrderTimeAsc(TimeGraphGroup a, TimeGraphGroup b)
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
        internal bool IsSelectable { get { return this.BehaviorMode.HasAnyFlag(GraphItemBehaviorMode.AnySelectable); } }
        /// <summary>
        /// Vrací true, pokud daný prvek může být přemísťován.
        /// Rozhoduje o tom Group, protože jednotlivé Items přemisťovat nelze.
        /// </summary>
        internal bool IsDragEnabled { get { return this.BehaviorMode.HasAnyFlag(GraphItemBehaviorMode.AnyMove); } }
        /// <summary>
        /// Obsahuje true, pokud this grupa je nyní přemisťována akcí DragMove.
        /// </summary>
        internal bool IsDragged { get { return this.ControlBuffered.InteractiveState.HasFlag(GInteractiveState.FlagDrag); } }
        /// <summary>
        /// Obsahuje true, pokud kterýkoli z mých prvků nebo já jsme ActiveTarget
        /// </summary>
        internal bool AnyItemIsActiveTarget { get { return this.ControlBuffered.Is.ActiveTarget || this._Items.Any(i => i.VisualControl.Is.ActiveTarget); } }
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
            this.Graph.Invalidate(TimeGraph.InvalidateItems.AllGroups);
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
            TimeGraphGroup gTimeGroup = new TimeGraphGroup(this._ParentGraph);

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
        float? ITimeGraphItem.Height { get { return this.Height; } }
        string ITimeGraphItem.Text { get { return this._FirstItem.Text; } }
        string ITimeGraphItem.ToolTip { get { return this._FirstItem.ToolTip; } }
        Color? ITimeGraphItem.BackColor { get { return this.BackColor; } }
        Color? ITimeGraphItem.TextColor { get { return this.TextColor; } }
        Color? ITimeGraphItem.HatchColor { get { return this._FirstItem.HatchColor; } }
        Color? ITimeGraphItem.LineColor { get { return this.LineColor; } }
        System.Drawing.Drawing2D.HatchStyle? ITimeGraphItem.BackStyle { get { return this._FirstItem.BackStyle; } }
        float? ITimeGraphItem.RatioBegin { get { return null; } }
        float? ITimeGraphItem.RatioEnd { get { return null; } }
        TimeGraphElementRatioStyle ITimeGraphItem.RatioStyle { get { return TimeGraphElementRatioStyle.None; } }
        Color? ITimeGraphItem.RatioBeginBackColor { get { return null; } }
        Color? ITimeGraphItem.RatioEndBackColor { get { return null; } }
        Color? ITimeGraphItem.RatioLineColor { get { return null; } }
        int? ITimeGraphItem.RatioLineWidth { get { return null; } }
        Image ITimeGraphItem.ImageBegin { get { return this.ImageBegin; } }
        Image ITimeGraphItem.ImageEnd { get { return this.ImageEnd; } }
        GraphItemBehaviorMode ITimeGraphItem.BehaviorMode { get { return this.BehaviorMode; } }
        ExtendedContentAlignment ITimeGraphItem.TextPosition { get { return this.TextPosition; } }
        TimeGraphElementBackEffectStyle ITimeGraphItem.BackEffectEditable { get { return this.BackEffectEditable; } }
        TimeGraphElementBackEffectStyle ITimeGraphItem.BackEffectNonEditable { get { return this.BackEffectNonEditable; } }
        TimeGraphItem ITimeGraphItem.VisualControl { get { return this.ControlBuffered; } set { this.ControlBuffered = value; } }
        void ITimeGraphItem.Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode) { this.Draw(e, boundsAbsolute, drawMode); }
        #endregion
    }
}
