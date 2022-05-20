// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using System.Windows.Forms;
using System.Drawing;

using DevExpress.Utils;
using System.Drawing.Drawing2D;
using DevExpress.Pdf.Native;
using DevExpress.XtraPdfViewer;
using DevExpress.XtraEditors;
using DevExpress.Office.History;
using System.Diagnostics;
using DevExpress.XtraEditors.Filtering.Templates;
using DevExpress.Utils.Extensions;
using DevExpress.XtraEditors.ViewInfo;

namespace Noris.Clients.Win.Components.AsolDX
{
    #region DxSyntaxEditControl

    public class DxSyntaxEditControl : DxRichEditControl
    {
        public DxSyntaxEditControl()
        {
            this.ActiveViewType = DevExpress.XtraRichEdit.RichEditViewType.Simple;
            _SyntaxService = new DxSyntaxEditService(this);
            this.ReplaceService<DevExpress.XtraRichEdit.Services.ISyntaxHighlightService>(_SyntaxService);
        }
        private DxSyntaxEditService _SyntaxService;
        /// <summary>
        /// Pravidla syntaxe
        /// </summary>
        internal Noris.WS.Parser.NetParser.Setting SyntaxRules { get { return _SyntaxRules; } set { _SyntaxRules = value; _SyntaxService.ExecuteSyntax(); } }
        private Noris.WS.Parser.NetParser.Setting _SyntaxRules;

    }
    /// <summary>
    /// Služba pro obarvení syntaxe
    /// </summary>
    public class DxSyntaxEditService : DevExpress.XtraRichEdit.Services.ISyntaxHighlightService
    {
        public DxSyntaxEditService(DxSyntaxEditControl dxSyntaxEditControl)
        {
            this._DxSyntaxEditControl = dxSyntaxEditControl;
            //syntaxColors = new SyntaxColors(UserLookAndFeel.Default);
        }
        readonly DxSyntaxEditControl _DxSyntaxEditControl;

        #region #ISyntaxHighlightServiceMembers
        void DevExpress.XtraRichEdit.Services.ISyntaxHighlightService.Execute()
        {
            _ExecuteSyntax();
        }

        void DevExpress.XtraRichEdit.Services.ISyntaxHighlightService.ForceExecute()
        {
            _ExecuteSyntax();
        }
        public void ExecuteSyntax() { _ExecuteSyntax(); }
        void _ExecuteSyntax()
        {
            string newText = _DxSyntaxEditControl.Text;
            if (newText.Length < 10 || !newText.EndsWith(";")) return;
            var syntaxRules = _DxSyntaxEditControl.SyntaxRules;
            if (syntaxRules is null) return;

            var parsedItem = Noris.WS.Parser.NetParser.Parser.ParseText(newText, syntaxRules);

            // Use DevExpress.CodeParser to parse text into tokens.
            // ITokenCategoryHelper tokenHelper = TokenCategoryHelperFactory.CreateHelper(lang_ID);
            DevExpress.CodeParser.ITokenCollection iTokens = null;
            HighlightSyntax(iTokens);
        }
        void HighlightSyntax(DevExpress.CodeParser.ITokenCollection tokens)
        {
            string text = _DxSyntaxEditControl.Text;
            if (text.Length < 10) return;

            var syntaxTokens = new List<DevExpress.XtraRichEdit.API.Native.SyntaxHighlightToken>();

            var propsGreen = new DevExpress.XtraRichEdit.API.Native.SyntaxHighlightProperties() { ForeColor = Color.DarkGreen };
            var propsBlue = new DevExpress.XtraRichEdit.API.Native.SyntaxHighlightProperties() { ForeColor = Color.DarkBlue };
            syntaxTokens.Add(new DevExpress.XtraRichEdit.API.Native.SyntaxHighlightToken(0, 5, propsGreen));
            syntaxTokens.Add(new DevExpress.XtraRichEdit.API.Native.SyntaxHighlightToken(5, 3, propsBlue));

            var document = _DxSyntaxEditControl.Document;
            document.ApplySyntaxHighlight(syntaxTokens);
        }
        #endregion


        //SyntaxColors syntaxColors;
        //SyntaxHighlightProperties commentProperties;
        //SyntaxHighlightProperties keywordProperties;
        //SyntaxHighlightProperties stringProperties;
        //SyntaxHighlightProperties xmlCommentProperties;
        //SyntaxHighlightProperties textProperties;



        //void HighlightSyntax(TokenCollection tokens)
        //{
        //    commentProperties = new SyntaxHighlightProperties();
        //    commentProperties.ForeColor = syntaxColors.CommentColor;

