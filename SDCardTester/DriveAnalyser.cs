﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;

namespace DjSoftSDCardTester
{
    /// <summary>
    /// Analyzer stavu disku
    /// </summary>
    public class DriveAnalyser
    {
        #region Konstrukce a základní data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="drive"></param>
        public DriveAnalyser()
        {
            this.InitGroups();
            this.AnalyseStepTime = TimeSpan.FromMilliseconds(300d);
        }
        private void InitGroups()
        {
            _FileGroups = FileGroup.GetGroups();
            _FileGroupDict = new Dictionary<string, IFileGroup>();
            foreach (var fileGroup in _FileGroups)
                foreach (var extension in fileGroup.Extensions)
                    _FileGroupDict.Add("." + extension, fileGroup);
            _GroupOther = _FileGroupDict[".?"];
            _GroupRemaining = _FileGroupDict[".*"];
        }
        protected Dictionary<string, IFileGroup> _FileGroupDict;
        protected FileGroup[] _FileGroups;
        protected IFileGroup _GroupOther;
        protected IFileGroup _GroupRemaining;
        protected System.IO.DriveInfo _Drive;
        /// <summary>
        /// Drive pro analýzu
        /// </summary>
        public System.IO.DriveInfo Drive { get { return _Drive; } }
        /// <summary>
        /// Aktuální stav skupin souborů.
        /// </summary>
        public FileGroup[] FileGroups { get { return _FileGroups; } }
        /// <summary>
        /// Text, obsahující název + přípony všech skupin
        /// </summary>
        public string CodeText 
        { 
            get 
            {
                StringBuilder sb = new StringBuilder();
                foreach (var group in this._FileGroups)
                    sb.AppendLine(group.CodeText);
                return sb.ToString();
            }
        }
        /// <summary>
        /// Aktuální stav skupin souborů, typovaný na interní interface
        /// </summary>
        protected IFileGroup[] IFileGroups { get { return _FileGroups; } }
        /// <summary>
        /// Časový interval, po jehož uplynutí se může opakovaně volat událost <see cref="AnalyseStep"/>.
        /// </summary>
        public TimeSpan AnalyseStepTime { get; set; }
        /// <summary>
        /// Událost vyvolaná po změně hodnot v <see cref="FileGroup"/>, mezi dvěma událostmi bude čas nejméně <see cref="AnalyseStepTime"/> i kdyby změny nastaly častěji.
        /// </summary>
        public event EventHandler AnalyseStep;
        /// <summary>
        /// Událost vyvolaná po jakémkoli doběhnutí analýzy, i po chybách.
        /// </summary>
        public event EventHandler AnalyseDone;
        /// <summary>
        /// Jedna skupina souborů. Má více přípon, ale všechny přípony reprezentují jeden druh souborů.
        /// Skupina určuje pořadí ve vizualizaci a barvu, sumarizuje počet a velikost souborů.
        /// </summary>
        public class FileGroup : IFileGroup
        {
            /// <summary>
            /// Vrátí sadu skupin
            /// </summary>
            /// <returns></returns>
            public static FileGroup[] GetGroups()
            {
                int order = 0;
                return new FileGroup[]
                {
                    new FileGroup(++order, "Obrázky", Skin.PictureGroupColor, "bimp bmp cdr cdx cgi cpt eml gif ico img jpe jpeg jpg pcx pdn png raw svg tif tiff"),
                    new FileGroup(++order, "Filmy", Skin.MovieGroupColor, "3gp avi flv m4v mkv mp2 mp4 mpeg mpg mx4 swf ts ts1 ts2 wmv"),
                    new FileGroup(++order, "Audio", Skin.AudioGroupColor, "m3u mp3 mpc wav wma"),
                    new FileGroup(++order, "Dokumenty", Skin.DocumentsGroupColor, "aspx bak css csv doc docx htm html chm map mht ods odt pdf pps ppsx ppt pptx ppx rtf txt wml xhtml xml"),
                    new FileGroup(++order, "Aplikace", Skin.ApplicationGroupColor, "api apl ashx asp asx bar bat bin binary cache cmd com conf config dll drv exe fon hlp ide info ini ion iso java lnk log msi ocx pdb php rdp reg sys theme tmp ttf url user vdi vhd webinfo webm whtt"),
                    new FileGroup(++order, "Programování", Skin.DevelopmentGroupColor, "as bas build cfg cfgs cs csproj db frt frx hegi heli js json jsonp key lng lock lst nupkg pas pbd pbl pbt pbw pfx prjx ps res resx rss sln snk sql sqlite srd srt suo vb vbproj vbs vcxproj vsix vxd x64 xaml xls xlsx xsd xsl xslt"),
                    new FileGroup(++order, "Archivy", Skin.ArchiveGroupColor, "7z arj ar0 ar1 ar2 ar3 ar4 ar5 ar6 ar7 ar8 cab dat gz jar pack rar zip"),
                    new FileGroup(++order, "Ostatní", Skin.OtherSpaceColor, "?"),
                    new FileGroup(++order, "Nezpracováno", Skin.UsedSpaceColor, "*")
                };
            }
            /// <summary>
            /// Privátní konstruktor
            /// </summary>
            /// <param name="order"></param>
            /// <param name="name"></param>
            /// <param name="color"></param>
            /// <param name="extensions"></param>
            private FileGroup(int order, string name, Color color, string extensions)
            {
                this.Order = order;
                this.Name = name;
                this.Color = color;
                this.Extensions = extensions.ToLower().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                this.FilesCount = 0;
                this.TotalLength = 0L;
            }
            /// <summary>
            /// Privátní konstruktor
            /// </summary>
            /// <param name="order"></param>
            /// <param name="name"></param>
            /// <param name="color"></param>
            /// <param name="filesCount"></param>
            /// <param name="totalLength"></param>
            public FileGroup(int order, string name, Color color, int filesCount, long totalLength)
            {
                this.Order = order;
                this.Name = name;
                this.Color = color;
                this.Extensions = new string[0];
                this.FilesCount = filesCount;
                this.TotalLength = totalLength;
            }
            public override string ToString()
            {
                string filesCount = FilesCount.ToString("### ### ### ##0").Trim();
                string totalLength = TotalLength.ToString("### ### ### ### ##0").Trim();
                return $"Group: {Name}; Files: {filesCount}; TotalLength: {totalLength}";
            }
            public int Order { get; private set; }
            public string Name { get; private set; }
            public Color Color { get; private set; }
            public string[] Extensions { get; private set; }
            /// <summary>
            /// Zdejší přípony jako jeden string, seřazené podle abecedy, oddělené mezerou
            /// </summary>
            public string ExtensionsText
            {
                get
                {
                    var extensions = Extensions.ToList();
                    extensions.Sort();
                    StringBuilder sb = new StringBuilder();
                    foreach (var extension in extensions)
                    {
                        if (extension.Length > 0 && extension.Length < 8)
                            sb.Append(" " + extension);
                    }
                    return (sb.Length == 0 ? "" : sb.ToString().Substring(1));
                }
            }
            /// <summary>
            /// Text, obsahující název + přípony
            /// </summary>
            public string CodeText { get { return $"\"{Name}\" : \"{ExtensionsText}\""; } }
            /// <summary>
            /// Nalezené chybějící přípony, které reprezentují 90% prostoru chybějících přípon
            /// </summary>
            public string[] MissingExtensions 
            { 
                get 
                {
                    if (_MissingExtensions is null || _MissingExtensions.Count == 0) return new string[0];

                    var extensions = _MissingExtensions.ToList();              // KeyValuePairs (přípona, její sumární velikost souborů)
                    extensions.Sort((a, b) => b.Value.CompareTo(a.Value));     // ORDER BY Value DESC;
                    long sumLength = extensions.Sum(kvp => kvp.Value);         // SUM(Length)
                    long maxLength = sumLength * 9L / 10L;                     // 90% celkové délky
                    sumLength = 0L;
                    List<string> result = new List<string>();
                    foreach (var kvp in extensions)
                    {
                        if (sumLength >= maxLength) break;

                        string extension = kvp.Key;
                        if (extension.Length > 1 && extension[0] == '.') extension = extension.Substring(1);
                        result.Add(extension);
                        sumLength += kvp.Value;
                    }
                    result.Sort();
                    return result.ToArray();
                }
            }
            /// <summary>
            /// Nalezené chybějící přípony jako jeden string
            /// </summary>
            public string MissingExtensionsText
            {
                get
                {
                    var extensions = MissingExtensions;
                    StringBuilder sb = new StringBuilder();
                    foreach (var extension in extensions)
                    {
                        if (extension.Length > 0 && extension.Length < 8)
                            sb.Append(" " + extension);
                    }
                    return (sb.Length == 0 ? "" : sb.ToString().Substring(1));
                }
            }
            private Dictionary<string, long> _MissingExtensions;
            /// <summary>
            /// Počet souborů v této grupě nalezených
            /// </summary>
            public int FilesCount { get; private set; }
            /// <summary>
            /// Celková délka souborů v této grupě
            /// </summary>
            public long TotalLength { get; private set; }

