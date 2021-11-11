using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Noris.Clients.Win.Components;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress
{
    /// <summary>
    /// Generátor náhodných textů
    /// </summary>
    public class Random
    {
        #region Náhodné slovo, věta, odstavec
        /// <summary>
        /// Vrať náhodné slovo
        /// </summary>
        /// <param name="firstUpper"></param>
        /// <returns></returns>
        public static string GetWord(bool firstUpper = false)
        {
            string word = WordBook[Rand.Next(WordBook.Length)];
            if (firstUpper) word = word.Substring(0, 1).ToUpper() + word.Substring(1);
            return word;
        }
        /// <summary>
        /// Vrať náhodnou sadu vět
        /// </summary>
        /// <param name="minWordCount"></param>
        /// <param name="maxWordCount"></param>
        /// <param name="minSentenceCount"></param>
        /// <param name="maxSentenceCount"></param>
        /// <returns></returns>
        public static string GetSentences(int minWordCount, int maxWordCount, int minSentenceCount, int maxSentenceCount)
        {
            string sentences = "";
            int sentenceCount = Rand.Next(minSentenceCount, maxSentenceCount);
            string eol = Environment.NewLine;
            for (int s = 0; s < sentenceCount; s++)
            {
                string sentence = GetSentence(minWordCount, maxWordCount, true);
                if (sentences.Length > 0)
                {
                    if (Rand.Next(3) == 0) sentences += eol;
                    else sentences += " ";
                }
                sentences += sentence;
            }
            return sentences;
        }
        /// <summary>
        /// Vrať pole náhodných vět
        /// </summary>
        /// <param name="minWordCount"></param>
        /// <param name="maxWordCount"></param>
        /// <param name="minSentenceCount"></param>
        /// <param name="maxSentenceCount"></param>
        /// <param name="addDot"></param>
        /// <returns></returns>
        public static string[] GetSentencesArray(int minWordCount, int maxWordCount, int minSentenceCount, int maxSentenceCount, bool addDot = false)
        {
            List<string> sentences = new List<string>();
            int sentenceCount = Rand.Next(minSentenceCount, maxSentenceCount);
            string eol = Environment.NewLine;
            for (int s = 0; s < sentenceCount; s++)
            {
                string sentence = GetSentence(minWordCount, maxWordCount, addDot);
                sentences.Add(sentence);
            }
            return sentences.ToArray();
        }
        /// <summary>
        /// Vrať náhodnou větu
        /// </summary>
        /// <param name="minCount"></param>
        /// <param name="maxCount"></param>
        /// <param name="addDot"></param>
        /// <returns></returns>
        public static string GetSentence(int minCount, int maxCount, bool addDot = false)
        {
            int count = Rand.Next(minCount, maxCount);
            return GetSentence(count, addDot);
        }
        /// <summary>
        /// Vrať náhodnou větu
        /// </summary>
        /// <param name="count"></param>
        /// <param name="addDot"></param>
        /// <returns></returns>
        public static string GetSentence(int count, bool addDot = false)
        {
            string sentence = "";
            for (int w = 0; w < count; w++)
                sentence += (sentence.Length > 0 ? ((Rand.Next(12) < 1) ? ", " : " ") : "") + GetWord((w == 0));
            if (addDot)
                sentence += ".";
            return sentence;
        }
        #endregion
        #region Zdroje slov
        /// <summary>
        /// Náhodná slova
        /// </summary>
        public static string[] WordBook { get { if (_WordBook is null) _WordBook = _GetWordBook(); return _WordBook; } }
        private static string[] _WordBook;
        /// <summary>
        /// Vrátí pole náhodných slov
        /// </summary>
        /// <returns></returns>
        private static string[] _GetWordBook()
        {
            string text = Text0B;

            // Některé znaky odstraníme, text rozdělíme na slova, a z nich vybereme pouze slova se 4 znaky a více:
            text = text.Replace("„", " ");
            text = text.Replace("“", " ");
            text = text.Replace(".", " ");
            text = text.Replace(",", " ");
            text = text.Replace(";", " ");
            text = text.Replace(":", " ");
            text = text.Replace("?", " ");
            text = text.Replace("!", " ");
            text = text.ToLower();
            var words = text.Split(" \r\n\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            return words.Where(w => w.Length >= 4).ToArray();
        }
        /// <summary>
        /// Text "Tři muži na toulkách"
        /// </summary>
        public static string Text0
        {
            get
            {
                return @"„Potřebujeme změnit způsob života,“ řekl Harris.
V tom okamžiku se otevřely dveře a nakoukla k nám paní Harrisová; že prý ji posílá Ethelberta, aby mi
připomněla, že kvůli Clarencovi nesmíme přijít domů moc pozdě. Já si teda myslím, že Ethleberta si o naše děti
dělá zbytečné starosti. Tomu klukovi vlastně vůbec nic nebylo. Dopoledne byl venku s tetou; a když se roztouženě
zakouká do výkladní skříně u cukráře, ta teta ho vezme do krámu a tak dlouho mu kupuje trubičky se šlehačkou a
mandlové dorty, dokud kluk neprohlásí, že už nemůže, a zdvořile, leč rezolutně cokoli dalšího sníst neodmítne. U
oběda pak pochopitelně nechce druhou porci nákypu a Ethelberta si hned myslí, že je to příznak nějaké nemoci.
Paní Harrisová dále dodala, že bychom vůbec měli přijít co nejdřív nahoru, pro své vlastní blaho, jinak že
zmeškáme výstup slečny Muriel, která přednese Bláznivou svačinu z Alenky v kraji divů. Muriel je ta Harrisova
starší, osmiletá; dítě bystré a inteligentní; ale já za svou osobu ji poslouchám raději, když recituje něco vážného.
Řekli jsme paní Harrisové, že jen dokouříme cigarety a přijdeme co nevidět; a prosili jsme ji, aby Muriel
nedovolila začít, dokud tam nebudeme. Slíbila, že se vynasnaží udržet to dítko na uzdě co nejdéle, a odešla. A
jakmile se za ní zavřely dveře, Harris se vrátil k větě, při níž ho prve přerušila.
„Důkladně změnit způsob života,“ řekl. „No však vy víte, jak to myslím.“
Problém byl jenom v tom, jak toho dosáhnout.
George navrhoval „úřední záležitost“. To bylo pro George typické, takový návrh. Svobodný mládenec se
domnívá, že vdaná žena nemá ponětí, jak se vyhnout parnímu válci. Znal jsem kdysi jednoho mládence, inženýra,
který si usmyslil, že si „v úřední záležitosti“ vyjede do Vídně. Jeho žena si přála vědět, v jaké úřední záležitosti.
Řekl jí tedy, že dostal za úkol navštívit všechny doly v okolí rakouského hlavního města a podat o nich hlášení.
Manželka prohlásila, že pojede s ním; taková to byla manželka. Pokoušel se jí to rozmluvit; vykládal jí, že důl není
vhodné prostředí pro krásnou ženu. Odvětila, to že instinktivně vycítila sama, a že s ním tedy nebude fárat dolů do
šachet; jen ho k nim každé ráno doprovodí a pak se až do jeho návratu na zem bude bavit po svém; bude se dívat
po vídeňských obchodech a sem tam si koupí pár věciček, které se jí třeba budou hodit. Protože s tím programem
přišel sám, nevěděl, chudák, jak se z něho vyvléci; a tak deset dlouhých letních dní skutečně trávil v dolech v okolí
Vídně a po večerech o nich psal hlášení a jeho žena je sama odesílala jeho firmě, která o ně vůbec neměla zájem.
Ne že bych si myslel, že Ethelberta nebo paní Harrisová patří k téhle sortě žen, ale s „úřední záležitostí“ se
prostě nemá přehánět - tu si má člověk schovávat jen pro případ potřeby zcela naléhavé.
„Ne, ne,“ pravil jsem tedy, „na to se musí jít zpříma a mužně. Já řeknu Ethelbertě, že jsem dospěl k závěru, že
manžel nikdy nepozná pravou cenu štěstí, když se mu těší neustále. Řeknu jí, že se chci naučit vážit si výhod, jichž
se mi dostává, vážit si jich tak, jak si to zasluhují, a z toho důvodu že se nejméně na tři týdny hodlám násilím
odtrhnout od ní a od dětí. A řeknu jí,“ dodal jsem obraceje se k Harrisovi, „žes to byl ty, kdo mě na mé povinnosti
v tomto směru upozornil; tobě že vděčíme...“
Harris tak nějak zbrkle postavil na stůl svou sklenici.
„Jestli tě smím o něco prosit, člověče,“ přerušil mě, „tak nic takového radši neříkej. Ethelberta by se o tom
určitě zmínila mé ženě a... no, já si zkrátka nechci přisvojovat zásluhy, které mi nepatří.“
„Jak to, že ti nepatří?“ pravil jsem. „To byl přece tvůj nápad!“
„Ale tys mě na něj přivedl,“ znovu mě přerušil Harris. „Říkal jsi přece, že to je chyba, když člověk zapadne do
vyježděných kolejí, a nepřetržitý život v kruhu rodinném že otupuje ducha.“
„To bylo míněno všeobecně,“ vysvětloval jsem.
„Mně to připadalo velice výstižné,“ pravil Harris, „a říkal jsem si, že to budu citovat Claře; Clara si o tobě
myslí, že jsi člověk velice rozumný, vím, že má o tobě vysoké mínění. A tak jsem přesvědčen, že...“
Teď jsem zase já přerušil jeho: „Hele, radši nebudeme nic riskovat. Tohle jsou choulostivé věci. A já už vím,
jak na to. Řekneme, že s tím nápadem přišel George.“
Georgeovi, jak si často s rozhořčením všímám, naprosto chybí přívětivá ochota podat někomu pomocnou ruku.
Řekli byste, že příležitost vysvobodit dva staré kamarády z těžkého dilematu přímo uvítá; ale on místo toho velice
zprotivněl.
„Jen si to zkuste,“ pravil, „a já jim oběma řeknu, že můj původní návrh zněl, abychom jeli společně, s dětmi a s
celými rodinami; já že bych byl s sebou vzal svou tetu a že jsme si mohli najmout rozkošný starý zámeček v
Normandii, o kterém dobře vím a který stojí hned u moře, kde je podnebí speciálně vhodné pro choulostivé dětičky
a kde mají mléko, jaké se v Anglii nesežene. A ještě jim řeknu, že vy jste ten návrh úplně rozmetali kategorickou
námitkou, že nám bude mnohem líp, když pojedeme sami.“
S člověkem, jako je George, nemá smysl jednat vlídně; na takového platí jen pevná rozhodnost.
„Jen si to zkus,“ pravil Harris, „a já, co mě se týče, tu tvou nabídku okamžitě přijmu. A ten zámek si najmeme.
Ty s sebou vezmeš tetu - o to se postarám - a protáhneme si to na celý měsíc. Naše děcka se v tobě vidí; Jerome a
já nebudeme nikdy k dosažení. Slíbil jsi, že Edgara naučíš chytat ryby; a hrát si na divokou zvěř, to taky zůstane na
tobě. Dick a Muriel beztak od minulé neděle nemluví o ničem jiném než o tom, jak jsi jim dělal hrocha. Budeme v
hájích pořádat společné pikniky - jenom nás jedenáct - a večer co večer bude na programu hudba a recitace.
Muriel, jak víš, umí zpaměti už šest básniček; a ostatní děti se učí ohromně rychle.“
A tak se George podvolil - on nemá moc pevné zásady -, i když ne zrovna ochotně. Co mu prý zbývá, když jsme
takoví neřádi a zbabělci a falešníci, že bychom se dokázali snížit k tak mrzkým úkladům? A jestli prý nemám v
úmyslu vypít celou láhev toho claretu sám, tak ať mu laskavě naleju aspoň jednu skleničku. A ještě dodal, poněkud
nelogicky, že je to ostatně úplně jedno, neboť jak Ethelberta tak paní Harrisová jsou ženy velice prozíravé a mají o
něm mnohem lepší mínění, než aby byť jenom na okamžik uvěřily, že by takový návrh mohl opravdu vyjít od něho.";
            }
        }
        /// <summary>
        /// Text "Tři muži na toulkách" celá první kapitola
        /// </summary>
        public static string Text0B
        {
            get
            {
                return @"„Potřebujeme změnit způsob života,“ řekl Harris.
V tom okamžiku se otevřely dveře a nakoukla k nám paní Harrisová; že prý ji posílá Ethelberta, aby mi
připomněla, že kvůli Clarencovi nesmíme přijít domů moc pozdě. Já si teda myslím, že Ethleberta si o naše děti
dělá zbytečné starosti. Tomu klukovi vlastně vůbec nic nebylo. Dopoledne byl venku s tetou; a když se roztouženě
zakouká do výkladní skříně u cukráře, ta teta ho vezme do krámu a tak dlouho mu kupuje trubičky se šlehačkou a
mandlové dorty, dokud kluk neprohlásí, že už nemůže, a zdvořile, leč rezolutně cokoli dalšího sníst neodmítne. U
oběda pak pochopitelně nechce druhou porci nákypu a Ethelberta si hned myslí, že je to příznak nějaké nemoci.
Paní Harrisová dále dodala, že bychom vůbec měli přijít co nejdřív nahoru, pro své vlastní blaho, jinak že
zmeškáme výstup slečny Muriel, která přednese Bláznivou svačinu z Alenky v kraji divů. Muriel je ta Harrisova
starší, osmiletá; dítě bystré a inteligentní; ale já za svou osobu ji poslouchám raději, když recituje něco vážného.
Řekli jsme paní Harrisové, že jen dokouříme cigarety a přijdeme co nevidět; a prosili jsme ji, aby Muriel
nedovolila začít, dokud tam nebudeme. Slíbila, že se vynasnaží udržet to dítko na uzdě co nejdéle, a odešla. A
jakmile se za ní zavřely dveře, Harris se vrátil k větě, při níž ho prve přerušila.
„Důkladně změnit způsob života,“ řekl. „No však vy víte, jak to myslím.“
Problém byl jenom v tom, jak toho dosáhnout.
George navrhoval „úřední záležitost“. To bylo pro George typické, takový návrh. Svobodný mládenec se
domnívá, že vdaná žena nemá ponětí, jak se vyhnout parnímu válci. Znal jsem kdysi jednoho mládence, inženýra,
který si usmyslil, že si „v úřední záležitosti“ vyjede do Vídně. Jeho žena si přála vědět, v jaké úřední záležitosti.
Řekl jí tedy, že dostal za úkol navštívit všechny doly v okolí rakouského hlavního města a podat o nich hlášení.
Manželka prohlásila, že pojede s ním; taková to byla manželka. Pokoušel se jí to rozmluvit; vykládal jí, že důl není
vhodné prostředí pro krásnou ženu. Odvětila, to že instinktivně vycítila sama, a že s ním tedy nebude fárat dolů do
šachet; jen ho k nim každé ráno doprovodí a pak se až do jeho návratu na zem bude bavit po svém; bude se dívat
po vídeňských obchodech a sem tam si koupí pár věciček, které se jí třeba budou hodit. Protože s tím programem
přišel sám, nevěděl, chudák, jak se z něho vyvléci; a tak deset dlouhých letních dní skutečně trávil v dolech v okolí
Vídně a po večerech o nich psal hlášení a jeho žena je sama odesílala jeho firmě, která o ně vůbec neměla zájem.
Ne že bych si myslel, že Ethelberta nebo paní Harrisová patří k téhle sortě žen, ale s „úřední záležitostí“ se
prostě nemá přehánět - tu si má člověk schovávat jen pro případ potřeby zcela naléhavé.
„Ne, ne,“ pravil jsem tedy, „na to se musí jít zpříma a mužně. Já řeknu Ethelbertě, že jsem dospěl k závěru, že
manžel nikdy nepozná pravou cenu štěstí, když se mu těší neustále. Řeknu jí, že se chci naučit vážit si výhod, jichž
se mi dostává, vážit si jich tak, jak si to zasluhují, a z toho důvodu že se nejméně na tři týdny hodlám nísilím
odtrhnout od ní a od dětí. A řeknu jí,“ dodal jsem obraceje se k Harrisovi, „žes to byl ty, kdo mě na mé povinnosti
v tomto směru upozornil; tobě že vděčíme...“
Harris tak nějak zbrkle postavil na stůl svou sklenici.
„Jestli tě smím o něco prosit, člověče,“ přerušil mě, „tak nic takového radši neříkej. Ethelberta by se o tom
určitě zmínila mé ženě a... no, já si zkrátka nechci přisvojovat zásluhy, které mi nepatří.“
„Jak to, že ti nepatří?“ pravil jsem. „To byl přece tvůj nápad!“
„Ale tys mě na něj přivedl,“ znovu mě přerušil Harris. „Říkal jsi přece, že to je chyba, když člověk zapadne do
vyježděných kolejí, a nepřetržitý život v kruhu rodinném že otupuje ducha.“
„To bylo míněno všeobecně,“ vysvětloval jsem.
„Mně to připadalo velice výstižné,“ pravil Harris, „a říkal jsem si, že to budu citovat Claře; Clara si o tobě
myslí, že jsi člověk velice rozumný, vím, že má o tobě vysoké mínění. A tak jsem přesvědčen, že...“
Teď jsem zase já přerušil jeho: „Hele, radši nebudeme nic riskovat. Tohle jsou choulostivé věci. A já už vím,
jak na to. Řekneme, že s tím nápadem přišel George.“
Georgeovi, jak si často s rozhořčením všímám, naprosto chybí přívětivá ochota podat někomu pomocnou ruku.
Řekli byste, že příležitost vysvobodit dva staré kamarády z těžkého dilematu přímo uvítá; ale on místo toho velice
zprotivněl.
„Jen si to zkuste,“ pravil, „a já jim oběma řeknu, že můj původní návrh zněl, abychom jeli společně, s dětmi a s
celými rodinami; já že bych byl s sebou vzal svou tetu a že jsme si mohli najmout rozkošný starý zámeček v
Normandii, o kterém dobře vím a který stojí hned u moře, kde je podnebí speciálně vhodné pro choulostivé dětičky
a kde mají mléko, jaké se v Anglii nesežene. A ještě jim řeknu, že vy jste ten návrh úplně rozmetali kategorickou
námitkou, že nám bude mnohem líp, když pojedeme sami.“
S člověkem, jako je George, nemá smysl jednat vlídně; na takového platí jen pevná rozhodnost.
„Jen si to zkus,“ pravil Harris, „a já, co mě se týče, tu tvou nabídku okamžitě přijmu. A ten zámek si najmeme.
Ty s sebou vezmeš tetu - o to se postarám - a protáhneme si to na celý měsíc. Naše děcka se v tobě vidí; Jerome a
já nebudeme nikdy k dosažení. Slíbil jsi, že Edgara naučíš chytat ryby; a hrát si na divokou zvěř, to taky zůstane na
Tři muži na toulkách
174
tobě. Dick a Muriel beztak od minulé neděle nemluví o ničem jiném než o tom, jak jsi jim dělal hrocha. Budeme v
hájích pořádat společné pikniky - jenom nás jedenáct - a večer co večer bude na programu hudba a recitace.
Muriel, jak víš, umí zpaměti už šest básniček; a ostatní děti se učí ohromně rychle.“
A tak se George podvolil - on nemá moc pevné zásady -, i když ne zrovna ochotně. Co mu prý zbývá, když jsme
takoví neřádi a zbabělci a falešníci, že bychom se dokázali snížit k tak mrzkým úkladům? A jestli prý nemám v
úmyslu vypít celou láhev toho claretu sám, tak ať mu laskavě naleju aspoň jednu skleničku. A ještě dodal, poněkud
nelogicky, že je to ostatně úplně jedno, neboť jak Ethelberta tak paní Harrisová jsou ženy velice prozíravé a mají o
něm mnohem lepší mínění, než aby byť jenom na okamžik uvěřily, že by takový návrh mohl opravdu vyjít od něho.
Tento nedůležitý bod byl tedy projednán a zbývala otázka, jak máme způsob života změnit.
Harris byl, jako obvykle, pro moře. Ví prý o jedné plachetnici - pro nás jako stvořené -, kterou bychom dokázali
zvládnout sami, bez té hordy ulejváků, kteří se jen flákají a přitom stojí spoustu peněz a zbavují plavbu veškeré
romantiky. Když prý bude mít k ruce jednoho šikovného plavčíka, může ji řídit úplně sám. Ale my jsme tu
plachetnici znali a hned jsme mu to připomněli; už jsme si na ní jednou s Harrisem vyjeli. Ta loď páchne ztuchlou
vodou, které má plné dno, a zkaženou zeleninou a spoustou všelijakých dalších smradů, vedle nichž žádný
normální mořský vzduch nemá nejmenší naději se prosadit. A pokud by si měl přijít na své jenom čich, pak přece
můžeme strávit jeden týden někde v rybí tržnici. Kromě toho není na té lodi místečko, kam by se člověk mohl
schovat před deštěm; kajuta má rozměry tři a půl metru krát jeden a půl metru a polovinu toho prostoru zabírají
kamna, která se rozpadávají, jak jen se chystáte v nich zatopit. Koupat se musíte na palubě a ručník uletí do moře,
zrovna když lezete z kádě. Harris a plavčík obstarávají všechnu práci, která může člověka bavit - napínají a
podkasávají plachty, kýlují loď a pouštějí si ji volně po větru a tak podobně - zatímco na George a mne zbývá
loupání brambor a umývání nádobí.
„No prosím,“ řekl Harris, „tak si teda opatříme pořádnou jachtu s kapitánem a vyjeďme si se vší parádou!“
Ale já jsem i proti tomuto řešení protestoval. Tyhle kapitány moc dobře znám; výlet po moři, to pro ně znamená
kotvit v dosahu souše, aby měli pár kroků k ženě a k rodině, o zamilované hospodě ani nemluvě.
Před lety, když jsem byl ještě mladý a nezkušený, jsem si jednou sám najal plachetnici. Tři okolnosti se spojily,
aby mě vehnaly do toho nerozumu: potkalo mě neočekávané štěstí; Ethelberta projevila touhu po mořském vánku;
a zrovna příští ráno jsem v klubu čirou náhodou vzal do ruky jedno číslo Sportovce a přišel tam na tento inzerát:
MILOVNÍKŮM PLACHTĚNÍ. - Jedinečná příležitost - Ferina, jola o 28 tunách. - Vlastník, odvolaný náhle v
obchodní záležitosti do ciziny, ochotně pronajme tohoto luxusně vybaveného „mořského chrta“ na jakoukoli kratší
i delší dobu. - Dvě kajuty a salón; pianino značky Woffenkoff; nový kotel na vyvářku. - 10 guinejí týdně. - Bližší
informace u firmy Pertwee a spol., Bucklersbury 3A.
To mi připadalo jako vyslyšená modlitba. „Nový kotel na vyvářku“ mě sice nikterak nezajímal; těch pár věcí,
které bude zapotřebí přeprat, klidně počká až domů, říkal jsem si. „Pianino značky Woffenkoff“, to však znělo
lákavě. Představil jsem si Ethelbertu, jak večer hraje - něco s refrénem, který po několika zkouškách může s námi
sborově pět posádka -, zatímco náš pohyblivý domov uhání „jako chrt“ po stříbrných vlnách.
Vzal jsem si drožku a na udanou adresu jsem si rovnou zajel. Pan Pertwee byl pán nenápadného vzhledu a měl
neokázalou kancelář ve třetím poschodí. Ukázal mi akvarel Feriny, letícího po větru. Paluba byla skloněna k
oceánu v úhlu 95 stupňů. Na palubě nebyla na tom obrázku ani živá duše; všecky patrně sklouzly do moře. Já taky
nechápu, jak by se tam někdo byl mohl udržet, ledaže by byl přitlučen hřebíky. Poukázal jsem na tuto nevýhodu,
ale agent mi vyložil, že ten obrázek představuje Ferinu v okamžiku, kdy se za onoho svého pověstného vítězného
závodu o medwayský pohár obrací o 180 stupňů a obeplouvá, nebím už co. Pan Pertwee předpokládal, že o té
události je mi všechno známo, tak jsem se radši na nic neptal. Dvě skvrnky až u samého rámu, které jsem v prní
chvíli pokládal za moly, zobrazovaly, jak se ukázalo, druhého a třetího vítěze v té proslulé regatě. Fotografie
Feriny kotvícího u Gravesendu, byla už méně impozantní, zato slibovala větší stabilitu. A jelikož všechny
odpovědi na mé dotazy zněly uspokojivě, najal jsem si tu loď na čtrnáct dní. Pan Pertwee pravil, že to je šťastná
náhoda, že ji chci pouze na čtrnáct dní - později jsem mu dal za pravdu -, protože ta doba přesně navazuje na další
pronájem. Kdybych prý Ferinu žádal na tři týdny, nemohl by mi vůbec vyhovět.
Když jsme se takto dohodli, pan Pertwee se mě zeptal, jestli jsem si už vyhlédl nějakého kapitána. Že jsem si
žádného nevyhlédl, to byla další šťastná náhoda - štěstí mi zřejmě přálo ve všem všudy - ježto pan Pertwee byl
přesvědčen, že nemohu udělat nic lepšího, než se přidržet pana Goylese, v jehož péči se loď v současné době
nalézá; je to znamenitý kapitán, ujišťoval mě pan Pertwee, námořník, který zná moře jako manžel vlastní manželku
a pod jehož velením nikdy nikdo nepřišel o život.
Pořád ještě bylo časné dopoledne a plachetnice kotvila u Harwiche. Chytil jsem vlak v deset pětačtyřicet z
nádraží Liverpool Street a v jednu hodinu jsem už rozmlouval s panem Goylesem přímo na palubě. Pan Goyles byl
obtloustlý chlapík, který měl v sobě něco otcovského. Vyložil jsem mu, jak si to představuji, že bych totiž rád
obeplul ty ostrovy nad Holandskem a pak to vzal nahoru k Norsku. Kapitán řekl „Výborně, pane,“ a zatvářil se,
jako kdyby ho vyhlídka na tu cestu nadchla; sám prý z ní bude mít požitek. Přešli jsme k otázce potravinových
zásob a pan Goyles projevil nadšení ještě větší. Přiznávám se ovšem, že množství potravin, které navrhoval, mě
překvapilo. Být to v dobách kapitána Drakea a španělského panství v karibské oblasti, byl bych pojal obavy, že se
chystá k něčemu nezákonnému. Ale on se tím svým otcovským způsobem zasmál a ujistil mě, že to nikterak
nepřeháníme. A když něco zbude, tak si to rozdělí a vezme domů lodní posádka - tak to bylo zřejmě zvykem. Já
měl sice dojem, že tu posádku zásobím na celou zimu, ale nechtěl jsem vypadat jako držgrešle, a tak jsem už nic
neříkal. Požadované množství nápojů mě překvapilo rovněž. Já jsem naplánoval tolik, kolik jsme podle mého
Tři muži na toulkách
175
odhadu mohli spotřebovat my sami, a pak pak Goyles zahovořil jménem posádky. K jeho cti musím říci, že o to
své mužstvo vskutku pečoval.
„Neradi bychom zažili nějaké orgie, pane Goylesi,“ namítl jsem.
„Orgie!“ zvolal pan Goyles. „Tahle kapička jim stačí tak akorát do čaje!“
A vysvětlil mi, že se řídí heslem: „Opatři si dobrý chlapy a dobře se o ně starej!“
„Pak vám odvedou lepší práci,“ dodal pan Goyles, „a rádi přijdou zas.“
Já za svou osobu jsem si ani moc nepřál, aby přišli zas. Začínal jsem jich mít až po krk, a to jsem je ještě ani
neviděl; pro mě to byla posádka chamtivá a nenažraná. Ale pan Goyles stál tak bodře na svém a já byl tak
nezkušený, že jsem mu opět nechal volnou ruku. A on slíbil, že osobně dohlédne, aby ani v této kategorii nepřišlo
nic nazmar.
I výběr členů posádky jsem nechal na něm. Říkal, že kvůli mně to všechno může zastat, a taky zastane, s dvěma
lodníky a jedním plavčíkem. Jestli narážel na likvidaci zásob jídla a pití, pak na to taková posádka rozhodně
nemohla stačit; ale snad měl na mysli obsluhu plachetnice.
Na zpáteční cestě jsem se stavil u svého krejčího a objednal jsm si úbor na jachtu a bílý klobouk, a krejčí slíbil,
že sebou hodí a že to všechno ušije včas. A pak jsem se vrátil domů a řekl jsem Ethelbertě, co všechno jsem
zařídil. Její radost kalila jen jediná obava - jestli jí švadleny budou moci včas ušít úbor na jachtu. To jsou ty
ženské!
Svatební cestu, na kterou jsme si vyjeli teprve před nedávnem, jsme museli předčasně ukončit, a tak jsme se
rozhodli, že tentokrát s sebou nikoho nepozveme a necháme si celou plachetnici jenom pro sebe. A že jsme se
takto rozhodli, za to dodnes děkuji nebesům. V pondělí jsme se nastrojili do samých nových věcí a vyrazili jsme.
Co měla na sobě Ethelberta, to už si nepamatuji, vím jenom, že jí to ohromně slušelo. Já měl oblek tmavomodrý,
lemovaný úzkou bílou paspulkou, což bylo myslím velice efektní.
Pan Goyles nás už čekal na palubě a oznámil nám, že je prostřeno k obědu. Kuchaře, to musím uznat, opatřil
znamenitého. Schopnosti ostatních členů posádky jsem neměl příležitost posoudit. Ale podle toho, jak vypadali za
odpočinku, mohu říci, že dělali dojem mužstva sympatického.
Představoval jsem si, že až se i posádka naobědvá, ihned zvedneme kotvu a že se budu opírat o zábradlí, s
doutníkem v ústech a s Ethelbertou po boku, a budu se dívat, jak se bílé útesy mé otčiny pomaloučku noří za obzor.
Ethelberta i já jsme se svých rolí v tomto představení řádně ujali a čekali jsme, majíce celou palubu sami pro sebe.
„Dávají si načas,“ prohodila Ethelberta.
„Nu,“ odvětil jsem já, „jestli mají ve čtrnácti dnech sníst aspoň polovičku toho, co je v této lodi uskladněno,
budou potřebovat na každé jídlo hezky slušnou dobu. Radši na ně nespěchejme, nebo z toho všeho nespořádají ani
čtvrtinu.“
„Zřejmě šli už spat,“ řekla Ethelberta o něco později. „Vždyť je pomalu čas na svačinu.“
Chovali se vskutku velice tiše. Šel jsem na příď a zavolal jsem dolů pod schůdky na kapitána Goylese. Zavolal
jsem na něj třikrát, a teprve potom se pomalu vyškrábal nahoru. Připadal mi nemotornější a starší, než když jsem
ho viděl naposled. Z pusy mu trčel vyhaslý doutník.
„Až budete se vším hotov, pane kapitáne, tak vyplujeme,“ řekl jsem.
Kapitán Goyles vyňal z pusy ten doutník.
„Dneska ne, pane, když dovolíte,“ odvětil.
„Ale! Copak se vám na dnešku nelíbí?“ zeptal jsem se. Vím, že námořníci jsou cháska pověrčivá, a tak jsem si
myslel, že pondělek třeba považují za den nešťastný.
„Den ten by nevadil,“ odpověděl kapitán Goyles, „ale vítr mi dělá starosti. A nevypadá na to, že by se chtěl
změnit.“
„Copak potřebujeme, aby se změnil?“ divil jsem se. „Podle mého je přesně takový, jaký má být - měli bychom
ho v zádech.“
„No právě, právě,“ přitakal kapitán Goyles, „v zádech, to je ten správnej výraz. V těch zádech bysme totiž měli
smrt, kdybysme, nedej Pámbu, museli v tomhle vyplout. Abyste rozuměl, pane,“ vysvětloval v odpověď na můj
udivený pohled, „tohle je vítr, kterýmu my říkáme »soušák«, poněvadž fouká přímo od souše.“
Musel jsem uznat, že ten člověk má pravdu: vítr skutečně foukal přímo od souše.
„V noci se možná obrátí,“ pravil kapitán Goyles už trochu nadějněji. „Naštěstí není prudkej, a tahle loď sedí
pevně.“
Pak si dal dutník zpátky dopusy a já se vrátil na záď a vyložil jsem Ethelbertě, proč se náš odjezd odkládá.
Ethelberta už neměla tak jásavou náladu, jako když jsme se nalodili, a přála si vědět, proč nemůžeme vyplout,
když vítr fouká od souše.
„Kdyby nevanul od souše,“ prohlásila, „tak by vanul od moře a hnal by nás zpátky ke břehu. Já si myslím, že
tohle je zrovna ten vítr, jaký potřebujeme.“
„To z tebe mluví tvá nezkušenost, miláčku,“ poučoval jsem ji. „To se jen tak zdá, že tohle je zrovna ten vítr,
jaký potřebujeme, ale není to ten vítr. Tomuhle větru my říkáme soušák a soušák je vždycky velice nebezpečný.
Ethelberta chtěla vědět, proč je soušák velice nebezpečný.
Ta její neústupnost mně už začínala jít na nervy; ale asi jsem sám nebyl v nejlepší kondici; monotónní houpání
zakotvené plachetničky působí na činorodého ducha depresívně.
„Podrobně ti to vysvětlit nemůžu,“ odvětil jsem podle pravdy, „vím jenom, že vyplout za tohoto větru by byl
vrchol hazardérství a mně na tobě příliš záleží, má drahá, než abych tě zbytečně vystavoval nebezpečí.“
Tři muži na toulkách
176
To jsem považoval za dost obratné uzavření debaty, ale Ethelberta na to podotkla, že za těchto okolností jsme
se mohli klidně nalodit až v úterý, a sešla do podpalubí.
Nazítří se vítr otočil k severu; byl jsem vzhůru ož od časného jitra a hned jsem na tu změnu upozornil kapitána
Goylese.
„No práve, právě, pane,“ pravil. „Je to smůla, ale to se nedá nic dělat.“
„Tak vy máte za to, že dnes vyplout nemůžeme?“ odvážil jsem se zeptat.
Kapitán se na mně nerozhněval. Jenom se zasmál.
„Inu, pane,“ povídá, „kdybyste chtěl jet do Ipswiche, tak bysme lepší vítr mít nemohli, jenomže my máme
namířeno k holandskýmu pobřeží a v tom pádě - no co vám mám povídat?“
Sdělil jsem tuto novinu Ethelbertě a dohodli jsme se, že ten den strávíme na břehu. Harwich není moc veselé
město, k večeru je tam dokonce dost velká nuda. „U doverského dvora“ jsme si dali čaj a pár chlabíčků s
řeřichovým salátem a pak jsme se vrátili na nábřeží, abychom se podívali, co dělá loď a kapitán Golyes. Na
kapitána jsme čekali celou hodinu. Když přišel, byl v mnohem lepší náladě než my; kdyby mi sám nebyl řekl, že
než jde na kutě, jakživ nevypije víc než jednu sklenici horkého grogu, byl bych si myslel, že je opilý.
Nazítří ráno vanul vítr k jihu, což kapitána Goylese nemálo zneklidnilo; ukázalo se totiž, že je stejně
nebezpečné vyplout, jako zůstat tam, kde jsme; zbývala nám jediná naděje: že se vítr změní, dřív než se nám něco
stane. To už Ethelberta pojala k té plachetnici značnou nechuť; prohlásila, že by mnohem raději strávila týden v
takové té lázeňské kabině na kolečkách, neboť lázeňská kabina na kolečkách se aspoň dá pevně postavit.
Strávili jsme další den v Harwichi a noc po něm - i tu příští - jsme přespali „U královy hlavy“, jelikož vítr vanul
neustále k jihu. V pátek foukal vítr přesně od východu. Kapitána Goylese jsem potkal na nábřeží a zmínil jsem se
mu, že za těchto okolností bychom se snad mohli vydat na cestu. Má umíněnost ho zjevně rozčílila.
„Kdybyste se v tom kapánel líp vyznal, pane,“ pravil, „sám byste pochopil, že to není možný. Vždyť vítr fouká
rovnou od moře.“
„Pane kapitáne,“ řekl jsem, „povězte mi laskavě, co jsem si to vlastně najal. Je to plachetnice nebo hausbót?“
Vypadal, jako kdyby ho ten dotaz překvapil.“
„Je to jola,“ povídá.
„Abyste totiž rozuměl, oč mi jde,“ na to já. „Může se s tou věcí vůbec pohnout? Nebo je tady pevně
přimontovaná? Jestli je přimontovaná,“ dodal jsem, „upřímně mi to prozraďte, a my si seženeme pár truhlíků s
břečťanem, dáme si je pod okénka z kajuty, palubu si osázíme dalšími kytičkami a natáhneme si přes ni plátěnou
střechu a prostě si to tady pěkně zvelebíme. Jestli se ale ta věc může hýbat...“
„Hejbat!“ skočil mi do řeči kapitán Goyles. „Když má Ferina za sebou ten správnej vítr...“
„A jaký je to vítr, ten správný?“ ptám se.
To kapitánu Goylesovi očividně zamotalo hlavu.
„V tomto týdnu,“ pravil jsem dále, „jsme už měli vítr od severu, od jihu, od východu, od západu - a to s
různými modifikacemi. Jestli víte o nějakém dalším bodu na větrné růžici, z něhož může vítr dout, řekněte mi o
něm, a já ještě počkám. Jestli o žádném nevíte a jestli vaše kotva už nezarostla do dna oceánu, tak ji dneska
zvedneme a uvidíme, co to bude dělat.“
Z toho vyrozuměl, že jsem odhodlán ke všemu.
„No prosím!“ řekl. „Vy jste pán, já jsem kmán. Mám, zaplať Pámbu, jenom jedno děcko, který ja na mě závislý,
a vykonavatelé vaší poslední vůle budou nepochybně vědět, jaký povinnosti je čekaj vůči mý starý.“
Jeho slavnostně vážný tón na mě zapůsobil.
„Pane Goylesi,“ řekl jsem, „jednejte se mnou na rovinu. Existuje nějaká naděje, nebo nějaké počasí, jež nám
umožní dostat se z tohohle pitomého hnízda?“
V kapitánu Goylesovi opět ožila ta jeho bodrá vlídnost.
„Víte, pane,“ povídá, „tohle je moc divný pobřeží. Jak už je člověk jednou venku na moři, tak je všechno v
pořádku, ale dostat se tam v takový skořápce, jak je tahle - víte, pane, upřímně řečeno, to je pěkná fuška.“
Rozloučili jsme se, když mě kapitán Goyles ujistil, že bude bdít nad počasím jako matka nad svým spícím
robátkem; to bylo jeho vlastní přirovnání a mě dost dojalo. Pak jsem ho zas viděl v poledne; bděl nad počasím za
oknem hospody „U řetězu a kotvy“.
Toho odpoledne v pět hodin se na mě usmálo štěstí; v prostředku hlavní třídy jsem potkal dva své kamarády,
plachtaře, kteří se v Harwichi museli zdržet, protože se jim polámalo kormidlo. Vyprávěl jsem jim, co mě potkalo,
a je to zjevně spíš pobavilo než překvapilo. kapitán Goyles a jeho lodníci pořád ještě bděli nad počasím. Běžel
jsem tedy ke „Králově hlavě“ a zburcoval jsem Ethelbertu. Pak jsme se všichni čtyři tiše přikradli na nábřeží a ke
své lodi. Na palubě byl jenom plavčík; moji dva přátelé se ujali velení a v šest hodin už jsme vesele klouzali podél
pobřeží k severu.
Tu noc jsme zakotvili v Aldoborough a příštího dne jsme dorazili do Yarmouthu. Tam se moji přátelé s námi
museli rozloučit, a tak jsme se orzhodl plachetnici opustit. Zásoby jsme hned zrána prodali na pláži v dražbě.
Prodělal jsem na tom, ale hřálo mě vědomí, že jsem doběhl kapitána Goylese. Ferinu jsem svěřil jednomu
místnímu námořníkovi, který ji za pár zlaťáků slíbil dopravit zpátky do Harwiche; a do Londýna jsme se vrátili
vlakem. Možná že všechny plachetnice nejsou jako Ferina a všichni kapitáni jako pan Goyles, ale já jsem po téhle
zkušenosti jak proti plachetnicím, tak proti jejich kapitánům zaujatý.
I George byl toho mínění, že plachetnice by znamenala spoustu odpovědnosti, a tak jsme tento nápad zavrhli.
„A co řeka?“ nadhodil Harris. „Na řece jsme zažili moc pěkné časy.“
Tři muži na toulkách
177
George mlčky zabafal ze svého doutníku a já rozlouskl další ořech.
„Řeka, to už není to, co to bývalo,“ řekl jsem. „Bůhví, co to v tom říčním vzduchu teď je - snad taková vlhkost
nějaká - že z toho vždycky dostanu hexnšús.“
„Já taky,“ pravil George. „Bůhví, čím to je, ale já si už nemůžu dovolit spát někde poblíž řeky. Na jaře jsem byl
týden u Joea a každou noc jsem se tam probudil už v sedm a pak jsem už nezavřel oka.“
„No to byl jen takový návrh,“ poznamenal Harris. „Mně osobně řeka taky nesvědčí; vždycky mi rozbouří to
moje revma.“
„Co mně dělá dobře,“ řekl jsem, „to jsou hory. Co byste říkali pěší tůře po Skotsku?“
„Ve Skotsku pořád prší,“ namítl George. „Já tam byl předloni tři neděle a v jednom kuse jsem byl zlitý - v tom
původním slova smyslu teda.“
„Moc hezky je ve Švýcarsku,“ podotkl Harris.
„Jenže do Švýcarska bychom nemohli jet sami, to by ženské nesnesly,“ namítl jsem. „Víš, jak to dopadlo
posledně. My musíme někam, kde by to delikátně vypiplané dámy nebo děti prostě nevydržely; někam, kde jsou
mizerné hotely a kde se cestuje nepohodlně; někam, kde nám to dá zabrat, kde se nadřeme a třeba budem mít
vysoko do žlabu...“
„Tak to pozor!“ přerušil mě George. „Pozor, kamaráde! Nezapomeň, že jedu taky já!“
„Já už to mám!“ zvolal Harris. „Vyjedeme si na kolech!“
George se zatvářil nerozhodně.
„To musíš každou chvíli do kopce,“ pravil. „A máš proti sobě vítr.“
„Ale pak zas jedeš s kopce a máš vítr v zádech,“ řekl Harris.
„To jsem nikdy nepozoroval,“ namítl George.
„Na nic lepšího, než je výlet na kolech, nepřijdeš,“ trval na svém Harris.
Já jsem s ním chtě nechtě musel souhlasit.
„A řeknu ti, kam si vyjedeme,“ dodal Harris. „Do Černého lesa.“
„No dovol! Tam je to pořád jenom do kopce,“ namítl George.
„Pořád ne,“ odvětil Harris. „Tak ze dvou třetin. Ale existuje jedna vymoženost, na kterou jsi zapomněl.“
Opatrně se rozhlédl a snížil hlas až do šepotu.
„Na ty kopce jezdí nahoru takové malinké vláčky, takové miniaturky s ozubenými kolečky, které...“
Vtom se otevřely dveře a objevila se paní Harrisová. Ethelberta si prý už nasazuje klobouk a Muriel po marném
čekání zarecitovala Bláznivou svačinu bez nás.
„Zítra ve čtyři v klubu,“ zašeptal mi Harris, když se zvedal, a já, když jsme šli nahoru, jsem to přihrál
Georgeovi.";
            }
        }
        /// <summary>
        /// Text "Tábor svatých"
        /// </summary>
        public static string Text1
        {
            get
            {
                return @"Starý profesor uvažoval všedně. Příliš mnoho četl, příliš mnoho přemýšlel, také příliš mnoho napsal na to, aby
se odvážil vyslovit dokonce i jen sám k sobě za okolností tak dokonale anormálních něco jiného, než banalitu
hodnou kompozice žáka ze sexty. Bylo krásně. Bylo horko, ale ne příliš, neboť čerstvý jarní vítr pomalu a bez
hluku přebíhal po kryté terase domu, jednoho z posledních směrem k vršku kopce, zavěšeného na úbočí skály jako
předsunutá stráž staré hnědé vesnice, která vévodila celé oblasti až k městu turistů dole, až k přepychové třídě na
břehu u vody, na niž se daly vytušit vrcholky zelených palem a bílé rezidence, až k samému moři, klidnému a
modrému, moři bohatých, z jehož povrchu byl náhle sloupnut lak opulence, který je obvykle pokrýval –
chromované jachty, svalnatí lyžaři, opálené dívky, těžká břicha rozvalená na palubě velkých obezřelých plachetnic
– nu a na tomto prázdném moři, neuvěřitelná rezavá flotila přibyvší z druhé strany zeměkoule, uvázlá padesát
metrů od břehu a kterou starý profesor od rána pozoroval. Příšerný puch latrín, který objevení této flotily
předcházel, jako hrom předchází bouři, se nyní úplně rozptýlil.
Oddaluje oko od dalekohledu na trojnožce, v němž se neuvěřitelná invaze hemžila tak blízko, že se zdálo, že
již přelezla svahy kopce a vrhla do domu, starý muž si protřel unavené víčko a poté zcela přirozeně obrátil pohled
ke dveřím svého domu. Byly to dveře z mohutného dubu, něco jako nesmrtelná hmota skloubená s veřejemi
pevnosti, v níž bylo vidět do tmavého dřeva vyryté rodové jméno starého pána a rok, který spatřil dostavění domu
předkem v přímé linii: 1673. Dveře spojovaly na téže úrovni terasu a hlavní místnost, která byla současně salónem,
knihovnou a pracovnou. Byly to jediné dveře v domě, neboť terasa vedla přímo do uličky malým schodištěm o
pěti schodech bez jakéhokoli plotu a po němž mohl každý kolemjdoucí po libosti vystoupit podle zvyku ve vesnici
panujícím, pokud dostal chuť pozdravit majitele. Každý den od úsvitu do noci zůstávaly tyto dveře otevřené a dnes
večer byly rovněž. Právě toho si starý muž všimnul poprvé. Tak pronesl těchto několik slov, jejichž úžasná
banálnost vyvolala na jeho rtech jakýsi okouzlený úsměv: „Sám se ptám, řekl si, zda v tomto případě, je nutno, aby
byly dveře otevřené nebo zavřené?…“
Poté se znovu ujal stráže, oko u dalekohledu využívaje toho, že zapadající slunce osvětlovalo naposledy před
příchodem noci neuvěřitelnou podívanou. Kolik jich tam bylo, na palubě všech těch uvázlých trosek? Pokud bylo
možno věřit děsivému počtu oznámenému v lakonických informačních sděleních, která dnes od rána následovala
jedno za druhým, možná byli napěchovaní po lidských vrstvách v podpalubí a na palubách, v chumlech tyčících se
až k můstkům a komínům, spodní mrtvé vrstvy nesoucí ty, které ještě žily, jako ony kolony mravenců na pochodu,
jejichž viditelná část je hemžením života a základna jakousi mravenčí cestou dlážděnou milióny mrtvol?
Starý profesor, jmenoval se Calgués – zaměřil dalekohled na jedno z plavidel nejlépe osvětlených sluncem.
Poté jej rozvážně zreguloval až k nejdokonalejší ostrosti jako badatel u svého mikroskopu, když v živné půdě objeví
kolonii mikrobů, jejíž existenci předvídal. Toto plavidlo byl parník více než šedesátník, jehož pět kolmých komínů
trubkové formy hlásalo velmi vysoký věk. Čtyři z nich byly v různé výši uříznuty časem, rzí, absencí údržby, ranami
osudu, jedním slovem bídou.
Uvázlá před pláží, ležela loď ve sklonu nějakých deseti stupňů. Jako na všech ostatních plavidlech této
strašidelné flotily, zatímco se stmívalo, nebylo vidět jediného světla ani nejmenšího záblesku.
Navigační světla, kotle, dynama, všechno muselo rázem zhasnout při záměrném ztroskotání, nebo možná pro
nedostatek topiva spočítaného co nejpřesněji na jednu a jedinou cestu, nebo také protože nikdo na palubě už
nepovažoval za nutné o cokoli se starat, exodus byv skočen u bran nového ráje.
Starý pán Calgués to vše pečlivě zaznamenával, detail po detailu, aniž by u sebe pozoroval sebemenší projev
emoce. Prostě, před avantgardou protisvěta, který se konečně odhodlal přijít osobně zaklepat na dveře hojnosti,
pociťoval nesmírný zájem.
Okem připoutaným k dalekohledu uviděl nejprve paže. Vypočítal, že kruh jím vykrojený na palubě lodi mohl
mít průměr kolem deseti metrů. Poté počítal dál, klidně, ale bylo to stejně obtížné, jako počítat stromy v lese.
Protože všechny tyto paže byly vztyčeny. Klátily se společně, nakláněly se k blízkému břehu, hubené černé a hnědé
větve, oživené větrem naděje. Paže byly nahé. Vynořovaly se z kusů bílého plátna, které měly být tunikami, tógami,
sarimi poutníků: byly to vychrtlé paže Gándhího. Dospěv k číslu dvou set, profesor přestal počítat, neboť dosáhl
hranic kruhu. Poté se pustil do rychlého výpočtu. Vezme-li se v úvahu délka a šířka paluby lodi, mohlo se stanovit,
že stejný obvod byl vedle sebe položen více než třicetkrát a že mezi každým z těchto třiceti bodově se dotýkajících
se kruhů se uložily dva prostory ve formě trojúhelníku stýkající se vrcholem a jejichž plocha se rovnala přibližně
třetině obvodu, tedy: 30 + 10 = 40 obvodů × 200 paží = 8 000 paží. Čtyři tisíce lidí. Na jediné palubě lodi!
Připustíme-li existenci vrstev překrývajících jedna druhou, nebo přinejmenším pravděpodobně tutéž hustotu na
každé palubě, mezipalubě a podpalubí, bylo třeba násobit nejméně osmi číslo už tak překvapující. Celkově: třicet
tisíc lidí na jediném plavidle! Aniž bychom počítali mrtvé, kteří plavali kolem boků lodi ve svých bílých cárech
táhnoucích se po povrchu vody, které živí hned zrána hodili přes palubu. Pro toto podivné gesto, které se nezdálo
být inspirováno hygienou – jinak, proč vyčkávat až konec cesty? - profesor nalezl, jak si myslel, jediné možné
vysvětlení. Calgués věřil v Boha. Věřil ve vše, v život věčný, ve vykoupení, v milosrdenství Boží, víru, naději. Věřil
také, a velmi pevně, že mrtvoly vyhozené na pobřeží Francie konečně dosáhly, i ony, ráje, že v něm dokonce
bloudily bez zábran a navždy, takto se těšící větší přízni než živí, kteří tím, že svoje mrtvé hodili do vody, jim naráz
dopřáli vysvobození, blaženost a věčnost. Toto gesto se nazývalo láska a profesor to chápal.
Nastala noc, aniž by den naposledy neosvítil rudými záblesky uvázlou flotilu. Bylo v ní více než sto plavidel,
všechna zrezivělá, nepoužitelná a všechna dosvědčující zázrak, který je vedl a chránil už z druhé strany světa, s
výjimkou jednoho, ztraceného ztroskotáním v blízkosti Ceylonu. Jedno za druhým, téměř způsobně seřazena podle
toho jak připlula, byla zapíchnuta ve skalách nebo v písku, s přídí obrácenou ke břehu a pozvednutou v posledním
vzepětí. Kolem dokola plavaly tisíce mrtvých v bílém, které poslední vlny dne začínaly pozvolna přinášet na
pevninu, pokládajíce je na břeh a poté opadávaly, aby odešly pro další. Sto plavidel! Starý profesor v sobě cítil zrod
jakéhosi záchvěvu pokory smíšené s vytržením, které člověk občas pocítí, když svoji mysl velmi silně zaměstnává
pojmy nekonečna nebo věčnosti. Navečer této neděle Velikonoční obléhalo osm set tisíc živých a tisíce mrtvých
mírumilovně hranici Západu. Nazítří bude po všem. Od břehu stoupaly až do kopců, k vesnici, až k terase domu
na poslech velmi příjemné zpěvy, ale navzdory jejich mírnosti, plné nesmírné síly, jako melopej
(říkanka)prozpěvovaná sborem osmi set tisíců hlasů. Křižáci kdysi v předvečer závěrečného útoku obcházeli
zpívajíce Jeruzalém. Po sedmém zaznění trub se bez boje zhroutily hradby Jericha. A když by melopej uprázdnila
místo tichu, budou snad vyvolené národy zase podrobeny nepřízni boží? Bylo rovněž slyšet burácení stovek
nákladních aut: od rána také armáda zaujímala pozice na břehu Středozemního moře. V nastavší noci se terasa
otevírala jen k nebi a ke hvězdám.
V domě bylo chladno, ale vcházeje, rozhodl se profesor nechat dveře otevřené. Copak dveře, byť zázrak
třistaleté řemeslné práce v západní nanejvýš úctyhodné dubovině, mohou ochránit svět, který se již přežil? Elektřina
nefungovala. Nepochybně také i techničtí pracovníci elektráren v pobřežní oblasti uprchli na sever, následujíce
veškerý zděšený lid, který se obracel zády a vytrácel v tichosti, aby neviděl, aby nic neviděl a tím také nic nechápal,
nebo přesněji nic chápat nechtěl.
Profesor zažehl petrolejové lampy, které měl pro případ poruchy vždy připravené a vhodil zápalku do krbu,
v němž pečlivě nachystaný oheň ihned vzplál, zahučel, zapraskal, šíře teplo a světlo. Poté zapnul tranzistor. Pop
muzika, rock, zpěvačky, plytcí žvanilové, negerští saxofonisté, guruové, sebevědomé hvězdy, moderátoři, poradci
ohledně zdraví, srdečních záležitostí a sexu, ti všichni opustili éter, náhle považováni za nevkusné, jakoby si
ohrožený Západ obzvláště hleděl svého posledního zvukového obrazu. Bylo slyšet Mozarta, stejný program na
všech stanicích: „Malá noční hudba“, docela prostě.
Starý profesor přátelsky pomyslel na programového pracovníka v pařížském studiu. Aniž by věděl či viděl,
tento muž pochopil. Na melopej osmi set tisíc hlasů, kterou zatím nemohl slyšet, našel instinktivně nejlepší
odpověď. Co na světě bylo západnější, civilizovanější, dokonalejší než Mozart? Je nemožné pobrukovat Mozarta
osmi sty tisíci hlasy. Mozart nikdy neskládal pro podněcování davů, ale aby bylo dojato srdce každého v jeho
osobnosti. Západ ve svojí jediné pravdě… Hlas zpravodaje vytrhl profesora z úvah:
„Vláda shromážděná kolem prezidenta republiky zasedala celý den v Elysejském paláci.";
            }
        }
        #endregion
        #region Generátor náhodné pravděpodobnosti, čísla, barvy...
        /// <summary>
        /// Vrátí true s danou pravděpodobností:
        /// 0 = nikdy není true; 100 = vždy je true; mezi tím: 10 = vrátí true v 10 případech ze 100 volání
        /// </summary>
        /// <param name="probability"></param>
        public static bool IsTrue(int probability = 50)
        {
            probability = _ToRange(probability, 0, 100);
            int value = Rand.Next(0, 100);                 // číslo 0-99
            return (value < probability);                  // Pokud je probability = 0, pak value nikdy není < 0, vždy vrátím false. Pokud probability = 100, pak value je vždy < 100. ....
        }
        /// <summary>
        /// Vrátí náhodnou barvu, volitelně v daném rozmezí 0 až 256, volitelně s náhodnou hodntou Alpha
        /// </summary>
        /// <param name="low"></param>
        /// <param name="high"></param>
        /// <param name="alpha">Hodnota Alpha</param>
        /// <param name="isRandomAlpha">Použít náhodný Alpha kanál v rozmezí 16 - 240? false = ne, Alpha bude 255</param>
        /// <returns></returns>
        public static Color GetColor(int low = 0, int high = 256, int? alpha = null, bool isRandomAlpha = false)
        {
            low = _ToRange(low, 0, 255);
            high = _ToRange(high, low, 256);
            var rand = Rand;
            int a = (alpha.HasValue ? _ToRange(alpha.Value, 0, 256) : (isRandomAlpha ? rand.Next(16, 240) : 255));
            int r = rand.Next(low, high);
            int g = rand.Next(low, high);
            int b = rand.Next(low, high);
            return Color.FromArgb(a, r, g, b);
        }
        /// <summary>
        /// Vrátí náhodný prvek z pole
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public static T GetItem<T>(params T[] items)
        {
            return GetItem((IList<T>)items);
        }
        /// <summary>
        /// Vrátí náhodný prvek z pole
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public static T GetItem<T>(IList<T> items)
        {
            if (items == null) return default(T);
            int count = items.Count;
            if (count == 0) return default(T);
            return items[Rand.Next(count)];
        }
        /// <summary>
        /// Vrátí daný počet náhodně vybraných prvků z pole
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="count"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static T[] GetItems<T>(int count, params T[] items)
        {
            return GetItems(count, (IList<T>)items);
        }
        /// <summary>
        /// Vrátí náhodný prvek z pole
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public static T[] GetItems<T>(int count, IList<T> items)
        {
            if (items == null) return null;
            int itemsCount = items.Count;
            List<T> result = new List<T>();
            if (count > 0)
            {   // Něco chtěli?
                if (count < itemsCount)
                {   // Chtěli méně prvků, než nám dali: vybereme si náhodně daný počet:
                    List<T> values = items.ToList();
                    for (int i = 0; i < count; i++)
                    {
                        if (values.Count == 0) break;                // Pojistka
                        int index = Rand.Next(values.Count);         // Náhodná pozice prvku ve zmenšujícím se Listu hodnot
                        result.Add(values[index]);                   // Do výsledku přidám prvek na náhodné pozici
                        values.RemoveAt(index);                      // A tentýž prvek z Listu odeberu, abych ho do výsledku nedával duplicitně...
                    }
                }
                else
                {   // Chtěli by víc prvků, než nám dali: vrátíme jen to co máme:
                    result.AddRange(items);
                }
            }
            return result.ToArray();
        }
        /// <summary>
        /// Vrátí danou hodnotu zarovnanou do min - max, obě meze jsou včetně
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private static int _ToRange(int value, int min, int max)
        {
            return (value < min ? min : (value > max ? max : value));
        }
        #endregion
        #region Náhoda
        /// <summary>
        /// Random generátor
        /// </summary>
        public static System.Random Rand { get { if (_Rand is null) _Rand = new System.Random(); return _Rand; } }
        private static System.Random _Rand;
        #endregion
    }
}
