using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using XmlSerializer = Noris.WS.Parser.XmlSerializer;

namespace Noris.UI.Desktop.MultiPage
{
    /// <summary>
    /// Layout okna s možností postupně jej dělit na podprostory
    /// </summary>
    public class WindowLayout
    {
        /// <summary>
        /// Konsturktor pro layout
        /// </summary>
        public WindowLayout()
        {
            __RootArea = new WindowArea(this);
        }
        /// <summary>
        /// Základní prostor layoutu. Může i nemusí být rozdělen.
        /// </summary>
        public WindowArea RootArea { get { return __RootArea; } } private WindowArea __RootArea;
        /// <summary>
        /// XML popisující layout.
        /// </summary>
        public string LayoutXml { get { return _CreateLayoutXml(); } }
        /// <summary>
        /// Vrátí XML layout pro aktuální objekt
        /// </summary>
        /// <returns></returns>
        private string _CreateLayoutXml()
        {
            var wsLayout = new Noris.WS.DataContracts.Desktop.Forms.FormLayout();
            fillWsParams(this, wsLayout);
            wsLayout.RootArea = __RootArea.CreateWsArea();
            return Noris.WS.Parser.XmlSerializer.Persist.Serialize(wsLayout, Noris.WS.Parser.XmlSerializer.PersistArgs.Default);

            // Konverze jednotlivých dat - vyjma rekurze:
            void fillWsParams(WindowLayout source, Noris.WS.DataContracts.Desktop.Forms.FormLayout target)
            {
                target.FormNormalBounds = null;
                // atd
            }
        }
    }
    /// <summary>
    /// Popis jedné části layoutu
    /// </summary>
    public class WindowArea
    {
        #region Konstruktory a privátní fieldy
        /// <summary>
        /// Konstruktor pro samostatný Root prostor
        /// </summary>
        public WindowArea()
        {
            __RootLayout = null;
            __ContentId = 0;
            __ContentType = WindowAreaContentType.UserControl;
        }
        /// <summary>
        /// Konstruktor pro Root prostor v layoutu
        /// </summary>
        /// <param name="rootLayout"></param>
        public WindowArea(WindowLayout rootLayout)
        {
            __RootLayout = rootLayout;
            __ContentId = 0;
            __ContentType = WindowAreaContentType.UserControl;
        }
        /// <summary>
        /// Konstruktor pro Child prostor
        /// </summary>
        /// <param name="parentArea"></param>
        /// <param name="contentId"></param>
        public WindowArea(WindowArea parentArea, int contentId)
        {
            __ParentArea = parentArea;
            __ContentId = contentId;
            __ContentType = WindowAreaContentType.UserControl;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = $"{_FullAreaId} : {__ContentType}";
            return text;
        }
        /// <summary>
        /// Parent layout. Nemusí být naplněno nikdy.
        /// </summary>
        private WindowLayout __RootLayout;
        /// <summary>
        /// Parent area. Pokud není, pak this je Root area.
        /// </summary>
        private WindowArea __ParentArea;
        /// <summary>
        /// Druh obsahu v tomto prostoru
        /// </summary>
        private WindowAreaContentType __ContentType;
        /// <summary>
        /// ID contentu: 0=Root, 1=<see cref="Content1"/> (vlevo nebo nahoře), 2=<see cref="Content2"/> (vpravo nebo dole)
        /// </summary>
        private int __ContentId;
        /// <summary>
        /// Dělený pod-prostor 1 (vlevo / nahoře)
        /// </summary>
        private WindowArea __Content1;
        /// <summary>
        /// Dělený pod-prostor 2 (vpravo / dole)
        /// </summary>
        private WindowArea __Content2;
        /// <summary>
        /// Pozice splitteru.
        /// </summary>
        private int? __SplitterPosition;
        /// <summary>
        /// Rozsah pohybu splitteru (šířka nebo výška prostoru).
        /// </summary>
        private int? __SplitterRange;
        /// <summary>
        /// Je splitter fixovaný = uživatel s ním nemůže pohybovat?
        /// </summary>
        private bool? __IsSplitterPositionFixed;
        /// <summary>
        /// Který Content je Fixed?
        /// </summary>
        private WindowAreaFixedContent? __FixedContent;
        /// <summary>
        /// Minimální šířka, platí i pro typ Container
        /// </summary>
        private int? __MinWidth;
        /// <summary>
        /// Minimální výška, platí i pro typ Container
        /// </summary>
        private int? __MinHeight;
        /// <summary>
        /// Vrátí validní Content pro aktuální stav <see cref="HasSplitter"/>
        /// </summary>
        /// <param name="content"></param>
        /// <param name="contentId"></param>
        /// <returns></returns>
        private WindowArea _GetContent(ref WindowArea content, int contentId)
        {
            if (HasSplitter)
            {
                if (content is null)
                    content = new WindowArea(this, contentId);
                return content;
            }
            return null;
        }
        #endregion
        #region AreaId
        /// <summary>
        /// Do daného pole vloží AreaId koncových prostorů pro UserControly. Nevkládá prostory typu Container.
        /// </summary>
        /// <param name="allAreaIds"></param>
        private void _AddAllUserAreaIdsTo(List<string> allAreaIds)
        {
            if (!HasSplitter)
            {   // Já nejsem Splitter = já jsem UserArea:
                allAreaIds.Add(_FullAreaId);
            }
            else
            {   // Já mám Splitter = mám dva Contenty:
                this.Content1._AddAllUserAreaIdsTo(allAreaIds);
                this.Content2._AddAllUserAreaIdsTo(allAreaIds);
            }
        }
        /// <summary>
        /// Plné AreaId = identifikace od parenta až ke mě, i kdybych já byl Container
        /// </summary>
        private string _FullAreaId 
        { 
            get 
            {
                var parentAreaId = __ParentArea?._FullAreaId ?? "";                                          // Parent: "C/P1"
                parentAreaId = (String.IsNullOrEmpty(parentAreaId)) ? "" : parentAreaId + AreaIdDelimiter;   // Cesta : "C/P1/"
                var currentAreaId = _CurrentAreaId;                                                          // Já:     "P2";
                return parentAreaId + currentAreaId;                                                         // Celek:  "C/P1/P2";
            }
        }
        /// <summary>
        /// Lokální AreaId = identifikace jen této úrovně: "C" = Container / "P1" nebo "P2" = panel 1 nebo 2
        /// </summary>
        private string _CurrentAreaId { get { return (__ContentId == 1 ? AreaIdContent1 : (__ContentId == 2 ? AreaIdContent2 : AreaIdContainer)); } }
        private const string AreaIdContainer = "C";
        private const string AreaIdContent1 = "P1";
        private const string AreaIdContent2 = "P2";
        private const string AreaIdDelimiter = "/";
        #endregion
        #region Public property
        /// <summary>
        /// Obsahuje true u Root prvku, tento prvek reprezentuje celé okno = Window.
        /// </summary>
        public bool IsRoot { get { return (__ParentArea is null); } }
        /// <summary>
        /// Druh obsahu v tomto prostoru: finální UserControl (typicky DynamicPage)? Nebo je tento prostor rozdělený na dva menší vodorovně nebo svisle?
        /// </summary>
        public WindowAreaContentType ContentType { get { return __ContentType; } set { __ContentType = value; } }
        /// <summary>
        /// Obsahuje true, pokud this prostor obsahuje nějaký splitter
        /// </summary>
        public bool HasSplitter { get { return (ContentType == WindowAreaContentType.SplitterVertical || ContentType == WindowAreaContentType.SplitterHorizontal); } }
        /// <summary>
        /// Určuje, která část obsahu bude při změně velikosti okna "pevná" a která se bude přizůsobovat velikosti
        /// </summary>
        public WindowAreaFixedContent? FixedContent { get { return (HasSplitter ? __FixedContent : null); } set { __FixedContent = value; } }
        /// <summary>
        /// Minimální šířka, platí i pro typ Container
        /// </summary>
        public int? MinWidth { get { return __MinWidth; } set { __MinWidth = value; } }
        /// <summary>
        /// Minimální výška, platí i pro typ Container
        /// </summary>
        public int? MinHeight { get { return __MinHeight; } set { __MinHeight = value; } }
        /// <summary>
        /// Pozice splitteru. Vyjadřuje velikost objektu (šířka/výška ve směru podle orientace <see cref="ContentType"/> na fixní straně od Splitteru (fixní strana = podle <see cref="FixedContent"/>).
        /// <para/>
        /// Pokud tedy <see cref="ContentType"/> = <see cref="WindowAreaContentType.SplitterVertical"/> a <see cref="FixedContent"/> = <see cref="WindowAreaFixedContent.Content1"/>,
        /// pak pozice splitteru vyjadřuje šířku panelu vlevo od splitteru.<br/>
        /// Pokud <see cref="ContentType"/> = <see cref="WindowAreaContentType.SplitterHorizontal"/> a <see cref="FixedContent"/> = <see cref="WindowAreaFixedContent.Content2"/>,
        /// pak pozice splitteru vyjadřuje výšku panelu dole pod splitterem, a to nezávisle na tom jak vysoký bude celý container.<br/>
        /// </summary>
        public int? SplitterPosition { get { return (HasSplitter ? __SplitterPosition : null); } set { __SplitterPosition = value; } }
        /// <summary>
        /// NEPOUŽÍVÁ SE, NEMÁ VÝZNAM
        /// <para/>
        /// Rozsah pohybu splitteru (šířka nebo výška prostoru).
        /// Podle této hodnoty a podle <see cref="FixedContent"/> je následně restorována pozice při vkládání layoutu do nového objektu.
        /// <para/>
        /// Pokud původní prostor měl šířku 1000 px, pak zde je 1000. Pokud fixovaný panel byl Panel2, je to uvedeno v <see cref="FixedContent"/>.
        /// Pozice splitteru zleva byla např. 420 (v <see cref="SplitterPosition"/>). Šířka fixního panelu tedy je (1000 - 420) = 580.
        /// Nyní budeme restorovat XmlLayout do nového prostoru, jehož šířka není 1000, ale 800px.
        /// Protože fixovaný panel je Panel2 (vpravo), pak nová pozice splitteru (zleva) je taková, aby Panel2 měl šířku stejnou jako původně (580): 
        /// nově tedy (800 - 580) = 220.
        /// <para/>
        /// Obdobné přepočty budou provedeny pro jinou situaci, kdy FixedPanel je None = splitter ke "gumový" = proporcionální.
        /// Pak se při restoru přepočte nová pozice splitteru pomocí poměru původní pozice ku Range.
        /// </summary>
        protected int? SplitterRange { get { return (HasSplitter ? __SplitterRange : null); } set { __SplitterRange = value; } }
        /// <summary>
        /// Je pozice splitteru fixovaná? Tedy uživatel s ním nemůže pohybovat?
        /// </summary>
        public bool? IsSplitterPositionFixed { get { return __IsSplitterPositionFixed; } set { __IsSplitterPositionFixed = value; } }
        /// <summary>
        /// ID tohoto prostoru; toto ID následně slouží pro umístění konkrétního obsahu (UserControl = DynamicPage) do konkrétního prostoru.
        /// </summary>
        public string AreaId { get { return (HasSplitter ? null : _FullAreaId); } }
        /// <summary>
        /// Obsahuje všechny prostory určené k umístění UserControlů v rámci this layoutu
        /// </summary>
        public string[] AllAreaIds { get { var allAreaIds = new List<string>(); _AddAllUserAreaIdsTo(allAreaIds); return allAreaIds.ToArray(); } }
        /// <summary>
        /// Pokud je tento prostor rozdělen na dva pomocí <see cref="ContentType"/> s hodnotou <see cref="WindowAreaContentType.SplitterVertical"/> nebo <see cref="WindowAreaContentType.SplitterHorizontal"/>,
        /// pak je zde podprostor vlevo nebo nahoře.<br/>
        /// Pokud není rozdělen, je zde null.
        /// <br/>
        /// Nelze vložit hodnotu. Pokud nastavíme správně <see cref="ContentType"/>, bude zde připraven validní prostor.
        /// </summary>
        public WindowArea Content1 { get { return _GetContent(ref __Content1, 1); } }
        /// <summary>
        /// Pokud je tento prostor rozdělen na dva pomocí <see cref="ContentType"/> s hodnotou <see cref="WindowAreaContentType.SplitterVertical"/> nebo <see cref="WindowAreaContentType.SplitterHorizontal"/>,
        /// pak je zde podprostor vpravo nebo dole.<br/>
        /// Pokud není rozdělen, je zde null.
        /// <br/>
        /// Nelze vložit hodnotu. Pokud nastavíme správně <see cref="ContentType"/>, bude zde připraven validní prostor.
        /// </summary>
        public WindowArea Content2 { get { return _GetContent(ref __Content2, 2); } }
        #endregion
        #region LayoutXml, konverze na WS struktury
        /// <summary>
        /// Vrátí instanci <see cref="Noris.WS.DataContracts.Desktop.Forms.Area"/> vytvořenou z dodané instance <see cref="WindowArea"/>. 
        /// Používá rekurzi i pro všechny svoje Childs.
        /// </summary>
        /// <returns></returns>
        internal Noris.WS.DataContracts.Desktop.Forms.Area CreateWsArea()
        {
            return _CreateWsArea(this);
        }
        /// <summary>
        /// Vrátí instanci <see cref="Noris.WS.DataContracts.Desktop.Forms.Area"/> vytvořenou z dodané instance <see cref="WindowArea"/>. 
        /// Používá rekurzi pro svoje Childs.
        /// </summary>
        /// <param name="winArea"></param>
        /// <returns></returns>
        private static Noris.WS.DataContracts.Desktop.Forms.Area _CreateWsArea(WindowArea winArea)
        {
            if (winArea is null) return null;

            var wsArea = new Noris.WS.DataContracts.Desktop.Forms.Area();
            fillWsParams(winArea, wsArea);
            if (winArea.HasSplitter)
            {
                wsArea.Content1 = _CreateWsArea(winArea.Content1);
                wsArea.Content2 = _CreateWsArea(winArea.Content2);
            }
            return wsArea;

            // Konverze jednotlivých dat - vyjma rekurze:
            void fillWsParams(WindowArea source, Noris.WS.DataContracts.Desktop.Forms.Area target)
            {
                target.AreaId = source._FullAreaId;
                target.ContentType = (source.HasSplitter ? WS.DataContracts.Desktop.Forms.AreaContentType.DxSplitContainer : WS.DataContracts.Desktop.Forms.AreaContentType.DxLayoutItemPanel);
                if (source.HasSplitter)
                {
                    target.SplitterOrientation = source.ContentType == WindowAreaContentType.SplitterHorizontal ? WS.DataContracts.Desktop.Forms.Orientation.Horizontal : WS.DataContracts.Desktop.Forms.Orientation.Vertical;
                    target.SplitterPosition = source.SplitterPosition;
                    target.SplitterRange = source.SplitterRange;
                    target.IsSplitterFixed = source.IsSplitterPositionFixed;
                    target.FixedPanel = source.FixedContent.HasValue ?
                            (Noris.WS.DataContracts.Desktop.Forms.FixedPanel?)(source.FixedContent.Value == WindowAreaFixedContent.Content1 ? WS.DataContracts.Desktop.Forms.FixedPanel.Panel1 : (source.FixedContent.Value == WindowAreaFixedContent.Content2 ? WS.DataContracts.Desktop.Forms.FixedPanel.Panel2 : WS.DataContracts.Desktop.Forms.FixedPanel.None)) :
                            (Noris.WS.DataContracts.Desktop.Forms.FixedPanel?)null; 
                }
            }
        }
        #endregion
    }
    /// <summary>
    /// Druh obsahu jednoho prostoru: prostor pro finální control nebo prostor rozdělený na dvě oblasti?
    /// </summary>
    public enum WindowAreaContentType
    {
        /// <summary>
        /// V tomto prostoru bude umístěn finální User control
        /// </summary>
        UserControl,
        /// <summary>
        /// V tomto prostoru bude umístěn svislý splitter a dvě oblasti, vlevo = <see cref="WindowArea.Content1"/> a vpravo = <see cref="WindowArea.Content2"/>.
        /// </summary>
        SplitterVertical,
        /// <summary>
        /// V tomto prostoru bude umístěn vodorovný splitter a dvě oblasti, nahoře = <see cref="WindowArea.Content1"/> a dole = <see cref="WindowArea.Content2"/>.
        /// </summary>
        SplitterHorizontal,
    }
    /// <summary>
    /// Určuje, která část obsahu bude při změně velikosti okna "pevná" a která se bude přizůsobovat velikosti
    /// </summary>
    public enum WindowAreaFixedContent
    {
        /// <summary>
        /// Žádná část není pevná = při změně velikosti okna se splitter posouvá relativně ke změně velikosti
        /// </summary>
        None,
        /// <summary>
        /// První část (<see cref="WindowArea.Content1"/>) je pevná a nemění velikost
        /// </summary>
        Content1,
        /// <summary>
        /// Druhá část (<see cref="WindowArea.Content2"/>) je pevná a nemění velikost
        /// </summary>
        Content2
    }
}
namespace Noris.Srv
{
    /// <summary>
    /// Třída zajišťující řízenou invokaci eventhandleru, podle zadaných nebo defaultních parametrů <b>InvokeOptions</b>.
    /// <para/>
    /// Standardně je event deklarován typicky: <c> public event CancelEventHandler BeforePopup;</c><br/>
    /// A běžně se vyvolává například: <c> this.BeforePopup?.Invoke(this, new CancelEventArgs());</c><br/>
    /// Do eventu může být zaregistrováno více cílových metod = handlerů události. A uvedeným postupem není možno nijak řídit, co se stane mezi jednotlivými voláními cílových handlerů.
    /// A není možno ani logovat časy jednotlivých handlerů.
    /// <para/>
    /// Proto je zde tato třída, která obojí dokáže - s využitím řízeného vyvolání jednotlivých handlerů.
    /// Rozdíl je pouze v tom, že namísto <c>Invoke</c> handleru se použije: 
    /// <code> 
    /// this.BeforePopup?.NrsInvoke(this, new CancelEventArgs() [, InvokeOptions invokeOptions]);
    /// </code><br/>
    /// Kde v <c>InvokeOptions invokeOptions</c> lze řídit, jak se detailně loguje a chová invokování handlerů.
    /// </summary>
    public static class MultiTargetInvoke
    {
        /// <summary>
        /// Detailně řízená invokace události (per-one-target)
        /// </summary>
        /// <param name="targetEvent"></param>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        /// <param name="traceEventName"></param>
        /// <param name="invokeOptions"></param>
        public static void NrsInvoke(this MulticastDelegate targetEvent, object sender, object eventArgs, string traceEventName = null, InvokeOptions invokeOptions = null)
        {
            if (targetEvent is null) return;

            var targetActions = targetEvent.GetInvocationList();
            if (targetActions.Length == 0) return;

            if (invokeOptions is null) invokeOptions = InvokeOptions.Default;            // Implicitní řídící argumenty
            string eventName = traceEventName ?? invokeOptions?.TraceEventName;
            using (createScope(invokeOptions.OuterEventScopeTreshold, eventName, null))
            {
                foreach (var targetAction in targetActions)
                {
                    // Nějaká akce před jedním target handlerem:
                    invokeOptions.ActionBeforeSingleTarget?.Invoke(eventArgs);           // Toto není vlastní událost, ale režijní akce před vyvoláním jednoho cílového handleru

                    // Vlastní jeden eventhandler je zde:
                    using (createScope(invokeOptions.SingleTargetScopeTreshold, eventName, targetAction))
                    {
                        targetAction.DynamicInvoke(sender, eventArgs);
                    }

                    // Nějaká akce po jednom target handleru:
                    invokeOptions.ActionBeforeSingleTarget?.Invoke(eventArgs);           // Toto není vlastní událost, ale režijní akce po vyvolání jednoho cílového handleru
                }
            }

            // Vytvořím trace scope, který v Konstruktoru zapíše Begin a v Dispose zapíše End
            IDisposable createScope(int treshold, string traceName, System.Delegate targetDelegate)
            {
                if (treshold < 0) return null;                                           // Záporný treshold nepíše nic

                // Pokud mám dodanou Target metodu, tak si vytáhnu její Name:
                string targetName = "";
                if (targetDelegate != null)
                {
                    string targetType = targetDelegate.Target.GetType().FullName;
                    string targetMetod = targetDelegate.Method.Name;
                    targetName = targetType + "." + targetMetod;
                }




                return null;
            }
        }
        /// <summary>
        /// Předvolby pro řízení vyvolávání události per-one-target
        /// </summary>
        public class InvokeOptions
        {
            #region Konstruktory, statické instance
            /// <summary>
            /// Defaultní parametry, kdy zápis do trace se provede po přesáhnutí tresholdu 250 milisec na jeden target handler, a při přesáhnutí celkové doby invokace (=všechny handlery) nad 500 milisec.
            /// </summary>
            public static InvokeOptions Default 
            { 
                get 
                {
                    if (__Default is null)
                        __Default = new InvokeOptions(500, 250);
                    return __Default;
                }
            }
            private static InvokeOptions __Default;
            /// <summary>
            /// Konkrétní parametry, kdy invokování událostí bude do trace zapsáno zcela vždy, i pro neměřitelně malé časy
            /// </summary>
            public static InvokeOptions FullTrace
            {
                get
                {
                    if (__FullTrace is null)
                        __FullTrace = new InvokeOptions(0, 0);
                    return __FullTrace;
                }
            }
            private static InvokeOptions __FullTrace;
            /// <summary>
            /// Konkrétní parametry, kdy invokování událostí nebude nikdy zapisovat do trace
            /// </summary>
            public static InvokeOptions NoTrace
            {
                get
                {
                    if (__NoTrace is null)
                        __NoTrace = new InvokeOptions(-1, -1);
                    return __NoTrace;
                }
            }
            private static InvokeOptions __NoTrace;
            /// <summary>
            /// Vytvoří parametry pro detailní řízení invokace eventhandleru (se zadáním tresholdů)
            /// </summary>
            /// <param name="outerEventScopeTreshold">Treshold pro zápis trace za celý event (=čas všech target handlerů): -1=nikdy | 0=vždy | +nn = až po překročení času tresholdu (v milisekundách)</param>
            /// <param name="singleTargetScopeTreshold">Treshold pro zápis trace za každý jednotlivý target handlerů (jedna cílová metoda, jichž může být vícero): -1=nikdy | 0=vždy | +nn = až po překročení času tresholdu (v milisekundách)</param>
            /// <param name="traceEventName">Jméno události do trace</param>
            public InvokeOptions(int outerEventScopeTreshold, int singleTargetScopeTreshold, string traceEventName = null)
            {
                this.TraceEventName = traceEventName;
                this.OuterEventScopeTreshold = outerEventScopeTreshold;
                this.SingleTargetScopeTreshold = singleTargetScopeTreshold;
                this.ActionBeforeSingleTarget = null;
                this.ActionAfterSingleTarget = null;
            }
            /// <summary>
            /// Vytvoří parametry pro detailní řízení invokace eventhandleru (se zadáním tresholdů) a se zadáním akcí Před/Po provedení jednoho handleru
            /// </summary>
            /// <param name="outerEventScopeTreshold">Treshold pro zápis trace za celý event (=čas všech target handlerů): -1=nikdy | 0=vždy | +nn = až po překročení času tresholdu (v milisekundách)</param>
            /// <param name="singleTargetScopeTreshold">Treshold pro zápis trace za každý jednotlivý target handlerů (jedna cílová metoda, jichž může být vícero): -1=nikdy | 0=vždy | +nn = až po překročení času tresholdu (v milisekundách)</param>
            /// <param name="actionBeforeSingleTarget">Akce, která bude volaná před zahájením každého jednoho target handleru</param>
            /// <param name="actionAfterSingleTarget">Akce, která bude volaná po dokončení každého jednoho target handleru</param>
            /// <param name="traceEventName">Jméno události do trace</param>
            public InvokeOptions(int outerEventScopeTreshold, int singleTargetScopeTreshold, Action<object> actionBeforeSingleTarget, Action<object> actionAfterSingleTarget, string traceEventName = null)
            {
                this.TraceEventName = traceEventName;
                this.OuterEventScopeTreshold = outerEventScopeTreshold;
                this.SingleTargetScopeTreshold = singleTargetScopeTreshold;
                this.ActionBeforeSingleTarget = null;
                this.ActionAfterSingleTarget = null;
            }
            #endregion
            #region Public data
            /// <summary>
            /// Jméno události vepisované do trace
            /// </summary>
            public readonly string TraceEventName;
            /// <summary>
            /// Treshold pro zápis trace za celý event (=čas všech target handlerů):<br/>
            /// 0 = Trace se zapisuje vždy;<br/>
            /// +nn = Trace se zapíše jen při překročení daného počtu milisekund;<br/>
            /// -1 = Trace se nezapisuje nikdy
            /// </summary>
            public readonly int OuterEventScopeTreshold;
            /// <summary>
            /// Treshold pro zápis trace za každý jednotlivý target handlerů (jedna cílová metoda, jichž může být vícero):<br/>
            /// 0 = Trace se zapisuje vždy;<br/>
            /// +nn = Trace se zapíše jen při překročení daného počtu milisekund;<br/>
            /// -1 = Trace se nezapisuje nikdy
            /// </summary>
            public readonly int SingleTargetScopeTreshold;
            /// <summary>
            /// Akce volaná těsně před vyvoláním jednoho target handleru; dostává jako parametr argument eventu připravený volající metodou, který bude předán do handleru
            /// </summary>
            public Action<object> ActionBeforeSingleTarget;
            /// <summary>
            /// Akce volaná těsně po vyvolání jednoho target handleru; dostává jako parametr argument eventu tak jak jej vrátil handler
            /// </summary>
            public Action<object> ActionAfterSingleTarget;
            #endregion
        }
    }
    /// <summary>
    /// Tester : vrstva Manager
    /// </summary>
    public class MultiTargetInvokeTest
    {
        /// <summary>
        /// Spustí test, vrátí string s výsledky
        /// </summary>
        /// <returns></returns>
        public static string RunTest()
        {
            var testMngr = new MultiTargetInvokeTest();
            var result = testMngr._RunTest();
            return result;
        }
        /// <summary>
        /// Spustí test, vrátí string s výsledky
        /// </summary>
        /// <returns></returns>
        private string _RunTest()
        {
            DateTime start = DateTime.Now;

            string result = "";
            string time;

            // Vytvořím instanci, která bude provádět testy, a zaregistruji několik cílových eventhandlerů:
            var testInst = new TestInstance();
            testInst.CountChanged1 += _TestHandler1a;

            testInst.CountChanged5 += _TestHandler5a;
            testInst.CountChanged5 += _TestHandler5b;
            testInst.CountChanged5 += _TestHandler5c;
            testInst.CountChanged5 += _TestHandler5d;
            testInst.CountChanged5 += _TestHandler5e;

            // TargetHandlers = 1
            _TestCycleCount = 1000;

            result += "\r\n - - - - - - - - -    1   target handler     - - - - - - - - - - - - - - - - - - \r\n\r\n";

            // 11. Nativní event s prázdným obsahem eventhandleru (když _DoSomeLoops = 0, nevolá se ani výkonná metoda _DoSomeTime() ) => měřím jen režii jaderného kódu):
            _AppEventLoops = 0;
            _EventHandlerCount = 0;
            time = testInst.RunTestNativeInvoke1(_TestCycleCount);
            result += $"11. Native Invoke EMPTY event for 1 target: time = <b>{time} microsecs</b>; handlers count = {_EventHandlerCount};\r\n";

            // 12. Nativní event s provedením něčeho uvnitř (když _DoSomeLoops > 0, pak volám _DoSomeTime()):
            _AppEventLoops = 100;
            _EventHandlerCount = 0;
            time = testInst.RunTestNativeInvoke1(_TestCycleCount);
            result += $"12. Native Invoke WORKING event for 1 target: time = <b>{time} microsecs</b>; handlers count = {_EventHandlerCount};\r\n";

            result += "\r\n";

            // 13. NrsInvoke event s prázdným obsahem eventhandleru (když _DoSomeLoops = 0, nevolá se ani výkonná metoda _DoSomeTime() ) => měřím jen režii jaderného kódu):
            _AppEventLoops = 0;
            _EventHandlerCount = 0;
            time = testInst.RunTestNrsInvoke1(_TestCycleCount);
            result += $"13. NrsInvoke EMPTY event for 1 target: time = <b>{time} microsecs</b>; handlers count = {_EventHandlerCount};\r\n";

            // 14. NrsInvoke event s provedením něčeho uvnitř = obdobně jako krok 2, ale místo event?.Invoke() se provádí event?.NrsInvoke():
            _AppEventLoops = 100;
            _EventHandlerCount = 0;
            time = testInst.RunTestNrsInvoke1(_TestCycleCount);
            result += $"14. NrsInvoke WORKING event for 1 target: time = <b>{time} microsecs</b>; handlers count = {_EventHandlerCount};\r\n";

            result += "\r\n - - - - - - - - -    5   target handlers    - - - - - - - - - - - - - - - - - - \r\n\r\n";

            // TargetHandlers = 5
            _TestCycleCount = 200;

            // 51. Nativní event s prázdným obsahem eventhandleru (když _DoSomeLoops = 0, nevolá se ani výkonná metoda _DoSomeTime() ) => měřím jen režii jaderného kódu):
            _AppEventLoops = 0;
            _EventHandlerCount = 0;
            time = testInst.RunTestNativeInvoke5(_TestCycleCount);
            result += $"51. Native Invoke EMPTY event for 5 targets: time = <b>{time} microsecs</b>; handlers count = {_EventHandlerCount};\r\n";

            // 52. Nativní event s provedením něčeho uvnitř (když _DoSomeLoops > 0, pak volám _DoSomeTime()):
            _AppEventLoops = 100;
            _EventHandlerCount = 0;
            time = testInst.RunTestNativeInvoke5(_TestCycleCount);
            result += $"52. Native Invoke WORKING event for 5 targets: time = <b>{time} microsecs</b>; handlers count = {_EventHandlerCount};\r\n";

            result += "\r\n";

            // 53. NrsInvoke event s prázdným obsahem eventhandleru (když _DoSomeLoops = 0, nevolá se ani výkonná metoda _DoSomeTime() ) => měřím jen režii jaderného kódu):
            _AppEventLoops = 0;
            _EventHandlerCount = 0;
            time = testInst.RunTestNrsInvoke5(_TestCycleCount);
            result += $"53. NrsInvoke EMPTY event for 5 targets: time = <b>{time} microsecs</b>; handlers count = {_EventHandlerCount};\r\n";

            // 54. NrsInvoke event s provedením něčeho uvnitř = obdobně jako krok 2, ale místo event?.Invoke() se provádí event?.NrsInvoke():
            _AppEventLoops = 100;
            _EventHandlerCount = 0;
            time = testInst.RunTestNrsInvoke5(_TestCycleCount);
            result += $"54. NrsInvoke WORKING event for 5 targets: time = <b>{time} microsecs</b>; handlers count = {_EventHandlerCount};\r\n";

            result += "\r\n - - - - - - - - -    test done              - - - - - - - - - - - - - - - - - - \r\n\r\n";

            DateTime end = DateTime.Now;
            TimeSpan duration = end - start;

            result += $"Total test time: {duration.TotalSeconds} seconds;";
            return result;
        }

