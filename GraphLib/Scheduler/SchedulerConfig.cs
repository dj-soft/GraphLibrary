using Noris.LCS.Base.WorkScheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    /// <summary>
    /// Třída pro uchování různých nastavení primárně na úrovni lokálního počítače.
    /// Jde víceméně o vlastnosti uživatelské, ne datové, jako je Zoom celého GUI, Zoom časové osy, nastavení přichytávání prvků při jejich pohybu, atd...
    /// </summary>
    public class SchedulerConfig : IXmlPersistNotify
    {
        #region Konstrukce, načtení, uložení
        /// <summary>
        /// Konstruktor, privátní, určený pro <see cref="Persist.Deserialize(string)"/>
        /// </summary>
        private SchedulerConfig() {   /* Tímto konstruktorem projde i deserializace objektu v rámci Persistor ! */   }
        /// <summary>
        /// Konstruktor pro standardní použití, provede i načtení dat.
        /// Lze zadat soubor = null, pak se použije defaultní. Ale musí se volat tento konstruktor.
        /// </summary>
        /// <param name="configFile"></param>
        public SchedulerConfig(string configFile)
            : this()
        {
            this._LoadFrom(configFile);
        }
        /// <summary>
        /// Načte data z dodaného souboru
        /// </summary>
        /// <param name="configFile"></param>
        private void _LoadFrom(string configFile)
        {
            if (String.IsNullOrEmpty(configFile))
                configFile = _CONFIG_FILE_NAME;
            if (!configFile.Contains("\\"))
                configFile = System.IO.Path.Combine(Application.App.AppLocalDataPath, configFile);

            if (System.IO.File.Exists(configFile))
                Persist.LoadTo(configFile, this);

            this._ConfigFile = configFile;
            this._AfterDeserialize();
        }
        /// <summary>
        /// Uloží aktuální stav objektu do svého souboru
        /// </summary>
        private void _Save()
        {
            if (this._SuppressSave) return;

            string configFile = this._ConfigFile;
            if (String.IsNullOrEmpty(configFile)) return;
            string data = Persist.Serialize(this);
            Application.App.TryRunBgr(() => System.IO.File.WriteAllText(configFile, data, Encoding.UTF8));
        }
        /// <summary>
        /// Fullname konfiguračního souboru
        /// </summary>
        private string _ConfigFile;
        /// <summary>
        /// true = ukládání do souboru je potlačeno
        /// </summary>
        private bool _SuppressSave;
        /// <summary>
        /// Default holé jméno konfiguračního souboru.
        /// Ukládá se do adresáře <see cref="Application.App.AppLocalDataPath"/>.
        /// </summary>
        private const string _CONFIG_FILE_NAME = "WorkScheduler.setting";
        #endregion
        #region Public property
        /// <summary>
        /// Zoom na časové ose
        /// </summary>
        public ToolbarSystemItem TimeAxisZoom
        {
            get { return (ToolbarSystemItem)(this._TimeAxisZoom & ToolbarSystemItem.TimeAxisZoomAll); }
            set { this._SetValue((ToolbarSystemItem)(value & ToolbarSystemItem.TimeAxisZoomAll), ref this._TimeAxisZoom); }
        }
        private ToolbarSystemItem _TimeAxisZoom = ToolbarSystemItem.TimeAxisZoomWorkWeek;
        /// <summary>
        /// Při pohybu prvků v grafu jej přichytávat k blízkým prvkům
        /// </summary>
        public bool MoveItemSnapToNearItems
        {
            get { return this._MoveItemSnapToNearItems; }
            set { this._SetValue(value, ref this._MoveItemSnapToNearItems); }
        }
        private bool _MoveItemSnapToNearItems = true;
        /// <summary>
        /// Vzdálenost v pixelech, na kterou se budou vyhledávat sousední prvky pro přichytávání k blízkým prvkům
        /// </summary>
        public int MoveItemSnapDistanceToNearItems
        {
            get { return this._MoveItemSnapDistanceToNearItems; }
            set { this._SetValue(value, 0, 50, ref this._MoveItemSnapDistanceToNearItems); }
        }
        private int _MoveItemSnapDistanceToNearItems = 10;
        /// <summary>
        /// Při pohybu prvků v grafu jej přichytávat k původnímu času prvku
        /// </summary>
        public bool MoveItemSnapToOriginalTime
        {
            get { return this._MoveItemSnapToOriginalTime; }
            set { this._SetValue(value, ref this._MoveItemSnapToOriginalTime); }
        }
        private bool _MoveItemSnapToOriginalTime = true;
        /// <summary>
        /// Při najetí myší zobrazovat vztahy v rámci celého postupu, nejen nejbližší sousední položky
        /// </summary>
        public bool GuiEditShowLinkWholeTask
        {
            get { return this._GuiEditShowLinkWholeTask; }
            set { this._SetValue(value, ref this._GuiEditShowLinkWholeTask); }
        }
        private bool _GuiEditShowLinkWholeTask = true;
        /// <summary>
        /// Vztahy zobrazovat jako křivky (výchozí: zobrazovat jako rovné čáry)
        /// </summary>
        public bool GuiEditShowLinkAsSCurve
        {
            get { return this._GuiEditShowLinkAsSCurve; }
            set { this._SetValue(value, ref this._GuiEditShowLinkAsSCurve); }
        }
        private bool _GuiEditShowLinkAsSCurve = true;
        /// <summary>
        /// Počet pixelů vzdálenosti od původního času, kdy se k tomuto původnímu času bude prvek přichytávat při pohybu na TOM SAMÉM grafu
        /// </summary>
        public int MoveItemSnapDistanceToOriginalTimeOnSameGraph
        {
            get { return this._MoveItemSnapDistanceToOriginalTimeOnSameGraph; }
            set { this._SetValue(value, 0, 50, ref this._MoveItemSnapDistanceToOriginalTimeOnSameGraph); }
        }
        private int _MoveItemSnapDistanceToOriginalTimeOnSameGraph = 5;
        /// <summary>
        /// Počet pixelů vzdálenosti od původního času, kdy se k tomuto původnímu času bude prvek přichytávat při pohybu na JINÉM grafu
        /// </summary>
        public int MoveItemSnapDistanceToOriginalTimeOnOtherGraph
        {
            get { return this._MoveItemSnapDistanceToOriginalTimeOnOtherGraph; }
            set { this._SetValue(value, 0, 50, ref this._MoveItemSnapDistanceToOriginalTimeOnOtherGraph); }
        }
        private int _MoveItemSnapDistanceToOriginalTimeOnOtherGraph = 15;
        /// <summary>
        /// Při pohybu prvků v grafu jej přichytávat k nejbližšímu zaokrouhlenému času
        /// </summary>
        public bool MoveItemSnapToNearRoundTime
        {
            get { return this._MoveItemSnapToNearRoundTime; }
            set { this._SetValue(value, ref this._MoveItemSnapToNearRoundTime); }
        }
        private bool _MoveItemSnapToNearRoundTime = true;

        /// <summary>
        /// Velikost prvku (šířka v pixelech), kdy považujeme vhodné přichytávat vždy na Begin.
        /// Menší prvky budou přichytávány na jejich Begin, i když je myš při přetahování na jejich fyzickém konci.
        /// Větší prvky umožní přichytávat i na jejich End, pokud je myš blíže ke konci prvku.
        /// </summary>
        public int MoveItemDetectSideMinSize
        {
            get { return this._MoveItemDetectSideMinSize; }
            set { this._SetValue(value, 0, 50, ref this._MoveItemDetectSideMinSize); }
        }
        private int _MoveItemDetectSideMinSize = 15;
        /// <summary>
        /// Poměrná část prvku, která se považuje za Begin, při určení strany přichytávání podle pozice myši na prvku.
        /// Pokud bude myš např. přesně v polovině prvku, bude se přichytávat čas Begin k sousedním prvkům k jejich času End.
        /// Na pozici <see cref="MoveItemDetectSideRatio"/> prvku se pozice láme mezi Begin a End.
        /// </summary>
        public float MoveItemDetectSideRatio
        {
            get { return this._MoveItemDetectSideRatio; }
            set { this._SetValue(value, 0f, 1f, ref this._MoveItemDetectSideRatio); }
        }
        private float _MoveItemDetectSideRatio = 0.60f;

        /// <summary>
        /// Přichytávání k objektům bez stisknutých kláves
        /// </summary>
        public MoveSnapInfo MoveSnapMouse { get; set; }
        /// <summary>
        /// Přichytávání k objektům při stisknuté klávese Ctrl
        /// </summary>
        public MoveSnapInfo MoveSnapCtrl { get; set; }
        /// <summary>
        /// Přichytávání k objektům při stisknuté klávese Shift
        /// </summary>
        public MoveSnapInfo MoveSnapShift { get; set; }
        /// <summary>
        /// Přichytávání k objektům při stisknuté kombinaci Ctrl + Shift
        /// </summary>
        public MoveSnapInfo MoveSnapCtrlShift { get; set; }

        #endregion
        #region IXmlPersistNotify : Podpora pro serializaci
        /// <summary>
        /// Aktuální stav procesu XML persistence.
        /// Umožňuje persistovanému objektu reagovat na ukládání nebo na načítání dat.
        /// Do této property vkládá XmlPersistor hodnotu odpovídající aktuální situaci.
        /// Datová instance může v set accessoru zareagovat a například připravit data pro Save, 
        /// anebo dokončit proces Load (navázat si další data nebo provést přepočty a další reakce).
        /// V procesu serializace (ukládání dat z objektu to XML) bude do property <see cref="IXmlPersistNotify.XmlPersistState"/> vložena hodnota <see cref="XmlPersistState.SaveBegin"/> a po dokončení ukládán ípak hodnota <see cref="XmlPersistState.SaveDone"/> a <see cref="XmlPersistState.None"/>.
        /// Obdobně při načítání dat z XML do objektu bude do property <see cref="IXmlPersistNotify.XmlPersistState"/> vložena hodnota <see cref="XmlPersistState.LoadBegin"/> a po dokončení načítání pak hodnota <see cref="XmlPersistState.LoadDone"/> a <see cref="XmlPersistState.None"/>.
        /// </summary>
        [PersistingEnabled(false)]
        XmlPersistState IXmlPersistNotify.XmlPersistState
        {
            get { return this._XmlPersistState; }
            set
            {
                this._XmlPersistState = value;
                switch (value)
                {
                    case XmlPersistState.LoadDone:
                        this._AfterDeserialize();
                        break;
                }
            }
        }
        private XmlPersistState _XmlPersistState;
        private void _AfterDeserialize()
        {
            if (this.MoveSnapMouse == null) this.MoveSnapMouse = new MoveSnapInfo();
            this.MoveSnapMouse.Owner = this;
            if (this.MoveSnapCtrl == null) this.MoveSnapCtrl = new MoveSnapInfo();
            if (this.MoveSnapShift == null) this.MoveSnapShift = new MoveSnapInfo();
            if (this.MoveSnapCtrlShift == null) this.MoveSnapCtrlShift = new MoveSnapInfo();


        }

        #endregion
        #region SubClasses
        /// <summary>
        /// Definice vlastností pro přichytávání objektů
        /// </summary>
        public class MoveSnapInfo
        {
            /// <summary>
            /// Je aktivní algoritmus: Přichytit k navazujícímu objektu (End = Begin)?
            /// </summary>
            public bool SequenceActive { get; set; }
            /// <summary>
            /// Vzdálenost pro algoritmus: Přichytit k navazujícímu objektu (End = Begin)
            /// </summary>
            public int SequenceDistance { get; set; }
            /// <summary>
            /// Je aktivní algoritmus: Přichytit k podkladovému objektu (Begin = Begin)?
            /// </summary>
            public bool InnerItemActive { get; set; }
            /// <summary>
            /// Vzdálenost pro algoritmus: Přichytit k podkladovému objektu (Begin = Begin)
            /// </summary>
            public int InnerItemDistance { get; set; }
            /// <summary>
            /// Je aktivní algoritmus: Přichytit k originálnímu času ve výchozím grafu?
            /// </summary>
            public bool OriginalTimeNearActive { get; set; }
            /// <summary>
            /// Vzdálenost pro algoritmus: Přichytit k originálnímu času ve výchozím grafu
            /// </summary>
            public int OriginalTimeNearDistance { get; set; }
            /// <summary>
            /// Je aktivní algoritmus: Přichytit k originálnímu času v jiném než výchozím grafu?
            /// </summary>
            public bool OriginalTimeLongActive { get; set; }
            /// <summary>
            /// Vzdálenost pro algoritmus: Přichytit k originálnímu času v jiném než výchozím grafu
            /// </summary>
            public int OriginalTimeLongDistance { get; set; }
            /// <summary>
            /// Je aktivní algoritmus: Přichytit k rastru osy?
            /// </summary>
            public bool GridTickActive { get; set; }
            /// <summary>
            /// Vzdálenost pro algoritmus: Přichytit k rastru osy
            /// </summary>
            public int GridTickDistance { get; set; }

            /// <summary>
            /// Majitel objektu
            /// </summary>
            [PersistingEnabled(false)]
            internal SchedulerConfig Owner { get; set; }
        }
        #endregion
        #region Podpora pro editaci
        /// <summary>
        /// Metoda vrací pole prvků, které slouží k editaci konfigurace.
        /// </summary>
        /// <returns></returns>
        public SchedulerEditorItem[] CreateEditorItems()
        {
            List<SchedulerEditorItem> itemList = new List<SchedulerEditorItem>();

            // Vkládám jednotlivé prvky: jejich titulek = text v Tree, jejich ToolTip, a jejich instanci editoru:
            itemList.Add(new SchedulerEditorItem(EditTitle_Snap + EditTitle_Separator + EditTitle_SnapMouse, "", new ConfigSnapSetPanel() { Data = this.MoveSnapMouse, Caption = "Přichycení při přetahování prvku, bez klávesnice" }));
            itemList.Add(new SchedulerEditorItem(EditTitle_Snap + EditTitle_Separator + EditTitle_SnapCtrl, "", new ConfigSnapSetPanel() { Data = this.MoveSnapCtrl, Caption = "Přichycení při přetahování prvku, s klávesou CTRL" }));
            itemList.Add(new SchedulerEditorItem(EditTitle_Snap + EditTitle_Separator + EditTitle_SnapShift, "", new ConfigSnapSetPanel() { Data = this.MoveSnapShift, Caption = "Přichycení při přetahování prvku, s klávesou SHIFT" }));
            itemList.Add(new SchedulerEditorItem(EditTitle_Snap + EditTitle_Separator + EditTitle_SnapCtrlShift, "", new ConfigSnapSetPanel() { Data = this.MoveSnapCtrlShift, Caption = "Přichycení při přetahování prvku, s klávesou CTRL + SHIFT" }));

            return itemList.ToArray();
        }
        protected static string EditTitle_Snap { get { return "Přichycení při přetahování"; } }
        protected static string EditTitle_SnapMouse { get { return "Přetahování myší"; } }
        protected static string EditTitle_SnapCtrl { get { return "Se stisknutým CTRL"; } }
        protected static string EditTitle_SnapShift { get { return "Se stisknutým SHIFT"; } }
        protected static string EditTitle_SnapCtrlShift { get { return "Se stisknutým CTRL+SHIFT"; } }
        public static string EditTitle_Separator { get { return "\\"; } }
        #endregion
        #region EditingScope : Editační scope
        /// <summary>
        /// Metoda vrátí new editační scope.
        /// Po dobu jeho života nebude Config ukládán, uloží se při Dispose tohoto scope.
        /// </summary>
        /// <returns></returns>
        public IDisposable CreateEditingScope()
        {
            return new EditingScope(this);
        }
        /// <summary>
        /// EditingScope : Editační scope
        /// </summary>
        protected class EditingScope : IDisposable
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="config"></param>
            public EditingScope(SchedulerConfig config)
            {
                this._Config = config;
                this._SuppressSave = config._SuppressSave;
                this._Config._SuppressSave = true;
            }
            SchedulerConfig _Config;
            bool _SuppressSave;
            void IDisposable.Dispose()
            {
                this._Config._SuppressSave = this._SuppressSave;
                this._Config._Save();
            }
        }
        #endregion
        #region Ukládání hodnoty, detekce změny, automatické uložení konfigurace
        private void _SetValue(bool value, ref bool storage)
        {
            if (value == storage) return;
            storage = value;
            this._Save();
        }
        private void _SetValue(int value, int minValue, int maxValue, ref int storage)
        {
            value = (value < minValue ? minValue : (value > maxValue ? maxValue : value));
            this._SetValue(value, ref storage);
        }
        private void _SetValue(int value, ref int storage)
        {
            if (value == storage) return;
            storage = value;
            this._Save();
        }
        private void _SetValue(float value, float minValue, float maxValue, ref float storage)
        {
            value = (value < minValue ? minValue : (value > maxValue ? maxValue : value));
            this._SetValue(value, ref storage);
        }
        private void _SetValue(float value, ref float storage)
        {
            if (value == storage) return;
            storage = value;
            this._Save();
        }
        private void _SetValue(string value, ref string storage)
        {
            if (value == storage) return;
            storage = value;
            this._Save();
        }
        private void _SetValue(ToolbarSystemItem value, ref ToolbarSystemItem storage)
        {
            if (value == storage) return;
            storage = value;
            this._Save();
        }
        #endregion
    }
    #region class SchedulerEditorItem : jedna položka v editoru konfigurace
    /// <summary>
    /// SchedulerEditorItem : jedna položka v editoru konfigurace
    /// </summary>
    public class SchedulerEditorItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="nodeText"></param>
        /// <param name="nodeToolTip"></param>
        /// <param name="visualControl"></param>
        public SchedulerEditorItem(string nodeText, string nodeToolTip, ISchedulerEditorControlItem visualControl)
        {
            this.NodeText = nodeText;
            this.NodeToolTip = nodeToolTip;
            this.VisualControl = visualControl;
        }
        /// <summary>
        /// Text položky konfigurace.
        /// Pokud bude obsahovat zpětné lomítko, jde o oddělovač v stromové hierarchii.
        /// </summary>
        public string NodeText { get; set; }
        /// <summary>
        /// ToolTip k položce stromu konfigurace
        /// </summary>
        public string NodeToolTip { get; set; }
        /// <summary>
        /// Vizuální control, který zobrazuje a edituje konfigurační data
        /// </summary>
        public ISchedulerEditorControlItem VisualControl { get; set; }
    }
    /// <summary>
    /// Předpis pro objekty, které mohou hrát roli editoru jedné položky konfigurace.
    /// Objekt musí umět načíst hodnoty z configu do vizuálních prvků <see cref="Read()"/>;
    /// uložit hodnoty z vizuálních prvků do configu <see cref="Save()"/>;
    /// a poskytnout vizuální objekt pro zobrazování <see cref="Panel"/>.
    /// </summary>
    public interface ISchedulerEditorControlItem
    {
        /// <summary>
        /// Objekt načte hodnoty z configu do vizuálních prvků
        /// </summary>
        void Read();
        /// <summary>
        /// Objekt uloží hodnoty z vizuálních prvků do configu
        /// </summary>
        void Save();
        /// <summary>
        /// Vizuální control zobrazovaný pro tuto položku konfigurace
        /// </summary>
        System.Windows.Forms.Panel Panel { get; }
    }
    #endregion
}
