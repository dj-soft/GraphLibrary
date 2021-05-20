using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using DevExpress.XtraEditors;
using WF = System.Windows.Forms;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Panel reprezentující DataForm - včetně záložek a scrollování
    /// </summary>
    public partial class DxDataForm : DxPanelControl, IDxDataForm
    {
        #region Konstruktor a proměnné, Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxDataForm()
        {
            __Pages = new Dictionary<string, DxDataFormPage>();
            __Items = new Dictionary<string, DxDataFormControlItem>();
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.MemoryMode = DxDataFormMemoryMode.Default;
        }
        /// <summary>
        /// Souhrn stránek
        /// </summary>
        public DxDataFormPage[] Pages { get { return __Pages.Values.ToArray(); } }
        private Dictionary<string, DxDataFormPage> __Pages;
        /// <summary>
        /// Souhrn aktuálních prvků
        /// </summary>
        public DxDataFormControlItem[] Items { get { return __Items.Values.ToArray(); } }
        private Dictionary<string, DxDataFormControlItem> __Items;
        /// <summary>
        /// Dispose prvku
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.DisposeContent();
            base.Dispose(disposing);
            this._ClearInstance();
        }
        /// <summary>
        /// Uvolní instance na které drží referenci, neřeší ale jejich Dispose.
        /// </summary>
        private void _ClearInstance()
        {
            __PagesClear();
            __Pages = null;

            __ItemsClear();
            __Items = null;
        }
        private void __PagesClear()
        {
            if (__Pages == null) return;
            foreach (var page in __Pages.Values)
                page.Dispose();
            __Pages.Clear();
        }
        private void __ItemsClear()
        {
            if (__Items == null) return;
            foreach (var item in __Items.Values)
                item.Dispose();
            __Items.Clear();
        }
        #endregion

        /// <summary>
        /// Režim práce s pamětí
        /// </summary>
        public DxDataFormMemoryMode MemoryMode { get; set; }

        #region Přidání / odebrání controlů do logických stránek (AddItems), tvorba nových stránek, 
        /// <summary>
        /// Přidá řadu controlů, řeší záložky
        /// </summary>
        /// <param name="items"></param>
        internal void AddItems(IEnumerable<IDataFormItem> items)
        {
            if (items == null) return;
            foreach (IDataFormItem item in items)
                _AddItem(item, true);
            _FinalisePages();
        }
        /// <summary>
        /// Přidá jeden control, řeší záložky.
        /// Pro více controlů prosím volejme <see cref="AddItems(IEnumerable{IDataFormItem})"/>!
        /// </summary>
        /// <param name="item"></param>
        internal void AddItem(IDataFormItem item)
        {
            _AddItem(item, true);
            _FinalisePages();
        }
        /// <summary>
        /// Přidá jeden control, volitelně finalizuje dotčenou stránku.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="skipFinalise"></param>
        private void _AddItem(IDataFormItem item, bool skipFinalise = false)
        {
            if (item == null) throw new ArgumentNullException("DxDataForm.AddItem(item) error: item is null.");
            string itemKey = _CheckNewItemKey(item);
            DxDataFormPage page = _GetPage(item);
            DxDataFormControlItem controlItem = page.AddItem(item, skipFinalise);        // I stránka sama si přidá prvek do svého pole, ale jen pro své zobrazovací potřeby.
            __Items.Add(itemKey, controlItem);             // Prvek přidávám do Dictionary bez obav, protože unikátnost klíče jsem prověřil v metodě _CheckNewItemKey() před chvilkou
        }
        /// <summary>
        /// Najde a nebo vytvoří a vrátí stránku <see cref="DxDataFormPage"/> podle dat v definici prvku.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private DxDataFormPage _GetPage(IDataFormItem item)
        {
            DxDataFormPage page;
            string pageKey = _GetKey(item.PageName);
            if (!__Pages.TryGetValue(pageKey, out page))
            {
                page = _CreatePage(item);
                __Pages.Add(pageKey, page);
            }
            return page;
        }
        /// <summary>
        /// Vytvoří a vrátí instanci <see cref="DxDataFormPage"/> podle dat v definici prvku.
        /// Vytvoří prvek, který není aktivní = to proto, aby v rámci inicializací nebyly generovány zbytečně controly.
        /// Pokud instance bude použita jako samostatná, musí ji aplikace aktivovat.
        /// Pokud instance bude na TabPane, pak aktivaci řídí přepínání záložek.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private DxDataFormPage _CreatePage(IDataFormItem item)
        {
            DxDataFormPage page = new DxDataFormPage(this);
            page.FillFrom(item);
            return page;
        }
        /// <summary>
        /// Finalizuje stránky z hlediska jejich vnitřního uspořádání i z hlediska zobrazení (jedn panel / více panelů na záložkách)
        /// </summary>
        private void _FinalisePages()
        {
            foreach (var page in __Pages.Values)
                page.FinaliseContent();

            PrepareTabForPages();
        }
        /// <summary>
        /// Zkontroluje, že daný <see cref="IDataFormItem"/> má neprázdný klíč <see cref="IDataFormItem.ItemName"/> a že tento klíč dosud není v this dataformu použit.
        /// Může vyhodit chybu.
        /// </summary>
        /// <param name="item"></param>
        private string _CheckNewItemKey(IDataFormItem item)
        {
            string itemKey = _GetKey(item.ItemName);
            if (itemKey == "") throw new ArgumentNullException("DxDataForm.AddItem(item) error: ItemName is empty.");
            if (__Items.ContainsKey(itemKey)) throw new ArgumentException($"DxDataForm.AddItem(item) error: ItemName '{item.ItemName}' already exists, duplicity name is not allowed.");
            return itemKey;
        }
        /// <summary>
        /// Vrací klíč z daného textu: pro null nebo empty vrátí "", jinak vrátí Trim().ToLower()
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string _GetKey(string name)
        {
            return (String.IsNullOrEmpty(name) ? "" : name.Trim().ToLower());
        }
        /// <summary>
        /// Vrátí index daného panelu
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        int IDxDataForm.IndexOf(DxDataFormScrollPanel panel) { return __Pages.Values.ToList().FindIndex(p => Object.ReferenceEquals(p, panel)); }
        /// <summary>
        /// Vrátí true pokud se control (s danými souřadnicemi) má brát jako viditelný v dané oblasti.
        /// Tato metoda může provádět optimalizaci v tom, že jako "viditelné" určí i controly nedaleko od reálně viditelné souřadnice.
        /// </summary>
        /// <param name="controlBounds"></param>
        /// <param name="visibleBounds"></param>
        /// <returns></returns>
        bool IDxDataForm.IsInVisibleBounds(Rectangle? controlBounds, Rectangle visibleBounds)
        {
            if (!controlBounds.HasValue) return false;
            int distX = 90;                                // Vzdálenost na ose X, kterou akceptujeme jako viditelnou 
            int distY = 60;                                //  ...Y... = (=rezerva okolo viditelné oblasti, kde už máme připravené fyzické controly)
            var cb = controlBounds.Value;

            if ((cb.Bottom + distY) < visibleBounds.Y) return false;
            if ((cb.Y - distY) > visibleBounds.Bottom) return false;

            if ((cb.Right + distX) < visibleBounds.X) return false;
            if ((cb.X - distX) > visibleBounds.Right) return false;

            return true;
        }
        #endregion

        private void PrepareTabForPages()
        {
            var pagesAll = __Pages.Values.ToArray();                    // Všechny stránky v poli (i prázdné)
            var pagesData = pagesAll.Where(p => !p.IsEmpty).ToArray();  // Jen ty stránky, které obsahují controly


            #warning příliš jednoduché, nezvládne změny, funguje jen pro první dávku prvků:


            if (pagesData.Length == 1)
            {
                var pageData = pagesData[0];
                pageData.PlaceToParent(this);
                pageData.IsActiveContent = true;
            }
            else if (pagesData.Length > 1)
            {
                if (_TabPane == null)
                {
                    _TabPane = new DxTabPane();
                    _TabPane.Dock = WF.DockStyle.Fill;
                    _TabPane.PageChangingPrepare += _TabPane_PageChangingPrepare;
                    _TabPane.PageChangingRelease += _TabPane_PageChangingRelease;
                    _TabPane.TransitionType = DxTabPaneTransitionType.SlideSlow;
                    _TabPane.TransitionType = DxTabPaneTransitionType.None;
                    this.Controls.Add(_TabPane);
                }

                foreach (var pageData in pagesData)
                {
                    var pane = _TabPane.AddNewPage(pageData.PageName ?? "", pageData.PageText ?? "Záložka s daty", pageData.PageToolTipText);
                    pageData.PlaceToParent(pane);
                    pageData.Dock = WF.DockStyle.Fill;
                }

                DxDataFormPage selectedPage = this.SelectedPage;
                if (selectedPage != null && !selectedPage.IsActiveContent)
                    selectedPage.IsActiveContent = true;
            }
        }
        /// <summary>
        /// Aktuálně viditelná stránka (záložka), resp. její obsah
        /// </summary>
        public DxDataFormPage SelectedPage 
        {
            get
            {
                if (_TabPane != null && _TabPane.Visible) return GetDataFormPage(_TabPane.SelectedPage);
                if (__Pages != null && __Pages.Count > 0) return __Pages.Values.ToArray()[0];
                return null;
            }
        }
        private void _TabPane_PageChangingPrepare(object sender, TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> e)
        {
            _TabPaneChangeStart = DxComponent.LogTimeCurrent;
            DxDataFormPage page = GetDataFormPage(e.Item);
            _TabPaneChangeNameNew = page?.DebugName;
            if (page != null) page.IsActiveContent = true;
        }

        private void _TabPane_PageChangingRelease(object sender, TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> e)
        {
            DxDataFormPage page = GetDataFormPage(e.Item);
            if (page != null) page.IsActiveContent = false;
            _TabPaneChangeNameOld = page?.DebugName;
            RunTabChangeDone();
            DxComponent.LogAddLineTime($"TabChange from '{_TabPaneChangeNameOld}' to '{_TabPaneChangeNameNew}'; Time: {DxComponent.LogTokenTimeMilisec}", _TabPaneChangeStart);
        }

        private long? _TabPaneChangeStart;
        private string _TabPaneChangeNameOld;
        private string _TabPaneChangeNameNew;
        /// <summary>
        /// Vrátí <see cref="DxDataFormPage"/> nacházející se na daném controlu
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        private DxDataFormPage GetDataFormPage(WF.Control parent)
        {
            return parent?.Controls.OfType<DxDataFormPage>().FirstOrDefault();
        }

        private DxTabPane _TabPane;
        /// <summary>
        /// Vyvolá akce po dokončení změny stránky, vhodné i pro časomíru a refresh zdrojů
        /// </summary>
        /// <param name="time"></param>
        private void RunTabChangeDone()
        {
            EventArgs args = EventArgs.Empty;
            OnTabChangeDone(args);
            TabChangeDone?.Invoke(this, args);
        }
        /// <summary>
        /// Akce po dokončení změny stránky, vhodné i pro časomíru a refresh zdrojů
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnTabChangeDone(EventArgs args) { }
        /// <summary>
        /// Akce po dokončení změny stránky, vhodné i pro časomíru a refresh zdrojů
        /// </summary>
        public event EventHandler TabChangeDone;
        /// <summary>
        /// Metoda vrátí true pro typ prvku, který může dostat klávesový Focus
        /// </summary>
        /// <param name="itemType"></param>
        /// <returns></returns>
        public static bool IsFocusableControl(DataFormItemType itemType)
        {
            switch (itemType)
            {
                case DataFormItemType.TextBox:
                case DataFormItemType.EditBox:
                case DataFormItemType.SpinnerBox:
                case DataFormItemType.CheckBox:
                case DataFormItemType.BreadCrumb:
                case DataFormItemType.ComboBoxList:
                case DataFormItemType.ComboBoxEdit:
                case DataFormItemType.ListView:
                case DataFormItemType.TreeView:
                case DataFormItemType.RadioButtonBox:
                case DataFormItemType.Button:
                case DataFormItemType.DropDownButton:
                    return true;
            }
            return false;
        }
        
    }
    /// <summary>
    /// Interní přístup do <see cref="DxDataForm"/> pro jeho podřízené třídy
    /// </summary>
    internal interface IDxDataForm
    {
        /// <summary>
        /// Vrátí index daného panelu
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        int IndexOf(DxDataFormScrollPanel panel);
        /// <summary>
        /// Vrátí true pokud se control (s danými souřadnicemi) má brát jako viditelný v dané oblasti.
        /// Tato metoda může provádět optimalizaci v tom, že jako "viditelné" určí i controly nedaleko od reálně viditelné souřadnice.
        /// </summary>
        /// <param name="controlBounds"></param>
        /// <param name="visibleBounds"></param>
        /// <returns></returns>
        bool IsInVisibleBounds(Rectangle? controlBounds, Rectangle visibleBounds);
    }
    #region class DxDataFormPage : Data jedné stránky (záložky) DataFormu
    /// <summary>
    /// Data jedné stránky (záložky) DataFormu: ID, titulek, ikona, vizuální control <see cref="DxDataFormScrollPanel"/>.
    /// Tento vizuální control může být umístěn přímo v <see cref="DxDataForm"/> (což je vizuální panel),
    /// anebo může být umístěn na záložce.
    /// </summary>
    public class DxDataFormPage : DxDataFormScrollPanel
    {
        #region Konstruktor, proměnné, Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataForm"></param>
        public DxDataFormPage(DxDataForm dataForm)
            : base(dataForm)
        {
            IsActiveContentInternal = false;
        }
        /// <summary>
        /// Jméno panelu
        /// </summary>
        public override string DebugName { get { return PageText; } }
        /// <summary>
        /// Dispose
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this._ClearInstance();
        }
        /// <summary>
        /// Uvolní instance na které drží referenci, neřeší ale jejich Dispose.
        /// </summary>
        private void _ClearInstance()
        {
        }
        /// <summary>
        /// Název stránky = klíč
        /// </summary>
        public string PageName { get; set; }
        /// <summary>
        /// Titulek stránky
        /// </summary>
        public string PageText { get; set; }
        /// <summary>
        /// Text ToolTipu stránky (jako Titulek ToolTipu slouží <see cref="PageText"/>)
        /// </summary>
        public string PageToolTipText { get; set; }
        /// <summary>
        /// Ikona stránky
        /// </summary>
        public string PageIconName { get; set; }
        /// <summary>
        /// Vepíše do svých proměnných data z daného prvku
        /// </summary>
        /// <param name="item"></param>
        public void FillFrom(IDataFormItem item)
        {
            this.PageName = item.PageName;
            this.PageText = item.PageText;
            this.PageToolTipText = item.PageToolTipText;
            this.PageIconName = item.PageIconName;
        }
        #endregion
    }
    #endregion
    #region class DxDataFormScrollPanel : Container, který hostuje DxDataFormContentPanel, a který se dokuje do parenta
    /// <summary>
    /// Container, který hostuje DxDataFormContentPanel, a který se dokuje do parenta = jeho velikost je omezená, 
    /// a hostuje v sobě <see cref="DxDataFormContentPanel"/>, který má velikost odpovídající svému obsahu a tento Content je pak posouván uvnitř this panelu = Scroll obsahu.
    /// Tento container v sobě obsahuje List <see cref="Items"/> jeho jednotlivých Controlů typu <see cref="DxDataFormControlItem"/>.
    /// </summary>
    public class DxDataFormScrollPanel : DxAutoScrollPanelControl, IDxDataFormScrollPanel
    {
        #region Konstruktor, proměnné, Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataForm"></param>
        public DxDataFormScrollPanel(DxDataForm dataForm)
        {
            __DataForm = dataForm;
            __ContentPanel = new DxDataFormContentPanel(this);
            __Items = new List<DxDataFormControlItem>();
            __IsActiveContent = true;
            __CurrentlyFocusedDataItem = null;
            this.Controls.Add(ContentPanel);
            this.Dock = WF.DockStyle.Fill;
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.DebugName;
        }
        /// <summary>
        /// Jméno panelu
        /// </summary>
        public virtual string DebugName { get { return $"ScrollPanel [{IDataForm.IndexOf(this)}]"; } }
        /// <summary>
        /// Dispose prvku
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.DisposeContent();
            base.Dispose(disposing);
            this._ClearInstance();
        }
        /// <summary>
        /// Uvolní instance na které drží referenci, neřeší ale jejich Dispose.
        /// </summary>
        private void _ClearInstance()
        {
            __DataForm = null;
            __ContentPanel = null;     // Instance byla Disposována standardně v this.Dispose() =>  this.DisposeContent();, tady jen zahazuji referenci na zombie objekt
            __Items?.Clear();          // Jednotlivé prvky nedisposujeme zde, ale na úrovni DxDataForm, protože tam je vytváříme a společně je tam evidujeme pod klíčem.
            __Items = null;
            __CurrentlyFocusedDataItem = null;
        }
        /// <summary>
        /// Odkaz na main instanci DataForm
        /// </summary>
        public DxDataForm DataForm { get { return __DataForm; } }
        private DxDataForm __DataForm;
        /// <summary>
        /// Odkaz na main instanci DataForm typovanou pro interní přístup
        /// </summary>
        private IDxDataForm IDataForm { get { return __DataForm; } }
        /// <summary>
        /// Vizuální panel, který má velikost pokrývající všechny Controly, je umístěn v this, a je posouván pomocí AutoScrollu
        /// </summary>
        internal DxDataFormContentPanel ContentPanel { get { return __ContentPanel; } }
        private DxDataFormContentPanel __ContentPanel;
        /// <summary>
        /// Soupis controlů, které jsou obsaženy v this ScrollPanelu (fyzicky jsou ale umístěny v <see cref="ContentPanel"/>)
        /// </summary>
        internal List<DxDataFormControlItem> Items { get { return __Items; } }
        private List<DxDataFormControlItem> __Items;
        /// <summary>
        /// Obsahuje true pokud this page neobsahuje žádný control
        /// </summary>
        public bool IsEmpty { get { return (this.__Items.Count == 0); } }
        #endregion
        #region Control s focusem, obecně focus
        /// <summary>
        /// Control, který má aktuálně focus. Lze setovat hodnotu, ve vizuálním containeru dostane daný prvek Focus.
        /// Při jakékoli změně focusu je volán event <see cref="FocusedItemChanged"/>.
        /// Zde se pracuje s popisnými daty typu <see cref="IDataFormItem"/>, které se vkládají do metody <see cref="AddItem(IDataFormItem, bool)"/>.
        /// </summary>
        public IDataFormItem FocusedItem { get { return __CurrentlyFocusedDataItem?.DataFormItem; } set { _SetFocusToItem(value); } }
        /// <summary>
        /// Aktivní prvek, hodnotu do této property setuje vlastní prvek ve své události GotFocus.
        /// Setování hodnoty tedy nemá měnit aktivní focus (to bychom nikdy neskončili), ale má řešit následky skutečné změny focusu.
        /// </summary>
        DxDataFormControlItem IDxDataFormScrollPanel.ActiveItem { get { return __CurrentlyFocusedDataItem; } set { _ActivatedItem(value); } }
        /// <summary>
        /// Zajistí předání focusu do daného prvku, pokud to je možné.
        /// Pokud vstupní prvek neodpovídá existujícímu controlu, ke změně focusu nedojde.
        /// </summary>
        /// <param name="item"></param>
        private void _SetFocusToItem(IDataFormItem item)
        {
            DxDataFormControlItem dataItem = (item != null ? this.__Items.FirstOrDefault(i => i.ContainsItem(item) && i.IsFocusableControl) : null);
            if (dataItem == null) return;

            // Prvek (dataItem) má mít focus (z logiky toho, že jsme tady),
            if (!dataItem.IsHosted)
            {   // a pokud aktuálně není hostován = není přítomen v Parent containeru,
                //  zajistíme, že Focusovaný prvek bude fyzicky vytvořen a umístěn do Parent containeru:
                RefreshVisibleItems("SetFocusToItem");
            }

            // Tato metoda nemění obsah proměnných (__CurrentlyFocusedDataItem, __PreviousFocusableDataItem, __NextFocusableDataItem).
            // To proběhne až jako reakce na GotFocus pomocí setování fokusovaného prvku do ActiveItem, následně metoda _ActivatedItem()...
            // Tady jen dáme vizuální focus:
            dataItem.SetFocus();
        }
        /// <summary>
        /// Je voláno poté, kdy byl aktivován daný control.
        /// To může být jak z aplikačního kódu (setování <see cref="FocusedItem"/>, tak z GUI, pohybem po controlech a následně event GotFocus, 
        /// který setuje focusovaný prvek do <see cref="IDxDataFormScrollPanel.ActiveItem"/>. 
        /// Nikdy se nesetuje NULL.
        /// </summary>
        /// <param name="dataItem"></param>
        private void _ActivatedItem(DxDataFormControlItem dataItem)
        {
            bool isChange = !Object.ReferenceEquals(dataItem, __CurrentlyFocusedDataItem);

            __CurrentlyFocusedDataItem = dataItem;
            _SearchNearControls(dataItem);
            _EnsureHostingFocusableItems();

            if (isChange)
                RunFocusedItemChanged();
        }
        /// <summary>
        /// Vyvolá události <see cref="OnFocusedItemChanged(TEventArgs{DxDataFormControlItem})"/> a <see cref="FocusedItemChanged"/>
        /// </summary>
        private void RunFocusedItemChanged()
        {
            TEventArgs<DxDataFormControlItem> args = new TEventArgs<DxDataFormControlItem>(__CurrentlyFocusedDataItem);
            OnFocusedItemChanged(args);
            FocusedItemChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Proběhne po změně focusovaného prvku <see cref="FocusedItem"/>
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnFocusedItemChanged(TEventArgs<DxDataFormControlItem> args) { }
        /// <summary>
        /// Vyvoolá se po změně focusovaného prvku <see cref="FocusedItem"/>
        /// </summary>
        public event EventHandler<TEventArgs<DxDataFormControlItem>> FocusedItemChanged;
        /// <summary>
        /// Najde a zapamatuje si referenci na nejbližší controly před a za daným prvkem.
        /// Tyto prvky jsou ty, které budou dosažitelné z daného prvku pomocí Tab a ShiftTab, a musí tedy být fyzicky přítomny na <see cref="ContentPanel"/>, aby focus správně chodil.
        /// </summary>
        /// <param name="dataItem"></param>
        private void _SearchNearControls(DxDataFormControlItem dataItem)
        {
            List<DxDataFormControlItem> items = this.Items;

            //   Izolovat a setřídit?
            // items = items.ToList();
            // items.Sort(DxDataFormControlItem.CompareByTabOrder);

            int index = items.FindIndex(i => Object.ReferenceEquals(i, dataItem));
            __PreviousFocusableDataItem = _SearchNearControl(items, index, -1, false);
            __NextFocusableDataItem = _SearchNearControl(items, index, 1, false);
        }
        /// <summary>
        /// Najde a vrátí prvek, který se v daném seznamu nachází nedaleko daného indexu v daném směru a může dostat focus.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="index"></param>
        /// <param name="step"></param>
        /// <param name="acceptIndex"></param>
        /// <returns></returns>
        private DxDataFormControlItem _SearchNearControl(List<DxDataFormControlItem> items, int index, int step, bool acceptIndex)
        {
            int count = items.Count;
            if (count == 0) return null;

            index = (index < 0 ? 0 : (index >= count ? count - 1 : index));    // Zarovnat index do mezí 0 až (count-1)
            int i = index;
            bool accept = acceptIndex;
            for (int t = 0; t < count; t++)
            {   // t není index, t je timeout!
                if (accept)
                {
                    if (items[i].CanGotFocus)
                        return items[i];
                }
                else
                {   // Další prvek budeme akceptovat vždy
                    accept = true;
                }
                i += step;
                if (i == index) break;

                // Dokola:
                if (i < 0) i = count - 1;
                else if (i >= count) i = 0;
            }
            return null;
        }
        /// <summary>
        /// Metoda zajistí, že prvky, které mají nebo mohou dostat nejbližší focus, budou hostovány v <see cref="ContentPanel"/>.
        /// Jde o prvky: <see cref="__CurrentlyFocusedDataItem"/>, <see cref="__PreviousFocusableDataItem"/>, <see cref="__NextFocusableDataItem"/>.
        /// Volá se po změně objektů uložených v těchto proměnných.
        /// Metoda zjistí, zda všechny objekty (které nejsou null) mají IsHost true, a pokud ne pak vyvolá 
        /// </summary>
        private void _EnsureHostingFocusableItems()
        {
            bool needRefresh = ((__CurrentlyFocusedDataItem != null && !__CurrentlyFocusedDataItem.IsHosted) ||
                                (__PreviousFocusableDataItem != null && !__PreviousFocusableDataItem.IsHosted) ||
                                (__NextFocusableDataItem != null && !__NextFocusableDataItem.IsHosted));
            if (needRefresh)
                RefreshVisibleItems("EnsureHostingFocusableItems");
        }
        /// <summary>
        /// Vrátí true pokud daný prvek má být zařazen mezi hostované prvky z důvodu Focusu (aktuální, předchozí, následující)
        /// </summary>
        /// <param name="dataItem"></param>
        /// <returns></returns>
        bool IDxDataFormScrollPanel.IsNearFocusableItem(DxDataFormControlItem dataItem)
        {
            if (dataItem != null)
            {
                if (__CurrentlyFocusedDataItem != null && Object.ReferenceEquals(dataItem, __CurrentlyFocusedDataItem)) return true;
                if (__PreviousFocusableDataItem != null && Object.ReferenceEquals(dataItem, __PreviousFocusableDataItem)) return true;
                if (__NextFocusableDataItem != null && Object.ReferenceEquals(dataItem, __NextFocusableDataItem)) return true;
            }
            return false;
        }
        /// <summary>
        /// Prvek, který má aktuálně focus
        /// </summary>
        DxDataFormControlItem __CurrentlyFocusedDataItem;
        /// <summary>
        /// Prvek, který je vlevo od focusu (má být zobrazen i když není vidět)
        /// </summary>
        DxDataFormControlItem __PreviousFocusableDataItem;
        /// <summary>
        /// Prvek, který je vpravo od focusu (má být zobrazen i když není vidět)
        /// </summary>
        DxDataFormControlItem __NextFocusableDataItem;

        #endregion


        /// <summary>
        /// Je provedeno po změně <see cref="DxAutoScrollPanelControl.VisibleBounds"/>.
        /// </summary>
        protected override void OnVisibleBoundsChanged()
        {
            base.OnVisibleBoundsChanged();
            ContentViewChanged();
        }
        /// <summary>
        /// Po změně viditelného prostoru provede Refresh viditelných controlů
        /// </summary>
        private void ContentViewChanged()
        {
            if (this.ContentPanel == null) return;                   // Toto nastane, pokud je voláno v procesu konstruktoru (což je, protože se mění velikost)
            this.ContentPanel.ContentVisibleBounds = this.VisibleBounds;
            RefreshVisibleItems("ContentViewChanged");
        }
        /// <summary>
        /// Do své evidence přidá control pro danou definici <paramref name="item"/>.
        /// Volitelně vynechá finalizaci (refreshe), to je vhodné pokud se z vyšších úrovní volá vícekrát AddItem opakovaně a finalizace se provede na závěr.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="skipFinalise"></param>
        /// <returns></returns>
        internal DxDataFormControlItem AddItem(IDataFormItem item, bool skipFinalise = false)
        {
            DxDataFormControlItem controlItem = new DxDataFormControlItem(this, item);
            Items.Add(controlItem);
            if (!skipFinalise)
                FinaliseContent();
            return controlItem;
        }
        /// <summary>
        /// Volá se po dokončení přidávání nebo přemisťování nebo odebírání prvků.
        /// </summary>
        internal void FinaliseContent()
        {
            RefreshContentSize();
            RefreshVisibleItems("FinaliseContent");
        }
        /// <summary>
        /// Z jednotlivých controlů vypočte potřebnou velikost pro <see cref="ContentPanel"/> a vepíši ji do něj.
        /// Tím se zajistí správné Scrollování obsahu.
        /// </summary>
        private void RefreshContentSize()
        {
            int maxR = 0;
            int maxB = 0;
            int tabIndex = 0;
            foreach (var item in Items)
            {
                item.TabIndex = tabIndex++;
                var bounds = item.Bounds;
                if (bounds.HasValue)
                {
                    if (bounds.Value.Right > maxR) maxR = bounds.Value.Right;
                    if (bounds.Value.Bottom > maxB) maxB = bounds.Value.Bottom;
                }
            }
            maxR += 6;
            maxB += 6;
            this.ContentPanel.Bounds = new Rectangle(0, 0, maxR, maxB);
        }
        /// <summary>
        /// Zajistí refresh viditelnosti prvků podle aktuální viditelné oblasti a dalších parametrů.
        /// Výsledkem je vytvoření controlu nebo jeho uvolnění podle potřeby.
        /// </summary>
        /// <param name="reason"></param>
        private void RefreshVisibleItems(string reason)
        {
            long beginTime = DxComponent.LogTimeCurrent;
            long startTime;

            DxComponent.LogAddTitle($"ScrollPanel '{DebugName}' RefreshVisibleItems");

            RefreshItemsInfo refreshInfo = new RefreshItemsInfo(this.ClientSize, this.VisibleBounds, this.CanOptimizeControls, this.IsActiveContent, DataForm.MemoryMode);

            // Tady proběhne příprava = vytvoření new instancí controlů, uložení controlů do refreshInfo pro hromadné přidání a pro hromadný release:
            startTime = DxComponent.LogTimeCurrent;
            foreach (var item in Items)
                item.PrepareVisibleItem(refreshInfo);
            DxComponent.LogAddLineTime($"ScrollPanel '{DebugName}' PrepareVisibleItems(): Items: {Items.Count}; {refreshInfo}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);

            // Tady hromadně přidám a odeberu controly z daného pole:
            if (refreshInfo.NeedRefreshContent)
                this.ContentPanel.RefreshVisibleItems(refreshInfo);

            // Tady proběhne závěr = nastavení proměnných a uvolnění z paměti pro zahozené controly:
            if (refreshInfo.NeedFinaliseItems)
            {
                startTime = DxComponent.LogTimeCurrent;
                foreach (var item in refreshInfo.AddedItems)
                    item.FinaliseVisibleItemAdd(refreshInfo);
                foreach (var item in refreshInfo.RemovedItems)
                    item.FinaliseVisibleItemRemoved(refreshInfo);
                DxComponent.LogAddLineTime($"ScrollPanel '{DebugName}' FinaliseVisibleItems(): Items: {Items.Count}; {refreshInfo}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
            }

            DxComponent.LogAddLineTime($"ScrollPanel '{DebugName}' RefreshVisibleItems({reason}); TotalTime: {DxComponent.LogTokenTimeMilisec}", beginTime);
        }
        /// <summary>
        /// Tohle bychom měli umět...
        /// </summary>
        /// <param name="items"></param>
        private void RemoveItems(IEnumerable<DxDataFormControlItem> items)
        {

        }
        /// <summary>
        /// Obsahuje true, pokud se v aktuální situaci má řešit optimalizace počtu vidielných controlů podle viditelné oblasti.
        /// <para/>
        /// True vrací tehdy, když prvek je NEaktivní <see cref="IsActiveContent"/> 
        /// (protože neaktivní prvek má mít vždy nulový počet controlů bez ohledu na svoje rozměry).
        /// Pak optimalizace zajistí odebrání všech prvků a úsporu paměti.
        /// <para/>
        /// True vrací tehdy, když prvek je AKTIVNÍ a jeho plná fyzická velikost je znatelně menší než velikost viditelná 
        /// (protože pak optimalizace zajistí používání menšího počtu controlů než je plný obsah, a ušetří se systémové zdroje).
        /// <para/>
        /// False vrací tehdy, když prvek je AKTIVNÍ a jeho plná fyzická velikost je poměrně podobná velikosti viditelné 
        /// (protože pak bude ve viditelné oblasti více než cca 90% controlů, a pak by režie s optimalizací neušetřila mnoho zdrojů).
        /// </summary>
        private bool CanOptimizeControls
        {
            get
            {
                if (!this.IsActiveContent) return true;              // Nejsem aktivní => optimalizace zajistí odebrání všech prvků => zásadní úspora paměti
                Size contentSize = this.ContentPanel.ClientSize;
                int contentWidth = contentSize.Width;
                int contentHeight = contentSize.Height;
                Size visibleSize = this.VisibleBounds.Size;
                int visibleWidth = visibleSize.Width;
                int visibleHeight = visibleSize.Height;

                if (visibleWidth > contentWidth) visibleWidth = contentWidth;         // Pokud Visible prostor je širší než je obsah, pak pro další výpočet beru jen potřebné Content pixely
                if (visibleHeight > contentHeight) visibleHeight = contentHeight;     //  aby optimalizace zabrala i tehdy, když Content je např. úzký a vysoký, a Visible je široký a nízký (pak reálně vidím třeba 30% obsahu, i když sumární počet pixelů je srovnatelný)

                decimal contentPixel = (decimal)(contentWidth * contentHeight);
                if (contentPixel < 100) return false;                // 100px (čtverečných) => nějaký minimalistický control => nebudeme optimalizovat
                
                decimal visiblePixel = (decimal)(visibleWidth * visibleHeight);
                decimal visibleratio = visiblePixel / contentPixel;  // Poměr viditelné části k celkové ploše: čím méně vidime, tím víc scrollujeme, a tím víc potřebujeme optimalizaci!

                return (visibleratio <= 0.9m);                       // Pokud vidíme 90% a méně, zapneme optimalizaci. POkud vidíme téměř nebo úplně vše, pak ji neřešíme = úspora paměti nestojí za tu práci.
            }

        }
        /// <summary>
        /// Obsahuje true, pokud obsah je aktivní, false pokud nikoliv. Výchozí je true.
        /// Lze setovat, okamžitě se projeví.
        /// Pokud bude setováno false, provede se screenshot aktuálního stavu do bitmapy a ta se bude vykreslovat, poté se zlikvidují controly.
        /// </summary>
        public bool IsActiveContent 
        {
            get { return __IsActiveContent; }
            set 
            {
                if (value == __IsActiveContent) return;

                if (__IsActiveContent) this.ContentPanel.CreateScreenshot();
                else this.ContentPanel.ReleaseScreenshot(false);

                __IsActiveContent = value; 
                RefreshVisibleItems("IsActiveContent changed"); 
            } 
        }
        /// <summary>
        /// True značí aktivní obsah. Setování nevyvolá refresh obsahu.
        /// </summary>
        protected bool IsActiveContentInternal { get { return __IsActiveContent; } set { __IsActiveContent = value; } }
        /// <summary>
        /// True značí aktivní obsah
        /// </summary>
        private bool __IsActiveContent;
        /// <summary>
        /// Umístí svůj vizuální container do daného Parenta.
        /// Před tím prověří, zda v něm již není a pokud tam už je, pak nic nedělá. Lze tedy volat libovolně často.
        /// </summary>
        /// <param name="parent"></param>
        public void PlaceToParent(WF.Control parent)
        {
            if (parent != null && this.Parent != null && Object.ReferenceEquals(this.Parent, parent)) return;           // Beze změny

            ReleaseFromParent();

            if (parent != null)
                parent.Controls.Add(this);
        }
        /// <summary>
        /// Odebere svůj vizuální container z jeho dosavadního Parenta
        /// </summary>
        public void ReleaseFromParent()
        {
            var parent = this.Parent;
            if (parent != null)
                parent.Controls.Remove(this);
        }
        #region class RefreshItemsInfo : sumarizační třída pro RefreshItems mezi ScrollPanel a ControlInfo
        /// <summary>
        /// Sumarizační třída pro RefreshItems mezi <see cref="DxDataFormScrollPanel"/> a <see cref="DxDataFormControlItem"/>
        /// </summary>
        internal class RefreshItemsInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="totalSize"></param>
            /// <param name="visibleBounds"></param>
            /// <param name="optimizeControls"></param>
            /// <param name="isActiveContent"></param>
            /// <param name="memoryMode"></param>
            public RefreshItemsInfo(Size totalSize, Rectangle visibleBounds, bool optimizeControls, bool isActiveContent, DxDataFormMemoryMode memoryMode)
            {
                this.TotalSize = totalSize;
                this.VisibleBounds = visibleBounds;
                this.OptimizeControls = optimizeControls;
                this.IsActiveContent = isActiveContent;
                this.MemoryMode = memoryMode;
                this.AddedItems = new List<DxDataFormControlItem>();
                this.RemovedItems = new List<DxDataFormControlItem>();
            }
            public Size TotalSize { get; private set; }
            public Rectangle VisibleBounds { get; private set; }
            public bool OptimizeControls { get; private set; }
            public bool IsActiveContent { get; private set; }
            public DxDataFormMemoryMode MemoryMode { get; private set; }
            public bool ModeIsHostAllways { get { return MemoryMode.HasFlag(DxDataFormMemoryMode.HostAllways); } }
            public bool ModeIsRemoveReleaseHandle { get { return MemoryMode.HasFlag(DxDataFormMemoryMode.RemoveReleaseHandle); } }
            public bool ModeIsRemoveDispose { get { return MemoryMode.HasFlag(DxDataFormMemoryMode.RemoveDispose); } }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.Text;
            }
            /// <summary>
            /// Textové vyjádření
            /// </summary>
            public string Text
            {
                get
                {
                    string text = "";
                    if (CreatedCount > 0) text += "; Created: " + CreatedCount;
                    if (HostedCount > 0) text += "; Hosted: " + HostedCount;
                    if (VisibleCount > 0) text += "; Visible: " + VisibleCount;
                    if (RemovedCount > 0) text += "; Removed: " + RemovedCount;
                    if (DestroyedCount > 0) text += "; Destroyed: " + DestroyedCount;
                    if (DisposedCount > 0) text += "; Disposed: " + DisposedCount;
                    if (text.Length > 0) text = text.Substring(2);
                    else text = "No actions";
                    return text;
                }
            }
            /// <summary>
            /// Pole controlů, které je třeba přidat hromadně do ContentPanelu
            /// </summary>
            public List<DxDataFormControlItem> AddedItems { get; private set; }
            /// <summary>
            /// Pole controlů, které je třeba přidat hromadně do ContentPanelu
            /// </summary>
            public List<DxDataFormControlItem> RemovedItems { get; private set; }
            /// <summary>
            /// Obsahuje true pokud je třeba změnit obsah Content panelu (tedy máme controly k přidání anebo k odebrání)
            /// </summary>
            public bool NeedRefreshContent { get { return (this.AddedItems.Count > 0 || this.RemovedItems.Count > 0); } }
            /// <summary>
            /// Obsahuje true pokud je třeba volat Finalise pro jednotlivé prvky
            /// </summary>
            public bool NeedFinaliseItems { get { return (this.AddedItems.Count > 0 || this.RemovedItems.Count > 0); } }
            /// <summary>
            /// Počet new instancí vytvořených Controlů
            /// </summary>
            public int CreatedCount { get; set; }
            /// <summary>
            /// Počet Controlů nyní umístěných do Parent containeru
            /// </summary>
            public int HostedCount { get; set; }
            /// <summary>
            /// Počet Controlů aktuálně viditelných (hostovaných v Containeru)
            /// </summary>
            public int VisibleCount { get; set; }
            /// <summary>
            /// Počet Controlů odebraných z Parent Containeru
            /// </summary>
            public int RemovedCount { get; set; }
            /// <summary>
            /// Počet Controlů s uvolněným handle (DestroyWindow)
            /// </summary>
            public int DestroyedCount { get; set; }
            /// <summary>
            /// Počet Controlů disposovaných
            /// </summary>
            public int DisposedCount { get; set; }
        }
        #endregion

    }
    /// <summary>
    /// Interní přístup do <see cref="DxDataFormScrollPanel"/>
    /// </summary>
    public interface IDxDataFormScrollPanel
    {
        /// <summary>
        /// Aktivní prvek, hodnotu do této property setuje prvek ve své události GotFocus.
        /// Setování hodnoty tedy nemá měnit aktivní focus (to bychom nikdy neskončili), ale má řešit následky skutečné změny focusu.
        /// </summary>
        DxDataFormControlItem ActiveItem { get; set; }
        /// <summary>
        /// Vrátí true pokud daný prvek má být zařazen mezi hostované prvky z důvodu Focusu (aktuální, předchozí, následující)
        /// </summary>
        /// <param name="dataItem"></param>
        /// <returns></returns>
        bool IsNearFocusableItem(DxDataFormControlItem dataItem);
    }
    #endregion
    #region class DxDataFormContentPanel : Hostitelský panel pro jednotlivé Controly
    /// <summary>
    /// Hostitelský panel pro jednotlivé Controly.
    /// Tento panel si udržuje svoji velikost odpovídající všem svým Controlům, 
    /// není Dock, není AutoScroll (to je jeho Parent = <see cref="DxDataFormScrollPanel"/>).
    /// </summary>
    public class DxDataFormContentPanel : DxPanelControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="scrollPanel"></param>
        public DxDataFormContentPanel(DxDataFormScrollPanel scrollPanel)
            : base()
        {
            this.__ScrollPanel = scrollPanel;
            this.Dock = WF.DockStyle.None;
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.AutoScroll = false;
            this.DoubleBuffered = true;
            this.SetStyle(WF.ControlStyles.UserPaint, true);         // Aby se nám spolehlivě volal OnPaintBackground()
        }
        /// <summary>
        /// Dispose prvku
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.DisposeContent();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Uvolní instance na které drží referenci, neřeší ale jejich Dispose.
        /// </summary>
        private void _ClearInstance()
        {
            ReleaseScreenshot(false);
            __ScrollPanel = null;
        }
        /// <summary>
        /// Main DataForm
        /// </summary>
        public DxDataForm DataForm { get { return __ScrollPanel?.DataForm; } }
        /// <summary>
        /// Odkaz na main instanci DataForm typovanou pro interní přístup
        /// </summary>
        private IDxDataForm IDataForm { get { return DataForm; } }
        /// <summary>
        /// ScrollPanel, který řídí zobrazení zdejšího panelu
        /// </summary>
        public DxDataFormScrollPanel ScrollPanel { get { return __ScrollPanel; } }
        private DxDataFormScrollPanel __ScrollPanel;
        /// <summary>
        /// Aktuálně viditelná oblast this controlu
        /// </summary>
        public Rectangle ContentVisibleBounds { get { return _ContentVisibleBounds; } set { _SetContentVisibleBounds(value); } }
        private Rectangle _ContentVisibleBounds;
        private void _SetContentVisibleBounds(Rectangle contentVisibleBounds)
        {
            if (contentVisibleBounds == _ContentVisibleBounds) return;

            _ContentVisibleBounds = contentVisibleBounds;
        }
        /// <summary>
        /// Zajistí hromadné přidání nových controlů z <paramref name="refreshInfo"/> z <see cref="DxDataFormScrollPanel.RefreshItemsInfo.RemovedItems"/>, 
        /// a odebrání (bohužel jednotkové) controlů z <see cref="DxDataFormScrollPanel.RefreshItemsInfo.RemovedItems"/>
        /// </summary>
        /// <param name="refreshInfo"></param>
        internal void RefreshVisibleItems(DxDataFormScrollPanel.RefreshItemsInfo refreshInfo)
        {
            long startTime;
            int count;

            // Následující dva řádky spotřebují řádově 4 mikrosekundy, nebudu s tím přetěžovat logovací informace:
            //   startTime = DxComponent.LogTimeCurrent;
            this.SuspendLayout();
            this.BeginInit();
            //   DxComponent.LogAddLineTime($"ContentPanel '{ScrollPanel?.DebugName}'; RefeshVisibleItems; SuspendBegin; Time: {DxComponent.LogTokenTimeMilisec}", startTime);

            count = refreshInfo.AddedItems.Count;
            if (count > 0)
            {
                startTime = DxComponent.LogTimeCurrent;
                var addControls = refreshInfo.AddedItems.Select(i => i.Control).ToArray();
                this.Controls.AddRange(addControls);
                refreshInfo.HostedCount += count;
                DxComponent.LogAddLineTime($"ContentPanel '{ScrollPanel?.DebugName}'; RefeshVisibleItems; AddControls: {count}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
            }

            count = refreshInfo.RemovedItems.Count;
            if (count > 0)
            {
                startTime = DxComponent.LogTimeCurrent;
                var removeControls = refreshInfo.RemovedItems.Select(i => i.Control).ToArray();
                // this.Controls.RemoveRange( -- neexistuje :-( -- )
                foreach (var removeControl in removeControls)
                    this.Controls.Remove(removeControl);
                refreshInfo.RemovedCount += count;
                DxComponent.LogAddLineTime($"ContentPanel '{ScrollPanel?.DebugName}'; RefeshVisibleItems; RemoveControls: {count}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
            }

            // Tohle vezme řádově 0,5 milisekundy, to do logu tedy dáme:
            startTime = DxComponent.LogTimeCurrent;
            this.EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
            DxComponent.LogAddLineTime($"ContentPanel '{ScrollPanel?.DebugName}'; RefeshVisibleItems; SuspendEnd; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        #region Screenshot
        /// <summary>
        /// Z aktuálního stavu controlu vytvoří a uloží Screenshot, který se bude kreslit na pozadí controlu.
        /// Poté mohou být všechny Child controly zahozeny a přitom this control bude vypadat jako by tam stále byly (ale budou to jen duchy).
        /// </summary>
        internal void CreateScreenshot()
        {
            ReleaseScreenshot(true);

            Point empty = Point.Empty;
            Size clientBounds = this.ClientSize;
            Bitmap bmp = new Bitmap(clientBounds.Width, clientBounds.Height);
            Rectangle target = new Rectangle(Point.Empty, clientBounds);
            this.DrawToBitmap(bmp, target);

            __Screenshot = bmp;
        }
        /// <summary>
        /// Pokud máme uchovaný Screenshot, pak jej korektně zahodí a volitelně provede invalidaci = překreslení obsahu.
        /// To je vhodné za provozu, ale není to vhodné v Dispose.
        /// </summary>
        /// <param name="withInvalidate"></param>
        internal void ReleaseScreenshot(bool withInvalidate)
        {
            if (__Screenshot != null)
            {
                try { __Screenshot.Dispose(); }
                catch { }
                
                __Screenshot = null;

                if (withInvalidate)
                    this.Invalidate();
            }
        }
        /// <summary>
        /// Po vykreslení pozadí přes něj mohu vykreslit Screenshot, pokud existuje
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaintBackground(WF.PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            PaintScreenshot(e);
        }
        /// <summary>
        /// Volá se po vykreslení OnPaintBackground, vykreslí Screenshot pokud existuje
        /// </summary>
        /// <param name="e"></param>
        private void PaintScreenshot(WF.PaintEventArgs e)
        {
            Bitmap bmp = __Screenshot;
            if (bmp == null) return;

            e.Graphics.DrawImage(bmp, Point.Empty);
        }

        private Bitmap __Screenshot;
        #endregion

    }
    #endregion
    #region class DxDataFormControlItem : Třída obsahující každý jeden prvek controlu v rámci DataFormu
    /// <summary>
    /// <see cref="DxDataFormControlItem"/> : Třída obsahující každý jeden prvek controlu v rámci DataFormu:
    /// jeho definici <see cref="IDataFormItem"/> i fyzický control.
    /// Umožňuje řešit jeho tvorbu a uvolnění OnDemand = podle viditelnosti v rámci Parenta.
    /// Šetří tak čas a paměťové nároky.
    /// </summary>
    public class DxDataFormControlItem : IDisposable
    {
        #region Konstruktor, Dispose, proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="scrollPanel"></param>
        /// <param name="dataFormItem"></param>
        /// <param name="control"></param>
        public DxDataFormControlItem(DxDataFormScrollPanel scrollPanel, IDataFormItem dataFormItem, WF.Control control = null)
        {
            if (dataFormItem is null)
                throw new ArgumentNullException("dataFormItem", "DxDataFormControlItem(dataFormItem) is null.");

            __ScrollPanel = scrollPanel;
            __DataFormItem = dataFormItem;
            __Control = control;
            __IsFocusableControl = DxDataForm.IsFocusableControl(dataFormItem.ItemType);
            if (control != null)
            {
                __ControlIsExternal = true;
                RegisterControlEvents(control);
            }
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ItemType: {this.__DataFormItem.ItemType}; Bounds: {this.Bounds}; IsHosted: {IsHosted}";
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            _ClearInstance();
        }
        /// <summary>
        /// Uvolní instance na které drží referenci, neřeší ale jejich Dispose (s výjimkou <see cref="__Control"/>, pokud jsme ji vytvářeli zde a stále existuje, pak ji Disposuje)
        /// </summary>
        private void _ClearInstance()
        {
            ReleaseControl(null, true);
            __ScrollPanel = null;
            __Control = null;
        }
        /// <summary>
        /// Main DataForm
        /// </summary>
        public DxDataForm DataForm { get { return __ScrollPanel?.DataForm; } }
        /// <summary>
        /// Odkaz na main instanci DataForm typovanou pro interní přístup
        /// </summary>
        private IDxDataForm IDataForm { get { return DataForm; } }
        /// <summary>
        /// Režim práce s pamětí
        /// </summary>
        protected DxDataFormMemoryMode MemoryMode { get { return (DataForm?.MemoryMode ?? DxDataFormMemoryMode.Default); } }
        /// <summary>
        /// ScrollPanel, který řídí zobrazení našeho <see cref="ContentPanel"/>
        /// </summary>
        public DxDataFormScrollPanel ScrollPanel { get { return __ScrollPanel; } }
        /// <summary>
        /// ScrollPanel pro interní přístup
        /// </summary>
        protected IDxDataFormScrollPanel IScrollPanel { get { return __ScrollPanel; } }
        private DxDataFormScrollPanel __ScrollPanel;
        /// <summary>
        /// Panel, v němž bude this control fyzicky umístěn
        /// </summary>
        public DxDataFormContentPanel ContentPanel { get { return __ScrollPanel?.ContentPanel; } }
        /// <summary>
        /// Fyzický control
        /// </summary>
        public WF.Control Control { get { return __Control; } }
        private WF.Control __Control;
        /// <summary>
        /// Obsahuje true tehdy, když zdejší <see cref="Control"/> je dodán externě. 
        /// Pak jej nemůžeme Disposovat a znovuvytvářet, ale musíme jej držet permanentně.
        /// </summary>
        private bool __ControlIsExternal;
        /// <summary>
        /// Definice jednoho prvku
        /// </summary>
        public IDataFormItem DataFormItem { get { return __DataFormItem; } }
        private IDataFormItem __DataFormItem;
        /// <summary>
        /// Obsahuje true tehdy, když zdejší prvek <see cref="DataFormItem"/> může dostat Focus podle svého typu.
        /// </summary>
        public bool IsFocusableControl { get { return __IsFocusableControl; } }
        private bool __IsFocusableControl;
        #endregion
        #region Řízení viditelnosti, OnDemand tvorba a release fyzického Controlu
        /// <summary>
        /// Index prvku pro procházení přes TAB
        /// </summary>
        public int TabIndex { get; set; }
        /// <summary>
        /// Souřadnice zjištěné primárně z <see cref="DataFormItem"/>, sekundárně z <see cref="Control"/>. Nebo null.
        /// </summary>
        public Rectangle? Bounds 
        {
            get 
            {
                if (__DataFormItem != null) return __DataFormItem.Bounds;
                if (__Control != null) return __Control.Bounds;
                return null;
            }
        }
        /// <summary>
        /// Obsahuje true pro prvek, který je aktuálně umístěn ve viditelném panelu
        /// </summary>
        public bool IsHosted { get; private set; }
        /// <summary>
        /// Zajistí předání Focusu do this prvku.
        /// Pokud prvek dosud neměl Focus, dostane jej a to vyvolá událost GotFocus.
        /// </summary>
        public void SetFocus()
        {
            var control = this.Control;
            if (control != null)
                control.Focus();
        }
        /// <summary>
        /// Obsahuje true pokud this prvek může dostat Focus.
        /// Tedy prvek musí být obecně fokusovatelný (nikoli Label), musí být obecně Viditelný <see cref="ItemVisible"/>,
        /// musí být Enabled a TabStop.
        /// </summary>
        public bool CanGotFocus
        {
            get
            {
                if (!IsFocusableControl) return false;
                if (!ItemVisible) return false;


                return true;
            }
        }
        /// <summary>
        /// Zajistí, že this prvek bude zobrazen podle toho, zda se nachází v dané viditelné oblasti
        /// </summary>
        /// <param name="refreshInfo"></param>
        internal void PrepareVisibleItem(DxDataFormScrollPanel.RefreshItemsInfo refreshInfo)
        {
            bool needHost = _IsVisibleItem(refreshInfo);             // Zdejší control MÁ BÝT umístěn v Content panelu?
            bool isHosted = IsHosted && (__Control != null);         // Zdejší control JE NYNÍ umístěn v Content panelu?

            if (needHost)
            {
                if (!isHosted)
                {
                    PrepareControl(refreshInfo);
                    refreshInfo.AddedItems.Add(this);
                }
                refreshInfo.VisibleCount++;
                RefreshItemValues();
            }
            else if (isHosted && !needHost)
            {
                refreshInfo.RemovedItems.Add(this);
            }
        }
        /// <summary>
        /// Dokončovací práce po výměně komponenty v ContentPanelu - po přidání nové komponenty pro tento prvek
        /// </summary>
        /// <param name="refreshInfo"></param>
        internal void FinaliseVisibleItemAdd(DxDataFormScrollPanel.RefreshItemsInfo refreshInfo)
        {
            // Fyzické přidání controlu do ContentPanelu provedl ContentPanel hromadně, tady si jen označíme že jsme hostování:
            IsHosted = true;
        }
        /// <summary>
        /// Dokončovací práce po výměně komponenty v ContentPanelu - po odebrání zdejší komponenty pro tento prvek
        /// </summary>
        /// <param name="refreshInfo"></param>
        internal void FinaliseVisibleItemRemoved(DxDataFormScrollPanel.RefreshItemsInfo refreshInfo)
        {
            ReleaseControl(refreshInfo, false);
        }
        /// <summary>
        /// Obsahuje true pokud this prvek má být někdy viditelný podle definice dat <see cref="IDataFormItem.Visible"/>.
        /// Pokud je tam null, považuje se to za true.
        /// </summary>
        internal bool ItemVisible
        {
            get
            {
                var dataVisible = this.__DataFormItem.Visible;
                if (dataVisible.HasValue && !dataVisible.Value) return false;
                return true;
            }
        }
        /// <summary>
        /// Vrátí true pokud this prvek má být aktuálně přítomen jako živý prvek v controlu <see cref="ContentPanel"/>.
        /// </summary>
        /// <param name="refreshInfo"></param>
        /// <returns></returns>
        private bool _IsVisibleItem(DxDataFormScrollPanel.RefreshItemsInfo refreshInfo)
        {
            // Pokud má být prvek Invisible, je to bez další diskuse:
            if (!ItemVisible) return false;

            // Pokud MemoryMode říká Allways, pak musí být hostován vždy:
            if (refreshInfo.ModeIsHostAllways) return true;

            // Prvek NEMÁ být vidět, pokud je obsah NEAKTIVNÍ (tj. panel je např. na skryté záložce):
            if (!refreshInfo.IsActiveContent) return false;

            // Prvek má být vidět, pokud má klávesový Focus anebo jeho TabIndex je +1 / -1 od aktuálního focusovaného prvku (aby bylo možno na něj přejít klávesou):
            if (this.IScrollPanel.IsNearFocusableItem(this)) return true;

            // Pokud se NEMÁ provádět optimalizace, pak má být prvek vidět bez ohledu na jeho souřadnice ve viditelném prostoru:
            if (!refreshInfo.OptimizeControls) return true;

            // Prvek má být vidět, pokud jeho souřadnice jsou ve viditelné oblasti nebo blízko ní:
            return IDataForm.IsInVisibleBounds(this.Bounds, refreshInfo.VisibleBounds);
        }
        /// <summary>
        /// Aktualizuje hodnoty na controlu, který je právě viditelný
        /// </summary>
        private void RefreshItemValues()
        {
            if (__Control == null) return;
            if (__Control.TabIndex != this.TabIndex) __Control.TabIndex = this.TabIndex;
            if (this.__DataFormItem != null && __Control.Bounds != this.__DataFormItem.Bounds) __Control.Bounds = this.__DataFormItem.Bounds;
        }
        /// <summary>
        /// Zajistí, že pro this prvek bude existovat platný WF Control v <see cref="Control"/>
        /// (tedy pokud je null, pak jej vytvoří a uloží).
        /// Napočítá statistiku a zaregistruje eventy.
        /// Nepřidává do Content panelu-
        /// </summary>
        /// <param name="refreshInfo"></param>
        /// <returns></returns>
        private void PrepareControl(DxDataFormScrollPanel.RefreshItemsInfo refreshInfo)
        {
            WF.Control control = __Control;
            if (control == null || control.IsDisposed)
            {
                __Control = DxComponent.CreateDataFormControl(__DataFormItem);
                refreshInfo.CreatedCount++;
                RegisterControlEvents(__Control);
            }
        }
        /// <summary>
        /// Aktuální control (pokud existuje) odebere z <see cref="ContentPanel"/>, a pak podle daného režimu jej uvolní z paměti (Handle plus Dispose)
        /// </summary>
        /// <param name="refreshInfo"></param>
        /// <param name="isFinal"></param>
        private void ReleaseControl(DxDataFormScrollPanel.RefreshItemsInfo refreshInfo, bool isFinal)
        {
            // Fyzické odebrání controlu z ContentPanelu provedl ContentPanel hromadně, tady si jen označíme že už NEJSME jsme hostování:
            IsHosted = false;

            WF.Control control = __Control;
            if (control == null || control.IsDisposed || control.Disposing) return;

            // Co budeme dělat:
            bool removeHandle = isFinal || (refreshInfo?.ModeIsRemoveReleaseHandle ?? false);
            bool disposeControl = isFinal || (refreshInfo?.ModeIsRemoveDispose ?? false);
            bool updateInfo = (refreshInfo != null);
            bool controlIsInternal = !__ControlIsExternal;

            if (removeHandle)
            {
                if (control.IsHandleCreated && !control.RecreatingHandle)
                {
                    DestroyWindow(control.Handle);
                    if (updateInfo) refreshInfo.DestroyedCount++;
                }
            }

            if (disposeControl)
            {   // V režimu RemoveDispose zlikvidujeme náš control, a při požadavku IsFinal taky (ale tam neprovedeme jeho Dispose, protože nejsme jeho autorem):
                if (controlIsInternal || isFinal)
                {   // Interní control anebo finální zahození this instance:
                    //  => Odvážeme eventy a kompletně zapomenu na control
                    //    (pokud by to byl externí control a nejde o finální zahození, tak si externí control stále necháváme v paměti včetně eventů,
                    //     ty se do externího controlu navázaly v konstruktoru)
                    UnRegisterControlEvents(control);
                    __Control = null;
                }
                if (controlIsInternal)
                {   // Interní control (ten jsme vytvořili my) budeme plně Disposovat:
                    try { control.Dispose(); }
                    catch { }
                    if (updateInfo) refreshInfo.DisposedCount++;
                }
            }
        }
        [DllImport("User32")]
        private static extern int DestroyWindow(IntPtr hWnd);
        #endregion
        #region Události controlu
        /// <summary>
        /// Naváže zdejší eventhandlery k danému controlu
        /// </summary>
        /// <param name="control"></param>
        private void RegisterControlEvents(WF.Control control)
        {
            if (__IsFocusableControl)
                control.GotFocus += Control_GotFocus;
        }
        /// <summary>
        /// Odváže zdejší eventhandlery k danému controlu
        /// </summary>
        /// <param name="control"></param>
        private void UnRegisterControlEvents(WF.Control control)
        {
            if (__IsFocusableControl)
                control.GotFocus -= Control_GotFocus;
        }

        private void Control_GotFocus(object sender, EventArgs e)
        {
            this.IScrollPanel.ActiveItem = this;
        }
        /// <summary>
        /// Vrací true
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal bool ContainsItem(IDataFormItem item)
        {
            return (item != null && Object.ReferenceEquals(this.__DataFormItem, item));
        }
        #endregion
    }
    #endregion
    #region class DataFormItem : Deklarace každého jednoho prvku v rámci DataFormu, implementace IDataFormItem
    /// <summary>
    /// Deklarace každého jednoho prvku v rámci DataFormu, implementace <see cref="IDataFormItem"/>
    /// </summary>
    public class DataFormItem : IDataFormItem
    {
        public string ItemName { get; set; }
        public int? TabIndex { get; set; }
        public string PageName { get; set; }
        public string PageText { get; set; }
        public string PageToolTipText { get; set; }
        public string PageIconName { get; set; }
        public DataFormItemType ItemType { get; set; }
        public Rectangle Bounds { get; set; }
        public string Text { get; set; }
        public bool? Visible { get; set; }
        public DevExpress.XtraEditors.Controls.BorderStyles? BorderStyle { get; set; }
        public LabelStyleType? LabelStyle { get; set; }
        public DevExpress.Utils.WordWrap? LabelWordWrap { get; set; }
        public DevExpress.XtraEditors.LabelAutoSizeMode? LabelAutoSize { get; set; }
        public DevExpress.Utils.HorzAlignment? LabelHAlignment { get; set; }
        public DevExpress.XtraEditors.Mask.MaskType? TextMaskType { get; set; }
        public string TextEditMask { get; set; }
        public bool? TextUseMaskAsDisplayFormat { get; set; }
        public DevExpress.XtraEditors.Controls.CheckBoxStyle? CheckBoxStyle { get; set; }
        public decimal? SpinMinValue { get; set; }
        public decimal? SpinMaxValue { get; set; }
        public decimal? SpinIncrement { get; set; }
        public DevExpress.XtraEditors.Controls.SpinStyles? SpinStyle { get; set; }
        public string ToolTipTitle { get; set; }
        public string ToolTipText { get; set; }
        public bool? Enabled { get; set; }
        public bool? ReadOnly { get; set; }
        public bool? TabStop { get; set; }


    }
    #endregion
    #region interface IDataFormItem, enums DataFormItemType, DxDataFormMemoryMode
    public interface IDataFormItem
    {
        string ItemName { get; }
        int? TabIndex { get; }
        string PageName { get; }
        string PageText { get; }
        string PageToolTipText { get; }
        string PageIconName { get; }
        DataFormItemType ItemType { get; }
        Rectangle Bounds { get; }
        string Text { get; }
        bool? Visible { get; }
        DevExpress.XtraEditors.Controls.BorderStyles? BorderStyle { get; }
        LabelStyleType? LabelStyle { get; }
        DevExpress.Utils.WordWrap? LabelWordWrap { get; }
        DevExpress.XtraEditors.LabelAutoSizeMode? LabelAutoSize { get; }
        DevExpress.Utils.HorzAlignment? LabelHAlignment { get; }
        DevExpress.XtraEditors.Mask.MaskType? TextMaskType { get; } 
        string TextEditMask { get; } 
        bool? TextUseMaskAsDisplayFormat { get; }
        DevExpress.XtraEditors.Controls.CheckBoxStyle? CheckBoxStyle { get; }
        decimal? SpinMinValue { get; }
        decimal? SpinMaxValue { get; }
        decimal? SpinIncrement { get; }
        DevExpress.XtraEditors.Controls.SpinStyles? SpinStyle { get; }
        string ToolTipTitle { get; } 
        string ToolTipText { get; }
        bool? Enabled { get; }
        bool? ReadOnly { get; } 
        bool? TabStop { get; }
    }

    /// <summary>
    /// Druh prvku v DataFormu
    /// </summary>
    public enum DataFormItemType
    {
        None = 0,
        Label,
        TextBox,
        EditBox,
        SpinnerBox,
        CheckBox,
        BreadCrumb,
        ComboBoxList,
        ComboBoxEdit,
        ListView,
        TreeView,
        RadioButtonBox,
        Button,
        DropDownButton,
        Image
    }
    /// <summary>
    /// Režim práce při zobrazování controlů v <see cref="DxDataForm"/>
    /// </summary>
    [Flags]
    public enum DxDataFormMemoryMode
    {
        /// <summary>
        /// Controly vůbec nezobrazovat = pouze pro testy paměti
        /// </summary>
        None = 0,
        /// <summary>
        /// Do parent containeru (Host) vkládat pouze controly ve viditelné oblasti, a po opuštění viditelné oblasti zase Controly z parenta odebírat
        /// </summary>
        HostOnlyVisible = 0x01,
        /// <summary>
        /// Do parent containeru vkládat vždy všechny controly a nechat je tam stále
        /// </summary>
        HostAllways = 0x02,
        /// <summary>
        /// Po odebrání controlu z parent containeru (Host) uvolnit handle pomocí User32.DestroyWindow()
        /// </summary>
        RemoveReleaseHandle = 0x10,
        /// <summary>
        /// Po odebrání controlu z parent containeru (Host) uvolnit handle pomocí User32.DestroyWindow() a samotný Control disposovat (v případě znovu potřeby bude vygenerován nový podle předpisu)
        /// </summary>
        RemoveDispose = 0x20,
        /// <summary>
        /// Optimální default pro runtime
        /// </summary>
        Default = HostOnlyVisible | RemoveReleaseHandle,
        /// <summary>
        /// Optimální default pro runtime
        /// </summary>
        Default2 = HostOnlyVisible | RemoveReleaseHandle | RemoveDispose
    }
    #endregion



    #region Pouze pro testování, později smazat
    partial class DxDataForm
    {
        #region Tvorba testovacích dat : CreateSamples()
        /// <summary>
        /// Vytvoří a vrátí pole prků pro určitý vzorek (ukázku)
        /// </summary>
        /// <param name="sampleId"></param>
        /// <returns></returns>
        public static IEnumerable<IDataFormItem> CreateSample(int sampleId)
        {
            switch (sampleId)
            {
                case 1: return _CreateSample1();
                case 2: return _CreateSample2();
                case 3: return _CreateSample3();
                case 4: return _CreateSample4();
                case 5: return _CreateSample5();
                case 6: return _CreateSample6();

            }
            return null;
        }
        private static IEnumerable<IDataFormItem> _CreateSample1()
        {
            int x1, y1, x2, y2;
            List<DataFormItem> items = new List<DataFormItem>();

            // Stránka 0
            x1 = 6;
            y1 = 8;
            x2 = 700;
            y2 = 8;

            int h0 = 20;
            int h1 = 30;
            int h2 = 20;

            _CreateSampleAddRelation1(items, "Reference:", x1, y1); y1 += h1;
            _CreateSampleAddRelation1(items, "Dodavatel:", x1, y1); y1 += h1;
            _CreateSampleAddRelation1(items, "Náš provoz:", x1, y1); y1 += h1;
            _CreateSampleAddRelation1(items, "Útvar:", x1, y1); y1 += h1;
            _CreateSampleAddRelation1(items, "Sklad:", x1, y1); y1 += h1;
            _CreateSampleAddRelation1(items, "Odpovědná osoba:", x1, y1); y1 += h1;
            _CreateSampleAddRelation1(items, "Expediční sklad:", x1, y1); y1 += h1;
            _CreateSampleAddRelation1(items, "Odběratel:", x1, y1); y1 += h1;

            y1 += h2;

            _CreateSampleAddAttributeMemo1(items, "Poznámka nákupní:", x2, y2, 180, 550, 7 * h1 + h0); y2 += 8 * h1;

            _CreateSampleAddAttribute3Prices(items, "Cena nákupní:", x1, y1); y1 += h1;
            _CreateSampleAddAttribute3Prices(items, "Cena DPH 0:", x1, y1); y1 += h1;
            _CreateSampleAddAttribute3Prices(items, "Cena DPH 1:", x1, y1); y1 += h1;
            _CreateSampleAddAttribute3Prices(items, "Cena DPH 2:", x1, y1); y1 += h1;
            _CreateSampleAddAttribute3Prices(items, "Cena evidenční:", x1, y1); y1 += h1;

            y1 += h2;

            _CreateSampleAddAttributeMemo1(items, "Poznámka cenová:", x2, y2, 180, 550, 4 * h1 + h0); y2 += 5 * h1;

            _CreateSampleAddLabel(items, "Rabaty:", x1, y1);
            _CreateSampleAddCheckBox(items, "Aplikovat rabat dodavatele", x1, y1, 350, 186, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1); y1 += h1;
            _CreateSampleAddCheckBox(items, "Aplikovat rabat skladu", x1, y1, 350, 186, DevExpress.XtraEditors.Controls.CheckBoxStyle.Default); y1 += h1;
            _CreateSampleAddCheckBox(items, "Aplikovat rabat odběratele", x1, y1, 350, 186, null); y1 += h1;
            _CreateSampleAddCheckBox(items, "Aplikovat rabat uživatele", x1, y1, 350, 186, null); y1 += h1;
            _CreateSampleAddCheckBox(items, "Aplikovat rabat termínový", x1, y1, 350, 186, null); y1 += h1;

            y1 += h2;

            _CreateSampleAddAttributeMemo1(items, "Poznámka k rabatům:", x2, y2, 180, 550, 4 * h1 + h0); y2 += 5 * h1;

            _CreateSampleAddDate2(items, "Datum objednávky", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum potvrzení", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum expedice", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum příjmu na sklad", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum přejímky kvality", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum zaúčtování", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum splatnosti", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum úhrady", x1, y1); y1 += h1;

            y1 += h2;

            _CreateSampleAddAttributeMemo1(items, "Předvolby:", x2, y2, 180, 550, 120); y2 += 135;
            _CreateSampleAddCheckBox3(items, "Tuzemský dodavatel", "Akciovka", "S.r.o.", x1, y1); y1 += h1;
            _CreateSampleAddCheckBox3(items, "Dodavatel v EU", "Majitel v EU", "Daně z příjmu v EU", x1, y1); y1 += h1;
            _CreateSampleAddCheckBox3(items, "Dodavatel v US", "Majitel v US", "Daně z příjmu v US", x1, y1); y1 += h1;
            _CreateSampleAddCheckBox3(items, "Nespolehlivý dodavatel", "Důvod: peníze", "Důvod: kriminální", x1, y1); y1 += h1;
            _CreateSampleAddCheckBox3(items, "Tuzemský odběratel", "akciovka", "sro", x1, y1); y1 += h1;
            _CreateSampleAddCheckBox3(items, "Koncový zákazník", "sro", "fyzická osoba", x1, y1); y1 += h1;
            _CreateSampleAddCheckBox3(items, "Hlídané zboží", "Spotřební daň", "Bezpečnostní problémy", x1, y1); y1 += h1;
            _CreateSampleAddCheckBox3(items, "Hlídaná platba", "Nespolehlivý plátce", "Nestandardní banka", x1, y1); y1 += h1;
            _CreateSampleAddCheckBox3(items, "Nadměrný objem", "hmotnostní", "finanční", x1, y1); y1 += h1;

            y1 += h2;

            _CreateSampleSetPage(items, "page0", "ZÁKLADNÍ ÚDAJE", "Tato záložka obsahuje základní údaje o dokladu", null);

            // Stránka 1
            x1 = 6;
            y1 = 8;
            x2 = 700;
            y2 = 8;

            _CreateSampleAddAttribute3Prices(items, "Cena nákupní €:", x1, y1); y1 += h1;
            _CreateSampleAddAttribute3Prices(items, "Cena DPH 0 €:", x1, y1); y1 += h1;
            _CreateSampleAddAttribute3Prices(items, "Cena DPH 1 €:", x1, y1); y1 += h1;
            _CreateSampleAddAttribute3Prices(items, "Cena DPH 2 €:", x1, y1); y1 += h1;
            _CreateSampleAddAttribute3Prices(items, "Cena evidenční €:", x1, y1); y1 += h1;

            _CreateSampleAddAttributeMemo1(items, "Poznámka k cizí měně:", x1, y1, 180, 550, 4 * h1 + h0); y1 += 5 * h1;

            _CreateSampleAddAttributeMemo1(items, "Poznámka k účtování:", x1, y1, 180, 550, 4 * h1 + h0); y1 += 5 * h1;

            y1 += h2;

            _CreateSampleAddLabel(items, "Účtování:", x1, y1);
            _CreateSampleAddCheckBox(items, "Účtovat do běžného deníku", x1, y1, 350, 186, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1); y1 += h1;
            _CreateSampleAddCheckBox(items, "Účtovat do reálného deníku", x1, y1, 350, 186, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1); y1 += h1;
            _CreateSampleAddCheckBox(items, "Účtovat jako rozpočtová organizace", x1, y1, 350, 186, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1); y1 += h1;
            _CreateSampleAddCheckBox(items, "Účtovat až po schválení majitelem", x1, y1, 350, 186, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1); y1 += h1;
            _CreateSampleAddCheckBox(items, "Účtovat do černého účetního rozvrhu", x1, y1, 350, 186, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1); y1 += h1;
            _CreateSampleAddCheckBox(items, "Účtovat až po zaplacení", x1, y1, 350, 186, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1); y1 += h1;
            _CreateSampleAddCheckBox(items, "Účtovat jen 30. února", x1, y1, 350, 186, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1); y1 += h1;

            _CreateSampleSetPage(items, "page1", "CENOVÉ ÚDAJE v €", "Tato záložka obsahuje údaje o cenách v €urech", null);

            // Stránka 2
            x1 = 6;
            y1 = 8;
            x2 = 700;
            y2 = 8;

            _CreateSampleAddRelation1(items, "Zapsal:", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum zadání do systému", x1, y1); y1 += h1;

            _CreateSampleSetPage(items, "page2", "SYSTÉMOVÉ ÚDAJE", "Tato záložka obsahuje údaje o osobě a času zadání do systému", null);

            return items;
        }
        private static IEnumerable<IDataFormItem> _CreateSample2()
        {
            List<DataFormItem> items = new List<DataFormItem>();
            Random rand = _SampleRandom;

            int x, y, rows;
            int[] widths;

            x = 6;
            y = 6;
            rows = 400;
            widths = new int[] { 50, 75, 150, 100, 250, 60, 60, 60, 20, 40 };
            for (int r = 1; r <= rows; r++)
                _CreateSampleAddSampleRow(items, "Ukázkový řádek " + r.ToString(), widths, ref x, ref y);

            _CreateSampleSetPage(items, "page0", "400 řádků x 10 textů", "Položky 1", null);


            x = 6;
            y = 6;
            rows = 300;
            widths = new int[] { 50, 50, 150, 150, 20, 40, 50, 50, 150, 150, 20, 40, 50, 50, 150, 150, 20, 40 };
            for (int r = 1; r <= rows; r++)
                _CreateSampleAddSampleRow(items, "Ukázkový řádek " + r.ToString(), widths, ref x, ref y);

            _CreateSampleSetPage(items, "page1", "300 řádků x 18 textů", "Položky 2", null);


            x = 6;
            y = 6;
            rows = 20;
            widths = new int[] { 150, 100, 75, 50, 40, 30, 20, 150, 100, 75, 50, 40, 30, 20, };
            for (int r = 1; r <= rows; r++)
                _CreateSampleAddSampleRow(items, "Ukázkový řádek " + r.ToString(), widths, ref x, ref y);

            _CreateSampleSetPage(items, "page2", "20 řádků x 14 textů", "Položky 3", null);


            x = 6;
            y = 6;
            rows = 7;
            widths = new int[] { 380, 230, 140, 90 };
            for (int r = 1; r <= rows; r++)
                _CreateSampleAddSampleRow(items, "Ukázkový řádek " + r.ToString(), widths, ref x, ref y);

            _CreateSampleSetPage(items, "page3", "7 řádků x 4 texty", "Položky 4", null);


            x = 6;
            y = 6;
            rows = 5;
            widths = new int[] { 80, 350, 80, 30, 50 };
            for (int r = 1; r <= rows; r++)
                _CreateSampleAddSampleRow(items, "Ukázkový řádek " + r.ToString(), widths, ref x, ref y);

            _CreateSampleSetPage(items, "page4", "5 řádků x 5 textů", "Položky 5", null);

            return items;
        }
        private static IEnumerable<IDataFormItem> _CreateSample3()
        {
            Random rand = _SampleRandom;

            int x1, y1, x2, y2;
            List<DataFormItem> items = new List<DataFormItem>();

            // Stránka 0
            x1 = 6;
            y1 = 8;
            x2 = 700;
            y2 = 8;

            int ha = 44;
            int hl = 18;
            int hs = 7;

            _CreateSampleAddRelation2(items, "Dílec VTPV:", x1, y1, 166, 317); y1 += ha;
            _CreateSampleAddRelation2(items, "Výrobní příkaz:", x1, y1, 166, 317); y1 += ha;

            _CreateSampleAddAttributeText2(items, "Datum kalkulace:", x1, y1, 166);
            _CreateSampleAddAttributeText2(items, "Datum dokumentace:", x1 + 168, y1, 166); y1 += ha;

            _CreateSampleAddAttributeText2(items, "Datum platnosti od:", x1, y1, 166);
            _CreateSampleAddAttributeText2(items, "Datum revize:", x1 + 168, y1, 166); y1 += ha;

            _CreateSampleAddAttributeText2(items, "Typ kalkulace:", x1, y1, 166);
            _CreateSampleAddAttributeCheck2(items, "Standardní kalkulace:", "", x1 + 166 + 2, y1, 129);
            _CreateSampleAddAttributeCheck2(items, "Kalkulovány i nadnorm. náklady:", "", x1 + 166 + 2 + 129 + 2, y1, 187); y1 += ha;

            _CreateSampleAddAttributeMemo2(items, "Poznámka:", x1, y1, 485, 82); y1 += (hl + 82 + hs);

            _CreateSample3Naklady(items, "Na MJ", x1, ref y1);
            _CreateSample3Naklady(items, "Na kalkulované množství", x1, ref y1);

            y1 += 16;
            _CreateSampleAddLabel(items, "Doplňující údaje", x1, y1, 300, DevExpress.Utils.HorzAlignment.Near, LabelStyleType.SubTitle); y1 += 32;

            _CreateSampleAddRelation2(items, "Parametry kalkulací:", x1, y1, 166, 317); y1 += ha;
            _CreateSampleAddRelation2(items, "Protokol:", x1, y1, 166, 317); y1 += ha;
            _CreateSampleAddRelation2(items, "T modifikace VTPV:", x1, y1, 166, 317); y1 += ha;
            _CreateSampleAddRelation2(items, "T modifikace STPV:", x1, y1, 166, 317); y1 += ha;
            _CreateSampleAddRelation2(items, "Plán. kalkulace:", x1, y1, 166, 317); y1 += ha;
            _CreateSampleAddRelation2(items, "Pořídil:", x1, y1, 166, 317); y1 += ha;

            _CreateSampleSetPage(items, "page0", "Kalkulace", "Tato záložka obsahuje údaje o kalkulaci obecně", null);

            // page1:
            _CreateSample3CenovyVektor(items, "page1", "Náklady za kalkulované množství");

            // page2:
            _CreateSample3CenovyVektor(items, "page2", "Jednotkové náklady");

            // page3:
            x1 = 6;
            y1 = 8;

            _CreateSampleAddAttributeText2(items, "Cena měna 0:", x1, y1, 198); y1 += ha;
            _CreateSampleAddAttributeText2(items, "Cena měna 1:", x1, y1, 198); y1 += ha;
            _CreateSampleAddAttributeText2(items, "Cena měna 2:", x1, y1, 198); y1 += ha;

            _CreateSampleSetPage(items, "page3", "UDA", "Uživatelsky definované atributy", null);


            return items;
        }
        private static IEnumerable<IDataFormItem> _CreateSample4()
        {
            Random rand = _SampleRandom;

            int x1, y1, x2, y2;
            List<DataFormItem> items = new List<DataFormItem>();

            // Stránka 0
            x1 = 6;
            y1 = 8;
            x2 = 700;
            y2 = 8;

            int hl = 18;
            int hs = 2;
            int ha = 20;
            int has = ha + hs;
            int es = 18;

            // page0:

            _CreateSampleAddRelation1(items, "Reference:", x1, y1, 190, 177, 267); y1 += has;
            _CreateSampleAddRelation1(items, "Rozměry ukazatele:", x1, y1, 190, 177, 267); y1 += has;
            _CreateSampleAddRelation1(items, "Master ukazatel:", x1, y1, 190, 177, 267); y1 += has;
            _CreateSampleAddRelation1(items, "Skupiny ukazatelů:", x1, y1, 190, 177, 267); y1 += has;
            _CreateSampleAddAttributeText1(items, "Jméno OLAP kostky:", x1, y1, 190, 446); y1 += has;

            y1 += es;

            x2 = 341;
            _CreateSampleAddCheckBox(items, "Zákaz načítání hodnot ukazatele:", x1, y1, 209, style: DevExpress.XtraEditors.Controls.CheckBoxStyle.Default, labelHalignment: DevExpress.Utils.HorzAlignment.Far);
            _CreateSampleAddAttributeText1(items, "Ukazatel obsahuje:", x2, y1, 150, 151); y1 += has;
            _CreateSampleAddAttributeText1(items, "Typ měrné jednotky:", x1, y1, 190, 132);
            _CreateSampleAddAttributeText1(items, "Kód měny:", x2, y1, 150, 151); y1 += has;
            _CreateSampleAddAttributeText1(items, "Měrná jednotka:", x1, y1, 190, 132); 
            y1 += has;
            _CreateSampleAddAttributeText1(items, "Zaokrouhlení částek:", x1, y1, 190, 132);
            _CreateSampleAddAttributeText1(items, "Zaokrouhlení směr:", x2, y1, 150, 151); y1 += has;
            _CreateSampleAddAttributeMemo1(items, "Poznámka:", x1, y1, 190, 446, 132); y1 += has;

            _CreateSampleSetPage(items, "page0", "Společné", "Společné údaje", null);

            // page1:
            x1 = 6;
            y1 = 8;
         
            _CreateSample4Head(items, null, x1, ref y1);
            _CreateSample4DimensionDate(items, "Rozměr - Datum", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr - Organizace", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr - Zakázka", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr - Útvar", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr 4", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr 5", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr 6", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr 7", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr 8", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr 9", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr 10", x1, ref y1);

            _CreateSampleSetPage(items, "page1", "Realita", "Realita", null);

            // page2:
            x1 = 6;
            y1 = 8;

            _CreateSample4Head(items, "Zámek typu plánu", x1, ref y1);
            _CreateSample4DimensionDate(items, "Rozměr - Datum", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr - Organizace", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr - Zakázka", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr - Útvar", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr 4", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr 5", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr 6", x1, ref y1);
            _CreateSample4DimensionOther(items, "Rozměr 7", x1, ref y1);

            _CreateSampleSetPage(items, "page2", "Plán", "Plán", null);


            return items;
        }
        private static IEnumerable<IDataFormItem> _CreateSample5()
        {
            List<IDataFormItem> items = new List<IDataFormItem>();
            Random rand = _SampleRandom;


            return items;
        }
        private static IEnumerable<IDataFormItem> _CreateSample6()
        {
            List<IDataFormItem> items = new List<IDataFormItem>();
            Random rand = _SampleRandom;


            return items;
        }

        // Specifické skupiny pro samply:
        private static void _CreateSample3Naklady(List<DataFormItem> items, string label, int x, ref int y)
        {
            int ha = 44;

            y += 16;

            _CreateSampleAddLabel(items, label, x, y, 300, DevExpress.Utils.HorzAlignment.Near, LabelStyleType.SubTitle); y += 32;

            _CreateSampleAddAttributeText2(items, "Kalkulované množství:", x, y, 166); y += ha;

            _CreateSampleAddAttributeText2(items, "Vlastní náklady na kalkulova", x, y, 166);
            _CreateSampleAddAttributeText2(items, "Úplné vlastní náklady", x + 2 + 166, y, 166);
            _CreateSampleAddAttributeText2(items, "Celkem", x + 2 + 166 + 2 + 166, y, 166); y += ha;

            _CreateSampleAddAttributeText2(items, "Skladová cena:", x, y, 166); y += ha;
        }
        private static void _CreateSample3CenovyVektor(List<DataFormItem> items, string pageName, string pageText)
        {
            int x = 6;
            int y = 6;
            int w0 = 222;
            int w1 = 151;
            _CreateSampleAddLabel(items, "Reference složky", x + 2, y, w0 - 4, DevExpress.Utils.HorzAlignment.Near);
            _CreateSampleAddLabel(items, "Bez nižších dílců", x + w0 + 2, y, w1 - 4, DevExpress.Utils.HorzAlignment.Near);
            _CreateSampleAddLabel(items, "Nižší dílce", x + w0 + 2 + w1 + 2, y, w1 - 4, DevExpress.Utils.HorzAlignment.Near);
            _CreateSampleAddLabel(items, "Celkem", x + w0 + 2 + w1 + 2 + w1 + 2, y, w1 - 4, DevExpress.Utils.HorzAlignment.Near); y += 20;

            for (int c = 0; c < 13; c++)
            {
                if (c < 12)
                    _CreateSampleAddText(items, x, y, w0);
                else
                    _CreateSampleAddLabel(items, "Celkem", x + 2, y, w0 - 4, DevExpress.Utils.HorzAlignment.Near);
                _CreateSampleAddText(items, x + w0 + 2, y, w1);
                _CreateSampleAddText(items, x + w0 + 2 + w1 + 2, y, w1);
                _CreateSampleAddText(items, x + w0 + 2 + w1 + 2 + w1 + 2, y, w1);
                y += 21;
            }

            _CreateSampleSetPage(items, pageName, pageText, "Tato záložka obsahuje údaje o cenovém vektoru", null);
        }
        private static void _CreateSample4Head(List<DataFormItem> items, string lockLabel, int x, ref int y)
        {
            int x2 = x + 118 + 2 + 222 + 2;
            int hl = 18;
            int hs = 2;
            int ha = 20;
            int has = ha + hs;
            int es = 18;

            _CreateSampleAddAttributeText1(items, "Naposledy načteno:", x, y, 118, 108);
            _CreateSampleAddAttributeText1(items, "Zámek hodnot:", x2, y, 93, 73); y += has;

            if (!String.IsNullOrEmpty(lockLabel))
            {
                _CreateSampleAddRelation1(items, lockLabel, x, y, 118, 222, 283); 
                y += has;
            }

            _CreateSampleAddAttributeText1(items, "Třída:", x, y, 118, 222);
            _CreateSampleAddAttributeText1(items, "Pořadač:", x2, y, 93, 188); y += has;

            _CreateSampleAddAttributeText1(items, "Atribut:", x, y, 118, 222);
            _CreateSampleAddAttributeText1(items, "Typ:", x2, y, 93, 188); y += has;

            _CreateSampleAddAttributeText1(items, "Funkce ukazatele:", x, y, 118, 222); y += has;

            _CreateSampleAddAttributeText1(items, "Uložený filtr:", x, y, 118, 222);
            _CreateSampleAddButton(items, "Editace", x2, y, 76, 20);
            y += has;

            _CreateSampleAddCheckBox(items, "Automatické generování procedury:", x, y, 240, style: DevExpress.XtraEditors.Controls.CheckBoxStyle.Default, labelHalignment: DevExpress.Utils.HorzAlignment.Far);
            _CreateSampleAddButton(items, "Editace procedury", x + 265, y, 155, 20);
            y += has;
        }
        private static void _CreateSample4DimensionDate(List<DataFormItem> items, string label, int x, ref int y)
        {
            int x2 = x + 118 + 2 + 222 + 2;
            int hl = 18;
            int hs = 2;
            int ha = 20;
            int has = ha + hs;
            int es = 18;

            y += 16;
            _CreateSampleAddLabel(items, label, x, y, 300, DevExpress.Utils.HorzAlignment.Near, LabelStyleType.SubTitle); y += 32;

            _CreateSampleAddCheckBox(items, "Generovat virtuální časový rozměr:", x, y, 240, style: DevExpress.XtraEditors.Controls.CheckBoxStyle.Default, labelHalignment: DevExpress.Utils.HorzAlignment.Far);
            y += has;

            _CreateSampleAddAttributeText1(items, "Atribut rozměru:", x, y, 118, 222);
            _CreateSampleAddAttributeText1(items, "Od počátku roku:", x2, y, 126, 155); 
            y += has;
        }
        private static void _CreateSample4DimensionOther(List<DataFormItem> items, string label, int x, ref int y)
        {
            int x2 = x + 118 + 2 + 222 + 2;
            int x3 = x + 400;
            int hl = 18;
            int hs = 2;
            int ha = 20;
            int has = ha + hs;
            int es = 18;

            y += 16;
            _CreateSampleAddLabel(items, label, x, y, 300, DevExpress.Utils.HorzAlignment.Near, LabelStyleType.SubTitle);
            _CreateSampleAddAttributeText1(items, "Typ:", x3, y, 70, 155);
            y += 32;

            _CreateSampleAddAttributeText1(items, "Atribut / Vztah rozměru:", x, y, 118 + 40, 280);
            _CreateSampleAddButton(items, "Rozšílený rozměr", x + 472, y, 154, 20);
            y += has;

            _CreateSampleAddAttributeText1(items, "Třída:", x, y, 118, 222);
            _CreateSampleAddAttributeText1(items, "Pořadač:", x2, y, 93, 188);
            y += has;

            _CreateSampleAddAttributeText1(items, "Filtr:", x, y, 118, 222);
            y += has;

            _CreateSampleAddAttributeText1(items, "Způsob zobrazení:", x, y, 118, 222);
            _CreateSampleAddAttributeText1(items, "Výraz:", x2, y, 93, 188);
            y += has;

        }

        // Primární jednotlivé prvky:
        private static void _CreateSampleAddLabel(List<DataFormItem> items, string label, int x, int y, int? w = null, DevExpress.Utils.HorzAlignment? labelHalignment = null, LabelStyleType? labelStyle = null)
        {
            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.Label,
                Bounds = new Rectangle(x, y, (w ?? 180), 20),
                Text = label,
                LabelHAlignment = (labelHalignment ?? DevExpress.Utils.HorzAlignment.Far),
                LabelAutoSize = LabelAutoSizeMode.None,
                LabelStyle = labelStyle
            });
        }
        private static void _CreateSampleAddText(List<DataFormItem> items, int x, int y, int? w = null, DevExpress.XtraEditors.Mask.MaskType? maskType = null, string mask = null,
            string toolTipText = null, string toolTipTitle = null)
        {
            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.TextBox,
                Bounds = new Rectangle(x, y, w ?? 100, 20),
                TextMaskType = maskType,
                TextEditMask = mask,
                ToolTipText = toolTipText,
                ToolTipTitle = toolTipTitle
            });
        }
        private static void _CreateSampleAddMemo(List<DataFormItem> items, int x, int y, int w, int h)
        {
            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.EditBox,
                Bounds = new Rectangle(x, y, w, h),
                ToolTipTitle = "POZNÁMKA",
                ToolTipText = "Zde můžete zadat libovolný text"
            });
        }
        private static void _CreateSampleAddCheckBox(List<DataFormItem> items, string label, int x, int y, int w, int addx = 0, DevExpress.XtraEditors.Controls.CheckBoxStyle? style = null, DevExpress.Utils.HorzAlignment? labelHalignment = null)
        {
            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.CheckBox,
                Bounds = new Rectangle(x + addx, y, w, 20),
                Text = label,
                CheckBoxStyle = (style ?? _SampleCheckBoxStyle()),
                LabelHAlignment = labelHalignment
            });
        }
        private static void _CreateSampleAddButton(List<DataFormItem> items, string label, int x, int y, int w, int h)
        {
            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.Button,
                Bounds = new Rectangle(x, y, w, h),
                Text = label
            });
        }

        // Standardní atributy a vztahy:
        private static void _CreateSampleAddAttributeText1(List<DataFormItem> items, string label, int x, int y, int wl, int wt, string tooltip = null)
        {
            _CreateSampleAddLabel(items, label, x, y + 0, wl, DevExpress.Utils.HorzAlignment.Far);
            _CreateSampleAddText(items, x + wl + 2, y, wt, toolTipText: tooltip ?? "Zde vyplňte atribut " + label);
        }
        private static void _CreateSampleAddAttributeText2(List<DataFormItem> items, string label, int x, int y, int w, string tooltip = null)
        {
            _CreateSampleAddLabel(items, label, x + 2, y, w - 4, DevExpress.Utils.HorzAlignment.Near);
            _CreateSampleAddText(items, x, y + 20, w, toolTipText: tooltip ?? "Zde vyplňte atribut " + label);
        }
        private static void _CreateSampleAddAttributeMemo1(List<DataFormItem> items, string label, int x, int y, int wl, int wt, int ht, string tooltip = null)
        {
            _CreateSampleAddLabel(items, label, x, y + 0, wl, DevExpress.Utils.HorzAlignment.Far);
            _CreateSampleAddMemo(items, x + wl + 2, y, wt, ht);
        }
        private static void _CreateSampleAddAttributeMemo2(List<DataFormItem> items, string label, int x, int y, int w, int h)
        {
            _CreateSampleAddLabel(items, label, x + 2, y, w - 4, DevExpress.Utils.HorzAlignment.Near);
            _CreateSampleAddMemo(items, x, y + 20, w, h);
        }
        private static void _CreateSampleAddAttributeCheck1(List<DataFormItem> items, string label, string text, int x, int y, int wl, int wc, string tooltip = null)
        {
            _CreateSampleAddLabel(items, label, x, y + 2, wl, DevExpress.Utils.HorzAlignment.Far);
            _CreateSampleAddCheckBox(items, text, x + wl + 2, y, wc, 0, DevExpress.XtraEditors.Controls.CheckBoxStyle.Default);
        }
        private static void _CreateSampleAddAttributeCheck2(List<DataFormItem> items, string label, string text, int x, int y, int w, string tooltip = null)
        {
            _CreateSampleAddLabel(items, label, x + 2, y, w - 4, DevExpress.Utils.HorzAlignment.Near);
            _CreateSampleAddCheckBox(items, text, x, y + 20, w, 0, DevExpress.XtraEditors.Controls.CheckBoxStyle.Default);
        }
        private static void _CreateSampleAddRelation1(List<DataFormItem> items, string label, int x, int y, int wl = 180, int wr = 150, int wn = 250)
        {
            _CreateSampleAddLabel(items, label, x, y + 0, wl, DevExpress.Utils.HorzAlignment.Far);

            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.TextBox,
                Bounds = new Rectangle(x + wl + 2, y, wr, 20)
            });

            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.TextBox,
                Bounds = new Rectangle(x + wl + 2 + wr + 2, y, wn, 20)
            });
        }
        private static void _CreateSampleAddRelation2(List<DataFormItem> items, string label, int x, int y, int wr, int wn, string tooltip = null)
        {
            _CreateSampleAddLabel(items, label, x + 0, y, wr - 4, DevExpress.Utils.HorzAlignment.Near);
            _CreateSampleAddText(items, x, y + 20, wr, toolTipText: tooltip ?? "Zde vyplňte referenci vztahu " + label);
            _CreateSampleAddText(items, x + wr + 2, y + 20, wn, toolTipText: tooltip ?? "Zde vyplňte název vztahu " + label);
        }

        // Testovací sample řádky:
        private static void _CreateSampleAddAttribute3Prices(List<DataFormItem> items, string label, int x, int y)
        {
            _CreateSampleAddLabel(items, label, x, y);

            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.TextBox,
                Bounds = new Rectangle(x + 183, y, 125, 20),
                TextMaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric,
                TextEditMask = "### ### ##0.00"
            });

            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.TextBox,
                Bounds = new Rectangle(x + 311, y, 125, 20),
                TextMaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric,
                TextEditMask = "### ### ##0.00"
            });

            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.TextBox,
                Bounds = new Rectangle(x + 439, y, 125, 20),
                TextMaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric,
                TextEditMask = "### ### ##0.00"
            });
        }
        private static void _CreateSampleAddDate2(List<DataFormItem> items, string label, int x, int y)
        {
            _CreateSampleAddLabel(items, label, x, y);

            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.TextBox,
                Bounds = new Rectangle(x + 183, y, 125, 20),
                TextMaskType = DevExpress.XtraEditors.Mask.MaskType.DateTime,
                TextEditMask = "d",
                ToolTipTitle = label + " - zahájení",
                ToolTipText = "Tento den se událost začala"
            });

            _CreateSampleAddLabel(items, "...", x + 311, y, 30, DevExpress.Utils.HorzAlignment.Center);

            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.TextBox,
                Bounds = new Rectangle(x + 344, y, 125, 20),
                TextMaskType = DevExpress.XtraEditors.Mask.MaskType.DateTime,
                TextEditMask = "d",
                ToolTipTitle = label + " - konec",
                ToolTipText = "Tento den se událost skončila"
            });
        }
        private static void _CreateSampleAddCheckBox3(List<DataFormItem> items, string label1, string label2, string label3, int x, int y)
        {
            DevExpress.XtraEditors.Controls.CheckBoxStyle style = DevExpress.XtraEditors.Controls.CheckBoxStyle.Default;

            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.CheckBox,
                Bounds = new Rectangle(x + 183, y, 250, 20),
                Text = label1,
                CheckBoxStyle = style
            });

            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.CheckBox,
                Bounds = new Rectangle(x + 436, y, 250, 20),
                Text = label2,
                CheckBoxStyle = style
            });

            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.CheckBox,
                Bounds = new Rectangle(x + 689, y, 250, 20),
                Text = label3,
                CheckBoxStyle = style
            });
        }
        private static void _CreateSampleAddSampleRow(List<DataFormItem> items, string label, int[] widths, ref int x, ref int y)
        {
            _CreateSampleAddLabel(items, label, x, y);

            string toolTipText = "Návodný text čili ToolTip k této položce v tuto chvíli nic zajímavého neobsahuje";
            int cx = x + 190;
            int t = 1;
            foreach (int w in widths)
            {
                _CreateSampleAddText(items, cx, y, w, toolTipText: toolTipText, toolTipTitle: "NÁPOVĚDA - " + label + ":" + (t++).ToString());
                cx += w + 3;
            }

            y += 22;
        }

        // Stránky - setování a analýza:
        private static void _CreateSampleSetPage(List<DataFormItem> items, string pageName, string pageText, string pageToolTipText, string pageIconName)
        {
            var pageItems = items.Where(i => i.PageName == null).ToArray();

            string itemsAnalyse = _CreateSampleAnalyse(pageItems);

            foreach (var pageItem in pageItems)
            {
                pageItem.PageName = pageName;
                pageItem.PageText = pageText;
                pageItem.PageToolTipText = (pageToolTipText ?? "") + itemsAnalyse;
                pageItem.PageIconName = pageIconName;
            }
        }
        /// <summary>
        /// Vrátí text, obsahující jednotlivé druhy přítomných prvků a jejich počet v daném poli
        /// </summary>
        /// <param name="pageItems"></param>
        /// <returns></returns>
        private static string _CreateSampleAnalyse(DataFormItem[] pageItems)
        {
            string info = "";
            string eol = "\r\n";
            var itemGroups = pageItems.GroupBy(i => i.ItemType);
            int countItems = 0;
            int countGDI = 0;
            int countUSER = 0;
            foreach (var itemGroup in itemGroups)
            {
                var itemType = itemGroup.Key;
                int countItem = itemGroup.Count();
                countItems += countItem;
                string line = "... Typ prvku: " + itemType.ToString() + ";  Počet prvků: " + countItem.ToString();
                switch (itemType)
                {
                    case DataFormItemType.Label:
                    case DataFormItemType.CheckBox:
                        countUSER += countItem;
                        break;
                    case DataFormItemType.TextBox:
                    case DataFormItemType.EditBox:
                        countGDI += 2 * countItem;
                        countUSER += 2 * countItem;
                        break;
                    default:
                        countGDI += countItem;
                        countUSER += countItem;
                        break;
                }
                info += eol + line;
            }

            string suma = $"CELKEM: {countItems};  GDI Handles: {countGDI};  USER Handles: {countUSER}";
            info += eol + suma;

            return info;
        }
        private static string _SampleItemName(List<DataFormItem> items) { return "item_" + (items.Count + 1000).ToString(); }

        // Tvorba podle předpisu DxDataFormSample:
        public static IEnumerable<IDataFormItem> CreateSample(DxDataFormSample sample)
        {
            List<DataFormItem> items = new List<DataFormItem>();
            Random rand = _SampleRandom;

            int w;
            int x = 6;
            int y = 8;
            for (int i = 0; i < sample.RowsCount; i++)
            {
                x = 6;
                if (sample.LabelCount >= 1)
                {
                    w = rand.Next(100, 200);
                    items.Add(new DataFormItem() { ItemName = _SampleItemName(items), ItemType = DataFormItemType.Label, LabelHAlignment = DevExpress.Utils.HorzAlignment.Far, LabelAutoSize = LabelAutoSizeMode.None, Bounds = new Rectangle(x, y, w, 20), Text = "Řádek " + (i + 1).ToString() + ":" });
                    x += w + 6;
                }
                if (sample.TextCount >= 1)
                {
                    w = rand.Next(180, 350);
                    items.Add(new DataFormItem() { ItemName = _SampleItemName(items), ItemType = DataFormItemType.TextBox, Bounds = new Rectangle(x, y, w, 20) });
                    x += w + 6;
                }
                if (sample.CheckCount >= 1)
                {
                    w = rand.Next(200, 250);
                    var style = _SampleCheckBoxStyle();
                    items.Add(new DataFormItem() { ItemName = _SampleItemName(items), ItemType = DataFormItemType.CheckBox, CheckBoxStyle = style, Bounds = new Rectangle(x, y, w, 20), Text = "Volba " + (i + 1).ToString() + "a. (" + style.ToString() + ")" });
                    x += w + 6;
                }
                if (sample.LabelCount >= 2)
                {
                    w = rand.Next(100, 200);
                    items.Add(new DataFormItem() { ItemName = _SampleItemName(items), ItemType = DataFormItemType.Label, LabelHAlignment = DevExpress.Utils.HorzAlignment.Far, LabelAutoSize = LabelAutoSizeMode.None, Bounds = new Rectangle(x, y, w, 20), Text = "Řádek " + (i + 1).ToString() + ":" });
                    x += w + 6;
                }
                if (sample.TextCount >= 2)
                {
                    w = rand.Next(250, 450);
                    items.Add(new DataFormItem() { ItemName = _SampleItemName(items), ItemType = DataFormItemType.TextBox, Bounds = new Rectangle(x, y, w, 20) });
                    x += w + 6;
                }
                if (sample.CheckCount >= 2)
                {
                    w = rand.Next(100, 200);
                    items.Add(new DataFormItem() { ItemName = _SampleItemName(items), ItemType = DataFormItemType.CheckBox, Bounds = new Rectangle(x, y, w, 20), Text = "Volba " + (i + 1).ToString() + "a." });
                    x += w + 6;
                }
                y += 30;
            }

            return items;
        }
        /// <summary>
        /// Random pro Samples
        /// </summary>
        private static Random _SampleRandom { get { if (__SampleRandom == null) __SampleRandom = new Random(); return __SampleRandom; } }
        private static Random __SampleRandom;
        private static int _SampleWidth(int min, int max) { return _SampleRandom.Next(min, max + 1); }
        /// <summary>
        /// Vrátí náhodný styl checkboxu
        /// </summary>
        /// <returns></returns>
        private static DevExpress.XtraEditors.Controls.CheckBoxStyle _SampleCheckBoxStyle()
        {
            var styles = _SampleCheckBoxStyles;
            return styles[_SampleRandom.Next(styles.Length)];
        }
        /// <summary>
        /// Soupis použitelných CheckBox stylů
        /// </summary>
        private static DevExpress.XtraEditors.Controls.CheckBoxStyle[] _SampleCheckBoxStyles
        {
            get
            {
                DevExpress.XtraEditors.Controls.CheckBoxStyle[] styles = new DevExpress.XtraEditors.Controls.CheckBoxStyle[]
                {
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.Default,
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.Radio,
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.CheckBox,
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgCheckBox1,
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgFlag1,
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgFlag2,
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgHeart1,
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgHeart2,
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgLock1,
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgRadio1,
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgRadio2,
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgStar1,
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgStar2,
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgThumb1,
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1,
                };
                return styles;
            }
        }
        #endregion
    }
    public class WfDataForm : WF.Panel
    {
        public void CreateSample(DxDataFormSample sample)
        {
            this.SuspendLayout();

            _Controls = new List<WF.Control>();
            int x = 6;
            int y = 8;
            for (int i = 0; i < sample.RowsCount; i++)
            {
                x = 6;
                if (sample.LabelCount >= 1)
                {
                    _Controls.Add(new WF.Label() { Bounds = new System.Drawing.Rectangle(x, y, 120, 17), Text = "Řádek " + (i + 1).ToString() + ":" });
                    x += 126;
                }
                if (sample.TextCount >= 1)
                {
                    _Controls.Add(new WF.TextBox() { Bounds = new System.Drawing.Rectangle(x, y - 3, 220, 17) });
                    x += 226;
                }
                if (sample.CheckCount >= 1)
                {
                    _Controls.Add(new WF.CheckBox() { Bounds = new System.Drawing.Rectangle(x, y - 3, 120, 17), Text = "Volba " + (i + 1).ToString() + "a." });
                    x += 126;
                }
                if (sample.LabelCount >= 2)
                {
                    _Controls.Add(new WF.Label() { Bounds = new System.Drawing.Rectangle(x, y, 120, 17), Text = "Řádek " + (i + 1).ToString() + ":" });
                    x += 126;
                }
                if (sample.TextCount >= 2)
                {
                    _Controls.Add(new WF.TextBox() { Bounds = new System.Drawing.Rectangle(x, y - 3, 220, 17) });
                    x += 226;
                }
                if (sample.CheckCount >= 2)
                {
                    _Controls.Add(new WF.CheckBox() { Bounds = new System.Drawing.Rectangle(x, y - 3, 120, 17), Text = "Volba " + (i + 1).ToString() + "b." });
                    x += 126;
                }
                y += 30;
            }

            this.Controls.AddRange(_Controls.ToArray());

            this.ResumeLayout(false);
            this.PerformLayout();
        }
        private void RemoveSampleItems(int percent)
        {
            Random rand = new Random();
            var removeControls = _Controls.Where(c => rand.Next(100) < percent).ToArray();
            foreach (var removeControl in removeControls)
                this.Controls.Remove(removeControl);
        }
        private List<WF.Control> _Controls;
        protected override void Dispose(bool disposing)
        {
            DisposeContent();
            base.Dispose(disposing);
        }
        protected void DisposeContent()
        {
            var controls = this.Controls.OfType<WF.Control>().ToArray();
            foreach (var control in controls)
            {
                if (control != null && !control.IsDisposed && !control.Disposing)
                {
                    this.Controls.Remove(control);
                    control.Dispose();
                }
            }
            _Controls.Clear();
        }
    }
    public class DxDataFormSample
    {
        public DxDataFormSample()
        { }
        public DxDataFormSample(int labelCount, int textCount, int checkCount, int rowsCount, int pagesCount)
        {
            this.LabelCount = labelCount;
            this.TextCount = textCount;
            this.CheckCount = checkCount;
            this.RowsCount = rowsCount;
            this.PagesCount = pagesCount;
        }
        public int LabelCount { get; set; }
        public int TextCount { get; set; }
        public int CheckCount { get; set; }
        public int RowsCount { get; set; }
        public int PagesCount { get; set; }
    }
    #endregion
}
