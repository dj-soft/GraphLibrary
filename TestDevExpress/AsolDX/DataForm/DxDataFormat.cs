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

namespace Noris.Clients.Win.Components.AsolDX.DataForm.Format
{
    /*    Deklarace dat  +  deklarace XSD  +  Načítací algoritmus
     1. Struktury dat jsou deklarovány zde, nemají žádnou logiku, jde jen  o obálku na data
     2. V podstatě identické struktury jsou deklarovány v XSD suoboru - stejná hierarchie tříd, stejnojmenné property ve stejných třídách, stejné enumy a jejich hodnoty
     3. Existuje statická třída DxDataFormatLoader, která z XML dokumentu (zadaného podle XSD schematu) vytvoří a vrátí odpovídající struktury zdejších tříd (C#)

          Hierarchie tříd
     - Třídy pro data jsou uspořádány hierarchicky
     - Bázová třída je DataFormatBase, nese pouze Name
     - Z ní jsou postupně děděny třídy pro Controly i pro Containery
     - Obdobná hierarchie je i ve schematu XSD (i tam je použita dědičnost typů!)

      TŘÍDA                                                            ÚČEL                                   PROPERTIES
    DataFormatBase                                                   Bázová                                 Name
      +  DataFormatBaseSubControl                                    Pro SubControly bez souřadnic          State, ToolTip, Invisible
           +  DataFormatSubButton                                    Pomocný SubButton, SubItem             Text, IconName, ActionName, ActionData
           +  DataFormatBaseControl                                  Pro všechny Controly                   ControlType, Bounds
                +  DataFormatControlLabel                            Label                                  Text, Alignment
                |    +  DataFormatControlTitle                       Titulek                                Style
                +  DataFormatBaseInputControl                        Obecný vstupní control                 Required
                |    +  DataFormatBaseTextControl                    Vstupní control s textem               Text, IconName, Alignment
                |    |    +  DataFormatControlCheckBox               Checkboxy více typů                    Style
                |    |    +  DataFormatControlButton                 Samostatné tlačítko                    ActionName, ActionData, HotKey
                |    |         +  DataFormatControlDropDownButton    Button s podnabídkou                   DropDownButtons
                |    +  DataFormatControlTextBox                     Běžný TextBox                          EditMask, Alignment
                |    |    +  DataFormatControlTextBoxButton          TextBox s tlačítky                     LeftButtons, RightButtons
                |    +  DataFormatComboBox                           ComboBox různých stylů                 Style, EditStyleName, ComboItems
                +  DataFormatBaseContainer                           Base pro containery                    Style, Margins, Controls
                     +  DataFormatContainerPanel                     Běžný panel vč. Nested                 
                     +  DataFormatContainerPage                      Stránka v PageSetu                     Title, IconName, Tabs
                     +  DataFormatContainerPageSet                   Sada stránek / záložky                 Pages
                     +  DataFormatContainerForm                      Kompletní formulář                     FormatVersion, MasterWidth, ..., Tabs
     

    */

    /*   Typy a vlastnosti


    CONTAINERY
    ----------
       Každý prvek má vlastnost: Name
            Bounds   Title   Icon  BackColor  ToolTip  Invisible
Form          -        -      A       A          -         -
PageSet       A        -      -       -          -         A
Page          -        A      A       A          A         A
Panel         A        A      A       A          -         A


    CONTROLY
    --------
       Každý prvek má vlastnosti: Name,  State,  ToolTip
               Bounds  BackColor  Invisible  Text   FontType  TextColor  Icon   Action  SubItems  SubButtons
SubItem          -       -             -      A       -          -         A
SubButton        -       -             -      A       -          -         A       A
Label            A       A             A      A       A          A         -
TitleRow         A       A             A      A       A          A         -
CheckBox         A       A             A      A       A          A         -
TextBox          A       A             A      -       A          A         -
TextBoxButton    A       A             A      -       A          A         -       -       -          A
ComboBox         A       A             A      -       A          A         -       -       A
Button           A       A             A      A       A          A         A       A
SplitButton      A       A             A      A       A          A         A       A       -          A


    */

    // Shared : třídy pouze nesou data, nemají funkcinalitu. Poměrně dobře korespondují s XML schematem DxDataFormat.Frm

