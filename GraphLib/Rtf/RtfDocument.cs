using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using Parsing = Asol.Tools.WorkScheduler.Data.Parsing;


namespace Asol.Tools.WorkScheduler.Rtf
{
    /// <summary>
    /// Logický obraz RTF dokumentu
    /// </summary>
    public class RtfDocument
    {
        /// <summary>
        /// Konstruktor dokumentu
        /// </summary>
        public RtfDocument()
        { }
        /// <summary>
        /// RTF text dokumentu
        /// </summary>
        public string RtfText
        {
            get
            {
                return this._RtfText;
            }
            set
            {
                this.InvalidateDocument();
                this._RtfText = value;
                this._LoadRtfText();
            }
        }
        private string _RtfText;
        /// <summary>
        /// Invaliduje obsah dokumentu, volá se po vložení nového RTF obsahu
        /// </summary>
        private void InvalidateDocument()
        {
            this._RtfText = null;
            this._Content = new List<RtfContent>();
            this._Fonts = new Dictionary<string, RtfFont>();
            this._Colors = new Dictionary<string, RtfColor>();
            this._RtfErrors = "";
        }
        /// <summary>
        /// Načte a analyzuje dodaný RTF text
        /// </summary>
        private void _LoadRtfText()
        {
            if (String.IsNullOrEmpty(this._RtfText)) return;

            Parsing.ParsedItem rootItem = Parsing.Parser.ParseString(this._RtfText, Parsing.DefaultSettings.Rtf);
            this._ProcessRtfDocument(rootItem);
        }
        /// <summary>
        /// Analyzuje parsovaný RTF text
        /// </summary>
        /// <param name="rootItem"></param>
        private void _ProcessRtfDocument(Parsing.ParsedItem rootItem)
        {
            if (rootItem == null || rootItem.ItemCount <= 0) return;

            foreach (Parsing.ParsedItem item in rootItem.Items)
            {
                switch (item.ItemType)
                {
                    case Data.Parsing.ItemType.None:
                    case Data.Parsing.ItemType.Blank:
                        break;
                    case Data.Parsing.ItemType.Text:
                        this._ProcessRtfText(item);
                        break;
                    case Data.Parsing.ItemType.Delimiter:
                        break;
                    case Data.Parsing.ItemType.Array:
                        if (item.HasItems)
                        {
                            switch (item.SegmentName)
                            {
                                case Parsing.DefaultSettings.RTF_ENTITY:
                                    this._ProcessRtfEntity(item);
                                    break;
                                case Parsing.DefaultSettings.RTF_BLOCK:
                                    this._ProcessRtfBlock(item);
                                    break;
                                case Parsing.DefaultSettings.RTF_CHAR2:
                                    this._ProcessRtfChar2(item);
                                    break;
                                case Parsing.DefaultSettings.RTF_CHARUNICODE:
                                    this._ProcessRtfCharUnicode(item);
                                    break;
                                default:
                                    this._ProcessAddRtfError("Segment", item.SegmentName);
                                    break;
                            }
                        }
                        break;
                    default:
                        this._ProcessAddRtfError("ItemType", item.ItemType.ToString());
                        break;
                }
            }
        }

