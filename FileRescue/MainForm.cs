using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Djs.Tools.CopyFile.Components;

namespace Djs.Tools.CopyFile
{
    public partial class MainForm : Form
    {
        #region Konstuktor, inicializace, události formuláře
        public MainForm()
        {
            InitializeComponent();
            InitializeProgress();
        }
        private void _Load(object sender, EventArgs e)
        {
            App.MainForm = this;
            this._PrepareInputValues();
        }
        private void _StartButton_Click(object sender, EventArgs e)
        {
            _StartCopy();
        }
        private void _CancelButton_Click(object sender, EventArgs e)
        {
            _StopCopy();
        }
        private void _PrepareInputValues()
        {
            SourceFileName = @"h:\Records\CT-2-HD-T2-26122025-2001.ts";    // @"f:\2-Phillips\02\Wallace a Gromit - Prokletí králíkodlaka.ts";
            TargetPathName = @"D:\Rescue";
            _ResetProgress();
        }
        #endregion
        #region Tvorba komponent
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._FilesPanel = new System.Windows.Forms.Panel();
            this._FilesPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
| System.Windows.Forms.AnchorStyles.Right)));
            this._FilesPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._FilesPanel.Controls.Add(this._TargetPathTxt);
            this._FilesPanel.Controls.Add(this._TargetPathLabel);
            this._FilesPanel.Controls.Add(this._SourceFileTxt);
            this._FilesPanel.Controls.Add(this._SourceFileLabel);
            this._FilesPanel.Location = new System.Drawing.Point(12, 13);
            this._FilesPanel.Name = "panel1";
            this._FilesPanel.Size = new System.Drawing.Size(560, 105);
            this._FilesPanel.TabIndex = 0;


            this._SourceFileLabel = new System.Windows.Forms.Label();
            this._SourceFileLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._SourceFileLabel.Location = new System.Drawing.Point(15, 9);
            this._SourceFileLabel.Name = "label1";
            this._SourceFileLabel.Size = new System.Drawing.Size(522, 13);
            this._SourceFileLabel.TabIndex = 0;
            this._SourceFileLabel.Text = "Zdroj (soubor nebo adresář + maska)";

            this._SourceFileTxt = new System.Windows.Forms.TextBox();
            this._SourceFileTxt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._SourceFileTxt.Location = new System.Drawing.Point(3, 25);
            this._SourceFileTxt.Name = "_SourceFileTxt";
            this._SourceFileTxt.Size = new System.Drawing.Size(550, 20);
            this._SourceFileTxt.TabIndex = 1;


            this._TargetPathLabel = new System.Windows.Forms.Label();
            this._TargetPathLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
           | System.Windows.Forms.AnchorStyles.Right)));
            this._TargetPathLabel.Location = new System.Drawing.Point(15, 54);
            this._TargetPathLabel.Name = "label2";
            this._TargetPathLabel.Size = new System.Drawing.Size(522, 13);
            this._TargetPathLabel.TabIndex = 2;
            this._TargetPathLabel.Text = "Cíl (adresář)";

            this._TargetPathTxt = new System.Windows.Forms.TextBox();
            this._TargetPathTxt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
          | System.Windows.Forms.AnchorStyles.Right)));
            this._TargetPathTxt.Location = new System.Drawing.Point(3, 70);
            this._TargetPathTxt.Name = "_TargetPathTxt";
            this._TargetPathTxt.Size = new System.Drawing.Size(550, 20);
            this._TargetPathTxt.TabIndex = 3;


            this._StartButton = new System.Windows.Forms.Button();
            this._StartButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this._StartButton.Location = new System.Drawing.Point(32, 124);
            this._StartButton.Name = "_StartButton";
            this._StartButton.Size = new System.Drawing.Size(147, 51);
            this._StartButton.TabIndex = 2;
            this._StartButton.Text = "START";
            this._StartButton.UseVisualStyleBackColor = true;
            this._StartButton.Click += new System.EventHandler(this._StartButton_Click);


            this._CancelButton = new System.Windows.Forms.Button();
            this._CancelButton.Enabled = false;
            this._CancelButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this._CancelButton.Location = new System.Drawing.Point(185, 124);
            this._CancelButton.Name = "_CancelButton";
            this._CancelButton.Size = new System.Drawing.Size(147, 51);
            this._CancelButton.TabIndex = 3;
            this._CancelButton.Text = "STORNO";
            this._CancelButton.UseVisualStyleBackColor = true;
            this._CancelButton.Click += new System.EventHandler(this._CancelButton_Click);


            this._ResultPanel = new FileContentMapPanel();
            this._ResultPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
          | System.Windows.Forms.AnchorStyles.Left)
          | System.Windows.Forms.AnchorStyles.Right)));
            this._ResultPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._ResultPanel.Location = new System.Drawing.Point(12, 181);
            this._ResultPanel.Name = "_ResultPanel";
            this._ResultPanel.Size = new System.Drawing.Size(560, 257);
            this._ResultPanel.TabIndex = 8;


            this._FilesPanel.SuspendLayout();
            this._FilesPanel.Controls.Add(this._SourceFileLabel);
            this._FilesPanel.Controls.Add(this._SourceFileTxt);
            this._FilesPanel.Controls.Add(this._TargetPathLabel);
            this._FilesPanel.Controls.Add(this._TargetPathTxt);
            this.SuspendLayout();
          
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 451);

            this.Controls.Add(this._FilesPanel);
            this.Controls.Add(this._StartButton);
            this.Controls.Add(this._CancelButton);
            this.Controls.Add(this._ResultPanel);
            this.MaximumSize = new System.Drawing.Size(3000, 750);
            this.MinimumSize = new System.Drawing.Size(600, 490);
            this.Name = "MainForm";
            this.Text = "Nouzový kopírovač souborů";
            this.Load += new System.EventHandler(this._Load);
            this._FilesPanel.ResumeLayout(false);
            this._FilesPanel.PerformLayout();
            this._ResultPanel.ResumeLayout(false);
            this._ResultPanel.PerformLayout();
            this.ResumeLayout(false);

        }
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel _FilesPanel;
        private System.Windows.Forms.TextBox _TargetPathTxt;
        private System.Windows.Forms.Label _TargetPathLabel;
        private System.Windows.Forms.TextBox _SourceFileTxt;
        private System.Windows.Forms.Label _SourceFileLabel;
        private System.Windows.Forms.Button _StartButton;
        private System.Windows.Forms.Button _CancelButton;
        private FileContentMapPanel _ResultPanel;

        #endregion
        #region Titulek okna
        /// <summary>
        /// Titulek okna bez suffixu = titulek aplikace
        /// </summary>
        private string __AppTitleTextStandard;
        /// <summary>
        /// Titulek okna aktuální, napojený na aktuální Text = fyzický titulek okna.
        /// Pokud bude opakovaně setována stejná hodnota, detekuje se to podle fieldu <see cref="__AppTitleTextCurrent"/> a nebude se do property Text nic vkládat.
        /// </summary>
        private string AppTitleTextCurrent
        {
            get { return __AppTitleTextCurrent; }
            set
            {
                if (!String.IsNullOrEmpty(value))
                {   // Pouze neprázdný string:
                    if (!String.Equals(value, __AppTitleTextCurrent))
                    {   // Pouze změněný string:
                        __AppTitleTextCurrent = value;
                        this.Text = value;
                    }
                }
            }
        }
        private string __AppTitleTextCurrent;
        #endregion
        #region Windows Taskbar Progress
        /// <summary>
        /// Inicializace komponenty pro zobrazení progresu v Taskbaru Windows
        /// </summary>
        private void InitializeProgress()
        {
            __TaskProgress = new TaskProgress(this);
            __TaskProgress.ProgressMaximum = 500;
            /*  Použití je jednoduché:
            var rand = new Random();
            var next = rand.Next(30);
            __TaskProgress.ProgressState = (next < 10 ? ThumbnailProgressState.Normal : next < 20 ? ThumbnailProgressState.Error : ThumbnailProgressState.Paused);
            __TaskProgress.ProgressValue = rand.Next(0, 100);
            */
            this.__AppTitleTextStandard = this.Text;
            this.__AppTitleTextCurrent = null;
        }
        protected override void WndProc(ref Message m)
        {
            __TaskProgress.FormWndProc(ref m);
            base.WndProc(ref m);
        }
        /// <summary>
        /// Hodnota progresu.
        /// Musí být v rozsahu 1 a více.
        /// Pokud bude setována hodnota nižší, než je aktuální <see cref="_TaskProgressValue"/>, tak bude <see cref="_TaskProgressValue"/> snížena na toto nově zadané maximum.
        /// </summary>
        private int _TaskProgressMaximum { get { return __TaskProgress.ProgressMaximum; } set { __TaskProgress.ProgressMaximum = value; } }
        /// <summary>
        /// Hodnota progresu.
        /// Musí být v rozsahu 0 až <see cref="ProgressMaximum"/>.
        /// </summary>
        private int _TaskProgressValue { get { return __TaskProgress.ProgressValue; } set { __TaskProgress.ProgressValue = value; } }
        /// <summary>
        /// Status progresu = odpovídá barvě
        /// </summary>
        private ThumbnailProgressState _TaskProgressState { get { return __TaskProgress.ProgressState; } set { __TaskProgress.ProgressState = value; } }
        /// <summary>
        /// Komponenta pro zobrazení progresu v Taskbaru Windows
        /// </summary>
        private TaskProgress __TaskProgress;
        #endregion
        #region Rozhraní na GUI
        internal string SourceFileName { get { return this._SourceFileTxt.Text; } set { this._SourceFileTxt.Text = value; } }
        internal string TargetPathName { get { return this._TargetPathTxt.Text; } set { this._TargetPathTxt.Text = value; } }
        #endregion
        #region Start async procesu, Cancel, obsluha progresu
        private async void _StartCopy()
        {
            _Copier = new FileCopierWin32();
            _Copier.Request = new FileCopierRequest() { SourceFile = SourceFileName, DestinationPath = TargetPathName };
            _Copier.Progress += _Copier_Progress;
           
            try
            {
                _SetState(true);
                _ResetProgress();

                // Spustíme dlouhodobou operaci na thread pool thread
                // Volající thread (GUI) pokračuje ihned dál
                await Task.Run(() => _Copier.Copy());

                // Po dokončení se vrátíme sem a to ve volajícím threadu.
                _Copier = null;
            }
            catch (Exception exc)
            {
            }
            finally
            {
                _SetState(false);
            }
        }
        private void _StopCopy()
        {
            var copier = _Copier;
            if (copier != null) copier.CancelProcess = true;
        }
        private void _SetState(bool isRunning)
        {
            _StartButton.Enabled = !isRunning;
            _CancelButton.Enabled = isRunning;
        }
        /// <summary>
        /// Progress handler volaný z <see cref="FileCopierWin32.CopyWithErrorRecovery(string, string, int, int)"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Copier_Progress(object sender, FileReadProgressEventArgs args)
        {
            if (this.InvokeRequired)
                this.BeginInvoke((Delegate)new Action<FileReadProgressEventArgs>(_ShowProgress), args);
            else
                this._ShowProgress(args);
        }
        private void _ResetProgress()
        {
            this._ResultPanel.ResetProgress();
        }
        private void _ShowProgress(FileReadProgressEventArgs args)
        {
            this._ResultPanel.ShowProgress(args);
        }
        private FileCopierWin32 _Copier;
        #endregion





        #region Start procesu STARÝ, Cancel, obsluha progresu
        //private async void _StartCopyOld()
        //{
        //    var copyData = new CopyDataInfo()
        //    {
        //        SourceFileName = this.SourceFileName,
        //        TargetPathName = this.TargetPathName,
        //    };
        //    copyData.ProgressChanged += _CopyData_ProgressChanged;
        //    _CopyWorker = copyData;

        //    _ResetProgress();
        //    _SetState(true);

        //    try
        //    {
        //        // Spustíme dlouhodobou operaci na thread pool thread
        //        // Volající thread (GUI) pokračuje ihned dál
        //        await Task.Run(() => copyData.RunCopy());

        //        // Po dokončení se vrátíme sem a to ve volajícím threadu.
        //        // Uložíme načtená data a vyvoláme event:
        //    }
        //    catch (Exception exc)
        //    {
        //    }
        //    finally
        //    {
        //        _SetState(false);
        //    }
        //}
        //private void _StopCopyOld()
        //{
        //    _CopyWorker?.StopCopy();
        //}
        //private CopyDataInfo _CopyWorker;

        //private void _CopyData_ProgressChanged(object sender, CopyDataProgressArgs args)
        //{
        //    if (this.InvokeRequired)
        //        this.BeginInvoke(new Action<CopyDataProgressArgs>(_ProgressChanged), args);
        //    else
        //        _ProgressChanged(args);
        //}
        //private void _ProgressChanged(CopyDataProgressArgs args)
        //{
        //    if (args.FileName != null)
        //        this.ProcessFileName = args.FileName;
        //    if (args.FileCopyInfo != null)
        //        this.ProcessCopyInfo = args.FileCopyInfo;
        //    if (args.SingleErrorCount.HasValue)
        //        this.ProcessErrorCount = args.SingleErrorCount;
        //    if (args.SingleErrorPercent.HasValue)
        //        this.ProcessErrorPercent = args.SingleErrorPercent;

        //    if (args.SingleProgressTotal.HasValue)
        //        _SingleFileMaximum = args.SingleProgressTotal.Value;
        //    if (args.SingleProgressValue.HasValue && _SingleFileMaximum.HasValue && _SingleFileMaximum.Value > 0L)
        //        this.ProcessPosition = getPermile(args.SingleProgressValue.Value);

        //    int getPermile(long value)
        //    {
        //        decimal ratio = (decimal)value / (decimal)_SingleFileMaximum.Value;                // Value / Total
        //        ratio = (ratio < 0m ? 0m : ratio > 1m ? 1m : ratio);                               // Range 0-1 include
        //        int result = (int)Math.Round((10000m * ratio), 0);
        //        return result;
        //    }
        //}
        //private long? _SingleFileMaximum;
        #endregion
    }
    //internal class CopyDataInfo
    //{
    //    internal string SourceFileName { get; set; }
    //    internal string TargetPathName { get; set; }
    //    internal int? CopyBlockSize { get; set; }
    //    internal int? RetryCount { get; set; }
    //    internal int? MaxErrorPct { get; set; }
    //    internal bool CancelRequest { get; set; }

    //    internal void RunCopy()
    //    {
    //        try
    //        {
    //            _RunCopyFile(this.SourceFileName, this.TargetPathName);
    //        }
    //        catch (Exception exc)
    //        {
    //            App.ShowWarning(exc.Message);
    //        }
    //    }
    //    internal void StopCopy()
    //    {
    //        CancelRequest = true;
    //    }

    //    private void _RunCopyFile(string sourceFile, string targetPath)
    //    {
    //        try
    //        {
    //            sourceFile = (sourceFile ?? "").Trim();
    //            targetPath = (targetPath ?? "").Trim();
    //            if (String.IsNullOrEmpty(sourceFile)) throw new ArgumentException($"Vstupní soubor není zadán.");
    //            if (String.IsNullOrEmpty(targetPath)) throw new ArgumentException($"Výstupní adresář není zadán.");
    //            if (!System.IO.File.Exists(sourceFile)) throw new ArgumentException($"Vstupní soubor '{sourceFile}' neexistuje.");
    //            var name = System.IO.Path.GetFileName(sourceFile);
    //            var targetFile = System.IO.Path.Combine(targetPath, name);

    //            var request = new FileCopierRequest() { SourceFile = sourceFile, DestinationFile = targetFile };
    //            var copier = new FileCopierWin32();
    //            copier.Progress += _Copier_Progress;
    //       //     copier.CopyWithErrorRecovery(request);
    //        }
    //        catch (Exception exc)
    //        {
    //            App.ShowWarning(exc.Message);
    //        }
    //    }
    //    /// <summary>
    //    /// Progress handler volaný z <see cref="FileCopierWin32.CopyWithErrorRecovery(string, string, int, int)"/>
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void _Copier_Progress(object sender, FileReadProgressEventArgs e)
    //    {
    //        switch (e.State)
    //        {
    //            case ProgressStateType.Begin:
    //                _CallProgress(new CopyDataProgressArgs() { FileName = e.FileMap.FileName, SingleProgressTotal = e.TotalFileSize, SingleProgressValue = 0 });
    //                break; ;
    //            case ProgressStateType.Read:
    //                _CallProgress(new CopyDataProgressArgs() { SingleProgressValue = e.BlockEndPosition, FileCopyInfo = getInfo(e.BlockEndPosition, e.TotalFileSize) });
    //                break; ;
    //        }

    //        if (CancelRequest)
    //            e.Cancel = true;


    //        string getInfo(long current, long total)
    //        {
    //            if (current > total) current = total;
    //            decimal percent = Math.Round(100m * (decimal)current / (decimal)total, 1);
    //            long currentKb = current / 1024L;
    //            long totalKb = total / 1024L;
    //            return $"{percent:F1}%   [ {currentKb:N0} KB / {totalKb:N0} KB ]";
    //        }

    //    }
    //    private void _CallProgress(CopyDataProgressArgs args)
    //    {
    //        if (ProgressChanged != null)
    //            ProgressChanged(this, args);
    //    }
        
    //    internal event EventHandler<CopyDataProgressArgs> ProgressChanged;


        /*          starý kód

        private void _RunCopyFileOld(string sourceFile, string targetPath)
        {
            try
            {
                sourceFile = (sourceFile ?? "").Trim();
                targetPath = (targetPath ?? "").Trim();
                if (String.IsNullOrEmpty(sourceFile)) throw new ArgumentException($"Vstupní soubor není zadán.");
                if (String.IsNullOrEmpty(targetPath)) throw new ArgumentException($"Výstupní adresář není zadán.");
                if (!System.IO.File.Exists(sourceFile)) throw new ArgumentException($"Vstupní soubor '{sourceFile}' neexistuje.");
                var name = System.IO.Path.GetFileName(sourceFile);
                var targetFile = System.IO.Path.Combine(targetPath, name);
                var fileMapInfo = new FileMapInfo() { FileName = sourceFile, ErrorBlocks = new List<FileMapBlock>() };

                long offset = 0L;
                long length = 0L;
                using (var sourceStream = System.IO.File.OpenRead(sourceFile))
                using (var targetStream = System.IO.File.OpenWrite(targetFile))
                {
                    length = sourceStream.Length;
                    fileMapInfo.Length = length;
                    _CallProgressBegin(name, length);

                    while (true)
                    {
                        _CallProgressStep(getInfo(offset + _BlockSizeDefault, length), offset, 0);
                        var size = _RunCopyBuffer(fileMapInfo, sourceStream, targetStream, offset);
                        if (CancelRequest) break;
                        if (size == 0) break;
                        offset += size;
                    }
                }
            }
            catch (Exception exc)
            {
                App.ShowWarning(exc.Message);
            }



            string getInfo(long current, long total)
            {
                if (current > total) current = total;
                decimal percent = Math.Round(100m * (decimal)current / (decimal)total, 1);
                long currentKb = current / 1024L;
                long totalKb = total / 1024L;
                return $"{percent:F1}%   [ {currentKb:N0} KB / {totalKb:N0} KB ]";
            }
        }


        /// <summary>
        /// Účelem metody je překopírovat nějaký kousek zdrojového streamu, počínaje daným offsetem do cílového streamu.
        /// Nejprve kopíruje větší buffer, a pokud dojde k chybě, pak buffer zmenšuje (přenáší menší blok a doufá, že v menší oblasti nebude chyba).
        /// Každý pokus opakuje <see cref="RetryCount"/> krát.
        /// Poté po chybě zmenší buffer na polovinu.
        /// Jakmile dosáhne dolní velikosti 4KB a stále je chyba, započte to jako nečitelný blok a do výstupu zapíše 0x00 na místo tohoto bloku.
        /// Vrátí velikost načteného bloku (nebo 4KB po finální chybě).
        /// Metoda sam by neměla vyhodit chybu.
        /// 
        /// Po každé chybě čtení chvilku počká (100 milisec).
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="targetStream"></param>
        /// <param name="sourceOffset"></param>
        /// <returns></returns>
        private int _RunCopyBuffer(FileMapInfo fileMapInfo, System.IO.FileStream sourceStream, System.IO.FileStream targetStream, long sourceOffset)
        {
            int retryCount = this.RetryCount ?? 3;
            retryCount = (retryCount < 0 ? 0 : (retryCount > 7 ? 7 : retryCount));
            int attemptCount = retryCount + 1;

            int resultSize = 0;

            int bufferSize = _BlockSizeDefault;
            int bufferMini = _BlockSizeMinimal;

            while (true)
            {
                var size = _RunCopyBufferSingle(fileMapInfo, sourceStream, targetStream, sourceOffset, bufferSize);
                if (size >= 0) { resultSize = size; break; }
                if (CancelRequest) { resultSize = 0; break; }

                // Tak nám došlo k chybě... A tady se rozhodneme, co s ní budeme dělat:
                // Odečteme jeden pokus...
                attemptCount--;
                if (attemptCount == 0)
                {   // Vyčerpali jsme všechny pokusy:
                    if (bufferSize <= bufferMini)
                    {   // A už nyní máme buffer na dolní hranici:
                        // Do výstupního souboru vepíšeme prázdnou hodnotu a započítáme tuto chybu,
                        //  vstupní soubor posuneme na další pozici a vrátíme tuto velikost jako realizovanou:
                        resultSize = _RunFillBufferSingle(fileMapInfo, sourceStream, targetStream, sourceOffset, bufferSize);
                        break;
                    }
                    // Vyčerpali jsme např 3 pokusy s výchozí velikostí => následovat budou další tři pokusy s bufferem o minimální velikosti:
                    bufferSize = bufferMini;
                    attemptCount = retryCount + 1;
                }
                else
                {   // Zatím jsme nevyčerpali všechny dané pokusy:
                    attemptCount--;
                }

                // Zkusíme to tedy znovu = po malé pauze, od výchozí pozice ve sourceStream:
                System.Threading.Thread.Sleep(25);
                sourceStream.Seek(sourceOffset, System.IO.SeekOrigin.Begin);
            }

            return resultSize;
        }
        /// <summary>
        /// Zkopíruje blok, v případě chyby vrátí -1.
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="targetStream"></param>
        /// <param name="sourceOffset"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        private int _RunCopyBufferSingle(FileMapInfo fileMapInfo, System.IO.FileStream sourceStream, System.IO.FileStream targetStream, long sourceOffset, int bufferSize)
        {
            int size = 0;
            try
            {
                var buffer = new byte[bufferSize];
                size = sourceStream.Read(buffer, 0, bufferSize);
                if (size > 0)
                    targetStream.Write(buffer, 0, size);
            }
            catch (Exception exc)
            {
                size = -1;
            }
            return size;
        }
        /// <summary>
        /// Do výstupního streamu vepíše prázdný blok, a vstupní stream jen posune.
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="targetStream"></param>
        /// <param name="sourceOffset"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        private int _RunFillBufferSingle(FileMapInfo fileMapInfo, System.IO.FileStream sourceStream, System.IO.FileStream targetStream, long sourceOffset, int bufferSize, byte[] emptySample = null)
        {
            if (emptySample is null || emptySample.Length == 0)
                emptySample = new byte[] { 0x00 };
            int esl = emptySample.Length;

            var buffer = new byte[bufferSize];
            for (int i = 0; i < bufferSize; i++)
                buffer[i] = emptySample[i % esl];

            targetStream.Write(buffer, 0, bufferSize);

            fileMapInfo.ErrorBlocks.Add(new FileMapBlock() { Begin = sourceOffset, Length = bufferSize });

            sourceStream.Seek((sourceOffset + bufferSize), System.IO.SeekOrigin.Begin);
            return bufferSize;
        }

        private void _CallProgressBegin(string fileName, long progressTotal)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, new CopyDataProgressArgs()
                {
                    FileName = fileName,
                    SingleProgressTotal = progressTotal,
                    SingleProgressValue = 0
                });
        }
        private void _CallProgressStep(string fileInfo, long? progressValue, int? errorCount)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, new CopyDataProgressArgs()
                {
                    FileCopyInfo = fileInfo,
                    SingleProgressValue = progressValue,
                    SingleErrorCount = errorCount
                });
        }

        private int _BlockSizeDefault { get { return 16384; } }
        private int _BlockSizeMinimal { get { return 4096; } }

        */
    // }

    //internal class CopyDataProgressArgs : EventArgs
    //{
    //    public string FileName { get; set; }
    //    public string FileCopyInfo { get; set; }
    //    public long? SingleProgressTotal { get; set; }
    //    public long? SingleProgressValue { get; set; }
    //    public int? SingleErrorCount { get; set; }
    //    public int? SingleErrorPercent { get; set; }
    //}
}
