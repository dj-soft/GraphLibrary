using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using WinForm = System.Windows.Forms;
using WinFormServices.Drawing;
using DevExpress.Utils.Svg;
using DevExpress.Utils.Design;

namespace Noris.Clients.Win.Components.AsolDX
{
#if Compile_TestDevExpress

    /// <summary>
    /// Adapter na systém TestDevExpress
    /// </summary>
    internal class CurrentSystemAdapter : ISystemAdapter
    {
        event EventHandler ISystemAdapter.InteractiveZoomChanged { add { } remove { } }
        decimal ISystemAdapter.ZoomRatio { get { return 1.0m; } }
        string ISystemAdapter.GetMessage(string messageCode, params object[] parameters) { return AdapterSupport.GetMessage(messageCode, parameters); }
        IEnumerable<IResourceItem> ISystemAdapter.GetResources() { return DataResources.GetResources(); }
        string ISystemAdapter.GetResourceItemKey(string name) { return DataResources.GetItemKey(name); }
        string ISystemAdapter.GetResourcePackKey(string name, out ResourceImageSizeType sizeType, out ResourceContentType contentType) { return DataResources.GetPackKey(name, out sizeType, out contentType); }
        byte[] ISystemAdapter.GetResourceContent(IResourceItem resourceItem) { return DataResources.GetResourceContent(resourceItem); }
        System.Windows.Forms.Shortcut ISystemAdapter.GetShortcutKeys(string shortCut) { return WinForm.Shortcut.None; }
        bool ISystemAdapter.CanRenderSvgImages { get { return false; } }
        Image ISystemAdapter.RenderSvgImage(SvgImage svgImage, Size size, ISvgPaletteProvider svgPalette) { return null; }
        /// <summary>
        /// Vyrenderuje Image která vypadá jako ikona pro zadaný text
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        Image ISystemAdapter.CreateCaptionImage(string caption, ResourceImageSizeType sizeType) { return AdapterSupport.CreateCaptionImage(caption, sizeType); }
    }
    /// <summary>
    /// Rozhraní předepisuje metodu <see cref="HandleEscapeKey()"/>, která umožní řešit klávesu Escape v rámci systému
    /// </summary>
    internal interface IEscapeKeyHandler
    {
        /// <summary>
        /// Systém zaregistroval klávesu Escape; a ptá se otevřených oken, které ji chce vyřešit...
        /// </summary>
        /// <returns></returns>
        bool HandleEscapeKey();
    }
#endif

