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
        /// Touto cestou se řeší všechny akce: Otevření formuláře, Vyvolání funkce z toolbaru, Vyvolání kontextové funkce, Změna grafického prvku, ...
        /// </summary>
        /// <param name="args">Data pro funkci</param>
        void CallAppHostFunction(AppHostRequestArgs args);
    }
    #endregion
    #region class AppHostCommand : Konstanty = příkazy (Command) pro IAppHost.CallAppHostFunction
    /// <summary>
    /// AppHostCommand : Konstanty = příkazy (Command) pro <see cref="IAppHost.CallAppHostFunction(AppHostRequestArgs)"/>
    /// </summary>
    public class AppHostCommand
    {
        /// <summary>
        /// Otevřít záznamy.
        /// Data v <see cref="AppHostRequestArgs.Data"/> obsahují instanci <see cref="GuiRequest"/>, kde je naplněna property <see cref="GuiRequest.RecordsToOpen"/>
        /// </summary>
        public const string OpenRecords = "OpenRecords";
        /// <summary>
        /// Bylo kliknuto na tlačítko Toolbaru, které obsahuje jednoduchou funkci.
        /// Data v <see cref="AppHostRequestArgs.Data"/> obsahují instanci <see cref="GuiRequest"/>, kde je naplněna property <see cref="GuiRequest.ToolbarItem"/> 
        /// </summary>
        public const string ToolbarClick = "ToolbarClick";
        /// <summary>
        /// Bylo kliknuto na kontextovou funkci.
        /// Data v <see cref="AppHostRequestArgs.Data"/> obsahují instanci <see cref="GuiRequest"/>, kde je naplněna property <see cref="GuiRequest.ToolbarItem"/> 
        /// </summary>
        public const string ContextMenuClick = "ContextMenuClick";


        /// <summary>
        /// Test před zavřením okna.
        /// Předává se pouze hodnota <see cref="AppHostRequestArgs.SessionId"/>, ale žádná data.
        /// Jako odpověď se očekává <see cref="GuiResponse"/>
        /// </summary>
        public const string QueryCloseWindow = "QueryCloseWindow";
        /// <summary>
        /// Zavírá se okno už doopravdy.
        /// Předává se pouze hodnota <see cref="AppHostRequestArgs.SessionId"/>, ale žádná data.
        /// Vyšší aplikace si má zahodit svoje data svázaná s tímto pluginem.
        /// </summary>
        public const string CloseWindow = "CloseWindow";
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
        /// <param name="command">Požadovaná akce, typicky některá z konstant v <see cref="AppHostCommand"/>.</param>
        /// <param name="data">Informace o požadavku, například button / kontextová funkce, plus data o stavu GUI. Předává se do aplikační funkce.</param>
        /// <param name="userData">Libovolná uživatelská data, která si připraví GUI v místě, kde vzniká požadavek; a která následně vyhodnotí v místě, kde se zpracovává odpověď. Nepředává se do aplikační funkce.</param>
        /// <param name="callBackAction"></param>
        public AppHostRequestArgs(int? sessionId, string command, object data, object userData = null, Action<AppHostResponseArgs> callBackAction = null)
        {
            this.SessionId = sessionId;
            this.Command = command;
            this.Data = Persist.Serialize(data);
            this.UserData = userData;
            this.CallBackAction = callBackAction;
        }
        /// <summary>
        /// SessionId dat
        /// </summary>
        public int? SessionId { get; protected set; }
        /// <summary>
        /// Command : příkaz (typ akce), typicky některá z konstant v <see cref="AppHostCommand"/>.
        /// </summary>
        public string Command { get; protected set; }
        /// <summary>
        /// Data : informace o požadavku, například button / kontextová funkce, plus data o stavu GUI. 
        /// Předává se do aplikační funkce.
        /// </summary>
        public string Data { get; protected set; }
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
        public Action<AppHostResponseArgs> CallBackAction { get; protected set; }
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
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Výpis zásobníku po chybě (<see cref="AppHostActionResult.Error"/>).
        /// </summary>
        public string StackTrace { get; set; }
        /// <summary>
        /// Data, která vrátila aplikační funkce. 
        /// Prázdný string = funkce doběhla, ale nevrátila data.
        /// Null = funkce nedoběhla (<see cref="Result"/> je buď <see cref="AppHostActionResult.Error"/> nebo <see cref="AppHostActionResult.Failure"/>).
        /// Aplikační funkce ukládá svoje výstupní data do NrsCowley.ServiceGateOutputUserData, 
        /// ukládá tam zazipovaný text obsahující serializovanou formu objektu <see cref="GuiResponse"/>.
        /// Deserializaci a vyhodnocení si provádí metoda <see cref="AppHostRequestArgs.CallBackAction"/>.
        /// </summary>
        public string Data { get; set; }
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
        /// Chyba, kterou hlásí aplikační funkce
        /// </summary>
        Error,
        /// <summary>
        /// Selhání, kdy nedošlo ani ke spuštění funkce
        /// </summary>
        Failure
    }
    #endregion
}
