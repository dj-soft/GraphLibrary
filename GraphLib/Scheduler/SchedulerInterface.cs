using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Services;

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
    public class DataSourceGetTablesResponse : DataSourceResponse<DataSourceGetTablesRequest>
    {
        public DataSourceGetTablesResponse(DataSourceGetTablesRequest request)
            : base(request)
        { }
        /// <summary>
        /// Titulek panelu, bude použit pokud bude existovat více zdrojů dat
        /// </summary>
        public Localizable.TextLoc Title { get; set; }
        /// <summary>
        /// Tabulka zobrazující úkoly k zaplánování, zobrazuje se vlevo.
        /// Tato tabulka by logicky neměla obsahovat časové grafy, jde o prostý seznam úkolů.
        /// Typicky obsahuje položky, které dosud nejsou obsaženy v grafech v WorkTable.
        /// </summary>
        public Table TaskPlanTable { get; set; }
        /// <summary>
        /// Tabulka zobrazující úkoly již zaplánované, zobrazuje se vlevo.
        /// Tato tabulka by logicky neměla obsahovat časové grafy, jde o prostý seznam úkolů.
        /// Typicky obsahuje položky, které již jsou obsaženy v grafech v WorkTable.
        /// </summary>
        public Table TaskWorkTable { get; set; }
        /// <summary>
        /// Hlavní tabulka obsahující plánování, zobrazuje se uprostřed v hlavním poli.
        /// Její položky = místa, kam se plánuje práce.
        /// Tabulka by měla obsahovat jeden sloupec typu TimeGraph.
        /// </summary>
        public Table SchedulerMainTable { get; set; }
        /// <summary>
        /// Vedlejší tabulka, obsahující použité zdroje pro plánování, zobrazuje se uprostřed pod hlavním polem.
        /// Její položky = zdroje použité k plánování, vybrané v tabulce vpravo.
        /// </summary>
        public Table SchedulerUsedTable { get; set; }
        /// <summary>
        /// Tabulka zdrojů 1, zobrazuje se vpravo
        /// </summary>
        public Table Source1Table { get; set; }
        /// <summary>
        /// Tabulka zdrojů 2, zobrazuje se vpravo
        /// </summary>
        public Table Source2Table { get; set; }
        /// <summary>
        /// Tabulka zdrojů 3, zobrazuje se vpravo
        /// </summary>
        public Table Source3Table { get; set; }
        /// <summary>
        /// Tabulka výstupních informací 1, zobrazuje se dole
        /// </summary>
        public Table Info1Table { get; set; }
        /// <summary>
        /// Tabulka výstupních informací 2, zobrazuje se dole
        /// </summary>
        public Table Info2Table { get; set; }
    }
    /// <summary>
    /// Enum určující obsah levého panelu v okně Scheduleru: typicky obsahuje úkoly, které se mají plánovat (již jsou v plánu, nebo ještě nejsou).
    /// </summary>
    public enum LeftPanelContentType
    {
        /// <summary>
        /// Žádný obsah, panel není nepoužit
        /// </summary>
        None = 0,
        /// <summary>
        /// Položky kategorie Plán, tabulka <see cref="DataSourceGetTablesRequest.TaskPlanTable"/>
        /// </summary>
        TaskPlan,
        /// <summary>
        /// Položky kategorie Work, tabulka <see cref="DataSourceGetTablesRequest.TaskWorkTable"/>
        /// </summary>
        TaskWork
    }
    /// <summary>
    /// Enum určující obsah pravého panelu v okně Scheduleru: typicky obsahuje nějaké potřebné zdroje, které se mají pro zaplánování použít
    /// </summary>
    public enum RightPanelContentType
    {
        /// <summary>
        /// Žádný obsah, panel není nepoužit
        /// </summary>
        None = 0,
        /// <summary>
        /// Zdroje 1, tabulka <see cref="DataSourceGetTablesRequest.Source1Table"/>
        /// </summary>
        Source1,
        /// <summary>
        /// Zdroje 2, tabulka <see cref="DataSourceGetTablesRequest.Source2Table"/>
        /// </summary>
        Source2,
        /// <summary>
        /// Zdroje 3, tabulka <see cref="DataSourceGetTablesRequest.Source3Table"/>
        /// </summary>
        Source3
    }
    /// <summary>
    /// Enum určující obsah dolního panelu v okně Scheduleru: typicky obsahuje nějaké informace o stavu zaplánování.
    /// Tyto informace se nikam interaktivně nepoužívají, jde o statické výstupní informace.
    /// </summary>
    public enum BottomPanelContentType
    {
        /// <summary>
        /// Žádný obsah, panel není nepoužit
        /// </summary>
        None = 0,
        /// <summary>
        /// Zdroje 1, tabulka <see cref="DataSourceGetTablesRequest.Info1Table"/>
        /// </summary>
        Info1,
        /// <summary>
        /// Zdroje 2, tabulka <see cref="DataSourceGetTablesRequest.Info2Table"/>
        /// </summary>
        Info2
    }
    #endregion
    #region GetData request and response
    /// <summary>
    /// Požadavek na vygenerování tabulek, které bude datový zdroj používat
    /// </summary>
    public class DataSourceGetDataRequest : DataSourceRequest
    {
        public DataSourceGetDataRequest(Data.ProgressData progressData)
            : base(progressData)
        {

        }
    }
    public class DataSourceGetDataResponse : DataSourceResponse<DataSourceGetDataRequest>
    {
        public DataSourceGetDataResponse(DataSourceGetDataRequest request)
            : base(request)
        { }

    }
    #endregion
}
