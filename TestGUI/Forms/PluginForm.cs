using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Scheduler;
using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.TestGUI
{
    /// <summary>
    /// Třída PluginForm pro účely testování nahrazuje klietnský plugin.
    /// Zajistí v podstatě totéž, co zajišťuje Connector v pluginu: 
    /// vytvoření datového objektu WorkScheduler, 
    /// jeho iniciaci pomocí datového balíku, 
    /// získání GUI controlu z WorkScheduler a jeho vložení do formuláře.
    /// Dále tento Form simuluje datovou základnu pro volání funkcí z WorkScheduler do Hosta.
    /// </summary>
    public partial class PluginForm : Form, IAppHost
    {
        #region Inicializace, tvorba GUI controlu z dodaných dat
        /// <summary>
        /// Konstruktor
        /// </summary>
        public PluginForm()
        {
            InitializeComponent();
            InitializeWorkScheduler();
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority1_ElementaryTimeDebug, "PluginForm", "Initialize", "", "Setting Maximized"))
            {
                this.StartPosition = FormStartPosition.CenterParent;
                this.WindowState = FormWindowState.Maximized;
            }
        }
        /// <summary>
        /// Inicializace controlu Scheduleru
        /// </summary>
        protected void InitializeWorkScheduler()
        {
            Application.App.AppCompanyName = "Asseco Solutions";
            Application.App.AppProductName = "WorkScheduler";

            string dataPack = this.SearchForDataPack();
            if (dataPack == null) return;
            // dataPack = Noris.LCS.Base.WorkScheduler.WorkSchedulerSupport.TryDecompress(dataPack);
            Noris.LCS.Base.WorkScheduler.GuiData guiData = Noris.LCS.Base.WorkScheduler.Persist.Deserialize(dataPack) as Noris.LCS.Base.WorkScheduler.GuiData;
            if (guiData == null) return;

            // Tady se jedná o samostatnou aplikaci, nechť se i tak chová:
            guiData.Properties.PluginFormBorder = PluginFormBorderStyle.Sizable;

            try
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority3_BellowNormal, "PluginForm", "InitializeWorkScheduler", ""))
                {
                    this.MainData = new Scheduler.MainData(this as IAppHost);
                    this.MainData.LoadData(guiData);
                    this.MainData.CreateControlToForm(this);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Při spouštění WorkScheduleru došlo k chybě:" + Environment.NewLine + exc.Message, "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }
        /// <summary>
        /// Main data Scheduleru
        /// </summary>
        protected Scheduler.MainData MainData;
        /// <summary>
        /// GUI control
        /// </summary>
        protected System.Windows.Forms.Control MainControl;
        /// <summary>
        /// Vrátí obsah nejnovějšího souboru obsahujícího balík s daty z adresáře AppLocalDataPath / Data.
        /// Může vrátit null.
        /// </summary>
        /// <returns></returns>
        protected string SearchForDataPack()
        {
            string dataPack = null;
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority3_BellowNormal, "PluginForm", "SearchForDataPack", ""))
            {
                string appPath = Application.App.AppCodePath;                                      // D:\Hobby\Csharp\GraphLibrary\bin
                if (TrySearchDataPackOne(appPath, "*.dat", out dataPack)) return dataPack;         // Soubory přímo vedle aplikace = přenosný předváděcí režim
                string prgPath = Path.Combine(Path.GetDirectoryName(appPath), "TestGUI");          // D:\Hobby\Csharp\GraphLibrary\TestGUI
                string name = "Data_20180101_120000.dat";
                if (TrySearchDataPackOne(prgPath, name, out dataPack)) return dataPack;            // Soubor přenášený v rámci zdrojových kódů
                string locPath = Application.App.GetAppLocalDataPath("Data");
                string pattern = "Data_????????_??????.dat";
                if (TrySearchDataPackOne(locPath, pattern, out dataPack)) return dataPack;         // Soubory uložené v Win adresáři této aplikace
            }
            return null;
        }
        /// <summary>
        /// Metoda najde v daném adresáři soubory dle daného paternu, vybere z nich nejnovější, 
        /// načte jeho obsah jako text a ten uloží do out parametru dataPack. Vrátí true.
        /// Pokud nenajde, vrací false a dataPack bude null.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <param name="dataPack"></param>
        /// <returns></returns>
        private static bool TrySearchDataPackOne(string path, string pattern, out string dataPack)
        {
            dataPack = null;
            if (String.IsNullOrEmpty(path)) return false;
            DirectoryInfo di = new DirectoryInfo(path);
            if (!di.Exists) return false;
            List<FileInfo> files = di.GetFiles(pattern).ToList();
            if (files.Count == 0) return false;
            if (files.Count > 1)
                files.Sort((a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
            string fileName = files[0].FullName;
            dataPack = File.ReadAllText(fileName);
            return true;
        }
        #endregion
        #region Implementace Scheduler.IAppHost, offline řešení požadovaných commandů
        void IAppHost.CallAppHostFunction(AppHostRequestArgs appRequest)
        {
            AppHostResponseArgs appResponse = CreateResponse(appRequest);

            if (appResponse == null)
            {
                ShowMsg("Rád bych provedl požadovanou akci:~" + appRequest.Request.ToString() + ";~~ale jsem jen obyčejný testovací formulář.");
                appResponse = new AppHostResponseArgs(appRequest);
                appResponse.Result = AppHostActionResult.Failure;
            }
            if (appRequest.CallBackAction != null)
                this._SendResponse(appRequest, appResponse);
        }
        /// <summary>
        /// Vrátí response na daný request
        /// </summary>
        /// <param name="appRequest"></param>
        /// <returns></returns>
        private static AppHostResponseArgs CreateResponse(AppHostRequestArgs appRequest)
        {
            GuiRequest guiRequest = appRequest.Request;
            GuiResponse guiResponse = null;
            switch (guiRequest.Command)
            {
                case GuiRequest.COMMAND_ToolbarClick:
                    guiResponse = CreateResponseToolbarClick(guiRequest);
                    break;
                case GuiRequest.COMMAND_ContextMenuClick:
                    guiResponse = CreateResponseContextMenuClick(guiRequest);
                    break;
                case GuiRequest.COMMAND_GraphItemMove:
                case GuiRequest.COMMAND_GraphItemResize:
                    guiResponse = GuiResponse.Success();
                    break;
                case GuiRequest.COMMAND_OpenRecords:
                    guiResponse = CreateResponseOpenRecords(guiRequest);
                    break;
                case GuiRequest.COMMAND_QueryCloseWindow:
                case GuiRequest.COMMAND_CloseWindow:
                    guiResponse = GuiResponse.Success();
                    break;
            }
            if (guiResponse == null) return null;
            AppHostResponseArgs appResponse = new AppHostResponseArgs(appRequest);
            appResponse.Result = AppHostActionResult.Success;
            appResponse.GuiResponse = guiResponse;
            return appResponse;
        }
        /// <summary>
        /// Vrátí response na akci ToolbarClick
        /// </summary>
        /// <param name="guiRequest"></param>
        /// <returns></returns>
        private static GuiResponse CreateResponseToolbarClick(GuiRequest guiRequest)
        {
            GuiResponse guiResponse = null;
            string txtResponse;
            if (guiRequest.ToolbarItem == null) return guiResponse;
            switch (guiRequest.ToolbarItem.Name)
            {
                case "tlbSave":
                    txtResponse = "H4sIAAAAAAAEAJWRy27CMBBFf8XytsJJQPSBkqBCqwqBQCIpVVV1MUkG6mJsZDuofFyX/a/aPEQW3XQz1vje8bnyxP2vjSA71IYrmdCIhZSgLFXF5SqhtV22omvaT+OtdxiL0pJFw+zcQ41gsUpoO4xuW1HYCjukHfa6N72wy+460cmhdEIr2PGKfYKEEtc0jSuwkMYLEDWSQ2X5fosJnSrNDZsMMzYAg+xF6XVWfmBVC9TsqeZzNFslDVLywEGolZ+QrjvfZ9YlSmhWlyUa40C5UqIAPbK4MeRea9izOciV87yFV+135/AS8YWNZMXdmJe8cEz3KKEQ53jZ3njjwD2KIOlJTOgShM80hY1rrCgy2Lk259aLEp8ns59vSoI0Djzob2b0T6bVdRM5rIsLcjzL8vFrkxg0P8K1B5A7j4sILktOfwHf/4QrGAIAAA==";
                    guiResponse = Persist.Deserialize(txtResponse) as GuiResponse;
                    break;
                case "tlbCube":
                    txtResponse = "H4sIAAAAAAAEAJWR0U7CMBSGX6XpraHbAMWQbUTQGAKBhE2MMV6cbQeslJa0HZGH89L3sgUMu/DGm9Oe/v/p96eNB59bQfaoDVcyoRELKUFZqorLdUJru2pFN3SQxjvvMBalJcuG2blHGsFildB2GN22orAVdkg77F/3+p0u63W6Z4fSCa1gzyv2ARJK3NA0rsBCGi9B1EiOleWHHSZ0pjQ3bDrK2BAMsmelN1n5jlUtULPHmi/Q7JQ0SMk9B6HWfkK67vc8sy5RQrO6LNEYB8qVEgXoscWtIXdaw4EtQK6d5zW8ar85h5eIL2wsK+7GvOSFU7oHCYX4jZcdjDcO3aUIkp7FhFpduxAz2Pq9KDLYuzbn1mtP0/n3FyVBGgee8jcw+idwBcI0iaO6uBAlTuZZPnlpQoPmQ7j2yHLr6SOCyyenP1pKJiIYAgAA";
                    guiResponse = Persist.Deserialize(txtResponse) as GuiResponse;
                    break;
            }
            return guiResponse;
        }
        /// <summary>
        /// Vrátí response na akci ContextMenuClick
        /// </summary>
        /// <param name="guiRequest"></param>
        /// <returns></returns>
        private static GuiResponse CreateResponseContextMenuClick(GuiRequest guiRequest)
        {
            return null;
        }
        /// <summary>
        /// Vrátí response na akci OpenRecords
        /// </summary>
        /// <param name="guiRequest"></param>
        /// <returns></returns>
        private static GuiResponse CreateResponseOpenRecords(GuiRequest guiRequest)
        {
            if (guiRequest.RecordsToOpen != null && guiRequest.RecordsToOpen.Length > 0)
            {
                GuiId guiId = guiRequest.RecordsToOpen[0];
                string text = "Nyní by se měl otevřít formulář záznamu:~~" + guiId.ToString();
                ShowMsg(null, text);
            }
            return GuiResponse.Success();
        }
        /// <summary>
        /// Zavolá callback akci
        /// </summary>
        /// <param name="appRequest"></param>
        /// <param name="appResponse"></param>
        protected void _SendResponse(AppHostRequestArgs appRequest, AppHostResponseArgs appResponse)
        {
            if (appRequest.CallBackAction == null) return;

            if (appResponse == null)
            {
                appResponse = new AppHostResponseArgs(appRequest);
                appResponse.Result = AppHostActionResult.Success;
            }
            appRequest.CallBackAction(appResponse);
        }
        /// <summary>
        /// Zobrazí hlášku jako Warning
        /// </summary>
        /// <param name="message"></param>
        protected void ShowMsg(string message)
        {
            ShowMsg((this as IWin32Window), message);
        }
        /// <summary>
        /// Zobrazí hlášku jako Warning
        /// </summary>
        /// <param name="message"></param>
        protected static void ShowMsg(IWin32Window owner, string message)
        {
            message = message.Replace("~", Environment.NewLine);
            System.Windows.Forms.MessageBox.Show(owner, message, "Problém:", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        #endregion
    }
}
