using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Asol.Tools.WorkScheduler.Application;
using System.Drawing;

using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Localizable;
using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.Services
{
    #region IDataSource, Requests and Responses : Source of data and classes
    #region interface IDataSource; classes DataSourceRequest and DataSourceResponse
    /// <summary>
    /// Interface pro datové zdroje, které poskytují data pro zobrazení
    /// </summary>
    public interface IDataSource : IPlugin
    {
        /// <summary>
        /// Datový zdroj dostane jistý požadavek, a ten zpracuje a vrací data.
        /// Formát požadavku a vrácené odpovědi je závislý na konkrétní situaci.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        DataSourceResponse ProcessRequest(DataSourceRequest request);
    }
    /// <summary>
    /// Obecný požadavek na datový zdroj
    /// </summary>
    public abstract class DataSourceRequest
    {
        public DataSourceRequest(Data.ProgressData progressData)
        {
            this._ProgressData = progressData;
        }
        /// <summary>
        /// Data for progress
        /// </summary>
        public Data.ProgressData ProgressData { get { return this._ProgressData; } } private Data.ProgressData _ProgressData;
    }
    /// <summary>
    /// Obecná odpověď na obecný požadavek na datový zdroj
    /// </summary>
    public class DataSourceResponse
    {
        public DataSourceResponse(DataSourceRequest request)
        {
            this.Request = request;
        }
        public DataSourceRequest Request { get; private set; }
    }
    #endregion
    #endregion
    #region IFunctionGlobal and classes
    /// <summary>
    /// Deklarace pro plugin, který může vytvořit sadu globálních funkcí do ToolBaru
    /// </summary>
    public interface IFunctionGlobal : IFunctionProvider
    {
        /// <summary>
        /// Vytvoří a vrátí sadu globálních funkcí pro this plugin
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        FunctionGlobalPrepareResponse PrepareGui(FunctionGlobalPrepareGuiRequest request);
        /// <summary>
        /// Metoda může prověřit funkce, vytvořené ostatními pluginy.
        /// Kterýkoli plugin tak může nastavit FunctionGlobalGroup.IsVisible = false, 
        /// nebo FunctionGlobalGroup.Items[].IsVisible nebo IsEnabled = false, a zajistit tak skrytí jakékoli funkce z jiné služby.
        /// </summary>
        /// <param name="request"></param>
        void CheckGui(FunctionGlobalCheckGuiRequest request);
    }
    /// <summary>
    /// Požadavek na vytvoření dat pro ToolBar (=globální funkce)
    /// </summary>
    public class FunctionGlobalPrepareGuiRequest
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="toolBar"></param>
        public FunctionGlobalPrepareGuiRequest(GToolBar toolBar)
        {
            this.ToolBar = toolBar;
        }
        /// <summary>
        /// Reference na GUI ToolBaru
        /// </summary>
        public GToolBar ToolBar { get; private set; }
    }
    public class FunctionGlobalCheckGuiRequest
    {
        public FunctionGlobalCheckGuiRequest(FunctionGlobalGroup[] items)
        {
            this.Items = items;
        }
        /// <summary>
        /// All group of global function items
        /// </summary>
        public FunctionGlobalGroup[] Items { get; private set; }
    }
    public class FunctionGlobalPrepareResponse
    {
        public FunctionGlobalGroup[] Items { get; set; }
    }
    /// <summary>
    /// One global function group for items (global functions are showed on main Toolbar)
    /// </summary>
    public class FunctionGlobalGroup
    {
        #region Properties
        public FunctionGlobalGroup()
        {
            this._Id = App.GetNextId(typeof(FunctionGlobalGroup));
            this.IsLiable = true;
            this.IsVisible = true;
            this.LayoutWidth = 24;
        }
        public FunctionGlobalGroup(IFunctionGlobal provider)
            : this()
        {
            this.Provider = provider;
        }
        /// <summary>
        /// ID of this group
        /// </summary>
        public int Id { get { return this._Id; } } private int _Id;
        /// <summary>
        /// Owner of this group = Service of type IFunctionGlobal.
        /// Může být null!
        /// </summary>
        public IFunctionGlobal Provider { get; private set; }
        /// <summary>
        /// Type of Owner of this group = Service of type IFunctionGlobal
        /// </summary>
        public Type ProviderType { get { return (this.Provider != null ? this.Provider.GetType() : null); } }
        /// <summary>
        /// Title of this group
        /// </summary>
        public TextLoc Title { get; set; }
        /// <summary>
        /// ToolTip for this group (active on Title bar)
        /// </summary>
        public TextLoc ToolTipTitle { get; set; }
        /// <summary>
        /// Is this group visible? Default = true
        /// </summary>
        public bool IsVisible { get; set; }
        /// <summary>
        /// Order of this group as string (for hierarchically created groups)
        /// </summary>
        public string Order { get; set; }
        /// <summary>
        /// Is this group liable?
        /// If only the optional group is in the toolbar, it will not be displayed at all.
        /// Default = true
        /// </summary>
        public bool IsLiable { get; set; }
        /// <summary>
        /// Items in this group
        /// </summary>
        public EList<FunctionGlobalItem> Items { get { this._CheckItems(); return this._Items; } }
        /// <summary>
        /// Width of one group in this group, in "modules", where one module is equal to one "micro" icon.
        /// Default = 24;
        /// </summary>
        public int LayoutWidth { get; set; }
        /// <summary>
        /// Any UserData for this function
        /// </summary>
        public object UserData { get; set; }
        #endregion
        #region Events
        /// <summary>
        /// Action called on Action on any item (Button, Label: Click; ComboBox: Select)
        /// </summary>
        public event FunctionItemEventHandler ItemAction;
        /// <summary>
        /// Action called before SubItems on any Item will be enumerated
        /// </summary>
        public event FunctionItemEventHandler SubItemsEnumerateBefore;
        /// <summary>
        /// Run event Click for an Item in this group
        /// </summary>
        internal virtual void OnItemAction(FunctionItem item)
        {
            if (this.ItemAction != null)
                this.ItemAction(this, new FunctionItemEventArgs(item));
        }
        /// <summary>
        /// Run event SubItemsEnumerateBefore
        /// </summary>
        internal virtual void OnSubItemsEnumerateBefore(FunctionItem item)
        {
            if (this.SubItemsEnumerateBefore != null)
                this.SubItemsEnumerateBefore(this, new FunctionItemEventArgs(item));
        }
        #endregion
        #region Items
        /// <summary>
        /// Check when _Items is not null. Create new instance and insert handler for events.
        /// </summary>
        protected void _CheckItems()
        {
            if (this._Items != null) return;
            this._Items = new EList<FunctionGlobalItem>();
            this._Items.ItemAddAfter += new EList<FunctionGlobalItem>.EListEventAfterHandler(_Items_ItemAddAfter);
            this._Items.ItemRemoveAfter += new EList<FunctionGlobalItem>.EListEventAfterHandler(_Items_ItemRemoveAfter);
        }
        private void _Items_ItemAddAfter(object sender, EList<FunctionGlobalItem>.EListAfterEventArgs args)
        {
            if (args.Item != null)
                args.Item.Group = this;
        }
        private void _Items_ItemRemoveAfter(object sender, EList<FunctionGlobalItem>.EListAfterEventArgs args)
        {
            if (args.Item != null)
                args.Item.Group = null;
        }
        private EList<FunctionGlobalItem> _Items = null;
        #endregion
        /// <summary>
        /// Return compare result (a.Order - b.Order), StringComparison.InvariantCulture
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        internal static int SortByOrder(FunctionGlobalGroup a, FunctionGlobalGroup b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            string ax = a.Order;
            string bx = b.Order;
            int cmp = String.Compare(ax, bx, StringComparison.InvariantCulture);
            if (cmp == 0)
                cmp = a.Id.CompareTo(b.Id);
            return cmp;
        }
    }
    /// <summary>
    /// Data o jedné globální funkci.
    /// Globální funkce jsou zobrazeny v toolbaru.
    /// </summary>
    public class FunctionGlobalItem : FunctionItem
    {
        /// <summary>
        /// Konstruktor.
        /// Jako parametr "provider" se předává reference na objekt, který položku vytořil.
        /// </summary>
        /// <param name="provider"></param>
        public FunctionGlobalItem(IFunctionProvider provider)
            : base(provider)
        {
            this.Size = FunctionGlobalItemSize.Half;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.TextText + "; Type: " + this.ItemType + "; Size: " + this.Size;
        }
        /// <summary>
        /// Skupina, do které patří tato položka
        /// </summary>
        public FunctionGlobalGroup Group { get; internal set; }
        /// <summary>
        /// Typ prvku GlobalItem
        /// </summary>
        public virtual FunctionGlobalItemType ItemType { get; set; }
        /// <summary>
        /// Velikost prvku na toolbaru, vzhledem k výšce toolbaru
        /// </summary>
        public virtual FunctionGlobalItemSize Size { get; set; }
        /// <summary>
        /// Nápověda ke zpracování layoutu této položky
        /// </summary>
        public virtual LayoutHint LayoutHint { get; set; }
        /// <summary>
        /// Explicitně požadovaná šířka prvku v počtu modulů
        /// </summary>
        public virtual int? ModuleWidth { get; set; }
    }
    #endregion
    #region IFunctionProvider, FunctionItem, FunctionItemEventHandler, FunctionItemEventArgs : Common function provider and classese
    /// <summary>
    /// Plugin for create Function items
    /// </summary>
    public interface IFunctionProvider : IPlugin
    {
    }
    /// <summary>
    /// Description of one common function
    /// </summary>
    public class FunctionItem
    {
        #region Properties
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="provider"></param>
        public FunctionItem(IFunctionProvider provider)
        {
            this._Id = App.GetNextId(typeof(FunctionItem));
            this._Provider = provider;
            this.IsVisible = true;
            this.IsEnabled = true;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.TextText;
        }
        /// <summary>
        /// ID of this item
        /// </summary>
        public int Id { get { return this._Id; } } private int _Id;
        /// <summary>
        /// Provider of this function
        /// </summary>
        protected IFunctionProvider _Provider;
        /// <summary>
        /// Provider of this function
        /// </summary>
        public IFunctionProvider Provider { get { return this._Provider; } }
        /// <summary>
        /// Type of provider for this function
        /// </summary>
        public Type ProviderType { get { return (this._Provider != null ? this._Provider.GetType() : null); } }
        /// <summary>
        /// Text of this item, localizable.
        /// </summary>
        public virtual TextLoc Text { get; set; }
        /// <summary>
        /// Current text of Text (localized) for this item
        /// </summary>
        public virtual string TextText { get { return (this.Text != null ? this.Text.Text : ""); } }
        /// <summary>
        /// ToolTip for this item, localizable.
        /// </summary>
        public virtual TextLoc ToolTip { get; set; }
        /// <summary>
        /// Current text of ToolTip (localized) for this item
        /// </summary>
        public virtual string ToolTipText { get { return (this.ToolTip != null ? this.ToolTip.Text : ""); } }
        /// <summary>
        /// Description of font. When is null, then FontInfo.Menu is used. Get allways return Clone of internal font.
        /// </summary>
        public virtual FontInfo Font { get { return (this._Font != null ? this._Font.Clone : null); } set { this._Font = (value != null ? value.Clone : null); } } private FontInfo _Font;
        /// <summary>
        /// Parent of this Item.
        /// Can be null.
        /// </summary>
        public FunctionItem Parent { get; private set; }
        /// <summary>
        /// SubItem array (for ComboBox, SplitButton, and so on)
        /// </summary>
        public virtual EList<FunctionItem> SubItems { get { this._CheckSubItems(); return this._SubItems; } }
        /// <summary>
        /// Invaliduje pole SubItems. Načte se poté znovu.
        /// </summary>
        public void SubItemsInvalidate() { this._SubItemsValid = false; }
        /// <summary>
        /// Image for standard state
        /// </summary>
        public virtual Image Image { get; set; }
        /// <summary>
        /// Image for MouseActive state
        /// </summary>
        public virtual Image ImageHot { get; set; }
        /// <summary>
        /// Set of images for all interactive states and Component sizes
        /// </summary>
        public virtual TypeArray<GInteractiveState, ComponentSize, Image> Images
        {
            get { if (this._Images == null) this._Images = new TypeArray<GInteractiveState, ComponentSize, Image>(); return this._Images; }
        }
        private TypeArray<GInteractiveState, ComponentSize, Image> _Images = null;
        /// <summary>
        /// Is this item visible? Default = true
        /// </summary>
        public virtual bool IsVisible { get; set; }
        /// <summary>
        /// Is this item enabled? Default = true
        /// </summary>
        public virtual bool IsEnabled { get; set; }
        /// <summary>
        /// Any Value for this function.
        /// For CheckButton + RadioButton: boolean true / false;
        /// For ComboBox: selected SubItem or null
        /// </summary>
        public virtual object Value { get; set; }
        /// <summary>
        /// Any UserData for this function
        /// </summary>
        public object UserData { get; set; }
        #endregion
        #region Events
        /// <summary>
        /// Action called on Click on item
        /// </summary>
        public event FunctionItemEventHandler Click;
        /// <summary>
        /// Action called on Click on any sub-item on this item (DropDown container, and so on)
        /// </summary>
        public event FunctionItemEventHandler SubItemsClick;
        /// <summary>
        /// Action called before SubItems will be enumerated.
        /// SubItems are enumerated only once after , 
        /// </summary>
        public event FunctionItemEventHandler SubItemsEnumerateBefore;
        /// <summary>
        /// Run event Click
        /// </summary>
        internal virtual void OnClick()
        {
            if (this.Click != null)
                this.Click(this, new FunctionItemEventArgs(this));
        }
        /// <summary>
        /// Run event SubItemsClick
        /// </summary>
        /// <param name="subItem">SubItem clicked</param>
        internal virtual void OnSubItemsClick(FunctionItem subItem)
        {
            if (this.SubItemsClick != null)
                this.SubItemsClick(this, new FunctionItemEventArgs(subItem));
        }
        /// <summary>
        /// Run event SubItemsEnumerateBefore
        /// </summary>
        internal virtual void OnSubItemsEnumerateBefore()
        {
            if (!this._SubItemsValid && this.SubItemsEnumerateBefore != null)
                this.SubItemsEnumerateBefore(this, new FunctionItemEventArgs(this));
            this._SubItemsValid = true;
        }
        #endregion
        #region SubItems
        /// <summary>
        /// true after Enumerate SubItems in OnSubItemsEnumerateBefore(), false after SubItemsInvalidate().
        /// </summary>
        protected bool _SubItemsValid { get; private set; }
        /// <summary>
        /// Check when _SubItems is not null. Create new instance and insert handler for events.
        /// </summary>
        protected void _CheckSubItems()
        {
            if (this._SubItems != null) return;
            this._SubItems = new EList<FunctionItem>();
            this._SubItems.ItemAddAfter += new EList<FunctionItem>.EListEventAfterHandler(_SubItems_ItemAddAfter);
            this._SubItems.ItemRemoveAfter += new EList<FunctionItem>.EListEventAfterHandler(_SubItems_ItemRemoveAfter);
        }
        private void _SubItems_ItemAddAfter(object sender, EList<FunctionItem>.EListAfterEventArgs args)
        {
            if (args.Item != null)
                args.Item.Parent = this;
        }
        private void _SubItems_ItemRemoveAfter(object sender, EList<FunctionItem>.EListAfterEventArgs args)
        {
            if (args.Item != null)
                args.Item.Parent = null;
        }
        private EList<FunctionItem> _SubItems = null;
    	#endregion
        #region Create menu
        public static System.Windows.Forms.ToolStripDropDownMenu CreateDropDownMenuFrom(IEnumerable<FunctionItem> items)
        {
            System.Windows.Forms.ToolStripDropDownMenu menu = new System.Windows.Forms.ToolStripDropDownMenu();
            menu.DropShadowEnabled = true;
            menu.ShowImageMargin = true;
            menu.ShowItemToolTips = true;
            menu.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;

            if (items != null)
            {
                foreach (FunctionItem item in items)
                {
                    if (item != null)
                        menu.Items.Add(item.CreateWinFormItem());
                }
            }

            return menu;
        }
        public virtual System.Windows.Forms.ToolStripItem CreateWinFormItem()
        {
            System.Windows.Forms.ToolStripItem result = null;
            string text = this.TextText;
            if (String.IsNullOrEmpty(text))
            {
                result = new System.Windows.Forms.ToolStripSeparator();
            }
            else
            {
                System.Windows.Forms.ToolStripMenuItem item = new System.Windows.Forms.ToolStripMenuItem(this.TextText, this.Image);

                string toolTip = this.ToolTipText;
                if (!String.IsNullOrEmpty(toolTip))
                {
                    item.ToolTipText = toolTip;
                    item.AutoToolTip = true;
                }

                result = item;
            }
            result.Tag = this;
            return result;
        }
        #endregion
    }
    /// <summary>
    /// Delegate for events on FunctionItem
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void FunctionItemEventHandler(object sender, FunctionItemEventArgs args);
    /// <summary>
    /// Argument for events on FunctionItem
    /// </summary>
    public class FunctionItemEventArgs : EventArgs
    {
        public FunctionItemEventArgs(FunctionItem item)
        {
            this._Item = item;
        }
        private FunctionItem _Item;
        /// <summary>
        /// Active FunctionItem
        /// </summary>
        public FunctionItem Item { get { return this._Item; } }
    }
    /// <summary>
    /// Size of Component
    /// </summary>
    public enum ComponentSize
    {
        None,
        /// <summary>
        /// Size of Component, where one FunctionGlobalItem with Size = Small has Height = 24 px and FunctionGlobalItem with Size = Large has Height = 48 px.
        /// </summary>
        Small,
        /// <summary>
        /// Size of Component, where one FunctionGlobalItem with Size = Small has Height = 32 px and FunctionGlobalItem with Size = Large has Height = 64 px.
        /// Default setting.
        /// </summary>
        Medium,
        /// <summary>
        /// Size of Component, where one FunctionGlobalItem with Size = Small has Height = 48 px and FunctionGlobalItem with Size = Large has Height = 96 px.
        /// </summary>
        Large
    }
    #endregion
}
