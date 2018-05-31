using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Asol.Tools.WorkScheduler.Application;

namespace Asol.Tools.WorkScheduler.Services
{
    public class TestDataSource : IDataSource
    {
        #region ProcessRequest
        protected virtual DataSourceResponse ProcessRequest(DataSourceRequest request)
        {
            if (request == null) return null;
            // if (request is DataSourceGetTablesRequest) return this.ProcessRequestGetControls(request as DataSourceGetTablesRequest);
            // if (request is DataSourceGetDataRequest) return this.ProcessRequestGetData(request as DataSourceGetDataRequest);

            return null;
        }
        #region Get Controls
        /*
        private DataSourceResponse ProcessRequestGetControls(DataSourceGetTablesRequest request)
        {
            DataSourceGetTablesResponse response = new DataSourceGetTablesResponse(request);




            return response;

        }
        */
        #endregion
        #region Get Data
        /*
        private DataSourceResponse ProcessRequestGetData(DataSourceGetDataRequest request)
        {
            DataSourceGetDataResponse response = new DataSourceGetDataResponse(request);

            Application.App.TraceInfo(Application.TracePriority.Priority1_ElementaryTimeDebug, "TestDataSource", "ProcessRequestGetData", "Start");
            System.Threading.Thread.Sleep(2000);
            Application.App.TraceInfo(Application.TracePriority.Priority1_ElementaryTimeDebug, "TestDataSource", "ProcessRequestGetData", "Done");

            return response;
        }
        */
        #endregion
        #endregion
        #region IDataSource members
        PluginActivity IPlugin.Activity { get { return PluginActivity.None; } }
        DataSourceResponse IDataSource.ProcessRequest(DataSourceRequest request) { return this.ProcessRequest(request); }
        #endregion
    }
}
