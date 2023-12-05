// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.DataProcessing.ExtractStorage;
using DevExpress.Diagram.Core.Layout.Native;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraRichEdit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SysIO = System.IO;
using WinDraw = System.Drawing;
using WinForm = System.Windows.Forms;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    /// <summary>
    /// <see cref="DxRepositoryManager"/> : uchovává v sobě fyzické editorové prvky, typicky z namespace <see cref="DevExpress.XtraEditors"/>.
    /// Na vyžádání je poskytuje volajícímu, který si je umisťuje do panelu <see cref="DxDataFormContentPanel"/>, a to když je vyžadována interakce s uživatelem:
    /// MouseOn anebo Keyboard editační akce.
    /// Po odchodu myši nebo focusu z editačního prvku je tento prvek vykreslen do bitmapy a obsah bitmapy je uložen do interaktivního prvku <see cref="Data.DataFormCell"/>,
    /// odkud je pak průběžně vykreslován do grafického panelu <see cref="DxDataFormContentPanel"/> (řídí <see cref="Data.DataFormCell"/>).
    /// </summary>
    public class DxRepositoryManager : IDisposable
    {
        #region Konstruktor a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataFormPanel"></param>
        public DxRepositoryManager(DxDataFormPanel dataFormPanel)
        {
            __DataFormPanel = dataFormPanel;
            _InitRepository();
            _InitCache();
        }
        void IDisposable.Dispose()
        {
            _DisposeRepository();
            _DisposeCache();
        }
        /// <summary>
        /// Panel dataformu
        /// </summary>
        private DxDataFormPanel _DataFormPanel { get { return __DataFormPanel; } } private DxDataFormPanel __DataFormPanel;
        #endregion
        #region Public hodnoty a služby
        /// <summary>
        /// Formát bitmap, který se ukládá do cache. Čte se z DataFormu: <see cref="DxDataFormPanel.CacheImageFormat"/>
        /// </summary>
        public WinDraw.Imaging.ImageFormat CacheImageFormat { get { return _DataFormPanel.CacheImageFormat; } }
        /// <summary>
        /// Invaliduje data v repozitory.
        /// Volá se po změně skinu a zoomu, protože poté je třeba nově vygenerovat.
        /// Po invalidaci bude navazující kreslení trvat o kousek déle, protože všechny vykreslovací prvky se budou znovu OnDemand generovat, a bude se postupně plnit cache.
        /// Ale o žádná dat se nepřijde, tady nejsou data jen nářadí...
        /// </summary>
        public void InvalidateManager()
        {
            _InvalidateRepozitory();
            _InvalidateCache();
        }
        #endregion
        #region Vykreslení prvku grafické, aktivace/deaktivace prvku nativního
        /// <summary>
        /// Je požadováno vykreslení buňky do dodané grafiky
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="pdea"></param>
        /// <param name="controlBounds"></param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        /// <returns></returns>
        public void PaintItem(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds, bool isDisplayed)
        {
            if (paintItem is null) return;
            if (_TryGetEditor(paintItem.EditorType, out var editor))
                editor.PaintItem(paintItem, pdea, controlBounds, isDisplayed);
        }
        /// <summary>
        /// Je voláno po změně interaktivního stavu dané buňky. Možná bude třeba pro buňku vytvořit a umístit nativní Control, anebo bude možno jej odebrat...
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="controlBounds"></param>
        public void ChangeItemInteractiveState(IPaintItemData paintItem, WinDraw.Rectangle controlBounds)
        {
            if (paintItem is null) return;
            if (_TryGetEditor(paintItem.EditorType, out var editor))
                editor.ChangeItemInteractiveState(paintItem, controlBounds);
        }
        #endregion
        #region Repository použitých editorů
        /// <summary>
        /// Inicializace repozitory editorů.
        /// </summary>
        private void _InitRepository()
        {
            __RepositoryDict = new Dictionary<DxRepositoryEditorType, DxRepositoryEditor>();
        }
        /// <summary>
        /// Invaliduje data v repozitory.
        /// Volá se po změně skinu a zoomu, protože poté je třeba nově vygenerovat 
        /// </summary>
        private void _InvalidateRepozitory()
        {
            foreach (var editor in __RepositoryDict.Values)
                ((IDisposable)editor).Dispose();
            __RepositoryDict.Clear();
        }
        /// <summary>
        /// Metoda najde / vytvoří a vrátí editor pro daný typ prvku.
        /// </summary>
        /// <param name="editorType"></param>
        /// <param name="editor"></param>
        /// <returns></returns>
        private bool _TryGetEditor(DxRepositoryEditorType editorType, out DxRepositoryEditor editor)
        {
            if (editorType == DxRepositoryEditorType.None) { editor = null; return false; }

            if (!__RepositoryDict.TryGetValue(editorType, out editor))
            {
                editor = DxRepositoryEditor.CreateEditor(this, editorType);
                __RepositoryDict.Add(editorType, editor);
            }
            return true;
        }
        /// <summary>
        /// Uvolní svoje data z repozitory
        /// </summary>
        private void _DisposeRepository()
        {
            _InvalidateRepozitory();
            __RepositoryDict = null;
        }
        /// <summary>
        /// Úložiště prvků editorů
        /// </summary>
        private Dictionary<DxRepositoryEditorType, DxRepositoryEditor> __RepositoryDict;

        #endregion
        #region Cache obrázků
        /// <summary>
        /// Pokud je dán klíč, pro který máme v evidenci data (uložená bitmapa ve formě byte[]), pak ji najde a vrátí true.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="imageData"></param>
        /// <returns></returns>
        public bool TryGetCacheImageData(string key, out byte[] imageData)
        {
            return TryGetCacheImageData(key, out imageData, out ulong _);
        }
        /// <summary>
        /// Pokud je dán klíč, pro který máme v evidenci data (uložená bitmapa ve formě byte[]), pak ji najde a vrátí true. Získá rovněž ID dat.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="imageData"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool TryGetCacheImageData(string key, out byte[] imageData, out ulong id)
        {
            if (key != null && __CacheDict.TryGetValue(key, out imageData, out id)) return true;

            imageData = null;
            id = 0UL;
            return false;
        }
        /// <summary>
        /// Pokud je dán ID, pro který máme v evidenci data (uložená bitmapa ve formě byte[]), pak ji najde a vrátí true.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="imageData"></param>
        /// <returns></returns>
        public bool TryGetCacheImageData(ulong id, out byte[] imageData)
        {
            if (id > 0UL && __CacheDict.TryGetValue(id, out imageData)) return true;

            imageData = null;
            return false;
        }
        /// <summary>
        /// Do cache přidá data (uložená bitmapa ve formě byte[]) pro daný klíč. Pokud klíč nebo data jsou null, nepřidá nic.
        /// Pokud pro daný klíč už data evidujeme, nepřidá ani nepřepíše je.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="imageData"></param>
        public ulong AddImageDataToCache(string key, byte[] imageData)
        {
            if (key is null || imageData is null) return 0UL;
            _CleanUpCache();
            return __CacheDict.Store(key, imageData);
        }
        /// <summary>
        /// Inicializace cache hotových obrázků
        /// </summary>
        private void _InitCache()
        {
            __CacheDict = new BiDictionary<byte[]>();
        }
        /// <summary>
        /// Invaliduje data v cache hotových obrázků.
        /// Volá se po změně skinu a zoomu, protože poté je třeba nově vygenerovat 
        /// </summary>
        private void _InvalidateCache()
        {
            __CacheDict.Clear();
            __CacheCleanUpLastTime = null;
        }
        /// <summary>
        /// Provede úklid cache, pokud je to vhodné
        /// </summary>
        private void _CleanUpCache()
        {
            if (!__CacheCleanUpLastTime.HasValue) __CacheCleanUpLastTime = DateTime.UtcNow;

            var timeSpan = DateTime.UtcNow - __CacheCleanUpLastTime.Value;
            int count = __CacheDict.Count;
            if (count < _CacheCleanUpMinCount) return;                                             // Počet prvků je maličký
            bool isMaxCount = (count >= _CacheCleanUpMaxCount);                                    // true pokud počet prvků v cache přesahuje MaxCount
            if (timeSpan.TotalMilliseconds < _CacheCleanUpIntervalMs && !isMaxCount) return;       // Čas od posledního úklidu je malý, a počet prvků nepřesahuje MaxCount

            // Určím velikost dat:
            bool isCleanUp = isMaxCount;
            int totalBytes = -1;
            if (!isCleanUp)
            {   // Není překročen MaxCount, ale už je čas na kontrolu => spočítám velikost dat:
                totalBytes = getCacheSize();
                isCleanUp = totalBytes >= _CacheCleanUpMaxSize;
            }

            if (isCleanUp)
            {   // Provedeme úklid:
                var startTime = DxComponent.LogTimeCurrent;

                if (totalBytes < 0 && DxComponent.LogActive)
                    totalBytes = getCacheSize();

                //  Z cache něco smažeme:
                // Vytvořím List, kde Key = klíč záznamu, a Value = velikost záznamu v Byte:
                var sizes = __CacheDict.KeyValues.Select(kvp => new KeyValuePair<string, int>(kvp.Key, getRecSize(kvp.Key, kvp.Value))).ToList();
                // Setřídím podle velikosti záznamu vzestupně:
                sizes.Sort((a, b) => a.Value.CompareTo(b.Value));
                // Ponechám si '_CacheCleanUpTargetCount' nejmenších záznamů, a ostatní odeberu:
                for (int i = _CacheCleanUpTargetCount; i < count; i++)
                    __CacheDict.Remove(sizes[i].Key);

                if (DxComponent.LogActive)
                {
                    int removeCount = 0;
                    int removeSize = 0;
                    for (int i = _CacheCleanUpTargetCount; i < count; i++)
                    {
                        removeCount++;
                        removeSize += sizes[i].Value;
                    }
                    DxComponent.LogAddLineTime($"DxRepositoryManager.CleanUpCache() From: {count} [{totalBytes} B]; Removed: {removeCount} [{removeSize} B]; Time: {DxComponent.LogTokenTimeMicrosec}", startTime);
                }
            }

            // Čas poslední kontroly / úklidu:
            __CacheCleanUpLastTime = DateTime.UtcNow;


            // Vrátí velikost dat celé cache
            int getCacheSize()
            {
                return __CacheDict.Count * 16 + __CacheDict.KeyValues.Select(kvp => getRecSize(kvp.Key, kvp.Value)).Sum();
            }
            // Vrátí velikost dat daného záznamu cache
            int getRecSize(string key, byte[] value)
            {
                return (2 * key.Length + value.Length);
            }
        }
        /// <summary>
        /// Počet prvků v cache, od kterého provádíme kontroly (pokud je méně, tak kontrolu neprovádíme)
        /// </summary>
        private const int _CacheCleanUpMinCount = 4000;
        /// <summary>
        /// Cílový počet prvků v cache po úklidu
        /// </summary>
        private const int _CacheCleanUpTargetCount = 2000;
        /// <summary>
        /// Cílová velikost cache po úklidu
        /// </summary>
        private const int _CacheCleanUpTargetSize = 2000000;
        /// <summary>
        /// Počet milisekund mezi dvěma úklidy cache; dřív nebudu ani testovat velikost, pouze 
        /// </summary>
        private const double _CacheCleanUpIntervalMs = 5000d;
        /// <summary>
        /// Maximální počet prvků v cache; jakmile aktuální počet překročí tuto mez, pak se provede úklid i dříve než v intervalu <see cref="_CacheCleanUpIntervalMs"/>
        /// </summary>
        private const int _CacheCleanUpMaxCount = 16000;
        /// <summary>
        /// Maximální velikost dat v cache; kontroluje se v intervalu <see cref="_CacheCleanUpIntervalMs"/>; jakmile je překročena, provede se úklid
        /// </summary>
        private const int _CacheCleanUpMaxSize = 16000000;
        /// <summary>
        /// Čas posledního úklidu cache
        /// </summary>
        private DateTime? __CacheCleanUpLastTime;
        /// <summary>
        /// Uvolní svoje data z cache hotových obrázků
        /// </summary>
        private void _DisposeCache()
        {
            _InvalidateCache();
            __CacheDict = null;
        }
        /// <summary>
        /// Úložiště hotových obrázků.
        /// Klíčem může být String = jednoznačný klíč definující data, anebo průběžně přidělený UInt64 = ID.
        /// </summary>
        private BiDictionary<byte[]> __CacheDict;
        #endregion
    }
    #region interface IPaintItemData : přepis pro prvek, který může být vykreslen
    /// <summary>
    /// <see cref="IPaintItemData"/> : přepis pro prvek, který může být vykreslen v metodě <see cref="DxRepositoryManager.PaintItem(IPaintItemData, PaintDataEventArgs, WinDraw.Rectangle, bool)"/>
    /// </summary>
    public interface IPaintItemData
    {
        /// <summary>
        /// Druh vstupního prvku
        /// </summary>
        DxRepositoryEditorType EditorType { get; }
        /// <summary>
        /// Interaktivní stav
        /// </summary>
        DxInteractiveState InteractiveState { get; }
        /// <summary>
        /// Nativní control, zobrazující zdejší data
        /// </summary>
        WinForm.Control NativeControl { get; set; }
        /// <summary>
        /// Zkusí najít hodnotu daného jména.
        /// Dané jméno nesmí obsahovat jméno sloupce.
        /// Hodnota se prioritně hledá v řádku (=specifická pro konkrétní řádek), a pokud tam není, pak se hledá v layoutu (defaultní hodnota).
        /// Jména hodnot jsou v konstantách v <see cref="Data.DxDataFormDef"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">Jméno vlastnosti. Nesmí obsahovat jméno sloupce, to bude přidáno.</param>
        /// <param name="content">Out hodnota</param>
        /// <returns></returns>
        public bool TryGetContent<T>(string name, out T content);
        /// <summary>
        /// Invaliduje si svoje cachovaná grafická data. Volá se po změnách jak dat, tak stylu.
        /// </summary>
        void InvalidateCache();
        /// <summary>
        /// ID, který vede zrychleně k datům v cache.
        /// Jakákoli změna hodnot v prvku sem nastaví null.
        /// </summary>
        ulong? ImageId { get; set; }
        /// <summary>
        /// Uložená data obrázku
        /// </summary>
        byte[] ImageData { get; set; }
    }
    #endregion
    #region Enumy
    /// <summary>
    /// Druh prvku
    /// </summary>
    public enum DxRepositoryEditorType
    {
        /// <summary>
        /// Žádný prvek
        /// </summary>
        None,
        /// <summary>
        /// Label
        /// </summary>
        Label,
        /// <summary>
        /// Titulkový řádek s možností vodorovné čáry a/nebo textu
        /// </summary>
        Title,
        /// <summary>
        /// TextBox - prostý bez buttonů (buttony má <see cref="TextBoxButton"/>), podporuje password i nullvalue
        /// </summary>
        TextBox,
        /// <summary>
        /// EditBox (Memo, Poznámka)
        /// </summary>
        EditBox,
        /// <summary>
        /// TextBox s buttony = pokrývá i Relation, Document, FileBox, CalendarBox a další textbox s přidanými tlačítky
        /// </summary>
        TextBoxButton,
        /// <summary>
        /// CheckBox: zaškrtávátko i DownButton
        /// </summary>
        CheckBox,
        /// <summary>
        /// Jeden prvek ze sady RadioButtonů
        /// </summary>
        RadioButton,
        /// <summary>
        /// Klasické tlačítko
        /// </summary>
        Button,
        /// <summary>
        /// Button s přidaným rozbalovacím menu
        /// </summary>
        DropDownButton,
        /// <summary>
        /// Combobox obou typů (List i Edit)
        /// </summary>
        ComboListBox,
        /// <summary>
        /// Posouvací hodnota, jedna nebo dvě
        /// </summary>
        TrackBar,
        /// <summary>
        /// Image
        /// </summary>
        Image,
        /// <summary>
        /// Malá tabulka
        /// </summary>
        Grid,
        /// <summary>
        /// Strom s prvky
        /// </summary>
        Tree,
        /// <summary>
        /// HTML prohlížeč
        /// </summary>
        HtmlContent,
        /// <summary>
        /// Container typu Panel, s možností titulku
        /// </summary>
        Panel,
        /// <summary>
        /// Container typu Sada stránek, jeho Child prvky jsou pouze Page
        /// </summary>
        PageSet,
        /// <summary>
        /// Container typu Stránka, je umístěn v <see cref="PageSet"/> a může obsahovat cokoliv
        /// </summary>
        Page
        // ... rozšíříme na požádání.
    }
    #endregion
    #region DxRepositoryEditor**** : Sada tříd, které fyzicky řídí tvorbu, plnění daty, vykreslení atd pro jednotlivé druhy DxRepositoryEditorType
    #region Label
    /// <summary>
    /// Label
    /// </summary>
    internal class DxRepositoryEditorLabel : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        public DxRepositoryEditorLabel(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        public override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.Label; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.DirectPaint; } }
        /// <summary>
        /// Je voláno po změně interaktivního stavu dané buňky. Možná bude třeba pro buňku vytvořit a umístit nativní Control, anebo bude možno jej odebrat...
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="controlBounds"></param>
        public override void ChangeItemInteractiveState(IPaintItemData paintItem, WinDraw.Rectangle controlBounds)
        {
        
        }
        /// <summary>
        /// Vykreslí prvek do dané grafiky
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        public override void PaintItem(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds, bool isDisplayed)
        {
            PaintImageData(paintItem, pdea, controlBounds);
        }
        /// <summary>
        /// Potomek zde přímo do cílové grafiky vykreslí obsah prvku, bez cache
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="pdea"></param>
        /// <param name="controlBounds"></param>
        protected override void PaintImageDirect(IPaintItemData paintItem, PaintDataEventArgs pdea, Rectangle controlBounds)
        {
            string text = GetItemLabel(paintItem);
            if (!String.IsNullOrEmpty(text))
            {
                var font = DxComponent.GetFontDefault(targetDpi: pdea.InteractivePanel.CurrentDpi);
                var color = DxComponent.GetSkinColor(SkinElementColor.CommonSkins_ControlText) ?? WinDraw.Color.Violet;
                DxComponent.DrawText(pdea.Graphics, text, font, controlBounds, color, WinDraw.ContentAlignment.MiddleLeft);
            }
        }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintItem, Rectangle controlBounds)
        {
            string text = GetItemLabel(paintItem);
            WinDraw.FontStyle fontStyle = GetItemContent(paintItem, Data.DxDataFormDef.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintItem, Data.DxDataFormDef.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintItem, Data.DxDataFormDef.TextColor, WinDraw.Color.Empty);

            return CreateKey(text, controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor));
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            // Některé controly (a DevExpress.XtraEditors.LabelControl mezi nimi) mají tu nectnost, že první vykreslení bitmapy v jejich životě je chybné.
            //  Nejde o první vykreslení do jedné Graphics, jde o první vykreslení po konstruktoru.
            //  Projevuje se to tak, že do Bitmapy vykreslí pozadí zcela černé v celé ploše mimo text Labelu!
            // Proto detekujeme první použití (kdy na začátku je __EditorPaint = null), a v tom případě vygenerujeme tu úplně první bitmapu naprázdno.
            bool isFirstUse = (__EditorPaint is null);

            __EditorPaint ??= _CreateControl(pdea);
            PrepareControl(__EditorPaint, paintItem, pdea, controlBounds);

            // První vygenerovanou bitmapu v životě Labelu vytvoříme a zahodíme, není pěkná...
            if (isFirstUse) CreateBitmapData(__EditorPaint);

            // Teprve ne-první bitmapy jsou OK:
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="paintItem"></param>
        /// <param name="pdea"></param>
        /// <param name="controlBounds"></param>
        protected void PrepareControl(DevExpress.XtraEditors.LabelControl control, IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            int w = controlBounds.Width;
            int h = controlBounds.Height;
            control.Text = GetItemLabel(paintItem);
            control.Size = controlBounds.Size;
            // a řada dalších...
        }
        /// <summary>
        /// Dispose objektu
        /// </summary>
        protected override void OnDispose()
        {
            if (__EditorPaint != null)
            {
                __EditorPaint.Dispose();
                __EditorPaint = null;
            }
        }
        DevExpress.XtraEditors.LabelControl __EditorPaint;
        private DevExpress.XtraEditors.LabelControl _CreateControl(PaintDataEventArgs pdea)
        {
            var control = new DevExpress.XtraEditors.LabelControl() { Location = new WinDraw.Point(25, -200) };
            control.ResetBackColor();
            control.LineStyle = WinDraw.Drawing2D.DashStyle.Solid;
            control.LineVisible = true;
            control.LineLocation = DevExpress.XtraEditors.LineLocation.Bottom;
            control.LineColor = WinDraw.Color.DarkBlue;
            control.LineOrientation = DevExpress.XtraEditors.LabelLineOrientation.Horizontal;
            control.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Office2003;
            // pdea.InteractivePanel.Controls.Add(control);
            return control;
        }
    }
    #endregion
    #region TextBox
    /// <summary>
    /// TextBox
    /// </summary>
    internal class DxRepositoryEditorTextBox : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        public DxRepositoryEditorTextBox(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        public override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.TextBox; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Je voláno po změně interaktivního stavu dané buňky. Možná bude třeba pro buňku vytvořit a umístit nativní Control, anebo bude možno jej odebrat...
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="controlBounds"></param>
        public override void ChangeItemInteractiveState(IPaintItemData paintItem, WinDraw.Rectangle controlBounds)
        {

        }
        /// <summary>
        /// Vykreslí prvek do dané grafiky
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        public override void PaintItem(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds, bool isDisplayed)
        {
            PaintImageData(paintItem, pdea, controlBounds);
        }
        /// <summary>
        /// Potomek zde přímo do cílové grafiky vykreslí obsah prvku, bez cache
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        protected override void PaintImageDirect(IPaintItemData paintItem, PaintDataEventArgs pdea, Rectangle controlBounds) { }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintItem, Rectangle controlBounds)
        {
            object value = GetItemValue(paintItem);
            WinDraw.FontStyle fontStyle = GetItemContent(paintItem, Data.DxDataFormDef.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintItem, Data.DxDataFormDef.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintItem, Data.DxDataFormDef.TextColor, WinDraw.Color.Empty);

            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor));
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= new DevExpress.XtraEditors.TextEdit();
            PrepareControl(__EditorPaint, paintItem, pdea, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="control">Cílový control</param>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        protected void PrepareControl(DevExpress.XtraEditors.TextEdit control, IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            control.EditValue = GetItemValue(paintItem);
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintItem);
            // control.ErrorText = "ErrorText ...";
            // control.ErrorImageOptions.Alignment = WinForm.ErrorIconAlignment.TopRight;
            // control.ErrorImageOptions.
        }
        /// <summary>
        /// Dispose objektu
        /// </summary>
        protected override void OnDispose()
        {
            if (__EditorPaint != null)
            {
                __EditorPaint.Dispose();
                __EditorPaint = null;
            }
        }
        /// <summary>
        /// Nativní control používaný pro vykreslení obrázku do bitmapy
        /// </summary>
        DevExpress.XtraEditors.TextEdit __EditorPaint;
    }
    #endregion
    #region TextBoxButton
    /// <summary>
    /// TextBoxButton
    /// </summary>
    internal class DxRepositoryEditorTextBoxButton : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        public DxRepositoryEditorTextBoxButton(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        public override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.TextBoxButton; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Je voláno po změně interaktivního stavu dané buňky. Možná bude třeba pro buňku vytvořit a umístit nativní Control, anebo bude možno jej odebrat...
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="controlBounds"></param>
        public override void ChangeItemInteractiveState(IPaintItemData paintItem, WinDraw.Rectangle controlBounds) { }
        /// <summary>
        /// Vykreslí prvek do dané grafiky
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        public override void PaintItem(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds, bool isDisplayed)
        {
            PaintImageData(paintItem, pdea, controlBounds);
        }
        /// <summary>
        /// Potomek zde přímo do cílové grafiky vykreslí obsah prvku, bez cache
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="pdea"></param>
        /// <param name="controlBounds"></param>
        protected override void PaintImageDirect(IPaintItemData paintItem, PaintDataEventArgs pdea, Rectangle controlBounds) { }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintItem, Rectangle controlBounds)
        {
            object value = GetItemValue(paintItem);
            WinDraw.FontStyle fontStyle = GetItemContent(paintItem, Data.DxDataFormDef.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintItem, Data.DxDataFormDef.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintItem, Data.DxDataFormDef.TextColor, WinDraw.Color.Empty);
            string buttons = GetItemContent(paintItem, Data.DxDataFormDef.TextBoxButtons, "");
            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor), buttons);
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= new DevExpress.XtraEditors.ButtonEdit();
            PrepareControl(__EditorPaint, paintItem, pdea, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="paintItem"></param>
        /// <param name="pdea"></param>
        /// <param name="controlBounds"></param>
        protected void PrepareControl(DevExpress.XtraEditors.ButtonEdit control, IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {

            object value = GetItemValue(paintItem);

            control.EditValue = value;
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintItem);
            control.Properties.Buttons.Clear();
            var buttons = this.GetItemContent(paintItem, Data.DxDataFormDef.TextBoxButtons, "");
            if (!String.IsNullOrEmpty(buttons))
            {
                control.Properties.ButtonsStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
                var buttonNames = buttons.Split(',', ';');
                foreach (var buttonName in buttonNames )
                {
                    if (Enum.TryParse< DevExpress.XtraEditors.Controls.ButtonPredefines>(buttonName, out DevExpress.XtraEditors.Controls.ButtonPredefines kind))
                        control.Properties.Buttons.Add(new DevExpress.XtraEditors.Controls.EditorButton(kind));
                }
            }
        }
        /// <summary>
        /// Dispose objektu
        /// </summary>
        protected override void OnDispose()
        {
            if (__EditorPaint != null)
            {
                __EditorPaint.Dispose();
                __EditorPaint = null;
            }
        }
        /// <summary>
        /// Nativní control používaný pro vykreslení obrázku do bitmapy
        /// </summary>
        DevExpress.XtraEditors.ButtonEdit __EditorPaint;
    }
    #endregion
    #region EditBox
    /// <summary>
    /// EditBox
    /// </summary>
    internal class DxRepositoryEditorEditBox : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        public DxRepositoryEditorEditBox(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        public override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.TextBox; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Je voláno po změně interaktivního stavu dané buňky. Možná bude třeba pro buňku vytvořit a umístit nativní Control, anebo bude možno jej odebrat...
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="controlBounds"></param>
        public override void ChangeItemInteractiveState(IPaintItemData paintItem, WinDraw.Rectangle controlBounds) { }
        /// <summary>
        /// Vykreslí prvek do dané grafiky
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        public override void PaintItem(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds, bool isDisplayed)
        {
            PaintImageData(paintItem, pdea, controlBounds);
        }
        /// <summary>
        /// Potomek zde přímo do cílové grafiky vykreslí obsah prvku, bez cache
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        protected override void PaintImageDirect(IPaintItemData paintItem, PaintDataEventArgs pdea, Rectangle controlBounds) { }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintItem, Rectangle controlBounds)
        {
            object value = GetItemValue(paintItem);
            WinDraw.FontStyle fontStyle = GetItemContent(paintItem, Data.DxDataFormDef.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintItem, Data.DxDataFormDef.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintItem, Data.DxDataFormDef.TextColor, WinDraw.Color.Empty);

            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor));
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= new DevExpress.XtraEditors.MemoEdit();
            PrepareControl(__EditorPaint, paintItem, pdea, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="control">Cílový control</param>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        protected void PrepareControl(DevExpress.XtraEditors.MemoEdit control, IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            control.EditValue = GetItemValue(paintItem);
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintItem);
        }
        /// <summary>
        /// Dispose objektu
        /// </summary>
        protected override void OnDispose()
        {
            if (__EditorPaint != null)
            {
                __EditorPaint.Dispose();
                __EditorPaint = null;
            }
        }
        /// <summary>
        /// Nativní control používaný pro vykreslení obrázku do bitmapy
        /// </summary>
        DevExpress.XtraEditors.MemoEdit __EditorPaint;
    }
    #endregion
    
    // checkboxy, spinnery, range, atd atd atd
    // containery

    #region Button
    /// <summary>
    /// Button
    /// </summary>
    internal class DxRepositoryEditorButton : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        public DxRepositoryEditorButton(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        public override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.Button; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Je voláno po změně interaktivního stavu dané buňky. Možná bude třeba pro buňku vytvořit a umístit nativní Control, anebo bude možno jej odebrat...
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="controlBounds"></param>
        public override void ChangeItemInteractiveState(IPaintItemData paintItem, WinDraw.Rectangle controlBounds)
        {

        }
        /// <summary>
        /// Vykreslí prvek do dané grafiky
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        public override void PaintItem(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds, bool isDisplayed)
        {
            PaintImageData(paintItem, pdea, controlBounds);
        }
        /// <summary>
        /// Potomek zde přímo do cílové grafiky vykreslí obsah prvku, bez cache
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        protected override void PaintImageDirect(IPaintItemData paintItem, PaintDataEventArgs pdea, Rectangle controlBounds) { }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintItem, Rectangle controlBounds)
        {
            string text = GetItemLabel(paintItem);
            var iconName = this.GetItemContent(paintItem, Data.DxDataFormDef.IconName, "");
            WinDraw.FontStyle fontStyle = GetItemContent(paintItem, Data.DxDataFormDef.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintItem, Data.DxDataFormDef.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintItem, Data.DxDataFormDef.TextColor, WinDraw.Color.Empty);

            return CreateKey(text, controlBounds.Size, iconName, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor));
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= new DevExpress.XtraEditors.SimpleButton();
            PrepareControl(__EditorPaint, paintItem, pdea, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="control">Cílový control</param>
        /// <param name="paintItem">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        protected void PrepareControl(DevExpress.XtraEditors.SimpleButton control, IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            control.Text = GetItemLabel(paintItem);
            control.Size = controlBounds.Size;

            var iconName = this.GetItemContent(paintItem, Data.DxDataFormDef.IconName, "");
            if (iconName != null)
            {
                DxComponent.ApplyImage(control.ImageOptions, iconName);
                control.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.LeftCenter;
            }
        }
        /// <summary>
        /// Dispose objektu
        /// </summary>
        protected override void OnDispose()
        {
            if (__EditorPaint != null)
            {
                __EditorPaint.Dispose();
                __EditorPaint = null;
            }
        }
        DevExpress.XtraEditors.SimpleButton __EditorPaint;
    }
    #endregion
    #region class DxRepositoryEditor : Obecný abtraktní předek konkrétních editorů; obsahuje Factory pro tvorbu konkrétních potomků
    /// <summary>
    /// Obecný abtraktní předek konkrétních editorů; obsahuje Factory pro tvorbu konkrétních potomků
    /// </summary>
    internal abstract class DxRepositoryEditor : IDisposable
    {
        #region Konstruktor, Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        public DxRepositoryEditor(DxRepositoryManager repositoryManager)
        {
            __RepositoryManager = repositoryManager;
        }
        /// <summary>
        /// Reference na <see cref="DxRepositoryManager"/>, do něhož tento editor patří
        /// </summary>
        protected DxRepositoryManager RepositoryManager { get { return __RepositoryManager; } } private DxRepositoryManager __RepositoryManager;
        /// <summary>
        /// Formát bitmap, který se ukládá do cache. Čte se z DataFormu: <see cref="DxDataFormPanel.CacheImageFormat"/>
        /// (přes <see cref="DxRepositoryManager.CacheImageFormat"/>)
        /// </summary>
        protected virtual WinDraw.Imaging.ImageFormat CacheImageFormat { get { return RepositoryManager.CacheImageFormat; } }
        /// <summary>
        /// Dispose
        /// </summary>
        void IDisposable.Dispose() { OnDispose(); }
        /// <summary>
        /// Potomek zde disposuje svoje prvky
        /// </summary>
        protected virtual void OnDispose() { }
        #endregion
        #region Factory metoda, která vytvoří a vrátí editor požadovaného typu = potomek zdejší třídy pro požadovaný typ
        /// <summary>
        /// Factory metoda, která vytvoří a vrátí new instanci editoru daného typu
        /// </summary>
        /// <param name="repositoryManager"></param>
        /// <param name="editorType"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static DxRepositoryEditor CreateEditor(DxRepositoryManager repositoryManager, DxRepositoryEditorType editorType)
        {
            switch (editorType)
            {
                case DxRepositoryEditorType.Label: return new DxRepositoryEditorLabel(repositoryManager);
                case DxRepositoryEditorType.TextBox: return new DxRepositoryEditorTextBox(repositoryManager);
                case DxRepositoryEditorType.TextBoxButton: return new DxRepositoryEditorTextBoxButton(repositoryManager);
                case DxRepositoryEditorType.Button: return new DxRepositoryEditorButton(repositoryManager);
                case DxRepositoryEditorType.EditBox: return new DxRepositoryEditorEditBox(repositoryManager);
            }
            throw new ArgumentException($"DxRepositoryEditor.CreateEditor(): Editor for type '{editorType}' does not exists.");
        }
        #endregion
        #region Support pro potomky, včetně abstract; vazba na Manager.Cache
        /// <summary>
        /// Potomek zde vykreslí prvek svého typu podle dodaných dat do dané grafiky a prostoru
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="pdea"></param>
        /// <param name="controlBounds"></param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        public abstract void PaintItem(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds, bool isDisplayed);
        /// <summary>
        /// Je voláno po změně interaktivního stavu dané buňky. Možná bude třeba pro buňku vytvořit a umístit nativní Control, anebo bude možno jej odebrat...
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="controlBounds"></param>
        public abstract void ChangeItemInteractiveState(IPaintItemData paintItem, WinDraw.Rectangle controlBounds);
        /// <summary>
        /// Typ editoru
        /// </summary>
        public abstract DxRepositoryEditorType EditorType { get; }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected abstract EditorCacheMode CacheMode { get; }
        /// <summary>
        /// Režim ukládání grafických dat
        /// </summary>
        protected enum EditorCacheMode
        {
            /// <summary>
            /// Nikdy neukládat = vždy generovat nová data
            /// </summary>
            None,
            /// <summary>
            /// Ukládat do privátní property <see cref="IPaintItemData.ImageData"/>
            /// </summary>
            ItemData,
            /// <summary>
            /// Ukládat do společné cache v <see cref="DxRepositoryManager"/>
            /// </summary>
            ManagerCache,
            /// <summary>
            /// Přímé kreslení (Label, Image, a možná další jako Title?)
            /// </summary>
            DirectPaint
        }
        /// <summary>
        /// Pokud je dán klíč, pro který máme v evidenci data (uložená bitmapa ve formě byte[]), pak ji najde a vrátí true. Určí i ID dat.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="imageData"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        protected bool TryGetCacheImageData(string key, out byte[] imageData, out ulong id)
        {
            return __RepositoryManager.TryGetCacheImageData(key, out imageData, out id);
        }
        /// <summary>
        /// Pokud je dán ID, pro který máme v evidenci data (uložená bitmapa ve formě byte[]), pak ji najde a vrátí true.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="imageData"></param>
        /// <returns></returns>
        protected bool TryGetCacheImageData(ulong id, out byte[] imageData)
        {
            return __RepositoryManager.TryGetCacheImageData(id, out imageData);
        }
        /// <summary>
        /// Do cache přidá data (uložená bitmapa ve formě byte[]) pro daný klíč. Pokud klíč nebo data jsou null, nepřidá nic.
        /// Pokud pro daný klíč už data evidujeme, aktualizuje je.
        /// Vrátí ID těchto dat. ID bude uloženo do prvku jako krátký identifikátor bitmapy.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="imageData"></param>
        protected ulong AddImageDataToCache(string key, byte[] imageData)
        {
            return __RepositoryManager.AddImageDataToCache(key, imageData);
        }
        /// <summary>
        /// Metoda vrátí klíč z dodaných dat
        /// </summary>
        /// <param name="text"></param>
        /// <param name="controlSize"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        protected virtual string CreateKey(string text, WinDraw.Size controlSize, params string[] others)
        {
            string key = $"{CreateEditorTypeCode(this.EditorType)};{text};{controlSize.Width}×{controlSize.Height}";
            foreach (var other in others)
                key += $";{other}";
            return key;
        }
        /// <summary>
        /// Vrátí krátký string reprezentující typ editoru, do klíče
        /// </summary>
        /// <param name="editorType"></param>
        /// <returns></returns>
        protected virtual string CreateEditorTypeCode(DxRepositoryEditorType editorType)
        {
            switch (editorType)
            {
                case DxRepositoryEditorType.None: return "0";
                case DxRepositoryEditorType.Label: return "L";
                case DxRepositoryEditorType.Title: return "H";
                case DxRepositoryEditorType.TextBox: return "T";
                case DxRepositoryEditorType.TextBoxButton: return "N";
                case DxRepositoryEditorType.EditBox: return "E";
                case DxRepositoryEditorType.CheckBox: return "X";
                case DxRepositoryEditorType.RadioButton: return "O";
                case DxRepositoryEditorType.Button: return "B";
                case DxRepositoryEditorType.DropDownButton: return "D";
                case DxRepositoryEditorType.ComboListBox: return "C";
                case DxRepositoryEditorType.TrackBar: return "A";
                case DxRepositoryEditorType.Image: return "I";
                case DxRepositoryEditorType.Grid: return "G";
                case DxRepositoryEditorType.Tree: return "S";
                case DxRepositoryEditorType.HtmlContent: return "W";
                case DxRepositoryEditorType.Panel: return "P";
                case DxRepositoryEditorType.PageSet: return "Q";
                case DxRepositoryEditorType.Page: return "Y";
            }
            return editorType.ToString();
        }
        #endregion
        #region Support pro potomky - čtení hodnot z prvku IPaintItemData
        /// <summary>
        /// Z prvku přečte a vrátí jeho hodnotu
        /// </summary>
        /// <param name="paintItem"></param>
        /// <returns></returns>
        protected object GetItemValue(IPaintItemData paintItem) { return (paintItem.TryGetContent<object>(Data.DxDataFormDef.Value, out var content) ? content : null); }
        /// <summary>
        /// Z prvku přečte a vrátí konstantní text Label
        /// </summary>
        /// <param name="paintItem"></param>
        /// <returns></returns>
        protected string GetItemLabel(IPaintItemData paintItem) { return (paintItem.TryGetContent<string>(Data.DxDataFormDef.Label, out var content) ? content : null); }
        /// <summary>
        /// Z prvku přečte a vrátí konstantní text ToolTipText
        /// </summary>
        /// <param name="paintItem"></param>
        /// <returns></returns>
        protected string GetItemToolTipText(IPaintItemData paintItem) { return (paintItem.TryGetContent<string>(Data.DxDataFormDef.ToolTipText, out var content) ? content : null); }
        /// <summary>
        /// Z prvku přečte a vrátí BorderStyle
        /// </summary>
        /// <param name="paintItem"></param>
        /// <returns></returns>
        protected BorderStyles GetItemBorderStyle(IPaintItemData paintItem)
        {
            if (!paintItem.TryGetContent<string>(Data.DxDataFormDef.BorderStyle, out var content)) return BorderStyles.Default;
            if (!Enum.TryParse(content, out BorderStyles borderStyles)) return BorderStyles.Default;
            return borderStyles;
        }
        /// <summary>
        /// Z prvku přečte a vrátí požadovanou hodnotu anebo vrátí default hodnotu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="paintItem"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected T GetItemContent<T>(IPaintItemData paintItem, string name, T defaultValue) { return (paintItem.TryGetContent<T>(name, out T content) ? content : defaultValue); }
        #endregion
        #region Podpora pro potomky: ValueToKey (konverze typové hodnoty do krátkého stringu do klíče)
        /// <summary>
        /// Převede danou hodnotu do stringu do klíče
        /// </summary>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        protected static string ToKey(WinDraw.FontStyle fontStyle)
        {
            return "S" +
                   (fontStyle.HasFlag(WinDraw.FontStyle.Bold) ? "B" : "") +
                   (fontStyle.HasFlag(WinDraw.FontStyle.Italic) ? "I" : "") +
                   (fontStyle.HasFlag(WinDraw.FontStyle.Underline) ? "U" : "") +
                   (fontStyle.HasFlag(WinDraw.FontStyle.Strikeout) ? "S" : "");
        }
        /// <summary>
        /// Převede danou hodnotu do stringu do klíče
        /// </summary>
        /// <param name="ratio"></param>
        /// <returns></returns>
        protected static string ToKey(float ratio)
        {
            return ratio.ToString().Replace(",", ".");
        }
        /// <summary>
        /// Převede danou hodnotu do stringu do klíče
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected static string ToKey(WinDraw.Color color)
        {
            return color.ToArgb().ToString("X8");
        }

        #endregion
        #region Podpora pro konverze: Control => Bitmap => byte[], a následně byte[] => Bitmap => Graphics
        /// <summary>
        /// Metoda řeší vykreslení Image pro potomka na základě vygenerovaných dat obrázku
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="pdea"></param>
        /// <param name="controlBounds"></param>
        protected virtual void PaintImageData(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            byte[] imageData = null;
            switch (CacheMode)
            {
                case EditorCacheMode.DirectPaint:
                    // Přímé kreslení - např. Label se řeší zde:
                    PaintImageDirect(paintItem, pdea, controlBounds);
                    break;
                case EditorCacheMode.ManagerCache:
                    // Použít ManagerCache: pokud v prvku máme ImageId, najdeme "naše" data podle toho ID:
                    //  => to je tehdy, když typicky opakovaně kreslíme prvek, který jsme už dříve kreslili, a máme tedy uloženo rychlé ID do cache...
                    // Uložená data nemusí být pouze "naše": pokud více prvků vytvoří shodný klíč pro data (key = CreateKey(..)),
                    //  pak těchto více prvků (díky klíči a organizaci CacheImage) pro tento společný klíč bude sdílet i společné ID.
                    // Účelem uloženého ID je jen to, že nemusíme generovat klíč Key při každém kreslení.
                    // Po změně fyzických dat v prvku anebo po změně stylu se provede invalidace (buď v konkrétním prvku, anebo celého systému cache)
                    //  a následující kreslení nenajde cachovaná data (ani v ManagerCache, ani v ImageData) a vytviří se nová...
                    ulong imageId;
                    bool isInCache = (paintItem.ImageId.HasValue && TryGetCacheImageData(paintItem.ImageId.Value, out imageData));
                    if (!isInCache)
                    {   // Nemáme ImageId (buď v prvku nebo v manageru): vytvoříme string klíč pro konkrétní data, a zkusíme najít Image podle klíče:
                        string key = CreateKey(paintItem, controlBounds);
                        // Pokud pro Key najdeme data, pak najdeme i ID = imageId:
                        if (!TryGetCacheImageData(key, out imageData, out imageId))
                        {   // Pokud ani pro klíč nenajdeme data, pak je vytvoříme právě nyní a přidáme do managera,
                            imageData = CreateImageData(paintItem, pdea, controlBounds);
                            //  a uchováme si imageId přidělené v manageru:
                            imageId = AddImageDataToCache(key, imageData);
                        }
                        paintItem.ImageId = imageId;
                    }
                    break;
                case EditorCacheMode.ItemData:
                    // Nepoužívat ManagerCache, ale jen lokální úložiště:
                    if (paintItem.ImageData is null)
                        paintItem.ImageData = CreateImageData(paintItem, pdea, controlBounds);
                    imageData = paintItem.ImageData;
                    break;
                case EditorCacheMode.None:
                    // Žádné úložiště, vždy znovu vygenerovat ImageData:
                    imageData = CreateImageData(paintItem, pdea, controlBounds);
                    break;
            }
            PaintFormData(imageData, pdea, controlBounds);
        }
        /// <summary>
        /// Potomek zde přímo do cílové grafiky vykreslí obsah prvku, bez cache
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="pdea"></param>
        /// <param name="controlBounds"></param>
        protected virtual void PaintImageDirect(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds) { }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="controlBounds"></param>
        /// <returns></returns>
        protected abstract string CreateKey(IPaintItemData paintItem, WinDraw.Rectangle controlBounds);
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="pdea"></param>
        /// <param name="controlBounds"></param>
        /// <returns></returns>
        protected abstract byte[] CreateImageData(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds);
        /// <summary>
        /// Požádá dodaný Control, aby se vykreslil do nové pracovní bitmapy daného nebo odpovídajícího rozměru, a z té bitmapy nám vrátil byte[]
        /// </summary>
        /// <param name="control"></param>
        /// <param name="exactBitmapSize"></param>
        /// <returns></returns>
        protected virtual byte[] CreateBitmapData(WinForm.Control control, WinDraw.Size? exactBitmapSize = null)
        {
            int w = exactBitmapSize?.Width ?? control.Width;
            int h = exactBitmapSize?.Height ?? control.Height;
            using (var bitmap = new WinDraw.Bitmap(w, h))
            {
                bitmap.MakeTransparent();
                control.DrawToBitmap(bitmap, new WinDraw.Rectangle(0, 0, w, h));
                return CreateBitmapData(bitmap);
            }
        }
        /// <summary>
        /// Z dodané bitmapy vrátí byte[]
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        protected virtual byte[] CreateBitmapData(WinDraw.Bitmap bitmap)
        {
            using (var stream = new SysIO.MemoryStream())
            {
                bitmap.Save(stream, this.CacheImageFormat);
                return stream.GetBuffer();
            }
        }
        /// <summary>
        /// Vykreslí obrázek uložený v dodaném prvku <paramref name="paintItem"/> do dané grafiky v <paramref name="pdea"/> na určenou souřadnici <paramref name="controlBounds"/>
        /// </summary>
        /// <param name="paintItem"></param>
        /// <param name="pdea"></param>
        /// <param name="controlBounds"></param>
        protected virtual void PaintFormData(IPaintItemData paintItem, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            PaintFormData(paintItem?.ImageData, pdea, controlBounds);
        }
        /// <summary>
        /// Vykreslí obrázek uložený v dodaném poli <paramref name="data"/> do dané grafiky v <paramref name="pdea"/> na určenou souřadnici <paramref name="controlBounds"/>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="pdea"></param>
        /// <param name="controlBounds"></param>
        protected virtual void PaintFormData(byte[] data, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            if (data != null && data.Length > 0)
            {
                using (var stream = new SysIO.MemoryStream(data))
                using (var bitmap = WinDraw.Bitmap.FromStream(stream))
                    pdea.Graphics.DrawImageUnscaled(bitmap, controlBounds);
            }
        }
        #endregion
    }
    #endregion
    #endregion
}
