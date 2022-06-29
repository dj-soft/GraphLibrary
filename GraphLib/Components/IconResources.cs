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
//     ResourceFile.LastWriteTime = 2022-06-29 07:44
#pragma warning disable 1591
namespace Asol.Tools.WorkScheduler.Resources.Images
{
    #region Pic16
    /// <summary>
    /// Obsah adresáře Pic16
    /// <para/>
    /// Programátor, který chce vidět jednotlivé ikonky, si najde soubor "ASOL.GraphLib.ics" v adresáři GraphLib,
    /// zkopíruje si jej do pracovního adresáře, přejmenuje příponu na .zip a rozzipuje.
    /// <para/>
    /// Programátor, který chce doplnit další resource, si do výše uvedeného rozbaleného adresáře přidá nové ikony nebno adresář s ikonami nebo jiná data,
    /// poté celý adresář zazipuje, přejmenuje celý zip na "ASOL.GraphLib.ics" a vloží soubor zpátky do adresáře GraphLib.
    /// <para/>
    /// Poté programátor spustí GraphLib z Visual studia v režimu Debug, a aplikace při startu nově vygeneruje soubor Components/IconResources.cs, obsahující nově dodané položky jako konstanty.
    /// </summary>
    public class Pic16
    {
        public const string AccessoriesCalculator2Png = "pic16/accessories-calculator-2.png";
        public const string AccessoriesCalculator3Png = "pic16/accessories-calculator-3.png";
        public const string AccessoriesCalculator6Png = "pic16/accessories-calculator-6.png";
        public const string AccessoriesClockPng = "pic16/accessories-clock.png";
        public const string AccessoriesDatePng = "pic16/accessories-date.png";
        public const string ArrowDown2Png = "pic16/arrow-down-2.png";
        public const string ArrowDownBluePng = "pic16/arrow-down-blue.png";
        public const string ArrowDownGrayPng = "pic16/arrow-down-gray.png";
        public const string ArrowDownGreenPng = "pic16/arrow-down-green.png";
        public const string ArrowDownRedPng = "pic16/arrow-down-red.png";
        public const string ArrowDownVioletPng = "pic16/arrow-down-violet.png";
        public const string ArrowDownYellowdarkPng = "pic16/arrow-down-yellowdark.png";
        public const string ArrowDownYellowPng = "pic16/arrow-down-yellow.png";
        public const string ArrowRight2Png = "pic16/arrow-right-2.png";
        public const string ArrowRightBluePng = "pic16/arrow-right-blue.png";
        public const string ArrowRightGrayPng = "pic16/arrow-right-gray.png";
        public const string ArrowRightGreenPng = "pic16/arrow-right-green.png";
        public const string ArrowRightRedPng = "pic16/arrow-right-red.png";
        public const string ArrowRightVioletPng = "pic16/arrow-right-violet.png";
        public const string ArrowRightYellowdarkPng = "pic16/arrow-right-yellowdark.png";
        public const string ArrowRightYellowPng = "pic16/arrow-right-yellow.png";
        public const string BulletBluePng = "pic16/bullet-blue.png";
        public const string BulletGoPng = "pic16/bullet-go.png";
        public const string BulletGreenPng = "pic16/bullet-green.png";
        public const string BulletOrangePng = "pic16/bullet-orange.png";
        public const string BulletPinkPng = "pic16/bullet-pink.png";
        public const string BulletPurplePng = "pic16/bullet-purple.png";
        public const string BulletRedPng = "pic16/bullet-red.png";
        public const string BulletStarPng = "pic16/bullet-star.png";
        public const string BulletWhitePng = "pic16/bullet-white.png";
        public const string BulletYellowPng = "pic16/bullet-yellow.png";
        public const string ComboDownPng = "pic16/combo_down.png";
        public const string DialogCancel4Png = "pic16/dialog-cancel-4.png";
        public const string DialogDisablePng = "pic16/dialog-disable.png";
        public const string DialogError5Png = "pic16/dialog-error-5.png";
        public const string DialogInformation3Png = "pic16/dialog-information-3.png";
        public const string DialogInformation4Png = "pic16/dialog-information-4.png";
        public const string DialogNo3Png = "pic16/dialog-no-3.png";
        public const string DialogOk5Png = "pic16/dialog-ok-5.png";
        public const string DialogOkApply5Png = "pic16/dialog-ok-apply-5.png";
        public const string FolderOrangeOpenPng = "pic16/folder-orange_open.png";
        public const string FolderOrangePng = "pic16/folder-orange.png";
        public const string FolderYellowOpenPng = "pic16/folder-yellow_open.png";
        public const string FolderYellowPng = "pic16/folder-yellow.png";
        public const string GnomeWebBrowserPng = "pic16/gnome-web-browser.png";
        public const string InternetMail5Png = "pic16/internet-mail-5.png";
        public const string InternetPng = "pic16/internet.png";
        public const string MagnifierPng = "pic16/magnifier.png";
        public const string MagnifierZoomInPng = "pic16/magnifier-zoom-in.png";
    }
    #endregion
    #region Pic24
    /// <summary>
    /// Obsah adresáře Pic24
    /// <para/>
    /// Programátor, který chce vidět jednotlivé ikonky, si najde soubor "ASOL.GraphLib.ics" v adresáři GraphLib,
    /// zkopíruje si jej do pracovního adresáře, přejmenuje příponu na .zip a rozzipuje.
    /// <para/>
    /// Programátor, který chce doplnit další resource, si do výše uvedeného rozbaleného adresáře přidá nové ikony nebno adresář s ikonami nebo jiná data,
    /// poté celý adresář zazipuje, přejmenuje celý zip na "ASOL.GraphLib.ics" a vloží soubor zpátky do adresáře GraphLib.
    /// <para/>
    /// Poté programátor spustí GraphLib z Visual studia v režimu Debug, a aplikace při startu nově vygeneruje soubor Components/IconResources.cs, obsahující nově dodané položky jako konstanty.
    /// </summary>
    public class Pic24
    {
        public const string AccessoriesCalculator2Png = "pic24/accessories-calculator-2.png";
        public const string AccessoriesCalculator3Png = "pic24/accessories-calculator-3.png";
        public const string AccessoriesCalculator5Png = "pic24/accessories-calculator-5.png";
        public const string AccessoriesClockPng = "pic24/accessories-clock.png";
        public const string AccessoriesDatePng = "pic24/accessories-date.png";
        public const string ArrowDown2Png = "pic24/arrow-down-2.png";
        public const string ArrowDownBluePng = "pic24/arrow-down-blue.png";
        public const string ArrowDownGrayPng = "pic24/arrow-down-gray.png";
        public const string ArrowDownGreenPng = "pic24/arrow-down-green.png";
        public const string ArrowDownRedPng = "pic24/arrow-down-red.png";
        public const string ArrowDownVioletPng = "pic24/arrow-down-violet.png";
        public const string ArrowDownYellowdarkPng = "pic24/arrow-down-yellowdark.png";
        public const string ArrowDownYellowPng = "pic24/arrow-down-yellow.png";
        public const string ArrowRight2Png = "pic24/arrow-right-2.png";
        public const string ArrowRightBluePng = "pic24/arrow-right-blue.png";
        public const string ArrowRightGrayPng = "pic24/arrow-right-gray.png";
        public const string ArrowRightGreenPng = "pic24/arrow-right-green.png";
        public const string ArrowRightRedPng = "pic24/arrow-right-red.png";
        public const string ArrowRightVioletPng = "pic24/arrow-right-violet.png";
        public const string ArrowRightYellowdarkPng = "pic24/arrow-right-yellowdark.png";
        public const string ArrowRightYellowPng = "pic24/arrow-right-yellow.png";
        public const string ComboDownPng = "pic24/combo_down.png";
        public const string DialogCancel4Png = "pic24/dialog-cancel-4.png";
        public const string DialogDisablePng = "pic24/dialog-disable.png";
        public const string DialogError5Png = "pic24/dialog-error-5.png";
        public const string DialogInformation3Png = "pic24/dialog-information-3.png";
        public const string DialogInformation4Png = "pic24/dialog-information-4.png";
        public const string DialogNo3Png = "pic24/dialog-no-3.png";
        public const string DialogOk5Png = "pic24/dialog-ok-5.png";
        public const string DialogOkApply5Png = "pic24/dialog-ok-apply-5.png";
        public const string FolderOrangeOpenPng = "pic24/folder-orange_open.png";
        public const string FolderOrangePng = "pic24/folder-orange.png";
        public const string FolderYellowOpenPng = "pic24/folder-yellow_open.png";
        public const string FolderYellowPng = "pic24/folder-yellow.png";
        public const string GnomeWebBrowserPng = "pic24/gnome-web-browser.png";
        public const string InternetMail5Png = "pic24/internet-mail-5.png";
        public const string InternetPng = "pic24/internet.png";
        public const string Zoom3Png = "pic24/zoom-3.png";
        public const string ZoomIn4Png = "pic24/zoom-in-4.png";
        public const string ZoomOut4Png = "pic24/zoom-out-4.png";
    }
    #endregion
    #region Pic32
    /// <summary>
    /// Obsah adresáře Pic32
    /// <para/>
    /// Programátor, který chce vidět jednotlivé ikonky, si najde soubor "ASOL.GraphLib.ics" v adresáři GraphLib,
    /// zkopíruje si jej do pracovního adresáře, přejmenuje příponu na .zip a rozzipuje.
    /// <para/>
    /// Programátor, který chce doplnit další resource, si do výše uvedeného rozbaleného adresáře přidá nové ikony nebno adresář s ikonami nebo jiná data,
    /// poté celý adresář zazipuje, přejmenuje celý zip na "ASOL.GraphLib.ics" a vloží soubor zpátky do adresáře GraphLib.
    /// <para/>
    /// Poté programátor spustí GraphLib z Visual studia v režimu Debug, a aplikace při startu nově vygeneruje soubor Components/IconResources.cs, obsahující nově dodané položky jako konstanty.
    /// </summary>
    public class Pic32
    {
        public const string AccessoriesCalculator2Png = "pic32/accessories-calculator-2.png";
        public const string AccessoriesCalculator3Png = "pic32/accessories-calculator-3.png";
        public const string AccessoriesCalculator5Png = "pic32/accessories-calculator-5.png";
        public const string AccessoriesClockPng = "pic32/accessories-clock.png";
        public const string AccessoriesDatePng = "pic32/accessories-date.png";
        public const string ArrowDown2Png = "pic32/arrow-down-2.png";
        public const string ArrowDownBluePng = "pic32/arrow-down-blue.png";
        public const string ArrowDownGrayPng = "pic32/arrow-down-gray.png";
        public const string ArrowDownGreenPng = "pic32/arrow-down-green.png";
        public const string ArrowDownRedPng = "pic32/arrow-down-red.png";
        public const string ArrowDownVioletPng = "pic32/arrow-down-violet.png";
        public const string ArrowDownYellowdarkPng = "pic32/arrow-down-yellowdark.png";
        public const string ArrowDownYellowPng = "pic32/arrow-down-yellow.png";
        public const string ArrowRight2Png = "pic32/arrow-right-2.png";
        public const string ArrowRightBluePng = "pic32/arrow-right-blue.png";
        public const string ArrowRightGrayPng = "pic32/arrow-right-gray.png";
        public const string ArrowRightGreenPng = "pic32/arrow-right-green.png";
        public const string ArrowRightRedPng = "pic32/arrow-right-red.png";
        public const string ArrowRightVioletPng = "pic32/arrow-right-violet.png";
        public const string ArrowRightYellowdarkPng = "pic32/arrow-right-yellowdark.png";
        public const string ArrowRightYellowPng = "pic32/arrow-right-yellow.png";
        public const string ComboDownPng = "pic32/combo_down.png";
        public const string DialogCancel4Png = "pic32/dialog-cancel-4.png";
        public const string DialogDisablePng = "pic32/dialog-disable.png";
        public const string DialogError5Png = "pic32/dialog-error-5.png";
        public const string DialogInformation3Png = "pic32/dialog-information-3.png";
        public const string DialogInformation4Png = "pic32/dialog-information-4.png";
        public const string DialogNo3Png = "pic32/dialog-no-3.png";
        public const string DialogOk5Png = "pic32/dialog-ok-5.png";
        public const string DialogOkApply5Png = "pic32/dialog-ok-apply-5.png";
        public const string FolderOrangeOpenPng = "pic32/folder-orange_open.png";
        public const string FolderOrangePng = "pic32/folder-orange.png";
        public const string FolderYellowOpenPng = "pic32/folder-yellow_open.png";
        public const string FolderYellowPng = "pic32/folder-yellow.png";
        public const string GnomeWebBrowserPng = "pic32/gnome-web-browser.png";
        public const string InternetMail5Png = "pic32/internet-mail-5.png";
        public const string InternetPng = "pic32/internet.png";
        public const string Zoom3Png = "pic32/zoom-3.png";
        public const string ZoomIn4Png = "pic32/zoom-in-4.png";
        public const string ZoomOut4Png = "pic32/zoom-out-4.png";
    }
    #endregion
}
#pragma warning restore 1591
