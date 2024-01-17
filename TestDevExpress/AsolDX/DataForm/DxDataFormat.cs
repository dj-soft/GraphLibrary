// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

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
    /*    DataFormat
      - Reprezentuje definici vzhledu a chování DataFormu
      - Odpovídá aktuální verzi formátu V4


      Form => { TabContainer | }
      - Tab:
         .Style = { TabContainer, TabHeader,  }











          Hierarchie uvnitř DataFormu
      - 'DataFormat' je třída reprezentující komplexní formát obsahu DataFormu
         - 'DataFormat' může být jednoduchý, pak v property 'Panel' obsahuje jednu instanci 'DataFormatFlowPanel', nezobrazuje záložky ale přímo obsah
         - 'DataFormat' může obsahovat i sadu záložek, umístěnou v property 'Pages', ta v sobě obsahuje sadu stránek 'DataFormatPages'
         -    Nikdy nesmí obsahovat obě najednou!
      - Jedna stránka 'DataFormatPage' je potomkem 'DataFormatFlowPanel', proto má podobné chování; obsahuje navíc text a ikony pro záložku
      - Panel 'DataFormatFlowPanel' může tedy být zobrazen jako single, anebo jako obsah jedné záložky
      - Panel 'DataFormatFlowPanel' v sobě může obsahovat jednotlivé containery, které může umísťovat pod sebe nebo vedle sebe v závislosti na disponibilním prostoru a vlastnostech
      - Jednotlivé prvky v 'DataFormatFlowPanel' jsou buď vnořené další 'DataFormatFlowPanel', anebo koncové Taby 'DataFormatTab'
      - Prvky v 'DataFormatFlowPanel' si samy určí svoji velikost podle svého obsahu a dalších vlastností
         a tím je následně určena i pozice jednotlivých prvků (tok obsahu dolů / dolů a pak doprava / zleva doprava a pak dolů / zalomení podle nastavení) => layout celé stránky
      - Prvky 'DataFormatTab' obsahují buď jednotlivé controly 'DataFormatControl' anebo grupy controlů 'DataFormatGroup', 
         oba typy prvků ale v rámci Tabu musí mít exaktně danou pozici (umístění i velikost), jejich velikost ani umístění se neurčuje nějakým výpočtem 
          = to je princip FormatVersion4 !!
      - Prvky 'DataFormatTab' tedy dokážou určit svoji velikost (Width a Height) = na základě velikosti svého obsahu a Padding a přítomnosti titulkového řádku a patkové linky;
          - Tyto prvky mohou / nemusí mít určenou explicitní velikost
          - Tyto prvky mohou mít určené vlastnosti pro řízení layoutu
      - Prvky 'DataFormatFlowPanel' si poskládají svoje Child prvky do layoutu podle jejich rozměru, zásadně v řazení pod sebe (X = 0, Y = průběžně dolů);


          Funkcionalita tříd
      - Pouze obsahují data
      - Třídy nemají uvnitř žádnou funkcionalitu
      - Existuje třída DataFormatManager, která zajišťuje veškerou funkcionalitu okolo Formátu



    */




    #region Shared : třídy pouze nesou data, nemají funkcinalitu

    /// <summary>
    /// Definice formátu jednoho bloku v DataFormu.
    /// Blok může představovat celou sadu stránek, nebo jednu stránku, nebo viditelný odstavec stránky, nebo vnitřní blok 
    /// </summary>
    public class DataFormatTab : DataFormatItem
    {
        /// <summary>
        /// Styl odstavce
        /// </summary>
        public TabStyle Style { get; set; }
        /// <summary>
        /// Pořadí prvku v poli ostatních prvků v Parentu
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// Název ikony v titulkovém řádku.
        /// Ignoruje se pro <see cref="Style"/> == <see cref="TabStyle.PageSet"/>.
        /// </summary>
        public string ImageName { get; set; }
        /// <summary>
        /// Souřadnice počátku, Left. Typicky je null. Zadáním lze exaktně umístit panel. Některé styly bloku (<see cref="TabStyle.Page"/>) tuto hodnotu ignorují.
        /// </summary>
        public int? Left { get; set; }
        /// <summary>
        /// Souřadnice počátku, Top. Typicky je null. Zadáním lze exaktně umístit panel. Některé styly bloku (<see cref="TabStyle.Page"/>) tuto hodnotu ignorují.
        /// </summary>
        public int? Top { get; set; }
        /// <summary>
        /// Celková šířka panelu. Pokud je null, určí se podle souřadnic vnitřních prvků. Některé styly bloku (<see cref="TabStyle.Page"/>) tuto hodnotu ignorují.
        /// </summary>
        public int? Width { get; set; }
        /// <summary>
        /// Celková výška panelu. Pokud je null, určí se podle souřadnic vnitřních prvků. Některé styly bloku (<see cref="TabStyle.Page"/>) tuto hodnotu ignorují.
        /// </summary>
        public int? Height { get; set; }
        /// <summary>
        /// Okraje okolo vnitřních prvků. Jde o prostor mezi hranou this containeru a vnitřními prvky v něm obsaženými.
        /// Tento okraj je prázdný, a uvnitř něj jsou pak umístěny vnitřní prvky.<br/>
        /// Left a Top určují pozici, kde začínají controly na souřadnici 0/0.
        /// Right a Bottom určují okraj za posledními prvky při výpočtu Width a Height, pokud zde nejsou určeny.
        /// </summary>
        public Margins InnerMargins { get; set; }

        /// <summary>
        /// Sada prvků
        /// </summary>
        public List<DataFormatItem> Items { get; set; }
    }
    /// <summary>
    /// Jeden konkrétní control (column) = vstupní prvek, nikoli container
    /// </summary>
    public class DataFormatControl : DataFormatItem
    {
        /// <summary>
        /// Souřadnice počátku, Left.
        /// </summary>
        public int Left { get; set; }
        /// <summary>
        /// Souřadnice počátku, Top.
        /// </summary>
        public int Top { get; set; }
        /// <summary>
        /// Celková šířka prvku.
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// Celková výška prvku. 
        /// Pokud je null, vytvoří ji GUI podle konkrétního prvku (typicky TextBox, CheckBox, Label a podobně: mají výšku pro zobrazení jednoho řádku).
        /// </summary>
        public int? Height { get; set; }
        /// <summary>
        /// Druh vstupního prvku
        /// </summary>
        public ControlType ControlType { get; set; }

        public string EditMask { get; set; }
        public Data.ItemSet Items { get; set; }
        public Data.ItemSet Buttons { get; set; }

    }
    /// <summary>
    /// Bázová třída pro <see cref="DataFormatTab"/> a <see cref="DataFormatControl"/>.
    /// </summary>
    public class DataFormatItem
    {
        /// <summary>
        /// Klíčové jméno prvku pro identifikaci
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Stav bloku nebo prvku (viditelnost, editovatelnost)
        /// </summary>
        public ItemState State { get; set; }
        /// <summary>
        /// Text titulku odstavce (titulek stránky, titulek odstavce, text Labelu, text CheckBoxu, Buttonu, atd).
        /// Použití se liší podle typu prvku.
        /// </summary>
        public string Text { get; set; }

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
    /// Styl jednoho bloku = odstavce
    /// </summary>
    public enum TabStyle
    {
        /// <summary>
        /// Běžný vnitřní TAB (odstavec), může / nemusí mít titulek (podle jeho titulku)
        /// </summary>
        Default,
        /// <summary>
        /// Záhlaví stránky. Ignoruje souřadnice 
        /// </summary>
        Page,
        /// <summary>
        /// Sada stránek; její vnitřní prvky musí být stylu <see cref="TabStyle.Page"/>
        /// </summary>
        PageSet
    }
    /// <summary>
    /// Stav prvku. 
    /// Jde o Flags. Lze je sčítat z celé hierarchie containerů (OR), výsledek popisuje stav nejvyššího prvku.
    /// Tedy pokud jeden jediný prvek v hierarchii je <see cref="Invisible"/>, pak finální prvek je neviditelný.
    /// Obdobně <see cref="Disabled"/> nebo <see cref="ReadOnly"/>.
    /// Pokud součet všech hodnot je <see cref="Default"/>, pak prvek je viditelný a editovatelný.
    /// </summary>
    [Flags]
    public enum ItemState
    {
        /// <summary>
        /// Prvek je viditelný a editovatelný
        /// </summary>
        Default = 0,
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
        /// CheckBox: zaškrtávátko i DownButton
        /// </summary>
        CheckEdit,
        /// <summary>
        /// Přepínací switch (moderní checkbox s animovaným přechodem On-Off)
        /// </summary>
        ToggleSwitch,
        /// <summary>
        /// Jeden prvek ze sady RadioButtonů
        /// </summary>
        RadioButton,
        /// <summary>
        /// Klasické tlačítko
        /// </summary>
        Button,
        /// <summary>
        /// Button s přidaným rozbalovacím menu
        /// </summary>
        DropDownButton,
        /// <summary>
        /// ComboBox bez obrázků
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

    /*
    /// <summary>
    /// Definice formátu jednoho DataFormu = buď sada záložek, anebo jeden panel
    /// </summary>
    internal class DataFormat
    {
        /// <summary>
        /// Hlavní záložky v DataFormu = souhrn jednotlivých stránek <see cref="DataFormatPage"/>.
        /// Pokud je <see cref="Pages"/> zadáno, pak musí být <see cref="Panel"/> null (a naopak).
        /// </summary>
        public DataFormatPages Pages { get; set; }
        /// <summary>
        /// Základní jednoduchá stránka DataFormu. Takový dataform pak zobrazuje přímo stránku, nikoli záložly.
        /// Pokud je <see cref="Panel"/> zadáno, pak musí být <see cref="Pages"/> null (a naopak).
        /// </summary>
        public DataFormatFlowPanel Panel { get; set; }
        /// <summary>
        /// Okraj mezi vnějším okrajem zobrazovacího panelu a vnitřními prvky (panely).
        /// </summary>
        public WinForm.Padding Padding { get; set; }
    }
    /// <summary>
    /// Definice formátu skupiny záložek v DataFormu = obsahuje sadu záložek
    /// </summary>
    internal class DataFormatPages
    {
        /// <summary>
        /// Soupis jednotlivých stránek = záložek
        /// </summary>
        public List<DataFormatPage> Pages { get; set; }
    }

    /// <summary>
    /// Definice formátu jedné záložky v DataFormu = potomek FlowPanel, obsahuje Taby
    /// </summary>
    internal class DataFormatPage : DataFormatFlowPanel
    {
        public string HeaderText { get; set; }
    }
    /// <summary>
    /// Definice formátu jednoho panelu v DataFormu = je umístěn jako jediný panel DataFormu, anebo jako jedna strána ve skupně záložek. Obsahuje buď Taby, nebo další FlowPanely.
    /// </summary>
    internal class DataFormatFlowPanel
    {

    }

   

    /// <summary>
    /// Definice formátu jedné grupy v DataFormu = obsahuje titulek optional velikost, a sadu prvků - buď columns, nebo vnořené grupy
    /// </summary>
    internal class DataFormatGroup : DataFormatFixedItem
    {
        public List<DataFormatControl> Controls { get; set; }
    }
    /// <summary>
    /// Definice formátu jednoho controlu v DataFormu = label, editbox, button, atd
    /// </summary>
    internal class DataFormatControl : DataFormatFixedItem
    {

        public DataFormatControl() { }

         


    }
    internal class DataFormatFixedItem
    {
        public WinDraw.Rectangle Bounds { get; set; }
    }
    */
}
