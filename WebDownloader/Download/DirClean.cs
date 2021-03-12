using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using IO = System.IO;

namespace Djs.Tools.WebDownloader.Download
{
    /// <summary>
    /// Čistič adresáře
    /// </summary>
    public class DirClean : WebBase
    {
        #region Public
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DirClean()
        {
            DeleteFileMask = "*.htm;*.html;*.tmp;*.css;*.js;*.xml;*thumb*.*;hts-cache";
            DeleteFileSmallerThan = 18000;
            DirectoryResult = _ResultPathDefault;
            HasMoveToResultDir = true;
            HasDeleteEmptyDirs = true;
            TestOnly = true;
        }
        /// <summary>
        /// Spustí čištění
        /// </summary>
        public void Start()
        {
            if (this.IsWorking)
            {
                Dialogs.Warning("Nelze nastartovat úklid, dosud běží.");
                return;
            }

            if (!IsValid)
            {
                Dialogs.Warning("Nelze nastartovat úklid, nejsou zadána platná data.");
                return;
            }
            StartBackThread("CleaningThread");
        }
        public string DirectoryToClean { get; set; }
        public bool RemoveDuplicities { get; set; }
        public string DirectoryResult { get; set; }
        public string DeleteFileMask { get; set; }
        public int DeleteFileSmallerThan { get; set; }
        public bool HasMoveToResultDir { get; set; }
        public bool HasDeleteEmptyDirs { get; set; }
        public int ResultPathFileCount { get; set; }
        public bool TestOnly { get; set; }
        #endregion
        #region Private akce
        /// <summary>
        /// Obsahuje true, když může proběhnout úklid - máme zadaná data
        /// </summary>
        protected bool IsValid
        {
            get
            {
                if (String.IsNullOrEmpty(DirectoryToClean)) return false;
                return true;
            }
        }
        /// <summary>
        /// Vstupní bod akce na pozadí
        /// </summary>
        protected override void RunBackThread()
        {
            PrepareVariables();
            IO.DirectoryInfo dirInfo = new IO.DirectoryInfo(_InputPath);
            CleanDirOne(dirInfo, 0, 0m, 1m);
            _ProgressArgs.SetState(WorkingState.Done);
            DoProgress(true);
        }
        /// <summary>
        /// Úklid jednoho daného adresáře: rekurzivně vyvolá úklid svých podadresářů, na závěr uklidí své soubory, a pokud daný adresář bude na závěr prázdný, pak uklidí i ten.
        /// </summary>
        /// <param name="dirInfo"></param>
        /// <param name="level"></param>
        /// <param name="ratioBegin"></param>
        /// <param name="ratioEnd"></param>
        private void CleanDirOne(IO.DirectoryInfo dirInfo, int level, decimal ratioBegin, decimal ratioEnd)
        {
            if (!dirInfo.Exists) return;

            // Podadresáře, rekurzivně:
            DoProgress(ratioBegin, dirInfo.FullName);                          // První progress není povinný, provede se pouze úplně první (protože LastTime je null)
            var subDirs = dirInfo.GetDirectories("*.*").ToList();

            int stepDir = 3;                                                   // Poměr velikosti kroku na jeden pod-adresář oproti kroku na všechny vlastní soubory
            int count = subDirs.Count;
            if (count > 1) subDirs.Sort((a, b) => String.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
            decimal progressTotal = (decimal)((stepDir * count) + 1);          // Za každý adresář započítáme = (dirStep) jednotek, pro soubory necháme 1 jednotku
            decimal innerRatioStep = (ratioEnd - ratioBegin) / progressTotal;
            int step = 0;
            foreach (var subDir in subDirs)
            {
                bool skipThisDir = false;
                if (level == 0)
                {   // Pokud máme přeskočit Result adresář, pak zjistíme, zda aktuální adresář (subDir) náhodou není právě on:
                    string subDirName = _AddPathDelimiterToEnd(subDir.FullName);
                    skipThisDir = String.Equals(subDirName, _ResultPath, StringComparison.InvariantCultureIgnoreCase);
                }

                if (!skipThisDir)
                {
                    decimal innerRatioBegin = ratioBegin + ((decimal)step * innerRatioStep);       // Pro úplně první SubDir: innerRatioBegin = 0, pro první SubDir v některém adresáři = ratioBegin
                    decimal innerRatioEnd = innerRatioBegin + innerRatioStep;                      // Pro poslední SubDir   : innerRatioEnd = (ratioEnd - innerRatioStep)
                    CleanDirOne(subDir, level + 1, innerRatioBegin, innerRatioEnd);
                }
                step += stepDir;
            }

            // Vlastní soubory:
            decimal ratio = ratioEnd - innerRatioStep;
            DoProgress(ratio, dirInfo.FullName + "\\*.*");                     // Progres pro smazání souborů = poslední jeden krok před ratioEnd 
            var fileInfos = DeleteFilesByConditions(dirInfo);
            if (_MoveToResultPath) MoveFilesToTarget(fileInfos);

            // Zrušení adresáře, pokud je nyní prázdný:
            if (_HasDeleteEmptyDirs) DeleteEmptyDir(dirInfo);

            DoProgress(ratioEnd, null, (level == 0));                          // Progres na konci každého adresáře, pro TopLevel adresář je progress povinný.
        }
        /// <summary>
        /// Naplň ratio progresu a text adresáře, a vyvolej progress ([force])
        /// Pokud je dáno <paramref name="force"/> = true, volej bezpodmínečně. Pokud je false, pak volej progres nejvýše 1x za 100ms.
        /// </summary>
        /// <param name="progressRatio"></param>
        /// <param name="currentDirectory"></param>
        /// <param name="force"></param>
        private void DoProgress(decimal progressRatio, string currentDirectory, bool force = false)
        {
            _ProgressArgs.SetInfo(progressRatio, currentDirectory);
            DoProgress(force);
        }
        /// <summary>
        /// Vyvolej progress event.
        /// Pokud je dáno <paramref name="force"/> = true, volej bezpodmínečně. Pokud je false, pak volej progres nejvýše 1x za 100ms.
        /// </summary>
        /// <param name="force"></param>
        private void DoProgress(bool force = false)
        {
            bool callProgress = (force || !_ProgressLastTime.HasValue);
            DateTime now = DateTime.Now;
            if (!callProgress)
            {
                TimeSpan time = now - _ProgressLastTime.Value;
                double milisecs = time.TotalMilliseconds;
                callProgress = (milisecs < 0d || milisecs >= 100d);
            }
            if (callProgress)
            {
                OnProgress(_ProgressArgs);
                _ProgressLastTime = now;
            }
        }
        /// <summary>
        /// Vyvolá evnthandler <see cref="Progress"/>
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnProgress(DirCleanProgressArgs args)
        {
            Progress?.Invoke(this, args);
        }
        public event EventHandler<DirCleanProgressArgs> Progress;

        /// <summary>
        /// Z dodaného adresáře smaže soubory vyhovující zadání (velikost, masky jména souboru k vymazání)
        /// </summary>
        /// <param name="dirInfo"></param>
        /// <returns></returns>
        private List<IO.FileInfo> DeleteFilesByConditions(IO.DirectoryInfo dirInfo)
        {
            var fileInfos = dirInfo.GetFiles().ToList();
            int count = fileInfos.Count;
            if (count == 0) return fileInfos;
            if (count > 1) fileInfos.Sort((a, b) => String.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));

            if (!_DeleteActive) return fileInfos;                    // Zkratka: pokud nebudeme nic mazat, rovnou vrátíme kompletní seznam souborů.

            var result = new List<IO.FileInfo>();
            long size = _DeleteFileSmallerThan;
            var masks = _DeleteFileMasks;
            bool existsMasks = (masks.Count > 0);
            foreach (var fileInfo in fileInfos)
            {
                bool delete = (fileInfo.Length < size);              // Smazat můžeme soubor, který je menší než limit
                if (!delete && existsMasks)
                    delete = IsFileNameMatchTo(fileInfo.Name, masks);// Anebo Smazat můžeme soubor, který vyhovuje některé masce (přípona nebo název souboru)

                if (delete)
                {
                    DeleteFile(fileInfo);                            // Něco smažeme, informace o DeleteFile přičteme do Args
                    DoProgress();
                }
                else result.Add(fileInfo);                           // A co nesmažeme, to vrátíme k dalšímu zpracování
            }

            return result;
        }
        /// <summary>
        /// Provede přesunutí daných souborů do cílového adresáře.
        /// Řídí jejich přejmenování a řídi počet souborů v cílovém adresáři.
        /// </summary>
        /// <param name="fileInfos"></param>
        private void MoveFilesToTarget(List<IO.FileInfo> fileInfos)
        {
            int count = fileInfos.Count;
            if (count == 0) return;
            int remaining = count;
            int resetAfter;
            string targetDir = GetCurrentTargetDirFor(count, false, out resetAfter);
            if (targetDir == null) return;
            foreach (var fileInfo in fileInfos)
            {
                string targetName = GetTargetName(fileInfo.FullName, targetDir);
                MoveFile(fileInfo, targetName);
                DoProgress();
                _CurrentResultCount++;
                remaining--;
                if ((--resetAfter) <= 0 && remaining > 0)
                {
                    targetDir = GetCurrentTargetDirFor(remaining, true, out resetAfter);
                    if (targetDir == null) return;
                }
            }
        }
        /// <summary>
        /// Vrátí aktuální výstupní adresář pro uložení daného počtu souborů.
        /// </summary>
        /// <param name="fileCount"></param>
        /// <param name="reset"></param>
        /// <param name="resetAfter"></param>
        /// <returns></returns>
        private string GetCurrentTargetDirFor(int fileCount, bool reset, out int resetAfter)
        {
            resetAfter = fileCount;
            if (fileCount == 0) return null;               // Nejsou soubory: vrátím null, akce v MoveFilesToTarget() skončí ihned.

            // V testovacím režimu vracím "": není to null (to by neproběhly žádné akce v MoveFilesToTarget()), tedy akce proběhnou, ale fyzický Move neproběhne.
            // A nebudeme ani vytvářet fyzické výstupní adresáře (to se provádí dole v této metodě).
            if (TestOnly) return "";

            string resultPath = _CurrentResultPath;
            if (!reset && resultPath != null)
            {   // Není povinný reset, a máme existující výstupní adresář - lze jej použít pro vstupní soubory?
                int currentCount = _CurrentResultCount;
                // Trocha logiky:
                //  - Pokud v existujícím výstupním adresáři je méně než (_ResultDirMinCount) souborů => budeme do něj přidávat:
                //    - pokud chceme přidat méně souborů, než abychom celkem dosáhli na (_ResultDirMaxCount) souborů, tak můžeme přidat všechny
                //    - pokud bychom přidali více, že bychom (_ResultDirMaxCount) přesáhli, tak přidáme jen do počtu (_ResultDirTopCount) = optimum, 
                //       a zbývající pak přidáme do dalších podadresářů
                //  - Pokud v existujícím výstupním adresáři je více než nebo rovno (_ResultDirMinCount) souborů:
                //    - pokud se s novými soubory vejdeme do počtu (_ResultDirMaxCount), tak je tam přidáme všechny
                //    - pokud bychom s novými soubory přesáhli počet (_ResultDirMaxCount), tak je do tohoto výstupu už dávat nebudeme a založíme další nový výstupní adresář
                if (currentCount < _ResultDirMinCount)
                {   // V aktuálním výstupu je méně než Min => vždy tam něco přidáme:
                    if ((currentCount + fileCount) <= _ResultDirMaxCount)      // Když do současného výstupu přidáme všechny vstupní soubory, nepřesáhneme Max:
                        resetAfter = fileCount;                                //  do aktuálního výstupu lze přidat všechny vstupní soubory
                    else
                        resetAfter = (_ResultDirMaxCount - currentCount);      // Jinak přidáme jen tolik, abychom dosáhli do Max, a pak provedeme Reset = založí se nový výstupní podadresář
                    return resultPath;
                }
                // V aktuálním adresáři je více než Min souborů: přidáme tam další jen tehdy, když dosud má adresář méně než Top souborů, 
                //   a s celým počtem souborů se vejdeme do Max (=nebudeme už dělit vstupní adresář):
                if (currentCount < _ResultDirTopCount && (currentCount + fileCount) <= _ResultDirMaxCount)
                {
                    resetAfter = fileCount;                                    //  do aktuálního výstupu lze přidat všechny vstupní soubory
                    return resultPath;
                }
                // V aktuálním adresáři je více než Min souborů (=už je dostatečně obsazený), a další soubory se tam už nevejdou všechny => založíme další prázdný adresář:
            }

            string resultRoot = _ResultPath;
            if (!IO.Directory.Exists(resultRoot)) IO.Directory.CreateDirectory(resultRoot);

            // Musíme najít další podadresář = najdeme poslední existující adresář s odpovídajícím jménem, a přidáme +1 k jeho číslu:
            string prefix = _ResultDirPrefix.Trim();                           // "W"
            int numLength = _ResultNumLength;                                  // Výstupní jednotlivé adresáře mají tvar: "W00025" = prefix + 5 číslic
            var direcories = new IO.DirectoryInfo(resultRoot).GetDirectories();
            int maxNumber = 0;
            foreach (var direcory in direcories) SelectMaxNumber(direcory.Name, prefix, numLength, ref maxNumber);

            maxNumber++;
            string newName = prefix + (maxNumber.ToString().PadLeft(numLength, '0'));
            resultPath = IO.Path.Combine(resultRoot, newName);
            IO.Directory.CreateDirectory(resultPath);
            _CurrentResultPath = resultPath;
            _CurrentResultCount = 0;

            // Kolik souborů dáme do nového výstupního adresáře?
            //  - Pokud celý vstupní adresář se vejde do počtu Max, dáme tam všechny vstupní soubory (udržíme je pohromadě);
            //  - Pokud na vstupu je víc než max souborů, budeme je stejně muset rozdělit do více adresářů, a tak jich dáme jen Top (=horní optimum):
            resetAfter = (fileCount <= _ResultDirMaxCount ? fileCount : _ResultDirTopCount);

            return resultPath;
        }
        /// <summary>
        /// Najd číslo adresáře a střádá jeho Max() do <paramref name="maxNumber"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prefix"></param>
        /// <param name="numLength"></param>
        /// <param name="maxNumber"></param>
        private void SelectMaxNumber(string name, string prefix, int numLength, ref int maxNumber)
        {
            int value;
            if (IsNameAsResult(name, prefix, numLength, out value) && value > maxNumber)
                maxNumber = value;
        }
        /// <summary>
        /// Vrátí true, pokud dané holé jméno adresáře začíná daným prefixem, a za ním následuje číslo dlouhé <paramref name="numLength"/> znaků.
        /// Pak ukládá hodnotu toho čísla do out <paramref name="value"/>. Jinak vrací false.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prefix"></param>
        /// <param name="numLength"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool IsNameAsResult(string name, string prefix, int numLength, out int value)
        {
            int prefixLength = prefix.Length;
            if (name.Length == (prefixLength + numLength) && name.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
            {
                string text = name.Substring(prefixLength);
                if (Int32.TryParse(text, out value) && value > 0)
                    return true;
            }
            value = 0;
            return false;
        }
        /// <summary>
        /// Metoda vrátí Fullname pro cílové umístění daného souboru.
        /// Soubor se bude nacházet v dodaném adresáři <paramref name="targetDir"/>, 
        /// a jeho holé jméno bude obsahovat celý podadresář ze sourceName relativně k domácímu Root adresáři souboru (<see cref="_InputPath"/>).
        /// </summary>
        /// <param name="sourceName"></param>
        /// <param name="targetDir"></param>
        /// <returns></returns>
        private string GetTargetName(string sourceName, string targetDir)
        {
            // Mějme Root adresář:            f:\WebPages\NudesPuri\
            // Pak tedy _SourcePathLength =   1234567890123456789012 = 22
            // Mějme soubor sourceName:       f:\WebPages\NudesPuri\img0.dditscdn.com\ff268cab8d9fbae1ed7506f97496274f17\7dc612d715c13ce96361af7916dd4fa9_erotic_800x6002aad.jpg
            // Odřízneme část od znaku 22 =   01234567890123456789012
            //   name =                                             img0.dditscdn.com\ff268cab8d9fbae1ed7506f97496274f17\7dc612d715c13ce96361af7916dd4fa9_erotic_800x6002aad.jpg
            // Nahradíme lomítka něčím jiným                        img0.dditscdn.com#ff268cab8d9fbae1ed7506f97496274f17#7dc612d715c13ce96361af7916dd4fa9_erotic_800x6002aad.jpg
            string name = sourceName.Substring(_InputPathLength);
            name = name.Replace("\\", "~");
            string targetName = IO.Path.Combine(targetDir, name);
            if (targetName.Length > 250)
            {   // Vyřešíme extra dlouhé jméno souboru:
                int remove = targetName.Length - 242;
                int suffix = ++_ResultShortNameSuffix;
                string n = IO.Path.GetFileNameWithoutExtension(name).Substring(0, name.Length - remove) + "~" + suffix.ToString("0000000");
                string e = IO.Path.GetExtension(name);
                targetName = IO.Path.Combine(targetDir, (n + e));
            }
            return targetName;
        }
        /// <summary>
        /// Zajistí smazání daného adresáře, pokud je zcela prázdný.
        /// </summary>
        /// <param name="dirInfo"></param>
        private void DeleteEmptyDir(IO.DirectoryInfo dirInfo)
        {
            if (TestOnly) return;

            var items = dirInfo.GetFileSystemInfos();
            if (items.Length == 0)
                DeleteDirectory(dirInfo);
        }
        /// <summary>
        /// Smaže jeden daný soubor
        /// </summary>
        /// <param name="sourceFile"></param>
        private void MoveFile(IO.FileInfo sourceFile, string targetName)
        {
            _ProgressArgs.AddMovedFile(sourceFile);
            if (TestOnly) return;

            try
            {
                bool isReadOnly = sourceFile.IsReadOnly;
                if (isReadOnly) sourceFile.IsReadOnly = false;
                sourceFile.MoveTo(targetName);
                IO.FileInfo targetFile = new IO.FileInfo(targetName);
                if (targetFile.Exists)
                {
                    if (isReadOnly) targetFile.IsReadOnly = true;
                    targetFile.CreationTimeUtc = sourceFile.CreationTimeUtc;
                    targetFile.LastWriteTimeUtc = sourceFile.LastWriteTimeUtc;
                }
            }
            catch (Exception) { }
        }
        /// <summary>
        /// Smaže jeden daný soubor
        /// </summary>
        /// <param name="deleteFile"></param>
        private void DeleteFile(IO.FileInfo deleteFile)
        {
            _ProgressArgs.AddDeletedFile(deleteFile);
            if (TestOnly) return;

            try { deleteFile.Delete(); }
            catch (Exception) { }
        }
        /// <summary>
        /// Smaže jeden daný adresář
        /// </summary>
        /// <param name="dirInfo"></param>
        private void DeleteDirectory(IO.DirectoryInfo dirInfo)
        {
            if (TestOnly) return;

            try { dirInfo.Delete(); dirInfo.Refresh(); }
            catch (Exception) { }
        }
        /// <summary>
        /// Z dodané sady wildcard masek (odděleny středníkem) vrátí pole Regex výrazů pro jejich filtrování.
        /// Pokud je na vstupu Empty, vrací prázdné pole.
        /// Typický vstup: "*.tmp; *.js; *thumb*.*; *.htm*;" atd
        /// </summary>
        /// <param name="fileMask"></param>
        /// <returns></returns>
        private List<Regex> CreateRegexes(string fileMask)
        {
            List<Regex> regexes = new List<Regex>();
            if (!String.IsNullOrEmpty(fileMask))
            {
                string[] masks = fileMask.Trim().Split(';');
                foreach (string mask in masks)
                {
                    if (!String.IsNullOrEmpty(mask))
                    {
                        Regex regex = RegexSupport.CreateWildcardsRegex(mask.Trim());
                        if (regex != null)
                            regexes.Add(regex);
                    }
                }
            }

            return regexes;
        }
        /// <summary>
        /// Vrátí true, pokud dané jméno souboru vyhovuje některé masce
        /// </summary>
        /// <param name="name"></param>
        /// <param name="masks"></param>
        /// <returns></returns>
        private bool IsFileNameMatchTo(string name, IEnumerable<Regex> masks)
        {
            if (masks == null) return false;
            return masks.Any(mask => mask.IsMatch(name));
        }
        /// <summary>
        /// Vrátí daný adresář Trim() a končící lomítkem
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string _AddPathDelimiterToEnd(string path)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentException("Zadaný adresář nesmí být prázdný!");
            path = path.Trim();
            if (!path.EndsWith("\\"))
                path = path + "\\";
            return path;
        }
        /// <summary>
        /// Připraví provozní proměnné
        /// </summary>
        private void PrepareVariables()
        {
            _InputPath = _AddPathDelimiterToEnd(DirectoryToClean.Trim());
            _InputPathLength = _InputPath.Length;
            _RemoveDuplicities = RemoveDuplicities;
            _DeleteFileSmallerThan = DeleteFileSmallerThan;
            _DeleteFileMasks = CreateRegexes(DeleteFileMask);
            _DeleteActive = (_DeleteFileMasks.Count > 0 || _DeleteFileSmallerThan > 0);
            _MoveToResultPath = HasMoveToResultDir;

            string resultPath = DirectoryResult;
            if (String.IsNullOrEmpty(resultPath)) resultPath = _ResultPathDefault;                           // Nezadaný výstupní adresář: použije se defaultní název
            resultPath = resultPath.Trim();
            if (!IO.Path.IsPathRooted(resultPath)) resultPath = IO.Path.Combine(_InputPath, resultPath);     // Výstupní adresář může být dán absolutní nebo relativní
            _ResultPath = _AddPathDelimiterToEnd(resultPath);

            _ResultDirPrefix = "W";
            _ResultNumLength = 5;
            _ResultShortNameSuffix = 0;
            _CurrentResultPath = null;
            _CurrentResultCount = 0;
            _ResultDirMinCount = 4000;
            _ResultDirTopCount = 5000;
            _ResultDirMaxCount = 6000;
            _HasDeleteEmptyDirs = HasDeleteEmptyDirs;
            _ProgressLastTime = null;
            _ProgressArgs = new DirCleanProgressArgs();
        }
        /// <summary>
        /// Adresář vstupní = ten budeme čistit. Text vždy končí lomítkem.
        /// </summary>
        private string _InputPath;
        /// <summary>
        /// Délka jména vstupního adresáře <see cref="_InputPath"/>, včetně lomítka.
        /// </summary>
        private int _InputPathLength;
        /// <summary>
        /// Odstranit duplicitní soubory
        /// </summary>
        private bool _RemoveDuplicities;
        /// <summary>
        /// Mezní velikost souborů: menší soubory budou smazány. 0 = nemazat nikdy.
        /// </summary>
        private int _DeleteFileSmallerThan;
        /// <summary>
        /// Masky souborů k vymazání
        /// </summary>
        private List<Regex> _DeleteFileMasks;
        /// <summary>
        /// Příznak, zda bude aktivní smazání souborů (velikost nebo maska)
        /// </summary>
        private bool _DeleteActive;
        /// <summary>
        /// Přesouvat soubory do výstupního adresáře <see cref="_ResultPath"/>?
        /// </summary>
        private bool _MoveToResultPath;
        /// <summary>
        /// Výstupní adresář, jeho root, vždy končí lomítkem
        /// </summary>
        private string _ResultPath;
        /// <summary>
        /// Prefix jména podadresáře pro výstupy, obsahuje typicky "W"
        /// </summary>
        private string _ResultDirPrefix;
        /// <summary>
        /// Počet číslic ve jménu výstupního podadresáře, typicky 5
        /// </summary>
        private int _ResultNumLength;
        /// <summary>
        /// Suffix jména target souboru pro zkracování jména
        /// </summary>
        private int _ResultShortNameSuffix;
        /// <summary>
        /// Aktuální výstupní adresář, plná cesta. Vychází z <see cref="_ResultPath"/> + <see cref="_ResultDirPrefix"/>.
        /// </summary>
        private string _CurrentResultPath;
        /// <summary>
        /// Aktuální počet v result adresáři <see cref="_CurrentResultPath"/>.
        /// </summary>
        private int _CurrentResultCount;
        /// <summary>
        /// Dolní počet souborů v result adresáři <see cref="_CurrentResultPath"/>.
        /// Pokud v aktuálním výstupním adresáři bude méně než tento počet souborů, budeme do něj přidávat další soubory z dalšího adresáře, i kdyby se do něj nevešly všechny.
        /// Pokud v něm bude více souborů, a bude se přidávat tolik souborů, že by to v součtu přesáhlo počet <see cref="_ResultDirMaxCount"/>, pak se založí další výstupní adresář.
        /// Více v metodě <see cref="GetCurrentTargetDirFor(int, bool, out int)"/>.
        /// </summary>
        private int _ResultDirMinCount;
        /// <summary>
        /// Horní počet souborů v result adresáři <see cref="_CurrentResultPath"/>.
        /// Pokud by přidáním dalšího vstupního adresáře byl překročen tento počet, ale ne <see cref="_ResultDirMaxCount"/>, mohou se do něj vejít.
        /// Pokud by ale měl být překročen počet <see cref="_ResultDirMaxCount"/>, pak se soubory z jednoho vstupního adresáře budou ukládat do více výstupních adresářů a to tak, 
        /// aby ve výstupním bylo nejvíce <see cref="_ResultDirTopCount"/> souborů.
        /// Více v metodě <see cref="GetCurrentTargetDirFor(int, bool, out int)"/>.
        /// </summary>
        private int _ResultDirTopCount;
        /// <summary>
        /// Maximální počet souborů v result adresáři <see cref="_CurrentResultPath"/>, nepřekročitelná hodnota.
        /// </summary>
        private int _ResultDirMaxCount;
        /// <summary>
        /// Máme smazat vstupní adresáře, pokud po úklidu budou prázdné?
        /// </summary>
        private bool _HasDeleteEmptyDirs;
        /// <summary>
        /// Poslední čas volání progressu
        /// </summary>
        private DateTime? _ProgressLastTime;
        /// <summary>
        /// Argumenty a data pro progress
        /// </summary>
        private DirCleanProgressArgs _ProgressArgs;
        /// <summary>
        /// Výchozí jméno pro result adresář
        /// </summary>
        private static string _ResultPathDefault { get { return "___Results"; } }
        #endregion
    }
    #region class DirCleanProgressArgs : ata pro zobrazení progresu DirClean
    /// <summary>
    /// Data pro zobrazení progressu
    /// </summary>
    public class DirCleanProgressArgs : EventArgs
    {
        public DirCleanProgressArgs()
        {
            State = WorkingState.Initiated;
        }
        public decimal ProgressRatio { get; private set; }
        public string CurrentDirectory { get; private set; }
        public int DeleteFileCount { get; private set; }
        public long DeleteFileSize { get; private set; }
        public int MoveFileCount { get; private set; }
        public long MoveFileSize { get; private set; }
        public WorkingState State { get; private set; }

