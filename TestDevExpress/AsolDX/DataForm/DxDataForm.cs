// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using WinDraw = System.Drawing;
using WinForm = System.Windows.Forms;

using DevExpress.XtraEditors;
using DevExpress.Utils.Extensions;
using DevExpress.XtraRichEdit.Model.History;
using Noris.Clients.Win.Components.AsolDX.DataForm.Data;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    #region DxDataFormPanel : vnější panel DataForm - koordinátor, virtuální container
    /// <summary>
    /// <see cref="DxDataFormPanel"/> : vnější panel DataForm - koordinátor, virtuální container.
    /// Obsahuje vnitřní ContentPanel typu <see cref="DxDataFormContentPanel"/>, který reálně zobrazuje obsah (řeší scrollování).
    /// Obsahuje kolekci řádků <see cref="DataFormRows"/> a deklaraci layoutu <see cref="DataFormLayoutSet"/>.
    /// Obsahuje managera fyzických controlů (obdoba RepositoryEditorů) <see cref="DxRepositoryManager"/>
    /// </summary>
    public class DxDataFormPanel : DxVirtualPanel
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxDataFormPanel()
        {
            _InitRows();
            _InitLayout();
            _InitRepository();
            _InitContent();

            __Initialized = true;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            _DisposeContent();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Obsahuje true po skončení inicializace
        /// </summary>
        private bool __Initialized;
        /// <summary>
        /// Po změně velikosti nebo scrollbarů ve virtual panelu zajistíme přepočet interaktivních prvků
        /// </summary>
        protected override void OnVisibleDesignBoundsChanged()
        {
            InteractiveItemsInvalidate(false);
        }
        #endregion
        #region Datové řádky
        /// <summary>
        /// Pole řádků zobrazených v formuláři
        /// </summary>
        public DataFormRows DataFormRows { get { return __DataFormRows; } }
        /// <summary>
        /// Inicializace dat řádků
        /// </summary>
        private void _InitRows()
        {
            __DataFormRows = new DataFormRows(this);
            __DataFormRows.CollectionChanged += _RowsChanged;
        }
        /// <summary>
        /// Po změně řádků (přidání, odebrání), nikoli po změně obsahu dat v řádku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _RowsChanged(object sender, EventArgs e)
        {
            DesignSizeInvalidate();
            InteractiveItemsInvalidate(true);
        }
        /// <summary>
        /// Fyzická kolekce řádků
        /// </summary>
        private DataFormRows __DataFormRows;
        #endregion
        #region Definice layoutu
        /// <summary>
        /// Definice vzhledu pro jednotlivý řádek: popisuje panely, záložky, prvky ve vnořené hierarchické podobě.
        /// Z této definice se následně generují jednotlivé interaktivní prvky pro jednotlivý řádek.
        /// </summary>
        public DataFormLayoutSet DataFormLayout { get { return __DataFormLayout; } }
        /// <summary>
        /// Inicializace dat layoutu
        /// </summary>
        private void _InitLayout()
        {
            __DataFormLayout = new DataFormLayoutSet(this);
            __DataFormLayout.CollectionChanged += _LayoutChanged;

            Padding = new WinForm.Padding(0);
        }
        /// <summary>
        /// Změna Padding vede ke změně <see cref="ContentDesignSize"/>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaddingChanged(EventArgs e)
        {
            DesignSizeInvalidate();
        }
        /// <summary>
        /// Definice vzhledu pro jednotlivý řádek
        /// </summary>
        private DataFormLayoutSet __DataFormLayout;
        /// <summary>
        /// Po změně definice layoutu (přidání, odebrání), nikoli po změně obsahu dat v prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LayoutChanged(object sender, EventArgs e)
        {
            DesignSizeInvalidate();
            InteractiveItemsInvalidate(true);
        }
        #endregion
        #region Repository manager


        private void _InitRepository()
        {
            __RepositoryManager = new DxRepositoryManager(this);
        }

        private DxRepositoryManager __RepositoryManager;
        #endregion
        #region ContentPanel
        /// <summary>
        /// Inicializace Content panelu
        /// </summary>
        private void _InitContent()
        {
            __DataFormContent = new DxDataFormContentPanel();
            this.ContentPanel = __DataFormContent;
        }
        private void _DisposeContent()
        {
            this.ContentPanel = null;
        }
        /// <summary>
        /// ContentPanel, potomek <see cref="DxDataFormContentPanel"/>
        /// </summary>
        public DxDataFormContentPanel DataFormContent { get { return __DataFormContent; } }
        /// <summary>
        /// ContentPanel, potomek <see cref="DxDataFormContentPanel"/>
        /// </summary>
        private DxDataFormContentPanel __DataFormContent;
        #endregion
        #region ContentDesignSize : velikost obsahu v designových pixelech
        /// <summary>
        /// Potřebná velikost obsahu v designových pixelech. Validovaná hodnota.
        /// </summary>
        public override WinDraw.Size? ContentDesignSize 
        {
            get
            {
                if (__Initialized && !__ContentDesignSize.HasValue)
                {
                    DesignSizeInvalidate();
                    __ContentDesignSize = _CalculateContentDesignSize();
                }
                return __ContentDesignSize;
            }
            set { }
        }
        /// <summary>
        /// Po změně hodnoty <see cref="DxVirtualPanel.ContentPanelDesignSize"/>
        /// </summary>
        protected override void OnContentPanelDesignSize()
        {
            base.OnContentPanelDesignSize();
            if (__Initialized && this.DataFormLayout.IsDesignSizeDependOnHostSize)
                DesignSizeInvalidate();
        }
        /// <summary>
        /// Invaliduje celkovou velikost <see cref="ContentDesignSize"/>.
        /// Provádí se po změně řádků nebo definice designu.
        /// </summary>
        protected override void DesignSizeInvalidate()
        {
            __ContentDesignSize = null;
            base.DesignSizeInvalidate();
        }
        /// <summary>
        /// Vypočte a vrátí údaj: Potřebná velikost obsahu v designových pixelech.
        /// </summary>
        /// <returns></returns>
        private WinDraw.Size _CalculateContentDesignSize()
        {
            var padding = this.Padding;
            this.DataFormLayout.HostDesignSize = this.ContentPanelDesignSize;            // Do Layoutu vložím viditelnou velikost
            this.DataFormRows.OneRowDesignSize = this.DataFormLayout.DesignSize;         // Z Layoutu načtu velikost jednoho řádku a vložím do RowSetu
            var allRowsDesignSize = this.DataFormRows.ContentDesignSize;                 // Z RowSetu načtu velikost všech řádků
            return allRowsDesignSize.Add(padding);
        }
        /// <summary>
        /// Potřebná velikost obsahu v designových pixelech. Úložiště.
        /// </summary>
        private WinDraw.Size? __ContentDesignSize;
        #endregion
        #region InteractiveItems : aktuálně dostupné prvky pro zobrazení
        /// <summary>
        /// Interaktivní data = jednotlivé prvky, platné pro aktuální layout a řádky a pozici Scrollbaru. Validní hodnota.
        /// </summary>
        public IList<IInteractiveItem> InteractiveItems { get { return _GetValidInteractiveItems(); } }
        /// <summary>
        /// Vrátí platé interaktivní prvky
        /// </summary>
        /// <returns></returns>
        private List<IInteractiveItem> _GetValidInteractiveItems()
        {
            if (__InteractiveItems is null || !_IsValidInteractiveRows())
                _PrepareValidInteractiveItems();
            return __InteractiveItems;
        }
        /// <summary>
        /// Vrátí true, pokud máme připravená platná data v <see cref="__InteractiveItems"/> pro aktuální viditelnou oblast.
        /// </summary>
        /// <returns></returns>
        private bool _IsValidInteractiveRows()
        {
            if (__InteractiveItems is null) return false;
            if (!_IsValidInteractiveItemsForVisibleBounds()) return false;
            return true;
        }
        /// <summary>
        /// Metoda zajistí přípravu interaktivních prvků do <see cref="__InteractiveItems"/> pro řádky, které jsou v aktuální viditelné oblasti plus kousek okolo.
        /// </summary>
        private void _PrepareValidInteractiveItems()
        {
            Int32Range designPixels = _GetDesignPixelsForInteractiveRows();
            var rows = this.DataFormRows.GetRowsInDesignPixels(designPixels);
            List<IInteractiveItem> items = new List<IInteractiveItem>();
            foreach (var row in rows)
                row.PrepareValidInteractiveItems(items);

            __InteractiveItems = items;
            __InteractiveItemsDesignPixels = designPixels;
        }
        /// <summary>
        /// Určí rozsah designových pixelů, za jejichž odpovídající řádky budeme generovat interaktivní prvky.
        /// Pokud vrátí null = pak se načtou všechny řádky.
        /// </summary>
        /// <returns></returns>
        private Int32Range _GetDesignPixelsForInteractiveRows()
        {
            var contentSize = this.ContentDesignSize;                                    // Jak velký je celý obsah dat v DataFormu = výška kompletního balíku všech řádků
            if (!contentSize.HasValue) return null;

            // Pokud celý rozsah mých řádků je menší než minimum pro dynamické stránkování, tak vrátím celý rozsah a vytvoří se prvky pro všechny řádky:
            int contentHeight = contentSize.Value.Height;
            if (contentHeight < _MinimalHeightToCreateDynamicItems) new Int32Range(0, contentHeight, false);

            var visibleBounds = this.VisibleDesignBounds;                                // Kolik prostoru mám reálně na zobrazení
            double ratio = (double)visibleBounds.Height / (double)contentHeight;         // Jak velkou poměrnou část z celých dat aktuálně zobrazíme v controlu
            // Pokud viditelná oblast pokrývá relativně větší část z výšky všech řádků, tak vrátím celý rozsah a vytvoří se prvky pro všechny řádky:
            if (ratio >= _MinimalRatioToCreateDynamicItems) new Int32Range(0, contentHeight, false);

            // Máme hodně velká data (hodně řádků * výška layoutu), vytvoříme prvky jen pro podmnožinu řádků a následně budeme provádět dynamické scrollování:
            int addition = 2 * visibleBounds.Height;
            if (addition < _MinimalAdditionheightForDynamicItems) addition = _MinimalAdditionheightForDynamicItems;
            int rowBegin = visibleBounds.Top - addition;
            if (rowBegin < 0) rowBegin = 0;
            int rowEnd = visibleBounds.Bottom + addition;
            if (rowEnd > contentHeight) rowEnd = contentHeight;

            bool isDynamic = (rowBegin > 0 && rowEnd <contentHeight);
            return new Int32Range(rowBegin, rowEnd, isDynamic);
        }
        /// <summary>
        /// Počet pixelů výšky dat (=výška všech řádků), kdy je jednodušší vytvořit prvky za všechny řádky, než začít řešit dynamické stránkování
        /// </summary>
        private const int _MinimalHeightToCreateDynamicItems = 2000;
        /// <summary>
        /// Poměr výšky viditelné ku výšce všech řádků, kdy je jednodušší vytvořit prvky za všechny řádky, než začít řešit dynamické stránkování.
        /// Pokud mám control, který zobrazuje na výšku 600px, a výška všech řádků (v aktuálním layoutu) je 2000px, pak se vyplatí vytvořit všechny interaktivní prvky najednou
        /// a při scrollování pak nebude nutno 
        /// </summary>
        private const double _MinimalRatioToCreateDynamicItems = 0.25d;
        /// <summary>
        /// Počet přidaných pixelů výšky nad viditelný počátek a pod viditelný konec.
        /// </summary>
        private const int _MinimalAdditionheightForDynamicItems = 800;
        /// <summary>
        /// Invaliduje soupis prvků v <see cref="InteractiveItems"/>.
        /// Podle parametru force: 
        /// true = invalidace bezpodmínečná; 
        /// false (nepovinně) = jen když aktuálně viditelný prostor zobrazuje data, která nejsou připravena (po Scrollu).
        /// </summary>
        /// <param name="force"></param>
        protected void InteractiveItemsInvalidate(bool force)
        {
            if (!force && _IsValidInteractiveItemsForVisibleBounds())
                // Non force (false) => pokud pro aktuální viditelnou oblast v VisibleDesignBounds máme již z dřívějška připraveny prvky, pak je nebudu invalidovat:
                return;
           
            // Musíme zahodit připravená data:
            __InteractiveItems = null;
            __InteractiveItemsDesignPixels = null;
        }
        /// <summary>
        /// Vrátí true, pokud máme připraveny prvky pro aktuálně viditelnou oblast. false pokud prvky nejsou připraveny vůbec, anebo nepokrývají viditelnou oblast.
        /// </summary>
        /// <returns></returns>
        private bool _IsValidInteractiveItemsForVisibleBounds()
        {
            if (__InteractiveItems != null && __InteractiveItemsDesignPixels != null)
            {
                var visibleBounds = this.VisibleDesignBounds;                  // Aktuálně zobrazená oblast
                if (visibleBounds.Height <= 0) return true;                    // Nic není viditelno: pak jsme OK.

                // Pokud viditelná oblast se nachází uvnitř oblasti, kterou máme připravenou od dřívějška, pak si data neinvalidujeme.
                Int32Range designPixels = __InteractiveItemsDesignPixels;
                // Jde o malý posun a my jsme si moudře připravili interaktivní prvky pro větší oblast, než bylo nutno, takže nyní nemusíme zahazovat a generovat vše...
                if (visibleBounds.Top >= designPixels.Begin && visibleBounds.Bottom <= designPixels.End) return true;
            }
            return false;
        }
        /// <summary>
        /// Interaktivní data = jednotlivé prvky, platné pro aktuální layout a řádky a pozici Scrollbaru. Úložiště.
        /// Pokrývá oblast na ose Y v rozsahu <see cref="__InteractiveItemsDesignPixels"/>.
        /// </summary>
        private List<IInteractiveItem> __InteractiveItems;
        /// <summary>
        /// Rozsah designových pixelů od prvního do posledního řádku, který má připravená svoje data v <see cref="__InteractiveItems"/>.
        /// Zde je využit i příznak IsVariable, který říká: 
        /// IsVariable = true: jsem proměnný interval, zobrazuji jen část řádků, při scrollování je třeba provádět reload interaktivních prvků;
        /// IsVariable = false: jsem konstantní interval, zobrazuji všechny řádky. 
        /// </summary>
        private Int32Range __InteractiveItemsDesignPixels;
        #endregion
    }
    #endregion
    #region DxDataFormContentPanel : fyzický interaktivní panel pro zobrazení contentu DataFormu
    /// <summary>
    /// <see cref="DxDataFormContentPanel"/> : fyzický interaktivní panel pro zobrazení contentu DataFormu.
    /// Řeší grafické vykreslení prvků a řeší interaktivitu myši a klávesnice.
    /// Pro fyzické vykreslení obsahu prvku volá jeho vlastní metodu, to neřeší panel.
    /// </summary>
    public class DxDataFormContentPanel : DxInteractivePanel
    {
        #region Info ... Vnoření prvků, souřadné systémy, řádky
        /*
        Třída DxInteractivePanel (náš předek) v sobě hostuje interaktivní prvky IInteractiveItem uložené v DxInteractivePanel.Items, ty tvoří Root úroveň.
        Mohou to být Containery { Panely, Záložky } nebo samotné prvky { TextBox, Label, Combo, Picture, CheckBox, atd }.
        Containery v sobě mohou obsahovat další Containery nebo samotné prvky.
        Containery obsahují svoje Child prvky v IInteractiveItem.Items. Jejich souřadný systém je pak relativní k jejich Parentu.
        Scrollování je podporováno jen na úrovni celého DataFormu, ale nikoliv na úrovni Containeru (=Panel ani Záložka nebude mít ScrollBary).
        _LayoutChanged podporuje Zoom. Výchozí je vložen při tvorbě DataFormu anebo při změně Zoomu, podporován je i interaktivní Zoom: Ctrl+MouseWheel.
        Jde o dvě hodnoty Zoomu: systémový × lokální.
        Prvek IInteractiveItem definuje svoji pozici v property DesignBounds, kde je souřadnice daná v Design pixelech (bez vlivu Zoomu), relativně k Parentu, v rámci řádku.

        Řádky:
        Pole prvků DxInteractivePanel.Items definuje vzhled jednoho řádku.








        */
        #endregion
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxDataFormContentPanel()
        {
           
        }
        /// <summary>
        /// Metoda zajistí, že velikost <see cref="DxInteractivePanel.ContentDesignSize"/> bude platná (bude odpovídat souhrnu velikosti prvků).
        /// Metoda je volána před každým Draw tohoto objektu.
        /// </summary>
        protected override void ContentDesignSizeCheckValidity(bool force = false)
        {
            // Zde jsme ve třídě DxDataFormContentPanel, kde zodpovědnost za velikost ContentDesignSize nese náš Parent = třída DxDataFormPanel.
            // Zde jsme voláni v čase Draw() tohoto ContentPanelu, a to je už mírně pozdě na přepočty ContentDesignSize, protože Parent už má svůj layout vytvořený.
            // Validaci i uchování hodnoty ContentDesignSize provádí parent (DxDataFormPanel), validaci volá před svým vykreslením.
            // Proto my vůbec nevoláme:
            //    base.ContentDesignSizeCheckValidity(force);
            // - protože by šlo o nadbytečnou akci.
            // Base třída by si napočítala ContentDesignSize ze svých InteractiveItems, které rozhodně netvoří celý obsazený prostor.
            // DataForm plní fyzické InteractiveItems pouze pro potřebné viditelné řádky, ale ContentDesignSize odpovídá všem řádkům.
        }
        #endregion
        #region Napojení zdejšího interaktivního panelu na zdroje v parentu DxDataFormPanel
        /// <summary>
        /// Hostitelský panel <see cref="DxDataFormPanel"/>; ten řeší naprostou většinu našich požadavků.
        /// My jsme jen jeho zobrazovací plocha.
        /// </summary>
        protected DxDataFormPanel DataFormPanel { get { return this.VirtualPanel as DxDataFormPanel; } }
        /// <summary>
        /// Interaktivní data = jednotlivé prvky.
        /// Třída <see cref="DxDataFormContentPanel"/> zde vrací pole z Parenta <see cref="DxDataFormPanel.InteractiveItems"/>.
        /// </summary>
        protected override IList<IInteractiveItem> InteractiveItems { get { return DataFormPanel?.InteractiveItems; } }
        #endregion
    }
    #endregion
}
