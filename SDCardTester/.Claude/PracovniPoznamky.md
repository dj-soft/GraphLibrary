# SDCardTester – pracovní poznámky

Dokument pro navázání práce v další session. Poslední aktualizace: **2026-10-15**.

## Projekt v kostce

- Projekt: `GraphLibrary\SDCardTester\SDCardTester.csproj` – WinForms, **.NET Framework 4.8**, C# 7.3 (bez `??=`, switch expressions apod.).
- Namespace: `DjSoft.Tools.SDCardTester`, assembly `Djs.SDCardTester`.
- Komentáře a texty UI jsou česky, XML doc komentáře u všech členů, kód členěn do `#region`.
- Konvence pojmenování: privátní fieldy `__Name`, privátní metody `_Name`, lokální funkce camelCase.
- **Post-build event** podepisuje exe (`signtool`) a kopíruje výstup do `D:\Tools\SdCardTester\`. Pro ověřovací build ho vypínáme: `/p:PostBuildEvent=`.
- MSBuild: `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`.
- Ověřovací build dělám v kopii projektu v `%TEMP%\frb_sdct` (robocopy bez `bin`, `obj`, `.vs`). Cesta ke scratchpadu Claude je pro MSBuild příliš dlouhá (MAX_PATH).
- `git` není v PATH dostupný.

## Hotovo 2026-10-15

### 1. Formulář `FileRescueInputWindow` (`Workers\FileRescue.cs`, konec souboru)

- Prvky: Label + MemoEdit (víceřádkový `TextBox`, výchozí 500×200) pro vstupní soubory; Label + `TextBox` + Button „...“ (`FolderBrowserDialog`) pro výstupní adresář; Buttony **OK** a **Storno**.
- Labely nad vstupními prvky.
- **Layout ručně v `OnResize` → `_DoLayout()`**, nepoužívá se `Anchor`/`Dock` (záměr autora – spolehlivější). MemoEdit mění W i H; TextBox mění W a je u dolního okraje; Buttony dole vlevo. `MinimumSize` nastaven. `AutoScaleMode.None`.
- Konstanty layoutu: `EdgeMargin` (ne `Margin` – kolize s `Control.Margin`), `LabelGap`, `RowGap`, `ButtonGap`, `ButtonWidth`, `ButtonHeight`, `SmallButtonWidth`, `DefaultMemoWidth`, `DefaultMemoHeight`.
- Data: property `InputData` (`FileRescueInputInfo`). Do prvků se přenáší v `OnVisibleChanged` (Show/ShowDialog nejsou virtuální). Null → prázdné prvky.
- OK: když `InputData` je null, vytvoří novou instanci; naplní `InputFileNames` a `OutputFilesPath`; `DialogResult.OK`; Close. Testovací příznaky `TestAddBadBlocks` / `TestAddSimulatedWaiting` formulář nemění.
- Storno: `DialogResult.Cancel`; Close; `InputData` neřeší.
- Esc = Storno, Enter (mimo Memo) = OK.
- Konstruktory: bez parametru a s `FileRescueInputInfo`.
- Autor ověřil v běžící aplikaci: **funguje dle požadavků**.

### 2. `AppSettings` a úprava singletonu `App` (`Application\App.cs`)

**Singleton `App` – dvoufázová inicializace** (konvence autora i z jiných aplikací):
- Fáze 1 = konstruktor `App()`: nesmí používat singleton; jen `__Settings = new AppSettings();`.
- Fáze 2 = `Initialize()`: volá se v getteru `Instance` uvnitř `lock` hned po `__Instance = new App();`. Smí používat singleton. Volá `__Settings.Load()` (a v budoucnu další inicializace).
- `public static AppSettings Settings => Instance.__Settings`.

**`AppSettings`** (název ne `Settings` – kolize s VS generovaným `Properties.Settings`):
- Soubor: `LocalApplicationData\DjSoft\SDCardTester\Settings.xml` (Local, ne Roaming – cesty jsou závislé na stroji).
- Hodnoty v `Dictionary<string, string> __Values`, klíč = název property přes `[CallerMemberName]`. Nová property = jeden řádek:
  `public string Xxx { get { return _GetValue(); } set { _SetValue(value); } }`
- Setter ukládá ihned (bez opožděného save), jen při skutečné změně (ordinální porovnání, včetně null).
- `LastFileRescueInputFileNames`: v úložišti/XML jen LF (setter odebere CR **před** porovnáním), getter vrací CRLF (nejdřív odebere CR, pak `\n` → `\r\n`).
- `LastFileRescueOutputFilesPath`: prostý string.
- `Load()`: `__IsLoading = true` v try/finally; načítá **přímo do slovníku**, ne přes settery. Chybějící/poškozený soubor → výchozí hodnoty (null), nic se nehlásí.
- `Save()`: při `__IsLoading` nic nedělá. Pod privátním zámkem `__Lock` zapíše `Settings.xml.tmp`, smaže původní, přejmenuje tmp. Výjimku nikdy nevyhodí – zachytí ji a `App.ShowError()` volá **až po uvolnění zámku** (jinak hrozí deadlock: `ShowError` → `Invoke` do GUI threadu, který může čekat na zámek v setteru).
- Opakované chybové hlášky při opakovaném selhání ukládání jsou zatím akceptovány (řešit případně později).
- Ztráta souboru Settings (pád mezi Delete a Rename) není katastrofa – akceptováno.
- Formát XML:
  ```xml
  <Settings Version="1">
    <Value Name="LastFileRescueInputFileNames">a.txt
  b.txt</Value>
    <Value Name="LastFileRescueOutputFilesPath">G:\out</Value>
  </Settings>
  ```
- Ověřeno round-trip testem (save → load, CRLF zachováno, tmp nezůstává) a autorem v běžící aplikaci.

## Otevřené body / známé problémy

1. **`FileRescue._CopySingleFile()` – NullReferenceException ve `finally`** (`Workers\FileRescue.cs`, cca ř. 244):
   `sourceStream.Close()` bez kontroly null. Když `File.Open` selže (soubor zamčený apod.), NRE ve `finally` zakryje původní výjimku a neproběhne zbytek `finally` – neuloží se log a stav nezmění na `Finished`. Oprava: `sourceStream?.Dispose()`. **Zatím neopraveno.**
2. **Napojení formuláře do `MainForm.RunFileRescue()`** (`Forms\MainForm.cs`, cca ř. 820) – autor rozpracovává sám. Zatím tam jsou natvrdo testovací cesty v `FileRescueInputInfo`. Předpokládané volání:
   ```csharp
   using (var window = new FileRescueInputWindow(inputInfo))
   {
       if (window.ShowDialog(this) != DialogResult.OK) return;
       inputInfo = window.InputData;
   }
   ```
   a předvyplnění z / uložení do `App.Settings.LastFileRescueInputFileNames` / `LastFileRescueOutputFilesPath`.
3. Drobnost: singleton `Instance` používá double-checked locking a `__Instance` je přiřazen před `Initialize()` → jiný thread by teoreticky mohl dostat Settings před dokončením `Load()`. Při startu WinForms aplikace prakticky nehrozí; akceptováno.
4. Stávající varování kompilátoru v `FileRescue.cs` (CS4014 u `SaveLogFileAsync()` ř. ~306, CS0168 nepoužité proměnné výjimek) – nesouvisí s dnešní prací, neřešeno.

## Jak FileRescue funguje (shrnutí z analýzy)

- `FileRescue : BackWorker<FileRescueInputInfo>` – záchrana souborů z poškozeného média po blocích.
- Čtení v threadu Work, zápis v samostatném threadu **Writter** (`WritterRequestInfo`, semafory `__WrittingSemaphoreStart` / `__WrittingSemaphoreDone`).
- Chybný blok se do cíle zapíše jako nuly (`WriteVoidBlock`) a kopírování pokračuje.
- `SingleFileInfo` (soubor), `SingleBlockInfo` (blok, stav `BlockStatus`, časy čtení/zápisu), `SingleLogInfo` (mapa bloků, `.rescuelog` pro navázání po přerušení, `ErrorLevelProcess` pro opakované pokusy o chybné bloky).
- Simulace chyb (`TestAddBadBlocks`) a zpomalení (`TestAddSimulatedWaiting`) pro testování.