    #region Konkrétní třídy Containerů
    /// <summary>
    /// Hlavičková informace o dokumentu: obsahuje pouze <see cref="XmlNamespace"/> a <see cref="FormatVersion"/>.
    /// </summary>
    public class DataFormatInfoForm
    {
        /// <summary>
        /// Namespace XML dokumentu
        /// </summary>
        public string XmlNamespace { get; set; }
        /// <summary>
        /// Formát tohoto souboru. Defaultní = 4
        /// </summary>
        public string FormatVersion { get; set; }
    }
    /// <summary>
    /// Celý DataForm
    /// </summary>
    public class DataFormatContainerForm : DataFormatBaseContainer
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormatContainerForm()
        {
            this.Style = ContainerStyleType.Form;
        }
        /// <summary>
        /// Namespace XML dokumentu
        /// </summary>
        public string XmlNamespace { get; set; }
        /// <summary>
        /// Formát tohoto souboru. Defaultní = 4
        /// </summary>
        public string FormatVersion { get; set; }
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
        /// Automaticky generovat labely atributů a vztahů, jejich umístění. Defaultní = <see cref="LabelPositionType.None"/>
        /// </summary>
        public LabelPositionType AutoLabelPosition { get; set; }
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
        /// Umístění labelů pro automaticky generované UDA atributy a vztahy. Defaultní = <see cref="LabelPositionType.Up"/>
        /// </summary>
        public LabelPositionType UdaLabelPosition { get; set; }
        /// <summary>
        /// Jednotlivé prvky - PageSet nebo Panely, vždy containery
        /// </summary>
        public DataFormatBaseContainer[] Tabs { get { return this.Controls?.OfType<DataFormatBaseContainer>().ToArray(); } }
    }
    /// <summary>
    /// Sada stránek = záložky
    /// </summary>
    public class DataFormatContainerPageSet : DataFormatBaseContainer
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormatContainerPageSet()
        {
            this.Style = ContainerStyleType.PageSet;
        }
        /// <summary>
        /// Stránky na záložkách
        /// </summary>
        public DataFormatContainerPage[] Pages { get { return this.Controls?.OfType<DataFormatContainerPage>().ToArray(); } }
    }
    /// <summary>
    /// Jedna stránka = obsahuje další Panely (nebo i PageSet nebo Controly).
    /// </summary>
    public class DataFormatContainerPage : DataFormatBaseContainer
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormatContainerPage()
        {
            this.Style = ContainerStyleType.Page;
        }
        /// <summary>
        /// Text titulku záhlaví
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Jméno ikony odstavce nebo prvku (v titulku stránky, v titulku odstavce, ikona Buttonu, atd).
        /// Použití se liší podle typu prvku.
        /// </summary>
        public string IconName { get; set; }
        /// <summary>
        /// Jednotlivé prvky - PageSet nebo Panely, vždy containery
        /// </summary>
        public DataFormatBaseContainer[] Tabs { get { return this.Controls?.OfType<DataFormatBaseContainer>().ToArray(); } }
    }
    /// <summary>
    /// Panel, může obsahovat controly i containery
    /// </summary>
    public class DataFormatContainerPanel : DataFormatBaseContainer
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormatContainerPanel() : base()
        {
            this.Style = ContainerStyleType.Panel;
        }
    }
    /// <summary>
    /// Panel, může obsahovat controly i containery
    /// </summary>
    public class DataFormatBaseContainer : DataFormatBaseControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormatBaseContainer() : base()
        {
            this.Style = ContainerStyleType.None;
        }
        /// <summary>
        /// Styl odstavce
        /// </summary>
        public ContainerStyleType Style { get; set; }
        /// <summary>
        /// Okraje = mezi krajem formuláře / Page / Panel a souřadnicí 0/0
        /// </summary>
        public Margins Margins { get; set; }
        /// <summary>
        /// Controly v rámci tohoto Containeru.
        /// Mohou zde být i další Containery.
        /// Default = null.
        /// </summary>
        public List<DataFormatBaseControl> Controls { get; set; }
    }
    #endregion
    #region Konkrétní třídy Controlů
    /// <summary>
    /// DxDataForm : Label
    /// </summary>
    public class DataFormatControlLabel : DataFormatBaseControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DataFormatControlLabel() : base()
        {
            this.ControlType = ControlType.Label;
            this.Alignment = ContentAlignmentType.Default;
        }
        /// <summary>
        /// Text popisku
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Zarovnání textu v rámci prostoru
        /// </summary>
        public ContentAlignmentType Alignment { get; set; }
        /// <summary>
        /// Debug text
        /// </summary>
        protected override string DebugText { get { return $"{ControlType}; Name: '{Name}'; Text: '{Text}'"; } }
    }
    /// <summary>
    /// DxDataForm : Title.<br/>
    /// Odpovídá XSD typu <c>type_title</c>
    /// </summary>
    public class DataFormatControlTitle : DataFormatControlLabel
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DataFormatControlTitle() : base()
        {
            this.ControlType = ControlType.Title;
        }
        /// <summary>
        /// Styl titulku.
        /// </summary>
        public TitleStyleType Style { get; set; }
    }
    /// <summary>
    /// DxDataForm : CheckBox.<br/>
    /// Odpovídá XSD typu <c>type_checkbox</c>
    /// </summary>
    public class DataFormatControlCheckBox : DataFormatBaseInputTextControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DataFormatControlCheckBox() : base()
        {
            this.ControlType = ControlType.CheckBox;
        }
        /// <summary>
        /// Styl vizualizace CheckBoxu
        /// </summary>
        public CheckBoxStyleType Style { get; set; }
        /// <summary>
        /// Debug text
        /// </summary>
        protected override string DebugText { get { return $"{ControlType}; Name: '{Name}'; Style: '{Style}'"; } }
    }
    /// <summary>
    /// DxDataForm : Button.<br/>
    /// Odpovídá XSD typu <c>type_button</c>
    /// </summary>
    public class DataFormatControlButton : DataFormatBaseInputTextControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DataFormatControlButton() : base()
        {
            this.ControlType = ControlType.Button;
        }
        /// <summary>
        /// Akce na tomto buttonu
        /// </summary>
        public string ActionName { get; set; }
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
    public class DataFormatControlDropDownButton : DataFormatControlButton
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DataFormatControlDropDownButton() : base()
        {
            this.ControlType = ControlType.DropDownButton;
            this.DropDownButtons = null;
        }
        /// <summary>
        /// DropDown prvky na buttonu.
        /// Výchozí hodnota je NULL.
        /// </summary>
        public List<DataFormatSubButton> DropDownButtons { get; set; }
    }
    /// <summary>
    /// DxDataForm : TextBox.<br/>
    /// Odpovídá XSD typu <c>type_textbox</c>
    /// <para/>
    /// Tato třída přináší property <see cref="Alignment"/>, <see cref="EditMask"/>.
    /// </summary>
    public class DataFormatControlTextBox : DataFormatBaseInputControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DataFormatControlTextBox() : base()
        {
            this.ControlType = ControlType.TextBox;
            this.Alignment = ContentAlignmentType.Default;
        }
        /// <summary>
        /// Editační maska
        /// </summary>
        public string EditMask { get; set; }
        /// <summary>
        /// Zarovnání textu v rámci prostoru
        /// </summary>
        public ContentAlignmentType Alignment { get; set; }
    }
    /// <summary>
    /// DxDataForm : TextBoxButton.<br/>
    /// Odpovídá XSD typu <c>type_textboxbutton</c>
    /// </summary>
    public class DataFormatControlTextBoxButton : DataFormatControlTextBox
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DataFormatControlTextBoxButton() : base()
        {
            this.ControlType = ControlType.TextBoxButton;
            this.LeftButtons = null;
            this.RightButtons = null;
        }
        /// <summary>
        /// Buttony na levé straně TextBoxu.
        /// Výchozí hodnota je NULL.
        /// </summary>
        public List<DataFormatSubButton> LeftButtons { get; set; }
        /// <summary>
        /// Buttony na levé straně TextBoxu.
        /// Výchozí hodnota je NULL.
        /// </summary>
        public List<DataFormatSubButton> RightButtons { get; set; }
        /// <summary>
        /// Viditelnost buttonů v textboxu v závislosti na aktivitě TextBoxu
        /// </summary>
        public ButtonsVisibilityType ButtonsVisibility { get; set; }
    }
    /// <summary>
    /// DxDataForm : ComboBox.<br/>
    /// Odpovídá XSD typu <c>type_combobox</c>
    /// <para/>
    /// Tato třída přináší property 
    /// </summary>
    public class DataFormatComboBox : DataFormatBaseInputControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DataFormatComboBox() : base()
        {
            this.ControlType = ControlType.ComboListBox;
            this.Style = ComboBoxStyleType.Default;
        }
        /// <summary>
        /// Položky v nabídce
        /// </summary>
        public List<DataFormatSubButton> ComboItems { get; set; }
        /// <summary>
        /// Název editačního stylu. Může být prázdné, pokud budou zadány prvky comboItem.
        /// </summary>
        public string EditStyleName { get; set; }
        /// <summary>
        /// Styl zobrazení ComboBoxu: S možností psaní, Pouze výběr hodnot, Výběr včetně zobrazení ikony
        /// </summary>
        public ComboBoxStyleType Style { get; set; }
    }
    /// <summary>
    /// DxDataForm : SubButton = součást <see cref="ControlType.DropDownButton"/> i <see cref="ControlType.TextBoxButton"/>.<br/>
    /// Odpovídá XSD typu <c>type_subbutton</c>
    /// </summary>
    public class DataFormatSubButton : DataFormatBaseSubControl
    {
        /// <summary>
        /// Konstruktor, nastaví defaulty
        /// </summary>
        public DataFormatSubButton() : base()
        {
        }
        /// <summary>
        /// Text popisku
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Jméno ikony odstavce nebo prvku (v titulku stránky, v titulku odstavce, ikona Buttonu, atd).
        /// Použití se liší podle typu prvku.
        /// </summary>
        public string IconName { get; set; }
        /// <summary>
        /// Akce na tomto sub-buttonu
        /// </summary>
        public ButtonActionType ActionType { get; set; }
        /// <summary>
        /// Data pro akci na tomto sub-buttonu
        /// </summary>
        public string ActionData { get; set; }
    }
    #endregion
    #region Bázové třídy Controlů
    /// <summary>
    /// Bázová třída pro všechny samostatné controly s neměnným textem a ikonou - Label, Button, CheckBox, ....<br/>
    /// Odpovídá XSD typu <c>type_base_input_text_control</c>
    /// <para/>
    /// Tato třída přináší property <see cref="Text"/>,  <see cref="IconName"/> a <see cref="Alignment"/>.
    /// </summary>
    public class DataFormatBaseInputTextControl : DataFormatBaseInputControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormatBaseInputTextControl() : base()
        {
            this.Alignment = ContentAlignmentType.Default;
        }
        /// <summary>
        /// Text popisku controlu
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
        public ContentAlignmentType Alignment { get; set; }
    }
    /// <summary>
    /// Bázová třída pro všechny samostatné interaktivní controly, které mohou mít vedle políčka Label - TextBox, ComboBox, TokenEdit, ...<br/>
    /// Odpovídá XSD typu <c>type_base_input_labeled_control</c>
    /// <para/>
    /// Tato třída přináší property <see cref="Label"/>, <see cref="LabelPosition"/> a <see cref="LabelWidth"/>.
    /// </summary>
    public class DataFormatBaseInputLabeledControl : DataFormatBaseInputControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormatBaseInputLabeledControl() : base()
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
        public LabelPositionType LabelPosition { get; set; }
        /// <summary>
        /// Nejvyšší šířka prostoru pro Label
        /// </summary>
        public int? LabelWidth { get; set; }
    }
    /// <summary>
    /// Bázová třída pro všechny samostatné interaktivní controly - TextBox, Button, CheckBox, ComboBox, ....<br/>
    /// Odpovídá XSD typu <c>type_base_input_control</c>
    /// <para/>
    /// Tato třída přináší property <see cref="Required"/>.
    /// </summary>
    public class DataFormatBaseInputControl : DataFormatBaseControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormatBaseInputControl() : base()
        {
            this.Required = RequiredType.Default;
        }
        /// <summary>
        /// Povinnost vyplnění prvku
        /// </summary>
        public RequiredType Required { get; set; }
    }
    /// <summary>
    /// Bázová třída pro všechny samostatné controly bez ohledu na jejich vlastní interaktivitu - tedy Label, Picture, Panel; a dále i pro interaktivní controly: TextBox, Button, CheckBox, ComboBox, ...
    /// Slouží i jako podklad pro Containery.<br/>
    /// Odpovídá XSD typu <c>type_base_control</c>
    /// <para/>
    /// Tato třída přináší souřadnice <see cref="Bounds"/>.
    /// </summary>
    public class DataFormatBaseControl : DataFormatBaseSubControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormatBaseControl() : base()
        {
            this.ControlType = ControlType.None;
        }
        /// <summary>
        /// Druh vstupního prvku (Control).
        /// </summary>
        public ControlType ControlType { get; set; }
        /// <summary>
        /// Umístění prvku. Výchozí je null.
        /// </summary>
        public Bounds Bounds { get; set; }
        /// <summary>
        /// Debug text
        /// </summary>
        protected override string DebugText { get { return $"{ControlType}; Name: '{Name}'"; } }
    }
    /// <summary>
    /// Bázová třída pro všechny controly - včetně subcontrolů (pro položky v ComboBoxu i SubButtony).<br/>
    /// Odpovídá XSD typu <c>type_base_sub_control</c>
    /// <para/>
    /// Tato třída přináší property: <see cref="State"/>, výraz <see cref="Invisible"/> a texty pro ToolTip <see cref="ToolTipTitle"/> a <see cref="ToolTipText"/>.
    /// </summary>
    public class DataFormatBaseSubControl : DataFormatBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormatBaseSubControl() : base()
        {
            this.State = ControlStateType.Default;
        }
        /// <summary>
        /// Stav bloku nebo prvku (viditelnost, editovatelnost)
        /// </summary>
        public ControlStateType State { get; set; }
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
    }
    /// <summary>
    /// Bázová třída pro všechny prvky - controly i containery.<br/>
    /// Odpovídá XSD typu <c>type_base</c>
    /// <para/>
    /// Tato třída přináší základní property: <see cref="Name"/>.
    /// </summary>
    public class DataFormatBase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormatBase()
        {
        }
        /// <summary>
        /// Klíčové jméno prvku pro jeho jednoznačnou identifikaci.
        /// Odstavce a prvky musí mít <see cref="Name"/> jednoznačné přes celý formulář = přes všechny záložky.
        /// Subprvky (=položky editačního stylu) a subbuttony (tlačítka v <see cref="ControlType.TextBoxButton"/>) mají <see cref="Name"/> jednoznačné jen v rámci svého prvku.
        /// </summary>
        public string Name { get; set; }
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
        /// <param name="width"></param>
        /// <param name="height"></param>
        public Bounds(int? left, int? top, int? width, int?height)
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
        public int? Left { get; set; }
        /// <summary>
        /// Top
        /// </summary>
        public int? Top { get; set; }
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
    /// Verze formátu definice frm.xml
    /// Odpovídá XSD typu <c>format_version_enum</c>
    /// </summary>
    public enum FormatVersionType
    {
        /// <summary>
        /// Default = Version1
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
        Up,
        /// <summary>
        /// Dole pod textem, písmo zarovnané doleva
        /// </summary>
        Bottom
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
        IconList
    }
    /// <summary>
    /// Styl viditelnosti buttonů v rámci prvku <see cref="DxRepositoryEditorTextBoxButton"/>.
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
    /// Akce, kterou button provede.
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
        /// Běžný vnitřní TAB (odstavec), může / nemusí mít titulek (podle přítomnosti textu <see cref="DataFormatBaseInputTextControl.Text"/>)
        /// </summary>
        Default,
        /// <summary>
        /// Vrcholový container, reprezentuje celý formulář
        /// </summary>
        Form,
        /// <summary>
        /// Sada stránek; její vnitřní prvky musí být stylu <see cref="ContainerStyleType.Page"/>
        /// </summary>
        PageSet,
        /// <summary>
        /// Záhlaví stránky. Ignoruje souřadnice 
        /// </summary>
        Page,
        /// <summary>
        /// Běžný panel, jako <see cref="Default"/>
        /// </summary>
        Panel
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
        ComboListBox,
        /// <summary>
        /// ComboBox s obrázky
        /// </summary>
        ImageComboListBox,
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
