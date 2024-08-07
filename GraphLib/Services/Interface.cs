﻿using System;
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
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="progressData"></param>
        public DataSourceRequest(Components.ProgressData progressData)
        {
            this._ProgressData = progressData;
        }
        /// <summary>
        /// Data for progress
        /// </summary>
        public Components.ProgressData ProgressData { get { return this._ProgressData; } } private Components.ProgressData _ProgressData;
    }
    /// <summary>
    /// Obecná odpověď na obecný požadavek na datový zdroj
    /// </summary>
    public class DataSourceResponse
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="request"></param>
        public DataSourceResponse(DataSourceRequest request)
        {
            this.Request = request;
        }
        /// <summary>
        /// Požadavek
        /// </summary>
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
        public FunctionGlobalPrepareGuiRequest(ToolBar toolBar)
        {
            this.ToolBar = toolBar;
        }
        /// <summary>
        /// Reference na GUI ToolBaru
        /// </summary>
        public ToolBar ToolBar { get; private set; }
    }
    /// <summary>
    /// Požadavek
    /// </summary>
    public class FunctionGlobalCheckGuiRequest
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="items"></param>
        public FunctionGlobalCheckGuiRequest(FunctionGlobalGroup[] items)
        {
            this.Items = items;
        }
        /// <summary>
        /// All group of global function items
        /// </summary>
        public FunctionGlobalGroup[] Items { get; private set; }
    }
    /// <summary>
    /// Odpověď
    /// </summary>
    public class FunctionGlobalPrepareResponse
    {
        /// <summary>
        /// Položky
        /// </summary>
        public FunctionGlobalGroup[] Items { get; set; }
    }
    /// <summary>
    /// One global function group for items (global functions are showed on main Toolbar)
    /// </summary>
    public class FunctionGlobalGroup
    {
        #region Properties
        /// <summary>
        /// Konstruktor
        /// </summary>
        public FunctionGlobalGroup()
        {
            this._Id = App.GetNextId(typeof(FunctionGlobalGroup));
            this.IsLiable = true;
            this.IsVisible = true;
            this.LayoutWidth = 24;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="provider"></param>
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
        /// <summary>
        /// Metoda přidá separátor jako další prvek grupy, pokud je to vhodné.
        /// Pokud je this grupa prázdná, anebo pokud poslední prvek grupy už je separátor, pak není vhodné přidávat další.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="force">true = Přidat separátor i když to není vhodné</param>
        public void AddSeparator(IFunctionProvider provider = null, bool force = false)
        {
            if (force || (this.Items.Count > 0 && this.Items[this.Items.Count - 1].ItemType != FunctionGlobalItemType.Separator))
            {
                FunctionGlobalItem functionItem = new FunctionGlobalItem(provider);
                functionItem.Name = "__separator__";
                functionItem.Text = "";
                functionItem.ItemType = FunctionGlobalItemType.Separator;
                this.Items.Add(functionItem);
            }
        }
        #endregion
        #region Events
        /// <summary>
        /// Událost volaná po změně hodnoty <see cref="FunctionItem.IsChecked"/> na prvku v rámci této grupy (Button).
        /// Tato událost je vyvolána před událostí Click.
        /// </summary>
        public event FunctionItemEventHandler ItemSelectedChange;
        /// <summary>
        /// Událost volaná po kliknutí na prvek v rámci této grupy (Button, Label: Click;   ComboBox: Select)
        /// </summary>
        public event FunctionItemEventHandler ItemClicked;
        /// <summary>
        /// Událost volaná těsně před tím, než se budou zobrazovat (číst) SubItems na nějakém prvku.
        /// Aplikace dostává možnost on-demand donačíst podpoložky, nebo je aktualizovat.
        /// </summary>
        public event FunctionItemEventHandler SubItemsEnumerateBefore;
        /// <summary>
        /// Vyvolá event <see cref="ItemClicked"/> na této grupě.
        /// </summary>
        internal virtual void OnItemClicked(FunctionItem item)
        {
            if (this.ItemClicked != null)
                this.ItemClicked(this, new FunctionItemEventArgs(item));
        }
        /// <summary>
        /// Vyvolá event <see cref="ItemSelectedChange"/> na této grupě.
        /// </summary>
        internal virtual void OnItemCheckedChange(FunctionItem item)
        {
            if (this.ItemSelectedChange != null)
                this.ItemSelectedChange(this, new FunctionItemEventArgs(item));
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
        /// Explicitně požadovaná šířka prvku v počtu modulů.
        /// Výška prvku je dána jeho velikostí <see cref="Size"/>.
        /// </summary>
        public virtual int? ModuleWidth { get; set; }
        /// <summary>
        /// Příznak, zda tento prvek bude persistovat svoji hodnotu do uživatelské konfigurace.
        /// Při příštím startu aplikace bude tato hodnota načtena z konfigurace a vložena do prvku.
        /// Defaultní hodnota je false.
        /// </summary>
        public virtual bool PersistEnabled { get; set; }
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
        /// ID tohoto prvku, jednoznačné, přidělené automaticky při vytvoření
        /// </summary>
        public int Id { get { return this._Id; } } private int _Id;
        /// <summary>
        /// Jméno tohoto prvku, prostor pro aplikační identifikátor položky
        /// </summary>
        public virtual string Name { get; set; }
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
        /// Background color for this item
        /// </summary>
        public virtual Color? BackColor { get; set; }
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
        /// Obsahuje true pokud this prvek může být <see cref="IsChecked"/> = být označen jako "aktivní"
        /// </summary>
        public virtual bool IsCheckable { get; set; }
        /// <summary>
        /// Obsahuje true, pokud tento prvek je aktivní (má u sebe zaškrtávátko)
        /// </summary>
        public virtual bool IsChecked { get; set; }
        /// <summary>
        /// Obsahuje název skupiny prvků, které se vzájemně chovají jako OptionGroup.
        /// To znamená, že právě jeden z prvků skupiny může být <see cref="IsChecked"/> = být označen jako aktivní.
        /// <para/>
        /// Chování:
        /// <para/>
        /// a) Pokud je <see cref="CheckedGroupName"/> prázdné, pak se button chová jako CheckBox: změna jeho hodnoty <see cref="IsChecked"/> neovlivní žádný jiný prvek.
        /// Kliknutí na takový prvek mění hodnotu <see cref="IsChecked"/> z false na true a naopak = lze jej shodit na false.
        /// <para/>
        /// b) Pokud je <see cref="CheckedGroupName"/> prázdné, pak se button chová jako RadioButton: kliknutí na neoznačený button jej označí a současně odznačí ostatní buttony v grupě.
        /// Opakované kliknutí na označený button jej neodznačí.
        /// Prvky jedné grupy <see cref="CheckedGroupName"/> se musí nacházet v jedné grafické skupině "GroupName" (platí pro Toolbar).
        /// Pokud by byly umístěny v jiné grupě, nebudou považovány za jednu skupinu, ale více oddělených skupin.
        /// Naproti tomu jedna grafická grupa "GroupName" může obsahovat více skupin <see cref="CheckedGroupName"/>.
        /// <para/>
        /// Je rozumné dávat prvky jedné <see cref="CheckedGroupName"/> blízko k sobě, ale technicky nutné to není.
        /// </summary>
        public virtual string CheckedGroupName { get; set; }
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
        /// Akce volaná po změně <see cref="IsChecked"/> na tomto prvku.
        /// </summary>
        public event FunctionItemEventHandler SelectedChange;
        /// <summary>
        /// Akce volaná po změně <see cref="IsChecked"/> na některém z podřízených prvků tohoto prvku.
        /// </summary>
        public event FunctionItemEventHandler SubItemsSelectedChange;
        /// <summary>
        /// Akce volaná po kliknutí na tento prvek.
        /// </summary>
        public event FunctionItemEventHandler Click;
        /// <summary>
        /// Akce volaná po kliknutí na některém z podřízených prvků tohoto prvku.
        /// </summary>
        public event FunctionItemEventHandler SubItemsClick;
        /// <summary>
        /// Action called before SubItems will be enumerated.
        /// SubItems are enumerated only once after , 
        /// </summary>
        public event FunctionItemEventHandler SubItemsEnumerateBefore;
        /// <summary>
        /// Vyvolá event SelectedChange
        /// </summary>
        internal virtual void OnCheckedChange()
        {
            if (this.SelectedChange != null)
                this.SelectedChange(this, new FunctionItemEventArgs(this));
        }
        /// <summary>
        /// Vyvolá event Click
        /// </summary>
        internal virtual void OnClick()
        {
            if (this.Click != null)
                this.Click(this, new FunctionItemEventArgs(this));
        }
        /// <summary>
        /// Vvyolá event SubItemsSelectedChange
        /// </summary>
        /// <param name="subItem">SubItem clicked</param>
        internal virtual void OnSubItemsCheckedChange(FunctionItem subItem)
        {
            if (this.SubItemsSelectedChange != null)
                this.SubItemsSelectedChange(this, new FunctionItemEventArgs(subItem));
        }
        /// <summary>
        /// Vvyolá event SubItemsClick
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
        /// <summary>
        /// Vytvoří systémovou nabídku <see cref="System.Windows.Forms.ToolStripDropDownMenu"/> z dodaných funkcí.
        /// Do property <see cref="System.Windows.Forms.ToolStripItem.Tag"/> každé položky menu 
        /// uloží referenci na prvek <see cref="FunctionItem"/>, z něhož je položka menu vytvořena.
        /// </summary>
        /// <param name="functionItems">Položky menu</param>
        /// <param name="modifyMenu">Akce, která může modifikovat menu ještě před přidáním položek</param>
        /// <returns></returns>
        public static System.Windows.Forms.ToolStripDropDownMenu CreateDropDownMenuFrom(IEnumerable<FunctionItem> functionItems, Action<System.Windows.Forms.ToolStripDropDownMenu> modifyMenu = null)
        {
            System.Windows.Forms.ToolStripDropDownMenu menu = Painter.CreateDropDownMenu();

            if (modifyMenu != null)
                modifyMenu(menu);

            var items = CreateWinFormItems(functionItems);
            if (items != null)
                menu.Items.AddRange(items);

            return menu;
        }
        /// <summary>
        /// Vytvoří položku systémové nabídky <see cref="System.Windows.Forms.ToolStripItem"/> z this prvku.
        /// Do property <see cref="System.Windows.Forms.ToolStripItem.Tag"/> uloží referenci na this.
        /// Pokud je <see cref="TextText"/> prázdný, vrátí separátor.
        /// </summary>
        /// <returns></returns>
        public virtual System.Windows.Forms.ToolStripItem CreateWinFormItem()
        {
            System.Windows.Forms.ToolStripItem result = null;
            string text = this.TextText;
            Color? backColor = this.BackColor;

            if (String.IsNullOrEmpty(text))
            {
                result = new System.Windows.Forms.ToolStripSeparator();
            }
            else
            {
                // Systémová tvorba položky menu:
                System.Windows.Forms.ToolStripMenuItem item = Painter.CreateDropDownItem(text, image: this.Image,
                    toolTip: this.ToolTipText, isEnabled: this.IsEnabled, isCheckable: this.IsCheckable, isChecked: this.IsChecked,
                    backColor: this.BackColor, name: this.Name);

                // SubItems dáme rekurzivně:
                var subItems = CreateWinFormItems(this.SubItems);
                if (subItems != null)
                {
                    if (backColor.HasValue)
                        item.DropDown.BackColor = backColor.Value;   // Barva SubMenu = barva this položky
                    item.DropDownItems.AddRange(subItems);
                }

                result = item;
            }
            result.Tag = this;
            return result;
        }
        /// <summary>
        /// Metoda vytvoří a vrátí kolekci prvků <see cref="System.Windows.Forms.ToolStripItem"/> z dodané kolekce prvků <see cref="FunctionItem"/>.
        /// </summary>
        /// <param name="functionItems"></param>
        /// <returns></returns>
        protected static System.Windows.Forms.ToolStripItem[] CreateWinFormItems(IEnumerable<FunctionItem> functionItems)
        {
            if (functionItems == null) return null;

            List<System.Windows.Forms.ToolStripItem> result = new List<System.Windows.Forms.ToolStripItem>();
            foreach (FunctionItem functionItem in functionItems)
            {
                if (functionItem != null)
                    result.Add(functionItem.CreateWinFormItem());
            }

            return ((result.Count > 0) ? result.ToArray() : null);
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
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="item"></param>
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
        /// <summary>
        /// Nezadáno
        /// </summary>
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
