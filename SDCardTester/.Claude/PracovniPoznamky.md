# SDCardTester – pracovní poznámky

Dokument pro navázání práce v další session. Popisuje **aktuální stav** (ne historii). Poslední aktualizace: **2026-11-19**.

## Projekt v kostce

- Projekt: `GraphLibrary\SDCardTester\SDCardTester.csproj` – WinForms, **.NET Framework 4.8**, C# 7.3 (bez `??=`, switch expressions apod.).
- Namespace: `DjSoft.Tools.SDCardTester` (+ `.Workers`), assembly `Djs.SDCardTester`.
- csproj má explicitní `<Compile Include>` → nové třídy přidávat do existujících souborů (nebo doplnit csproj).
- Komentáře a texty UI česky, XML doc komentáře u všech členů, kód členěn do `#region`.
- Konvence pojmenování: privátní fieldy `__Name`, privátní metody `_Name`, lokální funkce camelCase.
- Layout formulářů ručně (`OnResize` → `_DoLayout()`), ne `Anchor`/`Dock` – záměr autora.

### Build pro ověření (Claude)
- **Post-build event** podepisuje exe (`signtool`) a kopíruje výstup do `D:\Tools\SdCardTester\` → pro ověřovací build vypnout: `/p:PostBuildEvent=`.
- MSBuild: `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`.
- Build v kopii projektu `%TEMP%\frb_sdct` (robocopy `/MIR` bez `bin`, `obj`, `.vs`, `.Claude`). Scratchpad Claude je pro MSBuild příliš dlouhá cesta (MAX_PATH).
- `git` není v PATH.
- Vykreslení controlů bez spuštění aplikace: PowerShell skript načte exe přes reflexi, naplní `SummaryInfo` vzorovými daty a zavolá `DrawToBitmap` (osvědčilo se pro kontrolu layoutu Summary bloku).

## Architektura (relevantní části)

### Pracovní akce (`Workers\BackWorker.cs`)
- `BackWorker<TTarget>`: `StartAction` → `Task.Factory.StartNew(Run)`; události `WorkingStep` (throttling `WorkingStepTime` = 333 ms) a `WorkingDone`.
- Stav `RunState` (None / Run / Pause / Stop); `Stopping` v pauze čeká a zastaví `Stopwatch` (pauza se nepočítá do časů).
- **Pravidlo:** `Run()` musí vždy skončit `CallWorkingDone()` (ve `finally`), jinak GUI zůstane ve stavu Working. Výjimka uvnitř `Task` se nikde neprojeví.

### MainForm (`Forms\MainForm.cs`)
- `ShowControls(ActionState, withDataPanel)` – přepíná Toolbar (akční buttony / FlowControl Pause-Run-Stop), panely `DriveInfoPanel` / `ResultsInfoPanel`, TaskBar progress a titulek.
  - Start pracovního stavu: `_SetRunState(RunState.Run, true, false)` = jen ikonky, **bez akcí** (setter `RunState` by podle dosud nezměněného `CurrentState` vrátil okno do Dialog).
  - Konec: `withDataPanel = false` → panel výsledků zůstane zobrazen.
- Vzor akce (Analýza, testy, FileRescue): start = `ResultsInfoPanelClear()` + `ShowControls(state, true)` + vložit controly (potomci `WorkingResultControl`) + `Start()`; průběh = refresh přes `BeginInvoke`; konec = závěrečný refresh + `ShowControls(Dialog, false)`.
- Refresh přes `BeginInvoke` je asynchronní → data číst v GUI akci jako **jeden konzistentní snímek** (`GetSummary()`, `GetCurrentProgress()`).
- TaskBar: `_TaskProgressMaximum` / `_TaskProgressValue` / `_TaskProgressState` (třída `TaskProgress`), titulek `AppTitleTextCurrent`.
  - Formát titulku: **procenta na začátku** (Windows zkracují text zprava), zaokrouhleno **dolů**: `42% File Rescue`, `58% SD Card Write H:` / `SD Card Verify` / `SD Card Read`. Analýza titulek nemění.

### Nastavení (`Application\App.cs`)
- Singleton `App` s dvoufázovou inicializací: konstruktor jen `__Settings = new AppSettings()`; `Initialize()` (volá getter `Instance` pod zámkem) → `__Settings.Load()`.
- `AppSettings` (ne `Settings` – kolize s `Properties.Settings`), soubor `LocalApplicationData\DjSoft\SDCardTester\Settings.xml`:
  ```xml
  <Settings Version="1">
    <Value Name="LastFileRescueInputFileNames">a.txt
  b.txt</Value>
    <Value Name="LastFileRescueOutputFilesPath">G:\out</Value>
  </Settings>
  ```
  - Hodnoty v `Dictionary<string,string>`, klíč přes `[CallerMemberName]`. **Nová hodnota = jeden řádek:** `public string Xxx { get { return _GetValue(); } set { _SetValue(value); } }`.
  - Setter ukládá ihned, jen při skutečné změně (ordinálně, včetně null). Víceřádkové texty: v XML jen LF, getter vrací CRLF.
  - `Load()` pod `__IsLoading` přímo do slovníku; chybějící/poškozený soubor = výchozí hodnoty.
  - `Save()` pod `__Lock`: zápis `.tmp` → Delete → Move; výjimku zachytí, `App.ShowError()` až **po uvolnění zámku** (jinak deadlock přes `Invoke`).

### FileRescue (`Workers\FileRescue.cs`)
- `FileRescue : BackWorker<FileRescueInputInfo>` – záchrana souborů z poškozeného média po blocích.
- Čtení v threadu Work, zápis v threadu **Writter** (`WritterRequestInfo`, semafory). Nečitelný blok se do cíle zapíše jako nuly a pokračuje se.
- Datové třídy: `SingleFileInfo` (soubor, `FinalStatistic`, `ErrorMessage`), `SingleBlockInfo` (blok, `BlockStatus`, `ErrorsCount`, časy), `SingleLogInfo` (mapa bloků, `.rescuelog` pro navázání po přerušení, `ErrorLevelProcess`: 0 = 1. průchod, 1+ = opakování), `StatisticInfo`.
- Stav bloku: `None` (čte se), `OK`, `OKAfterErrors`, `WithError`, `ErrorAborted`; `MaxErrorsCount` = 3. **Chybný = jen `WithError` + `ErrorAborted`** (`IsErrorBlock`, `BadBlocksCount`).
- Cílový soubor s nečitelnými bloky se přejmenuje na `jméno~E{BadBlocksCount}.ext`; soubor přečtený až po opakování příponu nedostane.
- Vstup (`_PrepareFileList`): řádky = soubory nebo wildcard s plnou cestou (`X:\Dir\*.ts`). Chybný wildcard (neexistující adresář, relativní cesta) → `App.ShowError` a pokračuje se dalšími řádky. Výchozí výstup při prázdném adresáři: `Desktop\FildeRescue` (sic).
- `Run()`: příprava seznamu i smyčka souborů jsou v `try` s `catch` (`ShowError`) a `finally` (zafixuje čas, ukončí Writter, `CallWorkingDone`). Chyba jednoho souboru → `SingleFileInfo.ErrorMessage` + `ShowError`, pokračuje se dalším.
- Simulace pro testování: `TestAddBadBlocks`, `TestAddSimulatedWaiting` (nastavují se jen v kódu, formulář je nemění).
- Data pro GUI:
  - `GetCurrentProgress(out fileLength, out blocks)` – mapa aktuálního souboru (pro `LinearMapControl`).
  - `GetSummary()` → `SummaryInfo` (soubory, index aktuálního, bajty celkem/zpracováno, výsledky souborů `FileResult`: Pending, Ok, WithErrors, Interrupted, NotExists, Failed; aktuální soubor, fáze, `StatisticInfo`; čas akce bez pauz).
- Barvy bloků: `Skin.RescueBlock*Color` (Reading, Ok, OkAfterErrors, Error1, Error2, Aborted) ve všech paletách (`Helpers.cs`); `SingleBlockInfo.GetCategory/GetColor/GetCategoryText/GetToolTipText`.

### GUI FileRescue
- `FileRescueInputWindow` (konec `FileRescue.cs`): MemoEdit vstupních souborů, výstupní adresář + `FolderBrowserDialog`, OK / Storno (Esc, Enter mimo Memo). Data přes `InputData`, přenos do prvků v `OnVisibleChanged`. Předvyplnění a uložení přes `App.Settings` v `MainForm._GetFileRescueInfo()`.
- Mapa: `LinearMapControl` s barvami bloků a ToolTipem (ToolTip platí obecně, i pro mapu Analýzy).
- Summary blok v `ResultsInfoPanel` (konec `FileRescue.cs`, předek `FileRescueSummaryControl`):
  - `FileRescueTotalControl`: soubor X z N + čas, název souboru (celá cesta zdroje a cíle v ToolTipu), progress přes všechny soubory podle bajtů, počty výsledků.
  - `FileRescueFileControl`: fáze (1. průchod / opakování N z 3), zpracováno, rychlost čtení, 6 řádků podle kategorie bloků = zároveň **legenda barev mapy**.
- TaskBar (`_FileRescueTaskProgressFill`): hodnota = `ProcessedRatio`; stav `Paused` při pauze, `Error` pokud je nějaký soubor (i aktuální) s nečitelnými bloky / selhaný, jinak `Normal`. Při 1. průchodu může zčervenat a po úspěšném opakování zezelenat – **záměr, potvrzeno autorem**. Pause/Run volá refresh okamžitě (v pauze nechodí `WorkingStep`).

## Rozhodnutí autora (neměnit bez dotazu)
- Summary blok **bez seznamu souborů** (byl by dlouhý; přehled souborů řeší Windows Explorer).
- **Žádný odhad zbývajícího času**, ani v budoucnu.
- Chybné bloky = jen `WithError` + `ErrorAborted`.
- TaskBar může během záchrany měnit barvu Error ↔ Normal podle aktuálního stavu.
- Zaokrouhlení procent v titulku dolů.
- Opakované chybové hlášky při selhání ukládání Settings a ztráta Settings při pádu mezi Delete a Move – akceptováno.
- Double-checked locking singletonu `App` (teoretický souběh během `Initialize()`) – akceptováno.

## Otevřené body
1. **Neověřeno za běhu:**
   - zamčený zdrojový soubor (oprava NRE ve `finally` v `_CopySingleFile` – zavření streamu jen když není null);
   - chybný wildcard (oprava 2026-11-19: hláška a pokračování dalšími řádky, akce řádně skončí).
2. **Čtení obsahu (`ContentRead`)**: levý panel zobrazuje 4 fáze testovacích souborů, akce ale běží ve fázi `ReadAnyContent`, pro kterou control není → panel se možná nemění. Zjistit, zda je to záměr.
3. Varování kompilátoru v `FileRescue.cs`: CS4014 (`SaveLogFileAsync()` bez `await`), CS0168 (nepoužité proměnné výjimek); CS0168 i v `LinearMapControl.cs`. Neškodné.

## Tipy pro možný rozvoj

- **Viditelnost malých chybných bloků v mapě** (`LinearMapControl`): mapa má cca 760 px × 90 řádků ≈ 68 000 px, blok 128 kB → u souborů nad ~8 GB je víc bloků než pixelů. Blok užší než 1 px se nevykreslí (`LineInfo.GetInnerBounds` vrátí null), při více blocích na pixelu vyhraje poslední kreslený.
  - Návrh: v `MainForm` předávat bloky seřazené tak, aby chybné byly kresleny až po OK (pozice se nemění, blok má vlastní `Start`); v `LinearMapControl` minimální šířka 1 px jen pro prvky s volitelným rozhraním (např. `ILinearMapControlItemMinWidth`), aby se nezměnilo chování `DriveAnalyser.FileGroup`.
- **Validace vstupu už ve formuláři** `FileRescueInputWindow`: při OK zkontrolovat existenci souborů / adresářů wildcardů a zapisovatelnost výstupního adresáře, chyby ukázat dřív, než akce začne (dnes se hlásí až z threadu Work).
- **Souhrnná hláška nebo log po skončení**: jeden řádek za soubor (OK / `~E{n}` / nenalezeno / chyba) do textového souboru ve výstupním adresáři; nahradí seznam souborů v GUI, který autor nechce.
- **Další položky `AppSettings`**: stačí jeden řádek property (viz výše). Kandidáti: vybraná paleta `Skin`, poslední disk, velikost a pozice okna, `MaxErrorsCount` / velikost bloku pro FileRescue.
- **Parametry záchrany do formuláře**: `MaxErrorsCount`, velikost bloku, případně zaškrtávátka `TestAddBadBlocks` / `TestAddSimulatedWaiting` jen v Debug buildu.
- **Sjednocení obsluhy akcí v `MainForm`**: Analýza, testy a FileRescue mají stejný vzor (Prepare / Step / Done / RunPauseStop). Šlo by vytáhnout společnou pomocnou metodu nebo rozhraní, aby se chyby typu „Toolbar se vrátí do Dialog“ neopakovaly per akce.
- **`BackWorker.Run` obecně**: vzor `try / catch(ShowError) / finally(CallWorkingDone)` přenést do bázové třídy (např. `Run` → `RunCore`), aby žádný potomek nemohl nechat GUI ve stavu Working.
- **TaskBar i pro Analýzu disku**: Analýza dnes nemění titulek ani progress v TaskBaru; šlo by doplnit stejně jako u FileRescue (`% Analyse H:`).