        private void _ProcessRtfEntity(Parsing.ParsedItem item)
        {
            if (!item.HasItems) return;
            foreach (Parsing.ParsedItem subItem in item.Items)
            {
                string rtfValue;
                string rtfEntity = GetRtfEntity(subItem, out rtfValue);
                switch (rtfEntity)
                {
                    case "viewkind":


                        break;
                }
            }
            
        }
        #region Parse RTF inner blocks
        private void _ProcessRtfBlock(Parsing.ParsedItem item)
        {
            if (!item.HasItems) return;
            string rtfEntity = GetRtfEntityFirst(item);
            switch (rtfEntity)
            {
                case "fonttbl":
                    this._ProcessRtfBlockFontTable(item);
                    break;
                case "colortbl":
                    this._ProcessRtfBlockFColor(item);
                    break;
                default:
                    this._ProcessAddRtfError("Block", rtfEntity);
                    break;
            }
        }
        #endregion
        #region Common helper methods
        /// <summary>
        /// Find and return parsed segment for RTF document.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private Parsing.ParsedItem _GetMainRtfSegment(Parsing.ParsedItem item)
        {
            Parsing.ParsedItem rtfItem = null;
            if (item.SegmentName == Parsing.DefaultSettings.RTF_NONE && item.HasItems)
                rtfItem = item;
            else if (item.HasItems && item.Items[0].SegmentName == Parsing.DefaultSettings.RTF_NONE && item.Items[0].HasItems)
                rtfItem = item.Items[0];
            return rtfItem;
        }
        /// <summary>
        /// Split value (for example: "charset238") to name ("charset") and numeric value ("238").
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static string GetRtfEntity(Parsing.ParsedItem item)
        {
            string rtfValue;
            return GetRtfEntity(item, out rtfValue);
        }
        private static string GetRtfEntity(Parsing.ParsedItem item, out string rtfValue)
        {
            rtfValue = "";
            if (item == null) return "";
            string rtfEntity = item.Text;
            int firstValue = rtfEntity.IndexOfAny(ValuesArrayNumSpace);
            if (firstValue > 0)
            {
                rtfValue = rtfEntity.Substring(firstValue);
                rtfEntity = rtfEntity.Substring(0, firstValue);
            }
            return rtfEntity;
        }
        /// <summary>
        /// Array of char: 0 1 2 3 4 5 6 7 8 9
        /// </summary>
        private static char[] ValuesArrayNum { get { if (_ValuesArrayNum == null) _ValuesArrayNum = "0123456789".ToArray(); return _ValuesArrayNum; } } private static char[] _ValuesArrayNum;
        /// <summary>
        /// Array of char: {Space} 0 1 2 3 4 5 6 7 8 9
        /// </summary>
        private static char[] ValuesArrayNumSpace { get { if (_ValuesArrayNumSpace == null) _ValuesArrayNumSpace = " 0123456789".ToArray(); return _ValuesArrayNumSpace; } } private static char[] _ValuesArrayNumSpace;
        private static string GetRtfEntityFirst(Parsing.ParsedItem item)
        {
            Parsing.ParsedItem foundValue = GetFirstParserValue(item);
            return GetRtfEntity(foundValue);
        }
        private static string GetRtfEntityFirst(Parsing.ParsedItem item, out string rtfValue)
        {
            Parsing.ParsedItem foundValue = GetFirstParserValue(item);
            return GetRtfEntity(foundValue, out rtfValue);
        }
        private static Parsing.ParsedItem GetFirstParserValue(Parsing.ParsedItem item)
        {
            // Search for First-First-First-... value:
            Parsing.ParsedItem scanItem = item;
            while (true)
            {
                if (!scanItem.HasItems) break;
                scanItem = scanItem.Items[0];
            }
            return scanItem;
        }
        /// <summary>
        /// Třída pro uchování názvu a hodnoty
        /// </summary>
        protected struct NameValue
        {

            private string _Name;
            private string _Value;
            private int? _ValueInt;
            /// <summary>
            /// Jméno
            /// </summary>
            public string Name { get { return this._Name; } }
            /// <summary>
            /// Hodnota
            /// </summary>
            public string Value { get { return this._Value; } }
            /// <summary>
            /// Hodnota jako číslo
            /// </summary>
            public int? ValueInt { get { return this._ValueInt; } }
        }
        #endregion
        #region Content
        /// <summary>
        /// Zpracuje textový obsah prvku
        /// </summary>
        /// <param name="item"></param>
        private void _ProcessRtfText(Parsing.ParsedItem item)
        {
            
        }

        private void _ProcessRtfChar2(Parsing.ParsedItem parserSegment)
        {
           
        }