    #region class AdapterSupport
    /// <summary>
    /// Obecný support pro adapter
    /// </summary>
    internal static class AdapterSupport
    {
        /// <summary>
        /// Vyrenderuje dodaný text jako náhradní ikonu
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        public static Image CreateCaptionImage(string caption, ResourceImageSizeType sizeType)
        {
            var imageSize = DxComponent.GetImageSize(sizeType, true);
            Bitmap bitmap = new Bitmap(imageSize.Width, imageSize.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                RectangleF bounds = new RectangleF(0, 0, imageSize.Width, imageSize.Height);
                graphics.FillRectangle(Brushes.White, bounds);
                if (caption != null)
                {
                    string text = caption.Trim()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("_", "")
                        .Replace("+", "")
                        .Replace("/", "")
                        .Replace("*", "")
                        .Replace("#", "");
                    if (text.Length > 2) text = text.Substring(0, 2);
                    if (text.Length > 0)
                    {
                        var font = SystemFonts.MenuFont;
                        var textSize = graphics.MeasureString(text, font);
                        var textBounds = textSize.AlignTo(bounds, ContentAlignment.MiddleCenter);
                        graphics.DrawString(text, font, Brushes.Black, textBounds.Location);
                    }
                }
            }
            return bitmap;
        }
        public static string GetMessage(string messageCode, IEnumerable<object> parameters)
        {
            if (messageCode == null) return null;
            string text = GetMessageText(messageCode);
            if (text == null) return null;
            return String.Format(text, parameters);
        }
        /// <summary>
        /// Najde defaultní text daného kódu hlášky.
        /// Najde hlášku ve třídě <see cref="MsgCode"/>, vyhledá tam konstantu zadaného názvu, načte atribut dekorující danou konstantu, 
        /// atribut typu <see cref="DefaultMessageTextAttribute"/>, načte jeho hodnotu <see cref="DefaultMessageTextAttribute.DefaultText"/>, a vrátí ji.
        /// </summary>
        /// <param name="messageCode"></param>
        /// <returns></returns>
        public static string GetMessageText(string messageCode)
        {
            var msgField = typeof(MsgCode).GetFields(System.Reflection.BindingFlags.Public).Where(f => f.Name == messageCode).FirstOrDefault();
            if (msgField == null) return null;
            var defTextAttr = msgField.GetCustomAttributes(typeof(DefaultMessageTextAttribute), true).Cast<DefaultMessageTextAttribute>().FirstOrDefault();
            if (defTextAttr is null || String.IsNullOrEmpty(defTextAttr.DefaultText)) return null;
            return defTextAttr.DefaultText;
        }
    }
    #endregion
    #region class DataResources
    /// <summary>
    /// <see cref="DataResources"/> : systém lokálních zdrojů (typicky obrázky), načtené ze souborů z adresářů
    /// </summary>
    internal static class DataResources
    {
        #region Načtení zdrojů z disku
        /// <summary>
        /// Volá se jedenkrát, vrátí kompletní seznam všech zdrojů (Resource).
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IResourceItem> GetResources()
        {
            string resourcePath = DxComponent.ApplicationPath;
            List<IResourceItem> resourceList = new List<IResourceItem>();
            LoadResources(resourcePath, 0, "Resources", resourceList);
            LoadResources(resourcePath, 1, "Images", resourceList);
            LoadResources(resourcePath, 1, "pic", resourceList);
            LoadResources(resourcePath, 1, "pic-0", resourceList);
            return resourceList;
        }
        /// <summary>
        /// Zkusí najít zdroje v jednom adresáři
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="upDirs"></param>
        /// <param name="subDir"></param>
        /// <param name="resourceList"></param>
        private static void LoadResources(string resourcePath, int upDirs, string subDir, List<IResourceItem> resourceList)
        {
            string path = resourcePath;
            for (int i = 0; i < upDirs && !String.IsNullOrEmpty(path); i++)
                path = System.IO.Path.GetDirectoryName(path);
            if (String.IsNullOrEmpty(path)) return;
            if (!String.IsNullOrEmpty(subDir)) path = System.IO.Path.Combine(path, subDir);
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(path);
            if (!dirInfo.Exists) return;

            int commonPathLength = path.Length;
            foreach (var fileInfo in dirInfo.EnumerateFiles("*.*", System.IO.SearchOption.AllDirectories))
            {
                if (fileInfo == null || !fileInfo.Exists) continue;
                string fullName = fileInfo.FullName;
                if (fullName.Length <= commonPathLength) continue;
                string relativeName = fullName.Substring(commonPathLength);
                string itemKey = GetItemKey(relativeName);
                string packKey = GetPackKey(itemKey, out ResourceImageSizeType sizeType, out ResourceContentType contentType);
                if (contentType == ResourceContentType.None) continue;

                DataResourceItem resourceItem = new DataResourceItem(fullName, itemKey, packKey, sizeType, contentType);
                resourceList.Add(resourceItem);
            }
        }
        /// <summary>
        /// Vrátí korektně formátovaný klíč resource (provede Trim, ToLower, a náhradu zpětných lomítek a odstranění úvodních lomítek
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetItemKey(string name)
        {
            string key = (name ?? "").Trim().ToLower().Replace("\\", "/");
            while (key.Length > 0 && key[0] == '/') key = key.Substring(1).Trim();
            return key;
        }
        /// <summary>
        /// Vrátí obecné jméno zdroje z dodaného plného jména zdroje (oddělí velikost a typ souboru podle suffixu a přípony).
        /// Pro vstupní text např. "Noris/pic/AddressDelete-32x32.png" vrátí "Noris/pic/AddressDelete"
        /// a nastaví <paramref name="sizeType"/> = <see cref="ResourceImageSizeType.Large"/>;
        /// a <paramref name="contentType"/> = <see cref="ResourceContentType.Bitmap"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sizeType"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static string GetPackKey(string name, out ResourceImageSizeType sizeType, out ResourceContentType contentType)
        {
            string packKey = GetItemKey(name);
            sizeType = ResourceImageSizeType.None;
            contentType = ResourceContentType.None;
            if (!String.IsNullOrEmpty(packKey))
                if (RemoveContentTypeByExtension(ref packKey, out contentType) && ContentTypeSupportSize(contentType))
                    RemoveSizeTypeBySuffix(ref packKey, out sizeType);
            return packKey;
        }
        /// <summary>
        /// Vrací true, pokud dodaný typ obsahu podporuje uvádění velikosti v názvu zdroje (souboru). Typicky jde o obrázky.
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private static bool ContentTypeSupportSize(ResourceContentType contentType)
        {
            switch (contentType)
            {
                case ResourceContentType.Bitmap:
                case ResourceContentType.Vector:
                case ResourceContentType.Icon:
                case ResourceContentType.Cursor:
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Z dodaného jména souboru určí suffix, a podle něj detekuje velikost obrázku (dá do out parametru) a detekovaný suffix odřízne (celý).
        /// Vrátí true, pokud nějakou velikost detekoval a odřízl (tedy <paramref name="sizeType"/> je jiný než None). 
        /// Vrátí false, když je vstup prázdný, nebo bez suffixu nebo s neznámým suffixem, pak suffix neodřízne.
        /// <para/>
        /// Například pro vstup: "C:/Images/Button-24x24" detekuje <paramref name="sizeType"/> = <see cref="ResourceImageSizeType.Medium"/>, 
        /// a v ref parametru <paramref name="name"/> ponechá: "C:/Images/Button".
        /// <para/>
        /// Tato metoda se typicky volá až po metodě <see cref="RemoveContentTypeByExtension(ref string, out ResourceContentType)"/>, protože tam se řeší a odřízne přípona, a následně se zde řeší suffix jména souboru.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private static bool RemoveSizeTypeBySuffix(ref string name, out ResourceImageSizeType sizeType)
        {
            sizeType = ResourceImageSizeType.None;
            if (String.IsNullOrEmpty(name)) return false;
            name = name.TrimEnd();
            int index = name.LastIndexOf("-");
            if (index <= 0) return false;
            string suffix = name.Substring(index).ToLower();
            switch (suffix)
            {
                case "-16x16":
                case "-small":
                    sizeType = ResourceImageSizeType.Small;
                    break;
                case "-24x24":
                    sizeType = ResourceImageSizeType.Medium;
                    break;
                case "-32x32":
                case "-large":
                    sizeType = ResourceImageSizeType.Large;
                    break;
            }
            return (sizeType != ResourceImageSizeType.None);

            /*

            Noris/pic/address-book-large.svg
            Noris/pic/address-book-locations-large.svg
            Noris/pic/address-book-locations-small.svg
            Noris/pic/address-book-small.svg
            Noris/pic/address-book-undo-2-large.svg
            Noris/pic/address-book-undo-2-small.svg
            Noris/pic/address-book-update-bottom-left-large.svg
            Noris/pic/address-book-update-bottom-left-small.svg
            Noris/pic/AddressDelete-16x16.png
            Noris/pic/AddressDelete-24x24.png
            Noris/pic/AddressDelete-32x32.png
            Noris/pic/AddressEdit-16x16.png
            Noris/pic/AddressEdit-24x24.png
            Noris/pic/AddressEdit-32x32.png
            Noris/pic/AddressCheckedRuian-16x16.png
            Noris/pic/AddressCheckedRuian-24x24.png
            Noris/pic/AddressCheckedRuian-32x32.png

            */
        }
        /// <summary>
        /// Z dodaného jména souboru určí příponu, podle ní detekuje typ obsahu (dá do out parametru) a detekovanou příponu odřízne (včetně tečky).
        /// Vrátí true, pokud nějakou příponu detekoval a odřízl (tedy <paramref name="contentType"/> je jiný než None). 
        /// Vrátí false, když je vstup prázdný, nebo bez přípony nebo s neznámou příponou, pak příponu neodřízne.
        /// <para/>
        /// Například pro vstup: "C:/Images/Button-24x24.png" detekuje <paramref name="contentType"/> = <see cref="ResourceContentType.Bitmap"/>, 
        /// a v ref parametru <paramref name="name"/> ponechá: "C:/Images/Button-24x24".
        /// <para/>
        /// Tato metoda se typicky volá před metodou <see cref="RemoveSizeTypeBySuffix(ref string, out ResourceImageSizeType)"/>, protože tady se řeší a odřízne přípona, a následně se tam řeší suffix jména souboru.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="contentType"></param>
        private static bool RemoveContentTypeByExtension(ref string name, out ResourceContentType contentType)
        {
            contentType = ResourceContentType.None;
            if (String.IsNullOrEmpty(name)) return false;
            name = name.TrimEnd();
            string extension = System.IO.Path.GetExtension(name).ToLower();
            switch (extension)
            {
                case ".bmp":
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".pcx":
                case ".tif":
                case ".tiff":
                    contentType = ResourceContentType.Bitmap;
                    break;
                case ".svg":
                    contentType = ResourceContentType.Vector;
                    break;
                case ".mp4":
                case ".mpg":
                case ".mpeg":
                case ".avi":
                    contentType = ResourceContentType.Video;
                    break;
                case ".wav":
                case ".flac":
                case ".mp3":
                case ".mpc":
                    contentType = ResourceContentType.Audio;
                    break;
                case ".ico":
                    contentType = ResourceContentType.Icon;
                    break;
                case ".cur":
                    contentType = ResourceContentType.Cursor;
                    break;
                case ".htm":
                case ".html":
                case ".xml":
                    contentType = ResourceContentType.Xml;
                    break;
            }
            if (contentType != ResourceContentType.None)
                name = name.Substring(0, name.Length - extension.Length);
            return (contentType != ResourceContentType.None);
        }
        /// <summary>
        /// Vrátí obsah daného zdroje.
        /// </summary>
        /// <param name="resourceItem"></param>
        public static byte[] GetResourceContent(IResourceItem resourceItem)
        {
            return ((resourceItem is DataResourceItem dataItem) ? dataItem.Content : null);
        }
        #endregion
    }
    #endregion
    #region class DataResourceItem
    /// <summary>
    /// Třída obsahující reálně jeden prvek resource - klíče, jméno souboru, OnDemand načtený obsah
    /// </summary>
    internal class DataResourceItem : IResourceItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="itemKey"></param>
        /// <param name="packKey"></param>
        /// <param name="sizeType"></param>
        /// <param name="contentType"></param>
        internal DataResourceItem(string fileName, string itemKey, string packKey, ResourceImageSizeType sizeType, ResourceContentType contentType)
        {
            FileName = fileName;
            ItemKey = itemKey;
            PackKey = packKey;
            ContentType = contentType;
            SizeType = sizeType;
            _Content = null;
            _ContentLoaded = false;
        }
        /// <summary>
        /// Plné jméno souboru
        /// </summary>
        public string FileName { get; private set; }
        /// <summary>
        /// Klíč prvku (bez root adresáře, včetně velikosti a včetně přípony)
        /// </summary>
        public string ItemKey { get; private set; }
        /// <summary>
        /// Klíč skupiny (bez root adresáře, bez velikosti a bez přípony)
        /// </summary>
        public string PackKey { get; private set; }
        /// <summary>
        /// Typ obsahu
        /// </summary>
        public ResourceContentType ContentType { get; private set; }
        /// <summary>
        /// Typ velikosti
        /// </summary>
        public ResourceImageSizeType SizeType { get; private set; }
        /// <summary>
        /// Obsah zdroje = načtený z odpovídajícího souboru
        /// </summary>
        public byte[] Content { get { return _GetContent(); } }
        /// <summary>
        /// Vrátí obsah zdroje, buď dříve již načtený, nebo jej načte nyní. 
        /// </summary>
        /// <returns></returns>
        private byte[] _GetContent()
        {
            if (!_ContentLoaded)
            {
                _ContentLoaded = true;
                try { _Content = System.IO.File.ReadAllBytes(this.FileName); }
                catch { }
            }
            return _Content;
        }
        private byte[] _Content;
        private bool _ContentLoaded;
    }
    #endregion

}