        //    keywordProperties = new SyntaxHighlightProperties();
        //    keywordProperties.ForeColor = syntaxColors.KeywordColor;

        //    stringProperties = new SyntaxHighlightProperties();
        //    stringProperties.ForeColor = syntaxColors.StringColor;

        //    xmlCommentProperties = new SyntaxHighlightProperties();
        //    xmlCommentProperties.ForeColor = syntaxColors.XmlCommentColor;

        //    textProperties = new SyntaxHighlightProperties();
        //    textProperties.ForeColor = syntaxColors.TextColor;

        //    if (tokens == null || tokens.Count == 0)
        //        return;

        //    Document document = _DxSyntaxEditControl.Document;
        //    //CharacterProperties cp = document.BeginUpdateCharacters(0, 1);
        //    List<SyntaxHighlightToken> syntaxTokens = new List<SyntaxHighlightToken>(tokens.Count);
        //    foreach (Token token in tokens)
        //    {
        //        HighlightCategorizedToken((CategorizedToken)token, syntaxTokens);
        //    }
        //    document.ApplySyntaxHighlight(syntaxTokens);
        //    //document.EndUpdateCharacters(cp);
        //}
        //void HighlightCategorizedToken(CategorizedToken token, List<SyntaxHighlightToken> syntaxTokens)
        //{
        //    Color backColor = _DxSyntaxEditControl.ActiveView.BackColor;
        //    TokenCategory category = token.Category;
        //    if (category == TokenCategory.Comment)
        //        syntaxTokens.Add(SetTokenColor(token, commentProperties, backColor));
        //    else if (category == TokenCategory.Keyword)
        //        syntaxTokens.Add(SetTokenColor(token, keywordProperties, backColor));
        //    else if (category == TokenCategory.String)
        //        syntaxTokens.Add(SetTokenColor(token, stringProperties, backColor));
        //    else if (category == TokenCategory.XmlComment)
        //        syntaxTokens.Add(SetTokenColor(token, xmlCommentProperties, backColor));
        //    else
        //        syntaxTokens.Add(SetTokenColor(token, textProperties, backColor));
        //}
        //SyntaxHighlightToken SetTokenColor(Token token, SyntaxHighlightProperties foreColor, Color backColor)
        //{
        //    if (_DxSyntaxEditControl.Document.Paragraphs.Count < token.Range.Start.Line)
        //        return null;
        //    int paragraphStart = DocumentHelper.GetParagraphStart(_DxSyntaxEditControl.Document.Paragraphs[token.Range.Start.Line - 1]);
        //    int tokenStart = paragraphStart + token.Range.Start.Offset - 1;
        //    if (token.Range.End.Line != token.Range.Start.Line)
        //        paragraphStart = DocumentHelper.GetParagraphStart(_DxSyntaxEditControl.Document.Paragraphs[token.Range.End.Line - 1]);

        //    int tokenEnd = paragraphStart + token.Range.End.Offset - 1;
        //    Debug.Assert(tokenEnd > tokenStart);
        //    return new SyntaxHighlightToken(tokenStart, tokenEnd - tokenStart, foreColor);
        //}


    }

    // <summary>
    //  This class provides colors to highlight the tokens.
    // </summary>
    //public class SyntaxColors
    //{
    //    static Color DefaultCommentColor { get { return Color.Green; } }
    //    static Color DefaultKeywordColor { get { return Color.Blue; } }
    //    static Color DefaultStringColor { get { return Color.Brown; } }
    //    static Color DefaultXmlCommentColor { get { return Color.Gray; } }
    //    static Color DefaultTextColor { get { return Color.Black; } }
    //    UserLookAndFeel lookAndFeel;

    //    public Color CommentColor { get { return GetCommonColorByName(CommonSkins.SkinInformationColor, DefaultCommentColor); } }
    //    public Color KeywordColor { get { return GetCommonColorByName(CommonSkins.SkinQuestionColor, DefaultKeywordColor); } }
    //    public Color TextColor { get { return GetCommonColorByName(CommonColors.WindowText, DefaultTextColor); } }
    //    public Color XmlCommentColor { get { return GetCommonColorByName(CommonColors.DisabledText, DefaultXmlCommentColor); } }
    //    public Color StringColor { get { return GetCommonColorByName(CommonSkins.SkinWarningColor, DefaultStringColor); } }

    //    public SyntaxColors(UserLookAndFeel lookAndFeel)
    //    {
    //        this.lookAndFeel = lookAndFeel;
    //    }