        private void _ProcessRtfCharUnicode(Parsing.ParsedItem parserSegment)
        {
            
        }
        /// <summary>
        /// Soupis prvků obsahu
        /// </summary>
        protected List<RtfContent> _Content;
        /// <summary>
        /// Jeden prvek obsahu
        /// </summary>
        protected class RtfContent
        { }
        #endregion
        #region Fonts
        /// <summary>
        /// Process FontTable (=all fonts) from segment (parameter)
        /// </summary>
        /// <param name="item"></param>
        private void _ProcessRtfBlockFontTable(Parsing.ParsedItem item)
        {   // for example:  \fonttbl{\f0\fswiss\fcharset238{\*\fname Arial;}Arial CE;}{\f1\fmodern\fprq1\fcharset238 Envy Code R;}...{\f6\fnil\fcharset2 Symbol;}
            //               \fonttbl{\f0\fswiss\fcharset238{\*\fname Arial;}Arial CE;}{\f1\fmodern\fprq1\fcharset238 Envy Code R;}{\f2\fmodern\fprq1\fcharset0 Envy Code R;}{\f3\fnil\fprq2\fcharset238 Gentium Basic;}{\f4\fnil\fprq2\fcharset0 Gentium Basic;}{\f5\fnil\fcharset0 ;}{\f6\fnil\fcharset2 Symbol;}
            if (item.ItemCount < 1) return;
            for (int i = 1 /* Skip index [0], contains value with keyword "fonttbl" ! */; i < item.ItemCount; i++)
            {
                Parsing.ParsedItem subItem = item.Items[i];
                if (subItem.HasItems)
                    this._ProcessRtfBlockFontOne(subItem);
            }
        }
        /// <summary>
        /// Process one font
        /// </summary>
        /// <param name="item"></param>
        private void _ProcessRtfBlockFontOne(Parsing.ParsedItem item)
        {   // for example: \f0\fswiss\fcharset238{\*\fname Arial;}Arial CE;
            //              \f2\fmodern\fprq1\fcharset0 Envy Code R;
            if (!item.HasItems) return;
            RtfFont rtfFont = new RtfFont();
            for (int index = 0; index < item.ItemCount; index++)
            {
                Parsing.ParsedItem subItem = item.Items[index];
                Parsing.IParsedItemExtended eSubItem = subItem as Parsing.IParsedItemExtended;
                if (subItem.HasItems)
                {
                    if (index == 0)
                    {   // Font number in table:
                        rtfFont.Key = item.TextInner;                                    // f2
                    }
                    else
                    {
                        string rtfValue;
                        string rtfEntity = GetRtfEntityFirst(subItem, out rtfValue);     // "fswiss", "fcharset"+"238", 
                        switch (rtfEntity)
                        {
                            case "fswiss":
                            case "fmodern":
                            case "fnil":
                                rtfFont.Family = rtfEntity;
                                break;
                            case "fprq":
                                break;
                            case "fcharset":
                                rtfFont.CharSet = rtfValue;
                                this._ProcessRtfBlockFontName(item, ref index, rtfFont);
                                break;
                            default:
                                this._ProcessAddRtfError("FontItem", rtfEntity);
                                break;
                        }
                    }
                }
            }

            if (rtfFont.IsValid)
            {
                if (this._Fonts.ContainsKey(rtfFont.Key))
                    this._Fonts[rtfFont.Key] = rtfFont;
                else
                    this._Fonts.Add(rtfFont.Key, rtfFont);
            }
        }
        /// <summary>
        /// Process font name
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        /// <param name="rtfFont"></param>
        private void _ProcessRtfBlockFontName(Parsing.ParsedItem item, ref int index, RtfFont rtfFont)
        {
            index++;
            while (index < item.ItemCount)
            {
                Parsing.ParsedItem subItem = item.Items[index];
                index++;
                if (subItem.ItemType == Data.Parsing.ItemType.Text)
                {
                    rtfFont.Name = subItem.Text.TrimEnd(';', ' ');
                    break;
                }
            }
        }
        /// <summary>
        /// All fonts from fonttable
        /// </summary>
        protected Dictionary<string, RtfFont> _Fonts;
        /// <summary>
        /// One font
        /// </summary>
        protected class RtfFont
        {
            /// <summary>
            /// Vizualizace fontu
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.Key + ": " + this.Name + " (charset: " + this.CharSet + "; family: " + this.Family + ")";
            }
            /// <summary>
            /// Key of this font
            /// </summary>
            public string Key { get; set; }
            /// <summary>
            /// Family name of this font
            /// </summary>
            public string Family { get; set; }
            /// <summary>
            /// CharSet of this font
            /// </summary>
            public string CharSet { get; set; }
            /// <summary>
            /// System name of this font
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// true for valid font
            /// </summary>
            public bool IsValid { get { return (!String.IsNullOrEmpty(this.Key) && !String.IsNullOrEmpty(this.Name)); } }
        }
        #endregion
        #region Colors
        private void _ProcessRtfBlockFColor(Parsing.ParsedItem item)
        {   // For example:   {\colortbl ;\red255\green0\blue0;\red0\green128\blue0;\red0\green0\blue255;}
            int colorIndex = 0;
            int index = 2;
            int count = item.ItemCount;
            while (index < count)
            {
                if ((index + 2) >= count) break;
                this._ProcessRtfBlockFColorOne(item, ref index, ref colorIndex);
            }
        }

        private void _ProcessRtfBlockFColorOne(Parsing.ParsedItem item, ref int index, ref int colorIndex)
        {
            int r, g, b;

            if (!this._ProcessRtfBlockFColorValue(item, ref index, "red", out r)) return;
            if (!this._ProcessRtfBlockFColorValue(item, ref index, "green", out g)) return;
            if (!this._ProcessRtfBlockFColorValue(item, ref index, "blue", out b)) return;
            
            RtfColor rtfColor = new RtfColor();
            rtfColor.Key = (++colorIndex).ToString();
            rtfColor.Color = Color.FromArgb(255, r, g, b);
            this._Colors.Add(rtfColor.Key, rtfColor);
        }

