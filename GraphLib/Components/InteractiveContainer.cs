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
        public InteractiveContainer()
        {
            this._ItemList = new EList<IInteractiveItem>();
            this._ItemList.ItemAddBefore += new EList<IInteractiveItem>.EListEventBeforeHandler(_ItemList_ItemAddBefore);
            this._ItemList.ItemAddAfter += new EList<IInteractiveItem>.EListEventAfterHandler(_ItemList_ItemAddAfter);
            this._ItemList.ItemRemoveBefore += new EList<IInteractiveItem>.EListEventBeforeHandler(_ItemList_ItemRemoveBefore);
            this._ItemList.ItemRemoveAfter += new EList<IInteractiveItem>.EListEventAfterHandler(_ItemList_ItemRemoveAfter);
        }
        /// <summary>
        /// ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return base.ToString() + "; " + this._ItemList.Count.ToString() + " child items";
        }
        #endregion
        #region Items Add/Remove events

        void _ItemList_ItemAddBefore(object sender, EList<IInteractiveItem>.EListBeforeEventArgs args)
        {
            args.Item.Parent = this;
            this.OnItemAddBefore(args);
            if (this.ItemAddBefore != null)
                this.ItemAddBefore(this, args);
        }
        protected virtual void OnItemAddBefore(EList<IInteractiveItem>.EListBeforeEventArgs args) { }
        public event EList<IInteractiveItem>.EListEventBeforeHandler ItemAddBefore;

        void _ItemList_ItemAddAfter(object sender, EList<IInteractiveItem>.EListAfterEventArgs args)
        {
            this.OnItemAddAfter(args);
            if (this.ItemAddAfter != null)
                this.ItemAddAfter(this, args);
        }
        protected virtual void OnItemAddAfter(EList<IInteractiveItem>.EListAfterEventArgs args) { }
        public event EList<IInteractiveItem>.EListEventAfterHandler ItemAddAfter;

        void _ItemList_ItemRemoveBefore(object sender, EList<IInteractiveItem>.EListBeforeEventArgs args)
        {
            this.OnItemRemoveBefore(args);
            if (this.ItemRemoveBefore != null)
                this.ItemRemoveBefore(this, args);
        }
        protected virtual void OnItemRemoveBefore(EList<IInteractiveItem>.EListBeforeEventArgs args) { }
        public event EList<IInteractiveItem>.EListEventBeforeHandler ItemRemoveBefore;

        void _ItemList_ItemRemoveAfter(object sender, EList<IInteractiveItem>.EListAfterEventArgs args)
        {
            this.OnItemRemoveAfter(args);
            if (this.ItemRemoveAfter != null)
                this.ItemRemoveAfter(this, args);
            args.Item.Parent = null;
        }
        protected virtual void OnItemRemoveAfter(EList<IInteractiveItem>.EListAfterEventArgs args) { }
        public event EList<IInteractiveItem>.EListEventAfterHandler ItemRemoveAfter;

        #endregion
        #region Items
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
                this._ItemList.ClearSilent();
                this._ItemList.AddRangeSilent(value);
            }
        }
        /// <summary>
        /// Add one interactive item.
        /// Does not trigger Draw().
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(IInteractiveItem item)
        {
            this.ChildList.Add(item);
        }
        /// <summary>
        /// Add more interactive items.
        /// Does not trigger Draw().
        /// </summary>
        /// <param name="item"></param>
        public void AddItems(params IInteractiveItem[] items)
        {
            this.ChildList.AddRange(items);
        }
        /// <summary>
        /// Add more interactive items.
        /// Does not trigger Draw().
        /// </summary>
        /// <param name="item"></param>
        public void AddItems(IEnumerable<IInteractiveItem> items)
        {
            this.ChildList.AddRange(items);
        }
        protected EList<IInteractiveItem> ChildList
        {
            get
            {
                if (this._ItemList == null)
                    this._ItemList = new EList<IInteractiveItem>();
                return this._ItemList;
            }
        }
        private EList<IInteractiveItem> _ItemList;

        // List<IInteractiveItem> IInteractiveContainer.ItemList { get { return this._ItemList; } }
        // Rectangle IInteractiveContainer.ItemBounds { get { return this.ItemBounds; } }
        #endregion
        #region Interactive property and methods
       
        #endregion
        #region Draw to Graphic
        /// <summary>
        /// InteractiveContainer.Draw(): call PaintBackground
        /// </summary>
        /// <param name="e"></param>
        protected override void Draw(GInteractiveDrawArgs e)
        {
            this.PaintBackground(e);
            base.Draw(e);
        }
        protected virtual void PaintBackground(GInteractiveDrawArgs e)
        {
            
        }
        #endregion
    }
}
