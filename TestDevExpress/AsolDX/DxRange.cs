// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Noris.Clients.Win.Components.AsolDX
{
    #region class DecimalRange : Rozsah hodnot { Begin - Size - End } : Int32, Int32
    /// <summary>
    /// Rozsah Od-Do v rámci Decimal
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public class DecimalRange : AnyRange<Decimal, Decimal>
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin">Počátek intervalu</param>
        /// <param name="end">Konec intervalu</param>
        public DecimalRange(Decimal begin, Decimal end) : base(begin, end, false) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin">Počátek intervalu</param>
        /// <param name="end">Konec intervalu</param>
        /// <param name="isVariable">true pokud ve vytvořené instanci má být možno měnit hodnoty, false (default) = ReadOnly</param>
        public DecimalRange(Decimal begin, Decimal end, bool isVariable) : base(begin, end, isVariable) { }
        /// <summary>
        /// Konstruktor: vytvoří proměnnou instanci
        /// </summary>
        public DecimalRange() : base(0m, 0m, true) { }
        /// <summary>
        /// Vrátí new instanci vytvořenou klonováním this instance
        /// </summary>
        /// <returns></returns>
        public DecimalRange Clone() { return new DecimalRange(Begin, End); }
        /// <summary>
        /// Porovná dvě hodnoty a vrátí jejich pozici na ose.
        /// Vrací -1 pokud <paramref name="a"/> je menší než <paramref name="b"/>;
        /// Vrací 0 pokud <paramref name="a"/> je rovno <paramref name="b"/>;
        /// Vrací +1 pokud <paramref name="a"/> je větší než <paramref name="b"/>;
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected override int Compare(Decimal a, Decimal b) { return a.CompareTo(b); }
        /// <summary>
        /// Vrátí klon this instance
        /// </summary>
        /// <returns></returns>
        protected override AnyRange<Decimal, Decimal> CreateClone() { return new DecimalRange(Begin, End); }
        /// <summary>
        /// Vrátí new instanci shodné třídy
        /// </summary>
        /// <param name="begin">Počátek intervalu</param>
        /// <param name="end">Konec intervalu</param>
        /// <param name="isVariable">true pokud ve vytvořené instanci má být možno měnit hodnoty, false (default) = ReadOnly</param>
        /// <returns></returns>
        protected override AnyRange<Decimal, Decimal> CreateNew(Decimal begin, Decimal end, bool isVariable) { return new DecimalRange(begin, end, isVariable); }
        /// <summary>
        /// Vrátí délku prostoru od <paramref name="begin"/> do <paramref name="end"/>, 
        /// vrací tedy výsledek operace (<paramref name="end"/> - <paramref name="begin"/>)
        /// </summary>
        /// <param name="begin">Hodnota počátku</param>
        /// <param name="end">Hodnota konce</param>
        /// <returns></returns>
        protected override Decimal Distance(Decimal begin, Decimal end) { return end - begin; }
        /// <summary>
        /// Vratí souhrn dvou intervalů = od toho nižšího Begin do toho vyššího End.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static DecimalRange Union(DecimalRange value1, DecimalRange value2)
        {
            var b = (value1.Begin < value2.Begin ? value1.Begin : value2.Begin);
            var e = (value1.End > value2.End ? value1.End : value2.End);
            bool isVariable = value1.IsVariable && value2.IsVariable;
            return new DecimalRange(b, e, isVariable);
        }
        /// <summary>
        /// Vrátí průsečík dvou intervalů. Pokud jej nelze najít, vrátí null. Pokud je průsečík nulový (Begin = End), vrátí takový objekt.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static DecimalRange Intersect(DecimalRange value1, DecimalRange value2)
        {
            var b = (value1.Begin > value2.Begin ? value1.Begin : value2.Begin);
            var e = (value1.End < value2.End ? value1.End : value2.End);
            if (b > e) return null;
            bool isVariable = value1.IsVariable && value2.IsVariable;
            return new DecimalRange(b, e, isVariable);
        }
    }
    #endregion
    #region class Int32Range : Rozsah hodnot { Begin - Size - End } : Int32, Int32
    /// <summary>
    /// Rozsah Od-Do v rámci Int32
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public class Int32Range : AnyRange<int, int>
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public Int32Range(int begin, int end) : base(begin, end, false) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin">Počátek intervalu</param>
        /// <param name="end">Konec intervalu</param>
        /// <param name="isVariable">true pokud ve vytvořené instanci má být možno měnit hodnoty, false (default) = ReadOnly</param>
        public Int32Range(int begin, int end, bool isVariable) : base(begin, end, isVariable) { }
        /// <summary>
        /// Konstruktor: vytvoří proměnnou instanci
        /// </summary>
        public Int32Range() : base(0, 0, true) { }
        /// <summary>
        /// Vrátí new instanci vytvořenou klonováním this instance
        /// </summary>
        /// <returns></returns>
        public Int32Range Clone() { return new Int32Range(Begin, End, IsVariable); }
        /// <summary>
        /// Porovná dvě hodnoty a vrátí jejich pozici na ose.
        /// Vrací -1 pokud <paramref name="a"/> je menší než <paramref name="b"/>;
        /// Vrací 0 pokud <paramref name="a"/> je rovno <paramref name="b"/>;
        /// Vrací +1 pokud <paramref name="a"/> je větší než <paramref name="b"/>;
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected override int Compare(int a, int b) { return a.CompareTo(b); }
        /// <summary>
        /// Vrátí klon this instance
        /// </summary>
        /// <returns></returns>
        protected override AnyRange<int, int> CreateClone() { return new Int32Range(Begin, End, IsVariable); }
        /// <summary>
        /// Vrátí new instanci shodné třídy
        /// </summary>
        /// <param name="begin">Počátek intervalu</param>
        /// <param name="end">Konec intervalu</param>
        /// <param name="isVariable">true pokud ve vytvořené instanci má být možno měnit hodnoty, false (default) = ReadOnly</param>
        /// <returns></returns>
        protected override AnyRange<int, int> CreateNew(int begin, int end, bool isVariable) { return new Int32Range(begin, end, isVariable); }
        /// <summary>
        /// Vrátí délku prostoru od <paramref name="begin"/> do <paramref name="end"/>, 
        /// vrací tedy výsledek operace (<paramref name="end"/> - <paramref name="begin"/>)
        /// </summary>
        /// <param name="begin">Hodnota počátku</param>
        /// <param name="end">Hodnota konce</param>
        /// <returns></returns>
        protected override int Distance(int begin, int end) { return end - begin; }
        /// <summary>
        /// Vratí souhrn dvou intervalů = od toho nižšího Begin do toho vyššího End.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static Int32Range Union(Int32Range value1, Int32Range value2)
        {
            var b = (value1.Begin < value2.Begin ? value1.Begin : value2.Begin);
            var e = (value1.End > value2.End ? value1.End : value2.End);
            bool isVariable = value1.IsVariable && value2.IsVariable;
            return new Int32Range(b, e, isVariable);
        }
        /// <summary>
        /// Vrátí průsečík dvou intervalů. Pokud jej nelze najít, vrátí null. Pokud je průsečík nulový (Begin = End), vrátí takový objekt.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static Int32Range Intersect(Int32Range value1, Int32Range value2)
        {
            if (value1 is null || value2 is null) return null;
            var b = (value1.Begin > value2.Begin ? value1.Begin : value2.Begin);
            var e = (value1.End < value2.End ? value1.End : value2.End);
            if (b > e) return null;
            bool isVariable = value1.IsVariable && value2.IsVariable;
            return new Int32Range(b, e, isVariable);
        }
        /// <summary>
        /// Metoda vrátí true, pokud dané dvě hodnoty mají průsečík kladné velikosti.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static bool HasIntersect(Int32Range value1, Int32Range value2)
        {
            if (value1 is null || value2 is null) return false;
            var b = (value1.Begin > value2.Begin ? value1.Begin : value2.Begin);
            var e = (value1.End < value2.End ? value1.End : value2.End);
            return (b < e);
        }
    }
    #endregion
    #region class TimeRange : Rozsah hodnot { Begin - Size - End } : DateTime, TimeSpan
    /// <summary>
    /// Rozsah Od-Do v rámci DateTime + TimeSpan
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public class TimeRange : AnyRange<DateTime, TimeSpan>
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin">Počátek intervalu</param>
        /// <param name="end">Konec intervalu</param>
        public TimeRange(DateTime begin, DateTime end) : base(begin, end, false) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin">Počátek intervalu</param>
        /// <param name="end">Konec intervalu</param>
        /// <param name="isVariable">true pokud ve vytvořené instanci má být možno měnit hodnoty, false (default) = ReadOnly</param>
        public TimeRange(DateTime begin, DateTime end, bool isVariable) : base(begin, end, isVariable) { }
        /// <summary>
        /// Konstruktor: vytvoří proměnnou instanci
        /// </summary>
        public TimeRange() : base(DateTime.MinValue, DateTime.MinValue, true) { }
        /// <summary>
        /// Vrátí new instanci vytvořenou klonováním this instance
        /// </summary>
        /// <returns></returns>
        public TimeRange Clone() { return new TimeRange(Begin, End, IsVariable); }
        /// <summary>
        /// Porovná dvě hodnoty a vrátí jejich pozici na ose.
        /// Vrací -1 pokud <paramref name="a"/> je menší než <paramref name="b"/>;
        /// Vrací 0 pokud <paramref name="a"/> je rovno <paramref name="b"/>;
        /// Vrací +1 pokud <paramref name="a"/> je větší než <paramref name="b"/>;
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected override int Compare(DateTime a, DateTime b) { return a.CompareTo(b); }
        /// <summary>
        /// Vrátí klon this instance
        /// </summary>
        /// <returns></returns>
        protected override AnyRange<DateTime, TimeSpan> CreateClone() { return new TimeRange(Begin, End, IsVariable); }
        /// <summary>
        /// Vrátí new instanci shodné třídy
        /// </summary>
        /// <returns></returns>
        protected override AnyRange<DateTime, TimeSpan> CreateNew(DateTime begin, DateTime end, bool isVariable) { return new TimeRange(begin, end, isVariable); }
        /// <summary>
        /// Vrátí délku prostoru od <paramref name="begin"/> do <paramref name="end"/>, 
        /// vrací tedy výsledek operace (<paramref name="end"/> - <paramref name="begin"/>)
        /// </summary>
        /// <param name="begin">Hodnota počátku</param>
        /// <param name="end">Hodnota konce</param>
        /// <returns></returns>
        protected override TimeSpan Distance(DateTime begin, DateTime end) { return end - begin; }
        /// <summary>
        /// Vratí souhrn dvou intervalů = od toho nižšího Begin do toho vyššího End.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static TimeRange Union(TimeRange value1, TimeRange value2)
        {
            var b = (value1.Begin < value2.Begin ? value1.Begin : value2.Begin);
            var e = (value1.End > value2.End ? value1.End : value2.End);
            bool isVariable = value1.IsVariable && value2.IsVariable;
            return new TimeRange(b, e, isVariable);
        }
        /// <summary>
        /// Vrátí průsečík dvou intervalů. Pokud jej nelze najít, vrátí null. Pokud je průsečík nulový (Begin = End), vrátí takový objekt.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static TimeRange Intersect(TimeRange value1, TimeRange value2)
        {
            var b = (value1.Begin > value2.Begin ? value1.Begin : value2.Begin);
            var e = (value1.End < value2.End ? value1.End : value2.End);
            if (b > e) return null;
            bool isVariable = value1.IsVariable && value2.IsVariable;
            return new TimeRange(b, e, isVariable);
        }
    }
    #endregion
    #region class AnyRange : abstract class, Rozsah hodnot { Begin - Size - End }
    /// <summary>
    /// Rozsah Od-Do abstraktní
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public abstract class AnyRange<TEdge, TSize>
    {
        #region Public vrstva - konstruktor, property
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="begin">Počátek intervalu</param>
        /// <param name="end">Konec intervalu</param>
        /// <param name="isVariable">true pokud ve vytvořené instanci má být možno měnit hodnoty, false (default) = ReadOnly</param>
        public AnyRange(TEdge begin, TEdge end, bool isVariable) { __Begin = begin; __End = end; __Size = Distance(begin, end); __IsVariable = isVariable; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Begin} ÷ {End}";
        }
        /// <summary>
        /// DebugText
        /// </summary>
        protected virtual string DebugText { get { return $"Begin: {Begin}; End: {End}"; } }
        /// <summary>
        /// Počátek rozsahu (včetně)
        /// </summary>
        public TEdge Begin { get { return __Begin; } set { CheckVariable("Begin"); __Begin = value; __Size = Distance(__Begin, __End); } } TEdge __Begin;
        /// <summary>
        /// Konec rozsahu (mimo)
        /// </summary>
        public TEdge End { get { return __End; } set { CheckVariable("End"); __End = value; __Size = Distance(__Begin, __End); } } TEdge __End;
        /// <summary>
        /// Vloží obě hodnoty <paramref name="begin"/> i <paramref name="end"/> najednou.
        /// Instance musí být proměnná (<see cref="IsVariable"/>), jinak dojde k chybě.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public void Store(TEdge begin, TEdge end) { CheckVariable("Store"); __Begin = begin; __End = end; __Size = Distance(begin, end); }
        /// <summary>
        /// Délka rozsahu
        /// </summary>
        public TSize Size { get { return __Size; } } TSize __Size;
        /// <summary>
        /// Obsahuje true, pokud lze změnit, nebo false (default) pokud instance je od vytvoření neměnná
        /// </summary>
        public bool IsVariable { get { return __IsVariable; } } bool __IsVariable;
        /// <summary>
        /// Pokud this instance je Variable (<see cref="IsVariable"/> je true), pak je vše OK.
        /// Pokud není, dojde k chybě. Do chybové hlášky se vloží dodané jméno property <paramref name="propertyName"/> a jméno aktuálního typu (=potomek).
        /// </summary>
        /// <param name="propertyName"></param>
        protected void CheckVariable(string propertyName)
        {
            if (IsVariable) return;
            throw new FieldAccessException($"AnyRange can not set value to '{propertyName}', instance {this.GetType().Name} is not Variable.");
        }
        /// <summary>
        /// Obsahuje true u hodnot, kdy <see cref="End"/> je větší než <see cref="Begin"/>, tedy <see cref="Size"/> je kladné
        /// </summary>
        public bool IsPositive { get { return (Sign > 0); } }
        /// <summary>
        /// Obsahuje true u hodnot, kdy <see cref="End"/> je rovno <see cref="Begin"/>, tedy <see cref="Size"/> je nula
        /// </summary>
        public bool IsZero { get { return (Sign == 0); } }
        /// <summary>
        /// Obsahuje true u hodnot, kdy <see cref="End"/> je menší než <see cref="Begin"/>, tedy <see cref="Size"/> je záporné
        /// </summary>
        public bool IsNegative { get { return (Sign < 0); } }
        /// <summary>
        /// Obsahuje výsledek porovnání <see cref="Compare(TEdge, TEdge)"/>(<see cref="End"/>, <see cref="Begin"/>) : 
        /// -1 pokud <see cref="End"/> je menší než <see cref="Begin"/>;  
        /// 0 pokud <see cref="End"/> == <see cref="Begin"/>,  
        /// +1 pokud <see cref="End"/> je bětší než <see cref="Begin"/>
        /// </summary>
        protected int Sign { get { return Compare(End, Begin); } }
        #endregion
        #region Abstract protected
        /// <summary>
        /// Vrátí délku prostoru od <paramref name="begin"/> do <paramref name="end"/>, 
        /// vrací tedy výsledek operace (<paramref name="end"/> - <paramref name="begin"/>)
        /// </summary>
        /// <param name="begin">Hodnota počátku</param>
        /// <param name="end">Hodnota konce</param>
        /// <returns></returns>
        protected abstract TSize Distance(TEdge begin, TEdge end);
        /// <summary>
        /// Porovná dvě hodnoty a vrátí jejich pozici na ose.
        /// Vrací -1 pokud <paramref name="a"/> je menší než <paramref name="b"/>;
        /// Vrací 0 pokud <paramref name="a"/> je rovno <paramref name="b"/>;
        /// Vrací +1 pokud <paramref name="a"/> je větší než <paramref name="b"/>;
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected abstract int Compare(TEdge a, TEdge b);
        /// <summary>
        /// Vrátí klon this instance
        /// </summary>
        /// <returns></returns>
        protected abstract AnyRange<TEdge, TSize> CreateClone();
        /// <summary>
        /// Vrátí new instanci shodné třídy
        /// </summary>
        /// <param name="begin">Počátek intervalu</param>
        /// <param name="end">Konec intervalu</param>
        /// <param name="isVariable">true pokud ve vytvořené instanci má být možno měnit hodnoty, false (default) = ReadOnly</param>
        /// <returns></returns>
        protected abstract AnyRange<TEdge, TSize> CreateNew(TEdge begin, TEdge end, bool isVariable);
        /// <summary>
        /// Vrátí menší z daných hodnot
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected TEdge Min(TEdge a, TEdge b)
        {
            int compare = Compare(a, b);
            return (compare < 0 ? a : b);
        }
        /// <summary>
        /// Vrátí větší z daných hodnot
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected TEdge Max(TEdge a, TEdge b)
        {
            int compare = Compare(a, b);
            return (compare > 0 ? a : b);
        }
        #endregion
        #region Contains
        /// <summary>
        /// Vrátí true, pokud this instance pokrývá daný bod <paramref name="point"/>.
        /// Hodnota rovná <see cref="End"/> se akceptuje podle parametru <paramref name="acceptEnd"/>.
        /// </summary>
        /// <param name="point">Testovaný bod</param>
        /// <param name="acceptEnd">Akceptovat jako vyhovující bod i ten, který je roven End?</param>
        /// <returns></returns>
        public bool Contains(TEdge point, bool acceptEnd = false)
        {
            int sign = Sign;
            if (sign < 0) return false;                    // Tudy odejde situace, kdy this je negativní (End < Begin)

            int compareBegin = Compare(point, Begin);      // (point - Begin): -1 = point je menší než Begin
            int compareEnd = Compare(point, End);          // (point - End):   +1 = point je větší než End
            return ((compareBegin >= 0) && (compareEnd < 0 || (compareEnd == 0 && acceptEnd)));      // (point >= Begin) and ((point < End) or (point == End and acceptEnd))
        }
        #endregion
        #region HashCode, Equals, Operátory
        /// <summary>
        /// GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.Begin.GetHashCode() ^ this.End.GetHashCode();
        }
        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (!(obj is AnyRange<TEdge, TSize> other)) return false;
            return (Compare(this.Begin, other.Begin) == 0 && Compare(this.End, other.End) == 0);
        }
        /// <summary>
        /// Porovná dvě instance, zda obsahují shodné hodnoty
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(AnyRange<TEdge, TSize> a, AnyRange<TEdge, TSize> b)
        {
            bool aEmpty = ((object)a == null);
            bool bEmpty = ((object)b == null);
            if (aEmpty && bEmpty) return true;
            else if (aEmpty || bEmpty) return false;

            int compareBegins = a.Compare(a.Begin, b.Begin);
            int compareEnds = a.Compare(a.End, b.End);
            return (compareBegins == 0 && compareEnds == 0);
        }
        /// <summary>
        /// Porovná dvě instance, zda obsahují neshodné hodnoty
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(AnyRange<TEdge, TSize> a, AnyRange<TEdge, TSize> b)
        {
            return (!(a == b));
        }
        /// <summary>
        /// Násobení dvou intervalů = výsledkem je průnik (=společný interval). 
        /// Výsledkem může být null, pokud vstupní data nejsou platná (jsou záporná) nebo neexistuje průnik.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static AnyRange<TEdge, TSize> operator *(AnyRange<TEdge, TSize> a, AnyRange<TEdge, TSize> b)
        {
            bool aEmpty = ((object)a == null || a.IsNegative);
            bool bEmpty = ((object)b == null || b.IsNegative);
            if (aEmpty && bEmpty) return null;
            if (bEmpty) return a.CreateClone();
            if (aEmpty) return b.CreateClone();

            TEdge resultBegin = a.Max(a.Begin, b.Begin);
            TEdge resultEnd = a.Min(a.End, b.End);
            bool isVariable = a.IsVariable && b.IsVariable;
            int resultCompare = a.Compare(resultEnd, resultBegin);
            return (resultCompare < 0 ? null : a.CreateNew(resultBegin, resultEnd, isVariable));
        }
        /// <summary>
        /// Sčítání dvou intervalů = výsledkem je souhrn (=od menšího Begin po větší End)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static AnyRange<TEdge, TSize> operator +(AnyRange<TEdge, TSize> a, AnyRange<TEdge, TSize> b)
        {
            bool aEmpty = ((object)a == null || a.IsNegative);
            bool bEmpty = ((object)b == null || b.IsNegative);
            if (aEmpty && bEmpty) return null;
            if (bEmpty) return a.CreateClone();
            if (aEmpty) return b.CreateClone();

            TEdge resultBegin = a.Min(a.Begin, b.Begin);
            TEdge resultEnd = a.Max(a.End, b.End);
            bool isVariable = a.IsVariable && b.IsVariable;
            return a.CreateNew(resultBegin, resultEnd, isVariable);
        }
        #endregion
    }
    #endregion
}
