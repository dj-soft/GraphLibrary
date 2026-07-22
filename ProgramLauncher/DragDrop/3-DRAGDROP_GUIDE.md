# Implementace Drag&Drop zástupců v Windows Forms

## 📌 Přehled řešení

Vytvořil jsem **dvě verze** formuláře s Drag&Drop funkčností:

### 1️⃣ **ShortcutDropForm.cs** - Jednoduché řešení
- Kreslení přímo na formulář (GDI+)
- Vlastní kreslení ikon a popisků
- Klik na ikonu = detaily v MessageBox
- Minimální overhead

### 2️⃣ **AdvancedShortcutDropForm.cs** - Pokročilé řešení  
- Klientské prvky (UserControl)
- FlowLayoutPanel s automatickým zalamováním
- Tlačítka "Detaily" a "Smazat" u každé ikony
- Lepší UX pro více ikon

---

## 🎯 Klíčové koncepty

### 1. Povolení Drag&Drop
```csharp
// Na formuláři
this.AllowDrop = true;

// Nebo na panelu
flowPanel.AllowDrop = true;
```

### 2. Event Handlery

#### DragEnter - Validace přesunutých dat
```csharp
private void Form_DragEnter(object sender, DragEventArgs e)
{
    if (e.Data.GetDataPresent(DataFormats.FileDrop))
    {
        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        
        // Přijmi drop pouze pro .lnk soubory
        if (files.Any(f => f.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)))
        {
            e.Effect = DragDropEffects.Copy;
            return;
        }
    }
    e.Effect = DragDropEffects.None;
}
```

#### DragDrop - Zpracování souborů
```csharp
private void Form_DragDrop(object sender, DragEventArgs e)
{
    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
    Point dropLocation = this.PointToClient(new Point(e.X, e.Y));

    foreach (string lnkFile in files.Where(f => f.EndsWith(".lnk")))
    {
        ProcessDroppedShortcut(lnkFile, dropLocation);
    }
}
```

---

## 🔧 Workflow zpracování zástupce

```
┌─────────────────────────────┐
│ 1. Uživatel přetáhne .lnk   │
└──────────────┬──────────────┘
               ↓
┌─────────────────────────────┐
│ 2. DragEnter - Validace     │
│    Kontrola: je to .lnk?    │
│    e.Effect = Copy          │
└──────────────┬──────────────┘
               ↓
┌─────────────────────────────┐
│ 3. DragDrop - Zpracování    │
│    Extrakce souboru          │
└──────────────┬──────────────┘
               ↓
┌─────────────────────────────┐
│ 4. ShortcutLoader           │
│    Načti ShortcutInfo       │
└──────────────┬──────────────┘
               ↓
┌─────────────────────────────┐
│ 5. IconExtractor            │
│    Extrahuj ikonu           │
│    Změní velikost (GDI+)    │
└──────────────┬──────────────┘
               ↓
┌─────────────────────────────┐
│ 6. Vykresli na formulář     │
│    Na souřadnicích [X, Y]   │
└─────────────────────────────┘
```

---

## 💻 Praktické příklady

### Příklad 1: Minimální Drag&Drop formulář
```csharp
public class SimpleDropForm : Form
{
    public SimpleDropForm()
    {
        this.AllowDrop = true;
        this.DragEnter += (s, e) => 
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) 
                ? DragDropEffects.Copy 
                : DragDropEffects.None;
        };
        
        this.DragDrop += (s, e) =>
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files.Where(f => f.EndsWith(".lnk")))
            {
                var shortcut = ShortcutLoader.LoadShortcut(file);
                var icon = IconExtractor.GetIconFromShortcut(shortcut);
                // Zpracuj...
            }
        };
    }
}
```

### Příklad 2: Iterace přes přetažené zástupce
```csharp
private void ProcessDroppedFiles(DragEventArgs e)
{
    if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        return;

    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

    // Filtruj .lnk
    var shortcuts = files
        .Where(f => f.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        .ToList();

    // Procházej a zpracuj
    foreach (string lnkPath in shortcuts)
    {
        try
        {
            ShortcutInfo info = ShortcutLoader.LoadShortcut(lnkPath);
            Bitmap icon = (Bitmap)IconExtractor.GetIconFromShortcut(info, true);
            
            // Ulož do seznamu, vykresli, apod.
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Chyba: {ex.Message}");
        }
    }
}
```

### Příklad 3: Kreslení na formulář (GDI+)
```csharp
private void DrawShortcut(Graphics g, DroppedShortcut dropped)
{
    // 1. Vykresli ikonu
    g.DrawImage(
        dropped.Bitmap,
        dropped.Position.X,
        dropped.Position.Y,
        ICON_SIZE,
        ICON_SIZE);

    // 2. Vykresli popis
    var textBounds = new Rectangle(
        dropped.Position.X,
        dropped.Position.Y + ICON_SIZE,
        ICON_SIZE,
        TOOLTIP_HEIGHT);

    g.DrawString(
        dropped.ShortcutInfo.LinkName,
        this.Font,
        Brushes.Black,
        textBounds);

    // 3. Vykresli rámeček
    g.DrawRectangle(
        new Pen(Color.LightGray),
        dropped.Bounds);
}
```

