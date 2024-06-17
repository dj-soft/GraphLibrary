// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using DevExpress.XtraPdfViewer.Commands;
using DevExpress.XtraRichEdit.Layout;
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
                        {   // Pro panel budu počítat rozmístění jeho vnitřních prvků a následně i vnější rozměry panelu.
                            // Tím končí práce serveru (výpočtem layoutu uvnitř panelu), další provádí klient (rozmístění panelů do záložky / okna.
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
        internal static void ValidateControlInfo(IDataFormItem itemData)
        {
            _ValidateControlSize(itemData);
            _ValidateLabelSize(itemData);
        }
        /// <summary>
        /// Validuje (tzn. naplní prázdné hodnoty) velikost Controlu podle jeho typu, obsahu a fontu
        /// </summary>
        /// <param name="itemData"></param>
        private static void _ValidateControlSize(IDataFormItem itemData)
        {
            bool hasWidth = itemData.DesignWidthPixel.HasValue || itemData.DesignWidthPercent.HasValue || itemData.ImplicitControlMinimalWidth.HasValue || itemData.ImplicitControlOptimalWidth.HasValue;
            bool hasHeight = itemData.DesignHeightPixel.HasValue || itemData.DesignHeightPercent.HasValue || itemData.ImplicitControlMinimalHeight.HasValue || itemData.ImplicitControlOptimalHeight.HasValue;
            if (hasWidth && hasHeight) return;                                 // Rozměry máme, není co řešit.

            switch (itemData.ControlType)
            {
                // Poznámka k třetímu paramertu asOptimalDimension:
                //  prvek bude vždy vyžadovat přidělení zadaného prostoru (např. šířku sloupce nebo výšku řádku, a to i v případě ColSpan / RowSpan), rozdíl je:
                //   true =  Optimal  => pokud sloupec bude širší než je zde požadováno, pak prvek bude i tak mít zde zadanou šířku = optimální. Jako by v frm.xml bylo dáno Width="65".
                //   false = Minimal  => pokud sloupec bude širší než je zde požadováno, pak prvek se roztáhne na celou šířku prostoru. Jako by v frm.xml bylo dáno Width="100%".
                case ControlType.Label: setControlTextSize(12, 1, true); break;
                case ControlType.Button: setControlTextSize(18, 6, true); break;
                case ControlType.BarCode: setControlSize(32, 32, false); break;
                case ControlType.Image: setControlSize(32, 32, false); break;
                case ControlType.EditBox: setControlSize(250, 40, false); break;
                case ControlType.TextBox:
                    string name = GetItemKey(itemData.ColumnName, "");
                    if (name == "reference_subjektu" || name.EndsWith("_refer")) setControlSize(120, 20, true);
                    else if (name == "nazev_subjektu" || name.EndsWith("_nazev")) setControlSize(250, 20, true);
                    else setControlSize(100, 20, false);
                    break;
            }

            // Změří aktuální text itemData.ControlText (výška a šířka podle fontu itemData.ControlFont), 
            // přidá definovaný přídavek a výsledné rozměry vloží do ControlWidth a ControlHeight, jako (asOptimalDimension: true => Optimal / false = Minimal)
            void setControlTextSize(int addWidth, int addHeight, bool asOptimalDimension)
            {
                string text = itemData.ControlText;
                int width = TextDimension.GetTextWidth(text, itemData.ControlFont) + addWidth;
                int height = TextDimension.GetFontHeight(itemData.ControlFont) + addHeight;
                setControlSize(width, height, asOptimalDimension);
            }
            // Vloží dodané hodnoty do ControlWidth a ControlHeight, pokud tam nejsou, jako (asOptimalDimension: true => Optimal / false = Minimal)
            void setControlSize(int? width, int? height, bool asOptimalDimension)
            {
                if (!hasWidth)
                {
                    if (asOptimalDimension)
                        itemData.ImplicitControlOptimalWidth = width;
                    else
                        itemData.ImplicitControlMinimalWidth = width;
                }
                if (!hasHeight)
                {
                    if (asOptimalDimension)
                        itemData.ImplicitControlMinimalHeight = height;
                    else
                        itemData.ImplicitControlOptimalHeight = height;
                }
            }
        }
        /// <summary>
        /// Validuje (tzn. naplní prázdné hodnoty) velikost labelu Main a Suffix
        /// </summary>
        /// <param name="itemData"></param>
        private static void _ValidateLabelSize(IDataFormItem itemData)
        {
            if (itemData.LabelPosition != LabelPositionType.None && CheckTextSize(itemData.MainLabelText, itemData.MainLabelFont, itemData.ImplicitMainLabelWidth, itemData.ImplicitMainLabelHeight, out var mainWidth, out var mainHeight))
            {   // Main label existuje a je třeba aktualizovat některý rozměr:
                if (mainWidth.HasValue) itemData.ImplicitMainLabelWidth = mainWidth.Value;
                if (mainHeight.HasValue) itemData.ImplicitMainLabelHeight = mainHeight.Value;
            }
            if (CheckTextSize(itemData.SuffixLabelText, itemData.SuffixLabelFont, itemData.ImplicitSuffixLabelWidth, itemData.ImplicitSuffixLabelHeight, out var sufWidth, out var sufHeight))
            {   // Suffix label existuje a je třeba aktualizovat některý jeho rozměr:
                if (sufWidth.HasValue) itemData.ImplicitSuffixLabelWidth = sufWidth.Value;
                if (sufHeight.HasValue) itemData.ImplicitSuffixLabelHeight = sufHeight.Value;
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
            if (String.IsNullOrEmpty(text)) return false;            // Není zadán text

            bool hasWidth = inpWidth.HasValue;
            bool hasHeight = inpHeight.HasValue;
            if (hasWidth && hasHeight) return false;                 // Rozměry jsou plně určeny, není co řešit (i záporná čísla jsou akceptovaná)

            // Šířka:
            if (!hasWidth) outWidth = TextDimension.GetTextWidth(text, fontInfo);

            // Výška:
            if (!hasHeight) outHeight = TextDimension.GetFontHeight(fontInfo);

            return true;
        }
        /// <summary>
        /// Vrátí true, pokud dané souřadnice prvku (<see cref="DesignBounds"/>) určují pevnou = fixní pozici v layoutu (vrací true), nebo plovoucí (false).
        /// Aby pozice byla fixní, pak musí být souřadnice zadaná a musí mít dané hodnoty <see cref="DesignBounds.Left"/> a <see cref="DesignBounds.Top"/>.
        /// </summary>
        /// <param name="designBounds"></param>
        /// <returns></returns>
        internal static bool IsBoundsFixed(DesignBounds designBounds)
        {
            return (designBounds != null && designBounds.Left.HasValue && designBounds.Top.HasValue);
        }
        /// <summary>
        /// Vrátí true, pokud dané souřadnice prvku (<see cref="DesignBounds"/>) určují šířku prvku, tedy není třeba ji určovat podle typu a obsahu prvku.
        /// Tedy souřadnice je zadaná a má naplněnou <see cref="DesignBounds.Width"/>, ať už v pixelech, nebo v procentech.
        /// </summary>
        /// <param name="designBounds"></param>
        /// <returns></returns>
        internal static bool HasBoundsWidth(DesignBounds designBounds)
        {
            return (designBounds != null && designBounds.Width.HasValue);
        }
        /// <summary>
        /// Vrátí true, pokud dané souřadnice prvku (<see cref="DesignBounds"/>) určují výšku prvku, tedy není třeba ji určovat podle typu a obsahu prvku.
        /// Tedy souřadnice je zadaná a má naplněnou <see cref="DesignBounds.Height"/> (ta je zatím pouze v pixelech).
        /// </summary>
        /// <param name="designBounds"></param>
        /// <returns></returns>
        internal static bool HasBoundsHeight(DesignBounds designBounds)
        {
            return (designBounds != null && designBounds.Height.HasValue);
        }
        /// <summary>
        /// Vrátí klíč pro dané Name. Trim, Lower. Pro NULL vrací NULL.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="nullResult"></param>
        /// <returns></returns>
        internal static string GetItemKey(string name, string nullResult = null)
        {
            return (name != null ? name.Trim().ToLower() : nullResult);
        }
        #endregion
        #region class LayoutStyle : Styl vzhledu pro jeden container, podporuje dědičnost
        /// <summary>
        /// Styl vzhledu pro jeden container, podporuje dědičnost
        /// </summary>
        internal class StyleInfo
        {
            public StyleInfo()
            {
                this.CurrentAreaType = ContainerType.None;
                this.ColumnsCount = 0;
                this.ColumnWidths = null;
                this.FlowAreaBegin = null;
                this.AutoLabelPosition = LabelPositionType.None;
                this.Margins = null;
                this.ControlMargins = null;
                this.ColumnsDistance = 0;
                this.RowsDistance = 0;
                this.TopLabelOffsetX = 3;
                this.BottomLabelOffsetX = 3;
                this.LabelsRelativeToControl = true;

                this._Validate();
            }
            public StyleInfo(ContainerType areaType, int? columnsCount, string columnWidths, Location flowAreaBegin, Margins controlMargins, LabelPositionType autoLabelPosition, Margins margins, int columnsDistance, int rowsDistance, int topLabelOffsetX, int bottomLabelOffsetX, bool labelsRelativeToControl)
            {
                this.CurrentAreaType = areaType;
                this.ColumnsCount = columnsCount;
                this.ColumnWidths = columnWidths;
                this.FlowAreaBegin = flowAreaBegin;
                this.AutoLabelPosition = autoLabelPosition;
                this.Margins = margins;
                this.ControlMargins = controlMargins;
                this.ColumnsDistance = columnsDistance;
                this.RowsDistance = rowsDistance;
                this.TopLabelOffsetX = topLabelOffsetX;
                this.BottomLabelOffsetX = bottomLabelOffsetX;
                this.LabelsRelativeToControl = labelsRelativeToControl;

                this._Validate();
            }
            public StyleInfo(DfBaseArea dfArea)
            {
                this.CurrentAreaType = dfArea.AreaType;
                this.ColumnsCount = dfArea.ColumnsCount;
                this.ColumnWidths = dfArea.ColumnWidths;
                this.FlowAreaBegin = dfArea.FlowAreaBegin;
                this.AutoLabelPosition = dfArea.AutoLabelPosition ?? LabelPositionType.None;
                this.Margins = dfArea.Margins;
                this.ControlMargins = dfArea.ControlMargins;
                this.ColumnsDistance = dfArea.ColumnsDistance ?? 0;
                this.RowsDistance = dfArea.RowsDistance ?? 0;
                this.TopLabelOffsetX = dfArea.TopLabelOffsetX ?? 3;
                this.BottomLabelOffsetX = dfArea.BottomLabelOffsetX ?? 3;
                this.LabelsRelativeToControl = dfArea.LabelsRelativeToControl ?? true;

                this._Validate();
            }
            public StyleInfo(DfBaseArea dfArea, StyleInfo styleParent)
            {
                this.CurrentAreaType = dfArea.AreaType;

                // Sloupce (ColumnsCount a ColumnWidths):
                //  - Pokud aktuální 'dfArea' přináší nějaké informace v jedné (nebo v obou) proměnné ('ColumnWidths' nebo 'ColumnsCount'), pak se převezme z dfArea a nikoli z Parenta.
                //  - Nelze kombinovat jednu hodnotu z aktuálního 'dfArea' a druhou z 'styleParent'!
                //  - Protože uživatel typicky do containeru zadává jen jednu ze dvou hodnot, a tím definuje sloupce !!!
                if (dfArea != null && ((dfArea.ColumnsCount.HasValue && dfArea.ColumnsCount.Value > 0) || (!String.IsNullOrEmpty(dfArea.ColumnWidths))))
                {   // Obě hodnoty převezmu z aktuálního 'dfArea', protože ten existuje a alespoň jednu hodnotu deklaruje:
                    this.ColumnsCount = dfArea.ColumnsCount;
                    this.ColumnWidths = dfArea.ColumnWidths;
                }
                else if (styleParent != null)
                {   // Obě hodnoty z parenta, protože aktuální 'dfArea' nedeklaruje ani jednu hodnotu:
                    this.ColumnsCount = styleParent.ColumnsCount;
                    this.ColumnWidths = styleParent.ColumnWidths;
                }

                // Některé hodnoty se nedědí nikdy: pokud je nemá zadané přímo container, pak budou prázdné:
                this.FlowAreaBegin = dfArea.FlowAreaBegin;

                // Některé hodnoty se dědí jen pro určité druhy containerů, ale třeba do grupy se z vyšších stylů nedědí:
                bool isGroup = (this.CurrentAreaType == ContainerType.Group);
                this.Margins = dfArea.Margins != null ? dfArea.Margins : (isGroup ? null : styleParent.Margins);

                // Ostatní hodnoty jsou "jednomístné" a lze je dědit jednoduše:
                this.AutoLabelPosition = dfArea.AutoLabelPosition.HasValue ? dfArea.AutoLabelPosition.Value : styleParent.AutoLabelPosition;
                this.ControlMargins = dfArea.ControlMargins ?? styleParent.ControlMargins;
                this.ColumnsDistance = dfArea.ColumnsDistance.HasValue ? dfArea.ColumnsDistance.Value : styleParent.ColumnsDistance;
                this.RowsDistance = dfArea.RowsDistance.HasValue? dfArea.RowsDistance.Value : styleParent.RowsDistance;
                this.TopLabelOffsetX = dfArea.TopLabelOffsetX.HasValue ? dfArea.TopLabelOffsetX.Value : styleParent.TopLabelOffsetX;
                this.BottomLabelOffsetX = dfArea.BottomLabelOffsetX.HasValue ? dfArea.BottomLabelOffsetX.Value : styleParent.BottomLabelOffsetX;
                this.LabelsRelativeToControl = dfArea.LabelsRelativeToControl.HasValue ? dfArea.LabelsRelativeToControl.Value : styleParent.LabelsRelativeToControl;

                this._Validate();
            }
            /// <summary>
            /// Validace hodnot
            /// </summary>
            private void _Validate()
            {
                if (Margins is null) Margins = Margins.Empty;
                if (ControlMargins is null) ControlMargins = Margins.Empty;
            }
            /// <summary>
            /// Druh containeru načtený v this instanci, pochází z dodaného <see cref="DfBaseArea.AreaType"/>.
            /// </summary>
            public ContainerType CurrentAreaType { get; private set; }
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
            /// Souřadnice počátku Flow prostoru
            /// </summary>
            public Location FlowAreaBegin { get; private set; }
            /// <summary>
            /// Automaticky generovat labely atributů a vztahů, jejich umístění. Defaultní = <c>NULL</c>
            /// </summary>
            public LabelPositionType AutoLabelPosition { get; private set; }
            /// <summary>
            /// Volné okraje mezi hranou kontejneru a začátkem Child prvků = prázdný prostor
            /// </summary>
            public Margins Margins { get; set; }
            /// <summary>
            /// Odstupy mezi MainLabelem a Controlem, nebo SuffixLabelem a Controlem, v rámci jedné buňky (jeden prvek). Uplatní se, pouze pokud daný label existuje.
            /// </summary>
            public Margins ControlMargins { get; private set; }
            /// <summary>
            /// Volný prostor mezi dvěma sousedními sloupci (vnitřní Margin)
            /// </summary>
            public int ColumnsDistance { get; private set; }
            /// <summary>
            /// Volný prostor mezi dvěma sousedními řádky (vnitřní Margin)
            /// </summary>
            public int RowsDistance { get; private set; }
            /// <summary>
            /// Posunutí Main Labelu umístěného Top, na ose X, oproti souřadnici X vlastního Controlu.
            /// Kladná hodnota posune doprava.
            /// Může být záporné, pak bude label předsazen vlevo před Controlem.
            /// </summary>
            public int TopLabelOffsetX { get; private set; }
            /// <summary>
            /// Posunutí Main Labelu umístěného Bottom, na ose X, oproti souřadnici X vlastního Controlu.
            /// Kladná hodnota posune doprava.
            /// Může být záporné, pak bude label předsazen vlevo před Controlem.
            /// </summary>
            public int BottomLabelOffsetX { get; private set; }
            /// <summary>
            /// Labely budou umísťovány : true = pokud možno k controlu / false = vždy do gridu
            /// </summary>
            public bool LabelsRelativeToControl { get; private set; }
        }
        #endregion
        #region class ItemInfo : Dočasná pracovní a výkonná schránka na jednotlivý prvek layoutu, existuje jen po dobu výpočtu layoutu. Má tři tváře: Prvek/Container; Spolupráce s aplikací na doplnění hodnot; Spolupráce s FlowLayout na umístění do mřížky
        /// <summary>
        /// Dočasná pracovní a výkonná schránka na jednotlivý prvek layoutu (panel, grupa, control), v procesu určování layoutu prvků v rámci panelu.
        /// <para/>
        /// Uvnitř panelu jsou prvky rozmístěny fixně = jsou dané designerem formuláře. 
        /// Ale rozmístění sousedních panelů na DataFormu je více v rukou uživatele / pohledu / velikosti monitoru atd.
        /// </summary>
        internal class ItemInfo : IDataFormItem, IFlowLayoutItem, IDisposable
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
                                StyleInfo childStyle = new StyleInfo(dfChildContainer, this.__ChildStyle);
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
                __ChildStyle = style;
                __Childs = null;

                _InitData();
            }
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                __Childs?.ForEach(i => i?.Dispose());
                
                _ResetData();
                
                __DfItem = null;
                __Parent = null;
                __DfArgs = null;
                __ChildStyle = null;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this._Text;
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
            private StyleInfo __ChildStyle;
            /// <summary>
            /// Prvky mých Childs. Výchozí stav je null (většina prvků jsou Controly, které nemají Childs). Lze testovat <see cref="_HasChilds"/>.
            /// </summary>
            private List<ItemInfo> __Childs;
            /// <summary>
            /// Root prvek. Zde nikdy není null: pokud this je Root, pak zde je this.
            /// </summary>
            private ItemInfo _Root { get { return (__Parent?._Root ?? this); } }
            /// <summary>
            /// Styl pro Child prvky tohoto containeru. Pokud this je container, pak má definován svůj vlastní styl (<see cref="__ChildStyle"/>).
            /// Pokud jej nemá (typicky proto, že jde o Control), pak zde vrací aktuální styl ze svého Parenta.
            /// </summary>
            private StyleInfo _CurrentStyle { get { return __ChildStyle ?? __Parent?._CurrentStyle; } }
            /// <summary>
            /// Styl pocházející z parenta. pro tento prvek. Pokud this je container, pak má definován svůj vlastní styl (<see cref="__ChildStyle"/>).
            /// Pokud jej nemá (typicky proto, že jde o Control), pak zde vrací aktuální styl ze svého Parenta.
            /// </summary>
            private StyleInfo _ParentStyle { get { return __Parent?._CurrentStyle; } }
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
            /// Plná cesta a informace od Root přes jeho Child až ke mě = typy prvků oddělené šipkou.
            /// </summary>
            private string _Text
            {
                get
                {
                    var type = __DfItem.GetType().Name;
                    var name = __DfItem.Name;
                    var text = type + (!String.IsNullOrEmpty(name) ? $" '{name}'" : "");
                    if (this.__LayoutMode == LayoutModeType.Flow)
                    {
                        text += $"; Flow [ RowIndex: {__FlowRowBeginIndex}";
                        if (__RowSpan.HasValue && __RowSpan.Value > 1)
                            text += $"; RowSpan: {__RowSpan}";
                        text += $"; ColIndex: {__FlowColBeginIndex}";
                        if (__ColSpan.HasValue && __ColSpan.Value > 1)
                            text += $"; ColSpan: {__ColSpan}";
                        text += $" ]";
                    }

                    var parent = __Parent;
                    return (parent != null ? parent._Text + " => " : "") + text;
                }
            }
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
            /// Prvek reprezentuje grupu?
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
            /// <summary>
            /// Podkladový control
            /// </summary>
            private DfBaseControl _DfControl { get { return (__DfItem as DfBaseControl); } }
            /// <summary>
            /// Stav prvku
            /// </summary>
            private ControlStateType? _ControlState { get { return __DfItem.State; } }
            /// <summary>
            /// Stav prvku obsahuje příznak <see cref="ControlStateType.Absent"/>
            /// </summary>
            private bool _IsAbsent { get { var state = __DfItem.State; return state.HasValue && state.Value.HasFlag(ControlStateType.Absent); } }
            /// <summary>
            /// Všechny Child prvky (controly + grupy).
            /// Všechny je třeba umístit
            /// </summary>
            private ItemInfo[] _Childs { get { return __Childs?.ToArray(); } }
            /// <summary>
            /// Obsahuje true, pokud máme nějaké <see cref="_Childs"/> (tj. pole <see cref="__Childs"/> není null a obsahuje alespoň jeden prvek).
            /// </summary>
            private bool _HasChilds { get { return (this.__Childs != null && this.__Childs.Count > 0); } }
            /// <summary>
            /// Obsahuje všechny Child prvky = linearizované pole Child prvků, kde je vložen i Container, a po něm i všechny jeho Child prvky, rekurzivně.
            /// </summary>
            private ItemInfo[] _AllChildItems
            {
                get
                {
                    List<ItemInfo> allChilds = new List<ItemInfo>();
                    _AddAllChilds(this, allChilds, true);
                    return allChilds.ToArray();
                }
            }
            /// <summary>
            /// Obsahuje všechny Child controly = linearizované pole Child prvků, kde namísto Child containeru jsou vloženy Controly v něm obsažené.
            /// </summary>
            private ItemInfo[] _AllChildControls
            { 
                get
                {
                    List<ItemInfo> allChilds = new List<ItemInfo>();
                    _AddAllChilds(this, allChilds, false);
                    return allChilds.ToArray();
                }
            }
            /// <summary>
            /// Za prvek <paramref name="container"/> (který by měl být typu 'kontejner') přidá jeho Child prvky typu Control do vznikajícího pole <paramref name="allChildsTarget"/>.
            /// Pokud child prvek je typu Container, pak jej nepřidává, ale vyvolá rekurzivně tuto metodu pro něj = přidají se tak na místo containeru jeho jednotlivé Child prvky.
            /// </summary>
            /// <param name="container"></param>
            /// <param name="allChildsTarget"></param>
            /// <param name="addContainersItem"></param>
            private static void _AddAllChilds(ItemInfo container, List<ItemInfo> allChildsTarget, bool addContainersItem)
            {
                var childs = container.__Childs;
                if (childs != null)
                {
                    foreach (var child in childs)
                    {
                        if (child != null)
                        {
                            if (child._IsControl)
                                allChildsTarget.Add(child);
                            else if (child._IsContainer && addContainersItem)
                                allChildsTarget.Add(child);

                            if (child._HasChilds)
                                _AddAllChilds(child, allChildsTarget, addContainersItem);
                        }
                    }
                }
            }
            #endregion
            #region Další data o prvku - primárně pro interface IDataFormItem
            /// <summary>
            /// Je voláno na konci konstruktoru, když jsou uloženy základní proměnné <see cref="__DfItem"/>, <see cref="__Parent"/>, <see cref="__DfArgs"/>, <see cref="__ChildStyle"/>. Aktuálně neřešíme Childs, <see cref="__Childs"/> je null a dosud nebyly enumerovány.<br/>
            /// Připraví si trvalá data: vyhodnotí svůj zdroj (grupa / control) a opíše si z něj jeho specifické hodnoty do zdejších proměnných. Ani zde se neřeší Childs, pouze aktuální prvek <see cref="__DfItem"/> v jeho konkrétní formě.
            /// </summary>
            private void _InitData()
            {
                var dfItem = __DfItem;
                string name = dfItem.Name;
                __Name = name;

                string columnName = name;
                if (dfItem is DfBaseInputControl inputControl)
                {
                    if (!String.IsNullOrEmpty(inputControl.ColumnName)) columnName = inputControl.ColumnName;
                }
                __ColumnName = columnName;

                if (dfItem is DfPanel dfPanel)
                    _InitDataPanel(dfPanel);
                else if (dfItem is DfGroup dfGroup)
                    _InitDataGroup(dfGroup);
                else if (dfItem is DfBaseControl dfControl)
                    _InitDataControl(dfControl);

                _InitFlow();
            }
            /// <summary>
            /// Inicializace vlastních dat tohoto prvku pro prvek typu Panel.
            /// Neřešíme Childs!
            /// </summary>
            /// <param name="dfPanel"></param>
            private void _InitDataPanel(DfPanel dfPanel)
            {
                this.__DesignBounds = dfPanel.DesignBounds;
                this.__ParentBoundsName = null;
                this.__ColIndex = null;
                this.__ColSpan = null;
                this.__RowSpan = null;
                this.__HPosition = null;
                this.__VPosition = null;
                this.__ExpandControl = null;
                this.__LabelPosition = null;
                this.__ContainerType = ContainerType.Panel;
                this.__ControlType = ControlType.None;
                this.__ControlExists = true;
            }
            /// <summary>
            /// Inicializace vlastních dat tohoto prvku pro prvek typu Group.
            /// Neřešíme Childs!
            /// </summary>
            /// <param name="dfGroup"></param>
            private void _InitDataGroup(DfGroup dfGroup)
            {
                this.__DesignBounds = dfGroup.DesignBounds;
                this.__ParentBoundsName = dfGroup.ParentBoundsName;
                this.__ColIndex = dfGroup.ColIndex;
                this.__ColSpan = dfGroup.ColSpan;
                this.__RowSpan = dfGroup.RowSpan;
                this.__HPosition = dfGroup.HPosition;
                this.__VPosition = dfGroup.VPosition;
                this.__ExpandControl = dfGroup.ExpandControl;
                this.__LabelPosition = dfGroup.LabelPosition;
                this.__MainLabelText = dfGroup.Label;
                this.__MainLabelWidth = dfGroup.LabelWidth;
                this.__MainLabelStyle = dfGroup.LabelStyle;
                this.__ContainerType = ContainerType.Group;
                this.__ControlType = ControlType.None;
                this.__ControlExists = true;

                _RefreshData();
            }
            /// <summary>
            /// Inicializace vlastních dat tohoto prvku pro prvek typu Control.
            /// </summary>
            /// <param name="dfControl"></param>
            private void _InitDataControl(DfBaseControl dfControl)
            {
                this.__DesignBounds = dfControl.DesignBounds;
                this.__ParentBoundsName = dfControl.ParentBoundsName;
                this.__ColIndex = dfControl.ColIndex;
                this.__ColSpan = dfControl.ColSpan;
                this.__RowSpan = dfControl.RowSpan;
                this.__HPosition = dfControl.HPosition;
                this.__VPosition = dfControl.VPosition;
                this.__ExpandControl = dfControl.ExpandControl;
                this.__LabelPosition = LabelPositionType.None;
                this.__ContainerType = ContainerType.None;
                this.__ControlType = dfControl.ControlType;
                this.__ControlStyle = dfControl.ControlStyle;
                this.__ToolTipTitle = dfControl.ToolTipTitle;
                this.__ToolTipText = dfControl.ToolTipText;

                // Specifické informace podle druhu controlu::
                if (dfControl is DfBaseLabeledInputControl labeledInputControl)
                {   // Label vedle Controlu:
                    this.__LabelPosition = labeledInputControl.LabelPosition;
                    this.__MainLabelText = labeledInputControl.Label;
                    this.__MainLabelWidth = labeledInputControl.LabelWidth;
                    this.__MainLabelStyle = labeledInputControl.LabelStyle;
                    this.__SuffixLabelText = labeledInputControl.SuffixLabel;
                }

                if (dfControl is DfBaseInputTextControl inputTextControl)
                {   // Text uvnitř Controlu:
                    this.__ControlText = inputTextControl.Text;
                    this.__ControlIconName = inputTextControl.IconName;
                }

                _RefreshData();
            }
            /// <summary>
            /// Refreshuje hodnoty <see cref="__MainLabelExists"/>, <see cref="__ControlExists"/>, <see cref="__SuffixLabelExists"/>
            /// </summary>
            private void _RefreshData()
            {
                LabelPositionType lPos = __LabelPosition ?? _ParentStyle.AutoLabelPosition;
                this.__ValidLabelPosition = lPos;
                this.__MainLabelExists = (!String.IsNullOrEmpty(__MainLabelText) && (lPos == LabelPositionType.BeforeLeft || lPos == LabelPositionType.BeforeRight || lPos == LabelPositionType.Top || lPos == LabelPositionType.Bottom || lPos == LabelPositionType.BeforeRight));
                this.__ControlExists = (this.__ContainerType == ContainerType.Panel || this.__ContainerType == ContainerType.Group) || !(this.__ControlType == ControlType.None || this.__ControlType == ControlType.PlaceHolder);
                this.__SuffixLabelExists = (!String.IsNullOrEmpty(__SuffixLabelText) && __LabelPosition != LabelPositionType.After);
            }
            /// <summary>
            /// Zahodí veškerá data
            /// </summary>
            private void _ResetData()
            {
                __Name = null;
                __ColumnName = null;
                __DesignBounds = null;
                __ColIndex = null;
                __ColSpan = null;
                __RowSpan = null;
                __HPosition = null;
                __ExpandControl = null;
                __LabelPosition = null;
                __ControlType = ControlType.None;
                __ControlStyle = null;
                __ToolTipTitle = null;
                __ToolTipText = null;
                __MainLabelText = null;
                __MainLabelWidth = null;
                __MainLabelStyle = null;
                __ControlText = null;
                __ControlIconName = null;

                _ResetFlowData();
            }
            /// <summary>
            /// Jméno prvku
            /// </summary>
            private string __Name;
            /// <summary>
            /// Klíč prvků: Trim, Lower.  Pochází z <see cref="__Name"/>. Pokud tam je null, je i <see cref="_Key"/> = null.
            /// </summary>
            private string _Key { get { return GetItemKey(__Name); } }
            /// <summary>
            /// Jméno sloupce (nebo jméno prvku)
            /// </summary>
            private string __ColumnName;
            /// <summary>
            /// Umístění prvku. Výchozí je null.
            /// </summary>
            private DesignBounds __DesignBounds;
            /// <summary>
            /// Jméno prvku 'Name', který určuje souřadnice pro zdejší prvek, pokud ten je umístěn fixně = má definované souřadnice 'X' a 'Y'.
            /// </summary>
            private string __ParentBoundsName;
            /// <summary>
            /// Klíč prvku ParentBoundsName: Trim, Lower.  Pochází z <see cref="__ParentBoundsName"/>. Pokud tam je null, je i <see cref="_Key"/> = null.
            /// </summary>
            private string _ParentBoundsKey { get { return GetItemKey(__ParentBoundsName); } }
            /// <summary>
            /// Index sloupce, na kterém je prvek umístěn v režimu FlowLayout. Ten se použije, pokud prvky nemají exaktně dané souřadnice, spolu s atributem 'ColumnWidths'.
            /// </summary>
            private int? __ColIndex;
            /// <summary>
            /// Počet sloupců, které prvek obsazuje v FlowLayoutu. Ten se použije, pokud prvky nemají exaktně dané souřadnice, spolu s atributem 'ColumnWidths'.
            /// </summary>
            private int? __ColSpan;
            /// <summary>
            /// Počet řádků, které prvek obsazuje v FlowLayoutu. Ten se použije, pokud prvky nemají exaktně dané souřadnice.
            /// </summary>
            private int? __RowSpan;
            /// <summary>
            /// Umístění prvku vodorovně v rámci sloupce v případě, kdy šířka prvku je menší než šířka sloupce. Řeší tedy zarovnání controlu ve sloupci, nikoli zarovnání obsahu (textu) v rámci controlu.
            /// </summary>
            private HPositionType? __HPosition;
            /// <summary>
            /// Umístění prvku svisle v rámci řádku v případě, kdy výška prvku je menší než výška řádku. Řeší tedy zarovnání controlu v řádku, nikoli zarovnání obsahu (textu) v rámci controlu.
            /// </summary>
            private VPositionType? __VPosition;
            /// <summary>
            /// Rozšíření controlu do okolního prostoru pro labely, pokud labely nejsou použity
            /// </summary>
            private ExpandControlType? __ExpandControl;
            /// <summary>
            /// Umístění a zarovnání popisku (Main Labelu) vzhledem k souřadnicích controlu, zadané k prvku (nebo null)
            /// </summary>
            private LabelPositionType? __LabelPosition;
            /// <summary>
            /// Umístění a zarovnání popisku (Main Labelu) vzhledem k souřadnicích controlu, validované (není null)
            /// </summary>
            private LabelPositionType __ValidLabelPosition;
            /// <summary>
            /// Typ containeru
            /// </summary>
            private ContainerType __ContainerType;
            /// <summary>
            /// Typ prvku
            /// </summary>
            private ControlType __ControlType;
            /// <summary>
            /// Styl controlu (název, styl písma, velikost, barva popisku, barva textu a pozadí, atd)
            /// </summary>
            private DfControlStyle __ControlStyle;
            /// <summary>
            /// Titulek ToolTipu.
            /// </summary>
            private string __ToolTipTitle;
            /// <summary>
            /// Text ToolTipu.
            /// </summary>
            private string __ToolTipText;
            /// <summary>
            /// Existuje MainLabel (tzn. je definován text a je určená pozice)
            /// </summary>
            private bool __MainLabelExists;
            /// <summary>
            /// Text, popisující obsah políčka.
            /// </summary>
            private string __MainLabelText;
            /// <summary>
            /// Existuje Control (tzn. nějaký textbox nebo picture nebo button)? Nebo ne (třeba Placeholder)
            /// </summary>
            private bool __ControlExists;
            /// <summary>
            /// Existuje SuffixLabel (tzn. je definován text)
            /// </summary>
            private bool __SuffixLabelExists;
            /// <summary>
            /// Text suffix labelu. Jde o popisek vpravo od vstpního prvku, typicky obsahuje název jednotky (ks, Kč, $, kg, ...).
            /// Pokud je null, pak není ve formuláři definováno. Pokud je "", je tím definováno 'Bez labelu'.
            /// V existující definici mohou být přítomny formátovací funkce: "fm(xxx)", "fmr(xxx)". Přípravná funkce to má vyřešit.
            /// </summary>
            private string __SuffixLabelText;
            /// <summary>
            /// Nejvyšší šířka prostoru pro Label
            /// </summary>
            private int? __MainLabelWidth { get; set; }
            /// <summary>
            /// Styl pro Main label (název, styl písma, velikost, barva popisku, barva textu a pozadí, atd)
            /// </summary>
            private DfControlStyle __MainLabelStyle;
            /// <summary>
            /// Text popisku uvnitř controlu = text v Buttonu, text v CheckBoxu
            /// </summary>
            private string __ControlText;
            /// <summary>
            /// Jméno ikony odstavce nebo prvku (v titulku stránky, v titulku odstavce, ikona Buttonu, atd).
            /// Použití se liší podle typu prvku.
            /// </summary>
            private string __ControlIconName;
            /// <summary>
            /// Šířka pro Main label určená v kódu
            /// </summary>
            private int? __ImplicitMainLabelWidth;
            /// <summary>
            /// Výška pro Main label určená v kódu
            /// </summary>
            private int? __ImplicitMainLabelHeight;
            /// <summary>
            /// Výchozí minimální šířka vlastního controlu v pixelech, lze setovat.
            /// Pokud sloupec nebude mít žádnou šířku, a bude v něm tento prvek, a ten bude mít zde nastavenu určitou MinWidth, pak jeho sloupec ji bude mít nastavenu jako Implicitní.
            /// Nicméně pokud sloupec bude mít šířku větší, a prvek bude mít jen tuto MinWidth, pak prvek bude ve výsledku dimenzován na 100% reálné šířky sloupce, klidně větší než zdejší MinWidth.
            /// </summary>
            private int? __ImplicitControlMinimalWidth;
            /// <summary>
            /// Výchozí optimální šířka vlastního controlu v pixelech, lze setovat.
            /// Pokud sloupec bude mít výslednou šířku větší než tato OptimalWidth, pak prvek bude ve výsledku dimenzován na tuto OptimalWidth, jako by ji zadal uživatel do Width.
            /// </summary>
            private int? __ImplicitControlOptimalWidth;
            /// <summary>
            /// Výška pro Control určená v kódu
            /// </summary>
            private int? __ImplicitControlMinimalHeight;
            /// <summary>
            /// Výchozí optimální výška vlastního controlu v pixelech, lze setovat.
            /// Pokud řádek bude mít výslednou výšku větší než tato OptimalHeight, pak prvek bude ve výsledku dimenzován na tuto OptimalHeight, jako by ji zadal uživatel do Height.
            /// </summary>
            private int? __ImplicitControlOptimalHeight;
            /// <summary>
            /// Šířka pro Suffix label určená v kódu
            /// </summary>
            private int? __ImplicitSuffixLabelWidth;
            /// <summary>
            /// Výška pro Suffix label určená v kódu
            /// </summary>
            private int? __ImplicitSuffixLabelHeight;

            #region IDataFormItem : implementace
            DfForm IDataFormItem.DataForm { get { return _DfForm; } }
            DfBaseControl IDataFormItem.BaseControl { get { return _DfControl; } }
            string IDataFormItem.Name { get { return __Name; } }
            string IDataFormItem.ColumnName { get { return __ColumnName; } }
            ControlType IDataFormItem.ControlType { get { return __ControlType; } }
            DfFontInfo IDataFormItem.ControlFont { get { return null; } }
            string IDataFormItem.ControlText { get { return __ControlText; } }
            DfControlStyle IDataFormItem.ControlStyle { get { return __ControlStyle; } }
            DfFontInfo IDataFormItem.MainLabelFont { get { return null; } }
            string IDataFormItem.MainLabelText { get { return __MainLabelText; } set { __MainLabelText = value; } }
            int? IDataFormItem.MainLabelWidth { get { return __MainLabelWidth; } }
            DfFontInfo IDataFormItem.SuffixLabelFont { get { return null; } }
            string IDataFormItem.SuffixLabelText { get { return __SuffixLabelText; } set { __SuffixLabelText = value; } }
            string IDataFormItem.ToolTipTitle { get { return __ToolTipTitle; } set { __ToolTipTitle = value; } }
            string IDataFormItem.ToolTipText { get { return __ToolTipText; } set { __ToolTipText = value; } }

            int? IDataFormItem.DesignWidthPixel { get { return __DesignBounds?.Width?.NumberPixel; } }
            int? IDataFormItem.DesignWidthPercent { get { return __DesignBounds?.Width?.NumberPercent; } }
            int? IDataFormItem.DesignHeightPixel { get { return __DesignBounds?.Height?.NumberPixel; } }
            int? IDataFormItem.DesignHeightPercent { get { return __DesignBounds?.Height?.NumberPercent; } }
            int? IDataFormItem.DesignLabelWidth { get { return __MainLabelWidth; } }
            LabelPositionType IDataFormItem.LabelPosition { get { return __ValidLabelPosition; } }
            bool IDataFormItem.MainLabelExists { get { return __MainLabelExists ; } }
            bool IDataFormItem.ControlExists { get { return __ControlExists; } }
            bool IDataFormItem.SuffixLabelExists { get { return __SuffixLabelExists; } }

            // Implicitní, dopočtené pro prvek z jeho typu, textu atd:
            int? IDataFormItem.ImplicitMainLabelWidth { get { return __ImplicitMainLabelWidth; } set { __ImplicitMainLabelWidth = value; } }
            int? IDataFormItem.ImplicitMainLabelHeight { get { return __ImplicitMainLabelHeight; } set { __ImplicitMainLabelHeight = value; } }
            int? IDataFormItem.ImplicitControlMinimalWidth { get { return __ImplicitControlMinimalWidth; } set { __ImplicitControlMinimalWidth = value; } }
            int? IDataFormItem.ImplicitControlOptimalWidth { get { return __ImplicitControlOptimalWidth; } set { __ImplicitControlOptimalWidth = value; } }
            int? IDataFormItem.ImplicitControlMinimalHeight { get { return __ImplicitControlMinimalHeight; } set { __ImplicitControlMinimalHeight = value; } }
            int? IDataFormItem.ImplicitControlOptimalHeight { get { return __ImplicitControlOptimalHeight; } set { __ImplicitControlOptimalHeight = value; } }
            int? IDataFormItem.ImplicitSuffixLabelWidth { get { return __ImplicitSuffixLabelWidth; } set { __ImplicitSuffixLabelWidth = value; } }
            int? IDataFormItem.ImplicitSuffixLabelHeight { get { return __ImplicitSuffixLabelHeight; } set { __ImplicitSuffixLabelHeight = value; } }
            #endregion
            #endregion
            #region Další data o prvku - primárně pro IFlowLayoutItem
            /// <summary>
            /// Inicializuje data pro Flow layout
            /// </summary>
            private void _InitFlow()
            {
                bool isBoundsFixed = DfTemplateLayout.IsBoundsFixed(__DesignBounds);
                bool isFixedToParent = !String.IsNullOrEmpty(__ParentBoundsName);
                __LayoutMode = (isBoundsFixed && !isFixedToParent ? LayoutModeType.FixedAbsolute :
                               (isBoundsFixed && isFixedToParent ? LayoutModeType.BoundsInParent :
                               (!isBoundsFixed && isFixedToParent ? LayoutModeType.FlowInParent :
                               LayoutModeType.Flow)));
            }
            /// <summary>
            /// Resetuje veškerá provozní a výsledná data pro Flow layout. Volá se před zahájením prací na FlowLayoutu.
            /// </summary>
            private void _ResetFlowData()
            {
                __FlowRowBeginIndex = null;
                __FlowRowEndIndex = null;
                __FlowColBeginIndex = null;
                __FlowColEndIndex = null;
                _ResetFlowFinalResults();
            }
            /// <summary>
            /// Resetuje výsledné hodnoty FlowLayoutu. Volá se na začátku finalizace layoutu jako první metoda.
            /// Neresetuje designové hodnoty. Neresetuje umístění do Matrixu. Resetuje to co souvisí s pixely.
            /// </summary>
            private void _ResetFlowFinalResults()
            {
                __AcceptedWidth = false;
                __AcceptedHeight = false;
                __ControlBounds = null;
                __MainLabelBounds = null;
                __SuffixLabelBounds = null;
                __CellMatrix = null;
            }
            /// <summary>
            /// Režim layoutu
            /// </summary>
            private LayoutModeType __LayoutMode;
            /// <summary>
            /// Index sloupce, na kterém reálně začíná v režimu FlowLayout.
            /// </summary>
            private int? __FlowColBeginIndex;
            /// <summary>
            /// Index sloupce, na kterém reálně končí v režimu FlowLayout.
            /// </summary>
            private int? __FlowColEndIndex;
            /// <summary>
            /// Index řádku, na kterém reálně začíná prvek v režimu FlowLayout.
            /// </summary>
            private int? __FlowRowBeginIndex;
            /// <summary>
            /// Index řádku, na kterém reálně končí prvek v režimu FlowLayout.
            /// </summary>
            private int? __FlowRowEndIndex;
            /// <summary>
            /// Výsledná souřadnice celé buňky
            /// </summary>
            private CellMatrixInfo __CellMatrix;
            /// <summary>
            /// Výsledná souřadnice MainLabel v rámci parenta
            /// </summary>
            private ControlBounds __MainLabelBounds;
            /// <summary>
            /// Zarovnání textu MainLabel v prostoru <see cref="__MainLabelBounds"/>
            /// </summary>
            private ContentAlignmentType? __MainLabelAlignment;
            /// <summary>
            /// Výsledná souřadnice Controlu v rámci parenta
            /// </summary>
            private ControlBounds __ControlBounds;
            /// <summary>
            /// Výsledná souřadnice SuffixLabel v rámci parenta
            /// </summary>
            private ControlBounds __SuffixLabelBounds;
            /// <summary>
            /// Zarovnání textu SuffixLabel v prostoru <see cref="__SuffixLabelBounds"/>
            /// </summary>
            private ContentAlignmentType? __SuffixLabelAlignment;
            /// <summary>
            /// Algoritmus FlowLayout pro prvek zajistil potřebnou šířku
            /// </summary>
            private bool __AcceptedWidth;
            /// <summary>
            /// Algoritmus FlowLayout pro prvek zajistil potřebnou výšku
            /// </summary>
            private bool __AcceptedHeight;

            #region IFlowLayoutItem : implementace
            // Designové, čerpané z frm.xml:
            string IFlowLayoutItem.Name { get { return __Name; } }
            bool IFlowLayoutItem.IsAbsent { get { return _IsAbsent; } }
            bool IFlowLayoutItem.IsFlowMode { get { return (__LayoutMode == LayoutModeType.Flow); } }
            string IFlowLayoutItem.Text { get { return __Name; } }
            int? IFlowLayoutItem.DesignColIndex { get { return __ColIndex; } }
            int? IFlowLayoutItem.DesignColSpan { get { return __ColSpan; } }
            int? IFlowLayoutItem.DesignRowSpan { get { return __RowSpan; } }
            HPositionType? IFlowLayoutItem.DesignHPosition { get { return __HPosition; } }
            VPositionType? IFlowLayoutItem.DesignVPosition { get { return __VPosition; } }
            ExpandControlType? IFlowLayoutItem.DesignExpandControl { get { return __ExpandControl; } }
            int? IFlowLayoutItem.DesignWidthPixel { get { return __DesignBounds?.Width?.NumberPixel; } }
            int? IFlowLayoutItem.DesignWidthPercent { get { return __DesignBounds?.Width?.NumberPercent; } }
            int? IFlowLayoutItem.DesignHeightPixel { get { return __DesignBounds?.Height?.NumberPixel; } }
            int? IFlowLayoutItem.DesignHeightPercent { get { return __DesignBounds?.Height?.NumberPercent; } }
            int? IFlowLayoutItem.DesignLabelWidth { get { return __MainLabelWidth; } }
            LabelPositionType IFlowLayoutItem.LabelPosition { get { return __ValidLabelPosition; } }
            bool IFlowLayoutItem.MainLabelExists { get { return __MainLabelExists; } }
            bool IFlowLayoutItem.ControlExists { get { return __ControlExists; } }
            bool IFlowLayoutItem.SuffixLabelExists { get { return __SuffixLabelExists; } }

            // Implicitní, dopočtené pro prvek z jeho typu, textu atd:
            int? IFlowLayoutItem.ImplicitMainLabelWidth { get { return __ImplicitMainLabelWidth; } }
            int? IFlowLayoutItem.ImplicitMainLabelHeight { get { return __ImplicitMainLabelHeight; } }
            int? IFlowLayoutItem.ImplicitControlMinimalWidth { get { return __ImplicitControlMinimalWidth; } }
            int? IFlowLayoutItem.ImplicitControlOptimalWidth { get { return __ImplicitControlOptimalWidth; } }
            int? IFlowLayoutItem.ImplicitControlMinimalHeight { get { return __ImplicitControlMinimalHeight; } }
            int? IFlowLayoutItem.ImplicitControlOptimalHeight { get { return __ImplicitControlOptimalHeight; } }
            int? IFlowLayoutItem.ImplicitSuffixLabelWidth { get { return __ImplicitSuffixLabelWidth; } }
            int? IFlowLayoutItem.ImplicitSuffixLabelHeight { get { return __ImplicitSuffixLabelHeight; } }

            // Umístění prvku v rámci FlowLayoutu
            int? IFlowLayoutItem.FlowColBeginIndex { get { return __FlowColBeginIndex; } set { __FlowColBeginIndex = value; } }
            int? IFlowLayoutItem.FlowColEndIndex { get { return __FlowColEndIndex; } set { __FlowColEndIndex = value; } }
            int? IFlowLayoutItem.FlowRowBeginIndex { get { return __FlowRowBeginIndex; } set { __FlowRowBeginIndex = value; } }
            int? IFlowLayoutItem.FlowRowEndIndex { get { return __FlowRowEndIndex; } set { __FlowRowEndIndex = value; } }
            CellMatrixInfo IFlowLayoutItem.CellMatrix { get { return __CellMatrix; } set { __CellMatrix = value; } }
            ControlBounds IFlowLayoutItem.MainLabelBounds { get { return __MainLabelBounds; } set { __MainLabelBounds = value; } }
            ContentAlignmentType? IFlowLayoutItem.MainLabelAlignment { get { return __MainLabelAlignment; } set { __MainLabelAlignment = value; } }
            ControlBounds IFlowLayoutItem.ControlBounds { get { return __ControlBounds; } set { __ControlBounds = value; } }
            ControlBounds IFlowLayoutItem.SuffixLabelBounds { get { return __SuffixLabelBounds; } set { __SuffixLabelBounds = value; } }
            ContentAlignmentType? IFlowLayoutItem.SuffixLabelAlignment { get { return __SuffixLabelAlignment; } set { __SuffixLabelAlignment = value; } }
            bool IFlowLayoutItem.AcceptedWidth { get { return __AcceptedWidth; } set { __AcceptedWidth = value; } }
            bool IFlowLayoutItem.AcceptedHeight { get { return __AcceptedHeight; } set { __AcceptedHeight = value; } }

            void IFlowLayoutItem.ResetFlowFinalResults() { this._ResetFlowFinalResults(); }

            #endregion
            #endregion
            #region Zpracování layoutu panelu
            /// <summary>
            /// Zajistí plné zpracování this containeru, rekurzivně jeho Child containerů a zdejších Controlů.
            /// </summary>
            internal void ProcessPanel()
            {
                var args = this._LayoutArgs;
                var startTime = DxComponent.LogTimeCurrent;
                var guideLines = _ProcessContainer(args.DebugImagesWithGuideLines);      // Zpracuje this container, a rekurzivně jeho Child containery etc
                _PreparePanelAbsoluteBounds();   // Naplní absolutní souřadnice do všech (i vnořených) prvků
                if (args.LogTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Layout panel '{__Name}': {DxComponent.LogTokenTimeMicrosec}", startTime);
                if (args.SaveDebugImages) _CreateImageFile(guideLines);
            }
            /// <summary>
            /// Zajistí plné zpracování this containeru, rekurzivně jeho Child containerů a zdejších Controlů.
            /// </summary>
            /// <param name="takeGuideLines">Získat GuideLines?</param>
            private FlowGuideLine[] _ProcessContainer(bool takeGuideLines)
            {
                FlowGuideLine[] guideLines = null;
                if (_HasChilds)
                {
                    _PrepareChilds();                                // Příprava: Containery kompletně (rekurzivně), a poté všechny prvky: doplnění aplikačních dat a měření primární velikosti
                    guideLines = _PositionChilds(takeGuideLines);    // Kvalifikuje Child prvky na Fixed a Flow; a pro Flow prvky vyřeší jejich rozmístění (souřadnice) pomocí FlowLayoutu. Poté řeší Fixed prvky.
                }
                _ProcessMargins(guideLines);                         // Zpracuje Margins tohoto containeru = navýší všechny interní souřadnice svých prvků a upraví svoji velikost
                _ProcessContentSize();
                return guideLines;
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
                        item._PrepareMeForGroup(item._DfGroup);
                        item._ProcessContainer(false);                         // Child je Grupa = Container, ať si projde tuto metodu rekurzivně sám
                    }
                    else if (item._IsControl)
                    {
                        item._PrepareMeForControl(item._DfControl);
                    }
                }
            }
            /// <summary>
            /// Provede přípravné kroky před tvorbou layoutu pro danou grupu.
            /// </summary>
            /// <param name="dfGroup"></param>
            /// <exception cref="NotImplementedException"></exception>
            private void _PrepareMeForGroup(DfGroup dfGroup)
            {
                // Implicitní validace controlu řeší výhradně Main a Suffix Labely, jejich rozměr:
                DfTemplateLayout.ValidateControlInfo(this);
                _RefreshData();
            }
            /// <summary>
            /// Provede přípravné kroky před tvorbou layoutu pro daný control.
            /// Zajistí určení implicitních velikostí pro control a pro label.
            /// </summary>
            /// <param name="dfControl"></param>
            /// <exception cref="NotImplementedException"></exception>
            private void _PrepareMeForControl(DfBaseControl dfControl)
            {
                //  Tato metoda zajistí doplnění vlastností controlu pro ty hodnoty, které nejsou explicitně zadané v šabloně, 
                //     na základě dat dodaných z jádra pro konkrétní atribut dané třídy
                // Velikost controlu
                // MainLabel - text, velikost (nikoli konkrétní pozice)
                // Tooltip
                // Překlady textů z formátovacách stringů : "fm(MSG001)" => "Reference", atd  (pro labely, pro tooltipy)

                // Validace externí (tj. aplikační kód) a lokální (defaultní):
                this._LayoutArgs.InfoSource.ValidateControlInfo(this);
                DfTemplateLayout.ValidateControlInfo(this);
                _RefreshData();
            }
            /// <summary>
            /// Vytvoří pole sloupců, projde všechny Childs, nastaví jim LayoutMode = Fixed / Flow.
            /// Pro Flow prvky určí jejich pozici v matici: LayoutBeginRowIndex, LayoutBeginColumnIndex, LayoutEndColumnIndex.
            /// Pro jednosloupcový prvek typu Flow započítá jeho šířky (Label, Control) do jemu odpovídajícího sloupce.
            /// </summary>
            /// <param name="takeGuideLines">Získat GuideLines?</param>
            private FlowGuideLine[] _PositionChilds(bool takeGuideLines)
            {
                FlowGuideLine[] guideLines = null;
                this.__ContentSize = null;
                var childs = this.__Childs;
                if (childs.Count == 0) return guideLines;                      // Není co dělat...

                // Flow prvky a ty ostatní:
                List<ItemInfo> processItems = null;                            // Prvky pro další zpracování layoutu (Non Flow)
                Size contentSize = new Size();
                if (childs.Any(i => i.__LayoutMode == LayoutModeType.Flow && !i._IsAbsent))
                    // Pokud mám nějaké FlowItems, pak prvky LayoutModeType.Flow zpracuji a ostatní prvky dám do pole processItems:
                    processFlowItems(childs, contentSize, out processItems);
                else
                    // Nemám FlowItems? Pak v dalším kole zpracuji všechny prvky childs:
                    processItems = childs.ToList();

                // Non-Flow prvky:
                processNonFlowItems(processItems, contentSize, childs);

                // Finální velikost:
                __ContentSize = contentSize;

                return guideLines;


                // Z dodaného pole všech prvků (které obsahuje alespoň jeden v režimu Flow) vytvoří FlowLayout, vrátí jeho pozici, a určí prvky pro další zpracování NonFlow
                void processFlowItems(List<ItemInfo> items, Size contentSize, out List<ItemInfo> otherItems)
                {
                    otherItems = null;

                    using (var flowLayout = new DfFlowLayoutInfo(__ChildStyle, this.__DfItem.Name))
                    {
                        flowLayout.Reset();
                        foreach (var item in items)
                        {
                            if (item.__LayoutMode == LayoutModeType.Flow)
                            {   // Flow prvek zpracuji (pokud není Absent):
                                if (!item._IsAbsent)
                                    flowLayout.AddFlowItem(item);
                            }
                            else
                            {   // Prvek jiného typu new Flow odložím do seznamu ostatních prvků, a zpracují se poté:
                                if (otherItems is null) otherItems = new List<ItemInfo>();
                                otherItems.Add(item);
                            }
                        }
                        flowLayout.ProcessFlowItems();
                        if (takeGuideLines) guideLines = flowLayout.CreateGuideLines();

                        addBoundsToSize(flowLayout.FlowLayoutBounds, contentSize);
                    }
                }
                // Pro prvky z dodaného pole prvků ke zpracování provede jejich roztřídění a zpracování podle typu.
                void processNonFlowItems(List<ItemInfo> processItems, Size contentSize, List<ItemInfo> allItems)
                {
                    if (processItems is null || processItems.Count == 0) return;

                    // Dictionary pro vyhledávání prvků podle jména pro prvky s ParentBoundsName
                    Dictionary<string, ItemInfo> allDictionary = null;
                    if (processItems.Any(i => (i.__LayoutMode == LayoutModeType.FlowInParent || i.__LayoutMode == LayoutModeType.BoundsInParent)))
                    {   // Pokud nějaký prvek bude potřebovat najít svého parenta, tak si připravím Dictionary:
                        allDictionary = new Dictionary<string, ItemInfo>();
                        foreach (var item in allItems)
                            allDictionary.Add(item._Key, item);
                    }

                    // Projdu prvky a zpracuji je podle jejich režimu:
                    foreach (var processItem in processItems)
                    {
                        switch (processItem.__LayoutMode)
                        {
                            case LayoutModeType.FlowInParent:
                                processFlowInParentItem(processItem, contentSize, allDictionary);
                                break;
                            case LayoutModeType.BoundsInParent:
                                processBoundsInParentItem(processItem, contentSize, allDictionary);
                                break;
                            case LayoutModeType.FixedAbsolute:
                                processFixedAbsoluteItem(processItem, contentSize, allDictionary);
                                break;
                        }
                    }
                }
                // Zpracuje prvek v režimu FlowInParent
                void processFlowInParentItem(ItemInfo processItem, Size contentSize, Dictionary<string, ItemInfo> allDictionary)
                {
                    string key = processItem._ParentBoundsKey;
                    if (key != null && allDictionary.TryGetValue(key, out var parentItem) && parentItem.__CellMatrix != null)
                    {
                        processItem.__CellMatrix = parentItem.__CellMatrix;
                        processItem.__ControlExists = true;
                        DfFlowLayoutInfo.ProcessInnerItemBounds(processItem, __ChildStyle);
                        addItemBoundsToSize(processItem, contentSize);
                    }
                }
                // Zpracuje prvek v režimu BoundsInParent
                void processBoundsInParentItem(ItemInfo processItem, Size contentSize, Dictionary<string, ItemInfo> allDictionary)
                {
                    var bounds = processItem.__DesignBounds;
                    string key = processItem._ParentBoundsKey;
                    if (bounds != null && key != null && allDictionary.TryGetValue(key, out var parentItem) && parentItem.__CellMatrix != null)
                    {
                        var parentBounds = parentItem.__CellMatrix.GetBounds(processItem.__ExpandControl);  // Souřadnice Controlu v Parent buňce, volitelně Expanded
                        int pl = parentBounds.Left;
                        int pt = parentBounds.Top;

                        int l = bounds.Left ?? 0;                                        // Zadané souřadnice aktuálního prvku jsou relativní k Parent buňce
                        int t = bounds.Top ?? 0;
                        int w = getSize(bounds.Width, parentBounds.Width, 100);          // Pixely nebo procenta z Parent velikosti
                        int h = getSize(bounds.Height, parentBounds.Height, 20); 
                        var controlBounds = new ControlBounds(pl + l, pt + t, w, h);     // Posunu souřadnice prvku (l,t) o souřadnice parenta (x,y)

                        DfFlowLayoutInfo.ProcessFixedItemBounds(processItem, controlBounds, __ChildStyle);
                        addItemBoundsToSize(processItem, contentSize);
                    }
                }
                // Vrátí velikost 'currSize': pokud je v pixelech, pak přímo; pokud je v procentech, pak vůči 'parentSize'; a pokud není, pak 'defaultSize'
                int getSize(Int32P? currSize, int parentSize, int defaultSize)
                {
                    if (currSize.HasValue)
                    {
                        if (currSize.Value.IsPixel) return currSize.Value.NumberPixel.Value;
                        if (currSize.Value.IsPercent) return (int)Math.Round(((double)(parentSize * currSize.Value.NumberPercent.Value) / 100d), 0);
                    }
                    return defaultSize;
                }
                // Zpracuje prvek v režimu FixedAbsolute
                void processFixedAbsoluteItem(ItemInfo processItem, Size contentSize, Dictionary<string, ItemInfo> allDictionary)
                {
                    var bounds = processItem.__DesignBounds;
                    if (bounds != null)
                    {
                        int l = bounds.Left ?? 0;
                        int t = bounds.Top ?? 0;
                        int w = bounds.Width?.NumberPixel ?? 100;
                        int h = bounds.Height?.NumberPixel ?? 20;
                        var controlBounds = new ControlBounds(l, t, w, h);

                        DfFlowLayoutInfo.ProcessFixedItemBounds(processItem, controlBounds, __ChildStyle);
                        addItemBoundsToSize(processItem, contentSize);
                    }
                }
                // Přidá souřadnice aktivních oblastí daného prvku do sumární velikosti 'contentSize' (řeší Right a Bottom)
                void addItemBoundsToSize(ItemInfo processItem, Size contentSize)
                {
                    if (processItem.__MainLabelBounds != null) addBoundsToSize(processItem.__MainLabelBounds, contentSize);
                    if (processItem.__ControlBounds != null) addBoundsToSize(processItem.__ControlBounds, contentSize);
                    if (processItem.__SuffixLabelBounds != null) addBoundsToSize(processItem.__SuffixLabelBounds, contentSize);
                }
                // Zvětší velikost 'contentSize' tak, aby se do ní vešla souřadnice 'bounds'. Neřešíme Left a Top, jen Right a Bottom.
                void addBoundsToSize(ControlBounds bounds, Size contentSize)
                {
                    if (bounds is null) return;
                    if (!contentSize.Width.HasValue || bounds.Right > contentSize.Width.Value) contentSize.Width = bounds.Right;
                    if (!contentSize.Height.HasValue || bounds.Bottom > contentSize.Height.Value) contentSize.Height = bounds.Bottom;
                }
            }
            /// <summary>
            /// Zpracuje Margins tohoto containeru = navýší všechny interní souřadnice svých prvků a upraví svoji velikost
            /// </summary>
            /// <param name="guideLines">Vodiící linky</param>
            private void _ProcessMargins(FlowGuideLine[] guideLines)
            {
                var margins = __ChildStyle.Margins;
                // Distance Left, Top, Right, Bottom:
                int dl = margins?.Left ?? 0;
                if (dl < 0) dl = 0;
                int dt = margins?.Top ?? 0;
                if (dt < 0) dt = 0;
                int dr = margins?.Right ?? 0;
                if (dr < 0) dr = 0;
                int db = margins?.Bottom ?? 0;
                if (db < 0) db = 0;

                // Posunu svoje Childs, pokud je důvod:
                var childs = this.__Childs;
                if ((dl > 0 || dt > 0) && (childs != null && childs.Count > 0))
                {
                    foreach (var item in childs)
                        processMargins(item);
                }

                // Posunu guideLines, pokud je důvod:
                if (guideLines != null && (dl > 0 || dt > 0))
                {
                    foreach (var guideLine in guideLines)
                        guideLine.AddMargin(guideLine.Axis == AxisType.X ? dl : dt);
                }

                // Zvětším velikost obsahu celého containeru, pokud je důvod:
                int dw = dl + dr;
                int dh = dt + db;
                var size = this.__ContentSize;
                if (size != null)
                {
                    size.Width = size.Width + dw;
                    size.Height = size.Height + dh;
                }

                // Posune souřadnice v daném prvku o (dl, dt)
                void processMargins(ItemInfo item)
                {
                    if (item.__ControlExists) processBounds(item.__ControlBounds);
                    if (item.__MainLabelExists) processBounds(item.__MainLabelBounds);
                    if (item.__SuffixLabelExists) processBounds(item.__SuffixLabelBounds);
                }
                // Posune dané souřadnice o (dl, dt)
                void processBounds(ControlBounds bounds)
                {
                    if (bounds != null)
                    {
                        bounds.Left = bounds.Left + dl;
                        bounds.Top = bounds.Top + dt;
                    }
                }
            }
            /// <summary>
            /// Určí velikost <see cref="__ImplicitControlMinimalWidth"/> a <see cref="__ImplicitControlMinimalHeight"/> podle již existující <see cref="__ContentSize"/>.
            /// Tento algoritmus tedy přenese reálnou velikost obsahu z Containeru (kde <see cref="__ContentSize"/> je určena jako prostor vnitřních prvků) 
            /// do vnějších hodnot, které popisují tento prvek navenek (tedy ve vyšším containeru se bude při tvorbě jeho layoutu rezervovat patřičný prostor pro grupu a její obsah).
            /// </summary>
            private void _ProcessContentSize()
            {
                var size = this.__ContentSize;
                this.__ImplicitControlOptimalWidth = size?.Width;
                this.__ImplicitControlOptimalHeight = size?.Height;

                //  this.__ImplicitControlMinimalWidth = size?.Width;
                //  this.__ImplicitControlMinimalHeight = size?.Height;
            }
            /// <summary>
            /// Velikost obsahu včetně Margins
            /// </summary>
            private Size __ContentSize;
            #endregion
            #region Určení absolutní souřadnice každého prvku
            /// <summary>
            /// Projde všechny prvky a přidělí jim konkrétní absolutní souřadnice = v rámci Root panelu.
            /// Tato metoda je vyvolána pro prvek typu Panel. 
            /// <para/>
            /// Smí být vyvolána pouze jedenkrát, protože reálně posunuje souřadnice prvků. Opakované volání by je posunulo znovu.
            /// </summary>
            private void _PreparePanelAbsoluteBounds()
            {
                _PrepareContainerAbsoluteBounds(this, 0, 0);
            }
            /// <summary>
            /// Projde svoje prvky a posune souřadnice controlu a labelů o danou hodnotu.
            /// Pokud najde container, posune jeho souřadnice a zajistí rekurzivní posun i pro jeho prvky.
            /// Souřadnice vnořených containerů pak budou absolutní vzhledem k Root panelu.
            /// </summary>
            /// <param name="container"></param>
            /// <param name="addX"></param>
            /// <param name="addY"></param>
            private static void _PrepareContainerAbsoluteBounds(ItemInfo container, int addX, int addY)
            {
                var childs = container.__Childs;
                if (childs != null)
                {
                    foreach (var child in childs)
                    {
                        if (child != null)
                        {
                            prepareAbsoluteBounds(child, addX, addY);
                            if (child._HasChilds)
                            {
                                int nextX = child.__ControlBounds.Left;
                                int nextY = child.__ControlBounds.Top;
                                _PrepareContainerAbsoluteBounds(child, nextX, nextY);
                            }
                        }
                    }
                }

                void prepareAbsoluteBounds(ItemInfo item, int dx, int dy)
                {
                    if (dx != 0 || dy != 0)
                    {
                        prepareAbsoluteBoundsOne(item.__ControlBounds, dx, dy);
                        prepareAbsoluteBoundsOne(item.__MainLabelBounds, dx, dy);
                        prepareAbsoluteBoundsOne(item.__SuffixLabelBounds, dx, dy);
                    }
                }
                void prepareAbsoluteBoundsOne(ControlBounds bounds, int dx, int dy)
                {
                    if (bounds != null)
                    {
                        bounds.Left = bounds.Left + dx;
                        bounds.Top = bounds.Top + dy;
                    }
                }
            }
            #endregion
            #region Vizualizace PixelLayoutu (vytvoření Image, reprezentující aktuální stav layoutu)
            /// <summary>
            /// Z prvků layoutu vygeneruje bitmapu v měřítku 1:1, respektující rozměry a obsah containeru.
            /// Uloží ji do souboru do Temp adresáře.
            /// Plné jméno souboru vrátí.
            /// </summary>
            /// <param name="guideLines">Vodiící linky</param>
            /// <returns></returns>
            private string _CreateImageFile(FlowGuideLine[] guideLines)
            {
                var startTime = DxComponent.LogTimeCurrent;

                string file = null;

                var args = this._LayoutArgs;
                string dfName = args.DataForm.FileName;
                dfName = (!String.IsNullOrEmpty(dfName) ? System.IO.Path.GetFileNameWithoutExtension(dfName) : "dataform");      // dw_simple.frm.xml   =>   dw_simple.frm
                if (dfName.Contains(".")) dfName = System.IO.Path.GetFileNameWithoutExtension(dfName);                           // dw_simple.frm       =>   dw_simple

                using (var image = _CreateImage(guideLines))
                {
                    if (image != null)
                    {
                        string cnt = ((__ImageCounter++) % 1000).ToString("000");
                        string dateId = $"{DateTime.Now:yyyyMMdd_HHmmss}_{cnt}";
                        string itemId = dfName + "_" + this.__Name;
                        string name = $"ItemLayout_{itemId}_{dateId}.png";
                        string path = System.IO.Path.GetTempPath();
                        if (!String.IsNullOrEmpty(args.DebugImagePath))
                        {
                            path = System.IO.Path.Combine(path, args.DebugImagePath.Trim());
                            if (!System.IO.Directory.Exists(path))
                                System.IO.Directory.CreateDirectory(path);
                        }
                        file = System.IO.Path.Combine(path, name);
                        image.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }

                if (file != null)
                {
                    if (args.DebugImages is null) args.DebugImages = new List<string>();
                    args.DebugImages.Add(file);
                }
                if (args.LogTime) DxComponent.LogAddLineTime(LogActivityKind.DataFormRepository, $"Save Debug image for panel '{__Name}': {DxComponent.LogTokenTimeMicrosec}", startTime);
                return file;
            }
            /// <summary>
            /// Z prvků layoutu vygeneruje bitmapu v měřítku 1:1, respektující rozměry a obsah containeru.
            /// </summary>
            /// <param name="guideLines">Vodiící linky</param>
            /// <returns></returns>
            private System.Drawing.Image _CreateImage(FlowGuideLine[] guideLines)
            {
                // Kreslit, či nekreslit? - to je, oč tu běží...
                var size = this.__ContentSize;
                if (size is null) return null;
                int width = size.Width ?? 0;
                int height = size.Height ?? 0;
                if (width <= 0 || height <= 0) return null;

                // Definujeme barvy pro prvky:
                var workspaceColor = System.Drawing.Color.FromArgb(255, 206, 206, 206);

                var labelBorderColor = System.Drawing.Color.FromArgb(255, 178, 191, 198);
                var labelBackColor = System.Drawing.Color.FromArgb(255, 221, 221, 224);
                var labelTextColor = System.Drawing.Color.FromArgb(255, 80, 64, 80);

                var controlBorderColor = System.Drawing.Color.FromArgb(255, 80, 80, 86);
                var controlBackColor = System.Drawing.Color.FromArgb(255, 233, 233, 225);
                var controlTextColor1 = System.Drawing.Color.FromArgb(190, 96, 96, 96);
                var controlTextColor2 = System.Drawing.Color.FromArgb(255, 32, 32, 32);

                var guideLineCellColor = System.Drawing.Color.FromArgb(200, 200, 40, 60);
                var guideLineControlColor = System.Drawing.Color.FromArgb(200, 40, 200, 60);
                var guideLineLabelColor = System.Drawing.Color.FromArgb(200, 190, 180, 40); 

                // Image a grafické nástroje, jimiž postupně vykreslíme jednotlivé prvky:
                var image = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var graphics = System.Drawing.Graphics.FromImage(image))
                using (var brush = new System.Drawing.SolidBrush(controlBackColor))
                using (var pen = new System.Drawing.Pen(controlBorderColor))
                using (var stringFormat = new System.Drawing.StringFormat(System.Drawing.StringFormatFlags.NoWrap | System.Drawing.StringFormatFlags.LineLimit))
                {
                    graphics.Clear(workspaceColor);

                    var childs = this._AllChildItems;
                    foreach (var i in childs)
                        drawItem(i, graphics, brush, pen, stringFormat);

                    if (guideLines != null)
                    {
                        graphicsForRectangles(graphics);
                        foreach (var guideLine in guideLines)
                            drawGuideLine(guideLine, graphics, brush, pen);
                    }
                }
                return image;


                // Vykreslí celý prvek do grafiky
                void drawItem(ItemInfo item, System.Drawing.Graphics graphics, System.Drawing.SolidBrush brush, System.Drawing.Pen pen, System.Drawing.StringFormat stringFormat)
                {
                    var iFlow = item as IFlowLayoutItem;
                    var iData = item as IDataFormItem;

                    // Main Label:
                    if (iFlow.MainLabelExists && iFlow.MainLabelBounds != null)
                    {
                        var alignment = iFlow.MainLabelAlignment ?? ContentAlignmentType.MiddleLeft;
                        drawLabel(graphics, brush, pen, stringFormat, iData.MainLabelText, iFlow.MainLabelBounds, alignment);
                    }

                    // Suffix Label:
                    if (iFlow.SuffixLabelExists && iFlow.SuffixLabelBounds != null)
                    {
                        var alignment = iFlow.SuffixLabelAlignment ?? ContentAlignmentType.MiddleLeft;
                        drawLabel(graphics, brush, pen, stringFormat, iData.SuffixLabelText, iFlow.SuffixLabelBounds, alignment);
                    }

                    // Control (nikoliv Container):
                    if (item._IsControl && iFlow.ControlExists && iFlow.ControlBounds != null)
                    {
                        var alignment = ContentAlignmentType.MiddleCenter;
                        drawControl(graphics, brush, pen, stringFormat, iData.Name, iFlow.ControlBounds, alignment);
                    }
                }
                // Vykreslí Label včetně podkladu
                void drawLabel(System.Drawing.Graphics graphics, System.Drawing.SolidBrush brush, System.Drawing.Pen pen, System.Drawing.StringFormat stringFormat, string text, ControlBounds bounds, ContentAlignmentType alignment)
                {
                    var r = getRectangle(bounds);

                    using (var path = getControlPathSquare(r))
                    {
                        graphicsForRectangles(graphics);

                        brush.Color = labelBackColor;
                        graphics.FillPath(brush, path);                              // BackColor pod labelem MainLabel

                        pen.Color = labelBorderColor;
                        graphics.DrawPath(pen, path);                                // Border okolo prostoru pro MainLabel
                    }

                    if (isValidBounds(bounds))
                    {
                        var font = System.Drawing.SystemFonts.DefaultFont;
                        var size = graphics.MeasureString(text, font, r.Width);
                        var point = alignText(r, size, alignment);
                        brush.Color = labelTextColor;
                        graphicsForText(graphics);
                        graphics.DrawString(text, font, brush, point.X, point.Y, stringFormat);        // Vlastní text MainLabel / SuffixLabel
                    }
                }
                // Vykreslí Control včetně podkladu
                void drawControl(System.Drawing.Graphics graphics, System.Drawing.SolidBrush brush, System.Drawing.Pen pen, System.Drawing.StringFormat stringFormat, string text, ControlBounds bounds, ContentAlignmentType alignment)
                {
                    var r = getRectangle(bounds);
                    using (var path = getControlPathRound(r))
                    {
                        graphicsForText(graphics);

                        brush.Color = controlBackColor;
                        graphics.FillPath(brush, path);                              // BackColor pod Controlem

                        pen.Color = controlBorderColor;
                        graphics.DrawPath(pen, path);                                // Border okolo prostoru pro Controlem
                    }

                    if (isValidBounds(bounds))
                    {
                        var font = System.Drawing.SystemFonts.DefaultFont;
                        var size = graphics.MeasureString(text, font, r.Width, stringFormat);
                        var point = alignText(r, size, alignment);
                        brush.Color = controlTextColor1;
                        graphics.DrawString(text, font, brush, point.X + 0.60f, point.Y);        // Text uprostřed Controlu, 2x ...
                        brush.Color = controlTextColor2;
                        graphics.DrawString(text, font, brush, point.X, point.Y - 0.60f, stringFormat);
                    }
                }
                // Vrátí true, pokud do daných 'bounds' lze něco vepsat
                bool isValidBounds(ControlBounds bounds)
                {
                    return (bounds != null && bounds.Width >= 10 && bounds.Height >= 15);
                }
                // Vykreslí jednu GuideLine
                void drawGuideLine(FlowGuideLine guideLine, System.Drawing.Graphics graphics, System.Drawing.SolidBrush brush, System.Drawing.Pen pen)
                {
                    bool isCell = guideLine.LineType.HasFlag(GuideLineType.Cell);
                    bool isControl = guideLine.LineType.HasFlag(GuideLineType.Control);
                    bool isLabel = guideLine.LineType.HasFlag(GuideLineType.LabelBefore) || guideLine.LineType.HasFlag(GuideLineType.LabelAfter);

                    pen.Color = (isCell ? guideLineCellColor : (isControl ? guideLineControlColor : guideLineLabelColor));
                    pen.Width = 1f;
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                    pen.DashCap = System.Drawing.Drawing2D.DashCap.Round;

                    if (guideLine.Axis == AxisType.X)
                        graphics.DrawLine(pen, guideLine.Position, 0, guideLine.Position, height);
                    else
                        graphics.DrawLine(pen, 0, guideLine.Position, width, guideLine.Position);
                }
                // Nasetuje grafiku pro texty
                void graphicsForText(System.Drawing.Graphics graphics)
                {
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                }
                // Nasetuje grafiku pro čtverce
                void graphicsForRectangles(System.Drawing.Graphics graphics)
                {
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
                }
                // Vrátí Path pro obdélník
                System.Drawing.Drawing2D.GraphicsPath getControlPathSquare(System.Drawing.Rectangle rectangle)
                {
                    int l = rectangle.Left;
                    int t = rectangle.Top;
                    int r = rectangle.Right - 1;
                    int b = rectangle.Bottom - 1;
                    var path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddPolygon(new System.Drawing.PointF[]
                    {
                        new System.Drawing.PointF(l, t),
                        new System.Drawing.PointF(r, t),
                        new System.Drawing.PointF(r, b),
                        new System.Drawing.PointF(l, b),
                        new System.Drawing.PointF(l, t)
                    });
                    path.CloseFigure();
                    return path;
                }
                // Vrátí Path pro obdélník se zkosenými rohy
                System.Drawing.Drawing2D.GraphicsPath getControlPathRound(System.Drawing.Rectangle rectangle)
                {
                    int l = rectangle.Left;
                    int t = rectangle.Top;
                    int r = rectangle.Right - 1;
                    int b = rectangle.Bottom - 1;
                    int q = 2;
                    var path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddPolygon(new System.Drawing.PointF[]
                    {
                        new System.Drawing.PointF(l + q, t),
                        new System.Drawing.PointF(r - q, t),
                        new System.Drawing.PointF(r, t + q),
                        new System.Drawing.PointF(r, b - q),
                        new System.Drawing.PointF(r - q, b),
                        new System.Drawing.PointF(l + q, b),
                        new System.Drawing.PointF(l, b - q),
                        new System.Drawing.PointF(l, t + q),
                        new System.Drawing.PointF(l + q, t)
                    });
                    path.CloseFigure();
                    return path;
                }
                // Konvertuje Bounds na Drawing.Rectangle
                System.Drawing.Rectangle getRectangle(ControlBounds bounds)
                {
                    return new System.Drawing.Rectangle(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
                }
                // Vrátí souřadnice textu tak, aby byl zarovnán
                System.Drawing.PointF alignText(System.Drawing.Rectangle area, System.Drawing.SizeF content, ContentAlignmentType alignment)
                {
                    float x = area.X;
                    float y = area.Y;
                    float dx = 0f;
                    float dy = 0f;
                    float dw = (float)area.Width - content.Width;
                    if (dw < 0f) dw = 0f;
                    float dh = (float)area.Height - content.Height;
                    if (dh < 0f) dh = 0f;

                    switch (alignment)
                    {
                        case ContentAlignmentType.TopLeft:
                            break;
                        case ContentAlignmentType.TopCenter:
                            dx = dw / 2f;
                            break;
                        case ContentAlignmentType.TopRight:
                            dx = dw;
                            break;

                        case ContentAlignmentType.MiddleLeft:
                            dy = dh / 2f;
                            break;
                        case ContentAlignmentType.MiddleCenter:
                            dy = dh / 2f;
                            dx = dw / 2f;
                            break;
                        case ContentAlignmentType.MiddleRight:
                            dy = dh / 2f;
                            dx = dw;
                            break;

                        case ContentAlignmentType.BottomLeft:
                            dy = dh;
                            break;
                        case ContentAlignmentType.BottomCenter:
                            dy = dh;
                            dx = dw / 2f;
                            break;
                        case ContentAlignmentType.BottomRight:
                            dy = dh;
                            dx = dw;
                            break;

                    }
                    return new System.Drawing.PointF(x + dx, y + dy);
                }
            }
            /// <summary>
            /// Počitadlo vytvořených Images, slouží jako suffix názvu souboru
            /// </summary>
            private static int __ImageCounter = 0;
            #endregion
        }
        #endregion
    }
    #region class DfFlowLayoutInfo : koordinátor pro FlowLayout = jednotlivé prvky umístěné ve sloupcích a řádcích
    /// <summary>
    /// <see cref="DfFlowLayoutInfo"/> : koordinátor pro FlowLayout = jednotlivé prvky umístěné ve sloupcích a řádcích
    /// </summary>
    internal class DfFlowLayoutInfo : IDisposable
    {
        #region Konstruktor a základní proměnné
        /// <summary>
        /// Konstruktor pro deklaraci z daného stylu (smí být null)
        /// </summary>
        /// <param name="layoutStyle"></param>
        /// <param name="sourceId">ID zdroje do chybové hlášky</param>
        public DfFlowLayoutInfo(DfTemplateLayout.StyleInfo layoutStyle, string sourceId)
        {
            if (layoutStyle is null) throw new ArgumentNullException($"DfFlowLayoutInfo ctor fail: 'layoutStyle' is null.");

            __SourceId = sourceId;
            __Columns = new List<LineInfo>();
            __Rows = new List<LineInfo>();
            __Cells = new List<IFlowLayoutItem[]>();
            __Items = new List<IFlowLayoutItem>();
            __ItemsDict = new Dictionary<string, IFlowLayoutItem>();
            __Style = layoutStyle;

            _LoadStyle();
            _PrepareColumns();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return MapOfCellsAscii;
        }
        /// <summary>
        /// ID zdroje do chybové hlášky
        /// </summary>
        private string __SourceId;
        /// <summary>
        /// Definice šířek sloupců
        /// </summary>
        private List<LineInfo> __Columns;
        /// <summary>
        /// Definice výšek řádků
        /// </summary>
        private List<LineInfo> __Rows;
        /// <summary>
        /// Pole buněk.
        /// Ve směru Y (řádky) lze prvky přidávat snadným přidáním Add(pole).
        /// Ve směru X (sloupce) je pevně daný počet prvků = _ColumnsCount.
        /// Přidání nového řádku se tedy provede __Cells.Add(new IFlowLayoutItem[_ColumnsCount]).
        /// </summary>
        private List<IFlowLayoutItem[]> __Cells;
        /// <summary>
        /// Jednotlivé prvky
        /// </summary>
        private List<IFlowLayoutItem> __Items;
        /// <summary>
        /// Dictionary prvků, klíč je Key z Name
        /// </summary>
        private Dictionary<string, IFlowLayoutItem> __ItemsDict;
        /// <summary>
        /// Metoda zajistí existenci sloupců v <see cref="__Columns"/> podle požadavků z <see cref="__Style"/>.
        /// </summary>
        private void _PrepareColumns()
        {
            DfTemplateLayout.StyleInfo layoutStyle = __Style;
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
                        __Columns.Add(new LineInfo(this, AxisType.X, i, lblW, ctrW, lbrW));
                    }
                }

                if (__Columns.Count == 0)
                {   // Prostě jen počet sloupců, bez deklarované šířky:
                    if (layoutStyle.ColumnsCount.HasValue && layoutStyle.ColumnsCount.Value > 0)
                    {
                        int count = layoutStyle.ColumnsCount.Value;
                        for (int i = 0; i < count; i++)
                            __Columns.Add(new LineInfo(this, AxisType.X, i, null, null, null));
                    }
                }
            }

            if (__Columns.Count == 0)
            {   // Jediný (defaultní) sloupec, bez deklarované šířky:
                __Columns.Add(new LineInfo(this, AxisType.X, 0, null, null, null));
            }

            // Pokud styl předepisuje defaultní umístění labelu Top nebo Bottom a na odpovídající straně je definován OffsetX záporný => ten se poté umísťuje do prostoru vlevo od Controlu, tedy do Columns.LabelBefore,
            //  pak tento offset vepíšu do všech sloupců jako defaultní.
            var labelPos = __AutoLabelPosition;
            int labelOffset = (labelPos == LabelPositionType.Top ? -__TopLabelOffsetX : (labelPos == LabelPositionType.Bottom ? -__BottomLabelOffsetX : 0));
            if (labelOffset > 0)
            {   // Načítali jsme záporný offset odpovídající defaultní pozici MainLabelu; pokud labelOffset bude kladné (ted výchozí je záporné = předsazené doleva), pak tento offset vepíšu do všech sloupců jako 'LabelBeforeMaximalSize'.
                // Tím zajistím, že prostor LabelBefore (ve sloupcích má význam: "Nalevo od controlu") bude dostatečně veliký pro offset labelů.
                // Dělám to předem (nyní) - i když dosud nevím, zda reálně budou některé prvky mít takový Label (Exists a reálně umístěný jako Top nebo Bottom);
                //   důvod: takováto definice (LabelPos + záporný Offset) míří na celkový design panelů, a je naším cílem mít design shodný pro všechny panely, i když v určitém jednom panelu zrovna nebude žádný control mít takový label...
                foreach (var col in __Columns)
                    col.LabelBeforeMaximalSize = labelOffset;
            }

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
        /// Počet sloupců je dán pevně už na začátku.
        /// </summary>
        private int _ColumnsCount { get { return __Columns.Count; } }
        /// <summary>
        /// Kupodivu počet řádků není dán polem <see cref="__Rows"/> (to určíme později), ale počtem polí v <see cref="__Cells"/>. 
        /// Tam se přidává nový řádek tehdy, když je toho zapotřebí.
        /// </summary>
        private int _RowsCount { get { return __Cells.Count; } }
        /// <summary>
        /// Počet evidovaných prvků
        /// </summary>
        private int _ItemsCount { get { return __Items.Count; } }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            foreach (var column in __Columns)
                column.Dispose();
            foreach (var row in __Rows)
                row.Dispose();

            __ItemsDict = null;
            __Items = null;
            __Columns = null;
            __Rows = null;
            __Cells = null;
        }
        #endregion
        #region Informace o stylu celé grupy (StyleInfo), jeho načtení a odpovídající proměnné
        /// <summary>
        /// Načte / určí výchozí hodnoty na základě daného stylu
        /// </summary>
        private void _LoadStyle()
        {
            var style = __Style;

            __FlowBounds = new DesignBounds();
            __FlowBounds.Left = style.FlowAreaBegin?.Left;
            __FlowBounds.Top = style.FlowAreaBegin?.Top;
            __ControlMargins = new Margins(style.ControlMargins);
            __AutoLabelPosition = style.AutoLabelPosition;
            __ColumnsDistance = (style.ColumnsDistance >= 0 ? style.ColumnsDistance : 0);
            __RowsDistance = (style.RowsDistance >= 0 ? style.RowsDistance : 0);
            __ColumnImplicitSize = 50;
            __RowImplicitSize = 20;
            __TopLabelOffsetX = style.TopLabelOffsetX;
            __BottomLabelOffsetX = style.BottomLabelOffsetX;
            __LabelsRelativeToControl = style.LabelsRelativeToControl;
        }
        /// <summary>
        /// Styl panelu, definuje konstantní vlastnosti layoutu
        /// </summary>
        private DfTemplateLayout.StyleInfo __Style;
        /// <summary>
        /// Oblast FlowLayoutu od prvního do posledního sloupce a řádku. Není null, ale ve výchozím stavu nemusí mít zadané konkrétní souřadnice.
        /// </summary>
        private DesignBounds __FlowBounds;
        /// <summary>
        /// Odstupy mezi jakýmkoli Labelem a Controlem, uplatní se pouze pokud label existuje. Není null.
        /// </summary>
        private Margins __ControlMargins;
        /// <summary>
        /// Automaticky generovat labely atributů a vztahů, jejich umístění. Defaultní = <c>NULL</c>
        /// </summary>
        private LabelPositionType __AutoLabelPosition;
        /// <summary>
        /// Oddělovací vzdálenost mezi sloupci
        /// </summary>
        private int __ColumnsDistance;
        /// <summary>
        /// Oddělovací vzdálenost mezi řádky
        /// </summary>
        private int __RowsDistance;
        /// <summary>
        /// Výchozí šířka sloupce, pokud nebude jinak určeno
        /// </summary>
        private int __ColumnImplicitSize;
        /// <summary>
        /// Výchozí výška sloupce, pokud nebude jinak určeno
        /// </summary>
        private int __RowImplicitSize;
        /// <summary>
        /// Posunutí Main Labelu umístěného Top, na ose X, oproti souřadnici X vlastního Controlu.
        /// Kladná hodnota posune doprava.
        /// Může být záporné, pak bude label předsazen vlevo před Controlem.
        /// </summary>
        private int __TopLabelOffsetX;
        /// <summary>
        /// Posunutí Main Labelu umístěného Bottom, na ose X, oproti souřadnici X vlastního Controlu.
        /// Kladná hodnota posune doprava.
        /// Může být záporné, pak bude label předsazen vlevo před Controlem.
        /// </summary>
        private int __BottomLabelOffsetX;
        /// <summary>
        /// true = Umisťovat labely relativně vůči Controlu / false = dávat je striktně podle mřížky. 
        /// Projeví se to u controlů, které mají např. menší šířku, než okolní controly ve sloupci, a control bude zarovnán HPosition = Right, 
        /// pak jeho Label by měl být spíš umístěn k levé hraně Controlu (obzvlášť label na pozici Top, Bottom, BeforeRight).
        /// Rovněž má vliv na SufixLabel, který takto bude umístěn přímo vpravo vedle Controlu, a ne do sloupce s ostatními labely.
        /// </summary>
        private bool __LabelsRelativeToControl;
        #endregion
        #region Veškerý proces tvorby layoutu: výpočty velikostí řádků a sloupců, určení souřadnic pro jednotlivé buňky a jejich prvky
        /// <summary>
        /// Metoda resetuje celý svůj prostor. 
        /// Následně se volá metoda <see cref="AddFlowItem(IFlowLayoutItem)"/> pro jednotlivé prvky layoutu.
        /// Na závěr se volá metoda <see cref="ProcessFlowItems()"/> pro dopočtení souřadnic sloupců a řádků.
        /// </summary>
        public void Reset()
        {
            this.__Rows.ForEach(r => r.Dispose());
            this.__Rows.Clear();
            this.__Cells.Clear();
            this.__Items.Clear();
            this.__ItemsDict.Clear();
            this.__CurrentRowIndex = 0;
            this.__CurrentColIndex = 0;
        }
        /// <summary>
        /// Metoda přidá další prvek do layoutu.
        /// </summary>
        /// <param name="layoutItem"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AddFlowItem(IFlowLayoutItem layoutItem)
        {
            if (layoutItem is null || layoutItem.IsAbsent) return;

            int columnsCount = _ColumnsCount;
            bool isFixedColumn = false;
            int firstColumn = 0;
            int rowIndex = __CurrentRowIndex;
            int colIndex = __CurrentColIndex;
            int? itemColIndex = layoutItem.DesignColIndex;
            int itemColSpan = getSpan(layoutItem.DesignColSpan, columnsCount);
            int itemRowSpan = getSpan(layoutItem.DesignRowSpan, null);

            // Pokud autor zadal moc široký ColSpan 'itemColSpan', který se nevejde do layoutu, zmenším ho tak, aby se vešel:
            if (itemColSpan > columnsCount) itemColSpan = columnsCount;

            // Pokud autor zadal umísťovací index 'itemColIndex', připravíme se na to:
            if (itemColIndex.HasValue)
                processItemColIndex(itemColIndex.Value);

            // Poslední hodnota 'colIndex' (=X), na které mohu zkoušet hledat volné místo tak, abyc se mi vešel prvek s daným 'itemColSpan':
            int lastColumn = columnsCount - itemColSpan;

            // Nyní půjdu do mapy __Cells, a budu hledat pozici X,Y takovou, na které NEBUDE v této mapě v potřebné velikosti žádný prvek:
            int x = colIndex;
            int y = rowIndex;
            for (int t = 0; t < 120; t++)
            {   //   Hodnota t je jen TimeOut a nemá jiný význam. Typicky nadjeme volné místo během 5 pokusů, záleží na ColSpan.
                // Pokud aktuální souřadnice sloupce je za posledním přípustným sloupcem 'lastColumn', pak přejdeme na další řádek na první sloupec:
                checkCurrentIndexes();
                // Pokud aktuální pozice (x,y) je v rozsahu (itemColSpan,itemRowSpan) volná, pak skončíme:
                if (isDisponible(x, x + itemColSpan, y, y + itemRowSpan)) break;
                // Není volno: zvýšíme testovanou souřadnici a zkusíme další test:
                searchNextPosition();
             }
            //  Máme souřadnice X,Y kam umístíme náš prvek:
            // Do naší mapy __Cells a do prvku a do řádků __Rows:
            storeResults(x, x + itemColSpan, y, y + itemRowSpan);
           

            // Vrátí validní ColSpan pro daný požadavek a maximální počet prvků v konkrétní ose
            int getSpan(int? itemSpan, int? maxSpan)
            {
                if (!itemSpan.HasValue) return 1;
                int span = itemSpan.Value;
                return (span < 1 ? 1 : ((maxSpan.HasValue && span > maxSpan.Value) ? maxSpan.Value : span));
            }
            // Zpracuje požadavek na umístění na ColIndex
            void processItemColIndex(int iColIdx)
            {
                // a) Designer zadal hodnotu 'itemColIndex' mimo reálné sloupce (< 0 nebo  >= columnsCount) ?
                //   =>  V podstatě říka "Konec řádku": Chce přejít na nejbližší další řádek, která je celý prázdný (viz RowSpan), na index 0:
                if (iColIdx < 0 || iColIdx >= columnsCount)
                {
                    if (colIndex > 0)
                        rowIndex++;
                    colIndex = 0;
                }
                // b) Designer zadal hodnotu 'itemColIndex' v rozmezí 0 až poslední sloupec, tedy reálnou pozici uvnitř layoutu (včetně 0):
                //   => reálně budeme umísťovat prvek výhradně na daný sloupec. Ošetříme ColSpan. Zafixujeme sloupec i pro hledání na dalších řádcích.
                else
                {
                    // Pokud zadaný index + zadaný ColSpan přesáhne počet sloupců layoutu, pak zmenšíme ColSpan:
                    if ((iColIdx + itemColSpan) > columnsCount)      // Pro iColIdx = 3 a původní itemColSpan = 2 a pro layout se 4 sloupci:
                        itemColSpan = columnsCount - iColIdx;        //  změním itemColSpan = 1

                    // Pokud už aktuálně stojíme už za daným sloupcem, musíme jít na další řádek:
                    if (colIndex > iColIdx)
                        rowIndex++;

                    colIndex = iColIdx;                              // Aktuálně začneme umísťovat na daný sloupec
                    firstColumn = iColIdx;                           // I po přechodu na další řádek (při hledání vhodné pozice) budeme začínat na daném indexu, ne na 0.
                    isFixedColumn = true;                            // Fixed column říká, že pro další pokusy budeme jen zvyšovat číslo řádku. Sloupec neměníme.
                }
            }
            // Před testem dostupnosti prostoru ověří hodnoty X,Y: že sloupec není za koncem prostoru. Pokud ano, jde na další řádek a první sloupec.
            void checkCurrentIndexes()
            {
                if (x <= lastColumn) return;                         // Pokud jsme v reálném prostoru, OK.
                y++;                                                 // Jdeme na další řádek
                x = firstColumn;                                     //  a na jeho první sloupec.
            }
            // Nastaví souřadnice X,Y pro hledání dalšího volného místa
            void searchNextPosition()
            {
                if (isFixedColumn)
                {
                    x = firstColumn;
                    y++;
                }
                else
                {
                    x++;
                }
            }
            // Vrátí true, pokud v dané pozici (Left, Right, Top, Bottom) je volno pro umístění daného prvku
            bool isDisponible(int left, int right, int top, int bottom)
            {
                int rowsCount = _RowsCount;
                for (int r = top; r < bottom; r++)
                {   // Pomalý cyklus jde přes řádky:
                    if (r >= rowsCount) return true;                 // Daný řádek nemáme => je tedy celý volný (a ani vyšší čísla řádků mít nebudeme)!
                    var rowData = __Cells[r];                        // Obsah daného existujícího řádku = jednotlivé buňky
                    for (int c = left; c < right; c++)
                    {   // Rychlý cyklus jde přes sloupce:
                        if (rowData[c] != null) return false;        // Daná buňka je obsazená, konec.
                    }
                }
                return true;                                         // Všechny řádky existují, a požadované buňky jsou v nich volné.
            }
            // Zajistí obsazení buněk __Cells na daných pozicích aktuálním prvkem 'layoutItem'. Do něj i vepíše jeho nalezené pozice. Nastaví pozice pro další prvek.
            void storeResults(int left, int right, int top, int bottom)
            {
                for (int r = top; r < bottom; r++)
                {   // Pomalý cyklus jde přes řádky:
                    if (r >= _RowsCount) prepareRow(r);
                    var rowData = __Cells[r];                        // Obsah daného existujícího řádku
                    for (int c = left; c < right; c++)
                    {   // Rychlý cyklus jde přes sloupce:
                        if (rowData[c] != null)
                            throw new InvalidOperationException($"FlowLayout: pokus o vícenásobné použití buňky [Row:{r},Col:{c}].");
                        rowData[c] = layoutItem;                     // Danou buňku obsadíme daným prvkem
                    }
                }

                this.__Items.Add(layoutItem);
                string key = DfTemplateLayout.GetItemKey(layoutItem.Name);
                if (key != null)
                {
                    if (this.__ItemsDict.ContainsKey(key))
                        throw new ArgumentException($"Item with key '{key}' is duplicite in container '{__SourceId}'");
                    this.__ItemsDict.Add(key, layoutItem);
                }

                layoutItem.FlowRowBeginIndex = top;
                layoutItem.FlowRowEndIndex = bottom - 1;
                layoutItem.FlowColBeginIndex = left;
                layoutItem.FlowColEndIndex = right - 1;
                layoutItem.AcceptedWidth = false;
                layoutItem.AcceptedHeight = false;

                __CurrentColIndex = x + itemColSpan;
                __CurrentRowIndex = y;
            }
            // Zajistí, že v poli __Cells bude přítomen řádek na indexu (rIndex) = přidá tolik prvek, aby se tam dostal.
            void prepareRow(int rIndex)
            {
                if (rIndex >= 10000)
                    throw new InvalidOperationException($"FlowLayout: pokus o vytvoření layoutu s 10000 řádky.");

                int add = rIndex + 1 - __Cells.Count;
                while (add-- > 0)
                {
                    __Cells.Add(new IFlowLayoutItem[_ColumnsCount]);
                    __Rows.Add(new LineInfo(this, AxisType.Y, __Rows.Count));
                }
            }
        }
        /// <summary>
        /// Dopočítá šířky sloupců a výšky řádků podle obsahu a určí jejich fyzické souřadnice.
        /// Určí konkrétní souřadnice jednotlivých prvků (dodaných v metodě <see cref="AddFlowItem(IFlowLayoutItem)"/>) a vepíše je do těchto prvků.<br/>
        /// Výchozí souřadnice celého FlowLayout prostoru je definovaná stylem dodaným do konstruktoru, jeho hodnotou <see cref="DfTemplateLayout.StyleInfo.FlowAreaBegin"/>.
        /// Určí sumární prostor FlowLayout prostoru (protože dopočítá i konec obsazeného prostoru), viz <see cref="FlowLayoutBounds"/>.
        /// </summary>
        public void ProcessFlowItems()
        {
            _ProcessReset();
            _ProcessLabels();
            _ProcessControlSingleSpan();
            _ProcessControlMultiSpan();
            _ProcessDimensions();
            _ProcessItemsBounds();
        }
        /// <summary>
        /// Metoda vytvoří a vrátí pole vodících linek <see cref="FlowGuideLine"/>
        /// </summary>
        /// <returns></returns>
        public FlowGuideLine[] CreateGuideLines()
        {
            return _CreateGuideLines();
        }
        /// <summary>
        /// Metoda vyhledá zdejší evidovaný Floated prvek (ten je definován dodaným jménem <paramref name="parentName"/>), a vrátí jeho souřadnice celé buňky (<see cref="CellMatrixInfo"/>).
        /// Volající typiky tuto souřadnici umístí do <see cref="IFlowLayoutItem.CellMatrix"/> jiného prvku, který může následně pozicovat v rámci daného prostoru.
        /// <para/>
        /// Tato metoda tedy musí být vyvolaná až po vložení všech FlowLayout prvků metodou <see cref="AddFlowItem(IFlowLayoutItem)"/>, a po zpracování celého Flow layoutu metodou <see cref="ProcessFlowItems"/>.
        /// <para/>
        /// Typické použití: kostru Panelu v rámci DataFormu tvoří Floated prvky (standardní plovoucí layoutu: buňky, sloupce, ColSpan, RowSpan, atd).
        /// Designer v rámci tohoto layoutu vyhradí určitou část (lze použít ColSpan i RowSpan) pro nějaký Fixed prvek: vloží tedy do layoutu <c>PlaceHolder</c> jako Floated prvek, 
        /// a následně může zvolené Fixed prvky umístit dovnitř vytvořeného prostoru (lze do jednoho <c>PlaceHolder</c> vkládat více prvků, typicky relativně k jeho velikosti).
        /// </summary>
        /// <param name="parentName"></param>
        public CellMatrixInfo SearchParentBounds(string parentName)
        {
            string key = DfTemplateLayout.GetItemKey(parentName);
            if (key != null && this.__ItemsDict.TryGetValue(key, out var parentItem)) return parentItem.CellMatrix;
            return null;
        }
        /// <summary>
        /// Resetuje hodnoty ve sloupcích a řádcích
        /// </summary>
        private void _ProcessReset()
        {
            __Columns.ForEach(c => c.Reset());
            __Rows.ForEach(r => r.Reset());
            __Items.ForEach(i => i.ResetFlowFinalResults());
        }
        /// <summary>
        /// Zpracuje velikosti labelů jednotlivých itemů do odpovídajících řádků a sloupců
        /// </summary>
        private void _ProcessLabels()
        {
            // Pokud Label má definován offset (pro Top a Bottom pozice), a ten je záporný, značí to předsunutí labelu doleva od controlu.
            // Například __TopLabelOffsetX = -15  =>  Label v pozici Nad controlem bude nahoře, a bude začínat o 15px vlevo od začátku controlu.
            // Pokud ale současně mám __ControlMargins definující okraj 2px mezi labelem vlevo a controlem, pak o tyto 2px zmenším LabelOffset, 
            //  protože pozice Left Labelu se měří od okraje Left Controlu bez tohoto Margins.
            int shiftTopLabel = (this.__TopLabelOffsetX < 0 ? -this.__TopLabelOffsetX : 0);
            int shiftBottomLabel = (this.__BottomLabelOffsetX < 0 ? -this.__BottomLabelOffsetX : 0);
            int margin = this.__ControlMargins?.Left ?? 0;
            if (margin > 0)
            {   // Kladný 'shift' znamená předsunutí Labelu doleva před Control o daný počet pixelů:
                // Prostor pro LabelBefore budeme rezervovat menší, o Margin mezi Labelem a Controlem:
                shiftTopLabel -= margin;
                shiftBottomLabel -= margin;
            }

            // Všechny prvky zpracují svoje labely:
            __Items.ForEach(i => processLabel(i));

            // Zpracuje labely daného prvku do sloupců a řádků, kam patří
            void processLabel(IFlowLayoutItem item)
            {
                // MainLabel
                bool skipSuffix = false;
                if (item.MainLabelExists)
                {
                    var labelPosition = item.LabelPosition;
                    switch (item.LabelPosition)
                    {
                        case LabelPositionType.BeforeLeft:
                        case LabelPositionType.BeforeRight:
                            processLabelSize(item, __Columns, item.FlowColBeginIndex, item.DesignLabelWidth ?? item.ImplicitMainLabelWidth, true);
                            break;

                        case LabelPositionType.Top:
                            processLabelSize(item, __Rows, item.FlowRowBeginIndex, item.ImplicitMainLabelHeight, true);
                            if (shiftTopLabel > 0)
                                // Máme Label v pozici Top, a jeho X-offset je záporný (tedy Label je nad Controlem, a začíná o něco vlevo).
                                // Musím zajistit, aby v prostoru LabelBefore (vlevo od controlu) byl dostatek místa pro tento offset:
                                processLabelSize(item, __Columns, item.FlowColBeginIndex, shiftTopLabel, true);
                            break;
                        case LabelPositionType.Bottom:
                            processLabelSize(item, __Rows, item.FlowRowEndIndex, item.ImplicitMainLabelHeight, false);
                            if (shiftBottomLabel > 0)
                                // Máme Label v pozici Bottom, a jeho X-offset je záporný (tedy Label je pod Controlem, a začíná o něco vlevo).
                                // Musím zajistit, aby v prostoru LabelBefore (vlevo od controlu) byl dostatek místa pro tento offset:
                                processLabelSize(item, __Columns, item.FlowColBeginIndex, shiftBottomLabel, true);
                            break;

                        case LabelPositionType.After:
                            processLabelSize(item, __Columns, item.FlowColEndIndex, item.DesignLabelWidth ?? item.ImplicitMainLabelWidth, false);
                            skipSuffix = true;
                            break;
                    }
                }

                // SuffixLabel
                if (item.SuffixLabelExists && !skipSuffix)
                {
                    processLabelSize(item, __Columns, item.FlowColEndIndex, item.ImplicitSuffixLabelWidth, false);
                }
            }
            // Zpracuje jeden label (do sloupce / do řádku, v dané velikosti, do prostoru Before / After)
            void processLabelSize(IFlowLayoutItem item, List<LineInfo> lines, int? lineIndex, int? size, bool isBefore)
            {
                if (size.HasValue && size.Value > 0)
                {
                    var line = lines[lineIndex.Value];
                    if (isBefore)
                        line.LabelBeforeMaximalSize = size;
                    else
                        line.LabelAfterMaximalSize = size;
                }
            }
        }
        /// <summary>
        /// Zpracuje Max šířku controlu pro sloupce (z prvků v tom sloupci přítomných): 
        ///  nastřádá Max šířku Controlu v daném sloupci z prvků, které mají ColSpan = 1.<br/>
        /// Obdobně zpracuje řádky:
        ///  nastřádá Max výšku Controlu každého řádku z jednotlivých Itemů v tomto řádku, které mají RowSpan = 1.
        /// </summary>
        private void _ProcessControlSingleSpan()
        {
            // Projdu každý sloupec a každý řádek, najdu jeho vhodné prvky (které mají Span v dané ose == 1) a zaeviduji Max() jejich rozměru v daném směru:
            int colCnt = _ColumnsCount;
            int rowCnt = _RowsCount;
            for (int c = 0; c < colCnt; c++)
                processColumn(c);
            for (int r = 0; r < rowCnt; r++)
                processRow(r);

            // Zpracuje jeden Column
            void processColumn(int colIdx)
            {
                var column = __Columns[colIdx];
                for (int r = 0; r < rowCnt; r++)
                {
                    var item = __Cells[r][colIdx];
                    if (item != null)
                        processColumnItem(column, item);
                }
            }
            // Zpracuje jeden Item, pouze Control, z hlediska jeho sloupce, ColSpan = 1, nikoliv šířka v procentech:
            void processColumnItem(LineInfo column, IFlowLayoutItem item)
            {
                column.ContainsItem = true;
                if (item.FlowColBeginIndex == item.FlowColEndIndex && item.FlowColBeginIndex == column.Index && !item.DesignWidthPercent.HasValue) 
                {
                    column.ControlBoundsMaximalSize = item.DesignWidthPixel;
                    column.ControlImplicitMaximalSize = _GetMax(item.ImplicitControlMinimalWidth, item.ImplicitControlOptimalWidth);
                    item.AcceptedWidth = true;
                }
            }
            // Zpracuje jeden Row
            void processRow(int rowIdx)
            {
                var row = __Rows[rowIdx];
                for (int c = 0; c < colCnt; c++)
                {
                    var item = __Cells[rowIdx][c];
                    if (item != null)
                        processRowItem(row, item);
                }
            }
            // Zpracuje jeden Item, pouze Control, z hlediska jeho řádku, RowSpan = 1, nikoliv výška v procentech:
            void processRowItem(LineInfo row, IFlowLayoutItem item)
            {
                row.ContainsItem = true;
                if (item.FlowRowBeginIndex == item.FlowRowEndIndex && item.FlowRowBeginIndex == row.Index && !item.DesignHeightPercent.HasValue)
                {
                    row.ControlBoundsMaximalSize = item.DesignHeightPixel;
                    row.ControlImplicitMaximalSize = _GetMax(item.ImplicitControlMinimalHeight, item.ImplicitControlOptimalHeight);
                    item.AcceptedHeight = true;
                }
            }
        }
        /// <summary>
        /// Zpracuje Max šířku controlu pro sloupce (z prvků v tom sloupci přítomných): 
        ///  nastřádá Max šířku Controlu v daném sloupci z prvků, které mají ColSpan větší než 1.<br/>
        /// Obdobně zpracuje řádky:
        ///  nastřádá Max výšku Controlu každého řádku z jednotlivých Itemů v tomto řádku, které mají RowSpan větší než 1.
        /// </summary>
        private void _ProcessControlMultiSpan()
        {
            // Sečtu počet prvků Items, které nemají akceptovánu šířku a výšku. Pokud je jich nula, není co řešit a končím;
            var processWidthItems = __Items.Where(i => needProcessWidth(i)).ToList();
            var processHeightItems = __Items.Where(i => needProcessHeight(i)).ToList();
            if (processWidthItems.Count == 0 && processHeightItems.Count == 0) return;

            processWidthItems.Sort(compareX);
            processHeightItems.Sort(compareY);

            // Dál jdu ve "vlnách" 'q' (počínaje 0 a ++), kde "jedna vlna" řeší takové prvky, 
            //   které v dané ose (X/Y) mají právě tolik svých dimenzí, které dosud nemají určenou šířku/výšku typu Bounds nebo Implicit;
            // V rámci této akce daný prvek porovná svoji velikost proti velikosti z jemu odpovídajících dimenzí (sloupce / řádky) pro Control,
            // a případně si prvek zajistí potřebný prostor.
            int q = 0;
            for (int t = 0; t < 120; t++)
            {   // t je pouze timeout bez dalšího významu. Klíčový význam má hodnota 'q'.

                // Řeším prvky, které dosud nemají akceptovaný rozměr v některé ose (v poli processWidthItems a processHeightItems):
                bool isAcceptedWidth = false;
                for (int w = 0; w < processWidthItems.Count; w++)
                {
                    if (processItemWidth(processWidthItems[w], q))
                    {   // Pro tento prvek se podařilo zajistit dostatečnou šířku v odpovídajících sloupcích:
                        processWidthItems.RemoveAt(w);
                        w--;
                        isAcceptedWidth = true;
                    }
                }


                bool isAcceptedHeight = false;
                for (int h = 0; h < processHeightItems.Count; h++)
                {
                    if (processItemHeight(processHeightItems[h], q))
                    {   // Pro tento prvek se podařilo zajistit dostatečnou výšku v odpovídajících řádcích:
                        processHeightItems.RemoveAt(h);
                        h--;
                        isAcceptedHeight = true;
                    }
                }

                //  Jak to dopadlo:
                
                // Všechno je vyřešeno? Končíme:
                if (processWidthItems.Count == 0 && processHeightItems.Count == 0) return;

                // Něco se podařilo vyřešit? Dáme si to ještě jednou - se stejnou hladinou 'q', třeba vyřešíme i další prvek, protože se opře o nyní vypočtenou dimenzi:
                // Nebo: Ani jeden prvek není vyřešen? Pak navýšíme q a pojedeme to ještě jednou, tentokrát budeme řešit více nedefinovaných dimenzí pro jeden prvek:
                if (!(isAcceptedWidth || isAcceptedHeight))
                    q++;
            }

            // Zpracuje jeden prvek z pohledu šířky
            bool processItemWidth(IFlowLayoutItem item, int qWave)
            {
                if (!needProcessWidth(item)) return false;                     // Není nutno

                int begin = item.FlowColBeginIndex.Value;
                int end = item.FlowColEndIndex.Value;

                // Hodnota sizeType má vliv na to, zda vůbec budu hledat SizeSum (pokud já nemám rozměr Design nebo Implicit, tak hledat nemusím), a kam ji pak ukládat.
                int? implicitWidth = _GetMax(item.ImplicitControlMinimalWidth, item.ImplicitControlOptimalWidth);
                LineInfo.ControlSizeType sizeType = (item.DesignWidthPixel.HasValue ? LineInfo.ControlSizeType.MaxBounds : implicitWidth.HasValue ? LineInfo.ControlSizeType.MaxImplicit : LineInfo.ControlSizeType.None);
                if (sizeType == LineInfo.ControlSizeType.None) return false;   // Není nutno

                // Určovat hodnotu controlSizeSum budu v režimu BoundsOrImplicit.
                var sumType = LineInfo.ControlSizeType.BoundsOrImplicit;
                var controlSizeSum = _GetControlSizeSum(__Columns, begin, end, sumType, qWave, out var lastColumn, out var notColumns);
                if (lastColumn is null || !controlSizeSum.HasValue) return false;                    // V dané vlně v dimenzích (sloupce) tohoto prvku nenajdu takovou dimenzi, do které bych vložil jeho rozměr...

                // Obousměrné zpracování (Columns i Rows):
                int sizeItem = item.DesignWidthPixel ?? implicitWidth ?? 0;
                processItemAny(sizeItem, controlSizeSum.Value, sizeType, sumType, lastColumn, notColumns);
                item.AcceptedWidth = true;

                return true;
            }
            // Zpracuje jeden prvek z pohledu výšky
            bool processItemHeight(IFlowLayoutItem item, int qWave)
            {
                if (!needProcessHeight(item)) return false;                    // Není nutno

                int begin = item.FlowRowBeginIndex.Value;
                int end = item.FlowRowEndIndex.Value;

                // Hodnota sizeType má vliv na to, zda vůbec budu hledat SizeSum (pokud já nemám rozměr Design nebo Implicit, tak hledat nemusím), a kam ji pak ukládat.
                int? implicitHeight = _GetMax(item.ImplicitControlMinimalHeight, item.ImplicitControlOptimalHeight);
                LineInfo.ControlSizeType sizeType = (item.DesignHeightPixel.HasValue ? LineInfo.ControlSizeType.MaxBounds : implicitHeight.HasValue ? LineInfo.ControlSizeType.MaxImplicit : LineInfo.ControlSizeType.None);
                if (sizeType == LineInfo.ControlSizeType.None) return false;   // Není nutno
                
                // Určovat hodnotu controlSizeSum budu v režimu BoundsOrImplicit.
                var sumType = LineInfo.ControlSizeType.BoundsOrImplicit;
                var controlSizeSum = _GetControlSizeSum(__Rows, begin, end, sumType, qWave, out var lastRow, out var notRows);
                if (!controlSizeSum.HasValue) return false;                    // V dané vlně v dimenzích (řádky) tohoto prvku nenajdu takovou dimenzi, do které bych vložil jeho rozměr...

                // Obousměrné zpracování (Columns i Rows):
                int sizeItem = item.DesignHeightPixel ?? implicitHeight ?? 0;
                processItemAny(sizeItem, controlSizeSum.Value, sizeType, sumType, lastRow, notRows);
                item.AcceptedHeight = true;

                return true;
            }
            // Zpracuje jeden prvek = zajistí přidání velikosti do potřebné dimenze
            void processItemAny(int itemControlSize, int linesControlSumSize, LineInfo.ControlSizeType sizeType, LineInfo.ControlSizeType sumType, LineInfo lastLine, List<LineInfo> notLines)
            {
                // Mám šířku / výšku jednoho konkrétního controlu 'itemControlSize'. 
                // Mám součet dimenzí (šířky sloupců nebo výšky řádků pro Control) = 'linesControlSumSize'.
                // Máme i informace o dimenzích:
                //  poslední dimenze (sloupec / řádek) pro konkrétní prvek = 'lastLine';
                //  dimenze (sloupce / řádky) bez určeného rozměru 'notLines'.
                int sizeAdd = itemControlSize - linesControlSumSize;
                if (sizeAdd > 0)
                {   // Pokud náš prvek Control (sizeItem) je širší/vyšší, než prostor pro Controly v odpovídajících sloupcích/řádcích (linesControlSumSize),
                    //  máme tedy kladnou hodnotu 'sizeAdd', kterou musíme do některého sloupce/sloupců/řádku/řádků přidat, aby se náš prvek správně vešel.
                    int notCount = notLines?.Count ?? 0;
                    if (notCount == 0)
                    {   //  - pokud notLines je null nebo prázdné (=> všechny sloupce/řádky mají definovanou velikost), 
                        //     pak máme v lastLine ten (poslední) sloupec/řádek, kam případně navýšíme jeho rozměr kvůli našemu prvku = aby se vešel. 
                        //     Velikost této dimenze lastLine je v linesControlSumSize již započtena.
                        // Všechny dimenze (=sloupce/řádky) našeho prvku mají nějak určen rozměr (=šířku/výšku) controlu v režimu BoundsOrImplicit.
                        // A protože výsledný sumární prostor pro control je menší, než náš prvek potřebuje, tak zvětšíme poslední dimenzi:
                        int sizeLast = lastLine.GetControlSize(sumType).Value + sizeAdd;
                        if (sizeType == LineInfo.ControlSizeType.MaxBounds)
                            lastLine.ControlBoundsMaximalSize = sizeLast;
                        else
                            lastLine.ControlImplicitMaximalSize = sizeLast;
                    }
                    else
                    {   // Našli jsme nějaké dimenze (sloupce/řádky) bez velikosti (jsou v notLines); 
                        //  a víme, jak velký prostor pro náš prvek ještě potřebujeme alokovat ve sloupcích/řádcích (sizeAdd).
                        // (Dimenze přítomné v notLines nemají dosud určenou šířku/výšku, proto nebudu jejich rozměr zjišťovat).
                        //  Rovnoměrně rozpočítám velikost 'sizeAdd' do počtu sloupců 'notLines' a tento podíl do nich jednotlivě vložím:
                        //  Poznámka: případné labely okolo controlů a mezery mezi sloupci/řádky jsou v 'linesControlSumSize' započteny, a to i pro dimenze 'notLines'.
                        var sizes = getSizes(sizeAdd, notCount);
                        for (int ni = 0; ni < notCount; ni++)
                        {
                            var notLine = notLines[ni];
                            if (sizeType == LineInfo.ControlSizeType.MaxBounds)
                                notLine.ControlBoundsMaximalSize = sizes[ni];
                            else
                                notLine.ControlImplicitMaximalSize = sizes[ni];
                        }
                    }
                }
            }
            // Vrátí true pro prvek, který je třeba pracovat z hlediska šířky: má definovanou šířku (DesignWidthPixel nebo ImplicitControlMinimalWidth nebo ImplicitControlOptimalWidth), ale nemá ji zpracovanou (AcceptedWidth)
            bool needProcessWidth(IFlowLayoutItem item)
            {
                return ((item.DesignWidthPixel.HasValue || item.ImplicitControlMinimalWidth.HasValue || item.ImplicitControlOptimalWidth.HasValue) && !item.AcceptedWidth);
            }
            // Vrátí true pro prvek, který je třeba pracovat z hlediska výšky: má definovanou výšku (DesignHeightPixel nebo ImplicitControlMinimalHeight nebo ImplicitControlOptimalHeight), ale nemá ji zpracovanou (AcceptedHeight)
            bool needProcessHeight(IFlowLayoutItem item)
            {
                return ((item.DesignHeightPixel.HasValue || item.ImplicitControlMinimalHeight.HasValue || item.ImplicitControlOptimalHeight.HasValue) && !item.AcceptedHeight);
            }
            // Porovná dva prvky IFlowLayoutItem podle hodnoty FlowColEndIndex ASC = prvky zleva
            int compareX(IFlowLayoutItem a, IFlowLayoutItem b)
            {
                int ax = a.FlowColEndIndex ?? 0;
                int bx = b.FlowColEndIndex ?? 0;
                return ax.CompareTo(bx);
            }
            // Porovná dva prvky IFlowLayoutItem podle hodnoty FlowRowEndIndex ASC = prvky shora
            int compareY(IFlowLayoutItem a, IFlowLayoutItem b)
            {
                int ay = a.FlowRowEndIndex ?? 0;
                int by = b.FlowRowEndIndex ?? 0;
                return ay.CompareTo(by);
            }
            // Metoda vrátí pole, obsahující danou velikost 'sizeSum' rozdělenou na daný počet podílů 'count', včetně správného zaokrouhlování.
            int[] getSizes(int sizeSum, int count)
            {
                if (count <= 0) throw new ArgumentException($"DfFlowLayoutInfo._ProcessControlMultiSpan.getSize() fail: count = '{count}'");

                var positions = new int[count];
                var total = (double)sizeSum;
                var divider = (double)count;
                for (int p = 1; p <= count; p++)                                              // Příklady pro sizeSum = 217 a count = 3
                {
                    double position = (p < count) ? (total * (double)p / divider) : total;    // Float pozice: 72.333333; 144.666667; 217
                    positions[p - 1] = (int)Math.Round(position, 0);                          // Int zaokrouhlené pozice: 72; 145; 217
                }

                var sizes = new int[count];
                int lastP = 0;                             // Minulá pozice
                for (int p = 0; p < count; p++)
                {
                    int position = positions[p];           // Postupně projdu pozice 72; 145; 217
                    sizes[p] = position - lastP;           // Určím vzdálenost aktuální pozice (position) od minulé (lastP): 72; 73; 72
                    lastP = position;                      // Výchozí pozice pro další krok
                }

                return sizes;
            }
        }
        /// <summary>
        /// Do jednotlivých sloupců vloží jejich konkrétní souřadnici na základě výchozí souřadnice dané jako <see cref="__FlowBounds"/>.
        /// Současně s tím určí výslednou souřadnici konce prostoru FlowLayoutu, a tu vloží do <see cref="__FlowBounds"/>.<br/>
        /// Tato metoda neřeší souřadnice jednotlivých prvků, to řeší <see cref="_ProcessItemsBounds"/>.
        /// </summary>
        private void _ProcessDimensions()
        {
            int left = this.__FlowBounds.Left ?? 0;
            int top = this.__FlowBounds.Top ?? 0;

            var right = processDimension(__Columns, left);
            var bottom = processDimension(__Rows, top);

            this.__FlowBounds.Fill(left, top, right - left, bottom - top);
            this.__FlowLayoutBounds = new ControlBounds(left, top, right - left, bottom - top);

            // Zpracuje dodanou serii dimenzí (Column / Row): postupně do dimenzí vepíše dodaný begin a inkrementuje jej do další dimenze...
            int processDimension(List<LineInfo> lines, int begin)
            {
                int position = begin;
                int last = lines.Count - 1;
                for (int l = 0; l <= last; l++)
                    position = lines[l].Finish(position, (l == last));
                return position;
            }
        }
        /// <summary>
        /// Do jednotlivých prvků vloží jejich konkrétní souřadnici na základě souřadnic jejich dimenzí (sloupce, řádky).
        /// </summary>
        private void _ProcessItemsBounds()
        {
            // Zpracuje všechny prvky:
            foreach (var i in __Items)
            {
                i.CellMatrix = new CellMatrixInfo(__Columns[i.FlowColBeginIndex.Value], __Columns[i.FlowColEndIndex.Value], __Rows[i.FlowRowBeginIndex.Value], __Rows[i.FlowRowEndIndex.Value]);
                _ProcessInnerItemBounds(i, __LabelsRelativeToControl, __TopLabelOffsetX, __BottomLabelOffsetX);
            }
        }
        /// <summary>
        /// Metoda vytvoří a vrátí pole vodících linek <see cref="FlowGuideLine"/>
        /// </summary>
        /// <returns></returns>
        private FlowGuideLine[] _CreateGuideLines()
        {
            List<FlowGuideLine> guideLines = new List<FlowGuideLine>();
            addLines(__Columns);
            addLines(__Rows);
            guideLines.Sort(FlowGuideLine.CompareByLineType);
            return guideLines.ToArray();

            void addLines(List<LineInfo> lines)
            {
                int prevEnd = -1;
                int last = lines.Count - 1;
                for (int i = 0; i <= last; i++)
                {
                    var line = lines[i];
                    var axis = line.Axis;

                    int cellBegin = line.CurrentBegin.Value;
                    int labelBeforeBegin = line.LabelBeforeBegin.Value;
                    int labelBeforeEnd = line.LabelBeforeEnd.Value - 1;
                    int controlBegin = line.ControlBegin.Value;
                    int controlEnd = line.ControlEnd.Value - 1;
                    int labelAfterBegin = line.LabelAfterBegin.Value;
                    int labelAfterEnd = line.LabelAfterEnd.Value - 1;
                    int cellEnd = line.CurrentEnd.Value - 1;

                    if (cellBegin > prevEnd)
                        guideLines.Add(new FlowGuideLine(cellBegin, axis, GuideLineType.CellBegin));

                    if (labelBeforeBegin > cellBegin && labelBeforeBegin < controlBegin)
                        guideLines.Add(new FlowGuideLine(labelBeforeBegin, axis, GuideLineType.LabelBeforeBegin));

                    if (labelBeforeEnd > labelBeforeBegin && labelBeforeEnd < controlBegin)
                        guideLines.Add(new FlowGuideLine(labelBeforeEnd, axis, GuideLineType.LabelBeforeEnd));

                    if (controlBegin > cellBegin)
                        guideLines.Add(new FlowGuideLine(controlBegin, axis, GuideLineType.ControlBegin));

                    if (controlEnd > controlBegin && controlEnd < cellEnd)
                        guideLines.Add(new FlowGuideLine(controlEnd, axis, GuideLineType.ControlEnd));

                    if (labelAfterBegin > controlEnd && labelAfterBegin < cellEnd)
                        guideLines.Add(new FlowGuideLine(labelAfterBegin, axis, GuideLineType.LabelAfterBegin));

                    if (labelAfterEnd > labelAfterBegin && labelAfterEnd < cellEnd)
                        guideLines.Add(new FlowGuideLine(labelAfterEnd, axis, GuideLineType.LabelAfterEnd));

                    if (cellEnd > cellBegin)
                        guideLines.Add(new FlowGuideLine(cellEnd, axis, GuideLineType.CellEnd));

                    prevEnd = cellEnd;
                }
            }
        }
        /// <summary>
        /// Metoda z prvků v daném směru (parametr <paramref name="lastLine"/>: Columns nebo Rows) sečte a vrátí aktuální velikost pro control v daném rozmezí (begin až end).
        /// Pokud je zadaný rozsah <paramref name="begin"/> až <paramref name="end"/> větší (zahrnuje více dimenzí), pak výsledná suma zahrnuje nejen velikost pro Control,
        /// ale i velikosti <see cref="LineInfo.LabelAfterSize"/> a <see cref="LineInfo.DistanceAfterSize"/>, a z další dimenze <see cref="LineInfo.LabelBeforeSize"/>.
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="sizeType"></param>
        /// <param name="maxUndefinedCount"></param>
        /// <param name="lastLine"></param>
        /// <param name="foundUndefinedLines"></param>
        /// <returns></returns>
        private int? _GetControlSizeSum(List<LineInfo> lines, int begin, int end, LineInfo.ControlSizeType sizeType, int maxUndefinedCount, out LineInfo lastLine, out List<LineInfo> foundUndefinedLines)
        {
            // Významy velikostí v dimenzi, jejich MultiSpan a to, co z dimenze sčítáme do výsledného Controlu:
            // Control, který je MultiSpan, do sebe zahrnuje tyto velikosti z dimenzí, na kterých se nachází:
            //   Z ne-první dimenze            : prostor LabelBefore
            //   Z každé dimenze               : prostor Control
            //   Z ne-poslední dimenze         : prostor LabelAfter + DistanceAfter
            //  +------------------ Dimension Begin -------------------+------------------ Dimension Inner -------------------+------------------- Dimension End  -------------------+
            //  |  LabelBefore    Control   LabelAfter  DistanceAfter  |  LabelBefore    Control   LabelAfter  DistanceAfter  |  LabelBefore    Control   LabelAfter  DistanceAfter  |
            //  |                |-------------------------------------------------- Size 3x MultiSpan ------------------------------------------------|                             |
            //  |                |---------------------- Size 2x MultiSpan ---------------------|                                                                                    |
            //  |                                                                       |---------------------- Size 2x MultiSpan ---------------------|                             |
            //  |                |-------|                                                                                                                                           |
            //  +--------------------------------------------------------------------------------------------------------------------------------------------------------------------+

            lastLine = null;
            foundUndefinedLines = null;
            int size = 0;
            int undefinedCount = 0;
            for (int i = begin; i <= end; i++)
            {
                var line = lines[i];
                lastLine = line;

                if (i > begin) size += line.LabelBeforeSize + line.DistanceBeforeControl;      // Prostor před Controlem zahrnujeme počínaje od druhé dimenze

                var controlSize = line.GetControlSize(sizeType);      // Samotný Control beru vždy, v daném typu (Bounds / Implicit)
                if (controlSize.HasValue)
                {
                    size += controlSize.Value;
                }
                else
                {   // Aktuální dimenze (line) nemá určen rozměr požadovaného typu! Co s tím?
                    undefinedCount++;                                 // Počítám, kolik nedefinovaných dimenzí pokrývá aktuální prvek
                    if (undefinedCount > maxUndefinedCount)           // Pokud přesáhnu povolený počet, skončím a vrátím null:
                        return null;
                    // Nějaké nedefinované dimenze jsou povoleny, budu je střádat do výsledku:
                    if (foundUndefinedLines is null)
                        foundUndefinedLines = new List<LineInfo>();
                    foundUndefinedLines.Add(line);
                }

                if (i < end) size += line.DistanceAfterControl + line.LabelAfterSize + line.DistanceAfterSize;   // Prostor za Controlem zahrnujeme do dimenzí před tou poslední
            }
            return size;
        }
        /// <summary>
        /// Vrátí Max() z dodaných hodnot; řeší i NULL hodnoty.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        private static int? _GetMax(int? value1, int? value2)
        {
            bool has1 = value1.HasValue;
            bool has2 = value2.HasValue;
            if (has1 && has2) return (value1.Value > value2.Value ? value1.Value : value2.Value);
            if (has1) return value1;
            if (has2) return value2;
            return null;
        }
        /// <summary>
        /// Finální souřadnice prostoru FlowLayout
        /// </summary>
        public ControlBounds FlowLayoutBounds { get { return __FlowLayoutBounds; } } private ControlBounds __FlowLayoutBounds;
        // Proměnné...
        private int __CurrentRowIndex;
        private int __CurrentColIndex;
        #endregion
        #region Statické helpery pro umisťování souřadnic prvku
        /// <summary>
        /// Metoda určí a uloží do prvku <see cref="IFlowLayoutItem"/> <paramref name="item"/> souřadnice pro MainLabel, pro Control a pro SuffixLabel podle standardních pravidel.
        /// Metoda je používána pro běžné prvky FlowLayoutu, a i pro prvky, které mají Fixed layout, ale chtějí se částečně umístit do okolního FlowLayoutu pomocí Parenta = <c>PlaceHolder</c>.
        /// </summary>
        /// <param name="item">Prvek, do kterého chceme vypočítat souřadnice</param>
        /// <param name="layoutStyle">Definice stylu obsahuje v sobě potřebné hodnoty</param>
        public static void ProcessInnerItemBounds(IFlowLayoutItem item, DfTemplateLayout.StyleInfo layoutStyle)
        {
            if (item is null) throw new ArgumentNullException($"DfFlowLayoutInfo.ProcessInnerItemBounds fail: item is null.");
            if (item.CellMatrix is null) throw new ArgumentNullException($"DfFlowLayoutInfo.ProcessInnerItemBounds fail: item.CellBounds is null.");
            if (layoutStyle is null) throw new ArgumentNullException($"DfFlowLayoutInfo.ProcessInnerItemBounds fail: layoutStyle is null.");

            bool labelsRelativeToControl = true;
            int topLabelOffsetX = layoutStyle.TopLabelOffsetX;
            int bottomLabelOffsetX = layoutStyle.BottomLabelOffsetX;

            _ProcessInnerItemBounds(item, labelsRelativeToControl, topLabelOffsetX, bottomLabelOffsetX);
        }
        /// <summary>
        /// Metoda určí a uloží do prvku <see cref="IFlowLayoutItem"/> <paramref name="item"/> souřadnice pro MainLabel, pro Control a pro SuffixLabel podle standardních pravidel.
        /// Metoda je používána pro běžné prvky FlowLayoutu, a i pro prvky, které mají Fixed layout, ale chtějí se částečně umístit do okolního FlowLayoutu pomocí Parenta = <c>PlaceHolder</c>.
        /// </summary>
        /// <param name="item">Prvek, do kterého chceme vypočítat souřadnice</param>
        /// <param name="labelsRelativeToControl">Příznak, že labely chceme umísťovat přiměřeně k prostoru controlu (true) / exaktně do mřížky okolních prvků (false)</param>
        /// <param name="topLabelOffsetX">Offset X pro labely umístěné Top</param>
        /// <param name="bottomLabelOffsetX">Offset X pro labely umístěné Bottom</param>
        public static void ProcessInnerItemBounds(IFlowLayoutItem item, bool labelsRelativeToControl, int topLabelOffsetX, int bottomLabelOffsetX)
        {
            if (item is null) throw new ArgumentNullException($"DfFlowLayoutInfo.ProcessInnerItemBounds fail: item is null.");
            if (item.CellMatrix is null) throw new ArgumentNullException($"DfFlowLayoutInfo.ProcessInnerItemBounds fail: item.CellBounds is null.");

            _ProcessInnerItemBounds(item, labelsRelativeToControl, topLabelOffsetX, bottomLabelOffsetX);
        }
        /// <summary>
        /// Metoda určí a uloží do prvku <see cref="IFlowLayoutItem"/> <paramref name="item"/> souřadnice pro MainLabel, pro Control a pro SuffixLabel podle dodané souřadnice pro Control.
        /// </summary>
        /// <param name="item">Prvek, do kterého chceme vypočítat souřadnice</param>
        /// <param name="controlBounds">Oblast pro Control</param>
        /// <param name="layoutStyle">Definice stylu obsahuje v sobě potřebné hodnoty</param>
        public static void ProcessFixedItemBounds(IFlowLayoutItem item, ControlBounds controlBounds, DfTemplateLayout.StyleInfo layoutStyle)
        {
            if (item is null) throw new ArgumentNullException($"DfFlowLayoutInfo.ProcessFixedItemBounds fail: item is null.");
            if (controlBounds is null) throw new ArgumentNullException($"DfFlowLayoutInfo.ProcessFixedItemBounds fail: controlBounds is null.");
            if (layoutStyle is null) throw new ArgumentNullException($"DfFlowLayoutInfo.ProcessFixedItemBounds fail: layoutStyle is null.");
            int topLabelOffsetX = layoutStyle.TopLabelOffsetX;
            int bottomLabelOffsetX = layoutStyle.BottomLabelOffsetX;
            var margins = layoutStyle.ControlMargins;
            _ProcessFixedItemBounds(item, controlBounds, topLabelOffsetX, bottomLabelOffsetX, margins);
        }
        /// <summary>
        /// Metoda určí a uloží do prvku <see cref="IFlowLayoutItem"/> <paramref name="item"/> souřadnice pro MainLabel, pro Control a pro SuffixLabel podle dodané souřadnice pro Control.
        /// </summary>
        /// <param name="item">Prvek, do kterého chceme vypočítat souřadnice</param>
        /// <param name="controlBounds">Oblast pro Control</param>
        /// <param name="topLabelOffsetX">Offset X pro labely umístěné Top</param>
        /// <param name="bottomLabelOffsetX">Offset X pro labely umístěné Bottom</param>
        /// <param name="margins">Okraje kolem controlu vzhledem k labelům</param>
        public static void ProcessFixedItemBounds(IFlowLayoutItem item, ControlBounds controlBounds, int topLabelOffsetX, int bottomLabelOffsetX, Margins margins)
        {
            if (item is null) throw new ArgumentNullException($"DfFlowLayoutInfo.ProcessFixedItemBounds fail: item is null.");
            if (controlBounds is null) throw new ArgumentNullException($"DfFlowLayoutInfo.ProcessFixedItemBounds fail: controlBounds is null.");
            _ProcessFixedItemBounds(item, controlBounds, topLabelOffsetX, bottomLabelOffsetX, margins);
        }
        /// <summary>
        /// Metoda určí a uloží do prvku <see cref="IFlowLayoutItem"/> <paramref name="item"/> souřadnice pro MainLabel, pro Control a pro SuffixLabel podle standardních pravidel.
        /// Metoda je používána pro běžné prvky FlowLayoutu, a i pro prvky, které mají Fixed layout, ale chtějí se částečně umístit do okolního FlowLayoutu pomocí Parenta = <c>PlaceHolder</c>.
        /// </summary>
        /// <param name="item">Prvek, do kterého chceme vypočítat souřadnice</param>
        /// <param name="labelsRelativeToControl">Příznak, že labely chceme umísťovat přiměřeně k prostoru controlu (true) / exaktně do mřížky okolních prvků (false)</param>
        /// <param name="topLabelOffsetX">Offset X pro labely umístěné Top</param>
        /// <param name="bottomLabelOffsetX">Offset X pro labely umístěné Bottom</param>
        private static void _ProcessInnerItemBounds(IFlowLayoutItem item, bool labelsRelativeToControl, int topLabelOffsetX, int bottomLabelOffsetX)
        {
            // Sekvence:
            var mainLabelArea = getMainLabelArea();
            var suffixLabelArea = getSuffixLabelArea(mainLabelArea);
            var controlBounds = processItemControl(mainLabelArea | suffixLabelArea);
            if (mainLabelArea != UsedAreaType.None) processItemMainLabel(controlBounds);
            if (suffixLabelArea != UsedAreaType.None) processItemSuffixLabel(controlBounds);
            // hotovo.


            // Metoda zjistí, která oblast buňky bude obsazena Main Labelem.
            UsedAreaType getMainLabelArea()
            {   // Pokud MainLabel existuje, tak podle jeho pozice:
                var labelPos = item.MainLabelExists ? item.LabelPosition : LabelPositionType.None;
                switch (labelPos)
                {
                    case LabelPositionType.BeforeLeft:
                    case LabelPositionType.BeforeRight: return UsedAreaType.Left;
                    case LabelPositionType.After: return UsedAreaType.Right;
                    case LabelPositionType.Top: return UsedAreaType.Top;
                    case LabelPositionType.Bottom: return UsedAreaType.Bottom;
                }
                return UsedAreaType.None;
            }
            // Metoda zjistí, která oblast buňky bude obsazena Suffix Labelem.
            UsedAreaType getSuffixLabelArea(UsedAreaType mainLabelArea)
            {   // Poud SuffixLabel existuje 
                return (item.SuffixLabelExists && mainLabelArea != UsedAreaType.Right ? UsedAreaType.Right : UsedAreaType.None);
            }
            // Zpracuje souřadnici pro Control
            ControlBounds processItemControl(UsedAreaType labelUsedAreas)
            {
                // Které prostory může Control využít? Bude expandovat do některých sousedních prostorů, pokud jsou volné?
                var itemExpand = item.DesignExpandControl ?? ExpandControlType.None;

                // Využít oblast ... = pokud prvek chce (itemExpand) a současně pokud label tuto oblast nepoužívá:
                bool expLeft = itemExpand.HasFlag(ExpandControlType.Left) && !labelUsedAreas.HasFlag(UsedAreaType.Left);
                bool expTop = itemExpand.HasFlag(ExpandControlType.Top) && !labelUsedAreas.HasFlag(UsedAreaType.Top);
                bool expRight = itemExpand.HasFlag(ExpandControlType.Right) && !labelUsedAreas.HasFlag(UsedAreaType.Right);
                bool expBottom = itemExpand.HasFlag(ExpandControlType.Bottom) && !labelUsedAreas.HasFlag(UsedAreaType.Bottom);

                // Souřadnice zvolené oblasti, určené pro umístění Controlu:
                var cellMatrix = item.CellMatrix;
                int l = expLeft ? correctExpandedControlLeftByOffset(cellMatrix.LeftLabelLeft) : cellMatrix.ControlLeft;
                int t = expTop ? cellMatrix.TopLabelTop : cellMatrix.ControlTop;
                int r = expRight ? cellMatrix.RightLabelRight : cellMatrix.ControlRight;
                int b = expBottom ? cellMatrix.BottomLabelBottom : cellMatrix.ControlBottom;
                int w = r - l;
                int h = b - t;

                // Control obsadí celou oblast? Nebo jen menší část? Jakou? A v jaké relativní pozici?
                // Poznámka: Implicitní velikost 'ImplicitControlMinimalWidth' a 'ImplicitControlMinimalHeight' je velikost "doporučená minimální".
                //    Proto slouží při výpočtech velikosti souřadnic (metody _ProcessControlSingleSpan a _ProcessControlMultiSpan).
                //    Ale nevepisuji ji do reálné šířky / výšky prvku.
                //    Pokud má prvek zadanou pouze 'ImplicitControlMinimalWidth', pak reálnou šířku má 100% sloupce/sloupců.
                // Jinak to je u hodnoty 'ImplicitControlOptimalWidth' a 'ImplicitControlOptimalHeight': 
                //    to je velikost "doporučená" - a má tedy charakter zadaného rozměru do Bounds.Width / Height.
                //    Tedy pokud je zadaná, pak ji prvek bude mít.

                // Jak bude prostor využit z hlediska zadaných dimenzí controlu
                // ControlWidth:
                int cw = w;
                if (item.DesignWidthPixel.HasValue) cw = item.DesignWidthPixel.Value;                                  // Designer zadal do frm.xml hodnotu 'Width="85"'
                else if (item.DesignWidthPercent.HasValue) cw = (w * item.DesignWidthPercent.Value / 100);             // Designer zadal do frm.xml hodnotu 'Width="50%"'
                else if (item.ImplicitControlOptimalWidth.HasValue) cw = item.ImplicitControlOptimalWidth.Value;       // Kód určil optimální šířku, které se držíme
                cw = (cw < 0 ? 0 : (cw > w ? w : cw));               // 'cw' zarovnat do mezí 0 až 'w'

                // ControlHeight:
                int ch = h;
                if (item.DesignHeightPixel.HasValue) ch = item.DesignHeightPixel.Value;                                // Designer zadal do frm.xml hodnotu 'Height="50"'
                else if (item.DesignHeightPercent.HasValue) ch = (h * item.DesignHeightPercent.Value / 100);           // Designer zadal do frm.xml hodnotu 'Height="100%"'
                else if (item.ImplicitControlOptimalHeight.HasValue) ch = item.ImplicitControlOptimalHeight.Value;     // Kód určil optimální výšku, které se držíme
                ch = (ch < 0 ? 0 : (ch > h ? h : ch));               // 'ch' zarovnat do mezí 0 až 'h'

                // Bude nějaké zarovnání controlu (pokud má control šířku menší je šířka prostoru, a podobně pro výšku)?
                int cl = l;
                if (cw < w)
                {   // Vodorovně na střed / doprava?
                    var hPos = item.DesignHPosition ?? HPositionType.Left;
                    cl = getControlBegin(l, w, cw, hPos == HPositionType.Center, hPos == HPositionType.Right);
                }
                int ct = t;
                if (ch < h)
                {   // Svisle na střed / dolů?
                    var vPos = item.DesignVPosition ?? VPositionType.Top;
                    ct = getControlBegin(t, h, ch, vPos == VPositionType.Center, vPos == VPositionType.Bottom);
                }

                var bounds = new ControlBounds(cl, ct, cw, ch);
                item.ControlBounds = bounds;
                return bounds;
            }
            // Vrátí pozici počátku Controlu, který bude zarovnán na střed nebo na konec prostoru total
            int getControlBegin(int begin, int totalSize, int controlSize, bool isCenter, bool isEnd)
            {
                int controlBegin = begin;
                if ((isCenter || isEnd) && (controlSize < totalSize))     // Někam posunout? ... a control je menší, než total prostor => takže posouvat je požadováno a možno:
                {
                    int space = (totalSize - controlSize);                // Posouvací volný prostor (uvnitř Total, po odečtení Control)
                    if (isCenter) space = space / 2;                      // Posunout na střed => jen o polovinu  (pokud ne, pak posunu o celé space => na konec = Right/Bottom)
                    controlBegin = begin + space;                         // Posunu begin pro control, doprava / dolů, o daný prostor.
                }
                return controlBegin;
            }
            // Koriguje souřadnici Control.Left v situaci, kdy je Expanded doleva, tedy Control začíná přímo na samé na souřadnici Cell.Left, 
            //  a současně pokud by byl MainLabel umístěný Top nebo Bottom a odpovídající TopLabelOffsetX / BottomLabelOffsetX by byl záporný,
            //  pak musíme posunout Left Controlu doprava tak, aby předsunutý MainLabel začínal na pozici Left = 0!
            int correctExpandedControlLeftByOffset(int left)
            {
                int shift = 0;
                var labelPos = item.MainLabelExists ? item.LabelPosition : LabelPositionType.None;
                switch (labelPos)
                {   // Pokud MainLabel bude Nahoře nebo Dole, pak musím vzít do úvahy i OffsetX pro tento Label:
                    case LabelPositionType.Top:
                        shift = -topLabelOffsetX;
                        break;
                    case LabelPositionType.Bottom:
                        shift = -bottomLabelOffsetX;
                        break;
                }
                // Pokud nalezený shift bude kladný (=záporný offset), pak to znamená, že Label bude předsunutý doleva o (shift) pixelů.
                // A protože tato metoda běží tehdy, když Control je expandovaný doleva (tedy vlevo až na začátek buňky), a má "před ním" být předsazený Label,
                //  pak Label bude umístěn na pozici 'left' (na začátku buňky) a Control bude posunut doprava o tento 'shift':
                return ((shift > 0) ? left + shift : left);
            }
            // Zpracuje souřadnici pro MainLabel
            void processItemMainLabel(ControlBounds controlBounds)
            {
                var cellMatrix = item.CellMatrix;
                bool relativeToControl = labelsRelativeToControl && controlBounds != null;
                if (relativeToControl && controlBounds is null) relativeToControl = false;
                int l, r, t, b;
                var labelPos = item.LabelPosition;
                switch (labelPos)
                {
                    case LabelPositionType.BeforeLeft:
                    case LabelPositionType.BeforeRight:
                        l = cellMatrix.LeftLabelLeft;
                        r = (relativeToControl ? controlBounds.Left - cellMatrix.MarginControlLeft : cellMatrix.LeftLabelRight);
                        t = cellMatrix.ControlTop;
                        b = cellMatrix.ControlFirstBottom;
                        item.MainLabelBounds = new ControlBounds(l, t, (r - l), (b - t));
                        item.MainLabelAlignment = (labelPos == LabelPositionType.BeforeLeft ? ContentAlignmentType.MiddleLeft : (labelPos == LabelPositionType.BeforeRight ? ContentAlignmentType.MiddleRight : ContentAlignmentType.MiddleCenter));
                        break;
                    case LabelPositionType.After:
                        l = (relativeToControl ? controlBounds.Right + cellMatrix.MarginControlRight : cellMatrix.RightLabelLeft);
                        r = cellMatrix.RightLabelRight;
                        t = cellMatrix.ControlTop;
                        b = cellMatrix.ControlFirstBottom;
                        item.MainLabelBounds = new ControlBounds(l, t, (r - l), (b - t));
                        item.MainLabelAlignment = ContentAlignmentType.MiddleLeft;
                        break;
                    case LabelPositionType.Top:
                        l = (relativeToControl ? controlBounds.Left : cellMatrix.ControlLeft) + topLabelOffsetX;
                        r = (relativeToControl ? controlBounds.Right : cellMatrix.ControlRight);
                        t = cellMatrix.TopLabelTop;
                        b = cellMatrix.TopLabelBottom;
                        if (item.DesignLabelWidth.HasValue && item.DesignLabelWidth.Value >= 0)
                            r = l + item.DesignLabelWidth.Value;
                        item.MainLabelBounds = new ControlBounds(l, t, (r - l), (b - t));
                        item.MainLabelAlignment = ContentAlignmentType.BottomLeft;
                        break;
                    case LabelPositionType.Bottom:
                        l = (relativeToControl ? controlBounds.Left : cellMatrix.ControlLeft) + bottomLabelOffsetX;
                        r = (relativeToControl ? controlBounds.Right : cellMatrix.ControlRight);
                        t = cellMatrix.BottomLabelTop;
                        b = cellMatrix.BottomLabelBottom;
                        if (item.DesignLabelWidth.HasValue && item.DesignLabelWidth.Value >= 0)
                            r = l + item.DesignLabelWidth.Value;
                        item.MainLabelBounds = new ControlBounds(l, t, (r - l), (b - t));
                        item.MainLabelAlignment = ContentAlignmentType.TopLeft;
                        break;
                }
            }
            // Zpracuje souřadnici pro SuffixLabel
            void processItemSuffixLabel(ControlBounds controlBounds)
            {
                bool relativeToControl = labelsRelativeToControl && controlBounds != null;
                var cellMatrix = item.CellMatrix;
                if (relativeToControl && controlBounds is null) relativeToControl = false;
                int l, r, t, b;
                l = (relativeToControl ? controlBounds.Right + cellMatrix.MarginControlRight : cellMatrix.RightLabelLeft);
                r = cellMatrix.RightLabelRight;
                t = cellMatrix.ControlTop;
                b = cellMatrix.ControlFirstBottom;
                item.SuffixLabelBounds = new ControlBounds(l, t, (r - l), (b - t));
                item.SuffixLabelAlignment = ContentAlignmentType.MiddleLeft;
            }
        }
        /// <summary>
        /// Metoda určí a uloží do prvku <see cref="IFlowLayoutItem"/> <paramref name="item"/> souřadnice pro MainLabel, pro Control a pro SuffixLabel podle dodané souřadnice pro Control.
        /// </summary>
        /// <param name="item">Prvek, do kterého chceme vypočítat souřadnice</param>
        /// <param name="controlBounds">Oblast pro Control</param>
        /// <param name="topLabelOffsetX">Offset X pro labely umístěné Top</param>
        /// <param name="bottomLabelOffsetX">Offset X pro labely umístěné Bottom</param>
        /// <param name="margins">Okraje kolem controlu vzhledem k labelům</param>
        private static void _ProcessFixedItemBounds(IFlowLayoutItem item, ControlBounds controlBounds, int topLabelOffsetX, int bottomLabelOffsetX, Margins margins)
        {
            if (item.ControlExists)
                item.ControlBounds = controlBounds;

            if (item.MainLabelExists)
                processMainLabelBounds();

            if (item.SuffixLabelExists)
                processSuffixLabelBounds();


            // Vyřeší souřadnici pro MainLabel
            void processMainLabelBounds()
            {
                if (controlBounds is null) return;
                var labelPos = item.LabelPosition;
                if (labelPos == LabelPositionType.None) return;
                item.MainLabelBounds = getLabelBounds(labelPos, item.DesignLabelWidth ?? item.ImplicitMainLabelWidth, item.ImplicitMainLabelHeight, out var labelAlignment);
                item.MainLabelAlignment = labelAlignment;
            }
            // Vyřeší souřadnici pro SuffixLabel
            void processSuffixLabelBounds()
            {
                if (controlBounds is null) return;
                var labelPos = item.LabelPosition;
                if (item.MainLabelExists && labelPos == LabelPositionType.After) return;           // Pokud existuje Main label a ten je After, tak tam neůže být Suffix Label
                item.SuffixLabelBounds = getLabelBounds(labelPos, item.ImplicitSuffixLabelWidth, item.ImplicitSuffixLabelHeight, out var labelAlignment);
            }
            // Určí a vrátí souřadnici pro dané umístění
            ControlBounds getLabelBounds(LabelPositionType labelPos, int? width, int? height, out ContentAlignmentType labelAlignment)
            {
                labelAlignment = ContentAlignmentType.Default;
                if (!width.HasValue || width.Value <= 0 || !height.HasValue || height.Value <= 0) return null;

                int l, r, t, b;
                switch (labelPos)
                {
                    case LabelPositionType.BeforeLeft:
                    case LabelPositionType.BeforeRight:
                        r = controlBounds.Left - margins.Left;
                        l = r - width.Value;
                        t = controlBounds.Top;
                        b = t + height.Value;
                        labelAlignment = (labelPos == LabelPositionType.BeforeLeft ? ContentAlignmentType.MiddleLeft : (labelPos == LabelPositionType.BeforeRight ? ContentAlignmentType.MiddleRight : ContentAlignmentType.MiddleCenter));
                        return new ControlBounds(l, t, (r - l), (b - t));
                    case LabelPositionType.After:
                        l = controlBounds.Right + margins.Right;
                        r = l + width.Value;
                        t = controlBounds.Top;
                        b = t + height.Value;
                        labelAlignment = ContentAlignmentType.MiddleLeft;
                        return new ControlBounds(l, t, (r - l), (b - t));
                    case LabelPositionType.Top:
                        l = controlBounds.Left + topLabelOffsetX;
                        r = controlBounds.Right;
                        b = controlBounds.Top - margins.Top;
                        t = b - height.Value;
                        labelAlignment = ContentAlignmentType.BottomLeft;
                        return new ControlBounds(l, t, (r - l), (b - t));
                    case LabelPositionType.Bottom:
                        l = controlBounds.Left + bottomLabelOffsetX;
                        r = controlBounds.Right;
                        t = controlBounds.Bottom + margins.Bottom;
                        b = t + height.Value;
                        labelAlignment = ContentAlignmentType.TopLeft;
                        return new ControlBounds(l, t, (r - l), (b - t));
                }
                return null;
            }
        }
        /// <summary>
        /// Oblasti v buňce, které jsou využity pro labely a pro control
        /// </summary>
        [Flags]
        private enum UsedAreaType
        {
            None = 0,
            Left = 0x01,
            Top = 0x02,
            Right = 0x04,
            Bottom = 0x08,
            Center = 0x10
        }
        #endregion
        #region Vizualizace FlowLayoutu (rastr buněk v ASCII grafice)
        /// <summary>
        /// Zobrazení layoutu prvků v textu ("ASCII grafika")
        /// </summary>
        internal string MapOfCellsAscii { get { return _GetMapOfCellsAscii(); } }
        /// <summary>
        /// Vrátí textové vyjádření layoutu ("ASCII grafika")
        /// </summary>
        /// <returns></returns>
        private string _GetMapOfCellsAscii()
        {
            if (_ItemsCount == 0) return "";

            int colCnt = _ColumnsCount;
            int rowCnt = _RowsCount;
            if (colCnt <= 0 || rowCnt <= 0) return "";

            // Definice rozměru:
            int maxTitleLen = __Items.Max(i => (i.Text ?? "").Length);    // Nejdelší text, který se bude zobrazovat uvnitř buňky
            int modW = maxTitleLen + 10;                                  // Modulo na jeden sloupec (buňka + okraj; kde buňka = nejdelší text + 8 znaků kolem; okraj = 2 znaky mezi)
            int modH = 4;
            int lineW = colCnt * modW;
            int lineH = rowCnt * modH;

            // Definice grafiky:
            char hChar = '-';
            char vChar = '|';
            char tlChar = '/';
            char trChar = '\\';
            char blChar = '\\';
            char brChar = '/';
            char outerEmpty = ' ';
            char innerEmpty = ' ';

            // Připravím prázdný prostor:
            string space = "".PadRight(lineW, outerEmpty);
            var lines = new string[lineH];
            for (int i = 0; i < lineH; i++)
                lines[i] = space;

            // Vykreslím prvky:
            foreach (var item in __Items)
                drawItem(item);

            // Shrnu linky naplněného prostoru do textu:
            var sb = new StringBuilder();
            foreach (var l in lines)
                sb.AppendLine(l);
            return sb.ToString();


            // Nakreslí jeden celý prvek
            void drawItem(IFlowLayoutItem itm)
            {
                int firstCol = (itm.FlowColBeginIndex ?? 0) * modW;
                int nextCol = ((itm.FlowColEndIndex ?? 0) + 1) * modW;
                int width = nextCol - firstCol - 6;                  // Počet znaků na šířku uvnitř vizuálního prvku, po odečtení: 2 fixní vlevo + 2 fixní vpravo + 2 mezery za prvkem

                int firstRow = (itm.FlowRowBeginIndex ?? 0) * modH;
                int nextRow = ((itm.FlowRowEndIndex ?? 0) + 1) * modH;
                int lastRow = nextRow - 2;                           // Index posledního řádku, kam se vepisuje koncová vodorovná linka
                int centRow = firstRow + (lastRow - firstRow) / 2;   // Index prostředního řádku, kam se vepisuje text prvku

                string title = getTitle(itm.Text, width, innerEmpty);
                string hLine = "".PadRight(width, hChar);
                string space = "".PadRight(width, innerEmpty);
                string text;
                for (int row = firstRow; row <= lastRow; row++)
                {
                    if (row == firstRow)
                        text = $"{tlChar}{hChar}{hLine}{hChar}{trChar}";            // První řádek:         /-----------------------------------------\
                    else if (row == centRow)
                        text = $"{vChar}{innerEmpty}{title}{innerEmpty}{vChar}";    // Prostřední řádek:    |                reference                |
                    else if (row == lastRow)
                        text = $"{blChar}{hChar}{hLine}{hChar}{brChar}";            // Poslední řádek:      \-----------------------------------------/
                    else 
                        text = $"{vChar}{innerEmpty}{space}{innerEmpty}{vChar}";    // Okolní řádky:        |                                         |

                    drawText(text, row, firstCol);
                }
            }
            // Vrátí daný text v požadované délce, vystředěný zleva a zprava mezerami (znakem empty)
            string getTitle(string text, int width, char empty)
            {
                if (String.IsNullOrEmpty(text))
                    text = "???";
                text = text.Trim();
                int add = width - text.Length;
                if (add > 0)
                    text = "".PadRight(add / 2, empty)
                         + text
                         + "".PadRight(add - (add / 2), empty);
                else if (add < 0)
                    text = text.Substring(0, width);
                return text;
            }
            // Vloží daný text na daný řádek (Y) a pozici (X), v rámci stringů v poli lines
            void drawText(string text, int row, int index)
            {
                int end = index + text.Length;
                string lineO = lines[row];
                string lineN = (index <= 0 ? "" : lineO.Substring(0, index))
                              + text
                              + (lineO.Length > end ? lineO.Substring(end) : "");
                lines[row] = lineN;
            }
        }
        #endregion
        #region class LineInfo : reprezentuje jeden sloupec nebo jeden řádek
        /// <summary>
        /// <see cref="LineInfo"/> : reprezentuje jeden sloupec nebo jeden řádek.
        /// Eviduje prostor pro Control, a pro Label Before a Label After, vždy počet pixelů v odpovídajícím směru (Sloupec: X a Width; Řádek: Y a Height).
        /// </summary>
        internal class LineInfo : IDisposable
        {
            #region Konstruktor, proměnné
            /// <summary>
            /// Konstruktor, typicky pro Row
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="axis"></param>
            /// <param name="index"></param>
            internal LineInfo(DfFlowLayoutInfo owner, AxisType axis, int index)
            {
                __Owner = owner;
                __Axis = axis;
                __Index = index;
            }
            /// <summary>
            /// Konstruktor, typicky pro Column (definuje designové šířky)
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="axis"></param>
            /// <param name="index"></param>
            /// <param name="labelBeforeDesignSize"></param>
            /// <param name="controlDesignSize"></param>
            /// <param name="labelAfterDesignSize"></param>
            internal LineInfo(DfFlowLayoutInfo owner, AxisType axis, int index, int? labelBeforeDesignSize, int? controlDesignSize, int? labelAfterDesignSize)
            {
                __Owner = owner;
                __Axis = axis;
                __Index = index;
                __LabelBeforeDesignSize = labelBeforeDesignSize;
                __ControlDesignSize = controlDesignSize;
                __LabelAfterDesignSize = labelAfterDesignSize;
            }
            private DfFlowLayoutInfo __Owner;
            private AxisType __Axis;
            private int __Index;
            private bool __ContainsItem;

            private int? __LabelBeforeDesignSize;
            private int? __ControlDesignSize;
            private int? __LabelAfterDesignSize;

            private int? __LabelBeforeMaximalSize;
            private int? __ControlBoundsMaximalSize;
            private int? __ControlImplicitMaximalSize;
            private int? __LabelAfterMaximalSize;

            private int? __CurrentBegin;
            private int? __LabelBeforeBegin;
            private int? __LabelBeforeEnd;
            private int? __ControlBegin;
            private int? __ControlEnd;
            private int? __LabelAfterBegin;
            private int? __LabelAfterEnd;
            private int? __CurrentEnd;
            private int? __NextBegin;

            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                string text = $"{(__Axis == AxisType.X ? "Column" : "Row")}[{__Index}]";
                if (!__CurrentBegin.HasValue)
                {   // Před finalizací vracím velikosti (Size):
                    text += $"; LabelBeforeSize: {LabelBeforeSize}; ControlSize: {ControlSize}; LabelAfterSize: {LabelAfterSize}";
                }
                else
                {   // Po finalizaci vracím souřadnice (Begin):
                    text += $"; Begin: {__CurrentBegin}";
                    if (__ControlBegin.Value > __ControlBegin.Value) text += $"; Control: {__ControlBegin}";
                    text += $"; End: {__CurrentEnd}";
                }
                return text;
            }
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                __Owner = null;
            }
            #endregion
            #region Public hodnoty : velikosti pracovní, výsledné; pozice
            /// <summary>
            /// Typ osy X nebo Y
            /// </summary>
            public AxisType Axis { get { return __Axis; } }
            /// <summary>
            /// Index prvku v sekvenci; číslo sloupce/řádku, počínaje 0
            /// </summary>
            public int Index { get { return __Index; } }
            /// <summary>
            /// Velikost (šířka nebo výška) prostoru pro labely před controlem, deklarovaná ve vlastnostech sloupce (řádky nepoužívají).
            /// </summary>
            public int? LabelBeforeDesignSize { get { return __LabelBeforeDesignSize; } }
            /// <summary>
            /// Velikost (šířka nebo výška) prostoru pro controly, deklarovaná ve vlastnostech sloupce (řádky nepoužívají).
            /// </summary>
            public int? ControlDesignSize { get { return __ControlDesignSize; } }
            /// <summary>
            /// Velikost (šířka nebo výška) prostoru pro controly, implicitní, pokud nelze jinak určit
            /// </summary>
            public int? ControlImplicitSize { get { return __Owner?.GetControlImplicitSize(this.__Axis); } }
            /// <summary>
            /// Velikost (šířka nebo výška) prostoru pro labely za controlem, deklarovaná ve vlastnostech sloupce (řádky nepoužívají).
            /// </summary>
            public int? LabelAfterDesignSize { get { return __LabelAfterDesignSize; } }

            /// <summary>
            /// Obsahuje true pokud tato dimenze (konkrétní sloupec nebo řádek) obsahuje nějaký prvek.
            /// Nastavuje se v procesu <see cref="DfFlowLayoutInfo.ProcessFlowItems()"/> v podprocesu <see cref="DfFlowLayoutInfo._ProcessControlSingleSpan()"/>.
            /// Sloupce mohou existovat i bez prvku (výjimečně), řádky nikdy = všechny řádky mají nějaký prvek (protože neumíme přeskočit volný řádek).<br/>
            /// Lze nastavit na true, ale nelze vrátit z true na false.
            /// </summary>
            public bool ContainsItem { get { return __ContainsItem; } set { __ContainsItem |= value; } }
            /// <summary>
            /// Velikost (šířka nebo výška) labelů před controlem, dosud nejvyšší nalezená; null = dosud nebylo určeno.
            /// Setování hodnoty střádá maximální hodnotu ze všech dosud setovaných! Setování null nic nezmění.
            /// </summary>
            public int? LabelBeforeMaximalSize { get { return __LabelBeforeMaximalSize; } set { if (value.HasValue) __LabelBeforeMaximalSize = _GetMax(__LabelBeforeMaximalSize, value); } }
            /// <summary>
            /// Velikost (šířka nebo výška) controlů, definovaná exaktně v pixelech v designu (frm.xml) jako Width nebo Height, dosud nejvyšší nalezená; null = dosud nebylo určeno.
            /// Sem nepatří velikost určená jako implicitní pro daný prvek (tj. určená automaticky podle typu / editačního stylu / atributu). Ta patří do <see cref="ControlImplicitMaximalSize"/>.
            /// Setování hodnoty střádá maximální hodnotu ze všech dosud setovaných! Setování null nic nezmění.
            /// </summary>
            public int? ControlBoundsMaximalSize { get { return __ControlBoundsMaximalSize; } set { if (value.HasValue) __ControlBoundsMaximalSize = _GetMax(__ControlBoundsMaximalSize, value); } }
            /// <summary>
            /// Velikost (šířka nebo výška) controlů, určená podle jejich typu v případě, kdy nebyla exaktně daná v designu (frm.xml) jako Width nebo Height, dosud nejvyšší nalezená; null = dosud nebylo určeno.
            /// Setování hodnoty střádá maximální hodnotu ze všech dosud setovaných! Setování null nic nezmění.
            /// </summary>
            public int? ControlImplicitMaximalSize { get { return __ControlImplicitMaximalSize; } set { if (value.HasValue) __ControlImplicitMaximalSize = _GetMax(__ControlImplicitMaximalSize, value); } }
            /// <summary>
            /// Velikost (šířka nebo výška) labelů za controlem, dosud nejvyšší nalezená; null = dosud nebylo určeno.
            /// Setování hodnoty střádá maximální hodnotu ze všech dosud setovaných! Setování null nic nezmění.
            /// </summary>
            public int? LabelAfterMaximalSize { get { return __LabelAfterMaximalSize; } set { if (value.HasValue) __LabelAfterMaximalSize = _GetMax(__LabelAfterMaximalSize, value); } }

            /// <summary>
            /// Velikost (šířka nebo výška) labelů před controlem
            /// </summary>
            public int LabelBeforeSize { get { return (__LabelBeforeDesignSize ?? LabelBeforeMaximalSize ?? 0); } }
            /// <summary>
            /// Velikost mezery (šířka nebo výška) mezi LabelBefore a Control (Spacing před controlem).
            /// Pokud <see cref="LabelBeforeSize"/> není kladné číslo, pak je zde 0. Nechceme přidávat mezeru před control, když před controlem nebude label.
            /// </summary>
            public int DistanceBeforeControl { get { return _GetDistance(LabelBeforeSize, __Owner?.GetDistanceBeforeControl(this.__Axis) ?? 0); } }
            /// <summary>
            /// Velikost (šířka nebo výška) vlastních controlů
            /// </summary>
            public int ControlSize { get { return GetControlSize(ControlSizeType.Final) ?? 0; } }
            /// <summary>
            /// Velikost mezery (šířka nebo výška) mezi Control a LabelAfter (Spacing za controlem).
            /// Pokud <see cref="LabelAfterSize"/> není kladné číslo, pak je zde 0. Nechceme přidávat mezeru za control, když za controlem nebude label.
            /// </summary>
            public int DistanceAfterControl { get { return _GetDistance(LabelAfterSize, __Owner?.GetDistanceAfterControl(this.__Axis) ?? 0); } }
            /// <summary>
            /// Velikost mezery (šířka nebo výška) labelů za controlem
            /// </summary>
            public int LabelAfterSize { get { return (__LabelAfterDesignSize ?? LabelAfterMaximalSize ?? 0); } }
            /// <summary>
            /// Velikost prostoru za celým sloupcem / řádkem (Spacing)
            /// </summary>
            public int DistanceAfterSize { get { return __Owner?.GetDistanceSize(this.__Axis) ?? 0; } }

            /// <summary>
            /// Souřadnice, kde začíná tento prvek. Lze setovat.
            /// </summary>
            public int? CurrentBegin { get { return __CurrentBegin; } }
            /// <summary>
            /// Souřadnice, kde začíná Label před controlem. Jeho velikost je <see cref="LabelBeforeSize"/>.
            /// Pokud Label před controlem neexistuje, pak tato souřadnice obsahuje totéž co <see cref="ControlBegin"/>.
            /// </summary>
            public int? LabelBeforeBegin { get { return __LabelBeforeBegin; } }
            /// <summary>
            /// Souřadnice, kde končí Label před controlem. Jeho velikost je <see cref="LabelBeforeSize"/>.
            /// Pokud Label před controlem neexistuje, pak tato souřadnice obsahuje totéž co <see cref="ControlBegin"/>.
            /// </summary>
            public int? LabelBeforeEnd { get { return __LabelBeforeEnd; } }
            /// <summary>
            /// Souřadnice, kde začíná vlastní Control. Jeho velikost je <see cref="ControlSize"/>.
            /// </summary>
            public int? ControlBegin { get { return __ControlBegin; } }
            /// <summary>
            /// Souřadnice, kde končí vlastní Control. Jeho velikost je <see cref="ControlSize"/>.
            /// </summary>
            public int? ControlEnd { get { return __ControlEnd; } }
            /// <summary>
            /// Souřadnice, kde začíná Label za controlem. Jeho velikost je <see cref="LabelAfterSize"/>.
            /// </summary>
            public int? LabelAfterBegin { get { return __LabelAfterBegin; } }
            /// <summary>
            /// Souřadnice, kde končí Label za controlem. Jeho velikost je <see cref="LabelAfterSize"/>.
            /// </summary>
            public int? LabelAfterEnd { get { return __LabelAfterEnd; } }
            /// <summary>
            /// Souřadnice, kde končí tento prvek (konec LabelAfter).
            /// </summary>
            public int? CurrentEnd { get { return __CurrentEnd; } }
            /// <summary>
            /// Souřadnice, kde začíná následující prvek.
            /// </summary>
            public int? NextBegin { get { return __NextBegin; } }

            /// <summary>
            /// Vrátí Max() z dodaných hodnot; řeší i NULL hodnoty.
            /// </summary>
            /// <param name="value1"></param>
            /// <param name="value2"></param>
            /// <returns></returns>
            private static int? _GetMax(int? value1, int? value2)
            {
                bool has1 = value1.HasValue;
                bool has2 = value2.HasValue;
                if (has1 && has2) return (value1.Value > value2.Value ? value1.Value : value2.Value);
                if (has1) return value1;
                if (has2) return value2;
                return null;
            }
            /// <summary>
            /// Metoda vrátí velikost Distance <paramref name="distanceSize"/>, ale jen tehdy, pokud velikost reálného prvku <paramref name="labelSize"/> je kladná.
            /// </summary>
            /// <param name="labelSize"></param>
            /// <param name="distanceSize"></param>
            /// <returns></returns>
            private static int _GetDistance(int labelSize, int distanceSize)
            {
                return (labelSize > 0 ? distanceSize : 0);
            }
            /// <summary>
            /// Určí a vrátí velikost Controlu podle požadovaného typu
            /// </summary>
            /// <param name="sizeType"></param>
            /// <returns></returns>
            public int? GetControlSize(ControlSizeType sizeType)
            {
                switch (sizeType)
                {
                    case ControlSizeType.None: return null;
                    case ControlSizeType.Design: return ControlDesignSize;
                    case ControlSizeType.MaxBounds: return ControlBoundsMaximalSize;
                    case ControlSizeType.MaxImplicit: return ControlImplicitMaximalSize;
                    case ControlSizeType.DesignOrBounds: return ControlDesignSize ?? ControlBoundsMaximalSize;
                    case ControlSizeType.BoundsOrImplicit: return ControlBoundsMaximalSize ?? ControlImplicitMaximalSize;
                    case ControlSizeType.Final:
                        // Priority pro Final: 1. Design (pokud je dán), 2. Maximum z (BoundsMax nebo ImplicitMax), 3. výchozí pro dimenzi (záchranná konstanta).
                        var itemSize = _GetMax(ControlBoundsMaximalSize, ControlImplicitMaximalSize);
                        return ControlDesignSize ?? itemSize ?? ControlImplicitSize ?? 0;
                }
                return null;
            }
            /// <summary>
            /// Typ velikosti pro oblast Control, kterou určí metoda <see cref="GetControlSize(ControlSizeType)"/>
            /// </summary>
            public enum ControlSizeType
            {
                /// <summary>
                /// Žádná
                /// </summary>
                None = 0,
                /// <summary>
                /// Výhradně designová = určená v záhlaví sloupce
                /// </summary>
                Design,
                /// <summary>
                /// Maximum z nalezených prvků, exaktně zadané (jako atribut Width), bez ohledu na Designovou velikost celého sloupce / řádku
                /// </summary>
                MaxBounds,
                /// <summary>
                /// Maximum z nalezených prvků, určené implicitně podle typu a obsahu. Neobsahuje exaktní velikost prvků (Width / Height) ani Designovou velikost celého sloupce / řádku.
                /// </summary>
                MaxImplicit,
                /// <summary>
                /// Z designu (ze záhlaví) anebo když tam není, tak z prvků, exaktně zadané (jako atribut Width / Height). Neřeší <see cref="MaxImplicit"/> ani <see cref="LineInfo.ControlImplicitSize"/>.
                /// </summary>
                DesignOrBounds,
                /// <summary>
                /// Z prvků s definovaným rozměrem Bounds, anebo z implicitního rozměru = tedy z některých konkrétních prvků.
                /// Neřeší Design rozměr (ColumnWidths) ani Výchozí rozměr.
                /// </summary>
                BoundsOrImplicit,
                /// <summary>
                /// Finálně určená pro výsledný layout.
                /// </summary>
                Final
            }
            #endregion
            #region Metody pro zpracování layoutu
            /// <summary>
            /// Resetuje svoje provozní a výsledné hodnoty. Volá se na začátku finalizace layoutu jako první metoda.
            /// Neresetuje designové hodnoty.
            /// </summary>
            public void Reset()
            {
                __ContainsItem = false;
                __LabelBeforeMaximalSize = null;
                __ControlBoundsMaximalSize = null;
                __ControlImplicitMaximalSize = null;
                __LabelAfterMaximalSize = null;

                __CurrentBegin = null;
                __LabelBeforeBegin = null;
                __LabelBeforeEnd = null;
                __ControlBegin = null;
                __ControlEnd = null;
                __LabelAfterBegin = null;
                __LabelAfterEnd = null;
                __CurrentEnd = null;
                __NextBegin = null;
            }
            /// <summary>
            /// Dokončí zpracování pro tento prvek layoutu (jeden sloupec / jeden řádek).
            /// Na vstupu je souřadnice (X nebo Y), kde tento prvek začíná.
            /// Prvek si dopočte svoje souřadnice, a vrátí souřadnici kde končí (pro <paramref name="isLast"/> = true) nebo kde začíná další prvek (false).
            /// Výstup slouží pro snadné seriové zpracování.
            /// </summary>
            /// <param name="begin"></param>
            /// <param name="isLast"></param>
            /// <returns></returns>
            public int Finish(int begin, bool isLast)
            {
                _SetCurrentBegin(begin);
                return (isLast ? __CurrentEnd : __NextBegin).Value;
            }
            /// <summary>
            /// Nastaví aktuální počátek a dopočte aktuálně platné další pozice prvků. Lze setovat null.
            /// </summary>
            /// <param name="currentBegin"></param>
            private void _SetCurrentBegin(int? currentBegin)
            {
                if (currentBegin.HasValue)
                {
                    int position = currentBegin.Value;
                    __CurrentBegin = position;
                    __LabelBeforeBegin = position;
                    position += LabelBeforeSize;
                    __LabelBeforeEnd = position;
                    position += DistanceBeforeControl;
                    __ControlBegin = position;
                    position += ControlSize;
                    __ControlEnd = position;
                    position += DistanceAfterControl;
                    __LabelAfterBegin = position;
                    position += LabelAfterSize;
                    __LabelAfterEnd = position;
                    __CurrentEnd = position;
                    position += DistanceAfterSize;
                    __NextBegin = position;
                }
                else
                {
                    __CurrentBegin = null;
                    __LabelBeforeBegin = null;
                    __LabelBeforeEnd = null;
                    __ControlBegin = null;
                    __ControlEnd = null;
                    __LabelAfterBegin = null;
                    __LabelAfterEnd = null;
                    __CurrentEnd = null;
                    __NextBegin = null;
                }
            }
            #endregion
        }
        /// <summary>
        /// Vrátí velikost prostoru mezi sloupci (pro <paramref name="axis"/> == <see cref="AxisType.X"/>) nebo mezi řádky (pro <paramref name="axis"/> == <see cref="AxisType.Y"/>)
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private int GetControlImplicitSize(AxisType axis)
        {
            return (axis == AxisType.X ? this.__ColumnImplicitSize : this.__RowImplicitSize);
        }
        /// <summary>
        /// Vrátí velikost prostoru mezi LabelBefore a Control (Spacing před controlem)..
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private int GetDistanceBeforeControl(AxisType axis)
        {
            return (axis == AxisType.X ? __ControlMargins.Left : __ControlMargins.Top);
        }
        /// <summary>
        /// Vrátí velikost prostoru mezi Control a LabelAfter (Spacing za controlem).
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private int GetDistanceAfterControl(AxisType axis)
        {
            return (axis == AxisType.X ? __ControlMargins.Right : __ControlMargins.Bottom);
        }
        /// <summary>
        /// Vrátí velikost prostoru mezi sloupci (pro <paramref name="axis"/> == <see cref="AxisType.X"/>) nebo mezi řádky (pro <paramref name="axis"/> == <see cref="AxisType.Y"/>)
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private int GetDistanceSize(AxisType axis)
        {
            return (axis == AxisType.X ? this.__ColumnsDistance : this.__RowsDistance);
        }
        #endregion
    }
    #region Interface IFlowLayoutItem : Definuje potřebné vlastnosti prvku DataFormu , který bude umísťován do FlowLayoutu.
    /// <summary>
    /// <see cref="IFlowLayoutItem"/> : Definuje potřebné vlastnosti prvku DataFormu , který bude umísťován do FlowLayoutu.
    /// </summary>
    internal interface IFlowLayoutItem
    {
        // Designové, čerpané z frm.xml:
        /// <summary>
        /// Jméno z atributu Name
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Příznak, že s prvkem se vůbec nemá počítat při tvorbě layoutu (jeho stav obsahuje <see cref="ControlStateType.Absent"/>).
        /// </summary>
        bool IsAbsent { get; }
        /// <summary>
        /// Prvek bude řešen ve FlowLayout
        /// </summary>
        bool IsFlowMode { get; }
        /// <summary>
        /// Text, slouží jen pro popis prvku pro ladění. Implicitně Name nebo ColumnName.
        /// </summary>
        string Text { get; }
        /// <summary>
        /// Explicitně definovaný index sloupce, kde prvek má začínat
        /// </summary>
        int? DesignColIndex { get; }
        /// <summary>
        /// Počet obsazených spojených sloupců do šířky
        /// </summary>
        int? DesignColSpan { get; }
        /// <summary>
        /// Počet obsazených spojených řádků do výšky
        /// </summary>
        int? DesignRowSpan { get; }
        /// <summary>
        /// Umístění prvku vodorovně v rámci sloupce v případě, kdy šířka prvku je menší než šířka sloupce. Řeší tedy zarovnání controlu ve sloupci, nikoli zarovnání obsahu (textu) v rámci controlu.
        /// </summary>
        HPositionType? DesignHPosition { get; }
        /// <summary>
        /// Umístění prvku svisle v rámci řádku v případě, kdy výška prvku je menší než výška řádku. Řeší tedy zarovnání controlu v řádku, nikoli zarovnání obsahu (textu) v rámci controlu.
        /// </summary>
        VPositionType? DesignVPosition { get; }
        /// <summary>
        /// Rozšíření controlu do okolního prostoru pro labely, pokud labely nejsou použity
        /// </summary>
        ExpandControlType? DesignExpandControl { get; }
        /// <summary>
        /// Exaktně daná šířka v pixelech, v rámci Bounds prvku
        /// </summary>
        int? DesignWidthPixel { get; }
        /// <summary>
        /// Exaktně daná šířka v procentech, v rámci Bounds prvku
        /// </summary>
        int? DesignWidthPercent { get; }
        /// <summary>
        /// Exaktně daná výška v pixelech, v rámci Bounds prvku
        /// </summary>
        int? DesignHeightPixel { get; }
        /// <summary>
        /// Exaktně daná výška v procentech, v rámci Bounds prvku
        /// </summary>
        int? DesignHeightPercent { get; }
        /// <summary>
        /// Exaktně daná šířka Main labelu v pixelech
        /// </summary>
        int? DesignLabelWidth { get; }
        /// <summary>
        /// Pozice Main labelu
        /// </summary>
        LabelPositionType LabelPosition { get; }
        /// <summary>
        /// Existuje MainLabel (tzn. jeho text není prázdný a pozice je některá reálná)
        /// </summary>
        bool MainLabelExists { get; }
        /// <summary>
        /// Existuje Control (tzn. nějaký textbox nebo picture nebo button)? Nebo ne (třeba Placeholder)
        /// </summary>
        bool ControlExists { get; }
        /// <summary>
        /// Existuje SuffixLabel (tzn. jeho text není prázdný a prostor vpravo od controlu není obsazen labelem MainLabel)
        /// </summary>
        bool SuffixLabelExists { get; }

        // Implicitní, dopočtené pro prvek z jeho typu, textu atd:
        /// <summary>
        /// Dopočtená šířka Main Labelu
        /// </summary>
        int? ImplicitMainLabelWidth { get; }
        /// <summary>
        /// Dopočtená výška Main Labelu
        /// </summary>
        int? ImplicitMainLabelHeight { get; }
        /// <summary>
        /// Výchozí minimální šířka vlastního controlu v pixelech, lze setovat.
        /// Pokud sloupec nebude mít žádnou šířku, a bude v něm tento prvek, a ten bude mít zde nastavenu určitou MinWidth, pak jeho sloupec ji bude mít nastavenu jako Implicitní.
        /// Nicméně pokud sloupec bude mít šířku větší, a prvek bude mít jen tuto MinWidth, pak prvek bude ve výsledku dimenzován na 100% reálné šířky sloupce, klidně větší než zdejší MinWidth.
        /// </summary>
        int? ImplicitControlMinimalWidth { get; }
        /// <summary>
        /// Výchozí optimální šířka vlastního controlu v pixelech, lze setovat.
        /// Pokud sloupec bude mít výslednou šířku větší než tato OptimalWidth, pak prvek bude ve výsledku dimenzován na tuto OptimalWidth, jako by ji zadal uživatel do Width.
        /// </summary>
        int? ImplicitControlOptimalWidth { get; }
        /// <summary>
        /// Výchozí optimální šířka vlastního controlu v pixelech, lze setovat.
        /// Pokud sloupec bude mít výslednou šířku větší než tato OptimalWidth, pak prvek bude ve výsledku dimenzován na tuto OptimalWidth, jako by ji zadal uživatel do Width.
        /// </summary>
        int? ImplicitControlMinimalHeight { get; }
        /// <summary>
        /// Výchozí optimální výška vlastního controlu v pixelech, lze setovat.
        /// Pokud řádek bude mít výslednou výšku větší než tato OptimalHeight, pak prvek bude ve výsledku dimenzován na tuto OptimalHeight, jako by ji zadal uživatel do Height.
        /// </summary>
        int? ImplicitControlOptimalHeight { get; }
        /// <summary>
        /// Dopočtená šířka Suffix Labelu
        /// </summary>
        int? ImplicitSuffixLabelWidth { get; }
        /// <summary>
        /// Dopočtená výška Suffix Labelu
        /// </summary>
        int? ImplicitSuffixLabelHeight { get; }

        // Umístění prvku v rámci FlowLayoutu
        /// <summary>
        /// První sloupec, kde se prvek nachzí
        /// </summary>
        int? FlowColBeginIndex { get; set; }
        /// <summary>
        /// Poslední sloupec, kde se prvek nachzí
        /// </summary>
        int? FlowColEndIndex { get; set; }
        /// <summary>
        /// První řádek, kde se prvek nachzí
        /// </summary>
        int? FlowRowBeginIndex { get; set; }
        /// <summary>
        /// Poslední řádek, kde se prvek nachzí
        /// </summary>
        int? FlowRowEndIndex { get; set; }

        /// <summary>
        /// Výsledná souřadnice celé buňky
        /// </summary>
        CellMatrixInfo CellMatrix { get; set; }
        /// <summary>
        /// Výsledná souřadnice MainLabel v rámci parenta
        /// </summary>
        ControlBounds MainLabelBounds { get; set; }
        /// <summary>
        /// Zarovnání textu MainLabel v prostoru <see cref="MainLabelBounds"/>
        /// </summary>
        ContentAlignmentType? MainLabelAlignment { get; set; }
        /// <summary>
        /// Výsledná souřadnice Controlu v rámci parenta
        /// </summary>
        ControlBounds ControlBounds { get; set; }
        /// <summary>
        /// Výsledná souřadnice SuffixLabel v rámci parenta
        /// </summary>
        ControlBounds SuffixLabelBounds { get; set; }
        /// <summary>
        /// Zarovnání textu SuffixLabel v prostoru <see cref="SuffixLabelBounds"/>
        /// </summary>
        ContentAlignmentType? SuffixLabelAlignment { get; set; }
        /// <summary>
        /// Algoritmus FlowLayout pro prvek zajistil potřebnou šířku
        /// </summary>
        bool AcceptedWidth { get; set; }
        /// <summary>
        /// Algoritmus FlowLayout pro prvek zajistil potřebnou výšku
        /// </summary>
        bool AcceptedHeight { get; set; }
        /// <summary>
        /// Resetuje výsledné hodnoty FlowLayoutu. Volá se na začátku finalizace layoutu jako první metoda.
        /// Neresetuje designové hodnoty. Neresetuje umístění do Matrixu. Resetuje to co souvisí s pixely.
        /// </summary>
        void ResetFlowFinalResults();
    }
    #endregion
    #region class CellMatrixInfo : popisuje veškeré souřadnice v jednom prvku FlowLayoutu (kde začínají a končí labely, a kde control)
    /// <summary>
    /// <see cref="CellMatrixInfo"/> : popisuje veškeré souřadnice v jednom prvku FlowLayoutu (kde začínají a končí labely, a kde control)
    /// </summary>
    internal class CellMatrixInfo
    {
        #region Konstruktory a proměnné
        /// <summary>
        /// Konstruktor pro sadu sloupců
        /// </summary>
        /// <param name="columnBegin"></param>
        /// <param name="columnEnd"></param>
        /// <param name="rowBegin"></param>
        /// <param name="rowEnd"></param>
        internal CellMatrixInfo(DfFlowLayoutInfo.LineInfo columnBegin, DfFlowLayoutInfo.LineInfo columnEnd, DfFlowLayoutInfo.LineInfo rowBegin, DfFlowLayoutInfo.LineInfo rowEnd)
        {
            __Data = new int[2, 7];

            __Data[X, CellBegin] = columnBegin.CurrentBegin.Value;
            __Data[X, LabelBeforeEnd] = columnBegin.LabelBeforeEnd.Value;
            __Data[X, ControlBegin] = columnBegin.ControlBegin.Value;
            __Data[X, ControlFirstEnd] = columnBegin.ControlEnd.Value;
            __Data[X, ControlEnd] = columnEnd.ControlEnd.Value;
            __Data[X, LabelAfterBegin] = columnEnd.LabelAfterBegin.Value;
            __Data[X, CellEnd] = columnEnd.CurrentEnd.Value;

            __Data[Y, CellBegin] = rowBegin.CurrentBegin.Value;
            __Data[Y, LabelBeforeEnd] = rowBegin.LabelBeforeEnd.Value;
            __Data[Y, ControlBegin] = rowBegin.ControlBegin.Value;
            __Data[Y, ControlFirstEnd] = rowBegin.ControlEnd.Value;
            __Data[Y, ControlEnd] = rowEnd.ControlEnd.Value;
            __Data[Y, LabelAfterBegin] = rowEnd.LabelAfterBegin.Value;
            __Data[Y, CellEnd] = rowEnd.CurrentEnd.Value;
        }
        /// <summary>
        /// Konstruktor pro exaktní hodnoty
        /// </summary>
        /// <param name="labelLeftX"></param>
        /// <param name="labelLeftR"></param>
        /// <param name="controlX"></param>
        /// <param name="controlFirstR"></param>
        /// <param name="controlR"></param>
        /// <param name="labelRightX"></param>
        /// <param name="labelRightR"></param>
        /// <param name="labelTopY"></param>
        /// <param name="labelTopB"></param>
        /// <param name="controlT"></param>
        /// <param name="controlFirstB"></param>
        /// <param name="controlB"></param>
        /// <param name="labelBottomT"></param>
        /// <param name="labelBottomB"></param>
        internal CellMatrixInfo(int labelLeftX, int labelLeftR, int controlX, int controlFirstR, int controlR, int labelRightX, int labelRightR, int labelTopY, int labelTopB, int controlT, int controlFirstB, int controlB, int labelBottomT, int labelBottomB)
        {
            __Data = new int[2, 6];

            __Data[X, CellBegin] = labelLeftX;
            __Data[X, LabelBeforeEnd] = labelLeftR;
            __Data[X, ControlBegin] = controlX;
            __Data[X, ControlFirstEnd] = controlFirstR;
            __Data[X, ControlEnd] = controlR;
            __Data[X, LabelAfterBegin] = labelRightX;
            __Data[X, CellEnd] = labelRightR;

            __Data[Y,CellBegin] = labelTopY;
            __Data[Y,LabelBeforeEnd] = labelTopB;
            __Data[Y, ControlBegin] = controlT;
            __Data[Y, ControlFirstEnd] = controlFirstB;
            __Data[Y, ControlEnd] = controlB;
            __Data[Y, LabelAfterBegin] = labelBottomT;
            __Data[Y, CellEnd] = labelBottomB;
        }
        /// <summary>
        /// Pole souřadnic. <br/>
        /// První index: 0 = osa X,  1 = osa Y.<br/>
        /// Druhý index: 0 = začátek buňky = začátek labelu před (Vlevo/Nahoře),  1 = konec labelu před,  2 = začátek Controlu,  3 = konec Controlu,  4 = začátek labelu za (Vpravo/Dole),   5 = konec labelu za = konec buňky.
        /// </summary>
        private int[,] __Data;

        private const int X = 0;
        private const int Y = 1;
        private const int CellBegin = 0;
        private const int LabelBeforeBegin = 0;
        private const int LabelBeforeEnd = 1;
        private const int ControlBegin = 2;
        private const int ControlFirstEnd = 3;
        private const int ControlEnd = 4;
        private const int LabelAfterBegin = 5;
        private const int LabelAfterEnd = 6;
        private const int CellEnd = 6;
        #endregion
        #region Public data
        /// <summary>
        /// Celá buňka, souřadnice Left
        /// </summary>
        public int CellLeft { get { return __Data[X, CellBegin]; } }
        /// <summary>
        /// Label vlevo, souřadnice Left
        /// </summary>
        public int LeftLabelLeft { get { return __Data[X, LabelBeforeBegin]; } }
        /// <summary>
        /// Label vlevo, souřadnice Right
        /// </summary>
        public int LeftLabelRight { get { return __Data[X, LabelBeforeEnd]; } }
        /// <summary>
        /// Label vlevo, velikost Width
        /// </summary>
        public int LeftLabelWidth { get { return __Data[X, LabelBeforeEnd] - __Data[X, LabelBeforeBegin]; } }
        /// <summary>
        /// Vlastní Control, souřadnice Left
        /// </summary>
        public int ControlLeft { get { return __Data[X, ControlBegin]; } }
        /// <summary>
        /// Vlastní Control, souřadnice Right prvního columnu - má smysl pro prvky, které mají ColSpan větší než 1
        /// </summary>
        public int ControlFirstRight { get { return __Data[X, ControlFirstEnd]; } }
        /// <summary>
        /// Vlastní Control, souřadnice Right
        /// </summary>
        public int ControlRight { get { return __Data[X, ControlEnd]; } }
        /// <summary>
        /// Vlastní Control, velikost Width
        /// </summary>
        public int ControlWidth { get { return __Data[X, ControlEnd] - __Data[X, ControlBegin]; } }
        /// <summary>
        /// Label vpravo, souřadnice Left
        /// </summary>
        public int RightLabelLeft { get { return __Data[X, LabelAfterBegin]; } }
        /// <summary>
        /// Label vpravo, souřadnice Right
        /// </summary>
        public int RightLabelRight { get { return __Data[X, LabelAfterEnd]; } }
        /// <summary>
        /// Label vpravo, velikost Width
        /// </summary>
        public int RightLabelWidth { get { return __Data[X, LabelAfterEnd] - __Data[X, LabelAfterBegin]; } }
        /// <summary>
        /// Celá buňka, souřadnice Right
        /// </summary>
        public int CellRight { get { return __Data[X, CellEnd]; } }
        /// <summary>
        /// Celá buňka, velikost Width
        /// </summary>
        public int CellWidth { get { return __Data[X, CellEnd] - __Data[X, CellBegin]; } }

        /// <summary>
        /// Celá buňka, souřadnice Top
        /// </summary>
        public int CellTop { get { return __Data[Y, CellBegin]; } }
        /// <summary>
        /// Label nahoře, souřadnice Top
        /// </summary>
        public int TopLabelTop { get { return __Data[Y, LabelBeforeBegin]; } }
        /// <summary>
        /// Label nahoře, souřadnice Bottom
        /// </summary>
        public int TopLabelBottom { get { return __Data[Y, LabelBeforeEnd];; } }
        /// <summary>
        /// Label nahoře, velikost Height
        /// </summary>
        public int TopLabelHeight { get { return __Data[Y, LabelBeforeEnd] - __Data[Y, LabelBeforeBegin]; } }
        /// <summary>
        /// Vlastní Control, souřadnice Top
        /// </summary>
        public int ControlTop { get { return __Data[Y, ControlBegin]; } }
        /// <summary>
        /// Vlastní Control, souřadnice Bottom prvního řádku - má smysl pro prvky, které mají RowSpan větší než 1 (toto je Bottom pro Labely umístěné Vlevo a Vpravo)
        /// </summary>
        public int ControlFirstBottom { get { return __Data[Y, ControlFirstEnd]; } }
        /// <summary>
        /// Vlastní Control, souřadnice Bottom
        /// </summary>
        public int ControlBottom { get { return __Data[Y, ControlEnd]; } }
        /// <summary>
        /// Vlastní Control, velikost Height
        /// </summary>
        public int ControlHeight { get { return __Data[Y, ControlEnd] - __Data[Y, ControlBegin]; } }
        /// <summary>
        /// Label dole, souřadnice Top
        /// </summary>
        public int BottomLabelTop { get { return __Data[Y, LabelAfterBegin]; } }
        /// <summary>
        /// Label dole, souřadnice Bottom
        /// </summary>
        public int BottomLabelBottom { get { return __Data[Y, LabelAfterEnd]; } }
        /// <summary>
        /// Label dole, velikost Height
        /// </summary>
        public int BottomLabelHeight { get { return __Data[Y, LabelAfterEnd] - __Data[Y, LabelAfterBegin]; } }
        /// <summary>
        /// Celá buňka, souřadnice Bottom
        /// </summary>
        public int CellBottom { get { return __Data[Y, CellEnd]; } }
        /// <summary>
        /// Celá buňka, velikost Height
        /// </summary>
        public int CellHeight { get { return __Data[Y, CellEnd] - __Data[Y, CellBegin]; } }

        /// <summary>
        /// Okraj vlevo od controlu, za LeftLabel
        /// </summary>
        public int MarginControlLeft { get { return __Data[X, ControlBegin] - __Data[X, LabelBeforeEnd]; } }
        /// <summary>
        /// Okraj nahoře od controlu, pod TopLabel
        /// </summary>
        public int MarginControlTop { get { return __Data[Y, ControlBegin] - __Data[Y, LabelBeforeEnd]; } }
        /// <summary>
        /// Okraj vpravo od controlu, před RightLabel
        /// </summary>
        public int MarginControlRight { get { return __Data[X, LabelAfterBegin] - __Data[X, ControlEnd]; } }
        /// <summary>
        /// Okraj dole od controlu, před BottomLabel
        /// </summary>
        public int MarginControlBottom { get { return __Data[Y, LabelAfterBegin] - __Data[Y, ControlEnd]; } }
        #endregion
        #region Získání Bounds podle požadavku
        /// <summary>
        /// Vrátí souřadnice z matrixu, primárně controlové, volitelně expandované
        /// </summary>
        /// <param name="expandControl"></param>
        /// <returns></returns>
        internal ControlBounds GetBounds(ExpandControlType? expandControl = null)
        {
            var expand = expandControl ?? ExpandControlType.None;
            var expL = expand.HasFlag(ExpandControlType.Left);
            var expT = expand.HasFlag(ExpandControlType.Top);
            var expR = expand.HasFlag(ExpandControlType.Right);
            var expB = expand.HasFlag(ExpandControlType.Bottom);
            int l = expL ? this.CellLeft : this.ControlLeft;
            int t = expT ? this.CellTop : this.ControlTop;
            int r = expR ? this.CellRight : this.ControlRight;
            int b = expB ? this.CellBottom : this.ControlBottom;
            return new ControlBounds(l, t, (r - l), (b - t));
        }
        #endregion
    }
    #endregion
    #region class FlowGuideLines : Jedna vodící linka
    /// <summary>
    /// Jedna vodící linka
    /// </summary>
    internal class FlowGuideLine
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="position"></param>
        /// <param name="axis"></param>
        /// <param name="lineType"></param>
        internal FlowGuideLine(int position, AxisType axis, GuideLineType lineType)
        {
            Position = position;
            Axis = axis;
            LineType = lineType;
        }
        /// <summary>
        /// Komparátor
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        internal static int CompareByLineType(FlowGuideLine a, FlowGuideLine b)
        {
            int al = getLevel(a.LineType);
            int bl = getLevel(b.LineType);
            return (al.CompareTo(bl));

            int getLevel(GuideLineType lt)
            {
                if (lt.HasFlag(GuideLineType.Cell)) return 10;
                if (lt.HasFlag(GuideLineType.Control)) return 8;
                if (lt.HasFlag(GuideLineType.LabelBefore)) return 6;
                if (lt.HasFlag(GuideLineType.LabelAfter)) return 6;
                return 0;
            }
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Axis '{Axis}': {Position} px; '{LineType}'.";
        }
        /// <summary>
        /// Posune svoji pozici o daný Margin
        /// </summary>
        /// <param name="margin"></param>
        public void AddMargin(int margin)
        {
            this.Position += margin;
        }
        /// <summary>
        /// Pozice v pixelech od Top nebo Left, 0 = první viditelný pixel
        /// </summary>
        public int Position { get; private set; }
        /// <summary>
        /// Směr osy X nebo Y
        /// </summary>
        public AxisType Axis { get; private set; }
        /// <summary>
        /// Typ linky
        /// </summary>
        public GuideLineType LineType { get; private set; }
    }
    #endregion
    #region Enumy
    /// <summary>
    /// Osa X nebo Y
    /// </summary>
    internal enum AxisType
    {
        /// <summary>
        /// Osa X, vodorovná: na ní se pohybují sloupce, řeší se jejich Width (šířka)
        /// </summary>
        X,
        /// <summary>
        /// Osa Y, svislá: na ní se pohybují řádky, řeší se jejich Height (výška)
        /// </summary>
        Y
    }
    /// <summary>
    /// Typ vodící linie
    /// </summary>
    [Flags]
    internal enum GuideLineType
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None = 0,

        Cell = 0x0001,
        LabelBefore = 0x0002,
        Control = 0x0004,
        LabelAfter = 0x0008,
        Begin = 0x1000,
        End = 0x2000,

        /// <summary>
        /// Začátek buňky
        /// </summary>
        CellBegin = Cell | Begin,
        LabelBeforeBegin = LabelBefore | Begin,
        LabelBeforeEnd = LabelBefore | End,
        LabelAfterBegin = LabelAfter | Begin,
        LabelAfterEnd = LabelAfter | End,
        ControlBegin = Control | Begin,
        ControlEnd = Control | End,
        CellEnd = Cell | End
    }
    #endregion
    #endregion
    #region interface IControlInfoSource + IDataFormItem : Předpis rozhraní pro toho, kdo bude poskytovat informace o atributech a o rozměrech textů pro DataForm; rozhraní prvku DataFormu
    /// <summary>
    /// Předpis rozhraní pro toho, kdo bude poskytovat informace o atributech a o rozměrech textů pro DataForm.
    /// </summary>
    internal interface IControlInfoSource
    {
        /// <summary>
        /// Metoda má vrátit přeložený anebo vyplněný text
        /// </summary>
        /// <param name="formText"></param>
        /// <param name="name"></param>
        /// <param name="columnName"></param>
        /// <param name="form"></param>
        /// <returns></returns>


        zrušit;
            nahradit celkovým zpracováním jednoho controlu a předáním args;

        string TranslateText(string formText, string name, string columnName, DfForm form);

        /// <summary>
        /// Funkce, která vrátí stringový obsah nested šablony daného jména.<br/>
        /// Funkce bude volána s parametrem = jméno šablony (obsah atributu NestedTemplate), jeho úkolem je vrátit string = obsah požadované šablony (souboru).<br/>
        /// Pokud funkce požadovanou šablonu (soubor) nenajde, může sama ohlásit chybu. Anebo může vrátit null, pak bude Nested prvek ignorován.
        /// </summary>
        /// <param name="templateName"></param>
        /// <returns></returns>
        string NestedTemplateContentLoad(string templateName);
        /// <summary>
        /// Validuje (prověří a doplní) informace o konkrétním controlu.
        /// </summary>
        /// <param name="controlInfo">Data o controlu</param>
        /// <returns></returns>
        void ValidateControlInfo(IDataFormItem controlInfo);
    }
    /// <summary>
    /// Definuje potřebné vlastnosti prvku DataFormu (jednotlivý Control) pro aplikační kód, 
    /// pomocí kterého může aplikační kód doplňovat chybějící informace pro controly (typicky text popisku Main label, Tooltip, Editační styl, atd) na základě třídy a jména atributu / vztahu.
    /// </summary>
    internal interface IDataFormItem
    {
        /// <summary>
        /// Dataform, jehož layout vzniká
        /// </summary>
        DfForm DataForm { get; }
        /// <summary>
        /// Vlastní control, tak jak je deklarován v <c>frm.xml</c>
        /// </summary>
        DfBaseControl BaseControl { get; }
        /// <summary>
        /// Jméno prvku
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Jméno sloupce v datech (více prvků různého jména <see cref="Name"/> může zobrazovat data ze stejného prvku <see cref="ColumnName"/>).
        /// </summary>
        string ColumnName { get; }
        /// <summary>
        /// Typ controlu, definovaný v Form.xml
        /// </summary>
        ControlType ControlType { get; }
        /// <summary>
        /// Styl písma pro text controlu
        /// </summary>
        DfFontInfo ControlFont { get; }
        /// <summary>
        /// Fixní text v prvku, typicky v buttonu, v Checkboxu, Title, Label atd
        /// </summary>
        string ControlText { get; }
        /// <summary>
        /// Styl controlu (název, styl písma, velikost, barva popisku, barva textu a pozadí, atd)
        /// </summary>
        DfControlStyle ControlStyle { get; }
        /// <summary>
        /// Styl písma pro main label
        /// </summary>
        DfFontInfo MainLabelFont { get; }
        /// <summary>
        /// Text hlavního labelu.
        /// Pokud je null, pak není ve formuláři definováno. Pokud je "", je tím definováno 'Bez labelu'.
        /// V existující definici mohou být přítomny formátovací funkce: "fm(xxx)", "fmr(xxx)". Přípravná funkce to má vyřešit.
        /// </summary>
        string MainLabelText { get; set; }
        /// <summary>
        /// Explicitně definovaná šířka MainLabelu
        /// </summary>
        int? MainLabelWidth { get; }
        /// <summary>
        /// Styl písma pro suffix label
        /// </summary>
        DfFontInfo SuffixLabelFont { get; }
        /// <summary>
        /// Text suffix labelu. Jde o popisek vpravo od vstupního prvku, typicky obsahuje název jednotky (ks, Kč, $, kg, ...).
        /// Pokud je null, pak není ve formuláři definováno. Pokud je "", je tím definováno 'Bez labelu'.
        /// V existující definici mohou být přítomny formátovací funkce: "fm(xxx)", "fmr(xxx)". Přípravná funkce to má vyřešit.
        /// </summary>
        string SuffixLabelText { get; set; }
        /// <summary>
        /// Titulek ToolTipu.
        /// Pokud je null, pak není ve formuláři definováno. Pokud je "", je tím definováno 'Bez labelu'.
        /// V existující definici mohou být přítomny formátovací funkce: "fm(xxx)", "fmr(xxx)". Přípravná funkce to má vyřešit.
        /// </summary>
        string ToolTipTitle { get; set; }
        /// <summary>
        /// Text ToolTipu.
        /// Pokud je null, pak není ve formuláři definováno. Pokud je "", je tím definováno 'Bez labelu'.
        /// V existující definici mohou být přítomny formátovací funkce: "fm(xxx)", "fmr(xxx)". Přípravná funkce to má vyřešit.
        /// </summary>
        string ToolTipText { get; set; }
        /// <summary>
        /// Exaktně daná šířka v pixelech, v rámci Bounds prvku
        /// </summary>
        int? DesignWidthPixel { get; }
        /// <summary>
        /// Exaktně daná šířka v procentech, v rámci Bounds prvku
        /// </summary>
        int? DesignWidthPercent { get; }
        /// <summary>
        /// Exaktně daná výška v pixelech, v rámci Bounds prvku
        /// </summary>
        int? DesignHeightPixel { get; }
        /// <summary>
        /// Exaktně daná výška v procentech, v rámci Bounds prvku
        /// </summary>
        int? DesignHeightPercent { get; }
        /// <summary>
        /// Exaktně daná šířka labelu v pixelech
        /// </summary>
        int? DesignLabelWidth { get; }
        /// <summary>
        /// Pozice implicitního Main labelu
        /// </summary>
        LabelPositionType LabelPosition { get; }
        /// <summary>
        /// Existuje MainLabel (tzn. jeho text není prázdný)
        /// </summary>
        bool MainLabelExists { get; }
        /// <summary>
        /// Existuje Control (tzn. nějaký textbox nebo picture nebo button)? Nebo ne (třeba Placeholder)
        /// </summary>
        bool ControlExists { get; }
        /// <summary>
        /// Existuje SuffixLabel (tzn. jeho text není prázdný)
        /// </summary>
        bool SuffixLabelExists { get; }

        // Implicitní, dopočtené pro prvek z jeho typu, textu atd:
        /// <summary>
        /// Výchozí šířka Main labelu v pixelech, lze setovat
        /// </summary>
        int? ImplicitMainLabelWidth { get; set; }
        /// <summary>
        /// Výchozí výška Main labelu v pixelech, lze setovat
        /// </summary>
        int? ImplicitMainLabelHeight { get; set; }
        /// <summary>
        /// Výchozí minimální šířka vlastního controlu v pixelech, lze setovat.
        /// Pokud sloupec nebude mít žádnou šířku, a bude v něm tento prvek, a ten bude mít zde nastavenu určitou MinWidth, pak jeho sloupec ji bude mít nastavenu jako Implicitní.
        /// Nicméně pokud sloupec bude mít šířku větší, a prvek bude mít jen tuto MinWidth, pak prvek bude ve výsledku dimenzován na 100% reálné šířky sloupce, klidně větší než zdejší MinWidth.
        /// </summary>
        int? ImplicitControlMinimalWidth { get; set; }
        /// <summary>
        /// Výchozí optimální šířka vlastního controlu v pixelech, lze setovat.
        /// Pokud sloupec bude mít výslednou šířku větší než tato OptimalWidth, pak prvek bude ve výsledku dimenzován na tuto OptimalWidth, jako by ji zadal uživatel do Width.
        /// </summary>
        int? ImplicitControlOptimalWidth { get; set; }
        /// <summary>
        /// Výchozí výška vlastního controlu v pixelech, lze setovat.
        /// Pokud řádek nebude mít žádnou výšku, a bude v něm tento prvek, a ten bude mít zde nastavenu určitou MinHeight, pak jeho řádek ji bude mít nastavenu jako Implicitní.
        /// Nicméně pokud řádek bude mít výšku větší, a prvek bude mít jen tuto MinHeight, pak prvek bude ve výsledku dimenzován na 100% reálné výšky sloupce, klidně větší než zdejší MinHeight.
        /// </summary>
        int? ImplicitControlMinimalHeight { get; set; }
        /// <summary>
        /// Výchozí optimální výška vlastního controlu v pixelech, lze setovat.
        /// Pokud řádek bude mít výslednou výšku větší než tato OptimalHeight, pak prvek bude ve výsledku dimenzován na tuto OptimalHeight, jako by ji zadal uživatel do Height.
        /// </summary>
        int? ImplicitControlOptimalHeight { get; set; }
        /// <summary>
        /// Výchozí šířka Suffix labelu v pixelech, lze setovat
        /// </summary>
        int? ImplicitSuffixLabelWidth { get; set; }
        /// <summary>
        /// Výchozí výška Suffix labelu v pixelech, lze setovat
        /// </summary>
        int? ImplicitSuffixLabelHeight { get; set; }
    }
    /// <summary>
    /// Režim layoutu
    /// </summary>
    internal enum LayoutModeType
    {
        /// <summary>
        /// Neurčeno, bere se jako <see cref="Flow"/>
        /// </summary>
        None,
        /// <summary>
        /// <see cref="Flow"/> = umisťuje se do postupně vznikající mřížky s pevně daným počtem sloupců. 
        /// Mřížka určuje i rozložení Labelu a Controlu uvnitř buňky.
        /// </summary>
        Flow,
        /// <summary>
        /// <see cref="FlowInParent"/> = sám není součástí FlowLayoutu, ale využívá existující Parent buňku FlowLayoutu pro svoje umístění.
        /// Parent buňka je specifikována jejím jménem, odkázaným z <see cref="DfBaseControl.ParentBoundsName"/>.
        /// Uvnitř buňky je prvek umístěn zcela identicky, jako by byl součástí FlowLayoutu.
        /// Lze tak do jedné buňky vložit střídavě více prvků a řídit jejich Visible.
        /// </summary>
        FlowInParent,
        /// <summary>
        /// <see cref="BoundsInParent"/> = sám není součástí FlowLayoutu, ale využívá existující Parent buňku FlowLayoutu pro svoje umístění.
        /// Parent buňka je specifikována jejím jménem, odkázaným z <see cref="DfBaseControl.ParentBoundsName"/>.<br/>
        /// V rámci dané buňky je pak umístěn na základě svých Bounds. 
        /// Využívá tedy souřadnici odkázanou buňku, z ní načte její vnitřní souřadnice, určí počátek prostoru pro Control a ten použije jako bod 0/0,
        /// k němu přičte svoje zadané souřadnice Bounds a na výsledné místo se umístí. Může se tedy umístit i mimo prostor zadané buňky.
        /// </summary>
        BoundsInParent,
        /// <summary>
        /// <see cref="FixedAbsolute"/> Fixní s přesně danými souřadnicemi X,Y vzhledem k parent containeru. Bez vztahu k FlowLayoutu.
        /// </summary>
        FixedAbsolute
    }
    #endregion
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
        /// Mají se ukládat Debug obrázky? Pak budou jejich jména uložena v <see cref="DebugImages"/>
        /// </summary>
        public bool SaveDebugImages { get; set; }
        /// <summary>
        /// Do Debug obrázku vykreslovat i vodící linky?
        /// </summary>
        public bool DebugImagesWithGuideLines { get; set; }
        /// <summary>
        /// Podadresář pro DebugImages v rámci Windows Temp adresáře
        /// </summary>
        public string DebugImagePath { get; set; }
        /// <summary>
        /// Debug obrázky, pokud <see cref="SaveDebugImages"/> je true
        /// </summary>
        public List<string> DebugImages { get; set; }
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
            return _GetDefaultSize(height, DefaultFontHeightRatio, DefaultFontHeightAdd);
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
            return _GetDefaultSize(width, DefaultFontWidthRatio, DefaultFontWidthAdd);
        }
        /// <summary>
        /// Vrátí dodanou velikost převedenou na aktuální výchozí velikost fontu 
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ratio"></param>
        /// <param name="add"></param>
        /// <returns></returns>
        private static int _GetDefaultSize(float size, float ratio, float add)
        {
            return (int)(Math.Ceiling((size * ratio * DefaultFontEmSize / _FontEmSize) + add));
        }
        /// <summary>
        /// Standardní velikost fontu
        /// </summary>
        internal const float DefaultFontEmSize = 8.25f;
        internal const float DefaultFontHeightRatio = 0.66667f;
        internal const float DefaultFontHeightAdd = 2.00f;
        internal const float DefaultFontWidthRatio = 0.66667f;
        internal const float DefaultFontWidthAdd = 2.00f;
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
