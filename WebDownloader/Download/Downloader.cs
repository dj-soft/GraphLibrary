using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;

namespace Djs.Tools.WebDownloader.Download
{
    #region WebDownload
    /// <summary>
    /// Controler paralelních downloadů
    /// </summary>
    public class WebDownload
    {
        public WebDownload()
        {
            this._DownloadState = DownloadState.Initiated;
            this._ItemList = new List<DownloadItem>();
        }
        /// <summary>
        /// Zahájí provádění downloadu souborů daných generátorem adres
        /// </summary>
        /// <param name="webAdress"></param>
        public void Start(WebAdress webAdress)
        {
            if (!WebAdressValid(webAdress))
            {
                Dialogs.Warning("Nelze nastartovat download, zadaná adresa není platná.");
                return;
            }

            if (this.IsWorking)
            {
                Dialogs.Warning("Nelze nastartovat download, dosud běží.");
                return;
            }

            this.State = DownloadState.Initiated;

            if (this._SemaphoreDownload != null)
            {
                this._SemaphoreDownload.Dispose();
                this._SemaphoreDownload = null;
            }
            this._SemaphoreDownload = new AutoResetEvent(false);

            if (this._SemaphoreInteractive != null)
            {
                this._SemaphoreInteractive.Dispose();
                this._SemaphoreInteractive = null;
            }
            this._SemaphoreInteractive = new AutoResetEvent(false);

            this._ItemList.Clear();
            this.WebAdress = webAdress;
            
            this.State = DownloadState.Working;

            if (this._WorkThread != null && this._WorkThread.ThreadState == ThreadState.Running)
            {
                this._WorkThread.Abort();
                this._WorkThread = null;
            }
            this._WorkThread = new Thread(this._StartDwnl);
            this._WorkThread.Name = "WorkThread";
            this._WorkThread.IsBackground = true;
            this._WorkThread.Priority = ThreadPriority.BelowNormal;

            this._WorkThread.Start();
        }
        /// <summary>
        /// Kontrola adresy. Vrací true pokud je platná. Hlásí Warning pokud je neplatná, pak vrací false.
        /// </summary>
        /// <param name="webAdress"></param>
        /// <returns></returns>
        private static bool WebAdressValid(WebAdress webAdress)
        {
            if (webAdress != null && webAdress.IsValid) return true;
            return false;
        }
        /// <summary>
        /// Generátor adres
        /// </summary>
        private WebAdress WebAdress;
        /// <summary>
        /// Thread který řídí download
        /// </summary>
        private Thread _WorkThread;
        /// <summary>
        /// Semafor, který řídí čekání na uvolnění pracovního slotu downloadu
        /// </summary>
        private AutoResetEvent _SemaphoreDownload;
        /// <summary>
        /// Semafor, který řídí čekání na interaktivní ukončení režimu Pauza
        /// </summary>
        private AutoResetEvent _SemaphoreInteractive;
        /// <summary>
        /// Maximální počet simultánních vláken downloadu
        /// </summary>
        public int ThreadMaxCount { get; set; }
        #region Stavy, změny stavu, požadavky na změnu, eventy
        /// <summary>
        /// true pokud objekt pracuje (stav je: Working, Paused, Cancelling).
        /// </summary>
        public bool IsWorking
        {
            get
            {
                DownloadState s = this._DownloadState; 
                return (
                    s == DownloadState.Working ||
                    s == DownloadState.Paused ||
                    s == DownloadState.Cancelling);
            }
        }
        /// <summary>
        /// Povolí pokračování stahování poté, kdy bylo pauzováno (DownloadPause)
        /// </summary>
        public void DownloadResume()
        {
            if (this.State == DownloadState.Paused || this.State == DownloadState.Cancelling)
                this.State = DownloadState.Working;
        }
        /// <summary>
        /// Pozastaví stahování (Pauza), z pauzy lze pokračovat (DownloadResume) nebo stopnout (DownloadCancel).
        /// </summary>
        public void DownloadPause()
        {
            if (this.State == DownloadState.Working)
                this.State = DownloadState.Paused;
        }
        /// <summary>
        /// Zruší stahování (Stop). Pokud běží nějaké downloady, přechází se do stavu Cancelling, jinak do stavu Cancelled.
        /// </summary>
        public void DownloadCancel()
        {
            if (this.State == DownloadState.Working || this.State == DownloadState.Paused)
                this.State = DownloadState.Cancelling;
            else if (this.State == DownloadState.Cancelling)
                this.State = DownloadState.Cancelled;
        }
        /// <summary>
        /// Obsahuje true pokud stav == Cancelled.
        /// </summary>
        public bool AbortNow { get { return (this._DownloadState == DownloadState.Cancelled); } }
        /// <summary>
        /// Příznak dokončení číselné řady
        /// </summary>
        public bool IsDone { get; private set; }
        /// <summary>
        /// Stav downloadu.
        /// Změnit hodnotu stavu může jen proces sám, změna stavu nastavuje další příznaky.
        /// </summary>
        public DownloadState State
        {
            get { return this._DownloadState; }
            protected set
            {
                if (value != this._DownloadState)
                {
                    DownloadState stateBefore = this._DownloadState;
                    this._DownloadState = value;

                    if (stateBefore != DownloadState.Initiated && value == DownloadState.Initiated)
                    {   // Jedenkrát při zahájení downloadu
                        this.IsDone = false;
                    }
                    if (stateBefore != DownloadState.Working && value == DownloadState.Working)
                    {   // Při zahájení downloadu a při přechodu ze stavu Paused nebo Cancelling zpátky do Working
                    }
                    if ((stateBefore == DownloadState.Paused || stateBefore == DownloadState.Cancelling) && value != stateBefore)
                    {   // Při ukončení pauzy nebo stavu Cancelling:
                        this._SemaphoreSendInteractiveSignal();
                    }

                    this.OnStateChanged(stateBefore, value);
                }
            }
        }
        private DownloadState _DownloadState;
        protected virtual void OnStateChanged(DownloadState stateBefore, DownloadState stateCurrent)
        {
            if (this.StateChanged != null)
                this.StateChanged(this, new DownloadStateChangedArgs(stateBefore, stateCurrent));
        }
        /// <summary>
        /// Událost volaná po změně stavu celkového procesu downloadu
        /// </summary>
        public event DownloadStateChangedHandler StateChanged;
        /// <summary>
        /// Událost volaná po jakékoli změně downloadu (nový soubor, progres, dokončení).
        /// </summary>
        public event DownloadProgressChangedHandler DownloadProgress;
        #endregion
        #region Řízení downloadu
        private void _StartDwnl()
        {
            this.IsDone = false;
            this._SemaphoreDownload.Reset();
            this._SemaphoreInteractive.Reset();
            this._ItemId = 0;

            while (true)
            {
                if (this.AbortNow) break;
                DownloadItem item = this._GetWorkItem();
                if (item == null) break;
                this._StartWorkItem(item);
            }
        }
        /// <summary>
        /// Najde a vrátí objekt DownloadItem, který může zahájit download.
        /// Pokud nyní běží maximum downloadů, počká na uvolnění některého slotu.
        /// Pokud jsme ve stavu Cancel nebo Done, pak počká na uvolnění všech slotů a vrátí null = volající metoda tím skončí smyčku.
        /// / vytvoří nový / vrátí 
        /// </summary>
        /// <returns></returns>
        private DownloadItem _GetWorkItem()
        {
            if (this.IsInteractiveStop()) return null;

            if (this.IsDone)
            {   // Pokud je vše hotovo, pak jen počkám na dokončení všech downloadovaných položek:
                this._WaitForItemCount(0);
                return null;
            }

            // Není hotovo, a není ani interaktivní důvod skončit: počkáme na volný slot:
            this._WaitForFreeItem();
            if (this.AbortNow) return null;

            // Najdeme nebo vytvoříme nový slot:
            DownloadItem item = null;
            lock (this._ItemList)
            {
                item = this._ItemList.FirstOrDefault(i => i.TryLock());       // Najde první položku, která je dostupná, tu zamkne a vrátí.
                if (item == null)
                {
                    item = this._CreateNewDownloadItem();
                    this._ItemList.Add(item);
                }
            }
            return item;
        }
        /// <summary>
        /// Vytvoří a vrátí novou položku downloadu.
        /// </summary>
        /// <returns></returns>
        private DownloadItem _CreateNewDownloadItem()
        {
            DownloadItem item = new DownloadItem(true);
            item.DownloadChanged += new DownloadItemStateChangedHandler(_ItemDownloadChanged);
            return item;
        }
        /// <summary>
        /// Metoda řeší interaktivní požadavky na pauzu a klidný nebo abort cancel.
        /// Pokud je nějaký cancel, vrací true.
        /// Pokud je pauza, čeká na její uvolnění a pak řeší co dál.
        /// Jinak vrací false = není důvod čekat na interaktivní žádosti.
        /// </summary>
        /// <returns></returns>
        private bool IsInteractiveStop()
        {
            while (true)
            {
                switch (this.State)
                {
                    case DownloadState.Paused:
                        // V pauze čekám (1 sec) na jakoukoli interaktivní změnu, poté to vyhodnotíme znovu:
                        this._SemaphoreWaitForInteractiveChange(1000);
                        break;
                    case DownloadState.Cancelling:
                        // Cancelujeme: pokud nám už doběhly všechny downloady, pak vracím true = konec:
                        int count = this._ItemListWorkingCount();
                        if (count == 0) return true;
                        // Nedoběhly: počkám chvilku (1 sec) na interaktivní změnu, a pak se na to podíváme znovu:
                        this._SemaphoreWaitForInteractiveChange(1000);
                        break;
                    case DownloadState.Cancelled:
                        // Abortováno: vracím true bez čekání:
                        return true;
                    default:
                        // Cokoli jiného: není důvod ukončit smyčku z interaktivních důvodů:
                        return false;
                }
            }
        }
        /// <summary>
        /// Tato metoda čeká, až počet běžících downloadů bude menší než this._MaxThread.
        /// Tuto hodnotu vyhodnocuje před každým testem znovu = tím umožní měnit hodnotu dynamicky i při čekání.
        /// Pokud při čekání zjistí, že je nahozen příznak AbortNow = true, pak nečeká na nic a končí, pak vrací false.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private bool _WaitForFreeItem()
        {
            return this._WaitForItemCount(null);
        }
        /// <summary>
        /// Tato metoda čeká, až počet běžících downloadů bude menší nebo roven danému číslu.
        /// Při zadání hodnoty 0 tedy čeká na ukončení všech downloadů.
        /// Při zadání hodnoty null čeká na počet (this._MaxThread - 1), 
        /// tuto hodnotu vyhodnocuje před každým testem znovu = tím umožní měnit hodnotu dynamicky i při čekání.
        /// Pokud při čekání zjistí, že je nahozen příznak AbortNow = true, pak nečeká na nic a končí, pak vrací false.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private bool _WaitForItemCount(Int32? count)
        {
            while (true)
            {
                if (this.AbortNow) return false;
                int cnt = this._ItemListWorkingCount();              // Tohle chvilku může trvat (je tam lock)
                int max = (count.HasValue ? count.Value : this._MaxThread - 1);
                if (cnt <= max) return true;
                this._SemaphoreWaitForDownloadEnd(1000);
            }
        }
        /// <summary>
        /// Projde položky v seznamu _ItemList (seznam po dobu práce zamyká, ale nikoli položky).
        /// Mrtvé položky (v případě jejich chyby nebo potřeby) odstraní, a sečte zbývající živé, 
        /// tento počet živých položek vrací.
        /// </summary>
        /// <returns></returns>
        private int _ItemListWorkingCount()
        {
            int count = 0;
            lock (this._ItemList)
            {
                for (int i = 0; i < this._ItemList.Count; i++)
                {
                    DownloadItem item = this._ItemList[i];
                    if (item.IsLive)
                    {
                        count++;
                    }
                    else
                    {   // Neaktivní položku odeberu ze seznamu tehdy, když je nastaveno _ReuseDownloadItem = false,
                        // anebo když jde o položku, která byla cancelována anebo skončila chybou.
                        if (!this._ReuseDownloadItem || (item.State == DownloadItemState.Cancelled || item.State == DownloadItemState.Error))
                        {
                            this._ItemList.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
            return count;
        }
        /// <summary>
        /// Pomocí dodané položky začne stahovat soubor, který je dán aktuální adresou.
        /// </summary>
        /// <param name="item"></param>
        private void _StartWorkItem(DownloadItem item)
        {
            int itemId = ++this._ItemId;
            string url = this.WebAdress.Text;
            item.Start(itemId, url);               // Jakákoli změna stavu položky se hlásí do handleru _ItemDownloadChanged

            bool isDone = this.WebAdress.Increment();
            if (isDone)
                this.IsDone = true;
        }
        /// <summary>
        /// Handler události, kdy na položce došlo k jakékoli změně.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ItemDownloadChanged(object sender, DownloadItemStateChangedArgs args)
        {
            if (args.Item.IsDone)
                this._SemaphoreSendDownloadEndSignal();
        }
        /// <summary>
        /// ID položky, pořadové číslo počínaje 1
        /// </summary>
        private int _ItemId;
        /// <summary>
        /// Čeká na dokončení downloadu některé položky (pomocí semaforu).
        /// Je vhodné zadat timeout čekání, abychom zde nečekali dva roky.
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        private void _SemaphoreWaitForDownloadEnd(int millisecondsTimeout)
        {
            this._SemaphoreDownload.WaitOne(millisecondsTimeout);
        }
        /// <summary>
        /// Čeká na ukončení stavu Pauza (pomocí semaforu).
        /// Je vhodné zadat timeout čekání, abychom zde nečekali dva roky.
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        private void _SemaphoreWaitForInteractiveChange(int millisecondsTimeout)
        {
            this._SemaphoreInteractive.WaitOne(millisecondsTimeout);
        }
        /// <summary>
        /// Metoda se má zavolat po dokončení downloadu a po změně stavu položky DownloadItem (poté, kdy její Status už není Working).
        /// Tato metoda vydá signál čekající smyčce že se uvolnila nějaká položka downloadu a může se použít pro další soubor.
        /// </summary>
        private void _SemaphoreSendDownloadEndSignal()
        {
            this._SemaphoreDownload.Set();
        }
        /// <summary>
        /// Metoda se má zavolat po dokončení stavu z Pauza na něco jiného.
        /// Tato metoda vydá signál čekající smyčce že se uvolnila nějaká položka downloadu a může se použít pro další soubor.
        /// </summary>
        private void _SemaphoreSendInteractiveSignal()
        {
            this._SemaphoreInteractive.Set();
        }
        private bool _ReuseDownloadItem = true;
        /// <summary>
        /// Maximum vláken dle WebAdress v rozmezí 1 ÷ 12
        /// </summary>
        private int _MaxThread { get { int max = this.WebAdress.ThreadMaxCount; return (max > 12 ? 12 : (max < 1 ? 1 : max)); } }
        private List<DownloadItem> _ItemList;
        #endregion
    }
    /// <summary>
    /// Stavy procesu celého downloadu (nikoli jednotlivé soubory)
    /// </summary>
    public enum DownloadState
    {
        /// <summary>
        /// Připraveno
        /// </summary>
        Initiated,
        /// <summary>
        /// Pracuje
        /// </summary>
        Working,
        /// <summary>
        /// Pauza
        /// </summary>
        Paused,
        /// <summary>
        /// Požadavek na Cancel, čekání na dokončení běžících přenosů
        /// </summary>
        Cancelling,
        /// <summary>
        /// Dokončeno po Cancel, neběží žádné přenosy.
        /// </summary>
        Cancelled,
        /// <summary>
        /// Abortováno bez čekání na dokončení přenosů (Cancel i při běžících downloadech).
        /// </summary>
        Aborted,
        /// <summary>
        /// Ukončeno vlivem počtu chyb
        /// </summary>
        StoppedDueErrors,
        /// <summary>
        /// Dokončeno samovolně po dojetí do konce, bez jiného důvodu
        /// </summary>
        Done
    }
    #region Delegáty a EventArgs
    public class DownloadStateChangedArgs : EventArgs
    {
        public DownloadStateChangedArgs(DownloadState stateBefore, DownloadState stateCurrent)
        {
            this.StateBefore = stateBefore;
            this.StateCurrent = stateCurrent;
        }
        public DownloadState StateBefore { get; private set; }
        public DownloadState StateCurrent { get; private set; }
    }
    public delegate void DownloadStateChangedHandler(object sender, DownloadStateChangedArgs args);
    public class DownloadProgressChangedArgs : EventArgs
    { }
    public delegate void DownloadProgressChangedHandler(object sender, DownloadProgressChangedArgs args);
    #endregion
    #endregion
    #region DownloadItem
    public class DownloadItem : IDisposable
    {
        /// <summary>
        /// Vytvoří nvou instanci, volitelně ve stavu Blocked.
        /// </summary>
        /// <param name="locked"></param>
        public DownloadItem(bool locked) : this()
        {
            if (locked)
                this.State = DownloadItemState.Blocked;
        }
        public DownloadItem()
        {
            this.WebClient = new System.Net.WebClient();
            this.WebClient.DownloadFileCompleted += new AsyncCompletedEventHandler(WebClient_DownloadFileCompleted);
            this.WebClient.DownloadDataCompleted += new System.Net.DownloadDataCompletedEventHandler(WebClient_DownloadFileCompleted);
            this.WebClient.DownloadProgressChanged += new System.Net.DownloadProgressChangedEventHandler(WebClient_DownloadProgressChanged);
            this.WebClient.UseDefaultCredentials = true;
            this.WebClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache);

            this.State = DownloadItemState.Initiated;
        }
        void IDisposable.Dispose()
        {
            if (this.WebClient != null)
            {
                if (this.State == DownloadItemState.Working)
                    this.WebClient.CancelAsync();
                this.WebClient.Dispose();
                this.WebClient = null;
            }
        }
        private System.Net.WebClient WebClient;
        /// <summary>
        /// ID zadané ve startu
        /// </summary>
        public int Id { get; private set; }
        /// <summary>
        /// URL odkazu
        /// </summary>
        public string Url { get; private set; }
        /// <summary>
        /// URI odkazu
        /// </summary>
        public Uri Uri { get; private set; }
        /// <summary>
        /// Lokální úložiště souboru
        /// </summary>
        public string LocalFile { get; private set; }
        /// <summary>
        /// Stav stahování
        /// </summary>
        public DownloadItemState State { get; private set; }
        /// <summary>
        /// true pokud tato položka právě něco zpracovává
        /// </summary>
        public bool IsLive { get { return (this.State == DownloadItemState.Working); } }
        /// <summary>
        /// true pokud tato má skončený download.
        /// Tj. stav je: Done, Cancelled, Error.
        /// </summary>
        public bool IsDone { get { return (this.State == DownloadItemState.Done || this.State == DownloadItemState.Cancelled || this.State == DownloadItemState.Error); } }
        /// <summary>
        /// true pokud tuto položku lze obsadit pro další práci.
        /// Tj. stav je: Initiated, Done, Cancelled, Error.
        /// Pokud je stav Blocked nebo Working, pak není Available.
        /// </summary>
        public bool IsAvailable { get { return (this.State == DownloadItemState.Initiated || this.State == DownloadItemState.Done || this.State == DownloadItemState.Cancelled || this.State == DownloadItemState.Error); } }
        /// <summary>
        /// Obsadí tuto položku = nastaví stav Blocked, pokud je to možné.
        /// Pokud bude položka zablokována, vrátí true.
        /// Pokud to nejde, vrátí false ale ne chybu.
        /// </summary>
        public bool TryLock()
        {
            bool result = false;
            lock (this)
            {
                if (this.IsAvailable)
                {
                    this.State = DownloadItemState.Blocked;
                    result = true;
                }
            }
            return result;
        }
        /// <summary>
        /// Obsadí tuto položku = nastaví stav Blocked, pokud je to možné.
        /// Pokud to není možné, hodí chybu.
        /// </summary>
        public void Lock()
        {
            lock (this)
            {
                if (this.IsAvailable)
                {
                    this.State = DownloadItemState.Blocked;
                }
                else
                {
                    throw new AppException("Položku DownloadItem nelze zamknout, není dostupná.");
                }
            }
        }

        public long TotalSize { get; private set; }
        /// <summary>
        /// Celková očekávaná velikost souboru
        /// </summary>
        public long TotalBytesToReceive { get; private set; }
        /// <summary>
        /// Počet byte dosud přijatých (kumulativní hodnota, blíží se k TotalBytesToReceive)
        /// </summary>
        public long BytesReceived { get; private set; }
        /// <summary>
        /// Progres v procentech (0 ÷ 100)
        /// </summary>
        public int ProgressPercentage { get; private set; }
        /// <summary>
        /// true pokud bylo stornováno
        /// </summary>
        public bool Cancelled { get; private set; }
        /// <summary>
        /// Chyba pokud k ní došlo
        /// </summary>
        public Exception Error { get; private set; }
        /// <summary>
        /// Událost která je volána při každé změně: progres, konec, chyba, cancel.
        /// Konkrétní stav je možno číst v this.State.
        /// </summary>
        public event DownloadItemStateChangedHandler DownloadChanged;
        #region Rychlost stahování, blok, časy
        /// <summary>
        /// Obsahuje rychlost stažení aktuálního jednoho bloku v KB/sec
        /// </summary>
        public decimal BandKBsBlock { get { return this._GetBandKB(this.CurrentBlockSize, this.CurrentBlockStartTime, this.CurrentEventTime); } }
        /// <summary>
        /// Obsahuje aktuální rychlost stahování celého souboru v KB/sec.
        /// Zahrnuje to i úvodní čekání na první blok.
        /// Je možno použít property BandKBsData, která obsahuje jen stahování dat, ale ne úvodní čekání na ně.
        /// </summary>
        public decimal BandKBsTotal { get { return this._GetBandKB(this.BytesReceived, this.DownloadStartTime, this.CurrentEventTime); } }
        /// <summary>
        /// Obsahuje aktuální rychlost stahování celého souboru v KB/sec.
        /// Tato property nezahrnuje úvodní čekání na první blok (rychlost měří až od rozběhnutí stahování).
        /// Je možno použít property BandKBsTotal, která obsahuje i úvodní čekání na zahájení downloadu.
        /// </summary>
        public decimal BandKBsData { get { return this._GetBandKB(this.BytesReceived - this.SecondBlockStartPosition, this.SecondBlockStartTime, this.CurrentEventTime); } }

        /// <summary>
        /// Počet stažených byte při předchozí události
        /// </summary>
        protected long CurrentBlockStartPosition { get; private set; }
        /// <summary>
        /// Čas počátku stahování aktuálního bloku (=čas poslední předchozí aktivity)
        /// </summary>
        protected DateTime CurrentBlockStartTime { get; private set; }
        /// <summary>
        /// Počet byte v aktuálně přijatém bloku (hodnota BytesReceived v aktuální události mínus tato hodnota při předchozí události)
        /// </summary>
        public long CurrentBlockSize { get { return this.BytesReceived - this.CurrentBlockStartPosition; } }
        /// <summary>
        /// Počáteční pozice druhého bloku, potlačuje vliv čekání na start downloadu při určování celkové rychlosti stahování
        /// </summary>
        protected long SecondBlockStartPosition { get; private set; }
        /// <summary>
        /// Čas počátku stahování druhého bloku, potlačuje vliv čekání na start downloadu při určování celkové rychlosti stahování
        /// </summary>
        protected DateTime SecondBlockStartTime { get; private set; }
        /// <summary>
        /// Čas zahájení downloadu jednoho souboru
        /// </summary>
        public DateTime DownloadStartTime { get; private set; }
        /// <summary>
        /// Aktuální čas aktivity (kdy došlo k události DownloadChanged = progres, konec, atd)
        /// </summary>
        public DateTime CurrentEventTime { get; private set; }
        /// <summary>
        /// Doba od začátku downloadu (od DownloadStartTime do CurrentEventTime)
        /// </summary>
        public TimeSpan? DownloadTime { get { return (this.DownloadStartTime == DateTime.MinValue || this.CurrentEventTime == DateTime.MinValue || this.CurrentEventTime < this.DownloadStartTime ? (TimeSpan?)null : (TimeSpan?)(this.CurrentEventTime - this.DownloadStartTime)); } }
        /// <summary>
        /// Doba downloadu jednoho bloku (od CurrentBlockStartTime do CurrentEventTime)
        /// </summary>
        public TimeSpan? CurrentBlockTime { get { return (this.CurrentBlockStartTime == DateTime.MinValue || this.CurrentEventTime == DateTime.MinValue || this.CurrentEventTime < this.CurrentBlockStartTime ? (TimeSpan?)null : (TimeSpan?)(this.CurrentEventTime - this.CurrentBlockStartTime)); } }
        /// <summary>
        /// Vrátí rychlost stahování v KB/sec určenou na základě počtu byte a časového intervalu.
        /// Rychlost je zaokrouhlena na 0,1 KB/sec.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private decimal _GetBandKB(long bytes, DateTime begin, DateTime end)
        {
            if (begin == DateTime.MinValue || end == DateTime.MinValue || end < begin || bytes <= 0L) return 0m;
            TimeSpan time = end - begin;
            decimal band = (decimal)bytes / (decimal)time.Seconds;       // Byte/sec
            return Math.Round((band / 1024), 1);                         // KB/sec
        }
        #endregion
        #region Download one file
        /// <summary>
        /// Zahájí stahování daného URL do výchozího umístění
        /// </summary>
        /// <param name="url"></param>
        public void Start(string url)
        {
            this.Start(0, url, CreateLocalPath(url));
        }
        /// <summary>
        /// Zahájí stahování daného URL do explicitního umístění
        /// </summary>
        /// <param name="id"></param>
        /// <param name="url"></param>
        /// <param name="localFile"></param>
        public void Start(string url, string localFile)
        {
            this.Start(0, url, localFile);
        }
        /// <summary>
        /// Zahájí stahování daného URL do výchozího umístění
        /// </summary>
        /// <param name="id"></param>
        /// <param name="url"></param>
        public void Start(int id, string url)
        {
            this.Start(id, url, CreateLocalPath(url));
        }
        /// <summary>
        /// Zahájí stahování daného URL do explicitního umístění
        /// </summary>
        /// <param name="id"></param>
        /// <param name="url"></param>
        /// <param name="localFile"></param>
        public void Start(int id, string url, string localFile)
        {
            if (this.State == DownloadItemState.Working)
                throw new InvalidOperationException("Nelze nastartovat další download, pokud stávající ještě neskončil.");

            if (String.IsNullOrEmpty(url))
                throw new InvalidOperationException("Nelze provést download, URL adresa je prázdná.");

            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                throw new InvalidOperationException("Nelze provést download, URL adresa [" + url + "] není platná.");
            
            if (!ExistDirectory(localFile))
                throw new InvalidOperationException("Nelze provést download, adresář pro soubor [" + localFile + "] není možno vytvořit.");

            this.Id = id;
            this.Url = url;
            this.Uri = uri;
            this.LocalFile = localFile;
            this.Cancelled = false;
            this.Error = null;
            this.ClearWebResponseData();
            this.State = DownloadItemState.Working;
            this.WebClient.DownloadFileAsync(this.Uri, this.LocalFile);
        }
        private void WebClient_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            DateTime now = DateTime.Now;
            this.CurrentEventTime = now;
            long bytes = e.BytesReceived;
            this.BytesReceived = bytes;
            this.TotalBytesToReceive = e.TotalBytesToReceive;
            this.ProgressPercentage = e.ProgressPercentage;
            this.ProcessWebResponse();
            this.OnDownloadChanged();                     // event události
            this.CurrentBlockStartTime = now;             // Počáteční hodnoty pro příští událost (čas a počet Byte)
            this.CurrentBlockStartPosition = bytes;
            if ((this.SecondBlockStartPosition == 0L || this.SecondBlockStartTime == DateTime.MinValue) && bytes > 0L)
            {   // Hodnoty určující počátek druhého bloku dat (počínaje druhým blokem by nemělo docházet k čekání na data, jako v prvním bloku):
                this.SecondBlockStartPosition = bytes;
                this.SecondBlockStartTime = now;
            }
        }
        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.CurrentEventTime = DateTime.Now;
            this.Cancelled = e.Cancelled;
            this.Error = e.Error;
            this.State = (e.Cancelled ? DownloadItemState.Cancelled : (e.Error != null ? DownloadItemState.Error : DownloadItemState.Done));
            if (this.State == DownloadItemState.Done)
                this.SetFileDateByResponse();
            this.OnDownloadChanged();
        }
        protected virtual void OnDownloadChanged()
        {
            if (this.DownloadChanged != null)
                this.DownloadChanged(this, new DownloadItemStateChangedArgs(this));
        }
        #endregion
        #region WebResponse data
        private Dictionary<string, string> _ResponseDict;
        public DateTime? WebResponseLastModified { get; private set; }
        public Int64? WebResponseContentLength { get; private set; }
        public string WebResponseContentType { get; private set; }
        public string WebResponseServer { get; private set; }
        /// <summary>
        /// Vynuluje data v this, která se mají načítat z odpovědi this.WebClient.ResponseHeaders
        /// </summary>
        private void ClearWebResponseData()
        {
            this.TotalSize = 0L;
            this.TotalBytesToReceive = 0L;
            this.CurrentBlockStartPosition = 0L;
            this.BytesReceived = 0L;
            this.ProgressPercentage = 0;

            this.DownloadStartTime = DateTime.Now;
            this.CurrentEventTime = DateTime.MinValue;
            this.CurrentBlockStartTime = this.DownloadStartTime;
            this.SecondBlockStartPosition = 0L;
            this.SecondBlockStartTime = DateTime.MinValue;

            this._ResponseDict = new Dictionary<string, string>();
            this.WebResponseLastModified = null;

        }
        /// <summary>
        /// Zpracuje informace z this.WebClient.ResponseHeaders, pokud ještě nejsou načteny.
        /// </summary>
        private void ProcessWebResponse()
        {
            foreach (var header in this.WebClient.ResponseHeaders)
            {
                string name = header as string;
                if (!String.IsNullOrEmpty(name) && !this._ResponseDict.ContainsKey(name))
                {
                    string value = this.WebClient.ResponseHeaders[name];
                    this._ResponseDict.Add(name, value);
                    switch (name)
                    {
                        case "Content-Length":
                            if (!this.WebResponseContentLength.HasValue)
                                this.WebResponseContentLength = ParseResponseValue(value, 0L);
                            break;
                        case "Content-Type":
                            if (this.WebResponseContentType == null)
                                this.WebResponseContentType = value;
                            break;
                        case "Last-Modified":
                            if (!this.WebResponseLastModified.HasValue)
                                this.WebResponseLastModified = ParseResponseValue(value, (DateTime?)null);
                            break;
                        case "Server":
                            if (this.WebResponseServer == null)
                                this.WebResponseServer = value;
                            break;
                    }
                }
            }
            int x = this._ResponseDict.Count;
            int y = x + x;

            /*
             * 
+		[0]	{[Connection, keep-alive]}	System.Collections.Generic.KeyValuePair<string,string>
+		[1]	{[Content-Length, 3434684]}	System.Collections.Generic.KeyValuePair<string,string>
+		[2]	{[Cache-Control, max-age=864000,private]}	System.Collections.Generic.KeyValuePair<string,string>
+		[3]	{[Content-Type, image/jpeg]}	System.Collections.Generic.KeyValuePair<string,string>
+		[4]	{[Date, Fri, 07 Nov 2014 13:14:55 GMT]}	System.Collections.Generic.KeyValuePair<string,string>
+		[5]	{[Expires, Mon, 17 Nov 2014 13:14:55 GMT]}	System.Collections.Generic.KeyValuePair<string,string>
+		[6]	{[ETag, "540a35e9-3468bc"]}	System.Collections.Generic.KeyValuePair<string,string>
+		[7]	{[Last-Modified, Fri, 05 Sep 2014 22:15:05 GMT]}	System.Collections.Generic.KeyValuePair<string,string>
+		[8]	{[Server, nginx]}	System.Collections.Generic.KeyValuePair<string,string>
+		[9]	{[Accept-Ranges, bytes]}	System.Collections.Generic.KeyValuePair<string,string>
             * 
+		[0]	{[Connection, keep-alive]}	System.Collections.Generic.KeyValuePair<string,string>
+		[1]	{[Content-Length, 3434684]}	System.Collections.Generic.KeyValuePair<string,string>
+		[2]	{[Cache-Control, max-age=864000,private]}	System.Collections.Generic.KeyValuePair<string,string>
+		[3]	{[Content-Type, image/jpeg]}	System.Collections.Generic.KeyValuePair<string,string>
+		[4]	{[Date, Fri, 07 Nov 2014 13:14:55 GMT]}	System.Collections.Generic.KeyValuePair<string,string>
+		[5]	{[Expires, Mon, 17 Nov 2014 13:14:55 GMT]}	System.Collections.Generic.KeyValuePair<string,string>
+		[6]	{[ETag, "540a35e9-3468bc"]}	System.Collections.Generic.KeyValuePair<string,string>
+		[7]	{[Last-Modified, Fri, 05 Sep 2014 22:15:05 GMT]}	System.Collections.Generic.KeyValuePair<string,string>
+		[8]	{[Server, nginx]}	System.Collections.Generic.KeyValuePair<string,string>
+		[9]	{[Accept-Ranges, bytes]}	System.Collections.Generic.KeyValuePair<string,string>
             * 
+		[0]	{[Connection, keep-alive]}	System.Collections.Generic.KeyValuePair<string,string>
+		[1]	{[Content-Length, 3434684]}	System.Collections.Generic.KeyValuePair<string,string>
+		[2]	{[Cache-Control, max-age=864000,private]}	System.Collections.Generic.KeyValuePair<string,string>
+		[3]	{[Content-Type, image/jpeg]}	System.Collections.Generic.KeyValuePair<string,string>
+		[4]	{[Date, Fri, 07 Nov 2014 13:14:55 GMT]}	System.Collections.Generic.KeyValuePair<string,string>
+		[5]	{[Expires, Mon, 17 Nov 2014 13:14:55 GMT]}	System.Collections.Generic.KeyValuePair<string,string>
+		[6]	{[ETag, "540a35e9-3468bc"]}	System.Collections.Generic.KeyValuePair<string,string>
+		[7]	{[Last-Modified, Fri, 05 Sep 2014 22:15:05 GMT]}	System.Collections.Generic.KeyValuePair<string,string>
+		[8]	{[Server, nginx]}	System.Collections.Generic.KeyValuePair<string,string>
+		[9]	{[Accept-Ranges, bytes]}	System.Collections.Generic.KeyValuePair<string,string>
             * 
             * 
             */
        }

        private static int ParseResponseValue(string text, int defaultValue)
        {
            int value;
            if (String.IsNullOrEmpty(text) || !Int32.TryParse(text, out value)) return defaultValue;
            return value;
        }
        private static long? ParseResponseValue(string text, long? defaultValue)
        {
            long value;
            if (String.IsNullOrEmpty(text) || !Int64.TryParse(text, out value)) return defaultValue;
            return value;
        }
        private static DateTime? ParseResponseValue(string text, DateTime? defaultValue)
        {
            if (String.IsNullOrEmpty(text)) return defaultValue;
            char[] separators = new char[] { ',', ' ', ':' };

            string[] items = text.Split(separators, StringSplitOptions.RemoveEmptyEntries);  // Fri, 07 Nov 2014 13:14:55 GMT
            DateTime? value = null;
            if (items.Length == 8)
            {
                value = ParseResponseDT8(items);
                if (value.HasValue) return value;
            }

            return defaultValue;
        }

        private static DateTime? ParseResponseDT8(string[] items)
        {
            // Fri, 07 Nov 2014 13:14:55 GMT
            //  0    1  2    3   4  5  6  7 
            int day = ParseResponseValue(items[1], -1);
            int month = ParseResponseMonth(items[2], -1);
            int year = ParseResponseValue(items[3], -1);
            int hour = ParseResponseValue(items[4], -1);
            int minute = ParseResponseValue(items[5], -1);
            int second = ParseResponseValue(items[6], -1);
            bool isGmt = (items[7] == "GMT");
            bool isUtc = (items[7] == "UTC");
            if (InRange(month, 1, 12) && InRange(year, 1990, 2100) && InRange(hour, 0, 23) && InRange(minute, 0, 59) && InRange(second, 0, 59) && InRange(day, 1, (DateTime.DaysInMonth(year, month))))
            {
                DateTime value = new DateTime(year, month, day, hour, minute, second);
                if (isGmt || isUtc)
                    value = value.ToLocalTime();
                return value;
            }

            return null;
        }

        private static int ParseResponseMonth(string text, int defaultValue)
        {
            string key = (String.IsNullOrEmpty(text) ? "" : text.Trim().ToLower());
            if (key.Length > 3) key = key.Substring(0, 3);
            switch (key)
            {
                case "jan": return 1;
                case "feb": return 2;
                case "mar": return 3;
                case "apr": return 4;
                case "may": return 5;
                case "jun": return 6;
                case "jul": return 7;
                case "aug": return 8;
                case "sep": return 9;
                case "oct": return 10;
                case "nov": return 11;
                case "dec": return 12;
            }
            return defaultValue;
        }
        private static bool InRange(int value, int min, int max)
        {
            return (value >= min && value <= max);
        }
        /// <summary>
        /// Nastaví datum souboru podle informací z Response, pokud je máme
        /// </summary>
        private void SetFileDateByResponse()
        {
            if (!this.WebResponseLastModified.HasValue) return;
            try
            {
                FileInfo fi = new FileInfo(this.LocalFile);
                if (fi.Exists)
                    fi.LastWriteTime = this.WebResponseLastModified.Value;
            }
            catch { }
        }
        #endregion
        #region CreateLocalPath
        /// <summary>
        /// Vytvoří lokální jméno souboru včetně plné cesty pro daný soubor URL, v defaultním adresáři aplikace
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string CreateLocalPath(string url)
        {
            string rootPath;
            // rootPath = Path.GetDirectoryName(System.Windows.Forms.Application.CommonAppDataPath);
            // rootPath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Djs", "WebDownloader", "Data");
            rootPath = Path.Combine(Path.GetDirectoryName(typeof(DownloadItem).Assembly.Location), "Data");
            return CreateLocalPath(url, rootPath);
        }
        /// <summary>
        /// Vytvoří lokální jméno souboru včetně plné cesty pro daný soubor URL, v dané základní cestě
        /// </summary>
        /// <param name="url"></param>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public static string CreateLocalPath(string url, string rootPath)
        {
            if (String.IsNullOrEmpty(url)) return rootPath;
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return null;
            string file = uri.GetComponents(UriComponents.Host | UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.Unescaped);
            file = file.Replace("/", "\\");
            return Path.Combine(rootPath, file);
        }
        protected static bool ExistDirectory(string localFile)
        {
            string path = Path.GetDirectoryName(localFile);
            if (Directory.Exists(path)) return true;

            Directory.CreateDirectory(path);
            if (Directory.Exists(path)) return true;
            return false;
        }
        #endregion
    }
    /// <summary>
    /// Stav procesu downloadu
    /// </summary>
    public enum DownloadItemState
    {
        /// <summary>
        /// Objekt vytvořen, nezahájil práci, je možno startovat.
        /// </summary>
        Initiated,
        /// <summary>
        /// Objekt je vybrán k tomu, aby zahájil práci. Ještě neprobíhá download, ale brzo bude.
        /// </summary>
        Blocked,
        /// <summary>
        /// Probíhá stahování, nelze zahájit další práci.
        /// </summary>
        Working,
        /// <summary>
        /// Dokončeno bez chyb. Je možno zadat další stahování.
        /// </summary>
        Done,
        /// <summary>
        /// Stahování bylo zrušeno. Je možno zadat další stahování.
        /// </summary>
        Cancelled,
        /// <summary>
        /// Dokončeno s chybou, chyba je v this.Error. Je možno zadat další stahování.
        /// </summary>
        Error
    }
    #region Delegáty a EventArgs
    public class DownloadItemStateChangedArgs : EventArgs
    {
        public DownloadItemStateChangedArgs(DownloadItem item)
        {
            this.Item = item;
        }
        public DownloadItem Item { get; private set; }
    }
    public delegate void DownloadItemStateChangedHandler(object sender, DownloadItemStateChangedArgs args);
    #endregion
    #endregion
    #region UI
    public class WebDownloadPanel : WebPanel
    {
        #region Konstrukce
        public WebDownloadPanel()
        {
            this.DataInit();
            this.Init();
        }
        protected void Init()
        {
            AnchorStyles ab = AnchorStyles.Left | AnchorStyles.Bottom;
            this.SuspendLayout();

            int tabIndex = 0;
            int x = 14;
            this._WebGrid = new WebGrid(new Rectangle(x, 9, 819 - x, 144), ref tabIndex) { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
            this._FileCntLbl = new WebLabel("Přeneseno souborů:", new Rectangle(x - 3, 160, 129, 16), ref tabIndex) { Anchor = ab };
            this._FileCntTxt = new WebText(new Rectangle(x, 179, 119, 24), ref tabIndex) { Enabled = false, TextAlign = HorizontalAlignment.Right, Anchor = ab };
            this._FileSizeTxt = new WebText(new Rectangle(x, 204, 87, 24), ref tabIndex) { Enabled = false, TextAlign = HorizontalAlignment.Right, Anchor = ab };
            this._FileSizeLbl = new WebLabel("MB", new Rectangle(x + 93, 209, 28, 16), ref tabIndex) { Anchor = ab };
            this._BandKbsTxt = new WebText(new Rectangle(x, 229, 67, 24), ref tabIndex) { Enabled = false, TextAlign = HorizontalAlignment.Right, Anchor = ab };
            this._BandKbsLbl = new WebLabel("KB/sec", new Rectangle(x + 73, 234, 51, 16), ref tabIndex) { Anchor = ab };
            this._FileCntOkLbl = new WebLabel("Celkem dobrých:", new Rectangle(x + 153, 160, 109, 16), ref tabIndex) { Anchor = ab };
            this._FileCntOkTxt = new WebText(new Rectangle(x + 156, 179, 119, 24), ref tabIndex) { Enabled = false, TextAlign = HorizontalAlignment.Right, Anchor = ab };
            this._FileCurrOkLbl = new WebLabel("Počet dobrých v řadě:", new Rectangle(x + 153, 210, 139, 16), ref tabIndex) { Anchor = ab };
            this._FileCurrOkTxt = new WebText(new Rectangle(x + 156, 229, 119, 24), ref tabIndex) { Enabled = false, TextAlign = HorizontalAlignment.Right, Anchor = ab };
            this._FileCntErLbl = new WebLabel("Celkem chyb:", new Rectangle(x + 312, 160, 89, 16), ref tabIndex) { Anchor = ab };
            this._FileCntErTxt = new WebText(new Rectangle(x + 315, 179, 119, 24), ref tabIndex) { Enabled = false, TextAlign = HorizontalAlignment.Right, Anchor = ab };
            this._FileCurrErLbl = new WebLabel("Počet chyb v řadě:", new Rectangle(x + 312, 210, 119, 16), ref tabIndex) { Anchor = ab };
            this._FileCurrErTxt = new WebText(new Rectangle(x + 315, 229, 119, 24), ref tabIndex) { Enabled = false, TextAlign = HorizontalAlignment.Right, Anchor = ab };
            this._AutoEndLbl = new WebLabel("Automaticky skončit po počtu chyb v řadě:", new Rectangle(x + 464, 160, 257, 16), ref tabIndex) { Anchor = ab };
            this._AutoEndTxt = new WebNumeric(1, 99999999L, new Rectangle(x + 467, 179, 95, 24), ref tabIndex) { Anchor = ab };

            ((System.ComponentModel.ISupportInitialize)(this._AutoEndTxt)).BeginInit();

            this.Controls.Add(this._WebGrid);
            this.Controls.Add(this._FileCntLbl);
            this.Controls.Add(this._FileCntTxt);
            this.Controls.Add(this._FileSizeTxt);
            this.Controls.Add(this._FileSizeLbl);
            this.Controls.Add(this._BandKbsTxt);
            this.Controls.Add(this._BandKbsLbl);
            this.Controls.Add(this._FileCntOkLbl);
            this.Controls.Add(this._FileCntOkTxt);
            this.Controls.Add(this._FileCurrOkLbl);
            this.Controls.Add(this._FileCurrOkTxt);
            this.Controls.Add(this._FileCntErLbl);
            this.Controls.Add(this._FileCntErTxt);
            this.Controls.Add(this._FileCurrErLbl);
            this.Controls.Add(this._FileCurrErTxt);
            this.Controls.Add(this._AutoEndLbl);
            this.Controls.Add(this._AutoEndTxt);

            this.CreateActionButton("START", ref tabIndex);

            this._RunBtn = new WebButton(Properties.Resources.media_playback_start_4,new Rectangle(831, 9 /*63*/, 45, 32), ref tabIndex) { Anchor = AnchTR, Visible = false };
            this._RunBtn.Click += new EventHandler(_RunBtn_Click);
            this.Controls.Add(this._RunBtn);

            this._PauseBtn = new WebButton(Properties.Resources.media_playback_pause_4, new Rectangle(881, 9, 45, 32), ref tabIndex) { Anchor = AnchTR, Visible = false };
            this._PauseBtn.Click += new EventHandler(_PauseBtn_Click);
            this.Controls.Add(this._PauseBtn);

            this._StopBtn = new WebButton(Properties.Resources.media_playback_stop_4, new Rectangle(931, 9, 45, 32), ref tabIndex) { Anchor = AnchTR, Visible = false };
            this._StopBtn.Click += new EventHandler(_StopBtn_Click);
            this.Controls.Add(this._StopBtn);

            this.Size = new System.Drawing.Size(993, 263);

            this.ShowButtonByState();

            ((System.ComponentModel.ISupportInitialize)(this._AutoEndTxt)).EndInit();

            this.ResumeLayout(false);
        }

        private WebGrid _WebGrid;
        private WebLabel _FileCntLbl;
        private WebText _FileCntTxt;
        private WebText _FileSizeTxt;
        private WebLabel _FileSizeLbl;
        private WebText _BandKbsTxt;
        private WebLabel _BandKbsLbl;
        private WebLabel _FileCntOkLbl;
        private WebText _FileCntOkTxt;
        private WebLabel _FileCurrOkLbl;
        private WebText _FileCurrOkTxt;
        private WebLabel _FileCntErLbl;
        private WebText _FileCntErTxt;
        private WebLabel _FileCurrErLbl;
        private WebText _FileCurrErTxt;
        private WebLabel _AutoEndLbl;
        private WebNumeric _AutoEndTxt;
        private WebButton _RunBtn;
        private WebButton _PauseBtn;
        private WebButton _StopBtn;
        /// <summary>
        /// Zobrazí buttony START, Run,Pause,Stop podle stavu downloaderu
        /// </summary>
        protected void ShowButtonByState()
        {
            DownloadState state = ((this.WebDownload == null) ? DownloadState.Initiated : this.WebDownload.State);
            this.ShowButtonByState(state);
        }
        protected void ShowButtonByState(DownloadState state)
        {
            bool showStart = false;
            bool canStart = false;
            bool showRun = false;
            bool canRun = false;
            bool showPause = false;
            bool canPause = false;
            bool showStop = false;
            bool canStop = false;
            switch (state)
            {
                case DownloadState.Initiated:
                    // Na začátku
                    showStart = true;
                    canStart = true;
                    break;
                case DownloadState.Working:
                    // Pracuje se
                    showRun = true;
                    canRun = false;
                    showPause = true;
                    canPause = true;
                    showStop = true;
                    canStop = true;
                    break;
                case DownloadState.Paused:
                    // Jsme v pauze
                    showRun = true;
                    canRun = true;
                    showPause = true;
                    canPause = false;
                    showStop = true;
                    canStop = true;
                    break;
                case DownloadState.Cancelling:
                    // Čekáme na doběhnutí a budeme cancelovat
                    showRun = true;
                    canRun = false;
                    showPause = true;
                    canPause = false;
                    showStop = true;
                    canStop = true;
                    break;
                case DownloadState.Cancelled:
                case DownloadState.Aborted:
                case DownloadState.StoppedDueErrors:
                case DownloadState.Done:
                    // Různé stavy po dokončení
                    showStart = true;
                    canStart = true;
                    break;
            }
            this.ShowButtonByState(this.ActionButton, showStart, canStart);
            this.ShowButtonByState(this._RunBtn, showRun, canRun);
            this.ShowButtonByState(this._PauseBtn, showPause, canPause);
            this.ShowButtonByState(this._StopBtn, showStop, canStop);
        }
        /// <summary>
        /// Na daném controlu nastaví Visible a Enabled podle parametrů, pokud je to třeba
        /// </summary>
        /// <param name="control"></param>
        /// <param name="visible"></param>
        /// <param name="enabled"></param>
        private void ShowButtonByState(Control control, bool visible, bool enabled)
        {
            if (control.Visible != visible)
                control.Visible = visible;
            if (control.Enabled != enabled)
                control.Enabled = enabled;
        }
        /// <summary>
        /// Událost, kdy se zahájit download
        /// </summary>
        public event EventHandler StartClick;
        protected override void OnActionButton()
        {
            if (this.StartClick != null)
                this.StartClick(this, EventArgs.Empty);
        }
        void _RunBtn_Click(object sender, EventArgs e)
        {
            this.WebDownload.DownloadResume();
        }
        void _PauseBtn_Click(object sender, EventArgs e)
        {
            this.WebDownload.DownloadPause();
        }
        void _StopBtn_Click(object sender, EventArgs e)
        {
            this.WebDownload.DownloadCancel();
        }
        #endregion
        #region Data
        private void DataInit()
        {
            this.WebDownload = new WebDownload();
            this.WebDownload.StateChanged += new DownloadStateChangedHandler(WebDownload_StateChanged);
        }
        /// <summary>
        /// Po změně stavu downloadu = reakce se projeví na buttonech
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void WebDownload_StateChanged(object sender, DownloadStateChangedArgs e)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action<DownloadStateChangedArgs>(this.AfterDownloadStateChanged), e);
            else
                this.AfterDownloadStateChanged(e);
        }
        protected void AfterDownloadStateChanged(DownloadStateChangedArgs e)
        {
            this.ShowButtonByState(e.StateCurrent);
        }
        /// <summary>
        /// Zahájí provádění downloadu souborů daných generátorem adres
        /// </summary>
        /// <param name="webAdress"></param>
        public void Start(WebAdress webAdress)
        {
            this.WebDownload.Start(webAdress);
        }
        private WebDownload WebDownload;
        #endregion
    }
    #region WebGrid 
    public class WebGrid : DataGridView
    {
        #region Konstrukce
        public WebGrid() { }
        public WebGrid(Rectangle bounds, ref int tabIndex)
        {
            this.Location = bounds.Location;
            this.Size = bounds.Size;
            this.TabIndex = tabIndex++;
            
            this.AllowUserToAddRows = false;
            this.AllowUserToDeleteRows = false;
            this.AllowUserToOrderColumns = false;
            this.AllowUserToResizeColumns = true;
            this.AllowUserToResizeRows = false;
            this.AutoGenerateColumns = false;
            this.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            this.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            this.ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable;
            this.ColumnHeadersHeight = 23;
            // this.FontHeight = 7;
            this.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.ColumnHeadersVisible = true;
            this.DoubleBuffered = true;
            this.EditMode = DataGridViewEditMode.EditProgrammatically;
            this.EnableHeadersVisualStyles = true;
            this.MultiSelect = false;
            this.RowHeadersVisible = false;
            this.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            this.Columns.Add(new WebGridColumn(this, "id", "Položka", 55, null, null));
            this.Columns.Add(new WebGridColumn(this, "size", "Velikost [B]", 85, null, null));
            this.Columns.Add(new WebGridColumn(this, "progress", "Postup [%]", 140, null, new WebGridProgressCell()));
            this.Columns.Add(new WebGridColumn(this, "band", "Rychlost [KB/s]", 95, null, null));
            this.Columns.Add(new WebGridColumn(this, "time", "Čas [sec]", 65, null, null));
            this.Columns.Add(new WebGridColumn(this, "url", "Adresa", 800, 800f, null));

            this.WebRows = new List<WebGridRow>();
            /*
            this.AddWebRow(0);
            this.AddWebRow(1);
            this.AddWebRow(2);
            this.AddWebRow(3);
            */
        }

