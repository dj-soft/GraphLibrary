﻿// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using DevExpress.XtraEditors.Repository;
using Noris.Clients.Win.Components.AsolDX.DxForm;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    /// <summary>
    /// Třída, která načte XML soubor / stream obsahující <see cref="DfForm"/>, i rekurzivně (nested panely a grupy).
    /// Výstupem této třídy je exaktní obraz hodnot, tak jak jsou zadané v XML dokumentu.
    /// Tato třída neprovádí interní přepočty souřadnic ani doplňování labelů.
    /// </summary>
    internal static class DfTemplateLoader
    {
        #region Načítání obsahu a načítání Info - public rozhraní
        /// <summary>
        /// Načte a vrátí <see cref="DfForm"/> ze zadaného souboru
        /// </summary>
        /// <returns></returns>
        internal static DfForm LoadTemplate(DfTemplateLoadArgs args)
        {
            var dfForm = _LoadDfForm(args, false);
            return dfForm;
        }
        /// <summary>
        /// Načte a vrátí basic informace <see cref="DfInfoForm"/> ze zadaného souboru
        /// </summary>
        /// <returns></returns>
        internal static DfInfoForm LoadInfo(DfTemplateLoadArgs args)
        {
            var dfForm = _LoadDfForm(args, true);
            return _CreateInfoForm(dfForm);
        }
        /// <summary>
        /// Z dodaného kompletního <see cref="DfForm"/> vytvoří jednoduchý <see cref="DfInfoForm"/>: přenese pouze základní atributy 
        /// <see cref="DfForm.XmlNamespace"/> a <see cref="DfForm.FormatVersion"/>.
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        private static DfInfoForm _CreateInfoForm(DfForm form)
        {
            return new DfInfoForm() { XmlNamespace = form?.XmlNamespace, FormatVersion = form?.FormatVersion ?? FormatVersionType.Default };
        }
        #endregion
        #region Enumerace prvků formuláře
        /// <summary>
        /// Metoda vyvolá danou akci pro každou stránku
        /// </summary>
        /// <param name="dfForm">Formulář</param>
        /// <param name="scanAction">Akce provedená pro každou stránku</param>
        public static void ScanPages(DfForm dfForm, Action<DfPage> scanAction)
        {
            _ScanForm(dfForm, i =>
            {
                switch (i)
                {
                    case DfForm form:
                        return ScanResultType.ScanInto;
                    case DfPage page:
                        scanAction(page);
                        return ScanResultType.ScanNext;
                    default:
                        return ScanResultType.ScanNext;
                }
            });
        }
        /// <summary>
        /// Metoda vyvolá danou akci pro každý panel na kterékoli stránce
        /// </summary>
        /// <param name="dfForm">Formulář</param>
        /// <param name="scanAction">Akce provedená pro každý panel</param>
        public static void ScanPanels(DfForm dfForm, Action<DfPanel> scanAction)
        {
            _ScanForm(dfForm, i =>
            {
                switch (i)
                {
                    case DfForm form:
                    case DfPage page:
                        return ScanResultType.ScanInto;
                    case DfPanel panel:
                        scanAction(panel);
                        return ScanResultType.ScanNext;
                    default:
                        return ScanResultType.ScanNext;
                }
            });
        }
        /// <summary>
        /// Metoda vyvolá danou akci pro každý control na kterékoli stránce a panelu a grupě
        /// </summary>
        /// <param name="dfForm">Formulář</param>
        /// <param name="scanAction">Akce provedená pro každý control</param>
        public static void ScanControls(DfForm dfForm, Action<DfBaseControl> scanAction)
        {
            _ScanForm(dfForm, i =>
            {
                switch (i)
                {
                    case DfForm form:
                    case DfPage page:
                    case DfPanel panel:
                    case DfGroup group:
                        return ScanResultType.ScanInto;
                    case DfBaseControl control:
                        scanAction(control);
                        return ScanResultType.ScanNext;
                    default:
                        return ScanResultType.ScanNext;
                }
            });
        }
        /// <summary>
        /// Metoda postupně prochází celou hierarchii dodaného formuláře (šablony) <paramref name="dfForm"/>, a postupně každý prvek předává do dodaného <paramref name="scanner"/>.
        /// Tento scanner detekuje typ konkrétního prvku, provede s ním svoji akci, a vrátí informaci, zda prohledávat i child prvky daného prvku, nebo nás childs nezajímají, ale hledat se má dál, anebo rovnou skončit.
        /// </summary>
        /// <param name="dfForm"></param>
        /// <param name="scanner"></param>
        private static void _ScanForm(DfForm dfForm, Func<DfBase, ScanResultType> scanner)
        {
            if (dfForm is null || scanner is null) return;                     // Nejsou data
            ScanResultType result = scanOneItem(dfForm);
            if (result == ScanResultType.End) return;                          // Scanner odmítl samotný Form
            if (dfForm.Pages is null || dfForm.Pages.Count == 0) return;       // Nejsou Pages

            foreach (var page in dfForm.Pages)                                 // Projdi všechny stránky dané šablony
            {
                result = scanOneItem(page);                                    // Co s tou stránkou?
                if (result == ScanResultType.End) return;
                if (result == ScanResultType.ScanInto && page.Panels != null && page.Panels.Count > 0)
                {
                    foreach (var panel in page.Panels)                         // Projdi všechny panely dané stránky
                    {
                        result = scanOneItem(panel);                           // Co s tím panelem?
                        if (result == ScanResultType.End) return;
                        if (result == ScanResultType.ScanInto && panel.Childs != null && panel.Childs.Count > 0)
                        {
                            bool isEnd = scanItems(panel.Childs);
                            if (isEnd) return;
                        }
                    }
                }
            }
            // Určí výsledek scanování pro daný prvek
            ScanResultType scanOneItem(DfBase item)
            {
                return (item is null ? ScanResultType.ScanNext : scanner(item));
            }
            // Scanuje prvky v dodané kolekci
            bool scanItems(IEnumerable<DfBase> items)
            {
                if (items is null) return false;

                foreach (var item in items)                                    // Projdi všechny prvky dané kolekce
                {
                    result = scanOneItem(item);                                // Co s tím prvkem?
                    if (result == ScanResultType.End) return true;             // End daného prvku končí celý Scan
                    if (result == ScanResultType.ScanInto)
                    {   // Máme hledat i uvnitř... To už není tak triviální, záleží totiž na typu prvku...
                        bool isEnd = scanSubItemsIn(item);
                        if (isEnd) return true;
                    }
                }

                return false;
            }
            // Scanuje subprvky daného prvku, podle konkrétního typu
            bool scanSubItemsIn(DfBase parent)
            {
                switch (parent)
                {
                    case DfGroup group: return scanItems(group.Childs);
                }
                return false;
            }
        }
        /// <summary>
        /// Volba dalšího kroku při scanování obsahu
        /// </summary>
        private enum ScanResultType
        {
            /// <summary>
            /// Pokud aktuální prvek má svoje SubPrvky, hledej i v nich...
            /// </summary>
            ScanInto,
            /// <summary>
            /// Ignoruj subprvky aktuálního prvku, a hledej další prvek v téže úrovni a pak v nadřízených
            /// </summary>
            ScanNext,
            /// <summary>
            /// Skonči hledání
            /// </summary>
            End
        }
        #endregion
        #region Servis nad daty: GetAllControls()
        internal static DfBaseControl[] GetAllControls(DfForm form)
        {
            List<DfBaseControl> result = new List<DfBaseControl>();
            _AddAllControls(form, result);
            return result.ToArray();
        }
        internal static DfBaseControl[] GetAllControls(DfPage page)
        {
            List<DfBaseControl> result = new List<DfBaseControl>();
            _AddAllControls(page, result);
            return result.ToArray();
        }
        internal static DfBaseControl[] GetAllControls(DfPanel panel)
        {
            List<DfBaseControl> result = new List<DfBaseControl>();
            _AddAllControls(panel, result);
            return result.ToArray();
        }
        /// <summary>
        /// Přidá všechny controly z daného formu. Rekurzivně i pro Child těchto continerů.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="result"></param>
        private static void _AddAllControls(DfForm form, List<DfBaseControl> result)
        {
            var pages = form?.Pages;
            if (pages != null)
                pages.ForEach(p => _AddAllControls(p, result));
        }
        /// <summary>
        /// Přidá všechny controly z dané stránky. Rekurzivně i pro Child těchto continerů.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="result"></param>
        private static void _AddAllControls(DfPage page, List<DfBaseControl> result)
        {
            var panels = page?.Panels;
            if (panels != null)
                panels.ForEach(p => _AddAllControls(p, result));
        }
        /// <summary>
        /// Přidá všechny controly z daného panelu. Rekurzivně i pro Child těchto continerů.
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="result"></param>
        private static void _AddAllControls(DfPanel panel, List<DfBaseControl> result)
        {
            var childs = panel?.Childs;
            if (childs != null)
                childs.ForEach(p => _AddAllControls(p, result));
        }
        /// <summary>
        /// Přidá všechny controly pro daný prvek.. Rekurzivně i pro Child těchto continerů.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="result"></param>
        private static void _AddAllControls(DfBase child, List<DfBaseControl> result)
        {
            switch (child)
            {
                case DfBaseControl control: result.Add(control); break;
                case DfPage page: _AddAllControls(page, result); break;
                case DfPanel panel: _AddAllControls(panel, result); break;
            }
        }
        #endregion
        #region Načítání obsahu - private tvorba containerů

        // Každá zdejší větev / metoda načte pouze property deklarované přímo pro danou třídu, nikoli pro její předky!
        // Třídy předků načítá metoda _FillBaseAttributes(xElement, control);

        /// <summary>
        /// Z dodaných dat <paramref name="args"/> načte a vrátí odpovídající <see cref="DfForm"/>, volitelně včetně jeho obsahu podle <paramref name="onlyInfo"/>.
        /// </summary>
        /// <param name="args">Parametry pro načítání obsahu</param>
        /// <param name="onlyInfo"></param>
        /// <returns></returns>
        private static DfForm _LoadDfForm(DfTemplateLoadArgs args, bool onlyInfo)
        {
            DfContext context = new DfContext(args, onlyInfo);
            XElement xElement = _LoadRootXElement(context);
            if (xElement is null) return null;

            var startTime = DxComponent.LogTimeCurrent;
            var dfForm = _FillAreaDfForm(xElement, null, context);
            if (args.LogTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load '{(onlyInfo ? "DfInfo" : "DfForm")}' from 'XDocument': {DxComponent.LogTokenTimeMicrosec}", startTime);

            return dfForm;
        }
        /// <summary>
        /// Z dodaných podkladů, definujících šablonu (<paramref name="context"/>), načte a vrátí Root element <see cref="XElement"/>.
        /// </summary>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        /// <returns></returns>
        private static XElement _LoadRootXElement(DfContext context)
        {
            var args = context.LoadArgs;

            // Pokud není zadáno nic, co by určilo šablonu:
            if (args.TemplateDocument is null && String.IsNullOrEmpty(args.TemplateContent) && String.IsNullOrEmpty(args.TemplateFileName))
            {
                context.AddError($"Pro načtení formuláře nejsou předaná žádná data (DfTemplateArgs: TemplateFileName, TemplateContent, TemplateDocument jsou prázdné).");
                return null;
            }

            // Je zadán pouze název souboru => budeme načítat obsah souboru do TemplateContent:
            if (args.TemplateDocument is null && String.IsNullOrEmpty(args.TemplateContent))
            {   // Načteme soubor, pokud existuje:
                if (!System.IO.File.Exists(args.TemplateFileName)) 
                {
                    context.AddError($"Pro načtení formuláře je dodán název souboru '{args.TemplateFileName}', ten ale neexistuje.");
                    return null;
                }
                var startTime = DxComponent.LogTimeCurrent;
                args.TemplateContent = System.IO.File.ReadAllText(args.TemplateFileName);
                if (args.LogTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load 'Content' from file '{System.IO.Path.GetFileName(args.TemplateFileName)}': {DxComponent.LogTokenTimeMicrosec}", startTime);
            }

            // Nemáme parsovaný XDocument, ale máme stringový obsah => budeme jej parsovat do TemplateDocument:
            if (args.TemplateDocument is null && !String.IsNullOrEmpty(args.TemplateContent))
            {
                var startTime = DxComponent.LogTimeCurrent;
                args.TemplateDocument = XDocument.Parse(args.TemplateContent);
                if (args.LogTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Parse 'XDocument' from 'Content' ({args.TemplateContent.Length} B):': {DxComponent.LogTokenTimeMicrosec}", startTime);
            }

            return args.TemplateDocument?.Root;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <see cref="DfForm"/>.
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="dfForm"></param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        /// <returns></returns>
        private static DfForm _FillAreaDfForm(XElement xElement, DfForm dfForm, DfContext context)
        {
            if (xElement is null) return null;
            if (dfForm is null) dfForm = new DfForm();

            // Specifické Atributy:
            dfForm.XmlNamespace = _ReadAttributeString(xElement, "xmlns", null);
            dfForm.FormatVersion = _ReadAttributeEnum(xElement, "FormatVersion", FormatVersionType.Default, t => "Version" + t);
            context.Form = dfForm;
           
            // Rychlá odbočka, pokud nám stačí jen Namespace a FormatVersion?
            if (context.OnlyInfo) return dfForm;

            // FileName (a Name, pokud není explicitně načteno) podle jména souboru:
            dfForm.FileName = context.LoadArgs.TemplateFileName;
            if (dfForm.Name is null && !String.IsNullOrEmpty(context.LoadArgs.TemplateFileName)) dfForm.Name = System.IO.Path.GetFileNameWithoutExtension(context.LoadArgs.TemplateFileName ?? "");

            switch (dfForm.FormatVersion)
            {
                case FormatVersionType.Version1:
                case FormatVersionType.Version2:
                case FormatVersionType.Version3:
                    return _FillAreaDfFormIG(xElement, dfForm, context);
                case FormatVersionType.Version4:
                case FormatVersionType.Default:
                default:
                    return _FillAreaDfFormDX(xElement, dfForm, context);
            }
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <see cref="DfForm"/>.
        /// Specifická větev pro data verze 4
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="dfForm"></param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        /// <returns></returns>
        private static DfForm _FillAreaDfFormDX(XElement xElement, DfForm dfForm, DfContext context)
        {
            // Základní Atributy:
            _FillBaseAttributes(xElement, dfForm, context);

            // Atributy třídy DfForm:
            dfForm.MasterWidth = _ReadAttributeInt32N(xElement, "MasterWidth");
            dfForm.MasterHeight = _ReadAttributeInt32N(xElement, "MasterHeight");
            dfForm.TotalWidth = _ReadAttributeInt32N(xElement, "TotalWidth");
            dfForm.TotalHeight = _ReadAttributeInt32N(xElement, "TotalHeight");
            dfForm.DataSource = _ReadAttributeString(xElement, "DataSource", null);
            dfForm.Messages = _ReadAttributeString(xElement, "Messages", null);
            dfForm.UseNorisClass = _ReadAttributeInt32N(xElement, "UseNorisClass");
            dfForm.AddUda = _ReadAttributeBoolN(xElement, "AddUda");
            dfForm.UdaLabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "UdaLabelPosition", _FixLabelPosition);
            dfForm.ContextMenu = _ReadAttributeBoolN(xElement, "ContextMenu");

            // Implicit Page: do ní se vkládají Panely, pokud jsou zadány přímo do Formu
            DfPage implicitPage = null;

            // Elementy = stránky (nebo přímo panely):
            var xContainers = xElement.Elements();
            if (xContainers != null)
            {
                string sourceInfo = $"Formulář '{dfForm.Name}'";
                foreach (var xContainer in xContainers)
                {
                    var container = _CreateArea(xContainer, sourceInfo, context, "page", "panel", "nestedpanel");
                    if (container != null)
                    {
                        if (dfForm.Pages is null) dfForm.Pages = new List<DfPage>();
                        switch (container)
                        {
                            case DfPage page:
                                dfForm.Pages.Add(page);
                                break;
                            case DfPanel panel:
                                // Pokud je v rámci DfForm (=template) zadán přímo Panel (pro jednoduchost to umožňujeme), 
                                //  pak jej vložím do implicitPage = ta bude první v seznamu stránek:
                                if (implicitPage is null) implicitPage = createImplicitPage();
                                implicitPage.Panels.Add(panel);
                                break;
                        }
                    }
                }
            }
            return dfForm;


            // Vytvoří new instanci DfPage jako "Implicitní stránku", přidá ji jako stránku [0] do control.Pages a vrátí ji
            DfPage createImplicitPage()
            {
                DfPage iPage = new DfPage();
                iPage.Name = Guid.NewGuid().ToString();
                iPage.Panels = new List<DfPanel>();
                if (dfForm.Pages.Count == 0)
                    dfForm.Pages.Add(iPage);
                else
                    dfForm.Pages.Insert(0, iPage);
                return iPage;
            }
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající kontejner (page, panel, nestedpanel, group, nestedgroup), včetně jeho obsahu a child prvků.
        /// Výstupem je tedy buď <see cref="DfPage"/> nebo <see cref="DfPanel"/> (nebo null).
        /// Pokud je výstupem null, pak informace o chybě je již zanesena do <paramref name="context"/>, kam se uvádí i zdroj = <paramref name="sourceInfo"/>.
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="sourceInfo">Informace o zdrojovém místě, do chybové informace. Typicky: "Formulář 'Jméno'" nebo "Stránka 'Name01'".</param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        /// <param name="validNames">Očekávaná validní jména elementů. Pokud je zadáno, a je detekován jiný než daný element, vrátí se null.</param>
        private static DfBaseArea _CreateArea(XElement xElement, string sourceInfo, DfContext context, params string[] validNames)
        {
            string elementName = _GetValidElementName(xElement);               // page, panel, nestedpanel, group, nestedgroup
            if (String.IsNullOrEmpty(elementName)) return null;                // Nezadáno (?)
            // Pokud je dodán seznam validních jmen elementů (přinejmenším 1 prvek), ale aktuální element neodpovídá žádnému povolenému jménu, pak skončím:
            if (validNames != null && validNames.Length > 0 && !validNames.Any(v => String.Equals(v, elementName, StringComparison.OrdinalIgnoreCase)))
            {
                context.AddError($"{sourceInfo} obsahuje prvek '{elementName}', který není přípustný.");
                return null;
            }
            switch (elementName)
            {
                case "page": return _FillAreaPage(xElement, null, context);
                case "panel": return _FillAreaPanel(xElement, null, context);
                case "nestedpanel": return _FillAreaNestedPanel(xElement, null, context);
                case "group": return _FillAreaGroup(xElement, null, context);
                case "nestedgroup": return _FillAreaNestedGroup(xElement, null, context);
            }
            context.AddError($"{sourceInfo} obsahuje prvek '{elementName}', který zde není očekáváván.");
            return null;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <c>page</c>, včetně jeho obsahu (tj. atributy a child elementy).
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="dfPage"></param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        /// <returns></returns>
        private static DfPage _FillAreaPage(XElement xElement, DfPage dfPage, DfContext context)
        {
            // Výsledná instance:
            if (dfPage is null) dfPage = new DfPage();

            // Atributy:
            _FillBaseAttributes(xElement, dfPage, context);
            dfPage.IconName = _ReadAttributeString(xElement, "IconName", null);
            dfPage.Title = _ReadAttributeUserText(xElement, "Title", null, null, context);

            // Elementy = panely a nested panely:
            var xPanels = xElement.Elements();
            if (xPanels != null)
            {
                string sourceInfo = $"Stránka '{dfPage.Name}'";
                foreach (var xPanel in xPanels)
                {
                    var container = _CreateArea(xPanel, sourceInfo, context, "panel", "nestedpanel");
                    if (container != null && container is DfPanel panel)
                    {
                        if (dfPage.Panels is null) dfPage.Panels = new List<DfPanel>();
                        dfPage.Panels.Add(panel);
                    }
                }
            }

            return dfPage;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <c>panel</c>, včetně jeho obsahu (tj. atributy a child elementy).
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="dfPanel"></param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        /// <returns></returns>
        private static DfPanel _FillAreaPanel(XElement xElement, DfPanel dfPanel, DfContext context)
        {
            // Výsledná instance:
            if (dfPanel is null) dfPanel = new DfPanel();

            // Atributy:
            _FillBaseAttributes(xElement, dfPanel, context);
            dfPanel.IsHeader = _ReadAttributeBoolN(xElement, "IsHeader");
            dfPanel.HeaderOnPages = _ReadAttributeString(xElement, "HeaderOnPages", null);
            dfPanel.IconName = _ReadAttributeString(xElement, "IconName", null);
            dfPanel.Title = _ReadAttributeUserText(xElement, "Title", null, null, context);
            dfPanel.TitleStyle = _ReadAttributeEnumN<TitleStyleType>(xElement, "TitleStyle");
            dfPanel.TitleColorName = _ReadAttributeString(xElement, "TitleColorName", null);
            dfPanel.TitleColorLight = _ReadAttributeColorN(xElement, "TitleColorLight");
            dfPanel.TitleColorDark = _ReadAttributeColorN(xElement, "TitleColorDark");
            dfPanel.CollapseState = _ReadAttributeEnumN<PanelCollapseState>(xElement, "CollapseState");

            // Elementy = Controly + Grupy:
            _FillContainerChildElements(xElement, dfPanel, context);

            return dfPanel;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <c>nestedpanel</c>, včetně jeho obsahu (tj. atributy a child elementy).
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="dfPanelVoid"></param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        /// <returns></returns>
        private static DfPanel _FillAreaNestedPanel(XElement xElement, DfPanel dfPanelVoid, DfContext context)
        {
            // Instance DfNestedPanel slouží k načtení definice z aktuálního formuláře, ale nejde o výslednou instanci:
            DfNestedPanel dfNestedPanel = new DfNestedPanel();

            // Atributy:
            _FillBaseAttributes(xElement, dfNestedPanel, context);
            dfNestedPanel.NestedTemplate = _ReadAttributeString(xElement, "NestedTemplate", null);
            dfNestedPanel.NestedPanelName = _ReadAttributeString(xElement, "NestedPanelName", null);
            dfNestedPanel.IsHeader = _ReadAttributeBoolN(xElement, "IsHeader");
            dfNestedPanel.HeaderOnPages = _ReadAttributeString(xElement, "HeaderOnPages", null);

            // Nested šablona:
            if (!_TryLoadNestedTemplate(dfNestedPanel.NestedTemplate, context, out DfForm dfNestedForm, $"NestedPanel '{dfNestedPanel.Name}'")) return null;

            // Najde v načtené šabloně první panel DfPanel [kterýkoli nebo daného jména]:
            if (trySearchForPanel(dfNestedForm, dfNestedPanel.NestedPanelName, out DfPanel dfPanel))
            {   // Převezmeme něco z našeho NestedPanelu do výstupního DfPanel:
                dfPanel.Name = dfNestedPanel.Name;
                if (dfNestedPanel.State.HasValue) dfPanel.State = dfNestedPanel.State;
                if (dfNestedPanel.ToolTipTitle != null) dfPanel.ToolTipTitle = dfNestedPanel.ToolTipTitle;
                if (dfNestedPanel.ToolTipText != null) dfPanel.ToolTipText = dfNestedPanel.ToolTipText;
                if (dfNestedPanel.Invisible != null) dfPanel.Invisible = dfNestedPanel.Invisible;
                if (dfNestedPanel.IsHeader.HasValue) dfPanel.IsHeader = dfNestedPanel.IsHeader.Value;
                if (dfNestedPanel.HeaderOnPages != null) dfPanel.HeaderOnPages = dfNestedPanel.HeaderOnPages;
            }
            else
            {
                context.AddError($"Požadovaný NestedPanel '{dfNestedPanel.NestedPanelName}' nebyl v nested šabloně '{dfNestedPanel.NestedTemplate}' nalezen.");
            }
            return dfPanel;


            // V daném formuláři najde první panel [daného jména]
            bool trySearchForPanel(DfForm nestedForm, string panelName, out DfPanel panel)
            {
                DfPanel foundPanel = null;
                bool searchAny = String.IsNullOrEmpty(panelName);

                _ScanForm(nestedForm, i =>
                {
                    if (i is DfForm || i is DfPage) return ScanResultType.ScanInto;      // Formulář a jeho Page prohledávejme i dovnitř = hledáme Panel uvnitř stránky...
                    if (i is DfPanel p)
                    {   // Našli jsme panel: hledáme kterýkoli první, anebo konkrétní jméno?
                        if (searchAny || (String.Equals(p.Name, panelName, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (foundPanel is null)                                      // Pojistka proti chybě ve scanneru
                                foundPanel = p;
                            return ScanResultType.End;                                   // Tady scanner skončí.
                        }
                        // Je to sice panel, ale ne ten, který hledáme:
                        return ScanResultType.ScanNext;
                    }
                    // Na vstupu je něco jiného: hledejme dál (ale ne dovnitř):
                    return ScanResultType.ScanNext;
                });

                panel = foundPanel;
                return (foundPanel != null);
            }
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <c>group</c>, včetně jeho obsahu (tj. atributy a child elementy).
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="dfGroup"></param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        /// <returns></returns>
        private static DfGroup _FillAreaGroup(XElement xElement, DfGroup dfGroup, DfContext context)
        {
            // Výsledná instance:
            if (dfGroup is null) dfGroup = new DfGroup();

            // Atributy:
            _FillBaseAttributes(xElement, dfGroup, context);
            dfGroup.Break = _ReadAttributeBoolNX(xElement, "Break", null, true);
            dfGroup.ColIndex = _ReadAttributeInt32N(xElement, "ColIndex");
            dfGroup.ColSpan = _ReadAttributeInt32N(xElement, "ColSpan");
            dfGroup.RowSpan = _ReadAttributeInt32N(xElement, "RowSpan");
            dfGroup.HPosition = _ReadAttributeEnumN<HPositionType>(xElement, "HPosition");
            dfGroup.VPosition = _ReadAttributeEnumN<VPositionType>(xElement, "VPosition");
            dfGroup.ExpandControl = _ReadAttributeEnumN<ExpandControlType>(xElement, "ExpandControl");
            dfGroup.Label = _ReadAttributeUserText(xElement, "Label", null, null, context);
            dfGroup.LabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "LabelPosition", _FixLabelPosition);
            dfGroup.LabelWidth = _ReadAttributeInt32N(xElement, "LabelWidth");

            // Elementy = Controly + Panely:
            _FillContainerChildElements(xElement, dfGroup, context);

            return dfGroup;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <c>nestedgroup</c>, včetně jeho obsahu (tj. atributy a child elementy).
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="dfGroupVoid"></param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        /// <returns></returns>
        private static DfGroup _FillAreaNestedGroup(XElement xElement, DfGroup dfGroupVoid, DfContext context)
        {
            // Instance DfNestedGroup slouží k načtení definice z aktuálního formuláře, ale nejde o výslednou instanci:
            DfNestedGroup dfNestedGroup = new DfNestedGroup();

            // Atributy:
            _FillBaseAttributes(xElement, dfNestedGroup, context);
            dfNestedGroup.NestedTemplate = _ReadAttributeString(xElement, "NestedTemplate", null);
            dfNestedGroup.NestedGroupName = _ReadAttributeString(xElement, "NestedPanelName", null);
            dfNestedGroup.DesignBounds = _ReadAttributeBounds(xElement, null);
            dfNestedGroup.ParentBoundsName = _ReadAttributeString(xElement, "ParentBoundsName", null);
            dfNestedGroup.Break = _ReadAttributeBoolNX(xElement, "Break", null, true);
            dfNestedGroup.ColIndex = _ReadAttributeInt32N(xElement, "ColIndex");
            dfNestedGroup.ColSpan = _ReadAttributeInt32N(xElement, "ColSpan");
            dfNestedGroup.RowSpan = _ReadAttributeInt32N(xElement, "RowSpan");
            dfNestedGroup.HPosition = _ReadAttributeEnumN<HPositionType>(xElement, "HPosition");
            dfNestedGroup.VPosition = _ReadAttributeEnumN<VPositionType>(xElement, "VPosition");
            dfNestedGroup.ExpandControl = _ReadAttributeEnumN<ExpandControlType>(xElement, "ExpandControl");
            dfNestedGroup.Label = _ReadAttributeUserText(xElement, "Label", null, null, context);
            dfNestedGroup.LabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "LabelPosition", _FixLabelPosition);
            dfNestedGroup.LabelWidth = _ReadAttributeInt32N(xElement, "LabelWidth");

            // Nested šablona:
            if (!_TryLoadNestedTemplate(dfNestedGroup.NestedTemplate, context, out DfForm dfNestedForm, $"NestedGroup '{dfNestedGroup.Name}'")) return null;

            // Najde v načtené šabloně první panel DfPanel [kterýkoli nebo daného jména]:
            if (trySearchForGroup(dfNestedForm, dfNestedGroup.NestedGroupName, out DfGroup dfGroup))
            {   // Převezmeme něco z naší DfNestedGroup do výstupní DfGroup:
                dfGroup.Name = dfNestedGroup.Name;

                if (dfNestedGroup.State.HasValue) dfGroup.State = dfNestedGroup.State;
                if (dfNestedGroup.ToolTipTitle != null) dfGroup.ToolTipTitle = dfNestedGroup.ToolTipTitle;
                if (dfNestedGroup.ToolTipText != null) dfGroup.ToolTipText = dfNestedGroup.ToolTipText;
                if (dfNestedGroup.Invisible != null) dfGroup.Invisible = dfNestedGroup.Invisible;
                dfGroup.DesignBounds = dfNestedGroup.DesignBounds;
                dfGroup.ParentBoundsName = dfNestedGroup.ParentBoundsName;
                dfGroup.Break = dfNestedGroup.Break;
                dfGroup.ColIndex = dfNestedGroup.ColIndex;
                dfGroup.ColSpan = dfNestedGroup.ColSpan;
                dfGroup.RowSpan = dfNestedGroup.RowSpan;
                dfGroup.HPosition = dfNestedGroup.HPosition;
                dfGroup.VPosition = dfNestedGroup.VPosition;
                dfGroup.ExpandControl = dfNestedGroup.ExpandControl;
                dfGroup.Label = dfNestedGroup.Label;
                dfGroup.LabelPosition = dfNestedGroup.LabelPosition;
                dfGroup.LabelWidth = dfNestedGroup.LabelWidth;
            }
            else
            {
                context.AddError($"Požadovaná NestedGroup '{dfNestedGroup.NestedGroupName}' nebyla v nested šabloně '{dfNestedGroup.NestedTemplate}' nalezena.");
            }
            return dfGroup;


            // V daném formuláři najde první grupu [daného jména]
            bool trySearchForGroup(DfForm nestedForm, string groupName, out DfGroup group)
            {
                DfGroup foundGroup = null;
                bool searchAny = String.IsNullOrEmpty(groupName);

                _ScanForm(nestedForm, i =>
                {
                    if (i is DfForm || i is DfPage || i is DfPanel) return ScanResultType.ScanInto;          // Formulář a jeho Page a jejich Panel prohledávejme i dovnitř = hledáme Group uvnitř panelu...
                    if (i is DfGroup g)
                    {   // Našli jsme grupu: hledáme kteroukoli první, anebo konkrétní jméno?
                        if (searchAny || (String.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (foundGroup is null)                                      // Pojistka proti chybě ve scanneru
                                foundGroup = g;
                            return ScanResultType.End;                                   // Tady scanner skončí.
                        }
                        // Je to sice grupa, ale ne ta, kterou hledáme:
                        return ScanResultType.ScanNext;
                    }
                    // Na vstupu je něco jiného: hledejme dál (ale ne dovnitř):
                    return ScanResultType.ScanNext;
                });

                group = foundGroup;
                return (foundGroup != null);
            }
        }
        /// <summary>
        /// Z dat dodaného XElementu <paramref name="xElement"/> načte jeho Child elementy a vytvoří z nich Child controly které vloží do dodaného containeru <paramref name="control"/>.
        /// </summary>
        /// <param name="xElement">Element, z jehož Child elementů se mají načítat Child controly</param>
        /// <param name="control"></param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        private static void _FillContainerChildElements(XElement xElement, DfBaseContainer control, DfContext context)
        {
            var xChilds = xElement.Elements();
            if (xChilds != null)
            {
                string sourceInfo = $"Container {control.GetType().Name} '{control.Name}'";
                foreach (var xChild in xChilds)
                {
                    var child = _CreateChildItem(xChild, sourceInfo, context);
                    if (child != null && (child is DfBaseControl || child is DfGroup))
                    {   // Pouze Controly + Group
                        if (control.Childs is null) control.Childs = new List<DfBase>();
                        control.Childs.Add(child);
                    }
                }
            }
        }
        /// <summary>
        /// Metoda zajistí načtení instance <see cref="DfForm"/> pro danou <paramref name="dfNestedForm"/>, s pomocí loader v <paramref name="context"/>.
        /// </summary>
        /// <param name="nestedTemplate"></param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        /// <param name="dfNestedForm"></param>
        /// <param name="sourceInfo"></param>
        /// <returns></returns>
        private static bool _TryLoadNestedTemplate(string nestedTemplate, DfContext context, out DfForm dfNestedForm, string sourceInfo)
        {
            dfNestedForm = null;

            // Jméno nested šablony:
            if (String.IsNullOrEmpty(nestedTemplate))
            {
                context.AddError($"{sourceInfo} nemá zadanou šablonu 'NestedTemplate'.");
                return false;
            }

            // Obsah nested panelu získám s pomocí dodaného loaderu :
            if (context.LoadArgs.InfoSource is null)
            {
                context.AddError($"{sourceInfo} má zadanou šablonu 'NestedTemplate', ale není dodána metoda Loader, která by načetla její obsah. Není možno načíst obsah šablony.");
                return false;
            }
            string nestedContent = context.LoadArgs.InfoSource.NestedTemplateContentLoad(nestedTemplate);
            if (String.IsNullOrEmpty(nestedContent))
            {   // Prázdný obsah: pokud to loader vrátí, pak OK, je to legální cesta, jak zrušit Nested obsah:
                context.AddError($"{sourceInfo} má zadanou šablonu 'NestedTemplate' = '{nestedTemplate}', ale její obsah nelze načíst.");
                return false;
            }

            // Ze stringu 'nestedContent' (obsahuje Nested šablonu) získám celou šablonu = nested DfForm:
            var nestedArgs = context.LoadArgs.CreateNestedArgs(null, nestedContent, null);
            dfNestedForm = _LoadDfForm(nestedArgs, false);
            return (dfNestedForm != null);
        }
        #endregion
        #region Načítání obsahu - private tvorba jednotlivých controlů

        // Každá zdejší větev / metoda načte pouze property deklarované přímo pro danou třídu, nikoli pro její předky!

        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající Item, včetně jeho obsahu.
        /// Může to být některý Control, anebo grupa (včetně NestedGroup). Nikoli Panel. Výstupem může být null.
        /// Pokud je výstupem null, pak informace o chybě je již zanesena do <paramref name="context"/>, kam se uvádí i zdroj = <paramref name="sourceInfo"/>.
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="sourceInfo">Informace o zdrojovém místě, do chybové informace. Typicky: "Formulář 'Jméno'" nebo "Stránka 'Name01'".</param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        /// <param name="validNames">Očekávaná validní jména elementů. Pokud je zadáno, a je detekován jiný než daný element, vrátí se null.</param>
        /// <returns></returns>
        private static DfBase _CreateChildItem(XElement xElement, string sourceInfo, DfContext context, params string[] validNames)
        {
            string elementName = _GetValidElementName(xElement);               // label, textbox, textbox_button, button, combobox, ...,   !page, panel, nestedpanel,
            if (String.IsNullOrEmpty(elementName)) return null;                // Nezadáno (?)
            // Pokud je dodán seznam validních jmen elementů (přinejmenším 1 prvek), ale aktuální element neodpovídá žádnému povolenému jménu, pak skončím:
            if (validNames != null && validNames.Length > 0 && !validNames.Any(v => String.Equals(v, elementName, StringComparison.OrdinalIgnoreCase)))
            {
                context.AddError($"{sourceInfo} obsahuje prvek '{elementName}', který není přípustný.");
                return null;
            }
            switch (elementName)
            {
                // Controly:
                case "placeholder":  return _FillControlPlaceHolder(xElement, new DfPlaceHolder(), context);
                case "hline": return _FillControlHLine(xElement, new DfHLine(), context);
                case "vline": return _FillControlVLine(xElement, new DfVLine(), context);
                case "label": return _FillControlLabel(xElement, new DfLabel(), context);
                case "title": return _FillControlTitle(xElement, new DfTitle(), context);
                case "checkbox": return _FillControlCheckBox(xElement, new DfCheckBox(), context);
                case "button": return _FillControlButton(xElement, new DfButton(), context);
                case "dropdownbutton": return _FillControlDropDownButton(xElement, new DfDropDownButton(), context);
                case "textbox": return _FillControlTextBox(xElement, new DfTextBox(), context);
                case "editbox": return _FillControlEditBox(xElement, new DfEditBox(), context);
                case "textboxbutton": return _FillControlTextBoxButton(xElement, new DfTextBoxButton(), context);
                case "combobox": return _FillControlComboBox(xElement, new DfComboBox(), context);
                case "image": return _FillControlImage(xElement, new DfImage(), context);
                case "stepprogress": return _FillControlStepProgress(xElement, new DfStepProgress(), context);

                // SubContainery = grupy:
                case "group": return _FillAreaGroup(xElement, null, context);
                case "nestedgroup": return _FillAreaNestedGroup(xElement, null, context);
            }
            context.AddError($"{sourceInfo} obsahuje prvek '{elementName}', který zde není očekáváván.");
            return null;
        }
        private static DfBaseControl _FillControlPlaceHolder(XElement xElement, DfPlaceHolder control, DfContext context)
        {
            _FillBaseAttributes(xElement, control, context);
            return control;
        }
        private static DfHLine _FillControlHLine(XElement xElement, DfHLine control, DfContext context)
        {
            _FillBaseAttributes(xElement, control, context);
            return control;
        }
        private static DfVLine _FillControlVLine(XElement xElement, DfVLine control, DfContext context)
        {
            _FillBaseAttributes(xElement, control, context);
            return control;
        }
        private static DfBaseControl _FillControlLabel(XElement xElement, DfLabel control, DfContext context)
        {
            _FillBaseAttributes(xElement, control, context);
            control.Text = _ReadAttributeString(xElement, "Text", null);
            control.Alignment = _ReadAttributeEnumN<ContentAlignmentType>(xElement, "Alignment");
            return control;
        }
        private static DfBaseControl _FillControlTitle(XElement xElement, DfTitle control, DfContext context)
        {
            _FillBaseAttributes(xElement, control, context);
            control.IconName = _ReadAttributeString(xElement, "IconName", null);
            control.Title = _ReadAttributeString(xElement, "Title", null);
            control.Style = _ReadAttributeEnumN<TitleStyleType>(xElement, "Style");
            control.Alignment = _ReadAttributeEnumN<ContentAlignmentType>(xElement, "Alignment");
            return control;
        }
        private static DfBaseControl _FillControlCheckBox(XElement xElement, DfCheckBox control, DfContext context)
        {
            _FillBaseAttributes(xElement, control, context);
            control.Style = _ReadAttributeEnumN<CheckBoxStyleType>(xElement, "Style");
            return control;
        }
        private static DfBaseControl _FillControlButton(XElement xElement, DfButton control, DfContext context)
        {
            _FillBaseAttributes(xElement, control, context);
            control.ActionType = _ReadAttributeEnumN<ButtonActionType>(xElement, "ActionType");
            control.ActionData = _ReadAttributeString(xElement, "ActionData", null);
            control.HotKey = _ReadAttributeString(xElement, "HotKey", null);
            control.IconPosition = _ReadAttributeEnumN<IconPositionType>(xElement, "IconPosition");
            return control;
        }
        private static DfBaseControl _FillControlDropDownButton(XElement xElement, DfDropDownButton control, DfContext context)
        {
            _FillControlButton(xElement, control, context);
            control.DropDownButtons = _LoadSubButtons(xElement, "dropDownButton", context);
            return control;
        }
        private static DfBaseControl _FillControlTextBox(XElement xElement, DfTextBox control, DfContext context)
        {
            _FillBaseAttributes(xElement, control, context);
            control.EditMask = _ReadAttributeString(xElement, "EditMask", null);
            control.Alignment = _ReadAttributeEnumN<ContentAlignmentType>(xElement, "Alignment");
            return control;
        }
        private static DfBaseControl _FillControlEditBox(XElement xElement, DfEditBox control, DfContext context)
        {
            _FillBaseAttributes(xElement, control, context);
            return control;
        }
        private static DfBaseControl _FillControlTextBoxButton(XElement xElement, DfTextBoxButton control, DfContext context)
        {
            _FillControlTextBox(xElement, control, context);
            control.ButtonsVisibility = _ReadAttributeEnumN<ButtonsVisibilityType>(xElement, "ButtonsVisibility");
            control.LeftButtons = _LoadSubButtons(xElement, "leftButton", context);
            control.RightButtons = _LoadSubButtons(xElement, "rightButton", context);
            return control;
        }
        private static DfBaseControl _FillControlComboBox(XElement xElement, DfComboBox control, DfContext context)
        {
            _FillBaseAttributes(xElement, control, context);
            control.EditStyleName = _ReadAttributeString(xElement, "EditStyleName", null);
            control.Style = _ReadAttributeEnumN<ComboBoxStyleType>(xElement, "Style");
            control.ComboItems = _LoadSubTextItems(xElement, "comboItem", context);
            return control;
        }
        private static DfBaseControl _FillControlImage(XElement xElement, DfImage control, DfContext context)
        {
            _FillBaseAttributes(xElement, control, context);
            control.ImageName = _ReadAttributeString(xElement, "ImageName", null);
            control.ImageData = _ReadAttributeBytes(xElement, "ImageData", null);
            return control;
        }
        private static DfBaseControl _FillControlStepProgress(XElement xElement, DfStepProgress control, DfContext context)
        {
            _FillBaseAttributes(xElement, control, context);
            control.EditStyleName = _ReadAttributeString(xElement, "EditStyleName", null);

            return control;
        }
        private static List<DfSubButton> _LoadSubButtons(XElement xElement, string loadElementName, DfContext context)
        {
            List<DfSubButton> subButtons = null;

            var xItems = xElement.Elements();
            if (xItems != null)
            {
                foreach (var xItem in xItems)
                {
                    string elementName = xItem?.Name.LocalName;
                    if (String.Equals(elementName, loadElementName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var subButton = _FillControlSubButton(xItem, new DfSubButton(), context);
                        if (subButton != null)
                        {
                            if (subButtons is null)
                                subButtons = new List<DfSubButton>();
                            subButtons.Add(subButton);
                        }
                    }
                }
            }

            return subButtons;
        }
        private static List<DfSubTextItem> _LoadSubTextItems(XElement xElement, string loadElementName, DfContext context)
        {
            List<DfSubTextItem> subTextItems = null;

            var xItems = xElement.Elements();
            if (xItems != null)
            {
                foreach (var xItem in xItems)
                {
                    string elementName = xItem?.Name.LocalName;
                    if (String.Equals(elementName, loadElementName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var subTextItem = _FillControlSubTextItem(xItem, new DfSubTextItem(), context);
                        if (subTextItem != null)
                        {
                            if (subTextItems is null)
                                subTextItems = new List<DfSubTextItem>();
                            subTextItems.Add(subTextItem);
                        }
                    }
                }
            }

            return subTextItems;
        }
        private static DfSubButton _FillControlSubButton(XElement xElement, DfSubButton control, DfContext context)
        {
            _FillControlSubTextItem(xElement, control, context);
            control.ActionType = _ReadAttributeEnumN<SubButtonActionType>(xElement, "ActionType");
            control.ActionData = _ReadAttributeString(xElement, "ActionData", null);
            return control;
        }
        private static DfSubTextItem _FillControlSubTextItem(XElement xElement, DfSubTextItem control, DfContext context)
        {
            _FillBaseAttributes(xElement, control, context);
            control.Text = _ReadAttributeString(xElement, "Text", null);
            control.IconName = _ReadAttributeString(xElement, "IconName", null);
            return control;
        }
        #endregion
        #region Načítání a konverze starších verzí formátu Infragistic
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <see cref="DfForm"/>.
        /// Specifická větev pro data verze 1-3 = IG = Infragistic
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="dfForm"></param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        /// <returns></returns>
        private static DfForm _FillAreaDfFormIG(XElement xElement, DfForm dfForm, DfContext context)
        {
            // Atributy deklarované pro DataForm starší verze:
            dfForm.TotalWidth = _ReadAttributeInt32N(xElement, "TotalWidth");
            dfForm.TotalHeight = _ReadAttributeInt32N(xElement, "TotalHeight");
            dfForm.MasterWidth = _ReadAttributeInt32N(xElement, "MasterWidth");
            dfForm.MasterHeight = _ReadAttributeInt32N(xElement, "MasterHeight");
            dfForm.AutoLabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "LabelPos", _ConvertIGLabelPos);
            dfForm.ColumnsCount = _ReadAttributeInt32N(xElement, "TableColumns");
            dfForm.AddUda = _ReadAttributeBoolN(xElement, "AddUda");
            dfForm.UdaLabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "AutoUdaLablePos", _ConvertIGLabelPos);                 // textový překlep ve jménu atributu 'AutoUdaLablePos' je přítomen v DataForm.Frm.xsd
            dfForm.DataSource = _ReadAttributeString(xElement, "DataSource", null);
            dfForm.Messages = _ReadAttributeString(xElement, "Messages", null);
            dfForm.UseNorisClass = _ReadAttributeInt32N(xElement, "UseNorisClass");
            dfForm.ContextMenu = _ReadAttributeBoolN(xElement, "ContextMenu");

            // Načítání starší verze běží tak, že se načte <tab>, rozpozná se, co to je (Page / Panel / Group / Line) a následně se obsah tabu přidá do odpovídající struktury.
            // Obsah jednoho <tab>u je vždy vložen do samostatné grupy.
            // Panel má vždy jeden column, v tomto columnu je jedna celá grupa uložena jako jedna buňka.
            // Grupa tedy má strukturu (počet sloupců) definovaný pro její <tab>
            DfPage currentPage = null;
            DfPanel currentPanel = null;

            // Elementy = jednotlivé Taby, budeme je teprve klasifikovat:
            var xTabs = xElement.Elements();
            if (xTabs != null)
            {
                string sourceInfo = $"Formulář '{dfForm.Name}'";
                foreach (var xTab in xTabs)
                {
                    string tabName = _ReadAttributeString(xTab, "Name", null);
                    var tabType = getTabType(xTab, out var tabTitle, out var nestedTemplate);      // Co daný <tab> reprezentuje:   Záložku?   Titulkový panel?   Beztitulkovou grupu?   Vodorovnou linku?   Nic?
                    if (tabType == FormTabIGType.None) continue;

                    DfPanel targetPanel = getTargetPanel(tabType, tabName, tabTitle);              // Připrav mi panel, do kterého přidáme nový prvek (buď grupu pro prvky, anebo jen HLine)
                    if (targetPanel is null) continue;

                    switch (tabType)
                    {
                        case FormTabIGType.NestedTemplate:
                            _CreateNestedTemplateFromTabIG(xTab, nestedTemplate, dfForm, ref currentPage, ref currentPanel);
                            break;
                        case FormTabIGType.Page:
                        case FormTabIGType.Panel:
                        case FormTabIGType.Group:
                            var group = _CreateGroupFromTabIG(xTab, sourceInfo, context);
                            if (group != null) targetPanel.Childs.Add(group);
                            break;
                        case FormTabIGType.HLine:
                            targetPanel.Childs.Add(new DfHLine() { DesignBounds = new DesignBounds() { Height = 6 } });
                            break;
                    }
                }
            }
            return dfForm;

            // Určí, jaký druh Tabu je v XElementu (Nested / Page / Panel / Grupa / Line):
            FormTabIGType getTabType(XElement xTab, out string tabTitle, out string nestedTemplate)
            {
                tabTitle = null;
                nestedTemplate = null;

                // Pokud to není element <tab>, pak nebudu načítat nic:
                string elementName = _GetValidElementName(xTab);                              // tab nebo něco jiného?
                if (elementName != "tab") return FormTabIGType.None;

                // Načteme pár identifikačních informací, abychom poznali, jaký druh TABu máme zpracovat:
                nestedTemplate = _ReadAttributeString(xTab, "NestedTemplate", null);          // Název Nested šablony
                if (!String.IsNullOrEmpty(nestedTemplate)) return FormTabIGType.NestedTemplate;

                bool isEmptyTab = !xTab.HasElements && !xTab.HasAttributes;                   // Jde o element   <tab/>   tedy vodorovná oddělovací linka, nejspíš za nějakým datovým Tabem
                if (isEmptyTab) return FormTabIGType.HLine;                                     // tab bez dalšího obsahu = čára

                string tabPageLabel = _ReadAttributeString(xTab, "TabPageLabel", null);       // Titulek pro DfPage anebo DfPanel
                if (!String.IsNullOrEmpty(tabPageLabel))
                {
                    tabTitle = tabPageLabel;
                    string renderAs = _ReadAttributeString(xTab, "RenderAs", null);           // Pokud je tady "DesignTabWithLabel", pak je to DfPanel (máme titulek). Hodnoty:    "TabPage" / "DesignTab" / "DesignTabWithLabel"
                    if (!String.IsNullOrEmpty(renderAs))
                    {
                        string key = renderAs.Trim().ToLower();
                        switch (key)
                        {
                            case "tabpage": return FormTabIGType.Page;                          // Začátek nové záložky = Page
                            case "designtab": return FormTabIGType.Group;                       // "DesignTab" => grupa bez titulku - stejně jako bez tohoto atributu
                            case "designtabwithlabel": return FormTabIGType.Panel;              // "DesignTabWithLabel" => Panel (má titulek)
                        }
                    }
                    return FormTabIGType.Page;                                                  // Bez zadání RenderAs a se zadaným TabPageLabel => Začátek nové záložky = Page
                }

                return FormTabIGType.Group;                                                     // tab nikoli Nested, s obsahem, ale bez titulku = grupa
            }
            // Najde / vytvoří a vrátí panel, do kterého bude přidána grupa pro nový Tab
            DfPanel getTargetPanel(FormTabIGType type, string tabName, string tabTitle)
            {
                switch (type)
                {
                    case FormTabIGType.Page:
                        currentPage = createPage(tabName, tabTitle);
                        currentPanel = createPanel(tabName, null, TitleStyleType.NoTitle);
                        return currentPanel;
                    case FormTabIGType.Panel:
                        if (currentPage is null) currentPage = createPage(null, tabTitle);
                        currentPanel = createPanel(tabName, tabTitle, TitleStyleType.TextOnly);
                        return currentPanel;
                    case FormTabIGType.Group:
                    case FormTabIGType.HLine:
                        if (currentPage is null) currentPage = createPage(null, tabTitle);
                        if (currentPanel is null) currentPanel = createPanel(null, null, TitleStyleType.NoTitle);
                        return currentPanel;
                }
                return null;
            }
            // Vytvoří new instanci DfPage pro danou definici, přidá ji jako další stránku do dfForm.Pages a vrátí ji.
            DfPage createPage(string pageName, string pageTitle)
            {
                if (String.IsNullOrEmpty(pageName)) pageName = $"AutoPageName{context.AutoPageNumber}";

                var page = new DfPage
                {
                    Name = pageName,
                    Title = pageTitle,
                    Panels = new List<DfPanel>()
                };

                if (dfForm.Pages is null) dfForm.Pages = new List<DfPage>();
                dfForm.Pages.Add(page);

                return page;
            }
            // Vytvoří new instanci DfPanel pro danou definici, přidá ji jako další prvek do currentPage.Panels a vrátí jej.
            DfPanel createPanel(string panelName, string panelTitle, TitleStyleType titleStyle)
            {
                if (String.IsNullOrEmpty(panelName)) panelName = $"AutoPanelName{context.AutoPanelNumber}";

                var panel = new DfPanel()
                {
                    Name = panelName,
                    Title = panelTitle,
                    TitleStyle = titleStyle,
                    ColumnsCount = 1,
                    Childs = new List<DfBase>()
                };

                if (currentPage is null) currentPage = createPage(null, panelTitle);
                currentPage.Panels.Add(panel);

                return panel;
            }
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> formátu V1-V3 (Infragistic) načte a vrátí odpovídající kontejner (tab), včetně jeho obsahu a child prvků.
        /// Tato metoda není volaná pro NestedTab, tu řeší <see cref="_CreateNestedTemplateFromTabIG(XElement, string, DfForm, ref DfPage, ref DfPanel)"/>.
        /// Výstupem je tedy <see cref="DfGroup"/> (nebo null).
        /// Pokud je výstupem null, pak informace o chybě je již zanesena do <paramref name="context"/>, kam se uvádí i zdroj = <paramref name="sourceInfo"/>.
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="sourceInfo">Informace o zdrojovém místě, do chybové informace. Typicky: "Formulář 'Jméno'" nebo "Stránka 'Name01'".</param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        private static DfGroup _CreateGroupFromTabIG(XElement xElement, string sourceInfo, DfContext context)
        {
            string groupName = _ReadAttributeString(xElement, "Name", null);
            if (String.IsNullOrEmpty(groupName)) groupName = $"AutoGroupName{context.AutoGroupNumber}";

            var dfGroup = new DfGroup
            {
                Name = groupName,
                Invisible = _ReadAttributeString(xElement, "Invisible", null),
                ColumnsCount = _ReadAttributeInt32N(xElement, "TableColumns"),
                AutoLabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "LabelPos", _ConvertIGLabelPos),
                HPosition = _ReadAttributeEnumN<HPositionType>(xElement, "Align", _ConvertIGAlign),
                IsUDA = _ReadAttributeBoolN(xElement, "UDA")
            };

            var width = _ReadAttributeInt32PN(xElement, "Width");
            if (width.HasValue)
                dfGroup.DesignBounds = new DesignBounds() { Width = width };


            // ... a možná i další atributy z tabu do grupy zde?


            // Specifika:
            //  V novém formuláři je dědičnost některých hodnot Form => Page => Panel => Group;
            //  Ve starém formuláři je dědičnost krátká: Form => Group (protože obsahuje pouze Template a pod tím Tab):
            if (!dfGroup.ColumnsCount.HasValue) dfGroup.ColumnsCount = context.Form.ColumnsCount;

            // childs v <tab> jsou výhradně <columns>
            var xChilds = xElement.Elements();
            if (xChilds != null)
            {
                sourceInfo += $"; Tab '{dfGroup.Name}'";
                foreach (var xChild in xChilds)
                {
                    var child = _CreateChildItemFromColumnIG(xChild, "column", sourceInfo, context);
                    if (child != null && (child is DfBaseControl || child is DfGroup))
                    {   // Pouze Controly + Group
                        if (dfGroup.Childs is null) dfGroup.Childs = new List<DfBase>();
                        dfGroup.Childs.Add(child);
                    }
                }
            }

            return dfGroup;


        }
        /// <summary>
        /// Vytvoří a vrátí DfControl podle dat v dodaném elementu typu 'column' formátu V1-V3 (Infragistic) 
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="elementName">Jméno očekávaného elementu, ostatní nebudu načítat</param>
        /// <param name="sourceInfo"></param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        private static DfBase _CreateChildItemFromColumnIG(XElement xElement, string elementName, string sourceInfo, DfContext context)
        {
            string xName = _GetValidElementName(xElement);
            if (!String.Equals(xName, elementName, StringComparison.OrdinalIgnoreCase)) return null;

            var column = new DfColumn();

            // Načtu obecně platné atributy, deklarované v DataForm.Frm.xsd :
            column.Name = _ReadAttributeString(xElement, "Name", null);
            column.TabIndex = _ReadAttributeInt32N(xElement, "TabIndex");
            column.EditMask = _ReadAttributeString(xElement, "EditMask", null);
            column.InputWindow = _ReadAttributeString(xElement, "InputWindow", null);
            column.CurrencyDisplay = _ReadAttributeBoolN(xElement, "CurrencyDisplay");
            column.MaxLength = _ReadAttributeInt32N(xElement, "MaxLength");
            column.Label = _ReadAttributeString(xElement, "Label", null);
            column.LabelToolTip = _ReadAttributeString(xElement, "LabelToolTip", null);
            column.LabelToolTipHide = _ReadAttributeString(xElement, "LabelToolTipHide", null);
            column.Width = _ReadAttributeInt32PN(xElement, "Width");
            column.Height = _ReadAttributeInt32PN(xElement, "Height");
            column.Required = _ReadAttributeBoolN(xElement, "Required");
            column.Invisible = _ReadAttributeString(xElement, "Invisible", null);
            column.ReadOnly = _ReadAttributeBoolN(xElement, "ReadOnly");
            column.InputType = _ReadAttributeString(xElement, "InputType", null);                               // text / checkbox / radiobutton / string / dynamic / date / time / datetime / textarea / select / password / number / label / group / button / picturelistbox / file / calendar / picture / htmltext / AidcCode / color / Geography / PercentageBar / calculator / Placeholder
            column.SyntaxHighlightingType = _ReadAttributeString(xElement, "SyntaxHighlightingType", null);     // Sql / Xml
            column.AidcCodeType = _ReadAttributeString(xElement, "AidcCodeType", null);                         // Codabar / Code11 / DataMatrix / EAN13 / EAN8 / ....
            column.AidcCodeSettings = _ReadAttributeString(xElement, "AidcCodeSettings", null);
            column.PercentageBarSettings = _ReadAttributeString(xElement, "PercentageBarSettings", null);
            column.ButtonAction = _ReadAttributeString(xElement, "ButtonAction", null);                         // Click / Update / Close / RunFunction / ClickCheckRequired
            column.ButtonFunction = _ReadAttributeString(xElement, "ButtonFunction", null);
            column.ButtonFunctionLabelType = _ReadAttributeString(xElement, "ButtonFunctionLabelType", null);   // Text / Icon / IconText
            column.Values = _ReadAttributeString(xElement, "Values", null);
            column.Expr = _ReadAttributeString(xElement, "Expr", null);
            column.ExprType = _ReadAttributeString(xElement, "ExprType", null);                                 // String / Int32 / DateTime / Decimal
            column.EditStyle = _ReadAttributeString(xElement, "EditStyle", null);
            column.EditStyleViewMode = _ReadAttributeString(xElement, "EditStyleViewMode", null);               // Text / Icon / IconText
            column.Protect = _ReadAttributeString(xElement, "Protect", null);
            column.HtmlEdit = _ReadAttributeBoolN(xElement, "HtmlEdit");
            column.Relation = _ReadAttributeBoolN(xElement, "Relation");                                        // 0 / 1
            column.HtmlStyle = _ReadAttributeString(xElement, "HtmlStyle", null);
            column.ColSpan = _ReadAttributeInt32N(xElement, "ColSpan");
            column.RowSpan = _ReadAttributeInt32N(xElement, "RowSpan");
            column.LabelPos = _ReadAttributeEnumN<LabelPositionType>(xElement, "LabelPos", _ConvertIGLabelPos);                // type 'pos'         : Left / Up / None / Right
            column.Align = _ReadAttributeEnumN<ContentAlignmentType>(xElement, "Align", _ConvertIGLabelLeftRight);             // type 'leftright'   : Left / Right
            column.AlignValue = _ReadAttributeEnumN<ContentAlignmentType>(xElement, "AlignValue", _ConvertIGLabelLeftRight);   // type 'leftright'   : Left / Right
            column.RegisterItemChange = _ReadAttributeBoolN(xElement, "RegisterItemChange");
            column.RegisterDblClick = _ReadAttributeBoolN(xElement, "RegisterDblClick");
            column.BoxCols = _ReadAttributeString(xElement, "BoxCols", null);
            column.BoxRows = _ReadAttributeString(xElement, "BoxRows", null);
            column.BoxStyle = _ReadAttributeString(xElement, "BoxStyle", null);
            column.AttrShortName = _ReadAttributeBoolN(xElement, "AttrShortName");
            column.RelationParams = _ReadAttributeString(xElement, "RelationParams", null);
            column.RelationAddName = _ReadAttributeBoolN(xElement, "RelationAddName");
            column.RenderAs = _ReadAttributeString(xElement, "RenderAs", null);                                 // enum, ale string = bez omezení...
            column.SetEmptyStringIsNull = _ReadAttributeBoolN(xElement, "SetEmptyStringIsNull");
            column.MaxDropDownItems = _ReadAttributeInt32N(xElement, "MaxDropDownItems");
            column.IsBreak = _ReadAttributeBoolN(xElement, "Break");
            column.IsDefault = _ReadAttributeBoolN(xElement, "Default");
            column.AllowUserChangeInvisibility = _ReadAttributeString(xElement, "AllowUserChangeInvisibility", null);
            column.OneMonthOnly = _ReadAttributeBoolN(xElement, "OneMonthOnly");
            column.NoBorder = _ReadAttributeBoolN(xElement, "NoBorder");
            column.RadioTextAlign = _ReadAttributeString(xElement, "RadioTextAlign", null);
            column.TextAreaOverflow = _ReadAttributeString(xElement, "TextAreaOverflow", null);
            column.FontAndColor = _ReadAttributeString(xElement, "FontAndColor", null);
            column.AcceptPromptFormatMask = _ReadAttributeString(xElement, "AcceptPromptFormatMask", null);
            column.Image = _ReadAttributeString(xElement, "Image", null);
            column.ToolTip = _ReadAttributeString(xElement, "ToolTip", null);
            column.LinkType = _ReadAttributeString(xElement, "LinkType", null);                                 // email / phone / url
            column.AllowExtendedEditor = _ReadAttributeBoolN(xElement, "AllowExtendedEditor");
            column.SuppressReadOnlyFromDataForm = _ReadAttributeBoolN(xElement, "SuppressReadOnlyFromDataForm");
            column.DDLBEditor = _ReadAttributeString(xElement, "DDLBEditor", null);                             // Combobox / Breadcrumb
            column.ExtendedAttributes = _ReadAttributeString(xElement, "ExtendedAttributes", null);
            column.FileFilter = _ReadAttributeString(xElement, "FileFilter", null);

            // Typ controlu:
            DataControlType? controlType = _ConvertIGInputType(column);
            if (!controlType.HasValue)
                controlType = context.InfoSource.GetControlType(column, context.Form.UseNorisClass);
            if (!controlType.HasValue)
                controlType = DataControlType.TextBox;

            DfBaseControl dfBaseControl = null;
            DfBaseContainer dfBaseContainer = null;
            switch (controlType.Value)
            {   // text / checkbox / radiobutton / string / dynamic / date / time / datetime / textarea / select / password / number / label / 
                // group / button / picturelistbox / file / calendar / picture / htmltext / AidcCode / color / Geography / PercentageBar / calculator / Placeholder
                case DataControlType.Label:
                    var dfLabel = new DfLabel()
                    {
                        Text = column.Label,
                    };
                    dfBaseControl = dfLabel;
                    break;

                case DataControlType.PlaceHolder:
                    var dfPlaceholder = new DfPlaceHolder();
                    dfBaseControl = dfPlaceholder;
                    break;

                case DataControlType.Button:
                    var dfButton = new DfButton()
                    {
                        Text = column.Label,
                        IconName = column.Image,
                        ActionData = column.ButtonFunction
                    };
                    dfBaseControl = dfButton;
                    break;
                case DataControlType.ComboBox:
                case DataControlType.BreadCrumb:
                    var dfComboBox = new DfComboBox()
                    {
                        TabIndex = column.TabIndex,
                    };
                    dfBaseControl = dfComboBox;
                    break;
                case DataControlType.Group:
                    var dfGroup = new DfGroup()
                    {
                        Name = column.Name,
                        RowSpan = column.RowSpan,
                        ColSpan = column.ColSpan,
                        DesignBounds = createDesignBounds(column.Width, column.Height)
                    };
                    loadChildGroupColumns(dfGroup);
                    dfBaseContainer = dfGroup;
                    break;
                case DataControlType.TextBoxButton:
                    var dfTextBoxButton = new DfTextBoxButton()
                    {
                        Label = column.Label,
                        LabelPosition = column.LabelPos,
                        TabIndex = column.TabIndex,
                    };
                    dfBaseControl = dfTextBoxButton;
                    break;
                case DataControlType.TextBox:
                default:
                    // Pokrývá více InputType:   string / date / time / datetime / password / number 
                    var dfTextBox = new DfTextBox()
                    {
                        Label = column.Label,
                        LabelPosition = column.LabelPos,
                        TabIndex = column.TabIndex,
                    };
                    // Podle definice 'inputType' doplníme další vlastnosti, protože prvkem typu ControlType.TextBox se řeší vícero hodnot 'InputType':
                    dfBaseControl = dfTextBox;
                    break;
            }

            if (dfBaseControl != null)
            {
                dfBaseControl.ToolTipText = column.ToolTip;
                dfBaseControl.Name = column.Name;
                dfBaseControl.RowSpan = column.RowSpan;
                dfBaseControl.ColSpan = column.ColSpan;
                dfBaseControl.DesignBounds = createDesignBounds(column.Width, column.Height);
                dfBaseControl.Invisible = column.Invisible;
                dfBaseControl.State = (String.IsNullOrEmpty(column.Invisible) ? ControlStateType.Default : ControlStateType.Absent);
                return dfBaseControl;
            }

            if (dfBaseContainer != null)
            {
                return dfBaseContainer;
            }

            return null;


            // Vytvoří a vrátí souřadnice 'DesignBounds' pro danou šířku a výšku
            DesignBounds createDesignBounds(Int32P? width, Int32P? height)
            {
                if (width.HasValue || height.HasValue)
                    return new DesignBounds() { Width = width, Height = height };
                return null;
            }
            // Do dané grupy načte 'groupcolumn' z aktuálního elementu 'xElement'
            void loadChildGroupColumns(DfGroup dfGroup)
            {
                string groupInfo = sourceInfo + $"; Group '{dfGroup.Name}'";
                var xChilds = xElement.Elements();
                foreach (var xChild in xChilds)
                {
                    var child = _CreateChildItemFromColumnIG(xChild, "groupcolumn", sourceInfo, context);
                    if (child != null && (child is DfBaseControl || child is DfGroup))
                    {   // Pouze Controly + Group
                        if (dfGroup.Childs is null) dfGroup.Childs = new List<DfBase>();
                        dfGroup.Childs.Add(child);
                    }
                }
            }
        }
        /// <summary>
        /// Vytvoří NestedPanel pro IG verzi
        /// </summary>
        /// <param name="xTab"></param>
        /// <param name="nestedTemplate"></param>
        /// <param name="dfForm"></param>
        /// <param name="currentPage"></param>
        /// <param name="currentPanel"></param>
        private static void _CreateNestedTemplateFromTabIG(XElement xTab, string nestedTemplate, DfForm dfForm, ref DfPage currentPage, ref DfPanel currentPanel)
        {
#warning TODO : načítat NestedTemplate z FormatVersion 1-3 !!!
            // throw new NotImplementedException("FormatVersion1 and NestedTemplate: conversion not implemented.");
        }
        /// <summary>
        /// Konvertuje text zadaný jako Value pro atribut LabelPos (type 'pos') ve verzi IG, do hodnoty <see cref="LabelPositionType"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static LabelPositionType? _ConvertIGLabelPos(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                string key = value.Trim().ToLower();
                switch (key)
                {
                    case "none": return LabelPositionType.None;
                    case "left": return LabelPositionType.BeforeLeft;
                    case "up": return LabelPositionType.Top;
                    case "right": return LabelPositionType.After;
                }
            }
            return null;
        }
        /// <summary>
        /// Konvertuje text zadaný jako Value pro atribut Align (type 'leftright') ve verzi IG, do hodnoty <see cref="ContentAlignmentType"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static ContentAlignmentType? _ConvertIGLabelLeftRight(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                string key = value.Trim().ToLower();
                switch (key)
                {
                    case "left": return ContentAlignmentType.MiddleLeft;
                    case "right": return ContentAlignmentType.MiddleRight;
                }
            }
            return null;
        }
        /// <summary>
        /// Konvertuje text zadaný jako Value pro atribut Align (type 'leftrightcenter') ve verzi IG, do hodnoty <see cref="HPositionType"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static HPositionType? _ConvertIGAlign(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                string key = value.Trim().ToLower();
                switch (key)
                {
                    case "left": return HPositionType.Left;
                    case "center": return HPositionType.Center;
                    case "right": return HPositionType.Right;
                }
            }
            return null;
        }
        /// <summary>
        /// Konvertuje typ controlu z dodaného sloupce, načteného z Infragistic šablony z atributu "InputType", do hodnoty typu <see cref="DataControlType"/>.
        /// Více různých "InputType" se konvertoje do shodné hodnoty <see cref="DataControlType"/>, protože jsou reprezentovány stejným controlem.
        /// Odlišnost je pak dána např. v odlišném nastavení toho controlu.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private static DataControlType? _ConvertIGInputType(DfColumn column)
        {
            if (!String.IsNullOrEmpty(column.InputType))
            {
                string key = column.InputType.Trim().ToLower();
                switch (key)
                {   // text / checkbox / radiobutton / string / dynamic / date / time / datetime / textarea / select / password / number / label / group / button / picturelistbox / file / 
                    // calendar / picture / htmltext / AidcCode / color / Geography / PercentageBar / calculator / Placeholder
                    case "text": return DataControlType.TextBox;
                    case "checkbox": return DataControlType.CheckBox;
#warning RadioButton
                    case "radiobutton": return DataControlType.Button;               
                    case "string": return DataControlType.TextBox;
#warning Dynamic
                    case "dynamic": return DataControlType.TextBox;
                    case "date": return DataControlType.TextBox;
                    case "time": return DataControlType.TextBox;
                    case "datetime": return DataControlType.TextBox;
                    case "textarea": return DataControlType.EditBox;
                    case "select": return DataControlType.ComboBox;
                    case "password": return DataControlType.TextBox;
                    case "number": return DataControlType.TextBox;
                    case "label": return DataControlType.Label;
                    case "group": return DataControlType.Group;
                    case "button": return DataControlType.Button;
                    case "picturelistbox": return DataControlType.Label;
                    case "file": return DataControlType.TextBoxButton;
                    case "calendar": return DataControlType.Label;
                    case "picture": return DataControlType.Image;
                    case "htmltext": return DataControlType.EditBox;
                    case "aidccode": return DataControlType.Label;
                    case "color": return DataControlType.Label;
                    case "geography": return DataControlType.Label;
                    case "percentagebar": return DataControlType.Label;
                    case "calculator": return DataControlType.TextBox;
                    case "placeholder": return DataControlType.PlaceHolder;
                }
            }
            return null;
        }
        /// <summary>
        /// Druh Tabu = jeho konverze na prvek Df
        /// </summary>
        private enum FormTabIGType
        {
            None,
            /// <summary>
            /// Tab reprezentuje Nested template
            /// </summary>
            NestedTemplate,
            /// <summary>
            /// Tab reprezentuje záložku = DfPage
            /// </summary>
            Page,
            /// <summary>
            /// Tab reprezentuje blok s titulkem = DfPanel
            /// </summary>
            Panel,
            /// <summary>
            /// Tab reprezentuje blok bez titulku = DfGroup
            /// </summary>
            Group,
            /// <summary>
            /// Tab je prázdný = reprezentuje vodorovnou čáru = HLine
            /// </summary>
            HLine
        }
        #endregion
        #region Načítání XML atributů různého datového typu, včetně konverze (enumy, primitivní typy)
        /// <summary>
        /// Z dodaného <paramref name="xElement"/> načte hodnoty odpovídající cílovému typu,
        /// a vloží je do dodaného controlu <paramref name="target"/>.
        /// <para/>
        /// Načítá hodnoty odpovídající třídám: <see cref="DfBase"/>, <see cref="DfBaseControl"/>, <see cref="DfBaseInputControl"/>,
        /// <see cref="DfBaseInputTextControl"/>, <see cref="DfBaseLabeledInputControl"/>, <see cref="DfSubTextItem"/>, <see cref="DfSubButton"/>.<br/>
        /// Dále načítá hodnoty pro třídy containerů: <see cref="DfBaseArea"/>, <see cref="DfBaseContainer"/>.
        /// </summary>
        /// <param name="xElement">Element, z něhož se mají načítat atributy</param>
        /// <param name="target">Cílový prvek</param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        private static void _FillBaseAttributes(XElement xElement, DfBase target, DfContext context)
        {
            // Každá zdejší větev / metoda načte pouze property deklarované přímo pro danou třídu, nikoli pro její předky!
            string name = null;
            string columnName = null;

            // DfBase:
            name = _ReadAttributeString(xElement, "Name", null);
            target.Name = name;
            target.State = _ReadAttributeControlState(xElement, ControlStateType.Default);
            target.Invisible = _ReadAttributeString(xElement, "Invisible", null);
            // Atributy 'ToolTipTitle' a 'ToolTipText' načtu později, až budou k dispozici hodnoty 'name' (už máme) a 'columnName' (jen pro DfBaseInputControl) ...

            // Potomci směrem k Controlům:
            if (target is DfBaseControl control)
            {
                control.ControlStyle = _ReadAttributeStyle(xElement, null);
                control.DesignBounds = _ReadAttributeBounds(xElement, null);
                control.ParentBoundsName = _ReadAttributeString(xElement, "ParentBoundsName", null);
                control.Break = _ReadAttributeBoolNX(xElement, "Break", null, true);
                control.ColIndex = _ReadAttributeInt32N(xElement, "ColIndex");
                control.ColSpan = _ReadAttributeInt32N(xElement, "ColSpan");
                control.RowSpan = _ReadAttributeInt32N(xElement, "RowSpan");
                control.HPosition = _ReadAttributeEnumN<HPositionType>(xElement, "HPosition");
                control.VPosition = _ReadAttributeEnumN<VPositionType>(xElement, "VPosition");
                control.ExpandControl = _ReadAttributeEnumN<ExpandControlType>(xElement, "ExpandControl"); 
            }
            if (target is DfLine line)
            {
                line.LineWidth = _ReadAttributeInt32N(xElement, "LineWidth");
                line.LineColorLight = _ReadAttributeColorN(xElement, "LineColorLight");
                line.LineColorDark = _ReadAttributeColorN(xElement, "LineColorDark");
            }
            if (target is DfBaseInputControl inputControl)
            {
                columnName = _ReadAttributeString(xElement, "ColumnName", null);
                inputControl.ColumnName = columnName;
                inputControl.Required = _ReadAttributeEnumN<RequiredType>(xElement, "Required");
                inputControl.TabIndex = _ReadAttributeInt32N(xElement, "TabIndex");
            }

            // Nyní mám k dispozici hodnoty 'name' a 'columnName'; nyní mohu načítat texty přeložené pro konkrétní prvek:
            target.ToolTipTitle = _ReadAttributeUserText(xElement, "ToolTipTitle", name, columnName, context);
            target.ToolTipText = _ReadAttributeUserText(xElement, "ToolTipText", name, columnName, context);


            if (target is DfBaseInputTextControl textControl)
            {
                textControl.Text = _ReadAttributeUserText(xElement, "Text", name, columnName, context);
                textControl.IconName = _ReadAttributeString(xElement, "IconName", null);
                textControl.Alignment = _ReadAttributeEnumN<ContentAlignmentType>(xElement, "Alignment");
            }
            if (target is DfBaseLabeledInputControl labeledControl)
            {
                labeledControl.Label = _ReadAttributeUserText(xElement, "Label", name, columnName, context);
                labeledControl.LabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "LabelPosition", _FixLabelPosition);
                labeledControl.LabelWidth = _ReadAttributeInt32N(xElement, "LabelWidth");
                labeledControl.SuffixLabel = _ReadAttributeUserText(xElement, "SuffixLabel", name, columnName, context);
            }
            if (target is DfSubTextItem subTextItem)
            {
                subTextItem.Text = _ReadAttributeUserText(xElement, "Text", name, columnName, context);
                subTextItem.IconName = _ReadAttributeString(xElement, "IconName", null);
            }
            if (target is DfSubButton subButton)
            {
                subButton.ActionType = _ReadAttributeEnumN<SubButtonActionType>(xElement, "ActionType");
                subButton.ActionData = _ReadAttributeString(xElement, "ActionData", null);
            }

            // Potomci směrem k Containerům:
            if (target is DfBaseArea area)
            {
                area.BackColorName = _ReadAttributeString(xElement, "BackColorName", null);
                area.BackColorLight = _ReadAttributeColorN(xElement, "BackColorLight");
                area.BackColorDark = _ReadAttributeColorN(xElement, "BackColorDark");
                area.BackImageName = _ReadAttributeString(xElement, "BackImageName", null);
                area.BackImagePosition = _ReadAttributeEnumN<BackImagePositionType>(xElement, "BackImagePosition");
                area.FlowAreaBegin = _ReadAttributesLocation(xElement, "FlowAreaBegin", null);
                area.ColumnsCount = _ReadAttributeInt32N(xElement, "ColumnsCount");
                area.ColumnWidths = _ReadAttributeString(xElement, "ColumnWidths", null);
                area.AutoLabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "AutoLabelPosition", _FixLabelPosition);
                area.Margins = _ReadAttributesMargin(xElement, "Margins", null);
                area.ControlMargins = _ReadAttributesMargin(xElement, "ControlMargins", null);
                area.ColumnsDistance = _ReadAttributeInt32N(xElement, "ColumnsDistance");
                area.RowsDistance = _ReadAttributeInt32N(xElement, "RowsDistance");
                area.TopLabelOffsetX = _ReadAttributeInt32N(xElement, "TopLabelOffsetX");
                area.BottomLabelOffsetX = _ReadAttributeInt32N(xElement, "BottomLabelOffsetX");
                area.LabelsRelativeToControl = _ReadAttributeBoolNX(xElement, "LabelsRelativeToControl", null, true);
            }
            if (target is DfBaseContainer container)
            {
                container.DesignBounds = _ReadAttributeBounds(xElement, null);
                container.ParentBoundsName = _ReadAttributeString(xElement, "ParentBoundsName", null);
            }
        }
        /// <summary>
        /// Vrátí text načtený z daného atributu, přeložený do aktuálního jazyka
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="attributeName"></param>
        /// <param name="name"></param>
        /// <param name="columnName"></param>
        /// <param name="context">Průběžná data pro načítání obsahu</param>
        /// <returns></returns>
        private static string _ReadAttributeUserText(XElement xElement, string attributeName, string name, string columnName, DfContext context)
        {
            // Původně byl úmysl překládat texty ihned po načtení, tedy zde.
            // Ale z hlediska efektivity translátoru není vhodné jej volat po jednom textu = např. 5x pro jeden column (text + label + tooltipy ...).
            // Proto tady načtu jen to, co je v frm.xml, a další zpracování nechám na jindy.
            string text = _ReadAttributeString(xElement, attributeName, null);
            return text;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho String podobu
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        private static string _ReadAttributeString(XElement xElement, string attributeName, string defaultValue)
        {
            string value = defaultValue;
            if (xElement.HasAttributes && !String.IsNullOrEmpty(attributeName))
            {
                var xAttribute = xElement.Attribute(attributeName);
                if (xAttribute != null) value = xAttribute.Value;
            }
            return value;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho byte[] podobu konvertovanou ze stringu pomocí Base64
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        private static byte[] _ReadAttributeBytes(XElement xElement, string attributeName, byte[] defaultValue)
        {
            byte[] value = defaultValue;
            if (xElement.HasAttributes && !String.IsNullOrEmpty(attributeName))
            {
                var xAttribute = xElement.Attribute(attributeName);
                if (xAttribute != null)
                {
                    var text = xAttribute.Value;
                    if (!String.IsNullOrEmpty(text))
                    {
                        value = Convert.FromBase64String(text);
                    }
                }
            }
            return value;
        }
        /// <summary>
        /// Načte stav prvku <see cref="ControlStateType"/> z atributu 'State' anebo jej složí z atributů ''
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private static ControlStateType _ReadAttributeControlState(XElement xElement, ControlStateType defaultValue)
        {
            // Pokud je deklarován atribut 'State', pak načtu jeho hodnotu:
            var state = _ReadAttributeEnumN<ControlStateType>(xElement, "State");
            if (state.HasValue) return state.Value;

            // Nemáme 'State': vyhledám jednotlivé Boolean atributy a stav složím:
            bool isInvisible = _ReadAttributeBoolX(xElement, "Invisible");
            bool isReadOnly = _ReadAttributeBoolX(xElement, "ReadOnly");
            bool isDisabled = _ReadAttributeBoolX(xElement, "Disabled");
            bool isTabStop = _ReadAttributeBoolX(xElement, "TabStop", true, true);       // Pokud tam nebude tento atribut, pak se předpokládá, že TabStop je true. Pouze explicitní 'TabStop = "False"' mě vrátí false!

            // Složíme výsledek:
            state = ControlStateType.Enabled
                 | (isInvisible ? ControlStateType.Invisible : ControlStateType.Default)
                 | (isReadOnly ? ControlStateType.ReadOnly : ControlStateType.Default)
                 | (isDisabled ? ControlStateType.Disabled : ControlStateType.Default)
                 | (isTabStop ? ControlStateType.Default : ControlStateType.TabSkip);    // TabStop => Default, NonTabStop = TabSkip!
            return state.Value;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho Int32 podobu
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        private static int _ReadAttributeInt32(XElement xElement, string attributeName, int defaultValue)
        {
            int? value = _ReadAttributeInt32N(xElement, attributeName);
            return value ?? defaultValue;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho Int32 podobu
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        private static int? _ReadAttributeInt32N(XElement xElement, string attributeName)
        {
            int? value = null;
            if (xElement.HasAttributes && !String.IsNullOrEmpty(attributeName))
            {
                var xAttribute = xElement.Attribute(attributeName);
                if (xAttribute != null && !String.IsNullOrEmpty(xAttribute.Value) && Int32.TryParse(xAttribute.Value, out var result)) value = result;
            }
            return value;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho Int32PP podobu
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        private static Int32P? _ReadAttributeInt32PN(XElement xElement, string attributeName)
        {
            Int32P? value = null;
            if (xElement.HasAttributes && !String.IsNullOrEmpty(attributeName))
            {
                var xAttribute = xElement.Attribute(attributeName);
                if (xAttribute != null && !String.IsNullOrEmpty(xAttribute.Value) && Int32P.TryParse(xAttribute.Value, out var result)) value = result;
            }
            return value;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho Color? podobu
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        private static System.Drawing.Color _ReadAttributeColor(XElement xElement, string attributeName, System.Drawing.Color defaultValue)
        {
            System.Drawing.Color? value = _ReadAttributeColorN(xElement, attributeName);
            return value ?? defaultValue;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho Color? podobu
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        private static System.Drawing.Color? _ReadAttributeColorN(XElement xElement, string attributeName)
        {
            System.Drawing.Color? value = null;
            if (xElement.HasAttributes && !String.IsNullOrEmpty(attributeName))
            {
                var xAttribute = xElement.Attribute(attributeName);
                if (xAttribute != null && !String.IsNullOrEmpty(xAttribute.Value))
                {
                    var color = (System.Drawing.Color)Convertor.StringToColor(xAttribute.Value);
                    if (!color.IsEmpty)
                        value = color;
                }
            }
            return value;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho Boolean podobu
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        private static bool _ReadAttributeBool(XElement xElement, string attributeName, bool defaultValue)
        {
            bool? value = _ReadAttributeBoolN(xElement, attributeName);
            return value ?? defaultValue;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho Boolean? podobu
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        private static bool? _ReadAttributeBoolN(XElement xElement, string attributeName)
        {
            bool? value = null;
            if (xElement.HasAttributes && !String.IsNullOrEmpty(attributeName))
            {
                var xAttribute = xElement.Attribute(attributeName);
                if (xAttribute != null)
                {
                    if (!String.IsNullOrEmpty(xAttribute.Value))
                    {   // Hledaný atribut existuje:
                        var booln = _ConvertTextToBoolN(xAttribute.Value);
                        if (booln.HasValue) return booln.Value;
                    }
                }
            }
            return value;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména:
        /// Pokud neexistuje, pak vrátí hodnotu <paramref name="defaultValueNotExists"/> (defaultní false).
        /// Pokud atribut existuje, a nemá hodnotu, pak vrátí <paramref name="defaultValueNotValue"/>.
        /// Pokud má hodnotu, pak ji konvertuje na boolean a vrátí.
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValueNotExists">Vrácená hodnota, pokud atribut neexistuje</param>
        /// <param name="defaultValueNotValue">Vrácená hodnota, pokud atribut existuje - ale nemá zadanou žádnou hodnotu (nebo má, ale není platná)</param>
        private static bool _ReadAttributeBoolX(XElement xElement, string attributeName, bool defaultValueNotExists = false, bool defaultValueNotValue = true)
        {
            if (String.IsNullOrEmpty(attributeName)) return defaultValueNotExists;
            if (xElement.HasAttributes)
            {   // Máme atributy
                var xAttribute = xElement.Attribute(attributeName);
                if (xAttribute != null)
                {   // Hledaný atribut existuje:
                    var booln = _ConvertTextToBoolN(xAttribute.Value);
                    // A má nějakou rozumnou hodnotu:
                    if (booln.HasValue) return booln.Value;
                    // Atribut existuje, ale nemá rozpoznatelnou hodnotu (nebo nemá žádnou):
                    return defaultValueNotValue;
                }
            }
            // Atribut neexistuje:
            return defaultValueNotExists;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména:
        /// Pokud neexistuje, pak vrátí hodnotu <paramref name="defaultValueNotExists"/> (defaultní null).
        /// Pokud atribut existuje, a nemá hodnotu, pak vrátí <paramref name="defaultValueNotValue"/> (defaultně true).
        /// Pokud má hodnotu, pak ji konvertuje na boolean a vrátí.
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValueNotExists">Vrácená hodnota, pokud atribut neexistuje</param>
        /// <param name="defaultValueNotValue">Vrácená hodnota, pokud atribut existuje - ale nemá zadanou žádnou hodnotu (nebo má, ale není platná)</param>
        private static bool? _ReadAttributeBoolNX(XElement xElement, string attributeName, bool? defaultValueNotExists = null, bool? defaultValueNotValue = true)
        {
            if (String.IsNullOrEmpty(attributeName)) return defaultValueNotExists;
            if (xElement.HasAttributes)
            {   // Máme atributy
                var xAttribute = xElement.Attribute(attributeName);
                if (xAttribute != null)
                {   // Hledaný atribut existuje:
                    var booln = _ConvertTextToBoolN(xAttribute.Value);
                    // A má nějakou rozumnou hodnotu:
                    if (booln.HasValue) return booln.Value;
                    // Atribut existuje, ale nemá rozpoznatelnou hodnotu (nebo nemá žádnou):
                    return defaultValueNotValue;
                }
            }
            // Atribut neexistuje:
            return defaultValueNotExists;
        }
        /// <summary>
        /// Vrací true nebo false podle obsahu dodaného textu. Může vrátit null, když text je jiný.
        /// </summary>
        /// <param name="text">Dodaný text</param>
        private static bool? _ConvertTextToBoolN(string text)
        {
            if (!String.IsNullOrEmpty(text))
            {
                text = text.Trim().ToLower();
                switch (text)
                {
                    case "0":
                    case "f":
                    case "false":
                    case "n":
                    case "ne":
                    case "no":
                        return false;
                    case "1":
                    case "t":
                    case "true":
                    case "a":
                    case "ano":
                    case "y":
                    case "yes":
                        return true;
                }
            }
            return null;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho hodnotu převedenou do enumu <typeparamref name="TEnum"/>.
        /// Optional dovolí modifikovat načtený text z atributu pomocí funkce <paramref name="modifier"/>.
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        /// <param name="modifier">Modifikátor načteného textu z XML před tím, než proběhne jeho TryParse na hodnotu enumu</param>
        private static TEnum _ReadAttributeEnum<TEnum>(XElement xElement, string attributeName, TEnum defaultValue, Func<string, string> modifier = null) where TEnum : struct
        {
            TEnum? value = _ReadAttributeEnumN<TEnum>(xElement, attributeName, modifier);
            return value ?? defaultValue;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho hodnotu převedenou do enumu <typeparamref name="TEnum"/>.
        /// Optional dovolí modifikovat načtený text z atributu pomocí funkce <paramref name="modifier"/>.
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="modifier">Modifikátor načteného textu z XML před tím, než proběhne jeho TryParse na hodnotu enumu</param>
        private static TEnum? _ReadAttributeEnumN<TEnum>(XElement xElement, string attributeName, Func<string, string> modifier = null) where TEnum : struct
        {
            TEnum? value = null;
            if (xElement.HasAttributes && !String.IsNullOrEmpty(attributeName))
            {
                var xAttribute = xElement.Attribute(attributeName);
                if (xAttribute != null && !String.IsNullOrEmpty(xAttribute.Value))
                {
                    string text = xAttribute.Value;
                    if (modifier != null) text = modifier(text);
                    if (!String.IsNullOrEmpty(text))
                    {
                        if (Enum.TryParse<TEnum>(text, true, out var result))
                        {   // Jednoduchý výsledek je rychle:
                            // Poznámka: tato metoda umí řešit i Flags enumy, které jsou v textu zadané například "Invisible, TabSkip"
                            //  - pokud jsou správně definované jednotkové texty ("TabSkip"),
                            //  - pak správně parsuje i složený text a jeho výsledek!
                            value = result;
                        }
                        else 
                        {   // Nepodařilo se parsovat: pokud v textu je nějaký oddělovač, a pokud enum má Flags, tak budu hodnoty sčítat:
                            var delimiters = " ,;+|".ToCharArray();            // Tyto znaky mohou oddělovat jednotlivé hodnoty: "Invisible, TabSkip"
                            if (text.IndexOfAny(delimiters) > 0)
                            {   // Pokud enum má příznak [Flags], tak rozeberu text a parsuji po částech:
                                var typeCustAttributes = typeof(TEnum).GetCustomAttributes(typeof(System.FlagsAttribute), true);
                                if (typeCustAttributes.Length > 0)
                                {
                                    int? numbers = null;
                                    var parts = text.Split(delimiters);        // text je například "Invisible,TabSkip"
                                    foreach (var part in parts)                // zpracuji postupně: "Invisible", "TabSkip"
                                    {
                                        string item = part.Trim();
                                        if (modifier != null) item = modifier(item);
                                        if (item.Length > 0 && Enum.TryParse<TEnum>(item, true, out var partResult))
                                        {   // Mám hodnotu enumu, například ControlStateType.Invisible
                                            string numValue = Enum.Format(typeof(TEnum), partResult, "D");   // Vrátí string, obsahující decimální hodnotu nalezené jednotkové hodnoty Flags enumu
                                            if (Int32.TryParse(numValue, out var numValueResult))            // Pro ControlStateType.Invisible bude numValueResult = "1"
                                            {   
                                                if (!numbers.HasValue) numbers = 0;
                                                numbers = numbers.Value | numValueResult;                    // Sčítám numerické hodnoty
                                            }
                                        }
                                    }
                                    if (numbers.HasValue)
                                        value = (TEnum)Enum.ToObject(typeof(TEnum), numbers.Value);          // Konvertuji int na enum
                                }
                            }
                        }
                    }
                }
            }
            return value;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho hodnotu převedenou do enumu <typeparamref name="TEnum"/>.
        /// Pro převod načteného textu do typové hodnoty používá dodaný <paramref name="convertor"/>.
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="convertor">Konvertor načteného textu do typové hodnoty</param>
        private static TEnum? _ReadAttributeEnumN<TEnum>(XElement xElement, string attributeName, Func<string, TEnum?> convertor) where TEnum : struct
        {
            TEnum? value = null;
            if (xElement.HasAttributes && !String.IsNullOrEmpty(attributeName))
            {
                var xAttribute = xElement.Attribute(attributeName);
                if (xAttribute != null && !String.IsNullOrEmpty(xAttribute.Value))
                {
                    string text = xAttribute.Value;
                    value = convertor(text);
                }
            }
            return value;
        }
        /// <summary>
        /// Z dodaného <paramref name="xElement"/> načte styl prvku z atributu 'StyleName' nebo z elementu 'style'
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static DfControlStyle _ReadAttributeStyle(XElement xElement, DfControlStyle defaultValue)
        {
            string styleName = _ReadAttributeString(xElement, "StyleName", null);
            if (!String.IsNullOrEmpty(styleName)) return new DfControlStyle() { StyleName = styleName };
            return defaultValue;
        }
        /// <summary>
        /// Z dodaného <paramref name="xElement"/> načte hodnoty odpovídající souřadnicím prvku <see cref="DesignBounds"/> a vrátí je.
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="defaultValue"></param>
        private static DesignBounds _ReadAttributeBounds(XElement xElement, DesignBounds defaultValue)
        {
            DesignBounds designBounds = null;

            // Celý Bounds:
            var textBounds = _ReadAttributeString(xElement, "Bounds", null);
            if (!String.IsNullOrEmpty(textBounds))
            {
                var items = _SplitText(textBounds);
                if (items != null && items.Length >= 2)
                {
                    if (designBounds is null) designBounds = new DesignBounds();
                    designBounds.Left = _ParseInt32N(items[0]);
                    designBounds.Top = _ParseInt32N(items[1]);
                    if (items.Length >= 3) designBounds.Width = _ParseInt32PN(items[2]);
                    if (items.Length >= 4) designBounds.Height = _ParseInt32PN(items[3]);
                    if (designBounds.HasLocation && designBounds.HasSize) return designBounds;
                }
            }

            // Location
            var textLocation = _ReadAttributeString(xElement, "Location", null);
            if (!String.IsNullOrEmpty(textLocation))
            {
                var items = _SplitText(textLocation);
                if (items != null && items.Length >= 2)
                {
                    if (designBounds is null) designBounds = new DesignBounds();
                    designBounds.Left = _ParseInt32N(items[0]);
                    designBounds.Top = _ParseInt32N(items[1]);
                    if (designBounds.HasLocation && designBounds.HasSize) return designBounds;
                }
            }

            // Size
            var textSize = _ReadAttributeString(xElement, "Size", null);
            if (!String.IsNullOrEmpty(textSize))
            {
                var items = _SplitText(textSize);
                if (items != null && items.Length >= 2)
                {
                    if (designBounds is null) designBounds = new DesignBounds();
                    designBounds.Width = _ParseInt32PN(items[0]);
                    designBounds.Height = _ParseInt32PN(items[1]);
                    if (designBounds.HasLocation && designBounds.HasSize) return designBounds;
                }
            }

            // jednotlivé hodnoty:
            var textX = _ReadAttributeString(xElement, "X", null);
            if (!String.IsNullOrEmpty(textX))
            {
                if (designBounds is null) designBounds = new DesignBounds();
                designBounds.Left = _ParseInt32N(textX);
                if (designBounds.HasLocation && designBounds.HasSize) return designBounds;
            }

            var textY = _ReadAttributeString(xElement, "Y", null);
            if (!String.IsNullOrEmpty(textY))
            {
                if (designBounds is null) designBounds = new DesignBounds();
                designBounds.Top = _ParseInt32N(textY);
                if (designBounds.HasLocation && designBounds.HasSize) return designBounds;
            }

            var textWidth = _ReadAttributeString(xElement, "Width", null);
            if (!String.IsNullOrEmpty(textWidth))
            {
                if (designBounds is null) designBounds = new DesignBounds();
                designBounds.Width = _ParseInt32PN(textWidth);
                if (designBounds.HasLocation && designBounds.HasSize) return designBounds;
            }

            var textHeight = _ReadAttributeString(xElement, "Height", null);
            if (!String.IsNullOrEmpty(textHeight))
            {
                if (designBounds is null) designBounds = new DesignBounds();
                designBounds.Height = _ParseInt32PN(textHeight);
                if (designBounds.HasLocation && designBounds.HasSize) return designBounds;
            }

            if (designBounds != null && !designBounds.IsEmpty) return designBounds;
            return defaultValue;
        }
        /// <summary>
        /// Z dodaného <paramref name="xElement"/> načte hodnoty odpovídající souřadnicím <see cref="Location"/> ze zadaného prvku a vrátí je.
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        private static Location _ReadAttributesLocation(XElement xElement, string attributeName, Location defaultValue)
        {
            var textMargins = _ReadAttributeString(xElement, attributeName, null);
            if (!String.IsNullOrEmpty(textMargins))
            {
                var numbers = _SplitAndParseInt32N(textMargins);
                if (numbers != null && numbers.Count >= 1)
                {
                    int cnt = numbers.Count;
                    int? left = numbers[0];
                    int? top = (cnt >= 2 ? numbers[1] : null);
                    if (cnt == 2) return new Location(left, top);
                }
            }
            return defaultValue;
        }
        /// <summary>
        /// Z dodaného <paramref name="xElement"/> načte hodnoty odpovídající souřadnicím prvku a vrátí je.
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        private static Margins _ReadAttributesMargin(XElement xElement, string attributeName, Margins defaultValue)
        {
            var textMargins = _ReadAttributeString(xElement, attributeName, null);
            if (!String.IsNullOrEmpty(textMargins))
            {
                var numbers = _SplitAndParseInt32(textMargins);
                if (numbers != null && numbers.Count >= 1)
                {
                    int cnt = numbers.Count;

                    // All:
                    int left = numbers[0];
                    if (cnt == 1) return new Margins(left);

                    // Horizontal, Verical:
                    int top = numbers[1];
                    if (cnt == 2) return new Margins(left, top);

                    // Left, Top, Right, Bottom:
                    int right = numbers[2];
                    int bottom = (cnt >= 4 ? numbers[3] : top);
                    return new Margins(left, top, right, bottom);
                }
            }
            return defaultValue;
        }
        /// <summary>
        /// Rozdělí dodaný string <paramref name="text"/> v místě daných oddělovačů <paramref name="splitters"/> a převede prvky na čísla Int.
        /// Oddělovač jsou dodány jako jeden string (default = ";, "), ale chápou se jako sada znaků.
        /// Pokud je dodáno více znaků oddělovačů, pak se najde ten první z nich, který je v textu přítomen.<para/>
        /// Např. pro text = <c>"125,4; 200"</c> a oddělovače <c>";, "</c> bude nalezen první přítomný oddělovač <c>';'</c> a v jeho místě bude rozdělen vstupní text. Nikoliv v místě znaku <c>','</c>.
        /// <br/>
        /// Pokud nenajde žádné číslo, vrátí null: tedy pro vstup <c>NULL</c>, <c>""</c> i pro vstup <c>"řetězec"</c> bude vráceno <c>NULL</c>.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="splitters"></param>
        /// <returns></returns>
        private static List<Int32> _SplitAndParseInt32(string text, string splitters = ";, ")
        {
            var items = _SplitText(text, splitters);
            return _ConvertItems<Int32>(items, t => 
            { 
                if (!String.IsNullOrEmpty(t) && Int32.TryParse(t, out var n))
                    return new Tuple<bool, Int32>(true, n);
                return new Tuple<bool, Int32>(false, 0); 
            });
        }
        /// <summary>
        /// Rozdělí dodaný string <paramref name="text"/> v místě daných oddělovačů <paramref name="splitters"/> a převede prvky na čísla Int?.
        /// Oddělovač jsou dodány jako jeden string (default = ";, "), ale chápou se jako sada znaků.
        /// Pokud je dodáno více znaků oddělovačů, pak se najde ten první z nich, který je v textu přítomen.<para/>
        /// Např. pro text = <c>"125,4; 200"</c> a oddělovače <c>";, "</c> bude nalezen první přítomný oddělovač <c>';'</c> a v jeho místě bude rozdělen vstupní text. Nikoliv v místě znaku <c>','</c>.
        /// <br/>
        /// Pokud na vstupu je prázdný string, vrátí null.
        /// <br/>
        /// Pokud ale na vstupu bude string, který obsahuje mezi oddělovači něco nenumerického, bude na tom místě vrácenou NULL.
        /// Tedy pro vstup <c>NULL</c> bude vráceno pole s jedním prvekm NULL, 
        /// pro vstup<c>"4,,250"</c> bude vráceno pole se 3 prvky: 3, NULL, 250; atd.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="splitters"></param>
        /// <returns></returns>
        private static List<Int32?> _SplitAndParseInt32N(string text, string splitters = ";, ")
        {
            var items = _SplitText(text, splitters);
            return _ConvertItems<Int32?>(items, t => 
            {
                if (!String.IsNullOrEmpty(t) && Int32.TryParse(t, out var n))
                    return new Tuple<bool, Int32?>(true, n);
                return new Tuple<bool, Int32?>(true, null);
            });
        }
        /// <summary>
        /// Pokusí se parsovat dodaný text na Int32, nebo vrátí null
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static Int32? _ParseInt32N(string text)
        {
            if (!String.IsNullOrEmpty(text) && Int32.TryParse(text, out var n))
                return n;
            return null;
        }
        /// <summary>
        /// Rozdělí dodaný string <paramref name="text"/> v místě daných oddělovačů <paramref name="splitters"/> a převede prvky na čísla Int?.
        /// Oddělovač jsou dodány jako jeden string (default = ";, "), ale chápou se jako sada znaků.
        /// Pokud je dodáno více znaků oddělovačů, pak se najde ten první z nich, který je v textu přítomen.<para/>
        /// Např. pro text = <c>"125,4; 200"</c> a oddělovače <c>";, "</c> bude nalezen první přítomný oddělovač <c>';'</c> a v jeho místě bude rozdělen vstupní text. Nikoliv v místě znaku <c>','</c>.
        /// <br/>
        /// Pokud na vstupu je prázdný string, vrátí null.
        /// <br/>
        /// Pokud ale na vstupu bude string, který obsahuje mezi oddělovači něco nenumerického, bude na tom místě vrácenou NULL.
        /// Tedy pro vstup <c>NULL</c> bude vráceno pole s jedním prvekm NULL, 
        /// pro vstup<c>"4,,250"</c> bude vráceno pole se 3 prvky: 3, NULL, 250; atd.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="splitters"></param>
        /// <returns></returns>
        private static List<Int32P?> _SplitAndParseInt32PN(string text, string splitters = ";, ")
        {
            var items = _SplitText(text, splitters);
            return _ConvertItems<Int32P?>(items, t =>
            {
                if (!String.IsNullOrEmpty(t) && Int32P.TryParse(t, out var n))
                    return new Tuple<bool, Int32P?>(true, n);
                return new Tuple<bool, Int32P?>(true, null);
            });
        }
        /// <summary>
        /// Pokusí se parsovat dodaný text na Int32P, nebo vrátí null
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static Int32P? _ParseInt32PN(string text)
        {
            if (!String.IsNullOrEmpty(text) && Int32P.TryParse(text, out var n))
                return n;
            return null;
        }
        /// <summary>
        /// Rozdělí dodaný string <paramref name="text"/> v místě daných oddělovačů <paramref name="splitters"/> a vrátí nalezené prvky jako String.
        /// Oddělovač jsou dodány jako jeden string (default = ";, "), ale chápou se jako sada znaků.
        /// Pokud je dodáno více znaků oddělovačů, pak se najde ten první z nich, který je v textu přítomen.<para/>
        /// Např. pro text = <c>"125,4; 200"</c> a oddělovače <c>";, "</c> bude nalezen první přítomný oddělovač <c>';'</c> a v jeho místě bude rozdělen vstupní text. Nikoliv v místě znaku <c>','</c>.<br/>
        /// Pokud na vstupu je prázdný string, vrátí null.<br/>
        /// Pokud na vstupu je neprázdný string, ale neobsahuje žádný oddělovač, je vráceno pole s jedním stringem.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="splitters"></param>
        /// <returns></returns>
        private static string[] _SplitText(string text, string splitters = ";, ")
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(splitters)) return null;

            // Najdu ten oddělovač, který první je přítomen v zadaném textu:
            char splitter = '\0';
            foreach (var spl in splitters)
            {
                if (text.Contains(spl.ToString()))
                {
                    splitter = spl;
                    break;
                }
            }

            // Rozdělím text nalezeným oddělovačem:
            var items = (splitter != '\0') ? text.Split(splitter) : new string[] { text };      // Pokud jsem nenašel oddělovač, nebudu text rozdělovat a vezmu jej jako celek
            return items;
        }
        /// <summary>
        /// Dodané texty konvertuje daným konvertorem do výsledného typu, a vrací pole validních výsledků.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        private static List<T> _ConvertItems<T>(string[] items, Func<string, Tuple<bool, T>> parser)
        {
            if (items is null) return null;

            var result = new List<T>();
            foreach (var item in items)
            {
                var parsed = parser(item);
                if (parsed.Item1)
                    result.Add(parsed.Item2);
            }
            return (result.Count > 0 ? result : null);
        }
        /// <summary>
        /// Vrátí lokální jméno elementu, Trim(), ToLower().
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <returns></returns>
        private static string _GetValidElementName(XElement xElement)
        {
            return xElement?.Name.LocalName.Trim().ToLower() ?? "";
        }
        /// <summary>
        /// Klonuje dodané souřadnice
        /// </summary>
        /// <param name="designBounds"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static DesignBounds _CloneBounds(DesignBounds designBounds)
        {
            return (designBounds is null ? null : new DesignBounds(designBounds.Left, designBounds.Top, designBounds.Width, designBounds.Height));
        }
        /// <summary>
        /// Klonuje velikost z dodaných souřadnic
        /// </summary>
        /// <param name="designBounds"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static DesignBounds _CloneBoundsSize(DesignBounds designBounds)
        {
            return (designBounds is null ? null : new DesignBounds(null, null, designBounds.Width, designBounds.Height));
        }
        /// <summary>
        /// Klonuje pozici z dodaných souřadnic
        /// </summary>
        /// <param name="designBounds"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static DesignBounds _CloneBoundsLocation(DesignBounds designBounds)
        {
            return (designBounds is null ? null : new DesignBounds(designBounds.Left, designBounds.Top));
        }
        /// <summary>
        /// Klonuje dodané okraje
        /// </summary>
        /// <param name="margins"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static Margins _CloneMargins(Margins margins)
        {
            return (margins is null ? null : new Margins(margins.Left, margins.Top, margins.Right, margins.Bottom));
        }
        /// <summary>
        /// Umožní korigovat zadanou hodnotu do atributu typu <see cref="LabelPositionType"/>.
        /// Reprezentuje příklad, jak průběžně změnit názvy položek enumů:
        /// Pokud například v původním kódu existovala hodnota "Up", která byla poté v enumu přejmenována na "Top",
        /// tak existuje řada formulářů (XML dokumentů), které stále obsahují hodnotu "Up" - kterou sice XML editor nyní podtrhává jako vadnou,
        /// ale musíme ji umět načíst => převést text z "Up" na "Top"...
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string _FixLabelPosition(string value)
        {
            string key = (value ?? "").Trim().ToLower();
            return key switch
            {   // Ze staré hodnoty na aktuální:
                "up" => nameof(LabelPositionType.Top),
                "above" => nameof(LabelPositionType.Top),
                "left" => nameof(LabelPositionType.BeforeLeft),
                "right" => nameof(LabelPositionType.After),
                "down" => nameof(LabelPositionType.Bottom),
                "no" => nameof(LabelPositionType.None),
                _ => value,
            };
        }
        #endregion
        #region class DfContext : průběžný kontext při načítání dat šablony
        /// <summary>
        /// <see cref="DfContext"/> : průběžný kontext při načítání dat šablony
        /// </summary>
        private class DfContext
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="loadArgs"></param>
            /// <param name="onlyInfo"></param>
            public DfContext(DfTemplateLoadArgs loadArgs, bool onlyInfo)
            {
                LoadArgs = loadArgs;
                OnlyInfo = onlyInfo;
            }
            /// <summary>
            /// Vnější argumenty
            /// </summary>
            public DfTemplateLoadArgs LoadArgs { get; private set; }
            /// <summary>
            /// Načíst jen záhlaví dokumentu
            /// </summary>
            public bool OnlyInfo { get; private set; }
            /// <summary>
            /// Vznikající formulář
            /// </summary>
            public DfForm Form { get { return LoadArgs.Form; } set { LoadArgs.Form = value; } }
            /// <summary>
            /// Objekt, který je zdrojem dalších dat pro dataform ze strany systému.
            /// Například vyhledá popisný text pro datový control daného jména, určí velikost textu s daným obsahem a daným stylem, atd...
            /// </summary>
            public IControlInfoSource InfoSource { get { return LoadArgs.InfoSource; } }

            /// <summary>
            /// Přidá chybu, nalezenou v parsovaném souboru.
            /// </summary>
            /// <param name="message"></param>
            internal void AddError(string message)
            {
                LoadArgs.AddError(message);
            }

            [DebuggerHidden]
            internal int AutoPageNumber { get { return ++__AutoPageNumber; } } private int __AutoPageNumber = 0;
            [DebuggerHidden]
            internal int AutoPanelNumber { get { return ++__AutoPanelNumber; } } private int __AutoPanelNumber = 0;
            [DebuggerHidden]
            internal int AutoGroupNumber { get { return ++__AutoGroupNumber; } } private int __AutoGroupNumber = 0;
        }
        #endregion
        #region Hierarchie tříd a jejich vlastnosti
        /*

        DfBase                                  Name    State   ToolTip     Invisible
          DfBaseControl
            DfBaseInputControl
              DfBaseInputTextControl
              DfBaseLabeledInputControl
            DfLabel
            DfTitle
          DfSubTextItem
            DfSubButton
          DfBaseArea
            DfForm
            DfPage
            DfBaseContainer
              DfGroup
              DfPanel
          DfNestedGroup
          DfNestedPanel


        */
        #endregion
    }
    #region class DfTemplateLoadArgs : Argumenty pro načítání dat šablony
    /// <summary>
    /// Data pro načtení šablony DataFormu
    /// </summary>
    internal class DfTemplateLoadArgs : DfProcessArgs
    {
        /// <summary>
        /// Plný název souboru na disku, včetně adresáře a přípony
        /// </summary>
        public string TemplateFileName { get; set; }
        /// <summary>
        /// Obsah souboru šablony předaný jako string
        /// </summary>
        public string TemplateContent { get; set; }
        /// <summary>
        /// Parsovaný XML dokument
        /// </summary>
        public XDocument TemplateDocument { get; set; }
        /// <summary>
        /// Vznikající formulář = načtený obsah frm.xml
        /// </summary>
        public DfForm Form { get; set; }
        /// <summary>
        /// Vygeneruje a vrátí argument pro nested šablonu: fyzicky jiný dokument, ale společný zdroj dat <see cref="DfProcessArgs.InfoSource"/> a evidence chyb.
        /// </summary>
        /// <param name="nestedFile"></param>
        /// <param name="nestedContent"></param>
        /// <param name="nestedDocument"></param>
        /// <returns></returns>
        internal DfTemplateLoadArgs CreateNestedArgs(string nestedFile, string nestedContent, XDocument nestedDocument)
        {
            DfTemplateLoadArgs nestedArgs = new DfTemplateLoadArgs();
            nestedArgs.TemplateFileName = nestedFile;
            nestedArgs.TemplateContent = nestedContent;
            nestedArgs.TemplateDocument = nestedDocument;
            nestedArgs.InfoSource = this.InfoSource;
            nestedArgs.Errors = this.Errors;
            return nestedArgs;
        }
    }
    /// <summary>
    /// Bázová třída pro argumenty. Obsahuje podporu pro Errors.
    /// </summary>
    internal class DfProcessArgs
    {
        /// <summary>
        /// Objekt, který je zdrojem dalších dat pro dataform ze strany systému.
        /// Například vyhledá popisný text pro datový control daného jména, určí velikost textu s daným obsahem a daným stylem, atd...
        /// </summary>
        public IControlInfoSource InfoSource { get; set; }
        /// <summary>
        /// Logovat časy načítání
        /// </summary>
        public bool LogTime { get; set; }
        /// <summary>
        /// Obsahuje true, pokud jsou zachyceny nějaké chyby.
        /// </summary>
        public bool HasErrors { get { return (__Errors != null && __Errors.Length > 0); } }
        /// <summary>
        /// Souhrn chyb, nalezených v parsovaném souboru. Null = žádná.
        /// </summary>
        public string ErrorsText { get { return __Errors?.ToString(); } }
        /// <summary>
        /// Souhrn chyb. 
        /// Lze setovat z jiného argumentu, tím se soubor chyb sdílí mezi částmi procesu (Loading .. Layout).
        /// Autoinicializační (první get vytvoří new instanci).
        /// </summary>
        public StringBuilder Errors 
        { 
            get 
            {
                if (__Errors is null) __Errors = new StringBuilder();
                return __Errors;
            }
            set { __Errors = value; }
        }
        /// <summary>
        /// Přidá chybu, nalezenou v parsovaném souboru.
        /// </summary>
        /// <param name="message"></param>
        internal void AddError(string message)
        {
            if (!String.IsNullOrEmpty(message))
            {
                __Errors ??= new StringBuilder();
                __Errors.AppendLine(message);
            }
        }
        /// <summary>
        /// Souhrn chyb, výchozí je null
        /// </summary>
        private StringBuilder __Errors;
    }
    #endregion
    #region class DfColumn : obraz načteného sloupce z formuláře Infragistic (element column)
    /// <summary>
    /// Schránka na data, definující jeden <b><u>column</u></b> v deklaraci formuláře Infragistic
    /// </summary>
    internal class DfColumn
    {
        public string Name { get; set; }
        public int? TabIndex { get; set; }
        public string EditMask { get; set; }
        public string InputWindow { get; set; }
        public bool? CurrencyDisplay { get; set; }
        public int? MaxLength { get; set; }
        public string Label { get; set; }
        public string LabelToolTip { get; set; }
        public string LabelToolTipHide { get; set; }
        public Int32P? Width { get; set; }
        public Int32P? Height { get; set; }
        public bool? Required { get; set; }
        public string Invisible { get; set; }
        public bool? ReadOnly { get; set; }
        public string InputType { get; set; }
        public string SyntaxHighlightingType { get; set; }
        public string AidcCodeType { get; set; }
        public string AidcCodeSettings { get; set; }
        public string PercentageBarSettings { get; set; }
        public string ButtonAction { get; set; }
        public string ButtonFunction { get; set; }
        public string ButtonFunctionLabelType { get; set; }
        public string Values { get; set; }
        public string Expr { get; set; }
        public string ExprType { get; set; }
        public string EditStyle { get; set; }
        public string EditStyleViewMode { get; set; }
        public string Protect { get; set; }
        public bool? HtmlEdit { get; set; }
        public bool? Relation { get; set; }
        public string HtmlStyle { get; set; }
        public int? ColSpan { get; set; }
        public int? RowSpan { get; set; }
        public LabelPositionType? LabelPos { get; set; }
        public ContentAlignmentType? Align { get; set; }
        public ContentAlignmentType? AlignValue { get; set; }
        public bool? RegisterItemChange { get; set; }
        public bool? RegisterDblClick { get; set; }
        public string BoxCols { get; set; }
        public string BoxRows { get; set; }
        public string BoxStyle { get; set; }
        public bool? AttrShortName { get; set; }
        public string RelationParams { get; set; }
        public bool? RelationAddName { get; set; }
        public string RenderAs { get; set; }
        public bool? SetEmptyStringIsNull { get; set; }
        public int? MaxDropDownItems { get; set; }
        public bool? IsBreak { get; set; }
        public bool? IsDefault { get; set; }
        public string AllowUserChangeInvisibility { get; set; }
        public bool? OneMonthOnly { get; set; }
        public bool? NoBorder { get; set; }
        public string RadioTextAlign { get; set; }
        public string TextAreaOverflow { get; set; }
        public string FontAndColor { get; set; }
        public string AcceptPromptFormatMask { get; set; }
        public string Image { get; set; }
        public string ToolTip { get; set; }
        public string LinkType { get; set; }
        public bool? AllowExtendedEditor { get; set; }
        public bool? SuppressReadOnlyFromDataForm { get; set; }
        public string DDLBEditor { get; set; }
        public string ExtendedAttributes { get; set; }
        public string FileFilter { get; set; }
    }
    #endregion
}