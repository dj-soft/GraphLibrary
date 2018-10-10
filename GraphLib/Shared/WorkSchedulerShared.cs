// Supervisor: DAJ
// Part of Helios Green, proprietary software, (c) LCS International, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with LCS International, a. s. 
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Tento soubor obsahuje sadu tříd a enumů, které popisují data pro plugin WorkScheduler. Tato data se plní v Greenu a přes XML persistor se předávají do pluginu WorkScheduler.
// Tento soubor se nachází jednak v Greenu: Noris\App\Lcs\Base\WorkSchedulerShared.cs, a zcela identický i v GraphLibrary: \GraphLib\Shared\WorkSchedulerShared.cs
namespace Noris.LCS.Base.WorkScheduler
{
    #region GuiData : data předávaná mezi Helios Green a WorkScheduler
    #region GuiData : Kompletní datový balík, jehož data budou zobrazena v pluginu ASOL.WorkScheduler
    /// <summary>
    /// GuiData : Kompletní datový balík, jehož data budou zobrazena v pluginu ASOL.WorkScheduler.
    /// Obsahuje jak definice všech dat, tak i jejich obsah.
    /// Datový balík se vytvoří na aplikačním serveru, projde serializací a odejde do pluginu, kde se deserializuje a použije jako zdroj dat pro vykreslení.
    /// <para/>
    /// Po vytvoření datového balíku (tj. po jeho naplnění daty) je nutno zavolat jeho metodu <see cref="Finalise()"/>, která zajistí, 
    /// že všechny vložené objekty dostanou referenci na svého <see cref="IGuiItem.Parent"/>. Metodu <see cref="Finalise()"/> je možno volat i opakovaně,
    /// například po doplnění dalších prvků do objektu.
    /// <para/>
    /// Každý prvek v celé hierarchii má svoje property <see cref="IGuiItem.Name"/>, <see cref="IGuiItem.Parent"/> 
    /// a na jejich základě i <see cref="GuiBase.FullName"/>, které obsahuje jednoznačné a úplné jméno prvku od root prvku (<see cref="GuiData"/>).
    /// Pod tímto jménem jej lze vyhledat pomocí metody <see cref="FindByFullName(string)"/>.
    /// <para/>
    /// Všechny třídy jsou sealed, aby aplikaci nebylo povoleno používat vlastní potomky namísto tříd dodaných.
    /// Důvod je jednoduchý, konkrétní třídy musí existovat jak v aplikaci, tak na straně WorkScheduler;
    /// a tam by uživatelem definované třídy nebylo možno deserializovat.
    /// </summary>
    public sealed class GuiData : GuiBase, IXmlPersistNotify
    {
        #region Konstrukce a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiData()
        {
            this.Name = DATA_NAME;
            this.Properties = new GuiProperties() { Name = PROPERTIES_NAME };
            this.ToolbarItems = new GuiToolbarPanel() { Name = TOOLBAR_NAME };
            this.Pages = new GuiPages() { Name = PAGES_NAME };
            this.ContextMenuItems = new GuiContextMenuSet() { Name = CONTEXT_MENU_NAME };
        }
        /// <summary>
        /// Výchozí název celého objektu <see cref="GuiData"/>
        /// </summary>
        public const string DATA_NAME = "Data";
        /// <summary>
        /// Název prvku <see cref="Properties"/>
        /// </summary>
        public const string PROPERTIES_NAME = "properties";
        /// <summary>
        /// Název prvku <see cref="ToolbarItems"/>
        /// </summary>
        public const string TOOLBAR_NAME = "toolBar";
        /// <summary>
        /// Název prvku <see cref="Pages"/>
        /// </summary>
        public const string PAGES_NAME = "pages";
        /// <summary>
        /// Název prvku <see cref="ContextMenuItems"/>
        /// </summary>
        public const string CONTEXT_MENU_NAME = "contextMenu";
        /// <summary>
        /// Základní společné vlastnosti pro celý plugin
        /// </summary>
        public GuiProperties Properties { get; set; }
        /// <summary>
        /// Prvky zobrazené v Toolbaru nahoře
        /// </summary>
        public GuiToolbarPanel ToolbarItems { get; set; }
        /// <summary>
        /// Jednotlivé stránky (záložky) obsahující kompletní GUI
        /// </summary>
        public GuiPages Pages { get; set; }
        /// <summary>
        /// Definice všech kontextových funkcí.
        /// GUI si z nich pro konkrétní situaci vybere jen položky odpovídající jejich definici (shoda stránky, shoda panelu, shoda tabulky, shoda třídy prvku)
        /// </summary>
        public GuiContextMenuSet ContextMenuItems { get; set; }
        /// <summary>
        /// Potomek zde vrací soupis svých Child prvků
        /// </summary>
        [PersistingEnabled(false)]
        protected override IEnumerable<IGuiItem> Childs { get { return Union(this.ToolbarItems, this.Pages, this.ContextMenuItems); } }
        #endregion
        #region Finalizace (ruční, i po deserializaci); Vyhledání prvku podle FullName
        /// <summary>
        /// Metoda zajistí, že všichni členové tohoto balíku dat budou mít přísup ke svému parentovi.
        /// Tím dojde i k tomu, že každý prvek datového balíku bude mít platnou hodnotu ve své property <see cref="GuiBase.FullName"/>.
        /// </summary>
        public void FillParents()
        {
            this.FillParentToChilds();
        }
        /// <summary>
        /// Metoda provede kompletní finalizaci dat v objektu.
        /// Aktuálně provede pouze metodu <see cref="FillParents()"/>.
        /// </summary>
        public void Finalise()
        {
            this.FillParents();
        }
        /// <summary>
        /// Metoda najde první vyhovující prvek na základě jeho plného jména.
        /// Pokud by více prvků v jedné úrovni mělo shodné jméno (což je prakticky přípustné), pak se pracuje jen s prvním z nich.
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        public IGuiItem FindByFullName(string fullName)
        {
            return this.FindByName(fullName);
        }
        /// <summary>
        /// Aktuální stav procesu XML persistence.
        /// Umožňuje persistovanému objektu reagovat na ukládání nebo na načítání dat.
        /// Do této property vkládá XmlPersistor hodnotu odpovídající aktuální situaci.
        /// Datová instance může v set accessoru zareagovat a například připravit data pro Save, 
        /// anebo dokončit proces Load (navázat si další data nebo provést přepočty a další reakce).
        /// V procesu serializace (ukládání dat z objektu to XML) bude do property <see cref="IXmlPersistNotify.XmlPersistState"/> vložena hodnota <see cref="XmlPersistState.SaveBegin"/> a po dokončení ukládán ípak hodnota <see cref="XmlPersistState.SaveDone"/> a <see cref="XmlPersistState.None"/>.
        /// Obdobně při načítání dat z XML do objektu bude do property <see cref="IXmlPersistNotify.XmlPersistState"/> vložena hodnota <see cref="XmlPersistState.LoadBegin"/> a po dokončení načítání pak hodnota <see cref="XmlPersistState.LoadDone"/> a <see cref="XmlPersistState.None"/>.
        /// </summary>
        [PersistingEnabled(false)]
        XmlPersistState IXmlPersistNotify.XmlPersistState
        {
            get { return this._XmlPersistState; }
            set
            {
                switch (value)
                {
                    case XmlPersistState.LoadDone:
                        // Po ukončení načítání všech dat (tzn. i vnořených) se provede finalizace:
                        this.Finalise();
                        break;
                }
                this._XmlPersistState = value;
            }
        }
        /// <summary>
        /// Stav procesu persistování
        /// </summary>
        [PersistingEnabled(false)]
        private XmlPersistState _XmlPersistState;

