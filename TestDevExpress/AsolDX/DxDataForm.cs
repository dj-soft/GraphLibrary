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
using DevExpress.Utils.Extensions;
using DevExpress.XtraRichEdit.Model.History;

#region INFORMACE: Architektura DataFormu, Zobrazení dat, Konstrukce instancí
/*  [A]  Architektura DataFormu
    ===========================
   a. Vrcholový prvek je instance třídy DxDataForm, ten je vytvářen vnější aplikací, a ta pak komunikuje výhradně s DxDataForm
   b. Vstupem jsou jednak definice stránek, skupin a sloupců, setují se do DxDataForm.Pages
   c. Druhým vstupem jsou vlastní data (řádky, sloupce, dynamické definice buněk), vkládají se do DxDataForm.Data

    [B]  Zobrazení dat v DataFormu
    ==============================
   I. Nejjednodušší varianta je zobrazení jedné stránky (=bez záložek) s jednou částí (DxDataFormPart), bez záhlaví, filtrů a sumárních řádků
       Tato varianta je typická pro zobrazení jednoduchých instancí a oken
       I tato varianta může zobrazovat více než jeden řádek s daty
  II. Druhou variantou je zobrazení s více záložkami, pak záložky nikdy nemají více částí (DxDataFormPart) ani záhlaví, filtry atd
       Tato varianta je typická pro zobrazení instancí a složitých DynamicPage
 III. Třetí variantou je zobrazení jedné stránky (=bez záložek) s jednou nebo více částmi (DxDataFormPart), s možností zobrazovat záhlaví, filtry atd
       Tato varianta je typická pro zobrazení a editaci položek / řádků v přehledové šabloně

    [C]  Konstrukce instancí v DataFormu
    ====================================
   1. Základem je třída DxDataForm
      a) Vždy v sobě hostuje data = DxDataFormData
      b) Vždy v sobě drží jednu jedinou instanci třídy DxDataFormPanel = zobrazovač dat, viz dále
      c) Pokud DxDataForm zobrazuje jen jednu stránku, pak nemá záložkovník, ale přímo v sobě hostuje a zobrazuje (jako svůj Control) panel DxDataFormPanel
      d) Pokud DxDataForm zobrazuje více záložek, pak má (jako svůj Control) záložkovník (=standardní DxTabPane), ale má stále jen jednu instanci panelu DxDataFormPanel,
           a tuto instanci DxDataFormPanel vždy umísťuje do aktuálně zobrazené záložky; 
           v procesu přepínání záložek do tohoto panelu vkládá definici skupin a sloupců pro tuto záložku, 
           a vkládá do něj i odpovídající status jeho záložky (stav, v jakém byl naposledy tento panel, když zobrazoval tuto záložku) = pozice scrollbarů atd.

   2. Vysvětlení rozdílu mezi Stránkou: DxDataFormPage a Záložkou: DxDataFormTab
      a) Stránka DxDataFormPage je daná deklarací layoutu DataFormu, odpovídá prvku TAB který má daný LABEL
      b) Záložka DxDataFormTab je vizuální objekt, většinou odpovídá 1:1 stránce DxDataFormPage, 
           s výjimkou, kdy došlo k >povolenému mergování< dvou malých stránek (DxDataFormPage) do jedné záložky (DxDataFormTab)
           K tomu dojde typicky na velkém monitoru; pak je obsah dvou (i více) stránek umístěn vedle sebe na jedné vizuální záložce

   3. Zobrazení dat konkrétní stránky (celého DataFormu nebo jedné záložky) tedy zajišťuje panel DxDataFormPanel
      a) Ten má v sobě deklaraci layoutu (definice: Grupy DxDataFormGroup a v nich sloupce DxDataFormColumn)
           Pokud celý DataForm má vícero záložek, pak panel DxDataFormPanel obsahuje ve své deklaraci layoutu jen podmnožinu týkající se aktuální záložky
      b) DxDataFormPanel za určitých podmínek dovoluje vytvářet Splitted Parts = například zobrazí v levé části několik sloupců, 
           vpravo od nich svislý Splitter, a za Splitterem pak tytéž řádky, ale odscrollované sloupce doprava...
           Obdobně lze Splitnout řádky vodorovně: v horní části mít několik řádků, a v dolní části scrollovat celým přehledem...
      c) Tedy: panel DxDataFormPanel sám nezobrazuje řádky a sloupce a data, ale dává prostor jednotlivým částem typu DxDataFormPart, 
           aby v určité části panelu DxDataFormPanel zobrazily svoje data (řádky, sloupce, záhlaví, filtr, atd).
      d) Panel DxDataFormPanel tedy řídí tvorbu, přemístění a zánik částí, pomocí scrollbarů.
      e) Dále tento panel řídí synchronizaci souřadnic mezi sousedními částmi DxDataFormPart = tak, aby levá i pravá část vedle sebe zobrazovaly shodné řádky.

   4. Vlastní zobrazení dat tedy provádí každá jedna část DxDataFormPart.
      a) DxDataFormPart je tedy hlavní zobrazovač vlastních dat DataFormu = na něm se zobrazují labely, textboxy atd
      b) DxDataFormPart je potomkem DxScrollableContent, a jako Content používá Bufferovanou grafiku DxPanelBufferedGraphic.
      c) Pro každou jednotlivou část (DxDataFormPart) lze nastavit, zda má/nemá zobrazovat svoje oblasti: 
           RowHeader, ColumnHeader, RowFilter, HSplitterInitiator, VSplitterInitiator, SummaryRow, VScrollBar, HScrollBar.
      d) Panel na základě použití iniciátorů splitterů HSplitterInitiator, VSplitterInitiator umožní dělit svůj prostor mezi více částí, anebo je zase slučovat
      e) Veškerá data zobrazuje pomocí třídy Řádek = DxDataFormRow (a to i řádky ColumnHeaders, RowFilter, Summary).
           Zobrazení prvku RowHeader (vlevo) provádí instance řádku DxDataFormRow na základě požadavku z DxDataFormPart.
      f) Každá část zná svoje ID (vertikální i horizontální) a při komunikaci s datovým základem jej předává, 
           dokážeme tedy mít dvě části pod sebou, kdy každá část má jiné třídění řádků a jiný řádkový filtr = jiný počet řádků;
           obdobně lze pro různé svislé části zvolit jiné viditelné sloupce
      g) Část DxDataFormPart v defaultním nastavení zobrazuje všechny sloupce (DxDataFormGroup + DxDataFormColumn), 
           přebírá je z DxDataFormPanel (panel DxDataFormPanel obsahuje jednu vizuální záložku, a tedy podmnožinu ze všech sloupců DataFormu)
           DxDataFormPart ale může dostat explicitní seznam sloupců = pak zobrazuje jen tyto sloupce.

   5. Fyzické zobrazování tedy provádí DxDataFormPart, který pro řízení zobrazení dat z jednotlivých řádků a sloupců datové tabulky 
           používá třídu DxDataFormRow (řádek), ta uvnitř sebe používá DxDataFormCell (buňka).
      a) DataForm musí umět zobrazit data z více než jednoho řádku, a to ve všech třech režimech = včetně dat typu >Master<
      b) DxDataFormPart zobrazuje data z řádků různých typů: ColumnHeader, RowFilter, DataRow, SummaryRow; zobrazuje je do různých částí svého panelu
      c) Tím, že DxDataFormPart je potomkem DxScrollableContent, může nastavovat okraje pro scrollovaný content = DxScrollableContent.ContentVisualPadding
           Tímto způsobem vytvoří prostor nahoře pro ColumnHeader + RowFilter, dole pro SummaryRow a vlevo pro RowHeader;
           tyto prvky tedy vykresluje přímo do plochy DxDataFormPart, kdežto scrollovaný obsah řádků vykresluje do DxScrollableContent.ContentControl




*/
#endregion

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
            InitializeUserControls();
            InitializeImageCache();
            InitializeData();
        }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DisposeImageCache();
            DisposeUserControls();
            DisposeVisualPanels();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Jsou aktivní zápisy do logu? Default = false
        /// </summary>
        public override bool LogActive { get { return base.LogActive; } set { base.LogActive = value; if (_DataFormPanel != null) _DataFormPanel.LogActive = value; } }
        #endregion
        #region Data
        /// <summary>
        /// Inicializace dat
        /// </summary>
        private void InitializeData()
        {
            _Data = new DxDataFormData(this);
        }
        /// <summary>
        /// Vlastní data zobrazená v dataformu
        /// </summary>
        internal DxDataFormData Data { get { return _Data; } }
        private DxDataFormData _Data;
        #endregion
        #region Zobrazované prvky = definice dataformu = stránky, odvození záložek, a vlastní data
        /// <summary>
        /// Definice formuláře.
        /// Dokud nebude vložena definice vzhledu, bude prvek prázdný.
        /// </summary>
        public IDataForm Form { get { return __Form; } set { _SetForm(value); } }
        private IDataForm __Form;
        /// <summary>
        /// Vlastní data
        /// </summary>
        public System.Data.DataTable DataTable { get { return _DataTable; } set { _DataTable = value; this.Refresh(RefreshParts.InvalidateControl); } }
        private System.Data.DataTable _DataTable;
        /// <summary>
        /// Vloží danou definici vzhledu do this instance
        /// </summary>
        /// <param name="form"></param>
        private void _SetForm(IDataForm form)
        {
            __Form = form;
            CreateDataPages(form);
            CreateDataTabs();
            ActivateVisualPanels();
            ActivatePage();
        }
        /// <summary>
        /// Z dodané definice formuláře <see cref="IDataForm"/> vytvoří zdejší datové struktury: 
        /// naplní pole <see cref="_DataFormPages"/>, nic dalšího nedělá.
        /// </summary>
        /// <param name="form"></param>
        private void CreateDataPages(IDataForm form)
        {
            _DataFormPages = DxDataFormPage.CreateList(this, form?.Pages);
        }
        /// <summary>
        /// Určí souřadnice skupin na jednotlivých stránkách.
        /// Mohl by dělat i dynamický layout, v budoucnu...
        /// Výstupem je struktura záložek v <see cref="_DataFormTabs"/>. Jedna záložka obsahuje 1 nebo více stránek. 
        /// </summary>
        private void CreateDataTabs()
        {
            if (_DataFormPages == null) return;

            // Začněme základním layoutem v jednotlivých Pages (tam může dojít k přelévání grup zdola nahoru doprava):
            foreach (var dataPage in _DataFormPages)
                dataPage.PrepareGroupLayout();

            // V dalším kroku můžeme spojit některé stránky do větší kombinované stránky, pokud je v nich místo:
            if (_DataFormPages.Any(p => p.AllowMerge))
            {
            }

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
        /// <summary>
        /// Metoda invaliduje všechny souřadnice na stránkách, které jsou závislé na Zoomu a na DPI.
        /// Metoda sama neprovádí další přepočty layoutu ani tvorbu záložek, to je úkolem metody <see cref="CreateDataTabs"/>.
        /// </summary>
        private void InvalidateCurrentBounds()
        {
            _DataFormPages?.ForEachExec(p => p.InvalidateBounds());
        }
        /// <summary>
        /// Metoda zajistí, že ve všech definicích stránek v <see cref="_DataFormPages"/> budou správně určeny souřadnice skupin, 
        /// a budou z nich vytvořeny data pro záložky do <see cref="_DataFormTabs"/>.
        /// Následně budou připraveny vizuální controly a do nich naplněny patřičné grupy pro zobrazení.
        /// </summary>
        private void PrepareDataTabs()
        {
            if (_DataFormTabs == null) return;
            ActivateVisualPanels();
        }
        private void ActivatePage(string pageId = null)
        {
        }
        /// <summary>
        /// Po změně velikosti
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            CreateDataTabs();
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
            InvalidateCurrentBounds();
            CreateDataTabs();
        }
        /// <summary>
        /// Metoda zkusí najít navigační stránku (typově přesnou) a její data záložky <see cref="DxDataFormTab"/>
        /// pro vstupní obecnou stránku.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="tabPage"></param>
        /// <param name="formTab"></param>
        /// <returns></returns>
        private bool TryGetFormTab(DevExpress.XtraBars.Navigation.INavigationPageBase page, out DevExpress.XtraBars.Navigation.TabNavigationPage tabPage, out DxDataFormTab formTab)
        {
            tabPage = null;
            formTab = null;
            if (!(page is DevExpress.XtraBars.Navigation.TabNavigationPage tp)) return false;
            tabPage = tp;
            var tabName = tabPage.Name;
            return TryGetFormTab(tabName, out formTab);
        }
        /// <summary>
        /// Metoda zkusí najít data záložky <see cref="DxDataFormTab"/>
        /// pro daný název záložky.
        /// </summary>
        /// <param name="tabName"></param>
        /// <param name="formTab"></param>
        /// <returns></returns>
        private bool TryGetFormTab(string tabName, out DxDataFormTab formTab)
        {
            formTab = null;
            if (String.IsNullOrEmpty(tabName)) return false;
            return _DataFormTabs.TryGetFirst(t => String.Equals(t.TabName, tabName), out formTab);
        }
        /// <summary>
        /// Data jednotlivých stránek
        /// </summary>
        private List<DxDataFormPage> _DataFormPages;
        #endregion
        #region Práce s controly DxDataFormPanel (jednoduchý DataForm) a / nebo TabPane (záložky)
        /// <summary>
        /// Aktivuje patřičný control pro zobrazení DataFormu.
        /// </summary>
        private void ActivateVisualPanels()
        {
            int count = _DataFormTabsCount;
            if (count == 0)
                _DeactivateVisualPanels();
            if (count == 1)
                _ActivateSinglePanel();
            else if(count > 1)
                _ActivateTabPane();
        }
        /// <summary>
        /// Zajistí odebrání vizuálních controlů z this panelu.
        /// Použije se např. pokud přijde pole stránek obsahující 0 prvků.
        /// </summary>
        private void _DeactivateVisualPanels()
        {
            _DataFormPanel.RemoveControlFromParent(this, true);            // Pokud máme jako náš přímý Child control přítomný DataFormPanel, odebereme jej
            _DataFormTabPane.RemoveControlFromParent(this, true);          // Pokud máme jako náš Child control přítomný TabPane, odebereme jej
        }
        /// <summary>
        /// Zajistí správnou aktivaci controlů pro zobrazení jednoho panelu bez záložek.
        /// </summary>
        private void _ActivateSinglePanel()
        {
            _PrepareDataFormPanel();
            _DataFormTabPane.RemoveControlFromParent(this, true);          // Pokud máme jako náš Child control přítomný TabPane, odebereme jej
            _DataFormPanel.AddControlToParent(this, true);                 // Zajistíme, že DataFormPanel bude přítomný jako náš přímý Child control

            _DataFormPanel.Groups = _DataFormTabs.FirstOrDefault()?.Groups;
            _DataFormPanel.State = null;
            _DataFormPanel.Visible = true;
        }
        /// <summary>
        /// Zajistí správnou aktivaci controlů pro záložek pro více stránek DataFormu.
        /// </summary>
        private void _ActivateTabPane()
        {
            _PrepareDataFormTabPane();
            _DataFormPanel.RemoveControlFromParent(this, true);            // Pokud máme jako náš přímý Child control přítomný DataFormPanel, odebereme jej
            _PrepareDataFormTabPages();
            _DataFormTabPane.AddControlToParent(this, true);               // Zajistíme, že TabPane bude přítomný jako náš přímý Child control

            _DataFormTabPane.Visible = true;
            // Umístění panelu _DataFormPanel do patřičné záložky, jeho naplnění daty a jeho zobrazení se provádí až v eventhandleru po změně záložky.
        }
        /// <summary>
        /// Dispose vizuálních controlů
        /// </summary>
        private void DisposeVisualPanels()
        {
            _DisposeDataFormPanel();
            _DisposeDataFormTabPane();
        }
        /// <summary>
        /// Aktuální počet podkladů pro záložky = počet prvků v poli <see cref="_DataFormTabsCount"/>
        /// </summary>
        private int _DataFormTabsCount { get { return (_DataFormTabs?.Count ?? 0); } }
        /// <summary>
        /// Data jednotlivých záložek.
        /// Jedna záložka může obsahovat jednu nebo více stránek <see cref="DxDataFormPage"/>.
        /// Pokud záložka obsahuje více stránek, pak další stránky už mají vypočtené souřadnice skupin <see cref="DxDataFormGroup.CurrentGroupBounds"/> správně (a tedy i jejich prvky mají správné souřadnice).
        /// <para/>
        /// Toto pole je vytvořeno v metodě <see cref="CreateDataTabs"/>.
        /// </summary>
        private List<DxDataFormTab> _DataFormTabs;
        #region DxDataFormPanel
        /// <summary>
        /// Vytvoří vlastní panel DataForm
        /// </summary>
        private void _PrepareDataFormPanel()
        {
            if (_DataFormPanel != null) return;
            _DataFormPanel = new DxDataFormPanel(this);
            _DataFormPanel.Dock = DockStyle.Fill;
            _DataFormPanel.LogActive = this.LogActive;
            _DataFormPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
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
        #endregion
        #region DxTabPane
        /// <summary>
        /// Vytvoří vlastní záložkovník TabPane
        /// </summary>
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
            if (e.Page == null) return;

            _PrepareDataFormPanel();

            if (TryGetFormTab(e.OldPage, out var oldTabPage, out var oldFormTab))
            {
                oldFormTab.State = _DataFormPanel.State.Clone();
            }
            if (TryGetFormTab(e.Page, out var newTabPage, out var newFormTab))
            {
                _DataFormPanel.Groups = newFormTab.Groups;
                _DataFormPanel.State = newFormTab.State;

                _DataFormPanel.AddControlToParent(newTabPage);       // Zajistíme, že DataFormPanel bude přítomný jako Child control v nové stránce
                _DataFormPanel.Visible = true;

                _DataFormPanel.Refresh();
            }
            else
            {
                _DataFormPanel.Groups = null;
                _DataFormPanel.State = null;
            }
        }
        /// <summary>
        /// Do vlastního záložkovníku TabPane vygeneruje fyzické stránky podle obsahu v <see cref="_DataFormTabs"/>.
        /// </summary>
        private void _PrepareDataFormTabPages()
        {
            _DataFormTabPane.SetPages(_DataFormTabs, null, true, true);
        }
        /// <summary>
        /// Disposuje vlastní záložkovník TabPane
        /// </summary>
        private void _DisposeDataFormTabPane()
        {
            _DataFormTabPane?.Dispose();
            _DataFormTabPane = null;
        }
        /// <summary>
        /// Úložiště pro objekt se záložkami. Ve výchozím stavu je null, vytvoří se on-demand.
        /// </summary>
        private DxTabPane _DataFormTabPane;
        #endregion
        #endregion
        #region Služby pro controly se vztahem do DxDataFormPanel
        /// <summary>
        /// Daný control přidá do panelu na pozadí (control jen pro kreslení) anebo na popředí (control pro interakci).
        /// </summary>
        /// <param name="control"></param>
        /// <param name="parent"></param>
        internal void AddControl(Control control, Control parent)
        {
            if (control == null) return;
            if (parent == null)
                control.AddControlToParent(this);
            else
                control.AddControlToParent(parent);
        }
        /// <summary>
        /// Provede refresh panelu
        /// </summary>
        /// <param name="refreshParts"></param>
        internal void Refresh(RefreshParts refreshParts) { _DataFormPanel?.Refresh(refreshParts); }
        /// <summary>
        /// Aktuální velikost viditelného prostoru pro DataForm, když by v něm nebyly ScrollBary
        /// </summary>
        internal Size VisibleTotalSize { get { return (this._DataFormPanel?.ClientSize ?? Size.Empty); } }
        /// <summary>
        /// Aktuální velikost viditelného prostoru pro DataForm, po odečtení aktuálně zobrazených ScrollBarů (pokud jsou zobrazeny)
        /// </summary>
        internal Size VisibleContentSize { get { return (this._DataFormPanel?.VisibleContentSize ?? Size.Empty); } }
        /// <summary>
        /// Počet celkem deklarovaných prvků
        /// </summary>
        internal int ItemsCount { get { return _DataFormPages.Select(p => p.ItemsCount).Sum(); } }
        /// <summary>
        /// Počet aktuálně viditelných prvků
        /// </summary>
        internal int VisibleCellsCount { get { return _DataFormPanel?.VisibleCellsCount ?? 0; } }
        /// <summary>
        /// Metoda vrátí Brush odpovídající požadavku. Může vrátit null.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="appearance"></param>
        /// <param name="onMouse"></param>
        /// <param name="hasFocus"></param>
        /// <returns></returns>
        internal static Brush CreateBrushForAppearance(Rectangle? bounds, IDataFormBackgroundAppearance appearance, bool onMouse, bool hasFocus)
        {
            if (!bounds.HasValue || appearance == null || !bounds.Value.HasPixels()) return null;

            Color? color1 = null;
            Color? color2 = null;
            if (hasFocus)
            {   // Focus má přednost před OnMouse:
                color1 = GetCurrentColor(appearance.FocusedBackColor, appearance.FocusedBackColorName, ColorSourceType.ControlBack);
                color2 = GetCurrentColor(appearance.FocusedBackColorEnd, appearance.FocusedBackColorEndName, ColorSourceType.ControlBack);
            }
            else if ((hasFocus || onMouse) && !color1.HasValue)
            {   // Pokud jsme OnMouse anebo Focus, ale pro Focus nemáme definované barvy, tak vezmu barvu OnMouse:
                color1 = GetCurrentColor(appearance.OnMouseBackColor, appearance.OnMouseBackColorName, ColorSourceType.ControlBack);
                color2 = GetCurrentColor(appearance.OnMouseBackColorEnd, appearance.OnMouseBackColorEndName, ColorSourceType.ControlBack);
            }
            if (!color1.HasValue)
            {   // Pokud nemáme barvu, vezmu základní:
                color1 = GetCurrentColor(appearance.BackColor, appearance.BackColorName, ColorSourceType.ControlBack);
                color2 = GetCurrentColor(appearance.BackColorEnd, appearance.BackColorEndName, ColorSourceType.ControlBack);
            }

            return DxComponent.PaintCreateBrushForGradient(bounds.Value, color1, color2, appearance.GradientStyle);
        }
        /// <summary>
        /// Vytvoří <see cref="System.Drawing.Drawing2D.GraphicsPath"/>, která reprezentuje "Rám od obrazu" = "obdélník s otvorem uvnitř".
        /// Vnější hrany obdélníku jsou o (borderRange.Begin) menší než dané souřadnice <paramref name="outerBounds"/>, vnitřní hrany jsou menší o (borderRange.End).
        /// </summary>
        /// <param name="outerBounds">Vnější prostor pro rám</param>
        /// <param name="borderRange">Určuje odstup rámu od prostoru: Begin = odstup vnějšího okraje rámu od <paramref name="outerBounds"/>, Size = šířka rámu</param>
        /// <returns></returns>
        internal static System.Drawing.Drawing2D.GraphicsPath CreateGraphicsFrame(Rectangle outerBounds, Int32Range borderRange)
        {
            return CreateGraphicsFrame(outerBounds, new Padding(borderRange.Size), borderRange.Begin);
        }
        /// <summary>
        /// Vytvoří <see cref="System.Drawing.Drawing2D.GraphicsPath"/>, která reprezentuje "Rám od obrazu" = "obdélník s otvorem uvnitř".
        /// Vnější rozměr rámu je dán v <paramref name="bounds"/> (volitelně může být zmenšen o <paramref name="outerMargin"/>.
        /// Šířky rámu ve čtyřech hranách definuje <paramref name="sizes"/>.
        /// </summary>
        /// <param name="bounds">Vnější souřadnice rámu</param>
        /// <param name="sizes">Šířka rámu ve čtyřech hranách, může se tedy každá hrana lišit</param>
        /// <param name="outerMargin">Volitelně zmenšení mezi vnějšími souřadnicemi a vnějším okrajem rámu. Bude-li zadáno +5px, pak uvnitř daných <paramref name="bounds"/> bude 5px prázdných, a teprve uvnitř nich začne Frame.</param>
        /// <returns></returns>
        internal static System.Drawing.Drawing2D.GraphicsPath CreateGraphicsFrame(Rectangle bounds, Padding sizes, int? outerMargin)
        {
            if (outerMargin.HasValue) bounds = bounds.Enlarge(-outerMargin.Value);
            return CreateGraphicsFrame(bounds, sizes);
        }
        /// <summary>
        /// Vytvoří <see cref="System.Drawing.Drawing2D.GraphicsPath"/>, která reprezentuje "Rám od obrazu" = "obdélník s otvorem uvnitř".
        /// Šířky rámu ve čtyřech hranách definuje <paramref name="sizes"/>.
        /// </summary>
        /// <param name="bounds">Vnější souřadnice rámu</param>
        /// <param name="sizes">Šířka rámu ve čtyřech hranách, může se tedy každá hrana lišit</param>
        /// <returns></returns>
        internal static System.Drawing.Drawing2D.GraphicsPath CreateGraphicsFrame(Rectangle bounds, int sizes)
        {
            return CreateGraphicsFrame(bounds, new Padding (sizes));
        }
        /// <summary>
        /// Vytvoří <see cref="System.Drawing.Drawing2D.GraphicsPath"/>, která reprezentuje "Rám od obrazu" = "obdélník s otvorem uvnitř".
        /// Šířky rámu ve čtyřech hranách definuje <paramref name="sizes"/>.
        /// </summary>
        /// <param name="bounds">Vnější souřadnice rámu</param>
        /// <param name="sizes">Šířka rámu ve čtyřech hranách, může se tedy každá hrana lišit</param>
        /// <returns></returns>
        internal static System.Drawing.Drawing2D.GraphicsPath CreateGraphicsFrame(Rectangle bounds, Padding sizes)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Alternate);

            path.AddRectangle(bounds);

            Rectangle inner = bounds.Sub(sizes);
            path.AddRectangle(inner);

            return path;
        }
        /// <summary>
        /// Vykreslí pozadí obsahu
        /// </summary>
        /// <param name="e"></param>
        /// <param name="bounds"></param>
        /// <param name="appearance"></param>
        /// <param name="onMouse"></param>
        /// <param name="hasFocus"></param>
        /// <param name="withImage"></param>
        internal static void PaintBackground(DxBufferedGraphicPaintArgs e, Rectangle? bounds, IDataFormBackgroundAppearance appearance, bool onMouse, bool hasFocus, bool withImage)
        {
            if (appearance is null || !bounds.HasValue) return;

            // Pozadí:
            using (var brush = DxDataForm.CreateBrushForAppearance(bounds.Value, appearance, onMouse, hasFocus))
            {
                if (brush != null)
                {
                    e.Graphics.FillRectangle(brush, bounds.Value);
                }
            }

            // Image:
            if (withImage)
                DxComponent.PaintImage(e.Graphics, appearance.BackImageName, bounds.Value, appearance.BackImageFill, appearance.BackImageAlignment);
        }
        /// <summary>
        /// Vykreslí pozadí obsahu
        /// </summary>
        /// <param name="e"></param>
        /// <param name="bounds"></param>
        /// <param name="appearance"></param>
        internal static void PaintImage(DxBufferedGraphicPaintArgs e, Rectangle? bounds, IDataFormBackgroundAppearance appearance)
        {
            if (appearance is null || !bounds.HasValue) return;
            DxComponent.PaintImage(e.Graphics, appearance.BackImageName, bounds.Value, appearance.BackImageFill, appearance.BackImageAlignment);
        }
        /// <summary>
        /// Vykreslí rámeček v daném prostoru s danou šířkou v daném vzhledu
        /// </summary>
        /// <param name="e"></param>
        /// <param name="bounds"></param>
        /// <param name="sizes"></param>
        /// <param name="appearance"></param>
        /// <param name="onMouse"></param>
        /// <param name="hasFocus"></param>
        internal static void PaintFrame(DxBufferedGraphicPaintArgs e, Rectangle? bounds, int? sizes, IDataFormBackgroundAppearance appearance, bool onMouse, bool hasFocus)
        {
            if (!bounds.HasValue || !sizes.HasValue || sizes.Value <= 0 || appearance is null) return;
            using (var brush = CreateBrushForAppearance(bounds.Value, appearance, onMouse, hasFocus))
            {
                if (brush != null)
                {
                    using (var path = CreateGraphicsFrame(bounds.Value, sizes.Value))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
            }
        }
        /// <summary>
        /// Vykreslí daný text ikonu do daného prostoru v daném zarovnání
        /// </summary>
        /// <param name="e"></param>
        /// <param name="bounds"></param>
        /// <param name="appearance"></param>
        /// <param name="text"></param>
        internal static void PaintText(DxBufferedGraphicPaintArgs e, Rectangle? bounds, IDataFormColumnAppearance appearance, string text)
        {
            if (!bounds.HasValue || String.IsNullOrEmpty(text)) return;
            var font = Control.DefaultFont;
            var size = e.Graphics.MeasureString(text, font);
            var textBounds = size.AlignTo(bounds.Value, appearance.ContentAlignment ?? ContentAlignment.MiddleLeft);
            e.Graphics.DrawString(text, font, Brushes.Black, textBounds);
        }

        /// <summary>
        /// Vrátí barvu explicitně dodanou <paramref name="color"/>, 
        /// anebo vyhledá styl daného jména <paramref name="styleName"/> a z něj načte a vrátí barvu daného typu <paramref name="source"/>.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="styleName"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        internal static Color? GetCurrentColor(Color? color, string styleName, ColorSourceType source)
        {
            if (color.HasValue) return color;
            if (styleName == null) return null;
            var styleInfo = DxComponent.GetStyleInfo(styleName);
            if (styleInfo is null) return null;
            switch (source)
            {
                case ColorSourceType.LabelBack: return styleInfo.LabelBgColor;
                case ColorSourceType.LabelText: return styleInfo.LabelColor;
                case ColorSourceType.ControlBack: return styleInfo.AttributeColor;
                case ColorSourceType.ControlText: return styleInfo.AttributeColor;
            }
            return null;
        }
        /// <summary>
        /// Zdroje barev ze stylu
        /// </summary>
        internal enum ColorSourceType { None, LabelBack, LabelText, ControlBack, ControlText }
        #endregion
        #region Podpora pro vykreslování - controly a ImageCache
        #region Controly - zde jsou uloženy jednotlivé nativní controly, které se používají k editaci a k zobrazení
        /// <summary>
        /// Inicializuje data fyzických controlů (<see cref="_ControlsSets"/>, <see cref="_DxSuperToolTip"/>)
        /// </summary>
        private void InitializeUserControls()
        {
            _ControlsSets = new Dictionary<DataFormColumnType, DxDataFormControlSet>();
            _DxSuperToolTip = new DxSuperToolTip() { AcceptTitleOnlyAsValid = false };
        }
        /// <summary>
        /// Uvolní z paměti veškerá data fyzických controlů
        /// </summary>
        private void DisposeUserControls()
        {
            if (_ControlsSets == null) return;
            foreach (DxDataFormControlSet controlSet in _ControlsSets.Values)
                controlSet.Dispose();
            _ControlsSets.Clear();
        }
        /// <summary>
        /// Vrátí instanci reprezentující jeden typ controlu : <see cref="DataFormColumnType"/>.
        /// Vždy vrátí objekt, nikdy null.
        /// Využívá cache.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal DxDataFormControlSet GetControlSet(DxDataFormColumn item)
        {
            var dataFormControls = _ControlsSets;

            DxDataFormControlSet controlSet;
            DataFormColumnType itemType = item.ItemType;
            if (!dataFormControls.TryGetValue(itemType, out controlSet))
            {
                controlSet = new DxDataFormControlSet(this, itemType);
                dataFormControls.Add(itemType, controlSet);
            }
            return controlSet;
        }
        /// <summary>
        /// Vrátí control pro daný prvek a režim použití
        /// </summary>
        /// <param name="item"></param>
        /// <param name="mode"></param>
        /// <param name="parent">Parent, do něhož má být control umístěn. Pokud režim <paramref name="mode"/> je <see cref="DxDataFormControlUseMode.Draw"/>, pak parent smí být null.</param>
        /// <returns></returns>
        internal Control GetControl(DxDataFormColumn item, DxDataFormControlUseMode mode, Control parent)
        {
            DxDataFormControlSet controlSet = GetControlSet(item);
            Control drawControl = controlSet.GetControlForMode(item, mode, parent);
            return drawControl;
        }
        /// <summary>
        /// Sdílený objekt ToolTipu do všech controlů
        /// </summary>
        internal DxSuperToolTip DxSuperToolTip { get { return this._DxSuperToolTip; } }
        /// <summary>Sdílený objekt ToolTipu do všech controlů</summary>
        private DxSuperToolTip _DxSuperToolTip;
        /// <summary>Paměť dosud používaných typů controlů</summary>
        private Dictionary<DataFormColumnType, DxDataFormControlSet> _ControlsSets;
        #endregion
        #region Bitmap cache
        /// <summary>
        /// Inicializace subsystému ImageCache
        /// </summary>
        private void InitializeImageCache()
        {
            ImageCachePaintId = 0L;
            ImageCacheNextCleanId = _CACHECLEAN_OLD_LOOPS + 1;         // První pokus o úklid proběhne po tomto počtu PaintLoop, protože i kdyby bylo potřeba uklidit staré položky dříve, tak stejně nemůže zahodit starší položky - žádné by nevyhovovaly...
        }
        /// <summary>
        /// Dispose subsystému ImageCache
        /// </summary>
        private void DisposeImageCache()
        {
            ImageCacheInvalidate();
        }
        /// <summary>
        /// Zruší veškerý obsah z cache uložených Image <see cref="ImageCache"/>, kde jsou uloženy obrázky pro jednotlivé ne-aktivní controly...
        /// Je nutno volat po změně skinu nebo Zoomu.
        /// </summary>
        internal void ImageCacheInvalidate()
        {
            if (ImageCache == null) return;
            ImageCache.Values.ForEachExec(i => i.Dispose());
            ImageCache.Clear();
            ImageCache = null;
        }
        /// <summary>
        /// Má být voláno před zahájením vykreslení jednoho snímku.
        /// </summary>
        internal void ImagePaintStart()
        {
            ImageCachePaintId++;
            ImageCacheCountOld = ImageCacheCount;
        }
        /// <summary>
        /// Má být voláno po dokončení vykreslení jednoho snímku.
        /// Zajistí úklid nepotřebných dat v cache.
        /// </summary>
        internal void ImagePaintDone()
        {
            if (ImageCacheCount > ImageCacheCountOld || _NextCleanLiable)
                CleanImageCache();
        }
        /// <summary>
        /// Najde a vrátí <see cref="Image"/> pro obsah daného prvku.
        /// Obrázek hledá nejprve v cache, a pokud tam není pak jej vygeneruje a do cache uloží.
        /// <para/>
        /// POZOR: výstupem této metody je vždy new instance Image, a volající ji musí použít v using { } patternu, jinak zlikviduje paměť.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="graphics"></param>
        /// <returns></returns>
        internal Image CreateImage(DxDataFormColumn item, Graphics graphics)
        {
            if (ImageCache == null) ImageCache = new Dictionary<string, ImageCacheItem>();

            DxDataFormControlSet controlSet = GetControlSet(item);
            string key = controlSet.GetKeyToCache(item);
            if (key == null) return null;

            ImageCacheItem imageInfo = null;
            if (ImageCache.TryGetValue(key, out imageInfo))
            {   // Pokud mám Image již uloženu v Cache, je tam uložena jako byte[], a tady z ní vygenerujeme new Image a vrátíme, uživatel ji Disposuje sám:
                imageInfo.AddHit(ImageCachePaintId);
                return imageInfo.CreateImage();
            }
            else
            {   // Image v cache nemáme, takže nyní vytvoříme skutečný Image s pomocí controlu, obsah Image uložíme jako byte[] do cache, a uživateli vrátíme ten živý obraz:
                // Tímto postupem šetřím čas, protože Image použiju jak pro uložení do Cache, tak pro vykreslení do grafiky v controlu:
                Image image = CreateBitmapForItem(item, graphics);
                lock (ImageCache)
                {
                    if (ImageCache.TryGetValue(key, out imageInfo))
                    {
                        imageInfo.AddHit(ImageCachePaintId);
                    }
                    else
                    {   // Do cache přidám i image == null, tím ušetřím opakované vytváření / testování obrázku.
                        // Pro přidávání aplikuji lock(), i když tedy tahle činnost má probíhat jen v jednom threadu = GUI:
                        imageInfo = new ImageCacheItem(image, ImageCachePaintId);
                        ImageCache.Add(key, imageInfo);
                    }
                }
                return image;
            }
        }
        /// <summary>
        /// Fyzicky vytvoří a vrátí Image pro daný control
        /// </summary>
        /// <param name="item"></param>
        /// <param name="graphics"></param>
        /// <returns></returns>
        private Image CreateBitmapForItem(DxDataFormColumn item, Graphics graphics)
        {
            /*   Časomíra:

               1. Vykreslení bitmapy z paměti do Graphics                    10 mikrosekund
               2. Nastavení souřadnic (Bounds) do controlu                  300 mikrosekund
               3. Vložení textu (Text) do controlu                          150 mikrosekund
               4. Zrušení Selection v TextBoxu                                5 mikrosekund
               5. Vykreslení controlu do bitmapy                            480 mikrosekund

           */

            DxDataFormControlSet controlSet = GetControlSet(item);
            Control drawControl = controlSet.GetControlForDraw(item);

            int w = drawControl.Width;
            int h = drawControl.Height;
            if (w <= 0 || h <= 0) return null;

            Bitmap image = new Bitmap(w, h, graphics);
            drawControl.DrawToBitmap(image, new Rectangle(0, 0, w, h));

            return image;
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

            long paintLoop = ImageCachePaintId;
            if (!_NextCleanLiable && paintLoop < ImageCacheNextCleanId)   // Pokud není úklid povinný, a velikost jsme kontrolovali nedávno, nebudu to ještě řešit.
                return;

            long currentCacheSize = ImageCacheSize;
            if (currentCacheSize < _CACHESIZE_MIN)                   // Pokud mám v cache málo dat (pod 4MB), nebudeme uklízet.
            {
                _NextCleanLiable = false;
                ImageCacheNextCleanId = paintLoop + _CACHECLEAN_AFTER_LOOPS;
                if (LogActive) DxComponent.LogAddLineTime($"DxDataForm.CleanCache(): CacheSize={currentCacheSize}B; úklid není nutný. Čas akce: {DxComponent.LogTokenTimeMilisec}", startTime);
                return;
            }

            // Budu pracovat jen s těmi prvky, které nebyly dlouho použity:
            long lastLoop = paintLoop - _CACHECLEAN_OLD_LOOPS;
            var items = ImageCache.Where(kvp => kvp.Value.LastPaintLoop <= lastLoop).ToList();
            if (items.Count == 0)                                    // Pokud všechny prvky pravidelně používám, nebudu je zahazovat.
            {
                _NextCleanLiable = false;
                ImageCacheNextCleanId = paintLoop + _CACHECLEAN_AFTER_LOOPS_SMALL;
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
                ImageCacheNextCleanId = paintLoop + _CACHECLEAN_AFTER_LOOPS;
            }
            else                                                     // Tento úklid byl potřebný (z hlediska času nebo velikosti paměti), ale nedostali jsme se pod _CACHESIZE_MIN:
            {
                _NextCleanLiable = true;                             // Příště budeme volat úklid povinně!
                if (cleanedCacheSize < currentCacheSize)             // Sice jsme neuklidili pod minimum, ale něco jsme uklidili: příští kontrolu zaplánujeme o něco dříve:
                    ImageCacheNextCleanId = paintLoop + _CACHECLEAN_AFTER_LOOPS_SMALL;
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
        /// Po kterém vykreslení <see cref="ImageCachePaintId"/> budeme dělat další úklid
        /// </summary>
        private long ImageCacheNextCleanId;
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
        /// Počet prvků v cache
        /// </summary>
        private int ImageCacheCount { get { return ImageCache?.Count ?? 0; } }
        /// <summary>
        /// Počet prvků v cache na začátku jednoho kreslení = v době volání metody <see cref="ImagePaintStart"/>
        /// </summary>
        private int ImageCacheCountOld;
        /// <summary>
        /// Sumární velikost dat v cache
        /// </summary>
        private long ImageCacheSize { get { return (ImageCache != null ? ImageCache.Values.Select(i => i.Length).Sum() : 0L); } }
        /// <summary>
        /// Pořadí kreslení, slouží pro řízení obsahu cache a jejího úklidu
        /// </summary>
        private long ImageCachePaintId;
        /// <summary>
        /// Cache obrázků controlů
        /// </summary>
        private Dictionary<string, ImageCacheItem> ImageCache;
        /// <summary>
        /// Jeden záznam v cache
        /// </summary>
        private class ImageCacheItem : IDisposable
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="image"></param>
            /// <param name="paintLoop"></param>
            public ImageCacheItem(Image image, long paintLoop)
            {
                if (image != null)
                {
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                    {
                        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);    // PNG: čas v testu 20-24ms, spotřeba paměti 0.5MB.    BMP: čas 18-20ms, pamět 5MB.    TIFF: čas 50ms, paměť 1.5MB
                        _ImageContent = ms.ToArray();
                    }
                }
                else
                {
                    _ImageContent = null;
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
                if (_ImageContent == null) return null;

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(_ImageContent))
                    return Image.FromStream(ms);
            }
            /// <summary>
            /// Počet byte uložených jako Image v této položce cache
            /// </summary>
            public long Length { get { return _ImageContent?.Length ?? 0; } }
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
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                _ImageContent = null;
            }
        }
        #endregion
        #endregion
        #region Appearance
        /// <summary>
        /// Vzhled. Autoinicializační property. Nikdy není null. Setování null nastaví defaultní vzhled.
        /// </summary>
        public DxDataFormAppearance DataFormAppearance 
        { 
            get { if (_DataFormAppearance == null) _DataFormAppearance = new DxDataFormAppearance(); return _DataFormAppearance; }
            set { _DataFormAppearance = value; }
        }
        private DxDataFormAppearance _DataFormAppearance;
        /// <summary>
        /// Aktivace barevných indikátoru "OnDemand"
        /// </summary>
        public bool ItemIndicatorsVisible { get; set; }
        #endregion
    }
    #region class DxDataFormAppearance
    /// <summary>
    /// Definice vzhledu DataFormu
    /// </summary>
    public class DxDataFormAppearance
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxDataFormAppearance()
        {
            OnMouseIndicatorColor = Color.LightCoral;
            WithFocusIndicatorColor = Color.GreenYellow;
            CorrectIndicatorColor = Color.LightGreen;
            WarningIndicatorColor = Color.Orange;
            ErrorIndicatorColor = Color.DarkRed;
            RequiredIndicatorColor = Color.BlueViolet;
        }
        /// <summary>
        /// Barva indikátoru OnMouse
        /// </summary>
        public Color OnMouseIndicatorColor { get; set; }
        /// <summary>
        /// Barva indikátoru WithFocus
        /// </summary>
        public Color WithFocusIndicatorColor { get; set; }
        /// <summary>
        /// Barva indikátoru Correct
        /// </summary>
        public Color CorrectIndicatorColor { get; set; }
        /// <summary>
        /// Barva indikátoru Warning
        /// </summary>
        public Color WarningIndicatorColor { get; set; }
        /// <summary>
        /// Barva indikátoru Error
        /// </summary>
        public Color ErrorIndicatorColor { get; set; }
        /// <summary>
        /// Barva indikátoru Required
        /// </summary>
        public Color RequiredIndicatorColor { get; set; }
    }
    #endregion
}

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    #region class DxDataFormData : Data, která budou zobrazena v dataformu (ve formě DataTable, Array, List, Record)
    /// <summary>
    /// Data, která budou zobrazena v dataformu (ve formě DataTable, Array, List, Record)
    /// </summary>
    internal class DxDataFormData
    {
        #region Konstruktor a privátní rovina obecného zdroje dat
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataForm"></param>
        public DxDataFormData(DxDataForm dataForm)
        {
            _DataForm = dataForm;
            _Source = null;
            _CurrentSourceType = SourceType.None;
        }
        /// <summary>Vlastník - <see cref="DxDataForm"/></summary>
        private DxDataForm _DataForm;
        /// <summary>
        /// Datový zdroj.
        /// Může to být <see cref="System.Data.DataTable"/>, nebo 
        /// </summary>
        public object Source { get { return _Source; } set { _SetSource(value); } }
        /// <summary>
        /// Vloží zdroj, detekuje jeho druh, provede typovou inicializaci
        /// </summary>
        /// <param name="source"></param>
        private void _SetSource(object source)
        {
            if (source == null) _SetSourceNull();
            else if (source is System.Data.DataTable dataTable) _SetSourceDataTable(dataTable);
            else if (source is Array array) _SetSourceArray(array);
            else if (source is IList<object> list) _SetSourceList(list);

            _DataForm.Refresh(RefreshParts.RecalculateContentTotalSize | RefreshParts.InvalidateControl);
        }
        /// <summary>
        /// Nullování zdroje (odpojení)
        /// </summary>
        private void _SetSourceNull()
        {
            _Source = null;
            _SourceDataTable = null;
            _SourceArray = null;
            _SourceList = null;
            _SourceRecord = null;
            _CurrentSourceType = SourceType.None;
        }
        /// <summary>
        /// Obecný zdroj dat, netypová reference
        /// </summary>
        private object _Source;
        /// <summary>
        /// Aktuální typ dat
        /// </summary>
        private SourceType _CurrentSourceType;
        /// <summary>
        /// Typ datového zdroje
        /// </summary>
        private enum SourceType { None, DataTable, Array, List, Record }
        #endregion
        #region Public přístup - nezávislý na typu dat
        /// <summary>
        /// Počet řádků s daty. Pokud nejsou vložena data, vrací 0.
        /// </summary>
        /// <param name="partId">Identifikátor části. Různé části mohou mít různé řádkové filtry, a pak mají různé počty řádků.</param>
        internal int GetRowCount(DxDataFormPartId partId)
        {
            switch (_CurrentSourceType)
            {
                case SourceType.None: return 0;
                case SourceType.DataTable: return _GetRowCountDataTable(partId);
                case SourceType.Array: return _GetRowCountArray(partId);
                case SourceType.List: return _GetRowCountList(partId);
                case SourceType.Record: return _GetRowCountRecord(partId);
            }
            return 0;
        }
        /// <summary>
        /// Vrátí text pro sloupec daného řádku
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public string GetText(int rowIndex, IDataFormColumn column)
        {
            switch (_CurrentSourceType)
            {
                case SourceType.None: return null;
                case SourceType.DataTable: return _GetTextDataTable(rowIndex, column);
                case SourceType.Array: return _GetTextArray(rowIndex, column);
                case SourceType.List: return _GetTextList(rowIndex, column);
                case SourceType.Record: return _GetTextRecord(rowIndex, column);
            }
            return null;
        }
        /// <summary>
        /// Metoda má vrátit pole, obsahující ID řádků k zobrazení, pro řádky na viditelných pozicích First až Last (včetně).
        /// Správce dat by měl znát svoje data (řádky) včetně jejich řazení, kdy každý řádek má svoji jednoznačnou a kontinuální vizuální pozici v poli viditelných řádků, počínaje od 0.
        /// V rámci tohoto pole by měl dokázat najít řádky na daných pozicích, a zde vrátí pole jejich RowId ve správném pořadí, jak budou zobrazeny.
        /// </summary>
        /// <param name="partId">Identifikátor části. Různé části mohou mít různé řádkové filtry, a pak mají různé počty řádků.</param>
        /// <param name="rowIndexFirst"></param>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        internal int[] GetVisibleRowsId(DxDataFormPartId partId, int rowIndexFirst, int rowCount)
        {
            // Kontroly, zarovnání, zkratka pro chybné zadání nebo pro nula záznamů:
            if (rowIndexFirst < 0) rowIndexFirst = 0;
            if (rowCount <= 0 || rowIndexFirst >= this.GetRowCount(partId)) return new int[0];

            // Pokud by neexistovalo setřídění řádků, a pokud by RowId byly kontinuálně od 0 nahoru, pak by věc byla jednoduchá
            //  = vrátilo by se pole obsahující posloupnost čísel { rowIndexFirst, rowIndexFirst+1, ..., rowIndexLast }.
            // Ale svět není jednoduchý, RowId mohou obsahovat čísla záznamů / položek (objekty), takže musíme dovnitř:
            switch (_CurrentSourceType)
            {
                case SourceType.None: return null;
                case SourceType.DataTable: return _GetVisibleRowsIdDataTable(rowIndexFirst, rowCount);
                case SourceType.Array: return _GetVisibleRowsIdArray(rowIndexFirst, rowCount);
                case SourceType.List: return _GetVisibleRowsIdList(rowIndexFirst, rowCount);
                case SourceType.Record: return _GetVisibleRowsIdRecord(rowIndexFirst, rowCount);
            }
            return null;
        }
        #endregion
        #region Práce s konkrétním typem - DataTable
        /// <summary>
        /// Vloží datový zdroj typu DataTable
        /// </summary>
        /// <param name="dataTable"></param>
        private void _SetSourceDataTable(System.Data.DataTable dataTable)
        {
            _SetSourceNull();
            _Source = dataTable;
            _SourceDataTable = dataTable;
            _CurrentSourceType = SourceType.DataTable;
        }
        /// <summary>
        /// Vrátí počet řádků DataTable
        /// </summary>
        /// <returns></returns>
        private int _GetRowCountDataTable(DxDataFormPartId partId) { return (_SourceDataTable?.Rows.Count ?? 0); }
        /// <summary>
        /// Vrátí text prvku ze zdroje typu DataTable
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private string _GetTextDataTable(int rowIndex, IDataFormColumn column)
        {
            return null;
        }
        /// <summary>
        /// Vrátí pole obsahující RowId pro řádky, které mají být zobrazeny na daných vizuálních pozicích, pro DataTable
        /// </summary>
        /// <param name="rowIndexFirst"></param>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        private int[] _GetVisibleRowsIdDataTable(int rowIndexFirst, int rowCount)
        {
            List<int> result = new List<int>();
            int rowIndex = rowIndexFirst;
            while (result.Count < rowCount)
                result.Add(rowIndex++);
            return result.ToArray();
        }
        /// <summary>
        /// Datový zdroj typu DataTable
        /// </summary>
        private System.Data.DataTable _SourceDataTable;
        #endregion
        #region Práce s konkrétním typem - Array
        /// <summary>
        /// Vloží datový zdroj typu Array
        /// </summary>
        /// <param name="array"></param>
        private void _SetSourceArray(Array array)
        {
            _SetSourceNull();
            _Source = array;
            _SourceArray = array;
            _CurrentSourceType = SourceType.Array;
        }
        /// <summary>
        /// Vrátí počet řádků Array
        /// </summary>
        /// <returns></returns>
        private int _GetRowCountArray(DxDataFormPartId partId) { return (_SourceArray?.Length ?? 0); }
        /// <summary>
        /// Vrátí text prvku ze zdroje typu Array
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private string _GetTextArray(int rowIndex, IDataFormColumn column)
        {
            return null;
        }
        /// <summary>
        /// Vrátí pole obsahující RowId pro řádky, které mají být zobrazeny na daných vizuálních pozicích, pro Array
        /// </summary>
        /// <param name="rowIndexFirst"></param>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        private int[] _GetVisibleRowsIdArray(int rowIndexFirst, int rowCount)
        {
            return null;
        }
        /// <summary>
        /// Datový zdroj typu Array
        /// </summary>
        private Array _SourceArray;
        #endregion
        #region Práce s konkrétním typem - List
        /// <summary>
        /// Vloží datový zdroj typu List
        /// </summary>
        /// <param name="list"></param>
        private void _SetSourceList(IList<object> list)
        {
            _SetSourceNull();
            _Source = list;
            _SourceList = list;
            _CurrentSourceType = SourceType.List;
        }
        /// <summary>
        /// Vrátí počet řádků List
        /// </summary>
        /// <returns></returns>
        private int _GetRowCountList(DxDataFormPartId partId) { return (_SourceList?.Count ?? 0); }
        /// <summary>
        /// Vrátí text prvku ze zdroje typu List
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private string _GetTextList(int rowIndex, IDataFormColumn column)
        {
            return null;
        }
        /// <summary>
        /// Vrátí pole obsahující RowId pro řádky, které mají být zobrazeny na daných vizuálních pozicích, pro List
        /// </summary>
        /// <param name="rowIndexFirst"></param>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        private int[] _GetVisibleRowsIdList(int rowIndexFirst, int rowCount)
        {
            return null;
        }
        /// <summary>
        /// Datový zdroj typu List
        /// </summary>
        private IList<object> _SourceList;
        #endregion
        #region Práce s konkrétním typem - Record
        /// <summary>
        /// Vrátí počet řádků Record
        /// </summary>
        /// <returns></returns>
        private int _GetRowCountRecord(DxDataFormPartId partId) { return (_SourceArray?.Length ?? 0); }
        /// <summary>
        /// Vrátí text prvku ze zdroje typu Record
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private string _GetTextRecord(int rowIndex, IDataFormColumn column)
        {
            return null;
        }
        /// <summary>
        /// Vrátí pole obsahující RowId pro řádky, které mají být zobrazeny na daných vizuálních pozicích, pro Record
        /// </summary>
        /// <param name="rowIndexFirst"></param>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        private int[] _GetVisibleRowsIdRecord(int rowIndexFirst, int rowCount)
        {
            return null;
        }
        /// <summary>
        /// Datový zdroj typu Record
        /// </summary>
        private object _SourceRecord;
        #endregion
    }
    #endregion
    #region class DxDataFormPanel : Jeden panel dataformu: reprezentuje základní panel, hostuje v sobě dva ScrollBary a ContentPanel
    /// <summary>
    /// Jeden panel dataformu: reprezentuje základní panel zobrazující jednu plochu s daty.
    /// Je umístěn buď přímo v <see cref="DxDataForm"/>, pak jde o DataForm bez záložek;
    /// anebo je umístěn na stránce záložkovníku <see cref="DxTabPane"/>, pak jde o vícezáložkový DataForm.
    /// Tuto volbu řídí <see cref="DxDataForm"/>. 
    /// <para/>
    /// Jeden panel <see cref="DxDataFormPanel"/> v sobě hostuje přinejmenším jeden nebo více částí <see cref="DxDataFormPart"/>.
    /// Každá jedna část <see cref="DxDataFormPart"/> v sobě zobrazuje fyzická data, může mít / nemusí mít ScrollBary a Headery.
    /// Tyto části mohou být vzájemně spřažené (jeden svislý Scrollbar zobrazený úplně vpravo může ovládat více částí umístěných vedle sebe = vlevo).
    /// Přidávání a odebírání částí řídí <see cref="DxDataFormPanel"/>, stejně tak mezi ně vkládá ScrollBary a řídí jejich velikost.
    /// <para/>
    /// Panel <see cref="DxDataFormPanel"/> je zobrazován v <see cref="DxDataForm"/> buď v celé jeho ploše (to když DataForm obsahuje jen jednu stránku),
    /// anebo je v <see cref="DxDataForm"/> zobrazen záložkovník <see cref="DxTabPane"/>, a v každé záložce je zobrazován zdejší panel <see cref="DxDataFormPanel"/>,
    /// obsahuje pak jen grupy jedné konkrétní stránky.
    /// Toto řídí třída <see cref="DxDataForm"/> podle dodaných stránek a podle dynamického layoutu.
    /// </summary>
    internal class DxDataFormPanel : DxPanelControl
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

            InitializeParts();
            InitializeGroups();
        }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DisposeGroups();
            DisposeParts();

            base.Dispose(disposing);

            _DataForm = null;
        }
        /// <summary>Vlastník - <see cref="DxDataForm"/>, ale nemusí to být Parent!</summary>
        private DxDataForm _DataForm;
        /// <summary>
        /// Vlastník - <see cref="DxDataForm"/>
        /// </summary>
        public DxDataForm DataForm { get { return this._DataForm; } }
        /// <summary>
        /// Refresh celého panelu v daném režimu
        /// </summary>
        public void Refresh(RefreshParts refreshParts)
        {
            base.Refresh();
            this._Parts.ForEachExec(p => p.Refresh(refreshParts));
        }
        /// <summary>
        /// Refresh celého panelu
        /// </summary>
        public override void Refresh()
        {
            this.Refresh(RefreshParts.InvalidateControl);
        }
        #endregion
        #region Public vlastnosti
        /// <summary>
        /// Aktuální velikost viditelného prostoru pro DataForm, po odečtení prostoru pro ScrollBary (bez ohledu na jejich aktuální viditelnost)
        /// </summary>
        internal Size VisibleContentSize 
        {
            get 
            {
                Size size = this.ClientSize;
                int hScrollSize = _RootPart.DefaultHorizontalScrollBarHeight;
                int vScrollSize = _RootPart.DefaultVerticalScrollBarWidth;
                return new Size(size.Width - vScrollSize, size.Height - hScrollSize);
            }
        }
        /// <summary>
        /// Stav panelu - pozice scrollbarů atd. 
        /// Podporuje přepínání záložek - vizuálně jiný obsah, ale promítaný prostřednictvím jedné instance <see cref="DxDataFormPanel"/>.
        /// Při čtení bude vrácen objekt obsahující aktuální stav.
        /// Při zápisu budou hodnoty z vkládaného objektu aplikovány do panelu
        /// </summary>
        internal DxDataFormState State
        {
            get
            {
#warning TODO musí být navázáno na pole _Parts!
                if (_State == null)
                    _State = new DxDataFormState();
                return _State;
            }
            set
            {
                _State = value;
            }
        }
        private DxDataFormState _State;
        #endregion
        #region Parts - jednotlivé části DataFormu (splitterem oddělené bloky řádků nebo sloupců), výchozí je jedna část přes celý prostor panelu
        /// <summary>
        /// Inicializace jednotlivých částí DataFormu 
        /// </summary>
        private void InitializeParts()
        {
            _Parts = new List<DxDataFormPart>();
            AddPart(0, 0);
        }
        /// <summary>
        /// Dispose jednotlivých částí DataFormu 
        /// </summary>
        private void DisposeParts()
        { }
        private void AddPart(int partXId, int partYId)
        {
            DxDataFormPartId partId = new DxDataFormPartId(partXId, partYId);
            _RootPart = new DxDataFormPart(this, partId);
            _Parts.Add(_RootPart);
            _RootPart.Dock = ((_Parts.Count == 1) ? DockStyle.Fill : DockStyle.None);
            this.Controls.Add(_RootPart);
        }
        /// <summary>
        /// Pole jednotlivých částí
        /// </summary>
        private DxDataFormPart[] Parts { get { return _Parts.ToArray(); } }
        /// <summary>
        /// Kořenový Part, existuje vždy
        /// </summary>
        private DxDataFormPart _RootPart;
        /// <summary>Pole jednotlivých částí</summary>
        private List<DxDataFormPart> _Parts;
        #endregion
        #region Grupy a jejich Columns, jejich vkládání do prvků DxDataFormPart
        /// <summary>
        /// Zobrazované grupy a jejich prvky.
        /// Po vložení této definice neproběhne automaticky refresh controlu, je tedy vhodné následně volat <see cref="Refresh()"/>.
        /// </summary>
        public List<DxDataFormGroup> Groups { get { return _Groups; } set { _SetGroups(value); } }
        /// <summary>
        /// Inicializuje pole prvků
        /// </summary>
        private void InitializeGroups()
        {
            _Groups = new List<DxDataFormGroup>();
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
        /// Zahodí všechny položky o grupách a prvcích z this instance
        /// </summary>
        private void DisposeGroups()
        {
            if (_Groups != null)
            {
                _Groups.Clear();
                _Groups = null;
            }
        }
        /// <summary>
        /// Počet aktuálně viditelných prvků ve všech částech dohromady
        /// </summary>
        internal int? VisibleCellsCount { get { return _Parts.Select(p => p.VisibleCellsCount).Sum(); } }
        /// <summary>
        /// Pole skupin, které v tomto panelu zobrazujeme v jeho jednotlivých částech <see cref="DxDataFormGroup"/>
        /// </summary>
        private List<DxDataFormGroup> _Groups;
        #endregion
    }
    #endregion
    #region class DxDataFormPart : Jedna oddělená a samostatná skupina řádků a sloupců v rámci DataFormu
    /// <summary>
    /// <see cref="DxDataFormPart"/> : Jedna oddělená a samostatná skupina řádků/sloupců v rámci panelu DataFormu <see cref="DxDataFormPanel"/>.
    /// Prostor DataFormu (přesněji <see cref="DxDataFormPanel"/>) může být rozdělen na více sousedících částí = <see cref="DxDataFormPart"/>,
    /// které zobrazují tatáž data, ale jsou nascrollovaná na jiná místa, nebo mohou mít odlišné filtry a zobrazovat tedy jiné podmnožiny řádků.
    /// <para/>
    /// Toto rozčlenění povoluje a řídí <see cref="DxDataFormPanel"/> jako fyzický Parent těchto částí, pokyny k rozdělení dostává od hlavního <see cref="DxDataForm"/>.
    /// K interaktivní změně dává uživateli k dispozici vhodné Splittery.
    /// Rozdělení provádí uživatel pomocí "tahacího" tlačítka vpravo nahoře a následného zobrazení splitteru.
    /// Dostupnost Scrollbarů v jednotlivých částech v rámci <see cref="DxDataFormPanel"/> řídí <see cref="DxDataFormPanel"/>; 
    /// scrollbary jsou dostupné vždy v té krajní části v daném směru = vpravo svislý a dole vodorovný.
    /// Synchronizaci sousedních částí, které nemají svůj vlastní odpovídající scrollbar, zajišťuje <see cref="DxDataFormPanel"/>.
    /// Podkladový ScrollPanel <see cref="DxScrollableContent"/> dovoluje nastavit okraje kolem scrollovaného obsahu, 
    /// tyto okraje jsou využívány pro zobrazení "fixních" částí (vše okolo Rows) = ColumnHeader, RowFilter, SummaryRow, RowHeader.
    /// <para/>
    /// Typicky Master Dataform (nazývaný v Greenu "FreeForm") má pouze jednu část, která nezobrazuje ani ColumnHeaders ani RowHeaders, a ani nenabízí rozdělovací Splittery.
    /// DataForm používaný pro položky (nazývaný v Greenu "EditBrowse") toto rozčlenění umožňuje.
    /// Výhledový BrowseGrid rovněž.
    /// <para/>
    /// Každá jedna skupina se nazývá Part = <see cref="DxDataFormPart"/>, a skládá se z částí: RowHeader, ColumnHeader, RowFilter, Rows, SummaryRows a Footer.
    /// Tyto části jsou jednotlivě volitelné - odlišně pro první skupinu, pro vnitřní skupiny a pro skupinu poslední.
    /// Části Header, RowFilter jsou fixní k hornímu okraji a nescrollují;
    /// Části Rows, SummaryRows scrollují uprostřed;
    /// Část Footer je fixní k dolnímu okraji a nescrolluje.
    /// </summary>
    internal class DxDataFormPart : DxScrollableContent
    {
        #region Konstruktor, vlastník, prvky, identifikátory
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataPanel"></param>
        /// <param name="partId"></param>
        public DxDataFormPart(DxDataFormPanel dataPanel, DxDataFormPartId partId)
        {
            _DataPanel = dataPanel;
            _PartId = partId ?? new DxDataFormPartId();

            this.DoubleBuffered = true;
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;

            InitializeContentPanel();
            InitializeGroups();
            InitializePaint();
            InitializeInteractivity();
        }
        /// <summary>
        /// Dispose Part
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DisposeGroups();
            DisposeContentPanel();

            base.Dispose(disposing);

            _DataPanel = null;
        }
        /// <summary>Vlastník - <see cref="DxDataFormPanel"/></summary>
        private DxDataFormPanel _DataPanel;
        /// <summary>ID this části</summary>
        private DxDataFormPartId _PartId;
        /// <summary>
        /// Vlastník - <see cref="DxDataFormPanel"/>
        /// </summary>
        public DxDataFormPanel DataPanel { get { return this._DataPanel; } }
        /// <summary>
        /// Vlastník - <see cref="DxDataForm"/>
        /// </summary>
        public DxDataForm DataForm { get { return this._DataPanel.DataForm; } }
        /// <summary>
        /// Vzhled. Autoinicializační property. Nikdy není null. Setování null nastaví defaultní vzhled.
        /// </summary>
        public DxDataFormAppearance DataFormAppearance { get { return DataForm.DataFormAppearance; } }
        /// <summary>
        /// Vlastní data zobrazená v dataformu
        /// </summary>
        public DxDataFormData Data { get { return DataForm.Data; } }
        /// <summary>
        /// Identifikátor this části.
        /// S tímto ID se pak dotazuje parentů (dataformu a jeho dat) na řádky, sloupce atd.
        /// </summary>
        public DxDataFormPartId PartId { get { return _PartId; } }
        #endregion
        #region ContentPanel
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

            // TEST ONLY
            this.VScrollBarIndicators.AddIndicator(new Int32Range(50, 100), ScrollBarIndicatorType.BigCenter, Color.DarkRed);
            this.VScrollBarIndicators.AddIndicator(new Int32Range(500, 720), ScrollBarIndicatorType.FullSize | ScrollBarIndicatorType.OutsideGradientEffect, Color.DarkRed);
            this.VScrollBarIndicators.AddIndicator(new Int32Range(850, 1200), ScrollBarIndicatorType.ThirdNear, Color.DarkBlue);
            this.VScrollBarIndicators.AddIndicator(new Int32Range(1100, 1500), ScrollBarIndicatorType.HalfFar, Color.DarkGreen);

            for (int i = 20; i < 2000; i += 100)
                this.HScrollBarIndicators.AddIndicator(new Int32Range(i, i + 70), ScrollBarIndicatorType.FullSize | ScrollBarIndicatorType.InnerGradientEffect, Color.Red);
            this.HScrollBarIndicators.ColorAlphaArea = 160;
            this.HScrollBarIndicators.ColorAlphaThumb = 80;
            this.HScrollBarIndicators.Effect3DRatio = 0.75f;
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
        #region Grupy a jejich Items, viditelné grupy a viditelné itemy
        /// <summary>
        /// Zobrazované grupy a jejich prvky.
        /// Po vložení této definice neproběhne automaticky refresh controlu, je tedy vhodné následně volat <see cref="Refresh(RefreshParts)"/> 
        /// a předat v parametru požadavek <see cref="RefreshParts.InvalidateControl"/>.
        /// </summary>
        public List<DxDataFormGroup> Groups { get { return _GetGroups(); } set { _SetGroups(value); } }
        /// <summary>
        /// Inicializuje pole prvků
        /// </summary>
        private void InitializeGroups()
        {
            _VisibleItems = new List<DxDataFormColumn>();
        }
        /// <summary>
        /// Metoda vrátí grupy, které se aktuálně mají zobrazovat.
        /// Jsou to bud grupy zdejší, tedy explicitně zadané, anebo grupy společné pro celý panel <see cref="DxDataFormPanel.Groups"/>.
        /// </summary>
        /// <returns></returns>
        private List<DxDataFormGroup> _GetGroups()
        {
            return _Groups ?? _DataPanel.Groups;
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
        /// Metoda projde aktuální grupy a vypočítá velikost prostoru, do kterého se vejde souhrn jejich aktuálních souřadnic.
        /// Jde tedy o velikost potřebnou pro jeden řádek dat.
        /// Velikost je uložena do <see cref="_GroupsTotalSize"/>.
        /// </summary>
        /// <returns></returns>
        private void _CalculateGroupsTotalCurrentSize()
        {
            _GroupsTotalSize = Size.Empty;
            var groups = Groups;
            if (groups == null) return;
            Rectangle bounds = groups.Select(g => g.CurrentGroupBounds).SummaryVisibleRectangle() ?? Rectangle.Empty;
            _GroupsTotalSize = new Size(bounds.Right, bounds.Bottom);
        }
        /// <summary>
        /// Invaliduje aktuální rozměry všech grup v this objektu.
        /// Volá se typicky po změně zoomu nebo DPI.
        /// </summary>
        /// <returns></returns>
        private void _InvalidatGroupsCurrentBounds()
        {
            var groups = Groups;
            groups?.ForEachExec(g => g.InvalidateBounds());

            _LastCalcZoom = DxComponent.Zoom;
            _LastCalcDeviceDpi = this.CurrentDpi;
        }
        /// <summary>
        /// Připraví souhrn viditelných grup a prvků
        /// </summary>
        private void _PrepareVisibleGroupsItems()
        {
            var groups = Groups;
            Rectangle virtualBounds = this.ContentVirtualBounds;
            this._VisibleGroups = groups?.Where(g => g.IsVisibleInVirtualBounds(virtualBounds)).ToList();
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
        internal int? VisibleCellsCount { get { return _VisibleItems?.Count; } }

        private List<DxDataFormGroup> _Groups;
        private List<DxDataFormGroup> _VisibleGroups;
        private List<DxDataFormColumn> _VisibleItems;
        private Size _GroupsTotalSize;
        #endregion
        #region Řádky DxDataFormRow
        /// <summary>
        /// Metoda vypočítá velikost prostoru, do kterého se vejde souhrn všech řádků, 
        /// když pro každý jeden řádek bude třeba prostor <see cref="_GroupsTotalSize"/>.
        /// Jde tedy o velikost potřebnou pro celou tabulku dat.
        /// Velikost je uložena do <see cref="_GroupsTotalSize"/>.
        /// </summary>
        /// <returns></returns>
        private void _CalculateRowsTotalCurrentSize()
        {
            var innerSize = new Size(_GroupsTotalSize.Width, _RowCount * _RowHeight);
            var totalSize = innerSize.Add(0, 0);
            _RowsTotalSize = totalSize;
        }

        /// <summary>
        /// Připraví souhrn viditelných řádků
        /// </summary>
        private void _PrepareVisibleRows()
        {
            Rectangle virtualBounds = this.ContentVirtualBounds;               // Rozměr se vztahuje k celé ploše datové tabulky = všechny řádky od počátku prvního do konce posledního
            int rowCount = _RowCount;
            int rowHeight = _RowHeight;

            int rowLast = rowCount - 1;
            int rowVisibleFirst = (virtualBounds.Y / rowHeight).Align(0, rowLast);
            int rowVisibleLast = (virtualBounds.Bottom / rowHeight).Align(0, rowLast);

            _PrepareVisibleRows(rowVisibleFirst, (rowVisibleLast - rowVisibleFirst + 1));

            int visualBegin = (rowVisibleFirst * rowHeight) - virtualBounds.Y;
            _VisibleRows.ForEachExec(r => r.ApplyVisualPosition(ref visualBegin, rowHeight));
        }
        /// <summary>
        /// Zajistí, že pole <see cref="_VisibleRows"/> bude obsahovat ty řádky, které mají být viditelné, počínaje danou pozici, v daném počtu.
        /// Pole po ukončení této metody nebude null, může být prázdné.
        /// </summary>
        /// <param name="rowVisibleFirst"></param>
        /// <param name="rowVisibleCount"></param>
        private void _PrepareVisibleRows(int rowVisibleFirst, int rowVisibleCount)
        {
            // Zkratka: pokud máme platné pole, a máme v něm přinejmenším požadovaný počet prvků, a na první pozici pole je požadovaný řádek, pak nic není třeba řešit:
            List<DxDataFormRow> oldVisibleRows = _VisibleRows;
            if (oldVisibleRows != null && oldVisibleRows.Count == rowVisibleCount && (rowVisibleCount == 0 || (oldVisibleRows.Count > 0 && oldVisibleRows[0].RowIndex == rowVisibleFirst))) return;

            // Získám pole, obsahující RowId těch řádků, které mají být vidět na dané pozici (rowFirst) ++další, v daném počtu (rowCount):
            int[] visibleRowsId = Data.GetVisibleRowsId(this.PartId, rowVisibleFirst, rowVisibleCount);

            // Nejprve dosavadní řádky (pokud nejsou null): označím si v nich (hodnotou VisibleRow) ty řádky, které mají RowId odpovídající těm řádkům, které budou viditelné i nadále:
            //  - totiž, při posunu pole o několik málo picelů nám sice proběhne tato metoda, ale většina dosud viditelných řádků bude viditelná poté,
            //  a není třeba při každém miniposunu zahazovat kupu dat a generovat je znovu!
            Dictionary<int, DxDataFormRow> oldVisibleRowsDict = null;
            if (oldVisibleRows != null)
            {
                var rowsIdDict = visibleRowsId.CreateDictionary(i => i, true);
                oldVisibleRows.ForEachExec(r => r.VisibleRow = rowsIdDict.ContainsKey(r.RowId));  // Stávající objekty: Visible bude (true když mají být vidět i nyní, false pro ty instance, které se mohou zahodit)
                oldVisibleRowsDict = oldVisibleRows.CreateDictionary(r => r.RowId);
            }

            // Vytvořím nové pole, v tom pořadí, jaké bylo vráceno z Data.GetVisibleRowsId(), a postupně do něj vložím instance pro odpovídající řádek:
            List<DxDataFormRow> newVisibleRows = new List<DxDataFormRow>();
            int rowIndex = rowVisibleFirst;
            foreach (var visibleRowId in visibleRowsId)
            {
                DxDataFormRow visibleRow = null;
                if (oldVisibleRowsDict != null && oldVisibleRowsDict.TryGetValue(visibleRowId, out visibleRow))
                {   // Najdeme náš starý řádek pro shodné RowId?
                }
                else if (oldVisibleRows != null && oldVisibleRows.TryGetFirst(r => !r.VisibleRow, out visibleRow))
                {   // Najdeme nějaký cizí starý řádek, který nebude zapotřebí - tedy pro cizí nepotřebné RowId?
                    visibleRow.AssignRowId(visibleRowId);
                }
                else
                {   // Nemáme žadný starý řádek - ani náš, ani cizí : musíme si vygenerovat new instanci:
                    visibleRow = new DxDataFormRow(this, DxDataFormRowType.RowData, visibleRowId);
                }
                visibleRow.VisibleRow = true;
                visibleRow.RowIndex = rowIndex++;
                newVisibleRows.Add(visibleRow);
            }

            // Pokud máme nějaké staré řádky, které nebyly použité, zahodíme je:
            if (oldVisibleRows != null)
                oldVisibleRows.Where(r => !r.VisibleRow).ForEachExec(r => r.Dispose());

            _VisibleRows = newVisibleRows;
        }
        /// <summary>
        /// Pole řádků, které jsou aktuálně ve viditelné oblasti. 
        /// Toto pole je udržováno v metodě <see cref="_PrepareVisibleRows(int, int)"/>.
        /// Obsahuje viditelné řádky, jejich RowId a vizuální pozici...
        /// </summary>
        private List<DxDataFormRow> _VisibleRows;
        /// <summary>
        /// Počet celkem zobrazovaných řádků, v rozmezí 0 až 2G
        /// </summary>
        private int _RowCount { get { int rowCount = Data.GetRowCount(this.PartId); return (rowCount < 0 ? 0 : rowCount); } }
        /// <summary>
        /// Výška jednoho řádku = výška všech grup <see cref="_GroupsTotalSize"/>.Height s přidáním mezery <see cref="_RowHeightSpace"/>
        /// </summary>
        private int _RowHeight { get { return _GroupsTotalSize.Height + _RowHeightSpace; } }
        /// <summary>
        /// Přídavek k výšce jednoho řádku
        /// </summary>
        private int _RowHeightSpace { get { return 1; } }
        /// <summary>
        /// Velikost prostoru pro všechny řádky
        /// </summary>
        private Size _RowsTotalSize;
        #endregion
        #region Buňky DxDataFormCell

        #endregion
        #region Řízení zobrazení jednotlivých částí

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

            Control parent = this;
            // parent = this._ContentPanel;        // Pokud chci buttony vidět abych viděl přechody focusu...
            _FocusInButton = DxComponent.CreateDxSimpleButton(142, 5, 140, 25, parent, " Focus in...", tabStop: true);
            _FocusInButton.TabIndex = 0;
            _FocusOutButton = DxComponent.CreateDxSimpleButton(352, 5, 140, 25, parent, "... focus out.", tabStop: true);
            _FocusOutButton.TabIndex = 29;
        }
        private DxSimpleButton _FocusInButton;
        private DxSimpleButton _FocusOutButton;
        private DxDataFormColumn _CurrentFocusedItem;
        #endregion
        #region Myš - Move, Down
        private void InitializeInteractivityMouse()
        {
            this._CurrentOnMouseItem = null;
            this._CurrentOnMouseControlSet = null;
            this._CurrentOnMouseControl = null;
            this._ContentPanel.MouseLeave += _ContentPanel_MouseLeave;
            this._ContentPanel.MouseMove += _ContentPanel_MouseMove;
            this._ContentPanel.MouseDown += _ContentPanel_MouseDown;
        }
        /// <summary>
        /// Myš nás opustila
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ContentPanel_MouseLeave(object sender, EventArgs e)
        {
            Point absoluteLocation = MousePosition;
            Point location = this.PointToClient(absoluteLocation);
            if (!this._ContentPanel.Bounds.Contains(location))
                DetectMouseChangeForPoint(null);
            else
                DetectMouseChangeForPoint(this._ContentPanel.PointToClient(absoluteLocation));
        }
        /// <summary>
        /// Myš se pohybuje po Content panelu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ContentPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.None)
                DetectMouseChangeForPoint(e.Location);
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
        /// Vyhledá prvek nacházející se pod aktuální souřadnicí myši a zajistí pro prvky <see cref="MouseLeaveItem(bool)"/> a <see cref="MouseEnterItem(DxDataFormColumn)"/>.
        /// </summary>
        private void DetectMouseChangeForCurrentPoint()
        {
            Point absoluteLocation = Control.MousePosition;
            Point relativeLocation = _ContentPanel.PointToClient(absoluteLocation);
            DetectMouseChangeForPoint(relativeLocation);
        }
        /// <summary>
        /// Vyhledá prvek nacházející se pod danou souřadnicí myši a zajistí pro prvky <see cref="MouseLeaveItem(bool)"/> a <see cref="MouseEnterItem(DxDataFormColumn)"/>.
        /// </summary>
        /// <param name="location">Souřadnice myši relativně k controlu <see cref="_ContentPanel"/> = reálný parent prvků</param>
        private void DetectMouseChangeForPoint(Point? location)
        {
            DxBufferedLayer invalidateLayers = DxBufferedLayer.None;
            DetectMouseChangeGroupForPoint(location, ref invalidateLayers);
            DetectMouseChangeItemForPoint(location, ref invalidateLayers);
            if (invalidateLayers != DxBufferedLayer.None)
                this._ContentPanel.InvalidateLayers(invalidateLayers);
        }
        /// <summary>
        /// Detekuje aktuální grupu pod danou souřadnicí, detekuje změny (Leave a Enter) a udržuje v proměnné <see cref="_CurrentOnMouseGroup"/> aktuální grupu na dané souřadnici
        /// </summary>
        /// <param name="location"></param>
        /// <param name="invalidateLayers"></param>
        private void DetectMouseChangeGroupForPoint(Point? location, ref DxBufferedLayer invalidateLayers)
        {
            if (_VisibleGroups == null) return;

            DxDataFormGroup oldGroup = _CurrentOnMouseGroup;
            DxDataFormGroup newGroup = null;
            bool oldExists = (oldGroup != null);
            bool newExists = location.HasValue && _VisibleGroups.TryGetLast(i => i.IsVisibleOnPoint(location.Value), out newGroup);

            bool isMouseLeave = (oldExists && (!newExists || (newExists && !Object.ReferenceEquals(oldGroup, newGroup))));
            if (isMouseLeave)
                MouseLeaveGroup();

            bool isMouseEnter = (newExists && (!oldExists || (oldExists && !Object.ReferenceEquals(oldGroup, newGroup))));
            if (isMouseEnter)
                MouseEnterGroup(newGroup);

            if (isMouseLeave || isMouseEnter)
                invalidateLayers |= DxBufferedLayer.MainLayer;
        }
        /// <summary>
        /// Je voláno při příchodu myši na danou grupu.
        /// </summary>
        /// <param name="group"></param>
        private void MouseEnterGroup(DxDataFormGroup group)
        {
            _CurrentOnMouseGroup = group;
        }
        /// <summary>
        /// Je voláno při opuštění myši z aktuální grupy.
        /// </summary>
        private void MouseLeaveGroup()
        {
            _CurrentOnMouseGroup = null;
        }
        /// <summary>
        /// Grupa aktuálně se nacházející pod myší
        /// </summary>
        private DxDataFormGroup _CurrentOnMouseGroup;
        /// <summary>
        /// Detekuje aktuální prvek pod danou souřadnicí, detekuje změny (Leave a Enter) a udržuje v proměnné <see cref="_CurrentOnMouseItem"/> aktuální prvek na dané souřadnici
        /// </summary>
        /// <param name="location"></param>
        /// <param name="invalidateLayers"></param>
        private void DetectMouseChangeItemForPoint(Point? location, ref DxBufferedLayer invalidateLayers)
        {
            if (_VisibleItems == null) return;

            DxDataFormColumn oldItem = _CurrentOnMouseItem;
            DxDataFormColumn newItem = null;
            bool oldExists = (oldItem != null);
            bool newExists = location.HasValue && _VisibleItems.TryGetLast(i => i.IsVisibleOnPoint(location.Value), out newItem);

            bool isMouseLeave = (oldExists && (!newExists || (newExists && !Object.ReferenceEquals(oldItem, newItem))));
            if (isMouseLeave)
                MouseLeaveItem();

            bool isMouseEnter = (newExists && (!oldExists || (oldExists && !Object.ReferenceEquals(oldItem, newItem))));
            if (isMouseEnter)
                MouseEnterItem(newItem);

            if (isMouseLeave || isMouseEnter)
                invalidateLayers |= DxBufferedLayer.Overlay;
        }
        /// <summary>
        /// Je voláno při příchodu myši na daný prvek.
        /// </summary>
        /// <param name="item"></param>
        private void MouseEnterItem(DxDataFormColumn item)
        {
            if (item.VisibleBounds.HasValue)
            {
                _CurrentOnMouseItem = item;
                _CurrentOnMouseControlSet = DataForm.GetControlSet(item);
                _CurrentOnMouseControl = _CurrentOnMouseControlSet.GetControlForMouse(item, this._ContentPanel);
                if (!_ContentPanel.IsPaintLayersInProgress)
                {   // V době, kdy probíhá proces Paint, NEBUDU provádět ScrollToBounds:
                    //  Ono k tomu v reálu nedochází - Scroll standardně proběhne při KeyEnter (anebo ruční ScrollBar). To jen při testu provádím MouseMove => ScrollToBounds!
                    bool isScrolled = false;     // this.ScrollToBounds(item.CurrentBounds, null, true);
                    if (isScrolled) Refresh(RefreshParts.AfterScroll);
                }
            }
        }
        /// <summary>
        /// Je voláno při opuštění myši z aktuálního prvku.
        /// </summary>
        private void MouseLeaveItem(bool refresh = false)
        {
            var oldControl = _CurrentOnMouseControl;
            if (oldControl != null)
            {
                oldControl.Visible = false;
                oldControl.Location = new Point(0, -20 - oldControl.Height);
                oldControl.Enabled = false;
                if (oldControl is BaseControl baseControl)
                    baseControl.SuperTip = null;
                if (refresh)
                    oldControl.Refresh();
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
        private DxDataFormColumn _CurrentOnMouseItem;
        /// <summary>
        /// Datový set popisující control, nacházející se nyní pod myší
        /// </summary>
        private DxDataFormControlSet _CurrentOnMouseControlSet;
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

            // Zaeviduji si úkoly ke zpracování:
            _RefreshPartCurrentBounds |= refreshParts.HasFlag(RefreshParts.InvalidateCurrentBounds);
            _RefreshPartContentTotalSize |= refreshParts.HasFlag(RefreshParts.RecalculateContentTotalSize);
            _RefreshPartVisibleItems |= refreshParts.HasFlag(RefreshParts.ReloadVisibleItems);
            _RefreshPartNativeControlsLocation |= refreshParts.HasFlag(RefreshParts.NativeControlsLocation);
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
                    bool doAny = _RefreshPartCurrentBounds || _RefreshPartContentTotalSize || _RefreshPartVisibleItems || _RefreshPartNativeControlsLocation ||
                                 _RefreshPartCache || _RefreshPartInvalidateControl || _RefreshPartRefreshControl;
                    if (!doAny) return;

                    // Provedeme požadované akce; každá akce nejprve shodí svůj příznak (a teoreticky může nahodit jiný příznak):
                    if (_RefreshPartCurrentBounds) _DoRefreshPartCurrentBounds();
                    if (_RefreshPartContentTotalSize) _DoRefreshPartContentTotalSize();
                    if (_RefreshPartVisibleItems) _DoRefreshPartVisibleItems();
                    if (_RefreshPartNativeControlsLocation) _DoRefreshPartNativeControlsLocation();
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
        /// Vyřešit souřadnice nativních controlů, nacházejících se v Content panelu
        /// </summary>
        private bool _RefreshPartNativeControlsLocation;
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

            _CalculateGroupsTotalCurrentSize();
            _CalculateRowsTotalCurrentSize();

            ContentTotalSize = _RowsTotalSize;
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.ReloadVisibleItems"/>
        /// </summary>
        private void _DoRefreshPartVisibleItems()
        {
            _RefreshPartVisibleItems = false;

            // Připravím soupis aktuálně viditelných prvků:
            _PrepareVisibleRows();
            _PrepareVisibleGroupsItems();
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.NativeControlsLocation"/>
        /// </summary>
        private void _DoRefreshPartNativeControlsLocation()
        {
            _RefreshPartNativeControlsLocation = false;

            // Po změně viditelných prvků je třeba provést MouseLeave = prvek pod myší už není ten, co býval:
            this.MouseLeaveItem(true);

            // A zajistit, že po vykreslení prvků bude aktivován prvek, který se nachází pod myší:
            // Až po vykreslení proto, že proces vykreslení určí aktuální viditelné souřadnice prvků!
            this._AfterPaintSearchOnMouseItem = true;
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.InvalidateCache"/>
        /// </summary>
        private void _DoRefreshPartCache()
        {
            _RefreshPartCache = false;

            DataForm.ImageCacheInvalidate();
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
        protected override void OnCurrentDpiChanged()
        {
            base.OnCurrentDpiChanged();
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
            State.ContentVirtualLocation = this.ContentVirtualLocation;

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
        #region Vykreslení obsahu panelu
        /// <summary>
        /// Inicializace kreslení
        /// </summary>
        private void InitializePaint()
        {
            _AfterPaintSearchOnMouseItem = false;
            _PaintingItems = false;
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
                case DxBufferedLayer.Overlay:
                    PaintContentOverlay(args);
                    break;
            }
        }
        /// <summary>
        /// Metoda zajistí vykreslení aplikačního pozadí (okraj aktivních prvků)
        /// </summary>
        /// <param name="e"></param>
        private void PaintContentAppBackground(DxBufferedGraphicPaintArgs e)
        {
            return;

            bool isPainted = false;

            // _VisibleGroups.ForEachExec(g => PaintGroupStandard(g, visibleOrigin, e));

            var mouseControl = _CurrentOnMouseControl;
            var mouseItem = _CurrentOnMouseItem;
            if (mouseControl != null && mouseItem != null)
            {
                var indicators = mouseItem.IItem.Indicators;
                bool isThin = indicators.HasFlag(DataFormColumnIndicatorType.MouseOverThin);
                bool isBold = indicators.HasFlag(DataFormColumnIndicatorType.MouseOverBold);
                if (isThin || isBold)
                {
                    Color color = DataFormAppearance.OnMouseIndicatorColor;
                    PaintItemIndicator(e, mouseControl.Bounds, color, isBold, ref isPainted);
                }
            }

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
        /// <summary>
        /// Zajistí hlavní vykreslení obsahu - grupy a prvky
        /// </summary>
        /// <param name="e"></param>
        private void PaintContentMainLayer(DxBufferedGraphicPaintArgs e)
        {
            bool afterPaintSearchActiveItem = _AfterPaintSearchOnMouseItem;
            _AfterPaintSearchOnMouseItem = false;
            DataForm.ImagePaintStart();
            OnPaintContentStandard(e);
            DataForm.ImagePaintDone();
            if (afterPaintSearchActiveItem)
                DetectMouseChangeForCurrentPoint();
        }
        /// <summary>
        /// Metoda provede standardní vykreslení grup a prvků
        /// </summary>
        /// <param name="e"></param>
        private void OnPaintContentStandard(DxBufferedGraphicPaintArgs e)
        {
            var startTime = DxComponent.LogTimeCurrent;
            try
            {
                _PaintingItems = true;
                Point visibleOrigin = this.ContentVirtualLocation;
                _VisibleGroups.ForEachExec(g => PaintGroupStandard(g, visibleOrigin, e));
                _VisibleItems.ForEachExec(i => PaintItemStandard(i, visibleOrigin, e));
            }
            finally
            {
                _PaintingItems = false;
            }
            DxComponent.LogAddLineTime($"DxDataForm Paint Standard() Items: {_VisibleItems?.Count}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Provede vykreslení jedné dané grupy
        /// </summary>
        /// <param name="group"></param>
        /// <param name="visibleOrigin"></param>
        /// <param name="e"></param>
        private void PaintGroupStandard(DxDataFormGroup group, Point visibleOrigin, DxBufferedGraphicPaintArgs e)
        {
            var bounds = group.CurrentGroupBounds;
            Point location = bounds.Location.Sub(visibleOrigin);
            group.VisibleGroupBounds = new Rectangle(location, bounds.Size);

            bool onMouse = Object.ReferenceEquals(group, _CurrentOnMouseGroup);
            group.PaintGroup(e, onMouse, false);
        }
        /// <summary>
        /// Provede vykreslení jednoho daného prvku
        /// </summary>
        /// <param name="item"></param>
        /// <param name="visibleOrigin"></param>
        /// <param name="e"></param>
        private void PaintItemStandard(DxDataFormColumn item, Point visibleOrigin, DxBufferedGraphicPaintArgs e)
        {
            var controlSet = DataForm.GetControlSet(item);
            var bounds = item.CurrentBounds;
            Point location = bounds.Location.Sub(visibleOrigin);
            Color? indicatorColor = GetIndicatorColor(item, out bool isBold);

            // Pořadí akcí je mírně zmatené, protože:
            // 1. Indikátor chci kreslit 'pod' obrázek controlu (Image)
            // 2. Control ale nemusí vygenerovat obrázek (metoda CreateImage() vrátí null), pak chci vykreslit indikátor i bez existence obrázku
            // 3. Reálná velikost obrázku (Image) nemusí odpovídat velikosti prostoru (item.CurrentBounds), protože control může mít reálně jinou výšku, než jsme mu nadiktovali my dle designu
            // 4. Pokud tedy CreateImage vrátí obrázek, pak použijeme jeho rozměry pro vykreslení indikátoru; a pokud obrázek nevrátí, pak indikátor vykreslíme do designem určené velikosti.

            Rectangle? visibleBounds = null;
            if (controlSet.CanPaintByPainter)
            {
                if (item.IItem is IDataFormColumnImageText label)
                {
                    Control control = DataForm.GetControl(item, DxDataFormControlUseMode.Draw, null);
                    if (control is BaseControl baseControl)
                    {
                        var appearance = baseControl.GetViewInfo().PaintAppearance;
                        appearance.Font = DxComponent.ZoomToGui(appearance.Font, this.CurrentDpi);
                        Size size = item.CurrentBounds.Size;
                        visibleBounds = new Rectangle(location, size);
                        appearance.DrawString(e.GraphicsCache, label.Text, visibleBounds.Value);
                    }
                }
            }
            if (!visibleBounds.HasValue && controlSet.CanPaintByImage)
            {
                using (var image = DataForm.CreateImage(item, e.Graphics))
                {
                    if (image != null)
                    {
                        Size size = image.Size;
                        visibleBounds = new Rectangle(location, size);

                        if (indicatorColor.HasValue)
                            PaintItemIndicator(e, visibleBounds.Value, indicatorColor.Value, isBold);

                        e.Graphics.DrawImage(image, location);
                    }
                }
            }

            if (!visibleBounds.HasValue)
            {   // Když nebyl získán Image pro control, pak velikost prostoru převezmeme dle designu.
                // Značí to ale, že dosud nebyl vykreslen Indicator, ten se kreslil "pod obrázek" ale "podle jeho velikosti".
                visibleBounds = new Rectangle(location, bounds.Size);
                if (indicatorColor.HasValue)
                    PaintItemIndicator(e, visibleBounds.Value, indicatorColor.Value, isBold);
            }

            item.VisibleBounds = visibleBounds;
        }
        private void PaintContentOverlay(DxBufferedGraphicPaintArgs e)
        {
            bool isPainted = false;

            var mouseControl = _CurrentOnMouseControl;
            var mouseItem = _CurrentOnMouseItem;
            if (mouseControl != null && mouseItem != null)
            {
                var indicators = mouseItem.IItem.Indicators;
                bool isThin = indicators.HasFlag(DataFormColumnIndicatorType.MouseOverThin);
                bool isBold = indicators.HasFlag(DataFormColumnIndicatorType.MouseOverBold);
                if (isThin || isBold)
                {
                    Color color = DataFormAppearance.OnMouseIndicatorColor;
                    PaintItemIndicator(e, mouseControl.Bounds, color, isBold, ref isPainted);
                }
            }

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
        /// <summary>
        /// Metoda vrátí barvu, kterou se má vykreslit indikátor pro daný prvek
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isBold"></param>
        /// <returns></returns>
        private Color? GetIndicatorColor(DxDataFormColumn item, out bool isBold)
        {
            isBold = false;
            if (item == null) return null;

            var indicators = item.IItem.Indicators;
            bool itemIndicatorsVisible = DataForm.ItemIndicatorsVisible;
            var appearance = DataFormAppearance;

            Color? focusColor = null;             // Pokud prvek má focus, pak zde bude barva orámování focusu

            Color? statusColor = null;
            if (item.IItem.IndicatorColor.HasValue)
            {   // Zadání barvy IndicatorColor potlačí všechny ostatní příznaky indikátorů:
                if (IsIndicatorActive(indicators, itemIndicatorsVisible, DataFormColumnIndicatorType.IndicatorColorAllwaysBold, DataFormColumnIndicatorType.IndicatorColorOnDemandBold, DataFormColumnIndicatorType.IndicatorColorAllwaysThin, DataFormColumnIndicatorType.IndicatorColorOnDemandThin, ref isBold))
                    statusColor = item.IItem.IndicatorColor.Value;
            }
            else
            {
                if (IsIndicatorActive(indicators, itemIndicatorsVisible, DataFormColumnIndicatorType.ErrorAllwaysBold, DataFormColumnIndicatorType.ErrorOnDemandBold, DataFormColumnIndicatorType.ErrorAllwaysThin, DataFormColumnIndicatorType.ErrorOnDemandThin, ref isBold))
                    statusColor = appearance.ErrorIndicatorColor;
                else if (IsIndicatorActive(indicators, itemIndicatorsVisible, DataFormColumnIndicatorType.WarningAllwaysBold, DataFormColumnIndicatorType.WarningOnDemandBold, DataFormColumnIndicatorType.WarningAllwaysThin, DataFormColumnIndicatorType.WarningOnDemandThin, ref isBold))
                    statusColor = appearance.WarningIndicatorColor;
                else if (IsIndicatorActive(indicators, itemIndicatorsVisible, DataFormColumnIndicatorType.CorrectAllwaysBold, DataFormColumnIndicatorType.CorrectOnDemandBold, DataFormColumnIndicatorType.CorrectAllwaysThin, DataFormColumnIndicatorType.CorrectOnDemandThin, ref isBold))
                    statusColor = appearance.CorrectIndicatorColor;
                else if (IsIndicatorActive(indicators, itemIndicatorsVisible, DataFormColumnIndicatorType.RequiredAllwaysBold, DataFormColumnIndicatorType.RequiredOnDemandBold, DataFormColumnIndicatorType.RequiredAllwaysThin, DataFormColumnIndicatorType.RequiredOnDemandThin, ref isBold))
                    statusColor = appearance.RequiredIndicatorColor;
            }

            bool hasFocus = focusColor.HasValue;
            bool hasStatus = statusColor.HasValue;
            // Pokud bych měl souběh obou barev (focus i status), pak výsledná barva bude Morph (70% status + 30% focus)
            Color? resultColor = ((hasFocus && hasStatus) ? (Color?)statusColor.Value.Morph(focusColor.Value, 0.70f) :
                                 (hasFocus ? focusColor :
                                 (hasStatus ? statusColor : (Color?)null)));

            return resultColor;
        }
        /// <summary>
        /// Metoda určí, zda indikátor prvku (<paramref name="indicators"/>) vyhovuje některým zadaným hodnotám a vrátí true pokud ano.
        /// </summary>
        /// <param name="indicators"></param>
        /// <param name="itemIndicatorsVisible"></param>
        /// <param name="allwaysBold"></param>
        /// <param name="onDemandBold"></param>
        /// <param name="alwaysThin"></param>
        /// <param name="onDemandThin"></param>
        /// <param name="isBold"></param>
        /// <returns></returns>
        private bool IsIndicatorActive(DataFormColumnIndicatorType indicators, bool itemIndicatorsVisible,
            DataFormColumnIndicatorType allwaysBold, DataFormColumnIndicatorType onDemandBold, DataFormColumnIndicatorType alwaysThin, DataFormColumnIndicatorType onDemandThin,
            ref bool isBold)
        {
            if (indicators.HasFlag(allwaysBold) || (itemIndicatorsVisible && indicators.HasFlag(onDemandBold)))
            {
                isBold = true;
                return true;
            }
            if (indicators.HasFlag(alwaysThin) || (itemIndicatorsVisible && indicators.HasFlag(onDemandThin)))
                return true;
            return false;
        }
        /// <summary>
        /// Zajistí vykreslení slabého orámování (prozáření okrajů) pro daný prostor (prvek) danou barvou.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="isBold"></param>
        private void PaintItemIndicator(DxBufferedGraphicPaintArgs e, Rectangle bounds, Color color, bool isBold)
        {
            bool isPainted = false;
            PaintItemIndicator(e, bounds, color, isBold, ref isPainted);
        }
        /// <summary>
        /// Zajistí vykreslení slabého orámování (prozáření okrajů) pro daný prostor (prvek) danou barvou.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="bounds"></param>
        /// <param name="color"></param>
        /// <param name="isBold"></param>
        /// <param name="isPainted"></param>
        private void PaintItemIndicator(DxBufferedGraphicPaintArgs e, Rectangle bounds, Color color, bool isBold, ref bool isPainted)
        {
            // Tohle je vlastnost Drawing světa: pokud control má šířku 100, tak na pixelu 100 už není kreslen on ale to za ním...:
            bounds.Width--;
            bounds.Height--;

            // Kontrola získání barvy pozadí:
            //  var bgc = DxComponent.GetSkinColor(SkinElementColor.Control_PanelBackColor);
            //  var bgc1 = DxComponent.GetSkinColor(SkinElementColor.CommonSkins_Control);
            //  if (bgc != bgc1)
            //  { }

            if (isBold)
            {
                e.Graphics.DrawRectangle(DxComponent.PaintGetPen(color, 48), bounds.Enlarge(3));
                e.Graphics.DrawRectangle(DxComponent.PaintGetPen(color, 106), bounds.Enlarge(2));
                e.Graphics.DrawRectangle(DxComponent.PaintGetPen(color, 160), bounds.Enlarge(1));
            }
            else
            {
                e.Graphics.DrawRectangle(DxComponent.PaintGetPen(color, 48), bounds.Enlarge(2));
                e.Graphics.DrawRectangle(DxComponent.PaintGetPen(color, 80), bounds.Enlarge(1));
            }
            isPainted = true;
        }
        /// <summary>
        /// Pole jednotlivých vrstev bufferované grafiky
        /// </summary>
        private static DxBufferedLayer[] BufferedLayers { get { return new DxBufferedLayer[] { DxBufferedLayer.AppBackground, DxBufferedLayer.MainLayer, DxBufferedLayer.Overlay }; } }
        /// <summary>
        /// Souhrn vrstev použitých v this controlu, používá se při invalidaci všech vrstev
        /// </summary>
        private static DxBufferedLayer UsedLayers { get { return DxBufferedLayer.AppBackground | DxBufferedLayer.MainLayer | DxBufferedLayer.Overlay; } }
        /// <summary>
        /// Příznak, že po dokončení vykreslení standardní vrstvy máme najít aktivní prvek na aktuální souřadnici myši a případně jej aktivovat.
        /// Příznak je nastaven po scrollu, protože původní prvek pod myší nám "ujel jinam" a nyní pod myší může být narolovaný jiný aktivní prvek.
        /// </summary>
        private bool _AfterPaintSearchOnMouseItem;
        private bool _PaintingItems = false;

        #endregion
        #region Fyzické controly - tvorba, správa, vykreslení bitmapy skrze control
        /// <summary>
        /// Kompletní informace o jednom prvku: index řádku, deklarace, control set a fyzický control
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
            public DxDataFormColumn Item;
            /// <summary>
            /// Datový set popisující control, nacházející se nyní pod myší
            /// </summary>
            public DxDataFormControlSet ControlSet;
            /// <summary>
            /// Vizuální control, nacházející se nyní pod myší
            /// </summary>
            public Control Control;
        }
        #endregion
        #region Stav panelu - umožní uložit aktuální stav do objektu, a v budoucnu tento stav jej restorovat
        /// <summary>
        /// Stav panelu - pozice scrollbarů atd. 
        /// Podporuje přepínání záložek - vizuálně jiný obsah, ale promítaný prostřednictvím jedné instance <see cref="DxDataFormPanel"/>.
        /// Při čtení bude vrácen objekt obsahující aktuální stav.
        /// Při zápisu budou hodnoty z vkládaného objektu aplikovány do panelu
        /// </summary>
        internal DxDataFormState State
        {
            get
            {
                if (_State == null)
                    _State = new DxDataFormState();
                _FillStateFromPanel();
                return _State;
            }
            set
            {
                _State = value;
                _ApplyStateToPanel();
            }
        }
        /// <summary>Stav panelu - pozice scrollbarů atd. </summary>
        private DxDataFormState _State;
        /// <summary>
        /// Klíčové hodnoty z this panelu uloží do objektu <see cref="_State"/>.
        /// </summary>
        private void _FillStateFromPanel()
        {
            if (_State == null) return;
            _State.ContentVirtualLocation = this.ContentVirtualLocation;
        }
        /// <summary>
        /// Z objektu <see cref="_State"/> opíše klíčové hodnoty do this panelu
        /// </summary>
        private void _ApplyStateToPanel()
        {
            this.ContentVirtualLocation = _State?.ContentVirtualLocation ?? Point.Empty;
        }
        #endregion
    }
    /// <summary>
    /// Identifikátor jedné konkrétní části <see cref="DxDataFormPart"/>.
    /// Hodnoty <see cref="PartXId"/> i <see cref="PartYId"/> lze za běhu měnit, protože instance lze za dobu života přesouvat (přidávat / odebírat) 
    /// a tím se mění pozice části <see cref="DxDataFormPart"/>.
    /// </summary>
    internal class DxDataFormPartId
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxDataFormPartId() { }
        /// <summary>
        /// Konstruktor s danými hodnotami
        /// </summary>
        /// <param name="partXId"></param>
        /// <param name="partYId"></param>
        public DxDataFormPartId(int partXId, int partYId)
        {
            PartXId = partXId;
            PartYId = partYId;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"PartId: X={PartXId}; Y={PartYId}";
        }
        /// <summary>
        /// Identifikátor this části ve směru X = vodorovném = sloupce.
        /// Výchozí část má ID = 0; pokud se svislým splitterem rozdělí na dvě, pak část vpravo bude mít <see cref="PartXId"/> = 1, atd.
        /// S tímto ID se pak dotazuje parentů (dataformu a jeho dat) na řádky, sloupce atd.
        /// </summary>
        public int PartXId { get; set; }
        /// <summary>
        /// Identifikátor this části ve směru Y = vodorovném = řádky.
        /// Výchozí část má ID = 0; pokud se vodorovným splitterem rozdělí na dvě, pak část dole bude mít <see cref="PartYId"/> = 1, atd.
        /// S tímto ID se pak dotazuje parentů (dataformu a jeho dat) na řádky, sloupce atd.
        /// </summary>
        public int PartYId { get; set; }
    }
    #endregion
    #region class DxDataFormRow : Jeden vizuální řádek v rámci DxDataFormRowBand
    /// <summary>
    /// DxDataFormRow : Jeden vizuální řádek v rámci DxDataFormRowBand
    /// </summary>
    internal class DxDataFormRow : IDisposable
    {
        #region Konstruktor, vlastník, prvky
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="rowBand"></param>
        /// <param name="rowType"></param>
        /// <param name="rowId"></param>
        public DxDataFormRow(DxDataFormPart rowBand, DxDataFormRowType rowType, int rowId)
        {
            _RowBand = rowBand;
            _RowType = rowType;
            _RowId = rowId;
        }
        public void Dispose()
        {
            _RowBand = null;
            DisposeCells();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"RowType: {_RowType}; RowIndex: {RowIndex}; RowId: {_RowId}; VisualPositions: {VisualPositions}";
        }
        /// <summary>Vlastník - <see cref="DxDataFormPart"/></summary>
        private DxDataFormPart _RowBand;
        /// <summary>Typ řádku</summary>
        private DxDataFormRowType _RowType;
        /// <summary>ID řádku, odkazuje se do <see cref="DxDataForm"/> pro data</summary>
        private int _RowId;
        /// <summary>
        /// Vlastník - <see cref="DxDataForm"/>
        /// </summary>
        public DxDataForm DataForm { get { return this._RowBand.DataForm; } }
        /// <summary>
        /// Vlastník - <see cref="DxDataFormPart"/>
        /// </summary>
        public DxDataFormPart RowBand { get { return this._RowBand; } }
        /// <summary>
        /// Typ řádku
        /// </summary>
        public DxDataFormRowType RowType { get { return this._RowType; } }
        /// <summary>
        /// ID řádku, odkazuje se do <see cref="DxDataForm"/> pro data
        /// </summary>
        public int RowId { get { return this._RowId; } }
        /// <summary>
        /// Index viditelného řádku.
        /// V tomto pořadí jsou řádky zobrazeny.
        /// V poli řádků musí být tato hodnota kontinuální a vzestupná, počínaje 0.
        /// Řádky s hodnotou -1 (a jinou zápornou) nebudou zobrazeny.
        /// </summary>
        public int RowIndex { get; set; }

        #endregion

        /// <summary>
        /// Metoda je volána tehdy, když this řádek byl dosud použit pro určitý datový řádek (stávající <see cref="RowId"/>),
        /// ale pro něj již řádek není potřeba, ale je potřeba pro nový jiný řádek <paramref name="rowId"/>.
        /// Tento princip šetří režii při uvolnění instance z jednoho řádku a vytváření new instance pro zobrazení jiného řádku tím,
        /// že starou instanci použije pro jiný řádek.
        /// </summary>
        /// <param name="rowId"></param>
        internal void AssignRowId(int rowId)
        {
            _RowId = rowId;
            // cokoliv dalšího:
        }
        /// <summary>
        /// Zahodí a uvolní buňky
        /// </summary>
        private void DisposeCells()
        { }
        /// <summary>
        /// Do this instance vyplní hodnoty do <see cref="VisualPositions"/>, přičemž parametr <paramref name="visualBegin"/> na závěr navýší o <paramref name="visualSize"/>.
        /// </summary>
        /// <param name="visualBegin"></param>
        /// <param name="visualSize"></param>
        public void ApplyVisualPosition(ref int visualBegin, int visualSize)
        {
            int visualEnd = visualBegin + visualSize;
            _VisualPositions = new Int32Range(visualBegin, visualEnd);
            visualBegin = visualEnd;
        }
        /// <summary>
        /// Řádek je viditelný?
        /// </summary>
        public bool VisibleRow { get { return _VisibleRow; } set { _VisibleRow = value; } }
        private bool _VisibleRow;

        /// <summary>
        /// Umístění na ose Y; reálné pixely v rámci celého Bandu; nastavuje se pouze pro řádky 
        /// typu <see cref="DxDataFormRowType.RowData"/> a <see cref="DxDataFormRowType.RowSummary"/> = ty jsou pohyblivé podle ScrollBaru.
        /// 
        /// </summary>
        public Int32Range CurrentPositions { get { return _CurrentPositions; } set { _CurrentPositions = value; } }
        private Int32Range _CurrentPositions;
        /// <summary>
        /// Umístění na ose Y; vizuální pixely v rámci celého Bandu - u typu řádku <see cref="DxDataFormRowType.RowHeader"/>,
        /// <see cref="DxDataFormRowType.RowFilter"/> a <see cref="DxDataFormRowType.RowFooter"/> jsou permanentní (tyto řádky jsou nepohyblivé),
        /// u řádků typu řádku <see cref="DxDataFormRowType.RowData"/> a <see cref="DxDataFormRowType.RowSummary"/> jsou pohyblivé podle ScrollBaru.
        /// </summary>
        public Int32Range VisualPositions { get { return _VisualPositions; } set { _VisualPositions = value; } }
        private Int32Range _VisualPositions;
    }
    #endregion
    #region class DxDataFormCell : Jedna vizuální buňka v rámci DataFormu, průnik řádku a sloupce
    internal class DxDataFormCell
    {

    }
    #endregion
    #region class DxDataFormTab : Data jedné viditelné záložky. Záložka může shrnovat grupy z více stránek, pokud to layout umožní a potřebuje
    /// <summary>
    /// Data jedné viditelné záložky. Záložka může shrnovat grupy z více stránek, pokud to layout umožní a potřebuje.
    /// </summary>
    internal class DxDataFormTab : IPageItem
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
        /// První nenulový <see cref="DxDataFormPage.PageImageName"/>
        /// </summary>
        public string ImageName { get { return Pages.Select(p => p.PageImageName).Where(i => i != null).FirstOrDefault(); } }
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
        #region Stav záložky, umožní panelu shrnout svůj stav a uložit jej do záložky, a následně ze záložky jej promítnout do živého stavu
        /// <summary>
        /// Stav záložky, umožní panelu shrnout svůj stav a uložit jej do záložky, a následně ze záložky jej promítnout do živého stavu
        /// </summary>
        internal DxDataFormState State
        {
            get
            {
                if (_State == null)
                    _State = new DxDataFormState();
                return _State;
            }
            set
            {
                _State = value;
            }
        }
        private DxDataFormState _State;
        #endregion
        #region IPageItem
        /// <summary>
        /// PageId stránky
        /// </summary>
        string IPageItem.PageId { get { return this.TabName; } }
        /// <summary>
        /// Zobrazit Close button?
        /// </summary>
        bool IPageItem.CloseButtonVisible { get { return false; } }
        /// <summary>
        /// Sem bude umístěn Control záložky po přidání do TabPageHeaderu
        /// </summary>
        Control IPageItem.PageControl { get { return TabPageControl; } set { TabPageControl = value; } }
        /// <summary>
        /// Sem bude umístěn Control záložky po přidání do TabPageHeaderu
        /// </summary>
        protected Control TabPageControl { get; set; }
        /// <summary>
        /// Stringová identifikace prvku, musí být jednoznačná v rámci nadřízeného prvku
        /// </summary>
        string ITextItem.ItemId { get { return this.TabName; } }
        /// <summary>
        /// Hlavní text v prvku
        /// </summary>
        string ITextItem.Text { get { return this.TabText; } }
        /// <summary>
        /// Pořadí prvku, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        int ITextItem.ItemOrder { get; set; }
        /// <summary>
        /// Obsahuje tre tehdy, když před prvkem má být oddělovač
        /// </summary>
        bool ITextItem.ItemIsFirstInGroup { get { return false; } }
        /// <summary>
        /// Prvek je Visible?
        /// </summary>
        bool ITextItem.Visible { get { return true; } }
        /// <summary>
        /// Prvek je Enabled?
        /// </summary>
        bool ITextItem.Enabled { get { return true; } }
        /// <summary>
        /// Určuje, zda CheckBox je zaškrtnutý.
        /// Po změně zaškrtnutí v Ribbonu (uživatelem) je do této property setována aktuální hodnota z Ribbonu 
        /// a poté je vyvolána událost <see cref="DxRibbonControl.RibbonItemClick"/>.
        /// Hodnota může být null, pak první kliknutí nastaví false, druhé true, třetí zase false (na NULL se interaktivně nedá doklikat).
        /// <para/>
        /// Pokud konkrétní prvek nepodporuje null, akceptuje null jako false.
        /// </summary>
        bool? ITextItem.Checked { get; set; }
        /// <summary>
        /// Styl písma
        /// </summary>
        FontStyle? ITextItem.FontStyle { get { return null; } }
        /// <summary>
        /// Fyzický obrázek ikony.
        /// </summary>
        Image ITextItem.Image { get { return null; } }
        /// <summary>
        /// Fyzický vektor ikony
        /// </summary>
        DevExpress.Utils.Svg.SvgImage ITextItem.SvgImage { get { return null; } }
        /// <summary>
        /// Jméno ikony.
        /// Pro prvek typu CheckBox tato ikona reprezentuje stav, kdy <see cref="ITextItem.Checked"/> = NULL.
        /// </summary>
        string ITextItem.ImageName { get { return this.ImageName; } }
        /// <summary>
        /// Jméno ikony pro stav UnChecked u typu <see cref="RibbonItemType.CheckBoxToggle"/>
        /// </summary>
        string ITextItem.ImageNameUnChecked { get { return null; } }
        /// <summary>
        /// Jméno ikony pro stav Checked u typu <see cref="RibbonItemType.CheckBoxToggle"/>
        /// </summary>
        string ITextItem.ImageNameChecked { get { return null; } }
        /// <summary>
        /// Styl zobrazení
        /// </summary>
        BarItemPaintStyle ITextItem.ItemPaintStyle { get { return BarItemPaintStyle.Standard; } }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        object ITextItem.Tag { get { return null; } }
        /// <summary>
        /// Text ToolTipu
        /// </summary>
        string IToolTipItem.ToolTipText { get { return this.TabToolTipText; } }
        /// <summary>
        /// Titulek ToolTipu. Pokud nebude naplněn, vezme se text prvku.
        /// </summary>
        string IToolTipItem.ToolTipTitle { get { return this.TabToolTipTitle; } }
        /// <summary>
        /// Ikona ToolTipu
        /// </summary>
        string IToolTipItem.ToolTipIcon { get { return null; } }

        #endregion
    }
    #endregion
    #region class DxDataFormState : Stav DataFormu, slouží pro persistenci stavu při přepínání záložek
    /// <summary>
    /// Stav DataFormu, slouží pro persistenci stavu při přepínání záložek.
    /// Obsahuje pozice ScrollBarů (reálně obsahuje <see cref="ContentVirtualLocation"/>) a objekt s focusem.
    /// <para/>
    /// Má význam víceméně u záložkových DataFormů, aby při přepínání záložek byla konkrétní záložka zobrazena v tom stavu, v jakém byla opuštěna.
    /// </summary>
    internal class DxDataFormState
    {
        /// <summary>
        /// Posun obsahu daný pozicí ScrollBarů
        /// </summary>
        public Point ContentVirtualLocation { get; set; }
        /// <summary>
        /// Vrací klon objektu
        /// </summary>
        /// <returns></returns>
        public DxDataFormState Clone()
        {
            return (DxDataFormState)this.MemberwiseClone();
        }
    }
    #endregion
    #region class DxDataFormPage : Třída reprezentující jednu designem definovanou stránku v dataformu.
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
        /// Stránka je na aktivní záložce? 
        /// Po iniciaci se přebírá do GUI, následně udržuje GUI.
        /// V jeden okamžik může být aktivních více stránek najednou, pokud je více stránek <see cref="IDataFormPage"/> mergováno do jedné záložky.
        /// </summary>
        public bool Active { get; set; }
        #endregion
        #region Data o stránce
        /// <summary>
        /// ID stránky
        /// </summary>
        public string PageId { get { return IPage.PageId; } }
        /// <summary>
        /// Titulek stránky
        /// </summary>
        public string PageText { get { return IPage.PageText; } }
        /// <summary>
        /// Obsahuje true, pokud obsah této stránky je povoleno mergovat do předchozí stránky, pokud je dostatek prostoru.
        /// Stránky budou mergovány do vedle sebe stojících sloupců, každý bude mít nadpis své původní stránky.
        /// <para/>
        /// Aby byly stránky mergovány, musí mít tento příznak obě (nebo všechny).
        /// </summary>
        public bool AllowMerge { get { return IPage.AllowMerge; } }
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
        internal void InvalidateBounds()
        {
            _CurrentPageBounds = null;
            _Groups.ForEachExec(g => g.InvalidateBounds());
        }
        /// <summary>
        /// Metoda určí souřadnice všech skupin na této stránce, podle definovaných pravidel na grupě.
        /// Výchozí layout je sekvenční pod sebou, podle pravidel v grupě může docházet k zalomení na nový sloupec stránky na povinné grupě anebo tam kde je to vhodné.
        /// </summary>
        internal void PrepareGroupLayout()
        {
            Size pageVisibleSize = _DataForm.VisibleContentSize;               // Prostor k dispozici
            int x = 0;
            int y = 0;
            int right = 0;
            int bottom = 0;
            bool containForceBreak = false;
            bool containAllowBreak  = false;
            foreach (var dataGroup in Groups)
            {
                if (dataGroup.LayoutForceBreakToNewColumn)
                {   // Požadavek na Force Break:
                    containForceBreak = true;
                    if (y > 0 && right > 0)
                    {   // Můžeme vyhovět:
                        x = right;
                        y = 0;
                    }
                }
                else if (dataGroup.LayoutAllowBreakToNewColumn && !containAllowBreak)
                    // Zapamatujeme si, že je možno provést AllowBreak:
                    containAllowBreak  = true;

                dataGroup.CurrentGroupOrigin = new Point(x, y);                // Tady se invaliduje CurrentGroupBounds
                var groupBounds = dataGroup.CurrentGroupBounds;                // Tady dojde k vyhodnocení souřadnice CurrentGroupOrigin a k přepočtu DesignSize na CurrentSize.
                y = groupBounds.Bottom;
                if (groupBounds.Right > right) right = groupBounds.Right;      // Střádám největší Right pro případná zalomení
                if (groupBounds.Bottom > bottom) bottom = groupBounds.Bottom;  // Střádám největší Bottom pro případná zalomení
            }

            if (containAllowBreak && !containForceBreak)
            {   // Dynamické zalomení je možné (máme alespoň jednu grupu, která to povoluje) a nemáme povinné zalomení:

            }

            _CurrentPageBounds = null;                                         // Nápočet se provede až on-demand
        }
        /// <summary>
        /// Obsahuje součet souřadnic <see cref="DxDataFormGroup.CurrentGroupBounds"/> ze zdejších skupin
        /// </summary>
        internal Rectangle CurrentPageBounds { get { CheckCurrentBounds(); return _CurrentPageBounds.Value; } }
        private Rectangle? _CurrentPageBounds;
        /// <summary>
        /// Zajistí, že souřadnice <see cref="_CurrentPageBounds"/> a bude obsahovat platný součet souřadnic jednotlivých skupin
        /// </summary>
        private void CheckCurrentBounds()
        {
            if (!_CurrentPageBounds.HasValue)
                _CurrentPageBounds = Groups.Select(g => g.CurrentGroupBounds).SummaryVisibleRectangle() ?? Rectangle.Empty;
        }
        #endregion
    }
    #endregion
    #region class DxDataFormGroup : Třída reprezentující jednu grupu na stránce
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
                    var dataGroup = new DxDataFormGroup(dataPage, iGroup);
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
            __DataPage = dataPage;
            __IGroup = iGroup;
            __Items = new List<DxDataFormColumn>();
            DxDataFormColumn.AddToList(this, true, iGroup?.GroupHeader?.HeaderItems, __Items);
            DxDataFormColumn.AddToList(this, false, iGroup?.Items, __Items);
            _CalculateAutoSize();
            InvalidateBounds();
        }
        /// <summary>Vlastník - <see cref="DxDataFormPage"/></summary>
        private DxDataFormPage __DataPage;
        /// <summary>Deklarace grupy</summary>
        private IDataFormGroup __IGroup;
        /// <summary>Prvky</summary>
        private List<DxDataFormColumn> __Items;
        /// <summary>
        /// Vlastník - <see cref="DxDataForm"/>
        /// </summary>
        public DxDataForm DataForm { get { return this.__DataPage?.DataForm; } }
        /// <summary>
        /// Vlastník - <see cref="DxDataFormPage"/>
        /// </summary>
        public DxDataFormPage DataPage { get { return __DataPage; } }
        /// <summary>
        /// Deklarace grupy
        /// </summary>
        private IDataFormGroup _IGroup { get { return __IGroup; } }
        /// <summary>
        /// Jednotlivé prvky grupy
        /// </summary>
        public IList<DxDataFormColumn> Items { get { return __Items; } }
        /// <summary>
        /// Počet celkem deklarovaných prvků
        /// </summary>
        internal int ItemsCount { get { return __Items.Count; } }
        /// <summary>
        /// Viditelnost grupy
        /// </summary>
        public bool IsVisible { get { return _IGroup.IsVisible; } }
        #endregion
        #region Data o grupě
        /// <summary>
        /// Řízení layoutu: na této grupě je povoleno zalomení sloupce = tato grupa může být v případě potřeby umístěna jako první do dalšího sloupce
        /// </summary>
        internal bool LayoutAllowBreakToNewColumn { get { return (_IGroup.LayoutMode.HasFlag(DatFormGroupLayoutMode.AllowBreakToNewColumn)); } }
        /// <summary>
        /// Řízení layoutu: na této grupě je povinné zalomení sloupce = tato grupa má být umístěna jako první do dalšího sloupce
        /// </summary>
        internal bool LayoutForceBreakToNewColumn { get { return (_IGroup.LayoutMode.HasFlag(DatFormGroupLayoutMode.ForceBreakToNewColumn)); } }

        #endregion
        #region Souřadnice designové (velikost a souřadnice počátku Items)
        /// <summary>
        /// Prostor, ve kterém je vykreslen text titulku a případné další prvky.
        /// Je umístěn v <see cref="Coordinates.HeaderBackgroundBounds"/> a je oproti němu zmenšen o <see cref="Coordinates.HeaderPadding"/> dovnitř.
        /// Je relativní v rámci this grupy.
        /// </summary>
        internal Rectangle DesignTitleBounds { get { return __DesignTitleBounds; } } private Rectangle __DesignTitleBounds;
        /// <summary>
        /// Souřadnice a velikost prostoru v rámci grupy, ve kterém jsou zobrazeny jednotlivé Items = <see cref="DxDataFormColumn.DesignBounds"/>.
        /// Hodnota je relativní k this grupě a je v designových pixelech bez Zoomu.
        /// </summary>
        internal Rectangle DesignContentBounds { get { return __DesignContentBounds; } } private Rectangle __DesignContentBounds;
        /// <summary>
        /// Zajistí provedení výpočtu automatické velikosti grupy.
        /// Reaguje na <see cref="IDataFormGroup.DesignPadding"/>, čte prvky <see cref="Items"/> 
        /// a určuje hodnoty do <see cref="DesignContentBounds"/>
        /// </summary>
        private void _CalculateAutoSize()
        {
            // Získám definici souřadnic v grupě:
            var iGroup = _IGroup;
            var coordinates = new Coordinates() { BorderRange = iGroup.DesignBorderRange, Padding = iGroup.DesignPadding };
            var groupTitle = iGroup.GroupHeader;
            if (groupTitle != null && groupTitle.DesignHeaderHeight.HasValue && groupTitle.DesignHeaderHeight.Value > 0)
            {
                coordinates.HeaderHeight = groupTitle.DesignHeaderHeight.Value;
                coordinates.HeaderPadding = groupTitle.DesignTitlePadding;
                coordinates.LineRange = groupTitle.DesignLineRange;
            }

            // Určím velikost grupy:
            var designWidth = iGroup.DesignWidth;
            var designHeight = iGroup.DesignHeight;
            if (!(designWidth.HasValue && designHeight.HasValue))
            {   // Některá z hodnot (DesignWidth nebo DesignHeight) není zadaná, musíme ji dopočítat podle aktuálních prvků:
                var sizeOverheads = coordinates.SizeOverhead;
                var itemSummaryBounds = this.Items.Select(i => i.DesignBounds).SummaryVisibleRectangle() ?? Rectangle.Empty;
                if (!designWidth.HasValue)
                    designWidth = itemSummaryBounds.Right + sizeOverheads.Width;
                if (!designHeight.HasValue)
                    designHeight = itemSummaryBounds.Bottom + sizeOverheads.Height;
            }
            coordinates.Size = new Size(designWidth.Value, designHeight.Value);

            // Uložíme souřadnice jako celek a také načteme ContentBounds (=výsledek výpočtu) a uložíme do trvalé proměnné (kvůli rychlosti).
            __DesignCoordinates = coordinates;
            __DesignTitleBounds = coordinates.TitleBounds;
            __DesignContentBounds = coordinates.ContentBounds;
        }
        /// <summary>
        /// Souřadný systém v Design hodnotách (bez Zoomu)
        /// </summary>
        private Coordinates __DesignCoordinates;
        #endregion
        #region Souřadnice aktuální, viditelné
        /// <summary>
        /// Na této souřadnici (reálné) v rámci grupy začíná souřadnice 0/0 jejich prvků.
        /// Tuto hodnotu určuje správce DataFormu při tvorbě layoutu (Statický i dynamický laoyut).
        /// Tvorba layoutu probíhá po každé změně rozměru DataFormu i změně Zoomu a DPI.
        /// <para/>
        /// Po setování této souřadnice proběhne invalidace souřadnic Current i Visible, i jednotlivých prvků.
        /// Následně jsou tyto souřadnice on-demand přepočteny.
        /// </summary>
        public Point CurrentGroupOrigin { get { return __CurrentGroupOrigin; } set { __CurrentGroupOrigin = value; InvalidateBounds(); } } private Point __CurrentGroupOrigin;
        /// <summary>
        /// Reálná velikost grupy v aktuálním Zoomu.
        /// </summary>
        public Size CurrentGroupSize { get { this._CheckCurrentBounds(); return __CurrentGroupSize; } } private Size __CurrentGroupSize;
        /// <summary>
        /// Aktuální reálná absolutní (=v koordinátech vizuálního controlu včetně Zoomu!) souřadnice této grupy. 
        /// Souřadnice je daná počátkem <see cref="CurrentGroupOrigin"/>, který musí setovat koordinátor stránky, 
        /// a velikostí grupy <see cref="CurrentGroupSize"/>, která vychází z deklarace grupy <see cref="IDataFormGroup.DesignWidth"/> a <see cref="IDataFormGroup.DesignHeight"/>, a je přepočtena Zoomem a DPI.
        /// <para/>
        /// Tato souřadnice ale není posunuta ScrollBarem (je absolutní).
        /// Posunutá vizuální souřadnice je v <see cref="VisibleGroupBounds"/>.
        /// </summary>
        public Rectangle CurrentGroupBounds { get { this._CheckCurrentBounds(); return new Rectangle(__CurrentGroupOrigin, __CurrentGroupSize); } }
        /// <summary>
        /// Invaliduje souřadnice <see cref="CurrentGroupSize"/>, <see cref="CurrentGroupBounds"/> a <see cref="VisibleGroupBounds"/>.
        /// Invaliduje i svoje Items.
        /// Invalidují se souřadnice typu Current a Visible. 
        /// Tyto souřadnice budou on-demand přepočteny ze souřadnic typu Design, podle aktuálních hodnot Zoom a DPI.
        /// </summary>
        public void InvalidateBounds()
        {
            __CurrentCoordinatesValid = false;
            __Items.ForEachExec(i => i.InvalidateBounds());
        }
        /// <summary>
        /// Zajistí, že souřadnice <see cref="__CurrentGroupSize"/> a budou platné k souřadnicím designovým a k hodnotám aktuálním DPI
        /// </summary>
        /// <param name="force"></param>
        private void _CheckCurrentBounds(bool force = false)
        {
            if (force || !__CurrentCoordinatesValid || __CurrentCoordinates is null)
            {
                if (__CurrentCoordinates is null) __CurrentCoordinates = new Coordinates();
                __CurrentCoordinatesValid = true;

                __CurrentCoordinates.ZoomToGui(__DesignCoordinates, DataForm.CurrentDpi);

                // Opíšu si sadu výsledků z Coordinates, protože tam jsou "vypočítávané", a my je chceme mít permanentní:
                this.__CurrentGroupSize = __CurrentCoordinates.Size;
                this.__CurrentBorderBounds = __CurrentCoordinates.VisibleBorder ? __CurrentCoordinates.BorderOuterBounds : null;
                this.__CurrentBorderSizes = __CurrentCoordinates.BorderRange?.Size;
                this.__CurrentGroupBackground = __CurrentCoordinates.GroupBackgroundBounds;
                this.__CurrentTitleBackground = __CurrentCoordinates.HasHeader ? __CurrentCoordinates.HeaderBackgroundBounds : null;
                this.__CurrentTitleTextBounds = __CurrentCoordinates.HasHeader ? __CurrentCoordinates.TitleBounds : null;
                this.__CurrentTitleLineBounds = __CurrentCoordinates.VisibleLine ? __CurrentCoordinates.TitleLineBounds : null;
                this.__CurrentContentBackground = __CurrentCoordinates.ContentBackgroundBounds;
                this.__CurrentContentBounds = __CurrentCoordinates.ContentBounds;

                // Naše hodnota souřadnice (this.__CurrentGroupOrigin) je dána externě, musím ji zachovat, vložím ji do __CurrentCoordinates.Location:
                __CurrentCoordinates.Location = this.__CurrentGroupOrigin;

            }
        }
        /// <summary>
        /// Souřadný systém v Current hodnotách (s aplikovaným Zoomem)
        /// </summary>
        private Coordinates __CurrentCoordinates;
        /// <summary>
        /// Hodnoty v <see cref="__CurrentCoordinates"/> jsou platné?
        /// </summary>
        private bool __CurrentCoordinatesValid;
        /// <summary>
        /// Souřadnice borderu. Šířka borderu je <see cref="__CurrentBorderSizes"/>.
        /// Tato hodnota není autovalidována.
        /// </summary>
        private Rectangle? __CurrentBorderBounds;
        /// <summary>
        /// Šířka borderu. Souřadnice borderu je <see cref="__CurrentBorderBounds"/>.
        /// Tato hodnota není autovalidována.
        /// </summary>
        private int? __CurrentBorderSizes;
        /// <summary>
        /// Souřadnice celého pozadí (titulek + content).
        /// Tato hodnota není autovalidována.
        /// </summary>
        private Rectangle? __CurrentGroupBackground;
        /// <summary>
        /// Souřadnice pozadí titulku.
        /// Tato hodnota není autovalidována.
        /// </summary>
        private Rectangle? __CurrentTitleBackground;
        /// <summary>
        /// Souřadnice textu titulku = zmenšeno o Padding od <see cref="__CurrentTitleBackground"/>.
        /// Tato hodnota není autovalidována.
        /// </summary>
        private Rectangle? __CurrentTitleTextBounds;
        /// <summary>
        /// Souřadnice linky podtržení titulku.
        /// Tato hodnota není autovalidována.
        /// </summary>
        private Rectangle? __CurrentTitleLineBounds;
        /// <summary>
        /// Souřadnice pozadí contentu (prvky).
        /// Tato hodnota není autovalidována.
        /// </summary>
        private Rectangle? __CurrentContentBackground;
        /// <summary>
        /// Souřadnice prvků = zmenšeno o Padding od <see cref="__CurrentContentBackground"/>.
        /// Tato hodnota není autovalidována.
        /// </summary>
        private Rectangle? __CurrentContentBounds;
        /// <summary>
        /// Metoda vrátí aktuální viditelnou souřadnici daného Current prostoru.
        /// Na vstupu je souřadnice v koordinátech grupy, na výstupu je v koordinátech controlu.
        /// </summary>
        /// <param name="currentBounds"></param>
        /// <returns></returns>
        private Rectangle? _GetVisibleBounds(Rectangle? currentBounds)
        {
            var visibleGroupBounds = __VisibleGroupBounds;
            if (visibleGroupBounds.HasValue && currentBounds.HasValue) return currentBounds.Value.Add(visibleGroupBounds.Value.Location);
            return null;
        }
        /// <summary>
        /// Fyzické pixelové souřadnice této grupy na vizuálním controlu, kde se nyní tento prvek nachází.
        /// Jde o vizuální souřadnice v koordinátech controlu, odpovídají např. pohybu myši.
        /// Může být null, pak prvek není zobrazen. Null je i po invalidaci <see cref="InvalidateBounds()"/>.
        /// Tuto hodnotu sem ukládá řídící třída v procesu kreslení jako reálné souřadnice, kam byl prvek vykreslen.
        /// </summary>
        public Rectangle? VisibleGroupBounds { get { return __VisibleGroupBounds; } set { __VisibleGroupBounds = value; } } private Rectangle? __VisibleGroupBounds;
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
        /// <summary>
        /// Vrátí true, pokud this grupa má nastaveny viditelné souřadnice v <see cref="VisibleGroupBounds"/> 
        /// a pokud daný bod (souřadný systém shodný s <see cref="VisibleGroupBounds"/>) se nachází v prostoru this grupy
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsVisibleOnPoint(Point point)
        {
            return (IsVisible && VisibleGroupBounds.HasValue && VisibleGroupBounds.Value.Contains(point));
        }
        #endregion
        #region Kreslení grupy (Border, Background, Title)
        /// <summary>
        /// Metoda vykreslí grupu
        /// </summary>
        /// <param name="e"></param>
        /// <param name="onMouse"></param>
        /// <param name="hasFocus"></param>
        internal void PaintGroup(DxBufferedGraphicPaintArgs e, bool onMouse, bool hasFocus)
        {
            if (!this.VisibleGroupBounds.HasValue) return;
            _CheckCurrentBounds();
            _PaintBorder(e, onMouse, hasFocus);
            _PaintBackgrounds(e, onMouse, hasFocus);
        }
        /// <summary>
        /// Vykreslí border
        /// </summary>
        /// <param name="e"></param>
        /// <param name="onMouse"></param>
        /// <param name="hasFocus"></param>
        private void _PaintBorder(DxBufferedGraphicPaintArgs e, bool onMouse, bool hasFocus)
        {
            var appearance = _IGroup.BorderAppearance;
            if (appearance == null) return;

            var bounds = __CurrentBorderBounds;
            if (!bounds.HasValue) return;

            int sizes = __CurrentCoordinates.BorderRange?.Size ?? 0;
            if (sizes <= 0) return;

            DxDataForm.PaintFrame(e, _GetVisibleBounds(bounds), sizes, appearance, onMouse, hasFocus);
        }
        /// <summary>
        /// Vykreslí všechna pozadí ve správném pořadí
        /// </summary>
        /// <param name="e"></param>
        /// <param name="onMouse"></param>
        /// <param name="hasFocus"></param>
        private void _PaintBackgrounds(DxBufferedGraphicPaintArgs e, bool onMouse, bool hasFocus)
        {
            var groupTitle = _IGroup.GroupHeader;
            bool hasTitle = (groupTitle != null);
            if (hasTitle)
                DxDataForm.PaintBackground(e, _GetVisibleBounds(__CurrentTitleBackground), groupTitle.BackgroundAppearance, onMouse, hasFocus, false);     // Pozadí pod titulkem

            DxDataForm.PaintBackground(e, _GetVisibleBounds(__CurrentContentBackground), _IGroup.BackgroundAppearance, onMouse, hasFocus, false);          // Pozadí pod obsahem

            if (hasTitle)
                DxDataForm.PaintBackground(e, _GetVisibleBounds(__CurrentTitleLineBounds), groupTitle.LineAppearance, onMouse, hasFocus, false);           // Linka, nad obrázkem, pod texty

            DxDataForm.PaintImage(e, _GetVisibleBounds(__CurrentGroupBackground), _IGroup.BackgroundAppearance);                                           // Obrázek na pozadí celé grupy

        }
        #endregion
        #region class Coordinates : souřadnice různých míst v grupě
        /// <summary>
        /// Souřadný systém uvnitř grupy, určuje umístění borderu, titulkového prostoru a prostoru pro obsah,
        /// včetně linky v titulku a včetně zachování Padding.
        /// </summary>
        private class Coordinates
        {
            #region Vnější souřadnice a suma režie
            /// <summary>
            /// Souřadnice grupy zvenku = v prostoru Parent controlu
            /// </summary>
            public Point Location { get { return __Location; } set { __Location = value; } } private Point __Location;
            /// <summary>
            /// Velikost grupy celková
            /// </summary>
            public Size Size { get { return __Size; } set { __Size = value; } } private Size __Size;
            /// <summary>
            /// Kompletní vnější souřadnice této grupy relativně k parent controlu
            /// </summary>
            public Rectangle Bounds
            {
                get { return new Rectangle(Location, Size); }
                set
                {
                    Location = value.Location;
                    Size = value.Size;
                }
            }
            /// <summary>
            /// Velikost režie okolo prostoru Content.
            /// Jde o všechny pixely typu Border, Padding, TitleHeight.
            /// Tato velikost se má přidat k velikosti Content.Summary, aby byla vypočtena nejmenší potřebná velikost grupy.
            /// <para/>
            /// Tuto hodnotu je možno číst po zadání <see cref="BorderRange"/>, <see cref="Padding"/>, <see cref="HeaderHeight"/>.
            /// </summary>
            public Size SizeOverhead
            {
                get
                {
                    int bs = 2 * _BorderEnd;
                    int pw = __PaddingLeft + __PaddingRight;
                    int ph = __PaddingTop + __PaddingBottom;
                    var th = HeaderHeight;
                    return new Size(bs + pw, bs + ph + th);
                }
            }
            /*   JAK JE TO SE SOUŘADNÝM SYSTÉMEM:

                1. Grupa deklaruje svoji vnější designovou velikost = IDataFormGroup.DesignWidth a IDataFormGroup.DesignHeight; ale deklarovat to nemusí;
                2. V rámci tohoto prostoru se nachází i Border a Padding (v tomto pořadí), přičemž Padding je uvnitř Borderu
                3. Je na autorovi designu, aby se vnitřní prvky Items svými souřadnicemi vešly do prostoru grupy zmenšenému o Border a Padding.
                4. Grupy jsou skládány pod sebe = grupa 2 má svůj počátek Y přesně pod koncem grupy 1 = na jeho Bottom; na stejné souřadnici X, počínaje bodem { 0, 0 }.
                5. Veškeré souřadnice na vstupu jsou Designové = vztahují se k zoomu 100% a monitoru 96DPI. Reálné souřadnice přepočítává DataForm.

            */
            #endregion
            #region Border a Padding
            /// <summary>
            /// Bude se kreslit Border? To je tehdy, když <see cref="BorderRange"/> má kladnou velikost.
            /// Nicméně i když se Border nekreslí, akceptuje se jeho hodnota <see cref="BorderRange"/>.Begin, o kterou se zmenšuje vnější prostor.
            /// Prostor mezi začátkem grupy (<see cref="Bounds"/>) a začátkem Borderu se nijak nevykresluje, a má tedy barvu a vzhled parent controlu.
            /// </summary>
            public bool VisibleBorder { get { return (__BorderEnd > __BorderBegin); } }
            /// <summary>
            /// Umístění a velikost Borderu, měřeno od samotného okraje grupy směrem dovnitř, bez <see cref="Padding"/>.
            /// Pokud border není viditelný, je zde null.
            /// </summary>
            public Int32Range BorderRange
            {
                get { return (VisibleBorder ? new Int32Range(__BorderBegin, __BorderEnd) : null); }
                set
                {
                    _BorderBegin = (value?.Begin ?? 0);
                    _BorderEnd = (value?.End ?? 0);
                }
            }
            /// <summary>
            /// Pixel, na kterém začíná Border. Nikdy není záporný.
            /// Prostor mezi začátkem grupy a tímto borderem si grupa nevykresluje, prosvítá tam parent Control.
            /// </summary>
            private int _BorderBegin 
            {
                get { return __BorderBegin; }
                set { __BorderBegin = (value < 0 ? 0 : value); }
            }
            private int __BorderBegin;
            /// <summary>
            /// Pixel, na kterém (ve směru dovnitř grupy) končí Border. Nikdy není menší než <see cref="_BorderBegin"/>.
            /// </summary>
            private int _BorderEnd
            {
                get { return (__BorderEnd < __BorderBegin ? __BorderBegin : __BorderEnd); }
                set { __BorderEnd = (value < 0 ? 0 : value); }
            }
            private int __BorderEnd;
            /// <summary>
            /// Šířka linky borderu v pixelech. Pokud je nula, border se fyzicky nekreslí. Nikdy není záporná.
            /// </summary>
            private int _BorderThick { get { return _BorderEnd - _BorderBegin; } }
            /// <summary>
            /// Vnitřní okraje mezi vnitřkem Borderu a začátkem Inner prostoru pro prvky.
            /// Záporné hodnoty jsou nahrazeny 0.
            /// </summary>
            public Padding Padding
            {
                get { return new Padding(__PaddingLeft, __PaddingTop, __PaddingRight, __PaddingBottom); }
                set
                {
                    __PaddingLeft = (value.Left > 0 ? value.Left : 0);
                    __PaddingTop = (value.Top > 0 ? value.Top : 0);
                    __PaddingRight = (value.Right> 0 ? value.Right : 0);
                    __PaddingBottom = (value.Bottom > 0 ? value.Bottom : 0);
                }
            }
            private int __PaddingLeft;
            private int __PaddingTop;
            private int __PaddingRight;
            private int __PaddingBottom;
            /// <summary>
            /// Souřadnice vnějšího Borderu, může být Empty když <see cref="VisibleBorder"/> je false.
            /// Je relativní v rámci grupy.
            /// Je umístěn v prostoru <see cref="Bounds"/> s odstupem <see cref="_BorderBegin"/> od všech čtyř stran.
            /// </summary>
            public Rectangle BorderOuterBounds
            {
                get
                {
                    if (!VisibleBorder) return Rectangle.Empty;
                    int bb = _BorderBegin;
                    return new Rectangle(bb, bb, Size.Width - 2 * bb, Size.Height - 2 * bb);
                }
            }
            /// <summary>
            /// Souřadnice pozadí celé grupy, je přímo uvnitř Borderu (tzn. v rámci tohoto <see cref="GroupBackgroundBounds"/> se nachází i <see cref="Padding"/>)
            /// V tomto prostoru se nahoře nachází <see cref="HeaderBackgroundBounds"/> a dole <see cref="ContentBackgroundBounds"/>.
            /// Relativně k this grupě.
            /// </summary>
            public Rectangle GroupBackgroundBounds
            {
                get
                {
                    int bb = _BorderEnd;
                    return new Rectangle(bb, bb, Size.Width - 2 * bb, Size.Height - 2 * bb);
                }
            }
            #endregion
            #region Header
            /// <summary>
            /// Máme titulek?
            /// </summary>
            public bool HasHeader { get { return (__HeaderHeight > 0); } }
            /// <summary>
            /// Výška prostoru pro titulek. Nikdy není záporná, hodnota 0 = není titulek.
            /// </summary>
            public int HeaderHeight
            {
                get { return __HeaderHeight; }
                set { __HeaderHeight = (value < 0 ? 0 : value); }
            }
            private int __HeaderHeight;
            /// <summary>
            /// Vnitřní okraje mezi vnitřkem Borderu a začátkem Inner prostoru pro prvky.
            /// Záporné hodnoty jsou nahrazeny 0.
            /// </summary>
            public Padding HeaderPadding
            {
                get { return new Padding(__HeaderPaddingLeft, __HeaderPaddingTop, __HeaderPaddingRight, __HeaderPaddingBottom); }
                set
                {
                    __HeaderPaddingLeft = (value.Left > 0 ? value.Left : 0);
                    __HeaderPaddingTop = (value.Top > 0 ? value.Top : 0);
                    __HeaderPaddingRight = (value.Right > 0 ? value.Right : 0);
                    __HeaderPaddingBottom = (value.Bottom > 0 ? value.Bottom : 0);
                }
            }
            private int __HeaderPaddingLeft;
            private int __HeaderPaddingTop;
            private int __HeaderPaddingRight;
            private int __HeaderPaddingBottom;
            /// <summary>
            /// Je viditelná linka v oblasti titulku?
            /// </summary>
            public bool VisibleLine { get { return HasHeader && (_LineYEnd > _LineYBegin); } }
            /// <summary>
            /// Umístění linky v oblasti titulku, měřeno od horního okraje titulku, bez <see cref="Padding"/>.
            /// Pozor, relativně k <see cref="HeaderBackgroundBounds"/>.
            /// Pokud linka není viditelná, je zde null.
            /// </summary>
            public Int32Range LineRange
            {
                get { return (VisibleLine ? new Int32Range(_LineYBegin, _LineYEnd) : null); }
                set
                {
                    __LineYBegin = (value?.Begin ?? 0);
                    __LineYEnd = (value?.End ?? 0);
                }
            }
            /// <summary>
            /// Počátek (Top) linky titulku, zarovnaný do rozmezí 0 až <see cref="HeaderHeight"/> včetně.
            /// Pozor, hodnota je relativně k <see cref="HeaderBackgroundBounds"/>.
            /// </summary>
            private int _LineYBegin { get { int h = this.__HeaderHeight; int b = __LineYBegin; return (b > h ? h : (b < 0 ? 0 : b)); } }
            /// <summary>
            /// Konec (Bottom) linky titulku, zarovnaný do rozmezí <see cref="_LineYBegin"/> až <see cref="HeaderHeight"/> včetně.
            /// Pozor, hodnota je relativně k <see cref="HeaderBackgroundBounds"/>.
            /// </summary>
            private int _LineYEnd { get { int h = this.__HeaderHeight; int b = _LineYBegin; int e = __LineYEnd; return (e > h ? h : (e < b ? b : e)); } }
            private int __LineYBegin;
            private int __LineYEnd;
            /// <summary>
            /// Souřadnice pozadí titulku. 
            /// Nachází se přesně uvnitř <see cref="GroupBackgroundBounds"/>, a má výšku <see cref="HeaderHeight"/>.
            /// Je relativní v rámci this grupy.
            /// <para/>
            /// Tento prostor má být vybarven barvou pozadí a případně obsahuje obrázek pozadí ve vhodném zarovnání.
            /// </summary>
            public Rectangle HeaderBackgroundBounds
            {
                get
                {
                    if (!HasHeader) return Rectangle.Empty;
                    var bounds = GroupBackgroundBounds;
                    var height = HeaderHeight;
                    return new Rectangle(bounds.X, bounds.Y, bounds.Width, height);
                }
            }
            /// <summary>
            /// Prostor, ve kterém je vykreslen text titulku a případné další prvky.
            /// Je umístěn v <see cref="HeaderBackgroundBounds"/> a je oproti němu zmenšen o <see cref="HeaderPadding"/> dovnitř.
            /// Je relativní v rámci this grupy.
            /// </summary>
            public Rectangle TitleBounds
            {
                get
                {
                    if (!HasHeader) return Rectangle.Empty;
                    var bounds = HeaderBackgroundBounds;
                    int pl = __HeaderPaddingLeft;
                    int pt = __HeaderPaddingTop;
                    int pw = pl + __HeaderPaddingRight;
                    int ph = pt + __HeaderPaddingBottom;
                    return new Rectangle(bounds.X + pl, bounds.Y + pt, bounds.Width - pw, bounds.Height - ph);
                }
            }
            /// <summary>
            /// Prostor, ve kterém je vykreslena linka titulku. Může být Empty.
            /// Obecně je umístěn v <see cref="HeaderBackgroundBounds"/>.
            /// Ve směru X je od okrajů odsazen o Padding, ve směru Y nikoliv, ale je umístěn na pixelech <see cref="LineRange"/> vůči souřadnici <see cref="HeaderBackgroundBounds"/>.Y
            /// Je relativní v rámci this grupy.
            /// </summary>
            public Rectangle TitleLineBounds
            {
                get
                {
                    if (!HasHeader || !VisibleLine) return Rectangle.Empty;
                    var bounds = HeaderBackgroundBounds;
                    int lb = _LineYBegin;
                    int le = _LineYEnd;
                    if (le < lb) return Rectangle.Empty;
                    int pl = __PaddingLeft;
                    int pw = pl + __PaddingRight;
                    return new Rectangle(bounds.X + pl, bounds.Y + lb, bounds.Width - pw, le - lb);
                }
            }
            #endregion
            #region Content a Collapsed
            /// <summary>
            /// Souřadnice pozadí vlastního obsahu pod titulkem.
            /// Nachází se přesně uvnitř <see cref="GroupBackgroundBounds"/>, nahoře je zmenšený o prostor titulku a má souřadnici Y = <see cref="HeaderHeight"/>.
            /// Je relativní v rámci this grupy.
            /// <para/>
            /// Tento prostor má být vybarven barvou pozadí a případně obsahuje obrázek pozadí ve vhodném zarovnání.
            /// </summary>
            public Rectangle ContentBackgroundBounds
            {
                get
                {
                    var bounds = GroupBackgroundBounds;
                    var dy = (HasHeader ? HeaderHeight : 0);
                    var height = bounds.Height - dy;
                    return new Rectangle(bounds.X, bounds.Y + dy, bounds.Width, height);
                }
            }
            /// <summary>
            /// Souřadnice vlastního obsahu = jednotlivé prvky grupy.
            /// Nachází se přesně uvnitř <see cref="ContentBackgroundBounds"/>, a je zmenšen o <see cref="Padding"/> dovnitř.
            /// Je relativní v rámci this grupy.
            /// <para/>
            /// Relativně v tomto prostoru se nachází jednotlivé prvky grupy.
            /// </summary>
            public Rectangle ContentBounds
            {
                get
                {
                    var bounds = ContentBackgroundBounds;
                    int pl = __PaddingLeft;
                    int pt = __PaddingTop;
                    int pw = pl + __PaddingRight;
                    int ph = pt + __PaddingBottom;
                    return new Rectangle(bounds.X + pl, bounds.Y + pt, bounds.Width - pw, bounds.Height - ph);
                }
            }
            #endregion
            #region Zoomování
            /// <summary>
            /// Vypočítá svoje vnitřní hodnoty na aktuální, podle dodaných hodnot designových a podle daného DPI.
            /// Přepočte i hodnoty <see cref="Location"/> a <see cref="Size"/>, tedy i <see cref="Bounds"/>.
            /// </summary>
            /// <param name="designCoordinates"></param>
            /// <param name="currentDpi"></param>
            internal void ZoomToGui(Coordinates designCoordinates, int currentDpi)
            {
                this.__HeaderHeight = zoomToGui(designCoordinates.__HeaderHeight);
                this.__BorderBegin = zoomToGui(designCoordinates.__BorderBegin);
                this.__BorderEnd = zoomToGui(designCoordinates.__BorderEnd);
                this.__HeaderPaddingLeft = zoomToGui(designCoordinates.__HeaderPaddingLeft);
                this.__HeaderPaddingTop = zoomToGui(designCoordinates.__HeaderPaddingTop);
                this.__HeaderPaddingRight = zoomToGui(designCoordinates.__HeaderPaddingRight);
                this.__HeaderPaddingBottom = zoomToGui(designCoordinates.__HeaderPaddingBottom);
                this.__LineYBegin = zoomToGui(designCoordinates.__LineYBegin);
                this.__LineYEnd = zoomToGui(designCoordinates.__LineYEnd);
                this.__PaddingLeft = zoomToGui(designCoordinates.__PaddingLeft);
                this.__PaddingTop = zoomToGui(designCoordinates.__PaddingTop);
                this.__PaddingRight = zoomToGui(designCoordinates.__PaddingRight);
                this.__PaddingBottom = zoomToGui(designCoordinates.__PaddingBottom);

                this.__Location = DxComponent.ZoomToGui(designCoordinates.__Location, currentDpi);
                this.__Size = DxComponent.ZoomToGui(designCoordinates.__Size, currentDpi);

                int zoomToGui(int designValue)
                {
                    return (designValue != 0 ? DxComponent.ZoomToGui(designValue, currentDpi) : 0);
                }
            }
            #endregion
        }
        #endregion
    }
    #endregion
    #region class DxDataFormColumn : Třída reprezentující definici jednoho prvku odpovídající sloupci v DxDataFormu
    /// <summary>
    /// Třída reprezentující informace o sloupci v <see cref="DxDataForm"/>.
    /// Sloupec je myšleno ve smyslu vztahu k datové tabulce.
    /// S ohledem na rozmístění prvků v rámci <see cref="DxDataForm"/> nejde o "sloupec prvků pod sebou" 
    /// ale o definici jednoho viditelného prvku, který se může opakovat pro jednotlivé řádky.
    /// </summary>
    internal class DxDataFormColumn
    {
        #region Konstruktor, vlastník, prvky
        /// <summary>
        /// Vytvoří a vrátí List obsahující <see cref="DxDataFormColumn"/>, vytvořené z dodaných instancí <see cref="IDataFormColumn"/>.
        /// </summary>
        /// <param name="dataGroup"></param>
        /// <param name="isTitleItems">false pro běžné columny v prostoru Content, true pro columny v prostoru Title (=text titulku, ikony, atd)</param>
        /// <param name="iItems"></param>
        /// <returns></returns>
        public static List<DxDataFormColumn> CreateList(DxDataFormGroup dataGroup, bool isTitleItems, IEnumerable<IDataFormColumn> iItems)
        {
            List<DxDataFormColumn> dxItems = new List<DxDataFormColumn>();
            AddToList(dataGroup, isTitleItems, iItems, dxItems);
            return dxItems;
        }
        /// <summary>
        /// Naplní do dodaného Listu prvky <see cref="DxDataFormColumn"/> vytvořené z dodaných instancí <see cref="IDataFormColumn"/>.
        /// </summary>
        /// <param name="dataGroup"></param>
        /// <param name="isTitleItems">false pro běžné columny v prostoru Content, true pro columny v prostoru Title (=text titulku, ikony, atd)</param>
        /// <param name="iItems"></param>
        /// <param name="dxItems"></param>
        /// <returns></returns>
        public static void AddToList(DxDataFormGroup dataGroup, bool isTitleItems, IEnumerable<IDataFormColumn> iItems, List<DxDataFormColumn> dxItems)
        {
            if (iItems != null)
            {
                foreach (IDataFormColumn iItem in iItems)
                {
                    if (iItem == null) continue;
                    dxItems.Add(new DxDataFormColumn(dataGroup, isTitleItems, iItem));
                }
            }
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataGroup"></param>
        /// <param name="isTitleItem">false pro běžné columny v prostoru Content, true pro columny v prostoru Title (=text titulku, ikony, atd)</param>
        /// <param name="iItem"></param>
        public DxDataFormColumn(DxDataFormGroup dataGroup, bool isTitleItem, IDataFormColumn iItem)
            : base()
        {
            __DataGroup = dataGroup;
            __IsTitleItem = isTitleItem;
            __IItem = iItem;
        }
        /// <summary>Vlastník - <see cref="DxDataFormGroup"/></summary>
        private DxDataFormGroup __DataGroup;
        /// <summary>Hodnota false = prvek je umístěn v prostoru Content / Hodnota true = prvek je umístěn v prostoru Title</summary>
        private bool __IsTitleItem;
        /// <summary>Deklarace prvku</summary>
        private IDataFormColumn __IItem;
        /// <summary>
        /// Vlastník - <see cref="DxDataForm"/>
        /// </summary>
        public DxDataForm DataForm { get { return this.__DataGroup?.DataPage?.DataForm; } }
        /// <summary>
        /// Vlastník - <see cref="DxDataFormPage"/>
        /// </summary>
        public DxDataFormPage DataPage { get { return __DataGroup?.DataPage; } }
        /// <summary>
        /// Vlastník - <see cref="DxDataFormGroup"/>
        /// </summary>
        public DxDataFormGroup DataGroup { get { return __DataGroup; } }
        /// <summary>Hodnota false = prvek je umístěn v prostoru Content / Hodnota true = prvek je umístěn v prostoru Title</summary>
        public bool IsTitleItem { get { return __IsTitleItem; } }
        /// <summary>
        /// Deklarace prvku
        /// </summary>
        public IDataFormColumn IItem { get { return __IItem; } }
        #endregion
        #region Data z prvku
        /// <summary>
        /// Typ prvku
        /// </summary>
        public DataFormColumnType ItemType { get { return __IItem.ColumnType; } }
        /// <summary>
        /// Prvek je viditelný
        /// </summary>
        public bool IsVisible { get { return IItem.IsVisible; } }
        /// <summary>
        /// Řízení barevných indikátorů u prvku
        /// </summary>
        public DataFormColumnIndicatorType Indicators { get { return IItem.Indicators; } }
        /// <summary>
        /// Metoda zkusí vrátit deklaraci dat (prvek <see cref="IItem"/>) typovaný na daný interface.
        /// To je nutné pro zpracování konkrétního typu dat, když si nevystačíme s obecným rozhraním <see cref="IDataFormColumn"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="iItem"></param>
        /// <returns></returns>
        public bool TryGetIItem<T>(out T iItem)
        {
            if (__IItem is T item)
            {
                iItem = item;
                return true;
            }
            iItem = default;
            return false;
        }
        #endregion
        #region Souřadnice designové, aktuální, viditelné
        /// <summary>
        /// Souřadnice designové, v logických koordinátech (kde bod {0,0} je počátek datového prostoru zdejší grupy, bez posunu ScrollBarem).
        /// Typicky se vztahují k 96 DPI a Zoom 100%.
        /// Hodnota se přebírá z datového prvku <see cref="IItem"/>.
        /// </summary>
        public Rectangle DesignBounds { get { return IItem.DesignBounds; } }
        /// <summary>
        /// Aktuální viditelné koordináty - přepočtené z <see cref="DesignBounds"/> na aktuálně platné DPI a Zoom.
        /// <para/>
        /// Jde o souřadnici absolutní v rámci <see cref="DxDataFormPanel"/>, tedy nejde o souřadnici relativní vzhledem ke grupě, kam prvek patří; 
        /// tato souřadnice <see cref="CurrentBounds"/> již zahrnuje posun o počátek grupy <see cref="DxDataFormGroup.CurrentGroupOrigin"/>.
        /// <para/>
        /// Tato souřadnice ale není posunuta ScrollBarem (je absolutní).
        /// Posunutá vizuální souřadnice je v <see cref="VisibleBounds"/>.
        /// </summary>
        public Rectangle CurrentBounds { get { this.CheckCurrentBounds(); return __CurrentBounds.Value; } } private Rectangle? __CurrentBounds;
        /// <summary>
        /// Aktuální velikost prvku. Lze setovat (nezmění se umístění = <see cref="CurrentBounds"/>.Location).
        /// <para/>
        /// Setujme opatrně a jen v případě nutné potřeby, typicky tehdy, když konkrétní vizuální control nechce akceptovat předepsanou velikost (např. výška textboxu v jiném než očekávaném fontu).
        /// Vložená hodnota zde zůstane (a bude obsažena i v <see cref="CurrentBounds"/>) do doby invalidace = než se změní Zoom nebo Skin aplikace.
        /// </summary>
        public Size CurrentSize
        {
            get { this.CheckCurrentBounds(); return __CurrentBounds.Value.Size; }
            set
            {
                this.CheckCurrentBounds();
                __CurrentBounds = new Rectangle(__CurrentBounds.Value.Location, value);
                if (__VisibleBounds.HasValue)
                    __VisibleBounds = new Rectangle(__VisibleBounds.Value.Location, value);
            }
        }
        /// <summary>
        /// Invaliduje souřadnice <see cref="CurrentBounds"/> a <see cref="VisibleBounds"/>.
        /// </summary>
        public void InvalidateBounds()
        {
            __CurrentBounds = null;
            __VisibleBounds = null;
        }
        /// <summary>
        /// Zajistí, že souřadnice <see cref="__CurrentBounds"/> budou platné k souřadnicím designovým a k hodnotám aktuálním DPI
        /// </summary>
        private void CheckCurrentBounds()
        {
            if (!__CurrentBounds.HasValue)
            {
                if (!__IsTitleItem)
                {   // Běžný prvek v rámci prostoru Content:
                    var designBounds = this.DesignBounds.Add(this.DataGroup.DesignContentBounds.Location);       // Posunutí souřadnic o různé okraje grupy (Border, Title, Padding) vlevo a nahoře
                    var currentRelativeBounds = DxComponent.ZoomToGui(designBounds, DataForm.CurrentDpi);        // Přepočet pomocí Zoomu a DPI
                    __CurrentBounds = currentRelativeBounds.Add(this.DataGroup.CurrentGroupOrigin);              // Posunutí o reálný počátek parent grupy
                }
                else
                {   // Titulkový prvek v rámci prostoru Title:
                    var designBounds = this.DesignBounds.Add(this.DataGroup.DesignTitleBounds.Location);         // Posunutí souřadnic o různé okraje grupy (Border, TitlePadding) vlevo a nahoře
                    var currentRelativeBounds = DxComponent.ZoomToGui(designBounds, DataForm.CurrentDpi);        // Přepočet pomocí Zoomu a DPI
                    __CurrentBounds = currentRelativeBounds.Add(this.DataGroup.CurrentGroupOrigin);              // Posunutí o reálný počátek parent grupy
                }
            }
        }
        /// <summary>
        /// Fyzické pixelové souřadnice tohoto prvku na vizuálním controlu, kde se nyní tento prvek nachází.
        /// Jde o vizuální souřadnice v koordinátech controlu, odpovídají např. pohybu myši.
        /// Může být null, pak prvek není zobrazen. Null je i po invalidaci <see cref="InvalidateBounds()"/>.
        /// Tuto hodnotu ukládá řídící třída v procesu kreslení jako reálné souřadnice, kam byl prvek vykreslen.
        /// </summary>
        public Rectangle? VisibleBounds { get { return __VisibleBounds; } set { __VisibleBounds = value; } } private Rectangle? __VisibleBounds;
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
    #endregion
    #region class DxDataFormControlSet : správce několika vizuálních controlů jednoho druhu, jejich tvorba, a příprava k použití
    /// <summary>
    /// Instance třídy, která obhospodařuje jeden typ <see cref="DataFormColumnType"/> vizuálního controlu, 
    /// a má ve své evidenci až tři instance (Draw, Mouse, Focus)
    /// </summary>
    internal class DxDataFormControlSet : IDisposable
    {
        #region Konstruktor
        /// <summary>
        /// Vytvoří <see cref="DxDataFormControlSet"/> pro daný typ controlu
        /// </summary>
        /// <param name="dataForm"></param>
        /// <param name="itemType"></param>
        public DxDataFormControlSet(DxDataForm dataForm, DataFormColumnType itemType)
        {
            _DataForm = dataForm;
            _ItemType = itemType;
            _CanPaintByPainter = false;
            _CanPaintByImage = true;
            _CanPaintByControl = false;
            _CanCreateControl = true;
            switch (itemType)
            {
                case DataFormColumnType.Label:
                    _CreateControlFunction = _LabelCreate;
                    _GetKeyFunction = _LabelGetKey;
                    _FillControlAction = _LabelFill;
                    _ReadControlAction = _LabelRead;
                    _CanPaintByPainter = true;
                    _CanCreateControl = false;
                    break;
                case DataFormColumnType.TextBox:
                    _CreateControlFunction = _TextBoxCreate;
                    _GetKeyFunction = _TextBoxGetKey;
                    _FillControlAction = _TextBoxFill;
                    _ReadControlAction = _TextBoxRead;
                    break;
                case DataFormColumnType.TextBoxButton:
                    _CreateControlFunction = _TextBoxButtonCreate;
                    _GetKeyFunction = _TextBoxButtonGetKey;
                    _FillControlAction = _TextBoxButtonFill;
                    _ReadControlAction = _TextBoxButtonRead;
                    break;
                case DataFormColumnType.EditBox:
                    _CreateControlFunction = _EditBoxCreate;
                    _GetKeyFunction = _EditBoxGetKey;
                    _FillControlAction = _EditBoxFill;
                    _ReadControlAction = _EditBoxRead;
                    break;
                case DataFormColumnType.CheckBox:
                    _CreateControlFunction = _CheckBoxCreate;
                    _GetKeyFunction = _CheckBoxGetKey;
                    _FillControlAction = _CheckBoxFill;
                    _ReadControlAction = _CheckBoxRead;
                    break;
                case DataFormColumnType.ComboBoxList:
                    _CreateControlFunction = _ComboBoxListCreate;
                    _GetKeyFunction = _ComboBoxListGetKey;
                    _FillControlAction = _ComboBoxListFill;
                    _ReadControlAction = _ComboBoxListRead;
                    break;
                case DataFormColumnType.ComboBoxEdit:
                    _CreateControlFunction = _ComboBoxEditCreate;
                    _GetKeyFunction = _ComboBoxEditGetKey;
                    _FillControlAction = _ComboBoxEditFill;
                    _ReadControlAction = _ComboBoxEditRead;
                    break;
                case DataFormColumnType.TokenEdit:
                    _CreateControlFunction = _TokenEditCreate;
                    _GetKeyFunction = _TokenEditGetKey;
                    _FillControlAction = _TokenEditFill;
                    _ReadControlAction = _TokenEditRead;
                    break;
                case DataFormColumnType.Button:
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
            DisposeControl(ref _ControlDraw, DxDataFormControlUseMode.Draw);
            DisposeControl(ref _ControlMouse, DxDataFormControlUseMode.Mouse);
            DisposeControl(ref _ControlFocus, DxDataFormControlUseMode.Focus);

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
        private DataFormColumnType _ItemType;
        private Func<Control> _CreateControlFunction;
        private Func<DxDataFormColumn, string> _GetKeyFunction;
        private Action<DxDataFormColumn, Control, DxDataFormControlUseMode> _FillControlAction;
        private Action<DxDataFormColumn, Control> _ReadControlAction;
        private bool _Disposed;
        #endregion
        #region Label
        private Control _LabelCreate() { return new DxLabelControl() { AutoSizeMode = LabelAutoSizeMode.None }; }
        private string _LabelGetKey(DxDataFormColumn item)
        {
            string key = GetStandardKeyForItem(item);
            return key;
        }
        private void _LabelFill(DxDataFormColumn item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxLabelControl label)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxLabelControl).Name}.");
            CommonFill(item, label, mode, _LabelFillNext);
        }
        private void _LabelFillNext(DxDataFormColumn item, DxLabelControl label, DxDataFormControlUseMode mode)
        {
            //label.LineStyle = System.Drawing.Drawing2D.DashStyle.Solid;
            //label.LineOrientation = LabelLineOrientation.Horizontal;
            //label.LineColor = Color.Violet;
            //label.LineVisible = true;
        }
        private void _LabelRead(DxDataFormColumn item, Control control)
        { }
        #endregion
        #region TextBox
        private Control _TextBoxCreate() { return new DxTextEdit(); }
        private string _TextBoxGetKey(DxDataFormColumn item)
        {
            string key = GetStandardKeyForItem(item);
            return key;
        }
        private void _TextBoxFill(DxDataFormColumn item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxTextEdit textEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxTextEdit).Name}.");
            CommonFill(item, textEdit, mode);
            textEdit.DeselectAll();
            textEdit.SelectionStart = 0;
        }
        private void _TextBoxRead(DxDataFormColumn item, Control control)
        { }
        #endregion
        #region TextBoxButton
        private Control _TextBoxButtonCreate() { return new DxButtonEdit(); }
        private string _TextBoxButtonGetKey(DxDataFormColumn item)
        {
            string key = GetStandardKeyForItem(item, _TextBoxButtonGetKeySpec);
            return key;
        }
        private string _TextBoxButtonGetKeySpec(DxDataFormColumn item)
        {
            if (!item.TryGetIItem<IDataFormColumnTextBoxButton>(out var iItem)) return "";
            string key =
                (iItem.ButtonsVisibleAllways ? "A" : "a") +
                (iItem.ButtonAs3D ? "D" : "F") +
                (((int)iItem.ButtonKind) + 20).ToString();
            return key;
        }
        private void _TextBoxButtonFill(DxDataFormColumn item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxButtonEdit buttonEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxButtonEdit).Name}.");
            CommonFill(item, buttonEdit, mode, _TextBoxButtonFillSpec);
            //  textEdit.DeselectAll();
            buttonEdit.SelectionStart = 0;
        }
        private void _TextBoxButtonFillSpec(DxDataFormColumn item, DxButtonEdit buttonEdit, DxDataFormControlUseMode mode)
        {
            buttonEdit.SelectionStart = 0;
            buttonEdit.SelectionLength = 0;
            if (item.TryGetIItem<IDataFormColumnTextBoxButton>(out var iItem))
            {
                bool isNone = (iItem.ButtonKind == DataFormButtonKind.None);
                buttonEdit.ButtonsVisibility = (isNone ? DxChildControlVisibility.None :
                            (iItem.ButtonsVisibleAllways ? DxChildControlVisibility.Allways :
                            (mode == DxDataFormControlUseMode.Draw ? DxChildControlVisibility.None : DxChildControlVisibility.OnActiveControl)));
                buttonEdit.ButtonKind = ConvertButtonKind(iItem.ButtonKind);
                buttonEdit.ButtonsStyle = (iItem.ButtonAs3D ? DevExpress.XtraEditors.Controls.BorderStyles.Style3D : DevExpress.XtraEditors.Controls.BorderStyles.HotFlat); // HotFlat je nejlepší; ujde i Style3D; i UltraFlat;     testováno Default; Flat; NoBorder; Office2003; Simple; 
            }
        }
        private void _TextBoxButtonRead(DxDataFormColumn item, Control control)
        { }
        /// <summary>
        /// Vrací DevExpress hodnotu typu <see cref="DevExpress.XtraEditors.Controls.ButtonPredefines"/>
        /// z DataForm hodnoty typu <see cref="DataFormButtonKind"/>.
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        private static DevExpress.XtraEditors.Controls.ButtonPredefines ConvertButtonKind(DataFormButtonKind kind)
        {
            if (kind == DataFormButtonKind.None) return DevExpress.XtraEditors.Controls.ButtonPredefines.Separator;
            if (kind == DataFormButtonKind.Glyph) return DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
            return (DevExpress.XtraEditors.Controls.ButtonPredefines)((int)kind);        // Ostatní hodnoty jsou numericky shodné na obou stranách...
        }
        #endregion
        #region EditBox
        private Control _EditBoxCreate() { return new DxMemoEdit(); }
        private string _EditBoxGetKey(DxDataFormColumn item)
        {
            string key = GetStandardKeyForItem(item);
            return key;
        }
        private void _EditBoxFill(DxDataFormColumn item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxMemoEdit memoEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxMemoEdit).Name}.");
            CommonFill(item, memoEdit, mode);
            memoEdit.DeselectAll();
            memoEdit.SelectionStart = 0;
        }
        private void _EditBoxRead(DxDataFormColumn item, Control control)
        { }
        #endregion
        // SpinnerBox
        #region CheckBox
        private Control _CheckBoxCreate() { return new DxCheckEdit(); }
        private string _CheckBoxGetKey(DxDataFormColumn item)
        {
            string key = GetStandardKeyForItem(item);
            return key;
        }
        private void _CheckBoxFill(DxDataFormColumn item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxCheckEdit checkEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxCheckEdit).Name}.");
            CommonFill(item, checkEdit, mode);
        }
        private void _CheckBoxRead(DxDataFormColumn item, Control control)
        { }
        #endregion
        // BreadCrumb
        #region ComboBoxList
        private Control _ComboBoxListCreate() { return new DxTextEdit(); }
        private string _ComboBoxListGetKey(DxDataFormColumn item)
        {
            string key = GetStandardKeyForItem(item);
            return key;
        }
        private void _ComboBoxListFill(DxDataFormColumn item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxTextEdit textEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxTextEdit).Name}.");
            //  CommonFill(item, textEdit, mode);
            //  textEdit.DeselectAll();
            textEdit.SelectionStart = 0;
        }
        private void _ComboBoxListRead(DxDataFormColumn item, Control control)
        { }
        #endregion
        #region ComboBoxEdit
        private Control _ComboBoxEditCreate() { return new DxTextEdit(); }
        private string _ComboBoxEditGetKey(DxDataFormColumn item)
        {
            string key = GetStandardKeyForItem(item);
            return key;
        }
        private void _ComboBoxEditFill(DxDataFormColumn item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxTextEdit textEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxTextEdit).Name}.");
            //  CommonFill(item, textEdit, mode);
            //  textEdit.DeselectAll();
            textEdit.SelectionStart = 0;
        }
        private void _ComboBoxEditRead(DxDataFormColumn item, Control control)
        { }
        #endregion
        #region TokenEdit
        private Control _TokenEditCreate() { return new DxTokenEdit(); }
        private string _TokenEditGetKey(DxDataFormColumn item)
        {
            string key = GetStandardKeyForItem(item);
            return key;
        }
        private void _TokenEditFill(DxDataFormColumn item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxTokenEdit tokenEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxTokenEdit).Name}.");
            CommonFill(item, tokenEdit, mode, _TokenEditFillSpec);
        }
        private void _TokenEditFillSpec(DxDataFormColumn item, DxTokenEdit tokenEdit, DxDataFormControlUseMode mode)
        {
            bool fullFill = (mode == DxDataFormControlUseMode.Mouse || mode == DxDataFormControlUseMode.Focus);
            if (fullFill && item.TryGetIItem(out IDataFormColumnMenuText iItemMenuText))
            {
                tokenEdit.Tokens = iItemMenuText?.MenuItems;
            }
            if (tokenEdit.SelectedItems.Count == 0 && tokenEdit.Properties.Tokens.Count > 0)
                // tokenEdit.EditValue = tokenEdit.Properties.Tokens[0].Value;
                tokenEdit.EditValue = tokenEdit.Properties.Tokens[0].Value.ToString() + ", " + tokenEdit.Properties.Tokens[1].Value.ToString();
        }
        private void _TokenEditRead(DxDataFormColumn item, Control control)
        { }
        #endregion
        // ListView
        // TreeView
        // RadioButtonBox
        #region Button
        private Control _ButtonCreate() { return new DxSimpleButton(); }
        private string _ButtonGetKey(DxDataFormColumn item)
        {
            string key = GetStandardKeyForItem(item);
            return key;
        }
        private void _ButtonFill(DxDataFormColumn item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxSimpleButton button)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxSimpleButton).Name}.");
            CommonFill(item, button, mode);
        }
        private void _ButtonRead(DxDataFormColumn item, Control control)
        { }
        #endregion
        // CheckButton
        // DropDownButton
        // Image
        #region Společné metody pro získání klíče
        /// <summary>
        /// Vrátí standardní klíč daného prvku do ImageCache
        /// </summary>
        /// <param name="item"></param>
        /// <param name="specKeygenerator"></param>
        /// <returns></returns>
        private static string GetStandardKeyForItem(DxDataFormColumn item, Func<DxDataFormColumn, string> specKeygenerator = null)
        {
            var size = item.CurrentBounds.Size;
            string text = "";
            if (item.IItem is IDataFormColumnImageText iit)
                text = iit.Text;
            string type = ((int)item.ItemType).ToString();
            string appearance = GetAppearanceKey(item.IItem.Appearance);
            string specKey = (specKeygenerator != null ? specKeygenerator(item) ?? "" : "");
            string key = $"{size.Width}.{size.Height};{type}:{appearance}:{specKey}:{text}";
            return key;
        }
        /// <summary>
        /// Vrátí klíč pro danou Appearance
        /// </summary>
        /// <param name="appearance"></param>
        /// <returns></returns>
        private static string GetAppearanceKey(IDataFormColumnAppearance appearance)
        {
            if (appearance == null) return "";
            string text = "";
            if (appearance.FontSizeDelta.HasValue) text += appearance.FontSizeDelta.ToString();
            if (appearance.FontStyleBold.HasValue) text += appearance.FontStyleBold.Value ? "B" : "b";
            if (appearance.FontStyleItalic.HasValue) text += appearance.FontStyleItalic.Value ? "I" : "i";
            if (appearance.FontStyleUnderline.HasValue) text += appearance.FontStyleUnderline.Value ? "U" : "u";
            if (appearance.FontStyleStrikeOut.HasValue) text += appearance.FontStyleStrikeOut.Value ? "S" : "s";
            if (appearance.BackColor.HasValue) text += "G" + GetColorKey(appearance.BackColor.Value);
            if (appearance.ForeColor.HasValue) text += "F" + GetColorKey(appearance.ForeColor.Value);
            if (appearance.ContentAlignment.HasValue) text += "A" + ((int)appearance.ContentAlignment.Value).ToString("X4");
            return text;
        }
        /// <summary>
        /// Vrátí klíč pro danou barvu
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private static string GetColorKey(Color color)
        {
            return color.A.ToString("X2") + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }
        #endregion
        #region Společné metody pro naplnění prvku daty a Appearancí
        /// <summary>
        /// Naplní obecně platné hodnoty do daného controlu.
        /// Nastavuje: Souřadnice, Text, Enabled, Appearance, ToolTip, Visible.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="control"></param>
        /// <param name="mode"></param>
        /// <param name="specificFillMethod"></param>
        private void CommonFill<T>(DxDataFormColumn item, T control, DxDataFormControlUseMode mode, Action<DxDataFormColumn, T, DxDataFormControlUseMode> specificFillMethod = null) where T : BaseControl
        {
            // Určím fyzické umístění controlu: pro Draw je dávám na konstantní souřadnici 4/4 (Draw controly se nacházejí na spodním panelu a nejsou vidět):
            bool isDraw = (mode == DxDataFormControlUseMode.Draw || !item.VisibleBounds.HasValue);
            Rectangle bounds = new Rectangle((isDraw ? new Point(4, 4) : item.VisibleBounds.Value.Location), item.CurrentBounds.Size);

            if (item.IItem is IDataFormColumnImageText iit)
                control.Text = iit.Text;
            control.Enabled = true; // item.Enabled;
            control.SetBounds(bounds);
            // control.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Default;

            ApplyAppearance(control, item.IItem.Appearance);

            if (mode != DxDataFormControlUseMode.Draw)
                control.SuperTip = GetSuperTip(item, mode);

            if (specificFillMethod != null) specificFillMethod(item, control, mode);

            control.Visible = true;
        }
        /// <summary>
        /// Aplikuje vzhled definovaný v <paramref name="iAppearance"/> do daného controlu
        /// </summary>
        /// <param name="control"></param>
        /// <param name="iAppearance"></param>
        private void ApplyAppearance(BaseControl control, IDataFormColumnAppearance iAppearance)
        {
            if (control is BaseStyleControl baseStyleControl)
            {
                var cAppearance = baseStyleControl.Appearance;
                if (cAppearance.Name == "Modified")
                {   // Resetovat jen pokud je nutno - a to bez ohledu na stav zadání appearance:
                    cAppearance.Reset();
                    cAppearance.TextOptions.Reset();
                    cAppearance.Name = "Default";
                }
                if (iAppearance != null)
                {
                    if (iAppearance.FontSizeDelta.HasValue)
                        cAppearance.FontSizeDelta = iAppearance.FontSizeDelta.Value;
                    if (iAppearance.FontStyleBold.HasValue || iAppearance.FontStyleItalic.HasValue || iAppearance.FontStyleUnderline.HasValue || iAppearance.FontStyleStrikeOut.HasValue)
                        cAppearance.FontStyleDelta = ConvertFontStyle(iAppearance);
                    if (iAppearance.ContentAlignment.HasValue)
                        ApplyAlignment(cAppearance, iAppearance);

                    cAppearance.Name = "Modified";
                }
            }
        }
        /// <summary>
        /// Vrátí styl <see cref="FontStyle"/> vytvořený z dané appearance <see cref="IDataFormColumnAppearance"/>
        /// </summary>
        /// <param name="appearance"></param>
        /// <returns></returns>
        private FontStyle ConvertFontStyle(IDataFormColumnAppearance appearance)
        {
            FontStyle fontStyle = FontStyle.Regular;
            if (appearance.FontStyleBold.HasValue && appearance.FontStyleBold.Value) fontStyle |= FontStyle.Bold;
            if (appearance.FontStyleItalic.HasValue && appearance.FontStyleItalic.Value) fontStyle |= FontStyle.Italic;
            if (appearance.FontStyleUnderline.HasValue && appearance.FontStyleUnderline.Value) fontStyle |= FontStyle.Underline;
            if (appearance.FontStyleStrikeOut.HasValue && appearance.FontStyleStrikeOut.Value) fontStyle |= FontStyle.Strikeout;
            return fontStyle;
        }
        /// <summary>
        /// Do daného 
        /// </summary>
        /// <param name="cAppearance"></param>
        /// <param name="iAppearance"></param>
        private void ApplyAlignment(DevExpress.Utils.AppearanceObject cAppearance, IDataFormColumnAppearance iAppearance)
        {
            if (!iAppearance.ContentAlignment.HasValue) return;
            switch (iAppearance.ContentAlignment.Value)
            {
                case ContentAlignment.TopLeft:
                    cAppearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
                    cAppearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
                    break;
                case ContentAlignment.TopCenter:
                    cAppearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
                    cAppearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                    break;
                case ContentAlignment.TopRight:
                    cAppearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
                    cAppearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
                    break;
                case ContentAlignment.MiddleLeft:
                    cAppearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
                    cAppearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
                    break;
                case ContentAlignment.MiddleCenter:
                    cAppearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
                    cAppearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                    break;
                case ContentAlignment.MiddleRight:
                    cAppearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
                    cAppearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
                    break;
                case ContentAlignment.BottomLeft:
                    cAppearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Bottom;
                    cAppearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
                    break;
                case ContentAlignment.BottomCenter:
                    cAppearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Bottom;
                    cAppearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                    break;
                case ContentAlignment.BottomRight:
                    cAppearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Bottom;
                    cAppearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
                    break;
            }
        }
        /// <summary>
        /// Vrátí instanci <see cref="DxSuperToolTip"/> připravenou pro daný prvek a daný režim. Může vrátit null.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        private DxSuperToolTip GetSuperTip(DxDataFormColumn item, DxDataFormControlUseMode mode)
        {
            if (mode != DxDataFormControlUseMode.Mouse) return null;
            var superTip = _DataForm.DxSuperToolTip;
            superTip.LoadValues(item.IItem);
            if (!superTip.IsValid) return null;
            return superTip;
        }
        #endregion
        #region Získání a naplnění controlu z datového Itemu, a reverzní zpětné načtení hodnot z controlu do datového Itemu
        /// <summary>
        /// Typ prvku, který je popsán touto sadou
        /// </summary>
        internal DataFormColumnType ItemType { get { return _ItemType; } }
        /// <summary>
        /// Může být pro tento typ controlu použit pro režim Draw (vykreslení prvku bez myši) použit Painter namísto vlastního Controlu?
        /// </summary>
        internal bool CanPaintByPainter { get { return _CanPaintByPainter; } }
        private bool _CanPaintByPainter;
        /// <summary>
        /// Může být pro tento typ controlu použit pro režim Draw (vykreslení prvku bez myši) Control pro vytvoření statického Image?
        /// </summary>
        internal bool CanPaintByImage { get { return _CanPaintByImage; } }
        private bool _CanPaintByImage;
        internal bool CanPaintByControl { get { return _CanPaintByControl; } }
        private bool _CanPaintByControl;
        /// <summary>
        /// Může být pro tento typ controlu vytvářena instance pro režim Mouse a Focus ?
        /// </summary>
        internal bool CanCreateControl { get { return _CanCreateControl; } }
        private bool _CanCreateControl;

        /// <summary>
        /// Vrátí control pro daný prvek a režim
        /// </summary>
        /// <param name="item"></param>
        /// <param name="mode"></param>
        /// <param name="parent">Parent, do něhož má být control umístěn. Pokud režim <paramref name="mode"/> je <see cref="DxDataFormControlUseMode.Draw"/>, pak parent smí být null.</param>
        /// <returns></returns>
        internal Control GetControlForMode(DxDataFormColumn item, DxDataFormControlUseMode mode, Control parent)
        {
            switch (mode)
            {
                case DxDataFormControlUseMode.Draw: return GetControlForDraw(item);
                case DxDataFormControlUseMode.Mouse: return GetControlForMouse(item, parent);
                case DxDataFormControlUseMode.Focus: return GetControlForFocus(item, parent);
            }
            return null;
        }
        /// <summary>
        /// Vrátí control pro daný prvek a režim Draw
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal Control GetControlForDraw(DxDataFormColumn item)
        {
            CheckNonDisposed();
            if (_ControlDraw == null)
                _ControlDraw = _CreateControl(DxDataFormControlUseMode.Draw);
            _FillControl(item, _ControlDraw, DxDataFormControlUseMode.Draw, null);
            return _ControlDraw;
        }
        /// <summary>
        /// Vrátí control pro daný prvek a režim Mouse
        /// </summary>
        /// <param name="item"></param>
        /// <param name="parent">Parent, do něhož má být control umístěn</param>
        /// <returns></returns>
        internal Control GetControlForMouse(DxDataFormColumn item, Control parent)
        {
            CheckNonDisposed();
            if (_ControlMouse == null)
                _ControlMouse = _CreateControl(DxDataFormControlUseMode.Mouse);
            _FillControl(item, _ControlMouse, DxDataFormControlUseMode.Mouse, parent);
            return _ControlMouse;
        }
        /// <summary>
        /// Vrátí control pro daný prvek a režim Focus
        /// </summary>
        /// <param name="item"></param>
        /// <param name="parent">Parent, do něhož má být control umístěn</param>
        /// <returns></returns>
        internal Control GetControlForFocus(DxDataFormColumn item, Control parent)
        {
            CheckNonDisposed();
            if (_ControlFocus == null)
                _ControlFocus = _CreateControl(DxDataFormControlUseMode.Focus);
            _FillControl(item, _ControlFocus, DxDataFormControlUseMode.Focus, parent);
            return _ControlFocus;
        }
        /// <summary>
        /// Metoda vrátí stringový klíč do ImageCache pro konkrétní prvek.
        /// Vrácený klíč zohledňuje všechny potřebné a specifické hodnoty z konkrétního prvku.
        /// Je tedy jisté, že dva různé objekty, které vrátí stejný klíč, budou mít stejný vzhled.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal string GetKeyToCache(DxDataFormColumn item)
        {
            CheckNonDisposed();
            string key = _GetKeyFunction?.Invoke(item);
            return key;
        }
        /// <summary>
        /// Vytvoří new instanci zdejšího controlu, umístí ji do neviditelné souřadnice, přidá do Ownera a vrátí.
        /// </summary>
        /// <returns></returns>
        private Control _CreateControl(DxDataFormControlUseMode mode)
        {
            var control = _CreateControlFunction();
            Size size = control.Size;
            Point location = new Point(10, -10 - size.Height);
            control.Visible = false;
            control.SetBounds(new Rectangle(location, size));
            bool addToBackground = (mode == DxDataFormControlUseMode.Draw);
            return control;
        }
        /// <summary>
        /// Naplní daný control daty pro požadovaný režim práce, a umístí jej do parent controlu. Pokud je předán parent = null nebo režim mode = Draw, pak jej umístí do výchozího panelu.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="control"></param>
        /// <param name="mode"></param>
        /// <param name="parent">Parent, do něhož má být control umístěn. Pokud režim <paramref name="mode"/> je <see cref="DxDataFormControlUseMode.Draw"/>, pak parent smí být null.</param>
        private void _FillControl(DxDataFormColumn item, Control control, DxDataFormControlUseMode mode, Control parent)
        {
            _FillControlAction(item, control, mode);
            _DataForm.AddControl(control, (mode == DxDataFormControlUseMode.Draw ? null : parent));
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
        private void DisposeControl(ref Control control, DxDataFormControlUseMode mode)
        {
            if (control == null) return;
            control?.RemoveControlFromParent(hideControl: true);
            control.Dispose();
            control = null;
        }
        private Control _ControlDraw;
        private Control _ControlMouse;
        private Control _ControlFocus;
        #endregion
    }
    /// <summary>
    /// Způsob využití controlu (kreslení, pohyb myši, plný focus)
    /// </summary>
    internal enum DxDataFormControlUseMode { None, Draw, Mouse, Focus }
    #endregion
    #region Enumy : DxDataFormRowType, RefreshParts
    /// <summary>
    /// Typ řádku v rámci <see cref="DxDataForm"/>
    /// </summary>
    internal enum DxDataFormRowType
    {
        None = 0,
        /// <summary>
        /// Záhlaví
        /// </summary>
        RowHeader,
        /// <summary>
        /// Řádkový filtr
        /// </summary>
        RowFilter,
        /// <summary>
        /// Řádek s daty
        /// </summary>
        RowData,
        /// <summary>
        /// Řádek se sumářem
        /// </summary>
        RowSummary,
        /// <summary>
        /// Zápatí
        /// </summary>
        RowFooter
    }
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
        /// Vyřešit souřadnice nativních controlů, nacházejících se v Content panelu
        /// </summary>
        NativeControlsLocation = 0x0040,
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
        AfterItemsChangedSilent = RecalculateContentTotalSize | ReloadVisibleItems | NativeControlsLocation,
        /// <summary>
        /// Po změně prvků (přidání, odebrání, změna hodnot) (<see cref="RecalculateContentTotalSize"/> + <see cref="ReloadVisibleItems"/> + <see cref="InvalidateControl"/>).
        /// Tato hodnota není Silent = obsahuje i invalidaci <see cref="InvalidateControl"/> = překreslení controlu.
        /// <para/>
        /// Toto je standardní refresh.
        /// </summary>
        AfterItemsChanged = RecalculateContentTotalSize | ReloadVisibleItems | NativeControlsLocation | InvalidateControl,
        /// <summary>
        /// Po scrollování (<see cref="ReloadVisibleItems"/> + <see cref="InvalidateControl"/>)
        /// </summary>
        AfterScroll = ReloadVisibleItems | NativeControlsLocation | InvalidateControl,
        /// <summary>
        /// Po změně prvků (přidání, odebrání, změna hodnot) (<see cref="RecalculateContentTotalSize"/> + <see cref="ReloadVisibleItems"/> + <see cref="InvalidateControl"/>).
        /// <para/>
        /// Toto je standardní refresh.
        /// </summary>
        Default = AfterItemsChanged,
        /// <summary>
        /// Všechny akce, včetně invalidace cache (brutální refresh)
        /// </summary>
        All = RecalculateContentTotalSize | ReloadVisibleItems | NativeControlsLocation | InvalidateCache | InvalidateControl
    }
    #endregion
}