        // Několik cílových eventhandlerů :
        private void _TestHandler1a(object sender, EventArgs e) { _EventHandlerCount++; if (_AppEventLoops > 0) this._DoSomeTime(); }
        
        private void _TestHandler5a(object sender, EventArgs e) { _EventHandlerCount++; if (_AppEventLoops > 0) this._DoSomeTime(); }
        private void _TestHandler5b(object sender, EventArgs e) { _EventHandlerCount++; if (_AppEventLoops > 0) this._DoSomeTime(); }
        private void _TestHandler5c(object sender, EventArgs e) { _EventHandlerCount++; if (_AppEventLoops > 0) this._DoSomeTime(); }
        private void _TestHandler5d(object sender, EventArgs e) { _EventHandlerCount++; if (_AppEventLoops > 0) this._DoSomeTime(); }
        private void _TestHandler5e(object sender, EventArgs e) { _EventHandlerCount++; if (_AppEventLoops > 0) this._DoSomeTime(); }

        /// <summary>
        /// Něco jako udělej, simuluj aplikační eventhandler
        /// </summary>
        private void _DoSomeTime()
        {
            int loops = _AppEventLoops;
            string content = "";
            for (int i = 0; i < loops; i++)
                content = DateTime.Now.ToString();                             // Nějaká akce, která trvá nějakou dobu, jako simulace výkonu aplikačního eventhandleru
        }
        private int _TestCycleCount;
        private int _AppEventLoops;
        private int _EventHandlerCount;

