# Algoritmus extrakce ikon z EXE/DLL souborů

## 🎯 Přehled řešení

Vytvořil jsem třídu `IconExtractor`, která implementuje **komplexní algoritmus extrakce ikon** s následujícími strategiemi:

```
IconLocation + IconIndex
        ↓
Parsování IconLocation (oddělení cesty od indexu)
        ↓
Volání Win32 API ExtractIconEx()
        ↓
Konverze IntPtr → Icon objekt
        ↓
Fallback strategie (různé cesty k ikoně)
        ↓
Výsledek: Icon nebo Bitmap objekt
```

---

## 🔧 Implementované metody

### 1. **GetIconFromFile()** - Základní extrakce
```csharp
Icon icon = IconExtractor.GetIconFromFile(
    @"C:\Windows\System32\notepad.exe",  // Cesta k souboru
    0,                                     // Index ikony
    true                                   // Velká ikona (32x32)
);
```

**Parametry:**
- `filePath` - Cesta k EXE, DLL, ICO souboru
- `iconIndex` - Index ikony (0-based)
- `largeIcon` - True pro 32x32, False pro 16x16

---

### 2. **GetIconFromShortcut()** - Integrovaná extrakce ze zástupce
```csharp
var shortcutInfo = ShortcutLoader.LoadShortcut(@"C:\Desktop\App.lnk");
object result = IconExtractor.GetIconFromShortcut(shortcutInfo, returnBitmap: true);

// Výsledek je buď Icon, Bitmap nebo null
if (result is Bitmap bitmap) { /* ... */ }
```

**Strategie fallback:**
1. Pokus: IconLocation ze zástupce
2. Pokus: TargetPath (cíl zástupce)
3. Fallback: AssociatedIcon

---

### 3. **GetIconCount()** - Zjištění počtu ikon
```csharp
int count = IconExtractor.GetIconCount(@"C:\Windows\System32\shell32.dll");
Console.WriteLine($"Počet ikon: {count}");  // Výstup: Počet ikon: 332
```

---

### 4. **ExportAllIcons()** - Export všech ikon na disk
```csharp
int exported = IconExtractor.ExportAllIcons(
    @"C:\Windows\System32\shell32.dll",     // Zdroj
    @"C:\temp\exported_icons",               // Výstup
    "shell32"                                 // Základní jméno
);
Console.WriteLine($"Exportováno: {exported} ikon");
```

**Generuje:** `shell32_0.ico`, `shell32_1.ico`, atd.

---

### 5. **IconToBitmap()** - Konverze Icon → Bitmap
```csharp
Icon icon = IconExtractor.GetIconFromFile(filePath, 0);
Bitmap bitmap = IconExtractor.IconToBitmap(icon);

// Bitmap lze použít v WinForms PictureBox, GDI+ atd.
pictureBox1.Image = bitmap;
```

---

## 🎯 Algoritmus - Podrobný popis

### Krok 1: Parsování IconLocation
```
IconLocation: "C:\Windows\System32\shell32.dll,0"
              ↓
             Split na ','
              ↓
filePath: "C:\Windows\System32\shell32.dll"
iconIndex: 0
```

### Krok 2: Volání Win32 API
```csharp
uint result = ExtractIconEx(
    filePath,      // "C:\Windows\System32\shell32.dll"
    iconIndex,     // 0
    largeIcons,    // Output: IntPtr[] s handle na ikonu
    null,          // Malé ikony (ignorujeme)
    1              // Počet ikon k extrakci
);
```

### Krok 3: Konverze na .NET Icon
```csharp
IntPtr iconHandle = largeIcons[0];
Icon icon = Icon.FromHandle(iconHandle);
// ⚠️ Poznámka: Handle je spravován .NET, destruktor ho automaticky uvolní
```

### Krok 4: Fallback strategie
```
Pokud primární metoda selže:
  → Zkus stejný soubor s indexem 0
  → Zkus Icon.ExtractAssociatedIcon(filePath)
  → Vrať null
```