        private WebGridRow AddWebRow(int id)
        {
            WebGridRow row = new WebGridRow(this, id);
            this.Rows.Add(row);
            this.WebRows.Add(row);
            return row;
        }
        /// <summary>
        /// Soupis řádků v Gridu
        /// </summary>
        public List<WebGridRow> WebRows { get; private set; }
        #endregion
        #region Zobrazování dat
        /// <summary>
        /// Zobrazí data z dané položky downloadu.
        /// Pokud download neběží, pak řádek vyprázdní.
        /// </summary>
        /// <param name="item"></param>
        public void ShowItem(DownloadItem item)
        {
            bool working = (item.State == DownloadItemState.Working);
            WebGridRow row = this.SearchRow(item.Id, working);
            if (working)
                row.FillFrom(item);
            else if (!working && row != null)
                row.Clear();
        }
        /// <summary>
        /// Najde a vrátí řádek pro dané ID položky.
        /// Pokud nenajde, a musí existovat, pak buď obsadí existující prázdný, nebo založí nový.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="mustExists"></param>
        /// <returns></returns>
        private WebGridRow SearchRow(int itemId, bool mustExists)
        {
            WebGridRow row = this.WebRows.FirstOrDefault(r => r.ItemId.HasValue && r.ItemId.Value == itemId);
            if (row == null && mustExists)
            {
                row = this.WebRows.FirstOrDefault(r => r.IsEmpty);
                if (row == null && mustExists)
                {
                    int id = this.WebRows.Select(r => r.Id).Max();
                    row = this.AddWebRow(id + 1);
                }
            }
            return row;
        }
        /// <summary>
        /// Smaže řádky
        /// </summary>
        public void ClearData()
        {
            this.Rows.Clear();
            this.WebRows.Clear();
        }
        #endregion

    }
    public class WebGridColumn : DataGridViewColumn
    {
        public WebGridColumn(WebGrid grid, string name, string text, int width, float? fill, DataGridViewCell template)
        {
            this.WebGrid = grid;
            this.Name = name;
            this.HeaderText = text;
            this.SortMode = DataGridViewColumnSortMode.NotSortable;
            this.Width = width;
            this.AutoSizeMode = (fill.HasValue ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None);
            this.FillWeight = (fill.HasValue ? fill.Value : (float)width);
            this.CellTemplate = (template != null ? template : new WebGridTextCell());
        }
        protected WebGrid WebGrid { get; private set; }
    }
    public class WebGridRow : DataGridViewRow
    {
        public WebGridRow() { }
        public WebGridRow(WebGrid grid, int id)
        {
            this.WebGrid = grid;
            this.Id = id;
            this.Height = 16;
            this.CreateCells(grid);

            this.Cells[0].Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            this.Cells[1].Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            this.Cells[2].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.Cells[3].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.Cells[4].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.Cells[5].Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }
        /// <summary>
        /// Vyprázdní řádek
        /// </summary>
        public void Clear()
        {
            this.ItemId = null;
            this.ItemSize = null;
            this.ItemProgress = null;
            this.ItemSpeed = null;
            this.ItemTime = null;
            this.ItemUrl = null;
        }
        /// <summary>
        /// Naplní všechny položky řádku daty z dodaného objektu, který popisuje stahování dat.
        /// Musí být voláno z GUI threadu (sám si invokaci nezajistí).
        /// </summary>
        /// <param name="item"></param>
        public void FillFrom(DownloadItem item)
        {
            double? ratio = null;
            if (item.TotalBytesToReceive > 0L) ratio = (double)((decimal)item.BytesReceived / (decimal)item.TotalBytesToReceive);
            else if (item.ProgressPercentage > 0) ratio = (double)(((decimal)item.ProgressPercentage) / 100m);
            string toolTip = "";

            this.ItemId = item.Id;
            this.ItemSize = item.TotalBytesToReceive;
            this.ItemProgress = ratio;
            this.ItemSpeed = (item.BandKBsData > 0m ? (double?)item.BandKBsData : (double?)null);
            this.ItemTime = item.DownloadTime;
            this.ItemUrl = item.Url;
        }
        /// <summary>
        /// Owner
        /// </summary>
        protected WebGrid WebGrid { get; private set; }
        /// <summary>
        /// true když řádek nezobrazuje data, je empty
        /// </summary>
        public bool IsEmpty { get { return !this.ItemId.HasValue; } }
        /// <summary>
        /// ID řádku Gridu, konstantní hodnota
        /// </summary>
        public int Id { get; private set; }
        /// <summary>
        /// ID souboru downloadu, který se zde aktuálně zobrazuje. Null pokud je empty
        /// </summary>
        public int? ItemId { get { return this._ItemId; } set { this._ItemId = value; this.Cells[0].Value = NullableToString(value); } } private int? _ItemId;
        /// <summary>
        /// Délka dat ke stažení. Null pokud je empty
        /// </summary>
        public long? ItemSize { get { return this._ItemSize; } set { this._ItemSize = value; this.Cells[1].Value = NullableToString(value); } } private long? _ItemSize;
        /// <summary>
        /// Progres v rozmezí 0 až 1. Null pokud je empty
        /// </summary>
        public double? ItemProgress { get { return this._ItemProgress; } set { this._ItemProgress = value; this.Cells[2].Value = value; } } private double? _ItemProgress;
        /// <summary>
        /// Rychlost stahování v KB/sec. Null pokud je empty
        /// </summary>
        public double? ItemSpeed { get { return this._ItemSpeed; } set { this._ItemSpeed = value; this.Cells[3].Value = NullableToString(value); } } private double? _ItemSpeed;
        /// <summary>
        /// Počet sekund stahování v sec. Null pokud je empty
        /// </summary>
        public TimeSpan? ItemTime { get { return this._ItemTime; } set { this._ItemTime = value; this.Cells[4].Value = NullableToString(value); } } private TimeSpan? _ItemTime;
        /// <summary>
        /// Počet sekund stahování v sec. Null pokud je empty
        /// </summary>
        public string ItemUrl { get { return this._ItemUrl; } set { this._ItemUrl = value; this.Cells[5].Value = value; } } private string _ItemUrl;

