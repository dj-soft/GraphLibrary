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
        /// <param name="recordGId">Identifikátor záznamu</param>
        void RunOpenRecordForm(GId recordGId);
        /// <summary>
        /// Metoda, která zajistí provedení dané funkce
        /// </summary>
        /// <param name="args"></param>
        void RunContextFunction(RunContextFunctionArgs args);
    }
    #endregion
    public class RunContextFunctionArgs
    {
        public ItemActionArgs GraphItemArgs { get; set; }
        public string MenuItemText { get; set; }
    }
}
