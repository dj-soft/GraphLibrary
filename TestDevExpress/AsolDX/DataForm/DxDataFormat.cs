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
    /// Definice formátu jednoho odstavce v DataFormu = obsahuje titulek optional velikost, a sadu prvků - buď columns, nebo vnořené grupy
    /// </summary>
    internal class DataFormatTab
    {
        public List<DataFormatFixedItem> Items { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        /// <summary>
        /// Okraje za posledními prvky (vpravo, dole). Použije se tehdy, když není daná 
        /// </summary>
        public WinForm.Padding? Padding { get; set; }
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
}
