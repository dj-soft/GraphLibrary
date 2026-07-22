# Clipboard integrace do Drag&Drop formuláře

## 🎯 Nová funkčnost

Uživatel nyní může:
1. **Zkopírovat** zástupce do schránky (Ctrl+C na ploše)
2. **Pravý klik** v našem formuláři
3. **Vybrat "Vložit zástupce"** z kontextového menu
4. Ikona se vykresli na místo, kde klikl

---

## 📋 Implementované změny

### 1. Kontextové menu (pravý klik)

```csharp
private ContextMenuStrip contextMenu;

private void SetupContextMenu()
{
    contextMenu = new ContextMenuStrip();
    
    // Položka: Vložit
    var pasteItem = new ToolStripMenuItem("Vložit zástupce");
    pasteItem.Click += PasteShortcut_Click;
    contextMenu.Items.Add(pasteItem);
    
    contextMenu.Items.Add(new ToolStripSeparator());
    
    // Položka: Vymazat vše
    var clearItem = new ToolStripMenuItem("Vymazat vše");
    clearItem.Click += ClearAll_Click;
    contextMenu.Items.Add(clearItem);
    
    this.ContextMenuStrip = contextMenu;
}
```

### 2. Event: Pravý klik myši

```csharp
private void ShortcutDropForm_MouseDown(object sender, MouseEventArgs e)
{
    if (e.Button != MouseButtons.Right)
        return;
    
    // Ulož pozici pro vložení
    this.Tag = e.Location;
    
    // Zobraz menu
    contextMenu.Show(this, e.Location);
}
```

**Proč `MouseDown` a ne `MouseClick`?**
- `MouseDown` se spustí dříve
- Dovoluje zobrazit kontextové menu na přesné pozici
- `MouseClick` by se spustil až po zavření menu

### 3. Čtení z Clipboardu

```csharp
private void PasteShortcut_Click(object sender, EventArgs e)
{
    // Čti souborový seznam z Clipboardu
    if (!Clipboard.ContainsFileDropList())
    {
        MessageBox.Show("Clipboard neobsahuje soubory");
        return;
    }
    
    var files = Clipboard.GetFileDropList();  // StringCollection
    
    // Filtruj .lnk
    var lnkFiles = files
        .Cast<string>()
        .Where(f => f.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        .ToList();
    
    // Zpracuj
    foreach (string lnkFile in lnkFiles)
    {
        ProcessDroppedShortcut(lnkFile, pasteLocation);
    }
}
```

---

## 🔄 Workflow - Clipboard vs. Drag&Drop

```
┌─────────────────────────────────┐
│   DRAG & DROP                   │  │   CLIPBOARD (PRAVÝ KLIK)       │
├─────────────────────────────────┤  ├─────────────────────────────────┤
│ 1. DragEnter                    │  │ 1. MouseDown (pravý klik)       │
│    ↓ Validace                   │  │    ↓ Ukaž menu                  │
│ 2. DragDrop                     │  │ 2. PasteShortcut_Click          │
│    ↓ Čtení e.Data              │  │    ↓ Čtení Clipboard            │
│ 3. ProcessDroppedShortcut()     │  │ 3. ProcessDroppedShortcut()     │
│    ↓ Stejná metoda!             │  │    ↓ Stejná metoda!             │
│ 4. Vykresli                     │  │ 4. Vykresli                     │
└─────────────────────────────────┘  └─────────────────────────────────┘
```

**Klíč:** Obě cesty volají stejnou `ProcessDroppedShortcut()` metodu!

---

## 💡 Praktické příklady

### Příklad 1: Vložit pravým klikem

```csharp
// 1. Uživatel zkopíruje zástupce na ploše (Ctrl+C)
// 2. Jde do formuláře
// 3. Pravý klik na pozici [100, 100]
// ↓
// Formulář:
// - Načte soubor z Clipboardu
// - Spustí ProcessDroppedShortcut(lnkPath, [100, 100])
// - Vykresli ikonu na [100, 100]
```

### Příklad 2: Vložit víc zástupců

```csharp
// Pokud je v Clipboardu více .lnk souborů:
var lnkFiles = new[] { "C:\\Desktop\\app1.lnk", "C:\\Desktop\\app2.lnk" };

foreach (string lnk in lnkFiles)
{
    ProcessDroppedShortcut(lnk, pasteLocation);
    pasteLocation.X += ICON_SIZE + 10;  // Kaskáda
}
```

### Příklad 3: Helper třída - ClipboardHelper

```csharp
// Zkontroluj, zda je v Clipboardu .lnk
if (ClipboardHelper.HasShortcutsInClipboard())
{
    var shortcuts = ClipboardHelper.GetShortcutsFromClipboard();
    foreach (string lnk in shortcuts)
    {
        // Zpracuj...
    }
}

// Zkopíruj .lnk do Clipboardu (pro testování)
ClipboardHelper.CopyShortcutToClipboard(@"C:\Desktop\test.lnk");

// Vyčisti Clipboard
ClipboardHelper.ClearClipboard();
```

---

## 🛡️ Error handling

### Problem: Clipboard je prázdný
```csharp
if (!Clipboard.ContainsFileDropList())
{
    MessageBox.Show("Clipboard neobsahuje soubory");
    return;
}
```