---

## 💡 Praktické příklady

### Příklad 1: Extrakce z aplikace v zástupci
```csharp
// 1. Načti zástupce
var shortcut = ShortcutLoader.LoadShortcut(@"C:\Desktop\MyApp.lnk");

// 2. Extrahuj ikonu
var bitmap = IconExtractor.GetIconFromShortcut(shortcut, returnBitmap: true) 
    as Bitmap;

// 3. Použij v UI
pictureBox.Image = bitmap;
```

### Příklad 2: Iterace přes všechny ikony v DLL
```csharp
string dllPath = @"C:\Windows\System32\shell32.dll";
int count = IconExtractor.GetIconCount(dllPath);

for (int i = 0; i < count; i++)
{
    try
    {
        Icon icon = IconExtractor.GetIconFromFile(dllPath, i, true);
        if (icon != null)
        {
            // Ulož nebo zpracuj ikonu
            icon.Save($@"C:\temp\icon_{i}.ico");
            icon.Dispose();
        }
    }
    catch { /* next */ }
}
```

### Příklad 3: Vytvoření náhledu ve Windows formě
```csharp
Icon icon = IconExtractor.GetIconFromFile(
    @"C:\Program Files\App\app.exe", 
    0, 
    true
);

form.Icon = icon;  // Nastav ikonu okna
pictureBox.Image = IconExtractor.IconToBitmap(icon);  // Zobraz v PictureBox
```

---

## 🔌 Win32 API - ExtractIconEx()

### Deklarace:
```csharp
[DllImport("shell32.dll", CharSet = CharSet.Auto)]
extern uint ExtractIconEx(
    string lpszFile,           // Cesta k souboru
    int nIconIndex,            // Index ikony
    IntPtr[] phiconLarge,      // Output: velké ikony (32x32)
    IntPtr[] phiconSmall,      // Output: malé ikony (16x16)
    uint nIcons                // Počet ikon k extrakci
);
```

### Návratová hodnota:
- **0** = Žádné ikony
- **> 0** = Počet dostupných ikon v souboru

### Podporované formáty:
- ✅ EXE (Windows aplikace)
- ✅ DLL (knihovny)
- ✅ ICO (Ico soubory)
- ✅ BMP, GIF, JPG (některé ikony)

---

## ⚠️ Poznámky a best practices

### Memory management
```csharp
Icon icon = IconExtractor.GetIconFromFile(path, 0);
try 
{
    // Použij ikonu
}
finally
{
    icon?.Dispose();  // POVINNÉ!
}
```

### Chybové stavy
```csharp
try
{
    var icon = IconExtractor.GetIconFromFile(path, 999);  // Neexistující index
    // Fallback automaticky zkusí index 0
}
catch (Exception ex)
{
    Console.WriteLine($"Chyba: {ex.Message}");
}
```

### Performance
- ExtractIconEx() je IO operace - **cachuj** vysledky
- Extrakce více ikon: iteruj místo opakovaného volání API
- Pro velké DLL s tisíci ikonami: paralelizuj extrakci

---

## 🔄 Integrace se ShortcutInfo

```csharp
// Komplexní workflow
var shortcutInfo = ShortcutLoader.LoadShortcut(@"C:\Desktop\app.lnk");

// Ikona ze zástupce
var bitmap = IconExtractor.GetIconFromShortcut(shortcutInfo, returnBitmap: true);

// Nebo manuálně
var icon = IconExtractor.GetIconFromFile(
    shortcutInfo.IconLocation, 
    shortcutInfo.IconIndex
);

// Ulož
icon?.Save(@"C:\temp\extracted.ico");
```

---

## 📋 Soubory v balíčku

- **IconExtractor.cs** - Hlavní třída s algoritmem
- **IconExtractionExamples.cs** - 5 praktických příkladů
- Integruje se se stávajícími třídami (`ShortcutInfo`, `ShortcutLoader`)