    //    Color GetCommonColorByName(string colorName, Color defaultColor)
    //    {
    //        Skin skin = CommonSkins.GetSkin(lookAndFeel);
    //        if (skin == null)
    //            return defaultColor;
    //        return skin.Colors[colorName];
    //    }
    //}
    #endregion
    #region ComboCodeTable
    /// <summary>
    /// Tabulka obsahující položky editačního stylu.
    /// <para/>
    /// Nejprve je třeba objekt <see cref="ComboCodeTable"/> vytvořit - do konstruktoru se předá pole s jednotlivými hodnotami <see cref="ComboCodeTable.Item"/>.
    /// <para/>
    /// Následně se z této instance <see cref="ComboCodeTable"/> získá prvek <see cref="DevExpress.XtraEditors.Repository.RepositoryItemImageComboBox"/>, z metody <see cref="CreateRepositoryCombo"/>.<br/>
    /// Tento prvek se uloží do knihovny repozitory v <see cref="DevExpress.XtraGrid.GridControl"/>.RepositoryItems;<br/>
    /// a hlavně se vloží do odpovídajícího sloupce kde má být zobrazován do jeho property <see cref="DevExpress.XtraGrid.Columns.GridColumn"/>.ColumnEdit;<br/>
    /// Tím je zajištěno vykreslování ComboBoxu v řádkovém filtru a odpovídající zobrazení v buňkách Gridu.
    /// <para/>
    /// Pro barevné podkreslení buňky v Gridu je třeba použít tento postup:
    /// Zaregistrovat si svůj nový eventhandler na události View.CustomDrawCell;<br/>
    /// V tomto eventhandleru detekovat, že se kreslí sloupec s konkrétním editačním stylem (podle jména sloupce, nebo mít uložený objekt <see cref="ComboCodeTable"/> v Column.Tag;<br/>
    /// Jednoduše z eventhandleru převolat metodu <see cref="ComboCodeTable.DrawStatusCellCodeTable(object, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs)"/>;
    /// </summary>
    internal class ComboCodeTable
    {
        #region Metody ComboCodeTable (konstruktor, správa prvků, tvorba RepositoryItemImageComboBox, vykreslení buňky
        /// <summary>
        /// Konstruktor.
        /// <para/>
        /// Nejprve je třeba objekt <see cref="ComboCodeTable"/> vytvořit - do konstruktoru se předá pole s jednotlivými hodnotami <see cref="ComboCodeTable.Item"/>.
        /// <para/>
        /// Následně se z této instance <see cref="ComboCodeTable"/> získá prvek <see cref="DevExpress.XtraEditors.Repository.RepositoryItemImageComboBox"/>, z metody <see cref="CreateRepositoryCombo"/>.<br/>
        /// Tento prvek se uloží do knihovny repozitory v <see cref="DevExpress.XtraGrid.GridControl"/>.RepositoryItems;<br/>
        /// a hlavně se vloží do odpovídajícího sloupce kde má být zobrazován do jeho property <see cref="DevExpress.XtraGrid.Columns.GridColumn"/>.ColumnEdit;<br/>
        /// Tím je zajištěno vykreslování ComboBoxu v řádkovém filtru a odpovídající zobrazení v buňkách Gridu.
        /// <para/>
        /// Pro barevné podkreslení buňky v Gridu je třeba použít tento postup:
        /// Zaregistrovat si svůj nový eventhandler na události View.CustomDrawCell;<br/>
        /// V tomto eventhandleru detekovat, že se kreslí sloupec s konkrétním editačním stylem (podle jména sloupce, nebo mít uložený objekt <see cref="ComboCodeTable"/> v Column.Tag;<br/>
        /// Jednoduše z eventhandleru převolat metodu <see cref="ComboCodeTable.DrawStatusCellCodeTable(object, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs)"/>;
        /// </summary>
        /// <param name="items"></param>
        public ComboCodeTable(params Item[] items)
        {
            _ItemsDict = items.CreateDictionary(i => i.Value);
            GetValidResourceType();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"CodeTable; {ItemsCount} items.";
        }
        /// <summary>
        /// Prvky editačního stylu
        /// </summary>
        public Item[] Items { get { return _ItemsDict.Values.ToArray(); } }
        /// <summary>
        /// Vzhled buňky, která obsahuje hodnotu Value, která není zadaná v editačním stylu. Lze tak definovat ikonu i barvy. Jako text bude vždy zobrazena databázová hodnota.
        /// </summary>
        public Item NotFoundItemStyle { get; set; }
        /// <summary>
        /// Počet prvků editačního stylu
        /// </summary>
        public int ItemsCount { get { return this._ItemsDict.Count; } }
        /// <summary>
        /// Prvky jsou ulženy v Dictionary
        /// </summary>
        private Dictionary<object, Item> _ItemsDict;
        /// <summary>
        /// Zde je vytvořena a vrácena instance prvku Repository, která obsahuje Combo box obsahující zdejší hodnoty.
        /// <para/>
        /// Tento prvek se uloží do knihovny repozitory v <see cref="DevExpress.XtraGrid.GridControl"/>.RepositoryItems;<br/>
        /// a hlavně se vloží do odpovídajícího sloupce kde má být zobrazován do jeho property <see cref="DevExpress.XtraGrid.Columns.GridColumn"/>.ColumnEdit;<br/>
        /// Tím je zajištěno vykreslování ComboBoxu v řádkovém filtru a odpovídající zobrazení v buňkách Gridu.
        /// </summary>
        /// <returns></returns>
        public DevExpress.XtraEditors.Repository.RepositoryItemImageComboBox CreateRepositoryCombo()
        {
            DevExpress.XtraEditors.Repository.RepositoryItemImageComboBox repoCombo = new DevExpress.XtraEditors.Repository.RepositoryItemImageComboBox();
            var resourceType = GetValidResourceType();
            var imageCollection = GetImageCollection(ResourceImageSizeType.Small, resourceType);
            repoCombo.SmallImages = imageCollection;

            // repoCombo.ImmediatePopup = false;
            // repoCombo.NullValuePrompt = "Vyberte...";
            // repoCombo.PopupSizeable = true;
            // repoCombo.ReadOnly = false;
            repoCombo.ShowDropDown = DevExpress.XtraEditors.Controls.ShowDropDown.SingleClick;

            foreach (var item in _ItemsDict.Values)
                repoCombo.Items.Add(item.CreateComboItem(ResourceImageSizeType.Small, resourceType));
            int count = ItemsCount;
            repoCombo.DropDownRows = (count < 3 ? 3 : count < 12 ? count : 12);
            return repoCombo;
        }
        /// <summary>
        /// Pole obsahující Distinct typy ikon.
        /// Zde lze kontrolovat, že jsou zadány ikony shodného typu (pak má pole jen jeden prvek).
        /// Prvky bez ikony jsou ignorovány = pokud žádný prvek nemá definovanou ikonu, je zde pole o délce 0.
        /// </summary>
        private ResourceContentType[] ResourceTypes { get { return this._ItemsDict.Values.Select(i => i.ResourceType).Where(t => t != ResourceContentType.None).Distinct().ToArray(); } }
        /// <summary>
        /// Metoda vrátí použitelný typ ikon. 
        /// Pokud je zadán mix (více typů = vektor i bitmapy), vyhodí tato metoda chybu! 
        /// Pak by stejně nebyl ComboBox použitelný. Může mít jen jeden typ ikon.
        /// </summary>
        /// <returns></returns>
        private ResourceContentType GetValidResourceType()
        {
            var resourceTypes = ResourceTypes;
            if (resourceTypes.Length == 0) return ResourceContentType.None;
            if (resourceTypes.Length > 1) throw new InvalidOperationException($"Nelze v jedné CodeTable kombinovat různé typy ikon (bitmapy a vektory), aktuálně jsou detekovány: {resourceTypes.ToOneString(",")}.");
            return resourceTypes[0];
        }
        /// <summary>
        /// Vrátí ImageList pro daný druh ikon (vektor / bitmapa)
        /// </summary>
        /// <param name="sizeType"></param>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        private object GetImageCollection(ResourceImageSizeType sizeType, ResourceContentType resourceType)
        {
            switch (resourceType)
            {
                case ResourceContentType.None:
                    return null;
                case ResourceContentType.Bitmap:
                    return DxComponent.GetBitmapImageList(sizeType);
                case ResourceContentType.Vector:
                    return DxComponent.GetVectorImageList(sizeType);
                default:
                    throw new InvalidOperationException($"V CodeTable nelze použít jako ikony druh zdroje '{resourceType}'.");
            }
        }
        /// <summary>
        /// Zajistí vykreslení buňky Gridu s vlastnostmi, které definuje editační styl (barvy pozadí, textu, styl písma).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DrawStatusCellCodeTable(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            object value = e.CellValue;
            if (value is null || value is System.DBNull) return;
            if (_ItemsDict.TryGetValue(value, out var item))
                item.DrawStatusCellCodeTable(sender, e);
            else
                this.DrawStatusCellCodeTable(sender, e, value);
        }
        /// <summary>
        /// Použije se pro vykreslení buňky, jejíž hodnota nebyla nalezena jako platná položka editačního stylu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="value"></param>
        private void DrawStatusCellCodeTable(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e, object value)
        {
            e.DisplayText = value.ToString();
            if (this.NotFoundItemStyle != null)
            {
                this.NotFoundItemStyle.DrawStatusCellCodeTable(sender, e);

            }
        }
        #endregion
        #region class Item : Jeden prvek CodeTable
        /// <summary>
        /// Jeden prvek CodeTable
        /// </summary>
        public class Item
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="value"></param>
            /// <param name="displayText"></param>
            /// <param name="iconName"></param>
            /// <param name="backColor1"></param>
            /// <param name="backColor2"></param>
            /// <param name="textColor"></param>
            /// <param name="textStyle"></param>
            public Item(object value, string displayText, string iconName = null, Color? backColor1 = null, Color? backColor2 = null, Color? textColor = null, FontStyle? textStyle = null)
            {
                Value = value;
                DisplayText = displayText;
                IconName = iconName;
                BackColor1 = backColor1;
                BackColor2 = backColor2;
                TextColor = textColor;
                TextStyle = textStyle;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Value: '{Value}'; DisplayText: '{DisplayText}'";
            }
            /// <summary>
            /// Databázová hodnota
            /// </summary>
            public object Value { get; private set; }
            /// <summary>
            /// Zobrazený text
            /// </summary>
            public string DisplayText { get; private set; }
            /// <summary>
            /// Jméno ikony
            /// </summary>
            public string IconName { get; private set; }
            /// <summary>
            /// Barva pozadí, při kombinaci s <see cref="BackColor2"/> jde o barvu vlevo
            /// </summary>
            public Color? BackColor1 { get; private set; }
            /// <summary>
            /// Barva pozadí, vpravo
            /// </summary>
            public Color? BackColor2 { get; private set; }
            /// <summary>
            /// Barva písma
            /// </summary>
            public Color? TextColor { get; private set; }
            /// <summary>
            /// Styl písma
            /// </summary>
            public FontStyle? TextStyle { get; private set; }
            /// <summary>
            /// Druh obrázku určený podle přípony <see cref="IconName"/>
            /// </summary>
            public ResourceContentType ResourceType
            {
                get
                {
                    string iconName = this.IconName;
                    if (String.IsNullOrEmpty(iconName)) return ResourceContentType.None;
                    string extension = System.IO.Path.GetExtension(iconName);
                    if (String.IsNullOrEmpty(extension)) return ResourceContentType.None;
                    return DxComponent.GetContentTypeFromExtension(extension);
                }
            }
            /// <summary>
            /// Vytvoří a vrátí prvek ComboBoxu
            /// </summary>
            /// <param name="sizeType"></param>
            /// <param name="resourceType"></param>
            /// <returns></returns>
            public DevExpress.XtraEditors.Controls.ImageComboBoxItem CreateComboItem(ResourceImageSizeType sizeType, ResourceContentType resourceType)
            {
                int imageIndex = GetImageIndex(sizeType, resourceType);
                var comboItem = new DevExpress.XtraEditors.Controls.ImageComboBoxItem(this.DisplayText, this.Value, imageIndex);
                return comboItem;
            }
            /// <summary>
            /// Najde nebo přidá a vrátí index zdejšího obrázku v odpovídající kolekci
            /// </summary>
            /// <param name="sizeType"></param>
            /// <param name="resourceType"></param>
            /// <returns></returns>
            private int GetImageIndex(ResourceImageSizeType sizeType, ResourceContentType resourceType)
            {
                string imageName = this.IconName;
                if (!String.IsNullOrEmpty(imageName))
                {
                    switch (resourceType)
                    {
                        case ResourceContentType.Bitmap:
                            return DxComponent.GetBitmapImageIndex(imageName, sizeType);
                        case ResourceContentType.Vector:
                            return DxComponent.GetVectorImageIndex(imageName, sizeType);
                    }
                }
                return -1;
            }
            /// <summary>
            /// Vykreslí prvek s použitím zdejší definice barev a stylu
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            public void DrawStatusCellCodeTable(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
            {
                if (this.BackColor1.HasValue) e.Appearance.BackColor = this.BackColor1.Value;
                if (this.BackColor2.HasValue) e.Appearance.BackColor2 = this.BackColor2.Value;
                if (this.BackColor1.HasValue && this.BackColor2.HasValue) e.Appearance.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
                if (this.TextColor.HasValue) e.Appearance.ForeColor = this.TextColor.Value;
                if (this.TextStyle.HasValue) e.Appearance.FontStyleDelta = this.TextStyle.Value;

                e.DefaultDraw();
                e.Handled = true;
            }
        }
        #endregion
    }
    #endregion
}
