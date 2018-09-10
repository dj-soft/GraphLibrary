using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Components;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    #region Data předávaná mezi Helios Green a WorkScheduler. Identický balík je v GraphLib\Scheduler\WorkSchedulerDataSync.cs a v Manufacturing\WorkSchedulerDataSync.cs
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
            this.ToolbarItems = new GuiToolbarPanel() { Name = TOOLBAR_NAME  };
            this.Pages = new GuiPages() { Name = PAGES_NAME };
            this.ContextMenuItems = new GuiContextMenuSet() { Name = CONTEXT_MENU_NAME };
        }
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
        public void Finalise()
        {
            this.FillParentToChilds();
        }
        /// <summary>
        /// Metoda najde prvek na základě jeho plného jména
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
    #region GuiGrid : obsahuje veškeré data pro zobrazení jedné tabulky v WorkScheduler pluginu
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
    #region GuiTable : Jedna fyzická tabulka (ekvivalent DataTable, s podporou serializace)
    /// <summary>
    /// GuiTable : Jedna fyzická tabulka (ekvivalent DataTable, s podporou serializace)
    /// </summary>
    public sealed class GuiTable : GuiBase
    {
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
                return this._DataTable;
            }
            set
            {
                this._DataTable = value;
                this._DataTableValid = true;
                this._DataSerialValid = false;
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
        private string PersistedData
        {
            get { this._DataSerialValid = false; return this.DataSerial; }
            set { this.DataSerial = value; }
        }
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
            this.OneLineHeight = 20;
            this.UpperSpaceLogical = 1f;
            this.BottomMarginPixel = 1;
            this.TotalHeightMin = 22;
            this.TotalHeightMax = 200;
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
        /// Fyzická výška jedné logické linky grafu v pixelech.
        /// Určuje, tedy kolik pixelů bude vysoký prvek <see cref="GuiGraphItem"/>, jehož <see cref="GuiGraphItem.Height"/> = 1.0f.
        /// Pokud je null, bude použit default definovaný v GUI.
        /// </summary>
        public int? OneLineHeight { get; set; }
        /// <summary>
        /// Horní okraj = prostor nad nejvyšším prvkem grafu, který by měl být zobrazen jako prázdný, tak aby bylo vidět že nic dalšího už není.
        /// V tomto prostoru (těsně pod souřadnicí Top) se provádí Drag and Drop prvků.
        /// Hodnota je zadána v logických jednotkách, tedy v počtu standardních linek.
        /// Výchozí hodnota = 1.0 linka, nelze zadat zápornou hodnotu.
        /// </summary>
        public float? UpperSpaceLogical { get; set; }
        /// <summary>
        /// Dolní okraj = mezera pod dolním okrajem nejnižšího prvku grafu k dolnímu okraji controlu, v pixelech.
        /// </summary>
        public int? BottomMarginPixel { get; set; }
        /// <summary>
        /// Výška celého grafu, nejmenší přípustná, v pixelech.
        /// </summary>
        public int? TotalHeightMin { get; set; }
        /// <summary>
        /// Výška celého grafu, největší přípustná, v pixelech.
        /// </summary>
        public int? TotalHeightMax { get; set; }
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
    public sealed class GuiGraphItem : GuiBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiGraphItem()
        {
            this.Height = 1f;
            this.BehaviorMode = GraphItemBehaviorMode.DefaultText;
        }
        /// <summary>
        /// Obsahuje sloupce, které jsou povinné.
        /// Pokud dodaná data nebudou obsahovat některý z uvedených sloupců, načítání se neprovede.
        /// </summary>
        public const string StructureLiable = "item_class_id int, item_record_id int, parent_class_id int, parent_record_id int, begin datetime, end datetime";
        /// <summary>
        /// Obsahuje všechny sloupce, které jsou načítané do dat třídy <see cref="GuiGraphItem"/>, tj. i ty nepovinné.
        /// </summary>
        public const string StructureFull =
                    "item_class_id int, item_record_id int, parent_class_id int, parent_record_id int, " +
                    "group_class_id int, group_record_id int, data_class_id int, data_record_id" +
                    "begin datetime, end datetime, " +
                    "layer int, level int, order int, height float, ratio float, " +
                    "back_color string, line_color string, back_style string";
        /// <summary>
        /// Jméno prvku GuiGraphItem je vždy rovno textu z <see cref="ItemId"/>. Property Name zde nemá význam setovat.
        /// </summary>
        public override string Name
        {
            get { return (this.ItemId != null ? this.ItemId.Name : "NULL"); }
            set { }
        }
        /// <summary>
        /// ID položky časového grafu (ID obdélníčku).
        /// Z databáze se načítá ze sloupců: "item_class_id", "item_record_id", je POVINNÝ.
        /// </summary>
        public GuiId ItemId { get; set; }
        /// <summary>
        /// ID řádku, v jehož grafu se má tento prvek zobrazovat.
        /// Z databáze se načítá ze sloupců: "parent_class_id", "parent_record_id", je POVINNÝ.
        /// </summary>
        public GuiId ParentRowId { get; set; }
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
        /// Z databáze se načítá ze sloupce: "begin", je POVINNÝ.
        /// </summary>
        public DateTime Begin { get; set; }
        /// <summary>
        /// Datum a čas konce tohoto prvku.
        /// Z databáze se načítá ze sloupce: "end", je POVINNÝ.
        /// </summary>
        public DateTime End { get; set; }
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
        /// Prvky se stejným Order budou tříděny vzestupně podle data počátku <see cref="Begin"/>.
        /// Z databáze se načítá ze sloupce: "order", je NEPOVINNÝ.
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// Relativní výška tohoto prvku. Standardní hodnota = 1.0F. Fyzická výška (v pixelech) jednoho prvku je dána součinem 
        /// <see cref="Height"/> * <see cref="GuiGraphProperties.OneLineHeight"/>
        /// Prvky s výškou 0 a menší nebudou vykresleny.
        /// Z databáze se načítá ze sloupce: "height", je NEPOVINNÝ.
        /// </summary>
        public float Height { get; set; }
        /// <summary>
        /// Poměrná hodnota "nějakého" splnění v rámci prvku.
        /// Z databáze se načítá ze sloupce: "ratio", je NEPOVINNÝ.
        /// </summary>
        public float? Ratio { get; set; }
        /// <summary>
        /// Barva pozadí prvku.
        /// Z databáze se načítá ze sloupce: "back_color", je NEPOVINNÝ.
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Barva linek ohraničení prvku.
        /// Z databáze se načítá ze sloupce: "line_color", je NEPOVINNÝ.
        /// </summary>
        public Color? LineColor { get; set; }
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
            this.ToolbarShowSystemItems = true;
            this.Items = new List<GuiToolbarItem>();
        }
        /// <summary>
        /// Zobrazovat toolbar?
        /// </summary>
        public bool ToolbarVisible { get; set; }
        /// <summary>
        /// Zobrazovat systémové položky v Toolbaru?
        /// </summary>
        public bool ToolbarShowSystemItems { get; set; }
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
            this.Size = FunctionGlobalItemSize.Half;
            this.LayoutHint = LayoutHint.Default;
        }
        /// <summary>
        /// Velikost prvku na toolbaru, vzhledem k výšce toolbaru
        /// </summary>
        public FunctionGlobalItemSize Size { get; set; }
        /// <summary>
        /// Explicitně požadovaná šířka prvku v počtu modulů, opatrně s ohledem na obsah textu
        /// </summary>
        public int? ModuleWidth { get; set; }
        /// <summary>
        /// Nápověda ke zpracování layoutu této položky (jak se tato položka řadí za ostatní položky)
        /// </summary>
        public LayoutHint LayoutHint { get; set; }
        /// <summary>
        /// Název grupy, v níž bude tato položka toolbaru zařazena. Nezadáno = bude v implicitní skupině "FUNKCE".
        /// </summary>
        public string GroupName { get; set; }
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
        /// Definice, kde se smí tato funkce zobrazovat.
        /// </summary>
        public string ShowOnlyFor { get; set; }
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
        /// Klíčové jméno, používané v aplikaci jako strojový název prvku
        /// </summary>
        public virtual string Name { get; set; }
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
        /// </summary>
        [PersistingEnabled(false)]
        public string FullName
        {
            get
            {
                string fullName = "";
                string separator = "";
                IGuiItem item = this;
                while (item != null)
                {
                    fullName = (item.Name == null ? "NULL" : item.Name) + separator + fullName;
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
        /// Metoda najde prvek na základě jeho plného jména
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
        /// Klíčové jméno, používané v aplikaci jako strojový název prvku
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
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiImage()
        { }
        /// <summary>
        /// Název souboru
        /// </summary>
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
    #endregion
}
