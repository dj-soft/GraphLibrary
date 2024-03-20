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

using Noris.Clients.Win.Components.AsolDX.DataForm.Format;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    /// <summary>
    /// Třída, která načte XML soubor / stream obsahující <see cref="DfForm"/>, i rekurzivně (nested Tabs)
    /// </summary>
    internal class DfTemplateLoader
    {
        #region Načítání obsahu a načítání Info - public rozhraní
        /// <summary>
        /// Načte a vrátí <see cref="DfForm"/> ze zadaného souboru
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="nestedLoader">Funkce, která vrátí stringový obsah nested šablony daného jména</param>
        /// <param name="logTime">Logovat časy?</param>
        /// <returns></returns>
        internal static DfForm LoadFromFile(string fileName, Func<string, string> nestedLoader = null, bool logTime = false)
        {
            LoaderContext loaderContext = new LoaderContext() { NestedLoader = nestedLoader };
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
        /// <param name="content"></param>
        /// <param name="nestedLoader">Funkce, která vrátí stringový obsah nested šablony daného jména</param>
        /// <param name="logTime">Logovat časy?</param>
        /// <returns></returns>
        internal static DfForm LoadFromContent(string content, Func<string, string> nestedLoader = null, bool logTime = false)
        {
            LoaderContext loaderContext = new LoaderContext() { NestedLoader = nestedLoader };

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
        /// <param name="xDocument"></param>
        /// <param name="nestedLoader">Funkce, která vrátí stringový obsah nested šablony daného jména</param>
        /// <param name="logTime">Logovat časy?</param>
        /// <returns></returns>
        internal static DfForm LoadFromDocument(System.Xml.Linq.XDocument xDocument, Func<string, string> nestedLoader = null, bool logTime = false)
        {
            LoaderContext loaderContext = new LoaderContext() { NestedLoader = nestedLoader };

            var startTime3 = DxComponent.LogTimeCurrent;
            var form = _LoadFromDocument(xDocument, loaderContext);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load 'DataFormatContainerForm' from 'XDocument': {DxComponent.LogTokenTimeMicrosec}", startTime3);

            return form;
        }

        /// <summary>
        /// Načte a vrátí <see cref="DfInfoForm"/> ze zadaného souboru
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="logTime">Logovat časy?</param>
        /// <returns></returns>
        internal static DfInfoForm LoadInfoFromFile(string fileName, bool logTime = false)
        {
            return LoadInfoFromFile(fileName, out var _, logTime);
        }
        /// <summary>
        /// Načte a vrátí <see cref="DfInfoForm"/> ze zadaného souboru
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="xDocument"></param>
        /// <param name="logTime">Logovat časy?</param>
        /// <returns></returns>
        internal static DfInfoForm LoadInfoFromFile(string fileName, out System.Xml.Linq.XDocument xDocument, bool logTime = false)
        {
            LoaderContext loaderContext = new LoaderContext() { IsLoadOnlyDocumentAttributes = true };
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
        /// Načte a vrátí <see cref="DfInfoForm"/> ze zadané XML definice (=typicky obsah souboru)
        /// </summary>
        /// <param name="content"></param>
        /// <param name="logTime">Logovat časy?</param>
        /// <returns></returns>
        internal static DfInfoForm LoadInfoFromContent(string content, bool logTime = false)
        {
            return LoadInfoFromFile(content, out var _, logTime);
        }
        /// <summary>
        /// Načte a vrátí <see cref="DfInfoForm"/> ze zadané XML definice (=typicky obsah souboru)
        /// </summary>
        /// <param name="content"></param>
        /// <param name="xDocument"></param>
        /// <param name="logTime">Logovat časy?</param>
        /// <returns></returns>
        internal static DfInfoForm LoadInfoFromContent(string content, out System.Xml.Linq.XDocument xDocument, bool logTime = false)
        {
            LoaderContext loaderContext = new LoaderContext() { IsLoadOnlyDocumentAttributes = true };

            var startTime2 = DxComponent.LogTimeCurrent;
            xDocument = System.Xml.Linq.XDocument.Parse(content);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Parse 'XDocument' from 'Content' ({content.Length} B): {DxComponent.LogTokenTimeMicrosec}", startTime2);

            var startTime3 = DxComponent.LogTimeCurrent;
            var form = _LoadFromDocument(xDocument, loaderContext);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load 'DataFormatInfoForm' from 'XDocument': {DxComponent.LogTokenTimeMicrosec}", startTime3);

            return _CreateInfoForm(form);
        }
        /// <summary>
        /// Načte a vrátí <see cref="DfInfoForm"/> z dodaného <see cref="System.Xml.Linq.XDocument"/>
        /// </summary>
        /// <param name="xDocument"></param>
        /// <param name="logTime">Logovat časy?</param>
        /// <returns></returns>
        internal static DfInfoForm LoadInfoFromDocument(System.Xml.Linq.XDocument xDocument, bool logTime = false)
        {
            LoaderContext loaderContext = new LoaderContext() { IsLoadOnlyDocumentAttributes = true };

            var startTime3 = DxComponent.LogTimeCurrent;
            var form = _LoadFromDocument(xDocument, loaderContext);
            if (logTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load 'DataFormatInfoForm' from 'XDocument': {DxComponent.LogTokenTimeMicrosec}", startTime3);

            return _CreateInfoForm(form);
        }
        /// <summary>
        /// Z dodaného kompletního <see cref="DfForm"/> vytvoří jednoduchý <see cref="DfInfoForm"/>
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        private static DfInfoForm _CreateInfoForm(DfForm form)
        {
            return new DfInfoForm() { XmlNamespace = form?.XmlNamespace, FormatVersion = form?.FormatVersion };
        }
        #endregion
        #region Načítání obsahu - private tvorba containerů
        
        // Každá zdejší větev / metoda načte pouze property deklarované přímo pro danou třídu, nikoli pro její předky!

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
            control.FormatVersion = _ReadAttributeString(xElement, "FormatVersion", null);

            // Rychlá odbočka?
            if (loaderContext.IsLoadOnlyDocumentAttributes) return control;

            // Full Load:
            control.MasterWidth = _ReadAttributeInt32N(xElement, "MasterWidth", null);
            control.MasterHeight = _ReadAttributeInt32N(xElement, "MasterHeight", null);
            control.TotalWidth = _ReadAttributeInt32N(xElement, "TotalWidth", null);
            control.TotalHeight = _ReadAttributeInt32N(xElement, "TotalHeight", null);
            control.AutoLabelPosition = _ReadAttributeEnum(xElement, "AutoLabelPosition", LabelPositionType.None);
            control.DataSource = _ReadAttributeString(xElement, "DataSource", null);
            control.Messages = _ReadAttributeString(xElement, "Messages", null);
            control.UseNorisClass = _ReadAttributeInt32N(xElement, "UseNorisClass", null);
            control.AddUda = _ReadAttributeBoolN(xElement, "UseNorisClass", true);
            control.UdaLabelPosition = _ReadAttributeEnum(xElement, "UdaLabelPosition", LabelPositionType.Up);

            // Elementy:
            var xContainers = xElement.Elements();
            foreach (var xContainer in xContainers)
            {
                var container = _CreateContainer(xContainer, loaderContext);
                if (container != null)
                {
                    if (control.Childs is null)
                        control.Childs = new List<DfBase>();
                    control.Childs.Add(container);
                }
            }
            return control;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající kontejner (pageset, panel, nestedpanel), včetně jeho obsahu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="loaderContext">Průběžná data pro načítání obsahu</param>
        private static DfBaseContainer _CreateContainer(System.Xml.Linq.XElement xElement, LoaderContext loaderContext)
        {
            string elementName = xElement?.Name.LocalName.ToLower();          // pageset, panel, nestedpanel
            switch (elementName)
            {
                case "pageset":
                    return _FillContainerPageSet(xElement, null, loaderContext);
                case "panel":
                    return _FillContainerPanel(xElement, null, loaderContext);
                case "nestedpanel":
                    return _FillContainerNestedPanel(xElement, null, loaderContext);
            }
            return null;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající PageSet, včetně jeho obsahu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="control"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DfPageSet _FillContainerPageSet(System.Xml.Linq.XElement xElement, DfPageSet control, LoaderContext loaderContext)
        {
            // Záložkovník bez jednotlivých záložek neakceptuji:
            var xPages = xElement.Elements();
            if (xPages is null) return null;

            // Výsledná instance:
            if (control is null) control = new DfPageSet();

            // Atributy:
            _FillBaseAttributes(xElement, control);

            // Elementy:
            foreach (var xPage in xPages)
            {
                var page = _FillContainerPage(xPage, null, loaderContext);
                if (page != null)
                {
                    if (control.Childs is null)
                        control.Childs = new List<DfBase>();
                    control.Childs.Add(page);
                }
            }

            return ((control.Pages.Length > 0) ? control : null);
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající Page, včetně jeho obsahu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="control"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DfPage _FillContainerPage(System.Xml.Linq.XElement xElement, DfPage control, LoaderContext loaderContext)
        {
            // Výsledná instance:
            if (control is null) control = new DfPage();

            // Atributy:
            _FillBaseAttributes(xElement, control);
            control.Title = _ReadAttributeString(xElement, "Title", null);
            control.IconName = _ReadAttributeString(xElement, "IconName", null);

            // Elementy = Controly:
            _LoadContainerControls(xElement, control, loaderContext);

            return control;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající NestedPanel, včetně jeho obsahu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="control"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DfPanel _FillContainerNestedPanel(System.Xml.Linq.XElement xElement, DfPanel control, LoaderContext loaderContext)
        {
            // Nested šablona:
            string nestedTemplateName = _ReadAttributeString(xElement, "NestedTemplate", "");
            if (String.IsNullOrEmpty(nestedTemplateName)) return null;

            // Výsledná instance:
            if (control is null) control = new DfPanel();

            // Atributy:
            _FillBaseAttributes(xElement, control);

            // Obsah nested panelu:
            if (loaderContext.NestedLoader is null) throw new InvalidOperationException($"DataForm contains NestedTemplate '{nestedTemplateName}', but 'NestedLoader' is null.");
            string nestedContent = loaderContext.NestedLoader(nestedTemplateName);
            if (!String.IsNullOrEmpty(nestedContent))
            {
                var xNestedDocument = System.Xml.Linq.XDocument.Parse(nestedContent);
                var nestedTemplate = _LoadFromDocument(xNestedDocument, loaderContext);
                if (nestedTemplate != null && nestedTemplate.Tabs.OfType<DfPanel>().TryGetFirst(t => (t is not null), out var sourcePanel))
                {   // Přenesu některé atributy:
                    control.BackColorName = sourcePanel.BackColorName;
                    control.BackColorLight = sourcePanel.BackColorLight;
                    control.BackColorDark = sourcePanel.BackColorDark;
                    control.Bounds = _CloneBoundsSize(sourcePanel.Bounds);
                    control.Margins = _CloneMargins(sourcePanel.Margins);
                    control.State = sourcePanel.State;
                    control.Invisible = sourcePanel.Invisible;
                    control.ToolTipTitle = sourcePanel.ToolTipTitle;
                    control.ToolTipText = sourcePanel.ToolTipText;

                    // Přenesu všechny prvky Items:
                    control.Childs.AddRange(nestedTemplate.Childs);
                }
            }

            return control;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající Panel, včetně jeho obsahu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="control"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DfPanel _FillContainerPanel(System.Xml.Linq.XElement xElement, DfPanel control, LoaderContext loaderContext)
        {
            // Výsledná instance:
            if (control is null) control = new DfPanel();

            // Atributy:
            _FillBaseAttributes(xElement, control);
            control.IconName = _ReadAttributeString(xElement, "IconName", null);
            control.Title = _ReadAttributeString(xElement, "Title", null);
            control.TitleStyle = _ReadAttributeEnum(xElement, "TitleStyle", TitleStyleType.Default);
            control.CollapseState = _ReadAttributeEnum(xElement, "CollapseState", PanelCollapseState.Default);

            // Elementy = Controly:
            _LoadContainerControls(xElement, control, loaderContext);

            return control;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte jeho vnitřní elementy, reprezentující controly, a uloží je do dodaného containeru <paramref name="container"/>.
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="container"></param>
        /// <param name="loaderContext"></param>
        private static void _LoadContainerControls(System.Xml.Linq.XElement xElement, DfBaseContainer container, LoaderContext loaderContext)
        {
            // Elementy = Items:
            var xItems = xElement.Elements();
            if (xItems != null)
            {
                foreach (var xItem in xItems)
                {
                    var item = _CreateItem(xItem, loaderContext);
                    if (item != null)
                    {
                        if (container.Childs is null)
                            container.Childs = new List<DfBase>();
                        container.Childs.Add(item);
                    }
                }
            }
        }
        #endregion
        #region Načítání obsahu - private tvorba jednotlivých controlů

        // Každá zdejší větev / metoda načte pouze property deklarované přímo pro danou třídu, nikoli pro její předky!

        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající Item, včetně jeho obsahu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DfBase _CreateItem(System.Xml.Linq.XElement xElement, LoaderContext loaderContext)
        {
            string elementName = xElement?.Name.LocalName.ToLower();          // label, textbox, textbox_button, button, combobox, ...,   pageset, panel, nestedpanel,
            switch (elementName)
            {
                case "label": return _FillControlLabel(xElement, new DfLabel(), loaderContext);
                case "title": return _FillControlTitle(xElement, new DfTitle(), loaderContext);
                case "checkbox": return _FillControlCheckBox(xElement, new DfCheckBox(), loaderContext);
                case "button": return _FillControlButton(xElement, new DfButton(), loaderContext);
                case "dropdownbutton": return _FillControlDropDownButton(xElement, new DfDropDownButton(), loaderContext);
                case "textbox": return _FillControlTextBox(xElement, new DfTextBox(), loaderContext);
                case "textboxbutton": return _FillControlTextBoxButton(xElement, new DfTextBoxButton(), loaderContext);
                case "combobox": return _FillControlComboBox(xElement, new DfComboBox(), loaderContext);

                case "pageset": return _FillContainerPageSet(xElement, null, loaderContext);
                case "panel": return _FillContainerPanel(xElement, null, loaderContext);
                case "nestedpanel": return _FillContainerNestedPanel(xElement, null, loaderContext);
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
        /// <see cref="DfBaseInputTextControl"/>, <see cref="DfBaseLabeledInputControl"/>, <see cref="DfSubButton"/>, <see cref="DfBaseContainer"/>.
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="target"></param>
        private static void _FillBaseAttributes(System.Xml.Linq.XElement xElement, DfBase target)
        {
            // Každá zdejší větev / metoda načte pouze property deklarované přímo pro danou třídu, nikoli pro její předky!
            target.Name = _ReadAttributeString(xElement, "Name", null);
            target.State = _ReadAttributeEnum(xElement, "State", ControlStateType.Default);
            target.ToolTipTitle = _ReadAttributeString(xElement, "ToolTipTitle", null);
            target.ToolTipText = _ReadAttributeString(xElement, "ToolTipText", null);
            target.Invisible = _ReadAttributeString(xElement, "Invisible", null);

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
                labeledControl.LabelWidth = _ReadAttributeInt32N(xElement, "LabelWidth", null);
            }
            if (target is DfBaseContainer container)
            {
                container.BackColorName = _ReadAttributeString(xElement, "BackColorName", null);
                container.BackColorLight = _ReadAttributeColorN(xElement, "BackColorLight", null);
                container.BackColorDark = _ReadAttributeColorN(xElement, "BackColorDark", null);
                container.Margins = _ReadAttributesMargin(xElement, "Margins", null);
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
        private static int? _ReadAttributeInt32N(System.Xml.Linq.XElement xElement, string attributeName, int? defaultValue)
        {
            int? value = defaultValue;
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
        private static System.Drawing.Color? _ReadAttributeColorN(System.Xml.Linq.XElement xElement, string attributeName, System.Drawing.Color? defaultValue)
        {
            System.Drawing.Color? value = defaultValue;
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
        /// V daném elementu najde atribut daného jména a vrátí jeho Boolean? podobu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        private static bool? _ReadAttributeBoolN(System.Xml.Linq.XElement xElement, string attributeName, bool? defaultValue)
        {
            bool? value = defaultValue;
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
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        private static TEnum _ReadAttributeEnum<TEnum>(System.Xml.Linq.XElement xElement, string attributeName, TEnum defaultValue) where TEnum : struct
        {
            TEnum value = defaultValue;
            if (xElement.HasAttributes && !String.IsNullOrEmpty(attributeName))
            {
                var xAttribute = xElement.Attribute(attributeName);
                if (xAttribute != null && !String.IsNullOrEmpty(xAttribute.Value))
                {
                    if (Enum.TryParse<TEnum>(xAttribute.Value, true, out var result)) value = result;
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

            left = _ReadAttributeInt32N(xElement, "X", null);
            top = _ReadAttributeInt32N(xElement, "Y", null);
            width = _ReadAttributeInt32N(xElement, "Width", null);
            height = _ReadAttributeInt32N(xElement, "Height", null);
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
            /// Funkce, která vrátí stringový obsah nested šablony daného jména
            /// </summary>
            public Func<string, string> NestedLoader { get; set; }
            /// <summary>
            /// Máme načíst pouze atributy dokumentu, pro detekci jeho hlavičky (<see cref="DfForm.XmlNamespace"/> a <see cref="DfForm.FormatVersion"/>)
            /// </summary>
            public bool IsLoadOnlyDocumentAttributes { get; set; }
        }
        #endregion
    }
}