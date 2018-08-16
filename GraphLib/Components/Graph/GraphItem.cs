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
        /// <param name="data">Prvek grafu, ten v sobě obsahuje data</param>
        /// <param name="parent">Parent prvku, GUI container</param>
        public GTimeGraphItem(ITimeGraphItem data, IInteractiveParent parent, GTimeGraphGroup group, GGraphControlPosition position)
            : base()
        {
            this._Owner = data;
            this._Parent = parent;
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
        private IInteractiveParent _Parent;
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
        protected override void PrepareToolTip(GInteractiveChangeStateArgs e)
        {
            this._Group.GraphItemPrepareToolTip(e, this._Owner, this._Position);
        }
        protected override void AfterStateChangedRightClick(GInteractiveChangeStateArgs e)
        {
            this._Group.GraphItemRightClick(e, this._Owner, this._Position);
        }
        protected override void AfterStateChangedLeftDoubleClick(GInteractiveChangeStateArgs e)
        {
            this._Group.GraphItemLeftDoubleClick(e, this._Owner, this._Position);
        }
        protected override void AfterStateChangedLeftLongClick(GInteractiveChangeStateArgs e)
        {
            this._Group.GraphItemLeftLongClick(e, this._Owner, this._Position);
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
        #region Přetahování (Drag & Drop)
        public override bool IsDragEnabled
        {
            get { return (this._Position == GGraphControlPosition.Group); }
            set { }
        }
        protected override void DragAction(GDragActionArgs e)
        {
            base.DragAction(e);
        }
        protected override void DragThisOverBounds(GDragActionArgs e, Rectangle targetRelativeBounds)
        {
            var item = e.FindItemAtPoint(e.MouseCurrentAbsolutePoint.Value);
            var graph = SearchForParent(item, typeof(GTimeGraph));

            targetRelativeBounds.Y = this.Bounds.Y;
            base.DragThisOverBounds(e, targetRelativeBounds);
        }
        protected override void DragThisDropToBounds(GDragActionArgs e, Rectangle boundsTarget)
        {
            base.DragThisDropToBounds(e, boundsTarget);
        }
        protected override void DragThisOverEnd(GDragActionArgs e)
        {
            base.DragThisOverEnd(e);
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

            Color backColor = this.BackColor;
            Color borderColor = this.BorderColor;
            if (this.IsSelected) borderColor = Color.DarkGreen;
            int lineWidth = (this.IsSelected ? 2 : 1);
            if (boundsAbsolute.Width <= 2)
            {
                e.Graphics.FillRectangle(Skin.Brush(borderColor), boundsAbsolute);
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
                e.Graphics.DrawRectangle(Skin.Pen(borderColor, (float)lineWidth), boundsLineAbsolute);
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
