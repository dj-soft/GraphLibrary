using DjSoft.Tools.ProgramLauncher.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DjSoft.Tools.ProgramLauncher.Components.InteractiveMap;

namespace DjSoft.Tools.ProgramLauncher.Components
{
    #region DataControlPanel : Panel určený pro zobrazení sady dat
    /// <summary>
    /// <see cref="DataControlPanel"/> : Panel určený pro zobrazení sady dat
    /// </summary>
    public class DataControlPanel : DPanel, IDataControl
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataControlPanel()
        {
            this.InitializePanel();
        }
        /// <summary>
        /// Inicializuje panel a jeho obsah
        /// </summary>
        protected virtual void InitializePanel()
        {
            this.__ContentPadding = ControlSupport.StandardContentPadding;
            this.__ContentSize = null;
            this.__Cells = new Dictionary<string, CellInfo>();
            this.BorderStyle = BorderStyle.None;
            this.Buttons = new DialogButtonType[] { DialogButtonType.Ok };
            this.AcceptButtonType = DialogButtonType.Ok;
            this.CancelButtonType = DialogButtonType.Cancel;
        }
        /// <summary>
        /// Mezery mezi okraji this Panelu a prostorem jednotlivých prvků.
        /// Pozice prvků, zadávaná do metody <see cref="AddCell(ControlType, string, string, int, int, int)"/> je relativní k tomuto Padding.
        /// </summary>
        public Padding ContentPadding { get { return __ContentPadding; } set { __ContentPadding = value; LayoutContent(true); } } private Padding __ContentPadding;
        /// <summary>
        /// Optimální velikost, určená explicitně zvenku
        /// </summary>
        public virtual Size? OptimalSize { get; set; }
        /// <summary>
        /// Aktuální velikost, určená podle aktuálnícho obsahu Controlů a okrajů Padding
        /// </summary>
        public virtual Size? ContentSize { get { return __ContentSize; } } private Size? __ContentSize;
        /// <summary>
        /// Přítomné buttony v odpovídajícím pořadí
        /// </summary>
        public virtual DialogButtonType[] Buttons { get; set; }
        public virtual DialogButtonType AcceptButtonType { get; set; }
        public virtual DialogButtonType CancelButtonType { get; set; }

