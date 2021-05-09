using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using WinForm = System.Windows.Forms;
using WinFormServices.Drawing;

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
                public string Name;
                public string Label;
            }
        }
    }
}

namespace Noris.Clients.Common
{
    public class SupportScaling
    {
        public static int GetScaledValue(int designValue) { return designValue; }
    }
}

namespace Noris.Clients.Win.Components
{
    public interface IEscapeHandler
    {
        bool HandleEscapeKey();
    }

    #region class DebugControl : Podpora pro debugování vizuálního controlu   { DAJ 2020-02-07 }
    /// <summary>
    /// Debugování práce s libovolným controlem.
    /// Metoda <see cref="DebugControl.GetObjectStructure(object, string, bool)"/> v daném objektu najde <see cref="WinForm.Control"/>, zmapuje jeho Child Controls
    /// a vrátí tabulku obsahující strukturu daného controlu s viditelnou stromovu strukturou a s údaji o jednotlivých controlech.
    /// <para/>
    /// Metoda <see cref="DebugControl.TraceControlChanges(Control)"/> uloží daný control do statické paměti (jeho <see cref="WeakReference"/>), 
    /// a eviduje jeho změny a 
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
            return GetObjectStructure(control, delimiter, withTopParent);
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
            anything = _GetControlFrom(anything);
            if (anything is null) return "NULL";
            if (!(anything is WinForm.Control control)) return _GetFullTypeName(anything) + " does not recognized as System.Windows.Form.Control";
            var items = new List<ItemInfo>();
            _AddMapItems(items, control, "0", 0, Point.Empty);
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
        private static void _AddMapItems(List<ItemInfo> items, WinForm.Control control, string key, int level, Point origin)
        {
            if (control is null) return;
            try
            {
                var itemInfo = new ItemInfo(control, key, level, origin);
                items.Add(itemInfo);
            }
            catch { }

            if (control.Controls is null || control.Controls.Count == 0) return;

            int childLevel = level + 1;
            Point controlLocation = (control is WinForm.Form ? Point.Empty : control.Bounds.Location);
            Point clientLocation = ItemInfo.GetClientBounds(control).Location;
            Point childOrigin = new Point(origin.X + controlLocation.X + clientLocation.X, origin.Y + controlLocation.Y + clientLocation.Y);
            for (int l = 0; l < control.Controls.Count; l++)
            {
                var child = control.Controls[l];
                string childKey = key + "." + l.ToString();
                _AddMapItems(items, child, childKey, childLevel, childOrigin);
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
        #region Překlad WinMSG kódů
        /// <summary>
        /// Vrátí string odpovídající dané Win Message. Obsahuje název události a její hodnotu.
        /// Metoda může vrátit NULL pro zprávy, které se mají ignorovat (viz parametr <paramref name="forceAll"/>).
        /// </summary>
        /// <param name="m"></param>
        /// <param name="forceAll">true = Povinně zpracovat všechny zprávy. Default = false: negenerují se zprávy pro GETTEXTLENGTH(0x000E) a GETTEXT(0x000D).</param>
        /// <returns></returns>
        public static string GetWinMessage(Message m, bool forceAll = false)
        {
            int code = m.Msg;
            if (!forceAll && (code == WM.GETTEXTLENGTH || code == WM.GETTEXT)) return null;

            var wmDict = WmDict;
            string name = (wmDict.TryGetValue(m.Msg, out string value) ? value : "???");
            string message = $"{name} (0x{(m.Msg.ToString("X4"))})";
            return message;
        }
        /// <summary>
        /// Dictionary známých zpráv WinMsg
        /// </summary>
        private static Dictionary<int, string> WmDict { get { if (_WmDict is null) _WmDict = GetWmDict(); return _WmDict; } }
        private static Dictionary<int, string> _WmDict;
        /// <summary>
        /// Vygeneruje a vrátí Dictionary obsahující známé WinMSG kódy a jejich názvy.
        /// POužívá konstanty v třídě <see cref="WM"/>.
        /// </summary>
        /// <returns></returns>
        internal static Dictionary<int, string> GetWmDict()
        {
            Dictionary<int, string> wmDict = new Dictionary<int, string>();
            var fields = typeof(WM).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            foreach (var field in fields)
            {
                string name = field.Name;
                object value = field.GetValue(null);
                if ((value is int key) && !wmDict.ContainsKey(key))
                    wmDict.Add(key, name);
            }
            return wmDict;
        }
        #endregion
    }
    #endregion
    #region WinForm Message codes
    /// <summary>
    /// Windows Messages
    /// Defined in winuser.h from Windows SDK v6.1
    /// Documentation pulled from MSDN.
    /// </summary>
    public class WM
    {
        /// <summary>
        /// The WM_NULL message performs no operation. An application sends the WM_NULL message if it wants to post a message that the recipient window will ignore.
        /// </summary>
        public const int NULL = 0x0000;
        /// <summary>
        /// The WM_CREATE message is sent when an application requests that a window be created by calling the CreateWindowEx or CreateWindow function. (The message is sent before the function returns.) The window procedure of the new window receives this message after the window is created, but before the window becomes visible.
        /// </summary>
        public const int CREATE = 0x0001;
        /// <summary>
        /// The WM_DESTROY message is sent when a window is being destroyed. It is sent to the window procedure of the window being destroyed after the window is removed from the screen.
        /// This message is sent first to the window being destroyed and then to the child windows (if any) as they are destroyed. During the processing of the message, it can be assumed that all child windows still exist.
        /// /// </summary>
        public const int DESTROY = 0x0002;
        /// <summary>
        /// The WM_MOVE message is sent after a window has been moved.
        /// </summary>
        public const int MOVE = 0x0003;
        /// <summary>
        /// The WM_SIZE message is sent to a window after its size has changed.
        /// </summary>
        public const int SIZE = 0x0005;
        /// <summary>
        /// The WM_ACTIVATE message is sent to both the window being activated and the window being deactivated. If the windows use the same input queue, the message is sent synchronously, first to the window procedure of the top-level window being deactivated, then to the window procedure of the top-level window being activated. If the windows use different input queues, the message is sent asynchronously, so the window is activated immediately.
        /// </summary>
        public const int ACTIVATE = 0x0006;
        /// <summary>
        /// The WM_SETFOCUS message is sent to a window after it has gained the keyboard focus.
        /// </summary>
        public const int SETFOCUS = 0x0007;
        /// <summary>
        /// The WM_KILLFOCUS message is sent to a window immediately before it loses the keyboard focus.
        /// </summary>
        public const int KILLFOCUS = 0x0008;
        /// <summary>
        /// The WM_ENABLE message is sent when an application changes the enabled state of a window. It is sent to the window whose enabled state is changing. This message is sent before the EnableWindow function returns, but after the enabled state (WS_DISABLED style bit) of the window has changed.
        /// </summary>
        public const int ENABLE = 0x000A;
        /// <summary>
        /// An application sends the WM_SETREDRAW message to a window to allow changes in that window to be redrawn or to prevent changes in that window from being redrawn.
        /// </summary>
        public const int SETREDRAW = 0x000B;
        /// <summary>
        /// An application sends a WM_SETTEXT message to set the text of a window.
        /// </summary>
        public const int SETTEXT = 0x000C;
        /// <summary>
        /// An application sends a WM_GETTEXT message to copy the text that corresponds to a window into a buffer provided by the caller.
        /// </summary>
        public const int GETTEXT = 0x000D;
        /// <summary>
        /// An application sends a WM_GETTEXTLENGTH message to determine the length, in characters, of the text associated with a window.
        /// </summary>
        public const int GETTEXTLENGTH = 0x000E;
        /// <summary>
        /// The WM_PAINT message is sent when the system or another application makes a request to paint a portion of an application's window. The message is sent when the UpdateWindow or RedrawWindow function is called, or by the DispatchMessage function when the application obtains a WM_PAINT message by using the GetMessage or PeekMessage function.
        /// </summary>
        public const int PAINT = 0x000F;
        /// <summary>
        /// The WM_CLOSE message is sent as a signal that a window or an application should terminate.
        /// </summary>
        public const int CLOSE = 0x0010;
        /// <summary>
        /// The WM_QUERYENDSESSION message is sent when the user chooses to end the session or when an application calls one of the system shutdown functions. If any application returns zero, the session is not ended. The system stops sending WM_QUERYENDSESSION messages as soon as one application returns zero.
        /// After processing this message, the system sends the WM_ENDSESSION message with the wParam parameter set to the results of the WM_QUERYENDSESSION message.
        /// </summary>
        public const int QUERYENDSESSION = 0x0011;
        /// <summary>
        /// The WM_QUERYOPEN message is sent to an icon when the user requests that the window be restored to its previous size and position.
        /// </summary>
        public const int QUERYOPEN = 0x0013;
        /// <summary>
        /// The WM_ENDSESSION message is sent to an application after the system processes the results of the WM_QUERYENDSESSION message. The WM_ENDSESSION message informs the application whether the session is ending.
        /// </summary>
        public const int ENDSESSION = 0x0016;
        /// <summary>
        /// The WM_QUIT message indicates a request to terminate an application and is generated when the application calls the PostQuitMessage function. It causes the GetMessage function to return zero.
        /// </summary>
        public const int QUIT = 0x0012;
        /// <summary>
        /// The WM_ERASEBKGND message is sent when the window background must be erased (for example, when a window is resized). The message is sent to prepare an invalidated portion of a window for painting.
        /// </summary>
        public const int ERASEBKGND = 0x0014;
        /// <summary>
        /// This message is sent to all top-level windows when a change is made to a system color setting.
        /// </summary>
        public const int SYSCOLORCHANGE = 0x0015;
        /// <summary>
        /// The WM_SHOWWINDOW message is sent to a window when the window is about to be hidden or shown.
        /// </summary>
        public const int SHOWWINDOW = 0x0018;
        /// <summary>
        /// An application sends the WM_WININICHANGE message to all top-level windows after making a change to the WIN.INI file. The SystemParametersInfo function sends this message after an application uses the function to change a setting in WIN.INI.
        /// Note  The WM_WININICHANGE message is provided only for compatibility with earlier versions of the system. Applications should use the WM_SETTINGCHANGE message.
        /// </summary>
        public const int WININICHANGE = 0x001A;
        /// <summary>
        /// An application sends the WM_WININICHANGE message to all top-level windows after making a change to the WIN.INI file. The SystemParametersInfo function sends this message after an application uses the function to change a setting in WIN.INI.
        /// Note  The WM_WININICHANGE message is provided only for compatibility with earlier versions of the system. Applications should use the WM_SETTINGCHANGE message.
        /// </summary>
        public const int SETTINGCHANGE = WININICHANGE;
        /// <summary>
        /// The WM_DEVMODECHANGE message is sent to all top-level windows whenever the user changes device-mode settings.
        /// </summary>
        public const int DEVMODECHANGE = 0x001B;
        /// <summary>
        /// The WM_ACTIVATEAPP message is sent when a window belonging to a different application than the active window is about to be activated. The message is sent to the application whose window is being activated and to the application whose window is being deactivated.
        /// </summary>
        public const int ACTIVATEAPP = 0x001C;
        /// <summary>
        /// An application sends the WM_FONTCHANGE message to all top-level windows in the system after changing the pool of font resources.
        /// </summary>
        public const int FONTCHANGE = 0x001D;
        /// <summary>
        /// A message that is sent whenever there is a change in the system time.
        /// </summary>
        public const int TIMECHANGE = 0x001E;
        /// <summary>
        /// The WM_CANCELMODE message is sent to cancel certain modes, such as mouse capture. For example, the system sends this message to the active window when a dialog box or message box is displayed. Certain functions also send this message explicitly to the specified window regardless of whether it is the active window. For example, the EnableWindow function sends this message when disabling the specified window.
        /// </summary>
        public const int CANCELMODE = 0x001F;
        /// <summary>
        /// The WM_SETCURSOR message is sent to a window if the mouse causes the cursor to move within a window and mouse input is not captured.
        /// </summary>
        public const int SETCURSOR = 0x0020;
        /// <summary>
        /// The WM_MOUSEACTIVATE message is sent when the cursor is in an inactive window and the user presses a mouse button. The parent window receives this message only if the child window passes it to the DefWindowProc function.
        /// </summary>
        public const int MOUSEACTIVATE = 0x0021;
        /// <summary>
        /// The WM_CHILDACTIVATE message is sent to a child window when the user clicks the window's title bar or when the window is activated; moved; or sized.
        /// </summary>
        public const int CHILDACTIVATE = 0x0022;
        /// <summary>
        /// The WM_QUEUESYNC message is sent by a computer-based training (CBT) application to separate user-input messages from other messages sent through the WH_JOURNALPLAYBACK Hook procedure.
        /// </summary>
        public const int QUEUESYNC = 0x0023;
        /// <summary>
        /// The WM_GETMINMAXINFO message is sent to a window when the size or position of the window is about to change. An application can use this message to override the window's default maximized size and position, or its default minimum or maximum tracking size.
        /// </summary>
        public const int GETMINMAXINFO = 0x0024;
        /// <summary>
        /// Windows NT 3.51 and earlier: The WM_PAINTICON message is sent to a minimized window when the icon is to be painted. This message is not sent by newer versions of Microsoft Windows; except in unusual circumstances explained in the Remarks.
        /// </summary>
        public const int PAINTICON = 0x0026;
        /// <summary>
        /// Windows NT 3.51 and earlier: The WM_ICONERASEBKGND message is sent to a minimized window when the background of the icon must be filled before painting the icon. A window receives this message only if a class icon is defined for the window; otherwise, WM_ERASEBKGND is sent. This message is not sent by newer versions of Windows.
        /// </summary>
        public const int ICONERASEBKGND = 0x0027;
        /// <summary>
        /// The WM_NEXTDLGCTL message is sent to a dialog box procedure to set the keyboard focus to a different control in the dialog box.
        /// </summary>
        public const int NEXTDLGCTL = 0x0028;
        /// <summary>
        /// The WM_SPOOLERSTATUS message is sent from Print Manager whenever a job is added to or removed from the Print Manager queue.
        /// </summary>
        public const int SPOOLERSTATUS = 0x002A;
        /// <summary>
        /// The WM_DRAWITEM message is sent to the parent window of an owner-drawn button, combo box, list box, or menu when a visual aspect of the button, combo box, list box, or menu has changed.
        /// </summary>
        public const int DRAWITEM = 0x002B;
        /// <summary>
        /// The WM_MEASUREITEM message is sent to the owner window of a combo box, list box, list view control, or menu item when the control or menu is created.
        /// </summary>
        public const int MEASUREITEM = 0x002C;
        /// <summary>
        /// Sent to the owner of a list box or combo box when the list box or combo box is destroyed or when items are removed by the LB_DELETESTRING, LB_RESETCONTENT, CB_DELETESTRING, or CB_RESETCONTENT message. The system sends a WM_DELETEITEM message for each deleted item. The system sends the WM_DELETEITEM message for any deleted list box or combo box item with nonzero item data.
        /// </summary>
        public const int DELETEITEM = 0x002D;
        /// <summary>
        /// Sent by a list box with the LBS_WANTKEYBOARDINPUT style to its owner in response to a WM_KEYDOWN message.
        /// </summary>
        public const int VKEYTOITEM = 0x002E;
        /// <summary>
        /// Sent by a list box with the LBS_WANTKEYBOARDINPUT style to its owner in response to a WM_CHAR message.
        /// </summary>
        public const int CHARTOITEM = 0x002F;
        /// <summary>
        /// An application sends a WM_SETFONT message to specify the font that a control is to use when drawing text.
        /// </summary>
        public const int SETFONT = 0x0030;
        /// <summary>
        /// An application sends a WM_GETFONT message to a control to retrieve the font with which the control is currently drawing its text.
        /// </summary>
        public const int GETFONT = 0x0031;
        /// <summary>
        /// An application sends a WM_SETHOTKEY message to a window to associate a hot key with the window. When the user presses the hot key; the system activates the window.
        /// </summary>
        public const int SETHOTKEY = 0x0032;
        /// <summary>
        /// An application sends a WM_GETHOTKEY message to determine the hot key associated with a window.
        /// </summary>
        public const int GETHOTKEY = 0x0033;
        /// <summary>
        /// The WM_QUERYDRAGICON message is sent to a minimized (iconic) window. The window is about to be dragged by the user but does not have an icon defined for its class. An application can return a handle to an icon or cursor. The system displays this cursor or icon while the user drags the icon.
        /// </summary>
        public const int QUERYDRAGICON = 0x0037;
        /// <summary>
        /// The system sends the WM_COMPAREITEM message to determine the relative position of a new item in the sorted list of an owner-drawn combo box or list box. Whenever the application adds a new item, the system sends this message to the owner of a combo box or list box created with the CBS_SORT or LBS_SORT style.
        /// </summary>
        public const int COMPAREITEM = 0x0039;
        /// <summary>
        /// Active Accessibility sends the WM_GETOBJECT message to obtain information about an accessible object contained in a server application.
        /// Applications never send this message directly. It is sent only by Active Accessibility in response to calls to AccessibleObjectFromPoint, AccessibleObjectFromEvent, or AccessibleObjectFromWindow. However, server applications handle this message.
        /// </summary>
        public const int GETOBJECT = 0x003D;
        /// <summary>
        /// The WM_COMPACTING message is sent to all top-level windows when the system detects more than 12.5 percent of system time over a 30- to 60-second interval is being spent compacting memory. This indicates that system memory is low.
        /// </summary>
        public const int COMPACTING = 0x0041;
        /// <summary>
        /// WM_COMMNOTIFY is Obsolete for Win32-Based Applications
        /// </summary>
        [Obsolete]
        public const int COMMNOTIFY = 0x0044;
        /// <summary>
        /// The WM_WINDOWPOSCHANGING message is sent to a window whose size, position, or place in the Z order is about to change as a result of a call to the SetWindowPos function or another window-management function.
        /// </summary>
        public const int WINDOWPOSCHANGING = 0x0046;
        /// <summary>
        /// The WM_WINDOWPOSCHANGED message is sent to a window whose size, position, or place in the Z order has changed as a result of a call to the SetWindowPos function or another window-management function.
        /// </summary>
        public const int WINDOWPOSCHANGED = 0x0047;
        /// <summary>
        /// Notifies applications that the system, typically a battery-powered personal computer, is about to enter a suspended mode.
        /// Use: POWERBROADCAST
        /// </summary>
        [Obsolete]
        public const int POWER = 0x0048;
        /// <summary>
        /// An application sends the WM_COPYDATA message to pass data to another application.
        /// </summary>
        public const int COPYDATA = 0x004A;
        /// <summary>
        /// The WM_CANCELJOURNAL message is posted to an application when a user cancels the application's journaling activities. The message is posted with a NULL window handle.
        /// </summary>
        public const int CANCELJOURNAL = 0x004B;
        /// <summary>
        /// Sent by a common control to its parent window when an event has occurred or the control requires some information.
        /// </summary>
        public const int NOTIFY = 0x004E;
        /// <summary>
        /// The WM_INPUTLANGCHANGEREQUEST message is posted to the window with the focus when the user chooses a new input language, either with the hotkey (specified in the Keyboard control panel application) or from the indicator on the system taskbar. An application can accept the change by passing the message to the DefWindowProc function or reject the change (and prevent it from taking place) by returning immediately.
        /// </summary>
        public const int INPUTLANGCHANGEREQUEST = 0x0050;
        /// <summary>
        /// The WM_INPUTLANGCHANGE message is sent to the topmost affected window after an application's input language has been changed. You should make any application-specific settings and pass the message to the DefWindowProc function, which passes the message to all first-level child windows. These child windows can pass the message to DefWindowProc to have it pass the message to their child windows, and so on.
        /// </summary>
        public const int INPUTLANGCHANGE = 0x0051;
        /// <summary>
        /// Sent to an application that has initiated a training card with Microsoft Windows Help. The message informs the application when the user clicks an authorable button. An application initiates a training card by specifying the HELP_TCARD command in a call to the WinHelp function.
        /// </summary>
        public const int TCARD = 0x0052;
        /// <summary>
        /// Indicates that the user pressed the F1 key. If a menu is active when F1 is pressed, WM_HELP is sent to the window associated with the menu; otherwise, WM_HELP is sent to the window that has the keyboard focus. If no window has the keyboard focus, WM_HELP is sent to the currently active window.
        /// </summary>
        public const int HELP = 0x0053;
        /// <summary>
        /// The WM_USERCHANGED message is sent to all windows after the user has logged on or off. When the user logs on or off, the system updates the user-specific settings. The system sends this message immediately after updating the settings.
        /// </summary>
        public const int USERCHANGED = 0x0054;
        /// <summary>
        /// Determines if a window accepts ANSI or Unicode structures in the WM_NOTIFY notification message. WM_NOTIFYFORMAT messages are sent from a common control to its parent window and from the parent window to the common control.
        /// </summary>
        public const int NOTIFYFORMAT = 0x0055;
        /// <summary>
        /// The WM_CONTEXTMENU message notifies a window that the user clicked the right mouse button (right-clicked) in the window.
        /// </summary>
        public const int CONTEXTMENU = 0x007B;
        /// <summary>
        /// The WM_STYLECHANGING message is sent to a window when the SetWindowLong function is about to change one or more of the window's styles.
        /// </summary>
        public const int STYLECHANGING = 0x007C;
        /// <summary>
        /// The WM_STYLECHANGED message is sent to a window after the SetWindowLong function has changed one or more of the window's styles
        /// </summary>
        public const int STYLECHANGED = 0x007D;
        /// <summary>
        /// The WM_DISPLAYCHANGE message is sent to all windows when the display resolution has changed.
        /// </summary>
        public const int DISPLAYCHANGE = 0x007E;
        /// <summary>
        /// The WM_GETICON message is sent to a window to retrieve a handle to the large or small icon associated with a window. The system displays the large icon in the ALT+TAB dialog, and the small icon in the window caption.
        /// </summary>
        public const int GETICON = 0x007F;
        /// <summary>
        /// An application sends the WM_SETICON message to associate a new large or small icon with a window. The system displays the large icon in the ALT+TAB dialog box, and the small icon in the window caption.
        /// </summary>
        public const int SETICON = 0x0080;
        /// <summary>
        /// The WM_NCCREATE message is sent prior to the WM_CREATE message when a window is first created.
        /// </summary>
        public const int NCCREATE = 0x0081;
        /// <summary>
        /// The WM_NCDESTROY message informs a window that its nonclient area is being destroyed. The DestroyWindow function sends the WM_NCDESTROY message to the window following the WM_DESTROY message. WM_DESTROY is used to free the allocated memory object associated with the window.
        /// The WM_NCDESTROY message is sent after the child windows have been destroyed. In contrast, WM_DESTROY is sent before the child windows are destroyed.
        /// </summary>
        public const int NCDESTROY = 0x0082;
        /// <summary>
        /// The WM_NCCALCSIZE message is sent when the size and position of a window's client area must be calculated. By processing this message, an application can control the content of the window's client area when the size or position of the window changes.
        /// </summary>
        public const int NCCALCSIZE = 0x0083;
        /// <summary>
        /// The WM_NCHITTEST message is sent to a window when the cursor moves, or when a mouse button is pressed or released. If the mouse is not captured, the message is sent to the window beneath the cursor. Otherwise, the message is sent to the window that has captured the mouse.
        /// </summary>
        public const int NCHITTEST = 0x0084;
        /// <summary>
        /// The WM_NCPAINT message is sent to a window when its frame must be painted.
        /// </summary>
        public const int NCPAINT = 0x0085;
        /// <summary>
        /// The WM_NCACTIVATE message is sent to a window when its nonclient area needs to be changed to indicate an active or inactive state.
        /// </summary>
        public const int NCACTIVATE = 0x0086;
        /// <summary>
        /// The WM_GETDLGCODE message is sent to the window procedure associated with a control. By default, the system handles all keyboard input to the control; the system interprets certain types of keyboard input as dialog box navigation keys. To override this default behavior, the control can respond to the WM_GETDLGCODE message to indicate the types of input it wants to process itself.
        /// </summary>
        public const int GETDLGCODE = 0x0087;
        /// <summary>
        /// The WM_SYNCPAINT message is used to synchronize painting while avoiding linking independent GUI threads.
        /// </summary>
        public const int SYNCPAINT = 0x0088;
        /// <summary>
        /// The WM_NCMOUSEMOVE message is posted to a window when the cursor is moved within the nonclient area of the window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        public const int NCMOUSEMOVE = 0x00A0;
        /// <summary>
        /// The WM_NCLBUTTONDOWN message is posted when the user presses the left mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        public const int NCLBUTTONDOWN = 0x00A1;
        /// <summary>
        /// The WM_NCLBUTTONUP message is posted when the user releases the left mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        public const int NCLBUTTONUP = 0x00A2;
        /// <summary>
        /// The WM_NCLBUTTONDBLCLK message is posted when the user double-clicks the left mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        public const int NCLBUTTONDBLCLK = 0x00A3;
        /// <summary>
        /// The WM_NCRBUTTONDOWN message is posted when the user presses the right mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        public const int NCRBUTTONDOWN = 0x00A4;
        /// <summary>
        /// The WM_NCRBUTTONUP message is posted when the user releases the right mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        public const int NCRBUTTONUP = 0x00A5;
        /// <summary>
        /// The WM_NCRBUTTONDBLCLK message is posted when the user double-clicks the right mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        public const int NCRBUTTONDBLCLK = 0x00A6;
        /// <summary>
        /// The WM_NCMBUTTONDOWN message is posted when the user presses the middle mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        public const int NCMBUTTONDOWN = 0x00A7;
        /// <summary>
        /// The WM_NCMBUTTONUP message is posted when the user releases the middle mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        public const int NCMBUTTONUP = 0x00A8;
        /// <summary>
        /// The WM_NCMBUTTONDBLCLK message is posted when the user double-clicks the middle mouse button while the cursor is within the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        public const int NCMBUTTONDBLCLK = 0x00A9;
        /// <summary>
        /// The WM_NCXBUTTONDOWN message is posted when the user presses the first or second X button while the cursor is in the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        public const int NCXBUTTONDOWN = 0x00AB;
        /// <summary>
        /// The WM_NCXBUTTONUP message is posted when the user releases the first or second X button while the cursor is in the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        public const int NCXBUTTONUP = 0x00AC;
        /// <summary>
        /// The WM_NCXBUTTONDBLCLK message is posted when the user double-clicks the first or second X button while the cursor is in the nonclient area of a window. This message is posted to the window that contains the cursor. If a window has captured the mouse, this message is not posted.
        /// </summary>
        public const int NCXBUTTONDBLCLK = 0x00AD;
        /// <summary>
        /// The WM_INPUT_DEVICE_CHANGE message is sent to the window that registered to receive raw input. A window receives this message through its WindowProc function.
        /// </summary>
        public const int INPUT_DEVICE_CHANGE = 0x00FE;
        /// <summary>
        /// The WM_INPUT message is sent to the window that is getting raw input.
        /// </summary>
        public const int INPUT = 0x00FF;
        /// <summary>
        /// This message filters for keyboard messages.
        /// </summary>
        public const int KEYFIRST = 0x0100;
        /// <summary>
        /// The WM_KEYDOWN message is posted to the window with the keyboard focus when a nonsystem key is pressed. A nonsystem key is a key that is pressed when the ALT key is not pressed.
        /// </summary>
        public const int KEYDOWN = 0x0100;
        /// <summary>
        /// The WM_KEYUP message is posted to the window with the keyboard focus when a nonsystem key is released. A nonsystem key is a key that is pressed when the ALT key is not pressed, or a keyboard key that is pressed when a window has the keyboard focus.
        /// </summary>
        public const int KEYUP = 0x0101;
        /// <summary>
        /// The WM_CHAR message is posted to the window with the keyboard focus when a WM_KEYDOWN message is translated by the TranslateMessage function. The WM_CHAR message contains the character code of the key that was pressed.
        /// </summary>
        public const int CHAR = 0x0102;
        /// <summary>
        /// The WM_DEADCHAR message is posted to the window with the keyboard focus when a WM_KEYUP message is translated by the TranslateMessage function. WM_DEADCHAR specifies a character code generated by a dead key. A dead key is a key that generates a character, such as the umlaut (double-dot), that is combined with another character to form a composite character. For example, the umlaut-O character (Ö) is generated by typing the dead key for the umlaut character, and then typing the O key.
        /// </summary>
        public const int DEADCHAR = 0x0103;
        /// <summary>
        /// The WM_SYSKEYDOWN message is posted to the window with the keyboard focus when the user presses the F10 key (which activates the menu bar) or holds down the ALT key and then presses another key. It also occurs when no window currently has the keyboard focus; in this case, the WM_SYSKEYDOWN message is sent to the active window. The window that receives the message can distinguish between these two contexts by checking the context code in the lParam parameter.
        /// </summary>
        public const int SYSKEYDOWN = 0x0104;
        /// <summary>
        /// The WM_SYSKEYUP message is posted to the window with the keyboard focus when the user releases a key that was pressed while the ALT key was held down. It also occurs when no window currently has the keyboard focus; in this case, the WM_SYSKEYUP message is sent to the active window. The window that receives the message can distinguish between these two contexts by checking the context code in the lParam parameter.
        /// </summary>
        public const int SYSKEYUP = 0x0105;
        /// <summary>
        /// The WM_SYSCHAR message is posted to the window with the keyboard focus when a WM_SYSKEYDOWN message is translated by the TranslateMessage function. It specifies the character code of a system character key — that is, a character key that is pressed while the ALT key is down.
        /// </summary>
        public const int SYSCHAR = 0x0106;
        /// <summary>
        /// The WM_SYSDEADCHAR message is sent to the window with the keyboard focus when a WM_SYSKEYDOWN message is translated by the TranslateMessage function. WM_SYSDEADCHAR specifies the character code of a system dead key — that is, a dead key that is pressed while holding down the ALT key.
        /// </summary>
        public const int SYSDEADCHAR = 0x0107;
        /// <summary>
        /// The WM_UNICHAR message is posted to the window with the keyboard focus when a WM_KEYDOWN message is translated by the TranslateMessage function. The WM_UNICHAR message contains the character code of the key that was pressed.
        /// The WM_UNICHAR message is equivalent to WM_CHAR, but it uses Unicode Transformation Format (UTF)-32, whereas WM_CHAR uses UTF-16. It is designed to send or post Unicode characters to ANSI windows and it can can handle Unicode Supplementary Plane characters.
        /// </summary>
        public const int UNICHAR = 0x0109;
        /// <summary>
        /// This message filters for keyboard messages.
        /// </summary>
        public const int KEYLAST = 0x0109;
        /// <summary>
        /// Sent immediately before the IME generates the composition string as a result of a keystroke. A window receives this message through its WindowProc function.
        /// </summary>
        public const int IME_STARTCOMPOSITION = 0x010D;
        /// <summary>
        /// Sent to an application when the IME ends composition. A window receives this message through its WindowProc function.
        /// </summary>
        public const int IME_ENDCOMPOSITION = 0x010E;
        /// <summary>
        /// Sent to an application when the IME changes composition status as a result of a keystroke. A window receives this message through its WindowProc function.
        /// </summary>
        public const int IME_COMPOSITION = 0x010F;
        public const int IME_KEYLAST = 0x010F;
        /// <summary>
        /// The WM_INITDIALOG message is sent to the dialog box procedure immediately before a dialog box is displayed. Dialog box procedures typically use this message to initialize controls and carry out any other initialization tasks that affect the appearance of the dialog box.
        /// </summary>
        public const int INITDIALOG = 0x0110;
        /// <summary>
        /// The WM_COMMAND message is sent when the user selects a command item from a menu, when a control sends a notification message to its parent window, or when an accelerator keystroke is translated.
        /// </summary>
        public const int COMMAND = 0x0111;
        /// <summary>
        /// A window receives this message when the user chooses a command from the Window menu, clicks the maximize button, minimize button, restore button, close button, or moves the form. You can stop the form from moving by filtering this out.
        /// </summary>
        public const int SYSCOMMAND = 0x0112;
        /// <summary>
        /// The WM_TIMER message is posted to the installing thread's message queue when a timer expires. The message is posted by the GetMessage or PeekMessage function.
        /// </summary>
        public const int TIMER = 0x0113;
        /// <summary>
        /// The WM_HSCROLL message is sent to a window when a scroll event occurs in the window's standard horizontal scroll bar. This message is also sent to the owner of a horizontal scroll bar control when a scroll event occurs in the control.
        /// </summary>
        public const int HSCROLL = 0x0114;
        /// <summary>
        /// The WM_VSCROLL message is sent to a window when a scroll event occurs in the window's standard vertical scroll bar. This message is also sent to the owner of a vertical scroll bar control when a scroll event occurs in the control.
        /// </summary>
        public const int VSCROLL = 0x0115;
        /// <summary>
        /// The WM_INITMENU message is sent when a menu is about to become active. It occurs when the user clicks an item on the menu bar or presses a menu key. This allows the application to modify the menu before it is displayed.
        /// </summary>
        public const int INITMENU = 0x0116;
        /// <summary>
        /// The WM_INITMENUPOPUP message is sent when a drop-down menu or submenu is about to become active. This allows an application to modify the menu before it is displayed, without changing the entire menu.
        /// </summary>
        public const int INITMENUPOPUP = 0x0117;
        /// <summary>
        /// The WM_MENUSELECT message is sent to a menu's owner window when the user selects a menu item.
        /// </summary>
        public const int MENUSELECT = 0x011F;
        /// <summary>
        /// The WM_MENUCHAR message is sent when a menu is active and the user presses a key that does not correspond to any mnemonic or accelerator key. This message is sent to the window that owns the menu.
        /// </summary>
        public const int MENUCHAR = 0x0120;
        /// <summary>
        /// The WM_ENTERIDLE message is sent to the owner window of a modal dialog box or menu that is entering an idle state. A modal dialog box or menu enters an idle state when no messages are waiting in its queue after it has processed one or more previous messages.
        /// </summary>
        public const int ENTERIDLE = 0x0121;
        /// <summary>
        /// The WM_MENURBUTTONUP message is sent when the user releases the right mouse button while the cursor is on a menu item.
        /// </summary>
        public const int MENURBUTTONUP = 0x0122;
        /// <summary>
        /// The WM_MENUDRAG message is sent to the owner of a drag-and-drop menu when the user drags a menu item.
        /// </summary>
        public const int MENUDRAG = 0x0123;
        /// <summary>
        /// The WM_MENUGETOBJECT message is sent to the owner of a drag-and-drop menu when the mouse cursor enters a menu item or moves from the center of the item to the top or bottom of the item.
        /// </summary>
        public const int MENUGETOBJECT = 0x0124;
        /// <summary>
        /// The WM_UNINITMENUPOPUP message is sent when a drop-down menu or submenu has been destroyed.
        /// </summary>
        public const int UNINITMENUPOPUP = 0x0125;
        /// <summary>
        /// The WM_MENUCOMMAND message is sent when the user makes a selection from a menu.
        /// </summary>
        public const int MENUCOMMAND = 0x0126;
        /// <summary>
        /// An application sends the WM_CHANGEUISTATE message to indicate that the user interface (UI) state should be changed.
        /// </summary>
        public const int CHANGEUISTATE = 0x0127;
        /// <summary>
        /// An application sends the WM_UPDATEUISTATE message to change the user interface (UI) state for the specified window and all its child windows.
        /// </summary>
        public const int UPDATEUISTATE = 0x0128;
        /// <summary>
        /// An application sends the WM_QUERYUISTATE message to retrieve the user interface (UI) state for a window.
        /// </summary>
        public const int QUERYUISTATE = 0x0129;
        /// <summary>
        /// The WM_CTLCOLORMSGBOX message is sent to the owner window of a message box before Windows draws the message box. By responding to this message, the owner window can set the text and background colors of the message box by using the given display device context handle.
        /// </summary>
        public const int CTLCOLORMSGBOX = 0x0132;
        /// <summary>
        /// An edit control that is not read-only or disabled sends the WM_CTLCOLOREDIT message to its parent window when the control is about to be drawn. By responding to this message, the parent window can use the specified device context handle to set the text and background colors of the edit control.
        /// </summary>
        public const int CTLCOLOREDIT = 0x0133;
        /// <summary>
        /// Sent to the parent window of a list box before the system draws the list box. By responding to this message, the parent window can set the text and background colors of the list box by using the specified display device context handle.
        /// </summary>
        public const int CTLCOLORLISTBOX = 0x0134;
        /// <summary>
        /// The WM_CTLCOLORBTN message is sent to the parent window of a button before drawing the button. The parent window can change the button's text and background colors. However, only owner-drawn buttons respond to the parent window processing this message.
        /// </summary>
        public const int CTLCOLORBTN = 0x0135;
        /// <summary>
        /// The WM_CTLCOLORDLG message is sent to a dialog box before the system draws the dialog box. By responding to this message, the dialog box can set its text and background colors using the specified display device context handle.
        /// </summary>
        public const int CTLCOLORDLG = 0x0136;
        /// <summary>
        /// The WM_CTLCOLORSCROLLBAR message is sent to the parent window of a scroll bar control when the control is about to be drawn. By responding to this message, the parent window can use the display context handle to set the background color of the scroll bar control.
        /// </summary>
        public const int CTLCOLORSCROLLBAR = 0x0137;
        /// <summary>
        /// A static control, or an edit control that is read-only or disabled, sends the WM_CTLCOLORSTATIC message to its parent window when the control is about to be drawn. By responding to this message, the parent window can use the specified device context handle to set the text and background colors of the static control.
        /// </summary>
        public const int CTLCOLORSTATIC = 0x0138;
        /// <summary>
        /// Use WM_MOUSEFIRST to specify the first mouse message. Use the PeekMessage() Function.
        /// </summary>
        public const int MOUSEFIRST = 0x0200;
        /// <summary>
        /// The WM_MOUSEMOVE message is posted to a window when the cursor moves. If the mouse is not captured, the message is posted to the window that contains the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        public const int MOUSEMOVE = 0x0200;
        /// <summary>
        /// The WM_LBUTTONDOWN message is posted when the user presses the left mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        public const int LBUTTONDOWN = 0x0201;
        /// <summary>
        /// The WM_LBUTTONUP message is posted when the user releases the left mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        public const int LBUTTONUP = 0x0202;
        /// <summary>
        /// The WM_LBUTTONDBLCLK message is posted when the user double-clicks the left mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        public const int LBUTTONDBLCLK = 0x0203;
        /// <summary>
        /// The WM_RBUTTONDOWN message is posted when the user presses the right mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        public const int RBUTTONDOWN = 0x0204;
        /// <summary>
        /// The WM_RBUTTONUP message is posted when the user releases the right mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        public const int RBUTTONUP = 0x0205;
        /// <summary>
        /// The WM_RBUTTONDBLCLK message is posted when the user double-clicks the right mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        public const int RBUTTONDBLCLK = 0x0206;
        /// <summary>
        /// The WM_MBUTTONDOWN message is posted when the user presses the middle mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        public const int MBUTTONDOWN = 0x0207;
        /// <summary>
        /// The WM_MBUTTONUP message is posted when the user releases the middle mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        public const int MBUTTONUP = 0x0208;
        /// <summary>
        /// The WM_MBUTTONDBLCLK message is posted when the user double-clicks the middle mouse button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        public const int MBUTTONDBLCLK = 0x0209;
        /// <summary>
        /// The WM_MOUSEWHEEL message is sent to the focus window when the mouse wheel is rotated. The DefWindowProc function propagates the message to the window's parent. There should be no internal forwarding of the message, since DefWindowProc propagates it up the parent chain until it finds a window that processes it.
        /// </summary>
        public const int MOUSEWHEEL = 0x020A;
        /// <summary>
        /// The WM_XBUTTONDOWN message is posted when the user presses the first or second X button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        public const int XBUTTONDOWN = 0x020B;
        /// <summary>
        /// The WM_XBUTTONUP message is posted when the user releases the first or second X button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        public const int XBUTTONUP = 0x020C;
        /// <summary>
        /// The WM_XBUTTONDBLCLK message is posted when the user double-clicks the first or second X button while the cursor is in the client area of a window. If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the message is posted to the window that has captured the mouse.
        /// </summary>
        public const int XBUTTONDBLCLK = 0x020D;
        /// <summary>
        /// The WM_MOUSEHWHEEL message is sent to the focus window when the mouse's horizontal scroll wheel is tilted or rotated. The DefWindowProc function propagates the message to the window's parent. There should be no internal forwarding of the message, since DefWindowProc propagates it up the parent chain until it finds a window that processes it.
        /// </summary>
        public const int MOUSEHWHEEL = 0x020E;
        /// <summary>
        /// Use WM_MOUSELAST to specify the last mouse message. Used with PeekMessage() Function.
        /// </summary>
        public const int MOUSELAST = 0x020E;
        /// <summary>
        /// The WM_PARENTNOTIFY message is sent to the parent of a child window when the child window is created or destroyed, or when the user clicks a mouse button while the cursor is over the child window. When the child window is being created, the system sends WM_PARENTNOTIFY just before the CreateWindow or CreateWindowEx function that creates the window returns. When the child window is being destroyed, the system sends the message before any processing to destroy the window takes place.
        /// </summary>
        public const int PARENTNOTIFY = 0x0210;
        /// <summary>
        /// The WM_ENTERMENULOOP message informs an application's main window procedure that a menu modal loop has been entered.
        /// </summary>
        public const int ENTERMENULOOP = 0x0211;
        /// <summary>
        /// The WM_EXITMENULOOP message informs an application's main window procedure that a menu modal loop has been exited.
        /// </summary>
        public const int EXITMENULOOP = 0x0212;
        /// <summary>
        /// The WM_NEXTMENU message is sent to an application when the right or left arrow key is used to switch between the menu bar and the system menu.
        /// </summary>
        public const int NEXTMENU = 0x0213;
        /// <summary>
        /// The WM_SIZING message is sent to a window that the user is resizing. By processing this message, an application can monitor the size and position of the drag rectangle and, if needed, change its size or position.
        /// </summary>
        public const int SIZING = 0x0214;
        /// <summary>
        /// The WM_CAPTURECHANGED message is sent to the window that is losing the mouse capture.
        /// </summary>
        public const int CAPTURECHANGED = 0x0215;
        /// <summary>
        /// The WM_MOVING message is sent to a window that the user is moving. By processing this message, an application can monitor the position of the drag rectangle and, if needed, change its position.
        /// </summary>
        public const int MOVING = 0x0216;
        /// <summary>
        /// Notifies applications that a power-management event has occurred.
        /// </summary>
        public const int POWERBROADCAST = 0x0218;
        /// <summary>
        /// Notifies an application of a change to the hardware configuration of a device or the computer.
        /// </summary>
        public const int DEVICECHANGE = 0x0219;
        /// <summary>
        /// An application sends the WM_MDICREATE message to a multiple-document interface (MDI) client window to create an MDI child window.
        /// </summary>
        public const int MDICREATE = 0x0220;
        /// <summary>
        /// An application sends the WM_MDIDESTROY message to a multiple-document interface (MDI) client window to close an MDI child window.
        /// </summary>
        public const int MDIDESTROY = 0x0221;
        /// <summary>
        /// An application sends the WM_MDIACTIVATE message to a multiple-document interface (MDI) client window to instruct the client window to activate a different MDI child window.
        /// </summary>
        public const int MDIACTIVATE = 0x0222;
        /// <summary>
        /// An application sends the WM_MDIRESTORE message to a multiple-document interface (MDI) client window to restore an MDI child window from maximized or minimized size.
        /// </summary>
        public const int MDIRESTORE = 0x0223;
        /// <summary>
        /// An application sends the WM_MDINEXT message to a multiple-document interface (MDI) client window to activate the next or previous child window.
        /// </summary>
        public const int MDINEXT = 0x0224;
        /// <summary>
        /// An application sends the WM_MDIMAXIMIZE message to a multiple-document interface (MDI) client window to maximize an MDI child window. The system resizes the child window to make its client area fill the client window. The system places the child window's window menu icon in the rightmost position of the frame window's menu bar, and places the child window's restore icon in the leftmost position. The system also appends the title bar text of the child window to that of the frame window.
        /// </summary>
        public const int MDIMAXIMIZE = 0x0225;
        /// <summary>
        /// An application sends the WM_MDITILE message to a multiple-document interface (MDI) client window to arrange all of its MDI child windows in a tile format.
        /// </summary>
        public const int MDITILE = 0x0226;
        /// <summary>
        /// An application sends the WM_MDICASCADE message to a multiple-document interface (MDI) client window to arrange all its child windows in a cascade format.
        /// </summary>
        public const int MDICASCADE = 0x0227;
        /// <summary>
        /// An application sends the WM_MDIICONARRANGE message to a multiple-document interface (MDI) client window to arrange all minimized MDI child windows. It does not affect child windows that are not minimized.
        /// </summary>
        public const int MDIICONARRANGE = 0x0228;
        /// <summary>
        /// An application sends the WM_MDIGETACTIVE message to a multiple-document interface (MDI) client window to retrieve the handle to the active MDI child window.
        /// </summary>
        public const int MDIGETACTIVE = 0x0229;
        /// <summary>
        /// An application sends the WM_MDISETMENU message to a multiple-document interface (MDI) client window to replace the entire menu of an MDI frame window, to replace the window menu of the frame window, or both.
        /// </summary>
        public const int MDISETMENU = 0x0230;
        /// <summary>
        /// The WM_ENTERSIZEMOVE message is sent one time to a window after it enters the moving or sizing modal loop. The window enters the moving or sizing modal loop when the user clicks the window's title bar or sizing border, or when the window passes the WM_SYSCOMMAND message to the DefWindowProc function and the wParam parameter of the message specifies the SC_MOVE or SC_SIZE value. The operation is complete when DefWindowProc returns.
        /// The system sends the WM_ENTERSIZEMOVE message regardless of whether the dragging of full windows is enabled.
        /// </summary>
        public const int ENTERSIZEMOVE = 0x0231;
        /// <summary>
        /// The WM_EXITSIZEMOVE message is sent one time to a window, after it has exited the moving or sizing modal loop. The window enters the moving or sizing modal loop when the user clicks the window's title bar or sizing border, or when the window passes the WM_SYSCOMMAND message to the DefWindowProc function and the wParam parameter of the message specifies the SC_MOVE or SC_SIZE value. The operation is complete when DefWindowProc returns.
        /// </summary>
        public const int EXITSIZEMOVE = 0x0232;
        /// <summary>
        /// Sent when the user drops a file on the window of an application that has registered itself as a recipient of dropped files.
        /// </summary>
        public const int DROPFILES = 0x0233;
        /// <summary>
        /// An application sends the WM_MDIREFRESHMENU message to a multiple-document interface (MDI) client window to refresh the window menu of the MDI frame window.
        /// </summary>
        public const int MDIREFRESHMENU = 0x0234;
        /// <summary>
        /// Sent to an application when a window is activated. A window receives this message through its WindowProc function.
        /// </summary>
        public const int IME_SETCONTEXT = 0x0281;
        /// <summary>
        /// Sent to an application to notify it of changes to the IME window. A window receives this message through its WindowProc function.
        /// </summary>
        public const int IME_NOTIFY = 0x0282;
        /// <summary>
        /// Sent by an application to direct the IME window to carry out the requested command. The application uses this message to control the IME window that it has created. To send this message, the application calls the SendMessage function with the following parameters.
        /// </summary>
        public const int IME_CONTROL = 0x0283;
        /// <summary>
        /// Sent to an application when the IME window finds no space to extend the area for the composition window. A window receives this message through its WindowProc function.
        /// </summary>
        public const int IME_COMPOSITIONFULL = 0x0284;
        /// <summary>
        /// Sent to an application when the operating system is about to change the current IME. A window receives this message through its WindowProc function.
        /// </summary>
        public const int IME_SELECT = 0x0285;
        /// <summary>
        /// Sent to an application when the IME gets a character of the conversion result. A window receives this message through its WindowProc function.
        /// </summary>
        public const int IME_CHAR = 0x0286;
        /// <summary>
        /// Sent to an application to provide commands and request information. A window receives this message through its WindowProc function.
        /// </summary>
        public const int IME_REQUEST = 0x0288;
        /// <summary>
        /// Sent to an application by the IME to notify the application of a key press and to keep message order. A window receives this message through its WindowProc function.
        /// </summary>
        public const int IME_KEYDOWN = 0x0290;
        /// <summary>
        /// Sent to an application by the IME to notify the application of a key release and to keep message order. A window receives this message through its WindowProc function.
        /// </summary>
        public const int IME_KEYUP = 0x0291;
        /// <summary>
        /// The WM_MOUSEHOVER message is posted to a window when the cursor hovers over the client area of the window for the period of time specified in a prior call to TrackMouseEvent.
        /// </summary>
        public const int MOUSEHOVER = 0x02A1;
        /// <summary>
        /// The WM_MOUSELEAVE message is posted to a window when the cursor leaves the client area of the window specified in a prior call to TrackMouseEvent.
        /// </summary>
        public const int MOUSELEAVE = 0x02A3;
        /// <summary>
        /// The WM_NCMOUSEHOVER message is posted to a window when the cursor hovers over the nonclient area of the window for the period of time specified in a prior call to TrackMouseEvent.
        /// </summary>
        public const int NCMOUSEHOVER = 0x02A0;
        /// <summary>
        /// The WM_NCMOUSELEAVE message is posted to a window when the cursor leaves the nonclient area of the window specified in a prior call to TrackMouseEvent.
        /// </summary>
        public const int NCMOUSELEAVE = 0x02A2;
        /// <summary>
        /// The WM_WTSSESSION_CHANGE message notifies applications of changes in session state.
        /// </summary>
        public const int WTSSESSION_CHANGE = 0x02B1;
        public const int TABLET_FIRST = 0x02c0;
        public const int TABLET_LAST = 0x02df;
        /// <summary>
        /// An application sends a WM_CUT message to an edit control or combo box to delete (cut) the current selection, if any, in the edit control and copy the deleted text to the clipboard in CF_TEXT format.
        /// </summary>
        public const int CUT = 0x0300;
        /// <summary>
        /// An application sends the WM_COPY message to an edit control or combo box to copy the current selection to the clipboard in CF_TEXT format.
        /// </summary>
        public const int COPY = 0x0301;
        /// <summary>
        /// An application sends a WM_PASTE message to an edit control or combo box to copy the current content of the clipboard to the edit control at the current caret position. Data is inserted only if the clipboard contains data in CF_TEXT format.
        /// </summary>
        public const int PASTE = 0x0302;
        /// <summary>
        /// An application sends a WM_CLEAR message to an edit control or combo box to delete (clear) the current selection, if any, from the edit control.
        /// </summary>
        public const int CLEAR = 0x0303;
        /// <summary>
        /// An application sends a WM_UNDO message to an edit control to undo the last operation. When this message is sent to an edit control, the previously deleted text is restored or the previously added text is deleted.
        /// </summary>
        public const int UNDO = 0x0304;
        /// <summary>
        /// The WM_RENDERFORMAT message is sent to the clipboard owner if it has delayed rendering a specific clipboard format and if an application has requested data in that format. The clipboard owner must render data in the specified format and place it on the clipboard by calling the SetClipboardData function.
        /// </summary>
        public const int RENDERFORMAT = 0x0305;
        /// <summary>
        /// The WM_RENDERALLFORMATS message is sent to the clipboard owner before it is destroyed, if the clipboard owner has delayed rendering one or more clipboard formats. For the content of the clipboard to remain available to other applications, the clipboard owner must render data in all the formats it is capable of generating, and place the data on the clipboard by calling the SetClipboardData function.
        /// </summary>
        public const int RENDERALLFORMATS = 0x0306;
        /// <summary>
        /// The WM_DESTROYCLIPBOARD message is sent to the clipboard owner when a call to the EmptyClipboard function empties the clipboard.
        /// </summary>
        public const int DESTROYCLIPBOARD = 0x0307;
        /// <summary>
        /// The WM_DRAWCLIPBOARD message is sent to the first window in the clipboard viewer chain when the content of the clipboard changes. This enables a clipboard viewer window to display the new content of the clipboard.
        /// </summary>
        public const int DRAWCLIPBOARD = 0x0308;
        /// <summary>
        /// The WM_PAINTCLIPBOARD message is sent to the clipboard owner by a clipboard viewer window when the clipboard contains data in the CF_OWNERDISPLAY format and the clipboard viewer's client area needs repainting.
        /// </summary>
        public const int PAINTCLIPBOARD = 0x0309;
        /// <summary>
        /// The WM_VSCROLLCLIPBOARD message is sent to the clipboard owner by a clipboard viewer window when the clipboard contains data in the CF_OWNERDISPLAY format and an event occurs in the clipboard viewer's vertical scroll bar. The owner should scroll the clipboard image and update the scroll bar values.
        /// </summary>
        public const int VSCROLLCLIPBOARD = 0x030A;
        /// <summary>
        /// The WM_SIZECLIPBOARD message is sent to the clipboard owner by a clipboard viewer window when the clipboard contains data in the CF_OWNERDISPLAY format and the clipboard viewer's client area has changed size.
        /// </summary>
        public const int SIZECLIPBOARD = 0x030B;
        /// <summary>
        /// The WM_ASKCBFORMATNAME message is sent to the clipboard owner by a clipboard viewer window to request the name of a CF_OWNERDISPLAY clipboard format.
        /// </summary>
        public const int ASKCBFORMATNAME = 0x030C;
        /// <summary>
        /// The WM_CHANGECBCHAIN message is sent to the first window in the clipboard viewer chain when a window is being removed from the chain.
        /// </summary>
        public const int CHANGECBCHAIN = 0x030D;
        /// <summary>
        /// The WM_HSCROLLCLIPBOARD message is sent to the clipboard owner by a clipboard viewer window. This occurs when the clipboard contains data in the CF_OWNERDISPLAY format and an event occurs in the clipboard viewer's horizontal scroll bar. The owner should scroll the clipboard image and update the scroll bar values.
        /// </summary>
        public const int HSCROLLCLIPBOARD = 0x030E;
        /// <summary>
        /// This message informs a window that it is about to receive the keyboard focus, giving the window the opportunity to realize its logical palette when it receives the focus.
        /// </summary>
        public const int QUERYNEWPALETTE = 0x030F;
        /// <summary>
        /// The WM_PALETTEISCHANGING message informs applications that an application is going to realize its logical palette.
        /// </summary>
        public const int PALETTEISCHANGING = 0x0310;
        /// <summary>
        /// This message is sent by the OS to all top-level and overlapped windows after the window with the keyboard focus realizes its logical palette.
        /// This message enables windows that do not have the keyboard focus to realize their logical palettes and update their client areas.
        /// </summary>
        public const int PALETTECHANGED = 0x0311;
        /// <summary>
        /// The WM_HOTKEY message is posted when the user presses a hot key registered by the RegisterHotKey function. The message is placed at the top of the message queue associated with the thread that registered the hot key.
        /// </summary>
        public const int HOTKEY = 0x0312;
        /// <summary>
        /// The WM_PRINT message is sent to a window to request that it draw itself in the specified device context, most commonly in a printer device context.
        /// </summary>
        public const int PRINT = 0x0317;
        /// <summary>
        /// The WM_PRINTCLIENT message is sent to a window to request that it draw its client area in the specified device context, most commonly in a printer device context.
        /// </summary>
        public const int PRINTCLIENT = 0x0318;
        /// <summary>
        /// The WM_APPCOMMAND message notifies a window that the user generated an application command event, for example, by clicking an application command button using the mouse or typing an application command key on the keyboard.
        /// </summary>
        public const int APPCOMMAND = 0x0319;
        /// <summary>
        /// The WM_THEMECHANGED message is broadcast to every window following a theme change event. Examples of theme change events are the activation of a theme, the deactivation of a theme, or a transition from one theme to another.
        /// </summary>
        public const int THEMECHANGED = 0x031A;
        /// <summary>
        /// Sent when the contents of the clipboard have changed.
        /// </summary>
        public const int CLIPBOARDUPDATE = 0x031D;
        /// <summary>
        /// The system will send a window the WM_DWMCOMPOSITIONCHANGED message to indicate that the availability of desktop composition has changed.
        /// </summary>
        public const int DWMCOMPOSITIONCHANGED = 0x031E;
        /// <summary>
        /// WM_DWMNCRENDERINGCHANGED is called when the non-client area rendering status of a window has changed. Only windows that have set the flag DWM_BLURBEHIND.fTransitionOnMaximized to true will get this message.
        /// </summary>
        public const int DWMNCRENDERINGCHANGED = 0x031F;
        /// <summary>
        /// Sent to all top-level windows when the colorization color has changed.
        /// </summary>
        public const int DWMCOLORIZATIONCOLORCHANGED = 0x0320;
        /// <summary>
        /// WM_DWMWINDOWMAXIMIZEDCHANGE will let you know when a DWM composed window is maximized. You also have to register for this message as well. You'd have other windowd go opaque when this message is sent.
        /// </summary>
        public const int DWMWINDOWMAXIMIZEDCHANGE = 0x0321;
        /// <summary>
        /// Sent to request extended title bar information. A window receives this message through its WindowProc function.
        /// </summary>
        public const int GETTITLEBARINFOEX = 0x033F;
        public const int HANDHELDFIRST = 0x0358;
        public const int HANDHELDLAST = 0x035F;
        public const int AFXFIRST = 0x0360;
        public const int AFXLAST = 0x037F;
        public const int PENWINFIRST = 0x0380;
        public const int PENWINLAST = 0x038F;
        /// <summary>
        /// The WM_APP constant is used by applications to help define private messages, usually of the form WM_APP+X, where X is an integer value.
        /// </summary>
        public const int APP = 0x8000;
        /// <summary>
        /// The WM_USER constant is used by applications to help define private messages for use by private window classes, usually of the form WM_USER+X, where X is an integer value.
        /// </summary>
        public const int USER = 0x0400;

        /// <summary>
        /// An application sends the WM_CPL_LAUNCH message to Windows Control Panel to request that a Control Panel application be started.
        /// </summary>
        public const int CPL_LAUNCH = USER + 0x1000;
        /// <summary>
        /// The WM_CPL_LAUNCHED message is sent when a Control Panel application, started by the WM_CPL_LAUNCH message, has closed. The WM_CPL_LAUNCHED message is sent to the window identified by the wParam parameter of the WM_CPL_LAUNCH message that started the application.
        /// </summary>
        public const int CPL_LAUNCHED = USER + 0x1001;
        /// <summary>
        /// WM_SYSTIMER is a well-known yet still undocumented message. Windows uses WM_SYSTIMER for internal actions like scrolling.
        /// </summary>
        public const int SYSTIMER = 0x118;

        /// <summary>
        /// The accessibility state has changed.
        /// </summary>
        public const int HSHELL_ACCESSIBILITYSTATE = 11;
        /// <summary>
        /// The shell should activate its main window.
        /// </summary>
        public const int HSHELL_ACTIVATESHELLWINDOW = 3;
        /// <summary>
        /// The user completed an input event (for example, pressed an application command button on the mouse or an application command key on the keyboard), and the application did not handle the WM_APPCOMMAND message generated by that input.
        /// If the Shell procedure handles the WM_COMMAND message, it should not call CallNextHookEx. See the Return Value section for more information.
        /// </summary>
        public const int HSHELL_APPCOMMAND = 12;
        /// <summary>
        /// A window is being minimized or maximized. The system needs the coordinates of the minimized rectangle for the window.
        /// </summary>
        public const int HSHELL_GETMINRECT = 5;
        /// <summary>
        /// Keyboard language was changed or a new keyboard layout was loaded.
        /// </summary>
        public const int HSHELL_LANGUAGE = 8;
        /// <summary>
        /// The title of a window in the task bar has been redrawn.
        /// </summary>
        public const int HSHELL_REDRAW = 6;
        /// <summary>
        /// The user has selected the task list. A shell application that provides a task list should return TRUE to prevent Windows from starting its task list.
        /// </summary>
        public const int HSHELL_TASKMAN = 7;
        /// <summary>
        /// A top-level, unowned window has been created. The window exists when the system calls this hook.
        /// </summary>
        public const int HSHELL_WINDOWCREATED = 1;
        /// <summary>
        /// A top-level, unowned window is about to be destroyed. The window still exists when the system calls this hook.
        /// </summary>
        public const int HSHELL_WINDOWDESTROYED = 2;
        /// <summary>
        /// The activation has changed to a different top-level, unowned window.
        /// </summary>
        public const int HSHELL_WINDOWACTIVATED = 4;
        /// <summary>
        /// A top-level window is being replaced. The window exists when the system calls this hook.
        /// </summary>
        public const int HSHELL_WINDOWREPLACED = 13;
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
    /// v procesu <see cref="Globals.DisposeControls(Control, bool, Control, ContainerControl, bool)"/>
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

    /// <summary>
    /// Simulace Green
    /// </summary>
    public class ComponentConnector
    {
        /// <summary>
        /// Simulace Green
        /// </summary>
        public static GraphicsCache GraphicsCache { get { if (_GraphicsCache is null) _GraphicsCache = new GraphicsCache(); return _GraphicsCache; } }
        private static GraphicsCache _GraphicsCache;
        /// <summary>
        /// Zobrazí varování
        /// </summary>
        /// <param name="message"></param>
        public static void ShowWarningToDeveloper(string message)
        {
            if (!System.Diagnostics.Debugger.IsAttached) return;
            MessageBox.Show(Host.Owner, message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
        }
        /// <summary>
        /// Simulace Green
        /// </summary>
        public static AppHost Host { get { if (_Host == null) _Host = new AppHost(); return _Host; } }
        private static AppHost _Host;
    }
    public class AppHost
    {
        public bool InvokeRequired { get { return (AnyControl?.InvokeRequired ?? false); } }
        public void Invoke(Delegate method)
        {
            var control = AnyControl;
            if (control != null)
            {
                if (control.InvokeRequired)
                    control.Invoke(method);
                else
                    method.DynamicInvoke();
            }
        }
        public void Invoke(Delegate method, params object[] args)
        {
            var control = AnyControl;
            if (control != null)
            {
                if (control.InvokeRequired)
                    control.Invoke(method);
                else
                    method.DynamicInvoke(args);
            }
        }
        public Control Owner { get { return AnyControl; } }
        protected Control AnyControl { get { return Form.ActiveForm; } }
        public event EventHandler InteractiveZoomChanged;
    }
    /// <summary>
    /// Simulace Green
    /// </summary>
    public class GraphicsCache
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GraphicsCache()
        {
            _ImageList = new ImageList();
        }
        /// <summary>
        /// Zoom
        /// </summary>
        public decimal CurrentZoom { get { return 1m; } }
        /// <summary>
        /// Simulace Green
        /// </summary>
        /// <param name="sizeOrDefault"></param>
        /// <returns></returns>
        public ImageList GetImageList(UserGraphicsSize? sizeOrDefault = null)
        {
            return _ImageList;
        }
        private ImageList _ImageList;
        /// <summary>
        /// Simulace Green
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="size"></param>
        /// <param name="caption"></param>
        public Image GetResourceContent(string imageName, UserGraphicsSize size, string caption = null)
        {
            if (String.IsNullOrEmpty(imageName)) return null;
            if (!_ImageList.Images.ContainsKey(imageName))
            {
                var image = TestDevExpress.Properties.Resources.ResourceManager.GetObject(imageName) as Image;
                if (!(image is null))
                    _ImageList.Images.Add(imageName, image);
            }
            if (!_ImageList.Images.ContainsKey(imageName)) return null;
            return _ImageList.Images[imageName];
        }
        /// <summary>
        /// Simulace Green
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="size"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public int GetResourceIndex(string imageName, UserGraphicsSize size, string caption = null)
        {
            Image image = GetResourceContent(imageName, size, caption);
            if (image is null) return -1;
            return _ImageList.Images.IndexOfKey(imageName);
        }
    }
    /// <summary>
    /// Simulace Green
    /// </summary>
    public class UiSynchronizationHelper
    {
        /// <summary> Executes the event on a different thread (UI marshaling), and waits for the result. </summary>
        /// <exception cref="Exception"> Thrown when an exception error condition occurs. </exception>
        /// <typeparam name="TEventArgs"> Type of the event arguments. </typeparam>
        /// <param name="sender">              Source of the event. </param>
        /// <param name="eventArgs">           T event information. </param>
        /// <param name="invokeHandler">       The invoke handler. </param>
        /// <param name="beforeInvokeHandler"> (Optional) the before invoke handler. </param>
        public static void InvokeEvent<TEventArgs>(object sender, TEventArgs eventArgs, EventHandler<TEventArgs> invokeHandler, Action beforeInvokeHandler = null)
            where TEventArgs : EventArgs
        {
            if (ComponentConnector.Host.InvokeRequired)
            {// marshaling invoke
                if (beforeInvokeHandler != null) beforeInvokeHandler();
                ComponentConnector.Host.Invoke(new Action<object, TEventArgs>((s, e) =>
                {
                    try
                    {
                        invokeHandler(s, e); //Invoke handler method
                    }
                    catch (Exception ex)
                    {// store inner call stack to exception Data for future processing
                        _propagateExeption(ex);
                        throw;
                    }
                }), new[] { sender, eventArgs });
            }
            else
            {// direct call
                invokeHandler(sender, eventArgs); // invoke handler method
            }
        }
        /// <summary>
        /// Invoke
        /// </summary>
        /// <typeparam name="TSender"></typeparam>
        /// <typeparam name="TArgument"></typeparam>
        /// <param name="sender"></param>
        /// <param name="argument"></param>
        /// <param name="invokeHandler"></param>
        /// <param name="beforeInvokeHandler"></param>
        public static void Invoke<TSender, TArgument>(TSender sender, TArgument argument, Action<TSender, TArgument> invokeHandler, Action beforeInvokeHandler = null)
        {
            if (ComponentConnector.Host.InvokeRequired)
            {// marshaling invoke
                if (beforeInvokeHandler != null) beforeInvokeHandler();
                ComponentConnector.Host.Invoke(new Action<TSender, TArgument>((s, a) =>
                {
                    try
                    {
                        invokeHandler(s, a); //Invoke handler method
                    }
                    catch (Exception ex)
                    {// store inner call stack to exception Data for future processing
                        _propagateExeption(ex);
                        throw;
                    }
                }), new object[] { sender, argument });
            }
            else
            {// direct call
                invokeHandler(sender, argument); // invoke handler method
            }
        }
        private static void _propagateExeption(Exception ex)
        {
            if (ex.Data.Contains(InvokeExceptionDataKey) == false)
            {
                ex.Data.Add(InvokeExceptionDataKey, ex.StackTrace);
            }
            else
            {
                ex.Data[InvokeExceptionDataKey] = ex.Data[InvokeExceptionDataKey] + ex.StackTrace;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public const string InvokeExceptionDataKey = "InnerInvokeStack";
    }
}
namespace WinFormServices
{
    /// <summary>
    /// Simulace Green
    /// </summary>
    public class KeyboardHelper
    {
        /// <summary>
        /// Simulace Green
        /// </summary>
        /// <param name="hotKey"></param>
        /// <returns></returns>
        public static DevExpress.XtraBars.BarShortcut GetShortcutFromServerHotKey(string hotKey) { return new DevExpress.XtraBars.BarShortcut(Keys.None); }

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
