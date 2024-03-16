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
    /// Třída, která načte XML soubor / stream obsahující <see cref="DataFormatContainerForm"/>, i rekurzivně (nested Tabs)
    /// </summary>
    internal class DxDataFormatLoader
    {
        #region Načítání obsahu - public rozhraní
        /// <summary>
        /// Načte a vrátí <see cref="DataFormatContainerForm"/> ze zadaného souboru
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="nestedLoader">Funkce, která vrátí stringový obsah nested šablony daného jména</param>
        /// <returns></returns>
        internal static DataFormatContainerForm LoadFromFile(string fileName, Func<string, string> nestedLoader = null)
        {
            var startTime1 = DxComponent.LogTimeCurrent;
            LoaderContext loaderContext = new LoaderContext() { NestedLoader = nestedLoader };
            var xDocument = System.Xml.Linq.XDocument.Load(fileName);
            DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load 'XDocument' from file: {DxComponent.LogTokenTimeMicrosec}", startTime1);

            var startTime2 = DxComponent.LogTimeCurrent;
            var form = _LoadFromDocument(xDocument, loaderContext);
            DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Load 'DataFormatContainerForm' from XDocument: {DxComponent.LogTokenTimeMicrosec}", startTime2);

            return form;
        }
        /// <summary>
        /// Načte a vrátí <see cref="DataFormatContainerForm"/> ze zadané XML definice (=typicky obsah souboru)
        /// </summary>
        /// <param name="content"></param>
        /// <param name="nestedLoader">Funkce, která vrátí stringový obsah nested šablony daného jména</param>
        /// <returns></returns>
        internal static DataFormatContainerForm LoadFromContent(string content, Func<string, string> nestedLoader = null)
        {
            LoaderContext loaderContext = new LoaderContext() { NestedLoader = nestedLoader };
            var xDocument = System.Xml.Linq.XDocument.Parse(content);
            return _LoadFromDocument(xDocument, loaderContext);
        }
        /// <summary>
        /// Načte a vrátí <see cref="DataFormatContainerForm"/> z dodaného <see cref="System.Xml.Linq.XDocument"/>
        /// </summary>
        /// <param name="xDocument"></param>
        /// <param name="nestedLoader">Funkce, která vrátí stringový obsah nested šablony daného jména</param>
        /// <returns></returns>
        internal static DataFormatContainerForm LoadFromDocument(System.Xml.Linq.XDocument xDocument, Func<string, string> nestedLoader = null)
        {
            LoaderContext loaderContext = new LoaderContext() { NestedLoader = nestedLoader };
            return _LoadFromDocument(xDocument, loaderContext);
        }
        #endregion
        #region Načítání obsahu - private tvorba containerů
        /// <summary>
        /// Načte a vrátí <see cref="DataFormatContainerForm"/> z dodaného <see cref="System.Xml.Linq.XDocument"/>
        /// </summary>
        /// <param name="xDocument"></param>
        /// <param name="loaderContext">Průběžná data pro načítání obsahu</param>
        /// <returns></returns>
        private static DataFormatContainerForm _LoadFromDocument(System.Xml.Linq.XDocument xDocument, LoaderContext loaderContext)
        {
            DataFormatContainerForm form = new DataFormatContainerForm();
            var xElements = xDocument.Root.Elements();
            foreach (var xElement in xElements)
            {
                DataFormatBaseControl control = _LoadContainer(xElement, loaderContext);
                if (control != null)
                {
                    if (form.Controls is null)
                        form.Controls = new List<DataFormatBaseControl>();
                    form.Controls.Add(control);
                }
            }
            return form;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající kontejner (pageset, panel, nestedpanel), včetně jeho obsahu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="loaderContext">Průběžná data pro načítání obsahu</param>
        private static DataFormatBaseControl _LoadContainer(System.Xml.Linq.XElement xElement, LoaderContext loaderContext)
        {
            string elementName = xElement?.Name.LocalName.ToLower();          // pageset, panel, nestedpanel
            switch (elementName)
            {
                case "pageset":
                    return _LoadPageSet(xElement, loaderContext);
                case "panel":
                    return _LoadPanel(xElement, loaderContext);
                case "nestedpanel":
                    return _LoadNestedPanel(xElement, loaderContext);
            }
            return null;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající PageSet, včetně jeho obsahu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DataFormatBaseControl _LoadPageSet(System.Xml.Linq.XElement xElement, LoaderContext loaderContext)
        {
            // Záložkovník bez jednotlivých záložek neakceptuji:
            var xPages = xElement.Elements();
            if (xPages is null) return null;

            // Výsledná instance:
            var pageSet = new DataFormatContainerPageSet();
            pageSet.Style = ContainerStyle.PageSet;

            // Atributy:
            _FillBaseAttributes(xElement, pageSet);

            // Elementy:
            foreach (var xPage in xPages)
            {
                var page = _LoadPage(xPage, loaderContext);
                if (page != null)
                {
                    if (pageSet.Controls is null)
                        pageSet.Controls = new List<DataFormatBaseControl>();
                    pageSet.Controls.Add(page);
                }
            }

            return ((pageSet.Pages.Length > 0) ? pageSet : null);
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající Page, včetně jeho obsahu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DataFormatBaseControl _LoadPage(System.Xml.Linq.XElement xElement, LoaderContext loaderContext)
        {
            // Výsledná instance:
            var page = new DataFormatContainerPage();

            // Atributy:
            _FillBaseAttributes(xElement, page);
            page.Title = _ReadAttributeString(xElement, "Title", null);
            page.IconName = _ReadAttributeString(xElement, "IconName", null);

            // Elementy = Controly:
            _LoadContainerControls(xElement, page, loaderContext);

            return page;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající NestedPanel, včetně jeho obsahu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DataFormatBaseControl _LoadNestedPanel(System.Xml.Linq.XElement xElement, LoaderContext loaderContext)
        {
            // Nested šablona:
            string nestedTemplateName = _ReadAttributeString(xElement, "NestedTemplate", "");
            if (String.IsNullOrEmpty(nestedTemplateName)) return null;

            // Výsledná instance:
            var nestedPanel = new DataFormatContainerPanel();
            nestedPanel.Style = ContainerStyle.Panel;

            // Atributy:
            _FillBaseAttributes(xElement, nestedPanel);

            // Obsah nested panelu:
            if (loaderContext.NestedLoader is null) throw new InvalidOperationException($"DataForm contains NestedTemplate '{nestedTemplateName}', but 'NestedLoader' is null.");
            string nestedContent = loaderContext.NestedLoader(nestedTemplateName);
            if (!String.IsNullOrEmpty(nestedContent))
            {
                var xNestedDocument = System.Xml.Linq.XDocument.Parse(nestedContent);
                var nestedTemplate = _LoadFromDocument(xNestedDocument, loaderContext);
                if (nestedTemplate != null)
                {   // Přenesu některé atributy a všechny prvky Items:

                    nestedPanel.Controls.AddRange(nestedTemplate.Controls);
                }
            }

            return nestedPanel;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající Panel, včetně jeho obsahu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DataFormatBaseControl _LoadPanel(System.Xml.Linq.XElement xElement, LoaderContext loaderContext)
        {
            // Výsledná instance:
            var panel = new DataFormatContainerPanel();
            panel.Style = ContainerStyle.Panel;

            // Atributy:
            _FillBaseAttributes(xElement, panel);
            
            // Elementy = Controly:
            _LoadContainerControls(xElement, panel, loaderContext);

            return panel;
        }

        private static void _LoadContainerControls(System.Xml.Linq.XElement xElement, DataFormatBaseContainer container, LoaderContext loaderContext)
        {
            // Elementy = Items:
            var xItems = xElement.Elements();
            if (xItems != null)
            {
                foreach (var xItem in xItems)
                {
                    var item = _LoadItem(xItem, loaderContext);
                    if (item != null)
                    {
                        if (container.Controls is null)
                            container.Controls = new List<DataFormatBaseControl>();
                        container.Controls.Add(item);
                    }
                }
            }
        }
        #endregion
        #region Načítání obsahu - private tvorba jednotlivých controlů
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající Item, včetně jeho obsahu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DataFormatBaseControl _LoadItem(System.Xml.Linq.XElement xElement, LoaderContext loaderContext)
        {
            string elementName = xElement?.Name.LocalName.ToLower();          // label, textbox, textbox_button, button, combobox, ...,   pageset, panel, nestedpanel,
            switch (elementName)
            {
                case "label": return _FillControlLabel(xElement, new DataFormatControlLabel(), loaderContext);
                case "title": return _FillControlTitle(xElement, new DataFormatControlTitle(), loaderContext);
                case "checkbox": return _FillControlCheckBox(xElement, new DataFormatControlCheckBox(), loaderContext);
                case "button": return _FillControlButton(xElement, new DataFormatControlButton(), loaderContext);
                case "dropdownbutton": return _FillControlDropDownButton(xElement, new DataFormatControlDropDownButton(), loaderContext);
                case "textbox": return _FillControlTextBox(xElement, new DataFormatControlTextBox(), loaderContext);
                case "textboxbutton": return _FillControlTextBoxButton(xElement, new DataFormatControlTextBoxButton(), loaderContext);
                case "combobox": return _FillControlComboBox(xElement, new DataFormatComboBox(), loaderContext);

                case "panel": return _LoadPanel(xElement, loaderContext);
                case "nestedpanel": return _LoadNestedPanel(xElement, loaderContext);
                case "pageset": return _LoadPageSet(xElement, loaderContext);

            }
            return null;
        }
        private static DataFormatBaseControl _FillControlLabel(System.Xml.Linq.XElement xElement, DataFormatControlLabel control, LoaderContext loaderContext)
        {
            control.ControlType = ControlType.Label;
            _FillBaseAttributes(xElement, control);
            control.Text = _ReadAttributeString(xElement, "Text", null);
            control.Alignment = _ReadAttributeEnum(xElement, "Alignment", ContentAlignmentType.Default);
            return control;
        }
        private static DataFormatBaseControl _FillControlTitle(System.Xml.Linq.XElement xElement, DataFormatControlTitle control, LoaderContext loaderContext)
        {
            control.ControlType = ControlType.Title;
            _FillControlLabel(xElement, control, loaderContext);
            return control;
        }
        private static DataFormatBaseControl _FillControlCheckBox(System.Xml.Linq.XElement xElement, DataFormatControlCheckBox control, LoaderContext loaderContext)
        {
            control.ControlType = ControlType.CheckBox;
            _FillBaseAttributes(xElement, control);
            control.Style = _ReadAttributeEnum(xElement, "Style", CheckBoxStyleType.Default);
            return control;
        }
        private static DataFormatBaseControl _FillControlButton(System.Xml.Linq.XElement xElement, DataFormatControlButton control, LoaderContext loaderContext)
        {
            control.ControlType = ControlType.Button;
            _FillBaseAttributes(xElement, control);
            control.ActionName = _ReadAttributeString(xElement, "ActionName", null);
            control.ActionData = _ReadAttributeString(xElement, "ActionData", null);
            control.HotKey = _ReadAttributeString(xElement, "HotKey", null);
            return control;
        }
        private static DataFormatBaseControl _FillControlDropDownButton(System.Xml.Linq.XElement xElement, DataFormatControlDropDownButton control, LoaderContext loaderContext)
        {
            control.ControlType = ControlType.DropDownButton;
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
                        var subButton = _FillControlSubButton(xElement, new DataFormatSubButton(), loaderContext);
                        if (subButton != null)
                        {
                            if (control.DropDownButtons is null)
                                control.DropDownButtons = new List<DataFormatSubButton>();
                            control.DropDownButtons.Add(subButton);
                        }
                    }
                }
            }

            return control;
        }
        private static DataFormatBaseControl _FillControlTextBox(System.Xml.Linq.XElement xElement, DataFormatControlTextBox control, LoaderContext loaderContext)
        {
            control.ControlType = ControlType.TextBox;
            _FillBaseAttributes(xElement, control);
            control.EditMask = _ReadAttributeString(xElement, "EditMask", null);
            control.Alignment = _ReadAttributeEnum(xElement, "Alignment", ContentAlignmentType.Default);
            return control;
        }
        private static DataFormatBaseControl _FillControlTextBoxButton(System.Xml.Linq.XElement xElement, DataFormatControlTextBoxButton control, LoaderContext loaderContext)
        {
            control.ControlType = ControlType.TextBoxButton;
            _FillControlTextBox(xElement, control, loaderContext);

            // Elementy = SubButtons:
            var xItems = xElement.Elements();
            if (xItems != null)
            {
                foreach (var xItem in xItems)
                {
                    string elementName = xItem?.Name.LocalName;
                    if (String.Equals(elementName, "leftButton", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var subButton = _FillControlSubButton(xElement, new DataFormatSubButton(), loaderContext);
                        if (subButton != null)
                        {
                            if (control.LeftButtons is null)
                                control.LeftButtons = new List<DataFormatSubButton>();
                            control.LeftButtons.Add(subButton);
                        }
                    }
                    else if (String.Equals(elementName, "rightButton", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var subButton = _FillControlSubButton(xElement, new DataFormatSubButton(), loaderContext);
                        if (subButton != null)
                        {
                            if (control.RightButtons is null)
                                control.RightButtons = new List<DataFormatSubButton>();
                            control.RightButtons.Add(subButton);
                        }
                    }
                }
            }

            return control;
        }
        private static DataFormatBaseControl _FillControlComboBox(System.Xml.Linq.XElement xElement, DataFormatComboBox control, LoaderContext loaderContext)
        {
            control.ControlType = ControlType.ComboListBox;
            _FillBaseAttributes(xElement, control);
            control.Style = _ReadAttributeEnum(xElement, "Style", ComboBoxStyleType.Default);
            control.EditStyleName = _ReadAttributeString(xElement, "EditStyleName", null);

            // Elementy = SubButtons:
            var xItems = xElement.Elements();
            if (xItems != null)
            {
                foreach (var xItem in xItems)
                {
                    string elementName = xItem?.Name.LocalName;
                    if (String.Equals(elementName, "comboItem", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var comboItem = _FillControlSubButton(xElement, new DataFormatSubButton(), loaderContext);
                        if (comboItem != null)
                        {
                            if (control.ComboItems is null)
                                control.ComboItems = new List<DataFormatSubButton>();
                            control.ComboItems.Add(comboItem);
                        }
                    }
                }
            }

            return control;
        }
        private static DataFormatSubButton _FillControlSubButton(System.Xml.Linq.XElement xElement, DataFormatSubButton control, LoaderContext loaderContext)
        {
            _FillBaseAttributes(xElement, control);
            return control;
        }
        #endregion
        #region Načítání atributů
        /// <summary>
        /// Z dodaného <paramref name="xElement"/> načte hodnoty odpovídající cílovému typu,
        /// a vloží je do dodaného controlu <paramref name="target"/>.
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="target"></param>
        private static void _FillBaseAttributes(System.Xml.Linq.XElement xElement, DataFormatBase target)
        {
            target.Name = _ReadAttributeString(xElement, "Name", null);

            if (target is DataFormatBaseSubControl subControl)
            {
                subControl.State = _ReadAttributeEnum(xElement, "State", ItemState.Default);
                subControl.ToolTipTitle = _ReadAttributeString(xElement, "ToolTipTitle", null);
                subControl.ToolTipText = _ReadAttributeString(xElement, "ToolTipText", null);
                subControl.Invisible = _ReadAttributeString(xElement, "Invisible", null);
            }
            if (target is DataFormatBaseControl control)
            {
                control.Bounds = _ReadAttributeBounds(xElement, null);
            }
            if (target is DataFormatBaseInputControl inputControl)
            {
                inputControl.Required = _ReadAttributeEnum(xElement, "Required", RequiredType.Default);
            }
            if (target is DataFormatBaseTextControl textControl)
            {
                textControl.Text = _ReadAttributeString(xElement, "Text", null);
                textControl.IconName = _ReadAttributeString(xElement, "IconName", null);
                textControl.Alignment = _ReadAttributeEnum(xElement, "Alignment", ContentAlignmentType.Default);
            }
            if (target is DataFormatSubButton subButton)
            {
                subButton.Text = _ReadAttributeString(xElement, "Text", null);
                subButton.IconName = _ReadAttributeString(xElement, "IconName", null);
                subButton.ActionName = _ReadAttributeString(xElement, "ActionName", null);
                subButton.ActionData = _ReadAttributeString(xElement, "ActionData", null);
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
        /// <param name="defaultValue"></param>
        private static Margins _ReadAttributesMargin(System.Xml.Linq.XElement xElement, Margins defaultValue)
        {
            int? left, top, right, bottom;

            var textMargins = _ReadAttributeString(xElement, "Margins", null);
            if (!String.IsNullOrEmpty(textMargins))
            {
                var numbers = _SplitAndParseInt32(textMargins);
                if (numbers != null && numbers.Count >= 1)
                {
                    int cnt = numbers.Count;

                    left = numbers[0];
                    if (cnt == 1) return new Margins(left.Value);

                    top = numbers[1];
                    if (cnt == 2) return new Margins(left.Value, top.Value, left.Value, top.Value);

                    right = (cnt >= 3 ? numbers[2] : null);
                    bottom = (cnt >= 4 ? numbers[3] : null);
                    return new Margins(left ?? 0, top ?? 0, right ?? 0, bottom ?? 0);
                }
            }

            left = _ReadAttributeInt32N(xElement, "X", null);
            top = _ReadAttributeInt32N(xElement, "Y", null);
            right = _ReadAttributeInt32N(xElement, "Width", null);
            bottom = _ReadAttributeInt32N(xElement, "Height", null);
            if (left.HasValue || top.HasValue || right.HasValue || bottom.HasValue) new Margins(left ?? 0, top ?? 0, right ?? 0, bottom ?? 0);

            return defaultValue;
        }
        /// <summary>
        /// Rozdělí dodaný string <paramref name="text"/>v místě daných oddělovačů <paramref name="splitters"/> a převede prvky na čísla Int.
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
        #endregion
        #region class LoaderContext
        /// <summary>
        /// Třída, zahrnující v sobě průběžná data pro načítání obsahu <see cref="DataFormatTab"/> v metodách v <see cref="DxDataFormatLoader"/>
        /// </summary>
        private class LoaderContext
        {
            /// <summary>
            /// Funkce, která vrátí stringový obsah nested šablony daného jména
            /// </summary>
            public Func<string, string> NestedLoader { get; set; }

        }
        #endregion
    }
}