using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Data
{
    #region class TextComparable : třída zahrnující text a s ním spřaženou hodnotu, podle níž lze tyto prvky srovnávat
    /// <summary>
    /// <see cref="TextComparable"/> : třída zahrnující text a s ním spřaženou hodnotu, podle níž lze tyto prvky srovnávat.
    /// Typickou implementací je text "Jméno Příjmení", a spřažená hodnota "Příjmení Jméno".
    /// Důsledkem je čitelný text, a současně možnost setřídění podle příjmení...
    /// </summary>
    public class TextComparable : IComparable
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TextComparable() { }
        /// <summary>
        /// Konstruktor se zadáním textu
        /// </summary>
        /// <param name="text"></param>
        public TextComparable(string text) { this.Text = text; this.Value = text; }
        /// <summary>
        /// Konstruktor se zadáním textu i srovnávací hodnoty
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value"></param>
        public TextComparable(string text, IComparable value) { this.Text = text; this.Value = value; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return (this.Text == null ? "" : this.Text);
        }
        /// <summary>
        /// Vizuální hodnota
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Srovnávací hodnota
        /// </summary>
        public IComparable Value { get; set; }
        /// <summary>
        /// Porovnání podle <see cref="Value"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        int IComparable.CompareTo(object obj)
        {
            TextComparable other = obj as TextComparable;
            if (other == null) return 1;
            return this.Value.CompareTo(other.Value);
        }
        /// <summary>
        /// Implicitní konverze z <see cref="String"/> na <see cref="TextComparable"/>
        /// </summary>
        /// <param name="text"></param>
        public static implicit operator TextComparable(String text) { return new TextComparable(text); }
        /// <summary>
        /// Implicitní konverze z <see cref="TextComparable"/> na <see cref="String"/>
        /// </summary>
        /// <param name="text"></param>
        public static implicit operator String(TextComparable text) { return (text == null ? null : text.Text); }
    }
    #endregion
}
