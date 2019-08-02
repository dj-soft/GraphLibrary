// Supervisor: DAJ
// Part of Helios Green, proprietary software, (c) LCS International, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with LCS International, a. s. 
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Tento soubor obsahuje sadu tříd a enumů, které popisují data pro plugin WorkScheduler. Tato data se plní v Greenu a přes XML persistor se předávají do pluginu WorkScheduler.
// Tento soubor se nachází jednak v Greenu: Noris\App\Lcs\Base\WorkSchedulerShared.cs, a zcela identický i v GraphLibrary: \GraphLib\Shared\WorkSchedulerShared.cs
namespace Noris.LCS.Base.WorkScheduler
{
    #region GuiData : data předávaná z Helios Green do WorkScheduler při spouštění Pluginu
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
        public new void Finalise()
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
            this.TimeChangeSend = TimeChangeSendMode.None;
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
        /// Požadovaný režim odesílání informací o změně času na časové ose z pluginu do servisní funkce.
        /// Podle tohoto režimu bude po změně času odeslán command <see cref="GuiRequest.COMMAND_TimeChange"/>.
        /// Servisní funkce může reagovat = donačte další data, a v <see cref="GuiResponse"/> předá nové řádky a nové prvky grafů pro nový časový interval.
        /// Plugin je zařadí do svých stávajících vizuálních dat a zobrazí je.
        /// Další informace v property
        /// </summary>
        public TimeChangeSendMode TimeChangeSend { get; set; }
        /// <summary>
        /// Zvětšení časového intervalu aktuální časové osy použité do requestu <see cref="GuiRequest.COMMAND_TimeChange"/>.
        /// Pokud je požadavek na odesílání tohoto commandu po změně času v režimu <see cref="TimeChangeSendMode.OnNewTime"/>, 
        /// pak plugin (v situaci, že aktuální viditelný čas zahrnuje úsek, pro který dosud nejsou načtena data) 
        /// rozšíří viditelný úsek v tomto poměru a odešle daný command s rozšířeným časem, a označí si tento rozšířený čas jako pokrytý budoucími daty.
        /// <para/>
        /// Příklad pro režim <see cref="TimeChangeSendMode.OnNewTime"/> (na číselné ose): 
        /// Pokud máme načtena data pro rozsah { 50 - 80 } a uživatel zobrazí rozsah { 60 - 80 }, command se neposílá (máme dostatek dat).
        /// Jakmile uživatel posune časovou osu na rozsah { 70 - 90 }, plugin zjistí, že se zobrazuje nepokrytá část časové osy { 80 - 90 }.
        /// Určí rozšířený časový interval pomocí tohoto koeficientu <see cref="TimeChangeSendEnlargement"/>, 
        /// například pro koeficient navýšení <see cref="TimeChangeSendEnlargement"/> = 0.5d se nově zobrazený interval { 70 - 90 } rozšíří : 
        /// aktuální délka = (90-70)=20, poměr navýšení = (1 + <see cref="TimeChangeSendEnlargement"/>) = 1.5d; nová délka = 1.5d * 20 = 30, 
        /// výsledný interval = { 65 - 95 }, následně si tento interval přidá do interní informace o pokrytých časech, a tento interval odešle:
        /// Odešle tedy command <see cref="GuiRequest.COMMAND_TimeChange"/> a předá informaci o čase { 65 - 95 } v property <see cref="GuiRequestCurrentState.TimeAxisEnlargedValue"/>.
        /// <para/>
        /// Optimální hodnota navýšení je 1 až 2. Hodnota 2 zajistí, že při zobrazení úseku určité délky budou načtena data stejné délky vlevo, a také vpravo.
        /// Hodnota 1 zajistí, že vlevo i vpravo bude načtena polovina dat z viditelné oblasti.
        /// <para/>
        /// Pokud koefcient <see cref="TimeChangeSendEnlargement"/> je null, 0 nebo záporný, pak se zvětšení neprovádí.
        /// </summary>
        public double? TimeChangeSendEnlargement { get; set; }
        /// <summary>
        /// Tento časový úsek je pokrytý výchozími daty a považuje se za známý.
        /// </summary>
        public GuiTimeRange TimeChangeInitialValue { get; set; }
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
        /// Reakce na DoubleClick v prostoru Časového grafu
        /// </summary>
        public GuiDoubleClickAction DoubleClickOnGraph { get; set; }
        /// <summary>
        /// Reakce na DoubleClick v prostoru Prvku na Časovém grafu
        /// </summary>
        public GuiDoubleClickAction DoubleClickOnGraphItem { get; set; }
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
    /// <summary>
    /// Druh reakce na DoubleClick
    /// </summary>
    public enum GuiDoubleClickAction
    {
        /// <summary>
        /// Žádná reakce
        /// </summary>
        None = 0,
        /// <summary>
        /// Otevřít přiměřený formulář
        /// </summary>
        OpenForm,
        /// <summary>
        /// Nastavit Zoom na časové ose na prvek / na všechny prvky v grafu
        /// </summary>
        TimeZoom
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
            this.Name = PAGES_NAME;
            this.ShowPageTitleAllways = true;
            this.Pages = new List<GuiPage>();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Name + "; Count: " + (this.Pages != null ? this.Pages.Count.ToString() : "{Null}");
        }
        /// <summary>
        /// Název prvku <see cref="GuiPage"/>
        /// </summary>
        public const string PAGES_NAME = "pages";
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
        public void Add(GuiPage item)
        {
            if (item == null) return;
            if (this.Pages == null)
                this.Pages = new List<GuiPage>();
            this.Pages.Add(item);
        }
        /// <summary>
        /// Přidá další prvky do this seznamu
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<GuiPage> items)
        {
            if (items == null) return;
            if (this.Pages == null)
                this.Pages = new List<GuiPage>();
            this.Pages.AddRange(items);
        }
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
        /// Dolní panel, typicky používaný pro informace (detailní data, výsledky, problémy)
        /// </summary>
        public GuiPanel BottomPanel { get; private set; }
        /// <summary>
        /// Sada tabulek, které mohou obsahovat texty pro prvky grafů.
        /// Výchozí hodnota je null.
        /// </summary>
        public List<GuiDataTable> GraphItemTextTables { get; set; }
        /// <summary>
        /// Sada tabulek, které mohou obsahovat tooltipy pro prvky grafů.
        /// Výchozí hodnota je null.
        /// </summary>
        public List<GuiDataTable> GraphItemToolTipTables { get; set; }
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
        /// Zobrazovat titulkový řádek nad jednotlivými tabulkami vždy = i když je jen jedna?
        /// </summary>
        public bool ShowTableTitleAllways { get; set; }
        /// <summary>
        /// Zobrazovat tlačítko "Minimalizovat" na liště tabulek.
        /// </summary>
        public bool ShowMinimizeButton { get; set; }
        /// <summary>
        /// Přidá další prvek do this seznamu
        /// </summary>
        /// <param name="item"></param>
        public void Add(GuiGrid item)
        {
            if (item == null) return;
            if (this.Grids == null)
                this.Grids = new List<GuiGrid>();
            this.Grids.Add(item);
        }
        /// <summary>
        /// Přidá další prvky do this seznamu
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<GuiGrid> items)
        {
            if (items == null) return;
            if (this.Grids == null)
                this.Grids = new List<GuiGrid>();
            this.Grids.AddRange(items);
        }
        /// <summary>
        /// Počet prvků v kolekci
        /// </summary>
        public int Count { get { return this.Grids.Count; } }
        /// <summary>
        /// Obsahuje prvek na daném indexu
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GuiGrid this[int index] { get { return this.Grids[index]; } }
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
    /// Obsahuje tabulku s řádky <see cref="RowTable"/>, ta obsahuje pro každý řádek časový graf v <see cref="GuiDataRow.Graph"/>.
    /// Popisky prvků grafů a tooltipy mohou být v grafu, nebo v externí tabulce v <see cref="GraphTextTable"/> a <see cref="GraphToolTipTable"/>.
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
            this.GridProperties = new GuiGridProperties() { Name = GRID_PROPERTIES_NAME };
            this.GraphProperties = new GuiGraphProperties() { Name = GRAPH_PROPERTIES_NAME };
        }
        /// <summary>
        /// Název prvku <see cref="GridProperties"/>
        /// </summary>
        public const string GRID_PROPERTIES_NAME = "gridProperties";
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
            if (this.RowTable != null)
                text = "RowTable: " + this.RowTable.ToString();
            return text;
        }
        /// <summary>
        /// Definuje vlastnosti této vizuální tabulky, vyjma vlastností grafů
        /// </summary>
        public GuiGridProperties GridProperties { get; set; }
        /// <summary>
        /// Definuje vlastnosti grafu pro tuto tabulku
        /// </summary>
        public GuiGraphProperties GraphProperties { get; set; }
        /// <summary>
        /// Tabulka obsahující řádky k zobrazení.
        /// Součástí řádků je i možnost vkládat grafy do buňky anebo na pozadí.
        /// Řádky přímo obsahují filtry <see cref="GuiTagItem"/>.
        /// Řádky obsahují statický vztah na Parent řádek <see cref="GuiDataRow.ParentRowGuiId"/>.
        /// Tabulka jako taková obsahuje sadu vztahy <see cref="GuiGraphLink"/> v <see cref="GuiDataTable.GraphLinks"/>.
        /// </summary>
        public GuiDataTable RowTable { get; set; }
        /// <summary>
        /// Tabulka obsahující texty pro grafy
        /// </summary>
        public GuiDataTable GraphTextTable { get; set; }
        /// <summary>
        /// Tabulka obsahující ToolTipy pro grafy
        /// </summary>
        public GuiDataTable GraphToolTipTable { get; set; }
        /// <summary>
        /// Pole obsahující seznam aktivních kláves v této tabulce.
        /// Jakmile uživatel stiskne některou klávesu z tohoto seznamu, bude vyvolána událost <see cref="GuiRequest.COMMAND_KeyPress"/>.
        /// Výchozí hodnota je null.
        /// </summary>
        public List<GuiKeyAction> ActiveKeys { get; set; }
        /// <summary>
        /// Potomek zde vrací soupis svých Child prvků
        /// </summary>
        [PersistingEnabled(false)]
        protected override IEnumerable<IGuiItem> Childs { get { return Union(this.GridProperties, this.GraphProperties, this.RowTable, this.GraphTextTable, this.GraphToolTipTable); } }
    }
    /// <summary>
    /// Pár klíčů Parent - Child pro tvorbu stromové struktury
    /// </summary>
    public class GuiParentChild
    {
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Parent: " + this.Parent + "; Child: " + this.Child;
        }
        /// <summary>
        /// Klíč řádku Parent.
        /// určuje řádek, pod kterým bude jako Sub-Node zobrazen řádek <see cref="Child"/>.
        /// Za určitých okolností může být <see cref="Parent"/> = null, a to pokud je definován Grid jako Dynamický strom, 
        /// kde se prvky Childs vyhodnocují dynamicky podle zobrazeného času. 
        /// Viz <see cref="GuiGridProperties.ChildRowsEvaluate"/>, a hodnoty v <see cref="GuiChildRowsEvaluateMode"/>.
        /// <see cref="GuiGridProperties.ChildRowsEvaluate"/>
        /// </summary>
        public GuiId Parent { get; set; }
        /// <summary>
        /// Klíč řádku Child. Je logicky povinný.
        /// Pokud nebude vyplněn, pak tento záznam <see cref="GuiParentChild"/> bude při zpracování ignorován.
        /// </summary>
        public GuiId Child { get; set; }
    }
    #endregion
    #region GuiGridProperties : definiční vlastnosti jedné vizuální tabulky, vyjma vlastností grafů
    /// <summary>
    /// GuiGridProperties : definiční vlastnosti jedné vizuální tabulky, vyjma vlastností grafů
    /// </summary>
    public sealed class GuiGridProperties : GuiBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiGridProperties() : base()
        {
            this.TagFilterEnabled = true;
            this.TagFilterItemHeight = 26;
            this.TagFilterItemMaxCount = 100;
        }
        /// <summary>
        /// Obsahuje true, pokud má být zobrazen TagFilter = filtr s jednotlivými štítky.
        /// Data pro filtr se ukládají do <see cref="GuiDataRow.TagItems"/>, reprezentují jednoduché pole prvků, 
        /// kde prvek obsahuje klíč svého řádku (řádek v dané tabulce) a dále obsahuje text štítku (zobrazuje se uživateli) plus volitelně barvy štítku.
        /// </summary>
        public bool TagFilterEnabled { get; set; }
        /// <summary>
        /// Výška prvků v objektu TagFilter, v pixelech.
        /// Výchozí hodnota = 26.
        /// Objekt TagFilter zobrazuje běžně jen jeden řádek prvků, a až při najetí myší se rozbalí dolů na potřebnou výšku.
        /// </summary>
        public int TagFilterItemHeight { get; set; }
        /// <summary>
        /// Nejvyšší počet prvků zobrazených v TagFilter.
        /// Pokud jich bude více, pak ty s nejmenším počtem výskytů v řádcích budou skryty.
        /// Výchozí hodnota = 100;
        /// </summary>
        public int TagFilterItemMaxCount { get; set; }
        /// <summary>
        /// Procento kulatých krajů jednotlivých prvků v TagFilter.
        /// 0 = hranaté prvky; 100 = 100% = čisté půlkruhy. Hodnoty mimo rozsah jsou zarovnané do rozsahu 0 až 100 (včetně).
        /// Výchozí hodnota = 35;
        /// </summary>
        public int TagFilterRoundItemPercent { get; set; }
        /// <summary>
        /// Barva pozadí filtru TagFilter
        /// </summary>
        public Color? TagFilterBackColor { get; set; }
        /// <summary>
        /// Režim pro nalezení Child řádků v této tabulce.
        /// Nezadáno = <see cref="GuiChildRowsEvaluateMode.Static"/>.
        /// </summary>
        public GuiChildRowsEvaluateMode? ChildRowsEvaluate { get; set; }
        /// <summary>
        /// Zdrojová tabulka pro Child řádky, pokud v <see cref="ChildRowsEvaluate"/> je nastaven bit <see cref="GuiChildRowsEvaluateMode.InOtherTable"/>
        /// </summary>
        public string ChildRowsTableName { get; set; }
        /// <summary>
        /// Specifikace chování pro přenos položek grafu pro Child řádek, pokud se přenáší z jiné tabulky <see cref="ChildRowsTableName"/>.
        /// V takovém přenosu lze zajistit přenášení položek ze zdrojového grafu (v tabulce <see cref="ChildRowsTableName"/>) do cílového grafu v this tabulce
        /// v závislosti na čísle třídy, z níž pochází prvek grafu (podle jeho <see cref="GuiGraphItem.ItemId"/>, hodnota <see cref="GuiId.ClassId"/>).
        /// Prvky různých tříd tak mohou být přenášeny různým způsobem.
        /// Typické je, že "Pracovní doba" se bude přenášet vždy bez dalších podmínek, 
        /// "Pracovní úkol" se bude přenášet jen tehdy, když spadá do zobrazeného času a má synchronní prvek v Parent řádku, a ostatní prvky se přenášet nebudou.
        /// Pokud bude nalezen prvek, jehož číslo třídy nebude zadáno v této property, pak se vyhledá definice pro třídu 0 (=implicitní režim).
        /// Pokud nebude zadána definice pro třídu 0, pak se použije chování "Always" = přenášet vždy.
        /// <para/>
        /// Hodnota je poskládaná do textu ve formátu "číslo třídy:chování;číslo třídy:chování;..."
        /// kde chování je string:
        /// "None" = nepřenášet nikdy; 
        /// "Always" = přenášet vždy; 
        /// "ExistsPair" = přenášet, jen když v Parent řádku existuje shodný prvek bez ohledu na synchronní čas;
        /// "SynchronPair" = přenášet, jen když jsou synchronní časy (v Parent řádku existuje shodný prvek s časem společným s prvekm v Child řádku).
        /// Chování je case-insensitive, a postačuje zadat první znak.
        /// <para/>
        /// Veškerá klíčová slova jsou obsažena v konstantách třídy <see cref="GuiGridProperties"/>.CopyClasses_***
        /// </summary>
        public string ChildRowsCopyClassesMode { get; set; }
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků z this tabulky do jiných tabulek.
        /// Tento string určuje, které řádky se mohou účastnit přesouvání.
        /// Pokud zde bude prázdný string, nebude se moci z této tabulky provádět Drag and Move! To je výchozí stav.
        /// Pokud chceme specifikovat, do kterých cílových prvků chceme povolit přenesení zdrojové řádky, musíme to zadat do <see cref="RowDragMoveToTarget"/>.
        /// Zdejší string může obsahovat následující infromace, oddělené mezerou nebo čárkou. Texty musí být zadány přesně, nikoli tedy např. lower-case.
        /// <para/>
        /// Klíčová slova: 
        /// "DragOnlyActiveRow" = Přesouvat se bude pouze řádek, který chytila myš. Ostatní označené řádky se přesouvat nebudou.;
        /// "DragActivePlusSelectedRows" = Přesouvat se budou řádky označené kliknutím plus řádek, který chytila myš. To je intuitivně nejvhodnější nastavení.;
        /// "DragOnlySelectedRows" = Přesouvat se budou pouze řádky označené kliknutím. Řádek, který chytila myš, se přesouvat nebude (tedy pokud není označen ikonkou).;
        /// "DragSelectedThenActiveRow" = Přesouvat se budou primárně řádky označené kliknutím (a ne aktivní). Ale pokud nejsou označeny žádné řádky, tak se přesune řádek, který chytila myš.
        /// Rozdíl od "DragActivePlusSelectedRows" je v tom, že tady se nebude přesouvat aktivní řádek (myší) pokud existují řádky označené (ikonkou).
        /// Pokud nebude zadaná žádná hodnota typu "Drag*", pak se nebude přesouvat nic.
        /// Pokud bude zadáno více hodnot typu "Drag*", pak platí první z nich.
        /// Typ řádku:
        /// "Root" = přesouvat pouze řádky na pozici Root ve stromu
        /// "Child" = přesouvat pouze řádky na pozici Child ve stromu
        /// "Master" = přesouvat pouze řádky Master (rozpoznává se v <see cref="GuiId"/> řádku, kde <see cref="GuiId.EntryId"/> musí být null);
        /// "Entry" = přesouvat pouze řádky Entry (rozpoznává se v <see cref="GuiId"/> řádku, kde <see cref="GuiId.EntryId"/> nesmí být null);
        /// "Class12345" = přesouvat pouze řádky dané třídy;
        /// "MasterClass12345" = pouze řádky Master z dané třídy;
        /// "EntryClass12345" = pouze řádky Entry z dané třídy;
        /// Pokud nebude určeno nic z Class, Master, Entry, pak se budou brát všechny řádky bez omezení.
        /// <para/>
        /// Příklad: "DragActiveRow Class1364" bude přesouvat pouze řádek pod myší (ale ne označené řádky), a pouze řádky třídy 1364.
        /// <para/>
        /// Veškerá klíčová slova jsou obsažena v konstantách třídy <see cref="GuiGridProperties"/>.RowDragSource_***
        /// </summary>
        public string RowDragMoveSource { get; set; }
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků z this tabulky do jiných tabulek, určení cílového prvku kam lze řádek přesunout.
        /// Pokud tento string nebude naplněn (prázdný = výchozí stav), pak bude proces Drag and Move nabízet všechny cílové objekty jako dostupné.
        /// To z hlediska logiky není ideální, ale technicky je to v pořádku. Povolení nebo zákaz procesu Drag and Move se řídí v property <see cref="RowDragMoveSource"/>.
        /// Pokud bude tento string patřičně naplněn, pak budou potenciální cílové objekty filtrovány, a ne každý se bude nabízet jako dostupný cíl.
        /// <para/>
        /// String má obsahovat: FullName cílové tabulky kam lze řádek přenést, a několik klíčových slov, vše odděleno čárkami nebo mezerami.
        /// Více tabulek a jejich klíčových slov se odděluje středníkem. Texty musí být zadány přesně, nikoli tedy např. lower-case.
        /// Klíčová slova: 
        /// "RowRoot" = pouze do řádku Root;
        /// "RowChild" = pouze do řádku Child;
        /// "RowAny" = jakýkoli řádek (tato hodnota je implicitní, pokud nebude použita žádná jiná hodnota "Row*");
        /// "ToCell" = kamkoliv do buňky (tato hodnota je implicitní, pokud nebude použita žádná jiná hodnota "To*");
        /// "ToGraph" = kamkoli do grafu;
        /// "ToItem" = pouze do prvku grafu, kteréhokoli prvku;
        /// "ToItemClass12345" = pouze do prvku grafu dané třídy 12345 (prvky více tříd se specifikují jednotlivě) (číslo třídy musí navazovat na klíčové slovo);
        /// <para/>
        /// Příklad: "data/page/table1 RowRoot,ToItemClass1190;data/page/table2 RowRoot,ToItemClass1190"
        /// Tento příklad dovoluje přenést zdrojové řádky do dvou tabulek (data/page/table1 a data/page/table2), 
        /// v obou tabulkách jen do Root řádku a jen do prvku grafu třídy 1190.
        /// <para/>
        /// Veškerá klíčová slova jsou obsažena v konstantách třídy <see cref="GuiGridProperties"/>.RowDragTarget_***
        /// </summary>
        public string RowDragMoveToTarget { get; set; }
        /// <summary>
        /// Přidá jednu další definici interakce <see cref="GuiGridInteraction"/>
        /// </summary>
        /// <param name="interaction"></param>
        public void AddInteraction(GuiGridInteraction interaction)
        {
            if (this.InteractionList == null)
                this.InteractionList = new List<GuiGridInteraction>();
            if (interaction != null)
                this.InteractionList.Add(interaction);
        }
        /// <summary>
        /// Přidá další sadu definic interakcí <see cref="GuiGridInteraction"/>
        /// </summary>
        /// <param name="interactions"></param>
        public void AddInteractions(IEnumerable<GuiGridInteraction> interactions)
        {
            if (this.InteractionList == null)
                this.InteractionList = new List<GuiGridInteraction>();
            if (interactions != null)
                this.InteractionList.AddRange(interactions);
        }
        /// <summary>
        /// Soupis definic interakcí
        /// </summary>
        public List<GuiGridInteraction> InteractionList { get; set; }
        #region Konstanty pro zadavání chování
        /// <summary>
        /// Přenos položek grafu pro Child řádek, pokud se přenáší z jiné tabulky: nepřenášet nikdy
        /// </summary>
        public const string CopyClasses_None = "None";
        /// <summary>
        /// Přenos položek grafu pro Child řádek, pokud se přenáší z jiné tabulky: přenášet vždy
        /// </summary>
        public const string CopyClasses_Always = "Always";
        /// <summary>
        /// Přenos položek grafu pro Child řádek, pokud se přenáší z jiné tabulky: přenášet, jen když v Parent řádku existuje shodný prvek bez ohledu na synchronní čas
        /// </summary>
        public const string CopyClasses_ExistsPair = "ExistsPair";
        /// <summary>
        /// Přenos položek grafu pro Child řádek, pokud se přenáší z jiné tabulky: přenášet, jen když jsou synchronní časy (v Parent řádku existuje shodný prvek s časem společným s prvekm v Child řádku)
        /// </summary>
        public const string CopyClasses_SynchronPair = "SynchronPair";

        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, výběr vstupních řádků: 
        /// Přesouvat se bude pouze řádek, který chytila myš. Ostatní označené řádky se přesouvat nebudou.
        /// </summary>
        public const string RowDragSource_DragOnlyActiveRow = "DragOnlyActiveRow";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, výběr vstupních řádků:
        /// Přesouvat se budou řádky označené kliknutím plus řádek, který chytila myš. To je intuitivně nejvhodnější nastavení.
        /// </summary>
        public const string RowDragSource_DragActivePlusSelectedRows = "DragActivePlusSelectedRows";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, výběr vstupních řádků: 
        /// Přesouvat se budou pouze řádky označené kliknutím. Řádek, který chytila myš, se přesouvat nebude (tedy pokud není označen ikonkou).
        /// </summary>
        public const string RowDragSource_DragOnlySelectedRows = "DragOnlySelectedRows";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, výběr vstupních řádků: 
        /// Přesouvat se budou primárně řádky označené kliknutím (a ne aktivní). Ale pokud nejsou označeny žádné řádky, tak se přesune řádek, který chytila myš.
        /// Rozdíl od "DragActivePlusSelectedRows" je v tom, že tady se nebude přesouvat aktivní řádek (myší) pokud existují řádky označené (ikonkou).
        /// </summary>
        public const string RowDragSource_DragSelectedThenActiveRow = "DragSelectedThenActiveRow";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, výběr vstupních řádků: 
        /// přesouvat pouze řádky na pozici Root ve stromu
        /// </summary>
        public const string RowDragSource_Root = "Root";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, výběr vstupních řádků: 
        /// přesouvat pouze řádky na pozici Child ve stromu
        /// </summary>
        public const string RowDragSource_Child = "Child";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, výběr vstupních řádků: 
        /// přesouvat pouze řádky Master (rozpoznává se v <see cref="GuiId"/> řádku, kde <see cref="GuiId.EntryId"/> musí být null);
        /// </summary>
        public const string RowDragSource_Master = "Master";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, výběr vstupních řádků: 
        /// přesouvat pouze řádky Entry (rozpoznává se v <see cref="GuiId"/> řádku, kde <see cref="GuiId.EntryId"/> nesmí být null);
        /// </summary>
        public const string RowDragSource_Entry = "Entry";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, výběr vstupních řádků: 
        /// přesouvat pouze řádky dané třídy; 
        /// číslo třídy musí následovat bez mezery hned za tímto prefixem
        /// </summary>
        public const string RowDragSource_ClassPrefix = "Class";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, výběr vstupních řádků: 
        /// pouze řádky Master z dané třídy;
        /// číslo třídy musí následovat bez mezery hned za tímto prefixem
        /// </summary>
        public const string RowDragSource_MasterClassPrefix = "MasterClass";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, výběr vstupních řádků: 
        /// pouze řádky Entry z dané třídy;
        /// číslo třídy musí následovat bez mezery hned za tímto prefixem
        /// </summary>
        public const string RowDragSource_EntryClassPrefix = "EntryClass";

        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, omezení cílových prvků:
        /// pouze do řádku Root;
        /// </summary>
        public const string RowDragTarget_RowRoot = "RowRoot";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, omezení cílových prvků:
        /// pouze do řádku Child;
        /// </summary>
        public const string RowDragTarget_RowChild = "RowChild";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, omezení cílových prvků:
        /// jakýkoli řádek (tato hodnota je implicitní, pokud nebude použita žádná jiná hodnota "Row*");
        /// </summary>
        public const string RowDragTarget_RowAny = "RowAny";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, omezení cílových prvků:
        /// kamkoliv do buňky (tato hodnota je implicitní, pokud nebude použita žádná jiná hodnota "To*");
        /// </summary>
        public const string RowDragTarget_ToCell = "ToCell";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, omezení cílových prvků:
        /// kamkoli do grafu;
        /// </summary>
        public const string RowDragTarget_ToGraph = "ToGraph";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, omezení cílových prvků:
        /// pouze do prvku grafu, kteréhokoli prvku;
        /// </summary>
        public const string RowDragTarget_ToItem = "ToItem";
        /// <summary>
        /// Specifikace chování pro Drag and Move celých řádků, omezení cílových prvků:
        /// pouze do prvku grafu dané třídy 12345 (prvky více tříd se specifikují jednotlivě) (číslo třídy musí navazovat na klíčové slovo);
        /// </summary>
        public const string RowDragTarget_ToItemClassPrefix = "ToItemClass";

        #endregion
    }
    /// <summary>
    /// Předpis pro vyhledání Child řádků k danému Parent řádku
    /// </summary>
    [Flags]
    public enum GuiChildRowsEvaluateMode
    {
        /// <summary>
        /// Není zadáno Dynamické vyhodnocení.
        /// Řádek v tabulce může mít definovaného svého parenta v <see cref="GuiDataRow.ParentRowGuiId"/>, pak je jeho Childem, jiná cesta není.
        /// </summary>
        Static = 0,
        /// <summary>
        /// Brát prvky grafu pouze ve viditelném časovém rozsahu
        /// </summary>
        VisibleTimeOnly = 0x0001,

        /// <summary>
        /// Brát prvky grafu pouze tehdy, když čas parenta a čas child prvku mají společný časový průsečík 
        /// (=když parent prvek a child prvek pracují ve společný čas)
        /// </summary>
        ParentChildIntersectTimeOnly = 0x0002,

        /// <summary>
        /// Z Parenta hledat prvky grafu a z nich <see cref="GuiGraphItem.ItemId"/>, a tento <see cref="GuiId"/> vyhledávat v Child prvcích
        /// </summary>
        OnParentItem = 0x0010,
        /// <summary>
        /// Z Parenta hledat prvky grafu a z nich <see cref="GuiGraphItem.GroupId"/>, a tento <see cref="GuiId"/> vyhledávat v Child prvcích
        /// </summary>
        OnParentGroup = 0x0020,
        /// <summary>
        /// Z Parenta hledat prvky grafu a z nich <see cref="GuiGraphItem.DataId"/>, a tento <see cref="GuiId"/> vyhledávat v Child prvcích
        /// </summary>
        OnParentData = 0x0040,
        /// <summary>
        /// Z Parenta hledat prvky grafu a z nich <see cref="GuiGraphItem.RowId"/>, a tento <see cref="GuiId"/> vyhledávat v Child prvcích
        /// </summary>
        OnParentRow = 0x0080,

        /// <summary>
        /// V Child řádku vyhledat prvky grafu podle jejich <see cref="GuiGraphItem.ItemId"/>
        /// </summary>
        ToChildItem = 0x0100,
        /// <summary>
        /// V Child řádku vyhledat prvky grafu podle jejich <see cref="GuiGraphItem.GroupId"/>
        /// </summary>
        ToChildGroup = 0x0200,
        /// <summary>
        /// V Child řádku vyhledat prvky grafu podle jejich <see cref="GuiGraphItem.DataId"/>
        /// </summary>
        ToChildData = 0x0400,
        /// <summary>
        /// V Child řádku vyhledat prvky grafu podle jejich <see cref="GuiGraphItem.RowId"/>
        /// </summary>
        ToChildRow = 0x0800,

        /// <summary>
        /// Hledat Child řádky v jiné tabulce (její název je určen v property <see cref="GuiGridProperties.ChildRowsTableName"/>).
        /// Nalezený řádek z Child tabulky bude do this tabulky zkopírován (nebude do ní referencován) = vznikne new instance.
        /// Duplikování se provede i pro případný graf a jeho položky.
        /// </summary>
        InOtherTable = 0x1000,
        /// <summary>
        /// Hledat Child řádky pouze mezi Root řádky v jiné tabulce (její název je určen v property <see cref="GuiGridProperties.ChildRowsTableName"/>).
        /// K tomuto bitu může i nemusí být nastaven bit <see cref="InOtherRootRowsOnly"/>
        /// </summary>
        InOtherRootRowsOnly = 0x2000,
        /// <summary>
        /// Hledat Child řádky pouze mezi DynamicChild řádky.
        /// K tomuto bitu může i nemusí být nastaven bit <see cref="InOtherRootRowsOnly"/>
        /// </summary>
        InDynamicChildOnly = 0x4000
    }
    #endregion
    #region GuiGridInteraction : definice interakcí v rámci GUI (akce v jednom místě způsobí jinou akci jinde)
    /// <summary>
    /// GuiGridInteraction : definice interakcí v rámci GUI (akce v jednom místě způsobí jinou akci jinde)
    /// </summary>
    public sealed class GuiGridInteraction : GuiBase
    {
        /// <summary>
        /// Zdrojová akce, na kterou je tato interakce navázaná
        /// </summary>
        public SourceActionType SourceAction { get; set; }
        /// <summary>
        /// Cílová tabulka <see cref="GuiGrid"/>, kam bude akce odeslána.
        /// Pokud nebude zadáno, pak se buď tato interakce nepoužije, nebo se použije na Source tabulku, podle typu interakce.
        /// </summary>
        public string TargetGridFullName { get; set; }
        /// <summary>
        /// Akce, kterou má provést cílová tabulka
        /// </summary>
        public TargetActionType TargetAction { get; set; }
        /// <summary>
        /// Podmínky dle nastavení Toolbaru, za kterých se má tato interakce provést.
        /// V aktuální verzi se podmínky mohou vázat pouze na stav <see cref="GuiToolbarItem.IsChecked"/> prkvů toolbaru <see cref="GuiToolbarItem"/>.
        /// Na prvek toolbaru se vážou přes jeho jméno <see cref="GuiBase.Name"/>.
        /// Pokud některá ze zde vyjmenovaných položek bude zaškrtnutá, bude tato interakce použita, a naopak.
        /// </summary>
        public string Conditions { get; set; }
        /// <summary>
        /// true pokud tato interakce je podmíněná stavem Toolbarů
        /// </summary>
        public bool IsConditional { get { return !String.IsNullOrEmpty(this.Conditions); } }
    }
    /// <summary>
    /// Typ akce ve zdrojové tabulce, kterou je daná interakce spuštěna
    /// </summary>
    [Flags]
    public enum SourceActionType
    {
        /// <summary>
        /// Žádná zdrojová akce
        /// </summary>
        None = 0,
        /// <summary>
        /// Najetí myši nad řádek
        /// </summary>
        TableRowMouseOver = 0x0001,
        /// <summary>
        /// Aktivace řádku v situaci, kdy NENÍ žádný řádek označen ikonkou v záhlaví řádku (IsChecked)
        /// </summary>
        TableRowActivatedOnly = 0x0002,
        /// <summary>
        /// Aktivace řádku v situaci, kdy existují i řádky označené ikonkou v záhlaví řádku (IsChecked)
        /// </summary>
        TableRowActivatedWithRowsChecked = 0x0004,
        /// <summary>
        /// Změna v označení řádků ikonkou v záhlaví řádku (IsChecked)
        /// </summary>
        TableRowChecked = 0x0008,
        /// <summary>
        /// DoubleClick na řádek
        /// </summary>
        TableRowDoubleClicked = 0x0010,
        /// <summary>
        /// Změna na časové ose
        /// </summary>
        TimeAxisChanged = 0x1000,
        /// <summary>
        /// Click na toolbar
        /// </summary>
        ToolbarClicked = 0x2000
    }
    /// <summary>
    /// Typ akce v cílové tabulce, která je provedena
    /// </summary>
    [Flags]
    public enum TargetActionType : UInt64
    {
        /// <summary>
        /// Žádná akce v cíli
        /// </summary>
        None = 0,

        /// <summary>
        /// Hledej ze zdrojového prvku, podle jeho RowId
        /// </summary>
        SearchSourceRowId = 0x00000001,
        /// <summary>
        /// Hledej ze zdrojového prvku, podle jeho GroupId
        /// </summary>
        SearchSourceGroupId = 0x00000002,
        /// <summary>
        /// Hledej ze zdrojového prvku, podle jeho ItemId
        /// </summary>
        SearchSourceItemId = 0x00000004,
        /// <summary>
        /// Hledej ze zdrojového prvku, podle jeho DataId
        /// </summary>
        SearchSourceDataId = 0x00000008,
        /// <summary>
        /// Hledej ze zdrojového prvku pouze ve viditelné časové oblasti
        /// </summary>
        SearchSourceVisibleTime = 0x00000040,

        /// <summary>
        /// Hledej v cílovém prvku, najdi identické RowId
        /// </summary>
        SearchTargetRowId = 0x00000100,
        /// <summary>
        /// Hledej v cílovém prvku, najdi identické GroupId
        /// </summary>
        SearchTargetGroupId = 0x00000200,
        /// <summary>
        /// Hledej v cílovém prvku, najdi identické ItemId
        /// </summary>
        SearchTargetItemId = 0x00000400,
        /// <summary>
        /// Hledej v cílovém prvku, najdi identické DataId
        /// </summary>
        SearchTargetDataId = 0x00000800,

        /// <summary>
        /// Nastav na nalezené prvky IsSelected (platí jen na prvky grafu),
        /// tím se zobrazí jejich vztahy
        /// </summary>
        SelectTargetItem = 0x00010000,
        /// <summary>
        /// Nastav na nalezené prvky IsActivated (platí jen na prvky grafu),
        /// tím se více graficky zvýrazní, toto zvýraznění lze odebrat jen jinou programovou aktivitou
        /// </summary>
        ActivateTargetItem = 0x00020000,
        /// <summary>
        /// Nastav filtr na prvky v cílové tabulce
        /// </summary>
        FilterTargetItems = 0x00040000,
        /// <summary>
        /// Nastav filtr na řádky v cílové tabulce
        /// </summary>
        FilterTargetRows = 0x00080000,
        /// <summary>
        /// Pokud je specifikována tato hodnota v součinnosti s hodnotami <see cref="SelectTargetItem"/>, <see cref="ActivateTargetItem"/>, <see cref="FilterTargetRows"/>;
        /// tak před výběrem cílových prvků NEBUDE provedeno zrušení stávajících hodnot.
        /// Jinými slovy: pokud bude specifikováno jen <see cref="SelectTargetItem"/>, ale ne <see cref="LeaveCurrentTarget"/>, tak dosavadní stav Selected prvků bude ZRUŠEN.
        /// Pokud ale bude specifikováno jak <see cref="SelectTargetItem"/>, tak současně i <see cref="LeaveCurrentTarget"/>, tak dosavadní stav Selected prvků bude PONECHÁN.
        /// </summary>
        LeaveCurrentTarget = 0x00100000,

        /// <summary>
        /// Aktivuje sadu barev ve všech prvcích cílové tabulky. 
        /// Číslo sady barev je dáno parametrem interakce (v definici <see cref="GuiToolbarItem.RunInteractionNames"/>, za jménem interakce, za dvojtečkou jako první číselný parametr).
        /// Tato interakce se volá výhradně tlačítkem toolbaru.
        /// Barevné sady prvků grafu viz <see cref="GuiGraphItem.SkinCurrentIndex"/>
        /// </summary>
        ActivateGraphSkin = 0x00200000
    }
    #endregion
    #region GuiDataTable + GuiDataColumn + GuiDataRow = tabulka
    /// <summary>
    /// GuiDataTable : tabulka pro přenášení dat
    /// </summary>
    public class GuiDataTable : GuiBase, IGuiDataTable
    {
        #region Konstrukce a overrides
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiDataTable()
        {
            this.TableName = TABLE_NAME;
            this.ColumnList = new List<GuiDataColumn>();
            this.RowList = new List<GuiDataRow>();
            this.ParentChilds = new List<GuiParentChild>();
        }
        /// <summary>
        /// Výchozí název prvku <see cref="GuiDataTable"/>
        /// </summary>
        public const string TABLE_NAME = "table";
        /// <summary>
        /// Klíčové jméno, používané v aplikaci jako strojový název prvku.
        /// <see cref="Name"/> nesmí obsahovat zpětné lomítko (při pokusu o jeho použití je nahrazeno obyčejným lomítkem).
        /// Jméno nikdy není null; při vložení hodnoty null je vložena stringová konstanta "{Null}".
        /// </summary>
        public override string Name { get { return this.TableName; } set { } }
        /// <summary>
        /// Potomek zde vrací soupis svých Child prvků
        /// </summary>
        [PersistingEnabled(false)]
        protected override IEnumerable<IGuiItem> Childs { get { return Union(this.Columns, this.Rows, this.GraphLinks); } }
        #endregion
        #region Columns
        /// <summary>
        /// Sloupce.
        /// Přidávání a odebírání se provádí metodami AddColumn a RemoveColumn.
        /// </summary>
        [PersistingEnabled(false)]
        public GuiDataColumn[] Columns { get { return this.ColumnList.ToArray(); } }
        /// <summary>
        /// Počet řádků
        /// </summary>
        public int ColumnCount { get { return this.ColumnList.Count; } }
        /// <summary>
        /// Do this tabulky přidá dané sloupce.
        /// Sloupec nesmí být null, a nesmí patřit do žádné tabulky (tzn. ani do this tabulky).
        /// Sloupec lze z tabulky odebrat a teprve pak přidat do jiné tabulky.
        /// </summary>
        /// <param name="columns"></param>
        public void AddColumns(IEnumerable<GuiDataColumn> columns)
        {
            if (columns == null) return;
            foreach (GuiDataColumn column in columns)
                this.AddColumn(column);
        }
        /// <summary>
        /// Do this tabulky přidá daný sloupec.
        /// Sloupec nesmí být null, a nesmí patřit do žádné tabulky (tzn. ani do this tabulky).
        /// Sloupec lze z tabulky odebrat a teprve pak přidat do jiné tabulky.
        /// </summary>
        /// <param name="column"></param>
        public void AddColumn(GuiDataColumn column)
        {
            if (column == null) throw new ArgumentNullException("It is not allowed to add a NULL column to the table.");
            if (column.Table != null) throw new ArgumentException("It is not allowed in one table to add a column that belongs to another table.");
            ((IGuiDataTableMember)column).Table = this;
            this.ColumnList.Add(column);
        }
        /// <summary>
        /// Odebere z this tabulky daný sloupec.
        /// Ze všech řádků tabulky odebere buňku odpovídající indexu tohoto sloupce.
        /// </summary>
        /// <param name="column"></param>
        public void RemoveColumn(GuiDataColumn column)
        {
            if (column == null) throw new ArgumentNullException("It is not allowed to remove a NULL column from the table.");
            int columnIndex = this.ColumnList.FindIndex(c => Object.ReferenceEquals(c, column));
            if (columnIndex < 0) throw new ArgumentException("It is not allowed to remove a column that is not included in the table.");
            this.RemoveColumn(columnIndex);
        }
        /// <summary>
        /// Odebere z this tabulky sloupec na daném indexu.
        /// Ze všech řádků tabulky odebere buňku na daném indexu.
        /// </summary>
        /// <param name="columnIndex"></param>
        public void RemoveColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= this.ColumnCount) throw new ArgumentNullException("It is not allowed to remove a column whose index is out of range.");
            GuiDataColumn column = this.ColumnList[columnIndex];
            this.RowList.ForEach(r => r.RemoveCell(columnIndex));
            this.ColumnList.RemoveAt(columnIndex);
            ((IGuiDataTableMember)column).Table = null;
        }
        /// <summary>
        /// Z this tabulky odebere všechny sloupce.
        /// Ze všech řádků tabulky odebere všechny buňky.
        /// Řádky ponechává.
        /// </summary>
        public void ClearColumns()
        {
            this.ColumnList.ForEach(c => ((IGuiDataTableMember)c).Table = null);
            this.RowList.ForEach(r => r.ClearCells());
            this.ColumnList.Clear();
        }
        /// <summary>
        /// Vrátí index daného sloupce, nebo -1
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        int IGuiDataTable.GetColumnIndex(GuiDataColumn column)
        {
            if (column == null) return -1;
            return this.ColumnList.FindIndex(c => Object.ReferenceEquals(c, column));
        }
        /// <summary>
        /// Sloupce.
        /// Výchozí hodnota je prázdný List.
        /// </summary>
        [PropertyName("Columns")]
        protected List<GuiDataColumn> ColumnList { get; set; }
        #endregion
        #region Rows
        /// <summary>
        /// Řádky.
        /// Přidávání a odebírání se provádí metodami AddRow a RemoveRow.
        /// </summary>
        [PersistingEnabled(false)]
        public GuiDataRow[] Rows { get { return this.RowList.ToArray(); } }
        /// <summary>
        /// Počet řádků
        /// </summary>
        public int RowCount { get { return this.RowList.Count; } }
        /// <summary>
        /// Do this tabulky přidá dané řádky.
        /// Řádek nesmí být null, a nesmí patřit do žádné tabulky (tzn. ani do this tabulky).
        /// Řádek lze z tabulky odebrat a teprve pak přidat do jiné tabulky.
        /// </summary>
        /// <param name="rows"></param>
        public void AddRows(IEnumerable<GuiDataRow> rows)
        {
            if (rows == null) return;
            foreach (GuiDataRow row in rows)
                this.AddRow(row);
        }
        /// <summary>
        /// Do this tabulky přidá daný řádek.
        /// Řádek nesmí být null, a nesmí patřit do žádné tabulky (tzn. ani do this tabulky).
        /// Řádek lze z tabulky odebrat a teprve pak přidat do jiné tabulky.
        /// <para/>
        /// Pokud dodaný řádek má <see cref="GuiDataRow.RowGuiId"/> == null, a přitom v první buňce řádku je instance typu <see cref="GuiId"/>, 
        /// pak tuto ji vloží do <see cref="GuiDataRow.RowGuiId"/>.
        /// </summary>
        /// <param name="row"></param>
        public void AddRow(GuiDataRow row)
        {
            if (row == null) throw new ArgumentNullException("It is not allowed to add a NULL row to the table.");
            if (row.Table != null) throw new ArgumentException("It is not allowed in one table to add a row that belongs to another table.");
            if (row.RowGuiId == null && row.CellCount > 0)
            {
                object cell0 = row.Cells[0];
                if (cell0 != null && cell0 is GuiId)
                    row.RowGuiId = cell0 as GuiId;
            }
            ((IGuiDataTableMember)row).Table = this;
            this.RowList.Add(row);
        }
        /// <summary>
        /// Do this tabulky přidá řádek nově vytvořený pro dané hodnoty.
        /// </summary>
        /// <param name="values"></param>
        public GuiDataRow AddRow(params object[] values)
        {
            GuiDataRow row = new GuiDataRow(values);
            this.AddRow(row);
            return row;
        }
        /// <summary>
        /// Odebere z this tabulky daný řádek.
        /// </summary>
        /// <param name="row"></param>
        public void RemoveColumn(GuiDataRow row)
        {
            if (row == null) throw new ArgumentNullException("It is not allowed to remove a NULL row from the table.");
            int rowIndex = this.RowList.FindIndex(r => Object.ReferenceEquals(r, row));
            if (rowIndex < 0) throw new ArgumentException("It is not allowed to remove a row that is not included in the table.");
            this.RemoveRow(rowIndex);
        }
        /// <summary>
        /// Odebere z this tabulky řádek na daném indexu.
        /// </summary>
        /// <param name="rowIndex"></param>
        public void RemoveRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= this.RowCount) throw new ArgumentException("It is not allowed to remove a row whose index is out of range.");
            GuiDataRow row = this.RowList[rowIndex];
            this.RowList.RemoveAt(rowIndex);
            ((IGuiDataTableMember)row).Table = null;
        }
        /// <summary>
        /// Z this tabulky odebere všechny řádky.
        /// Sloupce ponechává.
        /// </summary>
        public void ClearRows()
        {
            this.RowList.ForEach(c => ((IGuiDataTableMember)c).Table = null);
            this.RowList.Clear();
        }
        /// <summary>
        /// Vrátí index daného řádku, nebo -1
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        int IGuiDataTable.GetRowIndex(GuiDataRow row)
        {
            if (row == null) return -1;
            return this.RowList.FindIndex(r => Object.ReferenceEquals(r, row));
        }
        /// <summary>
        /// Řádky.
        /// Výchozí hodnota je prázdný List.
        /// </summary>
        [PropertyName("Rows")]
        public List<GuiDataRow> RowList { get; set; }
        #endregion
        #region Public properties
        /// <summary>
        /// Název tabulky
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// Číslo třídy, z níž pochází data šablony
        /// </summary>
        public int? ClassId { get; set; }
        /// <summary>
        /// Číslo šablony
        /// </summary>
        public int? TemplateId { get; set; }
        /// <summary>
        /// Jednotlivé prvky typu <see cref="GuiTagItem"/>.
        /// Výchozí hodnota je null.
        /// Tagy mohou být zadány buď zde = hromadně pro celoou tabulku, anebo mohou být zadány přímo v konkrétním řádku v <see cref="GuiDataRow.TagItems"/>.
        /// Zadání lze libovolně kombinovat. Při konfliktu vyhrává prvek zadaný do řádku.
        /// </summary>
        public List<GuiTagItem> TagItems { get; set; }
        /// <summary>
        /// Tabulka definující vztah Parent - Child mezi dvěma řádky tabulky <see cref="Rows"/>.
        /// Výchozí hodnota je prázdný List.
        /// </summary>
        public List<GuiParentChild> ParentChilds { get; set; }
        /// <summary>
        /// Linky pro prvky grafů.
        /// Výchozí hodnota je null.
        /// </summary>
        public List<GuiGraphLink> GraphLinks { get; set; }
        /// <summary>
        /// Je povoleno označovat řádky (ikonka v záhlaví řádku)?
        /// </summary>
        public bool RowCheckEnabled { get; set; }
        /// <summary>
        /// Posun jednotlivých úrovní v TreeView, v pixelech. Nezadáno = 14 pixelů. Minimální hodnota = 10px. Maximální hodnota = 40px.
        /// </summary>
        public int? TreeViewNodeOffset { get; set; }
        /// <summary>
        /// Styl kreslení linky mezi Root nodem a jeho Child nody. Default = Dot.
        /// </summary>
        public GuiTreeViewLinkMode? TreeViewLinkMode { get; set; }
        /// <summary>
        /// Barva linky mezi Root nodem a jeho Child nody. Může obsahovat Alpha kanál.
        /// </summary>
        public Color? TreeViewLinkColor { get; set; }
        /// <summary>
        /// Vizuální styl základní
        /// </summary>
        public GuiVisualStyle DefaultVisualStyle { get; set; }
        /// <summary>
        /// Vizuální styl základní pro Child řádky
        /// </summary>
        public GuiVisualStyle DefaultChildVisualStyle { get; set; }
        /// <summary>
        /// Knihovna explicitních vizuálních stylů, použitých v buňkách / řádcích / sloupcích...
        /// Každý prvek tabulky má property StyleName, která se odkazuje na jméno <see cref="GuiVisualStyle"/>.Name do tohoto seznamu.
        /// Prvek tabulky sám nemá svůj objekt <see cref="GuiVisualStyle"/>, fyzické definice stylů jsou v tomto seznamu <see cref="VisualStyles"/>.
        /// </summary>
        public List<GuiVisualStyle> VisualStyles { get; set; }
        /// <summary>
        /// Obrázek použitý při označení řádku této tabulky
        /// </summary>
        public GuiImage RowCheckedImage { get; set; }
        /// <summary>
        /// Obrázek použitý pro neoznačený řádek této tabulky, pokud tabulka povoluje označování řádků myší (<see cref="RowCheckEnabled"/> je true)
        /// </summary>
        public GuiImage RowNonCheckedImage { get; set; }
        #endregion
        #region Vytvoření instance GuiDataTable z System.Data.DataTable
        /// <summary>
        /// Metoda vytvoří a vrátí instanci <see cref="GuiDataTable"/> z dodané tabulky <see cref="DataTable"/>
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        public static GuiDataTable CreateFromTable(DataTable dataTable)
        {
            if (dataTable == null) return null;

            GuiDataTable guiTable = new GuiDataTable();

            guiTable.TableName = dataTable.TableName;
            guiTable.AddColumns(GuiDataColumn.CreateFromTable(dataTable));
            GuiTableKeyColumns keyColumns = GuiTableKeyColumns.SearchInTable(guiTable);
            guiTable.AddRows(GuiDataRow.CreateFromTable(dataTable, keyColumns));

            return guiTable;
        }
        #endregion
    }
    /// <summary>
    /// GuiDataColumn : definice jednoho sloupce tabulky <see cref="GuiDataTable"/>
    /// </summary>
    public class GuiDataColumn : GuiTextItem, IGuiDataTableMember
    {
        #region Konstruktor a overrides
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiDataColumn()
        {
            this.AllowRowFilter = true;
            this.AllowSort = true;
            this.BrowseColumnType = WorkScheduler.BrowseColumnType.DataColumn;
            this.IsVisible = true;
            this.Width = 100;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "[" + (this.Name == null ? "Null" : this.Name) + "]" + (this.Title == null ? "" : " = \"" + this.Title + "\"");
        }
        #endregion
        #region Table
        /// <summary>
        /// Reference na vlastníka = tabulka.
        /// Je nastaveno při přidání do tabulky, je nulováno při odebrání.
        /// </summary>
        [PersistingEnabled(false)]
        public GuiDataTable Table { get { return this._Table; } }
        /// <summary>
        /// Member of IGuiDataTableMember
        /// </summary>
        [PersistingEnabled(false)]
        GuiDataTable IGuiDataTableMember.Table { get { return this._Table; } set { this._Table = value; } }
        /// <summary>
        /// Fyzické úložiště reference na <see cref="GuiDataTable"/>
        /// </summary>
        private GuiDataTable _Table;
        /// <summary>
        /// Reference na <see cref="Table"/> typovaná na interface <see cref="IGuiDataTable"/>, pro přístup k vnitřním metodám.
        /// </summary>
        [PersistingEnabled(false)]
        protected IGuiDataTable ITable { get { return (this._Table as IGuiDataTable); } }
        /// <summary>
        /// true pokud máme referenci na <see cref="Table"/>
        /// </summary>
        [PersistingEnabled(false)]
        protected bool HasTable { get { return (this._Table != null); } }
        #endregion
        #region Public properties
        /// <summary>
        /// Vrátí index sloupce v seznamu sloupců své tabulky. Pokud sloupec do žádné tabulky nepatří, vrátí -1.
        /// </summary>
        public int Index { get { return (this.HasTable ? this.ITable.GetColumnIndex(this) : -1); } }
        /// <summary>
        /// Typ sloupce v přehledu: pomocný, datový, ... Zobrazují se vždy jen sloupce typu DataColumn, ostatní sloupce jsou pomocné.
        /// Aktuálně hodnoty: RecordId, SubjectNumber, ObjectNumber, DataColumn, RelationHelpfulColumn, TotalCountHelpfulColumn
        /// </summary>
        public BrowseColumnType BrowseColumnType { get; set; }
        /// <summary>
        /// Formát sloupce v přehledu
        /// </summary>
        public string Format { get; set; }
        /// <summary>
        /// Zarovnání obsahu sloupce, uplatňuje se podle jeho typu
        /// </summary>
        public ContentAlignment? Alignment { get; set; }
        /// <summary>
        /// Informace o viditelnosti sloupce (zda má být vidět v přehledu)
        /// </summary>
        public bool IsVisible { get; set; }
        /// <summary>
        /// Šířka sloupce v přehledu
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// Hodnota <see cref="System.Data.DataColumn.DefaultValue"/>
        /// </summary>
        public object ColumnDefaultValue { get; set; }
        /// <summary>
        /// Hodnota <see cref="System.Data.DataColumn.ReadOnly"/>
        /// </summary>
        public bool ColumnReadOnly { get; set; }
        /// <summary>
        /// Povolit řádkové filtrování
        /// </summary>
        public bool AllowRowFilter { get; set; }
        /// <summary>
        /// Povolit třídění
        /// </summary>
        public bool AllowSort { get; set; }
        /// <summary>
        /// Pořadí sloupce v přehledu - pořadí zobrazení
        /// </summary>
        public int? SortIndex { get; set; }
        /// <summary>
        /// true pokud tento sloupec má být k dispozici uživateli (jeho viditelnost se pak řídí pomocí <see cref="IsVisible"/>),
        /// false pro sloupce "systémové", které se nikdy nezobrazují.
        /// </summary>
        public bool ColumnIsForUser { get { return (this.BrowseColumnType == BrowseColumnType.DataColumn); } }

        /// <summary>
        /// V této property je uloženo číslo třídy celé tabulky.
        /// Je to z historických důvodů, kdy Green generuje přehledovou šablonu do DataTable, a některá data dává do prvního sloupce.
        /// Po sestavení celé tabulky je toto číslo přeneseno do <see cref="GuiDataTable.ClassId"/>, a zde je vloženo NULL.
        /// </summary>
        public int? TableClassId { get; set; }
        /// <summary>
        /// V této property je uloženo číslo šablony celé tabulky.
        /// Je to z historických důvodů, kdy Green generuje přehledovou šablonu do DataTable, a některá data dává do prvního sloupce.
        /// Po sestavení celé tabulky je toto číslo přeneseno do <see cref="GuiDataTable.TemplateId"/>, a zde je vloženo NULL.
        /// </summary>
        public int? TableTemplateId { get; set; }
        /// <summary>
        /// Pokud this sloupec je textový, a obsahuje data ze záznamu ve vztahu, 
        /// pak tato property obsahuje název sloupce, který nese číslo vztaženého záznamu 
        /// (v odkázaném sloupci najdeme cislo_subjektu, a v jeho hlavičce pak RelationClassId)
        /// </summary>
        public string RelationRecordIdColumnName { get; set; }
        /// <summary>
        /// Pokud this sloupec obsahuje číslo záznamu v nějakém vztahu, pak zde je společné číslo třídy tohoto záznamu ve vztahu.
        /// POkud je vztah netypový, pak je zde asi číslo Obecného subjektu.
        /// </summary>
        public int? RelationClassId { get; set; }

        /// <summary>
        /// Explicitně definovaný styl pro tento sloupec. Pokud bude zadán, použije se tento a nebude se hledat styl dle jména <see cref="StyleName"/>.
        /// </summary>
        public GuiVisualStyle Style { get; set; }        /// <summary>
                                                         /// Název stylu pro tento sloupec. Odkazuje se na <see cref="GuiDataTable.VisualStyles"/>, na jméno prvku <see cref="GuiVisualStyle"/>.Name
                                                         /// </summary>
        public string StyleName { get; set; }
        #endregion
        #region Vytvoření instance GuiDataColumn z System.Data.DataColumn
        /// <summary>
        /// Vrátí seznam <see cref="GuiDataColumn"/> pro sloupce z dodané tabulky
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        public static List<GuiDataColumn> CreateFromTable(System.Data.DataTable dataTable)
        {
            if (dataTable == null) return null;
            return CreateFromColumns(dataTable.Columns);
        }
        /// <summary>
        /// Vrátí seznam <see cref="GuiDataColumn"/> pro sloupce z dodané kolekce
        /// </summary>
        /// <param name="dataColumns"></param>
        /// <returns></returns>
        public static List<GuiDataColumn> CreateFromColumns(System.Data.DataColumnCollection dataColumns)
        {
            if (dataColumns == null) return null;
            List<GuiDataColumn> guiColumns = new List<GuiDataColumn>();
            foreach (System.Data.DataColumn dataColumn in dataColumns)
                guiColumns.Add(CreateFromColumn(dataColumn));
            return guiColumns;
        }
        /// <summary>
        /// Vrátí instanci <see cref="GuiDataColumn"/> pro daný sloupec
        /// </summary>
        /// <param name="dataColumn"></param>
        /// <returns></returns>
        public static GuiDataColumn CreateFromColumn(System.Data.DataColumn dataColumn)
        {
            if (dataColumn == null) return null;

            GuiDataColumn guiColumn = new GuiDataColumn();

            guiColumn.Name = dataColumn.ColumnName;
            guiColumn.Title = GetPropertyValue(dataColumn, "Label", dataColumn.Caption);
            guiColumn.BrowseColumnType = GetPropertyValue(dataColumn, "BrowseColumnType", BrowseColumnType.None);
            guiColumn.Format = GetPropertyValue(dataColumn, "Format", "");
            guiColumn.Alignment = ContentAlignment.MiddleLeft;                           // Není odkud brát?
            guiColumn.IsVisible = GetPropertyValue(dataColumn, "IsVisible", true);
            guiColumn.Width = GetPropertyValue(dataColumn, "Width", 0);
            guiColumn.ColumnDefaultValue = dataColumn.DefaultValue;
            guiColumn.ColumnReadOnly = dataColumn.ReadOnly;
            guiColumn.AllowRowFilter = GetPropertyValue(dataColumn, "AllowRowFilter", true);
            guiColumn.AllowSort = GetPropertyValue(dataColumn, "AllowSort", true);
            guiColumn.SortIndex = GetPropertyValue(dataColumn, "SortIndex", (int?)null);

            // Převzetí dočasných údajů (jejich finálním cílem je Table, ale to až při zpracování z GuiDataTable do Table)
            guiColumn.TableClassId = GetPropertyValue(dataColumn, "ClassNumber", (int?)null);
            guiColumn.TableTemplateId = GetPropertyValue(dataColumn, "TemplateNumber", (int?)null);

            // Zpracování údajů o vztahu:
            int? relationNumber = GetPropertyValue(dataColumn, "RelationNumber", (int?)null);
            string relationTableAlias = GetPropertyValue(dataColumn, "RelationTableAlias", "");
            if (relationNumber.HasValue && relationNumber.Value != 0 && !String.IsNullOrEmpty(relationTableAlias))
                guiColumn.RelationRecordIdColumnName = "H_RN_" + relationNumber.Value.ToString() + "_" + relationTableAlias + "_RN_H";

            int? relationClassNumber = GetPropertyValue(dataColumn, "RelationClassNumber", (int?)null);
            if (relationClassNumber.HasValue && relationClassNumber.Value != 0)
                guiColumn.RelationClassId = relationClassNumber;


            // Původní konverze načítala všechny properties:
            // ---------------------------------------------
            //guiColumn.ColumnName = dataColumn.ColumnName;
            //guiColumn.ColumnCaption = dataColumn.Caption;
            //guiColumn.ColumnDefaultValue = dataColumn.DefaultValue;
            //guiColumn.ColumnReadOnly = dataColumn.ReadOnly;
            //guiColumn.ClassNumber = GetPropertyValue(dataColumn, "ClassNumber", (int?)null);
            //guiColumn.TemplateNumber = GetPropertyValue(dataColumn, "TemplateNumber", (int?)null);
            //guiColumn.Alias = GetPropertyValue(dataColumn, "Alias", "");
            //guiColumn.AllowRowFilter = GetPropertyValue(dataColumn, "AllowRowFilter", true);
            //guiColumn.AllowSort = GetPropertyValue(dataColumn, "AllowSort", true);
            //guiColumn.BrowseColumnType = GetPropertyValue(dataColumn, "BrowseColumnType", BrowseColumnType.None);
            //guiColumn.CodeName_FromSelect = GetPropertyValue(dataColumn, "CodeName_FromSelect", "");
            //guiColumn.CodeName_FromTemplate = GetPropertyValue(dataColumn, "CodeName_FromTemplate", "");
            //guiColumn.ColRelNum = GetPropertyValue(dataColumn, "ColRelNum", 0);
            //guiColumn.ColType = GetPropertyValue(dataColumn, "ColType", "");
            //guiColumn.DataTypeRepo = GetPropertyValue(dataColumn, "DataTypeRepo", "");
            //guiColumn.DataTypeSystem = GetPropertyValue(dataColumn, "DataTypeSystem", "");
            //guiColumn.Format = GetPropertyValue(dataColumn, "Format", "");
            //guiColumn.IsVisible = GetPropertyValue(dataColumn, "IsVisible", true);
            //guiColumn.Label = GetPropertyValue(dataColumn, "Label", "");
            //guiColumn.SortIndex = GetPropertyValue(dataColumn, "SortIndex", (int?)null);
            //guiColumn.Width = GetPropertyValue(dataColumn, "Width", 0);
            //guiColumn.RelationClassNumber = GetPropertyValue(dataColumn, "RelationClassNumber", (int?)null);
            //guiColumn.RelationNumber = GetPropertyValue(dataColumn, "RelationNumber", (int?)null);
            //guiColumn.RelationSide = GetPropertyValue(dataColumn, "RelationSide", "");
            //guiColumn.RelationVolumeType = GetPropertyValue(dataColumn, "RelationVolumeType", "");
            //guiColumn.RelationTableAlias = GetPropertyValue(dataColumn, "RelationTableAlias", "");

            return guiColumn;
        }
        /// <summary>
        /// Vrátí hodnotu požadovaného typu z dané property
        /// </summary>
        /// <param name="dataColumn">Data column</param>
        /// <param name="propertyName">Název extended property</param>
        /// <param name="defaultValue">Default hodnota</param>
        /// <returns></returns>
        protected static int GetPropertyValue(DataColumn dataColumn, string propertyName, int defaultValue)
        {
            object value;
            if (!TryGetPropertyValue(dataColumn, propertyName, out value)) return defaultValue;
            if (value is Int32) return (Int32)value;
            if (value is Int16) return (Int32)value;
            int number;
            if (value is string && Int32.TryParse((string)value, out number)) return number;
            return defaultValue;
        }
        /// <summary>
        /// Vrátí hodnotu požadovaného typu z dané property
        /// </summary>
        /// <param name="dataColumn">Data column</param>
        /// <param name="propertyName">Název extended property</param>
        /// <param name="defaultValue">Default hodnota</param>
        /// <returns></returns>
        protected static int? GetPropertyValue(DataColumn dataColumn, string propertyName, int? defaultValue)
        {
            object value;
            if (!TryGetPropertyValue(dataColumn, propertyName, out value)) return defaultValue;
            if (value is Int32) return (Int32)value;
            if (value is Int16) return (Int32)value;
            int number;
            if (value is string && Int32.TryParse((string)value, out number)) return number;
            return defaultValue;
        }
        /// <summary>
        /// Vrátí hodnotu požadovaného typu z dané property
        /// </summary>
        /// <param name="dataColumn">Data column</param>
        /// <param name="propertyName">Název extended property</param>
        /// <param name="defaultValue">Default hodnota</param>
        /// <returns></returns>
        protected static bool GetPropertyValue(DataColumn dataColumn, string propertyName, bool defaultValue)
        {
            object value;
            if (!TryGetPropertyValue(dataColumn, propertyName, out value)) return defaultValue;
            if (value == null) return defaultValue;
            if (value is Boolean) return (Boolean)value;
            if (!(value is String)) return defaultValue;
            string text = ((string)value).Trim();
            return String.Equals(text, "true", StringComparison.InvariantCultureIgnoreCase);
        }
        /// <summary>
        /// Vrátí hodnotu požadovaného typu z dané property
        /// </summary>
        /// <param name="dataColumn">Data column</param>
        /// <param name="propertyName">Název extended property</param>
        /// <param name="defaultValue">Default hodnota</param>
        /// <returns></returns>
        protected static string GetPropertyValue(DataColumn dataColumn, string propertyName, string defaultValue)
        {
            object value;
            if (!TryGetPropertyValue(dataColumn, propertyName, out value)) return defaultValue;
            if (value == null) return defaultValue;
            if (value is String) return (String)value;
            return value.ToString();
        }
        /// <summary>
        /// Vrátí hodnotu požadovaného typu z dané property
        /// </summary>
        /// <param name="dataColumn">Data column</param>
        /// <param name="propertyName">Název extended property</param>
        /// <param name="defaultValue">Default hodnota</param>
        /// <returns></returns>
        protected static BrowseColumnType GetPropertyValue(DataColumn dataColumn, string propertyName, BrowseColumnType defaultValue)
        {
            string text = GetPropertyValue(dataColumn, propertyName, "");
            return GetPropertyValue(text, defaultValue);
        }
        /// <summary>
        /// Vrátí hodnotu požadovaného typu z daného textu
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="defaultValue">Default hodnota</param>
        /// <returns></returns>
        protected static BrowseColumnType GetPropertyValue(string text, BrowseColumnType defaultValue)
        {
            if (!String.IsNullOrEmpty(text))
            {
                switch (text)
                {
                    case "RecordId": return BrowseColumnType.RecordId;
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
        /// Pokusí se najít a vrátit hodnotu z dané property
        /// </summary>
        /// <param name="dataColumn">Data column</param>
        /// <param name="propertyName">Název extended property</param>
        /// <param name="value">Výstup hodnoty</param>
        /// <returns></returns>
        protected static bool TryGetPropertyValue(DataColumn dataColumn, string propertyName, out object value)
        {
            value = null;
            if (dataColumn == null || dataColumn.ExtendedProperties.Count == 0 || !dataColumn.ExtendedProperties.ContainsKey(propertyName)) return false;
            value = dataColumn.ExtendedProperties[propertyName];
            return true;
        }
        #endregion
    }
    /// <summary>
    /// GuiDataRow : obsah jednoho řádku v tabulce <see cref="GuiDataTable"/>
    /// </summary>
    public class GuiDataRow : GuiBase, IGuiDataTableMember
    {
        #region Konstruktor a overrides
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiDataRow()
        {
            this.Cells = new List<object>();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiDataRow(params object[] values)
            : this()
        {
            this.Cells.AddRange(values);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "";
            if (this.Cells == null)
            {
                text = "Null";
            }
            else
            {
                foreach (object cell in this.Cells)
                    text = (text.Length == 0 ? "| " : text + " ") + (cell == null ? "Null" : cell.ToString()) + " |";
            }
            return text;
        }
        /// <summary>
        /// Klíčové jméno, používané v aplikaci jako strojový název prvku.
        /// <see cref="Name"/> nesmí obsahovat zpětné lomítko (při pokusu o jeho použití je nahrazeno obyčejným lomítkem).
        /// Jméno nikdy není null; při vložení hodnoty null je vložena stringová konstanta "{Null}".
        /// </summary>
        public override string Name { get { return "Row"; } set { } }
        /// <summary>
        /// Potomek zde vrací soupis svých Child prvků
        /// </summary>
        [PersistingEnabled(false)]
        protected override IEnumerable<IGuiItem> Childs { get { return Union(this.Cells, this.TagItems, this.Graph); } }
        #endregion
        #region Table
        /// <summary>
        /// Reference na vlastníka = tabulka.
        /// Je nastaveno při přidání do tabulky, je nulováno při odebrání.
        /// </summary>
        [PersistingEnabled(false)]
        public GuiDataTable Table { get { return this._Table; } }
        /// <summary>
        /// Member of IGuiDataTableMember
        /// </summary>
        [PersistingEnabled(false)]
        GuiDataTable IGuiDataTableMember.Table { get { return this._Table; } set { this._Table = value; } }
        /// <summary>
        /// Fyzické úložiště reference na <see cref="GuiDataTable"/>
        /// </summary>
        private GuiDataTable _Table;
        /// <summary>
        /// Reference na <see cref="Table"/> typovaná na interface <see cref="IGuiDataTable"/>, pro přístup k vnitřním metodám.
        /// </summary>
        [PersistingEnabled(false)]
        protected IGuiDataTable ITable { get { return (this._Table as IGuiDataTable); } }
        /// <summary>
        /// true pokud máme referenci na <see cref="Table"/>
        /// </summary>
        [PersistingEnabled(false)]
        protected bool HasTable { get { return (this._Table != null); } }
        #endregion
        #region Cells
        /// <summary>
        /// Typové čtení dat z daného indexu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public T Get<T>(int columnIndex)
        {
            object value = this[columnIndex];
            if (value == null) return default(T);
            if (value is T) return (T)value;          // Rychlá cesta pro exaktní shodu typu


            return default(T);
        }
        /// <summary>
        /// Vrátí true, pokud buňka na daném indexu obsahuje něco jiného než null.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public bool HasValue(int columnIndex)
        {
            object value = this[columnIndex];
            return (value != null);
        }
        /// <summary>
        /// Hodnota v buňce na daném indexu.
        /// Index musí být nezáporný.
        /// Při čtení z neexistujícího indexu je vracena hodnota null.
        /// Při zápisu na neexistující index pro hodnotu jinou než null je pole buněk rozšířeno tak, aby daný index existoval.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public object this[int columnIndex]
        {
            get
            {
                if (columnIndex < 0) throw new ArgumentException("It is not allowed to read a cell whose index is negative.");
                return ((columnIndex < this.Cells.Count) ? this.Cells[columnIndex] : null);
            }
            set
            {
                if (columnIndex < 0) throw new ArgumentException("It is not allowed to write a cell whose index is negative.");
                if (this.HasTable)
                {
                    if (columnIndex >= this.Table.ColumnCount) throw new ArgumentException("It is not allowed to write a cell whose index is above TableColumnCount.");
                }
                else
                {
                    if (columnIndex > 100) throw new ArgumentException("It is not allowed to write a cell (in isolated row) whose index is above 100.");
                }

                // Pokud je zapisovaná hodnota = null, a v poli Cells dosud není daný columnIndex přítomen, skončím 
                //  (protože není potřeba rozšiřovat pole Cells jen proto, abych do nové buňky vložil null):
                if (value == null && columnIndex >= this.Cells.Count) return;

                // Je zapotřebí provést zápis; a pokud pro daný index ještě nemám buňku, tak ji vytvořím:
                while (this.Cells.Count <= columnIndex)
                    this.Cells.Add(null);

                this.Cells[columnIndex] = value;
            }
        }
        /// <summary>
        /// Jednotlivé buňky v řádku.
        /// Výchozí hodnota je prázdný List.
        /// </summary>
        public List<object> Cells { get; set; }
        /// <summary>
        /// Počet buněk v řádku.
        /// </summary>
        public int CellCount { get { return this.Cells.Count; } }
        #endregion
        #region Public properties
        /// <summary>
        /// Vrátí index řádku v seznamu řádků this tabulky. Pokud řádek do žádné tabulky nepatří, vrátí -1.
        /// </summary>
        public int Index { get { return (this.HasTable ? this.ITable.GetRowIndex(this) : -1); } }
        /// <summary>
        /// Klíč this řádku
        /// </summary>
        public GuiId RowGuiId { get; set; }
        /// <summary>
        /// Konkrétní klíč mého Parent řádku, pokud this řádek patří jen pod jednoho parenta.
        /// Pokud this řádek patří pod více parentů, pak je zde uložen prázdný GuiId = <see cref="GuiId.Empty"/>, 
        /// tím se řádek stane "holkou pro všechny": nebude se zobrazovat jako Root, a jako svůj Child řádek si jej vyhledá 
        /// některý Parent řádek, podle definice v <see cref="GuiGridProperties.ChildRowsEvaluate"/>.
        /// </summary>
        public GuiId ParentRowGuiId { get; set; }
        /// <summary>
        /// Jednotlivé prvky typu <see cref="GuiTagItem"/>.
        /// Výchozí hodnota je null.
        /// Tagy mohou být zadány buď zde = přímo v konkrétním řádku, anebo mohou být zadány hromadně v <see cref="GuiDataTable.TagItems"/>.
        /// Zadání lze libovolně kombinovat. Při konfliktu vyhrává prvek zadaný do řádku.
        /// </summary>
        public List<GuiTagItem> TagItems { get; set; }
        /// <summary>
        /// Graf pro tento řádek (bude použit jako Background graf).
        /// Pokud má být graf umístěn ve sloupci, má být vepsán do některé buňky <see cref="Cells"/>.
        /// </summary>
        public GuiGraph Graph { get; set; }
        /// <summary>
        /// Explicitně definovaný styl pro tento řádek. Pokud bude zadán, použije se tento a nebude se hledat styl dle jména <see cref="StyleName"/>.
        /// </summary>
        public GuiVisualStyle Style { get; set; }
        /// <summary>
        /// Název stylu pro tento řádek. Odkazuje se na <see cref="GuiDataTable.VisualStyles"/>, na jméno prvku <see cref="GuiVisualStyle"/>.Name
        /// </summary>
        public string StyleName { get; set; }
        /// <summary>
        /// Obrázek použitý při označení tohoto řádku
        /// </summary>
        public GuiImage RowCheckedImage { get; set; }
        /// <summary>
        /// Obrázek použitý pro neoznačený řádek této tabulky, pokud tabulka povoluje označování řádků myší (<see cref="GuiDataTable.RowCheckEnabled"/> je true)
        /// </summary>
        public Image RowNonCheckedImage { get; set; }
        /// <summary>
        /// Ikonka v tomto řádku (v záhlaví)
        /// </summary>
        public GuiImage Icon { get; set; }
        #endregion
        #region Servis
        /// <summary>
        /// Odebere buňku na daném indexu.
        /// Pokud je index mimo rozsah, tak neodebere nic a chybu nevyvolá.
        /// </summary>
        /// <param name="columnIndex"></param>
        internal void RemoveCell(int columnIndex)
        {
            if (columnIndex >= 0 && columnIndex < this.Cells.Count)
                this.Cells.RemoveAt(columnIndex);
        }
        /// <summary>
        /// Odebere všechny buňky
        /// </summary>
        internal void ClearCells()
        {
            this.Cells.Clear();
        }
        #endregion
        #region Vytvoření instance GuiDataTable z System.Data.DataTable
        /// <summary>
        /// Vrátí seznam řádků <see cref="GuiDataRow"/> pro danou tabulku
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="keyColumns">Klíčové sloupce tabulky</param>
        /// <returns></returns>
        public static List<GuiDataRow> CreateFromTable(DataTable dataTable, GuiTableKeyColumns keyColumns = null)
        {
            if (dataTable == null) return null;
            if (keyColumns == null) keyColumns = GuiTableKeyColumns.SearchInTable(dataTable);
            return CreateFromTable(dataTable.Rows, keyColumns);
        }
        /// <summary>
        /// Vrátí seznam řádků <see cref="GuiDataRow"/> pro dané řádky
        /// </summary>
        /// <param name="dataRows"></param>
        /// <param name="keyColumns">Klíčové sloupce tabulky</param>
        /// <returns></returns>
        public static List<GuiDataRow> CreateFromTable(DataRowCollection dataRows, GuiTableKeyColumns keyColumns = null)
        {
            if (dataRows == null) return null;
            if (keyColumns == null && dataRows.Count > 0) keyColumns = GuiTableKeyColumns.SearchInTable(dataRows[0].Table);
            List<GuiDataRow> guiRows = new List<GuiDataRow>();
            foreach (DataRow dataRow in dataRows)
                guiRows.Add(CreateFromRow(dataRow, keyColumns));
            return guiRows;
        }
        /// <summary>
        /// Vrátí instanci <see cref="GuiDataRow"/> pro daný řádek
        /// </summary>
        /// <param name="dataRow"></param>
        /// <param name="keyColumns">Klíčové sloupce tabulky</param>
        /// <returns></returns>
        public static GuiDataRow CreateFromRow(DataRow dataRow, GuiTableKeyColumns keyColumns = null)
        {
            if (dataRow == null) return null;
            if (keyColumns == null) keyColumns = GuiTableKeyColumns.SearchInTable(dataRow.Table);
            GuiDataRow guiRow = new GuiDataRow();
            guiRow.Cells = new List<object>(dataRow.ItemArray);
            guiRow.RowGuiId = keyColumns.CreateRowGuiId(guiRow); // ((keyColumn != null && guiRow.Cells.Count > 0) ? keyColumn.CreateRowGuiId(guiRow) : null);
            return guiRow;
        }
        #endregion
    }
    /// <summary>
    /// Třída určená pro nalezení RecordID v tabulce <see cref="GuiDataTable"/>
    /// </summary>
    public class GuiTableKeyColumns
    {
        #region Konstrukce
        /// <summary>
        /// Vrací instanci <see cref="GuiTableKeyColumns"/> vytvořenou ze sloupců v <see cref="DataTable"/>
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        public static GuiTableKeyColumns SearchInTable(DataTable dataTable)
        {
            List<GuiDataColumn> guiColumns = (dataTable != null ? GuiDataColumn.CreateFromTable(dataTable) : null);
            return new GuiTableKeyColumns(guiColumns);
        }
        /// <summary>
        /// Vrací instanci <see cref="GuiTableKeyColumns"/> vytvořenou ze sloupců v <see cref="GuiDataTable"/>
        /// </summary>
        /// <param name="guiTable"></param>
        /// <returns></returns>
        public static GuiTableKeyColumns SearchInTable(GuiDataTable guiTable)
        {
            GuiDataColumn[] guiColumns = (guiTable != null ? guiTable.Columns : null);
            return new GuiTableKeyColumns(guiColumns);
        }
        /// <summary>
        /// Konstruktor pro danou sadu sloupců
        /// </summary>
        /// <param name="guiColumns"></param>
        private GuiTableKeyColumns(IEnumerable<GuiDataColumn> guiColumns)
        {
            this.RecordColumn = null;
            this.ObjectColumn = null;
            if (guiColumns != null)
            {
                this.RecordColumn = guiColumns.FirstOrDefault(c => c.BrowseColumnType == BrowseColumnType.RecordId);
                this.RecordColumnId = (this.RecordColumn != null ? this.RecordColumn.Index : -1);
                this.SubjectColumn = guiColumns.FirstOrDefault(c => c.BrowseColumnType == BrowseColumnType.SubjectNumber);
                this.SubjectColumnId = (this.SubjectColumn != null ? this.SubjectColumn.Index : -1);
                this.ObjectColumn = guiColumns.FirstOrDefault(c => c.BrowseColumnType == BrowseColumnType.ObjectNumber);
                this.ObjectColumnId = (this.ObjectColumn != null ? this.ObjectColumn.Index : -1);
            }
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "";
            if (this.HasRecordColumn) text += (text.Length > 0 ? "; " : "") + "RecordColumn: " + this.RecordColumn.Name;
            if (this.HasSubjectColumn) text += (text.Length > 0 ? "; " : "") + "SubjectColumn: " + this.SubjectColumn.Name;
            if (this.HasObjectColumn) text += (text.Length > 0 ? "; " : "") + "ObjectColumn: " + this.ObjectColumn.Name;
            if (text.Length == 0) text = "Empty";
            return text;
        }
        #endregion
        #region Klíčové sloupce
        /// <summary>
        /// Sloupec, který nese číslo záznamu = první sloupec tabulky, jehož <see cref="GuiDataColumn.BrowseColumnType"/> == <see cref="BrowseColumnType.RecordId"/>
        /// </summary>
        public GuiDataColumn RecordColumn { get; private set; }
        /// <summary>
        /// Sloupec, který nese číslo záznamu = první sloupec tabulky, jehož <see cref="GuiDataColumn.BrowseColumnType"/> == <see cref="BrowseColumnType.SubjectNumber"/>
        /// </summary>
        public GuiDataColumn SubjectColumn { get; private set; }
        /// <summary>
        /// Sloupec, který nese číslo položky = první sloupec tabulky, jehož <see cref="GuiDataColumn.BrowseColumnType"/> == <see cref="BrowseColumnType.ObjectNumber"/>
        /// </summary>
        public GuiDataColumn ObjectColumn { get; private set; }
        #endregion
        #region Tvorba GuiId pro dodaný řádek
        /// <summary>
        /// Metoda vrátí identifikátor <see cref="GuiId"/> pro dodaný řádek <see cref="GuiDataRow"/>, vytvořený z dat v řádku pro klíčové sloupce (this).
        /// </summary>
        /// <param name="guiRow"></param>
        /// <returns></returns>
        public GuiId CreateRowGuiId(GuiDataRow guiRow)
        {
            if (guiRow == null) return null;
            if (this.HasRecordColumn)
            {
                object value = guiRow[this.RecordColumnId];
                return value as GuiId;
            }
            if (this.HasSubjectColumn && this.HasObjectColumn)
            {
                int classId = (this.SubjectColumn.TableClassId.HasValue ? this.SubjectColumn.TableClassId.Value : 0);
                int subjectId = Convert.ToInt32(guiRow[this.SubjectColumnId]);
                int objectId = Convert.ToInt32(guiRow[this.ObjectColumnId]);
                if (subjectId == 0 && objectId == 0) return null;
                return new GuiId(classId, subjectId, objectId);
            }
            if (this.HasSubjectColumn)
            {
                int classId = (this.SubjectColumn.TableClassId.HasValue ? this.SubjectColumn.TableClassId.Value : 0);
                int subjectId = Convert.ToInt32(guiRow[this.SubjectColumnId]);
                if (subjectId == 0) return null;
                return new GuiId(classId, subjectId);
            }
            if (this.HasObjectColumn)
            {
                int classId = (this.ObjectColumn.TableClassId.HasValue ? this.ObjectColumn.TableClassId.Value : 0);
                int objectId = Convert.ToInt32(guiRow[this.ObjectColumnId]);
                if (objectId == 0) return null;
                return new GuiId(classId, 0, objectId);
            }
            return null;
        }
        /// <summary>
        /// Index sloupce <see cref="RecordColumn"/>
        /// </summary>
        protected int RecordColumnId { get; set; }
        /// <summary>
        /// true = existuje sloupec <see cref="RecordColumn"/>
        /// </summary>
        protected bool HasRecordColumn { get { return (this.RecordColumnId >= 0); } }
        /// <summary>
        /// Index sloupce <see cref="SubjectColumnId"/>
        /// </summary>
        protected int SubjectColumnId { get; set; }
        /// <summary>
        /// true = existuje sloupec <see cref="SubjectColumn"/>
        /// </summary>
        protected bool HasSubjectColumn { get { return (this.SubjectColumnId >= 0); } }
        /// <summary>
        /// Index sloupce <see cref="ObjectColumnId"/>
        /// </summary>
        protected int ObjectColumnId { get; set; }
        /// <summary>
        /// true = existuje sloupec <see cref="ObjectColumn"/>
        /// </summary>
        protected bool HasObjectColumn { get { return (this.ObjectColumnId >= 0); } }
        #endregion
    }
    /// <summary>
    /// Interface pro <see cref="GuiDataTable"/> pro přístup k interním metodám
    /// </summary>
    public interface IGuiDataTable
    {
        /// <summary>
        /// Vrací index řádku
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        int GetRowIndex(GuiDataRow row);
        /// <summary>
        /// Vrací index sloupce
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        int GetColumnIndex(GuiDataColumn column);
    }
    /// <summary>
    /// Interface pro členy tabulky <see cref="GuiDataTable"/>
    /// </summary>
    public interface IGuiDataTableMember
    {
        /// <summary>
        /// Reference na vlastníka = tabulka.
        /// Je nastaveno při přidání do tabulky, je nulováno při odebrání.
        /// Neprovádí se persistence.
        /// </summary>
        GuiDataTable Table { get; set; }
    }
    /// <summary>
    /// Styl kreslení linky mezi Root nodem a jeho Child nody
    /// </summary>
    public enum GuiTreeViewLinkMode
    {
        /// <summary>
        /// Nekreslit
        /// </summary>
        None,
        /// <summary>
        /// Tečkovaná čára
        /// </summary>
        Dot,
        /// <summary>
        /// Plná čára
        /// </summary>
        Line,
        /// <summary>
        /// Plná čára 2 pixely
        /// </summary>
        Line2px
    }
    #endregion
    #region GuiTagItem : reprezentuje Tag jednoho řádku, z Tagů všech řádků tabulky lze sestavit filtr typu "štítky"
    /// <summary>
    /// GuiTagItem : reprezentuje Tag jednoho řádku (obsahuje klíč řádku + jeden Tag).
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
            return "RowId: " + (this.RowId != null ? this.RowId.ToString() : "Null") + "; TagItem: " + this.TagText;
        }
        /// <summary>
        /// Identifikátor řádku
        /// </summary>
        public GuiId RowId { get; set; }
        /// <summary>
        /// Text tagu
        /// </summary>
        public string TagText { get; set; }
        /// <summary>
        /// Relativní velikost písma Tagu. Null = default = odpovídá hodnotě 1.0 = normální velikost.
        /// Minimum = 0 = odpovídá 50% velikosti, maximum = 2 = odpovídá 200% velikosti.
        /// </summary>
        public float? TagSize { get; set; }
        /// <summary>
        /// Barva pozadí v běžném (nevybraném) stavu
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Barva pozadí ve stavu, kdy je prvek označen (aktivní)
        /// </summary>
        public Color? BackColorChecked { get; set; }
    }
    #endregion
    #region GuiGraph : Definiční data jednoho grafu + jeho sada položek
    /// <summary>
    /// GuiGraph : Definiční data jednoho grafu + jeho sada položek
    /// </summary>
    public class GuiGraph : GuiBase
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiGraph()
            : base()
        {
            this.GraphProperties = null;
            this.GraphItems = new List<GuiGraphItem>();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Graph RowId: " + (this.RowId == null ? "Null" : this.RowId.ToString()) + 
                "; Items.Count: " + (this.GraphItems != null ? this.GraphItems.Count.ToString() : "{Null}");
        }
        /// <summary>
        /// Název prvku <see cref="GraphProperties"/>
        /// </summary>
        public const string GRAPH_PROPERTIES_NAME = "graphProperties";
        #endregion
        #region Data
        /// <summary>
        /// ID řádku, v němž bude tento graf umístěn
        /// </summary>
        public GuiId RowId { get; set; }
        /// <summary>
        /// Definuje vlastnosti tohoto konkrétního grafu.
        /// Běžně bývá null (výchozí hodnota), pak se vlastnosti grafu přebírají z nadřízeného prvku (typicky z tabulky).
        /// </summary>
        public GuiGraphProperties GraphProperties { get; set; }
        /// <summary>
        /// Barva pozadí.
        /// Pokud není zadána, pak je pozadí grafu průhledné = vykresluje se do svého Parent prvku.
        /// </summary>
        public Color? BackgroundColor { get; set; }
        /// <summary>
        /// Podbarvení počátku grafu.
        /// Podbarvení je postupné (LinearGradientBrush) : na počátku grafu je barva <see cref="BeginShadowColor"/>,
        /// podbarvení má šířku <see cref="BeginShadowArea"/> a plynule přechází do barvy pozadí grafu <see cref="BackgroundColor"/>.
        /// </summary>
        public Color? BeginShadowColor { get; set; }
        /// <summary>
        /// Poměrná část šířky grafu, která je podbarvena pro zvýrazněnéí počátku.
        /// </summary>
        public float? BeginShadowArea { get; set; }
        /// <summary>
        /// Ikona na počátku grafu.
        /// Je zobrazena v levé části grafu, ve středu jeho výšky.
        /// Typicky vyjadřuje problém na počátku.
        /// </summary>
        public GuiImage BeginImage { get; set; }
        /// <summary>
        /// Podbarvení počátku grafu.
        /// Podbarvení je postupné (LinearGradientBrush) : na počátku grafu je barva <see cref="EndShadowColor"/>,
        /// podbarvení má šířku <see cref="EndShadowArea"/> a plynule přechází do barvy pozadí grafu <see cref="BackgroundColor"/>.
        /// </summary>
        public Color? EndShadowColor { get; set; }
        /// <summary>
        /// Poměrná část šířky grafu, která je podbarvena pro zvýrazněnéí počátku.
        /// </summary>
        public float? EndShadowArea { get; set; }
        /// <summary>
        /// Ikona na počátku grafu.
        /// Je zobrazena v levé části grafu, ve středu jeho výšky.
        /// Typicky vyjadřuje problém na počátku.
        /// </summary>
        public GuiImage EndImage { get; set; }
        /// <summary>
        /// Prvky tohoto grafu.
        /// Pokud jsou prvky umístěny zde, pak se vždy jedná o prvky tohoto jednoho grafu.
        /// </summary>
        public List<GuiGraphItem> GraphItems { get; set; }
        /// <summary>
        /// Přidá další prvek do this seznamu
        /// </summary>
        /// <param name="item"></param>
        public void Add(GuiGraphItem item)
        {
            if (item == null) return;
            if (this.GraphItems == null)
                this.GraphItems = new List<GuiGraphItem>();
            this.GraphItems.Add(item);
        }
        /// <summary>
        /// Přidá další prvky do this seznamu
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<GuiGraphItem> items)
        {
            if (items == null) return;
            if (this.GraphItems == null)
                this.GraphItems = new List<GuiGraphItem>();
            this.GraphItems.AddRange(items);
        }
        /// <summary>
        /// Počet prvků v kolekci
        /// </summary>
        public int Count { get { return this.GraphItems.Count; } }
        #endregion
        #region Klonování definičních dat, bez přenosu dat prvků
        /// <summary>
        /// Metoda vrátí new instanci vznikou jako kopie this, ale neobsahuje data v <see cref="GraphItems"/>.
        /// </summary>
        /// <returns></returns>
        public GuiGraph GetDefinitionData()
        {
            GuiGraph clone = (GuiGraph)this.MemberwiseClone();
            clone.GraphItems = null;
            return clone;
        }
        #endregion
    }
    #endregion
    #region GuiGraphProperties + GuiTimeAxisSegment : vlastnosti grafu
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
        /// Barva pozadí časové osy, defaultní.
        /// Časová osa může mít definované časové segmenty, viz property <see cref="TimeAxisSegmentList"/>, ty mohou mít jinou barvu.
        /// </summary>
        public Color? TimeAxisBackColor { get; set; }
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
        /// Efekt pro vykreslení prvku, pokud je Editovatelný.
        /// Pokud není zadán, použije se Default.
        /// </summary>
        public GuiGraphItemBackEffectStyle? BackEffectEditable { get; set; }
        /// <summary>
        /// Efekt pro vykreslení prvku, pokud je Needitovatelný.
        /// Pokud není zadán, použije se Default.
        /// </summary>
        public GuiGraphItemBackEffectStyle? BackEffectNonEditable { get; set; }
        /// <summary>
        /// Nejmenší šířka prvku grafu v pixelech. 
        /// Pokud by byla vypočtena šířka menší, bude zvětšena na tuto hodnotu - aby byl prvek grafu viditelný.
        /// Výchozí hodnota = 0, neprovádí se zvětšení, malé prvky (krátký čas na širokém měřítku) nejsou vidět.
        /// </summary>
        public int GraphItemMinPixelWidth { get; set; }
        /// <summary>
        /// Fyzická výška jedné logické linky grafu v pixelech.
        /// Určuje, tedy kolik pixelů bude vysoký prvek <see cref="GuiGraphItem"/>, jehož <see cref="GuiGraphItem.Height"/> = 1.0f.
        /// Výchozí hodnota je 20.
        /// Tato hodnota <see cref="GraphLineHeight"/> platí pro řádky, v nichž se vyskytují pouze prvky s celočíselnou výškou v <see cref="GuiGraphItem.Height"/>.
        /// Pro řádky, kde se vyskytne výška prvku <see cref="GuiGraphItem.Height"/> desetinná, se použije údaj <see cref="GraphLinePartialHeight"/>.
        /// </summary>
        public int GraphLineHeight { get; set; }
        /// <summary>
        /// Fyzická výška jedné logické linky grafu v pixelech, pro řádky obsahující prvky s výškou <see cref="GuiGraphItem.Height"/> desetinnou.
        /// V takových řádcích je vhodné použít větší hodnotu výšky logické linky, aby byly lépe viditelné prvky s malou výškou (např. výška prvku 0.25).
        /// Výchozí hodnota je 40.
        /// </summary>
        public int GraphLinePartialHeight { get; set; }
        /// <summary>
        /// Text, který pokud je obsažen v některém sloupci v tabulce textů <see cref="GuiGrid.GraphTextTable"/>, je rozpoznán jako zlom na nový řádek textu.
        /// Lze tak definovat Přehledovou šablonu, která definuje více řádků popisku v grafu, s definováním místa začátku nového řádku.
        /// </summary>
        public string GraphTextRowDelimiter { get; set; }
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
        /// <summary>
        /// Barva linky základní.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je větší nebo rovno Prev.End, pak se použije <see cref="LinkColorStandard"/>.
        /// Další barvy viz <see cref="LinkColorWarning"/> a <see cref="LinkColorError"/>
        /// </summary>
        public Color? LinkColorStandard { get; set; }
        /// <summary>
        /// Barva linky varovná.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.End, ale Next.Begin je větší nebo rovno Prev.Begin, pak se použije <see cref="LinkColorWarning"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorError"/>
        /// </summary>
        public Color? LinkColorWarning { get; set; }
        /// <summary>
        /// Barva linky chybová.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.Begin, pak se použije <see cref="LinkColorError"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorWarning"/>
        /// </summary>
        public Color? LinkColorError { get; set; }
        /// <summary>
        /// Segmenty časové osy, které mají jinou barvu pozadí než je základní barva, a mohou obsahovat přídavný ToolTip
        /// </summary>
        public List<GuiTimeAxisSegment> TimeAxisSegmentList { get; set; }
    }
    /// <summary>
    /// GuiTimeAxisSegment : Definice jednoho segmentu časové osy
    /// </summary>
    public sealed class GuiTimeAxisSegment : GuiBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiTimeAxisSegment()
        { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "TimeAxisSegment TimeRange: " + this.TimeRange + "; ToolTip: " + this.ToolTip;
        }
        /// <summary>
        /// Časový rozsah
        /// </summary>
        public GuiTimeRange TimeRange { get; set; }
        /// <summary>
        /// Barva pozadí v tomto segmentu
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Rozmezí výšky na ose Y, které bude obarveno barvou <see cref="BackColor"/>, zadané absolutně v pixelech.
        /// Pokud je null, značí celou výšku osy.
        /// Souběh zadání <see cref="SizeRange"/> a <see cref="HeightRange"/>:
        /// Pokud je zadáno <see cref="HeightRange"/>, pak má přednost před zadáním <see cref="SizeRange"/>.
        /// Vyjadřuje prostor na vizuální ose ve směru Y, ve kterém bude zobrazeno obarvení tohoto segmentu.
        /// </summary>
        public GuiInt32Range HeightRange { get; set; }
        /// <summary>
        /// Rozmezí výšky na ose Y, které bude obarveno barvou <see cref="BackColor"/>, zadané relativně k výšce osy.
        /// Povolené rozmezí = 0 až 1, což je i defaultní hodnota v případě null, značí celou výšku osy.
        /// Souběh zadání <see cref="SizeRange"/> a <see cref="HeightRange"/>:
        /// Pokud je zadáno <see cref="HeightRange"/>, pak má přednost před zadáním <see cref="SizeRange"/>.
        /// Vyjadřuje prostor na vizuální ose ve směru Y, ve kterém bude zobrazeno obarvení tohoto segmentu.
        /// </summary>
        public GuiDoubleRange SizeRange { get; set; }
        /// <summary>
        /// Text pro Tooltip v daném rozmezí, přidává se pod standardní text ToolTipu
        /// </summary>
        public string ToolTip { get; set; }
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
            this.Name = GRAPH_TABLE_NAME;
            this.GraphItems = new List<GuiGraphItem>();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Name + "; Count: " + (this.GraphItems != null ? this.GraphItems.Count.ToString() : "{Null}");
        }
        /// <summary>
        /// Název prvku <see cref="GuiGraphTable"/>
        /// </summary>
        public const string GRAPH_TABLE_NAME = "graphTable";
        /// <summary>
        /// Soupis položek grafů. Typicky jeden soupis obsahuje položky pro všechny řádky tabulky. GUI vrstva si položky rozebere.
        /// </summary>
        public List<GuiGraphItem> GraphItems { get; set; }
        /// <summary>
        /// Přidá další prvek do this seznamu
        /// </summary>
        /// <param name="item"></param>
        public void Add(GuiGraphItem item)
        {
            if (item == null) return;
            if (this.GraphItems == null)
                this.GraphItems = new List<GuiGraphItem>();
            this.GraphItems.Add(item);
        }
        /// <summary>
        /// Přidá další prvky do this seznamu
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<GuiGraphItem> items)
        {
            if (items == null) return;
            if (this.GraphItems == null)
                this.GraphItems = new List<GuiGraphItem>();
            this.GraphItems.AddRange(items);
        }
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
    #region GuiGraphItem : Jeden obdélníček v grafu na časové ose; GuiGraphSkin
    /// <summary>
    /// GuiGraphItem : Jeden obdélníček v grafu na časové ose
    /// </summary>
    public sealed class GuiGraphItem : GuiBase
    {
        #region Standardní public properties a konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiGraphItem()
            : base()
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
            return "RowId: " + this.RowId?.ToString() + "; ItemId: " + this.ItemId?.ToString() + "; Time: " + this.Time;
        }
        /// <summary>
        /// ID položky časového grafu (ID obdélníčku).
        /// Z databáze se načítá ze sloupců: "item_class_id", "item_record_id", je POVINNÝ.
        /// </summary>
        [PropertyName("Item")]
        public GuiId ItemId { get; set; }
        /// <summary>
        /// ID řádku, v jehož grafu se má tento prvek zobrazovat.
        /// Z databáze se načítá ze sloupců: "row_class_id", "row_record_id", je POVINNÝ.
        /// </summary>
        [PropertyName("Row")]
        public GuiId RowId { get; set; }
        /// <summary>
        /// GroupId: číslo skupiny. Prvky se shodným GroupId budou vykreslovány do společného "rámce", 
        /// a pokud mezi jednotlivými prvky grafu se shodným <see cref="GroupId"/> bude na ose X nějaké volné místo,
        /// nebude mezi nimi vykreslován žádný "cizí" prvek.
        /// Z databáze se načítá ze sloupců: "group_class_id", "group_record_id", je NEPOVINNÝ.
        /// </summary>
        [PropertyName("Group")]
        public GuiId GroupId { get; set; }
        /// <summary>
        /// ID datového záznamu, jehož formulář se má rozkliknout po Ctrl + DoubleKliknutí na záznam.
        /// Z databáze se načítá ze sloupců: "data_class_id", "data_record_id", je NEPOVINNÝ.
        /// </summary>
        [PropertyName("Rec")]
        public GuiId DataId { get; set; }
        /// <summary>
        /// Datum a čas počátku tohoto prvku.
        /// Z databáze se načítá ze sloupce: "begin" a "end", je POVINNÝ.
        /// </summary>
        public GuiTimeRange Time { get; set; }
        /// <summary>
        /// Režim chování položky grafu (editovatelnost, texty, atd).
        /// Tato hodnota se nenačítá z SQL SELECTU, musí se naplnit ručně.
        /// </summary>
        [PersistingEnabled(false)]       // Serializaci zajišťuje property Specification
        public GraphItemBehaviorMode BehaviorMode { get; set; }
        /// <summary>
        /// Layer: Vizuální vrstva. Prvky z různých vrstev jsou kresleny "přes sebe" = mohou se překrývat.
        /// Nižší hodnota je kreslena dříve.
        /// Například: záporná hodnota Layer reprezentuje "podklad" který se needituje.
        /// Z databáze se načítá ze sloupce: "layer", je NEPOVINNÝ.
        /// </summary>
        [PersistingEnabled(false)]       // Serializaci zajišťuje property Specification
        public int Layer { get; set; }
        /// <summary>
        /// Level: Vizuální hladina. Prvky v jedné hladině jsou kresleny do společného vodorovného pásu, 
        /// další prvky ve vyšší hladině jsou všechny zase vykresleny ve svém odděleném pásu (nad tímto nižším pásem). 
        /// Nespadnou do prvků nižšího pásu i když by v něm bylo volné místo.
        /// Z databáze se načítá ze sloupce: "level", je NEPOVINNÝ.
        /// </summary>
        [PersistingEnabled(false)]       // Serializaci zajišťuje property Specification
        public int Level { get; set; }
        /// <summary>
        /// Order: pořadí prvku při výpočtech souřadnic Y před vykreslováním. 
        /// Prvky se stejným Order budou tříděny vzestupně podle data počátku <see cref="Time"/>.Begin.
        /// Z databáze se načítá ze sloupce: "order", je NEPOVINNÝ.
        /// </summary>
        [PersistingEnabled(false)]       // Serializaci zajišťuje property Specification
        public int Order { get; set; }
        /// <summary>
        /// Relativní výška tohoto prvku. Standardní hodnota = 1.0F. Fyzická výška (v pixelech) jednoho prvku je dána součinem 
        /// <see cref="Height"/> * <see cref="GuiGraphProperties.GraphLineHeight"/>
        /// Prvky s výškou 0 a menší nebudou vykresleny.
        /// Z databáze se načítá ze sloupce: "height", je NEPOVINNÝ.
        /// </summary>
        [PersistingEnabled(false)]       // Serializaci zajišťuje property Specification
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
        /// Efekt pro vykreslení prvku, pokud je Editovatelný.
        /// Pokud není zadán, převezme se hodnota z <see cref="GuiGraphProperties.BackEffectEditable"/>, nebo se použije Default.
        /// </summary>
        public GuiGraphItemBackEffectStyle? BackEffectEditable { get; set; }
        /// <summary>
        /// Efekt pro vykreslení prvku, pokud je Needitovatelný.
        /// Pokud není zadán, převezme se hodnota z <see cref="GuiGraphProperties.BackEffectNonEditable"/>, nebo se použije Default.
        /// </summary>
        public GuiGraphItemBackEffectStyle? BackEffectNonEditable { get; set; }
        #endregion
        #region Skiny: SkinCurrent, SkinDefault, SkinDict, SkinCurrentIndex
        /// <summary>
        /// Aktuální Skin, může to být buď <see cref="SkinDefault"/> (pokud <see cref="SkinCurrentIndex"/> == 0), nebo některý z <see cref="SkinDict"/>.
        /// Nikdy není null.
        /// Přímý přístup k této property se příliš nepoužívá.
        /// </summary>
        [PersistingEnabled(false)]               // Neprovádí se persistence.
        public GuiGraphSkin SkinCurrent
        {
            get
            {
                if (this._SkinCurrent == null)
                    this._SkinCurrent = this.SkinDefault;
                return this._SkinCurrent;
            }
        }
        private GuiGraphSkin _SkinCurrent;
        /// <summary>
        /// Defaultní skin, nikdy není null.
        /// Přímý přístup k hodnotám se používá pouze při aktualizaci dat GUI vrstvy z nějaké Response.
        /// </summary>
        public GuiGraphSkin SkinDefault
        {
            get
            {
                if (this._SkinDefault == null)
                    this._SkinDefault = new GuiGraphSkin();
                return this._SkinDefault;
            }
            private set
            {
                this._SkinDefault = value;
            }
        }
        private GuiGraphSkin _SkinDefault;
        /// <summary>
        /// Kolekce skinů pro jiné hodnoty klíče <see cref="SkinCurrentIndex"/> než 0.
        /// Může být null, dokud není použit skin s indexem jiným než 0.
        /// Přímý přístup k hodnotám se používá pouze při aktualizaci dat GUI vrstvy z nějaké Response.
        /// </summary>
        public Dictionary<int, GuiGraphSkin> SkinDict { get; private set; }
        /// <summary>
        /// Index aktuálního Skinu. Výchozí hodnota = 0, ta odkazuje na defaultní skin.
        /// Lze setovat libovolnou numerickou hodnotu, tím se aktivuje daný skin. Skin pro novou hodnotu bude automaticky vytvořen jako prázdný.
        /// Čtení konkrétní hodnoty se provádí z explicitně deklarovaného skinu, a pokud v konkrétní property je null, pak se čte z defaultního skinu.
        /// Zápis hodnoty se provádí výhradně do aktuálního skinu (explicitní / defaultní).
        /// Je tak zajištěno, že bude existovat defaultní sada grafických hodnot (=defaultní skin) 
        /// plus libovolně široká řada explicitních skinů, které mohou přepisovat (tj. definovat vlastní) hodnotu jen u některé property.
        /// Aplikace deklaruje nejprve kompletní defaultní skin, a poté deklaruje potřebnou sadu skinů.
        /// <para/>
        /// Konkrétní skiny si aktivuje uživatel v GUI, typicky nějakým tlačítkem v toolbaru, které má definovanou akci <see cref="TargetActionType.ActivateGraphSkin"/>,
        /// s parametrem odpovídajícím číslu skinu.
        /// <para/>
        /// Aplikační kód při definici grafického prvku postupuje takto:
        /// a) nastaví index <see cref="SkinCurrentIndex"/> na 0 (lze vynechat, to je default);
        /// b) naplní hodnoty do properties typu Color;
        /// c) nastaví index <see cref="SkinCurrentIndex"/> na další hodnoty, podle toho které skiny chce používat;
        /// d) naplní hodnoty do properties typu Color;
        /// <para/>
        /// Skin ovlivňuje hodnoty v těchto properties:
        /// <see cref="GuiGraphItem.BackColor"/>, <see cref="GuiGraphItem.HatchColor"/>, <see cref="GuiGraphItem.LineColor"/>, 
        /// <see cref="GuiGraphItem.BackStyle"/>, <see cref="GuiGraphItem.RatioBeginBackColor"/>, <see cref="GuiGraphItem.RatioEndBackColor"/>, 
        /// <see cref="GuiGraphItem.RatioLineColor"/>, <see cref="GuiGraphItem.RatioLineWidth"/>, 
        /// <see cref="GuiGraphItem.ImageBegin"/>, <see cref="GuiGraphItem.ImageEnd"/>.
        /// </summary>
        [PersistingEnabled(false)]               // Neprovádí se persistence. Aktuální hodnota SkinCurrentIndex (ze strany zdroje) nemá po persistenci význam (na straně cíle).
        public int SkinCurrentIndex
        {
            get { return this._SkinCurrentIndex; }
            set
            {
                int key = value;
                GuiGraphSkin skin = null;
                if (key == 0)
                {   // Aktivujeme výchozí skin:
                    skin = this.SkinDefault;
                }
                else
                {   // Aktivujeme explicitní skin:
                    if (this.SkinDict == null)
                        this.SkinDict = new Dictionary<int, GuiGraphSkin>();
                    if (!this.SkinDict.TryGetValue(key, out skin))
                    {
                        skin = new GuiGraphSkin();
                        this.SkinDict.Add(key, skin);
                    }
                }
                this._SkinCurrent = skin;
                this._SkinCurrentIndex = value;
            }
        }
        private int _SkinCurrentIndex;
        #endregion
        #region Aktuální hodnoty Skinu načtené z SkinCurrent a SkinDefault. Tyto property se nepersistují (to zajišťuje SkinDefault a SkinDict)
        /// <summary>
        /// Prvek je viditelný?
        /// Defaultní hodnota je null, tato hodnota se interpretuje jako true (v metodách, kde je třeba binární výstup)
        /// </summary>
        [PersistingEnabled(false)]               // Tato hodnota se persistuje v rámci skinu, tato property hodnotu čte a ukládá do skinu
        public bool? IsVisible { get { return this.SkinCurrent.IsVisible ?? this.SkinDefault.IsVisible; } set { this.SkinCurrent.IsVisible = value; } }
        /// <summary>
        /// Barva pozadí prvku.
        /// Pokud bude null, pak prvek nebude mít vyplněný svůj prostor (obdélník). Může mít vykreslené okraje (barva <see cref="LineColor"/>).
        /// Anebo může mít kreslené Ratio (viz property <see cref="RatioBegin"/>, <see cref="RatioEnd"/>, 
        /// <see cref="RatioBeginBackColor"/>, <see cref="RatioLineColor"/>, <see cref="RatioLineWidth"/>).
        /// Z databáze se načítá ze sloupce: "back_color", je NEPOVINNÝ.
        /// </summary>
        [PersistingEnabled(false)]               // Tato hodnota se persistuje v rámci skinu, tato property hodnotu čte a ukládá do skinu
        public Color? BackColor { get { return this.SkinCurrent.BackColor ?? this.SkinDefault.BackColor; } set { this.SkinCurrent.BackColor = value; } }
        /// <summary>
        /// Barva šrafování prvku, kreslená stylem <see cref="BackStyle"/>.
        /// Prvek nejprve vykreslí svoje pozadí barvou <see cref="BackColor"/>, 
        /// a pokud má definovaný styl <see cref="BackStyle"/>, pak přes toto pozadí vykreslí ještě daný styl (šrafování, jiné překrytí) touto barvou.
        /// Pokud bude definován styl <see cref="BackStyle"/> a nebude daná barva <see cref="HatchColor"/>,
        /// použije se barva <see cref="LineColor"/>.
        /// </summary>
        [PersistingEnabled(false)]               // Tato hodnota se persistuje v rámci skinu, tato property hodnotu čte a ukládá do skinu
        public Color? HatchColor { get { return this.SkinCurrent.HatchColor ?? this.SkinDefault.HatchColor; } set { this.SkinCurrent.HatchColor = value; } }
        /// <summary>
        /// Barva linek ohraničení prvku.
        /// Pokud je null, pak prvek nemá ohraničení pomocí linky (Border).
        /// Z databáze se načítá ze sloupce: "line_color", je NEPOVINNÝ.
        /// </summary>
        [PersistingEnabled(false)]               // Tato hodnota se persistuje v rámci skinu, tato property hodnotu čte a ukládá do skinu
        public Color? LineColor { get { return this.SkinCurrent.LineColor ?? this.SkinDefault.LineColor; } set { this.SkinCurrent.LineColor = value; } }
        /// <summary>
        /// Styl vzorku kresleného v pozadí.
        /// null = Solid.
        /// Z databáze se načítá ze sloupce: "back_style", je NEPOVINNÝ.
        /// </summary>
        [PersistingEnabled(false)]               // Tato hodnota se persistuje v rámci skinu, tato property hodnotu čte a ukládá do skinu
        public System.Drawing.Drawing2D.HatchStyle? BackStyle { get { return this.SkinCurrent.BackStyle ?? this.SkinDefault.BackStyle; } set { this.SkinCurrent.BackStyle = value; } }
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
        [PersistingEnabled(false)]               // Tato hodnota se persistuje v rámci skinu, tato property hodnotu čte a ukládá do skinu
        public Color? RatioBeginBackColor { get { return this.SkinCurrent.RatioBeginBackColor ?? this.SkinDefault.RatioBeginBackColor; } set { this.SkinCurrent.RatioBeginBackColor = value; } }
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
        [PersistingEnabled(false)]               // Tato hodnota se persistuje v rámci skinu, tato property hodnotu čte a ukládá do skinu
        public Color? RatioEndBackColor { get { return this.SkinCurrent.RatioEndBackColor ?? this.SkinDefault.RatioEndBackColor; } set { this.SkinCurrent.RatioEndBackColor = value; } }
        /// <summary>
        /// Barva linky, kreslená v úrovni Ratio.
        /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má zadanou hodnotu v rozsahu 0 (včetně) a více.
        /// Touto barvou je vykreslena přímá linie, která symbolizuje míru "naplnění" daného úseku, 
        /// a spojuje body Begin = { <see cref="Time"/>.Begin, <see cref="RatioBegin"/> } a { <see cref="Time"/>.End, <see cref="RatioEnd"/> }.
        /// Z databáze se načítá ze sloupce: "ratio_line_color", je NEPOVINNÝ.
        /// </summary>
        [PersistingEnabled(false)]               // Tato hodnota se persistuje v rámci skinu, tato property hodnotu čte a ukládá do skinu
        public Color? RatioLineColor { get { return this.SkinCurrent.RatioLineColor ?? this.SkinDefault.RatioLineColor; } set { this.SkinCurrent.RatioLineColor = value; } }
        /// <summary>
        /// Šířka linky, kreslená v úrovni Ratio.
        /// Použije se tehdy, když hodnota <see cref="RatioBegin"/> a/nebo <see cref="RatioEnd"/> má zadanou hodnotu v rozsahu 0 (včetně) a více.
        /// Čárou této šířky je vykreslena přímá linie, která symbolizuje míru "naplnění" daného úseku, 
        /// a spojuje body Begin = { <see cref="Time"/>.Begin, <see cref="RatioBegin"/> } a { <see cref="Time"/>.End, <see cref="RatioEnd"/> }.
        /// Z databáze se načítá ze sloupce: "ratio_line_width", je NEPOVINNÝ.
        /// </summary>
        [PersistingEnabled(false)]               // Tato hodnota se persistuje v rámci skinu, tato property hodnotu čte a ukládá do skinu
        public int? RatioLineWidth { get { return this.SkinCurrent.RatioLineWidth ?? this.SkinDefault.RatioLineWidth; } set { this.SkinCurrent.RatioLineWidth = value; } }
        /// <summary>
        /// Obrázek vykreslený 1x za jednu grupu na souřadnici jejího začátku.
        /// Obrázek může být umístěn do kteréhokoli jednoho prvku v rámci grupy, akceptován bude první ve směru času.
        /// <para/>
        /// Informace pro aktualizaci dat, při tvorbě dat do <see cref="GuiResponse"/>:
        /// Pokud dřívější grafický prvek obsahoval obrázek, a nový jej obsahovat nemá (=je třeba zhasnout obrázek), 
        /// pak se jako <see cref="ImageBegin"/> má vložit <see cref="GuiImage.Empty"/>
        /// </summary>
        [PersistingEnabled(false)]               // Tato hodnota se persistuje v rámci skinu, tato property hodnotu čte a ukládá do skinu
        public GuiImage ImageBegin { get { return this.SkinCurrent.ImageBegin ?? this.SkinDefault.ImageBegin; } set { this.SkinCurrent.ImageBegin = value; } }
        /// <summary>
        /// Obrázek vykreslený 1x za jednu grupu na souřadnici jejího konce.
        /// Obrázek může být umístěn do kteréhokoli jednoho prvku v rámci grupy, akceptován bude poslední ve směru času.
        /// <para/>
        /// Informace pro aktualizaci dat, při tvorbě dat do <see cref="GuiResponse"/>:
        /// Pokud dřívější grafický prvek obsahoval obrázek, a nový jej obsahovat nemá (=je třeba zhasnout obrázek), 
        /// pak se jako <see cref="ImageEnd"/> má vložit <see cref="GuiImage.Empty"/>
        /// </summary>
        [PersistingEnabled(false)]               // Tato hodnota se persistuje v rámci skinu, tato property hodnotu čte a ukládá do skinu
        public GuiImage ImageEnd { get { return this.SkinCurrent.ImageEnd ?? this.SkinDefault.ImageEnd; } set { this.SkinCurrent.ImageEnd = value; } }
        #endregion
        #region Optimalizace pro persistenci dat : protected properties s menší velikosti v serializaci, nahrazující standardní serializaci některých public properties
        /// <summary>
        /// Property která slouží k serializaci hodnot z <see cref="BehaviorMode"/>, <see cref="Layer"/>, 
        /// <see cref="Level"/>, <see cref="Order"/>, <see cref="Height"/>
        /// </summary>
        [PropertyName("Spec")]
        private string Specification
        {
            get
            {
                string result = ((int)this.BehaviorMode).ToString() + ";" +
                                (((this.Height % 1f) == 0f) ? ((int)this.Height).ToString() : this.Height.ToString()) + ";" +
                                this.Layer.ToString() + ";" +
                                this.Level.ToString() + ";" +
                                this.Order.ToString();
                return result;
            }
            set
            {
                GraphItemBehaviorMode behaviorMode = GraphItemBehaviorMode.None;
                float height = 1f;
                int layer = 0;
                int level = 0;
                int order = 0;

                if (!String.IsNullOrEmpty(value))
                {
                    string[] items = value.Split(';');
                    behaviorMode = (GraphItemBehaviorMode)ConvertToInt32(items, 0);
                    height = ConvertToSingle(items, 1);
                    layer = ConvertToInt32(items, 2);
                    level = ConvertToInt32(items, 3);
                    order = ConvertToInt32(items, 4);
                }

                this.BehaviorMode = behaviorMode;
                this.Height = height;
                this.Layer = layer;
                this.Level = level;
                this.Order = order;
            }
        }
        #endregion
        #region Data pro tvorbu záznamu
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
        #endregion
    }
    /// <summary>
    /// Styl výplně pozadí prvku grafu
    /// </summary>
    public enum GuiGraphItemBackEffectStyle
    {
        /// <summary>
        /// Standardní
        /// </summary>
        Default = 0,
        /// <summary>
        /// Plochý
        /// </summary>
        Flat,
        /// <summary>
        /// Výrazně trubkovitý tvar
        /// </summary>
        Pipe
    }
    /// <summary>
    /// Definice skinu pro grafický prvek
    /// </summary>
    [PersistingOnHeap]
    public class GuiGraphSkin
    {
        #region Standardní properties
        /// <summary>
        /// Prvek je viditelný?
        /// Defaultní hodnota je null, tato hodnota se interpretuje jako true (v metodách, kde je třeba binární výstup)
        /// </summary>
        public bool? IsVisible { get; set; }
        /// <summary>
        /// Barva pozadí prvku.
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Barva šrafování prvku, kreslená stylem <see cref="BackStyle"/>.
        /// Prvek nejprve vykreslí svoje pozadí barvou <see cref="BackColor"/>, 
        /// a pokud má definovaný styl <see cref="BackStyle"/>, pak přes toto pozadí vykreslí ještě daný styl (šrafování, jiné překrytí) touto barvou.
        /// Pokud bude definován styl <see cref="BackStyle"/> a nebude daná barva <see cref="HatchColor"/>,
        /// použije se barva <see cref="LineColor"/>.
        /// </summary>
        public Color? HatchColor { get; set; }
        /// <summary>
        /// Barva linek ohraničení prvku.
        /// Pokud je null, pak prvek nemá ohraničení pomocí linky (Border).
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
        /// Barva pozadí prvku, kreslená v části Ratio, na straně času Begin.
        /// Použije se tehdy, když hodnota <see cref="GuiGraphItem.RatioBegin"/> a/nebo <see cref="GuiGraphItem.RatioEnd"/> má hodnotu větší než 0f.
        /// Touto barvou je vykreslena dolní část prvku, která symbolizuje míru "naplnění" daného úseku.
        /// Tato část má tvar lichoběžníku, dolní okraj je na hodnotě 0, levý okraj má výšku <see cref="GuiGraphItem.RatioBegin"/>, pravý okraj má výšku <see cref="GuiGraphItem.RatioEnd"/>.
        /// Může sloužit k zobrazení vyčerpané pracovní kapacity, nebo jako lineární částečka grafu sloupcového nebo liniového.
        /// Tato barva se použije buď jako Solid color pro celý prvek v části Ratio, 
        /// anebo jako počáteční barva na souřadnici X = čas Begin při výplni Linear, 
        /// a to tehdy, pokud je zadána i barva <see cref="RatioEndBackColor"/> (ta reprezentuje barvu na souřadnici X = čas End).
        /// Z databáze se načítá ze sloupce: "ratio_begin_back_color", je NEPOVINNÝ.
        /// </summary>
        public Color? RatioBeginBackColor { get; set; }
        /// <summary>
        /// Barva pozadí prvku, kreslená v části Ratio, na straně času End.
        /// Použije se tehdy, když hodnota <see cref="GuiGraphItem.RatioBegin"/> a/nebo <see cref="GuiGraphItem.RatioEnd"/> má hodnotu větší než 0f.
        /// Touto barvou je vykreslena dolní část prvku, která symbolizuje míru "naplnění" daného úseku.
        /// Tato část má tvar lichoběžníku, dolní okraj je na hodnotě 0, levý okraj má výšku <see cref="GuiGraphItem.RatioBegin"/>, pravý okraj má výšku <see cref="GuiGraphItem.RatioEnd"/>.
        /// Může sloužit k zobrazení vyčerpané pracovní kapacity, nebo jako lineární částečka grafu sloupcového nebo liniového.
        /// Tato barva se použije jako koncová barva (na souřadnici X = čas End) v lineární výplni prostoru Ratio,
        /// kde počáteční barva výplně (na souřadnici X = čas Begin) je dána v <see cref="RatioBeginBackColor"/>.
        /// Z databáze se načítá ze sloupce: "ratio_end_back_color", je NEPOVINNÝ.
        /// </summary>
        public Color? RatioEndBackColor { get; set; }
        /// <summary>
        /// Barva linky, kreslená v úrovni Ratio.
        /// Použije se tehdy, když hodnota <see cref="GuiGraphItem.RatioBegin"/> a/nebo <see cref="GuiGraphItem.RatioEnd"/> má zadanou hodnotu v rozsahu 0 (včetně) a více.
        /// Touto barvou je vykreslena přímá linie, která symbolizuje míru "naplnění" daného úseku, 
        /// a spojuje body Begin = { <see cref="GuiGraphItem.Time"/>.Begin, <see cref="GuiGraphItem.RatioBegin"/> } a { <see cref="GuiGraphItem.Time"/>.End, <see cref="GuiGraphItem.RatioEnd"/> }.
        /// Z databáze se načítá ze sloupce: "ratio_line_color", je NEPOVINNÝ.
        /// </summary>
        public Color? RatioLineColor { get; set; }
        /// <summary>
        /// Šířka linky, kreslená v úrovni Ratio.
        /// Použije se tehdy, když hodnota <see cref="GuiGraphItem.RatioBegin"/> a/nebo <see cref="GuiGraphItem.RatioEnd"/> má zadanou hodnotu v rozsahu 0 (včetně) a více.
        /// Čárou této šířky je vykreslena přímá linie, která symbolizuje míru "naplnění" daného úseku, 
        /// a spojuje body Begin = { <see cref="GuiGraphItem.Time"/>.Begin, <see cref="GuiGraphItem.RatioBegin"/> } a { <see cref="GuiGraphItem.Time"/>.End, <see cref="GuiGraphItem.RatioEnd"/> }.
        /// Z databáze se načítá ze sloupce: "ratio_line_width", je NEPOVINNÝ.
        /// </summary>
        public int? RatioLineWidth { get; set; }
        /// <summary>
        /// Obrázek vykreslený 1x za jednu grupu na souřadnici jejího začátku.
        /// Obrázek může být umístěn do kteréhokoli jednoho prvku v rámci grupy, akceptován bude první ve směru času.
        /// <para/>
        /// Informace pro aktualizaci dat, při tvorbě dat do <see cref="GuiResponse"/>:
        /// Pokud dřívější grafický prvek obsahoval obrázek, a nový jej obsahovat nemá (=je třeba zhasnout obrázek), 
        /// pak se jako <see cref="ImageBegin"/> má vložit <see cref="GuiImage.Empty"/>
        /// </summary>
        public GuiImage ImageBegin { get; set; }
        /// <summary>
        /// Obrázek vykreslený 1x za jednu grupu na souřadnici jejího konce.
        /// Obrázek může být umístěn do kteréhokoli jednoho prvku v rámci grupy, akceptován bude poslední ve směru času.
        /// <para/>
        /// Informace pro aktualizaci dat, při tvorbě dat do <see cref="GuiResponse"/>:
        /// Pokud dřívější grafický prvek obsahoval obrázek, a nový jej obsahovat nemá (=je třeba zhasnout obrázek), 
        /// pak se jako <see cref="ImageEnd"/> má vložit <see cref="GuiImage.Empty"/>
        /// </summary>
        public GuiImage ImageEnd { get; set; }
        #endregion
    }
    #endregion
    #region GuiGraphLink : propojovací linka mezi prvky grafů GuiGraphItem
    /// <summary>
    /// GuiGraphLink : jedna propojovací linka mezi dvěma prvky grafů <see cref="GuiGraphItem"/>.
    /// Prvek grafu je identifikován pomocí jeho <see cref="GuiId"/>.
    /// </summary>
    public class GuiGraphLink : GuiBase
    {
        #region Standardní public properties a konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiGraphLink() : base()
        {
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Link; Prev: " + this.ItemIdPrev + "; Type: " + this.LinkType + "; Next: " + this.ItemIdNext;
        }
        /// <summary>
        /// ID prvku předchozího v tomto vztahu.
        /// ID může pocházet buď <see cref="GuiGraphItem.ItemId"/>, nebo <see cref="GuiGraphItem.GroupId"/>.
        /// Podle toho se Link chová a vykresluje.
        /// Z vizuálního hlediska je vhodnější používat <see cref="GuiGraphItem.GroupId"/>, neboť grupa se vykresluje jako kompaktní obdélník, 
        /// a pokud by se Linky vykreslovaly z jednotlivého Itemu (=někde z prostředka grupy), vypadalo by to divoce.
        /// </summary>
        [PropertyName("Prev")]
        public GuiId ItemIdPrev { get; set; }
        /// <summary>
        /// ID prvku následujícího v tomto vztahu.
        /// ID může pocházet buď <see cref="GuiGraphItem.ItemId"/>, nebo <see cref="GuiGraphItem.GroupId"/>.
        /// Podle toho se Link chová a vykresluje.
        /// Z vizuálního hlediska je vhodnější používat <see cref="GuiGraphItem.GroupId"/>, neboť grupa se vykresluje jako kompaktní obdélník, 
        /// a pokud by se Linky vykreslovaly z jednotlivého Itemu (=někde z prostředka grupy), vypadalo by to divoce.
        /// </summary>
        [PropertyName("Next")]
        public GuiId ItemIdNext { get; set; }
        /// <summary>
        /// Typ vztahu logický.
        /// Nezadáno = tento vztah se nemá vizualizovat.
        /// Pokud je link předáván jako součást response (v <see cref="GuiResponse.ChangeLinks"/>), pak null hodnota značí "Stávající Link odebrat".
        /// </summary>
        [PersistingEnabled(false)]       // Serializaci zajišťuje property Specification
        public GuiGraphItemLinkRelation? RelationType { get; set; }
        /// <summary>
        /// Typ vztahu grafický.
        /// Nezadáno = tento vztah se nemá vizualizovat.
        /// Pokud je link předáván jako součást response (v <see cref="GuiResponse.ChangeLinks"/>), pak null hodnota značí "Stávající Link odebrat".
        /// </summary>
        [PersistingEnabled(false)]       // Serializaci zajišťuje property Specification
        public GuiGraphItemLinkType? LinkType { get; set; }
        /// <summary>
        /// Šířka linky, nezadáno = 1
        /// </summary>
        [PersistingEnabled(false)]       // Serializaci zajišťuje property Specification
        public int? LinkWidth { get; set; }
        /// <summary>
        /// Barva linky základní.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je větší nebo rovno Prev.End, pak se použije <see cref="LinkColorStandard"/>.
        /// Další barvy viz <see cref="LinkColorWarning"/> a <see cref="LinkColorError"/>.
        /// Pokud bude null, převezme se barva z definice vlastností grafu <see cref="GuiGraphProperties.LinkColorStandard"/>.
        /// </summary>
        public Color? LinkColorStandard { get; set; }
        /// <summary>
        /// Barva linky varovná.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.End, ale Next.Begin je větší nebo rovno Prev.Begin, pak se použije <see cref="LinkColorWarning"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorError"/>
        /// Pokud bude null, převezme se barva z definice vlastností grafu <see cref="GuiGraphProperties.LinkColorWarning"/>.
        /// </summary>
        public Color? LinkColorWarning { get; set; }
        /// <summary>
        /// Barva linky chybová.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.Begin, pak se použije <see cref="LinkColorError"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorWarning"/>
        /// Pokud bude null, převezme se barva z definice vlastností grafu <see cref="GuiGraphProperties.LinkColorError"/>.
        /// </summary>
        public Color? LinkColorError { get; set; }
        #endregion
        #region Optimalizace pro persistenci dat : protected properties s menší velikosti v serializaci, nahrazující standardní serializaci některých public properties
        /// <summary>
        /// Property která slouží k serializaci hodnot z <see cref="RelationType"/>, <see cref="LinkType"/>, <see cref="LinkWidth"/>
        /// </summary>
        [PropertyName("Spec")]
        protected string Specification
        {
            get
            {
                string result = (this.RelationType.HasValue ? ((int)this.RelationType.Value).ToString() : "N") + ";" +
                                (this.LinkType.HasValue ? ((int)this.LinkType.Value).ToString() : "N") + ";" +
                                (this.LinkWidth.HasValue ? ((int)this.LinkWidth.Value).ToString() : "N");
                return result;
            }
            set
            {
                GuiGraphItemLinkRelation? relationType = null;
                GuiGraphItemLinkType? linkType = null;
                int? linkWidth = null;

                if (!String.IsNullOrEmpty(value))
                {
                    string[] items = value.Split(';');
                    int? num;
                    num = ConvertToInt32N(items, 0);
                    if (num.HasValue) relationType = (GuiGraphItemLinkRelation)num.Value;
                    num = ConvertToInt32N(items, 1);
                    if (num.HasValue) linkType = (GuiGraphItemLinkType)num.Value;
                    num = ConvertToInt32N(items, 2);
                    if (num.HasValue) linkWidth = num.Value;
                }

                this.RelationType = relationType;
                this.LinkType = linkType;
                this.LinkWidth = linkWidth;
            }
        }
        #endregion
    }
    /// <summary>
    /// Typ spojovací vztahu, který popisuje link <see cref="GuiGraphLink"/> mezi dvěma prvky.
    /// Má spíše datový charakter. Pomáhá vrstvě GUI určit, které vztahy kdy zobrazovat.
    /// </summary>
    public enum GuiGraphItemLinkRelation
    {
        /// <summary>
        /// Nezadáno.
        /// </summary>
        None,
        /// <summary>
        /// Prvky jsou na stejné úrovni, typicky dvě operace v jednom postupu.
        /// </summary>
        OneLevel,
        /// <summary>
        /// Prvky jsou na různých úrovních, typicky vztah mezi poslední operací nižšího postup a navazující operací vyššího postupu.
        /// </summary>
        DifferentLevel
    }
    /// <summary>
    /// Typ spojovací linky <see cref="GuiGraphLink"/> mezi dvěma prvky.
    /// Má víceméně vizuální dopad.
    /// </summary>
    public enum GuiGraphItemLinkType
    {
        /// <summary>
        /// Nezadáno.
        /// </summary>
        None,
        /// <summary>
        /// Neviditelná
        /// </summary>
        Invisible,
        /// <summary>
        /// Běžná přímá návaznost = Konec prvku Prev se napojí na Počátek prvku Next, rovnou čárou
        /// </summary>
        PrevEndToNextBeginLine,
        /// <summary>
        /// Běžná hladká návaznost = Konec prvku Prev se napojí na Počátek prvku Next, S-křivkou
        /// </summary>
        PrevEndToNextBeginSCurve,
        /// <summary>
        /// Běžná synchronizace = propojí se středy prvků
        /// </summary>
        PrevCenterToNextCenter
    }
    #endregion
    #region GuiKeyAction : Akce spojená s klávesou
    /// <summary>
    /// GuiKeyAction : Akce spojená s klávesou
    /// </summary>
    public class GuiKeyAction
    {
        /// <summary>
        /// Definice klávesy, pro kterou je tato akce definovaná
        /// </summary>
        public Keys KeyData { get; set; }
        /// <summary>
        /// Čas, po který maximálně bude blokován GUI, po dobu běhu této funkce.
        /// Pokud je zde null, pak běh funkce neblokuje GUI.
        /// Pokud je zde nějaký (kladný) čas, pak po tuto dobu bude GUI okna blokováno, do doby doběhnutí funkce nebo do doběhnutí tohoto Timeoutu.
        /// Po dobu blokování může být zobrazena hláška <see cref="BlockGuiMessage"/>.
        /// </summary>
        public TimeSpan? BlockGuiTime { get; set; }
        /// <summary>
        /// Zpráva zobrazená uživateli po dobu blokování GUI.
        /// Zpráva může obsahovat více řádků, oddělené CrLf.
        /// První řádek bude zobrazen výrazně (jako titulek), další řádky standardně.
        /// Zpráva bude zobrazena pouze tehdy, když <see cref="BlockGuiTime"/> bude obsahovat čas timeoutu, bez něj je message nepoužitá.
        /// </summary>
        public string BlockGuiMessage { get; set; }
        /// <summary>
        /// Předpis pro akce, které na základě aktivace tohoto prvku má provést vrstva GUI.
        /// Lze deklarovat více než jednu akci.
        /// Lze potlačit volání servisní funkce aplikačního serveru : <see cref="GuiActionType.SuppressCallAppHost"/>.
        /// </summary>
        public GuiActionType? GuiActions { get; set; }
        /// <summary>
        /// Jména akcí, které se mají provést, pokud <see cref="GuiActions"/> bude obsahovat <see cref="GuiActionType.RunInteractions"/>.
        /// Jde o seznam oddělený středníky nebo čárkami, jehož jednotlivé položky určují názvy tabulek Source a názvy interakcí <see cref="GuiGridInteraction"/>, které se na základě tohoto tlačítka mají provést.
        /// Název tabulky a název interakce je oddělen dvojtečkou (nebo tečkou);
        /// Název tabulky musí být FullName (například Data\pages\MainPage\mainPanel\GridCenter);
        /// interakce jako data jsou totiž definovány v rámci <see cref="GuiGrid.GridProperties"/>, v <see cref="GuiGridProperties.InteractionList"/>.
        /// Příklad: pokud toto tlačítko Toolbaru obsahuje <see cref="RunInteractionNames"/> = "GridLeft:SelectOperations", pak se provede:
        /// a) najde se tabulka (<see cref="GuiGrid"/>) s Name = "GridLeft", a
        /// b) provede se její interakce "SelectOperations".
        /// <para/>
        /// Za názvem interakce může být středník a parametr dané interakce. například: GridLeft:ShowColor:1,GridCenter:ShowColor:1
        /// <para/>
        /// Pokud bude požadována akce <see cref="GuiActions"/> : <see cref="GuiActionType.RunInteractions"/>, ale <see cref="RunInteractionNames"/>, 
        /// žádná interakce se neprovede.
        /// </summary>
        public string RunInteractionNames { get; set; }
        /// <summary>
        /// Zdrojové akce, které se mají po stisknutí tohoto tlačítka provést v rámci interakcí.
        /// Vyhodnocuje se pouze pokud v <see cref="GuiActions"/> je hodnota <see cref="GuiActionType.RunInteractions"/>.
        /// </summary>
        public SourceActionType? RunInteractionSource { get; set; }
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
        public void Add(GuiToolbarItem item)
        {
            if (this.Items == null)
                this.Items = new List<GuiToolbarItem>();
            this.Items.Add(item);
        }
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
        /// Název nebo binární obsah obrázku pro stav <see cref="IsChecked"/>.
        /// Pokud prvek nebude mít nastaveno <see cref="IsCheckable"/>, pak nebude nikdy ve stavu <see cref="IsChecked"/>, a tento obrázek se neuplatní.
        /// </summary>
        public GuiImage ImageChecked { get; set; }
        /// <summary>
        /// Druh prvku. Implicitní typ (pokud je zadáno null) = <see cref="FunctionGlobalItemType.Button"/>.
        /// Některé typy prvku mají rozšířené nastavení ve speciálním objektu:
        /// <para>Pro typ <see cref="FunctionGlobalItemType.TrackBar"/>: <see cref="TrackBarSettings"/></para>
        /// </summary>
        public FunctionGlobalItemType? ItemType { get; set; }
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
        /// <summary>
        /// true = prvek lze zaškrtnout (označit).
        /// Výchozí hodnota = null, bude interpretována jako false.
        /// </summary>
        public bool? IsCheckable { get; set; }
        /// <summary>
        /// true = prvek je zaškrtnutý (označený).
        /// Výchozí hodnota = null, bude interpretována jako false.
        /// </summary>
        public bool? IsChecked { get; set; }
        /// <summary>
        /// Obsahuje název skupiny prvků, které se vzájemně chovají jako OptionGroup.
        /// To znamená, že právě jeden z prvků skupiny může být <see cref="IsChecked"/> = být označen jako aktivní.
        /// <para/>
        /// Chování:
        /// <para/>
        /// a) Pokud je <see cref="CheckedGroupName"/> prázdné, pak se button chová jako CheckBox: změna jeho hodnoty <see cref="IsChecked"/> neovlivní žádný jiný prvek.
        /// Kliknutí na takový prvek mění hodnotu <see cref="IsChecked"/> z false na true a naopak = lze jej shodit na false.
        /// <para/>
        /// b) Pokud je <see cref="CheckedGroupName"/> prázdné, pak se button chová jako RadioButton: kliknutí na neoznačený button jej označí a současně odznačí ostatní buttony v grupě.
        /// Opakované kliknutí na označený button jej neodznačí.
        /// Prvky jedné grupy <see cref="CheckedGroupName"/> se musí nacházet v jedné grafické skupině <see cref="GroupName"/> (platí pro Toolbar).
        /// Pokud by byly umístěny v jiné grupě, nebudou považovány za jednu skupinu, ale více oddělených skupin.
        /// Naproti tomu jedna grafická grupa <see cref="GroupName"/> může obsahovat více skupin <see cref="CheckedGroupName"/>.
        /// <para/>
        /// Je rozumné dávat prvky jedné <see cref="CheckedGroupName"/> blízko k sobě, ale technicky nutné to není.
        /// </summary>
        public string CheckedGroupName { get; set; }
        /// <summary>
        /// Ukládat hodnotu tohoto prvku <see cref="IsChecked"/> do uživatelské konfigurace, a při příštím startu aplikace ji obnovit na předchozí hodnotu?
        /// Default = null = ano (aplikuje se jen na ty prvky, které mají nějakou hodnotu)
        /// </summary>
        public bool? StoreValueToConfig { get; set; }
        /// <summary>
        /// Čas, po který maximálně bude blokován GUI, po dobu běhu této funkce.
        /// Pokud je zde null, pak běh funkce neblokuje GUI.
        /// Pokud je zde nějaký (kladný) čas, pak po tuto dobu bude GUI okna blokováno, do doby doběhnutí funkce nebo do doběhnutí tohoto Timeoutu.
        /// Po dobu blokování může být zobrazena hláška <see cref="BlockGuiMessage"/>.
        /// </summary>
        public TimeSpan? BlockGuiTime { get; set; }
        /// <summary>
        /// Zpráva zobrazená uživateli po dobu blokování GUI.
        /// Zpráva může obsahovat více řádků, oddělené CrLf.
        /// První řádek bude zobrazen výrazně (jako titulek), další řádky standardně.
        /// Zpráva bude zobrazena pouze tehdy, když <see cref="BlockGuiTime"/> bude obsahovat čas timeoutu, bez něj je message nepoužitá.
        /// </summary>
        public string BlockGuiMessage { get; set; }
        /// <summary>
        /// Předpis pro akce, které na základě aktivace tohoto prvku má provést vrstva GUI.
        /// Lze deklarovat více než jednu akci.
        /// Lze potlačit volání servisní funkce aplikačního serveru : <see cref="GuiActionType.SuppressCallAppHost"/>.
        /// 
        /// </summary>
        public GuiActionType? GuiActions { get; set; }
        /// <summary>
        /// Jména akcí, které se mají provést, pokud <see cref="GuiActions"/> bude obsahovat <see cref="GuiActionType.RunInteractions"/>.
        /// Jde o seznam oddělený středníky nebo čárkami, jehož jednotlivé položky určují názvy tabulek Source a názvy interakcí <see cref="GuiGridInteraction"/>, které se na základě tohoto tlačítka mají provést.
        /// Název tabulky a název interakce je oddělen dvojtečkou (nebo tečkou);
        /// Název tabulky musí být FullName (například Data\pages\MainPage\mainPanel\GridCenter);
        /// interakce jako data jsou totiž definovány v rámci <see cref="GuiGrid.GridProperties"/>, v <see cref="GuiGridProperties.InteractionList"/>.
        /// Příklad: pokud toto tlačítko Toolbaru obsahuje <see cref="RunInteractionNames"/> = "GridLeft:SelectOperations", pak se provede:
        /// a) najde se tabulka (<see cref="GuiGrid"/>) s Name = "GridLeft", a
        /// b) provede se její interakce "SelectOperations".
        /// <para/>
        /// Za názvem interakce může být středník a parametr dané interakce. například: GridLeft:ShowColor:1,GridCenter:ShowColor:1
        /// <para/>
        /// Pokud bude požadována akce <see cref="GuiActions"/> : <see cref="GuiActionType.RunInteractions"/>, ale <see cref="RunInteractionNames"/>, 
        /// žádná interakce se neprovede.
        /// </summary>
        public string RunInteractionNames { get; set; }
        /// <summary>
        /// Zdrojové akce, které se mají po stisknutí tohoto tlačítka provést v rámci interakcí.
        /// Vyhodnocuje se pouze pokud v <see cref="GuiActions"/> je hodnota <see cref="GuiActionType.RunInteractions"/>.
        /// </summary>
        public SourceActionType? RunInteractionSource { get; set; }
        /// <summary>
        /// Vlastnosti pro TrackBar. 
        /// POužije se jen tehdy, když <see cref="ItemType"/> == <see cref="FunctionGlobalItemType.TrackBar"/>
        /// Výchozí hodnota je null.
        /// </summary>
        public GuiTrackBarSettings TrackBarSettings { get; set; }
    }
    /// <summary>
    /// Definice vlastností trackbaru
    /// </summary>
    public class GuiTrackBarSettings
    {
        /// <summary>
        /// Počet linek trackbaru
        /// </summary>
        public int? TrackLines { get; set; }
    }
    /// <summary>
    /// Akce, které má provést vrstva Gui po aktivaci tlačítka / funkce
    /// </summary>
    [Flags]
    public enum GuiActionType : UInt64
    {
        /// <summary>
        /// GUI nemá provádět žádnou akci, výchozí hodnota.
        /// </summary>
        None = 0,
        /// <summary>
        /// Zrušit všechny řádkové filtry pro všechny tabulky
        /// </summary>
        ResetAllRowFilters = 0x0000000000000010,
        /// <summary>
        /// Zrušit interaktivní filtry pro target tabulky
        /// </summary>
        ResetTargetInteractiveFilters = 0x0000000000000020,
        /// <summary>
        /// Zavolá interakce definované toolbarem v property <see cref="GuiToolbarItem.RunInteractionNames"/>, 
        /// předá do systému interakcí definici zdrojové akce <see cref="SourceActionType"/> z property <see cref="GuiToolbarItem.RunInteractionSource"/>
        /// </summary>
        RunInteractions = 0x0000000000000100,
        /// <summary>
        /// Aktivace této funkce NEBUDE volat funkci aplikačního serveru
        /// </summary>
        SuppressCallAppHost = 0x1000000000000000        // Nastaven bit 60
    }
    #endregion
    #region GuiContextMenuSet : Všechny položky všech Kontextových menu
    /// <summary>
    /// GuiContextMenuSet : Všechny položky všech Kontextových menu.
    /// Titulek menu se zadává do <see cref="GuiTextItem.Title"/>
    /// </summary>
    public sealed class GuiContextMenuSet : GuiTextItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiContextMenuSet()
        {
            this.Items = new List<GuiContextMenuItem>();
        }
        /// <summary>
        /// Barva pozadí menu
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Menu bude mít stín?
        /// </summary>
        public bool? DropShadowEnabled { get; set; }
        /// <summary>
        /// Průhlednost menu
        /// </summary>
        public float? Opacity { get; set; }
        /// <summary>
        /// Velikost ikon v menu; určuje velikost položek v menu
        /// </summary>
        public Size? ImageScalingSize { get; set; }
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
        /// Barva pozadí položky menu: má přednost před barvou menu pokud je zadána
        /// </summary>
        public Color? BackColor { get; set; }
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
        /// Pokud nebude zadáno číslo třídy, pak definice platí pro všechny třídy. 
        /// Jakmile bude zadáno alespoň jedno nebo více číslo třídy, pak bude platit definice jen pro zadané třídu.
        /// Před číslem třídy smí být písmeno C, nebo M nebo E.
        /// C značí, že definice platí pro všechny objekty třídy (Master i Entries), M platí jen pro Master a E pouze pro Entries.
        /// Lze tak definovat funkce platné jen pro hlavičkové prvky, anebo jen pro položky.
        /// Pokud písmeno není zadáno, platí definice pro celou třídu (jako by bylo zadáno písmeno C).
        /// <para/>
        /// Číslo třídy se testuje z grafického prvku z jeho <see cref="GuiGraphItem.ItemId"/> a z <see cref="GuiGraphItem.GroupId"/> a z <see cref="GuiGraphItem.DataId"/>.
        /// Jedna definice může obsahovat více výše popsaných prvků, oddělených středníkem.
        /// Definice může v rámci FullName obsahovat hvězdičku, která nahrazuje část FullName (nebo i celou hodnotu FullName). Hvězdičku lze použít namísto tříd, anebo se čísla tříd nemusí zadávat.
        /// <para/>
        /// Pokud chceme deklarovat nějakou funkci dostupnou v rámci celého grafu (tj. i v ploše grafu mimo konkrétní prvky), nebo i jinde v řádku mimo graf, 
        /// pak použijeme tříprvkovou definici (prvky oddělené dvojtečkou), která má na druhé pozici konstantní string <see cref="AREA_GRAF"/> nebo <see cref="AREA_ROW"/>.
        /// Zde jsou pak čísla tříd uváděna ve třetím prvku.
        /// <para/>
        /// Příklady celého textu:
        /// "Data\pageMain\mainPanel\workGrid:1190": funkce je dostupná pro hlavní stranu (pageMain), hlavní panel (mainPanel), pro jednu tabulku (workGrid), pro prvky třídy 1190;
        /// "Data\pageMain\*:1190": funkce je dostupná pro hlavní stranu (pageMain), pro všechny panely a tabulky, pro prvky třídy 1190;
        /// "Data\pageMain\*": funkce je dostupná pro hlavní stranu (pageMain), pro všechny panely a tabulky, pro prvky všech tříd;
        /// "Data\pageMain\*:1190,1815": funkce je dostupná pro hlavní stranu (pageMain), pro všechny panely a tabulky, pro prvky tříd 1190 a 1815;
        /// "Data\pageMain\mainPanel\workGrid:1190;Data\pageMain\*:1815": kombinace dvou deklarací
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
        /// <summary>
        /// Konstanta, určující že daná definice kontextové funkce se má použít pro plochu prvku grafu
        /// </summary>
        public const string AREA_ITEM = "Item";
        /// <summary>
        /// Konstanta, určující že daná definice kontextové funkce se má použít pro plochu grafu = tj. i mimo prvky grafu
        /// </summary>
        public const string AREA_GRAF = "Graf";
        /// <summary>
        /// Konstanta, určující že daná definice kontextové funkce se má použít pro plochu celého řádku = tj. i mimo graf
        /// </summary>
        public const string AREA_ROW = "Row";
        /// <summary>
        /// Prefix před číslo třídy, pro platnost definice pro celou třídu (Master + Entries)
        /// </summary>
        public const string CLASS_ALL = "C";
        /// <summary>
        /// Prefix před číslo třídy, pro platnost definice výhradně pro Master
        /// </summary>
        public const string CLASS_MASTER = "M";
        /// <summary>
        /// Prefix před číslo třídy, pro platnost definice výhradně pro Entries
        /// </summary>
        public const string CLASS_ENTRIES = "E";
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
    /// <para/>
    /// Obsahuje <see cref="Name"/>, <see cref="Parent"/>, <see cref="UserData"/>, <see cref="Childs"/>
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
        /// Objekt parenta, v němž je tento prvek umístěn.
        /// </summary>
        [PersistingEnabled(false)]
        public IGuiItem Parent { get { return this._Parent; } }
        /// <summary>
        /// Úložiště parenta
        /// </summary>
        private IGuiItem _Parent;
        /// <summary>
        /// Libovolná aplikační data, která neprochází serializací.
        /// Toto je prostor, který může využít aplikace k uložení svých dat nad rámec dat třídy, protože Gui třídy jsou sealed 
        /// a aplikace nemůže používat potomky základních tříd.
        /// </summary>
        [PersistingEnabled(false)]
        public object UserData { get; set; }
        /// <summary>
        /// Finalizace vnitřních dat objektu.
        /// Volá se těsně před vyhodnocením pole <see cref="Childs"/>, v procesu FillParentToChilds().
        /// Konkrétní instance nemusí provádět nic, anebo si může provést "rekalkulaci" svých dat.
        /// Tato metoda je volána vždy před odesláním dat Gui*.
        /// </summary>
        protected virtual void Finalise() { }
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
        /// <summary>
        /// Metoda vrátí nejbližího parenta daného typu. Může vrátit null. 
        /// Může vrátit sebe, pokud this je daného typu!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T GetParentOfType<T>() where T : class
        {
            IGuiItem item = this;
            Type type = typeof(T);
            for (int t = 0; t < 50; t++)
            {   // Jde jen o timeout...
                if (item.GetType() == type) return (item as T);
                item = item.Parent;
                if (item == null) break;
            }
            return null;
        }
        /// <summary>
        /// Kořenový objekt. Nikdy není null. Může být this.
        /// Má smysl použít až po proběhnutí metody <see cref="Finalise()"/>.
        /// </summary>
        protected IGuiItem Root
        {
            get
            {
                IGuiItem item = this;
                for (int t = 0; t < 50; t++)
                {   // Jde jen o timeout...
                    if (item.Parent == null) return item;
                    item = item.Parent;
                }
                return item;
            }
        }
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
                item.Finalise();
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
        #region Support pro serializaci - statické konvertory
        /// <summary>
        /// Metoda vrací Int32 hodnotu z dané buňky daného pole
        /// </summary>
        /// <param name="items"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int ConvertToInt32(string[] items, int index)
        {
            if (items == null || index < 0 || index >= items.Length || String.IsNullOrEmpty(items[index])) return 0;
            int value;
            if (!Int32.TryParse(items[index], out value)) return 0;
            return value;
        }
        /// <summary>
        /// Metoda vrací Int32? hodnotu z dané buňky daného pole
        /// </summary>
        /// <param name="items"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int? ConvertToInt32N(string[] items, int index)
        {
            if (items == null || index < 0 || index >= items.Length || String.IsNullOrEmpty(items[index])) return null;
            int value;
            if (!Int32.TryParse(items[index], out value)) return null;
            return value;
        }
        /// <summary>
        /// Metoda vrací Single hodnotu z dané buňky daného pole
        /// </summary>
        /// <param name="items"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static float ConvertToSingle(string[] items, int index)
        {
            if (items == null || index < 0 || index >= items.Length || String.IsNullOrEmpty(items[index])) return 0f;
            float value;
            if (!Single.TryParse(items[index], out value)) return 0f;
            return value;
        }
        /// <summary>
        /// Metoda vrací Single? hodnotu z dané buňky daného pole
        /// </summary>
        /// <param name="items"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static float? ConvertToSingleN(string[] items, int index)
        {
            if (items == null || index < 0 || index >= items.Length || String.IsNullOrEmpty(items[index])) return null;
            float value;
            if (!Single.TryParse(items[index], out value)) return null;
            return value;
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
        /// Member of interface IGuiBase : finalizace objektu před jeho odesláním.
        /// </summary>
        void IGuiItem.Finalise() { this.Finalise(); }
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
        /// Finalizace vnitřních dat objektu.
        /// Volá se těsně před vyhodnocením pole <see cref="Childs"/>, v procesu FillParentToChilds().
        /// Konkrétní instance nemusí provádět nic, anebo si může provést "rekalkulaci" svých dat.
        /// Tato metoda je volána vždy před odesláním dat Gui*.
        /// </summary>
        void Finalise();
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
        /// <summary>
        /// Obsahuje true, pokud this instance nemá žádná data o obrázku
        /// </summary>
        [PersistingEnabled(false)]
        public bool IsEmpty { get { return (String.IsNullOrEmpty(this.ImageFile) && this.ImageContent == null && this.Image == null); } }
        /// <summary>
        /// Empty obrázek: není null (takže se předává do GUI vrstvy), ale reprezentuje neexistující obrázek (takže se nevykresluje).
        /// Používá se při aktualizaci dat v <see cref="GuiResponse.AddItems"/>, když dřívější prvek grafu měl Image, ale nově jej mít nemá.
        /// </summary>
        public static GuiImage Empty { get { return new GuiImage(); } }
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
                return (text != null ? prefix + text : "");
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
    #region GuiVisualStyle : Třída pro definici vizuálního vzhledu grafického prvku
    /// <summary>
    /// GuiVisualStyle : Třída pro definici vizuálního vzhledu grafického prvku
    /// </summary>
    public class GuiVisualStyle : GuiBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiVisualStyle() { }

        /// <summary>
        /// Typ fontu.
        /// Konkrétní typ použitý na počítači je dán přiřazením fontu v rámci Windows na počítači, kde aplikace běží.
        /// </summary>
        public GuiFontSetType? FontType { get; set; }
        /// <summary>
        /// Relativní velikost fontu v procentech. Null = 100 = 100%
        /// </summary>
        public int? FontRelativeSize { get; set; }
        /// <summary>
        /// Font je Bold?
        /// </summary>
        public bool? FontBold { get; set; }
        /// <summary>
        /// Font je Italic?
        /// </summary>
        public bool? FontItalic { get; set; }
        /// <summary>
        /// Font je Underlined?
        /// </summary>
        public bool? FontUnderline { get; set; }

        /// <summary>
        /// Barva pozadí v prvku (řádek, buňka) pokud není Selected, a není to aktivní položka (řádek tabulky), prostě běžný prvek (řádek)
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Barva textu v prvku (řádek, buňka) pokud není Selected, a není to aktivní položka (řádek tabulky), prostě běžný prvek (řádek)
        /// </summary>
        public Color? TextColor { get; set; }
        /// <summary>
        /// Barva pozadí v prvku (řádek, buňka) pokud je Selected, a není to aktivní položka (řádek tabulky)
        /// </summary>
        public Color? SelectedBackColor { get; set; }
        /// <summary>
        /// Barva textu v prvku (řádek, buňka) pokud je Selected, a není to aktivní položka (řádek tabulky)
        /// </summary>
        public Color? SelectedTextColor { get; set; }
        /// <summary>
        /// Barva pozadí v prvku (řádek, buňka) pokud je tento prvek aktivní (řádek je vybraný) a v jeho controlu je focus.
        /// Po odchodu focusu z tohoto prvku je barva prvku změněna na 50% směrem k barvě BackColor nebo SelectedBackColor.
        /// </summary>
        public Color? ActiveBackColor { get; set; }
        /// <summary>
        /// Barva písma v prvku (řádek, buňka) pokud je tento prvek aktivní (řádek je vybraný) a v jeho controlu je focus.
        /// Po odchodu focusu z tohoto prvku je barva prvku změněna na 50% směrem k barvě TextColor nebo SelectedTextColor.
        /// </summary>
        public Color? ActiveTextColor { get; set; }
        /// <summary>
        /// Strany kreslené okolo buňky
        /// </summary>
        public GuiBorderSideType? GridLines { get; set; }
        /// <summary>
        /// Barva vodorovných linek
        /// </summary>
        public Color? HorizontalLineColor { get; set; }
        /// <summary>
        /// Barva svislých linek
        /// </summary>
        public Color? VerticalLineColor { get; set; }

        /// <summary>
        /// Vrátí shallow copy sebe sama
        /// </summary>
        /// <returns></returns>
        public GuiVisualStyle GetClone()
        {
            return this.MemberwiseClone() as GuiVisualStyle;
        }
    }
    /// <summary>
    /// Typ použitého fontu, reálný typ se odvozuje od aktuální sady na počítači
    /// </summary>
    public enum GuiFontSetType
    {
        /// <summary>Konkrétní typ písma</summary>
        DefaultFont = 0,
        /// <summary>Konkrétní typ písma</summary>
        DialogFont = 1,
        /// <summary>Konkrétní typ písma</summary>
        MenuFont = 2,
        /// <summary>Konkrétní typ písma</summary>
        CaptionFont = 3,
        /// <summary>Konkrétní typ písma</summary>
        IconTitleFont = 4,
        /// <summary>Konkrétní typ písma</summary>
        MessageBoxFont = 5,
        /// <summary>Konkrétní typ písma</summary>
        SmallCaptionFont = 6,
        /// <summary>Konkrétní typ písma</summary>
        StatusFont = 7,
        /// <summary>Konkrétní typ písma</summary>
        ExplicitFont = 8
    }
    /// <summary>
    /// Strany kreslené okolo prvku
    /// </summary>
    [Flags]
    public enum GuiBorderSideType
    {
        /// <summary>Žádná</summary>
        None = 0,
        /// <summary>Vlevo</summary>
        Left = 0x01,
        /// <summary>Vpravo</summary>
        Right = 0x02,
        /// <summary>Nahoře</summary>
        Top = 0x10,
        /// <summary>Dole</summary>
        Bottom = 0x20,
        /// <summary>Vodorovné = nahoře + dole</summary>
        Horizontal = Top | Bottom,
        /// <summary>Svislé = vlevo + vpravo</summary>
        Vertical = Left | Right,
        /// <summary>Všechny</summary>
        All = Horizontal | Vertical
    }
    #endregion
    #region GuiIdText : třída pro předání odkazu na záznam (GuiId) plus vizuální text
    /// <summary>
    /// GuiIdText : třída pro předání odkazu na záznam (GuiId) plus vizuální text.
    /// Instance třídy se běžně používá jako Value v tabulce, kde reprezentuje vztažený záznam.
    /// Její text je zobrazen s podtržením (vztah), a Ctrl + DoubleClick provede otevření daného záznamu.
    /// </summary>
    public class GuiIdText : IXmlSerializer
    {
        #region Konstruktor, standardní properties
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiIdText()
        { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text;
            // nehodící se: "GuiId: " + (this.GuiId != null ? this.GuiId.ToString() : "Null") + "; Text: " + (this.Text != null ? this.Text : "Null");
        }
        /// <summary>
        /// Identifikátor záznamu
        /// </summary>
        public GuiId GuiId { get; set; }
        /// <summary>
        /// Vizuální text (reference, název, jakýkoli jiný atribut záznamu)
        /// </summary>
        public string Text { get; set; }
        #endregion
        #region Serializace
        /// <summary>
        /// Obsahuje textové vyjádření zdejších dat
        /// </summary>
        public string Name
        {   // Name = "GuiId==Text" nebo "GuiId=."    ...   kde GuiId je: "1188:123456" + optional Entry: ":1234"
            get { return (this.GuiId != null ? ((IXmlSerializer)this.GuiId).XmlSerialData : "") + "=" + (this.Text != null ? "=" + this.Text : "."); }
            private set
            {
                GuiId guiId = null;
                string text = null;
                if (!String.IsNullOrEmpty(value) && value.Contains("="))
                {
                    int length = value.Length;
                    int index = value.IndexOf("=");
                    if (index > 0 && index <= (length - 2))
                    {   // Za rovnítkem musí být nejméně jeden znak!
                        // 1. Před rovnítkem je GuiId:
                        guiId = new GuiId();
                        ((IXmlSerializer)guiId).XmlSerialData = value.Substring(0, index);
                        // 2. Za rovnítkem je vždy povinně znak:
                        //  a) ještě jedno rovnítko: a za ním je text nebo prázdná string (=už nic)
                        //  b) tečka: text je NULL:
                        if (value[index + 1] == '=')
                            text = (index == (length - 2) ? "" : value.Substring(index + 2));
                    }
                }
                this.GuiId = guiId;
                this.Text = text;
            }
        }
        /// <summary>
        /// Tato property má obsahovat (get vrací, set akceptuje) XML data z celého aktuálního objektu.
        /// </summary>
        string IXmlSerializer.XmlSerialData { get { return this.Name; } set { this.Name = value; } }
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
        #region Konstruktor, standardní properties
        /// <summary>
        /// Konstruktor bez parametrů musí existovat kvůli deserialiaci.
        /// </summary>
        public GuiId() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="classId">Číslo třídy</param>
        /// <param name="recordId">Číslo záznamu</param>
        public GuiId(int classId, int recordId)
        {
            this.ClassId = classId;
            this.RecordId = recordId;
            this.EntryId = null;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="classId">Číslo třídy</param>
        /// <param name="recordId">Číslo záznamu (MasterId)</param>
        /// <param name="entryId">Číslo položky (EntryId)</param>
        public GuiId(int classId, int recordId, int entryId)
        {
            this.ClassId = classId;
            this.RecordId = recordId;
            this.EntryId = entryId;
        }
        /// <summary>
        /// Číslo třídy
        /// </summary>
        public int ClassId { get; private set; }
        /// <summary>
        /// Číslo záznamu = Master (subjekt, nonsubjekt).
        /// U položkového <see cref="GuiId"/> může být 0, nebo může obsahovat číslo Master záznamu.
        /// </summary>
        public int RecordId { get; private set; }
        /// <summary>
        /// Číslo položky = Entry (objekt).
        /// Pokud je null (=nemá hodnotu), pak jde o ID Master záznamu.
        /// </summary>
        public int? EntryId { get; private set; }
        /// <summary>
        /// true pro prázdný ID (kdy <see cref="ClassId"/> i <see cref="RecordId"/> == 0 a <see cref="EntryId"/> nemá hodnotu)
        /// </summary>
        public bool IsEmpty { get { return (this.ClassId == 0 && this.RecordId == 0 && !this.EntryId.HasValue); } }
        /// <summary>
        /// Vrátí new instanci <see cref="GuiId"/>, která je <see cref="IsEmpty"/>.
        /// Proč? Třeba pro klíč v Dictionary, který nesmí být null.
        /// </summary>
        public static GuiId Empty { get { return new GuiId(0, 0); } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "C:" + this.ClassId.ToString() + "; R:" + this.RecordId.ToString() + (this.EntryId.HasValue ? "; E:" + this.EntryId.Value.ToString() : "");
        }
        #endregion
        #region HashCode, Equals, ==, !=
        /// <summary>
        /// Vrátí HashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (!this._HashCode.HasValue)
                this._HashCode = this.ClassId.GetHashCode() ^ this.RecordId.GetHashCode() ^ (this.EntryId.HasValue ? this.EntryId.Value.GetHashCode() : 0);
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
            return (a.Name == b.Name);
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
        #endregion
        #region Serializace
        /// <summary>
        /// Obsahuje textové vyjádření zdejších dat
        /// </summary>
        public string Name
        {
            get { return this.ClassId.ToString() + ":" + this.RecordId.ToString() + (this.EntryId.HasValue ? ":" + this.EntryId.Value.ToString() : ""); }
            private set
            {
                int classId = 0;
                int recordId = 0;
                int entryId = 0;
                bool isValid = false;
                bool isEntry = false;
                if (!String.IsNullOrEmpty(value) && value.Contains(":"))
                {
                    string[] items = value.Split(':');
                    isValid = (Int32.TryParse(items[0], out classId) && Int32.TryParse(items[1], out recordId));
                    if (isValid && items.Length > 2)
                        isEntry = Int32.TryParse(items[2], out entryId);
                }
                this.ClassId = (isValid ? classId : 0);
                this.RecordId = (isValid ? recordId : 0);
                this.EntryId = (isEntry ? (int?)entryId : (int?)null);
            }
        }
        /// <summary>
        /// Tato property má obsahovat (get vrací, set akceptuje) XML data z celého aktuálního objektu.
        /// </summary>
        string IXmlSerializer.XmlSerialData { get { return this.Name; } set { this.Name = value; } }
        #endregion
    }
    #endregion
    #region GuiRange + GuiTimeRange + GuiSingleRange: rozsah { Begin ÷ End } dvou hodnot stejného datového typu
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
        protected override string GetSerial(DateTime value) { return Convertor.DateTimeToSerial(value); }
        /// <summary>
        /// Vrátí deserializovanou typovou hodnotu ze serializované formy
        /// </summary>
        /// <param name="serial"></param>
        /// <returns></returns>
        protected override DateTime GetValue(string serial) { return (DateTime)Convertor.SerialToDateTime(serial); }
        #endregion
    }
    /// <summary>
    /// GuiDoubleRange : rozsah { Begin ÷ End } dvou hodnot typu <see cref="Double"/>
    /// </summary>
    public class GuiDoubleRange : GuiRange<Double>, IXmlSerializer
    {
        #region Konstruktory
        /// <summary>
        /// Bezparametrický konstruktor, pro XML serializaci (Persistor)
        /// </summary>
        protected GuiDoubleRange() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public GuiDoubleRange(Double begin, Double end) : base(begin, end) { }
        #endregion
        #region Abstract overrides
        /// <summary>
        /// Vrátí serializovanou formu dané typové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override string GetSerial(Double value) { return Convertor.DoubleToString(value); }
        /// <summary>
        /// Vrátí deserializovanou typovou hodnotu ze serializované formy
        /// </summary>
        /// <param name="serial"></param>
        /// <returns></returns>
        protected override Double GetValue(string serial) { return (Double)Convertor.StringToDouble(serial); }
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
    /// GuiInt32Range : rozsah { Begin ÷ End } dvou hodnot typu <see cref="Int32"/>
    /// </summary>
    public class GuiInt32Range : GuiRange<Int32>, IXmlSerializer
    {
        #region Konstruktory
        /// <summary>
        /// Bezparametrický konstruktor, pro XML serializaci (Persistor)
        /// </summary>
        protected GuiInt32Range() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public GuiInt32Range(Int32 begin, Int32 end) : base(begin, end) { }
        #endregion
        #region Abstract overrides
        /// <summary>
        /// Vrátí serializovanou formu dané typové hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override string GetSerial(Int32 value) { return Convertor.Int32ToString(value); }
        /// <summary>
        /// Vrátí deserializovanou typovou hodnotu ze serializované formy
        /// </summary>
        /// <param name="serial"></param>
        /// <returns></returns>
        protected override Int32 GetValue(string serial) { return (Int32)Convertor.StringToInt32(serial); }
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
                    return text + ((this.ContextMenu != null && this.ContextMenu.ContextMenuItem != null) ? "; ContextItem: " + this.ContextMenu.ContextMenuItem.ToString() : "; ContextMenuItem is null");
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
        /// Data pro požadavek KeyPress
        /// </summary>
        public GuiRequestKeyPress KeyPress { get; set; }
        /// <summary>
        /// Pole záznamů k otevření
        /// </summary>
        public GuiId[] RecordsToOpen { get; set; }
        /// <summary>
        /// Položka Toolbaru, která vyvolala akci
        /// </summary>
        public GuiToolbarItem ToolbarItem { get; set; }
        /// <summary>
        /// Data pro řízení akce vyvolané kliknutím na kontextové menu.
        /// Data obsahují: určení funkce <see cref="GuiContextMenuRunArgs.ContextMenuItem"/>,
        /// určení místa (prvku) kde se kliklo <see cref="GuiContextMenuRunArgs.ContextItemId"/>,
        /// čas v grafu (pokud to bylo v časovém grafu) <see cref="GuiContextMenuRunArgs.ClickTime"/>.
        /// Další informace jsou uloženy v objektu popisujícím stav pluginu = v <see cref="CurrentState"/>.
        /// </summary>
        public GuiContextMenuRunArgs ContextMenu { get; set; }
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
        /// Informace o přemísťování řádků na určitý cíl (Drag and Move pro sadu řádků tabulky)
        /// </summary>
        public GuiRequestRowDragMove RowDragMove { get; set; }
        /// <summary>
        /// Data pro command <see cref="COMMAND_TimeChange"/>
        /// </summary>
        public GuiRequestTimeAxisChange TimeAxisChange { get; set; }
        /// <summary>
        /// Aktuální stav okna WorkScheduler
        /// </summary>
        public GuiRequestCurrentState CurrentState { get; set; }
        #region Konstanty - commandy
        /// <summary>
        /// Uživatel stiskl některou ze zadaných aktivních kláves.
        /// Objekt <see cref="GuiRequest"/> nese data o klávese a o objektu v property <see cref="GuiRequest.KeyPress"/>.
        /// </summary>
        public const string COMMAND_KeyPress = "KeyPress";
        /// <summary>
        /// Uživatel provedl změnu na časové ose.
        /// Tento command se posílá na základě základního nastavení dle <see cref=""/>
        /// Objekt <see cref="GuiRequest"/> nese data o klávese a o objektu v property <see cref="GuiRequest.KeyPress"/>.
        /// </summary>
        public const string COMMAND_TimeChange = "TimeChange";
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
        /// Byl proveden Drag and Drop pro některé řádky na některé místo.
        /// Objekt <see cref="GuiRequest"/> nese informaci o konkrétních zdrojových řádcích i o cílovém prvku v property <see cref="GuiRequest.RowDragMove"/>,
        /// Kompletní údaje o stavu GUI jsou v <see cref="GuiRequest.CurrentState"/>.
        /// Aplikační servisní funkce může zareagovat úpravou svých dat, a případně přeplánováním.
        /// </summary>
        public const string COMMAND_RowDragDrop = "RowDragDrop";
        /// <summary>
        /// Test před zavřením okna.
        /// Nepředávají se žádná upřesňující data.
        /// Jako odpověď se očekává <see cref="GuiResponse.Dialog"/>: pokud bude neprázdné, jde o dotaz před ukončením. 
        /// V tom případě se zobrazí dialog podle <see cref="GuiResponse.Dialog"/>.
        /// Message by měl být zhruba: "Data nejsou uložena, chcete je uložit?";
        /// Volby dialogu by měly obsahovat Yes nebo OK = to vede k uložení dat (jeho odsouhlasení pak zavolá COMMAND <see cref="COMMAND_SaveBeforeCloseWindow"/>).
        /// Pokud bude dialog obsahovat No, pak tato volba data neuloží a okno se zavře.
        /// Pokud bude dialog obsahovat Cancel, pak tato volba data neuloží ale NEZAVŘE okno.
        /// Pokud bude <see cref="GuiResponse.Dialog"/> prázdné, dialog nebude, okno se zavře bez ukládání dat.
        /// Při zavření okna se odešle command <see cref="COMMAND_CloseWindow"/>.
        /// </summary>
        public const string COMMAND_QueryCloseWindow = "QueryCloseWindow";
        /// <summary>
        /// Uložení dat před zavřením okna.
        /// Nepředávají se žádná upřesňující data.
        /// Tento command je volán před zavřením okna, po dialogu který deklaroval výsledek commandu <see cref="COMMAND_QueryCloseWindow"/>, po odsouhlasení uložení dat uživatelem.
        /// Tato funkce ukládá data do databáze. Je Synchronní, její TimeOut je 90 sekund.
        /// Pokud v této akci dojde k chybě, vrací se result = Failure.
        /// V této situaci Plugin reaguje na response, umožní provést dialog s uživatelem, například: "Při ukládání dat došlo k chybě, přejete si přesto zavřít aplikaci?";
        /// Pokud uživatel odpoví ANO nebo IGNORE (nebo pokud dialog nebude žádný), pak se okno zavře i po chybě.
        /// Jiné odpovědi nechají po chybě okno aplikace otevřené.
        /// </summary>
        public const string COMMAND_SaveBeforeCloseWindow = "SaveBeforeCloseWindow";
        /// <summary>
        /// Zavírá se okno už doopravdy.
        /// Nepředávají se žádná upřesňující data.
        /// Vyšší aplikace si má zahodit svoje data svázaná s tímto pluginem.
        /// </summary>
        public const string COMMAND_CloseWindow = "CloseWindow";
        #endregion
    }
    /// <summary>
    /// Data pro požadavek KeyPress
    /// </summary>
    public class GuiRequestKeyPress
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiRequestKeyPress()
        { }
        /// <summary>
        /// Stisknutá klávesa plus modifikátory (Shift, Control, Alt)
        /// </summary>
        public Keys KeyData { get; set; }
        /// <summary>
        /// Fullname objektu, kde došlo ke stisknutí klávesy (typicky Tabulka)
        /// </summary>
        public string ObjectFullName { get; set; }
        /// <summary>
        /// ID objektu, kde došlo ke stisknutí klávesy (typicky ID řádku)
        /// </summary>
        public GuiId ObjectGuiId { get; set; }
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
        /// V této property se nacházejí všechny prvky jedné skupiny <see cref="GuiGraphItem.GroupId"/>, neboť přesouvání se provádí vždy pro celé skupiny.
        /// Pokud prvek grafu nepatří do žádné skupiny (jeho <see cref="GuiGraphItem.GroupId"/> je null), pak tvoří svoji vlastní soukromou skupinu, a přesouvá se sám.
        /// </summary>
        public GuiGridItemId[] MoveItems { get; set; }
        /// <summary>
        /// Zdrojový řádek, před přesouváním.
        /// </summary>
        public GuiGridRowId SourceRowId { get; set; }
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
        public GuiGridRowId TargetRowId { get; set; }
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
            string text = "ResizeItems: ";
            if (this.ResizeItems != null && this.ResizeItems.Length > 0)
                text += this.ResizeItems[0].ToString();
            else
                text += "{Null}";

            if (this.SourceHeight != null && this.TargetHeight != null && this.SourceHeight != this.TargetHeight)
                text += "; Resize Height; From: " + this.SourceHeight.ToString() + "; To: " + this.TargetHeight.ToString();
            if (this.SourceTime != null && this.TargetTime != null && this.SourceTime != this.TargetTime)
                text += "; Resize Time; From: " + this.SourceTime.ToString() + "; To: " + this.TargetTime.ToString();
            return text;
        }
        /// <summary>
        /// Prvek, kterého se Resize týká. Jde o prvky jedné grupy.
        /// </summary>
        public GuiGridItemId[] ResizeItems { get; set; }
        /// <summary>
        /// Zdrojový řádek, kde probíhá přesouvání.
        /// </summary>
        public GuiId SourceRow { get; set; }
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
    /// Informace o přemísťování řádků na určitý cíl (Drag and Move pro sadu řádků tabulky)
    /// </summary>
    public class GuiRequestRowDragMove
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiRequestRowDragMove()
        { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "RowDragMove: ";
            if (this.SourceRows != null && this.SourceRows.Length > 0)
                text += "SourceRows: " + this.SourceRows[0].ToString();
            else
                text += "SourceRows: {Null}";

            text += "; TargetRow: " + ((this.TargetRow != null) ? this.TargetRow.ToString() : "{Null}");
            text += "; TargetTime: " + ((this.TargetTime.HasValue) ? this.TargetTime.ToString() : "{Null}");

            return text;
        }
        /// <summary>
        /// Zdrojové řádky, které se pohybují = ty byly vybrány, a s nimi se pohybuje někam...
        /// </summary>
        public GuiGridRowId[] SourceRows { get; set; }
        /// <summary>
        /// Cílový řádek, kam byly zdrojové řádky přemístěny a kde byla uvolněna myš = cíl
        /// </summary>
        public GuiGridRowId TargetRow { get; set; }
        /// <summary>
        /// Cílový konkrétní prvek grafu, kam jsou zdrojové řádky přemístěny.
        /// Aplikace může pracovat s tímto konkrétním prvkem, anebo z něj může odvodit celou grupu.
        /// Pokud není cílem přetahování konkrétní prvek, ale grupa (= skupina prvků), a uživatel tedy ukázal myší do prostoru mezi prvky (=spojovací linie),
        /// pak v této property <see cref="TargetItem"/> je null, a celá skupina je uvedena v <see cref="TargetGroup"/>.
        /// </summary>
        public GuiGridItemId TargetItem { get; set; }
        /// <summary>
        /// Cílová skupina prvků grafu, kam jsou zdrojové řádky přemístěny.
        /// V grafu se vyskytuje více prvků jedné grupy, a mezi nimi je zobrazena spojovací linie. A právě na tu linii ukázal uživatel jako na cíl při přetahování.
        /// Pokud by uživatel ukázal (jako na cíl) na konkrétní prvek, pak zde bude null a prvek bude uveden v <see cref="TargetItem"/>.
        /// </summary>
        public GuiGridItemId[] TargetGroup { get; set; }
        /// <summary>
        /// Čas v rámci grafu, kam byly řádky umístěny. Zde je čas zaokrouhlený na pixely.
        /// </summary>
        public DateTime? TargetTime { get; set; }
        /// <summary>
        /// Čas v rámci grafu, kam byly řádky umístěny. Zde je čas zaokrouhlený na malé dílky na časové ose.
        /// </summary>
        public DateTime? TargetTimeRound { get; set; }
    }
    /// <summary>
    /// Informace o změně hodnoty na časové ose
    /// </summary>
    public class GuiRequestTimeAxisChange
    {
        public GuiTimeRange TimeRangeVisible { get; set; }
        public GuiTimeRange TimeRangeEnlarged { get; set; }
        public GuiTimeRange[] TimeRangeKnown { get; set; }
    }
    /// <summary>
    /// Informace o stavu při kliknutí na kontextové menu
    /// </summary>
    public class GuiContextMenuRunArgs
    {
        /// <summary>
        /// Kontextová funkce, která vyvolala akci
        /// </summary>
        public GuiContextMenuItem ContextMenuItem { get; set; }
        /// <summary>
        /// Položka, na které bylo kliknuto.
        /// Může to být prvek grafu (pak má vyplněný údaj <see cref="GuiGridItemId.ItemId"/> a čas <see cref="ClickTime"/>), 
        /// nebo to může být graf mimo prvky (pak má vyplněn údaj <see cref="GuiGridRowId.RowId"/> a čas <see cref="ClickTime"/>),
        /// anebo to může být řádek mimo graf (pak má vyplněn pouze řádek <see cref="GuiGridRowId.RowId"/>, ale čas <see cref="ClickTime"/> je null).
        /// </summary>
        public GuiGridItemId ContextItemId { get; set; }
        /// <summary>
        /// Přesná časová pozice, kam bylo kliknuto.
        /// Je vyplněno tehdy, když je kliknuto na prvek grafu nebo na plochu grafu, ale je null pokud je kliknuto na řádek mimo prostor grafu.
        /// </summary>
        public DateTime? ClickTime { get; set; }
    }
    /// <summary>
    /// GuiCurrentState : stav okna Scheduleru v době události.
    /// Popisuje mj. vybrané položky grafů a vybrané řádky tabulek.
    /// Neobsahuje informace o aktuální akci, ty jsou přidány do specifických properties přímo do instance třídy <see cref="GuiRequest"/>.
    /// </summary>
    public class GuiRequestCurrentState
    {
        /// <summary>
        /// Aktuálně zobrazení záložka s daty (<see cref="GuiPage"/>).FullName.
        /// Plugin může nabízet jednu nebo více stránek (záložek) s daty, zde je uvedeno jméno té právě aktivní.
        /// </summary>
        public string ActivePage { get; set; }
        /// <summary>
        /// Aktuální hodnota časové osy, přesně rovna viditelné oblasti
        /// </summary>
        public GuiTimeRange TimeAxisValue { get; set; }
        /// <summary>
        /// Hodnota <see cref="TimeAxisValue"/>, rozšířená koeficientem <see cref="GuiProperties.TimeChangeSendEnlargement"/>.
        /// Tato property je naplněna pouze tehdy, když je předáván command <see cref="GuiRequest.COMMAND_TimeChange"/>.
        /// Vyjadřuje časový úsek, jehož obsah požadujeme načíst - aby mohl být zobrazen On-Demand přo pohybu na časové ose.
        /// </summary>
        public GuiTimeRange TimeAxisEnlargedValue { get; set; }
        /// <summary>
        /// Aktivní řádek, je nanejvýše jeden
        /// </summary>
        public GuiGridRowId ActiveRow { get; set; }
        /// <summary>
        /// Aktuálně označené (IsChecked) řádky tabulek v aktuálním okně.
        /// Řádky tabulek lze interaktivně označovat ikonkou.
        /// V této property jsou uvedeny ID řádků, ze všech tabulek které obsahují nějaké řádky IsChecked.
        /// bay bylo možno v tabulce označit řádky, musí mít tabulka nastaveno <see cref="GuiDataTable.RowCheckEnabled"/> = true
        /// </summary>
        public GuiGridRowId[] CheckedRows { get; set; }
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
        /// <see cref="GuiId"/> prvku grafu, jeho GroupId, pochází z <see cref="GuiGraphItem.GroupId"/>
        /// </summary>
        public GuiId GroupId { get; set; }
        /// <summary>
        /// <see cref="GuiId"/> prvku grafu, jeho ItemId, pochází z <see cref="GuiGraphItem.ItemId"/>
        /// </summary>
        public GuiId ItemId { get; set; }
        /// <summary>
        /// <see cref="GuiId"/> prvku grafu, jeho DataId, pochází z <see cref="GuiGraphItem.DataId"/>
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
        /// <see cref="GuiId"/> řádku tabulky, pochází z prvního sloupce tabulky <see cref="GuiGrid.RowTable"/>, v kombinaci s číslem třídy v properties sloupce [0]
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
        /// Definice dialogu s uživatelem po doběhnutí požadavku, null = bez dialogu
        /// </summary>
        public GuiDialog Dialog { get; set; }
        /// <summary>
        /// Data potřebná pro ukládání dat při zavírání okna.
        /// Měla by být naplněna v odpovědi na command <see cref="GuiRequest.COMMAND_QueryCloseWindow"/> (tj. dotaz před zavřením okna). Jindy nemají význam.
        /// </summary>
        public GuiSaveData CloseSaveData { get; set; }
        #endregion
        #region Data, která se promítají do GUI: Common, ToolbarItems, TimeAxisValue, RemoveItems, AddItems
        /// <summary>
        /// Společné jednoduché proměnné
        /// </summary>
        public GuiResponseCommon Common { get; set; }
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
        public List<GuiToolbarItem> ToolbarItems { get; set; }
        /// <summary>
        /// Požadovaná hodnota časové osy. 
        /// Bude aplikována, pokud není null.
        /// Hodnota bude vždy zarovnána do <see cref="GuiProperties.TotalTimeRange"/>
        /// </summary>
        public GuiTimeRange TimeAxisValue { get; set; }
        /// <summary>
        /// Tento časový úsek je pokrytý daty předanými v této <see cref="GuiResponse"/>, a proto se považuje za známý.
        /// Nemusí být naplněno, pokud předaná data pokrývají přesně úsek <see cref="GuiRequestCurrentState.TimeAxisEnlargedValue"/> (tento úsek se považuje za naplněný automaticky).
        /// </summary>
        public GuiTimeRange TimeAxisFilledValue { get; set; }
        /// <summary>
        /// Pole řádků v tabulkách, které se mají aktualizovat.
        /// Záznamy obsahují kompletní klíč řádku (=tabulka + RowId), a data řádku pro aktualizaci, anebo null pro smazání řádku.
        /// <para/>
        /// Výchozí hodnota této property je null.
        /// </summary>
        public List<GuiRefreshRow> RefreshRows { get; set; }
        /// <summary>
        /// Pole grafů, které se mají aktualizovat.
        /// Záznamy obsahují kompletní klíč řádku grafu (=tabulka + RowId), a data grafu pro aktualizaci, anebo null pro smazání grafu (?).
        /// <para/>
        /// Výchozí hodnota této property je null.
        /// </summary>
        public List<GuiRefreshGraph> RefreshGraphs { get; set; }
        /// <summary>
        /// Pole prvků grafů, které se mají aktualizovat.
        /// Záznamy obsahují kompletní klíč prvku grafu (=tabulka + RowId + ItemId), a data prvku grafu pro aktualizaci, anebo null pro smazání prvku grafu.
        /// <para/>
        /// Výchozí hodnota této property je null.
        /// </summary>
        public List<GuiRefreshGraphItem> RefreshGraphItems { get; set; }
        /// <summary>
        /// Pole řádků tabulek, které požadujeme aby byly Expanded.
        /// Používá se pro tabulky, které obsahují TreeView.
        /// Řádky v tomto seznamu uvedené budou Expanded = budou zobrazovat svoje Childs. 
        /// Současně budou Expanded všechny jejich Parent řádky, aby daný řádek byl viditelný.
        /// <para/>
        /// Výchozí hodnota této property je null.
        /// </summary>
        public List<GuiGridRowId> ExpandRows { get; set; }
        /// <summary>
        /// Pole změn linků.
        /// Link, který má být vyřazen, bude mít nastaveno <see cref="GuiGraphLink.LinkType"/> = null, a musí mít řádně naplněny obě strany vztahu (Prev i Next).
        /// Obecně: všechny linky (které mají řádně naplněny obě strany vztahu (Prev i Next)) budou z grafu nejprve odebrány, 
        /// a následně do grafu budou přidány ty linky, které mají nastaven <see cref="GuiGraphLink.LinkType"/>.
        /// </summary>
        public List<GuiResponseGraphLink> ChangeLinks { get; set; }
        #endregion
        #region Statické konstruktory: Success, Warning, Error
        /// <summary>
        /// Statický konstruktor, který vrací new instanci <see cref="GuiResponse"/>, kde <see cref="GuiResponse.ResponseState"/> = <see cref="GuiResponseState.Success"/>.
        /// </summary>
        public static GuiResponse Success() { return new GuiResponse() { ResponseState = GuiResponseState.Success }; }
        /// <summary>
        /// Statický konstruktor, který vrací new instanci <see cref="GuiResponse"/>, kde <see cref="GuiResponse.ResponseState"/> = <see cref="GuiResponseState.Success"/>.
        /// </summary>
        public static GuiResponse Success(string dialogMessage)
        {
            GuiResponse guiResponse = Success();
            guiResponse.Dialog = GuiDialog.Info(dialogMessage);
            return guiResponse;
        }

        /// <summary>
        /// Statický konstruktor, který vrací new instanci <see cref="GuiResponse"/>, kde <see cref="GuiResponse.ResponseState"/> = <see cref="GuiResponseState.Warning"/>, a nastaví danou zprávu do <see cref="Dialog"/>.
        /// </summary>
        public static GuiResponse Warning(string message) { return new GuiResponse() { ResponseState = GuiResponseState.Warning, Dialog = new GuiDialog() { Message = message } }; }
        /// <summary>
        /// Statický konstruktor, který vrací new instanci <see cref="GuiResponse"/>, kde <see cref="GuiResponse.ResponseState"/> = <see cref="GuiResponseState.Error"/>, a nastaví danou zprávu do <see cref="Dialog"/>.
        /// </summary>
        public static GuiResponse Error(string message) { return new GuiResponse() { ResponseState = GuiResponseState.Error, Dialog = new GuiDialog() { Message = message } }; }
        #endregion
    }
    /// <summary>
    /// GuiDialog : třída, která nese informace o dialogu
    /// </summary>
    public class GuiDialog
    {
        #region Statické konstruktory
        /// <summary>
        /// Vrací objekt <see cref="GuiDialog"/>, nastavený na oznámení s daným textem, tlačítko OK a ikona INFO
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static GuiDialog Info(string message)
        {
            GuiDialog guiDialog = new GuiDialog()
            {
                Message = message,
                Buttons = GuiDialogButtons.Ok,
                Icon = DialogIconInfo
            };
            return guiDialog;
        }
        /// <summary>
        /// Vrací objekt <see cref="GuiDialog"/>, nastavený na oznámení s daným textem, tlačítko OK a ikona WARNING
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static GuiDialog Warning(string message)
        {
            GuiDialog guiDialog = new GuiDialog()
            {
                Message = message,
                Buttons = GuiDialogButtons.Ok,
                Icon = DialogIconWarning
            };
            return guiDialog;
        }
        /// <summary>
        /// Vrací objekt <see cref="GuiDialog"/>, nastavený na oznámení s daným textem, tlačítko OK a ikona ERROR
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static GuiDialog Error(string message)
        {
            GuiDialog guiDialog = new GuiDialog()
            {
                Message = message,
                Buttons = GuiDialogButtons.Ok,
                Icon = DialogIconError
            };
            return guiDialog;
        }
        /// <summary>
        /// Název ikony zobrazované jako Dialog.Info
        /// </summary>
        public const string DialogIconInfo = Resources.Images.Status.DialogInformation3Png;
        /// <summary>
        /// Název ikony zobrazované jako Dialog.Info
        /// </summary>
        public const string DialogIconWarning = Resources.Images.Status.DialogWarning3Png;
        /// <summary>
        /// Název ikony zobrazované jako Dialog.Info
        /// </summary>
        public const string DialogIconError = Resources.Images.Status.DialogError4Png;
        /// <summary>
        /// Název ikony zobrazované jako Dialog.Question
        /// </summary>
        public const string DialogIconQuestion = Resources.Images.Status.DialogQuestion2Png;
        #endregion
        #region Data dialogu
        /// <summary>
        /// Titulek okna, defaultní je název okna <see cref="GuiProperties.PluginFormTitle"/>
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Ikona u informace, nezadáno = žádná
        /// </summary>
        public GuiImage Icon { get; set; }
        /// <summary>
        /// Textová zpráva uživateli.
        /// Používá se například při testu na zavření okna WorkScheduleru, 
        /// obsahuje typicky: "Data jsou změněna. Přijdete o ně. Zavřít?"
        /// Anebo se plní při stavu Warning nebo Error.
        /// <para/>
        /// Pokud je prázdné, pak nebude žádný dialog, i když by jiné hodnoty byly naplněny.
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Možnosti dialogu s uživatelem - nabízená tlačítka.
        /// Používá se například při testu na zavření okna WorkScheduleru, pro <see cref="Message"/> obsahuje hodnoty <see cref="GuiDialogButtons.YesNo"/>.
        /// Pokud bude zadána zpráva <see cref="Message"/>, ale tlačítka <see cref="Buttons"/> budou <see cref="GuiDialogButtons.None"/>, pak se zobrazí tlačítko OK.
        /// </summary>
        public GuiDialogButtons Buttons { get; set; }
        /// <summary>
        /// Obsahuje true, pokud this instance NEobsahuje data pro dialog
        /// </summary>
        public bool IsEmpty { get { return String.IsNullOrEmpty(this.Message); } }
        #endregion
    }
    /// <summary>
    /// GuiSaveData : třída, která předává informace o ukládání dat před zavřením okna.
    /// Její obsah se očekává v <see cref="GuiResponse"/> tehdy, kdy jde o odpověď na command <see cref="GuiRequest.COMMAND_QueryCloseWindow"/> (tj. dotaz před zavřením okna).
    /// </summary>
    public class GuiSaveData
    {
        #region Konfigurace ukládání při zavření pluginu
        /// <summary>
        /// Pokud zde bude true, pak nemusí být deklarován žádný dialog v <see cref="GuiResponse.Dialog"/>,
        /// a ukládání dat bude spuštěno bez dalších podmínek.
        /// Pokud zde bude false (výchozí stav), pak záleží na definovaném dialogu. 
        /// Ukládání dat bude provedeno tehdy, když bude proveden dialog 
        /// a odpověď na něj bude Yes nebo Ok.
        /// <para/>
        /// Ukládání dat se provede commandem <see cref="GuiRequest.COMMAND_SaveBeforeCloseWindow"/>.
        /// </summary>
        public bool AutoSave { get; set; }
        /// <summary>
        /// Pokud bude prováděno ukládání dat, lze v této property definovat Timeout této funkce.
        /// Pokud nebude deklarován TimeOut, pak po dobu ukládání dat nebude blokováno GUI.
        /// Pokud je zde nějaký (kladný) čas, pak po tuto dobu bude GUI okna blokováno, do doby doběhnutí funkce nebo do doběhnutí tohoto Timeoutu.
        /// Po dobu blokování může být zobrazena hláška <see cref="BlockGuiMessage"/>.
        /// </summary>
        public TimeSpan? BlockGuiTime { get; set; }
        /// <summary>
        /// Zpráva zobrazená uživateli po dobu blokování GUI.
        /// Zpráva může obsahovat více řádků, oddělené CrLf.
        /// První řádek bude zobrazen výrazně (jako titulek), další řádky standardně.
        /// Zpráva bude zobrazena pouze tehdy, když <see cref="BlockGuiTime"/> bude obsahovat čas timeoutu, bez něj je message nepoužitá.
        /// </summary>
        public string BlockGuiMessage { get; set; }
        #endregion
    }
    /// <summary>
    /// Společné hodnoty v odpovědi
    /// </summary>
    public class GuiResponseCommon
    {
        #region Obecné vlastnosti v odpovědi
        /// <summary>
        /// Zrušit všechny zobrazené linky.
        /// Pokud existují nějaké prvky, které jsou Selected, obnocí se Linky podle nich (pokud nebudou vlivem <see cref="ClearSelected"/> odselectovány)
        /// </summary>
        public bool ClearLinks { get; set; }
        /// <summary>
        /// Zrušit příznak IsSelected pro všechny prvky
        /// </summary>
        public bool ClearSelected { get; set; }
        /// <summary>
        /// Nastavit toto časové okno v grafech; null = nenastavovat
        /// </summary>
        public GuiTimeRange TimeAxisValue { get; set; }
        #endregion
    }
    /// <summary>
    /// Třída pro přenesení informace v rámci <see cref="GuiResponse"/> o změně řádku.
    /// Může přinést nový řádek (pokud <see cref="GridRowId"/> obsahuje dosud neexistující klíč <see cref="GuiGridRowId.RowId"/>,
    /// nebo nový obsah do stávajícího řádku, anebo smazat existující řádek, to když <see cref="RowData"/> je null.
    /// </summary>
    public class GuiRefreshRow
    {
        /// <summary>
        /// ID řádku, obsahuje Fullname tabulky a ID řádku
        /// </summary>
        public GuiGridRowId GridRowId { get; set; }
        /// <summary>
        /// Obsah řádku. Pokud je null, pak se má uvedený řádek odebrat z tabulky.
        /// </summary>
        public GuiDataRow RowData { get; set; }
    }
    /// <summary>
    /// Třída pro přenesení informace v rámci <see cref="GuiResponse"/> o změně celého grafu.
    /// Může přinést nový prvek (pokud <see cref="GridRowId"/> obsahuje řádek, kde dosud graf není,
    /// nebo nová data do stávajícího grafu, anebo může smazat existující prvek, to když <see cref="GraphData"/> je null.
    /// </summary>
    public class GuiRefreshGraph
    {
        /// <summary>
        /// ID řádku, obsahuje Fullname tabulky a ID řádku, kde se má graf aktualizovat
        /// </summary>
        public GuiGridRowId GridRowId { get; set; }
        /// <summary>
        /// Data grafu. 
        /// Pokud není null, pak se pro daný řádek má graf vytvořit nebo aktualizovat daty z tohoto grafu.
        /// Pokud je null, pak se má uvedený graf odebrat z řádku.
        /// </summary>
        public GuiGraph GraphData { get; set; }
        /// <summary>
        /// ID starých prvků cílového grafu, které se mají nejprve odstranit
        /// </summary>
        public List<GuiGridItemId> RemoveItems { get; set; }
        /// <summary>
        /// Režim práce s položkami grafů: pokud je zadán řádek, kde již graf existuje, pak tato property řídí chování vzhledem ke stávajícím prvkům.
        /// </summary>
        public GuiMergeMode ItemsMergeMode { get; set; }
    }
    /// <summary>
    /// Třída pro přenesení informace v rámci <see cref="GuiResponse"/> o změně prvku grafu.
    /// Může přinést nový prvek (pokud <see cref="GridItemId"/> obsahuje dosud neexistující klíč <see cref="GuiGridItemId.ItemId"/>,
    /// nebo nová data do stávajícího prvku, anebo může smazat existující prvek, to když <see cref="ItemData"/> je null.
    /// </summary>
    public class GuiRefreshGraphItem
    {
        /// <summary>
        /// ID prvku, obsahuje Fullname tabulky a ID prvku pro jeho vyhledání v tabulce a řádku
        /// </summary>
        public GuiGridItemId GridItemId { get; set; }
        /// <summary>
        /// Data prvku grafu. Pokud je null, pak se má uvedený prvek odebrat z grafu.
        /// </summary>
        public GuiGraphItem ItemData { get; set; }
    }
    /// <summary>
    /// GuiResponseGraphLink : třída sloužící pro přenos vztahů grafu (data z <see cref="GuiGraphLink"/>) z aplikace do GUI v nestrukturovaném seznamu.
    /// To znamená, že v jednom seznamu prvků jsou prvky patřící do různých tabulek.
    /// Používá se po editaci prvků, pro přenos souhrnu změn z aplikace do GUI.
    /// </summary>
    public class GuiResponseGraphLink : GuiGraphLink
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiResponseGraphLink() : base() { }
        /// <summary>
        /// FullName tabulky, do které se má tento prvek vložit.
        /// </summary>
        public string TableName { get; set; }
    }
    /// <summary>
    /// Režim spojení stávajích dat v prvku s daty novými
    /// </summary>
    public enum GuiMergeMode
    {
        /// <summary>
        /// Nezadáno, provede se Update
        /// </summary>
        None = 0,
        /// <summary>
        /// Nejprve odebrat všechny stávající prvky, teprve pak vložit všechny nové prvky
        /// </summary>
        RemoveAllOld,
        /// <summary>
        /// Stávající prvky ponechat, ale pokud v nových datech je údaj pro klíč, který již existuje ve starých datech, pak stará data přepsat novými.
        /// Toto je implicitní chování.
        /// </summary>
        Update,
        /// <summary>
        /// Stávající prvky ponechat, a pokud v nových datech je předán údaj pro klíč, který již existuje ve starých datech, pak nová data ignorovat a ponechat data stará.
        /// </summary>
        InsertOnly
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
    public enum GuiDialogButtons
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
        /// Tlačítko ZRUŠIT VŠE
        /// </summary>
        Abort = 0x0010,
        /// <summary>
        /// Tlačítko ZNOVU
        /// </summary>
        Retry = 0x0020,
        /// <summary>
        /// Tlačítko IGNORUJ
        /// </summary>
        Ignore = 0x0040,
        /// <summary>
        /// Tlačítko ULOŽIT
        /// </summary>
        Save = 0x0100,
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
        YesNo = Yes | No,
        /// <summary>
        /// Tlačítka ANO a NE a STORNO
        /// </summary>
        YesNoCancel = Yes | No | Cancel,
        /// <summary>
        /// Tlačítka ZNOVU a STORNO
        /// </summary>
        RetryCancel = Retry| Cancel,
        /// <summary>
        /// Tlačítka ULOŽIT a STORNO
        /// </summary>
        SaveCancel = Save | Cancel,
        /// <summary>
        /// Všechna tlačítka
        /// </summary>
        All = Ok | Yes | No | Cancel| Abort | Retry | Ignore | Save | Maybe
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
        Image,
        /// <summary>
        /// TrackBar
        /// </summary>
        TrackBar
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
        /// Zobrazit tlačítko Nastavení
        /// </summary>
        ShowSystemConfig = 0x00010000,
        /// <summary>
        /// Suma všech systémových tlačítek, aktuálně jen <see cref="ShowSystemConfig"/>
        /// </summary>
        SystemSettingsAll = ShowSystemConfig,
        /// <summary>
        /// Defaultní využití systémových položek: Zoom (Day + WorkWeek + WholeWeek + Month) + GoAll + ShowSystemConfig
        /// </summary>
        Default = TimeAxisZoomDWWM | TimeAxisGoAll | ShowSystemConfig
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
        /// Prvek lze označit myší (například pro nějakou funkci nebo pro klávesu Delete)
        /// </summary>
        CanSelect = 0x80,
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
        /// Nezobrazovat spojovací linie mezi prvky nikdy.
        /// </summary>
        ShowLinkNone = 0x100000,
        /// <summary>
        /// Zobrazit spojovací linie mezi prvky při stavu MouseOver.
        /// Tyto linky (MouseOver) jsou zobrazovány polovičně průhlednou barvou.
        /// Pokud nebude specifikována hodnota <see cref="ShowLinkInMouseOver"/> ani <see cref="ShowLinkInSelected"/> ani <see cref="ShowLinkAllways"/>, nebudou se zobrazovat spojovací linie mezi prvky nikdy.
        /// </summary>
        ShowLinkInMouseOver = 0x200000,
        /// <summary>
        /// Zobrazit spojovací linie mezi prvky při stavu Selected.
        /// Pokud nebude specifikována hodnota <see cref="ShowLinkInMouseOver"/> ani <see cref="ShowLinkInSelected"/> ani <see cref="ShowLinkAllways"/>, nebudou se zobrazovat spojovací linie mezi prvky nikdy.
        /// </summary>
        ShowLinkInSelected = 0x400000,
        /// <summary>
        /// Zobrazit spojovací linie mezi prvky při stavu vždy.
        /// Pokud nebude specifikována hodnota <see cref="ShowLinkInMouseOver"/> ani <see cref="ShowLinkInSelected"/> ani <see cref="ShowLinkAllways"/>, nebudou se zobrazovat spojovací linie mezi prvky nikdy.
        /// </summary>
        ShowLinkAllways = 0x800000,
        /// <summary>
        /// Default pro pracovní čas = 
        /// <see cref="ResizeTime"/> | <see cref="MoveToAnotherTime"/> | <see cref="MoveToAnotherRow"/> 
        /// | <see cref="ShowLinkInMouseOver"/> | <see cref="ShowLinkInSelected"/>
        /// </summary>
        DefaultWorkTime = ResizeTime | MoveToAnotherTime | MoveToAnotherRow | ShowLinkInMouseOver | ShowLinkInSelected,
        /// <summary>
        /// Default pro text = 
        /// <see cref="ShowCaptionInMouseOver"/> | <see cref="ShowCaptionInSelected"/> | <see cref="ShowToolTipFadeIn"/>
        /// </summary>
        DefaultText = ShowCaptionInMouseOver | ShowCaptionInSelected | ShowToolTipFadeIn,
        /// <summary>
        /// Souhrn příznaků, povolujících Drag and Drop prvku = 
        /// <see cref="MoveToAnotherTime"/> | <see cref="MoveToAnotherRow"/> | <see cref="MoveToAnotherTable"/>
        /// </summary>
        AnyMove = MoveToAnotherTime | MoveToAnotherRow | MoveToAnotherTable,
        /// <summary>
        /// Souhrn příznaků, povolujících Select prvku = 
        /// <see cref="MoveToAnotherTime"/> | <see cref="MoveToAnotherRow"/> | <see cref="MoveToAnotherTable"/> | <see cref="CanSelect"/>
        /// </summary>
        AnySelectable = MoveToAnotherTime | MoveToAnotherRow | MoveToAnotherTable | CanSelect,
        /// <summary>
        /// Souhrn příznaků, které smí mít prvek grafu, který je umístěn v Child řádku v tabulce.
        /// </summary>
        AllEnabledForChildRows = 
            ShowCaptionNone | ShowCaptionInMouseOver | ShowCaptionInSelected | ShowCaptionAllways |
            ShowToolTipNone | ShowToolTipFadeIn | ShowToolTipImmediatelly |
            CanSelect
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
    /// Režim odesílání commandu o změně času na časové ose z pluginu do servisní funkce.
    /// Pokud uživatel změní viditelný rozsah, pak podle této hodnoty odešle plugin command 
    /// </summary>
    public enum TimeChangeSendMode
    {
        /// <summary>
        /// Při pohybu na časové ose se neodesílá žádný požadavek. Toto je výchozí nastavení.
        /// </summary>
        None = 0,
        /// <summary>
        /// Při pohybu na časové ose se detekuje, zda je zobrazen úsek, který už byl zobrazen a jsou pro něj načtena data 
        /// (předchozím vyvoláním requestu <see cref="GuiRequest.COMMAND_TimeChange"/>), pak se tento request znovu neposílá.
        /// Pošle se jen tehdy, když uživatel najede na časový úsek, pro který jsme ještě request neposílali.
        /// </summary>
        OnNewTime,
        /// <summary>
        /// Při jakémkoli pohybu na časové ose se odešle request <see cref="GuiRequest.COMMAND_TimeChange"/>.
        /// </summary>
        Allways
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
        /// Identifikátor záznamu komplexní <see cref="GuiId"/>
        /// </summary>
        RecordId,
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
        /// Data pomocná pro řešení statického vztahu = číslo záznamu ve vztahu.
        /// Číslo třídy je v záhlaví tohoto sloupce.
        /// Jiné (datové) sloupce, které zobrazují texty ze vztaženého záznamu, se odkazují na tento sloupec, aby bylo možno otevřít záznam.
        /// </summary>
        RelationHelpfulColumn,
        /// <summary>
        /// Informace o počtu záznamů
        /// </summary>
        TotalCountHelpfulColumn
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
        /// Command pro refresh pluginu
        /// </summary>
        public const string CMD_REFRESH_PLUGIN = "RefreshPlugin";

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
                    case "RecordId": return BrowseColumnType.RecordId;
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
                case BrowseColumnType.RecordId: return "RecordId";
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
        /// Aktuálně hodnoty: RecordId, SubjectNumber, ObjectNumber, DataColumn, RelationHelpfulColumn, TotalCountHelpfulColumn
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
    #endregion
}
