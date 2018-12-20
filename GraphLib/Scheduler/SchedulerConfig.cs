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
    public partial class SchedulerConfig : IXmlPersistNotify
    {
        #region Konstrukce, načtení, uložení
        /// <summary>
        /// Konstruktor, privátní, určený pro <see cref="Persist.Deserialize(string)"/>
        /// </summary>
        private SchedulerConfig()
        {   /* Tímto konstruktorem projde i deserializace objektu v rámci Persistor ! */
            this.MoveSnapMouse = new MoveSnapInfo();
            this.MoveSnapCtrl = new MoveSnapInfo();
            this.MoveSnapShift = new MoveSnapInfo();
            this.MoveSnapCtrlShift = new MoveSnapInfo();
        }
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
        /// Uloží data konfigurace
        /// </summary>
        public void Save()
        {
            this._SaveNow();
        }
        /// <summary>
        /// Uloží data konfigurace, ale ne hned, ale až za nějaký (daný) čas.
        /// </summary>
        /// <param name="wait">Čas čekání. Pokud bude zadán čas menší než 200ms, provede se ihned.</param>
        public void Save(TimeSpan wait)
        {
            if (wait.TotalMilliseconds < 200d)
            {   // Bez čekání:
                this._SaveNow();
            }
            else
            {   // S čekáním:
                lock (this._SaveLock)
                {   // Jen jeden thread smí pracovat se _SaveWaiting v jednom čase:
                    if (!this._SaveNowWaiting)
                    {   // Pokud nečekáme na spuštění ukládání již teď, odložené spuštění nastartujeme nyní:
                        Application.App.RunAfter(wait, this._SaveDeferred);
                        this._SaveNowWaiting = true;
                        this._SaveRequesting = true;
                    }
                }
            }
        }
        /// <summary>
        /// Uplynul čas a je třeba provést odložené ukládání dat:
        /// </summary>
        private void _SaveDeferred()
        {
            bool saveRequesting = true;
            lock (this._SaveLock)
            {
                this._SaveNowWaiting = false;
                saveRequesting = this._SaveRequesting;
            }
            if (saveRequesting)
                this._SaveNow();
        }
        /// <summary>
        /// Uloží aktuální stav objektu do svého souboru, pokud zrovna není nastaveno blokování ukládání <see cref="_SuppressSave"/>.
        /// </summary>
        private void _Save()
        {
            if (!this._SuppressSave)
                this._SaveNow();
        }
        /// <summary>
        /// Uloží aktuální stav objektu do svého souboru
        /// </summary>
        private void _SaveNow()
        {
            string configFile = this._ConfigFile;
            if (String.IsNullOrEmpty(configFile)) return;
            lock (this._SaveLock)
            {
                this._SaveRequesting = false;
                // Od teď už odložené čekání na uložení nemusí volat fyzické ukládání.
            }

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
        /// Objekt zámku pro synchorny okolo odloženého ukládání dat
        /// </summary>
        private object _SaveLock = new object();
        /// <summary>
        /// Jsme ve stavu, kdy běží čas čekání na odložené uložení konfigurace.
        /// Na true se nastaví v metodě, která požaduje odložené uložení <see cref="Save(TimeSpan)"/>, na false se dává v metodě která řeší odložené uložení <see cref="_SaveDeferred"/>.
        /// 
        /// Změny se provádí pod zámkem <see cref="_SaveLock"/>.
        /// </summary>
        private bool _SaveNowWaiting;
        /// <summary>
        /// Jsme ve stavu, kdy požadujeme provést uložení dat.
        /// Na true se nastaví v metodě, která zahajuje odložené uložení <see cref="Save(TimeSpan)"/>, na false se dává v metodě která data ukládá <see cref="_SaveNow"/>.
        /// 
        /// Změny se provádí pod zámkem <see cref="_SaveLock"/>.
        /// </summary>
        private bool _SaveRequesting;
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
            set { this._TimeAxisZoom = ((ToolbarSystemItem)(value & ToolbarSystemItem.TimeAxisZoomAll)); }
        }
        private ToolbarSystemItem _TimeAxisZoom = ToolbarSystemItem.TimeAxisZoomWorkWeek;
        /// <summary>
        /// Vztahy zobrazovat jako křivky (výchozí: zobrazovat jako rovné čáry)
        /// </summary>
        public bool GuiEditShowLinkAsSCurve
        {
            get { return this._GuiEditShowLinkAsSCurve; }
            set { this._GuiEditShowLinkAsSCurve = value; }
        }
        private bool _GuiEditShowLinkAsSCurve = true;
        /// <summary>
        /// Při najetí myší zobrazovat vztahy v rámci celého postupu, nejen nejbližší sousední položky
        /// </summary>
        public bool GuiEditShowLinkMouseWholeTask
        {
            get { return this._GuiEditShowLinkMouseWholeTask; }
            set { this._GuiEditShowLinkMouseWholeTask = value; }
        }
        private bool _GuiEditShowLinkMouseWholeTask = true;
        /// <summary>
        /// Při najetí myší zobrazovat vztahy v rámci celého postupu, nejen nejbližší sousední položky
        /// </summary>
        public bool GuiEditShowLinkSelectedWholeTask
        {
            get { return this._GuiEditShowLinkSelectedWholeTask; }
            set { this._GuiEditShowLinkSelectedWholeTask = value; }
        }
        private bool _GuiEditShowLinkSelectedWholeTask = true;

        /// <summary>
        /// Velikost prvku (šířka v pixelech), kdy považujeme vhodné přichytávat vždy na Begin.
        /// Menší prvky budou přichytávány na jejich Begin, i když je myš při přetahování na jejich fyzickém konci.
        /// Větší prvky umožní přichytávat i na jejich End, pokud je myš blíže ke konci prvku.
        /// </summary>
        public int MoveItemDetectSideMinSize
        {
            get { return this._MoveItemDetectSideMinSize; }
            set { this._MoveItemDetectSideMinSize = GetValue(value, 0, 50); }
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
            set { this._MoveItemDetectSideRatio = GetValue(value, 0f, 1f); }
        }
        private float _MoveItemDetectSideRatio = 0.60f;

        /// <summary>
        /// Vrátí instanci <see cref="MoveSnapInfo"/>, odpovídající stisknutým modifikačním klávesám
        /// </summary>
        /// <param name="modifierKeys">Modifier keys v době vzniku akce (Ctrl, Shift, Alt)</param>
        /// <returns></returns>
        public MoveSnapInfo GetMoveSnapForKeys(System.Windows.Forms.Keys modifierKeys)
        {
            if (modifierKeys == System.Windows.Forms.Keys.Control) return this.MoveSnapCtrl;
            if (modifierKeys == System.Windows.Forms.Keys.Shift) return this.MoveSnapShift;
            if (modifierKeys == (System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)) return this.MoveSnapCtrlShift;
            return this.MoveSnapMouse;
        }
        /// <summary>
        /// Přichytávání k objektům bez stisknutých kláves
        /// </summary>
        public MoveSnapInfo MoveSnapMouse { get { return this._MoveSnapMouse; } set { if (value != null) { this._MoveSnapMouse = value; this._MoveSnapMouse.CheckValid(MoveSnapKeyType.None); } } } private MoveSnapInfo _MoveSnapMouse;
        /// <summary>
        /// Přichytávání k objektům při stisknuté klávese Ctrl
        /// </summary>
        public MoveSnapInfo MoveSnapCtrl { get { return this._MoveSnapCtrl; } set { if (value != null) { this._MoveSnapCtrl = value; this._MoveSnapCtrl.CheckValid(MoveSnapKeyType.Ctrl); } } } private MoveSnapInfo _MoveSnapCtrl;
        /// <summary>
        /// Přichytávání k objektům při stisknuté klávese Shift
        /// </summary>
        public MoveSnapInfo MoveSnapShift { get { return this._MoveSnapShift; } set { if (value != null) { this._MoveSnapShift = value; this._MoveSnapShift.CheckValid(MoveSnapKeyType.Shift); } } } private MoveSnapInfo _MoveSnapShift;
        /// <summary>
        /// Přichytávání k objektům při stisknuté kombinaci Ctrl + Shift
        /// </summary>
        public MoveSnapInfo MoveSnapCtrlShift { get { return this._MoveSnapCtrlShift; } set { if (value != null) { this._MoveSnapCtrlShift = value; this._MoveSnapCtrlShift.CheckValid(MoveSnapKeyType.CtrlShift); } } } private MoveSnapInfo _MoveSnapCtrlShift;
        #endregion
        #region UserConfig
        /// <summary>
        /// Pole libovolných objektů, které jsou ukládány spolu s konfigurací.
        /// Aplikační vrstva si v tomto poli může najít co potřebuje, anebo přidat co potřebuje.
        /// Konfigurační objekt nijak neřeší, co je zde skladováno.
        /// Tato property je autoinicializační (nikdy není čtena jako null).
        /// </summary>
        public List<object> UserConfig
        {
            get { if (this._UserConfig == null) this._UserConfig = new List<object>(); return this._UserConfig; }
            set { this._UserConfig = value; }
        }
        private List<object> _UserConfig;
        /// <summary>
        /// Metoda projde data v <see cref="UserConfig"/>, vybere z položek ty, které jsou dané třídy,
        /// přefiltruje je dodaným filtrem (pokud je dodán, jinak bere vše) a vrátí výsledný seznam.
        /// Aplikační kód pak může dát FirstOrDefault() a získat jeden vyhovující prvek...
        /// Metoda slouží k nalezení určité položky User konfigurace.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IEnumerable<T> UserConfigSearch<T>(Func<T, bool> filter = null)
        {
            bool hasFilter = (filter != null);
            return this.UserConfig.OfType<T>().Where(i => (!hasFilter || filter(i)));
        }
        #endregion
        #region Ověření hodnoty
        /// <summary>
        /// Vrátí hodnotu zarovnanou do daných mezí
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        protected static int GetValue(int value, int minValue, int maxValue)
        {
            return (value < minValue ? minValue : (value > maxValue ? maxValue : value));
        }
        /// <summary>
        /// Vrátí hodnotu zarovnanou do daných mezí
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        protected static float GetValue(float value, float minValue, float maxValue)
        {
            return (value < minValue ? minValue : (value > maxValue ? maxValue : value));
        }
        #endregion
        #region EditingScope : Editační scope - potlačí on-line ukládání po dobu hromadného zadávání dat
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
                    case XmlPersistState.LoadBegin:

                    case XmlPersistState.LoadDone:
                        this._AfterDeserialize();
                        break;
                }
            }
        }
        private XmlPersistState _XmlPersistState;
        private void _AfterDeserialize()
        { }
        #endregion
        #region SubClasses: MoveSnapInfo
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
            /// Typ modifikačních kláves
            /// </summary>
            public MoveSnapKeyType? KeyType { get; set; }

            /// <summary>
            /// Majitel objektu
            /// </summary>
            [PersistingEnabled(false)]
            internal SchedulerConfig Owner { get; set; }
            /// <summary>
            /// Metoda zajistí platnost dat v this objektu pro daný typ modifikačních kláves
            /// </summary>
            /// <param name="keyType">Typ modifikačních kláves</param>
            internal void CheckValid(MoveSnapKeyType keyType)
            {
                if (this.KeyType.HasValue && this.KeyType.Value == keyType) return;   // Data jsou platná

                // Nastavíme defaultní hodnoty:
                switch (keyType)
                {
                    case MoveSnapKeyType.None:             // Bez kláves = mírné přichycení
                    case MoveSnapKeyType.CtrlShift:        // CTRL + SHIFT: standardně nemá zvykové právo, nastavíme jako None:
                        this.SequenceActive = true;
                        this.SequenceDistance = 5;
                        this.InnerItemActive = true;
                        this.InnerItemDistance = 5;
                        this.OriginalTimeNearActive = true;
                        this.OriginalTimeNearDistance = 5;
                        this.OriginalTimeLongActive = true;
                        this.OriginalTimeLongDistance = 10;
                        this.GridTickActive = false;
                        this.GridTickDistance = 3;
                        break;
                    case MoveSnapKeyType.Ctrl:             // CTRL = přichycení na větší vzdálenosti
                        this.SequenceActive = true;
                        this.SequenceDistance = 20;
                        this.InnerItemActive = true;
                        this.InnerItemDistance = 20;
                        this.OriginalTimeNearActive = true;
                        this.OriginalTimeNearDistance = 20;
                        this.OriginalTimeLongActive = true;
                        this.OriginalTimeLongDistance = 30;
                        this.GridTickActive = true;
                        this.GridTickDistance = 7;
                        break;
                    case MoveSnapKeyType.Shift:            // SHIFT = bez přichycení
                        this.SequenceActive = false;
                        this.SequenceDistance = 10;
                        this.InnerItemActive = false;
                        this.InnerItemDistance = 10;
                        this.OriginalTimeNearActive = false;
                        this.OriginalTimeNearDistance = 10;
                        this.OriginalTimeLongActive = false;
                        this.OriginalTimeLongDistance = 10;
                        this.GridTickActive = false;
                        this.GridTickDistance = 10;
                        break;
                }

                this.KeyType = keyType;
            }
        }
        /// <summary>
        /// Typ modifikačních kláves pro MoveSnap
        /// </summary>
        public enum MoveSnapKeyType
        {
            /// <summary>
            /// Bez kláves
            /// </summary>
            None,
            /// <summary>
            /// S klávesou Ctrl
            /// </summary>
            Ctrl,
            /// <summary>
            /// S klávesou Shift
            /// </summary>
            Shift,
            /// <summary>
            /// S klávesou Ctrl + Shift
            /// </summary>
            CtrlShift
        }
        #endregion
    }
}
