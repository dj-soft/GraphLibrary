﻿// Supervisor: DAJ
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
namespace Noris.LCS.Manufacturing.WorkScheduler
{
    #region Data předávaná mezi Helios Green a WorkScheduler. Identický balík je v GraphLib\Scheduler\WorkSchedulerDataSync.cs a v Manufacturing\WorkSchedulerShared.cs
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
            this.ToolbarItems = new GuiToolbarPanel() { Name = TOOLBAR_NAME };
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
            this.GraphLineHeight = 20;
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
        /// Určuje, tedy kolik pixelů bude vysoký prvek <see cref="GuiGraphItem"/>, jehož <see cref="GuiGraphItem.Height"/> = 1.0f.
        /// Pokud je null, bude použit default definovaný v GUI.
        /// </summary>
        public int GraphLineHeight { get; set; }
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
        /// <see cref="Height"/> * <see cref="GuiGraphProperties.GraphLineHeight"/>
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
        public const string DATA_TABLE_GRAPH_STRUCTURE = "parent_rec_id int; parent_class_id int; item_rec_id int; item_class_id int; group_rec_id int; group_class_id int; data_rec_id int; data_class_id int; layer int; level int; is_user_fixed int; time_begin datetime; time_end datetime; height decimal; ratio decimal; back_color string; join_back_color string; data string";

