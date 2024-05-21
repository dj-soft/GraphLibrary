// Supervisor: David Janáček, od 01.01.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.
using DevExpress.XtraRichEdit.Import.Doc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TestDevExpress.AsolDX.News
{
    public class FontSizes
    {
        public static void CreateTable()
        {
            //   Jak určit velikost textu v situaci, kdy nemám Drawing ani Graphics, a chci to vcelku rychle:
            // - Použiju zdejší tabulku (viz dále)
            // - Vstupní text rozčlením na první písmeno, poté na dvojice: první+druhé, druhé+třetí, třetí+...+poslední, a pak sólo poslední písmeno
            // - Pro každý text najdu jeho velikost v tabulce, velikost reprezentuje vzdálenost mezi středy prvního a druhého písmena ve dvojici, anebo půlvelikost jednoho znaku
            // - Sečtu je.
            //   Příklad pro text: "ATdb\"
            // "A"  .. najdu velikost půlky textu = reprezentuje levou polovinu znaku
            // "AT" .. najdu vzdálenost středu = od poloviny prvního do poloviny druhého
            // "Td" ..  dtto
            // "db" ..  dtto
            // "b\" ..  dtto
            // "\"  .. velikost druhé půlky znaku
            //   Sečtu hodnoty a je hotovo.
            //   Když nenajdu kombinaci v tabulce, tak vyhledám jednotlivé písmeno první a druhé, a sečtu jejich půlvelikosti.

            //   Jak vypočítám tabulku:
            //  1. Vezmu jednotlivé znaky char(10) až char(266) a změřím šířku, uložím polovinu
            //  2. Sestavím kombinace dvojznaků, změřím velikost, odečtu půlvelikost jednotlivého znaku vlevo a vpravo, zbytek je velikost uprostřed, uložím
            //  Vznikne tedy tabulka: 0:znak pro jednotlivé, a znakA:znakB pro dvojznaky


            StringBuilder sb = new StringBuilder();

            for (int c = 32; c < 720; c++)
            {
                string s = ((char)c).ToString();
                if (s == "\"") s = "\\\"";
                else if (s == "\\") s = "\\\\";
                sb.Append(s);
            }
            string csstr = sb.ToString();
            string cscod = "   string ci = @\"" + csstr + "\";";

            string ci = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~¡¢£¤¥¦§¨©ª«¬®¯°±²³´µ¶·¸¹º»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿĀāĂăĄąĆćĈĉĊċČčĎďĐđĒēĔĕĖėĘęĚěĜĝĞğĠġĢģĤĥĦħĨĩĪīĬĭĮįİıĲĳĴĵĶķĸĹĺĻļĽľĿŀŁłŃńŅņŇňŉŊŋŌōŎŏŐőŒœŔŕŖŗŘřŚśŜŝŞşŠšŢţŤťŦŧŨũŪūŬŭŮůŰűŲųŴŵŶŷŸŹźŻżŽžſƀƁƂƃƄƅƆƇƈƉƊƋƌƍƎƏƐƑƒƓƔƕƖƗƘƙƚƛƜƝƞƟƠơƢƣƤƥƦƧƨƩƪƫƬƭƮƯưƱƲƳƴƵƶƷƸƹƺƻƼƽƾƿǀǁǂǃǄǅǆǇǈǉǊǋǌǍǎǏǐǑǒǓǔǕǖǗǘǙǚǛǜǝǞǟǠǡǢǣǤǥǦǧǨǩǪǫǬǭǮǯǰǱǲǳǴǵǶǷǸǹǺǻǼǽǾǿȀȁȂȃȄȅȆȇȈȉȊȋȌȍȎȏȐȑȒȓȔȕȖȗȘșȚțȜȝȞȟȠȡȢȣȤȥȦȧȨȩȪȫȬȭȮȯȰȱȲȳ";
            string co = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~¡¢£¤¥¦§¨©a«-aaaaaaaaaaaaaaXXXcAAAAAAACEEEEIIIIDNOOOOOxOUUUUYabaaaaaaeceeeeiiiionooooo-ouuuuubyAaAaAaCcCcCcCcDdDdEeEeEeEeEeGgGgGgGgHhhhIiIiIiIiIiujJjKkkLlLlLlLlLlNnNnNnnNnOoOoOoEeRrRrRrSsSsSsSsTtTtTtUuUuUuUuUuUuWwYyYZzZzZzFbbBbBbcCcDdAaOEeEFfGYMlIKklAWNnOOoMmPpRSsEltTtTUuUuYyZz33ee255spIIIIxxxxxxxxxAaIiOoUuUuUuUuUuaAaAaEeGgGgKkOoOo33jwwwGghDNnAaAaOoAaAaEeEeIiIiOoOoRrRrUuUuSsTtSsHhNdooZzAaEeOoOoOoOoYy";
            
            var values = new Dictionary<int, Info>();
            using (Bitmap bmp = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                bmp.SetResolution(144f, 144f);
                var family = SystemFonts.DefaultFont.FontFamily;
                float emSize = 20f;
                using (Font fontR = new Font(family, emSize, FontStyle.Regular))
                using (Font fontB = new Font(family, emSize, FontStyle.Bold))
                using (Graphics graphics = Graphics.FromImage(bmp))
                {
                    for (int c = 32; c < 512; c++)
                    {
                        int key = getKey1(c);
                        var text = getText1(c);
                        var sizeR = measureSize(text, graphics, fontR);
                        var sizeB = measureSize(text, graphics, fontB);

                        values.Add(key, new Info() { Code = key, Char1 = c, Char2 = 0, Text = text, SizeR = sizeR, SizeB = sizeB });
                    }
                }
            }

            int getKey1(int code)
            {
                return code - 32;
            }
            string getText1(int code)
            {
                return ((char)(code)).ToString();
            }
            SizeF measureSize(string text, Graphics graphics, Font font)
            {
                SizeF size = graphics.MeasureString(text, font);
                return size;
            }
        }
        private class Info
        {
            public override string ToString()
            {
                return $"'{Text}': R='{SizeR}';  B='{SizeB}'";
            }
            public int Code;
            public int Char1;
            public int Char2;
            public string Text;
            public SizeF SizeR;
            public SizeF SizeB;
        }
    }
}