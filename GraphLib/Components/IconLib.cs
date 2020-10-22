using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Asol.Tools.WorkScheduler.Application;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Library of images
    /// </summary>
    public class IconLib
    {
        #region Singleton
        /// <summary>
        /// Singleton, "já"
        /// </summary>
        protected static IconLib I
        {
            get
            {
                if (_I == null)
                {
                    lock (_ILock)
                    {
                        if (_I == null)
                        {
                            _I = new IconLib();
                        }
                    }
                }
                return _I;
            }
        }
        private static IconLib _I;
        private static object _ILock = new object();
        #endregion
        #region Public acces to image
        /// <summary>
        /// Return an image loaded from file from directory "bin/../[../../../]pic/*(size)/name.{png|jpg|gif|bmp}.
        /// Can return image with bigger or smaller size.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Image Image(string name, IconSize size)
        {
            return I._GetImage(name, (int)size, IconState.Standard);
        }
        /// <summary>
        /// Return an image loaded from file from directory "bin/../[../../../]pic/*(size)/name.{png|jpg|gif|bmp}.
        /// Can return image with bigger or smaller size.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Image Image(string name, int size)
        {
            return I._GetImage(name, size, IconState.Standard);
        }
        /// <summary>
        /// Return an image loaded from file from directory "bin/../[../../../]pic/*(size)/name(status).{png|jpg|gif|bmp}.
        /// Can return image with bigger or smaller size.
        /// Can return image with supplemental status.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="size"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static Image Image(string name, IconSize size, IconState state)
        {
            return I._GetImage(name, (int)size, state);
        }
        /// <summary>
        /// Return an image loaded from file from directory "bin/../[../../../]pic/*(size)/name(status).{png|jpg|gif|bmp}.
        /// Can return image with bigger or smaller size.
        /// Can return image with supplemental status.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="size"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static Image Image(string name, int size, IconState state)
        {
            return I._GetImage(name, size, state);
        }
        #endregion
        #region Private icon library mechanism: constructor, file list loading, icon search
        private IconLib()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location); // app/bin [?Debug]
            for (int t = 0; t < 6; t++)
            {
                string test = System.IO.Path.Combine(path, "pic");
                if (System.IO.Directory.Exists(test))
                {
                    path = test;
                    break;
                }
                path = System.IO.Path.GetDirectoryName(path);
                if (path.Length < 5) break;
            }

            this._ImageDict = new Dictionary<string, IconImages>();
            this._ImagePath = path;
            this._FileLists = new List<FileList>();
            this._LoadFileList();
            this._FillConstants();
        }
        /// <summary>
        /// Fill list of directories (bin\pic\*) to this._FileLists
        /// </summary>
        private void _LoadFileList()
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(this._ImagePath);
            if (!di.Exists) return;
            System.IO.DirectoryInfo[] subDirs = di.GetDirectories();
            foreach (System.IO.DirectoryInfo subDir in subDirs)
            {
                FileList fileList = FileList.CreateForDir(subDir);
                if (fileList != null)
                    this._FileLists.Add(fileList);
            }
            this._FileLists.Sort((a, b) => a.Size.CompareTo(b.Size));
        }
        private void _FillConstants()
        {
            this._StateSuffix = new Dictionary<IconState, string[]>();
            this._StateSuffix.Add(IconState.Standard, ";_std;_a".Split(';'));
            this._StateSuffix.Add(IconState.Disable, "_dis;_d".Split(';'));
            this._StateSuffix.Add(IconState.Hot, "_hot;_h".Split(';'));
            this._StateSuffix.Add(IconState.Focused, "_foc;_f".Split(';'));
            this._StateSuffix.Add(IconState.Pressed, "_dwn;_p".Split(';'));

            this._StateSupplements = new Dictionary<IconState, IconState[]>();
            this._StateSupplements.Add(IconState.Standard, new IconState[] { IconState.Standard });
            this._StateSupplements.Add(IconState.Disable, new IconState[] { IconState.Disable, IconState.Standard });
            this._StateSupplements.Add(IconState.Hot, new IconState[] { IconState.Hot, IconState.Pressed, IconState.Focused, IconState.Standard });
            this._StateSupplements.Add(IconState.Focused, new IconState[] { IconState.Focused, IconState.Hot, IconState.Pressed, IconState.Standard });
            this._StateSupplements.Add(IconState.Pressed, new IconState[] { IconState.Pressed, IconState.Hot, IconState.Focused, IconState.Standard });

            this._FileExtensions = ".png;.gif;.jpg;.bmp".Split(';');
        }
        /// <summary>
        /// Returns Image for specified name, size and state
        /// </summary>
        /// <param name="name"></param>
        /// <param name="size"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private Image _GetImage(string name, int size, IconState state)
        {
            if (String.IsNullOrEmpty(name))
                throw new GraphLibDataException("IconLib[name]: název ikony nesmí být prázdný.");
            string key = name.ToLower().Trim();
            IconImages icon;
            if (!this._ImageDict.TryGetValue(key, out icon))
            {
                icon = new IconImages(key, name, this);
                this._ImageDict.Add(key, icon);
            }

            return icon.Image(size, state);
        }
        /// <summary>
        /// Return a list of files for specified size (or greater, or smaller).
        /// If "shift" is not zero, then return images smaller (for negative shift) or bigger (positive shift).
        /// Value of "shift" is not in pixel, but in count of "set".
        /// For example, when exists size: 8,16,24,32,48,64, and requested size = 32, and shift= -1, then return is with size = 24.
        /// For requested size = 28 and shift = 0 is return set size = 32 (nearest bigger), for shift= -1, then return is with size = 24.
        /// Returns null only when does not exists any path (this.FileLists.Count == 0).
        /// </summary>
        /// <param name="size"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        protected FileList GetFileListForSize(int size, int shift)
        {
            if (this.FileLists.Count == 0) return null;

            int idx = this.FileLists.FindIndex(f => f.Size >= size);
            if (idx < 0)
                idx = this.FileLists.FindLastIndex(f => f.Size < size);

            if (shift != 0)
            {
                idx += shift;
                if (idx < 0) idx = 0;
                int last = this.FileLists.Count - 1;
                if (idx > last) idx = last;
            }

            return this.FileLists[idx];
        }
        /// <summary>
        /// Return an list of all FileList sorted for specified "size":
        /// at index [0] is FileList with required Size (or greater), at next positions is FileList with Size greater than "size" (ASC), 
        /// and then with Size smaller than "size" (DESC).
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        protected List<FileList> GetFileListsForSize(int size)
        {
            List<FileList> result = new List<FileList>(this.FileLists);
            result.Sort((a, b) => FileListCompareForSize(a, b, size));
            return result;
        }
        /// <summary>
        /// Comparer for method GetFileListsForSize()
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private int FileListCompareForSize(FileList a, FileList b, int size)
        {
            int ac = a.Size.CompareTo(size);     // ac is positive when a.Size > size, is zero when a.Size == size, is negative when a.Size < size
            int bc = b.Size.CompareTo(size);     // bc is positive when b.Size > size, is zero when b.Size == size, is negative when b.Size < size
            
            if (ac == 0 && bc == 0) return 0;    // a.Size == size, b.Size == size => a and be is equal
            if (ac == 0) return -1;              // a.Size == size, b.Size != size => a is better (-1 : a will be at index near [0] after Sort)
            if (bc == 0) return 1;               // a.Size != size, b.Size == size => b is better (+1 : b will be at index near [0] after Sort)

            if (ac > 0 && bc < 0) return -1;     // a.Size > size, b.Size < size => a is better
            if (ac < 0 && bc > 0) return 1;      // a.Size < size, b.Size > size => b is better

            int ab = a.Size.CompareTo(b.Size);   // ab is negative when a.Size < b.Size
            if (ac > 0 && ab < 0) return -1;     // a.Size > size (and b.Size > size) and a.Size < b.Size => a is better
            if (ac > 0 && ab > 0) return 1;      // a.Size > size (and b.Size > size) and a.Size > b.Size => b is better
            if (ac < 0 && ab < 0) return 1;      // a.Size < size (and b.Size < size) and a.Size < b.Size => b is better
            if (ac < 0 && ab > 0) return -1;     // a.Size < size (and b.Size < size) and a.Size > b.Size => a is better

            return 0;
        }
        /// <summary>
        /// Return array of state for specified state, where at first position [0] is specified state, 
        /// and on next positions is supplemental states for icons, when does not exists icon for exact state.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        protected IconState[] GetStateWithSupplement(IconState state)
        {
            return this._StateSupplements[state];
        }
        /// <summary>
        /// Search for file with icon and load it
        /// </summary>
        /// <param name="fileKey">Name of file, without a size and state suffix</param>
        /// <param name="fileList">List of files with requested size</param>
        /// <param name="state">State of icon</param>
        /// <returns></returns>
        protected System.Drawing.Image LoadImage(string fileKey, FileList fileList, IconState state)
        {
            string fileName = this._SearchForFileName(fileKey, this._StateSuffix[state], this._FileExtensions, fileList);
            if (fileName == null) return null;

            try
            {
                return System.Drawing.Bitmap.FromFile(fileName);
            }
            catch { }
            return null;
        }

        private string _SearchForFileName(string fileKey, string[] suffixes, string[] extensions, FileList fileList)
        {
            foreach (string ext in extensions)
                foreach (string suf in suffixes)
                {
                    string key = fileKey + suf + ext;
                    string fileName;
                    if (fileList.Files.TryGetValue(key, out fileName)) return fileName;
                }
            return null;
        }

       
        /// <summary>
        /// Full directory name for path with images: DLL files / pic
        /// </summary>
        private string _ImagePath;
        /// <summary>
        /// Dictionary of images: 
        /// Key = name of image without size and state,
        /// Value = IconImages (contain all size and all state, can on demand load new image, return requested image).
        /// Contain only object on demand loaded for requested names, not for all existing files.
        /// </summary>
        private Dictionary<string, IconImages> _ImageDict;
        /// <summary>
        /// List of all directories and its file names
        /// </summary>
        protected List<FileList> FileLists { get { return this._FileLists; } }
        /// <summary>
        /// List of all directories and its file names
        /// </summary>
        private List<FileList> _FileLists;
        /// <summary>
        /// List of file suffixes for each icon state
        /// </summary>
        private Dictionary<IconState, string[]> _StateSuffix;
        /// <summary>
        /// List of state supplements for specified state (when does not exists icon for specified state, we can return icon for other state)
        /// </summary>
        private Dictionary<IconState, IconState[]> _StateSupplements;
        /// <summary>
        /// List of file extensions, by their priority
        /// </summary>
        private string[] _FileExtensions;
        #endregion
        #region class FileList, IconImages
        /// <summary>
        /// One pic directory with its size and content (file list)
        /// </summary>
        protected class FileList
        {
            private FileList(System.IO.DirectoryInfo dirInfo, int size)
            {
                this.Path = dirInfo.FullName;
                this.Size = size;
                this._Files = null;
            }
            /// <summary>
            /// Visualisation
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "Size=" + this.Size.ToString() + "; Path=" + this.Path;
            }
            /// <summary>
            /// Full directory name for images of this.Size
            /// </summary>
            public string Path { get; private set; }
            /// <summary>
            /// Size of all images in this directory in typicall line: 16,24,32,48,64
            /// </summary>
            public int Size { get; private set; }
            /// <summary>
            /// List of names of all existing files in this directory
            /// </summary>
            public Dictionary<string, string> Files
            {
                get
                {
                    if (this._Files == null)
                    {
                        List<string> files = new List<string>(System.IO.Directory.GetFiles(this.Path));
                        files.Sort();
                        Dictionary<string, string> dict = new Dictionary<string, string>();
                        foreach (string file in files)
                        {
                            string key = System.IO.Path.GetFileName(file).ToLower();
                            dict.Add(key, file);
                        }
                        this._Files = dict;
                    }
                    return this._Files;
                }
            }
            private Dictionary<string, string> _Files;
            /// <summary>
            /// Return new FileList instance for specified directory.
            /// Return null, when directory does not exists or its name not ending with numeric (with length 1÷3 digits).
            /// </summary>
            /// <param name="subDir"></param>
            /// <returns></returns>
            internal static FileList CreateForDir(System.IO.DirectoryInfo subDir)
            {
                if (subDir == null || !subDir.Exists) return null;
                string name = subDir.Name;            // for example: "pic16"
                string text = "";
                for (int i = name.Length - 1; i >= 0; i--)
                {
                    char c = name[i];
                    if ("0123456789".IndexOf(c) < 0) break;
                    text = c.ToString() + text;
                }
                if (text.Length == 0 || text.Length > 3) return null;

                int size;
                if (!Int32.TryParse(text, out size)) return null;
                return new FileList(subDir, size);
            }
        }
        /// <summary>
        /// One icon with all its images (all sizes, all states)
        /// </summary>
        protected class IconImages
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="key"></param>
            /// <param name="name"></param>
            /// <param name="owner"></param>
            public IconImages(string key, string name, IconLib owner)
            {
                this.Key = key;
                this.Name = name;
                this.Owner = owner;
                this.ImageList = new List<IconImageSize>();
            }
            /// <summary>
            /// Visualisation
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "Image key=" + this.Key + "; loaded " + this.ImageList.Count.ToString() + " size(s).";
            }
            /// <summary>
            /// Key for image = lower, trimmed name of icon
            /// </summary>
            public string Key { get; private set; }
            /// <summary>
            /// Name of icon
            /// </summary>
            public string Name { get; private set; }
            /// <summary>
            /// Reference to owner object
            /// </summary>
            protected IconLib Owner { get; private set; }
            /// <summary>
            /// Reference to list of directories with icon files
            /// </summary>
            public List<FileList> FileLists { get { return this.Owner._FileLists; } }
            /// <summary>
            /// List of this icon in all sizes
            /// </summary>
            public List<IconImageSize> ImageList { get; private set; }
            /// <summary>
            /// Get image
            /// </summary>
            /// <param name="size"></param>
            /// <param name="state"></param>
            /// <returns></returns>
            internal Image Image(int size, IconState state)
            {
                List<FileList> fileLists = this.Owner.GetFileListsForSize(size);
                IconState[] states = this.Owner.GetStateWithSupplement(state);

                foreach (IconState s in states)
                    foreach (FileList fl in fileLists)
                    {
                        Image i = this._GetExactImage(fl, s);
                        if (i != null) return i;
                    }

                return null;
            }
            /// <summary>
            /// Return specified image, can try load image from file on first request.
            /// When image does not exists, return null (dont return any other = supplemental image).
            /// </summary>
            /// <param name="fileList"></param>
            /// <param name="state"></param>
            /// <returns></returns>
            private Image _GetExactImage(FileList fileList, IconState state)
            {
                IconImageSize imageSize = this.GetImageForSize(fileList.Size);
                if (imageSize == null) return null;

                // Quick resolve:
                if (imageSize.HasImage(state)) return imageSize[state];

                // OnDemand load:
                if (!imageSize.IsLoaded(state))
                    imageSize[state] = this.Owner.LoadImage(this.Key, fileList, state);

                // Is now loaded?
                if (imageSize.HasImage(state)) return imageSize[state];

                return null;
            }
            /// <summary>
            /// Returns set of images for specified size (or nearest upper, or nearest lower).
            /// If "shift" is not zero, then return images smaller (for negative shift) or bigger (positive shift).
            /// Value of "shift" is not in pixel, but in count of "set".
            /// For example, when exists size: 8,16,24,32,48,64, and requested size = 32, and shift= -1, then return is with size = 24.
            /// For requested size = 28 and shift = 0 is return set size = 32 (nearest bigger), for shift= -1, then return is with size = 24.
            /// Returns null only when does not exists any path (this.FileLists.Count == 0).
            /// </summary>
            /// <param name="size"></param>
            /// <returns></returns>
            private IconImageSize GetImageForSize(int size)
            {
                IconImageSize imageSize = this.ImageList.FirstOrDefault(i => i.Size == size);
                if (imageSize == null)
                {
                    imageSize = new IconImageSize(size);
                    this.ImageList.Add(imageSize);
                    if (this.ImageList.Count > 1)
                        this.ImageList.Sort((a, b) => (a.Size.CompareTo(b.Size)));
                }
                return imageSize;
            }
        }
        /// <summary>
        /// One icon in one size, with all states
        /// </summary>
        protected class IconImageSize
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="size"></param>
            public IconImageSize(int size)
            {
                this.Size = size;
                this.ImageDict = new Dictionary<IconState, Image>();
            }
            /// <summary>
            /// Visualisation
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "Images for size=" + this.Size.ToString() + "; loaded images count=" + this.ImageDict.Count.ToString();
            }
            /// <summary>
            /// Return true when contains item for specified state.
            /// When item for state is a null value, return true (contains null).
            /// </summary>
            /// <param name="state"></param>
            /// <returns></returns>
            public bool IsLoaded(IconState state)
            {
                return this.ImageDict.ContainsKey(state);
            }
            /// <summary>
            /// Return true when contains non-null image for specified state.
            /// When item for state is a null value, return false (does not contains an image).
            /// </summary>
            /// <param name="state"></param>
            /// <returns></returns>
            public bool HasImage(IconState state)
            {
                return (this.ImageDict.ContainsKey(state) && this.ImageDict[state] != null);
            }
            /// <summary>
            /// Get or set an Image for specified state.
            /// When image for state does not exists (or exists and contain null), return null.
            /// </summary>
            /// <param name="state"></param>
            /// <returns></returns>
            public Image this[IconState state]
            {
                get 
                {
                    Image image;
                    if (this.ImageDict.TryGetValue(state, out image))
                        return image;
                    return null;
                }
                set
                {
                    if (!this.ImageDict.ContainsKey(state))
                        this.ImageDict.Add(state, value);
                    else
                        this.ImageDict[state] = value;
                }
            }
            /// <summary>
            /// Velikost
            /// </summary>
            public int Size { get; private set; }
            /// <summary>
            /// Ikony pro různé stavy
            /// </summary>
            public Dictionary<IconState, Image> ImageDict { get; private set; }
        }
        #endregion
    }
    #region enum IconSize and IconState
    /// <summary>
    /// Enum IconSize (8; 16; 24; 32; 48; 64)
    /// </summary>
    public enum IconSize : int
    {
        /// <summary>
        /// Mikro - ikona = 8
        /// </summary>
        Micro8 = 8,
        /// <summary>
        /// Mini - ikona = 16
        /// </summary>
        Mini16 = 16,
        /// <summary>
        /// Malá - ikona = 24
        /// </summary>
        Small24 = 24,
        /// <summary>
        /// Standard - ikona = 32
        /// </summary>
        Standard32 = 32,
        /// <summary>
        /// Velká - ikona = 48
        /// </summary>
        Enlarged48 = 48,
        /// <summary>
        /// Extra velká - ikona = 64
        /// </summary>
        Big64 = 64
    }
    /// <summary>
    /// Stav ikony
    /// </summary>
    public enum IconState : int
    {
        /// <summary>
        /// Běžně dostupná
        /// </summary>
        Standard = 'a',
        /// <summary>
        /// Disabled
        /// </summary>
        Disable = 'd',
        /// <summary>
        /// Hot = pod myší
        /// </summary>
        Hot = 'h',
        /// <summary>
        /// Focus = s focusem
        /// </summary>
        Focused = 'f',
        /// <summary>
        /// Stisknutá
        /// </summary>
        Pressed = 'p'
    }
    #endregion
    /// <summary>
    /// Knihovna ikon
    /// </summary>

    public class IconLibrary
    {
        #region Image BackSand
        /// <summary>
        /// Vrátí obrázek, vytvořený na základě obrázku BackSand.jpg.
        /// Vrací přímou referenci na objekt v paměti = volání je velice rychlé (pod 1 mikrosekundu), 
        /// ale protože se vrací živá reference, pak zásahy do vráceného objektu se projeví v objektu v paměti = natrvalo.
        /// Případně je možno vyvolat metodu Backsand_Reset(), která zajistí regenerování obrázku z jeho definice.
        /// Bezpečnější je používání property Backsand, která vrací vždy nový objekt.
        /// Pro používání pro Button.Image, kde bude nastavováno Button.Enabled = false; je třeba použít property Backsand_FromFile.
        /// </summary>
        /// <remarks>
        /// Plné jméno vstupního souboru:
        /// C:\_Working\BackSand.jpg
        /// </remarks>
        public static System.Drawing.Image BackSand_Cached
        {
            get
            {
                if (_BackSand == null) _BackSand = BackSand;
                return _BackSand;
            }
        }
        /// <summary>
        /// Vrátí obrázek, vytvořený na základě obrázku BackSand.jpg.
        /// Nelze používat jako Image pro Buttony, kde bude nastavováno Enabled = false (pak dojde k chybě .NET: Ot of memory).
        /// Pak je třeba použít Image Backsand_FromFile.
        /// Vrací vždy novou instanci = volání je při častém používání výrazně (až 1000krát) pomalejší než použití Backsand_Cached
        /// (kde ale hrozí nebezpečí pokažení obrázku v paměti).
        /// Čas pro vytvoření jednoho Image (tj. každé jednotlivé čtení této property) pro obrázek velikosti 32x32 px (32bpp) je průměrně 350 mikrosekund.
        /// Pokud je třeba používat obrázek často a rychle, je možné využít property Backsand_Cached.
        /// </summary>
        /// <remarks>
        /// Plné jméno vstupního souboru:
        /// C:\_Working\BackSand.jpg
        /// </remarks>
        public static System.Drawing.Image BackSand { get { return _ImageCreateFrom(_BackSand_string, null); } }
        /// <summary>
        /// Vrátí obrázek, načtený ze souboru BackSand.jpg.
        /// Vrací vždy novou instanci = volání je při častém používání výrazně (až 1000krát) pomalejší než použití Backsand_Cached
        /// (kde ale hrozí nebezpečí pokažení obrázku v paměti).
        /// Čas pro vytvoření jednoho Image (tj. každé jednotlivé čtení této property) pro obrázek velikosti 32x32 px (32bpp) je průměrně 350 mikrosekund.
        /// Pokud je třeba používat obrázek často a rychle, je možné využít property Backsand_Cached.
        /// </summary>
        /// <remarks>
        /// Plné jméno vstupního souboru:
        /// C:\_Working\BackSand.jpg
        /// </remarks>
        public static System.Drawing.Image BackSand_FromFile { get { return _ImageCreateFrom(_BackSand_string, "BackSand.jpg"); } }
        /// <summary>
        /// Vygeneruje a vrátí string, který definuje obsah souboru Backsand
        /// </summary>
        /// <returns></returns>
        private static string _BackSand_string()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(4216);
            sb.Append("/9j/4AAQSkZJRgABAQEAYABgAAD/4QBoRXhpZgAATU0AKgAAAAgABAEaAAUAAAABAAAAPgEbAAUAAAABAAAARgEoAAMAAAABAAIAAAExAAIAAAASAAAATgAAAAAAAABg");
            sb.Append("AAAAAQAAAGAAAAABUGFpbnQuTkVUIHYzLjUuMTEA/9sAQwACAQECAQECAgICAgICAgMFAwMDAwMGBAQDBQcGBwcHBgcHCAkLCQgICggHBwoNCgoLDAwMDAcJDg8NDA4L");
            sb.Append("DAwM/9sAQwECAgIDAwMGAwMGDAgHCAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwM/8AAEQgA+gARAwEiAAIRAQMRAf/EAB8A");
            sb.Append("AAEFAQEBAQEBAAAAAAAAAAABAgMEBQYHCAkKC//EALUQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYn");
            sb.Append("KCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2drh");
            sb.Append("4uPk5ebn6Onq8fLz9PX29/j5+v/EAB8BAAMBAQEBAQEBAQEAAAAAAAABAgMEBQYHCAkKC//EALURAAIBAgQEAwQHBQQEAAECdwABAgMRBAUhMQYSQVEHYXETIjKBCBRC");
            sb.Append("kaGxwQkjM1LwFWJy0QoWJDThJfEXGBkaJicoKSo1Njc4OTpDREVGR0hJSlNUVVZXWFlaY2RlZmdoaWpzdHV2d3h5eoKDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKz");
            sb.Append("tLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uLj5OXm5+jp6vLz9PX29/j5+v/aAAwDAQACEQMRAD8A+0zpd5d3N5b6hN/Z0ljbsrEkfvHQA7Dg85HTrzW1BqGoXeiafptm");
            sb.Append("y2bXCqWIjKiVz91s46jI5HvWZd6/DI7TWi280X2cyvtiy1uSdo3Z6np7DNdV4dvrvxtBoF5DdNb3mlgRxr5XmB5M8EE8Dj+E8cV87KTtc9pRSdjzz+yNQ/56H/vlqK9R");
            sb.Append("/tLxn/z10min7Yr2cTyfT9OlsdU2zW94sbKpuIymH2Eg5+nQ13lz4NvNEeezs47i4tJljuYn3FIiCeHz7DI/GrunrqXjHV/+EkWWHTbZUGZ5k3MQgxgKONpx+OaluvEW");
            sb.Append("oeM/BFwokvGgilclIkVIgDhl4H8IweBxzXDUqOdRKeh0Rp2hexY+0Sf37f8A74orgP7Lv/8AnoP+/Zort5aZjaf8p6fqd5qrR6ZawrZweH7q2GIE/wBYka4BQ+43V0mv");
            sb.Append("+Go9D8BroEcxtWvkKG8jXaRhdy8DqDwp9qoeH4NLX4aJLJdia5UP9nt43HmyqOA5HXk/yrlPjB8Ubqfw9p+kyXUlvPbq0dzHEDu28bTnueteNedWrZHpc0aMLmN/wjmq");
            sb.Append("f9DFp/8A35X/AAorhPsFn/z9TflRXrfUl2Z5v1w6HwO+pXOoS31jN/pNvbO+HBOEGMqB6c/Til8Z+H5tS8IRa+ZP3kzchpd0jjJBIHXgjk/Sm/DDxVq3hnWI7fTYWE+o");
            sb.Append("R/Z0lX5mjDcbh7jJqxB4KvX19tPjeSSNVcGaQFY5e5HtzV+z9lU5kVzSnE4Py/8Aaaiu2/4VbJ/et/8Av4aK9D6wc3s2dJ4httG8F6THFp90LrVY4Y5dsIBZXcgMG7HA");
            sb.Append("6j3qv8QLDUY7ezupm+0XETrHGscgeG2UjIVmHcnt9az9c8Lf8If4yh1L7N/Zum3YMkMNvLvJUjhSzc8+tdVpPhFbj4d/Z7p/sCyTSXgiLAyKqg8DttPFeLK0JJtnbKPN");
            sb.Append("GyOB/wCE/vv+fex/74FFVf8AhGIf+eOofktFeh7RHL7FnaaPd3ni/wAU2EniCOVGt7ZIrWN02ghR8hweq88n2rZ8QTN4u8M3iyXi200MkrDy4tzsucOo9FAGfpWFIt8H");
            sb.Append("0e+e3xqktwot158iOMEknaP4T/Kr8XjDT/DniO1vJ1kOl3aMtyLRPmdypDL83QbsZ9q8nncpcyOymrJo89/sTTf+g9L/AN+Gor0D+1NN/wCeOlf9+l/woru+sRM/Zsm8");
            sb.Append("O+IX1XTdRjj1K1+0XCIkfltskcKjAKF/hHY4rn7X+1rqwk0O3Xy5owdigjaTjcefU5xVD4daUmn/ABF0+3eP7RJNGHWNTgq7A5U54z161ueLdK1Tw59r1O41HTbW6hLP");
            sb.Append("bx284Y3sZYrvUDoF6fga5XR/ecsSoybips5PMX/Pvdf9+RRUf/CyNa/5/IP+/Sf4UVX1Vl+0Re1W/k8H67Ikipfhm884mG87gSN2O+D24596h1jx9N41tvsa6fBB5cZS");
            sb.Append("BYI/mVepGeuCRUfjyGTQPFE2lyQx20lm/lrIqFTIp6Oc9ipB+lWPCXhbUbVZriwtLi4vLdftEUqsFXy16naev0rujCmqSl1OJynzcpkf8Ivdf9A+7/KitP8A4WTqPqf+");
            sb.Append("+BRR7SA/ZzG6Tdp468ai71u6mndfnuJHbl0XjaPcgYArqPBPxDuF0+5ZdNZbfTLdvNSJBuKEgB2J5OMqvHQVL4d8PL4YimtdtnHeZM4vGlRU8sMBsO7qTitbxPo114t8");
            sb.Append("TtfWd9a6eyxPFDDECxIzv9lxk4OOgFctecZRXY6KcZRld7nL/wDC0NL/AOhV0z/vlqKn/wCEn8Rf89rX/vpKKn2dHzK5qnc4/wAa6rcL4yulvGkk+xzNFHG4+VApOFx6");
            sb.Append("cV6x4P1eG50aTWL+6hRbi2YvDDHtjiz8oUAfd3cjPrXmvxgvJdV8e3l1dW8cT3QWUxhg3l7kB5I/rWj8M7qO88L6xpjMvm30aRKWfaeG3LzjheMH6j0rbGU+ahGxlQqJ");
            sb.Append("VWyx/wAIwv8A0Cp6Krf8IF4k/wCebf8AgUaK4dTb2zK/ijw9eXktq000VwL6JroiNWf7JHuxl2wTxjvV7xDokfhuG3hbUEmvpxE6eSmdoIICkEfN26etdL8INRfwVq0c");
            sb.Append("Nrf/AGqPVITY3gEWYgzbjsDe+Rz9azPjd4RHgzxlp2peUsMN1Cpit43DrE8RXPH9wggg9zu9KqWIlKpyRKpUFCnzPc2P7E8Rf89Lz/vy3+FFN/4agvv+gbZ/9+DRS56n");
            sb.Append("YrkibHg3Q7Xwf4QsYNQZrO4sXNxNkrukcOCoB7Z2j8Kw/wBonV4vHPxe0q1ju5FtJ9PgYrLKrJFIwLYDDjGNuffNZdr4taTWNatdSvJLpvspaGaJQQDgMWXspC5HpgYr");
            sb.Append("l9W8MT2mpBb6GdrONY5JLuBNy+U33XU9MdRnvj2rejRjGXNLcyliOZKKPRv+EV0//oLWP/faUVzP9l+Av+fjXv8AwKT/AAorb3Q5h2kxaXoXhPVrWabFzDd7IpjDkugV");
            sb.Append("gFHBJVj26DrTtX8STaZYWNuGkkW406OJmuOI2CFlCrx0AOOe/Ncq4Gp6ct80l4RHMI1XdvwijJH4DvXVeK/HatoUckmk2hincZMibmVwDhGGfulcMMY+8axlGpfVamK5");
            sb.Append("d+hjf8IXpX/P5bf9/aKn/wCEk8L/APPhp/8A34k/+Koo9lV/lY/aUzB0TxJcaJaXFnbyhYbyMxzsEBZ1/u1HKLrxRrDrJHLdzsgCKFO4BVwBgegAH4VVlu4TeFo42EQH");
            sb.Append("yAN91scke2a7jwPrGmWOhLHFdCTVriQyzTyN5UcceAPLL53ZJ549Md69DE13TXNCOpnRoqb97Y5P/hWmtf8AQF1L/vw1Feof8NCaf/e1j/yH/hRXm/2jiOxt9VpHl/iT");
            sb.Append("QbWx1N47W7S48nG4ovyHjOUPcDpmp/Ad5Dpl5dXFxbW90sULyxrOuYy46ZHf0x71Uv7sW+gWdrDcJJJvYSr5Chohngbxktzk9sVtaBqEV/4bm0f7K0l5NKXWZApYcDAA");
            sb.Append("POODnFepJr2a5tTlje9kY/8AbkP/AEDtN/75P+NFb3/CHzf8+8v/AIDf/Xorn5KfYrll/Mc3LFceFtahDGFplCuNpDgbhnnPGcHoa7Dw1o6+KNevNQt41s7e2i3qHfMh");
            sb.Append("2AFnYDqTz7ZIrm/FCK/je3VlBVkt8gjr+7Suj8cyNpfh9VtWa3WeHbKIjsEgEjYBx16Dr6VVfWKiVR3ND/hP4v8AnjP+VFdf9gg/54w/98CiuPlN+VH/2Q==");
            return sb.ToString();
        }
        /// <summary>
        /// Resetuje obrázek v paměti.
        /// Následující použití property Backsand_Direct si vytvoří nový, korektní - podle jeho originální deklarace.
        /// Tuto metodu je vhodné použít v situaci, kdy je správnost obrázku v paměti nejistá.
        /// </summary>
        public static void BackSand_Reset()
        {
            _BackSand = null;
        }
        /// <summary>
        /// Úložiště objektu v paměti, při prvním použití Backsand_Direct je objekt naplněn a následně využíván.
        /// </summary>
        private static System.Drawing.Image _BackSand = null;
        #endregion
        #region SPOLEČNÉ METODY PRO KONVERZI
        /// <summary>
        /// Vrátí Image z definice (string). Image generuje buď v paměti, anebo s pomocí souboru.
        /// Pokud není zadán parametr (cacheFileName) (když je null), generuje se Image v paměti.
        /// Pokud je soubor (cacheFileName) definován, pak se Image čte z něj.
        /// Pokud soubor neexistuje, nejprve se vytvoří.
        /// Umístění souboru (adresář) je dán property _ImageCacheDir, defaultně je to adresář (Sys:\Documents and Settings\All Users\Data aplikací\IconCache).
        /// Obsah obrázku definuje metoda getDataMethod.
        /// </summary>
        /// <param name="getDataMethod"></param>
        /// <param name="cacheFileName"></param>
        /// <returns></returns>
        private static System.Drawing.Image _ImageCreateFrom(_GetStringDelegate getDataMethod, string cacheFileName)
        {
            System.Drawing.Image image;
            if (cacheFileName == null)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Convert.FromBase64String(getDataMethod())))
                {
                    image = System.Drawing.Image.FromStream(ms);
                }
                return image;
            }
            string fileName = System.IO.Path.Combine(_ImageCacheDir, cacheFileName);
            if (!System.IO.File.Exists(fileName))
                System.IO.File.WriteAllBytes(fileName, Convert.FromBase64String(getDataMethod()));
            if (System.IO.File.Exists(fileName))
                return Image.FromFile(fileName);
            return null;
        }
        /// <summary>
        /// Obsahuje jméno adresáře, který slouží jako Cache pro obrázky systému.
        /// </summary>
        private static string _ImageCacheDir
        {
            get
            {
                string dirName = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), "IconCache");
                if (!System.IO.Directory.Exists(dirName))
                    System.IO.Directory.CreateDirectory(dirName);
                return dirName;
            }
        }
        /// <summary>
        /// Předpis pro metody, které generují obsah souboru s daty
        /// </summary>
        /// <returns></returns>
        private delegate string _GetStringDelegate();
        /// <summary>
        /// Vrátí Icon z dat (string), která jsou předána na vstup v kódování Base64
        /// </summary>
        /// <param name="data">Vstupní data v kódování Base64</param>
        /// <returns>Icon vytvořená z dat</returns>
        public static System.Drawing.Icon ConvertStringToIcon(string data)
        {
            System.Drawing.Icon icon;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Convert.FromBase64String(data)))
            {
                icon = new System.Drawing.Icon(ms);
            }
            return icon;
        }
        /// <summary>
        /// Vrátí Cursor z dat (string), která jsou předána na vstup v kódování Base64
        /// </summary>
        /// <param name="data">Vstupní data v kódování Base64</param>
        /// <returns>Cursor vytvořený z dat</returns>
        public static System.Windows.Forms.Cursor ConvertStringToCursor(string data)
        {
            System.Windows.Forms.Cursor cursor;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Convert.FromBase64String(data)))
            {
                cursor = new System.Windows.Forms.Cursor(ms);
            }
            return cursor;
        }
        #endregion
    }
    /// <summary>
    /// Standardized icons for use in GraphLibrary
    /// </summary>
    public class IconStandard
    {
        /// <summary>
        /// Standardní ikona : malá kulička barvy černé
        /// </summary>
        public static Image BulletBlack16 { get { return Asol.Tools.WorkScheduler.Properties.Resources.bullet_black_16; } }
        /// <summary>
        /// Standardní ikona : malá kulička barvy modré
        /// </summary>
        public static Image BulletBlue16 { get { return Asol.Tools.WorkScheduler.Properties.Resources.bullet_blue_16; } }
        /// <summary>
        /// Standardní ikona : malá kulička barvy zelené
        /// </summary>
        public static Image BulletGreen16 { get { return Asol.Tools.WorkScheduler.Properties.Resources.bullet_green_16; } }
        /// <summary>
        /// Standardní ikona : malá kulička barvy oranžové
        /// </summary>
        public static Image BulletOrange16 { get { return Asol.Tools.WorkScheduler.Properties.Resources.bullet_orange_16; } }
        /// <summary>
        /// Standardní ikona : malá kulička barvy jůzové
        /// </summary>
        public static Image BulletPink16 { get { return Asol.Tools.WorkScheduler.Properties.Resources.bullet_pink_16; } }
        /// <summary>
        /// Standardní ikona : malá kulička barvy fialové
        /// </summary>
        public static Image BulletPurple16 { get { return Asol.Tools.WorkScheduler.Properties.Resources.bullet_purple_16; } }
        /// <summary>
        /// Standardní ikona : malá kulička barvy červené
        /// </summary>
        public static Image BulletRed16 { get { return Asol.Tools.WorkScheduler.Properties.Resources.bullet_red_16; } }
        /// <summary>
        /// Standardní ikona : malá kulička barvy bílé
        /// </summary>
        public static Image BulletWhite16 { get { return Asol.Tools.WorkScheduler.Properties.Resources.bullet_white_16; } }
        /// <summary>
        /// Standardní ikona : malá kulička barvy žluté
        /// </summary>
        public static Image BulletYellow16 { get { return Asol.Tools.WorkScheduler.Properties.Resources.bullet_yellow_16; } }
        /// <summary>
        /// Standardní ikona : malá hvězdička
        /// </summary>
        public static Image BulletStar16 { get { return Asol.Tools.WorkScheduler.Properties.Resources.bullet_star_16; } }

        /// <summary>
        /// Standardní ikona : Export - 64
        /// </summary>
        public static Image DocumentExport { get { return Asol.Tools.WorkScheduler.Properties.Resources.document_export_64; } }
        /// <summary>
        /// Standardní ikona : Save - 64
        /// </summary>
        public static Image DocumentSave { get { return Asol.Tools.WorkScheduler.Properties.Resources.document_save_5_64; } }
        /// <summary>
        /// Standardní ikona : Save As - 64
        /// </summary>
        public static Image DocumentSaveAs { get { return Asol.Tools.WorkScheduler.Properties.Resources.document_save_as_5_64; } }

        /// <summary>
        /// Standardní ikona : EditCopy
        /// </summary>
        public static Image EditCopy { get { return Asol.Tools.WorkScheduler.Properties.Resources.edit_copy_3_64; } }
        /// <summary>
        /// Standardní ikona : EditCut
        /// </summary>
        public static Image EditCut { get { return Asol.Tools.WorkScheduler.Properties.Resources.edit_cut_3_64; } }
        /// <summary>
        /// Standardní ikona : EditPaste
        /// </summary>
        public static Image EditPaste { get { return Asol.Tools.WorkScheduler.Properties.Resources.edit_paste_3_64; } }
        /// <summary>
        /// Standardní ikona : EditUndo
        /// </summary>
        public static Image EditUndo { get { return Asol.Tools.WorkScheduler.Properties.Resources.edit_undo_3_64; } }
        /// <summary>
        /// Standardní ikona : EditRedo
        /// </summary>
        public static Image EditRedo { get { return Asol.Tools.WorkScheduler.Properties.Resources.edit_redo_3_64; } }

        /// <summary>
        /// Standardní ikona : GoTop
        /// </summary>
        public static Image GoTop { get { return Asol.Tools.WorkScheduler.Properties.Resources.go_top_3_64; } }
        /// <summary>
        /// Standardní ikona : GoUp
        /// </summary>
        public static Image GoUp { get { return Asol.Tools.WorkScheduler.Properties.Resources.go_up_4_64; } }
        /// <summary>
        /// Standardní ikona : GoDown
        /// </summary>
        public static Image GoDown { get { return Asol.Tools.WorkScheduler.Properties.Resources.go_down_4_64; } }
        /// <summary>
        /// Standardní ikona : GoBottom
        /// </summary>
        public static Image GoBottom { get { return Asol.Tools.WorkScheduler.Properties.Resources.go_bottom_3_64; } }

        /// <summary>
        /// Standardní ikona : GoHome
        /// </summary>
        public static Image GoHome { get { return Asol.Tools.WorkScheduler.Properties.Resources.go_first_2_64; } }
        /// <summary>
        /// Standardní ikona : GoLeft
        /// </summary>
        public static Image GoLeft { get { return Asol.Tools.WorkScheduler.Properties.Resources.go_previous_4_64; } }
        /// <summary>
        /// Standardní ikona : GoRight
        /// </summary>
        public static Image GoRight { get { return Asol.Tools.WorkScheduler.Properties.Resources.go_next_4_64; } }
        /// <summary>
        /// Standardní ikona : GoEnd
        /// </summary>
        public static Image GoEnd { get { return Asol.Tools.WorkScheduler.Properties.Resources.go_last_2_64; } }

        /// <summary>
        /// Standardní ikona : RelationRecord
        /// </summary>
        public static Image RelationRecord { get { return Asol.Tools.WorkScheduler.Properties.Resources.arrow_right_blue_24; } }
        /// <summary>
        /// Standardní ikona : RelationDocument
        /// </summary>
        public static Image RelationDocument { get { return Asol.Tools.WorkScheduler.Properties.Resources.arrow_right_darkyellow_24; } }
        /// <summary>
        /// Standardní ikona : OpenFolder
        /// </summary>
        public static Image OpenFolder { get { return Asol.Tools.WorkScheduler.Properties.Resources.document_open_2_24; } }
        /// <summary>
        /// Standardní ikona : Kalkulačka
        /// </summary>
        public static Image Calculator { get { return Asol.Tools.WorkScheduler.Properties.Resources.accessories_calculator_3_24; } }
        /// <summary>
        /// Standardní ikona : Kalendář
        /// </summary>
        public static Image Calendar { get { return Asol.Tools.WorkScheduler.Properties.Resources.view_pim_calendar_24; } }
        /// <summary>
        /// Standardní ikona : DropDown
        /// </summary>
        public static Image DropDown { get { return Asol.Tools.WorkScheduler.Properties.Resources.arrow_dropdown_gray_2_24; } }

        /// <summary>
        /// Standardní ikona : Refresh
        /// </summary>
        public static Image Refresh { get { return Asol.Tools.WorkScheduler.Properties.Resources.view_refresh_3_64; } }

        /// <summary>
        /// Standardní ikona : FlipHorizontal
        /// </summary>
        public static Image ObjectFlipHorizontal32 { get { return Asol.Tools.WorkScheduler.Properties.Resources.object_flip_horizontal_32; } }
        /// <summary>
        /// Standardní ikona : FlipVertical
        /// </summary>
        public static Image ObjectFlipVertical32 { get { return Asol.Tools.WorkScheduler.Properties.Resources.object_flip_vertical_32; } }
        /// <summary>
        /// Standardní ikona : Kalendář
        /// </summary>
        public static Image ViewPimCalendar32 { get { return Asol.Tools.WorkScheduler.Properties.Resources.view_pim_calendar_32; } }
        /// <summary>
        /// Standardní ikona : Zoom
        /// </summary>
        public static Image ZoomFitBest32 { get { return Asol.Tools.WorkScheduler.Properties.Resources.zoom_fit_best_3_32; } }

        /// <summary>
        /// Standardní ikona : Třídit ASC
        /// </summary>
        public static Image SortAsc { get { return Asol.Tools.WorkScheduler.Properties.Resources.go_up_2_16; } }
        /// <summary>
        /// Standardní ikona : Třídit DESC
        /// </summary>
        public static Image SortDesc { get { return Asol.Tools.WorkScheduler.Properties.Resources.go_down_2_16; } }
        /// <summary>
        /// Standardní ikona : Vybraný řádek
        /// </summary>
        public static Image RowSelected { get { return Asol.Tools.WorkScheduler.Properties.Resources.dialog_accept_2_16; } }

        /// <summary>
        /// Standardní ikona : Info
        /// </summary>
        public static Image IconInfo { get { return Asol.Tools.WorkScheduler.Properties.Resources.help_contents_32; } }
        /// <summary>
        /// Standardní ikona : Help
        /// </summary>
        public static Image IconHelp { get { return Asol.Tools.WorkScheduler.Properties.Resources.help_32; } }

        /// <summary>
        /// Standardní ikona : Drak
        /// </summary>
        internal static Image Dragon { get { return Asol.Tools.WorkScheduler.Properties.Resources.Dragon_128; } }

        /// <summary>
        /// Standardní ikona : Stín vlevo nahoře
        /// </summary>
        internal static Image Shadow00 { get { return Asol.Tools.WorkScheduler.Properties.Resources.Shadow00; } }
        /// <summary>
        /// Standardní ikona : Stín nahoře
        /// </summary>
        internal static Image Shadow01 { get { return Asol.Tools.WorkScheduler.Properties.Resources.Shadow01; } }
        /// <summary>
        /// Standardní ikona : Stín vpravo nahoře
        /// </summary>
        internal static Image Shadow02 { get { return Asol.Tools.WorkScheduler.Properties.Resources.Shadow02; } }
        /// <summary>
        /// Standardní ikona : Stín
        /// </summary>
        internal static Image Shadow10 { get { return Asol.Tools.WorkScheduler.Properties.Resources.Shadow10; } }
        /// <summary>
        /// Standardní ikona : Stín
        /// </summary>
        internal static Image Shadow12 { get { return Asol.Tools.WorkScheduler.Properties.Resources.Shadow12; } }
        /// <summary>
        /// Standardní ikona : Stín
        /// </summary>
        internal static Image Shadow20 { get { return Asol.Tools.WorkScheduler.Properties.Resources.Shadow20; } }
        /// <summary>
        /// Standardní ikona : Stín
        /// </summary>
        internal static Image Shadow21 { get { return Asol.Tools.WorkScheduler.Properties.Resources.Shadow21; } }
        /// <summary>
        /// Standardní ikona : Stín
        /// </summary>
        internal static Image Shadow22 { get { return Asol.Tools.WorkScheduler.Properties.Resources.Shadow22; } }
    }
}
