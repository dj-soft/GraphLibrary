using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Djs.Common.TextParser;
using System.Drawing;

namespace Djs.Common.Rtf
{
    public class RtfDocument
    {
        public RtfDocument()
        { }
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
      
        private void InvalidateDocument()
        {
            this._RtfText = null;
            this._Content = new List<RtfContent>();
            this._Fonts = new Dictionary<string, RtfFont>();
            this._Colors = new Dictionary<string, RtfColor>();
            this._RtfErrors = "";
        }
        private void _LoadRtfText()
        {
            if (String.IsNullOrEmpty(this._RtfText)) return;

            using (Parser parser = new Parser(TextParser.ParserDefaultSetting.Rtf))
            {
                List<ParserSegment> segmentList = parser.ParseString(this._RtfText);
                ParserSegment rtfSegment = _GetMainRtfSegment(segmentList);
                if (rtfSegment != null)
                    this._ProcessRtfDocument(rtfSegment);
            }
        }
        private void _ProcessRtfDocument(TextParser.ParserSegment parserSegment)
        {
            foreach (var item in parserSegment.Values)
            {
                switch (item.ValueType)
                {
                    case TextParser.ParserSegmentValueType.None:
                    case TextParser.ParserSegmentValueType.Blank:
                        break;
                    case TextParser.ParserSegmentValueType.Text:
                        if (item.HasContent)
                        {
                            this._ProcessRtfContent(item);
                        }
                        break;
                    case TextParser.ParserSegmentValueType.Delimiter:
                        if (item.HasContent)
                        { }
                        break;
                    case TextParser.ParserSegmentValueType.InnerSegment:
                        if (item.HasInnerSegment)
                        {
                            switch (item.InnerSegment.SegmentName)
                            {
                                case ParserDefaultSetting.RTF_ENTITY:
                                    this._ProcessRtfEntity(item.InnerSegment);
                                    break;
                                case ParserDefaultSetting.RTF_BLOCK:
                                    this._ProcessRtfBlock(item.InnerSegment);
                                    break;
                                case ParserDefaultSetting.RTF_CHAR2:
                                    this._ProcessRtfChar2(item.InnerSegment);
                                    break;
                                case ParserDefaultSetting.RTF_CHARUNICODE:
                                    this._ProcessRtfCharUnicode(item.InnerSegment);
                                    break;
                                default:
                                    this._ProcessAddRtfError("Segment", item.InnerSegment.SegmentName);
                                    break;
                            }
                        }
                        break;
                    default:
                        this._ProcessAddRtfError("ValueType", item.ValueType.ToString());
                        break;
                }
            }
        }

