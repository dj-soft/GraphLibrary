// Supervisor: David Janáček, od 01.11.2023
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

using DxfData = Noris.Clients.Win.Components.AsolDX.DataForm.Data;
using Noris.Clients.Win.Components.AsolDX.DataForm.Data;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    #region DxDataForm : koordinátor DataFormu - datová část, propojená s vizuálním DxDataFormPanel
    /// <summary>
    /// <see cref="DxDataForm"/> : koordinátor DataFormu - datová část, propojená s vizuálním DxDataFormPanel.
    /// Ttao třída řeší veškerá data, ale nejde o vizuální panel. Na ten je napojen.
    /// Obsahuje kolekci řádků <see cref="DataFormRows"/> a deklaraci layoutu <see cref="DataFormLayoutSet"/>.
    /// Obsahuje managera fyzických controlů (obdoba RepositoryEditorů) <see cref="DxRepositoryManager"/>
    /// </summary>
    internal class DxDataForm : IDisposable
    {
        #region Konstruktor a napojení na vizuální control
        /// <summary>
        /// Konstruktor
        /// </summary>
        internal DxDataForm(DxDataFormPanel dataFormPanel)
        {
            __DataFormPanel = dataFormPanel;
            _InitRows();
            _InitLayout();
            _InitRepository();

            __IsPrepared = true;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        public void Dispose()
        {
            _DisposeContent();
            __IsPrepared = false;
        }
        /// <summary>
        /// Obsahuje true po skončení inicializace
        /// </summary>
        internal bool IsPrepared { get { return __IsPrepared; } } private bool __IsPrepared;
        /// <summary>
        /// Rozpustí obsah
        /// </summary>
        private void _DisposeContent()
        {
            __DataFormPanel = null;
        }
        /// <summary>
        /// Po změně velikosti nebo scrollbarů ve virtual panelu zajistíme přepočet interaktivních prvků
        /// </summary>
        internal void OnVisibleDesignBoundsChanged()
        {
            InvalidateInteractiveItems(false);
        }
        #endregion
        #region Napojení na vizuální panel
        /// <summary>
        /// Obsahuje true, pokud this datový základ má napojen vizuální panel <see cref="DataFormPanel"/>
        /// </summary>
        internal bool HasDataFormPanel { get { return __DataFormPanel != null; } }
        /// <summary>
        /// Vizuální control <see cref="DxDataFormPanel"/> = virtuální hostitel obsahující Scrollbary a <see cref="DxDataFormContentPanel"/>
        /// </summary>
        internal DxDataFormPanel DataFormPanel { get { return __DataFormPanel; } set { __DataFormPanel = value; } }
        /// <summary>
        /// ContentPanel (<see cref="DxDataFormContentPanel"/>), v něm se fyzicky zobrazují obrazy a controly DataFormu
        /// </summary>
        internal DxDataFormContentPanel DataFormContent { get { return __DataFormPanel?.DataFormContent; } }
        /// <summary>
        /// Používat testovací vykreslování
        /// </summary>
        internal bool TestPainting { get { return DataFormPanel?.TestPainting ?? false; } set { if (DataFormPanel != null) DataFormPanel.TestPainting = value; } }
        /// <summary>
        /// Zajistí znovuvykreslení panelu s daty
        /// </summary>
        internal void DrawContent()
        {
            this.DataFormContent?.Draw();
        }
        /// <summary>
        /// Vizuální control <see cref="DxDataFormPanel"/> = virtuální hostitel obsahující Scrollbary a <see cref="DxDataFormContentPanel"/>
        /// </summary>
        private DxDataFormPanel __DataFormPanel;
        #endregion
        #region Formát DataFormu = *.frm.xml
        /// <summary>
        /// Zde je obsažena definice formátu dat v dataformu.
        /// </summary>
        public DxDataFormat DataFormat 
        {
            get { return __DataFormat; }
            set { }
        }
        private DxDataFormat __DataFormat;
        #endregion
        #region Datové řádky
        /// <summary>
        /// Pole řádků zobrazených v formuláři
        /// </summary>
        internal DxfData.DataFormRows DataFormRows { get { return __DataFormRows; } }
        /// <summary>
        /// Inicializace dat řádků
        /// </summary>
        private void _InitRows()
        {
            __DataFormRows = new DxfData.DataFormRows(this);
            __DataFormRows.CollectionChanged += _RowsChanged;
        }
        /// <summary>
        /// Po změně řádků (přidání, odebrání), nikoli po změně obsahu dat v řádku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _RowsChanged(object sender, EventArgs e)
        {
            InvalidateInteractiveItems(true);
            InvalidateContentDesignSize(true, true);
        }
        /// <summary>
        /// Fyzická kolekce řádků
        /// </summary>
        private DxfData.DataFormRows __DataFormRows;
        #endregion
        #region Definice layoutu : pole 
        /// <summary>
        /// Definice vzhledu pro jednotlivý řádek: popisuje panely, záložky, prvky ve vnořené hierarchické podobě.
        /// Z této definice se následně generují jednotlivé interaktivní prvky pro jednotlivý řádek.
        /// </summary>
        internal DxfData.DataFormLayoutSet DataFormLayout { get { return __DataFormLayout; } }
        /// <summary>
        /// Inicializace dat layoutu
        /// </summary>
        private void _InitLayout()
        {
            __DataFormLayout = new DxfData.DataFormLayoutSet(this);
            __DataFormLayout.CollectionChanged += _LayoutChanged;
            __Content = new DxfData.DataContent();
            PrepareDefaultContentValues();
        }
        /// <summary>
        /// Definice vzhledu pro jednotlivý řádek
        /// </summary>
        private DxfData.DataFormLayoutSet __DataFormLayout;
        /// <summary>
        /// Po změně definice layoutu (přidání, odebrání), nikoli po změně obsahu dat v prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LayoutChanged(object sender, EventArgs e)
        {
            InvalidateInteractiveItems(true);
            InvalidateContentDesignSize(true, true);
        }
        /// <summary>
        /// Obsah řádků: obsahuje sloupce i jejich datové a popisné hodnoty.
        /// Klíčem je název sloupce.
        /// </summary>
        internal DxfData.DataContent Content { get { return __Content; } } private DxfData.DataContent __Content;
        #endregion
        #region Defaultní hodnoty layoutu a chování, uložené do Content
        /// <summary>
        /// Naplní defaultní hodnoty určitých vlastností, které se použijí v případě, 
        /// kdy nebudou zadány hodnoty ani pro <see cref="DxfData.DataFormRow"/>, ani pro <see cref="DxLayoutItemInfo"/>.
        /// </summary>
        protected virtual void PrepareDefaultContentValues()
        {
            Content[DxfData.DxDataFormProperty.BorderStyle] = DataForm.BorderStyle.HotFlat;
            Content[DxfData.DxDataFormProperty.CheckBoxBorderStyle] = DataForm.BorderStyle.NoBorder;
            Content[Data.DxDataFormProperty.ToggleSwitchRatio] = 2.5f;
            Content[Data.DxDataFormProperty.CheckBoxLabelFalse] = "Vypnuto";
            Content[Data.DxDataFormProperty.CheckBoxLabelTrue] = "Aktivní";
        }
        #endregion
        #region Repository manager
        /// <summary>
        /// Repozitory, obsahující fyzické controly pro zobrazení a editaci dat
        /// </summary>
        internal DxRepositoryManager RepositoryManager { get { return __RepositoryManager; } }
        /// <summary>
        /// Inicializace repozitory
        /// </summary>
        private void _InitRepository()
        {
            __RepositoryManager = new DxRepositoryManager(this);
            __CacheImageFormat = WinDraw.Imaging.ImageFormat.Png;
        }
        /// <summary>
        /// Repozitory
        /// </summary>
        private DxRepositoryManager __RepositoryManager;
        /// <summary>
        /// Invaliduje repozitory a uložené bitmapy po změně skinu a zoomu. 
        /// Vyvolá překreslení grafického panelu <see cref="DataFormContent"/>.
        /// </summary>
        internal void InvalidateRepozitory()
        {
            __RepositoryManager?.InvalidateManager();
            InvalidateInteractiveImageCache();
            DataFormContent.Draw();
        }
        /// <summary>
        /// Formát bitmap, který se ukládá do cache
        /// </summary>
        internal WinDraw.Imaging.ImageFormat CacheImageFormat { get { return __CacheImageFormat; } set { __CacheImageFormat = value; InvalidateRepozitory(); } } private WinDraw.Imaging.ImageFormat __CacheImageFormat;
        #endregion
        #region ContentDesignSize : velikost obsahu v designových pixelech
        /// <summary>
        /// Potřebná velikost obsahu v designových pixelech. Validovaná hodnota.
        /// </summary>
        internal WinDraw.Size? ContentDesignSize
        {
            get
            {
                if (__IsPrepared && !__ContentDesignSize.HasValue)
                {
                    __ContentDesignSize = _CalculateContentDesignSize();
                }
                return __ContentDesignSize;
            }
            set { }
        }
        /// <summary>
        /// Zajistí invalidaci hodnoty <see cref="ContentDesignSize"/>, podle parametru <paramref name="forceSize"/>:
        /// Pokud je true, pak se invaliduje vždy (používá se po změně řádků nebo layoutu); 
        /// pokud je false pak se invaliduje jen tehdy, když uspořádání prvků je závislé na velikosti panelu (<see cref="DataFormLayoutSet.IsDesignSizeDependOnHostSize"/>.
        /// </summary>
        /// <param name="forceSize">Invalidovat velikost: true = vždy / false = jen když vychází z rozměrů hostitele</param>
        /// <param name="runDraw">Po invalidaci vyvolat Draw</param>
        internal void InvalidateContentDesignSize(bool forceSize, bool runDraw)
        {
            bool invalidateDesignSize = forceSize;
            if (!invalidateDesignSize && __IsPrepared && this.DataFormLayout.IsDesignSizeDependOnHostSize) invalidateDesignSize = true;
            if (invalidateDesignSize)
            {
                __ContentDesignSize = null;
                DataFormPanel.InvalidateVisualDesignSize(runDraw);
            }

            if (runDraw)
                this.DataFormContent?.Draw();
        }
        /// <summary>
        /// Vypočte a vrátí údaj: Potřebná velikost obsahu v designových pixelech.
        /// </summary>
        /// <returns></returns>
        private WinDraw.Size _CalculateContentDesignSize()
        {
            var padding = this.DataFormPanel.Padding;
            this.DataFormLayout.HostDesignSize = this.DataFormPanel.ContentPanelDesignSize;        // Do Layoutu vložím viditelnou velikost
            this.DataFormRows.OneRowDesignSize = this.DataFormLayout.DesignSize;                   // Z Layoutu načtu velikost jednoho řádku a vložím do RowSetu
            var allRowsDesignSize = this.DataFormRows.ContentDesignSize;                           // Z RowSetu načtu velikost všech řádků
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
        internal IList<IInteractiveItem> InteractiveItems { get { return _GetValidInteractiveItems(); } }
        /// <summary>
        /// Vrátí platé interaktivní prvky
        /// </summary>
        /// <returns></returns>
        private List<IInteractiveItem> _GetValidInteractiveItems()
        {
            if (__InteractiveItems is null || !_IsValidPreparedInteractiveRows())
                _PrepareValidInteractiveItems();
            return __InteractiveItems;
        }
        /// <summary>
        /// Metoda zajistí přípravu interaktivních prvků do <see cref="__InteractiveItems"/> pro řádky, které jsou v aktuální viditelné oblasti plus kousek okolo.
        /// </summary>
        private void _PrepareValidInteractiveItems()
        {
            Int32Range designPixels = _GetDesignPixelsForInteractiveRows();              // Rozsah designových pixelů Od-Do, jejichž řádky bychom měli načíst (aktuálně viditelné plus rezerva nahoře a dole, ale možná taky null = celý rozsah)
            var rows = this.DataFormRows.GetRowsInDesignPixels(ref designPixels);        // Načteme řádky a případně modifikujeme hodnoty v 'designPixels' podle reálných řádků
            var items = new List<IInteractiveItem>();
            foreach (var row in rows)
                row.PrepareValidInteractiveItems(items);                                 // Řádky připraví svoje interaktivní prvky

            DxComponent.LogAddLine(LogActivityKind.DataFormRepository,  $"DataFormPanel.PrepareValidInteractiveItems(): VisibleRows.Count: {rows.Length}; Items.Count: {items.Count}; PreparedPixels: {designPixels}");

            __VisibleRows = rows;
            __InteractiveItems = items;
            __InteractiveItemsPreparedDesignPixels = designPixels;

            this.DataFormContent?.ItemsAllChanged();
        }
        /// <summary>
        /// Určí rozsah designových pixelů, za jejichž odpovídající řádky budeme generovat interaktivní prvky.
        /// Obsahuje řadu optimalizací.
        /// Pokud vrátí null = pak se načtou všechny řádky.
        /// Pokud vrátí not null instanci, pak ta je Variable = lze do ní modifikovat reálný počátek a konec načtených dat, to řeší <see cref="DataFormRows.GetRowsInDesignPixels(ref Int32Range)"/>, podle reálně nalezených hodnot řádků.
        /// Zde určujeme "požadovaný rozsah", ale nehledáme konkrétní řádky a jejich souřadnice. To je určeno až při nalezení konkrétních řádků, pak se modifikuje tato instance <see cref="Int32Range"/>, 
        /// protože ta poté slouží při kontrole, zda máme načtena data pro nově posunutou oblast...
        /// </summary>
        /// <returns></returns>
        private Int32Range _GetDesignPixelsForInteractiveRows()
        {
            var contentSize = this.ContentDesignSize;                                    // Jak velký je celý obsah dat v DataFormu = výška kompletního balíku všech řádků
            if (!contentSize.HasValue) return null;
            if (!HasDataFormPanel) return null;

            /*   Slovní vysvětlení:

             1. Je jisto, že budeme načítat interaktivní prvky pro některé řádky;
             2. Buď můžeme načíst interaktivní prvky pro všechny řádky, ale to někdy může být milion prvků (Dataform může zobrazovat 10000 řádků a 50 sloupců + 50 labelů)
                  => to je maximalistická krajní mez, a to nechceme dopustit (velká spotřeba paměti, časy při hledání prvků v paměti);
             3. Anebo můžeme načíst pouze ty řádky, které jsou ve viditelné oblasti - bez žádné rezervy nad a pod
                  => pak ale každý malý Scroll vede k tomu, že nebudeme mít podklady pro nově nascrollovanou oblast a budeme hledat interaktivní prvky pro nově posunutou oblast
                     (každý malý Scroll vede k režii a spotřebě času a k pomalé reakci a trhání obrazu)
             4. Takže zvolíme střední cestu:
                - Pokud výška dat je menší než 2000 pixelů, tak načtu vše najednou, to vyřeší většinu běžných formulářů (odhaduji na 95%)
                - Pokud viditelný prostor reprezentuje 25% výšky dat nebo víc, pak načtu vše najednou, protože tím předvyřeším zdržení při scrollování
                - Pak už tedy řeším to, jak velkou podmnožinu dat z celkového rozsahu načtu nyní
                - Pokusíme se optimalizovat mezi úsporou paměti a rychlostí reakce při scrollování:
                - Když už načítám, tak načtu 2,5 obrazovky před i za aktuální prostor (tím urychlím scrollování), 
                   ale pro malý viditelný prostor by 2.5 násobek nestál za řeč - takže přinejmenším načtu 800px navíc
                   => toto je přídavek (výška v pixelech) nad rámec viditelné oblasti, nahoře i dole;
                - Určím tedy rozsah pixelů pro načítané řádky = aktuálně viditelný prostor, mínus přídavek nahoře, plus přídavek dole;
                - Pokud by se takto rozšířený prostor blížil k celému prostoru o nějakých 200 pixelů (nahoře a současně dole), tak radši načteme všechno;
                - A jen v ostatních případech vrátíme určený rozsah pixelů a načte se podmnožina řádků, což vede k trhání při scrollování 
                   (kdy při scrollování narazím na konec přednačtených dat a musím načíst data pro nový úsek)

            */

            // Pokud celý rozsah mých řádků je menší než minimum pro dynamické stránkování, tak vrátím celý rozsah a vytvoří se prvky pro všechny řádky:
            int contentHeight = contentSize.Value.Height;
            if (contentHeight < _MinimalHeightToCreateDynamicItems) return null;

            // Pokud aktuálně viditelná oblast pokrývá relativně větší část z výšky všech řádků, tak vrátím celý rozsah a vytvoří se prvky pro všechny řádky:
            var visibleBounds = this.DataFormPanel.VisibleDesignBounds;                  // Kolik prostoru mám reálně na zobrazení
            double ratio = (double)visibleBounds.Height / (double)contentHeight;         // Jak velkou poměrnou část z celých dat aktuálně zobrazíme v controlu
            if (ratio >= _MinimalRatioToCreateDynamicItems) return null;

            // Máme hodně velká data (hodně řádků * výška layoutu) a/nebo malý prostor: vytvoříme prvky jen pro podmnožinu řádků a následně budeme provádět dynamické scrollování:
            int addition = (int)(_AdditionRatioToDynamicItems * (double)visibleBounds.Height);
            if (addition < _MinimalAdditionHeightForDynamicItems) addition = _MinimalAdditionHeightForDynamicItems;
            int rowBegin = visibleBounds.Top - addition;
            if (rowBegin < 0) rowBegin = 0;
            int rowEnd = visibleBounds.Bottom + addition;
            if (rowEnd > contentHeight) rowEnd = contentHeight;

            // Pokud rowBegin a současně rowEnd jsou téměř u konce, pak načtu vše:
            if (rowBegin <= _MinimalDistanceToEndForDynamicItems && rowEnd >= (contentHeight - _MinimalDistanceToEndForDynamicItems)) return null;

            // Pouze tady budu načítat podmnožinu řádků:
            return new Int32Range(rowBegin, rowEnd, true);
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
        /// Násobek výšky viditelného controlu, pro který se načítají řádky nad rámec reálně viditelného prostoru. Jde o předem načtenou rezervu pro budoucí scrollování.
        /// </summary>
        private const double _AdditionRatioToDynamicItems = 2.5d;
        /// <summary>
        /// Počet přidaných pixelů výšky nad viditelný počátek a pod viditelný konec.
        /// </summary>
        private const int _MinimalAdditionHeightForDynamicItems = 800;
        /// <summary>
        /// Pokud určím dynamický rozsah, kterému k začátku i konci dat chybí tento počet pixelů, tak načtu všechny řádky
        /// </summary>
        private const int _MinimalDistanceToEndForDynamicItems = 200;
        /// <summary>
        /// Invaliduje soupis prvků v <see cref="InteractiveItems"/>.
        /// Podle parametru force: 
        /// true = invalidace bezpodmínečná; 
        /// false (nepovinně) = jen když aktuálně viditelný prostor zobrazuje data, která nejsou připravena (po Scrollu).
        /// </summary>
        /// <param name="force"></param>
        protected void InvalidateInteractiveItems(bool force)
        {
            if (!force && _IsValidPreparedInteractiveRows())
                // Non force (false) => pokud pro aktuální viditelnou oblast v VisibleDesignBounds máme již z dřívějška připraveny prvky, pak je nebudu invalidovat:
                return;
           
            // Musíme zahodit připravená data:
            __InteractiveItems = null;
            __InteractiveItemsPreparedDesignPixels = null;
            DxComponent.LogAddLine(LogActivityKind.DataFormRepository, $"DxDataFormPanel.InteractiveItemsInvalidate(): Invalidated items.");

            this.DataFormContent?.ItemsAllChanged();
        }
        /// <summary>
        /// Provede invalidaci cache bitmap v připravených interaktivních prvcích <see cref="InteractiveItems"/>.
        /// Provádí se po změně Skinu a/nebo Zoomu.
        /// Následující vykreslení obsahu vytvoří nové cachované bitmapy již s novým skinem.
        /// </summary>
        protected void InvalidateInteractiveImageCache()
        {
            if (__InteractiveItems is null) return;
            foreach (var item in __InteractiveItems.OfType<IPaintItemData>())
                item.InvalidateCache();
        }
        /// <summary>
        /// Vrátí true, pokud máme připraveny prvky pro aktuálně viditelnou oblast. false pokud prvky nejsou připraveny vůbec, anebo nepokrývají viditelnou oblast.
        /// </summary>
        /// <returns></returns>
        private bool _IsValidPreparedInteractiveRows()
        {
            var preparedDesignPixels = __InteractiveItemsPreparedDesignPixels;
            if (!HasDataFormPanel || __InteractiveItems is null || preparedDesignPixels is null)
            {
                DxComponent.LogAddLine(LogActivityKind.DataFormRepository, $"DxDataFormPanel.IsValidInteractiveRows(): data is null => Invalid");
                return false;                                                  // Nejsou-li data, pak nejsme validní.
            }

            // Aktuálně zobrazená oblast = je daná zobrazovačem:
            var visibleBounds = this.DataFormPanel.VisibleDesignBounds;
            if (visibleBounds.Height <= 0) 
            {
                return true;                                                   // Nic není viditelno: pak jsme OK.
            }

            // visibleBounds může být i větší, než je velikost dat (může zobrazovat prostor dole pod koncem reálných dat).
            // To by bez korekcí potom vedlo k tomu, že bychom pro ten malý dolní proužek nikdy neměli validní data, protože je nemáme z čeho připravit.
            // Proto určíme hodnoty VisibleDataBounds = to, co vidíme, a současně to obsahuje data:
            var designSize = this.ContentDesignSize;
            if (!designSize.HasValue)
            {
                DxComponent.LogAddLine(LogActivityKind.DataFormRepository, $"DxDataFormPanel.IsValidInteractiveRows(): ContentDesignSize is null => Valid");
                return true;                                                   // Nejsou-li data, pak jsme validní.
            }
            // Pokud by VisibleDesignBounds mělo z nějakého důvodu záporný Top, pak to neakceptujeme, data řádků máme počínaje od 0;
            int visibleDataTop = (visibleBounds.Top < 0 ? 0 : visibleBounds.Top);
            // Pokud Bottom viditelné oblasti je větší než Height existujících dat, pak jako reálný Bottom dáme konec našich dat:
            int visibleDataBottom = (visibleBounds.Bottom > designSize.Value.Height ? designSize.Value.Height : visibleBounds.Bottom);

            // Pokud viditelná oblast se nachází uvnitř oblasti, kterou máme připravenou od dřívějška, pak si data neinvalidujeme.
            // Jde o malý posun a my jsme si moudře připravili interaktivní prvky pro větší oblast, než bylo nutno, takže nyní nemusíme zahazovat a generovat vše...
            if (visibleDataTop >= preparedDesignPixels.Begin && visibleDataBottom <= preparedDesignPixels.End)
            {
                // DxComponent.LogAddLine($"DxDataFormPanel.IsValidInteractiveRows(): VisibleBounds is in PreparedDesignPixels =>  Valid");
                return true;                                                   // Viditelná oblast se nachází uvnitř oblasti, pro kterou jsou připravena data
            }
            DxComponent.LogAddLine(LogActivityKind.DataFormRepository, $"DxDataFormPanel.IsValidInteractiveRows(): VisibleBounds: {visibleDataTop}÷{visibleDataBottom}; PreparedPixels: {preparedDesignPixels.Begin}÷{preparedDesignPixels.End} => INVALID");
            return false;
        }
        /// <summary>
        /// Pole řádků, jejichž prvky jsou načteny v <see cref="__InteractiveItems"/>. Často jsou to všechny řádky z <see cref="DataFormRows"/>.
        /// </summary>
        private DxfData.DataFormRow[] __VisibleRows;
        /// <summary>
        /// Interaktivní data = jednotlivé prvky, platné pro aktuální layout a řádky a pozici Scrollbaru. Úložiště.
        /// Pokrývá oblast na ose Y v rozsahu <see cref="__InteractiveItemsPreparedDesignPixels"/>.
        /// </summary>
        private List<IInteractiveItem> __InteractiveItems;
        /// <summary>
        /// Rozsah designových pixelů od prvního do posledního řádku, který má připravená svoje data v <see cref="__InteractiveItems"/>.
        /// Zde je využit i příznak IsVariable, který říká: 
        /// IsVariable = true: jsem proměnný interval, zobrazuji jen část řádků, při scrollování je třeba provádět reload interaktivních prvků;
        /// IsVariable = false: jsem konstantní interval, zobrazuji všechny řádky. 
        /// </summary>
        private Int32Range __InteractiveItemsPreparedDesignPixels;
        #endregion
        #region Akce uživatele na DataFormu
        /// <summary>
        /// Uživatel provedl nějakou akci na dataformu (kliknutí...)
        /// </summary>
        /// <param name="actionInfo"></param>
        internal void OnInteractiveAction(DataFormActionInfo actionInfo)
        {
            string text = actionInfo.ToString();
            DxComponent.LogAddLine(LogActivityKind.DataFormEvents, text);

            switch (actionInfo.Action)
            {
                case DxDataFormAction.KeyDown: _OnInteractiveActionKeyDown(actionInfo as DataFormKeyActionInfo); break;
            }
        }
        /// <summary>
        /// Interaktivní akce: <see cref="DxDataFormAction.KeyDown"/>.
        /// Určité klávesy mohou změnit Focus.
        /// </summary>
        /// <param name="actionInfo"></param>
        private void _OnInteractiveActionKeyDown(DataFormKeyActionInfo actionInfo)
        {

        }
        /// <summary>
        /// Dataform vrací true, pokud chce dostat akci <see cref="DxDataFormAction.KeyDown"/> o stisku dané klávesy.
        /// Typicky chce dostávat jen klávesy Tab a ShiftTab a Enter = pro pohyb focusu po formuláři.
        /// <para/>
        /// Pokud zdejší metoda vrátí true, pak teprve bude volána metoda <see cref="OnInteractiveAction(DataFormActionInfo)"/> s kompletními daty (včetně dat o buňce),
        /// s akcí <see cref="DxDataFormAction.KeyDown"/>, /// kde bude reálně vyhodnocen požadavek na změnu focusu.<br/>
        /// Tady jen říkáme: "Když je to klávesa TAB, mohlo by nás to zajímat".
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal bool NeedTraceKeyDown(WinForm.Keys key)
        {
            return (key == WinForm.Keys.Tab || key == (WinForm.Keys.Tab | WinForm.Keys.Shift) || key == WinForm.Keys.Enter);
        }
        #endregion
    }
    #endregion
    #region Podpůrné datové třídy
    #region Argumenty DataFormActionInfo pro událost DxDataForm.OnInteractiveAction()
    /// <summary>
    /// Data pro akce typu Změna hodnoty s možností Cancel
    /// </summary>
    internal class DataFormValueChangingInfo : DataFormValueChangedInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="cell">Buňka, kde došlo k akci</param>
        /// <param name="action"></param>
        /// <param name="originalValue"></param>
        /// <param name="currentValue"></param>
        internal DataFormValueChangingInfo(DataFormCell cell, DxDataFormAction action, object originalValue, object currentValue)
            : base(cell, action, originalValue, currentValue)
        {
            this.Cancel = false;
        }
        /// <summary>
        /// DataForm požaduje storno editace
        /// </summary>
        internal bool Cancel { get; set; }
    }
    /// <summary>
    /// Data pro akce typu Změna hodnoty bez možnost Cancel
    /// </summary>
    internal class DataFormValueChangedInfo : DataFormActionInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="cell">Buňka, kde došlo k akci</param>
        /// <param name="action"></param>
        /// <param name="originalValue"></param>
        /// <param name="currentValue"></param>
        internal DataFormValueChangedInfo(DataFormCell cell, DxDataFormAction action, object originalValue, object currentValue)
            : base(cell, action)
        {
            this.OriginalValue = originalValue;
            this.CurrentValue = currentValue;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{base.ToString()}; OldValue: '{OriginalValue}'; NewValue: '{CurrentValue}'";
        }
        /// <summary>
        /// Hodnota v okamžiku vstupu kurzoru do prvku
        /// </summary>
        internal object OriginalValue { get; private set; }
        /// <summary>
        /// Hodnota v okamžiku ukončení editace
        /// </summary>
        internal object CurrentValue { get; private set; }
    }
    /// <summary>
    /// Data pro akce, které nesou informaci o stisknuté klávese v akci <see cref="DxDataFormAction.KeyDown"/>
    /// </summary>
    internal class DataFormKeyActionInfo : DataFormActionInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="cell">Buňka, kde došlo k akci</param>
        /// <param name="action"></param>
        /// <param name="keyArgs"></param>
        internal DataFormKeyActionInfo(DataFormCell cell, DxDataFormAction action, WinForm.KeyEventArgs keyArgs)
            : base(cell, action)
        {
            this.KeyArgs = keyArgs;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{base.ToString()}'; KeyData: '{this.KeyArgs.KeyData}'; Modifiers: '{this.KeyArgs.Modifiers}'";
        }
        /// <summary>
        /// Stisknutá klávesa
        /// </summary>
        internal WinForm.KeyEventArgs KeyArgs { get; private set; }
    }
    /// <summary>
    /// Data pro akce, které nesou informaci o stavu myši a modifikačních kláves
    /// </summary>
    internal class DataFormMouseActionInfo : DataFormActionInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="cell">Buňka, kde došlo k akci</param>
        /// <param name="action">Akce</param>
        /// <param name="modifierKeys"></param>
        /// <param name="mouseButtons"></param>
        /// <param name="mouseAbsoluteLocation"></param>
        internal DataFormMouseActionInfo(DataFormCell cell, DxDataFormAction action, WinForm.Keys modifierKeys, WinForm.MouseButtons mouseButtons, WinDraw.Point mouseAbsoluteLocation)
            : base(cell, action)
        {
            this.ModifierKeys = modifierKeys;
            this.MouseButtons = mouseButtons;
            this.MouseAbsoluteLocation = mouseAbsoluteLocation;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{base.ToString()}'; ModifierKeys: '{this.ModifierKeys}'; MouseButtons: '{this.MouseButtons}'; MouseAbsoluteLocation: '{this.MouseAbsoluteLocation}'";
        }
        /// <summary>
        /// Modifikační klávesy (Ctrl, Shift, Alt)
        /// </summary>
        internal WinForm.Keys ModifierKeys { get; private set; }
        /// <summary>
        /// Absolutní pozice myši
        /// </summary>
        internal WinDraw.Point MouseAbsoluteLocation { get; private set; }
        /// <summary>
        /// Aktuální tlačítka Buttonu
        /// </summary>
        internal WinForm.MouseButtons MouseButtons { get; private set; }
    }
    /// <summary>
    /// Data pro akce, které nesou název prvku (typicky SubButton)
    /// </summary>
    internal class DataFormItemNameInfo : DataFormActionInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="cell">Buňka, kde došlo k akci</param>
        /// <param name="action"></param>
        /// <param name="itemName"></param>
        internal DataFormItemNameInfo(DataFormCell cell, DxDataFormAction action, string itemName)
            : base(cell, action)
        {
            this.ItemName = itemName;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{base.ToString()}'; ItemName: '{this.ItemName}'";
        }
        /// <summary>
        /// Prvek, jeho název
        /// </summary>
        internal string ItemName { get; private set; }
    }
    /// <summary>
    /// Data pro akce, které nenesou žádná další data
    /// </summary>
    internal class DataFormActionInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="cell">Buňka, kde došlo k akci</param>
        /// <param name="action"></param>
        internal DataFormActionInfo(DataFormCell cell, DxDataFormAction action)
        {
            this.Cell = cell;
            this.Action = action;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Action '{this.Action}'; Row: {this.Row.RowId}; Column: '{this.ColumnName}'";
        }
        /// <summary>
        /// Buňka, kde došlo k akci
        /// </summary>
        internal DataFormCell Cell { get; private set; }
        /// <summary>
        /// Řádek, kde došlo k události
        /// </summary>
        internal DataFormRow Row { get { return Cell?.Row; } }
        /// <summary>
        /// Definice layoutu
        /// </summary>
        internal DataFormLayoutItem LayoutItem { get { return Cell?.LayoutItem; } }
        /// <summary>
        /// Sloupec, kde došlo k události
        /// </summary>
        internal string ColumnName { get { return Cell?.ColumnName; } }
        /// <summary>
        /// Druh události
        /// </summary>
        internal DxDataFormAction Action { get; private set; }
    }
    #endregion
    #endregion
}