        /// <summary>
        /// Key v Response: Status
        /// </summary>
        public const string KEY_RESPONSE_RESULT_STATUS = "ResultStatus";
        /// <summary>
        /// Key v Response: Message
        /// </summary>
        public const string KEY_RESPONSE_RESULT_MESSAGE = "ResultMessage";

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
        /// Vrací informace o daném sloupci, které načte z Extended properties daného sloupce 
        /// (tam je uložit Helios Green v metodě BrowseTemplateInfo.GetTemplateData(int, int?, int?, BigFilter, int?)).
        /// Referenci na sloupec, předaný sem jako parametr, si this instance neukládá, data z něj v této metodě fyzicky načte do svých jednoduchých proměnných.
        /// Sloupec může být poté zahozen, jeho data budou opsána zde.
        /// </summary>
        /// <param name="dataColumn"></param>
        /// <returns></returns>
        public static DataColumnExtendedInfo CreateForColumn(DataColumn dataColumn)
        {
            DataColumnExtendedInfo info = new DataColumnExtendedInfo();
            info._LoadFromDataColumn(dataColumn);
            return info;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        private DataColumnExtendedInfo() { }
        /// <summary>
        /// Načte data z ownera do properties
        /// </summary>
        private void _LoadFromDataColumn(DataColumn dataColumn)
        {
            this.ClassNumber = GetPropertyValue(dataColumn, "ClassNumber", (int?)null);
            this.TemplateNumber = GetPropertyValue(dataColumn, "TemplateNumber", (int?)null);
            this.Alias = GetPropertyValue(dataColumn, "Alias", "");
            this.AllowRowFilter = GetPropertyValue(dataColumn, "AllowRowFilter", true);
            this.AllowSort = GetPropertyValue(dataColumn, "AllowSort", true);
            this.BrowseColumnType = GetPropertyValue(dataColumn, "BrowseColumnType", "");
            this.CodeName_FromSelect = GetPropertyValue(dataColumn, "CodeName_FromSelect", "");
            this.CodeName_FromTemplate = GetPropertyValue(dataColumn, "CodeName_FromTemplate", "");
            this.ColRelNum = GetPropertyValue(dataColumn, "ColRelNum", 0);
            this.ColType = GetPropertyValue(dataColumn, "ColType", "");
            this.DataTypeRepo = GetPropertyValue(dataColumn, "DataTypeRepo", "");
            this.DataTypeSystem = GetPropertyValue(dataColumn, "DataTypeSystem", "");
            this.Format = GetPropertyValue(dataColumn, "Format", "");
            this.Index = GetPropertyValue(dataColumn, "Index", 0);
            this.IsVisible = GetPropertyValue(dataColumn, "IsVisible", true);
            this.Label = GetPropertyValue(dataColumn, "Label", "");
            this.SortIndex = GetPropertyValue(dataColumn, "SortIndex", (int?)null);
            this.Width = GetPropertyValue(dataColumn, "Width", 0);
            this.RelationClassNumber = GetPropertyValue(dataColumn, "RelationClassNumber", (int?)null);
            this.RelationNumber = GetPropertyValue(dataColumn, "RelationNumber", (int?)null);
            this.RelationSide = GetPropertyValue(dataColumn, "RelationSide", "");
            this.RelationVolumeType = GetPropertyValue(dataColumn, "RelationVolumeType", "");
            this.RelationTableAlias = GetPropertyValue(dataColumn, "RelationTableAlias", "");
        }
        /// <summary>
        /// Vrátí hodnotu požadovaného typu z dané property
        /// </summary>
        /// <param name="dataColumn"></param>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected int GetPropertyValue(DataColumn dataColumn, string propertyName, int defaultValue)
        {
            object value;
            if (!this.TryGetPropertyValue(dataColumn, propertyName, out value)) return defaultValue;
            if (value is Int32) return (Int32)value;
            if (value is Int16) return (Int32)value;
            int number;
            if (value is string && Int32.TryParse((string)value, out number)) return number;
            return defaultValue;
        }
        /// <summary>
        /// Vrátí hodnotu požadovaného typu z dané property
        /// </summary>
        /// <param name="dataColumn"></param>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected int? GetPropertyValue(DataColumn dataColumn, string propertyName, int? defaultValue)
        {
            object value;
            if (!this.TryGetPropertyValue(dataColumn, propertyName, out value)) return defaultValue;
            if (value is Int32) return (Int32)value;
            if (value is Int16) return (Int32)value;
            int number;
            if (value is string && Int32.TryParse((string)value, out number)) return number;
            return defaultValue;
        }
        /// <summary>
        /// Vrátí hodnotu požadovaného typu z dané property
        /// </summary>
        /// <param name="dataColumn"></param>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected bool GetPropertyValue(DataColumn dataColumn, string propertyName, bool defaultValue)
        {
            object value;
            if (!this.TryGetPropertyValue(dataColumn, propertyName, out value)) return defaultValue;
            if (value == null) return defaultValue;
            if (value is Boolean) return (Boolean)value;
            if (!(value is String)) return defaultValue;
            string text = ((string)value).Trim();
            return String.Equals(text, "true", StringComparison.InvariantCultureIgnoreCase);
        }
        /// <summary>
        /// Vrátí hodnotu požadovaného typu z dané property
        /// </summary>
        /// <param name="dataColumn"></param>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected string GetPropertyValue(DataColumn dataColumn, string propertyName, string defaultValue)
        {
            object value;
            if (!this.TryGetPropertyValue(dataColumn, propertyName, out value)) return defaultValue;
            if (value == null) return defaultValue;
            if (value is String) return (String)value;
            return value.ToString();
        }
        /// <summary>
        /// POkusí se najít a vrátit hodnotu z dané property
        /// </summary>
        /// <param name="dataColumn"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected bool TryGetPropertyValue(DataColumn dataColumn, string propertyName, out object value)
        {
            value = null;
            if (dataColumn == null || dataColumn.ExtendedProperties.Count == 0 || !dataColumn.ExtendedProperties.ContainsKey(propertyName)) return false;
            value = dataColumn.ExtendedProperties[propertyName];
            return true;
        }
        #endregion
        #region Public properties, obsahující hodnoty
        /// <summary>
        /// Číslo třídy, z níž pochází data šablony
        /// </summary>
        public int? ClassNumber { get; private set; }
        /// <summary>
        /// Číslo šablony
        /// </summary>
        public int? TemplateNumber { get; private set; }
        /// <summary>
        /// Alias sloupce = ColumnName
        /// </summary>
        public string Alias { get; private set; }
        /// <summary>
        /// Povolit řádkové filtrování
        /// </summary>
        public bool AllowRowFilter { get; private set; }
        /// <summary>
        /// Povolit třídění
        /// </summary>
        public bool AllowSort { get; private set; }
        /// <summary>
        /// Typ sloupce v přehledu: pomocný, datový, ... Zobrazují se vždy jen sloupce typu DataColumn, ostatní sloupce jsou pomocné.
        /// Aktuálně hodnoty: SubjectNumber, ObjectNumber, DataColumn, RelationHelpfulColumn, TotalCountHelpfulColumn
        /// </summary>
        public string BrowseColumnType { get; private set; }
        /// <summary>
        /// Název sloupce v SQL selectu
        /// </summary>
        public string CodeName_FromSelect { get; private set; }
        /// <summary>
        /// Název sloupce uvedený v definici šablony
        /// </summary>
        public string CodeName_FromTemplate { get; private set; }
        /// <summary>
        /// Číslo vztahu, z něhož pochází tento sloupec
        /// </summary>
        public int ColRelNum { get; private set; }
        /// <summary>
        /// Datový typ sloupce - NrsTypes (může se lišit od Repozitory, upravuje se dle dat, která se načtou do přehledu - DDLB, ...)
        /// </summary>
        public string ColType { get; private set; }
        /// <summary>
        /// Datový typ sloupce - NrsTypes (definice dle Repozitory - to, co je vidět)
        /// </summary>
        public string DataTypeRepo { get; private set; }
        /// <summary>
        /// Datový typ sloupce - c# SystemTypes
        /// </summary>
        public string DataTypeSystem { get; private set; }
        /// <summary>
        /// Formát sloupce v přehledu
        /// </summary>
        public string Format { get; private set; }
        /// <summary>
        /// Vrátí index sloupce v seznamu sloupců. Pokud sloupec do žádného seznamu nepatří, vrátí -1.
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// Informace o viditelnosti sloupce (zda má být vidět v přehledu)
        /// </summary>
        public bool IsVisible { get; private set; }
        /// <summary>
        /// Nadpis sloupce v přehledu
        /// </summary>
        public string Label { get; private set; }
        /// <summary>
        /// Pořadí sloupce v přehledu - pořadí zobrazení
        /// </summary>
        public int? SortIndex { get; private set; }
        /// <summary>
        /// Šířka sloupce v přehledu
        /// </summary>
        public int Width { get; private set; }
        /// <summary>
        /// Číslo třídy vztaženého záznamu v tomto sloupci
        /// </summary>
        public int? RelationClassNumber { get; private set; }
        /// <summary>
        /// Číslo vztahu v tomto sloupci, je rovno <see cref="ColRelNum"/>
        /// </summary>
        public int? RelationNumber { get; private set; }
        /// <summary>
        /// Strana vztahu: Undefined, Left, Right
        /// </summary>
        public string RelationSide { get; private set; }
        /// <summary>
        /// Databáze, kde máme hledat vztah (Product, Archival)
        /// </summary>
        public string RelationVolumeType { get; private set; }
        /// <summary>
        /// Alias tabulky, která nese číslo záznamu ve vztahu pro jeho rozkliknutí.
        /// Typický obsah: "TabGS_1_4".
        /// Jednoduchý návod, kterak vyhledati název sloupce této tabulky, ve kterém jest uloženo číslo záznamu v tomto vztahu:
        /// $"H_RN_{RelationNumber}_{RelationTableAlias}_RN_H", tedy ve výsledku: "H_RN_102037_TabGS_1_4_RN_H".
        /// Zcela stačí načíst obsah property <see cref="RelationRecordColumnName"/>.
        /// </summary>
        public string RelationTableAlias { get; private set; }
        /// <summary>
        /// Název sloupce, který obsahuje číslo záznamu, jehož reference nebo název jsou v aktuálním sloupci zobrazeny.
        /// </summary>
        public string RelationRecordColumnName { get { return (this.RelationNumber != 0 && !String.IsNullOrEmpty(this.RelationTableAlias) ? "H_RN_" + RelationNumber + "_" + RelationTableAlias + "_RN_H" : ""); } }
        /// <summary>
        /// true pokud tento sloupec má být k dispozici uživateli (jeho viditelnost se pak řídí pomocí <see cref="IsVisible"/>),
        /// false pro sloupce "systémové", které se nikdy nezobrazují.
        /// </summary>
        public bool ColumnIsForUser { get { return (!String.IsNullOrEmpty(this.BrowseColumnType) && this.BrowseColumnType == "DataColumn"); } }
        #endregion
    }
    #endregion
}