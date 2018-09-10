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

            try
            {
                using (var scope = Application.App.Trace.Scope(Application.TracePriority.Priority3_BellowNormal, "PluginForm", "InitializeWorkScheduler", ""))
                {
                    this.MainData = new Scheduler.MainData(this as IAppHost);
                    this.MainData.LoadData(dataPack);
                    this.MainControl = this.MainData.CreateControl();
                    this.Controls.Add(this.MainControl);
                    this.MainControl.Dock = DockStyle.Fill;
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
        /// <summary>
        /// Tato metoda zajistí otevření formuláře daného záznamu.
        /// Pouze převolá odpovídající metodu v <see cref="MainData"/>.
        /// </summary>
        /// <param name="recordGId"></param>
        void IAppHost.RunOpenRecordForm(GId recordGId)
        {
            ShowMsg("Rád bych otevřel záznam:~" + recordGId.ToString() + ";~~ale jsem jen obyčejný testovací formulář.");
        }
        /// <summary>
        /// Metoda, která zajistí provedení dané funkce
        /// </summary>
        /// <param name="runArgs"></param>
        void IAppHost.RunContextFunction(RunContextFunctionArgs runArgs)
        {
            ShowMsg("Rád bych provedl funkci:~" + runArgs.MenuItemText + ";~~ale jsem jen obyčejný testovací formulář.");
        }
        protected void ShowMsg(string message)
        {
            message = message.Replace("~", Environment.NewLine);
            System.Windows.Forms.MessageBox.Show((this as IWin32Window), message, "Problém:", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        #endregion
    }
}
