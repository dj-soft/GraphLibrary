using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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
            dataPack = Noris.LCS.Base.WorkScheduler.WorkSchedulerSupport.Decompress(dataPack);
            Noris.LCS.Base.WorkScheduler.GuiData guiData = Noris.LCS.Base.WorkScheduler.Persist.Deserialize(dataPack) as Noris.LCS.Base.WorkScheduler.GuiData;
            if (guiData == null) return;

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
            string fileName = null;
            using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority3_BellowNormal, "PluginForm", "SearchForDataPack", ""))
            {
                string appPath = Application.App.AppCodePath;                                                // D:\Hobby\Csharp\GraphLibrary\bin
                string datPath = System.IO.Path.GetDirectoryName(appPath);                                   // D:\Hobby\Csharp\GraphLibrary
                string fixFile = System.IO.Path.Combine(datPath, "TestGUI", "Data_20180101_120000.dat");     // D:\Hobby\Csharp\GraphLibrary\TestGUI\Data_20180101_120000.dat
                if (System.IO.File.Exists(fixFile))
                {
                    fileName = fixFile;
                }
                else
                {
                    string path = Application.App.GetAppLocalDataPath("Data");
                    System.IO.DirectoryInfo pathInfo = new System.IO.DirectoryInfo(path);
                    if (!pathInfo.Exists)
                    {
                        pathInfo.Create();
                    }
                    else
                    {
                        List<System.IO.FileInfo> fileList = pathInfo.GetFiles("Data_????????_??????.dat").ToList();
                        int fileCount = fileList.Count;
                        if (fileCount > 0)
                        {
                            if (fileCount > 1)
                                fileList.Sort((a, b) => DateTime.Compare(b.LastAccessTime, a.LastAccessTime));
                            fileName = fileList[0].FullName;
                        }
                    }
                }
                scope.AddItem("DataFile: " + (fileName != null ? fileName : "NULL"));
            }
            return (fileName != null ? System.IO.File.ReadAllText(fileName) : null);
        }
        #endregion
        #region Implementace Scheduler.IAppHost
        void IAppHost.CallAppHostFunction(AppHostRequestArgs args)
        {
            if (NeedMessage(args.Request))
                ShowMsg("Rád bych provedl požadovanou akci:~" + args.Request.ToString() + ";~~ale jsem jen obyčejný testovací formulář.");
            if (args.CallBackAction != null)
                this._SendResponse(args, null);
        }
        protected static bool NeedMessage(GuiRequest request)
        {
            switch (request.Command)
            {   // Tyto příkazy NEBUDOU hlásit uživateli okno typu "Rád bych provedl požadovanou akci" :
                case GuiRequest.COMMAND_GraphItemMove:
                case GuiRequest.COMMAND_CloseWindow:
                    return false;
            }
            return true;
        }
        protected void _SendResponse(AppHostRequestArgs request, AppHostResponseArgs response)
        {
            if (request.CallBackAction == null) return;

            AppHostResponseArgs args = new AppHostResponseArgs(request);
            args.Result = AppHostActionResult.Success;
            request.CallBackAction(args);
        }

        protected void ShowMsg(string message)
        {
            message = message.Replace("~", Environment.NewLine);
            System.Windows.Forms.MessageBox.Show((this as IWin32Window), message, "Problém:", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        #endregion
        /*

GuiRequest typu:
Command = "ToolbarClick"
kde 	FullName	"tlbCube"	string

vrací GuiResponse, který říká: 
Upravit Toolbar položky: tlbSave a tlbCube:

Výsledkem je AppHostResponseArgs:
		Data	"H4sIAAAAAAAEAJWRzU4CMRSFX6Xp1tD50RghM0MEjSEQSJgRY4yLOzMXrJSWtB0iD+fS97LlJ8zCjZvb3p5z+520Sf9rI8gOteFKpjRiISUoK1VzuUppY5ed6Jb2s2TrHcaitGTRMjv3UCNYrFMah9FdJwo74TWJw14c9W5C1o26J4fSKa1hx2v2CRIqXNMsqcFClixANEgOlRX7LaZ0qjQ3bDLM2QAMshel13n1gXUjULOnhs/RbJU0SMkDB6FWfkK67nyeW5copXlTVWiMAxVKiRL0yOLGkHutYc/mIFfO8xZexe/O4SXiCxvJmrsxL3nhmO5RQinO8fK98caBuxRB0pOYUqsbF2IKG78XZQ471xbceu15Mvv5piTIksBT/gZG/wQuQZg2cdiUF6LE8Swvxq9taNB+CNceWG49fkRw+eTsFw7ZY5kYAgAA"	string
		Result	Success	Asol.Tools.WorkScheduler.AppHostActionResult

a to se odešle do callBackAction

        */
    }
}
