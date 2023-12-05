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
using System.Windows.Forms;
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
        /// <param name="dataForm"></param>
        public DxRepositoryManager(DxDataFormPanel dataForm)
        {
            __DataForm = dataForm;
            _InitRepository();
            _InitCache();
        }
        void IDisposable.Dispose()
        {
            _DisposeRepository();
            _DisposeCache();
        }
        /// <summary>
        /// Hlavní instance Dataformu
        /// </summary>
        public DxDataFormPanel DataForm { get { return __DataForm; } } private DxDataFormPanel __DataForm;
        /// <summary>
        /// Panel obsahující data Dataformu
        /// </summary>
        public DxDataFormContentPanel DataFormContent { get { return __DataForm?.DataFormContent; } }
        /// <summary>
        /// Zajistí vykreslení obsahu Dataformu
        /// </summary>
        public void DataFormDraw() { DataFormContent?.Draw(); }
        #endregion
        #region Public hodnoty a služby
        /// <summary>
        /// Formát bitmap, který se ukládá do cache. Čte se z DataFormu: <see cref="DxDataFormPanel.CacheImageFormat"/>
        /// </summary>
        public WinDraw.Imaging.ImageFormat CacheImageFormat { get { return DataForm.CacheImageFormat; } }
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
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        /// <returns></returns>
        public void PaintItem(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds, bool isDisplayed)
        {
            if (paintData is null) return;
            if (_TryGetEditor(paintData.EditorType, out var editor))
                editor.PaintItem(paintData, pdea, controlBounds, isDisplayed);
        }
        /// <summary>
        /// Je voláno po změně interaktivního stavu dané buňky. Možná bude třeba pro buňku vytvořit a umístit nativní Control, anebo bude možno jej odebrat...
        /// </summary>
        /// <param name="paintData">Aktivní prvek</param>
        /// <param name="container">Fyzický container, na němž má být přítomný fyzický Control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        public void ChangeItemInteractiveState(IPaintItemData paintData, WinForm.Control container, WinDraw.Rectangle controlBounds)
        {
            if (paintData is null) return;
            if (_TryGetEditor(paintData.EditorType, out var editor))
                editor.ChangeItemInteractiveState(paintData, container, controlBounds);
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
        /// Interaktivní stav. 
        /// <para/>
        /// Lze setovat, setování provede validaci a po změně vyvolá odpovídající událost.
        /// Setování slouží víceméně pro řízení příznaku <see cref="DxInteractiveState.HasFocus"/>, 
        /// protože tento příznak řídí nativní control (jeho eventy <see cref="WinForm.Control.Enter"/> a <see cref="WinForm.Control.Leave"/>).
        /// </summary>
        DxInteractiveState InteractiveState { get; set; }
        /// <summary>
        /// Nativní control, zobrazující zdejší data. 
        /// O jeho vložení a odebrání se stará <see cref="DxRepositoryManager"/> v procesu změny interaktivního stavu prvku <see cref="InteractiveState"/>,
        /// v metodě <see cref="DxRepositoryManager.ChangeItemInteractiveState(IPaintItemData, WinForm.Control, Rectangle)"/>, na základě typu prvku <see cref="EditorType"/>.
        /// <para/>
        /// <see cref="DxRepositoryManager"/> určí, zda pro daný interaktivní stav je zapotřebí umístit nativní Control, a v případě potřeby jej získá,
        /// kompletně naplní daty zdejšího prvku, a do Container panelu jej vloží. 
        /// Manager pak odchytává eventy controlu, a předává je ke zpracování do DataFormu včetně this datového objektu jako zdroje dat.
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
        /// Vrátí true, pokud daný prvek má aktuálně používat nativní Control (tedy nebude se keslit jako Image),
        /// a to dle aktuálního interaktivním stavu objektu a s přihlédnutím ke stavu <paramref name="isDisplayed"/>.
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        /// <returns></returns>
        protected override bool NeedUseNativeControl(IPaintItemData paintData, bool isDisplayed)
        {
            return false;                        // Label nepotřebuje nativní control nikdy...
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek má vykreslovat svoje data (=kreslit pomocí metod např. <see cref="DxRepositoryEditor.CreateImageData(IPaintItemData, PaintDataEventArgs, Rectangle)"/>), 
        /// a to dle aktuálního interaktivním stavu objektu a s přihlédnutím ke stavu <paramref name="isDisplayed"/>.
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        /// <returns></returns>
        protected override bool NeedPaintItem(IPaintItemData paintData, bool isDisplayed)
        {
            return (isDisplayed);                // Label bude evykreslovat tehdy, když je ve viditelné oblasti
        }
        /// <summary>
        /// Potomek zde přímo do cílové grafiky vykreslí obsah prvku, bez cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected override void PaintImageDirect(IPaintItemData paintData, PaintDataEventArgs pdea, Rectangle controlBounds)
        {
            string text = GetItemLabel(paintData);
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
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, Rectangle controlBounds)
        {
            string text = GetItemLabel(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormDef.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormDef.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormDef.TextColor, WinDraw.Color.Empty);

            return CreateKey(text, controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor));
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            // Některé controly (a DevExpress.XtraEditors.LabelControl mezi nimi) mají tu nectnost, že první vykreslení bitmapy v jejich životě je chybné.
            //  Nejde o první vykreslení do jedné Graphics, jde o první vykreslení po konstruktoru.
            //  Projevuje se to tak, že do Bitmapy vykreslí pozadí zcela černé v celé ploše mimo text Labelu!
            // Proto detekujeme první použití (kdy na začátku je __EditorPaint = null), a v tom případě vygenerujeme tu úplně první bitmapu naprázdno.
            bool isFirstUse = (__EditorPaint is null);

            __EditorPaint ??= _CreateNativeControl(false);
            _FillNativeControl(paintData, __EditorPaint, controlBounds);

            // První vygenerovanou bitmapu v životě Labelu vytvoříme a zahodíme, není pěkná...
            if (isFirstUse) CreateBitmapData(__EditorPaint);

            // Teprve ne-první bitmapy jsou OK:
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override Control CreateNativeControl()
        {
            return null;
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="nativeControl">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected override void FillNativeControl(IPaintItemData paintData, Control nativeControl, Rectangle controlBounds) 
        {
            this._FillNativeControl(paintData, nativeControl as DevExpress.XtraEditors.LabelControl, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="withEvents"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <returns></returns>
        private DevExpress.XtraEditors.LabelControl _CreateNativeControl(bool withEvents)
        {
            var control = new DevExpress.XtraEditors.LabelControl() { Location = new WinDraw.Point(25, -200) };
            control.ResetBackColor();
            control.LineStyle = WinDraw.Drawing2D.DashStyle.Solid;
            control.LineVisible = true;
            control.LineLocation = DevExpress.XtraEditors.LineLocation.Bottom;
            control.LineColor = WinDraw.Color.DarkBlue;
            control.LineOrientation = DevExpress.XtraEditors.LabelLineOrientation.Horizontal;
            control.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Office2003;

            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(IPaintItemData paintData, DevExpress.XtraEditors.LabelControl control, WinDraw.Rectangle controlBounds)
        {
            control.Text = GetItemLabel(paintData);
            control.Size = controlBounds.Size;
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
        /// Nativní control používaný pro kreslení
        /// </summary>
        DevExpress.XtraEditors.LabelControl __EditorPaint;
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
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCacheWithItemImage; } }
        /// <summary>
        /// Potomek zde přímo do cílové grafiky vykreslí obsah prvku, bez cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        protected override void PaintImageDirect(IPaintItemData paintData, PaintDataEventArgs pdea, Rectangle controlBounds) { }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormDef.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormDef.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormDef.TextColor, WinDraw.Color.Empty);

            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor));
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= _CreateNativeControl(false);
            _FillNativeControl(paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="paintData"></param>
        /// <param name="nativeControl"></param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(IPaintItemData paintData, Control nativeControl, Rectangle controlBounds)
        {
            this._FillNativeControl(paintData, nativeControl as DevExpress.XtraEditors.TextEdit, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="withEvents"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <returns></returns>
        private DevExpress.XtraEditors.TextEdit _CreateNativeControl(bool withEvents)
        {
            var control = new DevExpress.XtraEditors.TextEdit() { Location = new WinDraw.Point(25, -200) };
            control.ResetBackColor();

            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected void _FillNativeControl(IPaintItemData paintData, DevExpress.XtraEditors.TextEdit control, WinDraw.Rectangle controlBounds)
        {
            control.EditValue = GetItemValue(paintData);
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintData);
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
        /// Nativní control používaný pro kreslení
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
        /// Potomek zde přímo do cílové grafiky vykreslí obsah prvku, bez cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected override void PaintImageDirect(IPaintItemData paintData, PaintDataEventArgs pdea, Rectangle controlBounds) { }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormDef.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormDef.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormDef.TextColor, WinDraw.Color.Empty);
            string buttons = GetItemContent(paintData, Data.DxDataFormDef.TextBoxButtons, "");
            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor), buttons);
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= new DevExpress.XtraEditors.ButtonEdit();
            PrepareControl(__EditorPaint, paintData, pdea, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected void PrepareControl(DevExpress.XtraEditors.ButtonEdit control, IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {

            object value = GetItemValue(paintData);

            control.EditValue = value;
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintData);
            control.Properties.Buttons.Clear();
            var buttons = this.GetItemContent(paintData, Data.DxDataFormDef.TextBoxButtons, "");
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
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override Control CreateNativeControl()
        {
            return new DevExpress.XtraEditors.ButtonEdit();
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="paintData"></param>
        /// <param name="nativeControl"></param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(IPaintItemData paintData, Control nativeControl, Rectangle controlBounds)
        {
        }
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
        /// Potomek zde přímo do cílové grafiky vykreslí obsah prvku, bez cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        protected override void PaintImageDirect(IPaintItemData paintData, PaintDataEventArgs pdea, Rectangle controlBounds) { }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormDef.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormDef.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormDef.TextColor, WinDraw.Color.Empty);

            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor));
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= new DevExpress.XtraEditors.MemoEdit();
            PrepareControl(__EditorPaint, paintData, pdea, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="control">Cílový control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        protected void PrepareControl(DevExpress.XtraEditors.MemoEdit control, IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            control.EditValue = GetItemValue(paintData);
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintData);
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
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override Control CreateNativeControl()
        {
            return new DevExpress.XtraEditors.MemoEdit();
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="paintData"></param>
        /// <param name="nativeControl"></param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(IPaintItemData paintData, Control nativeControl, Rectangle controlBounds)
        {
        }
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
        /// Potomek zde přímo do cílové grafiky vykreslí obsah prvku, bez cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        protected override void PaintImageDirect(IPaintItemData paintData, PaintDataEventArgs pdea, Rectangle controlBounds) { }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, Rectangle controlBounds)
        {
            string text = GetItemLabel(paintData);
            var iconName = this.GetItemContent(paintData, Data.DxDataFormDef.IconName, "");
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormDef.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormDef.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormDef.TextColor, WinDraw.Color.Empty);

            return CreateKey(text, controlBounds.Size, iconName, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor));
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= _CreateNativeControl(false);
            _FillNativeControl(paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="nativeControl">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected override void FillNativeControl(IPaintItemData paintData, Control nativeControl, Rectangle controlBounds)
        {
            this._FillNativeControl(paintData, nativeControl as DevExpress.XtraEditors.SimpleButton, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="withEvents"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <returns></returns>
        private DevExpress.XtraEditors.SimpleButton _CreateNativeControl(bool withEvents)
        {
            var control = new DevExpress.XtraEditors.SimpleButton() { Location = new WinDraw.Point(25, -200) };
            control.ResetBackColor();

            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected void _FillNativeControl(IPaintItemData paintData, DevExpress.XtraEditors.SimpleButton control, WinDraw.Rectangle controlBounds)
        {
            control.Text = GetItemLabel(paintData);
            control.Size = controlBounds.Size;

            var iconName = this.GetItemContent(paintData, Data.DxDataFormDef.IconName, "");
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
        /// <summary>
        /// Nativní control používaný pro kreslení
        /// </summary>
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
        /// Typ editoru
        /// </summary>
        public abstract DxRepositoryEditorType EditorType { get; }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected abstract EditorCacheMode CacheMode { get; }
        /// <summary>
        /// Dispose
        /// </summary>
        void IDisposable.Dispose() { _Dispose(); OnDispose(); }
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
        #region Podpora pro práci s nativním controlem
        /// <summary>
        /// Je voláno po změně interaktivního stavu dané buňky. Možná bude třeba pro buňku vytvořit a umístit nativní Control, anebo bude možno jej odebrat...
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="container">Fyzický container, na němž má být přítomný fyzický Control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        public virtual void ChangeItemInteractiveState(IPaintItemData paintData, WinForm.Control container, WinDraw.Rectangle controlBounds)
        {
            bool needUseNativeControl = NeedUseNativeControl(paintData, true);
            if (needUseNativeControl)
            {   // Pokud se aktuálně pro tento prvek používá Nativní control, pak jen ověříme, že je připraven a přítomen na správné souřadnici.
                // Často jsme volání víceméně po pohybu Scrollbarem kvůli přesunu nativního controlu na nové místo:
                CheckValidNativeControl(paintData, container, controlBounds, true);
                return;
            }
            else
            {   // Prvek by nemusel vlastnit nativní control - měl by jej tedy uvolnit:
                CheckReleaseNativeControl(paintData, controlBounds, true, true);
            }
        }
        /// <summary>
        /// Metoda zajistí, že pro daný prvek bude připraven nativní control.
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="container">Fyzický container, na němž má být přítomný fyzický Control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        /// <param name="isStateChanged">true = metoda je volána po změně interaktivního stavu, false = je volána v procesu Draw</param>
        protected virtual void CheckValidNativeControl(IPaintItemData paintData, WinForm.Control container, WinDraw.Rectangle controlBounds, bool isStateChanged)
        {
            // Základem práce je pár { IPaintItemData <=> NatoveControl } :
            ControlDataPair pair;
            if (!_TryGetAttachedPair(paintData, out pair))
                pair = _GetDisponiblePair(paintData);

            // Pár musí mít NativeControl. Nemá jej pouze při prvním použití, pak si jej nese stále, do konce až do Release:
            if (pair.NativeControl is null)
                pair.NativeControl = _CreateNativeControl();

            // Příznak, že NativeControl je pro paintData nový:
            bool isNonAttachedControl = (paintData.NativeControl is null || !Object.ReferenceEquals(paintData.NativeControl, pair.NativeControl));

            // Pokud v paintData mám jiný NativeControl, než je v Pair (nebo žádný), tak je korektně propojím:
            if (isNonAttachedControl)
                pair.AttachPaintData(paintData);

            // Do NativeControlu vložím buď všechna data, anebo jen souřadnici:
            var nativeControl = pair.NativeControl;
            if (nativeControl != null)
            {   // Pokud tedy máme vytvořený NativeControl (on ne každý Editor generuje nativní control!), tak jej naplníme daty:
                // ... a pokud je to nutno, pak jej zařadíme jako Child control do containeru:
                if (nativeControl.Parent is null)
                    container.Controls.Add(nativeControl);

                if (isNonAttachedControl)
                    _FillNativeControl(pair, paintData, controlBounds);
                else
                    _UpdateNativeBounds(pair, controlBounds);
            }
        }
        /// <summary>
        /// Zajistí prvotní vytvoření Controlu,
        /// </summary>
        /// <returns></returns>
        private WinForm.Control _CreateNativeControl()
        {
            var control = CreateNativeControl();

            // Handlery naše, pokud control není null:


            return control;
        }
        /// <summary>
        /// Zajistí naplnění controlu aktuálními daty,
        /// </summary>
        /// <param name="pair">Párová informace o datovém objektu a natovním controlu, plus target souřadnice</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair pair, IPaintItemData paintData, Rectangle controlBounds)
        {
            pair.NativeControl.Bounds = controlBounds;
            FillNativeControl(paintData, pair.NativeControl, controlBounds);   // To řeší konkrétní potomek - naplní všechna konkrétní data...
            pair.ControlBounds = controlBounds;
        }
        /// <summary>
        /// Zajistí naplnění souřadnic controlu aktuálními daty, jen pokud jsou tyto souřadnice jiné než předešlé...
        /// </summary>
        /// <param name="pair">Párová informace o datovém objektu a natovním controlu, plus target souřadnice</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _UpdateNativeBounds(ControlDataPair pair, Rectangle controlBounds)
        {
            bool isChangedBounds = (!pair.ControlBounds.HasValue || (pair.ControlBounds.HasValue && pair.ControlBounds.Value != controlBounds));
            if (isChangedBounds)
            {
                pair.NativeControl.Bounds = controlBounds;
                pair.ControlBounds = controlBounds;
            }
        }
        /// <summary>
        /// Potomek zde vytvoří a vrátí nativní control.
        /// </summary>
        /// <returns></returns>
        protected abstract WinForm.Control CreateNativeControl();
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// </summary>
        /// <param name="paintData"></param>
        /// <param name="nativeControl"></param>
        /// <param name="controlBounds"></param>
        protected abstract void FillNativeControl(IPaintItemData paintData, Control nativeControl, Rectangle controlBounds);
        /// <summary>
        /// Metoda zajistí, že pokud pro daný prvek máme připravený nativní control, tak nyní bude pravděpodobně odebrán.
        /// <para/>
        /// Prvek někdy necháme, a někdy odebereme... Kdy a proč?<br/>
        /// a) Pokud jsou souřadnice nativního prvku shodné s těmi aktuálními dle parametru <paramref name="controlBounds"/>, pak prvek ponecháme na místě.<br/>
        /// Důvod: pokud jen myš nebo kurzor odešla z prvku, ale prvek zůstává na svém místě, může ještě probíhat animace typu "FadeOut" (typicky buttony v některých skinech).
        /// Necháme tedy animaci proběhnout. Nativní control odebereme, až bude potřeba někde jinde, anebo při posunu controlu (až se budou lišit souřadnice ControlBounds).<br/>
        /// b) Pokud se liší souřadnice umístění nativního controlu (ControlBounds) od aktuálně zadaných, tak nativní control odeberu ihned.<br/>
        /// Důvod: Museli bychom control přemisťovat společně s virtuálním obsahem, a to nedokážeme synchronizovat tak, aby nevznikal dojem, 
        /// že nativní control po parent containeru plave.
        /// <para/>
        /// Proto si v rámci <see cref="ControlDataPair"/> pamatujeme souřadnice, kam jsme control posledně umístili (souřadnice "požadovaná"): 
        /// pro porovnání nepoužíváme souřadnici fyzického nativního controlu, protože ta se může lišit od souřadnice požadované...
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        /// <param name="isStateChanged">true = metoda je volána po změně interaktivního stavu, false = je volána v procesu Draw</param>
        protected virtual void CheckReleaseNativeControl(IPaintItemData paintData, WinDraw.Rectangle controlBounds, bool isDisplayed, bool isStateChanged)
        {
            if (!_TryGetAttachedPair(paintData, out var pair)) return;

            if (!isDisplayed)
            {
                pair.DetachPaintData();
            }

            // Prozatím nejlepší řešení:
            pair.DetachPaintData();
        }
        /// <summary>
        /// Metoda zjistí, zda daný prvek s daty <paramref name="paintData"/> je již napojen na některý nativní control v <see cref="_ControlUseQ"/> nebo <see cref="_ControlUseW"/>.
        /// Pokud je napojen, určí jej a vrátí true.
        /// </summary>
        /// <param name="paintData"></param>
        /// <param name="foundPair"></param>
        /// <returns></returns>
        private bool _TryGetAttachedPair(IPaintItemData paintData, out ControlDataPair foundPair)
        {
            if (paintData != null)
            {
                ControlDataPair pair;

                pair = _ControlUseQ;
                if (isAtttachedTo(pair)) { foundPair = pair; return true; }

                pair = _ControlUseW;
                if (isAtttachedTo(pair)) { foundPair = pair; return true; }
            }
            foundPair = null; 
            return false;

            // Metoda určí, zda aktuálně daný prvek 'paintData' je připojen v dodaném testovaném páru.
            bool isAtttachedTo(ControlDataPair testPair)
            {
                return (testPair.PaintData != null && Object.ReferenceEquals(testPair.PaintData, paintData));
            }
        }
        /// <summary>
        /// Najde a vrátí disponibilní pár, jehož <see cref="ControlDataPair.NativeControl"/> je možno použít pro zobrazení nových dat.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private ControlDataPair _GetDisponiblePair(IPaintItemData paintData)
        {
            ControlDataPair pair;

            pair = _ControlUseQ;
            if (isDisponible(pair)) return pair;

            pair = _ControlUseW;
            if (isDisponible(pair)) return pair;

            throw new InvalidOperationException($"DxRepositoryEditor has not any disponible ControlDataPair.");


            // Vrátí true, pokud zadaný Pair je možno aktuálně použít = je disponibilní:
            bool isDisponible(ControlDataPair testPair)
            {
                // Pokud tam nic není, můžeme to použít:
                if (testPair.NativeControl is null)
                {
                    testPair.DetachPaintData();
                    return true;
                }

                // Ani PaintData?
                if (testPair.PaintData is null) return true;

                // Pokud v páru je přítomen právě ten prvek paintData, který hledá Pair, pak je to OK:
                if (Object.ReferenceEquals(testPair.PaintData, paintData)) return true;

                // Pokud datový prvek v jeho aktuálním stavu stále potřebuje svůj nativní control, tak mu jej nebudeme brát:
                if (NeedUseNativeControl(testPair.PaintData, true)) return false;

                // Máme nativní control a máme i prvek, ale prvek jej už nepotřebuje = odvážeme datový prvek, a uvolněný Pair použijeme my:
                testPair.DetachPaintData();
                return true;
            }
        }
        /// <summary>
        /// Obsluha události po vstupu focusu do nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        private void _RunNativeControlFocusEnter(ControlDataPair dataPair)
        {
            if (dataPair.PaintData is null) return;

            // Přidat příznak HasFocus:
            DxInteractiveState maskHasFocus = DxInteractiveState.HasFocus;
            dataPair.PaintData.InteractiveState |= maskHasFocus;
            OnNativeControlFocusEnter(dataPair);
        }
        /// <summary>
        /// Obsluha události po odchodu focusu z nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        private void _RunNativeControlFocusLeave(ControlDataPair dataPair)
        {
            if (dataPair.PaintData is null) return;

            // Tady bychom mohli využít faktu, že máme NativeControl, a uložit si jeho obraz do PaintData.ImageData, a příště budeme vykreslovat přímo tento obraz:
            if (this.CacheMode == EditorCacheMode.ManagerCacheWithItemImage)
                dataPair.PaintData.ImageData = CreateBitmapData(dataPair.NativeControl);

            // Odebrat příznak HasFocus:
            DxInteractiveState maskNonFocus = (DxInteractiveState)(Int32.MaxValue ^ (int)DxInteractiveState.HasFocus);
            dataPair.PaintData.InteractiveState &= maskNonFocus;     // Tady proběhne InteractiveStateChange => zdejší ChangeItemInteractiveState() => a možná CheckReleaseNativeControl() => a tedy zmizí fyzický NativeControl z containeru
            OnNativeControlFocusEnter(dataPair);
            this.__RepositoryManager.DataFormDraw();                 // Musíme zajistit vykreslení panelu (sám si to neudělá), aby byl vidět obraz prvku!
        }
        /// <summary>
        /// Je voláno po vstupu focusu do nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        protected virtual void OnNativeControlFocusEnter(ControlDataPair dataPair) { }
        /// <summary>
        /// Je voláno po odchodu focusu z nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        protected virtual void OnNativeControlFocusLeave(ControlDataPair dataPair) { }
        #endregion
        #region ControlDataPair : dvě instance a třída, nativní WinForm controly reprezentující fyzický vstup
        /// <summary>
        /// Úložiště jednoho ze dvou controlů, které se poskytují do plochy k používání.
        /// </summary>
        private ControlDataPair _ControlUseQ { get { __ControlUseQ ??= new ControlDataPair(this); return __ControlUseQ; } } private ControlDataPair __ControlUseQ;
        /// <summary>
        /// Úložiště jednoho ze dvou controlů, které se poskytují do plochy k používání.
        /// </summary>
        private ControlDataPair _ControlUseW { get { __ControlUseW ??= new ControlDataPair(this); return __ControlUseW; } } private ControlDataPair __ControlUseW;
        /// <summary>
        /// Dispose nativních controlů
        /// </summary>
        private void _Dispose()
        {
            __ControlUseQ?.ReleaseAll();
            __ControlUseQ = null;

            __ControlUseW?.ReleaseAll();
            __ControlUseW = null;
        }
        /// <summary>
        /// Párová informace o nativním controlu <see cref="NativeControl"/> (který je takto součástí <see cref="DxRepositoryEditor"/>) 
        /// a jeho aktuálním přiřazení do konkrétního vizuální buňky <see cref="PaintData"/> (typ <see cref="IPaintItemData"/>).
        /// <para/>
        /// Nativní control se víceméně nemění, zůstává v této instanci umístěn až do <see cref="ReleaseAll()"/>.<br/>
        /// Vizuální buňka se mění podle potřeby.
        /// </summary>
        protected class ControlDataPair
        {
            #region Konstruktor, data, Release
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="editor"></param>
            public ControlDataPair(DxRepositoryEditor editor)
            {
                __Editor = editor;
            }
            /// <summary>
            /// Reference na editor
            /// </summary>
            private DxRepositoryEditor __Editor;
            /// <summary>
            /// Nativní control
            /// </summary>
            private WinForm.Control __NativeControl;
            /// <summary>
            /// Má můj prvek Focus?
            /// </summary>
            private bool __HasFocus;
            /// <summary>
            /// Nativní Control, víceméně zde bydlí permanentně týž objekt
            /// </summary>
            public WinForm.Control NativeControl
            {
                get { return __NativeControl; }
                set 
                {   // Eventhandlery původního controlu odvázat, a nového navázat:
                    _DetachNativeControlEvents();
                    __NativeControl = value;
                    _AttachNativeControlEvents();
                }
            }
            /// <summary>
            /// Interaktivní prvek, střídají se zde podle potřeby - kdo zrovna potřebuje mít nativní control, ten je zde uložen.
            /// </summary>
            public IPaintItemData PaintData { get; set; }
            /// <summary>
            /// Má můj prvek Focus?
            /// </summary>
            public bool HasFocus { get { return __HasFocus; } }
            /// <summary>
            /// Souřadnice, na kterou jsme nativní control původně umístili. Nemusí být shodná s <see cref="WinForm.Control.Bounds"/>, a to ani okamžitě po prvním setování.
            /// </summary>
            public WinDraw.Rectangle? ControlBounds { get; set; }
            /// <summary>
            /// Zapojí dodaný datový prvek jako nynějšího vlastníka zdejšího <see cref="NativeControl"/>
            /// </summary>
            /// <param name="paintData">Data konkrétního prvku</param>
            /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
            public void AttachPaintData(IPaintItemData paintData, WinDraw.Rectangle? controlBounds = null)
            {
                // Dosavadní datový prvek (i když by tam neměl být) odpojíme od NativeControl:
                if (this.PaintData != null) this.PaintData.NativeControl = null;

                // Nově dodaný datový prvek propojíme s NativeControl a uložíme sem:
                paintData.NativeControl = this.NativeControl;
                this.PaintData = paintData;

                this.ControlBounds = controlBounds;

                this.NativeControl.Visible = true;
                if (controlBounds.HasValue)
                    this.NativeControl.Bounds = controlBounds.Value;
            }
            /// <summary>
            /// Uvolní ze své evidence prvek <see cref="PaintData"/> včetně navazující logiky
            /// </summary>
            public void DetachPaintData()
            {
                if (this.PaintData != null) this.PaintData.NativeControl = null;
                this.PaintData = null;
                this.ControlBounds = null;
                if (this.NativeControl != null)
                {
                    this.NativeControl.Visible = false;
                    if (__HasFocus)
                        this.NativeControl.Parent.Select();
                }
            }
            /// <summary>
            /// Uvolní instance ve zdejší evidenci
            /// </summary>
            public void ReleaseAll()
            {
                DetachPaintData();

                if (this.NativeControl != null)
                {
                    this.NativeControl.Dispose();
                    this.NativeControl = null;
                }
            }
            #endregion
            #region Eventhandlery nativního controlu
            /// <summary>
            /// Po napojení nového <see cref="__NativeControl"/> do něj zaregistrujeme naše handlery
            /// </summary>
            private void _AttachNativeControlEvents()
            {
                var control = __NativeControl;
                if (control != null)
                {
                    control.Enter += _NativeControlEnter;
                    control.Leave += _NativeControlLeave;
                }
            }
            /// <summary>
            /// Před odpojením stávajícího <see cref="__NativeControl"/> z něj odregistrujeme naše handlery
            /// </summary>
            private void _DetachNativeControlEvents()
            {
                var control = __NativeControl;
                if (control != null)
                {
                    control.Enter -= _NativeControlEnter;
                    control.Leave -= _NativeControlLeave;
                }
            }
            /// <summary>
            /// Do nativního controlu vstoupil focus
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void _NativeControlEnter(object sender, EventArgs e)
            {
                __HasFocus = true;
                __Editor._RunNativeControlFocusEnter(this);
            }
            /// <summary>
            /// Z nativního controlu odešel focus
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void _NativeControlLeave(object sender, EventArgs e)
            {
                __HasFocus = false;
                __Editor._RunNativeControlFocusLeave(this);
            }
            #endregion
        }
        #endregion
        #region Podpora pro práci s vykreslením prvku a práci s Cache uložených obrazů jednotlivých Controlů
        /// <summary>
        /// Potomek zde vykreslí prvek svého typu podle dodaných dat do dané grafiky a prostoru
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        public void PaintItem(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds, bool isDisplayed)
        {
            if (paintData is null) return;

            bool needUseNativeControl = NeedUseNativeControl(paintData, isDisplayed);
            if (needUseNativeControl) 
            {   // Pokud se aktuálně pro tento prvek používá Nativní control, pak jen ověříme, že je připraven a přítomen na správné souřadnici.
                // Často jsme volání víceméně po pohybu Scrollbarem kvůli přesunu nativního controlu na nové místo:
                CheckValidNativeControl(paintData, pdea.InteractivePanel, controlBounds, false);
                return;
            }
            CheckReleaseNativeControl(paintData, controlBounds, isDisplayed, false);

            // Nepotřebujeme nativní control => nejspíš budeme jen kreslit jeho obraz:
            bool needPaint = NeedPaintItem(paintData, isDisplayed);
            if (needPaint)
            {
                PaintImageData(paintData, pdea, controlBounds);
            }
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek má aktuálně používat nativní Control (tedy: bude zobrazen fyzický Control, a nebude se kreslit jen jako statický Image),
        /// a to dle aktuálního interaktivním stavu objektu a s přihlédnutím ke stavu <paramref name="isDisplayed"/>.<br/>
        /// Pokud volající metoda neví, zda prvek je/není viditelný, pak jako parametr <paramref name="isDisplayed"/> musí poslat true.
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        /// <returns></returns>
        protected virtual bool NeedUseNativeControl(IPaintItemData paintData, bool isDisplayed)
        {   // Většina aktivních prvků to má takhle; a ty specifické prvky si metodu přepíšou...
            return (isDisplayed && ((paintData.InteractiveState & DxInteractiveState.MaskUseNativeControl) != 0));
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek má vykreslovat svoje data (=kreslit pomocí metod např. <see cref="DxRepositoryEditor.CreateImageData(IPaintItemData, PaintDataEventArgs, Rectangle)"/>), 
        /// a to dle aktuálního interaktivním stavu objektu a s přihlédnutím ke stavu <paramref name="isDisplayed"/>.
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        /// <returns></returns>
        protected virtual bool NeedPaintItem(IPaintItemData paintData, bool isDisplayed)
        {   // Většina aktivních prvků to má takhle; a ty specifické prvky si metodu přepíšou...
            return (isDisplayed && ((paintData.InteractiveState & DxInteractiveState.MaskUseNativeControl) == 0));
        }
        /// <summary>
        /// Metoda řeší vykreslení Image pro potomka na základě vygenerovaných dat obrázku
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected virtual void PaintImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            byte[] imageData = null;
            var cacheMode = CacheMode;
            switch (cacheMode)
            {
                case EditorCacheMode.DirectPaint:
                    // Přímé kreslení - např. Label se řeší zde:
                    PaintImageDirect(paintData, pdea, controlBounds);
                    break;
                case EditorCacheMode.ManagerCache:
                case EditorCacheMode.ManagerCacheWithItemImage:
                    if (cacheMode == EditorCacheMode.ManagerCacheWithItemImage && paintData.ImageData != null)
                    {   // Toto nastává v situaci, kdy jsme měli k dispozici NativeControl pro editaci (nebo MouseOver),
                        // a zachovali jsme si jeho poslední obraz v paintData.ImageData => pak jej budeme keslit přímo z něj:
                        imageData = paintData.ImageData;
                    }
                    else
                    {   // Použít ManagerCache: pokud v prvku máme ImageId, najdeme "naše" data podle toho ID:
                        //  => to je tehdy, když typicky opakovaně kreslíme prvek, který jsme už dříve kreslili, a máme tedy uloženo rychlé ID do cache...
                        // Uložená data nemusí být pouze "naše": pokud více prvků vytvoří shodný klíč pro data (key = CreateKey(..)),
                        //  pak těchto více prvků (díky klíči a organizaci CacheImage) pro tento společný klíč bude sdílet i společné ID.
                        // Účelem uloženého ID je jen to, že nemusíme generovat klíč Key při každém kreslení.
                        // Po změně fyzických dat v prvku anebo po změně stylu se provede invalidace (buď v konkrétním prvku, anebo celého systému cache)
                        //  a následující kreslení nenajde cachovaná data (ani v ManagerCache, ani v ImageData) a vytviří se nová...
                        ulong imageId;
                        bool isInCache = (paintData.ImageId.HasValue && TryGetCacheImageData(paintData.ImageId.Value, out imageData));
                        if (!isInCache)
                        {   // Nemáme ImageId (buď v prvku nebo v manageru): vytvoříme string klíč pro konkrétní data, a zkusíme najít Image podle klíče:
                            string key = CreateKey(paintData, controlBounds);
                            // Pokud pro Key najdeme data, pak najdeme i ID = imageId:
                            if (!TryGetCacheImageData(key, out imageData, out imageId))
                            {   // Pokud ani pro klíč nenajdeme data, pak je vytvoříme právě nyní a přidáme do managera,
                                imageData = CreateImageData(paintData, pdea, controlBounds);
                                //  a uchováme si imageId přidělené v manageru:
                                imageId = AddImageDataToCache(key, imageData);
                            }
                            paintData.ImageId = imageId;
                        }
                    }
                    break;
                case EditorCacheMode.ItemData:
                    // Nepoužívat ManagerCache, ale jen lokální úložiště:
                    if (paintData.ImageData is null)
                        paintData.ImageData = CreateImageData(paintData, pdea, controlBounds);
                    imageData = paintData.ImageData;
                    break;
                case EditorCacheMode.None:
                    // Žádné úložiště, vždy znovu vygenerovat ImageData:
                    imageData = CreateImageData(paintData, pdea, controlBounds);
                    break;
            }
            PaintFormData(imageData, pdea, controlBounds);
        }
        /// <summary>
        /// Potomek zde přímo do cílové grafiky vykreslí obsah prvku, bez cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected virtual void PaintImageDirect(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds) { }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        /// <returns></returns>
        protected abstract string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds);
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        /// <returns></returns>
        protected abstract byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds);
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
            /// Ukládat do společné cache v <see cref="DxRepositoryManager"/>, a při LostFocus zachytit živý NativeControl do ImageData
            /// </summary>
            ManagerCacheWithItemImage,
            /// <summary>
            /// Přímé kreslení (Label, Image, a možná další jako Title?)
            /// </summary>
            DirectPaint
        }
        #endregion
        #region Podpora pro potomky: čtení hodnot z prvku IPaintItemData
        /// <summary>
        /// Z prvku přečte a vrátí jeho hodnotu
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <returns></returns>
        protected object GetItemValue(IPaintItemData paintData) { return (paintData.TryGetContent<object>(Data.DxDataFormDef.Value, out var content) ? content : null); }
        /// <summary>
        /// Z prvku přečte a vrátí konstantní text Label
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <returns></returns>
        protected string GetItemLabel(IPaintItemData paintData) { return (paintData.TryGetContent<string>(Data.DxDataFormDef.Label, out var content) ? content : null); }
        /// <summary>
        /// Z prvku přečte a vrátí konstantní text ToolTipText
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <returns></returns>
        protected string GetItemToolTipText(IPaintItemData paintData) { return (paintData.TryGetContent<string>(Data.DxDataFormDef.ToolTipText, out var content) ? content : null); }
        /// <summary>
        /// Z prvku přečte a vrátí BorderStyle
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <returns></returns>
        protected BorderStyles GetItemBorderStyle(IPaintItemData paintData)
        {
            if (!paintData.TryGetContent<string>(Data.DxDataFormDef.BorderStyle, out var content)) return BorderStyles.Default;
            if (!Enum.TryParse(content, out BorderStyles borderStyles)) return BorderStyles.Default;
            return borderStyles;
        }
        /// <summary>
        /// Z prvku přečte a vrátí požadovanou hodnotu anebo vrátí default hodnotu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected T GetItemContent<T>(IPaintItemData paintData, string name, T defaultValue) { return (paintData.TryGetContent<T>(name, out T content) ? content : defaultValue); }
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
        /// Vykreslí obrázek uložený v dodaném prvku <paramref name="paintData"/> do dané grafiky v <paramref name="pdea"/> na určenou souřadnici <paramref name="controlBounds"/>
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected virtual void PaintFormData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            PaintFormData(paintData?.ImageData, pdea, controlBounds);
        }
        /// <summary>
        /// Vykreslí obrázek uložený v dodaném poli <paramref name="data"/> do dané grafiky v <paramref name="pdea"/> na určenou souřadnici <paramref name="controlBounds"/>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
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
