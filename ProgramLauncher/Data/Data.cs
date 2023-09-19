using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Drawing;
using DjSoft.Tools.ProgramLauncher.Components;
using static DjSoft.Tools.ProgramLauncher.App;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    public class DataItemGroup : DataItemBase
    {
    }
    public class DataItemApplication : DataItemBase
    {
    }
    public abstract class DataItemBase : IChildOfParent<InteractiveGraphicsControl>
    {
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.DataLayout?.Name} {this.MainTitle}";
        }
        /// <summary>
        /// Pozice prvku v matici X/Y
        /// </summary>
        public Point Adress { get { return __Adress; } set { __Adress = value; ResetParentLayout(); } } private Point __Adress;
        /// <summary>
        /// Prvek je viditelný?
        /// </summary>
        public bool Visible { get { return !__InVisible; } set { __InVisible = !value; } } private bool __InVisible;             // Nemám rád inicializaci proměnných v jejich deklaraci, a nemám tady ani konstruktor. Tak nechám defaultně Invisible = false a používám Visible = Not Invisible.
        /// <summary>
        /// Barvy pozadí celé buňky. Pokud obsahuje null, nekreslí se.
        /// </summary>
        public ColorSet CellBackColor { get { return __CellBackColor ?? App.CurrentAppearance.CellBackColor; } set { __CellBackColor = value; } } private ColorSet __CellBackColor;

        public virtual string MainTitle { get; set; }

        /// <summary>
        /// Příznak že prvek je aktivní. Pak používá pro své vlastní pozadí barvu <see cref="ColorSet.ActiveColor"/>
        /// </summary>
        public virtual bool IsActive { get; set; }
        public virtual string ImageName { get; set; }
        public virtual byte[] ImageContent { get; set; }

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
        /// Definice layoutu: buď je lokální (specifická), anebo převzatá z Parenta. 
        /// Parent je <see cref="InteractiveGraphicsControl"/>, ten má svůj daný layout.
        /// <para/>
        /// Definici lze setovat, pak má přednost před definicí z Parenta. Lze setovat null, tím se vrátíme k defaultní z Parenta.
        /// </summary>
        public virtual DataLayout DataLayout { get { return __DataLayout ?? __Parent?.DataLayout; } set { __DataLayout = value; ResetParentLayout(); } } private DataLayout __DataLayout;
        /// <summary>
        /// Souřadnice celého prvku ve virtuálním prostoru (tj. velikost odpovídá <see cref="DataItemBase.CellSize"/>.
        /// Pokud prvek nemá správnou adresu <see cref="Adress"/> (záporné hodnoty), pak má <see cref="VirtualBounds"/> = null! Pak nebude ani interaktivní.
        /// Pokud prvek má adresu OK, pak má <see cref="VirtualBounds"/> přidělenou i když jeho <see cref="Visible"/> by bylo false.
        /// </summary>
        public virtual Rectangle? VirtualBounds { get { return __VirtualBounds; } protected set { __VirtualBounds = value; } } private Rectangle? __VirtualBounds;
        /// <summary>
        /// Velikost celé buňky.
        /// Základ pro tvorbu layoutu = poskládání jednotlivých prvků do matice v controlu. Používá se společně s adresou buňky <see cref="DataItemBase.Adress"/>.
        /// <para/>
        /// Může mít zápornou šířku, pak obsazuje disponibilní šířku v controlu ("Spring").
        /// V případě, že určitý řádek (prvky na stejné adrese X) obsahuje prvky, jejichž <see cref="CellSize"/>.Width je záporné, pak tyto prvky obsadí celou šířku, 
        /// která je určena těmi řádky, které neobshaují "Spring" prvky.
        /// <para/>
        /// Nelze odvozovat šířku celého řádku od vizuálního controlu, vždy jen od fixních prvků.
        /// </summary>
        public Size CellSize { get { return __CellSize ?? this.DataLayout?.CellSize ?? Size.Empty; } set { __CellSize = value; ResetParentLayout(); } } private Size? __CellSize;
        /// <summary>
        /// Souřadnice vnitřního aktivního prostoru tohoto prvku ve virtuálním prostoru (tj. velikost odpovídá <see cref="DataLayout.CellSize"/>.
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
        /// Metoda vypočítá layout všech prvků v dodané kolekci = určí jejich <see cref="DataItemBase.VirtualBounds"/> a vrátí sumární velikost obsazeného prostoru.
        /// Ta pak slouží jako <see cref="InteractiveGraphicsControl.ContentSize"/>.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="dataLayout"></param>
        /// <returns></returns>
        public static Size RecalculateVirtualBounds(IEnumerable<DataItemBase> items, DataLayout dataLayout)
        {
            if (items.IsEmpty()) return Size.Empty;
            items.ForEachExec(i => i.VirtualBounds = null);                              // Resetujeme všechny
            var size = dataLayout?.CellSize;

            var rows = items.CreateDictionaryArray(i => i.Adress.Y);                     // Dictionary, kde Key = Adress.Y ... tedy index řádku, a Value = pole prvků DataItemBase, které se na tomto řádku nacházejí.

            // Sem budu dávat řádky, které mají v některém svém prvku šířku CellSize = -1 => jde o prvky, které mají mít šířku přes celý řádek, a my musíme nejprve určit šířku celého Content z ostatních řádků:
            //  Jde o List obsahující Tuple, kde Item1 = pozice Y celého řádku, a Item2 = výška celého řádku, a Item3 = pole prvků DataItemBase v tomto řádku (na stejné pozici Adress.Y).
            var springRows = new List<Tuple<int, int, DataItemBase[]>>();

            // V první fázi zpracuji souřadnice řádků, které mají všechny exaktní šířku buňky (CellSize.Width >= 0),
            //  a ostatní řádky (obsahující buňky se zápornou šířkou) si odložím do springRows do druhé fáze:
            int adressXLast = items.Max(i =>i.Adress.X);                                 // Nejvyší pozice X, slouží k nouzovému výpočtu šířky
            int adressYLast = rows.Keys.Max();
            int virtualTop = 0;                                                          // Průběžná souřadnice Top pro aktuálně řešený řádek, průběžně se navyšuje o výšku řádku
            int virtualRight = 0;                                                        // Souřadnice X použitá některým řádkem, nejvíce vpravo
            int virtualBottom = 0;                                                       // Souřadnice X použitá posledním řádkem, nejvíce dole
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
                        recalculateVirtualBoundsRow(row, rowHeight);
                    }
                    else
                    {   // Pokud tento řádek má nějaký prvek se zápornou šířkou prvku (=Spring), pak jej nezpracujeme nyní ale ve druhé fázi:
                        springRows.Add(new Tuple<int, int, DataItemBase[]>(virtualTop, rowHeight, row));
                    }
                }
                else
                {   // Na řádku (adressY) není ani jeden prvek: započítám prázdnou výšku:
                    if (size.HasValue)
                        rowHeight = size.Value.Height;
                }
                virtualTop += rowHeight;
            }
            virtualBottom = virtualTop;                                                    // Aktuálně nalezená souřadnice virtualY určuje dolní souřadnici celého obsahu

            // V druhé fázi zpracuji řádky Spring, s ohledem na dosud získanou šířku virtualRight:
            if (springRows.Count > 0)
            {
                // Pokud by všechny řádky obsahovaly prvek typu Spring, pak bude virtualRight == 0, protože pro žádný řádek se nevyvolala metoda recalculateVirtualBoundsRow().
                //  Tato metoda by posunula hodnotu virtualRight na konec posledního prvku (jeho Right).
                if (virtualRight <= 0)
                    // Proto určíme nouzovou šířku = počet prvků * šířka z obecného layoutu:
                    virtualRight = (adressXLast + 1) * (size.HasValue ? size.Value.Width : 64);

                foreach (var springRow in springRows)
                {   // Z uložených dat každého jednotlivého Spring řádku obnovím pozici Y (proměnná virtualTop se sdílí do metody recalculateVirtualBoundsRow()),
                    //    pošlu řádek typu Spring i s jeho výškou ke zpracování.
                    //    Zpracování najde prvky typu Spring a přidělí jim šířku disponibilní do celkové šířky layoutu virtualRight:
                    // Proto se musely nejprve zpracovat čisté Fixed řádky (určily pevnou šířku) a až poté Spring řádky (využijí 
                    virtualTop = springRow.Item1;
                    int rowHeight = springRow.Item2;
                    recalculateVirtualBoundsRow(springRow.Item3, springRow.Item2);
                }
            }

            return new Size(virtualRight, virtualBottom);


            // Vyřeší souřadnici X ve všech prvcích jednoho řádku:
            void recalculateVirtualBoundsRow(DataItemBase[] recalcRow, int height)
            {
                var columns = recalcRow.CreateDictionaryArray(i => i.Adress.X);          // Klíčem je pozice X. Value je pole prvků DataItemBase[] na stejné adrese X.
                int columnXTop = columns.Keys.Max();

                // Určím součet pevných šířek sloupců, a součet Spring šířek (záporné hodnoty):
                int fixedWidth = 0;                                                      // Sumární šířka sloupců s konkrétní šířkou
                int springWidth = 0;                                                     // Sumární šířka sloupců s spring šířkou (jejich záporné hodnoty mohou vyjadřovat % váhu, s jakou se podělí o disponibilní prostor)
                for (int columnX = 0; columnX <= columnXTop; columnX++)
                {
                    int cellWidth = getCellWidth(columns, columnX, out var _);           // Pokud výstupem je kladné číslo, pak máme přinejmenším jeden prvek s kladnou šířkou; pokud je jich víc, pak je vrácena Max šířka
                    if (cellWidth >= 0) fixedWidth += cellWidth;
                    else springWidth += cellWidth;
                }
                decimal? springWithRatio = null;                                         // Tady v případě potřeby bude ratio, přepočítávající šířku Spring do disponibilní šířky, OnDemand.

                // Nyní do všech prvků všech sloupců vepíšu jejich šířku a tedy kompletní VirtualBounds:
                int virtualX = 0;
                for (int columnX = 0; columnX <= columnXTop; columnX++)
                {   // Sloupce se zápornou souřadnicí X neřeším (budou null).
                    // Takto vyřeším i čísla sloupců (adressX), na kterých není žádný prvek = jim započítám prázdný defaultní prostor na ose X !!!
                    int cellWidth = getCellWidth(columns, columnX, out var column);      // Pokud výstupem je kladné číslo, pak máme přinejmenším jeden prvek s kladnou šířkou; pokud je jich víc, pak je vrácena Max šířka
                    if (column != null)
                    {
                        if (cellWidth < 0)
                        {   // Sloupec je typu Spring: vypočteme jeho aktuální reálnou šířku:

                            // Pokud dosud nebyl určen, tak nyní určím poměr pro přepočet disponibilní šířky pro Spring sloupce na jednotku jejich šířky:
                            if (!springWithRatio.HasValue)
                                springWithRatio = (springWidth >= 0 ? 0m : ((decimal)(virtualRight - fixedWidth)) / (decimal)springWidth);

                            cellWidth = (int)(Math.Round((springWithRatio.Value * (decimal)cellWidth), 0));
                            if (cellWidth < 24) cellWidth = 24;
                        }
                        // Nyní víme vše potřebné a do všech prvků této buňky vložíme jejich VirtualBounds:
                        var virtualBounds = new Rectangle(virtualX, virtualTop, cellWidth, height);
                        foreach (var item in column)
                            item.VirtualBounds = virtualBounds;

                    }
                    virtualX += cellWidth;
                }
                if (virtualRight < virtualX) virtualRight = virtualX;                   // Souřadnice X za posledním přítomným sloupcem je Right celého obsahu, střádáme její Max
            }


            // Určí a vrátí šířku dané buňky (ze všech v ní přítomných prvků); kladná = Fixed  |  záporná = Spring.
            // Pro pozice (columnIndex) na které nejsou žádné buňky vrací šířku z DataLayout = size.Value.Width
            int getCellWidth(Dictionary<int, DataItemBase[]> cells, int columnIndex, out DataItemBase[] cell)
            {
                if (cells.TryGetValue(columnIndex, out cell))
                {   // Na tomto sloupci (=buňka) jsou přítomny nějaké prvky:
                    int cellFixedWidth = cell.Select(i => i.CellSize.Width).Max();      // Max šířka ze všech prvků: nejvyšší kladná určuje Fixní šířku
                    int cellSpringWidth = cell.Select(i => i.CellSize.Width).Min();     // Min šířka ze všech prvků: nejmenší záporná určuje Spring šířku
                    return ((cellFixedWidth > 0) ? cellFixedWidth : cellSpringWidth);   // Kladná šířka = Fixed má přednost před zápornou = Spring (jde o souběh více prvků v jedné buňce) = kladná šířka Fixed se vloží i do VirtualBounds sousední buňky Spring
                }
                else if (size.HasValue)
                {   // Na sloupci (adressX) není ani jeden prvek: započítám prázdnou šířku:
                    return size.Value.Width;
                }
                return 0;
            }
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

            var virtualBounds = this.VirtualBounds;
            if (!virtualBounds.HasValue) return;

            var dataLayout = this.DataLayout;
            var paletteSet = App.CurrentAppearance;
            var clientBounds = this.Parent.GetControlBounds(virtualBounds.Value);
            var clientLocation = clientBounds.Location;
            var activeBounds = dataLayout.ContentBounds.GetShiftedRectangle(clientLocation);
            Color? color;
            e.Graphics.SetClip(activeBounds);

            InteractiveState interactiveState = this.InteractiveState;

            // Celé pozadí buňky (buňka může mít explicitně danou barvu pozadí):
            color = this.CellBackColor?.GetColor(interactiveState);
            if (color.HasValue)
                e.Graphics.FillRectangle(clientBounds, color.Value);

            // Pozadí aktivní části buňky:
            if (this.IsActive)
            {
                color = paletteSet.ActiveContentColor.ActiveColor;
                if (color.HasValue)
                    e.Graphics.FillRectangle(activeBounds, color.Value);
            }

            // Podkreslení celé buňky v myšoaktivním stavu:
            if ((interactiveState == InteractiveState.MouseOn || interactiveState == InteractiveState.MouseDown) && paletteSet.ActiveContentColor != null)
            {
                color = paletteSet.ActiveContentColor.GetColor(interactiveState);
                if (color.HasValue)
                    e.Graphics.FountainFill(activeBounds, color.Value);
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
                        e.Graphics.FountainFill(borderPath, color.Value, interactiveState);

                    // Linka Border:
                    if (dataLayout.BorderWidth > 0f)
                    {
                        var pen = App.GetPen(paletteSet.BorderLineColors, interactiveState, dataLayout.BorderWidth);
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
                        pgb.CenterColor = paletteSet.ButtonBackColors.MouseHighlightColor.Value;
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
                e.Graphics.DrawImage(image, imageBounds);
            }

            // Vypsat text:
            if (dataLayout.MainTitleBounds.HasContent() && !String.IsNullOrEmpty(this.MainTitle))
            {
                var mainTitleBounds = dataLayout.MainTitleBounds.GetShiftedRectangle(clientLocation);
                e.Graphics.DrawText(this.MainTitle, mainTitleBounds, dataLayout.MainTitleAppearance, interactiveState);
            }

            e.Graphics.ResetClip();
        }
        #endregion
    }
    #region class DataLayout = Layout prvku: rozmístění, velikost, styl písma
    /// <summary>
    /// Layout prvku: rozmístění, velikost, styl písma
    /// </summary>
    public class DataLayout
    {
        #region Public properties
        /// <summary>
        /// Jméno stylu
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Velikost celé buňky.
        /// Základ pro tvorbu layoutu = poskládání jednotlivých prvků do matice v controlu. Používá se společně s adresou buňky <see cref="DataItemBase.Adress"/>.
        /// <para/>
        /// Může mít zápornou šířku, pak obsazuje disponibilní šířku v controlu ("Spring").
        /// V případě, že určitý řádek (prvky na stejné adrese X) obsahuje prvky, jejichž <see cref="CellSize"/>.Width je záporné, pak tyto prvky obsadí celou šířku, 
        /// která je určena těmi řádky, které neobshaují "Spring" prvky.
        /// <para/>
        /// Nelze odvozovat šířku celého řádku od vizuálního controlu, vždy jen od fixních prvků.
        /// </summary>
        public Size CellSize { get; set; }
        /// <summary>
        /// Souřadnice aktivního prostoru pro data: v tomto prostoru je obsah myšo-aktivní.
        /// Vnější prostor okolo těchto souřadnic je prázdný a neaktivní, odděluje od sebe sousední buňky.
        /// <para/>
        /// V tomto prostoru se stínuje pozice myši barvou <see cref="ButtonBackColors"/> : <see cref="ColorSet.MouseHighlightColor"/>.
        /// </summary>
        public Rectangle ContentBounds { get; set; }
        /// <summary>
        /// Souřadnice prostoru s okrajem a vykresleným pozadím.
        /// V tomto prostoru je použita barva <see cref="BorderLineColors"/> a <see cref="ButtonBackColors"/>, 
        /// border má šířku <see cref="BorderWidth"/> a kulaté rohy <see cref="BorderRound"/>.
        /// <para/>
        /// Texty mohou být i mimo tento prostor.
        /// </summary>
        public Rectangle BorderBounds { get; set; }
        /// <summary>
        /// Zaoblení Borderu, 0 = ostře hranatý
        /// </summary>
        public int BorderRound { get; set; }
        /// <summary>
        /// Šířka linky Borderu, 0 = nekreslí se
        /// </summary>
        public float BorderWidth { get; set; }
        /// <summary>
        /// Souřadnice prostoru pro ikonu
        /// </summary>
        public Rectangle ImageBounds { get; set; }
        /// <summary>
        /// Velikost prostoru stínování myši, lze zakázat zadáním prázdného prostoru
        /// </summary>
        public Size MouseHighlightSize { get; set; }

        /// <summary>
        /// Souřadnice prostoru pro hlavní text
        /// </summary>
        public Rectangle MainTitleBounds { get; set; }
        public AppearanceTextPartType? MainTitleAppearanceType { get; set; }
        /// <summary>
        /// Vzhled hlavního textu
        /// </summary>
        public TextAppearance MainTitleAppearance 
        { 
            get { return __MainTitleAppearance ?? App.CurrentAppearance.GetTextAppearance(MainTitleAppearanceType ?? AppearanceTextPartType.MainTitle); } 
            set { __MainTitleAppearance = value; } } 
        private TextAppearance __MainTitleAppearance;
        #endregion
        #region Statické konstruktory konkrétních stylů
        /// <summary>
        /// Menší obdélník
        /// </summary>
        public static DataLayout SetSmallBrick
        {
            get
            {
                DataLayout dataLayout = new DataLayout()
                {
                    Name = "Menší cihla",
                    CellSize = new Size(160, 48),
                    ContentBounds = new Rectangle(2, 2, 156, 44),
                    BorderBounds = new Rectangle(4, 4, 40, 40),
                    MouseHighlightSize = new Size(40, 24),
                    BorderRound = 4,
                    BorderWidth = 1f,
                    ImageBounds = new Rectangle(8, 8, 24, 24),
                    MainTitleBounds = new Rectangle(46, 14, 95, 20),
                    MainTitleAppearanceType = AppearanceTextPartType.MainTitle
                };
                return dataLayout;
            }
        }
        /// <summary>
        /// Střední obdélník
        /// </summary>
        public static DataLayout SetMidiBrick
        {
            get
            {
                DataLayout dataLayout = new DataLayout()
                {
                    Name = "Menší cihla",
                    CellSize = new Size(160, 64),
                    ContentBounds = new Rectangle(2, 2, 156, 60),
                    BorderBounds = new Rectangle(4, 4, 56, 56),
                    MouseHighlightSize = new Size(40, 24),
                    BorderRound = 4,
                    BorderWidth = 1f,
                    ImageBounds = new Rectangle(8, 8, 48, 48),
                    MainTitleBounds = new Rectangle(62, 24, 95, 20),
                    MainTitleAppearanceType = AppearanceTextPartType.SubTitle
                };
                return dataLayout;
            }
        }
        /// <summary>
        /// Středně velký obdélník
        /// </summary>
        public static DataLayout SetMediumBrick
        {
            get
            {
                DataLayout dataLayout = new DataLayout()
                {
                    Name = "Střední cihla",
                    CellSize = new Size(180, 92),
                    ContentBounds = new Rectangle(4, 4, 173, 85),
                    BorderBounds = new Rectangle(14, 14, 64, 64),
                    MouseHighlightSize = new Size(48, 32),
                    BorderRound = 6,
                    BorderWidth = 1f,
                    ImageBounds = new Rectangle(22, 22, 48, 48),
                    MainTitleBounds = new Rectangle(82, 18, 95, 20),
                    MainTitleAppearanceType = AppearanceTextPartType.MainTitle
                };
                return dataLayout;
            }
        }
        /// <summary>
        /// Středně velký titulek
        /// </summary>
        public static DataLayout SetTitle
        {
            get
            {
                DataLayout dataLayout = new DataLayout()
                {
                    Name = "Střední titulek",
                    CellSize = new Size(-1, 24),
                    ContentBounds = new Rectangle(0, 0, 200, 24),
                    MainTitleBounds = new Rectangle(0, 0, 200, 24),
                    MainTitleAppearanceType = AppearanceTextPartType.MainTitle
                };
                return dataLayout;
            }
        }
        
        #endregion
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
        /// Pozice a stav myši
        /// </summary>
        public Components.MouseState MouseState { get { return __MouseState; } }
        /// <summary>
        /// Virtuální kontejner, do kterého je kresleno
        /// </summary>
        public Components.IVirtualContainer VirtualContainer { get { return __VirtualContainer; } }
    }
    #endregion
}
