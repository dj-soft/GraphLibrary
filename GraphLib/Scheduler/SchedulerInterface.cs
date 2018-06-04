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
        {
            this.DataDescriptorList = new List<DataDescriptor>();
        }
        /// <summary>
        /// Sada definic dat.
        /// Datový zdroj má přidat přinejmenším jednu položku, která popisuje data zobrazovaná v panelu typu SchedulerPanel
        /// </summary>
        public List<DataDescriptor> DataDescriptorList { get; private set; }

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
    #region class DataDescriptor
    /// <summary>
    /// DataDescriptor : Data popisující jednu kompletní sadu dat pro zobrazení v jednom panelu typu SchedulerPanel
    /// </summary>
    public class DataDescriptor
    {
        #region Základní sada dat
        /// <summary>
        /// Identifikátor sady dat.
        /// Typicky číslo subjektu verze dat, nebo verze dílenského plánu.
        /// </summary>
        public int DataId { get; set; }
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
        #endregion
        #region Analýza dat
        /// <summary>
        /// true pokud data potřebují donačíst nějakou tabulku.
        /// Tzn. tabulka byla vložena do některého z polí <see cref="ScheduleTables"/>, <see cref="TaskTables"/>, <see cref="SourceTables"/>, <see cref="InfoTables"/>,
        /// ale tabulka obsahuje 0 sloupců (=je prázdná).
        /// </summary>
        internal bool NeedLoadData { get { return (this.TablesForLoadData.Length > 0); } }
        /// <summary>
        /// Tabulky, které potřebují donačíst data.
        /// Tzn. tabulka byla vložena do některého z polí <see cref="ScheduleTables"/>, <see cref="TaskTables"/>, <see cref="SourceTables"/>, <see cref="InfoTables"/>,
        /// ale tabulka obsahuje 0 sloupců (=je prázdná).
        /// </summary>
        protected Table[] TablesForLoadData { get { return this.TablesAll.Where(t => t.ColumnsCount == 0).ToArray(); } }
        /// <summary>
        /// Pole všech tabulek.
        /// </summary>
        protected Table[] TablesAll
        {
            get
            {
                List<Table> tableList = new List<Table>();
                if (this.ScheduleTables != null) tableList.AddRange(this.ScheduleTables);
                if (this.TaskTables != null) tableList.AddRange(this.TaskTables);
                if (this.SourceTables != null) tableList.AddRange(this.SourceTables);
                if (this.InfoTables != null) tableList.AddRange(this.InfoTables);
                return tableList.ToArray();
            }
        }
        #endregion

    }
    #endregion
}
