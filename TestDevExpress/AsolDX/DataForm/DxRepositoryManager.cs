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
    internal class DxRepositoryManager : IDisposable
    {
        #region Konstruktor a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataForm"></param>
        internal DxRepositoryManager(DxDataForm dataForm)
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
        /// Datový základ DataFormu
        /// </summary>
        internal DxDataForm DataForm { get { return __DataForm; } } private DxDataForm __DataForm;
        /// <summary>
        /// Vizuální control <see cref="DxDataFormPanel"/> = virtuální hostitel obsahující Scrollbary a <see cref="DxDataFormContentPanel"/>
        /// </summary>
        internal DxDataFormPanel DataFormPanel { get { return __DataForm?.DataFormPanel; } }
        /// <summary>
        /// Panel obsahující data Dataformu
        /// </summary>
        internal DxDataFormContentPanel DataFormContent { get { return __DataForm?.DataFormContent; } }
        /// <summary>
        /// Zajistí vykreslení obsahu Dataformu
        /// </summary>
        internal void DataFormDraw() { DataFormContent?.Draw(); }
        #endregion
        #region Public hodnoty a služby
        /// <summary>
        /// Formát bitmap, který se ukládá do cache. Čte se z DataFormu: <see cref="DxDataForm.CacheImageFormat"/>
        /// </summary>
        internal WinDraw.Imaging.ImageFormat CacheImageFormat { get { return DataForm.CacheImageFormat; } }
        /// <summary>
        /// Invaliduje data v repozitory.
        /// Volá se po změně skinu a zoomu, protože poté je třeba nově vygenerovat.
        /// Po invalidaci bude navazující kreslení trvat o kousek déle, protože všechny vykreslovací prvky se budou znovu OnDemand generovat, a bude se postupně plnit cache.
        /// Ale o žádná dat se nepřijde, tady nejsou data jen nářadí...
        /// </summary>
        internal void InvalidateManager()
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
        internal void PaintItem(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds, bool isDisplayed)
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
        internal void ChangeItemInteractiveState(IPaintItemData paintData, WinForm.Control container, WinDraw.Rectangle controlBounds)
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
        internal bool TryGetCacheImageData(string key, out byte[] imageData)
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
        internal bool TryGetCacheImageData(string key, out byte[] imageData, out ulong id)
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
        internal bool TryGetCacheImageData(ulong id, out byte[] imageData)
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
        internal ulong AddImageDataToCache(string key, byte[] imageData)
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
    #region DxRepositoryEditor : abstraktní předek konkrétních editorů; obsahuje Factory pro tvorbu konkrétních potomků a spoustu výkonného kódu
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
        internal DxRepositoryEditor(DxRepositoryManager repositoryManager)
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
        /// Datový základ DataFormu
        /// </summary>
        internal DxDataForm DataForm { get { return __RepositoryManager.DataForm; } }
        /// <summary>
        /// Vizuální control <see cref="DxDataFormPanel"/> = virtuální hostitel obsahující Scrollbary a <see cref="DxDataFormContentPanel"/>
        /// </summary>
        internal DxDataFormPanel DataFormPanel { get { return __RepositoryManager?.DataFormPanel; } }
        /// <summary>
        /// Panel obsahující data Dataformu
        /// </summary>
        internal DxDataFormContentPanel DataFormContent { get { return __RepositoryManager.DataFormContent; } }
        /// <summary>
        /// Formát bitmap, který se ukládá do cache. Čte se z DataFormu: <see cref="DxDataForm.CacheImageFormat"/>
        /// (přes <see cref="DxRepositoryManager.CacheImageFormat"/>)
        /// </summary>
        protected virtual WinDraw.Imaging.ImageFormat CacheImageFormat { get { return RepositoryManager.CacheImageFormat; } }
        /// <summary>
        /// Typ editoru
        /// </summary>
        internal abstract DxRepositoryEditorType EditorType { get; }
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
        #region Podpora pro práci s nativním controlem, zpracování nízkoúrovňových eventů Controlu (Focus, Mouse, KeyDown)
        /// <summary>
        /// Je voláno po změně interaktivního stavu dané buňky. Možná bude třeba pro buňku vytvořit a umístit nativní Control, anebo bude možno jej odebrat...
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="container">Fyzický container, na němž má být přítomný fyzický Control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        internal virtual void ChangeItemInteractiveState(IPaintItemData paintData, WinForm.Control container, WinDraw.Rectangle controlBounds)
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
        #region Předání událostí (Mouse, Focus, RightClick, KeyDown) z Nativního controlu (z ControlDataPair) do datového objektu PaintData
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
        /// Obsluha události po kliknutí RightClick myši z nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        private void _RunNativeControlMouseRightClickUp(ControlDataPair dataPair)
        {
            if (dataPair.PaintData is null) return;
            var actionInfo = new DataFormMouseActionInfo(dataPair.PaintData as DxDData.DataFormCell, DxDData.DxDataFormAction.RightClick, WinForm.Control.ModifierKeys, dataPair.MouseDownButtons ?? WinForm.MouseButtons.Right, dataPair.MouseDownLoaction ?? WinForm.Control.MousePosition);
            this.DataForm.OnInteractiveAction(actionInfo);
            OnNativeControlMouseRightClickUp(dataPair);
        }
        /// <summary>
        /// Obsluha události při DoubleClicku myši na nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        private void _RunNativeControlMouseDoubleClick(ControlDataPair dataPair)
        {
            if (dataPair.PaintData is null) return;
            var actionInfo = new DataFormActionInfo(dataPair.PaintData as DxDData.DataFormCell, DxDData.DxDataFormAction.DoubleClick);
            this.DataForm.OnInteractiveAction(actionInfo);
            OnNativeControlMouseDoubleClick(dataPair);
        }
        /// <summary>
        /// Obsluha události po vstupu focusu do nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        private void _RunNativeControlFocusEnter(ControlDataPair dataPair)
        {
            if (dataPair.PaintData is null) return;
            dataPair.OriginalPaintData = dataPair.PaintData;
            if (dataPair.PaintData.InteractiveState.HasFlag(DxInteractiveState.HasFocus)) return;  // Zdejší metoda už nemusí měnit stav, nejspíš to stihla předešlá metoda

            // Přidat příznak HasFocus:
            DxInteractiveState maskHasFocus = DxInteractiveState.HasFocus;
            dataPair.PaintData.InteractiveState |= maskHasFocus;
            OnNativeControlFocusEnter(dataPair);

            this.DataForm.OnInteractiveAction(new DataFormActionInfo(dataPair.PaintData as DxDData.DataFormCell, DxDData.DxDataFormAction.GotFocus));
        }
        /// <summary>
        /// Nativní event controlu: někdo zmáčkl klávesu
        /// </summary>
        /// <param name="dataPair"></param>
        /// <param name="keyArgs"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _RunNativeControlKeyDown(ControlDataPair dataPair, WinForm.KeyEventArgs keyArgs)
        {
            if (dataPair.PaintData is null) return;
            if (!this.NeedProcessKeyDown(dataPair, keyArgs) && DataForm.NeedTraceKeyDown(keyArgs.KeyData))
            {   // Konkrétní buňka i DataForm chce dostávat informaci o dané stisknuté klávese:
                var actionInfo = new DataFormKeyActionInfo(dataPair.PaintData as DxDData.DataFormCell, DxDData.DxDataFormAction.KeyDown, keyArgs);
                this.DataForm.OnInteractiveAction(actionInfo);
            }
            OnNativeControlKeyDown(dataPair, keyArgs);
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

            this.DataForm.OnInteractiveAction(new DataFormActionInfo(dataPair.OriginalPaintData as DxDData.DataFormCell, DxDData.DxDataFormAction.LostFocus));

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
        /// Je voláno po kliknutí RightClick myši z nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        protected virtual void OnNativeControlMouseRightClickUp(ControlDataPair dataPair) { }
        /// <summary>
        /// Je voláno při DoubleClicku myši na nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        protected virtual void OnNativeControlMouseDoubleClick(ControlDataPair dataPair) { }
        /// <summary>
        /// Je voláno po vstupu focusu do nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        protected virtual void OnNativeControlFocusEnter(ControlDataPair dataPair) { }
        /// <summary>
        /// Je voláno po stisku klávesy v nativním controlu
        /// </summary>
        /// <param name="dataPair"></param>
        /// <param name="keyArgs"></param>
        protected virtual void OnNativeControlKeyDown(ControlDataPair dataPair, WinForm.KeyEventArgs keyArgs) { }
        /// <summary>
        /// Je voláno po odchodu focusu z nativního controlu
        /// </summary>
        /// <param name="dataPair"></param>
        protected virtual void OnNativeControlFocusLeave(ControlDataPair dataPair) { }
        /// <summary>
        /// Potomek v této metodě dostává informaci o stisknuté klávese, a může ovlivnit, zda tuto klávesu dostane DataForm k vyhodnocení, zda jde o klávesu posouvající Focus.
        /// Pokud potomek zde vrátí true, pak on sám bude zpracovávat tuto klávesu a nechce, aby ji dostal někdo jiný.
        /// Typicky EditBox chce zpracovat klávesu Enter (= nový řádek v textu) i kurzorové klávesy nahoru/dolů. 
        /// Stejně tak CheckBox chce zpracovat klávesu Enter (= změna stavu Checked).
        /// <para/>
        /// Bázová třída vrací false = všechny běžné klávesy mohou být vyhodnoceny v DataFormu a mohou přemístit Focus.
        /// </summary>
        /// <param name="dataPair"></param>
        /// <param name="keyArgs"></param>
        /// <returns></returns>
        protected virtual bool NeedProcessKeyDown(ControlDataPair dataPair, WinForm.KeyEventArgs keyArgs) { return false; }
        #endregion
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
            internal ControlDataPair(DxRepositoryEditor editor, string name)
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
            internal WinForm.Control NativeControl
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
            internal IPaintItemData PaintData { get; set; }
            /// <summary>
            /// Interaktivní prvek, kerý je nositelem editované hodnoty.
            /// S ohledem na proces LostFocus a Validation (kdy nejprve probíhá LostFocus, v jeho rámci pak zdejší <see cref="DetachPaintData()"/> kdy se nuluje <see cref="PaintData"/>)
            /// se v procesu validace nelze spolehnout na objekt v <see cref="PaintData"/> (ten už je null). Proto validace pracuje s <see cref="OriginalPaintData"/>.
            /// Hodnotu do <see cref="OriginalPaintData"/> vkládá konkrétní editor společně v té chvíli, kdy ukládá originál editované hodnoty do <see cref="OriginalValue"/>.
            /// </summary>
            internal IPaintItemData OriginalPaintData { get; set; }
            /// <summary>
            /// Hodnota buňky, která byla do nativního controlu <see cref="NativeControl"/> vložena v době plnění controlu v <see cref="DxRepositoryEditor.FillNativeControl(ControlDataPair, Rectangle)"/>.<br/>
            /// Pokud při opouštění buňky se výsledná hodnota v controlu liší od této uložené hodnoty, pak se hlásí změna hodnoty pomocí akce.
            /// </summary>
            internal object OriginalValue { get; set; }
            /// <summary>
            /// Hodnota buňky, která je v nativním controlu <see cref="NativeControl"/> přítomna v průběhu editace a po dokončení editace.
            /// Pokud při opouštění buňky se tato výsledná hodnota v controlu liší od hodnoty uložené v <see cref="OriginalValue"/>, pak se hlásí změna hodnoty pomocí akce.
            /// </summary>
            internal object CurrentValue { get; set; }
            /// <summary>
            /// Má můj prvek Focus?
            /// </summary>
            internal bool HasFocus { get { return __HasFocus; } }
            /// <summary>
            /// Souřadnice, na kterou jsme nativní control původně umístili. Nemusí být shodná s <see cref="WinForm.Control.Bounds"/>, a to ani okamžitě po prvním setování.
            /// </summary>
            internal WinDraw.Rectangle? ControlBounds { get; set; }
            /// <summary>
            /// Stisknuté buttony myši při události MouseDown
            /// </summary>
            internal WinForm.MouseButtons? MouseDownButtons { get { return __MouseDownButtons; } }
            /// <summary>
            /// Pozice myši absolutní při události MouseDown
            /// </summary>
            internal WinDraw.Point? MouseDownLoaction { get { return __MouseDownLoaction; } }
            /// <summary>
            /// Zapojí dodaný datový prvek jako nynějšího vlastníka zdejšího <see cref="NativeControl"/>
            /// </summary>
            /// <param name="paintData">Data konkrétního prvku</param>
            internal void AttachPaintData(IPaintItemData paintData)
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
            internal void DetachPaintData()
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
            internal void ReleaseAll()
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
                    control.MouseDown += _NativeControlMouseDown;
                    control.MouseUp += _NativeControlMouseUp;
                    control.DoubleClick += _NativeControlDoubleClick;
                    control.MouseLeave += _NativeControlMouseLeave;
                    control.Enter += _NativeControlEnter;
                    control.PreviewKeyDown += _NativeControlPreviewKeyDown;
                    control.KeyDown += _NativeControlKeyDown;
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
                    control.MouseEnter -= _NativeControlMouseEnter;
                    control.MouseMove -= _NativeControlMouseMove;
                    control.MouseDown -= _NativeControlMouseDown;
                    control.MouseUp -= _NativeControlMouseUp;
                    control.DoubleClick -= _NativeControlDoubleClick;
                    control.MouseLeave -= _NativeControlMouseLeave;
                    control.Enter -= _NativeControlEnter;
                    control.KeyDown -= _NativeControlKeyDown;
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
            /// Nativní event controlu: myš stiskla button
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            /// <exception cref="NotImplementedException"></exception>
            private void _NativeControlMouseDown(object sender, WinForm.MouseEventArgs e)
            {
                __MouseDownButtons = e.Button;
                __MouseDownLoaction = WinForm.Control.MousePosition;
            }
            /// <summary>
            /// Nativní event controlu: myš uvolnila button
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            /// <exception cref="NotImplementedException"></exception>
            private void _NativeControlMouseUp(object sender, WinForm.MouseEventArgs e)
            {
                if (__MouseDownButtons.HasValue && __MouseDownButtons.Value == WinForm.MouseButtons.Right)
                    __Editor._RunNativeControlMouseRightClickUp(this);
            }
            /// <summary>
            /// Nativní event controlu: myš dala DoubleClick
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            /// <exception cref="NotImplementedException"></exception>
            private void _NativeControlDoubleClick(object sender, EventArgs e)
            {
                __Editor._RunNativeControlMouseDoubleClick(this);
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
            /// Nativní event controlu: vyhodnotit klávesu KeyDown jestli nás zajímá
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void _NativeControlPreviewKeyDown(object sender, WinForm.PreviewKeyDownEventArgs e)
            {
                if (__Editor.DataForm.NeedTraceKeyDown(e.KeyData))
                    e.IsInputKey = true;
            }
            /// <summary>
            /// Nativní event controlu: někdo zmáčkl klávesu
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            /// <exception cref="NotImplementedException"></exception>
            private void _NativeControlKeyDown(object sender, WinForm.KeyEventArgs e)
            {
                __Editor._RunNativeControlKeyDown(this, e);
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
            /// <summary>
            /// Stisknuté buttony myši při události MouseDown
            /// </summary>
            private WinForm.MouseButtons? __MouseDownButtons;
            /// <summary>
            /// Pozice myši absolutní při události MouseDown
            /// </summary>
            private WinDraw.Point? __MouseDownLoaction;
            #endregion
        }
        #endregion
        #region Vykreslování obrazu prvku, tvorba obrazu z Controlu, ukládání a čtení obrazu z ImageCache v rámci DxRepositoryManager, tvorba klíče pro obraz v Cache
        /// <summary>
        /// Potomek zde vykreslí prvek svého typu podle dodaných dat do dané grafiky a prostoru
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        internal void PaintItem(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds, bool isDisplayed)
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
        /// <param name="graphics"></param>
        /// <param name="exactBitmapSize"></param>
        /// <returns></returns>
        protected virtual byte[] CreateBitmapData(WinForm.Control control, WinDraw.Graphics graphics, WinDraw.Size? exactBitmapSize = null, WinDraw.Color? fillBackColor = null)
        {
            int w = exactBitmapSize?.Width ?? control.Width;
            int h = exactBitmapSize?.Height ?? control.Height;
            using (var bitmap = new WinDraw.Bitmap(w, h, WinDraw.Imaging.PixelFormat.Format32bppArgb /* graphics */))
            {
                if (fillBackColor.HasValue) bitmap.Save(@"c:\DavidPrac\CheckBox0.png", WinDraw.Imaging.ImageFormat.Png);
                bitmap.MakeTransparent();
                if (fillBackColor.HasValue) bitmap.FillColor(fillBackColor.Value);
                if (fillBackColor.HasValue) bitmap.Save(@"c:\DavidPrac\CheckBox1.png", WinDraw.Imaging.ImageFormat.Png);
                control.DrawToBitmap(bitmap, new WinDraw.Rectangle(0, 0, w, h));
                if (fillBackColor.HasValue) bitmap.Save(@"c:\DavidPrac\CheckBox2.png", WinDraw.Imaging.ImageFormat.Png);
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
        #endregion
        #region Čtení a zápis hodnot z/do prvku IPaintItemData, konverze typů DevExpress
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
                var actionInfo = new DataFormValueChangedInfo(paintData as DxDData.DataFormCell, DxDData.DxDataFormAction.ValueChanged, oldValue, value);
                this.DataForm.OnInteractiveAction(actionInfo);
            }
        }
        /// <summary>
        /// Metoda slouží pro potomky, kdy před zahájením editace - typicky v metodě <see cref="FillNativeControl(ControlDataPair, Rectangle)"/> ukládáme
        /// data buňky <paramref name="paintData"/> uložíme do do dodaného páru <paramref name="dataPair"/> do <see cref="ControlDataPair.OriginalPaintData"/>;
        /// a dodanou hodnotu <paramref name="originalValue"/> (načtenou z datové buňky) uložíme do <see cref="ControlDataPair.OriginalValue"/>.
        /// Pokud dodaný pár <paramref name="dataPair"/> je null, je to validní stav a nic neřešíme.
        /// </summary>
        /// <param name="dataPair"></param>
        /// <param name="paintData"></param>
        /// <param name="originalValue"></param>
        protected void StorePairOriginalData(ControlDataPair dataPair, IPaintItemData paintData, object originalValue)
        {
            if (dataPair != null)
            {
                dataPair.OriginalPaintData = paintData;
                dataPair.OriginalValue = originalValue;
            }
        }
        /// <summary>
        /// Metoda slouží pro potomky, kdy uvnitř procesu editace (typicky v eventu EditValueChanged) uloží aktuální rozeditovanou hodnotu (dodanou jako <paramref name="currentValue"/>)
        /// do <see cref="ControlDataPair.CurrentValue"/>.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="currentValue"></param>
        protected void TryStorePairCurrentEditingValue(WinForm.Control control, object currentValue)
        {
            if (TryGetAttachedPair(control, out var dataPair))
            {   // Toto je průběžně volaný event, v procesu editace, a nemá valného významu:
                dataPair.CurrentValue = currentValue;
            }
        }
        /// <summary>
        /// Metoda slouží pro potomky, kdy při dokončení editace (LostFocus, nebo Validating...) máme nově editovanou hodnotu <paramref name="validatedValue"/>
        /// odeslat do dataformu k validaci: <see cref="DxDataFormPanel.OnInteractiveAction(DataFormActionInfo)"/> s akcí <see cref="DxDData.DxDataFormAction.ValueValidating"/>.
        /// Pokud nebude nastaveno <see cref="DataFormValueChangingInfo.Cancel"/>, pak se nová hodnota uloží do buňky a vrátí true.
        /// Pokud bude Cancel = true, pak se neuloží, a vrací se false.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="validatedValue"></param>
        /// <param name="forceValidating"></param>
        /// <param name="cancelInfo"></param>
        /// <returns></returns>
        protected bool TryStoreValidatingValue(WinForm.Control control, object validatedValue, bool forceValidating, out DataFormValueChangingInfo cancelInfo)
        {
            if (TryGetAttachedPair(control, out var dataPair) && dataPair.OriginalPaintData != null)
            {   // Toto je event volaný při ukončení editace
                dataPair.CurrentValue = validatedValue;
                if (forceValidating || !Object.Equals(dataPair.CurrentValue, dataPair.OriginalValue))
                {
                    IPaintItemData paintData = dataPair.OriginalPaintData;
                    var actionInfo = new DataFormValueChangingInfo(paintData as DxDData.DataFormCell, DxDData.DxDataFormAction.ValueValidating, dataPair.OriginalValue, dataPair.CurrentValue);
                    this.DataForm.OnInteractiveAction(actionInfo);
                    if (actionInfo.Cancel)
                    {   // Validace vrátila false: nebudu ukládat data, předám Info a vrátím false => volající si zajistí vrácení dat...:
                        cancelInfo = actionInfo;
                        return false;
                    }
                    else
                    {   // Změna dat a z validace nepřišlo Cancel => uložím nová data:
                        SetItemValue(dataPair.OriginalPaintData, dataPair.CurrentValue, true);
                        dataPair.OriginalValue = dataPair.CurrentValue;
                        paintData.InvalidateCache();
                    }
                }
            }
            // Cancel není, data jsou [nezměněna | uložena] => vracíme true:
            cancelInfo = null;
            return true;
        }
        /// <summary>
        /// Pro daný <paramref name="control"/> dohledá buňku dataformu a do dataformu odešle zadanou akci.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="action"></param>
        protected void RunDataFormAction(WinForm.Control control, DxDData.DxDataFormAction action)
        {
            if (TryGetAttachedPair(control, out var dataPair) && dataPair.OriginalPaintData != null)
            {
                var actionInfo = new DataFormActionInfo(dataPair.OriginalPaintData as DxDData.DataFormCell, action);
                this.DataForm.OnInteractiveAction(actionInfo);
            }
        }
        /// <summary>
        /// Pro daný <paramref name="control"/> dohledá buňku dataformu a do dataformu odešle zadanou akci a název prvku.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="action"></param>
        /// <param name="itemName"></param>
        protected void RunDataFormAction(WinForm.Control control, DxDData.DxDataFormAction action, string itemName)
        {
            if (TryGetAttachedPair(control, out var dataPair) && dataPair.OriginalPaintData != null)
            {
                var actionInfo = new DataFormItemNameInfo(dataPair.OriginalPaintData as DxDData.DataFormCell, action, itemName);
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
        /// Z prvku přečte a vrátí hodnotu <see cref="Data.DxDataFormProperty.CheckBoxBorderStyle"/> převedenou na <see cref="DxeCont.BorderStyles"/>
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
    }
    #endregion
    #region Enumy - pro repository a pro layout, typicky kopie DevExpress (nebo podmnožina hodnot); konvertory jsou v DxRepositoryEditor (tam se konvertují do DevExpress enumu)
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
    #region interface IPaintItemData : přepis pro prvek, který může být vykreslen
    /// <summary>
    /// <see cref="IPaintItemData"/> : přepis pro prvek, který může být vykreslen v metodě <see cref="DxRepositoryManager.PaintItem(IPaintItemData, PaintDataEventArgs, WinDraw.Rectangle, bool)"/>
    /// </summary>
    internal interface IPaintItemData
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
}
