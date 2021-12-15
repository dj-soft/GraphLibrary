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

}
