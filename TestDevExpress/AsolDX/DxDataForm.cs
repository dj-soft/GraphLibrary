// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using DevExpress.XtraEditors;

namespace Noris.Clients.Win.Components.AsolDX
{
    using Noris.Clients.Win.Components.AsolDX.DataForm;

    /// <summary>
    /// DataForm
    /// </summary>
    public class DxDataForm : DxPanelControl
    {
        #region Konstruktor a jednoduché property
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxDataForm()
        {
        }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DisposeVisualControls();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Jsou aktivní zápisy do logu? Default = false
        /// </summary>
        public override bool LogActive { get { return base.LogActive; } set { base.LogActive = value; if (_DataFormPanel != null) _DataFormPanel.LogActive = value; } }
        #endregion
        #region Zobrazované prvky = definice stránek a vlastní data
        /// <summary>
        /// Definice vzhledu.
        /// Dokud nebude vložena definice vzhledu, bude prvek prázdný.
        /// </summary>
        public IEnumerable<IDataFormPage> Pages { get { return _Pages.ToArray(); } set { _SetPages(value); } }
        private List<IDataFormPage> _Pages;
        /// <summary>
        /// Vlastní data
        /// </summary>
        public System.Data.DataTable DataTable { get { return _DataTable; } set { _DataTable = value; this.Refresh(RefreshParts.InvalidateControl); } }
        private System.Data.DataTable _DataTable;

        /// <summary>
        /// Vloží dané stránky do this instance
        /// </summary>
        /// <param name="pages"></param>
        private void _SetPages(IEnumerable<IDataFormPage> pages)
        {
            _FillPages(pages);
            _PrepareDataTabs();
            _ActivatePage();
        }
        /// <summary>
        /// Z dodaných stránek vytvoří zdejší datové struktury: naplní pole <see cref="_Pages"/> a <see cref="_DataFormPages"/>, nic dalšího nedělá
        /// </summary>
        /// <param name="pages"></param>
        private void _FillPages(IEnumerable<IDataFormPage> pages)
        {
            _DataFormPages = DxDataFormPage.CreateList(this, pages);
            _Pages = _DataFormPages.Select(p => p.IPage).ToList();
        }
        /// <summary>
        /// Metoda invaliduje všechny souřadnice na stránkách, které jsou závislé na Zoomu a na DPI.
        /// Metoda sama neprovádí další přepočty layoutu ani tvorbu záložek, to je úkolem metody <see cref="_PreparePagesLayout"/>.
        /// </summary>
        private void _InvalidateCurrentBounds()
        {
            _DataFormPages?.ForEachExec(p => p.InvalidateBounds());
        }
        /// <summary>
        /// Metoda zajistí, že ve všech definicích stránek v <see cref="_DataFormPages"/> budou správně určeny souřadnice skupin, 
        /// a budou z nich vytvořeny data pro záložky do <see cref="_DataFormTabs"/>.
        /// Následně budou připraveny vizuální controly a do nich naplněny patřičné grupy pro zobrazení.
        /// </summary>
        private void _PrepareDataTabs()
        {
            if (_DataFormPages == null) return;
            _PreparePagesLayout();
            _PrepareVisualControl();
        }
        /// <summary>
        /// Určí souřadnice skupin na jednotlivých stránkách.
        /// Mohl by dělat i dynamický layout, v budoucnu...
        /// Výstupem je struktura záložek v <see cref="_DataFormTabs"/>.
        /// </summary>
        private void _PreparePagesLayout()
        {
            if (_DataFormPages == null) return;

            // Začněme základním layoutem:
            foreach (var dataPage in _DataFormPages)
            {
                Point location = new Point(0, 0);
                foreach (var dataGroup in dataPage.Groups)
                {
                    dataGroup.CurrentGroupOrigin = location;
                    var size = dataGroup.CurrentGroupBounds.Size;    // Tady dojde k vyhodnocení souřadnice CurrentGroupOrigin a k přepočtu DesignSize na CurrentSize.
                    location.Y += size.Height;
                }
            }

            // Analýza dynamického layoutu:




            // Finalizace:
            _DataFormTabs = new List<DxDataFormTab>();
            int pageIndex = 0;
            foreach (var dataPage in _DataFormPages)
            {
                DxDataFormTab dataTab = new DxDataFormTab(this, "TabPage" + (pageIndex++).ToString());
                dataTab.Add(dataPage);
                _DataFormTabs.Add(dataTab);
            }
        }
        private void _ActivatePage(string pageId = null)
        {
        }
        private void _ActivateTab(string tabName = null)
        {
        }
        /// <summary>
        /// Po změně velikosti
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            _PreparePagesLayout();
        }
        /// <summary>
        /// Tento háček je vyvolán po jakékoli akci, která může vést k přepočtu vnitřních velikostí controlů.
        /// Je volán: po změně Zoomu, po změně Skinu, po změně DPI hostitelského okna.
        /// <para/>
        /// Potomek by v této metodě měl provést přepočty velikosti svých controlů, pokud závisejí na Zoomu a DPI (a možná Skinu) (rozdílnost DesignSize a CurrentSize).
        /// <para/>
        /// Metoda není volána po změně velikosti controlu samotného ani po změně ClientBounds, ta změna nezakládá důvod k přepočtu velikosti obsahu
        /// </summary>
        protected override void OnContentSizeChanged()
        {
            base.OnContentSizeChanged();
            _InvalidateCurrentBounds();
            _PreparePagesLayout();
        }

        private void Refresh(RefreshParts refreshParts)
        {

        }

        private bool TryGetFormTab(string tabName, out DxDataFormTab formTab)
        {
            return _DataFormTabs.TryGetFirst(t => String.Equals(t.TabName, tabName), out formTab);
        }
        /// <summary>
        /// Data jednotlivých stránek
        /// </summary>
        private List<DxDataFormPage> _DataFormPages;
        /// <summary>
        /// Data jednotlivých záložek.
        /// Jedna záložka může obsahovat jednu nebo více stránek <see cref="DxDataFormPage"/>.
        /// Pokud záložka obsahuje více stránek, pak další stránky už mají vypočtené souřadnice skupin <see cref="DxDataFormGroup.CurrentGroupBounds"/> správně (a tedy i jejich prvky mají správné souřadnice).
        /// <para/>
        /// Toto pole je vytvořeno v metodě <see cref="_PreparePagesLayout"/>.
        /// </summary>
        private List<DxDataFormTab> _DataFormTabs;

        #endregion
        #region Práce s controly DxDataFormPanel (jednoduchá DataForm) a / nebo TabPane (záložky)
        private void _PrepareVisualControl()
        {
            if (_DataFormTabs.Count <= 1)
                _PrepareSinglePanel();
            else
                PrepareTabPages();

        }
        private void _PrepareSinglePanel()
        {
            _PrepareDataFormPanel();
            _RemoveControlFromParent(_DataFormTabPane, this);        // Pokud máme jako náš Child control přítomný TabPane, odebereme jej
            _AddControlToParent(_DataFormPanel, this);               // Zajistíme, že DataFormPanel bude přítomný jako náš přímý Child control

            _DataFormPanel.Groups = _DataFormTabs.FirstOrDefault()?.Groups;
            _DataFormPanel.Visible = true;
        }
        private void PrepareTabPages()
        {
            _PrepareDataFormTabPane();
            _RemoveControlFromParent(_DataFormPanel, this);          // Pokud máme jako náš Child control přítomný DataFormPanel, odebereme jej
            _PrepareDataFormTabPages();
            _AddControlToParent(_DataFormTabPane, this);             // Zajistíme, že TabPane bude přítomný jako náš přímý Child control


            _DataFormTabPane.Visible = false;
        }

        /// <summary>
        /// Vytvoří vlastní panel DataForm
        /// </summary>
        private void _PrepareDataFormPanel()
        {
            if (_DataFormPanel != null) return;
            _DataFormPanel = new DxDataFormPanel(this);
            _DataFormPanel.Dock = DockStyle.Fill;
            _DataFormPanel.LogActive = this.LogActive;
        }
        /// <summary>
        /// Disposuje vlastní panel DataForm
        /// </summary>
        private void _DisposeDataFormPanel()
        {
            _DataFormPanel?.Dispose();
            _DataFormPanel = null;
        }
        /// <summary>
        /// Vlastní panel DataForm. Buď bude zobrazen rovnou, anebo na aktivní záložce.
        /// </summary>
        private DxDataFormPanel _DataFormPanel;

        private void _PrepareDataFormTabPane()
        {
            if (_DataFormTabPane != null) return;
            DxTabPane tabPane = new DxTabPane() { Dock = DockStyle.Fill };
            tabPane.Visible = false;
            tabPane.SelectedPageChanged += TabPane_SelectedPageChanged;
            tabPane.SelectedPageChanging += TabPane_SelectedPageChanging;
            _DataFormTabPane = tabPane;
        }
        private void TabPane_SelectedPageChanging(object sender, DevExpress.XtraBars.Navigation.SelectedPageChangingEventArgs e)
        {
        }
        private void TabPane_SelectedPageChanged(object sender, DevExpress.XtraBars.Navigation.SelectedPageChangedEventArgs e)
        {
            _PrepareDataFormPanel();
            var type = e.Page.GetType();
            var tabPage = e.Page as DevExpress.XtraBars.Navigation.TabNavigationPage;
            var tabName = tabPage.Name;


            if (TryGetFormTab(tabName, out DxDataFormTab formTab))
            {
                _DataFormPanel.Groups = formTab.Groups;
            }
            else
            {
                _DataFormPanel.Groups = null;
            }

            _AddControlToParent(_DataFormPanel, tabPage);               // Zajistíme, že DataFormPanel bude přítomný jako Child control v nové stránce
            _DataFormPanel.Visible = true;
        }

        private void _PrepareDataFormTabPages()
        {
            _DataFormTabPane.ClearPages();
            foreach (var dataTab in _DataFormTabs)
            {
                _DataFormTabPane.AddNewPage(dataTab.TabName, dataTab.TabText, dataTab.TabToolTipText);
            };
        }
        /// <summary>
        /// Disposuje vlastní panel DataForm
        /// </summary>
        private void _DisposeDataFormTabPane()
        {
            _DataFormTabPane?.Dispose();
            _DataFormPanel = null;
        }
        /// <summary>
        /// Úložiště pro objekt se záložkami. Ve výchozím stavu je null, vytvoří se on-demand.
        /// </summary>
        private DxTabPane _DataFormTabPane;

