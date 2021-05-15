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
    public partial class DxDataForm : DxPanelControl
    {
        #region Konstruktor a proměnné, Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxDataForm()
        {
            __Pages = new Dictionary<string, DxDataFormPage>();
            __Items = new Dictionary<string, DxDataFormControlItem>();
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

        /// <summary>
        /// Vrátí true pokud se control (s danými souřadnicemi) má brát jako viditelný v dané oblasti.
        /// Tato metoda může provádět optimalizaci v tom, že jako "viditelné" určí i controly nedaleko od reálně viditelné souřadnice.
        /// </summary>
        /// <param name="controlBounds"></param>
        /// <param name="visibleBounds"></param>
        /// <returns></returns>
        internal bool IsInVisibleBounds(Rectangle? controlBounds, Rectangle visibleBounds)
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
        #region Přidání controlů do logických stránek
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
            DxDataFormControlItem controlItem = page.AddItem(item, skipFinalise);
            __Items.Add(itemKey, controlItem);
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
        #endregion

        private void PrepareTabForPages()
        {
            // var pages = __Pages.Values.Where(p => !p.IsEmpty).ToArray();
            var pages = __Pages.Values.ToArray();                    // Všechny stránky v poli
            int count = pages.Where(p => !p.IsEmpty).Count();        // Počet stránek, které obsahují controly
            if (count > 0)
            {
                var page = pages[0];
                page.PlaceToParent(this);
            }
        }




    }
    /// <summary>
    /// Data jedné stránky (záložky) DataFormu: ID, titulek, ikona, vizuální control <see cref="DxDataFormScrollPanel"/>.
    /// Tento vizuální control může být umístěn přímo v <see cref="DxDataForm"/> (což je vizuální panel),
    /// anebo může být umístěn na záložce.
    /// </summary>
    public class DxDataFormPage : IDisposable
    {
        #region Konstruktor, proměnné, Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataForm"></param>
        public DxDataFormPage(DxDataForm dataForm)
        {
            __DataForm = dataForm;
            __ScrollPanel = new DxDataFormScrollPanel(this);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            this._ClearInstance();
        }
        /// <summary>
        /// Uvolní instance na které drží referenci, neřeší ale jejich Dispose.
        /// </summary>
        private void _ClearInstance()
        {
            __DataForm = null;
            __ScrollPanel?.Dispose();
            __ScrollPanel = null;
        }
        /// <summary>
        /// Odkaz na main instanci DataForm
        /// </summary>
        public DxDataForm DataForm { get { return __DataForm; } }
        private DxDataForm __DataForm;
        /// <summary>
        /// Vizuální prvek <see cref="DxDataFormScrollPanel"/>
        /// </summary>
        public DxDataFormScrollPanel ScrollPanel { get { return __ScrollPanel; } }
        private DxDataFormScrollPanel __ScrollPanel;
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
        /// <summary>
        /// Obsahuje true pokud this page neobsahuje žádný control
        /// </summary>
        public bool IsEmpty { get { return this.ScrollPanel.IsEmpty; } }
        /// <summary>
        /// Do své evidence přidá control pro danou definici <paramref name="item"/>.
        /// Volitelně vynechá finalizaci (refreshe), to je vhodné pokud se z vyšších úrovní volá vícekrát AddItem opakovaně a finalizace se provede na závěr.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="skipFinalise"></param>
        /// <returns></returns>
        internal DxDataFormControlItem AddItem(IDataFormItem item, bool skipFinalise = false)
        {
            return this.ScrollPanel.AddItem(item, skipFinalise);
        }
        /// <summary>
        /// Volá se po dokončení přidávání nebo přemisťování nebo odebírání prvků.
        /// </summary>
        internal void FinaliseContent()
        {
            this.ScrollPanel.FinaliseContent();
        }
        #endregion

        /// <summary>
        /// Umístí svůj vizuální container do daného Parenta.
        /// Před tím prověří, zda v něm již není a pokud tam už je, pak nic nedělá. Lze tedy volat libovolně často.
        /// </summary>
        /// <param name="parent"></param>
        public void PlaceToParent(WF.Control parent)
        {
            this.ScrollPanel.PlaceToParent(parent);
        }
        /// <summary>
        /// Odebere svůj vizuální container z jeho dosavadního Parenta
        /// </summary>
        public void ReleaseFromParent()
        {
            this.ScrollPanel.ReleaseFromParent();
        }
    }
    /// <summary>
    /// Container, který se dokuje do parenta = jeho velikost je omezená, 
    /// a hostuje v sobě <see cref="DxDataFormContentPanel"/>, který má velikost odpovídající svému obsahu a tento Content je pak posouván uvnitř this panelu = Scroll obsahu.
    /// Tento container v sobě obsahuje List <see cref="Items"/> jeho jednotlivých Controlů typu <see cref="DxDataFormControlItem"/>.
    /// </summary>
    public class DxDataFormScrollPanel : DxAutoScrollPanelControl
    {
        #region Konstruktor, proměnné, Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="page"></param>
        public DxDataFormScrollPanel(DxDataFormPage page)
        {
            __Page = page;
            __ContentPanel = new DxDataFormContentPanel(this);
            __Items = new List<DxDataFormControlItem>();
            this.Controls.Add(ContentPanel);
            this.Dock = WF.DockStyle.Fill;
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        }
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
            __Page = null;
            __ContentPanel = null;     // Instance byla Disposována standardně v this.Dispose() =>  this.DisposeContent();, tady jen zahazuji referenci na zombie objekt
            __Items?.Clear();          // Jednotlivé prvky nedisposujeme zde, ale na úrovni DxDataForm, protože tam je vytváříme a společně je tam evidujeme pod klíčem.
            __Items = null;
        }
        /// <summary>
        /// Odkaz na main instanci DataForm
        /// </summary>
        public DxDataForm DataForm { get { return __Page.DataForm; } }
        /// <summary>
        /// Odkaz na stránku, ve které this ScrollPanel bydlí.
        /// Stránka je datový objekt, nikoli nutně vizuální.
        /// Pokud <see cref="DxDataForm"/> obsahuje pouze jednu stránku, pak objekt <see cref="DxDataFormPage"/> nemá vizuální reprezentaci 
        /// a this panel <see cref="DxDataFormScrollPanel"/> je hostován přímo v <see cref="DxDataForm"/>.
        /// Pokud existuje více než jedna stránka, pak existuje více panelů <see cref="DxDataFormScrollPanel"/>, a každý je hostován ve své TabPage,
        /// a jejich TabContainer je hostován v <see cref="DxDataForm"/>.
        /// </summary>
        public DxDataFormPage Page { get { return __Page; } }
        private DxDataFormPage __Page;
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
            RefreshVisibleItems();
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
            RefreshVisibleItems();
        }
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

        private void RefreshVisibleItems()
        {
            var visibleBounds = this.VisibleBounds;
            foreach (var item in Items)
                item.RefreshVisibleItem(visibleBounds);
        }

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

    }
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
        {
            this.__ScrollPanel = scrollPanel;
            this.Dock = WF.DockStyle.None;
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.AutoScroll = false;
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
            __ScrollPanel = null;
        }
        /// <summary>
        /// Main DataForm
        /// </summary>
        public DxDataForm DataForm { get { return __ScrollPanel?.DataForm; } }
        /// <summary>
        /// ScrollPanel, který řídí zobrazení zdejšího panelu
        /// </summary>
        public DxDataFormScrollPanel ScrollPanel { get { return __ScrollPanel; } }
        private DxDataFormScrollPanel __ScrollPanel;
        public Rectangle ContentVisibleBounds { get { return _ContentVisibleBounds; } set { _SetContentVisibleBounds(value); } }
        private Rectangle _ContentVisibleBounds;
        private void _SetContentVisibleBounds(Rectangle contentVisibleBounds)
        {
            if (contentVisibleBounds == _ContentVisibleBounds) return;

            _ContentVisibleBounds = contentVisibleBounds;
        }

    }
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
            if (control != null)
            {
                __ControlIsExternal = true;
                RegisterControlEvents(control);
            }
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
            ReleaseControl(DxDataFormMemoryMode.RemoveReleaseHandle | DxDataFormMemoryMode.RemoveDispose, true);
            __ScrollPanel = null;
            __Control = null;
        }
        /// <summary>
        /// Main DataForm
        /// </summary>
        public DxDataForm DataForm { get { return __ScrollPanel?.DataForm; } }
        /// <summary>
        /// ScrollPanel, který řídí zobrazení našeho <see cref="ContentPanel"/>
        /// </summary>
        public DxDataFormScrollPanel ScrollPanel { get { return __ScrollPanel; } }
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
        public IDataFormItem DataFormItem { get { return __DataFormItem; } }
        private IDataFormItem __DataFormItem;
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
        /// Zajistí, že this prvek bude zobrazen podle toho, zda se nachází v dané viditelné oblasti
        /// </summary>
        /// <param name="visibleBounds"></param>
        internal void RefreshVisibleItem(Rectangle visibleBounds)
        {
            var controlBounds = this.Bounds;
            bool isVisible = DataForm.IsInVisibleBounds(controlBounds, visibleBounds); //.HasValue && IsInVisibleBounds( Rectangle.Intersect(visibleBounds, controlBounds.Value).HasPixels());
            bool isHosted = IsHosted && (__Control != null);

            if (isVisible)
            {
                if (!isHosted)
                {
                    WF.Control control = GetOrCreateControl();
                    ContentPanel.Controls.Add(control);
                    IsHosted = true;
                }
                RefreshItemValues();
            }
            else if (isHosted && !isVisible)
            {
                ReleaseControl(DataForm.MemoryMode);
                IsHosted = false;
            }
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
        /// Najde nebo vytvoří nový vizuální control a vrátí jej
        /// </summary>
        /// <returns></returns>
        private WF.Control GetOrCreateControl()
        {
            WF.Control control = __Control;
            if (control == null || control.IsDisposed)
            {
                __Control = DxComponent.CreateDataFormControl(__DataFormItem);
                // Navázat eventy controlu k nám:
                RegisterControlEvents(__Control);
                control = __Control;
            }
            return control;
        }
        /// <summary>
        /// Aktuální control (pokud existuje) odebere z <see cref="ContentPanel"/>, a pak podle daného režimu jej uvolní z paměti (Handle plus Dispose)
        /// </summary>
        /// <param name="memoryMode"></param>
        /// <param name="isFinal"></param>
        private void ReleaseControl(DxDataFormMemoryMode memoryMode, bool isFinal = false)
        {
            WF.Control control = __Control;
            if (control == null || control.IsDisposed || control.Disposing) return;

            ContentPanel.Controls.Remove(control);

            if (memoryMode.HasFlag(DxDataFormMemoryMode.RemoveReleaseHandle))
            {
                if (control != null && control.IsHandleCreated && !control.RecreatingHandle)
                    DestroyWindow(control.Handle);
            }

            if (!__ControlIsExternal && memoryMode.HasFlag(DxDataFormMemoryMode.RemoveDispose))
            {
                UnRegisterControlEvents(control);
                try { control.Dispose(); }
                catch { }
                __Control = null;
            }
            else if (isFinal)
            {
                UnRegisterControlEvents(control);
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

        }
        /// <summary>
        /// Odváže zdejší eventhandlery k danému controlu
        /// </summary>
        /// <param name="control"></param>
        private void UnRegisterControlEvents(WF.Control control)
        {

        }
        #endregion
    }
    /// <summary>
    /// Deklarace každého jednoho prvku v rámci DataFormu
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
        Default = HostOnlyVisible | RemoveReleaseHandle | RemoveDispose
    }
    #region Pouze pro testování, později smazat
    partial class DxDataForm 
    {   // Rozšíření standardu pro testy
        public void CreateSample(DxDataFormSample sample)
        {
            List<IDataFormItem> items = new List<IDataFormItem>();
            Random rand = new Random();
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

            int cx = 1000;
            int w;
            int x = 6;
            int y = 8;
            for (int i = 0; i < sample.RowsCount; i++)
            {
                x = 6;
                if (sample.LabelCount >= 1)
                {
                    w = rand.Next(100, 200);
                    items.Add(new DataFormItem() { ItemName = "item" + (cx++).ToString(), ItemType = DataFormItemType.Label, LabelHAlignment = DevExpress.Utils.HorzAlignment.Far, LabelAutoSize = LabelAutoSizeMode.None, Bounds = new Rectangle(x, y, w, 20), Text = "Řádek " + (i + 1).ToString() + ":" });
                    x += w + 6;
                }
                if (sample.TextCount >= 1)
                {
                    w = rand.Next(180, 350);
                    items.Add(new DataFormItem() { ItemName = "item" + (cx++).ToString(), ItemType = DataFormItemType.TextBox, Bounds = new Rectangle(x, y, w, 20) });
                    x += w + 6;
                }
                if (sample.CheckCount >= 1)
                {
                    w = rand.Next(200, 250);
                    var style = styles[rand.Next(styles.Length)];
                    items.Add(new DataFormItem() { ItemName = "item" + (cx++).ToString(), ItemType = DataFormItemType.CheckBox, CheckBoxStyle = style, Bounds = new Rectangle(x, y, w, 20), Text = "Volba " + (i + 1).ToString() + "a. (" + style.ToString() + ")"});
                    x += w + 6;
                }
                if (sample.LabelCount >= 2)
                {
                    w = rand.Next(100, 200);
                    items.Add(new DataFormItem() { ItemName = "item" + (cx++).ToString(), ItemType = DataFormItemType.Label, LabelHAlignment = DevExpress.Utils.HorzAlignment.Far, LabelAutoSize = LabelAutoSizeMode.None, Bounds = new Rectangle(x, y, w, 20), Text = "Řádek " + (i + 1).ToString() + ":" });
                    x += w + 6;
                }
                if (sample.TextCount >= 2)
                {
                    w = rand.Next(250, 450);
                    items.Add(new DataFormItem() { ItemName = "item" + (cx++).ToString(), ItemType = DataFormItemType.TextBox, Bounds = new Rectangle(x, y, w, 20) });
                    x += w + 6;
                }
                if (sample.CheckCount >= 2)
                {
                    w = rand.Next(100, 200);
                    items.Add(new DataFormItem() { ItemName = "item" + (cx++).ToString(), ItemType = DataFormItemType.CheckBox, Bounds = new Rectangle(x, y, w, 20), Text = "Volba " + (i + 1).ToString() + "a." });
                    x += w + 6;
                }
                y += 30;
            }

            this.AddItems(items);
        }
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

            if (!sample.NoAddControlsToPanel)
            {
                this.Controls.AddRange(_Controls.ToArray());
                if (sample.Add50ControlsToPanel)
                    RemoveSampleItems(50);
            }

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
        public bool NoAddControlsToPanel { get; set; }
        public bool Add50ControlsToPanel { get; set; }
    }
    #endregion
}
