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
- Automatický refresh disků po připojení výměnného disku nastaví Toolbar jako při startu (`InitDrives()`, včetně přepnutí na „jen výměnné“). Ruční volbu uživatele záměrně neodlišujeme od výchozího stavu.
- Vestavěná čtečka karet (výměna karty bez `WM_DEVICECHANGE`) se automaticky neobnovuje – cíleně neřešeno, viz „Reakce na změnu disků v systému“.

## Otevřené body
**UZAVŘENO (2026-11-20) – příčina: nestabilní MicroSD karta, viz VÝSLEDEK níže.** Kontrola 2. Stop u `Dobrodružství rabína Jákoba, Luis de Funes.ts` (zdroj `h:\Records\...`).
- Log OK dle očekávání: 8 657 bloků, všechny `OK`, všechny s WriteTime (žádný záporný), mapa bez mezer, konec = 1 134 690 304 B = délka cíle. Blok 616 431 616 (dříve nezapsaný) má nové časy → při navázání opraven.
- **ALE porovnání cíle se zdrojem (prvních 1 134 690 304 B) našlo 2 rozdíly:**
  - blok 21 757 952: 1 255 rozdílných bajtů v rozsahu 21 801 984 – 21 806 076 (~4 kB), v cíli jen 15 z nich nulových;
  - blok 616 300 544: 946 rozdílných bajtů v rozsahu 616 382 464 – 616 385 528 (~3 kB), v cíli 2 nulové. Je to blok těsně před dříve chybějícím blokem 616 431 616 (konec 1. běhu).
  - Zdroj čtený 2× po sobě je stabilní (shodný). Rozdíly nejsou nuly = nejde o díru, ale o jiný obsah (~3–4 kB uvnitř bloku, zarovnáno zhruba na 4 kB stránky?).
- Další krok: zjistit příčinu – podezření: (a) souběh bufferu: `Content` bloku vs. zápis ve Writteru (buffer znovu použit / přepsán před dokončením zápisu?), (b) zápis přes `FileStream` bez Flush při Stop / nezavřený stream z 1. běhu (blok 616 300 544 = předposlední blok 1. běhu, 1. běh byl starou verzí s nezavřeným streamem → mohl se zapsat neúplný buffer při finalizaci), (c) blok 21 757 952 – podívat se do zálohy / historie, zda leží na hranici některého dřívějšího přerušení. Porovnat ReadTime/WriteTime těchto bloků v logu a zda je zdroj (`h:`) čten z vadného média (opakované čtení dává stejná data – ověřeno pro 20 MB úsek).
- Pomocné: porovnání po 4 MB úsecích trvá ~2 min (1,1 GB).

**VÝSLEDEK (2026-11-20, pokračování): příčinou je zdrojová MicroSD karta (H:), ne FileRescue.**
- Časy čtení v logu: oba rozdílné bloky byly přečteny „okamžitě“ (21 757 952: **23 µs**, 616 300 544: **26 µs**), jejich sousedé ~120–290 ms (medián celého souboru 2,5 ms, p99 275 ms, max 549 ms). 128 kB za 23 µs = data z cache Windows (read-ahead při čtení předchozího pomalého bloku), ne přímé čtení.
- Opakované čtení zdroje **bez cache** (`FileOptions` 0x20000000 = NO_BUFFERING), blok 21 757 952, 6×: čtení 1 = shoda s cílem; čtení 2–6 se liší od cíle **i navzájem** (1 281–1 961 B v rozsahu offsetů 44 032–52 222 = dvě 4 kB stránky). Bajty se liší jednotlivými bity (např. `04`↔`00`, `49`↔`4F`, `88`↔`08`). Karta tedy vrací při každém čtení jiná data **bez chyby I/O**.
- Blok 616 300 544 se při kontrole (cached čtení) se zdrojem už shodoval – zdroj „mění“ obsah i tam.
- Během testu karta **přestala odpovídat** („Bylo zadáno zařízení, které neexistuje“) a disk H: zmizel ze systému → karta je vážně nestabilní.
- Závěr: FileRescue zapsal to, co karta v tu chvíli vrátila; žádná chyba kopírování, Writteru ani navázání. Tichá chyba čtení se dnes nedá odhalit, protože karta nehlásí I/O chybu.

**Možné rozšíření FileRescue (nápad, nerealizováno – rozhodne autor):**
- Čtení zdroje bez cache Windows (`FileOptions` NO_BUFFERING, bloky zarovnané na sektor – 128 kB je OK; pozor na zarovnání bufferu) → měřené časy budou skutečné a nebude se kopírovat „read-ahead“.
- Kontrolní čtení: každý blok přečíst 2× a porovnat; při neshodě číst N× (např. 5×) a sestavit výsledek **hlasováním po bajtech** (nejčastější hodnota), blok označit novým stavem např. „Nestabilní“ (vlastní barva v mapě, počet v Summary, přípona `~U{n}` nebo zápis v logu). Zpomalí čtení ~2×, proto volitelně (parametr ve formuláři / Settings).
- Ověřovací průchod po dokončení: znovu přečíst zdroj a porovnat s cílem, rozdílné bloky označit a znovu zpracovat.

**ODPOJENÍ ZDROJOVÉ KARTY – REALIZOVÁNA VARIANTA A (2026-11-20, rozhodnutí autora).** **Ověřeno autorem s reálnou kartou 2026-11-21:** detekce funguje, hláška s návodem přijde poměrně rychle. Logy: `Pes baskervillský.ts` (3,76 %) a `Pozor, vizita.ts` (0,29 %) – oba `BadBlocksCount 0`, `DeviceLostCount 1`, `ProblemRuns 1` → nepřečtené bloky správně nejsou chybné, soubory půjdou příště na konec fronty. **Znovu potvrzeno autorem (2026-11-21):** po odpojení zápis do logu + MessageBox; další spuštění začne následujícím souborem (soubor s odpojením je přeřazen na konec fronty, nepřeskočen trvale).
- Požadavek autora: uživatel dostane informaci; zapisovaný soubor nesmí skončit ve stavu „vše ztraceno“; po vyjmutí karty, ~10 s čekání a znovuzapojení lze často pokračovat bez chyb.
- Implementace (`Workers\FileRescue.cs`, region „Ztráta zdrojového zařízení (odpojená karta)“ uvnitř „Privátní řízení běhu“):
  - `__SourceDeviceLost`, `_IsSourceDeviceLostError(exc)` (kódy Win32 z `HResult`: 2, 3, 6, 15, 21, 55, 433, 1006, 1110, 1112, 1167, 1617; `DriveNotFound`/`DirectoryNotFound`/`FileNotFound`; jinak `!_IsSourceAvailable(true)` = `DriveInfo.IsReady` + `File.Exists`).
  - `_CopySingleBlock`: při chybě zařízení se blok **nepočítá jako chybný** (nic se nezapíše, `ErrorsCount` beze změny, blok zůstane `None`), nastaví se `__SourceDeviceLost`.
  - Série chyb: po každých 8 chybách čtení za sebou (`ConsecutiveErrorsCheckCount`) se čeká až 3 s (`ConsecutiveErrorsWaitMilisecs`), zda Windows zdroj neodpojí; úspěšné čtení počítadlo nuluje. Bloky chybné před odpojením mají 1 chybu (`WithError`) a při navázání se zkusí znovu.
  - Čtení 0 bajtů uvnitř souboru = výjimka (dříve `Empty` = OK bez zápisu → díra vedená jako OK). `CopyBlockResult.Empty` se už nepoužívá.
  - `_CopySingleFile`: po ztrátě zdroje `break` → `isComplete = false` → bez `~E`, bez data/atributů; selhání `File.Open` zdroje také kontroluje ztrátu zařízení.
  - `_PrepareSingleFile`: zdrojový soubor neexistuje a není dostupný ani disk → ztráta zdroje, soubor se nevykazuje jako „Nenalezeno“ (stav `None` = čeká).
  - `Run()`: po ztrátě zdroje se nezpracovávají další soubory; po `CallWorkingDone()` `App.ShowWarning` „Zdroj odpojen“ s postupem (vyjmout kartu, ~10 s, připojit, ověřit písmeno disku, spustit znovu se stejnými parametry).
  - Summary: `SummaryInfo.SourceDeviceLost`; Total control „Přerušeno: zdroj odpojen“, File control „Zdroj odpojen – soubor nedokončen“; soubor ve výsledcích jako „Přerušeno“.
- Test (`scratchpad\test_devicelost.ps1`, zdroj přes junction adresáře, junction smazána během kopírování → `File.Exists` false; simulovaná chyba čtení na 12 345 678):
  - kódy: `ERROR_DEV_NOT_EXIST` → ztráta zařízení, `ERROR_CRC` (zdroj dostupný) → chyba bloku;
  - po odpojení: `SourceDeviceLost`, 95 bloků, **0 chybných**, blok 12 320 768 = `None` bez chyby, žádný `~E`, cíl s původním jménem, výsledek Přerušeno;
  - po obnovení junction a novém spuštění: SHA256 cíle = zdroj.
- Neřešeno (možné rozšíření): varianta B (automatické čekání na připojení), volba „Znovu zkusit vzdané bloky“ pro dříve poškozené logy.

