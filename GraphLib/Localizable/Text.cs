using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Djs.Common.Localizable
{
    /// <summary>
    /// One localizable text.
    /// </summary>
    public class TextLoc
    {
        public TextLoc() { }
        public TextLoc(string text) { this._Text = text; this._Code = text; }
        public TextLoc(string code, string text) { this._Code = code; this._Text = text; }
        private string _Code;
        private string _Text;
        public override string ToString()
        {
            return this.Text;
        }
        public string Text { get { return (!String.IsNullOrEmpty(this._Code) ? Application.App.LocalizeCode(this._Code, this._Text) : this._Text); } }

        public static implicit operator TextLoc(string text) { return new TextLoc(text); }
        public static implicit operator String(TextLoc text) { return (text == null ? null : text.Text); }
    }
}