        /// <summary>
        /// Tester : vrstva Worker
        /// </summary>
        public class TestInstance
        {
            #region Konstrukce a časomíra
            /// <summary>
            /// Konstruktor
            /// </summary>
            public TestInstance()
            {
                __Frequency = System.Diagnostics.Stopwatch.Frequency;
                __StopWatch = new System.Diagnostics.Stopwatch();
                __StopWatch.Start();
            }
            /// <summary>
            /// Kolik tiků má jedna sekunda?
            /// </summary>
            private decimal __Frequency;
            /// <summary>
            /// Krystalové hodinky Quartz
            /// </summary>
            System.Diagnostics.Stopwatch __StopWatch;
            /// <summary>
            /// Aktuální čas na krystalových hodinkách
            /// </summary>
            private long _TimeCurrent { get { return __StopWatch.ElapsedTicks; } }
            /// <summary>
            /// Kolik mikrosekund trvalo od startu <paramref name="startTime"/> do teď?
            /// </summary>
            /// <param name="startTime"></param>
            /// <returns></returns>
            private decimal _GetTimeIntervalMicrosec(long startTime)
            {
                long currentTime = _TimeCurrent;
                return (1000000m * ((decimal)(currentTime - startTime))) / __Frequency;
            }
            #endregion
            #region Run test CountChanged1
            /// <summary>
            /// Provede daný počet cyklů <paramref name="cycleCount"/> a v každém cyklu vyvolá event <see cref="CountChanged1"/> v režimu NativeInvoke
            /// </summary>
            /// <param name="cycleCount"></param>
            /// <returns></returns>
            public string RunTestNativeInvoke1(int cycleCount)
            {
                var start = _TimeCurrent;
                for (int i = 0; i < cycleCount; i++)
                {
                    CountChanged1?.Invoke(this, EventArgs.Empty);
                }
                var time = _GetTimeIntervalMicrosec(start);
                return Math.Round(time, 0).ToString("### ### ##0").Trim();
            }
            /// <summary>
            /// Provede daný počet cyklů <paramref name="cycleCount"/> a v každém cyklu vyvolá event <see cref="CountChanged1"/> v režimu NrsInvoke
            /// </summary>
            /// <param name="cycleCount"></param>
            /// <returns></returns>
            public string RunTestNrsInvoke1(int cycleCount)
            {
                var start = _TimeCurrent;
                for (int i = 0; i < cycleCount; i++)
                {
                    CountChanged1?.NrsInvoke(this, EventArgs.Empty);
                }
                var time = _GetTimeIntervalMicrosec(start);
                return Math.Round(time, 0).ToString("### ### ##0").Trim();
            }
            /// <summary>
            /// Událost volaná po každé smyčce, cílem nechť je jeden target handler
            /// </summary>
            public event EventHandler CountChanged1;
            #endregion
            #region Run test CountChanged5
            /// <summary>
            /// Provede daný počet cyklů <paramref name="cycleCount"/> a v každém cyklu vyvolá event <see cref="CountChanged5"/> v režimu NativeInvoke
            /// </summary>
            /// <param name="cycleCount"></param>
            /// <returns></returns>
            public string RunTestNativeInvoke5(int cycleCount)
            {
                var start = _TimeCurrent;
                for (int i = 0; i < cycleCount; i++)
                {
                    CountChanged5?.Invoke(this, EventArgs.Empty);
                }
                var time = _GetTimeIntervalMicrosec(start);
                return Math.Round(time, 0).ToString("### ### ##0").Trim();
            }
            /// <summary>
            /// Provede daný počet cyklů <paramref name="cycleCount"/> a v každém cyklu vyvolá event <see cref="CountChanged5"/> v režimu NrsInvoke
            /// </summary>
            /// <param name="cycleCount"></param>
            /// <returns></returns>
            public string RunTestNrsInvoke5(int cycleCount)
            {
                var start = _TimeCurrent;
                for (int i = 0; i < cycleCount; i++)
                {
                    CountChanged5?.NrsInvoke(this, EventArgs.Empty);
                }
                var time = _GetTimeIntervalMicrosec(start);
                return Math.Round(time, 0).ToString("### ### ##0").Trim();
            }
            /// <summary>
            /// Událost volaná po každé smyčce, cílem nechť je pět target handlerů
            /// </summary>
            public event EventHandler CountChanged5;
            #endregion
        }
    }
}
namespace Noris.WS.DataContracts.Desktop.Forms
{
    #region třídy layoutu: FormLayout, Area; enum AreaContentType
    /// <summary>
    /// Deklarace layoutu: obsahuje popis okna a popis rozvržení vnitřních prostor
    /// </summary>
    internal class FormLayout
    {
        /// <summary>
        /// Okno je tabované (true) nebo plovoucí (false)
        /// </summary>
        public bool? IsTabbed { get; set; }
        /// <summary>
        /// Stav okna (Maximized / Normal); stav Minimized se sem neukládá, za stavu <see cref="IsTabbed"/> se hodnota ponechá na předešlé hodnotě
        /// </summary>
        public FormWindowState? FormState { get; set; }
        /// <summary>
        /// Souřadnice okna platné při <see cref="FormState"/> == <see cref="FormWindowState.Normal"/> a ne <see cref="IsTabbed"/>
        /// </summary>
        public System.Drawing.Rectangle? FormNormalBounds { get; set; }
        /// <summary>
        /// Zoom aktuální
        /// </summary>
        public decimal Zoom { get; set; }
        /// <summary>
        /// Laoyut struktury prvků - základní úroveň, obsahuje rekurzivně další instance <see cref="Area"/>
        /// </summary>
        public Area RootArea { get; set; }
    }
    /// <summary>
    /// Rozložení pracovní plochy, jedna plocha a její využití, rekurzivní třída.
    /// Obsah této třídy se persistuje do XML.
    /// POZOR tedy: neměňme jména [PropertyName("xxx")], jejich hodnoty jsou uloženy v XML tvaru na serveru 
    /// a podle atributu PropertyName budou načítána do aktuálních properties.
    /// Lze měnit jména properties.
    /// <para/>
    /// Obecně k XML persistoru: není nutno používat atribut [PropertyName("xxx")], ale pak musíme zajistit neměnnost názvů properties ve třídě.
    /// </summary>
    internal class Area
    {
        #region Data
        /// <summary>
        /// ID prostoru
        /// </summary>
        [XmlSerializer.PropertyName("AreaID")]
        public string AreaId { get; set; }
        /// <summary>
        /// Typ obsahu = co v prostoru je
        /// </summary>
        [XmlSerializer.PropertyName("Content")]
        public AreaContentType ContentType { get; set; }
        /// <summary>
        /// Uživatelský identifikátor
        /// </summary>
        [XmlSerializer.PropertyName("ControlID")]
        public string ControlId { get; set; }
        /// <summary>
        /// Text controlu, typicky jeho titulek
        /// </summary>
        [XmlSerializer.PersistingEnabled(false)]
        public string ContentText { get; set; }
        /// <summary>
        /// Orientace splitteru
        /// </summary>
        [XmlSerializer.PropertyName("SplitterOrientation")]
        public Orientation? SplitterOrientation { get; set; }
        /// <summary>
        /// Fixovaný splitter?
        /// </summary>
        [XmlSerializer.PropertyName("IsSplitterFixed")]
        public bool? IsSplitterFixed { get; set; }
        /// <summary>
        /// Fixovaný panel
        /// </summary>
        [XmlSerializer.PropertyName("FixedPanel")]
        public FixedPanel? FixedPanel { get; set; }
        /// <summary>
        /// Minimální velikost pro Panel1
        /// </summary>
        [XmlSerializer.PropertyName("MinSize1")]
        public int? MinSize1 { get; set; }
        /// <summary>
        /// Minimální velikost pro Panel2
        /// </summary>
        [XmlSerializer.PropertyName("MinSize2")]
        public int? MinSize2 { get; set; }
        /// <summary>
        /// Pozice splitteru absolutní, zleva nebo shora
        /// </summary>
        [XmlSerializer.PropertyName("SplitterPosition")]
        public int? SplitterPosition { get; set; }
        /// <summary>
        /// Rozsah pohybu splitteru (šířka nebo výška prostoru).
        /// Podle této hodnoty a podle <see cref="FixedPanel"/> je následně restorována pozice při vkládání layoutu do nového objektu.
        /// <para/>
        /// Pokud původní prostor měl šířku 1000 px, pak zde je 1000. Pokud fixovaný panel byl Panel2, je to uvedeno v <see cref="FixedPanel"/>.
        /// Pozice splitteru zleva byla např. 420 (v <see cref="SplitterPosition"/>). Šířka fixního panelu tedy je (1000 - 420) = 580.
        /// Nyní budeme restorovat XmlLayout do nového prostoru, jehož šířka není 1000, ale 800px.
        /// Protože fixovaný panel je Panel2 (vpravo), pak nová pozice splitteru (zleva) je taková, aby Panel2 měl šířku stejnou jako původně (580): 
        /// nově tedy (800 - 580) = 220.
        /// <para/>
        /// Obdobné přepočty budou provedeny pro jinou situaci, kdy FixedPanel je None = splitter ke "gumový" = proporcionální.
        /// Pak se při restoru přepočte nová pozice splitteru pomocí poměru původní pozice ku Range.
        /// </summary>
        [XmlSerializer.PropertyName("SplitterRange")]
        public int? SplitterRange { get; set; }
        /// <summary>
        /// Obsah panelu 1 (rekurzivní instance téže třídy)
        /// </summary>
        [XmlSerializer.PropertyName("Content1")]
        public Area Content1 { get; set; }
        /// <summary>
        /// Obsah panelu 2 (rekurzivní instance téže třídy)
        /// </summary>
        [XmlSerializer.PropertyName("Content2")]
        public Area Content2 { get; set; }
        #endregion
        #region IsEqual
        public static bool IsEqual(Area area1, Area area2)
        {
            if (!_IsEqualNull(area1, area2)) return false;     // Jeden je null a druhý není
            if (area1 == null) return true;                    // area1 je null (a druhý taky) = jsou si rovny

            if (area1.ContentType != area2.ContentType) return false;    // Jiný druh obsahu
                                                                         // Obě area mají shodný typ obsahu:
            bool contentIsSplitted = (area1.ContentType == AreaContentType.DxSplitContainer || area1.ContentType == AreaContentType.WfSplitContainer);
            if (!contentIsSplitted) return true;               // Obsah NENÍ split container = z hlediska porovnání layoutu na koncovém obsahu nezáleží, jsou si rovny.

            // Porovnáme deklaraci vzhledu SplitterContaineru:
            if (!_IsEqualNullable(area1.SplitterOrientation, area1.SplitterOrientation)) return false;
            if (!_IsEqualNullable(area1.IsSplitterFixed, area1.IsSplitterFixed)) return false;
            if (!_IsEqualNullable(area1.FixedPanel, area1.FixedPanel)) return false;
            if (!_IsEqualNullable(area1.MinSize1, area1.MinSize1)) return false;
            if (!_IsEqualNullable(area1.MinSize2, area1.MinSize2)) return false;

            // Porovnáme deklarovanou pozici splitteru:
            if (area1._SplitterPositionComparable != area2._SplitterPositionComparable) return false;

            // Porovnáme rekurzivně definice :
            if (!IsEqual(area1.Content1, area2.Content1)) return false;
            if (!IsEqual(area1.Content2, area2.Content2)) return false;

            return true;
        }
        private static bool _IsEqualNull(object a, object b)
        {
            bool an = a is null;
            bool bn = b is null;
            return (an == bn);
        }
        private static bool _IsEqualNullable<T>(T? a, T? b) where T : struct, IComparable
        {
            bool av = a.HasValue;
            bool bv = b.HasValue;
            if (av && bv) return (a.Value.CompareTo(b.Value) == 0);         // Obě mají hodnotu: výsledek = jsou si hodnoty rovny?
            if (av || bv) return false;         // Některý má hodnotu? false, protože jen jeden má hodnotu (kdyby měly hodnotu oba, skončili bychom dřív)
            return true;                        // Obě jsou null
        }
        private int _SplitterPositionComparable
        {
            get
            {
                var fixedPanel = this.FixedPanel ?? Forms.FixedPanel.Panel1;
                switch (fixedPanel)
                {
                    case Forms.FixedPanel.Panel1:
                        if (this.SplitterPosition.HasValue && this.SplitterRange.HasValue) return this.SplitterPosition.Value;
                        return 0;
                    case Forms.FixedPanel.Panel2:
                        if (this.SplitterPosition.HasValue && this.SplitterRange.HasValue) return this.SplitterPosition.Value - this.SplitterRange.Value;
                        return 0;
                    case Forms.FixedPanel.None:
                        if (this.SplitterPosition.HasValue && this.SplitterRange.HasValue && this.SplitterRange.Value > 0) return this.SplitterPosition.Value * 10000 / this.SplitterRange.Value;
                        return 0;
                }
                return 0;
            }
        }
        #endregion
    }
    /// <summary>
    /// Typ obsahu prostoru
    /// </summary>
    internal enum AreaContentType
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Žádný control
        /// </summary>
        Empty,
        /// <summary>
        /// Prázdný DxLayoutItemPanel
        /// </summary>
        EmptyLayoutPanel,
        /// <summary>
        /// Standardní naplněný DxLayoutItemPanel
        /// </summary>
        DxLayoutItemPanel,
        /// <summary>
        /// SplitContainer typu DevExpress
        /// </summary>
        DxSplitContainer,
        /// <summary>
        /// SplitContainer typu WinForm
        /// </summary>
        WfSplitContainer
    }
    /// <summary>
    /// Specifies how a form window is displayed.
    /// Like: System.Windows.Forms.FormWindowState
    /// </summary>
    public enum FormWindowState
    {
        /// <summary>
        /// A default sized window.
        /// </summary>
        Normal = 0,
        /// <summary>
        /// A minimized window.
        /// </summary>
        Minimized = 1,
        /// <summary>
        /// A maximized window.
        /// </summary>
        Maximized = 2
    }
    /// <summary>
    /// Specifies the orientation of controls or elements of controls.
    /// Like:  System.Windows.Forms.Orientation
    /// </summary>
    public enum Orientation
    {
        /// <summary>
        /// The control or element is oriented horizontally.
        /// </summary>
        Horizontal = 0,
        /// <summary>
        /// The control or element is oriented vertically.
        /// </summary>
        Vertical = 1
    }
    /// <summary>
    /// Specifies that System.Windows.Forms.SplitContainer.Panel1, System.Windows.Forms.SplitContainer.Panel2, or neither panel is fixed.
    /// Like:  System.Windows.Forms.FixedPanel
    /// </summary>
    public enum FixedPanel
    {
        /// <summary>
        /// Specifies that neither System.Windows.Forms.SplitContainer.Panel1, System.Windows.Forms.SplitContainer.Panel2 is fixed. A System.Windows.Forms.Control.Resize event affects both panels.
        /// </summary>
        None = 0,
        /// <summary>
        /// Specifies that System.Windows.Forms.SplitContainer.Panel1 is fixed. A System.Windows.Forms.Control.Resize event affects only System.Windows.Forms.SplitContainer.Panel2.
        /// </summary>
        Panel1 = 1,
        /// <summary>
        /// Specifies that System.Windows.Forms.SplitContainer.Panel2 is fixed. A System.Windows.Forms.Control.Resize event affects only System.Windows.Forms.SplitContainer.Panel1.
        /// </summary>
        Panel2 = 2
    }
    #endregion
}
namespace Noris.WS.DataContracts.Desktop.Data
{
    using System.Drawing;
    #region SvgImageArrayInfo a SvgImageArrayItem : Třída, která obsahuje data o sadě ikon SVG, pro jejich kombinaci do jedné výsledné ikony
    /// <summary>
    /// Třída, která obsahuje data o sadě ikon SVG, pro jejich kombinaci do jedné výsledné ikony (základní ikona plus jiná ikona jako její overlay).
    /// </summary>
    internal class SvgImageArrayInfo
    {
        #region Tvorba a public property
        /// <summary>
        /// Konstruktor
        /// </summary>
        public SvgImageArrayInfo()
        {
            Items = new List<SvgImageArrayItem>();
        }
        /// <summary>
        /// Konstruktor, rovnou přidá první obrázek do plného umístění (100%)
        /// </summary>
        /// <param name="name"></param>
        public SvgImageArrayInfo(string name)
            : this()
        {
            Add(name);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Count: {Items.Count}";
        }
        /// <summary>
        /// Pole jednotlivých obrázků a jejich umístění
        /// </summary>
        public List<SvgImageArrayItem> Items { get; private set; }
        /// <summary>
        /// Přidá další obrázek, v plném rozměru
        /// </summary>
        /// <param name="name"></param>
        public void Add(string name)
        {
            if (!String.IsNullOrEmpty(name))
                Items.Add(new SvgImageArrayItem(name));
        }
        /// <summary>
        /// Přidá další obrázek, do daného prostoru.
        /// Velikost musí být nejméně 10, jinak nebude provedeno.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bounds"></param>
        public void Add(string name, Rectangle bounds)
        {
            if (!String.IsNullOrEmpty(name) && bounds.Width >= 10 && bounds.Height >= 10)
                Items.Add(new SvgImageArrayItem(name, bounds));
        }
        /// <summary>
        /// Přidá další obrázek, do daného umístění.
        /// Velikost musí být nejméně 10, jinak nebude provedeno.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="contentAlignment"></param>
        /// <param name="percent"></param>
        public void Add(string name, ContentAlignment contentAlignment, int percent = 50)
        {
            if (!String.IsNullOrEmpty(name) && percent >= 10)
                Items.Add(new SvgImageArrayItem(name, GetRectangle(contentAlignment, percent)));
        }
        /// <summary>
        /// Přidá další prvek.
        /// </summary>
        /// <param name="item"></param>
        public void Add(SvgImageArrayItem item)
        {
            if (item != null)
                Items.Add(item);
        }
        /// <summary>
        /// Obsahuje true, pokud je objekt prázdný
        /// </summary>
        public bool IsEmpty { get { return (Items.Count == 0); } }
        /// <summary>
        /// Vymaže všechny obrázky
        /// </summary>
        public void Clear() { Items.Clear(); }
        #endregion
        #region Podpora
        /// <summary>
        /// Oddělovač dvou prvků <see cref="SvgImageArrayItem.Key"/> v rámci jednoho <see cref="SvgImageArrayInfo.Key"/>
        /// </summary>
        internal const string KeySplitDelimiter = KeyItemEnd + KeyItemBegin;
        /// <summary>
        /// Značka Begin jednoho prvku
        /// </summary>
        internal const string KeyItemBegin = "«";
        /// <summary>
        /// Značka End jednoho prvku
        /// </summary>
        internal const string KeyItemEnd = "»";
        /// <summary>
        /// Vrátí souřadnici prostoru v dané relativní pozici k základnímu prostoru { 0, 0, 100, 100 }.
        /// Lze specifikovat velikost cílového prostoru, ta musí být v rozmezí 16 až <see cref="BaseSize"/> (včetně).
        /// Jde o prostor, do kterého se promítne ikona, v rámci finální velikosti <see cref="BaseSize"/> x <see cref="BaseSize"/>.
        /// </summary>
        /// <param name="contentAlignment"></param>
        /// <param name="percent"></param>
        /// <returns></returns>
        public static Rectangle GetRectangle(ContentAlignment contentAlignment, int percent = 50)
        {
            percent = (percent < 10 ? 10 : (percent > 100 ? 100 : percent));   // Platné rozmezí procent je 10 až 100
            int size = SvgImageArrayInfo.BaseSize * percent / 100;             // Procento => velikost v rozsahu 0-120
            int de = BaseSize - size;                                          // Velikost celého volného prostoru
            int dc = de / 2;                                                   // Velikost pro Center
            switch (contentAlignment)
            {
                case ContentAlignment.TopLeft: return new Rectangle(0, 0, size, size);
                case ContentAlignment.TopCenter: return new Rectangle(dc, 0, size, size);
                case ContentAlignment.TopRight: return new Rectangle(de, 0, size, size);
                case ContentAlignment.MiddleLeft: return new Rectangle(0, dc, size, size);
                case ContentAlignment.MiddleCenter: return new Rectangle(dc, dc, size, size);
                case ContentAlignment.MiddleRight: return new Rectangle(de, dc, size, size);
                case ContentAlignment.BottomLeft: return new Rectangle(0, de, size, size);
                case ContentAlignment.BottomCenter: return new Rectangle(dc, de, size, size);
                case ContentAlignment.BottomRight: return new Rectangle(de, de, size, size);
            }
            return new Rectangle(dc, dc, size, size);
        }
        /// <summary>
        /// Základní velikost
        /// </summary>
        public const int BaseSize = 120;
        #endregion
        #region Serializace
        /// <summary>
        /// Obsahuje (vygeneruje) serializovaný string z this instance
        /// </summary>
        public string Serial { get { return XmlSerializer.Persist.Serialize(this, XmlSerializer.PersistArgs.MinimalXml); } }
        /// <summary>
        /// Klíč: obsahuje klíče všech obrázků <see cref="SvgImageArrayItem.Key"/>.
        /// Lze jej použít jako klíč do Dictionary, protože dvě instance <see cref="SvgImageArrayInfo"/> se stejným klíčem budou mít stejný vzhled výsledného obrázku.
        /// </summary>
        public string Key
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in Items)
                    sb.Append(item.Key);
                string key = sb.ToString();
                return key;
            }
        }
        /// <summary>
        /// Zkusí provést deserializaci
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryDeserialize(string serial, out SvgImageArrayInfo result)
        {   // <?xml version="1.0" encoding="utf-16"?><id-persistent Version="2.00"><id-data><id-value id-value.Type="Noris.Clients.Win.Components.AsolDX.SvgImageArrayInfo"><Items><id-item><id-value ImageName="devav/actions/printexcludeevaluations.svg" /></id-item><id-item><id-value ImageName="devav/actions/about.svg" ImageRelativeBounds="60;60;60;60" /></id-item></Items></id-value></id-data></id-persistent>
            if (!String.IsNullOrEmpty(serial))
            {
                serial = serial.Trim();
                if (serial.StartsWith("<?xml version=") && serial.EndsWith("</id-persistent>"))
                {   // Ze Serial:
                    object data = XmlSerializer.Persist.Deserialize(serial);
                    if (data != null && data is SvgImageArrayInfo array)
                    {
                        result = array;
                        return true;
                    }
                }
                else if (serial.StartsWith(SvgImageArrayInfo.KeyItemBegin) && serial.EndsWith(SvgImageArrayInfo.KeyItemEnd))   //  serial.Contains(SvgImageArrayInfo.KeySplitDelimiter))
                {   // Z Key = ten je ve tvaru:  «name1»«name2<X.Y.W.H>»    rozdělím v místě oddělovače »« ,  získám dva prvky   «name1    a    name2<X.Y.W.H>»   (v prvcích tedy může / nemusí být značka   «   anebo   »     (nemusí být u druhého prvku ze tří :-) )
                    SvgImageArrayInfo array = new SvgImageArrayInfo();
                    string[] serialItems = serial.Split(new string[] { SvgImageArrayInfo.KeySplitDelimiter }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var serialItem in serialItems)
                    {
                        if (SvgImageArrayItem.TryDeserialize(serialItem, out SvgImageArrayItem item))
                            array.Add(item);
                    }
                    if (!array.IsEmpty)
                    {
                        result = array;
                        return true;
                    }
                }
            }
            result = null;
            return false;
        }
        #endregion
    }
    /// <summary>
    /// Jedna ikona, obsažená v <see cref="SvgImageArrayInfo"/> = název ikony <see cref="ImageName"/>
    /// a její relativní umístění v prostoru výsledné ikony <see cref="ImageRelativeBounds"/>.
    /// </summary>
    internal class SvgImageArrayItem
    {
        #region Konstruktor a public data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public SvgImageArrayItem()
        {   // Toto používá víceméně jen deserializace
            ImageName = "";
            ImageRelativeBounds = null;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="name"></param>
        public SvgImageArrayItem(string name)
        {
            ImageName = name;
            ImageRelativeBounds = null;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bounds"></param>
        public SvgImageArrayItem(string name, Rectangle bounds)
        {
            ImageName = name;
            ImageRelativeBounds = bounds;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = $"Name: {ImageName}";
            if (ImageRelativeBounds.HasValue)
                text += $"; Bounds: {ImageRelativeBounds}";
            return text;
        }
        /// <summary>
        /// Pokusí se z dodaného stringu vytvořit a vrátit new instanci.
        /// String se očekává ve formě <see cref="Key"/>.
        /// </summary>
        /// <param name="serialItem"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        internal static bool TryDeserialize(string serialItem, out SvgImageArrayItem item)
        {
            item = null;
            if (String.IsNullOrEmpty(serialItem)) return false;

            serialItem = serialItem
                .Replace(SvgImageArrayInfo.KeyItemBegin, "")
                .Replace(SvgImageArrayInfo.KeyItemEnd, "")
                .Trim();                                      // Odstraníme zbývající Begin a End značky   «   a   »  (pokud tam jsou)
            if (!serialItem.StartsWith("@") && serialItem.IndexOfAny("*?:\t\r\n".ToCharArray()) >= 0) return false;  // Znakem @ začíná GenericSvg, tam jsou pravidla mírnější...

            var parts = serialItem.Split('<', '>');           // Z textu "imagename<0.0.60.30>" vytvořím tři prvky:    "imagename",    "0.0.60.30",    ""
            int count = parts.Length;
            if (parts.Length == 0) return false;
            string name = parts[0];
            if (String.IsNullOrEmpty(name)) return false;
            name = name.Trim();
            Rectangle? bounds = null;
            if (parts.Length > 1)
            {
                var coords = parts[1].Split('.');             // "0.0.60.30";
                if (coords.Length == 4)
                {
                    if (Int32.TryParse(coords[0], out int x) && (x >= 0 && x <= 120) &&
                        Int32.TryParse(coords[1], out int y) && (y >= 0 && y <= 120) &&
                        Int32.TryParse(coords[2], out int w) && (w >= 0 && w <= 120) &&
                        Int32.TryParse(coords[3], out int h) && (h >= 0 && h <= 120))
                        bounds = new Rectangle(x, y, w, h);
                }
            }
            if (!bounds.HasValue)
                item = new SvgImageArrayItem(name);
            else
                item = new SvgImageArrayItem(name, bounds.Value);
            return true;
        }
        /// <summary>
        /// Jméno SVG obrázku
        /// </summary>
        public string ImageName { get; set; }
        /// <summary>
        /// Souřadnice umístění obrázku v cílovém prostoru { 0, 0, <see cref="SvgImageArrayInfo.BaseSize"/>, <see cref="SvgImageArrayInfo.BaseSize"/> }.
        /// Pokud je zde null, bude obrázek umístěn do celého prostoru.
        /// </summary>
        public Rectangle? ImageRelativeBounds { get; set; }
        /// <summary>
        /// Klíč: obsahuje název obrázku a cílový prostor <see cref="ImageRelativeBounds"/>, pokud je zadán, ve formě:
        /// «image&lt;X.Y.W.H&gt;»
        /// </summary>
        public string Key
        {
            get
            {
                string key = SvgImageArrayInfo.KeyItemBegin + this.ImageName.Trim().ToLower();
                if (ImageRelativeBounds.HasValue)
                {
                    var bounds = ImageRelativeBounds.Value;
                    key += $"<{bounds.X}.{bounds.Y}.{bounds.Width}.{bounds.Height}>";
                }
                key += SvgImageArrayInfo.KeyItemEnd;
                return key;
            }
        }
        #endregion
    }
    #endregion
}
