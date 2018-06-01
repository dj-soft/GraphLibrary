using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Services;
using System.Drawing;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    #region Request a Response pro získání deklarace tabulek, použitých v Scheduleru
    public class DataSourceGetTablesRequest : DataSourceRequest
    {
        public DataSourceGetTablesRequest(Data.ProgressData progressData)
            : base(progressData)
        {
        }
    }
    public class DataSourceGetTablesResponse : DataSourceResponse
    {
        public DataSourceGetTablesResponse(DataSourceGetTablesRequest request)
            : base(request)
        { }
        /// <summary>
        /// Titulek panelu, bude použit pokud bude existovat více zdrojů dat
        /// </summary>
        public Localizable.TextLoc Title { get; set; }
        /// <summary>
        /// Ikona panelu, bude použita pokud bude existovat více zdrojů dat
        /// </summary>
        public Image Image { get; set; }
        /// <summary>
        /// Tabulky zobrazené v levém panelu, typicky podklady k zaplánování (výrobní příkazy, a jiné úkoly).
        /// Tyto tabulky na sobě nejsou závislé, jen se střídavě zobrazují v jednom prostoru, opatřeném TabHeaderem (pokud je jich více).
        /// Pokud bude toto pole null nebo prázdné, nebudou se podklady nabízet .
        /// </summary>
        public Table[] TaskTables { get; set; }
        /// <summary>
        /// Tabulky zobrazené v prostředním = hlavním Gridu, reprezentují hlavní plánovací pole.
        /// Typicky mají být dvě, přičemž první reprezentuje pracoviště, a do druhé se promítají další zdroje (vybrané v tabulkce zdrojů ).
        /// Tyto tabulky sdílí jeden Grid, mají tedy splečný layout sloupců, a očekává se že v jednom (posledním) sloupci budou obsahovat časový graf.
        /// Toto pole by nemělo být null nebo prázdné, protože pak Scheduler nemá smysl.
        /// </summary>
        public Table[] ScheduleTables { get; set; }
        /// <summary>
        /// Tabulky zobrazené v pravém panelu, typicky zdroje (pracovníky, přípravky, atd) k zaplánování.
        /// Tyto tabulky na sobě nejsou závislé, jen se střídavě zobrazují v jednom prostoru, opatřeném TabHeaderem (pokud je jich více).
        /// Pokud bude toto pole null nebo prázdné, nebudou se zdroje nabízet.
        /// </summary>
        public Table[] SourceTables { get; set; }
        /// <summary>
        /// Tabulky zobrazené v dolním panelu, typicky informace (konflikty, využití dat, atd) pro obsluhu.
        /// Tyto tabulky na sobě nejsou závislé, jen se střídavě zobrazují v jednom prostoru, opatřeném TabHeaderem (pokud je jich více).
        /// Pokud bude toto pole null nebo prázdné, nebudou se informace nabízet.
        /// </summary>
        public Table[] InfoTables { get; set; }
    }
    #endregion
    #region GetData request a response
    /// <summary>
    /// Požadavek na naplnění dat do tabulek, které datový zdroj již dříve připravil v požadavku <see cref="DataSourceGetTablesRequest"/> do odpovědi <see cref="DataSourceGetTablesResponse"/>.
    /// Nyní má datový zdroj do těchto tabulek naplnit data (=řádky).
    /// </summary>
    public class DataSourceGetDataRequest : DataSourceRequest
    {
        public DataSourceGetDataRequest(Data.ProgressData progressData, SchedulerPanel panel)
            : base(progressData)
        {
            this.Panel = panel;
        }
        public SchedulerPanel Panel { get; private set; }
    }
    public class DataSourceGetDataResponse : DataSourceResponse
    {
        public DataSourceGetDataResponse(DataSourceGetDataRequest request)
            : base(request)
        { }

    }
    #endregion
}