        private static string NullableToString(Int32? value)
        {
            if (!value.HasValue) return "";
            return value.Value.ToString("### ### ### ##0").Trim();
        }
        private static string NullableToString(Int64? value)
        {
            if (!value.HasValue) return "";
            return value.Value.ToString("### ### ### ##0").Trim();
        }
        private static string NullableToString(Double? value)
        {
            if (!value.HasValue) return "";
            return Math.Round(value.Value, 1).ToString("### ### ### ##0.0").Trim();
        }
        private static string NullableToString(TimeSpan? value)
        {
            if (!value.HasValue) return "";
            return value.Value.ToString("mm:ss.f");
        }
    }
    public class WebGridTextCell : DataGridViewTextBoxCell
    {
        public WebGridTextCell()
        {
        }
    }
    public class WebGridProgressCell : DataGridViewTextBoxCell
    {
        public WebGridProgressCell()
        {
            this.Value = "";
        }
        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates cellState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
        {
            value = "";
            formattedValue = "";
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, 
                value, formattedValue, errorText, 
                cellStyle, advancedBorderStyle, paintParts);

            if (this.Value is double)
            {
                double progressValue = (double)this.Value;
                progressValue = (progressValue < 0d ? 0d : (progressValue > 1d ? 1d : progressValue));
                using (LinearGradientBrush lgb = new LinearGradientBrush(cellBounds,
                    Color.FromArgb(255, 255, 255),
                    Color.FromArgb(96, 96, 255),
                    90f))
                {
                    Rectangle b = cellBounds;
                    b.Inflate(-1, -3);
                    b.Width = (int)Math.Round((double)b.Width * progressValue, 0);
                    graphics.FillRectangle(lgb, b);
                }

                string text = Math.Round(progressValue * 100d, 1).ToString("##0.0").Trim() + "%";
                using (Font font = new Font("Ariel", 7f, FontStyle.Regular))
                {
                    Size size = Size.Ceiling(graphics.MeasureString(text, font));
                    Point point = new Point(cellBounds.X + (cellBounds.Width - size.Width) / 2, cellBounds.Y + (cellBounds.Height - size.Height) / 2);
                    graphics.DrawString(text, font, Brushes.Black, point);
                }
            }
        }
    }
    #endregion
    #endregion
}
