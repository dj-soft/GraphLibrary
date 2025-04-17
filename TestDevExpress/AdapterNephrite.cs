using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using WinForm = System.Windows.Forms;
using WinFormServices.Drawing;
using AsolDX = Noris.Clients.Win.Components.AsolDX;


#region Noris classes and support
namespace Noris.Clients.Controllers
{
    /// <summary>
    /// Simulace Green
    /// </summary>
    public class DataFormDataSourceFacade
    {
        /// <summary>
        /// Simulace Green
        /// </summary>
        public class TabsFacade
        {
            /// <summary>
            /// Simulace Green
            /// </summary>
            public class LabeledTabInfo
            {
                /// <summary>
                /// Jméno
                /// </summary>
                public string Name;
                /// <summary>
                /// Popisek
                /// </summary>
                public string Label;
            }
        }
    }
}


namespace Noris.Clients.Win.Components
{
    #region class DebugControl : Podpora pro debugování vizuálního controlu   { DAJ 2020-02-07 }
    /// <summary>
    /// Debugování práce s libovolným controlem.
    /// <para/>
    /// Metoda <see cref="DebugControl.GetControlStructure(WinForm.Control, string, bool)"/> zmapuje dodaný control jeho Child Controls
    /// a vrátí textovou tabulku, obsahující strukturu daného controlu s viditelnou stromovu strukturou a s údaji o jednotlivých controlech.
    /// <para/>
    /// Metoda <see cref="DebugControl.GetObjectStructure(object, string, bool)"/> v daném objektu najde <see cref="WinForm.Control"/>, zmapuje jeho Child Controls
    /// a vrátí tabulku obsahující strukturu daného controlu s viditelnou stromovu strukturou a s údaji o jednotlivých controlech.
    /// </summary>
    public static class DebugControl
    {
        #region Mapování struktury controlu
        /// <summary>
        /// Z this controlu vygeneruje jeho mapu struktury, včetně jeho Child Controlů.
        /// </summary>
        /// <param name="control">Control, jehož mapa se bude generovat</param>
        /// <param name="delimiter">Oddělovač sloupců, default = dvě mezery</param>
        /// <param name="withTopParent">Vyhledat linku k Top parentu? Default je false</param>
        /// <returns></returns>
        public static string GetControlStructure(this WinForm.Control control, string delimiter = null, bool withTopParent = false)
        {
            return GetObjectStructure(control, delimiter, withTopParent, null);
        }
        /// <summary>
        /// Z this controlu vygeneruje jeho mapu struktury, včetně jeho Child Controlů.
        /// </summary>
        /// <param name="control">Control, jehož mapa se bude generovat</param>
        /// <param name="delimiter">Oddělovač sloupců, default = dvě mezery</param>
        /// <param name="withTopParent">Vyhledat linku k Top parentu? Default je false</param>
        /// <param name="filter">Filtrační funkce pro jednotlivé objekty. Default = null = všechny.</param>
        /// <returns></returns>
        public static string GetControlStructure(this WinForm.Control control, string delimiter, bool withTopParent, Func<Control, ScanFilterMode> filter)
        {
            return GetObjectStructure(control, delimiter, withTopParent, filter);
        }
        /// <summary>
        /// Vrátí textovou mapu struktury daného controlu, včetně jeho Child Controlů.
        /// </summary>
        /// <param name="anything">Cokoliv, metoda se pokusí detekovat o co jde a najít v tom nějaký <see cref="WinForm.Control"/></param>
        /// <param name="delimiter">Oddělovač sloupců, default = dvě mezery</param>
        /// <param name="withTopParent">Vyhledat linku k Top parentu? Default je false</param>
        /// <returns></returns>
        public static string GetObjectStructure(object anything, string delimiter = null, bool withTopParent = false)
        {
            return GetObjectStructure(anything, delimiter, withTopParent, null);
        }
        /// <summary>
        /// Vrátí textovou mapu struktury daného controlu, včetně jeho Child Controlů.
        /// </summary>
        /// <param name="anything">Cokoliv, metoda se pokusí detekovat o co jde a najít v tom nějaký <see cref="WinForm.Control"/></param>
        /// <param name="delimiter">Oddělovač sloupců, default = dvě mezery</param>
        /// <param name="withTopParent">Vyhledat linku k Top parentu? Default je false</param>
        /// <param name="filter">Filtrační funkce pro jednotlivé objekty. Default = null = všechny.</param>
        /// <returns></returns>
        public static string GetObjectStructure(object anything, string delimiter, bool withTopParent, Func<Control, ScanFilterMode> filter)
        {
            anything = _GetControlFrom(anything);
            if (anything is null) return "NULL";
            if (!(anything is WinForm.Control control)) return _GetFullTypeName(anything) + " does not recognized as System.Windows.Form.Control";

            if (withTopParent)
                control = SearchForTopParent(control);

            var items = new List<ItemInfo>();
            _AddMapItems(items, control, "0", 0, Point.Empty, filter);
            return ItemInfo.CreateMap(items, delimiter);
        }
        /// <summary>
        /// Metoda vrátí nejvyšší parent nalezený z daného controlu.
        /// Pokud control nemá parenta, je vrácen vstupní control.
        /// Pokud je předán control = null, je vráceno null (to je jediný případ).
        /// Pokud control už finálně bydlí na formuláři, je vrácen tento Form.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="searchFirstForm">Hledat jen po první instanci typu <see cref="WinForm.Form"/>, default = true, false = najde až WDesktop</param>
        /// <returns></returns>
        public static WinForm.Control SearchForTopParent(this WinForm.Control control, bool searchFirstForm = true)
        {
            WinForm.Control parent = control;
            while (parent != null)
            {
                if (searchFirstForm && parent is WinForm.Form) break;     // Našli jsme Form, a to nám postačuje
                if (parent.Parent is null) break;                         // Aktuální objekt (parent) už nemá svého Parenta, tím končíme vždy
                parent = parent.Parent;
            }
            return parent;
        }
        /// <summary>
        /// Metoda vrátí první vyhovující Parent objekt z this controlu, který vyhovuje zadané podmínce.
        /// Pokud je předán control = null, je vráceno null.
        /// Pokud control nemá parenta (pokud <paramref name="includeControl"/> je false), nebo žádný parent nevyhovuje filtru, je vrácen null.
        /// Vstupní control se běžně netestuje (hledáme Parenta), ale pokud zadáme optional parametr <paramref name="includeControl"/>, pak se vstupní control testuje a může být vrácen.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="filter"></param>
        /// <param name="result"></param>
        /// <param name="includeControl"></param>
        /// <returns></returns>
        public static bool TryFindForParent(this Control control, Func<Control, bool> filter, out Control result, bool includeControl = false)
        {
            result = null;
            if (control == null) return false;
            WinForm.Control test = (includeControl ? control : control.Parent);
            while (test != null)
            {
                if (filter(test))
                {
                    result = test;
                    return true;
                }
                test = test.Parent;
            }
            return false;
        }
        /// <summary>
        /// Varianta filtrování pro scanované objekty
        /// </summary>
        public enum ScanFilterMode
        {
            /// <summary>
            /// Defaultní = vypiš aktuální objekt i jeho Child objekty
            /// </summary>
            Default,
            /// <summary>
            /// Vypiš mě, ale už nescanuj moje Child objekty
            /// </summary>
            HideMyChilds,
            /// <summary>
            /// Nevypisuj mě a tedy ani moje Child objekty
            /// </summary>
            HideMeAndChilds
        }
        #region Privátní část
        /// <summary>
        /// Z něčeho dodaného se pokusí vyhledat a vrátit <see cref="WinForm.Control"/>
        /// </summary>
        /// <param name="anything"></param>
        /// <returns></returns>
        private static object _GetControlFrom(object anything)
        {
            if (anything is null) return null;
            switch (anything)
            {
                case WinForm.Control control: return control;
            }
            return anything;
        }
        /// <summary>
        /// Do daného Listu přidá daný prvek a všechny jeho Child controly, rekurzivně
        /// </summary>
        /// <param name="items"></param>
        /// <param name="control"></param>
        /// <param name="key"></param>
        /// <param name="level"></param>
        /// <param name="origin"></param>
        /// <param name="filter"></param>
        private static void _AddMapItems(List<ItemInfo> items, WinForm.Control control, string key, int level, Point origin, Func<Control, ScanFilterMode> filter)
        {
            if (control is null) return;

            ScanFilterMode scanMode = (filter is null ? ScanFilterMode.Default : filter(control));
            if (scanMode == ScanFilterMode.HideMeAndChilds) return;
            try
            {
                var itemInfo = new ItemInfo(control, key, level, origin);
                items.Add(itemInfo);
            }
            catch { }

            if (control.Controls is null || control.Controls.Count == 0 || scanMode == ScanFilterMode.HideMyChilds) return;

            int childLevel = level + 1;
            Point controlLocation = (control is WinForm.Form ? Point.Empty : control.Bounds.Location);
            Point clientLocation = ItemInfo.GetClientBounds(control).Location;
            Point childOrigin = new Point(origin.X + controlLocation.X + clientLocation.X, origin.Y + controlLocation.Y + clientLocation.Y);
            for (int l = 0; l < control.Controls.Count; l++)
            {
                var child = control.Controls[l];
                string childKey = key + "." + l.ToString();
                _AddMapItems(items, child, childKey, childLevel, childOrigin, filter);
            }
        }
        /// <summary>
        /// Vrátí "Namespace.Name" z typu daného objektu
        /// </summary>
        /// <param name="anything"></param>
        /// <returns></returns>
        private static string _GetFullTypeName(object anything)
        {
            if (anything is null) return "NULL";
            var type = anything.GetType();
            return type.Namespace + "." + type.Name;
        }
        /// <summary>
        /// Data o jednom controlu. Vlastní control není ukládán.
        /// </summary>
        private class ItemInfo
        {
            #region Konstruktor a data
            /// <summary>
            /// Konstruktor.
            /// Vlastní control není ukládán.
            /// </summary>
            /// <param name="control"></param>
            /// <param name="key"></param>
            /// <param name="level"></param>
            /// <param name="origin"></param>
            public ItemInfo(WinForm.Control control, string key, int level, Point origin)
            {
                Level = level;
                Key = key ?? "";
                if (control is null)
                {
                    Type = "";
                    Id = "";
                    ItemIndex = "";
                    Name = "NULL";
                    Text = "";
                    Data = "";
                    ControlRenderParams = "";
                    Dock = "";
                    Bounds = "";
                    AbsoluteBounds = "";
                    ClientRectangle = "";
                    Visible = "";
                    Enabled = "";
                    IsDisposed = "";
                }
                else
                {
                    Rectangle bounds = control.Bounds;
                    Rectangle absBounds = new Rectangle(new Point(origin.X + bounds.Location.X, origin.Y + bounds.Location.Y), bounds.Size);
                    Rectangle clientBounds = GetClientBounds(control);
                    Type = _GetFullTypeName(control);
                    Id = _GetIdFromControl(control);
                    ItemIndex = _GetOrderFromControl(control);
                    Name = _GetNameFromControl(control);
                    Text = _GetTextFromControl(control);
                    Data = _GetDataFromControl(control);
                    ControlRenderParams = _GetCrpFromControl(control);
                    Dock = control.Dock.ToString();
                    Bounds = Rect(bounds);
                    AbsoluteBounds = Rect(absBounds);
                    ClientRectangle = Rect(clientBounds);
                    Visible = YN(control.Visible);
                    Enabled = YN(control.Enabled);
                    IsDisposed = YN(control.IsDisposed);
                }
            }
            public readonly int Level;
            public readonly string Key;
            public readonly string Type;
            public readonly string Id;
            public readonly string ItemIndex;
            public readonly string Name;
            public readonly string Text;
            public readonly string Data;
            public readonly string ControlRenderParams;
            public readonly string Dock;
            public readonly string Bounds;
            public readonly string AbsoluteBounds;
            public readonly string ClientRectangle;
            public readonly string Visible;
            public readonly string Enabled;
            public readonly string IsDisposed;
            #endregion
            #region Vyhledání dat
            /// <summary>
            /// Vrací souřadnice klientského prostoru uvnitř daného controlu = relativně k jeho Location
            /// </summary>
            /// <param name="control"></param>
            /// <returns></returns>
            internal static Rectangle GetClientBounds(WinForm.Control control)
            {
                Rectangle clientRectangle = control.ClientRectangle;
                if (control is WinForm.Form)
                {
                    Point absFormOrigin = control.Location;
                    Point absClientOrigin = control.PointToScreen(Point.Empty);
                    Point relClientOrigin = new Point(absClientOrigin.X - absFormOrigin.X + clientRectangle.X, absClientOrigin.Y - absFormOrigin.Y + clientRectangle.Y);
                    clientRectangle = new Rectangle(relClientOrigin, clientRectangle.Size);
                }
                return clientRectangle;
            }
            /// <summary>
            /// Najde a vrátí jméno v daném controlu
            /// </summary>
            /// <param name="control"></param>
            /// <returns></returns>
            private static string _GetNameFromControl(WinForm.Control control)
            {
                return control.Name ?? "";
            }
            /// <summary>
            /// Vrací Text z daného Controlu. Text zkrátí na nanejvýše 60 znaků, a nahradí znaky Cr, Lf a Tab znaky Mezera.
            /// </summary>
            /// <param name="control"></param>
            /// <returns></returns>
            private static string _GetTextFromControl(WinForm.Control control)
            {
                string text = control.Text;
                if (text is null) text = "";
                if (text.Length > 60) text = text.Substring(0, 57) + "...";
                if (text.Contains("\r")) text = text.Replace("\r", " ");
                if (text.Contains("\n")) text = text.Replace("\n", " ");
                if (text.Contains("\t")) text = text.Replace("\t", " ");
                return text;
            }
            /// <summary>
            /// Najde a vrátí data v daném controlu
            /// </summary>
            /// <param name="control"></param>
            /// <returns></returns>
            private static string _GetDataFromControl(WinForm.Control control)
            {
                return "";
            }
            /// <summary>
            /// Metoda najde a vrátí string popisující ConrolRenderParams daného controlu
            /// </summary>
            /// <param name="control"></param>
            /// <returns></returns>
            private static string _GetCrpFromControl(WinForm.Control control)
            {
                string result = "";
                return result;
            }
            /// <summary>
            /// Metoda najde a vrátí string popisující ID z mapy daného controlu
            /// </summary>
            /// <param name="control"></param>
            /// <returns></returns>
            private static string _GetIdFromControl(WinForm.Control control)
            {
                string result = "";
                return result;
            }
            /// <summary>
            /// Metoda najde a vrátí string popisující Order
            /// </summary>
            /// <param name="control"></param>
            /// <returns></returns>
            private static string _GetOrderFromControl(WinForm.Control control)
            {
                string result = "";
                return result;
            }
           