        /// <summary>
        /// Zajistí vložení daného controlu do daného parenta, pokud tam není.
        /// Pokud by control před tím byl v jiném parentu, odebere jej tamodtud.
        /// Před změnou provede zhasnutí controlu.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="parent"></param>
        private void _AddControlToParent(Control control, Control parent)
        {
            if (control == null || parent == null) return;
            if (control.Parent != null && !Object.ReferenceEquals(control.Parent, parent))
            {   // Pokud mám parenta, a ten je jiný než má být:
                control.Visible = false;
                control.Parent.Controls.Remove(control);
            }
            if (control.Parent == null)
            {   // Pokud nemám parenta:
                control.Visible = false;
                parent.Controls.Add(control);
            }
        }
        /// <summary>
        /// Zajistí odebrání daného controlu z daného parenta, pokud tam je.
        /// Před tím provede zhasnutí controlu.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="parent"></param>
        private void _RemoveControlFromParent(Control control, Control parent)
        {
            if (control == null || parent == null) return;
            control.Visible = false;
            if (control.Parent != null && Object.ReferenceEquals(control.Parent, parent))
                control.Parent.Controls.Remove(control);
        }
        /// <summary>
        /// Dispose vizuálních controlů
        /// </summary>
        private void DisposeVisualControls()
        {
            _DisposeDataFormPanel();
            _DisposeDataFormTabPane();
        }
        #endregion
        #region Služby pro controly se vztahem do DxDataFormPanel
        /// <summary>
        /// Sdílený objekt ToolTipu do všech controlů
        /// </summary>
        internal DxSuperToolTip DxSuperToolTip { get { return _DataFormPanel?.DxSuperToolTip; } }
        /// <summary>
        /// Daný control přidá do panelu na pozadí (control jen pro kreslení) anebo na popředí (control pro interakci).
        /// </summary>
        /// <param name="control"></param>
        /// <param name="addToBackground"></param>
        internal void AddControl(Control control, bool addToBackground) { _DataFormPanel?.AddControl(control, addToBackground); }
        /// <summary>
        /// Daný control odebere z panelu na pozadí (control jen pro kreslení) anebo na popředí (control pro interakci).
        /// </summary>
        /// <param name="control"></param>
        /// <param name="removeFromBackground"></param>
        internal void RemoveControl(Control control, bool removeFromBackground) { _DataFormPanel?.RemoveControl(control, removeFromBackground); }
        /// <summary>
        /// Test výkonu
        /// </summary>
        /// <param name="count"></param>
        /// <param name="forceRefresh"></param>
        public void TestPerformance(int count, bool forceRefresh)
        {
            _DataFormPanel?.TestPerformance(count, forceRefresh);
        }
        /// <summary>
        /// Počet celkem deklarovaných prvků
        /// </summary>
        internal int ItemsCount { get { return _DataFormPages.Select(p => p.ItemsCount).Sum(); } }
        /// <summary>
        /// Počet aktuálně viditelných prvků
        /// </summary>
        internal int VisibleItemsCount { get { return _DataFormPanel?.VisibleItemsCount ?? 0; } }
        #endregion
    }
    /// <summary>
    /// Třída, která generuje testovací předpisy a data pro testy <see cref="DxDataForm"/>
    /// </summary>
    public class DxDataFormSamples
    {
        /// <summary>
        /// Vytvoří a vrátí data pro definici DataFormu
        /// </summary>
        /// <param name="texts"></param>
        /// <param name="tooltips"></param>
        /// <param name="sampleId"></param>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        public static List<IDataFormPage> CreateSampleData(string[] texts, string[] tooltips, int sampleId, int rowCount)
        {
            List<IDataFormPage> pages = new List<IDataFormPage>();
            DataFormPage page;

            page = CreateSamplePage(texts, tooltips, sampleId, rowCount, "Základní stránka", "Obsahuje běžné informace");
            pages.Add(page);

            if (sampleId == 2)
            {
                page = CreateSamplePage(texts, tooltips, 3, 125, "Doplňková stránka", "Obsahuje další málo používané informace");
                pages.Add(page);
            }

            return pages;
        }
        private static DataFormPage CreateSamplePage(string[] texts, string[] tooltips, int sampleId, int rowCount, string pageText, string pageToolTip)
        {
            DataFormPage page = new DataFormPage();
            page.PageText = pageText;
            page.ToolTipTitle = pageText;
            page.ToolTipText = pageToolTip;

            DataFormGroup group = new DataFormGroup();
            group.AutoGroupSizePadding = new Padding(12, 12, 12, 12);
            page.Groups.Add(group);

            Random random = new Random();
            int textsCount = texts.Length;
            int tooltipsCount = tooltips.Length;

            string text, tooltip;
            int[] widths = null;
            int addY = 0;
            switch (sampleId)
            {
                case 1:
                    widths = new int[] { 140, 260, 40, 300, 120 };
                    addY = 28;
                    break;
                case 2:
                    widths = new int[] { 80, 150, 80, 60, 100, 120, 160, 40, 120, 180, 80, 40, 60, 250 };
                    addY = 21;
                    break;
                case 3:
                    widths = new int[] { 250, 250, 60, 250, 250, 60, 250 };
                    addY = 30;
                    break;
            }
            int count = rowCount;
            int y = 80;
            int maxX = 0;
            int q;
            for (int r = 0; r < count; r++)
            {
                int x = 20;
                text = $"Řádek {(r + 1)}";
                DataFormItemImageText label = new DataFormItemImageText() { ItemType = DataFormItemType.Label, Text = text, DesignBounds = new Rectangle(x, y + 2, 70, 18) };
                group.Items.Add(label);

                x += 80;
                foreach (int width in widths)
                {
                    bool blank = (random.Next(100) == 68);
                    text = (!blank ? texts[random.Next(textsCount)] : "");
                    tooltip = (!blank ? tooltips[random.Next(tooltipsCount)] : "");

                    q = random.Next(100);
                    DataFormItemType itemType = (q < 5 ? DataFormItemType.None :
                                                (q < 10 ? DataFormItemType.CheckBox :
                                                (q < 15 ? DataFormItemType.Button :
                                                DataFormItemType.TextBox)));
                    switch (itemType)
                    {
                        case DataFormItemType.TextBox:
                            DataFormItemImageText textBox = new DataFormItemImageText() { ItemType = itemType, Text = text, ToolTipText = tooltip, DesignBounds = new Rectangle(x, y, width, 20) };
                            group.Items.Add(textBox);
                            break;
                        case DataFormItemType.CheckBox:
                            DataFormItemCheckItem checkBox = new DataFormItemCheckItem() { ItemType = itemType, Text = text, ToolTipText = tooltip, DesignBounds = new Rectangle(x, y, width, 20) };
                            group.Items.Add(checkBox);
                            break;
                        case DataFormItemType.Button:
                            DataFormItemImageText button = new DataFormItemImageText() { ItemType = itemType, Text = text, ToolTipText = tooltip, DesignBounds = new Rectangle(x, y, width, 20) };
                            group.Items.Add(button);
                            break;
                    }
                    x += width + 3;
                }
                maxX = x;
                y += addY;
            }

            return page;
        }
    }
}

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    /// <summary>
    /// Jeden panel dataformu: reprezentuje základní panel, hostuje v sobě dva ScrollBary 
    /// a ContentPanel, v němž se zobrazují grupy a v nich itemy.
    /// <para/>
    /// Panel <see cref="DxDataFormPanel"/> je zobrazován v <see cref="DxDataForm"/> buď v celé jeho ploše (to když DataForm obsahuje jen jednu stránku),
    /// anebo je v <see cref="DxDataForm"/> zobrazen záložkovník <see cref="DxTabPane"/>, a v každé záložce je zobrazován zdejší panel <see cref="DxDataFormPanel"/>,
    /// obsahuje pak jen grupy jedné konkrétní stránky.
    /// Toto řídí třída <see cref="DxDataForm"/> podle dodaných stránek a podle dynamického layoutu.
    /// </summary>
    internal class DxDataFormPanel : DxScrollableContent
    {
        #region Konstruktor a vztah na DxDataForm
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataForm"></param>
        public DxDataFormPanel(DxDataForm dataForm)
        {
            _DataForm = dataForm;

            this.DoubleBuffered = true;
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Style3D;

            InitializeContentPanel();
            InitializeGroups();
            InitializeControls();
            InitializePaint();
            InitializeInteractivity();
        }
        /// <summary>Vlastník - <see cref="DxDataForm"/>, ale nemusí to být Parent!</summary>
        private DxDataForm _DataForm;
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DisposeControls();
            InvalidateImageCache();
            _Groups?.Clear();
            DisposeGroups();
            DisposeContentPanel();

            base.Dispose(disposing);

            _DataForm = null;
        }
        /// <summary>
        /// Inicializuje panel <see cref="_ContentPanel"/>
        /// </summary>
        private void InitializeContentPanel()
        {
            this._ContentPanel = new DxPanelBufferedGraphic() { Visible = true, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            this._ContentPanel.LogActive = this.LogActive;
            this._ContentPanel.Layers = BufferedLayers;                        // Tady můžu přidat další vrstvy, když budu chtít kreslit 'pod' anebo 'nad' hlavní prvky
            this._ContentPanel.PaintLayer += _ContentPanel_PaintLayer;         // A tady bych pak musel reagovat na kreslení přidaných vrstev...
            this.ContentControl = this._ContentPanel;
        }
        /// <summary>
        /// Disposuje panel <see cref="_ContentPanel"/>
        /// </summary>
        private void DisposeContentPanel()
        {
            this.ContentControl = null;
            if (this._ContentPanel != null)
            {
                this._ContentPanel.PaintLayer -= _ContentPanel_PaintLayer;
                this._ContentPanel.Dispose();
                this._ContentPanel = null;
            }
        }
        /// <summary>
        /// Panel, ve kterém se vykresluje i hostuje obsah DataFormu. Panel je <see cref="DxPanelBufferedGraphic"/>, 
        /// ale z hlediska <see cref="DxDataForm"/> nemá žádnou funkcionalitu, ta je soustředěna do <see cref="DxDataFormPanel"/>.
        /// </summary>
        private DxPanelBufferedGraphic _ContentPanel;
        #endregion
        #region Public vlastnosti
        

        #endregion
        #region Grupy a jejich Items, viditelné grupy a viditelné itemy
        /// <summary>
        /// Zobrazované grupy a jejich prvky
        /// </summary>
        public List<DxDataFormGroup> Groups { get { return _Groups; } set { _SetGroups(value); } }
        /// <summary>
        /// Inicializuje pole prvků
        /// </summary>
        private void InitializeGroups()
        {
            _VisibleItems = new List<DxDataFormItem>();
        }
        /// <summary>
        /// Vloží do sebe dané grupy a zajistí minimální potřebné refreshe
        /// </summary>
        /// <param name="groups"></param>
        private void _SetGroups(List<DxDataFormGroup> groups)
        {
            DisposeGroups();
            if (groups != null)
                _Groups = groups.ToList();

            Refresh(RefreshParts.AfterItemsChangedSilent);
        }
        /// <summary>
        /// Invaliduje aktuální rozměry všech grup v this objektu
        /// </summary>
        /// <returns></returns>
        private void _InvalidatGroupsCurrentBounds()
        {
            _Groups?.ForEachExec(g => g.InvalidateBounds());

            _LastCalcZoom = DxComponent.Zoom;
            _LastCalcDeviceDpi = this.CurrentDpi;
        }
        /// <summary>
        /// Metoda projde aktuální grupy a vrátí velikost prostoru, do kterého se vejde souhrn jejich aktuálních souřadnic.
        /// Tato velikost se pak používá pro řízení scrollování.
        /// </summary>
        /// <returns></returns>
        private Size _GetGroupsTotalCurrentSize()
        {
            if (_Groups == null) return Size.Empty;
            Rectangle bounds = _Groups.Select(g => g.CurrentGroupBounds).SummaryVisibleRectangle() ?? Rectangle.Empty;
            return new Size(bounds.Right, bounds.Bottom);
        }
        /// <summary>
        /// Připraví souhrn viditelných grup a prvků
        /// </summary>
        private void _PrepareVisibleGroupsItems()
        {
            Rectangle virtualBounds = this.ContentVirtualBounds;
            this._VisibleGroups = this._Groups?.Where(g => g.IsVisibleInVirtualBounds(virtualBounds)).ToList();
            this._VisibleItems = this._VisibleGroups?.SelectMany(g => g.Items).Where(i => i.IsVisibleInVirtualBounds(virtualBounds)).ToList();
        }
        /// <summary>
        /// Zahodí všechny položky o grupách a prvcích z this instance
        /// </summary>
        private void DisposeGroups()
        {
            if (_Groups != null)
            {
                _Groups.Clear();
                _Groups = null;
            }
            if (_VisibleGroups != null)
            {
                _VisibleGroups.Clear();
                _VisibleGroups = null;
            }
            if (_VisibleItems != null)
            {
                _VisibleItems.Clear();
                _VisibleItems = null;
            }
        }

        /// <summary>
        /// Počet aktuálně viditelných prvků
        /// </summary>
        internal int? VisibleItemsCount { get { return _VisibleItems?.Count; } }

        private List<DxDataFormGroup> _Groups;
        private List<DxDataFormGroup> _VisibleGroups;
        private List<DxDataFormItem> _VisibleItems;
        #endregion
        #region Interaktivita
        private void InitializeInteractivity()
        {
            _CurrentFocusedItem = null;
            InitializeInteractivityKeyboard();
            InitializeInteractivityMouse();
        }
        #region Keyboard a Focus
        private void InitializeInteractivityKeyboard()
        {
            this._CurrentFocusedItem = null;

            Control parent = this._ContentPanel;        // finálně bude parentem this, pak buttony nebudou vidět
            _FocusInButton = DxComponent.CreateDxSimpleButton(142, 5, 140, 25, parent, " Focus in...", tabStop: true);
            _FocusInButton.TabIndex = 0;
            _FocusOutButton = DxComponent.CreateDxSimpleButton(352, 5, 140, 25, parent, "... focus out.", tabStop: true);
            _FocusOutButton.TabIndex = 29;
        }
        private DxSimpleButton _FocusInButton;
        private DxSimpleButton _FocusOutButton;
        private DxDataFormItem _CurrentFocusedItem;
        #endregion
        #region Myš - Move, Down
        private void InitializeInteractivityMouse()
        {
            this._CurrentOnMouseItem = null;
            this._CurrentOnMouseControlSet = null;
            this._CurrentOnMouseControl = null;
            this._ContentPanel.MouseMove += _ContentPanel_MouseMove;
            this._ContentPanel.MouseDown += _ContentPanel_MouseDown;
        }
        /// <summary>
        /// Myš se pohybuje po Content panelu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ContentPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.None)
                PrepareItemForPoint(e.Location);
        }
        /// <summary>
        /// Myš klikla v Content panelu = nejspíš bychom měli zařídit přípravu prvku a předání focusu ondoň
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ContentPanel_MouseDown(object sender, MouseEventArgs e)
        {
            // toto je nonsens, protože když pod myší existuje prvek, pak MouseDown přejde ondoň nativně, a nikoli z _ContentPanel_MouseDown.
            // Sem se dostanu jen tehdy, když myš klikne na panelu _ContentPanel v místě, kde není žádný prvek.
        }
        /// <summary>
        /// Vyhledá prvek nacházející se pod aktuální souřadnicí myši a zajistí pro prvky <see cref="MouseItemLeave()"/> a <see cref="MouseItemEnter(DxDataFormItem)"/>.
        /// </summary>
        private void PrepareItemForCurrentPoint()
        {
            Point absoluteLocation = Control.MousePosition;
            Point relativeLocation = _ContentPanel.PointToClient(absoluteLocation);
            PrepareItemForPoint(relativeLocation);
        }
        /// <summary>
        /// Vyhledá prvek nacházející se pod danou souřadnicí myši a zajistí pro prvky <see cref="MouseItemLeave()"/> a <see cref="MouseItemEnter(DxDataFormItem)"/>.
        /// </summary>
        /// <param name="location">Souřadnice myši relativně k controlu <see cref="_ContentPanel"/> = reálný parent prvků</param>
        private void PrepareItemForPoint(Point location)
        {
            if (_VisibleItems == null) return;

            DxDataFormItem oldItem = _CurrentOnMouseItem;
            bool oldExists = (oldItem != null);
            bool newExists = _VisibleItems.TryGetLast(i => i.IsVisibleOnPoint(location), out var newItem);

            bool isMouseLeave = (oldExists && (!newExists || (newExists && !Object.ReferenceEquals(oldItem, newItem))));
            if (isMouseLeave)
                MouseItemLeave();

            bool isMouseEnter = (newExists && (!oldExists || (oldExists && !Object.ReferenceEquals(oldItem, newItem))));
            if (isMouseEnter)
                MouseItemEnter(newItem);

            if (isMouseLeave || isMouseEnter)
                this._ContentPanel.InvalidateLayers(DxBufferedLayer.AppBackground);
        }
        /// <summary>
        /// Je voláno při příchodu myši na daný prvek.
        /// </summary>
        /// <param name="item"></param>
        private void MouseItemEnter(DxDataFormItem item)
        {
            if (item.VisibleBounds.HasValue)
            {
                _CurrentOnMouseItem = item;
                _CurrentOnMouseControlSet = GetControlSet(item);
                _CurrentOnMouseControl = _CurrentOnMouseControlSet.GetControlForMouse(item);
                if (!_ContentPanel.IsPaintLayersInProgress)
                {   // V době, kdy probíhá proces Paint, NEBUDU provádět Scroll:
                    //  Ono k tomu v reálu nedochází - Scroll standardně proběhne při KeyEnter (anebo ruční ScrollBar). To jen při testu provádím MouseMove => ScrollToBounds!
                    bool isScrolled = this.ScrollToBounds(item.CurrentBounds, null, true);
                    if (isScrolled) Refresh(RefreshParts.AfterScroll);
                }
            }
        }
        /// <summary>
        /// Je voláno při opuštění myši z aktuálního prvku.
        /// </summary>
        private void MouseItemLeave()
        {
            var oldControl = _CurrentOnMouseControl;
            if (oldControl != null)
            {
                oldControl.Visible = false;
                oldControl.Location = new Point(0, -20 - oldControl.Height);
                oldControl.Enabled = false;
                if (oldControl is BaseControl baseControl)
                    baseControl.SuperTip = null;
            }
            _CurrentOnMouseItem = null;
            _CurrentOnMouseControlSet = null;
            _CurrentOnMouseControl = null;
        }
        /// <summary>
        /// Prvek, nacházející se nyní pod myší
        /// </summary>
        private ControlOneInfo _CurrentItemOnMouseItem;
        /// <summary>
        /// Datový prvek, nacházející se nyní pod myší
        /// </summary>
        private DxDataFormItem _CurrentOnMouseItem;
        /// <summary>
        /// Datový set popisující control, nacházející se nyní pod myší
        /// </summary>
        private ControlSetInfo _CurrentOnMouseControlSet;
        /// <summary>
        /// Vizuální control, nacházející se nyní pod myší
        /// </summary>
        private System.Windows.Forms.Control _CurrentOnMouseControl;
        #endregion
        #endregion
        #region Refresh
        /// <summary>
        /// Provede refresh prvku
        /// </summary>
        public override void Refresh()
        {
            this.RunInGui(() => _RefreshInGui(RefreshParts.Default | RefreshParts.RefreshControl, UsedLayers));
        }
        /// <summary>
        /// Provede refresh daných částí
        /// </summary>
        /// <param name="refreshParts"></param>
        public void Refresh(RefreshParts refreshParts)
        {
            this.RunInGui(() => _RefreshInGui(refreshParts, UsedLayers));
        }
        /// <summary>
        /// Provede refresh daných částí a vrstev
        /// </summary>
        /// <param name="refreshParts"></param>
        /// <param name="layers"></param>
        public void Refresh(RefreshParts refreshParts, DxBufferedLayer layers)
        {
            this.RunInGui(() => _RefreshInGui(refreshParts, layers));
        }
        /// <summary>
        /// Refresh prováděný v GUI threadu
        /// </summary>
        /// <param name="refreshParts"></param>
        /// <param name="layers"></param>
        private void _RefreshInGui(RefreshParts refreshParts, DxBufferedLayer layers)
        {
            // Protože jsme v GUI threadu, nemusím řešit zamykání hodnot - nikdy nebudou dvě vlákna přistupovat k jednomu objektu současně!
            // Spíše musíme vyřešit to, že některá část procesu Refresh způsobí požadavek na Refresh jiné části, což ale může být nějaká část před i za aktuální částí.
            
            // Zapamatuji si úkoly ke zpracování:
            _RefreshPartCurrentBounds |= refreshParts.HasFlag(RefreshParts.InvalidateCurrentBounds);
            _RefreshPartContentTotalSize |= refreshParts.HasFlag(RefreshParts.RecalculateContentTotalSize);
            _RefreshPartVisibleItems |= refreshParts.HasFlag(RefreshParts.ReloadVisibleItems);
            _RefreshPartCache |= refreshParts.HasFlag(RefreshParts.InvalidateCache);
            _RefreshPartInvalidateControl |= refreshParts.HasFlag(RefreshParts.InvalidateControl);
            _RefreshPartRefreshControl |= refreshParts.HasFlag(RefreshParts.RefreshControl);
            _RefreshLayers |= layers;

            // Pokud právě nyní probíhá Refresh, nebudu jej provádět rekurzivně, ale nechám dřívější iteraci doběhnout a zpracovat nově požadované úkoly:
            if (_RefreshInProgress) return;

            // Nemusím řešit zámky, jsem vždy v jednom GUI threadu a nemusím tedy mít obavy z mezivláknové změny hodnot!
            _RefreshInProgress = true;
            try
            {
                while (true)
                {
                    // Autodetekce dalších požadavků:
                    _RefreshPartsAutoDetect();

                    // Pokud nebude co dělat, skončíme:
                    bool doAny = _RefreshPartCurrentBounds || _RefreshPartContentTotalSize || _RefreshPartVisibleItems ||
                                 _RefreshPartCache || _RefreshPartInvalidateControl || _RefreshPartRefreshControl;
                    if (!doAny) return;

                    // Provedeme požadované akce; každá akce nejprve shodí svůj příznak (a teoreticky může nahodit jiný příznak):
                    if (_RefreshPartCurrentBounds) _DoRefreshPartCurrentBounds();
                    if (_RefreshPartContentTotalSize) _DoRefreshPartContentTotalSize();
                    if (_RefreshPartVisibleItems) _DoRefreshPartVisibleItems();
                    if (_RefreshPartCache) _DoRefreshPartCache();
                    if (_RefreshPartInvalidateControl) _DoRefreshPartInvalidateControl();
                    if (_RefreshPartRefreshControl) _DoRefreshPartRefreshControl();
                }
            }
            catch (Exception exc)
            {
                DxComponent.LogAddException(exc);
            }
            finally
            {
                _RefreshInProgress = false;
            }
        }
        /// <summary>
        /// Refresh právě probíhá
        /// </summary>
        public bool IsRefreshInProgress { get { return _RefreshInProgress; } }
        /// <summary>
        /// Refresh právě probíhá
        /// </summary>
        private bool _RefreshInProgress;
        /// <summary>
        /// Zajistit přepočet CurrentBounds v prvcích (=provést InvalidateBounds) = provádí se po změně Zoomu a/nebo DPI
        /// </summary>
        private bool _RefreshPartCurrentBounds;
        /// <summary>
        /// Přepočítat celkovou velikost obsahu
        /// </summary>
        private bool _RefreshPartContentTotalSize;
        /// <summary>
        /// Určit aktuálně viditelné prvky
        /// </summary>
        private bool _RefreshPartVisibleItems;
        /// <summary>
        /// Resetovat cache předvykreslených controlů
        /// </summary>
        private bool _RefreshPartCache;
        /// <summary>
        /// Znovuvykreslit grafiku
        /// </summary>
        private bool _RefreshPartInvalidateControl;
        /// <summary>
        /// Explicitně vyvolat i metodu <see cref="Control.Refresh()"/>
        /// </summary>
        private bool _RefreshPartRefreshControl;
        /// <summary>
        /// Invalidovat tyto vrstvy v rámci _RefreshPartInvalidateControl
        /// </summary>
        private DxBufferedLayer _RefreshLayers;
        /// <summary>
        /// Detekuje automatické požadavky na Refresh
        /// </summary>
        private void _RefreshPartsAutoDetect()
        {
            if (!_RefreshPartCurrentBounds)
            {
                decimal currentZoom = DxComponent.Zoom;
                int currentDpi = this.CurrentDpi;
                if (_LastCalcZoom != currentZoom || _LastCalcDeviceDpi != currentDpi)
                    _RefreshPartCurrentBounds = true;
            }
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.InvalidateCurrentBounds"/>
        /// </summary>
        private void _DoRefreshPartCurrentBounds()
        {
            _RefreshPartCurrentBounds = false;

            _InvalidatGroupsCurrentBounds();
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.RecalculateContentTotalSize"/>
        /// </summary>
        private void _DoRefreshPartContentTotalSize()
        {
            _RefreshPartContentTotalSize = false;

            ContentTotalSize = _GetGroupsTotalCurrentSize();
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.ReloadVisibleItems"/>
        /// </summary>
        private void _DoRefreshPartVisibleItems()
        {
            _RefreshPartVisibleItems = false;

            // Po změně viditelných prvků je třeba provést MouseLeave = prvek pod myší už není ten, co býval:
            this.MouseItemLeave();

            // Připravím soupis aktuálně viditelných prvků:
            _PrepareVisibleGroupsItems();

            // A zajistit, že po vykreslení prvků bude aktivován prvek, který se nachází pod myší:
            // Až po vykreslení proto, že proces vykreslení určí aktuální viditelné souřadnice prvků!
            this._AfterPaintSearchActiveItem = true;
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.InvalidateCache"/>
        /// </summary>
        private void _DoRefreshPartCache()
        {
            _RefreshPartCache = false;

            this.InvalidateImageCache();
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.InvalidateControl"/>
        /// </summary>
        private void _DoRefreshPartInvalidateControl()
        {
            _RefreshPartInvalidateControl = false;

            var layers = this._RefreshLayers;
            if (layers != DxBufferedLayer.None)
                this._ContentPanel.InvalidateLayers(layers);
            this._RefreshLayers = DxBufferedLayer.None;
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.RefreshControl"/>
        /// </summary>
        private void _DoRefreshPartRefreshControl()
        {
            _RefreshPartRefreshControl = false;

            base.Refresh();
        }
        /// <summary>
        /// Po změně DPI je třeba provést kompletní refresh (souřadnice, cache, atd)
        /// </summary>
        protected override void OnDpiChanged()
        {
            base.OnDpiChanged();
            Refresh(RefreshParts.All);
        }
        /// <summary>
        /// Je vyvoláno po změně DPI, po změně Zoomu a po změně skinu. Volá se po přepočtu layoutu.
        /// Může vést k invalidaci interních dat v <see cref="DxScrollableContent.ContentControl"/>.
        /// </summary>
        protected override void OnInvalidateContentAfter()
        {
            base.OnInvalidateContentAfter();
            Refresh(RefreshParts.All);
        }
        /// <summary>
        /// Je voláno pokud dojde ke změně hodnoty <see cref="DxScrollableContent.ContentVirtualBounds"/>, před eventem <see cref="DxScrollableContent.ContentVirtualBoundsChanged"/>
        /// </summary>
        protected override void OnContentVirtualBoundsChanged()
        {
            base.OnContentVirtualBoundsChanged();
            Refresh(RefreshParts.AfterScroll);
        }
        /// <summary>
        /// Systémový Zoom, po který byly posledně přepočteny CurrentBounds
        /// </summary>
        private decimal _LastCalcZoom;
        /// <summary>
        /// DPI this controlu, po který byly posledně přepočteny CurrentBounds
        /// </summary>
        private int _LastCalcDeviceDpi;
        #endregion
        #region Vykreslování a Bitmap cache
        #region Vykreslení celého Contentu
        /// <summary>
        /// Inicializace kreslení
        /// </summary>
        private void InitializePaint()
        {
            _AfterPaintSearchActiveItem = false;
            _PaintingItems = false;
            _PaintLoop = 0L;
            _NextCleanPaintLoop = _CACHECLEAN_OLD_LOOPS + 1;         // První pokus o úklid proběhne po tomto počtu PaintLoop, protože i kdyby bylo potřeba uklidit staré položky, tak stejně nemůže zahodit starší položky - žádné by nevyhovovaly...
        }
        /// <summary>
        /// Test výkonu
        /// </summary>
        /// <param name="count"></param>
        /// <param name="forceRefresh"></param>
        internal void TestPerformance(int count, bool forceRefresh)
        {
            _PaintingPerformaceTestCount = count;
            _PaintingPerformaceForceRefresh = forceRefresh;
            Refresh(RefreshParts.InvalidateControl);
            Application.DoEvents();
        }
        /// <summary>
        /// ContentPanel chce vykreslit některou vrstvu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ContentPanel_PaintLayer(object sender, DxBufferedGraphicPaintArgs args)
        {
            switch (args.LayerId)
            {
                case DxBufferedLayer.AppBackground:
                    PaintContentAppBackground(args);
                    break;
                case DxBufferedLayer.MainLayer:
                    PaintContentMainLayer(args);
                    break;
            }
        }
        /// <summary>
        /// Metoda zajistí vykreslení aplikačního pozadí (okraj aktivních prvků)
        /// </summary>
        /// <param name="e"></param>
        private void PaintContentAppBackground(DxBufferedGraphicPaintArgs e)
        {
            bool isPainted = false;
            var mouseControl = _CurrentOnMouseControl;
            if (mouseControl != null)
                PaintBorder(e, mouseControl.Bounds, Color.DarkViolet, ref isPainted);

            //  Specifikum bufferované grafiky:
            // - pokud do konkrétní vrstvy jednou něco vepíšu, zůstane to tam (až do nějakého většího refreshe).
            // - pokud v procesu PaintLayer do předaného argumentu do e.Graphics nic nevepíšu, znamená to "beze změny".
            // - pokud tedy nyní nemám žádný control k vykreslení, ale posledně jsem něco vykreslil, měl bych grafiku smazat:
            // - k tomu používám e.LayerUserData
            bool oldPainted = (e.LayerUserData is bool && (bool)e.LayerUserData);
            if (oldPainted && !isPainted)
                e.UseBlankGraphics();
            e.LayerUserData = isPainted;
        }
        private void PaintBorder(DxBufferedGraphicPaintArgs e, Rectangle bounds, Color color, ref bool isPainted)
        {
            e.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(color, 32), bounds.Enlarge(3));
            e.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(color, 96), bounds.Enlarge(1));
            isPainted = true;
        }
        private void PaintContentMainLayer(DxBufferedGraphicPaintArgs e)
        { 
            bool afterPaintSearchActiveItem = _AfterPaintSearchActiveItem;
            _AfterPaintSearchActiveItem = false;
            _PaintLoop++;
            int cacheCount = ImageCacheCount;
            if (!_PaintingPerformaceForceRefresh && _PaintingPerformaceTestCount <= 1)
                OnPaintContentStandard(e);
            else
                OnPaintContentPerformaceTest(e);

            if (ImageCacheCount > cacheCount || _NextCleanLiable)
                CleanImageCache();

            if (afterPaintSearchActiveItem)
                PrepareItemForCurrentPoint();
        }
        private void OnPaintContentStandard(DxBufferedGraphicPaintArgs e)
        {
            var startTime = DxComponent.LogTimeCurrent;
            try
            {
                _PaintingItems = true;
                _VisibleItems.ForEachExec(i => PaintItem(i, e));
            }
            finally
            {
                _PaintingItems = false;
            }
            DxComponent.LogAddLineTime($"DxDataFormV2 Paint Standard() Items: {_VisibleItems?.Count}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        private void OnPaintContentPerformaceTest(DxBufferedGraphicPaintArgs e)
        {
            bool forceRefresh = _PaintingPerformaceForceRefresh;
            int count = (_PaintingPerformaceTestCount > 1 ? _PaintingPerformaceTestCount : 1);
            var size = this.ContentVisualSize;
            var startTime = DxComponent.LogTimeCurrent;
            try
            {
                _PaintingItems = true;
                int x = 0;
                int y = 0;
                Rectangle? sumBounds = new Rectangle(Point.Empty, this.ContentTotalSize);
                int maxX = size.Width - (sumBounds?.Right ?? 0) - 12;
                int maxY = size.Height - (sumBounds?.Bottom ?? 0) - 12;
                while (count > 0)
                {
                    if (forceRefresh) ImageCache = null;

                    Point? offset = null;                  // První smyčka má offset == null, bude tedy generovat VisibleBounds
                    _VisibleItems.ForEachExec(i => PaintItem(i, e, offset));
                    y += 7;
                    if (y >= maxY)
                    {
                        y = 0;
                        x += 36;
                        if (x >= maxX)
                            x = 0;
                    }
                    count--;
                    offset = new Point(x, y);              // Další smyčky budou kreslit posunuté obrázky, ale nebudou ukládat VisibleBounds do prvků
                }
            }
            finally
            {
                _PaintingItems = false;
                _PaintingPerformaceTestCount = 1;
                _PaintingPerformaceForceRefresh = false;
            }
            DxComponent.LogAddLineTime($"DxDataFormV2 Paint PerformanceTest() Items: {_VisibleItems.Count}; Loops: {count}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Provede vykreslení jednoho daného prvku
        /// </summary>
        /// <param name="item"></param>
        /// <param name="e"></param>
        /// <param name="offset"></param>
        private void PaintItem(DxDataFormItem item, DxBufferedGraphicPaintArgs e, Point? offset = null)
        {
            var bounds = item.CurrentBounds;
            using (var image = CreateImage(item))
            {
                if (image != null)
                {
                    var visibleOrigin = this.ContentVirtualLocation;
                    Point location = bounds.Location.Sub(visibleOrigin);
                    if (offset.HasValue)
                        location = location.Add(offset.Value);                      // když má offset hodnotu, pak kreslím "posunutý" obraz (jen pro testy), ale nejde o VisibleBounds
                    else
                        item.VisibleBounds = new Rectangle(location, bounds.Size);  // Hodnota offset = null: kreslím "platný obraz", takže si uložím vizuální souřadnici
                    e.Graphics.DrawImage(image, location);
                }
            }
        }
        /// <summary>
        /// Pole jednotlivých vrstev bufferované grafiky
        /// </summary>
        private static DxBufferedLayer[] BufferedLayers { get { return new DxBufferedLayer[] { DxBufferedLayer.AppBackground, DxBufferedLayer.MainLayer }; } }
        /// <summary>
        /// Souhrn vrstev použitých v this controlu, používá se při invalidaci všech vrstev
        /// </summary>
        private static DxBufferedLayer UsedLayers { get { return DxBufferedLayer.AppBackground | DxBufferedLayer.MainLayer; } }

        private bool _AfterPaintSearchActiveItem;
        private long _PaintLoop;
        private bool _PaintingItems = false;
        private int _PaintingPerformaceTestCount;
        private bool _PaintingPerformaceForceRefresh;

        #endregion
        #region Bitmap cache
        /// <summary>
        /// Najde a vrátí <see cref="Image"/> pro obsah daného prvku.
        /// Obrázek hledá nejprve v cache, a pokud tam není pak jej vygeneruje a do cache uloží.
        /// <para/>
        /// POZOR: výstupem této metody je vždy new instance Image, a volající ji musí použít v using { } patternu, jinak zlikviduje paměť.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private Image CreateImage(DxDataFormItem item)
        {
            if (ImageCache == null) ImageCache = new Dictionary<string, ImageCacheItem>();

            ControlSetInfo controlSet = GetControlSet(item);
            string key = controlSet.GetKeyToCache(item);
            if (key == null) return null;

            ImageCacheItem imageInfo = null;
            if (ImageCache.TryGetValue(key, out imageInfo))
            {   // Pokud mám Image již uloženu v Cache, je tam uložena jako byte[], a tady z ní vygenerujeme new Image a vrátíme, uživatel ji Disposuje sám:
                imageInfo.AddHit(_PaintLoop);
                return imageInfo.CreateImage();
            }
            else
            {   // Image v cache nemáme, takže nyní vytvoříme skutečný Image s pomocí controlu, obsah Image uložíme jako byte[] do cache, a uživateli vrátíme ten živý obraz:
                // Tímto postupem šetřím čas, protože Image použiju jak pro uložení do Cache, tak pro vykreslení do grafiky v controlu:
                Image image = CreateBitmapForItem(item);
                lock (ImageCache)
                {
                    if (ImageCache.TryGetValue(key, out imageInfo))
                    {
                        imageInfo.AddHit(_PaintLoop);
                    }
                    else
                    {   // Do cache přidám i image == null, tím ušetřím opakované vytváření / testování obrázku.
                        // Pro přidávání aplikuji lock(), i když tedy tahle činnost má probíhat jen v jednom threadu = GUI:
                        imageInfo = new ImageCacheItem(image, _PaintLoop);
                        ImageCache.Add(key, imageInfo);
                    }
                }
                return image;
            }

            
        }
        /// <summary>
        /// Před přidáním nového prvku do cache provede úklid zastaralých prvků v cache, podle potřeby.
        /// <para/>
        /// Časová náročnost: kontroly se provednou v řádu 0.2 milisekundy, reálný úklid (kontroly + odstranění starých a nepoužívaných položek cache) trvá cca 0.8 milisekundy.
        /// Obecně se pracuje s počtem prvků řádově pod 10000, což není problém.
        /// </summary>
        private void CleanImageCache()
        {
            if (ImageCacheCount == 0) return;

            var startTime = DxComponent.LogTimeCurrent;

            long paintLoop = _PaintLoop;
            if (!_NextCleanLiable && paintLoop < _NextCleanPaintLoop)   // Pokud není úklid povinný, a velikost jsme kontrolovali nedávno, nebudu to ještě řešit.
                return;

            long currentCacheSize = ImageCacheSize;
            if (currentCacheSize < _CACHESIZE_MIN)                   // Pokud mám v cache málo dat (pod 4MB), nebudeme uklízet.
            {
                _NextCleanLiable = false;
                _NextCleanPaintLoop = paintLoop + _CACHECLEAN_AFTER_LOOPS;
                if (LogActive) DxComponent.LogAddLineTime($"DxDataForm.CleanCache(): CacheSize={currentCacheSize}B; úklid není nutný. Čas akce: {DxComponent.LogTokenTimeMilisec}", startTime);
                return;
            }

            // Budu pracovat jen s těmi prvky, které nebyly dlouho použity:
            long lastLoop = paintLoop - _CACHECLEAN_OLD_LOOPS;
            var items = ImageCache.Where(kvp => kvp.Value.LastPaintLoop <= lastLoop).ToList();
            if (items.Count == 0)                                    // Pokud všechny prvky pravidelně používám, nebudu je zahazovat.
            {
                _NextCleanLiable = false;
                _NextCleanPaintLoop = paintLoop + _CACHECLEAN_AFTER_LOOPS_SMALL;
                if (LogActive) DxComponent.LogAddLineTime($"DxDataForm.CleanCache(): CacheSize={currentCacheSize}B; úklid není možný, všechny položky se používají. Čas akce: {DxComponent.LogTokenTimeMilisec}", startTime);
                return;
            }

            // Z nich zahodím ty, které byly použity méně než je průměr:
            string[] keys1 = null;
            decimal? averageUse = items.Average(kvp => (decimal)kvp.Value.HitCount);
            if (averageUse.HasValue)
                keys1 = items.Where(kvp => (decimal)kvp.Value.HitCount < averageUse.Value).Select(kvp => kvp.Key).ToArray();

            CleanImageCache(keys1);

            long cleanedCacheSize = ImageCacheSize;
            if (LogActive) DxComponent.LogAddLineTime($"DxDataForm.CleanCache(): CacheSize={currentCacheSize}B; Odstraněno {keys1?.Length ?? 0} položek; Po provedení úklidu: {cleanedCacheSize}B. Čas akce: {DxComponent.LogTokenTimeMilisec}", startTime);

            // Co a jak příště:
            if (cleanedCacheSize < _CACHESIZE_MIN)                   // Pokud byl tento úklid úspěšný z hlediska minimální paměti, pak příští úklid bude až za daný počet cyklů
            {
                _NextCleanLiable = false;
                _NextCleanPaintLoop = paintLoop + _CACHECLEAN_AFTER_LOOPS;
            }
            else                                                     // Tento úklid byl potřebný (z hlediska času nebo velikosti paměti), ale nedostali jsme se pod _CACHESIZE_MIN:
            {
                _NextCleanLiable = true;                             // Příště budeme volat úklid povinně!
                if (cleanedCacheSize < currentCacheSize)             // Sice jsme neuklidili pod minimum, ale něco jsme uklidili: příští kontrolu zaplánujeme o něco dříve:
                    _NextCleanPaintLoop = paintLoop + _CACHECLEAN_AFTER_LOOPS_SMALL;
            }
        }
        /// <summary>
        /// Z cache vyhodí záznamy pro dané klíče. 
        /// Tato metoda si v případě potřeby (tj. když jsou zadané nějaké klíče) zamkne cache na dobu úklidu.
        /// </summary>
        /// <param name="keys"></param>
        private void CleanImageCache(string[] keys)
        {
            if (keys == null || keys.Length == 0) return;
            lock (ImageCache)
            {
                foreach (string key in keys)
                {
                    if (ImageCache.ContainsKey(key))
                        ImageCache.Remove(key);
                }
            }
        }
        /// <summary>
        /// Po kterém vykreslení <see cref="_PaintLoop"/> budeme dělat další úklid
        /// </summary>
        private long _NextCleanPaintLoop;
        /// <summary>
        /// Po příštím vykreslení zavolat úklid cache i když nedojde k navýšení počtu prvků v cache, protože poslední úklid byl potřebný ale ne zcela úspěšný
        /// </summary>
        private bool _NextCleanLiable;
        /// <summary>
        /// Jaká velikost cache nám nepřekáží? Pokud bude cache menší, nebude probíhat její čištění.
        /// </summary>
        private const long _CACHESIZE_MIN = 1572864L;            // Pro provoz nechme 6MB:  6291456L;      Pro testování úklidu je vhodné mít 1.5MB = 1572864L
        /// <summary>
        /// Po kolika vykresleních controlu budeme ochotni provést další úklid cache?
        /// </summary>
        private const long _CACHECLEAN_AFTER_LOOPS = 6L;
        /// <summary>
        /// Po tolika vykresleních provedeme kontrolu velikosti cache když poslední kontrola neuklidila pod _CACHESIZE_MIN
        /// </summary>
        private const long _CACHECLEAN_AFTER_LOOPS_SMALL = 4L;
        /// <summary>
        /// Jak staré prvky z cache můžeme vyhodit? Počet vykreslovacích cyklů, kdy byl prvek použit.
        /// Pokud prvek nebyl posledních (NNN) cyklů potřeba, můžeme uvažovat o jeho zahození.
        /// </summary>
        private const long _CACHECLEAN_OLD_LOOPS = 12;
        /// <summary>
        /// Zruší veškerý obsah z cache uložených Image <see cref="ImageCache"/>, kde jsou uloženy obrázky pro jednotlivé ne-aktivní controly...
        /// Je nutno volat po změně skinu nebo Zoomu.
        /// </summary>
        private void InvalidateImageCache()
        {
            ImageCache = null;
        }
        /// <summary>
        /// Počet prvků v cache
        /// </summary>
        private int ImageCacheCount { get { return ImageCache?.Count ?? 0; } }
        /// <summary>
        /// Sumární velikost dat v cache
        /// </summary>
        private long ImageCacheSize { get { return (ImageCache != null ? ImageCache.Values.Select(i => i.Length).Sum() : 0L); } }
        /// <summary>
        /// Cache obrázků controlů
        /// </summary>
        private Dictionary<string, ImageCacheItem> ImageCache;
        private class ImageCacheItem
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="image"></param>
            /// <param name="paintLoop"></param>
            public ImageCacheItem(Image image, long paintLoop)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);    // PNG: čas v testu 20-24ms, spotřeba paměti 0.5MB.    BMP: čas 18-20ms, pamět 5MB.    TIFF: čas 50ms, paměť 1.5MB
                    _ImageContent = ms.ToArray();
                }
                this.HitCount = 1L;
                this.LastPaintLoop = paintLoop;
            }
            private byte[] _ImageContent;
            /// <summary>
            /// Metoda vrací new Image vytvořený z this položky cache
            /// </summary>
            /// <returns></returns>
            public Image CreateImage()
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(_ImageContent))
                    return Image.FromStream(ms);
            }
            /// <summary>
            /// Počet byte uložených jako Image v této položce cache
            /// </summary>
            public long Length { get { return _ImageContent.Length; } } 
            /// <summary>
            /// Počet použití této položky cache
            /// </summary>
            public long HitCount { get; private set; }
            /// <summary>
            /// Poslední číslo smyčky, kdy byl prvek použit
            /// </summary>
            public long LastPaintLoop { get; private set; }
            /// <summary>
            /// Přidá jednu trefu v použití prvku (nápočet statistiky prvku)
            /// </summary>
            public void AddHit(long paintLoop)
            { 
                HitCount++;
                if (LastPaintLoop < paintLoop)
                    LastPaintLoop = paintLoop;
            }
        }
        #endregion
        #endregion
        #region Fyzické controly - tvorba, správa, vykreslení bitmapy skrze control
        /// <summary>
        /// Inicializuje data fyzických controlů (<see cref="_ControlsSets"/>, <see cref="_DxSuperToolTip"/>)
        /// </summary>
        private void InitializeControls()
        {
            _ControlsSets = new Dictionary<DataFormItemType, ControlSetInfo>();
            _DxSuperToolTip = new DxSuperToolTip() { AcceptTitleOnlyAsValid = false };
        }
        /// <summary>
        /// Uvolní z paměti veškerá data fyzických controlů
        /// </summary>
        private void DisposeControls()
        {
            if (_ControlsSets == null) return;
            foreach (ControlSetInfo controlSet in _ControlsSets.Values)
                controlSet.Dispose();
            _ControlsSets.Clear();
        }
        private ControlSetInfo GetControlSet(DxDataFormItem item)
        {
            var dataFormControls = _ControlsSets;

            ControlSetInfo controlSet;
            DataFormItemType itemType = item.ItemType;
            if (!dataFormControls.TryGetValue(itemType, out controlSet))
            {
                controlSet = new ControlSetInfo(this._DataForm, itemType);
                dataFormControls.Add(itemType, controlSet);
            }
            return controlSet;

            //Control control;
            //if (!controlSet.TryGetValue(mode, out control))
            //{
            //    control = (itemType == DataFormItemType.Label ? (Control)new DxLabelControl() :
            //              (itemType == DataFormItemType.TextBox ? (Control)new DxTextEdit() :
            //              (itemType == DataFormItemType.CheckBox ? (Control)new DxCheckEdit() :
            //              (itemType == DataFormItemType.Button ? (Control)new DxSimpleButton() : (Control)null))));

            //    if (control != null)
            //    {

            //        Control parent = (mode == DxDataFormControlMode.Focused ? (Control)_ContentPanel :
            //                         (mode == DxDataFormControlMode.HotMouse ? (Control)_ContentPanel :
            //                         (mode == DxDataFormControlMode.Inactive ? (Control)this : (Control)null)));

            //        if (parent != null)
            //        {
            //            control.Location = new Point(5, 5);
            //            control.Visible = false;
            //            parent.Controls.Add(control);
            //        }
            //    }
            //    controlSet.Add(mode, control);
            //}
            //return control;
        }
        private Image CreateBitmapForItem(DxDataFormItem item)
        {
            /*   Časomíra:

               1. Vykreslení bitmapy z paměti do Graphics                    10 mikrosekund
               2. Nastavení souřadnic (Bounds) do controlu                  300 mikrosekund
               3. Vložení textu (Text) do controlu                          150 mikrosekund
               4. Zrušení Selection v TextBoxu                                5 mikrosekund
               5. Vykreslení controlu do bitmapy                            480 mikrosekund

           */

            ControlSetInfo controlSet = GetControlSet(item);
            Control drawControl = controlSet.GetControlForDraw(item);

            int w = drawControl.Width;
            int h = drawControl.Height;
            Bitmap image = new Bitmap(w, h);
            drawControl.DrawToBitmap(image, new Rectangle(0, 0, w, h));

            return image;
        }

        private Dictionary<DataFormItemType, ControlSetInfo> _ControlsSets;
        /// <summary>
        /// Instance třídy, která obhospodařuje jeden typ (<see cref="DataFormItemType"/>) vizuálního controlu, a má až tři instance (Draw, Mouse, Focus)
        /// </summary>
        private class ControlSetInfo : IDisposable
        {
            #region Konstruktor
            /// <summary>
            /// Vytvoří <see cref="ControlSetInfo"/> pro daný typ controlu
            /// </summary>
            /// <param name="dataForm"></param>
            /// <param name="itemType"></param>
            public ControlSetInfo(DxDataForm dataForm, DataFormItemType itemType)
            {
                _DataForm = dataForm;
                _ItemType = itemType;
                switch (itemType)
                {
                    case DataFormItemType.Label:
                        _CreateControlFunction = _LabelCreate;
                        _GetKeyFunction = _LabelGetKey;
                        _FillControlAction = _LabelFill;
                        _ReadControlAction = _LabelRead;
                        break;
                    case DataFormItemType.TextBox:
                        _CreateControlFunction = _TextBoxCreate;
                        _GetKeyFunction = _TextBoxGetKey;
                        _FillControlAction = _TextBoxFill;
                        _ReadControlAction = _TextBoxRead;
                        break;
                    case DataFormItemType.CheckBox:
                        _CreateControlFunction = _CheckBoxCreate;
                        _GetKeyFunction = _CheckBoxGetKey;
                        _FillControlAction = _CheckBoxFill;
                        _ReadControlAction = _CheckBoxRead;
                        break;
                    case DataFormItemType.Button:
                        _CreateControlFunction = _ButtonCreate;
                        _GetKeyFunction = _ButtonGetKey;
                        _FillControlAction = _ButtonFill;
                        _ReadControlAction = _ButtonRead;
                        break;
                    default:
                        throw new ArgumentException($"Není možno vytvořit 'ControlSetInfo' pro typ prvku '{itemType}'.");
                }
                _Disposed = false;
            }
            /// <summary>
            /// Dispose prvků
            /// </summary>
            public void Dispose()
            {
                DisposeControl(ref _ControlDraw, ControlUseMode.Draw);
                DisposeControl(ref _ControlMouse, ControlUseMode.Mouse);
                DisposeControl(ref _ControlFocus, ControlUseMode.Focus);

                _CreateControlFunction = null;
                _FillControlAction = null;
                _ReadControlAction = null;

                _Disposed = true;
            }
            /// <summary>
            /// Pokud je objekt disposován, vyhodí chybu.
            /// </summary>
            private void CheckNonDisposed()
            {
                if (_Disposed) throw new InvalidOperationException($"Nelze pracovat s objektem 'ControlSetInfo', protože je zrušen.");
            }
            /// <summary>Vlastník - <see cref="DxDataForm"/></summary>
            private DxDataForm _DataForm;
            private DataFormItemType _ItemType;
            private Func<Control> _CreateControlFunction;
            private Func<DxDataFormItem, string> _GetKeyFunction;
            private Action<DxDataFormItem, Control, ControlUseMode> _FillControlAction;
            private Action<DxDataFormItem, Control> _ReadControlAction;
            private bool _Disposed;
            #endregion
            #region Label
            private Control _LabelCreate() { return new DxLabelControl(); }
            private string _LabelGetKey(DxDataFormItem item) 
            {
                string key = GetStandardKeyForItem(item);
                return key;
            }
            private void _LabelFill(DxDataFormItem item, Control control, ControlUseMode mode)
            {
                if (!(control is DxLabelControl label)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxLabelControl).Name}.");
                CommonFill(item, label, mode);
            }
            private void _LabelRead(DxDataFormItem item, Control control)
            { }
            #endregion
            #region TextBox
            private Control _TextBoxCreate() { return new DxTextEdit(); }
            private string _TextBoxGetKey(DxDataFormItem item)
            {
                string key = GetStandardKeyForItem(item);
                return key;
            }
            private void _TextBoxFill(DxDataFormItem item, Control control, ControlUseMode mode)
            {
                if (!(control is DxTextEdit textEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxTextEdit).Name}.");
                CommonFill(item, textEdit, mode);
                textEdit.DeselectAll();
                textEdit.SelectionStart = 0;
            }
            private void _TextBoxRead(DxDataFormItem item, Control control)
            { }
            #endregion
            // EditBox
            // SpinnerBox
            #region CheckBox
            private Control _CheckBoxCreate() { return new DxCheckEdit(); }
            private string _CheckBoxGetKey(DxDataFormItem item)
            {
                string key = GetStandardKeyForItem(item);
                return key;
            }
            private void _CheckBoxFill(DxDataFormItem item, Control control, ControlUseMode mode)
            {
                if (!(control is DxCheckEdit checkEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxCheckEdit).Name}.");
                CommonFill(item, checkEdit, mode);
            }
            private void _CheckBoxRead(DxDataFormItem item, Control control)
            { }
            #endregion
            // BreadCrumb
            // ComboBoxList
            // ComboBoxEdit
            // ListView
            // TreeView
            // RadioButtonBox
            #region Button
            private Control _ButtonCreate() { return new DxSimpleButton(); }
            private string _ButtonGetKey(DxDataFormItem item)
            {
                string key = GetStandardKeyForItem(item);
                return key;
            }
            private void _ButtonFill(DxDataFormItem item, Control control, ControlUseMode mode)
            {
                if (!(control is DxSimpleButton button)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxSimpleButton).Name}.");
                CommonFill(item, button, mode);
            }
            private void _ButtonRead(DxDataFormItem item, Control control)
            { }
            #endregion
            // CheckButton
            // DropDownButton
            // Image
            #region Společné metody pro všechny typy prvků
            /// <summary>
            /// Naplní obecně platné hodnoty do daného controlu
            /// </summary>
            /// <param name="item"></param>
            /// <param name="control"></param>
            /// <param name="mode"></param>
            private void CommonFill(DxDataFormItem item, BaseControl control, ControlUseMode mode)
            {
                Rectangle bounds = item.CurrentBounds;
                if (mode == ControlUseMode.Draw)
                {
                    bounds.Location = new Point(4, 4);
                }
                else if (item.VisibleBounds.HasValue)
                {
                    bounds.Location = item.VisibleBounds.Value.Location;
                }

                if (item.IItem is IDataFormItemImageText iit)
                    control.Text = iit.Text;
                control.Enabled = true; // item.Enabled;
                control.SetBounds(bounds);
                control.Visible = true;
                control.SuperTip = GetSuperTip(item, mode);
            }
            /// <summary>
            /// Vrátí instanci <see cref="DxSuperToolTip"/> připravenou pro daný prvek a daný režim. Může vrátit null.
            /// </summary>
            /// <param name="item"></param>
            /// <param name="mode"></param>
            /// <returns></returns>
            private DxSuperToolTip GetSuperTip(DxDataFormItem item, ControlUseMode mode)
            {
                if (mode != ControlUseMode.Mouse) return null;
                var superTip = _DataForm.DxSuperToolTip;
                superTip.LoadValues(item.IItem);
                if (!superTip.IsValid) return null;
                return superTip;
            }
            /// <summary>
            /// Vrátí standardní klíč daného prvku do ImageCache
            /// </summary>
            /// <param name="item"></param>
            /// <returns></returns>
            private string GetStandardKeyForItem(DxDataFormItem item)
            {
                var size = item.CurrentBounds.Size;
                string text = "";
                if (item.IItem is IDataFormItemImageText iit)
                    text = iit.Text;
                string type = ((int)item.ItemType).ToString();
                string key = $"{size.Width}.{size.Height};{type}::{text}";
                return key;
            }
            #endregion
            #region Získání a naplnění controlu z datového Itemu, a reverzní zpětné načtení hodnot z controlu do datového Itemu
            internal Control GetControlForDraw(DxDataFormItem item)
            {
                CheckNonDisposed();
                if (_ControlDraw == null)
                    _ControlDraw = _CreateControl(ControlUseMode.Draw);
                _FillControl(item, _ControlDraw, ControlUseMode.Draw);
                return _ControlDraw;
            }
            internal Control GetControlForMouse(DxDataFormItem item)
            {
                CheckNonDisposed();
                if (_ControlMouse == null)
                    _ControlMouse = _CreateControl(ControlUseMode.Mouse);
                _FillControl(item, _ControlMouse, ControlUseMode.Mouse);
                return _ControlMouse;
            }
            internal Control GetControlForFocus(DxDataFormItem item)
            {
                CheckNonDisposed();
                if (_ControlFocus == null)
                    _ControlFocus = _CreateControl(ControlUseMode.Focus);
                _FillControl(item, _ControlFocus, ControlUseMode.Focus);
                return _ControlFocus;
            }
            /// <summary>
            /// Metoda vrátí stringový klíč do ImageCache pro konkrétní prvek.
            /// Vrácený klíč zohledňuje všechny potřebné a specifické hodnoty z konkrétního prvku.
            /// Je tedy jisté, že dva různé objekty, které vrátí stejný klíč, budou mít stejný vzhled.
            /// </summary>
            /// <param name="item"></param>
            /// <returns></returns>
            internal string GetKeyToCache(DxDataFormItem item)
            {
                CheckNonDisposed();
                string key = _GetKeyFunction?.Invoke(item);
                return key;
            }
            /// <summary>
            /// Vytvoří new instanci zdejšího controlu, umístí ji do neviditelné souřadnice, přidá do Ownera a vrátí.
            /// </summary>
            /// <returns></returns>
            private Control _CreateControl(ControlUseMode mode)
            {
                var control = _CreateControlFunction();
                Size size = control.Size;
                Point location = new Point(10, -10 - size.Height);
                Rectangle bounds = new Rectangle(location, size);
                control.Visible = false;
                control.SetBounds(bounds);
                bool addToBackground = (mode == ControlUseMode.Draw);
                _DataForm.AddControl(control, addToBackground);
                return control;
            }
            private void _FillControl(DxDataFormItem item, Control control, ControlUseMode mode)
            {
                _FillControlAction(item, control, mode);
                control.TabIndex = 10;

                //// source.SetBounds(bounds);                  // Nastavím správné umístění, to kvůli obrázkům na pozadí panelu (různé skiny!), aby obrázky odpovídaly aktuální pozici...
                //Rectangle sourceBounds = new Rectangle(4, 4, bounds.Width, bounds.Height);
                //source.SetBounds(sourceBounds);
                //source.Text = item.Text;

                //var size = source.Size;
                //if (size != bounds.Size)
                //{
                //    item.CurrentSize = size;
                //    bounds = item.CurrentBounds;
                //}

            }
            /// <summary>
            /// Odebere daný control z Ownera, disposuje jej a nulluje ref proměnnou.
            /// </summary>
            /// <param name="control"></param>
            /// <param name="mode"></param>
            private void DisposeControl(ref Control control, ControlUseMode mode)
            {
                if (control == null) return;
                bool removeFromBackground = (mode == ControlUseMode.Draw);
                _DataForm.RemoveControl(control, removeFromBackground);
                control.Dispose();
                control = null;
            }
            private Control _ControlDraw;
            private Control _ControlMouse;
            private Control _ControlFocus;
            #endregion
        }
        /// <summary>
        /// Kompletní informace o jednom prvku: index řádku, dekarace, control set a fyzický control
        /// </summary>
        private class ControlOneInfo
        {
            /// <summary>
            /// Řádek
            /// </summary>
            public int RowIndex;
            /// <summary>
            /// Datový prvek, nacházející se nyní pod myší
            /// </summary>
            public DxDataFormItem Item;
            /// <summary>
            /// Datový set popisující control, nacházející se nyní pod myší
            /// </summary>
            public ControlSetInfo ControlSet;
            /// <summary>
            /// Vizuální control, nacházející se nyní pod myší
            /// </summary>
            public Control Control;
        }
        private enum ControlUseMode { None, Draw, Mouse, Focus }
        /// <summary>
        /// Daný control přidá do panelu na pozadí (control jen pro kreslení) anebo na popředí (control pro interakci).
        /// </summary>
        /// <param name="control"></param>
        /// <param name="addToBackground"></param>
        internal void AddControl(Control control, bool addToBackground)
        {
            if (control == null) return;
            if (addToBackground)
                this.Controls.Add(control);
            else
                this._ContentPanel.Controls.Add(control);
        }
        /// <summary>
        /// Daný control odebere z panelu na pozadí (control jen pro kreslení) anebo na popředí (control pro interakci).
        /// </summary>
        /// <param name="control"></param>
        /// <param name="removeFromBackground"></param>
        internal void RemoveControl(Control control, bool removeFromBackground)
        {
            if (control == null) return;
            if (removeFromBackground)
            {
                if (this.Controls.Contains(control))
                    this.Controls.Remove(control);
            }
            else
            {
                if (this._ContentPanel.Controls.Contains(control))
                    this._ContentPanel.Controls.Remove(control);
            }
        }
        /// <summary>
        /// Sdílený objekt ToolTipu do všech controlů
        /// </summary>
        internal DxSuperToolTip DxSuperToolTip { get { return this._DxSuperToolTip; } }
        private DxSuperToolTip _DxSuperToolTip;
        #endregion
        #region Testovací prvky - zrušit!

        private void InitializeSampleControls()
        {
            _Label = new DxLabelControl() { Bounds = new Rectangle(20, 52, 70, 18), Text = "Popis", TabIndex = 1 };
            _ContentPanel.Controls.Add(_Label);
            _TextBox = new DxTextEdit() { Bounds = new Rectangle(100, 50, 80, 20), Text = "Pokus", TabIndex = 2, TabStop = false };
            _ContentPanel.Controls.Add(_TextBox);
            _CheckBox = new DxCheckEdit() { Bounds = new Rectangle(210, 50, 100, 20), Text = "Předvolba", TabIndex = 3, TabStop = false };
            _ContentPanel.Controls.Add(_CheckBox);
        }
        public DxTextEdit TextBox { get { return _TextBox; } }
        private DxLabelControl _Label;
        private DxTextEdit _TextBox;
        private DxCheckEdit _CheckBox;

        #endregion

    }
    /// <summary>
    /// Data jedné záložky
    /// </summary>
    internal class DxDataFormTab
    {
        #region Konstruktor, vlastník, prvky
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataForm"></param>
        /// <param name="tabName"></param>
        public DxDataFormTab(DxDataForm dataForm, string tabName)
        {
            _DataForm = dataForm;
            _TabName = tabName;
            _Pages = new List<DxDataFormPage>();
        }
        /// <summary>Vlastník - <see cref="DxDataForm"/></summary>
        private DxDataForm _DataForm;
        /// <summary>Jednoznačné jméno záložky, pro spárování TabPane.Page a dat</summary>
        private string _TabName;
        private List<DxDataFormPage> _Pages;
        public void Add(DxDataFormPage dataPage)
        {
            _Pages.Add(dataPage);
        }
        /// <summary>
        /// Vlastník - <see cref="DxDataForm"/>
        /// </summary>
        public DxDataForm DataForm { get { return _DataForm; } }
        /// <summary>
        /// Jednoznačné jméno záložky, pro spárování TabPane.Page a dat
        /// </summary>
        public string TabName { get { return _TabName; } }
        #endregion
        #region Data ze stránek a z jejich skupin
        /// <summary>
        /// Stránky na této záložce
        /// </summary>
        public List<DxDataFormPage> Pages { get { return _Pages; } }
        /// <summary>
        /// Titulek záložky = <see cref="IDataFormPage.PageText"/>, případně sloučený z více stránek
        /// </summary>
        public string TabText { get { return Pages.Select(p => p.PageText).ToOneString(" + "); } }
        public string TabToolTipTitle { get { return Pages.Select(p => p.ToolTipTitle).ToOneString(); } }
        public string TabToolTipText { get { return Pages.Select(p => p.ToolTipText).ToOneString(); } }
        /// <summary>
        /// Zobrazované grupy a jejich prvky. Jde o souhrn skupin z přítomných stránek.
        /// </summary>
        public List<DxDataFormGroup> Groups { get { return Pages.SelectMany(p => p.Groups).ToList(); } }
        #endregion

    }
    /// <summary>
    /// Třída reprezentující jednu designem definovanou stránku v dataformu.
    /// V dynamickém layoutu může jedna fyzická vizuální záložka obsahovat více designových stránek vedle sebe.
    /// Stránka obsahuje deklarované grupy. Stránka neobsahuje svoje souřadnice, stránka není vizuální element. To je až grupa.
    /// </summary>
    internal class DxDataFormPage
    {
        #region Konstruktor, vlastník, prvky
        /// <summary>
        /// Vytvoří a vrátí List obsahující <see cref="DxDataFormPage"/>, vytvořený z dodaných instancí <see cref="IDataFormPage"/>.
        /// </summary>
        /// <param name="dataForm"></param>
        /// <param name="iPages"></param>
        /// <returns></returns>
        public static List<DxDataFormPage> CreateList(DxDataForm dataForm, IEnumerable<IDataFormPage> iPages)
        {
            List<DxDataFormPage> dataPages = new List<DxDataFormPage>();
            if (iPages != null)
            {
                foreach (IDataFormPage iPage in iPages)
                {
                    if (iPage == null) continue;
                    DxDataFormPage dataPage = new DxDataFormPage(dataForm, iPage);
                    dataPages.Add(dataPage);
                }
            }
            return dataPages;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataForm"></param>
        /// <param name="iPage"></param>
        public DxDataFormPage(DxDataForm dataForm, IDataFormPage iPage)
        {
            _DataForm = dataForm;
            _IPage = iPage;
            _Groups = DxDataFormGroup.CreateList(this, iPage?.Groups);
        }
        /// <summary>Vlastník - <see cref="DxDataForm"/></summary>
        private DxDataForm _DataForm;
        /// <summary>Deklarace stránky</summary>
        private IDataFormPage _IPage;
        /// <summary>Grupy</summary>
        private List<DxDataFormGroup> _Groups;
        /// <summary>
        /// Vlastník - <see cref="DxDataForm"/>
        /// </summary>
        public DxDataForm DataForm { get { return _DataForm; } }
        /// <summary>
        /// Deklarace stránky
        /// </summary>
        public IDataFormPage IPage { get { return _IPage; } }
        /// <summary>
        /// Pole skupin na této stránce
        /// </summary>
        public IList<DxDataFormGroup> Groups { get { return _Groups; } }
        /// <summary>
        /// Stránka je aktivní? 
        /// Po iniciaci se přebírá do GUI, následně udržuje GUI.
        /// V jeden okamžik může být aktivních více stránek najednou, pokud je více stránek <see cref="IDataFormPage"/> mergováno do jedné záložky.
        /// </summary>
        public bool Active { get; set; }
        #endregion
        #region Data o stránce
        /// <summary>
        /// Titulek stránky
        /// </summary>
        public string PageText { get { return IPage.PageText; } }
        /// <summary>
        /// Titulek ToolTipu. Pokud nebude naplněn, vezme se text prvku.
        /// </summary>
        public string ToolTipTitle { get { return IPage.ToolTipTitle; } }
        /// <summary>
        /// Text ToolTipu
        /// </summary>
        public string ToolTipText { get { return IPage.ToolTipText; } }
        /// <summary>
        /// Název obrázku stránky
        /// </summary>
        public string PageImageName { get { return IPage.PageImageName; } }
        /// <summary>
        /// Počet celkem deklarovaných prvků
        /// </summary>
        internal int ItemsCount { get { return Groups.Select(g => g.ItemsCount).Sum(); } }
        #endregion
        #region Souřadnice a další služby
        /// <summary>
        /// Invaliduje souřadnice všech svých skupin. To s sebou nese i invalidaci souřadnic prvků těchto skupin.
        /// Invalidují se souřadnice typu Current a Visible. 
        /// Tyto souřadnice budou on-demand přepočteny ze souřadnic typu Design, podle aktuálních hodnot Zoom a DPI.
        /// </summary>
        public void InvalidateBounds()
        {
            _Groups.ForEachExec(g => g.InvalidateBounds());
        }

        #endregion

    }
    /// <summary>
    /// Třída reprezentující jednu grupu na stránce.
    /// Grupa obsahuje prvky.
    /// </summary>
    internal class DxDataFormGroup
    {
        #region Konstruktor, vlastník, prvky
        /// <summary>
        /// Vytvoří a vrátí List obsahující <see cref="DxDataFormGroup"/>, vytvořený z dodaných instancí <see cref="IDataFormGroup"/>.
        /// </summary>
        /// <param name="dataPage"></param>
        /// <param name="iGroups"></param>
        /// <returns></returns>
        public static List<DxDataFormGroup> CreateList(DxDataFormPage dataPage, IEnumerable<IDataFormGroup> iGroups)
        {
            List<DxDataFormGroup> dataGroups = new List<DxDataFormGroup>();
            if (iGroups != null)
            {
                foreach (IDataFormGroup iGroup in iGroups)
                {
                    if (iGroup == null) continue;
                    DxDataFormGroup dataGroup = new DxDataFormGroup(dataPage, iGroup);
                    dataGroups.Add(dataGroup);
                }
            }
            return dataGroups;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataPage"></param>
        /// <param name="iGroup"></param>
        public DxDataFormGroup(DxDataFormPage dataPage, IDataFormGroup iGroup)
        {
            _DataPage = dataPage;
            _IGroup = iGroup;
            _Items = DxDataFormItem.CreateList(this, iGroup?.Items);
            _CalculateAutoSize();
        }
        /// <summary>Vlastník - <see cref="DxDataFormPage"/></summary>
        private DxDataFormPage _DataPage;
        /// <summary>Deklarace grupy</summary>
        private IDataFormGroup _IGroup;
        /// <summary>Prvky</summary>
        private List<DxDataFormItem> _Items;
        /// <summary>
        /// Vlastník - <see cref="DxDataForm"/>
        /// </summary>
        public DxDataForm DataForm { get { return this._DataPage?.DataForm; } }
        /// <summary>
        /// Vlastník - <see cref="DxDataFormPage"/>
        /// </summary>
        public DxDataFormPage DataPage { get { return _DataPage; } }
        /// <summary>
        /// Deklarace grupy
        /// </summary>
        public IDataFormGroup IGroup { get { return _IGroup; } }
        /// <summary>
        /// Jednotlivé prvky grupy
        /// </summary>
        public IList<DxDataFormItem> Items { get { return _Items; } }
        /// <summary>
        /// Zajistí provedení výpočtu automatické velikosti grupy.
        /// </summary>
        private void _CalculateAutoSize()
        {
            ItemDesignOrigin = Point.Empty;
            if (!IGroup.AutoGroupSizePadding.HasValue) return;
            var padding = IGroup.AutoGroupSizePadding.Value;
            var itemSummaryBounds = this.Items.Select(i => i.DesignBounds).SummaryVisibleRectangle() ?? Rectangle.Empty;
            int w = itemSummaryBounds.Right + padding.Horizontal;
            int h = itemSummaryBounds.Bottom + padding.Vertical;
            ItemDesignOrigin = new Point(padding.Left, padding.Top);
            _DesignGroupAutoSize = new Size(w, h);
        }
        private Size? _DesignGroupAutoSize;
        #endregion
        /// <summary>
        /// Počet celkem deklarovaných prvků
        /// </summary>
        internal int ItemsCount { get { return Items.Count; } }

        #region Souřadnice designové, aktuální, viditelné

        /*   JAK JE TO SE SOUŘADNÝM SYSTÉMEM:

          1. Grupa deklaruje svoji vnější designovou velikost = DesignSize.
          2. Je na autorovi designu, aby se vnitřní prvky Items svými souřadnicemi vešly do prostoru grupy.
          3. Pokud autor designu chce aplikovat nějaké okraje (Padding), pak nechť je započítá do souřadnic prvků Items. DataForm je explicitně nepřidává.
          4. Grupy jsou skládány pod sebe = grupa 2 má svůj počátek přesně pod koncem grupy 1, na stejné souřadnici X, počínaje bodem { 0, 0 }.
          5. Veškeré souřadnice na vstupu jsou Designové = vztahují se k zoomu 100% a monitoru 96DPI. Reálné souřadnice přepočítává DataForm.

        */

        /// <summary>
        /// Souřadnice počátku, ke kterému jsou zadány designové souřadnice jednotlivých Items = <see cref="DxDataFormItem.DesignBounds"/>.
        /// </summary>
        internal Point ItemDesignOrigin { get; private set; }
        /// <summary>
        /// Velikost grupy daná designem = pro Zoom 100% a DPI = 96.
        /// Pokud v grupě je povolen AutoSize (<see cref="IDataFormGroup.AutoGroupSizePadding"/>, pak je zde velikost daná obsahem + daný Padding.
        /// </summary>
        public Size DesignGroupSize { get { return _DesignGroupAutoSize ?? IGroup.GroupSize; } }
        /// <summary>
        /// Viditelnost grupy
        /// </summary>
        public bool IsVisible { get { return IGroup.IsVisible; } }
        /// <summary>
        /// Vzhled grupy
        /// </summary>
        public IDataFormAppearance Appearance { get { return IGroup.Appearance; } }
        /// <summary>
        /// Aktuální velikost grupy, je daná designovou velikostí <see cref="DesignGroupSize"/> a je přepočtená Zoomem a DPI
        /// </summary>
        public Size CurrentGroupSize { get { this.CheckCurrentBounds(); return _CurrentGroupSize.Value; } }
        private Size? _CurrentGroupSize;
        /// <summary>
        /// Na této souřadnici (reálné) v rámci grupy začíná souřadnice 0/0 jejcih prvků.
        /// Tuto hodnotu určuje správce DataFormu při tvorbě layoutu (Statický i dynamický laoyut).
        /// Tvorba layoutu probíhá po každé změně rozměru DataFormu i změně Zoomu a DPI.
        /// <para/>
        /// Po setování této souřadnice proběhne invalidace souřadnic Current i Visible, i jednotlivých prvků.
        /// Následně jsou tyto souřadnice on-demand přepočteny.
        /// </summary>
        public Point CurrentGroupOrigin { get { return _CurrentGroupOrigin; } set { _CurrentGroupOrigin = value; InvalidateBounds(); } }
        private Point _CurrentGroupOrigin;
        /// <summary>
        /// Aktuální reálná absolutní souřadnice této grupy. 
        /// Souřadnice je daná počátkem <see cref="CurrentGroupOrigin"/>, který musí setovat koordinátor stránky, 
        /// a velikostí grupy <see cref="CurrentGroupSize"/>, která vychází z deklarace grupy <see cref="IDataFormGroup.GroupSize"/> a je přepočtena Zoomem a DPI.
        /// <para/>
        /// Tato souřadnice ale není posunuta ScrollBarem (je absolutní).
        /// Posunutá vizuální souřadnice je v <see cref="VisibleBounds"/>.
        /// </summary>
        public Rectangle CurrentGroupBounds { get { this.CheckCurrentBounds(); return _CurrentGroupBounds.Value; } }
        private Rectangle? _CurrentGroupBounds;
        /// <summary>
        /// Invaliduje souřadnice <see cref="CurrentGroupSize"/>, <see cref="CurrentGroupBounds"/> a <see cref="VisibleBounds"/>.
        /// Invaliduje i svoje Items.
        /// Invalidují se souřadnice typu Current a Visible. 
        /// Tyto souřadnice budou on-demand přepočteny ze souřadnic typu Design, podle aktuálních hodnot Zoom a DPI.
        /// </summary>
        public void InvalidateBounds()
        {
            _CurrentGroupSize = null;
            _CurrentGroupBounds = null;
            _VisibleBounds = null;
            _Items.ForEachExec(i => i.InvalidateBounds());
        }
        /// <summary>
        /// Zajistí, že souřadnice <see cref="_CurrentGroupSize"/> a budou platné k souřadnicím designovým a k hodnotám aktuálním DPI
        /// </summary>
        private void CheckCurrentBounds()
        {
            if (!_CurrentGroupSize.HasValue)
                _CurrentGroupSize = DxComponent.ZoomToGuiInt(DesignGroupSize, DataForm.CurrentDpi);
            if (!_CurrentGroupBounds.HasValue)
                _CurrentGroupBounds = new Rectangle(_CurrentGroupOrigin, _CurrentGroupSize.Value);
        }
        /// <summary>
        /// Fyzické pixelové souřadnice této grupy na vizuálním controlu, kde se nyní tento prvek nachází.
        /// Jde o vizuální souřadnice v koordinátech controlu, odpovídají např. pohybu myši.
        /// Může být null, pak prvek není zobrazen. Null je i po invalidaci <see cref="InvalidateBounds()"/>.
        /// Tuto hodnotu sem ukládá řídící třída v procesu kreslení jako reálné souřadnice, kam byl prvek vykreslen.
        /// </summary>
        public Rectangle? VisibleBounds { get { return _VisibleBounds; } set { _VisibleBounds = value; } }
        private Rectangle? _VisibleBounds;

        /// <summary>
        /// Vrátí true, pokud this prvek se nachází v rámci dané virtuální souřadnice.
        /// Tedy pokud souřadnice <see cref="CurrentGroupBounds"/> se alespoň zčásti nacházejí uvnitř souřadného prostoru dle parametru <paramref name="virtualBounds"/>.
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <returns></returns>
        public bool IsVisibleInVirtualBounds(Rectangle virtualBounds)
        {
            return (IsVisible && virtualBounds.Contains(CurrentGroupBounds, true));
        }
        #endregion
    }
    /// <summary>
    /// Třída reprezentující jeden každý vizuální prvek v <see cref="DxDataForm"/>.
    /// </summary>
    internal class DxDataFormItem
    {
        #region Konstruktor, vlastník, prvky
        /// <summary>
        /// Vytvoří a vrátí List obsahující <see cref="DxDataFormItem"/>, vytvořený z dodaných instancí <see cref="IDataFormItem"/>.
        /// </summary>
        /// <param name="dataGroup"></param>
        /// <param name="iItems"></param>
        /// <returns></returns>
        public static List<DxDataFormItem> CreateList(DxDataFormGroup dataGroup, IEnumerable<IDataFormItem> iItems)
        {
            List<DxDataFormItem> dataItems = new List<DxDataFormItem>();
            if (iItems != null)
            {
                foreach (IDataFormItem iItem in iItems)
                {
                    if (iItem == null) continue;
                    DxDataFormItem dataItem = new DxDataFormItem(dataGroup, iItem);
                    dataItems.Add(dataItem);
                }
            }
            return dataItems;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataGroup"></param>
        /// <param name="iItem"></param>
        public DxDataFormItem(DxDataFormGroup dataGroup, IDataFormItem iItem)
            : base()
        {
            _DataGroup = dataGroup;
            _IItem = iItem;
        }
        /// <summary>Vlastník - <see cref="DxDataFormGroup"/></summary>
        private DxDataFormGroup _DataGroup;
        /// <summary>Deklarace prvku</summary>
        private IDataFormItem _IItem;
        /// <summary>
        /// Vlastník - <see cref="DxDataForm"/>
        /// </summary>
        public DxDataForm DataForm { get { return this._DataGroup?.DataPage?.DataForm; } }
        /// <summary>
        /// Vlastník - <see cref="DxDataFormPage"/>
        /// </summary>
        public DxDataFormPage DataPage { get { return _DataGroup?.DataPage; } }
        /// <summary>
        /// Vlastník - <see cref="DxDataFormGroup"/>
        /// </summary>
        public DxDataFormGroup DataGroup { get { return _DataGroup; } }
        /// <summary>
        /// Deklarace prvku
        /// </summary>
        public IDataFormItem IItem { get { return _IItem; } }
        #endregion
        #region Data z prvku
        /// <summary>
        /// Typ prvku
        /// </summary>
        public DataFormItemType ItemType { get { return _IItem.ItemType; } }
        /// <summary>
        /// Prvek je viditelný
        /// </summary>
        public bool IsVisible { get { return IItem.IsVisible; } }
        /// <summary>
        /// Prvek bude podsvícen při pohybu myší nad ním
        /// </summary>
        public bool HotTrackingEnabled { get; set; }
        #endregion
        #region Souřadnice designové, aktuální, viditelné
        /// <summary>
        /// Souřadnice designové, v logických koordinátech (kde bod {0,0} je počátek zdejší grupy, bez posunu ScrollBarem).
        /// Typicky se vztahují k 96 DPI a Zoom 100%.
        /// Hodnota se přebírá z datového prvku <see cref="IItem"/>.
        /// </summary>
        public Rectangle DesignBounds { get { return IItem.DesignBounds; } }
        /// <summary>
        /// Aktuální logické koordináty - přepočtené z <see cref="DesignBounds"/> na aktuálně platné DPI a Zoom.
        /// <para/>
        /// Jde o souřadnici absolutní v rámci <see cref="DxDataFormPanel"/>, tedy nejde o souřadnici relativní vzhledem ke grupě, kam prvek patří; 
        /// tato souřadnice <see cref="CurrentBounds"/> již zahrnuje posun o počátek grupy <see cref="DxDataFormGroup.CurrentGroupOrigin"/>.
        /// <para/>
        /// Tato souřadnice ale není posunuta ScrollBarem (je absolutní).
        /// Posunutá vizuální souřadnice je v <see cref="VisibleBounds"/>.
        /// </summary>
        public Rectangle CurrentBounds { get { this.CheckCurrentBounds(); return _CurrentBounds.Value; } }
        private Rectangle? _CurrentBounds;
        /// <summary>
        /// Aktuální velikost prvku. Lze setovat (nezmění se umístění = <see cref="CurrentBounds"/>.Location).
        /// <para/>
        /// Setujme opatrně a jen v případě nutné potřeby, typicky tehdy, když konkrétní vizuální control nechce akceptovat předepsanou velikost (např. výška textboxu v jiném než očekávaném fontu).
        /// Vložená hodnota zde zůstane (a bude obsažena i v <see cref="CurrentBounds"/>) do doby invalidace = než se změní Zoom nebo Skin aplikace.
        /// </summary>
        public Size CurrentSize
        {
            get { this.CheckCurrentBounds(); return _CurrentBounds.Value.Size; }
            set
            {
                this.CheckCurrentBounds();
                _CurrentBounds = new Rectangle(_CurrentBounds.Value.Location, value);
                if (_VisibleBounds.HasValue)
                    _VisibleBounds = new Rectangle(_VisibleBounds.Value.Location, value);
            }
        }
        /// <summary>
        /// Invaliduje souřadnice <see cref="CurrentBounds"/> a <see cref="VisibleBounds"/>.
        /// </summary>
        public void InvalidateBounds()
        {
            _CurrentBounds = null;
            _VisibleBounds = null;
        }
        /// <summary>
        /// Zajistí, že souřadnice <see cref="_CurrentBounds"/> budou platné k souřadnicím designovým a k hodnotám aktuálním DPI
        /// </summary>
        private void CheckCurrentBounds()
        {
            if (!_CurrentBounds.HasValue)
            {
                var designBounds = this.DesignBounds.Add(this.DataGroup.ItemDesignOrigin);                   // Posunutí souřadnic o Padding (vlevo a nahoře)
                var currentRelativeBounds = DxComponent.ZoomToGuiInt(designBounds, DataForm.CurrentDpi);     // Přepočet pomocí Zoomu a DPI
                _CurrentBounds = currentRelativeBounds.Add(this.DataGroup.CurrentGroupOrigin);               // Posunutí o reálný počátek parent grupy
            }
        }
        /// <summary>
        /// Fyzické pixelové souřadnice tohoto prvku na vizuálním controlu, kde se nyní tento prvek nachází.
        /// Jde o vizuální souřadnice v koordinátech controlu, odpovídají např. pohybu myši.
        /// Může být null, pak prvek není zobrazen. Null je i po invalidaci <see cref="InvalidateBounds()"/>.
        /// Tuto hodnotu ukládá řídící třída v procesu kreslení jako reálné souřadnice, kam byl prvek vykreslen.
        /// </summary>
        public Rectangle? VisibleBounds { get { return _VisibleBounds; } set { _VisibleBounds = value; } }
        private Rectangle? _VisibleBounds;
        /// <summary>
        /// Vrátí true, pokud this prvek se nachází v rámci dané virtuální souřadnice.
        /// Tedy pokud souřadnice <see cref="CurrentBounds"/> se alespoň zčásti nacházejí uvnitř souřadného prostoru dle parametru <paramref name="virtualBounds"/>.
        /// <para/>
        /// Souřadnice <see cref="CurrentBounds"/> jsou evidovány v koordinátech controlu (tj. nejsou relativní ke své grupě), proto se mohou napřímo porovnávat s <paramref name="virtualBounds"/>, bez transformací.
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <returns></returns>
        public bool IsVisibleInVirtualBounds(Rectangle virtualBounds)
        {
            return (IsVisible && virtualBounds.Contains(CurrentBounds, true));
        }
        /// <summary>
        /// Vrátí true, pokud this prvek má nastaveny viditelné souřadnice v <see cref="VisibleBounds"/> 
        /// a pokud daný bod (souřadný systém shodný s <see cref="VisibleBounds"/>) se nachází v prostoru this prvku
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsVisibleOnPoint(Point point)
        {
            return (IsVisible && VisibleBounds.HasValue && VisibleBounds.Value.Contains(point));
        }
        #endregion
    }
    #region Enum RefreshParts
    /// <summary>
    /// Položky pro refresh
    /// </summary>
    [Flags]
    internal enum RefreshParts
    {
        /// <summary>
        /// Nic
        /// </summary>
        None = 0,
        /// <summary>
        /// Zajistit přepočet CurrentBounds v prvcích (=provést InvalidateBounds) = provádí se po změně Zoomu a/nebo DPI
        /// </summary>
        InvalidateCurrentBounds = 0x0001,
        /// <summary>
        /// Přepočítat celkovou velikost obsahu
        /// </summary>
        RecalculateContentTotalSize = 0x0002,
        /// <summary>
        /// Určit aktuálně viditelné prvky
        /// </summary>
        ReloadVisibleItems = 0x0004,
        /// <summary>
        /// Resetovat cache předvykreslených controlů
        /// </summary>
        InvalidateCache = 0x0010,
        /// <summary>
        /// Znovuvykreslit grafiku
        /// </summary>
        InvalidateControl = 0x0100,
        /// <summary>
        /// Explicitně vyvolat i metodu <see cref="Control.Refresh()"/>
        /// </summary>
        RefreshControl = 0x0200,

        /// <summary>
        /// Po změně prvků (přidání, odebrání, změna hodnot) (<see cref="RecalculateContentTotalSize"/> + <see cref="ReloadVisibleItems"/>).
        /// Tato hodnota je Silent = neobsahuje <see cref="InvalidateControl"/>.
        /// </summary>
        AfterItemsChangedSilent = RecalculateContentTotalSize | ReloadVisibleItems,
        /// <summary>
        /// Po změně prvků (přidání, odebrání, změna hodnot) (<see cref="RecalculateContentTotalSize"/> + <see cref="ReloadVisibleItems"/> + <see cref="InvalidateControl"/>).
        /// Tato hodnota není Silent = obsahuje i invalidaci <see cref="InvalidateControl"/> = překreslení controlu.
        /// <para/>
        /// Toto je standardní refresh.
        /// </summary>
        AfterItemsChanged = RecalculateContentTotalSize | ReloadVisibleItems | InvalidateControl,
        /// <summary>
        /// Po scrollování (<see cref="ReloadVisibleItems"/> + <see cref="InvalidateControl"/>)
        /// </summary>
        AfterScroll = ReloadVisibleItems | InvalidateControl,
        /// <summary>
        /// Po změně prvků (přidání, odebrání, změna hodnot) (<see cref="RecalculateContentTotalSize"/> + <see cref="ReloadVisibleItems"/> + <see cref="InvalidateControl"/>).
        /// <para/>
        /// Toto je standardní refresh.
        /// </summary>
        Default = AfterItemsChanged,
        /// <summary>
        /// Všechny akce, včetně invalidace cache (brutální refresh)
        /// </summary>
        All = RecalculateContentTotalSize | ReloadVisibleItems | InvalidateCache | InvalidateControl
    }
    #endregion
}
