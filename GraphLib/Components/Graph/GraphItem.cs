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
            this._InteractiveParent = interactiveParent;
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
            return this._Position.ToString() + "; " + base.ToString();
        }
        /// <summary>
        /// Vlastník tohoto grafického prvku = datový prvek grafu
        /// </summary>
        ITimeGraphItem IOwnerProperty<ITimeGraphItem>.Owner { get { return this._Owner; } set { this._Owner = value; } }
        private ITimeGraphItem _Owner;
        /// <summary>
        /// Parent tohoto grafického prvku = GUI prvek, v němž je tento grafický prvek hostován
        /// </summary>
        private IInteractiveParent _InteractiveParent;
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
            if (this.BoundsIsValid) return;
            this.Group.CalculateBounds();
            this.BoundsIsValid = true;
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
        /// Vlastní datový prvek grafu
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
            this.PrepareLinks();
            base.AfterStateChangedMouseEnter(e);
        }
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = MouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseLeave(GInteractiveChangeStateArgs e)
        {
            this.ResetLinks();
            base.AfterStateChangedMouseLeave(e);
        }
        /// <summary>
        /// Metoda zajistí provedení Select pro moji Parent grupu (pokud já jsem Item)
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            if (this._Position == GGraphControlPosition.Item)
                this._Group.GControl.ChangeSelect();
            base.AfterStateChangedLeftClick(e);
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
        /// a) Selectovat lze jen Group prvky, nikoli Item;
        /// b) Selectovat lze jen ty Item prvky, které je možno nějak editovat (posunout...)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool _GetSelectable(bool value)
        {
            return (this._Position == GGraphControlPosition.Group && this._Group.IsDragEnabled);
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
            if (e.DrawLayer == GInteractiveDrawLayer.Dynamic)
                this.DrawLinks(e, absoluteBounds, absoluteVisibleBounds, drawMode);
            else
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

                Rectangle[] boundsParts = _CreateBounds(boundsAbsolute, this._Position, false, false, false);
                float? effect3D = this._GetEffect3D(false);
                GPainter.DrawEffect3D(e.Graphics, boundsParts[0], color, System.Windows.Forms.Orientation.Horizontal, effect3D, null);
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
            // Stav IsSelected a IsFramed budeme vždy přebírat z GUI prvku Grupy, protože Select a Framed a Activated se řeší na úrovni Grupy:
            GTimeGraphItem groupItem = (this._Position == GGraphControlPosition.Group ? this :
                                        this._Position == GGraphControlPosition.Item ? this._Group.GControl : null);

            bool isSelected = (groupItem != null ? groupItem.IsSelected : false);
            bool isFramed = (groupItem != null ? groupItem.IsFramed : false);
            bool isActivated = (groupItem != null ? groupItem.IsActivated : false);
            bool hasBorder = (isSelected | isFramed);

            Rectangle[] boundsParts = _CreateBounds(boundsAbsolute, this._Position, this.IsFirstItem, this.IsLastItem, hasBorder);

            Color color;
            Color? itemBackColor = this.ItemBackColor;
            Color? borderColor = null;

            // Vykreslit pozadí pod prvkem:
            bool drawSelect = true;
            if (itemBackColor.HasValue)
            {
                color = this._Group.GetColorWithOpacity(itemBackColor.Value, e.DrawLayer, drawMode, false, true);
                System.Drawing.Drawing2D.HatchStyle? backStyle = this._Owner.BackStyle;
                if (backStyle.HasValue)
                {   // Máme-li BackStyle : neřešíme interaktivitu myši, ani vykreslení 3D efektu:
                    using (System.Drawing.Drawing2D.HatchBrush hb = new System.Drawing.Drawing2D.HatchBrush(backStyle.Value, color, Color.Transparent))
                    {
                        e.Graphics.FillRectangle(hb, boundsAbsolute);
                    }
                }
                else
                {   // Nemáme-li BackStyle : řešíme interaktivitu myši i vykreslení 3D efektu:
                    float? effect3D = this._GetEffect3D(true);
                    GPainter.DrawEffect3D(e.Graphics, boundsParts[0], color, System.Windows.Forms.Orientation.Horizontal, effect3D, null);

                    borderColor = (isSelected ? Skin.Graph.ElementSelectedLineColor :
                                  (isFramed ? Skin.Graph.ElementFramedLineColor : color));
                    Color colorTop = Skin.Modifiers.GetColor3DBorderLight(borderColor.Value, 0.50f);
                    Color colorBottom = Skin.Modifiers.GetColor3DBorderDark(borderColor.Value, 0.50f);
                    e.Graphics.FillRectangle(Skin.Brush(colorTop), boundsParts[2]);
                    e.Graphics.FillRectangle(Skin.Brush(colorBottom), boundsParts[4]);
                    if (!hasBorder)
                    {   // Běžné okraje prvku (3D efekt na krajních prvcích):
                        if (this.IsFirstItem)
                            e.Graphics.FillRectangle(Skin.Brush(colorTop), boundsParts[1]);
                        if (this.IsLastItem)
                            e.Graphics.FillRectangle(Skin.Brush(colorBottom), boundsParts[3]);
                    }

                    if (hasBorder && borderColor.HasValue)
                    {   // Zvýrazněné okraje prvku (Selected, Framed na krajních prvcích):
                        if (this.IsFirstItem)
                            e.Graphics.FillRectangle(Skin.Brush(borderColor.Value), boundsParts[1]);
                        if (this.IsLastItem)
                            e.Graphics.FillRectangle(Skin.Brush(borderColor.Value), boundsParts[3]);
                    }

                    drawSelect = false;
                }
            }

            // Vykreslit orámování prvku:
            if (drawSelect && hasBorder)
            {
                Color? itemLineColor = (isSelected ? (Color?)Skin.Graph.ElementSelectedLineColor : this.ItemLineColor);
                if (itemLineColor.HasValue)
                {
                    color = this._Group.GetColorWithOpacity(itemLineColor.Value, e.DrawLayer, drawMode, false, false);
                    var brush = Skin.Brush(color);
                    e.Graphics.FillRectangle(brush, boundsParts[1]);
                    e.Graphics.FillRectangle(brush, boundsParts[2]);
                    e.Graphics.FillRectangle(brush, boundsParts[3]);
                    e.Graphics.FillRectangle(brush, boundsParts[4]);
                }
            }

            // Ratio:

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
        /// Zkusí najít vztahy ke kreslení.
        /// Pokud nějaké najde, budou uloženy v <see cref="Links"/>.
        /// Jakmile v <see cref="Links"/> bude něco jiného než null, pak <see cref="StandardDrawToLayer"/> bude vracet i vrstvu <see cref="GInteractiveDrawLayer.Dynamic"/>,
        /// a tím se začne volat metoda <see cref="DrawLinks(GInteractiveDrawArgs, Rectangle, Rectangle, DrawItemMode)"/> = vykreslení linek.
        /// </summary>
        protected void PrepareLinks()
        {
            // Pokud this je na pozici Item, a naše grupa (this.Group) už má nalezené linky, pak je nebudeme opakovaně hledat pro prvek:
            if (this.Position == GGraphControlPosition.Item && this.Group.GControl.Links != null) return;

            CreateLinksArgs args = new CreateLinksArgs(this.Graph, this.Group, this.Item, this.Position);
            this.Graph.DataSource.CreateLinks(args);
            this.Links = args.Links;
        }
        /// <summary>
        /// Resetuje kreslené vztahy. Odteď se nebudou kreslit.
        /// </summary>
        protected void ResetLinks()
        {
            this.Links = null;
        }
        /// <summary>
        /// Vykreslí vztahy
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected void DrawLinks(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            GTimeGraphLink[] links = this.Links;
            if (links == null) return;

            Rectangle clipBounds = this.GetLinksAbsoluteClip();
            e.GraphicsClipWith(clipBounds, false, true);
            using (GPainter.GraphicsUseSmooth(e.Graphics))
            {
                foreach (GTimeGraphLink link in links)
                {
                    if (link != null && link.ItemPrev != null && link.ItemNext != null && link.LinkType.HasValue)
                    {
                        switch (link.LinkType.Value)
                        {
                            case GuiGraphItemLinkType.PrevEndToNextBeginLine:
                                this.DrawLinksPNLine(e, link);
                                break;
                            case GuiGraphItemLinkType.PrevEndToNextBeginSCurve:
                                this.DrawLinksPNSCurve(e, link);
                                break;

                        }
                    }
                }
            }
        }
        /// <summary>
        /// Vykreslí přímou linku 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="link"></param>
        protected void DrawLinksPNLine(GInteractiveDrawArgs e, GTimeGraphLink link)
        {
            Rectangle prevBounds = link.ItemPrev.BoundsAbsolute;
            Rectangle nextBounds = link.ItemNext.BoundsAbsolute;
            Point prevPoint = new Point(prevBounds.Right - 1, prevBounds.Y + prevBounds.Height / 2);
            Point nextPoint = new Point(nextBounds.X, nextBounds.Y + nextBounds.Height / 2);
            e.Graphics.DrawLine(Skin.Pen(Color.Red), prevPoint, nextPoint);
        }
        /// <summary>
        /// Vykreslí S-křivkovou linku 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="link"></param>
        protected void DrawLinksPNSCurve(GInteractiveDrawArgs e, GTimeGraphLink link)
        {
            Rectangle prevBounds = link.ItemPrev.BoundsAbsolute;
            Rectangle nextBounds = link.ItemNext.BoundsAbsolute;
            Point prevPoint = new Point(prevBounds.Right - 1, prevBounds.Y + prevBounds.Height / 2);
            Point nextPoint = new Point(nextBounds.X, nextBounds.Y + nextBounds.Height / 2);

            int diffY = (nextPoint.Y - prevPoint.Y);
            if (diffY < 0) diffY = -diffY;
            int addX = (nextPoint.X - prevPoint.X) / 4;
            int addY = diffY / 3;
            if (addX < 16) addX = 16;
            else if (addX < addY) addX = addY;
            Point prevTarget = new Point(prevPoint.X + addX, prevPoint.Y);
            Point nextTarget = new Point(nextPoint.X - addX, nextPoint.Y);

            Color color1 = (link.LinkColor.HasValue ? link.LinkColor.Value : Skin.Graph.LinkColor);
            Color color3 = color1.Morph(Color.Black, 0.667f);
            Pen pen = Skin.Pen(color3, 3f, startCap: System.Drawing.Drawing2D.LineCap.Round, endCap: System.Drawing.Drawing2D.LineCap.Round);
            e.Graphics.DrawBezier(pen, prevPoint, prevTarget, nextTarget, nextPoint);

            pen = Skin.Pen(color1);
            e.Graphics.DrawBezier(Skin.Pen(color1), prevPoint, prevTarget, nextTarget, nextPoint);
        }
        /// <summary>
        /// Vrátí Rectangle reprezentující rozumný clip pro kreslení linků
        /// </summary>
        /// <returns></returns>
        protected Rectangle GetLinksAbsoluteClip()
        {
            // 1. Souřadnice grafu, absolutní:
            BoundsInfo boundsInfo = BoundsInfo.CreateForChild(this.Graph);
            Rectangle linkAbsoluteBounds = boundsInfo.CurrentAbsVisibleBounds;

            // 2. Pokud je graf v tabulce, pak najdu prostor dat v tabulce (RowData):
            Grid.GTable gTable = this.Graph.SearchForParent(typeof(Grid.GTable)) as Grid.GTable;
            if (gTable != null)
            {
                Rectangle rowDataBounds = gTable.GetAbsoluteBoundsForArea(Grid.TableAreaType.RowData);
                // a prostor pro Linky zvětším v ose Y na celou oblast dat tabulky:
                linkAbsoluteBounds.Y = rowDataBounds.Y;
                linkAbsoluteBounds.Height = rowDataBounds.Height;
            }
            return linkAbsoluteBounds;
        }
        /// <summary>
        /// Pole vztahů, které kreslíme
        /// </summary>
        protected GTimeGraphLink[] Links;
        /// <summary>
        /// Vrstvy pro běžné kreslení: Obsahuje vrstvu Standard, plus vrstvu Dynamic = pokud pole <see cref="Links"/> není null.
        /// </summary>
        protected override GInteractiveDrawLayer StandardDrawToLayer { get { return GInteractiveDrawLayer.Standard | (this.Links != null ? GInteractiveDrawLayer.Dynamic : GInteractiveDrawLayer.None); } }
        #endregion
    }
    #region class GTimeGraphLink
    /// <summary>
    /// Třída reprezentující spojení dvou prvků grafu.
    /// </summary>
    public class GTimeGraphLink
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GTimeGraphLink()
        { }
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
        /// Barva linky
        /// </summary>
        public Color? LinkColor { get; set; }
        /// <summary>
        /// Data z GUI, nepovinná (zdejší hodnoty jsou separátní)
        /// </summary>
        public GuiGraphLink GuiGraphLink { get; set; }
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