### Problem: Nejsou tam .lnk soubory
```csharp
var lnkFiles = files
    .Cast<string>()
    .Where(f => f.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
    .ToList();

if (lnkFiles.Count == 0)
{
    MessageBox.Show("V Clipboardu nejsou .lnk soubory");
    return;
}
```

### Problem: Exception při čtení Clipboardu
```csharp
try
{
    var files = Clipboard.GetFileDropList();
    // ...
}
catch (Exception ex)
{
    MessageBox.Show($"Chyba: {ex.Message}", "Chyba", ...);
}
```

---

## 🎨 Kontextové menu - Struktura

```
┌──────────────────────────────────┐
│ Vložit zástupce                  │ ← PasteShortcut_Click
├──────────────────────────────────┤
│                                  │ ← Oddělovač
├──────────────────────────────────┤
│ Vymazat vše                      │ ← ClearAll_Click
└──────────────────────────────────┘
```

### Přidání dalších položek:

```csharp
contextMenu.Items.Add(new ToolStripSeparator());

var refreshItem = new ToolStripMenuItem("Obnovit");
refreshItem.Click += (s, e) => this.Invalidate();
contextMenu.Items.Add(refreshItem);

var infoItem = new ToolStripMenuItem("Informace");
infoItem.Click += (s, e) => MessageBox.Show("Info");
contextMenu.Items.Add(infoItem);
```

---

## 📍 Pozice myši (Tag)

```csharp
// Při MouseDown si ulož pozici
this.Tag = e.Location;  // Point [X, Y]

// Později ji použij v callback
Point pasteLocation = (Point)(this.Tag ?? new Point(20, 20));

// Defaultní pozice: [20, 20] pokud je Tag null
```

---

## 🔍 Detekce tlačítka myši

```csharp
private void Form_MouseDown(object sender, MouseEventArgs e)
{
    if (e.Button == MouseButtons.Left)
        // Levý klik - vybírání
    
    if (e.Button == MouseButtons.Right)
        // Pravý klik - kontextové menu
    
    if (e.Button == MouseButtons.Middle)
        // Střední klik - středové tlačítko
}
```

---

## ✅ Checklist - Co bylo přidáno

- [x] Kontextové menu (pravý klik)
- [x] `ShortcutDropForm_MouseDown()` - detektor pravého kliku
- [x] `PasteShortcut_Click()` - čtení z Clipboardu
- [x] `ClearAll_Click()` - vymazání všech ikon
- [x] Validace: obsahuje Clipboard .lnk?
- [x] Error handling: exception + user feedback
- [x] Filtrování .lnk souborů
- [x] Pozice vložení = místo kliku
- [x] Stejná metoda `ProcessDroppedShortcut()` pro oba způsoby

---

## 🚀 Jak to používat

### Uživatelský workflow:

1. **Na ploše:** Pravý klik na zástupce → Kopírovat (Ctrl+C)
   
2. **V aplikaci:** 
   - Pravý klik na formulář
   - Vybrat "Vložit zástupce"
   - Ikona se vykresli na místo kliku

3. **Výsledek:** Stejné jako Drag&Drop, ale bez tažení myší

---

## 🔧 Rozšíření - Klávesové zkratky

```csharp
// Přidej klávesové zkratky
protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
{
    if (keyData == (Keys.Control | Keys.V))  // Ctrl+V
    {
        // Vložit ze schránky na výchozí pozici [20, 20]
        PasteShortcutAtPosition(new Point(20, 20));
        return true;
    }
    
    if (keyData == (Keys.Control | Keys.A))  // Ctrl+A
    {
        // Vybrat všechny ikony
        SelectAllShortcuts();
        return true;
    }
    
    return base.ProcessCmdKey(ref msg, keyData);
}
```

---

## 📊 Porovnání metod vkládání

| Metoda | Výhody | Nevýhody |
|--------|--------|----------|
| **Drag&Drop** | Visuální, intuitivní | Musím tahat myší |
| **Clipboard + Pravý klik** | Bez tažení, přesná pozice | O jednu akci více |
| **Ctrl+V** | Nejrychlejší | Menší viditelnost |
| **Copy-Paste** | Kombinace obou | Nejpružnější |

---

## 💾 Klíčové metody

| Metoda | Popis |
|--------|-------|
| `SetupContextMenu()` | Vytvoří kontextové menu |
| `ShortcutDropForm_MouseDown()` | Detekuje pravý klik |
| `PasteShortcut_Click()` | Čte z Clipboardu a vloží |
| `ClearAll_Click()` | Vymaže všechny ikony |
| `Clipboard.ContainsFileDropList()` | Kontroluje obsah |
| `Clipboard.GetFileDropList()` | Čte seznam souborů |

---

## 🎓 Naučili jsme se

1. ✅ Detekce pravého kliku (`MouseDown`)
2. ✅ Kontextové menu (`ContextMenuStrip`)
3. ✅ Čtení z Clipboardu (`Clipboard` API)
4. ✅ Filtrování souborů (`.Where()`, `.EndsWith()`)
5. ✅ Reuse kódu (`ProcessDroppedShortcut()`)
6. ✅ UX patterns (menu, dialogy, error handling)

