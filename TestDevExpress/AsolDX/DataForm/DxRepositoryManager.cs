// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using SysIO = System.IO;
using WinDraw = System.Drawing;
using WinForm = System.Windows.Forms;
using DxDData = Noris.Clients.Win.Components.AsolDX.DataForm.Data;
using DxeEdit = DevExpress.XtraEditors;
using DxeCont = DevExpress.XtraEditors.Controls;
using System.Drawing;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    #region DxRepositoryManager : správce úložiště fyzických controlů, i cache obrázků jednotlivých naplněných buněk pro jejich pasivní vykreslení
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
                    DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"DxRepositoryManager.CleanUpCache() From: {count} [{totalBytes} B]; Removed: {removeCount} [{removeSize} B]; Time: {DxComponent.LogTokenTimeMicrosec}", startTime);
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
    #endregion
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
        /// v metodě <see cref="DxRepositoryManager.ChangeItemInteractiveState(IPaintItemData, WinForm.Control, WinDraw.Rectangle)"/>, na základě typu prvku <see cref="EditorType"/>.
        /// <para/>
        /// <see cref="DxRepositoryManager"/> určí, zda pro daný interaktivní stav je zapotřebí umístit nativní Control, a v případě potřeby jej získá,
        /// kompletně naplní daty zdejšího prvku, a do Container panelu jej vloží. 
        /// Manager pak odchytává eventy controlu, a předává je ke zpracování do DataFormu včetně this datového objektu jako zdroje dat.
        /// </summary>
        WinForm.Control NativeControl { get; set; }
        /// <summary>
        /// Řádek s daty
        /// </summary>
        DxDData.DataFormRow Row { get; }
        /// <summary>
        /// Definice layoutu pro tuto buňku
        /// </summary>
        DxDData.DataFormLayoutItem LayoutItem { get; }
        /// <summary>
        /// Zkusí najít hodnotu požadované vlastnosti.
        /// Hodnota se prioritně hledá v řádku (=specifická pro konkrétní řádek), a pokud tam není, pak se hledá v layoutu (defaultní hodnota).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <param name="content">Out hodnota</param>
        /// <returns></returns>
        bool TryGetContent<T>(DxDData.DxDataFormProperty property, out T content);
        /// <summary>
        /// Vloží danou hodnotu do daného datového prvku do daného jména
        /// </summary>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <param name="value"></param>
        void SetContent<T>(DxDData.DxDataFormProperty property, T value);
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
    #region DxRepositoryEditor : Abtraktní předek konkrétních editorů; obsahuje Factory pro tvorbu konkrétních potomků a spoustu výkonného kódu
    /// <summary>
    /// Obecný abtraktní předek konkrétních editorů; obsahuje Factory pro tvorbu konkrétních potomků
    /// </summary>
    internal abstract class DxRepositoryEditor : IDisposable
    {
        #region Konstruktor, Dispose.    Static Factory metoda - zařadit nové potomky pro nové typy editorů!
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        public DxRepositoryEditor(DxRepositoryManager repositoryManager)
        {
            __RepositoryManager = repositoryManager;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{EditorType}";
        }
        /// <summary>
        /// Reference na <see cref="DxRepositoryManager"/>, do něhož tento editor patří
        /// </summary>
        protected DxRepositoryManager RepositoryManager { get { return __RepositoryManager; } } private DxRepositoryManager __RepositoryManager;
        /// <summary>
        /// Hlavní instance Dataformu
        /// </summary>
        protected DxDataFormPanel DataForm { get { return __RepositoryManager?.DataForm; } }
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
                case DxRepositoryEditorType.CheckEdit: return new DxRepositoryEditorCheckEdit(repositoryManager);
                case DxRepositoryEditorType.ToggleSwitch: return new DxRepositoryEditorToggleSwitch(repositoryManager);
                case DxRepositoryEditorType.Button: return new DxRepositoryEditorButton(repositoryManager);
                case DxRepositoryEditorType.EditBox: return new DxRepositoryEditorEditBox(repositoryManager);
                case DxRepositoryEditorType.ComboListBox: return new DxRepositoryEditorComboListBox(repositoryManager);
                case DxRepositoryEditorType.ImageComboListBox: return new DxRepositoryEditorImageComboListBox(repositoryManager);
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
            // <=====   TEST / DEBUG / SMAZAT :
            var state = paintData.InteractiveState;
            if (state.HasFlag(DxInteractiveState.MouseLeftDown) || state.HasFlag(DxInteractiveState.HasFocus))
            {
                bool hasPair = TryGetAttachedPair(paintData, out var pair);
                var pairText = pair?.ToString();
            }
            //  =====>

            bool needUseNativeControl = NeedUseNativeControl(paintData, true, false);
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
            ControlDataPair dataPair;
            if (!TryGetAttachedPair(paintData, out dataPair))
                dataPair = _GetDisponiblePair(paintData);

            // Pár musí mít NativeControl. Nemá jej pouze při prvním použití, pak si jej nese stále, do konce až do Release:
            if (dataPair.NativeControl is null)
                dataPair.NativeControl = _CreateNativeControl();

            // Příznak, že NativeControl je pro paintData nový:
            bool isNonAttachedControl = (paintData.NativeControl is null || !Object.ReferenceEquals(paintData.NativeControl, dataPair.NativeControl));

            // Pokud v paintData mám jiný NativeControl, než je v Pair (nebo žádný), tak je korektně propojím:
            if (isNonAttachedControl)
                dataPair.AttachPaintData(paintData);

            // Do NativeControlu vložím buď všechna data, anebo jen souřadnici:
            var nativeControl = dataPair.NativeControl;
            if (nativeControl != null)
            {   // Pokud tedy máme vytvořený NativeControl (on ne každý Editor generuje nativní control!), tak jej naplníme daty:
                // ... a pokud je to nutno, pak jej zařadíme jako Child control do containeru:
                if (nativeControl.Parent is null)
                    container.Controls.Add(nativeControl);

                if (isNonAttachedControl)
                    _FillNativeControl(dataPair, controlBounds);
                else
                    _UpdateNativeBounds(dataPair, controlBounds);
                nativeControl.Visible = true;
                // _DebugNativeControl(pair);
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
        /// <param name="dataPair">Párová informace o datovém objektu a natovním controlu, plus target souřadnice</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            try
            {
                SuppressNativeEvents = true;
                FillNativeControl(dataPair, controlBounds);   // To řeší konkrétní potomek - naplní všechna konkrétní data...
                dataPair.NativeControl.Bounds = controlBounds;
                dataPair.ControlBounds = controlBounds;
            }
            finally
            {
                SuppressNativeEvents = false;
            }
        }
        /// <summary>
        /// Zajistí naplnění souřadnic controlu aktuálními daty, jen pokud jsou tyto souřadnice jiné než předešlé...
        /// </summary>
        /// <param name="dataPair">Párová informace o datovém objektu a natovním controlu, plus target souřadnice</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _UpdateNativeBounds(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            bool isChangedBounds = (!dataPair.ControlBounds.HasValue || (dataPair.ControlBounds.HasValue && dataPair.ControlBounds.Value != controlBounds));
            if (isChangedBounds)
            {
                dataPair.NativeControl.Bounds = controlBounds;
                dataPair.ControlBounds = controlBounds;
            }
        }
        /// <summary>
        /// Příznak, že aktuálně probíhá metoda <see cref="FillNativeControl(IPaintItemData, WinForm.Control, Rectangle)"/>, 
        /// tedy že do Nativního controlu se setují hodnoty z datového prvku - přitom mohou probíhat nativní eventy controlu (typicky: ValueChanged);
        /// ale pokud zde v <see cref="SuppressNativeEvents"/> je true, pak se musí ignorovat!<br/>
        /// To je zásadní informace pro eventhandlery ve třídách potomků, kde typicky bude: <code>if (SuppressNativeEvents) return;</code>
        /// </summary>
        protected bool SuppressNativeEvents { get; private set; }
        /// <summary>
        /// Potomek zde vytvoří a vrátí nativní control.
        /// </summary>
        /// <returns></returns>
        protected abstract WinForm.Control CreateNativeControl();
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected abstract void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds);
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
            if (!TryGetAttachedPair(paintData, out var pair)) return;

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
        protected bool TryGetAttachedPair(IPaintItemData paintData, out ControlDataPair foundPair)
        {
            if (paintData != null)
            {
                ControlDataPair dataPair;

                dataPair = _ControlUseQ;
                if (isAttachedTo(dataPair)) { foundPair = dataPair; return true; }

                dataPair = _ControlUseW;
                if (isAttachedTo(dataPair)) { foundPair = dataPair; return true; }
            }
            foundPair = null;
            return false;

            // Metoda určí, zda aktuálně daný prvek 'paintData' je připojen v dodaném testovaném páru.
            bool isAttachedTo(ControlDataPair testPair)
            {
                return (testPair.PaintData != null && Object.ReferenceEquals(testPair.PaintData, paintData));
            }
        }
        /// <summary>
        /// Metoda zjistí, zda k danému WinForm controlu <paramref name="nativeControl"/> máme v evidenci párovou informaci <see cref="ControlDataPair"/> a určí ji.
        /// Pokud je napojen, určí jej a vrátí true.
        /// </summary>
        /// <param name="nativeControl"></param>
        /// <param name="foundPair"></param>
        /// <returns></returns>
        protected bool TryGetAttachedPair(WinForm.Control nativeControl, out ControlDataPair foundPair)
        {
            if (nativeControl != null)
            {
                ControlDataPair dataPair;

                dataPair = _ControlUseQ;
                if (isAttachedTo(dataPair)) { foundPair = dataPair; return true; }

                dataPair = _ControlUseW;
                if (isAttachedTo(dataPair)) { foundPair = dataPair; return true; }
            }
            foundPair = null;
            return false;

            // Metoda určí, zda aktuálně daný prvek 'paintData' je připojen v dodaném testovaném páru.
            bool isAttachedTo(ControlDataPair testPair)
            {
                return (testPair.NativeControl != null && Object.ReferenceEquals(testPair.NativeControl, nativeControl));
            }
        }

        /// <summary>
        /// Najde a vrátí disponibilní pár, jehož <see cref="ControlDataPair.NativeControl"/> je možno použít pro zobrazení nových dat.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private ControlDataPair _GetDisponiblePair(IPaintItemData paintData)
        {
            ControlDataPair dataPair;

            dataPair = _ControlUseQ;
            if (isDisponible(dataPair, false)) return dataPair;

            dataPair = _ControlUseW;
            if (isDisponible(dataPair, false)) return dataPair;

            dataPair = _ControlUseQ;
            if (isDisponible(dataPair, true)) return dataPair;

            dataPair = _ControlUseW;
            if (isDisponible(dataPair, true)) return dataPair;

            throw new InvalidOperationException($"DxRepositoryEditor has not any disponible ControlDataPair.");


            // Vrátí true, pokud zadaný Pair je možno aktuálně použít = je disponibilní:
            // isUrgent: fakt něco potřebujeme najít - pokud máme někde prvek, který nemá Focus a má jen myš, tak jej uvolni!
            bool isDisponible(ControlDataPair testPair, bool isUrgent)
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

                // Pokud není velká nouze, a datový prvek v jeho aktuálním stavu stále potřebuje svůj nativní control, tak mu jej nebudeme brát:
                if (NeedUseNativeControl(testPair.PaintData, true, isUrgent)) return false;

                // Máme nativní control a máme i prvek, ale prvek jej už nepotřebuje = odvážeme datový prvek, a uvolněný Pair použijeme my:
                testPair.DetachPaintData();
                return true;
            }
        }
        /// <summary>
        /// Metoda zkusí najít data prvku, který je právě nyní zobrazen v předaném nativním controlu <paramref name="sender"/>.
        /// Metoda se používá v třídách potomků, kdy daný potomek zdejší třídy (např. <see cref="DxRepositoryEditorButton"/>) 
        /// má zaregistrován svůj eventhandler v určitém vizuálním Controlu který sám vytvořil v metodě <see cref="CreateNativeControl"/>,
        /// tento nativní control je zobrazen a provede daný event, vyvolá konkrétní eventhandler umístěný ve třídě tohoto potomka,
        /// ale potomek nepředstavuje konkrétní editovanou buňku - představuje jen konkretizovaný editační nástroj,
        /// propojující data <see cref="IPaintItemData"/> a vizuální prvek NativeControl.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="paintData"></param>
        /// <returns></returns>
        protected bool TryGetPaintData(object sender, out IPaintItemData paintData)
        {
            if (sender is WinForm.Control control)
            {
                ControlDataPair dataPair;

                dataPair = _ControlUseQ;
                if (isAtttachedTo(dataPair, control)) { paintData = dataPair.PaintData; return (paintData != null); }

                dataPair = _ControlUseW;
                if (isAtttachedTo(dataPair, control)) { paintData = dataPair.PaintData; return (paintData != null); }
            }
            paintData = null;
            return false;

            // Metoda určí, zda aktuálně daný nativní control 'nativeControl' je umístěn v dodaném testovaném páru.
            bool isAtttachedTo(ControlDataPair testPair, WinForm.Control nativeControl)
            {
                return (testPair.NativeControl != null && Object.ReferenceEquals(testPair.NativeControl, nativeControl));
            }
        }
        #endregion
        #region Předání událostí (Mouse, Focus) z Nativního controlu (z ControlDataPair) do datového objektu PaintData
        /// <summary>
        /// Obsluha události po vstupu myši do nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        private void _RunNativeControlMouseEnter(ControlDataPair dataPair)
        {
            if (dataPair.PaintData is null) return;
            if (dataPair.PaintData.InteractiveState.HasFlag(DxInteractiveState.HasMouse)) return;  // Zdejší metoda už nemusí měnit stav, nejspíš to stihla předešlá metoda

            // Přidat příznak HasMouse:
            DxInteractiveState maskHasMouse = DxInteractiveState.HasMouse;
            dataPair.PaintData.InteractiveState |= maskHasMouse;
            OnNativeControlMouseEnter(dataPair);
        }
        /// <summary>
        /// Obsluha události po odchodu myši z nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        private void _RunNativeControlMouseLeave(ControlDataPair dataPair)
        {
            if (dataPair.PaintData is null) return;
            if (!dataPair.PaintData.InteractiveState.HasFlag(DxInteractiveState.HasMouse)) return; // Zdejší metoda už nemusí měnit stav, nejspíš to stihla předešlá metoda

            // Tady bychom mohli využít faktu, že máme NativeControl, a uložit si jeho obraz do PaintData.ImageData, a příště budeme vykreslovat přímo tento obraz:
            //   Souhra  :  Mouse × Focus  ???
            // if (this.CacheMode == EditorCacheMode.ManagerCacheWithItemImage)
            //     dataPair.PaintData.ImageData = CreateBitmapData(dataPair.NativeControl);

            // Odebrat příznak HasMouse:
            DxInteractiveState maskNonMouse = (DxInteractiveState)(Int32.MaxValue ^ (int)DxInteractiveState.MaskMouse);
            dataPair.PaintData.InteractiveState &= maskNonMouse;     // Tady proběhne InteractiveStateChange => zdejší ChangeItemInteractiveState() => a nejspíš CheckReleaseNativeControl() => a tedy zmizí fyzický NativeControl z containeru!
            OnNativeControlMouseLeave(dataPair);

            this.__RepositoryManager.DataFormDraw();                 //  ... musíme tedy zajistit vykreslení panelu (sám si to neudělá), aby byl vidět obraz prvku namísto nativního Controlu!
        }
        /// <summary>
        /// Obsluha události po vstupu focusu do nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        private void _RunNativeControlFocusEnter(ControlDataPair dataPair)
        {
            if (dataPair.PaintData is null) return;
            if (dataPair.PaintData.InteractiveState.HasFlag(DxInteractiveState.HasFocus)) return;  // Zdejší metoda už nemusí měnit stav, nejspíš to stihla předešlá metoda

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

            // Tato metoda je volána rekurzivně: nejprve z nativního eventu Control.Leave (_NativeControlLeave), v tom prvním volání my změníme dataPair.PaintData.InteractiveState;
            //  změna stavu vyvolá CheckReleaseNativeControl() => DetachPaintData(), ale pak to už zde nemusíme měnit stav a spouštět akci znovu...
            if (!dataPair.PaintData.InteractiveState.HasFlag(DxInteractiveState.HasFocus)) return; // Zdejší metoda už nemusí měnit stav, nejspíš to stihla předešlá metoda

            // Tady bychom mohli využít faktu, že máme NativeControl, a uložit si jeho obraz do PaintData.ImageData, a příště budeme vykreslovat přímo tento obraz:
            //   - ale control v aktuální situaci vypadá stále jako by měl Focus, a jeho obraz tedy je zavádějící...
            // if (this.CacheMode == EditorCacheMode.ManagerCacheWithItemImage)
            //    dataPair.PaintData.ImageData = CreateBitmapData(dataPair.NativeControl);

            // Odebrat příznak HasFocus:
            DxInteractiveState maskNonFocus = (DxInteractiveState)(Int32.MaxValue ^ (int)DxInteractiveState.HasFocus);
            dataPair.PaintData.InteractiveState &= maskNonFocus;     // Tady proběhne InteractiveStateChange => zdejší ChangeItemInteractiveState() => a nejspíš CheckReleaseNativeControl() => a tedy zmizí fyzický NativeControl z containeru!
            OnNativeControlFocusLeave(dataPair);

            this.__RepositoryManager.DataFormDraw();                 //  ... musíme tedy zajistit vykreslení panelu (sám si to neudělá), aby byl vidět obraz prvku namísto nativního Controlu!
        }
        /// <summary>
        /// Je voláno po vstupu myši do nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        protected virtual void OnNativeControlMouseEnter(ControlDataPair dataPair) { }
        /// <summary>
        /// Je voláno po odchodu myši z nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        protected virtual void OnNativeControlMouseLeave(ControlDataPair dataPair) { }
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
        private ControlDataPair _ControlUseQ { get { __ControlUseQ ??= new ControlDataPair(this, "Q"); return __ControlUseQ; } }
        private ControlDataPair __ControlUseQ;
        /// <summary>
        /// Úložiště jednoho ze dvou controlů, které se poskytují do plochy k používání.
        /// </summary>
        private ControlDataPair _ControlUseW { get { __ControlUseW ??= new ControlDataPair(this, "W"); return __ControlUseW; } }
        private ControlDataPair __ControlUseW;
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
            /// <param name="name"></param>
            public ControlDataPair(DxRepositoryEditor editor, string name)
            {
                __Editor = editor;
                __Name = name;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"{__Editor.EditorType}[{__Name}]; PaintData: {PaintData}; NativeControl: {__NativeControl?.Text}";
            }
            /// <summary>
            /// Reference na editor
            /// </summary>
            private DxRepositoryEditor __Editor;
            /// <summary>
            /// Jméno pro debug
            /// </summary>
            private string __Name;
            /// <summary>
            /// Nativní control
            /// </summary>
            private WinForm.Control __NativeControl;
            /// <summary>
            /// Má můj prvek myš?
            /// </summary>
            private bool __HasMouse;
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
            /// Interaktivní prvek, kerý je nositelem editované hodnoty.
            /// S ohledem na proces LostFocus a Validation (kdy nejprve probíhá LostFocus, v jeho rámci pak zdejší <see cref="DetachPaintData()"/> kdy se nuluje <see cref="PaintData"/>)
            /// se v procesu validace nelze spolehnout na objekt v <see cref="PaintData"/> (ten už je null). Proto validace pracuje s <see cref="OriginalPaintData"/>.
            /// Hodnotu do <see cref="OriginalPaintData"/> vkládá konkrétní editor společně v té chvíli, kdy ukládá originál editované hodnoty do <see cref="OriginalValue"/>.
            /// </summary>
            public IPaintItemData OriginalPaintData { get; set; }
            /// <summary>
            /// Hodnota buňky, která byla do nativního controlu <see cref="NativeControl"/> vložena v době plnění controlu v <see cref="DxRepositoryEditor.FillNativeControl(ControlDataPair, Rectangle)"/>.<br/>
            /// Pokud při opouštění buňky se výsledná hodnota v controlu liší od této uložené hodnoty, pak se hlásí změna hodnoty pomocí akce.
            /// </summary>
            public object OriginalValue { get; set; }
            /// <summary>
            /// Hodnota buňky, která je v nativním controlu <see cref="NativeControl"/> přítomna v průběhu editace a po dokončení editace.
            /// Pokud při opouštění buňky se tato výsledná hodnota v controlu liší od hodnoty uložené v <see cref="OriginalValue"/>, pak se hlásí změna hodnoty pomocí akce.
            /// </summary>
            public object CurrentValue { get; set; }
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
            public void AttachPaintData(IPaintItemData paintData)
            {
                // Dosavadní datový prvek (i když by tam neměl být) odpojíme od NativeControl:
                if (this.PaintData != null) this.PaintData.NativeControl = null;

                // Nově dodaný datový prvek propojíme s NativeControl a uložíme sem:
                paintData.NativeControl = this.NativeControl;
                this.PaintData = paintData;

                this.ControlBounds = null;

                this.NativeControl.Visible = false;        // Visible = true dám až po naplnění daty, a po nastavení validní souřadnice, viz CheckValidNativeControl()


                // Toto by nemělo nastat:
                if (this.__HasFocus)
                { }

                if (this.__HasMouse)
                { }

                if (PaintData.InteractiveState.HasFlag(DxInteractiveState.HasFocus))
                { }
            }
            /// <summary>
            /// Uvolní ze své evidence prvek <see cref="PaintData"/> včetně navazující logiky
            /// </summary>
            public void DetachPaintData()
            {
                // Pokud končím ve stavu, kdy mám Focus, tak jej zruším:
                bool hasFocus = this.__HasFocus;
                if (hasFocus && this.PaintData != null)
                {
                    __Editor._RunNativeControlFocusLeave(this);
                    this.__HasFocus = false;
                }
                this.__HasMouse = false;

                // Nativní control skrýt, aby nepřekážel (a nekradl focus):
                if (this.NativeControl != null)
                {
                    if (hasFocus)
                        this.NativeControl.Parent.Select();
                    this.NativeControl.Visible = false;
                }

                // Toto musí být až na konci zdejší metody, protože v dřívějších krocích může dojít k vyvolání jiných metod, kde ještě potřebujeme mít vazbu na PaintData:
                if (this.PaintData != null) this.PaintData.NativeControl = null;
                this.ControlBounds = null;
                this.PaintData = null;
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
                    control.MouseEnter += _NativeControlMouseEnter;
                    control.MouseMove += _NativeControlMouseMove;
                    control.MouseLeave += _NativeControlMouseLeave;
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
            /// Nativní event controlu: myš vstoupila na objekt
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void _NativeControlMouseEnter(object sender, EventArgs e)
            {
                if (!__HasMouse)
                {
                    __HasMouse = true;
                    __Editor._RunNativeControlMouseEnter(this);
                }
            }
            /// <summary>
            /// Nativní event controlu: myš se pohybuje nad objektem (pouze hlídáme, zda jsme ve stavu HasMouse)
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void _NativeControlMouseMove(object sender, WinForm.MouseEventArgs e)
            {
                if (!__HasMouse)
                {
                    __HasMouse = true;
                    __Editor._RunNativeControlMouseEnter(this);
                }
            }
            /// <summary>
            /// Nativní event controlu: myš odešla z controlu
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void _NativeControlMouseLeave(object sender, EventArgs e)
            {
                if (__HasMouse)
                {
                    __HasMouse = false;
                    __Editor._RunNativeControlMouseLeave(this);
                }
            }
            /// <summary>
            /// Nativní event controlu: do controlu vstoupil focus
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void _NativeControlEnter(object sender, EventArgs e)
            {
                if (!__HasFocus)
                {
                    __Editor._RunNativeControlFocusEnter(this);
                    __HasFocus = true;
                }
            }
            /// <summary>
            /// Nativní event controlu: z controlu odešel focus
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void _NativeControlLeave(object sender, EventArgs e)
            {
                if (__HasFocus)
                {
                    __HasFocus = false;
                    __Editor._RunNativeControlFocusLeave(this);
                }
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

            bool needUseNativeControl = NeedUseNativeControl(paintData, isDisplayed, false);
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
        /// <param name="isUrgent">false = běžný dotaz, vrátí true když prvek má myš nebo má klávesový Focus; true = je velká nouze, vrátí true (=potřebuje NativeControl) jen tehdy, když prvek má klávesový Focus (vrátí false když má jen myš)</param>
        /// <returns></returns>
        protected virtual bool NeedUseNativeControl(IPaintItemData paintData, bool isDisplayed, bool isUrgent)
        {   // Většina aktivních prvků to má takhle; a ty specifické prvky si metodu přepíšou...
            if (!isDisplayed) return false;

            DxInteractiveState testMask = (isUrgent ? DxInteractiveState.HasFocus : DxInteractiveState.MaskUseNativeControl);
            return ((paintData.InteractiveState & testMask) != 0);
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
                            if (key != null)
                            {   // Prvek vytvořil klíč => spolupracujeme s Cache v RepositoryManagerem:
                                // Pokud pro Key najdeme data, pak najdeme i ID = imageId:
                                if (!TryGetCacheImageData(key, out imageData, out imageId))
                                {   // Pokud ani pro klíč nenajdeme data, pak je vytvoříme právě nyní a přidáme do managera,
                                    imageData = CreateImageData(paintData, pdea, controlBounds);
                                    //  a uchováme si imageId přidělené v manageru:
                                    imageId = AddImageDataToCache(key, imageData);
                                }
                                paintData.ImageId = imageId;
                            }
                            else
                            {   // Prvek nevytvořil klíč => používáme ImageData:
                                if (paintData.ImageData is null)
                                    paintData.ImageData = CreateImageData(paintData, pdea, controlBounds);
                                imageData = paintData.ImageData;
                            }
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
                case DxRepositoryEditorType.CheckEdit: return "X";
                case DxRepositoryEditorType.RadioButton: return "O";
                case DxRepositoryEditorType.Button: return "B";
                case DxRepositoryEditorType.DropDownButton: return "D";
                case DxRepositoryEditorType.ImageComboListBox: return "C";
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
        #region Podpora čtení a zápis hodnot z/do prvku IPaintItemData, konverze typů DevExpress
        /// <summary>
        /// Z prvku přečte a vrátí jeho hodnotu
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <returns></returns>
        protected object GetItemValue(IPaintItemData paintData) { return (paintData.TryGetContent<object>(Data.DxDataFormProperty.Value, out var content) ? content : null); }
        /// <summary>
        /// Z prvku přečte a vrátí jeho hodnotu
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="defaultValue">Defaultní hodnota, pokud nebude nalezena</param>
        /// <returns></returns>
        protected T GetItemValue<T>(IPaintItemData paintData, T defaultValue) { return (paintData.TryGetContent<T>(Data.DxDataFormProperty.Value, out var content) ? content : defaultValue); }
        /// <summary>
        /// Vloží danou hodnotu do daného datového prvku, provádí se po editaci
        /// </summary>
        /// <param name="paintData"></param>
        /// <param name="value"></param>
        /// <param name="callAction">Vyvolat akci po změně hodnoty?</param>
        protected void SetItemValue(IPaintItemData paintData, object value, bool callAction)
        {
            var oldValue = (callAction ? GetItemValue(paintData) : null);
            paintData.SetContent(Data.DxDataFormProperty.Value, value);
            if (callAction && !Object.Equals(value, oldValue))
            {
                var actionInfo = new DataFormValueChangedInfo(paintData.Row, paintData.LayoutItem.ColumnName, DxDData.DxDataFormAction.ValueChanged, oldValue, value);
                this.DataForm.OnInteractiveAction(actionInfo);
            }
        }
        /// <summary>
        /// Z prvku přečte a vrátí konstantní text Label
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <returns></returns>
        protected string GetItemLabel(IPaintItemData paintData) { return (paintData.TryGetContent<string>(Data.DxDataFormProperty.Label, out var content) ? content : null); }
        /// <summary>
        /// Z prvku přečte a vrátí konstantní text ToolTipText
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <returns></returns>
        protected string GetItemToolTipText(IPaintItemData paintData) { return (paintData.TryGetContent<string>(Data.DxDataFormProperty.ToolTipText, out var content) ? content : null); }
        /// <summary>
        /// Z prvku přečte a vrátí BorderStyle
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <returns></returns>
        protected DxeCont.BorderStyles GetItemBorderStyle(IPaintItemData paintData)
        {
            if (!paintData.TryGetContent<BorderStyle>(Data.DxDataFormProperty.BorderStyle, out var content)) return DxeCont.BorderStyles.Default;
            return ConvertToDxBorderStyles(content, DxeCont.BorderStyles.NoBorder);
        }
        /// <summary>
        /// Z prvku přečte a vrátí BorderStyle
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <returns></returns>
        protected DxeCont.BorderStyles GetItemCheckBoxBorderStyle(IPaintItemData paintData)
        {
            if (!paintData.TryGetContent<BorderStyle>(Data.DxDataFormProperty.CheckBoxBorderStyle, out var content)) return DxeCont.BorderStyles.NoBorder;
            return ConvertToDxBorderStyles(content, DxeCont.BorderStyles.NoBorder);
        }
        /// <summary>
        /// Z prvku přečte a vrátí BorderStyle - z vlastnosti <see cref="Data.DxDataFormProperty.IconHorizontAlignment"/>
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <returns></returns>
        protected DevExpress.Utils.HorzAlignment GetItemIconHorzizontalAlignment(IPaintItemData paintData)
        {
            if (!paintData.TryGetContent<HorizontAlignment>(Data.DxDataFormProperty.IconHorizontAlignment, out var content)) return DevExpress.Utils.HorzAlignment.Default;
            return ConvertToDxHorzAlignment(content, DevExpress.Utils.HorzAlignment.Default);
        }
        /// <summary>
        /// Z prvku přečte a vrátí požadovanou hodnotu anebo vrátí default hodnotu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected T GetItemContent<T>(IPaintItemData paintData, Data.DxDataFormProperty property, T defaultValue) { return (paintData.TryGetContent<T>(property, out T content) ? content : defaultValue); }
        /// <summary>
        /// Z prvku najde a určí požadovanou hodnotu, vrací trueé nalezeno
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="property">Určení požadované vlastnosti (typ property)</param>
        /// <param name="content"></param>
        /// <returns></returns>
        protected bool TryGetItemContent<T>(IPaintItemData paintData, Data.DxDataFormProperty property, out T content) { return paintData.TryGetContent<T>(property, out content); }
        /// <summary>
        /// Konverze do DevExpress hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected DevExpress.Utils.DefaultBoolean ConvertToDxBoolean(bool value)
        {
            return (value ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False);
        }
        /// <summary>
        /// Konverze do DevExpress hodnoty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected DevExpress.Utils.DefaultBoolean ConvertToDxBoolean(bool? value)
        {
            return (value.HasValue ? (value.Value ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False) : DevExpress.Utils.DefaultBoolean.Default);
        }
        /// <summary>
        /// Konverze do DevExpress hodnoty
        /// </summary>
        /// <param name="borderStyles"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected DxeCont.BorderStyles ConvertToDxBorderStyles(BorderStyle? borderStyles, DxeCont.BorderStyles defaultValue)
        {
            if (borderStyles.HasValue)
            {
                switch (borderStyles.Value)
                {
                    case BorderStyle.NoBorder: return DxeCont.BorderStyles.NoBorder;
                    case BorderStyle.Simple: return DxeCont.BorderStyles.Simple;
                    case BorderStyle.Flat: return DxeCont.BorderStyles.Flat;
                    case BorderStyle.HotFlat: return DxeCont.BorderStyles.HotFlat;
                    case BorderStyle.UltraFlat: return DxeCont.BorderStyles.UltraFlat;
                    case BorderStyle.Style3D: return DxeCont.BorderStyles.Style3D;
                    case BorderStyle.Office2003: return DxeCont.BorderStyles.Office2003;
                    case BorderStyle.Default: return DxeCont.BorderStyles.Default;
                }
            }
            return defaultValue;
        }
        /// <summary>
        /// Konverze do DevExpress hodnoty
        /// </summary>
        /// <param name="horizontAlignment"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected DevExpress.Utils.HorzAlignment ConvertToDxHorzAlignment(HorizontAlignment? horizontAlignment, DevExpress.Utils.HorzAlignment defaultValue)
        {
            if (horizontAlignment.HasValue)
            {
                switch (horizontAlignment)
                {
                    case HorizontAlignment.Default: return DevExpress.Utils.HorzAlignment.Default;
                    case HorizontAlignment.Near: return DevExpress.Utils.HorzAlignment.Near;
                    case HorizontAlignment.Center: return DevExpress.Utils.HorzAlignment.Center;
                    case HorizontAlignment.Far: return DevExpress.Utils.HorzAlignment.Far;
                }
            }
            return defaultValue;
        }
        #endregion
        #region Podpora pro tvorbu klíče: klíč se skládá z konkrétních směrodatných dat prvku a jednoznačně identifikuje bitmapu v cache
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache.
        /// Pokud prvek vrátí klíč = null, pak se jeho bitmapa neukládá do systémové Cache, ale do lokální proměnné v konkrétní Cell do ImageData.
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        /// <returns></returns>
        protected abstract string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds);
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
        #region Podpora pro používání Bitmapy: Control => Bitmap => byte[], a následně byte[] => Bitmap => Graphics
        /// <summary>
        /// Požádá dodaný Control, aby se vykreslil do nové pracovní bitmapy daného nebo odpovídajícího rozměru, a z té bitmapy nám vrátil byte[]
        /// </summary>
        /// <param name="control"></param>
        /// <param name="exactBitmapSize"></param>
        /// <param name="bitmapBackColor">Barva pozadí bitmapy</param>
        /// <returns></returns>
        protected virtual byte[] CreateBitmapData(WinForm.Control control, WinDraw.Size? exactBitmapSize = null, WinDraw.Color? bitmapBackColor = null)
        {
            int w = exactBitmapSize?.Width ?? control.Width;
            int h = exactBitmapSize?.Height ?? control.Height;
            using (var bitmap = new WinDraw.Bitmap(w, h))
            {
                if (bitmapBackColor.HasValue)
                    bitmap.MakeTransparent(bitmapBackColor.Value);
                else
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
    #region enum DxRepositoryEditorType
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
        CheckEdit,
        /// <summary>
        /// Přepínací switch (moderní checkbox s animovaným přechodem On-Off)
        /// </summary>
        ToggleSwitch,
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
        /// ComboBox bez obrázků
        /// </summary>
        ComboListBox,
        /// <summary>
        /// ComboBox s obrázky
        /// </summary>
        ImageComboListBox,
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
    #endregion
    #region Sada tříd konkrétních editorů (Label, TextBox, EditBox, Button, .......)
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
        /// <param name="isUrgent">false = běžný dotaz, vrátí true když prvek má myš nebo má klávesový Focus; true = je velká nouze, vrátí true (=potřebuje NativeControl) jen tehdy, když prvek má klávesový Focus (vrátí false když má jen myš)</param>
        /// <returns></returns>
        protected override bool NeedUseNativeControl(IPaintItemData paintData, bool isDisplayed, bool isUrgent)
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
        protected override void PaintImageDirect(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            string text = GetItemLabel(paintData);
            if (!String.IsNullOrEmpty(text))
            {
                var font = DxComponent.GetFontDefault(targetDpi: pdea.InteractivePanel.CurrentDpi);
                var color = DxComponent.GetSkinColor(SkinElementColor.CommonSkins_ControlText) ?? WinDraw.Color.Violet;
                var alignment = this.GetItemContent<WinDraw.ContentAlignment>(paintData, Data.DxDataFormProperty.LabelAlignment, WinDraw.ContentAlignment.MiddleLeft);
                DxComponent.DrawText(pdea.Graphics, text, font, controlBounds, color, alignment);
            }
        }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            string text = GetItemLabel(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);

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
            // Některé controly (a DxeEdit.LabelControl mezi nimi) mají tu nectnost, že první vykreslení bitmapy v jejich životě je chybné.
            //  Nejde o první vykreslení do jedné Graphics, jde o první vykreslení po konstruktoru.
            //  Projevuje se to tak, že do Bitmapy vykreslí pozadí zcela černé v celé ploše mimo text Labelu!
            // Proto detekujeme první použití (kdy na začátku je __EditorPaint = null), a v tom případě vygenerujeme tu úplně první bitmapu naprázdno.
            bool isFirstUse = (__EditorPaint is null);

            __EditorPaint ??= _CreateNativeControl(false);
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);

            // První vygenerovanou bitmapu v životě Labelu vytvoříme a zahodíme, není pěkná...
            if (isFirstUse) CreateBitmapData(__EditorPaint);

            // Teprve ne-první bitmapy jsou OK:
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return null;
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds) 
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.LabelControl, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <returns></returns>
        private DxeEdit.LabelControl _CreateNativeControl(bool isInteractive)
        {
            var control = new DxeEdit.LabelControl() { Location = new WinDraw.Point(25, -200) };
            control.ResetBackColor();
            control.LineStyle = WinDraw.Drawing2D.DashStyle.Solid;
            control.LineVisible = true;
            control.LineLocation = DxeEdit.LineLocation.Bottom;
            control.LineColor = WinDraw.Color.DarkBlue;
            control.LineOrientation = DxeEdit.LabelLineOrientation.Horizontal;
            control.BorderStyle = DxeCont.BorderStyles.Office2003;

            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.LabelControl control, WinDraw.Rectangle controlBounds)
        {   //  Ono po pravdě, protože 'CacheMode' je DirectPaint, a protože NeedUseNativeControl vrací vždy false, 
            // tak je jisto, že Label nikdy nebude používat nativní Control => neprojdeme tudy:
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
        DxeEdit.LabelControl __EditorPaint;
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
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);

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
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.TextEdit, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <returns></returns>
        private DxeEdit.TextEdit _CreateNativeControl(bool isInteractive)
        {
            var control = new DxeEdit.TextEdit() { Location = new WinDraw.Point(25, -200) };
            control.ResetBackColor();
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.TextEdit control, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            control.EditValue = value;
            if (dataPair != null)
            {
                dataPair.OriginalPaintData = paintData;
                dataPair.OriginalValue = value;
            }
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintData);
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.TextEdit control)
        {
            control.CausesValidation = true;
            control.EditValueChanged += _ControlEditValueChanged;
            control.Validating += _NativeControlValidating;
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v TextEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ControlEditValueChanged(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.TextEdit control && TryGetAttachedPair(control, out var dataPair))
            {   // Toto je průběžně volaný event, v procesu editace, a nemá valného významu:
                dataPair.CurrentValue = control.EditValue;
            }
        }
        /// <summary>
        /// Eventhandler při validaci zadané hodnoty
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _NativeControlValidating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.TextEdit control && TryGetAttachedPair(control, out var dataPair) && dataPair.OriginalPaintData != null)
            {   // Toto je event volaný při ukončení editace
                dataPair.CurrentValue = control.EditValue;
                if (!Object.Equals(dataPair.CurrentValue, dataPair.OriginalValue))
                {
                    IPaintItemData paintData = dataPair.OriginalPaintData;
                    var actionInfo = new DataFormValueChangingInfo(paintData.Row, paintData.LayoutItem.ColumnName, DxDData.DxDataFormAction.ValueValidating, dataPair.OriginalValue, dataPair.CurrentValue);
                    this.DataForm.OnInteractiveAction(actionInfo);
                    if (actionInfo.Cancel)
                    { }
                    else
                    {
                        SetItemValue(dataPair.OriginalPaintData, dataPair.CurrentValue, true);
                        dataPair.OriginalValue = dataPair.CurrentValue;
                        paintData.InvalidateCache();
                    }
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
        /// Nativní control používaný pro kreslení
        /// </summary>
        DxeEdit.TextEdit __EditorPaint;
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
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);
            var buttons = this.GetItemContent<DxDData.TextBoxButtonProperties>(paintData, Data.DxDataFormProperty.TextBoxButtons, null);
            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor), buttons?.Key);
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
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.ButtonEdit, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <returns></returns>
        private DxeEdit.ButtonEdit _CreateNativeControl(bool isInteractive)
        {
            var control = new DxeEdit.ButtonEdit() { Location = new WinDraw.Point(25, -200) };
            control.ResetBackColor();
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.ButtonEdit control, WinDraw.Rectangle controlBounds)
        {
            control.EditValue = GetItemValue(paintData);
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintData);

            control.Properties.Buttons.Clear();
            var buttons = this.GetItemContent<DxDData.TextBoxButtonProperties>(paintData, Data.DxDataFormProperty.TextBoxButtons, null);
            if (buttons != null)
            {
                control.Properties.ButtonsStyle = buttons.BorderStyle;
                control.Properties.Buttons.AddRange(buttons.CreateDxButtons());
            }
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.ButtonEdit control)
        {
            control.EditValueChanged += _ControlEditValueChanged;
            control.ButtonClick += _NativeControlButtonClick;
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v ButtonEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ControlEditValueChanged(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ButtonEdit control && TryGetPaintData(control, out var paintData))
            {
                SetItemValue(paintData, control.EditValue, true);
            }
        }
        /// <summary>
        /// Eventhandler po kliknutí na button v ButtonEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlButtonClick(object sender, DxeCont.ButtonPressedEventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ButtonEdit control && TryGetPaintData(control, out var paintData))
            {
                string actionName = e.Button.Tag as string;
                var actionInfo = new DataFormItemNameInfo(paintData.Row, paintData.LayoutItem.ColumnName, DxDData.DxDataFormAction.ButtonClick, actionName);
                this.DataForm.OnInteractiveAction(actionInfo);
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
        DxeEdit.ButtonEdit __EditorPaint;
    }
    #endregion
    #region CheckEdit
    /// <summary>
    /// CheckEdit
    /// </summary>
    internal class DxRepositoryEditorCheckEdit : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        public DxRepositoryEditorCheckEdit(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        public override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.CheckEdit; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);
            var buttons = this.GetItemContent<DxDData.TextBoxButtonProperties>(paintData, Data.DxDataFormProperty.TextBoxButtons, null);
            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor), buttons?.Key);
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
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint, null, pdea.InteractivePanel.BackColor);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.CheckEdit, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <returns></returns>
        private DxeEdit.CheckEdit _CreateNativeControl(bool isInteractive)
        {
            var control = new DxeEdit.CheckEdit() { Location = new WinDraw.Point(25, -200) };
            // Konstantní nastavení controlu:
            control.Properties.AutoHeight = false;
            control.Properties.AutoWidth = false;
            control.Properties.CheckBoxOptions.Style = DxeCont.CheckBoxStyle.SvgCheckBox1;
            control.Properties.GlyphAlignment = DevExpress.Utils.HorzAlignment.Near;
            // control.ResetBackColor();
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.CheckEdit control, WinDraw.Rectangle controlBounds)
        {
            control.Size = controlBounds.Size;
            control.ReadOnly = true;
            control.Enabled = true;
            string label = GetItemLabel(paintData);
            control.Checked = GetItemValue(paintData, false);
            control.Properties.Caption = label;
            control.Properties.DisplayValueUnchecked = GetItemContent(paintData, DxDData.DxDataFormProperty.CheckBoxLabelFalse, label);
            control.Properties.DisplayValueChecked = GetItemContent(paintData, DxDData.DxDataFormProperty.CheckBoxLabelTrue, label);
            control.Properties.BorderStyle = GetItemCheckBoxBorderStyle(paintData);
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.CheckEdit control)
        {
            control.EditValueChanged += _ControlEditValueChanged;
            control.CheckedChanged += _NativeControlCheckedChanged;
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v CheckEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ControlEditValueChanged(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.CheckEdit control && TryGetPaintData(control, out var paintData))
            {
                SetItemValue(paintData, control.EditValue, true);
            }
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v CheckEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlCheckedChanged(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.CheckEdit control && TryGetPaintData(control, out var paintData))
            {
                SetItemValue(paintData, control.EditValue, true);
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
        DxeEdit.CheckEdit __EditorPaint;
    }
    #endregion
    #region ToggleSwitch
    /// <summary>
    /// ToggleSwitch
    /// </summary>
    internal class DxRepositoryEditorToggleSwitch : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        public DxRepositoryEditorToggleSwitch(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        public override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.ToggleSwitch; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);
            var buttons = this.GetItemContent<DxDData.TextBoxButtonProperties>(paintData, Data.DxDataFormProperty.TextBoxButtons, null);
            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor), buttons?.Key);
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
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.ToggleSwitch, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <returns></returns>
        private DxeEdit.ToggleSwitch _CreateNativeControl(bool isInteractive)
        {
            var control = new DxeEdit.ToggleSwitch() { Location = new WinDraw.Point(25, -200) };
            // Konstantní nastavení controlu:
            control.Properties.AutoHeight = false;
            control.Properties.AutoWidth = false;
            control.Properties.ShowText = true;
            control.ResetBackColor();
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.ToggleSwitch control, WinDraw.Rectangle controlBounds)
        {
            control.Size = controlBounds.Size;
            control.IsOn = GetItemValue(paintData, false);
            control.Properties.BorderStyle = GetItemCheckBoxBorderStyle(paintData);
            control.Properties.EditorToThumbWidthRatio = GetItemContent(paintData, DxDData.DxDataFormProperty.ToggleSwitchRatio, 2.5f);
            control.Properties.OffText = GetItemContent(paintData, DxDData.DxDataFormProperty.CheckBoxLabelFalse, "Off");
            control.Properties.OnText = GetItemContent(paintData, DxDData.DxDataFormProperty.CheckBoxLabelTrue, "On");
            control.Properties.GlyphAlignment = GetItemIconHorzizontalAlignment(paintData);
            // control.ReadOnly = true;
            // control.Enabled = true;
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.ToggleSwitch control)
        {
            control.EditValueChanged += _ControlEditValueChanged;
            control.Toggled += _NativeControlToggled;
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v ToggleSwitch controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ControlEditValueChanged(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ToggleSwitch control && TryGetPaintData(control, out var paintData))
            {
                SetItemValue(paintData, control.EditValue, true);
            }
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v ToggleSwitch controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlToggled(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ToggleSwitch control && TryGetPaintData(control, out var paintData))
            {
                SetItemValue(paintData, control.IsOn, true);
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
        DxeEdit.ToggleSwitch __EditorPaint;
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
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);

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
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.MemoEdit, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <returns></returns>
        private DxeEdit.MemoEdit _CreateNativeControl(bool isInteractive)
        {
            var control = new DxeEdit.MemoEdit() { Location = new WinDraw.Point(25, -200) };
            control.ResetBackColor();
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.MemoEdit control, WinDraw.Rectangle controlBounds)
        {
            control.EditValue = GetItemValue(paintData);
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintData);
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.MemoEdit control)
        {
            control.EditValueChanged += _ControlEditValueChanged;
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v MemoEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ControlEditValueChanged(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.MemoEdit control && TryGetPaintData(control, out var paintData))
            {
                SetItemValue(paintData, control.EditValue, true);
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
        DxeEdit.MemoEdit __EditorPaint;
    }
    #endregion
    #region ComboListBox
    /// <summary>
    /// ComboListBox : <see cref="DxeEdit.ComboBoxEdit"/>
    /// </summary>
    internal class DxRepositoryEditorComboListBox : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        public DxRepositoryEditorComboListBox(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        public override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.ComboListBox; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);
            var buttons = this.GetItemContent<DxDData.TextBoxButtonProperties>(paintData, Data.DxDataFormProperty.TextBoxButtons, null);
            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor), buttons?.Key);
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
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.ComboBoxEdit, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <returns></returns>
        private DxeEdit.ComboBoxEdit _CreateNativeControl(bool isInteractive)
        {
            var control = new DxeEdit.ComboBoxEdit() { Location = new WinDraw.Point(25, -200) };
            control.ResetBackColor();
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.ComboBoxEdit control, WinDraw.Rectangle controlBounds)
        {
            _FillNativeComboItems(paintData, control);

            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintData);
        }
        /// <summary>
        /// Najde data pro položky ComboBoxu, vygeneruje je a naplní i aktuálně vybranou hodnotu.
        /// </summary>
        /// <param name="paintData"></param>
        /// <param name="control"></param>
        private void _FillNativeComboItems(IPaintItemData paintData, DxeEdit.ComboBoxEdit control)
        {
            var value = GetItemValue(paintData);
            int selectedIndex = -1;

            try
            {
                control.Properties.Items.BeginUpdate();
                control.Properties.Items.Clear();

                var comboItems = this.GetItemContent<DxDData.ImageComboBoxProperties>(paintData, Data.DxDataFormProperty.ComboBoxItems, null);
                if (comboItems != null)
                {
                    control.Properties.AllowDropDownWhenReadOnly = ConvertToDxBoolean(comboItems.AllowDropDownWhenReadOnly);
                    control.Properties.AutoComplete = comboItems.AutoComplete;
                    control.Properties.ImmediatePopup = comboItems.ImmediatePopup;
                    control.Properties.PopupSizeable = comboItems.PopupSizeable;
                    control.Properties.ShowDropDown = DxeCont.ShowDropDown.SingleClick;
                    control.Properties.ShowPopupShadow = true;
                    control.Properties.Sorted = false;

                    var dxComboItems = comboItems.CreateDxComboItems();
                    control.Properties.Items.AddRange(dxComboItems);

                    selectedIndex = comboItems.GetIndexOfValue(value);
                }
            }
            finally
            {
                control.Properties.Items.EndUpdate();
            }

            control.SelectedIndex = selectedIndex;
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.ComboBoxEdit control)
        {
            control.Properties.AllowMouseWheel = false;
            control.EditValueChanged += _NativeControlEditValueChanged;
            control.Properties.QueryPopUp += _NativeControlQueryPopUp;
            control.Properties.BeforePopup += _NativeControlBeforePopup;
            control.LostFocus += _NativeControlLostFocus;
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v ButtonEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _NativeControlEditValueChanged(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ComboBoxEdit control && TryGetPaintData(control, out var paintData))
            {
                var dxItem = control.SelectedItem;
                if (dxItem != null && dxItem is DxDData.ImageComboBoxProperties.Item item)
                {   // Je vybraná konkrétní položka?
                    SetItemValue(paintData, item.Value, true);
                }
                else
                {
                    var editValue = control.EditValue;

                }
            }
        }
        /// <summary>
        /// Eventhandler před pokusem o otevření Popup okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlQueryPopUp(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ComboBoxEdit control && TryGetPaintData(control, out var paintData))
            {
                if (paintData.LayoutItem.TryGetContent<WinDraw.Size>(DxDData.DxDataFormProperty.ComboPopupFormSize, out var size) && size.Width > 50 && size.Height > 50)
                    control.Properties.PopupFormSize = size;
            }
        }
        /// <summary>
        /// Eventhandler před otevřením Popup okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlBeforePopup(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;
        }
        /// <summary>
        /// Eventhandler po odchodu z prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlLostFocus(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ComboBoxEdit control && TryGetPaintData(control, out var paintData))
            {
                var size = control.GetPopupEditForm()?.Size;                   // Funguje   (na rozdíl od : control.Properties.PopupFormSize)
                if (size.HasValue && size.Value.Width > 50 && size.Value.Height > 50)
                    paintData.LayoutItem.SetContent<WinDraw.Size>(DxDData.DxDataFormProperty.ComboPopupFormSize, size.Value);
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
        DxeEdit.ComboBoxEdit __EditorPaint;
    }
    #endregion
    #region ImageComboListBox
    /// <summary>
    /// ImageComboListBox : <see cref="DxeEdit.ImageComboBoxEdit"/>
    /// </summary>
    internal class DxRepositoryEditorImageComboListBox : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        public DxRepositoryEditorImageComboListBox(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        public override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.ImageComboListBox; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);
            var buttons = this.GetItemContent<DxDData.TextBoxButtonProperties>(paintData, Data.DxDataFormProperty.TextBoxButtons, null);
            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor), buttons?.Key);
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
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.ImageComboBoxEdit, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <returns></returns>
        private DxeEdit.ImageComboBoxEdit _CreateNativeControl(bool isInteractive)
        {
            var control = new DxeEdit.ImageComboBoxEdit() { Location = new WinDraw.Point(25, -200) };
            control.ResetBackColor();
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.ImageComboBoxEdit control, WinDraw.Rectangle controlBounds)
        {
            if (dataPair != null)
            {
                dataPair.OriginalPaintData = paintData;
                dataPair.OriginalValue = GetItemValue(paintData);
            }

            _FillNativeComboItems(paintData, control);

            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintData);
        }
        /// <summary>
        /// Najde data pro položky ComboBoxu, vygeneruje je a naplní i aktuálně vybranou hodnotu.
        /// </summary>
        /// <param name="paintData"></param>
        /// <param name="control"></param>
        private void _FillNativeComboItems(IPaintItemData paintData, DxeEdit.ImageComboBoxEdit control)
        {
            var value = GetItemValue(paintData);
            int selectedIndex = -1;

            try
            {
                control.Properties.Items.BeginUpdate();
                control.Properties.Items.Clear();

                var comboItems = this.GetItemContent<DxDData.ImageComboBoxProperties>(paintData, Data.DxDataFormProperty.ComboBoxItems, null);
                if (comboItems != null)
                {
                    control.Properties.AllowDropDownWhenReadOnly = ConvertToDxBoolean(comboItems.AllowDropDownWhenReadOnly);
                    control.Properties.AutoComplete = comboItems.AutoComplete;
                    control.Properties.ImmediatePopup = comboItems.ImmediatePopup;
                    control.Properties.PopupSizeable = comboItems.PopupSizeable;
                    control.Properties.ShowDropDown = DxeCont.ShowDropDown.SingleClick;
                    control.Properties.ShowPopupShadow = true;
                    control.Properties.Sorted = false;

                    var dxComboItems = comboItems.CreateDxImageComboItems(out var imageList);
                    control.Properties.SmallImages = imageList;
                    control.Properties.Items.AddRange(dxComboItems);

                    selectedIndex = comboItems.GetIndexOfValue(value);
                }
            }
            finally
            {
                control.Properties.Items.EndUpdate();
            }

            control.SelectedIndex = selectedIndex;
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.ImageComboBoxEdit control)
        {
            control.Properties.AllowMouseWheel = false;
            control.CausesValidation = true;
            control.EditValueChanged += _ControlEditValueChanged;
            control.Properties.QueryPopUp += _NativeControlQueryPopUp;
            control.Properties.BeforePopup += _NativeControlBeforePopup;
            control.LostFocus += _NativeControlLostFocus;
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v ButtonEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ControlEditValueChanged(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ImageComboBoxEdit control && TryGetAttachedPair(control, out var dataPair) && dataPair.OriginalPaintData != null)
            {
                var dxItem = control.SelectedItem;
                if (dxItem != null && dxItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem dxImageItem)
                {   // Je vybraná konkrétní položka?
                    dataPair.CurrentValue = dxImageItem.Value;
                    if (!Object.Equals(dataPair.CurrentValue, dataPair.OriginalValue))
                    {
                        IPaintItemData paintData = dataPair.OriginalPaintData;
                        var actionInfo = new DataFormValueChangingInfo(paintData.Row, paintData.LayoutItem.ColumnName, DxDData.DxDataFormAction.ValueValidating, dataPair.OriginalValue, dataPair.CurrentValue);
                        this.DataForm.OnInteractiveAction(actionInfo);
                        if (actionInfo.Cancel)
                        { }
                        else
                        {
                            SetItemValue(dataPair.OriginalPaintData, dataPair.CurrentValue, true);
                            dataPair.OriginalValue = dataPair.CurrentValue;
                        }
                    }
                }
                else
                {
                    var editValue = control.EditValue;

                }
            }
        }
        /// <summary>
        /// Eventhandler před pokusem o otevření Popup okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlQueryPopUp(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ImageComboBoxEdit control && TryGetPaintData(control, out var paintData))
            {
                if (paintData.LayoutItem.TryGetContent<WinDraw.Size>(DxDData.DxDataFormProperty.ComboPopupFormSize, out var size) && size.Width > 50 && size.Height > 50)
                    control.Properties.PopupFormSize = size;
            }
        }
        /// <summary>
        /// Eventhandler před otevřením Popup okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlBeforePopup(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;
        }
        /// <summary>
        /// Eventhandler po odchodu z prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlLostFocus(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ImageComboBoxEdit control && TryGetPaintData(control, out var paintData))
            {
                var size = control.GetPopupEditForm()?.Size;                   // Funguje   (na rozdíl od : control.Properties.PopupFormSize)
                if (size.HasValue && size.Value.Width > 50 && size.Value.Height > 50)
                    paintData.LayoutItem.SetContent<WinDraw.Size>(DxDData.DxDataFormProperty.ComboPopupFormSize, size.Value);
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
        DxeEdit.ImageComboBoxEdit __EditorPaint;
    }
    #endregion
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
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            string text = GetItemLabel(paintData);
            var iconName = this.GetItemContent(paintData, Data.DxDataFormProperty.IconName, "");
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);

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
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.SimpleButton, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <returns></returns>
        private DxeEdit.SimpleButton _CreateNativeControl(bool isInteractive)
        {
            var control = new DxeEdit.SimpleButton() { Location = new WinDraw.Point(25, -200) };
            control.ResetBackColor();
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.SimpleButton control, WinDraw.Rectangle controlBounds)
        {
            control.Text = GetItemLabel(paintData);
            control.Size = controlBounds.Size;

            if (paintData.TryGetContent(DxDData.DxDataFormProperty.ButtonPaintStyle, out DxeCont.PaintStyles paintStyle))
                control.PaintStyle = paintStyle;

            var iconName = this.GetItemContent(paintData, Data.DxDataFormProperty.IconName, "");
            if (iconName != null)
            {
                DxComponent.ApplyImage(control.ImageOptions, iconName);
                control.ImageOptions.ImageToTextAlignment = DxeEdit.ImageAlignToText.LeftCenter;
            }
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.SimpleButton control)
        {
            control.Click += _ControlButtonClick;
        }
        /// <summary>
        /// Eventhandler po kliknutí na button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ControlButtonClick(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.SimpleButton control && TryGetPaintData(control, out var paintData))
            {
                var actionInfo = new DataFormActionInfo(paintData.Row, paintData.LayoutItem.ColumnName, DxDData.DxDataFormAction.ButtonClick);
                this.DataForm.OnInteractiveAction(actionInfo);
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
        DxeEdit.SimpleButton __EditorPaint;
    }
    #endregion
    #endregion
    #region Enumy pro layout, typicky kopie DevExpress (nebo podmnožina hodnot); konvertory jsou v DxRepositoryEditor (tam se konvertují do DevExpress enumu)
    /// <summary>
    /// Styl okraje
    /// </summary>
    public enum BorderStyle
    {
        NoBorder,
        Simple,
        Flat,
        HotFlat,
        UltraFlat,
        Style3D,
        Office2003,
        Default
    }
    public enum HorizontAlignment
    {
        Default,
        Near,
        Center,
        Far
    }

    #endregion
}
