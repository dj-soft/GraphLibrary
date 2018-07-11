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
    public partial class PluginForm : Form, Scheduler.IAppHost
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
                    this.MainData = new Scheduler.MainData(this as Scheduler.IAppHost);
                    this.MainData.LoadData(dataPack);
                    this.MainControl = this.MainData.CreateGui();
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
        void Scheduler.IAppHost.RunOpenRecordForm(GId recordGId)
        {
            System.Windows.Forms.MessageBox.Show("Rád bych otevřel záznam " + recordGId.ToString() + ";\r\n ale jsem jen testovací formulář.");
        }
        #endregion
    }
}
