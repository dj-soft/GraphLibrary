// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

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
    /// <summary>
    /// Adapter na systém TestDevExpress
    /// </summary>
    internal class CurrentSystemAdapter : ISystemAdapter
    {
        event EventHandler ISystemAdapter.InteractiveZoomChanged { add { } remove { } }
        decimal ISystemAdapter.ZoomRatio { get { return 1.0m; } }
        string ISystemAdapter.GetMessage(MsgCode messageCode, params object[] parameters) { return AdapterSupport.GetMessage(messageCode, parameters); }
        bool ISystemAdapter.IsPreferredVectorImage { get { return true; } }
        ResourceImageSizeType ISystemAdapter.ImageSizeStandard { get { return ResourceImageSizeType.Medium; } }
        IEnumerable<IResourceItem> ISystemAdapter.GetResources() { return DataResources.GetResources(); }
        string ISystemAdapter.GetResourceItemKey(string name) { return DataResources.GetItemKey(name); }
        string ISystemAdapter.GetResourcePackKey(string name, out ResourceImageSizeType sizeType, out ResourceContentType contentType) { return DataResources.GetPackKey(name, out sizeType, out contentType); }
        byte[] ISystemAdapter.GetResourceContent(IResourceItem resourceItem) { return DataResources.GetResourceContent(resourceItem); }

        bool ISystemAdapter.CanRenderSvgImages { get { return false; } }
        Image ISystemAdapter.RenderSvgImage(SvgImage svgImage, Size size, ISvgPaletteProvider svgPalette) { return null; }

        System.ComponentModel.ISynchronizeInvoke ISystemAdapter.Host { get { return DxComponent.MainForm ?? WinForm.Form.ActiveForm; } }
        WinForm.Shortcut ISystemAdapter.GetShortcutKeys(string shortCut) { return WinForm.Shortcut.None; }
        void ISystemAdapter.TraceText(TraceLevel level, Type type, string method, string keyword, params object[] arguments) { }
    }
    /// <summary>
    /// Rozhraní předepisuje metodu <see cref="HandleEscapeKey()"/>, která umožní řešit klávesu Escape v rámci systému
    /// </summary>
    internal interface IEscapeHandler
    {
        /// <summary>
        /// Systém zaregistroval klávesu Escape; a ptá se otevřených oken, které ji chce vyřešit...
        /// </summary>
        /// <returns></returns>
        bool HandleEscapeKey();
    }
    #region class AdapterSupport
    /// <summary>
    /// Obecný support pro adapter
    /// </summary>
    internal static class AdapterSupport
    {
        /// <summary>
        /// Vytvoří <see cref="SvgImage"/> pro daný text, namísto chybějící ikony.
        /// Pokud vrátí null, zkusí se provést <see cref="CreateCaptionImage(string, ResourceImageSizeType?, Size?)"/>.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        /// <returns></returns>
        public static SvgImage CreateCaptionVector(string caption, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            string text = DxComponent.GetCaptionForIcon(caption).ToUpper();
            if (text.Length > 2) text = text.Substring(0, 2);
            bool isWidth = (text == "MM" || text == "OO" || text == "WW" || text == "QQ" || text == "AA");
            string fillClass = "White";
            string borderClass = "Blue";
            string textClass = "Black";
            string sizePx = (isWidth ? "16px" : "18px");
            string textY = (isWidth ? "20" : "22");
            string weight = (isWidth ? "600" : "800");      // bold
            string svgContent = @"﻿<?xml version='1.0' encoding='UTF-8'?>
<svg x='0px' y='0px' viewBox='0 0 32 32' 
        version='1.1' xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' xml:space='preserve' 
        id='Layer_1' 
        style='enable-background:new 0 0 32 32'>
  <style type='text/css'>
	.White{fill:#FFFFFF;}
	.Red{fill:#D11C1C;}
	.Green{fill:#039C23;}
	.Blue{fill:#1177D7;}
	.Yellow{fill:#FFB115;}
	.Black{fill:#727272;}
	.st0{opacity:0.75;}
	.st1{opacity:0.5;}
  </style>
  <g id='icon" + text + @"' style='font-size: " + sizePx + @"; text-anchor: middle; font-family: serif; font-weight: " + weight + @"'>
    <path d='M31,0H1C0.5,0,0,0.5,0,1v30c0,0.5,0.5,1,1,1h30c0.5,0,1-0.5,1-1V1C32,0.5,31.5,0,31,0z M30,30H2V2h28V30z' class='" + borderClass + @"' />
    <path d='M30,30H2V2h28V30z' class='" + fillClass + @"' />
    <text x='16' y='" + textY + @"' class='" + textClass + @"'>" + text + @"</text>
  </g>
</svg>";
            svgContent = svgContent.Replace("'", "\"");
            return DxSvgImage.Create(caption, DxSvgImagePaletteType.Explicit, svgContent);

            /*

﻿<?xml version='1.0' encoding='UTF-8'?>
<svg x="0px" y="0px" viewBox="0 0 32 32" 
        version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" xml:space="preserve" 
        id="Layer_1" 
        style="enable-background:new 0 0 32 32">
  <style type="text/css">
	.Red{fill:#D11C1C;}
	.Green{fill:#039C23;}
	.Blue{fill:#1177D7;}
	.Yellow{fill:#FFB115;}
	.Black{fill:#727272;}
	.st0{opacity:0.75;}
	.st1{opacity:0.5;}
  </style>
  <g id="iconAB" style="font-size: 16px; text-anchor: middle; font-family: serif; font-weight: bold">
    <path d="M31,0H1C0.5,0,0,0.5,0,1v30c0,0.5,0.5,1,1,1h30c0.5,0,1-0.5,1-1V1C32,0.5,31.5,0,31,0z M30,30H2V2h28V30z" class="Black" />
    <!--  path d="M0,0L31,0L31,31L0,31L0,0Z" class="Black" / -->
    <text x="16" y="20" class="Blue">MM</text>
  </g>
</svg>

            */
        }
        /// <summary>
        /// Vyrenderuje dodaný text jako náhradní ikonu
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        /// <returns></returns>
        public static Image CreateCaptionImage(string caption, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            var realSize = imageSize ?? DxComponent.GetImageSize((sizeType ?? ResourceImageSizeType.Large), true);
            bool isDark = DxComponent.IsDarkTheme;
            Bitmap bitmap = new Bitmap(realSize.Width, realSize.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                RectangleF bounds = new RectangleF(0, 0, realSize.Width, realSize.Height);
                graphics.FillRectangle((isDark ? Brushes.MidnightBlue : Brushes.MintCream), bounds);
                string text = DxComponent.GetCaptionForIcon(caption);
                if (text.Length > 0)
                {
                    var font = SystemFonts.MenuFont;
                    var textSize = graphics.MeasureString(text, font);
                    var textBounds = textSize.AlignTo(bounds, ContentAlignment.MiddleCenter);
                    graphics.DrawString(text, font, (isDark ? Brushes.White : Brushes.Black), textBounds.Location);
                }
            }
            return bitmap;
        }
        /// <summary>
        /// Vrátí lokalizovanou hlášku
        /// </summary>
        /// <param name="messageCode"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string GetMessage(MsgCode messageCode, IEnumerable<object> parameters)
        {
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
        public static string GetMessageText(MsgCode messageCode)
        {
            if (_Messages == null) _Messages = new Dictionary<MsgCode, string>();
            string text = null;
            if (!_Messages.TryGetValue(messageCode, out text))
            {
                var msgField = typeof(MsgCode).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Where(f => f.Name == messageCode.ToString()).FirstOrDefault();
                if (msgField != null)
                {
                    var defTextAttr = msgField.GetCustomAttributes(typeof(DefaultMessageTextAttribute), true).Cast<DefaultMessageTextAttribute>().FirstOrDefault();
                    if (!(defTextAttr is null || String.IsNullOrEmpty(defTextAttr.DefaultText)))
                        text = defTextAttr.DefaultText;
                }

                // Pro daný kód si text uložím vždy, i když nebyl nalezen (=null) = abych jej příště jak osel nehledal znovu:
                lock (_Messages)
                {
                    if (!_Messages.ContainsKey(messageCode))
                        _Messages.Add(messageCode, text);
                }
            }
            return text;
        }
        /// <summary>
        /// Cache pro již lokalizované hlášky
        /// </summary>
        private static Dictionary<MsgCode, string> _Messages = null;
    }
    #endregion
    #region class DataResources
    /// <summary>
    /// <see cref="DataResources"/> : systém lokálních zdrojů (typicky obrázky), načtené ze souborů z adresářů
    /// </summary>
    internal static class DataResources
    {
        #region Načtení zdrojů
        /// <summary>
        /// Volá se jedenkrát, vrátí kompletní seznam všech zdrojů (Resource).
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IResourceItem> GetResources()
        {
            List<IResourceItem> resourceList = new List<IResourceItem>();
            AddResourcesFromResourcesBin(resourceList);
            if (resourceList.Count == 0)   // || String.Equals(DxComponent.ApplicationName, "TestDevExpress.exe", StringComparison.InvariantCultureIgnoreCase))
                AddResourcesFromSingleFiles(resourceList);
            return resourceList;
        }
        #endregion
        #region Načtení Resource.bin
        /// <summary>
        /// Do daného seznamu načte jednotlivé zdroje ze souboru ServerResources.bin
        /// </summary>
        /// <returns></returns>
        private static void AddResourcesFromResourcesBin(List<IResourceItem> resourceList)
        {
            string resourcePath = DxComponent.ApplicationPath;
            LoadFromResourcesBinInDirectory(resourcePath, 0, "", resourceList);               // Tady je umístěn soubor "ServerResources.bin" v uživatelském běhu klienta
            if (resourceList.Count == 0)
                LoadFromResourcesBinInDirectory(resourcePath, 3, "Noris\\Download", resourceList);   // Tady je umístěn v běhu TestDevExpress v Noris99\Noris_Support\TestDevExpress\bin\ : o 3 nahoru = "Noris99", a v něm "Noris\Download"
            if (resourceList.Count == 0)
                LoadFromResourcesBinInDirectory(resourcePath, 3, "Download", resourceList);   // Tady je umístěn v běhu klienta na vývojovém serveru = adresář aplikačního serveru Noris/Download
        }
        /// <summary>
        /// Načte obsah souboru "ServerResources.bin" z určeného adresáře (počet úrovní nahoru nahoru od aktuálního, a daný subdir)
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="upDirs"></param>
        /// <param name="subDir"></param>
        /// <param name="resourceList"></param>
        private static void LoadFromResourcesBinInDirectory(string resourcePath, int upDirs, string subDir, List<IResourceItem> resourceList)
        {
            string path = resourcePath;
            for (int i = 0; i < upDirs && !String.IsNullOrEmpty(path); i++)
                path = System.IO.Path.GetDirectoryName(path);
            if (String.IsNullOrEmpty(path)) return;
            if (!String.IsNullOrEmpty(subDir)) path = System.IO.Path.Combine(path, subDir);
            string fileName = System.IO.Path.Combine(path, "ServerResources.bin");
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(fileName);
            if (!fileInfo.Exists) return;
            int fileLengthMB = (int)(fileInfo.Length / 1000000L);
            if (fileLengthMB > 150) return;

            try
            {
                var startTime = DxComponent.LogTimeCurrent;
                byte[] content = System.IO.File.ReadAllBytes(fileInfo.FullName);
                LoadFromResourcesBinInArray(content, resourceList);
                var microsecs = DxComponent.LogGetTimeElapsed(startTime, DxComponent.LogTokenTimeMicrosec);
            }
            catch (Exception exc)
            {
                DxComponent.ShowMessageError($"Error on loading resources from file {fileInfo.FullName}: {exc.Message}", "Error");
            }
        }
        /// <summary>
        /// Načte zdroje z dodaného binárního pole ve formátu "ServerResources.bin".
        /// Je zajištěno, že velikost souboru je rozumná (do 150MB)
        /// </summary>
        /// <param name="content"></param>
        /// <param name="resourceList"></param>
        private static void LoadFromResourcesBinInArray(byte[] content, List<IResourceItem> resourceList)
        {
            StringBuilder sb = new StringBuilder();

            string validSignature2 = "ASOL$ApplicationServerResources$2.0";
            int signature2Length = validSignature2.Length;
            int signatureLength = signature2Length;
            string signature = ReadContentString(content, 0, signatureLength, sb);
            bool isValidSignature = (signature.Substring(0, signature2Length) == validSignature2);
            if (!isValidSignature)
                throw new FormatException($"Bad signature of ServerResource.bin file: '{signature}'");

            var length = content.Length;
            int headerEnd = length - 8;
            var headerBegin = ReadContentInt(content, headerEnd);
            var position = headerBegin;
            while (position < headerEnd)
            {
                // Načteme data z headeru:
                long fileBegin = ReadContentLong(content, ref position);
                int fileLength = ReadContentInt(content, ref position);
                int fileNameLength = ReadContentInt(content, ref position);
                string fileName = ReadContentString(content, ref position, fileNameLength, sb);
                if (fileLength >= 0 && fileBegin >= signatureLength && (fileBegin + fileLength) <= headerBegin)
                {
                    string itemKey = GetItemKey(fileName);
                    string packKey = GetPackKey(fileName, out var sizeType, out var contentType);
                    resourceList.Add(new DataResourceItem(content, fileBegin, fileLength, itemKey, packKey, sizeType, contentType));
                }
                else
                {
                    throw new FormatException($"Bad format of ServerResource.bin file: FileBegin ({fileBegin}) or FileLength ({fileLength}) is outside data area ({signatureLength}-{headerBegin}).");
                }
            }
        }
        /// <summary>
        /// Načte Int32 z dané pozice bufferu
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private static int ReadContentInt(byte[] content, int position)
        {
            return ReadContentInt(content, ref position);
        }
        /// <summary>
        /// Načte Int32 z dané pozice bufferu, pozici posune
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private static int ReadContentInt(byte[] content, ref int position)
        {
            return content[position++] | (content[position++] << 8) | (content[position++] << 16) | (content[position++] << 24);
        }
        /// <summary>
        /// Načte Int64 z dané pozice bufferu
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private static long ReadContentLong(byte[] content, int position)
        {
            return ReadContentLong(content, ref position);
        }
        /// <summary>
        /// Načte Int64 z dané pozice bufferu, pozici posune
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private static long ReadContentLong(byte[] content, ref int position)
        {
            long l = ReadContentInt(content, ref position);
            long h = ReadContentInt(content, ref position);
            return l | (h << 32);
        }
        /// <summary>
        /// Načte String z dané pozice bufferu, načítá pouze ASCII znaky (0-255)
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <param name="length"></param>
        /// <param name="sb"></param>
        /// <returns></returns>
        private static string ReadContentString(byte[] content, int position, int length, StringBuilder sb = null)
        {
            return ReadContentString(content, ref position, length, sb ?? new StringBuilder());
        }
        /// <summary>
        /// Načte String z dané pozice bufferu, načítá pouze ASCII znaky (0-255), pozici posune
        /// </summary>
        /// <param name="content"></param>
        /// <param name="position"></param>
        /// <param name="length"></param>
        /// <param name="sb"></param>
        /// <returns></returns>
        private static string ReadContentString(byte[] content, ref int position, int length, StringBuilder sb)
        {
            sb.Clear();
            for (int i = 0; i < length; i++)
                sb.Append((char)(content[position++]));
            return sb.ToString();
        }
        #endregion
        #region Načtení zdrojů z jednotlivých souborů disku
        /// <summary>
        /// Načte a vrátí zdroje z daného adresáře
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static IEnumerable<IResourceItem> GetResourcesFromDirectory(string directory)
        {
            List<IResourceItem> resourceList = new List<IResourceItem>();
            if (!String.IsNullOrEmpty(directory))
            {
                var dirInfo = new System.IO.DirectoryInfo(directory);
                if (dirInfo.Exists)
                {
                    int commonPathLength = dirInfo.Parent.FullName.Length;
                    LoadFromFilesInDirectory(dirInfo, commonPathLength, resourceList);
                }
            }
            return resourceList;
        }
        /// <summary>
        /// Do daného seznamu načte jednotlivé zdroje z podadresářů aplikace.
        /// </summary>
        /// <returns></returns>
        private static void AddResourcesFromSingleFiles(List<IResourceItem> resourceList)
        {
            string resourcePath = DxComponent.ApplicationPath;
            LoadFromFilesInDirectory(resourcePath, 0, "Resources", resourceList);
            LoadFromFilesInDirectory(resourcePath, 1, "Images", resourceList);
            LoadFromFilesInDirectory(resourcePath, 1, "pic", resourceList);
            LoadFromFilesInDirectory(resourcePath, 1, "pic-0", resourceList);
        }
        /// <summary>
        /// Zkusí najít zdroje v jednom adresáři
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="upDirs"></param>
        /// <param name="subDir"></param>
        /// <param name="resourceList"></param>
        private static void LoadFromFilesInDirectory(string resourcePath, int upDirs, string subDir, List<IResourceItem> resourceList)
        {
            string path = resourcePath;
            for (int i = 0; i < upDirs && !String.IsNullOrEmpty(path); i++)
                path = System.IO.Path.GetDirectoryName(path);
            if (String.IsNullOrEmpty(path)) return;
            int commonPathLength = path.Length;
            if (!String.IsNullOrEmpty(subDir)) path = System.IO.Path.Combine(path, subDir);
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(path);
            if (!dirInfo.Exists) return;

            LoadFromFilesInDirectory(dirInfo, commonPathLength, resourceList);
        }
        /// <summary>
        /// Načte zdroje z daného adresáře
        /// </summary>
        /// <param name="dirInfo"></param>
        /// <param name="commonPathLength"></param>
        /// <param name="resourceList"></param>
        private static void LoadFromFilesInDirectory(System.IO.DirectoryInfo dirInfo, int commonPathLength, List<IResourceItem> resourceList)
        { 
            foreach (var fileInfo in dirInfo.EnumerateFiles("*.*", System.IO.SearchOption.AllDirectories))
            {
                if (fileInfo == null || !fileInfo.Exists) continue;
                string fullName = fileInfo.FullName;
                if (fullName.Length <= commonPathLength) continue;
                string relativeName = fullName.Substring(commonPathLength);
                string itemKey = GetItemKey(relativeName);
                string packKey = GetPackKey(itemKey, out ResourceImageSizeType sizeType, out ResourceContentType contentType);
                if (contentType == ResourceContentType.None) continue;

                resourceList.Add(new DataResourceItem(fullName, itemKey, packKey, sizeType, contentType));
            }
        }
        #endregion
        #region Konverze, detekce velikosti a typu
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
            if (!String.IsNullOrEmpty(packKey) && !packKey.Contains("«"))
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
            if (sizeType != ResourceImageSizeType.None)
                name = name.Substring(0, name.Length - suffix.Length);
            return (sizeType != ResourceImageSizeType.None);
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
            contentType = DxComponent.GetContentTypeFromExtension(extension);
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
        /// Konstruktor
        /// </summary>
        /// <param name="content"></param>
        /// <param name="fileBegin"></param>
        /// <param name="fileLength"></param>
        /// <param name="itemKey"></param>
        /// <param name="packKey"></param>
        /// <param name="sizeType"></param>
        /// <param name="contentType"></param>
        public DataResourceItem(byte[] content, long fileBegin, int fileLength, string itemKey, string packKey, ResourceImageSizeType sizeType, ResourceContentType contentType)
        {
            FileName = null;
            ItemKey = itemKey;
            PackKey = packKey;
            SizeType = sizeType;
            ContentType = contentType;
            _Content = new byte[fileLength];
            if (fileLength > 0)
                Array.Copy(content, fileBegin, _Content, 0, fileLength);
            _ContentLoaded = true;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ItemKey: {ItemKey}; Size: {SizeType}; Content: {ContentType}";
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
                if (!String.IsNullOrEmpty(this.FileName) && System.IO.File.Exists(this.FileName))
                {
                    try { _Content = System.IO.File.ReadAllBytes(this.FileName); }
                    catch { }
                }
            }
            return _Content;
        }
        private byte[] _Content;
        private bool _ContentLoaded;
    }
    #endregion
}
