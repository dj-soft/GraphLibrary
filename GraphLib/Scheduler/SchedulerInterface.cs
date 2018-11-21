using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;
using Asol.Tools.WorkScheduler.Services;
using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Components.Graph;
using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler
{
    #region interface IAppHost : požadavky na objekt, který hraje roli hostitele Scheduleru.
    /// <summary>
    /// interface IAppHost : požadavky na objekt, který hraje roli hostitele Scheduleru.
    /// Jeho úkolem je implementovat metody, které zajišťují potřebné služby pro Scheduler v prostředí, ve kterém je hostitel doma.
    /// Typicky: otevřít formulář záznamu, spustit funkci...
    /// </summary>
    public interface IAppHost
    {
        /// <summary>
        /// Metoda, která zajistí provedení akce na aplikačním serveru.
        /// Touto cestou se řeší všechny akce: Otevření formuláře, Vyvolání funkce z toolbaru, Vyvolání kontextové funkce, Změna grafického prvku, ..., Zavření okna.
        /// Požadavek se může provádět asynchronně.
        /// Po jeho doběhnutí se volá metoda <see cref="AppHostRequestArgs.CallBackAction"/>.
        /// </summary>
        /// <param name="args">Data pro funkci</param>
        void CallAppHostFunction(AppHostRequestArgs args);
    }
    #endregion
    #region Argumenty metod IAppHost
    /// <summary>
    /// AppHostRequestArgs : Třída pro argument metody <see cref="IAppHost.CallAppHostFunction(AppHostRequestArgs)"/>
    /// </summary>
    public class AppHostRequestArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="sessionId">SessionId dat</param>
        /// <param name="request">Data pro požadavek. Předává se do aplikační funkce. 
        /// Jde o instanci třídy <see cref="GuiRequest"/>.
        /// Konkrétní požadavek je specifikován v <see cref="GuiRequest.Command"/>, jeho hodnota pochází z konstant {<see cref="GuiRequest.COMMAND_OpenRecords"/> atd}.</param>
        /// <param name="userData">Libovolná uživatelská data, která si připraví GUI v místě, kde vzniká požadavek; a která následně vyhodnotí v místě, kde se zpracovává odpověď. 
        /// Nepředává se do aplikační funkce.</param>
        /// <param name="callBackAction">Metoda, která bude zavolána po doběhnutí požadavku.
        /// Požadavek se zpracovává asynchronně, odpověď (response) přijde v jiném threadu, a do threadu GUI je invokována.
        /// Tato metoda dostává data ve formě <see cref="AppHostResponseArgs"/>, součástí dat je i původní požadavek (zdejší argument request) a v něm tedy i přidaná data (zdejší argument userData).
        /// </param>
        public AppHostRequestArgs(int? sessionId, GuiRequest request, object userData = null, Action<AppHostResponseArgs> callBackAction = null)
        {
            this.SessionId = sessionId;
            this.Request = request;
            this.UserData = userData;
            this.CallBackAction = callBackAction;
            this.OriginalCallBackAction = callBackAction;
        }
        /// <summary>
        /// SessionId dat
        /// </summary>
        public int? SessionId { get; protected set; }
        /// <summary>
        /// Data pro požadavek. 
        /// Zde je uložena standardní instance.
        /// </summary>
        public GuiRequest Request { get; protected set; }
        /// <summary>
        /// Data pro požadavek. 
        /// Zde je uložena serializovaná forma instance GuiRequest.
        /// Předává se do aplikační funkce.
        /// </summary>
        public string Data { get { if (this._Data == null) this._Data = Persist.Serialize(this.Request, PersistArgs.Compressed); return this._Data; } }
        private string _Data;
        /// <summary>
        /// Libovolná uživatelská data, která si připraví GUI v místě, kde vzniká požadavek; a která následně vyhodnotí v místě, kde se zpracovává odpověď. 
        /// Nepředává se do aplikační funkce.
        /// </summary>
        public object UserData { get; protected set; }
        /// <summary>
        /// CallBackAction : metoda v GUI, kterou zavolá objekt <see cref="IAppHost"/> po doběhnutí funkce.
        /// Pokud je null, pak <see cref="IAppHost"/> výsledky funkce neřeší (není komu je předat).
        /// Pokud je <see cref="CallBackAction"/> zadán, pak <see cref="IAppHost"/> vyhodnotí výsledky běhu, zabalí je do <see cref="AppHostResponseArgs"/> 
        /// a vše předá do metody <see cref="CallBackAction"/>.
        /// </summary>
        public Action<AppHostResponseArgs> CallBackAction { get; set; }
        /// <summary>
        /// OriginalCallBackAction : originální <see cref="CallBackAction"/> metoda, kterou předal uživatelský kód.
        /// </summary>
        public Action<AppHostResponseArgs> OriginalCallBackAction { get; protected set; }
    }
    /// <summary>
    /// Třída zachycující výsledky, s jakými byl dokončen konkrétní požadavek.
    /// </summary>
    public class AppHostResponseArgs
    {
        /// <summary>
        /// Konstruktor.
        /// Povinně musí dostat požadavek, ke kterému se tato Response vztahuje
        /// </summary>
        /// <param name="request"></param>
        public AppHostResponseArgs(AppHostRequestArgs request)
        {
            this.Request = request;
        }
        /// <summary>
        /// Data o požadavku, ke kterému se vztahuje this odpověď.
        /// </summary>
        public AppHostRequestArgs Request { get; private set; }
        /// <summary>
        /// Výsledek běhu akce.
        /// </summary>
        public AppHostActionResult Result { get; set; }
        /// <summary>
        /// Chybová nebo Warningová hláška. 
        /// Pokud <see cref="Result"/> je <see cref="AppHostActionResult.Success"/>, pak je zde prázdný string.
        /// Tato hláška obsahuje i kód hlášky na začátku, typicky: "[RecordNotFound] Záznam xxx nebyl nalezen."
        /// Naproti tomu property <see cref="UserMessage"/> obsahuje hlášku bez tohoto kódu chyby.
        /// </summary>
        public string FullMessage { get; set; }
        /// Chybová nebo Warningová hláška. 
        /// Pokud <see cref="Result"/> je <see cref="AppHostActionResult.Success"/>, pak je zde prázdný string.
        /// Tato hláška NEOBSAHUJE kód hlášky na začátku, její obsah je určen přímo pro uživatele, např.: "Záznam xxx nebyl nalezen."
        /// Naproti tomu property <see cref="FullMessage"/> obsahuje hlášku VČETNĚ kódu chyby.
        public string UserMessage { get; set; }
        /// <summary>
        /// Výpis zásobníku po chybě (<see cref="AppHostActionResult.EndWithError"/>).
        /// </summary>
        public string StackTrace { get; set; }
        /// <summary>
        /// Data, která vrátila aplikační funkce. 
        /// Prázdný string = funkce doběhla, ale nevrátila data.
        /// Null = funkce nedoběhla (<see cref="Result"/> je buď <see cref="AppHostActionResult.EndWithError"/> nebo <see cref="AppHostActionResult.Failure"/>).
        /// Aplikační funkce ukládá svoje výstupní data do NrsCowley.ServiceGateOutputUserData, 
        /// ukládá tam zazipovaný text obsahující serializovanou formu objektu <see cref="GuiResponse"/>.
        /// Deserializaci a vyhodnocení si provádí metoda <see cref="AppHostRequestArgs.CallBackAction"/>.
        /// </summary>
        public string Data
        {
            get { return this._Data; }
            set
            {
                this._Data = value;
                this._GuiResponse = null;
                this._GuiResponseDeserialized = false;
            }
        }
        /// <summary>
        /// Data z aplikační funkce, již deserializovaná
        /// </summary>
        public GuiResponse GuiResponse
        {
            get
            {
                if (this._GuiResponse == null && this._Data != null && !this._GuiResponseDeserialized)
                {
                    this._GuiResponseDeserialized = true;
                    this._GuiResponse = Persist.Deserialize(this._Data) as GuiResponse;
                }
                return this._GuiResponse;
            }
            set
            {
                this._GuiResponse = value;
                this._Data = (this._GuiResponse != null ? Persist.Serialize(this._GuiResponse, PersistArgs.Compressed) : null);
                this._GuiResponseDeserialized = true;
            }
        }
        /// <summary>
        /// Stringová data odpovědi, budou deserializována do <see cref="_GuiResponse"/>.
        /// </summary>
        private string _Data;
        /// <summary>
        /// Strukturovaná data odpovědi ve formě <see cref="GuiResponse"/>
        /// </summary>
        private GuiResponse _GuiResponse;
        /// <summary>
        /// Příznak, že již jednou proběhl pokus o deserializaci dat z <see cref="_Data"/> do <see cref="_GuiResponse"/>.
        /// Opakování deserializace nemá význam.
        /// </summary>
        private bool _GuiResponseDeserialized;
    }
    /// <summary>
    /// Stavy, jak může požadavek skončit
    /// </summary>
    public enum AppHostActionResult
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None = 0,
        /// <summary>
        /// Úspěch bez chyb
        /// </summary>
        Success,
        /// <summary>
        /// Úspěch s varováním
        /// </summary>
        SuccessWithWarning,
        /// <summary>
        /// Chyba, kterou hlásí aplikační funkce, ale přesto pokračovala a doběhla
        /// </summary>
        EndWithError,
        /// <summary>
        /// Chyba, kdy aplikační funkce skončila chybou a dál neběžela
        /// </summary>
        Failure,
        /// <summary>
        /// Selhání, kdy nedošlo ani ke spuštění funkce
        /// </summary>
        NotResponse
    }
    #endregion
}
