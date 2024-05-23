// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevExpress.XtraCharts;
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
        #region Tvorba layoutu formuláře i jednotlivých panelů

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
            var startTime = DxComponent.LogTimeCurrent;
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
            if (args.LogTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Create Layout: {DxComponent.LogTokenTimeMicrosec}", startTime);
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
        #endregion
        #region Implicitní určení hodnot do dataformu (velikosti controlů, velikosti textu)
        /// <summary>
        /// Validuje (prověří a doplní) informace o konkrétním controlu.
        /// </summary>
        /// <param name="itemData">Data o controlu</param>
        /// <returns></returns>
        internal static void ValidateControlInfo(ControlInfo itemData)
        {
            _ValidateControlSize(itemData);
            _ValidateLabelSize(itemData);
        }
        /// <summary>
        /// Validuje (tzn. naplní prázdné hodnoty) velikost Controlu podle jeho typu, obsahu a fontu
        /// </summary>
        /// <param name="itemData"></param>
        private static void _ValidateControlSize(ControlInfo itemData)
        {
            bool hasWidth = itemData.ControlWidth.HasValue;
            bool hasHeight = itemData.ControlHeight.HasValue;
            if (hasWidth && hasHeight) return;                                 // Rozměry máme, není co řešit.

            switch (itemData.ControlType)
            {
                case ControlType.Label: setControlTextSize(20, 3); break;
                case ControlType.Button: setControlTextSize(40, 12); break;
                case ControlType.BarCode: setControlSize(96, 96); break;
                case ControlType.Image: setControlSize(96, 96); break;
                case ControlType.EditBox: setControlSize(250, 96); break;
                default:
                    string name = (itemData.ColumnName ?? "").Trim().ToLower();
                    if (name == "reference_subjektu" || name.EndsWith("_refer")) setControlSize(120, 20);
                    else if (name == "nazev_subjektu" || name.EndsWith("_nazev")) setControlSize(250, 20);
                    else setControlSize(160, 20);
                    break;
            }

            // Změří aktuální text itemData.ControlText (výška a šířka podle fontu itemData.ControlFont), přidá definovaný přídavek a výsledné rozměry vloží do ControlWidth a ControlHeight
            void setControlTextSize(int addWidth, int addHeight)
            {
                string text = itemData.ControlText;
                int width = TextDimension.GetTextWidth(text, itemData.ControlFont) + addWidth;
                int height = TextDimension.GetFontHeight(itemData.ControlFont) + addHeight;
                setControlSize(width, height);
            }
            // Vloží dodané hodnoty do ControlWidth a ControlHeight, pokud tam nejsou
            void setControlSize(int width, int height)
            {
                if (!hasWidth) itemData.ControlWidth = width;
                if (!hasHeight) itemData.ControlHeight = height;
            }
        }
        /// <summary>
        /// Validuje (tzn. naplní prázdné hodnoty) velikost labelu Main a Suffix
        /// </summary>
        /// <param name="itemData"></param>
        private static void _ValidateLabelSize(ControlInfo itemData)
        {
            if (itemData.LabelPosition != LabelPositionType.None && CheckTextSize(itemData.MainLabelText, itemData.MainLabelFont, itemData.MainLabelWidth, itemData.MainLabelHeight, out var mainWidth, out var mainHeight))
            {   // Main label existuje a je třeba aktualizovat některý rozměr:
                if (mainWidth.HasValue) itemData.MainLabelWidth = mainWidth.Value;
                if (mainHeight.HasValue) itemData.MainLabelHeight = mainHeight.Value;
            }
            if (CheckTextSize(itemData.SuffixLabelText, itemData.SuffixLabelFont, itemData.SuffixLabelWidth, itemData.SuffixLabelHeight, out var sufWidth, out var sufHeight))
            {   // Suffix label existuje a je třeba aktualizovat některý jeho rozměr:
                if (sufWidth.HasValue) itemData.SuffixLabelWidth = sufWidth.Value;
                if (sufHeight.HasValue) itemData.SuffixLabelHeight = sufHeight.Value;
            }
        }
        /// <summary>
        /// Metoda ověří zadání šířky a výšky pro daný text
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fontInfo"></param>
        /// <param name="inpWidth"></param>
        /// <param name="inpHeight"></param>
        /// <param name="outWidth"></param>
        /// <param name="outHeight"></param>
        /// <returns></returns>
        internal static bool CheckTextSize(string text, DfFontInfo fontInfo, int? inpWidth, int? inpHeight, out int? outWidth, out int? outHeight)
        {
            outWidth = null;
            outHeight = null;
            if (String.IsNullOrEmpty(text)) return false;                      // Není zadán text

            bool hasWidth = inpWidth.HasValue;
            bool hasHeight = inpHeight.HasValue;
            if (hasWidth && hasHeight) return false;                           // Rozměry jsou plně určeny, není co řešit (i záporná čísla jsou akceptovaná)

            // Šířka:
            if (!hasWidth) outWidth = TextDimension.GetTextWidth(text, fontInfo);

            // Výška:
            if (!hasHeight) outHeight = TextDimension.GetFontHeight(fontInfo);

            return true;
        }
        #endregion
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
                // Sloupce (ColumnsCount a ColumnWidths):
                //  - Pokud aktuální 'dfArea' přináší nějaké informace v jedné (nebo v obou) proměnné ('ColumnWidths' nebo 'ColumnsCount'), pak se převezme z dfArea a nikoli z Parenta.
                //  - Nelze kombinovat jednu hodnotu z aktuálního 'dfArea' a druhou z 'styleParent'!
                if (dfArea != null && ((dfArea.ColumnsCount.HasValue && dfArea.ColumnsCount.Value > 0) || (!String.IsNullOrEmpty(dfArea.ColumnWidths))))
                {   // Obě hodnoty z aktuálního 'dfArea':
                    this.ColumnsCount = dfArea.ColumnsCount;
                    this.ColumnWidths = dfArea.ColumnWidths;
                }
                else if (styleParent != null)
                {   // Obě hodnoty z parenta:
                    this.ColumnsCount = styleParent.ColumnsCount;
                    this.ColumnWidths = styleParent.ColumnWidths;
                }

                // Ostatní hodnoty jsou "jednomístné" a lze je dědit jednoduše:
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
                _ProcessContainer();             // Zpracuje this container, a rekurzivně jeho Child containery etc
                _PreparePanelAbsoluteBounds();   // Naplní absolutní souřadnice do všech (i vnořených) prvků
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
                    _CalculateSpanColumns();     // Dopočítá šířky pro Span sloupce
                    _PrepareRelativeBounds();    // Určí relativní souřadnice všech Child prvků
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
                var itemData = new ControlInfo(_DfForm, __Name, __Name, ControlType.None);
                this.__ControlData = itemData;

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
                this.__ControlData = itemData;

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

                var inputTextControl = baseControl as DfBaseInputTextControl;
                if (inputTextControl != null)
                {
                    itemData.ControlText = inputTextControl.Text;
                }

                // Validace externí (tj. aplikační kód) a lokální (defaultní):
                this._LayoutArgs.InfoSource.ValidateControlInfo(itemData);
                DfTemplateLayout.ValidateControlInfo(itemData);

                // Převezmeme výsledky:
                this._ControlSize = new Size(itemData.ControlWidth, itemData.ControlHeight);
                if (itemData.LabelPosition != LabelPositionType.None && !String.IsNullOrEmpty(itemData.MainLabelText))
                    this._MainLabelSize = new Size(itemData.MainLabelWidth, itemData.MainLabelHeight);
                if (!String.IsNullOrEmpty(itemData.SuffixLabelText))
                    this._SuffixLabelSize = new Size(itemData.SuffixLabelWidth, itemData.SuffixLabelHeight);

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
                        bool isSingleCOlumn = (itemColSpan == 1);
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
            private void _CalculateSpanColumns()
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
        /// Styl písma pro text controlu
        /// </summary>
        public DfFontInfo ControlFont { get; set; }
        /// <summary>
        /// Fixní text v prvku, typicky v buttonu, v Checkboxu, Title, Label atd
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
        /// Styl písma pro main label
        /// </summary>
        public DfFontInfo MainLabelFont { get; set; }
        /// <summary>
        /// Text hlavního labelu.
        /// Pokud je null, pak není ve formuláři definováno. Pokud je "", je tím definováno 'Bez labelu'.
        /// V existující definici mohou být přítomny formátovací funkce: "fm(xxx)", "fmr(xxx)". Přípravná funkce to má vyřešit.
        /// </summary>
        public string MainLabelText { get; set; }
        /// <summary>
        /// Styl písma pro suffix label
        /// </summary>
        public DfFontInfo SuffixLabelFont { get; set; }
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
        /// <summary>
        /// Výška labelu <see cref="SuffixLabelText"/> ve standardních pixelech. 
        /// Pokud je null, pak není ve formuláři definováno, a přípravná funkce má rozměr určit. 
        /// Pokud je hodnota uvedená ve formuláři, jde uvedena zde.
        /// </summary>
        public int? SuffixLabelHeight { get; set; }
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
    #region class TextDimension : měřítko textu a fontu
    /// <summary>
    /// Třída, která změří velikost konkrétního textu v daném fontu.
    /// </summary>
    internal class TextDimension
    {
        /// <summary>
        /// Vrátí výšku daného fontu = počet pixelů pro jednořádkový text labelu.
        /// </summary>
        /// <param name="fontInfo"></param>
        /// <returns></returns>
        internal static int GetFontHeight(DfFontInfo fontInfo = null)
        {
            var height = _GetTextHeight(fontInfo?.SizeRatio);
            return _GetDefaultSize(height);
        }
        /// <summary>
        /// Vrátí šířku daného textu v daném fontu = počet pixelů pro jednořádkový text labelu.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fontInfo"></param>
        /// <returns></returns>
        internal static int GetTextWidth(string text, DfFontInfo fontInfo = null)
        {
            if (String.IsNullOrEmpty(text)) return 0;

            bool isBold = (fontInfo != null && fontInfo.Style.HasValue && fontInfo.Style.Value.HasFlag(FontStyleType.Bold));
            var width = _GetTextWidth(text, isBold, fontInfo?.SizeRatio);
            return _GetDefaultSize(width);
        }
        /// <summary>
        /// Vrátí dodanou velikost převedenou na aktuální výchozí velikost fontu 
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private static int _GetDefaultSize(float size)
        {
            return (int)(Math.Ceiling(size * DefaultFontEmSize / _FontEmSize));
        }
        /// <summary>
        /// Standardní velikost fontu
        /// </summary>
        internal const float DefaultFontEmSize = 8.25f;
        #region Private metody : tyto metody obsahují mnoho konstantních dat, která nejsou zadaná programátorem, ale vygenerovaná specifickým kódem - viz aplikace TestDevExpress, třída TestDevExpress.AsolDX.News.FontSizes
        /// <summary>
        /// Metoda vrátí vstupní text, v němž budou diakritické znaky a znaky neběžné nahrazeny běžnými.<br/>
        /// Pokud tedy na vstupu je: <c>"Černé Poříčí 519/a"</c>,<br/>
        /// pak na výstupu bude: <c>"Cerne Porici 519/a"</c>.
        /// </summary>
        /// <param name="text">Vstupní text</param>
        /// <returns></returns>
        private static string _TextToBasic(string text)
        {
            if (text == null || text.Length == 0) return text;

            StringBuilder result = new StringBuilder();
            foreach (char c in text)
                result.Append(_CharToBasic(c));
            return result.ToString();
        }
        /// <summary>
        /// Metoda vrátí Basic znak k znaku zadanému
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static char _CharToBasic(char c)
        {
            switch ((int)c)
            {
                case 32: return ' ';         // ' '
                case 33: return '!';         // '!'
                case 34: return '"';         // '"'
                case 35: return '#';         // '#'
                case 36: return '$';         // '$'
                case 37: return '%';         // '%'
                case 38: return '&';         // '&'
                case 39: return '\'';        // '''
                case 40: return '(';         // '('
                case 41: return ')';         // ')'
                case 42: return '*';         // '*'
                case 43: return '+';         // '+'
                case 44: return ',';         // ','
                case 45: return '-';         // '-'
                case 46: return '.';         // '.'
                case 47: return '/';         // '/'
                case 48: return '0';         // '0'
                case 49: return '1';         // '1'
                case 50: return '2';         // '2'
                case 51: return '3';         // '3'
                case 52: return '4';         // '4'
                case 53: return '5';         // '5'
                case 54: return '6';         // '6'
                case 55: return '7';         // '7'
                case 56: return '8';         // '8'
                case 57: return '9';         // '9'
                case 58: return ':';         // ':'
                case 59: return ';';         // ';'
                case 60: return '<';         // '<'
                case 61: return '=';         // '='
                case 62: return '>';         // '>'
                case 63: return '?';         // '?'
                case 64: return '@';         // '@'
                case 65: return 'A';         // 'A'
                case 66: return 'B';         // 'B'
                case 67: return 'C';         // 'C'
                case 68: return 'D';         // 'D'
                case 69: return 'E';         // 'E'
                case 70: return 'F';         // 'F'
                case 71: return 'G';         // 'G'
                case 72: return 'H';         // 'H'
                case 73: return 'I';         // 'I'
                case 74: return 'J';         // 'J'
                case 75: return 'K';         // 'K'
                case 76: return 'L';         // 'L'
                case 77: return 'M';         // 'M'
                case 78: return 'N';         // 'N'
                case 79: return 'O';         // 'O'
                case 80: return 'P';         // 'P'
                case 81: return 'Q';         // 'Q'
                case 82: return 'R';         // 'R'
                case 83: return 'S';         // 'S'
                case 84: return 'T';         // 'T'
                case 85: return 'U';         // 'U'
                case 86: return 'V';         // 'V'
                case 87: return 'W';         // 'W'
                case 88: return 'X';         // 'X'
                case 89: return 'Y';         // 'Y'
                case 90: return 'Z';         // 'Z'
                case 91: return '[';         // '['
                case 92: return '\\';        // '\'
                case 93: return ']';         // ']'
                case 94: return '^';         // '^'
                case 95: return '_';         // '_'
                case 96: return '\'';        // '`'
                case 97: return 'a';         // 'a'
                case 98: return 'b';         // 'b'
                case 99: return 'c';         // 'c'
                case 100: return 'd';         // 'd'
                case 101: return 'e';         // 'e'
                case 102: return 'f';         // 'f'
                case 103: return 'g';         // 'g'
                case 104: return 'h';         // 'h'
                case 105: return 'i';         // 'i'
                case 106: return 'j';         // 'j'
                case 107: return 'k';         // 'k'
                case 108: return 'l';         // 'l'
                case 109: return 'm';         // 'm'
                case 110: return 'n';         // 'n'
                case 111: return 'o';         // 'o'
                case 112: return 'p';         // 'p'
                case 113: return 'q';         // 'q'
                case 114: return 'r';         // 'r'
                case 115: return 's';         // 's'
                case 116: return 't';         // 't'
                case 117: return 'u';         // 'u'
                case 118: return 'v';         // 'v'
                case 119: return 'w';         // 'w'
                case 120: return 'x';         // 'x'
                case 121: return 'y';         // 'y'
                case 122: return 'z';         // 'z'
                case 123: return '{';         // '{'
                case 124: return '|';         // '|'
                case 125: return '}';         // '}'
                case 126: return '~';         // '~'
                case 160: return ' ';         // ' '
                case 161: return '!';         // '¡'
                case 162: return 'c';         // '¢'
                case 163: return 'L';         // '£'
                case 164: return 'x';         // '¤'
                case 165: return 'Y';         // '¥'
                case 166: return '|';         // '¦'
                case 167: return '§';         // '§'
                case 168: return '¨';         // '¨'
                case 169: return 'O';         // '©'
                case 170: return '¨';         // 'ª'
                case 171: return '«';         // '«'
                case 172: return '_';         // '¬'
                case 173: return ' ';         // '­'
                case 174: return 'O';         // '®'
                case 175: return '_';         // '¯'
                case 176: return 'o';         // '°'
                case 177: return '+';         // '±'
                case 178: return '¨';         // '²'
                case 179: return '¨';         // '³'
                case 180: return '\'';        // '´'
                case 181: return 'u';         // 'µ'
                case 182: return 'T';         // '¶'
                case 183: return '.';         // '·'
                case 184: return ',';         // '¸'
                case 185: return '¨';         // '¹'
                case 186: return 'o';         // 'º'
                case 187: return '»';         // '»'
                case 188: return 'X';         // '¼'
                case 189: return 'X';         // '½'
                case 190: return 'X';         // '¾'
                case 191: return '?';         // '¿'
                case 192: return 'A';         // 'À'
                case 193: return 'A';         // 'Á'
                case 194: return 'A';         // 'Â'
                case 195: return 'A';         // 'Ã'
                case 196: return 'A';         // 'Ä'
                case 197: return 'A';         // 'Å'
                case 198: return 'A';         // 'Æ'
                case 199: return 'C';         // 'Ç'
                case 200: return 'E';         // 'È'
                case 201: return 'E';         // 'É'
                case 202: return 'E';         // 'Ê'
                case 203: return 'E';         // 'Ë'
                case 204: return 'I';         // 'Ì'
                case 205: return 'I';         // 'Í'
                case 206: return 'I';         // 'Î'
                case 207: return 'I';         // 'Ï'
                case 208: return 'D';         // 'Ð'
                case 209: return 'N';         // 'Ñ'
                case 210: return 'O';         // 'Ò'
                case 211: return 'O';         // 'Ó'
                case 212: return 'O';         // 'Ô'
                case 213: return 'O';         // 'Õ'
                case 214: return 'O';         // 'Ö'
                case 215: return '×';         // '×'
                case 216: return 'O';         // 'Ø'
                case 217: return 'U';         // 'Ù'
                case 218: return 'U';         // 'Ú'
                case 219: return 'U';         // 'Û'
                case 220: return 'U';         // 'Ü'
                case 221: return 'Y';         // 'Ý'
                case 222: return 'P';         // 'Þ'
                case 223: return 'B';         // 'ß'
                case 224: return 'a';         // 'à'
                case 225: return 'a';         // 'á'
                case 226: return 'a';         // 'â'
                case 227: return 'a';         // 'ã'
                case 228: return 'a';         // 'ä'
                case 229: return 'a';         // 'å'
                case 230: return 'a';         // 'æ'
                case 231: return 'c';         // 'ç'
                case 232: return 'e';         // 'è'
                case 233: return 'e';         // 'é'
                case 234: return 'e';         // 'ê'
                case 235: return 'e';         // 'ë'
                case 236: return 'i';         // 'ì'
                case 237: return 'i';         // 'í'
                case 238: return 'i';         // 'î'
                case 239: return 'i';         // 'ï'
                case 240: return 'o';         // 'ð'
                case 241: return 'n';         // 'ñ'
                case 242: return 'o';         // 'ò'
                case 243: return 'o';         // 'ó'
                case 244: return 'o';         // 'ô'
                case 245: return 'o';         // 'õ'
                case 246: return 'o';         // 'ö'
                case 247: return '-';         // '÷'
                case 248: return 'o';         // 'ø'
                case 249: return 'u';         // 'ù'
                case 250: return 'u';         // 'ú'
                case 251: return 'u';         // 'û'
                case 252: return 'u';         // 'ü'
                case 253: return 'y';         // 'ý'
                case 254: return 'b';         // 'þ'
                case 255: return 'y';         // 'ÿ'
                case 256: return 'A';         // 'Ā'
                case 257: return 'a';         // 'ā'
                case 258: return 'A';         // 'Ă'
                case 259: return 'a';         // 'ă'
                case 260: return 'A';         // 'Ą'
                case 261: return 'a';         // 'ą'
                case 262: return 'C';         // 'Ć'
                case 263: return 'c';         // 'ć'
                case 264: return 'C';         // 'Ĉ'
                case 265: return 'c';         // 'ĉ'
                case 266: return 'C';         // 'Ċ'
                case 267: return 'c';         // 'ċ'
                case 268: return 'C';         // 'Č'
                case 269: return 'c';         // 'č'
                case 270: return 'D';         // 'Ď'
                case 271: return 'd';         // 'ď'
                case 272: return 'D';         // 'Đ'
                case 273: return 'd';         // 'đ'
                case 274: return 'E';         // 'Ē'
                case 275: return 'e';         // 'ē'
                case 276: return 'E';         // 'Ĕ'
                case 277: return 'e';         // 'ĕ'
                case 278: return 'E';         // 'Ė'
                case 279: return 'e';         // 'ė'
                case 280: return 'E';         // 'Ę'
                case 281: return 'e';         // 'ę'
                case 282: return 'E';         // 'Ě'
                case 283: return 'e';         // 'ě'
                case 284: return 'G';         // 'Ĝ'
                case 285: return 'g';         // 'ĝ'
                case 286: return 'G';         // 'Ğ'
                case 287: return 'g';         // 'ğ'
                case 288: return 'G';         // 'Ġ'
                case 289: return 'g';         // 'ġ'
                case 290: return 'G';         // 'Ģ'
                case 291: return 'g';         // 'ģ'
                case 292: return 'H';         // 'Ĥ'
                case 293: return 'h';         // 'ĥ'
                case 294: return 'H';         // 'Ħ'
                case 295: return 'h';         // 'ħ'
                case 296: return 'I';         // 'Ĩ'
                case 297: return 'i';         // 'ĩ'
                case 298: return 'I';         // 'Ī'
                case 299: return 'i';         // 'ī'
                case 300: return 'I';         // 'Ĭ'
                case 301: return 'i';         // 'ĭ'
                case 302: return 'I';         // 'Į'
                case 303: return 'i';         // 'į'
                case 304: return 'I';         // 'İ'
                case 305: return 'i';         // 'ı'
                case 306: return 'U';         // 'Ĳ'
                case 307: return 'u';         // 'ĳ'
                case 308: return 'J';         // 'Ĵ'
                case 309: return 'j';         // 'ĵ'
                case 310: return 'K';         // 'Ķ'
                case 311: return 'k';         // 'ķ'
                case 312: return 'k';         // 'ĸ'
                case 313: return 'L';         // 'Ĺ'
                case 314: return 'l';         // 'ĺ'
                case 315: return 'L';         // 'Ļ'
                case 316: return 'l';         // 'ļ'
                case 317: return 'L';         // 'Ľ'
                case 318: return 'l';         // 'ľ'
                case 319: return 'L';         // 'Ŀ'
                case 320: return 'l';         // 'ŀ'
                case 321: return 'L';         // 'Ł'
                case 322: return 'l';         // 'ł'
                case 323: return 'N';         // 'Ń'
                case 324: return 'n';         // 'ń'
                case 325: return 'N';         // 'Ņ'
                case 326: return 'n';         // 'ņ'
                case 327: return 'N';         // 'Ň'
                case 328: return 'n';         // 'ň'
                case 329: return 'h';         // 'ŉ'
                case 330: return 'N';         // 'Ŋ'
                case 331: return 'n';         // 'ŋ'
                case 332: return 'O';         // 'Ō'
                case 333: return 'o';         // 'ō'
                case 334: return 'O';         // 'Ŏ'
                case 335: return 'o';         // 'ŏ'
                case 336: return 'O';         // 'Ő'
                case 337: return 'o';         // 'ő'
                case 338: return 'E';         // 'Œ'
                case 339: return 'e';         // 'œ'
                case 340: return 'R';         // 'Ŕ'
                case 341: return 'r';         // 'ŕ'
                case 342: return 'R';         // 'Ŗ'
                case 343: return 'r';         // 'ŗ'
                case 344: return 'R';         // 'Ř'
                case 345: return 'r';         // 'ř'
                case 346: return 'S';         // 'Ś'
                case 347: return 's';         // 'ś'
                case 348: return 'S';         // 'Ŝ'
                case 349: return 's';         // 'ŝ'
                case 350: return 'S';         // 'Ş'
                case 351: return 's';         // 'ş'
                case 352: return 'S';         // 'Š'
                case 353: return 's';         // 'š'
                case 354: return 'T';         // 'Ţ'
                case 355: return 't';         // 'ţ'
                case 356: return 'T';         // 'Ť'
                case 357: return 't';         // 'ť'
                case 358: return 't';         // 'Ŧ'
                case 359: return 'T';         // 'ŧ'
                case 360: return 'U';         // 'Ũ'
                case 361: return 'u';         // 'ũ'
                case 362: return 'U';         // 'Ū'
                case 363: return 'u';         // 'ū'
                case 364: return 'U';         // 'Ŭ'
                case 365: return 'u';         // 'ŭ'
                case 366: return 'U';         // 'Ů'
                case 367: return 'u';         // 'ů'
                case 368: return 'U';         // 'Ű'
                case 369: return 'u';         // 'ű'
                case 370: return 'U';         // 'Ų'
                case 371: return 'u';         // 'ų'
                case 372: return 'W';         // 'Ŵ'
                case 373: return 'w';         // 'ŵ'
                case 374: return 'Y';         // 'Ŷ'
                case 375: return 'y';         // 'ŷ'
                case 376: return 'Y';         // 'Ÿ'
                case 377: return 'Z';         // 'Ź'
                case 378: return 'z';         // 'ź'
                case 379: return 'Z';         // 'Ż'
                case 380: return 'z';         // 'ż'
                case 381: return 'Z';         // 'Ž'
                case 382: return 'z';         // 'ž'
                case 461: return 'A';         // 'Ǎ'
                case 462: return 'a';         // 'ǎ'
                case 463: return 'I';         // 'Ǐ'
                case 464: return 'i';         // 'ǐ'
                case 465: return 'O';         // 'Ǒ'
                case 466: return 'o';         // 'ǒ'
                case 467: return 'U';         // 'Ǔ'
                case 468: return 'u';         // 'ǔ'
                case 469: return 'U';         // 'Ǖ'
                case 470: return 'u';         // 'ǖ'
                case 471: return 'U';         // 'Ǘ'
                case 472: return 'u';         // 'ǘ'
                case 473: return 'U';         // 'Ǚ'
                case 474: return 'u';         // 'ǚ'
                case 475: return 'U';         // 'Ǜ'
                case 476: return 'u';         // 'ǜ'
                case 477: return 'e';         // 'ǝ'
                case 478: return 'A';         // 'Ǟ'
                case 479: return 'a';         // 'ǟ'
                case 480: return 'A';         // 'Ǡ'
                case 481: return 'a';         // 'ǡ'
                case 482: return 'E';         // 'Ǣ'
                case 483: return 'e';         // 'ǣ'
                case 484: return 'G';         // 'Ǥ'
                case 485: return 'g';         // 'ǥ'
                case 486: return 'G';         // 'Ǧ'
                case 487: return 'g';         // 'ǧ'
                case 488: return 'K';         // 'Ǩ'
                case 489: return 'k';         // 'ǩ'
                case 490: return 'O';         // 'Ǫ'
                case 491: return 'o';         // 'ǫ'
                case 492: return 'O';         // 'Ǭ'
                case 493: return 'o';         // 'ǭ'
                case 494: return 'J';         // 'Ǯ'
                case 495: return 'j';         // 'ǯ'
                case 496: return 'j';         // 'ǰ'
                case 497: return 'D';         // 'Ǳ'
                case 498: return 'd';         // 'ǲ'
                case 499: return 'd';         // 'ǳ'
                case 500: return 'G';         // 'Ǵ'
                case 501: return 'g';         // 'ǵ'
                case 502: return 'F';         // 'Ƕ'
                case 503: return 'f';         // 'Ƿ'
                case 504: return 'N';         // 'Ǹ'
                case 505: return 'n';         // 'ǹ'
                case 506: return 'A';         // 'Ǻ'
                case 507: return 'a';         // 'ǻ'
                case 508: return 'A';         // 'Ǽ'
                case 509: return 'a';         // 'ǽ'
                case 510: return 'O';         // 'Ǿ'
                case 511: return 'o';         // 'ǿ'
                case 512: return 'A';         // 'Ȁ'
                case 513: return 'a';         // 'ȁ'
                case 514: return 'A';         // 'Ȃ'
                case 515: return 'a';         // 'ȃ'
                case 516: return 'E';         // 'Ȅ'
                case 517: return 'e';         // 'ȅ'
                case 518: return 'E';         // 'Ȇ'
                case 519: return 'e';         // 'ȇ'
                case 520: return 'I';         // 'Ȉ'
                case 521: return 'i';         // 'ȉ'
                case 522: return 'I';         // 'Ȋ'
                case 523: return 'i';         // 'ȋ'
                case 524: return 'O';         // 'Ȍ'
                case 525: return 'o';         // 'ȍ'
                case 526: return 'O';         // 'Ȏ'
                case 527: return 'o';         // 'ȏ'
                case 528: return 'R';         // 'Ȑ'
                case 529: return 'r';         // 'ȑ'
                case 530: return 'R';         // 'Ȓ'
                case 531: return 'r';         // 'ȓ'
                case 532: return 'U';         // 'Ȕ'
                case 533: return 'u';         // 'ȕ'
                case 534: return 'U';         // 'Ȗ'
                case 535: return 'u';         // 'ȗ'
                case 536: return 'S';         // 'Ș'
                case 537: return 's';         // 'ș'
                case 538: return 'T';         // 'Ț'
                case 539: return 't';         // 'ț'
                case 540: return 'X';         // 'Ȝ'
                case 541: return 'X';         // 'ȝ'
                case 542: return 'X';         // 'Ȟ'
                case 543: return 'X';         // 'ȟ'
                case 544: return 'X';         // 'Ƞ'
                case 545: return 'X';         // 'ȡ'
                case 546: return 'X';         // 'Ȣ'
                case 547: return 'X';         // 'ȣ'
                case 548: return 'Z';         // 'Ȥ'
                case 549: return 'z';         // 'ȥ'
                case 550: return 'A';         // 'Ȧ'
                case 551: return 'a';         // 'ȧ'
                case 552: return 'E';         // 'Ȩ'
                case 553: return 'e';         // 'ȩ'
                case 554: return 'O';         // 'Ȫ'
                case 555: return 'o';         // 'ȫ'
                case 556: return 'O';         // 'Ȭ'
                case 557: return 'o';         // 'ȭ'
                case 558: return 'O';         // 'Ȯ'
                case 559: return 'o';         // 'ȯ'
                case 560: return 'O';         // 'Ȱ'
                case 561: return 'o';         // 'ȱ'
                case 562: return 'Y';         // 'Ȳ'
                case 563: return 'y';         // 'ȳ'
            }
            return 'O';                       // Náhradní
        }
        /// <summary>
        /// Vrátí výšku textu
        /// </summary>
        /// <param name="sizeRatio"></param>
        /// <returns></returns>
        private static float _GetTextHeight(float? sizeRatio = null)
        {
            float height = _FontHeight;

            // Korekce dle úpravy velikosti:
            if (sizeRatio.HasValue && sizeRatio.Value > 0f && sizeRatio.Value != 1f)
                height = sizeRatio.Value * height;

            return height;
        }
        /// <summary>
        /// Vypočte a vrátí šířku dodaného textu
        /// </summary>
        /// <param name="text"></param>
        /// <param name="isBold"></param>
        /// <param name="sizeRatio"></param>
        /// <returns></returns>
        private static float _GetTextWidth(string text, bool isBold = false, float? sizeRatio = null)
        {
            if (text == null || text.Length == 0) return 0f;
            float width = (!isBold ? _FontMarginR : _FontMarginB);   // Základní šířka textu = Margin
            int last = text.Length - 1;
            char cPrev = ' ';
            char cCurr;
            for (int i = 0; i <= last; i++)
            {
                cCurr = _CharToBasic(text[i]);                       // Basic Znak ze vstupního textu na aktuální pozici
                if (i == 0)
                {
                    width += _GetCharWidth1(cCurr, isBold);          // První znak: beru levou polovinu jeho plné šířky (levá == pravá)
                }
                else
                {
                    width += _GetCharWidth2(cPrev, cCurr, isBold);   // Každý průběžný znak: beru vzdálenost mezi polovinou znaku Previous a polovinou znaku Current
                    if (i == last)
                        width += _GetCharWidth1(cCurr, isBold);      // Poslední znak: beru pravou polovinu jeho plné šířky (levá == pravá)
                }
                cPrev = cCurr;
            }

            // Korekce dle úpravy velikosti:
            if (sizeRatio.HasValue && sizeRatio.Value > 0f && sizeRatio.Value != 1f)
                width = sizeRatio.Value * width;

            return width;
        }
        /// <summary>
        /// Vrátí vzdálenost mezi středem znaku <paramref name="cPrev"/> a středem znaku <paramref name="cCurr"/> pro styl písma <paramref name="isBold"/>.
        /// </summary>
        /// <param name="cPrev"></param>
        /// <param name="cCurr"></param>
        /// <param name="isBold"></param>
        /// <returns></returns>
        private static float _GetCharWidth2(char cPrev, char cCurr, bool isBold)
        {
            int code = _GetCharCode(cPrev);
            switch (code)
            {
                case 0: return _GetCharWidth(cCurr, isBold, @"0v0v111N2K2h5k615k61;G;d7_7{/`/}2+2H2+2H2|3C6=6Z111N2+2H111N111N5k615k615k615k615k615k615k615k615k615k61111N111N6=6Z6=6Z6=6Z5k61=P=n7_7{7_7{8X8u8X8u7_7{6d7+9S9o8X8u111N4p577_7{5k61:L:i8X8u9S9o7_7{9S9o8X8u7_7{6d7+8X8u7_7{<@<]7_7{7_7{6d7+111N111N111N4D4`5e6+5k615k614p575k615k61111N5k615k610@0\0@0\4p570@0\:L:i5k615k615k615k612+2H4p57111N5k614p578X8u4p574p574p572-2I0m142-2I6=6Z5k612+2H5k615k616=6Z");
                case 1: return _GetCharWidth(cCurr, isBold, @"111N1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O72");
                case 2: return _GetCharWidth(cCurr, isBold, @"2K2h2]3@3v4Y7@7y7@7y<r=V949m161o3V4:3V4:4Q557h8L2]3@3V4:2]3@2]3@7@7y7@7y7@7y7@7y7@7y7@7y7@7y7@7y7@7y7@7y2]3@2]3@7h8L7h8L7h8L7@7y>|?`949m949m:-:g:-:g949m898s:~;a:-:g2]3@6E7)949m7@7y;w<[:-:g:~;a949m:~;a:-:g949m898s:-:g949m=k>N949m949m898s2]3@2]3@2]3@5o6R7:7s7@7y7@7y6E7)7@7y7@7y2]3@7@7y7@7y1k2N1k2N6E7)1k2N;w<[7@7y7@7y7@7y7@7y3V4:6E7)2]3@7@7y6E7):-:g6E7)6E7)6E7)3X4;2C2|3X4;7h8L7@7y3V4:7@7y7@7y7h8L");
                case 3: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 4: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 5: return _GetCharWidth(cCurr, isBold, @";G;d;Y<<<r=V@<@v@<@vEoFRB0Bj:2:k<R=6<R=6=N>1@eAH;Y<<<R=6;Y<<;Y<<@<@v@<@v@<@v@<@v@<@v@<@v@<@v@<@v@<@v@<@v;Y<<;Y<<@eAH@eAH@eAH@<@vGxH\B0BjB0BjC*CdC*CdB0BjA6AoCzD^C*Cd;Y<<?B?{B0Bj@<@vDsEWC*CdCzD^B0BjCzD^C*CdB0BjA6AoC*CdB0BjFgGKB0BjB0BjA6Ao;Y<<;Y<<;Y<<>k?O@6@p@<@v@<@v?B?{@<@v@<@v;Y<<@<@v@<@v:g;K:g;K?B?{:g;KDsEW@<@v@<@v@<@v@<@v<R=6?B?{;Y<<@<@v?B?{C*Cd?B?{?B?{?B?{<T=8;?;x<T=8@eAH@<@v<R=6@<@v@<@v@eAH");
                case 6: return _GetCharWidth(cCurr, isBold, @"7_7{7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_");
                case 7: return _GetCharWidth(cCurr, isBold, @"/`/}/r0U161o4U594U59:2:k6I7-.K/.0k1O0k1O1g2J4~5a/r0U0k1O/r0U/r0U4U594U594U594U594U594U594U594U594U594U59/r0U/r0U4~5a4~5a4~5a4U59<;<u6I7-6I7-7C7}7C7}6I7-5O628=8w7C7}/r0U3[4>6I7-4U59979p7C7}8=8w6I7-8=8w7C7}6I7-5O627C7}6I7-;+;d6I7-6I7-5O62/r0U/r0U/r0U3.3h4O534U594U593[4>4U594U59/r0U4U594U59/*/d/*/d3[4>/*/d979p4U594U594U594U590k1O3[4>/r0U4U593[4>7C7}3[4>3[4>3[4>0m1Q/X0<0m1Q4~5a4U590k1O4U594U594~5a");
                case 8: return _GetCharWidth(cCurr, isBold, @"2+2H2<2w3V4:6v7Z6v7Z<R=68j9N0k1O363q363q414k7H8,2<2w363q2<2w2<2w6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z2<2w2<2w7H8,7H8,7H8,6v7Z>[?@8j9N8j9N9c:H9c:H8j9N7o8S:^;B9c:H2<2w5{6_8j9N6v7Z;W<;9c:H:^;B8j9N:^;B9c:H8j9N7o8S9c:H8j9N=K>/8j9N8j9N7o8S2<2w2<2w2<2w5O636p7T6v7Z6v7Z5{6_6v7Z6v7Z2<2w6v7Z6v7Z1K2/1K2/5{6_1K2/;W<;6v7Z6v7Z6v7Z6v7Z363q5{6_2<2w6v7Z5{6_9c:H5{6_5{6_5{6_383r1x2]383r7H8,6v7Z363q6v7Z6v7Z7H8,");
                case 9: return _GetCharWidth(cCurr, isBold, @"2+2H2<2w3V4:6v7Z6v7Z<R=68j9N0k1O363q363q414k7H8,2<2w363q2<2w2<2w6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z2<2w2<2w7H8,7H8,7H8,6v7Z>[?@8j9N8j9N9c:H9c:H8j9N7o8S:^;B9c:H2<2w5{6_8j9N6v7Z;W<;9c:H:^;B8j9N:^;B9c:H8j9N7o8S9c:H8j9N=K>/8j9N8j9N7o8S2<2w2<2w2<2w5O636p7T6v7Z6v7Z5{6_6v7Z6v7Z2<2w6v7Z6v7Z1K2/1K2/5{6_1K2/;W<;6v7Z6v7Z6v7Z6v7Z363q5{6_2<2w6v7Z5{6_9c:H5{6_5{6_5{6_383r1x2]383r7H8,6v7Z363q6v7Z6v7Z7H8,");
                case 10: return _GetCharWidth(cCurr, isBold, @"2|3C383q4Q557q8U7q8U=N>19e:H1g2J414k414k5,5f8C8}383q414k383q383q7q8U7q8U7q8U7q8U7q8U7q8U7q8U7q8U7q8U7q8U383q383q8C8}8C8}8C8}7q8U?W@;9e:H9e:H:^;B:^;B9e:H8j9N;Y<<:^;B383q6v7Z9e:H7q8U<R=6:^;B;Y<<9e:H;Y<<:^;B9e:H8j9N:^;B9e:H>F?*9e:H9e:H8j9N383q383q383q6J7-7k8N7q8U7q8U6v7Z7q8U7q8U383q7q8U7q8U2F3)2F3)6v7Z2F3)<R=67q8U7q8U7q8U7q8U414k6v7Z383q7q8U6v7Z:^;B6v7Z6v7Z6v7Z434m2t3W434m8C8}7q8U414k7q8U7q8U8C8}");
                case 11: return _GetCharWidth(cCurr, isBold, @"6=6Z6O727h8L;2;k;2;k@eAH<|=_4~5a7H8,7H8,8C8};Z<>6O727H8,6O726O72;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k6O726O72;Z<>;Z<>;Z<>;2;kBnCR<|=_<|=_=u>Y=u>Y<|=_<+<e>p?S=u>Y6O72:7:q<|=_;2;k?i@M=u>Y>p?S<|=_>p?S=u>Y<|=_<+<e=u>Y<|=_A]BA<|=_<|=_<+<e6O726O726O729a:D;,;e;2;k;2;k:7:q;2;k;2;k6O72;2;k;2;k5]6@5]6@:7:q5]6@?i@M;2;k;2;k;2;k;2;k7H8,:7:q6O72;2;k:7:q=u>Y:7:q:7:q:7:q7J8.656n7J8.;Z<>;2;k7H8,;2;k;2;k;Z<>");
                case 12: return _GetCharWidth(cCurr, isBold, @"111N1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O72");
                case 13: return _GetCharWidth(cCurr, isBold, @"2+2H2<2w3V4:6v7Z6v7Z<R=68j9N0k1O363q363q414k7H8,2<2w363q2<2w2<2w6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z2<2w2<2w7H8,7H8,7H8,6v7Z>[?@8j9N8j9N9c:H9c:H8j9N7o8S:^;B9c:H2<2w5{6_8j9N6v7Z;W<;9c:H:^;B8j9N:^;B9c:H8j9N7o8S9c:H8j9N=K>/8j9N8j9N7o8S2<2w2<2w2<2w5O636p7T6v7Z6v7Z5{6_6v7Z6v7Z2<2w6v7Z6v7Z1K2/1K2/5{6_1K2/;W<;6v7Z6v7Z6v7Z6v7Z363q5{6_2<2w6v7Z5{6_9c:H5{6_5{6_5{6_383r1x2]383r7H8,6v7Z363q6v7Z6v7Z7H8,");
                case 14: return _GetCharWidth(cCurr, isBold, @"111N1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O72");
                case 15: return _GetCharWidth(cCurr, isBold, @"111N1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O72");
                case 16: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 17: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 18: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 19: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 20: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 21: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 22: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 23: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 24: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 25: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 26: return _GetCharWidth(cCurr, isBold, @"111N1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O72");
                case 27: return _GetCharWidth(cCurr, isBold, @"111N1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O72");
                case 28: return _GetCharWidth(cCurr, isBold, @"6=6Z6O727h8L;2;k;2;k@eAH<|=_4~5a7H8,7H8,8C8};Z<>6O727H8,6O726O72;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k6O726O72;Z<>;Z<>;Z<>;2;kBnCR<|=_<|=_=u>Y=u>Y<|=_<+<e>p?S=u>Y6O72:7:q<|=_;2;k?i@M=u>Y>p?S<|=_>p?S=u>Y<|=_<+<e=u>Y<|=_A]BA<|=_<|=_<+<e6O726O726O729a:D;,;e;2;k;2;k:7:q;2;k;2;k6O72;2;k;2;k5]6@5]6@:7:q5]6@?i@M;2;k;2;k;2;k;2;k7H8,:7:q6O72;2;k:7:q=u>Y:7:q:7:q:7:q7J8.656n7J8.;Z<>;2;k7H8,;2;k;2;k;Z<>");
                case 29: return _GetCharWidth(cCurr, isBold, @"6=6Z6O727h8L;2;k;2;k@eAH<|=_4~5a7H8,7H8,8C8};Z<>6O727H8,6O726O72;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k6O726O72;Z<>;Z<>;Z<>;2;kBnCR<|=_<|=_=u>Y=u>Y<|=_<+<e>p?S=u>Y6O72:7:q<|=_;2;k?i@M=u>Y>p?S<|=_>p?S=u>Y<|=_<+<e=u>Y<|=_A]BA<|=_<|=_<+<e6O726O726O729a:D;,;e;2;k;2;k:7:q;2;k;2;k6O72;2;k;2;k5]6@5]6@:7:q5]6@?i@M;2;k;2;k;2;k;2;k7H8,:7:q6O72;2;k:7:q=u>Y:7:q:7:q:7:q7J8.656n7J8.;Z<>;2;k7H8,;2;k;2;k;Z<>");
                case 30: return _GetCharWidth(cCurr, isBold, @"6=6Z6O727h8L;2;k;2;k@eAH<|=_4~5a7H8,7H8,8C8};Z<>6O727H8,6O726O72;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k6O726O72;Z<>;Z<>;Z<>;2;kBnCR<|=_<|=_=u>Y=u>Y<|=_<+<e>p?S=u>Y6O72:7:q<|=_;2;k?i@M=u>Y>p?S<|=_>p?S=u>Y<|=_<+<e=u>Y<|=_A]BA<|=_<|=_<+<e6O726O726O729a:D;,;e;2;k;2;k:7:q;2;k;2;k6O72;2;k;2;k5]6@5]6@:7:q5]6@?i@M;2;k;2;k;2;k;2;k7H8,:7:q6O72;2;k:7:q=u>Y:7:q:7:q:7:q7J8.656n7J8.;Z<>;2;k7H8,;2;k;2;k;Z<>");
                case 31: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 32: return _GetCharWidth(cCurr, isBold, @"=P=n=b>F>|?`BEC*BEC*GxH\D9Ds<;<u>[?@>[?@?W@;BnCR=b>F>[?@=b>F=b>FBEC*BEC*BEC*BEC*BEC*BEC*BEC*BEC*BEC*BEC*=b>F=b>FBnCRBnCRBnCRBEC*J+JfD9DsD9DsE3EmE3EmD9DsC?CyF-FgE3Em=b>FAKB/D9DsBEC*F}GaE3EmF-FgD9DsF-FgE3EmD9DsC?CyE3EmD9DsHqIUD9DsD9DsC?Cy=b>F=b>F=b>F@tAXB?ByBEC*BEC*AKB/BEC*BEC*=b>FBEC*BEC*<p=T<p=TAKB/<p=TF}GaBEC*BEC*BEC*BEC*>[?@AKB/=b>FBEC*AKB/E3EmAKB/AKB/AKB/>^?B=H>,>^?BBnCRBEC*>[?@BEC*BEC*BnCR");
                case 33: return _GetCharWidth(cCurr, isBold, @"7_7{7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_");
                case 34: return _GetCharWidth(cCurr, isBold, @"7_7{7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_");
                case 35: return _GetCharWidth(cCurr, isBold, @"8X8u8j9N:-:g=M>1=M>1C*Cd?A?{7C7}9c:H9c:H:^;B=u>Y8j9N9c:H8j9N8j9N=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>18j9N8j9N=u>Y=u>Y=u>Y=M>1E3Em?A?{?A?{@:@u@:@u?A?{>F?*A5Ao@:@u8j9N<R=6?A?{=M>1B.Bh@:@uA5Ao?A?{A5Ao@:@u?A?{>F?*@:@u?A?{CxD\?A?{?A?{>F?*8j9N8j9N8j9N;|<`=G>+=M>1=M>1<R=6=M>1=M>18j9N=M>1=M>17x8\7x8\<R=67x8\B.Bh=M>1=M>1=M>1=M>19c:H<R=68j9N=M>1<R=6@:@u<R=6<R=6<R=69e:I8P949e:I=u>Y=M>19c:H=M>1=M>1=u>Y");
                case 36: return _GetCharWidth(cCurr, isBold, @"8X8u8j9N:-:g=M>1=M>1C*Cd?A?{7C7}9c:H9c:H:^;B=u>Y8j9N9c:H8j9N8j9N=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>18j9N8j9N=u>Y=u>Y=u>Y=M>1E3Em?A?{?A?{@:@u@:@u?A?{>F?*A5Ao@:@u8j9N<R=6?A?{=M>1B.Bh@:@uA5Ao?A?{A5Ao@:@u?A?{>F?*@:@u?A?{CxD\?A?{?A?{>F?*8j9N8j9N8j9N;|<`=G>+=M>1=M>1<R=6=M>1=M>18j9N=M>1=M>17x8\7x8\<R=67x8\B.Bh=M>1=M>1=M>1=M>19c:H<R=68j9N=M>1<R=6@:@u<R=6<R=6<R=69e:I8P949e:I=u>Y=M>19c:H=M>1=M>1=u>Y");
                case 37: return _GetCharWidth(cCurr, isBold, @"7_7{7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_");
                case 38: return _GetCharWidth(cCurr, isBold, @"6d7+6v7Y898s;Y<<;Y<<A6Ao=M>05O627o8S7o8S8j9N<+<e6v7Y7o8S6v7Y6v7Y;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<6v7Y6v7Y<+<e<+<e<+<e;Y<<C?Cy=M>0=M>0>F?*>F?*=M>0<R=6?A?z>F?*6v7Y:^;B=M>0;Y<<@:@t>F?*?A?z=M>0?A?z>F?*=M>0<R=6>F?*=M>0B.Bh=M>0=M>0<R=66v7Y6v7Y6v7Y:2:k;S<6;Y<<;Y<<:^;B;Y<<;Y<<6v7Y;Y<<;Y<<6.6g6.6g:^;B6.6g@:@t;Y<<;Y<<;Y<<;Y<<7o8S:^;B6v7Y;Y<<:^;B>F?*:^;B:^;B:^;B7q8U6\7?7q8U<+<e;Y<<7o8S;Y<<;Y<<<+<e");
                case 39: return _GetCharWidth(cCurr, isBold, @"9S9o9d:H:~;a>H?+>H?+CzD^@<@u8=8w:^;B:^;B;Y<<>p?S9d:H:^;B9d:H9d:H>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+9d:H9d:H>p?S>p?S>p?S>H?+F-Fg@<@u@<@uA5AoA5Ao@<@u?A?zB0BiA5Ao9d:H=M>0@<@u>H?+C)CbA5AoB0Bi@<@uB0BiA5Ao@<@u?A?zA5Ao@<@uDsEV@<@u@<@u?A?z9d:H9d:H9d:H<w=Z>A>{>H?+>H?+=M>0>H?+>H?+9d:H>H?+>H?+8s9V8s9V=M>08s9VC)Cb>H?+>H?+>H?+>H?+:^;B=M>09d:H>H?+=M>0A5Ao=M>0=M>0=M>0:`;C9J:.:`;C>p?S>H?+:^;B>H?+>H?+>p?S");
                case 40: return _GetCharWidth(cCurr, isBold, @"8X8u8j9N:-:g=M>1=M>1C*Cd?A?{7C7}9c:H9c:H:^;B=u>Y8j9N9c:H8j9N8j9N=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>18j9N8j9N=u>Y=u>Y=u>Y=M>1E3Em?A?{?A?{@:@u@:@u?A?{>F?*A5Ao@:@u8j9N<R=6?A?{=M>1B.Bh@:@uA5Ao?A?{A5Ao@:@u?A?{>F?*@:@u?A?{CxD\?A?{?A?{>F?*8j9N8j9N8j9N;|<`=G>+=M>1=M>1<R=6=M>1=M>18j9N=M>1=M>17x8\7x8\<R=67x8\B.Bh=M>1=M>1=M>1=M>19c:H<R=68j9N=M>1<R=6@:@u<R=6<R=6<R=69e:I8P949e:I=u>Y=M>19c:H=M>1=M>1=u>Y");
                case 41: return _GetCharWidth(cCurr, isBold, @"111N1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O72");
                case 42: return _GetCharWidth(cCurr, isBold, @"4p575,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q");
                case 43: return _GetCharWidth(cCurr, isBold, @"7_7{7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_");
                case 44: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 45: return _GetCharWidth(cCurr, isBold, @":L:i:^;A;w<[?A?z?A?zDsEWA5An979p;W<;;W<;<R=6?i@M:^;A;W<;:^;A:^;A?A?z?A?z?A?z?A?z?A?z?A?z?A?z?A?z?A?z?A?z:^;A:^;A?i@M?i@M?i@M?A?zF}GaA5AnA5AnB.BhB.BhA5An@:@tC)CbB.Bh:^;A>F?*A5An?A?zCxD\B.BhC)CbA5AnC)CbB.BhA5An@:@tB.BhA5AnElFPA5AnA5An@:@t:^;A:^;A:^;A=p>S?;?t?A?z?A?z>F?*?A?z?A?z:^;A?A?z?A?z9l:O9l:O>F?*9l:OCxD\?A?z?A?z?A?z?A?z;W<;>F?*:^;A?A?z>F?*B.Bh>F?*>F?*>F?*;Y<<:D:};Y<<?i@M?A?z;W<;?A?z?A?z?i@M");
                case 46: return _GetCharWidth(cCurr, isBold, @"8X8u8j9N:-:g=M>1=M>1C*Cd?A?{7C7}9c:H9c:H:^;B=u>Y8j9N9c:H8j9N8j9N=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>18j9N8j9N=u>Y=u>Y=u>Y=M>1E3Em?A?{?A?{@:@u@:@u?A?{>F?*A5Ao@:@u8j9N<R=6?A?{=M>1B.Bh@:@uA5Ao?A?{A5Ao@:@u?A?{>F?*@:@u?A?{CxD\?A?{?A?{>F?*8j9N8j9N8j9N;|<`=G>+=M>1=M>1<R=6=M>1=M>18j9N=M>1=M>17x8\7x8\<R=67x8\B.Bh=M>1=M>1=M>1=M>19c:H<R=68j9N=M>1<R=6@:@u<R=6<R=6<R=69e:I8P949e:I=u>Y=M>19c:H=M>1=M>1=u>Y");
                case 47: return _GetCharWidth(cCurr, isBold, @"9S9o9d:H:~;a>H?+>H?+CzD^@<@u8=8w:^;B:^;B;Y<<>p?S9d:H:^;B9d:H9d:H>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+9d:H9d:H>p?S>p?S>p?S>H?+F-Fg@<@u@<@uA5AoA5Ao@<@u?A?zB0BiA5Ao9d:H=M>0@<@u>H?+C)CbA5AoB0Bi@<@uB0BiA5Ao@<@u?A?zA5Ao@<@uDsEV@<@u@<@u?A?z9d:H9d:H9d:H<w=Z>A>{>H?+>H?+=M>0>H?+>H?+9d:H>H?+>H?+8s9V8s9V=M>08s9VC)Cb>H?+>H?+>H?+>H?+:^;B=M>09d:H>H?+=M>0A5Ao=M>0=M>0=M>0:`;C9J:.:`;C>p?S>H?+:^;B>H?+>H?+>p?S");
                case 48: return _GetCharWidth(cCurr, isBold, @"7_7{7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_");
                case 49: return _GetCharWidth(cCurr, isBold, @"9S9o9d:H:~;a>H?+>H?+CzD^@<@u8=8w:^;B:^;B;Y<<>p?S9d:H:^;B9d:H9d:H>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+9d:H9d:H>p?S>p?S>p?S>H?+F-Fg@<@u@<@uA5AoA5Ao@<@u?A?zB0BiA5Ao9d:H=M>0@<@u>H?+C)CbA5AoB0Bi@<@uB0BiA5Ao@<@u?A?zA5Ao@<@uDsEV@<@u@<@u?A?z9d:H9d:H9d:H<w=Z>A>{>H?+>H?+=M>0>H?+>H?+9d:H>H?+>H?+8s9V8s9V=M>08s9VC)Cb>H?+>H?+>H?+>H?+:^;B=M>09d:H>H?+=M>0A5Ao=M>0=M>0=M>0:`;C9J:.:`;C>p?S>H?+:^;B>H?+>H?+>p?S");
                case 50: return _GetCharWidth(cCurr, isBold, @"8X8u8j9N:-:g=M>1=M>1C*Cd?A?{7C7}9c:H9c:H:^;B=u>Y8j9N9c:H8j9N8j9N=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>18j9N8j9N=u>Y=u>Y=u>Y=M>1E3Em?A?{?A?{@:@u@:@u?A?{>F?*A5Ao@:@u8j9N<R=6?A?{=M>1B.Bh@:@uA5Ao?A?{A5Ao@:@u?A?{>F?*@:@u?A?{CxD\?A?{?A?{>F?*8j9N8j9N8j9N;|<`=G>+=M>1=M>1<R=6=M>1=M>18j9N=M>1=M>17x8\7x8\<R=67x8\B.Bh=M>1=M>1=M>1=M>19c:H<R=68j9N=M>1<R=6@:@u<R=6<R=6<R=69e:I8P949e:I=u>Y=M>19c:H=M>1=M>1=u>Y");
                case 51: return _GetCharWidth(cCurr, isBold, @"7_7{7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_");
                case 52: return _GetCharWidth(cCurr, isBold, @"6d7+6v7Y898s;Y<<;Y<<A6Ao=M>05O627o8S7o8S8j9N<+<e6v7Y7o8S6v7Y6v7Y;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<6v7Y6v7Y<+<e<+<e<+<e;Y<<C?Cy=M>0=M>0>F?*>F?*=M>0<R=6?A?z>F?*6v7Y:^;B=M>0;Y<<@:@t>F?*?A?z=M>0?A?z>F?*=M>0<R=6>F?*=M>0B.Bh=M>0=M>0<R=66v7Y6v7Y6v7Y:2:k;S<6;Y<<;Y<<:^;B;Y<<;Y<<6v7Y;Y<<;Y<<6.6g6.6g:^;B6.6g@:@t;Y<<;Y<<;Y<<;Y<<7o8S:^;B6v7Y;Y<<:^;B>F?*:^;B:^;B:^;B7q8U6\7?7q8U<+<e;Y<<7o8S;Y<<;Y<<<+<e");
                case 53: return _GetCharWidth(cCurr, isBold, @"8X8u8j9N:-:g=M>1=M>1C*Cd?A?{7C7}9c:H9c:H:^;B=u>Y8j9N9c:H8j9N8j9N=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>18j9N8j9N=u>Y=u>Y=u>Y=M>1E3Em?A?{?A?{@:@u@:@u?A?{>F?*A5Ao@:@u8j9N<R=6?A?{=M>1B.Bh@:@uA5Ao?A?{A5Ao@:@u?A?{>F?*@:@u?A?{CxD\?A?{?A?{>F?*8j9N8j9N8j9N;|<`=G>+=M>1=M>1<R=6=M>1=M>18j9N=M>1=M>17x8\7x8\<R=67x8\B.Bh=M>1=M>1=M>1=M>19c:H<R=68j9N=M>1<R=6@:@u<R=6<R=6<R=69e:I8P949e:I=u>Y=M>19c:H=M>1=M>1=u>Y");
                case 54: return _GetCharWidth(cCurr, isBold, @"7_7{7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_");
                case 55: return _GetCharWidth(cCurr, isBold, @"<@<]<R=5=k>NA5AnA5AnFgGKC)Cb;+;d=K>/=K>/>F?*A]BA<R=5=K>/<R=5<R=5A5AnA5AnA5AnA5AnA5AnA5AnA5AnA5AnA5AnA5An<R=5<R=5A]BAA]BAA]BAA5AnHqIUC)CbC)CbCxD\CxD\C)CbB.BhDsEVCxD\<R=5@:@tC)CbA5AnElFPCxD\DsEVC)CbDsEVCxD\C)CbB.BhCxD\C)CbG`HCC)CbC)CbB.Bh<R=5<R=5<R=5?d@GA/AhA5AnA5An@:@tA5AnA5An<R=5A5AnA5An;`<C;`<C@:@t;`<CElFPA5AnA5AnA5AnA5An=K>/@:@t<R=5A5An@:@tCxD\@:@t@:@t@:@t=M>0<8<q=M>0A]BAA5An=K>/A5AnA5AnA]BA");
                case 56: return _GetCharWidth(cCurr, isBold, @"7_7{7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_");
                case 57: return _GetCharWidth(cCurr, isBold, @"7_7{7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_");
                case 58: return _GetCharWidth(cCurr, isBold, @"6d7+6v7Y898s;Y<<;Y<<A6Ao=M>05O627o8S7o8S8j9N<+<e6v7Y7o8S6v7Y6v7Y;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<6v7Y6v7Y<+<e<+<e<+<e;Y<<C?Cy=M>0=M>0>F?*>F?*=M>0<R=6?A?z>F?*6v7Y:^;B=M>0;Y<<@:@t>F?*?A?z=M>0?A?z>F?*=M>0<R=6>F?*=M>0B.Bh=M>0=M>0<R=66v7Y6v7Y6v7Y:2:k;S<6;Y<<;Y<<:^;B;Y<<;Y<<6v7Y;Y<<;Y<<6.6g6.6g:^;B6.6g@:@t;Y<<;Y<<;Y<<;Y<<7o8S:^;B6v7Y;Y<<:^;B>F?*:^;B:^;B:^;B7q8U6\7?7q8U<+<e;Y<<7o8S;Y<<;Y<<<+<e");
                case 59: return _GetCharWidth(cCurr, isBold, @"111N1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O72");
                case 60: return _GetCharWidth(cCurr, isBold, @"111N1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O72");
                case 61: return _GetCharWidth(cCurr, isBold, @"111N1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O72");
                case 62: return _GetCharWidth(cCurr, isBold, @"4D4`4U595o6R999r999r>k?O;-;f3.3h5O635O636J7-9a:D4U595O634U594U59999r999r999r999r999r999r999r999r999r999r4U594U599a:D9a:D9a:D999r@tAX;-;f;-;f;|<`;|<`;-;f:2:k<w=Z;|<`4U598>8w;-;f999r=p>S;|<`<w=Z;-;f<w=Z;|<`;-;f:2:k;|<`;-;f?d@G;-;f;-;f:2:k4U594U594U597h8K929l999r999r8>8w999r999r4U59999r999r3d4G3d4G8>8w3d4G=p>S999r999r999r999r5O638>8w4U59999r8>8w;|<`8>8w8>8w8>8w5Q644;4u5Q649a:D999r5O63999r999r9a:D");
                case 63: return _GetCharWidth(cCurr, isBold, @"5e6+5v6Z7:7s:Z;=:Z;=@6@p<N=14O536p7T6p7T7k8N;,;e5v6Z6p7T5v6Z5v6Z:Z;=:Z;=:Z;=:Z;=:Z;=:Z;=:Z;=:Z;=:Z;=:Z;=5v6Z5v6Z;,;e;,;e;,;e:Z;=B?By<N=1<N=1=G>+=G>+<N=1;S<6>A>{=G>+5v6Z9_:B<N=1:Z;=?;?t=G>+>A>{<N=1>A>{=G>+<N=1;S<6=G>+<N=1A/Ah<N=1<N=1;S<65v6Z5v6Z5v6Z929l:S;7:Z;=:Z;=9_:B:Z;=:Z;=5v6Z:Z;=:Z;=5/5h5/5h9_:B5/5h?;?t:Z;=:Z;=:Z;=:Z;=6p7T9_:B5v6Z:Z;=9_:B=G>+9_:B9_:B9_:B6r7U5\6@6r7U;,;e:Z;=6p7T:Z;=:Z;=;,;e");
                case 64: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 65: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 66: return _GetCharWidth(cCurr, isBold, @"4p575,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q");
                case 67: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 68: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 69: return _GetCharWidth(cCurr, isBold, @"111N1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O72");
                case 70: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 71: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 72: return _GetCharWidth(cCurr, isBold, @"0@0\0Q151k2N555n555n:g;K7)7b/*/d1K2/1K2/2F3)5]6@0Q151K2/0Q150Q15555n555n555n555n555n555n555n555n555n555n0Q150Q155]6@5]6@5]6@555n<p=T7)7b7)7b7x8\7x8\7)7b6.6g8s9V7x8\0Q154:4s7)7b555n9l:O7x8\8s9V7)7b8s9V7x8\7)7b6.6g7x8\7)7b;`<C7)7b7)7b6.6g0Q150Q150Q153d4G5/5h555n555n4:4s555n555n0Q15555n555n/`0C/`0C4:4s/`0C9l:O555n555n555n555n1K2/4:4s0Q15555n4:4s7x8\4:4s4:4s4:4s1M20070q1M205]6@555n1K2/555n555n5]6@");
                case 73: return _GetCharWidth(cCurr, isBold, @"0@0\0Q151k2N555n555n:g;K7)7b/*/d1K2/1K2/2F3)5]6@0Q151K2/0Q150Q15555n555n555n555n555n555n555n555n555n555n0Q150Q155]6@5]6@5]6@555n<p=T7)7b7)7b7x8\7x8\7)7b6.6g8s9V7x8\0Q154:4s7)7b555n9l:O7x8\8s9V7)7b8s9V7x8\7)7b6.6g7x8\7)7b;`<C7)7b7)7b6.6g0Q150Q150Q153d4G5/5h555n555n4:4s555n555n0Q15555n555n/`0C/`0C4:4s/`0C9l:O555n555n555n555n1K2/4:4s0Q15555n4:4s7x8\4:4s4:4s4:4s1M20070q1M205]6@555n1K2/555n555n5]6@");
                case 74: return _GetCharWidth(cCurr, isBold, @"4p575,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q");
                case 75: return _GetCharWidth(cCurr, isBold, @"0@0\0Q151k2N555n555n:g;K7)7b/*/d1K2/1K2/2F3)5]6@0Q151K2/0Q150Q15555n555n555n555n555n555n555n555n555n555n0Q150Q155]6@5]6@5]6@555n<p=T7)7b7)7b7x8\7x8\7)7b6.6g8s9V7x8\0Q154:4s7)7b555n9l:O7x8\8s9V7)7b8s9V7x8\7)7b6.6g7x8\7)7b;`<C7)7b7)7b6.6g0Q150Q150Q153d4G5/5h555n555n4:4s555n555n0Q15555n555n/`0C/`0C4:4s/`0C9l:O555n555n555n555n1K2/4:4s0Q15555n4:4s7x8\4:4s4:4s4:4s1M20070q1M205]6@555n1K2/555n555n5]6@");
                case 76: return _GetCharWidth(cCurr, isBold, @":L:i:^;A;w<[?A?z?A?zDsEWA5An979p;W<;;W<;<R=6?i@M:^;A;W<;:^;A:^;A?A?z?A?z?A?z?A?z?A?z?A?z?A?z?A?z?A?z?A?z:^;A:^;A?i@M?i@M?i@M?A?zF}GaA5AnA5AnB.BhB.BhA5An@:@tC)CbB.Bh:^;A>F?*A5An?A?zCxD\B.BhC)CbA5AnC)CbB.BhA5An@:@tB.BhA5AnElFPA5AnA5An@:@t:^;A:^;A:^;A=p>S?;?t?A?z?A?z>F?*?A?z?A?z:^;A?A?z?A?z9l:O9l:O>F?*9l:OCxD\?A?z?A?z?A?z?A?z;W<;>F?*:^;A?A?z>F?*B.Bh>F?*>F?*>F?*;Y<<:D:};Y<<?i@M?A?z;W<;?A?z?A?z?i@M");
                case 77: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 78: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 79: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 80: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 81: return _GetCharWidth(cCurr, isBold, @"2+2H2<2w3V4:6v7Z6v7Z<R=68j9N0k1O363q363q414k7H8,2<2w363q2<2w2<2w6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z2<2w2<2w7H8,7H8,7H8,6v7Z>[?@8j9N8j9N9c:H9c:H8j9N7o8S:^;B9c:H2<2w5{6_8j9N6v7Z;W<;9c:H:^;B8j9N:^;B9c:H8j9N7o8S9c:H8j9N=K>/8j9N8j9N7o8S2<2w2<2w2<2w5O636p7T6v7Z6v7Z5{6_6v7Z6v7Z2<2w6v7Z6v7Z1K2/1K2/5{6_1K2/;W<;6v7Z6v7Z6v7Z6v7Z363q5{6_2<2w6v7Z5{6_9c:H5{6_5{6_5{6_383r1x2]383r7H8,6v7Z363q6v7Z6v7Z7H8,");
                case 82: return _GetCharWidth(cCurr, isBold, @"4p575,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q");
                case 83: return _GetCharWidth(cCurr, isBold, @"111N1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O72");
                case 84: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 85: return _GetCharWidth(cCurr, isBold, @"4p575,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q");
                case 86: return _GetCharWidth(cCurr, isBold, @"8X8u8j9N:-:g=M>1=M>1C*Cd?A?{7C7}9c:H9c:H:^;B=u>Y8j9N9c:H8j9N8j9N=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>18j9N8j9N=u>Y=u>Y=u>Y=M>1E3Em?A?{?A?{@:@u@:@u?A?{>F?*A5Ao@:@u8j9N<R=6?A?{=M>1B.Bh@:@uA5Ao?A?{A5Ao@:@u?A?{>F?*@:@u?A?{CxD\?A?{?A?{>F?*8j9N8j9N8j9N;|<`=G>+=M>1=M>1<R=6=M>1=M>18j9N=M>1=M>17x8\7x8\<R=67x8\B.Bh=M>1=M>1=M>1=M>19c:H<R=68j9N=M>1<R=6@:@u<R=6<R=6<R=69e:I8P949e:I=u>Y=M>19c:H=M>1=M>1=u>Y");
                case 87: return _GetCharWidth(cCurr, isBold, @"4p575,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q");
                case 88: return _GetCharWidth(cCurr, isBold, @"4p575,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q");
                case 89: return _GetCharWidth(cCurr, isBold, @"4p575,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q");
                case 90: return _GetCharWidth(cCurr, isBold, @"2-2I2?2x3X4;6x7[6x7[<T=88l9O0m1Q383r383r434m7J8.2?2x383r2?2x2?2x6x7[6x7[6x7[6x7[6x7[6x7[6x7[6x7[6x7[6x7[2?2x2?2x7J8.7J8.7J8.6x7[>^?B8l9O8l9O9e:I9e:I8l9O7q8U:`;C9e:I2?2x5}6a8l9O6x7[;Y<<9e:I:`;C8l9O:`;C9e:I8l9O7q8U9e:I8l9O=M>08l9O8l9O7q8U2?2x2?2x2?2x5Q646r7U6x7[6x7[5}6a6x7[6x7[2?2x6x7[6x7[1M201M205}6a1M20;Y<<6x7[6x7[6x7[6x7[383r5}6a2?2x6x7[5}6a9e:I5}6a5}6a5}6a3:3s1{2^3:3s7J8.6x7[383r6x7[6x7[7J8.");
                case 91: return _GetCharWidth(cCurr, isBold, @"0m141)1c2C2|5b6F5b6F;?;x7V8:/X0<1x2]1x2]2t3W656n1)1c1x2]1)1c1)1c5b6F5b6F5b6F5b6F5b6F5b6F5b6F5b6F5b6F5b6F1)1c1)1c656n656n656n5b6F=H>,7V8:7V8:8P948P947V8:6\7?9J:.8P941)1c4h5K7V8:5b6F:D:}8P949J:.7V8:9J:.8P947V8:6\7?8P947V8:<8<q7V8:7V8:6\7?1)1c1)1c1)1c4;4u5\6@5b6F5b6F4h5K5b6F5b6F1)1c5b6F5b6F070q070q4h5K070q:D:}5b6F5b6F5b6F5b6F1x2]4h5K1)1c5b6F4h5K8P944h5K4h5K4h5K1{2^0e1I1{2^656n5b6F1x2]5b6F5b6F656n");
                case 92: return _GetCharWidth(cCurr, isBold, @"2-2I2?2x3X4;6x7[6x7[<T=88l9O0m1Q383r383r434m7J8.2?2x383r2?2x2?2x6x7[6x7[6x7[6x7[6x7[6x7[6x7[6x7[6x7[6x7[2?2x2?2x7J8.7J8.7J8.6x7[>^?B8l9O8l9O9e:I9e:I8l9O7q8U:`;C9e:I2?2x5}6a8l9O6x7[;Y<<9e:I:`;C8l9O:`;C9e:I8l9O7q8U9e:I8l9O=M>08l9O8l9O7q8U2?2x2?2x2?2x5Q646r7U6x7[6x7[5}6a6x7[6x7[2?2x6x7[6x7[1M201M205}6a1M20;Y<<6x7[6x7[6x7[6x7[383r5}6a2?2x6x7[5}6a9e:I5}6a5}6a5}6a3:3s1{2^3:3s7J8.6x7[383r6x7[6x7[7J8.");
                case 93: return _GetCharWidth(cCurr, isBold, @"6=6Z6O727h8L;2;k;2;k@eAH<|=_4~5a7H8,7H8,8C8};Z<>6O727H8,6O726O72;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k6O726O72;Z<>;Z<>;Z<>;2;kBnCR<|=_<|=_=u>Y=u>Y<|=_<+<e>p?S=u>Y6O72:7:q<|=_;2;k?i@M=u>Y>p?S<|=_>p?S=u>Y<|=_<+<e=u>Y<|=_A]BA<|=_<|=_<+<e6O726O726O729a:D;,;e;2;k;2;k:7:q;2;k;2;k6O72;2;k;2;k5]6@5]6@:7:q5]6@?i@M;2;k;2;k;2;k;2;k7H8,:7:q6O72;2;k:7:q=u>Y:7:q:7:q:7:q7J8.656n7J8.;Z<>;2;k7H8,;2;k;2;k;Z<>");
                case 94: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 95: return _GetCharWidth(cCurr, isBold, @"2+2H2<2w3V4:6v7Z6v7Z<R=68j9N0k1O363q363q414k7H8,2<2w363q2<2w2<2w6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z2<2w2<2w7H8,7H8,7H8,6v7Z>[?@8j9N8j9N9c:H9c:H8j9N7o8S:^;B9c:H2<2w5{6_8j9N6v7Z;W<;9c:H:^;B8j9N:^;B9c:H8j9N7o8S9c:H8j9N=K>/8j9N8j9N7o8S2<2w2<2w2<2w5O636p7T6v7Z6v7Z5{6_6v7Z6v7Z2<2w6v7Z6v7Z1K2/1K2/5{6_1K2/;W<;6v7Z6v7Z6v7Z6v7Z363q5{6_2<2w6v7Z5{6_9c:H5{6_5{6_5{6_383r1x2]383r7H8,6v7Z363q6v7Z6v7Z7H8,");
                case 96: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 97: return _GetCharWidth(cCurr, isBold, @"5k615|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k");
                case 98: return _GetCharWidth(cCurr, isBold, @"6=6Z6O727h8L;2;k;2;k@eAH<|=_4~5a7H8,7H8,8C8};Z<>6O727H8,6O726O72;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k6O726O72;Z<>;Z<>;Z<>;2;kBnCR<|=_<|=_=u>Y=u>Y<|=_<+<e>p?S=u>Y6O72:7:q<|=_;2;k?i@M=u>Y>p?S<|=_>p?S=u>Y<|=_<+<e=u>Y<|=_A]BA<|=_<|=_<+<e6O726O726O729a:D;,;e;2;k;2;k:7:q;2;k;2;k6O72;2;k;2;k5]6@5]6@:7:q5]6@?i@M;2;k;2;k;2;k;2;k7H8,:7:q6O72;2;k:7:q=u>Y:7:q:7:q:7:q7J8.656n7J8.;Z<>;2;k7H8,;2;k;2;k;Z<>");
            }
            return 0f;
        }
        /// <summary>
        /// Vrátí polovinu šířky daného znaku <paramref name="cCurr"/> pro styl písma <paramref name="isBold"/>.
        /// </summary>
        /// <param name="cCurr"></param>
        /// <param name="isBold"></param>
        /// <returns></returns>
        private static float _GetCharWidth1(char cCurr, bool isBold)
        {
            return _GetCharWidth(cCurr, isBold, @",O,O,a,}-z.A1D1a1D1a6v7=383U+9+V-Z-w-Z-w.U.r1l23,a,}-Z-w,a,},a,}1D1a1D1a1D1a1D1a1D1a1D1a1D1a1D1a1D1a1D1a,a,},a,}1l231l231l231D1a9*9G383U383U414O414O383U2=2Z5,5H414O,a,}0I0f383U1D1a5{6B414O5,5H383U5,5H414O383U2=2Z414O383U7o86383U383U2=2Z,a,},a,},a,}/s091>1Z1D1a1D1a0I0f1D1a1D1a,a,}1D1a1D1a+o,6+o,60I0f+o,65{6B1D1a1D1a1D1a1D1a-Z-w0I0f,a,}1D1a0I0f414O0I0f0I0f0I0f-\-y,G,c-\-y1l231D1a-Z-w1D1a1D1a1l23");
        }
        /// <summary>
        /// Vrátí float hodnotu z datového stringu <paramref name="data"/> z pozice odpovídající danému znaku <paramref name="cCurr"/> pro styl písma <paramref name="isBold"/>.
        /// Datový blok obsahuje sekvenci dat: dva znaky pro Regular, dva znaky pro Bold, postupně pro všechny znaky převedené na kódy = odpovídající pozici.
        /// </summary>
        /// <param name="cCurr"></param>
        /// <param name="isBold"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static float _GetCharWidth(char cCurr, bool isBold, string data)
        {
            int code = _GetCharCode(cCurr);                          // Pozice dat pro daný znak v datovém poli = index v jednoznačném soupisu podporovaných znaků '_TextCharList'. Není záporné.
            int index = 4 * code + (isBold ? 2 : 0);                 // Index ukazuje na počátek dat pro daný znak a Bold: data jsou sekvence 4 pozic pro jeden znak (cCurr); vždy 2 znaky Regular + 2 znaky Bold, 
            return ((86f * getVal(index)) + getVal(index + 1)) / 70f;//  v kódování Char(40) až Char(126) v pořadí HL (kde Char(126) již není přítomno), tedy 40 až 125 = 86 hodnot na 1 znak... 2 znaky = 0..7395; děleno 70f = 0 až 105,65, s krokem 0,014 px

            float getVal(int i)
            {
                int val = (int)data[i] - 40;                         // Pokud na dané pozici je znak '(' = Char(40), odpovídá to hodnotě 0
                return (float)val;
            }
        }
        /// <summary>
        /// Vrátí text dlouhý 4 znaky, reprezentující dané šířky Regular <paramref name="widthR"/> a Bold <paramref name="widthB"/>, tak aby byl korektně dekódován v metodě <see cref="_GetCharWidth(char, bool, string)"/>,
        /// </summary>
        /// <param name="widthR"></param>
        /// <param name="widthB"></param>
        /// <returns></returns>
        private static string _GetDataForWidths(float widthR, float widthB)
        {
            split(widthR, out char rh, out char rl);
            split(widthB, out char bh, out char bl);
            return rh.ToString() + rl.ToString() + bh.ToString() + bl.ToString();

            // Konverze Float na dva znaky:
            void split(double width, out char h, out char l)
            {
                if (width <= 0f) { h = '('; l = '('; return; }       // Znak  '('  reprezentuje 0

                double w = 70d * width;                              // Pro zadanou šířku width = 34.975 bude w = 2448.25
                int hi = (int)Math.Truncate(w / 86d);                // Pro vstup w = 2448.25 je h = (2448.25 / 86d = 28.468023   => Truncate  =  28
                int lo = (int)Math.Round((w % 86d), 0);              // Pro vstup w = 2448.25 je l = (2448.25 % 86d = 40.25       => Round     =  40
                h = (char)(hi + 40);                                 // Char(40) = hodnota 0
                l = (char)(lo + 40);
            }
        }
        /// <summary>
        /// Vrátí kód (=index) dodaného znaku, anebo kód náhradního (existujícího) znaku pro daný znak.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static int _GetCharCode(char c)
        {
            var list = _TextCharList;
            int code = list.IndexOf(c);
            if (code == -1) code = list.IndexOf('D');
            return code;
        }
        /// <summary>
        /// Obsahuje všechny řešené Basic znaky, na odpovídajících pozicích
        /// </summary>
        private const string _TextCharList = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_abcdefghijklmnopqrstuvwxyz{|}~§¨«»×";
        /// <summary>
        /// Velikost písma, pro kterou je systém naplněn
        /// </summary>
        private const float _FontEmSize = 20f;
        /// <summary>
        /// Výška textu v defaultním fontu
        /// </summary>
        private const float _FontHeight = 50.273f;
        /// <summary>
        /// Margins textu v defaultním fontu Regular
        /// </summary>
        private const float _FontMarginR = 13.333f;
        /// <summary>
        /// Margins textu v defaultním fontu Bold
        /// </summary>
        private const float _FontMarginB = 13.333f;
        #endregion
    }
    #endregion
}
