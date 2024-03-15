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
    /// Třída, která načte XML soubor / stream obsahující <see cref="DataFormatTab"/>, i rekurzivně (nested Tabs)
    /// </summary>
    internal class DxDataFormatLoader
    {
        #region Načítání obsahu - public rozhraní
        /// <summary>
        /// Načte a vrátí <see cref="DataFormatTab"/> ze zadaného souboru
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
        /// Načte a vrátí <see cref="DataFormatTab"/> ze zadané XML definice (=typicky obsah souboru)
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
        /// Načte a vrátí <see cref="DataFormatTab"/> z dodaného <see cref="System.Xml.Linq.XDocument"/>
        /// </summary>
        /// <param name="xDocument"></param>
        /// <param name="nestedLoader">Funkce, která vrátí stringový obsah nested šablony daného jména</param>
        /// <returns></returns>
        internal static DataFormatContainerForm LoadFromDocument(System.Xml.Linq.XDocument xDocument, Func<string, string> nestedLoader = null)
        {
            LoaderContext loaderContext = new LoaderContext() { NestedLoader = nestedLoader };
            return _LoadFromDocument(xDocument, loaderContext);
        }
        /// <summary>
        /// Načte a vrátí <see cref="DataFormatTab"/> z dodaného <see cref="System.Xml.Linq.XDocument"/>
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
                DataFormatControlBase control = _LoadContainer(xElement, loaderContext);
                if (control != null) form.Controls.Add(control);
            }
            return form;
        }
        #endregion
        #region Načítání obsahu - private tvorba containerů
        /// <summary>
        /// Z dodaného elementu <paramref name="xElement"/> načte a vrátí odpovídající kontejner (pageset, panel, nestedpanel), včetně jeho obsahu
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="loaderContext">Průběžná data pro načítání obsahu</param>
        private static DataFormatControlBase _LoadContainer(System.Xml.Linq.XElement xElement, LoaderContext loaderContext)
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
        /// Z dodaného elementu <paramref name="xPageSet"/> načte a vrátí odpovídající PageSet, včetně jeho obsahu
        /// </summary>
        /// <param name="xPageSet"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DataFormatControlBase _LoadPageSet(System.Xml.Linq.XElement xPageSet, LoaderContext loaderContext)
        {
            // Záložkovník bez jednotlivých záložek neakceptuji:
            var xPages = xPageSet.Elements();
            if (xPages is null) return null;

            // Výsledná instance:
            var pageSet = new DataFormatContainerPageSet();

            // Atributy:

            // Elementy:
            foreach (var xPage in xPages)
            {
                var page = _LoadPage(xPage, loaderContext);
                if (page != null) pageSet.Controls.Add(page);
            }

            return ((pageSet.Pages.Length > 0) ? pageSet : null);
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xPage"/> načte a vrátí odpovídající Page, včetně jeho obsahu
        /// </summary>
        /// <param name="xPage"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DataFormatControlBase _LoadPage(System.Xml.Linq.XElement xPage, LoaderContext loaderContext)
        {
            // Výsledná instance:
            var page = new DataFormatContainerPage();

            // Atributy:

            // Elementy:
            var xItems = xPage.Elements();
            if (xItems != null)
            {
                foreach (var xItem in xItems)
                {
                    var item = _LoadItem(xItem, loaderContext);
                    if (item != null) page.Controls.Add(page);
                }
            }

            return page;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xNestedPanel"/> načte a vrátí odpovídající NestedPanel, včetně jeho obsahu
        /// </summary>
        /// <param name="xNestedPanel"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DataFormatControlBase _LoadNestedPanel(System.Xml.Linq.XElement xNestedPanel, LoaderContext loaderContext)
        {
            // Nested šablona:
            string nestedTemplateName = _ReadAttributeString(xNestedPanel, "NestedTemplate", "");
            if (String.IsNullOrEmpty(nestedTemplateName)) return null;

            // Výsledná instance:
            var nestedPanel = new DataFormatContainerPanel();

            // Atributy:
            nestedPanel.Name = _ReadAttributeString(xNestedPanel, "Name", "");

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
        /// Z dodaného elementu <paramref name="xPanel"/> načte a vrátí odpovídající Panel, včetně jeho obsahu
        /// </summary>
        /// <param name="xPanel"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DataFormatControlBase _LoadPanel(System.Xml.Linq.XElement xPanel, LoaderContext loaderContext)
        {
            // Výsledná instance:
            var panel = new DataFormatContainerPanel();

            // Atributy:

            // Elementy = Items:
            var xItems = xPanel.Elements();
            if (xItems != null)
            {
                foreach (var xItem in xItems)
                {
                    var item = _LoadItem(xItem, loaderContext);
                    if (item != null) panel.Controls.Add(panel);
                }
            }

            return panel;
        }
        #endregion
        #region Načítání obsahu - private tvorba jednotlivých controlů
        /// <summary>
        /// Z dodaného elementu <paramref name="xItem"/> načte a vrátí odpovídající Item, včetně jeho obsahu
        /// </summary>
        /// <param name="xItem"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DataFormatControlBase _LoadItem(System.Xml.Linq.XElement xItem, LoaderContext loaderContext)
        {
            string elementName = xItem?.Name.LocalName.ToLower();          // label, textbox, textbox_button, button, combobox, ...,   pageset, panel, nestedpanel,
            switch (elementName)
            {
                case "label": return _LoadItemLabel(xItem, loaderContext);
                case "textbox": return _LoadItemTextBox(xItem, loaderContext);
                case "textbox_button": return _LoadItemTextBoxButton(xItem, loaderContext);
                case "button": return _LoadItemButton(xItem, loaderContext);

                case "panel": return _LoadPanel(xItem, loaderContext);
                case "nestedpanel": return _LoadNestedPanel(xItem, loaderContext);
                case "pageset": return _LoadPageSet(xItem, loaderContext);

            }
            return null;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xItem"/> načte a vrátí odpovídající Label, včetně jeho obsahu
        /// </summary>
        /// <param name="xItem"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DataFormatControlBase _LoadItemLabel(System.Xml.Linq.XElement xItem, LoaderContext loaderContext)
        {
            var control = new DataFormatControlLabel() { ControlType = ControlType.Label };
            _FillCommonAttributes(xItem, control);

            control.Text = _ReadAttributeString(xItem, "Text", null);
            control.Alignment = _ReadAttributeEnum(xItem, "Alignment", ContentAlignmentType.Default);

            return control;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xItem"/> načte a vrátí odpovídající TextBox, včetně jeho obsahu
        /// </summary>
        /// <param name="xItem"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DataFormatControlBase _LoadItemTextBox(System.Xml.Linq.XElement xItem, LoaderContext loaderContext)
        {
            var control = new DataFormatControlTextBox() { ControlType = ControlType.TextBox };
            _FillCommonAttributes(xItem, control);


            return control;
        }
        /// <summary>
        /// Z dodaného elementu <paramref name="xItem"/> načte a vrátí odpovídající TextBoxButton, včetně jeho obsahu
        /// </summary>
        /// <param name="xItem"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DataFormatControlBase _LoadItemTextBoxButton(System.Xml.Linq.XElement xItem, LoaderContext loaderContext)
        {
            var control = new DataFormatControlTextBoxButton() { ControlType = ControlType.TextBoxButton };
            _FillCommonAttributes(xItem, control);


            return control;

        }

        /// <summary>
        /// Z dodaného elementu <paramref name="xItem"/> načte a vrátí odpovídající Button, včetně jeho obsahu
        /// </summary>
        /// <param name="xItem"></param>
        /// <param name="loaderContext"></param>
        /// <returns></returns>
        private static DataFormatControlBase _LoadItemButton(System.Xml.Linq.XElement xItem, LoaderContext loaderContext)
        {
            var control = new DataFormatControlButton() { ControlType = ControlType.Button };
            _FillCommonAttributes(xItem, control);


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
        private static void _FillCommonAttributes(System.Xml.Linq.XElement xElement, DataFormatBase target)
        {
            target.Name = _ReadAttributeString(xElement, "Name", null);

            if (target is DataFormatSubControlBase subControl)
            {
                subControl.State = _ReadAttributeEnum(xElement, "State", ItemState.Default);
                subControl.ToolTipTitle = _ReadAttributeString(xElement, "ToolTipTitle", null);
                subControl.ToolTipText = _ReadAttributeString(xElement, "ToolTipText", null);
                subControl.Invisible = _ReadAttributeString(xElement, "Invisible", null);
            }
            if (target is DataFormatControlBase control)
            {
                control.Bounds = _ReadAttributeBounds(xElement, null);
            }
            if (target is DataFormatInputControlBase inputControl)
            {
                inputControl.Required = _ReadAttributeEnum(xElement, "Required", RequiredType.Default);
            }
            if (target is DataFormatTextControlBase textControl)
            {
                textControl.Text = _ReadAttributeString(xElement, "Text", null);
                textControl.IconName = _ReadAttributeString(xElement, "IconName", null);
                textControl.Alignment = _ReadAttributeEnum(xElement, "Alignment", ContentAlignmentType.Default);
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