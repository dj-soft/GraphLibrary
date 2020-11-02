// Supervisor: DAJ
// Part of GraphLib, proprietary software, (c) Asseco solutions, a. s. + DJ-Soft
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco solutions, a. s. 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Tento soubor obsahuje třídy a konstanty, které popisují názvy resources na straně pluginu. Resources jsou Images a další zdroje ve formě bytového pole.
// UPOZORNĚNÍ: tento soubor nemá být editován uživatelem, protože jeho obsah si udržuje knihovna GraphLib sama.
//   Plugin si načte resources, což je obsah ZIP souboru umístěného v tomtéž adresáři, kde je DLL soubor pluginu. Soubor má název "ASOL.GraphLib.ics".
//   A poté, když plugin běží v rámci VisualStudia (má připojen debugger), a přitom existuje ve vhodném umístění soubor "WorkSchedulerResources.cs",
//      pak plugin prověří, že soubor "IconResources.cs" obsahuje uložené datum "LastWriteTime" souboru "ASOL.GraphLib.ics".
//   Pokud fyzické zdroje ("ASOL.GraphLib.ics") jsou novější, pak znovu vygeneruje kompletní obsah souboru "IconResources.cs".
// Generátor tohoto souboru je v aplikaci GraphLib, v kódu "GraphLib\Application\Resources.cs".
// Místo, kde je uloženo datum "LastWriteTime" souboru "ASOL.GraphLib.ics" je na následujícím řádku:
//     ResourceFile.LastWriteTime = 2020-11-02 19:37
#pragma warning disable 1591
namespace Asol.Tools.WorkScheduler.Resources.Images
{
    #region AsolGraphlibIcs_Pic16
    /// <summary>
    /// Obsah adresáře Asol.graphlib.ics\Pic16
    /// <para/>
    /// Programátor, který chce vidět jednotlivé ikonky, si najde soubor "ASOL.GraphLib.res" v adresáři pluginu WorkScheduler,
    /// zkopíruje si jej do pracovního adresáře, přejmenuje příponu na .zip a rozzipuje.
    /// <para/>
    /// Programátor, který chce doplnit další resource, si do výše uvedeného rozbaleného adresáře přidá nové ikony nebno adresář s ikonami nebo jiná data,
    /// poté celý adresář zazipuje, přejmenuje celý zip na "ASOL.GraphLib.res" a vloží soubor do balíčku WorkScheduleru.
    /// <para/>
    /// Poté programátor spustí WorkScheduler z Visual studia v režimu Debug, a plugin při startu nově vygeneruje soubor WorkSchedulerResources.cs, obsahující nově dodané položky jako konstanty.
    /// </summary>
    public class AsolGraphlibIcs_Pic16
    {
        public const string AccessoriesCalculator2Png = "asol.graphlib.ics/pic16/accessories-calculator-2.png";
        public const string AccessoriesCalculator3Png = "asol.graphlib.ics/pic16/accessories-calculator-3.png";
        public const string AccessoriesCalculator6Png = "asol.graphlib.ics/pic16/accessories-calculator-6.png";
        public const string AccessoriesClockPng = "asol.graphlib.ics/pic16/accessories-clock.png";
        public const string AccessoriesDatePng = "asol.graphlib.ics/pic16/accessories-date.png";
        public const string ArrowDown2Png = "asol.graphlib.ics/pic16/arrow-down-2.png";
        public const string ArrowDownBluePng = "asol.graphlib.ics/pic16/arrow-down-blue.png";
        public const string ArrowDownGrayPng = "asol.graphlib.ics/pic16/arrow-down-gray.png";
        public const string ArrowDownGreenPng = "asol.graphlib.ics/pic16/arrow-down-green.png";
        public const string ArrowDownRedPng = "asol.graphlib.ics/pic16/arrow-down-red.png";
        public const string ArrowDownVioletPng = "asol.graphlib.ics/pic16/arrow-down-violet.png";
        public const string ArrowDownYellowPng = "asol.graphlib.ics/pic16/arrow-down-yellow.png";
        public const string ArrowRight2Png = "asol.graphlib.ics/pic16/arrow-right-2.png";
        public const string ArrowRightBluePng = "asol.graphlib.ics/pic16/arrow-right-blue.png";
        public const string ArrowRightGrayPng = "asol.graphlib.ics/pic16/arrow-right-gray.png";
        public const string ArrowRightGreenPng = "asol.graphlib.ics/pic16/arrow-right-green.png";
        public const string ArrowRightRedPng = "asol.graphlib.ics/pic16/arrow-right-red.png";
        public const string ArrowRightVioletPng = "asol.graphlib.ics/pic16/arrow-right-violet.png";
        public const string ArrowRightYellowPng = "asol.graphlib.ics/pic16/arrow-right-yellow.png";
        public const string BulletBluePng = "asol.graphlib.ics/pic16/bullet-blue.png";
        public const string BulletGoPng = "asol.graphlib.ics/pic16/bullet-go.png";
        public const string BulletGreenPng = "asol.graphlib.ics/pic16/bullet-green.png";
        public const string BulletOrangePng = "asol.graphlib.ics/pic16/bullet-orange.png";
        public const string BulletPinkPng = "asol.graphlib.ics/pic16/bullet-pink.png";
        public const string BulletPurplePng = "asol.graphlib.ics/pic16/bullet-purple.png";
        public const string BulletRedPng = "asol.graphlib.ics/pic16/bullet-red.png";
        public const string BulletStarPng = "asol.graphlib.ics/pic16/bullet-star.png";
        public const string BulletWhitePng = "asol.graphlib.ics/pic16/bullet-white.png";
        public const string BulletYellowPng = "asol.graphlib.ics/pic16/bullet-yellow.png";
        public const string ComboDownPng = "asol.graphlib.ics/pic16/combo_down.png";
        public const string DialogCancel4Png = "asol.graphlib.ics/pic16/dialog-cancel-4.png";
        public const string DialogDisablePng = "asol.graphlib.ics/pic16/dialog-disable.png";
        public const string DialogError5Png = "asol.graphlib.ics/pic16/dialog-error-5.png";
        public const string DialogInformation3Png = "asol.graphlib.ics/pic16/dialog-information-3.png";
        public const string DialogInformation4Png = "asol.graphlib.ics/pic16/dialog-information-4.png";
        public const string DialogNo3Png = "asol.graphlib.ics/pic16/dialog-no-3.png";
        public const string DialogOk5Png = "asol.graphlib.ics/pic16/dialog-ok-5.png";
        public const string DialogOkApply5Png = "asol.graphlib.ics/pic16/dialog-ok-apply-5.png";
        public const string FolderOrangeOpenPng = "asol.graphlib.ics/pic16/folder-orange_open.png";
        public const string FolderOrangePng = "asol.graphlib.ics/pic16/folder-orange.png";
        public const string FolderYellowOpenPng = "asol.graphlib.ics/pic16/folder-yellow_open.png";
        public const string FolderYellowPng = "asol.graphlib.ics/pic16/folder-yellow.png";
        public const string GnomeWebBrowserPng = "asol.graphlib.ics/pic16/gnome-web-browser.png";
        public const string InternetMail5Png = "asol.graphlib.ics/pic16/internet-mail-5.png";
        public const string InternetPng = "asol.graphlib.ics/pic16/internet.png";
        public const string MagnifierPng = "asol.graphlib.ics/pic16/magnifier.png";
        public const string MagnifierZoomInPng = "asol.graphlib.ics/pic16/magnifier-zoom-in.png";
    }
    #endregion
    #region AsolGraphlibIcs_Pic24
    /// <summary>
    /// Obsah adresáře Asol.graphlib.ics\Pic24
    /// <para/>
    /// Programátor, který chce vidět jednotlivé ikonky, si najde soubor "ASOL.GraphLib.res" v adresáři pluginu WorkScheduler,
    /// zkopíruje si jej do pracovního adresáře, přejmenuje příponu na .zip a rozzipuje.
    /// <para/>
    /// Programátor, který chce doplnit další resource, si do výše uvedeného rozbaleného adresáře přidá nové ikony nebno adresář s ikonami nebo jiná data,
    /// poté celý adresář zazipuje, přejmenuje celý zip na "ASOL.GraphLib.res" a vloží soubor do balíčku WorkScheduleru.
    /// <para/>
    /// Poté programátor spustí WorkScheduler z Visual studia v režimu Debug, a plugin při startu nově vygeneruje soubor WorkSchedulerResources.cs, obsahující nově dodané položky jako konstanty.
    /// </summary>
    public class AsolGraphlibIcs_Pic24
    {
        public const string AccessoriesCalculator2Png = "asol.graphlib.ics/pic24/accessories-calculator-2.png";
        public const string AccessoriesCalculator3Png = "asol.graphlib.ics/pic24/accessories-calculator-3.png";
        public const string AccessoriesCalculator5Png = "asol.graphlib.ics/pic24/accessories-calculator-5.png";
        public const string AccessoriesClockPng = "asol.graphlib.ics/pic24/accessories-clock.png";
        public const string AccessoriesDatePng = "asol.graphlib.ics/pic24/accessories-date.png";
        public const string ArrowDown2Png = "asol.graphlib.ics/pic24/arrow-down-2.png";
        public const string ArrowDownBluePng = "asol.graphlib.ics/pic24/arrow-down-blue.png";
        public const string ArrowDownGrayPng = "asol.graphlib.ics/pic24/arrow-down-gray.png";
        public const string ArrowDownGreenPng = "asol.graphlib.ics/pic24/arrow-down-green.png";
        public const string ArrowDownRedPng = "asol.graphlib.ics/pic24/arrow-down-red.png";
        public const string ArrowDownVioletPng = "asol.graphlib.ics/pic24/arrow-down-violet.png";
        public const string ArrowDownYellowPng = "asol.graphlib.ics/pic24/arrow-down-yellow.png";
        public const string ArrowRight2Png = "asol.graphlib.ics/pic24/arrow-right-2.png";
        public const string ArrowRightBluePng = "asol.graphlib.ics/pic24/arrow-right-blue.png";
        public const string ArrowRightGrayPng = "asol.graphlib.ics/pic24/arrow-right-gray.png";
        public const string ArrowRightGreenPng = "asol.graphlib.ics/pic24/arrow-right-green.png";
        public const string ArrowRightRedPng = "asol.graphlib.ics/pic24/arrow-right-red.png";
        public const string ArrowRightVioletPng = "asol.graphlib.ics/pic24/arrow-right-violet.png";
        public const string ArrowRightYellowPng = "asol.graphlib.ics/pic24/arrow-right-yellow.png";
        public const string ComboDownPng = "asol.graphlib.ics/pic24/combo_down.png";
        public const string DialogCancel4Png = "asol.graphlib.ics/pic24/dialog-cancel-4.png";
        public const string DialogDisablePng = "asol.graphlib.ics/pic24/dialog-disable.png";
        public const string DialogError5Png = "asol.graphlib.ics/pic24/dialog-error-5.png";
        public const string DialogInformation3Png = "asol.graphlib.ics/pic24/dialog-information-3.png";
        public const string DialogInformation4Png = "asol.graphlib.ics/pic24/dialog-information-4.png";
        public const string DialogNo3Png = "asol.graphlib.ics/pic24/dialog-no-3.png";
        public const string DialogOk5Png = "asol.graphlib.ics/pic24/dialog-ok-5.png";
        public const string DialogOkApply5Png = "asol.graphlib.ics/pic24/dialog-ok-apply-5.png";
        public const string FolderOrangeOpenPng = "asol.graphlib.ics/pic24/folder-orange_open.png";
        public const string FolderOrangePng = "asol.graphlib.ics/pic24/folder-orange.png";
        public const string FolderYellowOpenPng = "asol.graphlib.ics/pic24/folder-yellow_open.png";
        public const string FolderYellowPng = "asol.graphlib.ics/pic24/folder-yellow.png";
        public const string GnomeWebBrowserPng = "asol.graphlib.ics/pic24/gnome-web-browser.png";
        public const string InternetMail5Png = "asol.graphlib.ics/pic24/internet-mail-5.png";
        public const string InternetPng = "asol.graphlib.ics/pic24/internet.png";
        public const string Zoom3Png = "asol.graphlib.ics/pic24/zoom-3.png";
        public const string ZoomIn4Png = "asol.graphlib.ics/pic24/zoom-in-4.png";
        public const string ZoomOut4Png = "asol.graphlib.ics/pic24/zoom-out-4.png";
    }
    #endregion
    #region AsolGraphlibIcs_Pic32
    /// <summary>
    /// Obsah adresáře Asol.graphlib.ics\Pic32
    /// <para/>
    /// Programátor, který chce vidět jednotlivé ikonky, si najde soubor "ASOL.GraphLib.res" v adresáři pluginu WorkScheduler,
    /// zkopíruje si jej do pracovního adresáře, přejmenuje příponu na .zip a rozzipuje.
    /// <para/>
    /// Programátor, který chce doplnit další resource, si do výše uvedeného rozbaleného adresáře přidá nové ikony nebno adresář s ikonami nebo jiná data,
    /// poté celý adresář zazipuje, přejmenuje celý zip na "ASOL.GraphLib.res" a vloží soubor do balíčku WorkScheduleru.
    /// <para/>
    /// Poté programátor spustí WorkScheduler z Visual studia v režimu Debug, a plugin při startu nově vygeneruje soubor WorkSchedulerResources.cs, obsahující nově dodané položky jako konstanty.
    /// </summary>
    public class AsolGraphlibIcs_Pic32
    {
        public const string AccessoriesCalculator2Png = "asol.graphlib.ics/pic32/accessories-calculator-2.png";
        public const string AccessoriesCalculator3Png = "asol.graphlib.ics/pic32/accessories-calculator-3.png";
        public const string AccessoriesCalculator5Png = "asol.graphlib.ics/pic32/accessories-calculator-5.png";
        public const string AccessoriesClockPng = "asol.graphlib.ics/pic32/accessories-clock.png";
        public const string AccessoriesDatePng = "asol.graphlib.ics/pic32/accessories-date.png";
        public const string ArrowDown2Png = "asol.graphlib.ics/pic32/arrow-down-2.png";
        public const string ArrowDownBluePng = "asol.graphlib.ics/pic32/arrow-down-blue.png";
        public const string ArrowDownGrayPng = "asol.graphlib.ics/pic32/arrow-down-gray.png";
        public const string ArrowDownGreenPng = "asol.graphlib.ics/pic32/arrow-down-green.png";
        public const string ArrowDownRedPng = "asol.graphlib.ics/pic32/arrow-down-red.png";
        public const string ArrowDownVioletPng = "asol.graphlib.ics/pic32/arrow-down-violet.png";
        public const string ArrowDownYellowPng = "asol.graphlib.ics/pic32/arrow-down-yellow.png";
        public const string ArrowRight2Png = "asol.graphlib.ics/pic32/arrow-right-2.png";
        public const string ArrowRightBluePng = "asol.graphlib.ics/pic32/arrow-right-blue.png";
        public const string ArrowRightGrayPng = "asol.graphlib.ics/pic32/arrow-right-gray.png";
        public const string ArrowRightGreenPng = "asol.graphlib.ics/pic32/arrow-right-green.png";
        public const string ArrowRightRedPng = "asol.graphlib.ics/pic32/arrow-right-red.png";
        public const string ArrowRightVioletPng = "asol.graphlib.ics/pic32/arrow-right-violet.png";
        public const string ArrowRightYellowPng = "asol.graphlib.ics/pic32/arrow-right-yellow.png";
        public const string ComboDownPng = "asol.graphlib.ics/pic32/combo_down.png";
        public const string DialogCancel4Png = "asol.graphlib.ics/pic32/dialog-cancel-4.png";
        public const string DialogDisablePng = "asol.graphlib.ics/pic32/dialog-disable.png";
        public const string DialogError5Png = "asol.graphlib.ics/pic32/dialog-error-5.png";
        public const string DialogInformation3Png = "asol.graphlib.ics/pic32/dialog-information-3.png";
        public const string DialogInformation4Png = "asol.graphlib.ics/pic32/dialog-information-4.png";
        public const string DialogNo3Png = "asol.graphlib.ics/pic32/dialog-no-3.png";
        public const string DialogOk5Png = "asol.graphlib.ics/pic32/dialog-ok-5.png";
        public const string DialogOkApply5Png = "asol.graphlib.ics/pic32/dialog-ok-apply-5.png";
        public const string FolderOrangeOpenPng = "asol.graphlib.ics/pic32/folder-orange_open.png";
        public const string FolderOrangePng = "asol.graphlib.ics/pic32/folder-orange.png";
        public const string FolderYellowOpenPng = "asol.graphlib.ics/pic32/folder-yellow_open.png";
        public const string FolderYellowPng = "asol.graphlib.ics/pic32/folder-yellow.png";
        public const string GnomeWebBrowserPng = "asol.graphlib.ics/pic32/gnome-web-browser.png";
        public const string InternetMail5Png = "asol.graphlib.ics/pic32/internet-mail-5.png";
        public const string InternetPng = "asol.graphlib.ics/pic32/internet.png";
        public const string Zoom3Png = "asol.graphlib.ics/pic32/zoom-3.png";
        public const string ZoomIn4Png = "asol.graphlib.ics/pic32/zoom-in-4.png";
        public const string ZoomOut4Png = "asol.graphlib.ics/pic32/zoom-out-4.png";
    }
    #endregion
}
#pragma warning restore 1591
