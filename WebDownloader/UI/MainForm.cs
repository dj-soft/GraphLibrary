﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Djs.Tools.WebDownloader.Download;

namespace Djs.Tools.WebDownloader.UI
{
    public partial class MainForm : Form
    {
        #region Tvorba a život formuláře a základní eventy
        public MainForm()
        {
            InitializeComponent();
            string sample = "https://content3.wantedbabes.com/pinupfiles.com/2205/00.jpg"; // "http://ftop.ru/images/201409/ftop.ru_120070.jpg";
            if (Clipboard.ContainsText(TextDataFormat.Text))
            {
                string clipText = Clipboard.GetText(TextDataFormat.Text);
                if (!String.IsNullOrEmpty(clipText) && !clipText.Contains('\r') && clipText.Trim().StartsWith("http://"))
                    sample = clipText;
            }
            this._ToolbarInit();

            this._DirCleanInit(this.TabPageCleanDir);
            this._SamplePanel.SampleText = sample;
            this._SamplePanel.Parse += new EventHandler(_SamplePanel_Parse);
            this._AdressPanel.Preview += new EventHandler(_AdressPanel_Preview);
            this._DownloadPanel.TargetPathChanged += _DownloadPanel_TargetPathChanged;
            this._DownloadPanel.StartClick += new EventHandler(_DownloadPanel_StartClick);
            this.Activated += new EventHandler(MainForm_Activated);
            this.Disposed += new EventHandler(MainForm_Disposed);
            Microsoft.Win32.SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            App.Config.SaveEnabled = true;
        }
        void MainForm_Activated(object sender, EventArgs e)
        {
            if (App.MainForm == null) App.MainForm = this;
        }
        void MainForm_Disposed(object sender, EventArgs e)
        {
            Microsoft.Win32.SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            App.MainForm = null;
        }
        void _SamplePanel_Parse(object sender, EventArgs e)
        {
            this._AdressPanel.Parse(this._SamplePanel.SampleText);
            _DirCleanReloadPath();
        }
        void _AdressPanel_Preview(object sender, EventArgs e)
        {
            using (UI.PreviewForm pvf = new UI.PreviewForm())
            {
                pvf.WebAdress = this._AdressPanel.WebAdress.Clone;
                pvf.TargetPath = TargetPath;
                pvf.CreateSampleUrls(500);
                pvf.ShowDialog(this);
            }
        }
        private void _DownloadPanel_TargetPathChanged(object sender, EventArgs e)
        {
            App.Config.SaveToPath = TargetPath;
            _DirCleanReloadPath();
        }
        void _DownloadPanel_StartClick(object sender, EventArgs e)
        {
            this._DownloadPanel.Start(this._AdressPanel.WebAdress);
        }
        internal string TargetPath { get { return this._DownloadPanel.TargetPath; } set { this._DownloadPanel.TargetPath = value; } }
        #endregion
        #region SystemEvents.PowerModeChanged: Sleep => Pause

        private void SystemEvents_PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case Microsoft.Win32.PowerModes.Suspend:
                    this.DownloadSysPowerPause();
                    break;
                case Microsoft.Win32.PowerModes.Resume:
                    this.DownloadSysPowerResume();
                    break;

            }
        }
        private void DownloadSysPowerPause()
        {
            this.DownloadSysPowerRunning = false;
            if (this._DownloadPanel.DownloadState == WorkingState.Working)
            {
                this._DownloadPanel.DownloadPause();
                this.DownloadSysPowerRunning = true;
            }
        }
        private void DownloadSysPowerResume()
        {
            if (this.DownloadSysPowerRunning)
            {
                this._DownloadPanel.DownloadResume();
                this.DownloadSysPowerRunning = false;
            }
        }
        private bool DownloadSysPowerRunning;

        #endregion
        #region Práce s toolbarem
        private void _ToolbarInit()
        {
            this._ToolOpenDrop.DropDownItems.Clear();
            this._ToolSaveAuto.ImageScaling = ToolStripItemImageScaling.None;
            this._ToolSaveAuto.Image = GetImage(App.Config.SaveAutomatic, Properties.Resources.dialog_accept);
            this._ToolSaveOnDownload.ImageScaling = ToolStripItemImageScaling.None;
            this._ToolSaveOnDownload.Image = GetImage(App.Config.SaveOnDownload, Properties.Resources.dialog_accept);

            string targetPath = App.Config.SaveToPath;
            if (String.IsNullOrEmpty(targetPath)) targetPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            TargetPath = targetPath;
        }
        private void _SaveButton_ButtonClick(object sender, EventArgs e)
        {
            if (!this._AdressPanel.WebAdress.CanSave)
            {
                Dialogs.Warning("Tuto adresu nelze uložit. Vyplňte její vzorec.");
                return;
            }
            App.Run(this._AdressPanel.WebAdress.Save);
        }
        private void _SaveButton_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.Name)
            {
                case "_ToolSaveAuto":
                    App.Config.SaveAutomatic = !App.Config.SaveAutomatic;
                    this._ToolSaveAuto.Image = GetImage(App.Config.SaveAutomatic, Properties.Resources.dialog_accept);
                    break;
                case "_ToolSaveOnDownload":
                    App.Config.SaveOnDownload = !App.Config.SaveOnDownload;
                    this._ToolSaveOnDownload.Image = GetImage(App.Config.SaveOnDownload, Properties.Resources.dialog_accept);
                    break;
            }
        }
        #endregion
        #region DirClean
        private void _DirCleanInit(Control parent)
        {
            this._DirCleanPanel = new DirCleanPanel() { Dock = DockStyle.Fill };
            parent.Controls.Add(this._DirCleanPanel);
        }
        /// <summary>
        /// Z údajů na stránce Download určí cílový adresář a umístí jej do stránky Úklid
        /// </summary>
        private void _DirCleanReloadPath()
        {
            string currUrl = this._AdressPanel.WebAdress.Text;                // Aktuální konkrétní adresa URL
            string path = Download.DownloadItem.CreateRootPath(currUrl, this.TargetPath);
            _DirCleanPanel.CleanPath = path;
        }
        private DirCleanPanel _DirCleanPanel;





        private Image GetImage(bool value, Image image)
        {
            return (value ? image : null);
        }
        #endregion
    }
}