        /// <summary>
        /// Po kliknutí na některý button: ukládání dat
        /// </summary>
        /// <param name="args"></param>
        public virtual void DialogButtonClicked(DialogButtonEventArgs args)
        {
            switch (args.DialogButtonType)
            {
                case DialogButtonType.Ok:
                    DataStore();
                    this.ResultsToForm(DialogResult.OK, true);
                    break;
                case DialogButtonType.Apply:
                    DataStore();
                    this.ResultsToForm(DialogResult.OK, false);
                    break;
                case DialogButtonType.Cancel:
                    this.ResultsToForm(DialogResult.Cancel, true);
                    break;
            }
        }
        /// <summary>
        /// Do formuláře tohoto okna vepíše daný <see cref="DialogResult"/> a volitelně okno zavře.
        /// </summary>
        /// <param name="dialogResult"></param>
        /// <param name="closeForm"></param>
        protected void ResultsToForm(DialogResult? dialogResult, bool closeForm)
        {
            var form = this.FindForm();
            if (form != null)
            {
                // Pokud jsem do Resultu dříve vepsal OK, pak do něj už nemůžu vepisovat Cancel, protože už proběhlo uložení dat do objektu (proto tam je to OK)...
                if (dialogResult.HasValue && form.DialogResult != DialogResult.OK)
                    form.DialogResult = dialogResult.Value;

                if (closeForm)
                    form.Close();
            }
        }
        #endregion
        #region Vkládání prvků AddCell(), buňky Cells, třída CellInfo
        /// <summary>
        /// Odstraní všechny vložené prvky
        /// </summary>
        public void ClearCells()
        {
            foreach (var cell in __Cells.Values)
                cell.Dispose();
            __Cells.Clear();
            __ContentSize = null;
        }
        /// <summary>
        /// Do this panelu přidá další buňku pro zobrazení dat: label, control, jméno datové property, pozice.
        /// </summary>
        /// <param name="controlType">Typ prvku</param>
        /// <param name="label">Textový popisek</param>
        /// <param name="propertyName">Název property z datového objektu, na který je buňka navázaná</param>
        /// <param name="left">Souřadnice Left</param>
        /// <param name="top">Souřadnice Top</param>
        /// <param name="width">Šířka</param>
        /// <param name="height">Výška</param>
        /// <param name="initializer">Metoda, která inicializuje control</param>
        /// <param name="validator">Metoda, která validuje zadanou textovou hodnotu</param>
        public Control AddCell(ControlType controlType, string label, string propertyName, int left, int top, int width, int? height = null, Action<Control> initializer = null, Func<string, string> validator = null)
        {
            int x = left;
            int y = top;
            return _AddCell(controlType, label, propertyName, ref x, ref y, width, height, initializer, validator);
        }
        /// <summary>
        /// Do this panelu přidá další buňku pro zobrazení dat: label, control, jméno datové property, pozice.
        /// </summary>
        /// <param name="controlType">Typ prvku</param>
        /// <param name="label">Textový popisek</param>
        /// <param name="propertyName">Název property z datového objektu, na který je buňka navázaná</param>
        /// <param name="y">ref Souřadnice Y</param>
        /// <param name="top">Souřadnice Top</param>
        /// <param name="width">Šířka</param>
        /// <param name="height">Výška</param>
        /// <param name="initializer">Metoda, která inicializuje control</param>
        /// <param name="validator">Metoda, která validuje zadanou textovou hodnotu</param>
        public Control AddCell(ControlType controlType, string label, string propertyName, ref int x, int top, int width, int? height = null, Action<Control> initializer = null, Func<string, string> validator = null)
        {
            int y = top;
            return _AddCell(controlType, label, propertyName, ref x, ref y, width, height, initializer, validator);
        }
        /// <summary>
        /// Do this panelu přidá další buňku pro zobrazení dat: label, control, jméno datové property, pozice.
        /// </summary>
        /// <param name="controlType">Typ prvku</param>
        /// <param name="label">Textový popisek</param>
        /// <param name="propertyName">Název property z datového objektu, na který je buňka navázaná</param>
        /// <param name="left">Souřadnice Left</param>
        /// <param name="x">ref Souřadnice X</param>
        /// <param name="width">Šířka</param>
        /// <param name="height">Výška</param>
        /// <param name="initializer">Metoda, která inicializuje control</param>
        /// <param name="validator">Metoda, která validuje zadanou textovou hodnotu</param>
        public Control AddCell(ControlType controlType, string label, string propertyName, int left, ref int y, int width, int? height = null, Action<Control> initializer = null, Func<string, string> validator = null)
        {
            int x = left;
            return _AddCell(controlType, label, propertyName, ref x, ref y, width, height, initializer, validator);
        }
        /// <summary>
        /// Do this panelu přidá další buňku pro zobrazení dat: label, control, jméno datové property, pozice.
        /// </summary>
        /// <param name="controlType">Typ prvku</param>
        /// <param name="label">Textový popisek</param>
        /// <param name="propertyName">Název property z datového objektu, na který je buňka navázaná</param>
        /// <param name="x">Souřadnice X vlastního controlu. Pokud control bude mít samostatný control pro Label, pak bude Label na souřadnici X + 2</param>
        /// <param name="y">Souřadnice Y vlastního controlu. Pokud control bude mít samostatný control pro Label, pak bude Label na souřadnici Y - <see cref="LabelHeight"/> = 15</param>
        /// <param name="width"></param>
        /// <param name="height">Výška</param>
        /// <param name="initializer">Metoda, která inicializuje control</param>
        /// <param name="validator">Metoda, která validuje zadanou textovou hodnotu</param>
        private Control _AddCell(ControlType controlType, string textLabel, string propertyName, ref int x, ref int y, int width, int? height, Action<Control> initializer, Func<string, string> validator)
        {
            Control result = null;
            CellInfo cell = new CellInfo(propertyName, controlType);
            cell.Validator = validator;
            
            // Pokud daný typ controlu použije Label, přidáme jej nyní (tj. pokud je dán text Labelu, a typ controlu je jiný než (Label + CheckBox):
            bool addLabelControl = (!String.IsNullOrEmpty(textLabel) && (!(controlType == ControlType.Label || controlType == ControlType.CheckBox)));
            if (addLabelControl)
            {
                cell.LabelControl = ControlSupport.CreateControl(ControlType.Label, textLabel, this);
                cell.LabelBounds = new Rectangle(x + 2, y - LabelHeight, width - 6, LabelHeight);
                LayoutContentOne(cell.LabelControl, cell.LabelBounds.Value);
                result = cell.LabelControl;
            }

            // Vlastní control:
            if (controlType != ControlType.None)
            {
                string text = (addLabelControl ? "" : textLabel);
                cell.InputControl = ControlSupport.CreateControl(controlType, text, this);
                cell.InputControl.Validating += cell.InputControlValidating;
                if (cell.InputControl != null && height.HasValue) cell.InputControl.Height = height.Value;
                cell.InputBounds = new Rectangle(x, y, width, cell.InputControl?.Height ?? 20);
                LayoutContentOne(cell.InputControl, cell.InputBounds.Value);
                result = cell.InputControl;

                initializer?.Invoke(cell.InputControl);

                // Posunu souřadnice X a Y na konec controlu:
                x = cell.InputBounds.Value.Right + SpacingX;
                y = cell.InputBounds.Value.Bottom + SpacingY;
            }
            else if (addLabelControl)
            {   // Pokud nemám vlastní control, ale mám Label, pak posunu X a Y podle velikosti Labelu:
                x = cell.LabelBounds.Value.Right + SpacingX;
                y = cell.LabelBounds.Value.Bottom + SpacingY;
            }

            __Cells.Add(propertyName, cell);

            return result;
        }
        /// <summary>
        /// Výška samostatného controlu Label, definuje víceméně vertikální odstup Labelu nad Controlem ve směru Y = svisle
        /// </summary>
        public int LabelHeight { get { return __LabelHeight; } set { __LabelHeight = (value < 7 ? 7 : (value > 24 ? 24 : value)); } } private int __LabelHeight = 15;
        /// <summary>
        /// Odstup dvou sousedních prvků ve směru X = vodorovně
        /// </summary>
        public int SpacingX { get { return __SpacingX; } set { __SpacingX = (value < 0 ? 0 : (value > 24 ? 24 : value)); } } private int __SpacingX = 8;
        /// <summary>
        /// Odstup dvou sousedních prvků ve směru Y = svisle
        /// </summary>
        public int SpacingY { get { return __SpacingY; } set { __SpacingY = (value < 0 ? 0 : (value > 24 ? 24 : value)); } } private int __SpacingY = 4;
        /// <summary>
        /// Vyhledá vstupní control pro danou property.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="control"></param>
        /// <returns></returns>
        public bool TryGetControl(string propertyName, out Control control)
        {
            if (!String.IsNullOrEmpty(propertyName) && __Cells.TryGetValue(propertyName, out var cell))
            {
                control = cell.InputControl;
                return true;
            }
            control = null;
            return false;
        }
        /// <summary>
        /// Buňky
        /// </summary>
        private Dictionary<string, CellInfo> __Cells;
        /// <summary>
        /// Jedna buňka s daty
        /// </summary>
        protected class CellInfo : IDisposable
        {
            public CellInfo(string propertyName, ControlType inputType)
            {
                PropertyName = propertyName;
                InputType = inputType;
            }
            public string PropertyName { get; private set; }
            public ControlType InputType { get; private set; }
            public Rectangle? LabelBounds { get; set; }
            public Rectangle? InputBounds { get; set; }
            public Control LabelControl { get; set; }
            public Control InputControl { get; set; }
            public Func<string, string> Validator { get; set; }
            /// <summary>
            /// Vstupní control právě provádí validaci
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            /// <exception cref="NotImplementedException"></exception>
            public void InputControlValidating(object sender, System.ComponentModel.CancelEventArgs e)
            {
                if (this.Validator != null)
                {
                    string inputText = InputControl.Text;
                    string outputText = this.Validator(inputText);
                    InputControl.Text = outputText;
                }
            }
            /// <summary>
            /// Dispose prvku
            /// </summary>
            public void Dispose()
            {
                disposeControl(LabelControl);
                disposeControl(InputControl);

                PropertyName = null;
                InputType = ControlType.None;
                LabelControl = null;
                InputControl = null;

                void disposeControl(Control control)
                {
                    if (control is null) return;
                    if (control.Parent != null)
                        control.Parent.Controls.Remove(control);
                    control.Dispose();
                }
            }
        }
        #endregion
        #region Fyzické umístění controlů v panelu podle buněk a paddingu
        /// <summary>
        /// Zajistí umístění obsahu všech buněk do aktuálního prostoru se zohledněním <see cref="ContentPadding"/>, 
        /// a současně napočte sumární velikost <see cref=""/>
        /// 
        /// </summary>
        /// <param name="force"></param>
        protected virtual void LayoutContent(bool force)
        {
            __ContentSize = null;
            foreach (var cell in __Cells)
                LayoutContentOne(cell.Value);
        }
        /// <summary>
        /// Umístí daný prvek <paramref name="control"/> na danou relativní souřadnici <paramref name="relativeBounds"/>, k níž přičte <see cref="ContentPadding"/>.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="relativeBounds"></param>
        protected virtual void LayoutContentOne(CellInfo cell)
        {
            if (cell != null)
            {
                if (cell.LabelControl != null)
                    LayoutContentOne(cell.LabelControl, cell.LabelBounds.Value);

                if (cell.InputControl != null)
                    LayoutContentOne(cell.InputControl, cell.InputBounds.Value);
            }
        }
        /// <summary>
        /// Umístí daný prvek <paramref name="control"/> na danou relativní souřadnici <paramref name="relativeBounds"/>, k níž přičte <see cref="ContentPadding"/>.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="relativeBounds"></param>
        protected virtual void LayoutContentOne(Control control, Rectangle relativeBounds)
        {
            if (control is null) return;

            var padding = this.ContentPadding;
            var bounds = new Rectangle(relativeBounds.X + padding.Left, relativeBounds.Y + padding.Top, relativeBounds.Width, relativeBounds.Height);
            control.Bounds = bounds;

            // Souřadnice Bounds.End + Padding = Right a Bottom:
            int cr = bounds.Right + padding.Right;
            int cb = bounds.Bottom + padding.Bottom;

            if (__ContentSize.HasValue)
            {   // Aktuální souhrnná velikost obsahu:
                int sw = __ContentSize.Value.Width;
                int sh = __ContentSize.Value.Height;
                // Nová souhrnná velikost obsahu = ta větší z (Bounds, Content):
                int nw = (cr > sw ? cr : sw);
                int nh = (cb > sh ? cb : sh);
                // Pokud nová velikost je v některém směru větší než dosavadní, tak uložíme upravenou ContentSize:
                if (nw > sw || nh > sh) __ContentSize = new Size(nw, nh);
            }
            else
            {   // První control sám určuje ContentSize:
                __ContentSize = new Size(cr, cb);
            }
        }
        #endregion
        #region Datový objekt, načtení a uložení dat
        /// <summary>
        /// Datový objekt obsahující data
        /// </summary>
        public object DataObject { get { return __DataObject; } set { __DataObject = value; this.DataShow(); } } private object __DataObject;
        /// <summary>
        /// Převezme data z properties z objektu <see cref="DataObject"/> a vloží je do vizuálních controlů.
        /// </summary>
        protected virtual void DataShow()
        {
            var mapItems = CreateCellPropertyMap( PropertyMode.Read);
            if (mapItems.Count > 0)
            {
                var dataObject = __DataObject;
                foreach (var mapItem in mapItems)
                {
                    object value = mapItem.Item2.GetValue(dataObject);
                    ValueShow(mapItem.Item1.InputType, mapItem.Item1.InputControl, value);
                }
            }
        }
        /// <summary>
        /// Získá data z vizuálních controlů a uloží je do properties do objektu <see cref="DataObject"/>
        /// </summary>
        protected virtual void DataStore()
        {
            var mapItems = CreateCellPropertyMap(PropertyMode.Write);
            if (mapItems.Count > 0)
            {
                var dataObject = __DataObject;
                foreach (var mapItem in mapItems)
                {
                    object value = ValueStore(mapItem.Item1.InputType, mapItem.Item1.InputControl, mapItem.Item2.PropertyType);
                    mapItem.Item2.SetValue(dataObject, value);
                }
            }

            this.DataStoreAfter?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Událost vyvolaná po uložení dat
        /// </summary>
        public event EventHandler DataStoreAfter;
        /// <summary>
        /// Metoda vrátí pole obsahující pár informací: Item1 = data o buňce, Item2 = PropertyInfo pro aktuální datový objekt
        /// </summary>
        /// <param name="propertyMode"></param>
        /// <returns></returns>
        protected List<Tuple<CellInfo, System.Reflection.PropertyInfo>> CreateCellPropertyMap(PropertyMode propertyMode)
        {
            var result = new List<Tuple<CellInfo, System.Reflection.PropertyInfo>>();

            var dataObject = __DataObject;
            if (dataObject != null)
            {
                bool needGetMethod = (propertyMode == PropertyMode.Read || propertyMode == PropertyMode.Both);
                bool needSetMethod = (propertyMode == PropertyMode.Read || propertyMode == PropertyMode.Both);
                var properties = (dataObject != null ? dataObject.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) : null);
                foreach (var cell in __Cells.Values)
                {
                    if (cell.InputControl is null) continue;

                    var property = properties.FirstOrDefault(p => p.Name == cell.PropertyName);
                    if (property is null) continue;
                    if (needGetMethod && property.GetGetMethod() is null) continue;
                    if (needSetMethod && property.GetSetMethod() is null) continue;

                    result.Add(new Tuple<CellInfo, System.Reflection.PropertyInfo>(cell, property));
                }
            }
            return result;
        }
        /// <summary>
        /// Režim práce s daty v property
        /// </summary>
        protected enum PropertyMode { None, Read, Write, Both }
        /// <summary>
        /// Metoda vloží dodanou hodnotu (načtenou z datového objektu) do daného vizuálního controlu
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="inputControl"></param>
        /// <param name="value"></param>
        protected static void ValueShow(ControlType inputType, Control inputControl, object value)
        {
            if (inputControl is IValueStorage iValueStorage)
                iValueStorage.Value = value;
            else
                inputControl.Text = Conversion.ToString(value);
        }
        /// <summary>
        /// Metoda načte hodnotu zadanou do daného vizuálního controlu a vrátí ji konvertovanou do cílového datového typu
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="inputControl"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        protected static object ValueStore(ControlType inputType, Control inputControl, Type valueType)
        {
            object value = null;
            if (inputControl is IValueStorage iValueStorage)
                value = Conversion.ToType(iValueStorage.Value, valueType);
            else
                value = Conversion.ToType(inputControl.Text, valueType);
            return value;
        }
        #endregion
    }
    #endregion
    #region Interface
    /// <summary>
    /// Vlastnosti DataControlu pro jeho snadné použití v <see cref="DialogForm"/>
    /// </summary>
    public interface IDataControl
    {
        /// <summary>
        /// Optimální velikost, určená explicitně zvenku
        /// </summary>
        Size? OptimalSize { get; }
        /// <summary>
        /// Aktuální velikost, určená podle aktuálnícho obsahu Controlů a okrajů Padding
        /// </summary>
        Size? ContentSize { get; }
        /// <summary>
        /// Přítomné buttony v odpovídajícím pořadí
        /// </summary>
        DialogButtonType[] Buttons { get; }
        /// <summary>
        /// Typ buttonu, který reprezentuje Accept = klávesa Enter na formuláři
        /// </summary>
        DialogButtonType AcceptButtonType { get; }
        /// <summary>
        /// Typ buttonu, který reprezentuje Cancel = klávesa Escape na formuláři
        /// </summary>
        DialogButtonType CancelButtonType { get; }
        /// <summary>
        /// Po kliknutí na některý button
        /// </summary>
        /// <param name="args"></param>
        void DialogButtonClicked(DialogButtonEventArgs args);
    }
    /// <summary>
    /// Předpis pro třídu, která dokáže akceptovat a vydat hodnotu. Typicky pro Control v rámci <see cref="IDataControl"/>.
    /// </summary>
    public interface IValueStorage
    {
        object Value { get; set; }
    }
    #endregion
}
