using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.TestGUI
{



    public class Tests
    {
        internal static void TestLinq(int loops)
        {
            Tests instance = new Tests();
            TestData[] data = instance.CreateTestData(250000);

            for (int loop = 0; loop < loops; loop++)
            {
                instance.TestLinq(data, 100);

            }

        }
        private Tests()
        {
            #region Jména a příjmení

            // Jména a příjmení jsou ze stránek na internetu, ze seznamů nejčastějších jmen a příjmení (stránky ministerstva vnitra):
            string namesM = "Jiří;Jan;Petr;Josef;Pavel;Jaroslav;Martin;Miroslav;Tomáš;František;Zdeněk;Václav;Karel;Milan;Michal;Vladimír;Lukáš;David;Ladislav;Jakub;Stanislav;Roman;Ondřej;Antonín;Radek;Marek;Daniel;Miloslav;" +
                            "Vojtěch;Jaromír;Filip;Ivan;Aleš;Libor;Oldřich;Rudolf;Vlastimil;Jindřich;Miloš;Adam;Lubomír;Patrik;Bohumil;Luboš;Robert;Matěj;Dominik;Radim;Richard;Ivo;Rostislav;Dušan;Luděk;Vladislav;Bohuslav;Alois;" +
                            "Vit;Vít;Štěpán;Kamil;Ján;Jozef;Zbyněk;Štefan;Viktor;Emil;Michael;Eduard;Vítězslav;Ludvík;René;Marcel;Peter;Dalibor;Radomír;Otakar;Bedřich;Šimon;Břetislav;Vilém;Vratislav;Matyáš;Radovan;Leoš;Marian;" +
                            "Igor;Přemysl;Bohumir;Bohumír;Alexandr;Kryštof;Otto;Arnošt;Svatopluk;Denis;Adolf;Hynek;Erik;Bronislav;Alexander";
            string namesF = "Marie;Jana;Eva;Anna;Hana;Věra;Lenka;Alena;Kateřina;Petra;Lucie;Jaroslava;Ludmila;Helena;Jitka;Martina;Zdeňka;Veronika;Jarmila;Michaela;Ivana;Jiřina;Monika;Tereza;Božena;Zuzana;Vlasta;Markéta;Marcela;" +
                            "Dagmar;Dana;Libuše;Marta;Irena;Miroslava;Barbora;Pavla;Eliška;Růžena;Olga;Kristýna;Andrea;Iveta;Šárka;Pavlína;Blanka;Milada;Zdenka;Klára;Renata;Nikola;Gabriela;Adéla;Radka;Simona;Milena;Miloslava;" +
                            "Iva;Daniela;Miluše;Denisa;Karolína;Romana;Aneta;Ilona;Stanislava;Květoslava;Emilie;Anežka;Naděžda;Soňa;Vladimíra;Kamila;Drahomíra;Danuše;Jindřiška;Natálie;Františka;Renáta;Mária;Alžběta;Vendula;Štěpánka;" +
                            "Bohumila;Ladislava;Magdalena;Dominika;Blažena;Žaneta;Květa;Sabina;Julie;Antonie;Alice;Kristina;Karla;Hedvika;Květuše;Alexandra;Silvie";
            string prijmM = "NOVÁK;SVOBODA;NOVOTNÝ;DVOŘÁK;ČERNÝ;PROCHÁZKA;KUČERA;VESELÝ;KREJČÍ;HORÁK;NĚMEC;MAREK;POSPÍŠIL;POKORNÝ;HÁJEK;KRÁL;JELÍNEK;RŮŽIČKA;BENEŠ;FIALA;SEDLÁČEK;DOLEŽAL;ZEMAN;KOLÁŘ;NAVRÁTIL;ČERMÁK;VANĚK;URBAN;BLAŽEK;" +
                            "KŘÍŽ;KOVÁŘ;KRATOCHVÍL;BARTOŠ;VLČEK;POLÁK;MUSIL;KOPECKÝ;ŠIMEK;KONEČNÝ;MALÝ;HOLUB;ČECH;STANĚK;KADLEC;ŠTĚPÁNEK;DOSTÁL;SOUKUP;ŠŤASTNÝ;MAREŠ;MORAVEC;SÝKORA;TICHÝ;VALENTA;VÁVRA;MATOUŠEK;ŘÍHA;BLÁHA;BUREŠ;ŠEVČÍK;" +
                            "HRUŠKA;MAŠEK;DUŠEK;PAVLÍK;HAVLÍČEK;JANDA;HRUBÝ;MACH;LIŠKA;BEDNÁŘ;MACHÁČEK;VÍTEK;BERAN";
            string prijmF = "NOVÁKOVÁ;SVOBODOVÁ;NOVOTNÁ;DVOŘÁKOVÁ;ČERNÁ;PROCHÁZKOVÁ;KUČEROVÁ;VESELÁ;HORÁKOVÁ;NĚMCOVÁ;MARKOVÁ;POKORNÁ;POSPÍŠILOVÁ;HÁJKOVÁ;KRÁLOVÁ;JELÍNKOVÁ;RŮŽIČKOVÁ;BENEŠOVÁ;FIALOVÁ;SEDLÁČKOVÁ;DOLEŽALOVÁ;ZEMANOVÁ;KOLÁŘOVÁ;" +
                            "NAVRÁTILOVÁ;ČERMÁKOVÁ;VAŇKOVÁ;URBANOVÁ;KRATOCHVÍLOVÁ;ŠIMKOVÁ;BLAŽKOVÁ;KŘÍŽOVÁ;KOPECKÁ;KOVÁŘOVÁ;BARTOŠOVÁ;VLČKOVÁ;POLÁKOVÁ;KONEČNÁ;MUSILOVÁ;ČECHOVÁ;MALÁ;STAŇKOVÁ;ŠTĚPÁNKOVÁ;HOLUBOVÁ;ŠŤASTNÁ;KADLECOVÁ;DOSTÁLOVÁ;" +
                            "SOUKUPOVÁ;MAREŠOVÁ;SÝKOROVÁ;VALENTOVÁ;MORAVCOVÁ;VÁVROVÁ;TICHÁ;MATOUŠKOVÁ;BLÁHOVÁ;ŘÍHOVÁ;MACHOVÁ;MAŠKOVÁ;ŠEVČÍKOVÁ;BUREŠOVÁ;ŠMÍDOVÁ;DUŠKOVÁ;PAVLÍKOVÁ;KREJČOVÁ;JANDOVÁ;HRUŠKOVÁ;HAVLÍČKOVÁ;HRUBÁ;BERANOVÁ;LIŠKOVÁ;BEDNÁŘOVÁ;TOMANOVÁ";

            _NamesM = namesM.Split(';');
            _PrijmM = prijmM.Split(';');
            _NamesF = namesF.Split(';');
            _PrijmF = prijmF.Split(';');

            #endregion

            _Rand = new Random(DateTime.Now.Millisecond);
        }
        private string GetRandomItem(string[] items)
        {
            return items[_Rand.Next(items.Length)];
        }
        private TestData[] CreateTestData(int count)
        {
            List<TestData> list = new List<TestData>();
            using (var scope = Application.App.Trace.Scope("Tests", "CreateTestData", "", "Count: " + count))
            {
                for (int id = 0; id < count; id++)
                {
                    TestData data = new TestData();
                    data.Id = id;
                    CreateRandomName(out data.Name, out data.Prijm);
                    data.Birthday = GetRandomDate(-75);
                    list.Add(data);
                }
            }
            return list.ToArray();
        }
        private void TestLinq(TestData[] data, int count)
        {
            using (var scope = Application.App.Trace.Scope("Tests", "TestLinq", "", "Count: " + count))
            {
                for (int test = 0; test < count; test++)
                {
                    string name, prijm;
                    CreateRandomName(out name, out prijm);
                    TestData[] found = data.Where(d => d.Name == name && d.Prijm == prijm).ToArray();
                }
                double time1 = scope.ElapsedTime.TotalMilliseconds / (double)count;
                scope.AddItem("Time/1 linq: " + time1.ToString("### ##0.000") + " milisec");
            }
        }
        private void CreateRandomName(out string name, out string prijm)
        {
            if (_Rand.Next(0, 100) > 50)
            {
                name = GetRandomItem(_NamesM);
                prijm = GetRandomItem(_PrijmM);
            }
            else
            {
                name = GetRandomItem(_NamesF);
                prijm = GetRandomItem(_PrijmF);
            }
        }
        private DateTime GetRandomDate(int years)
        {
            DateTime now = DateTime.Now;
            DateTime end = now.AddYears(years);
            TimeSpan time = end - now;
            return now.AddDays(time.TotalDays * _Rand.NextDouble());
        }
        internal class TestData
        {
            public override string ToString()
            {
                return Id + ": " + Name + " " + Prijm + "; nar. " + Birthday;
            }
            public string Name;
            public string Prijm;
            public int Id;
            public DateTime Birthday;

        }
        private string[] _NamesM;
        private string[] _NamesF;
        private string[] _PrijmM;
        private string[] _PrijmF;

        private Random _Rand;


    }
}
