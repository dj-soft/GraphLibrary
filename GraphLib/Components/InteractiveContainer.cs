using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Abstract ancestor for interactive item, which contain collection of IInteractiveItem
    /// </summary>
    public class InteractiveContainer : InteractiveObject, IInteractiveItem, IInteractiveParent
    {
        #region Constructor
        /// <summary>
        /// InteractiveContainer constructor : create ItemList
        /// </summary>
        public InteractiveContainer() : base()
        {
            this._ChildList = new EList<IInteractiveItem>();
            this._ChildList.ItemAddBefore += new EList<IInteractiveItem>.EListEventBeforeHandler(_ItemList_ItemAddBefore);
            this._ChildList.ItemAddAfter += new EList<IInteractiveItem>.EListEventAfterHandler(_ItemList_ItemAddAfter);
            this._ChildList.ItemRemoveBefore += new EList<IInteractiveItem>.EListEventBeforeHandler(_ItemList_ItemRemoveBefore);
            this._ChildList.ItemRemoveAfter += new EList<IInteractiveItem>.EListEventAfterHandler(_ItemList_ItemRemoveAfter);
        }
        /// <summary>
        /// ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return base.ToString() + "; " + this._ChildList.Count.ToString() + " child items";
        }
        #endregion
        #region Items Add/Remove events
        /// <summary>
        /// Zajistí akce před přidáním prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void _ItemList_ItemAddBefore(object sender, EList<IInteractiveItem>.EListBeforeEventArgs args)
        {
            args.Item.Parent = this;
            this.OnItemAddBefore(args);
            if (this.ItemAddBefore != null)
                this.ItemAddBefore(this, args);
        }
        /// <summary>
        /// Háček volaný před přidáním prvku
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemAddBefore(EList<IInteractiveItem>.EListBeforeEventArgs args) { }
        /// <summary>
        /// Event volaný před přidáním prvku
        /// </summary>
        public event EList<IInteractiveItem>.EListEventBeforeHandler ItemAddBefore;
        /// <summary>
        /// Zajistí akce po přidání prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void _ItemList_ItemAddAfter(object sender, EList<IInteractiveItem>.EListAfterEventArgs args)
        {
            this.OnItemAddAfter(args);
            if (this.ItemAddAfter != null)
                this.ItemAddAfter(this, args);
        }
        /// <summary>
        /// Háček volaný po přidání prvku
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemAddAfter(EList<IInteractiveItem>.EListAfterEventArgs args) { }
        /// <summary>
        /// Event volaný po přidání prvku
        /// </summary>
        public event EList<IInteractiveItem>.EListEventAfterHandler ItemAddAfter;
        /// <summary>
        /// Zajistí akce před odebráním prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void _ItemList_ItemRemoveBefore(object sender, EList<IInteractiveItem>.EListBeforeEventArgs args)
        {
            this.OnItemRemoveBefore(args);
            if (this.ItemRemoveBefore != null)
                this.ItemRemoveBefore(this, args);
        }
        /// <summary>
        /// Háček volaný před odebráním prvku
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemRemoveBefore(EList<IInteractiveItem>.EListBeforeEventArgs args) { }
        /// <summary>
        /// Event volaný před odebráním prvku
        /// </summary>
        public event EList<IInteractiveItem>.EListEventBeforeHandler ItemRemoveBefore;
        /// <summary>
        /// Zajistí akce po odebrání prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void _ItemList_ItemRemoveAfter(object sender, EList<IInteractiveItem>.EListAfterEventArgs args)
        {
            this.OnItemRemoveAfter(args);
            if (this.ItemRemoveAfter != null)
                this.ItemRemoveAfter(this, args);
            args.Item.Parent = null;
        }
        /// <summary>
        /// Háček volaný po odebrání prvku
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemRemoveAfter(EList<IInteractiveItem>.EListAfterEventArgs args) { }
        /// <summary>
        /// Event volaný po odebrání prvku
        /// </summary>
        public event EList<IInteractiveItem>.EListEventAfterHandler ItemRemoveAfter;

        #endregion
        #region Items
        /// <summary>
        /// Child prvky tohoto prvku
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { return this.ChildList; } }
        /// <summary>
        /// Interactive items in this container.
        /// Any collection can be stored.
        /// Set of value does not trigger Draw().
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IEnumerable<IInteractiveItem> Items
        {
            get { return this.ChildList; }
            set
            {
                this._ChildList.ClearSilent();
                this._ChildList.AddRangeSilent(value);
            }
        }
        /// <summary>
        /// Add one interactive item.
        /// Does not trigger Draw().
        /// </summary>
        /// <param name="item"></param>
        public virtual void AddItem(IInteractiveItem item)
        {
            this.ChildList.Add(item);
        }
        /// <summary>
        /// Add more interactive items.
        /// Does not trigger Draw().
        /// </summary>
        /// <param name="items"></param>
        public void AddItems(params IInteractiveItem[] items)
        {
            this.ChildList.AddRange(items);
        }
        /// <summary>
        /// Add more interactive items.
        /// Does not trigger Draw().
        /// </summary>
        /// <param name="items"></param>
        public void AddItems(IEnumerable<IInteractiveItem> items)
        {
            this.ChildList.AddRange(items);
        }
        /// <summary>
        /// Vymaže všechny prvky z pole Items.
        /// </summary>
        public virtual void ClearItems()
        {
            this.ChildList.Clear();
        }
        /// <summary>
        /// Soupis Child prvků
        /// </summary>
        protected EList<IInteractiveItem> ChildList
        {
            get
            {
                if (this._ChildList == null)
                {
                    this._ChildList = new EList<IInteractiveItem>();
                    this._ChildList.ItemAddAfter += _ChildList_ItemAddAfter;
                    this._ChildList.ItemRemoveAfter += _ChildList_ItemRemoveAfter;
                }
                return this._ChildList;
            }
        }
        private void _ChildList_ItemAddAfter(object sender, EList<IInteractiveItem>.EListAfterEventArgs args)
        {
            args.Item.Parent = this;
        }
        private void _ChildList_ItemRemoveAfter(object sender, EList<IInteractiveItem>.EListAfterEventArgs args)
        {
            args.Item.Parent = null;
        }
        private EList<IInteractiveItem> _ChildList;
        #endregion
        #region Interactive property and methods
       
        #endregion
        #region Draw to Graphic
        /// <summary>
        /// InteractiveContainer.Draw(): call PaintBackground
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            this.PaintBackground(e);
            base.Draw(e, absoluteBounds, absoluteVisibleBounds);
        }
        /// <summary>
        /// Vykreslení pozadí
        /// </summary>
        /// <param name="e"></param>
        protected virtual void PaintBackground(GInteractiveDrawArgs e)
        {
        }
        #endregion
    }
    #region InteractiveLabeledContainer : Interaktivní container, nabízející Label a titulkovou oddělovací čáru
    /// <summary>
    /// <see cref="InteractiveLabeledContainer"/> : Interaktivní container, nabízející Label a titulkovou oddělovací čáru
    /// </summary>
    public class InteractiveLabeledContainer : InteractiveContainer
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public InteractiveLabeledContainer()
        {
            this.TitleLabel = new GLabel()
            {
                Text = "TitleLabel",
                Bounds = new Rectangle(4, 4, 180, 20),
                Alignment = ContentAlignment.TopLeft,
                PrepareToolTipInParent = true
            };
            this.AddItem(this.TitleLabel);

            this.TitleLine = new GLine3D();
            this.AddItem(this.TitleLine);
        }
        /// <summary>
        /// Titulkový label
        /// </summary>
        public GLabel TitleLabel { get; private set; }
        /// <summary>
        /// Modifikátor fontu pro titulkový label <see cref="TitleLabel"/> platný v době, kdy this Container má Focus
        /// </summary>
        public FontModifierInfo TitleFontModifierOnFocus { get; set; }
        /// <summary>
        /// Barva textu pro titulkový label <see cref="TitleLabel"/> platná v době, kdy this Container má Focus
        /// </summary>
        public Color? TitleTextColorOnFocus { get; set; }
        /// <summary>
        /// Titulková čára
        /// </summary>
        public GLine3D TitleLine { get; set; }
        #region Interaktivita
        /// <summary>
        /// Po vstupu Focusu do Containeru. Třída <see cref="InteractiveLabeledContainer"/> řídí modifikaci fontu a barvy titulku
        /// (vložením <see cref="TitleFontModifierOnFocus"/> a <see cref="TitleTextColorOnFocus"/> do objektu <see cref="TitleLabel"/>).
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedFocusEnter(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusEnter(e);
            TitleLabel.FontDynamicModifier = TitleFontModifierOnFocus;
            TitleLabel.TextColorDynamic = TitleTextColorOnFocus;
            TitleLabel.Invalidate();
        }
        /// <summary>
        /// Po odchodu Focusu z Containeru. Třída <see cref="InteractiveLabeledContainer"/> řídí modifikaci fontu a barvy titulku
        /// (odebráním <see cref="TitleFontModifierOnFocus"/> a <see cref="TitleTextColorOnFocus"/> z objektu <see cref="TitleLabel"/>).
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedFocusLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusLeave(e);
            TitleLabel.FontDynamicModifier = null;
            TitleLabel.TextColorDynamic = null;
            TitleLabel.Invalidate();
        }
        #endregion
    }
    #endregion
}
