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
using Noris.Clients.Win.Components.AsolDX;
using Noris.Clients.Win.Components.Obsoletes.Data;

#region INFORMACE: Architektura DataFormu, Zobrazení dat, Konstrukce instancí

/*  [A]  Architektura DataFormu
    ===========================
   a. Vrcholový prvek je instance třídy DxDataForm, ten je vytvářen vnější aplikací, a ta pak komunikuje výhradně s DxDataForm
   b. Vstupem jsou jednak definice stránek, skupin a sloupců, setují se do DxDataForm.Pages
   c. Druhým vstupem jsou vlastní data (řádky, sloupce, dynamické definice buněk), vkládají se do DxDataForm.Data

    [B]  Zobrazení dat v DataFormu
    ==============================
   I. Nejjednodušší varianta je zobrazení jedné stránky (=bez záložek) s jednou částí (DxDataFormPart), bez záhlaví, filtrů a sumárních řádků;
       Tato varianta je typická pro zobrazení jednoduchých instancí a oken;
       I tato varianta může zobrazovat více než jeden řádek s daty.
  II. Druhou variantou je zobrazení s více záložkami, pak záložky nikdy nemají více částí (DxDataFormPart) ani záhlaví, filtry atd;
       Tato varianta je typická pro zobrazení Master části instancí a složitých DynamicPage.
 III. Třetí variantou je zobrazení volitelně bez záložek nebo s více záložkami, s jednou nebo více částmi (DxDataFormPart), s možností zobrazovat záhlaví, filtry atd;
       Tato varianta je typická pro zobrazení a editaci položek / řádků v přehledové šabloně.

    [C]  Konstrukce instancí v DataFormu
    ====================================
   1. Základem je třída DxDataForm
      a) Vždy v sobě hostuje data = DxDataFormData
      b) Vždy v sobě drží jednu jedinou instanci třídy DxDataFormPanel = zobrazovač dat, viz dále
      c) Pokud DxDataForm zobrazuje jen jednu stránku, pak nemá záložkovník, ale přímo v sobě hostuje a zobrazuje (jako svůj Control) panel DxDataFormPanel
      d) Pokud DxDataForm zobrazuje více záložek, pak DxDataForm (jako vrcholový Control) v sobě hostuje záložkovník (=standardní DxTabPane), ale má stále jen jednu instanci panelu DxDataFormPanel,
           a tuto instanci DxDataFormPanel vždy umísťuje do aktuálně zobrazené záložky v DxTabPane;
           v procesu přepínání záložek si DxDataForm aktualizuje referenci na definici právě zobrazené stránky, a prostřednictvím řádků tyto definice zobrazuje;
           a vkládá do něj i odpovídající status jeho záložky (stav, v jakém byl naposledy tento panel, když zobrazoval tuto záložku) = pozice splitterů, scrollbarů atd.

   2. Zobrazení dat konkrétní stránky (celého DataFormu nebo jedné záložky) tedy zajišťuje panel DxDataFormPanel
      a) Ten má v sobě deklaraci layoutu (definice: Grupy DxDataFormGroup a v nich sloupce DxDataFormColumn)
           Pokud celý DataForm má vícero záložek, pak panel DxDataFormPanel obsahuje ve své deklaraci layoutu jen podmnožinu skupin DxDataFormGroup týkající se aktuální záložky
      b) DxDataFormPanel za určitých podmínek dovoluje vytvářet Splitted Parts = například zobrazí v levé části několik sloupců, 
           vpravo od nich svislý Splitter, a za Splitterem pak tytéž řádky, ale odscrollované sloupce doprava...
           Obdobně lze Splitnout řádky vodorovně: v horní části mít několik řádků, a v dolní části scrollovat celým přehledem...
      c) Tedy: panel DxDataFormPanel sám nezobrazuje řádky a sloupce a data, ale dává prostor jednotlivým částem typu DxDataFormPart, 
           aby v určité části panelu DxDataFormPanel zobrazily svoje data (řádky, sloupce, záhlaví, filtr, atd).
      d) Panel DxDataFormPanel tedy řídí tvorbu, přemístění a zánik částí, pomocí splitterů.
      e) Dále tento panel řídí synchronizaci souřadnic mezi sousedními částmi DxDataFormPart = tak, aby levá i pravá část vedle sebe zobrazovaly shodné řádky,
           a horní a dolní část zobrazovaly shodné sloupce.

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


    [D]  Souřadnicové systémy
    =========================
   1. Nativní    = pixely na fyzickém Controlu, kde 0/0 je první viditelný pixel vlevo nahoře
   2. Absolutní  = souřadnice celková na datovém prvku, kde 0/0 je první pixel vlevo nahoře na prvním řádku, prvním sloupci
                   Následující řádek má svůj Y počátek na konci Y předešlého řádku (řádek 0:Bottom = řádek 1:Top)
                   Tyto souřadnice se nemění (pokud nedojde ke změně dat řádků = filtr, přetřídění);
   3. Virtuální  = souřadnice daná Scrollováním a aktuální velikostí viditelného prostoru = "Kukátko" na Absolutní souřadnice
   4. Current    = souřadnice v rámci nejbližšího Parenta, například pozice řádku v parent Part, nebo pozice Grupy v parent řádku, 
                   nebo pozice prvku Item v rámci Grupy
   5. Vizuální   = souřadnice prvku přepočítaná do Nativní souřadnice na Controlu. Tedy Vizuální  ==  Nativní !
                   Jde tedy o Absolutní souřadnici prvku, upravenou pomocí Virtuálního okna.

   Příklad:
   --------
     Mějme tedy vizuální control se zobrazovanou fyzickou velikostí W=800 a H=600, s možností scrollování obsahu;
     Mějme k tomu datový prvek, který určil svoji šířku W=1200 a výšku jednoho řádku RH=400, a počet řádků RN=20, pak Absolutní velikost zobrazovaných dat: W=1200 a H=8000
     ScrollControl tedy dovoluje scrollovat v obou směrech X i Y, určuje tak zobrazované "Virtuální okno"
     Virtuální (odscrollované) okno tedy bude např. X=100, Y=1250, W=800, H=600;
     Najděme tedy řádky, které jsou aktuálně viditelné na virtuální souřadnici Y: 1250 + 600 = 1250÷1850
      (při výšce řádku 400 mají první řádky tyto souřadnice Y: [0]: 0÷400; [1]: 400÷800; [2]: 800÷1200; [3]: 1200÷1600; [4]: 1600÷2000; [5]: 2000÷2400; ...)
      Vyhledáme tedy viditelné řádky s Absolutními souřadnicemi Y v zobrazované oblasti 1250÷1850: [3]: 1200÷1600; [4]: 1600÷2000;
     Řádek [3] má absolutní souřadnici Y 1200÷1600, virtuální okno má souřadnice Y 1250÷1850;
     Řádek [3] na sobě nese grupy, jejichž Current souřadnice (=relativně k řádku) jsou například: [0]: 0÷120; [1]: 120÷180; [2]: 180÷400;
     Pak tedy grupa [1] v řádku [3]:
        - má Current souřadnice Y: 120÷180;
        - odpovídající Absolute souřadnice je Current + 1200 (to je Absolutní souřadnice Y řádku [3]), tedy grupa [1] má Absolute Y = 1320÷1380;
        - V rámci Virtuálního okna, jehož Y: 1250÷1850 pak bude Vizuální souřadnice grupy [1] v řádku [3]: (Absolute - 1250) = 70÷130 = pixely v Controlu, kam bude grupa reálně kreslena.


    [E]  Oblast řádků (Splitter) / Řádek / Grupy prvků / Prvky
    ==========================================================
   1. Máme pole řádků, které si udržují svoji Absolutní souřadnici Y, a jejich souřadnice X = 0 (jde o řádky pod sebou umístěné);
       Jeden řádek reprezentuje instance DxDataFormRow;
       Pole všech po sobě jdoucích řádků je uloženo v instanci DxDataFormRowSet;
   2. Pokud DataForm provede Split = vytvoří novou oblast pro zobrazování řádků (dvě oblasti nad sebou, kde každá oblast zobrazuje jiné řádky), 
       pak je vytvořena nová instance sady DxDataFormRowSet pro novou oblast PartY;
   3. Jednotlivé oblasti mohou mít jiné filtry řádků i jiné třídění = jinou množinu řádků, v nichž si DxDataFormRowSet udržuje pořadí a absolutní souřadnici Y;
   4. Každá tato oblast má svůj svislý Scrollbar, proto má každá oblast svoje vlastní Virtuální okno (scrollovaná pozice a výška);
   5. Každá tato oblast je zobrazována v samostatném Controlu DxDataFormPart (: DxScrollableContent)
   6. Vykreslení Controlu tedy probíhá tak, že:
      a) jsou nalezeny viditelné řádky (provede se 1x po změně Virtuálního okna, tedy po Scrollu a po Resize; a po změně sady řádků);
      b) vyvolá se metoda pro kreslení řádku;
      c) řádek vykreslí svoji grafiku (možná jen linku, nebo záhlaví řádku);
      d) řádek prochází grupy, a to buď svoje vlastní nebo ze společné definice (viz níže), 
          předává jim požadavek na vykreslení a svoji vizuální souřadnici počátku a viditelnou oblast, a data svého řádku;
      e) grupa určí svoji vizuální oblast, a pokud je z ní něco vidět, pak vykreslí sebe a své prvky;
   7. Grupy v řádku:
      a) pokud budou mít všechny řádky stejnou velikost všech prvků a grup a jejich pozice, tak mohou používat společnou definici grup = pochází z DxDataFormPage
      b) pokud ale některé grupy budou řídit svoji velikost + pozici individuálně podle obsahu řádku (budou se tedy lišit řádek od řádku), 
         pak odlišné řádky musí mít svoji definici grup uloženou lokálně, s vlastním layoutem grup a prvků;
      c) 




*/
#endregion

namespace Noris.Clients.Win.Components.Obsoletes.DataForm
{
    using Noris.Clients.Win.Components.Obsoletes.DataForm.Internal;

