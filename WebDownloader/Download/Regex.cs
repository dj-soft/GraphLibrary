using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Djs.Tools.WebDownloader.Download
{
    #region class RegexSupport
    /// <summary>
    /// Třída pro podporu konverze Wildcard patternu na <see cref="Regex"/>
    /// </summary>
    public static class RegexSupport
    {
        /// <summary>
        /// Metoda vrátí true, pokud hodnota value vyhovuje vzorci pattern.
        /// Pro opakované používání stejného patternu je vhodnější získat <see cref="Regex"/> metodou <see cref="CreateWildcardsRegex(string)"/>,
        /// a ten pak používat pro testování různých hodnot opakovaně.
        /// </summary>
        /// <param name="value">Hodnota, například "Abcdef ghij"</param>
        /// <param name="pattern">Vzorec, například "Abc??f *"</param>
        /// <returns></returns>
        public static bool IsMatchWildcards(string value, string pattern)
        {
            return IsMatchWildcards(value, pattern, true);
        }
        /// <summary>
        /// Metoda vrátí true, pokud hodnota value vyhovuje vzorci pattern.
        /// Pro opakované používání stejného patternu je vhodnější získat <see cref="Regex"/> metodou <see cref="CreateWildcardsRegex(string, bool)"/>,
        /// a ten pak používat pro testování různých hodnot opakovaně.
        /// </summary>
        /// <param name="value">Hodnota, například "Abcdef ghij"</param>
        /// <param name="pattern">Vzorec, například "Abc??f *"</param>
        /// <param name="ignoreCase">true = ignoruje velikost znaků, false = neignoruje</param>
        /// <returns></returns>
        public static bool IsMatchWildcards(string value, string pattern, bool ignoreCase)
        {
            if (!IsWildcardsValid(pattern)) return false;
            Regex regex = CreateWildcardsRegex(pattern, ignoreCase);
            return regex.IsMatch(value);
        }
        /// <summary>
        /// Metoda vrátí true, pokud daný pattern je formálně správný a může být použit v metodě <see cref="CreateWildcardsRegex(string)"/>.
        /// </summary>
        /// <param name="pattern">Pattern s užitím standardních Wildcards * a ?</param>
        /// <returns></returns>
        public static bool IsWildcardsValid(string pattern)
        {
            if (pattern == null) return false;
            pattern = pattern.Trim();
            if (pattern.Length == 0) return false;
            if (IllegalCharactersRegex.IsMatch(pattern)) return false;
            return true;
        }
        /// <summary>
        /// Metoda vrátí pole <see cref="Regex"/>, které dovolují porovnávat konkrétní texty se standardní Wildcards notací.
        /// Z dodané sady wildcard masek (odděleny středníkem) vrátí pole Regex výrazů pro jejich filtrování.
        /// Pokud je na vstupu Empty, vrací prázdné pole.
        /// Typický vstup: "*.tmp; *.js; *thumb*.*; *.htm*;" atd
        /// Tedy: text "Abcdefg" vyhovuje patternu "Ab??e*".
        /// Volitelně lze požádat, aby <see cref="Regex"/> měl zapnutou option <see cref="RegexOptions.IgnoreCase"/>: true = ignoruje velikost znaků, false = neignoruje
        /// </summary>
        /// <param name="pattern">Pattern s užitím standardních Wildcards * a ?</param>
        /// <returns></returns>
        public static Regex[] CreateWildcardsRegexes(string patterns)
        {
            return CreateWildcardsRegexes(patterns, true);
        }
        /// <summary>
        /// Metoda vrátí pole <see cref="Regex"/>, které dovolují porovnávat konkrétní texty se standardní Wildcards notací.
        /// Z dodané sady wildcard masek (odděleny středníkem) vrátí pole Regex výrazů pro jejich filtrování.
        /// Pokud je na vstupu Empty, vrací prázdné pole.
        /// Typický vstup: "*.tmp; *.js; *thumb*.*; *.htm*;" atd
        /// Tedy: text "Abcdefg" vyhovuje patternu "Ab??e*".
        /// Volitelně lze požádat, aby <see cref="Regex"/> měl zapnutou option <see cref="RegexOptions.IgnoreCase"/>: true = ignoruje velikost znaků, false = neignoruje
        /// </summary>
        /// <param name="pattern">Patterny s užitím standardních Wildcards * a ?</param>
        /// <returns></returns>
        public static Regex[] CreateWildcardsRegexes(string patterns, bool ignoreCase)
        {
            List<Regex> regexes = new List<Regex>();
            if (!String.IsNullOrEmpty(patterns))
            {
                string[] masks = patterns.Trim().Split(';');
                foreach (string mask in masks)
                {
                    if (!String.IsNullOrEmpty(mask))
                    {
                        Regex regex = CreateWildcardsRegex(mask.Trim(), true);
                        if (regex != null)
                            regexes.Add(regex);
                    }
                }
            }

            return regexes.ToArray();
        }
        /// <summary>
        /// Vrátí dodanou kolekci textů filtrovanou podle dané kolekce regulárních výrazů.
        /// Kolekce <paramref name="data"/> typicky obsahuje seznam souborů nebo názvů;
        /// kolekce <paramref name="regexes"/> obsahuje výstup zdejší metody <see cref="CreateWildcardsRegexes(string)"/>;
        /// výstup zdejší metody pak obsahuje jen vyhovující soubory.
        /// <para/>
        /// Pokud <paramref name="data"/> je null, výstupem je null.
        /// Pokud <paramref name="regexes"/> je null nebo prázdná kolekce, pak výstupem je vstupní kolekce <paramref name="data"/>.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="regexes"></param>
        /// <returns></returns>
        public static IEnumerable<string> FilterByRegexes(IEnumerable<string> data, IEnumerable<Regex> regexes)
        {
            if (data == null) return null;
            if (regexes == null || regexes.Count() == 0) return data;
            return data.Where(t => IsTextMatchToAny(t, regexes));
        }
        /// <summary>
        /// Vrátí true, pokud daný text vyhovuje některé masce
        /// </summary>
        /// <param name="name"></param>
        /// <param name="regexes"></param>
        /// <returns></returns>
        public static bool IsTextMatchToAny(string text, IEnumerable<Regex> regexes)
        {
            if (text == null) return false;
            if (regexes == null) return false;
            return regexes.Any(mask => mask.IsMatch(text));
        }
        /// <summary>
        /// Metoda vrátí <see cref="Regex"/>, který dovoluje porovnávat texty se standardní Wildcards notací.
        /// Tedy: text "Abcdefg" vyhovuje patternu "Ab??e*".
        /// Volitelně lze požádat, aby <see cref="Regex"/> měl zapnutou option <see cref="RegexOptions.IgnoreCase"/>: true = ignoruje velikost znaků, false = neignoruje
        /// </summary>
        /// <param name="pattern">Pattern s užitím standardních Wildcards * a ?</param>
        /// <returns></returns>
        public static Regex CreateWildcardsRegex(string pattern)
        {
            return CreateWildcardsRegex(pattern, true);
        }
        /// <summary>
        /// Metoda vrátí <see cref="Regex"/>, který dovoluje porovnávat texty se standardní Wildcards notací.
        /// Tedy: text "Abcdefg" vyhovuje patternu "Ab??e*".
        /// Volitelně lze požádat, aby <see cref="Regex"/> měl zapnutou option <see cref="RegexOptions.IgnoreCase"/>: true = ignoruje velikost znaků, false = neignoruje
        /// </summary>
        /// <param name="pattern">Pattern s užitím standardních Wildcards * a ?</param>
        /// <param name="ignoreCase">true = ignoruje velikost znaků, false = neignoruje</param>
        /// <returns></returns>
        public static Regex CreateWildcardsRegex(string pattern, bool ignoreCase)
        {
            if (pattern == null) throw new ArgumentNullException();

            pattern = pattern.Trim();
            if (pattern.Length == 0) throw new ArgumentException("Pattern is empty.");

            if (IllegalCharactersRegex.IsMatch(pattern)) throw new ArgumentException("Pattern contains illegal characters.");

            bool hasExtension = CatchExtentionRegex.IsMatch(pattern);
            bool matchExact = false;
            if (HasQuestionMarkRegEx.IsMatch(pattern))
                matchExact = true;
            else if (hasExtension)
                matchExact = CatchExtentionRegex.Match(pattern).Groups[1].Length != 3;

            string regexString = Regex.Escape(pattern);
            regexString = "^" + Regex.Replace(regexString, @"\\\*", ".*");
            regexString = Regex.Replace(regexString, @"\\\?", ".");
            if (!matchExact && hasExtension)
            {
                regexString += NonDotCharacters;
            }
            regexString += "$";
            RegexOptions regexOptions = (ignoreCase ? RegexOptions.Compiled | RegexOptions.IgnoreCase : RegexOptions.Compiled);
            Regex regex = new Regex(regexString, regexOptions);
            return regex;
        }
        private static Regex HasQuestionMarkRegEx = new Regex(@"\?", RegexOptions.Compiled);
        private static Regex IllegalCharactersRegex = new Regex("[" + @"\/:<>|" + "\"]", RegexOptions.Compiled);
        private static Regex CatchExtentionRegex = new Regex(@"^\s*.+\.([^\.]+)\s*$", RegexOptions.Compiled);
        private static string NonDotCharacters = @"[^.]*";
    }
    #endregion
}
