// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using DevExpress.Data.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WinDraw = System.Drawing;
using WinForm = System.Windows.Forms;

namespace Noris.WS.DataContracts.DxForm
{
    /*     INFORMACE :  Datové třídy  +  XSD Schema  +  Načítání dat;      POSTUP pro přidání nové třídy anebo nové vlastnosti.
          Deklarace dat  +  deklarace XSD  +  Načítací algoritmus
     1. Struktury dat jsou deklarovány zde, nemají žádnou logiku, jde jen  o obálku na data
     2. V podstatě identické struktury jsou deklarovány v XSD suoboru - stejná hierarchie tříd, stejnojmenné property ve stejných třídách, stejné enumy a jejich hodnoty
     3. Existuje statická třída DxDataFormatLoader, která z XML dokumentu (zadaného podle XSD schematu) vytvoří a vrátí odpovídající struktury zdejších tříd (C#)

          Hierarchie tříd
     - Třídy pro data jsou uspořádány hierarchicky
     - Bázová třída je DfBase, nese Name, State, ToolTip, Invisible
     - Z ní jsou postupně děděny třídy pro Controly i pro Containery
     - Obdobná hierarchie je i ve schematu XSD (i tam je použita dědičnost typů!)

      TŘÍDA                                                            ÚČEL                                   PROPERTIES
    DfBase                                                 Bázová pro SubControly, Controly i Containery     Name, State, ToolTip, Invisible
      +  DfSubTextItem                                     Pomocný SubTextItem                               Text, IconName
      |    +  DfSubButton                                  Pomocný SubButton                                 ActionName, ActionData
      +  DfBaseControl                                     Bázová pro všechny Controly                       ControlType, Bounds
      |    +  DfLabel                                      Control Label                                     Text, Alignment
      |    +  DfTitle                                      Control Title                                     IconName, Title, Style, Alignment
      |    +  DfBaseInputControl                           Bázová pro všechny vstupní prvky                  Required
      |         +  DfBaseLabeledInputControl               Bázová pro vstupní prvky s Labelem                Label, LabelPosition, LabelWidth
      |         |    +  DfTextBox                          Control TextBox                                   EditMask, Alignment
      |         |    |    + DfTextBoxButton                Control TextBox s Buttony                         LeftButtons, RightButtons, ButtonsVisibility
      |         |    +  DfComboBox                         Control ComboBox                                  ComboItems, EditStyleName, Style
      |         +  DfBaseInputTextControl                  Bázová pro prvky obsahující Text                  Text, IconName, Alignment
      |              +  DfCheckBox                         Control CheckBox (a podobné)                      Style
      |              +  DfButton                           Control Button                                    ActionType, ActionData, HotKey
      |                   + DfDropDownButton               Control DropDown Button                           DropDownButtons
      +  DfBaseContainer                                   Bázová pro containery                             BackColorName, BackColorLight, BackColorDark, Margins, Childs
           +  DfPanel                                      Panel (s možností Collapse)                       Bounds, IconName, Title, TitleStyle, CollapseState
           +  DfPage                                       Jedna stránka                                     Title, IconName
           +  DfPanelSet                                   Sada stránek                                      Bounds
           +  DfForm                                       Celý formulář, sada vlastností Template           ... kupa ...


          Postup, jak přidat novou třídu  { třeba pro zadání controlu Piškvorky }:
          ------------------------------------------------------------------------
      - C#: deklarace - Najdi vhodného předka = jaké properties využijeme, abychom přidali jen nové { Je to Input, a chci Label, takže předkem bude DfBaseLabeledInputControl }
        - Přidej nový typ controlu do enumu   public enum ControlType   dole v této třídě  { asi  Piskvorky }
        - Napiš zdejší třídu, nejspíš DfPiskvorky : DfBaseLabeledInputControl
        - Přepiš její   public override ControlType ControlType { get { a vracej  svůj typ  ControlType.Piskvorky;  } }
        - Přidej do své třídy její speciální vlastnosti, třeba definici vzhledu Piškvorek, jako nějaký nový enum, třeba public PiskvorkyStyleType Style { get; set; }
        - Nadefinuj ten enum PiskvorkyStyleType { Default, Konkrétní typy, ... }
      - XSD - Přejdi do XSD schematu
        - Přidej odpovídající typ { asi type_piskvorky }
        - Doplň jeho base typ { asi <xs:extension base="type_base_labeled_input_control"> }
        - Přidej jeho atribut Style, typu odpovídajícího enumu  { piskvorky_style_enum }
            - Dodržuj shodu jména property a atributu = je to vhodné pro čitelnost
        - Nadeklaruj ten enum, podobně jako je deklarovaný každý jiný, například "checkbox_style_enum"
            - Dodržuj shodu jména hodnoty enumu a jméno enumerace = nutné, na tom je založena načítací metoda _ReadAttributeEnum()
        - Přidávej popisky  { <xs:annotation><xs:documentation xml:lang="cs-cz">Styl zobrazení Piškvorek</xs:documentation></xs:annotation>  }
        - Zajisti, aby tento typ bylo možno přidat do panelu tak jako ostatní controly: 
           - Najdi bázový container:  <xs:complexType name="type_base_container">
           - Přidej do něj další možnost elementu  { asi: <xs:element name="piskvorky" type="type_piskvorky" minOccurs="0" maxOccurs="unbounded">  }
      - Načítání - Přejdi do loaderu = třída DfTemplateLoader, a zajisti, že nový element bude možno použít:
        - V metodě  private static DfBase _CreateItem(..)  přidej novou větev pro nový typ elementu
            case "piskvorky": return _FillControlPiskvorky(xElement, new DfPiskvorky(), loaderContext);
        - Vytvoř navrženou metodu  private static DfBaseControl _FillControlPiskvorky(..) obdobně jako jsou ostatní
           - Nejdřív zavolej :       _FillBaseAttributes(xElement, control);             // Načte atributy bázových tříd (sám rozpozná, které to jsou)
           - A potom načti svoje vlastní atributy:
               control.Style = _ReadAttributeEnum(xElement, "Style", PiskvorkyStyleType.Default);
      - A pak kompletně naprogramuj její zobrazování a interaktivitu (to je nad rámec tohoto úkolu)


          Postup, jak přidat novou hodnotu (property) do existujícího controlu:
          ---------------------------------------------------------------------
      - Z předešlého návodu si vyber jen potřebné kroky:
      - Najdi dotyčnou třídu v tomto souboru, například DfTextBox
        - Přidej do ní její novou hodnotu (property), třeba definici stylu rámečku, plus přidej odpovídající enum
      - Přejdi do XSD schematu
        - A do odpovídajícího XSD typu controlu přidej nový atribut plus jeho typ
        - Případně nadeklaruj typ (enumeration)
      - Načítání - Přejdi do loaderu = třída DfTemplateLoader
        - Najdi načítání dané třídy, například   private static DfBaseControl _FillControlTextBox(..)
        - Přidej řádek pro načtené nové property vhodnou metodou
     

    */

    // Shared : třídy pouze nesou data, nemají funkcionalitu. Poměrně dobře korespondují s XML schematem DxDataFormat.Frm

