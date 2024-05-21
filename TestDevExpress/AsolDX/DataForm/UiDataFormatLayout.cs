// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Noris.WS.DataContracts.DxForm;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    /// <summary>
    /// Třída, která vypočítá layout celého formuláře = všech jeho stránek a panelů.
    /// Layout = rozmístění prvků v rámci panelů, a velikost vlastních panelů.
    /// Neprovádí se ale umístění panelů do stránek, to už závisí i na rozměru fyzického controlu, to provádí klientský DataForm.
    /// </summary>
    internal static class DfTemplateLayout
    {
        /// <summary>
        /// Vytvoří layout jednotlivých panelů, určí jejich velikost.
        /// </summary>
        /// <param name="args">Data a parametry pro tvornu layoutu</param>
        internal static void CreateLayout(DfTemplateLayoutArgs args)
        {
            _CreateLayout(args);
        }
        /// <summary>
        /// Vytvoří layout jednotlivých panelů, určí jejich velikost.
        /// </summary>
        /// <param name="args">Data a parametry pro tvorbu layoutu</param>
        private static void _CreateLayout(DfTemplateLayoutArgs args)
        {
            if (args is null) throw new ArgumentNullException($"DataForm.DfTemplateLayout.CreateLayout() : args is null.");
            if (args.DataForm is null) throw new ArgumentNullException($"DataForm.DfTemplateLayout.CreateLayout() : DataForm is null.");
            if (args.InfoSource is null) throw new ArgumentNullException($"DataForm.DfTemplateLayout.CreateLayout() : InfoSource is null.");

            // Pro Parent objekty (Form, Page, Panel) nepočítám jejich souřadnice = ty je nemají.
            // Ale střádám si jejich hierarchicky definovaný styl (LayoutStyle), který následně používám pro tvorbu layoutu uvnitř panelu:
            var dfForm = args.DataForm;
            if (dfForm != null && dfForm.Pages != null)
            {
                StyleInfo styleForm = new StyleInfo(dfForm);
                foreach (var dfPage in dfForm.Pages)
                {
                    if (dfPage != null && dfPage.Panels != null)
                    {
                        StyleInfo stylePage = new StyleInfo(dfPage, styleForm);
                        foreach (var dfPanel in dfPage.Panels)
                        {   // Pro panel budu počítat rozmístění vnitřních prvků a následně i rozměry panelu
                            StyleInfo stylePanel = new StyleInfo(dfPanel, stylePage);
                            using (var panelItem = ItemInfo.CreateRoot(dfPanel, args, stylePanel))
                            {
                                panelItem.ProcessPanel();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validuje (prověří a doplní) informace o konkrétním controlu.
        /// </summary>
        /// <param name="controlInfo">Data o controlu</param>
        /// <returns></returns>
        private static void ValidateControlInfo(ControlInfo controlInfo)
        {
            switch (controlInfo.ControlType)
            {
                case ControlType.Button: setControlSize(200, 35); break;
                case ControlType.BarCode: setControlSize(96, 96); break;
                case ControlType.Image: setControlSize(96, 96); break;
                case ControlType.EditBox: setControlSize(250, 96); break;
                default:
                    string name = (controlInfo.ColumnName ?? "").Trim().ToLower();
                    if (name.EndsWith("_refer")) setControlSize(120, 20); 
                    if (name.EndsWith("_nazev")) setControlSize(250, 20);
                    break;
            }
            

            // Vloží ControlSize
            void setControlSize(int width, int height)
            {
                if (!controlInfo.ControlWidth.HasValue) controlInfo.ControlWidth = width;
                if (!controlInfo.ControlHeight.HasValue) controlInfo.ControlHeight = height;
            }
        }

        /// <summary>
        /// Sestaví fyzický layout pro prvky v daném containeru, určí velikost containeru
        /// </summary>
        /// <param name="args">Data a parametry pro tvorbu layoutu</param>
        /// <param name="dfPanel"></param>
        /// <param name="styleContainer">Styl pro daný panel. Panel si jej sám nevytvoří, protože musí mít k dispozici styl parenta (dědičnost pro implicitní hodnoty!)</param>
        private static void _CreateLayoutPanel(DfTemplateLayoutArgs args, DfPanel dfPanel, StyleInfo styleContainer)
        {
            //if (dfPanel is null) return;

            //using (var rootItem = ItemInfo.CreateRoot(dfPanel, args, styleContainer))
            //{
            //    rootItem.SetChildRelativeBound();
            //    rootItem.SetAbsoluteBound();
            //}

            /*
            if (dfContainer.Childs is null || dfContainer.Childs.Count == 0)
            {   // Prázdný container:
                dfContainer.DesignSize = GetEmptyDesignSize(dfContainer, styleContainer);
                return;
            }

            //  Postup tohoto algoritmu:
            // 1. Pokud this container obsahuje vnořené containery (=grupy), pak je vyřeším nejprve, tím se určí jejich designová velikost
            // 2. Následně řeším prvky, které nemají danou souřadnici X = ty přiděluji do "virtuálních" sloupců, které jsou dané v 'styleContainer.ColumnWidths'
            //    - virtuální sloupce mají danou šířku (anebo nemusí mít), určí se prvky, které do sloupců patří (nemají X, počítám jim sloupec)



            // Vnitřní prvky rozdělím na různé skupiny: Containery, a pak Child které mají souřadnici Y, a pak ty, které ji nemají:
            _ClassifyItems(dfContainer.Childs, out var containerChilds, out var fixedChilds, out var floatingChilds);

            // Nejprve vyřeším vnořené grupy, tím určím jejich (vnitřní a vnější) velikost:
            foreach (var containerChild in containerChilds)
            {
                StyleInfo childStyle = new StyleInfo(containerChild, styleContainer);
                _CreateLayoutPanel(args, containerChild, childStyle);
            }

            // Pole obsahující sloupce pro tento container, vycházejíc z 'styleContainer.ColumnWidths' a 'styleContainer.ColumnWidths':
            var columns = ColumnInfo.CreateColumns(styleContainer);



            // Nejprve umístím fixně definované prvky = tj. ty, které mají danou souřadnici X/Y; přitom střádám MaxY:
            int maxX = 0;
            int maxY = 0;
            foreach (var fixedChild in fixedChilds)
                _CreateLayoutFixedChild(fixedChild, ref maxX, ref maxY);

            // Nyní rozmístím plovoucí prvky = legacy layout zadaný do logických sloupců:
            foreach (var floatingChild in floatingChilds)
                _CreateLayoutFloatingChild(floatingChild, ref maxX, ref maxY);

            // Určí velikost containeru:
            dfContainer.DesignSize = GetDesignSize(dfContainer, styleContainer, maxX, maxY);

            */
        }

        /// <summary>
        /// Roztřídí prvky z pole <paramref name="childs"/> na ty, které mají v Bounds určenou souřadnici Top, ty dá do out pole <paramref name="fixedChilds"/>,
        /// ostatní dá do out pole <paramref name="floatingChilds"/>.<br/>
        /// Současně vytvoří pole kontejnerů (bez ohledu na jejich Fixed), ty se zpracovávají rekurzivně.<br/>
        /// Null prvky nedává nikam, ty se nezpracovávají.
        /// </summary>
        /// <param name="childs"></param>
        /// <param name="containerChilds"></param>
        /// <param name="fixedChilds"></param>
        /// <param name="floatingChilds"></param>
        private static void _ClassifyItems(List<DfBase> childs, out List<DfBaseContainer> containerChilds, out List<DfBase> fixedChilds, out List<DfBase> floatingChilds)
        {
            containerChilds = new List<DfBaseContainer>();
            fixedChilds = new List<DfBase>();
            floatingChilds = new List<DfBase>();

            //if (childs != null)
            //{
            //    foreach (var child in childs)
            //    {
            //        if (child is null) continue;

            //        Bounds bounds = null;
            //        if (child is DfBaseContainer container)
            //        {
            //            containerChilds.Add(container);
            //            bounds = container.Bounds;
            //        }
            //        else if (child is DfBaseControl control)
            //        {
            //            bounds = control.Bounds;
            //        }

            //        if (bounds != null && bounds.Top.HasValue)
            //            fixedChilds.Add(child);
            //        else
            //            floatingChilds.Add(child);
            //    }
            //}
        }



        /// <summary>
        /// Vrátí velikost containeru, který nemá žádné vnitřní prvky.
        /// </summary>
        /// <param name="dfContainer">Container. Může deklarovat svoji velikost.</param>
        /// <param name="containerStyle">Styl pro container včetně stylů zděděných. Může deklarovat okraje.</param>
        /// <returns></returns>
        private static ContainerSize GetEmptyDesignSize(DfBaseContainer dfContainer, StyleInfo containerStyle)
        {
            return new ContainerSize(0, 0, 0, 0);
        }
        /// <summary>
        /// Vrátí velikost containeru, který nemá žádné vnitřní prvky.
        /// </summary>
        /// <param name="dfContainer">Container. Může deklarovat svoji velikost.</param>
        /// <param name="containerStyle">Styl pro container včetně stylů zděděných. Může deklarovat okraje.</param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <returns></returns>
        private static ContainerSize GetDesignSize(DfBaseContainer dfContainer, StyleInfo containerStyle, int maxX, int maxY)
        {
            return new ContainerSize(0, 0, 0, 0);
        }
        #region class LayoutStyle : Styl vzhledu pro jeden container, podporuje dědičnost
        /// <summary>
        /// Styl vzhledu pro jeden container, podporuje dědičnost
        /// </summary>
        private class StyleInfo
        {
            public StyleInfo()
            {
                this.ColumnsCount = 0;
                this.ColumnWidths = null;
                this.AutoLabelPosition = LabelPositionType.None;
                this.Margins = null;
            }
            public StyleInfo(int? columnsCount, string columnWidths, LabelPositionType autoLabelPosition, Margins margins)
            {
                this.ColumnsCount = columnsCount;
                this.ColumnWidths = columnWidths;
                this.AutoLabelPosition = autoLabelPosition;
                this.Margins = margins;
            }
            public StyleInfo(DfBaseArea dfArea)
            {
                this.ColumnsCount = dfArea.ColumnsCount;
                this.ColumnWidths = dfArea.ColumnWidths;
                this.AutoLabelPosition = dfArea.AutoLabelPosition ?? LabelPositionType.None;
                this.Margins = dfArea.Margins;
            }
            public StyleInfo(DfBaseArea dfArea, StyleInfo styleParent)
            {
                this.ColumnsCount = (dfArea.ColumnsCount.HasValue ? dfArea.ColumnsCount : styleParent.ColumnsCount);
                this.ColumnWidths = dfArea.ColumnWidths != null ? dfArea.ColumnWidths : styleParent.ColumnWidths;
                this.AutoLabelPosition = dfArea.AutoLabelPosition.HasValue ? dfArea.AutoLabelPosition.Value : styleParent.AutoLabelPosition;
                this.Margins = dfArea.Margins != null ? dfArea.Margins : styleParent.Margins;
            }
            /// <summary>
            /// Počet sloupců layoutu. Šířka sloupců se určí podle reálného obsahu (maximum šířky prvků).
            /// Při zadání <see cref="ColumnsCount"/> se již nezadává <see cref="ColumnWidths"/>.
            /// </summary>
            public int? ColumnsCount { get; private set; }
            /// <summary>
            /// Šířky jednotlivých sloupců layoutu, oddělené čárkou; např. 150,350,100 (deklaruje tři sloupce dané šířky). 
            /// Při zadání <see cref="ColumnWidths"/> se již nezadává <see cref="ColumnsCount"/>.
            /// </summary>
            public string ColumnWidths { get; private set; }
            /// <summary>
            /// Automaticky generovat labely atributů a vztahů, jejich umístění. Defaultní = <c>NULL</c>
            /// </summary>
            public LabelPositionType AutoLabelPosition { get; private set; }
            public Margins Margins { get; set; }
        }
        #endregion
        #region class ColumnInfo : Průběžná data o layoutu jednoho sloupce
        /// <summary>
        /// Průběžná data o layoutu jednoho sloupce
        /// </summary>
        private class ColumnInfo : IDisposable
        {
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Index: {Index}; LeftLabelWidth: '{LeftLabelWidth}'; ControlWidth: '{ControlWidth}'; RightLabelWidth: '{RighLabelWidth}'";
            }
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {

            }
            /// <summary>
            /// Vytvoří sadu prvků <see cref="ColumnInfo"/> pro daný styl.
            /// Pokud není dodán styl, anebo ten neobsahuje nic o sloupcích, pak vrátí pole s jedním defaultním sloupcem.
            /// </summary>
            /// <param name="layoutStyle"></param>
            /// <returns></returns>
            public static ColumnInfo[] CreateColumns(StyleInfo layoutStyle)
            {
                List<ColumnInfo> columns = new List<ColumnInfo>();

                // Nějak dané šířky:
                if (layoutStyle != null)
                {   // Podle definovaného stylu:
                    if (layoutStyle.ColumnWidths != null)
                    {   // Explicitní šířky:
                        var columnWidths = layoutStyle.ColumnWidths;
                        var cols = columnWidths.Split(';');
                        int count = cols.Length;
                        for (int i = 0; i < count; i++)
                        {
                            parseCol(cols[i], out int? lblW, out int? ctrW, out int? lbrW);
                            columns.Add(new ColumnInfo() { Index = i, LeftLabelDefinedWidth = lblW, ControlDefinedWidth = ctrW, RightLabelDefinedWidth = lbrW });
                        }
                    }

                    if (columns.Count == 0)
                    {   // Prostě jen počet sloupců, bez deklarované šířky:
                        if (layoutStyle.ColumnsCount.HasValue && layoutStyle.ColumnsCount.Value > 0)
                        {
                            int count = layoutStyle.ColumnsCount.Value;
                            for (int i = 0; i < count; i++)
                                columns.Add(new ColumnInfo() { Index = i });
                        }
                    }
                }

                if (columns.Count == 0)
                {   // Jediný (defaultní) sloupec, bez deklarované šířky:
                    columns.Add(new ColumnInfo() { Index = 0 });
                }

                return columns.ToArray();


                // Parsuje text "10,20,30" na tři číslice
                void parseCol(string text, out int? lblW, out int? ctrW, out int? lbrW)
                {
                    lblW = null;
                    ctrW = null;
                    lbrW = null;

                    if (!String.IsNullOrEmpty(text))
                    {
                        var cells = text.Split(',');
                        int count = cells.Length;
                        if (count == 1)
                        {
                            ctrW = parseInt(cells[0]);
                        }
                        else if (count == 2)
                        {
                            lblW = parseInt(cells[0]);
                            ctrW = parseInt(cells[1]);
                        }
                        else if (count >= 3)
                        {
                            lblW = parseInt(cells[0]);
                            ctrW = parseInt(cells[1]);
                            lbrW = parseInt(cells[2]);
                        }
                    }
                }
                // Parsuje text na číslo
                int? parseInt(string text)
                {
                    if (String.IsNullOrEmpty(text)) return null;
                    if (!Int32.TryParse(text.Trim(), out var number)) return null;
                    return (number < 0 ? -1 : number);
                }
            }
            /// <summary>
            /// Index sloupce
            /// </summary>
            public int Index { get; set; }
            /// <summary>
            /// Šířka labelu vlevo od controlu, explicitně zadaná hodnota v deklaraci (null = nezadáno)
            /// </summary>
            public int? LeftLabelDefinedWidth { get; private set; }
            /// <summary>
            /// Šířka labelu vlevo od controlu, maximální nalezená hodnota (null = žádný prvek)
            /// </summary>
            public int? LeftLabelMaximalWidth { get; private set; }
            /// <summary>
            /// Šířka sloupce, kde je umístěn control, explicitně zadaná hodnota v deklaraci (null = nezadáno)
            /// </summary>
            public int? ControlDefinedWidth { get; private set; }
            /// <summary>
            /// Šířka sloupce, kde je umístěn control, explicitně zadaná hodnota v deklaraci (null = nezadáno)
            /// </summary>
            public int? ControlMaximalWidth { get; private set; }
            /// <summary>
            /// Šířka labelu vpravo od controlu, explicitně zadaná hodnota v deklaraci (null = nezadáno)
            /// </summary>
            public int? RightLabelDefinedWidth { get; private set; }
            /// <summary>
            /// Šířka labelu vpravo od controlu, maximální nalezená hodnota (null = žádný prvek)
            /// </summary>
            public int? RightLabelMaximalWidth { get; private set; }

            /// <summary>
            /// Šířka prostoru Label vlevo
            /// </summary>
            public int? LeftLabelWidth { get { return LeftLabelDefinedWidth ?? LeftLabelMaximalWidth; } }
            /// <summary>
            /// Šířka prostoru Control
            /// </summary>
            public int? ControlWidth { get { return ControlDefinedWidth ?? ControlMaximalWidth; } }
            /// <summary>
            /// Šířka prostoru Label vpravo
            /// </summary>
            public int? RighLabelWidth { get { return RightLabelDefinedWidth ?? RightLabelMaximalWidth; } }

            /// <summary>
            /// Pozice Left pro prostor Labelu vlevo
            /// </summary>
            public int? ColumnLeftLabelLeft { get; private set; }
            /// <summary>
            /// Pozice Left pro prostor Controlu
            /// </summary>
            public int? ColumnControlLeft { get; private set; }
            /// <summary>
            /// Pozice Left pro prostor Labelu vpravo
            /// </summary>
            public int? ColumnRightLabelLeft { get; private set; }
            /// <summary>
            /// Pozice Right pro prostor celého columnu = zde přesně začíná další sloupec
            /// </summary>
            public int? ColumnRight { get; private set; }

            /// <summary>
            /// Akceptuje šířky controlu, Main a Suffix labelu z dodaného prvku do zdejších hodnot pro tento sloupec.
            /// Volá se tehdy, když dodaný prvek má ColSpan = 1 a tedy jeho rozměry přímo ovlivňují jeden konkrétní sloupec.
            /// </summary>
            /// <param name="item"></param>
            internal void AcceptItemSingleWidth(ItemInfo item)
            {
                if (item != null)
                {
                    var itemData = item.ControlData;

                    // Vlastní control:
                    if (itemData.ControlWidth.HasValue)
                        this.ControlMaximalWidth = getMaxWidth(this.ControlMaximalWidth, itemData.ControlWidth);

                    // Main Label:
                    if (!String.IsNullOrEmpty(itemData.MainLabelText) && itemData.MainLabelWidth.HasValue)
                    {
                        if (itemData.LabelPosition == LabelPositionType.BeforeLeft || itemData.LabelPosition == LabelPositionType.BeforeRight)
                            // Vlevo:
                            this.LeftLabelMaximalWidth = getMaxWidth(this.LeftLabelMaximalWidth, itemData.MainLabelWidth);
                        else if (itemData.LabelPosition == LabelPositionType.Up || itemData.LabelPosition == LabelPositionType.Bottom)
                            // Nad/Pod controlem:
                            this.ControlMaximalWidth = getMaxWidth(this.ControlMaximalWidth, itemData.MainLabelWidth);
                    }

                    // Suffix Label:
                    if (!String.IsNullOrEmpty(itemData.SuffixLabelText) && itemData.SuffixLabelWidth.HasValue)
                        this.RightLabelMaximalWidth = getMaxWidth(this.RightLabelMaximalWidth, itemData.SuffixLabelWidth);
                }

                // Vrátí Max(Width) z těch, co nejsou null.
                int? getMaxWidth(int? currentWidth, int? itemWidth)
                {
                    if (currentWidth.HasValue && itemWidth.HasValue) return (currentWidth.Value > itemWidth.Value ? currentWidth.Value : itemWidth.Value);
                    if (currentWidth.HasValue) return currentWidth;
                    if (itemWidth.HasValue) return itemWidth;
                    return null;
                }
            }
        }
        #endregion
        #region class ItemInfo : Dočasná pracovní a výkonná schránka na jednotlivý prvek layoutu
        /// <summary>
        /// Dočasná pracovní a výkonná schránka na jednotlivý prvek layoutu (panel, grupa, control), v procesu určování layoutu prvků v rámci panelu.
        /// <para/>
        /// Uvnitř panelu jsou prvky rozmístěny fixně = jsou dané designerem formuláře. 
        /// Ale rozmístění sousedních panelů na DataFormu je více v rukou uživatele / pohledu / velikosti monitoru atd.
        /// </summary>
        private class ItemInfo : IDisposable
        {
            #region Konstrukce, Dispose, základní stromové vlastnosti, Childs a jejich tvorba
            /// <summary>
            /// Vytvoří Root prvek, a sučasně v něm najde Containery a vytvoří rekurzivně celou strukturu
            /// </summary>
            /// <param name="dfItem"></param>
            /// <param name="dfArgs"></param>
            /// <param name="style">Styl pro daný panel. Panel si jej sám nevytvoří, protože musí mít k dispozici styl parenta (dědičnost pro implicitní hodnoty!)</param>
            /// <returns></returns>
            internal static ItemInfo CreateRoot(DfBase dfItem, DfTemplateLayoutArgs dfArgs, StyleInfo style)
            {
                var rootItem = new ItemInfo(dfItem, null, dfArgs, style);
                rootItem._CreateChilds();
                return rootItem;
            }
            /// <summary>
            /// Pokud this obsahuje container <see cref="DfBaseContainer"/>, pak projde jeho <see cref="DfBaseContainer.Childs"/>
            /// a pro každý z nich vytvoří svůj <see cref="_Childs"/> prvek své vlastní třídy a korektně jej naváže.
            /// Pokud tento Child prvek je container, pak vyvolá tuto metodu rekurzivně i pro něj.
            /// </summary>
            private void _CreateChilds()
            {
                if (__DfItem is DfBaseContainer dfContainer && dfContainer.Childs != null)
                {
                    __Childs = new List<ItemInfo>();
                    var dfChilds = dfContainer.Childs;
                    foreach (var dfChild in dfChilds)
                    {
                        if (dfChild != null)
                        {
                            // a) Container (tj. Group)?
                            if (dfChild is DfBaseContainer dfChildContainer)
                            {
                                StyleInfo childStyle = new StyleInfo(dfChildContainer, this.__Style);
                                ItemInfo childContainer = new ItemInfo(dfChild, this, null, childStyle);
                                __Childs.Add(childContainer);
                                childContainer._CreateChilds();
                            }
                            // b) Control
                            else if (dfChild is DfBaseControl dfChildControl)
                            {
                                ItemInfo childControl = new ItemInfo(dfChild, this, null, null);
                                __Childs.Add(childControl);
                            }
                        }
                    }
                }
            }
            /// <summary>
            /// Privátní konstruktor
            /// </summary>
            /// <param name="dfItem"></param>
            /// <param name="parent"></param>
            /// <param name="dfArgs"></param>
            /// <param name="style"></param>
            private ItemInfo(DfBase dfItem, ItemInfo parent, DfTemplateLayoutArgs dfArgs, StyleInfo style)
            {
                __DfItem = dfItem;
                __Parent = parent;
                __DfArgs = dfArgs;
                __Style = style;
                __Childs = null;
                _InitData();
            }
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                __Childs?.ForEach(i => i?.Dispose());
                this._ResetData();
                __DfItem = null;
                __Parent = null;
                __DfArgs = null;
                __Style = null;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this._FullPath;
            }
            /// <summary>
            /// Definiční prvek = načtený z XML šablony (panel, grupa, control...)
            /// </summary>
            private DfBase __DfItem;
            /// <summary>
            /// Náš přímý parent. Přes něj dojdeme až k Root prvku.
            /// Pozor, zde může být null v případě Root prvku.
            /// </summary>
            private ItemInfo __Parent;
            /// <summary>
            /// Vstupní argumenty celého formu.
            /// Pozor, zde je instance pouze v Root prvku. Běžně pro práci se používá <see cref="_LayoutArgs"/>.
            /// </summary>
            private DfTemplateLayoutArgs __DfArgs;
            /// <summary>
            /// Styl pro tento kontejner.
            /// Container si styl sám nevytvoří, protože musí mít k dispozici styl parenta (dědičnost pro implicitní hodnoty!).
            /// Jednotlivé controly zde mají null, najdou si styl ve svém parentu.
            /// </summary>
            private StyleInfo __Style;
            /// <summary>
            /// Prvky mých Childs. Výchozí stav je null (většina prvků jsou Controly, které nemají Childs). Lze testovat <see cref="_HasChilds"/>.
            /// </summary>
            private List<ItemInfo> __Childs;

            /// <summary>
            /// Root prvek. Zde nikdy není null: pokud this je Root, pak zde je this.
            /// </summary>
            private ItemInfo _Root { get { return (__Parent?._Root ?? this); } }
            /// <summary>
            /// Parent prvek. Může být null.
            /// </summary>
            private ItemInfo _Parent { get { return __Parent; } }
            /// <summary>
            /// Styl pro tento prvek. Pokud this je container, pak má definován svůj vlastní styl (<see cref="__Style"/>).
            /// Pokud jej nemá (typicky proto, že jde o Control), pak zde vrací aktuální styl ze svého Parenta.
            /// </summary>
            private StyleInfo _CurrentStyle { get { return __Style ?? __Parent?._CurrentStyle; } }
            /// <summary>
            /// Obsahuje true, pokud this je Root
            /// </summary>
            private bool _IsRoot { get { return (__Parent is null); } }
            /// <summary>
            /// Argumenty pro layout šablony = obsahuje Form, Source, Errors...
            /// Zde není nikdy null.
            /// </summary>
            private DfTemplateLayoutArgs _LayoutArgs { get { return _Root.__DfArgs; } }
            /// <summary>
            /// Celý formulář
            /// </summary>
            private DfForm _DfForm { get { return _LayoutArgs.DataForm; } }
            /// <summary>
            /// Objekt, který je zdrojem dalších dat pro dataform ze strany systému.
            /// Například vyhledá popisný text pro datový control daného jména, určí velikost textu s daným obsahem a daným stylem, atd...
            /// </summary>
            private IControlInfoSource _InfoSource { get { return _LayoutArgs.InfoSource; } }
            /// <summary>
            /// Plná cesta od Root přes jeho Child až ke mě = typy prvků oddělené šipkou.
            /// </summary>
            private string _FullPath
            {
                get
                {
                    var type = __DfItem.GetType().Name;
                    var name = __DfItem.Name;
                    var text = type + (!String.IsNullOrEmpty(name) ? $" '{name}'" : "");

                    var parent = __Parent;
                    return (parent != null ? parent._FullPath + " => " : "") + text;
                }
            }
            /// <summary>
            /// Prvek reprezentuje container (panel, grupa)
            /// </summary>
            private bool _IsContainer { get { return (__DfItem is DfBaseContainer); } }
            /// <summary>
            /// Container (tedy Panel + Group)
            /// </summary>
            private DfBaseContainer _DfContainer { get { return (__DfItem as DfBaseContainer); } }
            /// <summary>
            /// Prvek reprezentuje grupu
            /// </summary>
            private bool _IsGroup { get { return (__DfItem is DfGroup); } }
            /// <summary>
            /// Grupa uvnitř panelu nebo grupy
            /// </summary>
            private DfGroup _DfGroup { get { return (__DfItem as DfGroup); } }
            /// <summary>
            /// Prvek reprezentuje samostatný control: pak <see cref="__DfItem"/> je potomkem <see cref="DfBaseControl"/>.
            /// </summary>
            private bool _IsControl { get { return (__DfItem is DfBaseControl); } }
            private DfBaseControl _DfControl { get { return (__DfItem as DfBaseControl); } }
            /// <summary>
            /// Všechny Child prvky (controly + grupy).
            /// Všechny je třeba umístit
            /// </summary>
            private ItemInfo[] _Childs { get { return __Childs?.ToArray(); } }
            /// <summary>
            /// Obsahuje true, pokud máme nějaké <see cref="_Childs"/> (tj. pole <see cref="__Childs"/> není null a obsahuje alespoň jeden prvek).
            /// </summary>
            private bool _HasChilds { get { return (this.__Childs != null && this.__Childs.Count > 0); } }
            #endregion
            #region Další data
            /// <summary>
            /// Připraví si trvalá data
            /// </summary>
            private void _InitData()
            {
                string name = __DfItem.Name;
                __Name = name;

                string columnName = name;
                if (__DfItem is DfBaseInputControl inputControl)
                {
                    if (!String.IsNullOrEmpty(inputControl.ColumnName)) columnName = inputControl.ColumnName;
                }
                __ColumnName = columnName;


            }
            /// <summary>
            /// Zahodí veškerá data
            /// </summary>
            private void _ResetData()
            {
                __Name = null;
                __ColumnName = null;
                __Bounds = null;
                __ColIndex = null;
                __ColSpan = null;
                __LayoutBeginRowIndex = null;
                __LayoutBeginColumnIndex = null;
                __LayoutEndColumnIndex = null;
                __LayoutMode = LayoutModeType.None;
                if (__LayoutColumns != null)
                {
                    foreach (var column in __LayoutColumns)
                        column.Dispose();
                    __LayoutColumns = null;
                }
                __ControlData = null;
            }
            /// <summary>
            /// Jméno prvku
            /// </summary>
            private string __Name;
            /// <summary>
            /// Jméno sloupce (nebo jméno prvku)
            /// </summary>
            private string __ColumnName;

            /// <summary>
            /// Umístění prvku. Výchozí je null.
            /// </summary>
            private Bounds __Bounds;
            /// <summary>
            /// Index sloupce, na kterém je prvek umístěn v režimu FlowLayout. Ten se použije, pokud prvky nemají exaktně dané souřadnice, spolu s atributem 'ColumnWidths'.
            /// </summary>
            private int? __ColIndex;
            /// <summary>
            /// Počet sloupců, které prvek obsazuje v FlowLayoutu. Ten se použije, pokud prvky nemají exaktně dané souřadnice, spolu s atributem 'ColumnWidths'.
            /// </summary>
            private int? __ColSpan;

            /// <summary>
            /// Index řádku, na kterém se reálně nachází prvek v režimu FlowLayout.
            /// </summary>
            private int? __LayoutBeginRowIndex;
            /// <summary>
            /// Index sloupce, na kterém reálně začíná v režimu FlowLayout.
            /// </summary>
            private int? __LayoutBeginColumnIndex;
            /// <summary>
            /// Index sloupce, na kterém reálně končí v režimu FlowLayout.
            /// </summary>
            private int? __LayoutEndColumnIndex;

            /// <summary>
            /// Režim layoutu
            /// </summary>
            private LayoutModeType __LayoutMode;
            /// <summary>
            /// Sloupce pro FlowLayout pro Child prvky tohoto containeru
            /// </summary>
            private ColumnInfo[] __LayoutColumns;
            /// <summary>
            /// Sada dat shrnutá z heterogenních prvků do konstantní struktury, pracovní pro výpočty layoutu.
            /// Jsou sdílená i s volající metodou = struktura obsahující definiční data, která může doplňovat aplikační vrstva (label, rozměry, editační styl, atd)
            /// </summary>
            private ControlInfo __ControlData;


            /// <summary>
            /// Index řádku, na kterém se reálně nachází prvek v režimu FlowLayout.
            /// </summary>
            internal int? LayoutBeginRowIndex { get { return __LayoutBeginRowIndex; } }
            /// <summary>
            /// Index sloupce, na kterém reálně začíná v režimu FlowLayout.
            /// </summary>
            internal int? LayoutBeginColumnIndex { get { return __LayoutBeginColumnIndex; } }
            /// <summary>
            /// Index sloupce, na kterém reálně končí v režimu FlowLayout.
            /// </summary>
            internal int? LayoutEndColumnIndex { get { return __LayoutEndColumnIndex; } }
            /// <summary>
            /// Sada dat shrnutá z heterogenních prvků do konstantní struktury, pracovní pro výpočty layoutu.
            /// Jsou sdílená i s volající metodou = struktura obsahující definiční data, která může doplňovat aplikační vrstva (label, rozměry, editační styl, atd)
            /// </summary>
            internal ControlInfo ControlData { get { return __ControlData; } }
            #endregion
            #region Zpracování layoutu panelu
            /// <summary>
            /// Zajistí plné zpracování this containeru, rekurzivně jeho Child containerů a zdejších Controlů.
            /// </summary>
            internal void ProcessPanel()
            {
                _ProcessContainer();
                _PreparePanelAbsoluteBounds();
            }
            /// <summary>
            /// Zajistí plné zpracování this containeru, rekurzivně jeho Child containerů a zdejších Controlů.
            /// </summary>
            private void _ProcessContainer()
            {
                if (_HasChilds)
                {
                    _PrepareChilds();            // Příprava: Containery kompletně (rekurzivně), a poté všechny prvky: doplnění aplikačních dat a měření primární velikosti
                    _SetChildsFlowPositions();   // Rozmístění našich Child prvků (zde se Child Containery již řeší jako rovnocenné s Controly, neřešíme už rekurzi)
                    _CalculateColumns();         // 
                    _PrepareRelativeBounds();
                }

            }
            /// <summary>
            /// Připraví data Childs: kompletní příprava Containeru, a prvotní příprava pro pozicování Group + Control
            /// </summary>
            private void _PrepareChilds()
            {
                // Přípravná fáze, jejím výsledkem jsou určené velikosti Child Containerů a Controlů:
                var childs = this.__Childs;
                foreach (var item in childs)
                {
                    if (item._IsGroup)
                    {   // Grupa si nyní kompletně připraví layout pro svoje Child prvky.
                        // A poté při tvorbě našeho layoutu s ním budeme zacházet jako s obyčejným prvkem.
                        item._ProcessContainer();                          // Child je Grupa = Container, ať si projde tuto metodu rekurzivně sám
                        item._PrepareForGroup(item._DfGroup);
                    }
                    else if (item._IsControl)
                    {
                        item._PrepareForControl(item._DfControl);
                    }
                }
            }
            /// <summary>
            /// Provede přípravné kroky před tvorbou layoutu pro danou grupu.
            /// Připraví instanci <see cref="__ControlData"/>, kde budeme připravovat info o umístění containeru a jeho labelů.
            /// </summary>
            /// <param name="dfGroup"></param>
            /// <exception cref="NotImplementedException"></exception>
            private void _PrepareForGroup(DfGroup dfGroup)
            {
                // Základní příprava:
                this.__Bounds = dfGroup.Bounds;
                this.__ColIndex = dfGroup.ColIndex;
                this.__ColSpan = dfGroup.ColSpan;

                //  Využijeme strukturu pro data, kterou naplníme známými hodnotami z formuláře, a pošleme do systému k doplnění těch chybějících hodnot:
                var itemData = new ControlInfo(__DfArgs.DataForm, __Name, __Name, ControlType.None);
                __ControlData = itemData;

                // Label u grupy???
            }
            /// <summary>
            /// Provede přípravné kroky před tvorbou layoutu pro daný control.
            /// Připraví instanci <see cref="__ControlData"/>, kde budeme připravovat info o umístění controlu a jeho labelů.
            /// </summary>
            /// <param name="baseControl"></param>
            /// <exception cref="NotImplementedException"></exception>
            private void _PrepareForControl(DfBaseControl baseControl)
            {
                //  Tato metoda zajistí doplnění vlastností controlu pro ty hodnoty, které nejsou explicitně zadané v šabloně, 
                //     na základě dat dodaných z jádra pro konkrétní atribut dané třídy
                // Velikost controlu
                // MainLabel - text, velikost (nikoli konkrétní pozice)
                // Tooltip
                // Překlady textů z formátovacách stringů : "fm(MSG001)" => "Reference", atd  (pro labely, pro tooltipy)

                // Základní příprava:
                this.__Bounds = baseControl.Bounds;
                this.__ColIndex = baseControl.ColIndex;
                this.__ColSpan = baseControl.ColSpan;

                //  Využijeme strukturu pro data, kterou naplníme známými hodnotami z formuláře, a pošleme do systému k doplnění těch chybějících hodnot:
                var itemData = new ControlInfo(_DfForm, __Name, __ColumnName, baseControl.ControlType);
                __ControlData = itemData;

                // Bázové informace:
                itemData.ControlStyle = baseControl.ControlStyle;
                itemData.LabelPosition = LabelPositionType.None;
                itemData.ControlWidth = baseControl.Bounds?.Width;
                itemData.ControlHeight = baseControl.Bounds?.Height;
                itemData.ToolTipTitle = baseControl.ToolTipTitle;
                itemData.ToolTipText = baseControl.ToolTipText;

                // Specifické informace:
                var labeledInputControl = baseControl as DfBaseLabeledInputControl;
                if (labeledInputControl != null)
                {
                    itemData.LabelPosition = labeledInputControl.LabelPosition ?? _CurrentStyle.AutoLabelPosition;
                    itemData.MainLabelText = labeledInputControl.Label;
                    itemData.MainLabelWidth = labeledInputControl.LabelWidth;
                }

                // Validace externí (tj. aplikační kód) a lokální (defaultní):
                this._LayoutArgs.InfoSource.ValidateControlInfo(__ControlData);
                DfTemplateLayout.ValidateControlInfo(__ControlData);

                // Převezmeme výsledky:
                this._ControlSize = new Size(itemData.ControlWidth, itemData.ControlHeight);
                if (itemData.LabelPosition != LabelPositionType.None && !String.IsNullOrEmpty(itemData.MainLabelText))
                    this._MainLabelSize = new Size(itemData.MainLabelWidth, itemData.MainLabelHeight);
                if (!String.IsNullOrEmpty(itemData.SuffixLabelText))
                    this._SuffixLabelSize = new Size(itemData.SuffixLabelWidth, itemData.MainLabelHeight);

            }
            /// <summary>
            /// Vytvoří pole sloupců, projde všechny Childs, nastaví jim LayoutMode = Fixed / Flow.
            /// Pro Flow prvky určí jejich pozici v matici: LayoutBeginRowIndex, LayoutBeginColumnIndex, LayoutEndColumnIndex.
            /// Pro jednosloupcový prvek typu Flow započítá jeho šířky (Label, Control) do jemu odpovídajícího sloupce.
            /// </summary>
            private void _SetChildsFlowPositions()
            {
                // Všechny moje Childs: 
                //  pokud konkrétní Child má definovanou pozici (X,Y), pak mu jen nastavím příznak __LayoutMode = Fixed;
                //  zde řeším Flow prvky (takové, co nemají X,Y) = zařazuji je do layoutových sloupců, a napočítávám Max hodnoty jejich sloupců:
                this.__LayoutColumns = ColumnInfo.CreateColumns(this.__Style);
                int columnsCount = this.__LayoutColumns.Length;
                int rowIndex = 0;
                int columnFlowIndex = 0;
                var childs = this.__Childs;
                foreach (var item in childs)
                {
                    var itemData = item.__ControlData;
                    if (item.__Bounds != null && item.__Bounds.Left.HasValue && item.__Bounds.Top.HasValue)
                    {   // Fixed:
                        item.__LayoutMode = LayoutModeType.Fixed;
                    }
                    else
                    {   // Flow:
                        item.__LayoutMode = LayoutModeType.Flow;

                        // Index začátku sloupce pro prvek je buď explicitní, nebo průběžný:
                        //  Explicitní akceptuji 0 a vyšší, i když by byl zadaný větší než MaxCount:
                        int itemBeginColumnIndex = (item.__ColIndex.HasValue && item.__ColIndex.Value >= 0) ? item.__ColIndex.Value : columnFlowIndex;
                        if (itemBeginColumnIndex >= columnsCount)
                        {   // Pokud target překročí existující sloupce, pak jdu na další řádek a na index 0:
                            //  Pokud někdo explicitně definuje 3 sloupce, a pak dá ColIndex = 10, pak je to pokyn pro Nový řádek a sloupec 0:
                            //   Pokud běžně překročím na další sloupec za počet sloupců, stejně tak jdu na další řádek a na index 0:
                            itemBeginColumnIndex = 0;
                            rowIndex++;
                        }
                        else if (itemBeginColumnIndex < columnFlowIndex)
                        {   // Pokud target je menší než aktuální sloupec (tedy je dán explicitně), pak jdu na další řádek, ale target sloupec ponechám (protože je menší než 'columnsCount'):
                            //  Pokud někdo explicitně definuje 3 sloupce, a je na sloupci 0, pak může dát pak ColIndex = 2  =>  a tak skočí na poslední sloupec a vynechá prosto sloupce 1,
                            //  anebo je na sloupci 1 a dá ColIndex = 1  =>  tak skočí na další řádek a vepíše prvek do (téhož) sloupce 1:
                            rowIndex++;
                        }

                        // Index Next sloupce je dán indexem Begin + jeho ColSpan (default 1), s ohledem na celkový počet sloupců:
                        //  NextIndex smí být roven nejvýše 'columnsCount'; bude-li větší, pak se zarovná:
                        int itemColSpan = (item.__ColSpan.HasValue && item.__ColSpan.Value > 1 ? item.__ColSpan.Value : 1);
                        int itemNextColumnIndex = itemBeginColumnIndex + itemColSpan;
                        if (itemNextColumnIndex > columnsCount) itemNextColumnIndex = columnsCount;

                        // Umístění aktuálního prvku:
                        item.__LayoutBeginRowIndex = rowIndex;
                        item.__LayoutBeginColumnIndex = itemBeginColumnIndex;
                        item.__LayoutEndColumnIndex = itemNextColumnIndex - 1;

                        // Pokud prvek obsazuje právě jeden sloupec layoutu, pak by měl do svého sloupce započítat svoje šířky:
                        if (itemColSpan == 1)
                            this.__LayoutColumns[itemBeginColumnIndex].AcceptItemSingleWidth(item);

                        // Počáteční sloupec pro další prvek:
                        //  Pokud 'columnIndex' bude rovno 'columnsCount', pak pro nějbližší další prvek přeskočíme na nový řádek (až to bude potřeba).
                        columnFlowIndex = itemNextColumnIndex;
                    }
                }
            }
            /// <summary>
            /// Projde Flow prvky, které zabírají na šířku více než jeden sloupec (ColSpan větší než 1) a jejich šířky promítne do odpovídajících sloupců.
            /// </summary>
            private void _CalculateColumns()
            {
                var childs = this.__Childs;
                foreach (var item in childs)
                {
                    var itemData = item.__ControlData;
                    if (item.__LayoutMode == LayoutModeType.Flow && item.__LayoutEndColumnIndex.Value > item.__LayoutBeginColumnIndex.Value)
                    {   // Prvek je typu Float, a má ColSpan > 1:

                        
                    }
                }
            }
            /// <summary>
            /// Projde všechny prvky a přidělí jim konkrétní relativní souřadnice = v rámci jejich Parenta.
            /// Současně určí svoji vnitřní velikost (this ke container).
            /// </summary>
            private void _PrepareRelativeBounds()
            { }

            /// <summary>
            /// Projde všechny prvky a přidělí jim konkrétní absolutní souřadnice = v rámci Root panelu.
            /// Tato metoda je vyvolána pro prvek typu Panel.
            /// </summary>
            private void _PreparePanelAbsoluteBounds()
            {

                this._ControlSize = new Size(0, 0);
            }

            /// <summary>
            /// Všechny svoje Child prvky umístí na konkrétní souřadnici.
            /// Ty, které mají souřadnici danou explicitně jsou jednoznačné.
            /// Ty ostatní umístí jako plovoucí do FlowLayoutu = sloupce, index sloupce a colspan...
            /// </summary>
            private void _PositionChilds()
            {
                _SetChildsFlowPositions();


                // Nyní určím X a Width souřadnice sloupců:
                var container = this._DfContainer;
                var layoutOrigin = container.FlowLayoutOrigin;
                int layoutTop = layoutOrigin?.Top ?? 0;
                int layoutLeft = layoutOrigin?.Left ?? 0;
                int currentTop = layoutTop;
                int currentLeft = layoutLeft;
                var childs = this.__Childs;
                foreach (var item in childs)
                {
                    
                }

                // Všechny moje Childs, jejich pozice absolutní anebo jejich zařazení do layoutu:
            }

            #endregion
            #region Určení relativní souřadnice každého prvku
            /// <summary>
            /// Metoda určí relativní souřadnice svých Child prvků. Nakonec určí i svoji velikost na základě velikosti a pozice Child prvků.
            /// Container může mít svoje souřadnice i exaktně určené (pokud je to grupa).
            /// </summary>
            internal void SetChildRelativeBound()
            {
                //if (this._HasChilds)
                //{
                //    var childs = this.__Childs;
                //    // Určím velikost každé Child buňky:
                //    foreach (var item in childs)
                //    {
                //        if (item._IsContainer)
                //            item.SetChildRelativeBound();                      // Child je Container, ať si projde tuto metodu rekurzivně sám
                //        else if (item._IsControl)
                //            item._SetBaseControlSizes(__DfItem as DfBaseControl);
                //    }

                //    // Určím sloupce mého layoutu a zařadím moje Child prvky do těchto sloupců:
                //    var columns = ColumnInfo.CreateColumns(this.__Style);

                //    //  * Pokud prvek nemá určené ani X ani Y, pak je volně plovoucí;
                //    //  * Pokud prvek má zadané X, pak spadá do aktuálního řádku Y, ale na danou souřadnici;
                //    //  * Pokud prvek má určené obě X i Y, pak jej umístím na jeho místo i kdyby na dané souřadnici už něco bylo (to je odpovědnost autora);


                //}

                // Určím velikost mojí jako containeru:

            }
            #endregion
            #region Určení velikosti this controlu = jeden prvek (atribut)
            /// <summary>
            /// Metoda určí velikost zdejšího controlu, potomek <see cref="DfBaseControl"/>.
            /// </summary>
            private void _SetBaseControlSizes(DfBaseControl baseControl)
            {
                //// Jméno [nebo jméno sloupce] a jeho styl:
                //string columnName = __ColumnName;
                //string styleName = baseControl.ControlStyle?.StyleName;
                //FontStyleType controlStyle = FontStyleType.Regular;

                //// Vlastní control, jeho velikost:
                //var designBounds = baseControl.Bounds;
                //int? controlWidth = designBounds?.Width;
                //int? controlHeight = designBounds?.Height;
                //bool hasWidth = (controlWidth.HasValue && controlWidth.Value >= 0);
                //bool hasHeight = (controlHeight.HasValue && controlHeight.Value >= 0);

                //// Pokud nemám zadanou šířku nebo výšku:
                //if (!(hasWidth && hasHeight))
                //{
                //    // Zeptáme se zdroje, zda pro daný control určí velikost podle jména a typu atributu:
                //    var size = this._InfoSource.GetSizeForAttribute(columnName, baseControl.ControlType, controlStyle);
                //    if (!hasWidth)
                //    {
                //        controlWidth = size?.Width;
                //        hasWidth = (controlWidth.HasValue && controlWidth.Value >= 0);
                //    }

                //    if (!hasHeight)
                //    {
                //        controlHeight = size?.Height;
                //        hasHeight = (controlHeight.HasValue && controlHeight.Value >= 0);
                //    }

                //    // Pokud nemám zadanou šířku nebo výšku ani poté:
                //    if (!(hasWidth && hasHeight))
                //    {
                //        // Zeptáme se generické metody, jakou velikost bude mít control:
                //        size = DfTemplateLayout.GetDefaultSizeForAttribute(columnName, baseControl.ControlType, controlStyle);
                //        if (!hasWidth)
                //        {
                //            controlWidth = size?.Width;
                //            hasWidth = (controlWidth.HasValue && controlWidth.Value >= 0);
                //        }

                //        if (!hasHeight)
                //        {
                //            controlHeight = size?.Height;
                //            hasHeight = (controlHeight.HasValue && controlHeight.Value >= 0);
                //        }
                //    }
                //}
                //this._ControlSize = new Size(controlWidth, controlHeight);


                //// Control může být i s automatickým labelem:
                //if (baseControl is DfBaseLabeledInputControl labeledControl)
                //    _SetLabeledControlSizes(labeledControl, columnName);
            }
            /// <summary>
            /// Metoda určí velikost a pozici zdejšího controlu s labelem, potomek <see cref="DfBaseLabeledInputControl"/>.
            /// </summary>
            /// <param name="labeledControl"></param>
            /// <param name="columnName"></param>
            private void _SetLabeledControlSizes(DfBaseLabeledInputControl labeledControl, string columnName)
            {
                //LabelPositionType labelPosition = labeledControl.LabelPosition ?? this.__Style.AutoLabelPosition;
                //string mainLabel = labeledControl.Label;
                //bool hasLabel = !String.IsNullOrEmpty(mainLabel);
                //if (!hasLabel && labelPosition == LabelPositionType.None) return;        // Pokud nemám dán svůj label, a pozice labelu je None, pak Main label řešit nebudu.


            }


            private Size _ControlSize { get; set; }
            private Size _MainLabelSize { get; set; }
            private Size _SuffixLabelSize { get; set; }
            #endregion
            #region Určení absolutní souřadnice každého prvku
            internal void SetAbsoluteBound()
            { }
            #endregion
        }
        #endregion
    }
    #region class DfTemplateLayoutArgs : Data pro algoritmy rozmístění prvků šablony DataFormu
    /// <summary>
    /// Data pro algoritmy rozmístění prvků šablony DataFormu
    /// </summary>
    internal class DfTemplateLayoutArgs : DfProcessArgs
    {
        /// <summary>
        /// Dataform, jehož layout vzniká
        /// </summary>
        public DfForm DataForm { get; set; }
        /// <summary>
        /// Objekt, který je zdrojem dalších dat pro dataform ze strany systému.
        /// Například vyhledá popisný text pro datový control daného jména, určí velikost textu s daným obsahem a daným stylem, atd...
        /// </summary>
        public IControlInfoSource InfoSource { get; set; }
    }
    #endregion
    #region interface IControlInfoSource : Předpis rozhraní pro toho, kdo bude poskytovat informace o atributech a o rozměrech textů pro DataForm
    /// <summary>
    /// Předpis rozhraní pro toho, kdo bude poskytovat informace o atributech a o rozměrech textů pro DataForm.
    /// </summary>
    internal interface IControlInfoSource
    {
        /// <summary>
        /// Validuje (prověří a doplní) informace o konkrétním controlu.
        /// </summary>
        /// <param name="controlInfo">Data o controlu</param>
        /// <returns></returns>
        void ValidateControlInfo(ControlInfo controlInfo);
    }
    /// <summary>
    /// Data popisující control v rámci DataFormu
    /// </summary>
    internal class ControlInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataForm">Dataform, jehož layout vzniká</param>
        /// <param name="name">Name prvku (controlu)</param>
        /// <param name="columnName">ColumnName</param>
        /// <param name="controlType">Typ controlu</param>
        public ControlInfo(DfForm dataForm, string name, string columnName, ControlType controlType)
        {
            Name = name;
            ColumnName = columnName;
            ControlType = controlType;
            DataForm = dataForm;
        }
        /// <summary>
        /// Dataform, jehož layout vzniká
        /// </summary>
        public DfForm DataForm { get; private set; }
        /// <summary>
        /// Jméno prvku
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Jméno sloupce v datech (více prvků různého jména <see cref="Name"/> může zobrazovat data ze stejného prvku <see cref="ColumnName"/>).
        /// </summary>
        public string ColumnName { get; private set; }
        /// <summary>
        /// Typ controlu, definovaný v Form.xml
        /// </summary>
        public ControlType ControlType { get; private set; }
        /// <summary>
        /// Fixní text v prvku, typicky v buttonu, v Checkboxu atd
        /// </summary>
        public string ControlText { get; set; }
        /// <summary>
        /// Styl controlu (název, styl písma, velikost, barva popisku, barva textu a pozadí, atd)
        /// </summary>
        public DfControlStyle ControlStyle { get; set; }
        /// <summary>
        /// Pozice hlavního labelu
        /// </summary>
        public LabelPositionType LabelPosition { get; set; }
        /// <summary>
        /// Text hlavního labelu.
        /// Pokud je null, pak není ve formuláři definováno. Pokud je "", je tím definováno 'Bez labelu'.
        /// V existující definici mohou být přítomny formátovací funkce: "fm(xxx)", "fmr(xxx)". Přípravná funkce to má vyřešit.
        /// </summary>
        public string MainLabelText { get; set; }
        /// <summary>
        /// Text suffix labelu. Jde o popisek vpravo od vstpního prvku, typicky obsahuje název jednotky (ks, Kč, $, kg, ...).
        /// Pokud je null, pak není ve formuláři definováno. Pokud je "", je tím definováno 'Bez labelu'.
        /// V existující definici mohou být přítomny formátovací funkce: "fm(xxx)", "fmr(xxx)". Přípravná funkce to má vyřešit.
        /// </summary>
        public string SuffixLabelText { get; set; }
        /// <summary>
        /// Titulek ToolTipu.
        /// Pokud je null, pak není ve formuláři definováno. Pokud je "", je tím definováno 'Bez labelu'.
        /// V existující definici mohou být přítomny formátovací funkce: "fm(xxx)", "fmr(xxx)". Přípravná funkce to má vyřešit.
        /// </summary>
        public string ToolTipTitle { get; set; }
        /// <summary>
        /// Text ToolTipu.
        /// Pokud je null, pak není ve formuláři definováno. Pokud je "", je tím definováno 'Bez labelu'.
        /// V existující definici mohou být přítomny formátovací funkce: "fm(xxx)", "fmr(xxx)". Přípravná funkce to má vyřešit.
        /// </summary>
        public string ToolTipText { get; set; }
        /// <summary>
        /// Šířka controlu ve standardních pixelech. 
        /// Pokud je null, pak není ve formuláři definováno, a přípravná funkce má rozměr určit. 
        /// Pokud je hodnota uvedená ve formuláři, jde uvedena zde.
        /// </summary>
        public int? ControlWidth { get; set; }
        /// <summary>
        /// Výška controlu ve standardních pixelech. 
        /// Pokud je null, pak není ve formuláři definováno, a přípravná funkce má rozměr určit. 
        /// Pokud je hodnota uvedená ve formuláři, jde uvedena zde.
        /// </summary>
        public int? ControlHeight { get; set; }
        /// <summary>
        /// Šířka labelu <see cref="MainLabelText"/> ve standardních pixelech. 
        /// Pokud je null, pak není ve formuláři definováno, a přípravná funkce má rozměr určit. 
        /// Pokud je hodnota uvedená ve formuláři, jde uvedena zde.
        /// </summary>
        public int? MainLabelWidth { get; set; }
        /// <summary>
        /// Šířka labelu <see cref="MainLabelText"/> ve standardních pixelech. 
        /// Pokud je null, pak není ve formuláři definováno, a přípravná funkce má rozměr určit. 
        /// Pokud je hodnota uvedená ve formuláři, jde uvedena zde.
        /// </summary>
        public int? MainLabelHeight { get; set; }
        /// <summary>
        /// Šířka labelu <see cref="SuffixLabelText"/> ve standardních pixelech. 
        /// Pokud je null, pak není ve formuláři definováno, a přípravná funkce má rozměr určit. 
        /// Pokud je hodnota uvedená ve formuláři, jde uvedena zde.
        /// </summary>
        public int? SuffixLabelWidth { get; set; }
    }
    /// <summary>
    /// Režim layoutu
    /// </summary>
    internal enum LayoutModeType
    {
        None,
        Fixed,
        Flow
    }
    #endregion
}
