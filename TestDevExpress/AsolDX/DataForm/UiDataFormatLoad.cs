// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Noris.WS.DataContracts.DxForm;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    /// <summary>
    /// Třída, která načte XML soubor / stream obsahující <see cref="DfForm"/>, i rekurzivně (nested Tabs)
    /// </summary>
    internal static class DfTemplateLoader
    {
        #region Načítání obsahu a načítání Info - public rozhraní
        /// <summary>
        /// Načte a vrátí <see cref="DfForm"/> ze zadaného souboru
        /// </summary>
        /// <param name="fileName">Plný název souboru na disku, včetně adresáře a přípony</param>
        /// <param name="nestedLoader">Funkce, která vrátí stringový obsah nested šablony daného jména.<br/>
        /// Pokud bude jako <paramref name="nestedLoader"/> předána hodnota null, a v šabloně bude detekován Nested prvek, pak dojde k chybě.<br/>
        /// Loader bude volán s parametrem = jméno šablony (obsah atributu NestedTemplate), jeho úkolem je vrátit string = obsah požadované šablony (souboru).<br/>
        /// Pokud loader požadovanou šablonu (soubor) nenajde, může sám loader ohlásit chybu. Anebo může vrátit null, pak bude Nested prvek ignorován.</param>
        /// <param name="logTime">Logovat časy?</param>
        /// <returns></returns>
        internal static DfForm LoadFromFile(string fileName, Func<string, string> nestedLoader = null, bool logTime = false)
        {
            LoaderContext loaderContext = new LoaderContext() { NestedLoader = nestedLoader, FileName = fileName };
            string name = System.IO.Path.GetFileName(fileName);

            var startTime1 = DxComponent.LogTimeCurrent;
            string content = System.IO.File.ReadAllText(fileName);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load 'Content' from file '{name}': {DxComponent.LogTokenTimeMicrosec}", startTime1);

            var startTime2 = DxComponent.LogTimeCurrent;
            var xDocument = System.Xml.Linq.XDocument.Parse(content);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Parse 'XDocument' from 'Content' ({content.Length} B): {DxComponent.LogTokenTimeMicrosec}", startTime2);

            var startTime3 = DxComponent.LogTimeCurrent;
            var form = _LoadFromDocument(xDocument, loaderContext);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load 'DataFormatContainerForm' from 'XDocument': {DxComponent.LogTokenTimeMicrosec}", startTime3);

            return form;
        }
        /// <summary>
        /// Načte a vrátí <see cref="DfForm"/> ze zadané XML definice (=typicky obsah souboru)
        /// </summary>
        /// <param name="content">Obsah souboru šablony předaný jako string</param>
        /// <param name="nestedLoader">Funkce, která vrátí stringový obsah nested šablony daného jména.<br/>
        /// Pokud bude jako <paramref name="nestedLoader"/> předána hodnota null, a v šabloně bude detekován Nested prvek, pak dojde k chybě.<br/>
        /// Loader bude volán s parametrem = jméno šablony (obsah atributu NestedTemplate), jeho úkolem je vrátit string = obsah požadované šablony (souboru).<br/>
        /// Pokud loader požadovanou šablonu (soubor) nenajde, může sám loader ohlásit chybu. Anebo může vrátit null, pak bude Nested prvek ignorován.</param>
        /// <param name="logTime">Logovat časy?</param>
        /// <param name="fileName">Jméno souboru, pouze do atributu <see cref="DfForm.FileName"/> a pro chybové hlášky</param>
        /// <returns></returns>
        internal static DfForm LoadFromContent(string content, Func<string, string> nestedLoader = null, bool logTime = false, string fileName = null)
        {
            LoaderContext loaderContext = new LoaderContext() { NestedLoader = nestedLoader, FileName = fileName };

            var startTime2 = DxComponent.LogTimeCurrent;
            var xDocument = System.Xml.Linq.XDocument.Parse(content);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Parse 'XDocument' from 'Content' ({content.Length} B): {DxComponent.LogTokenTimeMicrosec}", startTime2);

            var startTime3 = DxComponent.LogTimeCurrent;
            var form = _LoadFromDocument(xDocument, loaderContext);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load 'DataFormatContainerForm' from 'XDocument': {DxComponent.LogTokenTimeMicrosec}", startTime3);

            return form;
        }
        /// <summary>
        /// Načte a vrátí <see cref="DfForm"/> z dodaného <see cref="System.Xml.Linq.XDocument"/>
        /// </summary>
        /// <param name="xDocument">Parsovaný XML dokument</param>
        /// <param name="nestedLoader">Funkce, která vrátí stringový obsah nested šablony daného jména.<br/>
        /// Pokud bude jako <paramref name="nestedLoader"/> předána hodnota null, a v šabloně bude detekován Nested prvek, pak dojde k chybě.<br/>
        /// Loader bude volán s parametrem = jméno šablony (obsah atributu NestedTemplate), jeho úkolem je vrátit string = obsah požadované šablony (souboru).<br/>
        /// Pokud loader požadovanou šablonu (soubor) nenajde, může sám loader ohlásit chybu. Anebo může vrátit null, pak bude Nested prvek ignorován.</param>
        /// <param name="logTime">Logovat časy?</param>
        /// <param name="fileName">Jméno souboru, pouze do atributu <see cref="DfForm.FileName"/> a pro chybové hlášky</param>
        /// <returns></returns>
        internal static DfForm LoadFromDocument(System.Xml.Linq.XDocument xDocument, Func<string, string> nestedLoader = null, bool logTime = false, string fileName = null)
        {
            LoaderContext loaderContext = new LoaderContext() { NestedLoader = nestedLoader, FileName = fileName };

            var startTime3 = DxComponent.LogTimeCurrent;
            var form = _LoadFromDocument(xDocument, loaderContext);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load 'DataFormatContainerForm' from 'XDocument': {DxComponent.LogTokenTimeMicrosec}", startTime3);

            return form;
        }

        /// <summary>
        /// Načte a vrátí instanci třídy <see cref="DfInfoForm"/> ze zadaného souboru - je to velice rychlá cesta pro načtení základních atributů 
        /// <see cref="DfForm.XmlNamespace"/> a <see cref="DfForm.FormatVersion"/>.
        /// </summary>
        /// <param name="fileName">Plný název souboru na disku, včetně adresáře a přípony</param>
        /// <param name="logTime">Logovat časy?</param>
        /// <returns></returns>
        internal static DfInfoForm LoadInfoFromFile(string fileName, bool logTime = false)
        {
            return LoadInfoFromFile(fileName, out var _, logTime);
        }
        /// <summary>
        /// Načte a vrátí <see cref="DfInfoForm"/> ze zadaného souboru - je to velice rychlá cesta pro načtení základních atributů 
        /// <see cref="DfForm.XmlNamespace"/> a <see cref="DfForm.FormatVersion"/>.
        /// </summary>
        /// <param name="fileName">Plný název souboru na disku, včetně adresáře a přípony</param>
        /// <param name="xDocument">Out načtený dokument, usnadní budoucí načítání plného <see cref="DfForm"/> v metodě <see cref="LoadFromDocument(System.Xml.Linq.XDocument, Func{string, string}, bool, string)"/></param>
        /// <param name="logTime">Logovat časy?</param>
        /// <returns></returns>
        internal static DfInfoForm LoadInfoFromFile(string fileName, out System.Xml.Linq.XDocument xDocument, bool logTime = false)
        {
            LoaderContext loaderContext = new LoaderContext() { IsLoadOnlyDocumentAttributes = true, FileName = fileName };
            string name = System.IO.Path.GetFileName(fileName);

            var startTime1 = DxComponent.LogTimeCurrent;
            string content = System.IO.File.ReadAllText(fileName);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load 'Content' from file '{name}': {DxComponent.LogTokenTimeMicrosec}", startTime1);

            var startTime2 = DxComponent.LogTimeCurrent;
            xDocument = System.Xml.Linq.XDocument.Parse(content);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Parse 'XDocument' from 'Content' ({content.Length} B): {DxComponent.LogTokenTimeMicrosec}", startTime2);

            var startTime3 = DxComponent.LogTimeCurrent;
            var form = _LoadFromDocument(xDocument, loaderContext);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load 'DataFormatInfoForm' from 'XDocument': {DxComponent.LogTokenTimeMicrosec}", startTime3);

            return _CreateInfoForm(form);
        }
        /// <summary>
        /// Načte a vrátí <see cref="DfInfoForm"/> ze zadané XML definice (=typicky obsah souboru) - je to velice rychlá cesta pro načtení základních atributů 
        /// <see cref="DfForm.XmlNamespace"/> a <see cref="DfForm.FormatVersion"/>.
        /// </summary>
        /// <param name="content">Obsah souboru šablony předaný jako string</param>
        /// <param name="logTime">Logovat časy?</param>
        /// <param name="fileName">Jméno souboru, pouze do atributu <see cref="DfForm.FileName"/> a pro chybové hlášky</param>
        /// <returns></returns>
        internal static DfInfoForm LoadInfoFromContent(string content, bool logTime = false, string fileName = null)
        {
            return LoadInfoFromContent(content, out var _, logTime, fileName);
        }
        /// <summary>
        /// Načte a vrátí <see cref="DfInfoForm"/> ze zadané XML definice (=typicky obsah souboru) - je to velice rychlá cesta pro načtení základních atributů 
        /// <see cref="DfForm.XmlNamespace"/> a <see cref="DfForm.FormatVersion"/>.
        /// </summary>
        /// <param name="content">Obsah souboru šablony předaný jako string</param>
        /// <param name="xDocument">Out načtený dokument, usnadní budoucí načítání plného <see cref="DfForm"/> v metodě <see cref="LoadFromDocument(System.Xml.Linq.XDocument, Func{string, string}, bool, string)"/></param>
        /// <param name="logTime">Logovat časy?</param>
        /// <param name="fileName">Jméno souboru, pouze do atributu <see cref="DfForm.FileName"/> a pro chybové hlášky</param>
        /// <returns></returns>
        internal static DfInfoForm LoadInfoFromContent(string content, out System.Xml.Linq.XDocument xDocument, bool logTime = false, string fileName = null)
        {
            LoaderContext loaderContext = new LoaderContext() { IsLoadOnlyDocumentAttributes = true, FileName = fileName };

            var startTime2 = DxComponent.LogTimeCurrent;
            xDocument = System.Xml.Linq.XDocument.Parse(content);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Parse 'XDocument' from 'Content' ({content.Length} B): {DxComponent.LogTokenTimeMicrosec}", startTime2);

            var startTime3 = DxComponent.LogTimeCurrent;
            var form = _LoadFromDocument(xDocument, loaderContext);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load 'DataFormatInfoForm' from 'XDocument': {DxComponent.LogTokenTimeMicrosec}", startTime3);

            return _CreateInfoForm(form);
        }
        /// <summary>
        /// Načte a vrátí <see cref="DfInfoForm"/> z dodaného <see cref="System.Xml.Linq.XDocument"/> - je to velice rychlá cesta pro načtení základních atributů 
        /// <see cref="DfForm.XmlNamespace"/> a <see cref="DfForm.FormatVersion"/>.
        /// </summary>
        /// <param name="xDocument">Parsovaný XML dokument</param>
        /// <param name="logTime">Logovat časy?</param>
        /// <param name="fileName">Jméno souboru, pouze do atributu <see cref="DfForm.FileName"/> a pro chybové hlášky</param>
        /// <returns></returns>
        internal static DfInfoForm LoadInfoFromDocument(System.Xml.Linq.XDocument xDocument, bool logTime = false, string fileName = null)
        {
            LoaderContext loaderContext = new LoaderContext() { IsLoadOnlyDocumentAttributes = true, FileName = fileName };

            var startTime3 = DxComponent.LogTimeCurrent;
            var form = _LoadFromDocument(xDocument, loaderContext);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load 'DataFormatInfoForm' from 'XDocument': {DxComponent.LogTokenTimeMicrosec}", startTime3);

            return _CreateInfoForm(form);
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
        #region Načítání obsahu - private tvorba containerů

        // Každá zdejší větev / metoda načte pouze property deklarované přímo pro danou třídu, nikoli pro její předky!
        // Třídy předků načítá metoda _FillBaseAttributes(xElement, control);

        /// <summary>
        /// Načte a vrátí <see cref="DfForm"/> z dodaného <see cref="System.Xml.Linq.XDocument"/>
        /// </summary>
        /// <param name="xDocument"></param>
        /// <param name="loaderContext">Průběžná data pro načítání obsahu</param>
        /// <returns></returns>
        private static DfForm _LoadFromDocument(System.Xml.Linq.XDocument xDocument, LoaderContext loaderContext)
        {
            return _FillContainerForm(xDocument.Root, null, loaderContext);
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající Form, včetně jeho obsahu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="control"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DfForm _FillContainerForm(System.Xml.Linq.XElement xElement, DfForm control, LoaderContext loaderContext)
        {
            if (control is null) control = new DfForm();

            // Atributy:
            _FillBaseAttributes(xElement, control);
            control.XmlNamespace = _ReadAttributeString(xElement, "xmlns", null);
            control.FormatVersion = _ReadAttributeEnum(xElement, "FormatVersion", FormatVersionType.Default, t => "Version" + t);

            // Rychlá odbočka?
            if (loaderContext.IsLoadOnlyDocumentAttributes) return control;

            // FileName (a Name, pokud není explicitně načteno) podle jména souboru:
            control.FileName = loaderContext.FileName;
            if (control.Name is null) control.Name = System.IO.Path.GetFileNameWithoutExtension(loaderContext.FileName ?? "");

            // Full Load:
            control.MasterWidth = _ReadAttributeInt32N(xElement, "MasterWidth");
            control.MasterHeight = _ReadAttributeInt32N(xElement, "MasterHeight");
            control.TotalWidth = _ReadAttributeInt32N(xElement, "TotalWidth");
            control.TotalHeight = _ReadAttributeInt32N(xElement, "TotalHeight");
            control.AutoLabelPosition = _ReadAttributeEnum(xElement, "AutoLabelPosition", LabelPositionType.None);
            control.DataSource = _ReadAttributeString(xElement, "DataSource", null);
            control.Messages = _ReadAttributeString(xElement, "Messages", null);
            control.UseNorisClass = _ReadAttributeInt32N(xElement, "UseNorisClass");
            control.AddUda = _ReadAttributeBoolN(xElement, "AddUda");
            control.UdaLabelPosition = _ReadAttributeEnum(xElement, "UdaLabelPosition", LabelPositionType.Up);
            control.Margins = _ReadAttributesMargin(xElement, "Margins", null);
            control.ContextMenu = _ReadAttributeBoolN(xElement, "ContextMenu");
            control.ColumnWidths = _ReadAttributeString(xElement, "ColumnWidths", null);

            // Implicit Page: do ní se vkládají Panely, pokud jsou zadány přímo do Formu
            DfPage implicitPage = null;

            // Elementy = stránky (nebo přímo panely):
            var xContainers = xElement.Elements();
            foreach (var xContainer in xContainers)
            {
                var container = _CreateArea(xContainer, loaderContext, "page", "panel", "nestedpanel");
                if (container != null)
                {
                    if (control.Pages is null) control.Pages = new List<DfPage>();
                    switch (container)
                    {
                        case DfPage page:
                            control.Pages.Add(page); 
                            break;
                        case DfPanel panel:
                            // Pokud je v rámci DfForm (=template) zadán přímo Panel (pro jednoduchost to umožníme), 
                            //  pak jej vložím do implicitPage = ta bude první v seznamu stránek:
                            if (implicitPage is null) implicitPage = createImplicitPage();
                            implicitPage.Panels.Add(panel);
                            break;
                        default:
                            loaderContext.AddError($"Formulář '{loaderContext.FileName}' obsahuje element '{xContainer.Name}', který zde není očekáváván.");
                            break;
                    }
                }
                else
                {
                    loaderContext.AddError($"Formulář '{loaderContext.FileName}' obsahuje element '{xContainer.Name}', který zde není očekáváván.");
                }
            }
            return control;


            // Vytvoří new instanci DfPage jako "Implicitní stránku", přidá ji jako stránku [0] do control.Pages a vrátí ji
            DfPage createImplicitPage()
            {
                DfPage iPage = new DfPage();
                iPage.Name = Guid.NewGuid().ToString();
                iPage.Panels = new List<DfPanel>();
                if (control.Pages.Count == 0)
                    control.Pages.Add(iPage);
                else
                    control.Pages.Insert(0, iPage);
                return iPage;
            }
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající kontejner (page, panel, nestedpanel, group, nestedgroup), včetně jeho obsahua child prvků.
        /// Výstupem je tedy buď <see cref="DfPage"/> nebo <see cref="DfPanel"/> (nebo null).
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="loaderContext">Průběžná data pro načítání obsahu</param>
        /// <param name="validNames">Očekávaná validní jména elementů. Pokud je zadáno, a je detekován jiný než daný element, vrátí se null.</param>
        private static DfBaseArea _CreateArea(System.Xml.Linq.XElement xElement, LoaderContext loaderContext, params string[] validNames)
        {
            string elementName = _GetValidElementName(xElement);               // page, panel, nestedpanel, group, nestedgroup
            if (String.IsNullOrEmpty(elementName)) return null;                // Nezadáno (?)
            // Pokud je dodán seznam validních jmen elementů (přinejmenším 1 prvek), ale aktuální element neodpovídá žádnému povolenému jménu, pak skončím:
            if (validNames != null && validNames.Length > 0 && !validNames.Any(v => String.Equals(v, elementName, StringComparison.OrdinalIgnoreCase))) return null;
            switch (elementName)
            {
                case "page":
                    return _FillAreaPage(xElement, null, loaderContext);
                case "panel":
                    return _FillAreaPanel(xElement, null, loaderContext);
                case "nestedpanel":
                    return _FillAreaNestedPanel(xElement, null, loaderContext);
                case "group":
                    return _FillAreaGroup(xElement, null, loaderContext);
                case "nestedgroup":
                    return _FillAreaNestedGroup(xElement, null, loaderContext);
            }
            return null;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <c>page</c>, včetně jeho obsahu (tj. atributy a child elementy).
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="dfPage"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DfPage _FillAreaPage(System.Xml.Linq.XElement xElement, DfPage dfPage, LoaderContext loaderContext)
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
                foreach (var xPanel in xPanels)
                {
                    var container = _CreateArea(xPanel, loaderContext, "panel", "nestedpanel");
                    if (container != null && container is DfPanel panel)
                    {
                        if (dfPage.Panels is null) dfPage.Panels = new List<DfPanel>();
                        dfPage.Panels.Add(panel);
                    }
                    else
                    {
                        loaderContext.AddError($"Stránka '{dfPage.Name}' obsahuje element '{xPanel.Name}', který zde není očekáváván.");
                    }
                }
            }

            return dfPage;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <c>panel</c>, včetně jeho obsahu (tj. atributy a child elementy).
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="dfPanel"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DfPanel _FillAreaPanel(System.Xml.Linq.XElement xElement, DfPanel dfPanel, LoaderContext loaderContext)
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
            _FillContainerChildElements(xElement, dfPanel, loaderContext);

            return dfPanel;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <c>nestedpanel</c>, včetně jeho obsahu (tj. atributy a child elementy).
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="dfPanelVoid"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DfPanel _FillAreaNestedPanel(System.Xml.Linq.XElement xElement, DfPanel dfPanelVoid, LoaderContext loaderContext)
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
            if (!_TryLoadNestedTemplate(dfNestedPanel.NestedTemplate, loaderContext, out DfForm dfNestedForm, $"NestedPanel '{dfNestedPanel.Name}'")) return null;

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
        /// <param name="xElement"></param>
        /// <param name="dfGroup"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DfGroup _FillAreaGroup(System.Xml.Linq.XElement xElement, DfGroup dfGroup, LoaderContext loaderContext)
        {
            // Výsledná instance:
            if (dfGroup is null) dfGroup = new DfGroup();

            // Atributy:
            _FillBaseAttributes(xElement, dfGroup);
            // Grupa nemá vlastní specifické atributy.

            // Elementy = Controly + Panely:
            _FillContainerChildElements(xElement, dfGroup, loaderContext);

            return dfGroup;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <c>nestedgroup</c>, včetně jeho obsahu (tj. atributy a child elementy).
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="dfGroupVoid"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DfGroup _FillAreaNestedGroup(System.Xml.Linq.XElement xElement, DfGroup dfGroupVoid, LoaderContext loaderContext)
        {
            // Instance DfNestedGroup slouží k načtení definice z aktuálního formuláře, ale nejde o výslednou instanci:
            DfNestedGroup dfNestedGroup = new DfNestedGroup();

            // Atributy:
            _FillBaseAttributes(xElement, dfNestedGroup);
            dfNestedGroup.NestedTemplate = _ReadAttributeString(xElement, "NestedTemplate", null);
            dfNestedGroup.NestedGroupName = _ReadAttributeString(xElement, "NestedPanelName", null);
            dfNestedGroup.Bounds = _ReadAttributeBounds(xElement, null);

            // Nested šablona:
            if (!_TryLoadNestedTemplate(dfNestedGroup.NestedTemplate, loaderContext, out DfForm dfNestedForm, $"NestedGroup '{dfNestedGroup.Name}'")) return null;

            // Najde v načtené šabloně první panel DfPanel [kterýkoli nebo daného jména]:
            if (trySearchForGroup(dfNestedForm, dfNestedGroup.NestedGroupName, out DfGroup dfGroup))
            {   // Převezmeme něco z naší DfNestedGroup do výstupní DfGroup:
                dfGroup.Name = dfNestedGroup.Name;

                if (dfNestedGroup.State.HasValue) dfGroup.State = dfNestedGroup.State;
                if (dfNestedGroup.ToolTipTitle != null) dfGroup.ToolTipTitle = dfNestedGroup.ToolTipTitle;
                if (dfNestedGroup.ToolTipText != null) dfGroup.ToolTipText = dfNestedGroup.ToolTipText;
                if (dfNestedGroup.Invisible != null) dfGroup.Invisible = dfNestedGroup.Invisible;
                dfGroup.Bounds = dfNestedGroup.Bounds;
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
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající <c>group</c>. Načítá pouze atribut, ale ne elementy.
        /// Složí jak pro načtení standardní grupy, tak i pro deklaraci NestedGroup.
        /// Pro NestedGroup se následně nenačítají jeho XElementy, ale načte se obsah Nested šablony (externě) a vloží se do jeho elementů.
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="control"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DfGroup _FillAreaGroupAttributes(System.Xml.Linq.XElement xElement, DfGroup control, LoaderContext loaderContext)
        {
            // Výsledná instance:
            if (control is null) control = new DfGroup();

            // Atributy:
            _FillBaseAttributes(xElement, control);
            // Grupa nemá vlastní atributy.

            return control;
        }
        /// <summary>
        /// Z dat dodaného XElementu <paramref name="xElement"/> načte jeho Child elementy a vytvoří z nich Child controly které vloží do dodaného containeru <paramref name="control"/>.
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="control"></param>
        /// <param name="loaderContext"></param>
        private static void _FillContainerChildElements(System.Xml.Linq.XElement xElement, DfBaseContainer control, LoaderContext loaderContext)
        {
            var xChilds = xElement.Elements();
            if (xChilds != null)
            {
                foreach (var xChild in xChilds)
                {
                    var child = _CreateChildItem(xChild, loaderContext);
                    if (child != null && (child is DfBaseControl || child is DfGroup))
                    {   // Pouze Controly + Group
                        if (control.Childs is null) control.Childs = new List<DfBase>();
                        control.Childs.Add(child);
                    }
                    else
                    {
                        loaderContext.AddError($"Container {control.GetType().Name} '{control.Name}' obsahuje element '{xChild.Name}', který zde není očekáváván.");
                    }
                }
            }
        }
        /// <summary>
        /// Metoda zajistí načtení instance <see cref="DfForm"/> pro danou <paramref name="dfNestedForm"/>, s pomocí loader v <paramref name="loaderContext"/>.
        /// </summary>
        /// <param name="nestedTemplate"></param>
        /// <param name="loaderContext"></param>
        /// <param name="dfNestedForm"></param>
        /// <param name="sourceInfo"></param>
        /// <returns></returns>
        private static bool _TryLoadNestedTemplate(string nestedTemplate, LoaderContext loaderContext, out DfForm dfNestedForm, string sourceInfo)
        {
            dfNestedForm = null;

            // Jméno nested šablony:
            if (String.IsNullOrEmpty(nestedTemplate))
            {
                loaderContext.AddError($"{sourceInfo} nemá zadanou šablonu 'NestedTemplate'.");
                return false;
            }

            // Obsah nested panelu získám s pomocí dodaného loaderu :
            if (loaderContext.NestedLoader is null)
            {
                loaderContext.AddError($"{sourceInfo} má zadanou šablonu 'NestedTemplate', ale není dodána metoda Loader, která by načetla její obsah. Není možno načíst obsah šablony.");
                return false;
            }
            string nestedContent = loaderContext.NestedLoader(nestedTemplate);
            if (String.IsNullOrEmpty(nestedContent))
            {   // Prázdný obsah: pokud to loader vrátí, pak OK, je to legální cesta, jak zrušit Nested obsah:
                return false;
            }

            // Ze stringu z obsahu Nested šablony získám celou šablonu:
            var xNestedDocument = System.Xml.Linq.XDocument.Parse(nestedContent);
            dfNestedForm = _LoadFromDocument(xNestedDocument, loaderContext);
            return (dfNestedForm != null);
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
            ScanResultType result = scanner(dfForm);
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
        #region Načítání obsahu - private tvorba jednotlivých controlů

        // Každá zdejší větev / metoda načte pouze property deklarované přímo pro danou třídu, nikoli pro její předky!

        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající Item, včetně jeho obsahu.
        /// Může to být některý Control, anebo Panel (včetně NestedPanel).
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DfBase _CreateChildItem(System.Xml.Linq.XElement xElement, LoaderContext loaderContext)
        {
            string elementName = xElement?.Name.LocalName.ToLower();          // label, textbox, textbox_button, button, combobox, ...,   !page, panel, nestedpanel,
            switch (elementName)
            {
                // Controly:
                case "label": return _FillControlLabel(xElement, new DfLabel(), loaderContext);
                case "title": return _FillControlTitle(xElement, new DfTitle(), loaderContext);
                case "checkbox": return _FillControlCheckBox(xElement, new DfCheckBox(), loaderContext);
                case "button": return _FillControlButton(xElement, new DfButton(), loaderContext);
                case "dropdownbutton": return _FillControlDropDownButton(xElement, new DfDropDownButton(), loaderContext);
                case "textbox": return _FillControlTextBox(xElement, new DfTextBox(), loaderContext);
                case "textboxbutton": return _FillControlTextBoxButton(xElement, new DfTextBoxButton(), loaderContext);
                case "combobox": return _FillControlComboBox(xElement, new DfComboBox(), loaderContext);

                // SubContainery = grupy:
                case "group": return _FillAreaGroup(xElement, null, loaderContext);
                case "nestedgroup": return _FillAreaNestedGroup(xElement, null, loaderContext);
            }
            return null;
        }
        private static DfBaseControl _FillControlLabel(System.Xml.Linq.XElement xElement, DfLabel control, LoaderContext loaderContext)
        {
            _FillBaseAttributes(xElement, control);
            control.Text = _ReadAttributeString(xElement, "Text", null);
            control.Alignment = _ReadAttributeEnum(xElement, "Alignment", ContentAlignmentType.Default);
            return control;
        }
        private static DfBaseControl _FillControlTitle(System.Xml.Linq.XElement xElement, DfTitle control, LoaderContext loaderContext)
        {
            _FillBaseAttributes(xElement, control);
            control.IconName = _ReadAttributeString(xElement, "IconName", null);
            control.Title = _ReadAttributeString(xElement, "Title", null);
            control.Style = _ReadAttributeEnum(xElement, "Style", TitleStyleType.Default);
            return control;
        }
        private static DfBaseControl _FillControlCheckBox(System.Xml.Linq.XElement xElement, DfCheckBox control, LoaderContext loaderContext)
        {
            _FillBaseAttributes(xElement, control);
            control.Style = _ReadAttributeEnum(xElement, "Style", CheckBoxStyleType.Default);
            return control;
        }
        private static DfBaseControl _FillControlButton(System.Xml.Linq.XElement xElement, DfButton control, LoaderContext loaderContext)
        {
            _FillBaseAttributes(xElement, control);
            control.ActionType = _ReadAttributeEnum(xElement, "ActionType", ButtonActionType.Default);
            control.ActionData = _ReadAttributeString(xElement, "ActionData", null);
            control.HotKey = _ReadAttributeString(xElement, "HotKey", null);
            return control;
        }
        private static DfBaseControl _FillControlDropDownButton(System.Xml.Linq.XElement xElement, DfDropDownButton control, LoaderContext loaderContext)
        {
            _FillControlButton(xElement, control, loaderContext);

            // Elementy = Items:
            var xItems = xElement.Elements();
            if (xItems != null)
            {
                foreach (var xItem in xItems)
                {
                    string elementName = xItem?.Name.LocalName;
                    if (String.Equals(elementName, "dropDownButton", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var subButton = _FillControlSubButton(xItem, new DfSubButton(), loaderContext);
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
        private static DfBaseControl _FillControlTextBox(System.Xml.Linq.XElement xElement, DfTextBox control, LoaderContext loaderContext)
        {
            _FillBaseAttributes(xElement, control);
            control.EditMask = _ReadAttributeString(xElement, "EditMask", null);
            control.Alignment = _ReadAttributeEnum(xElement, "Alignment", ContentAlignmentType.Default);
            return control;
        }
        private static DfBaseControl _FillControlTextBoxButton(System.Xml.Linq.XElement xElement, DfTextBoxButton control, LoaderContext loaderContext)
        {
            _FillControlTextBox(xElement, control, loaderContext);
            control.ButtonsVisibility = _ReadAttributeEnum(xElement, "ButtonsVisibility", ButtonsVisibilityType.Default);

            // Elementy = SubButtons:
            var xItems = xElement.Elements();
            if (xItems != null)
            {
                foreach (var xItem in xItems)
                {
                    string elementName = xItem?.Name.LocalName;
                    if (String.Equals(elementName, "leftButton", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var subButton = _FillControlSubButton(xItem, new DfSubButton(), loaderContext);
                        if (subButton != null)
                        {
                            if (control.LeftButtons is null)
                                control.LeftButtons = new List<DfSubButton>();
                            control.LeftButtons.Add(subButton);
                        }
                    }
                    else if (String.Equals(elementName, "rightButton", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var subButton = _FillControlSubButton(xItem, new DfSubButton(), loaderContext);
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
        private static DfBaseControl _FillControlComboBox(System.Xml.Linq.XElement xElement, DfComboBox control, LoaderContext loaderContext)
        {
            _FillBaseAttributes(xElement, control);
            control.EditStyleName = _ReadAttributeString(xElement, "EditStyleName", null);
            control.Style = _ReadAttributeEnum(xElement, "Style", ComboBoxStyleType.Default);

            // Elementy = SubButtons:
            var xItems = xElement.Elements();
            if (xItems != null)
            {
                foreach (var xItem in xItems)
                {
                    string elementName = xItem?.Name.LocalName;
                    if (String.Equals(elementName, "comboItem", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var comboItem = _FillControlSubTextItem(xItem, new DfSubTextItem(), loaderContext);
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
        private static DfSubButton _FillControlSubButton(System.Xml.Linq.XElement xElement, DfSubButton control, LoaderContext loaderContext)
        {
            _FillControlSubTextItem(xElement, control, loaderContext);
            control.ActionType = _ReadAttributeEnum(xElement, "ActionType", SubButtonActionType.Default);
            control.ActionData = _ReadAttributeString(xElement, "ActionData", null);
            return control;
        }
        private static DfSubTextItem _FillControlSubTextItem(System.Xml.Linq.XElement xElement, DfSubTextItem control, LoaderContext loaderContext)
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
        /// <param name="xElement"></param>
        /// <param name="target"></param>
        private static void _FillBaseAttributes(System.Xml.Linq.XElement xElement, DfBase target)
        {
            // Každá zdejší větev / metoda načte pouze property deklarované přímo pro danou třídu, nikoli pro její předky!

            // DfBase:
            target.Name = _ReadAttributeString(xElement, "Name", null);
            target.State = _ReadAttributeEnumN<ControlStateType>(xElement, "State");
            target.ToolTipTitle = _ReadAttributeString(xElement, "ToolTipTitle", null);
            target.ToolTipText = _ReadAttributeString(xElement, "ToolTipText", null);
            target.Invisible = _ReadAttributeString(xElement, "Invisible", null);

            // Potomci směrem k Controlům:
            if (target is DfBaseControl control)
            {
                control.Bounds = _ReadAttributeBounds(xElement, null);
            }
            if (target is DfBaseInputControl inputControl)
            {
                inputControl.Required = _ReadAttributeEnum(xElement, "Required", RequiredType.Default);
            }
            if (target is DfBaseInputTextControl textControl)
            {
                textControl.Text = _ReadAttributeString(xElement, "Text", null);
                textControl.IconName = _ReadAttributeString(xElement, "IconName", null);
                textControl.Alignment = _ReadAttributeEnum(xElement, "Alignment", ContentAlignmentType.Default);
            }
            if (target is DfBaseLabeledInputControl labeledControl)
            {
                labeledControl.Label = _ReadAttributeString(xElement, "Label", null);
                labeledControl.LabelPosition = _ReadAttributeEnum(xElement, "LabelPosition", LabelPositionType.Default);
                labeledControl.LabelWidth = _ReadAttributeInt32N(xElement, "LabelWidth");
            }

            // Potomci směrem k Containerům:
            if (target is DfBaseArea area)
            {
                area.BackColorName = _ReadAttributeString(xElement, "BackColorName", null);
                area.BackColorLight = _ReadAttributeColorN(xElement, "BackColorLight");
                area.BackColorDark = _ReadAttributeColorN(xElement, "BackColorDark");
                area.BackImageName = _ReadAttributeString(xElement, "BackImageName", null);
                area.BackImagePosition = _ReadAttributeEnum(xElement, "BackImagePosition", BackImagePositionType.Default);
                area.Margins = _ReadAttributesMargin(xElement, "Margins", null);
                area.ContextMenu = _ReadAttributeBoolN(xElement, "ContextMenu");
                area.ColumnWidths = _ReadAttributeString(xElement, "ColumnWidths", null);
                area.AutoLabelPosition = _ReadAttributeEnumN<LabelPositionType>(xElement, "AutoLabelPosition");
            }
            if (target is DfBaseContainer container)
            {
                container.Bounds = _ReadAttributeBounds(xElement, null);
            }
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho String podobu
        /// </summary>
        /// <param name="xElement"></param>
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
        /// V daném elementu najde atribut daného jména a vrátí jeho Int32 podobu
        /// </summary>
        /// <param name="xElement"></param>
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
        /// <param name="xElement"></param>
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
        /// <param name="xElement"></param>
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
        /// <param name="xElement"></param>
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
        /// <param name="xElement"></param>
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
        /// <param name="xElement"></param>
        /// <param name="attributeName"></param>
        private static bool? _ReadAttributeBoolN(System.Xml.Linq.XElement xElement, string attributeName)
        {
            bool? value = null;
            if (xElement.HasAttributes && !String.IsNullOrEmpty(attributeName))
            {
                var xAttribute = xElement.Attribute(attributeName);
                if (xAttribute != null && !String.IsNullOrEmpty(xAttribute.Value))
                {
                    string text = xAttribute.Value.Trim().ToLower();
                    switch (text)
                    {
                        case "0":
                        case "f":
                        case "false":
                        case "n":
                        case "ne":
                            value = false;
                            break;
                        case "1":
                        case "t":
                        case "true":
                        case "a":
                        case "ano":
                            value = true;
                            break;
                    }
                }
            }
            return value;
        }
        /// <summary>
        /// V daném elementu najde atribut daného jména a vrátí jeho hodnotu převedenou do enumu <typeparamref name="TEnum"/>.
        /// Optional dovolí modifikovat načtený text z atributu pomocí funkce <paramref name="modifier"/>.
        /// </summary>
        /// <param name="xElement"></param>
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
        /// <param name="xElement"></param>
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
        /// Z dodaného <paramref name="xElement"/> načte hodnoty odpovídající souřadnicím prvku a vrátí je.
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="defaultValue"></param>
        private static Bounds _ReadAttributeBounds(System.Xml.Linq.XElement xElement, Bounds defaultValue)
        {
            int? left, top, width, height;

            var textBounds = _ReadAttributeString(xElement, "Bounds", null);
            if (!String.IsNullOrEmpty(textBounds))
            {
                var numbers = _SplitAndParseInt32(textBounds);
                if (numbers != null && numbers.Count >= 2)
                {
                    int cnt = numbers.Count;
                    left = numbers[0];
                    top = numbers[1];
                    width = (cnt >= 3 ? numbers[2] : null);
                    height = (cnt >= 4 ? numbers[3] : null);
                    return new Bounds(left, top, width, height);
                }
            }

            left = _ReadAttributeInt32N(xElement, "X");
            top = _ReadAttributeInt32N(xElement, "Y");
            width = _ReadAttributeInt32N(xElement, "Width");
            height = _ReadAttributeInt32N(xElement, "Height");
            if (left.HasValue || top.HasValue || width.HasValue || height.HasValue) return new Bounds(left, top, width, height);

            return defaultValue;
        }
        /// <summary>
        /// Z dodaného <paramref name="xElement"/> načte hodnoty odpovídající souřadnicím prvku a vrátí je.
        /// </summary>
        /// <param name="xElement"></param>
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
            List<int> result = new List<int>();
            foreach ( var item in items ) 
            {
                if (!String.IsNullOrEmpty(item) && Int32.TryParse(item.Trim().Replace(",", "."), out var number))
                    result.Add(number);
            }
            return (result.Count > 0 ? result : null);
        }
        /// <summary>
        /// Vrátí lokální jméno elementu, Trim(), ToLower().
        /// </summary>
        /// <param name="xElement"></param>
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
        #region class LoaderContext
        /// <summary>
        /// Třída, zahrnující v sobě průběžná data pro načítání obsahu <see cref="DfForm"/> v metodách v <see cref="DfTemplateLoader"/>
        /// </summary>
        private class LoaderContext
        {
            /// <summary>
            /// Funkce, která vrátí stringový obsah nested šablony daného jména.<br/>
            /// Pokud bude jako <see cref="NestedLoader"/> předána hodnota null, a v šabloně bude detekován Nested prvek, pak dojde k chybě.<br/>
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
            switch(child)
            {
                case DfBaseControl control: result.Add(control); break;
                case DfPage page: _AddAllControls(page, result); break;
                case DfPanel panel: _AddAllControls(panel, result); break;
            }
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
}