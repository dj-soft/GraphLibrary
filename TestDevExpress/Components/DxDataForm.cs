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
            var pagesAll = __Pages.Values.ToArray();                    // Všechny stránky v poli (i prázdné)
            var pagesData = pagesAll.Where(p => !p.IsEmpty).ToArray();  // Jen ty stránky, které obsahují controly


            #warning příliš jednoduché, nezvládne změny, funguje jen pro první dávku prvků:


            if (pagesData.Length == 1)
            {
                var pageData = pagesData[0];
                pageData.PlaceToParent(this);
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
            }
        }
        private void _TabPane_PageChangingPrepare(object sender, TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> e)
        {
            _TabPaneChangeStart = DateTime.Now;
            DxDataFormPage page = GetDataFormPage(e.Item);
            if (page != null) page.IsActiveContent = true;
        }

        private void _TabPane_PageChangingRelease(object sender, TEventArgs<DevExpress.XtraBars.Navigation.TabNavigationPage> e)
        {
            DxDataFormPage page = GetDataFormPage(e.Item);
            if (page != null) page.IsActiveContent = false;
            if (_TabPaneChangeStart.HasValue) RunTabChangeDone(DateTime.Now - _TabPaneChangeStart.Value);
        }
        private DateTime? _TabPaneChangeStart;
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
        private void RunTabChangeDone(TimeSpan time)
        {
            TEventArgs<TimeSpan> args = new TEventArgs<TimeSpan>(time);
            OnTabChangeDone(args);
            TabChangeDone?.Invoke(this, args);
        }
        /// <summary>
        /// Akce po dokončení změny stránky, vhodné i pro časomíru a refresh zdrojů
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnTabChangeDone(TEventArgs<TimeSpan> args) { }
        /// <summary>
        /// Akce po dokončení změny stránky, vhodné i pro časomíru a refresh zdrojů
        /// </summary>
        public event EventHandler<TEventArgs<TimeSpan>> TabChangeDone;
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


        #region Tvorba testovacích dat : CreateSamples()
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

            _CreateSampleAddReferName1(items, "Reference:", x1, y1); y1 += h1;
            _CreateSampleAddReferName1(items, "Dodavatel:", x1, y1); y1 += h1;
            _CreateSampleAddReferName1(items, "Náš provoz:", x1, y1); y1 += h1;
            _CreateSampleAddReferName1(items, "Útvar:", x1, y1); y1 += h1;
            _CreateSampleAddReferName1(items, "Sklad:", x1, y1); y1 += h1;
            _CreateSampleAddReferName1(items, "Odpovědná osoba:", x1, y1); y1 += h1;
            _CreateSampleAddReferName1(items, "Expediční sklad:", x1, y1); y1 += h1;
            _CreateSampleAddReferName1(items, "Odběratel:", x1, y1); y1 += h1;

            y1 += h2;

            _CreateSampleAddMemo(items, "Poznámka nákupní:", x2, y2, 550, 7 * h1 + h0); y2 += 8 * h1;

            _CreateSampleAddPrice3(items, "Cena nákupní:", x1, y1); y1 += h1;
            _CreateSampleAddPrice3(items, "Cena DPH 0:", x1, y1); y1 += h1;
            _CreateSampleAddPrice3(items, "Cena DPH 1:", x1, y1); y1 += h1;
            _CreateSampleAddPrice3(items, "Cena DPH 2:", x1, y1); y1 += h1;
            _CreateSampleAddPrice3(items, "Cena evidenční:", x1, y1); y1 += h1;

            y1 += h2;

            _CreateSampleAddMemo(items, "Poznámka cenová:", x2, y2, 550, 4 * h1 + h0); y2 += 5 * h1;

            _CreateSampleAddLabel1(items, "Rabaty:", x1, y1);
            _CreateSampleAddCheckBox1(items, "Aplikovat rabat dodavatele", DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, x1, y1); y1 += h1;
            _CreateSampleAddCheckBox1(items, "Aplikovat rabat skladu", DevExpress.XtraEditors.Controls.CheckBoxStyle.Default, x1, y1); y1 += h1;
            _CreateSampleAddCheckBox1(items, "Aplikovat rabat odběratele", null, x1, y1); y1 += h1;
            _CreateSampleAddCheckBox1(items, "Aplikovat rabat uživatele", null, x1, y1); y1 += h1;
            _CreateSampleAddCheckBox1(items, "Aplikovat rabat termínový", null, x1, y1); y1 += h1;

            y1 += h2;

            _CreateSampleAddMemo(items, "Poznámka k rabatům:", x2, y2, 550, 4 * h1 + h0); y2 += 5 * h1;

            _CreateSampleAddDate2(items, "Datum objednávky", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum potvrzení", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum expedice", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum příjmu na sklad", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum přejímky kvality", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum zaúčtování", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum splatnosti", x1, y1); y1 += h1;
            _CreateSampleAddDate2(items, "Datum úhrady", x1, y1); y1 += h1;

            y1 += h2;

            _CreateSampleAddMemo(items, "Předvolby:", x2, y2, 550, 120); y2 += 135;
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

            _CreateSampleAddPrice3(items, "Cena nákupní €:", x1, y1); y1 += h1;
            _CreateSampleAddPrice3(items, "Cena DPH 0 €:", x1, y1); y1 += h1;
            _CreateSampleAddPrice3(items, "Cena DPH 1 €:", x1, y1); y1 += h1;
            _CreateSampleAddPrice3(items, "Cena DPH 2 €:", x1, y1); y1 += h1;
            _CreateSampleAddPrice3(items, "Cena evidenční €:", x1, y1); y1 += h1;

            _CreateSampleAddMemo(items, "Poznámka k cizí měně:", x1, y1, 550, 4 * h1 + h0); y1 += 5 * h1;

            _CreateSampleAddMemo(items, "Poznámka k účtování:", x1, y1, 550, 4 * h1 + h0); y1 += 5 * h1;

            y1 += h2;

            _CreateSampleAddLabel1(items, "Účtování:", x1, y1);
            _CreateSampleAddCheckBox1(items, "Účtovat do běžného deníku", DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, x1, y1); y1 += h1;
            _CreateSampleAddCheckBox1(items, "Účtovat do reálného deníku", DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, x1, y1); y1 += h1;
            _CreateSampleAddCheckBox1(items, "Účtovat jako rozpočtová organizace", DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, x1, y1); y1 += h1;
            _CreateSampleAddCheckBox1(items, "Účtovat až po schválení majitelem", DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, x1, y1); y1 += h1;
            _CreateSampleAddCheckBox1(items, "Účtovat do černého účetního rozvrhu", DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, x1, y1); y1 += h1;
            _CreateSampleAddCheckBox1(items, "Účtovat až po zaplacení", DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, x1, y1); y1 += h1;
            _CreateSampleAddCheckBox1(items, "Účtovat jen 30. února", DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, x1, y1); y1 += h1;

            _CreateSampleSetPage(items, "page1", "CENOVÉ ÚDAJE v €", "Tato záložka obsahuje údaje o cenách v €urech", null);

            // Stránka 2
            x1 = 6;
            y1 = 8;
            x2 = 700;
            y2 = 8;

            _CreateSampleAddReferName1(items, "Zapsal:", x1, y1); y1 += h1;
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
            List<IDataFormItem> items = new List<IDataFormItem>();
            Random rand = _SampleRandom;


            return items;
        }
        private static IEnumerable<IDataFormItem> _CreateSample4()
        {
            List<IDataFormItem> items = new List<IDataFormItem>();
            Random rand = _SampleRandom;


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
        private static void _CreateSampleAddLabel1(List<DataFormItem> items, string label, int x, int y, int? w = null, DevExpress.Utils.HorzAlignment? labelHalignment = null)
        {
            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.Label,
                Bounds = new Rectangle(x, y, (w ?? 180), 20),
                Text = label,
                LabelHAlignment = (labelHalignment ?? DevExpress.Utils.HorzAlignment.Far),
                LabelAutoSize = LabelAutoSizeMode.None
            });
        }
        private static void _CreateSampleAddText1(List<DataFormItem> items, int x, int y, int? w = null, DevExpress.XtraEditors.Mask.MaskType? maskType = null, string mask = null, 
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
        private static void _CreateSampleAddReferName1(List<DataFormItem> items, string label, int x, int y)
        {
            _CreateSampleAddLabel1(items, label, x, y);

            items.Add(new DataFormItem() 
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.TextBox, 
                Bounds = new Rectangle(x + 183, y, 150, 20) 
            });

            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.TextBox,
                Bounds = new Rectangle(x + 336, y, 250, 20)
            });
        }
        private static void _CreateSampleAddPrice3(List<DataFormItem> items, string label, int x, int y)
        {
            _CreateSampleAddLabel1(items, label, x, y);

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
            _CreateSampleAddLabel1(items, label, x, y);

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

            _CreateSampleAddLabel1(items, "...", x + 311, y, 30, DevExpress.Utils.HorzAlignment.Center);

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
        private static void _CreateSampleAddMemo(List<DataFormItem> items, string label, int x, int y, int w, int h)
        {
            _CreateSampleAddLabel1(items, label, x, y);

            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.EditBox,
                Bounds = new Rectangle(x + 183, y, w, h),
                ToolTipTitle = "POZNÁMKA",
                ToolTipText = "Zde můžete zadat libovolný text"
            });
        }
        private static void _CreateSampleAddCheckBox1(List<DataFormItem> items, string label, DevExpress.XtraEditors.Controls.CheckBoxStyle? style, int x, int y)
        {
            if (!style.HasValue) style = _SampleCheckBoxStyle();

            items.Add(new DataFormItem()
            {
                ItemName = _SampleItemName(items),
                ItemType = DataFormItemType.CheckBox,
                Bounds = new Rectangle(x + 183, y, 350, 20),
                Text = label,
                CheckBoxStyle = style
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
            _CreateSampleAddLabel1(items, label, x, y);

            string toolTipText = "Návodný text čili ToolTip k této položce v tuto chvíli nic zajímavého neobsahuje";
            int cx = x + 190;
            int t = 1;
            foreach (int w in widths)
            {
                _CreateSampleAddText1(items, cx, y, w, toolTipText: toolTipText , toolTipTitle: "NÁPOVĚDA - " + label + ":" + (t++).ToString());
                cx += w + 3;
            }

            y += 22;
        }
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
        }
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
                RefreshVisibleItems();
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
            _EnsureHostingFocusableItemd();

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
        private void _EnsureHostingFocusableItemd()
        {
            bool needRefresh = ((__CurrentlyFocusedDataItem != null && !__CurrentlyFocusedDataItem.IsHosted) ||
                                (__PreviousFocusableDataItem != null && !__PreviousFocusableDataItem.IsHosted) ||
                                (__NextFocusableDataItem != null && !__NextFocusableDataItem.IsHosted));
            if (needRefresh)
                RefreshVisibleItems();
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

        DxDataFormControlItem __CurrentlyFocusedDataItem;
        DxDataFormControlItem __PreviousFocusableDataItem;
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
        private void RefreshVisibleItems()
        {
            var visibleBounds = this.VisibleBounds;
            bool isActiveContent = this.__IsActiveContent;

            this.SuspendLayout();
            this.BeginInit();

            foreach (var item in Items)
                item.RefreshVisibleItem(visibleBounds, isActiveContent);

            this.EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
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
                RefreshVisibleItems(); 
            } 
        }
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
        /// Tedy prvek musí být obecně fokusovatelný (nikoli Label), musí být obecně Viditelný <see cref="Visible"/>,
        /// musí být Enabled a TabStop.
        /// </summary>
        public bool CanGotFocus
        {
            get
            {
                if (!IsFocusableControl) return false;
                if (!Visible) return false;


                return true;
            }
        }
        /// <summary>
        /// Zajistí, že this prvek bude zobrazen podle toho, zda se nachází v dané viditelné oblasti
        /// </summary>
        /// <param name="visibleBounds"></param>
        /// <param name="isActiveContent"></param>
        internal void RefreshVisibleItem(Rectangle visibleBounds, bool isActiveContent)
        {
            bool isVisible = _IsVisibleItem(visibleBounds, isActiveContent);
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
        /// Obsahuje true pokud this prvek má být někdy viditelný podle definice dat <see cref="IDataFormItem.Visible"/>.
        /// Pokud je tam null, považuje se to za true.
        /// </summary>
        internal bool Visible
        {
            get
            {
                var dataVisible = this.__DataFormItem.Visible;
                if (dataVisible.HasValue && !dataVisible.Value) return false;
                return true;
            }
        }
        /// <summary>
        /// Obsahuje true pokud this prvek má být aktuálně přítomen jako živý prvek v controlu <see cref="ContentPanel"/>,
        /// z hlediska aktivity parent prvku i z hlediska souřadnic
        /// </summary>
        /// <returns></returns>
        internal bool IsCurrentlyVisibleItem
        {
            get
            {
                Rectangle visibleBounds = this.ScrollPanel.VisibleBounds;
                bool isActiveContent = this.ScrollPanel.IsActiveContent;
                return _IsVisibleItem(visibleBounds, isActiveContent);
            }
        }
        /// <summary>
        /// Vrátí true pokud this prvek má být aktuálně přítomen jako živý prvek v controlu <see cref="ContentPanel"/>.
        /// </summary>
        /// <param name="visibleBounds"></param>
        /// <param name="isActiveContent"></param>
        /// <returns></returns>
        private bool _IsVisibleItem(Rectangle visibleBounds, bool isActiveContent)
        {
            // Prvek má být vidět, pokud je aktivní obsah, a pokud v definici prvku není Visible = false:
            if (!isActiveContent) return false;
            if (!Visible) return false;

            // Prvek má být vidět, pokud má klávesový Focus anebo jeho TabIndex je +1 / -1 od aktuálního focusovaného prvku (aby bylo možno na něj přejít klávesou):
            if (this.IScrollPanel.IsNearFocusableItem(this)) return true;

            // Prvek má být vidět, pokud jeho souřadnice jsou ve viditelné oblasti nebo blízko ní:
            var controlBounds = this.Bounds;
            bool isVisibleBounds = DataForm.IsInVisibleBounds(controlBounds, visibleBounds);
            return isVisibleBounds;
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
        Default = HostOnlyVisible | RemoveReleaseHandle | RemoveDispose
    }
    #endregion



    #region Pouze pro testování, později smazat
  
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
