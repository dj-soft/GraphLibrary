using Asol.Tools.WorkScheduler.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Components.Graph
{
    #region class GTimeGraphItem : vizuální a interaktivní control, který se vkládá do implementace ITimeGraphItem
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
            this.IsSelectable = true;
        }
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
        #region Veřejná data
        /// <summary>
        /// Graf, do něhož tento vizuální interaktivní prvek patří.
        /// Může se lišit od našeho vizuálního parenta, protože this prvek může být Child prvkem Grupy, a ta je Child prvkem Grafu.
        /// Nicméně přes zdejší Grupu je vždy možno najít Graf, v němž je this prvek zobrazován.
        /// </summary>
        internal GTimeGraph Graph { get { return this._Group.Graph; } }
        /// <summary>
        /// Souřadnice na ose X. Jednotkou jsou pixely.
        /// Tato osa je společná jak pro virtuální, tak pro reálné souřadnice.
        /// Hodnota 0 odpovídá prvnímu viditelnému pixelu vlevo.
        /// </summary>
        public Int32Range CoordinateX { get; set; }
        /// <summary>
        /// Barva pozadí tohoto prvku
        /// </summary>
        public override Color BackColor
        {
            get
            {
                if (this.BackColorUser.HasValue) return this.BackColorUser.Value;
                if (this._Owner != null && this._Owner.BackColor.HasValue) return this._Owner.BackColor.Value;
                return Skin.Graph.ElementBackColor;
            }
            set { }
        }
        /// <summary>
        /// Barva okraje prvku
        /// </summary>
        public Color BorderColor
        {
            get
            {
                if (this.BorderColorUser.HasValue) return this.BorderColorUser.Value;
                if (this._Owner != null && this._Owner.BorderColor.HasValue) return this._Owner.BorderColor.Value;
                return Skin.Graph.ElementBorderColor;
            }
        }
        public Color? BorderColorUser { get; set; }
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
        /// Metoda zajistí přípravu ToolTipu pro zdejší prvek (data) na dané pozici (position).
        /// </summary>
        /// <param name="e"></param>
        protected override void PrepareToolTip(GInteractiveChangeStateArgs e)
        {
            this.Graph.GraphItemPrepareToolTip(e, this._Group, this._Owner, this._Position);
        }
        /// <summary>
        /// Metoda zajistí zpracování události RightCLick na grafickém prvku (data) na dané pozici (position).
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedRightClick(GInteractiveChangeStateArgs e)
        {
            this.Graph.GraphItemRightClick(e, this._Group, this._Owner, this._Position);
        }
        /// <summary>
        /// Metoda zajistí zpracování události LeftDoubleCLick na grafickém prvku (data) na dané pozici (position).
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedLeftDoubleClick(GInteractiveChangeStateArgs e)
        {
            this.Graph.GraphItemLeftDoubleClick(e, this._Group, this._Owner, this._Position);
        }
        /// <summary>
        /// Metoda zajistí zpracování události LeftLongCLick na grafickém prvku (data) na dané pozici (position).
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedLeftLongClick(GInteractiveChangeStateArgs e)
        {
            this.Graph.GraphItemLeftLongClick(e, this._Group, this._Owner, this._Position);
        }
        public override bool IsSelected
        {
            get { return base.IsSelected; }
            set
            {
                bool oldValue = base.IsSelected;
                bool newValue = value;
                if (oldValue != newValue)
                {
                    base.IsSelected = newValue;
                    this.Repaint();
                }
            }
        }
        #endregion
        #region Přetahování (Drag & Drop) : týká se výhradně prvků typu Group!
        /// <summary>
        /// Vrací true, pokud daný prvek může být přemísťován.
        /// </summary>
        public override bool IsDragEnabled
        {
            get { return (this._Position == GGraphControlPosition.Group && this._Group.IsDragEnabled); }
            set { }
        }
        protected override void DragThisStartBounds(GDragActionArgs e, Rectangle targetRelativeBounds)
        {
            this.DragDropGroupCallSource(e, ref targetRelativeBounds);
            base.DragThisStartBounds(e, targetRelativeBounds);
        }
        protected override void DragThisOverBounds(GDragActionArgs e, Rectangle targetRelativeBounds)
        {
            this.DragDropGroupCallSource(e, ref targetRelativeBounds);
            base.DragThisOverBounds(e, targetRelativeBounds);
        }
        protected override void DragThisDropToBounds(GDragActionArgs e, Rectangle targetRelativeBounds)
        {
            this.DragDropGroupCallSource(e, ref targetRelativeBounds);
            base.DragThisDropToBounds(e, targetRelativeBounds);
        }
        protected override void DragThisOverEnd(GDragActionArgs e)
        {
            Rectangle targetRelativeBounds = Rectangle.Empty;
            this.DragDropGroupCallSource(e, ref targetRelativeBounds);
            base.DragThisOverEnd(e);
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
            IInteractiveItem item = e.FindItemAtPoint(e.MouseCurrentAbsolutePoint.Value);
            Rectangle targetAbsoluteBounds = e.BoundsInfo.GetAbsBounds(targetRelativeBounds);

            // Sestavím argument (pro this prvek) a doplním do něj údaje o dalších prvcích:
            ItemDragDropArgs args = new ItemDragDropArgs(e, this.Graph, this._Group, this._Owner, this._Position, targetAbsoluteBounds);
            args.ParentGraph = this.Graph;
            args.ParentTable = this.SearchForParent(typeof(Grid.GTable)) as Grid.GTable;
            args.TargetItem = item;
            args.TargetGraph = SearchForItem(item, true, typeof(GTimeGraph)) as GTimeGraph;
            args.TargetTable = SearchForItem(item, true, typeof(Grid.GTable)) as Grid.GTable;
            args.IsFinalised = true;

            // Předem připravím "defaultní" výsledky (ale datový zdroj, který se vyvolá v následujícím řádku) má plnou moc nastavit libovolné výsledky po svém:
            this.DragPrepareDefaultValidity(args);

            // Zavolám datový zdroj, ten vyhodnotí zda se může prvek posunout, nebo na jaké souřadnice se může posunout, a určí vzhled kreslení (disabled?):
            // On by mohl i zařídit "scrollování" v čase (osa X => TimeAxis) nebo v místě (osa Y => řádek, kam položku přesunout):
            this.Graph.DragDropGroupCallSource(args);

            // Převezmu výsledky (buď defaultní, nebo modifikované datovým zdrojem):
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
        #region Kreslení prvku
        /// <summary>
        /// Vykreslí this prvek
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        /// <param name="drawMode">Režim kreslení (pomáhá řešit Drag & Drop procesy)</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            this._Owner.Draw(e, absoluteBounds, drawMode);
        }
        /// <summary>
        /// Vykreslování "Přes Child prvky": pokud this prvek vykresluje Grupu, pak ano!
        /// </summary>
        protected override bool NeedDrawOverChilds { get { return (this._Position == GGraphControlPosition.Group); } set { } }
        /// <summary>
        /// Metoda volaná pro vykreslování "Přes Child prvky": převolá se grupa.
        /// </summary>
        /// <param name="e"></param>
        protected override void DrawOverChilds(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            if (this._Position == GGraphControlPosition.Group)
                this._Group.DrawOverChilds(e, boundsAbsolute, drawMode);
        }
        /// <summary>
        /// Metoda je volaná pro vykreslení jedné položky grafu.
        /// </summary>
        /// <param name="e">Standardní data pro kreslení</param>
        /// <param name="boundsAbsolute">Absolutní souřadnice tohoto prvku</param>
        /// <param name="drawMode">Režim kreslení (má význam pro akce Drag & Drop)</param>
        public void DrawItem(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode)
        {
            this.DrawItem(e, boundsAbsolute, drawMode, null);
        }
        /// <summary>
        /// Metoda je volaná pro vykreslení prvku.
        /// Implementátor může bez nejmenších obav převolat <see cref="GControl"/>.<see cref="GTimeGraphItem.DrawItem(GInteractiveDrawArgs, Rectangle, DrawItemMode, int?)"/> Draw
        /// </summary>
        /// <param name="e">Standardní data pro kreslení</param>
        /// <param name="boundsAbsolute">Absolutní souřadnice tohoto prvku</param>
        /// <param name="drawMode">Režim kreslení (má význam pro akce Drag & Drop)</param>
        /// <param name="backColor">Explicitně definovaná barva pozadí</param>
        /// <param name="borderColor">Explicitně definovaná barva okraje</param>
        /// <param name="enlargeBounds">Změna rozměru Bounds ve všech směrech</param>
        public void DrawItem(GInteractiveDrawArgs e, Rectangle boundsAbsolute, DrawItemMode drawMode, int? enlargeBounds)
        {
            if (boundsAbsolute.Height <= 0 || boundsAbsolute.Width < 0) return;
            if (enlargeBounds.HasValue)
                boundsAbsolute = boundsAbsolute.Enlarge(enlargeBounds.Value);
            if (boundsAbsolute.Width < 1)
                boundsAbsolute.Width = 1;

            Color backColor = this._Group.GetColorWithOpacity(this.BackColor, e);
            Color lineColor = this._Group.GetColorWithOpacity((this.IsSelected ? Color.DarkGreen : this.BorderColor), e);
            int lineWidth = (this.IsSelected ? 2 : 1);
            if (boundsAbsolute.Width <= 2)
            {
                e.Graphics.FillRectangle(Skin.Brush(lineColor), boundsAbsolute);
            }
            else
            {
                System.Drawing.Drawing2D.HatchStyle? backStyle = this._Owner.BackStyle;
                if (backStyle.HasValue)
                {
                    using (System.Drawing.Drawing2D.HatchBrush hb = new System.Drawing.Drawing2D.HatchBrush(backStyle.Value, backColor, Color.Transparent))
                    {
                        e.Graphics.FillRectangle(hb, boundsAbsolute);
                    }
                }
                else
                {
                    e.Graphics.FillRectangle(Skin.Brush(backColor), boundsAbsolute);
                }

                Rectangle boundsLineAbsolute = boundsAbsolute.Enlarge(1 - lineWidth, 1 - lineWidth, -lineWidth, -lineWidth);
                e.Graphics.DrawRectangle(Skin.Pen(lineColor, (float)lineWidth), boundsLineAbsolute);
            }
        }
        /// <summary>
        /// Volba, zda metoda <see cref="Repaint()"/> způsobí i vyvolání metody <see cref="Parent"/>.<see cref="IInteractiveParent.Repaint"/>.
        /// </summary>
        protected override RepaintParentMode RepaintParent { get { return RepaintParentMode.Always; } }
        #endregion
    }
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
