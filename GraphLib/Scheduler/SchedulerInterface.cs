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
        /// Metoda, která zajistí otevření formuláře daného záznamu
        /// </summary>
        /// <param name="args">Data pro funkci</param>
        void RunOpenRecordsForm(RunOpenRecordsArgs args);
        /// <summary>
        /// Metoda, která zajistí provedení dané funkce vyvolané z Toolbaru
        /// </summary>
        /// <param name="args">Data pro funkci</param>
        void RunToolBarFunction(RunToolbarFunctionArgs args);
        /// <summary>
        /// Metoda, která zajistí vyvolání akce ToolBar.SelectedChange vyvolané z Toolbaru
        /// </summary>
        /// <param name="args">Data pro funkci</param>
        void RunToolBarSelectedChange(RunToolbarFunctionArgs args);
        /// <summary>
        /// Metoda, která zajistí provedení dané funkce vyvolané z Kontextového menu
        /// </summary>
        /// <param name="args">Data pro funkci</param>
        void RunContextFunction(RunContextFunctionArgs args);
    }
    #endregion
    #region Argumenty metod IAppHost
    /// <summary>
    /// Třída obsahující záznamy k otevření
    /// </summary>
    public sealed class RunOpenRecordsArgs : RunAppHostArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="sessionId">SessionId dat</param>
        /// <param name="guiIds">Soupis záznamů, které chce plugin otevřít</param>
        public RunOpenRecordsArgs(int? sessionId, IEnumerable<GuiId> guiIds) : base (sessionId)
        {
            this.GuiIds = guiIds;
        }
        /// <summary>
        /// Soupis záznamů, které chce plugin otevřít
        /// </summary>
        public IEnumerable<GuiId> GuiIds { get; private set; }
    }
    /// <summary>
    /// Třída popisující funkci toolbaru, která má být spuštěna
    /// </summary>
    public sealed class RunToolbarFunctionArgs : RunAppHostArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="sessionId">SessionId dat</param>
        /// <param name="guiToolbarItem">Funkce toolbaru, která se má spustit</param>
        public RunToolbarFunctionArgs(int? sessionId, GuiToolbarItem guiToolbarItem) : base (sessionId)
        {
            this.GuiToolbarItem = guiToolbarItem;
        }
        /// <summary>
        /// Funkce toolbaru, která se má spustit
        /// </summary>
        public GuiToolbarItem GuiToolbarItem { get; private set; }
    }
    /// <summary>
    /// Třída popisující funkci kontextového menu, která má být spuštěna
    /// </summary>
    public sealed class RunContextFunctionArgs : RunAppHostArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="sessionId">SessionId dat</param>
        /// <param name="guiContextMenuItem">Kontextová funkce, která se má spustit</param>
        public RunContextFunctionArgs(int? sessionId, GuiContextMenuItem guiContextMenuItem) : base (sessionId)
        {
            this.GuiContextMenuItem = guiContextMenuItem;
        }
        /// <summary>
        /// Kontextová funkce, která se má spustit
        /// </summary>
        public GuiContextMenuItem GuiContextMenuItem { get; private set; }
        public ItemActionArgs GraphItemArgs { get; set; }
    }
    /// <summary>
    /// Bázová třída pro argumenty metod IAppHost
    /// </summary>
    public abstract class RunAppHostArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="sessionId">SessionId dat</param>
        public RunAppHostArgs(int? sessionId)
        {
            this.SessionId = sessionId;
        }
        /// <summary>
        /// SessionId dat
        /// </summary>
        public int? SessionId { get; protected set; }
    }
    #endregion
}