        private bool _ProcessRtfBlockFColorValue(Parsing.ParsedItem item, ref int index, string name, out int value)
        {
            value = 0;
            string rtfEntity, rtfValue;
            rtfEntity = GetRtfEntity(item.Items[index++], out rtfValue);     // "", "red"+"255", 
            if (rtfEntity != name) return false;
            if (!Int32.TryParse(rtfValue, out value)) return false;
            return (value >= 0 && value <= 255);
        }
        private Dictionary<string, RtfColor> _Colors;
        /// <summary>
        /// RTF barva
        /// </summary>
        protected class RtfColor
        {
            /// <summary>
            /// Klíč
            /// </summary>
            public string Key { get; set; }
            /// <summary>
            /// Barva
            /// </summary>
            public Color Color { get; set; }
        }
        #endregion
        #region Errors
        /// <summary>
        /// Zaloguje chybu při analýze (jde o nedostatečné schopnosti analyzeru), 
        /// pro další rozšiřování analyzeru.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        protected void _ProcessAddRtfError(string type, string name)
        {
            this._RtfErrors += type + "\t" + name + "\r\n";
        }
        /// <summary>
        /// Suma chyb
        /// </summary>
        protected string _RtfErrors;
        #endregion
        #region Test RTF texts
        internal static string TestRtfText1
        {
            get
            {
                return @"{\rtf1\ansi\ansicpg1250\deff0\deflang1029{\fonttbl{\f0\fswiss\fcharset238{\*\fname Arial;}Arial CE;}{\f1\fmodern\fprq1\fcharset238 Envy Code R;}{\f2\fmodern\fprq1\fcharset0 Envy Code R;}{\f3\fnil\fprq2\fcharset238 Gentium Basic;}{\f4\fnil\fprq2\fcharset0 Gentium Basic;}{\f5\fnil\fcharset0 ;}{\f6\fnil\fcharset2 Symbol;}}
{\colortbl ;\red255\green0\blue0;}
{\*\generator Msftedit 5.41.15.1515;}\viewkind4\uc1\pard\qc\b\f0\fs40 Titulek\par
\pard{\pntext\f6\'B7\tab}{\*\pn\pnlvlblt\pnf6\pnindent0{\pntxtb\'B7}}\fi-720\li720\b0\f1\fs20 Odstavec Envy Code R; 10\lang1033\f2\par
\lang1029\f3{\pntext\f6\'B7\tab}Odstavec Gentium Basic; 10\lang1033\f4\par
\cf1\lang1029\f1{\pntext\f6\'B7\tab}Odstavec \'e8erven\'fd\cf0\lang1033\f2\par
\pard\f5\par
\par
}
 ";
            }
        }
        internal static string TestRtfText2
        {
            get
            {
                return @"{\rtf1\ansi\ansicpg1250\deff0\deflang1029{\fonttbl{\f0\fswiss\fcharset238{\*\fname Arial;}Arial CE;}{\f1\fmodern\fprq1\fcharset238 Envy Code R;}{\f2\fmodern\fprq1\fcharset0 Envy Code R;}{\f3\fnil\fprq2\fcharset238 Gentium Basic;}{\f4\fnil\fprq2\fcharset0 Gentium Basic;}{\f5\fnil\fcharset0 ;}{\f6\fnil\fcharset2 Symbol;}}
{\colortbl ;\red255\green0\blue0;\red0\green128\blue0;\red0\green0\blue255;}
{\*\generator Msftedit 5.41.15.1515;}\viewkind4\uc1\pard\qc\b\f0\fs40 Titulek\par
\pard{\pntext\f6\'B7\tab}{\*\pn\pnlvlblt\pnf6\pnindent0{\pntxtb\'B7}}\fi-720\li720\b0\f1\fs20 Odstavec Envy Code R; 10\lang1033\f2\par
\lang1029\f3{\pntext\f6\'B7\tab}Odstavec Gentium Basic; 10\lang1033\f4\par
\cf1\lang1029\f1{\pntext\f6\'B7\tab}Odstavec \'e8erven\'fd\cf0\lang1033\f2\par
\cf2\lang1029\f1{\pntext\f6\'B7\tab}Odstavec zelen\'fd\cf0\lang1033\f2\par
\cf3\lang1029\f1{\pntext\f6\'B7\tab}Odstavec modr\'fd\cf0\lang1033\f2\par
\pard\f5\par
\lang1029\f1 Tabul\'e1tory:\par
\pard\tx1136\tx3408\tx6816\tab 2cm\tab 6cm\tab 12cm\par
\lang1033\f5\par
\lang1029\f1 Jin\'e9 tabul\'e1tory:\par
\pard\tx568\tx3976\tx5680\tab 1cm\tab 7cm\tab 10cm\par
\pard\tx1136\tx3408\tx6816\lang1033\f5\par
\par
}
 ";
            }
        }
        #endregion
    }
}
