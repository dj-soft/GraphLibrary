using Noris.Clients.Win.Components.AsolDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestDevExpress.AsolDX.News;
using TestDevExpress.Forms;

namespace TestDevExpress.Components
{
    #region class RunTargetInfo : řešení požadavku "Označ určitý kód (třída / metoda) pro jeho snadné automatické spuštění z Ribbonu
    /// <summary>
    /// <see cref="RunTargetInfo"/> : řešení požadavku "Označ určitý kód (třída / metoda) pro jeho snadné automatické spuštění z Ribbonu
    /// <para/>
    /// <b>1. Formulář</b>: Pokud určitý formulář chce být spouštěn z tlačítka v ribbonu okna <see cref="MainAppForm"/>, pak nechť si do záhlaví třídy přidá CustomAtribut <see cref="RunTestFormAttribute"/> a naplní jej.<br/>
    /// Okno aplikace <see cref="MainAppForm"/> si při spuštění najde tyto formuláře, a sestaví je a do Ribbonu zobrazí jejich tlačítka.
    /// <para/>
    /// <b>2. Testovací algoritmus</b>: Pokud určitá třída obsahuje testovací metodu, která chce být spouštěna z Ribbonu okna <see cref="MainAppForm"/> a jejím úkolem je např. něco otestovat, 
    /// pak nechť si do záhlaví třídy přidá CustomAtribut <see cref="RunTestFormAttribute"/> a naplní jej.<br/>
    /// Okno aplikace <see cref="MainAppForm"/> si při spuštění najde tyto formuláře, a sestaví je a do Ribbonu zobrazí jejich tlačítka.
    /// </summary>
    public class RunTargetInfo
    {
        #region Public definiční data
        /// <summary>
        /// Název stránky, kde se bude button nacházet.
        /// null = výchozí: "ZÁKLADNÍ"
        /// </summary>
        public string PageText { get; set; }
        /// <summary>
        /// Pořadí stránky.
        /// Když více prvků bude mít shodný text <see cref="PageText"/> a rozdélné pořadí <see cref="PageOrder"/>, pak se do výsledku akceptuje nejvyšší <see cref="PageOrder"/> v rámci stránky.
        /// </summary>
        public int PageOrder { get; set; }
        /// <summary>
        /// Název grupy, kde se bude button nacházet.
        /// null = výchozí = "FUNKCE"
        /// </summary>
        public string GroupText { get; set; }
        /// <summary>
        /// Pořadí grupy ve stránce.
        /// Když více prvků bude mít shodný text <see cref="GroupText"/> a rozdélné pořadí <see cref="GroupOrder"/>, pak se do výsledku akceptuje nejvyšší <see cref="GroupOrder"/> v rámci grupy.
        /// </summary>
        public int GroupOrder { get; set; }
        /// <summary>
        /// Název buttonu = zobrazený text.
        /// Prázdný text je přípustný, pokud je dána ikona <see cref="ButtonImage"/>.
        /// </summary>
        public string ButtonText { get; set; }
        /// <summary>
        /// Pořadí tlačítka v rámci grupy.
        /// </summary>
        public int ButtonOrder { get; set; }
        /// <summary>
        /// Ikona tlačítka.
        /// Prázdná ikona je přípustná, pokud je dán text <see cref="ButtonText"/>.
        /// </summary>
        public string ButtonImage { get; set; }
        /// <summary>
        /// ToolTip k buttonu
        /// </summary>
        public string ButtonToolTip { get; set; }
        /// <summary>
        /// Spouštět jako Modální okno
        /// </summary>
        public bool RunAsModal { get; set; }
        /// <summary>
        /// Spouštět jako Floating okno
        /// </summary>
        public bool RunAsFloating { get; set; }
        /// <summary>
        /// ToolTip k otevřenému oknu nad záložkou
        /// </summary>
        public string TabViewToolTip { get; set; }
        /// <summary>
        /// true pro použitelný prvek
        /// </summary>
        internal bool IsValid { get { return (!String.IsNullOrEmpty(ButtonText) || !String.IsNullOrEmpty(ButtonImage)); } }
        /// <summary>
        /// Druh spouštění cíle
        /// </summary>
        internal TargetRunType RunType { get; set; }
        /// <summary>
        /// Cílový Type
        /// </summary>
        internal Type RunTargetType { get; set; }
        /// <summary>
        /// Cílová metoda
        /// </summary>
        internal System.Reflection.MethodInfo RunTargetMethod { get; set; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{GroupText}: {ButtonText}";
        }
        /// <summary>
        /// Způsob spuštění cíle
        /// </summary>
        public enum TargetRunType
        {
            /// <summary>
            /// Není co spustit
            /// </summary>
            None,
            /// <summary>
            /// Formulář: vytvořit new instanci a zobrazit ji
            /// </summary>
            Form,
            /// <summary>
            /// Metoda: spustit ji (statická)
            /// </summary>
            StaticMethod
        }
        #endregion
        #region Static vyhledání typů, které obsahují spustitelnou informaci
        /// <summary>
        /// Metoda vrací pole obsahující typy formulářů a instance <see cref="RunTargetInfo"/>, pocházející z atributu třídy <see cref="RunTestFormAttribute"/>
        /// </summary>
        /// <returns></returns>
        public static RunTargetInfo[] GetRunTargets()
        {
            var myAssembly = typeof(RunTargetInfo).Assembly;

            var runFormInfos = new List<RunTargetInfo>();
            DxComponent.GetTypes(myAssembly, t => ContainsRunTargetAttribute(t, runFormInfos));

            return runFormInfos.ToArray();
        }
        /// <summary>
        /// Metoda, která zjistí, zda daný <see cref="Type"/> je/není formulář spustitelný z hlavního okna aplikace.
        /// Side effectem metody je střádání spustitelných formulářů společně s informacemi <see cref="RunTargetInfo"/>, které formuláře deklarují.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="runFormInfos"></param>
        /// <returns></returns>
        private static bool ContainsRunTargetAttribute(Type type, List<RunTargetInfo> runFormInfos)
        {
            if (type is null) return false;

            // Pokud typ je potomkem Formu, pak by mohl obsahovat atribut RunFormInfoAttribute, který specifikuje Form spustitelný z Ribbonu:
            if (type.IsSubclassOf(typeof(System.Windows.Forms.Form)))
            {
                var attrs = type.GetCustomAttributes(typeof(RunTestFormAttribute), true);
                if (attrs.Length > 0)
                {
                    RunTargetInfo runFormInfo = RunTargetInfo.CreateForFormAttribute(type, attrs[0] as RunTestFormAttribute);
                    if (runFormInfo != null && runFormInfo.IsValid)
                        runFormInfos.Add(runFormInfo);
                }
            }

            // Pokud typ obsahuje atribut 'RunTestClassAttribute', pak vyhledáme jeho vlastní (nezděděné) metody s atributem 'RunTestMethodAttribute':
            if (type.GetCustomAttributes(typeof(RunTestClassAttribute), true).Length > 0)
            {
                var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attrs = method.GetCustomAttributes(typeof(RunTestMethodAttribute), true);
                    if (attrs.Length > 0)
                    {
                        RunTargetInfo runMethodInfo = RunTargetInfo.CreateForMethodAttribute(type, method, attrs[0] as RunTestMethodAttribute);
                        if (runMethodInfo != null && runMethodInfo.IsValid)
                            runFormInfos.Add(runMethodInfo);
                    }
                }
            }

