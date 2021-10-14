// Supervisor: David Janáček
// Part of Helios Green, proprietary software, (c) Asseco solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noris.Clients.Win.Components
{
    #region class WatchTimer : budík ("Vzbuď mě za ... milisekund; zavolej mě každých ... milisekund; ...")   [DAJ 0064307 2020-01-17]
    /// <summary>
    /// WatchTimer : budík ("Vzbuď mě za ... milisekund; zavolej mě každých ... milisekund; ...")
    /// </summary>
    public class WatchTimer
    {
        #region Public static rozhraní
        /// <summary>
        /// Zavolej mě (danou akci) za daný počet milisekund
        /// </summary>
        /// <param name="action"></param>
        /// <param name="miliseconds"></param>
        /// <param name="synchronizeUI"></param>
        /// <param name="id">ID existujícího budíku, kterému chci změnit vlastnosti a reaktivovat jej. Pokud je null, nebo takový budík již neexistuje, bude standardně vytvořen a uložen nový budík.</param>
        /// <returns></returns>
        public static Guid CallMeAfter(Action action, int miliseconds, bool synchronizeUI = true, Guid? id = null)
        {
            WatchItem watchItem = new WatchItem(action, null, null, miliseconds, false, synchronizeUI);
            Timer._AddItem(watchItem, id);
            return watchItem.Guid;
        }
        /// <summary>
        /// Zavolej mě (danou akci včetně parametru) za daný počet milisekund
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actionParam"></param>
        /// <param name="param"></param>
        /// <param name="miliseconds"></param>
        /// <param name="synchronizeUI"></param>
        /// <param name="id">ID existujícího budíku, kterému chci změnit vlastnosti a reaktivovat jej. Pokud je null, nebo takový budík již neexistuje, bude standardně vytvořen a uložen nový budík.</param>
        /// <returns></returns>
        public static Guid CallMeAfter(Action<object> action, Action<object> actionParam, object param, int miliseconds, bool synchronizeUI = true, Guid? id = null)
        {
            WatchItem watchItem = new WatchItem(null, actionParam, param, miliseconds, false, synchronizeUI);
            Timer._AddItem(watchItem, id);
            return watchItem.Guid;
        }
        /// <summary>
        /// Volej mě opakovaně (danou akci) vždy za daný počet milisekund
        /// </summary>
        /// <param name="action"></param>
        /// <param name="miliseconds"></param>
        /// <param name="synchronizeUI"></param>
        /// <param name="id">ID existujícího budíku, kterému chci změnit vlastnosti a reaktivovat jej. Pokud je null, nebo takový budík již neexistuje, bude standardně vytvořen a uložen nový budík.</param>
        /// <returns></returns>
        public static Guid CallMeEvery(Action action, int miliseconds, bool synchronizeUI = true, Guid? id = null)
        {
            WatchItem watchItem = new WatchItem(action, null, null, miliseconds, true, synchronizeUI);
            Timer._AddItem(watchItem, id);
            return watchItem.Guid;
        }
        /// <summary>
        /// Volej mě opakovaně (danou akci včetně parametru) vždy za daný počet milisekund
        /// </summary>
        /// <param name="actionParam"></param>
        /// <param name="param"></param>
        /// <param name="miliseconds"></param>
        /// <param name="synchronizeUI"></param>
        /// <param name="id">ID existujícího budíku, kterému chci změnit vlastnosti a reaktivovat jej. Pokud je null, nebo takový budík již neexistuje, bude standardně vytvořen a uložen nový budík.</param>
        /// <returns></returns>
        public static Guid CallMeEvery(Action<object> actionParam, object param, int miliseconds, bool synchronizeUI = true, Guid? id = null)
        {
            WatchItem watchItem = new WatchItem(null, actionParam, param, miliseconds, true, synchronizeUI);
            Timer._AddItem(watchItem, id);
            return watchItem.Guid;
        }
        /// <summary>
        /// Zavolej mě (danou akci) jednou v daném čase
        /// </summary>
        /// <param name="action"></param>
        /// <param name="targetTime"></param>
        /// <param name="synchronizeUI"></param>
        /// <param name="id">ID existujícího budíku, kterému chci změnit vlastnosti a reaktivovat jej. Pokud je null, nebo takový budík již neexistuje, bude standardně vytvořen a uložen nový budík.</param>
        /// <returns></returns>
        public static Guid CallMeInTime(Action action, DateTime targetTime, bool synchronizeUI = true, Guid? id = null)
        {
            WatchItem watchItem = new WatchItem(action, null, null, targetTime, synchronizeUI);
            Timer._AddItem(watchItem, id);
            return watchItem.Guid;
        }
        /// <summary>
        /// Zavolej mě (danou akci včetně parametru) jednou v daném čase
        /// </summary>
        /// <param name="actionParam"></param>
        /// <param name="param"></param>
        /// <param name="targetTime"></param>
        /// <param name="synchronizeUI"></param>
        /// <param name="id">ID existujícího budíku, kterému chci změnit vlastnosti a reaktivovat jej. Pokud je null, nebo takový budík již neexistuje, bude standardně vytvořen a uložen nový budík.</param>
        /// <returns></returns>
        public static Guid CallMeInTime(Action<object> actionParam, object param, DateTime targetTime, bool synchronizeUI = true, Guid? id = null)
        {
            WatchItem watchItem = new WatchItem(null, actionParam, param, targetTime, synchronizeUI);
            Timer._AddItem(watchItem, id);
            return watchItem.Guid;
        }
        /// <summary>
        /// Odebere časovač dle jeho <see cref="Guid"/>.
        /// ID časovače je vráceno ze všech metod, které časovač přidávají.
        /// Pokud byl přidán časovač typu "jedno volání" (např. metodou <see cref="CallMeAfter(Action, int, bool, Guid?)"/>, 
        /// pak není třeba jej odebírat po jeho proběhnutí - časovač je odebrán automaticky.
        /// Takový časovač je ale možno odebrat před jeho aktivací - pak k aktivaci nedojde.
        /// Lze odebrat časovač cyklický, typu "volej mě každých 100 milisekund".
        /// </summary>
        /// <param name="id"></param>
        public static void Remove(Guid id)
        {
            Timer._RemoveItem(id);
        }
        /// <summary>
        /// Přesnost časového rozlišení v milisekundách.
        /// Vysoká přesnost = nízké milisekundy vedou k vyšší zátěži systému.
        /// Výchozí přesnost = 20ms.
        /// Přípustné hodnoty: 1 až 1000.
        /// </summary>
        public static int ResolutionMiliseconds { get { return Timer._ResolutionMiliseconds; } set { Timer._ResolutionMiliseconds = value; } }
        #endregion
        #region Singleton a instanční data
        /// <summary>
        /// Instance (singleton)
        /// </summary>
        private static WatchTimer Timer
        {
            get
            {
                if (__Timer is null)
                {
                    lock (__Lock)
                    {
                        if (__Timer is null)
                            __Timer = new WatchTimer();
                    }
                }
                return __Timer;
            }
        }
        private static WatchTimer __Timer;
        private static readonly object __Lock = new object();
        /// <summary>
        /// Jediný a privátní konstruktor
        /// </summary>
        private WatchTimer()
        {
            this._Items = new Dictionary<Guid, WatchItem>();
            this._TimeBase = new System.Diagnostics.Stopwatch();
            this._ResolutionMiliseconds = 20;
            this._TimeBase.Start();
            this._ThreadInit();
        }
        /// <summary>
        /// Položky časovače
        /// </summary>
        private Dictionary<Guid, WatchItem> _Items;
        /// <summary>
        /// Stáleběžící hodiny, které nejsou ovlivněny tím, když si uživatel na počítači provede změnu systémového času.
        /// Nechci události timeru typu "Po uplynutí času T" opírat o hodnotu DateTime.Now, protože ta se může změnit...
        /// Jiné události typu "Zavolej mě v čase ..." se o DateTime opírat musí.
        /// </summary>
        private System.Diagnostics.Stopwatch _TimeBase;
        /// <summary>
        /// Aktuální čas v počtu milisekund, od zahájení běhu. Nepřerušovaná časová osa.
        /// </summary>
        private long _CurrentTime { get { return _TimeBase.ElapsedMilliseconds; } }
        /// <summary>
        /// Přesnost časového rozlišení v milisekundách.
        /// Vysoká přesnost = nízké milisekundy vedou k vyšší zátěži systému.
        /// Výchozí přesnost = 20ms.
        /// Přípustné hodnoty: 1 až 1000.
        /// </summary>
        private int _ResolutionMiliseconds
        {
            get { return __ResolutionMiliseconds; }
            set { __ResolutionMiliseconds = (value < 1 ? 1 : (value > 1000 ? 1000 : value)); }
        }
        private int __ResolutionMiliseconds;

        #endregion
        #region Jediný thread OnBackground
        /// <summary>
        /// Inicializuje thread na pozadí
        /// </summary>
        private void _ThreadInit()
        {
            this._AppExit = false;
            this._Semaphore = new System.Threading.AutoResetEvent(false);
            System.Windows.Forms.Application.ApplicationExit += Application_ApplicationExit;
            this._Thread = new System.Threading.Thread(_ThreadRun)
            {
                Name = "WatchTimer",
                IsBackground = true
            };
            this._Thread.Start();
        }
        /// <summary>
        /// Eventhandler události <see cref="System.Windows.Forms.Application.ApplicationExit"/>.
        /// Zajistí ukončení běhu našeho vlákna.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            this._AppExit = true;
            this._Semaphore.Set();
        }
        /// <summary>
        /// Nekonečná *) procedura threadu na pozadí.
        /// Střídavě spí (po dobu <see cref="_ResolutionMiliseconds"/>) a pak kontroluje, jestli nemá něco dělat.
        /// (Nekonečná smyčka = jen do konce běhu aplikace)
        /// </summary>
        private void _ThreadRun()
        {
            while (true)
            {
                if (this._AppExit) break;
                this._RunItems();
                this._DoSleep();
            }
        }
        /// <summary>
        /// Metoda zajistí zpracování všech budíků: 
        /// Ten který je aktivní, ten se provede;
        /// Ten, který je po provedení vyřízen (anebo už je disablován dříve), ten se odstraní.
        /// </summary>
        private void _RunItems()
        {
            WatchItem[] items = this._LockedItems;         // Beru všechny prvky, abych mohl najít deaktivované a ty pak vyhodit ze seznamu
            foreach (WatchItem item in items)
            {
                if (item.IsActive && item.NeedRun)
                    item.Run();
                if (!item.IsActive)
                    _RemoveItem(item);
            }
        }
        /// <summary>
        /// Metoda zajistí uspání aktuálního threadu na dobu do příštího budíčku (pokud takový existuje) anebo na 1 minutu (sichr je sichr).
        /// Pokud zjistí, že některý budíček je nyní aktivní a zvoní, pak k uspání nedojde a řízení se z této metody vrací ihned.
        /// </summary>
        private void _DoSleep()
        {
            WatchItem[] items = this._LockedItems.Where(i => i.IsActivated).ToArray();   // Beru jen aktivované prvky, protože ty mohou deklarovat svůj platný čas SleepTime
            int time = 60000;
            if (items.Length > 0)
            {
                time = items.Min(i => i.SleepTime);
                if (time > 60000) time = 60000;
            }
            if (time > 0)
                this._Semaphore.WaitOne(time);
        }
        /// <summary>
        /// Metoda vrací pole všech budíků, získané z <see cref="_Items"/>, zajistí mezivláknovou synchronizaci
        /// </summary>
        private WatchItem[] _LockedItems
        {
            get
            {
                WatchItem[] items = null;
                lock (this._Items)
                    items = this._Items.Values.ToArray();
                return items;
            }
        }
        /// <summary>
        /// Příznak, že celá aplikace se ukončuje, a thread na pozadí má skončit svůj běh
        /// </summary>
        private bool _AppExit;
        /// <summary>
        /// Thread, ve kterém běží hodiny Timeru
        /// </summary>
        private System.Threading.Thread _Thread;
        /// <summary>
        /// Semafor = uspávač a budíček pro thread na pozadí
        /// </summary>
        private System.Threading.AutoResetEvent _Semaphore;
        #endregion
        #region Instanční práce s položkami = jednotlivé budíky
        /// <summary>
        /// Do interní kolekce přidá nový prvek a aktivuje jej.
        /// </summary>
        /// <param name="watchItem"></param>
        /// <param name="id">ID existujícího budíku, kterému chci změnit vlastnosti a reaktivovat jej. Pokud je null, nebo takový budík již neexistuje, bude standardně vytvořen a uložen nový budík.</param>
        private void _AddItem(WatchItem watchItem, Guid? id)
        {
            lock (_Items)
            {
                if (id.HasValue && _Items.TryGetValue(id.Value, out WatchItem currentItem))
                {
                    currentItem.RefreshFrom(watchItem, true, true);   // Reload dat včetně aktivace a resynchronizace ID:  watchItem.Guid = currentItem.Guid;
                }
                else
                {
                    watchItem.Owner = this;
                    _Items.Add(watchItem.Guid, watchItem);
                    watchItem.Activate();        // Aktivujeme nový budík = nastavíme jeho cílový čas buzení;
                }
            }
            this._Semaphore.Set();               // Rozsvítíme semafor, probudíme výkonný thread, ten si prověří zda má něco dělat a hlavně si nastaví vhodný interval na spánek
        }
        /// <summary>
        /// Ze seznamu odebere budík dle daného ID.
        /// </summary>
        /// <param name="guid"></param>
        private void _RemoveItem(Guid guid)
        {
            lock (_Items)
            {
                if (_Items.TryGetValue(guid, out var watchItem))
                {
                    watchItem.Disable();
                    _Items.Remove(guid);
                    watchItem.Owner = null;
                }
            }
        }
        /// <summary>
        /// Ze seznamu odebere daný budík.
        /// </summary>
        /// <param name="watchItem"></param>
        private void _RemoveItem(WatchItem watchItem)
        {
            lock (_Items)
            {
                if (_Items.ContainsKey(watchItem.Guid))
                {
                    watchItem.Disable();
                    _Items.Remove(watchItem.Guid);
                    watchItem.Owner = null;
                }
            }
        }
        #endregion
        #region class WatchItem : jeden časovač
        /// <summary>
        /// WatchItem : jeden časovač
        /// </summary>
        private class WatchItem
        {
            #region Konstrukce, statická data (proměnné)
            /// <summary>
            /// Privátní konstruktor jen přidělí <see cref="Guid"/>
            /// </summary>
            private WatchItem()
            {
                this._Guid = Guid.NewGuid();
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="action"></param>
            /// <param name="actionParam"></param>
            /// <param name="param"></param>
            /// <param name="miliseconds"></param>
            /// <param name="repeated"></param>
            /// <param name="synchronizeUI"></param>
            public WatchItem(Action action, Action<object> actionParam, object param, int miliseconds, bool repeated, bool synchronizeUI) : this()
            {
                this._Action = action;
                this._ActionParam = actionParam;
                this._Param = param;
                this._Miliseconds = miliseconds;
                this._ActiveTime = null;
                this._TargetTime = null;
                this._WatchType = (!repeated ? WatchItemType.SingleInterval : WatchItemType.RepeatedInterval);
                this._SynchronizeUI = synchronizeUI;
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="action"></param>
            /// <param name="actionParam"></param>
            /// <param name="param"></param>
            /// <param name="targetTime"></param>
            /// <param name="synchronizeUI"></param>
            public WatchItem(Action action, Action<object> actionParam, object param, DateTime targetTime, bool synchronizeUI) : this()
            {
                this._Action = action;
                this._ActionParam = actionParam;
                this._Param = param;
                this._Miliseconds = null;
                this._ActiveTime = null;
                this._TargetTime = targetTime;
                this._WatchType = WatchItemType.TargetTime;
                this._SynchronizeUI = synchronizeUI;
            }
            /// <summary>
            /// Vygeneruje nový <see cref="Guid"/>. Smí se volat pouze před zařazením do kolekce (Dictionary).
            /// </summary>
            public void RecreateGuid()
            {
                this._Guid = Guid.NewGuid();
            }
            /// <summary>
            /// Do this instance vloží data z dodané instance, a volitelně provede aktivaci.
            /// </summary>
            /// <param name="sourceItem">Zdrojový prvek, odsud se budou brát data, která se vloží do this instance</param>
            /// <param name="activate">true = provést aktivaci this instance <see cref="Activate(int?)"/> po vložení nových dat (aktivace nastaví cílový čas, kdy bude this budík aktivován)</param>
            /// <param name="resyncId">Pokud bude true, pak do instance <paramref name="sourceItem"/> vloží ID z this:<see cref="Guid"/></param>
            public void RefreshFrom(WatchItem sourceItem, bool activate = false, bool resyncId = false)
            {
                lock (this)
                {
                    this._Guid = sourceItem._Guid;
                    this._Action = sourceItem._Action;
                    this._ActionParam = sourceItem._ActionParam;
                    this._Param = sourceItem._Param;
                    this._Miliseconds = sourceItem._Miliseconds;
                    this._TargetTime = sourceItem._TargetTime;
                    this._WatchType = sourceItem._WatchType;
                    this._SynchronizeUI = sourceItem._SynchronizeUI;
                    if (activate)
                        this.Activate();
                }
            }
            /// <summary>
            /// ID časovače, používá se interně i jako vnější klíč pro přístup k "našemu" budíku
            /// </summary>
            public Guid Guid { get { return this._Guid; } }
            /// <summary>
            /// Vlastník = správce budíků
            /// </summary>
            public WatchTimer Owner { get; set; }
            /// <summary>
            /// Obsahuje true pro aktivní budík, false pro neaktivní. 
            /// Aktivní budíky jsou všechny zadané, které ještě neproběhly, opakovací budíky jsou aktivní stále.
            /// Budík lze deaktivovat metodou <see cref="Disable()"/> kdykoliv, anebo je deaktivován po proběhnutí jejich akce u budíků jednorázových.
            /// Neaktivní budíky se mají zahodit ze seznamu pracovních budíků.
            /// </summary>
            public bool IsActive { get { return this._IsActive; } }
            /// <summary>
            /// Obsahuje true pro aktivní (<see cref="IsActive"/>) a aktivovaný budík.
            /// Aktivní je každý, který dosud neproběhl, anebo který je opakovaný.
            /// Aktivovaný je budík s intervalem, který byl nastartován metodou <see cref="Activate(int?)"/>, a tedy ví kdy má proběhnout.
            /// </summary>
            public bool IsActivated { get { return this._IsActivated; } }
            /// <summary>
            /// ID budíku
            /// </summary>
            private Guid _Guid;
            /// <summary>
            /// Druh budíku
            /// </summary>
            private WatchItemType _WatchType;
            /// <summary>
            /// Interval v milisekundách
            /// </summary>
            private int? _Miliseconds;
            /// <summary>
            /// Cílový čas
            /// </summary>
            private DateTime? _TargetTime;
            /// <summary>
            /// Volaná akce bez parametrů
            /// </summary>
            private Action _Action;
            /// <summary>
            /// Volaná akce parametry
            /// </summary>
            private Action<object> _ActionParam;
            /// <summary>
            /// Parametry pro akci
            /// </summary>
            private object _Param;
            /// <summary>
            /// Volat v synchronizovaném threadu, pomocí <see cref="UiSynchronizationHelper.Invoke{TSender, TArgument}(TSender, TArgument, Action{TSender, TArgument}, Action)"/>
            /// </summary>
            private bool _SynchronizeUI;
            /// <summary>
            /// Systémový čas, kdy bude tento timer aktivován, 
            /// platí v režimu <see cref="WatchItemType.SingleInterval"/> nebo <see cref="WatchItemType.RepeatedInterval"/>.
            /// Výchozí hodnota = null, takový budík není aktivní.
            /// </summary>
            private long? _ActiveTime;
            /// <summary>
            /// Druh budíku
            /// </summary>
            private enum WatchItemType
            {
                /// <summary>
                /// Neurčen
                /// </summary>
                None = 0,
                /// <summary>
                /// Jednorázový budík, určený délkou intervalu od začátku do aktivace ("Minutka") = vzbuď mě za Nnn milisekund
                /// </summary>
                SingleInterval,
                /// <summary>
                /// Opakovaný budík, určený délkou intervalu od jedné do druhé aktivace ("Animátor") = vzbuď mě každých Nnn milisekund
                /// </summary>
                RepeatedInterval,
                /// <summary>
                /// Časový budík (z principu jednorázový), určený časem buzení (klasický "Budíček")
                /// </summary>
                TargetTime,
                /// <summary>
                /// Deaktivovaný budík. Pokud je budík odebrán ze seznamu, pak je souběžně deaktivován.
                /// Pokud je v jiném threadu instance budíku získána ze seznamu aktivních budíků, 
                /// a v mezičase (do jeho fyzického zpracování) je budík ze seznamu odebrán,
                /// pak je deaktivací zajištěno že již nemá být aktivní i když by nadešel jeho čas.
                /// </summary>
                Inactive
            }
            #endregion
            #region Detekce aktivity budíku
            /// <summary>
            /// Metoda nastaví cílový čas, kdy má být tento budíček aktivován od teď, kdy "teď" je dáno časem systému.
            /// Volitelně může nejprve nastavit novou délku intervalu 
            /// (parametr <paramref name="miliseconds"/> nejprve vloží do <see cref="_Miliseconds"/> a až poté provede aktivaci).
            /// </summary>
            /// <param name="miliseconds">Volitelně </param>
            public void Activate(int? miliseconds = null)
            {
                lock (this)
                {
                    if (this._IsInterval)
                    {
                        if (miliseconds.HasValue && miliseconds.Value > 0)
                            this._Miliseconds = miliseconds.Value;
                        this._ActiveTime = _CurrentTime + (long)this._Miliseconds;
                    }
                }
            }
            /// <summary>
            /// Zruší (zakáže) this timer. Toto je definitivní akce.
            /// Volá se při vyřazování budíku ze seznamu, před odebráním z Dictionary.
            /// </summary>
            public void Disable()
            {
                lock (this)
                {
                    this._WatchType = WatchItemType.None;
                }
            }
            /// <summary>
            /// Vrátí true, pokud tento budík se má spustit (<see cref="Run()"/>).
            /// </summary>
            /// <returns></returns>
            public bool NeedRun
            {
                get
                {
                    if (!_HasOwner) return false;
                    if (!_IsActive) return false;
                    int sleepTime = this.SleepTime;
                    return (sleepTime <= 0);
                }
            }
            /// <summary>
            /// Obsahuje počet milisekund do času, kdy se má tento budíček aktivovat.
            /// Pokud je čas budíku již dosažen, a má se aktivovat, pak property obsahuje 0 (nevrací se záporná čísla).
            /// Pokud je budík příliš daleko v budoucnosti, vrací se čas nanejvýše 1 hodina (v milisekundách 3600000), nikdy ne více.
            /// </summary>
            public int SleepTime
            {
                get
                {
                    if (!_HasOwner) return _HourI;
                    long currentTime = this._CurrentTime;
                    long sleepTime = 0L;
                    lock (this)
                    {
                        if (this._IsIntervalActivated) sleepTime = this._ActiveTime.Value - currentTime;
                        else if (this._IsTargetTime) sleepTime = (long)(((TimeSpan)(this._TargetTime - DateTime.Now)).TotalMilliseconds);
                        else sleepTime = _HourL;
                    }
                    if (sleepTime <= 0L) return 0;                   // Exspirovaný nebo aktivní timer
                    if (sleepTime >= _HourL) return _HourI;          // Timer platný za více než jednu hodinu (čísla jsou v milisekundách) = vrátí 1 hodinu spánku
                    return (int)sleepTime;                           // Bezpečný převod long => int, protože krajní meze jsme ošetřili.
                }
            }
            /// <summary>
            /// Jedna hodina v milisekundách, Int32
            /// </summary>
            private const int _HourI = 3600000;
            /// <summary>
            /// Jedna hodina v milisekundách, Int64
            /// </summary>
            private const long _HourL = 3600000L;
            /// <summary>
            /// true pokud máme <see cref="Owner"/>
            /// </summary>
            private bool _HasOwner { get { return (this.Owner != null); } }
            /// <summary>
            /// Aktuální čas v počtu milisekund, od zahájení běhu. Nepřerušovaná časová osa. Získáno z <see cref="Owner"/>.
            /// Pokud nemáme <see cref="Owner"/>, tato property vyhodí chybu.
            /// </summary>
            private long _CurrentTime { get { return this.Owner._CurrentTime; } }
            /// <summary>
            /// Obsahuje true, pokud jsme jakýkoli intervalový timer (s počtem milisekund)
            /// </summary>
            private bool _IsInterval { get { var wt = this._WatchType; return (wt == WatchItemType.SingleInterval || wt == WatchItemType.RepeatedInterval); } }
            /// <summary>
            /// Obsahuje true, pokud jsme jakýkoli intervalový timer (s počtem milisekund) a máme nastaven čas aktivace <see cref="_ActiveTime"/>
            /// </summary>
            private bool _IsIntervalActivated { get { var wt = this._WatchType; return ((wt == WatchItemType.SingleInterval || wt == WatchItemType.RepeatedInterval) && this._ActiveTime.HasValue); } }
            /// <summary>
            /// Obsahuje true, pokud jsme opakovaný intervalový timer
            /// </summary>
            private bool _IsRepeated { get { var wt = this._WatchType; return ((wt == WatchItemType.RepeatedInterval) && this._ActiveTime.HasValue); } }
            /// <summary>
            /// Obsahuje true, pokud jsme opakovaný intervalový timer a máme nastaven čas aktivace <see cref="_ActiveTime"/>
            /// </summary>
            private bool _IsRepeatedActivated { get { var wt = this._WatchType; return ((wt == WatchItemType.RepeatedInterval) && this._ActiveTime.HasValue); } }
            /// <summary>
            /// Obsahuje true, pokud jsme časovaný timer (s cílovým časem) a máme nastaven čas <see cref="_TargetTime"/>
            /// </summary>
            private bool _IsTargetTime { get { var wt = this._WatchType; return ((wt == WatchItemType.TargetTime) && this._TargetTime.HasValue); } }
            /// <summary>
            /// Obsahuje true, pokud jsme jakýkoli aktivní timer 
            /// </summary>
            private bool _IsActive { get { var wt = this._WatchType; return wt == WatchItemType.SingleInterval || wt == WatchItemType.RepeatedInterval || wt == WatchItemType.TargetTime; } }
            /// <summary>
            /// Obsahuje true pro aktivní (<see cref="IsActive"/>) a aktivovaný budík.
            /// Aktivní je každý, který dosud neproběhl, anebo který je opakovaný.
            /// Aktivovaný je budík s intervalem, který byl nastartován metodou <see cref="Activate(int?)"/>, a tedy ví kdy má proběhnout;
            /// anebo budík s cílovým časem, který je platný.
            /// </summary>
            private bool _IsActivated
            {
                get
                {
                    if (!this._IsActive) return false;
                    if (this._IsInterval) return this._ActiveTime.HasValue;
                    if (this._IsTargetTime) return this._TargetTime.HasValue;
                    return false;
                }
            }
            #endregion
            #region Provádění akce budíku
            /// <summary>
            /// Provede akci budíku.
            /// Pokud je budík jednorázový, pak po proběhnutí akce se deaktivuje, hodnota <see cref="IsActive"/> bude false, 
            /// a budík by se měl odstranit ze seznamu budíků.
            /// </summary>
            public void Run()
            {
                try
                {
                    this._RunAction();
                }
                catch { }
                finally
                {   // Ukončení akce => restart: provádíme i po chybě v provádění výkonné akce budíku !
                    //  znamená to: deaktivaci jednorázových budíků, anebo nastavení nového intervalu u opakovaných budíků.
                    this._DoneAction();
                }
            }
            /// <summary>
            /// Metoda provede vlastní akci budíku, včetně případné invokace GUI threadu
            /// </summary>
            private void _RunAction()
            {
                if (this._SynchronizeUI && ComponentConnector.Host.InvokeRequired)
                    ComponentConnector.Host.Invoke(new Action(this._RunActionThread));
                else
                    this._RunActionThread();
            }
            /// <summary>
            /// Metoda provede vlastní akci budíku. Metoda je již volána v potřebném threadu.
            /// </summary>
            private void _RunActionThread()
            {   // Vždy těsně před akcí otestuji aktivitu
                if (this._IsActive) this._Action?.Invoke();
                if (this._IsActive) this._ActionParam?.Invoke(this._Param);
            }
            /// <summary>
            /// Metoda restartuje (aktivuje) budík na další zvonění, pokud je tento budík opakovaný.
            /// Pokud je neopakovací, pak budík disabluje.
            /// </summary>
            private void _DoneAction()
            {
                if (this._IsRepeated)
                    this.Activate();
                else
                    this.Disable();
            }
            #endregion
        }
        #endregion
    }
    #endregion
}
