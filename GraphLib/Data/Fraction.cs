using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Djs.Common.Data
{
    /// <summary>
    /// Fraction = zlomek
    /// </summary>
    public struct Fraction
    {
        #region Konstrukce, public property, private proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="integral">Celočíselný díl (před zlomkem hodnota 5 ve výrazu : 5 a 1/3)</param>
        public Fraction(int integral)
        {
            this._Integral = integral;
            this._Numerator = 0;
            this._Denominator = 1;
            this._CheckFraction();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="numerator">Čitatel (ve zlomku nahoře)</param>
        /// <param name="denominator">Jmenovatel (ve zlomku dole)</param>
        public Fraction(int numerator, int denominator)
        {
            this._Integral = 0;
            this._Numerator = numerator;
            this._Denominator = denominator;
            this._CheckFraction();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="integral">Celočíselný díl (před zlomkem hodnota 5 ve výrazu : 5 a 1/3)</param>
        /// <param name="numerator">Čitatel (ve zlomku nahoře)</param>
        /// <param name="denominator">Jmenovatel (ve zlomku dole)</param>
        public Fraction(int integral, int numerator, int denominator)
        {
            this._Integral = integral;
            this._Numerator = numerator;
            this._Denominator = denominator;
            this._CheckFraction();
        }
        public override string ToString()
        {
            return (this._Integral != 0 ? this._Integral.ToString() + " " : "") + (this._Numerator == 0 ? "" : (this._Numerator.ToString() + "/" + this._Denominator.ToString()).Trim());
        }
        /// <summary>
        /// Celočíselný díl (před zlomkem hodnota 5 ve výrazu : 5 a 1/3)
        /// </summary>
        public int Integral { get { return this._Integral; } }
        private int _Integral;
        /// <summary>
        /// Čitatel (ve zlomku nahoře)
        /// </summary>
        public int Numerator { get { return this._Numerator; } }
        private int _Numerator;
        /// <summary>
        /// Jmenovatel (ve zlomku dole)
        /// </summary>
        public int Denominator { get { return this._Denominator; } }
        private int _Denominator;
        /// <summary>
        /// Obsahuje plného čitatele (=Integral * Denominator + Numerator)
        /// </summary>
        private int WholeNumerator { get { return (this._Integral * this._Denominator) + this._Numerator; } }
        /// <summary>
        /// true pokud hodnota == 0 (0 + 0/x)
        /// </summary>
        public bool IsZero { get { return this._Integral == 0 && this._Numerator == 0; } }
        /// <summary>
        /// Obsahuje hodnotu Zero (0 + 0/1)
        /// </summary>
        public static Fraction Zero { get { return new Fraction(0, 0, 1); } }
        /// <summary>
        /// Obsahuje kopii this hodnoty
        /// </summary>
        public Fraction Clone { get { return new Fraction(this._Integral, this._Numerator, this._Denominator); } }
        /// <summary>
        /// Obsahuje zápornou kopii this hodnoty
        /// </summary>
        public Fraction Negative { get { return new Fraction(-this._Integral, -this._Numerator, this._Denominator); } }
        /// <summary>
        /// Odchytí problém, kdy jmenovatel je roven 0 (dojde k chybě).
        /// Pokud jmenovatel je záporný, obrátí znaménka čitatele i jmenovatele.
        /// Pokud čitatel je větší než jmenovatel, převede celočíselnou část do Integral.
        /// Pokud čitatel je menší než -jmenovatel, převede celočíselnou část do Integral.
        /// </summary>
        private void _CheckFraction()
        {
            int numerator = this._Numerator;
            int denominator = this._Denominator;
            if (numerator != 0 && denominator == 0)
                throw new OverflowException("A Fraction can not have a zero denominator, when has non-zero numerator.");
        }
        /// <summary>
        /// GetHashCode()
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (_Integral << 24) | (_Numerator << 12) | (_Denominator);
        }
        /// <summary>
        /// Equals() - pro použití GID v Hashtabulkách
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Fraction)) return false;
            return (Fraction._IsEqual(this, (Fraction)obj));
        }
        /// <summary>
        /// Porovnání dvou instancí této struktury, zda obsahují shodná data
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool _IsEqual(Fraction a, Fraction b)
        {
            return (a._Integral == b._Integral && a._Numerator == b._Numerator && a._Denominator == b._Denominator);
        }
        #endregion
        #region Zjednodušení zlomku
        /// <summary>
        /// Vrátí daný zlomek, zjednodušený do normalizovaného tvaru (má část Integral podle potřeby, nemá záporný jmenovatel, zlomková část je zjednodušená na společného dělitele)
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Fraction Simplify(Fraction f)
        {
            return _CreateSimplify(f, true, SimplifyIntegralMode.ToIntegral, true);
        }
        /// <summary>
        /// Vrátí daný zlomek, zjednodušený do normalizovaného tvaru (má část Integral podle potřeby, nemá záporný jmenovatel, zlomková část je zjednodušená na společného dělitele)
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Fraction Simplify(int integral, int numerator, int denominator)
        {
            return _CreateSimplify(integral, numerator, denominator, true, SimplifyIntegralMode.ToIntegral, true);
        }
        /// <summary>
        /// Zjednodušit zlomek
        /// </summary>
        private static Fraction _CreateSimplify(Fraction source, bool negativeDenominatorToPositive, SimplifyIntegralMode integralMode, bool simplifyFraction)
        {
            return _CreateSimplify(source.Integral, source.Numerator, source.Denominator, negativeDenominatorToPositive, integralMode, simplifyFraction);
        }
        /// <summary>
        /// Zjednodušit zlomek
        /// </summary>
        private static Fraction _CreateSimplify(int integral, int numerator, int denominator, bool negativeDenominatorToPositive, SimplifyIntegralMode integralMode, bool simplifyFraction)
        {
            // Negative denominator: prepare
            bool hasNegativeDenominator = (denominator < 0);
            if (hasNegativeDenominator)
            {   // Uvnitř metody vždy pracuji s kladným jmenovatelem:
                numerator = -numerator;
                denominator = -denominator;
            }

            // Integral part
            if (denominator != 0)
            {
                switch (integralMode)
                {
                    case SimplifyIntegralMode.ToIntegral:
                        if (numerator >= denominator)
                        {
                            int addIntegral = (numerator / denominator);
                            integral += addIntegral;
                            numerator -= (addIntegral * denominator);
                        }
                        if (numerator <= -denominator)
                        {
                            int subIntegral = (-numerator / denominator);
                            integral -= subIntegral;
                            numerator += (subIntegral * denominator);
                        }
                        break;

                    case SimplifyIntegralMode.ToNumerator:
                        if (integral != 0)
                        {
                            int addNominator = integral * denominator;
                            integral = 0;
                            numerator += addNominator;
                        }
                        break;
                }
            }

            // Simplify Fraction part (Nominator and Denominator):
            if (simplifyFraction)
            {
                _SimplifyFraction(ref numerator, ref denominator);
            }

            // Negative denominator: undone
            if (hasNegativeDenominator && !negativeDenominatorToPositive)
            {   // Pokud jsme měli negativní jmenovatel, ale není požadavek na jeho otočení na pozitivní => vrátím (dosud) pozitivní jmenovatel na záporný, a otočím i čitatel:
                numerator = -numerator;
                denominator = -denominator;
            }

            return new Fraction(integral, numerator, denominator);
        }
        /// <summary>
        /// Režim zjednodušení celočíselné části zlomku
        /// </summary>
        private enum SimplifyIntegralMode { None, ToIntegral, ToNumerator }
        #endregion
        #region Společný jmenovatel, zjednodušení zlomku
        /// <summary>
        /// Zjednoduší zlomek. Například pro zadání 10/20 je výsledek 1/2. Nebo pro 3/21 = 1/7. 
        /// Pro 0/16 = 0/1.
        /// Pro -25/60 = -5/12.
        /// </summary>
        /// <param name="numerator"></param>
        /// <param name="denominator"></param>
        private static void _SimplifyFraction(ref int numerator, ref int denominator)
        {
            if (denominator == 0) return;                  // Jmenovatel == 0 => nezjedodušuji      (12/0 = 12/0)
            if (numerator == 0)                            // Čitatel == 0 => jmenovatel = 1 a hotovo
            {   //  Například  0/16 = 0/1:
                denominator = 1;
                return;
            }
            // Převést záporné hodnoty na kladné:
            bool negativeDenominator = (denominator < 0);
            if (negativeDenominator)
            {
                numerator = -numerator;
                denominator = -denominator;
            }
            bool negativeNumerator = (numerator < 0);
            if (negativeNumerator)
            {
                numerator = -numerator;
            }

            // Společný dělitel, pokrácení zlomku tímto dělitelem:
            int divider = _GetCommonDivisor(numerator, denominator);
            if (divider > 0)
            {
                numerator /= divider;
                denominator /= divider;
            }

            // Vrátit záporné hodnoty tam, kam patří:
            if (negativeNumerator)
            {
                numerator = -numerator;
            }
            if (negativeDenominator)
            {
                denominator = -denominator;
                numerator = -numerator;
            }
        }
        /// <summary>
        /// Vrátí nejmenší společný násobek dvou čísel.
        /// Pro čísla do velikosti (_CacheSize) využívá cache. Pro čísla vyšší než hodnota (_CacheSize) se výsledek vždy vypočítá.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static int _GetCommonMultiple(int a, int b)
        {
            int divisor = _GetCommonDivisor(a, b);
            return a * b / divisor;
        }
        /// <summary>
        /// Vrátí nejvyšší společný dělitel dvou čísel.
        /// Pro čísla do velikosti (_CacheSize) využívá cache. Pro čísla vyšší než hodnota (_CacheSize) se výsledek vždy vypočítá.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static int _GetCommonDivisor(int a, int b)
        {
            bool toCache = ((a >= 0 && a < _CacheSize) && (b >= 0 && b < _CacheSize));
            int? divisor = (toCache ? _CommonDivisors[a, b] : null);
            bool inCache = (divisor.HasValue);
            if (!inCache) divisor = _CalcCommonDivisor(a, b);
            if (toCache && !inCache)
                _CommonDivisors[a, b] = divisor;
            return divisor.Value;
        }
        /// <summary>
        /// Vypočítá nejvyšší společný dělitel dvou čísel.
        /// </summary>
        /// <param name="numerator"></param>
        /// <param name="denominator"></param>
        /// <returns></returns>
        private static int _CalcCommonDivisor(int numerator, int denominator)
        {
            // Automatické výsledky:
            if (numerator == denominator) return numerator;                    //   47 / 47 : společný dělitel == 47
            if (numerator <= 1 || denominator <= 1) return 1;                  //    1 / 27 : společný dělitel == 1   (v podstatě není)

            // Triviální výsledky:
            if ((numerator % denominator) == 0) return denominator;            //   48 /  6 : společný dělitel == 6   (48 / 6 = 8 beze zbytku)
            if ((denominator % numerator) == 0) return numerator;              //    6 / 48 : společný dělitel == 6   (dtto v opačném pořadí)

            // Postupné hledání nejvyššího společného dělitele:
            #region Komentář s příkladem
            /* 
            pro příklad dám čísla numerator = 48 a denominator = 60
            Proměnná "divisor" představuje kumulativní nejvyšší společný dělitel počínaje 1, a zvyšuje se násobením;
            Proměnná "d" představuje testovacího dělitele, počínaje 1, a zvyšuje se inkrementací;

            V našem příkladě půjdou hodnoty takto:
            -----------------------------------------------------------------------------
            V 1. cyklu bude: d = 1; d++; d = 2;
            Test A = OK (48 % 2 == 0 and 60 % 2 == 0)
             divisor = 1 * 2 = 2;
             numerator = 48 / 2 = 24
             denominator = 60 / 2 = 30;
             d = 1;
            
            Test B: 24 / 1 = 24, 24 není >= 1; 30 / 1 = 30; 30 není >= 1; pokračujeme dál

            -----------------------------------------------------------------------------
            V 2. cyklu bude: d = 1; d++; d = 2;
            Test A = OK (24 % 2 == 0 and 30 % 2 == 0)
             divisor = 2 * 2 = 4;
             numerator = 24 / 2 = 12
             denominator = 30 / 2 = 15;
             d = 1;
            
            Test B: 12 / 1 = 12, 12 není >= 1; 15 / 1 = 15; 15 není >= 1; pokračujeme dál

            -----------------------------------------------------------------------------
            V 3. cyklu bude: d = 1; d++; d = 2;
            Test A = NE (12 % 2 == 0 and 15 % 2 != 0)
            
            Test B: 12 / 2 = 6, 6 není >= 2; 15 / 2 = 7; 7 není >= 2; pokračujeme dál

            -----------------------------------------------------------------------------
            V 4. cyklu bude: d = 2; d++; d = 3;
            Test A = OK (12 % 3 == 0 and 15 % 3 == 0)
             divisor = 4 * 3 = 12;
             numerator = 12 / 3 = 4
             denominator = 15 / 3 = 5;
             d = 1;
            
            Test B: 4 / 1 = 4, 4 není >= 1; 5 / 1 = 5; 5 není >= 1; pokračujeme dál

            -----------------------------------------------------------------------------
            V 5. cyklu bude: d = 1; d++; d = 2;
            Test A = NE (4 % 2 == 0 and 5 % 2 != 0)
            
            Test B: 4 / 2 = 2, 2 je >= 2; 5 / 2 = 2; 2 je >= 2; skončíme

            -----------------------------------------------------------------------------
            Výsledek: divisor = 12; což je správně pro hodnoty 60 a 48.
            
            Spotřebovali jsme 5 cyklů.

            */
            #endregion
            int divisor = 1;
            int d = 1;
            while (true)
            {
                d++;
                if (((numerator % d) == 0) && ((denominator % d) == 0))
                {   // Test A: obě hodnoty jsou dělitelné:
                    divisor *= d;
                    numerator /= d;
                    denominator /= d;
                    d = 1;
                }
                if (((numerator / d) <= d) && ((denominator / d) <= d))
                    // test B: pokud podíl (numerator nebo denominator) / d přesáhne d, pak jsem překročil hodnotu odmocniny a další hledání nemá smysl, skončím:
                    break;
            }
            return divisor;
        }
        private const int _CacheSize = 200;
        /// <summary>
        /// Autoinicializační property pro přístup k cache pro společné dělitele
        /// </summary>
        private static int?[,] _CommonDivisors
        {
            get
            {
                if (__CommonDivisors == null)
                    __CommonDivisors = new int?[_CacheSize, _CacheSize];
                return __CommonDivisors;
            }
        }
        /// <summary>
        /// Úložiště cache pro společné dělitele
        /// </summary>
        private static int?[,] __CommonDivisors = null;
        #endregion
        #region Implicitní konvertory typů
        /// <summary>
        /// Konverze na Single
        /// </summary>
        /// <param name="f"></param>
        public static implicit operator Single (Fraction f) { return (Single)f.Value; }
        /// <summary>
        /// Konverze na Double
        /// </summary>
        /// <param name="f"></param>
        public static implicit operator Double (Fraction f) { return (Double)f.Value; }
        /// <summary>
        /// Konverze na Decimal 
        /// </summary>
        /// <param name="f"></param>
        public static implicit operator Decimal (Fraction f) { return f.Value; }
        /// <summary>
        /// Decimal hodnota tohoto zlomku
        /// </summary>
        public Decimal Value { get { return (decimal)this._Integral + ((decimal)this.Numerator / (decimal)this._Denominator); } }
        #endregion
        #region Operátory ==, !=, +, *
        /// <summary>
        /// Operátor "je rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Fraction a, Fraction b)
        {
            return Fraction._IsEqual(a, b);
        }
        /// <summary>
        /// Operátor "není rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(Fraction a, Fraction b)
        {
            return (!Fraction._IsEqual(a, b));
        }

        /// <summary>
        /// Operátor "plus" = sčítání dvou zlomků (4/5 + 3/20 = 16/20 + 3/20 = 19/20)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Fraction operator +(Fraction a, Fraction b)
        {
            if (a.IsZero && b.IsZero) return Zero;
            if (a.IsZero) return b.Clone;
            if (b.IsZero) return a.Clone;

            int d = _GetCommonMultiple(a.Denominator, b.Denominator);

            int i = a.Integral + b.Integral;
            int an = (a.Denominator == d ? a.Numerator : a.Numerator * d / a.Denominator);
            int bn = (b.Denominator == d ? b.Numerator : b.Numerator * d / b.Denominator);
            int n = an + bn;

            return Simplify(i, n, d);
        }
        /// <summary>
        /// Operátor "mínus" = odečítání dvou zlomků (3/5 - 3/20 = 12/20 - 3/20 = 9/20)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Fraction operator -(Fraction a, Fraction b)
        {
            if (a.IsZero && b.IsZero) return Zero;
            if (a.IsZero) return b.Negative;
            if (b.IsZero) return a.Clone;

            int d = _GetCommonMultiple(a.Denominator, b.Denominator);

            int i = a.Integral - b.Integral;
            int an = (a.Denominator == d ? a.Numerator : a.Numerator * d / a.Denominator);
            int bn = (b.Denominator == d ? b.Numerator : b.Numerator * d / b.Denominator);
            int n = an - bn;

            return Simplify(i, n, d);
        }
        /// <summary>
        /// Operátor "násobení" = násobení dvou zlomků  (4/5 * 3/20 = 12/60 = 1/5)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Fraction operator *(Fraction a, Fraction b)
        {
            if (a.IsZero || b.IsZero) return Zero;

            int i = 0;
            int n = a.WholeNumerator * b.WholeNumerator;             // (5 + 1/2) * (2 + 1/3) = 11/2 * 7/3 = 77/6
            int d = a.Denominator * b.Denominator;

            return Simplify(i, n, d);
        }
        /// <summary>
        /// Operátor "dělení" = dělení dvou zlomků  (4/5  /  8/9  =  4/5  *  9/8  =  45/40 = 9/8)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Fraction operator /(Fraction a, Fraction b)
        {
            if (a.IsZero) return Zero;
            if (b.IsZero)
                throw new OverflowException("Divide with Fraction == Zero is impossible.");

            int i = 0;
            int n = a.WholeNumerator * b.Denominator;
            int d = a.Denominator * a.WholeNumerator;

            return Simplify(i, n, d);
        }

        /// <summary>
        /// Operátor "plus" = sčítání zlomku + čísla  (2 a 4/7 + 12 = 14 a 4/7)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Fraction operator +(Fraction a, int b)
        {
            if (a.IsZero && b == 0) return Zero;
            if (a.IsZero) return new Fraction(b, 0, 1);
            if (b == 0) return a.Clone;

            return Simplify(a.Integral + b, a.Numerator, a.Denominator);
        }
        /// <summary>
        /// Operátor "mínus" = odečítání zlomku + čísla  (12 a 4/7 - 1 = 11 a 4/7)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Fraction operator -(Fraction a, int b)
        {
            if (a.IsZero && b == 0) return Zero;
            if (a.IsZero) return new Fraction(-b, 0, 1);
            if (b == 0) return a.Clone;

            return Simplify(a.Integral - b, a.Numerator, a.Denominator);
        }
        /// <summary>
        /// Operátor "mínus" = odečítání zlomku + čísla  (6 - (1 a 1/3) = 4 a 2/3)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Fraction operator -(int a, Fraction b)
        {
            if (a == 0 && b.IsZero) return Zero;
            if (a == 0) return b.Negative;
            if (b.IsZero) return new Fraction(a, 0, 1);

            return Simplify(a - b.Integral, -b.Numerator, b.Denominator);
        }
        /// <summary>
        /// Operátor "násobení" = násobení zlomku a čísla (4/5 * 2 = 8/5 = 1 a 3/5)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Fraction operator *(Fraction a, int b)
        {
            if (a.IsZero || b == 0) return Zero;

            int i = 0;
            int n = a.WholeNumerator * b;             // (5 + 1/2) * (2 + 1/3) = 11/2 * 7/3 = 77/6
            int d = a.Denominator;

            return Simplify(i, n, d);
        }
        /// <summary>
        /// Operátor "dělení" = dělení zlomku číslem  (4/5  /  2  =  2/5)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Fraction operator /(Fraction a, int b)
        {
            if (a.IsZero) return Zero;
            if (b == 0)
                throw new OverflowException("Divide with Integer == Zero is impossible.");

            int i = 0;
            int n = a.WholeNumerator / b;
            int d = a.Denominator;

            return Simplify(i, n, d);
        }

        #endregion
    }
}
