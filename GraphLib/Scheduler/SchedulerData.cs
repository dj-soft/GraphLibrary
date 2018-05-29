using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Asol.Tools.WorkScheduler.Application;
using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Services;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    /// <summary>
    /// Třída, která obsahuje veškerá data Scheduleru.
    /// Současně představuje jak zdroj globálních funkcí do ToolBaru (IFunctionGlobal), tak i zdroj dat pro grafy v GUI (IDataSource).
    /// </summary>
    public class SchedulerData : IFunctionGlobal, IDataSource
    {
        #region Konstruktor a privátní data
        public SchedulerData()
        {
            // Konstruktor vrátí new objekt, ale pouze pro použití jako Plugin.
            // Pro plnohodnotné použití a permanentní životnost je určen singleton Data.
            // Ten po vytvoření instance třídy SchedulerData navíc volá metodu Initialise(), která fyzicky naplní instanci potřebnými daty.
        }
        /// <summary>
        /// Singleton, jediná instance obsahující reálná data datového zdroje
        /// </summary>
        public static SchedulerData Data
        {
            get
            {
                if (__Data == null)
                {
                    lock (__Lock)
                    {
                        if (__Data == null)
                            __Data = CreateData();
                    }
                }
                return __Data;
            }
        }
        private static SchedulerData CreateData()
        {
            SchedulerData data = new SchedulerData();
            data.Initialise();
            return data;
        }
        private static SchedulerData __Data;
        private static object __Lock = new object();
        #endregion
        #region Inicializace dat objektu, který se používá jako datová základna
        /// <summary>
        /// Inicializace dat Scheduleru
        /// </summary>
        protected void Initialise()
        {
        }
        #endregion
        #region implementace IFunctionGlobal a IDataSource
        PluginActivity IPlugin.Activity { get { return PluginActivity.Standard; } }
        /// <summary>
        /// Vytvoří a vrátí sadu globálních funkcí pro this plugin
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        FunctionGlobalPrepareResponse IFunctionGlobal.PrepareGui(FunctionGlobalPrepareGuiRequest request)
        {
            return Data.PrepareGui(request);
        }
        /// <summary>
        /// Metoda může prověřit funkce, vytvořené ostatními pluginy.
        /// Kterýkoli plugin tak může nastavit FunctionGlobalGroup.IsVisible = false, 
        /// nebo FunctionGlobalGroup.Items[].IsVisible nebo IsEnabled = false, a zajistit tak skrytí jakékoli funkce z jiné služby.
        /// </summary>
        /// <param name="request"></param>
        void IFunctionGlobal.CheckGui(FunctionGlobalCheckGuiRequest request)
        {
            Data.CheckGui(request);
        }
        /// <summary>
        /// Datový zdroj dostane jistý požadavek, a ten zpracuje a vrací data.
        /// Formát požadavku a vrácené odpovědi je závislý na konkrétní situaci.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        DataSourceResponse IDataSource.ProcessRequest(DataSourceRequest request)
        {
            return Data.ProcessRequest(request);
        }
        #endregion
        #region Tvorba prvků GUI (ToolBar), obsluha událostí vyvolaných v Toolbaru (eventhandlery)
        /// <summary>
        /// Připraví GUI.
        /// Běží v rámci Singletonu.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected FunctionGlobalPrepareResponse PrepareGui(FunctionGlobalPrepareGuiRequest request)
        {
            this._ToolBar = request.ToolBar;

            List<FunctionGlobalGroup> groups = new List<FunctionGlobalGroup>();
            this._PrepareGuiEdit(groups);
            this._PrepareGuiShow(groups);

            FunctionGlobalPrepareResponse response = new FunctionGlobalPrepareResponse();
            response.Items = groups.ToArray();
            return response;
        }
        private void _PrepareGuiEdit(List<FunctionGlobalGroup> groups)
        {
            FunctionGlobalGroup group = new FunctionGlobalGroup(this);
            group.Title = "ÚPRAVY";
            group.Order = "A1";
            group.ToolTipTitle = "Úpravy zadaných dat";

            this.ButtonUndo = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = Components.IconStandard.EditUndo, Text = "Zpět", IsEnabled = false, LayoutHint = LayoutHint.NextItemSkipToNextRow };
            this.ButtonUndo.Click += ButtonUndo_Click;
            this.ButtonRedo = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Small, Image = Components.IconStandard.EditRedo, Text = "Vpřed", IsEnabled = true };
            this.ButtonRedo.Click += ButtonRedo_Click;
            this.ButtonReload = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.Refresh, Text = "Přenačíst", ToolTip = "Zruší všechny provedené změny a znovu načte data z databáze", IsEnabled = true };
            this.ButtonReload.Click += ButtonReload_Click;
            this.ButtonSave = new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Button, Size = FunctionGlobalItemSize.Half, Image = Components.IconStandard.DocumentSave, Text = "Uložit", ToolTip = "Uloží všechny provedené změny do databáze", IsEnabled = false };
            this.ButtonSave.Click += ButtonSave_Click;

            group.Items.Add(this.ButtonUndo);
            group.Items.Add(this.ButtonRedo);
            group.Items.Add(new FunctionGlobalItem(this) { ItemType = FunctionGlobalItemType.Separator, Size = FunctionGlobalItemSize.Whole });
            group.Items.Add(this.ButtonReload);
            group.Items.Add(this.ButtonSave);

            groups.Add(group);
        }
        private void _PrepareGuiShow(List<FunctionGlobalGroup> groups)
        { }
        /// <summary>
        /// Reference na objekt GToolBar, který reprezentuje hlavní toolbar aplikace.
        /// </summary>
        private GToolBar _ToolBar;
        private void ButtonUndo_Click(object sender, FunctionItemEventArgs args) { }
        private void ButtonRedo_Click(object sender, FunctionItemEventArgs args) { }
        private void ButtonReload_Click(object sender, FunctionItemEventArgs args) { App }
        private void ButtonSave_Click(object sender, FunctionItemEventArgs args) { }

        protected void CheckGui(FunctionGlobalCheckGuiRequest request)
        { }
        protected FunctionGlobalItem ButtonUndo;
        protected FunctionGlobalItem ButtonRedo;
        protected FunctionGlobalItem ButtonReload;
        protected FunctionGlobalItem ButtonSave;
        #endregion
        #region Zpracování požadavku z GUI vrstvy
        protected DataSourceResponse<DataSourceRequest> ProcessRequest(DataSourceRequest request)
        {
            if (request == null) return null;
            if (request is DataSourceGetTablesRequest) return this.ProcessRequestGetTables(request as DataSourceGetTablesRequest);
            if (request is DataSourceGetDataRequest) return this.ProcessRequestGetData(request as DataSourceGetDataRequest);

            return null;
        }
        protected DataSourceResponse<DataSourceRequest> ProcessRequestGetTables(DataSourceGetTablesRequest request)
        {
            DataSourceGetTablesResponse response = new DataSourceGetTablesResponse(request);



            return response;
        }
        #endregion
    }
}