### Příklad 4: FlowLayoutPanel s custom controls
```csharp
private void AddShortcutIcon(string lnkFile)
{
    ShortcutInfo info = ShortcutLoader.LoadShortcut(lnkFile);
    Bitmap bitmap = (Bitmap)IconExtractor.GetIconFromShortcut(info, true);

    // Vytvoř custom control
    var control = new ShortcutIconControl(info, bitmap);
    
    // Přidej event handlery
    control.DeleteClicked += (s, e) => flowPanel.Controls.Remove(control);
    control.DetailsClicked += (s, e) => MessageBox.Show(info.ToString());

    flowPanel.Controls.Add(control);
}
```

---

## 📋 Třída DroppedShortcut

Reprezentuje jednu přesunutou ikonu na formuláři:

```csharp
public class DroppedShortcut
{
    public Point Position { get; set; }              // Místo na formuláři
    public ShortcutInfo ShortcutInfo { get; set; }   // Údaje zástupce
    public Icon Icon { get; set; }                   // Icon objekt
    public Bitmap Bitmap { get; set; }               // Bitmap na vykreslení
    public Size IconSize { get; set; }               // Velikost (64x64)
    public Rectangle Bounds { get; }                 // Pro hit testing
}
```

---

## 🎨 Formát drag&drop dat

### DataFormats.FileDrop
```
string[] files = {
    @"C:\Desktop\Shortcut1.lnk",
    @"C:\Desktop\Shortcut2.lnk",
    @"C:\Desktop\Document.docx"  // Ignorujeme
}
```

### Krok za krokem:
1. **Čti** - `e.Data.GetData(DataFormats.FileDrop)`
2. **Filtruj** - `Where(f => f.EndsWith(".lnk"))`
3. **Zpracuj** - `ShortcutLoader.LoadShortcut(lnkPath)`
4. **Extrahuj** - `IconExtractor.GetIconFromShortcut()`
5. **Vykresli** - `Graphics.DrawImage()` nebo `PictureBox.Image`

---

## 🔑 Klíčové metody v ShortcutDropForm

### ProcessDroppedShortcut()
```csharp
private void ProcessDroppedShortcut(string lnkFilePath, Point position)
{
    // 1. Načti ShortcutInfo
    ShortcutInfo shortcutInfo = ShortcutLoader.LoadShortcut(lnkFilePath);
    
    // 2. Extrahuj ikonu
    object iconResult = IconExtractor.GetIconFromShortcut(
        shortcutInfo, 
        returnBitmap: true);
    
    // 3. Změní velikost
    Bitmap resized = ResizeBitmap(iconResult as Bitmap, ICON_SIZE, ICON_SIZE);
    
    // 4. Ulož do seznamu
    droppedShortcuts.Add(new DroppedShortcut
    {
        Position = position,
        ShortcutInfo = shortcutInfo,
        Bitmap = resized,
        IconSize = new Size(ICON_SIZE, ICON_SIZE)
    });
}
```

### ShowShortcutDetails()
```csharp
private void ShowShortcutDetails(DroppedShortcut dropped)
{
    var info = dropped.ShortcutInfo;
    MessageBox.Show($@"
Název: {info.LinkName}
Cíl: {info.TargetPath}
Argumenty: {info.Arguments}
Popis: {info.Description}");
}
```

---

## 📊 Porovnání verzí

| Vlastnost | ShortcutDropForm | AdvancedShortcutDropForm |
|-----------|-----------------|------------------------|
| **Kreslení** | GDI+ (Paint event) | UserControl (PictureBox) |
| **Složitost** | Nižší | Vyšší |
| **Flexibilita** | Omezená | Vysoká |
| **UI elementy** | Vlastní | Tlačítka, Labels |
| **Zalamování** | Ruční | Automatické |
| **Vhodnost** | Jednoduchý náhled | Produkční aplikace |

---

## ✅ Checklist implementace

- [x] Nastavit `AllowDrop = true`
- [x] Implementovat `DragEnter` s validací .lnk
- [x] Implementovat `DragDrop` s procesováním
- [x] Volat `ShortcutLoader.LoadShortcut()`
- [x] Volat `IconExtractor.GetIconFromShortcut()`
- [x] Zmenit velikost bitmap s GDI+
- [x] Vykreslit na formuláři (Paint event)
- [x] Implementovat hit testing (klik na ikonu)
- [x] Zobrazit detaily v MessageBox

---

## 🚀 Spuštění aplikace

### 1. Zkompiluj projekt
```bash
# Visual Studio: Build → Build Solution
# Nebo: dotnet build
```

### 2. Spusť aplikaci
```bash
# F5 v Visual Studio
# Nebo: dotnet run
```

### 3. Otestuj
1. Jdi na Windows Plochu
2. Najdi nějaký zástupce (**.lnk** soubor)
3. Přetáhni do aplikace
4. Měla by se zobrazit ikona
5. Klikni na ikonu → detaily