            return false;
        }
        /// <summary>
        /// Vytvoří, naplní a vrátí instanci <see cref="RunTargetInfo"/> z dodaného atributu <see cref="RunTestFormAttribute"/>
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="runTestForm"></param>
        /// <returns></returns>
        private static RunTargetInfo CreateForFormAttribute(Type targetType, RunTestFormAttribute runTestForm)
        {
            if (runTestForm is null) return null;
            RunTargetInfo runTargetInfo = new RunTargetInfo();
            runTargetInfo.PageText = runTestForm.PageText;
            runTargetInfo.PageOrder = runTestForm.PageOrder;
            runTargetInfo.GroupText = runTestForm.GroupText;
            runTargetInfo.GroupOrder = runTestForm.GroupOrder;
            runTargetInfo.ButtonText = runTestForm.ButtonText;
            runTargetInfo.ButtonOrder = runTestForm.ButtonOrder;
            runTargetInfo.ButtonImage = runTestForm.ButtonImage;
            runTargetInfo.ButtonToolTip = runTestForm.ButtonToolTip;
            runTargetInfo.RunType = TargetRunType.Form;
            runTargetInfo.RunTargetType = targetType;
            runTargetInfo.RunAsFloating = runTestForm.RunAsFloating;
            runTargetInfo.RunAsModal = runTestForm.RunAsModal;
            runTargetInfo.TabViewToolTip = runTestForm.TabViewToolTip;
            return runTargetInfo;
        }
        /// <summary>
        /// Vytvoří, naplní a vrátí instanci <see cref="RunTargetInfo"/> z dodaného atributu <see cref="RunTestMethodAttribute"/>
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="method"></param>
        /// <param name="runTestForm"></param>
        /// <returns></returns>
        private static RunTargetInfo CreateForMethodAttribute(Type targetType, System.Reflection.MethodInfo method, RunTestMethodAttribute runTestForm)
        {
            if (runTestForm is null) return null;
            RunTargetInfo runTargetInfo = new RunTargetInfo();
            runTargetInfo.PageText = runTestForm.PageText;
            runTargetInfo.PageOrder = runTestForm.PageOrder;
            runTargetInfo.GroupText = runTestForm.GroupText;
            runTargetInfo.GroupOrder = runTestForm.GroupOrder;
            runTargetInfo.ButtonText = runTestForm.ButtonText;
            runTargetInfo.ButtonOrder = runTestForm.ButtonOrder;
            runTargetInfo.ButtonImage = runTestForm.ButtonImage;
            runTargetInfo.ButtonToolTip = runTestForm.ButtonToolTip;
            runTargetInfo.RunType = TargetRunType.StaticMethod;
            runTargetInfo.RunTargetType = targetType;
            runTargetInfo.RunTargetMethod = method;
            return runTargetInfo;
        }
        #endregion
        #region Tvorba deklarace Ribbonu, MenuItemClick, Run
        /// <summary>
        /// Vygeneruje korektní definici prvků do Ribbonu za dané formuláře
        /// </summary>
        /// <param name="runFormInfos"></param>
        /// <param name="pages"></param>
        /// <param name="basicPage"></param>
        public static void CreateRibbonPages(RunTargetInfo[] runFormInfos, List<DataRibbonPage> pages, DataRibbonPage basicPage)
        {
            if (runFormInfos is null || runFormInfos.Length == 0) return;

            // var rfiPages = runFormInfos.GroupBy(i => i.Item2.PageText ?? "").ToArray();

            // Grupy za stránky podle PageText, tříděné podle PageOrder, v rámci jedné stránky podle nejvyšší hodnoty PageOrder:
            var rPages = runFormInfos.CreateSortedGroups(i => (i.PageText ?? ""), i => (i.PageOrder), (a, b) => (a > b ? a : b));
            int pageOrder = basicPage.PageOrder;
            foreach (var rPage in rPages)
            {   // V daném pořadí, které vychází z PageOrder:
                // Stránka Ribbonu: defaultní nebo nová explicitně pojmenovaná:
                DataRibbonPage dxPage = null;
                if (rPage.Item1 == "")
                {   // HomePage:
                    dxPage = basicPage;
                }
                else if (pages.TryFindFirst(out dxPage, p => String.Equals(p.PageText, rPage.Item1)))
                {   // Existující page, nalezena:
                }
                else
                {   // Nová page, vytvoříme:
                    dxPage = new DataRibbonPage() { PageText = rPage.Item1, PageOrder = ++pageOrder };
                    pages.Add(dxPage);
                }

                // RibbonGrupy v rámci jedné stránky = z aktuální stránky rPage vezmu prvky (rPage.Item2), vytvořím skupiny podle GroupText a setřídím podle GroupOrder.Max() :
                var rGroups = rPage.Item2.CreateSortedGroups(i => (i.GroupText ?? ""), i => (i.GroupOrder), (a, b) => (a > b ? a : b));
                int groupOrder = 0;
                foreach (var rGroup in rGroups)
                {   // Jedna grupa za druhou, v pořadí GroupOrder:
                    string groupText = (!String.IsNullOrEmpty(rGroup.Item1) ? rGroup.Item1 : "FUNKCE");
                    DataRibbonGroup dxGroup = new DataRibbonGroup() { GroupText = groupText, GroupOrder = ++groupOrder };
                    dxPage.Groups.Add(dxGroup);

                    // Jednotlivá tlačítka do grupy:
                    var rButtons = rGroup.Item2.ToList();
                    rButtons.Sort((a, b) => a.ButtonOrder.CompareTo(b.ButtonOrder));
                    int buttonOrder = 0;
                    foreach (var rButton in rButtons)
                    {
                        var dxItem = new DataRibbonItem() { Text = rButton.ButtonText, ToolTipText = rButton.ButtonToolTip, ImageName = rButton.ButtonImage, ItemOrder = ++buttonOrder, Tag = rButton };
                        dxItem.ClickAction = RunTargetAction;
                        dxGroup.Items.Add(dxItem);
                    }
                }
            }
        }
        /// <summary>
        /// Akce volaná z buttonu v Ribbonu, jejím úkolem je otevřít dané okno, jehož definice je uložena v Tagu dodaného prvku, anebo spustit testovací funkci
        /// </summary>
        /// <param name="item"></param>
        private static void RunTargetAction(IMenuItem item)
        {
            if (item.Tag is RunTargetInfo runTargetInfo)
                runTargetInfo.Run();
        }
        /// <summary>
        /// Instanční metoda v <see cref="RunTargetInfo"/>, má za úkol otevřít patřičným způsobem svůj formulář
        /// </summary>
        private void Run()
        {
            try
            {
                switch (this.RunType)
                {
                    case TargetRunType.Form:
                        var form = System.Activator.CreateInstance(this.RunTargetType) as System.Windows.Forms.Form;
                        var configIsMdiChild = ((form is IFormStatusWorking iForm) ? iForm.ConfigIsMdiChild : (bool?)null);
                        bool runAsFloating = (configIsMdiChild.HasValue ? !configIsMdiChild.Value : this.RunAsFloating);
                        DxMainAppForm.ShowChildForm(form, runAsFloating, this.TabViewToolTip);
                        break;
                    case TargetRunType.StaticMethod:
                        this.RunTargetMethod?.Invoke(null, null);
                        break;
                }
            }
            catch (Exception exc)
            {
                DxComponent.ShowMessageException(exc);
            }
        }
        #endregion
    }
    #region atribut RunTestFormAttribute
    /// <summary>
    /// Dekorátor třídy, která chce mít svoji ikonu v Ribbonu v Main okně
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RunTestFormAttribute : Attribute
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="pageText"></param>
        /// <param name="pageOrder"></param>
        /// <param name="groupText"></param>
        /// <param name="groupOrder"></param>
        /// <param name="buttonText"></param>
        /// <param name="buttonOrder"></param>
        /// <param name="buttonImage"></param>
        /// <param name="buttonToolTip"></param>
        /// <param name="runAsFloating"></param>
        /// <param name="tabViewToolTip"></param>
        public RunTestFormAttribute(string pageText = null, int pageOrder = 0, string groupText = null, int groupOrder = 0,
            string buttonText = null, int buttonOrder = 0, string buttonImage = null,
            string buttonToolTip = null, bool runAsFloating = false, string tabViewToolTip = null)
        {
            this.PageText = pageText;
            this.PageOrder = pageOrder;
            this.GroupText = groupText;
            this.GroupOrder = groupOrder;
            this.ButtonText = buttonText;
            this.ButtonOrder = buttonOrder;
            this.ButtonImage = buttonImage;
            this.ButtonToolTip = buttonToolTip;
            this.RunAsFloating = runAsFloating;
            this.TabViewToolTip = tabViewToolTip;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{GroupText}: {ButtonText}";
        }
        /// <summary>
        /// Název stránky, kde se bude button nacházet.
        /// null = výchozí: "ZÁKLADNÍ"
        /// </summary>
        public string PageText { get; private set; }
        /// <summary>
        /// Pořadí stránky.
        /// Když více prvků bude mít shodný text <see cref="PageText"/> a rozdélné pořadí <see cref="PageOrder"/>, pak se do výsledku akceptuje nejvyšší <see cref="PageOrder"/> v rámci stránky.
        /// </summary>
        public int PageOrder { get; private set; }
        /// <summary>
        /// Název grupy, kde se bude button nacházet.
        /// null = výchozí = "FUNKCE"
        /// </summary>
        public string GroupText { get; private set; }
        /// <summary>
        /// Pořadí grupy ve stránce.
        /// Když více prvků bude mít shodný text <see cref="GroupText"/> a rozdélné pořadí <see cref="GroupOrder"/>, pak se do výsledku akceptuje nejvyšší <see cref="GroupOrder"/> v rámci grupy.
        /// </summary>
        public int GroupOrder { get; private set; }
        /// <summary>
        /// Název buttonu = zobrazený text.
        /// Prázdný text je přípustný, pokud je dána ikona <see cref="ButtonImage"/>.
        /// </summary>
        public string ButtonText { get; private set; }
        /// <summary>
        /// Pořadí tlačítka v rámci grupy.
        /// </summary>
        public int ButtonOrder { get; private set; }
        /// <summary>
        /// Ikona tlačítka.
        /// Prázdná ikona je přípustná, pokud je dán text <see cref="ButtonText"/>.
        /// </summary>
        public string ButtonImage { get; private set; }
        /// <summary>
        /// ToolTip k buttonu
        /// </summary>
        public string ButtonToolTip { get; private set; }
        /// <summary>
        /// Spouštět jako Modální okno
        /// </summary>
        public bool RunAsModal { get; private set; }
        /// <summary>
        /// Spouštět jako Floating okno
        /// </summary>
        public bool RunAsFloating { get; private set; }
        /// <summary>
        /// ToolTip k otevřenému oknu nad záložkou
        /// </summary>
        public string TabViewToolTip { get; private set; }
    }
    #endregion
    #region atribut RunTestClassAttribute, RunTestMethodAttribute
    /// <summary>
    /// Dekorátor třídy, která obsahuje testovací metody s atributem <see cref="RunTestMethodAttribute"/>.
    /// Bez zdejšího dekorátoru <see cref="RunTestClassAttribute"/> nebude třída rozpoznána a nebudou hledány konkrétní metody, tedy tento atribut je povinný i když neobsahuje další data.
    /// Repezentuje příznak na třídě.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RunTestClassAttribute : Attribute
    {
    }
    /// <summary>
    /// Dekorátor metody, která chce mít svoji ikonu v Ribbonu v Main okně.
    /// Musí jít o public static metodu bez parametrů.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RunTestMethodAttribute : Attribute
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="pageText"></param>
        /// <param name="pageOrder"></param>
        /// <param name="groupText"></param>
        /// <param name="groupOrder"></param>
        /// <param name="buttonText"></param>
        /// <param name="buttonOrder"></param>
        /// <param name="buttonImage"></param>
        /// <param name="buttonToolTip"></param>
        public RunTestMethodAttribute(string pageText = null, int pageOrder = 0, string groupText = null, int groupOrder = 0,
            string buttonText = null, int buttonOrder = 0, string buttonImage = null,
            string buttonToolTip = null)
        {
            this.PageText = pageText;
            this.PageOrder = pageOrder;
            this.GroupText = groupText;
            this.GroupOrder = groupOrder;
            this.ButtonText = buttonText;
            this.ButtonOrder = buttonOrder;
            this.ButtonImage = buttonImage;
            this.ButtonToolTip = buttonToolTip;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{GroupText}: {ButtonText}";
        }
        /// <summary>
        /// Název stránky, kde se bude button nacházet.
        /// null = výchozí: "ZÁKLADNÍ"
        /// </summary>
        public string PageText { get; private set; }
        /// <summary>
        /// Pořadí stránky.
        /// Když více prvků bude mít shodný text <see cref="PageText"/> a rozdélné pořadí <see cref="PageOrder"/>, pak se do výsledku akceptuje nejvyšší <see cref="PageOrder"/> v rámci stránky.
        /// </summary>
        public int PageOrder { get; private set; }
        /// <summary>
        /// Název grupy, kde se bude button nacházet.
        /// null = výchozí = "FUNKCE"
        /// </summary>
        public string GroupText { get; private set; }
        /// <summary>
        /// Pořadí grupy ve stránce.
        /// Když více prvků bude mít shodný text <see cref="GroupText"/> a rozdélné pořadí <see cref="GroupOrder"/>, pak se do výsledku akceptuje nejvyšší <see cref="GroupOrder"/> v rámci grupy.
        /// </summary>
        public int GroupOrder { get; private set; }
        /// <summary>
        /// Název buttonu = zobrazený text.
        /// Prázdný text je přípustný, pokud je dána ikona <see cref="ButtonImage"/>.
        /// </summary>
        public string ButtonText { get; private set; }
        /// <summary>
        /// Pořadí tlačítka v rámci grupy.
        /// </summary>
        public int ButtonOrder { get; private set; }
        /// <summary>
        /// Ikona tlačítka.
        /// Prázdná ikona je přípustná, pokud je dán text <see cref="ButtonText"/>.
        /// </summary>
        public string ButtonImage { get; private set; }
        /// <summary>
        /// ToolTip k buttonu
        /// </summary>
        public string ButtonToolTip { get; private set; }
    }
    #endregion
    /// <summary>
    /// Rozhraní pro prvek, který definuje button v Ribbonu (Stránka, Grupa, Button)
    /// </summary>
    public interface IRunItemAttribute
    {
        /// <summary>
        /// Název stránky, kde se bude button nacházet.
        /// null = výchozí: "ZÁKLADNÍ"
        /// </summary>
        string PageText { get; }
        /// <summary>
        /// Pořadí stránky.
        /// Když více prvků bude mít shodný text <see cref="PageText"/> a rozdélné pořadí <see cref="PageOrder"/>, pak se do výsledku akceptuje nejvyšší <see cref="PageOrder"/> v rámci stránky.
        /// </summary>
        int PageOrder { get; }
        /// <summary>
        /// Název grupy, kde se bude button nacházet.
        /// null = výchozí = "FUNKCE"
        /// </summary>
        string GroupText { get; }
        /// <summary>
        /// Pořadí grupy ve stránce.
        /// Když více prvků bude mít shodný text <see cref="GroupText"/> a rozdélné pořadí <see cref="GroupOrder"/>, pak se do výsledku akceptuje nejvyšší <see cref="GroupOrder"/> v rámci grupy.
        /// </summary>
        int GroupOrder { get; }
        /// <summary>
        /// Název buttonu = zobrazený text.
        /// Prázdný text je přípustný, pokud je dána ikona <see cref="ButtonImage"/>.
        /// </summary>
        string ButtonText { get; }
        /// <summary>
        /// Pořadí tlačítka v rámci grupy.
        /// </summary>
        int ButtonOrder { get; }
        /// <summary>
        /// Ikona tlačítka.
        /// Prázdná ikona je přípustná, pokud je dán text <see cref="ButtonText"/>.
        /// </summary>
        string ButtonImage { get; }
        /// <summary>
        /// ToolTip k buttonu
        /// </summary>
        string ButtonToolTip { get; }
    }
    #endregion
}
