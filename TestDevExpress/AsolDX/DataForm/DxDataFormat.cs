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

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    /*   Hierarchie uvnitř DataFormu
      - 'DxDataFormat' je třída reprezentující komplexní formát obsahu DataFormu
         - 'DxDataFormat' může být jednoduchý, pak v property 'Panel' obsahuje jednu instanci 'DxDataFormatFlowPanel', nezobrazuje záložky ale přímo obsah
         - 'DxDataFormat' může obsahovat i sadu záložek, umístěnou v property 'Pages', ta v sobě obsahuje sadu stránek 'DxDataFormatPages'
         - Nikdy nesmí obsahovat obě najednou
      - Jedna stránka je potomkem 'DxDataFormatFlowPanel', proto má podobné chování; obsahuje navíc text a ikony pro záložku
      - Panel 'DxDataFormatFlowPanel' může tedy být zobrazen jako single, anebo jako obsah jedné záložky
      - Panel 'DxDataFormatFlowPanel' v sobě může obsahovat jednotlivé prvky, které může umísťovat pod sebe nebo vedle sebe v závislosti na disponibilním prostoru a vlastnostech
      - Jednotlivé prvky v 'DxDataFormatFlowPanel' jsou buď vnořené další 'DxDataFormatFlowPanel', anebo koncové Taby 'DxDataFormatTab'
      - Prvky v 'DxDataFormatFlowPanel' si samy určí svoji velikost podle svého obsahu a dalších vlastností
         a tím je následně určena i pozice jednotlivých prvků (tok obsahu dolů / dolů a pak doprava / zleva doprava a pak dolů / zalomení podle nastavení) => layout celé stránky
      - Prvky 'DxDataFormatTab' obsahují buď jednotlivé controly 'DxDataFormatControl' anebo grupy controlů 'DxDataFormatGroup', 
         oba typy prvků ale v rámci Tabu musí mít exaktně danou pozici (umístění i velikost), jejich velikost ani umístění se neurčuje nějakým výpočtem 
          = to je princip FormatVersion4 !!
      - Prvky 'DxDataFormatTab' tedy dokážou určit svoji velikost (Width a Height) = na základě velikosti svého obsahu a Padding a přítomnosti titulkového řádku a patkové linky;
          - Tyto prvky mohou / nemusí mít určenou explicitní velikost
          - Tyto prvky mohou mít určené vlastnosti pro řízení layoutu
      - Prvky 'DxDataFormatFlowPanel' si poskládají svoje Child prvky do layoutu podle jejich rozměru, zásadně v řazení pod sebe (X = 0, Y = průběžně dolů);





    */
    /// <summary>
    /// Definice formátu jednoho DataFormu = buď sada záložek, anebo jeden panel
    /// </summary>
    internal class DxDataFormat
    {

    }
    /// <summary>
    /// Definice formátu skupiny záložek v DataFormu = obsahuje sadu záložek
    /// </summary>
    internal class DxDataFormatPages
    {

    }

    /// <summary>
    /// Definice formátu jedné záložky v DataFormu = potomek FlowPanel, obsahuje Taby
    /// </summary>
    internal class DxDataFormatPage : DxDataFormatFlowPanel
    {

    }
    /// <summary>
    /// Definice formátu jednoho panelu v DataFormu = je umístěn jako jediný panel DataFormu, anebo jako jedna strána ve skupně záložek. Obsahuje buď Taby, nebo další FlowPanely.
    /// </summary>
    internal class DxDataFormatFlowPanel
    {

    }

    /// <summary>
    /// Definice formátu jednoho odstavce v DataFormu = obsahuje titulek optional velikost, a sadu prvků - buď columns, nebo vnořené grupy
    /// </summary>
    internal class DxDataFormatTab
    {

    }

    /// <summary>
    /// Definice formátu jedné grupy v DataFormu = obsahuje titulek optional velikost, a sadu prvků - buď columns, nebo vnořené grupy
    /// </summary>
    internal class DxDataFormatGroup
    {

    }
    /// <summary>
    /// Definice formátu jednoho controlu v DataFormu = label, editbox, button, atd
    /// </summary>
    internal class DxDataFormatControl
    {

    }
}
