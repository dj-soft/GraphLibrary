// Supervisor: David Janáček, od 01.11.2023
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
            dfGroup.RowSpan = _ReadAttributeInt32N(xElement, "RowSpan");
            dfGroup.HPosition = _ReadAttributeEnumN<HPositionType>(xElement, "HPosition");
            dfGroup.VPosition = _ReadAttributeEnumN<VPositionType>(xElement, "VPosition");
            dfGroup.ExpandControl = _ReadAttributeEnumN<ExpandControlType>(xElement, "ExpandControl");
            dfGroup.Label = _ReadAttributeString(xElement, "Label", null);
            dfGroup.LabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "LabelPosition", _FixLabelPosition);
            dfGroup.LabelWidth = _ReadAttributeInt32N(xElement, "LabelWidth");

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
            dfNestedGroup.DesignBounds = _ReadAttributeBounds(xElement, null);
            dfNestedGroup.ParentBoundsName = _ReadAttributeString(xElement, "ParentBoundsName", null);
            dfNestedGroup.ColIndex = _ReadAttributeInt32N(xElement, "ColIndex");
            dfNestedGroup.ColSpan = _ReadAttributeInt32N(xElement, "ColSpan");
            dfNestedGroup.RowSpan = _ReadAttributeInt32N(xElement, "RowSpan");
            dfNestedGroup.HPosition = _ReadAttributeEnumN<HPositionType>(xElement, "HPosition");
            dfNestedGroup.VPosition = _ReadAttributeEnumN<VPositionType>(xElement, "VPosition");
            dfNestedGroup.ExpandControl = _ReadAttributeEnumN<ExpandControlType>(xElement, "ExpandControl");
            dfNestedGroup.Label = _ReadAttributeString(xElement, "Label", null);
            dfNestedGroup.LabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "LabelPosition", _FixLabelPosition);
            dfNestedGroup.LabelWidth = _ReadAttributeInt32N(xElement, "LabelWidth");

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
                dfGroup.DesignBounds = dfNestedGroup.DesignBounds;
                dfGroup.ParentBoundsName = dfNestedGroup.ParentBoundsName;
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
                case "placeholder":  return _FillControlPlaceHolder(xElement, new DfPlaceHolder(), args);
                case "label": return _FillControlLabel(xElement, new DfLabel(), args);
                case "title": return _FillControlTitle(xElement, new DfTitle(), args);
                case "checkbox": return _FillControlCheckBox(xElement, new DfCheckBox(), args);
                case "button": return _FillControlButton(xElement, new DfButton(), args);
                case "dropdownbutton": return _FillControlDropDownButton(xElement, new DfDropDownButton(), args);
                case "textbox": return _FillControlTextBox(xElement, new DfTextBox(), args);
                case "editbox": return _FillControlEditBox(xElement, new DfEditBox(), args);
                case "textboxbutton": return _FillControlTextBoxButton(xElement, new DfTextBoxButton(), args);
                case "combobox": return _FillControlComboBox(xElement, new DfComboBox(), args);
                case "image": return _FillControlImage(xElement, new DfImage(), args);
                case "stepprogress": return _FillControlStepProgress(xElement, new DfStepProgress(), args);

                // SubContainery = grupy:
                case "group": return _FillAreaGroup(xElement, null, args);
                case "nestedgroup": return _FillAreaNestedGroup(xElement, null, args);
            }
            args.AddError($"{sourceInfo} obsahuje prvek '{elementName}', který zde není očekáváván.");
            return null;
        }
        private static DfBaseControl _FillControlPlaceHolder(System.Xml.Linq.XElement xElement, DfPlaceHolder control, DfTemplateLoadArgs args)
        {
            _FillBaseAttributes(xElement, control);
            return control;
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
            control.IconPosition = _ReadAttributeEnumN<IconPositionType>(xElement, "IconPosition");
            return control;
        }
        private static DfBaseControl _FillControlDropDownButton(System.Xml.Linq.XElement xElement, DfDropDownButton control, DfTemplateLoadArgs args)
        {
            _FillControlButton(xElement, control, args);
            control.DropDownButtons = _LoadSubButtons(xElement, "dropDownButton", args);
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
            control.LeftButtons = _LoadSubButtons(xElement, "leftButton", args);
            control.RightButtons = _LoadSubButtons(xElement, "rightButton", args);
            return control;
        }
        private static DfBaseControl _FillControlComboBox(System.Xml.Linq.XElement xElement, DfComboBox control, DfTemplateLoadArgs args)
        {
            _FillBaseAttributes(xElement, control);
            control.EditStyleName = _ReadAttributeString(xElement, "EditStyleName", null);
            control.Style = _ReadAttributeEnumN<ComboBoxStyleType>(xElement, "Style");
            control.ComboItems = _LoadSubTextItems(xElement, "comboItem", args);
            return control;
        }
        private static DfBaseControl _FillControlImage(System.Xml.Linq.XElement xElement, DfImage control, DfTemplateLoadArgs args)
        {
            _FillBaseAttributes(xElement, control);
            control.ImageName = _ReadAttributeString(xElement, "ImageName", null);
            control.ImageData = _ReadAttributeBytes(xElement, "ImageData", null);
            return control;
        }
        private static DfBaseControl _FillControlStepProgress(System.Xml.Linq.XElement xElement, DfStepProgress control, DfTemplateLoadArgs args)
        {
            _FillBaseAttributes(xElement, control);
            control.EditStyleName = _ReadAttributeString(xElement, "EditStyleName", null);

            return control;
        }
        private static List<DfSubButton> _LoadSubButtons(System.Xml.Linq.XElement xElement, string loadElementName, DfTemplateLoadArgs args)
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
                        var subButton = _FillControlSubButton(xItem, new DfSubButton(), args);
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
        private static List<DfSubTextItem> _LoadSubTextItems(System.Xml.Linq.XElement xElement, string loadElementName, DfTemplateLoadArgs args)
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
                        var subTextItem = _FillControlSubTextItem(xItem, new DfSubTextItem(), args);
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
        /// <see cref="DfBaseInputTextControl"/>, <see cref="DfBaseLabeledInputControl"/>, <see cref="DfSubTextItem"/>, <see cref="DfSubButton"/>.<br/>
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
                control.DesignBounds = _ReadAttributeBounds(xElement, null);
                control.ParentBoundsName = _ReadAttributeString(xElement, "ParentBoundsName", null);
                control.ColIndex = _ReadAttributeInt32N(xElement, "ColIndex");
                control.ColSpan = _ReadAttributeInt32N(xElement, "ColSpan");
                control.RowSpan = _ReadAttributeInt32N(xElement, "RowSpan");
                control.HPosition = _ReadAttributeEnumN<HPositionType>(xElement, "HPosition");
                control.VPosition = _ReadAttributeEnumN<VPositionType>(xElement, "VPosition");
                control.ExpandControl = _ReadAttributeEnumN<ExpandControlType>(xElement, "ExpandControl"); 
            }
            if (target is DfBaseInputControl inputControl)
            {
                inputControl.Required = _ReadAttributeEnumN<RequiredType>(xElement, "Required");
                inputControl.ColumnName = _ReadAttributeString(xElement, "ColumnName", null);
                inputControl.TabIndex = _ReadAttributeInt32N(xElement, "TabIndex");
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
                labeledControl.LabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "LabelPosition", _FixLabelPosition);
                labeledControl.LabelWidth = _ReadAttributeInt32N(xElement, "LabelWidth");
                labeledControl.SuffixLabel = _ReadAttributeString(xElement, "SuffixLabel", null);
            }
            if (target is DfSubTextItem subTextItem)
            {
                subTextItem.Text = _ReadAttributeString(xElement, "Text", null);
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
        /// V daném elementu najde atribut daného jména a vrátí jeho byte[] podobu konvertovanou ze stringu pomocí Base64
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        private static byte[] _ReadAttributeBytes(System.Xml.Linq.XElement xElement, string attributeName, byte[] defaultValue)
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
        /// V daném elementu najde atribut daného jména a vrátí jeho Int32PP podobu
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="attributeName"></param>
        private static Int32P? _ReadAttributeInt32PN(System.Xml.Linq.XElement xElement, string attributeName)
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
                if (xAttribute != null)
                {
                    if (String.IsNullOrEmpty(xAttribute.Value))
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
        private static bool? _ReadAttributeBoolNX(System.Xml.Linq.XElement xElement, string attributeName, bool? defaultValueNotExists = null, bool? defaultValueNotValue = true)
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
        /// Z dodaného <paramref name="xElement"/> načte hodnoty odpovídající souřadnicím prvku <see cref="DesignBounds"/> a vrátí je.
        /// </summary>
        /// <param name="xElement">Element, v němž se má hledat zadaný atribut</param>
        /// <param name="defaultValue"></param>
        private static DesignBounds _ReadAttributeBounds(System.Xml.Linq.XElement xElement, DesignBounds defaultValue)
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
        private static string _GetValidElementName(System.Xml.Linq.XElement xElement)
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
        /// tak existuje řada formulářů (XML dokumentyú, které stále obsahují hodnotu "Up", kterou sice XML editor nyní podtrhává jako vadnou,
        /// ale musíme ji umět načíst => převést text z "Up" na "Top"...
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string _FixLabelPosition(string value)
        {
            string key = (value ?? "").Trim().ToLower();
            return key switch
            {   // Ze staré hodnoty na aktuální:
                "up" => "Top",
                _ => value,
            };
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