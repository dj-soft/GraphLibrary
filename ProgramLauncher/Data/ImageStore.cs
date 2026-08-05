using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    /// <summary>
    /// Třída slouží k uchování dat obrázků v souboru uloženém vedle <see cref="Settings.FileName"/>, 
    /// do tohoto Store jsou ukládány obrázky, které se fyzicky načetly ze zdrojových souborů.
    /// Pokud poté zdrojový soubor zmizí (je přemístěn), pak se bude používat jeho offlone kopie z tohoto Store.
    /// </summary>
    public class ImageStore : IDisposable
    {
        #region Konstrukce a Dispose
        /// <summary>
        /// Konstruktor, který vytvoří instanci ImageStore a připraví interní evidenci obrázků.
        /// </summary>
        public ImageStore() 
        {
            __Store = new Dictionary<string, ImageItem>();
        }
        public void Dispose()
        {
            _SaveStore(false);
            foreach (var item in __Store.Values)
                item.Dispose();
            __Store.Clear();
        }
        private Dictionary<string, ImageItem> __Store;
        #endregion
        #region Standardní získání Image ze souboru
        /// <summary>
        /// Vrátí klíč pro Image
        /// </summary>
        /// <param name="name"></param>
        /// <param name="iconIndex"></param>
        /// <returns></returns>
        public static string GetImageKey(string name, int? iconIndex)
        {
            var key = name.Trim().ToLower().Replace("\\", "/");
            if (iconIndex.HasValue && iconIndex.Value != 0) key += $":{iconIndex.Value}";          // SubKey s číslem ikony jen když je index ikony != 0, protože 0 je defaultní hodnota.
            return key;
        }
        /// <summary>
        /// Najde / vytvoří a vrátí Image z dané definice.
        /// </summary>
        /// <param name="imageName">Jméno souboru s obrázkem (PNG, JPG, BMP), nebo ikona (ICO), anebo soubor s ikonou (DLL, EXE)</param>
        /// <param name="iconIndex">Index ikony, pokud soubor odkazuje na DLL/EXE</param>
        /// <returns></returns>
        public Image GetImage(string imageName, int? iconIndex = null)
        {
            Image image = null;

            if (String.IsNullOrEmpty(imageName)) return image;
            imageName = imageName.Trim();

            string key = ImageStore.GetImageKey(imageName, iconIndex);
            lock (__Store)
            {   // Pouze jeden thread: požadavky seřadíme do fronty!
                _CheckStore();
                if (!__Store.TryGetValue(key, out var item))
                {   // Vytvoříme novou položku, která bude mít jen jméno souboru a index ikony, ale nemá načtený Image ani jeho obsah:
                    item = new ImageItem(this, imageName, iconIndex ?? 0, null, null);
                    __Store.Add(key, item);
                }

                item.CheckImage();

                image = item.Image;
            }

            return image;
        }
        #endregion
        #region Offline store
        /// <summary>
        /// Zahájí časovač, který po nějakém čase uloží data do Store souboru. Pokud již časovač běží, pak se restartuje jeho Timeout.
        /// </summary>
        internal void StartTimerToSave()
        {
            // Nastartujeme časovač, který po daném čase 2000 milisec zavolá metodu _TimerSaveStore, která uloží data do Store souboru.
            // Pokud již časovač s daným Guidem běží, pak se restartuje jeho Timeout.
            _TimerSaveStoreGuid = WatchTimer.CallMeAfter(_TimerSaveStore, 2000, false, _TimerSaveStoreGuid);
        }
        /// <summary>
        /// Guid našeho časovače. Pokud je null, pak časovač neběží. 
        /// Pokud není null, pak časovač běží a po nějakém čase zavolá <see cref="_TimerSaveStore"/>.
        /// Předáním tohoto Guidu do <see cref="WatchTimer.CallMeAfter(Action, int, bool, Guid?)"/> se časovač restartuje a Timeout se začne počítat znovu.
        /// </summary>
        private Guid? _TimerSaveStoreGuid;
        /// <summary>
        /// Zavolá <see cref="_SaveStore(bool)"/> s force = false, aby se uložila data do Store souboru, protože se změnila.
        /// </summary>
        private void _TimerSaveStore()
        {
            _TimerSaveStoreGuid = null;
            _SaveStore(false);
        }
        /// <summary>
        /// Zajistí, že v offline store budou aktuální data Images ze StoreFile
        /// </summary>
        private void _CheckStore()
        {
            var settingFile = _SettingsStoreFileName;
            var loadedFile = __ImageStoreFileName;
            if (String.Equals(settingFile, loadedFile)) return;

            _LoadStore(settingFile);
        }
        /// <summary>
        /// Do zdejší evidence načte data ze Store souboru, který je uložen vedle <see cref="Settings.FileName"/>. Pokud soubor neexistuje, pak se nic nenačte.
        /// Před tím odebere úadje ze Store souboru z načtených položek, a pokud některá položka zůstane prázdná, pak se odstraní z evidence.
        /// </summary>
        /// <param name="storeFileName"></param>
        private void _LoadStore(string storeFileName)
        {
            __ImageStoreFileName = storeFileName;
            
            // Smazat offline data z načtených položek, a poté odebrat prázdné položky:
            __Store.Values.ForEachExec(i => i.ClearStoreData());
            var emptyKeys = __Store.Where(kv => kv.Value.IsEmpty).Select(kv => kv.Key).ToArray();
            emptyKeys.ForEachExec(key => __Store.Remove(key));

            // Načíst offline data z Store souboru:
            if (String.IsNullOrEmpty(storeFileName)) return;
            if (!System.IO.File.Exists(storeFileName)) return;

            using (var stream = new System.IO.FileStream(storeFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                var count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var imageNameItem = reader.ReadString();
                    var iconIndexItem = reader.ReadInt32();
                    var storeDataLength = reader.ReadInt32();
                    var storeData = reader.ReadBytes(storeDataLength);

                    addStoreData(imageNameItem, iconIndexItem, storeData);
                }
            }

            // Uloží data načtená ze Store souboru do zdejší evidence, pokud tam ještě nejsou:
            void addStoreData(string imageNm, int iconIdx, byte[] storeDat)
            {
                var key = GetImageKey(imageNm, iconIdx);
                if (__Store.TryGetValue(key, out var item))
                {
                    item.SetStoreData(storeDat);
                }
                else
                {
                    item = new ImageItem(this, imageNm, iconIdx, null, storeDat);
                    __Store.Add(key, item);
                }
            }
        }
        /// <summary>
        /// Uloží svoje data do Store souboru, pokud je zadán. Pokud force = true, pak se uloží i bez změn.
        /// </summary>
        /// <param name="force"></param>
        private void _SaveStore(bool force)
        {
            // Cílový Store soubor:
            var storeFileName = __ImageStoreFileName;
            if (String.IsNullOrEmpty(storeFileName)) return;                   // Není zadán soubor

            // Možná nebudeme ukládat, pokud se nic nezměnilo, ale pokud je force = true, pak se uloží i bez změn.
            bool hasChanges = __Store.Values.Any(i => i.StoreDataChanged);     // Pokud máme nějakou položku se změnou...
            bool needSave = hasChanges || force;
            if (!needSave) return;

            // Co budeme ukládat: ty položky, které mají nejaká načtená data; primárně z ImageData, ale pokud není, pak ze StoreData:
            var images = __Store.Values.Where(i => i.HasData).ToArray();

            using (var stream = new System.IO.FileStream(storeFileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(images.Length);
                foreach (var item in images)
                {
                    var dataToStore = item.DataForStore;
                    writer.Write(item.ImageName);
                    writer.Write(item.IconIndex);
                    writer.Write(dataToStore.Length);
                    writer.Write(dataToStore);
                    item.StoreDataChanged = false;
                }
            }
        }
        /// <summary>
        /// Obsahuje plné jméno souboru, kde jsou fyzicky uložena offline data Images.
        /// </summary>
        private string __ImageStoreFileName = null;
        /// <summary>
        /// Vrátí plné jméno souboru, kde by měla být uložena data podle jména soubour Settings (<see cref="App.Settings.FileName"/>).
        /// </summary>
        private string _SettingsStoreFileName
        {
            get
            {
                var settingsFile = App.Settings.FileName;
                if (String.IsNullOrEmpty(settingsFile)) return null;
                var path = System.IO.Path.GetDirectoryName(settingsFile);
                var name = System.IO.Path.GetFileNameWithoutExtension(settingsFile);
                return System.IO.Path.Combine(path, name + ".imgstore");
            }
        }
        #endregion
        #region class ImageItem
        /// <summary>
        /// Data o jednom obrázku: buď z originálního souboru, nebo ze Store souboru. Obsahuje i vytvořený Image pro rychlé kreslení.
        /// </summary>
        private class ImageItem : IDisposable
        {
            #region Konstruktor a základní data
            /// <summary>
            /// Konstruktor, který vytvoří instanci ImageItem s danými daty.
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="imageName"></param>
            /// <param name="iconIndex"></param>
            /// <param name="imageData"></param>
            /// <param name="storeData"></param>
            public ImageItem(ImageStore owner, string imageName, int iconIndex, byte[] imageData, byte[] storeData)
            {
                __Owner = owner;
                ImageName = imageName;
                IconIndex = iconIndex;
                ImageData = imageData;
                StoreData = storeData;
            }
            /// <summary>
            /// Vrátí textovou informaci o této položce, která obsahuje jméno souboru, index ikony, délku ImageData a StoreData a příznak StoreDataChanged.
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                string text = this.ImageName;
                if (IconIndex != 0) text += $":{IconIndex}";
                if (ImageData != null) text += $";  ImageData: {ImageData.Length:N0} B";
                if (StoreData != null) text += $";  StoreData: {StoreData.Length:N0} B";
                if (StoreDataChanged) text += ";  StoreDataChanged";
                return text;
            }
            /// <summary>
            /// Owner
            /// </summary>
            private ImageStore __Owner;
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                if (Image != null)
                {
                    Image.Dispose();
                    Image = null;
                }
                ImageData = null;
                StoreData = null;
                __Owner = null;
            }
            /// <summary>
            /// Plný náze souboru
            /// </summary>
            public string ImageName { get; private set; }
            /// <summary>
            /// Index ikony, nebo 0, pokud se jedná o obrázek, který není ikona.
            /// </summary>
            public int IconIndex { get; private set; }
            /// <summary>
            /// Vrátí true, pokud je položka prázdná = nemá ani StoreData ani ImageData.
            /// </summary>
            public bool IsEmpty { get { return (StoreData == null && ImageData == null); } }
            #endregion
            #region Data ze vstupního Image souboru
            /// <summary>
            /// Uložená data obrázku, která se načítají z originálního fyzického souboru.
            /// </summary>
            public byte[] ImageData { get; private set; }
            /// <summary>
            /// Obsahuje true, pokud proběhl pokus o načtení dat obrázku z daného souboru <see cref="ImageName"/>. 
            /// Pokud je false, pak jsme to ještě nezkoušeli.
            /// Pokud jsme to zkusili, ale soubor neexistuje, pak zde je true a v datech <see cref="ImageData"/> je null.
            /// </summary>
            public bool ImageFileChecked { get; private set; }
            /// <summary>
            /// Zajistí prvotní načtení dat z originálního souboru <see cref="ImageName"/> a uloží je do <see cref="ImageData"/>.
            /// Pokud jsme to již zkoušeli, pak <see cref="ImageFileChecked"/> je true a znovu se to nezkouší.
            /// </summary>
            public void CheckImage()
            {
                // Ceou tuto akci provedu pro jednu tuto instanci jen jedenkrát, až bude třeba získat její Image. Pokud se to již zkoušelo, pak se to znovu nezkouší.
                if (this.ImageFileChecked) return;
                this.ImageFileChecked = true;

                _TryLoadLocalFile();
                _TrySolveStoreData();
            }
            /// <summary>
            /// Zkusí načíst lokální soubor s obrázkem, pokud existuje. Pokud neexistuje, pak nenačte nic.
            /// </summary>
            private void _TryLoadLocalFile()
            {
                string imageName = this.ImageName;
                if (String.IsNullOrEmpty(imageName)) return;
                try
                {
                    _LoadLocalFile();
                }
                catch (Exception)
                {
                    this.ImageData = null;
                    this.Image = null;
                }
            }
            /// <summary>
            /// Načte obrázek (případně ikonu) ze svého souboru.
            /// Zajistí, že binární obsah bude uložen do <see cref="ImageData"/>, a objekt bitmapy v <see cref="Image"/>.
            /// </summary>
            /// <returns></returns>
            private void _LoadLocalFile()
            {
                this.ImageData = null;
                this.Image = null;

                string imageName = this.ImageName;
                if (String.IsNullOrEmpty(imageName)) return;

                imageName = System.Environment.ExpandEnvironmentVariables(imageName);      // %windir%  =>  C:\Windows    atd
                if (!System.IO.File.Exists(imageName)) return;

                imageName = System.IO.Path.GetFullPath(imageName);
                var extension = System.IO.Path.GetExtension(imageName).ToLower();
                switch (extension)
                {
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".bmp":
                    case ".gif":
                        // Soubor načtu do byte[] content; z nějk vytvořím Stream a z něj Image.
                        // Tak mám: jedno čtení souboru, a dva potřebné výstupy:
                        this.ImageData = System.IO.File.ReadAllBytes(imageName);
                        using (var imageStream = new System.IO.MemoryStream(this.ImageData))
                            this.Image = Image.FromStream(imageStream);
                        break;

                    case ".ico":
                        // Tady nechci do content načítat obsah souboru, prtože to je ikona.
                        // Radši v něm budu mít Bitmapu PNG:
                        using (var icon = new Icon(imageName, new Size(48, 48)))
                        {
                            var bitmap = icon.ToBitmap();                                // Vytvoří new instanci Image = izolovanou od Icon
                            this.Image = bitmap;
                            this.ImageData = storeIconToImageContent(bitmap);
                        }                                                                // Icon lze disposovat
                        break;

                    case ".exe":
                    case ".dll":
                        using (var icon = Icon.ExtractAssociatedIcon(imageName))
                        {
                            var bitmap = icon.ToBitmap();                                // Vytvoří new instanci Image = izolovanou od Icon
                            this.Image = bitmap;
                            this.ImageData = storeIconToImageContent(bitmap);
                        }                                                                // Icon lze disposovat
                        break;
                }


                // Dodanou Bitmapu převede na PNG a uloží do byte[] content a vrátí. Výsledek se použije pro ImageStore.
                byte[] storeIconToImageContent(Bitmap bitmap)
                {
                    using (var memoryStream = new System.IO.MemoryStream())
                    {
                        bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                        return memoryStream.ToArray();
                    }
                }
            }
            #endregion
            #region Data ze Store souboru
            /// <summary>
            /// Data obrázku načtená ze Store souboru, nemusí být aktuální vůči originálnímu souboru, ale pokud originální soubor zmizí, pak se použije tato offline záložní kopie.
            /// </summary>
            public byte[] StoreData { get; private set; }
            /// <summary>
            /// Příznak, že tento objekt má změněná data StoreData, která je potřeba uložit do Store souboru.
            /// </summary>
            public bool StoreDataChanged { get; set; }
            /// <summary>
            /// Zajistí buď aktualizaci <see cref="StoreData"/> z <see cref="ImageData"/>, pokud je k dispozici, 
            /// anebo pokud není, pak z <see cref="StoreData"/> vytvoří <see cref="Image"/>.
            /// </summary>
            private void _TrySolveStoreData()
            {
                if (this.ImageData != null)
                {   // Pokud máme načtená ImageData, pak zajistíme, že StoreData bude mít stejný obsah, aby se pak následně uložilo do Store souboru:
                    if (!_EqualContent(this.ImageData, this.StoreData))
                    {   // Pokud se liší, pak aktualizujeme StoreData a nastavíme příznak, že se změnila a požádáme Ownera, aby data po nějakém čase uložil:
                        this.StoreData = this.ImageData;
                        this.StoreDataChanged = true;
                        this.__Owner?.StartTimerToSave();
                    }
                    // Víc řešit nmusíme, protože máme aktuální ImageData a z ní se vytvořil Image.
                }
                else if (this.StoreData != null)
                {   // Nemáme ImageData, ale máme StoreData, pak z ní vytvoříme v podstatě záložní Image:
                    try
                    {
                        using (var imageStream = new System.IO.MemoryStream(this.StoreData))
                            this.Image = Image.FromStream(imageStream);
                    }
                    catch (Exception exc)
                    {
                        this.Image = null;
                    }
                }
            }
            /// <summary>
            /// Vrací true, pokud dodané dvě pole byte mají identický obsah. 
            /// Pokud jsou obě null, pak vrátí true. 
            /// Pokud je jedna null a druhá ne, pak vrátí false. 
            /// Pokud mají různou délku, pak vrátí false.
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            private static bool _EqualContent(byte[] a, byte[] b)
            {
                if (a == null && b == null) return true;
                if (a == null || b == null) return false;
                if (a.Length != b.Length) return false;
                for (int i = 0; i < a.Length; i++)
                    if (a[i] != b[i]) return false;
                return true;
            }
            /// <summary>
            /// Zahodí data načtená ze Store souboru.
            /// </summary>
            public void ClearStoreData()
            {
                StoreData = null;
                StoreDataChanged = false;
            }
            /// <summary>
            /// Uloží si data načená ze Store souboru, která se načítají z originálního fyzického souboru.
            /// </summary>
            /// <param name="storeData"></param>
            public void SetStoreData(byte[] storeData)
            {
                StoreData = storeData;
                StoreDataChanged = false;
            }
            /// <summary>
            /// Vrátí true, pokud má položka nějaká data = buď ImageData nebo StoreData.
            /// Pak má smysl získat obsah dat z <see cref="DataForStore"/>.
            /// </summary>
            public bool HasData { get { return (ImageData != null || StoreData != null); } }
            /// <summary>
            /// Data pro uložení do Store: primárně ImageData, ale pokud není, pak StoreData. Pokud není ani jedno, pak vrátí null.
            /// </summary>
            public byte[] DataForStore { get { return ImageData ?? StoreData; } }
            #endregion
            #region Bitmapa
            /// <summary>
            /// Vytvořený obrázek pro rychlé kreslení, který je načtený z originálního souboru nebo ze Store souboru.
            /// </summary>
            public Image Image { get; private set; }
            private Image __Image;
            #endregion
        }
        #endregion
    }
}
