﻿// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Noris.WS.DataContracts.DxForm;

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
        /// <param name="args"></param>
        /// <param name="onlyInfo"></param>
        /// <returns></returns>
        private static DfForm _LoadDfForm(DfTemplateLoadArgs args, bool onlyInfo)
        {
            System.Xml.Linq.XElement xElement = _LoadRootXElement(args);
            if (xElement is null) return null;

            var startTime = DxComponent.LogTimeCurrent;
            var dfForm = _FillAreaDfForm(xElement, null, args, onlyInfo);
            if (args.LogTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load '{(onlyInfo ? "DfInfo" : "DfForm")}' from 'XDocument': {DxComponent.LogTokenTimeMicrosec}", startTime);

            return dfForm;
        }
        /// <summary>
        /// Z dodaných podkladů, definujících šablonu (<paramref name="args"/>), načte a vrátí Root element <see cref="System.Xml.Linq.XElement"/>.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static System.Xml.Linq.XElement _LoadRootXElement(DfTemplateLoadArgs args)
        {
            // Pokud není zadáno nic, co by určilo šablonu:
            if (args.TemplateDocument is null && String.IsNullOrEmpty(args.TemplateContent) && String.IsNullOrEmpty(args.TemplateFileName))
            {
                args.AddError($"Pro načtení formuláře nejsou předaná žádná data (DfTemplateArgs: TemplateFileName, TemplateContent, TemplateDocument jsou prázdné).");
                return null;
            }

            // Je zadán pouze název souboru => budeme načítat obsah souboru do TemplateContent:
            if (args.TemplateDocument is null && String.IsNullOrEmpty(args.TemplateContent))
            {   // Načteme soubor, pokud existuje:
                if (!System.IO.File.Exists(args.TemplateFileName)) 
                {
                    args.AddError($"Pro načtení formuláře je dodán název souboru '{args.TemplateFileName}', ten ale neexistuje.");
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
                args.TemplateDocument = System.Xml.Linq.XDocument.Parse(args.TemplateContent);
                if (args.LogTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Parse 'XDocument' from 'Content' ({args.TemplateContent.Length} B):': {DxComponent.LogTokenTimeMicrosec}", startTime);
            }

            return args.TemplateDocument?.Root;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <see cref="DfForm"/>.
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="dfForm"></param>
        /// <param name="args"></param>
        /// <param name="onlyInfo"></param>
        /// <returns></returns>
        private static DfForm _FillAreaDfForm(System.Xml.Linq.XElement xElement, DfForm dfForm, DfTemplateLoadArgs args, bool onlyInfo)
        {
            if (xElement is null) return null;
            if (dfForm is null) dfForm = new DfForm();

            // Atributy:
            _FillBaseAttributes(xElement, dfForm);
            dfForm.XmlNamespace = _ReadAttributeString(xElement, "xmlns", null);
            dfForm.FormatVersion = _ReadAttributeEnum(xElement, "FormatVersion", FormatVersionType.Default, t => "Version" + t);

            // Rychlá odbočka?
            if (onlyInfo) return dfForm;

            // FileName (a Name, pokud není explicitně načteno) podle jména souboru:
            dfForm.FileName = args.TemplateFileName;
            if (dfForm.Name is null && !String.IsNullOrEmpty(args.TemplateFileName)) dfForm.Name = System.IO.Path.GetFileNameWithoutExtension(args.TemplateFileName ?? "");

            // Full Load:
            dfForm.MasterWidth = _ReadAttributeInt32N(xElement, "MasterWidth");
            dfForm.MasterHeight = _ReadAttributeInt32N(xElement, "MasterHeight");
            dfForm.TotalWidth = _ReadAttributeInt32N(xElement, "TotalWidth");
            dfForm.TotalHeight = _ReadAttributeInt32N(xElement, "TotalHeight");
            dfForm.AutoLabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "AutoLabelPosition");
            dfForm.DataSource = _ReadAttributeString(xElement, "DataSource", null);
            dfForm.Messages = _ReadAttributeString(xElement, "Messages", null);
            dfForm.UseNorisClass = _ReadAttributeInt32N(xElement, "UseNorisClass");
            dfForm.AddUda = _ReadAttributeBoolN(xElement, "AddUda");
            dfForm.UdaLabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "UdaLabelPosition");
            dfForm.Margins = _ReadAttributesMargin(xElement, "Margins", null);
            dfForm.ContextMenu = _ReadAttributeBoolN(xElement, "ContextMenu");
            dfForm.ColumnsCount = _ReadAttributeInt32N(xElement, "ColumnsCount");
            dfForm.ColumnWidths = _ReadAttributeString(xElement, "ColumnWidths", null);

            // Implicit Page: do ní se vkládají Panely, pokud jsou zadány přímo do Formu
            DfPage implicitPage = null;

            // Elementy = stránky (nebo přímo panely):
            var xContainers = xElement.Elements();
            if (xContainers != null)
            {
                string sourceInfo = $"Formulář '{dfForm.Name}'";
                foreach (var xContainer in xContainers)
                {
                    var container = _CreateArea(xContainer, sourceInfo, args, "page", "panel", "nestedpanel");
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
        /// Pokud je výstupem null, pak informace o chybě je již zanesena do <paramref name="args"/>, kam se uvádí i zdroj = <paramref name="sourceInfo"/>.
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="sourceInfo">Informace o zdrojovém místě, do chybové informace. Typicky: "Formulář 'Jméno'" nebo "Stránka 'Name01'".</param>
        /// <param name="args">Průběžná data pro načítání obsahu</param>
        /// <param name="validNames">Očekávaná validní jména elementů. Pokud je zadáno, a je detekován jiný než daný element, vrátí se null.</param>
        private static DfBaseArea _CreateArea(System.Xml.Linq.XElement xElement, string sourceInfo, DfTemplateLoadArgs args, params string[] validNames)
        {
            string elementName = _GetValidElementName(xElement);               // page, panel, nestedpanel, group, nestedgroup
            if (String.IsNullOrEmpty(elementName)) return null;                // Nezadáno (?)
            // Pokud je dodán seznam validních jmen elementů (přinejmenším 1 prvek), ale aktuální element neodpovídá žádnému povolenému jménu, pak skončím:
            if (validNames != null && validNames.Length > 0 && !validNames.Any(v => String.Equals(v, elementName, StringComparison.OrdinalIgnoreCase)))
            {
                args.AddError($"{sourceInfo} obsahuje prvek '{elementName}', který není přípustný.");
                return null;
            }
            switch (elementName)
            {
                case "page": return _FillAreaPage(xElement, null, args);
                case "panel": return _FillAreaPanel(xElement, null, args);
                case "nestedpanel": return _FillAreaNestedPanel(xElement, null, args);
                case "group": return _FillAreaGroup(xElement, null, args);
                case "nestedgroup": return _FillAreaNestedGroup(xElement, null, args);
            }
            args.AddError($"{sourceInfo} obsahuje prvek '{elementName}', který zde není očekáváván.");
            return null;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <c>page</c>, včetně jeho obsahu (tj. atributy a child elementy).
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="dfPage"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static DfPage _FillAreaPage(System.Xml.Linq.XElement xElement, DfPage dfPage, DfTemplateLoadArgs args)
        {
            // Výsledná instance:
            if (dfPage is null) dfPage = new DfPage();

            // Atributy:
            _FillBaseAttributes(xElement, dfPage);
            dfPage.IconName = _ReadAttributeString(xElement, "IconName", null);
            dfPage.Title = _ReadAttributeString(xElement, "Title", null);

            // Elementy = panely a nested panely:
            var xPanels = xElement.Elements();
            if (xPanels != null)
            {
                string sourceInfo = $"Stránka '{dfPage.Name}'";
                foreach (var xPanel in xPanels)
                {
                    var container = _CreateArea(xPanel, sourceInfo, args, "panel", "nestedpanel");
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
        /// <param name="args"></param>
        /// <returns></returns>
        private static DfPanel _FillAreaPanel(System.Xml.Linq.XElement xElement, DfPanel dfPanel, DfTemplateLoadArgs args)
        {
            // Výsledná instance:
            if (dfPanel is null) dfPanel = new DfPanel();

            // Atributy:
            _FillBaseAttributes(xElement, dfPanel);
            dfPanel.IsHeader = _ReadAttributeBoolN(xElement, "IsHeader");
            dfPanel.HeaderOnPages = _ReadAttributeString(xElement, "HeaderOnPages", null);
            dfPanel.IconName = _ReadAttributeString(xElement, "IconName", null);
            dfPanel.Title = _ReadAttributeString(xElement, "Title", null);
            dfPanel.TitleStyle = _ReadAttributeEnumN<TitleStyleType>(xElement, "TitleStyle");
            dfPanel.TitleColorName = _ReadAttributeString(xElement, "TitleColorName", null);
            dfPanel.TitleColorLight = _ReadAttributeColorN(xElement, "TitleColorLight");
            dfPanel.TitleColorDark = _ReadAttributeColorN(xElement, "TitleColorDark");
            dfPanel.CollapseState = _ReadAttributeEnumN<PanelCollapseState>(xElement, "CollapseState");

            // Elementy = Controly + Grupy:
            _FillContainerChildElements(xElement, dfPanel, args);

            return dfPanel;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <c>nestedpanel</c>, včetně jeho obsahu (tj. atributy a child elementy).
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="dfPanelVoid"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static DfPanel _FillAreaNestedPanel(System.Xml.Linq.XElement xElement, DfPanel dfPanelVoid, DfTemplateLoadArgs args)
        {
            // Instance DfNestedPanel slouží k načtení definice z aktuálního formuláře, ale nejde o výslednou instanci:
            DfNestedPanel dfNestedPanel = new DfNestedPanel();

            // Atributy:
            _FillBaseAttributes(xElement, dfNestedPanel);
            dfNestedPanel.NestedTemplate = _ReadAttributeString(xElement, "NestedTemplate", null);
            dfNestedPanel.NestedPanelName = _ReadAttributeString(xElement, "NestedPanelName", null);
            dfNestedPanel.IsHeader = _ReadAttributeBoolN(xElement, "IsHeader");
            dfNestedPanel.HeaderOnPages = _ReadAttributeString(xElement, "HeaderOnPages", null);

            // Nested šablona:
            if (!_TryLoadNestedTemplate(dfNestedPanel.NestedTemplate, args, out DfForm dfNestedForm, $"NestedPanel '{dfNestedPanel.Name}'")) return null;

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
                args.AddError($"Požadovaný NestedPanel '{dfNestedPanel.NestedPanelName}' nebyl v nested šabloně '{dfNestedPanel.NestedTemplate}' nalezen.");
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
        /// <param name="args"></param>
        /// <returns></returns>
        private static DfGroup _FillAreaGroup(System.Xml.Linq.XElement xElement, DfGroup dfGroup, DfTemplateLoadArgs args)
        {
            // Výsledná instance:
            if (dfGroup is null) dfGroup = new DfGroup();

            // Atributy:
            _FillBaseAttributes(xElement, dfGroup);
            dfGroup.ColIndex = _ReadAttributeInt32N(xElement, "ColIndex");
            dfGroup.ColSpan = _ReadAttributeInt32N(xElement, "ColSpan");

            // Elementy = Controly + Panely:
            _FillContainerChildElements(xElement, dfGroup, args);

            return dfGroup;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <c>nestedgroup</c>, včetně jeho obsahu (tj. atributy a child elementy).
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="dfGroupVoid"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static DfGroup _FillAreaNestedGroup(System.Xml.Linq.XElement xElement, DfGroup dfGroupVoid, DfTemplateLoadArgs args)
        {
            // Instance DfNestedGroup slouží k načtení definice z aktuálního formuláře, ale nejde o výslednou instanci:
            DfNestedGroup dfNestedGroup = new DfNestedGroup();

            // Atributy:
            _FillBaseAttributes(xElement, dfNestedGroup);
            dfNestedGroup.NestedTemplate = _ReadAttributeString(xElement, "NestedTemplate", null);
            dfNestedGroup.NestedGroupName = _ReadAttributeString(xElement, "NestedPanelName", null);
            dfNestedGroup.Bounds = _ReadAttributeBounds(xElement, null);
            dfNestedGroup.ColIndex = _ReadAttributeInt32N(xElement, "ColIndex");
            dfNestedGroup.ColSpan = _ReadAttributeInt32N(xElement, "ColSpan");

            // Nested šablona:
            if (!_TryLoadNestedTemplate(dfNestedGroup.NestedTemplate, args, out DfForm dfNestedForm, $"NestedGroup '{dfNestedGroup.Name}'")) return null;

            // Najde v načtené šabloně první panel DfPanel [kterýkoli nebo daného jména]:
            if (trySearchForGroup(dfNestedForm, dfNestedGroup.NestedGroupName, out DfGroup dfGroup))
            {   // Převezmeme něco z naší DfNestedGroup do výstupní DfGroup:
                dfGroup.Name = dfNestedGroup.Name;

                if (dfNestedGroup.State.HasValue) dfGroup.State = dfNestedGroup.State;
                if (dfNestedGroup.ToolTipTitle != null) dfGroup.ToolTipTitle = dfNestedGroup.ToolTipTitle;
                if (dfNestedGroup.ToolTipText != null) dfGroup.ToolTipText = dfNestedGroup.ToolTipText;
                if (dfNestedGroup.Invisible != null) dfGroup.Invisible = dfNestedGroup.Invisible;
                dfGroup.Bounds = dfNestedGroup.Bounds;
                dfGroup.ColIndex = dfNestedGroup.ColIndex;
                dfGroup.ColSpan = dfNestedGroup.ColSpan;
            }
            else
            {
                args.AddError($"Požadovaná NestedGroup '{dfNestedGroup.NestedGroupName}' nebyla v nested šabloně '{dfNestedGroup.NestedTemplate}' nalezena.");
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
        /// <param name="args"></param>
        private static void _FillContainerChildElements(System.Xml.Linq.XElement xElement, DfBaseContainer control, DfTemplateLoadArgs args)
        {
            var xChilds = xElement.Elements();
            if (xChilds != null)
            {
                string sourceInfo = $"Container {control.GetType().Name} '{control.Name}'";
                foreach (var xChild in xChilds)
                {
                    var child = _CreateChildItem(xChild, sourceInfo, args);
                    if (child != null && (child is DfBaseControl || child is DfGroup))
                    {   // Pouze Controly + Group
                        if (control.Childs is null) control.Childs = new List<DfBase>();
                        control.Childs.Add(child);
                    }
                }
            }
        }
        /// <summary>
        /// Metoda zajistí načtení instance <see cref="DfForm"/> pro danou <paramref name="dfNestedForm"/>, s pomocí loader v <paramref name="args"/>.
        /// </summary>
        /// <param name="nestedTemplate"></param>
        /// <param name="args"></param>
        /// <param name="dfNestedForm"></param>
        /// <param name="sourceInfo"></param>
        /// <returns></returns>
        private static bool _TryLoadNestedTemplate(string nestedTemplate, DfTemplateLoadArgs args, out DfForm dfNestedForm, string sourceInfo)
        {
            dfNestedForm = null;

            // Jméno nested šablony:
            if (String.IsNullOrEmpty(nestedTemplate))
            {
                args.AddError($"{sourceInfo} nemá zadanou šablonu 'NestedTemplate'.");
                return false;
            }

            // Obsah nested panelu získám s pomocí dodaného loaderu :
            if (args.NestedTemplateLoader is null)
            {
                args.AddError($"{sourceInfo} má zadanou šablonu 'NestedTemplate', ale není dodána metoda Loader, která by načetla její obsah. Není možno načíst obsah šablony.");
                return false;
            }
            string nestedContent = args.NestedTemplateLoader(nestedTemplate);
            if (String.IsNullOrEmpty(nestedContent))
            {   // Prázdný obsah: pokud to loader vrátí, pak OK, je to legální cesta, jak zrušit Nested obsah:
                args.AddError($"{sourceInfo} má zadanou šablonu 'NestedTemplate' = '{nestedTemplate}', ale její obsah nelze načíst.");
                return false;
            }

            // Ze stringu 'nestedContent' (obsahuje Nested šablonu) získám celou šablonu = nested DfForm:
            // var xNestedDocument = System.Xml.Linq.XDocument.Parse(nestedContent);
            var nestedArgs = args.CreateNestedArgs(null, nestedContent, null);
            dfNestedForm = _LoadDfForm(nestedArgs, false);
            return (dfNestedForm != null);
        }
        #endregion
        #region Načítání obsahu - private tvorba jednotlivých controlů

        // Každá zdejší větev / metoda načte pouze property deklarované přímo pro danou třídu, nikoli pro její předky!

        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající Item, včetně jeho obsahu.
        /// Může to být některý Control, anebo grupa (včetně NestedGroup). Nikoli Panel. Výstupem může být null.
        /// Pokud je výstupem null, pak informace o chybě je již zanesena do <paramref name="args"/>, kam se uvádí i zdroj = <paramref name="sourceInfo"/>.
        /// </summary>
        /// <param name="xElement">Element, z něhož se má vytvořit objekt</param>
        /// <param name="sourceInfo">Informace o zdrojovém místě, do chybové informace. Typicky: "Formulář 'Jméno'" nebo "Stránka 'Name01'".</param>
        /// <param name="args">Průběžná data pro načítání obsahu</param>
        /// <param name="validNames">Očekávaná validní jména elementů. Pokud je zadáno, a je detekován jiný než daný element, vrátí se null.</param>
        /// <returns></returns>
        private static DfBase _CreateChildItem(System.Xml.Linq.XElement xElement, string sourceInfo, DfTemplateLoadArgs args, params string[] validNames)
        {
            string elementName = _GetValidElementName(xElement);               // label, textbox, textbox_button, button, combobox, ...,   !page, panel, nestedpanel,
            if (String.IsNullOrEmpty(elementName)) return null;                // Nezadáno (?)
            // Pokud je dodán seznam validních jmen elementů (přinejmenším 1 prvek), ale aktuální element neodpovídá žádnému povolenému jménu, pak skončím:
            if (validNames != null && validNames.Length > 0 && !validNames.Any(v => String.Equals(v, elementName, StringComparison.OrdinalIgnoreCase)))
            {
                args.AddError($"{sourceInfo} obsahuje prvek '{elementName}', který není přípustný.");
                return null;
            }
            switch (elementName)
            {
                // Controly:
                case "label": return _FillControlLabel(xElement, new DfLabel(), args);
                case "title": return _FillControlTitle(xElement, new DfTitle(), args);
                case "checkbox": return _FillControlCheckBox(xElement, new DfCheckBox(), args);
                case "button": return _FillControlButton(xElement, new DfButton(), args);
                case "dropdownbutton": return _FillControlDropDownButton(xElement, new DfDropDownButton(), args);
                case "textbox": return _FillControlTextBox(xElement, new DfTextBox(), args);
                case "editbox": return _FillControlEditBox(xElement, new DfEditBox(), args);
                case "textboxbutton": return _FillControlTextBoxButton(xElement, new DfTextBoxButton(), args);
                case "combobox": return _FillControlComboBox(xElement, new DfComboBox(), args);

                // SubContainery = grupy:
                case "group": return _FillAreaGroup(xElement, null, args);
                case "nestedgroup": return _FillAreaNestedGroup(xElement, null, args);
            }
            args.AddError($"{sourceInfo} obsahuje prvek '{elementName}', který zde není očekáváván.");
            return null;
        }
        private static DfBaseControl _FillControlLabel(System.Xml.Linq.XElement xElement, DfLabel control, DfTemplateLoadArgs args)
        {
            _FillBaseAttributes(xElement, control);
            control.Text = _ReadAttributeString(xElement, "Text", null);
            control.Alignment = _ReadAttributeEnumN<ContentAlignmentType>(xElement, "Alignment");
            return control;
        }
        private static DfBaseControl _FillControlTitle(System.Xml.Linq.XElement xElement, DfTitle control, DfTemplateLoadArgs args)
        {
            _FillBaseAttributes(xElement, control);
            control.IconName = _ReadAttributeString(xElement, "IconName", null);
            control.Title = _ReadAttributeString(xElement, "Title", null);
            control.Style = _ReadAttributeEnumN<TitleStyleType>(xElement, "Style");
            control.Alignment = _ReadAttributeEnumN<ContentAlignmentType>(xElement, "Alignment");
            return control;
        }
        private static DfBaseControl _FillControlCheckBox(System.Xml.Linq.XElement xElement, DfCheckBox control, DfTemplateLoadArgs args)
        {
            _FillBaseAttributes(xElement, control);
            control.Style = _ReadAttributeEnumN<CheckBoxStyleType>(xElement, "Style");
            return control;
        }
        private static DfBaseControl _FillControlButton(System.Xml.Linq.XElement xElement, DfButton control, DfTemplateLoadArgs args)
        {
            _FillBaseAttributes(xElement, control);
            control.ActionType = _ReadAttributeEnumN<ButtonActionType>(xElement, "ActionType");
            control.ActionData = _ReadAttributeString(xElement, "ActionData", null);
            control.HotKey = _ReadAttributeString(xElement, "HotKey", null);
            return control;
        }
        private static DfBaseControl _FillControlDropDownButton(System.Xml.Linq.XElement xElement, DfDropDownButton control, DfTemplateLoadArgs args)
        {
            _FillControlButton(xElement, control, args);

            // Elementy = Items:
            var xItems = xElement.Elements();
            if (xItems != null)
            {
                foreach (var xItem in xItems)
                {
                    string elementName = xItem?.Name.LocalName;
                    if (String.Equals(elementName, "dropDownButton", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var subButton = _FillControlSubButton(xItem, new DfSubButton(), args);
                        if (subButton != null)
                        {
                            if (control.DropDownButtons is null)
                                control.DropDownButtons = new List<DfSubButton>();
                            control.DropDownButtons.Add(subButton);
                        }
                    }
                }
            }

            return control;
        }
        private static DfBaseControl _FillControlTextBox(System.Xml.Linq.XElement xElement, DfTextBox control, DfTemplateLoadArgs args)
        {
            _FillBaseAttributes(xElement, control);
            control.EditMask = _ReadAttributeString(xElement, "EditMask", null);
            control.Alignment = _ReadAttributeEnumN<ContentAlignmentType>(xElement, "Alignment");
            return control;
        }
        private static DfBaseControl _FillControlEditBox(System.Xml.Linq.XElement xElement, DfEditBox control, DfTemplateLoadArgs args)
        {
            _FillBaseAttributes(xElement, control);
            return control;
        }
        private static DfBaseControl _FillControlTextBoxButton(System.Xml.Linq.XElement xElement, DfTextBoxButton control, DfTemplateLoadArgs args)
        {
            _FillControlTextBox(xElement, control, args);
            control.ButtonsVisibility = _ReadAttributeEnumN<ButtonsVisibilityType>(xElement, "ButtonsVisibility");

            // Elementy = SubButtons:
            var xItems = xElement.Elements();
            if (xItems != null)
            {
                foreach (var xItem in xItems)
                {
                    string elementName = xItem?.Name.LocalName;
                    if (String.Equals(elementName, "leftButton", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var subButton = _FillControlSubButton(xItem, new DfSubButton(), args);
                        if (subButton != null)
                        {
                            if (control.LeftButtons is null)
                                control.LeftButtons = new List<DfSubButton>();
                            control.LeftButtons.Add(subButton);
                        }
                    }
                    else if (String.Equals(elementName, "rightButton", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var subButton = _FillControlSubButton(xItem, new DfSubButton(), args);
                        if (subButton != null)
                        {
                            if (control.RightButtons is null)
                                control.RightButtons = new List<DfSubButton>();
                            control.RightButtons.Add(subButton);
                        }
                    }
                }
            }

            return control;
        }
        private static DfBaseControl _FillControlComboBox(System.Xml.Linq.XElement xElement, DfComboBox control, DfTemplateLoadArgs args)
        {
            _FillBaseAttributes(xElement, control);
            control.EditStyleName = _ReadAttributeString(xElement, "EditStyleName", null);
            control.Style = _ReadAttributeEnumN<ComboBoxStyleType>(xElement, "Style");

            // Elementy = SubButtons:
            var xItems = xElement.Elements();
            if (xItems != null)
            {
                foreach (var xItem in xItems)
                {
                    string elementName = xItem?.Name.LocalName;
                    if (String.Equals(elementName, "comboItem", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var comboItem = _FillControlSubTextItem(xItem, new DfSubTextItem(), args);
                        if (comboItem != null)
                        {
                            if (control.ComboItems is null)
                                control.ComboItems = new List<DfSubTextItem>();
                            control.ComboItems.Add(comboItem);
                        }
                    }
                }
            }

            return control;
        }
        private static DfSubButton _FillControlSubButton(System.Xml.Linq.XElement xElement, DfSubButton control, DfTemplateLoadArgs args)
        {
            _FillControlSubTextItem(xElement, control, args);
            control.ActionType = _ReadAttributeEnumN<SubButtonActionType>(xElement, "ActionType");
            control.ActionData = _ReadAttributeString(xElement, "ActionData", null);
            return control;
        }
        private static DfSubTextItem _FillControlSubTextItem(System.Xml.Linq.XElement xElement, DfSubTextItem control, DfTemplateLoadArgs args)
        {
            _FillBaseAttributes(xElement, control);
            control.Text = _ReadAttributeString(xElement, "Text", null);
            control.IconName = _ReadAttributeString(xElement, "IconName", null);
            return control;
        }
        #endregion
        #region Načítání atributů
        /// <summary>
        /// Z dodaného <paramref name="xElement"/> načte hodnoty odpovídající cílovému typu,
        /// a vloží je do dodaného controlu <paramref name="target"/>.
        /// <para/>
        /// Načítá hodnoty odpovídající třídám: <see cref="DfBase"/>, <see cref="DfBaseControl"/>, <see cref="DfBaseInputControl"/>,
        /// <see cref="DfBaseInputTextControl"/>, <see cref="DfBaseLabeledInputControl"/>, <see cref="DfSubButton"/>.<br/>
        /// Dále načítá hodnoty pro třídy containerů: <see cref="DfBaseArea"/>, <see cref="DfBaseContainer"/>.
        /// </summary>
        /// <param name="xElement">Element, z něhož se mají načítat atributy</param>
        /// <param name="target"></param>
        private static void _FillBaseAttributes(System.Xml.Linq.XElement xElement, DfBase target)
        {
            // Každá zdejší větev / metoda načte pouze property deklarované přímo pro danou třídu, nikoli pro její předky!

            // DfBase:
            target.Name = _ReadAttributeString(xElement, "Name", null);
            target.State = _ReadAttributeControlState(xElement, ControlStateType.Default);
            target.ToolTipTitle = _ReadAttributeString(xElement, "ToolTipTitle", null);
            target.ToolTipText = _ReadAttributeString(xElement, "ToolTipText", null);
            target.Invisible = _ReadAttributeString(xElement, "Invisible", null);

            // Potomci směrem k Controlům:
            if (target is DfBaseControl control)
            {
                control.ControlStyle = _ReadAttributeStyle(xElement, null);
                control.Bounds = _ReadAttributeBounds(xElement, null);
                control.ColIndex = _ReadAttributeInt32N(xElement, "ColIndex");
                control.ColSpan = _ReadAttributeInt32N(xElement, "ColSpan");
            }
            if (target is DfBaseInputControl inputControl)
            {
                inputControl.Required = _ReadAttributeEnumN<RequiredType>(xElement, "Required");
            }
            if (target is DfBaseInputTextControl textControl)
            {
                textControl.Text = _ReadAttributeString(xElement, "Text", null);
                textControl.IconName = _ReadAttributeString(xElement, "IconName", null);
                textControl.Alignment = _ReadAttributeEnumN<ContentAlignmentType>(xElement, "Alignment");
            }
            if (target is DfBaseLabeledInputControl labeledControl)
            {
                labeledControl.Label = _ReadAttributeString(xElement, "Label", null);
                labeledControl.LabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "LabelPosition");
                labeledControl.LabelWidth = _ReadAttributeInt32N(xElement, "LabelWidth");
            }

            // Potomci směrem k Containerům:
            if (target is DfBaseArea area)
            {
                area.BackColorName = _ReadAttributeString(xElement, "BackColorName", null);
                area.BackColorLight = _ReadAttributeColorN(xElement, "BackColorLight");
                area.BackColorDark = _ReadAttributeColorN(xElement, "BackColorDark");
                area.BackImageName = _ReadAttributeString(xElement, "BackImageName", null);
                area.BackImagePosition = _ReadAttributeEnumN<BackImagePositionType>(xElement, "BackImagePosition");
                area.Margins = _ReadAttributesMargin(xElement, "Margins", null);
                area.ColumnsCount = _ReadAttributeInt32N(xElement, "ColumnsCount");
                area.ColumnWidths = _ReadAttributeString(xElement, "ColumnWidths", null);
                area.AutoLabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "AutoLabelPosition");
            }
            if (target is DfBaseContainer container)
            {
                container.Bounds = _ReadAttributeBounds(xElement, null);
                container.FlowLayoutOrigin = _ReadAttributesLocation(xElement, "FlowLayoutOrigin", null);
            }
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho String podobu
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        private static string _ReadAttributeString(System.Xml.Linq.XElement xElement, string attributeName, string defaultValue)
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
        /// Načte stav prvku <see cref="ControlStateType"/> z atributu 'State' anebo jej složí z atributů ''
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private static ControlStateType _ReadAttributeControlState(System.Xml.Linq.XElement xElement, ControlStateType defaultValue)
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
        private static int _ReadAttributeInt32(System.Xml.Linq.XElement xElement, string attributeName, int defaultValue)
        {
            int? value = _ReadAttributeInt32N(xElement, attributeName);
            return value ?? defaultValue;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho Int32 podobu
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        private static int? _ReadAttributeInt32N(System.Xml.Linq.XElement xElement, string attributeName)
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
        /// V daném elementu najde atribut daného jména a vrátí jeho Color? podobu
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        private static System.Drawing.Color _ReadAttributeColor(System.Xml.Linq.XElement xElement, string attributeName, System.Drawing.Color defaultValue)
        {
            System.Drawing.Color? value = _ReadAttributeColorN(xElement, attributeName);
            return value ?? defaultValue;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho Color? podobu
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        private static System.Drawing.Color? _ReadAttributeColorN(System.Xml.Linq.XElement xElement, string attributeName)
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
        private static bool _ReadAttributeBool(System.Xml.Linq.XElement xElement, string attributeName, bool defaultValue)
        {
            bool? value = _ReadAttributeBoolN(xElement, attributeName);
            return value ?? defaultValue;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho Boolean? podobu
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        private static bool? _ReadAttributeBoolN(System.Xml.Linq.XElement xElement, string attributeName)
        {
            bool? value = null;
            if (xElement.HasAttributes && !String.IsNullOrEmpty(attributeName))
            {
                var xAttribute = xElement.Attribute(attributeName);
                if (xAttribute != null && !String.IsNullOrEmpty(xAttribute.Value))
                {
                    var booln = _ConvertTextToBoolN(xAttribute.Value);
                    if (booln.HasValue) return booln.Value;
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
        private static bool _ReadAttributeBoolX(System.Xml.Linq.XElement xElement, string attributeName, bool defaultValueNotExists = false, bool defaultValueNotValue = true)
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
                    // Atribut existuje, ale nemá rozpozonatelnou hodnotu (nebo nemá žádnou):
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
        private static TEnum _ReadAttributeEnum<TEnum>(System.Xml.Linq.XElement xElement, string attributeName, TEnum defaultValue, Func<string, string> modifier = null) where TEnum : struct
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
        private static TEnum? _ReadAttributeEnumN<TEnum>(System.Xml.Linq.XElement xElement, string attributeName, Func<string, string> modifier = null) where TEnum : struct
        {
            TEnum? value = null;
            if (xElement.HasAttributes && !String.IsNullOrEmpty(attributeName))
            {
                var xAttribute = xElement.Attribute(attributeName);
                if (xAttribute != null && !String.IsNullOrEmpty(xAttribute.Value))
                {
                    string text = xAttribute.Value;
                    if (modifier != null) text = modifier(text);
                    if (Enum.TryParse<TEnum>(text, true, out var result)) value = result;
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
        private static DfControlStyle _ReadAttributeStyle(System.Xml.Linq.XElement xElement, DfControlStyle defaultValue)
        {
            string styleName = _ReadAttributeString(xElement, "StyleName", null);
            if (!String.IsNullOrEmpty(styleName)) return new DfControlStyle() { StyleName = styleName };
            return defaultValue;
        }
        /// <summary>
        /// Z dodaného <paramref name="xElement"/> načte hodnoty odpovídající souřadnicím prvku <see cref="Bounds"/> a vrátí je.
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="defaultValue"></param>
        private static Bounds _ReadAttributeBounds(System.Xml.Linq.XElement xElement, Bounds defaultValue)
        {
            int? left, top, width, height;

            Bounds bounds = defaultValue;
            bool hasBounds = false;

            // Celý Bounds:
            var textBounds = _ReadAttributeString(xElement, "Bounds", null);
            if (!String.IsNullOrEmpty(textBounds))
            {
                var numbers = _SplitAndParseInt32N(textBounds);
                if (numbers != null && numbers.Count >= 1)
                {
                    int cnt = numbers.Count;
                    left = numbers[0];
                    top = (cnt >= 2 ? numbers[1] : null);
                    width = (cnt >= 3 ? numbers[2] : null);
                    height = (cnt >= 4 ? numbers[3] : null);
                    bounds = new Bounds(left, top, width, height);
                    hasBounds = true;
                    if (cnt >= 3) return bounds;           // Pokud byly zadány hodnoty alespoň X,Y,Width: tak už nebudeme řešit ani Size ani jednotlivé souřadnice.
                }
            }

            // Size:
            var textSize = _ReadAttributeString(xElement, "Size", null);
            if (!String.IsNullOrEmpty(textSize))
            {
                var numbers = _SplitAndParseInt32N(textSize);
                if (numbers != null && numbers.Count >= 1)
                {
                    int cnt = numbers.Count;
                    width = numbers[0];
                    height = (cnt >= 2 ? numbers[1] : null);
                    if (hasBounds)
                    {   // Pokud byl nalezen element Bounds s alespoň jedním prvkem (viz výše), tak zde doplním jeho Size a je hotovo:
                        bounds.Width = width;
                        bounds.Height = height;
                        return bounds;
                    }
                    // Element Bounds nemáme, máme jen Size:
                    bounds = new Bounds(null, null, width, height);
                    // Projdeme dál, možná najdeme X a Y?
                    hasBounds = true;
                }
            }

            // Jednotlivé hodnoty:
            left = _ReadAttributeInt32N(xElement, "X");
            top = _ReadAttributeInt32N(xElement, "Y");
            width = _ReadAttributeInt32N(xElement, "Width");
            height = _ReadAttributeInt32N(xElement, "Height");
            bool hasDimension = (left.HasValue || top.HasValue || width.HasValue || height.HasValue);
            if (!hasDimension) return bounds;              // Pokud nemáme žádnou jednotkovou dimenzi, pak vrátím bounds v současném stavu (může tam být defaultValue).

            // Máme nějakou dimenzi, musíme mít i cílovou instanci 'bounds', abychom měli nalezenou dimenzi kam dát:
            if (!hasBounds)
                bounds = new Bounds();

            if (left.HasValue) bounds.Left = left;
            if (top.HasValue) bounds.Top = top;
            if (width.HasValue) bounds.Width = width;
            if (height.HasValue) bounds.Height = height;

            return bounds;
        }
        /// <summary>
        /// Z dodaného <paramref name="xElement"/> načte hodnoty odpovídající souřadnicím <see cref="Location"/> ze zadaného prvku a vrátí je.
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        private static Location _ReadAttributesLocation(System.Xml.Linq.XElement xElement, string attributeName, Location defaultValue)
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
        private static Margins _ReadAttributesMargin(System.Xml.Linq.XElement xElement, string attributeName, Margins defaultValue)
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
        private static List<int> _SplitAndParseInt32(string text, string splitters = ";, ")
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

            // Rozdělím text nalezeným oddělovačem a převedu jednotlivé prvky na číslice:
            var items = (splitter != '\0') ? text.Split(splitter) : new string[] { text };      // Pokud jsem nenašel oddělovač, nebudu text rozdělovat a vezmu jej jako celek
            var result = new List<int>();
            foreach ( var item in items ) 
            {
                if (!String.IsNullOrEmpty(item) && Int32.TryParse(item.Trim().Replace(",", "."), out var number))
                    result.Add(number);
            }
            return (result.Count > 0 ? result : null);
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
        private static List<int?> _SplitAndParseInt32N(string text, string splitters = ";, ")
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

            // Rozdělím text nalezeným oddělovačem a převedu jednotlivé prvky na číslice:
            var items = (splitter != '\0') ? text.Split(splitter) : new string[] { text };      // Pokud jsem nenašel oddělovač, nebudu text rozdělovat a vezmu jej jako celek
            var result = new List<int?>();
            foreach (var item in items)
            {
                if (!String.IsNullOrEmpty(item) && Int32.TryParse(item.Trim().Replace(",", "."), out var number))
                    result.Add(number);
                else
                    result.Add(null);
            }
            return result;
        }
        /// <summary>
        /// Vrátí lokální jméno elementu, Trim(), ToLower().
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <returns></returns>
        private static string _GetValidElementName(System.Xml.Linq.XElement xElement)
        {
            return xElement?.Name.LocalName.Trim().ToLower() ?? "";
        }
        /// <summary>
        /// Klonuje dodané souřadnice
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static Bounds _CloneBounds(Bounds bounds)
        {
            return (bounds is null ? null : new Bounds(bounds.Left, bounds.Top, bounds.Width, bounds.Height));
        }
        /// <summary>
        /// Klonuje velikost z dodaných souřadnic
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static Bounds _CloneBoundsSize(Bounds bounds)
        {
            return (bounds is null ? null : new Bounds(null, null, bounds.Width, bounds.Height));
        }
        /// <summary>
        /// Klonuje pozici z dodaných souřadnic
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static Bounds _CloneBoundsLocation(Bounds bounds)
        {
            return (bounds is null ? null : new Bounds(bounds.Left, bounds.Top, null, null));
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
        #endregion
        #region class DfTemplateArgs
        /// <summary>
        /// Třída, zahrnující v sobě průběžná data pro načítání obsahu <see cref="DfForm"/> v metodách v <see cref="DfTemplateLoader"/>
        /// </summary>
        private class LoaderArgs
        {
            /// <summary>
            /// Funkce, která vrátí stringový obsah nested šablony daného jména.<br/>
            /// Pokud bude jako <see cref="NestedTemplateLoader"/> předána hodnota null, a v šabloně bude detekován Nested prvek, pak dojde k chybě.<br/>
            /// Loader bude volán s parametrem = jméno šablony (obsah atributu NestedTemplate), jeho úkolem je vrátit string = obsah požadované šablony (souboru).<br/>
            /// Pokud loader požadovanou šablonu (soubor) nenajde, může sám loader ohlásit chybu. Anebo může vrátit null, pak bude Nested prvek ignorován.
            /// </summary>
            public Func<string, string> NestedLoader { get; set; }
            /// <summary>
            /// Jméno vstupního souboru, typicky plné včetně cesty
            /// </summary>
            public string FileName { get; set; }
            /// <summary>
            /// Holé jméno vstupního souboru, bez adresáře a bez přípony
            /// </summary>
            public string Name { get { return (!String.IsNullOrEmpty(this.FileName) ? System.IO.Path.GetFileNameWithoutExtension(this.FileName) : ""); } }
            /// <summary>
            /// Máme načíst pouze atributy dokumentu, pro detekci jeho hlavičky (<see cref="DfForm.XmlNamespace"/> a <see cref="DfForm.FormatVersion"/>)
            /// </summary>
            public bool IsLoadOnlyDocumentAttributes { get; set; }

            /// <summary>
            /// Přidá chybu, nalezenou v parsovaném souboru.
            /// </summary>
            /// <param name="message"></param>
            public void AddError(string message)
            {
                if (!String.IsNullOrEmpty(message))
                {
                    __Errors ??= new StringBuilder();
                    __Errors.AppendLine(message);
                }
            }
            /// <summary>
            /// Souhrn chyb, nalezených v parsovaném souboru.
            /// </summary>
            public string Errors { get { return __Errors?.ToString(); } }
            /// <summary>
            /// Obsahuje true, pokud jsou zachyceny nějaké chyby.
            /// </summary>
            public bool HasErrors { get { return (__Errors != null && __Errors.Length > 0); } }
            /// <summary>
            /// Souhrn chyb, výchozí je null
            /// </summary>
            private StringBuilder __Errors;
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
        public System.Xml.Linq.XDocument TemplateDocument { get; set; }
        /// <summary>
        /// Funkce, která vrátí stringový obsah nested šablony daného jména.<br/>
        /// Pokud bude jako <see cref="NestedTemplateLoader"/> předána hodnota null, a v šabloně bude detekován Nested prvek, pak dojde k chybě.<br/>
        /// Loader bude volán s parametrem = jméno šablony (obsah atributu NestedTemplate), jeho úkolem je vrátit string = obsah požadované šablony (souboru).<br/>
        /// Pokud loader požadovanou šablonu (soubor) nenajde, může sám loader ohlásit chybu. Anebo může vrátit null, pak bude Nested prvek ignorován.
        /// </summary>
        public Func<string, string> NestedTemplateLoader { get; set; }
        /// <summary>
        /// Vygeneruje a vrátí argument pro nested šablonu: fyzicky jiný dokument, ale společná metoda <see cref="NestedTemplateLoader"/> a evidence chyb.
        /// </summary>
        /// <param name="nestedFile"></param>
        /// <param name="nestedContent"></param>
        /// <param name="nestedDocument"></param>
        /// <returns></returns>
        internal DfTemplateLoadArgs CreateNestedArgs(string nestedFile, string nestedContent, System.Xml.Linq.XDocument nestedDocument)
        {
            DfTemplateLoadArgs nestedArgs = new DfTemplateLoadArgs();
            nestedArgs.TemplateFileName = nestedFile;
            nestedArgs.TemplateContent = nestedContent;
            nestedArgs.TemplateDocument = nestedDocument;
            nestedArgs.NestedTemplateLoader = this.NestedTemplateLoader;
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
}