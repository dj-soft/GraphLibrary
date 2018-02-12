using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Djs.Common.Application;

namespace Djs.Common.Services
{
    public class TestDataSource : IDataSource
    {
        #region ProcessRequest
        protected virtual DataSourceResponse ProcessRequest(DataSourceRequest request)
        {
            if (request == null) return null;
            if (request is DataSourceGetControlsRequest) return this.ProcessRequestGetControls(request as DataSourceGetControlsRequest);
            if (request is DataSourceGetDataRequest) return this.ProcessRequestGetData(request as DataSourceGetDataRequest);

            return null;
        }
        #region Get Controls
        private DataSourceResponse ProcessRequestGetControls(DataSourceGetControlsRequest request)
        {
            DataSourceGetControlsResponse response = new DataSourceGetControlsResponse(request);




            return response;

        }
        #endregion
        #region Get Data
        private DataSourceResponse ProcessRequestGetData(DataSourceGetDataRequest request)
        {
            DataSourceGetDataResponse response = new DataSourceGetDataResponse(request);

            Application.App.TraceInfo(Application.TracePriority.ElementaryTimeDebug_1, "TestDataSource", "ProcessRequestGetData", "Start");
            System.Threading.Thread.Sleep(2000);
            Application.App.TraceInfo(Application.TracePriority.ElementaryTimeDebug_1, "TestDataSource", "ProcessRequestGetData", "Done");

            return response;
        }
        #endregion
        #endregion
        #region IDataSource members
        PluginActivity IPlugin.Activity { get { return PluginActivity.Standard; } }
        DataSourceResponse IDataSource.ProcessRequest(DataSourceRequest request) { return this.ProcessRequest(request); }
        #endregion
    }
}