    /// <summary>
    /// DataForm
    /// </summary>
    public class DxDataForm : DxPanelControl, IDxDataFormWorking
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
            InitializeTabDef();
            InitializeAppearance();
        }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DisposeImageCache();
            DisposeUserControls();
            _DisposeVisualPanels();
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
            __RowSets = new Dictionary<int, DxDataFormRowSet>();
            __Data = new DxDataFormData(this);
        }
        /// <summary>
        /// Vlastní data zobrazená v dataformu
        /// </summary>
        internal DxDataFormData Data { get { return __Data; } }
        private DxDataFormData __Data;
        #endregion
        #region Sady řádků
        /// <summary>
        /// Vrátí pole řádků, které jsou (anebo mohou být) zobrazováno v <see cref="DxDataFormPart"/> dle daného ID.
        /// Tyto řádky jsou sdíleny všemi sousedními částmi vlevo i vpravo a jsou společně scrollovány, a mají tedy společné souřadnice Y.
        /// <para/>
        /// Jde tedy o všechny řádky, nejen ty aktuálně viditelné. 
        /// Z tohoto pole se pak vybírají řádky, které jsou aktuálně zobrazeny: po změně viditelné oblasti se znovu vyhledají a uloží.
        /// <para/>
        /// Řádkový filtr: zde jsou vráceny jen ty řádky, které vyhovují řádkovému filtru! Pokud tedy datový zdroj obsahuje 500 řádků, a řádkovému filtru vyhovuje jen 60, pak je zde oněch 60!
        /// Aktuální control z oněch 60 může zobrazovat (scrollovat) např. jen 24 řádků, ale k dispozici je těch 60.
        /// <para/>
        /// Pokud jsou přítomny části nad a pod (vodorovné splittery), pak ty mohou zobrazovat fyzicky jiné řádky = jinak zafiltrované, jinak setříděné, v jiné vizuální pozici (jinak odscrollované).
        /// </summary>
        internal List<DxDataFormRow> GetRows(DxDataFormPartId partId)
        {
            return _GetRowSet(partId.PartYId).RowsVisible;
        }
        /// <summary>
        /// Vrátí velikost prostoru všech řádků = na výšku i na šířku
        /// </summary>
        /// <param name="partId"></param>
        /// <returns></returns>
        internal Size GetRowsTotalSize(DxDataFormPartId partId)
        {
            return _GetRowSet(partId.PartYId).RowsTotalSize;
        }
        /// <summary>
        /// Najde / vytvoří a vrátí RowSet daného ID
        /// </summary>
        /// <param name="setId"></param>
        /// <returns></returns>
        private DxDataFormRowSet _GetRowSet(int setId)
        {
            if (!__RowSets.TryGetValue(setId, out var rowSet))
                __RowSets.Add(setId, rowSet = new DxDataFormRowSet(this, setId));
            return rowSet;
        }
        /// <summary>
        /// Zajistí znovuvytvoření všech řádků pro všechny sady řádků,
        /// aktuálně přítomné v <see cref="__RowSets"/>
        /// </summary>
        private void _ReloadAllRows()
        {
            lock (__RowSets)
            {
                foreach (var rowSet in __RowSets.Values)
                    rowSet.ReloadRows();
            }
        }
        /// <summary>
        /// Index prvků <see cref="DxDataFormRowSet"/> podle klíče int, 
        /// který odpovídá <see cref="DxDataFormPartId.PartYId"/> = <see cref="DxDataFormRowSet.SetId"/>.
        /// 
        /// </summary>
        private Dictionary<int, DxDataFormRowSet> __RowSets;
        #endregion
        #region Zobrazované prvky = definice dataformu = stránky, odvození záložek, a vlastní data
        /// <summary>
        /// Definice formuláře.
        /// Dokud nebude vložena definice vzhledu, bude prvek prázdný.
        /// </summary>
        public IDataForm IForm { get { return __IForm; } set { _SetIForm(value); } }
        private IDataForm __IForm;
        /// <summary>
        /// Vloží danou definici vzhledu do this instance
        /// </summary>
        /// <param name="iForm"></param>
        private void _SetIForm(IDataForm iForm)
        {
            _CreateDataPages(iForm);
            _ActivateVisualPanels();
            _ActivatePage();
        }
        /// <summary>
        /// Z dodané definice formuláře <see cref="IDataForm"/> vytvoří zdejší datové struktury: 
        /// naplní pole <see cref="__DataFormPages"/>, nic dalšího nedělá.
        /// </summary>
        /// <param name="iForm"></param>
        private void _CreateDataPages(IDataForm iForm)
        {
            __IForm = iForm;
            __DataFormPages = DxDataFormPage.CreateList(this, iForm?.Pages);
        }
        /// <summary>
        /// Metoda invaliduje všechny souřadnice na stránkách, které jsou závislé na Zoomu a na DPI.
        /// </summary>
        private void InvalidateCurrentBounds()
        {
            __DataFormPages?.ForEachExec(p => p.InvalidateLayout());
        }
        private void _ActivatePage(string pageId = null)
        {
        }
        /// <summary>
        /// Po změně velikosti
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this._CheckPageDynamicLayout();
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
        }
        /// <summary>
        /// Metoda zkusí najít navigační stránku (typově přesnou) a její data stránky <see cref="DxDataFormPage"/>
        /// pro vstupní obecnou stránku záložky.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="tabPage"></param>
        /// <param name="dxPage"></param>
        /// <returns></returns>
        private bool _TryGetFormTab(DevExpress.XtraBars.Navigation.INavigationPageBase page, out DevExpress.XtraBars.Navigation.TabNavigationPage tabPage, out DxDataFormPage dxPage)
        {
            tabPage = null;
            dxPage = null;
            if (!(page is DevExpress.XtraBars.Navigation.TabNavigationPage tp)) return false;
            tabPage = tp;
            var tabName = tabPage.Name;
            return _TryGetFormPage(tabName, out dxPage);
        }
        /// <summary>
        /// Metoda zkusí najít data stránky <see cref="DxDataFormPage"/>
        /// pro dané ID stránky.
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="dxPage"></param>
        /// <returns></returns>
        private bool _TryGetFormPage(string pageId, out DxDataFormPage dxPage)
        {
            dxPage = null;
            if (String.IsNullOrEmpty(pageId)) return false;
            return __DataFormPages.TryGetFirst(p => String.Equals(p.PageId, pageId), out dxPage);
        }
        /// <summary>
        /// Aktuální počet stránek dle definice = počet prvků v poli <see cref="__DataFormPages"/>
        /// </summary>
        private int _DataFormPageCount { get { return (__DataFormPages?.Count ?? 0); } }
        /// <summary>
        /// Data definic pro jednotlivé vstupní stránky.
        /// Jedna stránka je definovaná designerem, a je umístěna na jedné záložce <see cref=" DevExpress.XtraBars.Navigation.TabNavigationPage"/> nebo v celém prostoru <see cref="DxDataForm"/>.
        /// </summary>
        private List<DxDataFormPage> __DataFormPages;
        #region Práce s controly DxDataFormPanel (jednoduchý DataForm) a / nebo TabPane (záložky)
        /// <summary>
        /// Aktivuje patřičný control pro zobrazení DataFormu.
        /// </summary>
        private void _ActivateVisualPanels()
        {
            int count = _DataFormPageCount;
            if (count == 0)
                _DeactivateVisualPanels();
            else if (count == 1)
                _ActivateSinglePanel();
            else if (count > 1)
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

            _DataFormPanel.State = null;
            _DataFormPanel.Visible = true;

            ActiveDxPage = __DataFormPages.FirstOrDefault();
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
        private void _DisposeVisualPanels()
        {
            _DisposeDataFormPanel();
            _DisposeDataFormTabPane();
        }
        #endregion
        #region DxDataFormPanel = hlavní vizuální zobrazovač (hostuje jednotlivé splitované DxDataFormPart-s, přinejmenším jeden)
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
        #region DxTabPane - fyzický záložkovník (horní záložky = jednotlivé vizuální stránky na DataFormu)
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
        /// <summary>
        /// Začíná změna záložky TabPane. Tady ji lze stornovat.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabPane_SelectedPageChanging(object sender, DevExpress.XtraBars.Navigation.SelectedPageChangingEventArgs e)
        {
        }
        /// <summary>
        /// Proběhla změna záložky TabPane. Tady ji už nelze stornovat, tady na ni musíme reagovat.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabPane_SelectedPageChanged(object sender, DevExpress.XtraBars.Navigation.SelectedPageChangedEventArgs e)
        {
            if (e.Page == null) return;

            _PrepareDataFormPanel();

            if (_TryGetFormTab(e.OldPage, out var oldTabPage, out var oldDxPage))
            {
                oldDxPage.State = _DataFormPanel.State.Clone();
            }
            if (_TryGetFormTab(e.Page, out var newTabPage, out var newDxPage))
            {
                ActiveDxPage = newDxPage;

                _DataFormPanel.AddControlToParent(newTabPage);       // Zajistíme, že DataFormPanel (zobrazovací panel, jediný) bude přítomný jako Child control v nově vybrané stránce (v záložkovníku)
                _DataFormPanel.State = newDxPage.State;
                _DataFormPanel.Visible = true;
            }
            else
            {
                ActiveDxPage = newDxPage;
                _DataFormPanel.State = null;
            }
        }
        /// <summary>
        /// Do vlastního záložkovníku TabPane vygeneruje fyzické stránky podle obsahu v <see cref="__DataFormPages"/> = definice jednotlivých stránek.
        /// </summary>
        private void _PrepareDataFormTabPages()
        {
            _DataFormTabPane.SetPages(__DataFormPages, null, true, true);
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
        #region ActiveDxPage = definice obsahu a vzhledu aktuálně viditelné stránky
        /// <summary>
        /// Inicializuje pole <see cref="ActiveDxPage"/> a související objekty
        /// </summary>
        private void InitializeTabDef()
        {
            __ActiveDxPage = null;
            __DynamicGroupHeight = false;
        }
        /// <summary>
        /// Definice aktuálně zobrazené stránky = deklarace obsažených skupin a prvků.
        /// Pokud má DataForm více stránek (vizuálních záložek), pak je zde definice právě té aktivní = viditelné.
        /// Podle této definice se odvozují definice pro jednotlivé řádky.
        /// </summary>
        internal DxDataFormPage ActiveDxPage { get { return __ActiveDxPage; } private set { _SetActiveDxPage(value, RefreshParts.All); } }
        private DxDataFormPage __ActiveDxPage;
        /// <summary>
        /// Aktuální velikost prvku.
        /// Hodnota je v aktuálních pixelech (nikoli designové pixely) = přepočteno Zoomem a DPI.
        /// </summary>
        internal Size ActiveDxPageCurrentSize { get { return __ActiveDxPage?.CurrentSize ?? Size.Empty; } }
        /// <summary>
        /// Výška jednoho řádku v aktuálním layoutu = reálná výška všech grup včetně <see cref="IDataFormPage.DesignPadding"/> + mezera <see cref="DxDataForm.RowHeightSpace"/>.
        /// </summary>
        internal int ActiveDxPageCurrentRowHeight { get { return __ActiveDxPage?.CurrentRowHeight ?? 0; } }
        /// <summary>
        /// Nastaví jako Aktivní stránku tu dodanou, a zajistí minimální potřebné refreshe
        /// </summary>
        /// <param name="activeDxPage"></param>
        /// <param name="refreshParts"></param>
        private void _SetActiveDxPage(DxDataFormPage activeDxPage, RefreshParts refreshParts = RefreshParts.None)
        {
            activeDxPage.CheckValidLayout();
            __ActiveDxPage = activeDxPage;
            if (refreshParts != RefreshParts.None)
                Refresh(refreshParts);
        }
        /// <summary>
        /// Metoda je volána po změně velikosti DataFormu, umožní definici stránky reagovat dynamickým přeskupením svého obsahu.
        /// Na široké stránce mohou být grupy poskládané jinak...
        /// </summary>
        private void _CheckPageDynamicLayout()
        {
            var activeDxPage = this.ActiveDxPage;
            if (activeDxPage != null && activeDxPage.HasDynamicGroupLayout)
                activeDxPage.RecalculateDynamicGroupLayout();
        }
        #endregion

        #region CurrentGroups = definice vzhledu aktuálního DataFormu (layout), mění se při přepínání záložek
        /// <summary>
        /// Inicializuje pole <see cref="CurrentGroupDefinitions"/> a související
        /// </summary>
        private void InitializeGroups()
        {
            __DynamicGroupHeight = false;
        }
        /// <summary>
        /// Dynamická výška grup = může se měnit pro různé řádky
        /// </summary>
        private bool __DynamicGroupHeight;
        #endregion
        #region Služby pro controly se vztahem do DxDataFormPanel - vizuální, grafické (Brush, Paint, atd)
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
        /// Provede refresh DataFormu
        /// </summary>
        /// <param name="refreshParts"></param>
        internal void Refresh(RefreshParts refreshParts)
        {
            if (refreshParts.HasFlag(RefreshParts.ReloadAllRows)) this._ReloadAllRows();

            _DataFormPanel?.Refresh(refreshParts);
        }
        /// <summary>
        /// Aktuální velikost viditelného prostoru pro DataForm, když by v něm nebyly ScrollBary
        /// </summary>
        internal Size VisibleTotalSize { get { return (this._DataFormPanel?.ClientSize ?? Size.Empty); } }
        /// <summary>
        /// Aktuální velikost viditelného prostoru pro DataForm, po odečtení aktuálně zobrazených ScrollBarů (pokud jsou zobrazeny)
        /// </summary>
        internal Size VisibleContentSize { get { return (this._DataFormPanel?.VisibleContentSize ?? Size.Empty); } }
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
            return CreateGraphicsFrame(bounds, new Padding(sizes));
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
        /// Vykreslí rámeček v daném prostoru s danou šířkou v daném vzhledu
        /// </summary>
        /// <param name="e"></param>
        /// <param name="bounds"></param>
        /// <param name="sizes"></param>
        /// <param name="appearance"></param>
        /// <param name="onMouse"></param>
        /// <param name="hasFocus"></param>
        internal static void PaintFrame(DxBufferedGraphicPaintArgs e, Rectangle? bounds, Padding? sizes, IDataFormBackgroundAppearance appearance, bool onMouse, bool hasFocus)
        {
            if (!bounds.HasValue || !sizes.HasValue || appearance is null) return;
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
        internal DxDataFormControlSet GetControlSet(DxDataFormItem item)
        {
            DataFormColumnType itemType = item.ItemType;
            return _ControlsSets.Get(itemType, () => new DxDataFormControlSet(this, itemType), true);
        }
        /// <summary>
        /// Vrátí control pro daný prvek a režim použití
        /// </summary>
        /// <param name="item"></param>
        /// <param name="mode"></param>
        /// <param name="parent">Parent, do něhož má být control umístěn. Pokud režim <paramref name="mode"/> je <see cref="DxDataFormControlUseMode.Draw"/>, pak parent smí být null.</param>
        /// <returns></returns>
        internal Control GetControl(DxDataFormItem item, DxDataFormControlUseMode mode, Control parent)
        {
            DxDataFormControlSet controlSet = GetControlSet(item);
            return controlSet.GetControlForMode(item, mode, parent);
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
        internal Image CreateImage(DxDataFormItem item, Graphics graphics)
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
        private Image CreateBitmapForItem(DxDataFormItem item, Graphics graphics)
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
                if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"DxDataForm.CleanCache(): CacheSize={currentCacheSize}B; úklid není nutný. Čas akce: {DxComponent.LogTokenTimeMilisec}", startTime);
                return;
            }

            // Budu pracovat jen s těmi prvky, které nebyly dlouho použity:
            long lastLoop = paintLoop - _CACHECLEAN_OLD_LOOPS;
            var items = ImageCache.Where(kvp => kvp.Value.LastPaintLoop <= lastLoop).ToList();
            if (items.Count == 0)                                    // Pokud všechny prvky pravidelně používám, nebudu je zahazovat.
            {
                _NextCleanLiable = false;
                ImageCacheNextCleanId = paintLoop + _CACHECLEAN_AFTER_LOOPS_SMALL;
                if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"DxDataForm.CleanCache(): CacheSize={currentCacheSize}B; úklid není možný, všechny položky se používají. Čas akce: {DxComponent.LogTokenTimeMilisec}", startTime);
                return;
            }

            // Z nich zahodím ty, které byly použity méně než je průměr:
            string[] keys1 = null;
            decimal? averageUse = items.Average(kvp => (decimal)kvp.Value.HitCount);
            if (averageUse.HasValue)
                keys1 = items.Where(kvp => (decimal)kvp.Value.HitCount < averageUse.Value).Select(kvp => kvp.Key).ToArray();

            CleanImageCache(keys1);

            long cleanedCacheSize = ImageCacheSize;
            if (LogActive) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"DxDataForm.CleanCache(): CacheSize={currentCacheSize}B; Odstraněno {keys1?.Length ?? 0} položek; Po provedení úklidu: {cleanedCacheSize}B. Čas akce: {DxComponent.LogTokenTimeMilisec}", startTime);

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
        /// Inicializace proměnných definujících vzhled
        /// </summary>
        private void InitializeAppearance()
        {
            __DataFormAppearance = new DxDataFormAppearance();
            __ItemIndicatorsVisible = false;
            __RowHeightSpace = 1;
        }
        /// <summary>
        /// Vzhled. Autoinicializační property. Nikdy není null. Setování null nastaví defaultní vzhled.
        /// </summary>
        public DxDataFormAppearance DataFormAppearance
        {
            get { if (__DataFormAppearance == null) __DataFormAppearance = new DxDataFormAppearance(); return __DataFormAppearance; }
            set { __DataFormAppearance = value; }
        }
        private DxDataFormAppearance __DataFormAppearance;
        /// <summary>
        /// Aktivace barevných indikátoru "OnDemand"
        /// </summary>
        public bool ItemIndicatorsVisible
        {
            get { return __ItemIndicatorsVisible; }
            set
            {
                __ItemIndicatorsVisible = value;
                this.Refresh(RefreshParts.InvalidateControl);
            }
        }
        private bool __ItemIndicatorsVisible;
        /// <summary>
        /// Výška mezery mezi řádky
        /// </summary>
        public int RowHeightSpace
        {
            get { return __RowHeightSpace; }
            set
            {
                __RowHeightSpace = (value < 0 ? 0 : (value > 10 ? 10 : value));
                this.Refresh(RefreshParts.All);
            }
        }
        private int __RowHeightSpace;
        #endregion
    }
    internal interface IDxDataFormWorking
    {
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

namespace Noris.Clients.Win.Components.Obsoletes.DataForm.Internal
{
    // Zobrazovaná data:
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
            __DataForm = dataForm;
            _SetSourceNull();
        }
        /// <summary>Vlastník - <see cref="DxDataForm"/></summary>
        private DxDataForm __DataForm;
        /// <summary>Vlastník - <see cref="DxDataForm"/> přetypovaný na <see cref="IDxDataFormWorking"/>, pro interní přístup</summary>
        private IDxDataFormWorking IDataForm { get { return __DataForm as IDxDataFormWorking; } }
        /// <summary>
        /// Datový zdroj.
        /// Může to být <see cref="System.Data.DataTable"/>, nebo 
        /// </summary>
        public object Source { get { return __Source; } set { _SetSource(value); } }
        /// <summary>
        /// Vloží zdroj, detekuje jeho druh, provede typovou inicializaci
        /// </summary>
        /// <param name="source"></param>
        private void _SetSource(object source)
        {
            if (source == null) _SetSourceNull();
            else if (source is System.Data.DataTable dataTable) _SetSourceDataTable(dataTable);
            else if (source is IList<object> list) _SetSourceList(list);
            else throw new ArgumentException($"Unsupported Data.Source for DataForm, type: '{source.GetType().Name}'.");

            __DataForm.Refresh(RefreshParts.ReloadAllRows | RefreshParts.ReloadVisibleRows | RefreshParts.RecalculateContentTotalSize | RefreshParts.InvalidateControl);
        }
        /// <summary>
        /// Nullování zdroje (odpojení)
        /// </summary>
        private void _SetSourceNull()
        {
            __Source = null;
            __SourceDataTable = null;
            __SourceList = null;
            __SourceRecord = null;
            __CurrentSourceType = SourceType.None;
        }
        /// <summary>
        /// Obecný zdroj dat, netypová reference
        /// </summary>
        private object __Source;
        /// <summary>
        /// Index sloupců.
        /// Klíčem je jméno sloupce v té formě, v jaké jej deklaruje datový zdroj.
        /// Value je odpovídající interní informace zdroje, používá ji pouze konkrétní zdroj.
        /// </summary>
        private Dictionary<string, object> __Columns;
        /// <summary>
        /// Index sloupců.
        /// Klíčem je nativní pořadí sloupce, v jaké jej deklaruje datový zdroj.
        /// Value je odpovídající interní informace zdroje, používá ji pouze konkrétní zdroj.
        /// </summary>
        private Dictionary<int, object> __ColumnIndexes;
        /// <summary>
        /// Index řádků. 
        /// Klíčem je unikátní RowID.
        /// Value je odpovídající interní informace zdroje, používá ji pouze konkrétní zdroj.
        /// </summary>
        private Dictionary<int, object> __Rows;
        /// <summary>
        /// Aktuální typ dat
        /// </summary>
        private SourceType __CurrentSourceType;
        /// <summary>
        /// Typ datového zdroje
        /// </summary>
        private enum SourceType { None, DataTable, List, Record }
        #endregion
        #region Public přístup - nezávislý na typu dat
        /// <summary>
        /// Počet řádků s daty.
        /// </summary>
        public int RowsCount { get { return ((__CurrentSourceType != SourceType.None && __Rows != null) ? __Rows.Count : 0); } }
        /// <summary>
        /// ID všech řádků s daty. Může být null.
        /// Hodnota ID je daná zdrojem, pro <see cref="SourceType.DataTable"/> je to index řádku.
        /// </summary>
        public int[] RowsId { get { return ((__CurrentSourceType != SourceType.None && __Rows != null) ? __Rows.Keys.ToArray() : null); } }
        /// <summary>
        /// Jména všech sloupců s daty. Může být null.
        /// </summary>
        public string[] ColumnNames { get { return ((__CurrentSourceType != SourceType.None && __Columns != null) ? __Columns.Keys.ToArray() : null); } }
        /// <summary>
        /// Přístup k datům buňky (řádek x sloupec)
        /// </summary>
        /// <param name="rowId"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public object this[int rowId, string columnName]
        {
            get { return _GetValue(rowId, columnName); }
            set { _SetValue(rowId, columnName, value); }
        }

        private object _GetValue(int rowId, string columnName) { return null; }
        private void _SetValue(int rowId, string columnName, object value) { }


        /// <summary>
        /// Počet řádků s daty. Pokud nejsou vložena data, vrací 0.
        /// </summary>
        /// <param name="partId">Identifikátor části. Různé části mohou mít různé řádkové filtry, a pak mají různé počty řádků.</param>
        internal int GetRowCount(DxDataFormPartId partId)
        {
            switch (__CurrentSourceType)
            {
                case SourceType.None: return 0;
                case SourceType.DataTable: return _GetRowCountDataTable(partId);
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
            switch (__CurrentSourceType)
            {
                case SourceType.None: return null;
                case SourceType.DataTable: return _GetTextDataTable(rowIndex, column);
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
            switch (__CurrentSourceType)
            {
                case SourceType.None: return null;
                case SourceType.DataTable: return _GetVisibleRowsIdDataTable(rowIndexFirst, rowCount);
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
            __Source = dataTable;
            __SourceDataTable = dataTable;
            __CurrentSourceType = SourceType.DataTable;
        }
        /// <summary>
        /// Vrátí počet řádků DataTable
        /// </summary>
        /// <returns></returns>
        private int _GetRowCountDataTable(DxDataFormPartId partId) { return (__SourceDataTable?.Rows.Count ?? 0); }
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
        private System.Data.DataTable __SourceDataTable;
        #endregion
        #region Práce s konkrétním typem - List
        /// <summary>
        /// Vloží datový zdroj typu List
        /// </summary>
        /// <param name="list"></param>
        private void _SetSourceList(IList<object> list)
        {
            _SetSourceNull();
            __Source = list;
            __SourceList = list;
            __CurrentSourceType = SourceType.List;
        }
        /// <summary>
        /// Vrátí počet řádků List
        /// </summary>
        /// <returns></returns>
        private int _GetRowCountList(DxDataFormPartId partId) { return (__SourceList?.Count ?? 0); }
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
        private IList<object> __SourceList;
        #endregion
        #region Práce s konkrétním typem - Record
        /// <summary>
        /// Vrátí počet řádků Record
        /// </summary>
        /// <returns></returns>
        private int _GetRowCountRecord(DxDataFormPartId partId) { return 0; }
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
        private object __SourceRecord;
        #endregion
    }
    #endregion

    // Definice vzhledu:
    #region class DxDataFormPage : Třída reprezentující jednu designem definovanou stránku v dataformu. Jedna Page je umístěna na jedné záložce.
    /// <summary>
    /// <see cref="DxDataFormPage"/> : Třída reprezentující jednu designem definovanou stránku v dataformu.
    /// Odpovídá tedy jedné definici stránky <see cref="IDataFormPage"/>.
    /// </summary>
    internal class DxDataFormPage : DxLayoutItem, IPageItem
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
                foreach (var iPage in iPages)
                {
                    if (iPage != null)
                        dataPages.Add(new DxDataFormPage(dataForm, iPage));
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
            __DataForm = dataForm;
            __IPage = iPage;
            __Groups = DxDataFormGroup.CreateList(this, iPage.Groups);
        }
        /// <summary>Designový a datový vlastník - <see cref="DxDataForm"/></summary>
        private DxDataForm __DataForm;
        /// <summary>Konkrétní vlastník - řádek <see cref="DxDataFormRow"/></summary>
        private DxDataFormRow __DxRow;
        /// <summary>Deklarace této stránky</summary>
        private IDataFormPage __IPage;
        /// <summary>Grupy</summary>
        private List<DxDataFormGroup> __Groups;
        /// <summary>
        /// Významový vlastník - <see cref="DxDataForm"/>
        /// </summary>
        internal DxDataForm DataForm { get { return __DataForm; } }
        /// <summary>
        /// Konkrétní vlastník - řádek <see cref="DxDataFormRow"/>.
        /// Stránka může být umístěna buď pouze v Dataformu, pak jde o <u>Definiční stránku</u> (obecnou),
        /// anebo může být stránka umístěna na konkrétním řádku, pak jde o konkrétní prvek layoutu řádku.
        /// Stránka umístěná na konkrétním řádku může řešit viditelnost jednotlivých prvků podle konkrétních dat na řádku a podle toho řídit svůj vzhled.
        /// </summary>
        internal DxDataFormRow DxRow
        {
            get { return __DxRow; }
            set
            {   // Setování vyvolá rekalkulaci (invalidaci) hodnot AbsolutePoint:
                this.__DxRow = value;
                this.RecalcAbsolutePoint();
            }
        }
        /// <summary>
        /// Deklarace dat celého formuláře
        /// </summary>
        internal IDataForm IForm { get { return __DataForm.IForm; } }
        /// <summary>
        /// Deklarace stránky této stránky
        /// </summary>
        internal IDataFormPage IPage { get { return __IPage; } }
        /// <summary>
        /// Pole fyzických skupin na této stránce
        /// </summary>
        internal IList<DxDataFormGroup> Groups { get { return __Groups; } }
        /// <summary>
        /// Stránka je na aktivní záložce? 
        /// Po iniciaci se přebírá do GUI, následně udržuje GUI.
        /// V jeden okamžik může být aktivních více stránek najednou, pokud je více stránek <see cref="IDataFormPage"/> mergováno do jedné záložky.
        /// </summary>
        internal bool IsActive { get { return __IsActive; } set { __IsActive = value; } }
        private bool __IsActive;
        #endregion
        #region Data o stránce
        /// <summary>
        /// ID stránky
        /// </summary>
        internal string PageId { get { return IPage.PageId; } }
        /// <summary>
        /// Titulek stránky
        /// </summary>
        internal string PageText { get { return IPage.PageText; } }
        /// <summary>
        /// Obsahuje true, pokud obsah této stránky je povoleno mergovat do předchozí stránky, pokud je dostatek prostoru.
        /// Stránky budou mergovány do vedle sebe stojících sloupců, každý bude mít nadpis své původní stránky.
        /// <para/>
        /// Aby byly stránky mergovány, musí mít tento příznak obě (nebo všechny).
        /// </summary>
        internal bool AllowMerge { get { return IPage.AllowMerge; } }
        /// <summary>
        /// Titulek ToolTipu. Pokud nebude naplněn, vezme se text prvku.
        /// </summary>
        internal string ToolTipTitle { get { return IPage.ToolTipTitle; } }
        /// <summary>
        /// Text ToolTipu
        /// </summary>
        internal string ToolTipText { get { return IPage.ToolTipText; } }
        /// <summary>
        /// Název obrázku stránky
        /// </summary>
        internal string PageImageName { get { return IPage.PageImageName; } }
        /// <summary>
        /// Počet celkem deklarovaných prvků
        /// </summary>
        internal int ItemsCount { get { return Groups.Select(g => g.ItemsCount).Sum(); } }
        #endregion
        #region Souřadnice a layout
        /// <summary>
        /// Prvek má přepočítat svůj layout, poté kdy proběhl přepočet layoutu jeho Child prvků.
        /// Tato metoda je volána z metody <see cref="DxLayoutItem.RecalculateLayout(bool)"/> po přepočtech Child prvků.
        /// <para/>
        /// Bázová třída <see cref="DxLayoutItem"/> v této metodě nic nedělá.
        /// </summary>
        /// <param name="callHandler"></param>
        protected override void RecalculateCurrentLayout(bool callHandler)
        {
            // Child prvky (= grupy) byly právě přepočítány, mají určenou svoji velikost = je odvozena z velikosti jejich obsahu.
            // Nyní určím pozici těchto skupin a návazně pak i naši velikost.
            _SetGroupsStaticLocation(out Size staticPageSize, out bool hasDynamicGroupLayout);

            this.CurrentLocalPoint = new Point(0, 0);
            this.CurrentStaticSize = staticPageSize;
            this.CurrentSize = staticPageSize;
            this.HasDynamicGroupLayout = hasDynamicGroupLayout;

            this.RecalculateDynamicGroupLayout();
        }
        /// <summary>
        /// Metoda určí souřadnice pro všechny svoje skupiny, s optimálním rozmístěním do daného prostoru.
        /// Na výstupu dává velikost stránky = sumární velikost skupin + <see cref="IDataFormPage.DesignPadding"/>.
        /// </summary>
        /// <param name="staticPageSize"></param>
        /// <param name="hasDynamicGroupLayout"></param>
        private void _SetGroupsStaticLocation(out Size staticPageSize, out bool hasDynamicGroupLayout)
        {
            var currentPadding = DxComponent.ZoomToGui(IPage.DesignPadding, this.CurrentDPI);
            var currentSpacing = DxComponent.ZoomToGui(IPage.DesignSpacing, this.CurrentDPI);
            hasDynamicGroupLayout = false;

            int beginX = currentPadding.Left;
            int beginY = currentPadding.Top;
            int groupX = beginX;
            int groupY = beginY;
            int maxRight = beginX;
            int maxBottom = beginY;
            bool containForceBreak = false;
            bool containAllowBreak = false;
            foreach (var dxGroup in Groups)
            {   // Jedna grupa za druhou:
                if (dxGroup is null || !dxGroup.IsVisible) continue;

                if (dxGroup.LayoutForceBreakToNewColumn)
                {   // Požadavek na Force Break:
                    containForceBreak = true;
                    if (groupY > beginY && maxRight > beginX)
                    {   // Můžeme vyhovět tehdy, když aktuální skupina není první odshora (groupY > beginY) a pokud už máme určenou souřadnici maxRight:
                        groupX = maxRight + currentSpacing.Width;
                        groupY = beginY;
                    }
                }
                else
                {   // Nemáme povinné zalomení grupy do nového sloupce => aktuální grupa bude na současné pozici groupX.
                    // Pokud aktuální hodnota groupY je větší než počáteční beginY, znamená to že už nějaká grupa je zobrazena => přidáme tedy SpacingY:
                    if (groupY > beginY) groupY += currentSpacing.Height;

                    // Zapamatujeme si, zda je možno na této stránce provést AllowBreak:
                    if (dxGroup.LayoutAllowBreakToNewColumn && !containAllowBreak) containAllowBreak = true;
                }

                dxGroup.CurrentLocalPoint = new Point(groupX, groupY);
                var groupSize = dxGroup.CurrentSize;
                int groupRight = groupX + groupSize.Width;
                int groupBottom = groupY + groupSize.Height;
                if (groupRight > maxRight) maxRight = groupRight;              // Střádám největší Right
                if (groupBottom > maxBottom) maxBottom = groupBottom;          // Střádám největší Bottom

                // Příští grupa 
                groupY = groupBottom;
            }

            if (containAllowBreak && !containForceBreak) //  && maxRight > contentSize.Width)
            {   // Dynamické zalomení na této stránce je možné (máme alespoň jednu grupu, která to povoluje) a nemáme povinné zalomení a nynější obsah je větší než aktuální prostor:
                hasDynamicGroupLayout = true;
            }

            // K souřadnicím maxRight a maxBottom přidáme odpovídající Padding a vygenerujeme PageSize:
            maxRight += currentPadding.Right;
            maxBottom += currentPadding.Bottom;
            staticPageSize = new Size(maxRight, maxBottom);
        }
        /// <summary>
        /// Souhrnná velikost stránky ve statickém layoutu = jedna grupa pod druhou / nebo s pouze povinným zalomením vedle sebe.
        /// V aktuálních (nikoli designových) pixelech.
        /// Pokud stránka nemá dynamický layout (<see cref="HasDynamicGroupLayout"/> je false), pak toto je i aktuální velikost stránky <see cref="DxLayoutItem.CurrentSize"/>.
        /// </summary>
        internal Size CurrentStaticSize { get { return __CurrentStaticSize; } private set { __CurrentStaticSize = value; } }
        private Size __CurrentStaticSize;
        /// <summary>
        /// Výška jednoho řádku v aktuálním layoutu = reálná výška všech grup včetně <see cref="IDataFormPage.DesignPadding"/> + mezera <see cref="DxDataForm.RowHeightSpace"/>.
        /// </summary>
        public int CurrentRowHeight { get { return this.CurrentSize.Height + this.DataForm.RowHeightSpace; } }
        /// <summary>
        /// Fyzické DPI, přebírá se z controlu <see cref="DataForm"/>
        /// </summary>
        protected override int CurrentDPI { get { return this.DataForm.CurrentDpi; } }
        /// <summary>
        /// Child prvky v layoutu stránky jsou Grupy
        /// </summary>
        protected override IEnumerable<IDxLayoutItem> LayoutChilds { get { return Groups; } }
        #endregion
        #region DynamicGroupLayout
        /// <summary>
        /// Obsahuje true, pokud tato stránka má nějaké grupy, které povolují dynamický layout = mohou být jinak rozmístěny v závislosti na aktuální šířce stránky.
        /// Pokud ano, pak po změně velikosti DataFormu je třeba vyvolat <see cref="RecalculateDynamicGroupLayout()"/>.
        /// </summary>
        internal bool HasDynamicGroupLayout
        {
            get
            {
                if (!__HasDynamicGroupLayout.HasValue)
                    this.RecalculateCurrentLayout(false);
                return __HasDynamicGroupLayout ?? false;
            }
            private set { __HasDynamicGroupLayout = value; }
        }
        private bool? __HasDynamicGroupLayout;
        /// <summary>
        /// Provede přepočet dynamického layoutu grup na stránce s ohledem na aktuální velikost DataFormu.
        /// Pokud je aktuální hodnota <see cref="HasDynamicGroupLayout"/> = false, tato metoda nic neprovede a rovnou skončí.
        /// </summary>
        internal void RecalculateDynamicGroupLayout()
        {
            if (!this.HasDynamicGroupLayout) return;

            Size contentSize = DataForm.VisibleContentSize;
            Size staticSize = this.CurrentStaticSize;
            // Rozmístit grupy se statickou velikostí staticSize do prostoru contentSize s dynamickým zalomením:
            foreach (var dxGroup in Groups)
            { }
            // Uložit přepočtenou velikost do CurrentSize:
            // this.CurrentSize = 
        }
        #endregion
        #region Stav stránky, umožní panelu shrnout svůj stav a uložit jej do záložky, a následně ze záložky jej promítnout do živého stavu
        /// <summary>
        /// Stav stránky, umožní panelu shrnout svůj stav a uložit jej do záložky, a následně ze záložky jej promítnout do živého stavu
        /// </summary>
        internal DxDataFormState State
        {
            get
            {
                if (__State == null)
                    __State = new DxDataFormState();
                return __State;
            }
            set
            {
                __State = value;
            }
        }
        private DxDataFormState __State;
        #endregion
        #region Implementace interface IPageItem : dovoluje umístit tuto definici stránky přímo do TabPane jako definici jeho záložky
        /// <summary>
        /// PageId stránky
        /// </summary>
        string IPageItem.PageId { get { return this.PageId; } }
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
        string ITextItem.ItemId { get { return this.PageId; } }
        /// <summary>
        /// Hlavní text v prvku
        /// </summary>
        string ITextItem.Text { get { return this.PageText; } }
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
        string ITextItem.ImageName { get { return this.PageImageName; } }
        /// <summary>
        /// Jméno ikony pro stav UnChecked u typu <see cref="RibbonItemType.CheckBoxToggle"/>
        /// </summary>
        string ITextItem.ImageNameUnChecked { get { return null; } }
        /// <summary>
        /// Jméno ikony pro stav Checked u typu <see cref="RibbonItemType.CheckBoxToggle"/>
        /// </summary>
        string ITextItem.ImageNameChecked { get { return null; } }
        /// <summary>
        /// Odvození ikony podle textu
        /// </summary>
        ImageFromCaptionType ITextItem.ImageFromCaptionMode { get { return ImageFromCaptionType.Disabled; } }
        /// <summary>
        /// Styl zobrazení
        /// </summary>
        BarItemPaintStyle ITextItem.ItemPaintStyle { get { return BarItemPaintStyle.Standard; } }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        object ITextItem.Tag { get { return null; } }
        /// <summary>
        /// Ikona ToolTipu
        /// </summary>
        string IToolTipItem.ToolTipIcon { get { return null; } }
        /// <summary>
        /// Titulek ToolTipu. Pokud nebude naplněn, vezme se text prvku.
        /// </summary>
        string IToolTipItem.ToolTipTitle { get { return this.ToolTipTitle; } }
        /// <summary>
        /// Text ToolTipu
        /// </summary>
        string IToolTipItem.ToolTipText { get { return this.ToolTipText; } }
        /// <summary>
        /// Povoluje se HTML?
        /// </summary>
        bool? IToolTipItem.ToolTipAllowHtml { get { return null; } }
        #endregion
    }
    #endregion
    #region class DxDataFormGroup : Třída reprezentující jednu designem definovanou grupu.
    /// <summary>
    /// <see cref="DxDataFormGroup"/> : Třída obsahující jednu designem definovanou grupu. Obsahuje oblasti Header, Content a odpovídající prvky.
    /// Zajišťuje vykreslení podkladu grupy.
    /// </summary>
    internal class DxDataFormGroup : DxLayoutItem
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
                    if (iGroup == null)
                        dataGroups.Add(new DxDataFormGroup(dataPage, iGroup));
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
            var items = new List<DxDataFormItem>();
            DxDataFormItem.AddToList(this, true, iGroup?.GroupHeader?.HeaderItems, items);
            DxDataFormItem.AddToList(this, false, iGroup?.Items, items);
            __Items = items;
        }
        /// <summary>Vlastník - <see cref="DxDataFormPage"/></summary>
        private DxDataFormPage __DataPage;
        /// <summary>Deklarace této grupy</summary>
        private IDataFormGroup __IGroup;
        /// <summary>Prvky v této grupě</summary>
        private List<DxDataFormItem> __Items;
        /// <summary>
        /// Vlastník - <see cref="DxDataForm"/>
        /// </summary>
        internal DxDataForm DataForm { get { return this.__DataPage?.DataForm; } }
        /// <summary>
        /// Vlastník - <see cref="DxDataFormPage"/>
        /// </summary>
        internal DxDataFormPage DataPage { get { return __DataPage; } }
        /// <summary>
        /// Aktuálně platná hodnota DeviceDpi
        /// </summary>
        internal int CurrentDpi { get { return DataForm.CurrentDpi; } }
        /// <summary>
        /// Deklarace parenta stránky = form
        /// </summary>
        internal IDataForm IForm { get { return DataForm?.IForm; } }
        /// <summary>
        /// Deklarace parenta grupy = stránka
        /// </summary>
        internal IDataFormPage IPage { get { return __DataPage?.IPage; } }
        /// <summary>
        /// Deklarace grupy
        /// </summary>
        internal IDataFormGroup IGroup { get { return __IGroup; } }
        /// <summary>
        /// Jednotlivé prvky grupy
        /// </summary>
        internal IList<DxDataFormItem> Items { get { return __Items; } }
        /// <summary>
        /// Počet celkem deklarovaných prvků
        /// </summary>
        internal int ItemsCount { get { return __Items.Count; } }
        /// <summary>
        /// Viditelnost grupy
        /// </summary>
        public override bool IsVisible { get { return IGroup.IsVisible; } set { } }
        #endregion
        #region Data o grupě
        /// <summary>
        /// Řízení layoutu: na této grupě je povoleno zalomení sloupce = tato grupa může být v případě potřeby umístěna jako první do dalšího sloupce
        /// </summary>
        internal bool LayoutAllowBreakToNewColumn { get { return (IGroup.LayoutMode.HasFlag(DatFormGroupLayoutMode.AllowBreakToNewColumn)); } }
        /// <summary>
        /// Řízení layoutu: na této grupě je povinné zalomení sloupce = tato grupa má být umístěna jako první do dalšího sloupce
        /// </summary>
        internal bool LayoutForceBreakToNewColumn { get { return (IGroup.LayoutMode.HasFlag(DatFormGroupLayoutMode.ForceBreakToNewColumn)); } }
        #endregion
        #region Layout a Souřadnice
        /// <summary>
        /// Přepočet souřadic prvků a velikosti a koordinátů grupy.
        /// </summary>
        /// <param name="callHandler"></param>
        public override void RecalculateLayout(bool callHandler)
        {
            this.PrepareGroupCoordinates();
            base.RecalculateLayout(callHandler);
            this.FinaliseGroupCoordinates();
        }
        /// <summary>
        /// Metoda připraví základní koordináty grupy pro její záhlaví a obsah, aby byl určen počátek (Origin) pro prvky typu Header i Content.
        /// Tato metoda zatím neřeší celkovou velikost grupy, protože ta bude dána až po dopočítání souřadnic prvků.
        /// Tedy setuje aktuální (Zoomované) hodnoty do <see cref="CurrentHeaderLocation"/> a <see cref="CurrentContentLocation"/>.
        /// <para/>
        /// Po této metodě může následovat příprava souřadnic jednotlivých prvků, a poté pak zdejší metoda <see cref="FinaliseGroupCoordinates()"/>.
        /// </summary>
        protected void PrepareGroupCoordinates()
        {
            // a) Koordináty designové, první část určující Header = záhlaví:
            var designCds = _DesignCoordinates;

            // b) Koordináty aktuální, Zoomuji je z Designových:
            var currentCds = _CurrentCoordinates;
            currentCds.ZoomToGui(designCds, CurrentDpi);

            // c) Uložíme si základní Current hodnoty = obsahují počátek oblasti Header a Items:
            //   => z těchto souřadnic si budou jednotlivé prvky (v metodě base.RecalculateLayout(callHandler))
            //      určovat svoje souřadnice CurrentLocalPoint = relativní k grupě
            _ReloadCurrentLocations();
        }
        /// <summary>
        /// Metoda dopočítá finální velikost grupy po dopočtení souřadnic prvků.
        /// Metoda tedy běží po určení souřadnic jednotlivých prvků grupy, a podle nich určí dynamickou velikost zdejšího prostoru Content 
        /// (=ta je dána prostorem prvků) a tedy výslednou velikost grupy.
        /// </summary>
        protected void FinaliseGroupCoordinates()
        {
            // Vypočteme velikost grupy podle obsahu:
            _CalculateCurrentSizeFromItems();

            // Uložíme si aktuální výsledné souřadnice:
            _ReloadCurrentBounds();
        }
        /// <summary>
        /// Vypočítá aktuální pixelovou velikost grupy podle zadaných designových hodnot <see cref="IDataFormGroup.DesignWidth"/> a <see cref="IDataFormGroup.DesignHeight"/>,
        /// a pokud některý z rozměrů není zadán, pak jej dopočítá podle aktuálních souřadnic prvků v Header + Content, plus zadané okraje (Border, Padding, HeaderHeight).
        /// </summary>
        private void _CalculateCurrentSizeFromItems()
        {
            var iGroup = IGroup;
            var currentDpi = CurrentDpi;
            var currentWidth = DxComponent.ZoomToGui(iGroup.DesignWidth, currentDpi);
            var currentHeight = DxComponent.ZoomToGui(iGroup.DesignHeight, currentDpi);
            var currentCds = _CurrentCoordinates;
            if (!currentWidth.HasValue || !currentWidth.HasValue)
            {   // Některý z rozměrů (nebo oba) bude určen velikostí obsahu:
                var sumTitleBounds = this.Items.Where(i => i.IsHeaderItem).Select(i => i.CurrentBounds).SummaryVisibleRectangle();
                var sumContentBounds = this.Items.Where(i => i.IsContentItem).Select(i => i.CurrentBounds).SummaryVisibleRectangle();
                var titlePadding = currentCds.HeaderTotalPadding;
                var contentPadding = currentCds.ContentTotalPadding;
                if (!currentWidth.HasValue || currentWidth.Value < 0)
                {   // Definice grupy neobsahuje explicitní šířku = určíme ji podle obsahu (Header ? Content):
                    int width = 0;
                    if (sumTitleBounds.HasValue)
                    {
                        int widthHeader = sumTitleBounds.Value.Width + titlePadding.Horizontal;
                        if (width < widthHeader) width = widthHeader;
                    }
                    else if (width < titlePadding.Horizontal)
                        width = titlePadding.Horizontal;

                    if (sumContentBounds.HasValue)
                    {
                        int widthContent = sumContentBounds.Value.Width + contentPadding.Horizontal;
                        if (width < widthContent) width = widthContent;
                    }
                    else if (width < contentPadding.Horizontal)
                        width = contentPadding.Horizontal;

                    currentWidth = width;
                }
                if (!currentHeight.HasValue || currentHeight.Value < 0)
                {   // Definice grupy neobsahuje explicitní výšku = určíme ji podle obsahu (Content + fixní Header):
                    int height = 0;
                    if (sumContentBounds.HasValue)
                    {
                        int heightContent = sumContentBounds.Value.Height + contentPadding.Vertical;
                        if (height < heightContent) height = heightContent;
                    }
                    else if (height < contentPadding.Vertical)
                        height = contentPadding.Vertical;

                    currentHeight = height;
                }
            }
            currentCds.Size = new Size(currentWidth.Value, currentHeight.Value);
        }
        /// <summary>
        /// Souřadný systém v Design hodnotách (bez Zoomu).
        /// Vždy obsahuje platnou hodnotu = není null a základní obsahuje data z <see cref="IGroup"/>.
        /// Nenapočítává ale souřadnice z podřízených prvků, obsahuje pouze Border, Padding, a údaje z GroupHeader. 
        /// Tato property nezajišťuje tedy souřadnice Content = ignoruje hodnoty <see cref="IDataFormGroup.DesignWidth"/> a <see cref="IDataFormGroup.DesignHeight"/>.
        /// </summary>
        private Coordinates _DesignCoordinates
        {
            get
            {
                if (__DesignCoordinates is null) __DesignCoordinates = new Coordinates();
                if (!__DesignCoordinatesValid) __DesignCoordinatesValid = _DesignCoordinatesReload(__DesignCoordinates);
                return __DesignCoordinates;
            }
        }
        /// <summary>
        /// Do předané instance designových koordinátů naplní základní statická data z definice grupy.
        /// Nenačítají se hodnoty <see cref="IDataFormGroup.DesignWidth"/> a <see cref="IDataFormGroup.DesignHeight"/>.
        /// </summary>
        /// <param name="designCds"></param>
        private bool _DesignCoordinatesReload(Coordinates designCds)
        {
            var iGroup = IGroup;
            designCds.BorderRange = iGroup.DesignBorderRange;
            designCds.ContentPadding = iGroup.DesignPadding;
            var groupHeader = iGroup.GroupHeader;
            if (groupHeader != null && groupHeader.DesignHeaderHeight.HasValue && groupHeader.DesignHeaderHeight.Value > 0)
            {
                designCds.HeaderHeight = groupHeader.DesignHeaderHeight.Value;
                designCds.HeaderPadding = groupHeader.DesignTitlePadding;
                designCds.LineYRange = groupHeader.DesignLineRange;
            }
            return true;
        }
        /// <summary>
        /// Souřadný systém v Design hodnotách (bez Zoomu).
        /// </summary>
        private Coordinates __DesignCoordinates;
        /// <summary>
        /// Platnost dat v <see cref="__DesignCoordinates"/>
        /// </summary>
        private bool __DesignCoordinatesValid;
        /// <summary>
        /// Souřadný systém v Current hodnotách (včetně Zoomu a DPI).
        /// Není null.
        /// </summary>
        private Coordinates _CurrentCoordinates
        {
            get
            {
                if (__CurrentCoordinates is null) __CurrentCoordinates = new Coordinates();
                return __CurrentCoordinates;
            }
        }
        private Coordinates __CurrentCoordinates;
        /// <summary>
        /// Uloží si aktuální výsledné pozice základních oblastí (z vypočítávaných properties z <see cref="_CurrentCoordinates"/> do jednoduchých privátních fields proměnných).
        /// Z těchto souřadnic si budou jednotlivé prvky (v metodě base.RecalculateLayout(callHandler)) určovat svoje souřadnice CurrentLocalPoint = relativní k grupě.
        /// </summary>
        private void _ReloadCurrentLocations()
        {
            var currentCds = _CurrentCoordinates;
            CurrentHeaderLocation = currentCds.HeaderBounds.Location;
            CurrentContentLocation = currentCds.ContentBounds.Location;
        }
        /// <summary>
        /// Uloží si aktuální výsledné souřadnice (z vypočítávaných properties z <see cref="_CurrentCoordinates"/> do jednoduchých privátních fields proměnných).
        /// Volá se po přepočtech a po změně Minimised.
        /// </summary>
        private void _ReloadCurrentBounds()
        {
            var currentCds = _CurrentCoordinates;

            this.CurrentGroupSize = currentCds.Size;
            this.CurrentBorderExists = currentCds.HasBorder;
            this.CurrentBorderOuterBounds = currentCds.BorderOuterBounds;
            this.CurrentBorderSizes = currentCds.BorderSizes;
            this.CurrentGroupBackgroundBounds = currentCds.GroupBackgroundBounds;
            this.CurrentHeaderExists = currentCds.HasHeader;
            this.CurrentHeaderBackgroundBounds = currentCds.HeaderBackgroundBounds;
            this.CurrentHeaderBounds = currentCds.HeaderBounds;
            this.CurrentLineExists = currentCds.HasLine;
            this.CurrentLineBounds = currentCds.LineBounds;
            this.CurrentContentExists = currentCds.HasContent;
            this.CurrentContentBackgroundBounds = currentCds.ContentBackgroundBounds;
            this.CurrentContentBounds = currentCds.ContentBounds;
        }
        #endregion
        #region Souřadnice aktuální, viditelné, pixelové;  testy viditelnosti
        /// <summary>
        /// Current souřadnice počátku prvků v oblasti Header (aktuální pixely včetně Zoomu a DPI).
        /// <para/>
        /// Je relativní v rámci this grupy (bod 0/0 je na počátku this grupy).
        /// </summary>
        public Point CurrentHeaderLocation { get { return __CurrentHeaderLocation; } private set { __CurrentHeaderLocation = value; } }
        private Point __CurrentHeaderLocation;
        /// <summary>
        /// Current souřadnice počátku prvků v oblasti Content (aktuální pixely včetně Zoomu a DPI).
        /// <para/>
        /// Je relativní v rámci this grupy (bod 0/0 je na počátku this grupy).
        /// </summary>
        public Point CurrentContentLocation { get { return __CurrentContentLocation; } private set { __CurrentContentLocation = value; } }
        private Point __CurrentContentLocation;
        /// <summary>
        /// Aktuální velikost prvku.
        /// Hodnota je v aktuálních pixelech (nikoli designové pixely) = přepočteno Zoomem a DPI.
        /// </summary>
        public override Size CurrentSize { get { return __CurrentGroupSize; } set { /* Velikost se setuje jinde */ } }
        /// <summary>
        /// Aktuální velikost prvku. Lze setovat.
        /// </summary>
        protected Size CurrentGroupSize { get { return __CurrentGroupSize; } set { __CurrentGroupSize = value; } }
        private Size __CurrentGroupSize;
        /// <summary>
        /// Souřadnice, kde tento prvek (grupa) má svůj vnější počátek v rámci svého parenta = lokální souřadice.
        /// Hodnota je v aktuálních pixelech (nikoli designové pixely) = je už přepočtena Zoomem a DPI.
        /// <para/>
        /// Tuto hodnotu do prvku může vkládat jeho Parent, pokud ten si rozmísťuje svoje Child prvky,
        /// nebo ji může setovat prvek sám, pokud má danou fixní pozici.
        /// </summary>
        public override Point CurrentLocalPoint { get => base.CurrentLocalPoint; set => base.CurrentLocalPoint = value; }
        /// <summary>
        /// Existuje Border?
        /// </summary>
        protected bool CurrentBorderExists { get { return __CurrentBorderExists; } private set { __CurrentBorderExists = value; } }
        private bool __CurrentBorderExists;
        /// <summary>
        /// Vnější souřadnice Border.
        /// <para/>
        /// Je relativní v rámci this grupy (bod 0/0 je na počátku this grupy).
        /// </summary>
        protected Rectangle CurrentBorderOuterBounds { get { return __CurrentBorderOuterBounds; } private set { __CurrentBorderOuterBounds = value; } }
        private Rectangle __CurrentBorderOuterBounds;
        /// <summary>
        /// Šířky rámečku Border.
        /// </summary>
        protected Padding CurrentBorderSizes { get { return __CurrentBorderSizes; } private set { __CurrentBorderSizes = value; } }
        private Padding __CurrentBorderSizes;
        /// <summary>
        /// Celý prostor pozadí grupy (Header + Content), leží přesně uvnitř Borderu.
        /// <para/>
        /// Je relativní v rámci this grupy (bod 0/0 je na počátku this grupy).
        /// </summary>
        protected Rectangle CurrentGroupBackgroundBounds { get { return __CurrentBackgroundBounds; } private set { __CurrentBackgroundBounds = value; } }
        private Rectangle __CurrentBackgroundBounds;
        /// <summary>
        /// Existuje Header?
        /// </summary>
        protected bool CurrentHeaderExists { get { return __CurrentHeaderExists; } private set { __CurrentHeaderExists = value; } }
        private bool __CurrentHeaderExists;
        /// <summary>
        /// Celý prostor pozadí oblasti Header. 
        /// Vykresluje se stylem Header.
        /// Uvnitř tohoto prostoru se nachází <see cref="CurrentHeaderBounds"/>.
        /// <para/>
        /// Je relativní v rámci this grupy (bod 0/0 je na počátku this grupy).
        /// </summary>
        protected Rectangle CurrentHeaderBackgroundBounds { get { return __CurrentHeaderBackgroundBounds; } private set { __CurrentHeaderBackgroundBounds = value; } }
        private Rectangle __CurrentHeaderBackgroundBounds;
        /// <summary>
        /// Prostor, ve kterém je vykreslen text titulku a případné další prvky typu Header.
        /// Je umístěn v <see cref="CurrentHeaderBackgroundBounds"/> a je oproti němu zmenšen o HeaderPadding dovnitř.
        /// <para/>
        /// Je relativní v rámci this grupy (bod 0/0 je na počátku this grupy).
        /// </summary>
        protected Rectangle CurrentHeaderBounds { get { return __CurrentHeaderBounds; } private set { __CurrentHeaderBounds = value; } }
        private Rectangle __CurrentHeaderBounds;
        /// <summary>
        /// Existuje Line?
        /// </summary>
        protected bool CurrentLineExists { get { return __CurrentLineExists; } private set { __CurrentLineExists = value; } }
        private bool __CurrentLineExists;
        /// <summary>
        /// Souřadnice Line.
        /// <para/>
        /// Je relativní v rámci this grupy (bod 0/0 je na počátku this grupy).
        /// </summary>
        protected Rectangle CurrentLineBounds { get { return __CurrentLineBounds; } private set { __CurrentLineBounds = value; } }
        private Rectangle __CurrentLineBounds;
        /// <summary>
        /// Existuje Content? false když je Minimised
        /// </summary>
        protected bool CurrentContentExists { get { return __CurrentContentExists; } private set { __CurrentContentExists = value; } }
        private bool __CurrentContentExists;
        /// <summary>
        /// Celý prostor pozadí oblasti Content. 
        /// Vykresluje se stylem Content.
        /// Uvnitř tohoto prostoru se nachází <see cref="CurrentHeaderBounds"/>.
        /// <para/>
        /// Je relativní v rámci this grupy (bod 0/0 je na počátku this grupy).
        /// </summary>
        protected Rectangle CurrentContentBackgroundBounds { get { return __CurrentContentBackgroundBounds; } private set { __CurrentContentBackgroundBounds = value; } }
        private Rectangle __CurrentContentBackgroundBounds;
        /// <summary>
        /// Souřadnice a velikost prostoru v rámci grupy, ve kterém jsou zobrazeny jednotlivé Items = <see cref="DxDataFormItem"/>.
        /// <para/>
        /// Je relativní v rámci this grupy (bod 0/0 je na počátku this grupy).
        /// </summary>
        internal Rectangle CurrentContentBounds { get { return __CurrentContentBounds; } private set { __CurrentContentBounds = value; } }
        private Rectangle __CurrentContentBounds;
        /// <summary>
        /// Vrátí true, pokud daný absolutní bod se nachází v this grupě.
        /// Využije k tomu souřadnici <see cref="AbsoluteGroupBackgroundBounds"/> = prostor uvnitř borderu.
        /// </summary>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        public bool IsVisibleOnPoint(Point absolutePoint)
        {
            return (IsVisible && AbsoluteGroupBackgroundBounds.Contains(absolutePoint));
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
            if (!this.CurrentBorderExists) return;

            var appearance = IGroup.BorderAppearance;
            if (appearance == null) return;

            DxDataForm.PaintFrame(e, AbsoluteBorderOuterBounds, CurrentBorderSizes, appearance, onMouse, hasFocus);
        }
        /// <summary>
        /// Vykreslí všechna pozadí ve správném pořadí
        /// </summary>
        /// <param name="e"></param>
        /// <param name="onMouse"></param>
        /// <param name="hasFocus"></param>
        private void _PaintBackgrounds(DxBufferedGraphicPaintArgs e, bool onMouse, bool hasFocus)
        {
            var iGroup = IGroup;
            if (CurrentHeaderExists)
                DxDataForm.PaintBackground(e, AbsoluteHeaderBackgroundBounds, iGroup.GroupHeader?.BackgroundAppearance, onMouse, hasFocus, false);   // Pozadí pod titulkem

            if (CurrentContentExists)
                DxDataForm.PaintBackground(e, AbsoluteContentBackgroundBounds, iGroup.BackgroundAppearance, onMouse, hasFocus, false);               // Pozadí pod obsahem

            if (CurrentLineExists)
                DxDataForm.PaintBackground(e, AbsoluteLineBounds, iGroup.GroupHeader?.LineAppearance, onMouse, hasFocus, false);                     // Linka, nad obrázkem, pod texty

            DxDataForm.PaintImage(e, AbsoluteGroupBackgroundBounds, iGroup.BackgroundAppearance);                                                    // Obrázek na pozadí celé grupy
        }
        /// <summary>
        /// Absolutní souřadnice Border, vnější
        /// </summary>
        protected Rectangle AbsoluteBorderOuterBounds { get { return this.GetAbsoluteBounds(CurrentBorderOuterBounds); } }
        /// <summary>
        /// Absolutní souřadnice pozadí celé grupy (Header + Content)
        /// </summary>
        protected Rectangle AbsoluteGroupBackgroundBounds { get { return this.GetAbsoluteBounds(CurrentGroupBackgroundBounds); } }
        /// <summary>
        /// Absolutní souřadnice pozadí pod Header
        /// </summary>
        protected Rectangle AbsoluteHeaderBackgroundBounds { get { return this.GetAbsoluteBounds(CurrentHeaderBackgroundBounds); } }
        /// <summary>
        /// Absolutní souřadnice Line
        /// </summary>
        protected Rectangle AbsoluteLineBounds { get { return this.GetAbsoluteBounds(CurrentLineBounds); } }
        /// <summary>
        /// Absolutní souřadnice pozadí pod Content
        /// </summary>
        protected Rectangle AbsoluteContentBackgroundBounds { get { return this.GetAbsoluteBounds(CurrentContentBackgroundBounds); } }
        #endregion
        #region class Coordinates : souřadnice různých míst v grupě
        /// <summary>
        /// Souřadný systém uvnitř grupy.
        /// Určuje souřadnice borderu, titulkového prostoru a prostoru pro obsah;
        /// včetně linky v titulku - se zachováním Paddingu.
        /// </summary>
        private class Coordinates
        {
            #region Vnější souřadnice a suma režie
            /// <summary>
            /// Souřadnice grupy zvenku = v prostoru Parent controlu
            /// </summary>
            public Point Location { get { return __Location; } set { __Location = value; } }
            private Point __Location;
            /// <summary>
            /// Velikost grupy celková
            /// </summary>
            public Size Size { get { return __Size; } set { __Size = value; } }
            private Size __Size;
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
            /// Tuto hodnotu je možno číst po zadání <see cref="BorderRange"/>, <see cref="ContentPadding"/>, <see cref="HeaderHeight"/>.
            /// </summary>
            public Size SizeOverhead
            {
                get
                {
                    int bs = 2 * _BorderEnd;
                    int pw = __ContentPaddingLeft + __ContentPaddingRight;
                    int ph = __ContentPaddingTop + __ContentPaddingBottom;
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
            #region Border, BorderOuterBounds, GroupBackgroundBounds
            /// <summary>
            /// Bude se kreslit Border? To je tehdy, když <see cref="BorderRange"/> má kladnou velikost.
            /// Nicméně i když se Border nekreslí, akceptuje se jeho hodnota <see cref="BorderRange"/>.Begin, o kterou se zmenšuje vnější prostor.
            /// Prostor mezi začátkem grupy (<see cref="Bounds"/>) a začátkem Borderu se nijak nevykresluje, a má tedy barvu a vzhled parent controlu.
            /// </summary>
            public bool HasBorder { get { return (__BorderEnd > __BorderBegin); } }
            /// <summary>
            /// Umístění a velikost Borderu, měřeno od samotného okraje grupy směrem dovnitř, bez <see cref="ContentPadding"/>.
            /// Pokud border není viditelný, je zde null.
            /// </summary>
            public Int32Range BorderRange
            {
                get { return (HasBorder ? new Int32Range(__BorderBegin, __BorderEnd) : null); }
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
            /// Souřadnice vnějšího Borderu, může být Empty když <see cref="HasBorder"/> je false.
            /// Je relativní v rámci grupy.
            /// Je umístěn v prostoru <see cref="Bounds"/> s odstupem <see cref="_BorderBegin"/> od všech čtyř stran.
            /// </summary>
            public Rectangle BorderOuterBounds
            {
                get
                {
                    if (!HasBorder) return Rectangle.Empty;
                    int bb = _BorderBegin;
                    return new Rectangle(bb, bb, Size.Width - 2 * bb, Size.Height - 2 * bb);
                }
            }
            /// <summary>
            /// Šířky rámečku Border.
            /// </summary>
            public Padding BorderSizes
            {
                get
                {
                    if (!HasBorder) return Padding.Empty;
                    int bt = _BorderThick;
                    return new Padding(bt);
                }
            }
            /// <summary>
            /// Souřadnice pozadí celé grupy, je přímo uvnitř Borderu (tzn. v rámci tohoto <see cref="GroupBackgroundBounds"/> se nachází i <see cref="ContentPadding"/>)
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
            /// Výška prostoru pro titulek. 
            /// Nikdy není záporná, hodnota 0 = není titulek (pak <see cref="HasHeader"/> je false).
            /// Uvnitř této výšky se nachází okraje <see cref="HeaderPadding"/> a v nich pak <see cref="HeaderBounds"/> = prostor pro obsah záhlaví.
            /// Uvnitř této výšky se rovněž nachází linka <see cref="LineBounds"/>.
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
                get { return (HasHeader ? new Padding(__HeaderPaddingLeft, __HeaderPaddingTop, __HeaderPaddingRight, __HeaderPaddingBottom) : Padding.Empty); }
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
            /// Úplně okraje mezi vnějším obrysem grupy a vnitřním prostorem Header = obsahuje <see cref="BorderRange"/> + <see cref="HeaderPadding"/>.
            /// Pokud neexistuje Header (<see cref="HasHeader"/> je false), pak je zde <see cref="Padding.Empty"/>.
            /// Ve směru Y neobsahuje nic ve smyslu Content.
            /// </summary>
            public Padding HeaderTotalPadding
            {
                get
                {
                    if (!HasHeader) return Padding.Empty;

                    int b = _BorderEnd;
                    return new Padding(b + __HeaderPaddingLeft, b + __HeaderPaddingTop, b + __HeaderPaddingRight, b + __HeaderPaddingBottom);
                }
            }
            /// <summary>
            /// Souřadnice pozadí titulku. 
            /// Nachází se přesně uvnitř <see cref="GroupBackgroundBounds"/>, a má výšku <see cref="HeaderHeight"/>.
            /// Je relativní v rámci this grupy.
            /// <para/>
            /// Tento prostor má být vybarven barvou pozadí a případně obsahuje obrázek pozadí ve vhodném zarovnání.
            /// Pak je obrázek zarovnán přesně k Borderu.
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
            /// Prostor, ve kterém jsou umístěny prvky Headeru = texty, labely, buttony.
            /// Je umístěn v <see cref="HeaderBackgroundBounds"/> a je oproti němu zmenšen o <see cref="HeaderPadding"/> dovnitř.
            /// Je relativní v rámci this grupy.
            /// </summary>
            public Rectangle HeaderBounds
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
            #endregion
            #region Line
            /// <summary>
            /// Je viditelná linka v oblasti titulku?
            /// Obsahuje true, pokud existuje záhlaví, a jsou zadány souřadnice linky, a nejsme <see cref="IsMinimised"/>
            /// </summary>
            public bool HasLine { get { return HasHeader && (_LineYEnd > _LineYBegin) && !IsMinimised; } }
            /// <summary>
            /// Umístění linky v oblasti titulku, měřeno od horního okraje titulku, bez <see cref="ContentPadding"/>.
            /// Pozor, relativně k <see cref="HeaderBackgroundBounds"/>.
            /// Pokud linka není viditelná, je zde null.
            /// </summary>
            public Int32Range LineYRange
            {
                get { return (HasLine ? new Int32Range(_LineYBegin, _LineYEnd) : null); }
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
            /// Prostor, ve kterém je vykreslena linka titulku. Může být Empty.
            /// Obecně je umístěn v <see cref="HeaderBackgroundBounds"/>.
            /// Ve směru X je od okrajů odsazen o Padding, ve směru Y nikoliv, ale je umístěn na pixelech <see cref="LineYRange"/> vůči souřadnici <see cref="HeaderBackgroundBounds"/>.Y
            /// Je relativní v rámci this grupy.
            /// </summary>
            public Rectangle LineBounds
            {
                get
                {
                    if (!HasHeader || !HasLine) return Rectangle.Empty;
                    var bounds = HeaderBackgroundBounds;
                    int lb = _LineYBegin;
                    int le = _LineYEnd;
                    if (le < lb) return Rectangle.Empty;
                    int pl = __ContentPaddingLeft;
                    int pw = pl + __ContentPaddingRight;
                    return new Rectangle(bounds.X + pl, bounds.Y + lb, bounds.Width - pw, le - lb);
                }
            }
            #endregion
            #region Content
            /// <summary>
            /// Obsahuje true, pokud se má zobrazovat Content = nejsme <see cref="IsMinimised"/>
            /// </summary>
            public bool HasContent { get { return !IsMinimised; } }
            /// <summary>
            /// Souřadnice pozadí vlastního obsahu pod titulkem.
            /// Nachází se přesně uvnitř <see cref="GroupBackgroundBounds"/>, nahoře je zmenšený o prostor titulku a má souřadnici Y = <see cref="HeaderHeight"/>.
            /// Je relativní v rámci this grupy.
            /// <para/>
            /// Tento prostor má být vybarven barvou pozadí a případně obsahuje obrázek pozadí ve vhodném zarovnání.
            /// Uvnitř tohoto prostoru se nachází <see cref="ContentPadding"/>, a v něm pak <see cref="ContentBounds"/>.
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
            /// Vnitřní okraje mezi vnitřkem Borderu a začátkem reálného prostoru Content pro prvky <see cref="ContentBounds"/>.
            /// Záporné hodnoty jsou nahrazeny 0.
            /// </summary>
            public Padding ContentPadding
            {
                get { return new Padding(__ContentPaddingLeft, __ContentPaddingTop, __ContentPaddingRight, __ContentPaddingBottom); }
                set
                {
                    __ContentPaddingLeft = (value.Left > 0 ? value.Left : 0);
                    __ContentPaddingTop = (value.Top > 0 ? value.Top : 0);
                    __ContentPaddingRight = (value.Right > 0 ? value.Right : 0);
                    __ContentPaddingBottom = (value.Bottom > 0 ? value.Bottom : 0);
                }
            }
            private int __ContentPaddingLeft;
            private int __ContentPaddingTop;
            private int __ContentPaddingRight;
            private int __ContentPaddingBottom;
            /// <summary>
            /// Úplné okraje mezi vnějším obrysem grupy a vnitřním prostorem Content = obsahuje <see cref="BorderRange"/> + <see cref="HeaderPadding"/>.
            /// Pozor: Ve směru Y obsahuje i kompletní prostor pro Header = <see cref="HeaderHeight"/>, pokud je definován.
            /// Lze tedy z požadovaného vnitřního prostoru pro Content po přičtení tohoto <see cref="ContentTotalPadding"/> určit vnější rozměr grupy.
            /// </summary>
            public Padding ContentTotalPadding
            {
                get
                {
                    int b = _BorderEnd;
                    var dy = (HasHeader ? HeaderHeight : 0);
                    return new Padding(b + __ContentPaddingLeft, b + dy + __ContentPaddingTop, b + __ContentPaddingRight, b + __ContentPaddingBottom);
                }
            }
            /// <summary>
            /// Souřadnice vlastního obsahu = jednotlivé prvky grupy.
            /// Nachází se přesně uvnitř <see cref="ContentBackgroundBounds"/>, a je zmenšen o <see cref="ContentPadding"/> dovnitř.
            /// Je relativní v rámci this grupy.
            /// <para/>
            /// Relativně v tomto prostoru se nachází jednotlivé prvky grupy.
            /// </summary>
            public Rectangle ContentBounds
            {
                get
                {
                    var bounds = ContentBackgroundBounds;
                    int pl = __ContentPaddingLeft;
                    int pt = __ContentPaddingTop;
                    int pw = pl + __ContentPaddingRight;
                    int ph = pt + __ContentPaddingBottom;
                    return new Rectangle(bounds.X + pl, bounds.Y + pt, bounds.Width - pw, bounds.Height - ph);
                }
            }
            #endregion
            #region Minimized
            /// <summary>
            /// Lze řídit, zda grupa může být Minimized.
            /// Pokud je zde true, pak grupa zobrazuje odpovídající tlačítko a reaguje na hodnotu <see cref="IsMinimised"/>.
            /// </summary>
            public bool EnableMinimise
            {
                get { return __EnableMinimise; }
                set { __EnableMinimise = value; }
            }
            private bool __EnableMinimise;
            /// <summary>
            /// Lze řídit, zda je/není grupa Minimized.
            /// Pokud je Minimized, pak grupa navenek zobrazuje jen Header, a nikoli Line a Content.
            /// Pokud je false (výchozí stav), pak je grupa standardní.
            /// Setovat <see cref="IsMinimised"/> = true má význam pouze u grupy, která má povoleno <see cref="EnableMinimise"/> = true.
            /// Setování hodnoty změní odpovídající hodnoty v this instanci, ale samo o sobě nevyvolá změnu vzhledu - je třeba zavolat vhodný Refresh.
            /// </summary>
            public bool IsMinimised
            {
                get { return __EnableMinimise && __IsMinimised; }
                set { __IsMinimised = value; }
            }
            private bool __IsMinimised;
            #endregion
            #region Zoomování
            /// <summary>
            /// Vypočítá svoje vnitřní hodnoty na aktuální, podle dodaných hodnot designových a podle daného DPI.
            /// Přepočte i hodnoty <see cref="Location"/> a <see cref="Size"/>, tedy i <see cref="Bounds"/>.
            /// </summary>
            /// <param name="sourceDesignCoordinates"></param>
            /// <param name="currentDpi"></param>
            internal void ZoomToGui(Coordinates sourceDesignCoordinates, int currentDpi)
            {
                this.__HeaderHeight = zoomToGui(sourceDesignCoordinates.__HeaderHeight);
                this.__BorderBegin = zoomToGui(sourceDesignCoordinates.__BorderBegin);
                this.__BorderEnd = zoomToGui(sourceDesignCoordinates.__BorderEnd);
                this.__HeaderPaddingLeft = zoomToGui(sourceDesignCoordinates.__HeaderPaddingLeft);
                this.__HeaderPaddingTop = zoomToGui(sourceDesignCoordinates.__HeaderPaddingTop);
                this.__HeaderPaddingRight = zoomToGui(sourceDesignCoordinates.__HeaderPaddingRight);
                this.__HeaderPaddingBottom = zoomToGui(sourceDesignCoordinates.__HeaderPaddingBottom);
                this.__LineYBegin = zoomToGui(sourceDesignCoordinates.__LineYBegin);
                this.__LineYEnd = zoomToGui(sourceDesignCoordinates.__LineYEnd);
                this.__ContentPaddingLeft = zoomToGui(sourceDesignCoordinates.__ContentPaddingLeft);
                this.__ContentPaddingTop = zoomToGui(sourceDesignCoordinates.__ContentPaddingTop);
                this.__ContentPaddingRight = zoomToGui(sourceDesignCoordinates.__ContentPaddingRight);
                this.__ContentPaddingBottom = zoomToGui(sourceDesignCoordinates.__ContentPaddingBottom);

                this.__Location = DxComponent.ZoomToGui(sourceDesignCoordinates.__Location, currentDpi);
                this.__Size = DxComponent.ZoomToGui(sourceDesignCoordinates.__Size, currentDpi);

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
    internal class DxDataFormItem : DxLayoutItem
    {
        #region Konstruktor, vlastník, prvky
        /// <summary>
        /// Vytvoří a vrátí List obsahující <see cref="DxDataFormItem"/>, vytvořené z dodaných instancí <see cref="IDataFormColumn"/>.
        /// </summary>
        /// <param name="dataGroup"></param>
        /// <param name="isHeaderItems">false pro běžné columny v prostoru Content, true pro columny v prostoru Header (=text titulku, ikony, atd)</param>
        /// <param name="iItems"></param>
        /// <returns></returns>
        public static List<DxDataFormItem> CreateList(DxDataFormGroup dataGroup, bool isHeaderItems, IEnumerable<IDataFormColumn> iItems)
        {
            List<DxDataFormItem> dxItems = new List<DxDataFormItem>();
            AddToList(dataGroup, isHeaderItems, iItems, dxItems);
            return dxItems;
        }
        /// <summary>
        /// Naplní do dodaného Listu prvky <see cref="DxDataFormItem"/> vytvořené z dodaných instancí <see cref="IDataFormColumn"/>.
        /// </summary>
        /// <param name="dataGroup"></param>
        /// <param name="isHeaderItems">false pro běžné columny v prostoru Content, true pro columny v prostoru Header (=text titulku, ikony, atd)</param>
        /// <param name="iItems"></param>
        /// <param name="dxItems"></param>
        /// <returns></returns>
        public static void AddToList(DxDataFormGroup dataGroup, bool isHeaderItems, IEnumerable<IDataFormColumn> iItems, List<DxDataFormItem> dxItems)
        {
            if (iItems != null)
            {
                foreach (IDataFormColumn iItem in iItems)
                {
                    if (iItem == null) continue;
                    dxItems.Add(new DxDataFormItem(dataGroup, isHeaderItems, iItem));
                }
            }
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataGroup"></param>
        /// <param name="isTitleItem">false pro běžné columny v prostoru Content, true pro columny v prostoru Title (=text titulku, ikony, atd)</param>
        /// <param name="iItem"></param>
        public DxDataFormItem(DxDataFormGroup dataGroup, bool isTitleItem, IDataFormColumn iItem)
            : base()
        {
            __DataGroup = dataGroup;
            __IsHeaderItem = isTitleItem;
            __IItem = iItem;
        }
        /// <summary>Vlastník - <see cref="DxDataFormGroup"/></summary>
        private DxDataFormGroup __DataGroup;
        /// <summary>Hodnota false = prvek je umístěn v prostoru Content / Hodnota true = prvek je umístěn v prostoru Title</summary>
        private bool __IsHeaderItem;
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
        /// <summary>Hodnota true = prvek je umístěn v prostoru Header / false = jinde (nejspíš <see cref="IsContentItem"/>)</summary>
        public bool IsHeaderItem { get { return __IsHeaderItem; } }
        /// <summary>Hodnota true = prvek je umístěn v prostoru Content / false = jinde (nejspíš <see cref="IsHeaderItem"/>)</summary>
        public bool IsContentItem { get { return !__IsHeaderItem; } }
        /// <summary>
        /// Deklarace vlastností prvku = od designera
        /// </summary>
        public IDataFormColumn IItem { get { return __IItem; } }
        #endregion
        #region Data z prvku
        /// <summary>
        /// Typ prvku
        /// </summary>
        public DataFormColumnType ItemType { get { return __IItem.ColumnType; } }
        /// <summary>
        /// Viditelnost prvku, pochází z <see cref="IDataFormColumn.IsVisible"/>
        /// </summary>
        public override bool IsVisible { get { return IItem.IsVisible; } set { } }
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
        #region Souřadnice prvku Current
        /// <summary>
        /// Metoda určí zdejší aktuální souřadnici a velikost, podle dat v definici prvku a podle definice souřadnic v grupě
        /// </summary>
        /// <param name="callHandler"></param>
        protected override void RecalculateCurrentLayout(bool callHandler)
        {
            var group = this.DataGroup;
            var currentBounds = DxComponent.ZoomToGui(this.IItem.DesignBounds, this.CurrentDPI);                       // Souřadnice dané designem
            var originPoint = (IsHeaderItem ? group.CurrentHeaderLocation : group.CurrentContentLocation);             // Výchozí bod (počátek oblasti) v rámci grupy
            this.CurrentBounds = currentBounds.Add(originPoint);
        }
        /// <summary>
        /// Souřadnice this prvku v prostoru parenta = grupy. 
        /// Tyto souřadnice již zahrnují typ prvku (<see cref="IsHeaderItem"/> nebo <see cref="IsContentItem"/>)
        /// a jsou tedy umístěny do tomu odpovídajícího prostoru v grupě.
        /// Jde o souřadnice pixelové (přepočítané Zoomem a DPI).
        /// </summary>
        public Rectangle CurrentBounds
        {
            get { return new Rectangle(CurrentLocalPoint, CurrentSize); }
            set { CurrentLocalPoint = value.Location; CurrentSize = value.Size; }
        }

        /// <summary>
        /// Fyzické pixelové souřadnice tohoto prvku na vizuálním controlu, kde se nyní tento prvek nachází.
        /// Jde o vizuální souřadnice v koordinátech controlu, odpovídají např. pohybu myši.
        /// Může být null, pak prvek není zobrazen. Null je i po invalidaci <see cref="InvalidateBounds()"/>.
        /// Tuto hodnotu ukládá řídící třída v procesu kreslení jako reálné souřadnice, kam byl prvek vykreslen.
        /// </summary>
        public Rectangle? VisibleBounds { get { return __VisibleBounds; } set { __VisibleBounds = value; } }
        private Rectangle? __VisibleBounds;
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

    // Provozní vizuální prvky, které se reálně účastní zobrazování:
    #region class DxDataFormPanel : Jeden panel dataformu: reprezentuje celý panel zobrazující plochu se všemi daty = jedna nebo více částí "DxDataFormPart"
    /// <summary>
    /// Jeden panel dataformu: reprezentuje celý panel zobrazující plochu se všemi daty = jedna nebo více částí <see cref="DxDataFormPart"/>.
    /// Je umístěn buď přímo v <see cref="DxDataForm"/> v celé jeho ploše (to když DataForm obsahuje jen jednu stránku, pak jde o DataForm bez záložek);
    /// anebo je umístěn na určité stránce záložkovníku <see cref="DxTabPane"/>, pak jde o vícezáložkový DataForm.
    /// Tuto volbu řídí <see cref="DxDataForm"/> podle dodaných stránek a podle dynamického layoutu.
    /// <para/>
    /// Panel <see cref="DxDataFormPanel"/> v sobě obsahuje definici skupin <see cref="DxDataFormGroup"/> v property <see cref="Groups"/>, která určuje layout prvků dataformu.
    /// <para/>
    /// Jeden panel <see cref="DxDataFormPanel"/> v sobě hostuje přinejmenším jednu nebo více částí <see cref="DxDataFormPart"/>.
    /// Každá jedna část <see cref="DxDataFormPart"/> v sobě zobrazuje fyzická data, může mít / nemusí mít ScrollBary a Headery.
    /// Tyto části mohou být vzájemně spřažené (jeden svislý Scrollbar zobrazený úplně vpravo může ovládat více částí umístěných vedle sebe = vlevo).
    /// Přidávání a odebírání částí řídí <see cref="DxDataFormPanel"/>, stejně tak mezi ně vkládá Splittery a řídí jejich velikost.
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
        }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
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
            this._Parts.ForEachExec(p => p.Refresh(refreshParts));

            if (refreshParts.HasFlag(RefreshParts.RefreshControl))
                base.Refresh();
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
        #region Groups = definice vzhledu aktuálního DataFormu (layout)

        #endregion
        #region Parts = jednotlivé části DataFormu (splitterem oddělené bloky řádků nebo sloupců), výchozí je jedna část přes celý prostor panelu
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
    }
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
    #endregion
    #region class DxDataFormPart : Jedna oddělená a samostatná skupina řádků a sloupců v rámci DataFormu = vizuální Control
    /// <summary>
    /// <see cref="DxDataFormPart"/> : Jedna oddělená a samostatná skupina řádků/sloupců v rámci panelu DataFormu <see cref="DxDataFormPanel"/>.
    /// Toto je primární zobrazovací vizuální Control!
    /// <para/>
    /// Prostor DataFormu (přesněji <see cref="DxDataFormPanel"/>) může být rozdělen na více sousedících částí = <see cref="DxDataFormPart"/>,
    /// které zobrazují tatáž data, ale jsou nascrollovaná na jiná místa, nebo mohou mít odlišné filtry a zobrazovat tedy jiné podmnožiny řádků.
    /// <para/>
    /// Toto rozčlenění povoluje a řídí <see cref="DxDataFormPanel"/> jako fyzický Parent těchto částí, pokyny k rozdělení dostává od hlavního <see cref="DxDataForm"/>.
    /// K interaktivní změně dává uživateli k dispozici vhodné Splittery.
    /// Rozdělení provádí uživatel pomocí "tahacího" tlačítka vpravo nahoře a následného zobrazení splitteru.
    /// Dostupnost Splitterů v jednotlivých částech v rámci <see cref="DxDataFormPanel"/> řídí <see cref="DxDataFormPanel"/>; 
    /// Splittery jsou dostupné vždy v té krajní části v daném směru = vlevo svislý a nahoře vodorovný.
    /// <para/>
    /// Posouvání obsahu řídí Scrollbary, které nabízí vždy ta poslední <see cref="DxDataFormPart"/> v daném směru: svislý Scrollbar zobrazuje jen nejpravější část, 
    /// vodorovný Scrollbar zobrazuje jen nejspodnější část.
    /// Synchronizaci sousedních částí, které nemají svůj vlastní odpovídající Scrollbar, zajišťuje <see cref="DxDataFormPanel"/>.
    /// <para/>
    /// Každá jedna skupina <see cref="DxDataFormPart"/>, a skládá se z částí: RowHeader, ColumnHeader, RowFilter, Rows, SummaryRows a Footer.
    /// Tyto části jsou jednotlivě volitelné - odlišně pro první skupinu, pro vnitřní skupiny a pro skupinu poslední.
    /// Části Header, RowFilter jsou fixní k hornímu okraji a nescrollují;
    /// Části Rows, SummaryRows scrollují uprostřed;
    /// Část Footer je fixní k dolnímu okraji a nescrolluje.
    /// Podkladový ScrollPanel <see cref="DxScrollableContent"/> dovoluje nastavit libovolné okraje kolem scrollovaného obsahu <see cref="DxScrollableContent.ContentVisualPadding"/>, 
    /// tyto okraje jsou využívány pro zobrazení "fixních" částí (vše okolo Rows) = ColumnHeader, RowFilter, SummaryRow, RowHeader.
    /// <para/>
    /// Typicky Master Dataform (nazývaný v Greenu "FreeForm") má pouze jednu jedinou část <see cref="DxDataFormPart"/>, která nezobrazuje ani ColumnHeaders ani RowHeaders ani SummaryRow,
    /// a ani nenabízí rozdělovací Splittery.
    /// DataForm používaný pro položky (nazývaný v Greenu "EditBrowse") toto rozčlenění umožňuje.
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
            InitializeRows();
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
            DisposeData();
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
        private DxDataFormPanel DataPanel { get { return this._DataPanel; } }
        /// <summary>
        /// Vlastník - <see cref="DxDataForm"/>
        /// </summary>
        private DxDataForm DataForm { get { return this._DataPanel.DataForm; } }
        /// <summary>
        /// Vzhled. Autoinicializační property. Nikdy není null. Setování null nastaví defaultní vzhled.
        /// </summary>
        private DxDataFormAppearance DataFormAppearance { get { return DataForm.DataFormAppearance; } }
        /// <summary>
        /// Vlastní data zobrazená v dataformu
        /// </summary>
        private DxDataFormData Data { get { return DataForm.Data; } }
        /// <summary>
        /// Identifikátor this části.
        /// S tímto ID se pak dotazuje parentů (dataformu a jeho dat) na řádky, sloupce atd.
        /// </summary>
        public DxDataFormPartId PartId { get { return _PartId; } }
        /// <summary>
        /// Aktuálně zobrazované grupy, obsahující definice jednotlivých prvků <see cref="DxDataFormItem"/>.
        /// Grupy jsou získány z viditelných řádků, z jejich instancí stránek <see cref="DxDataFormPage"/>.
        /// </summary>
#warning TODO !!!
        private List<DxDataFormGroup> CurrentGroups { get { return null /* this.DataForm.CurrentGroupDefinitions */; } }
        /// <summary>
        /// Aktuální sumární velikost sady grup v pixelech.
        /// Je vypočtena pro aktuální grupy <see cref="CurrentGroups"/> po jejich setování a slouží pro vizuální práci s controly.
        /// </summary>
#warning TODO !!!
        private Size CurrentGroupsSize { get { return Size.Empty /* this.DataForm.CurrentGroupsSize */; } }
        /// <summary>
        /// Aktuální sumární velikost sady grup v pixelech.
        /// Je vypočtena pro aktuální grupy <see cref="CurrentGroups"/> po jejich setování a slouží pro vizuální práci s controly.
        /// </summary>
        private Size CurrentRowsTotalSize { get { return this.DataForm.GetRowsTotalSize(this.PartId); } }
        /// <summary>
        /// Výška jednoho řádku. 
        /// Je vypočtena po vložení definice vzhledu <see cref="CurrentGroups"/> jako největší hodnota Bottom ze všech souřadnic grup.
        /// K výšce je přičtena hodnota mezery mezi řádky.
        /// Zdejší hodnota tedy reprezentuje výšku každého řádku <see cref="DxDataFormRow"/> v aktuálním dataformu/záložce.
        /// </summary>
#warning TODO !!!
        private int CurrentRowHeight { get { return 0  /* this.DataForm.ActivePageRowHeight */ ; } }
        #endregion
        #region ContentPanel: vizuální panel obsahující vlastní data a controly, nachází se uvnitř this (DxScrollableContent) a jeho obsah je řízeně Scrollován
        /// <summary>
        /// Inicializuje panel <see cref="__ContentPanel"/>
        /// </summary>
        private void InitializeContentPanel()
        {
            this.__ContentPanel = new DxPanelBufferedGraphic() { Visible = true, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            this.__ContentPanel.LogActive = this.LogActive;
            this.__ContentPanel.Layers = BufferedLayers;                        // Tady můžu přidat další vrstvy, když budu chtít kreslit 'pod' anebo 'nad' hlavní prvky
            this.__ContentPanel.PaintLayer += _ContentPanel_PaintLayer;         // A tady bych pak musel reagovat na kreslení přidaných vrstev...
            this.ContentControl = this.__ContentPanel;

            /*    TEST ONLY  :
            this.VScrollBarIndicators.AddIndicator(new Int32Range(50, 100), ScrollBarIndicatorType.BigCenter, Color.DarkRed);
            this.VScrollBarIndicators.AddIndicator(new Int32Range(500, 720), ScrollBarIndicatorType.FullSize | ScrollBarIndicatorType.OutsideGradientEffect, Color.DarkRed);
            this.VScrollBarIndicators.AddIndicator(new Int32Range(850, 1200), ScrollBarIndicatorType.ThirdNear, Color.DarkBlue);
            this.VScrollBarIndicators.AddIndicator(new Int32Range(1100, 1500), ScrollBarIndicatorType.HalfFar, Color.DarkGreen);

            for (int i = 20; i < 2000; i += 100)
                this.HScrollBarIndicators.AddIndicator(new Int32Range(i, i + 70), ScrollBarIndicatorType.FullSize | ScrollBarIndicatorType.InnerGradientEffect, Color.Red);
            this.HScrollBarIndicators.ColorAlphaArea = 160;
            this.HScrollBarIndicators.ColorAlphaThumb = 80;
            this.HScrollBarIndicators.Effect3DRatio = 0.75f;
            */
        }
        /// <summary>
        /// Disposuje panel <see cref="__ContentPanel"/>
        /// </summary>
        private void DisposeContentPanel()
        {
            this.ContentControl = null;
            if (this.__ContentPanel != null)
            {
                this.__ContentPanel.PaintLayer -= _ContentPanel_PaintLayer;
                this.__ContentPanel.Dispose();
                this.__ContentPanel = null;
            }
        }
        /// <summary>
        /// Panel, ve kterém se vykresluje i hostuje obsah DataFormu. Panel je <see cref="DxPanelBufferedGraphic"/>, 
        /// ale z hlediska <see cref="DxDataForm"/> nemá žádnou funkcionalitu, ta je soustředěna do <see cref="DxDataFormPanel"/>.
        /// </summary>
        private DxPanelBufferedGraphic __ContentPanel;
        #endregion
        #region Řádky DxDataFormRow, grupy DxDataFormGroup
        /// <summary>
        /// Inicializace pole řádků
        /// </summary>
        private void InitializeRows()
        {
            // Zatím netřeba; řádky se v Part necachují, čteme je z DataFormu ...
        }
        /// <summary>
        /// Obsahuje pole řádků, které jsou (anebo mohou být) zobrazovány v aktuálním <see cref="DxDataFormPart"/> - vyhovují aktuálnímu filtru a mají nastavené třídění. 
        /// Mají správnou hodnotu vizuální pozice a indexu <see cref="DxDataFormRow.VisualIndex"/> a <see cref="DxDataFormRow.TotalYPosition"/>.
        /// Tyto řádky jsou sdíleny všemi sousedními částmi vlevo i vpravo a jsou společně scrollovány, a mají tedy společné souřadnice Y.
        /// <para/>
        /// Jde tedy o všechny řádky, nejen ty aktuálně zobrazené. Z tohoto pole se pak vybírají řádky, které jsou aktuálně zobrazeny: po změně viditelné oblasti se znovu vyhledají a uloží.
        /// Pokud je aktivní řádkový filtr, pak tyto řádky mu vyhovují (nevyhovující zde nejsou).
        /// <para/>
        /// Pokud jsou přítomny části nad a pod (vodorovné splittery), pak ty mohou zobrazovat fyzicky jiné řádky = jinak zafiltrované, jinak setříděné, v jiné vizuální pozici (jinak odscrollované).
        /// </summary>
        private List<DxDataFormRow> _Rows { get { return this.DataForm.GetRows(this.PartId); } }
        /// <summary>
        /// Pole řádků, které jsou aktuálně ve viditelné oblasti. 
        /// Toto pole je udržováno v metodě <see cref="_PrepareVisibleRows()"/>.
        /// </summary>
        private List<DxDataFormRow> _RowsVisible;
        /// <summary>
        /// Připraví souhrn aktuálně viditelných řádků.
        /// Volá se po změně viditelné souřadnice <see cref="DxScrollableContent.ContentVirtualBounds"/> nebo po změně filtru / třídění řádků = z metody 
        /// </summary>
        private void _PrepareVisibleRows()
        {
            Rectangle virtualBounds = this.ContentVirtualBounds;               // Rozměr se vztahuje k celé ploše datové tabulky = všechny řádky od počátku prvního do konce posledního
            Int32Range visibleYPosition = new Int32Range(virtualBounds.Y, virtualBounds.Bottom);
            _RowsVisible = _Rows.GetVisibleItems(r => r.TotalYPosition, visibleYPosition);
        }
        #endregion
        #region 
        /// <summary>
        /// Zobrazované grupy a jejich prvky.
        /// Po vložení této definice neproběhne automaticky refresh controlu, je tedy vhodné následně volat <see cref="Refresh(RefreshParts)"/> 
        /// a předat v parametru požadavek <see cref="RefreshParts.InvalidateControl"/>.
        /// </summary>
#warning TODO !!!
        public List<DxDataFormGroup> Groups { get { return null  /*  this.DataForm.CurrentGroupDefinitions  */ ; } }
        /// <summary>
        /// Inicializuje pole prvků
        /// </summary>
        private void InitializeGroups()
        {
            __VisibleItems = new List<DxDataFormItem>();
        }
        /// <summary>
        /// Invaliduje aktuální rozměry všech grup v this objektu.
        /// Volá se typicky po změně zoomu nebo DPI.
        /// </summary>
        /// <returns></returns>
        private void _InvalidatGroupsCurrentBounds()
        {
            var groups = Groups;
#warning TODO !!!
            groups?.ForEachExec(g => /* g.InvalidateBounds()  */ { });

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
#warning TODO !!!
            this.__VisibleGroups = groups?.Where(g => true    /* g.IsVisibleInVirtualBounds(virtualBounds) */ ).ToList();
            this.__VisibleItems = this.__VisibleGroups?.SelectMany(g => g.Items).Where(i => i.IsVisibleInVirtualBounds(virtualBounds)).ToList();
        }
        /// <summary>
        /// Zahodí všechny položky o grupách a prvcích z this instance
        /// </summary>
        private void DisposeData()
        {
            if (__VisibleGroups != null)
            {
                __VisibleGroups.Clear();
                __VisibleGroups = null;
            }
            if (__VisibleItems != null)
            {
                __VisibleItems.Clear();
                __VisibleItems = null;
            }
        }

        private List<DxDataFormGroup> __VisibleGroups;
        private List<DxDataFormItem> __VisibleItems;
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
        private DxDataFormItem _CurrentFocusedItem;
        #endregion
        #region Myš - Move, Down
        private void InitializeInteractivityMouse()
        {
            this.__CurrentOnMouseItem = null;
            this.__CurrentOnMouseControlSet = null;
            this.__CurrentOnMouseControl = null;
            this.__ContentPanel.MouseLeave += _ContentPanel_MouseLeave;
            this.__ContentPanel.MouseMove += _ContentPanel_MouseMove;
            this.__ContentPanel.MouseDown += _ContentPanel_MouseDown;
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
            if (!this.__ContentPanel.Bounds.Contains(location))
                DetectMouseChangeForPoint(null);
            else
                DetectMouseChangeForPoint(this.__ContentPanel.PointToClient(absoluteLocation));
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
        /// Vyhledá prvek nacházející se pod aktuální souřadnicí myši a zajistí pro prvky <see cref="MouseLeaveItem(bool)"/> a <see cref="MouseEnterItem(DxDataFormItem)"/>.
        /// </summary>
        private void DetectMouseChangeForCurrentPoint()
        {
            Point absoluteLocation = Control.MousePosition;
            Point relativeLocation = __ContentPanel.PointToClient(absoluteLocation);
            DetectMouseChangeForPoint(relativeLocation);
        }
        /// <summary>
        /// Vyhledá prvek nacházející se pod danou souřadnicí myši a zajistí pro prvky <see cref="MouseLeaveItem(bool)"/> a <see cref="MouseEnterItem(DxDataFormItem)"/>.
        /// </summary>
        /// <param name="location">Souřadnice myši relativně k controlu <see cref="__ContentPanel"/> = reálný parent prvků</param>
        private void DetectMouseChangeForPoint(Point? location)
        {
            DxBufferedLayer invalidateLayers = DxBufferedLayer.None;
            DetectMouseChangeGroupForPoint(location, ref invalidateLayers);
            DetectMouseChangeItemForPoint(location, ref invalidateLayers);
            if (invalidateLayers != DxBufferedLayer.None)
                this.__ContentPanel.InvalidateLayers(invalidateLayers);
        }
        /// <summary>
        /// Detekuje aktuální grupu pod danou souřadnicí, detekuje změny (Leave a Enter) a udržuje v proměnné <see cref="__CurrentOnMouseGroup"/> aktuální grupu na dané souřadnici
        /// </summary>
        /// <param name="location"></param>
        /// <param name="invalidateLayers"></param>
        private void DetectMouseChangeGroupForPoint(Point? location, ref DxBufferedLayer invalidateLayers)
        {
            if (__VisibleGroups == null) return;

            DxDataFormGroup oldGroup = __CurrentOnMouseGroup;
            DxDataFormGroup newGroup = null;
            bool oldExists = (oldGroup != null);
            bool newExists = location.HasValue && __VisibleGroups.TryGetLast(i => i.IsVisibleOnPoint(location.Value), out newGroup);

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
            __CurrentOnMouseGroup = group;
        }
        /// <summary>
        /// Je voláno při opuštění myši z aktuální grupy.
        /// </summary>
        private void MouseLeaveGroup()
        {
            __CurrentOnMouseGroup = null;
        }
        /// <summary>
        /// Grupa aktuálně se nacházející pod myší
        /// </summary>
        private DxDataFormGroup __CurrentOnMouseGroup;
        /// <summary>
        /// Detekuje aktuální prvek pod danou souřadnicí, detekuje změny (Leave a Enter) a udržuje v proměnné <see cref="__CurrentOnMouseItem"/> aktuální prvek na dané souřadnici
        /// </summary>
        /// <param name="location"></param>
        /// <param name="invalidateLayers"></param>
        private void DetectMouseChangeItemForPoint(Point? location, ref DxBufferedLayer invalidateLayers)
        {
            if (__VisibleItems == null) return;

            DxDataFormItem oldItem = __CurrentOnMouseItem;
            DxDataFormItem newItem = null;
            bool oldExists = (oldItem != null);
            bool newExists = location.HasValue && __VisibleItems.TryGetLast(i => i.IsVisibleOnPoint(location.Value), out newItem);

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
        private void MouseEnterItem(DxDataFormItem item)
        {
            if (item.VisibleBounds.HasValue)
            {
                __CurrentOnMouseItem = item;
                __CurrentOnMouseControlSet = DataForm.GetControlSet(item);
                __CurrentOnMouseControl = __CurrentOnMouseControlSet.GetControlForMouse(item, this.__ContentPanel);
                if (!__ContentPanel.IsPaintLayersInProgress)
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
            var oldControl = __CurrentOnMouseControl;
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
            __CurrentOnMouseItem = null;
            __CurrentOnMouseControlSet = null;
            __CurrentOnMouseControl = null;
        }
        /// <summary>
        /// Prvek, nacházející se nyní pod myší
        /// </summary>
        private ControlOneInfo _CurrentItemOnMouseItem;
        /// <summary>
        /// Datový prvek, nacházející se nyní pod myší
        /// </summary>
        private DxDataFormItem __CurrentOnMouseItem;
        /// <summary>
        /// Datový set popisující control, nacházející se nyní pod myší
        /// </summary>
        private DxDataFormControlSet __CurrentOnMouseControlSet;
        /// <summary>
        /// Vizuální control, nacházející se nyní pod myší
        /// </summary>
        private System.Windows.Forms.Control __CurrentOnMouseControl;
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
            _RefreshPartVisibleItems |= refreshParts.HasFlag(RefreshParts.ReloadVisibleCells);
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
        private bool _RefreshInProgress { get { return _BitStorage[0]; } set { _BitStorage[0] = value; } }
        /// <summary>
        /// Zajistit přepočet CurrentBounds v prvcích (=provést InvalidateBounds) = provádí se po změně Zoomu a/nebo DPI
        /// </summary>
        private bool _RefreshPartCurrentBounds { get { return _BitStorage[1]; } set { _BitStorage[1] = value; } }
        /// <summary>
        /// Přepočítat celkovou velikost obsahu
        /// </summary>
        private bool _RefreshPartContentTotalSize { get { return _BitStorage[2]; } set { _BitStorage[2] = value; } }
        /// <summary>
        /// Určit aktuálně viditelné prvky
        /// </summary>
        private bool _RefreshPartVisibleItems { get { return _BitStorage[3]; } set { _BitStorage[3] = value; } }
        /// <summary>
        /// Vyřešit souřadnice nativních controlů, nacházejících se v Content panelu
        /// </summary>
        private bool _RefreshPartNativeControlsLocation { get { return _BitStorage[4]; } set { _BitStorage[4] = value; } }
        /// <summary>
        /// Resetovat cache předvykreslených controlů
        /// </summary>
        private bool _RefreshPartCache { get { return _BitStorage[5]; } set { _BitStorage[5] = value; } }
        /// <summary>
        /// Znovuvykreslit grafiku
        /// </summary>
        private bool _RefreshPartInvalidateControl { get { return _BitStorage[6]; } set { _BitStorage[6] = value; } }
        /// <summary>
        /// Explicitně vyvolat i metodu <see cref="Control.Refresh()"/>
        /// </summary>
        private bool _RefreshPartRefreshControl { get { return _BitStorage[7]; } set { _BitStorage[7] = value; } }
        /// <summary>
        /// Úložiště boolean hodnot
        /// </summary>
        private BitStorage32 _BitStorage { get { if (__BitStorage is null) __BitStorage = new BitStorage32(); return __BitStorage; } }
        private BitStorage32 __BitStorage;
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

            ContentTotalSize = this.CurrentRowsTotalSize;
        }
        /// <summary>
        /// Provede akci Refresh, <see cref="RefreshParts.ReloadVisibleCells"/>
        /// </summary>
        private void _DoRefreshPartVisibleItems()
        {
            _RefreshPartVisibleItems = false;

            // Připravím soupis aktuálně viditelných řádků a prvků:
            _PrepareVisibleRows();
#warning TODO !!!
            /*   _PrepareVisibleItems();   */
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
                this.__ContentPanel.InvalidateLayers(layers);
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

            var mouseControl = __CurrentOnMouseControl;
            var mouseItem = __CurrentOnMouseItem;
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
                __VisibleGroups.ForEachExec(g => PaintGroupStandard(g, visibleOrigin, e));
                __VisibleItems.ForEachExec(i => PaintItemStandard(i, visibleOrigin, e));
            }
            finally
            {
                _PaintingItems = false;
            }
            DxComponent.LogAddLineTime(LogActivityKind.Paint, $"DxDataForm Paint Standard() Items: {__VisibleItems?.Count}; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Provede vykreslení jedné dané grupy
        /// </summary>
        /// <param name="group"></param>
        /// <param name="visibleOrigin"></param>
        /// <param name="e"></param>
        private void PaintGroupStandard(DxDataFormGroup group, Point visibleOrigin, DxBufferedGraphicPaintArgs e)
        {
#warning TODO !!!
            /*
            var bounds = group.CurrentGroupBounds;
            Point location = bounds.Location.Sub(visibleOrigin);
            group.VisibleGroupBounds = new Rectangle(location, bounds.Size);
            bool onMouse = Object.ReferenceEquals(group, __CurrentOnMouseGroup);
            group.PaintGroup(e, onMouse, false);
            */
        }
        /// <summary>
        /// Provede vykreslení jednoho daného prvku
        /// </summary>
        /// <param name="item"></param>
        /// <param name="visibleOrigin"></param>
        /// <param name="e"></param>
        private void PaintItemStandard(DxDataFormItem item, Point visibleOrigin, DxBufferedGraphicPaintArgs e)
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

            var mouseControl = __CurrentOnMouseControl;
            var mouseItem = __CurrentOnMouseItem;
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
        private Color? GetIndicatorColor(DxDataFormItem item, out bool isBold)
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
            public DxDataFormItem Item;
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

    #region class DxDataFormRowSet : Sada řádků
    /// <summary>
    /// Sada řádků: v této třídě jsou uloženy datové řádky s aplikovaným řádkovým filtrem a tříděním.
    /// Třída v sobě tedy ukládá jeden seznam řádků (obsahující aktuální podmnožinu ze všech řádků), plus objekt filtru a sorteru.
    /// Navíc řeší výpočet layoutu v ose Y = sekvenční souřadnice řádků.
    /// </summary>
    internal class DxDataFormRowSet : IDisposable
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataForm"></param>
        /// <param name="setId"></param>
        public DxDataFormRowSet(DxDataForm dataForm, int setId)
        {
            this.__DataForm = dataForm;
            this.ReloadRows();
        }
        public void Dispose()
        {
            this.__SetId = -1;
            this.__Rows = null;
            this.__Filter = null;
            this.__Sorter = null;
            this.__DataForm = null;
        }
        /// <summary>
        /// Reference na <see cref="DxDataForm"/>
        /// </summary>
        public DxDataForm DataForm { get { return __DataForm; } }
        private DxDataForm __DataForm;
        /// <summary>
        /// Vlastní data zobrazená v dataformu
        /// </summary>
        private DxDataFormData Data { get { return this.DataForm.Data; } }
        /// <summary>
        /// ID sady řádků
        /// </summary>
        public int SetId { get { return __SetId; } }
        private int __SetId;
        /// <summary>
        /// Sada řádků = všechny z datového zdroje, v jejich původním třídění dle datového zdroje.
        /// </summary>
        public List<DxDataFormRow> Rows { get { return __Rows; } }
        private List<DxDataFormRow> __Rows;
        /// <summary>
        /// Sada dostupných řádků = vyhovující aktuálnímu filtru <see cref="Filter"/>, setříděné sorterem <see cref="Sorter"/>.
        /// </summary>
        public List<DxDataFormRow> RowsVisible { get { return __RowsVisible; } }
        private List<DxDataFormRow> __RowsVisible;
        /// <summary>
        /// Velikost prostoru všech řádků = na výšku i na šířku
        /// </summary>
        public Size RowsTotalSize { get { return __RowsTotalSize; } }
        private Size __RowsTotalSize;
        /// <summary>
        /// Aktuální sumární velikost sady grup v pixelech.
        /// Je vypočtena pro aktuální grupy <see cref="CurrentGroupDefinitions"/> po jejich setování a slouží pro vizuální práci s controly.
        /// </summary>
#warning TODO !!!        
        public Size CurrentGroupsSize { get { return Size.Empty   /*  this.DataForm.CurrentGroupsSize  */ ; } }
        /// <summary>
        /// Výška jednoho řádku. 
        /// Je vypočtena po vložení definice vzhledu <see cref="CurrentGroupDefinitions"/> jako největší hodnota Bottom ze všech souřadnic grup.
        /// K výšce je přičtena hodnota mezery mezi řádky.
        /// Zdejší hodnota tedy reprezentuje výšku každého řádku <see cref="DxDataFormRow"/> v aktuálním dataformu/záložce.
        /// </summary>
#warning TODO !!!        
        public int CurrentRowHeight { get { return 0  /*   this.DataForm.ActivePageRowHeight  */; } }
        /// <summary>
        /// Filtr řádků.
        /// Setování filtru provede kompletní přepočet vizuálních dat řádků: filtrace, třídění, vizuální pozice:
        /// <see cref="DxDataFormRow.IsVisibleFilter"/>; <see cref="DxDataFormRow.VisualIndex"/>; <see cref="DxDataFormRow.TotalYPosition"/>; 
        /// </summary>
        public Func<DxDataFormRow, bool> Filter
        {
            get { return __Filter; }
            set
            {
                __Filter = value;
                _FilterRows();
                _SortRows();
                _SetVisualPositions();
                _RecalcSize();
            }
        }
        private Func<DxDataFormRow, bool> __Filter;
        /// <summary>
        /// Třídění řádků.
        /// Setování třídění provede kompletní přepočet vizuálních dat řádků: třídění, vizuální pozice:
        /// <see cref="DxDataFormRow.IsVisibleFilter"/>; <see cref="DxDataFormRow.VisualIndex"/>; <see cref="DxDataFormRow.TotalYPosition"/>; 
        /// </summary>
        public Func<DxDataFormRow, DxDataFormRow, int> Sorter
        {
            get { return __Sorter; }
            set
            {
                __Sorter = value;
                _SortRows();
                _SetVisualPositions();
            }
        }
        private Func<DxDataFormRow, DxDataFormRow, int> __Sorter;
        /// <summary>
        /// Přenačte všechna aktuální data z datového zdroje, aplikuje filtr a třídění a přepočte vizuální hodnoty řádků.
        /// </summary>
        public void ReloadRows()
        {
            _ReloadRows();
            _FilterRows();
            _SortRows();
            _SetVisualPositions();
            _RecalcSize();
        }
        /// <summary>
        /// Přepočte souřadnice Y řádků a následně i celkovou velikost celé kolekce řádků
        /// </summary>
        public void ReloadSize()
        {
            _SetVisualPositions();
            _RecalcSize();
        }
        /// <summary>
        /// Načte do sebe řádky z datového zdroje.
        /// Načítají se všechny. Přitom se z nich vytváří instance <see cref="DxDataFormRow"/>.
        /// Zde se neřeší <see cref="DxDataFormRow.IsVisibleFilter"/> ani <see cref="Sorter"/>, ani layout.
        /// </summary>
        private void _ReloadRows()
        {
            var dataForm = this.DataForm;
            var setId = this.SetId;
            int rowIndex = 0;
            __Rows = this.Data.RowsId.Select(rowId => new DxDataFormRow(dataForm, setId, rowId, rowIndex++)).ToList();
        }
        /// <summary>
        /// Metoda do všech řádků v <see cref="__Rows"/> nastaví jejich viditelnost <see cref="DxDataFormRow.IsVisibleFilter"/> podle aktuálního filtru <see cref="Filter"/>,
        /// a řádky které jsou viditelné vloží do pole <see cref="__RowsVisible"/>.
        /// </summary>
        private void _FilterRows()
        {
            var filter = this.Filter;
            if (filter != null)
                __Rows.ForEachExec(row => { row.IsVisibleFilter = filter(row); });
            else
                __Rows.ForEachExec(row => { row.IsVisibleFilter = true; });
            __RowsVisible = __Rows.Where(row => row.IsVisibleFilter).ToList();
        }
        /// <summary>
        /// Metoda setřídí řádky v poli <see cref="__RowsVisible"/> pomocí třídiče <see cref="Sorter"/>.
        /// Pokud není dodán, pak je setřídí podle nativního pořadí ze zdroje dat.
        /// </summary>
        private void _SortRows()
        {
            var sorter = this.Sorter;
            if (sorter != null)
                __RowsVisible.Sort((a, b) => sorter(a, b));
            else
                __RowsVisible.Sort((a, b) => a.RowIndex.CompareTo(b.RowIndex));
        }
        /// <summary>
        /// Metoda do všech řádků v poli <see cref="__RowsVisible"/> nastaví jejich vizuální souřadnice v ose Y tak aby sekvenčně navazovaly.
        /// </summary>
        private void _SetVisualPositions()
        {
            DxDataFormRow.SetVisualPositions(__RowsVisible);
        }

        private void _RecalcSize()
        {
            var totalWidth = this.CurrentGroupsSize.Width;
            var rows = this.__RowsVisible;
            int totalHeight = (rows.Count > 0 ? rows[rows.Count - 1].TotalYPosition.End : 0);
            __RowsTotalSize = new Size(totalWidth, totalHeight);
        }
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
        /// <param name="dataForm">Majitel</param>
        /// <param name="partY">Oblast řádků</param>
        /// <param name="rowId">ID řádku</param>
        /// <param name="rowIndex">Index řádku ve zdroji</param>
        public DxDataFormRow(DxDataForm dataForm, int partY, int rowId, int rowIndex)
        {
            __DataForm = dataForm;
            __RowType = DxDataFormRowType.RowData;
            __RowId = rowId;
            __RowIndex = rowIndex;
            __TotalYPosition = new Int32Range();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataForm">Majitel</param>
        /// <param name="partY">Oblast řádků</param>
        /// <param name="rowType">Typ řádku</param>
        public DxDataFormRow(DxDataForm dataForm, int partY, DxDataFormRowType rowType)
        {
            __DataForm = dataForm;
            __RowType = rowType;
            __RowId = -1;
            __TotalYPosition = new Int32Range();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            __DataForm = null;
            __RowType = DxDataFormRowType.None;
            __RowId = -1;
            __TotalYPosition = null;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"RowType: {__RowType}; RowIndex: {RowIndex}; RowId: {__RowId}; VisualPositions: {TotalYPosition}";
        }
        /// <summary>Vlastník - <see cref="DxDataFormPart"/></summary>
        private DxDataForm __DataForm;
        /// <summary>Typ řádku</summary>
        private DxDataFormRowType __RowType;
        /// <summary>ID řádku, odkazuje se do <see cref="DxDataForm"/> pro data</summary>
        private int __RowId;
        /// <summary>Pořadový index řádku, počínaje 0</summary>
        private int __RowIndex;
        /// <summary>Vizuální index řádku: 0 a kladné pro viditelné řádky, -1 pro neviditelné</summary>
        private int __VisualIndex;
        /// <summary>
        /// Vlastník - <see cref="DxDataForm"/>
        /// </summary>
        public DxDataForm DataForm { get { return this.__DataForm; } }
        /// <summary>
        /// Typ řádku
        /// </summary>
        public DxDataFormRowType RowType { get { return this.__RowType; } }
        /// <summary>
        /// Aktuální výška tohoto řádku. Výchozí stav je, že řádek přebírá tuto výšku z <see cref="DxDataForm.ActivePageRowHeight"/>.
        /// Hodnotu lze setovat: pokud je setována not null, pak bude akceptována; pokud je setována null, pak se začne opět přebírat z <see cref="DxDataForm.ActivePageRowHeight"/>.
        /// Hodnota musí obsahovat i výšku oddělovače mezi řádky.
        /// </summary>
        public int CurrentRowHeight
        {
#warning TODO !!!        
            get { return (__CurrentRowHeight ?? 0  /*  this.DataForm.ActivePageRowHeight   */ ); }
            set { __CurrentRowHeight = value; }
        }
        private int? __CurrentRowHeight;
        /// <summary>
        /// ID řádku, odkazuje se do <see cref="DxDataForm"/> pro data
        /// </summary>
        public int RowId { get { return this.__RowId; } }
        /// <summary>
        /// Pořadový index řádku v datovém zdroji, počínaje 0.
        /// </summary>
        public int RowIndex { get { return this.__RowIndex; } }
        /// <summary>
        /// Prvek je viditelný vlivem filtru?
        /// Lze setovat.
        /// Pokud je false, pak hodnota <see cref="VisualIndex"/> je -1 a <see cref="TotalYPosition"/> je null!
        /// </summary>
        public bool IsVisibleFilter { get; set; }
        /// <summary>
        /// Vizuální index řádku při zobrazení podle aktuálního filtru a třídění, počínaje 0.
        /// V tomto pořadí jsou řádky zobrazeny.
        /// V poli řádků musí být tato hodnota kontinuální a vzestupná, počínaje 0.
        /// Datové řádky s hodnotou -1 (a jinou zápornou) nebudou zobrazeny = nevyhovují filtru.
        /// </summary>
        public int VisualIndex { get { return (IsVisibleFilter ? this.__VisualIndex : -1); } }
        /// <summary>
        /// Umístění na ose Y; vizuální pixely v rámci celého Bandu - u typu řádku <see cref="DxDataFormRowType.RowHeader"/>,
        /// <see cref="DxDataFormRowType.RowFilter"/> a <see cref="DxDataFormRowType.RowFooter"/> jsou permanentní (tyto řádky jsou nepohyblivé při pohybu ScrollBaru),
        /// u řádků typu řádku <see cref="DxDataFormRowType.RowData"/> a <see cref="DxDataFormRowType.RowSummary"/> jsou pohyblivé podle ScrollBaru.
        /// <para/>
        /// Úplně první řádek má tedy souřadnici Y = 0, úplně poslední řádek může mít souřadnici Y třeba 150 000 pixelů = hluboko dole. V tomto rámci se scrolluje.
        /// <para/>
        /// Řádek který nevyhovuje filtru (má <see cref="IsVisibleFilter"/> = false) zde má null! 
        /// S takovým řádkem se nemá vizuálně počítat.
        /// </summary>
        public Int32Range TotalYPosition { get { return (IsVisibleFilter ? __TotalYPosition : null); } }
        private Int32Range __TotalYPosition;
        /// <summary>
        /// Řádek se aktuálně nachází ve viditelné oblasti = je z něj zobrazen přinejmenším jeden pixel?
        /// </summary>
        public bool IsInVisibleArea { get { return __IsInVisibleArea; } set { __IsInVisibleArea = value; } }
        private bool __IsInVisibleArea;
        /// <summary>
        /// Umístění na ose Y; reálné pixely v rámci celého Bandu; nastavuje se pouze pro řádky 
        /// typu <see cref="DxDataFormRowType.RowData"/> a <see cref="DxDataFormRowType.RowSummary"/> = ty jsou pohyblivé podle ScrollBaru.
        /// 
        /// </summary>
        public Int32Range CurrentPositions { get { return __CurrentPositions; } }
        private Int32Range __CurrentPositions;

        #endregion
        /// <summary>
        /// Metoda do všech řádků dané kolekce naplní jejich hodnoty <see cref="VisualIndex"/> a <see cref="TotalYPosition"/>,
        /// podle hodnoty <see cref="IsVisibleFilter"/> a <see cref="CurrentRowHeight"/>.
        /// </summary>
        /// <param name="rows"></param>
        internal static void SetVisualPositions(IEnumerable<DxDataFormRow> rows)
        {
            if (rows is null) return;
            int visualIndex = 0;
            int visualBegin = 0;
            foreach (var row in rows)
            {
                if (row.IsVisibleFilter)
                {
                    row.__VisualIndex = visualIndex++;
                    int visualHeight = row.CurrentRowHeight;
                    int visualEnd = visualBegin + visualHeight;
                    row.__TotalYPosition.Store(visualBegin, visualEnd);
                    visualBegin = visualEnd;
                }
                else
                {
                    row.__VisualIndex = -1;
                }
            }
        }
        /// <summary>
        /// Metoda do všech řádků dané kolekce počínaje daným indexem naplní jejich hodnoty <see cref="VisualIndex"/> a <see cref="TotalYPosition"/>,
        /// podle hodnoty <see cref="IsVisibleFilter"/> a <see cref="CurrentRowHeight"/>.
        /// Používá se tehdy, když daný řádek změnil svoji výšku.
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="fromIndex"></param>
        internal static void SetVisualPositions(IList<DxDataFormRow> rows, int fromIndex)
        {
            if (rows is null) return;
            int count = rows.Count;
            if (count < 0) return;
            if (fromIndex >= count) return;
            if (fromIndex < 0) fromIndex = 0;
            var fromRow = rows[fromIndex];
            int visualIndex = fromRow.VisualIndex;
            int visualBegin = fromRow.TotalYPosition.Begin;
            for (int i = fromIndex; i < count; i++)
            {
                var row = rows[i];
                if (row.IsVisibleFilter)
                {
                    row.__VisualIndex = visualIndex++;
                    int visualHeight = row.CurrentRowHeight;
                    int visualEnd = visualBegin + visualHeight;
                    row.__TotalYPosition.Store(visualBegin, visualEnd);
                    visualBegin = visualEnd;
                }
                else
                {
                    row.__VisualIndex = -1;
                }
            }
        }




#warning TODO !!!        
        public List<DxDataFormGroup> Groups { get { return null  /*  this.__DataForm.CurrentGroupDefinitions  */ ; } }


        /// <summary>
        /// Zahodí a uvolní buňky
        /// </summary>
        private void DisposeCells()
        { }


    }
    #endregion


    #region class DxLayoutItem : bázová třída řešící svoje souřadnice v prostoru svého parenta
    /// <summary>
    /// Bázová třída pro třídy, které reprezentují nějaký layout = mají svoji vlastní souřadnici designovou, z ní odvozenou
    /// </summary>
    internal class DxLayoutItem : IDxLayoutItem
    {
        /// <summary>
        /// Designová pozice prvku v rámci parenta.
        /// Hodnota je v designových pixelech = 100% Zoom a 96 DPI.
        /// <para/>
        /// Po setování této property je vhodné zavolat (na nejvyšší úrovni) metodu <see cref="ResetCurrentLayout"/>.
        /// Neprovádí se automaticky, aby v procesu inicializace neprobíhala příliš často.
        /// </summary>
        public virtual Point? DesignLocation
        {
            get { return __DesignLocation; }
            set
            {
                __DesignLocation = value;
                __CurrentLocalPoint = null;
                __CurrentAbsolutePoint = null;
            }
        }
        private Point? __DesignLocation;
        /// <summary>
        /// Designová velikost prvku.
        /// Hodnota je v designových pixelech = 100% Zoom a 96 DPI.
        /// <para/>
        /// Po setování této property je vhodné zavolat (na nejvyšší úrovni) metodu <see cref="ResetCurrentLayout"/>.
        /// Neprovádí se automaticky, aby v procesu inicializace neprobíhala příliš často.
        /// </summary>
        public virtual Size? DesignSize
        {
            get { return __DesignSize; }
            set
            {
                __DesignSize = value;
                __CurrentSize = null;
            }
        }
        private Size? __DesignSize;
        /// <summary>
        /// Instance parenta, v němž jsme umístěni. Jeho souřadnice <see cref="CurrentAbsolutePoint"/> je základnou pro naši souřadnici <see cref="CurrentAbsolutePoint"/>.
        /// <para/>
        /// Po setování této property je vhodné zavolat (na nejvyšší úrovni) metodu <see cref="ResetAbsolutePoint"/>.
        /// Neprovádí se automaticky, aby v procesu inicializace neprobíhala příliš často.
        /// </summary>
        public virtual IDxLayoutItem LayoutParent
        {
            get { return __LayoutParent; }
            set
            {
                __LayoutParent = value;
                __CurrentAbsolutePoint = null;
            }
        }
        private IDxLayoutItem __LayoutParent;
        /// <summary>
        /// Aktuální hodnota DPI z controlu, na němž je layout umístěn.
        /// <para/>
        /// Bázová třída v této property vrací DPI ze svého parenta, a pokud jej nemá, pak vrací výchozí designovou hodnotu <see cref="DxComponent.DesignDpi"/>.
        /// Root instance layoutu tedy má tuto property overridovat a vrátit hodnotu ze svého controlu, aby se vracela reálná hodnota!
        /// Ne-root třídy mohou nechat defaultní chování.
        /// </summary>
        protected virtual int CurrentDPI { get { return LayoutParent?.CurrentDPI ?? DxComponent.DesignDpi; } }
        int IDxLayoutItem.CurrentDPI { get { return this.CurrentDPI; } }
        /// <summary>
        /// Souřadnice, kde tento prvek má svůj vnější počátek v rámci svého parenta = lokální souřadice.
        /// Hodnota je v aktuálních pixelech (nikoli designové pixely) = je už přepočtena Zoomem a DPI.
        /// <para/>
        /// Tuto hodnotu do prvku může vkládat jeho Parent, pokud ten si rozmísťuje svoje Child prvky,
        /// nebo ji může setovat prvek sám, pokud má danou fixní pozici.
        /// </summary>
        public virtual Point CurrentLocalPoint
        {
            get
            {
                if (!__CurrentLocalPoint.HasValue) RecalcCurrentBounds();
                return __CurrentLocalPoint.Value;
            }
            set
            {
                __CurrentLocalPoint = value;
                __CurrentAbsolutePoint = null;
            }
        }
        /// <summary>
        /// Cachovaná hodnota <see cref="CurrentLocalPoint"/>. 
        /// Po inicializaci a po resetu <see cref="ResetCurrentLayout"/> je null, nejbližší čtení <see cref="CurrentLocalPoint"/> tuto hodnotu vypočte z <see cref="DesignLocation"/>.
        /// </summary>
        private Point? __CurrentLocalPoint;
        /// <summary>
        /// Aktuální velikost prvku.
        /// Hodnota je v aktuálních pixelech (nikoli designové pixely) = přepočteno Zoomem a DPI.
        /// </summary>
        public virtual Size CurrentSize
        {
            get
            {
                if (!__CurrentSize.HasValue) RecalcCurrentBounds();
                return __CurrentSize.Value;
            }
            set
            {
                __CurrentSize = value;
            }
        }
        /// <summary>
        /// Cachovaná hodnota <see cref="CurrentSize"/>. 
        /// Po inicializaci a po resetu <see cref="ResetCurrentLayout"/> je null, nejbližší čtení <see cref="CurrentSize"/> tuto hodnotu vypočte z <see cref="DesignSize"/>.
        /// </summary>
        private Size? __CurrentSize;
        /// <summary>
        /// Viditelnost prvku, default = true
        /// </summary>
        public virtual bool IsVisible { get { return __IsVisible; } set { __IsVisible = value; } }
        private bool __IsVisible = true;
        /// <summary>
        /// Přepočte souřadnice <see cref="CurrentLocalPoint"/> a <see cref="CurrentSize"/> z hodnot <see cref="DesignLocation"/> a <see cref="DesignSize"/>.
        /// Počítá je najednou, hlavně kvůli korektnímu zarovnání souřadnice Right a Bottom (aby Zoom nerozhazoval toto zarovnání) (a taky kvůli výkonu).
        /// <para/>
        /// Je přípustné, aby <see cref="DesignLocation"/> a/nebo <see cref="DesignSize"/> bylo null, 
        /// pak odpovídající hodnota <see cref="CurrentLocalPoint"/> a/nebo <see cref="CurrentSize"/> bude Empty.<br/>
        /// Nicméně se očekává, že u takových potomků si tento potomek může svoji pozici / velikost řídit i jinak.
        /// </summary>
        protected virtual void RecalcCurrentBounds()
        {
            var designLocation = DesignLocation;
            var designSize = DesignSize;
            bool hasPoint = designLocation.HasValue;
            bool hasSize = designSize.HasValue;
            if (hasPoint && hasSize)
            {
                var currentBounds = DxComponent.ZoomToGui(new Rectangle(designLocation.Value, designSize.Value), CurrentDPI);
                __CurrentLocalPoint = currentBounds.Location;
                __CurrentSize = currentBounds.Size;
            }
            else if (hasPoint && !hasSize)
            {
                __CurrentLocalPoint = DxComponent.ZoomToGui(designLocation.Value, CurrentDPI);
                __CurrentSize = Size.Empty;
            }
            else if (!hasPoint && hasSize)
            {
                __CurrentLocalPoint = Point.Empty; ;
                __CurrentSize = DxComponent.ZoomToGui(designSize.Value, CurrentDPI);
            }
        }
        /// <summary>
        /// Souřadnice, kde tento prvek má svůj vnější počátek v absolutních pixelech = v rámci nejvyššího hostitele, počínaje <see cref="LayoutParent"/>.
        /// Hodnota je v aktuálních pixelech (nikoli designové pixely) = přepočteno Zoomem a DPI.
        /// </summary>
        public virtual Point CurrentAbsolutePoint
        {
            get
            {
                if (!__CurrentAbsolutePoint.HasValue) RecalcAbsolutePoint();
                return __CurrentAbsolutePoint.Value;
            }
        }
        /// <summary>
        /// Cachovaná hodnota <see cref="CurrentAbsolutePoint"/>.
        /// Po inicializaci a po <see cref="ResetAbsolutePoint"/> je null, nejbližší čtení <see cref="CurrentAbsolutePoint"/> ji znovu dopočte.
        /// </summary>
        private Point? __CurrentAbsolutePoint;
        /// <summary>
        /// Přepočte souřadnici <see cref="CurrentAbsolutePoint"/> ze své souřadnice <see cref="CurrentLocalPoint"/> 
        /// a z parenta <see cref="LayoutParent"/> (pokud jej má) = z jeho souřadnice <see cref="CurrentAbsolutePoint"/>.
        /// </summary>
        protected virtual void RecalcAbsolutePoint()
        {
            var localPoint = this.CurrentLocalPoint;
            var parent = this.LayoutParent;
            __CurrentAbsolutePoint = (parent != null ? new Point(parent.CurrentAbsolutePoint.X + localPoint.X, parent.CurrentAbsolutePoint.Y + localPoint.Y) : localPoint);
        }
        /// <summary>
        /// Provede reset souřadnic tohoto prvku <see cref="DxLayoutItem.CurrentAbsolutePoint"/> = provádí se vždy po změně pozice prvku = po jeho přemístění.
        /// Současně se provede stejný reset všech zdejších prvků <see cref="DxLayoutItem.LayoutChilds"/>.
        /// </summary>
        public virtual void ResetAbsolutePoint()
        {
            __CurrentAbsolutePoint = null;
            LayoutChilds?.ForEach(c => c.ResetAbsolutePoint());
        }
        /// <summary>
        /// Souřadnice tohoto prvku v absolutních pixelech = v rámci nejvyššího hostitele, počínaje <see cref="LayoutParent"/>.
        /// Hodnota je v aktuálních pixelech (nikoli designové pixely) = přepočteno Zoomem a DPI.
        /// </summary>
        public virtual Rectangle CurrentAbsoluteBounds { get { return new Rectangle(CurrentAbsolutePoint, CurrentSize); } }
        /// <summary>
        /// Metoda vrátí absolutní pozici (v rámci Controlu) pro dodanou souřadnici typu Current = relativní k this instanci.
        /// Pokud tedy this instance má někde ve svém prostoru vykreslit (nebo umístit) cokoliv na svoji Current souřadnici { 10, 5, 100, 20 }, pak bod { 10, 5 } se vztahuje k jejímu počátku.
        /// Prvek jako takový je ale umístěn v rámci hierarchie Parentů na konkrétní absolutní souřadnici v Root controlu, 
        /// a tato absolutní souřadnice <see cref="CurrentAbsolutePoint"/> = např { 240, 380 }, pak zdejší metoda vrátí dodanou souřadnici posunutou o aktuální <see cref="CurrentAbsolutePoint"/>,
        /// výstupem tedy bude { (240+10), (380+5), 100,20 }.
        /// </summary>
        /// <param name="currentBounds"></param>
        /// <returns></returns>
        protected virtual Rectangle GetAbsoluteBounds(Rectangle currentBounds)
        {
            var origin = this.CurrentAbsolutePoint;
            return currentBounds.Add(origin);
        }
        /// <summary>
        /// Provede reset souřadnic tohoto prvku <see cref="DxLayoutItem.CurrentLocalPoint"/> a <see cref="DxLayoutItem.CurrentSize"/> = provádí se vždy po změně Zoomu anebo DPI.
        /// Současně se provede stejný reset všech zdejších prvků <see cref="DxLayoutItem.LayoutChilds"/>.
        /// </summary>
        public virtual void ResetCurrentLayout()
        {
            __CurrentLocalPoint = null;
            __CurrentSize = null;
            LayoutChilds?.ForEach(c => c.ResetCurrentLayout());
        }
        /// <summary>
        /// Metoda zajistí, že pokud aktuálně není zdejší layout platný (tj. hodnota <see cref="DxLayoutItem.IsValidLayout"/> je false), pak bude provedena metoda <see cref="DxLayoutItem.RecalculateLayout(bool)"/>.
        /// Tím se <see cref="DxLayoutItem.IsValidLayout"/> nastaví na true.
        /// Layout lze invalidovat metodou 
        /// </summary>
        /// <param name="force"></param>
        public virtual void CheckValidLayout(bool force = false)
        {
            if (force || !IsValidLayout)
                RecalculateLayout(true);
        }
        /// <summary>
        /// Invaliduje zdejší layout. Bude následně vypočten znovu.
        /// </summary>
        public virtual void InvalidateLayout()
        {
            __IsValidLayout = false;
            __CurrentAbsolutePoint = null;
            this.LayoutChilds?.ForEach(c => c.InvalidateLayout());
        }
        /// <summary>
        /// Aktuální layout tohoto prvku je platný?
        /// Potomek může setovat. Má setovat true v metodě <see cref="DxLayoutItem.RecalculateLayout(bool)"/>, pokud nevolá base metodu.
        /// </summary>
        public bool IsValidLayout { get { return __IsValidLayout; } protected set { __IsValidLayout = value; } }
        private bool __IsValidLayout;
        /// <summary>
        /// Metoda zajistí přepočet layoutu (rozložení prvků) v Child prvcích = vyvolá v jejich instanci tuto metodu (předá jim hodnotu parametru <paramref name="callHandler"/> = false), 
        /// a následně vyvolá v this prvku metodu <see cref="DxLayoutItem.RecalculateCurrentLayout(bool)"/>.
        /// Budou určeny aktuální souřadnice.
        /// Pokud je na vstupu <paramref name="callHandler"/> = true a tento prvek změní svoji velikost, pak vyvolá svoji metodu <see cref="DxLayoutItem.OnRecalculateSizeChanged()"/>.
        /// <para/>
        /// Metoda je volána zvenku při požadavku na přepočet layoutu = uspořádání vnitřních prvků (jejich viditelnost, velikost, pozice) jednak pro Child prvky, a následně i pro this prvek.
        /// Metoda a určí celkovou velikost this prvku, pokud je dynamicky závislý na svém obsahu.
        /// Tato metoda (nebo zdejší potomci) tedy může reagovat na aktuální hodnoty v datech a pro některé svoje prvky může nastavit Invisible, a dynamicky změnit svou velikost.
        /// Pokud změní this prvek svoji sumární velikost, pak tedy vyvolá svoji metodu <see cref="DxLayoutItem.OnRecalculateSizeChanged()"/>.
        /// <para/>
        /// Bázová třída <see cref="DxLayoutItem"/> v této metodě volá přepočet do Child prvků <see cref="LayoutChilds"/> : <see cref="IDxLayoutItem.RecalculateLayout(bool)"/>;<br/>
        /// a pak vyvolá zdejší <see cref="DxLayoutItem.RecalculateCurrentLayout(bool)"/>;<br/>
        /// poté nastaví this . <see cref="DxLayoutItem.IsValidLayout"/> = true;<br/>
        /// a skončí.
        /// <para/>
        /// Pokud potomek řeší layout jinak, nemusí volat bázovou metodu, ale musí nastavit <see cref="DxLayoutItem.IsValidLayout"/> = true;
        /// </summary>
        /// <param name="callHandler">Požadavek aby po změně velikosti byl volán handler <see cref="DxLayoutItem.OnRecalculateSizeChanged"/></param>
        public virtual void RecalculateLayout(bool callHandler)
        {
            this.LayoutChilds?.ForEach(c => c.RecalculateLayout(false));
            RecalculateCurrentLayout(callHandler);
            __IsValidLayout = true;
        }
        /// <summary>
        /// Prvek má přepočítat svůj layout, poté kdy proběhl přepočet layoutu jeho Child prvků.
        /// Tato metoda je volána z metody <see cref="DxLayoutItem.RecalculateLayout(bool)"/> po přepočtech Child prvků.
        /// <para/>
        /// Bázová třída <see cref="DxLayoutItem"/> v této metodě nic nedělá.
        /// </summary>
        /// <param name="callHandler">Požadavek aby po změně velikosti byl volán handler <see cref="DxLayoutItem.OnRecalculateSizeChanged"/></param>
        protected virtual void RecalculateCurrentLayout(bool callHandler) { }
        /// <summary>
        /// Je voláno poté, kdy prvek v rámci rekalkulace layoutu změnil svoji velikost.
        /// </summary>
        protected virtual void OnRecalculateSizeChanged() { }
        /// <summary>
        /// Moje Childs. Tyto prvky jsou resetovány jako závislé vždy po resetu this instance, a jsou volány jejich rekalkulace v rámci rekalkulace this prvku.
        /// Může být null.
        /// </summary>
        protected virtual IEnumerable<IDxLayoutItem> LayoutChilds { get { return null; } }
    }
    /// <summary>
    /// Předpis pro prvky layoutu
    /// </summary>
    internal interface IDxLayoutItem
    {
        /// <summary>
        /// Instance parenta, v němž jsme umístěni. Jeho souřadnice <see cref="CurrentAbsolutePoint"/> je základnou pro naši souřadnici <see cref="CurrentAbsolutePoint"/>.
        /// <para/>
        /// Po setování této property je vhodné zavolat (na nejvyšší úrovni) metodu <see cref="ResetAbsolutePoint"/>.
        /// Neprovádí se automaticky, aby v procesu inicializace neprobíhala příliš často.
        /// </summary>
        IDxLayoutItem LayoutParent { get; }
        /// <summary>
        /// Aktuální hodnota DPI z controlu, na němž je layout umístěn.
        /// </summary>
        int CurrentDPI { get; }
        /// <summary>
        /// Souřadnice, kde tento prvek má svůj vnější počátek v absolutních pixelech = v rámci nejvyššího hostitele, počínaje <see cref="LayoutParent"/>.
        /// Hodnota je v aktuálních pixelech (nikoli designové pixely) = přepočteno Zoomem a DPI.
        /// </summary>
        Point CurrentAbsolutePoint { get; }
        /// <summary>
        /// Provede reset souřadnic tohoto prvku <see cref="CurrentAbsolutePoint"/> = provádí se vždy po změně pozice prvku = po jeho přemístění.
        /// Současně se provede stejný reset všech zdejších prvků Childs, pokud je má.
        /// </summary>
        void ResetAbsolutePoint();
        /// <summary>
        /// Provede reset lokálních souřadnic tohoto prvku = provádí se vždy po změně Zoomu anebo DPI.
        /// Současně se provede stejný reset všech zdejších prvků Childs, pokud je má.
        /// </summary>
        void ResetCurrentLayout();
        /// <summary>
        /// Invaliduje zdejší layout. Bude následně vypočten znovu.
        /// </summary>
        void InvalidateLayout();
        /// <summary>
        /// Metoda vyvolá tuto metodu pro všechny svoje Childs a poté nastaví platnost svého layoutu na true.
        /// </summary>
        /// <param name="callHandler">Požadavek aby po změně velikosti byl volán handler <see cref="DxLayoutItem.OnRecalculateSizeChanged"/></param>
        void RecalculateLayout(bool callHandler);
    }
    #endregion

    // ???

    #region class DxDataFormCell : Jedna vizuální buňka v rámci DataFormu, průnik řádku a sloupce
    /// <summary>
    /// <see cref="DxDataFormCell"/> : Jedna vizuální buňka v rámci DataFormu, průnik řádku a sloupce.
    /// Musí být v maximální míře lehká: rychle vytvořitelná, s minimální spotřebou paměti.
    /// </summary>
    internal class DxDataFormCell
    {

    }
    #endregion

    // Jednotlivé Controly
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
            __DataForm = dataForm;
            __ItemType = itemType;
            __UseControlForDraw = true;
            __UseControlForMouse = true;
            __UseControlForFocus = true;

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
                    __UseControlForDraw = false;
                    __UseControlForMouse = false;
                    __UseControlForFocus = false;

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
            DisposeControl(ref __ControlDraw, DxDataFormControlUseMode.Draw);
            DisposeControl(ref __ControlMouse, DxDataFormControlUseMode.Mouse);
            DisposeControl(ref __ControlFocus, DxDataFormControlUseMode.Focus);

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
        private DxDataForm __DataForm;
        private DataFormColumnType __ItemType;
        private Func<Control> _CreateControlFunction;
        private Func<DxDataFormItem, string> _GetKeyFunction;
        private Action<DxDataFormItem, Control, DxDataFormControlUseMode> _FillControlAction;
        private Action<DxDataFormItem, Control> _ReadControlAction;
        private bool _Disposed;
        #endregion
        #region Label
        private Control _LabelCreate() { return new DxLabelControl() { AutoSizeMode = LabelAutoSizeMode.None }; }
        private string _LabelGetKey(DxDataFormItem item)
        {
            string key = GetStandardKeyForItem(item);
            return key;
        }
        private void _LabelFill(DxDataFormItem item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxLabelControl label)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxLabelControl).Name}.");
            CommonFill(item, label, mode, _LabelFillNext);
        }
        private void _LabelFillNext(DxDataFormItem item, DxLabelControl label, DxDataFormControlUseMode mode)
        {
            //label.LineStyle = System.Drawing.Drawing2D.DashStyle.Solid;
            //label.LineOrientation = LabelLineOrientation.Horizontal;
            //label.LineColor = Color.Violet;
            //label.LineVisible = true;
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
        private void _TextBoxFill(DxDataFormItem item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxTextEdit textEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxTextEdit).Name}.");
            CommonFill(item, textEdit, mode);
            textEdit.DeselectAll();
            textEdit.SelectionStart = 0;
        }
        private void _TextBoxRead(DxDataFormItem item, Control control)
        { }
        #endregion
        #region TextBoxButton
        private Control _TextBoxButtonCreate() { return new DxButtonEdit(); }
        private string _TextBoxButtonGetKey(DxDataFormItem item)
        {
            string key = GetStandardKeyForItem(item, _TextBoxButtonGetKeySpec);
            return key;
        }
        private string _TextBoxButtonGetKeySpec(DxDataFormItem item)
        {
            if (!item.TryGetIItem<IDataFormColumnTextBoxButton>(out var iItem)) return "";
            string key =
                (iItem.ButtonsVisibleAllways ? "A" : "a") +
                (iItem.ButtonAs3D ? "D" : "F") +
                (((int)iItem.ButtonKind) + 20).ToString();
            return key;
        }
        private void _TextBoxButtonFill(DxDataFormItem item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxButtonEdit buttonEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxButtonEdit).Name}.");
            CommonFill(item, buttonEdit, mode, _TextBoxButtonFillSpec);
            //  textEdit.DeselectAll();
            buttonEdit.SelectionStart = 0;
        }
        private void _TextBoxButtonFillSpec(DxDataFormItem item, DxButtonEdit buttonEdit, DxDataFormControlUseMode mode)
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
        private void _TextBoxButtonRead(DxDataFormItem item, Control control)
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
        private string _EditBoxGetKey(DxDataFormItem item)
        {
            string key = GetStandardKeyForItem(item);
            return key;
        }
        private void _EditBoxFill(DxDataFormItem item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxMemoEdit memoEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxMemoEdit).Name}.");
            CommonFill(item, memoEdit, mode);
            memoEdit.DeselectAll();
            memoEdit.SelectionStart = 0;
        }
        private void _EditBoxRead(DxDataFormItem item, Control control)
        { }
        #endregion
        // SpinnerBox
        #region CheckBox
        private Control _CheckBoxCreate() { return new DxCheckEdit(); }
        private string _CheckBoxGetKey(DxDataFormItem item)
        {
            string key = GetStandardKeyForItem(item);
            return key;
        }
        private void _CheckBoxFill(DxDataFormItem item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxCheckEdit checkEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxCheckEdit).Name}.");
            CommonFill(item, checkEdit, mode);
        }
        private void _CheckBoxRead(DxDataFormItem item, Control control)
        { }
        #endregion
        // BreadCrumb
        #region ComboBoxList
        private Control _ComboBoxListCreate() { return new DxTextEdit(); }
        private string _ComboBoxListGetKey(DxDataFormItem item)
        {
            string key = GetStandardKeyForItem(item);
            return key;
        }
        private void _ComboBoxListFill(DxDataFormItem item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxTextEdit textEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxTextEdit).Name}.");
            //  CommonFill(item, textEdit, mode);
            //  textEdit.DeselectAll();
            textEdit.SelectionStart = 0;
        }
        private void _ComboBoxListRead(DxDataFormItem item, Control control)
        { }
        #endregion
        #region ComboBoxEdit
        private Control _ComboBoxEditCreate() { return new DxTextEdit(); }
        private string _ComboBoxEditGetKey(DxDataFormItem item)
        {
            string key = GetStandardKeyForItem(item);
            return key;
        }
        private void _ComboBoxEditFill(DxDataFormItem item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxTextEdit textEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxTextEdit).Name}.");
            //  CommonFill(item, textEdit, mode);
            //  textEdit.DeselectAll();
            textEdit.SelectionStart = 0;
        }
        private void _ComboBoxEditRead(DxDataFormItem item, Control control)
        { }
        #endregion
        #region TokenEdit
        private Control _TokenEditCreate() { return new DxTokenEdit(); }
        private string _TokenEditGetKey(DxDataFormItem item)
        {
            string key = GetStandardKeyForItem(item);
            return key;
        }
        private void _TokenEditFill(DxDataFormItem item, Control control, DxDataFormControlUseMode mode)
        {
            if (!(control is DxTokenEdit tokenEdit)) throw new InvalidOperationException($"Nelze naplnit data do objektu typu {control.GetType().Name}, je očekáván objekt typu {typeof(DxTokenEdit).Name}.");
            CommonFill(item, tokenEdit, mode, _TokenEditFillSpec);
        }
        private void _TokenEditFillSpec(DxDataFormItem item, DxTokenEdit tokenEdit, DxDataFormControlUseMode mode)
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
        private void _TokenEditRead(DxDataFormItem item, Control control)
        { }
        #endregion
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
        private void _ButtonFill(DxDataFormItem item, Control control, DxDataFormControlUseMode mode)
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
        #region Společné metody pro získání klíče
        /// <summary>
        /// Vrátí standardní klíč daného prvku do ImageCache
        /// </summary>
        /// <param name="item"></param>
        /// <param name="specKeygenerator"></param>
        /// <returns></returns>
        private static string GetStandardKeyForItem(DxDataFormItem item, Func<DxDataFormItem, string> specKeygenerator = null)
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
        private void CommonFill<T>(DxDataFormItem item, T control, DxDataFormControlUseMode mode, Action<DxDataFormItem, T, DxDataFormControlUseMode> specificFillMethod = null) where T : BaseControl
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
        private DxSuperToolTip GetSuperTip(DxDataFormItem item, DxDataFormControlUseMode mode)
        {
            if (mode != DxDataFormControlUseMode.Mouse) return null;
            var superTip = __DataForm.DxSuperToolTip;
            superTip.LoadValues(item.IItem);
            if (!superTip.IsValid) return null;
            return superTip;
        }
        #endregion
        #region Získání a naplnění controlu z datového Itemu, a reverzní zpětné načtení hodnot z controlu do datového Itemu
        /// <summary>
        /// Typ prvku, který je popsán touto sadou
        /// </summary>
        internal DataFormColumnType ItemType { get { return __ItemType; } }
        /// <summary>
        /// Má být pro tento typ controlu pro <u>režim Draw</u> vytvořen fyzický Control?
        /// Pokud ano, pak bude control vytvořen, naplněn, umístěn do Parenta, vykreslen, Image zachycen a následně používán při kreslení Parenta.
        /// Pokud nemá být vytvořen fyzický Control pro zobrazení prvku, pak existuje metoda, která do dané cílové grafiky vykreslí obraz controlu přímo, 
        /// při akceptování všech jeho vlastností.
        /// </summary>
        internal bool UseControlForDraw { get { return __UseControlForDraw; } }
        private bool __UseControlForDraw;
        /// <summary>
        /// Má být pro tento typ controlu pro <u>režim OnMouse</u> vytvořen fyzický Control?
        /// Pokud ano, pak bude control vytvořen, naplněn, umístěn do Parenta na správné místo a bude fyzicky používán pokud nad jeho prostorem bude myš.
        /// Toto chování zajistí "živý vzhled" controlu = vizuální reakci na pohyb myši, a ToolTip.
        /// Pokud nemá být vytvořen fyzický Control pro zobrazení prvku, pak existuje metoda, která do dané cílové grafiky vykreslí obraz controlu přímo, 
        /// při akceptování všech jeho vlastností.
        /// </summary>
        internal bool UseControlForMouse { get { return __UseControlForMouse; } }
        private bool __UseControlForMouse;
        /// <summary>
        /// Má být pro tento typ controlu pro <u>režim Focus</u> vytvořen fyzický Control?
        /// Pokud ano, pak bude control vytvořen, naplněn, umístěn do Parenta na správné místo a bude do něj předán Focus, control sám si bude řešit uživatelskou interakci.
        /// Pokud nemá být vytvořen fyzický Control pro statické kreslení prvku, pak existuje metoda, která do dané cílové grafiky vykreslí obraz controlu přímo, 
        /// při akceptování všech jeho vlastností.
        /// </summary>
        internal bool UseControlForFocus { get { return __UseControlForFocus; } }
        private bool __UseControlForFocus;

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
        internal Control GetControlForMode(DxDataFormItem item, DxDataFormControlUseMode mode, Control parent)
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
        internal Control GetControlForDraw(DxDataFormItem item)
        {
            if (!__UseControlForDraw) return null;
            CheckNonDisposed();
            if (__ControlDraw == null)
                __ControlDraw = _CreateControl(DxDataFormControlUseMode.Draw);
            _FillControl(item, __ControlDraw, DxDataFormControlUseMode.Draw, null);
            return __ControlDraw;
        }
        /// <summary>
        /// Vrátí control pro daný prvek a režim Mouse
        /// </summary>
        /// <param name="item"></param>
        /// <param name="parent">Parent, do něhož má být control umístěn</param>
        /// <returns></returns>
        internal Control GetControlForMouse(DxDataFormItem item, Control parent)
        {
            if (!__UseControlForMouse) return null;
            CheckNonDisposed();
            if (__ControlMouse == null)
                __ControlMouse = _CreateControl(DxDataFormControlUseMode.Mouse);
            _FillControl(item, __ControlMouse, DxDataFormControlUseMode.Mouse, parent);
            return __ControlMouse;
        }
        /// <summary>
        /// Vrátí control pro daný prvek a režim Focus
        /// </summary>
        /// <param name="item"></param>
        /// <param name="parent">Parent, do něhož má být control umístěn</param>
        /// <returns></returns>
        internal Control GetControlForFocus(DxDataFormItem item, Control parent)
        {
            if (!__UseControlForFocus) return null;
            CheckNonDisposed();
            if (__ControlFocus == null)
                __ControlFocus = _CreateControl(DxDataFormControlUseMode.Focus);
            _FillControl(item, __ControlFocus, DxDataFormControlUseMode.Focus, parent);
            return __ControlFocus;
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
        private void _FillControl(DxDataFormItem item, Control control, DxDataFormControlUseMode mode, Control parent)
        {
            _FillControlAction(item, control, mode);
            __DataForm.AddControl(control, (mode == DxDataFormControlUseMode.Draw ? null : parent));
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
        private Control __ControlDraw;
        private Control __ControlMouse;
        private Control __ControlFocus;
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
        /// Znovu načíst všechny řádky = po změně zdroje dat
        /// </summary>
        ReloadAllRows = 0x0100,
        /// <summary>
        /// Určit aktuálně viditelné řádky
        /// </summary>
        ReloadVisibleRows = 0x0200,
        /// <summary>
        /// Určit aktuálně viditelné grupy
        /// </summary>
        ReloadVisibleGroups = 0x0400,
        /// <summary>
        /// Určit aktuálně viditelné buňky
        /// </summary>
        ReloadVisibleCells = 0x0800,
        /// <summary>
        /// Resetovat cache předvykreslených controlů
        /// </summary>
        InvalidateCache = 0x1000,
        /// <summary>
        /// Vyřešit souřadnice nativních controlů, nacházejících se v Content panelu
        /// </summary>
        NativeControlsLocation = 0x2000,
        /// <summary>
        /// Znovuvykreslit grafiku
        /// </summary>
        InvalidateControl = 0x4000,
        /// <summary>
        /// Explicitně vyvolat i metodu <see cref="Control.Refresh()"/>
        /// </summary>
        RefreshControl = 0x8000,

        /// <summary>
        /// Po změně řádků (<see cref="RecalculateContentTotalSize"/> + <see cref="ReloadVisibleCells"/>).
        /// Tato hodnota je Silent = neobsahuje <see cref="InvalidateControl"/>.
        /// </summary>
        AfterRowsChangedSilent = RecalculateContentTotalSize | ReloadVisibleRows | ReloadVisibleGroups | ReloadVisibleCells | NativeControlsLocation,
        /// <summary>
        /// Po změně řádků nebo prvků (přidání, odebrání, změna hodnot) (<see cref="RecalculateContentTotalSize"/> + <see cref="ReloadVisibleCells"/>).
        /// Tato hodnota je Silent = neobsahuje <see cref="InvalidateControl"/>.
        /// </summary>
        AfterGroupsChangedSilent = RecalculateContentTotalSize | ReloadVisibleCells | NativeControlsLocation,
        /// <summary>
        /// Po změně prvků (přidání, odebrání, změna hodnot) (<see cref="RecalculateContentTotalSize"/> + <see cref="ReloadVisibleCells"/>).
        /// Tato hodnota je Silent = neobsahuje <see cref="InvalidateControl"/>.
        /// </summary>
        AfterCellsChangedSilent = RecalculateContentTotalSize | ReloadVisibleCells | NativeControlsLocation,
        /// <summary>
        /// Po změně prvků (přidání, odebrání, změna hodnot) (<see cref="RecalculateContentTotalSize"/> + <see cref="ReloadVisibleCells"/> + <see cref="InvalidateControl"/>).
        /// Tato hodnota není Silent = obsahuje i invalidaci <see cref="InvalidateControl"/> = překreslení controlu.
        /// <para/>
        /// Toto je standardní refresh.
        /// </summary>
        AfterItemsChanged = RecalculateContentTotalSize | ReloadVisibleCells | NativeControlsLocation | InvalidateControl,
        /// <summary>
        /// Po scrollování (<see cref="ReloadVisibleCells"/> + <see cref="InvalidateControl"/>)
        /// </summary>
        AfterScroll = ReloadVisibleCells | NativeControlsLocation | InvalidateControl,
        /// <summary>
        /// Po změně prvků (přidání, odebrání, změna hodnot) (<see cref="RecalculateContentTotalSize"/> + <see cref="ReloadVisibleCells"/> + <see cref="InvalidateControl"/>).
        /// <para/>
        /// Toto je standardní refresh.
        /// </summary>
        Default = AfterItemsChanged,
        /// <summary>
        /// Všechny akce, včetně invalidace cache (brutální refresh)
        /// </summary>
        All = RecalculateContentTotalSize | ReloadVisibleRows | ReloadVisibleGroups | ReloadVisibleCells | NativeControlsLocation | InvalidateCache | InvalidateControl
    }
    #endregion
}
