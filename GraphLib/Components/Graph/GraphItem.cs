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
    public class GTimeGraphItem : InteractiveDragObject, IOwnerProperty<ITimeGraphItem>
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
        /// Vložení hodnoty do této property způsobí veškeré zpracování akcí (<see cref="ProcessAction.All"/>).
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
        /// Souřadnice na ose X. Jednotkou jsou pixely.
        /// Tato osa je společná jak pro virtuální, tak pro reálné souřadnice.
        /// Hodnota 0 odpovídá prvnímu viditelnému pixelu vlevo.
        /// </summary>
        public Int32Range CoordinateX { get; set; }
        /// <summary>
        /// Barva pozadí tohoto prvku: tento override vrací průhlednou barvu. Základní metoda nemá kreslit pozadí povinně.
        /// Více viz <see cref="ItemBackColor"/>.
        /// </summary>
        public override Color BackColor { get { return Color.Transparent; } set { } }
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
        /// Přidá vnořený objekt
        /// </summary>
        /// <param name="child"></param>
        public void AddItem(IInteractiveItem child)
        {
            if (this._Childs == null)
                this._Childs = new List<Components.IInteractiveItem>();
            this._Childs.Add(child);
        }
        private List<IInteractiveItem> _Childs;
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
            this.PrepareLinksMouse();
            base.AfterStateChangedMouseEnter(e);
        }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = MouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseLeave(GInteractiveChangeStateArgs e)
        {
            if (InteractiveLeaveThisGroup(e))
                this.ResetLinksMouse();
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
            this.PrepareLinksSelect(args.NewValue);
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
        protected override void PrepareToolTip(GInteractiveChangeStateArgs e)
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
        /// Selectovat lze jen Group prvky, nikoli Item;
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool _GetSelectable(bool value)
        {
            return (this._Position == GGraphControlPosition.Group && this._Group.IsDragEnabled);
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

            // Sestavím argument (pro this prvek) a doplním do něj údaje o dalších prvcích:
            Rectangle targetAbsoluteBounds = e.BoundsInfo.GetAbsBounds(targetRelativeBounds);
            ItemDragDropArgs args = new ItemDragDropArgs(e, this.Graph, this._Group, this._Owner, this._Position, targetAbsoluteBounds);
            args.ParentGraph = this.Graph;
            args.ParentTable = this.SearchForParent(typeof(Grid.GTable)) as Grid.GTable;
            args.SearchForTargets(e.MouseCurrentAbsolutePoint.Value);

            args.IsFinalised = true;

            // Předem připravím "defaultní" výsledky (ale datový zdroj, který se vyvolá v následujícím řádku) má plnou moc nastavit libovolné výsledky po svém:
            this.DragPrepareDefaultValidity(args);

            // Zavolám datový zdroj, ten vyhodnotí zda se může prvek posunout, nebo na jaké souřadnice se může posunout, a určí vzhled kreslení (disabled?):
            // On by mohl i zařídit "scrollování" v čase (osa X => TimeAxis) nebo v místě (osa Y => řádek, kam položku přesunout):
            this.Graph.DragDropGroupCallSource(args);

            // Převezmu výsledky (buď defaultní, nebo modifikované aplikační logikou):
            this.DragDropTargetItem = args.TargetDropItem;
            this._Group.DragDropDrawInteractiveOpacity = args.TargetValidityOpacity;
            if (args.DragToAbsoluteBounds.HasValue)
            {
                targetAbsoluteBounds = args.DragToAbsoluteBounds.Value;
                targetRelativeBounds = e.BoundsInfo.GetRelBounds(targetAbsoluteBounds);
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
                float? effect3D = this._GetEffect3D(false);
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
            Color? itemBackColor = this.ItemBackColor;
            if (!itemBackColor.HasValue)
                itemBackColor = this.RatioBeginBackColor;
            if (!itemBackColor.HasValue)
                itemBackColor = Skin.Graph.BackColor;
            Color foreColor = itemBackColor.Value.Contrast();
            Rectangle boundsText = boundsAbsolute;
            boundsText.Y = boundsText.Y;
            GPainter.DrawString(e.Graphics, boundsText, text, foreColor, fontInfo, ContentAlignment.MiddleCenter);
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
            graphItemArgs.Effect3D = this._GetEffect3D(true);
            graphItemArgs.IsFirstItem = this.IsFirstItem;
            graphItemArgs.IsLastItem = this.IsLastItem;
            graphItemArgs.HatchStyle = this._Owner.BackStyle;
            graphItemArgs.HatchColor = (this._Owner.HatchColor ?? this._Owner.LineColor);

            graphItemArgs.RatioBegin = this._Owner.RatioBegin;
            graphItemArgs.RatioBeginBackColor = this._Owner.RatioBeginBackColor;
            graphItemArgs.RatioEnd = this._Owner.RatioEnd;
            graphItemArgs.RatioEndBackColor = this._Owner.RatioEndBackColor;
            graphItemArgs.RatioLineColor = this._Owner.RatioLineColor;
            graphItemArgs.RatioLineWidth = this._Owner.RatioLineWidth;

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
        /// Vrátí úroveň 3D efektu pro this prvek
        /// </summary>
        /// <param name="forItem"></param>
        /// <returns></returns>
        private float? _GetEffect3D(bool forItem)
        {
            GraphItemBehaviorMode behavior = this._Owner.BehaviorMode;
            bool isEditable = behavior.HasAnyFlag(GraphItemBehaviorMode.AnyMove | GraphItemBehaviorMode.ResizeTime | GraphItemBehaviorMode.ResizeHeight);
            GInteractiveState state = this.GroupInteractiveState;
            float? effect3D = (isEditable ? GPainter.GetEffect3D(state) : (float?)-0.10f);
            if (effect3D.HasValue && effect3D.Value != 0f)
                effect3D = effect3D.Value * (forItem ? 1.25f : 0.90f);
            return effect3D;
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
                this.PrepareLinksMouse();

            if (fromSelect)
                this.PrepareLinksSelect(true);
        }
        /// <summary>
        /// Zkusí najít vztahy ke kreslení.
        /// Pokud nějaké najde, budou uloženy v <see cref="LinksMouse"/>.
        /// </summary>
        protected void PrepareLinksMouse()
        {
            // Pokud this prvek nemá zobrazovat linky v MouseOver, pak skončíme:
            if (!this.Item.BehaviorMode.HasFlag(GraphItemBehaviorMode.ShowLinkInMouseOver)) return;

            // Pokud this je na pozici Item, a naše grupa (this.Group) už má nalezené linky, pak je nebudeme opakovaně hledat pro prvek:
            if (this.Position == GGraphControlPosition.Item && this.Group.GControl.LinksMouse != null) return;

            GTimeGraphItem item = this;
            CreateLinksArgs args = new CreateLinksArgs(item.Graph, item.Group, item.Item, item.Position, CreateLinksItemEventType.MouseOver);
            item.Graph.DataSource.CreateLinks(args);
            GTimeGraphLinkItem[] linksOne = args.Links;
            if (linksOne == null || linksOne.Length == 0) return;

            this.Graph.GraphLinkArray.AddLinks(linksOne, GTimeGraphLinkMode.Mouse, 0.45f);
            this.LinksMouse = linksOne;
        }
        /// <summary>
        /// Resetuje kreslené vztahy. Odteď se nebudou kreslit.
        /// </summary>
        protected void ResetLinksMouse()
        {
            if (this.LinksMouse != null)
                this.Graph.GraphLinkArray.RemoveLinks(this.LinksMouse, GTimeGraphLinkMode.Mouse);
            this.LinksMouse = null;
        }
        /// <summary>
        /// Pole vztahů, které jsou získány při MouseEnter a odebrány při MouseLeave
        /// </summary>
        protected GTimeGraphLinkItem[] LinksMouse;
        /// <summary>
        /// Připrava linků po změně <see cref="InteractiveObject.IsSelected"/>
        /// </summary>
        /// <param name="isSelected">Aktuálně platný stav IsSelected prvku</param>
        protected void PrepareLinksSelect(bool isSelected)
        {
            // Pokud this prvek nemá zobrazovat linky v IsSelected stavu, pak skončíme:
            if (!this.Item.BehaviorMode.HasFlag(GraphItemBehaviorMode.ShowLinkInSelected)) return;

            // Pokud this je na pozici Item, a naše grupa (this.Group) už má nalezené linky, pak je nebudeme opakovaně hledat pro prvek:
            if (this.Position == GGraphControlPosition.Item && this.Group.GControl.LinksMouse != null) return;

            if (this.LinksSelect != null)
            {   // Deselectovat:
                this.Graph.GraphLinkArray.RemoveLinks(this.LinksSelect, GTimeGraphLinkMode.Select);
                this.LinksSelect = null;
            }

            if (isSelected)
            {   // Selectovat:
                GTimeGraphItem item = this;
                CreateLinksArgs args = new CreateLinksArgs(item.Graph, item.Group, item.Item, item.Position, CreateLinksItemEventType.ItemSelected);
                item.Graph.DataSource.CreateLinks(args);
                GTimeGraphLinkItem[] links = args.Links;
                if (links != null && links.Length > 0)
                {
                    this.Graph.GraphLinkArray.AddLinks(links, GTimeGraphLinkMode.Select, 0.8f);
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
    #region GTimeGraphLinkArray : Vizuální pole, obsahující prvky GTimeGraphLinkItem
    /// <summary>
    /// GTimeGraphLinkArray : Vizuální pole, obsahující prvky <see cref="GTimeGraphLinkItem"/>. Jde o <see cref="InteractiveObject"/>, 
    /// který nemá implementovanou interaktivitu, ale je součástí tabulky <see cref="Grid.GTable"/> 
    /// (anebo je členem vizuálních prvků hlavního controlu Host), 
    /// a je vykreslován do vrstvy Dynamic.
    /// Graf samotný obsahuje referenci na tento objekt, referenci dohledává on-demand a případně ji vytváří a umisťuje tak, 
    /// aby objekt byl dostupný i dalším grafům.
    /// Toto jedno pole je společné všem grafům jedné tabulky (nebo jednoho hostitele).
    /// </summary>
    public class GTimeGraphLinkArray : InteractiveObject
    {
        #region Konstrukce, úložiště linků, reference na ownera (tabulka / graf)
        /// <summary>
        /// Konstruktor pro graf
        /// </summary>
        /// <param name="ownerGraph"></param>
        public GTimeGraphLinkArray(GTimeGraph ownerGraph)
            : this()
        {
            this._OwnerGraph = ownerGraph;
        }
        /// <summary>
        /// Konstruktor pro tabulku
        /// </summary>
        /// <param name="ownerGTable"></param>
        public GTimeGraphLinkArray(Grid.GTable ownerGTable)
            : this()
        {
            this._OwnerGTable = ownerGTable;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GTimeGraphLinkArray()
        {
            this._LinkDict = new Dictionary<UInt64, LinkInfo>();
        }
        /// <summary>
        /// Úložiště linků + přidaných dat
        /// </summary>
        private Dictionary<UInt64, LinkInfo> _LinkDict;
        /// <summary>
        /// true pokud this objekt je platný jen pro jeden Graf
        /// </summary>
        public bool IsForOneGraph { get { return (this._OwnerGraph != null && this._OwnerGTable == null); } }
        /// <summary>
        /// Graf, jehož jsme koordinátorem (může být null?)
        /// </summary>
        private GTimeGraph _OwnerGraph;
        /// <summary>
        /// true pokud this objekt je platný pro celou GTable
        /// </summary>
        public bool IsForGTable { get { return (this._OwnerGTable != null); } }
        /// <summary>
        /// Tabulka, jíž jsme koordinátorem (může být null?)
        /// </summary>
        private Grid.GTable _OwnerGTable;
        #endregion
        #region Prvky: přidávání, odebírání, enumerace
        /// <summary>
        /// Přidá dané linky do paměti
        /// </summary>
        /// <param name="links">Souhrn linků k přidání</param>
        /// <param name="mode">Důvod zobrazení</param>
        /// <param name="ratio">Průhlednost v rozsahu 0 (neviditelná) - 1 (plná barva)</param>
        public void AddLinks(IEnumerable<GTimeGraphLinkItem> links, GTimeGraphLinkMode mode, float ratio)
        {
            if (links == null) return;
            if (mode == GTimeGraphLinkMode.None || ratio <= 0f) return;
            Dictionary<UInt64, LinkInfo> linkDict = this._LinkDict;
            foreach (GTimeGraphLinkItem link in links)
            {
                if (link == null) continue;
                UInt64 key = link.Key;
                LinkInfo linkInfo;
                bool exists = linkDict.TryGetValue(key, out linkInfo);
                if (!exists)
                {   // Pro daný klíč (Prev-Next) dosud nemám link => založím si nový, a přidám do něj dodaná data:
                    linkInfo = new LinkInfo(link) { Mode = mode, Ratio = ratio };
                    linkDict.Add(key, linkInfo);
                }
                else
                {   // Link máme => můžeme v něm navýšit hodnoty:
                    if (linkInfo.Ratio < ratio) linkInfo.Ratio = ratio;
                    linkInfo.Mode |= mode;
                }
            }
            // Zajistíme překreslení všech vztahů:
            this.Repaint();
        }
        /// <summary>
        /// Odebere dané linky z paměti, pokud v ní jsou a pokud již neexistuje důvod pro jejich zobrazování.
        /// Důvod zobrazení: každý link v sobě eviduje souhrn důvodů, pro které byl zobrazen (metoda <see cref="AddLinks(IEnumerable{GTimeGraphLinkItem}, GTimeGraphLinkMode, float)"/>),
        /// důvody z opakovaných volání této metody se průběžně sčítají, a při odebírání se odečítají.
        /// A až tam nezbyde žádný, bude link ze seznamu odebrán.
        /// </summary>
        /// <param name="links">Souhrn linků k odebrání</param>
        /// <param name="mode">Důvod, pro který byl link zobrazen</param>
        public void RemoveLinks(IEnumerable<GTimeGraphLinkItem> links, GTimeGraphLinkMode mode)
        {
            if (links == null) return;
            if (mode == GTimeGraphLinkMode.None) return;
            bool repaint = false;
            GTimeGraphLinkMode reMode = GTimeGraphLinkMode.All ^ mode;         // reMode nyní obsahuje XOR požadovanou hodnotu, použije se pro AND nulování
            Dictionary<UInt64, LinkInfo> linkDict = this._LinkDict;
            foreach (GTimeGraphLinkItem link in links)
            {
                if (link == null) continue;
                UInt64 key = link.Key;
                LinkInfo linkInfo;
                bool exists = linkDict.TryGetValue(key, out linkInfo);
                if (!exists) continue;

                // Zdejší algoritmus Remove nedokáže provést snížení hodnoty Ratio. Dokáže jen odebrat bit z hodnoty Mode.
                // Ke snížení Ratio: k tomu by došlo jen tehdy, když by link bylů nejprve zobrazen z důvodu Mouse, kde je Ratio menší než 1.0 (např. 0.80),
                //  pak by se přidal důvod Select a s tím i zvýšení Ratio na hodnotu 1,
                //  a poté by se odebral důvod Select - a nyní bychom očekávali snížení Ratio z 1.0 zpátky na 0.80.
                // Prakticky ale se to projeví jen v tomto postupu:
                //  a) Najdu myší na prvek = rozsvítí se Linky v režimu Mouse
                //  b) Kliknu na prvek = prvek se označí - Linky se rozsvítí i v režimu Select (takže Mode == Mouse | Select)
                //  c) Kliknu na prvek = z prvku se odebere důvod Select, zůstane tam Mouse, ale zůstane původní Ratio
                // Ale poté myš odjede z prvku, a Linky se odeberou i z důvodu Mouse, a Linky tak kompletně zhasnou.

                linkInfo.Mode &= reMode;                                       // Vstupní hodnota (mode) bude z hodnoty linkInfo.Mode vynulována
                if (linkInfo.Mode == GTimeGraphLinkMode.None)                  // A pokud v Mode nezbyla žádná hodnota, link odebereme.
                {
                    linkDict.Remove(key);
                    repaint = true;
                }
            }
            if (repaint)
                this.Repaint();
        }
        /// <summary>
        /// Smaže všechny linky z this paměti. Tím není provedeno jejich odstranění z paměti tabulky, ale pouze z paměti vykreslování.
        /// Jednoduše: linky pro aktuální objekt zhasnou.
        /// </summary>
        public void Clear()
        {
            this._LinkDict.Clear();
            this.Repaint();
        }
        /// <summary>
        /// Obsahuje true, pokud this prvek v sobě obsahuje nějaké linky k vykreslení
        /// </summary>
        public bool ContainLinks { get { return (this._LinkDict.Count > 0); } }
        /// <summary>
        /// Souhrn všech aktuálních linků, bez dalších informací
        /// </summary>
        public IEnumerable<GTimeGraphLinkItem> Links { get { return this._LinkDict.Values.Select(l => l.Link); } }
        #endregion
        #region Subclass LinkInfo: třída pro reálně ukládané prvky - obsahuje navíc i důvod zobrazení a transparentnost
        /// <summary>
        /// LinkInfo: třída pro reálně ukládané prvky - obsahuje navíc i důvod zobrazení a transparentnost
        /// </summary>
        private class LinkInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="link"></param>
            public LinkInfo(GTimeGraphLinkItem link)
            {
                this._Link = link;
                this._Mode = GTimeGraphLinkMode.None;
                this._Ratio = 0f;
            }
            private GTimeGraphLinkItem _Link;
            private GTimeGraphLinkMode _Mode;
            private float _Ratio;
            /// <summary>
            /// Objekt vztahu
            /// </summary>
            public GTimeGraphLinkItem Link { get { return this._Link; } }
            /// <summary>
            /// Důvod zobrazení: zadává ten, kdo si přeje zobrazit
            /// </summary>
            public GTimeGraphLinkMode Mode { get { return this._Mode; } set { this._Mode = value; } }
            /// <summary>
            /// Průhlednost Linku:
            /// hodnota v rozsahu 0.0 (neviditelná) - 1.0 (plná barva)
            /// </summary>
            public float Ratio { get { return this._Ratio; } set { this._Ratio = (value < 0f ? 0f : (value > 1f ? 1f : value)); } }
            /// <summary>
            /// Vykreslí tuto jednu linku
            /// </summary>
            /// <param name="e"></param>
            internal void Draw(GInteractiveDrawArgs e)
            {
                if (this.Link.NeedDraw)
                    this.Link.Draw(e, this.Mode, this.Ratio);
            }
        }
        #endregion
        #region Podpora pro kreslení (InteractiveObject)
        /// <summary>
        /// Vrstvy, do nichž se běžně má vykreslovat tento objekt.
        /// Tato hodnota se v metodě <see cref="InteractiveObject.Repaint()"/> vepíše do <see cref="InteractiveObject.RepaintToLayers"/>.
        /// Vrstva <see cref="GInteractiveDrawLayer.Standard"/> je běžná pro normální kreslení;
        /// vrstva <see cref="GInteractiveDrawLayer.Interactive"/> se používá při Drag and Drop;
        /// vrstva <see cref="GInteractiveDrawLayer.Dynamic"/> se používá pro kreslení linek mezi prvky nad vrstvou při přetahování.
        /// Vrstvy lze kombinovat.
        /// Vrstva <see cref="GInteractiveDrawLayer.None"/> je přípustná:  prvek se nekreslí, ale je přítomný a interaktivní.
        /// </summary>
        protected override GInteractiveDrawLayer StandardDrawToLayer { get { return (this.ContainLinks ? GInteractiveDrawLayer.Dynamic : GInteractiveDrawLayer.None); } }
        /// <summary>
        /// Vrstvy, do nichž se aktuálně (tj. v nejbližším kreslení) bude vykreslovat tento objekt.
        /// Po vykreslení se sem ukládá None, tím se šetří čas na kreslení (nekreslí se nezměněné prvky).
        /// </summary>
        protected override GInteractiveDrawLayer RepaintToLayers { get { return this.StandardDrawToLayer; } set { base.RepaintToLayers = value; } }
        /// <summary>
        /// Výchozí metoda pro kreslení prvku, volaná z jádra systému.
        /// </summary>
        /// <param name="e">Kreslící argument</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            if (e.DrawLayer != GInteractiveDrawLayer.Dynamic) return;

            // Najdeme oblast pro kreslení (Clip na oblast grafu, nebo na oblast grafu + dat v tabulce):
            Rectangle clip = this.GetClip(e.GraphicsBounds);

            // Na grafiku nasadíme clip a hladkou kresbu:
            using (GPainter.GraphicsUse(e.Graphics, clip, GraphicSetting.Smooth))
            {
                // Vykreslíme prvky:
                foreach (LinkInfo linkInfo in this._LinkDict.Values)
                    linkInfo.Draw(e);
            }
        }
        /// <summary>
        /// Metoda najde prostor v absolutních souřadnicích, kam se mají vykreslovat linky:
        /// </summary>
        /// <param name="graphicsBounds"></param>
        /// <returns></returns>
        protected Rectangle GetClip(Rectangle graphicsBounds)
        {
            Rectangle clip = graphicsBounds;

            if (this._OwnerGTable != null)
            {   // Prostor pro data v rámci tabulky:
                Rectangle dataBounds = this._OwnerGTable.GetAbsoluteBoundsForArea(Grid.TableAreaType.RowData);
                clip = Rectangle.Intersect(clip, dataBounds);

                // Pokud v tabulce najdu alespoň jeden sloupec typu Graf (používá časovou osu)...
                var graphColumns = this._OwnerGTable.Columns.Where(c => c.ColumnProperties.UseTimeAxis).ToArray();
                if (graphColumns != null && graphColumns.Length > 0)
                {   // ...pak zmenším prostor clipu ve směru X pouze na prostor daný všemi grafy v tabulce (on nemusí být pouze jeden):
                    Rectangle c0 = graphColumns[0].ColumnHeader.BoundsAbsolute;  // Souřadnice (X) prvního sloupce s grafem
                    Rectangle c1 = (graphColumns.Length == 1 ? c0 : graphColumns[graphColumns.Length - 1].ColumnHeader.BoundsAbsolute); // Souřadnice (X) posledního sloupce s grafem
                    int x = (c0.X > clip.X ? c0.X : clip.X);                     // Left omezit podle prvního sloupce
                    int r = (c1.Right < clip.Right ? c1.Right : clip.Right);     // Right omezit podle posledního sloupce
                    clip.X = x;
                    clip.Width = (r - x);
                }
            }
            else if (this._OwnerGraph != null)
            {   // Prostor pro data v rámci grafu:
                Rectangle graphBounds = this._OwnerGraph.BoundsAbsolute;
                clip = Rectangle.Intersect(clip, graphBounds);
            }

            return clip;
        }
        #endregion
    }
    /// <summary>
    /// Důvod zobrazení Linku
    /// </summary>
    [Flags]
    public enum GTimeGraphLinkMode
    {
        /// <summary>
        /// Není důvod
        /// </summary>
        None = 0,
        /// <summary>
        /// MouseEnter / MouseLeave
        /// </summary>
        Mouse = 1,
        /// <summary>
        /// IsSelected / deselect
        /// </summary>
        Select = 2,
        /// <summary>
        /// Souhrn všech platných hodnot
        /// </summary>
        All = Mouse | Select
    }
    #endregion
    #region class GTimeGraphLink : Datová třída, reprezentující spojení dvou prvků grafu.
    /// <summary>
    /// GTimeGraphLink : Datová třída, reprezentující spojení dvou prvků grafu.
    /// Nejde v pravém smyslu o interaktivní objekt.
    /// </summary>
    public class GTimeGraphLinkItem
    {
        #region Konstrukce a základní data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GTimeGraphLinkItem()
        { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "GraphLink; IdPrev: " + this.ItemIdPrev + "; IdNext: " + this.ItemIdNext + "; LinkType: " + (this.LinkType.HasValue ? this.LinkType.Value.ToString() : "{Null}");
        }
        /// <summary>
        /// ID prvku předchozího
        /// </summary>
        public int ItemIdPrev { get; set; }
        /// <summary>
        /// Vizuální data prvku předchozího
        /// </summary>
        public GTimeGraphItem ItemPrev { get; set; }
        /// <summary>
        /// ID prvku následujícího
        /// </summary>
        public int ItemIdNext { get; set; }
        /// <summary>
        /// Vizuální data prvku následujícího
        /// </summary>
        public GTimeGraphItem ItemNext { get; set; }
        /// <summary>
        /// Typ linky, nezadáno = použije se <see cref="GuiGraphItemLinkType.PrevEndToNextBeginLine"/>
        /// </summary>
        public GuiGraphItemLinkType? LinkType { get; set; }
        /// <summary>
        /// Šířka linky, nezadáno = 1
        /// </summary>
        public int? LinkWidth { get; set; }
        /// <summary>
        /// Barva linky základní.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je větší nebo rovno Prev.End, pak se použije <see cref="LinkColorStandard"/>.
        /// Další barvy viz <see cref="LinkColorWarning"/> a <see cref="LinkColorError"/>
        /// </summary>
        public Color? LinkColorStandard { get; set; }
        /// <summary>
        /// Barva linky varovná.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.End, ale Next.Begin je větší nebo rovno Prev.Begin, pak se použije <see cref="LinkColorWarning"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorError"/>
        /// </summary>
        public Color? LinkColorWarning { get; set; }
        /// <summary>
        /// Barva linky chybová.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.Begin, pak se použije <see cref="LinkColorError"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorWarning"/>
        /// </summary>
        public Color? LinkColorError { get; set; }
        /// <summary>
        /// Data z GUI, nepovinná (zdejší hodnoty jsou separátní)
        /// </summary>
        public GuiGraphLink GuiGraphLink { get; set; }
        #endregion
        #region Obousměrný přístup k prvkům a jejich ID
        /// <summary>
        /// Vrátí ID prvku na dané straně
        /// </summary>
        /// <param name="side">Strana: Negative = Prev; Positive = Next</param>
        /// <returns></returns>
        internal int GetId(Direction side)
        {
            switch (side)
            {
                case Direction.Negative: return this.ItemIdPrev;
                case Direction.Positive: return this.ItemIdNext;
            }
            return 0;
        }
        /// <summary>
        /// Vrátí prvek na dané straně
        /// </summary>
        /// <param name="side">Strana: Negative = Prev; Positive = Next</param>
        /// <returns></returns>
        internal GTimeGraphItem GetItem(Direction side)
        {
            switch (side)
            {
                case Direction.Negative: return this.ItemPrev;
                case Direction.Positive: return this.ItemNext;
            }
            return null;
        }
        /// <summary>
        /// Uloží daný prvek na danou stranu
        /// </summary>
        /// <param name="side">Strana: Negative = Prev; Positive = Next</param>
        /// <param name="item">Prvek</param>
        /// <returns></returns>
        internal void SetItem(Direction side, GTimeGraphItem item)
        {
            switch (side)
            {
                case Direction.Negative:
                    this.ItemPrev = item;
                    break;
                case Direction.Positive:
                    this.ItemNext = item;
                    break;
            }
        }
        #endregion
        #region Klíč linku
        /// <summary>
        /// UInt64 klíč tohoto prvku, obsahuje klíče <see cref="GTimeGraphLinkItem.ItemIdPrev"/> a <see cref="GTimeGraphLinkItem.ItemIdNext"/>
        /// </summary>
        public UInt64 Key { get { return GetLinkKey(this); } }
        /// <summary>
        /// Vrací složené číslo UInt64 obsahující klíče:
        /// v horních čtyřech bytech = <see cref="GTimeGraphLinkItem.ItemIdPrev"/>;
        /// v dolních čtyřech bytech = <see cref="GTimeGraphLinkItem.ItemIdNext"/>;
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        protected static UInt64 GetLinkKey(GTimeGraphLinkItem link)
        {
            return (link != null ? GetKey(link.ItemIdPrev, link.ItemIdNext) : 0);
        }
        /// <summary>
        /// Vrací složené číslo UInt64 obsahující dvě dodaná čísla Int32:
        /// v horních čtyřech  bytech je uloženo číslo a, v dolních čtyřech bytech pak číslo b.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected static UInt64 GetKey(int a, int b)
        {
            ulong ua = ((ulong)(a)) & 0xffffffffL;
            ulong ub = ((ulong)(b)) & 0xffffffffL;
            return (ulong)((ua << 32) | ub);
        }
        #endregion
        #region Kreslení jednoho linku
        /// <summary>
        /// Obsahuje true, pokud se má linka kreslit (je viditelná a má oba objekty Prev i Next)
        /// </summary>
        internal bool NeedDraw { get { return (this.IsLinkTypeVisible && this.ItemPrev != null && this.ItemNext != null); } }
        /// <summary>
        /// Obsahuje true, pokud linka podle jejího typu je viditelná
        /// </summary>
        internal bool IsLinkTypeVisible { get { return (this.LinkType.HasValue && (this.LinkType.Value == GuiGraphItemLinkType.PrevCenterToNextCenter || this.LinkType.Value == GuiGraphItemLinkType.PrevEndToNextBeginLine || this.LinkType.Value == GuiGraphItemLinkType.PrevEndToNextBeginSCurve)); } }
        /// <summary>
        /// Vykreslí tuto jednu linku
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="mode">Důvody zobrazení</param>
        /// <param name="ratio">Poměr průhlednosti: hodnota v rozsahu 0.0 (neviditelná) - 1.0 (plná barva)</param>
        internal void Draw(GInteractiveDrawArgs e, GTimeGraphLinkMode mode, float ratio)
        {
            GuiGraphItemLinkType linkType = (this.LinkType ?? GuiGraphItemLinkType.None);
            switch (linkType)
            {
                case GuiGraphItemLinkType.PrevCenterToNextCenter:
                    this.DrawCenter(e, mode, ratio);
                    break;
                case GuiGraphItemLinkType.PrevEndToNextBeginLine:
                    this.DrawPrevNext(e, mode, ratio, false);
                    break;
                case GuiGraphItemLinkType.PrevEndToNextBeginSCurve:
                    this.DrawPrevNext(e, mode, ratio, true);
                    break;
            }
        }
        /// <summary>
        /// Vykreslí přímou linku nebo křivku Prev.Center to Next.Center
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="mode">Důvody zobrazení</param>
        /// <param name="ratio">Poměr průhlednosti: hodnota v rozsahu 0.0 (neviditelná) - 1.0 (plná barva)</param>
        protected void DrawCenter(GInteractiveDrawArgs e, GTimeGraphLinkMode mode, float ratio)
        {
            GTimeGraph graph = (this.ItemNext != null ? this.ItemNext.Graph : (this.ItemPrev != null ? this.ItemPrev.Graph : null));
            RelationState relationState = GetRelationState(this.ItemPrev, this.ItemNext);
            Color color1 = this.GetColorForState(relationState, graph);

            Point? prevPoint = GetPoint(this.ItemPrev, RectangleSide.CenterX | RectangleSide.CenterY, true);
            Point? nextPoint = GetPoint(this.ItemNext, RectangleSide.CenterX | RectangleSide.CenterY, true);
            if (!(prevPoint.HasValue && nextPoint.HasValue)) return;

            GPainter.DrawLinkLine(e.Graphics, prevPoint.Value, nextPoint.Value, color1, this.LinkWidth, System.Drawing.Drawing2D.LineCap.Round, System.Drawing.Drawing2D.LineCap.ArrowAnchor, ratio);
        }
        /// <summary>
        /// Vykreslí přímou linku nebo S křivku { Prev.End to Next.Begin }
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="mode">Důvody zobrazení</param>
        /// <param name="ratio">Poměr průhlednosti: hodnota v rozsahu 0.0 (neviditelná) - 1.0 (plná barva)</param>
        /// <param name="asSCurve"></param>
        protected void DrawPrevNext(GInteractiveDrawArgs e, GTimeGraphLinkMode mode, float ratio, bool asSCurve)
        {
            GTimeGraph graph = (this.ItemNext != null ? this.ItemNext.Graph : (this.ItemPrev != null ? this.ItemPrev.Graph : null));
            RelationState relationState = GetRelationState(this.ItemPrev, this.ItemNext);
            Color color1 = this.GetColorForState(relationState, graph);

            Point? prevPoint = GetPoint(this.ItemPrev, RectangleSide.MiddleRight, true);
            Point? nextPoint = GetPoint(this.ItemNext, RectangleSide.MiddleLeft, true);

            using (System.Drawing.Drawing2D.GraphicsPath graphicsPath = GPainter.CreatePathLinkLine(prevPoint, nextPoint, asSCurve))
            {
                GPainter.DrawLinkPath(e.Graphics, graphicsPath, color1, this.LinkWidth, System.Drawing.Drawing2D.LineCap.Round, System.Drawing.Drawing2D.LineCap.ArrowAnchor, ratio);
            }
        }
        /// <summary>
        /// Vrátí požadovaný bod, nacházející se na daném místě absolutní souřadnice daného prvku.
        /// Pokud je prvek neviditelný (on, nebo kterýkoli z jeho Parentů), může vrátit null pokud je požadavek "onlyVisible" = true.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="side"></param>
        /// <param name="onlyVisible">true = vracet bod pouze pro objekt, který může být viditelný (z hlediska Is.Visible jeho a všech jeho Parentů)</param>
        /// <returns></returns>
        protected static Point? GetPoint(InteractiveObject item, RectangleSide side, bool onlyVisible)
        {
            if (item == null) return null;
            BoundsInfo boundsInfo = item.BoundsInfo;
            if (onlyVisible && !boundsInfo.CurrentIsVisible) return null;
            Rectangle absBounds = boundsInfo.CurrentAbsBounds;
            return absBounds.GetPoint(side);
        }
        /// <summary>
        /// Vrací stav popisující vztah času Prev a Next
        /// </summary>
        /// <param name="itemPrev"></param>
        /// <param name="itemNext"></param>
        /// <returns></returns>
        protected static RelationState GetRelationState(GTimeGraphItem itemPrev, GTimeGraphItem itemNext)
        {
            if (itemPrev == null || itemNext == null) return RelationState.Standard;
            TimeRange timePrev = itemPrev.Item.Time;
            TimeRange timeNext = itemNext.Item.Time;
            if (timeNext.Begin.Value >= timePrev.End.Value) return RelationState.Standard;
            if (timeNext.Begin.Value >= timePrev.Begin.Value) return RelationState.Warning;
            return RelationState.Error;
        }
        /// <summary>
        /// Vrací barvu pro daný čas
        /// </summary>
        /// <param name="state"></param>
        /// <param name="graph"></param>
        /// <returns></returns>
        protected Color GetColorForState(RelationState state, GTimeGraph graph = null)
        {
            switch (state)
            {
                case RelationState.Warning:
                    return (this.LinkColorWarning.HasValue ? this.LinkColorWarning.Value :
                           (graph != null && graph.LinkColorWarning.HasValue ? graph.LinkColorWarning.Value : Skin.Graph.LinkColorWarning));
                case RelationState.Error:
                    return (this.LinkColorError.HasValue ? this.LinkColorError.Value :
                           (graph != null && graph.LinkColorError.HasValue ? graph.LinkColorError.Value : Skin.Graph.LinkColorError));
                case RelationState.Standard:
                default:
                    return (this.LinkColorStandard.HasValue ? this.LinkColorStandard.Value :
                           (graph != null && graph.LinkColorStandard.HasValue ? graph.LinkColorStandard.Value : Skin.Graph.LinkColorStandard));
            }
        }
        /// <summary>
        /// Vztah prvků Prev - Next z hlediska času a určení barvy
        /// </summary>
        protected enum RelationState
        {
            /// <summary>
            /// Neurčen
            /// </summary>
            None,
            /// <summary>
            /// Standardní, kdy Next.Begin je v nebo za časem Prev.End
            /// </summary>
            Standard,
            /// <summary>
            /// Varování, kdy Next.Begin je v nebo za časem Prev.Begin, ale dříve než Prev.End
            /// </summary>
            Warning,
            /// <summary>
            /// Chyba, kdy Next.Begin je před časem Prev.Begin
            /// </summary>
            Error
        }
        #endregion
    }
    #endregion
    #region enum GGraphControlPosition
    /// <summary>
    /// Pozice GUI controlu pro prvek grafu
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
