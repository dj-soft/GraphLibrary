using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Asol.Tools.WorkScheduler.Components;
using RES = Noris.LCS.Base.WorkScheduler.Resources;
using System.Drawing;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components.Test
{
    /// <summary>
    /// Třída obsahující testy dat
    /// </summary>
    [TestClass]
    public class TestComponent
    {
        #region TextEditorController
        /// <summary>
        /// Testy k TextBoxu - jednoduché dělení na slova
        /// </summary>
        [TestMethod]
        public void TestTextsSimple()
        {
            string text = "První slovo, třetí slovo (páté slovo v závorce).";
            SizeF charSize = new SizeF(5, 8);
            var chars = TextEditorController.CharacterPositionInfo.CreateChars(text, charSize, 160f);

            List<Tuple<Int32Range, string>> words = new List<Tuple<Int32Range, string>>();
            int index = 0;
            while (true)
            {
                if (!TrySearchWord(chars, Direction.Positive, words, ref index)) break;
                if (words.Count > text.Length)
                    throw new AssertFailedException("Chyba GTextEdit.EditorStateInfo.TrySearchWord(): příliš mnoho slov vytvořených z nemnoha písmen.");
            }

            _CheckWord(words, 0, 0, 5, "První");
            _CheckWord(words, 1, 6, 11, "slovo");
            _CheckWord(words, 2, 13, 18, "třetí");
            _CheckWord(words, 3, 19, 24, "slovo");
            _CheckWord(words, 4, 26, 30, "páté");
            _CheckWord(words, 5, 31, 36, "slovo");
            _CheckWord(words, 6, 37, 38, "v");
            _CheckWord(words, 7, 39, 46, "závorce");

            var char30 = chars[words[2].Item1.Begin];      // Slovo [2] = "třetí", znak na pozici [0] = 't' (na pozici 13 v textu)
            var bounds = char30.Bounds;
            if (bounds.X != 65f) throw new AssertFailedException("Chyba GTextEdit.EditorStateInfo.TrySearchWord(): slovo na pozici 3 nezačíná na pixelu 65.");
        }
        /// <summary>
        /// Testy k TextBoxu - interaktivní dělení na slova
        /// </summary>
        [TestMethod]
        public void TestTextsInteractive()
        {
            string text = "První slovo, třetí slovo (páté slovo v závorce).";
            //             012345678901234567890123456789012345678901234567890
            //             0         1         2         3         4         5

            // Text je dlouhý 48 znaků, 1 znak má šířku 5px, jeden řádek má max 160px, budou dva řádky: "První slovo, třetí slovo (páté s", "lovo v závorce).":
            var chars = TextEditorController.CharacterPositionInfo.CreateChars(text, new SizeF(5, 8), 160f);


            bool found;
            int idx = 28;        // "t" ve slově "páté"
            found = TextEditorController.TrySearchWordEnd(chars, Direction.Positive, ref idx);               // Má být 30
            if (idx != 30) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchWordEnd(a): Nalezený index je chybný: {idx}, má být: 30.");

            found = TextEditorController.TrySearchWordEnd(chars, Direction.Positive, ref idx);               // Má zůstat 30
            if (idx != 30) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchWordEnd(b): Nalezený index je chybný: {idx}, má být: 30.");

            idx++;
            found = TextEditorController.TrySearchWordEnd(chars, Direction.Positive, ref idx);               // Má se najít konec dalšího slova = 36
            if (idx != 36) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchWordEnd(c): Nalezený index je chybný: {idx}, má být: 30.");
            idx = 30;

            found = TextEditorController.TrySearchWordBegin(chars, Direction.Negative, ref idx);             // Má se najít 26
            if (idx != 26) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchWordBegin(d): Nalezený index je chybný: {idx}, má být: 26.");

            found = TextEditorController.TrySearchWordBegin(chars, Direction.Negative, ref idx);             // Má zůstat 26
            if (idx != 26) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchWordBegin(e): Nalezený index je chybný: {idx}, má být: 26.");

            found = TextEditorController.TrySearchWordEnd(chars, Direction.Negative, ref idx);               // Má se najít 24
            if (idx != 24) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchWordEnd(f): Nalezený index je chybný: {idx}, má být: 24.");

            found = TextEditorController.TrySearchWordBegin(chars, Direction.Negative, ref idx);             // Má se najít 19
            if (idx != 19) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchWordBegin(g): Nalezený index je chybný: {idx}, má být: 19.");


            Int32Range word;
            found = TextEditorController.TrySearchNearWord(chars, 14, out word);                             // Má se najít slovo "třetí" = 13-18
            _CheckWord(found, word, "A", 13, 18);

            found = TextEditorController.TrySearchNearWord(chars, 25, out word);                             // Má se najít závorka před "(páté", najde se "páté" = 26-30
            _CheckWord(found, word, "B", 26, 30);

            found = TextEditorController.TrySearchNearWord(chars, 30, out word, false);                      // Má se najít mezera mezi "páté slovo", false = najde "páté" = 26-30
            _CheckWord(found, word, "C", 26, 30);

            found = TextEditorController.TrySearchNearWord(chars, 30, out word, true);                       // Má se najít mezera mezi "páté slovo", true = najde "slovo" = 31-36
            _CheckWord(found, word, "D", 31, 36);

        }
        private bool TrySearchWord(TextEditorController.CharacterPositionInfo[] chars, Direction direction, List<Tuple<Int32Range, string>> words, ref int index)
        {
            bool foundBegin = TextEditorController.TrySearchWordBegin(chars, direction, ref index);
            int indexBegin = index;
            bool foundEnd = TextEditorController.TrySearchWordEnd(chars, direction, ref index);
            int indexEnd = index;

            if (!(foundBegin && foundEnd)) return false;

            string word = chars.WhereIndex((i, c) => (i >= indexBegin && i < indexEnd))
                               .ToOneString("", c => c.Content.ToString());
            var item = new Tuple<Int32Range, string>(new Int32Range(indexBegin, indexEnd), word);
            words.Add(item);
            return true;
        }
        private void _CheckWord(List<Tuple<Int32Range, string>> words, int index, int begin, int end, string text)
        {
            if (words == null || index >= words.Count) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchWord(): nenalezeno slovo na indexu {index}.");
            var word = words[index];
            if (word.Item1.Begin != begin) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchWord(): na indexu: {index} je slovo: {word.Item2} s chybným Begin: {word.Item1.Begin}, očekávná hodnota: {begin}.");
            if (word.Item1.End != end) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchWord(): na indexu: {index} je slovo: {word.Item2} s chybným End: {word.Item1.End}, očekávná hodnota: {end}.");
            if (word.Item2 != text) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchWord(): na indexu: {index} je chybné slovo: '{word.Item2}', očekávná hodnota: '{text}'.");
        }
        private void _CheckWord(bool found, Int32Range word, string info, int begin, int end)
        {
            if (!found) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchNearWord({info}): Nenalezeno slovo.");
            if (word == null) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchNearWord({info}): Nalezeno NULL.");
            if (word.Begin != begin) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchNearWord({info}): Nalezen chybný Begin: {word.Begin}, má být: {begin}.");
            if (word.End != end) throw new AssertFailedException($"Chyba GTextEdit.EditorStateInfo.TrySearchNearWord({info}): Nalezen chybný End: {word.End}, má být: {end}.");
        }
        #endregion
    }
}
