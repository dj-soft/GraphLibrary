using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Base.WorkScheduler;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Components.Graph
{
    /// <summary>
    /// GTimeGraphItem : vizuální a interaktivní control, který se vkládá do implementace ITimeGraphItem.
    /// Tento prvek je zobrazován ve dvou režimech: buď jako přímý child prvek vizuálního grafu, pak reprezentuje grupu prvků (i kdyby grupa měla jen jeden prvek),
    /// anebo jako child prvek této grupy, pak reprezentuje jeden konkrétní prvek grafu (GraphItem).
    /// </summary>
    public class GTimeGraphItem : InteractiveDragObject, IResizeObject, IOwnerProperty<ITimeGraphItem>
    {
        #region Konstruktor, privátní proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="data">Datový prvek grafu</param>
        /// <param name="interactiveParent">Interaktivní vizuální parent prvku, GUI container (buď graf pro grupu, nebo grupa pro item)</param>
        /// <param name="group">Data grupy, do níž vytvářený prvek bude patřit (přímo nebo nepřímo)</param>
        /// <param name="position">Pozice vytvářeného prvku</param>
        public GTimeGraphItem(ITimeGraphItem data, IInteractiveParent interactiveParent, GTimeGraphGroup group, GGraphControlPosition position)
            : base()
        {
            this._Owner = data;
            this.Parent = interactiveParent;
            this._Group = group;
            this._Position = position;
            this.Is.GetSelectable = this._GetSelectable;
            this.Is.GetMouseDragMove = this._GetMouseDragMove;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Position: " + this._Position.ToString() + "; GroupId: " + this.Group.GroupId + 
                ((this.Position == GGraphControlPosition.Item) ? ("; ItemId: " + this.Item.ItemId.ToString()) : "") +
                "; " + base.ToString();
        }
        /// <summary>
        /// Vlastník tohoto grafického prvku = datový prvek grafu
        /// </summary>
        ITimeGraphItem IOwnerProperty<ITimeGraphItem>.Owner { get { return this._Owner; } set { this._Owner = value; } }
        private ITimeGraphItem _Owner;
        /// <summary>
        /// Grupa, která slouží jako vazba na globální data
        /// </summary>
        private GTimeGraphGroup _Group;
        /// <summary>
        /// Pozice tohoto prvku
        /// </summary>
        private GGraphControlPosition _Position;
        #endregion
        #region Souřadnice
        /// <summary>
        /// Souřadnice tohoto prvku v rámci jeho Parenta.
        /// Přepočet na absolutní souřadnice provádí (extension) metoda IInteractiveItem.GetAbsoluteVisibleBounds().
        /// Vložení hodnoty do této property způsobí veškeré zpracování akcí (<see cref="ProcessAction.ChangeAll"/>).
        /// Vložení souřadnice nastaví i její platnost: <see cref="BoundsIsValid"/> = true.
        /// </summary>
        public override Rectangle Bounds
        {
            get { this.CheckBoundsValid(); return base.Bounds; }
            set { this.BoundsIsValid = true; base.Bounds = value; }
        }
        /// <summary>
        /// Metoda zajistí platnost souřadnic v <see cref="Bounds"/>
        /// </summary>
        protected void CheckBoundsValid()
        {
            this.Graph.CheckValid();
            if (!this.BoundsIsValid)
            {
                this.Group.CalculateBounds();
                this.BoundsIsValid = true;
            }
        }
        /// <summary>
        /// Metoda invaliduje souřadnice <see cref="Bounds"/>
        /// </summary>
        internal void InvalidateBounds()
        {
            this.BoundsIsValid = false;
        }
        /// <summary>
        /// true = souřadnice prvku <see cref="Bounds"/> jsou platné
        /// </summary>
        internal bool BoundsIsValid { get; private set; }
        #endregion
        #region Veřejná data
        /// <summary>
        /// Graf, do něhož tento vizuální interaktivní prvek patří.
        /// Může se lišit od našeho vizuálního parenta, protože this prvek může být Child prvkem Grupy, a ta je Child prvkem Grafu.
        /// Nicméně přes zdejší Grupu je vždy možno najít Graf, v němž je this prvek zobrazován.
        /// </summary>
        internal GTimeGraph Graph { get { return this._Group.Graph; } }
        /// <summary>
        /// Skupina, do které tento vizuální interaktivní prvek patří.
        /// </summary>
        internal GTimeGraphGroup Group { get { return this._Group; } }
        /// <summary>
        /// Vlastní datový prvek grafu, pro který je vytvořen this grafický prvek
        /// </summary>
        internal ITimeGraphItem Item { get { return this._Owner; } }
        /// <summary>
        /// Časový interval
        /// </summary>
        internal TimeRange Time
        {
            get
            {
                switch (this.Position)
                {
                    case GGraphControlPosition.Group: return this.Group.Time;
                    case GGraphControlPosition.Item: return this.Item.Time;
                }
                return null;
            }
            set
            {
                switch (this.Position)
                {
                    case GGraphControlPosition.Group:
                        this.Group.SetTime(value);
                        break;
                    case GGraphControlPosition.Item:
                        break;
                }
            }
        }
        /// <summary>
        /// Souřadnice na ose X. Jednotkou jsou pixely.
        /// Tato osa je společná jak pro virtuální, tak pro reálné souřadnice.
        /// Hodnota 0 odpovídá prvnímu viditelnému pixelu vlevo.
        /// </summary>
        public Int32Range CoordinateX { get; set; }
        /// <summary>
        /// Barva pozadí tohoto prvku: tento override vrací průhlednou barvu. Základní metoda nemá kreslit pozadí povinně.
        /// Více viz <see cref="ItemBackColor"/>.
        /// </summary>
        public override Color BackColorDefault { get { return Color.Transparent; } }
        /// <summary>
        /// Pozice tohoto prvku (Group / Item)
        /// </summary>
        public GGraphControlPosition Position { get { return this._Position; } }
        /// <summary>
        /// Metoda vrátí datové prvky, pro které je vytvořen tento vizuální prvek.
        /// Pokud this prvek je typu <see cref="Position"/> == <see cref="GGraphControlPosition.Group"/>, pak vrátí datové prvky ze všech svých prvků.
        /// Pokud this prvek je typu <see cref="Position"/> == <see cref="GGraphControlPosition.Item"/>, pak se řídí parametrem wholeGroup:
        /// a) true = najde všechny prvky své skupiny; b) false = vrátí jen svůj datový prvek.
        /// </summary>
        /// <param name="wholeGroup">Vrátit všechny prvky i tehdy, když daný prvek reprezentuje jednu položku?</param>
        /// <returns></returns>
        public ITimeGraphItem[] GetDataItems(bool wholeGroup)
        {
            switch (this.Position)
            {
                case GGraphControlPosition.Group:
                    return this.Group.Items;
                case GGraphControlPosition.Item:
                    if (wholeGroup)
                        return this.Group.Items;
                    return new ITimeGraphItem[] { this._Owner };
            }
            return null;
        }
        #endregion
        #region Čtení dat pro kreslení
        /// <summary>
        /// Barva pozadí prvku grafu, načtená z <see cref="ITimeGraphItem.BackColor"/>. 
        /// Může být null.
        /// </summary>
        protected Color? ItemBackColor { get { return this._Owner.BackColor; } }
        /// <summary>
        /// Barva textu (písma).
        /// </summary>
        protected Color? ItemTextColor { get { return this._Owner.TextColor; } }
        /// <summary>
        /// Barva šrafování pozadí prvku grafu, načtená z <see cref="ITimeGraphItem.HatchColor"/>. 
        /// Může být null.
        /// </summary>
        protected Color? ItemHatchColor { get { return this._Owner.HatchColor; } }
        /// <summary>
        /// Barva linky prvku grafu, načtená z <see cref="ITimeGraphItem.LineColor"/>. 
        /// Může být null.
        /// </summary>
        protected Color? ItemLineColor { get { return this._Owner.LineColor; } }
        /// <summary>
        /// Barva pozadí části Ratio, čas Begin, načtená z <see cref="ITimeGraphItem.RatioBeginBackColor"/>. 
        /// Může být null.
        /// </summary>
        protected Color? RatioBeginBackColor { get { return this._Owner.RatioBeginBackColor; } }
        /// <summary>
        /// Barva pozadí části Ratio, čas End, načtená z <see cref="ITimeGraphItem.RatioEndBackColor"/>. 
        /// Může být null.
        /// </summary>
        protected Color? RatioEndBackColor { get { return this._Owner.RatioEndBackColor; } }
        /// <summary>
        /// Barva linie části Ratio, načtená z <see cref="ITimeGraphItem.RatioLineColor"/>. 
        /// Může být null.
        /// </summary>
        protected Color? RatioLineColor { get { return this._Owner.RatioLineColor; } }
        #endregion
        #region Child prvky: přidávání, kolekce
        /// <summary>
        /// Child prvky, může být null (pro <see cref="GControl"/> v roli controlu jednotlivého <see cref="ITimeGraphItem"/>), 
        /// nebo může obsahovat vnořené prvky (pro <see cref="GControl"/> v roli controlu skupiny <see cref="GTimeGraphGroup"/>).
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { return this._Childs; } }
        /// <summary>
        /// Pole Child prvků: obsahuje jednak prvky z <see cref="_GraphItems"/>, a k tomu prvky pro Resize z <see cref="_ResizeControl"/>
        /// </summary>
        private List<IInteractiveItem> _Childs
        {
            get
            {
                if (this.__Childs == null)
                {   // CheckValid:
                    this.__Childs = new List<IInteractiveItem>();
                    // Pole grafických prvků:
                    if (this._GraphItems != null && this._GraphItems.Count > 0)
                        this.__Childs.AddRange(this._GraphItems);
                    // Pole prvků Resize:
                    if (this._CanResize && this._ResizeControl != null && this._ResizeControl.HasChilds)
                        this.__Childs.AddRange(this._ResizeControl.Childs);
                }
                return this.__Childs;
            }
        }
        /// <summary>
        /// Invaliduje pole Childs
        /// </summary>
        private void _InvalidateChilds()
        {
            this.__Childs = null;
        }
        private List<IInteractiveItem> __Childs;
        /// <summary>
        /// Přidá vnořený objekt
        /// </summary>
        /// <param name="graphItem"></param>
        public void AddGraphItem(GTimeGraphItem graphItem)
        {
            if (this._GraphItems == null)
                this._GraphItems = new List<GTimeGraphItem>();
            this._GraphItems.Add(graphItem);
            this._InvalidateChilds();
        }
        private List<GTimeGraphItem> _GraphItems;
        #endregion
        #region Interaktivita
        /// <summary>
        /// Při každé změně interaktivity
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChanged(e);
            this.GroupInteractiveState = e.TargetState;
        }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = MouseEnter
        /// Přípravu tooltipu je vhodnější provést v metodě <see cref="InteractiveObject.PrepareToolTip(GInteractiveChangeStateArgs)"/>, ta je volaná hned poté.
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseEnter(GInteractiveChangeStateArgs e)
        {
            this.PrepareLinksForMouseOver();
            base.AfterStateChangedMouseEnter(e);
        }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = MouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseLeave(GInteractiveChangeStateArgs e)
        {
            if (InteractiveLeaveThisGroup(e))
                this.RemoveLinksForMouseOver();
            base.AfterStateChangedMouseLeave(e);
        }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = LeftClick
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            // Zdejší třída GTimeGraphItem slouží jak pro práci s Group, tak pro Item (rozlišuje se to dle this.Position).
            // Prvek na pozici Group lze Selectovat, ten si to zařizuje sám (má Is.Selectable = true, takže pro něj se IsSelected řeší systémově).
            // Ale prvek na pozici Item nelze Selectovat, namísto toho budeme selectovat jeho Group prvek:
            if (this.Position == GGraphControlPosition.Item)
                this.Group.GControl.IsSelectedTryToggle();
            
            base.AfterStateChangedLeftClick(e);
        }
        /// <summary>
        /// Háček při změně hodnoty <see cref="InteractiveObject.IsSelected"/>
        /// </summary>
        /// <param name="args"></param>
        protected override void OnIsSelectedChanged(GPropertyChangeArgs<bool> args)
        {
            this.PrepareLinksForSelect(args.NewValue);
            base.OnIsSelectedChanged(args);
        }
        /// <summary>
        /// Metoda zajistí zpracování události RightCLick na grafickém prvku (data) na dané pozici (position).
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedRightClick(GInteractiveChangeStateArgs e)
        {
            ItemActionArgs args = new ItemActionArgs(e, this.Graph, this._Group, this._Owner, this._Position);
            this.Graph.GraphItemRightClick(args);
        }
        /// <summary>
        /// Metoda zajistí zpracování události LeftDoubleCLick na grafickém prvku (data) na dané pozici (position).
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedLeftDoubleClick(GInteractiveChangeStateArgs e)
        {
            ItemActionArgs args = new ItemActionArgs(e, this.Graph, this._Group, this._Owner, this._Position);
            this.Graph.GraphItemLeftDoubleClick(args);
        }
        /// <summary>
        /// Metoda zajistí zpracování události LeftLongCLick na grafickém prvku (data) na dané pozici (position).
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedLeftLongClick(GInteractiveChangeStateArgs e)
        {
            ItemActionArgs args = new ItemActionArgs(e, this.Graph, this._Group, this._Owner, this._Position);
            this.Graph.GraphItemLeftLongClick(args);
        }
        /// <summary>
        /// Metoda zajistí přípravu ToolTipu pro zdejší prvek (data) na dané pozici (position).
        /// </summary>
        /// <param name="e"></param>
        public override void PrepareToolTip(GInteractiveChangeStateArgs e)
        {
            TimeRange timeRange = this._Group.Time;
            string eol = Environment.NewLine;
            string timeText = "Začátek:\t" + timeRange.Begin.Value.ToUser() + eol + "Konec:\t" + timeRange.End.Value.ToUser() + eol;
            CreateToolTipArgs args = new CreateToolTipArgs(e, this.Graph, this._Group, timeText, this._Owner, this._Position);
            this.Graph.GraphItemPrepareToolTip(args);
        }
        /// <summary>
        /// Interaktivní stav grupy
        /// </summary>
        protected GInteractiveState GroupInteractiveState
        {
            get
            {
                if (this._Position == GGraphControlPosition.Group && this._GroupState.HasValue) return this._GroupState.Value;
                if (this._Group != null && this._Group.GControl._GroupState.HasValue) return this._Group.GControl._GroupState.Value;
                return this.InteractiveState;
            }
            set
            {
                if (this._Position == GGraphControlPosition.Group) this._GroupState = value;
                else if (this._Position == GGraphControlPosition.Item && this._Group != null) this._Group.GControl._GroupState = value;
            }
        }
        /// <summary>
        /// Stav tohoto prvku, pokud prvek je typu Group.
        /// Pro jiné prvky je null.
        /// Využívá se při vykreslování prvků.
        /// </summary>
        protected GInteractiveState? _GroupState;
        /// <summary>
        /// Hodnota Selectable :
        /// Selectovat lze jen Group prvky, nikoli Item.
        /// A to jen tehdy, pokud první prvek grupy má ve svém BehaviorMode hodnotu AnyMove nebo CanSelect.
        /// Get metoda použitá v <see cref="InteractiveProperties"/> objektu pro získání hodnoty Selectable.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool _GetSelectable(bool value)
        {
            return (this._Position == GGraphControlPosition.Group && this._Group.IsSelectable);
        }
        /// <summary>
        /// Metoda vrátí true, pokud událost MouseLeave opouští skutečně datovou skupinu grafu.
        /// Tzn.: pokud this je na pozici Item, a událost popisuje opouštění tohoto prvku Item, 
        /// ale přitom vstupujeme do prvku Group, který je naší grupou, pak reálně nejde o Leave této skupiny.
        /// Obdobně při Leave objektu na pozici Group a vstupu do objektu Item v téže skupině nejde o reálný Leave.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected bool InteractiveLeaveThisGroup(GInteractiveChangeStateArgs e)
        {
            if (e.ChangeState != GInteractiveChangeState.MouseLeave) return false;       // Pokud událost NENÍ MouseLeave, pak nejde o opuštění čehokoli
            bool isLeave = true;
            if (e.EnterItem != null && e.EnterItem is GTimeGraphItem)
            {
                GTimeGraphItem targetItem = e.EnterItem as GTimeGraphItem;     // Do tohoto objektu vstupujeme. Je to objekt patřící k nějakému grafu.
                switch (this.Position)
                {
                    case GGraphControlPosition.Group:
                        // Provádíme Leave z takového prvku, který reprezentuje Grupu:
                        //  pak zjistíme, zda cílový prvek (targetItem) není grafickým prvkem některého z mých Items:
                        isLeave = !this.Group.Items.Any(i => Object.ReferenceEquals(i.GControl, targetItem));
                        break;
                    case GGraphControlPosition.Item:
                        // Provádíme Leave z prvku, který je na pozici Item:
                        //  pak zjistíme, zda cílový prvek (targetItem) není grafickým prvkem patřící me vlastní grupě:
                        isLeave = !Object.ReferenceEquals(this.Group.GControl, targetItem);
                        break;
                }
            }
            return isLeave;
        }
        #endregion
        #region Přetahování (Drag and Drop) : týká se výhradně prvků typu Group!
        /// <summary>
        /// Vrací true, pokud daný prvek může být přemísťován.
        /// Get metoda použitá v <see cref="InteractiveProperties"/> objektu pro získání hodnoty MouseDragMove.
        /// </summary>
        private bool _GetMouseDragMove(bool value)
        {
            return (this._Position == GGraphControlPosition.Group && this._Group.IsDragEnabled);
        }
        /// <summary>
        /// Volá se na začátku procesu přesouvání, pro aktivní objekt.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="targetRelativeBounds"></param>
        protected override void DragThisStart(GDragActionArgs e, Rectangle targetRelativeBounds)
        {
            this.DragDropGroupCallSource(e, ref targetRelativeBounds);
            base.DragThisStart(e, targetRelativeBounds);
        }
        /// <summary>
        /// Volá se v procesu přesouvání, pro aktivní objekt.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="targetRelativeBounds"></param>
        protected override void DragThisOverPoint(GDragActionArgs e, Rectangle targetRelativeBounds)
        {
            this.DragThisRestrictByBehavior(e, ref targetRelativeBounds);
            this.DragDropGroupCallSource(e, ref targetRelativeBounds);
            base.DragThisOverPoint(e, targetRelativeBounds);
        }
        /// <summary>
        /// Volá se při ukončení Drag and Drop, při akci <see cref="DragActionType.DragThisDrop"/>, pro aktivní objekt (=ten který je přesouván).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="targetRelativeBounds"></param>
        protected override void DragThisDropToPoint(GDragActionArgs e, Rectangle targetRelativeBounds)
        {
            this.DragThisRestrictByBehavior(e, ref targetRelativeBounds);
            this.DragDropGroupCallSource(e, ref targetRelativeBounds);
            base.DragThisDropToPoint(e, targetRelativeBounds);
        }
        /// <summary>
        /// Je voláno po skončení přetahování, ať už skončilo OK (=Drop) nebo Escape (=Cancel).
        /// Účelem je provést úklid aplikačních dat po skončení přetahování.
        /// </summary>
        /// <param name="e"></param>
        protected override void DragThisEnd(GDragActionArgs e)
        {
            Rectangle targetRelativeBounds = Rectangle.Empty;
            this.DragDropGroupCallSource(e, ref targetRelativeBounds);
            base.DragThisEnd(e);
            this._Group.DragDropDrawInteractiveOpacity = null;
        }
        /// <summary>
        /// Metoda je volána před zahájením zpracování operace Drag and Drop, a slouží k omezení souřadnic přetahovaného prvku dle definice chování prvku.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="targetRelativeBounds"></param>
        protected void DragThisRestrictByBehavior(GDragActionArgs e, ref Rectangle targetRelativeBounds)
        {
            if (e.DragOriginRelativeBounds.HasValue)
            {
                GraphItemBehaviorMode behaviorMode = this.Group.BehaviorMode;
                if (!behaviorMode.HasAnyFlag(GraphItemBehaviorMode.MoveToAnotherTime))
                {   // Nejsou povoleny přesuny v čase:
                    targetRelativeBounds.X = e.DragOriginRelativeBounds.Value.X;
                }
                if (!behaviorMode.HasAnyFlag(GraphItemBehaviorMode.MoveToAnotherRow))
                {   // Nejsou povoleny přesuny na jiný řádek:
                    targetRelativeBounds.Y = e.DragOriginRelativeBounds.Value.Y;
                }
            }
        }
        /// <summary>
        /// Metoda je volána v procesu hledání cílového prvku pod souřadnicí myši v procesu Drag and Drop.
        /// Vrací souřadnici, odpovídající souřadnici myši Target, ale s omezením dle definice <see cref="GraphItemBehaviorMode"/> (=zákaz změny času nebo zákaz změny řádku)
        /// </summary>
        /// <param name="mouseDownAbsolutePoint"></param>
        /// <param name="targetAbsolutePoint"></param>
        protected Point DragThisRestrictByBehavior(Point mouseDownAbsolutePoint, Point targetAbsolutePoint)
        {
            Point resultPoint = targetAbsolutePoint;
            GraphItemBehaviorMode behaviorMode = this.Group.BehaviorMode;
            if (!behaviorMode.HasAnyFlag(GraphItemBehaviorMode.MoveToAnotherTime))
            {   // Nejsou povoleny přesuny v čase:
                resultPoint.X = mouseDownAbsolutePoint.X;
            }
            if (!behaviorMode.HasAnyFlag(GraphItemBehaviorMode.MoveToAnotherRow))
            {   // Nejsou povoleny přesuny na jiný řádek:
                resultPoint.Y = mouseDownAbsolutePoint.Y;
            }
            return resultPoint;
        }
        /// <summary>
        /// Klíčová metoda, která v procesu Drag and Drop určuje, zda, jak a kam se právě přesouvá grafický prvek.
        /// Metoda vyhledá odpovídající podkladový graf, respektive jiný podkladový prostor, a omezí případně pohyb prvku jen do vyhrazené oblasti.
        /// Současně si ukládá nalezený podkladový graf jako případný cíl...
        /// Metoda vrací true, pokud došlo ke změně souřadnic.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="targetRelativeBounds">Relativní souřadnice prvku (při akci Start = výchozí, při akci Move a Drop = cílová)
        /// Na vstupu: absolutní souřadnice místa, kam by uživatel rád přesunul prvek grafu;
        /// Na výstupu: </param>
        /// <returns></returns>
        protected void DragDropGroupCallSource(GDragActionArgs e, ref Rectangle targetRelativeBounds)
        {
            /* Popis chování:
    * SITUACE  : Přetahuji položku grafu, hledám prvek nad nímž se pohybuji (můj vlastní graf, jiný graf v rámci téže tabulky, jakýkoli jiný graf, jiný objekt?)
    * POTŘEBA  : Chci jednak omezit pohyb prvku jen na vhodná místa,
    *            a jednak chci určit, nad kterým prvkem se položka grafu pohybuje = to ovlivňuje chování i případně vykreslování
    * PODMÍNKY : a) pokud jsem nad svým vlastním grafem, pak je pohyb povolen
    *            b) pokud jsem nad tabulkou, pak je pohyb povolen jen nad oblastí dat, a to jen v rámci sloupce grafů
    *            c) určuji, zda jsem nad vlastním grafem, nebo nad vlastní tabulkou, a nad nějakým jiným grafem.
    *            Podle toho se následně rozhoduje, jak je pohyb přípustný.
    *            Po určení situace se volá datový zdroj pro určení důsledků
    * VÝSLEDKY : 
            */

            // Najdu prvek, nad nímž se aktuálně pohybuji:
            Point targetAbsolutePoint = this.DragThisRestrictByBehavior(e.MouseDownAbsolutePoint, e.MouseCurrentAbsolutePoint.Value);
            e.MouseCurrentAbsolutePoint = targetAbsolutePoint;

            // Sestavím argument (pro this prvek) a doplním do něj údaje o dalších prvcích:
            Rectangle targetAbsoluteBounds = e.BoundsInfo.GetAbsoluteBounds(targetRelativeBounds);
            ItemDragDropArgs args = new ItemDragDropArgs(e, this.Graph, this._Group, this._Owner, this._Position, targetAbsoluteBounds);
            args.ParentGraph = this.Graph;
            args.ParentTable = this.SearchForParent(typeof(Grid.GTable)) as Grid.GTable;
            args.SearchForTargets(targetAbsolutePoint);

            args.IsFinalised = true;

            // Předem připravím "defaultní" výsledky (ale datový zdroj, který se vyvolá v následujícím řádku) má plnou moc nastavit libovolné výsledky po svém:
            this.DragPrepareDefaultValidity(args);

            // Zavolám datový zdroj, ten vyhodnotí zda se může prvek posunout, nebo na jaké souřadnice se může posunout, a určí vzhled kreslení (disabled?):
            // On by mohl i zařídit "scrollování" v čase (osa X => TimeAxis) nebo v místě (osa Y => řádek, kam položku přesunout):
            this.Graph.DragDropGroupCallSource(args);

            // Převezmu výsledky (buď defaultní, nebo modifikované aplikační logikou):
            this.DragDropTargetItem = args.TargetDropItem;
            this._Group.DragDropDrawInteractiveOpacity = args.TargetValidityOpacity;
            if (args.BoundsFinal.HasValue)
            {
                targetRelativeBounds = args.BoundsFinal.Value;
                targetAbsoluteBounds = args.BoundsFinalAbsolute.Value;
            }
        }
        /// <summary>
        /// Metoda nastaví defaultní hodnotu platnosti podle toho, zda se cíl nachází nad grafem, anebo jak daleko se nachází od nejbližšího prostoru RowArea vlastní tabulky
        /// </summary>
        /// <param name="args"></param>
        protected void DragPrepareDefaultValidity(ItemDragDropArgs args)
        {
            ItemDragTargetType targetType = args.TargetType;

            if (targetType.HasFlag(ItemDragTargetType.OnSameGraph))
            {
                args.TargetDropItem = args.TargetGraph;
                args.TargetValidityRatio = 1.0m;
            }
            else if (targetType.HasFlag(ItemDragTargetType.OnOtherGraph))
            {
                args.TargetDropItem = args.TargetGraph;
                args.TargetValidityRatio = 1.0m;
            }
            else if (targetType.HasAnyFlag(ItemDragTargetType.OnTable))
            {
                args.TargetDropItem = null;
                Rectangle? homeBounds = args.HomeAbsoluteBounds;
                int distance = (homeBounds.HasValue ? homeBounds.Value.GetOuterDistance(args.MouseCurrentAbsolutePoint.Value) : 150);
                args.TargetValidityRatio = GetValidityForDistance(distance, 150m, 0.250m, 0.500m);
            }
            else
            {
                args.TargetDropItem = null;
                args.TargetValidityRatio = 0.250m;
            }
        }
        /// <summary>
        /// Vrací hodnotu ValidityRatio pro vzdálenost v pixelech
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="maxDistance"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        protected static decimal GetValidityForDistance(decimal distance, decimal maxDistance, decimal minValue, decimal maxValue)
        {
            if (distance <= 0m) return maxValue;
            if (distance > maxDistance) return minValue;
            return minValue + ((maxDistance - distance) / maxDistance) * (maxValue - minValue);
        }
        #endregion
        #region Resize (pomocí instance třídy ResizeControl)
        /// <summary>
        /// true pokud this control může provádět Resize
        /// </summary>
        public bool CanResize { get { return this._CanResize; } set { this._SetCanResize(value); } } private bool _CanResize;
        /// <summary>
        /// Nastaví hodnotu <see cref="CanResize"/> a naváže další akce
        /// </summary>
        /// <param name="canResize"></param>
        private void _SetCanResize(bool canResize)
        {
            bool isChange = (canResize != this._CanResize);
            this._CanResize = canResize;

            if (canResize)
            {
                if (this._ResizeControl == null)
                {
                    this._ResizeControl = new ResizeControl(this) { };
                    this._ResizeControl.ShowResizeAllways = false;
                    this._ResizeControl.CanUpsideDown = false;
                    this._ResizeControl.ResizeSides = RectangleSide.Vertical;
                    isChange = true;
                }
            }
            if (isChange) this._InvalidateChilds();
        }
        /// <summary>
        /// Do této metody je posílána informace o probíhajícím Resize tohoto grafického prvku
        /// </summary>
        /// <param name="e">Data</param>
        void IResizeObject.SetBoundsResized(ResizeObjectArgs e)
        {
            BoundsInfo boundsInfo = this.BoundsInfo;
            TimeRange timeRangeTarget = this._GetResizedTimeRange(e, boundsInfo, AxisTickType.Pixel);
            ItemResizeArgs args = new ItemResizeArgs(e, this.Graph, this._Group, this._Owner, this._Position, boundsInfo, timeRangeTarget);

            this.Graph.ResizeGroupCallSource(args);

            if (args.BoundsFinal.HasValue)
            {
                this.Bounds = args.BoundsFinal.Value;

                // Vložit nový čas do grupy: při ukončení (DragThisDrop) povinně, při pohybu (DragThisMove) jen pro grupu s 1 prvkem:
                bool setTime =
                    ((e.ResizeAction == DragActionType.DragThisDrop) ||
                     (e.ResizeAction == DragActionType.DragThisMove && this.Position == GGraphControlPosition.Group && this.Group.ItemCount == 1));
                if (setTime)
                    this.Time = args.TimeRangeFinal;       // Změna času u prvku typu Group provede vhodné rozmístění vnitřních prvků typu Item!
                this.Parent.Repaint();
            }
        }
        /// <summary>
        /// Metoda určí a vrátí časový interval prvku this po Resize
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsInfo"></param>
        /// <param name="roundTickType">Režim zaokrouhlení (vzhledem k aktuálnímu rozlišení osy)</param>
        /// <returns></returns>
        private TimeRange _GetResizedTimeRange(ResizeObjectArgs e, BoundsInfo boundsInfo, AxisTickType roundTickType = AxisTickType.None)
        {
            // Tento algoritmus zachová přesné DateTime z původního stavu v situaci, kdy se nezměnila pixelová pozice vizuálního prvku
            //  (pokud BoundsTarget.X == BoundsOriginal.X, tak se nezmění DateTime Begin...)
            // Pokud došlo ke změně pozice (X nebo Right), pak se z pozice pixelu vypočítá DateTime (s pomocí časové osy grafu).
            // Nepřihlížíme k tvrzení ze zdroje akce k tomu, kterou stranu prvku přemísťoval.
            TimeRange timeRangeCurrent = this.Time;
            DateTime? begin = timeRangeCurrent.Begin.Value;
            DateTime? end = timeRangeCurrent.End.Value;

            if (e.BoundsTarget.X != e.BoundsOriginal.X)
                begin = this.Graph.GetTimeForPosition(e.BoundsTarget.X);

            if (e.BoundsTarget.Right != e.BoundsOriginal.Right)
                end = this.Graph.GetTimeForPosition(e.BoundsTarget.Right);

            TimeRange timeRangeTarget = new TimeRange(begin, end);
            return timeRangeTarget;
        }
        /// <summary>
        /// Řídící control pro interaktivitu Resize
        /// </summary>
        private ResizeControl _ResizeControl;
        #endregion
        #region Kreslení prvku - řízení a obecná rovina
        /// <summary>
        /// Vykreslí this prvek
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        /// <param name="drawMode">Režim kreslení (pomáhá řešit Drag and Drop procesy)</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            if (e.DrawLayer != GInteractiveDrawLayer.Dynamic)
                this._Owner.Draw(e, absoluteBounds, drawMode);
        }
        /// <summary>
        /// Vykreslování "Přes Child prvky": pokud this prvek vykresluje Grupu, pak ano!
        /// </summary>
        protected override bool NeedDrawOverChilds { get { return (this._Position == GGraphControlPosition.Group); } set { } }
        /// <summary>
        /// Metoda volaná pro vykreslování "Přes Child prvky": převolá se grupa.
        /// </summary>
        /// <param name="e">Standardní data pro kreslení</param>
        /// <param name="boundsAbsolute">Absolutní souřadnice tohoto prvku</param>
        /// <param name="drawMode">Režim kreslení (má význam pro akce Drag and Drop)</param>
        protected override void DrawOverChilds(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            if (this._Position == GGraphControlPosition.Group)
                this._Group.DrawOverChilds(e, boundsAbsolute, drawMode);
        }
        /// <summary>
        /// Metoda je volaná pro vykreslení jedné položky grafu.
        /// Implementátor může bez nejmenších obav převolat <see cref="GControl"/>.<see cref="GTimeGraphItem.DrawItem(GInteractiveDrawArgs, Rectangle, DrawItemMode)"/>;
        /// a to jak pro typ prvku <see cref="GGraphControlPosition.Group"/>, tak pro <see cref="GGraphControlPosition.Item"/>.
        /// </summary>
        /// <param name="e">Standardní data pro kreslení</param>
        /// <param name="boundsAbsolute">Absolutní souřadnice tohoto prvku</param>
        /// <param name="drawMode">Režim kreslení (má význam pro akce Drag and Drop)</param>
        public void DrawItem(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            if (boundsAbsolute.Height <= 0 || boundsAbsolute.Width < 0) return;
            if (boundsAbsolute.Width < 1)
                boundsAbsolute.Width = 1;

            switch (this._Position)
            {
                case GGraphControlPosition.Group:
                    this.DrawItemGroup(e, boundsAbsolute, drawMode);
                    break;
                case GGraphControlPosition.Item:
                    this.DrawItemItem(e, boundsAbsolute, drawMode);
                    break;
            }
        }
        /// <summary>
        /// Volba, zda metoda <see cref="InteractiveObject.Repaint()"/> způsobí i vyvolání metody Parent.Repaint().
        /// </summary>
        protected override RepaintParentMode RepaintParent { get { return RepaintParentMode.Always; } }
        #endregion
        #region Fyzické kreslení grupy (= spojovací linie a text)
        /// <summary>
        /// Vykreslí prvek typu <see cref="GGraphControlPosition.Group"/> = podklad pod konkrétními prvky = spojovací čára
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="drawMode"></param>
        protected void DrawItemGroup(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            // Barva pozadí se přebírá z prvního prvku. Pokud prvek nemá barvu pozadí, pak se nekreslí ani spojovací linie:
            Color? itemBackColor = this.ItemBackColor;
            if (itemBackColor.HasValue)
            {
                // Reálně použitá barva pozadí pro spojovací linii je částečně (33%) průhledná:
                Color color = this._Group.GetColorWithOpacity(itemBackColor.Value, e.DrawLayer, drawMode, true, true);

                Rectangle[] boundsParts = GPainter.GraphItemCreateBounds(boundsAbsolute, true, false, false, false);
                float? effect3D = this.CurrentEffect3DGroup;
                GPainter.DrawEffect3D(e.Graphics, boundsParts[0], color, System.Windows.Forms.Orientation.Horizontal, effect3D, null);
            }
        }
        /// <summary>
        /// Vykreslí ikonky zadané jako <see cref="ITimeGraphItem.ImageBegin"/> a <see cref="ITimeGraphItem.ImageEnd"/>.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="boundsVisibleAbsolute"></param>
        /// <param name="drawMode"></param>
        internal void DrawImages(GInteractiveDrawArgs e, ref Rectangle boundsAbsolute, Rectangle boundsVisibleAbsolute, DrawItemMode drawMode)
        {
            Image image;

            image = this._Owner.ImageBegin;
            if (image != null)
                this.DrawImage(image, e, ref boundsAbsolute, ContentAlignment.MiddleLeft, boundsVisibleAbsolute, drawMode);
            image = this._Owner.ImageEnd;
            if (image != null)
                this.DrawImage(image, e, ref boundsAbsolute, ContentAlignment.MiddleRight, boundsVisibleAbsolute, drawMode);
        }
        /// <summary>
        /// Vykreslí Image
        /// </summary>
        /// <param name="image"></param>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="imageAlignment"></param>
        /// <param name="boundsVisibleAbsolute"></param>
        /// <param name="drawMode"></param>
        protected void DrawImage(Image image, GInteractiveDrawArgs e, ref Rectangle boundsAbsolute, ContentAlignment imageAlignment, Rectangle boundsVisibleAbsolute, DrawItemMode drawMode)
        {
            if (image == null) return;
            Size imageSize = image.Size;
            int height = boundsAbsolute.Height - 2;
            if (imageSize.Height > height)
                imageSize = imageSize.ZoomToHeight(height);
            Rectangle imageBounds = imageSize.AlignTo(boundsAbsolute, imageAlignment);
            if (!boundsVisibleAbsolute.IntersectsWith(imageBounds)) return;

            // Vykreslit Image:
            e.Graphics.DrawImage(image, imageBounds);

            // Upravím souřadnice boundsAbsolute tak, aby nepokrývaly oblast, do které byl vykreslen Image:
            switch (imageAlignment)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.BottomLeft:
                    boundsAbsolute = new Rectangle(imageBounds.Right, boundsAbsolute.Y, boundsAbsolute.Right - imageBounds.Right, boundsAbsolute.Height);
                    break;
                case ContentAlignment.TopRight:
                case ContentAlignment.MiddleRight:
                case ContentAlignment.BottomRight:
                    boundsAbsolute = new Rectangle(boundsAbsolute.X, boundsAbsolute.Y, imageBounds.X - boundsAbsolute.X, boundsAbsolute.Height);
                    break;
            }
        }
        /// <summary>
        /// Vykreslí text dané grupy
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="text"></param>
        /// <param name="fontInfo"></param>
        internal void DrawText(GInteractiveDrawArgs e, Rectangle boundsAbsolute, string text, FontInfo fontInfo)
        {
            Color textColor = this.TextColorCurrent;
            Rectangle boundsText = boundsAbsolute;
            boundsText.Y = boundsText.Y;
            GPainter.DrawString(e.Graphics, text, fontInfo, boundsText, ContentAlignment.MiddleCenter, textColor);
        }
        /// <summary>
        /// Barva textu (písma) získaná dle pravidel z prvku
        /// </summary>
        private Color TextColorCurrent
        {
            get
            {
                if (this.ItemTextColor.HasValue) return this.ItemTextColor.Value;
                Color backColor = this.ItemBackColor ?? this.RatioBeginBackColor ?? Skin.Graph.BackColor;
                return backColor.Contrast();
            }
        }
        #endregion
        #region Fyzické kreslení konkrétního prvku
        /// <summary>
        /// Vykreslí prvek typu <see cref="GGraphControlPosition.Item"/> = vlastní grafický prvek
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        /// <param name="drawMode"></param>
        protected void DrawItemItem(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            GPainter.GraphItemArgs graphItemArgs = new GPainter.GraphItemArgs(e.Graphics, boundsAbsolute);
            graphItemArgs.IsGroup = false;
            graphItemArgs.BackColor = this._Group.GetColorWithOpacity(this.ItemBackColor, e.DrawLayer, drawMode, false, true);
            graphItemArgs.LineColor = this._Group.GetColorWithOpacity(this.ItemLineColor, e.DrawLayer, drawMode, false, true);
            graphItemArgs.InteractiveState = this.GroupInteractiveState;
            graphItemArgs.Effect3D = this.CurrentEffect3DItem;
            graphItemArgs.IsFirstItem = this.IsFirstItem;
            graphItemArgs.IsLastItem = this.IsLastItem;
            graphItemArgs.HatchStyle = this._Owner.BackStyle;
            graphItemArgs.HatchColor = (this._Owner.HatchColor ?? this._Owner.LineColor);

            graphItemArgs.RatioBegin = this._Owner.RatioBegin;
            graphItemArgs.RatioBeginBackColor = this._Owner.RatioBeginBackColor;
            graphItemArgs.RatioEnd = this._Owner.RatioEnd;
            graphItemArgs.RatioEndBackColor = this._Owner.RatioEndBackColor;
            graphItemArgs.RatioStyle = this._Owner.RatioStyle;
            graphItemArgs.RatioLineColor = this._Owner.RatioLineColor;
            graphItemArgs.RatioLineWidth = this._Owner.RatioLineWidth;
            graphItemArgs.BackEffectStyle = (this.IsEditable ? this._Owner.BackEffectEditable : this._Owner.BackEffectNonEditable);

            // Stav IsSelected a IsFramed budeme vždy přebírat z GUI prvku Grupy, protože Select a Framed a Activated se řeší na úrovni Grupy:
            GTimeGraphItem groupItem = (this._Position == GGraphControlPosition.Group ? this :
                                        this._Position == GGraphControlPosition.Item ? this._Group.GControl : null);
            if (groupItem != null)
            {
                graphItemArgs.IsSelected = groupItem.IsSelected;
                graphItemArgs.IsFramed = groupItem.IsFramed;
                graphItemArgs.IsActivated = groupItem.IsActivated;
            }

            GPainter.GraphItemDraw(graphItemArgs);
        }
        /// <summary>
        /// Vytvoří pole souřadnic, které obsahuje pět prvků:
        /// [0]: střed; [1]: levý kraj; [2]: horní kraj; [3]: pravý kraj; [4]: dolní kraj;
        /// </summary>
        /// <param name="boundsAbsolute">Souřadnice prvku absolutní</param>
        /// <param name="position">Pozice prvku (Group / Item)</param>
        /// <param name="isFirst">Prvek je prvním v grupě?</param>
        /// <param name="isLast">Prvek je posledním v grupě?</param>
        /// <param name="hasBorder">Prvek je Selected nebo Framed?</param>
        private static Rectangle[] _CreateBounds(Rectangle boundsAbsolute, GGraphControlPosition position, bool isFirst, bool isLast, bool hasBorder)
        {
            int x = boundsAbsolute.X;
            int y = boundsAbsolute.Y;
            int w = boundsAbsolute.Width;
            int h = boundsAbsolute.Height;
            int wb = (w <= 2 ? 0 : ((w < 5) ? 1 : (hasBorder ? 2 : 1)));
            int hb = (h <= 2 ? 0 : ((h < 10 || position == GGraphControlPosition.Group) ? 1 : (hasBorder ? 3 : 2)));    // Výška proužku "horní a dolní okraj"
            int hc = h - 2 * hb;

            Rectangle[] boundsParts = new Rectangle[5];
            if (position == GGraphControlPosition.Group)
                boundsParts[0] = new Rectangle(x, y + hb, w, hc);              // Střední prostor pro Group
            else
                boundsParts[0] = new Rectangle(x, y, w, h);                    // Střední prostor pro Item
            if (isFirst)
                boundsParts[1] = new Rectangle(x, y + hb, wb, hc + hb);        // Levý okraj
            boundsParts[2] = new Rectangle(x, y, w, hb);                       // Horní okraj
            if (isLast)
                boundsParts[3] = new Rectangle(x + w - wb, y, wb, hc + hb);    // Pravý okraj
            boundsParts[4] = new Rectangle(x, y + hb + hc, w, hb);             // Dolní okraj

            return boundsParts;
        }
        /// <summary>
        /// 3D efekt pro prvek za aktuálního stavu
        /// </summary>
        protected float? CurrentEffect3DItem
        {
            get
            {
                float? effect3D = (IsInteractive ? GPainter.GetEffect3D(GroupInteractiveState) : (float?)-0.10f);
                return ((effect3D.HasValue && effect3D.Value != 0f) ? (float?)effect3D.Value * 1.25f : (float?)null);
            }
        }
        /// <summary>
        /// 3D efekt pro grupu za aktuálního stavu
        /// </summary>
        protected float? CurrentEffect3DGroup
        {
            get
            {
                float? effect3D = (IsInteractive ? GPainter.GetEffect3D(GroupInteractiveState) : (float?)-0.10f);
                return ((effect3D.HasValue && effect3D.Value != 0f) ? (float?)effect3D.Value * 0.90f : (float?)null);
            }
        }
        /// <summary>
        /// true = prvek je editovatelný
        /// </summary>
        protected bool IsEditable
        {
            get { return (this._Owner.BehaviorMode.HasAnyFlag(GraphItemBehaviorMode.AnyMove | GraphItemBehaviorMode.ResizeTime | GraphItemBehaviorMode.ResizeHeight)); }
        }
        /// <summary>
        /// true = prvek je selectovatelný
        /// </summary>
        protected bool IsSelectable
        {
            get { return (this._Owner.BehaviorMode.HasFlag(GraphItemBehaviorMode.CanSelect)); }
        }
        /// <summary>
        /// true = prvek je interaktivní (=editovatelný nebo selectovatelný)
        /// </summary>
        protected bool IsInteractive
        {
            get { return (this._Owner.BehaviorMode.HasAnyFlag(GraphItemBehaviorMode.AnyMove | GraphItemBehaviorMode.ResizeTime | GraphItemBehaviorMode.ResizeHeight | GraphItemBehaviorMode.CanSelect)); }
        }
        /// <summary>
        /// Obsahuje true, pokud this prvek reprezentuje Item, který je prvním v grupě
        /// </summary>
        protected bool IsFirstItem
        {
            get
            {
                if (this._Position != GGraphControlPosition.Item) return false;
                ITimeGraphItem dataItem = this._Owner;
                ITimeGraphItem firstItem = this._Group.Items[0];
                return (Object.ReferenceEquals(dataItem, firstItem));
            }
        }
        /// <summary>
        /// Obsahuje true, pokud this prvek reprezentuje Item, který je posledním v grupě
        /// </summary>
        protected bool IsLastItem
        {
            get
            {
                if (this._Position != GGraphControlPosition.Item) return false;
                ITimeGraphItem dataItem = this._Owner;
                ITimeGraphItem lastItem = this._Group.Items[this._Group.ItemCount - 1];
                return (Object.ReferenceEquals(dataItem, lastItem));
            }
        }
        #endregion
        #region Vztahy (= Linky) - získání a kreslení
        /// <summary>
        /// Zajistí aktivaci linků pro this item
        /// </summary>
        /// <param name="fromMouse"></param>
        /// <param name="fromSelect"></param>
        internal void ActivateLink(bool fromMouse, bool fromSelect)
        {
            if (fromMouse)
                this.PrepareLinksForMouseOver();

            if (fromSelect)
                this.PrepareLinksForSelect(true);
        }
        /// <summary>
        /// Zajistí zobrazení vztahů (Linky) z this prvku na jeho sousedy, v situaci kdy na this prvek najela myš (MouseEnter).
        /// Metoda vyhledá linky pomocí metody <see cref="ITimeGraphDataSource.CreateLinks(CreateLinksArgs)"/>, 
        /// poté je přidá do globálního controlu pro zobrazování Linků <see cref="GTimeGraph.GraphLinkArray"/> (v <see cref="Graph"/>), 
        /// a vloží je do <see cref="LinksForMouseOver"/> - protože po odjetí myši bude volaná metoda <see cref="RemoveLinksForMouseOver()"/>,
        /// která tyto vztahy ze zobrazení odebere.
        /// </summary>
        protected void PrepareLinksForMouseOver()
        {
            // Pokud aktuální graf NENÍ nebo NEMÁ zobrazovat linky v MouseOver, pak skončíme:
            if (this.Graph == null || this.Graph.DataSource == null || !this.Graph.GraphLinkArray.CurrentLinksMode.HasAnyFlag(GTimeGraphLinkMode.MouseOver)) return;

            // Pokud aktuální PRVEK nemá zobrazovat linky:
            if (!this.Group.IsShowLinks) return;

            // Pokud this je na pozici Item, a naše grupa (this.Group) už má nalezené linky, pak je nebudeme opakovaně hledat pro prvek:
            if (this.Position == GGraphControlPosition.Item && this.Group.GControl.LinksForMouseOver != null) return;

            GTimeGraphItem item = this;
            CreateLinksArgs args = new CreateLinksArgs(item.Graph, item.Group, item.Item, item.Position, CreateLinksItemEventType.MouseOver);
            item.Graph.DataSource.CreateLinks(args);
            GTimeGraphLinkItem[] linksOne = args.Links;
            if (linksOne == null || linksOne.Length == 0) return;

            this.Graph.GraphLinkArray.AddLinks(linksOne, GTimeGraphLinkMode.MouseOver);
            this.LinksForMouseOver = linksOne;
        }
        /// <summary>
        /// Zajistí konec zobrazování těch vztahů (Linky) z this prvku na sousedy, které byly přidány při MouseEnter (v metodě <see cref="PrepareLinksForMouseOver()"/>).
        /// Vztahy má uloženy v <see cref="LinksForMouseOver"/>, odebere je z globálního controlu pro zobrazování Linků <see cref="GTimeGraph.GraphLinkArray"/> (v <see cref="Graph"/>), 
        /// a na závěr nulluje <see cref="LinksForMouseOver"/>.
        /// </summary>
        protected void RemoveLinksForMouseOver()
        {
            if (this.LinksForMouseOver != null)
                this.Graph.GraphLinkArray.RemoveLinks(this.LinksForMouseOver, GTimeGraphLinkMode.MouseOver);
            this.LinksForMouseOver = null;
        }
        /// <summary>
        /// Pole vztahů, které jsou získány při MouseEnter a odebrány při MouseLeave
        /// </summary>
        protected GTimeGraphLinkItem[] LinksForMouseOver;
        /// <summary>
        /// Zajistí zobrazení /zhasnutí vztahů (Linků) z this prvku na sousedy, jako reaklci na změnu hodnoty <see cref="InteractiveObject.IsSelected"/>.
        /// </summary>
        /// <param name="isSelected">Aktuálně platný stav IsSelected prvku</param>
        protected void PrepareLinksForSelect(bool isSelected)
        {
            // Pokud aktuální graf NEMÁ zobrazovat linky v IsSelected stavu, pak skončíme:
            if (this.Graph == null || this.Graph.DataSource == null || !this.Graph.GraphLinkArray.CurrentLinksMode.HasAnyFlag(GTimeGraphLinkMode.Selected)) return;

            // Pokud aktuální PRVEK nemá zobrazovat linky:
            if (!this.Group.IsShowLinks) return;

            // Pokud this je na pozici Item, a naše grupa (this.Group) už má nalezené linky, pak je nebudeme opakovaně hledat pro prvek:
            if (this.Position == GGraphControlPosition.Item && this.Group.GControl.LinksForMouseOver != null) return;

            if (this.LinksSelect != null)
            {   // Pokud něco máme z dřívějška, tak to odebereme:
                this.Graph.GraphLinkArray.RemoveLinks(this.LinksSelect, GTimeGraphLinkMode.Selected);
                this.LinksSelect = null;
            }

            if (isSelected)
            {   // Prvek je vybrán, a má se Selectovat:
                GTimeGraphItem item = this;
                CreateLinksArgs args = new CreateLinksArgs(item.Graph, item.Group, item.Item, item.Position, CreateLinksItemEventType.ItemSelected);
                item.Graph.DataSource.CreateLinks(args);
                GTimeGraphLinkItem[] links = args.Links;
                if (links != null && links.Length > 0)
                {
                    this.Graph.GraphLinkArray.AddLinks(links, GTimeGraphLinkMode.Selected);
                    this.LinksSelect = links;
                }
            }
        }
        /// <summary>
        /// Pole vztahů, které jsou získány při Select = true a odebrány při Select = false
        /// </summary>
        protected GTimeGraphLinkItem[] LinksSelect;
        #endregion
    }
    #region enum GGraphControlPosition
    /// <summary>
    /// Pozice GUI controlu pro prvek grafu (Group / Item)
    /// </summary>
    public enum GGraphControlPosition
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Control pro grupu
        /// </summary>
        Group,
        /// <summary>
        /// Control pro konkrétní instanci <see cref="ITimeGraphItem"/>
        /// </summary>
        Item
    }
    #endregion
}
