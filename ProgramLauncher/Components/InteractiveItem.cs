using DjSoft.Tools.ProgramLauncher.Data;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace DjSoft.Tools.ProgramLauncher.Components
{
    /// <summary>
    /// Třída reprezentující jeden vizuální prvek v rámci interaktivního controlu <see cref="InteractiveGraphicsControl"/>
    /// </summary>
    public class InteractiveItem : IChildOfParent<InteractiveGraphicsControl>
    {
        public InteractiveItem()
        {
            __Visible = true;
            __Enabled = true;
            __Interactive = true;
            __MouseDragActiveCurrentAlpha = 0.45f;
        }
        #region Public zobrazovaná data
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.MainTitle};  Adress: [{this.Adress.X},{this.Adress.Y}]; Layout: '{this.DataLayout?.Name}'";
        }
        /// <summary>
        /// Pozice prvku v matici X/Y
        /// </summary>
        public virtual Point Adress { get { return __Adress; } set { __Adress = value; ResetParentLayout(); } } private Point __Adress;
        /// <summary>
        /// Prvek je viditelný?
        /// </summary>
        public virtual bool Visible { get { return __Visible; } set { __Visible = value; } } private bool __Visible;
        /// <summary>
        /// Prvek je Enabled (false = Disabled)?
        /// </summary>
        public virtual bool Enabled { get { return __Enabled; } set { __Enabled = value; } } private bool __Enabled;
        /// <summary>
        /// Prvek je interaktivní?
        /// </summary>
        public virtual bool Interactive { get { return __Interactive; } set { __Interactive = value; } } private bool __Interactive;
        /// <summary>
        /// Barvy pozadí celé buňky. Pokud obsahuje null, nekreslí se.
        /// </summary>
        public virtual ColorSet CellBackColor { get { return __CellBackColor ?? App.CurrentAppearance.CellBackColor; } set { __CellBackColor = value; } } private ColorSet __CellBackColor;
        /// <summary>
        /// Main titulek
        /// </summary>
        public virtual string MainTitle { get { return __MainTitle; } set { __MainTitle = value; } } private string __MainTitle;
        /// <summary>
        /// Popisek
        /// </summary>
        public virtual string Description { get { return __Description; } set { __Description = value; } } private string __Description;
        /// <summary>
        /// Příznak že prvek je aktivní. Pak používá pro své vlastní pozadí barvu <see cref="ColorSet.DownColor"/>
        /// </summary>
        public virtual bool Down { get { return __Down; } set { __Down = value; } } private bool __Down;
        /// <summary>
        /// Jméno obrázku
        /// </summary>
        public virtual string ImageName { get { return __ImageName; } set { __ImageName = value; } } private string __ImageName;
        /// <summary>
        /// Data obrázku
        /// </summary>
        public virtual byte[] ImageContent { get { return __ImageContent; } set { __ImageContent = value; } } private byte[] __ImageContent;
        /// <summary>
        /// Prostor pro definiční data tohoto prvku
        /// </summary>
        public object UserData { get; set; }
        /// <summary>
        /// Prostor pro dočasné poznámky o tomto prvku
        /// </summary>
        public object Tag { get; set; }
        #endregion
        #region Vztah na Parenta = EditablePanel, a z něj navázané údaje
        /// <summary>
        /// Odkaz na Parenta
        /// </summary>
        protected InteractiveGraphicsControl Parent { get { return __Parent; } }
        /// <summary>
        /// Obsahuje true když je umístěn na Parentu
        /// </summary>
        protected bool HasParent { get { return __Parent != null; } }
        /// <summary>
        /// Zruší platnost layoutu jednotlivých prvků přítomných v Parentu
        /// </summary>
        protected virtual void ResetParentLayout()
        {
            Parent?.ResetItemLayout();
        }
        InteractiveGraphicsControl IChildOfParent<InteractiveGraphicsControl>.Parent { get { return __Parent; } set { __Parent = value; } }
        private InteractiveGraphicsControl __Parent;
        #endregion
        #region Údaje získané z Layoutu
        /// <summary>
        /// Definice layoutu: buď je lokální (specifická) podle <see cref="LayoutKind"/>, 
        /// anebo převzatá z Parenta (ten má svůj <see cref="InteractiveGraphicsControl.DefaultLayoutKind"/>).
        /// </summary>
        public virtual ItemLayoutInfo DataLayout { get { return __Parent?.GetLayout(LayoutKind); } }
        /// <summary>
        /// Layout tohoto prvku. Default = null = přebírá se z panelu, na kterém je umístěn.
        /// <para/>
        /// Definici lze setovat, pak má přednost před definicí z Parenta. Lze setovat null, tím se vrátíme k defaultní z Parenta.
        /// </summary>
        public virtual DataLayoutKind? LayoutKind { get { return __LayoutKind; } set { __LayoutKind = value; } } private DataLayoutKind? __LayoutKind;
        /// <summary>
        /// Souřadnice celého prvku ve virtuálním prostoru (tj. velikost odpovídá <see cref="InteractiveItem.CellSize"/>.
        /// Pokud prvek nemá správnou adresu <see cref="Adress"/> (záporné hodnoty), pak má <see cref="VirtualBounds"/> = null! Pak nebude ani interaktivní.
        /// Pokud prvek má adresu OK, pak má <see cref="VirtualBounds"/> přidělenou i když jeho <see cref="Visible"/> by bylo false.
        /// </summary>
        public virtual Rectangle? VirtualBounds { get { return __VirtualBounds; } protected set { __VirtualBounds = value; } } private Rectangle? __VirtualBounds;
        /// <summary>
        /// Velikost celé buňky.
        /// Základ pro tvorbu layoutu = poskládání jednotlivých prvků do matice v controlu. Používá se společně s adresou buňky <see cref="InteractiveItem.Adress"/>.
        /// <para/>
        /// Může mít zápornou šířku, pak obsazuje disponibilní šířku v controlu ("Spring").
        /// V případě, že určitý řádek (prvky na stejné adrese X) obsahuje prvky, jejichž <see cref="CellSize"/>.Width je záporné, pak tyto prvky obsadí celou šířku, 
        /// která je určena těmi řádky, které neobshaují "Spring" prvky.
        /// <para/>
        /// Nelze odvozovat šířku celého řádku od vizuálního controlu, vždy jen od fixních prvků.
        /// </summary>
        public Size CellSize { get { return __CellSize ?? this.DataLayout?.CellSize ?? Size.Empty; } set { __CellSize = value; ResetParentLayout(); } } private Size? __CellSize;
        /// <summary>
        /// Souřadnice vnitřního aktivního prostoru tohoto prvku ve virtuálním prostoru (tj. velikost odpovídá <see cref="ItemLayoutInfo.CellSize"/>.
        /// Pokud prvek nemá správnou adresu <see cref="Adress"/> (záporné hodnoty), pak má <see cref="VirtualBounds"/> = null! Pak nebude ani interaktivní.
        /// Pokud prvek má adresu OK, pak má <see cref="VirtualBounds"/> přidělenou i když jeho <see cref="Visible"/> by bylo false.
        /// </summary>
        public virtual Rectangle? VirtualContentBounds
        {
            get
            {
                Rectangle? virtualContentBounds = null;
                var virtualBounds = this.VirtualBounds;
                if (virtualBounds.HasValue)
                {
                    var dataLayout = this.DataLayout;
                    virtualContentBounds = dataLayout.ContentBounds.GetShiftedRectangle(virtualBounds.Value.Location);
                }
                return virtualContentBounds;
            }
        }
        #endregion
        #region Přepočet layoutu sady prvků
        /// <summary>
        /// Metoda vypočítá layout všech prvků v dodané kolekci = určí jejich <see cref="InteractiveItem.VirtualBounds"/> a vrátí sumární velikost obsazeného prostoru.
        /// Ta pak slouží jako <see cref="InteractiveGraphicsControl.ContentSize"/>.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="dataLayout"></param>
        /// <returns></returns>
        public static InteractiveMap RecalculateVirtualBounds(IEnumerable<InteractiveItem> items, ItemLayoutInfo dataLayout)
        {
            // Výstupní mapa prvků:
            var standardSize = dataLayout?.CellSize ?? new Size(120, 24);
            InteractiveMap currentMap = new InteractiveMap(standardSize);

            if (items.IsEmpty()) return currentMap;
            items.ForEachExec(i => i.VirtualBounds = null);                              // Resetujeme všechny buňky

            var rows = items.CreateDictionaryArray(i => i.Adress.Y);                     // Dictionary, kde Key = Adress.Y ... tedy index řádku, a Value = pole prvků DataItemBase, které se na tomto řádku nacházejí.

            
            // Sem budu dávat řádky, které mají v některém svém prvku šířku CellSize = -1 => jde o prvky, které mají mít šířku přes celý řádek, a my musíme nejprve určit šířku celého Content z ostatních řádků:
            //  Jde o List obsahující Tuple, kde Item1 = pozice Y celého řádku, a Item2 = výška celého řádku, a Item3 = pole prvků DataItemBase v tomto řádku (na stejné pozici Adress.Y).
            var springRows = new List<RecalculateSpringInfo>();

            // V první fázi zpracuji souřadnice řádků, které mají všechny exaktní šířku buňky (CellSize.Width >= 0),
            //  a ostatní řádky (obsahující buňky se zápornou šířkou) si odložím do springRows do druhé fáze:
            int adressXLast = items.Max(i => i.Adress.X);                                // Nejvyší pozice X, slouží k nouzovému výpočtu šířky
            int adressYLast = rows.Keys.Max();
            int virtualTop = 0;                                                          // Průběžná souřadnice Top pro aktuálně řešený řádek, průběžně se navyšuje o výšku řádku
             for (int adressY = 0; adressY <= adressYLast; adressY++)
            {   // Řádky se zápornou souřadnicí Y neřeším (budou null).
                // Takto vyřeším i čísla řádků (adressY), na kterých není žádný prvek = jim započítám prázdný prostor na ose Y !!!
                int rowHeight = 0;
                if (rows.TryGetValue(adressY, out var row))
                {   // Na tomto řádku jsou prvky:
                    rowHeight = row.Max(r => r.CellSize.Height);                         // Nejvyšší buňka určuje výšku celého řádku

                    bool hasSpring = row.Any(i => i.CellSize.Width < 0);
                    if (!hasSpring)
                    {   // Všechny prvky mají určenou šířku => zpracujeme je:
                        recalculateVirtualBoundsRow(row, rowHeight, adressY);
                    }
                    else
                    {   // Pokud tento řádek má nějaký prvek se zápornou šířkou prvku (=Spring), pak jej nezpracujeme nyní, ale později = ve druhé fázi:
                        springRows.Add(new RecalculateSpringInfo(adressY, virtualTop, rowHeight, row));
                    }
                }
                else
                {   // Na řádku (adressY) není ani jeden prvek: započítám prázdnou výšku:
                    rowHeight = standardSize.Height;
                }
                virtualTop += rowHeight;
            }

            // V druhé fázi zpracuji řádky Spring, s ohledem na dosud získanou maximální šířku currentMap.ContentSize.Width:
            if (springRows.Count > 0)
            {
                foreach (var springRow in springRows)
                {   // Z uložených dat každého jednotlivého Spring řádku obnovím pozici Y (proměnná virtualTop se sdílí do metody recalculateVirtualBoundsRow()),
                    //    pošlu řádek typu Spring i s jeho výškou ke zpracování.
                    //    Zpracování najde prvky typu Spring a přidělí jim šířku disponibilní do celkové šířky layoutu virtualRight:
                    // Proto se musely nejprve zpracovat řádky obsahující výhradně Fixed prvky (=určily nám sumární pevnou šířku),
                    //    a až poté Spring řádky (pro určení šířky Spring prvků využijeme disponibilní šířku z celého řádku)
                    recalculateVirtualBoundsRow(springRow.Items, springRow.Height, springRow.AdressY);
                }
            }

            return currentMap;


            // Vyřeší souřadnici X ve všech prvcích jednoho řádku:
            void recalculateVirtualBoundsRow(InteractiveItem[] recalcRow, int height, int adrY)
            {
                var columns = recalcRow.CreateDictionaryArray(i => i.Adress.X);          // Klíčem je pozice X. Value je pole prvků DataItemBase[] na stejné adrese X.

                // Určím součet pevných šířek sloupců, a součet Spring šířek (záporné hodnoty):
                int fixedWidth = 0;                                                      // Sumární šířka sloupců s konkrétní šířkou
                int springWidth = 0;                                                     // Sumární šířka sloupců s spring šířkou (jejich záporné hodnoty mohou vyjadřovat % váhu, s jakou se podělí o disponibilní prostor)
                for (int columnX = 0; columnX <= adressXLast; columnX++)
                {
                    int cellWidth = getCellWidth(columns, columnX, out var _);           // Pokud výstupem je kladné číslo, pak máme přinejmenším jeden prvek s kladnou šířkou; pokud je jich víc, pak je vrácena Max šířka
                    if (cellWidth >= 0) fixedWidth += cellWidth;
                    else springWidth += cellWidth;
                }
                decimal? springWithRatio = null;                                         // Tady v případě potřeby bude ratio, přepočítávající šířku Spring do disponibilní šířky, OnDemand.

                // Nyní do všech prvků všech sloupců vepíšu jejich šířku a tedy kompletní VirtualBounds:
                int virtualX = 0;
                for (int columnX = 0; columnX <= adressXLast; columnX++)
                {   // Sloupce se zápornou souřadnicí X neřeším (budou null).
                    // Takto vyřeším i čísla sloupců (adressX), na kterých není žádný prvek = jim započítám prázdný defaultní prostor na ose X !!!
                    Point adress = new Point(columnX, adrY);                             // Logická adresa buňky
                    int cellWidth = getCellWidth(columns, columnX, out var column);      // Pokud výstupem je kladné číslo, pak máme přinejmenším jeden prvek s kladnou šířkou; pokud je jich víc, pak je vrácena Max šířka
                    if (column != null)
                    {   // Na souřadnici X (logická adresa) máme nějaké prvky:
                        if (cellWidth < 0)
                        {   // Sloupec je typu Spring: vypočteme jeho aktuální reálnou šířku:
                            if (!springWithRatio.HasValue)
                            {   // Pokud dosud nebyl určen koeficient přepočtu šířky na virtuální pixely (springWithRatio),
                                //  tak nyní určím poměr pro přepočet disponibilní šířky pro Spring sloupce na jednotku jejich šířky:
                                int virtualRight = currentMap.ContentSize.Width;         // Dosud maximální virtuální souřadnice Right
                                // Pokud by všechny řádky obsahovaly prvek typu Spring, pak currentMap.ContentSize bude == 0, protože pro žádný řádek se nevyvolala metoda recalculateVirtualBoundsRow().
                                // Proto nyní určím šířku celkovou podle standardní velikosti buňky a maximální adresy X (počet prvků * šířka z obecného layoutu):
                                if (virtualRight <= 0)
                                    virtualRight = (adressXLast + 1) * (standardSize.Width);

                                // Přepočtový koeficient mezi springWidth (suma Spring šířek) => volný prostor šířky (virtualRight - fixedWidth):
                                springWithRatio = (springWidth >= 0 ? 0m : ((decimal)(virtualRight - fixedWidth)) / (decimal)springWidth);
                            }

                            // Fyzická šířka v pixelech pro prvek typu Spring:
                            cellWidth = (int)(Math.Round((springWithRatio.Value * (decimal)cellWidth), 0));
                            if (cellWidth < 24) cellWidth = 24;
                        }

                        // Nyní víme vše potřebné a do všech prvků této buňky vložíme jejich VirtualBounds:
                        var virtualBounds = new Rectangle(virtualX, virtualTop, cellWidth, height);
                        foreach (var item in column)
                            item.VirtualBounds = virtualBounds;

                        // A vložím souřadnici (logickou i virtuální) do mapy:
                        currentMap.Add(adress, virtualBounds, column);
                    }
                    else
                    {   // Na souřadnici X (logická adresa) není žádný prvek:
                        // Vložím prázdnou souřadnici do mapy:
                        var virtualBounds = new Rectangle(virtualX, virtualTop, cellWidth, height);
                        currentMap.Add(adress, virtualBounds, null);
                    }
                    virtualX += cellWidth;
                }
            }


            // Určí a vrátí šířku dané buňky (ze všech v ní přítomných prvků); kladná = Fixed  |  záporná = Spring.
            // Pro pozice (columnIndex) na které nejsou žádné buňky vrací šířku z DataLayout = size.Value.Width
            int getCellWidth(Dictionary<int, InteractiveItem[]> cells, int columnIndex, out InteractiveItem[] cell)
            {
                if (cells.TryGetValue(columnIndex, out cell))
                {   // Na tomto sloupci (=buňka) jsou přítomny nějaké prvky:
                    int cellFixedWidth = cell.Select(i => i.CellSize.Width).Max();      // Max šířka ze všech prvků: nejvyšší kladná určuje Fixní šířku
                    int cellSpringWidth = cell.Select(i => i.CellSize.Width).Min();     // Min šířka ze všech prvků: nejmenší záporná určuje Spring šířku
                    return ((cellFixedWidth > 0) ? cellFixedWidth : cellSpringWidth);   // Kladná šířka = Fixed má přednost před zápornou = Spring (jde o souběh více prvků v jedné buňce) = kladná šířka Fixed se vloží i do VirtualBounds sousední buňky Spring
                }
                
                // Na sloupci (adressX) není ani jeden prvek: započítám prázdnou šířku z layoutu:
                return standardSize.Width;
            }
        }
        /// <summary>
        /// Dočasná úschovna pro řádky obsahující buňky, z nichž některá je Spring
        /// </summary>
        private class RecalculateSpringInfo
        {
            public RecalculateSpringInfo(int adressY, int virtualY, int height, InteractiveItem[] items)
            {
                this.AdressY = adressY;
                this.VirtualY = virtualY;
                this.Height = height;
                this.Items = items;
            }
            /// <summary>
            /// Logická adresa Y = pořadové číslo řádku v layoutu
            /// </summary>
            public int AdressY { get; private set; }
            /// <summary>
            /// Pixelová souřadnice Y ve virtuálním prostoru
            /// </summary>
            public int VirtualY { get; private set; }
            /// <summary>
            /// Pixelová výška řádku
            /// </summary>
            public int Height { get; private set; }
            /// <summary>
            /// Všechny prvky v tomto řádku, souhrn ze všech sloupců
            /// </summary>
            public InteractiveItem[] Items { get; private set; }
        }
        #endregion
        #region Interaktivita
        /// <summary>
        /// Vrátí true, pokud tento prvek má svoji virtuální aktivní plochu na daném virtuálním bodu.
        /// Virtuální = v souřadném systému datových prvků, nikoli pixely vizuálního controlu. Mezi tím existuje posunutí dané Scrollbary.
        /// </summary>
        /// <param name="virtualPoint"></param>
        /// <returns></returns>
        public bool IsActiveOnVirtualPoint(Point virtualPoint)
        {
            var virtualContentBounds = this.VirtualContentBounds;
            return virtualContentBounds.HasValue && virtualContentBounds.Value.Contains(virtualPoint);
        }
        #endregion
        #region Kreslení
        /// <summary>
        /// Interaktivní stav. Nastavuje Control, prvek na něj jen reaguje.
        /// </summary>
        public virtual InteractiveState InteractiveState { get; set; }
        /// <summary>
        /// Zajistí vykreslení prvku
        /// </summary>
        /// <param name="e"></param>
        public virtual void Paint(PaintDataEventArgs e)
        {
            if (!HasParent || !Visible) return;

            bool paintGhost = (e.MouseDragState == MouseDragState.MouseDragActiveCurrent);       // true => kreslíme "ducha" = prvek, který je přesouván, má určitou průhlednost / nebo jen rámeček?
            var virtualBounds = (paintGhost ? e.MouseDragCurrentBounds : this.VirtualBounds);    // Kreslení "ducha" je na jiné souřadnici, než na místě prvku samotného
            if (!virtualBounds.HasValue) return;
            float? alpha = (paintGhost ? (float?)MouseDragActiveCurrentAlpha : (float?)null);

            var dataLayout = this.DataLayout;
            var paletteSet = App.CurrentAppearance;
            var clientBounds = this.Parent.GetControlBounds(virtualBounds.Value);
            var clientLocation = clientBounds.Location;
            var activeBounds = dataLayout.ContentBounds.GetShiftedRectangle(clientLocation);
            var workspaceColor = App.CurrentAppearance.WorkspaceColor;

            Color? color;
            e.Graphics.SetClip(clientBounds);

            InteractiveState interactiveState = this.InteractiveState;

            // Celé pozadí buňky (buňka může mít explicitně danou barvu pozadí):
            color = this.CellBackColor?.GetColor(interactiveState);
            if (color.HasValue)
            {   // Barva buňky se smíchá s barvou WorkspaceColor a vykreslí se celé její pozadí,
                // a tato barva se pak stává základnou pro Morphování a kreslení všech dalších barev v různých oblastech:
                workspaceColor = workspaceColor.Morph(color.Value);
                e.Graphics.FillRectangle(clientBounds, workspaceColor, alpha);
            }
            // Pozadí aktivní části buňky:
            if (this.Down)
            {
                color = paletteSet.ActiveContentColor.DownColor;
                if (color.HasValue)
                    e.Graphics.FillRectangle(activeBounds, workspaceColor.Morph(color.Value), alpha);
            }

            // Podkreslení celé buňky v myšoaktivním stavu:
            if ((interactiveState == InteractiveState.MouseOn || interactiveState == InteractiveState.MouseDown) && paletteSet.ActiveContentColor != null)
            {
                color = paletteSet.ActiveContentColor.GetColor(interactiveState);
                if (color.HasValue)
                    e.Graphics.FountainFill(activeBounds, workspaceColor.Morph(color.Value), Components.InteractiveState.Default, alpha);
            }

            // Rámeček a pozadí typu Border:
            var borderBounds = dataLayout.BorderBounds.GetShiftedRectangle(clientLocation);
            if (borderBounds.HasContent())
            {
                using (var borderPath = borderBounds.GetRoundedRectanglePath(dataLayout.BorderRound))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    // Výplň dáme pod border:
                    color = paletteSet.ButtonBackColors.GetColor(interactiveState);
                    if (color.HasValue)
                        e.Graphics.FountainFill(borderPath, workspaceColor.Morph(color.Value), interactiveState, alpha);

                    // Linka Border:
                    if (dataLayout.BorderWidth > 0f)
                    {
                        var pen = App.GetPen(paletteSet.BorderLineColors, interactiveState, dataLayout.BorderWidth, alpha);
                        if (pen != null) e.Graphics.DrawPath(pen, borderPath);
                    }
                }
            }

            // Zvýraznit pozici myši:
            if (interactiveState == InteractiveState.MouseOn && dataLayout.MouseHighlightSize.HasContent() && paletteSet.ButtonBackColors.MouseHighlightColor.HasValue)
            {
                using (GraphicsPath mousePath = new GraphicsPath())
                {
                    var mousePoint = e.MouseState.LocationControl;
                    var mouseBounds = mousePoint.GetRectangleFromCenter(dataLayout.MouseHighlightSize);
                    new Rectangle(mousePoint.X - 24, mousePoint.Y - 16, 48, 32);
                    mousePath.AddEllipse(mouseBounds);
                    using (System.Drawing.Drawing2D.PathGradientBrush pgb = new PathGradientBrush(mousePath))       // points
                    {
                        pgb.CenterPoint = mousePoint;
                        pgb.CenterColor = workspaceColor.Morph(paletteSet.ButtonBackColors.MouseHighlightColor).GetAlpha(alpha);
                        pgb.SurroundColors = new Color[] { Color.Transparent, Color.Transparent, Color.Transparent, Color.Transparent };
                        e.Graphics.FillPath(pgb, mousePath);
                    }
                }
            }

            // Vykreslit Image:
            var image = App.GetImage(this.ImageName, this.ImageContent);
            if (dataLayout.ImageBounds.HasContent() && image != null)
            {
                e.Graphics.ResetClip();
                e.Graphics.SmoothingMode = SmoothingMode.None;
                var imageBounds = dataLayout.ImageBounds.GetShiftedRectangle(clientLocation);
                e.Graphics.DrawImage(image, imageBounds, alpha);
            }

            // Vypsat text:
            if (dataLayout.MainTitleBounds.HasContent() && !String.IsNullOrEmpty(this.MainTitle))
            {
                var mainTitleBounds = dataLayout.MainTitleBounds.GetShiftedRectangle(clientLocation);
                e.Graphics.DrawText(this.MainTitle, mainTitleBounds, dataLayout.MainTitleAppearance, interactiveState, alpha);
            }

            e.Graphics.ResetClip();
        }
        /// <summary>
        /// Hodnota průhlednosti pro kreslení přesouvaného prvku v režimu Mouse DragAndDrop.
        /// 0 = neviditelný / 1 = plně viditelný. Defaultní = 0.45
        /// </summary>
        public float MouseDragActiveCurrentAlpha { get { return __MouseDragActiveCurrentAlpha; } set { __MouseDragActiveCurrentAlpha = (value < 0f ? 0f : (value > 1f ? 1f : value)); } } private float __MouseDragActiveCurrentAlpha;
        #endregion
    }
    #region class InteractiveMap = mapa interaktivních prvků ve Virtual souřadnicích
    /// <summary>
    /// <see cref="InteractiveMap"/> = mapa interaktivních prvků ve Virtual souřadnicích
    /// </summary>
    public class InteractiveMap
    {
        public InteractiveMap(Size standardSize)
        {
            __StandardSize = standardSize;
            __Cells = new List<Cell>();
        }
        /// <summary>
        /// Přidá další buňku pro danou logickou a virtuální souřadnici
        /// </summary>
        /// <param name="adress"></param>
        /// <param name="virtualBounds"></param>
        /// <param name="items"></param>
        public void Add(Point adress, Rectangle virtualBounds, InteractiveItem[] items)
        {
            int right = virtualBounds.Right;
            int bottom = virtualBounds.Bottom;
            if (__VirtualRight < right) __VirtualRight = right;
            if (__VirtualBottom < bottom) __VirtualBottom = bottom;

            __Cells.Add(new Cell(adress, virtualBounds, items));
        }
        /// <summary>
        /// Najde a vrátí buňku na dané virtuální souřadnici. Může vráit null.
        /// </summary>
        /// <param name="virtualPoint"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal Cell GetCellAtPoint(Point virtualPoint)
        {
            if (virtualPoint.X < 0 || virtualPoint.X >= __VirtualRight) return null;
            if (virtualPoint.Y < 0 || virtualPoint.Y >= __VirtualBottom) return null;

            return __Cells.FirstOrDefault(c => c.VirtualBounds.Contains(virtualPoint));
        }

        private Size __StandardSize;
        private List<Cell> __Cells;
        private int __VirtualRight;
        private int __VirtualBottom;
        public Size ContentSize { get { return new Size(__VirtualRight, __VirtualBottom); } }
        public class Cell
        {
            public Cell(Point adress, Rectangle virtualBounds, InteractiveItem[] items)
            {
                __Adress = adress;
                __VirtualBounds = virtualBounds;
                __Items = items;
            }
            public Point Adress { get { return __Adress; } } private Point __Adress;
            public Rectangle VirtualBounds { get { return __VirtualBounds; } } private Rectangle __VirtualBounds;
            public InteractiveItem[] Items { get { return __Items; } } private InteractiveItem[] __Items;
        }
    }
    #endregion
    #region Enumy
    /// <summary>
    /// Stav prvku
    /// </summary>
    public enum InteractiveState
    {
        Default = 0,
        Disabled,
        Enabled,
        MouseOn,
        MouseDown
    }
    #endregion
    #region class PaintDataEventArgs
    /// <summary>
    /// Argument pro kreslení dat
    /// </summary>
    public class PaintDataEventArgs : EventArgs, IDisposable
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="e"></param>
        /// <param name="mouseState"></param>
        /// <param name="virtualContainer"></param>
        public PaintDataEventArgs(PaintEventArgs e, Components.MouseState mouseState, Components.IVirtualContainer virtualContainer)
        {
            __Graphics = new WeakReference<Graphics>(e.Graphics);
            __ClipRectangle = e.ClipRectangle;
            __MouseState = mouseState;
            __VirtualContainer = virtualContainer;
        }
        private WeakReference<Graphics> __Graphics;
        private Rectangle __ClipRectangle;
        private Rectangle? __MouseDragCurrentBounds;
        private MouseDragState __MouseDragState;
        private Components.MouseState __MouseState;
        private Components.IVirtualContainer __VirtualContainer;
        void IDisposable.Dispose()
        {
            __Graphics = null;
        }
        /// <summary>
        /// Gets the graphics used to paint. <br/>
        /// The System.Drawing.Graphics object used to paint. The System.Drawing.Graphics object provides methods for drawing objects on the display device.
        /// </summary>
        public Graphics Graphics { get { return (__Graphics.TryGetTarget(out var graphics) ? graphics : null); } }
        /// <summary>
        /// Gets the rectangle in which to paint. <br/>
        /// The System.Drawing.Rectangle in which to paint.
        /// </summary>
        public Rectangle ClipRectangle { get { return __ClipRectangle; } }
        /// <summary>
        /// Souřadnice aktivního prvku, kam by byl přesunut v procesu Mouse DragAndDrop, když <see cref="MouseDragState"/> je <see cref="MouseDragState.MouseDragActiveCurrent"/>
        /// </summary>
        public Rectangle? MouseDragCurrentBounds { get { return __MouseDragCurrentBounds; } set { __MouseDragCurrentBounds = value; } }
        /// <summary>
        /// Stav procesu Mouse DragAndDrop pro aktuální vykreslovaný prvek
        /// </summary>
        public MouseDragState MouseDragState { get { return __MouseDragState; } set { __MouseDragState = value; } }
        /// <summary>
        /// Pozice a stav myši
        /// </summary>
        public Components.MouseState MouseState { get { return __MouseState; } }
        /// <summary>
        /// Virtuální kontejner, do kterého je kresleno
        /// </summary>
        public Components.IVirtualContainer VirtualContainer { get { return __VirtualContainer; } }
    }
    /// <summary>
    /// Stav procesu Mouse DragAndDrop
    /// </summary>
    public enum MouseDragState
    {
        /// <summary>
        /// Nejedná se o Mouse DragAndDrop
        /// </summary>
        None,
        /// <summary>
        /// Aktuální vykreslovaný prvek je "pod" myší v procesu Mouse DragAndDrop = jde o běžný prvek, který není přesouván, ale leží na místě, kde se nachází myš v tomto procesu
        /// </summary>
        MouseDragTarget,
        /// <summary>
        /// Aktuální vykreslovaný prvek je ten, který se přesouvá v procesu Mouse DragAndDrop.
        /// V tomto stavu se má vykreslit ve své původní pozici (Source).
        /// </summary>
        MouseDragActiveOriginal,
        /// <summary>
        /// Aktuální vykreslovaný prvek je ten, který se přesouvá v procesu Mouse DragAndDrop.
        /// V tomto stavu se má vykreslit ve své cílové pozici, kde je zrovna umístěn při přetažení myší.
        /// Pak se má pro kreslení použít souřadnice <see cref="PaintDataEventArgs.MouseDragCurrentBounds"/>
        /// </summary>
        MouseDragActiveCurrent
    }
    #endregion
}