    #region Konkrétní třídy Containerů
    /// <summary>
    /// Hlavičková informace o dokumentu: obsahuje pouze <see cref="XmlNamespace"/> a <see cref="FormatVersion"/>.
    /// </summary>
    internal class DfInfoForm
    {
        /// <summary>
        /// Namespace XML dokumentu
        /// </summary>
        public string XmlNamespace { get; set; }
        /// <summary>
        /// Formát tohoto souboru. Defaultní = 4
        /// </summary>
        public FormatVersionType FormatVersion { get; set; }
    }
    /// <summary>
    /// Celý DataForm. Obsahuje stránky <see cref="Pages"/> v počtu 0 až +Nnn.
    /// <para/>
    /// Odpovídá XSD typu: <c>template</c>
    /// </summary>
    internal class DfForm : DfBaseArea
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DfForm()
        {
        }
        /// <summary>
        /// Styl bloku
        /// </summary>
        public override ContainerStyleType Style { get { return ContainerStyleType.Form; } }
        /// <summary>
        /// Název souboru, který je načten
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Namespace XML dokumentu
        /// </summary>
        public string XmlNamespace { get; set; }
        /// <summary>
        /// Formát tohoto souboru. Defaultní = 4
        /// </summary>
        public FormatVersionType FormatVersion { get; set; }
        /// <summary>
        /// Šířka oblasti DataFormu (Master) (tedy bez dynamických vztahů a dolních záložek)
        /// </summary>
        public int? MasterWidth { get; set; }
        /// <summary>
        /// Výška oblasti DataFormu (Master) (tedy bez dynamických vztahů a dolních záložek)
        /// </summary>
        public int? MasterHeight { get; set; }
        /// <summary>
        /// Celková šířka (tedy včetně dynamických vztahů a dolních záložek)
        /// </summary>
        public int? TotalWidth { get; set; }
        /// <summary>
        /// Celková výška (tedy včetně dynamických vztahů a dolních záložek)
        /// </summary>
        public int? TotalHeight { get; set; }
        /// <summary>
        /// Jméno souboru, z něhož bude načtena definice dat
        /// </summary>
        public string DataSource { get; set; }
        /// <summary>
        /// Jméno souboru, z něhož budou čteny překlady popisků a tooltipů
        /// </summary>
        public string Messages { get; set; }
        /// <summary>
        /// Číslo třídy, pro kterou budou načteny popisky atributů a vztahů
        /// </summary>
        public int? UseNorisClass { get; set; }
        /// <summary>
        /// Automaticky přidat záložku pro UDA atributy a vztahy. Nezadáno = True
        /// </summary>
        public bool? AddUda { get; set; }
        /// <summary>
        /// Umístění labelů pro automaticky generované UDA atributy a vztahy. Defaultní = <see cref="LabelPositionType.Top"/>
        /// </summary>
        public LabelPositionType? UdaLabelPosition { get; set; }
        /// <summary>
        /// Povolit kontextové menu.
        /// </summary>
        public bool? ContextMenu { get; set; }
        /// <summary>
        /// Jednotlivé prvky - stránky.
        /// Default = null.
        /// Pokud <see cref="DfForm"/> obsahuje pouze jednu stránku, pak se nezobrazuje horní záložkovník (TabHeaders).
        /// </summary>
        public List<DfPage> Pages { get; set; }
        /// <summary>
        /// Debug text
        /// </summary>
        protected override string DebugText { get { return $"{Style} '{Name}'; Pages: {(Pages is null ? "NULL" : Pages.Count.ToString())}"; } }
    }
    /// <summary>
    /// Stránka = jedna záložka. Obsahuje vnořené panely <see cref="DfPanel"/>.
    /// Pokud <see cref="DfForm"/> obsahuje pouze jednu stránku <see cref="DfPage"/>, pak není zobrazen horní záložkovník (TabHeaders), ale obsah této stránky obsazuje celou plochu dataformu.
    /// <para/>
    /// Odpovídá XSD typu: <c>type_page</c>
    /// </summary>
    internal class DfPage : DfBaseArea
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DfPage() : base()
        {
        }
        /// <summary>
        /// Styl bloku
        /// </summary>
        public override ContainerStyleType Style { get { return ContainerStyleType.Page; } }
        /// <summary>
        /// Jméno ikony panelu, zobrazuje se vlevo v titulku.
        /// </summary>
        public string IconName { get; set; }
        /// <summary>
        /// Titulek panelu.
        /// Zobrazuje se pouze u 
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Panely na této stránce.
        /// Default = null.
        /// </summary>
        public List<DfPanel> Panels { get; set; }
        /// <summary>
        /// Debug text
        /// </summary>
        protected override string DebugText { get { return $"{Style} '{Name}'; Title: '{Title}'; Panels: {(Panels is null ? "NULL" : Panels.Count.ToString())}"; } }
    }
    /// <summary>
    /// Panel, může obsahovat jednotlivé controly i vnořené containery.
    /// <para/>
    /// Odpovídá XSD typu: <c>type_panel</c>
    /// </summary>
    internal class DfPanel : DfBaseContainer
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DfPanel() : base()
        {
        }
        /// <summary>
        /// Styl bloku
        /// </summary>
        public override ContainerStyleType Style { get { return ContainerStyleType.Panel; } }
        /// <summary>
        /// Obsahuje true, pokud tento panel reprezentuje záhlaví. To může být vykresleno na každé záložce, vždy jako první horní panel, v šířce přes všechny sloupce.
        /// </summary>
        public bool? IsHeader { get; set; }
        /// <summary>
        /// Název stránek (page), na kterých je záhlaví zobrazeno. Pouze pokud IsHeader je true. Pokud nebude HeaderOnPages zadáno, bude vykresleno pro všechny stránky.
        /// </summary>
        public string HeaderOnPages { get; set; }
        /// <summary>
        /// Jméno ikony panelu, zobrazuje se vlevo v titulku.
        /// </summary>
        public string IconName { get; set; }
        /// <summary>
        /// Titulek panelu.
        /// Zobrazuje se pouze u 
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Styl titulku.
        /// </summary>
        public TitleStyleType? TitleStyle { get; set; }
        /// <summary>
        /// Barva textu titulku, zadaná jako název kalíšku.
        /// </summary>
        public string TitleColorName { get; set; }
        /// <summary>
        /// Barva textu titulku, zadaná explicitně pro světlé skiny: buď jménem např. Red, LightGray, anebo jako RGB: 0xDDFFDD, atd.
        /// </summary>
        public System.Drawing.Color? TitleColorLight { get; set; }
        /// <summary>
        /// Barva textu titulku, zadaná explicitně pro tmavé skiny: buď jménem např. Red, LightGray, anebo jako RGB: 0xDDFFDD, atd.
        /// </summary>
        public System.Drawing.Color? TitleColorDark { get; set; }
        /// <summary>
        /// Možnosti přeskupování panelu (Collapse / Expand)
        /// </summary>
        public PanelCollapseState? CollapseState { get; set; }
        /// <summary>
        /// Debug text
        /// </summary>
        protected override string DebugText { get { return $"{Style} '{Name}'; Title: '{Title}'; Childs: {(Childs is null ? "NULL" : Childs.Count.ToString())}"; } }
    }
    /// <summary>
    /// Grupa uvnitř Panelu nebo uvnitř grupy, ale bez chování Panelu = nemá Collapsed a Title
    /// <para/>
    /// Odpovídá XSD typu: <c>type_group</c>
    /// </summary>
    internal class DfGroup : DfBaseContainer
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DfGroup() : base()
        {
        }
        /// <summary>
        /// Styl bloku
        /// </summary>
        public override ContainerStyleType Style { get { return ContainerStyleType.Group; } }
        /// <summary>
        /// Index sloupce, na kterém je prvek umístěn v režimu FlowLayout. Ten se použije, pokud prvky nemají exaktně dané souřadnice, spolu s atributem 'ColumnWidths'.
        /// </summary>
        public int? ColIndex { get; set; }
        /// <summary>
        /// Počet sloupců, které prvek obsazuje v FlowLayoutu. Ten se použije, pokud prvky nemají exaktně dané souřadnice, spolu s atributem 'ColumnWidths'.
        /// </summary>
        public int? ColSpan { get; set; }
        /// <summary>
        /// Počet řádků, které prvek obsazuje v FlowLayoutu. Ten se použije, pokud prvky nemají exaktně dané souřadnice.
        /// </summary>
        public int? RowSpan { get; set; }
        /// <summary>
        /// Umístění prvku vodorovně v rámci sloupce v případě, kdy šířka prvku je menší než šířka sloupce. Řeší tedy zarovnání controlu ve sloupci, nikoli zarovnání obsahu (textu) v rámci controlu.
        /// </summary>
        public HPositionType? HPosition { get; set; }
    }
    /// <summary>
    /// Vnořený panel, vložený do Page.
    /// Z hlediska deklarace DataFormu jde o prvek, který nemá mnoho svých vlastností, a nemá ani Child prvky.
    /// Pouze definuje svoji zdrojovou šablonu.
    /// V procesu načítání deklarace je vyhledána a načtena odpovídající <see cref="NestedTemplate"/>, a v ní je nalezen první panel a ten je použit namísto tohoto Nested panelu.
    /// Tedy panel z vnořené šablony se stane panelem v aktuální šabloně.
    /// <para/>
    /// Odpovídá XSD typu: <c>type_nested_panel</c>
    /// </summary>
    internal class DfNestedPanel : DfBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DfNestedPanel() : base()
        {
        }
        /// <summary>
        /// Nested template = název šablony, ze které se načítá definice vnořeného panelu.
        /// </summary>
        public string NestedTemplate { get; set; }
        /// <summary>
        /// Jméno hledaného panelu v nested šabloně. Pokud není zadáno, vezme se první nalezený panel.
        /// </summary>
        public string NestedPanelName { get; set; }
        /// <summary>
        /// Obsahuje true, pokud tento panel reprezentuje záhlaví. To může být vykresleno na každé záložce, vždy jako první horní panel, v šířce přes všechny sloupce.
        /// </summary>
        public bool? IsHeader { get; set; }
        /// <summary>
        /// Název stránek (page), na kterých je záhlaví zobrazeno. Pouze pokud IsHeader je true. Pokud nebude HeaderOnPages zadáno, bude vykresleno pro všechny stránky.
        /// </summary>
        public string HeaderOnPages { get; set; }
    }
    /// <summary>
    /// Vnořená grupa, vložená do Panelu nebo do Grupy.
    /// Z hlediska deklarace DataFormu jde o prvek, který nemá mnoho svých vlastností, a nemá ani Child prvky.
    /// Pouze definuje svoji zdrojovou šablonu.
    /// V procesu načítání deklarace je vyhledána a načtena odpovídající <see cref="NestedTemplate"/>, a v ní je nalezena první grupa a ta je použita namísto této Nested grupy.
    /// Tedy grupa z vnořené šablony se stane grupou v aktuální šabloně.
    /// <para/>
    /// Odpovídá XSD typu: <c>type_nested_group</c>
    /// </summary>
    internal class DfNestedGroup : DfBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DfNestedGroup() : base()
        {
        }
        /// <summary>
        /// Nested template = název šablony, ze které se načítá definice vnořené grupy.
        /// </summary>
        public string NestedTemplate { get; set; }
        /// <summary>
        /// Jméno hledané grupy v nested šabloně. Pokud není zadáno, vezme se první nalezená grupa.
        /// </summary>
        public string NestedGroupName { get; set; }
        /// <summary>
        /// Umístění prvku. Výchozí je null.
        /// Pokud je panel umístěn přímo jako Child v nadřazené <see cref="DfPage"/>, pak se ignoruje umístění (Left a Top) dané v <see cref="Bounds"/>, akceptuje se jen Width a Height.
        /// Pak jde o primární panely, a ty jsou typicky uživatelsky ovladatelné.
        /// </summary>
        public Bounds Bounds { get; set; }
        /// <summary>
        /// Jméno prvku 'Name', který určuje souřadnice pro zdejší prvek, pokud ten je umístěn fixně = má definované souřadnice 'X' a 'Y'.
        /// </summary>
        public string ParentBounds { get; set; }
        /// <summary>
        /// Index sloupce, na kterém je prvek umístěn v režimu FlowLayout. Ten se použije, pokud prvky nemají exaktně dané souřadnice, spolu s atributem 'ColumnWidths'.
        /// </summary>
        public int? ColIndex { get; set; }
        /// <summary>
        /// Počet sloupců, které prvek obsazuje v FlowLayoutu. Ten se použije, pokud prvky nemají exaktně dané souřadnice, spolu s atributem 'ColumnWidths'.
        /// </summary>
        public int? ColSpan { get; set; }
        /// <summary>
        /// Počet řádků, které prvek obsazuje v FlowLayoutu. Ten se použije, pokud prvky nemají exaktně dané souřadnice.
        /// </summary>
        public int? RowSpan { get; set; }
        /// <summary>
        /// Umístění prvku vodorovně v rámci sloupce v případě, kdy šířka prvku je menší než šířka sloupce. Řeší tedy zarovnání controlu ve sloupci, nikoli zarovnání obsahu (textu) v rámci controlu.
        /// </summary>
        public HPositionType? HPosition { get; set; }
    }
    /// <summary>
    /// Prostor obsahující controly = základ pro <see cref="DfPanel"/> a pro <see cref="DfGroup"/>.<br/>
    /// Jejím úkolem je deklarovat vzhled pozadí containeru a okraje.
    /// <para/>
    /// Odpovídá XSD typu <c>type_base_container</c>
    /// </summary>
    internal class DfBaseContainer : DfBaseArea
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DfBaseContainer() : base()
        {
        }
        /// <summary>
        /// Controly v rámci tohoto Containeru.
        /// Mohou zde být i další Containery.
        /// Default = null.
        /// </summary>
        public List<DfBase> Childs { get; set; }
        /// <summary>
        /// Umístění prvku. Výchozí je null.
        /// Pokud je panel umístěn přímo jako Child v nadřazené <see cref="DfPage"/>, pak se ignoruje umístění (Left a Top) dané v <see cref="Bounds"/>, akceptuje se jen Width a Height.
        /// Pak jde o primární panely, a ty jsou typicky uživatelsky ovladatelné.
        /// </summary>
        public Bounds Bounds { get; set; }
        /// <summary>
        /// Jméno prvku 'Name', který určuje souřadnice pro zdejší prvek, pokud ten je umístěn fixně = má definované souřadnice 'X' a 'Y'.
        /// </summary>
        public string ParentBounds { get; set; }
        /// <summary>
        /// Souřadnice bodu, kde začíná rozmísťování prvků, které nemají zadanou exaktní souřadnici X, Y. Výchozí je "0,0". Zadává se ve formě: "X,Y".
        /// </summary>
        public Location FlowLayoutOrigin { get; set; }
        /// <summary>
        /// Velikost tohoto conatineru, určená výpočtem.
        /// </summary>
        public ContainerSize DesignSize { get; set; }
        /// <summary>
        /// Debug text
        /// </summary>
        protected override string DebugText { get { return $"{Style}; Name: {Name}"; } }
    }
    /// <summary>
    /// Prostor s něčím uvnitř = základ pro <see cref="DfPage"/>, pro <see cref="DfPanel"/> a pro <see cref="DfGroup"/>.
    /// </summary>
    internal class DfBaseArea : DfBase
    {
        /// <summary>
        /// Styl bloku
        /// </summary>
        public virtual ContainerStyleType Style { get { return ContainerStyleType.None; } }
        /// <summary>
        /// Název barevného kalíšku barvy pozadí
        /// </summary>
        public string BackColorName { get; set; }
        /// <summary>
        /// Barva pozadí, zadaná explicitně pro světlé skiny: buď jménem např. Red, LightGray, anebo jako RGB: 0xDDFFDD, atd.
        /// </summary>
        public System.Drawing.Color? BackColorLight { get; set; }
        /// <summary>
        /// Barva pozadí, zadaná explicitně pro tmavé skiny: buď jménem např. Red, LightGray, anebo jako RGB: 0xDDFFDD, atd.
        /// </summary>
        public System.Drawing.Color? BackColorDark { get; set; }
        /// <summary>
        /// Obrázek vykreslený na pozadí prostoru. Zadán má být jako jméno souboru.
        /// </summary>
        public string BackImageName { get; set; }
        /// <summary>
        /// Pozice obrázku <see cref="BackImageName"/> vzhledem k prostoru.
        /// </summary>
        public BackImagePositionType? BackImagePosition { get; set; }
        /// <summary>
        /// Souřadnice počátku Flow prostoru
        /// </summary>
        public Location FlowAreaBegin { get; set; }
        /// <summary>
        /// Okraje = mezi krajem formuláře / Page / Panel a souřadnicí 0/0
        /// </summary>
        public Margins Margins { get; set; }
        /// <summary>
        /// Odstupy mezi MainLabelem a Controlem, nebo SuffixLabelem a Controlem, v rámci jedné buňky (jeden prvek). Uplatní se, pouze pokud daný label existuje.
        /// </summary>
        public Margins ControlMargins { get; set; }
        /// <summary>
        /// Počet sloupců layoutu. Šířka sloupců se určí podle reálného obsahu (maximum šířky prvků).
        /// Při zadání <see cref="ColumnsCount"/> se již nezadává <see cref="ColumnWidths"/>.
        /// </summary>
        public int? ColumnsCount { get; set; }
        /// <summary>
        /// Šířky jednotlivých sloupců layoutu, oddělené čárkou; např. 150;350;100 (deklaruje tři sloupce dané šířky).
        /// Sloupce mohou deklarovat i šířku pro label umístěný před controlem, pak se použije čárka: "100,150; 120,300; 80,100" (deklaruje tři sloupce s danou šířkou labelu a šířkou controlu).
        /// Mezery jsou ignorovány.
        /// Při zadání <see cref="ColumnWidths"/> se již nezadává <see cref="ColumnsCount"/>.
        /// </summary>
        public string ColumnWidths { get; set; }
        /// <summary>
        /// Volný prostor v pixelech mezi dvěma sousedními sloupci (vnitřní Margin). Nezadáno = sloupce navazují těsně vedle sebe.
        /// </summary>
        public int? ColumnsDistance { get; set; }
        /// <summary>
        /// Volný prostor v pixelech mezi dvěma sousedními řádky (vnitřní Margin). Nezadáno = řádky navazují těsně vedle sebe.
        /// </summary>
        public int? RowsDistance { get; set; }
        /// <summary>
        /// Automaticky generovat labely atributů a vztahů, jejich umístění. Defaultní = <c>NULL</c>
        /// </summary>
        public LabelPositionType? AutoLabelPosition { get; set; }
    }
    #endregion
    #region Konkrétní třídy Controlů
    /// <summary>
    /// DxDataForm : PlaceHolder.<br/>
    /// Odpovídá XSD typu <c>type_base_control</c>, ale použitý v elementu <c>placeholder</c>
    /// </summary>
    internal class DfPlaceHolder : DfBaseControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DfPlaceHolder() : base()
        {
        }
        /// <summary>
        /// Druh vstupního prvku (Control).
        /// </summary>
        public override ControlType ControlType { get { return ControlType.PlaceHolder; } }
    }
    /// <summary>
    /// DxDataForm : Label.<br/>
    /// Odpovídá XSD typu <c>type_label</c>
    /// </summary>
    internal class DfLabel : DfBaseControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DfLabel() : base()
        {
        }
        /// <summary>
        /// Druh vstupního prvku (Control).
        /// </summary>
        public override ControlType ControlType { get { return ControlType.Label; } }
        /// <summary>
        /// Pevný text prvku (text Labelu). Může obsahovat formátovací funkce fm(), fmr(), fmdr().
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Zarovnání textu v rámci prostoru
        /// </summary>
        public ContentAlignmentType? Alignment { get; set; }
        /// <summary>
        /// Debug text
        /// </summary>
        protected override string DebugText { get { return $"{ControlType}; Name: '{Name}'; Text: '{Text}'"; } }
    }
    /// <summary>
    /// DxDataForm : Title.<br/>
    /// Odpovídá XSD typu <c>type_title</c>
    /// </summary>
    internal class DfTitle : DfBaseControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DfTitle() : base()
        {
        }
        /// <summary>
        /// Druh vstupního prvku (Control).
        /// </summary>
        public override ControlType ControlType { get { return ControlType.Title; } }
        /// <summary>
        /// Jméno ikony odstavce nebo prvku (v titulku stránky, v titulku odstavce, ikona Buttonu, atd).
        /// Použití se liší podle typu prvku.
        /// </summary>
        public string IconName { get; set; }
        /// <summary>
        /// Pevný text prvku = text titulku. Může obsahovat formátovací funkce fm(), fmr(), fmdr().
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Styl titulku.
        /// </summary>
        public TitleStyleType? Style { get; set; }
        /// <summary>
        /// Zarovnání obsahu (titulku) v definovaném prostoru prvku.
        /// </summary>
        public ContentAlignmentType? Alignment { get; set; }
        /// <summary>
        /// Debug text
        /// </summary>
        protected override string DebugText { get { return $"{ControlType}; Name: '{Name}'; Style: '{Style}'; Title: '{Title}'"; } }
    }
    /// <summary>
    /// DxDataForm : CheckBox.<br/>
    /// Odpovídá XSD typu <c>type_checkbox</c>
    /// </summary>
    internal class DfCheckBox : DfBaseInputTextControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DfCheckBox() : base()
        {
        }
        /// <summary>
        /// Druh vstupního prvku (Control).
        /// </summary>
        public override ControlType ControlType { get { return ControlType.CheckBox; } }
        /// <summary>
        /// Styl vizualizace CheckBoxu
        /// </summary>
        public CheckBoxStyleType? Style { get; set; }
        /// <summary>
        /// Debug text
        /// </summary>
        protected override string DebugText { get { return $"{ControlType}; Name: '{Name}'; Style: '{Style}'; Text: '{Text}'"; } }
    }
    /// <summary>
    /// DxDataForm : Button.<br/>
    /// Odpovídá XSD typu <c>type_button</c>
    /// </summary>
    internal class DfButton : DfBaseInputTextControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DfButton() : base()
        {
        }
        /// <summary>
        /// Druh vstupního prvku (Control).
        /// </summary>
        public override ControlType ControlType { get { return ControlType.Button; } }
        /// <summary>
        /// Akce na tomto buttonu
        /// </summary>
        public ButtonActionType? ActionType { get; set; }
        /// <summary>
        /// Data pro akci na tomto buttonu
        /// </summary>
        public string ActionData { get; set; }
        /// <summary>
        /// Klávesová zkratka
        /// </summary>
        public string HotKey { get; set; }
    }
    /// <summary>
    /// DxDataForm : DropDownButton.<br/>
    /// Odpovídá XSD typu <c>type_drop_down_button</c>
    /// </summary>
    internal class DfDropDownButton : DfButton
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DfDropDownButton() : base()
        {
            this.DropDownButtons = null;
        }
        /// <summary>
        /// Druh vstupního prvku (Control).
        /// </summary>
        public override ControlType ControlType { get { return ControlType.DropDownButton; } }
        /// <summary>
        /// DropDown prvky na buttonu.
        /// Výchozí hodnota je NULL.
        /// </summary>
        public List<DfSubButton> DropDownButtons { get; set; }
    }
    /// <summary>
    /// DxDataForm : EditBox.<br/>
    /// Odpovídá XSD typu <c>type_editbox</c>
    /// <para/>
    /// </summary>
    internal class DfEditBox : DfBaseLabeledInputControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DfEditBox() : base()
        {
        }
        /// <summary>
        /// Druh vstupního prvku (Control).
        /// </summary>
        public override ControlType ControlType { get { return ControlType.EditBox; } }
    }
    /// <summary>
    /// DxDataForm : TextBox.<br/>
    /// Odpovídá XSD typu <c>type_textbox</c>
    /// <para/>
    /// Tato třída přináší property <see cref="Alignment"/>, <see cref="EditMask"/>.
    /// </summary>
    internal class DfTextBox : DfBaseLabeledInputControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DfTextBox() : base()
        {
            this.Alignment = null;
        }
        /// <summary>
        /// Druh vstupního prvku (Control).
        /// </summary>
        public override ControlType ControlType { get { return ControlType.TextBox; } }
        /// <summary>
        /// Editační maska
        /// </summary>
        public string EditMask { get; set; }
        /// <summary>
        /// Zarovnání textu v rámci prostoru
        /// </summary>
        public ContentAlignmentType? Alignment { get; set; }
    }
    /// <summary>
    /// DxDataForm : TextBoxButton.<br/>
    /// Odpovídá XSD typu <c>type_textboxbutton</c>
    /// </summary>
    internal class DfTextBoxButton : DfTextBox
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DfTextBoxButton() : base()
        {
            this.LeftButtons = null;
            this.RightButtons = null;
        }
        /// <summary>
        /// Druh vstupního prvku (Control).
        /// </summary>
        public override ControlType ControlType { get { return ControlType.TextBoxButton; } }
        /// <summary>
        /// Buttony na levé straně TextBoxu.
        /// Výchozí hodnota je NULL.
        /// </summary>
        public List<DfSubButton> LeftButtons { get; set; }
        /// <summary>
        /// Buttony na levé straně TextBoxu.
        /// Výchozí hodnota je NULL.
        /// </summary>
        public List<DfSubButton> RightButtons { get; set; }
        /// <summary>
        /// Viditelnost buttonů v textboxu v závislosti na aktivitě TextBoxu
        /// </summary>
        public ButtonsVisibilityType? ButtonsVisibility { get; set; }
    }
    /// <summary>
    /// DxDataForm : ComboBox.<br/>
    /// Odpovídá XSD typu <c>type_combobox</c>
    /// <para/>
    /// Tato třída přináší property <see cref="ComboItems"/>, <see cref="EditStyleName"/>, <see cref="Style"/>.
    /// </summary>
    internal class DfComboBox : DfBaseLabeledInputControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DfComboBox() : base()
        {
            this.Style = null;
        }
        /// <summary>
        /// Druh vstupního prvku (Control).
        /// </summary>
        public override ControlType ControlType { get { return ControlType.ComboBox; } }
        /// <summary>
        /// Položky v nabídce
        /// </summary>
        public List<DfSubTextItem> ComboItems { get; set; }
        /// <summary>
        /// Název editačního stylu. Může být prázdné, pokud budou zadány prvky comboItem.
        /// </summary>
        public string EditStyleName { get; set; }
        /// <summary>
        /// Styl zobrazení ComboBoxu: S možností psaní, Pouze výběr hodnot, Výběr včetně zobrazení ikony
        /// </summary>
        public ComboBoxStyleType? Style { get; set; }
    }
    /// <summary>
    /// DxDataForm : DfImage.<br/>
    /// Odpovídá XSD typu <c>type_image</c>
    /// <para/>
    /// Tato třída přináší property <see cref="ImageName"/>, <see cref="ImageData"/>.
    /// </summary>
    internal class DfImage : DfBaseLabeledInputControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DfImage() : base()
        {
        }
        /// <summary>
        /// Druh vstupního prvku (Control).
        /// </summary>
        public override ControlType ControlType { get { return ControlType.Image; } }
        /// <summary>
        /// Jméno obrázku. Obrázek bude vyhledán mezi ikonami (tedy 'pic/images/obrazek').
        /// </summary>
        public string ImageName { get; set; }
        /// <summary>
        /// Konstantní obsah obrázku dodaný ze serveru ve formě Base64.
        /// </summary>
        public byte[] ImageData { get; set; }
    }
    /// <summary>
    /// DxDataForm : DfStepProgress.<br/>
    /// Odpovídá XSD typu <c>type_stepprogress</c>
    /// <para/>
    /// Tato třída přináší property <see cref="Steps"/>, <see cref="EditStyleName"/>.
    /// </summary>
    internal class DfStepProgress : DfBaseLabeledInputControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DfStepProgress() : base()
        {
        }
        /// <summary>
        /// Druh vstupního prvku (Control).
        /// </summary>
        public override ControlType ControlType { get { return ControlType.StepProgressBar; } }
        /// <summary>
        /// Jednotlivý krok v progresu, jeho statická vizualizace.
        /// </summary>
        public List<DfSubTextItem> Steps { get; set; }
        /// <summary>
        /// Název editačního stylu. Může být prázdné, pokud budou zadány prvky 'step'
        /// </summary>
        public string EditStyleName { get; set; }
    }
    /// <summary>
    /// DxDataForm : SubButton = součást <see cref="ControlType.DropDownButton"/> i <see cref="ControlType.TextBoxButton"/>.<br/>
    /// Odpovídá XSD typu <c>type_subbutton</c>
    /// </summary>
    internal class DfSubButton : DfSubTextItem
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DfSubButton() : base()
        {
        }
        /// <summary>
        /// Akce, kterou tento sub-button provede
        /// </summary>
        public SubButtonActionType? ActionType { get; set; }
        /// <summary>
        /// Data pro akci (název akce pro Clipboard, název editoru, atd)
        /// </summary>
        public string ActionData { get; set; }
    }
    /// <summary>
    /// DxDataForm : DfSubTextItem = součást <see cref="ControlType.ComboBox"/>.<br/>
    /// Odpovídá XSD typu <c>type_subtextitem</c>
    /// </summary>
    internal class DfSubTextItem : DfBase
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DfSubTextItem() : base()
        {
        }
        /// <summary>
        /// Text popisku
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Jméno ikony u textu.
        /// Použití se liší podle typu prvku.
        /// </summary>
        public string IconName { get; set; }
    }
    #endregion
    #region Bázové třídy Controlů
    /// <summary>
    /// Bázová třída pro všechny samostatné controly s neměnným textem a ikonou - Label, Button, CheckBox, ....<br/>
    /// Odpovídá XSD typu <c>type_base_input_text_control</c>
    /// <para/>
    /// Tato třída přináší property <see cref="Text"/>,  <see cref="IconName"/> a <see cref="Alignment"/>.
    /// </summary>
    internal class DfBaseInputTextControl : DfBaseInputControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DfBaseInputTextControl() : base()
        {
            this.Alignment = ContentAlignmentType.Default;
        }
        /// <summary>
        /// Text popisku uvnitř controlu = text v Buttonu, text v CheckBoxu
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Jméno ikony odstavce nebo prvku (v titulku stránky, v titulku odstavce, ikona Buttonu, atd).
        /// Použití se liší podle typu prvku.
        /// </summary>
        public string IconName { get; set; }
        /// <summary>
        /// Zarovnání textu v rámci prostoru
        /// </summary>
        public ContentAlignmentType? Alignment { get; set; }
    }
    /// <summary>
    /// Bázová třída pro všechny samostatné interaktivní controly, které mohou mít vedle vlastního prvku (Control) i prvek typu Label.
    /// Typicky jde o: TextBox, ComboBox, TokenEdit, atd ...<br/>
    /// Odpovídá XSD typu <c>type_base_input_labeled_control</c>
    /// <para/>
    /// Tato třída přináší property <see cref="Label"/>, <see cref="LabelPosition"/> a <see cref="LabelWidth"/>.
    /// </summary>
    internal class DfBaseLabeledInputControl : DfBaseInputControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DfBaseLabeledInputControl() : base()
        {
            this.Label = null;
            this.LabelPosition = LabelPositionType.Default;
            this.LabelWidth = null;
        }
        /// <summary>
        /// Text, popisující obsah políčka.
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// Umístění a zarovnání popisku (Labelu) vzhledem k souřadnicích controlu
        /// </summary>
        public LabelPositionType? LabelPosition { get; set; }
        /// <summary>
        /// Nejvyšší šířka prostoru pro Label
        /// </summary>
        public int? LabelWidth { get; set; }
        /// <summary>
        /// Styl pro Label (název, styl písma, velikost, barva popisku, barva textu a pozadí, atd)
        /// </summary>
        public DfControlStyle LabelStyle { get; set; }
        /// <summary>
        /// Souřadnice labelu
        /// </summary>
        public ControlBounds LabelDesignBounds { get; set; }
        /// <summary>
        /// Debug text
        /// </summary>
        protected override string DebugText { get { return $"{ControlType}; Name: '{Name}'; Label: '{Label}'"; } }
    }
    /// <summary>
    /// Bázová třída pro všechny samostatné interaktivní controly - TextBox, Button, CheckBox, ComboBox, ....<br/>
    /// Odpovídá XSD typu <c>type_base_input_control</c>
    /// <para/>
    /// Tato třída přináší property <see cref="Required"/>.
    /// </summary>
    internal class DfBaseInputControl : DfBaseControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DfBaseInputControl() : base()
        {
            this.Required = RequiredType.Default;
        }
        /// <summary>
        /// Povinnost vyplnění prvku
        /// </summary>
        public RequiredType? Required { get; set; }
        /// <summary>
        /// Explicitně zadaný název sloupce (datového prvku), jehož data tento prvek zobrazuje. 
        /// Běžně se nemusí zadávat, implicitně se data čtou a ukládají z/do datového sloupce se jménem podle atributu 'Name'. 
        /// Pokud ale je třeba jeden atribut/vztah zobrazit na více místech formuláře, pak se pro tyto různé controly zadá různé 'Name' (musí být unikátní), a použije se pro ně shodné 'ColumnName'.
        /// </summary>
        public string ColumnName { get; set; }
    }
    /// <summary>
    /// Bázová třída pro všechny samostatné controly bez ohledu na jejich vlastní interaktivitu - tedy Label, Picture, Panel; a dále i pro interaktivní controly: TextBox, Button, CheckBox, ComboBox, ...
    /// Neslouží jako podklad pro Containery! Ty se odvozují od <see cref="DfBase"/> a <see cref="DfBaseArea"/>.<br/>
    /// Odpovídá XSD typu <c>type_base_control</c>
    /// <para/>
    /// Tato třída přináší souřadnice <see cref="Bounds"/>.
    /// </summary>
    internal class DfBaseControl : DfBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DfBaseControl() : base()
        {
        }
        /// <summary>
        /// Druh vstupního prvku (Control).
        /// </summary>
        public virtual ControlType ControlType { get { return ControlType.None; } }
        /// <summary>
        /// Styl controlu (název, styl písma, velikost, barva popisku, barva textu a pozadí, atd)
        /// </summary>
        public DfControlStyle ControlStyle { get; set; }
        /// <summary>
        /// Umístění prvku. Výchozí je null.
        /// <para/>
        /// Pokud má prvek zadané souřadnice X a Y, pak je umístěn na požadované souřadnice = je Fixní, a není umisťován do sloupců a řádků plovoucího layoutu (FlowLayout).
        /// Souřadnice se vztahují k parentu (panel nebo grupa), ve kterém je prvek definován jako jeho Child element.
        /// Je možno ale zadat atribut 'ParentBounds' a do něj uvést jméno jiného sousedního prvku, který je umístěn do FlowLayoutu.
        /// Pak budou souřadnice 'Bounds' zdejšího prvku umístěny relativně do souřadnic daného Parent prvku.
        /// </summary>
        public Bounds Bounds { get; set; }
        /// <summary>
        /// Jméno prvku 'Name', který určuje souřadnice pro zdejší prvek, pokud ten je umístěn fixně = má definované souřadnice 'X' a 'Y'.
        /// </summary>
        public string ParentBounds { get; set; }
        /// <summary>
        /// Index sloupce, na kterém je prvek umístěn v režimu FlowLayout. Ten se použije, pokud prvky nemají exaktně dané souřadnice, spolu s atributem 'ColumnWidths'.
        /// </summary>
        public int? ColIndex { get; set; }
        /// <summary>
        /// Počet sloupců, které prvek obsazuje v FlowLayoutu. Ten se použije, pokud prvky nemají exaktně dané souřadnice, spolu s atributem 'ColumnWidths'.
        /// </summary>
        public int? ColSpan { get; set; }
        /// <summary>
        /// Počet řádků, které prvek obsazuje v FlowLayoutu. Ten se použije, pokud prvky nemají exaktně dané souřadnice.
        /// </summary>
        public int? RowSpan { get; set; }
        /// <summary>
        /// Umístění prvku vodorovně v rámci sloupce v případě, kdy šířka prvku je menší než šířka sloupce. Řeší tedy zarovnání controlu ve sloupci, nikoli zarovnání obsahu (textu) v rámci controlu.
        /// </summary>
        public HPositionType? HPosition { get; set; }
        /// <summary>
        /// Umístění prvku svislé v rámci řádku v případě, kdy výška prvku je menší než výška sloupce. Řeší tedy zarovnání controlu v řádku, nikoli zarovnání obsahu (textu) v rámci controlu.
        /// </summary>
        public VPositionType? VPosition { get; set; }
        /// <summary>
        /// Rozšíření controlu do okolního prostoru pro labely, pokud labely nejsou použity
        /// </summary>
        public ExpandControlType? ExpandControl { get; set; }
        /// <summary>
        /// Souřadnice samotného vstupního prvku
        /// </summary>
        public ControlBounds ControlDesignBounds { get; set; }
        /// <summary>
        /// Debug text
        /// </summary>
        protected override string DebugText { get { return $"{ControlType}; Name: '{Name}'"; } }
    }
    /// <summary>
    /// Bázová třída pro všechny prvky - controly i containery.<br/>
    /// Odpovídá XSD typu <c>type_base</c>
    /// <para/>
    /// Tato třída přináší základní property: <see cref="Name"/>.
    /// </summary>
    internal class DfBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DfBase()
        {
        }
        /// <summary>
        /// Klíčové jméno prvku pro jeho jednoznačnou identifikaci.
        /// Odstavce a prvky musí mít <see cref="Name"/> jednoznačné přes celý formulář = přes všechny záložky.
        /// Subprvky (=položky editačního stylu) a subbuttony (tlačítka v <see cref="ControlType.TextBoxButton"/>) mají <see cref="Name"/> jednoznačné jen v rámci svého prvku.
        /// Pokud více controlů má zobrazovat data jednoho datového sloupce, použije se atribut <see cref="DfBaseInputControl.ColumnName"/>.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Stav bloku nebo prvku (viditelnost, editovatelnost)
        /// </summary>
        public ControlStateType? State { get; set; }
        /// <summary>
        /// Titulek ToolTipu.
        /// </summary>
        public string ToolTipTitle { get; set; }
        /// <summary>
        /// Text ToolTipu.
        /// </summary>
        public string ToolTipText { get; set; }
        /// <summary>
        /// Výraz určující Invisible
        /// </summary>
        public string Invisible { get; set; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.DebugText; }
        /// <summary>
        /// Debug text
        /// </summary>
        protected virtual string DebugText { get { return $"Name: '{Name}'"; } }
    }
    #endregion
    #region Podpůrné třídy a enumy
    /// <summary>
    /// Souřadnice
    /// </summary>
    public sealed class Bounds
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Bounds() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        public Bounds(Int32? left, Int32? top)
        {
            this.Left = left;
            this.Top = top;
            this.Width = null;
            this.Height = null;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public Bounds(Int32? left, Int32? top, Int32? width, Int32? height)
        {
            this.Left = left;
            this.Top = top;
            this.Width = width.HasValue ? new Int32P(width.Value, false) : null;
            this.Height = height.HasValue ? new Int32P(height.Value, false) : null;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public Bounds(Int32? left, Int32? top, Int32P? width, Int32P? height)
        {
            this.Left = left;
            this.Top = top;
            this.Width = width;
            this.Height = height;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="source"></param>
        public Bounds(Bounds source)
        {
            this.Left = source?.Left;
            this.Top = source?.Top;
            this.Width = source?.Width;
            this.Height = source?.Height;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Left: {Left}; Top: {Top}; Width: {Width}; Height: {Height}";
        }
        /// <summary>
        /// Obsahuje true, když žádná souřadnice není naplněna (všechny jsou null).
        /// </summary>
        public bool IsEmpty { get { return !(this.Left.HasValue || this.Top.HasValue || this.Width.HasValue || this.Height.HasValue); } }
        /// <summary>
        /// Je určena souřadnice <see cref="Left"/> i <see cref="Top"/>
        /// </summary>
        public bool HasLocation { get { return this.Left.HasValue && this.Top.HasValue; } }
        /// <summary>
        /// Je určena souřadnice <see cref="Width"/> i <see cref="Height"/>
        /// </summary>
        public bool HasSize { get { return this.Width.HasValue && this.Height.HasValue; } }
        /// <summary>
        /// Uloží do sebe dodané hodnoty. Pokud bude dodáno null, uloží jej jako hodnotu (nepřeskočí ji).
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Fill(Int32? left, Int32? top, Int32? width, Int32? height)
        {
            this.Left = left;
            this.Top = top;
            this.Width = width.HasValue ? new Int32P(width.Value, false) : null;
            this.Height = height.HasValue ? new Int32P(height.Value, false) : null;
        }
        /// <summary>
        /// Uloží do sebe dodané hodnoty. Pokud bude dodáno null, uloží jej jako hodnotu (nepřeskočí ji).
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Fill(Int32? left, Int32? top, Int32P? width, Int32P? height)
        {
            this.Left = left;
            this.Top = top;
            this.Width = width;
            this.Height = height;
        }
        /// <summary>
        /// Left
        /// </summary>
        public Int32? Left { get; set; }
        /// <summary>
        /// Top
        /// </summary>
        public Int32? Top { get; set; }
        /// <summary>
        /// Width: může být zadána hodnota absolutní, nebo procentuální. Viz příznak <see cref="Int32P.IsPercent"/>.
        /// Může zde být null.
        /// </summary>
        public Int32P? Width { get; set; }
        /// <summary>
        /// Height
        /// </summary>
        public Int32P? Height { get; set; }
        /// <summary>
        /// Right (pouze pokud <see cref="Left"/> má hodnotu, <see cref="Width"/> má hodnotu, a tato je pixelová)
        /// </summary>
        public int? Right { get { var l = Left; var w = Width; return ((l.HasValue && w.HasValue && w.Value.IsPixel) ? (int?)(l.Value + w.Value.NumberPixel.Value) : (int?)null); } }
        /// <summary>
        /// Bottom (pouze pokud <see cref="Top"/> má hodnotu, <see cref="Height"/> má hodnotu, a tato je pixelová)
        /// </summary>
        public int? Bottom { get { var t = Top; var h = Height; return ((t.HasValue && h.HasValue && h.Value.IsPixel) ? (int?)(t.Value + h.Value.NumberPixel.Value) : (int?)null); } }
    }
    /// <summary>
    /// Pozice a velikost controlu
    /// </summary>
    public sealed class ControlBounds
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ControlBounds() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public ControlBounds(int left, int top, int width, int height)
        {
            this.Left = left;
            this.Top = top;
            this.Width = width;
            this.Height = height;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Left: {Left}; Top: {Top}; Width: {Width}; Height: {Height}";
        }
        /// <summary>
        /// Left
        /// </summary>
        public int Left { get; set; }
        /// <summary>
        /// Top
        /// </summary>
        public int Top { get; set; }
        /// <summary>
        /// Width
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// Height
        /// </summary>
        public int Height { get; set; }
    }
    /// <summary>
    /// Velikost kontejneru
    /// </summary>
    public sealed class ContainerSize
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ContainerSize() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="clientWidth"></param>
        /// <param name="clientHeight"></param>
        public ContainerSize(int clientWidth, int clientHeight)
        {
            this.ClientWidth = clientWidth;
            this.ClientHeight = clientHeight;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="clientWidth"></param>
        /// <param name="clientHeight"></param>
        /// <param name="containerHeight"></param>
        /// <param name="containerWidth"></param>
        public ContainerSize(int clientWidth, int clientHeight, int containerWidth, int containerHeight)
        {
            this.ClientWidth = clientWidth;
            this.ClientHeight = clientHeight;
            this.ContainerWidth = containerWidth;
            this.ContainerHeight = containerHeight;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ClientWidth: {ClientWidth}; ClientHeight: {ClientHeight}";
        }
        /// <summary>
        /// <see cref="ClientWidth"/>: šířka vnitřního prostoru, kde jsou umístěny controly
        /// </summary>
        public int ClientWidth { get; set; }
        /// <summary>
        /// <see cref="ClientHeight"/>: výška vnitřního prostoru, kde jsou umístěny controly
        /// </summary>
        public int ClientHeight { get; set; }
        /// <summary>
        /// <see cref="ContainerWidth"/>: šířka vnějšího prostoru, který obsazuje container
        /// </summary>
        public int ContainerWidth { get; set; }
        /// <summary>
        /// <see cref="ContainerHeight"/>: výška vnějšího prostoru, který obsazuje container
        /// </summary>
        public int ContainerHeight { get; set; }
    }
    /// <summary>
    /// Umístění
    /// </summary>
    public sealed class Location
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Location() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        public Location(int? left, int? top)
        {
            this.Left = left;
            this.Top = top;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Left: {Left}; Top: {Top}";
        }
        /// <summary>
        /// Obsahuje true, když žádná souřadnice není naplněna (všechny jsou null).
        /// </summary>
        public bool IsEmpty { get { return !(this.Left.HasValue || this.Top.HasValue); } }
        /// <summary>
        /// Left
        /// </summary>
        public int? Left { get; set; }
        /// <summary>
        /// Top
        /// </summary>
        public int? Top { get; set; }
    }
    /// <summary>
    /// Umístění
    /// </summary>
    public sealed class Size
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Size() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public Size(int? width, int? height)
        {
            this.Width = width;
            this.Height = height;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Width: {Width}; Height: {Height}";
        }
        /// <summary>
        /// Obsahuje true, když žádná souřadnice není naplněna (všechny jsou null).
        /// </summary>
        public bool IsEmpty { get { return !(this.Width.HasValue || this.Height.HasValue); } }
        /// <summary>
        /// Width
        /// </summary>
        public int? Width { get; set; }
        /// <summary>
        /// Height
        /// </summary>
        public int? Height { get; set; }
    }
    /// <summary>
    /// Hodnota typu Int32 <see cref="Number"/> s příznakem <see cref="IsPercent"/>, zda jde o procenta.
    /// </summary>
    public struct Int32P : IEquatable<Int32P>
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="number"></param>
        public Int32P(int number)
        {
            this.__Number = number;
            this.__IsPercent = false;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="number"></param>
        /// <param name="isPercent"></param>
        public Int32P(int number, bool isPercent)
        {
            this.__Number = number;
            this.__IsPercent = isPercent;
        }
        /// <summary>
        /// Prázdný prvek
        /// </summary>
        public static Int32P Empty { get { return new Int32P(0); } }
        /// <summary>
        /// Počet
        /// </summary>
        private int __Number;
        /// <summary>
        /// Příznak procent (true) / hodnoty (false)
        /// </summary>
        private bool __IsPercent;
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Number}{(IsPercent ? "%" : "")}";
        }
        /// <summary>
        /// Hodnota
        /// </summary>
        public int Number { get { return __Number; } set { __Number = value; } }
        /// <summary>
        /// Jde o pixel?
        /// </summary>
        public bool IsPixel { get { return !__IsPercent; } set { __IsPercent = !value; } }
        /// <summary>
        /// Jde o procento?
        /// </summary>
        public bool IsPercent { get { return __IsPercent; } set { __IsPercent = value; } }
        /// <summary>
        /// Číslo, pokud je v pixelech, jinak null
        /// </summary>
        public int? NumberPixel { get { return (!__IsPercent ? (int?)Number : (int?)null); } }
        /// <summary>
        /// Číslo, pokud je v procentech, jinak null
        /// </summary>
        public int? NumberPercent { get { return (__IsPercent ? (int?)Number : (int?)null); } }
        /// <summary>
        /// Hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.Number.GetHashCode() ^ (IsPercent ? 0x40000000 : 0);
        }
        /// <summary>
        /// Vrací Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (!(obj is Int32P)) return false;
            return _IsEquals(this, (Int32P)obj);
        }
        /// <summary>
        /// Vrací Equals
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        private static bool _IsEquals(Int32P value1, Int32P value2)
        {
            return (value1.Number == value2.Number) && (value1.IsPercent = value2.IsPercent);
        }
        /// <summary>
        /// Parsuje dodaný text na hodnotu <see cref="Int32P"/>.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryParse(string text, out Int32P value)
        {
            value = default(Int32P);
            if (String.IsNullOrEmpty(text)) return false;
            bool isPercent = text.Contains("%");
            if (isPercent) text = text.Replace("%", "");
            int number = 0;
            if (String.IsNullOrEmpty(text) || !Int32.TryParse(text, out number)) return false;
            value = new Int32P(number, isPercent);
            return true;
        }
        /// <summary>
        /// Shodnost s jinou instancí
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        bool IEquatable<Int32P>.Equals(Int32P other)
        {
            return _IsEquals(this, other);
        }
    }
    /// <summary>
    /// Styl pro control, typicky kalíšek. Budeme podporovat i rozšířený způsob zadání stylu.
    /// </summary>
    public sealed class DfControlStyle
    {
        /// <summary>
        /// Název kalíšku = definuje parametry stylu
        /// </summary>
        public string StyleName { get; set; }
        /// <summary>
        /// Styl písma pro control
        /// </summary>
        public FontStyleType? ControlFontStyle { get; set; }
        /// <summary>
        /// Změna velikosti písma proti defaultnímu, pro control
        /// </summary>
        public float? ControlFontSizeDelta { get; set; }
        /// <summary>
        /// Styl písma pro label u controlu
        /// </summary>
        public FontStyleType? LabelFontStyle { get; set; }
        /// <summary>
        /// Změna velikosti písma proti defaultnímu, pro label u controlu
        /// </summary>
        public float? LabelFontSizeDelta { get; set; }
    }
    /// <summary>
    /// Okraje
    /// </summary>
    public sealed class Margins
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Margins() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="all"></param>
        public Margins(int all)
        {
            this.Left = all;
            this.Top = all;
            this.Right = all;
            this.Bottom = all;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="horizontal"></param>
        /// <param name="vertical"></param>
        public Margins(int horizontal, int vertical)
        {
            this.Left = horizontal;
            this.Top = vertical;
            this.Right = horizontal;
            this.Bottom = vertical;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public Margins(int left, int top, int right, int bottom)
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="source"></param>
        public Margins(Margins source)
        {
            this.Left = source?.Left ?? 0;
            this.Top = source?.Top ?? 0;
            this.Right = source?.Right?? 0;
            this.Bottom = source?.Bottom ?? 0;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (IsEmpty) return "Empty";
            if (IsAll)return $"All: {Left}";
            return $"Left: {Left}; Top: {Top}; Right: {Right}; Bottom: {Bottom}";
        }
        /// <summary>
        /// Okraj vlevo
        /// </summary>
        public int Left { get; set; }
        /// <summary>
        /// Okraj nahoře
        /// </summary>
        public int Top { get; set; }
        /// <summary>
        /// Okraj vpravo
        /// </summary>
        public int Right { get; set; }
        /// <summary>
        /// Okraj dole
        /// </summary>
        public int Bottom { get; set; }
        /// <summary>
        /// Je empty = vše je 0
        /// </summary>
        private bool IsEmpty { get { return (this.Left == 0 && this.Top == 0 && this.Right == 0 && this.Bottom == 0); } }
        /// <summary>
        /// Je všud stejný = všechny hodnoty jsou stejné
        /// </summary>
        private bool IsAll { get { return (this.Left == this.Top && this.Top == this.Right && this.Right == this.Bottom); } }
    }
    /// <summary>
    /// Informace o stylu písma
    /// </summary>
    public class DfFontInfo
    {
        /// <summary>
        /// Styl (tučné, kurzíva, obyčejné)
        /// </summary>
        public FontStyleType? Style { get; set; }
        /// <summary>
        /// Rozdíl velikosti písma jako koeficient: null = 1f = default = beze změny; hodnoty větší než 1 zvětšují písmo (1.25f = +25% proti defaultu, 0.80f = -20% proti defaultu)
        /// </summary>
        public float? SizeRatio { get; set; }
    }
    /// <summary>
    /// Verze formátu definice frm.xml
    /// Odpovídá XSD typu <c>format_version_enum</c>
    /// </summary>
    public enum FormatVersionType
    {
        /// <summary>
        /// Default = Version4
        /// </summary>
        Default,
        /// <summary>
        /// Version1 = Infragistic
        /// </summary>
        Version1,
        /// <summary>
        /// Version2 = Infragistic
        /// </summary>
        Version2,
        /// <summary>
        /// Version3 = Infragistic
        /// </summary>
        Version3,
        /// <summary>
        /// Version4 = DevExpress
        /// </summary>
        Version4,
    }
    /// <summary>
    /// Umístění a zarovnání popisku (Labelu) vzhledem k souřadnicích controlu.
    /// Odpovídá XSD typu <c>label_position_enum</c>
    /// </summary>
    public enum LabelPositionType
    {
        /// <summary>
        /// Default = Nikde = výchozí, anebo u konkrétního prvku = podle nastavení formuláře
        /// </summary>
        Default,
        /// <summary>
        /// Nikde
        /// </summary>
        None,
        /// <summary>
        /// Vlevo před textem, písmo zarovnané doleva
        /// </summary>
        BeforeLeft,
        /// <summary>
        /// Vlevo před textem, písmo zarovnané doprava
        /// </summary>
        BeforeRight,
        /// <summary>
        /// Vpravo za textem, zarovnaný doleva (tam se typicky píšou jednotky, např. Kč, KWh atd)
        /// </summary>
        After,
        /// <summary>
        /// Nahoře nad textem, písmo zarovnané doleva
        /// </summary>
        Top,
        /// <summary>
        /// Dole pod textem, písmo zarovnané doleva
        /// </summary>
        Bottom
    }
    /// <summary>
    /// Styl písma
    /// </summary>
    [Flags]
    public enum FontStyleType
    {
        /// <summary>
        /// Standardní
        /// </summary>
        Regular = 0,
        /// <summary>
        /// Bold
        /// </summary>
        Bold = 0x01,
        /// <summary>
        /// Italic
        /// </summary>
        Italic = 0x02,
        /// <summary>
        /// Underlined
        /// </summary>
        Underlined = 0x04
    }
    /// <summary>
    /// Zarovnání obsahu do jeho daného prostoru v obou osách (X + Y).
    /// Numerickými hodnotami se shoduje s enumem <see cref="AlignmentSideType"/>.
    /// Odpovídá XSD typu <c>content_alignment_enum</c>
    /// </summary>
    [Flags]
    public enum ContentAlignmentType
    {
        /// <summary>
        /// Nahoře vlevo
        /// </summary>
        TopLeft = AlignmentSideType.VTop | AlignmentSideType.HLeft,
        /// <summary>
        /// Nahoře uprostřed
        /// </summary>
        TopCenter = AlignmentSideType.VTop | AlignmentSideType.HCenter,
        /// <summary>
        /// Nahoře vpravo
        /// </summary>
        TopRight = AlignmentSideType.VTop | AlignmentSideType.HRight,
        /// <summary>
        /// Svisle na střed, vlevo
        /// </summary>
        MiddleLeft = AlignmentSideType.VMiddle | AlignmentSideType.HLeft,
        /// <summary>
        /// Svisle na střed, uprostřed
        /// </summary>
        MiddleCenter = AlignmentSideType.VMiddle | AlignmentSideType.HCenter,
        /// <summary>
        /// Svisle na střed, vpravo
        /// </summary>
        MiddleRight = AlignmentSideType.VMiddle | AlignmentSideType.HRight,
        /// <summary>
        /// Dole vlevo
        /// </summary>
        BottomLeft = AlignmentSideType.VBottom | AlignmentSideType.HLeft,
        /// <summary>
        /// Dole na střed
        /// </summary>
        BottomCenter = AlignmentSideType.VBottom | AlignmentSideType.HCenter,
        /// <summary>
        /// Dole vpravo
        /// </summary>
        BottomRight = AlignmentSideType.VBottom | AlignmentSideType.HRight,

        /// <summary>
        /// Defaultně = Nahoře vlevo
        /// </summary>
        Default = TopLeft
    }
    /// <summary>
    /// Umístění prvku vodorovně (Horizontálně) v rámci sloupce v případě, kdy šířka prvku je menší než šířka sloupce.
    /// Řeší tedy zarovnání controlu ve sloupci, nikoli zarovnání obsahu (textu) v rámci controlu.
    /// </summary>
    public enum HPositionType
    {
        /// <summary>
        /// Default = <see cref="Left"/>.
        /// </summary>
        Default,
        /// <summary>
        /// Doleva.
        /// </summary>
        Left,
        /// <summary>
        /// Na střed.
        /// </summary>
        Center,
        /// <summary>
        /// Doprava.
        /// </summary>
        Right
    }
    /// <summary>
    /// Umístění prvku svisle (Vertikálně) v rámci řádku v případě, kdy výška prvku je menší než výška řádku.
    /// Řeší tedy zarovnání controlu v řádku, nikoli zarovnání obsahu (textu) v rámci controlu.
    /// </summary>
    public enum VPositionType
    {
        /// <summary>
        /// Default = <see cref="Top"/>.
        /// </summary>
        Default,
        /// <summary>
        /// Nahoru
        /// </summary>
        Top,
        /// <summary>
        /// Na střed.
        /// </summary>
        Center,
        /// <summary>
        /// Dolů.
        /// </summary>
        Bottom
    }
    /// <summary>
    /// Rozšíření vlastního controlu do míst, kde je nevyužitý prostor protože tam není label
    /// </summary>
    public enum ExpandControlType
    {
        /// <summary>
        /// Default = <see cref="None"/>
        /// </summary>
        Default = None,
        /// <summary>
        /// Žádné rozšíření, využije standardní prostor pro Control
        /// </summary>
        None = 0,
        /// <summary>
        /// Doleva, pokud vlevo není Label
        /// </summary>
        Left = 0x01,
        /// <summary>
        /// Nahoru, pokud nahoře není Label
        /// </summary>
        Top = 0x02,
        /// <summary>
        /// Doprava, pokud vpravo není Label
        /// </summary>
        Right = 0x04,
        /// <summary>
        /// Dolů, pokud dole není Label
        /// </summary>
        Bottom = 0x08,
        /// <summary>
        /// Vlevo i vpravo
        /// </summary>
        FullWidth = Left | Right,
        /// <summary>
        /// Nahoru i dolů
        /// </summary>
        FullHeight = Top | Bottom,
        /// <summary>
        /// Celý prostor, kde nejsou labely
        /// </summary>
        All = FullWidth | FullHeight
    }
    /// <summary>
    /// Umístění obrázku vzhledem k prostoru
    /// </summary>
    public enum BackImagePositionType
    {
        /// <summary>
        /// Default = BottomRight: Obrázek bude vykreslen do pravého dolního rohu.
        /// </summary>
        Default,
        /// <summary>
        /// Obrázek nebude zobrazen.
        /// </summary>
        None,
        /// <summary>
        /// Obrázek bude vykreslen do levého horního rohu.
        /// </summary>
        TopLeft,
        /// <summary>
        /// Obrázek bude vykreslen k hornímu okraji na vodorovný střed.
        /// </summary>
        TopCenter,
        /// <summary>
        /// Obrázek bude vykreslen do pravého horního rohu.
        /// </summary>
        TopRight,
        /// <summary>
        /// Obrázek bude vykreslen do doleva, na svislý střed.
        /// </summary>
        MiddleLeft,
        /// <summary>
        /// Obrázek bude vykreslen přímo na střed oblasti.
        /// </summary>
        MiddleCenter,
        /// <summary>
        /// Obrázek bude vykreslen doprava, na svislý střed.
        /// </summary>
        MiddleRight,
        /// <summary>
        /// Obrázek bude vykreslen do levého dolního rohu.
        /// </summary>
        BottomLeft,
        /// <summary>
        /// Obrázek bude vykreslen ke spodnímu okraji na vodorovný střed.
        /// </summary>
        BottomCenter,
        /// <summary>
        /// Obrázek bude vykreslen do pravého dolního rohu.
        /// </summary>
        BottomRight,
        /// <summary>
        /// Obrázek se vykreslí na pozadí opakovaně vedle sebe i pod sebou jako tapeta.
        /// </summary>
        Tile
    }
    /// <summary>
    /// Zarovnání obsahu, jednotlivé osy a směry, bez kombinací. 
    /// Numerickými hodnotami se shoduje s enumem <see cref="ContentAlignmentType"/>
    /// Nemá odpovídající XSD typ.
    /// </summary>
    [Flags]
    public enum AlignmentSideType
    {
        /// <summary>
        /// Nezarovnáno
        /// </summary>
        None = 0,
        /// <summary>
        /// Horizontálně: vlevo
        /// </summary>
        HLeft = 0b00000100,
        /// <summary>
        /// Horizontálně: uprostřed
        /// </summary>
        HCenter = 0b00000010,
        /// <summary>
        /// Horizontálně: vpravo
        /// </summary>
        HRight = 0b00000001,
        /// <summary>
        /// Vertikálně: nahoru
        /// </summary>
        VTop = 0b01000000,
        /// <summary>
        /// Vertikálně: na střed
        /// </summary>
        VMiddle = 0b00100000,
        /// <summary>
        /// Vertikálně: dole
        /// </summary>
        VBottom = 0b00010000
    }
    /// <summary>
    /// Styl zobrazení Title řádku.
    /// Odpovídá XSD typu <c>title_style_enum</c>
    /// </summary>
    public enum TitleStyleType
    {
        /// <summary>
        /// Defaultní = TextWithLineBottom.
        /// </summary>
        Default,
        /// <summary>
        /// Pouze výrazný text, bez linky.
        /// </summary>
        TextOnly,
        /// <summary>
        /// Text a linka pod textem v celé šířce.
        /// </summary>
        TextWithLineBottom,
        /// <summary>
        /// Text a linka vpravo od textu ve výšce linky řádku.
        /// </summary>
        TextWithLineRight,
        /// <summary>
        /// Text a linka nad textem.
        /// </summary>
        TextWithLineAbove
    }
    /// <summary>
    /// Styl zobrazení CheckBoxu.
    /// Odpovídá XSD typu <c>checkbox_style_enum</c>
    /// </summary>
    public enum CheckBoxStyleType
    {
        /// <summary>
        /// Default = CheckBox
        /// </summary>
        Default,
        /// <summary>
        /// Klasický CheckBox: čtvercové zaškrtávátko s křížkem uvnitř.
        /// </summary>
        CheckBox,
        /// <summary>
        /// Button s aktivním stavem Down (zmáčknuté výrazné tlačítko).
        /// </summary>
        DownButton,
        /// <summary>
        /// RadioButton: kolečko s kulatým puntíkem uvnitř.
        /// </summary>
        RadioButton,
        /// <summary>
        /// Zapínač ve stylu Android; TrackBar: přesouvající se kulička zleva doprava se zvýrazněním barvy ON.
        /// </summary>
        ToggleSwitch
    }
    /// <summary>
    /// Styl zobrazení ComboBoxu.
    /// Odpovídá XSD typu <c>combobox_style_enum</c>
    /// </summary>
    public enum ComboBoxStyleType
    {
        /// <summary>
        /// Default = List
        /// </summary>
        Default,
        /// <summary>
        /// Do políčka je možno vepisovat text, anebo je možno vybrat z nabídky. Nemá ikonu.
        /// </summary>
        ListEdit,
        /// <summary>
        /// Do políčka je možno pouze vybrat hodnotu z nabídky. Nemá ikonu.
        /// </summary>
        List,
        /// <summary>
        /// Do políčka je možno pouze vybrat hodnotu z nabídky. Zobrazuje ikonu i text.
        /// </summary>
        IconTextList,
        /// <summary>
        /// Do políčka je možno pouze vybrat hodnotu z nabídky. Zobrazuje pouze ikonu.
        /// </summary>
        IconList,
        /// <summary>
        /// Postup procesu - všechny hodnoty editačního stylu jsou zobrazeny, s jedna z nich je zvýrazněna
        /// </summary>
        BreadCrumb
    }
    /// <summary>
    /// Styl viditelnosti buttonů v rámci prvku <see cref="DfTextBoxButton"/>.
    /// Odpovídá XSD typu <c>buttons_visibility_enum</c>
    /// </summary>
    public enum ButtonsVisibilityType
    {
        /// <summary>
        /// Defaultní = jen v aktivním prvku (Focus nebo MouseOn), vyjma stavu Disabled
        /// </summary>
        Default,
        /// <summary>
        /// Jen v aktivním prvku (Focus nebo MouseOn), vyjma stavu Disabled
        /// </summary>
        OnlyActive,
        /// <summary>
        /// Viditelné vždy
        /// </summary>
        VisibleAlways,
        /// <summary>
        /// Neviditelné
        /// </summary>
        Invisible
    }
    /// <summary>
    /// Akce, kterou button provede.<br/>
    /// Odpovídá XSD typu <c>button_action_enum</c>
    /// </summary>
    public enum ButtonActionType
    {
        /// <summary>
        /// Default = Click = Odešle kliknutí na server
        /// </summary>
        Default,
        /// <summary>
        /// Click = Odešle kliknutí na server
        /// </summary>
        Click,
        /// <summary>
        /// Odešle Update dat na server
        /// </summary>
        Update,
        /// <summary>
        /// Ověří vyplněnost Required polí
        /// </summary>
        ClickCheckRequired,
        /// <summary>
        /// Spustí funkci, název funkce je v atributu ActionData
        /// </summary>
        RunFunction,
        /// <summary>
        /// Zavře okno, protože je zima a táhne nám na záda
        /// </summary>
        Close
    }
    /// <summary>
    /// Akce, kterou button provede.<br/>
    /// Odpovídá XSD typu <c>button_action_enum</c>
    /// </summary>
    public enum SubButtonActionType
    {
        /// <summary>
        /// Default = Click = Odešle kliknutí na server
        /// </summary>
        Default,
        /// <summary>
        /// Click = Odešle kliknutí na server
        /// </summary>
        Click,
        /// <summary>
        /// Otevře editor aktuální hodnoty (kalendář, Word, Kalkulačka), podle ActionData
        /// </summary>
        OpenEditor,
        /// <summary>
        /// Otevře FileDialog, podle ActionData
        /// </summary>
        FileDialog,
        /// <summary>
        /// Akce s Clipboardem, podle ActionData
        /// </summary>
        ClipboardAction,
        /// <summary>
        /// Akce se vztaženým záznamem
        /// </summary>
        RelationRecord,
        /// <summary>
        /// Akce se vztaženým dokumentem
        /// </summary>
        RelationDocument,
        /// <summary>
        /// 
        /// </summary>
        xxx
    }
    /// <summary>
    /// Stav prvku. 
    /// Jde o Flags. Lze je sčítat z celé hierarchie containerů (OR), výsledek popisuje stav nejvyššího prvku.
    /// Tedy pokud jeden jediný prvek v hierarchii je <see cref="Invisible"/>, pak finální prvek je neviditelný.
    /// Obdobně <see cref="Disabled"/> nebo <see cref="ReadOnly"/>.
    /// Pokud součet všech hodnot je <see cref="Default"/>, pak prvek je viditelný a editovatelný.
    /// </summary>
    [Flags]
    public enum ControlStateType
    {
        /// <summary>
        /// Výchozí = Enabled + Visible + TabStop
        /// </summary>
        Default = 0,
        /// <summary>
        /// Prvek je viditelný a editovatelný, a TAB na něm zastavuje.
        /// </summary>
        Enabled = 0,
        /// <summary>
        /// Prvek je neviditelný.
        /// </summary>
        Invisible = 0x0001,
        /// <summary>
        /// Prvek je ReadOnly: lze do něj dát focus, ale nelze změnit jeho hodnotu.
        /// Na Button lze kliknout a provede akci.
        /// Kliknutí na CheckBox nezmění jeho stav.
        /// DateTime otevře kalendář, ale nezmění svoji hodnotu.
        /// Vztah otevře vztažený záznam.
        /// A obdobně pro jiné typy prvků.
        /// </summary>
        ReadOnly = 0x0002,
        /// <summary>
        /// Prvek je Disabled: nelze do něj dát Focus.
        /// Na Button nelze kliknout = neprovede akci.
        /// DateTime neotevře kalendář.
        /// Vztah neotevře vztažený záznam.
        /// </summary>
        Disabled = 0x0004,
        /// <summary>
        /// Pokud je tento příznak aktivní, pak tento prvek je přeskakován při pohybu klávesnicí (jeho TabStop je false).
        /// Lze tedy hierarchicky zakázat TabStop pro samotný prvek, nebo pro určitý jeho Parent (a tím zakázat TabStop pro všechny prvky v Parent containeru).
        /// </summary>
        TabSkip = 0x0008
    }
    /// <summary>
    /// Styl jednoho bloku = odstavce
    /// </summary>
    public enum ContainerStyleType
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Běžný vnitřní TAB (odstavec), může / nemusí mít titulek (podle přítomnosti textu <see cref="DfBaseInputTextControl.Text"/>)
        /// </summary>
        Default,
        /// <summary>
        /// Vrcholový container, reprezentuje celý formulář
        /// </summary>
        Form,
        /// <summary>
        /// Záhlaví stránky. Ignoruje souřadnice 
        /// </summary>
        Page,
        /// <summary>
        /// Běžný panel, jako <see cref="Default"/>
        /// </summary>
        Panel,
        /// <summary>
        /// Grupa uvnitř panelu
        /// </summary>
        Group
    }
    /// <summary>
    /// Stav Collapsed panelu = stav a možnost jeho minimalizace a zvětšení na plnou velikost.<br/>
    /// Odpovídá XSD typu <c>panel_collapse_state_enum</c>
    /// </summary>
    public enum PanelCollapseState
    {
        /// <summary>
        /// Panel nelze minimalizovat, je ve standardní velikosti
        /// </summary>
        Default,
        /// <summary>
        /// Panel je zobrazen ve standardní velikosti, ale uživatel jej může interaktivně minimalizovat = přejde do stavu <see cref="IsCollapsed"/>
        /// </summary>
        IsExpanded,
        /// <summary>
        /// Panel je zobrazen jako minimalizovaný, ale uživatel jej může interaktivně rozbalit do standardní velikosti = přejde do stavu <see cref="IsExpanded"/>
        /// </summary>
        IsCollapsed
    }
    /// <summary>
    /// Povinnost vyplnění prvku (zadání neprázdné hodnoty)
    /// </summary>
    public enum RequiredType
    {
        /// <summary>
        /// Běžná nepovinná hodnota
        /// </summary>
        Default,
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Důležitá hodnota, ale nepovinná (Warning)
        /// </summary>
        Important,
        /// <summary>
        /// Povinná hodnota (Error)
        /// </summary>
        Required,
        /// <summary>
        /// Běžná nepovinná hodnota, zpětná kompatibilita
        /// </summary>
        False,
        /// <summary>
        /// Povinná hodnota (Error), zpětná kompatibilita
        /// </summary>
        True
    }
    /// <summary>
    /// Druh vstupního prvku
    /// </summary>
    public enum ControlType
    {
        /// <summary>
        /// Žádný prvek
        /// </summary>
        None,
        /// <summary>
        /// Jen obsazený prostor, typicky jako vyhrazený prostor ve FlowLayoutu pro následné umístění controlu s danými Bounds
        /// </summary>
        PlaceHolder,
        /// <summary>
        /// Label
        /// </summary>
        Label,
        /// <summary>
        /// Titulkový řádek s možností vodorovné čáry a/nebo textu
        /// </summary>
        Title,
        /// <summary>
        /// TextBox - prostý bez buttonů (buttony má <see cref="TextBoxButton"/>), podporuje password i nullvalue
        /// </summary>
        TextBox,
        /// <summary>
        /// EditBox (Memo, Poznámka)
        /// </summary>
        EditBox,
        /// <summary>
        /// TextBox s buttony = pokrývá i Relation, Document, FileBox, CalendarBox a další textbox s přidanými tlačítky
        /// </summary>
        TextBoxButton,
        /// <summary>
        /// CheckBox: zaškrtávátko i DownButton i RadioButton i ToggleSwitch
        /// </summary>
        CheckBox,
        /// <summary>
        /// Klasické tlačítko
        /// </summary>
        Button,
        /// <summary>
        /// Button s přidaným rozbalovacím menu
        /// </summary>
        DropDownButton,
        /// <summary>
        /// ComboBox bez obrázků nebo s obrázky
        /// </summary>
        ComboBox,
        /// <summary>
        /// Posouvací hodnota, jedna nebo dvě
        /// </summary>
        TrackBar,
        /// <summary>
        /// Image
        /// </summary>
        Image,
        /// <summary>
        /// Bar code
        /// </summary>
        BarCode,
        /// <summary>
        /// Postup procesu - všechny hodnoty editačního stylu s jednou zvýrazněnou
        /// </summary>
        BreadCrumb,
        /// <summary>
        /// Obdoba BreadCrumb, jednotlivé kroky procesu s obrázky, názvy, komentáři
        /// </summary>
        StepProgressBar,
        /// <summary>
        /// Soupis více položek zobrazený v jednom řádku (adresáti emailu)
        /// </summary>
        TokenEdit,
        /// <summary>
        /// Malá tabulka
        /// </summary>
        Grid,
        /// <summary>
        /// Strom s prvky
        /// </summary>
        Tree,
        /// <summary>
        /// HTML prohlížeč
        /// </summary>
        HtmlContent
    }
    #endregion
}