        internal void AddMovedFile(IO.FileInfo fileInfo)
        {
            MoveFileCount++;
            MoveFileSize += fileInfo.Length;
        }
        internal void AddDeletedFile(IO.FileInfo fileInfo)
        {
            DeleteFileCount++;
            DeleteFileSize += fileInfo.Length;
        }

        internal void SetInfo(decimal progressRatio, string currentDirectory)
        {
            this.ProgressRatio = (progressRatio < 0m ? 0m : (progressRatio > 1m ? 1m : progressRatio));
            if (currentDirectory != null)
                this.CurrentDirectory = currentDirectory;
            if (State != WorkingState.Working) State = WorkingState.Working;
        }
        internal void SetState(WorkingState state)
        {
            State = state;
        }
    }
    #endregion
    #region UI
    /*
    public class DirCleanPanel : WebActionPanel
    {
        #region Konstrukce
        public DirCleanPanel() : base() { }
        protected override void InitComponents()
        {
            base.InitComponents();

            this.DataInit();

            this.SuspendLayout();

            int tabIndex = 0;
            int x = DesignContentLeft;
            int y = DesignContentTop;
            int r = DesignContentRight;
            int labelHeight = DesignLabelHeight;
            int labelDistanceY = DesignLabelSpaceY;
            int textHeight = DesignTextHeight;
            int textDistanceY = DesignTextSpaceY;
            int textLabelOffset = DesignTextToLabelOffsetY;

            this._TargetDirLbl = new WebLabel("Cílový adresář:", new Rectangle(x + DesignLabelOffsetX, y, 320, labelHeight), ref tabIndex) { TextAlign = ContentAlignment.MiddleLeft };
            y += labelDistanceY;
            this._TargetDirTxt = new WebText(new Rectangle(x, y, r - x - DesignSmallButtonWidth - 0, textHeight), ref tabIndex) { Enabled = true, TextAlign = HorizontalAlignment.Left }; ;
            this._TargetDirTxt.TextChanged += _TargetDirTxt_TextChanged;
            this._TargetDirBtn = new WebButton("...", new Rectangle(r - DesignSmallButtonWidth, y, DesignSmallButtonWidth, textHeight), ref tabIndex);
            this._TargetDirBtn.Click += _TargetDirBtn_Click;
            y += textDistanceY;

            this.CreateActionButton("UKLIDIT", ref tabIndex);




            this.ShowButtonByState();

            ((System.ComponentModel.ISupportInitialize)(this._AutoEndTxt)).EndInit();

            this.ResumeLayout(false);
        }
        protected override void RecalcLayout()
        {
            base.RecalcLayout();
        }
        #endregion
        #region Data
        private void DataInit()
        {
            this.WebDownload = new WebDownload();
            this.WebDownload.StateChanged += WebDownload_StateChanged;
            this.WebDownload.DownloadProgress += WebDownload_DownloadProgress;
        }
        #endregion
    }
    */
    #endregion
}