**Původní analýza (2026-11-20):**
- Co se stane při odpojení (`_CopySingleBlock`, catch): `Read` vyhodí `IOException` („zařízení neexistuje“) → blok se bere jako **chybný blok média**: zapíše se jako nuly, `ErrorsCount++`, stav `WithError`. Otevřený `sourceStream` je po odpojení mrtvý (handle se neobnoví ani po připojení karty) → **každý další blok selže okamžitě**.
  - 1. průchod tak během chvíle označí celý zbytek souboru `WithError`, opakování (úrovně 1, 2) proběhnou stejně rychle → po 3 chybách **`ErrorAborted` = definitivně vzdáno**.
  - Smyčka skončí „normálně“ (`isComplete`) → `_CopySingleAttributes` přejmenuje cíl na `jméno~E{tisíce}.ts`.
  - Další soubory: `CheckFile()` → zdroj neexistuje → `NotExists`, přeskočí se (to je v pořádku).
- Nové spuštění po připojení karty: log obsahuje zbytek souboru jako `ErrorAborted` (`IsDone`) → **nic se už nezkusí**, soubor je vykázán jako hotový s chybami a cíl má původní jméno přejmenované na `~E` → data jsou ztracena, dokud uživatel ručně nesmaže log. Pokud Stop přišel dřív, než bloky dosáhly 3 chyb (`WithError`), navázání je zkusí znovu – ale jen náhodou.
- Riziko navíc: kdyby `Read` po odpojení vrátil 0 bajtů (místo výjimky), blok se uloží jako `Empty` = **OK bez zápisu** (díra vedená jako OK). Uvnitř délky souboru by `Empty` nemělo nastat → bezpečnější brát jako chybu.
- Kontrola existujících logů v `g:\Cdisk\FileRescueTestTarget` (2026-11-20): všechny 100 % bez chyb, jen „Dobrodružství rabína Jákoba“ 26,42 % bez chyb → žádný log zatím není poškozen.
- **Návrh řešení:**
  1. **Rozlišit chybu zařízení od chyby bloku:** po výjimce čtení ověřit dostupnost zdroje (`File.Exists`, případně `DriveInfo.IsReady`) a/nebo podle `HResult` (`ERROR_DEV_NOT_EXIST` 55, `ERROR_NOT_READY` 21, `ERROR_FILE_NOT_FOUND`/`PATH_NOT_FOUND` 2/3, `ERROR_DEVICE_REMOVED` 1617). Chyba zařízení **se nepočítá jako chyba bloku**: blok zůstane `None`, nic se nezapíše, `ErrorsCount` se nemění.
  2. **Reakce na ztrátu zařízení:** varianta A – soubor i celý běh ukončit (uložit log, zavřít cíl, nepřejmenovávat), v Summary/hlášce „Zdrojové zařízení se odpojilo“; po připojení karty uživatel spustí znovu a navázání (hledání „děr“) pokračuje. Varianta B – automaticky čekat (stav podobný Pauze, text „Čekám na připojení zdroje“), periodicky testovat zdroj, po připojení znovu otevřít `sourceStream` a pokračovat; Stop funguje jako dnes.
  3. **Pojistka „série chyb“:** N po sobě jdoucích chyb (např. 8) → ověřit zdroj jako v bodě 1; i když zařízení „existuje“, ale neodpovídá, přejít do stavu z bodu 2 místo masového označování bloků.
  4. `Empty` uvnitř délky souboru brát jako chybu (ne OK).
  5. Volitelně záchrana dříve poškozených logů: volba „Znovu zkusit vzdané bloky“ (při načtení logu `ErrorAborted`/`WithError` → `ErrorsCount = 0`, `None`).