        #endregion
    }
    #endregion
    #region GuiProperties : Základní společné vlastnosti pro celý plugin
    /// <summary>
    /// GuiProperties : Základní společné vlastnosti pro celý plugin
    /// </summary>
    public class GuiProperties : GuiBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiProperties()
        {
            this.PluginFormTitle = "WorkScheduler plugin";
            this.PluginFormBorder = PluginFormBorderStyle.SizableToolWindow;
            this.PluginFormIsMaximized = true;
            this.TotalTimeRange = TotalTimeRangeDefault;
            this.InitialTimeRange = InitialTimeRangeDefault;
        }
        /// <summary>
        /// Titulek okna pluginu.
        /// Výchozí hodnota = "WorkScheduler plugin".
        /// </summary>
        public string PluginFormTitle { get; set; }
        /// <summary>
        /// Typ okrajů okna pluginu.
        /// Výchozí hodnota = <see cref="PluginFormBorderStyle.SizableToolWindow"/>.
        /// </summary>
        public PluginFormBorderStyle PluginFormBorder { get; set; }
        /// <summary>
        /// Plugin bude při otevření maximalizován.
        /// Výchozí hodnota = true
        /// </summary>
        public bool PluginFormIsMaximized { get; set; }
        /// <summary>
        /// Celkový dostupný časový interval. Časy mimo interval nebude možno zobrazit.
        /// Výchozí hodnota: -1 rok až +5 roků
        /// </summary>
        public GuiTimeRange TotalTimeRange { get; set; }
        /// <summary>
        /// Výchozí časový interval, zobrazený po startu pluginu.
        /// Výchozí hodnota: aktuální týden od pondělí do neděle, +- 8 hodin
        /// </summary>
        public GuiTimeRange InitialTimeRange { get; set; }
        /// <summary>
        /// Způsob umístění prvku grafu při jeho přetahování (Drag and Drop), na ose Y, v rámci původního grafu (odkud prvek pochází)
        /// </summary>
        public GraphItemMoveAlignY GraphItemMoveSameGraph { get; set; }
        /// <summary>
        /// Způsob umístění prvku grafu při jeho přetahování (Drag and Drop), na ose Y, v rámci jiného než původního grafu.
        /// Zde se nemá používat hodnota <see cref="GraphItemMoveAlignY.OnOriginalItemPosition"/>.
        /// </summary>
        public GraphItemMoveAlignY GraphItemMoveOtherGraph { get; set; }
        /// <summary>
        /// Defaultní časový interval pro <see cref="TotalTimeRange"/>
        /// </summary>
        public static GuiTimeRange TotalTimeRangeDefault
        {
            get
            {
                DateTime now = DateTime.Now;
                DateTime begin = new DateTime(now.Year - 1, 1, 1);
                DateTime end = begin.AddYears(6);
                return new GuiTimeRange(begin, end);
            }
        }
        /// <summary>
        /// Defaultní časový interval pro <see cref="InitialTimeRange"/>
        /// </summary>
        public static GuiTimeRange InitialTimeRangeDefault
        {
            get
            {
                DateTime now = DateTime.Now;
                int dow = (now.DayOfWeek == DayOfWeek.Sunday ? 6 : ((int)now.DayOfWeek) - 1);
                DateTime begin = new DateTime(now.Year, now.Month, now.Day).AddDays(-dow);
                DateTime end = begin.AddDays(7d);
                double add = 6d;
                return new GuiTimeRange(begin.AddHours(-add), end.AddHours(add));
            }
        }
    }
    #endregion
    #region GuiPages : kompletní sada stránek (GuiPage) s daty
    /// <summary>
    /// GuiPages : kompletní sada stránek (GuiPage) s daty
    /// </summary>
    public sealed class GuiPages : GuiBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiPages()
        {
            this.ShowPageTitleAllways = true;
            this.Pages = new List<GuiPage>();
        }
        /// <summary>
        /// Zobrazovat titulkový řádek nad jednotlivými <see cref="GuiPage"/> vždy = i když je jen jedna?
        /// </summary>
        public bool ShowPageTitleAllways { get; set; }
        /// <summary>
        /// Sada stránek s daty
        /// </summary>
        public List<GuiPage> Pages { get; set; }
        /// <summary>
        /// Přidá další prvek do this seznamu
        /// </summary>
        /// <param name="item"></param>
        public void Add(GuiPage item) { this.Pages.Add(item); }
        /// <summary>
        /// Přidá další prvky do this seznamu
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<GuiPage> items) { this.Pages.AddRange(items); }
        /// <summary>
        /// Počet prvků v kolekci
        /// </summary>
        public int Count { get { return this.Pages.Count; } }
        /// <summary>
        /// Obsahuje prvek na daném indexu
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GuiPage this[int index] { get { return this.Pages[index]; } }
        /// <summary>
        /// Potomek zde vrací soupis svých Child prvků
        /// </summary>
        [PersistingEnabled(false)]
        protected override IEnumerable<IGuiItem> Childs { get { return this.Pages; } }
    }
    #endregion
    #region GuiPage : Jedna úplná stránka s daty, obsahuje kompletní editační GUI vyjma ToolBaru; stránek může být více vedle sebe (na záložkách)
    /// <summary>
    /// GuiPage : Jedna úplná stránka s daty, obsahuje kompletní editační GUI vyjma ToolBaru; stránek může být více vedle sebe (na záložkách).
    /// Stránka obsahuje jednotlivé panely (levý, hlavní, pravý, dolní), a textové popisky.
    /// </summary>
    public sealed class GuiPage : GuiTextItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiPage()
        {
            this.LeftPanel = new GuiPanel() { Name = LEFT_PANEL_NAME };
            this.MainPanel = new GuiPanel() { Name = MAIN_PANEL_NAME };
            this.RightPanel = new GuiPanel() { Name = RIGHT_PANEL_NAME };
            this.BottomPanel = new GuiPanel() { Name = BOTTOM_PANEL_NAME };
        }
        /// <summary>
        /// Název prvku <see cref="LeftPanel"/>
        /// </summary>
        public const string LEFT_PANEL_NAME = "leftPanel";
        /// <summary>
        /// Název prvku <see cref="MainPanel"/>
        /// </summary>
        public const string MAIN_PANEL_NAME = "mainPanel";
        /// <summary>
        /// Název prvku <see cref="RightPanel"/>
        /// </summary>
        public const string RIGHT_PANEL_NAME = "rightPanel";
        /// <summary>
        /// Název prvku <see cref="BottomPanel"/>
        /// </summary>
        public const string BOTTOM_PANEL_NAME = "bottomPanel";
        /// <summary>
        /// Levý panel, typicky používaný pro úkoly ("co se má udělat")
        /// </summary>
        public GuiPanel LeftPanel { get; private set; }
        /// <summary>
        /// Střední panel, typicky používaný pro hlavní plochu rozmisťování práce
        /// </summary>
        public GuiPanel MainPanel { get; private set; }
        /// <summary>
        /// Levý panel, typicky používaný pro zdroje ("kdo to může dělat, co můžeme použít")
        /// </summary>
        public GuiPanel RightPanel { get; private set; }
        /// <summary>
        /// Střední panel, typicky používaný pro informace (detailní data, výsledky, problémy)
        /// </summary>
        public GuiPanel BottomPanel { get; private set; }
        /// <summary>
        /// Potomek zde vrací soupis svých Child prvků
        /// </summary>
        [PersistingEnabled(false)]
        protected override IEnumerable<IGuiItem> Childs { get { return Union(this.LeftPanel, this.MainPanel, this.RightPanel, this.BottomPanel); } }
    }
    #endregion
    #region GuiPanel : Obsah jednoho panelu s více tabulkami
    /// <summary>
    /// GuiPanel : Obsah jednoho panelu s více tabulkami
    /// </summary>
    public sealed class GuiPanel : GuiBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiPanel()
        {
            this.Grids = new List<GuiGrid>();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "{ empty panel }";
            if (this.Grids != null && this.Grids.Count > 0)
            {
                text = "|";
                foreach (GuiGrid grid in this.Grids)
                {
                    text += " " + grid.Title + " |";
                }
            }
            return text;
        }
        /// <summary>
        /// Tabulky zobrazené v tomto panelu.
        /// <para/>
        /// Pokud nebude žádná (výchozí stav), pak panel nebude zobrazen.
        /// <para/>
        /// Pokud jich bude více, bude vždy zobrazena lišta se záhlavími tabulek (záložky = TabPage), kde budou uvedeny popisy z tabulky (Title, Image, ToolTip).
        /// <para/>
        /// Pokud bude jen jedna tabulka, pak zobrazení lišty řídí proměnná <see cref="ShowTableTitleAllways"/>.
        /// <para/>
        /// Lišta může obshovat i tlačítko "Minimalizovat", jeho zobrazení řídí proměnná <see cref="ShowMinimizeButton"/>.
        /// </summary>
        public List<GuiGrid> Grids { get; set; }
        /// <summary>
        /// Zobrazovat titulkový řádek nad jednotlivými <see cref="GuiTable"/> vždy = i když je jen jedna?
        /// </summary>
        public bool ShowTableTitleAllways { get; set; }
        /// <summary>
        /// Zobrazovat tlačítko "Minimalizovat" na liště tabulek.
        /// </summary>
        public bool ShowMinimizeButton { get; set; }
        /// <summary>
        /// Potomek zde vrací soupis svých Child prvků
        /// </summary>
        [PersistingEnabled(false)]
        protected override IEnumerable<IGuiItem> Childs { get { return this.Grids; } }
    }
    #endregion
    #region GuiGrid : obsahuje veškeré data pro zobrazení jedné tabulky v WorkScheduler pluginu (Rows, Graph, Text).
    /// <summary>
    /// GuiGrid : obsahuje veškeré data pro zobrazení jedné tabulky v WorkScheduler pluginu.
    /// Obsahuje řádky <see cref="Rows"/>, obsahuje jednotlivé prvky časových grafů <see cref="GraphItems"/>, a obsahuje textové popisky k těmto grafům <see cref="GraphTexts"/>.
    /// Obsahuje i zadání vlastností grafu <see cref="GraphProperties"/>, a textové popisky tabulky (Name, Title, ToolTip, Image).
    /// Tyto prvky jsou zde uloženy takříkajíc na hromadě, bez nějakého ladu a skladu.
    /// Teprve až WorkScheduler si je probere a poskládá z nich vizuální tabulku a do ní vloží patřičné grafy.
    /// </summary>
    public sealed class GuiGrid : GuiTextItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiGrid()
        {
            this.GraphProperties = new GuiGraphProperties() { Name = GRAPH_PROPERTIES_NAME };
            this.GraphItems = new List<GuiGraphTable>();
            this.GraphTexts = new List<GuiTable>();
        }
        /// <summary>
        /// Název prvku <see cref="GraphProperties"/>
        /// </summary>
        public const string GRAPH_PROPERTIES_NAME = "graphProperties";
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "{ empty grid }";
            if (this.Rows != null)
                text = "Rows: " + this.Rows.ToString();
            return text;
        }
        /// <summary>
        /// Definuje vlastnosti grafu pro tuto tabulku
        /// </summary>
        public GuiGraphProperties GraphProperties { get; set; }
        /// <summary>
        /// Tabulka s řádky, typicky načtená dle přehledové šablony
        /// </summary>
        public GuiTable Rows { get; set; }
        /// <summary>
        /// Tagy k řádkům v tabulce <see cref="Rows"/>.
        /// Tagy řádku dovolují k jednomu řádku připojit { 0 - 1 - mnoho } textových popisků (visačky = Tagy), a následně podle nich zafiltrovat řádky.
        /// </summary>
        public GuiTagItems RowTags { get; set; }

        /// <summary>
        /// Tabulky s grafickými prvky.
        /// Jedna vizuální tabulka může v grafech zobrazovat prvky, pocházející z různých zdrojů.
        /// V této property tak může být více tabulek třídy <see cref="GuiGraphTable"/>, kde každá tabulka obsahuje typicky prvky grafů z jednoho konkrétního zdroje.
        /// </summary>
        public List<GuiGraphTable> GraphItems { get; set; }
        /// <summary>
        /// Tabulky s popisnými texty pro položky grafu, typicky načtená dle přehledové šablony.
        /// Tabulek je možno vložit více, každá tabulka může obsahovat přehledovou šablonu jiné třídy nebo s jiným filtrem.
        /// Konkrétní řádek se dohledává podle GuiId grafického prvku, který se vyhledává v těchto tabulkách.
        /// </summary>
        public List<GuiTable> GraphTexts { get; set; }
        /// <summary>
        /// Potomek zde vrací soupis svých Child prvků
        /// </summary>
        [PersistingEnabled(false)]
        protected override IEnumerable<IGuiItem> Childs { get { return Union(this.Rows, this.GraphItems, this.GraphTexts); } }
    }
    #endregion
    #region GuiTable : Jedna fyzická tabulka (ekvivalent DataTable, s podporou serializace a implicitní konverze z/na DataTable)
    /// <summary>
    /// GuiTable : Jedna fyzická tabulka (ekvivalent DataTable, s podporou serializace a implicitní konverze z/na DataTable)
    /// </summary>
    public sealed class GuiTable : GuiBase
    {
        #region Standardní public properties
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiTable()
        { }
        /// <summary>
        /// Vizualiace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "{ empty table }";
            System.Data.DataTable table = this.DataTable;
            if (table != null || table.Columns.Count > 0)
            {
                text = "|";
                foreach (System.Data.DataColumn column in table.Columns)
                    text += " " + column.ColumnName + " |";
                text += " ... " + table.Rows.Count.ToString() + " rows";
            }
            return text;
        }
        /// <summary>
        /// Serializovaná data tabulky
        /// </summary>
        [PersistingEnabled(false)]
        public string DataSerial
        {
            get
            {
                if (!this._DataSerialValid && this._DataTableValid)
                {
                    this._DataSerial = WorkSchedulerSupport.TableSerialize(this._DataTable);
                    this._DataSerialValid = true;
                }
                return this._DataSerial;
            }
            set
            {
                this._DataSerial = value;
                this._DataSerialValid = true;
                this._DataTableValid = false;
                this._ColumnsExtendedInfo = null;
            }
        }
        /// <summary>
        /// Obsahuje serializovaná data tabulky
        /// </summary>
        private string _DataSerial = null;
        /// <summary>
        /// Obsahuje true, pokud jsou data v <see cref="_DataSerial"/> platná
        /// </summary>
        private bool _DataSerialValid = false;
        /// <summary>
        /// Nativní data tabulky
        /// </summary>
        [PersistingEnabled(false)]
        public System.Data.DataTable DataTable
        {
            get
            {
                if (!this._DataTableValid && this._DataSerialValid)
                {
                    this._DataTable = WorkSchedulerSupport.TableDeserialize(this._DataSerial);
                    this._DataTableValid = true;
                }
                // Tento krok zajistí, že serializovaný obsah (DataSerial) bude od teď pokládán za neplatný.
                // Proč? 
                //   Když už někdo čte obsah DataTable, tak jej následně může modifikovat - přidat/odebrat řádek, změnit hodnotu, atd - a my se to nedozvíme.
                //   Není ani naším cílem se to dozvědět, to by byla dost zbytečná komplikace.
                // Takže pokud poté, co si někdo vyzvedl obsah DataTable (a možná ho modifikoval), a někdo pak bude číst serializovaný obsah (DataSerial),
                //   tak tento DataSerial vytvoříme nový, platný z aktuální DataTable.
                // Reverzní postup neděláme - že bychom po načtení DataSerial invalidovali DataTable, protože čtením stringu se nemůže nijak změnit jeho obsah.
                this._DataSerialValid = false;
                return this._DataTable;
            }
            set
            {
                this._DataTable = value;
                this._DataTableValid = true;
                this._DataSerialValid = false;
                this._ColumnsExtendedInfo = null;
            }
        }
        /// <summary>
        /// Obsahuje nativní data tabulky
        /// </summary>
        private System.Data.DataTable _DataTable = null;
        /// <summary>
        /// Obsahuje true, pokud jsou data v <see cref="_DataTable"/> platná
        /// </summary>
        private bool _DataTableValid = false;
        /// <summary>
        /// Tato property je zde kvůli persistenci (serializaci).
        /// { get } vždy vrací platná serializovaná data (primárně z <see cref="DataTable"/> pokud tam jsou, sekundárně z <see cref="DataSerial"/>).
        /// { set } vloží serializovaná data do <see cref="DataSerial"/>, tím zajistí i jejich on-demand promítnutí do <see cref="DataTable"/>.
        /// </summary>
        [PersistingEnabled(true)]
        [PropertyName("Data")]
        private string PersistedData
        {
            get { return this.DataSerial; }
            set { this.DataSerial = value; }
        }
        /// <summary>
        /// Obsahuje rozšiřující informace o sloupcích. Pokud <see cref="DataTable"/> je null, pak i <see cref="ColumnsExtendedInfo"/> je null.
        /// <para/>
        /// Typický postup používání:
        /// <para/>
        /// Pro získání <see cref="GuiTable"/> s daty z SQL SELECTU a následné doplnění dat do <see cref="DataColumnExtendedInfo"/>:
        /// <para/>
        /// <code>
        /// GuiTable guiTable = SqlSelect.
        /// </code>
        /// </summary>
        /// <remarks>
        /// Třída <see cref="DataColumnExtendedInfo"/> vždycky čte i ukládá data přímo do <see cref="System.Data.DataColumn.ExtendedProperties"/>, 
        /// do sloupce pro který je konkrétní instance <see cref="DataColumnExtendedInfo"/> vytvořena.
        /// Pokud uživatel vloží nějakou novou DataTable, pak je <see cref="ColumnsExtendedInfo"/> invalidováno (vloženo null) a znovu bude vytvořeno on demand pro nové sloupce.
        /// Data vložená do ExtendedProperties se tím neztrácejí.
        /// </remarks>
        [PersistingEnabled(false)]
        public DataColumnsExtendedInfo ColumnsExtendedInfo
        {
            get
            {
                if (this._ColumnsExtendedInfo == null)
                {
                    System.Data.DataTable dataTable = this.DataTable;
                    if (dataTable != null)
                        this._ColumnsExtendedInfo = DataColumnsExtendedInfo.CreateForTable(dataTable);
                }
                this._DataSerial = null;
                return this._ColumnsExtendedInfo;
            }
        }
        private DataColumnsExtendedInfo _ColumnsExtendedInfo;
        #endregion
        #region Implicitní konverze GuiTable <==> System.Data.DataTable
        /// <summary>
        /// Implicitní konverze z <see cref="GuiTable"/> na <see cref="System.Data.DataTable"/>.
        /// Pokud je na vstupu <see cref="GuiTable"/> = null, pak na výstupu je <see cref="System.Data.DataTable"/> == null.
        /// Výstupem je vždy new instance tabulky, která není provázaná s <see cref="DataTable"/> = jde o různé objekty.
        /// </summary>
        /// <param name="guiTable"></param>
        public static implicit operator System.Data.DataTable(GuiTable guiTable) { return (guiTable != null ? WorkSchedulerSupport.TableDeserialize(guiTable.DataSerial) : null); }
        /// <summary>
        /// Implicitní konverze z <see cref="System.Data.DataTable"/> na <see cref="GuiTable"/>.
        /// Pokud je na vstupu <see cref="System.Data.DataTable"/> = null, pak na výstupu je <see cref="GuiTable"/> == null.
        /// Výstupem je new instance <see cref="GuiTable"/>, jejíž <see cref="DataTable"/> je new objekt, izolovaný od vstupní tabulky.
        /// </summary>
        /// <param name="dataTable"></param>
        public static implicit operator GuiTable(System.Data.DataTable dataTable) { return (dataTable != null ? new GuiTable() { Name = dataTable.TableName, DataSerial = WorkSchedulerSupport.TableSerialize(dataTable) } : null); }
        #endregion
        #region Čtení dat z tabulky
        /// <summary>
        /// Metoda načte z tabulky čísla typu int z prvního sloupce (kde se očekává číslo záznamu), doplní číslo třídy z Extended properties (default = parametr),
        /// a vrátí unique pole těchto čísel záznamů.
        /// Metoda vrací null, pokud neexistuje tabulka, nebo nemá sloupce anebo první sloupec není Int32.
        /// </summary>
        /// <param name="classNumber"></param>
        /// <returns></returns>
        public GuiId[] GetRecords(int classNumber = 0)
        {
            GuiId[] records, duplicites;
            this._GetRecords(classNumber, out records, out duplicites);
            return records;
        }
        /// <summary>
        /// Metoda načte z tabulky čísla typu int z prvního sloupce (kde se očekává číslo záznamu), doplní číslo třídy z Extended properties (default = parametr),
        /// najde duplicitní výskyty a vrátí jejich soupis.
        /// Metoda vrací null, pokud neexistuje tabulka, nebo nemá sloupce anebo první sloupec není Int32.
        /// </summary>
        /// <param name="classNumber"></param>
        /// <returns></returns>
        public GuiId[] GetDuplicities(int classNumber = 0)
        {
            GuiId[] records, duplicites;
            this._GetRecords(classNumber, out records, out duplicites);
            return duplicites;
        }
        /// <summary>
        /// Metoda načte z tabulky čísla typu int z prvního sloupce (kde se očekává číslo záznamu), doplní číslo třídy z Extended properties (default = parametr),
        /// a do out parametrů uloží jak pole unique záznamů, tak pole duplicitních záznamů.
        /// Out pole records obsahuje každý záznam pouze jedenkrát; obsahuje záznamy nonduplicitní (tj. ty, které jsou v tabulce jen jedenkrát), a obsahuje i záznamy, které jsou duplicitní (v tabulce se vyskytují vícekrát).
        /// Out pole duplicites obsahuje jen takové záznamy, které jsou v tabulce více než jednou. V poli duplicites jsou jen jedenkrát. Tyto záznamy jsou uvedeny i v poli records.
        /// Metoda vrací null, pokud neexistuje tabulka, nebo nemá sloupce anebo první sloupec není Int32.
        /// </summary>
        /// <param name="records"></param>
        /// <param name="duplicites"></param>
        /// <param name="classNumber"></param>
        /// <returns></returns>
        public void GetDuplicityRecords(out GuiId[] records, out GuiId[] duplicites, int classNumber = 0)
        {
            this._GetRecords(classNumber, out records, out duplicites);
        }
        /// <summary>
        /// Metoda najde a vrátí unique pole záznamů a unique pole duplicit záznamů.
        /// </summary>
        /// <param name="classNumber"></param>
        /// <param name="records"></param>
        /// <param name="duplicites"></param>
        private void _GetRecords(int classNumber, out GuiId[] records, out GuiId[] duplicites)
        {
            records = null;
            duplicites = null;
            System.Data.DataTable dataTable = this.DataTable;
            if (dataTable == null || dataTable.Columns.Count == 0) return;
            var colInfo0 = this.ColumnsExtendedInfo[0];
            if (colInfo0.ColumnType != typeof(int)) return;

            int? colClassNumber = colInfo0.ClassNumber;
            int recClassNumber = (colClassNumber.HasValue ? colClassNumber.Value : classNumber);
           
            Dictionary<int, GuiId> recordDict = new Dictionary<int, GuiId>();
            Dictionary<int, GuiId> duplicityDict = new Dictionary<int, GuiId>();

            foreach (DataRow row in dataTable.Rows)
            {
                int recRecordNumber = (int)row[0];         // Máme ověřeno, že sloupec [0] obsahuje typ int !
                if (!recordDict.ContainsKey(recRecordNumber))
                    recordDict.Add(recRecordNumber, new GuiId(recClassNumber, recRecordNumber));
                else if (!duplicityDict.ContainsKey(recRecordNumber))
                    duplicityDict.Add(recRecordNumber, new GuiId(recClassNumber, recRecordNumber));
            }
            records = recordDict.Values.ToArray();
            duplicites = duplicityDict.Values.ToArray();
        }
        #endregion
    }
    #endregion
    #region GuiTagItems : pole prvků GuiTagItem, reprezentuje Tagy jednoho řádku
    /// <summary>
    /// GuiTagItems : pole prvků GuiTagItem, reprezentuje Tagy jednoho řádku
    /// </summary>
    public class GuiTagItems : GuiBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiTagItems()
        {
            this.Name = TAG_ITEMS_NAME;
            this.TagItemList = new List<GuiTagItem>();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Name + "; Count: " + this.TagItemList.Count.ToString();
        }
        /// <summary>
        /// Název prvku <see cref="GuiTagItems"/>
        /// </summary>
        public const string TAG_ITEMS_NAME = "tagItems";
        /// <summary>
        /// Jednotlivé prvky typu <see cref="GuiTagItem"/>
        /// </summary>
        public List<GuiTagItem> TagItemList { get; set; }
    }
    /// <summary>
    /// GuiTagItem : reprezentuje Tagy jednoho řádku (obsahuje klíč řádku + jeden Tag).
    /// Tagy řádku dovolují k jednomu řádku připojit { 0 - 1 - mnoho } textových popisků (visačky = Tagy), a následně podle nich zafiltrovat řádky.
    /// </summary>
    public class GuiTagItem : GuiBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiTagItem()
        { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "RowId: " + (this.RowId != null ? this.RowId.ToString() : "Null") + "; TagItem: " + this.TagItem;
        }
        /// <summary>
        /// Identifikátor řádku
        /// </summary>
        public GuiId RowId { get; set; }
        /// <summary>
        /// Text tagu
        /// </summary>
        public string TagItem { get; set; }
    }
    #endregion
    #region GuiGraphProperties : vlastnosti grafu
    /// <summary>
    /// GuiGraphProperties : vlastnosti grafu
    /// </summary>
    public sealed class GuiGraphProperties : GuiBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiGraphProperties()
        {
            this.TimeAxisMode = TimeGraphTimeAxisMode.Standard;
            this.AxisResizeMode = AxisResizeContentMode.ChangeValueEnd;
            this.InteractiveChangeMode = AxisInteractiveChangeMode.All;
            this.GraphLineHeight = 20;
            this.GraphLinePartialHeight = 40;
            this.UpperSpaceLogical = 1f;
            this.BottomMarginPixel = 1;
            this.TableRowHeightMin = 22;
            this.TableRowHeightMax = 200;
            this.LogarithmicRatio = 0.60f;
            this.LogarithmicGraphDrawOuterShadow = 0.20f;
        }
        /// <summary>
        /// Režim zobrazování času na ose X
        /// </summary>
        public TimeGraphTimeAxisMode TimeAxisMode { get; set; }
        /// <summary>
        /// Režim chování při změně velikosti: zachovat měřítko a změnit hodnotu End, nebo zachovat hodnotu End a změnit měřítko?
        /// </summary>
        public AxisResizeContentMode AxisResizeMode { get; set; }
        /// <summary>
        /// Možnosti uživatele změnit zobrazený rozsah anebo měřítko
        /// </summary>
        public AxisInteractiveChangeMode InteractiveChangeMode { get; set; }
        /// <summary>
        /// Pozice grafu v tabulce
        /// </summary>
        public DataGraphPositionType GraphPosition { get; set; }
        /// <summary>
        /// Fyzická výška jedné logické linky grafu v pixelech.
        /// Určuje, tedy kolik pixelů bude vysoký prvek <see cref="GuiGraphItem"/>, jehož <see cref="GuiGraphBaseItem.Height"/> = 1.0f.
        /// Výchozí hodnota je 20.
        /// Tato hodnota <see cref="GraphLineHeight"/> platí pro řádky, v nichž se vyskytují pouze prvky s celočíselnou výškou v <see cref="GuiGraphBaseItem.Height"/>.
        /// Pro řádky, kde se vyskytne výška prvku <see cref="GuiGraphBaseItem.Height"/> desetinná, se použije údaj <see cref="GraphLinePartialHeight"/>.
        /// </summary>
        public int GraphLineHeight { get; set; }
        /// <summary>
        /// Fyzická výška jedné logické linky grafu v pixelech, pro řádky obsahující prvky s výškou <see cref="GuiGraphBaseItem.Height"/> desetinnou.
        /// V takových řádcích je vhodné použít větší hodnotu výšky logické linky, aby byly lépe viditelné prvky s malou výškou (např. výška prvku 0.25).
        /// Výchozí hodnota je 40.
        /// </summary>
        public int GraphLinePartialHeight { get; set; }
        /// <summary>
        /// Horní okraj = prostor nad nejvyšším prvkem grafu, který by měl být zobrazen jako prázdný, tak aby bylo vidět že nic dalšího už není.
        /// V tomto prostoru (těsně pod souřadnicí Top) se provádí Drag and Drop prvků.
        /// Hodnota je zadána v logických jednotkách, tedy v počtu standardních linek.
        /// Výchozí hodnota = 1.0 linka, nelze zadat zápornou hodnotu.
        /// </summary>
        public float UpperSpaceLogical { get; set; }
        /// <summary>
        /// Dolní okraj = mezera pod dolním okrajem nejnižšího prvku grafu k dolnímu okraji controlu, v pixelech.
        /// </summary>
        public int BottomMarginPixel { get; set; }
        /// <summary>
        /// Výška celého grafu, nejmenší přípustná, v pixelech.
        /// </summary>
        public int TableRowHeightMin { get; set; }
        /// <summary>
        /// Výška celého grafu, největší přípustná, v pixelech.
        /// </summary>
        public int TableRowHeightMax { get; set; }
        /// <summary>
        /// Logaritmická časová osa: Rozsah lineární části grafu uprostřed logaritmické časové osy.
        /// Implicitní hodnota (pokud není zadáno jinak) = 0.60f, povolené rozmezí od 0.40f po 0.90f.
        /// </summary>
        public float? LogarithmicRatio { get; set; }
        /// <summary>
        /// Logaritmická časová osa: vykreslovat vystínování oblastí s logaritmickým měřítkem osy (tedy ty levé a pravé okraje, kde již neplatí lineární měřítko).
        /// Zde se zadává hodnota 0 až 1, která reprezentuje úroven vystínování těchto okrajů.
        /// Hodnota 0 = žádné stínování, hodnota 1 = krajní pixel je zcela černý. 
        /// Implicitní hodnota (pokud není zadáno jinak) = 0.20f.
        /// </summary>
        public float? LogarithmicGraphDrawOuterShadow { get; set; }
        /// <summary>
        /// Průhlednost prvků grafu při běžném vykreslování.
        /// Má hodnotu null (průhlednost se neaplikuje), nebo 0 ÷ 255. 
        /// Hodnota 255 má stejný význam jako null = plně viditelný graf. 
        /// Hodnota 0 = zcela neviditelné prvky (ale fyzicky jsou přítomné).
        /// Výchozí hodnota = null.
        /// </summary>
        public int? Opacity { get; set; }
    }
    #endregion
    #region GuiGraphTable : Objekt reprezentující sadu grafických prvků GuiGraphItem, umožní pracovat s položkami grafu typově
    /// <summary>
    /// GuiGraphTable : Objekt reprezentující sadu grafických prvků <see cref="GuiGraphItem"/>, umožní pracovat s položkami grafu typově
    /// </summary>
    public sealed class GuiGraphTable : GuiBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiGraphTable()
        {
            this.GraphItems = new List<GuiGraphItem>();
        }
        /// <summary>
        /// Soupis položek grafů. Typicky jeden soupis obsahuje položky pro všechny řádky tabulky. GUI vrstva si položky rozebere.
        /// </summary>
        public List<GuiGraphItem> GraphItems { get; set; }
        /// <summary>
        /// Přidá další prvek do this seznamu
        /// </summary>
        /// <param name="item"></param>
        public void Add(GuiGraphItem item) { this.GraphItems.Add(item); }
        /// <summary>
        /// Přidá další prvky do this seznamu
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<GuiGraphItem> items) { this.GraphItems.AddRange(items); }
        /// <summary>
        /// Počet prvků v kolekci
        /// </summary>
        public int Count { get { return this.GraphItems.Count; } }
        /// <summary>
        /// Obsahuje prvek na daném indexu
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GuiGraphItem this[int index] { get { return this.GraphItems[index]; } }
        /// <summary>
        /// Potomek zde vrací soupis svých Child prvků
        /// </summary>
        [PersistingEnabled(false)]
        protected override IEnumerable<IGuiItem> Childs { get { return this.GraphItems; } }
    }
    #endregion
    #region GuiGraphItem : Jeden obdélníček v grafu na časové ose
    /// <summary>
    /// GuiGraphItem : Jeden obdélníček v grafu na časové ose
    /// </summary>
    public sealed class GuiGraphItem : GuiGraphBaseItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiGraphItem() : base() { }
        /// <summary>
        /// Obsahuje sloupce, které jsou povinné.
        /// Pokud dodaná data nebudou obsahovat některý z uvedených sloupců, načítání se neprovede.
        /// </summary>
        public const string StructureLiable = "item_class_id int, item_record_id int, row_class_id int, row_record_id int, begin datetime, end datetime";
        /// <summary>
        /// Obsahuje všechny sloupce, které jsou načítané do dat třídy <see cref="GuiGraphItem"/>, tj. i ty nepovinné.
        /// </summary>
        public const string StructureFull =
                    "item_class_id int, item_record_id int, row_class_id int, row_record_id int, " +
                    "group_class_id int, group_record_id int, data_class_id int, data_record_id int, " +
                    "begin datetime, end datetime, " +
                    "layer int, level int, order int, height float, " +
                    "text string, tooltip string, " +
                    "back_color string, line_color string, back_style string, " +
                    "ratio_begin float, ratio_end float, ratio_begin_back_color string, ratio_end_back_color string, ratio_line_color string, ratio_line_width int";
    }
    /// <summary>
    /// Bázová třída pro předávání dat o grafických položkách.
    /// Existují dva potomci: 
    /// 1. <see cref="GuiGraphItem"/> pro předávání základního balíku dat po jejich načtení, je umístěn v hierarchické struktuře, 
    /// obsahuje navíc <see cref="GuiGraphItem.StructureFull"/> a <see cref="GuiGraphItem.StructureLiable"/>;
    /// 2. <see cref="GuiGridGraphItem"/> pro předávání změnových dat, 
    /// obsahuje navíc <see cref="GuiGridGraphItem.TableName"/> pro určení tabulky, kam se má prvek přidat.
    /// </summary>
    public abstract class GuiGraphBaseItem : GuiBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiGraphBaseItem()
        {
            this.Height = 1f;
            this.BehaviorMode = GraphItemBehaviorMode.DefaultText;
        }
        /// <summary>
        /// Jméno prvku GuiGraphItem je vždy rovno textu z <see cref="ItemId"/>. Property Name zde nemá význam setovat.
        /// </summary>
        public override string Name
        {
            get { return (this.ItemId != null ? this.ItemId.Name : "NULL"); }
            set { }
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "RowId: " + this.RowId.ToString() + "; ItemId: " + this.ItemId.ToString() + "; Time: " + this.Time;
        }
        /// <summary>
        /// ID položky časového grafu (ID obdélníčku).
        /// Z databáze se načítá ze sloupců: "item_class_id", "item_record_id", je POVINNÝ.
        /// </summary>
        public GuiId ItemId { get; set; }
        /// <summary>
        /// ID řádku, v jehož grafu se má tento prvek zobrazovat.
        /// Z databáze se načítá ze sloupců: "row_class_id", "row_record_id", je POVINNÝ.
        /// </summary>
        public GuiId RowId { get; set; }
        /// <summary>
        /// GroupId: číslo skupiny. Prvky se shodným GroupId budou vykreslovány do společného "rámce", 
        /// a pokud mezi jednotlivými prvky grafu se shodným <see cref="GroupId"/> bude na ose X nějaké volné místo,
        /// nebude mezi nimi vykreslován žádný "cizí" prvek.
        /// Z databáze se načítá ze sloupců: "group_class_id", "group_record_id", je NEPOVINNÝ.
        /// </summary>
        public GuiId GroupId { get; set; }
        /// <summary>
        /// ID datového záznamu, jehož formulář se má rozkliknout po Ctrl + DoubleKliknutí na záznam.
        /// Z databáze se načítá ze sloupců: "data_class_id", "data_record_id", je NEPOVINNÝ.
        /// </summary>
        public GuiId DataId { get; set; }
        /// <summary>
        /// Datum a čas počátku tohoto prvku.
        /// Z databáze se načítá ze sloupce: "begin" a "end", je POVINNÝ.
        /// </summary>
        public GuiTimeRange Time { get; set; }
        /// <summary>
        /// Layer: Vizuální vrstva. Prvky z různých vrstev jsou kresleny "přes sebe" = mohou se překrývat.
        /// Nižší hodnota je kreslena dříve.
        /// Například: záporná hodnota Layer reprezentuje "podklad" který se needituje.
        /// Z databáze se načítá ze sloupce: "layer", je NEPOVINNÝ.
        /// </summary>
        public int Layer { get; set; }
        /// <summary>
        /// Level: Vizuální hladina. Prvky v jedné hladině jsou kresleny do společného vodorovného pásu, 
        /// další prvky ve vyšší hladině jsou všechny zase vykresleny ve svém odděleném pásu (nad tímto nižším pásem). 
        /// Nespadnou do prvků nižšího pásu i když by v něm bylo volné místo.
        /// Z databáze se načítá ze sloupce: "level", je NEPOVINNÝ.
        /// </summary>
        public int Level { get; set; }
        /// <summary>
        /// Order: pořadí prvku při výpočtech souřadnic Y před vykreslováním. 
        /// Prvky se stejným Order budou tříděny vzestupně podle data počátku <see cref="Time"/>.Begin.
        /// Z databáze se načítá ze sloupce: "order", je NEPOVINNÝ.
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// Relativní výška tohoto prvku. Standardní hodnota = 1.0F. Fyzická výška (v pixelech) jednoho prvku je dána součinem 
        /// <see cref="Height"/> * <see cref="GuiGraphProperties.GraphLineHeight"/>
        /// Prvky s výškou 0 a menší nebudou vykresleny.
        /// Z databáze se načítá ze sloupce: "height", je NEPOVINNÝ.
        /// </summary>
        public float Height { get; set; }
        /// <summary>
        /// Text pro zobrazení uvnitř tohoto prvku.
        /// Pokud je null, bude se hledat v tabulce textů.
        /// Z databáze se načítá ze sloupce: "text", je NEPOVINNÝ.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// ToolTip pro zobrazení u tohoto tohoto prvku.
        /// Pokud je null, bude se hledat v tabulce textů.
        /// Z databáze se načítá ze sloupce: "tooltip", je NEPOVINNÝ.
        /// </summary>
        public string ToolTip { get; set; }
        /// <summary>
        /// Barva pozadí prvku.
        /// Pokud bude null, pak prvek nebude mít vyplněný svůj prostor (obdélník). Může mít vykreslené okraje (barva <see cref="LineColor"/>).
        /// Anebo může mít kreslené Ratio (viz property <see cref="RatioBegin"/>, <see cref="RatioEnd"/>, 
        /// <see cref="RatioBeginBackColor"/>, <see cref="RatioLineColor"/>, <see cref="RatioLineWidth"/>).
        /// Z databáze se načítá ze sloupce: "back_color", je NEPOVINNÝ.
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Barva linek ohraničení prvku.
        /// Pokud je null, pak prvek nemá ohraničení pomocí linky (Border).
        /// Z databáze se načítá ze sloupce: "line_color", je NEPOVINNÝ.
        /// </summary>
        public Color? LineColor { get; set; }
        /// <summary>
        /// Poměrná hodnota "nějakého" splnění v rámci prvku, na jeho počátku.
        /// Běžně se vykresluje jako poměrná část prvku, měřeno odspodu, která symbolizuje míru "naplnění" daného úseku.
        /// Část Ratio má tvar lichoběžníku, a spojuje body Begin = { <see cref="Time"/>.Begin, <see cref="RatioBegin"/> } a { <see cref="Time"/>.End, <see cref="RatioEnd"/> }.
        /// <para/>
        /// Pro zjednodušení zadávání: pokud je naplněno <see cref="RatioBegin"/>, ale v <see cref="RatioEnd"/> je null, 
        /// pak vykreslovací algoritmus předpokládá hodnotu End stejnou jako Begin. To znamená, že pro "obdélníkové" ratio stačí naplnit jen <see cref="RatioBegin"/>.
        /// Ale opačně to neplatí.
        /// <para/>
        /// Z databáze se načítá ze sloupce: "ratio_begin", je NEPOVINNÝ.
        /// </summary>
        public float? RatioBegin { get; set; }
        /// <summary>
        /// Poměrná hodnota "nějakého" splnění v rámci prvku, na jeho konci.
        /// Běžně se vykresluje jako poměrná část prvku, měřeno odspodu, která symbolizuje míru "naplnění" daného úseku.
        /// Část Ratio má tvar lichoběžníku, a spojuje body Begin = { <see cref="Time"/>.Begin, <see cref="RatioBegin"/> } a { <see cref="Time"/>.End, <see cref="RatioEnd"/> }.
        /// <para/>
        /// Pro zjednodušení zadávání: pokud je naplněno <see cref="RatioBegin"/>, ale v <see cref="RatioEnd"/> je null, 
        /// pak vykreslovací algoritmus předpokládá hodnotu End stejnou jako Begin. To znamená, že pro "obdélníkové" ratio stačí naplnit jen <see cref="RatioBegin"/>.
        /// Ale opačně to neplatí.
        /// <para/>
        /// Z databáze se načítá ze sloupce: "ratio_end", je NEPOVINNÝ.
        /// </summary>
        public float? RatioEnd { get; set; }
        /// <summary>
        /// Barva pozadí prvku, kreslená v části Ratio, na straně času Begin.
        /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má hodnotu větší než 0f.
        /// Touto barvou je vykreslena dolní část prvku, která symbolizuje míru "naplnění" daného úseku.
        /// Tato část má tvar lichoběžníku, dolní okraj je na hodnotě 0, levý okraj má výšku <see cref="RatioBegin"/>, pravý okraj má výšku <see cref="RatioEnd"/>.
        /// Může sloužit k zobrazení vyčerpané pracovní kapacity, nebo jako lineární částečka grafu sloupcového nebo liniového.
        /// Tato barva se použije buď jako Solid color pro celý prvek v části Ratio, 
        /// anebo jako počáteční barva na souřadnici X = čas Begin při výplni Linear, 
        /// a to tehdy, pokud je zadána i barva <see cref="RatioEndBackColor"/> (ta reprezentuje barvu na souřadnici X = čas End).
        /// Z databáze se načítá ze sloupce: "ratio_begin_back_color", je NEPOVINNÝ.
        /// </summary>
        public Color? RatioBeginBackColor { get; set; }
        /// <summary>
        /// Barva pozadí prvku, kreslená v části Ratio, na straně času End.
        /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má hodnotu větší než 0f.
        /// Touto barvou je vykreslena dolní část prvku, která symbolizuje míru "naplnění" daného úseku.
        /// Tato část má tvar lichoběžníku, dolní okraj je na hodnotě 0, levý okraj má výšku <see cref="RatioBegin"/>, pravý okraj má výšku <see cref="RatioEnd"/>.
        /// Může sloužit k zobrazení vyčerpané pracovní kapacity, nebo jako lineární částečka grafu sloupcového nebo liniového.
        /// Tato barva se použije jako koncová barva (na souřadnici X = čas End) v lineární výplni prostoru Ratio,
        /// kde počáteční barva výplně (na souřadnici X = čas Begin) je dána v <see cref="RatioBeginBackColor"/>.
        /// Z databáze se načítá ze sloupce: "ratio_end_back_color", je NEPOVINNÝ.
        /// </summary>
        public Color? RatioEndBackColor { get; set; }
        /// <summary>
        /// Barva linky, kreslená v úrovni Ratio.
        /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má zadanou hodnotu v rozsahu 0 (včetně) a více.
        /// Touto barvou je vykreslena přímá linie, která symbolizuje míru "naplnění" daného úseku, 
        /// a spojuje body Begin = { <see cref="Time"/>.Begin, <see cref="RatioBegin"/> } a { <see cref="Time"/>.End, <see cref="RatioEnd"/> }.
        /// Z databáze se načítá ze sloupce: "ratio_line_color", je NEPOVINNÝ.
        /// </summary>
        public Color? RatioLineColor { get; set; }
        /// <summary>
        /// Šířka linky, kreslená v úrovni Ratio.
        /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má zadanou hodnotu v rozsahu 0 (včetně) a více.
        /// Čárou této šířky je vykreslena přímá linie, která symbolizuje míru "naplnění" daného úseku, 
        /// a spojuje body Begin = { <see cref="Time"/>.Begin, <see cref="RatioBegin"/> } a { <see cref="Time"/>.End, <see cref="RatioEnd"/> }.
        /// Z databáze se načítá ze sloupce: "ratio_line_width", je NEPOVINNÝ.
        /// </summary>
        public int? RatioLineWidth { get; set; }
        /// <summary>
        /// Styl vzorku kresleného v pozadí.
        /// null = Solid.
        /// Z databáze se načítá ze sloupce: "back_style", je NEPOVINNÝ.
        /// </summary>
        public System.Drawing.Drawing2D.HatchStyle? BackStyle { get; set; }
        /// <summary>
        /// Režim chování položky grafu (editovatelnost, texty, atd).
        /// Tato hodnota se nenačítá z SQL SELECTU, musí se naplnit ručně.
        /// </summary>
        public GraphItemBehaviorMode BehaviorMode { get; set; }
    }
    #endregion
    #region GuiToolbarPanel : Celý Toolbar
    /// <summary>
    /// GuiToolbarPanel : Celý Toolbar, obsahuje položky v seznamu <see cref="Items"/> 
    /// </summary>
    public sealed class GuiToolbarPanel : GuiBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiToolbarPanel()
        {
            this.ToolbarVisible = true;
            this.ToolbarShowSystemItems = ToolbarSystemItem.TimeAxisZoomDWWM | ToolbarSystemItem.TimeAxisGoAll;
            this.Items = new List<GuiToolbarItem>();
        }
        /// <summary>
        /// Zobrazovat toolbar?
        /// </summary>
        public bool ToolbarVisible { get; set; }
        /// <summary>
        /// Které systémové položky zobrazovat v Toolbaru?
        /// </summary>
        public ToolbarSystemItem ToolbarShowSystemItems { get; set; }
        /// <summary>
        /// Všechny položky obsažené v Toolbaru
        /// </summary>
        public List<GuiToolbarItem> Items { get; set; }
        /// <summary>
        /// Přidá další prvek do this seznamu
        /// </summary>
        /// <param name="item"></param>
        public void Add(GuiToolbarItem item) { this.Items.Add(item); }
        /// <summary>
        /// Přidá další prvky do this seznamu
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<GuiToolbarItem> items) { this.Items.AddRange(items); }
        /// <summary>
        /// Počet prvků v kolekci
        /// </summary>
        public int Count { get { return this.Items.Count; } }
        /// <summary>
        /// Obsahuje prvek na daném indexu
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GuiToolbarItem this[int index] { get { return this.Items[index]; } }
        /// <summary>
        /// Potomek zde vrací soupis svých Child prvků
        /// </summary>
        [PersistingEnabled(false)]
        protected override IEnumerable<IGuiItem> Childs { get { return this.Items; } }
    }
    #endregion
    #region GuiToolbarItem : Položka zobrazovaná v Toolbaru
    /// <summary>
    /// GuiToolbarItem : Položka zobrazovaná v Toolbaru
    /// </summary>
    public sealed class GuiToolbarItem : GuiTextItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiToolbarItem()
        {
        }
        /// <summary>
        /// Název nebo binární obsah obrázku pro stav MouseActive
        /// </summary>
        public GuiImage ImageHot { get; set; }
        /// <summary>
        /// Velikost prvku na toolbaru, vzhledem k výšce toolbaru
        /// Výchozí hodnota = null, bude interpretována jako Half.
        /// </summary>
        public FunctionGlobalItemSize? Size { get; set; }
        /// <summary>
        /// Explicitně požadovaná šířka prvku v počtu modulů, opatrně s ohledem na obsah textu
        /// </summary>
        public int? ModuleWidth { get; set; }
        /// <summary>
        /// Nápověda ke zpracování layoutu této položky (jak se tato položka řadí za ostatní položky).
        /// Výchozí hodnota = null, bude interpretována jako Default.
        /// </summary>
        public LayoutHint? LayoutHint { get; set; }
        /// <summary>
        /// Název grupy, v níž bude tato položka toolbaru zařazena. Nezadáno = bude v implicitní skupině "FUNKCE".
        /// </summary>
        public string GroupName { get; set; }
        /// <summary>
        /// true = prvek je viditelný.
        /// Výchozí hodnota = null, bude interpretována jako true.
        /// </summary>
        public bool? Visible { get; set; }
        /// <summary>
        /// true = prvek je dostupný.
        /// Výchozí hodnota = null, bude interpretována jako true.
        /// </summary>
        public bool? Enable { get; set; }
    }
    #endregion
    #region GuiContextMenuSet : Všechny položky všech Kontextových menu
    /// <summary>
    /// GuiContextMenuSet : Všechny položky všech Kontextových menu
    /// </summary>
    public sealed class GuiContextMenuSet : GuiBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiContextMenuSet()
        {
            this.Items = new List<GuiContextMenuItem>();
        }
        /// <summary>
        /// Všechny položky obsažené v Toolbaru
        /// </summary>
        public List<GuiContextMenuItem> Items { get; set; }
        /// <summary>
        /// Přidá další prvek do this seznamu
        /// </summary>
        /// <param name="item"></param>
        public void Add(GuiContextMenuItem item) { this.Items.Add(item); }
        /// <summary>
        /// Přidá další prvky do this seznamu
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<GuiContextMenuItem> items) { this.Items.AddRange(items); }
        /// <summary>
        /// Počet prvků v kolekci
        /// </summary>
        public int Count { get { return this.Items.Count; } }
        /// <summary>
        /// Obsahuje prvek na daném indexu
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GuiContextMenuItem this[int index] { get { return this.Items[index]; } }
        /// <summary>
        /// Potomek zde vrací soupis svých Child prvků
        /// </summary>
        [PersistingEnabled(false)]
        protected override IEnumerable<IGuiItem> Childs { get { return this.Items; } }
    }
    #endregion
    #region GuiContextMenuItem : Jedna položka nabídky, zobrazovaná v kontextovém menu
    /// <summary>
    /// GuiContextMenuItem : Jedna položka nabídky, zobrazovaná v kontextovém menu
    /// </summary>
    public sealed class GuiContextMenuItem : GuiTextItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiContextMenuItem()
        { }
        /// <summary>
        /// Definice prvků, pro které se má tato funkce zobrazovat.
        /// <para/>
        /// Pravidla pro zadávání textu: 
        /// Text má obsahovat klíč prvku (řádku, grafu) ve formě FullName gridu, kde je grafický prvek zobrazen + volitelně číslo třídy grafického prvku.
        /// FullName gridu lze přečíst z konkrétního objektu <see cref="GuiGrid"/> poté, kdy je tento Grid plně zařazen do struktur <see cref="GuiData"/>, 
        /// a na tomto hlavním objektu proběhla metoda <see cref="GuiData.FillParents()"/> (metodu lze spouštět kdykoliv, i opakovaně).
        /// FullName gridu <see cref="GuiGrid"/> má formu: "Data\pageMain\mainPanel\workGrid". Více viz <see cref="GuiBase.FullName"/>.
        /// Text pro definici <see cref="VisibleFor"/> i <see cref="EnableFor"/> obsahuje FullName gridu, volitelně pak dvojtečku a číslo třídy prvku grafu, pro který je funkce určená.
        /// Číslo třídy: může jich být uvedeno více, oddělené čárkami.
        /// Definice může obsahovat více prvků, oddělených středníkem.
        /// Definice může v rámci FullName obsahovat hvězdičku, která nahrazuje část FullName (nebo i celou hodnotu FullName).
        /// <para/>
        /// Příklady celého textu:
        /// "Data\pageMain\mainPanel\workGrid:1190": funkce je dostupná pro hlavní stranu (pageMain), hlavní panel (mainPanel), pro jednu tabulku (workGrid), pro prvky třídy 1190;
        /// "Data\pageMain\*:1190": funkce je dostupná pro hlavní stranu (pageMain), pro všechny panely a tabulky, pro prvky třídy 1190;
        /// "Data\pageMain\*": funkce je dostupná pro hlavní stranu (pageMain), pro všechny panely a tabulky, pro prvky všech tříd;
        /// "Data\pageMain\*:1190,1815": funkce je dostupná pro hlavní stranu (pageMain), pro všechny panely a tabulky, pro prvky tříd 1190 a 1815
        /// </summary>
        public string VisibleFor { get; set; }
        /// <summary>
        /// Definice prvků, pro které má být tato funkce dostupná.
        /// Pokud bude prázdné, pak bude funkce dostupná pro všechny prvky, kde se funkce bude zobrazovat.
        /// Jakmile začne aplikace zadávat <see cref="EnableFor"/>, pak musí akceptovat, že pro nezadané prvky bude funkce Not Enabled.
        /// <para/>
        /// Pravidla pro zadávání textu: stejná jako pro <see cref="VisibleFor"/>.
        /// </summary>
        public string EnableFor { get; set; }
    }
    #endregion
    #region GuiTextItem : Vizuální prvek v GUI, obsahuje Name, Title, ToolTip a Image
    /// <summary>
    /// GuiTextItem : Vizuální prvek v GUI, obsahuje Name, Title, ToolTip a Image
    /// </summary>
    public class GuiTextItem : GuiBase
    {
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "[" + this.Name + "] " + this.Title + " ... " + this.ToolTip;
        }
        /// <summary>
        /// Titulek, zobrazovaný pro uživatele vždy (text v záhlaví stránky, text tlačítka toolbaru, text položky funkce, atd)
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// ToolTip, zobrazovaný pro uživatele po najetí myší na prvek
        /// </summary>
        public string ToolTip { get; set; }
        /// <summary>
        /// Název nebo binární obsah obrázku
        /// </summary>
        public GuiImage Image { get; set; }
    }
    #endregion
    #region GuiBase : Bázová třída všech prvků Gui*
    /// <summary>
    /// GuiBase : Bázová třída všech prvků Gui*
    /// </summary>
    public abstract class GuiBase : IGuiItem
    {
        #region Data : Name, UserData, Parent, Childs
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "[" + this.Name + "]";
        }
        /// <summary>
        /// Klíčové jméno, používané v aplikaci jako strojový název prvku.
        /// <see cref="Name"/> nesmí obsahovat zpětné lomítko (při pokusu o jeho použití je nahrazeno obyčejným lomítkem).
        /// Jméno nikdy není null; při vložení hodnoty null je vložena stringová konstanta "{Null}".
        /// </summary>
        public virtual string Name { get { return this._Name; } set { this._Name = (value == null ? NAME_NULL : value.Replace(NAME_SEPARATOR, "/")); } }
        private string _Name = NAME_NULL;
        /// <summary>
        /// Libovolná aplikační data, která neprochází serializací.
        /// Toto je prostor, který může využít aplikace k uložení svých dat nad rámec dat třídy, protože Gui třídy jsou sealed 
        /// a aplikace nemůže používat potomky základních tříd.
        /// </summary>
        [PersistingEnabled(false)]
        public object UserData { get; set; }
        /// <summary>
        /// Objekt parenta, v němž je tento prvek umístěn.
        /// </summary>
        [PersistingEnabled(false)]
        public IGuiItem Parent { get { return this._Parent; } }
        /// <summary>
        /// Úložiště parenta
        /// </summary>
        [PersistingEnabled(false)]
        private IGuiItem _Parent;
        /// <summary>
        /// V této property vrací daný objekt všechny svoje přímé Child objekty.
        /// Pokud objekt nemá Child objekty, vrací null (to zajišťuje bázová třída <see cref="GuiBase"/>).
        /// Pokud má jednu sadu Child objektů, vrací ji napřímo.
        /// Pokud objekt má více sad Child objektů, pak vrací jejich souhrnný seznam, vytvořený metodou <see cref="Union(object[])"/>.
        /// </summary>
        [PersistingEnabled(false)]
        protected virtual IEnumerable<IGuiItem> Childs { get { return null; } }
        #endregion
        #region Práce s řadou Parentů
        /// <summary>
        /// Plné jméno tohoto objektu = počínaje Root parentem, 
        /// obsahuje veškerá jména (<see cref="IGuiItem.Name"/>) oddělená zpětným lomítkem,
        /// až k this jménu <see cref="Name"/>.
        /// Největší možná délka <see cref="FullName"/> je 1024 znaků.
        /// <see cref="FullName"/> nezačíná zpětným lomítkem, má typicky tvar: "Data\pageMain\mainPanel\workGrid".
        /// V uvedené ukázce, jednotlivé složky jména:
        /// - "Data" je implicitní Name prvku <see cref="GuiData"/>, ale toto jméno lze přepsat;
        /// - "pageMain" je název stránky, definuje aplikace
        /// - "mainPanel" je konstantní název hlavního panelu, aplikace běžně nepřepisuje (ale může)
        /// - "workGrid" je název konkrétní tabulky <see cref="GuiGrid"/> (tabulka obsahuje řádky, prvky grafů a textové prvky). Název definuje aplikace.
        /// </summary>
        [PersistingEnabled(false)]
        public virtual string FullName
        {
            get
            {
                string fullName = "";
                string separator = "";
                IGuiItem item = this;
                while (item != null)
                {
                    fullName = item.Name + separator + fullName;
                    if (fullName.Length > 1024) break;
                    if (separator.Length == 0) separator = NAME_SEPARATOR;
                    item = item.Parent;
                }
                return fullName;
            }
        }
        /// <summary>
        /// Oddělovač úrovní jmen ve <see cref="FullName"/>
        /// </summary>
        protected const string NAME_SEPARATOR = "\\";
        /// <summary>
        /// Náhradní jméno namísto hodnoty null
        /// </summary>
        protected const string NAME_NULL = "{Null}";
        #endregion
        #region Servis pro potomky: Vložení Parenta do Childs; tvorba Union()
        /// <summary>
        /// Metoda vloží this instanci do všech svých Childs objektů, a zajistí totéž i pro tyto Childs
        /// </summary>
        protected virtual void FillParentToChilds()
        {
            Queue<IGuiItem> queue = new Queue<IGuiItem>();
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                IGuiItem item = queue.Dequeue();
                IEnumerable<IGuiItem> childs = item.Childs;
                if (childs == null) continue;
                foreach (IGuiItem child in childs)
                {
                    if (child == null) continue;
                    child.Parent = item;
                    queue.Enqueue(child);
                }
            }
        }
        /// <summary>
        /// Metoda najde první vyhovující prvek na základě jeho plného jména.
        /// Pokud by více prvků v jedné úrovni mělo shodné jméno (což je prakticky přípustné), pak se pracuje jen s prvním z nich.
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        protected virtual IGuiItem FindByName(string fullName)
        {
            if (String.IsNullOrEmpty(fullName)) return null;
            string[] names = fullName.Split(new string[] { NAME_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
            int pointer = 0;
            int length = names.Length;

            // Pokud neopdovídá název Root prvku, skončíme hned:
            IGuiItem item = this;
            if (!String.Equals(item.Name, names[pointer], StringComparison.InvariantCulture)) return null;

            pointer++;
            while (pointer < length)
            {
                var childs = item.Childs;
                if (childs == null) return null;
                item = childs.FirstOrDefault(i => String.Equals(i.Name, names[pointer], StringComparison.InvariantCulture));
                if (item == null) return null;
                pointer++;
            }
            return item;
        }
        /// <summary>
        /// Metoda vezme všechny dodané parametry, a přidá je do výstupního spojeného seznamu.
        /// Pokud vstup je pole (IEnumerable) prvků <see cref="IGuiItem"/>, pak do výstupu přidá všechny prvky pole.
        /// Pokud vstup je jeden objekt <see cref="IGuiItem"/>, pak do výstupu přidá daný objekt.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        protected static IEnumerable<IGuiItem> Union(params object[] items)
        {
            List<IGuiItem> result = new List<IGuiItem>();
            foreach (object item in items)
            {
                if (item is IEnumerable<IGuiItem>)
                    result.AddRange(item as IEnumerable<IGuiItem>);
                else if (item is IGuiItem)
                    result.Add(item as IGuiItem);
            }
            return result;
        }
        #endregion
        #region Implementace IGuiBase
        /// <summary>
        /// Member of interface IGuiBase : Klíčové jméno, používané v aplikaci jako strojový název prvku
        /// </summary>
        [PersistingEnabled(false)]
        string IGuiItem.Name { get { return this.Name; } }
        /// <summary>
        /// Member of interface IGuiBase : objekt parenta včetně možnosti setování
        /// </summary>
        [PersistingEnabled(false)]
        IGuiItem IGuiItem.Parent { get { return this._Parent; } set { this._Parent = value; } }
        /// <summary>
        /// Member of interface IGuiBase : soupis všech Child objektů tohoto objektu.
        /// </summary>
        [PersistingEnabled(false)]
        IEnumerable<IGuiItem> IGuiItem.Childs { get { return this.Childs; } }
        #endregion
    }
    #endregion
    #region IGuiBase : společný interface všech prvků Gui* (kromě GuiId)
    /// <summary>
    /// Interface, který implementuje každá třída Gui (kromě GuiId), a který garantuje přítomnost prvku <see cref="Parent"/> včetně { set } accessoru.
    /// Tzn. přes tento interface je možno nasetovat parenta do každého objektu.
    /// </summary>
    public interface IGuiItem
    {
        /// <summary>
        /// Klíčové jméno, používané v aplikaci jako strojový název prvku.
        /// <see cref="Name"/> nesmí obsahovat zpětné lomítko (při pokusu o jeho použití je nahrazeno obyčejným lomítkem).
        /// Jméno nikdy není null; při vložení hodnoty null je vložena stringová konstanta "{Null}".
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Parent this objektu
        /// </summary>
        IGuiItem Parent { get; set; }
        /// <summary>
        /// V této property vrací daný objekt všechny svoje přímé Child objekty.
        /// Pokud objekt nemá Child objekty, vrací null.
        /// Pokud má jednu sadu Child objektů, vrací ji.
        /// Pokád má více sad Child objektů, pak vrací jejich Union.
        /// </summary>
        IEnumerable<IGuiItem> Childs { get; }
    }
    #endregion
    #region GuiImage : Třída pro předávání ikony z aplikačního serveru do pluginu
    /// <summary>
    /// GuiImage : Třída pro předávání ikony z aplikačního serveru do pluginu.
    /// Tato třída nemá property Name, je používána uvnitř prvků, které mají svoje jméno.
    /// </summary>
    public sealed class GuiImage : IXmlSerializer
    {
        #region Konstruktor, základní property
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiImage()
        { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (!String.IsNullOrEmpty(this.ImageFile)) return "File: " + this.ImageFile;
            if (this.ImageContent != null) return "Content: " + this.ImageContent.Length.ToString() + " Byte";
            if (this.Image != null) return "Image: " + this.Image.Size.ToString() + " pixels";
            return "GuiImage: empty";
        }
        /// <summary>
        /// Název souboru
        /// </summary>
        [PersistingEnabled(false)]
        public string ImageFile { get; set; }
        /// <summary>
        /// Binární obsah souboru
        /// </summary>
        [PersistingEnabled(false)]
        public byte[] ImageContent { get; set; }
        /// <summary>
        /// Fyzický obrázek
        /// </summary>
        [PersistingEnabled(false)]
        public Image Image { get; set; }
        #endregion
        #region Serializace
        /// <summary>
        /// Tato property má obsahovat (get vrací, set akceptuje) XML data z celého aktuálního objektu.
        /// </summary>
        [PersistingEnabled(true)]
        string IXmlSerializer.XmlSerialData
        {
            get
            {   // Potřebujeme získat string pro persistenci:
                string prefix = "";
                string text = null;
                if (!String.IsNullOrEmpty(this.ImageFile))
                {
                    prefix = FILE_PREFIX;
                    text = this.ImageFile;
                }
                else if (this.ImageContent != null)
                {
                    prefix = CONTENT_PREFIX;
                    text = System.Convert.ToBase64String(this.ImageContent, Base64FormattingOptions.None);
                }
                else if (this.Image != null)
                {
                    prefix = IMAGE_PREFIX;
                    byte[] data = null;
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                    {
                        this.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        data = ms.ToArray();
                        text = System.Convert.ToBase64String(data, Base64FormattingOptions.None);
                    }
                }
                return (text != null ? prefix + text : null);
            }
            set
            {   // Je vložen (persistovaný) string, vložíme jej tam kam patří (podle prefixu):
                this.ImageFile = null;
                this.ImageContent = null;
                this.Image = null;
                if (!String.IsNullOrEmpty(value) && value.Length > 2)
                {
                    string prefix = value.Substring(0, 2);
                    string text = value.Substring(2);
                    try
                    {
                        switch (prefix)
                        {
                            case FILE_PREFIX:
                                this.ImageFile = text;
                                break;
                            case CONTENT_PREFIX:
                                this.ImageContent = System.Convert.FromBase64String(text);
                                break;
                            case IMAGE_PREFIX:
                                byte[] data = System.Convert.FromBase64String(text);
                                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(data))
                                {
                                    this.Image = Bitmap.FromStream(ms);
                                }
                                break;
                        }
                    }
                    catch { }
                }
            }
        }
        /// <summary>
        /// Prefix pro data, pokud pocházejí z <see cref="ImageFile"/>
        /// </summary>
        private const string FILE_PREFIX = "F:";
        /// <summary>
        /// Prefix pro data, pokud pocházejí z <see cref="ImageContent"/>
        /// </summary>
        private const string CONTENT_PREFIX = "C:";
        /// <summary>
        /// Prefix pro data, pokud pocházejí z <see cref="Image"/>
        /// </summary>
        private const string IMAGE_PREFIX = "I:";
        #endregion
        #region Implicitní konverze GuiImage <==> String, Image, Byte[]
        /// <summary>
        /// Implicitní konverze z <see cref="GuiImage"/> na <see cref="System.String"/>.
        /// Pokud je na vstupu <see cref="GuiImage"/> = null, pak na výstupu je <see cref="System.String"/> == null.
        /// Výstupem je vždy new instance objektu, která není provázaná s <see cref="GuiImage"/> = jde o různé objekty.
        /// </summary>
        /// <param name="guiImage"></param>
        public static implicit operator System.String(GuiImage guiImage) { return (guiImage != null ? guiImage.ImageFile : null); }
        /// <summary>
        /// Implicitní konverze z <see cref="System.String"/> na <see cref="GuiImage"/>.
        /// Pokud je na vstupu <see cref="System.String"/> = null, pak na výstupu je <see cref="GuiImage"/> == null.
        /// Výstupem je new instance <see cref="GuiImage"/>, jejíž <see cref="ImageFile"/> obsahuje zadaný string.
        /// Zadané jméno souboru by mělo pocházet z konstant v namespace <see cref="Noris.LCS.Base.WorkScheduler.Resources"/>.
        /// </summary>
        /// <param name="imageFile"></param>
        public static implicit operator GuiImage(System.String imageFile) { return (imageFile != null ? new GuiImage() { ImageFile = imageFile } : null); }

        /// <summary>
        /// Implicitní konverze z <see cref="GuiImage"/> na <see cref="System.Drawing.Image"/>.
        /// Pokud je na vstupu <see cref="GuiImage"/> = null, pak na výstupu je <see cref="System.Drawing.Image"/> == null.
        /// Pokud vstupní instance <see cref="GuiImage"/> má v property <see cref="Image"/> null, vrací se null (tato konverze neslouží ke generování Image).
        /// </summary>
        /// <param name="guiImage"></param>
        public static implicit operator System.Drawing.Image(GuiImage guiImage) { return (guiImage != null ? guiImage.Image : null); }
        /// <summary>
        /// Implicitní konverze z <see cref="System.Drawing.Image"/> na <see cref="GuiImage"/>.
        /// Pokud je na vstupu <see cref="System.Drawing.Image"/> = null, pak na výstupu je <see cref="GuiImage"/> == null.
        /// Výstupem je new instance <see cref="GuiImage"/>, jejíž <see cref="ImageFile"/> obsahuje zadaný string.
        /// </summary>
        /// <param name="image"></param>
        public static implicit operator GuiImage(System.Drawing.Image image) { return (image != null ? new GuiImage() { Image = image } : null); }

        /// <summary>
        /// Implicitní konverze z <see cref="GuiImage"/> na <see cref="System.Byte"/>[].
        /// Pokud je na vstupu <see cref="GuiImage"/> = null, pak na výstupu je <see cref="System.Byte"/>[] == null.
        /// Výstupem je reference přímo na instanci objektu <see cref="ImageContent"/> (neprovádí se kopie pole).
        /// </summary>
        /// <param name="guiImage"></param>
        public static implicit operator System.Byte[] (GuiImage guiImage) { return (guiImage != null ? guiImage.ImageContent : null); }
        /// <summary>
        /// Implicitní konverze z <see cref="System.Byte"/>[] na <see cref="GuiImage"/>.
        /// Pokud je na vstupu <see cref="System.Byte"/>[] = null, pak na výstupu je <see cref="GuiImage"/> == null.
        /// Výstupem je new instance <see cref="GuiImage"/>, jejíž <see cref="ImageContent"/> obsahuje referenci na dané pole (neprovádí se kopie pole).
        /// </summary>
        /// <param name="imageContent"></param>
        public static implicit operator GuiImage(System.Byte[] imageContent) { return (imageContent != null ? new GuiImage() { ImageContent = imageContent } : null); }
        #endregion
    }
    #endregion
    #region GuiId : Identifikátor čísla třídy a čísla záznamu, použitelný i jako klíč v Dictionary.
    /// <summary>
    /// GuiId : Identifikátor čísla třídy a čísla záznamu, použitelný i jako klíč v Dictionary.
    /// Třída zahrnující <see cref="ClassId"/> + <see cref="RecordId"/>, ale bez omezení 
    /// Tato třída řeší: <see cref="GetHashCode()"/>, <see cref="Equals(object)"/> a přepisuje operátory == a !=
    /// </summary>
    public sealed class GuiId : IXmlSerializer
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="classId">Číslo třídy</param>
        /// <param name="recordId">Číslo záznamu</param>
        public GuiId(int classId, int recordId)
        {
            this.ClassId = classId;
            this.RecordId = recordId;
        }
        /// <summary>
        /// Konstruktor bez parametrů musí existovat kvůli deserialiaci.
        /// </summary>
        public GuiId() { }
        /// <summary>
        /// Číslo třídy
        /// </summary>
        public int ClassId { get; private set; }
        /// <summary>
        /// Číslo záznamu
        /// </summary>
        public int RecordId { get; private set; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "C:" + this.ClassId.ToString() + "; R:" + this.RecordId.ToString();
        }
        /// <summary>
        /// Vrátí HashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (!this._HashCode.HasValue)
                this._HashCode = this.ClassId.GetHashCode() ^ this.RecordId.GetHashCode();
            return this._HashCode.Value;
        }
        /// <summary>
        /// Vrátí true pokud daný objekt je typu <see cref="GuiId"/> a obsahuje shodné hodnoty v <see cref="ClassId"/> a <see cref="RecordId"/>.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return _Equals(this, obj as GuiId);
        }
        /// <summary>
        /// Equals pro dvě instance
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool _Equals(GuiId a, GuiId b)
        {
            bool an = ((object)a == null);
            bool bn = ((object)b == null);
            if (an && bn) return true;
            if (an || bn) return false;
            return (a.ClassId == b.ClassId && a.RecordId == b.RecordId);
        }
        /// <summary>
        /// Lazy initialized HasCode
        /// </summary>
        private int? _HashCode;
        /// <summary>
        /// Operátor == porovná hodnoty v objektech vlevo a vpravo
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(GuiId a, GuiId b) { return _Equals(a, b); }
        /// <summary>
        /// Operátor != porovná hodnoty v objektech vlevo a vpravo
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(GuiId a, GuiId b) { return !_Equals(a, b); }
        /// <summary>
        /// Obsahuje textové vyjádření zdejších dat
        /// </summary>
        public string Name
        {
            get { return this.ClassId.ToString() + ":" + this.RecordId.ToString(); }
            private set
            {
                int classId = 0;
                int recordId = 0;
                bool isValid = false;
                if (!String.IsNullOrEmpty(value) && value.Contains(":"))
                {
                    string[] items = value.Split(':');
                    isValid = (Int32.TryParse(items[0], out classId) && Int32.TryParse(items[1], out recordId));
                }
                this.ClassId = (isValid ? classId : 0);
                this.RecordId = (isValid ? recordId : 0);
            }
        }
        /// <summary>
        /// Tato property má obsahovat (get vrací, set akceptuje) XML data z celého aktuálního objektu.
        /// </summary>
        string IXmlSerializer.XmlSerialData
        {
            get { return this.Name; }
            set { this.Name = value; }
        }
    }
    #endregion
    #region GuiRange : rozsah { Begin ÷ End } dvou hodnot stejného datového typu; class GuiTimeRange, GuiSingleRange
    /// <summary>
    /// GuiRange : rozsah { Begin ÷ End } dvou hodnot stejného datového typu
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class GuiRange<T> : IXmlSerializer where T : IComparable
    {
        #region Konstruktor, Vizualizace, Begin, End, Clear()
        /// <summary>
        /// Bezparametrický konstruktor, pro XML serializaci (Persistor)
        /// </summary>
        protected GuiRange() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public GuiRange(T begin, T end)
        {
            this.Begin = begin;
            this.End = end;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Begin + " ÷ " + this.End;
        }
        /// <summary>
        /// Počátek intervalu, běžně se počítá "včetně"
        /// </summary>
        public T Begin { get; protected set; }
        /// <summary>
        /// Konec intervalu, běžně se počítá "mimo"
        /// </summary>
        public T End { get; protected set; }
        #endregion
        #region Podpora pro serializaci: Clear(), TrySplitPair(), JoinPair()
        /// <summary>
        /// Do <see cref="Begin"/> a <see cref="End"/> vloží defaultní hodnotu generického typu T.
        /// </summary>
        protected void Clear()
        {
            this.Begin = default(T);
            this.End = default(T);
        }
        /// <summary>
        /// Metoda rozdělí dodaný text v místě delimiteru, a do out parametrů uloží text před delimiterem (begin) a za ním (end).
        /// Pokud by text obsahoval více delimiterů, pak další pozice (za druhým delimiterem) budou zahozeny.
        /// Vrací true = text byl v pořádku a je rozdělen, nebo false = text nebyl správně.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        protected bool TrySplitPair(string text, out string begin, out string end)
        {
            begin = null;
            end = null;
            if (String.IsNullOrEmpty(text)) return false;
            if (!text.Contains(DELIMITER)) return false;
            string[] items = text.Split(new string[] { DELIMITER }, StringSplitOptions.None);
            begin = items[0];
            end = (items.Length > 1 ? items[1] : "");
            return true;
        }
        /// <summary>
        /// Vrátí korektně spojený text begin + DELIMITER + end.
        /// Pokud je na vstupu begin nebo end == null, vrací null.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        protected string JoinPair(string begin, string end)
        {
            if (begin == null || end == null) return null;
            return begin + DELIMITER + end;
        }
        /// <summary>
        /// Oddělovač hodnot Begin a End v serializované formě
        /// </summary>
        protected const string DELIMITER = "÷";
        #endregion
        #region IXmlSerializer
        /// <summary>
        /// Explicitní serializace
        /// </summary>
        string IXmlSerializer.XmlSerialData
        {
            get { return JoinPair(GetSerial(this.Begin), GetSerial(this.End)); }
            set
            {
                this.Clear();
                string begin, end;
                if (TrySplitPair(value, out begin, out end))
                {
                    this.Begin = GetValue(begin);
                    this.End = GetValue(end);
                }
            }
        }
        /// <summary>
        /// Vrátí serializovanou formu dané typové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected abstract string GetSerial(T value);
        /// <summary>
        /// Vrátí deserializovanou typovou hodnotu ze serializované formy
        /// </summary>
        /// <param name="serial"></param>
        /// <returns></returns>
        protected abstract T GetValue(string serial);
        #endregion
        #region Override ==  !=
        /// <summary>
        /// GetHashCode()
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.Begin.GetHashCode() ^ this.End.GetHashCode();
        }
        /// <summary>
        /// Equals() - pro použití GID v Hashtabulkách
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return (_IsEqual(this, obj as GuiRange<T>));
        }
        /// <summary>
        /// Porovnání dvou instancí této struktury, zda obsahují shodná data
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool _IsEqual(GuiRange<T> a, GuiRange<T> b)
        {
            bool an = ((object)a) == null;
            bool bn = ((object)b) == null;
            if (an && bn) return true;           // null == null
            if (an || bn) return false;          // (any object) != null
            return (a.Begin.CompareTo(b.Begin) == 0 && a.End.CompareTo(b.End) == 0);
        }
        /// <summary>
        /// Operátor "je rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(GuiRange<T> a, GuiRange<T> b)
        {
            return _IsEqual(a, b);
        }
        /// <summary>
        /// Operátor "není rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(GuiRange<T> a, GuiRange<T> b)
        {
            return !_IsEqual(a, b);
        }
        #endregion
    }
    /// <summary>
    /// GuiTimeRange : rozsah { Begin ÷ End } dvou hodnot typu <see cref="DateTime"/>
    /// </summary>
    public class GuiTimeRange : GuiRange<DateTime>, IXmlSerializer
    {
        #region Konstruktory
        /// <summary>
        /// Bezparametrický konstruktor, pro XML serializaci (Persistor)
        /// </summary>
        protected GuiTimeRange() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public GuiTimeRange(DateTime begin, DateTime end) : base(begin, end) { }
        #endregion
        #region Abstract overrides
        /// <summary>
        /// Vrátí serializovanou formu dané typové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override string GetSerial(DateTime value) { return Convertor.DateTimeToString(value); }
        /// <summary>
        /// Vrátí deserializovanou typovou hodnotu ze serializované formy
        /// </summary>
        /// <param name="serial"></param>
        /// <returns></returns>
        protected override DateTime GetValue(string serial) { return (DateTime)Convertor.StringToDateTime(serial); }
        #endregion
    }
    /// <summary>
    /// GuiSingleRange : rozsah { Begin ÷ End } dvou hodnot typu <see cref="Single"/>
    /// </summary>
    public class GuiSingleRange : GuiRange<Single>, IXmlSerializer
    {
        #region Konstruktory
        /// <summary>
        /// Bezparametrický konstruktor, pro XML serializaci (Persistor)
        /// </summary>
        protected GuiSingleRange() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public GuiSingleRange(Single begin, Single end) : base(begin, end) { }
        #endregion
        #region Abstract overrides
        /// <summary>
        /// Vrátí serializovanou formu dané typové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override string GetSerial(Single value) { return Convertor.SingleToString(value); }
        /// <summary>
        /// Vrátí deserializovanou typovou hodnotu ze serializované formy
        /// </summary>
        /// <param name="serial"></param>
        /// <returns></returns>
        protected override Single GetValue(string serial) { return (Single)Convertor.StringToSingle(serial); }
        #endregion
    }
    /// <summary>
    /// Podpora pro vyjádření strany (<see cref="Begin"/> a <see cref="End"/>) a pohybu (<see cref="Prev"/> a <see cref="Next"/>).
    /// Strany a orientace
    /// </summary>
    public enum GuiSide : int
    {
        /// <summary>
        /// Begin: začátek
        /// </summary>
        Begin = -2,
        /// <summary>
        /// Prev: směr k začátku
        /// </summary>
        Prev = -1,
        /// <summary>
        /// None: bez pohybu, střed
        /// </summary>
        None = 0,
        /// <summary>
        /// Next: směr ke konci
        /// </summary>
        Next = 1,
        /// <summary>
        /// End: konec
        /// </summary>
        End = 2
    }
    #endregion
    #endregion
    #region GuiRequest : data předávaná z WorkScheduler do Helios Green jako součást požadavku
    /// <summary>
    /// GuiRequest : data předávaná z WorkScheduler do Helios Green jako součást požadavku.
    /// GUI vrstva pluginu WorkScheduler (GraphLibrary) tato data vygeneruje ze svého aktuálního stavu, a předá je do klientského pluginu ASOL.WorkScheduler.
    /// Klientský plugin (v Connector.cs) vyvolá servisní funkci, a do ní předá serializovaný obraz této instance (do ServiceGateUserData).
    /// Servisní funkce si převezme serializovaný obraz a deserializuje zpět instanci <see cref="GuiRequest"/>, najde její <see cref="GuiRequest.Command"/>
    /// a vyvolá odpovídající metodu. Ta zajistí zpracování požadavku, a nakonec vytvoří new instanci třídy <see cref="GuiResponse"/>, do které vloží výsledná data.
    /// Ta se vracejí ze servisní funkce v serializované podobě (v ServiceGateUserData) zpátky do klientského pluginu.
    /// Klientský plugin data najde, deserializuje, spojí je s původním požadavkem <see cref="GuiRequest"/> a předá do GUI vrstvy pluginu WorkScheduler (GraphLibrary).
    /// Tam pak může dojít k požadované reakci, například přemístění prvku grafu, nebo změna jeho charakteru.
    /// </summary>
    public class GuiRequest
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiRequest() { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (String.IsNullOrEmpty(this.Command)) return "Undefined command.";
            string text = "Command: " + this.Command;
            switch (this.Command)
            {
                case COMMAND_OpenRecords:
                    return text + ((this.RecordsToOpen != null && this.RecordsToOpen.Length > 0) ? "; Record: " + this.RecordsToOpen[0].ToString() : "; no RecordsToOpen");
                case COMMAND_ToolbarClick:
                    return text + ((this.ToolbarItem != null) ? "; Item: " + this.ToolbarItem.ToString() : "; ToolbarItem is null");
                case COMMAND_ContextMenuClick:
                    return text + ((this.ContextMenuItem != null) ? "; Item: " + this.ContextMenuItem.ToString() : "; ContextMenuItem is null");
                case COMMAND_GraphItemMove:
                    return text + ((this.GraphItemMove != null) ? "; Item: " + this.GraphItemMove.ToString() : "; GraphItemMove is null");
                case COMMAND_GraphItemResize:
                    return text + ((this.GraphItemResize != null) ? "; Item: " + this.GraphItemResize.ToString() : "; GraphItemResize is null");


                case COMMAND_QueryCloseWindow:
                    return text;
                case COMMAND_CloseWindow:
                    return text;
            }
            return base.ToString();
        }
        /// <summary>
        /// Požadovaná akce, typicky některá z konstant v <see cref="GuiRequest"/>
        /// </summary>
        public string Command { get; set; }
        /// <summary>
        /// Pole záznamů k otevření
        /// </summary>
        public GuiId[] RecordsToOpen { get; set; }
        /// <summary>
        /// Položka Toolbaru, která vyvolala akci
        /// </summary>
        public GuiToolbarItem ToolbarItem { get; set; }
        /// <summary>
        /// Kontextová funkce, která vyvolala akci
        /// </summary>
        public GuiContextMenuItem ContextMenuItem { get; set; }
        /// <summary>
        /// Aktivní prvek grafu, jehož se akce týká (buď kliknutí na kontextové menu, nebo interaktivní změna prvku = přesouvání / resize)
        /// </summary>
        public GuiGridItemId ActiveGraphItem { get; set; }
        /// <summary>
        /// Informace o přesunu prvku (který je přesouván procesem Drag and Drop) na jiné místo: prvek, původní a nové umístění v čase a prostoru
        /// </summary>
        public GuiRequestGraphItemMove GraphItemMove { get; set; }
        /// <summary>
        /// Informace o změně velikosti prvku (který je upravován procesem Drag and Drop) na jinou velikost: prvek, původní a nová velikost
        /// </summary>
        public GuiRequestGraphItemResize GraphItemResize { get; set; }
        /// <summary>
        /// Aktuální stav okna WorkScheduler
        /// </summary>
        public GuiRequestCurrentState CurrentState { get; set; }
        #region Konstanty - commandy
        /// <summary>
        /// Otevřít záznamy.
        /// Objekt <see cref="GuiRequest"/> nese záznamy k otevření v property <see cref="GuiRequest.RecordsToOpen"/>.
        /// </summary>
        public const string COMMAND_OpenRecords = "OpenRecords";
        /// <summary>
        /// Bylo kliknuto na tlačítko Toolbaru, které obsahuje jednoduchou funkci.
        /// Objekt <see cref="GuiRequest"/> nese informaci o konkrétním tlačítku toolbaru v property <see cref="GuiRequest.ToolbarItem"/> 
        /// </summary>
        public const string COMMAND_ToolbarClick = "ToolbarClick";
        /// <summary>
        /// Bylo kliknuto na kontextovou funkci.
        /// Objekt <see cref="GuiRequest"/> nese informaci o konkrétní kontextové funkci v property <see cref="GuiRequest.ToolbarItem"/> 
        /// </summary>
        public const string COMMAND_ContextMenuClick = "ContextMenuClick";
        /// <summary>
        /// Byl přemístěn prvek grafu.
        /// Objekt <see cref="GuiRequest"/> nese informaci o konkrétním aktivním prvku grafu v property <see cref="GuiRequest.ActiveGraphItem"/>,
        /// a informaci o pohybu prvku v property <see cref="GuiRequest.GraphItemMove"/> (sada prvků v pohybu, výchozí řádek a čas, cílový řádek a čas).
        /// Kompletní údaje o stavu GUI jsou v <see cref="GuiRequest.CurrentState"/>.
        /// Aplikační servisní funkce může určit správnější cílové umístění na základě rozsáhlejších informací a algoritmů, než má k dispozici plugin.
        /// </summary>
        public const string COMMAND_GraphItemMove = "GraphItemMove";
        /// <summary>
        /// Byl změněn prvek grafu (změněna byla jeho šířka / výška).
        /// Objekt <see cref="GuiRequest"/> nese informaci o konkrétním aktivním prvku grafu v property <see cref="GuiRequest.ActiveGraphItem"/>,
        /// a informaci o změně prvku v property <see cref="GuiRequest.GraphItemResize"/> (změněný prvek, výchozí výška nebo čas, cílová výška nebo čas).
        /// Kompletní údaje o stavu GUI jsou v <see cref="GuiRequest.CurrentState"/>.
        /// Aplikační servisní funkce může zareagovat úpravou svých dat, a případně přeplánováním.
        /// </summary>
        public const string COMMAND_GraphItemResize = "GraphItemResize";


        /// <summary>
        /// Test před zavřením okna.
        /// Nepředávají se žádná upřesňující data.
        /// Jako odpověď se očekává <see cref="GuiResponse.Message"/>: pokud bude neprázdné, jde o dotaz před ukončením. 
        /// V tom případě se zobrazí dialog podle <see cref="GuiResponse.Dialog"/>.
        /// Pokud bude <see cref="GuiResponse.Message"/> prázdné, dialog nebude, okno se zavře.
        /// Při zavření okna se odešle command <see cref="COMMAND_CloseWindow"/>.
        /// </summary>
        public const string COMMAND_QueryCloseWindow = "QueryCloseWindow";
        /// <summary>
        /// Zavírá se okno už doopravdy.
        /// Nepředávají se žádná upřesňující data.
        /// Vyšší aplikace si má zahodit svoje data svázaná s tímto pluginem.
        /// </summary>
        public const string COMMAND_CloseWindow = "CloseWindow";
        #endregion
    }
    /// <summary>
    /// Informace o přesunu prvku (který je přesouván procesem Drag and Drop) na jiné místo: prvek, původní a nové umístění v čase a prostoru
    /// </summary>
    public class GuiRequestGraphItemMove
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiRequestGraphItemMove()
        { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "MoveItems: ";
            if (this.MoveItems != null)
            {
                int count = this.MoveItems.Length;
                if (count > 0)
                    text += this.MoveItems[0].ToString();
                if (count > 1)
                    text += ", ...";
            }
            else
                text += "{Null}";

            if (this.SourceRow != null && this.TargetRow != null && this.SourceRow != this.TargetRow)
                text += "; Change Row; From: " + this.SourceRow.ToString() + "; To: " + this.TargetRow.ToString();
            if (this.SourceTime != null && this.TargetTime != null && this.SourceTime != this.TargetTime)
                text += "; Change Time; From: " + this.SourceTime.ToString() + "; To: " + this.TargetTime.ToString();
            return text;
        }
        /// <summary>
        /// Soubor prvků, které jsou přesouvány.
        /// V této property se nacházejí všechny prvky jedné skupiny <see cref="GuiGraphBaseItem.GroupId"/>, neboť přesouvání se provádí vždy pro celé skupiny.
        /// Pokud prvek grafu nepatří do žádné skupiny (jeho <see cref="GuiGraphBaseItem.GroupId"/> je null), pak tvoří svoji vlastní soukromou skupinu, a přesouvá se sám.
        /// </summary>
        public GuiGridItemId[] MoveItems { get; set; }
        /// <summary>
        /// Zdrojový řádek, před přesouváním.
        /// </summary>
        public GuiId SourceRow { get; set; }
        /// <summary>
        /// Výchozí čas prvku, před přesouváním.
        /// Pokud prvek patří do skupiny (<see cref="MoveItems"/> obsahuje více než jeden prvek), pak je zde v <see cref="SourceTime"/> sumární čas celé skupiny.
        /// </summary>
        public GuiTimeRange SourceTime { get; set; }
        /// <summary>
        /// Umístění "Pevného bodu" na prvku Source.
        /// Jako "Pevný bod" se bere buď začátek, nebo konec prvku. 
        /// Tento (časový) okamžik z údaje <see cref="TargetTime"/> by se měl při aplikačním přeplánování zachovat na požadované hodnotě (pokud možno),
        /// a opačný údaj se může upravit.
        /// "Pevný bod" je například ten čas (Begin nebo End), který při interaktivním pohybu byl "Přichycen" k sousednímu prvku.
        /// </summary>
        public GuiSide MoveFixedPoint { get; set; }
        /// <summary>
        /// Cílový řádek (tam by to uživatel rád umístil)
        /// </summary>
        public GuiId TargetRow { get; set; }
        /// <summary>
        /// Cílový čas (tam by to uživatel rád umístil).
        /// Jedná se o čas <see cref="SourceTime"/>, posunutý na jiné místo, beze změny délky časového intervalu.
        /// </summary>
        public GuiTimeRange TargetTime { get; set; }
    }
    /// <summary>
    /// Informace o změně velikosti prvku (který je upravován procesem Drag and Drop) na jinou velikost: prvek, původní a nová velikost
    /// </summary>
    public class GuiRequestGraphItemResize
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiRequestGraphItemResize()
        { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "ResizeItem: ";
            if (this.ResizeItem != null)
                text += this.ResizeItem.ToString();
            else
                text += "{Null}";

            if (this.SourceHeight != null && this.TargetHeight != null && this.SourceHeight != this.TargetHeight)
                text += "; Resize Height; From: " + this.SourceHeight.ToString() + "; To: " + this.TargetHeight.ToString();
            if (this.SourceTime != null && this.TargetTime != null && this.SourceTime != this.TargetTime)
                text += "; Resize Time; From: " + this.SourceTime.ToString() + "; To: " + this.TargetTime.ToString();
            return text;
        }
        /// <summary>
        /// Prvek, kterého se Resize týká
        /// </summary>
        public GuiGridItemId ResizeItem { get; set; }
        /// <summary>
        /// Původní výška prvku grafu (od-do v grafu).
        /// </summary>
        public GuiSingleRange SourceHeight { get; set; }
        /// <summary>
        /// Výchozí čas prvku, před změnou.
        /// </summary>
        public GuiTimeRange SourceTime { get; set; }
        /// <summary>
        /// Cílová výška prvku grafu (tam by to uživatel rád natáhl).
        /// Pokud se změna týká šířky prvku, pak je zde null.
        /// Pokud je zde hodnota, pak se může lišit od <see cref="SourceHeight"/> buď v hodnotě Begin, nebo End (nelze najednou měnit obě hodnoty, to by bylo Move a ne Resize).
        /// </summary>
        public GuiSingleRange TargetHeight { get; set; }
        /// <summary>
        /// Cílový čas (tam by to uživatel rád natáhl).
        /// Pokud se změna týká výšky prvku, pak je zde null.
        /// Pokud je zde hodnota, pak se může lišit od <see cref="SourceTime"/> buď v hodnotě Begin, nebo End (nelze najednou měnit obě hodnoty, to by bylo Move a ne Resize).
        /// </summary>
        public GuiTimeRange TargetTime { get; set; }
    }
    /// <summary>
    /// GuiCurrentState : stav okna Scheduleru v době události.
    /// Popisuje mj. vybrané položky grafů a vybrané řádky tabulek.
    /// Neobsahuje informace o aktuální akci, ty jsou přidány do specifických properties přímo do instance třídy <see cref="GuiRequest"/>.
    /// </summary>
    public class GuiRequestCurrentState
    {
        /// <summary>
        /// Aktuální hodnota časové osy
        /// </summary>
        public GuiTimeRange TimeAxisValue { get; set; }
        /// <summary>
        /// Aktivní řádek, je nanejvýše jeden
        /// </summary>
        public GuiGridRowId ActiveRow { get; set; }
        /// <summary>
        /// Aktuálně označené řádky tabulek v aktuálním okně.
        /// Řádky tabulek lze označovat ikonkou.
        /// </summary>
        public GuiGridRowId[] SelectedRows { get; set; }
        /// <summary>
        /// Aktuálně označené prvky grafů tabulek v aktuálním okně.
        /// Prvky grafů lze označovat klikáním nebo framováním.
        /// </summary>
        public GuiGridItemId[] SelectedGraphItems { get; set; }
    }
    /// <summary>
    /// Plný identifikátor prvku tabulky:
    /// obsahuje FullName tabulky, <see cref="GuiId"/> řádku tabulky, a pokud se jedná o prvek grafu, pak i jeho identifikátor.
    /// Umožňuje aplikaci najít data tohoto řádku na základě všech identifikátorů.
    /// </summary>
    public class GuiGridItemId : GuiGridRowId
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiGridItemId()
        { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = base.ToString();
            if (this.ItemId != null)
                text += "; ItemId: " + this.ItemId.ToString();
            if (this.GroupId != null)
                text += "; GroupId: " + this.GroupId.ToString();
            return text;
        }
        /// <summary>
        /// <see cref="GuiId"/> prvku grafu, jeho GroupId, pochází z <see cref="GuiGraphBaseItem.GroupId"/>
        /// </summary>
        public GuiId GroupId { get; set; }
        /// <summary>
        /// <see cref="GuiId"/> prvku grafu, jeho ItemId, pochází z <see cref="GuiGraphBaseItem.ItemId"/>
        /// </summary>
        public GuiId ItemId { get; set; }
        /// <summary>
        /// <see cref="GuiId"/> prvku grafu, jeho DataId, pochází z <see cref="GuiGraphBaseItem.DataId"/>
        /// </summary>
        public GuiId DataId { get; set; }
    }
    /// <summary>
    /// Plný identifikátor řádku tabulky:
    /// obsahuje FullName tabulky a <see cref="GuiId"/> řádku tabulky.
    /// </summary>
    public class GuiGridRowId
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiGridRowId()
        { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "TableName: " + (this.TableName != null ? this.TableName : "{Null}");
            if (this.RowId != null)
                text += "; RowId: " + this.RowId.ToString();
            return text;
        }
        /// <summary>
        /// Název tabulky, pochází z <see cref="GuiGrid"/>.FullName
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// <see cref="GuiId"/> řádku tabulky, pochází z prvního sloupce tabulky <see cref="GuiGrid.Rows"/>, v kombinaci s číslem třídy v properties sloupce [0]
        /// </summary>
        public GuiId RowId { get; set; }
    }
    #endregion
    #region GuiResponse : data předávaná z Helios Green do WorkScheduler jako součást response
    /// <summary>
    /// GuiResponse : data předávaná z Helios Green do WorkScheduler jako součást response
    /// </summary>
    public class GuiResponse
    {
        #region Konstrukce, základní stavové property: ResponseState, Message, Dialog
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiResponse() { }
        /// <summary>
        /// Stav dokončení funkce
        /// </summary>
        public GuiResponseState ResponseState { get; set; }
        /// <summary>
        /// Textová zpráva uživateli.
        /// Používá se například při testu na zavření okna WorkScheduleru, 
        /// obsahuje typicky: "Data jsou změněna. Přijdete o ně. Zavřít?"
        /// Anebo se plní při stavu Warning nebo Error.
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Možnosti dialogu s uživatelem.
        /// Používá se například při testu na zavření okna WorkScheduleru, pro <see cref="Message"/> obsahuje hodnoty <see cref="GuiDialogResponse.YesNo"/>.
        /// </summary>
        public GuiDialogResponse Dialog { get; set; }
        #endregion
        #region Data, která se promítají do GUI: ToolbarItems, TimeAxisValue, RemoveItems, AddItems
        /// <summary>
        /// Pole prvků Toolbaru, na nichž mohlo dojít ke změně.
        /// Lze změnit pouze ty položky, které byly při inicializaci GUI deklarovány.
        /// Položky se identifikují podle shody jména prvku: <see cref="GuiBase.Name"/>.
        /// <para/>
        /// Touto formou může aplikační kód provádět změny některých vlastností existujících položek Toolbaru, ale pouze těch které lze provést do již umístěných položek Toolbaru.
        /// Touto formou nelze přidat ani odebrat prvky Toolbaru (ale je možno změnit jejich viditelnost <see cref="GuiToolbarItem.Visible"/>, a tak je vizuálně "přidat" nebo "odebrat").
        /// Nelze tedy změnit příslušnost prvku do grupy.
        /// Řeší hodnoty pro tyto properties:
        /// <see cref="GuiTextItem.Title"/>, <see cref="GuiTextItem.ToolTip"/>, <see cref="GuiTextItem.Image"/>, <see cref="GuiToolbarItem.ImageHot"/>, 
        /// <see cref="GuiToolbarItem.Visible"/>, <see cref="GuiToolbarItem.Enable"/>, <see cref="GuiToolbarItem.Size"/>, 
        /// <see cref="GuiToolbarItem.ModuleWidth"/>, <see cref="GuiToolbarItem.LayoutHint"/>.
        /// </summary>
        public GuiToolbarItem[] ToolbarItems { get; set; }
        /// <summary>
        /// Požadovaná hodnota časové osy. 
        /// Bude aplikována, pokud není null.
        /// Hodnota bude vždy zarovnána do <see cref="GuiProperties.TotalTimeRange"/>
        /// </summary>
        public GuiTimeRange TimeAxisValue { get; set; }
        /// <summary>
        /// Pole prvků grafů, které se mají z GUI odebrat.
        /// Jednotlivé prvky obsahují název cílové tabulky v <see cref="GuiGridRowId.TableName"/>.
        /// V tomto seznamu tedy mohou být prvky pocházející z kterékoli tabulky GUI.
        /// </summary>
        public GuiGridItemId[] RemoveItems { get; set; }
        /// <summary>
        /// Pole prvků grafů, které se mají do GUI nově vložit.
        /// Jednotlivé prvky obsahují název cílové tabulky v <see cref="GuiGridGraphItem.TableName"/>.
        /// V tomto seznamu tedy mohou být prvky do kterékoli tabulky GUI.
        /// </summary>
        public GuiGridGraphItem[] AddItems { get; set; }
        #endregion
        #region Statické konstruktory: Success, Warning, Error
        /// <summary>
        /// Statický konstruktor, který vrací new instanci <see cref="GuiResponse"/>, kde <see cref="GuiResponse.ResponseState"/> = <see cref="GuiResponseState.Success"/>.
        /// </summary>
        public static GuiResponse Success() { return new GuiResponse() { ResponseState = GuiResponseState.Success }; }
        /// <summary>
        /// Statický konstruktor, který vrací new instanci <see cref="GuiResponse"/>, kde <see cref="GuiResponse.ResponseState"/> = <see cref="GuiResponseState.Warning"/>, a nastaví danou zprávu do <see cref="Message"/>.
        /// </summary>
        public static GuiResponse Warning(string message) { return new GuiResponse() { ResponseState = GuiResponseState.Warning, Message = message }; }
        /// <summary>
        /// Statický konstruktor, který vrací new instanci <see cref="GuiResponse"/>, kde <see cref="GuiResponse.ResponseState"/> = <see cref="GuiResponseState.Error"/>, a nastaví danou zprávu do <see cref="Message"/>.
        /// </summary>
        public static GuiResponse Error(string message) { return new GuiResponse() { ResponseState = GuiResponseState.Error, Message = message }; }
        #endregion
    }
    /// <summary>
    /// GuiGridGraphItem : třída pro přenos prvků grafu (data z <see cref="GuiGraphBaseItem"/> z aplikace do GUI v nestrukturovaném seznamu.
    /// To znamená, že v jednom seznamu prvků jsou prvky patřídíc do různých tabulek.
    /// Používá se po editaci prvků, pro přenos souhrnu změn z aplikace do GUI.
    /// </summary>
    public class GuiGridGraphItem : GuiGraphBaseItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiGridGraphItem() : base() { }
        /// <summary>
        /// FullName tabulky, do které se má tento prvek vložit.
        /// </summary>
        public string TableName { get; set; }
    }
    /// <summary>
    /// Stav dokončení funkce
    /// </summary>
    public enum GuiResponseState
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None = 0,
        /// <summary>
        /// S úspěchem
        /// </summary>
        Success,
        /// <summary>
        /// S varováním
        /// </summary>
        Warning,
        /// <summary>
        /// S chybou
        /// </summary>
        Error
    }
    /// <summary>
    /// Dialog, možnosti a odpovědi
    /// </summary>
    [Flags]
    public enum GuiDialogResponse
    {
        /// <summary>
        /// Žádný dialog
        /// </summary>
        None = 0,
        /// <summary>
        /// Tlačítko OK
        /// </summary>
        Ok = 0x0001,
        /// <summary>
        /// Tlačítko ANO
        /// </summary>
        Yes = 0x0002,
        /// <summary>
        /// Tlačítko NE
        /// </summary>
        No = 0x0004,
        /// <summary>
        /// Tlačítko STORNO
        /// </summary>
        Cancel = 0x0008,
        /// <summary>
        /// Tlačítko ZNOVU
        /// </summary>
        Retry = 0x0010,
        /// <summary>
        /// Tlačítko MOŽNÁ
        /// </summary>
        Maybe = 0x1000,
        /// <summary>
        /// Tlačítka OK a STORNO
        /// </summary>
        OkCancel = Ok | Cancel,
        /// <summary>
        /// Tlačítka ANO a NE
        /// </summary>
        YesNo = Yes | No
    }
    #endregion
    #region Enumy, které se sdílí mezi WorkScheduler a GraphLibrary
    // VAROVÁNÍ : Změna názvu jednotlivých enumů je zásadní změnou, která se musí promítnout i do konstant ve WorkSchedulerSupport a to jak zde, tak v Greenu.
    //            Hodnoty se z Greenu předávají v textové formě, a tady v GUI se z textu získávají parsováním (Enum.TryParse()) !

    /// <summary>
    /// Typ prvku na ToolBaru
    /// </summary>
    public enum FunctionGlobalItemType
    {
        /// <summary>
        /// Nezadáno
        /// </summary>
        None,
        /// <summary>
        /// Oddělovač podskupin v rámci jedné grupy
        /// </summary>
        Separator,
        /// <summary>
        /// Textový popisek
        /// </summary>
        Label,
        /// <summary>
        /// Tlačítko
        /// </summary>
        Button,
        /// <summary>
        /// ComboBox
        /// </summary>
        ComboBox,
        /// <summary>
        /// Obrázek
        /// </summary>
        Image
    }
    /// <summary>
    /// Velikost prvku na toolbaru. Lze chápat jako počet prvků dané velikosti, které zaplní toolbar na výšku.
    /// </summary>
    public enum FunctionGlobalItemSize : int
    {
        /// <summary>
        /// Prvek není zobrazován
        /// </summary>
        None = 0,
        /// <summary>
        /// Mikro prvek výšky 1. Zobrazuje se pouze ikona. Text je ignorován.
        /// </summary>
        Micro = 1,
        /// <summary>
        /// Malý prvek, výška 1/3 toolbaru.
        /// </summary>
        Small = 2,
        /// <summary>
        /// Půlprvek, výšky 1/2 toolbaru.
        /// </summary>
        Half = 3,
        /// <summary>
        /// Velký prvek, výšky 2/3 toolbaru.
        /// </summary>
        Large = 4,
        /// <summary>
        /// Prvek přes celý toolbar
        /// </summary>
        Whole = 6
    }
    /// <summary>
    /// Požadavky na řízení layoutu
    /// </summary>
    [Flags]
    public enum LayoutHint
    {
        /// <summary>
        /// Necháme to na automatu
        /// </summary>
        Default = 0,

        /// <summary>
        /// Tento prvek musí být na témže řádku, jako prvek předešlý (tedy pokud má shodnou výšku, jinak je hint ignorován)
        /// </summary>
        ThisItemOnSameRow = 0x0001,
        /// <summary>
        /// Tento prvek musí být na novém řádku (nebo novém odstavci, pokud by se nový řádek nevešel)
        /// </summary>
        ThisItemSkipToNextRow = 0x0002,
        /// <summary>
        /// Tento prvek musí být vždy na novém odstavci (jako za separátorem)
        /// </summary>
        ThisItemSkipToNextTable = 0x0004,

        /// <summary>
        /// Následující prvek musí být na témže řádku, jako prvek tento (tedy pokud má shodnou výšku, jinak je hint ignorován)
        /// </summary>
        NextItemOnSameRow = 0x0010,
        /// <summary>
        /// Následující prvek musí být na novém řádku (nebo novém odstavci, pokud by se nový řádek nevešel)
        /// </summary>
        NextItemSkipToNextRow = 0x0020,
        /// <summary>
        /// Následující prvek musí být na novém odstavci
        /// </summary>
        NextItemSkipToNextTable = 0x0040
    }
    /// <summary>
    /// Položky systémového menu, které mají být zobrazeny
    /// </summary>
    [Flags]
    public enum ToolbarSystemItem : UInt64
    {
        /// <summary>
        /// Nezadáno
        /// </summary>
        None = 0,
        /// <summary>
        /// Časová osa, Zoom: Jedna hodina
        /// </summary>
        TimeAxisZoomHour = 0x00000001,
        /// <summary>
        /// Časová osa, Zoom: Půl dne
        /// </summary>
        TimeAxisZoomHalfDay = 0x00000002,
        /// <summary>
        /// Časová osa, Zoom: Celý den
        /// </summary>
        TimeAxisZoomOneDay = 0x00000004,
        /// <summary>
        /// Časová osa, Zoom: Pracovní týden (Po-Pá)
        /// </summary>
        TimeAxisZoomWorkWeek = 0x00000008,
        /// <summary>
        /// Časová osa, Zoom: Celý týden (Po-Ne)
        /// </summary>
        TimeAxisZoomWholeWeek = 0x00000010,
        /// <summary>
        /// Časová osa, Zoom: Dekáda (deset dní)
        /// </summary>
        TimeAxisZoomDayDecade = 0x00000020,
        /// <summary>
        /// Časová osa, Zoom: Měsíc
        /// </summary>
        TimeAxisZoomMonth = 0x00000040,
        /// <summary>
        /// Časová osa, Zoom: Tři měsíce = čtvrtletí
        /// </summary>
        TimeAxisZoomQuarter = 0x00000080,
        /// <summary>
        /// Časová osa, Zoom: Šest měsíců = půl roku
        /// </summary>
        TimeAxisZoomHalfYear = 0x00000100,
        /// <summary>
        /// Časová osa, Zoom: Celý rok
        /// </summary>
        TimeAxisZoomWholeYear = 0x00000200,
        /// <summary>
        /// Časová osa, Zoom: Den + Pracovní týden + Celý týden + Měsíc
        /// </summary>
        TimeAxisZoomDWWM = TimeAxisZoomOneDay | TimeAxisZoomWorkWeek | TimeAxisZoomWholeWeek | TimeAxisZoomMonth,
        /// <summary>
        /// Časová osa, Zoom: úplně všechno
        /// </summary>
        TimeAxisZoomAll = 0x000003FF,
        /// <summary>
        /// Časová osa, Přejdi na: minulá stránka
        /// </summary>
        TimeAxisGoPrev = 0x00001000,
        /// <summary>
        /// Časová osa, Přejdi na: aktuální datum
        /// </summary>
        TimeAxisGoHome = 0x00002000,
        /// <summary>
        /// Časová osa, Přejdi na: budoucí stránka
        /// </summary>
        TimeAxisGoNext = 0x00004000,
        /// <summary>
        /// Časová osa, Přejdi na: vše (minulá, aktuální, budoucí)
        /// </summary>
        TimeAxisGoAll = TimeAxisGoPrev | TimeAxisGoHome | TimeAxisGoNext,
        /// <summary>
        /// Časová osa, souhrn všech akcí
        /// </summary>
        TimeAxisAll = TimeAxisZoomAll | TimeAxisGoAll,
        /// <summary>
        /// Přesouvání prvku: přichytávat k nejbližším prvkům
        /// </summary>
        MoveItemSnapToNearItems = 0x00010000,
        /// <summary>
        /// Přesouvání prvku: přichytávat k původnímu času
        /// </summary>
        MoveItemSnapToOriginalTime = 0x00020000,
        /// <summary>
        /// Přesouvání prvku: přichytávat k zaokrouhlenému času
        /// </summary>
        MoveItemSnapToRoundTimeGrid = 0x00040000,
        /// <summary>
        /// Přesouvání prvku: všechny akce
        /// </summary>
        MoveItemAll = MoveItemSnapToNearItems | MoveItemSnapToOriginalTime | MoveItemSnapToRoundTimeGrid,
        /// <summary>
        /// Defaultní využití systémových položek: Zoom (Day + WorkWeek + WholeWeek + Month) + GoAll + MoveItemAll
        /// </summary>
        Default = TimeAxisZoomDWWM | TimeAxisGoAll | MoveItemAll
    }
    /// <summary>
    /// Režim, jak osa reaguje na změnu velikosti.
    /// Pokud osa obsahuje data pro rozsah { 100 ÷ 150 } a má velikost 50 pixelů, 
    /// pak po změně velikosti osy na 100 pixelů může dojít k jedné ze dvou akcí: změna rozsahu, nebo změna měřítka.
    /// a) změní se zobrazený rozsah, a zachová se měřítko (to je defaultní chování), pak 
    /// </summary>
    public enum AxisResizeContentMode
    {
        /// <summary>
        /// Neurčeno, v případě nutnosti se použije ChangeValue
        /// </summary>
        None,
        /// <summary>
        /// Změna hodnoty End:
        /// Pokud osa ve výchozím stavu zobrazuje data pro rozsah { 100 ÷ 150 } a má velikost 50 pixelů, 
        /// pak po změně velikosti osy na 100 pixelů se zachová měřítko (1:1), a zvětší se rozsah zobrazených dat tak, 
        /// že osa bude nově zobrazovat data pro rozsah { 100 ÷ 200 }.
        /// </summary>
        ChangeValueEnd,
        /// <summary>
        /// Změna měřítka:
        /// Pokud osa ve výchozím stavu zobrazuje data pro rozsah { 100 ÷ 150 } a má velikost 50 pixelů, 
        /// pak po změně velikosti osy na 100 pixelů se ponechá rozsah zobrazených hodnot (stále bude zobrazen rozsah dat { 100 ÷ 150 }),
        /// ale upraví se měřítko tak, že osa bude zobrazovat více detailů (z měřítka 1:1 bude 2:1).
        /// </summary>
        ChangeScale
    }
    /// <summary>
    /// Režim, jak může uživatel interaktivně (myší) měnit hodnotu na ose.
    /// </summary>
    [Flags]
    public enum AxisInteractiveChangeMode
    {
        /// <summary>
        /// Uživatel interaktivně (myší) NESMÍ měnit hodnotu na ose ani posunutím, ani změnou měřítka.
        /// </summary>
        None = 0,
        /// <summary>
        /// Uživatel interaktivně (myší) SMÍ měnit hodnotu na ose posunutím.
        /// </summary>
        Shift = 1,
        /// <summary>
        /// Uživatel interaktivně (myší) SMÍ měnit hodnotu na ose změnou měřítka.
        /// </summary>
        Zoom = 2,
        /// <summary>
        /// Uživatel interaktivně (myší) SMÍ měnit hodnotu na ose jak posunutím, tak i změnou měřítka.
        /// </summary>
        All = Shift | Zoom
    }
    /// <summary>
    /// Režimy chování položky grafu. Zahrnují možnosti editace a možnosti zobrazování textu, tooltipu a vztahů.
    /// Editovatelnost položky grafu.
    /// </summary>
    [Flags]
    public enum GraphItemBehaviorMode : int
    {
        /// <summary>
        /// Bez zadání
        /// </summary>
        None = 0,
        /// <summary>
        /// Lze změnit délku času (roztáhnout šířku pomocí přesunutí začátku nebo konce)
        /// </summary>
        ResizeTime = 0x01,
        /// <summary>
        /// Lze změnit výšku = obsazený prostor v grafu (roztáhnout výšku)
        /// </summary>
        ResizeHeight = 0x02,
        /// <summary>
        /// Lze přesunout položku grafu na ose X = čas doleva / doprava
        /// </summary>
        MoveToAnotherTime = 0x10,
        /// <summary>
        /// Lze přesunout položku grafu na ose Y = na jiný řádek tabulky
        /// </summary>
        MoveToAnotherRow = 0x20,
        /// <summary>
        /// Lze přesunout položku grafu do jiné tabulky
        /// </summary>
        MoveToAnotherTable = 0x40,
        /// <summary>
        /// Nezobrazovat text v prvku nikdy.
        /// Toto je explicitní hodnota; ale shodné chování bude použito i když nebude specifikována žádná jiná hodnota ShowCaption*.
        /// </summary>
        ShowCaptionNone = 0x1000,
        /// <summary>
        /// Zobrazit text v prvku při stavu MouseOver.
        /// Pokud nebude specifikována hodnota <see cref="ShowCaptionInMouseOver"/> ani <see cref="ShowCaptionInSelected"/> ani <see cref="ShowCaptionAllways"/>, nebude se zobrazovat text v prvku vůbec.
        /// </summary>
        ShowCaptionInMouseOver = 0x2000,
        /// <summary>
        /// Zobrazit text v prvku při stavu Selected.
        /// Pokud nebude specifikována hodnota <see cref="ShowCaptionInMouseOver"/> ani <see cref="ShowCaptionInSelected"/> ani <see cref="ShowCaptionAllways"/>, nebude se zobrazovat text v prvku vůbec.
        /// </summary>
        ShowCaptionInSelected = 0x4000,
        /// <summary>
        /// Zobrazit text v prvku vždy.
        /// Pokud nebude specifikována hodnota <see cref="ShowCaptionInMouseOver"/> ani <see cref="ShowCaptionInSelected"/> ani <see cref="ShowCaptionAllways"/>, nebude se zobrazovat text v prvku vůbec.
        /// </summary>
        ShowCaptionAllways = 0x8000,
        /// <summary>
        /// Nezobrazovat ToolTip nikdy.
        /// Toto je explicitní hodnota; ale shodné chování bude použito i když nebude specifikována žádná jiná hodnota ShowToolTip*.
        /// </summary>
        ShowToolTipNone = 0x10000,
        /// <summary>
        /// Zobrazit ToolTip až nějaký čas po najetí myší, a po přiměřeném čase (vzhledem k délce zobrazeného textu) zhasnout.
        /// Pokud nebude specifikována hodnota <see cref="ShowToolTipImmediatelly"/> ani <see cref="ShowToolTipFadeIn"/>, nebude se zobrazovat ToolTip vůbec.
        /// </summary>
        ShowToolTipFadeIn = 0x20000,
        /// <summary>
        /// Zobrazit ToolTip okamžitě po najetí myší na prvek (trochu brutus) a nechat svítit "skoro pořád".
        /// Pokud nebude specifikována hodnota <see cref="ShowToolTipImmediatelly"/> ani <see cref="ShowToolTipFadeIn"/>, nebude se zobrazovat ToolTip vůbec.
        /// </summary>
        ShowToolTipImmediatelly = 0x40000,
        /// <summary>
        /// Default pro pracovní čas = <see cref="ResizeTime"/> | <see cref="MoveToAnotherTime"/> | <see cref="MoveToAnotherRow"/>
        /// </summary>
        DefaultWorkTime = ResizeTime | MoveToAnotherTime | MoveToAnotherRow,
        /// <summary>
        /// Default pro text = <see cref="ShowCaptionInMouseOver"/> | <see cref="ShowCaptionInSelected"/> | <see cref="ShowToolTipFadeIn"/>
        /// </summary>
        DefaultText = ShowCaptionInMouseOver | ShowCaptionInSelected | ShowToolTipFadeIn,
        /// <summary>
        /// Souhrn příznaků, povolujících Drag and Drop prvku = <see cref="MoveToAnotherTime"/> | <see cref="MoveToAnotherRow"/> | <see cref="MoveToAnotherTable"/>
        /// </summary>
        AnyMove = MoveToAnotherTime | MoveToAnotherRow | MoveToAnotherTable
    }
    /// <summary>
    /// Režim přepočtu DateTime na osu X.
    /// </summary>
    public enum TimeGraphTimeAxisMode
    {
        /// <summary>
        /// Výchozí = podle vlastníka (sloupce, nebo tabulky).
        /// </summary>
        Default = 0,
        /// <summary>
        /// Standardní režim, kdy graf má osu X rovnou 1:1 k prvku TimeAxis.
        /// Využívá se v situaci, kdy prvky grafu jsou kresleny přímo pod TimeAxis.
        /// </summary>
        Standard,
        /// <summary>
        /// Proporcionální režim, kdy graf vykresluje ve své ploše stejný časový úsek jako TimeAxis,
        /// ale graf má jinou šířku v pixelech než časová osa (a tedy může mít i jiný počátek = souřadnici Bounds.X.
        /// Pak se pro přepočet hodnoty DateTime na hodnotu pixelu na ose X nepoužívá přímo TimeConverter, ale prostý přepočet vzdáleností.
        /// </summary>
        ProportionalScale,
        /// <summary>
        /// Logaritmický režim, kdy graf dovolí vykreslit všechny prvky grafu bez ohledu na to, že jejich pozice X (datum) je mimo rozsah TimeAxis.
        /// Vykreslování probíhá tak, že střední část grafu (typicky 60%) zobrazuje prvky proporcionálně (tj. lineárně) k časovému oknu,
        /// a okraje (vlevo a vpravo) zobrazují prvky ležící mimo časové okno, jejichž souřadnice X je určena logaritmicky.
        /// Na souřadnici X = 0 (úplně vlevo v grafu) se zobrazují prvky, jejichž Begin = mínus nekonečno,
        /// a na X = Right (úplně vpravo v grafu) se zobrazují prvky, jejichž End = plus nekonečno.
        /// </summary>
        LogarithmicScale
    }
    /// <summary>
    /// Pozice grafu v tabulce
    /// </summary>
    public enum DataGraphPositionType
    {
        /// <summary>
        /// V dané tabulce není graf (výchozí stav)
        /// </summary>
        None,
        /// <summary>
        /// Graf zobrazit v posledním sloupci (sloupec bude do tabulky přidán)
        /// </summary>
        InLastColumn,
        /// <summary>
        /// Graf zobrazit jako poklad, měřítko časové osy = proporcionální
        /// </summary>
        OnBackgroundProportional,
        /// <summary>
        /// Graf zobrazit jako poklad, měřítko časové osy = logaritmické
        /// </summary>
        OnBackgroundLogarithmic
    }

    /// <summary>
    /// Okraje formuláře
    /// </summary>
    public enum PluginFormBorderStyle
    {
        /// <summary>
        /// No border
        /// </summary>
        None = 0,
        /// <summary>
        /// A fixed, single-line border.
        /// </summary>
        FixedSingle = 1,
        /// <summary>
        /// A fixed, three-dimensional border.
        /// </summary>
        Fixed3D = 2,
        /// <summary>
        /// A thick, fixed dialog-style border.
        /// </summary>
        FixedDialog = 3,
        /// <summary>
        /// A resizable border.
        /// </summary>
        Sizable = 4,
        /// <summary>
        /// A tool window border that is not resizable. A tool window does not appear in
        /// the taskbar or in the window that appears when the user presses ALT+TAB. Although
        /// forms that specify System.Windows.Forms.FormBorderStyle.FixedToolWindow typically
        /// are not shown in the taskbar, you must also ensure that the System.Windows.Forms.Form.ShowInTaskbar
        /// property is set to false, since its default value is true.
        /// </summary>
        FixedToolWindow = 5,
        /// <summary>
        /// A resizable tool window border. A tool window does not appear in the taskbar
        /// or in the window that appears when the user presses ALT+TAB.
        /// </summary>
        SizableToolWindow = 6
    }
    /// <summary>
    /// Způsob umístění prvku grafu při jeho přetahování (Drag and Drop), na ose Y = v jaké výšce v grafu se prvek bude přesouvat
    /// </summary>
    public enum GraphItemMoveAlignY
    {
        /// <summary>
        /// Neurčeno, použije se <see cref="OnMousePosition"/>
        /// </summary>
        None = 0,
        /// <summary>
        /// Prvek se pohybuje na ose Y přesně podle pozice myši (volně plave).
        /// </summary>
        OnMousePosition,
        /// <summary>
        /// Prvek se pohybuje na té souřadnici Y, ve které byl původně umístěn (jezdí ve své kolejnici).
        /// Tato hodnota má význam pouze pro řízení pohybu po vlastním grafu (když se prvek nepřesouvá na jiný graf).
        /// Pokud bude tato hodnota zadaná pro řízení pohybu po cizím grafu, použije se <see cref="OnGraphTopPosition"/>.
        /// </summary>
        OnOriginalItemPosition,
        /// <summary>
        /// Prvek se pohybuje těsně pod horním okrajem grafu (visí nahoře a spustí se dolů).
        /// </summary>
        OnGraphTopPosition
    }
    #endregion
    #region class WorkSchedulerSupport : Třída obsahující konstanty a další podporu WorkScheduleru - identický kód je v Helios Green i v GraphLibrary !!!
    /// <summary>
    /// WorkSchedulerSupport : Třída obsahující konstanty a další podporu WorkScheduleru
    /// </summary>
    public class WorkSchedulerSupport
    {
        #region Konstanty
        /// <summary>
        /// "ASOL": Autor pluginu
        /// </summary>
        public const string MS_PLUGIN_AUTHOR = "ASOL";
        /// <summary>
        /// "GreenForMsPlugin": Název pluginu
        /// </summary>
        public const string MS_PLUGIN_NAME = "WorkScheduler";
        /// <summary>
        /// 0: číslo funkce "???"
        /// </summary>
        public const int MS_LICENSE_FUNC_NUMBER = 0;

        /// <summary>
        /// Command pro start pluginu
        /// </summary>
        public const string CMD_START_PLUGIN = "StartPlugin";
        /// <summary>
        /// Key v Request: "PluginId", obsahuje aktuálně přidělené ID číslo pluginu.
        /// Toto ID se odesílá do Helios Green do servisní funkce, aby tato funkce dokázala podle ID najít odpovídající serverovou sadu dat, odpovídající klientu.
        /// </summary>
        public const string KEY_REQUEST_PLUGIN_ID = "PluginId";
        /// <summary>
        /// Key v Request: "ServiceCwl", obsahuje číslo nebo identifikaci servisní funkce.
        /// Hodnota v tomto klíči tedy je stringová, a může být dvojí formát:
        /// 1) pouze číslo: "7894" = číslo funkce; anebo
        /// 2) číslo, dvojtečku a text: "1180:SchedulerService" = číslo třídy a název akce, která identifikuje funkci.
        /// Varianta 1 je snazší z hlediska pluginu, ale Cowley typu Run musí buď znát číslo funkce, anebo jej musí získat z repozitory pro třídu a název.
        /// Varianta 2 je optimálnější z hlediska Cowleyho typu Run, který tuto identifikaci sestavuje, protože opíše číslo třídy a název hlavní metody cowleyho.
        /// </summary>
        public const string KEY_REQUEST_SERVICE_CWL = "ServiceCwl";
        /// <summary>
        /// Key v Request: "Data"
        /// </summary>
        public const string KEY_REQUEST_DATA = "Data";
        /// <summary>
        /// Key v Request: "DataZip"
        /// </summary>
        public const string KEY_REQUEST_DATA_ZIP = "DataZip";

        /// <summary>
        /// Key v Response: Status
        /// </summary>
        public const string KEY_RESPONSE_RESULT_STATUS = "ResultStatus";
        /// <summary>
        /// Key v Response: Message
        /// </summary>
        public const string KEY_RESPONSE_RESULT_MESSAGE = "ResultMessage";


        // následující konstanty se zruší:
        /*
        /// <summary>
        /// Key v Request: "DataDeclaration"
        /// </summary>
        public const string KEY_REQUEST_DATA_DECLARATION = "DataDeclaration";
        /// <summary>
        /// Název tabulky "DataDeclaration"
        /// </summary>
        public const string DATA_DECLARATION_NAME = "DataDeclaration";
        /// <summary>
        /// Struktura tabulky "DataDeclaration"
        /// </summary>
        public const string DATA_DECLARATION_STRUCTURE = "data_id int; target string; content string; name string; title string; tooltip string; image string; data string";
        /// <summary>
        /// Key v Request: "Table.{{DataId}}.{{Name}}.Row.{{Part}}"
        /// </summary>
        public const string KEY_REQUEST_TABLE_ROW = "Table.{{DataId}}.{{Name}}.Row.{{Part}}";
        /// <summary>
        /// Key v Request: "Table.{{DataId}}.{{Name}}.Graph.{{Part}}"
        /// </summary>
        public const string KEY_REQUEST_TABLE_GRAPH = "Table.{{DataId}}.{{Name}}.Graph.{{Part}}";
        /// <summary>
        /// Key v Request: "Table.{{DataId}}.{{Name}}.Rel.{{Part}}"
        /// </summary>
        public const string KEY_REQUEST_TABLE_REL = "Table.{{DataId}}.{{Name}}.Rel.{{Part}}";
        /// <summary>
        /// Key v Request: "Table.{{DataId}}.{{Name}}.Item.{{Part}}"
        /// </summary>
        public const string KEY_REQUEST_TABLE_ITEM = "Table.{{DataId}}.{{Name}}.Item.{{Part}}";
        /// <summary>
        /// Pattern v KEY_REQUEST_TABLE_???, na jehož místo se vloží název tabulky
        /// </summary>
        public const string KEY_REQUEST_PATTERN_TABLENAME = "{{Name}}";
        /// <summary>
        /// Pattern v KEY_REQUEST_TABLE_???, na jehož místo se vloží číslo verze dat
        /// </summary>
        public const string KEY_REQUEST_PATTERN_DATAID = "{{DataId}}";
        /// <summary>
        /// Pattern v KEY_REQUEST_TABLE_???, na jehož místo se vloží pořadové číslo tabulky
        /// </summary>
        public const string KEY_REQUEST_PATTERN_PART = "{{Part}}";
        /// <summary>
        /// Struktura tabulky "Table.Graph"
        /// </summary>
        public const string DATA_TABLE_GRAPH_STRUCTURE = "row_rec_id int; row_class_id int; item_rec_id int; item_class_id int; group_rec_id int; group_class_id int; data_rec_id int; data_class_id int; layer int; level int; is_user_fixed int; time_begin datetime; time_end datetime; height decimal; ratio decimal; back_color string; join_back_color string; data string";

        /// <summary>
        /// Název GUI obsahu: nic
        /// </summary>
        public const string GUI_CONTENT_NONE = "";
        /// <summary>
        /// Název GUI obsahu: Panel
        /// </summary>
        public const string GUI_CONTENT_PANEL = "panel";
        /// <summary>
        /// Název GUI obsahu: Button
        /// </summary>
        public const string GUI_CONTENT_BUTTON = "button";
        /// <summary>
        /// Název GUI obsahu: Table
        /// </summary>
        public const string GUI_CONTENT_TABLE = "table";
        /// <summary>
        /// Název GUI obsahu: Function
        /// </summary>
        public const string GUI_CONTENT_FUNCTION = "function";

        /// <summary>
        /// Název GUI panelu: Main
        /// </summary>
        public const string GUI_TARGET_MAIN = "main";
        /// <summary>
        /// Název GUI panelu: Toolbar
        /// </summary>
        public const string GUI_TARGET_TOOLBAR = "toolbar";
        /// <summary>
        /// Název GUI panelu: Task
        /// </summary>
        public const string GUI_TARGET_TASK = "task";
        /// <summary>
        /// Název GUI panelu: Schedule
        /// </summary>
        public const string GUI_TARGET_SCHEDULE = "schedule";
        /// <summary>
        /// Název GUI panelu: Source
        /// </summary>
        public const string GUI_TARGET_SOURCE = "source";
        /// <summary>
        /// Název GUI panelu: Info
        /// </summary>
        public const string GUI_TARGET_INFO = "info";

        /// <summary>
        /// Název proměnné v deklaraci TABLE v prvku DATA: proměnná určující pozici časového grafu
        /// </summary>
        public const string DATA_TABLE_GRAPH_POSITION = "GraphPosition";
        /// <summary>
        /// Název hodnoty v deklaraci TABLE v prvku DATA: hodnota určující neexistující graf (pak není celá proměnná povinná)
        /// </summary>
        public const string DATA_TABLE_POSITION_NONE = "None";
        /// <summary>
        /// Název hodnoty v deklaraci TABLE v prvku DATA: hodnota určující graf umístěný v samostatném posledním sloupci tabulky
        /// </summary>
        public const string DATA_TABLE_POSITION_IN_LAST_COLUMN = "InLastColumn";
        /// <summary>
        /// Název hodnoty v deklaraci TABLE v prvku DATA: hodnota určující graf zobrazený jako neinteraktivní pozadí řádku, s časovou osou proporcionální, shodnou se základní osou
        /// </summary>
        public const string DATA_TABLE_POSITION_BACKGROUND_PROPORTIONAL = "OnBackgroundProportional";
        /// <summary>
        /// Název hodnoty v deklaraci TABLE v prvku DATA: hodnota určující graf zobrazený jako neinteraktivní pozadí řádku, s časovou osou logaritmickou, zobrazující prvky všech časů
        /// </summary>
        public const string DATA_TABLE_POSITION_BACKGROUND_LOGARITHMIC = "OnBackgroundLogarithmic";

        /// <summary>
        /// Název proměnné v deklaraci TABLE v prvku DATA: proměnná určující výšku jedné logické linky grafu v pixelech. Hodnota je Int32 v rozmezí  4 - 32 pixelů
        /// </summary>
        public const string DATA_TABLE_GRAPH_LINE_HEIGHT = "LineHeight";
        /// <summary>
        /// Název proměnné v deklaraci TABLE v prvku DATA: proměnná určující MINIMÁLNÍ výšku jednoho řádku s grafem, v pixelech. Hodnota je Int32 v rozmezí  15 - 320 pixelů
        /// </summary>
        public const string DATA_TABLE_GRAPH_MIN_HEIGHT = "MinHeight";
        /// <summary>
        /// Název proměnné v deklaraci TABLE v prvku DATA: proměnná určující MAXIMÁLNÍ výšku jednoho řádku s grafem, v pixelech. Hodnota je Int32 v rozmezí  15 - 320 pixelů
        /// </summary>
        public const string DATA_TABLE_GRAPH_MAX_HEIGHT = "MaxHeight";

        /// <summary>
        /// Název proměnné v deklaraci FUNCTION v prvku DATA: proměnná určující seznam tabulek (názvy oddělená čárkou), pro jejichž grafické prvky se má tato funkce nabízet. Typicky: workplace_table,source_table
        /// </summary>
        public const string DATA_FUNCTION_TABLE_NAMES = "TableNames";
        /// <summary>
        /// Název proměnné v deklaraci FUNCTION v prvku DATA: proměnná určující seznam tříd (čísla oddělená čárkou), pro jejichž grafické prvky se má tato funkce nabízet. Typicky: 1188,1190,1362
        /// Pokud seznam bude obsahovat i číslo 0 (taková třída neexistuje), pak se tato funkce bude nabízet jako kontextové menu v celém řádku (tj. i v prostoru grafu, kde není žádný prvek).
        /// Řádky, které obsahují graf "OnBackground" nikdy nenabízí kontextové funkce pro jednotlivé prvky dat, protože jde o "statické pozadí řádku", nikoli o pracovní prvek.
        /// </summary>
        public const string DATA_FUNCTION_CLASS_NUMBERS = "ClassNumbers";

        /// <summary>
        /// Název proměnné v deklaraci BUTTON v prvku DATA: proměnná určující výšku prvku v počtu jednotlivých modulů. Default = 2, povolené hodnoty: 1,2,3,4,6.
        /// </summary>
        public const string DATA_BUTTON_HEIGHT = "ButtonHeight";
        /// <summary>
        /// Název proměnné v deklaraci BUTTON v prvku DATA: proměnná určující šířku prvku v počtu jednotlivých modulů. Default = neurčeno, určí se podle velikosti textu a výšky HEIGHT.
        /// Může sloužit ke zpřesnění layoutu.
        /// </summary>
        public const string DATA_BUTTON_WIDTH = "ButtonWidth";
        /// <summary>
        /// Název proměnné v deklaraci BUTTON v prvku DATA: proměnná určující chování generátoru layoutu, obsahuje jednotlivé texty DATA_BUTTON_LAYOUT_*, oddělené čárkou.
        /// </summary>
        public const string DATA_BUTTON_LAYOUT = "Layout";
        /// <summary>
        /// Název proměnné v deklaraci BUTTON v prvku DATA: proměnná obsahuje název grupy, v níž se button objeví.
        /// Pokud nebude název zadán, pak se button objeví v implicitní grupě "FUNKCE".
        /// </summary>
        public const string DATA_BUTTON_GROUPNAME = "GroupName";

        // Tyto hodnoty musí exaktně odpovídat hodnotám enumu Asol.Tools.WorkScheduler.Components.LayoutHint, neboť jejich parsování se provádí na úrovni enumu (pouze s IgnoreCase = true):

        /// <summary>
        /// Hodnota proměnné LAYOUT v deklaraci BUTTON v prvku DATA: tento prvek musí být povinně v tom řádku, jako předešlý prvek.
        /// </summary>
        public const string DATA_BUTTON_LAYOUT_ThisItemOnSameRow = "ThisItemOnSameRow";
        /// <summary>
        /// Hodnota proměnné LAYOUT v deklaraci BUTTON v prvku DATA: tento prvek musí být povinně prvním na novém řádku.
        /// </summary>
        public const string DATA_BUTTON_LAYOUT_ThisItemSkipToNextRow = "ThisItemSkipToNextRow";
        /// <summary>
        /// Hodnota proměnné LAYOUT v deklaraci BUTTON v prvku DATA: tento prvek musí být povinně v novém bloku = první prvek v prvním řádku, jakoby za separátorem.
        /// </summary>
        public const string DATA_BUTTON_LAYOUT_ThisItemSkipToNextTable = "ThisItemSkipToNextTable";
        /// <summary>
        /// Hodnota proměnné LAYOUT v deklaraci BUTTON v prvku DATA: příští prvek musí být povinně v tom řádku, jako předešlý prvek. 
        /// </summary>
        public const string DATA_BUTTON_LAYOUT_NextItemOnSameRow = "NextItemOnSameRow";
        /// <summary>
        /// Hodnota proměnné LAYOUT v deklaraci BUTTON v prvku DATA: příští prvek musí být povinně prvním na novém řádku. 
        /// </summary>
        public const string DATA_BUTTON_LAYOUT_NextItemSkipToNextRow = "NextItemSkipToNextRow";
        /// <summary>
        /// Hodnota proměnné LAYOUT v deklaraci BUTTON v prvku DATA: příští prvek musí být povinně v novém bloku = první prvek v prvním řádku, jakoby za separátorem. 
        /// </summary>
        public const string DATA_BUTTON_LAYOUT_NextItemSkipToNextTable = "NextItemSkipToNextTable";

        /// <summary>
        /// Název proměnné EditMode v tabulce Graph v prvku DATA: proměnná, určující vlastnosti jednotlivého prvku grafu, obsahuje jednotlivé texty DATA_GRAPHITEM_EDITMODE_*, oddělené čárkou.
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE = "EditMode";
        /// <summary>
        /// Název proměnné BackStyle v tabulce Graph v prvku DATA: proměnná, určující styl vykreslení pozadí jednotlivého prvku grafu. Váže se na enum <see cref="System.Drawing.Drawing2D.HatchStyle"/>.
        /// </summary>
        public const string DATA_GRAPHITEM_BACKSTYLE = "BackStyle";
        /// <summary>
        /// Název proměnné BorderColor v tabulce Graph v prvku DATA: proměnná, určující barvu orámování jednotlivého prvku grafu.
        /// </summary>
        public const string DATA_GRAPHITEM_BORDERCOLOR = "BorderColor";
        /// <summary>
        /// Hodnota proměnné EditMode v tabulce Graph v prvku DATA: Lze změnit délku času (roztáhnout šířku pomocí přesunutí začátku nebo konce)
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE_ResizeTime = "ResizeTime";
        /// <summary>
        /// Hodnota proměnné EditMode v tabulce Graph v prvku DATA: Lze změnit výšku = obsazený prostor v grafu (roztáhnout výšku)
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE_ResizeHeight = "ResizeHeight";
        /// <summary>
        /// Hodnota proměnné EditMode v tabulce Graph v prvku DATA: Lze přesunout položku grafu na ose X = čas doleva / doprava
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE_MoveToAnotherTime = "MoveToAnotherTime";
        /// <summary>
        /// Hodnota proměnné EditMode v tabulce Graph v prvku DATA: Lze přesunout položku grafu na ose Y = na jiný řádek tabulky
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE_MoveToAnotherRow = "MoveToAnotherRow";
        /// <summary>
        /// Hodnota proměnné EditMode v tabulce Graph v prvku DATA: Lze přesunout položku grafu do jiné tabulky
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE_MoveToAnotherTable = "MoveToAnotherTable";
        /// <summary>
        /// Hodnota proměnné EditMode v tabulce Graph v prvku DATA: Nezobrazovat text v prvku nikdy
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE_ShowCaptionNone = "ShowCaptionNone";
        /// <summary>
        /// Hodnota proměnné EditMode v tabulce Graph v prvku DATA: Zobrazit text v prvku při stavu MouseOver
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE_ShowCaptionInMouseOver = "ShowCaptionInMouseOver";
        /// <summary>
        /// Hodnota proměnné EditMode v tabulce Graph v prvku DATA: Zobrazit text v prvku při stavu Selected
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE_ShowCaptionInSelected = "ShowCaptionInSelected";
        /// <summary>
        /// Hodnota proměnné EditMode v tabulce Graph v prvku DATA: Zobrazit text v prvku vždy
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE_ShowCaptionAllways = "ShowCaptionAllways";
        /// <summary>
        /// Hodnota proměnné EditMode v tabulce Graph v prvku DATA: Nezobrazovat ToolTip nikdy.
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE_ShowToolTipNone = "ShowToolTipNone";
        /// <summary>
        /// Hodnota proměnné EditMode v tabulce Graph v prvku DATA: Zobrazit ToolTip až nějaký čas po najetí myší, a po přiměřeném čase zhasnout.
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE_ShowToolTipFadeIn = "ShowToolTipFadeIn";
        /// <summary>
        /// Hodnota proměnné EditMode v tabulce Graph v prvku DATA: Zobrazit ToolTip okamžitě po najetí myší na prvek (trochu brutus) a nechat svítit "skoro pořád"
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE_ShowToolTipImmediatelly = "ShowToolTipImmediatelly";
        /// <summary>
        /// Hodnota proměnné EditMode v tabulce Graph v prvku DATA: Default pro pracovní čas = ResizeTime | MoveToAnotherTime | MoveToAnotherRow
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE_DefaultWorkTime = "DefaultWorkTime";
        /// <summary>
        /// Hodnota proměnné EditMode v tabulce Graph v prvku DATA: Default pro text = ShowCaptionInMouseOver | ShowCaptionInSelected | ShowToolTipFadeIn
        /// </summary>
        public const string DATA_GRAPHITEM_EDITMODE_DefaultText = "DefaultText";
        */
        #endregion
        #region Podpora tvorby tabulek a sloupců
        /// <summary>
        /// Vrátí DataTable daného jména a obsahující dané sloupce.
        /// Sloupce jsou zadány jedním stringem ve formě: "název typ, název typ, ...", kde typ je název datového typu dle níže uvedeného soupisu.
        /// string = char = text = varchar = nvarchar; sbyte; short = int16; int = int32; long = int64; byte; ushort = uint16; uint = uint32; ulong = uint64;
        /// single = float; double; decimal = numeric; bool = boolean; datetime = date; binary = image = picture.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static DataTable CreateTable(string tableName, string structure)
        {
            DataTable table = new DataTable();
            table.TableName = tableName;

            ColumnInfo[] columns = ParseTableFullStructure(structure);
            foreach (ColumnInfo column in columns)
                table.Columns.Add(column.Name, column.Type);

            return table;
        }
        /// <summary>
        /// Metoda vyhodí chybu <see cref="InvalidDataException"/> s odpovídající zprávou, pokud daná tabulka není v pořádku (je null, anebo ne obsahuje všechny dané sloupce (ověřuje jejich název a typ)).
        /// Struktura je načtena metodou <see cref="ParseTableFullStructure(string)"/> a má obecně formát: {column type[, {column type}, ...]}".
        /// Například: "cislo_subjektu int, reference_subjektu string, nazev_subjektu string, datum_od datetime".
        /// </summary>
        /// <param name="table"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static void CheckTable(DataTable table, string structure)
        {
            string badColumns;
            string message = _VerifyTable(table, structure, out badColumns);
            if (message == null) return;
            throw new InvalidDataException(message);
        }
        /// <summary>
        /// Metoda vrátí true, pokud daná tabulka je v pořádku = není null, a obsahuje všechny dané sloupce (ověřuje jejich název a typ).
        /// Struktura je načtena metodou <see cref="ParseTableFullStructure(string)"/> a má obecně formát: {column type[, {column type}, ...]}".
        /// Například: "cislo_subjektu int, reference_subjektu string, nazev_subjektu string, datum_od datetime".
        /// </summary>
        /// <param name="table"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static bool VerifyTable(DataTable table, string structure)
        {
            string badColumns;
            string message = _VerifyTable(table, structure, out badColumns);
            return (message == null);
        }
        /// <summary>
        /// Metoda vrátí true, pokud daná tabulka je v pořádku = není null, a obsahuje všechny dané sloupce (ověřuje jejich název a typ).
        /// Pokud vrátí false, pak v out parametru badColumns bude uložen seznam chybějících nebo špatných sloupců.
        /// Struktura je načtena metodou <see cref="ParseTableFullStructure(string)"/> a má obecně formát: {column type[, {column type}, ...]}".
        /// Například: "cislo_subjektu int, reference_subjektu string, nazev_subjektu string, datum_od datetime".
        /// </summary>
        /// <param name="table"></param>
        /// <param name="structure"></param>
        /// <param name="badColumns"></param>
        /// <returns></returns>
        public static bool VerifyTable(DataTable table, string structure, out string badColumns)
        {
            string message = _VerifyTable(table, structure, out badColumns);
            return (message == null);
        }
        /// <summary>
        /// Metoda vrátí true, pokud daná tabulka je v pořádku = není null, a obsahuje všechny dané sloupce (ověřuje jejich název a typ).
        /// Struktura je načtena metodou <see cref="ParseTableFullStructure(string)"/> a má obecně formát: {column type[, {column type}, ...]}".
        /// Například: "cislo_subjektu int, reference_subjektu string, nazev_subjektu string, datum_od datetime".
        /// </summary>
        /// <param name="table"></param>
        /// <param name="structure"></param>
        /// <param name="badColumns"></param>
        /// <returns></returns>
        private static string _VerifyTable(DataTable table, string structure, out string badColumns)
        {
            badColumns = "";

            if (table == null) return "DataTable is null";

            string message = "";
            ColumnInfo[] columns = ParseTableFullStructure(structure);
            foreach (ColumnInfo column in columns)
            {
                string columnName = column.Name;
                if (!table.Columns.Contains(columnName))
                {   // Chybějící sloupec:
                    badColumns += ", " + columnName + " " + column.TypeName;
                    message += ", missing column: " + columnName + " " + column.TypeName;
                }
                else
                {
                    Type expectedType = column.Type;
                    Type columnType = table.Columns[columnName].DataType;
                    if (!_IsExpectedType(columnType, expectedType))
                    {   // Nesprávný typ sloupce:
                        badColumns += ", " + columnName + " " + column.TypeName;
                        message += ", wrong type in column: " + columnName + " " + column.TypeName + " (current Type: " + columnType.Name + ")";
                    }
                }
            }

            if (badColumns.Length > 0) badColumns = badColumns.Substring(2);

            if (message.Length == 0) return null;              // OK
            return "Incorrect structure of table <" + table.TableName + ">: " + message.Substring(2);
        }
        /// <summary>
        /// Vrátí true, pokud datový typ sloupce (sourceType) je vyhovující pro očekávaný cílový typ sloupce (targetType).
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        private static bool _IsExpectedType(Type sourceType, Type targetType)
        {
            if (sourceType == targetType) return true;

            string sourceName = sourceType.Namespace + "." + sourceType.Name;
            string targetName = targetType.Namespace + "." + targetType.Name;
            if (sourceName == targetName) return true;
            string convert = targetName + " = " + sourceName;

            switch (convert)
            {   // Co je převoditelné:
                //   Cílový typ    = Zdrojový typ
                case "System.Int16 = System.Byte":
                case "System.Int16 = System.SByte":
                case "System.Int16 = System.Int32":
                case "System.Int16 = System.Int64":
                case "System.Int16 = System.UInt32":
                case "System.Int16 = System.UInt64":

                case "System.Int32 = System.Byte":
                case "System.Int32 = System.SByte":
                case "System.Int32 = System.Int16":
                case "System.Int32 = System.Int64":
                case "System.Int32 = System.UInt16":
                case "System.Int32 = System.UInt64":

                case "System.Int64 = System.Byte":
                case "System.Int64 = System.SByte":
                case "System.Int64 = System.Int16":
                case "System.Int64 = System.Int32":
                case "System.Int64 = System.UInt16":
                case "System.Int64 = System.UInt32":

                case "System.Single = System.Byte":
                case "System.Single = System.SByte":
                case "System.Single = System.Int16":
                case "System.Single = System.Int32":
                case "System.Single = System.Int64":
                case "System.Single = System.UInt16":
                case "System.Single = System.UInt32":
                case "System.Single = System.UInt64":
                case "System.Single = System.Double":
                case "System.Single = System.Decimal":

                case "System.Double = System.Byte":
                case "System.Double = System.SByte":
                case "System.Double = System.Int16":
                case "System.Double = System.Int32":
                case "System.Double = System.Int64":
                case "System.Double = System.UInt16":
                case "System.Double = System.UInt32":
                case "System.Double = System.UInt64":
                case "System.Double = System.Single":
                case "System.Double = System.Decimal":

                case "System.Decimal = System.Byte":
                case "System.Decimal = System.SByte":
                case "System.Decimal = System.Int16":
                case "System.Decimal = System.Int32":
                case "System.Decimal = System.Int64":
                case "System.Decimal = System.UInt16":
                case "System.Decimal = System.UInt32":
                case "System.Decimal = System.UInt64":
                case "System.Decimal = System.Single":
                case "System.Decimal = System.Double":

                    return true;
            }
            return false;
        }
        /// <summary>
        /// Metoda z textové podoby struktury vrací typově definované pole, které obsahuje zadanou strukturu.
        /// Sloupce jsou zadány jedním stringem ve formě: "název typ, název typ, ...", kde typ je název datového typu dle níže uvedeného soupisu.
        /// string = char = text = varchar = nvarchar; sbyte; short = int16; int = int32; long = int64; byte; ushort = uint16; uint = uint32; ulong = uint64;
        /// single = float; double; decimal = numeric; bool = boolean; datetime = date; binary = image = picture.
        /// <para/>
        /// Výstupní prvky mají tento obsah: Item1 = název sloupce, Item2 = Type odpovídající zadanému datovému typu
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        [Obsolete("Use ParseTableFullStructure() instead", true)]
        public static Tuple<string, Type>[] ParseTableStructure(string structure)
        {   // Only for ppublic interface
            ColumnInfo[] columnList = ParseTableFullStructure(structure);
            return columnList.Select(i => new Tuple<string, Type>(i.Name, i.Type)).ToArray();
        }
        /// <summary>
        /// Metoda z textové podoby struktury vrací typově definované pole, které obsahuje zadanou strukturu.
        /// Sloupce jsou zadány jedním stringem ve formě: "název typ, název typ, ...", kde typ je název datového typu dle níže uvedeného soupisu.
        /// string = char = text = varchar = nvarchar; sbyte; short = int16; int = int32; long = int64; byte; ushort = uint16; uint = uint32; ulong = uint64;
        /// single = float; double; decimal = numeric; bool = boolean; datetime = date; binary = image = picture.
        /// <para/>
        /// Výstupní prvky mají tento obsah: Item1 = název sloupce, Item2 = zadaný datový typ (string), Item3 = Type odpovídající zadanému typu
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static ColumnInfo[] ParseTableFullStructure(string structure)
        {
            List<ColumnInfo> columnList = new List<ColumnInfo>();
            if (!String.IsNullOrEmpty(structure))
            {
                char[] columnDelimiters = ",;".ToCharArray();
                char[] partDelimiters = " :".ToCharArray();
                string[] columns = structure.Split(columnDelimiters, StringSplitOptions.RemoveEmptyEntries);
                int count = columns.Length;
                for (int i = 0; i < count; i++)
                {
                    string[] column = columns[i].Trim().Split(partDelimiters, StringSplitOptions.RemoveEmptyEntries);
                    if (column.Length < 2) continue;
                    string name = column[0];
                    if (String.IsNullOrEmpty(name)) continue;
                    string typeName = column[1];
                    Type type = GetTypeFromName(typeName);
                    if (type == null) continue;
                    columnList.Add(new ColumnInfo(name, typeName, type));
                }
            }
            return columnList.ToArray();
        }
        /// <summary>
        /// Vrátí Type pro daný název typu.
        /// Detekuje tyto typy:
        /// string = char = text = varchar = nvarchar; sbyte; short = int16; int = int32; long = int64; byte; ushort = uint16; uint = uint32; ulong = uint64;
        /// single = float; double; decimal = numeric; bool = boolean; datetime = date; binary = image = picture.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static Type GetTypeFromName(string typeName)
        {
            if (String.IsNullOrEmpty(typeName)) return null;
            typeName = typeName.Trim().ToLower();
            switch (typeName)
            {
                case "string":
                case "char":
                case "text":
                case "varchar":
                case "nvarchar":
                    return typeof(String);

                case "sbyte":
                    return typeof(SByte);

                case "short":
                case "int16":
                    return typeof(Int16);

                case "int":
                case "int32":
                    return typeof(Int32);

                case "long":
                case "int64":
                    return typeof(Int64);

                case "byte":
                    return typeof(Byte);

                case "ushort":
                case "uint16":
                    return typeof(UInt16);

                case "uint":
                case "uint32":
                    return typeof(UInt32);

                case "ulong":
                case "uint64":
                    return typeof(UInt64);

                case "single":
                case "float":
                    return typeof(Single);

                case "double":
                    return typeof(Double);

                case "decimal":
                case "numeric":
                    return typeof(Decimal);

                case "bool":
                case "boolean":
                    return typeof(Boolean);

                case "datetime":
                case "date":
                    return typeof(DateTime);

                case "binary":
                case "image":
                case "picture":
                    return typeof(Byte[]);

            }

            return null;
        }
        /// <summary>
        /// Údaje o sloupci
        /// </summary>
        public class ColumnInfo
        {
            internal ColumnInfo(string name, string typeName, Type type)
            {
                this.Name = name;
                this.TypeName = typeName;
                this.Type = type;
            }
            /// <summary>
            /// Název sloupce
            /// </summary>
            public string Name { get; private set; }
            /// <summary>
            /// Zadaný název datového typu
            /// </summary>
            public string TypeName { get; private set; }
            /// <summary>
            /// Odpovídající Type
            /// </summary>
            public Type Type { get; private set; }
        }
        #endregion
        #region Serializace a Deserializace DataTable
        /// <summary>
        /// Serializuje tabulku. Z objektu DataTable vrátí text.
        /// Text lze převést na tabulku metodou TableDeserialize().
        /// Používají se o párové metody na instanci třídy DataTable : ReadXml() a WriteXml().
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static string TableSerialize(DataTable table)
        {
            if (table == null) return null;
            try
            {
                StringBuilder sb = new StringBuilder();
                using (System.IO.StringWriter writer = new System.IO.StringWriter(sb))
                {
                    table.WriteXml(writer, XmlWriteMode.WriteSchema);
                }
                return sb.ToString();
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException("Zadanou tabulku (table) není možno serializovat do stringu. Při serializaci je hlášena chyba " + exc.Message + ".");
            }
        }
        /// <summary>
        /// Deserializuje tabulku. Z textu vrátí objekt DataTable.
        /// Vstupní text má být vytvořen metodou <see cref="TableSerialize(DataTable)"/>.
        /// Používají se o párové metody na instanci třídy DataTable : ReadXml() a WriteXml().
        /// Tato metoda při chybě hodí chybu, jinak vrátí Table. Nikdy nevrací null.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DataTable TableDeserialize(string data)
        {
            return _TableDeserialize(data, false);
        }
        /// <summary>
        /// Deserializuje tabulku. Z textu vytvoří objekt DataTable a vrátí true.
        /// Text má být vytvořen metodou TableSerialize().
        /// Používají se o párové metody na instanci třídy DataTable : ReadXml() a WriteXml().
        /// Tato metoda při chybě vrátí false a do out parametru table nechá null. Nikdy nehodí chybu.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public static bool TryTableDeserialize(string data, out DataTable table)
        {
            table = _TableDeserialize(data, true);
            return (table != null);
        }
        /// <summary>
        /// Deserializuje tabulku. Z textu vrátí objekt DataTable.
        /// Text má být vytvořen metodou TableSerialize().
        /// Používají se o párové metody na instanci třídy DataTable : ReadXml() a WriteXml().
        /// Při chybě se chová podle parametru ignoreErrors.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ignoreErrors"></param>
        /// <returns></returns>
        private static DataTable _TableDeserialize(string data, bool ignoreErrors)
        {
            DataTable table = null;
            string message = null;
            if (String.IsNullOrEmpty(data))
                message = "TableDeserialize: Zadaný řetězec (data) není možno převést do formátu DataTable, řetězec je prázdný.";
            else
            {
                try
                {
                    table = new DataTable();
                    using (System.IO.StringReader reader = new System.IO.StringReader(data))
                    {
                        table.ReadXml(reader);
                    }
                }
                catch (Exception exc)
                {
                    table = null;
                    message = "TableDeserialize: Zadaný řetězec (data) není možno převést do formátu DataTable. Při deserializaci je hlášena chyba " + exc.Message + ".";
                }
            }
            if (table == null && !ignoreErrors)
                throw new ArgumentException(message);
            return table;
        }
        #endregion
        #region Serializace a Deserializace Image
        /// <summary>
        /// Serializuje Image. Z objektu Image vrátí text (obsahuje obrázek ve formátu PNG, obsah byte[] převedený na text formátu Base64).
        /// Text lze převést na tabulku metodou ImageDeserialize().
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static string ImageSerialize(Image image)
        {
            if (image == null) return null;
            try
            {
                string target = null;
                using (System.IO.MemoryStream memoryStream = new MemoryStream())
                {
                    System.Drawing.Imaging.ImageFormat imageFormat = System.Drawing.Imaging.ImageFormat.Png;
                    image.Save(memoryStream, imageFormat);
                    byte[] outBuffer = memoryStream.ToArray();
                    target = System.Convert.ToBase64String(outBuffer, Base64FormattingOptions.InsertLineBreaks);
                }
                return target;
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException("Zadaný obrázek (Image) není možno serializovat do stringu. Při serializaci je hlášena chyba " + exc.Message + ".");
            }
        }
        /// <summary>
        /// Deserializuje obrázek. Z textu vrátí objekt Image.
        /// Vstupní obrázek má být vytvořen metodou <see cref="ImageSerialize(Image)"/>.
        /// Tato metoda při chybě hodí chybu, jinak vrátí Table. Nikdy nevrací null.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Image ImageDeserialize(string data)
        {
            return _ImageDeserialize(data, false);
        }
        /// <summary>
        /// Deserializuje obrázek. Z textu vytvoří objekt Image a vrátí true.
        /// Vstupní obrázek má být vytvořen metodou <see cref="ImageSerialize(Image)"/>.
        /// Tato metoda při chybě vrátí false a do out parametru image nechá null. Nikdy nehodí chybu.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public static bool TryImageDeserialize(string data, out Image image)
        {
            image = _ImageDeserialize(data, true);
            return (image != null);
        }
        /// <summary>
        /// Deserializuje obrázek. Z textu vrátí objekt Image.
        /// Vstupní obrázek má být vytvořen metodou <see cref="ImageSerialize(Image)"/>.
        /// Při chybě se chová podle parametru ignoreErrors.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ignoreErrors"></param>
        /// <returns></returns>
        private static Image _ImageDeserialize(string data, bool ignoreErrors)
        {
            Image image = null;
            string message = null;
            if (String.IsNullOrEmpty(data))
                message = "ImageDeserialize: Zadaný řetězec (data) není možno převést do formátu Image, řetězec je prázdný.";
            else
            {
                try
                {
                    byte[] inpBuffer = System.Convert.FromBase64String(data);
                    using (System.IO.MemoryStream inpStream = new MemoryStream(inpBuffer))
                    {
                        image = Image.FromStream(inpStream);
                    }
                }
                catch (Exception exc)
                {
                    image = null;
                    message = "ImageDeserialize: Zadaný řetězec (data) není možno převést do formátu Image. Při deserializaci je hlášena chyba " + exc.Message + ".";
                }
            }
            if (image == null && !ignoreErrors)
                throw new ArgumentException(message);
            return image;
        }
        #endregion
        #region Serializace a Deserializace Color
        /// <summary>
        /// Serializuje Color.
        /// Text lze převést na barvu metodou ColorDeserialize().
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string ColorSerialize(Color color)
        {
            if (color.IsKnownColor) return color.Name;
            return "0x" + color.A.ToString("X2") + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }
        /// <summary>
        /// Deserializuje a vrátí Color.
        /// Vstupní barva má být vytvořena metodou <see cref="ColorSerialize(Color)"/>.
        /// Tato metoda může hodit chybu.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Color ColorDeserialize(string data)
        {
            return _ColorDeserialize(data, false).Value;
        }
        /// <summary>
        /// Deserializuje Color.
        /// Vstupní barva má být vytvořena metodou <see cref="ColorSerialize(Color)"/>.
        /// Tato metoda při chybě vrátí false a do out parametru color uloží null. Nikdy nehodí chybu.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static bool TryColorDeserialize(string data, out Color? color)
        {
            color = _ColorDeserialize(data, true);
            return (color.HasValue);
        }
        /// <summary>
        /// Deserializuje obrázek. Z textu vrátí objekt Image.
        /// Vstupní obrázek má být vytvořen metodou <see cref="ImageSerialize(Image)"/>.
        /// Při chybě se chová podle parametru ignoreErrors.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ignoreErrors"></param>
        /// <returns></returns>
        private static Color? _ColorDeserialize(string data, bool ignoreErrors)
        {
            Color? color = null;
            string message = null;
            if (String.IsNullOrEmpty(data))
                message = "ImageDeserialize: Zadaný řetězec (data) není možno převést do formátu Image, řetězec je prázdný.";
            else
            {
                if (data.StartsWith("0x") || data.StartsWith("0&"))
                    color = _GetColorHex(data);
                else
                    color = _GetColorName(data);
            }
            if (!color.HasValue && !ignoreErrors)
                throw new ArgumentException(message);
            return color;
        }
        /// <summary>
        /// Vrátí barvu pro zadaný název barvy. Název může být string z enumu <see cref="KnownColor"/>, například "Violet";, ignoruje se velikost písmen.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Color? _GetColorName(string name)
        {
            KnownColor color;
            if (Enum.TryParse(name, true, out color)) return Color.FromKnownColor(color);
            return null;
        }
        /// <summary>
        /// Vrátí barvu pro zadaný hexadecimální řetězec.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Color? _GetColorHex(string name)
        {
            System.Globalization.NumberFormatInfo nfi = new System.Globalization.NumberFormatInfo();
            string hexValue = name.Substring(2).ToUpper();
            int value;
            if (!Int32.TryParse(hexValue, System.Globalization.NumberStyles.AllowHexSpecifier, nfi, out value)) return null;
            Color color = Color.FromArgb(value);
            if (color.A == 0)                              // Pokud v barvě NENÍ zadáno nic do složky Alpha, jde nejspíš o opomenutí!
                color = Color.FromArgb(255, color);        //   (implicitně se hodnota Alpha nezadává, a přitom se předpokládá že tan bude 255)  =>  Alpha = 255 = plná barva
            return color;
        }
        #endregion
        #region Komprimace a dekomprimace stringu
        /// <summary>
        /// Metoda vrátí daný string KOMPRIMOVANÝ pomocí <see cref="System.IO.Compression.GZipStream"/>, převedený do Base64 stringu.
        /// Standardní serializovanou DataTable tato komprimace zmenší na cca 3-5% původní délky stringu.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string Compress(string source)
        {
            if (source == null || source.Length == 0) return source;

            string target = null;
            byte[] inpBuffer = System.Text.Encoding.UTF8.GetBytes(source);
            using (System.IO.MemoryStream inpStream = new MemoryStream(inpBuffer))
            using (System.IO.MemoryStream outStream = new MemoryStream())
            {
                using (System.IO.Compression.GZipStream zipStream = new System.IO.Compression.GZipStream(outStream, System.IO.Compression.CompressionMode.Compress))
                {
                    inpStream.CopyTo(zipStream);
                }   // Obsah streamu outStream je použitelný až po Dispose streamu GZipStream !
                outStream.Flush();
                byte[] outBuffer = outStream.ToArray();
                target = System.Convert.ToBase64String(outBuffer, Base64FormattingOptions.InsertLineBreaks);
            }
            return target;
        }
        /// <summary>
        /// Metoda vrátí daný string DEKOMPRIMOVANÝ pomocí <see cref="System.IO.Compression.GZipStream"/>, převedený z Base64 stringu.
        /// Standardní serializovanou DataTable tato komprimace zmenší na cca 3-8% původní délky stringu.
        /// Pokud při dekomprimaci dojde k chybě+, vrátí null.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string TryDecompress(string source)
        {
            if (source == null || source.Length == 0) return source;
            string target = null;
            try
            {
                target = Decompress(source);
            }
            catch (Exception)
            {
                target = null;
            }
            return target;
        }
        /// <summary>
        /// Metoda vrátí daný string DEKOMPRIMOVANÝ pomocí <see cref="System.IO.Compression.GZipStream"/>, převedený z Base64 stringu.
        /// Standardní serializovanou DataTable tato komprimace zmenší na cca 3-8% původní délky stringu.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string Decompress(string source)
        {
            if (source == null || source.Length == 0) return source;

            string target = null;
            byte[] inpBuffer = System.Convert.FromBase64String(source);
            using (System.IO.MemoryStream inpStream = new MemoryStream(inpBuffer))
            using (System.IO.MemoryStream outStream = new MemoryStream())
            {
                using (System.IO.Compression.GZipStream zipStream = new System.IO.Compression.GZipStream(inpStream, System.IO.Compression.CompressionMode.Decompress))
                {
                    zipStream.CopyTo(outStream);
                }   // Obsah streamu outStream je použitelný až po Dispose streamu GZipStream !
                outStream.Flush();
                byte[] outBuffer = outStream.ToArray();
                target = System.Text.Encoding.UTF8.GetString(outBuffer);
            }
            return target;
        }
        #endregion
        #region Odesílání a příjem datového balíku
        /// <summary>
        /// Vytvoří a vrátí <see cref="DataBuffer"/> pro zápis dat.
        /// Data se do něj vkládají metodami <see cref="DataBuffer.WriteText(string, string)"/>atd, zapsaný text se získá v property <see cref="DataBuffer.WrittenContent"/>.
        /// </summary>
        /// <returns></returns>
        public static DataBuffer CreateDataBufferWriter()
        {
            return new DataBuffer();
        }
        /// <summary>
        /// Vytvoří a vrátí <see cref="DataBuffer"/> pro čtení dat.
        /// Vstupní text se předává do této metody.
        /// Data se čtou metodami 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static DataBuffer CreateDataBufferReader(string content)
        {
            return new DataBuffer(content);
        }
        /// <summary>
        /// Buffer pro zápis a čtení dat
        /// </summary>
        public class DataBuffer : IDisposable
        {
            #region Konstrukce, buffer, Dispose
            internal DataBuffer()
            {
                this._StringBuilder = new StringBuilder();
                this._Writer = new StringWriter(this._StringBuilder);
            }
            internal DataBuffer(string data)
            {
                this._Reader = new StringReader(data);
            }
            private System.Text.StringBuilder _StringBuilder;
            private System.IO.StringWriter _Writer;
            private System.IO.StringReader _Reader;
            void IDisposable.Dispose()
            {
                try
                {
                    if (this._Writer != null)
                    {
                        this._Writer.Close();
                        this._Writer.Dispose();
                        this._Writer = null;
                    }
                    if (this._StringBuilder != null)
                    {
                        this._StringBuilder = null;
                    }
                    if (this._Reader != null)
                    {
                        this._Reader.Close();
                        this._Reader.Dispose();
                        this._Reader = null;
                    }
                }
                catch { }
            }
            #endregion
            #region Write: Zápis dat
            /// <summary>
            /// true pokud this Buffer je v režimu Write = umožní zapisovat, ale ne číst
            /// </summary>
            public bool IsWritter { get { return (this._Writer != null); } }
            /// <summary>
            /// Do bufferu zapíše data, která získá komprimací daného textu.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="text"></param>
            public void WriteText(string key, string text)
            {
                this.WriteText(key, text, false);
            }
            /// <summary>
            /// Do bufferu zapíše data, která získá komprimací daného textu.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="text"></param>
            /// <param name="verify"></param>
            public void WriteText(string key, string text, bool verify)
            {
                this._CheckWritter("WriteText method");
                string data = WorkSchedulerSupport.Compress(text);
                if (verify)
                {
                    string test = WorkSchedulerSupport.Decompress(data);
                    if (test.Length != text.Length || test != text)
                        throw new InvalidOperationException("WorkSchedulerSupport.Compress() and Decompress() error.");
                }
                this.WriteData(key, data);
            }
            /// <summary>
            /// Do bufferu zapíše komprimovaná data
            /// </summary>
            /// <param name="key"></param>
            /// <param name="data"></param>
            public void WriteData(string key, string data)
            {
                this._CheckWritter("WriteData method");
                key = key
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Replace("<", "{")
                    .Replace(">", "}");
                this._Writer.WriteLine(KEY_BEGIN);
                this._Writer.WriteLine(key);
                this._Writer.WriteLine(KEY_END);
                this._Writer.WriteLine(DATA_BEGIN);
                this._Writer.WriteLine(data);
                this._Writer.WriteLine(DATA_END);
                this._Writer.WriteLine();
            }
            /// <summary>
            /// Obsahuje aktuálně zapsaná data, ale pouze v režimu <see cref="IsWritter"/>.
            /// </summary>
            public string WrittenContent
            {
                get
                {
                    this._CheckWritter("WrittenContent property");
                    this._Writer.Flush();
                    return this._StringBuilder.ToString();
                }
            }
            /// <summary>
            /// Ověří, že this Buffer je v režimu <see cref="IsWritter"/>.
            /// Pokud není, vyhodí chybu.
            /// </summary>
            /// <param name="usedMember"></param>
            private void _CheckWritter(string usedMember)
            {
                if (!this.IsWritter)
                    throw new InvalidOperationException("Instance of DataBuffer is not in Writer mode. Using the " + usedMember + " is not possible.");
            }
            #endregion
            #region Read: čtení dat
            /// <summary>
            /// true pokud this Buffer je v režimu Read = umožní číst, ale ne zapisovat
            /// </summary>
            public bool IsReader { get { return (this._Reader != null); } }
            /// <summary>
            /// Metoda najde v textu nejbližší klíč a jeho obsah, načte je, data dekomrpimuje, a vepíše do out parametrů, pak vrací true.
            /// Pokud nic nemá (došla data), vrací false.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="text"></param>
            /// <returns></returns>
            public bool ReadNextText(out string key, out string text)
            {
                this._CheckReader("ReadNextText method");
                text = null;
                string data;
                if (!this._ReadNextBlock(out key, out data)) return false;

                text = WorkSchedulerSupport.Decompress(data);
                return true;
            }
            /// <summary>
            /// Metoda najde v textu nejbližší klíč a jeho obsah, načte je, načtený obsah nezmění (neprovede dekomrpimaci), a vepíše do out parametrů, pak vrací true.
            /// Pokud nic nemá (došla data), vrací false.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public bool ReadNextData(out string key, out string data)
            {
                this._CheckReader("ReadNextText method");
                data = null;
                if (!this._ReadNextBlock(out key, out data)) return false;
                return true;
            }
            /// <summary>
            /// Metoda z bloku dat načte key a value, kde value je prostý načtený blok dat (bez dekomprimace).
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            private bool _ReadNextBlock(out string key, out string value)
            {
                key = null;
                value = null;
                if (this._ReaderIsEnd) return false;

                StringBuilder content = new StringBuilder();

                _ReadState currState = _ReadState.Before;            // state obsahuje stav, který byl před chvilkou, před řádkem který načteme do line.
                while (!(currState == _ReadState.DataEnd || currState == _ReadState.Incorrect))         // Pokud najdu stav Konec dat nebo Chyba, tak nebudu pokračovat.
                {
                    string line = this._Reader.ReadLine();
                    if (line == null)
                    {
                        this._ReaderIsEnd = true;                    // Úplný konec dat (fyzický)
                        break;
                    }
                    _ReadState lineState = _ReadDetectLine(line);    // Čemu odpovídá načtený řádek
                    if (lineState == _ReadState.Empty) continue;     // Prázdné přeskočím

                    switch (currState)                               // Stavový automat vychází ze stavu před řádkem line
                    {
                        case _ReadState.Before:
                            if (lineState == _ReadState.KeyBegin) currState = _ReadState.KeyBegin;
                            break;
                        case _ReadState.KeyBegin:
                            if (lineState == _ReadState.Other)
                            {
                                key = line;
                                currState = _ReadState.Key;
                            }
                            break;
                        case _ReadState.Key:
                            if (lineState == _ReadState.KeyEnd) currState = _ReadState.KeyEnd;
                            break;
                        case _ReadState.KeyEnd:
                            if (lineState == _ReadState.DataBegin) currState = _ReadState.DataBegin;
                            break;
                        case _ReadState.DataBegin:
                        case _ReadState.Data:
                            if (lineState == _ReadState.Other)
                            {
                                content.AppendLine(line);
                                currState = _ReadState.Data;
                            }
                            else if (lineState == _ReadState.DataEnd) currState = _ReadState.DataEnd;
                            else currState = _ReadState.Incorrect;             // Pokud uprostřed dat najdu nějaký jiný klíčový text, který nečekám, pak skončím s chybou
                            break;
                        case _ReadState.DataEnd:
                            break;
                    }
                }

                if (currState == _ReadState.DataEnd)
                {
                    value = content.ToString();
                    return true;
                }
                return false;
            }
            private static _ReadState _ReadDetectLine(string line)
            {
                if (String.IsNullOrEmpty(line)) return _ReadState.Empty;
                if (String.Equals(line, KEY_BEGIN, StringComparison.InvariantCultureIgnoreCase)) return _ReadState.KeyBegin;
                if (String.Equals(line, KEY_END, StringComparison.InvariantCultureIgnoreCase)) return _ReadState.KeyEnd;
                if (String.Equals(line, DATA_BEGIN, StringComparison.InvariantCultureIgnoreCase)) return _ReadState.DataBegin;
                if (String.Equals(line, DATA_END, StringComparison.InvariantCultureIgnoreCase)) return _ReadState.DataEnd;
                return _ReadState.Other;
            }
            private enum _ReadState { Before, Empty, KeyBegin, Key, KeyEnd, DataBegin, Data, DataEnd, Other, Incorrect }
            /// <summary>
            /// true, pokud Reader došel na konec vstupních dat, a už nic dalšího nepřečte.
            /// </summary>
            public bool ReaderIsEnd
            {
                get
                {
                    this._CheckReader("ReaderIsEnd property");
                    return this._ReaderIsEnd;
                }
            }
            private bool _ReaderIsEnd;
            /// <summary>
            /// Ověří, že this Buffer je v režimu <see cref="IsWritter"/>.
            /// Pokud není, vyhodí chybu.
            /// </summary>
            /// <param name="usedMember"></param>
            private void _CheckReader(string usedMember)
            {
                if (!this.IsReader)
                    throw new InvalidOperationException("Instance of DataBuffer is not in Reader mode. Using the " + usedMember + " is not possible.");
            }
            #endregion
            #region Konstanty
            /// <summary>
            /// Klíč, začátek
            /// </summary>
            protected const string KEY_BEGIN = "<Key>";
            /// <summary>
            /// Klíč, konec
            /// </summary>
            protected const string KEY_END = "</Key>";
            /// <summary>
            /// Data, začátek
            /// </summary>
            protected const string DATA_BEGIN = "<Data>";
            /// <summary>
            /// Data, konec
            /// </summary>
            protected const string DATA_END = "</Data>";
            #endregion
        }
        #endregion
    }
    #endregion
    #region class DataColumnExtendedInfo : Třída obsahující rozšířené informace o jednom sloupci tabulky
    /// <summary>
    /// DataColumnExtendedInfo : Třída obsahující rozšířené informace o jednom sloupci tabulky <see cref="DataColumn"/>),
    /// které do objektu <see cref="DataColumn"/>.ExtendedProperties přidává Helios Green po načtení dat z přehledové šablony.
    /// </summary>
    public class DataColumnExtendedInfo
    {
        #region Konstrukce a načtení dat
        /// <summary>
        /// Vrací pole informací o všech sloupcích tabulky, pro které načte z Extended properties daného sloupce 
        /// (tam je uložil Helios Green v metodě BrowseTemplateInfo.GetTemplateData(int, int?, int?, BigFilter, int?)).
        /// Referenci na sloupce tabulky, předaný sem jako parametr, si this instance ukládá, a dovoluje tyto informace měnit a vkládat změny zase do sloupce.
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        public static DataColumnExtendedInfo[] CreateForTable(DataTable dataTable)
        {
            int count = dataTable.Columns.Count;
            DataColumnExtendedInfo[] infos = new DataColumnExtendedInfo[count];
            for (int c = 0; c < count; c++)
                infos[c] = new DataColumnExtendedInfo(dataTable.Columns[c]);
            return infos;
        }
        /// <summary>
        /// Vrací informace o daném sloupci, které načte z Extended properties daného sloupce 
        /// (tam je uložil Helios Green v metodě BrowseTemplateInfo.GetTemplateData(int, int?, int?, BigFilter, int?)).
        /// Referenci na sloupec, předaný sem jako parametr, si this instance ukládá, a dovoluje tyto informace měnit a vkládat změny zase do sloupce.
        /// </summary>
        /// <param name="dataColumn"></param>
        /// <returns></returns>
        public static DataColumnExtendedInfo CreateForColumn(DataColumn dataColumn)
        {
            DataColumnExtendedInfo info = new DataColumnExtendedInfo(dataColumn);
            return info;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        private DataColumnExtendedInfo(DataColumn dataColumn)
        {
            this._DataColumn = dataColumn;
        }
        private DataColumn _DataColumn;
        /// <summary>
        /// Vrátí hodnotu požadovaného typu z dané property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected int GetPropertyValue(string propertyName, int defaultValue)
        {
            object value;
            if (!this.TryGetPropertyValue(propertyName, out value)) return defaultValue;
            if (value is Int32) return (Int32)value;
            if (value is Int16) return (Int32)value;
            int number;
            if (value is string && Int32.TryParse((string)value, out number)) return number;
            return defaultValue;
        }
        /// <summary>
        /// Vrátí hodnotu požadovaného typu z dané property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected int? GetPropertyValue(string propertyName, int? defaultValue)
        {
            object value;
            if (!this.TryGetPropertyValue(propertyName, out value)) return defaultValue;
            if (value is Int32) return (Int32)value;
            if (value is Int16) return (Int32)value;
            int number;
            if (value is string && Int32.TryParse((string)value, out number)) return number;
            return defaultValue;
        }
        /// <summary>
        /// Vrátí hodnotu požadovaného typu z dané property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected bool GetPropertyValue(string propertyName, bool defaultValue)
        {
            object value;
            if (!this.TryGetPropertyValue(propertyName, out value)) return defaultValue;
            if (value == null) return defaultValue;
            if (value is Boolean) return (Boolean)value;
            if (!(value is String)) return defaultValue;
            string text = ((string)value).Trim();
            return String.Equals(text, "true", StringComparison.InvariantCultureIgnoreCase);
        }
        /// <summary>
        /// Vrátí hodnotu požadovaného typu z dané property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected string GetPropertyValue(string propertyName, string defaultValue)
        {
            object value;
            if (!this.TryGetPropertyValue(propertyName, out value)) return defaultValue;
            if (value == null) return defaultValue;
            if (value is String) return (String)value;
            return value.ToString();
        }
        /// <summary>
        /// Pokusí se najít a vrátit hodnotu z dané property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected bool TryGetPropertyValue(string propertyName, out object value)
        {
            DataColumn dataColumn = this._DataColumn;
            value = null;
            if (dataColumn == null || dataColumn.ExtendedProperties.Count == 0 || !dataColumn.ExtendedProperties.ContainsKey(propertyName)) return false;
            value = dataColumn.ExtendedProperties[propertyName];
            return true;
        }
        /// <summary>
        /// Vloží danou hodnotu do daného klíče
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        protected void SetPropertyValue(string propertyName, object value)
        {
            DataColumn dataColumn = this._DataColumn;
            if (dataColumn == null) return;
            if (dataColumn.ExtendedProperties.Count == 0 || !dataColumn.ExtendedProperties.ContainsKey(propertyName))
                dataColumn.ExtendedProperties.Add(propertyName, value);
            else
                dataColumn.ExtendedProperties[propertyName] = value;
        }
        /// <summary>
        /// Metoda vrátí typovou hodnotu <see cref="BrowseColumnType"/> ze stringu
        /// </summary>
        /// <param name="text"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected static BrowseColumnType GetEnum(string text, BrowseColumnType defaultValue)
        {
            if (!String.IsNullOrEmpty(text))
            {
                switch (text)
                {
                    case "SubjectNumber": return BrowseColumnType.SubjectNumber;
                    case "ObjectNumber": return BrowseColumnType.ObjectNumber;
                    case "DataColumn": return BrowseColumnType.DataColumn;
                    case "RelationHelpfulColumn": return BrowseColumnType.RelationHelpfulColumn;
                    case "TotalCountHelpfulColumn": return BrowseColumnType.TotalCountHelpfulColumn;
                }
            }
            return defaultValue;
        }
        /// <summary>
        /// Metoda vrátí typovou hodnotu <see cref="BrowseColumnType"/> ze stringu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string GetText(BrowseColumnType value)
        {
            switch (value)
            {
                case BrowseColumnType.SubjectNumber: return "SubjectNumber";
                case BrowseColumnType.ObjectNumber: return "ObjectNumber";
                case BrowseColumnType.DataColumn: return "DataColumn";
                case BrowseColumnType.RelationHelpfulColumn: return "RelationHelpfulColumn";
                case BrowseColumnType.TotalCountHelpfulColumn: return "TotalCountHelpfulColumn";
            }
            return "";
        }
        #endregion
        #region Public properties, obsahující hodnoty
        /// <summary>
        /// Číslo třídy, z níž pochází data šablony
        /// </summary>
        public int? ClassNumber { get { return this.GetPropertyValue("ClassNumber", (int?)null); } set { this.SetPropertyValue("ClassNumber", value); } }
        /// <summary>
        /// Číslo šablony
        /// </summary>
        public int? TemplateNumber { get { return this.GetPropertyValue("TemplateNumber", (int?)null); } set { this.SetPropertyValue("TemplateNumber", value); } }
        /// <summary>
        /// Alias sloupce = ColumnName
        /// </summary>
        public string Alias { get { return this.GetPropertyValue("Alias", ""); } set { this.SetPropertyValue("Alias", value); } }
        /// <summary>
        /// Povolit řádkové filtrování
        /// </summary>
        public bool AllowRowFilter { get { return this.GetPropertyValue("AllowRowFilter", true); } set { this.SetPropertyValue("AllowRowFilter", value); } }
        /// <summary>
        /// Povolit třídění
        /// </summary>
        public bool AllowSort { get { return this.GetPropertyValue("AllowSort", true); } set { this.SetPropertyValue("AllowSort", value); } }
        /// <summary>
        /// Typ sloupce v přehledu: pomocný, datový, ... Zobrazují se vždy jen sloupce typu DataColumn, ostatní sloupce jsou pomocné.
        /// Aktuálně hodnoty: SubjectNumber, ObjectNumber, DataColumn, RelationHelpfulColumn, TotalCountHelpfulColumn
        /// </summary>
        public BrowseColumnType BrowseColumnType { get { return GetEnum(this.GetPropertyValue("BrowseColumnType", ""), BrowseColumnType.None); } set { this.SetPropertyValue("BrowseColumnType", GetText(value)); } }
        /// <summary>
        /// Název sloupce v SQL selectu
        /// </summary>
        public string CodeName_FromSelect { get { return this.GetPropertyValue("CodeName_FromSelect", ""); } set { this.SetPropertyValue("CodeName_FromSelect", value); } }
        /// <summary>
        /// Název sloupce uvedený v definici šablony
        /// </summary>
        public string CodeName_FromTemplate { get { return this.GetPropertyValue("CodeName_FromTemplate", ""); } set { this.SetPropertyValue("CodeName_FromTemplate", value); } }
        /// <summary>
        /// Číslo vztahu, z něhož pochází tento sloupec
        /// </summary>
        public int ColRelNum { get { return this.GetPropertyValue("ColRelNum", 0); } set { this.SetPropertyValue("ColRelNum", value); } }
        /// <summary>
        /// Datový typ sloupce - NrsTypes (může se lišit od Repozitory, upravuje se dle dat, která se načtou do přehledu - DDLB, ...)
        /// </summary>
        public string ColType { get { return this.GetPropertyValue("ColType", ""); } set { this.SetPropertyValue("ColType", value); } }
        /// <summary>
        /// Datový typ sloupce - NrsTypes (definice dle Repozitory - to, co je vidět)
        /// </summary>
        public string DataTypeRepo { get { return this.GetPropertyValue("DataTypeRepo", ""); } set { this.SetPropertyValue("DataTypeRepo", value); } }
        /// <summary>
        /// Datový typ sloupce - c# SystemTypes
        /// </summary>
        public string DataTypeSystem { get { return this.GetPropertyValue("DataTypeSystem", ""); } set { this.SetPropertyValue("DataTypeSystem", value); } }
        /// <summary>
        /// Formát sloupce v přehledu
        /// </summary>
        public string Format { get { return this.GetPropertyValue("Format", ""); } set { this.SetPropertyValue("Format", value); } }
        /// <summary>
        /// Vrátí index sloupce v seznamu sloupců. Pokud sloupec do žádného seznamu nepatří, vrátí -1.
        /// </summary>
        public int Index { get { return this._DataColumn.Ordinal; } }
        /// <summary>
        /// Informace o viditelnosti sloupce (zda má být vidět v přehledu)
        /// </summary>
        public bool IsVisible { get { return this.GetPropertyValue("IsVisible", true); } set { this.SetPropertyValue("IsVisible", value); } }
        /// <summary>
        /// Nadpis sloupce v přehledu
        /// </summary>
        public string Label { get { return this.GetPropertyValue("Label", ""); } set { this.SetPropertyValue("Label", value); } }
        /// <summary>
        /// Pořadí sloupce v přehledu - pořadí zobrazení
        /// </summary>
        public int? SortIndex { get { return this.GetPropertyValue("SortIndex", (int?)null); } set { this.SetPropertyValue("SortIndex", value); } }
        /// <summary>
        /// Šířka sloupce v přehledu
        /// </summary>
        public int Width { get { return this.GetPropertyValue("Width", 0); } set { this.SetPropertyValue("Width", value); } }
        /// <summary>
        /// Číslo třídy vztaženého záznamu v tomto sloupci
        /// </summary>
        public int? RelationClassNumber { get { return this.GetPropertyValue("RelationClassNumber", (int?)null); } set { this.SetPropertyValue("RelationClassNumber", value); } }
        /// <summary>
        /// Číslo vztahu v tomto sloupci, je rovno <see cref="ColRelNum"/>
        /// </summary>
        public int? RelationNumber { get { return this.GetPropertyValue("RelationNumber", (int?)null); } set { this.SetPropertyValue("RelationNumber", value); } }
        /// <summary>
        /// Strana vztahu: Undefined, Left, Right
        /// </summary>
        public string RelationSide { get { return this.GetPropertyValue("RelationSide", ""); } set { this.SetPropertyValue("RelationSide", value); } }
        /// <summary>
        /// Databáze, kde máme hledat vztah (Product, Archival)
        /// </summary>
        public string RelationVolumeType { get { return this.GetPropertyValue("RelationVolumeType", ""); } set { this.SetPropertyValue("RelationVolumeType", value); } }
        /// <summary>
        /// Alias tabulky, která nese číslo záznamu ve vztahu pro jeho rozkliknutí.
        /// Typický obsah: "TabGS_1_4".
        /// Jednoduchý návod, kterak vyhledati název sloupce této tabulky, ve kterém jest uloženo číslo záznamu v tomto vztahu:
        /// $"H_RN_{RelationNumber}_{RelationTableAlias}_RN_H", tedy ve výsledku: "H_RN_102037_TabGS_1_4_RN_H".
        /// Zcela stačí načíst obsah property <see cref="RelationRecordColumnName"/>.
        /// </summary>
        public string RelationTableAlias { get { return this.GetPropertyValue("RelationTableAlias", ""); } set { this.SetPropertyValue("RelationTableAlias", value); } }
        /// <summary>
        /// Název sloupce, který obsahuje číslo záznamu, jehož reference nebo název jsou v aktuálním sloupci zobrazeny.
        /// </summary>
        public string RelationRecordColumnName { get { return (this.RelationNumber != 0 && !String.IsNullOrEmpty(this.RelationTableAlias) ? "H_RN_" + RelationNumber + "_" + RelationTableAlias + "_RN_H" : ""); } }
        /// <summary>
        /// true pokud tento sloupec má být k dispozici uživateli (jeho viditelnost se pak řídí pomocí <see cref="IsVisible"/>),
        /// false pro sloupce "systémové", které se nikdy nezobrazují.
        /// </summary>
        public bool ColumnIsForUser { get { return (this.BrowseColumnType == BrowseColumnType.DataColumn); } }
        /// <summary>
        /// Hodnota <see cref="System.Data.DataColumn.DataType"/>
        /// </summary>
        public Type ColumnType { get { return this._DataColumn.DataType; } }
        /// <summary>
        /// Hodnota <see cref="System.Data.DataColumn.ColumnName"/>
        /// </summary>
        public string ColumnName { get { return this._DataColumn.ColumnName; } }
        #endregion
        #region Support
        /// <summary>
        /// Metoda, která jednoduše nastaví daný sloupec tabulky tak, aby byl zobrazen uživateli v Gridu.
        /// Metoda nastaví hodnoty podle parametrů, a navíc nastaví: <see cref="BrowseColumnType"/> = <see cref="BrowseColumnType.DataColumn"/>;
        /// a <see cref="IsVisible"/> = true.
        /// </summary>
        /// <param name="label">Titulkový text sloupce</param>
        /// <param name="width">Šířka sloupce v pixelech</param>
        /// <param name="allowSort">Povolit třídění, výchozí = true</param>
        /// <param name="format">Formátovací string pro obsah sloupce, výchozí = null</param>
        /// <param name="allowRowFilter">Povolit řádkový filtr pro sloupec, výchozí = false</param>
        public void PrepareDataColumn(string label, int width, bool allowSort = true, string format = null, bool allowRowFilter = false)
        {
            this.Label = label;
            this.Width = width;
            this.BrowseColumnType = BrowseColumnType.DataColumn;
            this.IsVisible = true;
            this.AllowRowFilter = allowRowFilter;
            this.AllowSort = allowSort;
            this.Format = format;
        }
        #endregion
    }
    /// <summary>
    /// DataColumnsExtendedInfo : Třída obsahující rozšířené informace o všech sloupcích tabulky <see cref="DataColumn"/>),
    /// </summary>
    public class DataColumnsExtendedInfo
    {
        #region Konstrukce, načtení dat
        /// <summary>
        /// Vytvoří instanci <see cref="DataColumnsExtendedInfo"/> pro sloupce dané tabulky.
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        public static DataColumnsExtendedInfo CreateForTable(DataTable dataTable)
        {
            if (dataTable == null) return null;
            return new DataColumnsExtendedInfo(dataTable);
        }
        /// <summary>
        /// Konstruktor, rovnou načte data
        /// </summary>
        /// <param name="dataTable"></param>
        private DataColumnsExtendedInfo(DataTable dataTable)
        { 
            int count = dataTable.Columns.Count;

            this._InfoIndexDict = new Dictionary<int, DataColumnExtendedInfo>();
            this._InfoNameDict = new Dictionary<string, DataColumnExtendedInfo>();
            for (int c = 0; c < count; c++)
            {
                DataColumn column = dataTable.Columns[c];
                DataColumnExtendedInfo info = DataColumnExtendedInfo.CreateForColumn(column);
                this._InfoIndexDict.Add(c, info);
                string key = GetKey(column.ColumnName);
                if (!this._InfoNameDict.ContainsKey(key))
                    this._InfoNameDict.Add(key, info);
            }
        }
        private Dictionary<int, DataColumnExtendedInfo> _InfoIndexDict;
        private Dictionary<string, DataColumnExtendedInfo> _InfoNameDict;
        private static string GetKey(string columnName)
        {
            return (columnName == null ? "" : columnName.Trim().ToLower());
        }
        #endregion
        #region Public properties
        /// <summary>
        /// Vrátí <see cref="DataColumnExtendedInfo"/> pro sloupec na daném indexu.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public DataColumnExtendedInfo this[int index] { get { return this._InfoIndexDict[index]; } }
        /// <summary>
        /// Vrátí <see cref="DataColumnExtendedInfo"/> pro sloupec daného jména.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public DataColumnExtendedInfo this[string columnName] { get { return this._InfoNameDict[GetKey(columnName)]; } }
        /// <summary>
        /// Počet položek v tomto úložišti, odpovídá počtu sloupců tabulky
        /// </summary>
        public int Count { get { return this._InfoIndexDict.Count; } }
        #endregion
    }
    #region Enum BrowseColumnType
    /// <summary>
    /// Typ dat ve sloupci
    /// </summary>
    public enum BrowseColumnType
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None = 0,
        /// <summary>
        /// Číslo [non]subjektu
        /// </summary>
        SubjectNumber,
        /// <summary>
        /// Číslo objektu
        /// </summary>
        ObjectNumber,
        /// <summary>
        /// Data zobrazovaná uživateli
        /// </summary>
        DataColumn,
        /// <summary>
        /// Data pomocná pro řešení statického vztahu
        /// </summary>
        RelationHelpfulColumn,
        /// <summary>
        /// Informace o počtu záznamů
        /// </summary>
        TotalCountHelpfulColumn
    }
    #endregion
    #endregion
}