            #endregion
            #region Formátování výstupu
            /// <summary>
            /// Z dodaných prvků vytvoří textovou mapu a vrátí ji
            /// </summary>
            /// <param name="items">Prvky</param>
            /// <param name="delimiter">Oddělovač sloupců, default = dvě mezery</param>
            /// <returns></returns>
            public static string CreateMap(IEnumerable<ItemInfo> items, string delimiter = null)
            {
                bool align = !(delimiter != null && delimiter == "\t");
                if (delimiter is null) delimiter = "  ";
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                string[] titles = _GetMapTitles();
                int[] widths = _GetMapLength(titles, items, align);
                _AddMapTexts(sb, titles, widths, delimiter, align);
                _AddMapTexts(sb, null, widths, delimiter, true, '-');
                foreach (ItemInfo item in items)
                {
                    string[] values = item._GetMapValues();
                    _AddMapTexts(sb, values, widths, delimiter, align);
                }
                return sb.ToString();
            }
            /// <summary>
            /// Metoda vrátí potřebné šířky jednotlivých sloupců pro formátovaný výpis titulku a daných prvků.
            /// Metoda určí délku textu pro jednotlivé sloupce jako Max() : jednak z textu titulku, a druhak ze všech textů všech dodaných položek.
            /// </summary>
            /// <param name="titles">Texty titulků</param>
            /// <param name="items">Položky s daty</param>
            /// <param name="align">Zarovnat na max délku?</param>
            /// <returns></returns>
            private static int[] _GetMapLength(string[] titles, IEnumerable<ItemInfo> items, bool align)
            {
                int[] result = titles.Select(t => t.Length).ToArray();
                if (align)
                {
                    foreach (ItemInfo item in items)
                    {
                        string[] values = item._GetMapValues();
                        for (int i = 0; i < values.Length; i++)
                        {
                            if (values[i].Length > result[i])
                                result[i] = values[i].Length;
                        }
                    }
                }
                return result;
            }
            /// <summary>
            /// Vrátí titulky hodnot this objektu, bez zarovnání délky
            /// </summary>
            /// <returns></returns>
            private static string[] _GetMapTitles()
            {
                string[] titles = new string[]
                {
                    // nameof(Key), nameof(Type), nameof(Id), nameof(ItemIndex), nameof(Name), nameof(Text), nameof(Data), nameof(ControlRenderParams), nameof(Dock), nameof(Bounds), nameof(AbsoluteBounds), nameof(ClientRectangle), nameof(Visible), nameof(Enabled)
                    nameof(Key), nameof(Type), nameof(ItemIndex), nameof(Name), nameof(Text), nameof(Dock), nameof(Bounds), nameof(AbsoluteBounds), nameof(ClientRectangle), nameof(Visible), nameof(Enabled)
                };
                return titles;
            }
            /// <summary>
            /// Vrátí hodnoty this objektu, bez zarovnání délky
            /// </summary>
            /// <returns></returns>
            private string[] _GetMapValues()
            {
                string[] values = new string[]
                {
                    // Key, Type, Id, ItemIndex, Name, Text, Data, ControlRenderParams, Dock, Bounds, AbsoluteBounds, ClientRectangle, Visible, Enabled
                    Key, Type, ItemIndex, Name, Text, Dock, Bounds, AbsoluteBounds, ClientRectangle, Visible, Enabled
                };
                return values;
            }
            /// <summary>
            /// Do daného stringbuilderu vepíše dodané texty v dané délce, zarovnané na délku daným znakem.
            /// Pokud není dodán text (pole nebo prvek pole je null), nevadí.
            /// </summary>
            /// <param name="sb"></param>
            /// <param name="texts"></param>
            /// <param name="widths"></param>
            /// <param name="delimiter"></param>
            /// <param name="align"></param>
            /// <param name="padChar"></param>
            private static void _AddMapTexts(System.Text.StringBuilder sb, string[] texts, int[] widths, string delimiter, bool align, char padChar = ' ')
            {
                int tl = texts?.Length ?? -1;
                int wl = widths.Length;
                int last = wl - 1;
                for (int l = 0; l <= last; l++)
                {
                    string text = (l < tl ? texts[l] ?? "" : "");
                    if (align)
                        text = text.PadRight(widths[l], padChar);
                    sb.Append(text);
                    if (l < last)
                        sb.Append(delimiter);
                    else
                        sb.AppendLine();
                }
            }
            /// <summary>
            /// Konverze Boolean to String
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            private static string YN(bool value) { return (value ? "True " : "False"); }
            /// <summary>
            /// Konverze Rectangle to String
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            private static string Rect(Rectangle value) { return $"{{X={IntL(value.X)} Y={IntL(value.Y)} W={IntL(value.Width)} H={IntL(value.Height)}}}"; }
            /// <summary>
            /// Konverze Int32 to String s danou nejmenší délkou, číslo doprava
            /// </summary>
            /// <param name="value"></param>
            /// <param name="length"></param>
            /// <returns></returns>
            private static string IntR(int value, int length = 4) { string x = value.ToString(); int l = x.Length; return (l < length ? x.PadLeft(length) : x); }
            /// <summary>
            /// Konverze Int32 to String s danou nejmenší délkou, číslo doprava
            /// </summary>
            /// <param name="value"></param>
            /// <param name="length"></param>
            /// <returns></returns>
            private static string IntL(int value, int length = 4) { string x = value.ToString(); int l = x.Length; return (l < length ? x.PadRight(length) : x); }
            #endregion
        }
        #endregion
        #endregion

    }
    #endregion
    #region Globals
    internal class Globals
    {
        internal static void NotifyToast(Control control, object data, string text, System.Windows.Forms.ToolTipIcon icon)
        {

        }
    }
    #endregion
}


