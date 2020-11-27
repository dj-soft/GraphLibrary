namespace Djs.Tools.WebDownloader.UI
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this._StatusStrip = new System.Windows.Forms.StatusStrip();
            this._StatusInfoLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this._ToolStrip = new System.Windows.Forms.ToolStrip();
            this._ToolOpenDrop = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem9 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem10 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem11 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripMenuItem();
            this._ToolDeleteBtn = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this._SaveButton = new System.Windows.Forms.ToolStripSplitButton();
            this._ToolSaveAuto = new System.Windows.Forms.ToolStripMenuItem();
            this._ToolSaveOnDownload = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this._DirectoryCleanBtn = new System.Windows.Forms.ToolStripButton();
            this._MainSpl = new System.Windows.Forms.SplitContainer();
            this._InputSpl = new System.Windows.Forms.SplitContainer();
            this._SamplePanel = new Djs.Tools.WebDownloader.Download.WebSamplePanel();
            this._AdressPanel = new Djs.Tools.WebDownloader.Download.WebAdressPanel();
            this._DownloadPanel = new Djs.Tools.WebDownloader.Download.WebDownloadPanel();
            this.MainTabControl = new System.Windows.Forms.TabControl();
            this.TagbPageDownloader = new System.Windows.Forms.TabPage();
            this.TabPageCleanDir = new System.Windows.Forms.TabPage();
            this.ImageList = new System.Windows.Forms.ImageList(this.components);
            this._StatusStrip.SuspendLayout();
            this._ToolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._MainSpl)).BeginInit();
            this._MainSpl.Panel1.SuspendLayout();
            this._MainSpl.Panel2.SuspendLayout();
            this._MainSpl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._InputSpl)).BeginInit();
            this._InputSpl.Panel1.SuspendLayout();
            this._InputSpl.Panel2.SuspendLayout();
            this._InputSpl.SuspendLayout();
            this.MainTabControl.SuspendLayout();
            this.TagbPageDownloader.SuspendLayout();
            this.SuspendLayout();
            // 
            // _StatusStrip
            // 
            this._StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._StatusInfoLabel});
            this._StatusStrip.Location = new System.Drawing.Point(0, 722);
            this._StatusStrip.Name = "_StatusStrip";
            this._StatusStrip.Size = new System.Drawing.Size(1384, 22);
            this._StatusStrip.TabIndex = 6;
            this._StatusStrip.Text = "_StatusStrip";
            // 
            // _StatusInfoLabel
            // 
            this._StatusInfoLabel.Name = "_StatusInfoLabel";
            this._StatusInfoLabel.Size = new System.Drawing.Size(1425, 17);
            this._StatusInfoLabel.Spring = true;
            this._StatusInfoLabel.Text = "Informace...";
            this._StatusInfoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _ToolStrip
            // 
            this._ToolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this._ToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._ToolOpenDrop,
            this._ToolDeleteBtn,
            this.toolStripSeparator1,
            this._SaveButton,
            this.toolStripSeparator3,
            this._DirectoryCleanBtn});
            this._ToolStrip.Location = new System.Drawing.Point(0, 0);
            this._ToolStrip.Name = "_ToolStrip";
            this._ToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this._ToolStrip.Size = new System.Drawing.Size(1384, 31);
            this._ToolStrip.TabIndex = 7;
            this._ToolStrip.Text = "_ToolStrip";
            // 
            // _ToolOpenDrop
            // 
            this._ToolOpenDrop.AutoSize = false;
            this._ToolOpenDrop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this._ToolOpenDrop.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem3,
            this.toolStripMenuItem4,
            this.toolStripMenuItem5,
            this.toolStripSeparator2,
            this.toolStripMenuItem6,
            this.toolStripMenuItem7,
            this.toolStripMenuItem8});
            this._ToolOpenDrop.Image = ((System.Drawing.Image)(resources.GetObject("_ToolOpenDrop.Image")));
            this._ToolOpenDrop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._ToolOpenDrop.Name = "_ToolOpenDrop";
            this._ToolOpenDrop.Size = new System.Drawing.Size(480, 22);
            this._ToolOpenDrop.Text = "Načíst definici...";
            this._ToolOpenDrop.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._ToolOpenDrop.ToolTipText = "Načíst definici z definic dříve uložených";
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(180, 22);
            this.toolStripMenuItem3.Text = "toolStripMenuItem3";
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(180, 22);
            this.toolStripMenuItem4.Text = "toolStripMenuItem4";
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(180, 22);
            this.toolStripMenuItem5.Text = "toolStripMenuItem5";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(177, 6);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(180, 22);
            this.toolStripMenuItem6.Text = "toolStripMenuItem6";
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem9,
            this.toolStripMenuItem10,
            this.toolStripMenuItem11});
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            this.toolStripMenuItem7.Size = new System.Drawing.Size(180, 22);
            this.toolStripMenuItem7.Text = "toolStripMenuItem7";
            // 
            // toolStripMenuItem9
            // 
            this.toolStripMenuItem9.Name = "toolStripMenuItem9";
            this.toolStripMenuItem9.Size = new System.Drawing.Size(186, 22);
            this.toolStripMenuItem9.Text = "toolStripMenuItem9";
            // 
            // toolStripMenuItem10
            // 
            this.toolStripMenuItem10.Name = "toolStripMenuItem10";
            this.toolStripMenuItem10.Size = new System.Drawing.Size(186, 22);
            this.toolStripMenuItem10.Text = "toolStripMenuItem10";
            // 
            // toolStripMenuItem11
            // 
            this.toolStripMenuItem11.Name = "toolStripMenuItem11";
            this.toolStripMenuItem11.Size = new System.Drawing.Size(186, 22);
            this.toolStripMenuItem11.Text = "toolStripMenuItem11";
            // 
            // toolStripMenuItem8
            // 
            this.toolStripMenuItem8.Name = "toolStripMenuItem8";
            this.toolStripMenuItem8.Size = new System.Drawing.Size(180, 22);
            this.toolStripMenuItem8.Text = "toolStripMenuItem8";
            // 
            // _ToolDeleteBtn
            // 
            this._ToolDeleteBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._ToolDeleteBtn.Image = global::Djs.Tools.WebDownloader.Properties.Resources.archive_remove;
            this._ToolDeleteBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._ToolDeleteBtn.Name = "_ToolDeleteBtn";
            this._ToolDeleteBtn.Size = new System.Drawing.Size(28, 28);
            this._ToolDeleteBtn.Text = "Delete";
            this._ToolDeleteBtn.ToolTipText = "Smazat aktuální definici z nabídky";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 31);
            // 
            // _SaveButton
            // 
            this._SaveButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._ToolSaveAuto,
            this._ToolSaveOnDownload});
            this._SaveButton.Image = global::Djs.Tools.WebDownloader.Properties.Resources.media_floppy_3_5_3;
            this._SaveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._SaveButton.Name = "_SaveButton";
            this._SaveButton.Size = new System.Drawing.Size(77, 28);
            this._SaveButton.Text = "Uložit";
            this._SaveButton.ToolTipText = "Uložit aktuální definici nyní";
            this._SaveButton.ButtonClick += new System.EventHandler(this._SaveButton_ButtonClick);
            this._SaveButton.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this._SaveButton_DropDownItemClicked);
            // 
            // _ToolSaveAuto
            // 
            this._ToolSaveAuto.Name = "_ToolSaveAuto";
            this._ToolSaveAuto.Size = new System.Drawing.Size(194, 22);
            this._ToolSaveAuto.Text = "Ukládat automaticky";
            this._ToolSaveAuto.ToolTipText = "Ukládá definici při každé její změně";
            // 
            // _ToolSaveOnDownload
            // 
            this._ToolSaveOnDownload.Image = global::Djs.Tools.WebDownloader.Properties.Resources.dialog_accept;
            this._ToolSaveOnDownload.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this._ToolSaveOnDownload.Name = "_ToolSaveOnDownload";
            this._ToolSaveOnDownload.Size = new System.Drawing.Size(194, 22);
            this._ToolSaveOnDownload.Text = "Ukládat při downloadu";
            this._ToolSaveOnDownload.ToolTipText = "Ukládá definici v průběhu downloadu po úspěšném downloadu každého jednoho souboru" +
    "";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 31);
            // 
            // _DirectoryCleanBtn
            // 
            this._DirectoryCleanBtn.Image = global::Djs.Tools.WebDownloader.Properties.Resources.edit_clear_2;
            this._DirectoryCleanBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._DirectoryCleanBtn.Name = "_DirectoryCleanBtn";
            this._DirectoryCleanBtn.Size = new System.Drawing.Size(127, 28);
            this._DirectoryCleanBtn.Text = "Čištění adresáře...";
            this._DirectoryCleanBtn.Click += new System.EventHandler(this._DirectoryCleanBtn_Click);
            // 
            // _MainSpl
            // 
            this._MainSpl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._MainSpl.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this._MainSpl.Location = new System.Drawing.Point(3, 3);
            this._MainSpl.Name = "_MainSpl";
            this._MainSpl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // _MainSpl.Panel1
            // 
            this._MainSpl.Panel1.Controls.Add(this._InputSpl);
            this._MainSpl.Panel1MinSize = 280;
            // 
            // _MainSpl.Panel2
            // 
            this._MainSpl.Panel2.Controls.Add(this._DownloadPanel);
            this._MainSpl.Size = new System.Drawing.Size(1370, 645);
            this._MainSpl.SplitterDistance = 320;
            this._MainSpl.TabIndex = 5;
            // 
            // _InputSpl
            // 
            this._InputSpl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._InputSpl.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this._InputSpl.IsSplitterFixed = true;
            this._InputSpl.Location = new System.Drawing.Point(0, 0);
            this._InputSpl.Name = "_InputSpl";
            this._InputSpl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // _InputSpl.Panel1
            // 
            this._InputSpl.Panel1.Controls.Add(this._SamplePanel);
            this._InputSpl.Panel1MinSize = 60;
            // 
            // _InputSpl.Panel2
            // 
            this._InputSpl.Panel2.Controls.Add(this._AdressPanel);
            this._InputSpl.Panel2MinSize = 150;
            this._InputSpl.Size = new System.Drawing.Size(1370, 320);
            this._InputSpl.SplitterDistance = 71;
            this._InputSpl.SplitterWidth = 1;
            this._InputSpl.TabIndex = 6;
            // 
            // _SamplePanel
            // 
            this._SamplePanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._SamplePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._SamplePanel.Location = new System.Drawing.Point(0, 0);
            this._SamplePanel.Name = "_SamplePanel";
            this._SamplePanel.SampleText = "";
            this._SamplePanel.Size = new System.Drawing.Size(1370, 71);
            this._SamplePanel.TabIndex = 0;
            // 
            // _AdressPanel
            // 
            this._AdressPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._AdressPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._AdressPanel.Location = new System.Drawing.Point(0, 0);
            this._AdressPanel.Name = "_AdressPanel";
            this._AdressPanel.Size = new System.Drawing.Size(1370, 248);
            this._AdressPanel.TabIndex = 3;
            // 
            // _DownloadPanel
            // 
            this._DownloadPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._DownloadPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._DownloadPanel.Location = new System.Drawing.Point(0, 0);
            this._DownloadPanel.MinimumSize = new System.Drawing.Size(500, 203);
            this._DownloadPanel.Name = "_DownloadPanel";
            this._DownloadPanel.Size = new System.Drawing.Size(1370, 321);
            this._DownloadPanel.TabIndex = 4;
            this._DownloadPanel.TargetPath = "";
            // 
            // MainTabControl
            // 
            this.MainTabControl.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.MainTabControl.Controls.Add(this.TagbPageDownloader);
            this.MainTabControl.Controls.Add(this.TabPageCleanDir);
            this.MainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainTabControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.MainTabControl.ImageList = this.ImageList;
            this.MainTabControl.ItemSize = new System.Drawing.Size(101, 32);
            this.MainTabControl.Location = new System.Drawing.Point(0, 31);
            this.MainTabControl.Name = "MainTabControl";
            this.MainTabControl.SelectedIndex = 0;
            this.MainTabControl.Size = new System.Drawing.Size(1384, 691);
            this.MainTabControl.TabIndex = 8;
            // 
            // TagbPageDownloader
            // 
            this.TagbPageDownloader.Controls.Add(this._MainSpl);
            this.TagbPageDownloader.ImageKey = "download-later.png";
            this.TagbPageDownloader.Location = new System.Drawing.Point(4, 36);
            this.TagbPageDownloader.Name = "TagbPageDownloader";
            this.TagbPageDownloader.Padding = new System.Windows.Forms.Padding(3);
            this.TagbPageDownloader.Size = new System.Drawing.Size(1376, 651);
            this.TagbPageDownloader.TabIndex = 0;
            this.TagbPageDownloader.Text = "Download";
            this.TagbPageDownloader.UseVisualStyleBackColor = true;
            // 
            // TabPageCleanDir
            // 
            this.TabPageCleanDir.ImageKey = "edit-clear-2.png";
            this.TabPageCleanDir.Location = new System.Drawing.Point(4, 36);
            this.TabPageCleanDir.Name = "TabPageCleanDir";
            this.TabPageCleanDir.Padding = new System.Windows.Forms.Padding(3);
            this.TabPageCleanDir.Size = new System.Drawing.Size(1432, 697);
            this.TabPageCleanDir.TabIndex = 1;
            this.TabPageCleanDir.Text = "Čištění adresáře";
            this.TabPageCleanDir.UseVisualStyleBackColor = true;
            // 
            // ImageList
            // 
            this.ImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ImageList.ImageStream")));
            this.ImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.ImageList.Images.SetKeyName(0, "download-3.png");
            this.ImageList.Images.SetKeyName(1, "download-later.png");
            this.ImageList.Images.SetKeyName(2, "edit-clear-2.png");
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1384, 744);
            this.Controls.Add(this.MainTabControl);
            this.Controls.Add(this._ToolStrip);
            this.Controls.Add(this._StatusStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "Web Downloader v3.0";
            this._StatusStrip.ResumeLayout(false);
            this._StatusStrip.PerformLayout();
            this._ToolStrip.ResumeLayout(false);
            this._ToolStrip.PerformLayout();
            this._MainSpl.Panel1.ResumeLayout(false);
            this._MainSpl.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._MainSpl)).EndInit();
            this._MainSpl.ResumeLayout(false);
            this._InputSpl.Panel1.ResumeLayout(false);
            this._InputSpl.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._InputSpl)).EndInit();
            this._InputSpl.ResumeLayout(false);
            this.MainTabControl.ResumeLayout(false);
            this.TagbPageDownloader.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Djs.Tools.WebDownloader.Download.WebSamplePanel _SamplePanel;
        private Djs.Tools.WebDownloader.Download.WebAdressPanel _AdressPanel;
        private Djs.Tools.WebDownloader.Download.WebDownloadPanel _DownloadPanel;
        private System.Windows.Forms.SplitContainer _MainSpl;
        private System.Windows.Forms.SplitContainer _InputSpl;
        private System.Windows.Forms.StatusStrip _StatusStrip;
        private System.Windows.Forms.ToolStrip _ToolStrip;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSplitButton _SaveButton;
        private System.Windows.Forms.ToolStripMenuItem _ToolSaveAuto;
        private System.Windows.Forms.ToolStripMenuItem _ToolSaveOnDownload;
        private System.Windows.Forms.ToolStripDropDownButton _ToolOpenDrop;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem7;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem9;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem10;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem11;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem8;
        private System.Windows.Forms.ToolStripButton _ToolDeleteBtn;
        private System.Windows.Forms.ToolStripStatusLabel _StatusInfoLabel;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton _DirectoryCleanBtn;
        private System.Windows.Forms.TabControl MainTabControl;
        private System.Windows.Forms.TabPage TagbPageDownloader;
        private System.Windows.Forms.TabPage TabPageCleanDir;
        private System.Windows.Forms.ImageList ImageList;
    }
}

