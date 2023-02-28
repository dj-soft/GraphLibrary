using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Hlavní okno testovací aplikace - fyzická třída.
    /// Načítá dostupné formuláře, které chtějí být členem Ribbonu v hlavním okně (podle implementace <see cref="RunFormInfo"/>
    /// </summary>
    public class MainAppForm : Noris.Clients.Win.Components.AsolDX.DxMainAppForm
    {
        /// <summary>
        /// Příprava Ribbonu
        /// </summary>
        protected override void DxRibbonPrepare()
        {
            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage basicPage;
            DataRibbonGroup group;

            basicPage = new DataRibbonPage() { PageId = "DxBasicPage", PageText = "Domů", MergeOrder = 1, PageOrder = 1 };
            pages.Add(basicPage);
            group = DxRibbonControl.CreateSkinIGroup("DESIGN", addUhdSupport: true) as DataRibbonGroup;
            group.Items.Add(ImagePickerForm.CreateRibbonButton());
            basicPage.Groups.Add(group);

            var runFormInfos = RunFormInfo.GetForms();

            RunFormInfo.CreateRibbonPages(runFormInfos, pages, basicPage);

            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);

            this.DxRibbon.AllowCustomization = true;

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;
        }
        /// <summary>
        /// Kliknutí na Ribbon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DxRibbonControl_RibbonItemClick(object sender, TEventArgs<IRibbonItem> e)
        {
            switch (e.Item.ItemId)
            {
                case "": break;
            }
        }


        #region DockManager - služby
        protected override void InitializeDockPanelsContent()
        {
            var logControl = new TestDevExpress.Components.AppLogPanel();
            this.AddControlToDockPanels(logControl, "Log aplikace", DevExpress.XtraBars.Docking.DockingStyle.Right, DevExpress.XtraBars.Docking.DockVisibility.AutoHide);

            //var logControl2 = new TestDevExpress.Components.AppLogPanel();
            //this.AddControlToDockPanels(logControl2, "Doplňkový log", DevExpress.XtraBars.Docking.DockingStyle.Right, DevExpress.XtraBars.Docking.DockVisibility.AutoHide);

            //var logControl3 = new TestDevExpress.Components.AppLogPanel();
            //this.AddControlToDockPanels(logControl3, "přídavný log", DevExpress.XtraBars.Docking.DockingStyle.Right, DevExpress.XtraBars.Docking.DockVisibility.AutoHide);

            //var logControl4 = new TestDevExpress.Components.AppLogPanel();
            //this.AddControlToDockPanels(logControl4, "Jinopohledový log", DevExpress.XtraBars.Docking.DockingStyle.Left, DevExpress.XtraBars.Docking.DockVisibility.AutoHide);
        }
        #endregion

    }
    /// <summary>
    /// Definice spouštěcí ikony pro určitý formulář.
    /// <para/>
    /// Pokud určitý formulář chce být spouštěn z tlačítka v ribbonu okna <see cref="MainAppForm"/>, pak nechť si do své definice zařadí:
    /// <code>
    /// public static TestDevExpress.Forms.RunFormInfo RunFormInfo { get { return new RunFormInfo() { /* zde naplní svoje data pro button */ }; } }
    /// </code>
    /// Okno aplikace si při spuštění najde tyto formuláře, a sestaví je a do Ribbonu zobrazí jejich tlačítka.
    /// <para/>
    /// Proč takhle a ne přes interface? Interface předepisuje instanční property, což by znamenalo při hledání vhodných typů: 
    /// Vyhledat vhodné typy formulářů implementující nový interface (což jde snadno);
    /// ale potom vytvořit instanci každého takového formuláře (dost časové náročné, zvlášť u komplexních Formů) 
    /// jen proto, abych si přečetl jeho instanční property s deklarací buttonu do Ribbonu.<br/>
    /// Zdejší varianta
    /// </summary>
    public class RunFormInfo
    {
        #region Public data
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
        /// ToolTip k buttonu
        /// </summary>
        public string ButtonToolTip { get; set; }
        /// <summary>
        /// Ikona tlačítka.
        /// Prázdná ikona je přípustná, pokud je dán text <see cref="ButtonText"/>.
        /// </summary>
        public string ButtonImage { get; set; }
        /// <summary>
        /// Pořadí tlačítka v rámci grupy.
        /// </summary>
        public int ButtonOrder { get; set; }
        /// <summary>
        /// Spouštět jako Modální okno
        /// </summary>
        public bool RunAsModal { get; set; }
        /// <summary>
        /// Spouštět jako Floating okno
        /// </summary>
        public bool RunAsFloating { get; set; }
        #endregion
        #region Static vyhledání typů formulářů, které nabízejí tuto informaci
        /// <summary>
        /// Metoda vrací pole obsahující typy formulářů a jejich instance <see cref="RunFormInfo"/>
        /// </summary>
        /// <returns></returns>
        public static Tuple<Type, RunFormInfo>[] GetForms()
        {
            // var allAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            var myAssembly = typeof(RunFormInfo).Assembly;

            var runFormInfos = new List<Tuple<Type, RunFormInfo>>();
            DxComponent.GetTypes(myAssembly, t => ContainsRunFormInfo(t, runFormInfos));

            return runFormInfos.ToArray();
        }
        /// <summary>
        /// Metoda, která zjistí, zda daný <see cref="Type"/> je/není formulář spustitelný z hlavního okna aplikace.
        /// Side effectem metody je střádání spustitelných formulářů společně s informacemi <see cref="RunFormInfo"/>, které formuláře deklarují.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="runFormInfos"></param>
        /// <returns></returns>
        private static bool ContainsRunFormInfo(Type t, List<Tuple<Type, RunFormInfo>> runFormInfos)
        {
            if (t is null) return false;
            if (!t.IsSubclassOf(typeof(System.Windows.Forms.Form))) return false;

            var rfiProperty = t.GetProperty("RunFormInfo", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (rfiProperty is null) return false;

            var rfiValue = rfiProperty.GetValue(null);
            if (rfiValue is null) return false;
            if (rfiValue is not RunFormInfo rfiInstance) return false;

            if (rfiInstance.IsValid)
                runFormInfos.Add(new Tuple<Type, RunFormInfo>(t, rfiInstance));

            return false;
        }
        /// <summary>
        /// true pro použitelný prvek
        /// </summary>
        private bool IsValid { get { return (!String.IsNullOrEmpty(ButtonText) || !String.IsNullOrEmpty(ButtonImage)); } }
        #endregion
        #region Tvorba deklarace Ribbonu
        /// <summary>
        /// Vygeneruje korektní definici prvků do Ribbonu za dané formuláře
        /// </summary>
        /// <param name="runFormInfos"></param>
        /// <param name="pages"></param>
        /// <param name="basicPage"></param>
        public static void CreateRibbonPages(Tuple<Type, RunFormInfo>[] runFormInfos,  List<DataRibbonPage> pages, DataRibbonPage basicPage)
        {
            if (runFormInfos is null || runFormInfos.Length == 0) return;

            // var rfiPages = runFormInfos.GroupBy(i => i.Item2.PageText ?? "").ToArray();

            // Grupy za stránky podle PageText, tříděné podle PageOrder, v rámci jedné stránky podle nejvyšší hodnoty PageOrder:
            var rPages = runFormInfos.CreateSortedGroups(i => (i.Item2.PageText ?? ""), i => (i.Item2.PageOrder), (a, b) => (a > b ? a : b));
            foreach (var rPage in rPages)
            {   // V daném pořadí, které vychází z PageOrder:
                // Stránka Ribbonu: defaultní nebo nová explicitně pojmenovaná:
                DataRibbonPage dxPage = null;
                if (rPage.Item1 == "")
                    dxPage = basicPage;
                else
                {
                    dxPage = new DataRibbonPage() { PageText = rPage.Item1 };
                    pages.Add(dxPage);
                }

                // RibbonGrupy = z aktuální stránky rPage vezmu prvky (rPage.Item2), vytvořím skupiny podle GroupText a setřídím podle GroupOrder.Max() :
                var rGroups = rPage.Item2.CreateSortedGroups(i => (i.Item2.GroupText ?? ""), i => (i.Item2.GroupOrder), (a, b) => (a > b ? a : b));
                foreach (var rGroup in rGroups)
                {   // Jedna grupa za druhou, v pořadí GroupOrder:
                    string groupText = (!String.IsNullOrEmpty(rGroup.Item1) ? rGroup.Item1 : "FUNKCE");
                    DataRibbonGroup dxGroup = new DataRibbonGroup() { GroupText = groupText };
                    dxPage.Groups.Add(dxGroup);

                    // Jednotlivá tlačítka do grupy:
                    var rButtons = rGroup.Item2.ToList();
                    rButtons.Sort((a, b) => a.Item2.ButtonOrder.CompareTo(b.Item2.ButtonOrder));
                    foreach (var rButton in rButtons)
                    {
                        var dxItem = new DataRibbonItem() { Text = rButton.Item2.ButtonText, ToolTipText = rButton.Item2.ButtonToolTip, ImageName = rButton.Item2.ButtonImage, Tag = rButton };
                        dxItem.ClickAction = RunFormAction;
                        dxGroup.Items.Add(dxItem);
                    }
                }
            }
        }
        private static void RunFormAction(IMenuItem item)
        {
            if (item.Tag is not Tuple<Type, RunFormInfo> runFormInfo) return;

            runFormInfo.Item2.Run(runFormInfo.Item1);
        }
        private void Run(Type formType)
        {
            var form = System.Activator.CreateInstance(formType) as System.Windows.Forms.Form;
            DxMainAppForm.ShowChildForm(form, this.RunAsFloating);
            // form.ShowDialog();
        }
        #endregion
    }
}
