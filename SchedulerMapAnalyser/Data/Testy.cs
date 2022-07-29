using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.SchedulerMap.Analyser.Data
{
    public class Testy
    {
        public static void Run()
        {
            TestSortedList();
            TestSortedDict();
            TestSortedSet();

        }
        #region SortedList | SortedDict | SortedSet
        /*   Srovnání tříd :
        SortedList   - se chová dost jako klasická Dictionary
                     - duplicitní vložení se nepovoluje = dojde k chybě
                     - prvky jsou vždy nativně setříděny podle hodnoty klíče
                        - vložením new instance s hodnotou klíče mezi dosavadními hodnotami se nový prvek zařadí mezi ně
                     
                   ==> navíc umí k datům přistupovat jako List : najít index prvku tak jako to umí List a najít prvek podle tohoto indexu pole, nejen podle klíče Key

        SortedDict   - se chová dost jako klasická Dictionary
                     - duplicitní vložení se nepovoluje = dojde k chybě
                     - prvky jsou vždy nativně setříděny podle hodnoty klíče
                        - vložením new instance s hodnotou klíče mezi dosavadními hodnotami se nový prvek zařadí mezi ně

                     - neumí ale pracovat s prvky jako s Listem = procházet podle indexu

        SortedSet    - chová se trochu jako List, prvky lze přidávat a odebírat
                     - nemá explicitně daný Key
                     - automaticky třídy prvky podle jejich komparátoru (do konstruktoru se musí vložit instance objektu, která implementuje komparátor)
                     - prvky jsou vždy nativně setříděny podle hodnoty klíče
                     - pokus o opakované vložení nové instance objektu, jehož klíčovou hodnotu už v poli máme:
                        - nevyvolá chybu, ale new instance se nevloží, z metody Add() se vrací false
                     - změna dat v uložené instanci, která způsobí změnu klíčové hodnoty:
                        - nepřetřídí data, a není ani metoda pro vynucený Sort()
                     - dokáže najít data podle klíče, ale musí se zadat new instance stejného typu s požadovanou hodnotou klíče 
                        - protože klíč není dán explicitně, ale je vyhodnocován až v komparátoru
                     - dokáže vyhledat subset prvků daného rozsahu, ale zase musí dostat dvě new instance pro deklarování toho rozsahu Od-Do

        */
        private static void TestSortedList()
        {
            SortedList<int, Dato> sortedList = new SortedList<int, Dato>();

            sortedList.Add(10, (new Dato(10, "A      1")));
            sortedList.Add(20, (new Dato(20, "BB     1")));
            sortedList.Add(30, (new Dato(30, "CCC    1")));
            sortedList.Add(40, (new Dato(40, "DDDD   1")));
            sortedList.Add(50, (new Dato(50, "EEEEE  1")));
            string tx0 = GetText(sortedList);
            /* výsledek:
[10, 10 = A      1]
[20, 20 = BB     1]
[30, 30 = CCC    1]
[40, 40 = DDDD   1]
[50, 50 = EEEEE  1]
            */

            // Tohle způsobí chybu:
            //   sortedList.Add(40, (new Dato(40, "DDDDDD 2")));
            // Tím se liší od SortedSet, který by druhý prvek stejného klíče nepřidal.


            sortedList.Clear();
            sortedList.Add(40, (new Dato(40, "DDDD   1")));
            sortedList.Add(20, (new Dato(20, "BB     1")));
            sortedList.Add(10, (new Dato(10, "A      1")));
            sortedList.Add(50, (new Dato(50, "EEEEE  1")));
            sortedList.Add(30, (new Dato(30, "CCC    1")));
            string tx1 = GetText(sortedList);
            /* výsledek:
[10, 10 = A      1]
[20, 20 = BB     1]
[30, 30 = CCC    1]
[40, 40 = DDDD   1]
[50, 50 = EEEEE  1]
            */
            string tx2 = GetText(sortedList.Values);
            /* výsledek:
10 = A      1
20 = BB     1
30 = CCC    1
40 = DDDD   1
50 = EEEEE  1
            */

            // Umí vyhledat index podle klíče:
            int index40 = sortedList.IndexOfKey(40);             // Vrátí  3
            int index45 = sortedList.IndexOfKey(45);             // Vrátí -1 = neexistuje, ale ne chybu
            var dato40 = sortedList.Values[index40];             // Vrátí instanci '40 = DDDD   1'

            int indexEE = sortedList.IndexOfValue(new Dato(50, "EEEEE  1"));      // Vrátí -1 : Takový prvek tam je, ale jiná instance. Nehledá podle klíče.
            int indexDD = sortedList.IndexOfValue(dato40);                        // Vrátí  3 : Tento prvek tam je na indexu 3

            // Odeberu prvek na indexu:
            sortedList.RemoveAt(indexDD);
            string tx3 = GetText(sortedList);
            /* výsledek:
[10, 10 = A      1]
[20, 20 = BB     1]
[30, 30 = CCC    1]
[50, 50 = EEEEE  1]
            */


            sortedList.Add(40, dato40);
            string tx4 = GetText(sortedList);
            /* výsledek:
[10, 10 = A      1]
[20, 20 = BB     1]
[30, 30 = CCC    1]
[40, 40 = DDDD   1]
[50, 50 = EEEEE  1]
            */


            bool has20 = sortedList.TryGetValue(20, out var dato20);          // true, a získám prvek '20 = BB     1'
            /* výsledek:

            */


            // sortedList.


            /* výsledek:

            */

            /* výsledek:

            */

        }
        private static void TestSortedDict()
        {
            SortedDictionary<int, Dato> sortedDict = new SortedDictionary<int, Dato>();

            sortedDict.Add(10, (new Dato(10, "A      1")));
            sortedDict.Add(20, (new Dato(20, "BB     1")));
            sortedDict.Add(30, (new Dato(30, "CCC    1")));
            sortedDict.Add(40, (new Dato(40, "DDDD   1")));
            sortedDict.Add(50, (new Dato(50, "EEEEE  1")));
            string tx0 = GetText(sortedDict);
            /* výsledek:
[10, 10 = A      1]
[20, 20 = BB     1]
[30, 30 = CCC    1]
[40, 40 = DDDD   1]
[50, 50 = EEEEE  1]
            */

            // Tohle způsobí chybu:
            //   sortedList.Add(40, (new Dato(40, "DDDDDD 2")));
            // Tím se liší od SortedSet, který by druhý prvek stejného klíče nepřidal.


            sortedDict.Clear();
            sortedDict.Add(40, (new Dato(40, "DDDD   1")));
            sortedDict.Add(20, (new Dato(20, "BB     1")));
            sortedDict.Add(10, (new Dato(10, "A      1")));
            sortedDict.Add(50, (new Dato(50, "EEEEE  1")));
            sortedDict.Add(30, (new Dato(30, "CCC    1")));
            string tx1 = GetText(sortedDict);
            /* výsledek:
[10, 10 = A      1]
[20, 20 = BB     1]
[30, 30 = CCC    1]
[40, 40 = DDDD   1]
[50, 50 = EEEEE  1]
            */


            string tx2 = GetText(sortedDict.Values);
            /* výsledek:
10 = A      1
20 = BB     1
30 = CCC    1
40 = DDDD   1
50 = EEEEE  1
            */

            // Neumí vyhledat index podle klíče:
            //int index40 = sortedDict.IndexOfKey(40);             // Vrátí  3
            //int index45 = sortedDict.IndexOfKey(45);             // Vrátí -1 = neexistuje, ale ne chybu
            //var dato40 = sortedDict.Values[index40];             // Vrátí instanci '40 = DDDD   1'

            //int indexEE = sortedDict.IndexOfValue(new Dato(50, "EEEEE  1"));      // Vrátí -1 : Takový prvek tam je, ale jiná instance. Nehledá podle klíče.
            //int indexDD = sortedDict.IndexOfValue(dato40);                        // Vrátí  3 : Tento prvek tam je na indexu 3

           


            bool has20 = sortedDict.TryGetValue(20, out var dato20);          // true, a získám prvek '20 = BB     1'
            /* výsledek:

            */


            // sortedList.


            /* výsledek:

            */

            /* výsledek:

            */

        }
        private static void TestSortedSet()
        {
            SortedSet<Dato> sortedSet = new SortedSet<Dato>(new Dato());
            string tx0 = GetText(sortedSet);

            // Normálka:
            sortedSet.Add(new Dato(10, "AAA"));
            sortedSet.Add(new Dato(20, "BBBB"));
            sortedSet.Add(new Dato(30, "CCCCC"));
            sortedSet.Add(new Dato(40, "DDDDDD"));
            string tx1 = GetText(sortedSet);
            /* výsledek: 
10 = AAA
20 = BBBB
30 = CCCCC
40 = DDDDDD
            */

            sortedSet.Clear();

            // Sám třídí:
            sortedSet.Add(new Dato(40, "DDDDDD"));
            sortedSet.Add(new Dato(20, "BBBB"));
            sortedSet.Add(new Dato(30, "CCCCC"));
            sortedSet.Add(new Dato(10, "AAA"));
            string tx3 = GetText(sortedSet);
            /* výsledek: 
10 = AAA
20 = BBBB
30 = CCCCC
40 = DDDDDD
            */

            sortedSet.Clear();

            // Automaticky přidává jen první výskyt dle hodnoty, při opakovaném přidání stejné hodnoty novou instanci nepřidá:
            bool addD1 = sortedSet.Add(new Dato(40, "DDDDDD 1"));        // Vrátí true...
            bool addA1 = sortedSet.Add(new Dato(10, "AAA    1"));
            bool addB1 = sortedSet.Add(new Dato(20, "BBBB   1"));
            bool addC1 = sortedSet.Add(new Dato(30, "CCCCC"));
            bool addA2 = sortedSet.Add(new Dato(10, "AAA    2"));        // Vrátí false
            bool addB2 = sortedSet.Add(new Dato(20, "BBBB   2"));        // Vrátí false
            string tx4 = GetText(sortedSet);
            /* výsledek: 
10 = AAA    1
20 = BBBB   1
30 = CCCCC
40 = DDDDDD 1
            */

            sortedSet.RemoveWhere(d => d.X == 20 || d.X == 30);
            string tx5 = GetText(sortedSet);
            /* výsledek: 
10 = AAA    1
40 = DDDDDD 1
            */

            bool addC3 = sortedSet.Add(new Dato(30, "CCCCC  3"));        // Vrátí true...
            bool addB3 = sortedSet.Add(new Dato(20, "BBBB   2"));        // Vrátí true...
            string tx6 = GetText(sortedSet);
            /* výsledek: 
10 = AAA    1
20 = BBBB   2          -- Nově přidaný prvek, protože původní 20 byl odstraněn
30 = CCCCC  3          -- Nově přidaný prvek, protože původní 30 byl odstraněn
40 = DDDDDD 1
            */

            var reverse = sortedSet.Reverse();
            string tx7 = GetText(reverse);
            /* výsledek: 
40 = DDDDDD 1
30 = CCCCC  3
20 = BBBB   2
10 = AAA    1
            */


            // plus existuje řada metod pro práci s dvěma množinami (průniky, uniony, testy shody, přesahu atd) dostupné díky komparátoru klíče

            // sortedSet.GetViewBetween()


            // Změna hodnoty prvku = jeho klíčovou hodnotu X v době, kdy už je přítomen a zatříděn v SortedListu:
            var items = sortedSet.ToArray();
            var datoB = items[1];
            datoB.X = 35;
            string tx8 = GetText(sortedSet);
            /* výsledek:
10 = AAA    1
35 = BBBB   2          -- prvek zůstal stále na své pozici [1], i když by už měl být na pozici [2] ... za 30, viz první sloupec = klíče X
30 = CCCCC  3
40 = DDDDDD 1
            */


            // Vytvořím nov objekt z dosavadních dat, měl by se přetřídit:
            sortedSet = new SortedSet<Dato>(sortedSet, new Dato());
            string tx9 = GetText(sortedSet);
            /* výsledek:
10 = AAA    1
30 = CCCCC  3
35 = BBBB   2         -- Změněná instance BBB má nyní klíčovou hodnotu X = 35 = dostala se na správné místo.
40 = DDDDDD 1
            */



        }
        private static string GetText(IEnumerable items)
        {
            string tx = "";
            foreach (var item in items)
                tx += item.ToString() + "\r\n";
            return tx;
        }
        private static string GetText(IEnumerable<Dato> sortedSet)
        {
            string tx = "";
            foreach (var item in sortedSet)
                tx += item.ToString() + "\r\n";
            return tx;
        }
        internal class Dato : IComparer<Dato>
        {
            public Dato() { }
            public Dato(int x, string text) { X = x; Text = text; }
            public override string ToString() { return $"{X} = {Text}"; }
            public int X;
            public string Text;
            int IComparer<Dato>.Compare(Dato a, Dato b)
            {
                return a.X.CompareTo(b.X);
            }
        }
        #endregion
    }
}