**TRVALE NEČITELNÝ SOUBOR – REALIZOVÁNY VARIANTY 1 a 2 (2026-11-20, rozhodnutí autora).** Ověřeno automatickým testem, v aplikaci zatím neověřeno.
- Problém: soubor, který nejde přečíst a na zdroji ho nelze smazat ani přejmenovat, při každém opakování blokoval ostatní soubory (seřazeno podle jména; při odpojení karty se běh ukončí právě na něm).
- **Varianta 1 – vynechání ve vstupu:** řádek začínající `!` = maska souborů k vynechání (wildcards `*` `?`, bez ohledu na velikost písmen). Maska s adresářem (`\`, `:`) se porovnává s celou cestou, maska bez adresáře jen se jménem souboru (`!*real*` tedy nevyřadí vše v adresáři „real“). Řádky `!` mohou být kdekoli ve vstupu. Prázdné řádky (i jen mezery) se ignorují, duplicitní soubory se zpracují jen jednou. Při přechodu na jinou kartu autor řádky `!` jednoduše smaže.
  - Popis nad vstupním polem `FileRescueInputWindow` (text autora, víceřádkový Label): „Vstupní soubory: / Zadejte adresář + názvy souborů nebo wildcards; / Zadejte více možností na samostatné řádky; / Pro vynechání souborů zadejte jako první znak vykřičník / Prázdné řádky budou ignorovány“. Layout počítá výšky obou Labelů zvlášť (`inputLabelHeight`, `outputLabelHeight`); ověřeno vykreslením okna.
- **Varianta 2 – automatické odložení na konec fronty:** hlavička logu má nové řádky `DeviceLostCount:` a `ProblemRuns:` (`SingleLogInfo.DeviceLostCount`, `ProblemRuns`; načítají se v `LoadLogFile`, zapisují v `SaveLogFile`; starý log bez nich = 0).
  - `_CopySingleFile` ve `finally`: při ztrátě zdroje `DeviceLostCount++`; pokud soubor není dokončen a v tomto běhu se odpojil zdroj nebo byly chyby čtení (`__CurrentFileReadErrors > 0`, typicky Stop u beznadějného souboru), `ProblemRuns++`.
  - `_PrepareFileList` → `_SortFilesByProblems`: z hlavičky každého existujícího logu (`SingleLogInfo.ReadProblemCounters`, čte jen hlavičku) zjistí součet problémů; pořadí = bez problémů podle jména, pak problémové podle počtu problémů vzestupně a jména.
  - Bez automatického úplného vynechání po N odpojeních (navrženo jako volitelné, zatím ne – autor má ruční `!`).
- Test (`scratchpad\test_exclude_defer.ps1`): vynechání `!a2.bin` + `!<dir>\b*` → zůstane jen a1, duplicita a prázdné řádky OK, `!*real*` nevyřadí soubory v adresáři „real“; x.bin (odpojení zdroje) + y.bin: 1. běh x→odpojení, log x `DeviceLostCount 1, ProblemRuns 1`, y nezpracován; 2. běh pořadí **y, x**, oba OK a shodné se zdrojem (SHA256).

**ODKLÁDÁNÍ NEČITELNÝCH SOUBORŮ (2026-11-20/21, návrh odsouhlasen autorem).** Ověřeno automatickým testem, v aplikaci zatím neověřeno.
- Podnět: `Hercule Poirot - Rodinné sídlo…ts` – všech 4 976 přečtených bloků (15 %) chybných od 1. bloku, karta zůstala připojená, chyby hlásil i Windows. Běh trval 2 190 s, z toho ~31 min čekání 3 s po každých 8 chybách (kontrola odpojení karty).
- **Oprava čekání:** kontrola odpojení po sérii chyb proběhne po 8, 16, 32, 64… chybách (`__NextDeviceCheckAt` se zdvojnásobí, po úspěšném čtení zpět na 8); při právě odkládaném souboru se nekontroluje.
- **Kritérium (konstanty ve `FileRescue`, region „Odkládání nečitelných souborů“):** `DeferWindowBlocks = 256` (posuvné okno 32 MB), `DeferMinBlocks = 64` (vyhodnocovat od 8 MB přečtených v tomto běhu), `DeferErrorRatio = 0.5`, `DeferConsecutiveErrors = 128`. Odložit, pokud chybovost v okně ≥ 50 % nebo 128 chyb za sebou. Jen v 1. vlně (`__DeferEnabled`) a v 1. průchodu souboru (`ErrorLevelProcess == 0`); okno počítá jen bloky čtené v tomto běhu (`_DeferWindowAdd` v `_CopySingleBlock`).
- **Odložení:** smyčka v `_CopySingleFile` skončí (`__CurrentFileDefer`), bez `~E`/data/atributů, chybné bloky `WithError` (1), `ProblemRuns++` (soubor příště na konec fronty), `SingleFileInfo.IsDeferred = true` → `FileResult.Deferred`.
- **Po 1. vlně** (ne po Stop, ne po odpojení zdroje): `_AskForSecondWave` – `App.ShowQuestion` (nové, Ano/Ne) se seznamem odložených souborů (max. 15 + „… a dalších N“, u každého zpracováno % a chybných %), dotaz „Mám nyní zkusit opakovaný pokus o čtení?“. Během dotazu je časomíra zastavena. **Ano** = 2. vlna (`_RunFiles(deferred)`, `__DeferEnabled = false`, plné zpracování vč. opakování chybných bloků). **Ne** = konec; ruční nový běh: odložené soubory jsou na konci fronty, kritérium platí znovu (~8 MB čtení), pak znovu dotaz (odsouhlaseno autorem).
- `Run()` rozdělen: `_RunFiles(files)` = jedna vlna.
- **Summary:** `SummaryInfo.FilesDeferred`, `CurrentFileDeferred`, `IsSecondWave`. Total control nový 5. řádek **„Počet nečitelných souborů: N“** (= jen odložené, rozhodnutí autora), při N > 0 Bold + `Skin.RescueBlockAbortedColor` (`PaintRow` má nový volitelný parametr barvy), `OptimalHeight` = 5 řádků. File control fáze „Nečitelný soubor – odložen“, ve 2. vlně prefix „Opakovaný pokus: “. TaskBar: `FilesDeferred > 0` = Error.
- Test (`scratchpad\test_defer.ps1`, reflexe bez dialogu, 3 soubory po 10 MB, b_bad simulovaně nečitelný celý): 1. vlna 10 s – b_bad odložen po 64 blocích (8 MB), ostatní OK, `ProblemRuns: 1`, žádný `~E`; 2. vlna 16 s – b_bad zpracován celý → `b_bad~E81.bin`, Odloženo 0. Summary vykresleno (červený tučný řádek). Dialog `App.ShowQuestion` testován jen překladem.
- Pozn.: odložení stále trvá ~9 s navíc (kontroly odpojení po 8, 16, 32 chybách × 3 s); případně zkrátit.

**Zamrzlá karta – hlídání operací se zdrojem (2026-11-21, IMPLEMENTOVÁNO, ověřeno simulací, v aplikaci zatím ne).**
- Případ z praxe (Pes baskervillský, 14:04): karta zamrzla, Windows ji neodpojily; `Read` visel neomezeně (i `DriveInfo('H').IsReady` a výpis PnP disků visely), Stop nefungoval, protože se testuje jen mezi bloky. Pozn.: šlo stále o 1. vlnu – soubory s problémy jsou na konci fronty (řazení podle `DeviceLostCount + ProblemRuns`), znovu odložené soubory měly ProblemRuns 1 → 2.
- Řešení `_SourceIo<T>(io, disposeLateResult, timeoutSeconds)`: operace se zdrojem běží v samostatném vlákně (Task LongRunning), pracovní vlákno čeká po 100 ms, obnovuje GUI (`CallWorkingStepWhenTime`), a operaci **opustí** po Stop + `SourceStopAbandonSeconds = 3` nebo po časovém limitu → `__SourceHung = true`, `SourceHungException` (potomek IOException). Visící vlákno zůstane na pozadí do vyjmutí karty; stream uvolní až pokračování po jeho dokončení (`_CloseSourceStream`, `__AbandonedSourceIo`), otevřený stream z opuštěného `File.Open` se uvolní sám (`disposeLateResult`).
- Hlídáno: čtení bloku (Seek+Read), `File.Open` zdroje, `SingleFileInfo.CheckFile`, `_PrepareFileList` (prochází adresáře zdroje), `_IsSourceAvailable` (limit `SourceAvailableTimeoutSeconds = 5` s; neodpověď = nedostupný).
- Zpracování jako ztráta zdroje (`__SourceDeviceLost`): blok zůstane None, ErrorsCount beze změny, `DeviceLostCount++`, `ProblemRuns++`, běh končí. Závěrečné hlášení „Zdroj neodpovídá“ s postupem (vyjmout kartu, 10 s, připojit, spustit znovu).
- Summary: `SummaryInfo.SourceNotRespondingSeconds` (operace visí ≥ `SourceHangWarningSeconds = 20` s) → fáze souboru červeně „Zdroj neodpovídá N s – vyjměte kartu“, TaskBar Error; po skončení `SourceHung` → „Přerušeno: zdroj neodpovídá“ / „Zdroj neodpovídá – soubor nedokončen“.
- Test `SingleFileInfo.SimulatedHangPosition` (čtení bloku s pozicí usne navždy). `scratchpad\test_hang.ps1`: 3 soubory po 10 MB, b_hang zamrzne v bloku 40 → po 26 s Summary hlásí 26 s neodpovídá; Stop → běh skončil za 3,1 s; log b_hang: 40 OK + 1 None, BadBlocks 0, DeviceLostCount 1, ProblemRuns 1; data 40 bloků shodná se zdrojem; c_good nezpracován. Regresní `test_defer.ps1` beze změny (10 s / 16 s).
- Režie: jedno předání do vlákna na blok (desítky µs), proti čtení 128 kB z karty zanedbatelné.

**Barvení OK bloků podle rychlosti čtení – navrženo 2026-11-20, IMPLEMENTOVÁNO 2026-11-21 (ověřeno renderem, v aplikaci zatím ne).**
- Analýza logů (OK bloky s ReadTime):
  - `…After the funeral.ts1` 9 834 bloků: medián 11,9 ms, p10 13 µs, p90 81 ms, p99 377 ms, max 920 ms; 48 % bloků < 300 µs (z cache).
  - `Den pro mou lásku.ts` 27 004 bl.: medián 58 µs, p90 67 ms, max 840 ms; 53 % z cache.
  - `ABBA - Koncert.ts` 18 416 bl.: 56 % z cache.
  - Vzor „dlouhý čas – desítky µs“ potvrzen (např. 105621, 18, 51058, 18, …): fyzické čtení 256 kB, druhá polovina z cache. Zarovnání páru se liší podle souboru (rychlý blok převážně na sudé, nebo liché pozici) → **čas jednotlivého bloku nemá samostatně smysl**.
  - Rychlost v okně (Den pro mou lásku): okno 2 bloky stále zkreslené (p95 3 125 MB/s – posunutý pár); **okno 8–16 bloků stabilní**: 16 bl. (2 MB) p1 0,9 / p5 1,7 / medián 7,9 / p95 11,4 / p99 16,7 MB/s. Rozsah p1–p99 ≈ 1,3 dekády; pomalé úseky (~1 MB/s) jsou reálná informace o slabých místech karty.
- Návrh:
  1. **Metrika = propustnost okna, ne průměr časů:** pro OK blok `v = Σ bajtů / Σ ReadTime` přes centrované okno ±8 bloků (16 bloků = 2 MB, konstanta), jen OK/OKAfterErrors bloky s časem; cache bloky se tím přirozeně započtou do páru. Počítá se v GUI threadu při refreshi z pole bloků (prefix součty, O(n)), uloží se do bloku (`SpeedMBs`), log se nemění.
  2. **Adaptivní rozsah:** histogram log10(v) (např. 120 košů 0,01 MB/s – 10 GB/s); dolní mez = p2, horní = p98 z dosavadních hodnot aktuálního souboru (robustní k výkyvům); minimální šíře rozsahu 1 dekáda (×10) kolem mediánu, aby při stálé rychlosti nevznikal falešný kontrast.
  3. **Logaritmická škála:** `t = (log v − log lo) / (log hi − log lo)`, oříznout 0..1; kvantovat na 6–8 odstínů (stabilnější obraz, čitelná legenda).
  4. **Barvy:** v každé paletě `Skin` dvě nové barvy `RescueBlockOkSlowColor` (tmavá, tlumená zelená) a `RescueBlockOkFastColor` (jasná zelená), interpolace (nejlépe v HSL – světlost/sytost, odstín zelený); `OKAfterErrors` zůstává žlutozelená a musí být odlišitelná.
  5. **Legenda/ToolTip:** v Summary řádek OK doplnit gradientem s mezemi „1,2 – 11 MB/s“; ToolTip bloku uvede rychlost okna (MB/s).
  6. Volitelně přepínač „Barvit podle rychlosti“ (zap./vyp.), uložený v `AppSettings`.
- **Rozhodnutí autora (2026-11-20):** pomalé = tmavé, 6–8 odstínů (kvantováno), okno 16 bloků (2 MB), legenda s gradientem a mezemi MB/s v Summary **i** přepínač „Barvit podle rychlosti“ (uložit v `AppSettings`).
- **Implementace (2026-11-21, na pokyn autora během běžícího testu):**
  - `FileRescue.SpeedShading` (statická, vnořená): konstanty `WindowBlocks = 16`, `WindowMinBlocks = 4`, `ShadesCount = 7`, `MinSamples = 32`, `LowPercentile = 0,02`, `HighPercentile = 0,98`, `MinRangeDecades = 1`; histogram 120 košů log10 0,01 MB/s – 10 GB/s. `Apply(blocks, enabled)` setřídí kopii podle `BlockStart`, prefix součty bajtů/času/počtu jen OK + OKAfterErrors bloků s časem, okno centrované (u krajů posunuté), uloží `SingleBlockInfo.SpeedMBs` a `SpeedShade` (jen při zapnutí a platném rozsahu), vrátí `FileRescue.SpeedRange` (Enabled, IsValid, LowMBs, HighMBs). `GetShadeColor(i)` = lineární RGB interpolace Slow → Fast, `FormatSpeed`.
  - `SingleBlockInfo.GetColor()` instanční: kategorie Ok + `SpeedShade` → odstín, jinak barva kategorie (OKAfterErrors a chyby beze změny). ToolTip bloku: „Rychlost čtení v okolí: X MB/s“; řádky „Čtení“ a „Zápis“ (časy jednotlivého bloku) odebrány na přání autora (2026-11-21), časy zůstávají v logu. Autor ověřil barevnou škálu za běhu (jiná karta): „vypadá moc pěkně“.
  - `Skin`: `RescueBlockOkSlowColor` / `RescueBlockOkFastColor` ve všech 3 paletách (Light 25,90,35 → 120,230,125).
  - `AppSettings.FileRescueSpeedShading` (bool, výchozí true, uloženo "1"/"0").
  - `MainForm._FileRescuerRefresh`: před vyplněním Summary volá `SpeedShading.Apply` na snímek bloků a předá `SpeedRange` do `FileRescueFileControl`. `_FileRescueSpeedShadingToggled`: přepne nastavení, přepočítá bloky z `LinearMapControl.MapItems` (funguje i po skončení běhu), refresh mapy a Summary.
  - `FileRescueFileControl`: u „Přečteno“ místo čtverečku pruh odstínů; nový řádek „Rychlost: [pruh 7 odstínů] lo – hi MB/s“ (nebo „zatím málo dat“); řádek s kreslenou zaškrtávací volbou „Barvit podle rychlosti“ (klik → event `SpeedShadingToggled`, kurzor ruka). `OptimalHeight` = 11 řádků + 8.
  - Ověřeno renderem z reálných logů (`scratchpad\render_speed.ps1`): Tajemná zahrada 14 165 bl., Apply 6 ms, rozsah 1,1 – 24 MB/s; Den pro mou lásku 27 004 bl., 20 ms, 1,2 – 12 MB/s. Pomalé úseky jsou v mapě zřetelné jako tmavé pruhy.

**TODO – Konfigurace parametrů odkládání (autor 2026-11-20):** hodnoty `DeferWindowBlocks`, `DeferMinBlocks`, `DeferErrorRatio`, `DeferConsecutiveErrors` (případně i `ConsecutiveErrorsCheckCount`, `ConsecutiveErrorsWaitMilisecs`, `MaxErrorsCount`) přesunout z konstant do konfigurace (`AppSettings` + formulář / dialog nastavení).

**TODO – Varianta 3 (odloženo autorem 2026-11-20):** tlačítko „Přeskočit soubor“ v Toolbaru za běhu: ukončí jen aktuální soubor (uloží log, bez `~E`, výsledek „Přeskočeno“), zapíše `ProblemRuns++` a pokračuje dalším souborem. Vyžaduje nový stav vedle Run/Pause/Stop v `BackWorker` / `MainForm` (`ShowControls`, `ShowFlowButtonsEnabled`, `_SetRunState`).

**TODO (odloženo autorem 2026-11-20):** Oprava měření času zápisu ve Writteru – sdílený `Stopwatch` (Work volá v `Stopping` Stop/Start, Writter současně čte `CurrentTime`) dává občas záporný / přehnaný `WriteTime`; blok se záporným časem se pak při navázání zbytečně přečte znovu (data v pořádku). Řešení: ve Writteru měřit přes `Stopwatch.GetTimestamp()`. Detail v sekci „Ověření autorem v aplikaci (2026-11-20)“.

0. **Stop a navázání FileRescue** – opraveno 2026-11-20, ověřeno automatickým testem; v běžící aplikaci autor zatím neověřil (viz sekce níže).
   - Zbývá volitelně: před přeskočením hotového souboru ověřit existenci a délku cíle (dnes jen podle logu).
1. **Neověřeno za běhu:**
   - zamčený zdrojový soubor (oprava NRE ve `finally` v `_CopySingleFile` – zavření streamu jen když není null);
   - chybný wildcard (oprava 2026-11-19: hláška a pokračování dalšími řádky, akce řádně skončí).
2. **Čtení obsahu (`ContentRead`)**: levý panel zobrazuje 4 fáze testovacích souborů, akce ale běží ve fázi `ReadAnyContent`, pro kterou control není → panel se možná nemění. Zjistit, zda je to záměr.
3. Varování kompilátoru v `FileRescue.cs`: CS4014 (`SaveLogFileAsync()` bez `await`), CS0168 (nepoužité proměnné výjimek); CS0168 i v `LinearMapControl.cs`. Neškodné.

## Reakce na změnu disků v systému – PŘIDÁNO 2026-11-24, ověřeno autorem v aplikaci pro USB čtečku (odpojení i připojení jednotky)
- Cíl: po připojení nebo odpojení výměnného disku (USB čtečka, flash disk) se Toolbar sám nastaví tak, jako by aplikace startovala s aktuální sadou disků.
- `MainForm.WndProc` (region Windows Taskbar Progress, už existoval kvůli TaskProgress) volá `_DrivesDeviceChange` pro `WM_DEVICECHANGE` (0x0219).
  - Bere jen `DBT_DEVICEARRIVAL` (0x8000) a `DBT_DEVICEREMOVECOMPLETE` (0x8004) s `DBT_DEVTYP_VOLUME` (2). Písmena se čtou z `dbcv_unitmask`.
  - Kdyby jednotka poslala zprávu s příznakem média (`dbcv_flags & DBTF_MEDIA`), zpracuje se stejně. Příznak slouží jen pro text ve StatusBaru. Vestavěný slot ji ale neposílá (viz cíleně neřešený bod níže).
- Region „Změna disků v systému“:
  - `_DrivesChangedNotify`: text ve `StatusInfoLabel` („Připojen disk H:“, „Odpojen disk H:“…) + `WatchTimer.CallMeAfter(_DrivesChangedRefresh, delay, true, __DrivesChangedTimerId)`.
    - Prodleva: připojení `DrivesArrivalDelayMiliseconds` = 2500 ms (nový disk potřebuje čas, než je `IsReady`), odpojení `DrivesRemovalDelayMiliseconds` = 1000 ms (návrh autora 2026-11-24).
    - Budík je jeden společný (debounce přes Guid): každá další zpráva ho posune a platí prodleva podle poslední zprávy. Vrácené Guid se ukládá, protože jednorázový budík se po proběhnutí odebere.
  - `_DrivesChangedRefresh` (GUI): běží-li akce, nastaví jen `__DeviceWasChanged` (refresh proběhne v `ShowControls` při přechodu Working → Dialog, pokud se nezavírá okno). Jinak `InitDrives()` = stav jako při startu, záměrně včetně přepnutí na „jen výměnné“ (autor: výběr uživatele se od výchozího stavu neodlišuje).
  - Ochrana výsledků: při `__DrivesAutoRefresh` a zobrazeném panelu výsledků (`CurrentDataPanelState` pracovní) `ToolDriveCombo_SelectedIndexChanged` nic nedělá, takže panel výsledků i mapa zůstanou a obnoví se jen combo.
- Test `test_devchange.ps1` (simulované zprávy přes WndProc):
  - 3 zprávy během 300 ms, poslední připojení → po 2,1 s nic, po 2,9 s jeden refresh (stejné timer Guid);
  - poslední zpráva odpojení → po 0,7 s nic, po 1,4 s jeden refresh;
  - jiný typ zařízení nebo jiná událost → nic;
  - panel výsledků zůstal;
  - během akce jen příznak, refresh po skončení akce.
- **CÍLENĚ NEŘEŠENO (rozhodnutí autora 2026-11-24): vestavěná čtečka karet (slot v notebooku).**
  - Písmeno jednotky je v systému trvale, mění se jen připravenost média. Windows při vložení nebo vyjmutí karty nepošlou `WM_DEVICECHANGE` (ověřeno autorem breakpointem ve `WndProc`, přestože Windows vložení zaznamenaly: notifikace „S jednotkou M jsou problémy“ = nečistý souborový systém na kartě).
  - Broadcast s `DBTF_MEDIA` posílají spolehlivě jen CD/DVD mechaniky. Čtečky karet posílají jen `GUID_IO_MEDIA_ARRIVAL` / `GUID_IO_VOLUME_MOUNT`, a to oknům registrovaným přes `RegisterDeviceNotification` na otevřený handle jednotky.
  - Zvažované varianty:
    - (A) registrace na handle jednotky: držený handle blokuje „Bezpečně odebrat“, hodně Win32 kódu, nejisté u konkrétního ovladače;
    - (B) periodické sledování `IsReady` výměnných jednotek přes `WatchTimer.CallMeEvery` na pozadí, mimo běh akcí.
  - Obojí zamítnuto jako zbytečně mnoho práce pro malý efekt. U vestavěného slotu stačí po výměně karty ručně kliknout na tlačítko Obnovit disky.

## Zavření okna během běhu akce – PŘIDÁNO 2026-11-22, ověřeno autorem v aplikaci (dotaz, po Ano rychlé dokončení a zavření)
- Dříve: proces skončil okamžitě (všechna vlákna jsou background). Data v cíli a protokol zůstaly konzistentní, protože blok je OK až po Write; chyběla ale finální uložení (log, ZdraviMedia, přejmenování `~E`).
- Nyní (`MainForm`, region „Zavření okna během běhu akce“), platí pro všechny pracovní stavy (`_IsWorkingState`):
  - UserClosing: dotaz „Zastavit a ukončit aplikaci?“ (výchozí Ne). Po Ano `_CloseStopStart` nastaví `__CloseAfterStop`, `App.IsClosing`, text ve StatusBaru (`StatusInfoLabel`) a `RunState = Stop`. Zavření se zruší a `ShowControls` při přechodu Working → Dialog zavolá `BeginInvoke(Close)`.
  - Opakované kliknutí na křížek během čekání: „Ukončit okamžitě bez dokončení?“ (výchozí Ne).
  - Jiný CloseReason (vypnutí Windows, Správce úloh): Stop a synchronní čekání nejvýš 5 s (`_CloseWaitForStop`, čeká na vynulování `_FileRescuer`/`_DriveAnalyser`/`_DriveTester`), pak zavřít.
- `App.IsClosing`: `ShowInfo`/`ShowWarning`/`ShowError` nic nezobrazí, `ShowQuestion` vrací false. Nastavuje se při Stopu kvůli zavření i při každém skutečném zavření okna.
- Test `test_close.ps1` (skutečné FileRescue 400 MB, konfigurace zálohovaná a vrácená):
  - Stop při zavření: okno zavřeno za ~95 ms, log 2541 bloků OK = 333 053 952 B = délka cíle, ZdraviMedia uložen;
  - WindowsShutDown: Cancel=false za 250 ms, log a cíl konzistentní;
  - bez běhu: zavře se hned.
  - Dialogy (MessageBox) automaticky testovány nebyly.

## Umístění hlavního okna – PŘIDÁNO 2026-11-22 (v aplikaci zatím neověřeno)
- Konfigurace: `AppSettings.MainFormPlacement` = "Normal|Maximized;X;Y;Šířka;Výška"; souřadnice jsou vždy Bounds stavu Normal.
- Rozhodnutí autora:
  - (A) při zavření Maximized/Minimized se ukládá `Form.RestoreBounds`, tedy poslední Normal pozice včetně přesunů v aktuálním běhu;
  - (B) při otevření musí být celé okno na pracovní ploše jednoho monitoru, vybírá se monitor s největším průnikem, jinak nejbližší; okno se případně zmenší (nejvýš na MinimumSize) a posune dovnitř. Kontroluje se vždy, i pro Maximized.
- `MainForm`, region „Umístění okna“:
  - `_PlacementRestore` se volá na konci konstruktoru (Manual, Bounds, případně Maximized). Původně se volal v `OnLoad`, kde okno při Maximized nejdřív probliklo v Normal pozici. `RestoreBounds` setovat nelze, je jen get. V konstruktoru je handle už vytvořený, okno je ale neviditelné: test ukázal, že při prvním VisibleChanged je okno už Maximized a RestoreBounds = uložené Bounds;
  - `OnLoad` (po base) volá pojistku `_PlacementCheckAfterLoad`: pokud AutoScale (font/DPI) změnil obnovené Normal Bounds, vrátí je. U Maximized to jde jen přepnutím Normal → Maximized, takže může probliknout, ale jen ve výjimečném případě;
  - `OnFormClosing` (není-li Cancel) volá `_PlacementSave`;
  - `OnResize` si pamatuje poslední stav jiný než Minimized (zavření z minimalizovaného okna uloží stav před minimalizací).
- Čisté statické funkce `_PlacementFormat`, `_PlacementParse`, `_PlacementFitToScreens(bounds, minSize, workingAreas[])`.
- Test `test_placement.ps1` (reflexe, okna se zavírají přes Dispose, aby se nepřepsala uživatelova konfigurace): parse/format, 9 scénářů monitorů, RestoreBounds při Maximized/Minimized a obnova z Maximized na původní Normal Bounds – vše OK.
- Pozn.: Windows 10/11 počítají do Bounds neviditelný okraj (~7 px), okno přisazené k levému okraji má X ≈ -7. Při otevření se proto posune o těch pár px dovnitř, nekumuluje se to.

## StatusBar s datem buildu – PŘIDÁNO 2026-11-22
- `SDCardTester.csproj`: target `GenerateBuildDateInfo` (BeforeTargets=CoreCompile) zapíše přes `WriteCodeFragment` soubor `obj\<Config>\BuildDateInfo.g.cs` s `[assembly: AssemblyMetadata("BuildDate", "yyyy-MM-ddTHH:mm:ss")]` a přidá ho do Compile. Datum se mění při každém skutečném buildu. Pokud VS vyhodnotí projekt jako aktuální, nepřekládá se a datum zůstane.
- Datum se nebere z PE hlavičky, protože `Deterministic=true` tam místo času dává hash.
- `MainForm.StatusBarInitialize()` (region „StatusBar“): `StatusStrip` dole, pružný prázdný label + `StatusBuildLabel` vpravo „Build: dd.MM.yyyy HH:mm“, tooltip s verzí. Do `Controls` se přidává až po InitializeComponent, takže se dokuje první a zabírá celou šířku okna.
- `_GetBuildDate()` čte atribut; když chybí, zobrazí „Build: neznámý“.
- Ověřeno reflexním testem (`test_statusbar.ps1`): datum přečteno, StatusBar 1015×22 dole, levý panel zkrácen nad něj, render v pořádku.

## FileRescue a spánek Windows – OPRAVENO 2026-11-22 (v aplikaci zatím neověřeno)
### Nález (log `Sirotčinec slečny Peregrinové pro podivné děti.ts.rescuelog`, 2026-11-22)
- Systémový log: spánek S3 „System Idle“ 13:14:11 – probuzení 13:27:52 (818 s). Blok na pozici 1 559 363 584 měl čtení **824 s** (spánek + ~6 s probuzení USB), další blok zápis 10,8 s (roztočení disku G:). Všech 15 793 bloků OK, data v pořádku; `TotalCopyTime` 941 s, skutečné kopírování ~120 s.
- Čtení během spánku stálo, po probuzení doběhlo; čtečka i karta spánek přežily. **Obsah ověřen** (2026-11-22): 8 bloků kolem místa uspání (pozice 1 558 970 368 – 1 560 018 944, 1 MB, včetně bloku čteného přes spánek a bloku se zápisem 10,8 s) zdroj H: = cíl, 0 rozdílných bajtů. **Stopwatch (QPC) i DateTime dobu spánku započítávají.**
- Důsledky před opravou: zkreslené časy (blok, celkový čas, barvení rychlosti); hlídání zamrzlé karty po probuzení vidělo operaci > 800 s → krátce „Zdroj neodpovídá – vyjměte kartu“, TaskBar červený; Teploměr zdraví by započetl 1× zamrznutí (→ „S rizikem“) a 1 výpadek > 0,5 s. Kdyby Windows při spánku vypnuly USB, karta by zmizela → běh skončí jako odpojení (data neohrožena).
### Oprava (schválil autor: A i B)
- **A – zákaz automatického spánku během záchrany:** `_PreventSystemSleep(true/false)` = `SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED)` na začátku `Run()`, vrácení `ES_CONTINUOUS` na začátku `finally` (stejné vlákno – nastavení platí pro vlákno). Monitor se vypnout smí; ruční uspání / zavření víka to nezablokuje.
- **B – doba spánku se nepočítá jako problém karty:** `_GetAwakeTicks()` = `QueryUnbiasedInterruptTime` (čas bez spánku, 100 ns, rozlišení ~15,6 ms). `_SourceIo` (limit, opuštění po Stop, počítání zamrznutí) a `_GetSourceNotRespondingSeconds` (`__SourceIoStartTicks`) měří čas bez spánku. Doba čtení bloku (`_CopySingleBlock.getReadSeconds`) = Stopwatch − doba spánku (`_GetSleptSeconds` = QPC − čas bez spánku, pod 1 s = šum → 0) → správně v logu, barvení i Teploměru. Celkový čas běhu dobu spánku dál obsahuje (kosmetika, ponecháno).
- Vedlejší oprava: chyba bloku před začátkem měření (simulovaná chyba, `timeBegin = 0`) ukládala jako dobu čtení čas od startu Stopwatch → nyní 0.
### Ověřeno
- `scratchpad\test_sleep.ps1`: po zákazu stav vlákna 0x80000001, po povolení 0x80000000; bez spánku čas bez spánku = přesný čas (2,509 s), spánek 0. (`powercfg /requests` vyžaduje administrátora – nepoužito.)
- `test_hang.ps1`: hlídání zamrzlé karty beze změny (varování po 26 s, Stop do 3,1 s, data OK). `test_health_run.ps1`: beze změny, chybné bloky se simulovanou chybou mají dobu 0.
- Skutečný spánek netestován (počítač se neuspával).

## „Teploměr zdraví“ zdrojového média – REALIZOVÁNO 2026-11-21, zobrazení v aplikaci ověřeno autorem
### Realizace
- Nový soubor `Workers\MediaHealth.cs` (v csproj): třída `MediaHealth` (sběr dat, výpočet), `HealthResult`, `HealthIndicator`, `HealthLevel` (Discard / Risk / Ok / Excellent) a control `MediaHealthControl : FileRescueSummaryControl`.
- Sběr dat (thread Work, zámek; GUI čte `GetResult`):
  - `FileRescue.Run()` → `__Health.Reset()`; `_CopySingleFile` → `StartFile(zdroj)` (posuvné okno rychlosti jen v rámci souboru);
  - `_CopySingleBlock` → `AddBlockRead(pozice, bajty, doba, isError, hadPreviousErrors)` u úspěchu i chyby bloku; ztráta / zamrznutí zdroje se jako chyba bloku nehlásí. Doba chyby jen když čtení začalo (simulovaná chyba nastává před měřením);
  - `_SourceIo` → `AddHang()` při opuštění operace a při doběhnutí vnější operace delší než `SourceHangWarningSeconds` (20 s); probíhající zamrznutí se v `GetSummary` přičte živě; odpojení (`__SourceDeviceLost`) se počítá jen když nebylo zamrznutí (to už je započteno).
- Ukazatele: nečitelné bloky = bloky, které v tomto běhu selhaly a dosud nebyly přečteny (`HashSet` soubor|pozice); přečteno po opakování = úspěch bloku s předchozí chybou (i z dřívějšího běhu); pomalé čtení a výpadky jen u bloků bez předchozí chyby (opakované čtení vadného místa je pomalé a je už započteno chybou); stabilita p5/medián rychlosti okna 16 bloků (min. 64 vzorků), histogramy log10 s 50 třídami na řád.
- **Doplněk oproti návrhu (pojistka pro pomalý port):** práh 0,1 s i 0,5 s je nejméně 3× medián doby čtení bloku (`SlowReadMedianFactor`, medián z min. 32 bloků). Na pomalém USB (medián nad 33 ms na blok) by jinak zdravá karta vycházela jako vadná. Zvýšený práh je vidět v názvu ukazatele a v rozpisu.
- Verdikt = nejhorší ukazatel; při shodě úrovně ten nejblíže horšímu pásmu; pozice ukazatele = (úroveň + pozice v pásmu) / 4. Pod 256 MB přečtených dat: „Zatím málo dat“ (bledá stupnice, bez ukazatele), ale výsledek S rizikem / Vyhodit se zobrazí s „Předběžně:“.
- Zobrazení: control pod Summary blokem vlevo (`MainForm._FileRescueSummaryPanelPrepare`, `_FileRescueHealthControl`): řádek „Zdraví média: verdikt“, pruh se 4 pásmy (barvy bloků mapy: vzdaný / 2+ chyby / po opakování / OK, tj. podle palety), trojúhelníkový ukazatel, řádek důvodu; ToolTip s rozpisem ukazatelů.
- Výsledek po skončení běhu: `ZdraviMedia yyyy-MM-dd HH-mm-ss.txt` ve výstupním adresáři (zdroj, cíl, čas, způsob ukončení, verdikt, důvod, ukazatele s prahy); jen pokud běh něco četl nebo se zdroj odpojil, chyba uložení se ignoruje.
### Ověřeno (scratchpad `test_health.ps1`, `test_health_run.ps1`)
- Přehrání 25 dnešních logů (`G:\Cdisk\FileRescueTestTarget`, karta kopírovaná 2026-11-21 19:30–20:12): **Excelentní stav**, 0 chyb, 2 bloky > 0,1 s (< 0,001 %), stabilita 0,33, medián bloku 2,5 ms.
- Umělé scénáře: zdravá karta na rychlém i pomalém portu (medián 41 ms → práh 0,122 s) = Excelentní; 0,74 % bloků > 0,1 s = S rizikem; 3 chybné bloky = S rizikem, po přečtení 2 z nich 1 nečitelný + 2 po opakování; 1× odpojení = S rizikem; 3× zamrznutí = Zcela vyhodit; 100 MB bez chyb = Zatím málo dat; 100 MB s 1 chybou = Zcela vyhodit (předběžně).
- Celý běh `FileRescue.Run()` nad souborem 700 MB se simulovanými chybami (`TestAddBadBlocks`): 5 600 bloků, 7 chybných pokusů, 1 nečitelný, 3 po opakování → S rizikem; soubor `ZdraviMedia …txt` vytvořen.
- Vykreslení controlu do PNG (6 stavů) – bez ořezu textů v šířce 245 px.
- Neověřeno: zobrazení v běžící aplikaci (výška levého panelu se 3 bloky), reálná vadná karta.

### Původní návrh
- **Rozhodnutí autora 2026-11-21:** (1) data jen z čtení v aktuálním běhu (ne z logů předchozích běhů); (2) prahy dle tabulky níže schváleny; (3) po skončení uložit verdikt + důvod + rozpis do textového souboru ve výstupním adresáři.
- Tabulka prahů (verdikt = nejhorší řádek): vzdané bloky 0 / 0 / ≤ 0,1 % / > 0,1 %; přečteno po opakování 0 / ≤ 0,01 % / ≤ 1 % / > 1 %; odpojení/zamrznutí 0 / 0 / 1–2 / ≥ 3; bloky > 0,5 s 0 / ≤ 0,01 % / ≤ 0,5 % / > 0,5 %; bloky > 0,1 s ≤ 0,01 % / ≤ 0,5 % / ≤ 5 % / > 5 %; p5/medián ≥ 0,30 / ≥ 0,15 / nižší / – (pořadí Excelentní / V pořádku / S rizikem / Vyhodit).
- Kalibrace z logů: zdravá karta (11 souborů, 104–32 766 bloků): medián rychlosti okna 16 bl. 22–46 MB/s, p5/medián 0,23–0,39, **0 % bloků > 100 ms**, 0 chyb. Umírající MicroSD (dřívější logy): p90 67–81 ms, p99 377 ms, max 920 ms, chyby, odpojování, tichý převrat bitů. Poměr p5/medián rozlišuje slabě (0,21 vs 0,23–0,39) → jen doplňková metrika; rozhodující jsou výpadky (> 100 / > 500 ms), chyby a odpojení.
- Metriky: A chyby čtení (podíl chybných / vzdaných / přečtených po opakování), B odpojení a zamrznutí, C výpadky latence (% bloků > 100 ms a > 500 ms), D stabilita rychlosti (p5/medián okna 16 bl.). Absolutní rychlost ne (závisí na čtečce a portu). Tichý převrat bitů FileRescue nepozná (nemá referenci).
- Verdikt = **nejhorší ukazatel** (tabulka prahů v odpovědi autorovi 2026-11-21: vzdané bloky, přečteno po opakování, odpojení/zamrznutí, bloky > 0,5 s, bloky > 0,1 s, p5/medián); pozice ukazatele uvnitř pásma podle toho, jak blízko je ukazatel hranici. Pásma: Excelentní / Funguje v pořádku / Používat s rizikem / Zcela vyhodit. Pod 256 MB přečtených dat „málo dat“.
- Zobrazení: nový control pod Summary vlevo, vodorovný pruh se 4 barevnými pásmy, ukazatel, verdikt + hlavní důvod, ToolTip s rozpisem metrik. Výpočet v samostatné třídě (`MediaHealth`), použitelné i pro DriveTester.

## FileRescue: neexistující cílový adresář a chyby souborů – OPRAVENO 2026-11-21, ověřeno autorem v aplikaci (neexistující disk = chyba, neexistující adresář = vytvořen)
- Chyba (nalezena autorem): neexistující výstupní adresář se nevytvořil; každý soubor skončil výjimkou (zápis logu), MessageBox a pokračování dalším souborem – opakovaně až do konce.
- Oprava: `Run()` před přípravou seznamu volá `_PrepareOutputDirectory()` – `Directory.CreateDirectory` (i vnořené); nejde-li vytvořit, `App.ShowError` „Chyba cílového adresáře“ a běh končí. Cesta (včetně výchozí „FildeRescue“ na ploše) z jedné metody `_GetOutputPath()`.
- `_RunFiles` iteruje po indexu: při výjimce souboru, pokud následuje další soubor (a není Stop / ztráta zdroje), `_AskContinueAfterFileError` → `App.ShowQuestion` „Při zpracování souboru došlo k chybě: … Přejete si pokračovat dalším souborem?“ (časomíra stojí); Ne → `__AbortedByUser`, konec běhu bez 2. vlny. U posledního souboru jen `App.ShowError` (OK). Text ukazuje `exc.Message`, plný `exc.ToString()` zůstává v `SingleFileInfo.ErrorMessage`. `App.Show*` běží synchronně v GUI threadu (`RunInGui` → `Invoke`).
- Test (`scratchpad\test_outdir.ps1`, dialogy zavírá automat přes WM_COMMAND): vnořený neexistující adresář vytvořen; 3 soubory s chybou (místo logu adresář): odpověď Ano → 2× dotaz Ano/Ne + 1× OK, chyba u 3 z 3; odpověď Ne → 1 dotaz, chyba u 1 z 3, `__AbortedByUser = true`.

## DriveTester: zápis a čtení testovacích dat – revize a opravy 2026-11-21
Revize `SaveTestData` / `ReadTestData` (`Workers\DriveTester.cs`). Autor schválil opravy 1–6, drobnosti odloženy. Ověřeno testem metod přes reflexi (`scratchpad\test_drivetester.ps1`), v aplikaci zatím ne.
1. **Chyby I/O se tiše spolkly** (`catch { }` kolem celé smyčky) → test skončil s „O.K.“. Nyní: `RunTestSaveOneFileWriteAsync` chybu zápisu vrací ve výsledku (chybné bajty = nezapsaná část, `FileTimeInfo.ErrorMessage`), soubor se přejmenuje na `.tmpError` a nečte se; po `SaveMaxConsecutiveFailedFiles = 3` selháních po sobě konec s varováním. `RunTestReadOneFileAsync` chybu čtení bufferu započte (celý buffer chybný), pokračuje za ním (Seek), po `ReadConsecutiveErrorsCheck = 8` chybách ověří `IsTargetReady()`; chyba otevření = celý soubor chybný; nepřečtený zbytek (ne po Stop) = chybný. Před každým souborem `IsTargetReady()`, jinak konec s varováním.
2. **Kontrola po zápisu z cache:** autor upřesnil, že u dlouhých souborů (desítky–stovky MB) rychlost odpovídá realitě → beze změny. **Krátké soubory (4 kB)** se nyní čtou mimo cache: `FileOptions` 0x20000000 (FILE_FLAG_NO_BUFFERING, .NET Framework ji akceptuje), `bufferSize = 1`, čtení do připnutého pole na adresu zarovnanou na 4096 (`ReadUnbuffered`); jen pro délky násobku 4096, jinak (nebo když otevření selže) standardní čtení.
3. **`Run()` bez try/finally** → nyní `try / catch (App.ShowError) / finally CallWorkingDone()`.
4. **Přetečení `int errorBytes`** (soubor > 2 GB celý chybný → záporné → „O.K.“) → `long` (`VerifyTestData`, `CallTestStep`).
5. **Přechodné chyby maskované druhým čtením** → `RunTestReadOneFile`: 2. čtení bez chyby = varování (`FileTimeInfo.WarningBytes/WarningFiles`, text „O.K., přechodná chyba (…)“, barva `Skin.TestResultWarningBackColor` žlutá ve všech paletách), soubor se nepřejmenuje; 2. čtení s chybou = chyba s max(chyby 1., 2. čtení), přejmenování.
6. **Slabý vzor dat** (perioda 256 B → všech 2048 sektorů 1MB bufferu shodných) → nový formát po 512B sektorech: bajty 0–7 pozice v souboru, 8–11 číslo souboru, 12–15 značka `TestDataMagic` "DjST", zbytek xorshift64 ze semínka (číslo souboru, pozice). Generování ~600 MB/s. Data nezávisí na velikosti bufferů (`CreateTestData(fileNumber, fileOffset, length)`). **Zpětná kompatibilita:** formát se určí podle 1. bufferu (`TestDataFormat`); soubory bez značky se ověřují původním vzorem (`_ProcessTestDataLegacy`) – starší testovací soubory na kartách tedy zůstávají platné.
- Výsledky testu: správná data 0 chyb; 1 převrácený bit = 1 B; prohozené 2 sektory = 988 B; jiný soubor / jiná pozice ≈ celý buffer; zápis+čtení 40 MB a 4 kB (nebufferovaně) 0 chyb; poškozený bajt → „Chyba (1 B) !“ + `.tmpError`; zamčený soubor při čtení = 4096 B chyb, při zápisu = 16 MB chyb + zpráva, bez výjimky; legacy soubor 3 MB 0 chyb.
- Neověřeno: skutečná chyba čtení uprostřed souboru (vadný sektor) a přechodná chyba – nelze simulovat bez vadného média.
- **Odložené drobnosti:** `ProgressRatio` ořez na `100m` místo `1m`; `GetTestFileType` vrací pro cizí soubor `TestFileError` místo `OtherFile`; `ReadTestData` bez testovacího adresáře skončí bez zprávy.

## Stop a navázání FileRescue – OPRAVENO 2026-11-20

### Realizované změny (`Workers\FileRescue.cs`)
- **A2 (návrh autora):** `WritterRequestInfo.ResultAfterWrite` (`CopyBlockResult`). `_CopySingleBlock` posílá Success (`WriteDataBlock`) / Error (`WriteVoidBlock`) jako `ResultAfterWrite`; výsledek do bloku vepíše `doWriteBlock` až po úspěšném `Write`. `Empty` se ukládá přímo v threadu Work. Nezapsaný blok zůstává ve stavu před čtením → v logu nezpracovaný → přečte se znovu. Řeší i A4.
- **Navázání hledá „díry“:** `_GetFirstBlockLock` = `ErrorLevelProcess = 0; CurrentBlockIndex = null; return _GetNextBlockLock();`. V `_GetNextBlockLock` po projití existujících bloků na úrovni 0 `continue` → nový blok za posledním, na konci souboru přechod na úroveň 1.
- **`_WrittingThreadWaitIdle()`:** před `GetNextBlock` při opakování chybných bloků (`ErrorLevelProcess > 0`) a na konci 1. průchodu počká na dokončení zápisu, aby hledání vidělo aktuální stav / `ErrorsCount` posledního bloku (jinak by blok dostal méně pokusů). Při 1. průchodu se nečeká → čtení a zápis běží dál paralelně.
- **A1/A3:** Writter končí jen na `EndThread` (odstraněno `IsStoped` z `waitForRequest` i z `_WrittingThreadAddRequest`) → po Stop se dopíše poslední blok a stream se zavře.
- **A5 + ReadOnly:** `_CopySingleAttributes()` (datum, atributy, přejmenování `~E`) jen když `isComplete` = smyčka skončila tím, že nebyl další blok (ne Stop, ne chyba zápisu, ne výjimka). Nutné i proto, že po opravě A3 by se přejmenování při Stop už skutečně provedlo.
- **Oprava starých logů:** `LoadLogFile` – blok `OK` / `OKAfterErrors` bez `WriteTime` (a délkou > 0) se načte jako nezpracovaný (`None`, resp. `WithError` při `ErrorsCount > 0`) → přečte se znovu. Týká se např. souboru „Dobrodružství rabína Jákoba“ níže: při navázání se poslední blok 616 431 616 dočte správně.

### Ověřeno (automatický test přes reflexi nad sestaveným exe, dočasné soubory 20 MB)
- T1: Stop po 0,9 s → log 61 bloků OK, žádný bez WriteTime, cíl = přesně 61 × 131 072 B, obsah OK bloků sedí se zdrojem; cíl lze ihned otevřít pro zápis (stream zavřen). Navázání → SHA256 cíle = zdroj.
- T2: ručně vyrobená díra (blok OK bez WriteTime + nuly v cíli) → navázání ji opraví, SHA256 shoda.
- T3: simulované chyby + Stop → žádné `~E`, původní jméno zůstává; dokončení → `test3~E1.bin`, blok `ErrorAborted` s 3 chybami (všechny 3 pokusy proběhly), mimo nečitelný blok obsah shodný.
- Skript: scratchpad `test_resume.ps1` (dočasný, nepatří do projektu).

### Ověření autorem v aplikaci (2026-11-20) a nález: záporný WriteTime
- Hotové soubory se jen prověřily podle logu a přeskočily, přerušený soubor navázal na očekávané pozici. OK.
- `Den pro mou lásku.ts.rescuelog` se změnil (záloha: `g:\Cdisk\FileRescueTestTargetResults\Filmy 058\`). Rozdíl jen ve 2 řádcích:
  - `TotalCopyTime` 703,1 s → 0,195 s (finální log přepíše čas jen za aktuální běh – drobnost, lze časem sčítat);
  - blok `2133458944;131072;0;OK;12;-29` → `...;OK;50260;101`. **Záporný WriteTime** → `FromLogLine` jej odmítne (`wtim >= 0`) → nová oprava starých logů blok vzala jako nezapsaný → přečetl a zapsal se znovu.
- Obsah: blok v cíli = záloha = zdroj (0 rozdílných bajtů), celé soubory cíl vs. záloha SHA256 shoda. Data byla v pořádku, šlo jen o zbytečné přečtení 1 bloku.
- **Příčina záporného času (ne pauza / Stop):** jeden `Stopwatch` sdílí thread Work i Writter. Work volá při každém bloku `Stopping` → `Stopwatch.Stop()` / `Start()`; Writter současně čte `CurrentTime` (`ElapsedTicks`) → `Stopwatch` není thread-safe, při souběhu se přečte roztržený stav (započtený úsek 2×) → `timeBegin` větší než konec → záporný (nebo i přehnaný) čas. Pauza by čas zápisu jen zkrátila, nikdy nezáporný.
- Návrh opravy (čeká na autora): ve Writteru měřit dobu zápisu přes `Stopwatch.GetTimestamp()` (monotónní, thread-safe) místo sdíleného `CurrentTime`; případně obecně měřit intervaly bloků takto a sdílený Stopwatch nechat jen pro celkový čas bez pauz.

### Původní analýza (2026-11-19)

Otázka autora: Stop uprostřed souboru → nové spuštění se stejnými parametry. Má platit: hotové soubory se přeskočí, rozpracovaný se naváže od dalšího bloku, pak další soubory.

**Verdikt:** základ je navržen správně – log `.rescuelog` se po Stop uloží, navázání pokračuje za posledním blokem v logu (`_GetFirstBlockLock`), hotové soubory se podle logu přeskočí (`_CopySingleFile`: `GetFirstBlock()` vrátí null). **Ale** v okamžiku Stop může vzniknout tichá díra v datech. Řádky níže platí k 2026-11-19 (`Workers\FileRescue.cs`).

### Nalezené chyby
- **A1 – Writter při Stop končí předčasně** (ř. 847 `if (IsStoped) return null;` ve `waitForRequest`, ř. 812): idle Writter po Stop skončí, poslední WriteDataBlock a CloseWritterStream už nikdo nezpracuje → poslední blok nezapsán, stream zůstane otevřený.
- **A2 – výsledek bloku se zapisuje předem (ZÁSADNÍ dle autora)** (ř. 498–499 ignoruje návratovou hodnotu `_WrittingThreadAddRequest`, ř. 516 `StoreBlockResultRead`; ř. 782 `if (IsStoped) return false;`): blok je v logu OK, i když se nezapsal → po navázání 128 kB nul bez hlášení.
- **A3 – otevřený cílový soubor po Stop** (důsledek A1; ř. 461, 857, 445): nové spuštění ve stejném běhu aplikace → `File.Open` sdílení selže → `__WrittingStreamAbort` → přečte se 1 blok, označí OK, nezapíše; každé spuštění posune o 1 falešný blok. Po restartu aplikace se neprojeví. Také přejmenování `~E` / datum tiše selžou.
- **A4 – chyba zápisu nemění stav bloku** (ř. 869 tichý `return` při abort, ř. 886–891).
- **A5 – `~E` přejmenování i při Stop** (ř. 464 `_CopySingleAttributes()` ve `finally`, ř. 565–573; ř. 319 cíl je vždy původní jméno): navázání zapisuje do nového souboru s původním jménem, data zůstanou v `~E`.
- Doplňky: atribut ReadOnly se kopíruje i na rozpracovaný soubor (ř. 562) → navázání nepůjde otevřít pro zápis; přeskočení hotového souboru se neověřuje na cíli (existence, délka) (ř. 427–428); finální `SaveLogFile(true)` čeká na async uložení max. 5 s, pak tiše neuloží (ř. 1832–1837) – bezpečné, jen se znovu zkopíruje pár bloků.

### Řešení A2 navržené autorem (realizováno 2026-11-20)
- `WritterRequestInfo` + nová property `CopyBlockResult ResultAfterWrite` = výsledek, který se do bloku vepíše **až po úspěšném zápisu**.
- `_CopySingleBlock`: Success → `WriteDataBlock` s `ResultAfterWrite = Success`; Error → `WriteVoidBlock` s `ResultAfterWrite = Error` (i `ErrorsCount++` až po zápisu); **Empty** jde mimo Writter → `StoreBlockResultRead(Empty)` zůstává v threadu Work. Řádek 516 pro Success/Error zrušit.
- `doWriteBlock`: po úspěšném `Write` volat `writeBlock.StoreBlockResultRead(request.ResultAfterWrite)`. Při chybě / abort / nezařazeném requestu se stav nemění → blok zůstane `None` a v logu jako nezpracovaný. Tím se řeší i A4 a datový dopad A1.

### Nutná doprovodná změna (realizováno 2026-11-20)
- Dnes navázání pokračuje jen **za posledním blokem** a pak přejde na úrovně chyb 1..3 → blok `None` s `ErrorsCount = 0` **uprostřed** mapy se nikdy nenajde. Bez úpravy by A2 vyrobilo trvalé díry.
- Úprava: `_GetFirstBlockLock` = `ErrorLevelProcess = 0; CurrentBlockIndex = null; return _GetNextBlockLock();` (prohledá mapu od začátku); v `_GetNextBlockLock` po projití úrovně 0 nepřecházet na úroveň 1, ale `continue` → krok 1 vytvoří nové bloky za posledním (nebo na konci souboru přejde na úroveň 1). Pozor: dnešní podmínka `if (scanFromZero && !hasNextBlocks) return null;` by na úrovni 0 vrátila null dřív, než se vytvoří nové bloky – musí se upravit.

### Vedlejší účinky (přijatelné)
- Blok čekající na zápis je krátce vidět jako „Čte se“.
- Pokud se blok nezapíše během opakování chybných bloků (chyba zápisu), v aktuálním běhu se už nezkusí (zůstal na své úrovni chyb) – zkusí se při příštím spuštění.
- `Status` / `ErrorsCount` nově mění thread Writter, čte Work a GUI; jde o prosté přiřazení před uvolněním dalšího requestu – nanejvýš krátce zastaralé zobrazení.

### Doporučeno opravit spolu s tím
- A1/A3: Writter ukončovat jen na `EndThread` (posílá ho vždy `Run()` ve `finally`) – odstranit `IsStoped` z `waitForRequest`; v `_WrittingThreadAddRequest` při Stop nevracet false, jen čekat na volný Writter (timeout ponechat).
- A5 + ReadOnly: `_CopySingleAttributes` (přejmenování `~E`, atributy, datum) jen po úplném dokončení souboru (žádný další blok, ne Stop).
- Volitelně: před přeskočením hotového souboru ověřit existenci a délku cíle.

### Ověřeno na reálném souboru (2026-11-20)
`g:\Cdisk\FileRescueTestTarget\Dobrodružství rabína Jákoba, Luis de Funes.ts` (zdroj `h:\Records\...`, 4 294 705 152 B, zastaveno při 14,36 %):
- Log: `ProcessedSize` 616 562 688 B, poslední blok `616431616;131072;0;OK;762459;` – stav **OK, ale bez WriteTime** (zápis neproběhl).
- Cílový soubor: délka **616 431 616 B** = přesně o jeden blok (131 072 B) kratší, než tvrdí log → **A1/A2 potvrzeno**.
- Při navázání by se pokračovalo od 616 562 688 → v cíli by zůstala díra 128 kB nul, vedená jako OK.
- `LastWriteTime` cíle = čas zastavení (19.11.2026 22:04:55), ne datum zdroje → datum ze zdroje se zjevně neuplatnilo (konzistentní s A3: stream nezavřen, při pozdějším zavření handle Windows čas přepíše). Zdroj nebyl dostupný, obsah bloků jsem neporovnával.
- Pozn.: chybějící `WriteTime` v logu je spolehlivý indikátor nezapsaného bloku – lze využít i jako kontrolu / opravu existujících logů.

**Stav:** A1–A5 a ReadOnly realizováno 2026-11-20 (viz začátek sekce). Otevřené zůstává jen ověření cíle před přeskočením hotového souboru.

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

## Tipy pro záchranu dat z karet (praktické zkušenosti)
- **Karta dlouho bez napájení: nechat ji nejdřív „probudit“.** Případ 2026-11-21: karta dlouho mimo napájení se při čtení prvního souboru („50 x a stále poprvé.ts“) odpojila. FileRescue soubor přeřadil na konec fronty, zpracoval ostatní soubory a nakonec i tento dokončil: 2,0 GB, 15 309 bloků, 0 chyb, bez opakování, rovnoměrné čtení (medián 1,4 ms/blok ≈ 90 MB/s, max 34 ms). Data tedy byla celou dobu v pořádku.
  - Pravděpodobné vysvětlení (z logu nelze dokázat, ale vše s ním souhlasí): po dlouhém odpojení dělá řadič karty po zapnutí údržbu – sestavuje tabulku převodu adres, prochází paměť a přepisuje buňky, které při skladování ztratily náboj. Současné čtení z počítače pak odpovídá pomalu, čtečka / Windows to vyhodnotí jako selhání a kartu odpojí. Nejvíc to postihne první čtený soubor; po dokončení údržby karta pracuje normálně. Roli může hrát i vyšší odběr proudu (slabý USB port).
  - **Postup:** kartu po připojení nechat několik minut zapojenou bez čtení, teprve potom spustit záchranu. Přeřazování problémových souborů na konec fronty (`DeviceLostCount + ProblemRuns`) má podobný účinek automaticky.
- **Rozlišení „data v pořádku, vadí přístup“ od „vadná data“ podle logu:** soubor dokončený bez chybných a opakovaně čtených bloků, s rovnoměrnými časy čtení = data byla v pořádku, problém byl v řadiči / kontaktech / napájení. Chybné bloky, opakování, střídání dlouhých a velmi krátkých časů a výpadky ve stovkách ms = degradovaná paměť (taková karta se má vyřadit).
- **Soubory dříve nečitelné se mohou později přečíst** (případ 2026-11-21, dvě nahrávky po měsíci 0 chyb, ověřen i synchronizační bajt všech 31 mil. TS paketů): pravděpodobně kontakty, napájení, zaseknutý řadič nebo teplotní vliv na čtení NAND, ne obnovení ztraceného náboje. Před odepsáním souborů zkusit jindy, jinou čtečku, jiný USB port.
- **Jiný USB port / pomalejší port může pomoci:** u nestabilní MicroSD (opakované odpojování na rychlém portu) běžel přenos na pomalejším portu déle bez odpojení.
- **Datum 1. 1. 1980** u zachráněných souborů je zkopírované ze zdroje = nahrávací zařízení nemělo nastavené hodiny; na obsah nemá vliv.
