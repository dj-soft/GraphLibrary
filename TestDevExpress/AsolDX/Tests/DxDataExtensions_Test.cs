using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noris.Clients.Win.Components.Tests
{
    using Noris.Clients.Win.Components.AsolDX;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Testy
    /// </summary>
    [TestClass]
    public class DataExtensionsTest
    {
        /// <summary>
        /// Test Align()
        /// </summary>
        [TestMethod]
        public void TestAlign()
        {
            // Int32
            TestAlignType(10, 20, new int[] { 5, 10, 15, 20, 25 }, new int[] { 10, 10, 15, 20, 20 });

            // Decimal
            TestAlignType(10m, 20m, new decimal[] { 5m, 9.999m, 10m, 10.001m, 15m, 19.999m, 20m, 20.001m, 25m }, new decimal[] { 10m, 10m, 10m, 10.001m, 15m, 19.999m, 20m, 20m, 20m });

            // DateTime
            TestAlignType(new DateTime(2021, 7, 1), new DateTime(2021, 7, 31),
                new DateTime[] { new DateTime(2021, 6, 1), new DateTime(2021, 7, 1), new DateTime(2021, 7, 15), new DateTime(2021, 7, 31), new DateTime(2021, 8, 10) },
                new DateTime[] { new DateTime(2021, 7, 1), new DateTime(2021, 7, 1), new DateTime(2021, 7, 15), new DateTime(2021, 7, 31), new DateTime(2021, 7, 31) });

            // String
            TestAlignType("E", "I", new string[] { "A", "Dyson", "E", "Epsilon", "G", "I", "Idea", "K" }, new string[] { "E", "E", "E", "Epsilon", "G", "I", "I", "I" });
        }
        /// <summary>
        /// Provede test Align() pro dané rozmezí Min-Max, pro dané hodnoty a pro očekávané výsledky
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="values"></param>
        /// <param name="results"></param>
        private void TestAlignType<T>(T min, T max, T[] values, T[] results) where T : IComparable<T>
        {
            if (values.Length != results.Length) Assert.Fail("values.Length != results.Length");

            for (int i = 0; i < values.Length; i++)
            {
                T value = values[i];
                T result = results[i];
                T align;

                align = value.Align(min, max);
                Assert.AreEqual(align, result);

                align = value.Align(min, min);
                Assert.AreEqual(align, min);

                align = value.Align(max, min);
                Assert.AreEqual(align, max);
            }
        }
    }

    /// <summary>
    /// Testy
    /// </summary>
    [TestClass]
    public class CompressorTest
    {
        /// <summary>
        /// Test komprimace 
        /// </summary>
        [TestMethod]
        public void TestCompress()
        {
            _Result = new StringBuilder();
            _Stopwatch = new System.Diagnostics.Stopwatch();
            _Frequency = (decimal)System.Diagnostics.Stopwatch.Frequency;
            _Stopwatch.Start();
            _FormatCsv = true;

            // První spuštění je na načtení a rozjezd DLL, ale jeho výsledky zahazuji:
            _TestCompressText(_GetText(1), "Kratší anglický text");
            _Result.Clear();

            _TestCompressAddTitle();
            _TestCompressText(_GetText(1), "Kratší anglický text");
            _TestCompressText(_GetText(2), "Dlouhý HTML kód");
            _TestCompressText(_GetText(3), "Delší český text");
            _TestCompressText(_GetText(4), "Delší anglický text");
            _TestCompressText(_GetText(5), "Dost dlouhý anglický text");

            string compressInfo = _Result.ToString();
        }
        private StringBuilder _Result;
        private System.Diagnostics.Stopwatch _Stopwatch;
        private decimal _Frequency;
        private bool _FormatCsv;
        /// <summary>
        /// Test komprimace dodaného textu
        /// </summary>
        /// <param name="input">Text ke komprimaci</param>
        /// <param name="info">Popis formátu textu do resultu</param>
        private void _TestCompressText(string input, string info)
        {
            _TestCompressOne(input, Noris.Clients.Win.Components.AsolDX.CompressionMode.ZipStreamOptimal, info);
            _TestCompressOne(input, Noris.Clients.Win.Components.AsolDX.CompressionMode.ZipStreamFast, info);
            _TestCompressOne(input, Noris.Clients.Win.Components.AsolDX.CompressionMode.DeflateOptimal, info);
            _TestCompressOne(input, Noris.Clients.Win.Components.AsolDX.CompressionMode.DeflateFast, info);
        }
        /// <summary>
        /// Test komprimace dodaného textu
        /// </summary>
        /// <param name="input">Text ke komprimaci</param>
        /// <param name="mode">Režim</param>
        /// <param name="info">Popis formátu textu do resultu</param>
        private void _TestCompressOne(string input, Noris.Clients.Win.Components.AsolDX.CompressionMode mode, string info)
        {
            var data = Encoding.UTF8.GetBytes(input);
            long t0 = _Stopwatch.ElapsedTicks;
            var zip = Noris.Clients.Win.Components.AsolDX.Compressor.Compress(data, mode);
            long t1 = _Stopwatch.ElapsedTicks;
            var unzip = Noris.Clients.Win.Components.AsolDX.Compressor.DeCompress(zip);
            long t2 = _Stopwatch.ElapsedTicks;
            var output = Encoding.UTF8.GetString(unzip);

            // string signature = zip.Take(16).Select(b => b.ToString("X2")).ToOneString("-");

            if (input != output)
                Assert.Fail("Unzip != Zip");

            // Result:
            string ratio = $"{Math.Round((100d * ((double)zip.Length) / ((double)data.Length)), 2).ToString()}" + (_FormatCsv ? "%" : "");
            string timeZip = _GetTime(t1 - t0);
            string timeUnZip = _GetTime(t2 - t1);

            if (_FormatCsv)
                _AddLine($"{info}\t{input.Length}\t{mode}\t{ratio}\t{timeZip}\t{timeUnZip}");
            else
                _AddLine($"{info} : Length = {input.Length};  Mode = {mode};  Ratio = {ratio};  Compress time = {timeZip}; DeCompress time = {timeUnZip};");
        }
        private void _TestCompressAddTitle()
        {
            if (_FormatCsv)
            {
                _AddLine($"INFO\tLength\tMode\tRatio\tCompress time\tDeCompress time");
                _AddLine($"\tByte\t\t%\tmilisec\tmilisec");
            }
        }
        private void _AddLine(string line) { _Result.AppendLine(line); }
        private string _GetTime(long ticks)
        {
            string milisecs = Math.Round((1000m * (decimal)ticks / _Frequency), 3).ToString();
            return (_FormatCsv ? milisecs.Replace(".", ",") : milisecs + " milisec");
        }
        private string _GetText(int sample)
        {
            switch (sample)
            {
                case 1:
                    return @"SQL Server APPLY operator has two variants; CROSS APPLY and OUTER APPLY

    The CROSS APPLY operator returns only those rows from the left table expression (in its final output) if it matches with the right table expression. In other words, the right table expression returns rows for the left table expression match only.
    The OUTER APPLY operator returns all the rows from the left table expression irrespective of its match with the right table expression. For those rows for which there are no corresponding matches in the right table expression, it contains NULL values in columns of the right table expression.
    So you might conclude, the CROSS APPLY is equivalent to an INNER JOIN (or to be more precise its like a CROSS JOIN with a correlated sub-query) with an implicit join condition of 1=1 whereas the OUTER APPLY is equivalent to a LEFT OUTER JOIN.

You might be wondering if the same can be achieved with a regular JOIN clause, so why and when do you use the APPLY operator? Although the same can be achieved with a normal JOIN, the need of APPLY arises if you have a table-valued expression on the right part and in some cases the use of the APPLY operator boosts performance of your query. Let me explain with some examples.";
                case 2:
                    return @"<!DOCTYPE html>
<html>
<head>
    <meta http-equiv=‼X-UA-Compatible‼ content=‼IE=edge‼>
    <meta name=‼viewport‼ content=‼width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no‼ />
    <link rel=‼shortcut icon‼ href=‼/Styles/images/grapp16.ico‼ type=‼image/x-icon‼>
    <title>GRAPP - Grafick&#225; prezentace polohy</title>
    <link rel=‼stylesheet‼ type=‼text/css‼ href=‼/DXR.axd?r=1_75,1_69,1_70,1_71,1_74,1_251,1_248,1_250,1_247,1_293,1_292,1_109,0_2,0_8,0_12,0_20,0_24,0_28,0_16,0_32-jy2Zi‼ />
    <link href=‼/Content/Styles?v=BipFQTfD1N-6bUZ3nhlMNjg58RLfaYr4BjKyPSwdPBs1‼ rel=‼stylesheet‼/>

    <script src=‼/Content/Scripts?v=6eUjY_TtuOlFi-Ohs1ZB2cguztCvpGOUPRd19ryZniQ1‼></script>

    <script type=‼text/javascript‼>
        var grappUri = '';
        var Resources = {
            LocationNoFound: 'Prohlížeč nedokázal najít Vaši polohu.',
            RestrictionTooltip: 'Zobrazit detail omezení provozu',
            RestrictionNoPlannedTooltip: 'Zobrazit detail mimořádnosti v provozu',
            MapControlRestrictionEnable: 'Zobrazit mimořádnosti v provozu',
            MapControlRestrictionDisable: 'Nezobrazovat mimořádnosti v provozu',
            MapControlPlannedRestrictionEnable: 'Zobrazit omezení provozu',
            MapControlPlannedRestrictionDisable: 'Nezobrazovat omezení provozu',
            MapControlFullscreen: 'Režim celé obrazovky',
            MapControlMyPosition: 'Moje poloha'
        };
        var postback=0;
    </script>
    <script id=‼dxis_1071636073‼ src=‼/DXR.axd?r=1_16,1_66,1_17,1_18,1_19,1_20,1_21,1_25,1_68,1_51,1_22,1_14,17_8,17_15,1_32,1_42,1_34,17_42,1_28,1_58,17_41,1_44,1_57,1_56,17_40,1_225,1_226,1_29,1_36,1_49,1_254,1_252,1_280,1_50,1_55,17_14,1_54,17_22,1_26,1_27,1_43,1_37,1_24,1_265,1_266,1_253,1_259,1_257,1_260,1_261,1_258,1_262,1_255,1_263,1_264,1_268,1_276,1_278,1_279,1_267,1_271,1_272,1_273,1_256,1_269,1_270,1_274,1_275,1_277,17_1,17_10,1_62,1_60,17_44,1_59,17_45,1_61,17_46,17_47,1_63,17_11,1_52,17_16,17_17,1_38,17_5,1_65,17_19,1_53,1_41,17_3,1_46,17_20,17_21,1_231,1_228,1_234,17_35,17_29-ly2Zi‼ type=‼text/javascript‼></script>
</head>
<body>
    <div class=‼container-fluid‼>
        <div id=‼divWait‼>
            <div id=‼pimgWait‼>
                <img src=‼/Styles/images/ajax-loader1.gif‼ />
            </div>
        </div>
        <div id=‼OGCookieModal‼ class=‼modal fade CPPControl‼ role=‼dialog‼ data-keyboard=‼false‼ data-backdrop=‼static‼>
            <div class=‼modal-dialog‼>
                <div class=‼modal-content‼>
                    <div class=‼modal-header‼>
                        <div class=‼container-fluid‼>
                            <div class=‼row‼>
                                <div class=‼col-xs-12 col-sm-12 col-md-12 col-lg-12‼>
                                    Nastaven&#237; Cookies<a id=‼OGBtnCookieClose‼ href=‼#‼ class=‼CookieBtnClose‼ onclick=‼return CookieBtnClose();‼>×</a>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class=‼modal-body‼>
                        <div class=‼container-fluid‼>
                            <div class=‼row‼>
                                <div class=‼col-xs-12 col-sm-12 col-md-12 col-lg-12‼>
                                    Na těchto webových stránkách používáme technické cookies pro jejich správné fungování. Kliknutím na <b>Povolit vše</b> vyjadřujete souhlas s použitím všech cookies. Tento souhlas můžete vzít kdykoliv zpět a nastavení upravit v patičce webové stránky pod odkazem <b>Cookies</b>. Podrobnější informace najdete v 
                                    <a href=‼https://www.spravazeleznic.cz/cookies#noCookies‼ class=‼link‼ target=‼_blank‼>Prohl&#225;šen&#237; o použ&#237;v&#225;n&#237; cookies</a>.
                                </div>
                            </div>
                            <div class=‼row‼ id=‼OGCookieChB‼ style=‼display: none‼>
                                <div class=‼col-xs-12 col-sm-12 col-md-12 col-lg-12‼>
                                    <br />
                                    <div>
                                        <input type=‼checkbox‼ id=‼OGCookieTechnical‼ name=‼OGCookieTechnical‼ class=‼checkbox-cookie‼ checked disabled />
                                        <label class=‼checkbox-cookie-label‼ style=‼opacity: 0.6‼ for=‼OGCookieTechnical‼>
                                            Technick&#233; (nezbytn&#233;) cookies – jsou nezbytn&#233; pro spr&#225;vn&#233; fungov&#225;n&#237; internetov&#253;ch str&#225;nek. Umožňuj&#237; z&#225;kladn&#237; funkce internetov&#253;ch str&#225;nek.
                                        </label>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class=‼modal-footer‼>
                        <div class=‼container-fluid‼>
                            <div class=‼row‼>
                                <div class=‼col-xs-6 col-sm-6 col-md-6 col-lg-6‼>
                                    <button id=‼OGBtnCookieSelect‼ class=‼btn btnLeft‼ onclick=‼return CookieBtnSelect();‼>Povolit vybran&#233;</button>
                                </div>
                                <div class=‼col-xs-6 col-sm-6 col-md-6 col-lg-6 text-right‼>
                                    <button id=‼OGBtnCookieAcceptAll‼ class=‼btn btnRight‼ onclick=‼return CookieBtnAcceptAll();‼>Povolit vše</button>
                                    <button id=‼OGBtnCookieAcceptSel‼ class=‼btn btnRight‼ style=‼display: none;‼ onclick=‼return CookieBtnAcceptSel();‼>Povolit vybran&#233;</button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div id=‼loginModal‼ class=‼modal fade‼ role=‼dialog‼>
            <div class=‼modal-dialog‼>
                <div class=‼modal-content‼>
                    <div class=‼modal-header‼>
                        <h1>Přihl&#225;šen&#237;<button type=‼button‼ class=‼close‼ data-dismiss=‼modal‼><span aria-hidden=‼true‼>&times;</span></button></h1>
                    </div>
                    <div class=‼modal-body‼>
                        <div class=‼container-fluid‼>
<form action=‼/Main/Login‼ method=‼post‼ name=‼formAccount‼>                                <div class=‼row‼>
                                    <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12‼>
                                        <label for=‼txtName‼>
                                        </label>
                                        <input id=‼txtName‼ name=‼txtName‼ type=‼text‼ class=‼form-control‼ placeholder=‼Jm&#233;no‼ autofocus />
                                    </div>
                                </div>
                                <div class=‼row‼>
                                    <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12‼>
                                        <label for=‼txtPassword‼>
                                        </label>
                                        <input id=‼txtPassword‼ name=‼txtPassword‼ type=‼password‼ class=‼form-control‼ placeholder=‼Heslo‼ />
                                    </div>
                                </div>
                                <div class=‼row‼>
                                    <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12‼>
                                        <button type=‼submit‼ class=‼btn‼>Přihl&#225;sit<svg class=‼svgArrrowStation‼ xmlns=‼http://www.w3.org/2000/svg‼ viewBox=‼0 0 250 250‼><polygon fill=‼#ffffff‼ points=‼35 115.49 184.84 115.49 103.23 34.61 125.85 34.61 215 123.64 125.85 215.46 103.23 215.46 185.93 130.94 35 130.94 35 115.49‼ /></svg></button>
                                    </div>
                                </div>
</form>                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div id=‼modalBasic‼ class=‼modal fade‼ role=‼dialog‼>
            <div class=‼modal-dialog modalLoading‼>
                <div class=‼modal-content‼>
                    <div class=‼modal-header‼>
                        <h1>
                            Nač&#237;t&#225; se
                        </h1>
                    </div>
                    <div class=‼modal-body‼>
                        <label>Čekejte pros&#237;m</label>
                    </div>
                </div>
            </div>
        </div>
<div id=‼aboutModal‼ class=‼modal fade‼ role=‼dialog‼>
    <div class=‼modal-dialog‼>
        <div class=‼modal-content‼>
            <div class=‼modal-header‼>
                <h1>
                    O n&#225;s
                    <button type=‼button‼ class=‼close‼ data-dismiss=‼modal‼>&times;</button>
                </h1>
            </div>
            <div class=‼modal-body‼>
                <div class=‼container-fluid‼>
                    <div class=‼content‼>
                        <div class=‼row‼>
                            <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12‼>
                                Vlastn&#237;k: Spr&#225;va železnic, s.o.
                            </div>
                        </div>
                        <div class=‼row‼>
                            <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12‼>
                                Provozovatel: OLTIS Group, a.s.
                            </div>
                        </div>
                        <div class=‼row‼>
                            <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12 borderBottom‼>
                            </div>
                        </div>
                        <div class=‼row‼>
                            <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12‼>
                                podpora +420 702 001 199 (8-16h)
                            </div>
                        </div>
                        <div class=‼row‼>
                            <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12‼>
                                verze: 4.8.0
                            </div>
                        </div>
                        <div class=‼row‼>
                            <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12 borderBottom‼>
                            </div>
                        </div>
                        <div class=‼row‼>
                            <div class=‼col-lg-4 col-md-4 col-sm-4 col-xs-6‼ style=‼min-width:250px‼>
                                <img src=‼/Styles/images/LogoSZDC.svg‼ class=‼szdcLogo‼ />                                
                            </div>
                            <div class=‼col-lg-4 col-md-4 col-sm-4 hidden-xs‼>
                            </div>
                            <div class=‼col-lg-4 col-md-4 col-sm-4 col-xs-6‼ style=‼min-width:250px‼>
                                <img src=‼/Styles/images/oltisLogo.png‼ class=‼oltisLogo‼ />
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>


        <div class=‼headerrow‼>
            <div class=‼row‼>
                <div class=‼col-lg-6 col-md-6 col-sm-10 col-xs-10‼>
                    <img src=‼/Styles/images/LogoGRAPP.svg‼ class=‼headerLogo‼ style=‼max-width:170px‼ />
                </div>
                <div class=‼col-lg-6 col-md-6 hidden-sm hidden-xs‼>
                    <a href=‼https://www.spravazeleznic.cz‼ target=‼_blank‼>
                        <img src=‼/Styles/images/LogoSZDCHeader.svg‼ class=‼headerLogo pull-right‼ style=‼max-width:140px‼ />
                    </a>
                </div>
                <div class=‼hidden-lg hidden-md col-sm-2 col-xs-2‼>
                    <div class=‼btn-group hamburger_menu pull-right‼>
    <button type=‼button‼ class=‼btn dropdown-toggle‼ data-toggle=‼dropdown‼ aria-haspopup=‼true‼ aria-expanded=‼false‼>
        <span class=‼glyphicon glyphicon-menu-hamburger‼ aria-hidden=‼true‼></span>
    </button>
    <ul class=‼dropdown-menu dropdown-menu-right‼>
        <li><a onclick=‼FilterHide(); $('#collapseThree').collapse('show');‼ class=‼btn‼>Vyhled&#225;v&#225;n&#237;</a></li>
        <li><a onclick=‼FilterHide(); $('#collapseOne').collapse('show');‼ class=‼btn‼>Filtr dopravců</a></li>
        <li><a onclick=‼FilterHide(); $('#collapseTwo').collapse('show');‼ class=‼btn‼>Ostatn&#237; filtry</a></li>
        <li>
            <a class=‼btn‼ data-toggle=‼modal‼ data-target=‼#aboutModal‼>O n&#225;s</a>
        </li>
        <li>
                <a class=‼btn‼ data-toggle=‼modal‼ data-target=‼#loginModal‼>Přihl&#225;sit</a>
        </li>
        <li>
            <a href=‼/Main/ChangeCulture?lang=cs‼ class=‼mobileLanguages‼><img src=‼/Styles/images/Language/cs.png‼></a>
            <a href=‼/Main/ChangeCulture?lang=en‼ class=‼mobileLanguages‼><img src=‼/Styles/images/Language/en.png‼></a>
            <a href=‼/Main/ChangeCulture?lang=de‼ class=‼mobileLanguages‼><img src=‼/Styles/images/Language/de.png‼></a>
            <a href=‼/Main/ChangeCulture?lang=pl‼ class=‼mobileLanguages‼><img src=‼/Styles/images/Language/pl.png‼></a>
        </li>
    </ul>
</div>
                </div>
            </div>
            <div class=‼hide-qm hideArrow hidden-xs hidden-sm‼>
                <img class=‼svgMenu‼ id=‼headerUp‼ src=‼/Styles/images/icons-svg/ikona-sipka-nahoru.svg‼ onclick=‼JQHide('.headersubrow'); JQHide('#headerUp'); JQShow('#headerDown');‼ />
                <img class=‼svgMenu‼ id=‼headerDown‼ src=‼/Styles/images/icons-svg/ikona-sipka-dolu.svg‼ style=‼display:none‼ onclick=‼JQShow('.headersubrow'); JQShow('#headerUp'); JQHide('#headerDown');‼ />
            </div>
            <div class=‼headersubrow hidden-xs hidden-sm‼>
                <div class=‼row‼>
                    <div class=‼col-lg-4 col-md-5‼>
                        <div class=‼infoVersion‼>
                            verze: 4.8.0
                            <div class=‼rightPipe‼>&nbsp;</div>
                            podpora +420 702 001 199 (8-16h)
                        </div>
                    </div>

                    <div class=‼col-lg-6 col-md-4‼>
                        <div class=‼Marquee‼>
                            <div class=‼quickMessage btnPad‼>
                            </div>
                        </div>
                    </div>
                    <div class=‼col-lg-2 col-md-3‼>
                        <div id=‼divBtnPad‼ class=‼btnPad pull-right‼>
<div class=‼lang btn-group float_right‼>   
    <div type=‼button‼ class=‼btn dropdown-toggle‼ data-toggle=‼dropdown‼ onmouseover=‼spanLang.style.color = svgLang.style.fill = '#002b59';spanLang.style.fontWeight = 'bold'‼ onmouseout=‼spanLang.style.color = svgLang.style.fill = '#00a1e0'; spanLang.style.fontWeight = 'normal'‼ aria-haspopup=‼true‼ aria-expanded=‼false‼>
        <span id=‼spanLang‼ class=‼actualLanguage‼>
            Česky
        </span>
        <svg class=‼svgMenu‼ xmlns=‼http://www.w3.org/2000/svg‼ viewBox=‼0 0 250 250‼ onmouseover=‼spanLang.style.color = '#002b59'; spanLang.style.fontWeight = 'bold'‼ onmouseout=‼spanLang.style.color = '#00a1e0'; spanLang.style.fontWeight = 'normal'‼>                        
            <polygon id=‼svgLang‼ fill=‼#00a1e0‼ points=‼198 88.5 125 161.5 52 88.5 198 88.5‼ transform=‼translate(0 -60)‼ />
        </svg>        
    </div>
    <ul class=‼dropdown-menu‼>
        <li><a href=‼/Main/ChangeCulture?lang=cs‼>Česky</a></li>
        <li><a href=‼/Main/ChangeCulture?lang=en‼>English</a></li>
        <li><a href=‼/Main/ChangeCulture?lang=de‼>Deutsch</a></li>
        <li><a href=‼/Main/ChangeCulture?lang=pl‼>Polski</a></li>
    </ul>
</div>
                            <span class=‼rightPipe‼></span>
                            <a href=‼https://provoz.spravazeleznic.cz/Portal/Help.aspx?hid=1&amp;appId=18‼ target=‼_blank‼ class=‼help_button‼>
                                <svg class=‼svgMenu‼ xmlns=‼http://www.w3.org/2000/svg‼ viewBox=‼0 0 250 250‼>
                                    <path id=‼svgHelp‼ fill=‼#00a1e0‼ d=‼M229.09,30.88a11.11,11.11,0,0,0-6.21-5.74,12.33,12.33,0,0,0-4.54-.84H31.58a12,12,0,0,0-4.49.84,11.2,11.2,0,0,0-3.69,2.31,11.62,11.62,0,0,0-2.48,3.43A9.74,9.74,0,0,0,20,35.06V150.41a9.67,9.67,0,0,0,.92,4.17A11,11,0,0,0,23.4,158a11.3,11.3,0,0,0,3.69,2.28,12,12,0,0,0,4.49.82h93.34l64.7,64.63V161.07h28.72a12.31,12.31,0,0,0,4.54-.82,11.27,11.27,0,0,0,3.73-2.28,10.64,10.64,0,0,0,2.48-3.39,9.52,9.52,0,0,0,.91-4.17V35.06A9.59,9.59,0,0,0,229.09,30.88ZM132.09,131H110.82V117.12h21.27Zm18.72-49.85a21.07,21.07,0,0,1-4.41,7.09,31.72,31.72,0,0,1-6.8,5.38,60.65,60.65,0,0,1-9,4.31v12H112.08V92.19c2.5-.66,4.75-1.35,6.77-2a25.39,25.39,0,0,0,6.34-3.43,16.69,16.69,0,0,0,4.87-4.9,11.65,11.65,0,0,0,1.77-6.33c0-3.51-1.14-6-3.4-7.5s-5.46-2.25-9.55-2.25a27.09,27.09,0,0,0-8.57,1.63,39,39,0,0,0-8.7,4.22H99.5V55.51a64.3,64.3,0,0,1,10.36-3A65.9,65.9,0,0,1,124.08,51q13,0,20.65,5.74a18,18,0,0,1,7.64,15A26.52,26.52,0,0,1,150.81,81.16Z‼ />
                                </svg>
                            </a>
                            <span class=‼rightPipe‼></span>
                            
<div class=‼account‼>
        <div class=‼login‼>
            <div type=‼button‼ class=‼btn‼ data-toggle=‼modal‼ data-target=‼#loginModal‼>
                <svg class=‼svgMenuLogin‼ xmlns=‼http://www.w3.org/2000/svg‼ viewBox=‼0 0 250 250‼><path id=‼svgLogin‼ fill=‼#00a1e0‼ d=‼M192,198.87c-.11-1.74-.27-3.81-.45-6.25s-.44-5-.72-7.8-.59-5.62-.92-8.56-.69-5.76-1-8.51-.75-5.31-1.14-7.71-.78-4.43-1.17-6.12a73.3,73.3,0,0,0-2.09-7.89,32.78,32.78,0,0,0-2.86-6.32,21.42,21.42,0,0,0-4.1-5,26.85,26.85,0,0,0-5.95-3.94c-2.19-1.06-4.12-2-5.79-2.68s-3.17-1.32-4.48-1.77a29.28,29.28,0,0,0-3.53-1,16.65,16.65,0,0,0-2.81-.29,8.69,8.69,0,0,0-4.66,1.14,30.94,30.94,0,0,0-3.23,2.31l-.75.67q-10.81,9.23-21.39,9.21-10.3,0-21.29-9.21l-.67-.67a30.94,30.94,0,0,0-3.23-2.31,8.69,8.69,0,0,0-4.66-1.14,16.65,16.65,0,0,0-2.81.29,25.13,25.13,0,0,0-3.56,1c-1.34.47-2.85,1.08-4.52,1.79s-3.61,1.63-5.8,2.69a24.11,24.11,0,0,0-5.9,3.86,22.07,22.07,0,0,0-4.07,5A32.78,32.78,0,0,0,65.54,146q-1.17,3.54-2.17,7.89-.59,2.6-1.17,6.17c-.39,2.37-.78,4.94-1.14,7.71s-.7,5.62-1,8.55-.61,5.78-.89,8.51-.51,5.33-.7,7.76-.36,4.51-.47,6.25ZM125.21,118.2c.75-.06,1.53-.14,2.31-.25a27.52,27.52,0,0,0,10.18-4,28,28,0,0,0,7.93-7.79,34.68,34.68,0,0,0,4.61-9.45,35.15,35.15,0,0,0,1.59-10.6c0-1.12,0-2.21-.13-3.27a25.88,25.88,0,0,0-.45-3.27,36.77,36.77,0,0,0-3.95-11.32,34.28,34.28,0,0,0-7-9,30.72,30.72,0,0,0-9.21-5.87,28.27,28.27,0,0,0-10.74-2.1c-.78,0-1.55,0-2.31,0a14.29,14.29,0,0,0-2.3.28,26.76,26.76,0,0,0-10.19,4,29.06,29.06,0,0,0-8,7.79A34.68,34.68,0,0,0,93,72.8a35.15,35.15,0,0,0-1.59,10.6c0,1.06,0,2.12.12,3.18A28.08,28.08,0,0,0,92,89.86a36.4,36.4,0,0,0,4,11.4,35.54,35.54,0,0,0,7,9,31.33,31.33,0,0,0,9.22,5.9,27.44,27.44,0,0,0,10.73,2.14c.78,0,1.56,0,2.31-.08‼ transform=‼translate(0 -20)‼ /></svg>                
            </div>            
        </div>
</div>
                            <input type=‼hidden‼ id=‼token‼ value=‼CEE9217EA074A4BDDC82343DB433A7B3FD119C2B6451D0196C1ABC672D47E44F‼ />
                        </div>
                    </div>
                </div>
            </div>
            <div class=‼row‼>
                <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12 alertrow‼>
                </div>
            </div>
        </div>
        <div class=‼mainContent‼>
            

<div id=‼trainRoute‼>
</div>

<div class=‼mapWrapp‼>
    <div id=‼map‼ class=‼map‼>
       
        
    </div>
</div>


<div class=‼panel-group‼ id=‼accordion‼ role=‼tablist‼ aria-multiselectable=‼true‼>
    <div class=‼panel panel-default‼>
        <div class=‼panel-heading side_buttons hidden-xs hidden-sm‼ role=‼tab‼ id=‼headingThree‼ data-toggle=‼collapse‼ data-target=‼#collapseThree‼ onclick=‼PanelHide(); FilterHide();‼>
            <a class=‼collapsed‼ role=‼button‼ data-toggle=‼collapse‼ data-parent=‼#accordion‼ href=‼#collapseThree‼ aria-expanded=‼false‼ aria-controls=‼collapseThree‼ onclick=‼PanelHide(); FilterHide();‼>
                Vyhled&#225;v&#225;n&#237;
            </a>
        </div>
        <div id=‼collapseThree‼ class=‼panel-collapse collapse‼ role=‼tabpanel‼ aria-labelledby=‼headingThree‼>
            <h1>
                Vyhled&#225;v&#225;n&#237;
                <button type=‼button‼ data-toggle=‼collapse‼ data-parent=‼#accordion‼ href=‼#collapseThree‼ aria-expanded=‼true‼ aria-controls=‼collapseThree‼ class=‼close‼><span aria-hidden=‼true‼>&times;</span></button>
            </h1>
            <div class=‼panel-body‼>
                <div class=‼search‼>
    <div class=‼container-fluid content‼>
        <div class=‼row‼>
            <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12 title‼>
                Nastaven&#237;
            </div>
        </div>
        <div class=‼row‼>
                <div class=‼col-lg-5 col-md-8 col-sm-12 col-xs-12‼>
                    <input id=‼searchByTrainNumberCB‼ type=‼checkbox‼ class=‼checkbox-custom‼ name=‼searchByTrainNumberCB‼ checked />
                    <label class=‼checkbox-custom-label‼ for=‼searchByTrainNumberCB‼>
                        podle č&#237;sla vlaku
                    </label>
                </div>
        </div>
        <div class=‼row‼>
            <div class=‼col-lg-5 col-md-8 col-sm-12 col-xs-12‼>
                <input id=‼searchByTrainNameCB‼ type=‼checkbox‼ class=‼checkbox-custom‼ name=‼searchByTrainNameCB‼ checked />
                <label class=‼checkbox-custom-label‼ for=‼searchByTrainNameCB‼>
                    podle jm&#233;na vlaku
                </label>
            </div>
        </div>


        <div class=‼row searchRadio‼>
            <div class=‼col-lg-5 col-md-5 col-sm-5 col-xs-5‼>
                <label>
                    obsahuje č&#225;st
                    <input class=‼searchContainsText‼ type=‼radio‼ name=‼radioSearchText‼ value=‼0‼ checked />
                    <span class=‼checkmark‼></span>
                </label>
            </div>
            <div class=‼hidden-lg hidden-md hidden-sm col-xs-2‼></div>
            <div class=‼col-lg-5 col-md-5 col-sm-5 col-xs-5‼>
                <label>
                    <input class=‼searchEqualsText‼ type=‼radio‼ name=‼radioSearchText‼ value=‼1‼ />
                    <span class=‼checkmark‼></span>
                    přesn&#253; text
                </label>
            </div>
        </div>
        <div class=‼row‼>
            <div class=‼col-lg-10 col-md-10 col-sm-12 col-xs-12‼>
                <img src=‼/Styles/images/icons-svg/ikona-hledat.svg‼ class=‼svgSearch2‼ />
                <table class=‼dxeButtonEditSys dxeButtonEdit‼ id=‼searchPhrase‼>
	<tr>
		<td style=‼display:none;‼><input id=‼searchPhrase_VI‼ name=‼searchPhrase_VI‼ type=‼hidden‼ /></td><td class=‼dxic‼ onmousedown=‼return ASPx.DDMC_MD(&#39;searchPhrase&#39;, event)‼ style=‼width:100%;‼><input class=‼dxeEditArea dxeEditAreaSys‼ id=‼searchPhrase_I‼ name=‼searchPhrase‼ onfocus=‼ASPx.EGotFocus(&#39;searchPhrase&#39;)‼ onblur=‼ASPx.ELostFocus(&#39;searchPhrase&#39;)‼ onchange=‼ASPx.ETextChanged(&#39;searchPhrase&#39;)‼ type=‼text‼ /></td><td id=‼searchPhrase_B-1‼ class=‼dxeButton dxeButtonEditButton‼ onmousedown=‼return ASPx.DDDropDown(&#39;searchPhrase&#39;, event)‼ style=‼background-color:White;border-style:None;-moz-user-select:none;padding-left:0px;padding-right:0px;padding-top:0px;padding-bottom:0px;‼><img id=‼searchPhrase_B-1Img‼ src=‼/Styles/images/icons-png/ikona-seznam.png‼ alt=‼v‼ style=‼height:20px;width:20px;‼ /></td>
	</tr>
</table><div id=‼searchPhrase_DDD_PW-1‼ class=‼dxpcDropDown dxpclW dxpc-ddSys‼ style=‼z-index:10000;display:none;visibility:hidden;‼>
	<div class=‼dxpc-mainDiv dxpc-shadow‼>
		<div class=‼dxpc-contentWrapper‼>
			<div class=‼dxpc-content‼>
				<table class=‼dxeListBox‼ id=‼searchPhrase_DDD_L‼ style=‼border-collapse:separate;‼>
					<tr>
						<td style=‼vertical-align:Top;‼><div id=‼searchPhrase_DDD_L_D‼ class=‼dxlbd‼ style=‼width:100%;overflow-x:hidden;overflow-y:auto;‼>
							<input id=‼searchPhrase_DDD_L_VI‼ type=‼hidden‼ name=‼searchPhrase$DDD$L‼ /><table style=‼border-collapse:separate;visibility:hidden!important;display:none!important;‼>
								<tr id=‼searchPhrase_DDD_L_LBI-1‼ class=‼dxeListBoxItemRow‼>
									<td id=‼searchPhrase_DDD_L_LBII‼ class=‼dxeListBoxItem‼>&nbsp;</td>
								</tr>
							</table><table id=‼searchPhrase_DDD_L_LBT‼ style=‼width:100%;border-collapse:separate;‼>
								<tr>
									<td></td>
								</tr>
							</table>
						</div></td>
					</tr>
				</table><script id=‼dxss_1145230500‼ type=‼text/javascript‼>
<!--
ASPx.createControl(MVCxClientListBox,'searchPhrase_DDD_L','',{'uniqueID':'searchPhrase$DDD$L','scStates':6,'scPostfix':'','stateObject':{'CustomCallback':''},'isSyncEnabled':false,'isComboBoxList':true,'hasSampleItem':true,'isHasFakeRow':true,'hoverClasses':['dxeListBoxItemHover'],'selectedClasses':['dxeListBoxItemSelected'],'disabledClasses':['dxeDisabled'],'itemsInfo':[]},{'SelectedIndexChanged':function (s, e) { ASPx.CBLBSelectedIndexChanged('searchPhrase', e); },'ItemClick':function (s, e) { ASPx.CBLBItemMouseUp('searchPhrase', e); }},null,{'decorationStyles':[{'key':'F','className':'dxeFocused','cssText':''}]});

//-->
</script>
			</div>
		</div>
	</div>
</div><script id=‼dxss_1763174120‼ type=‼text/javascript‼>
<!--
ASPx.AddHoverItems('searchPhrase_DDD',[[['dxpc-closeBtnHover'],[''],['HCB-1']]]);
ASPx.createControl(ASPxClientPopupControl,'searchPhrase_DDD','',{'uniqueID':'searchPhrase$DDD','adjustInnerControlsSizeOnShow':false,'popupAnimationType':'slide','closeAction':'CloseButton','popupHorizontalAlign':'LeftSides','popupVerticalAlign':'Below'},{'Shown':function (s, e) { ASPx.DDBPCShown('searchPhrase', e); }});

//-->
</script><script id=‼dxss_1271616722‼ type=‼text/javascript‼>
<!--
ASPx.AddHoverItems('searchPhrase',[[['dxeButtonEditButtonHover'],['padding-left:0px;padding-top:0px;padding-right:0px;padding-bottom:0px;'],['B-1']]]);
ASPx.RemoveHoverItems('searchPhrase',[[['B-100']]]);
ASPx.AddPressedItems('searchPhrase',[[['dxeButtonEditButtonPressed'],['padding-left:0px;padding-top:0px;padding-right:0px;padding-bottom:0px;'],['B-1']]]);
ASPx.RemovePressedItems('searchPhrase',[[['B-100']]]);
ASPx.AddDisabledItems('searchPhrase',[[['dxeDisabled'],[''],['','I']],[['dxeDisabled dxeButtonDisabled'],[''],['B-1'],,[[{'spriteCssClass':'dxEditors_edtDropDownDisabled'}]],['Img']]]);
ASPx.RemoveDisabledItems('searchPhrase',[[['B-100'],]]);
ASPx.createControl(MVCxClientComboBox,'searchPhrase','',{'scStates':2,'scPostfix':'','autoCompleteAttribute':{'name':'autocomplete','value':'off'},'isDropDownListStyle':false,'dropDownRows':8,'lastSuccessValue':null,'islastSuccessValueInit':true,'allowNull':true},{'SelectedIndexChanged':function(s,e){FilterApply();}},null,{'decorationStyles':[{'key':'F','className':'dxeFocused','cssText':''}]});

//-->
</script>                
            </div>
        </div>
    </div>
    <div class=‼container-fluid auto-height filterButton‼>
        <div class=‼row‼>
            <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12‼>
                <a onclick=‼FilterApply()‼ class=‼btn ok‼>Vyhledat</a>
                <a onclick=‼FilterReset()‼ class=‼btn storno‼>Obnovit v&#253;choz&#237;</a>
            </div>
        </div>
    </div>
</div>

            </div>
        </div>
    </div>
    <div class=‼panel panel-default‼>
        <div class=‼panel-heading side_buttons hidden-xs hidden-sm‼ role=‼tab‼ id=‼headingOne‼ data-toggle=‼collapse‼ data-target=‼#collapseOne‼ onclick=‼PanelHide(); FilterHide();‼>
            <a role=‼button‼ data-toggle=‼collapse‼ data-parent=‼#accordion‼ href=‼#collapseOne‼ aria-expanded=‼true‼ aria-controls=‼collapseOne‼ onclick=‼PanelHide(); FilterHide();‼>
                Filtr dopravců
            </a>
        </div>
        <div id=‼collapseOne‼ class=‼panel-collapse collapse‼ role=‼tabpanel‼ aria-labelledby=‼headingOne‼>
            <h1>
                Dopravci
                <button type=‼button‼ data-toggle=‼collapse‼ data-parent=‼#accordion‼ href=‼#collapseOne‼ aria-expanded=‼true‼ aria-controls=‼collapseOne‼ class=‼close‼><span aria-hidden=‼true‼>&times;</span></button>
            </h1>
            <div class=‼panel-body‼>
                <div class=‼carriers‼>
    <div class=‼container-fluid content‼>
        <div class=‼row allCheck‼>
            <div class=‼col-lg-6 col-md-6 col-sm-6 col-xs-12‼>
                <input name=‼allCarriers‼ class=‼carrierAll checkbox-custom‼ type=‼checkbox‼ id=‼allCarriers‼ checked onclick=‼$('#carrierMaskText').val(''); FilterCheckboxes('#carrierMaskText', '.carrierCB'); SetCheckBoxesByCheckbox('.carrierCB', '#allCarriers');‼ />
                <label class=‼checkbox-custom-label‼ for=‼allCarriers‼>
                    <span class=‼bold‼>všichni dopravci</span>
                </label>
            </div>
            <div class=‼col-lg-6 col-md-6 col-sm-6 col-xs-12‼>
                <input name=‼foreignCarriers‼ class=‼checkbox-custom‼ type=‼checkbox‼ id=‼foreignCarriers‼ checked />
                <label class=‼checkbox-custom-label‼ for=‼foreignCarriers‼>
                    <span class=‼bold‼>zahraničn&#237; dopravci</span>
                </label>
            </div>
        </div>
        <div class=‼row‼>
            <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12 carrierMask‼>
                <img src=‼/Styles/images/icons-svg/ikona-hledat.svg‼ class=‼svgSearch1‼ />
                <input class=‼form-control‼ type=‼text‼ id=‼carrierMaskText‼ onkeyup=‼FilterCheckboxes(this, '.carrierCB'); SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText');‼ />
            </div>
        </div>
        <div class=‼row‼>
            <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12 title‼>
                <div>
                    <span class=‼bold‼>Seznam dopravců</span>
                </div>
            </div>
        </div>
        <div class=‼row‼>
            <div class=‼col-lg-6 col-md-6 col-sm-6 col-xs-12‼>


                    <div class=‼fullWidth‼>
                        <input id=‼ARRIVA vlaky s.r.o.‼ name=‼ARRIVA vlaky s.r.o.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼991919‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼ARRIVA vlaky s.r.o.‼>
                            ARRIVA vlaky s.r.o.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼AŽD Praha, s.r.o.‼ name=‼AŽD Praha, s.r.o.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼992230‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼AŽD Praha, s.r.o.‼>
                            AŽD Praha, s.r.o.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼CityRail, a.s.‼ name=‼CityRail, a.s.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼992719‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼CityRail, a.s.‼>
                            CityRail, a.s.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼ČESK&#193; Z&#193;PADN&#205; DR&#193;HA s. r. o.‼ name=‼ČESK&#193; Z&#193;PADN&#205; DR&#193;HA s. r. o.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼993030‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼ČESK&#193; Z&#193;PADN&#205; DR&#193;HA s. r. o.‼>
                            ČESK&#193; Z&#193;PADN&#205; DR&#193;HA s. r. o.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼Česk&#233; dr&#225;hy, a.s.‼ name=‼Česk&#233; dr&#225;hy, a.s.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼990010‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼Česk&#233; dr&#225;hy, a.s.‼>
                            Česk&#233; dr&#225;hy, a.s.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼Die L&#228;nderbahn CZ s.r.o.‼ name=‼Die L&#228;nderbahn CZ s.r.o.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼993188‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼Die L&#228;nderbahn CZ s.r.o.‼>
                            Die L&#228;nderbahn CZ s.r.o.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼Die L&#228;nderbahn GmbH DLB‼ name=‼Die L&#228;nderbahn GmbH DLB‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼991943‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼Die L&#228;nderbahn GmbH DLB‼>
                            Die L&#228;nderbahn GmbH DLB
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼GW Train Regio a.s.‼ name=‼GW Train Regio a.s.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼991950‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼GW Train Regio a.s.‼>
                            GW Train Regio a.s.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼IDS LocoCare s.r.o.‼ name=‼IDS LocoCare s.r.o.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼993196‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼IDS LocoCare s.r.o.‼>
                            IDS LocoCare s.r.o.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼Jindřichohradeck&#233; m&#237;stn&#237; dr&#225;hy, a.s.‼ name=‼Jindřichohradeck&#233; m&#237;stn&#237; dr&#225;hy, a.s.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼991075‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼Jindřichohradeck&#233; m&#237;stn&#237; dr&#225;hy, a.s.‼>
                            Jindřichohradeck&#233; m&#237;stn&#237; dr&#225;hy, a.s.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼Kladensk&#225; dopravn&#237; a strojn&#237; s.r.o.‼ name=‼Kladensk&#225; dopravn&#237; a strojn&#237; s.r.o.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼992693‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼Kladensk&#225; dopravn&#237; a strojn&#237; s.r.o.‼>
                            Kladensk&#225; dopravn&#237; a strojn&#237; s.r.o.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼KŽC Doprava, s.r.o.‼ name=‼KŽC Doprava, s.r.o.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼991638‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼KŽC Doprava, s.r.o.‼>
                            KŽC Doprava, s.r.o.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼Leo Express Global a.s.‼ name=‼Leo Express Global a.s.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼991976‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼Leo Express Global a.s.‼>
                            Leo Express Global a.s.
                        </label>
                    </div>
            </div>
            <div class=‼col-lg-6 col-md-6 col-sm-6 col-xs-12‼>
                    <div class=‼fullWidth‼>
                        <input id=‼Leo Express s.r.o.‼ name=‼Leo Express s.r.o.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼993089‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼Leo Express s.r.o.‼>
                            Leo Express s.r.o.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼Leo Express Tenders s.r.o.‼ name=‼Leo Express Tenders s.r.o.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼993162‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼Leo Express Tenders s.r.o.‼>
                            Leo Express Tenders s.r.o.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼Lok&#225;lka Group, spolek‼ name=‼Lok&#225;lka Group, spolek‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼991257‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼Lok&#225;lka Group, spolek‼>
                            Lok&#225;lka Group, spolek
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼MBM rail s.r.o.‼ name=‼MBM rail s.r.o.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼991935‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼MBM rail s.r.o.‼>
                            MBM rail s.r.o.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼METRANS Rail s.r.o.‼ name=‼METRANS Rail s.r.o.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼991562‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼METRANS Rail s.r.o.‼>
                            METRANS Rail s.r.o.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼Puš s.r.o.‼ name=‼Puš s.r.o.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼991125‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼Puš s.r.o.‼>
                            Puš s.r.o.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼Rail system s.r.o.‼ name=‼Rail system s.r.o.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼992644‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼Rail system s.r.o.‼>
                            Rail system s.r.o.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼Railway Capital a.s.‼ name=‼Railway Capital a.s.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼992842‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼Railway Capital a.s.‼>
                            Railway Capital a.s.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼RegioJet a.s.‼ name=‼RegioJet a.s.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼991927‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼RegioJet a.s.‼>
                            RegioJet a.s.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼RegioJet &#218;K a.s.‼ name=‼RegioJet &#218;K a.s.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼993170‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼RegioJet &#218;K a.s.‼>
                            RegioJet &#218;K a.s.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼Slezsk&#233; zemsk&#233; dr&#225;hy, o.p.s.‼ name=‼Slezsk&#233; zemsk&#233; dr&#225;hy, o.p.s.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼991810‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼Slezsk&#233; zemsk&#233; dr&#225;hy, o.p.s.‼>
                            Slezsk&#233; zemsk&#233; dr&#225;hy, o.p.s.
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼V&#253;zkumn&#253; &#218;stav Železničn&#237;, a.s. ‼ name=‼V&#253;zkumn&#253; &#218;stav Železničn&#237;, a.s. ‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼992909‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼V&#253;zkumn&#253; &#218;stav Železničn&#237;, a.s. ‼>
                            V&#253;zkumn&#253; &#218;stav Železničn&#237;, a.s. 
                        </label>
                    </div>
                    <div class=‼fullWidth‼>
                        <input id=‼ZABABA, s.r.o.‼ name=‼ZABABA, s.r.o.‼ class=‼carrierCB checkbox-custom‼ type=‼checkbox‼ value=‼991612‼ checked onclick=‼SetAllCheckboxByChildAndMask('.carrierCB', '#allCarriers', '#carrierMaskText')‼ />
                        <label class=‼checkbox-custom-label‼ for=‼ZABABA, s.r.o.‼>
                            ZABABA, s.r.o.
                        </label>
                    </div>
            </div>
        </div>
    </div>
    <div class=‼container-fluid auto-height filterButton‼>
        <div class=‼row‼>
            <div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12‼>
                <a onclick=‼FilterApply()‼ class=‼btn ok‼>Zobrazit dopravce</a>
                <a onclick=‼FilterReset()‼ class=‼btn storno‼>Obnovit v&#253;choz&#237;</a>
            </div>
        </div>
    </div>
</div>

            </div>
        </div>
    </div>
    <div class=‼panel panel-default‼>
        <div class=‼panel-heading side_buttons hidden-xs hidden-sm‼ role=‼tab‼ id=‼headingTwo‼ data-toggle=‼collapse‼ data-target=‼#collapseTwo‼ onclick=‼PanelHide(); FilterHide();‼>
            <a class=‼collapsed‼ role=‼button‼ data-toggle=‼collapse‼ data-parent=‼#accordion‼ href=‼#collapseTwo‼ aria-expanded=‼false‼ aria-controls=‼collapseTwo‼ onclick=‼PanelHide(); FilterHide();‼>
                Ostatn&#237; filtry
            </a>
        </div>
        <div id=‼collapseTwo‼ class=‼panel-collapse collapse‼ role=‼tabpanel‼ aria-labelledby=‼headingTwo‼>
            <h1>
                Ostatn&#237; filtry
                <button type=‼button‼ data-toggle=‼collapse‼ data-parent=‼#accordion‼ href=‼#collapseTwo‼ aria-expanded=‼true‼ aria-controls=‼collapseTwo‼ class=‼close‼><span aria-hidden=‼true‼>&times;</span></button>
            </h1>
            <div class=‼panel-body‼>
                <div class=‼filter‼>
	<div class=‼container-fluid content‼>
		<div class=‼row‼>
			<div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12 title‼>
				Druhy vlaků osobn&#237; přepravy
			</div>
		</div>
		<div class=‼row‼>


			<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
				<input name=‼allPublicTrain‼ class=‼publicKindOfTrainAllCB checkbox-custom‼ type=‼checkbox‼ id=‼allPublicTrain‼ checked onclick=‼SetCheckBoxesByCheckbox('.publicKindOfTrainCB', '#allPublicTrain')‼ />
				<label class=‼checkbox-custom-label‼ for=‼allPublicTrain‼>
					<span>vše</span>
				</label>
			</div>

				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼LE‼ name=‼LE‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼LE‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼LE‼>
						<span>LE</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼Ex‼ name=‼Ex‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼Ex‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼Ex‼>
						<span>Ex</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼Sp‼ name=‼Sp‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼Sp‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼Sp‼>
						<span>Sp</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼rj‼ name=‼rj‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼rj‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼rj‼>
						<span>rj</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼TL‼ name=‼TL‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼TL‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼TL‼>
						<span>TL</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼EC‼ name=‼EC‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼EC‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼EC‼>
						<span>EC</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼SC‼ name=‼SC‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼SC‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼SC‼>
						<span>SC</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼AEx‼ name=‼AEx‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼AEx‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼AEx‼>
						<span>AEx</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼Os‼ name=‼Os‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼Os‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼Os‼>
						<span>Os</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼Rx‼ name=‼Rx‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼Rx‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼Rx‼>
						<span>Rx</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼TLX‼ name=‼TLX‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼TLX‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼TLX‼>
						<span>TLX</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼IC‼ name=‼IC‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼IC‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼IC‼>
						<span>IC</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼EN‼ name=‼EN‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼EN‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼EN‼>
						<span>EN</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼R‼ name=‼R‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼R‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼R‼>
						<span>R</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼RJ‼ name=‼RJ‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼RJ‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼RJ‼>
						<span>RJ</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼NJ‼ name=‼NJ‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼NJ‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼NJ‼>
						<span>NJ</span>
					</label>
				</div>
				<div class=‼col-lg-2 col-md-3 col-sm-3 col-xs-4‼>
					<input id=‼LET‼ name=‼LET‼ class=‼publicKindOfTrainCB checkbox-custom‼ type=‼checkbox‼ value=‼LET‼ onclick=‼SetAllCheckboxByChild('.publicKindOfTrainCB', '#allPublicTrain')‼ checked />
					<label class=‼checkbox-custom-label‼ for=‼LET‼>
						<span>LET</span>
					</label>
				</div>
		</div>
		<div class=‼row‼>
			<div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12‼>
				<div class=‼separator‼></div>
			</div>
		</div>

		<div class=‼row‼>
			<div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12 title‼>
				Zpožděn&#237;
			</div>
		</div>
		<div class=‼row‼>
			<div class=‼col-lg-4 col-md-5 col-sm-6 col-xs-6‼>
				<input id=‼lead‼ name=‼lead‼ class=‼delayCB checkbox-custom-delay‼ type=‼checkbox‼ value=‼0‼ checked onclick=‼RemoveCustomDelay();‼ />
				<label for=‼lead‼ class=‼checkbox-custom-label-delay delay_lead‼>
					n&#225;skok
				</label>
			</div>
			<div class=‼col-lg-4 col-md-5 col-sm-6 col-xs-6‼>
				<input id=‼to60‼ name=‼to60‼ class=‼delayCB checkbox-custom-delay‼ type=‼checkbox‼ value=‼60‼ checked onclick=‼RemoveCustomDelay();‼ />
				<label for=‼to60‼ class=‼checkbox-custom-label-delay delay_to60‼>
					31 - 60 minut
				</label>
			</div>
		</div>
		<div class=‼row‼>
			<div class=‼col-lg-4 col-md-5 col-sm-6 col-xs-6‼>
				<input id=‼to5‼ name=‼to5‼ class=‼delayCB checkbox-custom-delay‼ type=‼checkbox‼ value=‼5‼ checked onclick=‼RemoveCustomDelay();‼ />
				<label for=‼to5‼ class=‼checkbox-custom-label-delay delay_to5‼>
					0 - 5 minut
				</label>
			</div>
			<div class=‼col-lg-4 col-md-5 col-sm-6 col-xs-6‼>
				<input id=‼moreThen60‼ name=‼moreThen60‼ class=‼delayCB checkbox-custom-delay‼ type=‼checkbox‼ value=‼61‼ checked onclick=‼RemoveCustomDelay();‼ />
				<label for=‼moreThen60‼ class=‼checkbox-custom-label-delay delay_moreThen60‼>
					v&#237;ce než 1 hodina
				</label>
			</div>

		</div>
		<div class=‼row‼>
			<div class=‼col-lg-4 col-md-5 col-sm-6 col-xs-6‼>
				<input id=‼to15‼ name=‼to15‼ class=‼delayCB checkbox-custom-delay‼ type=‼checkbox‼ value=‼15‼ checked onclick=‼RemoveCustomDelay();‼ />
				<label for=‼to15‼ class=‼checkbox-custom-label-delay delay_to15‼>
					6 - 15 minut
				</label>
			</div>
			<div class=‼col-lg-4 col-md-5 col-sm-6 col-xs-6‼>
				<input id=‼unknown‼ name=‼unknown‼ class=‼delayCB checkbox-custom-delay‼ type=‼checkbox‼ value=‼-1‼ checked onclick=‼RemoveCustomDelay();‼ />
				<label for=‼unknown‼ class=‼checkbox-custom-label-delay delay_unknown‼>
					nezn&#225;m&#233;
				</label>
			</div>
		</div>
		<div class=‼row‼>
			<div class=‼col-lg-4 col-md-5 col-sm-6 col-xs-6‼>
				<input id=‼to30‼ name=‼to30‼ class=‼delayCB checkbox-custom-delay‼ type=‼checkbox‼ value=‼30‼ checked onclick=‼RemoveCustomDelay();‼ />
				<label for=‼to30‼ class=‼checkbox-custom-label-delay delay_to30‼>
					16 - 30 minut
				</label>
			</div>
		</div>
		<div class=‼row‼>
		</div>		
	</div>
	<div class=‼container-fluid auto-height filterButton‼>
		<div class=‼row‼>
			<div class=‼col-lg-12 col-md-12 col-sm-12 col-xs-12‼>
				<a onclick=‼FilterApply()‼ class=‼btn ok‼>Použ&#237;t filtr</a>
				<a onclick=‼FilterReset()‼ class=‼btn storno‼>Obnovit v&#253;choz&#237;</a>
			</div>
		</div>
	</div>
</div>

            </div>
        </div>
    </div>
</div>


        </div>
        <div class=‼refreshText‼>
            Automatick&#225; aktualizace za
            <span class=‼bold‼ id=‼refreshTime‼></span>
            <span class=‼bold‼> s</span>
        </div>
        <div class=‼footerrow hidden-sm hidden-xs‼>
            <input type=‼hidden‼ id=‼trNothing‼ value=‼ž&#225;dn&#233;‼ />
            <div class=‼row‼>
                <div class=‼col-lg-2 col-md-2‼>
                    <div class=‼footerBig‼>
                        Dopravců:
                        <span id=‼infoCarriersVal‼>0</span>
                        <br />
                        Vlaků:
                        <span id=‼infoCount‼>0</span>
                    </div>
                </div>
                <div class=‼col-lg-4 col-md-4‼>
                        <div class=‼footerBig footerBorderLeft‼>
                            Osobn&#237; vlaky typu:
                            <span id=‼infoPublicVal‼>0</span>
                        </div>
                </div>
                <div class=‼col-lg-2 col-md-2‼>
                    <div class=‼footerSmall footerBorderLeft‼>
                        Vlastn&#237;k: Spr&#225;va železnic, s.o.
                        <br />
                        Provozovatel: OLTIS Group, a.s.                        
                    </div>
                </div>
                <div class=‼col-lg-4 col-md-4‼>
                    <div class=‼footerSmall footerBorderLeft‼>
                        &#169; 2020 Spr&#225;va železnic, jak&#233;koliv užit&#237; dat bez souhlasu vlastn&#237;ka je porušen&#237;m z&#225;kona č. 121/2000 Sb., autorsk&#253; z&#225;kon, v platn&#233;m zněn&#237;
                        <span runat=‼server‼ id=‼OGCookieLink‼ class=‼CPPCookieLink‼ onclick='CookieShow();' title=‼Zobrazit nastaven&#237; Cookies.‼><img ID=‼OGCookieIcon‼ src=‼/Styles/images/cookieicon.svg‼ class=‼CookieFooterIcon‼/>Cookies.</span>
                    </div>
                </div>
            </div>
        </div>
    </div>
</body>
</html>".Replace("‼", "\"");
                case 3:
                    return @"Hezky se vyspi, ráno možná nevyjde slunce..
13. 09. 2022 8:58:35
Vdala jsem se za Tammyho, přímého potomka indiánů z kmene Čerokí. Indiáni jsou obyčejní, hrdí, moudří, stateční a ví, že život na planetě se odvíjí v cyklech. Neomylně cítí, že cyklus blahobytu končí. Vrátíme se všichni do lesů?
Indiánské mýty a pověsti jsou zvláštní a nahánějí hrůzu.
Indiáni sami říkají, že oni si na rozdíl od nás, moderních lidí, uvědomují, že svět je nebezpečné místo. Dívají se na nás udiveně, když říkáme, svět je tak báječný a plný možností, lidé jsou úžasní, víc duchovní, nic nám nehrozí, máme spoustu jídla, věcí a oblečení.
A přitom se z nás staly jen rozmazlené děti. Nesneseme ani kousek nepohodlí, studenou vodu, zimu a nabitý telefon na 30 % nás vyděsí téměř k smrti.
Pro čerokézské indiány je alfou a omegou života les, který je nejlepším učitelem přežití na světě.
Nic tě pro život nepřipraví lépe.
article_photo
(Danka Štoflová)
Jen si vzpomeňte, jak se vaše smysly zvláštně zostří, když vstoupíte do lesa.
Mozek si okamžitě rozpomene na pradávný boj o přežití, na nebezpečí, které číhá na každém rohu. Váš sluch, ať si to uvědomujete nebo ne, převezme základní funkci, neboť zrak a čich jsou tady téměř k ničemu. Vnímáte i ty sebemenší vibrace stromů, jejich větví, uvědomujete si každý krok, mech a hlínu pod nohama. Slyšíte tolik zvuků najednou, až si říkáte, to jsem si nikdy nevšiml, že tak dobře slyším. Cítíte v nose vlhkou hlínu, mech, vůni stromů a teplou trávu.
Všimli jste si někdy, co vám v lese udělá úplně úžasnou radost, až se vám srdce zachvěje?
Je to objevení potůčku, nebo studánky, tak křišťálově čisté a chladné, až odhodíte dlouhými lety opečovávaný strach „voda je otrávená a plná chemikálií“, pokleknete, spojíte dlaně a hltavě pijete. Nebo jahody, víte které, ty malinké, slaďounké, které freneticky obíráte a strkáte do pusy, a nemůžete se jich nabažit.
Nebo houby. Vzpomínáte si na pocit, když radostně běžíte k sametovým hlavičkám, nenápadně se krčících v mechu pod stromy, a voláte: „A támhle je další, a další, no já se zblázním. Tolik hub jsem ještě nikdy neviděl.“
Ta nádherná radost, co se vám tetelí kolem srdce, je ta prapůvodní, co nám Matička Země vložila do útrob. Je to bohatství, které bychom měli opatrovat a hýčkat. Je to radost ze života, dar, že vždycky přežijeme díky přírodě, i když to tak nevypadá.
Indiáni svým dětem před spaním neříkají, hezky se vyspi, čeká tě krásné a slunečné ráno, nový den.
Indiáni svým dětem říkají, pořádně se vyspi, je možné, že se ráno vzbudíš a nevyjde slunce.
Věřte mi nebo ne, indiánské děti mají každé ráno obrovskou radost, že slunce opravdu vyšlo.
Indiáni svým dětem u jídla neříkají, sněz, co chceš. Klidně to nech, když už nemůžeš, nic se neděje.
Indiáni svým dětem říkají: „Sněz, co můžeš, je možné, že další jídlo už nebude.“
Moderní děti by se zasmáli a řekli by, no né, co je to za blbost, vždyť jsou obchody plné jídla. Jenže to se může velice rychle změnit.
Indiáni mají geneticky zakódovanou informaci, že život planety a lidí se odvíjí v cyklech, a že cyklus blahobytu se zákonitě střídá s cyklem bídy a utrpení.
Indiáni svým dětem říkají: „Tenhle cyklus klidu a hojnosti, trvá už moc dlouho, brzy přijde změna.“
A jejich děti, jako jedny z mála na téhle planetě, budou umět nastražit past, ulovit zvíře, zabít ho, vyvrhnout, stáhnout, rozdělat oheň i bez zapalovače a upéct. Z kůže si udělat boty a rukavice, nebo teplou čepici.
Moderní děti umřou, protože zabít zvířátko, to se prostě nedělá. Je to nelidské.
Indiáni tvrdí, že bída a hlad, zažene všechny lidi zpátky do lesů. A tam si na žádnou lidskost nikdo hrát nebude. Přežije jen ten, kdo je tichý, bystrý, pozorný a silný.
Všichni se diví, kde indiáni berou radost ze života, když nemají téměř nic. Mají neuvěřitelný smysl pro humor, navzájem se dobírají a od srdce smějí.
Oni totiž pochopili, že vlastně nepotřebují vůbec nic, a přitom mají úplně všechno.
Uvědomují si, že slunce, které se ráno objeví na obloze, není samozřejmost, že déšť, který jim máčí tváře může být ten úplně poslední a že mírný vítr, který jim cuchá dlouhé vlasy, se může změnit ve vichřici. Vnímají okamžik.
Indiáni jsou připraveni na změnu a váží si každého dne, kdy nepřišla.
Každé ráno se pozorně podívají na oblohu, do nosu hluboce vtáhnou chladný vzduch a zadívají se do lesa, do korun stromů. Všimnou si, v jaké náladě jsou ptáci, a v jakém rytmu se pohybují listy stromů. Vnímají dech lesa a barvu oblohy.
Indiáni nedělají zbytečnou paniku, ani moc nepláčou. Jen tiše, sebe a své děti, připravují na změnu.
Radují se a smějí, a tím děkují za den, kdy se nic špatného nestalo.
Indiáni jsou stateční a hrdí. Tuto vlastnost vědomě posilují v dušičkách svých potomků, opakují jim prastaré příběhy o tom, jak je nepřátelé chtěli krutě vyhubit, zabíjeli jejich děti a ženy, zapalovali vesnice a obydlí, chtěli jim vzít jejich území, které jim po staletí patří.
Vyprávějí dětem o hrdinech svého kmene, kteří zemřeli, aby jejich potomci mohli žít.
Posilují v nich jakousi neviditelnou odpovědnost za život svůj a druhých, niternou potřebu postarat se o své jediné bohatství, ženy a děti, pokračovatele rodu. Nasytit je, rozdělat oheň a držet je v bezpečí. Ukazovat jim, co je správné a jak nejlépe přežit.
Děti do sebe hladově nasávají příběhy a informace. Touží být stejní jako jejich stateční, hrdí předci.
Ještě nikdy jsem neslyšela dítě ve škole tak hrdě a nahlas říct: „Já jsem Čerokéz, kdo jsi ty?“
A věřte mi, že každé americké dítě ví, že určitě není víc.
Protože málokdo z nás ví, kdo vlastně je. Jste pyšní, že jste Češi? Nebo vám v hlavách zaškrábe obava, co jsme to za národ, pořád se jen krčíme a bojíme zakřičet nahlas, co se nám nelíbí. Ovládají nás tupci, kteří si jen masírují své ego a na nás, na svůj národ, vůbec nemyslí.
Kamarádka mi vyprávěla, jak byla na dovolené v Egyptě. Říkala mi, holka, já se tak v jídelně příšerně styděla, že jsem Češka. Radši jsem ani nemluvila. Jak jsme rozeřvaní, hulvátští, neumíme vychovávat ke slušnosti ani své děti. Jak jsme povýšení, ze svého blahobytu, který neznamená vůbec nic.
Indiánské děti jsou tiché. Ne, že by neuměli řádit a hrát si. Ale hrají si prostě jinak. Neustále naslouchají a vnímají své okolí. Doma, svoji matku a její náladu, hrnce bublající na kamnech /kdy už bude něco dobrého?/, zvuky zvenčí /není to dodávka mého táty?/, a venku dokonale vnímají zvuky lesa. Pozorně do sebe nasávají informace, a vědí přesně, i když nemají hodinky, kdy mají jít domů.
Přátelství je pro ně tak důležité, jako jejich vlastní život. Protože bez přítele nemáš nic. Vyndá ti třísku z nohy, a dělá, že nezahlédl slzy, když jste přikrčení v trávě sledovali, jak vlk brutálně zadávil srnku. Pomůže ti nahoru na strom a dá ti facku, když zbytečně ulomíš větev.
Svoje přátelství si indiáni udržují až do smrti. Jsou pro ně tak vzácná a důležitá, že je může přetrhnout jedině smrt.
Lásku ke svým ženám prožívají hluboce a niterně, a můžete ji zahlédnout v očích, při běžných činnostech. Třeba když jejich žena utírá pusinku kojenci, dívají se na ni tak láskyplně a hřejivě, že ani nemusí nic říkat. Indiáni opravdu milují.
Je to cit, hluboko zakořeněný v duši, silný a mocný, který mě i po letech mezi indiány, nepřestává překvapovat. Pro indiána je jeho žena, kus sebe samého. Jsou zvláštně propojení, hluboce šťastní, a i beze slov vědí, jak na tom právě jsou. Indiáni by pro svou ženu, udělali všechno na světě. Jsou svázaní neviditelnou stuhou kolem svých srdcí, aniž by jejich prsty okatě zdobil snubní prsten.
Není to jako v moderních domácnostech, kdy muž řekne: „Moje žena mi děsně leze na nervy, po dětech příšerně ztloustla, je hloupá a štěkavá. Vyměním ji za mladší model, líbivý a lépe ovladatelný.“
Indián řekne: „Moje žena není šťastná, cítím to. Zaslouží si více péče a pozornosti, a také bych jí měl ubrat něco z jejích povinností. Z lesa ji přinesu kytičku lučních květin, ukážu ji tu nejkrásnější z nich a povím jí, že je stejně krásná a křehká jako ona. Také ji budu víc objímat, aby cítila, ke komu opravdu patří, a věděla, že je se mnou v bezpečí.“
Když se indiánská žena trošinku spraví, její muž se zasměje. „A tohle všechno je moje. Dobře se starám o svoji rodinu, moje žena a děti nestrádají. Líbí se mi takhle, je měkká a víc hřeje. Vím, že hubená žena je nervní typ. Chci laskavou a spokojenou ženu, co se hodně směje. A to ta moje je.“
Indiánští muži a ženy často pracují v sociálních službách a ve zdravotnictví. Už jsem vám o tom psala.
Je to proto, že v jejich duších mocně přede životní motor, v taktu - pomáhej druhým, potřebují tě. Soucit a hluboká lidskost se spojí v jediném, hřejivém dotyku: „Já vím, co je utrpení, koluje v mé krvi a já prostě nedopustím, aby někdo trpěl víc, než musí.“ Vědomí, že mohou odebrat utrpení a rozzářit nemocné tvářičky, hubených, zkroucených tělíček je posiluje a dává jim chuť ráno vstát a jí do práce, každý den zas a znovu. Kdo jiný to udělá lépe, než já?
Winnie je indiánská žena středního věku. Pracuje na lůžkovém oddělení následné péče, u lidiček, kteří se už nikdy nevrátí domů. Někteří si to uvědomují, a někteří ne.
Jednoho rána, jako každý den, pečovala o starou paní Haydenovou, krmila ji a myla, a když jí rozčesávala dlouhé, šedé vlasy, vesele jí vyprávěla o svých dětech.
Paní Haydenová se z ničeho nic zle rozkřičela, vytrhla jí kartáč z ruky a hodila ho vztekle na zem.
„Já už nechci Winnie, abys ke mně chodila! Nepřeji si, aby se o mě starala nějaká špinavá, černá indiánka, špindíra a hloupá ženská!“
Winnie strnula.
A pak se posadila ke staré ženě na postel a smutně se usmála.
„Já vím, že nejste zlá ženská, paní Haydenová. Jenom jste nešťastná a osamělá. A nevíte pár věcí.“
Otřela si slzičku z oka.
„Ano, mám tmavší kůži, to je pravda, ale je to jen proto, že my, indiáni, žijeme už po staletí venku, na čerstvém vzduchu, v přírodě. Kůže nám ztmavla, aby se tak chránila před celodenním sluncem. Říká se tomu evoluce. Černé vlasy máme proto, abychom se za tmavých nocí, když se nás nepřátelé snažili vyhubit, mohli bezpečně schovat v lese. Splynout se stromy a noční tmou, zachránit si život. Bílá, zářivá kůže a světlé vlasy, by nás v měsíčních paprscích prozradily. Všechno, co nám příroda nadělila, je jen a jen pro náš prospěch, pro naše přežití.
Všimla jste si někdy, jaké my indiáni máme zářivé oči? Všichni, do jednoho.
Je to proto, že za dlouhých nocí, s láskou hledíme nahoru ke hvězdám. Žijí tam naši mrtví předci. A když toho svého milovaného poznáme a najdeme na noční obloze, vesele nám zabliká na pozdrav. Naše oči jsou zářivé od svitu hvězd, jejich síla a věčnost v nich přetrvává, a když se na vás teď dívám, dívají se na vás i zářivé hvězdy, které jsou jinak v obrovské dálce.
Jediné, co opravdu nejsem, tak nejsem špindíra. Udržuji vás v naprosté čistotě, a není mi jedno, jestli se cítíte pohodlně a příjemně. Na rozdíl od vašich dvou dcer, které za vámi nechodí a je jim úplně jedno, jestli jste čistá a voňavá, a jestli se smějete. Kdybych neměla pocit, že je o vás dobře postaráno, nemohla bych se doma podívat do očí své matce a svým dětem. Cítila bych, že jsem selhala v tom nejdůležitějším na světě. V lidskosti. Tady a teď, jsem vaše dcera já. Krmím vás, myji a povídám si s vámi o běžných věcech. Nejsem nikdo cizí. Je pro mě důležitá vaše důstojnost. A zítra přijdu znova a další den také. Mohla bych si najít jinou práci, například v obchodě u pokladny. Ale zkusila jsem to pár dní a málem jsem umřela. Ta práce neměla žádný smysl.“
Nadechla se a potáhla nosem.
„Tahle práce tady, smysl má. Vy víte, že jste na sklonku svého života. Prožijte ho prosím nejlépe jak můžete. Já vám s tím pomůžu. Budu se smát, vyprávět vám o své rodině, číst vám vaše nejmilejší knihy, dojímat se hudbou. Budu vás držet za ruku, když vám bude úzko, budu vás hladit, když budete mít bolesti. Ať mám kůži tmavou nebo ne, budu tady každý den. Budu se na vás usmívat a ukazovat vám to zvláštní světlo, které mám uvnitř. Je to sluneční paprsek, který má každičký, živý tvor od svého narození v sobě. Je to životní síla. Ten váš už je slabý, schoulený do malé kuličky, uprostřed hrudi. A ten můj, silný a zářivý, ten váš, každé ráno rozbaluje, natahuje a zahřívá. Tuhle práci nemůže dělat nikdo se slabým paprskem, rychle ho vyčerpá a nemá pak z čeho dávat. Možná jsem hloupá, nemám žádné extra vzdělání, ale my indiáni věříme, že životní síla, to teplé a hřejivé, co cítíme uprostřed hrudníku, můžeme předávat dál. Nechte mě prosím, ať vás zahřeji. Dnes, zítra i pozítří. Beze mě vám bude zima, paní Haydenová.“
Stará žena se stydlivě usměje. „Promiň zlatíčko, jsem jenom zlá, osaměla ženská. A bojím se. Bojím se být sama, a přitom každého od sebe odstrkuju. Nechci být na obtíž. Nechci, abys viděla moje intimní partie a myla je. Stydím se za své staré, nemohoucí tělo.“
Winnie ji pohladí po ruce. „Já nevidím staré, nemohoucí tělo. Já vidím tělo své matky, kterou miluji, která mě potřebuje a vracím ji tak péči, kterou mi v dětství a pak celý život dávala. Mám úžasný pocit, když vím, že se cítíte pohodlně a dobře. Váš životní paprsek pokaždé zazáří a spojí se s tím mým. Nabíjí mě to. Vím, že jsem svůj den nestrávila zbytečně. Tady jste moje matka.“
Paní Haydenová se rozpláče. „Ach, Winnie, jak se ti jen odvděčím za to, co pro mě děláš.“
Winnie stiskne její ruku pevněji.
„Mně stačí jediné, paní Haydenová, zablikejte na mě vesele z noční oblohy, až budete jednou tam nahoře. Poznám vás a vaše zářivé světlo se usadí v mých očích. A já ho pak budu předávat dál, těm, co ho doopravdy potřebují.“
Moje děti a můj muž, tohle zvláštní světlo v očích mají. Jsou hluboké, zářivé, moudré, daleké, a přesto tak hřejivě blízko. Jako hvězdy.
Howgh.";
                case 4:
                    return @"Liz Truss faces unrest over public spending cuts and pensions triple lock threat
Senior Tory ministers, Labour party and the public all expected to resist cuts, especially to frontline services
UK politics live – latest news updates
Liz Truss at the Conservative party conference in Birmingham.
A YouGov poll shows half of Conservative members think Liz Truss should resign. Photograph: Christopher Thomond/The Guardian
Pippa Crerar and Jessica Elgot
Tue 18 Oct 2022 19.07 BST
Last modified on Tue 18 Oct 2022 19.32 BST
Liz Truss is facing cabinet unrest over her plans for brutal public spending cuts across all departments after the disastrous mini-budget put major pledges at risk, including the pensions triple lock.
The prime minister held a 90-minute cabinet meeting on Tuesday in which she warned ministers that “difficult decisions” lay ahead.
The chancellor, Jeremy Hunt, told them “everything is on the table” as he strives to find tens of billions of pounds in savings after ditching Truss’s economic plan. Health, education and welfare are among those areas expected to be hit.
One Whitehall official said departments were already preparing for cuts “significantly higher” than previously planned, with Hunt’s tax U-turns estimated to raise L32bn, leaving a L38bn hole in the public finances.
Truss remains in a precarious position, having in effect handed power to Hunt, with a YouGov poll showing that half of Conservative members think she should resign. A significant majority would also support a coronation of a new prime minister by Tory MPs.
Senior ministers are expected to capitalise on Truss’s weakness and resist deep cuts, with the defence secretary, Ben Wallace, indicating he would be prepared to quit his job if the prime minister does not honour her campaign pledge to spend 3% of GDP on defence by 2030.
She faces a humiliating prime minister’s questions against Keir Starmer on Wednesday after one of the most dramatic U-turns in modern times; and Downing Street aides are concerned rebel Tory MPs could seize the moment to publicly call for her to go.
Asked whether it was “no longer a question of whether Truss goes, but when she goes”, the former levelling up secretary Michael Gove said: “Absolutely right.”
In a speech, he added: “All of us are going to face a hell of a lot of pain in the next two months.”
Liz Truss is the last stop on a long Tory journey away from grownup government
Rafael Behr
Rafael Behr
Read more
Whitehall officials said planned cuts – drawn up before Kwasi Kwarteng was sacked but now paused, in which departments would make capital savings of 10% to 15% and day-to-day savings of at least 2% – were now likely to be more brutal.
“It’s fair to say that things are looking tougher than they were,” one said. “Every department is going to have to play a role in finding efficiencies. No department will be ring-fenced.”
Government sources confirmed the existing three-year spending review would not be rewritten, but said savings would be required within that because of inflation. In the next spending round, from 2024-25, spending is expected to fall dramatically.
Hunt told ministers they would be asked to find ways to save money, with the focus on areas that would not affect public services. He will meet each one this week to thrash out figures which will be submitted to the Office for Budget Responsibility on Friday.
However, almost everything appears to be up for grabs. Truss could abandon the state pension triple lock to help plug the fiscal black hole, leaving more than 12 million people facing a real-terms cut in their incomes in April.
Her spokesperson refused four times to commit to keeping the pensions guarantee, despite it being a key 2019 manifesto commitment that Truss confirmed she would stick to just weeks ago.
Under the guarantee, state pensioners would get a rise of about 10% in April 2023, which would take their weekly payment from L185.15 to just over L200, helping to alleviate some of the other pressures on their budgets during the cost of living crisis.
Chloe Smith, the work and pensions secretary, has previously defended the triple lock but a source close to her said she accepted everything was on the table now.
In contrast, Truss backed off a plan to scrap the government’s commitment to raise defence spending to 3% of GDP by 2030 after a defence source insisted Wallace “will hold the prime minister to the pledges made”, and the armed forces minister James Heappey threatened to quit during a live radio interview.
As one of the biggest spending departments, health is also expected to be in the line of fire, despite Covid backlogs, ambulance delays and thousands of NHS staff vacancies.
Sign up to First Edition
Free daily newsletter
Archie Bland and Nimo Omer take you through the top stories and what they mean, free every weekday morning
Privacy Notice: Newsletters may contain info about charities, online ads, and content funded by outside parties. For more information see our Privacy Policy. We use Google reCaptcha to protect our website and the Google Privacy Policy and Terms of Service apply.
One Whitehall source said Hunt’s initial approach would be to try to find savings by suppressing spending in line with inflation, and tweaking the tax system.
Cuts to frontline services were far less politically palatable, with one source calling it “incredibly difficult to the point of being toxic, and it’d be weaponised by the Labour party very effectively”.
In departments such as health and education, officials have already said making cuts of more than around 2% would be near impossible.
Other tax measures under consideration include a new windfall tax, which Hunt hinted he favoured during exchanges in the Commons, as well as Treasury-favoured measures, such as targeting specific tax reliefs for landowners.
The new cap on revenues of renewable energy generators is hoped to raise L3bn to L10bn, depending on where the level is set, but a further windfall tax on oil and gas producers is also being explored.
Targeting pension tax relief is favoured by some in the Treasury, which could generate around L10bn a year if the relief was removed on higher rate payers.
Having spoken to the right-wing European Research Group on Tuesday night, Truss will continue to meet disgruntled Tory MPs on Wednesday, touring the Commons tea rooms after PMQs.
Groups of Tory MPs are still plotting her downfall but have so far failed to coalesce around a single unity candidate.
The Trades Union Congress general secretary, Frances O’Grady, said of the prospect that the government could ask departments to find more savings: “They just can’t. People won’t take it any more. Border guards, prison officers, NHS nurses - who are they going to cut? What’s left? I think it’s untenable for them.
“They’re going to have to think again, if they think they can just come back and keep hammering working people, because I’ve never seen such determination. People have had it; they’re almost beyond anger. They’re just saying, no.”
The International Monetary Fund has praised Hunt’s actions in reversing most of the mini-budget’s unfunded tax cuts.
“The UK authorities’ recent policy announcements signal commitment to fiscal discipline and help better align fiscal and monetary policy in the fight against inflation,” a spokesperson said.
… as you’re joining us today from the Czech Republic, we have a small favour to ask. Tens of millions have placed their trust in the Guardian’s fearless journalism since we started publishing 200 years ago, turning to us in moments of crisis, uncertainty, solidarity and hope. More than 1.5 million supporters, from 180 countries, now power us financially – keeping us open to all, and fiercely independent.
Unlike many others, the Guardian has no shareholders and no billionaire owner. Just the determination and passion to deliver high-impact global reporting, always free from commercial or political influence. Reporting like this is vital for democracy, for fairness and to demand better from the powerful.
And we provide all this for free, for everyone to read. We do this because we believe in information equality. Greater numbers of people can keep track of the events shaping our world, understand their impact on people and communities, and become inspired to take meaningful action. Millions can benefit from open access to quality, truthful news, regardless of their ability to pay for it.
";
                case 5:
                    return @"JEROME KLAPKA JEROME
TŘI MUŽI VE ČLUNU 

AUTORŮV PROPAGAČNÍ ÚVOD
K této knize se svět chová velice laskavě. Jen z různých edicí vydaných v Anglii se už prodalo přes půldruhého miliónu výtisků. A v Chicagu se mi už před mnoha lety dostalo ujištění - z úst jistého podnikavého piráta nyní v. v. - že víc než milión výtisků se prodalo ve Spojených státech; vzhledem k tomu, že tam má kniha vyšla ještě před mezinárodní dohodou o ochraně autorských práv, nepřineslo mi to sice pražádný zisk hmotný, ale věhlas a obliba, které jsem si tím vysloužil u americké veřejnosti, to jsou aktiva, nad nimiž nelze ohrnovat nos. Přeloženo bylo toto mé dílo snad do všech jazyků evropských, a nadto do několika asijských. To mi vyneslo mnoho tisíc dopisů od lidí mladých i od lidí starých; od lidí zdravých i od lidí nemocných; od lidí veselých i od lidí smutných. Přicházely ze všech končin světa, od mužů i žen všech zemí. A i kdyby nebylo jiného efektu než těch dopisů, byl bych rád, že jsem tu knihu napsal. Schoval jsem si několik zčernalých listů z jednoho výtisku, které mi poslal mladý koloniální důstojník z Jižní Afriky; vzal je z torny mrtvého kamaráda, kterého našli na bojišti na Spionskopu. Tolik k doporučujícím dokladům. Zbývá jen ještě objasnit, jakými přednostmi se dá ospravedlnit tak mimořádný úspěch. Já sám to objasnit nedovedu. Napsal jsem knihy, které mi připadají mnohem chytřejší, knihy, které mi připadají mnohem humornější. Ale pokud si mě čtenářská obec vůbec připomíná, pak jedině jako autora „ Tří mužů ve člunu (o psu nemluvě) “. Jistá část kritiků naznačovala, že ta kniha má proto takový úspěch u lidí, že je tak neuhlazená, že postrádá jakýkoli humor; ale dneska už má člověk dojem, že takhle se ta hádanka rozluštit nedá. Slabé umělecké dílo se může prosadit jen v jisté době a jen u omezené skupiny publika; ale nedokáže okruh toho publika téměř po celou polovinu století stále a stále rozšiřovat. Ale ať už je vysvětlení jakékoli, já jsem došel k závěru, že napsání té knihy si smím připočíst k dobru. Ovšem, jestli jsem ji doopravdy napsal. Moc se na to psaní totiž nepamatuji. Pamatuji se jenom, že jsem se cítil velmi mladý a z důvodů čistě osobních nesmyslně sám se sebou spokojený. Bylo to v létě a v létě je Londýn tak krásný. Ležel pod mým oknem jako nějaké město z pohádky, v závoji ze zlatého oparu, neboť pokoj, kde jsem pracoval, byl vysoko nad cylindry komínů; a za nocí zářila světla z dálek pode mnou, takže jsem shlížel dolů jako do Aladinovy jeskyně s drahokamy. A v těchhle letních měsících jsem psal tuto knihu; měl jsem dojem, že nic jiného se dělat nedá.
Autorův propagační úvod
7
AUTOROVA PŘEDMLUVA K PRVNÍMU VYDÁNÍ
Hlavní krása této knihy nepočívá ani tak v literárním stylu nebo v obsáhlosti a užitečnosti informací, které podává, jako spíš v její čiré pravdivosti.
Její stránky vytvořil záznam událostí, které se skutečně zběhly.
Já jsem je pouze přibarvil, ale za to jsem si nenaúčtoval žádný příplatek.
George a Harris a Montmorency, to nejsou plody básnické obrazotvornosti, ale bytosti z masa a krve, zvláště George, který váží dobrých šestaosmdesát kilo. Existují možná díla, jež nad toto dílo vynikají hloubkou myšlenek a znalostí lidské povahy;
existují možná knihy, jež jsou schopny soupeřit s tou mou v originalitě a v objemnosti; ale ani jediný spis, aspoň z těch, které byly až dodnes objeveny, ji nemůže překonat, pokud jde o beznadějnou a nevyléčitelnou pravdomluvnost.
A to právě, ještě víc než všechny ostatní jeho půvaby, zvýší bezpochyby cenu tohoto svazečku v očích seriózního čtenáře; a dodá ještě větší váhu naučení, které ten příběh poskytuje.
V Londýně, v srpnu 1889
Autorova předmluva k prvnímu vydání
9

Tři marodi - Čím strádají George a Harris - Oběť jednoho sta a sedmi fatálních nemocí - Užitečný recept - Léky na jaterní neduhy u dětí - Jsme zajedno, že jsme přepracováni a že si potřebujeme odpočinout - Týden nad vzdouvající se hlubinou? - George navrhuje řeku - Montmorency vznáší námitku - Původní návrh je prosazen většinou tří hlasů proti jednomu.
I
Byli jsme čtyři - George, William Samuel Harris, já a Montmorency. Seděli jsme u mě v pokoji, kouřili jsme a každý z nás vykládal, jak je špatný - špatný z hlediska zdravotního, pochopitelně.
Ani jeden jsme se necítili ve své kůži, a to nám šlo na nervy.
Harris říkal, že chvílemi se s ním všechno začne tak prapodivně motat, že si stěží uvědomuje, co dělá; a pak říkal George, že s ním se taky chvílemi všechno motá a že i on si stěží uvědomuje, co dělá. A já, já zas měl v nepořádku játra. Věděl jsem, že jsou to játra, to, co mám v nepořádku, protože jsem si zrovna přečetl prospekt na zázrační jaterní pilulky, kde byly podrobně popsány příznaky, podle nichž člověk pozná, že má v nepořádku játra. Já měl ty příznaky všechny.
Prapodivná věc, ale já když si čtu reklamní oznámení na nějakou zázračnou medicínu, tak vždycky dojdu k závěru, že tou dotyčnou nemocí, o které se tam pojednává, trpím v té nejvirulentnější podobě. Pokaždé odpovídá diagnóza přesně všem pocitům, které mám odjakživa já.
Vzpomínám si, jak jsem si jednoho dne zašel do knihovny Britského muzea, abych se poučil, co má člověk podnikati proti jakési nepatrné chorůbce, která se o mě pokoušela - mám dojem, že to byla senná rýma. Vyhledal jsem příslušnou publikaci a prostudoval jsem si všechno, co jsem si prostudovat přišel; a potom jsem tak nějak bezmyšlenkovitě a bezplánovitě obracel listy a naprosto lhostejně si začal číst o jiných nemocech, jen tak povšechně. Už si nepamatuji, co to bylo za chorobu, do které jsem se poprvé víc zahloubal - vím jenom, že to byla nějaká strašlivá, zhoubná metla lidstva - ale ještě jsem ani zpolovičky nepřelétl očima výčet „varovných symptomů“, a už na mě dolehlo poznání, že tu chorobu mám.
Na chvilku jsem úplně zkameněl hrůzou; a pak jsem, už v naprosté apatii zoufalství, zase obrátil pár stránek. narazil jsem na tyfus - pročetl jsem si příznaky - objevil jsem, že mám i tyfus, a už ho mám zřejmě řadu měsíců a že to vůbec nevím - a ptal jsem se v duchu, co mám ještě; nalistoval jsem tanec svatého Víta - zjistil jsem, jak jsem očekával, že ten mám taky - začal jsem se o svůj případ zajímat a rozhodl jsem se prozkoumat ho důkladně, a tak jsem se do toho dal podle abecedy. Prostudoval jsem Addisonovu chorobu a dověděl se, že na ni stůňu a že se do čtrnácti dnů může vystupňovat v addisonskou krizi.
O Brightově nemoci jsem se ke své úlevě dočetl, že ji mám jen ve formě zcela mírné, takže v mém případě se s ní snad dá ještě nějaký čas žít. Zato cukrovku jsem měl s vážnými komplikacemi a s cholerou jsem se zřejmě už narodil. Svědomitě jsem probádal všechna písmena abecedy a pouze o jediném neduhu jsem mohl s jistotou usoudit, že jím netrpím, a to o sklonu k samovolným potratům.
V první chvíli mě to dost zamrzelo; připadalo mi to jako urážlivé přezírání. Jak to, že netrpím sklonem k samovolným potratům? Jak to, že zrovna já mám být takto omezován? Po nějaké chvíli však ve mně převládly pocity méně chamtivé. uvážil jsem, že mám všechny ostatní ve farmakologii známé nemoci, začal jsem se na celou tu věc dívat méně sobecky a došel jsem k rozhodnutí, že se bez sklonu k samovolným potratům obejdu. Záškrt, jak se ukázalo, mě zachvátil, aniž jsem si toho povšiml, rovnou ve svém nejzavilejším stádiu a žloutenkou infekční jsem se očividně nakazil už ve věku jinošském. Po žloutence už tam žádné další choroby nebyly, usoudil jsem tudíž, že ani ve mně už nic jiného nehlodá.
Seděl jsem a dumal. Přemítal jsem, jaký to musím být z lékařského hlediska zajímavý případ a jak cennou akvizicí bych byl pro kandidáty veškerého lékařství. Kdyby medici měli mne, nemuseli by dělat žádnou klinickou praxi. Já sám vydám za celou kliniku. Úplně by jim stačilo párkrát si mě obejít, a hned by si mohli doběhnout pro diplom.
Pak mi v mysli vyvstal problém, kolik asi mi ještě zbývá let života. Zkoušel jsem sám sebe vyšetřit. Chtěl jsem si spočítat puls. Nejdřív jsem si vůbec žádný puls nenahmatal. Potom se zčistajasna roztepal. Vytáhl jsem hodinky a odpočítával. Napočítal jsem sto čtyřicet sedm tepů za minutu. Pak jsem chtěl vědět, jak mi tluče srdce. Ale ani srdce jsem si nenahmatal. Úplně se mi zastavilo. Od té doby jsem už byl sice přinucen přiklonit se k názoru, že na svém místě být muselo a zastavit se nemohlo, ale vysvětlení mi chybí pořád. Proklepal jsem si celé své průčelí, od míst, kterým říkám pás, až k hlavě, vzal jsem to i kousíček do stran a kousíček přes ramena na záda, ale na nic podezřelého jsem nenarazil a nic podezřelého jsem neslyšel. Ještě jsem se pokusil kouknout na jazyk. Vyplázl jsem ho, jak nejdál to šlo, zavřel jsem jedno oko a tím druhým jsem se snažil jazyk prohlédnout. Viděl jsem mu jenom na špičku a jediné, co mi ta námaha vynesla, bylo ještě důkladnější ujištění, že mám spálu.
Vešel jsem do té čítárny jako šťastný, zdravím kypící člověk. Ven jsem se vybelhal jako zchátralá troska.
Odebral jsem se k svému lékaři. Je to můj dobrý, starý kamarád, a když si vezmu do hlavy, že stůňu, tak mi vždycky sáhne na puls, mrkne mi na jazyk a popovídá si se mnou o počasí - a to všechno zadarmo; tak jsem si řekl, že tentokrát mu prokážu znamenitou službu, když k němu zajdu. „Doktor nesmí vyjít ze cviku,“ povídám si. „Ať má tedy mne! Na mně se pocvičí mnohem víc než na sedmnácti stech obyčejných, běžných pacientů s jednou, nanejvýš dvěma nemocemi.“ A tak jsem zašel rovnou k němu a on povídá:
„Tak co tě trápí?“
„Líčením, co mě trápí,“ řekl jsem, „nebudu mařit tvůj čas, milý příteli. Život je krátký, takže bys mohl vydechnout naposled, než bych s tím líčením byl hotov. Povím ti raději, co mě netrápí. netrápí mě sklon k samovolným potratům. Proč mě sklon k samovolným potratům netrápí, to ti říci nemohu; spokoj se s faktem, že mě netrápí. Zato mě trápí všechno ostatní.“
A pověděl jsem mu, jak jsem na to všechno přišel.
On si mě otevřel tam, kde mám ústa, nakoukl do mě, chňapl mi po zápěstí, pak mě, zrovna když jsem to nečekal, praštil přes prsa - tomu říkám zbabělost - a hned vzápětí mi do prsou trkl skrání. Načež se posadil, napsal recept, složil ho a podal mi ho a já si ho strčil do kapsy a šel jsem pryč.
Ani jsem ten recept nerozložil. Šel jsem s ním do nejbližší lékárny a tam jsem se s ním vytasil. Lékárník si ho přečetl a vrátil mi ho.
Řekl, tohle že on nevede.
„Jste přece lékárník?“ zeptal jsem se.
„Ano, jsem lékárník,“ řekl on. „Kdybych byl potravní konzum a k tomu ještě rodinný penzión, pak bych vám snad mohl být k službám. Co mi v tomto případě hází klacky pod nohy, je skutečnost, že jsem pouze lékárník.“
Přečetl jsem si ten recept. Stálo v něm:
1 půlkilový biftek, k tomu
1 půllitr piva
pravidelně po 6 hodinách,
1 patnáctikilometrová procházka každé ráno,
1 postel přesně v 11 každý večer.
A neláduj si do hlavy věci, kterým nerozumíš.
Řídil jsem se podle těchto pokynů, což mělo ten šťastný výsledek - mluvím jenom za sebe - že můj život byl zachován a běží dokonce dodnes.
Tentokrát, abych se vrátil k tomu prospektu na jaterní pilulky, jsem však měl všechny příznaky zcela nepochybně, především ten hlavní, totiž „naprostou nechuť k jakékoli práci.“
Co já si v tomhle směru vytrpím, žádný jazyk není s to vypovědět. To je u mě už od nejútlejšího dětství učiněné mučednictví. Když jsem byl větší chlapec, nebylo snad jediného dne, aby mě ta nemoc nechala na pokoji. Lékařská věda nebyla tenkrát ještě tak pokročilá jako dnes, a tak to naši napořád přičítali lenosti.
„Tak necháš už konečně toho válení, ty kluku líná, ulejvácká, a začneš dělat něco pořádného, aby sis zasloužil byt a stravu?“ křičeli na mě, nevědouce samozřejmě, že jsem nemocný.
A nedávali mi pilulky; dávali mi pohlavky. Ale ty pohlavky, ať to zní sebeneuvěřitelněji, mě často vyléčily - na nějakou chvíli. Teď už dovedu posoudit, že jeden takový pohlavek měl mnohem zdárnější účinek na má játra a mnohem hbitěji povzbudil mou chuť skočit sem nebo tam a bez dalšího otálení udělat, co se po mně chtělo, než dneska celá krabička pilulek.
A to je častý zjev, poslechněte - tyhle obyčejnské, staromódní medicíny jsou kolikrát účinnější než všechny serepetičky z lékáren.
A tak jsme seděli aspoň půl hodiny a popisovali si navzájem svoje nemoci. Já jsem líčil Georgeovi a Williamu Harrisovi, jak mi je, když ráno vstávám, a William Harris nám vykládal, jak mu je, když jde večer spát; a George stál na rohožce před krbem a vtipně a působivě nám mimicky předváděl, jak jemu je v noci.
George si ovšem jenom namlouvá, že je nemocný; ve skutečnosti mu - mezi námi - nechybí vůbec nic.
V tom okamžiku nám zaklepala na dveře paní Poppetsová; chtěla vědět, jestli už máme chuť na večeři. Smutně jsme se jeden na druhého pousmáli a odpověděli jsme, že bychom se snad měli pokusit vpravit do sebe ždibec potravy. Harris řekl, že nějaké to soustečko v žaludku často průběh choroby poněkud zmírní. A tak paní Poppetsová přinesla podnos a my jsme se dovlekli ke stolu a nimrali se ve filátkách na cibulce a v několika kouscích reveňového koláče.
Musel jsem být v té době velice zesláblý; vzpomínám si totiž, že asi po půlhodině nebo tak nějak jsem ztratil všechnu chuť k jídlu - případ u mě neobvyklý - a že jsem už nechtěl ani sýr.
Když jsme tu povinnost měli konečně s krku, naplnili jsme si znova sklenice, zapálili dýmky a opět jsme se rozhovořili o svém zdravotním stavu. Co to s námi doopravdy je, to nemohl s jistotou říci ani jeden z nás; ale jednomyslně jsme došli k náhledu, že ať už máme cokoli, máme to z přepracování.
„Potřebujeme si zkrátka odpočinout,“ prohlásil Harris.
„Odpočinout si a důkladně změnit způsob života,“ dodal George. „Přílišné mozkové vypětí ochromilo celou naši tělesnou soustavu. Když změníme prostředí a nebudeme nuceni neustále myslet, zrestaurujeme opět svou duševní rovnováhu.“
George má bratrance, který je v trestním rejstříku veden jako studující medicíny, a po něm se pochopitelně vyjadřuje tak trochu jako rodinný lékař.
Já jsem s Georgem souhlasil a navrhoval jsem, abychom si vyhledali nějaké odlehlé, starosvětské místečko, daleko od běsnícího davu, a abychom tam prosnili slunečný týden v ospalých alejích - nějaký polozapomenutý koutek, který si někde mimo dosah hlučného světa schovaly víly - nějaké dravčí hnízdo, důmyslně přilepené na útesech času, kde by převalující se vlnobití devatenáctého století bylo slyšet jen slabounce a z velikých dálav.
Harris řekl, že to by podle jeho názoru byla pěkná otrava. Zná prý taková místa, jaká mám na mysli; kdekdo tam chodí spat už v osm, ani za peníze, ani za dobré slovo tam člověk nesežene Milovníka sportu, a pro kuřivo aby jeden chodil patnáct kilometrů.
„Kdepak,“ řekl Harris, „když chcete odpočinek a změnu, tak není nad výlet po moři.
Já jsem proti výletu po moři vehementně protestoval. Výlet po moři vám udělá dobře, když na něj máte pár měsíců, ale na jeden týden je to nepředloženost.
Vyplujete v pondělí, v nitru pevně zakořeněnou představu, jak si užijete. S lehkým srdcem zamáváte na rozloučenou mládencům na břehu, zapálíte si svou nej mohutnější dýmku a chvástavě si vykračujete po palubě, jako kdybyste byl kapitán Cook, sir Francis Drake a Krištof Kolumbus v jedné osobě. V úterý si říkáte, že jste se nikam plavit neměl. Ve středu, ve čtvrtek a v pátek si říkáte, že by vám bylo líp, kdyby bylo po vás. V sobotu jste už schopen pozřít kapku čistého hovězího bujónu, posadit se na palubu a s mírným, malátným úsměvem hlesnout v odpověď, když se vás dobrosrdeční lidé zeptají, jak se cítíte dneska. V neděli už zase můžete udělat pár kroků a sníst trochu hutné stravy. A v pondělí ráno, když s kufrem a deštníkem v ruce stojíte na boku lodi u zábradlí a máte už vystoupit na břeh, začíná se vám to ohromně líbit.
Vzpomínám si, jak můj švagr se jednou vypravil na krátkou cestu po moři, aby si upevnil zdraví. Zajistil si kabinu z Londýna do Liverpoolu a zpět; a když dorazil do Liverpoolu, měl jen jedinou snahu: prodat ten zpáteční lístek.
Nabízel ho prý s ohromnou slevou po celém městě a nakonec ho za osmnáct pencí prodal nějakému mládenci, který vypadal, jako kdyby měl žloutenku, a jemuž lékaři doporučili, aby jel k moři a hodně se pohyboval.
„Moře!“ zvolal můj švagr, lýskyplně mu tiskna lístek do dlaně. „Jéje, toho si takhle užijete, že vám to vystačí na celý život. A co se pohybu týče? Když na téhle lodi budete jenom sedět, tak budete mít víc pohybu, než kdybyste na souši dělal kotrmelce.“
On sám - můj švagr totiž - jel zpátky vlakem. Prohlásil, že pro nějo je až dost zdravá severozápadní dráha.
Jiný člověk, kterého jsem znal, se zase vydal na týdenní plavbu podél pobřeží, a než odrazili od břehu, přišel se ho zeptat stevard, jestli bude platit po každém jídle zvlášť, nebo jestli si přeje celé stravování rovnou předplatit.
Stevard doporučoval to druhé řešení, přijde prý o mnoho laciněji. Za celý týden to prý bude dělat dvě libry pět šilinků. K snídani prý bývá ryba a pak nějaké rožněné maso. Oběd je v jednu a skládá se ze čtyř chodů. Večeře v šest - polévka, ryba, předkrm, porce masa nebo drůbeže, salát, sladkosti, sýr a ovoce. A v deset je lehká masitá večeře na noc.
Můj přítel usoudil, že by se měl rozhodnout pro to předplatné (on je pořádný jedlík), a taky se pro ně rozhodl.
Oběd se podával, sotva odpluli z Sheernessu. Můj přítel neměl takový hlad, jaký myslel, že mít bude, a tak se spokojil s kousíčkem vařeného hovězího a několika jahůdkami se šlehačkou. Odpoledne byl značně zadumaný; chvílemi měl pocit, jako by už dlouhé týdny nejedl nic jiného než vařené hovězí, a chvílemi si zase říkal, že se určitě už dlouhá léta živí jedině jahodami se šlehačkou.
A ani to hovězí ani ty jahody se šlehačkou nedělaly spokojený dojem. Chovaly se spíš rozmrzele.
V šest přišli tomu mému příteli oznámit, že se podává večeře. Ta zpráva v něm nevzbudila pažádné nadšení, uvědomoval si však, že by bylo záhodno odjíst něco z těch dvou liber a pěti šilinků, a tak se přidržoval lan a všeho možného a sestoupil do podpalubí. Na nejspodnějším schůdku ho uvítala lahodná vůně cibule a horké šunky, smíšená s vůní smažených ryb a zeleniny; a tu už k němu přistoupil stevard a se servilním úsměvem se zeptal:
„Co mohu pánovi nabídnout?“
„Nabídněte mi rámě a vyveďte mě odtud,“ zněla mdlá odpověď.
A tak s ním honem vyběhli nahoru, na závětrné straně ho zaklesli do zábradlí a tam ho nechali.
Celé příští čtyři dny vedl prostý a bohulibý život pouze ve společnosti lodních sucharů (myslím tím opravdové suchary, nikoli členy lodní posádky, kteří měli všichni veliký smysl pro humor) a sodovky. Ale takhle k sobotě mu zase narozsl hřebínek a zašel si do jídelny na slabý čaj s toastem bez másla a v pondělí se už přecpal řídkým kuřecím vývarem. V úterý vystoupil z lodi, a když se plnou parou vzdalovala od přístavního můstku, zíral na ní pln lítosti.
„To se jí to pluje,“ říkal si. „To se jí to pluje, když si odváží za dvě libry jídla, které patří mně a z kterého jsem nic neměl!“
Tvrdil, že kdyby mu byli přidali jeden jediný den, byl by si ten dluh zinkasoval.
A proto já jsem byl proti výletu po moři. Nikoli kvůli sobě, to jsem řekl rovnou. Mně nanic nebývá. Ale měl jsem starost o Geortge. George zas tvrdil, že jemu by se zle neudělalo, a že by se mu to dokonce líbilo, ale mně a Harrisovi že to nedoporučuje, my dva že bychom to určitě odnesli. A Harris prohlásil, že co se jeho týče, pro něho je to odjakživa učiněná záhada, jak se někomu může podařit dostat mořskou nemoc - on prý je přesvědčen, že to lidi schválně předstírají, aby vypadali zajímavě - on prý si už mockrát přál mořskou nemoc dostat, ale nikdy to nedokázal.
A potom nám vyprávěl historky, jak se plavil přes Kanál, když se moře tak divoce vztekalo, že cestující museli být připoutáni k lůžkům, a jak on a kapitán byli jediné dvě živé bytosti na palubě, kterým nic nebylo. Pak zas to byl on a druhý kormidelník, kterým nic nebylo. Prostě vždycky to byl on a ještě někdo. A když to nebylo on a ještě někdo, tak to byl on sám.
To je stejně divná věc, že nikdo nedostane mořksou nemoc - na suché zemi. Na moři, tam vidíte spousty lidí, kterým je hrozně zle, tam jsou jich plné lodě; ale na suché zemi jsem ještě nepotkal člověka, který by věděl, co je to mít mořskou nemoc. Kde se ty tisíce a tisíce lidí, co nesnášejí plavbu po moři a co se jimi každá loď zrovna hemží, schovávají, když jsou na suché zemi, to je namouduši záhada.
Kdyby se jich většina podobala tomu chlapíkovi, kterého jsem jednou viděl na lodi do Yarmouthu, pak bych tu zdánlivou šarádu dovedl rozlousknout docela snadno. Jen jsme odrazili od southendské přístavní hráze, už se ten člověk nebezpečně vykláněl z jednoho okénka na boku. Běžím k němu, abych ho zachránil.
„Hej, nevyklánějte se tolik!“ povídám a tahám ho zpátky za rameno. „Sletíte do vody!“
„Ach bóže, už abych tam radši byl!“ To byla jediná odpověď, kterou jsem z něho dostal. A tak jsem ho nechal být.
Za tři neděle jsem ho uviděl v kavárně jednoho hotelu v Bathu. Vyprávěl o svých cestách a nadšeně vykládal, jak miluje moře.
„Mně prostě to houpání vůbec nic nedělá,“ pravil zrovna v odpověď na závistivý dotaz jakéhosi dobře vychovaného mladíka. „Pravda ovšem je, že jednou, jedinkrát mě bylo kapánek divně, to přiznávám. U Hornova mysu. To ráno potom naše loď ztroskotala.“
„A nebylo vám jednou trošku nanic u southendského mola?“ zeptal jsem se ho. „Tam jste si přece přál, aby vás radši hodili do moře.“
„U southendského mola?“ opáčil a tvářil se, jako by nevěděl, oč jde.
„Ano. Při plavbě do Yarmouthu. V pátek to byly tři neděle.“
„Á! No ovšem,“ zvolal v šťastném osvícení. „Už si vzpomínám. To mě tlačil žaludek, tenkrát odpoledne. To po těch kyselých okurkách, poslechněte. Na žádné slušné lodi jsem jakživ nejedl tak mizerné kyselé okurky. Vy jste si je taky dal?“
Já jsem si proti mořské nemoci vymyslel sám pro sebe znamenitý preventivní prostředek: já se kolébám. To se postavíte doprostřed paluby, a jak se loď kymácí a houpe, pohybujete tělem tak, abyste pořád stáli svisle. Když se zvedá příď, nakláníte se celým tělem dopředu, až se nosem skoro dotknete paluby; a když jde nahoru záď, tak se nakláníte dozadu. Hodinu nebo dvě to jde moc dobře, celý týden se ovšem takhle kolébat nemůžete.
Najednou povídá Jiří: „Tak pojeďme na řeku.“
Tam bychom měli čistý vzduch, povídá, pohyb i klid; ustavičná změna scenérie by zaujala našeho ducha (i to, co místo toho má Harris) a po té tělesné námaze bychom s chutí jedli a výtečně spali.
Harris na to řekl, že podle jeho mínění by George neměl dělat nic takového, co by v něm probouzelo ještě větší chuť spát, než jakou má v jednom kuse, jelikož to by už pro něho bylo nebezpečné. Nedovede si prý dobře představit, jak by George dokázal prospat ještě víc času, než prospí teď, když přece den má pouze čtyřiadvacet hodin, a to jak v létě tak v zimě. Ale kdyby to přece jen dokázal, pak už by to prý bylo totéž, jako kdyby byl mrtvý, a tím by aspoň ušetřil za byt a za stravu.
Pak ale Harris dodal, že přesto přese všecko by mu řeka seděla. Já sice nevím, jak by řeka mohla sedět, ledaže by seděla modelem, a ani potom mi není jasné, na čem by seděla, ale mně řeka seděla taky, a tak jsme oba, Harris i j á, řekli, že to je od George výborný nápad, a řekli j sme to tónem, z kterého byl dost j asně znát náš údiv nad tím, že se z George najednou vyklubal člověk tak rozumný.
Jediný, komu ten návrh nepadl do noty, byl Montmorency. Jemu se ovšem řeka nikdy moc nezamlouvala, jemu opravdu ne.
„Copak vy, mládenci, vy si tam na své přijdete,“ povídá, „vy máte řeku rádi, ale já ne. Co já tam budu dělat? Scenérie, to pro mě není nic a kuřák nejsem. A když uvidím myš, tak mi nezastavíte; a když se mně bude chtít spát, tak budete s lodí všelijak blbnout a shodíte mě do vody. Jestli se ptáte na moje mínění, tak já to považuju za úplnou pitomost.“
Ale byli jsme tři proti jednomu, a tak byl ten návrh přijat.
Rozvíjíme plány - Požitek z táboření pod širým nebem za krásných nocí - Dto za deštivých nocí - Rozhodujeme se pro kompromis - Montmorency a první dojmy z něho - Obavy, jestli není příliš dobrý pro tento svět, obavy to, vzápětí odsunuté jako neopodstatněné - Jednání se odročuje.
II
Vytáhli jsme mapy a rozvíjely plány.
Dohodli jsme se, že vyrazíme příští sobotu z Kongstonu. Harris a já tam pojedeme hned ráno a loď dopravíme do Chertsey a George, který se může utrhnout ze zaměstnání až odpoledne (chodí denně od deseti do čtyř spát do jedné banky, denně s výjimkou soboty, kdy ho vždycky probudí a vyšoupnou ven už ve dvě), se s námi sejde až tam.
Máme „tábořit pod širým nebem“, nebo spát v hostincích?
George a já jsme hlasovali pro táboření pod širým nebem. Říkali jsme, že by to bylo takové romantické a nespoutané, takové skoro patriarchální.
Ze srdcí chladných, smutných mraků zvolna vyprchává zlatá vzpomínka na zemřelé slunce. Ptáci už nezpívají, ztichli jako truchlící děti a jen plačtivý křik vodní slípky a chraptivé vrzání chřástala polního čeří posvátnou hrůzou strnulou tišinu nad vodním lůžkem, na němž umírající den dýchá z posledního.
Z potemnělých lesů na obou březích se bezhlučným, plíživým krokem vynořuje stradšidelná armáda noci, ty šedé stíny, které mají zahnat stále ještě otálející zadní voj světla; tichýma, neviditelnýma nohama postupují nad vlnící se trávou při řece a skrze vzdychající rákosí; a noc na chmurném trůnu rozkládá své černé perutě nad hasnoucím světlem a ze svého přízračného paláce, ozářeného sinalými hvězdami, kraluje v říši ticha.
A my zajíždíme s lodičkou do nějaké poklidné zátočinky, rozbíjíme tábor a vaříme a jíme střídmou večeři. Pak si nacpáváme a zapalujeme statné dýmky a melodickým polohlasem rozpřádáme příjemný potlach; a do zámlk v našem hovoru nám řeka, dovádějící kolem lodi, šplouchá podivuhodné starobylé zkazky a starobylá tajemství a tichounce zpívá tu starobylou dětskou písničku, kterou už zpívá tolik tisíc let - a kterou tolik tisíc let ještě zpívat bude, než jí vysokým věkem ochraptí hlas -, tu písničku, o níž si my, kteří jsme se naučili milovat na řece její proměnlivou tvář, my, kteří tak často hledáme útočiště na jejích pružných ňadrech, myslíme, že jí rozumíme, i když bychom nedokázali vyprávět pouhými slovy obsah toho, čemu nasloucháme.
A tak tam sedíme, nad lemem té řeky, a měsíc, který ji má taky rád, sestupuje níž, aby jí dal bratrské políbení a objal ji svýma stříbrnýma pažema a přitulil se k ní. A my se na ni dáváme, jak plyne, věčně ševelící, věčně rozezpívaná, na schůzku se svým králem, s mořem - až se naše hlasy ztratí v mlčení a až nám vyhasnou dýmky - až i my, mládenci tak všední, tak tuctoví, máme najednou neuvěřitelný pocit, že jsme plni myšlenek, napůl rozteskňujících, napůl rozjařujících, a nechceme už a ani nepotřebujeme mluvit - až se zasmějeme, vstaneme, vyklepáme z vyhaslých dýmek popel, řekneme si „dobrou noc“ a ukolébáni šuměním vody a šelestěním stromů usneme pod velkolepými tichými hvězdami a máme sen, že země je zase mladá - mladá a líbezná, jaká bývala, než jí staletí rozhořčení a starostí zbrázdila sličnou tvář, než jí to milující slunce zestárlo nad hříchy a zpozdilostmi jejích dětí - líbezná, jako by v těch dávných dnech, kdy jako nezkušená matka nás, své dítky, živila z hlubokosti svých prsů - než nás svody nalíčené civilizace odlákaly z její schovívavé náruče a než jsme se kvůli jedovatým posměškům a afektovanosti začali stydět za ten prostý život, co jsme vedli u ní, a za ten prostý, ale důstojný domov, v němž se lidstvo před tolika tisíciletími zrodilo.
„A co když bude pršet?“ povídá Harris.
Harrise jakživi ničím nedojmete. V Harrisovi není kouska poetičnosti - ani ždibec divého prahnutí po nedosažitelnu. Harris jakživ „nepláče, nevěda proč“ Má-li Harris v očích slzy, můžete se vsadit, že buď zrovna snědl syrovou cibuli, nebo si nakapal na kotletu příliš mnoho worcesterské omáčky.
Kdybyste s Harrisem stanuli v noci na mořském břehu a zašeptali:
„Zmlkni! Což neslyšíš? To nemůže být nic jiného než mořské panny pějící v hlubinách těch zvlněných vod. Či to truchlící duše lkají žalozpěvy nad svými umrlými těly, zachycenými ve spleti chaluh?“
Harris by vás vzal pod paží a řekl:
„Já ti povím, co to je, kamaráde. Leze na tebe rýma. Pojď hezky se mnou, já znám tadyhle za rohem podniček, kde dostaneš nejlepší skotskou whisky, jakou jsi kdy ochutnal, a ta tě v cuku letu postaví na nohy.“
Harris totiž vždycky ví o podničku za rohem, kde člověk dostane něco vynikajícího z oblasti nápojů. Jsem přesvědčen, že kdybyste Harrise potkali v ráji (za předpokladu, že jeho byste tam potkat mohli), hned by vás vítal slovy:
„To jsem rád, kamaráde, že jsi tady. Já tadyhle za rohem našel rozkošný podniček, kde dostaneš nektar - no doopravdy prvotřídní.“
V tom případě, o němž je řeč, když totiž běželo o to táboření pod širým nebem, byl však Harrisův praktický názor velmi vhodnou připomínkou. Tábořit pod širým nebem za deštivého počasí, to není žádná radost.
Je večer. Jste promoklí na kůži, v lodi je dobrých pět centimetrů vody a všecky vaše věci jsou vlhké. Spatříte na břehu místo, kde je o něco málo míň kaluží než na ostatních místech, která jste ohledali, a tak přistanete a vylovíte z lodi stan a dva z vás se ho pokoušejí postavit.
Je prosáklý vodou a těžký a plácá sebou a hroutí se a lepí se vám na hlavu a šíleně vás rozčiluje. A přitom nepřetržitě leje. Postavit stan, to je i za sucha dost obtížné; ale když prší, tak je to práce herkulovská. A ten druhý
chlapík, místo aby vám pomáhal, si z vás očividně dělá blázny. Sotva se vám podařilo tu vaši stranu jakžtakž
vypnout, on vám s ní ze svého konce cukne, a zas je všecko na draka.
„No tak! Co to vyvádíš?“ voláte na něj.
„Co ty to vyvádíš?“ odpovídá on. „Pusť to!“
„Ty tam za to netahej! Vždyť ty se v tom vůbec nevyznáš, ty trumbero!“ křičíte.
Já se v tom vyznám,“ ječí on. „Ty to tam máš pustit!“
„Povídám ti, že se v tom nevyznáš,“ řvete a nejraději byste po něm skočil; škubnete lanem a vytáhnete mu ze
země všecky kolíky.
„Ách, ten pitomec pitomá!“ slyšíte ho cedit mezi zuby. A vzápětí následuje prudké trhnutí - a vyletí kolíky na vaší straně. Odložíte palici a vykročíte kolem stanu, abyste tomu nemehlu pověděl, co si o tom všem myslíte, ale současně i on vykročí, stejným směrem jako vy, aby vám vyložil, jak on se na to dívá. A tak běháte za sebou, pořád dokolečka, a nadáváte si, až se stan sesuje na hromadu a vy se přes jeho trosky kouknete jeden na druhého a oba jedním dechem rozhořčeně zařvete:
„Tady to máš! Co jsem ti říkal?“
A tu váš třetí kamarád, který zatím vybíral z lodi vodu a lil si ji do rukávů a posledních deset minut nepřetržitě klel, začne být zvědav, proč si u všech ďasů takhle hračičkaříte a proč jste ještě nepostavili ten zatracený stan.
Nakonec ho - tak či onak - přece jen postavíte a vynesete z lodi svéí věci. Chtít si rozdělat oheň z dříví, to by byl pokus naprosto beznadějný, a tak si zapálíte lihový vařič a sesednete se kolem toho.
Převládající složkou večeře je dešťová voda. Chléb, to je dešťová voda ze dvou třetin, nesmírně bohaté na dešťovou vodu jsou i taštičky se sekaným hovězím, a máslo, džem, sůl a káva daly dohromady s dešťovou vodou polévku.
Po večeři zjistíte, že máte vlhký tabák, a že si nemůžete zakouřit. Naštěstí máte s sebou láhev nápoje, který požit v náležitém množství rozveseluje a rozjařuje, a ten ve vás natolik obnoví zájem o život, že vás přiměje jít spat.
Načež se vám zdá, že si vám zčistajasna sedl na prsa slon a současně že vybuchla sopka a svrhla vás na dno moře - i s tím slonem, který vám dál klidně spí na prsou. Probudíte se a okamžitě pochopíte, že se vskutku stalo něco strašlivého. V prvním okamžiku máte dojem, že nastal konec světa; pak si uvědomíte, že to se stát nemohlo, ale že se na vás vrhli zloději a vrahové nebo že vypukl požár, a toto mínění vyjadřujete obvyklým způsobem. Pomoc však odnikud nepřichází a vy víte jen jedno: že po vás šlapou tisíce lidí a že se co nevidět udusíte.
Nejste však zřejmě sám, kdo je na tom bledě. Slyšíte přidušené výkřiky, ozývající se zpod vašeho lůžka. Rozhodnete se, že ať se děje co se děje, prodáte svůj život draho, a začnete se zuřivě rvát, rozdávaje doprava i doleva rány a kopance a v jednom kuse přitom divoce ječíte, až konečně někde něco povolí a vy máte najednou hlavu na četstvém vzduchu. Ve vzdálenosti asi půl metru od sebe matně rozeznáte nějakého polooblečeného halamu, který se vás chystá zavraždit, připravujete se na zápas na život a na smrt, ale vtom se vám v mozku rozbřeskne, že je to Jim.
„Á ták to jsi ty!“ povídá, neboť vás v tom okamžiku taky poznal.
„No!“ odvětíte a protíráte si oči. „Co se stalo?“
„Ale ten zatracený stan se zřítil,“ povídá Jim. „Kde je Bill?“
Pak oba hlasitě hulákáte „Bille!“ a pod vámi se začne vzdouvat a zmítat půda a z těch zřícenin se ozývá ten přiškrcený hlas, který jste už před chviličkou slyšeli:
„Přestaň se mi, ksakru, válet po hlavě!“
A ven se vyškrábe Bill, zablácený, pošlapaný lidský vrak, a zcela bezdůvodně má útočnou náladu - zjevně se domnívá, že to všechno jste udělali jemu naschvál.
Ráno ani jednomu z vás tří není valně do řeči, neboť jste se v noci strašně nachladili; kvůli každé maličkosti se hned hádáte a po celou snídani jeden na druhého sípavým chrapotem nadáváte.
Proto jsme se rozhodli, že venku budeme spát jen za pěkných nocí; a když bude pršet nebo když zatoužíme po změně, přespíme jako pořádní lidé v hotelu, v hostinci nebo v krčmě.
Montmorency přivítal tento kompromis s velikým uspokojením. On si v romantické samotě nelibuje. On horuje pro místa hlučná; a je-li to tam kapánek obhroublé, tím víc se mu tam líbí. Kdybyste Montmorencyho viděli, určitě byste měli dojem, že je to andílek, který z nějakého člověčenstvu utajeného důvodu byl seslán na zem v podobě malého foxteriéra. Montmorency má takový ten výraz, říkající „Ach, ten svět je tak zkažený a já bych si tolik přál ho zlepšit a zušlechtit,“ takový ten výraz, jaký dovede vehnat slzy do očí zbožných starých dam a pánů.
Když se ke mne přistěhoval, aby žil na mé útraty, nevěřil jsem, žře se mi podaří uchovat ho při životě. Sedával jsem u něho a pozoroval ho, jak leží na dece a vzhlíží ke mně, a říkával jsem si: „Ach jé, tenhle pejsek tady dlouho nebude. Toho mi jednou unesou v nebeské káře k janým výšinám, nic jiného ho nečeká.“
Ale když jsem pak zaplatil dobrého půl tuctu kuřat, která roztrhal; když jsem ho vrčícího a rafajícího vyvlekl za zátylek ze sto čtrnácti pouličních rvaček; když mi jedna rozběsněná ženská přinesla ukázat mrtvou kočku a říkala mi vrahu; když mě soused, co bydlí ob dům, pohnal před soud, poněvadž prý nechávám volně pobíhat krvelačného psa, který ho za chladné noci zahnal do jeho vlastní kůlny a přes dvě hodiny mu nedovolil vystrčit ze dveří nos; a když jsem se dověděl, že náš zahradník se vsadil, že Montmorendy zakousne ve stanovené době tolik a tolik krys, a vyhrál třicet šilinků, začal jsem věřit, že ten můj pejsánek bude přece jen ponechán na světě o něco déle.
Potloukat se kolem stájí, shromažďovat hordy těch nejvykřičenějších hafanů, jaké lze ve městě sehnat, pochodovat v jejich čele po perifériích a vodit je tam do boj ů s dalšími vykřičenými hafany - task si Montmorency
představuje „život“. A proto, jak jsem prve poznamenal, schválil tak emfaticky ten návrh týkající se krčem, hostinců a hotelů.
Když jsme takto k spokojenosti všech nás čtyř rozřešili problém noclehů, zbývalo už pouze projednat, co všechno si vezmeme s sebou; i zahájili jsme o tom rozepři, když tu Harris prohlásil, že on se už toho na jeden večer nařečnil až dost, a navrhl, abychom si někam vyšli a rozjasnili tváře; objevil prý kousek za náměstím podniček, kde si můžeme dát kapku irské, která stojí za ochutnání.
George řekl, že on žízeň má (co ho znám, tak jaktěživ neřekl, že žízeň nemá), a ježto já měl takovou předtuchu, že trocha whisky, pěkně teplé, s plátečkem citrónu, mi značně uleví v mé nevolnosti, byla debata za všeobecného souhlasu odročena na příští večer a celé shromáždění si nasadilo klobouky a vyšlo z domu.
Děláme přípravy - Harrisova pracovní metoda - Jak postarší hlava rodiny věší obraz - George má rozumnou připomínku - Požitek z koupele za časného jitra - Opatření pro případ, že by se loď převrhla.
III
A tak jsme se příští večer opět sešli, abychom prohovořili a projednali své plány. Harris řekl:
„V první řadě se musíme dohodnout, co si vezmeme s sebou. Ty si vem kousek papíru, Jerome, a piš, ty Georgi, ty si vem k ruce katalog z toho obchodu se smíšeným zbožím a mně někdo půjčte tužku a já potom sestavím seznam.“
To je celý Harris - ochotně vezme sám na sebe bířmě veškeré práce a vloží je na bedra těch druhých.
Vždycky mi připomene chudáka strýce Podgera. V životě jste neviděli takový blázinec po celém domě, jako když se můj strýc Podger pustí do nějaké práce. Od rámaře přijde obraz, stojí opřen o stěnu v jídelně a čeká, až ho někdo pověsí. Teta Podgerová se zeptá, co s ním, a strýc Podger řekne:
„Á, to nechte na mně. S tím si nikdo z vás nedělejte starosti. To všechno zařídím sám.“
A sundá si sako a dá se do toho. Služku pošle koupit za šest pencí hřebíků, a vzápětí za ní žene jednoho z podomků, aby jí řekl, jak ty hřebíky mají být dlouhé. A tak postupně zaměstná a uvede v chod celý dům.
„Ty jdi pro kladivo, Wille,“ volá, „a ty mi přines pravítko, Tome; a pak budu potřebovat štafle a taky bych tu měl mít nějakou kuchyňskou stoličku; a Jime! ty skoč k panu Gogglesovi a řekni mu: ,Tatínek se dává pěkně poroučet a doufá, že s tou nohou to už máte lepší, a půjčil byste mu laskavě libelu?‘ A ty tady zůstaň, Marie, někdo mně bude muset posvítit. A až se vrátí to děvče, tak musí ještě doběhnout pro kus pořádného špagátu. A Tome! - kde je Tom? - pojď sem, Tome, ty mi ten obraz podáš.“
Potom obraz zvedne, upustí ho, obraz vyletí z rámu, strýc se snaží zachránit sklo a pořeže se; načež poskakuje po pokoji a hledá svůj kapesník. Ale nemůže ten kapesník najít, protože ho má v kapse saka, které si sundal, a nemá ponětí, kam si to sako dal, a tak celá domácnost musí nechat shánění nářadí a dát se do shánění strýcova saka, a strýc zatím tancuje po celém pokoji a kdekomu se plete pod nohy.
„Copak v celém baráku není ani jediná živá duše, která by věděla, kde to moje sako je? V životě jsem neviděl takovou hromadu budižkničemů, na mou duši, že ne. Je vás šest! - a nejste s to najít sako, které jsem si sundal ani ne před pěti minutami! To vám teda řeknu, že ze všech...“
Zvedne se, zjistí, že si na tom saku seděl, a křičí:
„No, teď už hledat nemusíte, už jsem to sako našel sám. Od vás tak chtít, abyste mi něco našli! To bych to rovnou mohl chtít na naší kočce! “
A když se po půlhodině, věnované obvazování strýcova prstu, sežene nové sklo a snesou se všechny nástroje a štafle a stolička a svíčka, a celá rodina i se služkou a s posluhovačkou stojí v půlkruhu připravena pomáhat, strýc se znovu pustí do díla. Dva mu musejí držet stoličku, třetí mu na ni pomůže vylézt a dává mu tam záchranu, čtvrtý mu podává hřebík, pátý k němu zvedá kladivo a strýc sáhne po hřebíku a upustí ho.
„No prosím,“ řekne dotčeným tónem, „a hřebík je v tahu!“
A my musíme všichni na kolena a plazit se a hledat, zatímco on stojí na stoličce a remcá a přeje si vědět, jestli ho tam míníme nechat stát celý večer.
Hřebík je konečně na světě, ale strýc zase ztratil kladivo.
„Kde mám kladivo? Kam já j sem to kladivo dal? Kristepane! Čučí vás na mě sedum a ani j eden z vás neví, kam jstem dal kladivo! “
Vypátráme kladivo, ale on zase najednou ne a ne najít na stěně znamínko, kam se má zatlouci hřebík, a my všichni musíme jeden po druhém vylézt k němu na stoličku a snažit se to jeho znamínko objevit. Každý je vidíme někde jinde a on nám postupně všem vynadá, že jsme pitomci a ať radši slezeme dolů. A pak se chopí pravítka a stěnu přeměří a zjistí, že od rohu to má dělat polovinu ze sedmdesáti sedmi centimetrů a devíti a půl milimetru, a pokouší se to vypočítat z hlavy, a to ho dožene div ne k nepříčetnosti.
Pak se to i my pokoušíme vypočítat z hlavy, každému vyjde něco docela jiného a jeden druhému se pošklebujeme. Načež ve všeobecné vřavě upadne v zapomenutí původní číslo a strýc Podger musí měřit znova.
Tentokrát si na to vezme kus provázku a v kritickém okamžiku, když se z té své stoličky vyklání v pětačtyřicetistupňovém úhlu do strany a snaží se dosáhnout bodu ležícího o sem a půl centimetru dále, než kam
vůbec může dosáhnout, provázek se mu vysmekne z prstů a ten blázen stará uklouzne a zřítí se na otevřené piáno, a tím, jak znenadání třískne hlavou i tělem do všech kláves současně, vyloudí vskutku pěkný hudební efekt.
A teta Marie prohlásí, že nedovolí, aby děti poslouchaly takové výrazy.
Posléze si strýc Podger znovu označí příslušné místo, levou rukou na ně nasadí špičku hřebíku a pravou rukou se chopí kladiva.
Hned prvním úderem si rozmačká palec a s vřískotem upustí kladivo na prsty něčí nohy.
Teta Marie vysloví mírným hlasem naději, že příště jí snad strýc Podger zavčas oznámí, kdy zas bude zatloukat do zdi hřebík, aby se mohla domluvit s matkou a strávila ten týden u ní.
„Ách, vy ženské! Vás všecko hned vyvede z míry!“ odvětí strýc Podger, sbíraje se na nohy. „To já, já tyhle drobné domácí práce dělám rád.“
A poté zahájí další pokus; při druhém úderu proletí hřebík skrze celou omítku a půlka kladiva za ním a strýc Podger naletí tak prudce na zeď, že si málem rozplácne nos.
I musíme opět vyhledat pravítko a provázek a ve zdi vznikne nová díra; obraz visí až někdy k půlnoci - značně nakřivo a nespolehlivě - stěna na metry kolem dokola vypadá, jako kdyby po ní byl někdo jezdil hráběmi, a kdekdo je k smrti utahaný a umlácený - jen strýc Podger ne.
„No - a je to!“ praví, těžce sestoupí se stoličky na kuří oko naší posluhovačky a obhlíží spoušť, kterou natropil, s očividnou pýchou. „A to prosím existujou lidi, kteří by si na takovou maličkost někoho zjednali!“
A Harris, to já vím, bude zrovna taková, až bude starší. Však jsem mu to taky řekl. A prohlásil jsem, že nepřipustím, aby si sám nabral tolik těžké práce.
„Ne, ne! Ty si vezmeš papír a tužku a ten katalog, George bude zapisovat a tu hlavní práci udělám já.“
První seznam, který jsme sestavili, jsme museli zahodit. Bylo nám jasné, že horním tokem Temže by neproplulo plavidlo tak velikánské, aby mohlo pobrat všechny věci, které jsme uvedli jako nepostradatelné; a tak jsme ten seznam roztrhali a podívali j sme se jeden na druhého.
A George povídá:
„Poslouchejte, my na to jdeme úplně špatně. Nesmíme myslet na věci, které s sebou mít chceme, ale jenom na věci, které s sebou mít musíme “
Čas od času se George projeví docela rozumně. Což člověka překvapí. A tomuhle říkám přímo moudrost, nejen s ohledem na ten tehdejší případ, ale i se zřetelem na celou naši plavbu po řece života. Kolik lidí si na tuhle cestu přetíží loď, že se s nimi div nepotopí pod nákladem hloupostí, o nichž si leckdo myslí, že jsou pro zábavu a pohodlí na tom výletu nepostradatelné, které však ve skutečnosti nejsou nic jiného než bezúčelné haraburdí!
Až po vršek stěžně přecpou ten ubohý korábek skvostnými obleky a velikými domy; zbytečným služebnictvem a davem nastrojených přátel, kteří o ně ani za mák nestojí a o které oni sami nestojí ani za máček; nákladnými radovánkami, při nichž se nikdo nebaví, okázalostmi a obřadnostmi, konvencemi a předstíráním a hlavně - a to jsou ty nejtěžší, nejnesmyslnější kusy toho haraburdí! - strachem, co si pomyslí soused, přepychem, který se už přejídá, zábavičkami, které už nudí, nicotnou parádou, pod níž - jako pod tou železnou korunou nasazovanou za dávných časů zločincům - obolavěná hlava krvácí a klesá do bezvědomí.
To je haraburdí, člověče, samé haraburdí! Hoď to všecko přes palubu! S tím je loď tak těžká, že u vesel div neomdlíš. S tím je tak nemotorná, její řízení tě přivádí do tolika nebezpečí, že si ani na okamžik neoddychneš od úzkostí a starostí, ani na okamžik si nemůžeš odpočinout v lenivém zasnění - nemáš kdy se zahledět na prchavé stíny, ve vánku lehce těkající nad mělčinami, na sluneční paprsky, jiskřivě prosakující mezi vlnkami, na vznešené stromy na břehu, shlížející na své vlastní podoby, na lesy, které jsou samá zeleň a samé zlato, na lekníny a stulíky, na zádumčivě se vlnící rákosí, na houštiny ostřice, na vstavače, na modré pomněnky.
To haraburdí hoď přes palubu, člověče! Ať loď tvého života je lehká, naložená jenom tím, co nutně potřebuješ - domovem, kde jsi doopravdy doma, prostými radostmi, jedním nebo dvěma přáteli, hodnými toho označení, někým, koho můžeš milovat a kdo může milovat tebe, kočkou, psem, dýmkou - nebo i dvěma -, dostatečnou zásobou jídla a dostatečnou zásobou šatstva - a o něco víc než dostatečnou zásobou pití; neboť žízeň je věc nebezpečná.
Na takové lodi se ti pak bude snadněji veslovat, ta se s tebou hned tak nepřevrhne, a když se převrhne, tak to nebude taková pohroma; dobrému, solidnímu zboží voda moc neuškodí. A budeš mít čas na přemýšlení, zrovna tak jako na práci. Čas nalokat se slunečního jasu života - čas zaposlouchat se do té eolské hudby, kterou boží vítr vyluzuje ze strun lidských srdcí kolem nás - čas...
Jé, moc se omlouvám. Dočista jsem zapomněl.
No tak j sme ten seznam přehráli na George, a on ho začal sepisovat.
„Stan si s sebou brát nebudeme,“ prohlásil, „opatříme si na loď natahovací střechu. To bude o moc jednodušší a taky pohodlnější.“
Ten nápad se nám zamlouval a byli jsme pro. Nevím, jestli jste to, o čem teď mluvím, už někdy viděli. To překlenete loď železnými žebry, zahutými do oblouků, přes ně napnete velikánskou plachtu, tu dole připevníte k oběma bokům po celé jejich délce, a tím vlastně loď proměnte v takový domeček, kde je krásně útulno, i když kapánek dusno. Jenomže všechno na světě má svou stinnou stránku, jak poznamenal ten chlapík, co mu umřela tchýně, když na něj přišli, aby zaplatil útraty na její pohřeb.
George řekl, že v tom případě musíme mít s sebou tři deky, lampu, nějaké mýdlo, kartáč a hřeben (společný), kartáček na zuby (každý svůj), zubní pasty, holicí náčiní (to je, jako když se učíte slovíčka na hodinu
francouzšitny, viďte?) a dvě veliké osušky ke koupání. Často si všímám, jaké se dělají gigantické přípravy na koupání, když lidi jednou někam k řece, ale jak se potom, když tam jsou, moc nekoupou.
To je zrovna tak, jako když jedete k moři. Já si pokaždé umiňuju - když o tom přemýšlím ještě v Lonýdně -, že si denně ráno přivstanu a půjdu se namočit do moře ještě před snídaní, a vždycky si zbožně zapakuju plavky a osušku. Kupuju si výhradně červené plavky. Červené plavky se mi líbí nejvíc. Výtečně mi jdou k pleti. Ale když potom k tomu moři přijedu, tak můj pocit, že tu časnou ranní koupel tolik potřebuju, není už najednou tak výrazný, jako byl v Londýně. Naopak, spíš mám pocit, že potřebuju zůstat do posledního okamžiku v posteli a pak rovnou sejít dolů ke snídani. Jednou nebo dvakrát ve mně zvítězí čest, vstanu před šestou, trochu se přiobléknu, seberu plavky a ručník a ponuře doškobrtám k moři. Ale rozkoš mi to nepůsobí žádnou. Zřejmě tam chovají zvlášť řezavý východní vítr, který mají nachystaný speciálně pro mě, až se časně ráno půjdu koupat; k té příležitosti taky seberou kdejaký trojhranný kámen, všechny položí hezky navrch, zašpičatí skaliska a ty špice zasypou trochou písku, abych je neviděl, a navíc vezmou moře a posunou je o tři kilometry dál, takže musím, schoulený do svých vlastních zkřížených paží a drkotaje zimou, přebrotit lán patnácticentometrové hloubky. A když konečně dorazím k moři, to moře se chová neurvale a vysloveně mě uráží.
Drapne mě obrovská vlna, složí si mě, že vypadám jako vsedě, a pak nejsurovější silou, jaké je schopna, se mnou práskne na skalisko, které tam dali schválně kvůli mně. A dřív než stačím zařvat „Au! Júúú!“ a zjistit, co mám pryč, už je ta vlna zpátky a smýkne se mnou doprostřed oceánu. Zběsile sebou plácám směrem k pobřeží, pochybuju, že ještě někdy uvidím domov a přátele, a vyčítám si, proč jsem nebyl hodnější na svou sestřičku v klukovských letech (když jsem já byl v klukovských letech, pochopitelně, nikoli ona). A přesně v okamžiku, kdy jsem se vzdal veškeré nadsěje, vlna se dá náhle na ústup a nechá mě rozcabeného jako mořskou hvězdici na mokrém písku; vyškrábu se na nohy, ohlédnu se s vidím, že jsem plaval o život v sotva půlmetrové hloubce. Po jedné noze doskáču na břeh, obléknu se a dopotácím se domů, a tam musím předstírat, že se mi to líbilo.
I v tomto případě jsme všichni vedli řeči, jako kdybychom se chystali každé ráno si pořádně zaplavat. Georte pravil, že není nad to, probudit se za čiperného rána v lodi a hned se ponořit do průhledné řeky. Harris pravil, že nic tak nezvýší chuť k jídlu, jako když si člověk před snídaní zaplave. U něho prý to vždycky zvýší chuť k jídlu. Na to řekl George, že jestli po koupeli spořádá Harris ještě víc jídla než obvykle, pak on, George, musí být ostře proti tomu, aby se Harris koupal.
I tak to prý bude perná dřina, táhnout proti proudu tolik jídla, aby to stačilo pro Harrise.
Já jsem však George přiměl k zamyšlení, oč bude pro nás příjemnější mít v dodi Harrise čistého a svěžího, i kdybychom vážně museli s sebou vzít o pár metráků proviantu víc; a George musel to mé hledisko uznat a svou námitku proti Harrisovým koupelím vzal zpátky.
Posléze jsme se dohodli, že osušky si s sebou vezmeme tři, aby jeden na druhého nemusel čekat.
Pokud šlo o věci na sebe, mínil George, že nám úplně postačí dva flanelové obleky, protože ty si můžeme sami vyprat v řece, až je ušpiníme. Ptali jsme se ho, jestli už někdy zkoušel vyprat v řece flanelový oblek, a Georte odvětil, že on sám to sice nezkoušel, ale že zná pár mládenců, kteří to zkusili, a že to prý byla hračka. A já s Harrisem j sme si bláhově mysleli, že George ví, o čem mluví, a že si tři úctyhodní mladí muži bez vliveného postavení a bez jakýchkoli zkušeností s praním mohou vskutku kouskem mýdla vyprat vlastní košile a kalhoty v řece Temži.
Teprve ve dnech, které byly před námi, v době, kdy už bylo příliš pozdě, jsme se měli dovědět, že George je ničemný podvodník, který o té věci neví zřejmě vůbec nic. Kdybyste ty šaty byli viděli, když jsme je - leč nepředbíhejme, jak se to říká v šestákových krvácích.
George nám rovněž kladl na srdce, abychom si vzali dost rezervního spodního prádla a spoustu ponožek pro případ, že bychom se převrhli a potřebovali se převléknout; a taky spoustu kapesníků, ty že se budou hodit jako utěrky, a kromě lehkých veslařských střevíců ještě jedny vysoké botky z pořádné kůže, ty že taky budeme potřebovat, kdybychom se převrhli.
Otázka stravování - Námitky proti petroleji jako atmosféře - Výhody sýra jako spolucestujícího - Vdaná žena opouští domov - Další opatření pro případ, že bychom se převrhli - Jak balím já - Zavilost kartáčků na zuby - Jak bylí George a Harris - Hrozné chování Montmorendyho - Ukládáme se ke spánku.
IV
Pak jsme vzali na přetřes otázku stravování. George povídá:
„Začneme snídaní.“ (George je velice praktický.) „Tak tedy k snídani budeme potřebovat pánev,“ (Harris řekl, ta že je těžko stravitelná; ale my jsme ho prostě vybídli, aby neblbnul, a George pokračoval) „konvici na čaj, kotlík a lihový vařič.“
„Jen ne petrolej!“ dodal s významným pohledme George a Harris i já jsme přikývli.
Jednou jsme si s sebou vzali vařič petrolejový, ale to ,již nikdy více!“ Ten týden jsme měli dojem, jako bychom žili v krámě s petrolejem. Plechovka s tím petrolejem totiž tekla. A když odněkud teče petrolej, to si teda nepřejte. My jsme tu plechovku měli v samé špici lodi, a ten petrolej tekl odtamtud až ke kormidlu a cestou se vsákl do celé lodi a do všeho, co v ní bylo, a natekl do řeky a prostoupil celou scenérii a zamořil ovzduší. Někdy foukal západní petrolejový vítr, jindy východní petrolejový vítr; ale ať už přicházel od sněhů Arktidy nebo se zrodil v nehostinné písečné poušti, k nám vždycky doletěl stejně prosycený vůní petroleje.
A náš petrolej tekl dál a poničil i západy slunce; dokonce i měsíční paprsky určitě už zaváněly petrolejem.
V Marlow jsme se mu pokusili utéci. Loď jsme nechali u mostu a přes město jsme se vydali pěšky, abychom tomu petroleji unikli; táhl se však za námi. Za chvilku ho bylo plné město. Pustili jsme se přes hřbitov, ale tam zřejmě nebožtíky pochovávali v petroleji. I Hlavní třída smrděla petrolejem; divli jsme se, jak tam lidi dokážou bydlet. A tak jsme ušli kilometry a kilometry po silnici k Birminghamu; ale ani to nebylo nic platné, i ten venkov se koupal v petroleji.
Na konci tohohle výletu jsme se o půlnoci všichni sešli na opuštěné pláni pod takovým olysalým dubem a zakleli jsme se strašlivou přísahou (kleli jsme kvůli tomu neřádu samozřejmě celý týden, ale jenom tak normálně, středostavovsky, kdežto teď to byl úkon slavnostní) - zakleli jsme se tedy strašlivou přísahou, že si už jakživi nevezmeme na loď petrolej.
Proto jsme i tentokrát dali přednost lihu. Ono i s tím je to dost zlé. To máte denaturátovou sekanou a denaturátové pečivo. Jenomže denaturovaný líh, když ho tělu poskytnete větší množství, je o něco výživnější než petrolej.
Jako další pomůcky k snídani navrhoval George vejce a slaninu, což se dá lehce připravit, studená masa, čaj, chléb, máslo a džem. K obědům bychom si prý mohli brát studené pečínky se suchary, chléb s máslem a džemem - nikoli však sýr. Sýr, zrovna tak jako petrolej, se všude cpe do popředí. Chce celou loď jenom pro sebe. Prolézá celým košem s jídlem a všemu ostatnímu v tom koši dává sýrovou příchuť. Těžko uhodnete, jestli jíte jablkový závin nebo párek nebo jahody se šlehačkou. Všecko to připomíná sýr. Sýr má totiž příliš mocný odér.
Vzpomínám si, jak jeden můj přítel zakoupil v Liverpoolu páreček sýrů. Byly to výtečné sýry, vyzrále a uležené, s vůní o síle dvou set koní, na niž bylo možno vystavit záruku, že dostřelí do vzdálenosti čtyř a půl kilometru a na vzdálenost dvou set metrů srazí k zemi člověka. Já byl tehdy taky v Liverpoolu a ten můj přítel se mne přišel zeptat, jestli bych byl tak hodný a vzal mu ty sýry s sebou do Londýna, protože on se tam dostane až za den nebo dva, a myslí si, že ty sýry by se už neměly déle skladovat.
„Ale milerád, kamaráde,“ odpověděl jsem, „milerád.“
Zašel jsem pro ty sýry k němu a odvážel jsem je drožkou. Byla to taková rachotina na rozsypání, tažená dýchavičným náměsíčníkem s nohama do X, o němž se jeho majitel v okamžiku nadšení během konverzace vyjádřil jako o koni. Sýry jsem položil na střechu kabiny a drožka se rozdrkotala tempem, které by sloužilo ke cti nejsvižnějšímu dosud sestrojenému parnímu válci, a tak jsme jeli, vesele jako s umíráčkem, dokud jsme nezabočili za roh. Tam vítr zanesl aróma sýrů přímo k našemu komoni. To ho probudilo; zděšeně zafrkal a vyrazil vpřed rychlostí pěti kilometrů za hodinu. Vítr stále vanul směrem k němu, a než jsme dorazili na konec ulice, dostal už ze sebe rychlost téměř šestikilometrovou, takže invalidy a tělnaté staré dámy nechával za sebou prostě v nedohlednu.
Před nádražím měli co dělat dva nosiči, aby společně s kočím koně zastavili a udrželi na místě; a patrně by se jim to vůbec nebylo podařilo, kdyby jeden z nich nebyl měl tolik duchapřítomnosti, že tomu splašenci zakryl kapesníkem nozdry a honem před ním zapálil kus balicího papíru.
Koupil jsme si jízdenku a hrdě jsem se svými sýry kráčel po nástupišti a lidé se přede mnou uctivlě rozestupovali na obě strany. Vlak byl přeplněn a já se musel vecpat do oddělení, kde už bylo sedm cestujících. Jakýsi nerudný starý pán protestoval, ale já jsem se tam přece jen vecpal; sýry jsem dal nahoru do sítě, s vlídným pousmáním jsem se vtěsnal na lavici a prohodil jsem, že je dnes teploučko. Za několik okamžiků začal ten starý pán nervózně poposedat.
„Je tu nějak dusno,“ řekl.
„Přímo k zalknutí,“ přitakal jeho soused.
Pak oba začali čenichat, při třetím začenichání uhodili hřebík na hlavičku, zvedli se a bez dalších slov odešli. Pak vstala jakási korpulentní dáma, prohlásila, že takovéto týrání počestné vdané ženy považuje prostě za hanebnost, sebrala kufr a osm balíků a rovněž odešla. Zbývající čtyři cestující poseděli až do chvíle, kdy
slavnostně vypadající muž, sedící v koutě a oblečením a celkovým vzezřením vzbuzující dojem, že je příslušníkem kasty majitelů pohřebních ústavů, poznamenal, že mu to jaksi připomíná zesnulé batole; pak se ti tři ostatní cestující pokusili, všichni současně, vyrazit ze dveří a vzájemně se zranili.
Usmál jsem se na toho černého pána a poznamenal jsem, že teď zřejmě budeme mít celé kupé pro sebe; a on se bodře zasmál a řekl, že někteří lidé nadělají spousty cavyků kvůli úplným maličkostem. Ale jen jsme vyjeli, začala i na něho padat podivná sklíčenost, a tak když jsme dorazili do Crewe, pozval jsem ho, aby se se mnou šel napít. Přijal, probojovali jsme se tudíž k bufetu, a tam jsme čtvrt hodiny pokřikovali a dupali a mávali deštníky, načež přišla nějaká slečna a otázala se nás, jestli snad něco nechceme.
„Co vy si dáte?“ zeptal jsem se, obraceje se k svému příteli.
„Velký koňak, slečno. A bez sodovky, prosím,“ odvětil.
A když ho vypil, klidně odešel a nastoupil do jiného vagónu, což mi připadalo nevychované.
Od Crewe jsem měl celé kupé pro sebe, přestože vlak byl stále přeplněn. Na každém nádraží, kde jsme zastavili, si lidé toho prázdného oddělení všimli a hnali se k němu. „No vidíš, Marie! Pojď, tadyhle je místa dost.“ „Dobrá, Tome, nastoupíme sem,“ volali na sebe. A přiběhli s těžkými kufry a přede dveřmi se vždycky prali o to, kdo vleze dovnitř první. A pak vždycky jeden ty dveře otevřel, vystoupil po schůdkách a vyvrávoral pozpátku do náruče toho, co byl za ním. A tak vstupovali jeden po druhém každý začenichal a odpotácel se ven a vtěsnal se do jiného oddělení nebo si doplatil na první třídu.
Z eustonského nádraží jsem sýry dopravil do domu svého přítele. Jeho žena, když vstoupila do pokoje, chviličku čichala a pak řekla:
„Co to je? Nešetřete mě, povězte mi rovnou to nejhorší.“
„Sýry jsou to,“ odvětil jsem. „Tom si je koupil v Liverpoolu a požádal mě, abych mu je sem zavezl.“
A dodal jsem, že ona, jak doufám pochopí, že já jsem v tom nevinně, a ona řekla, že tím je si jista, ale až se vrátí Tom, že si s ním pohovoří.
Tom se musle v Liverpoolu zdržet déle, než očekával, a když se ani třetího dne ještě nevrátil domů, přišla jeho žena ke mně.
„Co Tom o těch sýrech říkal?“ zeptala se.
Odvětil jsem, že nařídil, aby byly uchovány někde ve vlhku a aby na ně nikdo nesahal.
„Nikoho ani nenapadne, aby na ně sahal,“ řekla. „Přivoněl si k nim vůbec?“
Pravil jsem, že pravděpodobně ano, a dodal jsem, že mu na nich zřejmě nesmírně záleželo.
„Takže myslíte, že by ho mrzelo,“ vyzvídala, „kdybych někomu dala zlatku, aby je někam odnesl a zakopal?“ Odpověděl jsem, že by se pravděpodobně už nikdy neusmál.
Tu dostala nápad. A řekla:
„A co kdybyste mu je vzal do opatrování vy? Dovolte, abych je poslala k vám.“
„Madam,“ odvětil jsem, ,já osobně mám vůni sýrů rád a na tok jak jsme s nimi tuhle cestoval z Liverpoolu, budu vždycky vzpomínat jako na šťastné zakončení příjemné dovolené. Leč na tomto světě musíme brát ohled na ostatní. Dáma, pod jejíž střechou mám čest přebývat, je vdova, a pokud je mi známo, patrně též sirota. Ta dáma má mocný, řekl bych až výmluvný odpor k tomu, aby ji někdo, jak to ona sama formuluje, »dožíral«. Přítomnost sýrů vašeho manžela ve svém bytě by určitě, to instinktivně cítím, považovala za »dožírání«. A o mně se bohdá nikdy neřekne, že jsem dožíral vdovu a sirotu.“
„No dobrá,“ řekla žena mého přítele povstávajíc, „pak mohu říci pouze tolik, že seberu děti a budeme bydlet v hotelu, dokud ty sýry někdo nesní. Žít společně s nimi pod jednou střechou, to prostě odmítám.“
Dodržela slovo a domácnost přenechala v péči své posluhovačky, která na otázku, jestli ten zápach přežije, odpověděla „Jakej zápach?“ a která, když ji přivedli až těsně k těm sýrům a řekli jí, aby si k nim pořádně přičichla, prohlásila, že rozeznává slabounkou vůni melounů. Z toho bylo možno usoudit, že této ženě ovzduší bytu nijak zvlášť neublíží, a byla tam tedy ponechána.
Účet za hotel činil patnáct guinejí, a když můj přítel spočítal všechno dohromady, zjistil, že jeho ty sýry přišly na osm a půl šilinku za půl kila. Pravil, že s nesmírnou chutí sní kousek sýra, tohle že je však nad jeho prostředky. A tak se rozhodl, že se těch dvou sýrů zbaví, a hodil je do jednoho průplavu u doků; ale musle je zase vylovit, ježto chlapi z nákladních bárek si stěžovali; prý jim z nich bylo na omdlení. Potom je jedné temné noci odnesl na místní hřbitov a nechal je tam v márnici. Ale přišel na ně ohledač mrtvol a ztropil pekelný randál.
Křičel, že to je úklad, jehož cílem je vzkřísit mrtvoly a jeho tak připravit o živobytí.
Nakonec se můj přítel těch sýrů zbavil tím, že je odvezl do jednoho přímožského města a tam je zakopal na pobřeží. A tím tomu místu zajistil značnou proslulost. Návštěvníci se najednou začali divit, proč si už dřív nepovšimli, jak silný je tam vzuch, a ještě řadu let potom se tam houfně hrnuli souchotináři a vůbec lidé slabí na prsa.
A proto jsem i já, ačkoli sýry velice rád, dal Georgeovi za pravdu, když je odmítl vzít s sebou.
„Taky si odpustíme odpolední svačinu,“ dodal George (načež Harris protáhl obličej), „ale zato si pravidelně v sedm dáme denně pořádnou, vydatnou baštu - prostě svačinu, večeři a sousto na noc v jednom.“
Harris hned vypadal veseleji. George navrhoval masové a jablečné záviny, šunku, studené pečínky, rajčata a hlávkový a okurkový salát a ovoce. K pití jsme si s sebou brali jakousi báječnou lepkavou míchanici podle Harrisova receptu, které, když se rozředí vodou, se dá říkat limonáda, spousty čaje a láhev whisky, pro případ, jak pravil George, že bychom se převrhli.
Podle mého názoru se George k té představě, že se můžeme převrhnout, vracel zbytečně často. To přece nebyl ten pravý duch, jaký měl vládnout našim cestovním přípravám.
Ale že jsme s sebou měli tu whisky, to jsem dodnes rád.
Pivo ani víno jsme si nebrali. To není dobré na řeku. Pak jste ospalí a máte těžké nohy. Takhle navečer, když se jen tak poflakujete po městě a koukáte po holkách, to je nějaká ta sklenička na místě; ale když vám na hlavu praží slunce a čeká vás tvrdá dřina, tak nepijte.
Sepisovali jsme seznam všeho, co se má vzít, a byl z toho seznam hezky dlouhý, než jsme se toho večera rozešli. Nazítří, to byl pátek, jsme to všechno nanosili ke mně a večer jsme se sešli, abychom to zapakovali. Na šatstvo jsme měli obrovský kožený kufr a na poživatiny a kuchařské náčiní dva koše. Stůl jsme odstrčili k oknu, všecky ty věci jsme snesli na jednu hromadu doprostřed pokoje, sedli jsme si kolem té hromady a dívali se na ni.
Já jsem řekl, že to zapakuju sám.
Na to, jak umím pakovat, jsem dost pyšný. Pakování je jedna z těch mnoha věcí, v nichž se vyznám líp než kterýkoli žijící člověk. (Někdy mě samotného překvapí, jak mnoho těch věcí je.) Důtklivě jsem George i Harrise na tuto skutečnost upozornil a radil jsem jim, aby to všechno nechali na mně. Skočili na ten návrh s ochotou, která se mi hned nějak nezamlouvala. George si nacpal dýmku a rozvalil se v lenošce a Harris si dal nohy na stůl a zapálil si doutník.
Ttakhle jsem to ovšem nemínil. Představoval jsem si to - samozřejmě - tak, že já budu při té práci vrchním dohlížitelem a Harris a George že se budou podle mých direktiv motat sem a tam a já že je každou chvíli odstrčím s takovým tím „Jdi ty...!“ nebo „Pusť, já to udělám sám,“ nebo „No prosím, vždyť to není nic těžkého!“ - prostě že je budu tak říkajíc učit. Že si to vyložili tak, jak si to vyložili, to mě popudilo. Nic mě totiž tolik nepopudí, jako když si ti druzí jenom sedí a nic nedělají, zatímco já pracuju.
Jednou jsem bydlel s člověkem, který mě tímhle způsobem doháněl skoro k šílenství. Vždycky když jsem něco dělal, on se povaloval na pohovce a koukal na mě; celé hodiny mě vydržel sledovat očima, ať jsem se v tom pokoji kamkoli pohnul. Říkal, že mu dělá úžasně dobře, když se může dívat, jak pilně markýruju práci. Prý mu to dokazuje, že život není jen jalové snění, určené k prozevlování a prozívání, ale vznešené poslání, plné povinností a tvrdé práce. Často už prý si kladl otázku, jak mohl vůbec existovat, než se seznámil se mnou, když neměl nikoho, koho by mohl pozorovat při práci.
Tak takovýhle já nejsem. Já nevydržím klidně sedět, když vidím, jak se někdo jiný otrocky lopotí. To musím vstát a dozírat na něj, obcházet ho s rukama v kapsách a radit mu, jak na to. To dělá ta moje činorodá povaha. Tomu se prostě neubráním.
Přesto jsem neřekl ani slovo a dal jsem se do toho pakování. Dalo to víc práce, než jsem předpokládal, ale posléze jsem byl hotov s kufrem, sedl jsem si na něj a stáhl jsem ho řemeny.
„A co boty? Ty tam nedáš?“ zeptal se Harris.
Rozhlédl jsem se a zjistil, že na boty jsem zapomněl. To je celý Harris. Ani necekne samozřejmě, dokud kufr nezavřu a nestáhnu ho řemeny. A George se smál - tím svým popuzujícím, bezdůvodným, přiblblým chechotem, při kterém mu huba div nevyletí z pantů a který mě pokaždé rozzuří.
Otevřel jsem kufr a přibalil boty; a potom, právě když jsem se chystal kufr zase zavřít, napadla mě strašlivá myšlenka. Dal jsem tam svůj kartáček na zuby? Nevím, čím to je, ale já vážně nikdy nevím, jestli jsem si dal do kufru kartáček na zuby.
Můj kartáček na zuby, to je věc, která mě při každém cestování v jednom kuse straší a proměňuje mi život v peklo. V noci se mi zdá, že jsem si ho nezapakoval, probudím se zborcen studeným potem, vyskočím z postele a sháním se po kartáčku. A ráno ho zapakuju dřív, než si vyčistím zuby, takže ho zase musím vypakovat, a vždycky ho najdu, až když vytahám z kufru všecko ostatní; pak musím zapakovat znova a na kartáček zapomenu a v posledním okamžiku pro něj pádím nahoru a na nádraží ho vezu v kapse, zabalený do kapesníku.
I teď jsem samozřejmě musel všecko do poslední mrtě zase vypakovat, ale kartáček, samozřejmě, ne a ne najít. Celý obsah kufru jsem vyházel a tak důkladně zpřeházel, že to u mne vypadalo přibližně jako před stvořením světa, když ještě vládl chaos. Georgeův a Harrisův kartáček jsem měl v ruce, samozřejmě! aspoň osmnáctkrát, ale ten svůj jsem nenašel. Tak jsem všecky ty věci házel zpátky do kufru, jednu po druhé, každou jsem zvedl a protřepal. A ten kartáček j sem našel v j edné botě. A pakoval j sem znova.
Když jsem s tím byl hotov, zeptal se George, jestli je tam mýdlo. Řekl jsem mu, na tom že mi pendrek záleží, jestli tam je nebo není mýdlo, kufr jsem zabouchl a stáhl řemeny a zjistil jsem, že jsem si do něho dal pytlík s tabákem a musel jsem ho nanovo otevřít. Definitvně byl zavřen v 10,05 a ještě zbývalo zapakovat koše. Harris poznamenal, že budeme chtít vyrazit už za necelých dvanáct hodin, a že by tedy snad bylo lepší, kdyby si to ostatní vzal na starost on s Georgem; s tím jsem souhlasil, posadil jsem se a do práce se dali oni.
Začali s veselou myslí očividně v úmyslu předvést mi, jak se to dělá. Já se zdržel poznámek; prostě jsem čekal. Až milého George pověsí, nej horším pakovačem na tomto světě bude Harris. Díval jsem se na ty stohy talířů a šálků a konvic a lahví a sklenic a konzerv a vařičů a pečiva a rajčat, atd., a tušil jsem, že brzy to bude vzrušující.
A bylo. Zahájili to tím, že rozbili jeden šálek. To bylo první, co udělali. A udělali to jenom proto, aby ukázali, co všechno by udělat dovedli, a aby vzbudili zájem.
Pak Harris postavil sklenici s jahodovým džemem rovnou na jedno rajče a rozmačkal je, takže je museli z koše vybírat lžičkou.
Pak přišel na řadu George, a ten dupl na máslo. Já nic neřekl, ale přistoupil jsem blíž, sedl si na kraj stolu a pozoroval je. To je popouzelo mnohem víc než všechno, co bych byl mohl říci. To jsem vycítil. Byli ze mne
nervózní a rozčilení, na všecko šlapali, všecko, co vzali do ruky, hned někam zašantročili, a když to pak
potřebovali, tak to nemohli najít; a nákyp dali do koše na dno a těžké předměty navrch, takže z nákypu udělali kaši.
Sůl, tu rozsypali prostě do všeho - a to máslo? V životě jsem si nedovedl představit, že by se dva chlapi dovedli tolik vydovádět s kouskem másla za jeden šilink a dvě pence. Když si je George odlepil z pantofle, chtěli je nacpat do konvičky na mléko. Tam samozřejmě nešlo, a to, co tam z něho přece jen šlo, to zase nešlo ven. Nakonec je kupodivu vyškrábali a odložili je na židli, a tam si na ně sedl Harris a máslo se k němu přilíplo a oba šmejdili po celém pokoji a pátrali, kam se podělo.
„Já bych přísahal, že jsem ho dal tuhle na židli,“ pravil George, zíraje na prázdné sedadlo.
„No jo, já sám jsem viděl, jak ho tam dáváš, to není ani minuta,“ přikyvoval Harris.
A znova se vydali na obhlídku celého pokoje a znova se sešli uprostřed a civěli jeden na druhého.
„To teda je ta nejfantastičtější věc, jakou jsem kdy zažil,“ prohlásil George.
„Úplná záhada!“ dodal Harris.
Pak se najednou George octl Harrisovi za zády a to máslo uviděl.
„Prosím tě, vždyť jeho celou tu dobu máš tadyhle!“ zvolal rozhořčeně.
„Kde?“ zařval Harris a začal se točit dokolečka.
„Copak ty nedovedeš chviličku klidně postát?“ hřměl George, lítaje za ním.
Nakonec to máslo seškrábali a nandali je do čajové konvice.
Všeho toho se, přirozeně, zúčastnil i Montmorency. Montmorencyho životní ctižádostí je totiž plést se lidem pod nohy a provokovat je k nadávkám. Když se mu podaří vklouznout někam, kde je obzvlášť nežádoucí, kde příšerně překáží, kde lidi rozzuří do té míry, že po něm házejí vším, co je po ruce, pak teprve má pocit, že nepromarnil den.
Jeho nejvyšší metou a touhou je docílit, aby se přes něj někdo přerazil a pak ho nepřetržitě po celou hodinu proklínal; to když se mu povede, pak je jeho domýšlivost naprosto nesnesitelná.
A tak se vždycky běžel posadit na to, co George a Harris chtěli zrovna zapakovat, a řídil se utkvělou představou, že kdykoli ti dva vztahujou po něčem ruku, vztahujou ji po jeho studeném, vlhkém čumáku. Tlapky strkal do džemů, sápal se na lžičky a pak si začal hrát, že citróny jsou myši, skočil do jednoho koše a tři ty myši
zakousl, dřív než se Harrisovi podažilo uzemnit ho železnou pánví.
Harris tvrdil, že já jsem ho k tomu všemu podněcoval. Nikoli, nepodněcoval jsem ho. Takový pes žádné podněcování nepotřebuje. Aby takhle jančil, k tomu ho podněcuje vrozený prvotní hřích, s kterým už přišel na svět.
Pakování skončilo ve 12.50. Harris seděl na tom větším koši a vyslovil naději, že v něm nic nebude rozbité. A George řekl, že jestli v něm něco rozbité bude, tak to je rozbité už teď, a tato úvaha ho zjevně uklidnila. A dodal,
že je zralý pro postel. Harris měl té noci spát u nás, a tak jsme šli všichni nahoru do ložnice.
Házeli jsme šilinkem, kdo s kým bude sdílet lože, a Harrisovi vyšlo, že má spát se mnou.
„Chceš spát radši u zdi nebo dál ode zdi?“ zeptal se mne.
Odpověděl jsem, to že je mi jedno, jen když to bude v posteli.
A Harris řekl, že to je fousaté.
„A kdy vás mám vzbudit, mládenci?“ tázal se George.
Haris řekl: „V sedm.“
A já řekl „Ne - v šest!“, protože jsem ještě chtěl napsat pár dopisů.
Kvůli tomu j sme se já a Harris chvilku hádali, ale nakonec j sme si vyšli na půl cesty vstříc a dohodli j sme se na půl sedmé.
„Vzbuď nás v šest třicet, Georgi,“ řekli jsme.
George nám na to neodpověděl, a když jsme k němu přistoupili, zjistili jsme, že už spí; a tak jsme mu k posteli postavili vaničku s vodou, aby do ní skočil, až bude ráno vstávat, a šli jsme taky spat.
Paní P. nás burcuje - Ospalec George - Švindl s předpovídáním počasí - Naše zavazadla - Malý zpustlík - Sběh lidí kolem nás - Vznešeně odjíždíme na nádraží Waterloo - Nevědomost personálu Jihozápadní dráhy, pokud jde o vlaky - Jsme na vodě, na vodě v otevřené lodi.
V
Ale byla to paní Poppetsová, kdo mě ráno vzbudil.
„Jestlipak víte, pane, že už je skoro devět hodin?“ volala.
„Devět čeho?“ vykřikl jsem, vyskakuje z postele.
„Devět hodin,“ odvětila skrze klíčovou dírku. „Říkám si, že jste asi zaspali.“
Vzbudil jsem Harrise a řekl jsem mu, co se stalo.
„Já myslel, že tys chtěl vstávat v šest,“ pravil Harris.
„Taky že chtěl,“ odpověděl jsem. „Proč jsi mě nevzbudil?“
„Jak jsem tě mohl vzbudit, když jsi ty nevzbudil mě,“ odsekl. „A teď se už stejně nedostaneme na vodu dřív než někdy po dvanácté. Tak nechápu, proč vůbec lezeš z postele.“
„Ty buď rád, že z ní lezu,“ odvětil jsem. „Kdybych tě neprobudil, tak tu budeš takhle ležet celých těch čtrnáct dní.“
Tímto tónem jsme na sebe ňafali už pět minut, když nás přerušilo vyzývavé chrápání Georgeovo. Tím jsme si poprvé od okamžiku, kdy jsme byli vyburcováni ze spánku, uvědomili jeho existenci. A on si tam ležel - ten chlap, který chtěl vědět, v kolik hodin nás má vzbudit - na zádech, s hubou dokořán a s koleny nahoru.
Nevím, čím to je, to na mou duši nevím, ale pohled na někoho jiného, jak si spí v posteli, zatímco já jsem na nohou, mě vždycky rozzuří. To na mě působí přímo otřesně, když musím přihlížet, jak někdo dokáže drahocenné hodiny života - ty okamžiky k nezaplacení, které se mu nikdy nenavrátí - promarňovat jako nějaké hovado jen a jen spánkem.
No prosím! - a takhle tam George v odporném lenošení zahazoval ten neocenitelný dar času; cenný život, z jehož každé vteřiny bude jednou muset skládat počet, mu ubíhal nevyužit. Mohl se cpát slaninou s vejci, pošťuchovat psa nebo koketovat s naší služtičkou, místo aby se takto povaloval, zapadlý do duchamorného nevědomí.
To bylo strašlivé pomyšlení. A zasáhlo zřejmě Harrise i mne současně. Rozhodli jsme se, že ho zachráníme, a pro toto ušlechilé předsevzetí jsme rázem zapomněli i na svou vlastní rozepři. Vrhli jsme se k Georgeovi, servali s něho deku, Harris mu jednu přišil pantoflem a já mu zařval do ucha a George se probudil.
„Coseděé?“ pravil a posadil se.
„Vstávej, ty kládo jedna slabomyslná!“ zaburácel Harris. „Je čtvrt na deset!“
„Co?“ zavřeštěl George a vyskočil z postele do vaničky. - „Kdo to sem ksakru postavil?“
Řekli jsme mu, že musel pořádně zblbnout, když už nevidí ani vaničku.
Dooblékli jsme se, a když došlo na konečnou úpravu zevnějšku, vzpomněli jsme si, že jsme si už zapakovali kartáčky na zuby, kartáč na vlasy a hřeben (ten můj kartáček na zuby, to bude jednou moje smrt, to já vím), a museli jsme jít dolů všecko to z kufru vylovit. A když jsme to konečně dokázali, chtěl George holicí náčiní. Řekli jsme mu, že dneska se musí objeít bez holení, jelikož kvůli němu ani kvůli komukoli jinému už ten kufr přepakovávat nebudeme.
„Mějte rozum,“ povídá. „Copak můžu jít do banky takhle?“
To nesporně byla vůči celé bankovní čtvrti surovost, co nám však bylo do útrap lidstva? Bankovní čtvrť, jak to svým běžným drsným způsobem vyjádřil Harris, to bude muset spolknout.
Sešli jsme dolů k snídani. Montmorendy si pozval dva jiné psy, aby se s ním přišli rozloučit, a ti si zrovna krátili čas tím, že se rvali před domovními dveřmi. Uklidnili jsme je deštníkem a zasedli jsme ke kotletám a studenému hovězímu.
„Pořádně se nasnídat, to je věc velice důležitá,“ pravil Harris a začal s dvěma kotletami; ty je prý nutno jíst, dokud jsou teplé, hovězí, to prý počká.
George se zmocnil novin a předčítal nám zprávy o neštěstích na řece a předpověď počasí, kterážto předpověď prorokovala „zamračeno, chladno, proměnlivo až deštivo,“ (prostě ještě příšernější počasí, než bývá obvykle), „občasné místní bouřky, východní vítr, celková deprese nad hrabstvími střední Anglie (Londýn a Kanál); barometr klesá.“
Já si přece jen myslím, že ze všech těch pitomých, popuzujících šaškáren, jimiž jsme sužováni, jde tenhle podvod s „předpovídáním počasí“ na nervy nejvíc. Vždycky „předpovídá“ přesně to, co bylo včera nebo předevčírem, a přesně opak toho, co se chystá dnes.
Vzpomínám si, jak jsme si jednou na sklonku podzimu úplně zkazili dovolenou tím, že jsme se řídili povětrnostními zprávami v místních novinách. „Dnes jest očekávati vydatné přeháňky a bouřky,“ stálo tam třeba v pondělí, a tak jsme se vzdali plánovaného pikniku v přírodě a celý den jsme trčeli doma a čekali, kdy začne pršet. Kolem našeho dou proudili výletníci, v kočárech i omnibusech, všichni v té nejlepší, nejveselejší náladě neboť svítilo sluníčko a nikde nebylo vidět mráček.
„Á jé,“ říkali jsme si, koukajíce na ně z okna, „ti přijedou domů promočení!“
Chichtali jsme se při pomyšlení, jak zmoknou, odstoupili jsme od okna, prohrábli oheň a vzali si knížky a přerovnávali sbírečku mořských chaluh a mušliček. K poledni, když jsme div nepadli horkem, jak nám do pokoje pražilo slunce, jsme si kladli otázku, kdy asi spustí ty prudké přeháňky a občasné bouřky.
„Všecko to přijde odpoledne, uvidíte,“ říkali jsme si. „A ti tak promoknou, ti lidé, to bude taková legrace!“
V jednu hodinu se nás naše domácí přišla zeptat, jestli nepůjdeme ven, když je tak překrásný den.
„Kdepak, kdepak,“ odvětili jsme s mazaným uchichtnutím. „My promoknout nechceme. Kdepak.“
A když se odpoledne chýlilo ke konci a pořád to ještě nevypadalo na déšť, snažili jsme se zvednout si náladu představou, že to spadne zčistajasna, až lidi už budou na cestě domů a nebudou se mít kam schovat, takže na nich nezůstane suchá ani nitka. Leč nespadla ani kapička a celý den se vydařil a přišla po něm líbezná noc.
Nazítří jsme si přečetli, že bude „suché, pěkné, ustalující se počasí, značné horko“; vzali jsme si na sebe své nejlehčí hadříky a vyrazili jsme do přírody a za půl hodiny se přihnal hustý liják, zvedl se hnusně studený vítr a to obojí trvalo bez ustání celý den a my jsme se vrátili domů s rýmou a s revmatismem v celém těle a hned jsme museli do postelí.
Počasí, to je prostě něco, nač vůbec nestačím. Jaktěživ se v něm nevyznám. A barometr je k ničemu; to je zrovna taková šalba jako ty novinářské předpovědi.
Jeden visel v jistém oxfordském hotelu, kde jsem byl ubytován loni na jaře, a když jsem tam přišel, tak ukazoval na „ustáleně pěkně“. A venku pršelo, jen se lilo; celý den; nedovedl jsem si to vysvětlit. Zaťukal jsem na
ten barometr a ručička poskočila a ukázala na „velmi sucho“. Zrovna šel kolem kluk, co tam čistil boty, zastavil se u mě a povídá, že to asi znamená, jak bude zítra. Já spíš soudil, že to ukazuje, jak bylo předminulý týden, ale ten kluk povídá, to že asi ne.
Příští ráno jsem na ten barometr zaťukal znova a ručička vyletěla ještě výš a venku cedilo jako snad nikdy. Ve středu jsem do něj šel šťouchnut zase a ručička běžela přes „ustáleně pěkně “, „velmi sucho“ a „vedro “, až se zarazila o takový ten čudlík a dál už nemohla. Dělala, co mohla, ale ten aparát byl sestrojen tak, že důrazněji už pěkné počasí prorokovat nemohl, to už by se byl pochroumal. Ručička očividně chtěla běžet dál a udělat prognózu na katastrofální sucha a vypaření všeho vodstva a sluneční úžehy a písečné vichřice a podobné věci, ale ten čudlík jí v tom zabránil, takže se musela spokojit s tím, že ukázala na to prosté, obyčejné „vedro“.
A venku zatím vytrvale lilo jako z konve a níže položená část města byla pod vodou, neboť se rozvodnila řeka.
Čistič obuvi viděl v chování barometru zjevný příslib, že někdy později se nám nádherné počasí udrží po velmi dlouhou dobu, a nahlas mi přečetl veršovánku vytištěnou nad tím orákulem, něco jako:
Dlouho slibované trvá dlouze; narychlo oznámené přeletí pouze.
Toho léta se pěkné počasí nedostavilo vůbec. Počítám, že ten přístroj se zmiňoval až o jaru příštího roku.
Pak jsou ty nové barometry, ty dlouhé, jako tyčky. Z těch už jsem teda úplný jelen. Tam se na jedné straně ukazuje stav v deset hodin dopoledne včera a na druhé straně stav v deset hodin dopoledne dnes; jenže přivstat si tak, abyste to šel kontrolovat zrovna v deset, se člověku nepodaří, to dá rozum. Klesá a stoupá to tam na déšť nebo na pěkně, na silný vítr nebo na slabý vítr, na jednom konci je MIN a na druhém MAX (nemám ponětí, který Max to má být), a když na to zaťukáte, tak vám to vůbec neodpoví. A ještě si to musíte přepočítat na příslušnou výšku nad mořem a převést na Fahrenheita, a já ani potom nevím, co mě vlastně čeká.
Ale kdo vlastně touží po tom, aby se mu předpovídalo počasí? Když ten nečas přijde, tak je to protivné až dost, a aspoň jsme z toho neměli mizernou náladu už napřed. Prorok, jakého máme rádi, to je ten stařeček, který se toho obzvlášť ponurého rána, kdy si obzvlášť vroucně přejeme, aby byl krásný den, rozhlédne obzvlášť znaleckým zrakem po obzoru a řekne:
„Ale kdepák, pane, já počítám, že se to vybere. To se určitě protrhá, pane.“
„Ten se v tom hlot vyzná,“ říkáme si, popřejeme mu dobré jitro a vyrážíme. „Ohromná věc, jak tomu tihle staroušci rozumějí.“
A pociťujeme k tomu člověku náklonnost, kterou nikterak neoslabí fakt, že se to nevybralo a že celý den nepřetržitě leje.
„Inu,“ myslíme si, „aspoň se snažil.“
Zatímco vůči člověku, který nám prorokoval špatné počasí, chováme jen myšlenky trpké a pomstychtivé.
„Vybere se to, co myslíte?“ voláme bodře, když jdeme kolem něho.
„Ne ne, pane. Dneska to bohužel celej den nebude stát za nic,“ kroutí hlavou ten člověk.
„Dědek jeden pitomá!“ bručíme. Co ten o tom může vědět?“ A když na jeho zlověstnou předtuchu dojde, vracíme se s pocity ještě větší zloby vůči němu a s takovým mlhavým dojmem, že to nějak spískal on.
Toho dotyčného rána bylo příliš jasno a slunečno, aby nás George mohl nějak znepokojit, když nám tónem, při němž měla stydnout krev, předčítal, jak „barometr klesá“, jak „atmosférické proruchy postupují nad jižní Evropou šikmo k severu,“ a jak se „přibližuje oblast nízkého tlaku“. Když tedy zjistil, že nás nemůže uvrhnout v zoufalství a že zbytečně maří čas, šlohnul mi cigaretu, kteru jsem si zrovna pečlivě ukroutil, a šel do banky.
Harris a já jsme dojedli to málo, co George nechal na stole, a pak jsme vyvlekli před dům svá zavazadla a vyhlíželi jsme drožku.
Těch zavazadel bylo dost, když jsme je snesli na jednu hromadu. Měli jsme ten veliký kufr a jeden menší kufřík, příruční, a ty dva koše a velikánskou roli dek a asi tak čtvero nebo patero svrchníků a pršáků a několik deštníků a pak meloun, který měl jednu tašku jenom pro sebe, protože ho byl takový kus, že se nikam jinam nevešel, a ještě pár kilo hroznů v další tašce a takové japonské papírové paraple a pánev, která byla tak dlouhá, že jsme ji nemohli k ničemu připakovat, a tak jsme ji jen tak zabalili do papíru.
No, byla toho pořádná kupa a Harris a já jsme se za ni začali jaksi stydět, i když teď nechápu, proč vlastně. Drožka se neobjevovala, zato se objevovali četní uličníci, kterým jsme zřejmě skýtali zajímavou podívanou, a tak se stavěli kolem nás.
První se přiloudal ten kluk od Biggse. Biggs je náš zelinář a má zvláštní talent zaměstnávat ty nej sprostší a nejzpustlejší učedníky, jaké civilizace dosud zplodila. Když se v našem okolí vyskytne něco až neobvykle zlotřilého v klukovském provedení, hned víme, žte je to Biggsův nejnovější učedník. Po té vraždě v Great Coram Street došla prý naše ulice okamžitě k závěru, že v tom má prsty Biggsův učedník (ten tehdejší), a kdyby se mu při přísném křížovém výslechu, kterému ho podrobilo číslo 1 9, k němuž ráno po tom zločinu zaskočil pro objednávku (číslu 1 9 asistovalo číslo 21 , protože náhodou stálo zrovna před domem), kdyby se mu tedy bylo nepodařilo prokázat při tom výslechu dokonalé alibi, bylo by to s ním dopadlo moc špatně. Já jsem toho tehdejšího Biggsova učedníka neznal, ale soudě podle toho, co vím o všech těch dalších, sám bych byl tomu alibi velký význam nepřikládal.
Jak už jsem tedy řekl, za rohem se ukázal Biggsův učedník. Když se prvně zjevil v dohledu, měl očividně veliký spěch, ale jakmile si všiml Harrise a mne a Montmorencyho a našich věcí, zpomalil svůj běh a vypoulil oči. Harris
i já jsme se na něho zaškaredili. Jen poněkud citlivé povahy by se to bylo dotklo, avšak Biggsovi učedníci nejsou obyčejně žádné netykavky. Ani ne metr od našich schůdků se ten kluk zastavil, opře se o železnou tyč v plotě, pečlivě si vybral stéblo trávy, které by se dalo cucat, a začal nás upřeně pozorovat. Zřejmě se mínil dožít toho, jak to s námi dopadne.
Za chvilku šel na protějším chodníku učedník hokynářův. Biggsův kluk na něj zavolal:
„Ahoj! Přízemí z dvaačtyřicítky se stěhuje.“
Hokynářův učedník přešel přes ulici a zaujal postavení na druhé straně našich schůdků. Pak se u nás zastavil mladý pán od obchodníka s obuví a připojil se k učedníkovi od Biggse, zatímco vrchní dohlížitel na čistotu vyprzádněných pohárů od „Modrých sloupů“ zaujal postavení zcela samostatné, a to na obrubě chodníku.
„Hlad teda mít nebudou, co?“ řekl pán z krámu s obuví.
„Hele, ty by sis taky vzal s sebou jednu nebo dvě věci,“ namítly Modré sloupy, „dyby ses v malej lodičce chystal přes Atlantickej oceán.“
„Ty se neplavěj přes oceán,“ vložil se do toho kluk od Biggse, „ty jedou hledat Stanleye.“
Tou dobou se už shromážidl slušný hlouček a lidi se jeden druhého ptali, co se to děje. Jedna skupina (ta mladší a frivolnější část hloučku) trvala na tom, že je to svatba, a za ženicha označovali Harrise; zatímco ti starší a uvážlivější v tom lidu se přikláněli k názoru, že jde o pohřeb a já že jsem patrně bratr mrtvoly.
Konečně se vynořila prázdná drožka (je to ulice, kterou zpravidla - když je nikdo nepotřebuje - projíždějí tři prázné drožky za minutu, postávají a pletou se vám do cesty), i naskládali jsme do ní sebe i své příslušenství, vyhodili jsme z ní pár Montmorencyho kamarádů, kteří se mu zřejmě zapřisáhli, že ho nikdy neopustí, a odjížděli jsme; dav nám provolával slávu a Biggsův učedník po nás pro štěstí hodil mrkví.
Na nádraží Waterloo jsme dorazili v jedenáct a vyptávali jsme se, odkud vyjíždí vlak v 11,05. To pochopitelně nikdo nevěděl; na tomhle nádraží nikdy nikdo neví, odkud který vlak vyjíždí, nebo kam který vlak, když už odněkud vyjíždí, dojíždí, no tam prostě nikdy nikdo neví nic. Nosič, který se ujal našich zavazadel, měl za to, že náš vlak jede z nástupiště číslo dvě, avšak jiný nosič, s kterým jsme o tom problému rovněž diskutovali, prý někde zaslechl cosi o nástupišti číslo jedna. Přednosta stanice byl však naproti tomu přesvědčen, že ten vlak vyjede z nástupiště pro vnitrolondýnskou dopravu.
Abychom se to dověděli s konečnou platností, šli jsme nahoru a zeptali jsme se hlavního výpravčího, a ten nám řekl, že zrovna potkal nějakého člověka, který se zmínil, že ten vlak viděl na nástupišti číslo tři. Odebrali jsme se na nástupiště číslo tři, ale tam se příslušní činitelé domnívali, že ten jejich vlak je expres do Southamptonu nebo taky možná lokálka do Windsoru. Ale že to není vlak do Kongstonu, to věděli naprosto jistě, i když nevěděli jistě, proč to vědí tak jistě.
Pak řekl náš nosič, že náš vlak bude nejspíš ten vlak na zvýšeném nástupišti; prý ho zná, ten náš vlak. Tak jsme šli na zvýšené nástupiště a ptali jsme se přímo strojvedoucího, jestli jede do Kongstonu. Pravil, že s jistotou to pochopitelně tvrdit nemůže, ale že si myslí, že tam jede. A i kdyby ten jeho vlak nebyl ten v 11.05 do Kingstonu, tak prý dost pevně věří, že je to vlak v 9,32 do Virginia Water, nebo rychlík v 10.00 na ostrov Wight, anebo prostě někam tím směrem, což prý bezpečně poznáme, až tam dojedeme. Strčili jsme mu do dlaně půlkorunu a prosili
jsme ho, ať z toho udělá ten v 11.05 do Kingstonu.
„Na téhle trati stejně nikdo neví, co jste za vlak a kam jedete,“ řekli jsme mu. „Cestu jistě znáte, tak odtud potichounku vyklouzněte a vemte to na Kingston.“
„No, já teda nevím, páni,“ odvětil ten šlechetný muž, „ale předpokládám, že nějakej vlak do Kingstonu jet musí, tak tam teda pojedu já. Tu půlkorunu mi klidně nechte.“
Takto jsme se Londýnskou a Jihozápadní dráhou dostali do Kingstonu.
Později jsme se dověděli, že ten vlak, co jsme s ním jeli, byl ve skutečnosti poštovní vlak do Exeteru a že ho na
nádraží Waterloo hodiny a hodiny hledali a nikdo nevěděl, co se s ním stalo.
Naše loď na nás čekala v Kingstonu přímo pod mostem, k ní j sme tedy zameřili své kroky, na ni j sme naskládali svá zavazadla a do ní jsme posléze vstoupili.
„Nechybí vám něco, páni?“ ptal se muž, co mu loď patřila.
„Ne, nic tu nechybí,“ odpověděli jsme a pak jsme, Harris u vesel, já u kormidla a Montmorency, nešťastný a hluboce nedůvěřivý, na přídi, odrazili na vody, jež měly být čtrnáct dní naším domovem.
Kingston - Poučené poznámky k raným dějinám Anglie - Poučné postřehy k otázce vyřezávaného dubu a života všeobecně - Smutný případ Stivvingse juniora - Dumání o starožitnostech - Zapomínám, že kormidluji - Zajímavý výsledek - Bludiště v Hampton Courtu - Harris coby průvodce.
VI
Bylo překrásné dopoledne pozdního jara či raného léta, podle toho, jak je libo to brát, kdy líbezný lesk trávy a listí se už zardívá víc do zelena a kdy se rok podobá sličné dívence, rozechvělé podivným tepotem, jaký se probouzí na prahu ženství.
Kuriózní kingstonské postranní uličky vyhlížely v místech, kde se svažují k vodě, velice malebně v tom zářícím slunci, třpytící se řeka s přeplouvajícími lodicemi, zelení osázená navigace, pečlivě udržované vily na protějším
břehu, Harris funící ve svém oranžovočerveném flanelovém sáčku u vesel, v dálce se chvílemi objevující šedivý starý palác Tudorů - to vše tvořilo prosluněný obraz, tak pestrý a přitom tichý, tak plný života a přece tak pokojný, že jsem cítil, ačkoli den ještě příliš nepokročil, jak mě ukolébává do snivého zadumání.
A zadumal jsem se nad Kingstonem, či „Kyningestunem“, jak se jmenoval v dávných dnech, kdy v něm byli korunováni saští „kyningové“. Řeku tady překročil dokonce veliký Caesar a na svazích okolních kopečků tábořily římské legie. Caesar, zrovna tak jako v pozdějších letech Alžběta, se zřejmě zastavil všude; jenomže dbal víc o svou pověst než dobrá královna Bětka; nezapadl do kdejaké hospody.
Ta byla po hospodách celá divá, ta panenská královna Anglie. Do okruhu patnácti kilometrů od Londýna byste sotva našli někjakou aspoň trochu lákavou krčmu, v které toho či onoho dne neposeděla nebo nepřespala, nebo do které aspoň nenakoukla. Teď mě napadá, kdyby Harris obrátil novou stránku a stal se z něho slavný a užitečný člověk a kdyby to dotáhl až na ministerského předsedu a umřel, jestli by na všechny hospody, které poctil svou návštěvou, dali cedulky jako „V tomto podniku vypil jedno pivo Harris“, „Zde si v létě v roce 88 dal harris dvě chlazené skotské“, „Zde v prosinci 1886 vyrazili dveře s Harrisem“
Ale ne, takových hospod by bylo strašně mnoho. Proslavily by se naopak ty podniky, do kterých Harris jakživ nevstoupil. „Jediný hostinec v jižním Londýně, kde nikdy nepopíjel Harris!“ Do toho by se lidé přímo hrnuli, aby zjistili co to s ním je.
Jak asi nenáviděl Kyningestun ten chudák prostoduchá král Edvin! Už korunovační hosina ho úplně zmohla. Asi mu nedělala dobře kančí hlava se sladkou nádivkou (mně by teda dobře nedělal, to vím) a už měl až po krk vína a strdí, a tak vyklouzl z toho uřvaného veselí, aby si ukradl klidnou chvilku při měsíčku pro svou milovanou Elgivu.
A snad stáli ruku v ruce v okně dívali se na klidný odraz luny na řece a ze vzdálených síní k nim chvílemi jen slabě doléhal řev a randál bujné, nevázané zábavy.
Ale pak surově vrazí do té tiché komnaty bestiální Odo a svatý Dunstan, vmetou královně do líbezné tváře
spoustu sprostých urážek a odvlečou chudáčka Edvina zpátky do ohlušujícího chaosu opilé vřavy.
Po letech byli saští králové i saské hodokvasy pohřeni bok po boku za třeskotu hudby válečné a sláva Kingstonu na čas pohasla, aby se zaskvěla ještě jednou, když se rezidencí Tudorů a Stuartů stal Hampton Court a k říčnímu břehu byly připoutány královské lodice a po stupních vedoucích k vodě si nadutě vykračovali šviháci v pestrých pláštěnkách a pokřikovali: „Hej, převozní če! A zčerstva, přisámbůh!“
Mnohé z těch starých domů tam v okolí hovoří velmi jasně o dnech, kdy Kingston byl hradcem královým a sídlila tu, v blízkosti svého panovníka, šlechta a dvořanstvo a dlouhá cesta k bránám paláce bývala celé dny rozjásána řinčící ocelí, vzpínajímíci se oři, šustícími hedváby a samety a sličnými tvářemi. Z těch velikých, prostorných domů s arkýřovými mřížkovanými okny, obrovskými krby a střechami plnými lomenic dýchají časy přiléhavých nohavic a jupic, perlami vyšívaných náprsenek a komplikovaných kleteb. Ty domy byly zbudovány v dobách, „kdy lidé ještě uměli stavět.“ Jejich tvrdé červené cihly se časem ještě pevněji usadí a jejich dubová
schodiště nezapraskají a nezavrzají, když se po nich snažíte sestoupit v tichosti.
Když už mluvím o dubových schodištích, tak si vzpomínám, že v jednom domě v Kingstonu je takové dubové schodiště s nádhernými řezbami. Dnes je v tom domě krám - je to na starém tržišti -, ale kdysi to zřejmě bylo sídlo nějaké slavné osobnosti. Jeden můj přítel, který v Kingstonu bydlí, si tam šel jednoho dne koupit klobouk a v záchvatu bezmyšlenkovitosti sáhl do kapsy a bez řečí ho na místě zaplatil.
Obchodníka (on toho mého přítele dobře zná) to pochopitelně v první chvíli vyvedlo z konceptu; ale rychle se vzpamatoval, a poněvadž cítil, že by se mělo něco udělat, aby se povzbudila chuť takové způsoby opakovat, zeptal se našeho hrdiny, jestli by se nechtěl podívat na pěkné starodávné řezby v dubu. Můj přítel řekl, že by chtěl, a obchodník ho tedy zavedl dozadu za krám a na schodiště. Sloupkové zábradlí, to bylo skvostné umělecké dílo a dubové ostění podél celého schodiště zdobily řezby, za které by se nemusel stydět žádný palác.
Po tom schodišti došli do salónu, což byla rozlehlá, světlá místnost, polepená dosti děsivou, byť bujarou tapetou s modrým podkladem. Pozoruhodného však nebylo v celém tom bytě pranic a můj přítel nechápal, proč sem byl zaveden. Ale majitel domu přistoupil ke zdi a zaklepal na tapetu. Ozval se dřevěný zvuk.
„Dub,“ vysvětloval obchodník. „Všude samý vyřezávaný dub, až ke stropu, zrovna takový, jaký jste viděl na schodech.“
„U čertovy babičky!“ zvolal vyčítavě můj přítel. „Snad nechcete říct, člověče, že jste vyřezávaný dub zakryl modrou tapetou?“
„Ovšem,“ zněla odpověď. „A stálo mě to spoustu peněz. Nejdřív se to pochopitelně muselo všecko zabednit překližkami. Ale teď aspoň je ten pokoj veselý. Předtím byl příšerně ponurý.“
Nemohu říci, že to mám tomu člověku moc za zlé (čímž jsem mu nepochybně svalil ze srdce velikánský balvan). Ze svého hlediska, totiž z hlediska běžného domácího, který touží brát život pokud možno z té jasné strán,y nikoli tedy z hlediska vášnivého milovníka starožitností, má úplně pravdu. Na vyřezávaný dub je milo pohledět, kousek ho člověk může i mít doma, ale bydlet v něm, to je pro toho, kdo má docela jiné záliby, bezpochyby trochu skličující. To by mohl rovnou bydlet v kostele.
Ne, na tomhle případu je smutná jiná věc: že někdo, kdo o vyřezávaný dub nestojí, má z něho v salónu celé ostění, zatímco lidé, kteří o něj stojí, musejí zaplatit obrovské peníze, když ho chtějí mít. Ale tak to zřejmě na světě chodí. Každý má to, po čem netouží, a to, po čem touží, to mají ti druzí.
Ženatí muži mají ženy a zřejmě by byli rádi, kdyby je neměli; a mladí svobodní chlapi naříkají, že je nemohou získat. Chudáci, kteří jen tak tak seženou živobytí sami pro sebe, mají osm dětí se zdravou chutí k jídlu. A bohatí staří manželé nemají nikoho, komu by odkázali své peníze, a umírají bezdětní.
A co děvčata a jejich nápadníci? Když holky nápadníky mají, tak jim na nich nezáleží. Vykládají, že by se bez nich krásně obešly, že jim jdou na nervy, a proč prý se raději nejdou dvořit slečně Smithové a slečně Brownové, které jsou ošklivé a postarší a o žádné nápadníky nemohou zavadit. Ony samy že žádné nápadníky nepotřebují. Stejně se nikdy nehodlají vdávat.
Ale o těchhle věcech darmo mluvit! Člověku je z nich jenom smutno.
Se mnou chodil do školy jeden chlapec, říkali jsme mu Hodný Fridolín. Doopravdy se jmenoval Stivvings. To byl tak zvláštní kluk, že jsem se s podobným už nikdy nesetkal. On se vám vážně rád učil. Namouduši. Doma dostával v jednom kuse vynadáno, že v posteli místo spaní sedí a učí se řečtině. A francouzská nepravidelná slovesa? Od těch ho prostě nemohli odtrhnout. Měl plnou hlavu nepochopitelných a nepřirozených tužeb, jako že musí být chloubou svých rodičů a pýchou své školy; prahl po samých jedničkách, po tom, aby z něho vyrostl mudřec, zkrátka samé takovéhle slabomyslnosti měl v hlavě. Co živ jsem nepoznal tak divné stvoření - a přitom hodné, podotýkám, neškodné jako nenarozené batole.
No a tenhle chlapec se obyčejně tak dvakrát do týdne rozstonal, takže nemohl chodit do školy. Nikdy se žádný kluk tolik nenastonal jako ten Hodný Fridolín. Kdykoli se v okruhu patnácti kilometrů vyskytla jakákoli známá nemoc, on ji hned dostal a dostal ji v té nejhorší formě. V největších červencových parnech ulovil obvykle katar průdušek a o vánocích míval sennou rýmu. Po šestinedělním údobí sucha býval stižen revmatismem, a když si vyšel do listopadové mlhy, vracíval se domů se slunečním úpalem.
V jednom roce mu chudákovi museli dát narkózu a vytrhat mu zuby a nahradit je chrupem falešným, tak strašlivě ho totiž ty zuby bolely; ale z toho zas dostal nuralgii a bolesti v uších. Rýmu, tu měl pořád, až na jedno devítinedělní údobí, kdy ležel se spálou; a taky oznobeniny měl v jednom kuse. Když tenkrát v roce 1871 strašila ta cholera, v našem kraji byl od ní kupodivu pokoj. V celé farnosti byl jenom jeden vyložený případ: ten případ, to byl malý Stivvings.
Vždycky když stonal, musel být v posteli a krmit se kuřátky a piškotky s vanilkovým krémem a hrozny ze skleníků, a on v té posteli pořád vzlykal, protože si nesměl opakovat latinu a sebrali mu německou mluvnici.
A my ostatní kluci, my co bychom byli mlerádi obětovali deset školních pololetí za jediný den stonání a ani trošku jsme netoužili poskytnout rodičům nějaký důvod, aby na nás byli nafoukaní, my jsme si nedokázali uhnat ani housera. Blbli jsme v průvanech, a ono nám to dělalo dobře a byli jsme pak ještě čilejší; a jedli jsme věci, po kterých nám mělo být zle, ale jen jsme po nich tloustli a dosávali ještě větší hlad. Ať jsme si vymysleli cokoli, z ničeho jsme se nerozstonali - dokud nenastaly prázdniny. Potom, hned toho dne, kdy byla naposled škola, jsme dostali rýmu a černý kašel a všechny možné další choroby a ty se nás držely až do začátku nového školního roku; to jsme se najednou přes všechno, co jsme proti tomu podnikali, uzdravili a byli jsme na tom líp než kdykoli jindy.
Takový je život; a my nejsme nic než tráva, kterou posečou a hodí do pece a spálí.
Ale vraťme se k tomu problému vyřezávaného dubu: ti museli mít náramný smysl pro umění a pro krásu, ti naši prapradědové. Vždyť všechny ty dnešní umělecké poklady jsou jenom odněkud vyštrachané předměty běžné denní potřeby z doby před takovými třemi, čtyřmi sty lety. Rád bych věděl, jestli jsou doopravdy tak překrásné ty staré mísy na polévku a džbánky na pivo a kratiknoty, které si dnes tak vysoko ceníme, nebo jestli jim to kouzlo, co pro nás mají, dodává jenom ta aureola starobylosti, která nad nimi září. Ten starý modrý porcelán, který si pro ozdobu rozvěšujeme po stěnách, to je přece obyčejné, tuctové nádobí, denně používané v domácnostech před několika stoletími; a ti růžoví pastýři a žluté pastýřky, které strkáme do rukou všem svým přátelům, aby se nad nimi rozplývali nadšením a předstírali, že takovým věcem rozumějí, to byly bezcenné ozdůbky na krbových římsách, které máma z osmnáctého století dávala brečícím batolatům místo dudlíku.
Bude to v budoucnosti zrovna tak? Budou draze ceněné poklady dneška vždykcy jen těmi lacinými obyčejnostmi včerejška? Budou naše talíře s těmi modrobílými krajinkami srovnány v řadách nad krby celebrit z roku 2000 a něco? Budou ty bílé koflíky s tou zlatou obrubou a s tou krásnou zlatou květinou (neznámého druhu) na dně, které teď naše služky naprosto bezstarostně rozbíjejí, jednou pečlivě slepeny a vystaveny na nějaké poličce, kde je bude smět oprašovat pouze paní domu?
Vemte třeba toho porcelánového psa, co je okrasou ložnice v bytě, který jsem si najal i se zařízením. Je to bílý pes. Oči má modré. Čenich delikátně červený, s černými skvrnami. Hlavu má toporně zvednutou a v jeho výrazu se zračí roztomilost hraničící s přiblblostí. Já se mu neobdivuju. Jako dílo umělecké mi jde dokonce na nervy. Cyničtí kamarádi si z něho utahují a ani má bytná k němu nechová žádný obdiv a jeho přítomnost v bytě omlouvá tím, že jí ho darovala teta.
Ale je víc než pravděpodobné, že za dvě stě let bude ten pes odněkud vyštrachán, už bez noh a se zlomeným ohonem, a že ho někdo koupí jako starý porcelán a dá si ho do vitríny. A lidi ho budou ze všech stran okulovat a obdivovat. Uchvátí je podivnuhodná sytost té barvy na čumáku a budou se dohadovat, jak kráný byl bezpochyby ten kousíček ohonu, co se už nenašel.
My v dnešních dobách na tom psu nic krásného nevidíme. Příliš dobře ho známe. Jako západ slunce a hvězdy; jejich nádhera v nás taky nebudí posvátný úžas, protože podívaná na ně je nám všední. A s tím porcelánovým psem je to zrovna tak. V roce 2288 bude lidi přivádět do vytržení. Výroba takových psů bude označována jako zapomenuté umění. Naši potomci nebudou umět pochopit, jak jsme to dělali, a budou říkat, jak jsme byli šikovní.
Bude se o nás láskyplně psát jako „o těch skvělých dávných kumštýřích, co proslavili devatenácté století a vytvářeli ty porcelánové pej sky“
0 výšivce, kterou naše nejstarší dcera dělala ve škole, se bude hovořit jako o „gobelínu z viktoriánské éry“ a její cena bude téměř nedostupná. Po modrobílých korblících z dnešních zájezdních hospod bude veliká sháňka a budou se, celé popraskané a omlácené, vyvažovat zlatem a boháči jich budou používat jako pohárů na červené borauxské; a návštěvníci z Japonska skoupí všechny „dárky z lázní Ramsgate“ a „suvenýry z lázní Margate“, které nějak unikly zkáze, a odvezou si je zpátky do Jeda jako starodávné anglické kuriozity.
V tomto bodě mých úvah Harris pustil vesla, vyletěl z lavičky a hned si zase hrcnul na zadek a nohy vymrštil do vzduchu, Montmorency zavyl a udělal kotrmelec a koš, co byl nahoře, povyskočil a všechno se z něho vysypalo.
Poněkud mě to udivilo, ale nerozhněval jsem se. Naopak, zeptal jsem se velice vlídně:
„Co to vyvádíš, prosím tě?“
„Co já to vyvádím? No dovol...“
Nikoli, po zralé úvaze nebudu to, co Harris řekl, opakovat. Snad měl právo dávat vinu mně, to připouštím, nic však neomlouvá hrubý tón a drsné výrazy, zvláště u člověka, který byl slušně vychován, a to Harris, jak je mi známo, byl. Myslel jsem totiž na jiné věci a zapomněl jsem, což jistě kdekdo dovede pochopit, že kormidluji, a v důsledku toho jsme se do značné míry promísili s navigací. V prvním okamžiku se dalo těžko říci, co jsme my a co je middlesexský břeh řeky; ale po chvíli jsme na to přišli a provedli jsme odluku.
Harris však prohlásil, že se už nadřel dost a že by teď měla být řada na mně; tak jsem vystoupil na břeh, popadl vlečné lano a táhl loď podél Hampton Courtu. Ta je tak milá, ta stará zeď, co se tam zvedá podél řeky! Kdykoli tamtudy jdu, vždycky mi pohled na ni udělá dobře. Taková přívětivá, pestrá, líbezná stará zeď! Jak kouzelně by se vyjímala na obraze, s tím lišejníkem, co se plazí tuhle, a s tím mechem, co bují támhle, a s tou mladou révou, co přes ni tadyhle nesměle vykukuje, aby viděla, co se děje na rušné řece, a s tím rozvážným věkovitým břečťanem, co se po ní hustě rozrůstá o kus dál! Na každých deset metrů té staré zdi připadá padesát různých barevných odstínů, tónů a polotónů. Kdybych já uměl kreslit a vyznal se v malování, určitě bych si udělal pěkný obrázek té staré zdi. Často si říkám, že kdybych mohl v Hampton courtu bydlet, moc by se mi to asi líbilo. Vypadá to tam tak pokojně, tak tiše, je to takový ten milý starobylý kout, po kterém je příjemné se potulovat za časného rána, dřív než se tam nahrnou lidi.
Ale kdyby k tomu doopravdy došlo, tak bych pravděpodobně moc nadšený nebyl. Musí tam na vás padat příšerná nuda a tíseň, když přijde večer a vaše lampa hází tajuplné stíny na dřevem vykládané stěny a studenými kamennými chodbami se nese ozvěna vzdálených kroků, jednu chvíli se přibližuje a jednu chvíli odumírá, a všechno utichá jako ve smrti, až na tlukot vašeho vlastního srdce.
Jsme děti slunce, my muži a ženy. A milujeme světlo a život. Jen proto se tísníme v městech a velkoměstech a venkov se rok od roku víc a víc vylidňuje. Když slunce svítí - za dne, kdy příroda všude kolem nás je samý ruch a shon - to máme docela rádi volná úbočí kopců a hluboké lesy; ale v noci, když se naše matka Země uložila ke spánku a nás nechala bdít, ach, to nám svět připadá pustý a dostáváme strach jako malé děti v mlčícím domě. Pak sedíme a hořekujeme a toužíme po ulicích ozářených plynovými lampami a po zvuku člověčích hlasů a po spřízněném tepu života. V tom velebném tichu, kdy jen temné stromy šelestí v nočním větru, se cítíme tak bezmocní a tak malí. Kolem nás je taková spousta duchů a jejich slabounké vzdechy nás tolik rozesmutňují. Shlukněme se tedy dohromady ve velkých městech, zažehněme ohromné slavnostní ohně z miliónů plynových hořáků a křičme a všichni společně zpívejme a předvádějme, jak jsme stateční!
Harris se mě ptal, jestli jsem někdy byl v hamptoncourtském bludišti. On tam prý jednou zašel, aby jím někoho provedl. Napřed si to bludiště prostudoval podle plánku a zjistil, že je velice jednoduché; až nevtipné mu připadalo - škoda těch dvou pencí za vstupné. Ten plánek, řekl mi Harris, je zřejmě míněn jako kanadský žertík, jelikož ani za mák neodpovídá skutečnosti a jeho účelem je, aby návštěvník zabloudil. Ten člověk, co ho tam Harris vedl, byl jeden jeho bratránek z venkova.
„Jenom to tu omrkneme, abys mohl vypravovat, žes tu byl,“ řekl mu Harris, ,je to totiž nesmírně prostoduché. Musíš prostě na každém příštím rohu zahnout doprava. Budeme si tam asi tak deset minut špacírovat a potom půjdeme někam na oběd.“
Krátce potom, co tam vešli, potkali pár lidí, kteří říkali, že tam jsou už tři čtvrtě hodiny a že už toho mají tak akorát dost. Harris jim nabídl, že jestli je jim libo, mohou jít s ním; právě prý přichází zvenku, vezme to jednou kolem a hned půjde pryč. Ti lidé řekli, že je to od něho moc hezké, přidali se k němu a drželi se mu v patách.
Cestou přibírali různé další lidi, kteří to už chtěli mít za sebou, až posléze shromáždili vůbec všechny návštěvníky bludiště. Lidé, kteří se už nadobro vzdali veškeré naděje, že se ještě někdy dostanou buď dovnitř nebo ven, že ještě někdy uzří domov a přátele, sebrali při pohledu na Harrise a jeho družinu znova odvahu, připojili se k jeho procesí a blahořečili mu. Harris mi řekl, že podle jeho odhadu za ním muselo jít zhruba dvacet lidí; a jedna žena s děckem, která tam byla už celé dopoledne, si prosadila, že se do něho pevně zavěsí, takový měla strach, že by ho mohla ztratit z dohledu.
Harris zahýbal na každém rohu doprava, ale byl to zřejmě pořádný kus cesty a Harrisův bratránek poznamenal, že je to patrně náramně rozlehlé bludiště.
„Ch, jedno z největších v Evropě,“ pravil Harris.
„To jistě,“ odvětil bratránek, „vždyť jsme už ušli dobré tři kilometry.“
1 Harrisovi se to už zdálo nějak divné, ale pochodoval dál, dokud nepřešli kolem půlky žemle, která se válela na zemi a které si Harrisův bratránek, jak se dušoval, povšiml už před sedmi minutami. „Vyloučeno!“ tvrdil Harris,
ale ta žena s děckem řekla „Jaképak vyloučeno!“, ježto prý ona sama tu žemli odebrala svému dítěti a zrovna tady ji, těsně před tím, než potkala Harrise, hodila na zem. A ještě dodala, že by byla nevýslovně šťastná, kdyby ho byla nepotkala nikdy, a vyslovila mínění, že je podvodník. To Harrise rozzlobilo, vytáhl plánek a vysvětloval svou teorii.
„Ten plánek se nám může hodit, copak o to,“ poznamenal kdosi v průvodu, ,jen jestli na něm dokážeme ukázat místo, kde právě jsme.“
To Harris nedokázal a navrhoval, že by tedy snad bylo nejlepší jít zpátky ke vchodu a začít znova. Pro tu část návrhu, podle níž se mělo začít znova, nejevil nidko zvláštní nadšení; ale že by bylo radno jít zpátky ke vchodu, to bylo schváleno naprosto jednomyslně, a tak se všichni otočili a opět se vlekli za Harrisem, tentokrát opačným směrem. Uplynulo dalších deset minut a procesí se ocitlo v prostředku bludiště.
V první chvili chtěl Harris předstírat, že to je právě to, čeho toužil docílit, avšak zástup se tvářil nebezpečně, a tak se Harris rozhodl pojmout to jako nedopatření.
Teď měli ostatně pevní bod, z kterého se dalo vyjít. Teď už věděli, kde jsou, znovu vzali v potaz plánek a všechno jim připadalo jednodušší než dřív, i vydali se potřetí na pochod.
Za tři minuty byly zase zpátky v tom prostředku.
A pak se už nemohli dostat prostě nikam. Ať se dali kteroukoli cestou, vždycky je dovedla zpátky do středu bludiště. Z toho se časem vyvinulo takové pravidlo, že někteří lidé v tom středu prostě zůstali a čekali na ty ostatní, až se projdou kolem dokola a zase se vrátí k nim. Po nějakém čase vylovil Harris opět ten plánek, ale dav se, jak ho jen zahlédl, velice rozběsnil a radil Harrisovi, aby si z něho udělal natáčky na vlasy. Harris prý se nemohl zbavit dojmu, že poněkud ztrácí popularitu.
Nakonec se všichni zcvokli a snažili se přihulákat zřízence. Ten člověk skutečně přišel, vylezl z protější strany na žebřík a odtamtud na ně řval pokyny. Ale tou dobou už jim všem vířil v hlavách takový zmatek, že nebyli schopni cokoli pochopit, a tak jim ten člověk řekl, ať zůstanou stát na místě a on že si pro ně přijde. Semkli se tedy dohromady a čekali; a zřízenec slezl ze žebříku a vešel dovnitř.
Osud tomu chtěl, že to byl zřízenec mladičký a v tomto oboru ješě nevycvičený, a tak se k těm lidem nemohl dostat a nakonec zabloudil sám. Tu a tam ho zahlédli, jak uhání kolem druhé strany toho živého plotu, a on je takéí párkrát spatřil a pokaždé se jal pádit ještě rychleji, aby se k nim dostal, a oni vždycky čekali asi tak pět minut a ten hoch se pak vždycky znova objevil na stejném místě a ptal se, kde to zase jsou.
Museli tedy počkat, dokud se nevrátil od oběda jeden ze starších zřízenců, a teprve pak se dostali ven.
Harris mi řekl, že pokud on to může posoudit, je to bludiště velice pěkné; a tak jsme se dohodli, že se na zpáteční cestě pokusíme zavést tam George.
Řeka v nedělním kroji - Úbor na řeku - Příležitost pro muže - Harris postrádá vkus - Georgeovo flanelové sáčko - Den se slečnou jako vystřiženou z módního žurnálu - Náhrobek paní Thomasové - Člověk, který nemá rád hroby ani rakve, ani lebky - Harris se zlobí - Má svůj názor na George, banky a limonády - Vyvádí skopičiny.
VII
Když mi Harris vypravoval o tom svém zážitku v bludišti, to jsme zrovna proplouvali moulseyským zdymadlem. Trvalo to dost dlouho, než nás do něho vpustili, nebyla tam totiž žádná jiná loď než ta naše a to zdymadlo je veliké. Nepamatuji se, že bych byl někdy předtím viděl v moulseyském zdymadle jenom jednu jedinou loď. Je to přece nejfrekventovanější zdymadlo na celé řece, ani boulterské nevyjímaje.
Stávám tam občas a pozoruj u ten ruch, když člověk nevidí ani kousek vody, ale jen a jen oslňující změť pestrých pánských sáček a křiklavých čapek a vyzývavých klobouků a mnohobarevných slunečníků a hedvábných přehozů a pelerínek a vlajících stuh a úhledných bílých obleků; když se z nábřeží dívate dolů do zdýmací komory, můžete si třeba představovat, že je to ohromná krabice, do které jsou bez ladu a skladu naházeny květiny všech odstínů barev, a vrší se v ní v duhoskvoucích kupách a vyplňují kdekteré místečko.
Za pěkných nedělí skýtá zdýmací komora tuhle podívanou po celý den, zatímco za obojími vraty, po proudu i proti proudu, čekají řady dalších a dalších lodí, až na ně přijde řada; a nové lodi připlouvají a odplouvají, takže ta prosluněná řeka, od zámku až hamptonskému kosteku, je poseta a pokropena žlutí a modří, oranží a bělobou, červení a růžovím. A všichni obyvatelé Hamptonu a Moulseye se obléknou do veslařských úborů a poflakují se se svými pejsky kolem zdymadla, koketují, pokuřují a pozorují lodě a to všechno dohromady, ty čapky a kazajky mužů, ty lahodně barevné šaty žen, ti rozdovádění psi, ty plující loďky, ty bílé plachty, ta příjemná krajina a ta rozjiskřená voda, to všechno tvoří jednu z nej radostnějších podívaných, jaké jsem poznal v blízkosti toho pochmurného starého Londýna.
Řeka dopřává možnosti v oblasti odívání. Tam konečně jednou máme i my muži příležitost předvést svůj vkus, pokud jde o barvy, a já si myslím, když už se mě na to ptáte, že se tam projevujeme jako náramní šviháci. Já mám na sobě v každém případě rád kapánek červené - červené s černou. Mám totiž, abyste věděli, takové ty zlatavě hnědé vlasy, zvlášť pěkného odstínu, jak slýchávám, a k těm se červená hodí překrásně. A taky se domnívám, že se vedle nich znamenitě vyjímá bledě modrá vázanka, a takové ty juchtové střevíce ovšem, a rudá hedvábná šerpa kolem pasu - šerpa je mnohem slušivější než nějaký pásek.
Harris se neustále drží odstínů nebo kombinací oranžové a žluté, ale podle mého mínění to od něho není moc moudré. Má příliš snědou pleť, aby si mohl dovolit žluté barvy. Jemu žluté barvy nesluší, to je bez debaty. Já mu radím, aby se přiklonil k modré jako barvě základní a k bílé nebo krémové jako barvě doplňkové - ale marná sláva! Čím horší má člověk vkus, tím tvrdošíjněji se ho drží. A je to pro něj veliká škoda, protože takhle jakživ neudělá díru do světa, ačkoli by se našly dvě tři barvy, v kterých by možná nevypadal tak strašně, kdyby si ovšem narazil klobouk.
George si na ten náš výlet koupil pár nových věcí, a ty mě vysloveně rozmrzely. To flanelové sáčko přímo řvalo. Byl bych nrerad, kdyby se Georgeovi doneslo, co o tom sáčku soudím, ale jiná charakteristika pro ně prostě neexistuje. Přinesl si je domů ve čtvrtek večer a hned nám je ukázal. Ptali jsme se ho, jak by označil jeho barvu, a on se přiznal, že to neví. Tahle barva se prý ani pojmenovat nedá. Pokud jde o vzorek, prodavač ho prý prohlásil za orientální. George si to sáčko vzal na sebe a byl zvědav, co o něm soudíme. Harris řekl, že jako taková ta věc, co se zjara věší nad kvetoucí záhony, aby odstrašovala ptactvo, by získalo všechnu jeho úctu; má-li se však na ně dívat jako na součást oděvu pro kteroukoli jinou lidskou bytost než pro černocha stojícího z reklamních důvodů před obchodem s kávou, pak prý se mu z něho dělá zle. George se velice nafrnil; ale, jak řekl Harris, když jeho mínění znát nechtěl, tak proč se na ně ptal? Harrisovi i mně dělalo to sáčko značné starosti; báli jsme se totiž, že kvůli němu se naše loď stane středem pozornosti.
Ani dívky nevypadají v lodi zrovna špatně, když jsou pěkně oblečeny. Podle mého názoru nic nenadělá takovou parádu jako vkusný veslařský úbor. Jenže „veslařský úbor“ - to by měly všechny dámy konečně pochopit - má být úbor, v kterém se dá veslovat a ne pouze stát ve výkladní skříni. To vám úplně otráví celý výlet, když máte v lodi někoho, kdo se v jednom kuse stará jenom o své šaty a vůbec ne o tu plavbu. Já jednou měl tu smůlu, že jsem si vyjel na piknik na řece s dvěma takovými slečnami. To jsme si teda dali!
Obě byly báječně vyšňořené - samé kraječky a hedvábné kaýrky a kytičky a mašličky a k tomu roztomilé střevíčky a vzdušné rukavičky. Jenže to bylo oblečení pro fotografický ateliér a ne pro řeku. Byly to prostě „veslařské úbory“ podle francouzských módních žurnálů. V tomhle chtít laškovat někde v blízkosti skutečné země, skutečného vzduchu a skutečné vody, to bylo čiré bláznovství.
Jejich první myšlenkou bylo, že loď není dost čistá. Oprášili jsme jim všechna sedátka a ujišťovali jsme je, že v lodi čisto je, ale slečny nám nevěřily. Jedna přejela špičkou ukazováčku v rukavičce po polštářku a výsledek ukázala té druhé, načež si obě povzdechly a usedly, tváříce se jako musčednice z prvních dob křesťanství, které se snaží zaujmout u kůlu pozici co nejpohodlnější. Když člověk vesluje, tak se mu lehce stane, že trošku cáká, a tu se ukázalo, že stačí kapka vody a je po róbách. Stopa po té kapce prý nikdy nezmizí a na šatech zůstane navěky skvrna.
Já byl veslovod. A vesloval jsem, jak nejlíp jsem uměl. Už skoro půl metru nad vodou jsem obracel vesla tak, aby se zasekla pěkně hranou, po každém záběru jsem nejdřív nechal vodu z listů odkapat, než jsem zabral znova, a pro každý zásek jsem si vyhlédl to nejklidnější místečko na hladině. (Kolega na přídi po chvíli prohlásil, že se necítí být veslařem natolik povolaným, aby mohl veslovat se mnou, že však bude sedět naprosto klidně a studovat, jestli mu to dovolím, můj styl. Ten ho prý neobyčsejně zajímá.) Ale přes to přese všecko, přes veškeré úsilí se mi nepodařilo zabránit, aby občasná sprška vody nedopadla na ty šaty.
Dívky si nestěžovaly, jenom se k sobě co nejtěsněji tulily, seděly tam s pevně semknutými rty, a pokaždé, když na ně cákla kapička vody, viditelně se přirkčily a přiscvrkly. Byl na ně povznášející pohled, jak tak mlčky trpěly, mě však nicméně znervózňoval. Jsem člověk příliš citlivý. Vesloval jsem stdále nešikovněji a nepravidelněji a čím víc jsem se snažil necákat, tím víc jsem cákal.
Nakonec jsem to vzdal; řekl jsem, že budu veslovat na špici. Můj kolega si taky myslel, že to tak bude lepší, a tak jsme si vyměnili místa. Slečnám bezděčně uklouzl úlevný povzdech, když jsem se stěhoval k přídi, a na okamžik se očividně rozzářily. Chudinky malé! Se mnou na tom byly o mnoho líp. Chlápek, co si proti nim sedl teď, byl taková ta bezstarostná, přitroublá veselá kopa a ohleduplnosti měl asi tolik jako štěně bernardýna. Hodinu jste ho mohli probodávat očima, on to vůbec nepostřehl, a i kdyby to postřehl, tak si z toho nic nedělal. Nasadil solidní ostré, chvástavé tempo, při kterém na celou loď stříkala voda jako z vodotrysku a celá posádka se hned musela pevně držet. Když na některé šatičky vyšplíchl víc než půllitr vody, vždycky se bodře zasmál, zvolal „Jéje, odpusťte,“ a podával slečnám svůj kapesník, aby se osušily.
„Ale to nic,“ zamumlaly pokaždé ty ubohé dívenky a pokradmu přes sebe přetahovaly šály a pelerínky a pokoušely se chránit okrajkovanými slunečníčky.
Oběd, to pro ně bylo hotové utrpení. Chtělo se po nich, aby si sedly do trávy, jenže ta tráva byla samý prach; a kmeny stromů, které jim byly nabídnuty jako opěradla, zřejmě už několik týdnů nikdo nevykartáčoval; tak si na zem rozprostřely kapesníčky a posadily se na ně zpříma jako svíce. Jeden z nás, když šel kolem nich s mísou studeného jazyka s majonézou, zakopl o nějaký kořen a jazyk s majonézou vyletěl do vzduchu. Ani kousek na ně naštěstí nespadl, ale ta nehoda je upozornila na zcela nové nebezpečí a velice je rozrušila; takže když se potom někdo zvedl a měl v ruce něco, co mohlo sletět a udělat škodu, obě slečny po něm se vzrůstající úzkostí koukaly, dokud se zase neposadil.
„No a teď vzhůru, děvčata,“ řekl jim povzbudivě náš milý druhý veslař, když bylo po jídle, „bude se umývat nádobí!“
Dívky mu hned neporozuměly. A když pochopily, jak to myslí, prohlásily, že nemají ponětí, jak se nádobí umývá.
„Á, to já vás v cuku letu naučím,“ smál se ten chlapík, „to je ohromná legrace! To si lehnete na bři..., chci říct, že se nakloníte k vodě, a všecko v ní pěkně ošploucháte.“
Starší sestra namítala, že na takovou práci nejsou bohužel vhodně oblečeny.
„Ále, to se lehko spraví,“ řekl druhý veslař bezstarostně. „Prostě si vykasejte sukně.“
A skutečně je k tomu přinutil. Tyhle věci, to prý je při pikniku ta největší švanda. A dívky řekly, to že by byly nikdy neřekly.
Teď když si na to znova vzpomínám, tak si říkám, jestli ten mládenec doopravdy byl tak natvrdlý, jak jsme si tenkrát mysleli. Nebo spíš - ale ne, vyloučeno, vždyť se tvářil jako nevinné prostoduché děcko!
Harris chtěl vystoupit u hamptonského kostela a jít si prohlédnout náhrobek paní Thomasové.
„Co to bylo zač, ta paní Thomasová?“ ptal jsem se ho.
„Jak to mám vědět?“ odvětil Harris. „Má prý pozoruhodný náhrobek, a já bych ho chtěl vidět.“
Já byl proti tomu. Možná že je se mnou něco v nepořádku, ale hřbitovní pomníky mě nikdy nelákaly. Vím, že když přijdete do nějaké vesnice nebo do nějakého města, tak se sluší a patří, abyste uháněli na hřbitov a pokochali se obhlídkou hrobů, ale já si tenhle druh rekreace vždycky dokážu odepřít. Mne nebaví prolézat za zády nějakých sípavých dědků tmavé a studené kostely a číst si tam epitafy. Ani podívaná na kus popraskané pamětní desky, zapuštěné do nějakého kamene, mi neskýtá to, čemu říkám pocit opravdového štěstí.
Tím, jaký klid dovedu zachovat před vzrušujícími nápisy a s jakým nedostatkem entuziasmu naslouchám historiím místních rodin, odkajživa ohromuji důstojné kostelníky a jejich city zraňuji špatně zakrývanou nedočkavostí, abych už byl pryč.
Jednoho slunného dne jsem se za zlatého rána opíral lokty o nízkou kamennou zídku, střežící venkovský kostelík, kouřil jsem a s hlubokou, tichou blažeností jsem vychutnával líbezný klid scenérie, kterou jsem měl před sebou; šedivý starý kostel, obrostlý chomáči břečťanu a s prapodivným dřevěným portikem, celým vyřezávaným, bílá cestička, vinoucí se dolů s kopce mezi řadami vysokých jilmů, chalupy s doškovými střechami, vykukující nad úhledně sestřiženými živými ploty, v dolíku stříbrná řeka a kolem ní zalesněné kopce.
Byla to rozkošná krajinka. Idylická, poetická a probouzela ve mně vzletné myšlenky. Měl jsem pocit, že jsem dobrý a ušlechtilý. Měl jsem pocit, že už nikdy nebudu chtít hřešit a lumpačit. Sem půjdu žít, nikdy se už nedopustím žádné špatnosti, povedu bezúhonný, přečistý život, a až zestárnu, budu mít stříbrné vlasy a... no, taková a podobná jsem měl předsevzetí.
V té chvíli jsem všem svým přátelům a příbuzným odpustil jejich špatnosti a proradnosti a žehnal jsem jim. Oni nevěděli, že jim žehnám. Šli dál svou zhýralou cestou, nevědouce o tom, co já, daleko od nich, v té mírumilovné vesničce, pro ně dělám. Ale i tak jsem to pro ně dělal a přál jsem si, abych jim nějak mohl sdělit, že to pro ně
dělám, jelikož jsem jim chtěl přinést štěstí. Stále ještě jsem se zabýval těmito vznešenými, tklivými myšlenkami, když tu mé snění přerušil pronikavý písklavý hlas, který vykřikoval:
„Už du, vašnosti, už k nim du. Jo jo, vašnosti, už du, jenom na mě nesměj spěchat.“
Vzhlédl jsem a spatřil holohlavého staříka, který ke mně pajdal přes hřbitov, v ruce ohromný svazek klíčů, jež se při každém kroku rozcinkaly.
Mlčky, důstojným gestem jsem mu naznačoval, aby šel pryč, on se však dál přibelhával ke mně a co chvíli zapištěl:
„Už du, vašnosti, už du. Já kapánek kulhám. Už nejsem takovej čipera, jako sem bejval. Ráčej tudyhle, vašnosti.“
„Zmizte, dědku mizerná!“ křikl jsem.
„Dřív jsem přijít nemoh, vašnosti,“ odpověděl. „Moje panička jich zmerčila teprvá teď, v poslední chvíli. Ráčej za mnou, vašnosti.“
„Zmizte!“ opakoval jsem. „Ztraťte se, než přeskočím tu zídku a zabiju vás!“
To ho zřejmě překvapilo.
„Copak voni nechtěj vidět zdejší náhrobky?“ ptal se.
„Ne!“ odpověděl jsem. „Nechci! Chci zůstat stát na tomhle místě, opřený o tuhle oblázkovou zídku. Zmizte a nechte mě na pokoji. Jsem přecpaný krásnými a ušlechtilými myšlenkami a v tom stavu chci setrvat, protože je mi v něm dobře a příjemně. Tak se mi tu nemotejte, nerozčilujte mě a nesnažte se těmi svými pitomými náhrobky ze mě vyplašit moje lepší já. Táhněte, sežeňte si někoho, kdo vás levně pohřbí, a já zaplatím polovičku výloh.“
To ho na okamžik zarazilo. Promnul si oči a pozorně se na mě zadíval. Zvenčí vypadám dost lidsky, a tak si to všechno nemohl srovnat v hlavě.
„Voni sou v těchle končinách cizej, že jo?“ ptal se. „Voni tady nežijou?“
„Ne,“ řekl jsem, „nežiju tady. Kdybych tady žil, tak byste nežil vy.“
„Tak to teda přece,“ pravil, „musej chtít vidět zdejší náhrobky - hroby - no co sou tam pohřbený lidi - rakve.“ „To jste úplně vedle,“ křičel jsem, protože už ve mně všechno vřelo. „Nechci vidět náhrobky - aspoň ne ty vaše! Co bych z toho měl? Máme svoje vlastní hroby - naše rodina totiž. Můj strýc Podger například má na hřbitově v Kensal Greenu pomník, který je chloubou celého kraje; v hrobce mého děda v Bow je dost místa pro osm návštěvníků a moje prateta Susan má na finchleyském hřbitově hrob vyzděný cihlami a na něm kámen s basreliéfem takové jakési čajové konvice a kolem dokola tam má prvotřídní obložení ze světlé žuly, které stálo těžké peníze. Když zatoužím po hrobech, tak se jdu poveselit na ty naše. O cizí hroby nestojím. Jenom na ten váš se půjdu podívat, až vás pochovají. Víc pro vás udělat nemůžu.“
Rozbrečel se. A vykládal, že na jednom hrobě tam je kus kamene, o němž kdosi prohlásil, že je to pravděpodobně pozůstatek nějaké sochy, a na jednom náhrobku že je vytesáno pár slov, která nikdy nikdo nedokázal dešifrovat.
I teď jsem toho zatvrzelce odmítl, řekl tedy srdceryvným tónem: „To se nepudou podívat ani na naše památeční vokno?“
Ani na to jsem se nechtěl jít podívat, vypálil tedy svou poslední ránu. Přišoural se blíž a chraplavě mi našeptával:
„Dole v kryptě mám dvě lebky, aspoň na ty se dou kouknout. Prosím jich, dou se kouknout na ty lebky. Sou mladej, sou na prázdninách a jistě si chtěj něco užít. Dou se teda kouknout na ty lebky.“
Otočil jsem se a prchal, a v tom běhu jsem slyšel, jak za mnou volá:
„Dou se kouknout na ty lebky! Vrátěj se a dou se kouknout na ty lebky!“
Harris, ten si však v hrobech, hrobkách a náhrobních nápisech libuje a z pomyšlení, že neuvidí rov paní Thomasové, se div nezbláznil. Na to, že uvidí rov paní Thomasové, se prý těšil od první chvíle, co jsme začali plánovat tenhle výlet, vůbec by na něj prý nejel, nebýt naděje, že uvidí rov paní Thomasové.
Připomněl jsem mu George a že musíme být s lodí kolem páté v Sheppertonu, kde se s ním máme sejít, a Harris začal na George nasazovat psí hlavu. Jak to prý, že se George může celý den někde flákat a my dva se musíme sami dřít s touhle nemotornou, vratkou starou kocábkou proti proudu jen proto, že se s ním máme sejít? To nemohl taky přiložit ruku k dílu? To si nemohl vzít jeden den volna a jet rovnou s námi? Hromk aby bacil do té jeho banky! S ním to tam stejně nevytrhnou!
„Kdykoli tam za ním přijdu,“ rozhorloval se Harris, , jakživ nemá nic na práci. Celý den dřepí za sklem a jenom se snaží vypadat, jako že něco dělá. K čemu je takový chlap za sklem dobrý? Já musím pracovat, abych se uživil. Proč on teda nepracuje taky? Co je jim takhle v té bance platný? A k čemu jsou vůbec všechny ty banky dobré? Seberou tvoje peníze, a když na ně potom vystavíš šek, tak na něj načmárají „není krytí“, „obraťte se na výstavce“ a pošlou ti ho zpátky. Tak co z toho máš? A takovouhle lumpárnu mně minulý týden provedli dvakrát. Ale já už si to nedám líbit a svoje konto u nich zruším. Kdyby byl George s námi, mohli jsme se jít kouknout na ten náhrobek. Já ostatně ani nevěřím, že je v bance. Někde se flinká a tropí alotria, docela určitě! a všecku tuhle dřinu nechal na nás. Já vystoupím a půjdu se napít.“
Upozornil jsem ho, že od hospod jsme na hony daleko, a on začal nadávat na řeku a co on prý takhle z té řeky má a to prý každý, kdo si vyjede na řeku, má umřít žízní?
Když Harris začne takhle, tak je nejlepší se s ním nehádat. Za chvilku mu dojde dech a pomaloučku se uklidní. Jenom jsem mu připomněl, že v jednom koši máme láhev citrónové šťávy a ve špici lodi bandasku s vodou, a když se ty dvě věci smíchají, že je z toho studený a osvěžující nápoj.
Ale on hned vyletěl a navezl se i do citronády a do všech „podobných břečiček pro žáčky z nedělní školy“, jak tomu říkal, prostě do zázvorové limonády a malinového syrupu, atd. atd. To všecko prý jenom kazí trávení a ničí tělo a duši jakbysmet a je příčinou poloviny všech zločinů v Anglii.
Něčeho se však prý napít musí, i vylezl na lavičku a natáhl se pro tu láhev. Ta byla až na dně koše a nahmatat ji, to zřejmě nebylo jen tak, a Harris se tudíž musel naklánět a natahovat dál a dál, a jak se i při tom, ačkoliv byl hlavou dolů, snažil pořád kormidlovat, zatáhl za to druhé lanko a narval loď do břehu. Tím nárazem se překotil, zapíchl se do koše a zůstal v něm stát na hlavě, s rukama křečovitě svírajícíma luby lodi a s nohama trčícíma do vzduchu. Vůbec se neodvážil pohnout, protože měl strach, že sletí do řeky, a tak tam musel takhle trčet, dokud se mi nepodařilo popadnout ho za nohy a z toho koše ho vytáhnout, a to ho rozvzteklilo víc než všechno ostatní.
Vydírání - Jak na to správně reagovat - Sobecké hulvátství majitelů pobřežní půdy - Výstražné tabule - Nekřesťanské pocity Harrisovy - Jak Harris zpívá kuplety - Večírek ve vybrané společnosti - Hanebné chování dvou zlotřilých mladíků - Pár užitečných informací - George si kupuje bandžo.
VIII
Zastavili jsme se pod vrbami u Kempton Parku a obědvali jsme. Tam je to moc hezké; podél řeky se tam táhne příjemná travnatá rovinka, nad kterou se sklánění vrby. Zrovna jsme si dali třetí chod - bílý chléb s džemem -, když k nám přišel nějaký gentleman bez kabátu a s krátkou fajfkou a chtěl vědět, jestli my víme, že na tomhle pozemku nemáme co dělat. Odpověděli jsme mu, že jsme o té věci ještě natolik neuvažovali, abychom mohli dospět k definitivnímu úsudku, ale jestli nás jako gentleman ujistí čestným slovem, že tu nemáme co dělat, že mu bez váhání uvěříme.
To žádané ujištění nám dal a my jsme mu za ně poděkovali, ale on tam otálel dál a tvářil se neuspokojeně, tak jsme se ho zeptali, co ještě pro něho můžeme udělat; a Harris, který je vždycky ochoten se s každým skamarádit, mu nabídl krajíček chleba s džemem.
Ten chlap zřejmě patřil k nějaké sekte zapřisáhlých odpůrců chleba s džemem, neboť ho odmítl, a to velice hrubě, jako kdyby ho rozzlobilo, že jsme ho jím pokoušeli, a dodal, že má za povinnost nás odtud vyhnat.
Harris řekl, že jestli to má za povinnost, tak by to holt měl udělat, a zeptal se ho, jakým způsobem by se to podle jeho představ dalo provést nejlíp. Harris je to, čemu se říká „dobře udělaný mužský“, a vypadá jako samá kost a šlacha, a ten chlap si ho změřil od hlavy až k patě a prohlásil, že se půjde poradit se svým šéfem a pak že se vrátí a mrskne s námi do vody.
Toť se ví, že jsme ho už nikdy neviděli, a to, co vlastně chtěl, byl, toť se ví, šilink. U řeky je jich dost, takových hrubiánů, co si v létě přijdou na pěkné peníze tím, že courají po břehu a takhle vydírají nedovtipné bambuly. Tvrdí, že je posílá majitel pozemku. Na to máte správně reagovat tak, že tomu chlapovi dáte své jméno a adresu, a ať si vás majitel, když s tím má doopravdy něco společného, zažaluje a podá důkaz, jakou škodu jste mu na tom pozemku způsobili, když jste si na něj na jednom místě sedli. Jenže lidé jsou většinou tak hrozní lenoši a ustrašenci, že si tu lumpárnu raději nechají líbit, čímž ji jenom podporují, místo aby ji s trochou ráznosti jednou provždy zatrhli.
A když za to opravdu může majitel pozemku, pak by měl být veřejně pranýřován. To sobectví vlastníků pobřežní půdy rok od roku vzrůstá. Kdyby bylo po jejich, tak by snad uzavřeli celou řeku Temži. S jejími menšími přítoky a rameny to už beztak dělají. Do dna zarážejí kůly, od břehu ke břehu natahují řetězy a na kdejaký strom přitloukají obrovské výstražné tabule. Pohled na tyhle výstražné tabule probouzí v mé povaze všechny ty nejhorší pudy. To mám vždycky chuť tu tabuli strhnout a tak dlouho ji otloukat o hlavu toho, kdo jim tam dal, až ho zabiju a potom pochovám a tu tabuli mu budu moci dát na hrob jako pomník.
Svěřil jsem se s těmito svými pocity Harrisovi, a ten mi řekl, že on mívá pocity ještě horší. On prý toužívá nenen zabít toho člověka, který za vyvěšení takové tabule nese odpovědnost, ale zmasakrovat celou jeho rodinu a všechny jeho přátele a příbuzné a pak ještě vypálit jeho dům. To už se mi zdálo poněkud přehnané a taky jsem to Harrisovi řekl, ale on mi odpověděl: „Vůbec ne! To by takovému člověku po zásluze patřilo. A ještě bych na tom spáleništi zazpíval kuplet.“
Že v té své krvežíznivosti zachází Harris takhle daleko, to už mě znepokojilo. Nikdy bychom neměli připustit, aby se náš cit pro spravedlnost zvrhl v pouhou pomstychtivost. Velice dlouho mi trvalo, než jsem Harrise přiměl, aby v té věci zaujal křesťanštější stanovisko, ale nakonec se mi to přece jen podařilo a Harris slíbil, že tedy ušetří přátele a příbuzenstvo a že na spáleništi nebude zpívat kuplet.
Vy jste nikdy neslyšeli Harrise zpívat kuplet, takže nemůžete pochopit, jak velikou službu jsem tím prokázal lidstvu. Jednou z Harrisových utkvělých myšlenek totiž je, že umí zpívat kuplety; ti Harrisovi přátelé, kteří už zažili Harrisovy pokusy v tom směru, mají naopak utkvělou myšlenku, že to Harris neumí, že to nikdy umět nebude a že by se mu nikdy nemělo dovolit aby to zkoušel.
Když je Harris někde ve společnosti vyzván, aby zazpíval odvětí vždycky: „No prosím, ale já umím zpívat jenom kuplety,“ a řekne to tónem, který má naznačit, že jeho podání kupletů je něco, co byste si měli jednou poslechnout a pak umřít.
„Ale to je právě báječné,“ řekne hostitelka. „Zazpívejte nám nějaký, pane Harrisi.“ A Harris se zvedne a kráčí k pianu a září blahovůlí jako člověk nesmírně velkomyslný, který se zrovna chystá někoho něčím obdarovat.
„Teď se, prosím vás, všichni utište,“ volá hostitelka a točí se kolem dokola. „Pan Harris nám zazpívá nějaký kupletek.“
„Výborně!“ jásají hosté a spěchají ze zimní zahrady a přibíhají po schodech a shánějí po celém domě jeden druhého a cpou se do salónu a uvelebují se tam a už napřed se všichni uculují.
A Harris začíná.
Nu, u kupletisty nekoukáte tak moc na kvalitu hlasu. Neočekáváte přesný přednes ani bezvadnou vokalizaci. Nevadí vám, když ten člověk uprostřed tónu zjistí, že to vzal moc vysoko, a zničehonic přeskočí níž. Nestaráte se o nějaký rytmus. Nerozčílí vás, když je zpěvák o dva takty před doprovodem a uprostřed verše znanadání zpívat přestane, dohodne se s pianistou a pak s tím veršem začne ještě jednou. Ale počítáte s tím, že uslyšíte text.
Na to připraveni nejste, že si zpěvák nevzpomene na víc než na první tři řádky první sloky a ty že bude znova a znova opakovat, dokud nepřijde řada na refrén. Na to připraveni nejste, že se zpěvák uprostřed věty najednou odmlčí, uchichtne se a pak řekne, že to je sice naprosto nepochopitelné, ale zaboha prý si nemůže vzpomenout, jak je to dál, a pak že se bude chvilku snažit nějak si to v duchu sesumírovat a o hodně později že se na to najednou rozpomene, a to v okamžiku, když už je v docela jiné části písně, a že bez nejmenšího upozornění nechá zpívání a honem vám odříká zbytek toho dřívějšího verše. Na to připraveni - ale co, já vám to prostě předvedu, jak Harris zpívá kuplety, a udělejte si úsudek sami.
HARRIS (stojí před pianem a obrací se k nedočkavým zástupům): Víte, ona je to taková otřepaná věc, bohužel. Vy ji budete všichni znát, víte. Ale je to jediná věc, kterou umím. No je to prostě ta píseň soudce ze Zástěrky, víte - ne, ze Zástěrky ne, je to z toho - z toho - no však vy víte z čeho,- z té druhé operety prostě, no však vy víte. A refrén musíte všichni zpívat sborem, víte.
(Radostný šum v davu a dychtivost připojit se k zpívání refrénu. Nervózní pianista skvěle hraje předehru k písni soudce z Přelíčení před porotou. Nadchází okamžik, kdy Harris měl začít. Harris to nebere na vědomí. Nervózní pianista znovu začíná hrát předehru a současně Harris začíná zpívat a vychrlí první dva verše admirálovy písně ze Zástěrky. Nervózní pianista se ještě chvíli pokouší prosazovat tu předehru, pak toho nechá a snaží se dohonit Harrise doprovodem k písni soudce z Přelíčení před porotou, vidí, že ten doprovod nesedí, usiluje o to, aby si uvědomil, co to vlastně dělá a kde je, má dojem, že mu selhává rozum, a přestává hrát.)
HARRIS (ho vlídně povzbuzuje): Všecko v pořádku. Hrajete moc pěkně, vážně. Jen tak dál.
NERVÓZNÍ PIANISTA: Poslechněte, někde muselo dojít k omylu. Co to zpíváte?
HARRIS (bez nejmenšího zaváhání): No přece píseň soudce z Přelíčení před porotou. Copak vy ji neznáte?
JEDEN HARRISŮV PŘÍTEL (zezadu ze salónu): To nezpíváš, ty troubo, zpíváš admirálovu píseň ze Zástěrky.
(Dlouhé dohadování mezi Harrisem a Harrisovým přítelem, co to Harris vlastně zpívá. Přítel nakonec prohlásí, že je úplně jedno, co to Harris zpívá, pokud to Harris doopravdy zpívá, a Harris, v němž očividně hárá pocit utrpěného bezpráví, žádá pianistu, aby začal znova načež pianista hraje předehru k písni admirálově a Harris se chytí jednoho místa v hudbě, které považuje za zvlášť vhodné k nástupu, a spustí.)
HARRIS: Když j sem byl mlád
a udělal doktorát...
(Všeobecný výbuch smíchu, který si Harris vykládá jako kompliment. Pianista pomyslí na manželku a na celou svou rodinu, vzdává ten nerovný boj a odchází; a jeho místo zaujímá muž se silnějšími nervy.)
NOVÝ PIANISTA (bodře): Tak hele, kamaráde, vy prostě začnete a já se k vám připojím. S nějakou předehrou si nebudeme lámat hlavu.
HARRIS (zvolna mu svítá, v čem je vysvělení těch zmatků, a směje se): Prokristapána! Nezlobte se na mě, prosím vás. No jo, vždyť já jsem ty dva kuplety motal dohromady. To mě Jenkins tak popletl, víte. Tak jedem!
(Zpívá; jeho hlas zjevně vychází ze sklkepů a zní v něm nenápadné varování, že se blíží zemětřesení.)
Když jsem byl mlád, pracoval jsem v potu tváří coby poskok v advokátní kanceláři.
(Otočí se kpianistovi) Vzal jsem to moc hluboko, kamaráde. Začneme znova, prosím vás.
(Podruhé zpívá ty dva verše, tentokrát vysokým flasetem. Mohutný úžas v obecenstvu. Nervózní stará dáma poblíž krbu se dává do pláče a musí být odvedena ze salónu.)
Harris (zpívá dál):
Myl j sem tam okna, myl j sem tam dveře, myl j sem...
Ne ne, tak to není... Myl jsem tam okna zasklených dveří, myl jsem taky podlahy... ale ne, ksakru!
Jé, promiňte. To je zvláštní, na tenhle verš si najednou nemůžu vzpomenout. Myl jsem... myl jsem tam... ale to nic, riskneme to prostě a přejdeme rovnou k refrénu.
(Zpívá):
A nyní hejsa hejsa hejsa hejsa hejsasá mě pánem svého loďstva jmenovala královná.
No a teď všichni! Tyhle poslední dva verše opakovat sborem, víte.
SBOR: A nyní hej sa hej sa hej sa hej sa hej sasááá
ho pánem svého loďstva jmenovala královnááá.
A Harris si jakživ neuvědomí, jakého ze sebe dělá kašpara a jak otravuje spoustu lidí, kteří mu nijak neublížili. Vážně si představuje, že jim poskytuje vybraný požitek, a nabízí se, že po večeři zapěje další kuplet.
Když už mluvíme o kupletech a večírcích, tak si vzpomínám na jednu takovou pozoruhodnou příhodu, kterou jsem osobně zažil; ta příhoda vrhá dost světla na skryté duševní pochody v lidech, a tak si myslím, že by měla být na těchto stránkách zaznamenána.
Pořádal se večírek a byli jsme tam samí elegantní a vysoce kultivovaní lidé. měli jsme na sobě svoje nejlepší šaty, bavili jsme se vybraně a byli jsme velice spokojení - všichni až na dva mládence, studenty, kteří se právě vrátili z Německa; byli to takoví obyčejní chlapci a nikde neměli stání a zřejmě se necítili ve své kůži, jako kdyby měli dojem, že se na tom večírku pořád nic neděje. Ve skutečnosti to bylo tím, že jsme pro ně byli příliš rafinovaní. Nestačili prostě na naši duchaplnou a přitom uhlazenou konverzaci a na naše vytříbené záliby. Mezi námi nebyli prostě na místě. Neměli tam vůbec být. Na tom jsme se později všichni shodli.
Přehrávali jsme si morceaux od starých německých mistrů. Diskutovali jsme o filosofii a etice. Delikátně a důstojně jsme flirtovali. Dokonce jsme i vtipkovali - nesmírně vytříbeně ovšem.
Po večeři kdosi recitoval nějakou francouzskou báseň a my jsme prohlásili, že je překrásná; a potom jedna dáma zapěla španělsky takovou sentimentální baladu, a nad tou jeden či dva z nás zaplakali - byla tak dojemná.
A tu se zveli ti dva mladíci a zeptali se nás, jestli jsme někdy slyšeli profesora Slossenna Boschena (který prý právě přišel a je dole v jídelně) zpívat jeho slavný německý kuplet.
Pokud jsme se mohli upamatovat, neslyšel ho nikdo z nás.
Mladíci řekli, že je to nejlegračnější kuplet, jaký kdo kdy složil, a jestli prý chceme, požádají pana Slossenna Boschena, s kterým se výborně znají, aby nám ho zazpíval. Je prý tak legrační, že když ho pan Slossenn Boschen jednou zpíval německému císaři, museli ho (německého císaře totiž) odnést na lože.
A nikdo prý ten kuplet neumí zazpívat tak jako pan Slossenn Boschen; během celé té produkce zachovává tak hlubokou vážnost, až by se mohlo zdát, že zpívá něco tragického, ale tím větší je to ovšem legrace; ani přednesem ani chováním prý nikdy nenaznačí, že to, co zpívá, je vlastně veselé - tím by celý efekt pokazil. Právě pro ten jeho nesmírně vážný, skoro až dojemný výraz je ten kuplet tak neodolatelně směšný.
Řekli jsme, že si ho poslechneme s velikou chutí, že se potřebujeme pořádně zasmát, a oni tedy šli dolů pro pana Slossenna Boschena.
Ten zřejmě tu svou píseň zpíval velice rád, protože přišel okamžitě a beze slova usedl k pianu.
„Jé, to se pobavíte! To se nachechtáte! “ říkali tiše ti dva mladíci, když procházeli salónem, a pak zaujali nenápadné postavení za profesorovými zády.
Pan Slossenn Boschen se doprovázel sám. Předehra zrovna nenapovídala, že půjde o kuplet. Měla melodii spíš zasmušilou, až rozryvnou. Skoro z ní naskakovala husí kůže, ale my jsme si navzájem šeptali, že to je ta německá metoda, a chystali jsme se na velikou zábavu.
Já sám německy neumím. Ve škole jsem se němčině učil, ale do dvou let potom, co jsem vystudoval, jsem z ní všechna slovíčka zapomněl a od té doby je mi mnohem líp. Přesto jsem nechtěl, aby ti lidé tam tu moji nevědomost vytušili; a připadl jsem na nápad, který se mi zdál znamenitý. Nespouštěl jsem pohled z těch dvou studentů a dělal jsem všechno po nich. Když se pochechtávali, pochechtával jsem se taky; když smíchy řvali, řval jsem smíchy i já; a kromě toho jsem se ještě tu a tam polohlasně zasmál sám od sebe, jako kdybych byl postřehl drobnou perličku humoru, která ostatním unikla. To jsem pohládal za obzvlášť vychytralé.
Ale jak píseň pokračovala, stále jsem zjišťoval, že i spousta jiných lidí upírá oči na ty dva mládence zrovna tak jako já. I tihle lidé se pochechtávali, když se pochechtávali oba studenti, a řvali smíchy, když řvali smíchy oba studenti; a poněvadž oba studenti se při tom kupletu pochechtávali, řvali smích a vybuchovali v hlasitý řehot v jednom kuse, šlo všechno hladce jako po másle.
Přesto však ten německý profesor nevypadal spokojeně. Zprvu, když jsme se smát teprve začínali, vyjadřoval výraz jeho tváře nesmírný údiv, jako kdyby smích bylo to poslední, co očekával jako uznání. To nám připadalo velice směšné; a říkali jsme, že už jeho vážné chování tvoří polovinu celé té švandy. Kdyby jen sebeméně naznačil, že dobře ví, jakou hraje komedii, všechno by to hned pokazil. A když jsme se pak smáli víc a víc, údiv v jeho obličeji vystřídalo pobouření a rozhořčení a zlostně se na nás na všechny mračil (jenom na ty dva mladíky ne, jelikož ti stáli hned za ním, a nemohl je tedy vidět). To už nás rozchechtávalo tak, že jsme se svíjeli v křečích. Říkali jsme, že to bude naše smrt, tenhleten kuplet. Už sama ta slova, říkali jsme, stačí, aby se člověk válel po zemi, ale když je ještě navíc provází ta jeho markýrovaná vážnost - ne, to už se prostě nedá vydržet!
Při posledních verších se nadobro překonal. Škaredil se na nás s tak soustředěnou zuřivostí, že by nás byl velice znervóznil, kdyby se nám už napřed nebylo dostalo upozornění, že takhle se v Německu zpívají kuplety. A do té pochmurné hudby vložil tak kvílivý tón agónie, že nevědět, že je to písnička veselá, vyli bychom se snad rozplakali.
Dozpíval za burácejícího chechotu. Volali jsme, že to byla ta největší legrace, jakou jsme v životě slyšeli. Divili jsme se, jak se po něčem takovémhle pořád ještě může ve světě udržovat názor, že Němci nemají žádný smysl pro humor. A ptali jsme se profesora, proč ten kuplet nepřeložil do angličtiny, aby mu rozuměli i lidé nevzdělaní a mohli si poslechnout, jak pořádný kuplet má vlastně vypadat.
Tu se pan Slossenn Boschen zvedl a začal příšerně řádit. Nadával nám německy (řekl bych, že pro tento účel je to jazyk neobyčejně zdatný), poskakoval, zatínal pěsti a tupil nás vší angličtinou, kterou uměl. Křičel, že takto ho v životě nikdo neurazil.
Vyšlo najevo, že to, co zpíval, nebyl vůbec žádný kuplet. Bylo to o jedné dívčině, která žila v Harcu a obětovala vlastní život, aby spasila duši svého milého; ten taky umřel a setkal se s ní v oblacích; ale potom, v poslední sloce, dal té její duši kvinde a upláchnul jí s jinou duší - podrobnosti si už přesně nepamatuji, vím jenom, že to bylo něco hrozně smutného. Pan Boschen pravil, že když to jednou zpíval německému císaři, tak vzlykal (německý císař totiž) jako malé děcko. A dodal (pan Boschen totiž), že ta píseň je všeobecně uznávána za jednu nejtragičtějších a nejdojemnějších písní v německé řeči.
Byli jsme v trapné situaci - v moc trapné. Co jsme měli říkat? Rozhlíželi jsme se po těch dvou mládencích, co to celé nastrojili, ti se ale hned, jak ta píseň skončila, nenápadně vytratili z domu.
Takhle ten večírek skončil. V životě jsem už nezažil společnost, která by se rozcházela v takové tichosti a tak naprosto bez ceremonií. Ani jsme jeden druhému neřekli dobrou noc. Dolů jsme odcházeli po jednom, našlapovali jsme co nejměkčeji a drželi jsme se ve stínu. Sluhy jsme žádali o klobouky a pláště úplně šeptem, domovní dveře
jsme si otevřeli každý sám, jak jsme vyklouzli na ulici, honem jsme se hnali za roh a dělali jsme, co jsme mohli, abychom se jeden druhému vyhnuli.
Od té doby se nijak zvlášť nezajímám o německé písně.
V půl třetí jsme dojeli k sunburyskému zdymadlu. Ten kus Temže před jeho vraty je líbezně hezký a taky to druhé rameno řeky je půvabné; ale tím se nesnažte veslovat.
Já jsem se o to jednou pokusil. Seděl jsem tenkrát u vesel a ptal jsem se chlapců, co kormidlovali, jestli se to podle jejich názoru dá dokázat, a oni řekli, jistě, podle jejich názoru prý ano, musí se ovšem pořádně zabírat. To jsem byli zrovna pod lávkou pro pěší, co tam vede přes vodu, a já se opřel do vesel, sebral všechny své síly a dal se do toho.
Vesloval jsem úžasně. Nasadil jsem solidní, rovnoměrné tempo. Vložil jsem do toho paže, nohy i záda. Zabíral jsem rázně, hbitě, tvrdě, no, předvedl jsem styl skutku velkolepý. Moji dva přátelé říkali, že je radost se na mě dívat. Asi tak po pěti minutách jsem si myslel, že už musíme být skoro u jezu, a zvedl jsem hlavu. Byli jsme pořád pod tou lávkou, přesně na stejném místě, kde jsme byli, když jsem začal veslovat, a ti dva idioti se chechtali, div se neztrhali. Dřel jsem se totiž jako blázen, jen abych loď udržel pod tou lávkou. Od té doby nechávám ty druhé, aby v řečištích vedle plavebních kanálů veslovali proti proudu.
Teď jsme veslovali až k Waltonu, který je kupodivu dost veliký na to, že je to jen městečko u řeky. A jako všechna města u řeky i Walton vybíhá k vodě jen uzoučkým cípkem, takže z lodi máte dojem, že je to vesnice, která má všeho všudy půl tuctu domů. Jediná města mezi Londýnem a Oxfordem, z kterých z řeky něco uvidíte, jsou Windsor a Abingdon. Všechna ostatní se schovávají někde za rohem a na řeku vykukují jen jedinou ulicí; a já jim děkuju, že jsou tak ohleduplná a že břehy řeky ponechávají hájům a polím a vodním zařízením.
I Reading, ačkoli dělá, co může, aby poničil, pošpinil a zohyzdil ten kus řeky, na který dosáhne, má aspoň tolik slušnosti, že větší část své šeredné tváře schovává tak, aby nani od vody nebylo vidět.
Ve Waltonu si samozřejmě zřídil nějakou malou stanici Caesar - měl tam tábor nebo opevněné ležení nebo takového něco. Postupovat po řekách proti proudu - to bylo prostě jeho. A taky tam ovšem byla královna Alžběta. Té ženské neuniknete, ať se hnete kamkoli. A nějaký čas tam pobyli Cromwell a Bradshaw (ne ten Bradshaw, co sestavuje jízdní řády, nýbrž ten kat krále Karla I.) To mohla být dost zábavná parta, ti všichni dohromady.
Ve waltonském kostele mají železnou „uzdu pro štěkny“ Za dávných časů krotívali takovými věcmi ženské jazyky. Teď už se o to nikdo ani nepokouší. Železo je patrně čím dál vzácnější a jiné kovy na to nejsou dost pevné.
V tom kostele jsou také pamětihodné náhrobky a já se bál, že od nich Harrise vůbec nedostanu; ale zřejmě si na ně nevzpomněl a tak jsme jeli dál. Za mostem se řeka strašlivě kroutí, takže to tam vypadá malebně, jenže pro toho, kdo táhne loď nebo vesluje, je to jenom k zlosti a mezi ním a kormidelníkem to vyvolává samé hádky.
Jedete tu kolem Oatlands Parku, který je na pravém břehu. Slavné, starobylé sídlo. Někomu, nevím už komu, je ukradl Jindřich VIII. a žil si v něm. V zahradě je tam jeskyně, kterou si můžete za jistý poplatek prohlédnout a která je prý tuze krásná; ale já toho na ní moc nevidím. Nebožka vévodkyně z Yorku, která v Oatlandu žila, měla hrozně ráda psy a měla jich ohromné množství. A dala si pro ně zřídit zvláštní hřbitov, kde je všechny pochovala, když pošli; leží jich tam aspoň padesát a každý má nad sebou náhrobní kámen a na něm vytesaný epitaf.
A já si troufám říct, že ti psi si to zaslouží zrovna tak jako kterýkoli průměrný křesťan.
U „Corwayských kůlů“ - v prvním ohybu nad waltonským mostem - došlo k bitvě mezi Caesarem a Cassivellaunem. Cassivellaunus tady řeku pro Caesara nečekaně upravil: zarazil do ní spoustu kůlů (a bezpochyby na ně přitloukl výstražné tabule). Ale Caesar se přesto dostal na druhý břeh.
Caesara prostě nezaženete od téhle řeky ničím. Takových chlapů by teď bylo zapotřebí na těch soukromých ramenech.
Halliford a Shepperton jsou v těch svých končinách, jimiž se dotýkají řeky, docela pěkná městečka; ale jinak není ani na jednom z nich nic pozoruhodného. Na sheppertonském hřbitově je však jeden hrob s nějakou básničkou na pomníku, měl jsem tedy strach, aby Harris nechtěl vystoupit a motat se kolem něj. A když jsme se blížili k přístavišti, viděl jsem, jak na ně upírá roztoužené zraky. Ale podařilo se mi shodit mu obratným pohybem do řeky čapku a on v tom rozčilení, když ji lovil, a v tom rozhořčení, jak nadával na mou nešikovnost, na své milované hroby zapomněl.
Ve Weybridgi společně vékají do Temže Wey (moc hezká říčka, pro menší loďky splavná až do Guldfordu, jedna z těch, které se odjakživa chystám prozkoumat, k čemuž se nemůžu dostat), Bourne a Basingstonský průplav. Zdymadlo je hned naproti městu a první, co jsme spatřili, když jsme se k němu dostali na dohled, bylo Georgeovo sáčko nahoře nad vraty. Průzkumem z větší blízkosti pak vyšlo najevo, že v tom sáčku je i George.
Montmorency se dal do zuřivého štěkotu, já pokřikoval, Harris hulákal; George mával kloboukem a řval v odpověď. Strážce zdymadla se přiřítil s dlouhým hákem, neboť byl přesvědčen, že někdo spadl do zdýmací komory, a když zjistil, že tam nespadl nikdo, byl zjevně rozmrzelý.
George měl v ruce jakýsi prapodivný balíček ve voskovaném plátně. Na jednom konci byl zakulacený a po stranách plochý a z toho konce trčela taková dlouhá rovná rukojeť.
„Co to máš?“ volal Harris. „Pánev?“
„Ne,“ odvětil George a v očích mu podivně, divoce zablýsklo. „To je tuhle sezónu strašně v módě. Na řece už to má kdekdo. To je bandžo.“
„To ján nevěděl, že umíš hrát na bandžo!“ vykřikli jsme, Harris i já, jedním dechem.
„Vždyť já to taky neumím,“ odvětil George. „Ale prý je to velice lehké. A mám k tomu návod.“
Zapřeháme George do práce - Pohanské pudy vlečných lan - Nevděčné chování dvojsifu - Ti, co táhnou, a ti, co se vezou - K čemu se hodí milenci - Podivné zmizení postarší dámy - Velký spěch, malá rychlost - Ve vleku děvčat: vzrušující zážitek - Zmizelé zdymadlo aneb strašidelná řeka - Hudba - Zachráněni!
IX
Teď, když už byl s námi George, přinutili jsme ho pracovat. On pracovat nechtěl, samozřejmě; to bych ani nemusel říkat. Že prý se dost nadřel v bance, vykládal. Ale Harris, člověk v jádře necitelný a nelítostný, mu řekl:
„Tak teď se dost nadřeš na řece, pro změnu; změna, ta každému svědčí. A ven!“
George nemohl s dobrým svědomím - kde by taky dobré svědomí vzal? - nic namítat, vzmohl se pouzena poznámku, že by snad raději měl zůstat v lodi a vařit čaj a Harris a já že bychom ho měli táhnout, protože příprava čaje je práce strastiplná a Harris a já vypadáme unaveně. Jedinou naší odpovědí na to bylo, že jsme mu vložili do ruky vlečné lano, on se ho tedy chopil a vystoupil z lodi.
Vlečné lano, to je věc velice záhadná a nevypočitatelná. Stočíte je se stejnou péčí a trpělivostí, s jakou skládáte nové kalhoty, a o pět minut později, když si pro ně jdete, je z něho děsivá motanice, která vás pobouří až do hloubi duše.
Nechci pomlouvat, ale jsem pevně přesvědčen, že kdybyste vzali běžné vlečné lano, natáhli je rovně přes louku a pak se k němu na třicet vteřin otočili zády, zjistili byste, až byste se na ně zas podívali, že se smrsklo na jednu hromadu do prostředka té louky a tam že se všelijak propletlo, nadělalo na sobě uzle, ztratilo oba své konce a proměnilo se v samé smyčky; a že byste si k němu museli sednout do trávy a že by vám trvalo dobrou půlhodinku, než byste je za neustálého nadávání zase rozmotali.
Takové mínění já mám o vlečných lanech všeobecně. Je ovšem možné, že existují čestné výjimky; neříkám, že ne. Je možné, že existují vlečná lana, která svému poslání dělají čest - lana svědomitá, slušného chování - lana, která si o sobě nemyslí, že slouží k háčkování a nepokoušejí se, jak jen je necháte bez dohledu, ze sebe uštrikovat dečku na lenoch fotelu. Říkám, že je možné, že existují i taková lana; upřímně v to dufám. Ale zatím jsem se s žádným takovým nesetkal.
To naše vlečné lano jsem sám připravil, než jsme dojeli k zdymadlu. Harrisovi bych je byl nesvěřil, ten je nedbalý. Já sám jsem je zvolna a pozorně svinul, uprostřed převázal a jemně položil na podlážku lodi. Harris je potom odobrně zvedl a vložil do ruky Georgovi. George je pevně uchopil, držel je daleko od sebe a jal se je rozvíjet, jako kdyby odmotával plenky s novorozeněte; a ještě neodvinul ani celých dvanáct metrů a už se na věc víc než čemukoli jinému podobala mizerně upletené rohožce.
A takhle je to vždycky, s vlečným lanem to vždycky dopadne stejně. Ten na břehu, ten co se lano snaží rozmotat, si myslí, že to zavinil ten, co lano svinoval; a když si člověk na řece něco myslí, tak to taky řekne.
„Cos to s tím prováděl, to sis chtěl udělat rybářskou síť? Podívej se na ten mišmaš, co ti z toho vyšel! Tos to nemoh svinout jaksepatří, ty tajtrlíku pitomá?“ láteří co chvíli, když se s tím lanem zuřivě pere a rozkládá je po navigaci a lítá kolem dokola a marně hledá jeho konec.
Zatím ten, co lano svinoval, je přesvědčen, že všechnu vinu na tom propletenci má ten, co se snaží lano rozvinout.
„Bylo úplně v pořádku, než se dostalo do rukou tobě!“ křičí rozhořčeně. „Proč na to, co děláš, taky trochu nemyslíš? Proč se vším zacházíš tak humpolácky? Ty bys dokázal nadělat uzle i na trámu z lešení, od tebe by mě to nepřekvapilo.“
A mají takový vztek, že by nejradši jeden druhého na tom lanu pověsil. Uplyne deset minut a ten první začne řvát a zuřit, dupe po tom provaze a pokouší se ho rozmotat tím, že vždycky popadne tu první smyčku, co mu přijde pod ruku a škube za ni. Tím se samozřejmě celá ta spleť ještě pevněji zadrhuje. Načež ten z lodi vyleze na břeh a spěchá na pomoc a jeden druhému se plete do cesty a strašně překáží. Oba chňapnou po stejném úseku lana a každý za něj táhne opačným směrem a diví se, kde to vázne. Konečně to přece jen všecko rozmotají, otočí se a zjistí, že loď jim zatím sebral proud a unáší ji rovnou k jezu.
To jsem jednou, opravdu viděl na vlastní oči. Bylo to nad Boveney, za takového dost větrného jitra. Veslovali jsme dolů po proudu, a když jsme projeli tam tím ohybem, spatřili jsme na břehu dva muže. Koukali jeden na druhého tak zaraženě a s tak bezmocným zoufalstvím, že jsem podobný výraz v lidské tváři nikdy předtím ani potom neviděl, a třímali dlouhé vlečné lano. Hned nám bylo jasné, že se tady něco stalo, tak jsme přibrzdili a ptali jsme se těch mužů, co s nimi je.
„No co! Uplavala nám loď!“ zvolali dotčeně. „Jen teď jsme vylezli na břeh, abychom rozmotali vlečné lano, ohlédli j sme se - a loď už byla v tahu!“
A zřejmě to považovali za sprosťárnu a nevděk, to, co jim ta loď provedla, a nemile se jich to dotklo.
Našli jsme jim toho ulejváka asi o kilometr níž, v rákosí, které ho zachytilo, a dopravili jsme jim ho zpátky. Vsadil bych se, že nejmíň týden neposkytli té své lodi další příležitost k útěku.
Nikdy na ty dva nezapomenu, jak pobíhali sem a tam po tom břehu, tahali za sebou vlečné lano a koukali po své lodi.
Ve spojitosti s koníčkováním vidí člověk na řece spoustu legračních scén. Jednou z nejběžnějších je ta, jak si dva zapřažení svižně vykračují, zabráni do živé diskuse, zatímco ten, co se devadesát metrů za nimi veze v lodi, na
ně marně křií, aby se zastavili, a v naprostém zoufalství zuřivě mává veslem. Něco je někde nazadrmo; ulomilo se kormidlo, tyč s hákem sletěla přes palubu, nebo tomu v lodi spadl do vody klobouk a hbitě uplouvá po proudu. Ten, co se veze, zaráží ty dva nejdřív mile a zdvořile.
„Hej! Zastavte se na moment, prosím vás!“ volá bodře. „Sletěl mi do vody klobouk.“
Potom: „Hej! Tome! Dicku! Copak vám zalehly uši?“ To už méně přívětivě.
A pak: „Hej! Jděte do háje, vy blbouni bezhlaví! Hej, zastavte se! Ách, vy...!“
Načež se vymrští, poskakuje po lodi, řve, až je v obličeji celý rudý, a vystřídá všecky nadávky, které zná. A na břehu se zastavují malí kluci, dělají si z něho legraci a házejí po něm kamením, a on se veze kolem nich rychlostí šesti kilometrů za hodinu a nemůže z lodi ven.
Mnohým takovým trampotám by se zabránilo, kdyby ti, co táhnou, měli pořád na paměti, že táhnou někoho, a často se ohlíželi, aby viděli, jak se tomu, co se veze, daří. Nejlepší je, když táhne jen jeden. Jak táhnou dva, hned začnou žvanit a na všecko zapomenou a loď, ježto neklade moc velký odpor, nestačí sama na to, aby jim připomínala svou existenci.
Na doklad toho, jak nesmírné roztržitosti je schopen koníčkující páreček, nám později večer, když jsme se po jídle dali o té věci do diskuse, vyprávěl George velice pozoruhodnou příhodu.
On a ještě tři další muži se jednou večer snažili vyveslovat s velice těžce naloženou lodí od Maidenheadu nahoru a kousek nad cookhamským zdymadlem spatřili mládence a dívku, kteří šli po stezce podél vody, zabráni do zjevně zajímavého a poutavého rozhovoru. Drželi před sebou, každý oběma rukama, lodní bidlo s hákem a k prostředku toho bidla bylo přivázáno vlečné lano, které se plazilo za nimi s druhým koncem ve vodě. A nikde poblíž žádná loď. Ani nikde v dohledu žádná loď. Někdy dřív to lano nějakou loď držet muselo, to bylo nesporné; ale co se s ní stalo, jaký hrůzný osud ji stihl, ji i všechny ty, co byli v ní, to bylo pohřbeno v záhadách. Ale ať už to bylo jakékoli neštěstí, nikterak zřejmě neznepokojovalo tu slečnu a toho mladého pána, kteří koníčkovali. Měli bidlo s hákem, měli vlečné lano a očividně taky dojem, že nic jiného už k té své práci nepotřebují.
George už už na ně chtěl zavolat a probudit je, ale neudělal to, protože mu najednou bleskl hlavou báječný nápad. Vzal hák, natáhl se a konec toho lana vtáhl do lodi; pak na něm udělal smyčku, navlékli si ji na stěžeň, uklidili vesla, posadili se na záď a zapálili si dýmky.
A ten mládenec a ta dívka táhli ty čtyři kolohnáty a jejich těžkou loď až do Marlow.
George říkal, že v životě neviděl tolik úzkostiplného smutku soustředěného do jediného pohledu, jako dkyž si ten mladý páreček u zdymadla uvědomil, že poslední tři kilometry táhl cizí loď. George měl prý dojem, že nebýt umírňujícího vlivu té líbezné dívky, byl by snad ten mladý muž povolil uzdu velice hrubé řeči.
Dívenka se z toho údivu vzpamatovala první, spráskla ruce a zvolala zděšeně:
„Ale kde je tedy tetička, Henry?“
„A našli někdy tu starou dámu?“ ptal se Harris.
George odvětil, to že neví.
Jiného příkladu nebezpečného nedostatku porozumění mezi tím, co táhne, a tím, co se veze, jsme George a já byli jednou svědky kousek nad Waltonem. Bylo to v těch místech, kde se navigace tak mírně svažuje k řece. Tábořili jsme na protěším břehu a koukali jsme do neurčita. Tu se objevila malá loďka, kterou úžasně rychle táhl proti proudu mohutný kůň, na němž seděl malý kluk. V lodi se v ospalých, pohodlných pozicích rozvalovalo pět chlapíků, z nichž ten u kormidla vypadal obzvlášť líně.
„Rád bych viděl tu melu, kdyby zatáhl za nesprávnou šňůru,“ zabručel George, když nás míjeli. A přesně v tom okamžiku to ten člověk udělal a loď najela na břeh s takovým randálem, jako kdyby se naráz roztrhlo čtyřicet tisíc plátěných prostěradel. Dva muži, jeden koš a tři vesla okamžitě opustili plavidlo přes levobok a spočinuli na břehu a o jeden a půl okamžiku později se další dva muži vylodili přes pravobok a dosedli mezi bidla s háky a plachty a cestovní brašny a láhve. Poslední člen posádky pokračoval ještě asi dvacet metrů v plavbě a potom vystoupil po hlavě.
Lodi se tím zřejmě jaksi ulehčilo a plula dál ještě svižněji, zatímco ten klouček řval z plna hrdla a pobízel svého oře do kroku. Ti chlapíci se na zemi posadili a civěli jeden na druhého. Teprve za několik vteřin si uvědomili, co se to s nimi stalo, a začali ze všech sil řvát na toho kluka, aby zastavil. Ale ten neměl smysl pro nic jiného než pro koně, a tak je nemohl slyšet, a my jsme se za nimi dívali, jak za tím koněm uhánějí, dokud nám nezmizeli z očí.
Nemohu říci, že mi jich po tom karambolu bylo líto. Přál bych si dokonce, aby všechny ty mladé blázny, co se tímhle způsobem dávají vozit po řece - a je jich plno - potkal podobný malér. Nejen že se sami vydávají v nebezpečí, ale navíc ještě ohrožují a obtěžují všechny ostatní lodě. Při té rychlosti, kterou plují, se nikomu nemohou včas vyhnout a nikdo se nemůže včas vyhnout jim. Jejich lano se zachytí o váš stěžeň a převrhne vás, nebo sebere někoho ve vaší lodi a buď ho srazí do vody, nebo mu rozřízne tvář. Nejlepší je zůstat stát na místě, vyndat stěžeň a připravit se, že tím tlustším jeho koncem je odstrčíte od sebe.
Ze všeho, co můžete zažít při koníčkování, vás nic tak nevzruší, jako když vás táhnou děvčata. To je sanzace, kterou by si nikdo neměl nechat ujít. Holky musejí být na koníčkování vždycky tři; dvě drží lano a třetí pobíhá kolem nich a hihňá se. Obyčejně začnou tím, že se zašmodrchají do lana. Nějak si to lano omotají kolem nohou a musejí si sednout na cestičku a jedna druhou vysvobodit, ale při tom si zas udělají smyčky kolem krku a div se neuškrtí. Nakonec však lano přece jenom natáhnu a dají se v běh a táhnou loď tempem až nebezpečným. Než urazí sto metrů, jsou pochopitelně bez dechu, a tak se znenadání zastaví, dřepnou si do trávy a smějí se a vy se s lodí dostanete do silnějšího proudu a otočíte se, dřív než si uvědomíte, co se stalo, a než se můžete chopit vesel. A pak se holky zvednou a diví se.
„Hele!“ volají. „On nám zajel rovnou do prostředka.“
Pak chvilku táhnou loď docela slušně, ale po chvilce jednu z nich napadne, že by si měla trochu založit a zašpendlit sukni, a tak se za tím účelem zase všecky zastaví a loď najede na břeh.
Vyskočíte a odrazíte ji na vodu a voláte na dívky, aby se nezastavovaly.
„No? Co je?“ volají v odpověď.
„Nezastavujte se!“ křičíte.
„Cože?“
„Nezastavujte se! Jděte dál! Jděte pořád dál!“
„Běž k nim, Emily, a zeptej se, co to vlastně chtějí,“ řekne jedna z dívek; a Emily běží k nám a ptá se, co je.
„Co to vlastně chcete? ptá se. „Stalo se něco?“
„Ne,“ odpovíte, „všecko je v pořádku; jenom musíte jít dál, víte, nesmíte se zastavovat.“
„Proč ne?“
„No protože se nedá kormidlovat, když se pořád zastavujete. Musíte loď udržovat ve stejnoměrném švunku.“
„V čem?“
„Ve stejnoměrném švunku. No, musí mít pořád stejnoměrný pohyb.“
„Á, už chápu. Já to holkám řeknu. Děláme to dobře?“
„Ano, ano, moc. Vážně. Jenom se nezastavujte.“
„Ono to vlastně vůbec nic není. Já myslela, kdovíjaká je to dřina.“
„Kdepak, to je docela jednoduché. Jenom musíte táhnout pěkně stejnoměrně, to je všecko.“
„Já rozumím. Podejte mi tu mou červenou šálu, je támhle pod polštářkem.“
Najdete šálu, podáte ji té slečně, ale zatím už přiběhla i druhá a že prý by si taky vzala šálu, a pro všecko prý vezme i tu Maryinu. Ale Mary ji nechce, tak ji zas přinesou zpátky a místo ní chtějí hřebínek. Uplyne nemíň dvacet minut, než konečně zase odstartují, ale hned v příští zatáčce uvidí krávu a vy musíte vystoupit na břeh, abyste jim tu krávu odehnali z cesty.
Nu, když táhnou loď děvčata, tak se v ní ani chviličku nenudíte.
Po chvíli si George s lanem přece jen poradil a táhl nás bez přestávky až k Penton Hooku. Tam jsme prodiskutovali důležitou otázku táboření. Už jsme se rozhodli, že této noci přespíme na palubě, a šlo jen o to, máme-li se utábořit hned někde tady, nebo jet dál až za Staines. Ale ježto slunce bylo pořád ještě na obloze, nezdálo se nám, že už je čas myslet na to, jak se uložíme ke spánku, a tak jsme se domluvili, že dojedeme ještě o pět a půl kilometru dál, až k Runnymeadu, do tkové klidné, lesnaté končiny, kde se najdou pěkná, chráněná místečka.
Později jsme však moc litovali, že jsme nezůstali v Penton Hooku. Pět kilometrů proti proudu, to je časně po ránu hračka, ale když to máte vyveslovat na konci dlouhého dne, tak vás to pěkně utahá. Těch pár posledních kilometrů už nemáte zájem o krajinu. Už si nepovídáte a nesmějete se. Urazíte půl kilometru, a máte dojem, že to byly kilometry dva. Nechce se vám věřit, že jste teprve tam, kde jste, a jste přesvědčeni, že máte špatnou mapu; a když se už plahočíte takových, jak to odhadujete, nejmíň patnáct kilometrů a pořád ještě se před vámi nevynořilo zdymadlo, tak se naprosto vážně začínáte bát, že je někdo šlohnul a utekl s ním.
Vzpomínám si, jak jsem jednou byl na řece v troubě (myslím to obrazně, samozřejmě). Vyjel jsem si s jednou slečnou - sestřenicí z matčiny strany - a veslovali jsme po proudu ke Goringu. Bylo už dost pozdě a koukali jsme, abychom už byli doma - totiž ona koukala, aby už byla doma. Hodinky ukazovaly půl sedmé, když jsme dorazili k bensonskému zdymadlu, a začínalo se stmívat - a ta slečna ztrácela klid. Že prý musí být doma na večeři. Ujistil jsem ji, že u večeře bych sám chtěl sedět doma, a s ní, a vytáhl jsem mapu, kterou jsem měl s sebou, abych se podíval, jak to máme ještě daleko. Zjistil jsem, že něco přes dva kilometry máme k nejbližšímu zdymadlu - u Wallingfordu - a tamodtud osm kilometrů do Cleeve.
„Á, tak to stihneme,“ řekl jsem. „Nejbližším zdymadlem proplujeme ještě před sedmou a pak už nás čeká jenm jedno.“ A pohodlně jsem se usadil a začal vytrvale zabírat.
Projeli jsme pod mostem a já jsem se krátce potom té dívky ptal, jestli už vidí zdejší zdymadlo. Řekla, že nevidí žádné zdymadlo; já na to udělal „ale“ a vesloval jsem dál. Za pět minut jsem ji požádal, aby se znova dobře podívala před sebe.
„Ne,“ pravila, „po zdymadle ani památky.“
„A to... to víš jistě, že poznáš zdymadlo, když nějaké uvidíš?“ zeptal jsem se rozpačitě, nechtěje ji urazit.
Ale ji ten dotaz neurazil, jenom poznamenala, ať se raději podívám sám; a tak jsem položil vesla a rozhlédl jsem se. Řeka před námi ubíhala úplně rovně, v tom přítmí jsem ji mohl vidět do vzdálenosti půldruhého kilometru; po zdymadle ani potuchy.
„Nezabloudili jsme, poslechni?“ otázala se má společnice.
To jsem si nedovedl představit; mohli jsme ovšem, a to jsem jí naznačil, vjet nějakým omylem do hlavního řečiště a proud nás odnese k vodopádům.
Ta představa ji nikterak neutděšila a dala se do pláče. Tvrdila, že se oba utopíme, a pro ni prý je to trest za to, že si vyjela se mnou.
Já jsem měl za to, že by to byl trest přehnaný, ale má sestřenka soudila, že ne, a vyslovila naději, že to budeme mít odbyto co nej rychlej i.
Snažil jsem se jí dodat mysli a celou tu záhadu objasnit. Řekl jsem, že jsem zřejmě nevesloval tak rychle, jak jsem si představoval, ale teď že budeme u toho zdymadla co nevidět. A zabíral jsem další dva kilometry.
Pak už to i mne začalo znervózňovat. Znovu jsem se díval na mapu. Wallingfordské zdymadlo tam bylo jasně zakresleno dva a půl kilometru pod bensonským. A byla to dobrá, spolehlivá mapa; ostatně, sám jsem se na to zdymadlo pamatoval. Projel jsem jím už dvakrát. Kde jsme se to tedy octli? Co se to s námi stalo? Už jsem si začal myslet, že je to všechno jenom sen, ve skutečnosti že spím doma v posteli a že se za minutku probudím a dovím se, že je deset pryč.
Ptal jsem se své sestřenky, jestli si nemyslí, že je to všechno jenom sen, a ona mi odpověděla, že stejnou otázku chtěla zrovna položit mně; a pak jsme společně uvažovali, zda spíme oba, a jestli ano, kdo z nás je ta skutečná osoba, co sní, a kdo je ta snová vidina; bylo to čím dál tím zajímavější.
Ale já jsem přesto pořád vesloval dál a pořád nebylo nikde vidět zdymadlo a řeka byla v těch shlukujících se nočních stínech pořád pochmurnější a tajuplnější a všechno kolem nám připadalo pořád příšernější a obludnější. Musel jsem myslet na skřítky a divoženky a bludičky a na ty ničemné holky, co vysedávají celé noci na skalách a lákají lidi do vodních vírů a tak podobně; a vyčítal jsem si, proč jsem nebyl hodnější a proč jsem se nenaučil víc kostelních písní; a uprostřed těch úvah jsem uslyšel tahací harmoniku, na kterou někdo hrál - mizerně - oblažující melodii skladby „Ten je zasejc doběh“, a věděl jsem, že jsme zachráněni.
Nejsem zvláštním milovníkem tahací harmoniky; ale - ách! - tenkrát nám ty zvuky připadaly překrásné; mnohem krásnější než zpěv Orfeův nebo loutna Apollonova nebo jakékoli jiné podobné tóny. Nějaká nebeská melodie by nás v tom našem tehdejším duševním rozpoložení byla ještě víc zdrtila. Nějakou luznou, jímavou hudbu, dokonale zahranou, bychom byli považovali za harminii z onoho světa a byli bychom se vzdali všech nadějí. Ale v tónech toho „Ten je zasejc doběh“, jak je bez jakéhokoli rytmu a v nezamýšlených variacích chrlil ten dýchavičný akordeon, bylo něco jedinečně lidského a povzbudivého.
Ty lahodné zvuky se přibližovaly a za chviličku se těsně po našem boku octla loď, v níž byly provozovány.
Seděla v ní parta vesnických sekáčů a sekand, kteří podnikli plavbu při měsíčku. (Měsíček vůbec nesvítil, ale to nebyla jejich vina). V životě jsem neviděl roztomilejší, sympatičtější lidi. Pozdravil jsem je a zeptal se jich, jestli by mi nemohli říct, kudy se dostanu k wallingfordskému zdymadlu; a vyložil jsem jim, jak to zdymadlo už celé dvě hodiny marně hledám.
„Wallingfordský zdejmadlo?“ odpověděli. „Pánbu s váma, pane, to už je přeci přes rok zbouraný. To už je přeci pryč, wallingfordský zdejmadlo. A tady jste vostatně až skoro u Cleeve. To mě teda podrž, Bille, von ten pán hledá wallingfordský zdejmadlo!“
Něco takového mě vůbec nenapadlo. Nejraději bych je byl všecky popadl kolem krku a pomodlil se, aby jim Pánbůh požehnal; ale proud byl v těch místehc příliš prudký, aby mi to dovolil, a tak jsem se musel spokojit s chladně znějícími slovy díků.
Ale zato jsme jim děkovali pořád znova a znova a říkali jsme, jaký je hezký večer, a přáli jsme jim příjemnou plavbu, a já jsem je tuším všecky zval, aby na celý týden přijeli ke mně, a má sestřenice dodala, že její matinka by byla neskonale šťastná, kdyby se s nimi mohla seznámit. A pak jsme si zazpívali „sbor vojáků“ z Fausta a domů jsme přece jen dojeli ještě před večeří.
Naše první noc - Pod plachtou - Naléhavé volání o pomoc - Protivné chování čajových konvic, a jak mu čelit - Večeře - Jak získat pocit, že jsme ctnostní - Hledá se pohodlně vybavený, dobře vysušený neobydlený ostro, nejraději v končinách jižního Pacifiku - Komická příhoda Georgeova otce - Neklidná noc.
X
Harris a já jsme si už začínali myslet, že bellweirské zdymadlo taky nějak podobně zmizelo ze světa. George nás táhl až do Stainesu, a tamodtud jsme zase my táhli jeho a měli jsme dojem, že za sebou vlečeme padesát tun a že jsme tak ušli šedesát kilometrů. Bylo půl osmé když jsme konečně projeli zdymadlem. Všichni jsme nasedli do lodi, veslovali podél levého břehu a vyhlíželi místo, kde by se dalo přistát.
Původně jsme chtěli dojet až k ostrovu Magna Charta, který je v líbezně hezké končině, kde se řeka vine mělkým zeleným údolíčkem, a utábořit se v některé z těch mnoha zátok, co obklopují tu malinkou pevninku. Ale teď už jsme po nějaké malebnosti ani zdaleka tolik netoužili jako dřív za dne. Pro tuhle noc by nám byl docela stačil proužek vody mezi lodí na uhlí a plynárnou. Už jsme nepotřebovali žádnou scenérii. Potřebovali jsme se navečeřet a uložit ke spánku. Ale přece jen jsme doveslovali až k dolnímu výběžku ostrova - řečenému výběžek Výletníků - a zapadli jsme do velice příjemného koutečku pod mohutným jilmem, k jehož vyčnělým kořenům jsme připoutali loď.
Pak jsme si mysleli, že si dáme večeři (svačinu jsme si odpustili, abychom ušetřili čas), avšak George řekl nikoli; že prý bychom měli napřed natáhnout plachtu, prý dřív než se docela setmí, abychom na to viděli. Teprve potom, až se vší prací budeme hotovi, si prý můžeme s klidným svědomím sednout a najíst se.
To natažení plachty nám ale dalo mnohem víc práce, než jsme očekávali. A v představách to vypadalo tak jednoduše! Vezmete pět železných prutů ohnutých do oblouků, takže vypadají jako obrovské branky na kroketovém hřišti, vzklenete je nad lodí, napnete přes ně plachtu a dole ji upevníte; to může trvat takových deset minut, říkali jsme si.
Ale to byl odhad velice nedostatečný.
Vzali jsme ta žebra a snažili jsme se je zastrčit do příslušných zdířek. Určitě si nepředstvujete, že je to práce nebezpečná; ale když se teď na ni dívám s odstupem, pokládám za zázrak, že jsme ji všichni přežili a můžeme o ní vypravovat. To nebyly železné oblouky, to byli běsové. Nejdřív se jim vůbec nechtělo zapadnout do těch zdířek a my jsme museli po nich skákat a kopat do nich a mlátit je bidlem s hákem; a když konečně do těch otvorů zapadly, tak se ukázalo, že každý patřil do úplně jiných otvorů, a bylo nutno dostat je zase ven.
Ale ani ven se jim nechtělo, dokud jsme se na každý z nich nevrhli dva najednou a nesvedli s ním pětiminutový zápas, načež jeden po druhém vyskočili vždycky zcela znenadání a snažily se nás shodit do vody a utopit. V prostředku měly háčky, a když jsme se nedívali, tak nás těmi háčky štípaly do choulostivých částí těla; a zatímco jsme se rvali s jedním koncem oblouku a pokoušeli se ho přesvědčit, že musí konat svou povinnost, druhý konec se nám zbaběle dostal za záda a praštil nás přes hlavu.
Nakonec jsme je zasadili a už jenom zbývalo napnout přes ně plachtu. George ji porozvinul a jeden její konec připevnil ke špici lodi. Harris si stoupl doprostřed a měl plachtu převzít od George a rozvíjet ji dál až ke mně; já na ni čekal na zádi. Dostávala se ke mně velice dlouho. George se svého podílu zhostil docela dobře, ale pro Harrise to byla práce neznámá, a tak to všecko zbabral.
Jak to udělal, to nevím, on sám to vysvětlit nedovedl, ale po desetiminutovém nadlidském úsilí se mu nějakým tajuplným způsobem podařilo se do té plachty důkladně zavinout. Byl do ní tak pevně zamotán a zabalen a zastrkán, že se nemohl dostat ven. Podnikal pochopitelně zuřivý boj o svobodu - primární to právo každého Angličana - a při něm (jak jsme se dověděl později) povalil George; načež George začal za hrozných nadávek na adresu Harrisovu zápasit rovněž a i on se do té plachty zamotal a zavinul.
Já o tom všem tehdy vůbec nevěděl. Nechápal jsem, co se to děje. Bylo mi řečeno, abych se postavil tam, kam jsem se taky postavil, a abych čekal, až se plachta dostane ke mně, a tak jsem tam spolu s Montmorencym stál a poslušně čekal. Viděli jsme ovšem, jak se plachta vzdouvá a zmítá, a to značně, domnívali jsme se však, že to je součást pracovní metody, a nikterak jsme do toho nezasahovali.
Také jsme zpod plachty slyšeli všelijaké přidušené řeči a správně jsme vytušili, že těm dvěma to nejde moc od ruky, došli jsme tudíž k závěru, že počkáme, dokud se to všechno poněkud nezjednoduší, a teprve pak že se k práci připojíme.
A tak jsme nějaký čas čekali, ale situace nám připadala čím tál tím prekérnější, až se konečně na lodním boku vydrala ven z té změti Georgeova hlava a zahovořila.
Pravila:
„Tak nám přeci pomoz, ty blboune nejapná! Stojíš tady jako kus polena a musíš přeci vědět, že se tu dusíme, ty mumie vycpaná! “
Prosbu o pomoc nikdy nedokážu nevyslyšet, šel jsem je tedy vybalit; však byl už nejvyšší čas, Harris byl v obličeji skoro černý.
Pak nám to ještě dalo půl hodiny tvrdé námahy, než byla plachta řádně napnuta a teprve potom jsme na palubě poklidili a začali si připravovat večeři. Konvici s vodou jsme postavili na vařič až do špice lodi a odešli jsme ke kormidlu a dělali jsme, jako že si jí vůbec nevšímáme a že si chceme vypakovat ostatní věci.
To je jediný způsob, jak na řece přimět vodu v konvici, aby přišla do varu. Když vidí, že na to čekáte, že už jsme netrpěliví, tak ani nezabublá. Musíte jít od ní pryč, a dát se do jídla, jako kdybyste s čajem vůbec nepočítali. Ani ohlížet se po ní nesmíte. To ji pak za chvilku uslyšíte, jak klokotá, celá nedočkavá, aby už z ní byl čaj.
Dobře taky působí, když máte hodně naspěch, velice hlasitě říkat jeden druhému, jak nemáte na čaj vůbec chuť a že si ho ani nebudete dělat. To musíte být u té konvice hodně blízko, aby vás mohla slyšet, a pak musíte zařvat: „Já si dneska čaj nedám; co ty, Georgi?“ A George musí zařvat v odpověď: „Já taky ne, já čaj nerad; napijeme se radši limonády, čaj je těžko stravitelný.“ Načež se voda v konvici okamžitě začne tak prudce vařit, že přeteče a uhasí vařič.
Použili jsme tohoto nevinného triku a výsledek toho byl, že když všechno ostatní bylo hotovo, čaj na nás už čekal. Pak jsme si rozsvítili lucernu a po turecku jsme se sčapli k večeři.
Moc už jsme tu večeři potřebovali.
Celých pětatřicet minut se po celé délce i šířce lodi neozýval jiný zvuk kromě cinkotu příborů a nádobí a vytrvalého klapání čtyř chrupů. Koncem pětatřicáté minuty udělal Harris „ách“, vytáhl zpod sebe svou levou nohu a zastrčil tam místo ní pravou.
Po dalších pěti minutách udělal „ách“ i George a odhodil svůj talíř na břeh; a tři minuty potom dal - poprvé od okamžiku, kdy jsme vyjeli - také Montmorency najevo, že je spokojen, převall se na bok a natáhl nohy; a nakonec jsem i já udělal „ách“, zaklonil jsem hlavu, bacil jsem se do ní o jedno to železné žebro a vůbec mě to nedopálilo. Dokonce jsem ani nezasakroval.
Jak si člověk připadá dobrý, když je najeden - jak je spokojen sám se sebou i s celým světem! Pocit blaha a vyrovnanosti prý způsobuje čisté svědomí; to jsem slyšel od lidí, kteří to sami na sobě vyzkoušeli; ale plný žaludek vám to zařídí zrovna tak dobře a přijde vás to laciněji a dostanete se k tomu mnohem snáz. Po vydatném a dobře stráveném jídle si člověk připadá tak shovívavý a šlechetný, má tak vznešené myšlenky a tak laskavé srdce!
Stejně je to zvláštní, jak je náš intelekt ovládán našimi zažívacími orgány. Nejsme schopni pracovat, nejsem schopni myslet, dokud si to nepřeje náš žaludek. Ten nám diktuje naše emoce, naše vášně. Po slanině s vejci velí: „Pracuj!“ Po bifteku a černém pivu radí: „Spi!“ Po šálku čaje (dvě lžičky na jeden šálek a nenechávejte ustát déle než tři minuty) praví mozku: „Tak, a teď vzhůru a ukaž, co dokážeš! Buď výmluvný, bystrý, cituplný, dívej se na
přírodu a na život nepředpojatýma očima; rozepni bílá křádla rozdychtěné myšlenky a vzlétni jako bohorovný duch nad svět, který se točí pod tebou, vzlétni skrze dlouhé áleje zářících hvězd až k branám věčnosti!“
Po horkých koblihách praví: „Buď tupý a neinteligentní jako polní hovado - jako živočich bez mozku a s malátnýma očima, v nichž nezasvitne ani jediný paprsek obrazotvornosti, naděje, strachu, lásky, života.“ A po koňaku, požitém v dostatečném množství, vyzývá: „No tak pojď, ty tajdrdlíku, škleb se a dělej kotrmelce, ať se tvoji bližní zasmějou - plácej pitomosti a blekotej nssmysly a předveď, jak bezmocný kretének je chudák člověk, když se mu vtip a vůle utopily jako koťata v trošce alkoholu.“
Jsme prostě ti nejubožejší otroci svých žaludků. Nepachtěte se po tom, abyste byli mravní a pocitví, přátelé moji; jenom se bedlivě starejte o své žaludky a krmte je opatrně a uvážlivě. Pak ctnost a spokojenost samy vstoupí do vašich srdcí a budou jimi vládnout a vy nemusíte vynakládat žádné úsilí, abyste jich dosáhli; pak budete dobrými občany a milujícími manžely a něžně chápajícími otci - prostě ušlechtilými, zbožnými lidmi.
Harris, George a já jsme před tou večeří byli svárliví a kousaví a měli jsme mizernou náladu; po té večeři jsme klidně seděli a jeden na druhého jsme se usmívali, ba usmívali jsme se i na toho psa. Měli jsme se navzájem rádi, měli jsme prostě rádi kdekoho. Harris při jednom kroku šlápl Georgeovi na kuří oko. Stát se to před večeří, byl by George vyslovil taková přání a takové touhy stran Harrisova osudu na tomto i na onom světě, že by se i rozvážný člověk zachvěl.
Ale teď jen poznamenal: „Ouha, kamaráde, to je moje noha.“
A Harris, místo aby svým nej protivnějším tónem podotkl, že se člověk moc těžko vyhne tomu, aby šlápl na některou část Georgeovy nohy, když se musí pohybovat v okruhu deseti metrů do místa, kde Harris sedí, a místo toho, aby dále prohlásil, že George, když už má takhle dlouhé nohy, nemá v lodi normálních rozměrů co dělat, a že by bylo radno, aby je aspoň nechával viset přes luby do vody - což všechno by byl před večeří určitě řekl - místo toho se teď omluvil: „Jé, odpusť, člověče, doufám, že tě to moc nebolelo.“
A George řekl: „Ale ne, vůbec ne.“ A že prý to stejně byla jeho vina. A Harris řekl, kdepak, jeho vina to prý byla.
No bylo to moc hezké je poslouchat.
Zapálili jsme si dýmky a seděli jsme dál a vyhlíželi do tiché noci a povídali si.
George říkal, proč prý bychom nemohli žít neustále takto - daleko od světa, od jeho hříchů a pokušení, a trávit dny v střídmosti a mírumilovnosti a konat dobro. Já jsem se přiznal, že po něčem takovém jsem sám už často zatoužil; a pak jsme diskutovali, zda by nebylo možné, abychom odjeli, my čtyři, na nějaký příhodný, dobře vybavený neobydlený ostrov a žili tam v lesích.
Harris jen připoměl, že s neobydlenými ostrovy, aspoň pokud on slýchává, je jedna potíž, a to, že jsou hrozně vlhké; ale George pravil, že nemusí být vlhké, když se řádně odvodní a vysuší.
A tím jsme se dostali k problémům sucha a vláhy v širším i užším slova smyslu, a to v Georgeově mysli vyvolalo vzpomínku na velice legrační příhodu, kterou kdysi zažil jeho otec. A tak George začal vypravovat, jak jeho otec kdysi cestoval ještě s jedním chlapíkem po Walesu a jak jednoho večera zakotvili v malé hospůdce, kde už byli jiní chlapíci, a jak se s těmi chlapíky sčuchli a jak s nimi strávili celý večer.
Zažili večer velice rozjařený a dlouho byli vzhůru, a když se konečně zvedli, aby šli spat, byli i oni (přihodilo se to v době, kdy Georgeův otec byl ještě hodně mladý) dost rozjařeni. Měli spát (Georgeův otec a jeho přítel) v jednom pokoji, ale každý v jiné posteli. Vzali si svíčku a šli nahoru. Ta svíčka žďuchla do zdi, když se s ní dostali do pokoje, a zhasla, a tak jim nezbylo, než se odstrojit a dotápat se do postelí potmě. I dotápali se tam potmě; avšak místo aby si vlezli každý do jiné postele - což si mysleli, že se jim povedlo - doplazáli se oba, aniž si to uvědomili, do té samé, a to tak, že jeden se do ní dostal hlavou k hornímu konci a druhý se do ní vyšplahal z opačné strany a uložil se obráceně, nohama na polštář.
Chvilku bylo ticho a pak se ozval Georgeův otec:
„Joe!“
„Co je, Tome?“ ozval se z druhého konce postele hlas osloveného.
„U mě v posteli leží nějaký chlap,“ pravil Georgeův otec. „A má nohy na mém polštáři.“
„To je teda pěkná záhada, Tome,“ odpověděl ten druhý, „protože ať se propadnu, jestli taky nemám v posteli nějakého cizího chlapa.“
„A co s ním uděláš?“ zeptal se Georgeův otec.
„No, shodím ho na zem,“ odvětil Joe.
„Tak já taky,“ řekl udatně Georgeův otec.
Nastal krátký zápas, po němž následoval dvojí těžký bouchanec na podlahu a pak se ozval dosti zkormoucený hlas:
„Jářku, Tome!“
„Jo?“
„Jak jsi pořídil?“
„Inu, mám-li říci pravdu, ten chlap shodil z postele mě.“
„Ten můj taky! Jářku, mně se tahle hospoda zrovna moc nezamlouvá. A tobě?“
„Jak se jmenovala ta hospoda?“ zeptal se Harris.
„U prasete a píšťaly,“ řekl George. „Proč?“
„Á, tak nic, to není ta samá,“ odvětil Harris.
„O čem to mluvíš?“ vyzvídal George.
„Ale to je taková divná shoda,“ bručel Harris. „Přesně totéž se kdysi v jedné vesnické hospůdce stalo mému otci. Často nám o tom vypravoval. Tak jsem si myslel, jestli to nebylo v té samé hospodě.“
Toho večera jsme šli na kutě v deset hodin a já si myslel, že se mi bude výborně spát, když jsem tak unaven; ale nebylo tomu tak. Obyčejně se svléknu a položím hlavu na polštář a vtom někdo zabuší na dveře a volá, že je půl deváté; ale té noci jako by se proti mně všecko spiklo; že ten zážitek byl pro mě úplně nový, že loď byla tvrdá, že jsem byl celý zkroucený (ležel jsem nohama pod jedním sedátkem, a hlavou na druhém), že do lodi ze všech stran pleskala voda, že ve větvích šuměl vítr, to všechno mi bralo klid a spánek.
Na několik hodin jsem přece jen usnul, ale pak mě zas nějaká lodní součástka, která tam zřejmě vyrostla teprve v noci - protože tam určitě nebyla, když jsme vyjížděli, a ráno potom zmizela - v jednom kuse rýpala do páteře. Chvíli jsme při tom spal dál a zdálo se mi, že jsem spolknul zlaťák a že mi do zad vyvrtávají nebozezem díru, aby tu minci ze mne vytáhli. Připadalo mi to od nich moc nepěkné a říkal jsem, že jim snad ty peníze mohu zatím zůstat dlužen, že je koncem měsíce stejně dostanou. O tom však nechtěli ani slyšet, a že prý by si je mnohem radši vzali hned, protože jinak by se nahromadila spousta úroků. Brzy mě naštvali přespříliš a řekl jsem jim, co si o nich myslím, ale pak ve mně tím nebozlezem zašťourali tak bolestivě, že jsem se probudil.
V lodi bylo dusno a mě bolela hlava; tak jsem si řekl, že vylezu na chladný noční vzduch. Natáhl jsem na sebe ty kusy oblečení, které jsem mohl nahmatat - některé byly moje a některé Georgeovy a Harrisovy - a vysoukal jsem se zpod plachty na břeh.
Byla překrásná noc. Měsíc zapadl a pokojnou zemi nechal v samotě s hvězdami. A hvězdy jako by s ní, se svou sestrou, v tom tichu, v tom mlčení hovořily, jako by s ní, zatímco my, její děti, spíme, rozmlouvaly o náramných tajemstvích, a to hlasy takové hloubky a rozléhavosti, že dětské ucho lidí není schopno jejich zvuk zachytit.
Budí v nás posvátnou hrůzu, ty nezbadatelné hvězdy, tak studené, tak jasné. Jsme vskutku jako děti, jejichž nožičky zabloudily do nějakého osvětleného chrámu božstva, jemuž se učily vzdávat úctu, ale jež neznají; jako děti, které stojí pod kopulí plnou ozvěn, klenoucí se nad dalekými průhledy matným jasem, a hledí vzhůru, napůl v naději, napůl ve strachu, že uvidí, jak se tam vznáší nějaké děsivé zjevení.
A přesto poskytuje tolik útěchy a síly - taková noc. Před její vznešeností se naše drobné strasti zahanbeně odplíží pryč. Den byl samá trampota a starost, v našich srdcích bylo plno zloby a hořkosti a měli jsme pocit, že svět je k nám krutý a nespravedlivý. A potom přijde noc a jako nějaká přelaskavá, milující matka nám něžně položí dlaň na horečkou obolavěné čelo, zvedne náš uslzený obličejíček ke své tváři a usměje se, a třebaže nepromluví, my víme, co říká; a pak si přitiskne naše horké, rozpálené líce k ňadrům a bolest je tatam.
Někdy je však ta naše bolest nesmírně hluboká a vážná a tu stojíme před nocí v naprostém mlčení, protože pro to, co nás trýzní, není slov, jenom vzdychat se dá. A noc má pro nás v srdci plno soucitu; zmírnit naše utrpení, to nemůže; ale vezme nás za ruce a svět pod námi je najednou maličký a daleký a my se, unášeni jejími temnými křídly, octneme na okamžik v blízkosti ještě větší Moci, než jakou má ona, a v podivuhodné záři této nezuměrné Moci leží před námi celý lidský život jako kniha a my si uvědomujeme, že bolest a žal jsou jen andělé Boží.
A pouze ti, kteří měli někdy na hlavě korunu utrpení, mohou do té podivuhodné záře pohledět; a když se pak vrátí, nesmějí o ní hovořit a nesmějí prozradit tajemství, které teď znají.
Bylo jednou několik ušlechtilých rytířů, a ti projížděli cizí zemí a cesta je zavedla do hlubokého lesa, kde je husté a mohutné hloží drásalo až do krve a kde zabloudili. Listí stromů, které v tom lese rostly, bylo velice tmavé a husté, takže větvovím nepronikl ani jediný paprsek světla, který by zjasnil tu zasmušilost a tesknotu.
A jak se prodírali tím temným lesem, jeden ten rytíř ztratil náhle své druhy a zajel od nich příliš daleko a už se k nim nevrátil; a oni, těžce sklíčeni, jeli dál bez něho a oplakávali ho jako mrtvého.
Leč když dorazili k tomu spanilému hradu, k němuž měli namířeno, zůstali v něm mnoho dní a veselili se; a jednoho večera, když v bujaré bezstarostnosti seděli kolem polen rozhořelých ve slavnostní síni a pili na přátelství z poháru, kolujícího od úst k ústům, najednou se objevil ten jejich druh, který se jim ztratil, a vítal se s nimi. Šat měl rozedraný jako žebrák a jeho sličné tělo bylo poseto mnoha těžkými ranami, avšak z tváře mu mocně vyzařovala hluboká radost.
I vyslýchali ho a vyptávali se, co ho to potkalo; a on jim vyprávěl, jak v tom temném lese ztratil cestu a jak mnoho dní a nocí bloudil, až rozdrásán a krvácející ulehl na zem, aby zemřel.
A pak, když už byl smrti blízký, tu - ejhle! - k němu skrze tu ponurou divočinu přišla jakási urozená panna, vzala ho za ruku a vedla ho skrytými stezkami, neznámými smrtelníkům, až se v temnotě lesa rozbřesklo světlo, proti němuž světlo denní bylo jako lampička proti slunci; a v tom podivuhodném světle uviděl náš utrmácený rytíř jakoby ve snu zjevení a to zjevení mu připadalo tak velebné, tak čarokrásné, že zapomněl na své krvácející rány a stál tam jako ve extázi a pociťoval radost, hlubokou jako moře, jehož hloubku nedokáže nikdo odhadnout.
A zjevení se zase rozplynulo a rytíř, pokleknuv na zem, děkoval tomu dobrému světci, který v onom smutném lese svedl jeho kroky z cesty, a umožnil mu tak spatřit zjevení, jež tam přebývalo v skrytu.
Ten temný hvozd se jmenoval Žal; ale o tom zjevení, které v něm ten dobrý rytíř spatřil, o tom se nesmíme ani slůvkem zmínit.
Jak George, kdysi za dávných časů, vstal jednou časně zrána - George, Harris a Montmorency nesnášejí pohled na studenou vodu - J. projevuje heroismus a odhodlanost - George a jeho košile: příběh s mravním naučením - Harris coby kuchař - Historická retrospektiva, vložená sem speciálně pro školní potřebu.
XI
Ráno jsem se probudil v šest a zjistil jsem, že i George je už vzhůru. Oba jsme se otočili na druhý bok a pokoušeli se ještě usnout, ale nešlo nám to. Mít nějaký obzvláštní důvod, proč bychom neměli ještě znova usnout, proč bychom naopak měli vyskočit a honem se obléknout, to bychom byli upadli do spánku ještě při pohledu na hodinky a byli bychom spali až do deseti. Ježto však nebylo vůbec nutno, abychom vstali dřív než za dvě hodiny - přinejmenším - a vstávat už teď byl holý nesmysl, bylo zcela ve shodě s vrozenou ničemností věcí vůbec, že jsme oba měli pocit, že kdybychom zůstali ležet jen o pět minut déle, byla by to naše smrt.
George řekl, že něco podobného, jenže v mnohem horší podobě, se mu stalo asi tak před osmnácti měsíci, když ještě bydlel sám u jisté paní Gippingsové. Jednou večer se mu prý porouchaly hodinky a zastavily se na osmi patnácti. Ale on o tom nevěděl, protože je z nějakého důvodu zapomněl natáhnout, když šel spat (což se mu obyčejně nestávalo), a pověsil si je nad polštář, aniž se na ně podíval.
Stalo se to v zimě, když už byl přede dveřmi nejkratší den roku, a ještě k tomu v týdnu, kdy byla pořádn mlha, takže když se George ráno probudil a dosud byla úplná tma, nic ho nemohlo upozornit, kolik je vlastně hodin. Natáhl ruku po hodinkách a sundal je. Bylo osm patnáct.
„Stůjte při mně všichni svatí!“ vykřikl George. „Vždyť v devět musím být v bance! Proč mě nikdo nevzbudil? To je ostuda!“ Praštil hodinkami, vyletěl z postele, umyl se ve studené vodě, oblékl se, oholil se ve studené vodě, protože na teplou neměl čas čekat, a pak se ve spěchu ještě jednou podíval na hodinky.
Jestli to bylo tím otřesem, který utrpěly, když je prve odhodil na postel, nebo něčím jiným, to se George dodnes nedověděl, jisto však je, že se od těch osmi patnácti zase rozeběhly a teď ukazovalo za pět minut tři čtvrtě na devět.
George je popadl a hnal se dolů. V jídelně bylo ticho a tma; žádný oheň v kamnech, žádná snídaně. George si řekl, že tohle je od pí G. ničemná hanebnost, a umínil si, že jí večer, až se vrátí domů, poví, co si o ní myslí. Pak na sebe hodil zimník a klobouk, chytil deštník a běžel k domovním dveřím. Ty byly dosud na závoru. George dal pí G. jakožto línou starou bábu do klatby, divil se, proč lidi nedovedou vstát v slušnou, úctyhodnou hodinu, odemkl si a odstrčil závoru a vyřítil se ven.
Uháněl ze všech sil pár set metrů a teprve na konci vzdálenosti, kteoru urazil, mu začalo být divné, že je venku tak málo lidí a že nejsou otevřeny žádné obchody. Ráno bylo sice nesmírně temné a mlhavé, ale stejně mu připadalo neobvyklé, že se kvůli tomu zastavila veškerá činnost. On sám do práce musí; tak jak to, že si ostatní lidé hoví v posteli jenom proto, že je tma a mlha?
Posléze dorazil do Holbornu. Všude ještě rolety! A omnibus nikde! V dohledu bylijen tři lidé, z nichž jeden byl strážník, jedna kára naložená zelím pro tržiště a jedna polorozpadlá drožka. George vytáhl hodin,y a pohlédl na ně; za pět minut devět! Zastavil se a změřil si puls. Shýbl se a ohmatal si nohy. Pak, s hodinkami stále v ruce, přistoupil ke strážníkovi a zeptal se ho, jestli neví, kolik je hodin.
„Kolik je hodin?“ odvětil muž a s očividným podezřením si změřil George od hlavy až k patě. „Když budete poslouchat, tak je uslyšíte odbíjet.“
George tedy poslouchal a nějaké nedaleké hodiny mu za okamžik posloužily.
„Ale vždyť tloukly jenom třikrát!“ pravil George uraženým tónem, když odbíjení doznělo.
„A kolikrát podle vás měly tlouct?“ děl konstábl.
„No devětkrát,“ prohlásil George a předvedl svoje hodinky.
„Víte, kde bydlíte?“ zeptal se přísně strážce veřejného pořádku.
George se zamyslil a pak udal svou adresu.
„Aha, tam teda, jo?“ pravil strážník. „Tak dejte na mou radu, hezky pokojně se tam odeberte a ty hodinky si tam vemte s sebou. A už o nich nechci slyšet ani slovo.“
A tak se George zadumaně vrátil domů a zase si sám otevřel dům.
V první chvíli, když byl zas ve svém pokoji, byl rozhodnut se odstrojit a jít si ještě lehnout; ale když si
představil, jak by se pak musel znova obékat a koupat a mýt, řkel si, že si už lehnout nepůjde, že si jen trochu zdřímne v lenošce.
Ale nemohl usnout; v životě se necítil tak čilý; rozsvítil si tedy lampu, vytáhl si šachy a sám si zahrál jednu partii. Ale moc ho to neupoutalo; ta partie byla nějaká pomalá; nechal tedy šachů a pokoušel se číst. Leč ani četba ho nikterak nezaujala, a tak si zase oblékl zimník a vyšel si na procházku.
Byla to procházka příšerně osamělá a neutěšená a všichni strážníci, které potkal, se na něj dívali s netajeným
podezřením, svítili si na něj lampičkami a vždycky šli chivlku za ním, a to na něj mělo takový účinek, že nakonec začal mít pocit, jako by byl skutečně něco provedl, a jak zaslechl to přibližující se komisní ťap - ťap, pokaždé se okamžitě odkradl do postranní uličky a skryl se v nějakém temném výklenku.
Toto chování samozřejmě ještě zvýšilo nedůvěru policejního sboru vůči němu, a tak ho tahali z jeho úkrytů a vyptávali se, co to tam dělá; a když odpověděl, že nic, že si jen vyšel na malou procházku (byly čtyři hodiny ráno), dívali se na něj, jako by jim věšel na nos bulíky; a nakonec ho dva konstáblové v civilu vyprovodili až domů, aby
se přesvědčili, jestli doopravdy bydlí tam, kde říkal. Dívali se, jak si otvírá vlastním klíčem, a pak zaujali vyčkávací postavení na protějším chodníku a začali jeho dům hlídat.
George, když byl zase doma, chtěl - jen aby nějak utloukl čas - rozdělat oheň a uvařit si něco k snídani; ale nebyl schopen se čehikoli dotknout, uhlákem plným uhlí počínaje a čajovou lžičkou konče, aby to buď neupustil, nebo se přes to nepřerazil a nenadělal při tom hrozný randál, takže se ho zmocnila smrtelná hrůza, že se probudí pí G., že si bude myslet, že jsou u ní lupiči, že otevře okno a zařve „patról!“ a že ti dva tajní vtrhnou do domu, dají mu želízka a odvedou ho na strážnici.
Měl už tou dobou nervy v tak chorobném stavu, že si představoval, jak je s ním zavedeno přelíčení, jak se to všecko snaží vysvětlit porotě, jak mu nikdo nevěří, jak je odsuzován k dvaceti letům nucených prací a jeho matka jak uírá, protože jí puklo srdce. A tak zanechal všech pokusů udělat si snídani, zachumlal se do zimníku a seděl v lenošce, dokud o půl osmé nesešla dolů pí G.
Od toho rána prý už nikdy nevstal zbytečně časně; taková to pro něj byla výstraha!
To jsme seděli zabaleni v dekách, když mi George vyprávěl tuhle pravdivou příhodu, a když ji dovyprávěl, já jsem se podjal úkolu probudit veslem Harrise. Třetí rýpnutí mělo úspěch; ale Harris se jen převrátil na druhý bok a řekl, že bude dole v minutě a že se mu mají připravit ty šněrovací botky. Brzy jsme ho však s pomocí lodního háku poučili, kde ve skutečnosti je, a Harris se znenadání prudce posadil, takže Monmorency, který spal spánkem spravedlivého na jeho prsou, odletěl jak široký tak dlouhý na druhý konec lodi.
Pak jsme svinuli plachtu a všichni čtyři jsme vystrčili hlavu přes bok lodi, podívali jsme se na vodu a zachvěli jsme se. Včera večer jsme se opájeli vidinou, jak vstaneme za časného jitra, odhodíme deky a šály, stáhneme plachtu, s bujarým pokřikem skočíme do řeky a s velikým gustem si dlouze, příjemně zaplaveme. Teď však, když to jitro nastalo, se nám ta vyhlídka už nezdála tak lákavá. Voda vypadala mokře a mrazivě a vítr studil.
„Tak co, kdo tam půjde první?“ ozval se posléze Harris.
O to prvenství se nikdo nedral. George, ten se s tím problémem vypořádal tak, že se uklidil do lodi a začal si oblékat ponožky. Montmorendymu uklouzlo bezděčné zavytí, jako kdyby už pouhá myšlenka na vodu mu naháněla hrůzu. A Harris řekl, že by bylo velice nesnadné vylézt zpátky do lodě, a šel si vyhrabat svoje kalhoty.
Já jsem se jen tak lehce vzdát nechtěl, i když mě skok do té vody nikterak nevábil.
Mohou tam být kořeny stromů nebo vodní řady, říkal jsem si. Rozhodl jsem se to tedy řešit kompromisem, že totiž sejdu na samý kraj břehu a aspoň se celý ošplíchám; a tak jsem si vzal ručník, vylezl jsem z lodi a vysoukal se na větev jednoho stromu, která se ohýbala až do vody.
Byla štiplavá zima. Vítr řezal jako nůž. Řekl jsem si, že se radši neošplíchám. Že se vrátím na loď a obléknu se; i otočil jsem se, abych mohl lézt zpátky, a zrovna když jsem se otáček, ta pitomá větev rupla a já a ručník jsme s ohromným žblunknutím sletěli do řeky, a než jsem si mohl uvědomit, co se to vlastně stalo, už jsem byl uprostřed proudu a měl jsem v sobě přes čtyři litry temžské vody.
„No ne! J. tam skočil!“ slyšel jsem z úst Harrisových, když jsem se, sotva dechu popadaje, vynořil nad hladinu. „To bych byl teda neřekl, že k tomu sebere odvahu. Ty ano?“
„Je to fajn?“ křikl na mě George.
„Báseň!“ prskal jsem. „Jste pitomci, že sem neskočíte taky. Já bych si to nedal ujít za nic na světě. Jen si to okuste! To nechce nic jiného než kapánek odhodlání.“
Ale nepřemluvil jsem je.
Při oblékání jsme toho rána zažili moc zábavnou příhodu. Mně bylo strašně zima, když jsem se dostal zpátky na loď, a jak jsem honem koukal, abych už měl něco na sobě, nešikovně jsem si shodil do vody košili. Příšerně mě to rozvzteklilo, zvláště když George se dal do smíchu. Mně se nezdálo, že by na tom bylo něco k smíchu, a taky jsem to Georgeovi řekl, ale on se smál tím víc. To jsem v životě neviděl aby se někdo takhle smál. Nakonec mě úplně připravil o rozvahu a já jsem mu důrazně připomněl, že je debilní kretén a slabomyslný idiot; ale on se řehtal ještě hlasitěji. A pak, když jsem tu košili lovil z vody, jsem si najednou uvědomil, že to není moje košile, ale Georgeova, a že jsem si ji s tou svou jenom spletl; teď teprve mi celý ten vtip došel a rozesmál jsem se já. A čím častěji jsem pohledem skouzl s Georgeovy mokré košile na George, který smíchy zrovna řval, tím větší jsem z toho měl zábavu a tak hrozně jsem se rozchechtal, že jsem tu košili znova upustil do řeky.
„T... t... to si ji - ani - nevytáhneš?“ křičel George mezi výbuchy smíchu.
Hodnou chvíli jsem mu vůbec nebyl schopen odpovědět, tak jsem se smál, ale nakonec se mi i při tom řehotu přece jen podařilo vyprsknout:
„To není moje košile - to je tvoje!“
V životě jsem ve výrazu žádné lidské tváře neviděl tak náhlý přechod z veselosti do neúprosné vážnosti.
„Cože?“ zaječel a vyskočil. „Ty blboune nemotorná! To by tě ubylo, kdybys dával trošku pozor? Proč se ksakru nejdeš obléknout na břeh? Ty tak patříš na loď, ty teda jo! Dej sem hák!“
Pokoušel jsem se ho přesvědčit, jaká to je legrace, ale on to nechápal. Pokud jde o vtipy, mívá George hrozně dlouhé vedení.
Harris navrhoval, abychom si k snídani dali míchaná vajíčka. A prohlásil, že je udělá. A z toho, jak je líčil, jsme nabyli dojmu, že na míchaná vajíčka je vyslovený mistr. Často prý je připravuje, když je někde na pikniku nebo na jachtě. Prý se jimi doslova proslavil. Ti, kdož jeho míchaná vajíčka jednou okusili - to bylo z jeho řečí zcela jasné - nestáli už o žádné jiné jídlo, a když nemohli mít Harrisova míchaná vajíčka, zvolna chřadli a zmírali.
Když jsme ho tak poslouchali, sbíhaly se nám v ústech sliny; i vyndali jsme mu vařič a pánev a veškerá vajíčka, která se v koši nerozmačkala a nevytekla na všechno kolem, a prosili jsme ho, ať už se do toho dá.
Rozbíjení vajíček mu trochu dělalo potíže - totiž ne, rozbíjení samo mu potíže nedělalo, spíš to, jak obsah těch rozbitých vajíček dostat na pánev, jak si jím nepokecat kalhoty a jak zabránit, aby mu netekl do rukávů; ale posléze přece jen asi půl tuctu vajíček umístil v pánvi; načež si přičapl na bobek k vařiči a proháněl vajíčka vidličkou.
To byla zřejmě práce velice svízelná, pokud jsme to my dva, George a já, mohli posoudit. Kdykoli se Harris přiblížil k pánvi, vždycky se spálil a pak hned všecko upustil a roztancoval se kolem vařiče a třepal prsty a sakroval. Namouduši, kdykoli jsme se na něj ohlédli, pokaždé přecváděl tenhle výstup. Zpočátku jsme si dokonce mysleli, že to je nezbytná součást kulinárního ceremonielu.
Říkali jsme si, že patrně nevíme, co to jsou míchaná vajíčka, a usoudili jsme, že to musí být nějaký pokrm Indiánů nebo Kanaků z Havajských ostrovů, který, má-li být řádně připraven, vyžaduje tance a různá zaklínání. Jednou se k pánvi přiblížil Montmorency a přistrčil k ní nos, a tu vyprskl omastek a přismahl ho, načež i Montmorency začal tancovat a sakrovat. No byla to prostě jedna z nejpozoruhodnějších a nejvzrušivějších pracovních operací, jakých jsem byl kdy svědkem. George i já jsme byli velice zarmouceni, když skončila.
Výsledek nebyl ani zdaleka tak zdařilý, jak Harris předpovídal. Dost malý efekt na všechen ten rámus. Šest vajíček se dalo na tu pánev a všechno, co z toho vzešlo, byla jedna lžička uškvařených a nechutně vypadajících cucků.
Harris tvrdil, že za to může ta pánev; kdyby byl měl kastrůlek a vařič na plyn, tak to prý dopadlo mnohem líp. Dohodli jmse se tudíž, že o tuto krmi se už nebudeme pokoušet, dokud s sebou nebudeme mít zmíněné potřeby pro domácnost.
Než jsem dosnídali, slunce už mělo větší moc a vítr ustal a bylo tak rozkošné ráno, jaké jen si možno přát. Z toho, co jsme kolem sebe viděli, nám skoro nic nepřipomínalo devatenácté století; a když jsme se v tom ranním slunci rozhlíželi po řece, mohli jsme si bezmála představovat, že ta staletí mezi dneškem a oním provždy památným červnovým jitrem z roku 1215 byla odsunuta stranou a že my, synové anglických zemanů, tu v odění utkaném podomácku a s kýkami za pasy čekáme, abychom byli svědky při psaní té úžasné stránky dějin, jejíž význam má teprve za nějakých čtyři sta let vyložit prostému lidu jakýsi Oliver Cromwell, který si ji důkladně prostuduje.
Je krásné letní ráno - slunné, a klidné. Ale ve vzduchu už je cítit napětí nadcházejících vzrušujících okamžiků. Král Jan přespal v Duncroft Hallu a městečko Staines se po celý včerejšek ozývalo řinečním ozbrojenců a dusotem těžkých koní po hrbolatém dláždění a pokřikem kapitánů a zlosnými nadávkami a otrlými žerty vousatých lučištníků, piknerů, ratištníků a podivně mluvících cizokrajných kopiníků.
Přijely sem skupiny rytířů a majitelů panství v pláštích veselých barev, teť po cestě umazaných a zaprášených A ustrašení měšťané museli po celý ten večer hezky zčerstva otvírat vrata hloučkům neurvalých vojáků, pro které se muselo najít ubytování a jídlo, a obojí to nejlepší, jinak běda domu a všem, co v něm přebývají; neboť v těchto bouřlivých dobách meč je soudcem, porotou, žalobcem i katem a za to, co si vezme, zaplatí, když se mu ovšem uráčí, tím, že ty, které obral, nechá naživu.
Kolem táborových ohňů na tržišti se shukují další a další baronské čety, a zatímco večer tmavne a přechází do noci, vojáci nenasytně jedí a pijí a vyřvávají chvastounské pijácké písně a hrají a hádají se. Zář ohňů vrhá pitvorné stíny na kupy jejich odložené zbroje a na jejich hromotlucké postavy. Kolem nich pokradmu obcházejí děti z městečka a zvědavě je okukují; a ještě blíž se stahují statné venkovské holky a se smíchem si vyměňují hospodské žertíky a jízlivosti s těmi nafoukanými kavaleristy, tak nepodobnými vesnickým chasníkům, kteří teď, zcela přehlíženi, stojí někde v pozadí a na širokých zevlujících obličejích mají přitroublé úsměvy. A z okolních plání sem slabě jiskří světla dalších vzdálenějších ležení, neboť tuhle se utábořili přívrženci nějakého mocného pána a tamhle zase jako přikrčení vlci vyčkávají za městem francouzští žoldáci proradného Jana.
A tak, zatímco v každé temné uličce pochodovaly hlídky a na všech okolních návrších blikaly strážní ohně, zvolna odešla noc a nad tím spanilým údolím kolem staré Temže se rozbřesklo jitro toho velikého dne, který má být těhotný osudem věků dosud nezrozených.
Na spodnějším z obou ostrůvků, zrovna nad místem, kde jsme se zastavili, je už od šedého svítání nesmírně rušno a znějí tam hlasy mnoha řemeslníků. Vztyčuje se tam obrovský stan, který tam byl dopraven už včera večer, a tesaři pilně stloukají řady lavic, zatímco učedníci z města Londýna čekají s petrobarevnými tkaninami a hedvábím a přehozy ze zlata a stříbra.
A teď, hle! po cestě, která se podél řeky vidne od Stainesu, k nám přichází asi deset mohutných halapartníků - jsou to vojáci baronů -, kteří se smějí a baví se hlubokými hrdelními basy; zastavují se na tom druhém břehu asi tak sto metrů nad námi a čekají, opřeni o své zbraně.
A tak po celé hodiny pochodují od Stainesu nové a nové hloučky a houfy ozbrojenců, jejichž přílby a náprsní pancíře odrážejí dlouhé, nízko nad zemí letící paprsky ranního slunce, až konečně celá ta cesta, kam oko dohlédne, je hustě poseta třpytící se ocelí a vzpínajícími se oři. A od hloučku k hloučku cválají křičící jezdci a v teplém vánku se lenivě třepotají malé korouhvičky a chvílemi nastane ještě větší rozruch, když se ty šiky rozestupují do obou stran, aby nějaký významný baron, sedící na obrněném koni a obklopený strážemi a panstvem, mohl projet a zaujmout své místo v čele svých nevolníků a vazalů.
A po celém svahu Cooper’s Hillu, zrovna naproti, stojí zásupy udivených venkovanů a zvědavých občanů, kteří přiběhli ze Stainesu, a nikdo z nich dobře neví, co ten shon vlastně znamená, a každý si tu slavnou událost, kterou chce vidět, vykládá po svém; někdo říká, že z toho, co se dneska děje, vzejde mnoho dobrého pro všechen lid, ale starci nad tím kroutí hlavou, protože ti už takové řeči slyšeli mnohokrát.
A celá řeka až po Staines je poseta pramičkami a loďmi a malými člunky z kůže - ty kožené už upadají v nemilost a dneska jich užívají jen chudší lidé. Všechna ta plavidla převezli nebo přetáhli svalnatí veslaři přes peřeje, na jejichž místě bude po letech stát úhledné zdymadlo Bell Weir, a teď se s nimi shlukují v nej větší blízkosti, na jakou si troufnou, kolem ohromných krytých lodic, které jsou tu připraveny, aby odvezly krále Jana tam, kde na jeho podpis čeká ta osudová Charta.
Je poledne a my spolu se vším lidem trpělivě čekáme už hodiny a už se začínají šířit zvěsti, že věrolomný Jan znovu unikl z moci baronů a se svými žoldáky v patách se vykradl z Duncroft Hallu a že se brzy pustí do jiných akcí, než je podpisování listin zaručujících svobodu lidu.
Ale není tomu tak! Tentokrát ho drží pěst z ocele a zcela marně se snaží vykroutit a vyklouznout. V dálce na cestě se zvedl oblak prachu, blíží se a zvětšuje, dusit mnoha kopyt zní stále silněji a mezi sešikovanými vojáky, rozestupujícími se na dvě strany, si razí průjezd oslnivá kavaláda pestře oděných lordů a rytířů. A vpředu i vzadu i po obou stranách toho průvodu jede tělesná stráž baronů a v jejich středu král Jan.
Jede tam, kde čekají lodice, a význační baroni vystupují z oddílů svých vojáků a jdou mu vstříc. A on je zdraví s úsměvy a smíchem a přívětivými slovy, jako kdyby to byla nějaká slavnost na jeho počest, to, k čemu byl pozván. Avšek když se zvedne v třmenech, aby sestoupil z koně, přeletí jediným rychlým pohledem od svých francouzských žoldáků, seřazených v pozadí, k neúprosným oddílům baronským, jimiž je sevřen.
Opravdu je už pozdě? Jedna prudká rána tomu jezdci, kterého má po boku a který nic takového nečeká, jeden hlasitý povel francouzským četám, jeden troufalý útok na nepřipravené šiky před ním - a ti odbojní baroni by možná proklínali den, kdy se odvážili zkřížit jeho plány! Smělejší ruka mohla ještě v tomto okamžiku zvrátit situaci. Být na jeho místě takový Richard! Pak pohár svobody mohl být ještě odtržen od rtů Anglie, takže by ještě stovky let nepoznala, jak chutná volnost.
Ale srdce krále Jana ochabuje před těmi odhodlanými tvářemi anglických bojovníků a jeho paže klesá zpátky k otěžím. Jan sestupuje s koně a usedá do první lodice. A baroni, každý ruku v železné rukavici na jilci meče, tam jdou za ním a zazní povel k odplutí.
Zvolna odrážejí těžké, skvostně vyšňořené lodice od runnymedského břehu. Zvolna a s námanou si razí cestu hbitým proudem, až s temným zaduněním narážejí na ostrůvek, který od toho dne ponese jméno ostrov Magna Charta. A král Jan vystupuje na břeh a my v bezdechém mlčení čekáme, až vzduch rozčísne obrovský jásot a bude položen - pevně, jak už dnes víme - ten slavný základní kámen anglického chrámu svobody.
Jindřich VIII. a Anna Boleynová - Jaké to má nevýhody, když pobýváme v jednom domě s párkem milenců - Krušné doby pro anglický národ - Noční hledání malebnosti - Bez domu i bez domova - Harris se připravuje na smrt - Přichází anděl - Jak působí na Harrise nenadálá radost - Skrovná večeře - Oběd - Vysoká cena za hořčici - Děsivá bitva - Maidenhead - Plachtíme - Tři rybáři - Jsme stiženi kletbou.
XII
Seděl j sem na břehu a v duchu j sem si vyvolával tuto scénu, když tu George poznamenal, že j elikož j sem si tak pěkně odpočinul, mohl bych snad být tak laskav a pomoci při umývání nádobí; přivolán takto ze dnů slavné minulosti do prozaické přítomnosti s veškerou její mizerií a hříšností, sesunul jsem se do lodi a kouskem dřeva a chomáčem trávy jsem vyčistil pánev a nakonec jsem ji vyleštil Georgeovou mokrou košilí.
Pak jsme si vyšli na ostrov Magna Charta a podívali se na kámen, který stojí v chatrči, kde prý ta slavná listina byla podepsána; ale jestli byla opravdu podepsána tam, nebo, jak se taky tvrdí, na druhém břehu v Runnymede, k tomu já se odmítám závazně vyslovit. Ptáte-li se mne jenom na mé soukromé mínění, pak se spíš přikláním k té lidové verzi, podle níž to bylo na ostrově. Být jedním z těch tehdejších baronů, určitě bych byl svým druhům důrazně doporučoval, abychom tak úskočného chlapa jako král Jan dostali na ostrov, kde má míň možností zahrát nám nějaký překvapující kousek.
Na pozemcích panství Ankerwyke, které je kousek od výběžku Výletníků, stojí rozvaliny starého kláštera, a tam někde prý čekával Jindřich VIII. na Annu Boleynovou. Taky se s ní scházíval v zámku Hever v Kentu a taky někde poblíž městečka St. Albans. V oněch dnech muselo být pro anglické občany dost těžké najít místečko, kde by ti dva lehkomyslní mladí lidé spolu nelaškovali.
Pobývali jste někdy v domě, kde je zamilovaný páreček? To jde dost na nervy. Řeknete si, že si půjdete sednout do salónu, a vydáte se tam. Když otvíráte dveře, slyšíte hluk, jako kdyby si někdo najednou něco uvědomil, a když vstoupíte, u okna, cele zaujata protější stranou ulice, stojí Emily a na druhém konci místnosti stojí váš přítel John Edward, do hloubi duše uchvácený fotografiemi příbuzenstva cizích lidí.
„Á!“ řeknete a zůstanete stát hned za dveřmi. „Já nevěděl, že tu někdo je.“
„Ale! Vážně ne?“ praví chladně Emily, tónem, který vám dává najevo, že vám nevěří.
Chviličku přešlapujete a pak podotknete:
„Už je hodně tma. Proč si nerozsvítíte plyn?“
John Edward udělá „Á!“ a že prý si toho ani nevšiml; a Emily odvětí, že tatíček to námá rád, když se svítí už odpoledne.
Povíte jim jednu nebo dvě novinky a seznámíte je se svým názorem na irskou otázku; ale to je očividně nezajímá. Ke každému tématu poznamenají pouze „Ale!“, „Tak?“, „Vážně?“, „Hm“ a „Neříkejte!“ Po pětiminutové konverzaci v tomto stylu se nenápadně odsunete až ke dveřím a vyklouznete ze salónu a velice vás překvapí, že dveře se za vámi okamžitě zavírají a samy se zabouchnou, ačkoli jste se jich ani nedotkli.
0 půl hodiny později si usmyslíte, že si vykouříte jednu dýmku v zimní zahradě. Na té jediné židli, která tam je, sedí Emily; a John Edward, dá-li se věřit tomu, co hlásá jeho oblek, seděl zřejmě na podlaze. Ani jeden z nich nepromluví, ale oba vás obdaří pohledem, který říká vše, co se v civilizovaném lidském společenství říci smí; takže neprodleně vycouváte ven a zavřete za sebou dveře.
Teď už se bojíte strčit nos do kterékoli místnosti v domě, a tak se chvilku procházíte nahoru a dolů po schodišti a pak se jdete posadit do svého pokoje. Tam vás to však po jisté době přestane bavit a tak si nadadíte klobouk a vyjdete si do zahrady. Pustíte se po pěšině, a když jdete kolem altánku, nakouknete dovnitř - a ti dva mladí blázni jsou i tady a tulí se k sobě v jednom koutku; ovšemže vás zahlédnou a jsou zřejmě přesvědčeni, že je s nějakými ohavnými úmysly, které jsou vám podobné, schválně všude sledujete.
„Proč tu na tyhle věci nemají nějaký zvláštní pokoj, kde by ty lidičky internovali?“ bručíte; vřítíte se do haly, popadnete deštník a jdete pryč z domu.
Takhle nějak to muselo dopadat, když se ten poblázněný chlapec Jindřich VIII. dvořil své Aničce. Lidé z buckinghamského hrabství na ně každou chvíli neočekávaně narazili, jak náměsíčně bloumají kolem Windsoru a Wraysbury, a divili se „Á, to jste vy!“ a Jindřich se červenal a říkal „No,“ a že prý sem jde za jedním známým; a Anna říkala: „Jé, to jsem ráda, že vás vidím. Zrovinka jsem potkala pana Jindřicha VIII. tuhle v uličce, a on vám má stejnou cestu jako já.“
A ti lidé se vždycky vzdálili a nakonec si řekli: „Radši odtud zmizíme, dokud se tu bude takhle vrkat a cukrovat. Odjedeme do Kentu.“
1 odjeli do Kentu, a první, co v Kentu uviděli, jakmile tam dorazili, byli Jindřich s Annou, laškující poblíž zámku Hever.
„Ále, k čertu!“ řekli si lidé. „Pojďme odtud! Na tohle se nedá koukat. Pojeďme do St. Albans - to je takové pěkné, tiché místečko, St. Albans.“
A když se dostali do St. Albans, i tam byl ten otravný páreček a líbal se pod zdmi opatství. A tu se ti lidé dali k pirátům a nechali toho, až když bylo po té stavbě.
Ten kousek řeky od výběžku Výletníků až ke zdymadlu u Starého Windsoru je rozkošný. Po břehu běží stinná cesta, tu a tam obtečkovaná hezounkými malými chatami, až k „Ouseleyským zvonkůmů, což je malebná hospůdka - hospůdky na řece jsou většinou malebné - a navíc podnik, kde se můžete napít výtečného piva, jak říká Harris; a v
otázkách z tohoto oboru můžete na Harrisovo slovo dát. Starý Windsor je místo svým způsobem proslulé. Měl zde palác Edward Vyznavač a zde právní řád tehdejšího věku usvědčil slavného hraběte Godwina, že nese vinu na smrti králova bratra. Hrabě Godwin si ulomil kousek chleba a podržel jej v ruce.
„Jsem-li vinen,“ pravil hrabě, „ať se tímto chlebem udávím, až ho budu jíst.“
Pak si ten chléb vložil do úst a polkl ho a udávil se jím a umřel.
Za Starým Windsorem je Temže dost nezajímavá a v tu starou milou řeku se zase promění, až když se blííte k Boveney. George a já jsme koníčkovali podél Home Parku, který se rozprostírá na pravém břehu od mostu Albertova k mostu Victoriinu. A když jsme míjeli Datchet, George se mě ptal, jestli se pamatuju na náš první výlet nahoru po řece a na to, jak jsme v deset hodin večer přistali u Datchetu a toužili se vyspat.
Odpověděl jsem mu, že se na to pamatuju. To bude hezky dlouho trvat, než na to zapomenu.
Bylo to v sobotu před srpnovými dny bankovního volna. Padali jsme únavou a měli jsme hlad - vyjeli jsme si v téže trojici - a když jsme dorazili k Datchetu, vytahali jsme z lodi koš, dvě brašny, deky a pláště a talové věci a vydali jsme se hledat nocleh. Šli jsme koilem velice hezkého hotýlku s plaménkem a psím vínem kolem vchodu; ale neměli tam kozí list a já jsem si z nějakého neznámého důvodu umanul, že chci kozí list, a povídám:
„Ne, semhle nechoďme! Pojďme ještě o kousek dál a podívejme se, jestli tu není nějaký hotel obrostlý kozím listem.“
A tak jsme pochodovali dál, až jsme došli k jinému hotýlku. Ten byl taky moc hezký a byl obrostlý kozím listem, po celé jedné straně, ale Harrisovi se zase nezamlouval chlapík, co se opíral o hlavní vchod. Že prý ani trochu nedělá dojem člověka přívětivého a že prý má šeredné boty; tak jsme šli ještě dál. Ušli jsme notný kus cesty, ale žádné další hotely jsme už neviděli. A pak jsme potkali nějakého muže a prosili jsme ho, aby nám ukázal, kudy se dostaneme k různým zdejším hotelům.
„Ale vždyť jdete rovnou od nich,“ povídá ten muž. „To se musíte otočit a dát se zpátky, a tak přijdete k ,Jelenu‘.“
„Tam už jsme byli,“ my na to, „ale tam se nám to nelíbilo, nemají tam kozí list.“
„No potom je tu ještě Panský dvůr, hned naproti. Tam jste se už ptali?“
Harris mu řekl, že tam se nám jít nechtělo, že se nám nezamlouval chlapík, co tam stál ve dveřích - jemu, Harrisovi, že se nezamlouvala barva jeho vlasů a taky jeho boty, že se mu nazamlouvaly.
„Tak to tedy nevím, co budete dělat, namouduši,“ pravil náš informátor, „poněvadž tohle jsou jediné dva hostince v celé obci.“
„Jiný hostinec tu není?“ vykřikl Harris.
„Žádný,“ odvětil ten muž.
„Prokristapána, co si počneme?“ zahořekoval Harris.
A tu promluvil George. Pravil, že Harris a já si můžeme dát postavit speciální hotel jen pro sebe, je-li nám libo, a že si do něj můžeme dát stvořit i nějaké lidi, ale podkud jeho se týče, on že se vrátí k „Jelenu“.
Velicí duchové nikdy v ničem neuskuteční své ideály; a tak jsme si - Harris i já - povzdychli nad marností všech světských tužeb a vykročili jsme za Georgem.
Vnesli jsme svou bagáž do „Jelena“ a složili ji v hale.
Přišel hostinský a povídá: „Dobrý večer, pánové.“
„Dobrý večer,“ řekl George. Potřebovali bychom tři lůžka, prosím.“
„Velice lituji, pane,“ pravil hostinský, „ale to bohužel nepůjde.“
„No, nevadí,“ řekl George, „dvě nám taky postačí. Dva se prostě vyspíme v jedné posteli, no ne?“ dodal, obraceje se k Harrisovi a ke mně.
Harris povídá: „No jistě;“ myslel si, že v té jedné posteli se klidně vyspím já s Georgem.
„Velice lituji, pane,“ opakoval hostinský, „ale my nemáme v celém podniku ani jediné lůžko. Beztak už, abych pravdu řekl, dáváme do jednoho lůžka dva, ba dokonce i tři pány.“
To nás trochu vyvedlo z míry.
Ale Harris, starý zkušený cestovatel, se hbitě ujal situace a řekl s bodrým smíchem:
„No to se nedá nic dělat, musíme se spokojit s tím, co je. Ustelte nám prostě v kulečníkové síni.“
„Velice lituji, pane. Na biliáru už spí tři pánové a dva další spí v kavárničce. Dneska vás opravdu nemohu ubytovat.“
Posbírali jsme tedy své věci a šli do Panského dvora. Byl to moc hezký podniček. Já jsem prohlásil, že mně se v něm bude líbit mnohem víc než v tamtom; a Harris přitakal „Ano, ovšem,“ tady to prý bude výborné a na toho chlapa s těmi zrzavými vlasy se prostě nemusíme koukat; ostatně ten chudák za to ani nemůže, že má zrzavé vlasy. To bylo od Harrise moc hezké a rozumné.
V „Panském dvoře“ nás vůbec nepustili ke slovu. Hostinská nás hned na prahu přivítala sdělením, že jsme už čtrnáctá parta, kterou v poslední půldruhé hodině poslala pryč. Když jsme jí pokorně připomněli stáje, kulečníkovou síň a uhelný sklep, pohrdavě se rozesmála; všechny tyto útulky jsou už dávno zabrány.
A neví aspoň o nějakém stavení ve vesnici, kde by nám poskytli přístřeší na jednu noc?
Inu, jestli prý nejsme moc nároční - ona sama nám to, aby nebylo mýlky, nikterak nedoporučuje - tak by tu byla jedna malá krčma, asi tak tři čtvrtě kilometru po silnici k Etonu...
Na bližší údaje jsme už ani nečekali; chopili jsme se koše a brašen a plášťů a dek a balíků a uháněli jsme. Měli jsme spíš dojem, že jsme uběhli půldruhého kilometru, a ne tři čtvrtě kilometru, ale nakonec jsme dorazili k cíli a skoro bez dechu jsme vpadli do výčepu.
V té krčmě nás odbyli dost neurvale. Prostě se nám vysmáli. V celém stavení mají prý jenom tři postele a v těch už spí sedm pánů a dva manželské páry. Jakýsi dobrosrdečný lodník, který v tom šenku náhodou pobýval, si však myslel, že bychom to mohli zkusit u hokynáře vedle „Jelena“, a tak jsme šli zase zpátky.
Hokynář měl plně obsazeno. Ale jakási stará ženská, s kterou jsme se potkali u něho v krámě, nás laskavě vzala s sebou k jedné své přítelkyni, která bydlela skoro o půl kilometru dál a příležitostně pronajímala pánům pokoje.
Ta stará ženská chodila velice pomalu, a tak nám to k té její přítelkyni trvalo dvacet minut. A celou tu cestu, co jsme se s ní vlekli, nám zpestřovala líčením rozmanitých bolestí, co má v zádech.
Pokoje její přítelkyně už byly obsazeny. Dostali jsme tam však doporučení do čísla 27. V čísle 27 bylo plno, ale poslali nás do čísla 32, a v čísle 32 bylo taky plno.
Vrátili jsme se tedy na silnici a Harris seposadil na koš a prohlásil, že dál už nepůjde. Pravil, že tohle je zřejmě pokojné místečko a že na něm rád zemře. Žádal George a mne, abychom za něj políbili jeho matku a řekli všem jeho příbuzným, že jim odpustil a zemřel šťasten.
V tom okamžiku se objevil anděl přestrojený za chlapečka (nedovedu si představit, že by si anděl mohl vybrat přestrojení působivější), který držel v jedné ruce džbán piva a v druhé cosi na provázku, co pouštěl na každý plochý káme v cestě a pak vždycky zase vytahoval nahoru, čímž vyluzoval obzvláště odpudivé zvuky, působící trýznivě.
Ptali jsme se toho nebeského posla (že to nebeský posel je, to jsme za chvilku bezpečně zjistili), zda neví o nějakém osamělém domě s nečetnými a slabými obyvateli (stařičké dámy a ochrnutí pánové zvlášť vítáni), které by bylo lehce možno zastrašit, aby pro jednu noc přenechali svá lože třem zoufalcům; nebo, jestli nic takového neexistuje, zda by nám aspoň nemohl doporučit nějaký prázný prasečí chlívek nebo nějakou vápenickou pec mimo provoz nebo cokoli podobného. O žádném takovém útulku chlapeček nevěděl - aspoň ne o žádném, který by byl při ruce -, ale řekl, že jestli chceme jít s ním, tak jeho maminka že má jednu místnost volnou a mohla by nás tam na noc uložit.
Padli jsme mu v tom měsíčním světle kolem krku a blahořečili mu, což mohl být překrásný výjev, nebýt toho, že i chlapce to naše pohnutí dočista zmohlo, takže se pod jeho tíhou neudržel na nohou, padl na zem a nás všechny strhl na sebe. Harrise ta radost zdolala do té míry, že omdlel a musel popadnout chlapcův džbán s pivem a do polovičky ho vyprázdnit, aby zase nabyl vědomí, a pak se dal v běh a George a mne nechal nést všechna zavazadla.
Chlapec bydlel v domečku o čtyřech místnostech a jeho matka - předobrá duše! - nám dala k večeři opečenou slaninu a my jsme ji všecku - dvě a půl kila - spořádali a navrch ještě povidlový koláč a vypili jsme dva hrnce čaje a pak jsme šli spat.
V té místnosti byly dvě postele; jedna - takové to nízké lehátko na kolečkách - byla široká 75 cm, a na té jsem spal já s Georgem a udrželi jsme se na ní jenom tím, že jsme se prostěradlem přivázali těsně k sobě; ta druhá byla postýlka toho chlapečka, a tu měl sám pro sebe Harris a ránu mu z ní trčely ven obě bosé nohy, jichž jsme George a já, když jsme se myli, použili jako věšáků na ručníky.
Když jsme příště přijeli do Datchetu, už jsme tak neohrnovali nos při výběru hotelu.
Ale abychom se vrátili k našemu nynějšímu výletu: nestalo se nic vzrušujícího, a tak jsme vytrvale koníčkovali až kousek pod Opičí ostrov, kde jsme zarazili u břehu, abychom se naobědvali. Pustili jsme se do studeného hovězího a pak jsme zjistili, že jsme si zapomněli vzít s sebou hořčici. A snad nikdy v životě, ani předtím ani potom, jsem neměl tak strašnou chuť na hořčici jako tehdy. Obyčejně o hořčici nijak zvlášť nestojím a jenom velmi zřídka si ji vezmu, ale tehdy bych za ni byl dal všechno na světě. Nemám ponětí, co všechno vlastně na světě existuje, ale kdokoli, kdo by mi byl zrovna v tu chvíli přinesl lžíci hořčice, mohl to mít sakumpak. Takhle rozhazovačný jsem vždycky, když něco chci a nemohu to sehnat.
I Harris prohlásil, že za hořčici by dal všechno na světě. Takže objevit se tam tenkrát někdo s kelímkem hořčice, tak by byl udělal terno; mohl být do konce života pánem všeho živého i neživého na celém světě.
Ale kuš! Jak Harris tak já bychom se určitě byli koukali z té úmluvy vykroutit, až bychom tu hořčici byli dostali. Takovéhle marnotratné nabídky dělává člověk v okamžicích rozrušení, ale když pak o nich začne chladně přemýšlet, tak samozřejmě vidí, v jak absurdním nepoměru jsou k hodnotě toho požadovaného zboží. Na vlastní uši jsem slyšel, jak jeden člověk, který se ve Švýcarsku vydal do hor, prohlašoval, že byl dal všecko na světě za sklenici piva, a když pak vylezl k jedné boudě, kde pivo měli, ztropil děsivý kravál, protože na něm za jednu láhev chtěli pět franků. Křičel, že to je škandální zlodějina a že o tom napíše do Timesů.
Ale z toho, že jsme neměli hořčici, padla na celou loď ponurá nálada. To hovězí jsme snědli mlčky. Život nám připadal nicotný a ne zábavný. Vzpomínali jsme na blažené dny dětství a vzdychali jsme. Maloučko jsme však pookřáli nad jablkovým koláčem, a když George vylovil z koše plechovku s ananasovým kompotem a přikutálel ji do prsotředka lodi, měli jsme pocit, že život přece jen stojí za žití.
Máme ananas velice rádi, všichni tři. Dívali jsme se na obrázek na obalu plechovky a dělali jsme si chutě na tu šťávu. Jeden na druhého jsme se usmívali a Harris si přiychystal lžíci.
Potom jsme hledali nůž na otvírání konzerv. Vyházeli jsme všechno, co bylo v koši. Vyházeli jsme všechno z brašen. Nadzvedli jsme podlážky na dně lodi. Pak jsme všechno odnesli na břeh a důkladně protřepali. Ale otvírač konzerv nebyl k nalezení.
Harris se tedy pokusil otevřít tu plechovku kapesním nožíkem, zlomil si želízko a šeredně se řízl; načež to George zkoušel s nůžkami, ale ty mu vyletěly z ruky a málem mu vypíchly jedno oko. zatímco si ti dva ovazovali rány, já se snažil udělat do té věci díru bodcem na bidle s hákem, ten bodec se však po plechu smekl a bidlo mnou
mrsklo do půlmetrového bahna mezi lodí a břehem a plechovka se bez nejmenší úhony odkutálela o kus dál a rozbila šálek na čaj.
To jsme se už všichni rozzuřili. Zanesli jsme tu plechovku na břeh, Harris vlezl do jednoho pole a sebral tam veliký ostrý kámen, já se vrátil na loď a vyndal stěžeň a pak George držel tu plechovku, Harris nasadil na její víko ostrý hrot toho svého kamene a já jsem uchopil stěžeň, zvedl ho vysoko do vzduchu, sebral všechnu svou sílu a bacil.
Toho dne zachránil Georgeovi život jeho slamák. Má ho dodnes schovaný (totiž to, co z něho zbylo) a za dlouhých zimních večerů, když si mládenci zapálí dýmky a vyprávějí prášilovské historky o nebezpečenstvích, která přestáli, George přinese ten slamák a nechá ho kolovat a opět a nanovo je přetřásána ta vzrušující příhoda, pokaždé s novým přeháněním.
Harris z toho vyvázl jen s povrchním zraněním.
Poté jsem si tu plechovku vzal na starost já sám a bušil jsem do ní stěžněm tak dlouho, až jsem byl skoro úplně bez sebe a píchalo mě u srdce, načež se jí ujal Harris.
Stloukli jsme ji na placato; stloukli jsme ji do kostky; ztřískali jsme ji do všech tvarů známých v geometrii - ale díru jsme do ní udělat nedokázali.
Pak se do ní dal George a zmlátil ji do podoby tak nevídané, tak strašidelné, tak nepozemsky divé a ohyzdné, že se vyděsil a odhodil stěžeň. Pak jsme se všichni tři kolem ní sesedli do trávy a zahleděli jsme se na ni.
Přes její vršek se táhla dlouhá rýha, s kterou vypadala, jako kdyby se na nás posměšně šklebila, což nás do té míry rozběsnilo, že se Harris na ni vrhl, drapl ji a fláknul s ní daleko do prostředka řeky, a když se potopila, zasypali jsme ji nadávkami a pak jsme se vřítili do lodi a vesloval jsme pryč z toho místa a nedopřáli jsme si oddychu, dokud jsme nedorazili do Maidenheadu.
Maidenhead je příliš velká snobárna, aby to tam bylo příjemné. Stal se dostaveníčkem módních říčních elegánků a jejich vyparáděných společnic. Je to město okázalých hotelů, k jejichž stálé klientele patří hlavně mladí frajírci a slečny od baletu. A je to čarodějnická kuchyně, z které vyplouvají ti říční démoni - parníčky. Každý vévoda, o němž píše London Journal, vlastní „domeček“ v Maindenheadu; a vždycky tam večeří hrdinka třísvazkové limonády, když si vyrazí na freje s manželem jiné ženy.
Maidenheadem jsme tedy projeli rychle a teprve pak jsme zvolnili tempo a v tom velkolepém kousku řeky mezi zdymadly boulterským a cookhamským jsme si dali na čas. Clivedenské lesy pořád ještě neodložily něžný jarní šat a tyčily se nad vodou v jediné dlouhé harmonii prolínajících se odstínů rusalčích zelení. Svým nedotčeným půvabem je tohle snad nejlíbezněší úsek z celé řeky a my jsme se svou lodičkou vyplouvali z hlubin jeho poklidu pomalu a s otálením.
Pod Cookhamem jsme zaveslovali do vedlejšího řečiště a udělali jsme si čaj; a když jsme pak projeli zdymadlem, byl už večer. Zvedl se dost silný vítr - nám příznivý, kupodivu; neboť na řece vane vítr zpravidla přímo proti vám, ať jedete kterýmkoli směrem. Vane proti vám ráno, když jste vyrazili na celodenní výlet, takže musíte veliký kus cesty uveslovat, ale těšíte se, jak pohodlně se vám pojede zpátky s plachtou. Po svačině se však vítr otočí a vy jste celou cestu domů zase nuceni těžce veslovat proti němu.
Když si zapomenete vzít s sebou plachtu, pak je ovšem vítr jak při cestě tam, tak při cestě zpátky pro vás příznivý. No bodejť! Vždyť život je tvrdá zkouška, a jako jiskry létají vždycky nahoru, tak je člověk zrozen k samým trampotám.
Ale toho večera došlo zřejmě k nějakému omylu a vítr nám místo do tváří foukal do zad. Byli jsme o tom pěkně zticha, abychom to nezakřikli, honem, než se na to přijde, jsme vytáhli plachtu a pak jsme s v pozicích myslitelů rozložili po lodi a plachta se vzdula a vypjala a ševelila o stěžeň a loď letěla.
Já jsem kormidloval.
Neznám vzrušivější zážitek než plachtění. Tím se člověk nejvíc, jak to zatím jde, přiblížil k létání - když ovšem nemluvíme o létání ve snu. Máte dojem, že vás perutě hučícího větru unášejí neznámo kam. Už nejste ty pomalé, nemotorné, nedomrlé uplácaniny z hlíny, klikatě se plazící po zemi; najednou je z vás část Přírody. Vaše srdce bije na jejím srdci. Příroda si vás k tomu svému srdci zvedá těmi nádhernými pažemi, jimiž vás objala. Váš duch a její duch jedno jsou. Vaše údy se zbavily tíže! Zpívá vám hudba sfér. Země vám připadá daleká a maličká, a z oblak, jež zčistajasna máte tak těsně nad hlavou, jsou vaši bratři a vy na ně dosáhnete.
Měli jsme řeku sami pro sebe, jenom v dálce před sebou jsme viděli rybářskou pramičku, zakotvenou v proudu, a na ní tři rybáře. Klouzali jsme po hladině a letěli mezi zalesněnými břehy a nikdo z nás nemluvil.
Já pořád kormidloval.
Když jsme se k těm třem rybářům víc přiblížili, viděli jsme, že to jsou muži věkovití a velebného vzezření. Seděli v té pramici na třech stoličkách a upřeně pozorovali své vlasce. A rudý západ slunce vrhal mystické světlo na vodu, barvil naohnivo k nebi strmící lesy a dělal zlatou svatozář z nakupených mraků. Byla to chvíle hlubokého okouzlení, exatických nadějí a tužeb. Naše malá plachta se odrážela od purpurové oblohy, kolem nás se snášelo šero a halilo svět do duhových stínů; a zezadu se k nám plížila noc.
Připadali jsme si jako rytíři z nějaké dávné legendy, plavící se přes tajuplné jezero do neznámého hájemství soumraku a k veliké říši zapadajícího slunce.
Ale nevjeli jsme do hájemství soumraku; vjeli jsme plnou rychlostí do té pramice, co na ní chytali ryby ti tři starci. V první chvíli jsme vůbec nevěděli, co se to stalo, protože nám výhled zakrývala plachta, ale z povahy řečí, jež se rozlehly večerním vzduchem, jsme vytušili, že jsme se dostali do samé blízkosti lidských bytostí a že ty bytosti něco rozmrzelo a pozlobilo.
Harris stáhl plachtu a teprve pak jsme uzřeli, co se přihodilo. Srazili jsme ty tři staré pány z jejich stoliček na valnou hromadu na dně pramice, kde se teď pracně roztřiďovali na jednotlivce a sbírali se sebe ryby; a při té práci nás proklínali; ale kletby, jimiž nás častovali, nebylyl nicméně kletby časté, nýbrž kletby vzácné, bohatě rozvité, pečlivě promyšlené a obsažné, kletby, které pamatovaly na celou naši životní dráhu, zasahovaly až do daleké budoucnosti, týkaly se veškerého našeho příbuzenstva a vztahovaly se vůbec na všechno, co s námi jakkoli souvisí - no prostě kletby spolehlivé, důkladné.
Harris jim na to řekl, že když tam tak celý den jenom sedí a chytají ryby, tak by nám měli být vděčni, že jsme jim vnesli do života karpet vzruchu, a ještě dodal, že u lidí jejich věku ho takový nedostatek sebeovládání pohoršuje a zarmucuje.
Ale nebylo to k ničemu.
George potom prohlásil, že kormidlovat bude on. Že by se duch, jako je můj, dokázal soustředit na řízení lodi, to prý nelze očekávat, tak aspoň téhle lodi ať prý se raději ujme prostý, obyčejný smrtelník, než se všichni, jak tu jsme, utopíme. A zasedl ke kormidlu a dovezl nás do Marlow.
V Marlow j sme loď nechali pod mostem a na noc j sme se ubytovali „U koruny“
Marlow - Bishamské opatství - Medmenhanští mniši - Montmorency se rozmýšlí, má-li zavraždit starého kocoura - Leč posléze se rozhodne, že ho nechá naživu - Hanebné chování jednoho foxteriéra a obchodním domě - Odjíždíme z Marlow - Impozantní průvod - Parník; užitečný návod, jak ho zlobit a jak mu překážet - Odmítáme pít řeku - Pokojný pes - Jak Harris nepochopitelně zmizel i s hovězím.
";
            }
            return "Zde nic není";
        }
    }
}