namespace Noris.Clients.Win.Components
{
    /// <summary>
    /// Simulace Green
    /// </summary>
    internal interface IInfragisticsDevExpressSkinableSupport
    {
        void DevexpressSkinChanged(DevExpressToInfragisticsAppearanceConverter.StyleChangedEventArgs arg);
    }
    /// <summary>
    /// Rozhraní pro objekt, který si sám dokáže disposovat svoje controly.
    /// Metoda <see cref="IDisposableContainer.DisposeControls()"/> se pak vyvolá 
    /// namísto toho, aby se volala metoda <see cref="IDisposable.Dispose()"/> jednotlivých controlů.
    /// </summary>
    public interface IDisposableContainer
    {
        /// <summary>
        /// Objekt si má sám disposovat svoje controly a zahodit je z pole <see cref="Control.Controls"/>.
        /// Objekt ale nemá provádět Dispose sám sebe, od toho je metoda <see cref="IDisposable.Dispose()"/>
        /// </summary>
        void DisposeControls();
    }
   
    /// <summary>
    /// Simulace Green
    /// </summary>
    internal sealed class DevExpressToInfragisticsAppearanceConverter
    {
        public sealed class StyleChangedEventArgs { }
    }
    


}
namespace WinFormServices.Drawing
{
    /// <summary>
    /// Simulace Green
    /// </summary>
    public enum UserGraphicsSize 
    {
        /// <summary>None</summary>
        None,
        /// <summary>Small</summary>
        Small,
        /// <summary>Medium</summary>
        Medium,
        /// <summary>Large</summary>
        Large
    }
}


#endregion


