using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.Localizable
{
    /// <summary>
    /// Jeden lokalizovatelný text.
    /// Obsahuje <see cref="Text"/> (uživatelský) a kód, podle kterého text najít v překladovém slovníku.
    /// Překladový slovník implementuje třída <see cref="Application.App"/> v metodě <see cref="Application.App.LocalizeCode(string, string)"/>.
    /// </summary>
    public class TextLoc
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TextLoc() { }
        /// <summary>
        /// Konstruktor pro daný text
        /// </summary>
        /// <param name="text"></param>
        public TextLoc(string text) { this._Text = text; this._Code = text; }
        /// <summary>
        /// Konstruktor pro konkrétní kód a jemu odpovídající text
        /// </summary>
        /// <param name="code"></param>
        /// <param name="text"></param>
        public TextLoc(string code, string text) { this._Code = code; this._Text = text; }
        private string _Code;
        private string _Text;
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text;
        }
        /// <summary>
        /// Text: buď zadaný v konstruktoru (pokud nebyl zadán kód), anebo lokalizovaný pomocí metody <see cref="Application.App.LocalizeCode(string, string)"/> pro aktuální jazyk.
        /// </summary>
        public string Text { get { return (!String.IsNullOrEmpty(this._Code) ? Application.App.LocalizeCode(this._Code, this._Text) : this._Text); } }
        /// <summary>
        /// Implicitní konverze z <see cref="String"/> na <see cref="TextLoc"/>
        /// </summary>
        /// <param name="text"></param>
        public static implicit operator TextLoc(String text) { return new TextLoc(text); }
        /// <summary>
        /// Implicitní konverze z <see cref="TextLoc"/> na <see cref="String"/>
        /// </summary>
        /// <param name="text"></param>
        public static implicit operator String(TextLoc text) { return (text == null ? null : text.Text); }
    }
}
