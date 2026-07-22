# C# Parser pro Windows .lnk zástupce

## Přehled

Tento projekt umožňuje v C# načítać a manipulovat se soubory Windows zástupců (**.lnk**).

## Komponenty

### 1. **ShortcutInfo.cs** - Datová třída
Obsahuje všechny vlastnosti zástupce:
- `TargetPath` - Cesta k cílové aplikaci/souboru
- `Arguments` - Argumenty pro cílovou aplikaci
- `WorkingDirectory` - Pracovní adresář
- `Description` - Popis zástupce
- `IconLocation` - Cesta k ikoně
- `IconIndex` - Index ikony v souboru ikon
- `WindowStyle` - Styl okna (Normal, Minimized, Maximized)
- `HotKey` - Horká klávesa
- `RelativePath` - Relativní cesta
- `LinkPath` - Cesta ke zástupci
- `LinkName` - Jméno zástupce

### 2. **ShortcutLoader.cs** - Třída pro načítání/ukládání
Hlavní třída s metodami:
- `LoadShortcut(path)` - Načte zástupce z disku do objektu
- `SaveShortcut(path, info)` - Vytvoří/uloží nový zástupce

### 3. **Program.cs** - Příklady použití
Obsahuje tři příklady:
1. Načtení a vypsání existujícího zástupce
2. Vytvoření zcela nového zástupce
3. Modifikace existujícího zástupce

## Použití

### Import do projektu
Zkopírujte soubory `ShortcutInfo.cs` a `ShortcutLoader.cs` do svého projektu.

### Příklad - Načtení zástupce

```csharp
try
{
    ShortcutInfo shortcut = ShortcutLoader.LoadShortcut(@"C:\Desktop\MyApp.lnk");
    
    Console.WriteLine($"Cílový soubor: {shortcut.TargetPath}");
    Console.WriteLine($"Argumenty: {shortcut.Arguments}");
    Console.WriteLine($"Popis: {shortcut.Description}");
}
catch (Exception ex)
{
    Console.WriteLine($"Chyba: {ex.Message}");
}
```

### Příklad - Vytvoření zástupce

```csharp
var shortcut = new ShortcutInfo
{
    TargetPath = @"C:\Program Files\App\app.exe",
    Arguments = "--debug",
    WorkingDirectory = @"C:\Program Files\App",
    Description = "Spuštění aplikace v debug módu",
    WindowStyle = WindowStyle.Normal
};

ShortcutLoader.SaveShortcut(@"C:\Desktop\MyApp.lnk", shortcut);
```

## Technické poznámky

### COM Interop
Kód používá Windows COM API (WScript.Shell) pro práci se zástupci. To vyžaduje:
- Windows OS
- .NET Framework (nebo .NET 6+ na Windows)
- COM objekty jsou na konci uvolněny pomocí `Marshal.ReleaseComObject()`

### Jednotlivé vlastnosti zástupce

| Vlastnost | Editátor Windows | Popis |
|-----------|-----------------|-------|
| TargetPath | "Cíl:" | Cesta k aplikaci/souboru |
| Arguments | "Argumenty:" | Parametry pro aplikaci |
| WorkingDirectory | "Počáteční adresář:" | Pracovní adresář |
| Description | "Komentář:" | Popis zástupce |
| IconLocation | "Změnit ikonu" | Cesta k ikoně |
| WindowStyle | "Okno:" | Jak se má okno spustit |
| HotKey | "Klávesová zkratka:" | Globální klávesová kombinace |

### Chybějící vlastnosti

Některé vlastnosti nejsou dostupné přes standardní COM API:
- **Spustit jako administrátor** - Vyžaduje parsování binárního formátu .lnk
- **Plné okno** - Zastaralý parametr

Pro úplný přístup k těmto vlastnostem by bylo potřeba parsovat binární formát souboru .lnk přímo.

## Příklad binárního parsování (pokročilé)

Pokud potřebujete přístup k vlastnostem, které COM API neposkytuje, viz Microsoft dokumentace:
- [MS-SHLLINK]: Shell Link Binary File Format
- NuGet balíčky: `SharpShell`, `DotNetZip`

## Licence

Tento kód je volně k použití.