            void IFileGroup.Reset()
            {
                FilesCount = 0;
                TotalLength = 0L;
            }
            void IFileGroup.Add(int filesCount, long totalLength)
            {
                FilesCount += filesCount;
                TotalLength += totalLength;
            }
            void IFileGroup.AddMissingExtension(string extension, long length)
            {
                if (_MissingExtensions is null) _MissingExtensions = new Dictionary<string, long>();
                if (String.IsNullOrEmpty(extension)) return;
                if (!_MissingExtensions.ContainsKey(extension))
                    _MissingExtensions.Add(extension, 0L);
                _MissingExtensions[extension] = _MissingExtensions[extension] + length;
            }
            string IFileGroup.MissingExtensionsText { get { return MissingExtensionsText; } }
            long IFileGroup.TotalLength { get { return TotalLength; } set { TotalLength = value; } }
        }
        /// <summary>
        /// Interface pro interní přístup na data grupy
        /// </summary>
        protected interface IFileGroup
        {
            void Reset();
            void Add(int filesCount, long totalLength);
            void AddMissingExtension(string extension, long length);
            string MissingExtensionsText { get; }
            long TotalLength { get; set; }
        }
        #endregion
        #region Základní zmapování obsahu souboru a převod na FileGroup
        /// <summary>
        /// Vrátí pole základních informací o využití prostoru daného disku.
        /// </summary>
        /// <param name="drive"></param>
        /// <param name="totalSize"></param>
        /// <returns></returns>
        public static DriveAnalyser.FileGroup[] GetFileGroupsForDrive(System.IO.DriveInfo drive, out long totalSize)
        {
            totalSize = 0L;
            List<DriveAnalyser.FileGroup> fileGroups = new List<FileGroup>();

            if (drive != null)
            {
                totalSize = drive.TotalSize;
                if (totalSize < 0L) totalSize = 0L;

                long usedSize = totalSize - drive.TotalFreeSpace;
                if (usedSize < 0L) usedSize = 0L;
                long otherSize = drive.TotalFreeSpace - drive.AvailableFreeSpace;
                if (otherSize < 0L) otherSize = 0L;
                long freeSize = drive.TotalSize - usedSize - otherSize;
                if (freeSize < 0L) freeSize = 0L;

                long testSize = DriveTester.GetTestFiles(drive).Select(f => f.Length).Sum();
                if (testSize < 0L) testSize = 0L;
                if (testSize > 0L) usedSize -= testSize;
                if (usedSize < 0L) usedSize = 0L;

                int order = 0;
                if (usedSize > 0L) fileGroups.Add(new FileGroup(++order, "Obsazeno", Skin.UsedSpaceColor, 0, usedSize));
                if (testSize > 0L) fileGroups.Add(new FileGroup(++order, "Testovací", Skin.TestFilesGroupColor, 0, testSize));
                if (otherSize > 0L) fileGroups.Add(new FileGroup(++order, "Ostatní", Skin.OtherSpaceColor, 0, otherSize));
            }

            return fileGroups.ToArray();
        }
        #endregion
        #region Běh analýzy
        /// <summary>
        /// Požádá o provedení analýzy daného disku
        /// </summary>
        /// <param name="drive"></param>
        public void BeginAnalyse(System.IO.DriveInfo drive)
        {
            if (drive != null && drive.IsReady && !AnalyseRunning)
                StartAnalyse(drive);
            else
                CallAnalyseDone();
        }
        /// <summary>
        /// Požádá o zastavení běhu analýzy
        /// </summary>
        public void StopAnalyse()
        {
            if (AnalyseRunning)
                AnalyseStopping = true;
        }
        /// <summary>
        /// Zahájení analýzy, zde v threadu volajícího
        /// </summary>
        /// <param name="drive"></param>
        protected void StartAnalyse(System.IO.DriveInfo drive)
        { 
            AnalyseRunning = true;
            AnalyseStopping = false;
            _Drive = drive;
            Task.Factory.StartNew(RunAnalyse);
        }
        /// <summary>
        /// Zahájení analýzy, zde již v threadu Working
        /// </summary>
        protected void RunAnalyse()
        {
            LastStepTime = null;

            foreach (var iGroup in this.IFileGroups) iGroup.Reset();
            this.AnalyseDirectoriesDone = 0;
            this.AnalyseDirectoriesQueue = 0;

            var root = _Drive;
            _GroupRemaining.TotalLength = (root.TotalSize - root.TotalFreeSpace);
            CallAnalyseStep(1, true);

            var nextDirs = new Stack<System.IO.DirectoryInfo>();
            processDirectory(root.RootDirectory);
            CallAnalyseStep(nextDirs.Count);
            while (nextDirs.Count > 0 && !AnalyseStopping)
            {
                processDirectory(nextDirs.Pop());
                CallAnalyseStep(nextDirs.Count);
            }
            CallAnalyseStep(nextDirs.Count, true);

            string missingExtensionsText = _GroupOther.MissingExtensionsText;
            string codeText = this.CodeText;
            CallAnalyseDone();

            // Zpracuje jeden adresář:
            // načte jeho subdirs a files, a roztřídí: subdirs přidá do zásobníku práce nextDirs, files zařadí do patřičné skupiny podle jeho přípony
            void processDirectory(System.IO.DirectoryInfo dirInfo)
            {
                this.AnalyseDirectoriesDone++;
                try
                {
                    var entries = dirInfo.GetFileSystemInfos("*.*", System.IO.SearchOption.TopDirectoryOnly).ToList();
                    entries.Sort((a, b) => String.Compare(a.Name, b.Name));
                    foreach (var entry in entries)
                    {
                        if (entry is System.IO.FileInfo file)
                            addOneFile(file);
                        else if (entry is System.IO.DirectoryInfo subDir)
                            nextDirs.Push(subDir);
                    }
                }
                catch { /* Chyby práv nebo chyby disku */ }
            }

            // Zpracuje jeden soubor:
            // podle jeho přípony najde grupu a do ní přičte jeho velikost, a odečte velikost od _GroupRemaining
            void addOneFile(System.IO.FileInfo file)
            {
                string ext = file.Extension.ToLower();
                if (!this._FileGroupDict.TryGetValue(ext, out var group))
                {
                    group = _GroupOther;
                    if (ext.Length > 0 && file.Length >= 8192L)
                        group.AddMissingExtension(ext, file.Length);
                }
                group.Add(1, file.Length);

                var remainingTotal = _GroupRemaining.TotalLength - file.Length;
                if (remainingTotal < 0L) remainingTotal = 0L;
                _GroupRemaining.TotalLength = remainingTotal;
            }
        }
        /// <summary>
        /// Čas posledního hlášení změny
        /// </summary>
        protected DateTime? LastStepTime;
        /// <summary>
        /// Vyvolá událost <see cref="AnalyseStep"/>, pokud je odpovídající čas
        /// </summary>
        /// <param name="queueCount"></param>
        /// <param name="force"></param>
        protected void CallAnalyseStep(int queueCount, bool force = false)
        {
            var nowTime = DateTime.Now;
            var lastTime = LastStepTime;
            var stepTime = AnalyseStepTime;
            if (force || !lastTime.HasValue || stepTime.TotalMilliseconds <= 0d || (lastTime.HasValue && ((TimeSpan)(nowTime - lastTime.Value) >= stepTime)))
            {
                AnalyseDirectoriesQueue = queueCount;
                AnalyseStep?.Invoke(this, EventArgs.Empty);
                LastStepTime = nowTime;
            }
        }
        /// <summary>
        /// Vyvolá událost <see cref="AnalyseDone"/>
        /// </summary>
        protected void CallAnalyseDone()
        {
            AnalyseDone?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Analýza právě běží?
        /// </summary>
        public bool AnalyseRunning { get; private set; }
        /// <summary>
        /// Je vydán požadavek na zastavení analýzy
        /// </summary>
        public bool AnalyseStopping { get; private set; }
        /// <summary>
        /// Počet adresářů již analyzovaných
        /// </summary>
        public int AnalyseDirectoriesDone { get; private set; }
        /// <summary>
        /// Počet adresářů čekajících ve frontě na zpracování. 
        /// Toto není celkový počet dosud nezpracovaných adresářů, ale jen těch, o kterých už víme a čekají.
        /// </summary>
        public int AnalyseDirectoriesQueue { get; private set; }
        #endregion
    }
    #region Vizuální control pro orientační zobrazení obsahu jedné grupy v přehledném panelu
    /// <summary>
    /// Vizuální control pro orientační zobrazení obsahu jedné grupy v přehledném panelu
    /// </summary>
    public class DriveAnalyseGroupControl : DriveResultControl
    {
        public DriveAnalyseGroupControl(string name, Color color)
            : this()
        {
            this.GroupText = name;
            this.GroupColor = color;
        }
        public DriveAnalyseGroupControl()
            : base()
        {
            this.GroupRatio = 0f;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle clientBounds = this.ClientRectangle;
            Rectangle outerBounds = new Rectangle(clientBounds.X + 1, clientBounds.Y + 1, clientBounds.Width - 2, clientBounds.Height - 2);
            Rectangle innerBounds = new Rectangle(outerBounds.X + 2, outerBounds.Y + 2, outerBounds.Width - 4, outerBounds.Height - 4);
            this.PaintBackground(e, clientBounds, outerBounds, innerBounds);
            this.PaintRatio(e, clientBounds, outerBounds, innerBounds);
            this.PaintText(e, clientBounds, outerBounds, innerBounds);
        }
        private void PaintBackground(PaintEventArgs e, Rectangle clientBounds, Rectangle outerBounds, Rectangle innerBounds)
        {
            Painter.PaintRectangle(e.Graphics, this.BackColor, clientBounds);                           // Šedá barva okolo
            Painter.PaintRectangle(e.Graphics, this.GroupColor, outerBounds);                           // Barevný rámeček bez 3D efektu
        }
        private void PaintRatio(PaintEventArgs e, Rectangle clientBounds, Rectangle outerBounds, Rectangle innerBounds)
        {
            int ratioX = innerBounds.X + (int)(Math.Round((this.GroupRatio * innerBounds.Width), 0));
            Rectangle ratioBounds;

            if (ratioX > innerBounds.X)
            {   // Vykreslit vlevo barvu skupiny ve 3D efektu:
                ratioBounds = new Rectangle(innerBounds.X, innerBounds.Y, ratioX - innerBounds.X, innerBounds.Height);
                Painter.PaintBar3D(e.Graphics, this.GroupColor, ratioBounds);
            }
            if (ratioX < innerBounds.Right)
            {   // Vykreslit vpravo barvu pozadí (jako běžný podklad):
                ratioBounds = new Rectangle(ratioX, innerBounds.Y, innerBounds.Right - ratioX, innerBounds.Height);
                Painter.PaintRectangle(e.Graphics, this.BackColor, ratioBounds);
            }
        }
        private void PaintText(PaintEventArgs e, Rectangle clientBounds, Rectangle outerBounds, Rectangle innerBounds)
        {
            var font = this.Font;
            bool disposeFont = false;
            if (this.IsActive)
            {
                font = new Font(font, FontStyle.Bold);
                disposeFont = true;
            }
            Painter.PaintText(e.Graphics, font, this.GroupText, this.ForeColor, innerBounds, ContentAlignment.MiddleLeft);
            
            string text = Math.Round(100d * this.GroupRatio, 1).ToString() + "%";
            Painter.PaintText(e.Graphics, font, text, this.ForeColor, innerBounds, ContentAlignment.MiddleRight);

            if (disposeFont)
                font.Dispose();
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string GroupText { get; set; }
        /// <summary>
        /// Barva podkladu
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color GroupColor { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double GroupRatio { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsActive { get; set; }
        protected override int CurrentOptimalHeight { get { return OptimalHeight; } }
        /// <summary>
        /// Optimální výška
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static int OptimalHeight { get { return 27; } }
    }
    #endregion
}