        private void _ProcessRtfEntity(ParserSegment rtfSegment)
        {
            foreach (ParserSegmentValue parsedValue in rtfSegment.Values)
            {
                string rtfValue;
                string rtfEntity = GetRtfEntity(parsedValue, out rtfValue);
                switch (rtfEntity)
                {
                    case "viewkind":


                        break;
                }
            }
            
        }
        #region Parse RTF inner blocks
        private void _ProcessRtfBlock(ParserSegment rtfSegment)
        {
            if (rtfSegment.ValueCount == 0) return;
            string rtfEntity = GetRtfEntityFirst(rtfSegment);
            switch (rtfEntity)
            {
                case "fonttbl":
                    this._ProcessRtfBlockFontTable(rtfSegment);
                    break;
                case "colortbl":
                    this._ProcessRtfBlockFColor(rtfSegment);
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
        /// <param name="segmentList"></param>
        /// <returns></returns>
        private ParserSegment _GetMainRtfSegment(List<ParserSegment> segmentList)
        {
            if (segmentList.Count > 0 && segmentList[0].SegmentName == TextParser.ParserDefaultSetting.RTF_NONE && segmentList[0].ValueCount > 0)
            {
                ParserSegmentValue rtfFirstValue = segmentList[0].ValueList[0];
                if (rtfFirstValue.HasInnerSegment && rtfFirstValue.InnerSegment.SegmentName == TextParser.ParserDefaultSetting.RTF_DOCUMENT)
                    return rtfFirstValue.InnerSegment;
            }
            return null;
        }
        /// <summary>
        /// Split value (for example: "charset238") to name ("charset") and numeric value ("238").
        /// </summary>
        /// <param name="parserValue"></param>
        /// <returns></returns>
        protected static string GetRtfEntity(ParserSegmentValue parserValue)
        {
            string rtfValue;
            return GetRtfEntity(parserValue, out rtfValue);
        }
        protected static string GetRtfEntity(ParserSegmentValue parserValue, out string rtfValue)
        {
            rtfValue = "";
            if (parserValue == null) return "";
            string rtfEntity = parserValue.InnerText;
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
        protected static char[] ValuesArrayNum { get { if (_ValuesArrayNum == null) _ValuesArrayNum = "0123456789".ToArray(); return _ValuesArrayNum; } } private static char[] _ValuesArrayNum;
        /// <summary>
        /// Array of char: {Space} 0 1 2 3 4 5 6 7 8 9
        /// </summary>
        protected static char[] ValuesArrayNumSpace { get { if (_ValuesArrayNumSpace == null) _ValuesArrayNumSpace = " 0123456789".ToArray(); return _ValuesArrayNumSpace; } } private static char[] _ValuesArrayNumSpace;
        protected static string GetRtfEntityFirst(ParserSegment parserSegment)
        {
            ParserSegmentValue foundValue = GetFirstParserValue(parserSegment);
            return GetRtfEntity(foundValue);
        }
        protected static string GetRtfEntityFirst(ParserSegment parserSegment, out string rtfValue)
        {
            ParserSegmentValue foundValue = GetFirstParserValue(parserSegment);
            return GetRtfEntity(foundValue, out rtfValue);
        }
        protected static ParserSegmentValue GetFirstParserValue(ParserSegment parserSegment)
        {
            // Search for First-First-First-... value:
            ParserSegment scanSegment = parserSegment;
            ParserSegmentValue foundValue = null;
            while (true)
            {
                ParserSegmentValue parserValue = (scanSegment != null && scanSegment.ValueCount > 0 ? scanSegment.ValueList[0] : null);
                if (parserValue == null)
                    // Current scanSegment has no values => exit from loop, we found "foundValue" value.
                    break;
                if (parserValue.HasInnerSegment)
                {   // First value has inner segment => scan "recursively" this segment:
                    foundValue = parserValue;        // current value (parserValue) can be last found value...
                    scanSegment = parserValue.InnerSegment;
                    continue;
                }
                if (parserValue.HasContent)
                    foundValue = parserValue;        // current value (parserValue) is last found value
                break;
            }
            return foundValue;
        }
        protected struct NameValue
        {

            private string _Name;
            private string _Value;
            private int _ValueInt;
            public string Name { get { return this._Name; } }
            public string Value { get { return this._Value; } }
            public int ValueInt { get { return this._ValueInt; } }
        }
        #endregion
        #region Setting

        protected class RtfSetting
        {

        }
        #endregion
        #region Content

        protected void _ProcessRtfContent(ParserSegmentValue item)
        {
            
        }

        private void _ProcessRtfChar2(ParserSegment parserSegment)
        {
           
        }

        private void _ProcessRtfCharUnicode(ParserSegment parserSegment)
        {
            
        }

        protected List<RtfContent> _Content;
        protected class RtfContent
        { }
        #endregion
        #region Fonts
        /// <summary>
        /// Process FontTable (=all fonts) from segment (parameter)
        /// </summary>
        /// <param name="rtfSegment"></param>
        protected void _ProcessRtfBlockFontTable(ParserSegment rtfSegment)
        {   // for example:  \fonttbl{\f0\fswiss\fcharset238{\*\fname Arial;}Arial CE;}{\f1\fmodern\fprq1\fcharset238 Envy Code R;}...{\f6\fnil\fcharset2 Symbol;}
            //               \fonttbl{\f0\fswiss\fcharset238{\*\fname Arial;}Arial CE;}{\f1\fmodern\fprq1\fcharset238 Envy Code R;}{\f2\fmodern\fprq1\fcharset0 Envy Code R;}{\f3\fnil\fprq2\fcharset238 Gentium Basic;}{\f4\fnil\fprq2\fcharset0 Gentium Basic;}{\f5\fnil\fcharset0 ;}{\f6\fnil\fcharset2 Symbol;}
            for (int i = 1 /* Skip index [0], contains value with keyword "fonttbl" ! */; i < rtfSegment.ValueCount; i++)
            {
                ParserSegmentValue parsedValue = rtfSegment.ValueList[i];
                if (parsedValue.HasInnerSegment)
                    this._ProcessRtfBlockFontOne(parsedValue.InnerSegment);
            }
        }
        /// <summary>
        /// Process one font
        /// </summary>
        /// <param name="rtfSegment"></param>
        protected void _ProcessRtfBlockFontOne(ParserSegment rtfSegment)
        {   // for example: \f0\fswiss\fcharset238{\*\fname Arial;}Arial CE;
            //              \f2\fmodern\fprq1\fcharset0 Envy Code R;
            RtfFont rtfFont = new RtfFont();
            for (int index = 0; index < rtfSegment.ValueCount; index++)
            {
                ParserSegmentValue parsedValue = rtfSegment.ValueList[index];
                if (parsedValue.HasInnerSegment)
                {
                    ParserSegment segment = parsedValue.InnerSegment;
                    if (index == 0)
                    {   // Font number in table:
                        rtfFont.Key = segment.InnerText;                                 // f2
                    }
                    else
                    {
                        string rtfValue;
                        string rtfEntity = GetRtfEntityFirst(segment, out rtfValue);     // "fswiss", "fcharset"+"238", 
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
                                this._ProcessRtfBlockFontName(rtfSegment, ref index, rtfFont);
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
        /// <param name="rtfSegment"></param>
        /// <param name="index"></param>
        /// <param name="rtfFont"></param>
        private void _ProcessRtfBlockFontName(ParserSegment rtfSegment, ref int index, RtfFont rtfFont)
        {
            index++;
            while (index < rtfSegment.ValueCount)
            {
                ParserSegmentValue parsedValue = rtfSegment.ValueList[index];
                index++;
                if (parsedValue.ValueType == ParserSegmentValueType.Text)
                {
                    rtfFont.Name = parsedValue.Text.TrimEnd(';', ' ');
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
        protected void _ProcessRtfBlockFColor(ParserSegment rtfSegment)
        {   // For example:   {\colortbl ;\red255\green0\blue0;\red0\green128\blue0;\red0\green0\blue255;}
            int colorIndex = 0;
            int index = 2;
            int count = rtfSegment.ValueCount;
            while (index < count)
            {
                if ((index + 2) >= count) break;
                this._ProcessRtfBlockFColorOne(rtfSegment, ref index, ref colorIndex);
            }
        }

        private void _ProcessRtfBlockFColorOne(ParserSegment rtfSegment, ref int index, ref int colorIndex)
        {
            int r, g, b;

            if (!this._ProcessRtfBlockFColorValue(rtfSegment, ref index, "red", out r)) return;
            if (!this._ProcessRtfBlockFColorValue(rtfSegment, ref index, "green", out g)) return;
            if (!this._ProcessRtfBlockFColorValue(rtfSegment, ref index, "blue", out b)) return;

            
            RtfColor rtfColor = new RtfColor();
            rtfColor.Key = (++colorIndex).ToString();
            rtfColor.Color = Color.FromArgb(255, r, g, b);
            this._Colors.Add(rtfColor.Key, rtfColor);
        }

        private bool _ProcessRtfBlockFColorValue(ParserSegment rtfSegment, ref int index, string name, out int value)
        {
            value = 0;
            string rtfValue, rtfEntity;
            rtfEntity = GetRtfEntity(rtfSegment.ValueList[index++], out rtfValue);     // "", "red"+"255", 
            if (rtfEntity != name) return false;
            if (!Int32.TryParse(rtfValue, out value)) return false;
            return (value >= 0 && value <= 255);
        }
        private Dictionary<string, RtfColor> _Colors;
        protected class RtfColor
        {
            public string Key { get; set; }
            public Color Color { get; set; }
        }
        #endregion
        #region Errors
        protected void _ProcessAddRtfError(string type, string name)
        {
            this._RtfErrors += type + "\t" + name + "\r\n";
        }
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
