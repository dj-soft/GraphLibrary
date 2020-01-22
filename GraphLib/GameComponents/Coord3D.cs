// Supervisor: David Janáček
// S využitím: http://geomalgorithms.com/index.html

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.GameComponents
{
    #region class Point3D
    /// <summary>
    /// Souřadnice 3D bodu
    /// <para/>
    /// Nativní souřadný systém si představujme jako : 
    /// X (vodorovně zleva doprava, kladné vpravo);
    /// Y (svislé, kladné nahoru);
    /// Z (vodorovné, od pozorovatele vpřed, kladné ve směru pohledu očí)
    /// </summary>
    public class Point3D : BaseXD
    {
        #region Konstruktory a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Point3D(double x, double y, double z)
            : base()
        {
            _X = x;
            _Y = y;
            _Z = z;
        }
        /// <summary>
        /// Nulový bod, všechny jeho souřadnice jsou 0
        /// </summary>
        public static Point3D Empty { get { return new Point3D(0d, 0d, 0d); } }
        /// <summary>
        /// Textové vyjádření obsahu prvku
        /// </summary>
        public override string Text { get { return base.Text + $"; X: {_X}; Y: {_Y}; Z: {_Z}"; } }
        /// <summary>
        /// Souřadnice X
        /// </summary>
        public double X { get { return _X; } set { this.ResetId(); _X = value; } }
        private double _X;
        /// <summary>
        /// Souřadnice Y
        /// </summary>
        public double Y { get { return _Y; } set { this.ResetId(); _Y = value; } }
        private double _Y;
        /// <summary>
        /// Souřadnice Z
        /// </summary>
        public double Z { get { return _Z; } set { this.ResetId(); _Z = value; } }
        private double _Z;
        #endregion
        #region Délky a úhly
        /// <summary>
        /// Délka přepony X-Y
        /// </summary>
        public double HypXY { get { return Math3D.GetHypotenuse(_X, _Y); } }
        /// <summary>
        /// Délka přepony X-Y
        /// </summary>
        public double HypXZ { get { return Math3D.GetHypotenuse(_X, _Z); } }
        /// <summary>
        /// Délka přepony X-Y
        /// </summary>
        public double HypYZ { get { return Math3D.GetHypotenuse(_Y, _Z); } }
        /// <summary>
        /// Délka přepony X-Y-Z
        /// </summary>
        public double HypXYZ { get { return Math3D.GetHypotenuse(_X, _Y, _Z); } }
        /// <summary>
        /// Úhel ve vodorovné rovině X-Y
        /// </summary>
        public Angle2D AngleH { get { return new Angle2D(_X, _Z); } }
        /// <summary>
        /// Úhel ve svislé rovině XY-Z
        /// </summary>
        public Angle2D AngleV { get { return new Angle2D(HypXZ, _Y); } }
        /// <summary>
        /// Úhel 3D
        /// </summary>
        public Angle3D Angle { get { return new Angle3D(_X, _Y, _Z); } }
        #endregion
        #region Matematika
        /// <summary>
        /// Metoda vrátí skalární součin dvou vektorů (dot product), 
        /// dle vzorce: r = (a.Vector.X * b.Vector.X) +  (a.Vector.Y * b.Vector.Y).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Double ScalarProduct(Point3D a, Point3D b)
        {
            return (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
        }

        #endregion
        #region Sčítání, odčítání, násobení, porovnání
        /// <summary>
        /// Sčítání: result = a + b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Point3D operator +(Point3D a, Point3D b) { return new Point3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
        /// <summary>
        /// Odečítání: result = a - b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Point3D operator -(Point3D a, Point3D b) { return new Point3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
        /// <summary>
        /// Násobení: result = a * q
        /// </summary>
        /// <param name="a">Výchozí bod</param>
        /// <param name="q">Koeficient násobení, výsledek bude mít všechny souřadnice vynásobené</param>
        /// <returns></returns>
        public static Point3D operator *(Point3D a, double q) { return new Point3D(a.X * q, a.Y * q, a.Z * q); }
        /// <summary>
        /// Operátor "je rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Point3D a, Point3D b) { return IsEqual(a, b, () => IsEqualValues(a, b)); }
        /// <summary>
        /// Operátor "není rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(Point3D a, Point3D b) { return !IsEqual(a, b, () => IsEqualValues(a, b)); }
        /// <summary>
        /// Metoda vrátí true, pokud dané dvě instance obsahují shodná data.
        /// Obě instance musí být not null, jinak dojde k chybě.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected static bool IsEqualValues(Point3D a, Point3D b) { return (a.X == b.X && a.Y == b.Y && a.Z == b.Z); }
        /// <summary>
        /// Vrátí hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() { return CalculateHashCode(this.X, this.Y, this.Z); }
        /// <summary>
        /// Vrací příznak rovnosti
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return (this == (obj as Point3D));
        }
        #endregion
    }
    #endregion
    #region class Angle3D
    /// <summary>
    /// Směr vektoru v prostoru
    /// </summary>
    public class Angle3D : BaseXD
    {
        #region Konstruktory a data
        /// <summary>
        /// Konstruktor ze dvou úhlů
        /// </summary>
        /// <param name="angleH"></param>
        /// <param name="angleV"></param>
        public Angle3D(Angle2D angleH, Angle2D angleV)
            : base()
        {
            this.AngleH = angleH;
            this.AngleV = angleV;
        }
        /// <summary>
        /// Konstruktor ze tří souřadnic
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="dz"></param>
        public Angle3D(double dx, double dy, double dz)
            : base()
        {
            _SetCoords(dx, dy, dz);
        }
        /// <summary>
        /// Konstruktor ze souřadnic daného bodu
        /// </summary>
        /// <param name="point3D"></param>
        public Angle3D(Point3D point3D)
            : base()
        {
            _SetCoords(point3D.X, point3D.Y, point3D.Z);
        }
        /// <summary>
        /// Text prvku
        /// </summary>
        public override string Text { get { return base.Text + $"; H: {AngleH.DegreeDMS}; V: {AngleV.DegreeDMS}"; } }
        /// <summary>
        /// Úhel horizontální, rovnoběžný s rovinou X-Z, počínaje 0 = směr osy X ke kladným hodnotám, 
        /// úhel narůstá ve směru k ose Z k jejím kladným hodnotám (90°) = proti směru hodinových ručiček, 
        /// pak dále k záporným hodnotám osy X (180°) a nakonec k záporným hodnotám osy Z (270°).
        /// <para/>
        /// Z hlediska postupného vkládání dat a pravidla pro hodnotu <see cref="AngleV"/> je třeba vkládat nejprve horizontální úhel <see cref="AngleH"/>,
        /// a poté vertikální úhel <see cref="AngleV"/> (protože vložení jeho hodnoty může vést k obrácení hodnoty <see cref="AngleH"/>).
        /// </summary>
        public Angle2D AngleH { get { return _AngleH; } set { _SetAngleH(value); } }
        private Angle2D _AngleH;
        /// <summary>
        /// Úhel vertikální, ve svislé rovině, kde hodnota 0 reprezentuje vektor v rovině X-Z ve směru úhlu <see cref="AngleV"/>, 
        /// kde kladná hodnota představuje vektor mířící "vzhůru" ve směru kladné osy Y;
        /// kde hodnota 0.5d * <see cref="Math.PI"/> (=90°) reprezentuje svislý vektor směřující nahoru = kladná osa Y;
        /// kde hodnota 1.5d až 2.0d * <see cref="Math.PI"/> (=270° až 360°)  reprezentují dolní "polokouli" (v záporných hodnotách osy Y).
        /// Hodnoty 0.5d až 1.5d * <see cref="Math.PI"/> (=90° až 270°) se zde nevyskytují, protože reprezentují odvrácenou stranu úhlu <see cref="AngleH"/>.
        /// Vložení takového úhlu do <see cref="AngleV"/> provede obrácení hodnoty <see cref="AngleH"/> o 180° a vložení odpovídající hodnoty do <see cref="AngleV"/>.
        /// <para/>
        /// Z hlediska postupného vkládání dat a pravidla pro hodnotu <see cref="AngleV"/> je třeba vkládat nejprve horizontální úhel <see cref="AngleH"/>,
        /// a poté vertikální úhel <see cref="AngleV"/> (protože vložení jeho hodnoty může vést k obrácení hodnoty <see cref="AngleH"/>).
        /// </summary>
        public Angle2D AngleV { get { return _AngleV; } set { _SetAngleV(value); } }
        private Angle2D _AngleV;
        /// <summary>
        /// Vloží daný úhel do <see cref="_AngleH"/>, provede potřebné přepočty
        /// </summary>
        /// <param name="angleH"></param>
        private void _SetAngleH(Angle2D angleH)
        {
            _AngleH = angleH;
            _TestMirrorAngleV();
            ResetId();
        }
        /// <summary>
        /// Vloží daný úhel do <see cref="_AngleV"/>, provede potřebné přepočty
        /// </summary>
        /// <param name="angleV"></param>
        private void _SetAngleV(Angle2D angleV)
        {
            _AngleV = angleV;
            _TestMirrorAngleV();
            ResetId();
        }
        /// <summary>
        /// Zajistí otočení úhlů do kladné polokoule = úhel <see cref="AngleV"/> je vždy v rozsahu 0° - 90° anebo 270° - 0°
        /// </summary>
        private void _TestMirrorAngleV()
        {
            if (_AngleH.IsNull() || _AngleV.IsNull()) return;
            Double degrees = _AngleV.Degrees;
            if (degrees > 90d && degrees < 270d)
            {   // Vertikální úhel se pohybuje v protilehlé straně k úhlu AngleH, proto tedy úhel AngleH obrátíme na druhou stranu a úhel AngleV zrcadlíme:
                //  a) Pracujeme pro úhly > 90 a < 270;
                //  b) Úhly  91 až 180 převádíme na úhly  89 až   0;
                //  c) Úhly 181 až 269 převádíme na úhly 359 až 271;
                _AngleH = _AngleH.Negative;
                _AngleV = _AngleV.MirrorY;
                ResetId();
            }
        }
        #endregion
        #region Souřadnice jednotkového bodu
        /// <summary>
        /// Souřadnice jednotkového bodu.
        /// Je to bod, který je vzdálen 1.0 od bodu {0, 0, 0} v this úhlu.
        /// </summary>
        public Point3D Point
        {
            get
            {
                Point2D pointV = this._AngleV.Point;
                double dy = pointV.Y;
                double xy = pointV.X;
                Point2D pointH = this._AngleH.Point;
                double dx = xy * pointH.X;
                double dz = xy * pointH.Y;
                return new Point3D(dx, dy, dz);
            }
            set { _SetCoords(value.X, value.Y, value.Z); }
        }
        /// <summary>
        /// Do this instance vepíše úhel odpovídající daným souřadnicím cílového bodu.
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="dz"></param>
        private void _SetCoords(double dx, double dy, double dz)
        {
            double xz = Math3D.GetHypotenuse(dx, dz);
            this._AngleH = new Angle2D(dx, dz);
            this._AngleV = new Angle2D(xz, dy);
            this.ResetId();
        }
        #endregion
        #region Sčítání, odčítání, porovnání
        /// <summary>
        /// Sčítání: result = a + b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Angle3D operator +(Angle3D a, Angle3D b) { return new Angle3D(a.AngleH + b.AngleH, a.AngleV + b.AngleV); }
        /// <summary>
        /// Odečítání: result = a - b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Angle3D operator -(Angle3D a, Angle3D b) { return new Angle3D(a.AngleH - b.AngleH, a.AngleV - b.AngleV); }
        /// <summary>
        /// Operátor "je rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Angle3D a, Angle3D b) { return IsEqual(a, b, () => IsEqualValues(a, b)); }
        /// <summary>
        /// Operátor "není rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(Angle3D a, Angle3D b) { return !IsEqual(a, b, () => IsEqualValues(a, b)); }
        /// <summary>
        /// Metoda vrátí true, pokud dané dvě instance obsahují shodná data.
        /// Obě instance musí být not null, jinak dojde k chybě.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected static bool IsEqualValues(Angle3D a, Angle3D b) { return (a.AngleH.Rad == b.AngleH.Rad && a.AngleV.Rad == b.AngleV.Rad); }
        /// <summary>
        /// Vrátí hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() { return CalculateHashCode(this.AngleH.Rad, this.AngleV.Rad); }
        /// <summary>
        /// Vrací příznak rovnosti
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return (this == (obj as Angle3D));
        }
        #endregion
    }
    #endregion
    #region class Vector3D
    /// <summary>
    /// Vektor v prostoru (dva body = prostorový úhel a délka)
    /// </summary>
    public class Vector3D : BaseXD
    {
        #region Konstruktory a data
        /// <summary>
        /// Konstruktor pro čistý vektor = ten, jehož <see cref="OriginPoint"/> je <see cref="Point3D.Empty"/> = { 0, 0 }.
        /// Zadaná hodnota reprezentuje <see cref="TargetPoint"/>
        /// </summary>
        /// <param name="targetPoint"></param>
        public Vector3D(Point3D targetPoint)
            : base()
        {
            OriginPoint = Point3D.Empty;
            TargetPoint = targetPoint;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="originPoint"></param>
        /// <param name="targetPoint"></param>
        public Vector3D(Point3D originPoint, Point3D targetPoint)
            : base()
        {
            OriginPoint = originPoint;
            TargetPoint = targetPoint;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="originPoint"></param>
        /// <param name="angle"></param>
        /// <param name="length"></param>
        public Vector3D(Point3D originPoint, Angle3D angle, Double length)
            : base()
        {
            OriginPoint = originPoint;
            Angle = angle;
            Length = length;
        }
        /// <summary>
        /// Souřadnice bodu 1 = výchozí bod vektoru
        /// </summary>
        public Point3D OriginPoint { get { return _OriginPoint; } set { _OriginPoint = value; _Reset(false, true); } }
        private Point3D _OriginPoint;
        private UInt64? _OriginPointId;
        /// <summary>
        /// Souřadnice bodu 2 = cílový bod vektoru
        /// </summary>
        public Point3D TargetPoint { get { _CheckTarget(); return _TargetPoint; } set { _TargetPoint = value; _Reset(false, true); } }
        private Point3D _TargetPoint;
        private UInt64? _TargetPointId;
        /// <summary>
        /// Úhel 3D z bodu <see cref="OriginPoint"/> do bodu <see cref="TargetPoint"/>
        /// </summary>
        public Angle3D Angle { get { _CheckAngleLength(); return _Angle; } set { _Angle = value; _Reset(true, false); } }
        private Angle3D _Angle;
        private UInt64? _AngleId;
        /// <summary>
        /// Vzdálenost z bodu <see cref="OriginPoint"/> do bodu <see cref="TargetPoint"/>
        /// </summary>
        public Double Length { get { _CheckAngleLength(); return _Length.Value; } set { _Length = value; _Reset(true, false); } }
        private Double? _Length;
        private Double? _LengthId;
        /// <summary>
        /// Čistý vektor = rozdíl (<see cref="TargetPoint"/> - <see cref="OriginPoint"/>), vyjadřuje pouze směr a velikost vektoru. 
        /// Jeho počáteční bod = 0.
        /// Slouží k matematickým výpočtům.
        /// </summary>
        public Point3D Vector { get { return (this.TargetPoint - this.OriginPoint); } }
        /// <summary>
        /// Obsahuje true, pokud this vektor je nulový : jeho počátek == konec, jeho délka == 0
        /// </summary>
        public bool IsZero { get { return ((this._TargetPoint.IsNotNull()) ? (this.OriginPoint == this.TargetPoint) : (this._Length.HasValue ? (this._Length.Value == 0d) : true)); } }
        #endregion
        #region Privátní přepočty mezi hodnotami TargetPoint <=> Angle+Length
        /// <summary>
        /// Resetuje Target a/nebo Angle+Length, vždy interní ID, plus zachová ID platných dat.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="angle"></param>
        private void _Reset(bool target, bool angle)
        {
            if (target)
            {
                _TargetPoint = null;
            }
            if (angle)
            {
                _Angle = null;
                _Length = null;
            }
            _SaveId();
            this.ResetId();
        }
        /// <summary>
        /// Zajistí, že cílový bod bude platný
        /// </summary>
        private void _CheckTarget()
        {
            if (_TargetPoint == null || IsChanged(_OriginPoint, _OriginPointId) || IsChanged(_Angle, _AngleId) || IsChanged(_Length, _LengthId))
            {
                Math3D.CalculateTarget(_OriginPoint, _Angle, _Length.Value, out _TargetPoint);
                _SaveId();
            }
        }
        /// <summary>
        /// Zajistí, že úhel a vzdálenost bude platný
        /// </summary>
        private void _CheckAngleLength()
        {
            if (_Angle == null || !_Length.HasValue || IsChanged(_OriginPoint, _OriginPointId) || IsChanged(_TargetPoint, _TargetPointId))
            {
                double length;
                Math3D.CalculateAngleLength(_OriginPoint, _TargetPoint, out _Angle, out length);
                _Length = length;
                _SaveId();
            }
        }
        /// <summary>
        /// Uloží si ID všech hodnot po jejich vypočítání
        /// </summary>
        private void _SaveId()
        {
            _OriginPointId = _OriginPoint?.Id;
            _TargetPointId = _TargetPoint?.Id;
            _AngleId = _Angle?.Id;
            _LengthId = _Length;
        }
        #endregion
        #region Matematické vyjádření přímky, nalezení bodu na vektoru, určení kolmé roviny
        /// <summary>
        /// Matice vektoru pro výpočty.
        /// Řádky matice obsahují dimenze: [0,] = dX; [1,] = dY; [2,] = dZ;
        /// Sloupce obsahují koeficienty: [,0] = d?0; [,1] = d?t;
        /// <para/>
        /// Souřadnice bodu "t" na vektoru je pak dána výpočtem Pt = { X = dX0 + t * dXt; Y = dY0 + t * dYt; Z = dZ0 + t * dZt; };
        /// konkrétně pro m = Matrix: Pt = { X = m[0,0] + t * m[0,1]; Y = m[1,0] + t * m[1,1]; Z = m[2,0] + t * m[2,1]; };
        /// <para/>
        /// pro t = { -nekonečno až +nekonečno } pro přímku, nebo { 0 až 1 } pro vektor v rozmezí Origin až Target.
        /// </summary>
        public double[,] Matrix
        {
            get
            {
                Point3D p0 = this.OriginPoint;
                Point3D p1 = this.Vector;
                double[,] matrix = new double[3, 2]
                {
                    {  p0.X, p1.X },
                    {  p0.Y, p1.Y },
                    {  p0.Z, p1.Z }
                };
                return matrix;
            }
        }
        /// <summary>
        /// Metoda vrátí skalární součin dvou vektorů (dot product), 
        /// dle vzorce: r = (a.Vector.X * b.Vector.X) +  (a.Vector.Y * b.Vector.Y).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Double ScalarProduct(Vector3D a, Vector3D b)
        {
            return Point3D.ScalarProduct(a.Vector, b.Vector);
        }



        /// <summary>
        /// Vrátí bod na this vektoru, který je vzdálen (<paramref name="distance"/>) od bodu <see cref="OriginPoint"/>
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public Point3D GetPointAtDistance(double distance)
        {
            return this.OriginPoint + (this.Angle.Point * distance);
        }
        /// <summary>
        /// Vrátí bod na this vektoru, který se nachází na relativní pozici (<paramref name="t"/>);
        /// relativně k bodu <see cref="OriginPoint"/> (pro t = 0) až <see cref="TargetPoint"/> (pro t = 1).
        /// Hodnota t smí být libovolná, tj. i mimo rozsaj 0 ÷ 1.
        /// Výpočet probíhá podle matice <see cref="Matrix"/>, která definuje přímku na základě dvou bodů.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Point3D GetPointMatrix(double t)
        {
            var m = Matrix;
            return new Point3D(m[0, 0] + t * m[0, 1], m[1, 0] + t * m[1, 1], m[2, 0] + t * m[2, 1]);
        }
        /// <summary>
        /// Vrátí rovinu kolmou na this vektor, která jej protíná ve vzdálenosti (<paramref name="distance"/>) od bodu <see cref="OriginPoint"/>
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public Plane3D GetPlanePerpendicular(double distance)
        {
            Point3D pointA = GetPointAtDistance(distance);


            return null;
        }

        #endregion
        #region Sčítání, odčítání, porovnání
        /// <summary>
        /// Operátor : Point2D = Point2D + Vector2D; 
        /// kde z vektoru je akceptována pouze jeho čistá část <see cref="Vector3D.Vector"/>,
        /// ignoruje se jeho počáteční bod <see cref="Vector3D.OriginPoint"/>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Point3D operator +(Point3D a, Vector3D b) { var v = b.Vector; return new Point3D(a.X + v.X, a.Y + v.Y, a.Z + v.Z); }
        /// <summary>
        /// Operátor : Point2D = Point2D - Vector2D; 
        /// kde z vektoru je akceptována pouze jeho čistá část <see cref="Vector3D.Vector"/>,
        /// ignoruje se jeho počáteční bod <see cref="Vector3D.OriginPoint"/>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Point3D operator -(Point3D a, Vector3D b) { var v = b.Vector; return new Point3D(a.X - v.X, a.Y - v.Y, a.Z - v.Z); }

        /// <summary>
        /// Operátor "je rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Vector3D a, Vector3D b) { return IsEqual(a, b, () => IsEqualValues(a, b)); }
        /// <summary>
        /// Operátor "není rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(Vector3D a, Vector3D b) { return !IsEqual(a, b, () => IsEqualValues(a, b)); }
        /// <summary>
        /// Metoda vrátí true, pokud dané dvě instance obsahují shodná data.
        /// Obě instance musí být not null, jinak dojde k chybě.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected static bool IsEqualValues(Vector3D a, Vector3D b) { return (a.OriginPoint == b.OriginPoint && a.TargetPoint == b.TargetPoint); }
        /// <summary>
        /// Vrátí hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() { return CalculateHashCode(this.OriginPoint.GetHashCode(), this.TargetPoint.GetHashCode()); }
        /// <summary>
        /// Vrací příznak rovnosti
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return (this == (obj as Vector3D));
        }
        #endregion
    }
    #endregion
    #region class Polygon3D
    /// <summary>
    /// Víceúhelník
    /// </summary>
    public class Polygon3D : BaseXD, IEnumerable<Point3D>
    {
        #region Konstruktory a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="points"></param>
        public Polygon3D(params Point3D[] points)
        {
            this._Points = points;
        }
        /// <summary>
        /// Jednotlivé body
        /// </summary>
        public Point3D[] Points { get { return _Points; } set { ResetId(); _Points = value; } }
        private Point3D[] _Points;
        /// <summary>
        /// Do this pole přidá další body
        /// </summary>
        /// <param name="points"></param>
        public void AddPoints(params Point3D[] points)
        {
            if (points != null && points.Length > 0)
                Points = Points.Union(points).ToArray();
        }
        /// <summary>
        /// Do this pole přidá další body
        /// </summary>
        /// <param name="points"></param>
        public void AddPoints(IEnumerable<Point3D> points)
        {
            if (points != null)
                Points = Points.Union(points).ToArray();
        }
        /// <summary>
        /// Počet bodů
        /// </summary>
        public int PointCount { get { return _Points.Length; } }
        /// <summary>
        /// Typový Enumerátor
        /// </summary>
        /// <returns></returns>
        IEnumerator<Point3D> IEnumerable<Point3D>.GetEnumerator() { return this._Points.AsEnumerable<Point3D>().GetEnumerator(); }
        /// <summary>
        /// Enumerátor
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() { return this._Points.GetEnumerator(); }
        #endregion
    }
    #endregion
    #region class Line3D, Triangle3D, Rectangle3D
    /// <summary>
    /// Čára mezi dvěma body
    /// </summary>
    public class Line3D : BaseXD
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        public Line3D(Point3D point1, Point3D point2)
        {
            _Point1 = point1;
            _Point2 = point2;
        }
        /// <summary>
        /// Textové vyjádření obsahu prvku
        /// </summary>
        public override string Text { get { return base.Text + $"; Point1: {_Point1}; Point2: {_Point2}"; } }
        /// <summary>
        /// Bod 1
        /// </summary>
        public Point3D Point1 { get { return _Point1; } set { this.ResetId(); _Point1 = value; } }
        private Point3D _Point1;
        /// <summary>
        /// Bod 2
        /// </summary>
        public Point3D Point2 { get { return _Point2; } set { this.ResetId(); _Point2 = value; } }
        private Point3D _Point2;
    }
    /// <summary>
    /// Trojúhelník
    /// </summary>
    public class Triangle3D : BaseXD
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        public Triangle3D(Point3D point1, Point3D point2, Point3D point3)
            : base()
        {
            _Point1 = point1;
            _Point2 = point2;
            _Point3 = point3;
        }
        /// <summary>
        /// Bod 1
        /// </summary>
        public Point3D Point1 { get { return _Point1; } set { this.ResetId(); _Point1 = value; } }
        private Point3D _Point1;
        /// <summary>
        /// Bod 2
        /// </summary>
        public Point3D Point2 { get { return _Point2; } set { this.ResetId(); _Point2 = value; } }
        private Point3D _Point2;
        /// <summary>
        /// Bod 2
        /// </summary>
        public Point3D Point3 { get { return _Point3; } set { this.ResetId(); _Point3 = value; } }
        private Point3D _Point3;
    }
    /// <summary>
    /// Čtyřúhelník
    /// </summary>
    public class Rectangle3D : BaseXD
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <param name="point4"></param>
        public Rectangle3D(Point3D point1, Point3D point2, Point3D point3, Point3D point4)
            : base()
        {
            _Point1 = point1;
            _Point2 = point2;
            _Point3 = point3;
            _Point4 = point4;
        }
        /// <summary>
        /// Bod 1
        /// </summary>
        public Point3D Point1 { get { return _Point1; } set { this.ResetId(); _Point1 = value; } }
        private Point3D _Point1;
        /// <summary>
        /// Bod 2
        /// </summary>
        public Point3D Point2 { get { return _Point2; } set { this.ResetId(); _Point2 = value; } }
        private Point3D _Point2;
        /// <summary>
        /// Bod 3
        /// </summary>
        public Point3D Point3 { get { return _Point3; } set { this.ResetId(); _Point3 = value; } }
        private Point3D _Point3;
        /// <summary>
        /// Bod 4
        /// </summary>
        public Point3D Point4 { get { return _Point4; } set { this.ResetId(); _Point4 = value; } }
        private Point3D _Point4;
    }
    #endregion
    #region class Plane3D
    /// <summary>
    /// Plane3D : definice roviny a její matematika
    /// </summary>
    public class Plane3D : BaseXD
    {
        #region Konstruktor a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="pointA"></param>
        /// <param name="pointB"></param>
        /// <param name="pointC"></param>
        public Plane3D(Point3D pointA, Point3D pointB, Point3D pointC)
            : base()
        {
            _PointA = pointA;
            _PointB = pointB;
            _PointC = pointC;
        }
        /// <summary>
        /// Bod A
        /// </summary>
        public Point3D PointA { get { return _PointA; } set { this.ResetId(); _PointA = value; } }
        private Point3D _PointA;
        /// <summary>
        /// Bod B
        /// </summary>
        public Point3D PointB { get { return _PointB; } set { this.ResetId(); _PointB = value; } }
        private Point3D _PointB;
        /// <summary>
        /// Bod C
        /// </summary>
        public Point3D PointC { get { return _PointC; } set { this.ResetId(); _PointC = value; } }
        private Point3D _PointC;
        /// <summary>
        /// Vektor z bodu A do bodu B
        /// </summary>
        public Vector3D VectorAB { get { return new Vector3D(this.PointA, this.PointB); } }
        /// <summary>
        /// Vektor z bodu A do bodu C
        /// </summary>
        public Vector3D VectorAC { get { return new Vector3D(this.PointA, this.PointC); } }
        #endregion
        #region Matematické vyjádření roviny
        /// <summary>
        /// Matice vektoru pro výpočty.
        /// Řádky matice obsahují dimenze: [0,] = dX; [1,] = dY; [2,] = dZ;
        /// Sloupce obsahují koeficienty: [,0] = d?0; [,1] = d?s; [,1] = d?t;
        /// <para/>
        /// Souřadnice bodu "s,t" na rovině je pak dána výpočtem Pts = { X = dX0 + t * dX1; Y = dY0 + t * dY1; Z = dZ0 + t * dZ1; };
        /// konkrétně pro m = Matrix: Pt = { X = m[0,0] + t * m[0,1]; Y = m[1,0] + t * m[1,1]; Z = m[2,0] + t * m[2,1]; };
        /// <para/>
        /// pro t = { -nekonečno až +nekonečno } pro přímku, nebo { 0 až 1 } pro vektor v rozmezí Origin až Target.
        /// </summary>
        public double[,] Matrix
        {
            get
            {
                Point3D pa = this.PointA;
                Point3D pb = this.PointB;
                Point3D pc = this.PointC;
                Vector3D v = this.VectorAB;
                Vector3D u = this.VectorAC;

                Point3D p0 = this.OriginPoint;
                Point3D p1 = this.TargetPoint - p0;
                double[,] matrix = new double[3, 2]
                {
                    {  p0.X, p1.X },
                    {  p0.Y, p1.Y },
                    {  p0.Z, p1.Z }
                };
                return matrix;
            }
        }

        /// <summary>
        /// Vrátí souřadnici průsečíku daného vektoru a this roviny
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public Point3D GetIntersect(Vector3D vector)
        {
            return null;
        }
        #endregion
        #region Sčítání, odčítání, porovnání
        /// <summary>
        /// Operátor "je rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Plane3D a, Plane3D b) { return IsEqual(a, b, () => IsEqualValues(a, b)); }
        /// <summary>
        /// Operátor "není rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(Plane3D a, Plane3D b) { return !IsEqual(a, b, () => IsEqualValues(a, b)); }
        /// <summary>
        /// Metoda vrátí true, pokud dané dvě instance obsahují shodná data.
        /// Obě instance musí být not null, jinak dojde k chybě.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected static bool IsEqualValues(Plane3D a, Plane3D b) { return (a.PointA == b.PointA && a.PointB == b.PointB && a.PointC == b.PointC); }
        /// <summary>
        /// Vrátí hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() { return CalculateHashCode(this.PointA.GetHashCode(), this.PointB.GetHashCode(), this.PointC.GetHashCode()); }
        /// <summary>
        /// Vrací příznak rovnosti
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return (this == (obj as Plane3D));
        }
        #endregion

    }
    #endregion
    #region class Point2D
    /// <summary>
    /// Souřadnice 2D bodu
    /// </summary>
    public class Point2D : BaseXD
    {
        #region Konstruktory a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Point2D(double x, double y)
            : base()
        {
            _X = x;
            _Y = y;
        }
        /// <summary>
        /// Nulový bod, všechny jeho souřadnice jsou 0
        /// </summary>
        public static Point2D Empty { get { return new Point2D(0d, 0d); } }
        /// <summary>
        /// Textové vyjádření obsahu prvku
        /// </summary>
        public override string Text { get { return base.Text + $"; X: {_X}; Y: {_Y}"; } }
        /// <summary>
        /// Souřadnice X
        /// </summary>
        public double X { get { return _X; } set { this.ResetId(); _X = value; } }
        private double _X;
        /// <summary>
        /// Souřadnice Y
        /// </summary>
        public double Y { get { return _Y; } set { this.ResetId(); _Y = value; } }
        private double _Y;
        #endregion
        #region Délky a úhly
        /// <summary>
        /// Délka přepony X-Y
        /// </summary>
        public double HypXY { get { return Math3D.GetHypotenuse(_X, _Y); } }
        /// <summary>
        /// Úhel ve vodorovné rovině X-Y
        /// </summary>
        public Angle2D Angle { get { return new Angle2D(_X, _Y); } }
        #endregion
        #region Matematika
        /// <summary>
        /// Metoda vrátí skalární součin dvou souřadnic (dot product), 
        /// dle vzorce: r = (a.X * b.X) +  (a.Y * b.Y).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Double ScalarProduct(Point2D a, Point2D b)
        {
            return (a.X * b.X) + (a.Y + b.Y);
        }
        #endregion
        #region Sčítání, odčítání, násobení, porovnání
        /// <summary>
        /// Sčítání: result = a + b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Point2D operator +(Point2D a, Point2D b) { return new Point2D(a.X + b.X, a.Y + b.Y); }
        /// <summary>
        /// Odečítání: result = a - b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Point2D operator -(Point2D a, Point2D b) { return new Point2D(a.X - b.X, a.Y - b.Y); }
        /// <summary>
        /// Násobení: result = a * q
        /// </summary>
        /// <param name="a">Výchozí bod</param>
        /// <param name="q">Koeficient násobení, výsledek bude mít všechny souřadnice vynásobené</param>
        /// <returns></returns>
        public static Point2D operator *(Point2D a, double q) { return new Point2D(a.X * q, a.Y * q); }
        /// <summary>
        /// Operátor "je rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Point2D a, Point2D b) { return IsEqual(a, b, () => IsEqualValues(a, b)); }
        /// <summary>
        /// Operátor "není rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(Point2D a, Point2D b) { return !IsEqual(a, b, () => IsEqualValues(a, b)); }
        /// <summary>
        /// Metoda vrátí true, pokud dané dvě instance obsahují shodná data.
        /// Obě instance musí být not null, jinak dojde k chybě.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected static bool IsEqualValues(Point2D a, Point2D b) { return (a.X == b.X && a.Y == b.Y); }
        /// <summary>
        /// Vrátí hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() { return CalculateHashCode(this.X, this.Y); }
        /// <summary>
        /// Vrací příznak rovnosti
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return (this == (obj as Point2D));
        }
        #endregion
        #region Implicitní konvertory
        /// <summary>
        /// Implicitní konverze
        /// </summary>
        /// <param name="point2D"></param>
        public static implicit operator System.Drawing.PointF(Point2D point2D) { return new System.Drawing.PointF((float)point2D.X, (float)point2D.Y); }
        /// <summary>
        /// Implicitní konverze
        /// </summary>
        /// <param name="point"></param>
        public static implicit operator Point2D(System.Drawing.PointF point) { return new Point2D((double)point.X, (double)point.Y); }
        #endregion
    }
    #endregion
    #region class Angle2D
    /// <summary>
    /// Jeden úhel (radiány, stupně)
    /// </summary>
    public class Angle2D : BaseXD
    {
        #region Konstruktory a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="rad"></param>
        public Angle2D(double rad)
            : base()
        {
            this.Rad = rad;
        }
        /// <summary>
        /// Konstruktor pro zadané hodnoty na ose X a Y
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public Angle2D(double dx, double dy)
        {
            //int cx = dx.CompareTo(0d);
            //int cy = dy.CompareTo(0d);

            //// Vyřeším pravé úhly: doprava, doleva; nahoru, dolů:
            //if (cy == 0) return (cx >= 0 ? Angle.Angle0 : Angle.Angle2);
            //if (cx == 0) return (cy >= 0 ? Angle.Angle1 : Angle.Angle3);

            this.Rad = Math.Atan2(dy, dx);
        }
        /// <summary>
        /// Konstruktor vycházející z hodnoty úhlu ve stupních 0° - 360°
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static Angle2D FromDegrees(double degrees)
        {
            return new Angle2D(ConvertDegreeToRad(degrees));
        }
        /// <summary>
        /// Text prvku
        /// </summary>
        public override string Text { get { return base.Text + $"; {DegreeDMS}"; } }
        /// <summary>
        /// Úhel v radiánech, v rozsahu 0d ÷ (2d * <see cref="Math.PI"/>)
        /// </summary>
        public double Rad { get { return _Rad; } set { this.ResetId(); _Rad = (value % R); } }
        private double _Rad;
        /// <summary>
        /// Úhel ve stupních, v rozsahu 0d ÷ 360d, desetinné zlomky
        /// </summary>
        public double Degrees { get { return ConvertRadToDegree(Rad); } set { Rad = ConvertDegreeToRad(value); } }
        /// <summary>
        /// Stupně vyjádřené jako text "stupně°:minuty':vteřiny''"
        /// </summary>
        public string DegreeDMS { get { return ConvertRadToDMS(Rad); } }
        /// <summary>
        /// Hodnota Pí = 3.1415926535897932384626433832795028841971d; (na 40 míst)
        /// </summary>
        public const double Pi = 3.1415926535897932384626433832795028841971d;
        /// <summary>
        /// Hodnota 2d * <see cref="Pi"/> = 6.2831853071795862.......d; (na 40 míst)
        /// </summary>
        public const double R = 2d * Pi;
        /// <summary>
        /// Hodnota 360 (počet stupňů Degrees)
        /// </summary>
        public const double DEG = 360d;
        #endregion
        #region Mirrory a Protilehlý úhel
        /// <summary>
        /// Obsahuje úhel opačného směru, tedy např. k úhlu 30° je <see cref="Negative"/> úhel = 210°, atd
        /// </summary>
        public Angle2D Negative { get { return new Angle2D(this.Rad + Pi); } }
        /// <summary>
        /// Obsahuje this úhel zrcadlený kolem osy X (=vodorovně = shora dolů a zdola nahoru)
        /// </summary>
        public Angle2D MirrorX { get { return GetMirror(0.00d); } }
        /// <summary>
        /// Obsahuje this úhel zrcadlený kolem osy Y (=svisle = zleva doprava a zprava doleva)
        /// </summary>
        public Angle2D MirrorY { get { return GetMirror(0.25d); } }
        /// <summary>
        /// Vrací this úhel zrcadlený na druhou stranu dané osy, kde osa je dána poměrem 0 - 1 z celého kruhu.
        /// Například zadání <paramref name="relativeAxis"/> = 0.25d vrátí úhel zrcadlený kolem osy Y;
        /// hodnota 0.00d vrátí zrcadlení kolem osy X;
        /// lze zrcadlit kolem libovolného úhlu.
        /// </summary>
        /// <param name="relativeAxis"></param>
        /// <returns></returns>
        public Angle2D GetMirror(double relativeAxis)
        {
            double degrees = this.Degrees;
            double axis = DEG * (relativeAxis % 1d);
            degrees = 2d * axis - degrees;
            if (degrees < 0d) degrees += DEG;
            return Angle2D.FromDegrees(degrees);
        }
        #endregion
        #region Souřadnice jednotkového bodu
        /// <summary>
        /// Souřadnice jednotkového bodu.
        /// Je to bod, který je vzdálen 1.0 od bodu {0, 0} v this úhlu.
        /// <para/>
        /// Např. Pro úhel 30° je souřadnice X = 0.866025404d a souřadnice Y = 0.5d;
        /// pro úhel 45° jsou obě souřadnice X = Y = 0.707106781, 
        /// pro úhel 180% je X = -1d a Y = 0, atd.
        /// </summary>
        public Point2D Point
        {
            get
            {
                double rad = this.Rad;
                double dx = Math.Cos(rad);
                double dy = Math.Sin(rad);
                return new Point2D(dx, dy);
            }
            set
            {
                double dx = value.X;
                double dy = value.Y;
                this.Rad = Math.Atan2(dy, dx);
            }
        }
        #endregion
        #region Statické konstruktory pro čtyři pravé úhly, konverze na stupně, minuty, vteřiny
        /// <summary>
        /// Úhel 0 = přímo v kladné ose X
        /// </summary>
        public static Angle2D Angle0 { get { return new Angle2D(0d); } }
        /// <summary>
        /// Úhel 90° = přímo v kladné ose Y
        /// </summary>
        public static Angle2D Angle1 { get { return new Angle2D(0.5d * Math.PI); } }
        /// <summary>
        /// Úhel 180° = přímo v záporné ose X
        /// </summary>
        public static Angle2D Angle2 { get { return new Angle2D(Math.PI); } }
        /// <summary>
        /// Úhel 270° = přímo v záporné ose Y
        /// </summary>
        public static Angle2D Angle3 { get { return new Angle2D(1.5d * Math.PI); } }
        /// <summary>
        /// Ze zadaného čísla v obloukové míře (radech) vrátí stupně 0-360
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        public static double ConvertRadToDegree(double rad)
        {
            return DEG * (rad % R) / R;
        }
        /// <summary>
        /// Ze zadaného čísla ve stupních (0-360) vrátí obloukovou míru (rad)
        /// </summary>
        /// <param name="degree"></param>
        /// <returns></returns>
        public static double ConvertDegreeToRad(double degree)
        {
            return (degree % DEG) * R / DEG;
        }
        /// <summary>
        /// Ze zadaného čísla v obloukové míře (radech) vrátí úhlové "stupně°:minuty':vteřiny''"
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        public static string ConvertRadToDMS(double rad)
        {
            return ConvertDegreeToDMS(ConvertRadToDegree(rad));
        }
        /// <summary>
        /// Ze zadaného čísla ve stupních vrátí úhlové "stupně°:minuty':vteřiny''"
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static string ConvertDegreeToDMS(double degrees)
        {
            int d0 = ((int)degrees) % 360;       // 0-359
            double d = 60d * (degrees % 1d);     // 0.000 - 59.999
            int d1 = ((int)d);                   // 0 - 59
            d = 60d * d;
            int d2 = ((int)d);
            return d0.ToString() + "°"
                + ((d1 != 0 || d2 != 0) ? ":" + d1.ToString("00") + "'" : "")
                + ((d2 != 0) ? ":" + d2.ToString("00") + "''" : "");
        }
        #endregion
        #region Sčítání, odčítání, porovnání
        /// <summary>
        /// Sčítání: result = a + b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Angle2D operator +(Angle2D a, Angle2D b) { return new Angle2D(a.Rad + b.Rad); }
        /// <summary>
        /// Odečítání: result = a - b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Angle2D operator -(Angle2D a, Angle2D b) { return new Angle2D(a.Rad - b.Rad); }
        /// <summary>
        /// Operátor "je rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Angle2D a, Angle2D b) { return IsEqual(a, b, () => IsEqualValues(a, b)); }
        /// <summary>
        /// Operátor "není rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(Angle2D a, Angle2D b) { return !IsEqual(a, b, () => IsEqualValues(a, b)); }
        /// <summary>
        /// Metoda vrátí true, pokud dané dvě instance obsahují shodná data.
        /// Obě instance musí být not null, jinak dojde k chybě.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected static bool IsEqualValues(Angle2D a, Angle2D b) { return (a.Rad == b.Rad); }
        /// <summary>
        /// Vrátí hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() { return CalculateHashCode(this.Rad); }
        /// <summary>
        /// Vrací příznak rovnosti
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return (this == (obj as Angle2D));
        }
        #endregion
        #region Implicitní konvertory
        /// <summary>
        /// Implicitní konverze
        /// </summary>
        /// <param name="angle2D"></param>
        public static implicit operator Double(Angle2D angle2D) { return angle2D.Rad; }
        /// <summary>
        /// Implicitní konverze
        /// </summary>
        /// <param name="angle"></param>
        public static implicit operator Angle2D(Double angle) { return new Angle2D(angle); }
        #endregion
    }
    #endregion
    #region class Vector2D
    /// <summary>
    /// Vektor v rovině (dva body = úhel a délka)
    /// </summary>
    public class Vector2D : BaseXD
    {
        #region Konstruktory a data
        /// <summary>
        /// Konstruktor pro čistý vektor = ten, jehož <see cref="OriginPoint"/> je <see cref="Point2D.Empty"/> = { 0, 0 }.
        /// Zadaná hodnota reprezentuje <see cref="TargetPoint"/>
        /// </summary>
        /// <param name="targetPoint"></param>
        public Vector2D(Point2D targetPoint)
            : base()
        {
            OriginPoint = Point2D.Empty;
            TargetPoint = targetPoint;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="originPoint"></param>
        /// <param name="targetPoint"></param>
        public Vector2D(Point2D originPoint, Point2D targetPoint)
            : base()
        {
            OriginPoint = originPoint;
            TargetPoint = targetPoint;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="originPoint"></param>
        /// <param name="angle"></param>
        /// <param name="length"></param>
        public Vector2D(Point2D originPoint, Angle2D angle, Double length)
            : base()
        {
            OriginPoint = originPoint;
            Angle = angle;
            Length = length;
        }
        /// <summary>
        /// Souřadnice bodu 1 = výchozí bod vektoru
        /// </summary>
        public Point2D OriginPoint { get { return _OriginPoint; } set { _OriginPoint = value; _Reset(false, true); } }
        private Point2D _OriginPoint;
        private UInt64? _OriginPointId;
        /// <summary>
        /// Souřadnice bodu 2 = cílový bod vektoru
        /// </summary>
        public Point2D TargetPoint { get { _CheckTarget(); return _TargetPoint; } set { _TargetPoint = value; _Reset(false, true); } }
        private Point2D _TargetPoint;
        private UInt64? _TargetPointId;
        /// <summary>
        /// Úhel 3D z bodu <see cref="OriginPoint"/> do bodu <see cref="TargetPoint"/>
        /// </summary>
        public Angle2D Angle { get { _CheckAngleLength(); return _Angle; } set { _Angle = value; _Reset(true, false); } }
        private Angle2D _Angle;
        private UInt64? _AngleId;
        /// <summary>
        /// Vzdálenost z bodu <see cref="OriginPoint"/> do bodu <see cref="TargetPoint"/>
        /// </summary>
        public Double Length { get { _CheckAngleLength(); return _Length.Value; } set { _Length = value; _Reset(true, false); } }
        private Double? _Length;
        private Double? _LengthId;
        /// <summary>
        /// Čistý vektor = rozdíl (<see cref="TargetPoint"/> - <see cref="OriginPoint"/>), vyjadřuje pouze směr a velikost vektoru. 
        /// Jeho počáteční bod = 0.
        /// Slouží k matematickým výpočtům.
        /// </summary>
        public Point2D Vector { get { return (this.TargetPoint - this.OriginPoint); } }
        /// <summary>
        /// Obsahuje true, pokud this vektor je nulový : jeho počátek == konec, jeho délka == 0
        /// </summary>
        public bool IsZero { get { return ((this._TargetPoint.IsNotNull()) ? (this.OriginPoint == this.TargetPoint) : (this._Length.HasValue ? (this._Length.Value == 0d) : true)); } }
        #endregion
        #region Privátní přepočty mezi hodnotami TargetPoint <=> Angle+Length
        /// <summary>
        /// Resetuje Target a/nebo Angle+Length, vždy interní ID, plus zachová ID platných dat.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="angle"></param>
        private void _Reset(bool target, bool angle)
        {
            if (target)
            {
                _TargetPoint = null;
            }
            if (angle)
            {
                _Angle = null;
                _Length = null;
            }
            _SaveId();
            this.ResetId();
        }
        /// <summary>
        /// Zajistí, že cílový bod bude platný
        /// </summary>
        private void _CheckTarget()
        {
            if (_TargetPoint == null || IsChanged(_OriginPoint, _OriginPointId) || IsChanged(_Angle, _AngleId) || IsChanged(_Length, _LengthId))
            {
                Math3D.CalculateTarget(_OriginPoint, _Angle, _Length.Value, out _TargetPoint);
                _SaveId();
            }
        }
        /// <summary>
        /// Zajistí, že úhel a vzdálenost bude platný
        /// </summary>
        private void _CheckAngleLength()
        {
            if (_Angle == null || !_Length.HasValue || IsChanged(_OriginPoint, _OriginPointId) || IsChanged(_TargetPoint, _TargetPointId))
            {
                double length;
                Math3D.CalculateAngleLength(_OriginPoint, _TargetPoint, out _Angle, out length);
                _Length = length;
                _SaveId();
            }
        }
        /// <summary>
        /// Uloží si ID všech hodnot po jejich vypočítání
        /// </summary>
        private void _SaveId()
        {
            _OriginPointId = _OriginPoint?.Id;
            _TargetPointId = _TargetPoint?.Id;
            _AngleId = _Angle?.Id;
            _LengthId = _Length;
        }
        #endregion
        #region Matematické vyjádření přímky, nalezení bodu na vektoru, určení kolmé roviny, součiny skalární a vektorový
        /// <summary>
        /// Matice vektoru pro výpočty.
        /// Řádky obsahují dimenze: [0,] = dX; [1,] = dY;
        /// Sloupce obsahují koeficienty: [,0] = d?0; [,1] = d?1;
        /// Souřadnice bodu na vektoru je pak dána výpočtem Pt = { X = dX0 + t * dX1; Y = dY0 + t * dY1; },
        /// pro t = { -nekonečno až +nekonečno } pro přímku, nebo { 0 až 1 } pro vektor v rozmezí Origin až Target.
        /// </summary>
        public double[,] Matrix
        {
            get
            {
                Point2D p0 = this.OriginPoint;
                Point2D p1 = this.Vector;
                double[,] matrix = new double[2, 2]
                {
                    {  p0.X, p1.X },
                    {  p0.Y, p1.Y }
                };
                return matrix;
            }
        }
        /// <summary>
        /// Metoda vrátí skalární součin dvou vektorů (dot product), 
        /// dle vzorce: r = (a.Vector.X * b.Vector.X) +  (a.Vector.Y * b.Vector.Y).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Double ScalarProduct(Vector2D a, Vector2D b)
        {
            return Point2D.ScalarProduct(a.Vector, b.Vector);
        }

        /// <summary>
        /// Vrátí bod na this vektoru, který je vzdálen (<paramref name="distance"/>) od bodu <see cref="OriginPoint"/>
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public Point2D GetPointAtDistance(double distance)
        {
            return this.OriginPoint + (this.Angle.Point * distance);
        }
        /// <summary>
        /// Vrátí bod na this vektoru, který se nachází na relativní pozici (<paramref name="t"/>);
        /// relativně k bodu <see cref="OriginPoint"/> (pro t = 0) až <see cref="TargetPoint"/> (pro t = 1).
        /// Hodnota t smí být libovolná, tj. i mimo rozsaj 0 ÷ 1.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Point2D GetPointMatrix(double t)
        {
            var m = Matrix;
            return new Point2D(m[0, 0] + t * m[0, 1], m[1, 0] + t * m[1, 1]);
        }
        /// <summary>
        /// Vrátí vektor kolmý na this vektor, který jej protíná v bodě P
        /// (který leží na this vektoru ve vzdálenosti (t) mezi <see cref="OriginPoint"/> a <see cref="TargetPoint"/>).
        /// Vektor reprezentuje kolmici ve směru <paramref name="rotate"/> (proti směru / ve směru hodinových ručiček).
        /// Může být specifikována i rotace <see cref="Rotate2D.Opposite"/> i <see cref="Rotate2D.None"/>, 
        /// pak ale výsledek nepředstavuje kolmici ale o protilehlý vektor nebo o týž vektor.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="rotate">Směr kolmice, lze zadat kteroukoli ze čtyř možností</param>
        /// <returns></returns>
        public Vector2D GetVectorPerpendicular(double t, Rotate2D rotate)
        {
            var point = GetPointMatrix(t);
            var vector = Vector;
            switch (rotate)
            {
                case Rotate2D.None:
                    return new Vector2D(point, new Point2D(point.X + vector.X, point.Y + vector.Y));
                case Rotate2D.CCW:
                    return new Vector2D(point, new Point2D(point.X - vector.Y, point.Y + vector.X));
                case Rotate2D.Opposite:
                    return new Vector2D(point, new Point2D(point.X - vector.X, point.Y - vector.Y));
                case Rotate2D.CW:
                    return new Vector2D(point, new Point2D(point.X + vector.Y, point.Y - vector.X));
            }
            return null;
        }


        #endregion
        #region Sčítání, odčítání, porovnání
        /// <summary>
        /// Operátor : Point2D = Point2D + Vector2D; 
        /// kde z vektoru je akceptována pouze jeho čistá část <see cref="Vector2D.Vector"/>,
        /// ignoruje se jeho počáteční bod <see cref="Vector2D.OriginPoint"/>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Point2D operator +(Point2D a, Vector2D b) { var v = b.Vector; return new Point2D(a.X + v.X, a.Y + v.Y); }
        /// <summary>
        /// Operátor : Point2D = Point2D - Vector2D; 
        /// kde z vektoru je akceptována pouze jeho čistá část <see cref="Vector2D.Vector"/>,
        /// ignoruje se jeho počáteční bod <see cref="Vector2D.OriginPoint"/>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Point2D operator -(Point2D a, Vector2D b) { var v = b.Vector; return new Point2D(a.X - v.X, a.Y - v.Y); }

        /// <summary>
        /// Operátor "je rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Vector2D a, Vector2D b) { return IsEqual(a, b, () => IsEqualValues(a, b)); }
        /// <summary>
        /// Operátor "není rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(Vector2D a, Vector2D b) { return !IsEqual(a, b, () => IsEqualValues(a, b)); }
        /// <summary>
        /// Metoda vrátí true, pokud dané dvě instance obsahují shodná data.
        /// Obě instance musí být not null, jinak dojde k chybě.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected static bool IsEqualValues(Vector2D a, Vector2D b) { return (a.OriginPoint == b.OriginPoint && a.TargetPoint == b.TargetPoint); }
        /// <summary>
        /// Vrátí hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() { return CalculateHashCode(this.OriginPoint.GetHashCode(), this.TargetPoint.GetHashCode()); }
        /// <summary>
        /// Vrací příznak rovnosti
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return (this == (obj as Vector2D));
        }
        #endregion

    }
    #endregion
    #region class BaseXD, enumy
    /// <summary>
    /// Bázová třída s ID (Timestamp)
    /// </summary>
    public class BaseXD
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public BaseXD()
        {
            _Id = null;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.Text; }
        /// <summary>
        /// Textové vyjádření obsahu prvku
        /// </summary>
        public virtual string Text
        {
            get { return this.GetType().Name; }
        }
        /// <summary>
        /// Resetuje svoje ID. Až bude potřeba, přidělí se nové. Má se volat při každé změně hodnoty v prvku.
        /// </summary>
        protected void ResetId()
        {
            this._Id = null;
        }
        /// <summary>
        /// Po změně dat v instanci
        /// </summary>
        protected void NewId()
        {
            this._Id = ++_LastId;
        }
        /// <summary>
        /// ID prvku. 
        /// Při každé změně dat prvku jeho obsahu je změněno.
        /// <para/>
        /// ID prvku je při změnách fakticky resetováno (na null), a až při prvním čtení hodnoty po změně je přiděleno nové ID.
        /// </summary>
        public UInt64 Id { get { if (!_Id.HasValue) NewId(); return _Id.Value; } }
        private UInt64? _Id = null;
        /// <summary>
        /// Vrátí true, pokud hodnota v daném prvku byla změněna od doby, kdy bylo získáno jeho ID
        /// </summary>
        /// <param name="item"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsChanged(BaseXD item, UInt64? id)
        {
            bool v1 = !(item == null);
            bool v2 = id.HasValue;
            if (v1 != v2) return true;           // Pokud item je null a ID má hodnotu, anebo naopak, pak je tu změna
            if (!v1) return false;               // Pokud item je null (a ID tedy samozřejmě nemá hodnotu, protože v1 == v2), pak to není změna (stále NULL)
            return (item.Id != id);              // Protože item není null, a ID má má tedy také hodnotu, pak změna je když se liší ID
        }
        /// <summary>
        /// Vrátí true, pokud hodnota se liší od hodnoty předešlé
        /// </summary>
        /// <param name="newValue"></param>
        /// <param name="oldValue"></param>
        /// <returns></returns>
        public static bool IsChanged(Double? newValue, Double? oldValue)
        {
            bool v1 = newValue.HasValue;
            bool v2 = oldValue.HasValue;
            if (v1 != v2) return true;           // Pokud newValue je null a oldValue má hodnotu, anebo naopak, pak je tu změna
            if (!v1) return false;               // Pokud newValue je null (a oldValue tedy samozřejmě nemá hodnotu, protože v1 == v2), pak to není změna (stále NULL)
            return (newValue.Value != oldValue.Value);     // Protože newValue není null, a oldValue má tedy také hodnotu, pak změna je když se liší hodnota
        }
        private static UInt64 _LastId = 0L;
        /// <summary>
        /// Metoda vrátí true, pokud oba objekty jsou si rovny.
        /// Jsou si rovny, pokud jsou oba null, anebo oba nejsou null a jsou shodného typu a dodaná funkce vrátí true.
        /// Nejsou si rovny, když jen jeden je null a druhý není, nebo jsou různého typu, anebo dodaná funkce vrátí false.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="equality"></param>
        /// <returns></returns>
        protected static bool IsEqual(BaseXD a, BaseXD b, Func<bool> equality)
        {
            bool an = a.IsNull();
            bool bn = b.IsNull();
            if (an && bn) return true;
            if (an || bn) return false;
            if (a.GetType() != b.GetType()) return false;
            return equality();
        }
        /// <summary>
        /// Vrátí hashcode
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        protected static int CalculateHashCode(params Double[] values)
        {
            int result = 0;
            foreach (Double value in values)
                result = result ^ value.GetHashCode();
            return result;
        }
        /// <summary>
        /// Vrátí hashcode
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        protected static int CalculateHashCode(params int[] values)
        {
            int result = 0;
            foreach (int value in values)
                result = result ^ value;
            return result;
        }
    }
    /// <summary>
    /// Rotace ve standardních 2D souřadnicích, kde +X = vpravo, +Y = nahoru, -X = vlevo, -Y = dolů
    /// </summary>
    public enum Rotate2D
    {
        /// <summary>
        /// Bez rotace
        /// </summary>
        None = 0,
        /// <summary>
        /// Counterclockwise = proti směru hodinových ručiček
        /// </summary>
        CCW,
        /// <summary>
        /// Protilehlá strana = o 180°
        /// </summary>
        Opposite,
        /// <summary>
        /// Clockwise = po směru hodinových ručiček
        /// </summary>
        CW
    }
    #endregion
    #region class Math3D - implementace prostorové matematiky
    /// <summary>
    /// Math3D - implementace prostorové matematiky
    /// </summary>
    public static class Math3D
    {
        /// <summary>
        /// Vratí druhou odmocninu ze součtu čtverců dodaných hodnot, tedy přeponu nad odvěsnami.
        /// Pro 2D výpočet budou 2 parametry, pro 3D výpočet budou 3 parametry.
        /// </summary>
        /// <param name="lenths"></param>
        /// <returns></returns>
        public static double GetHypotenuse(params Double[] lenths)
        {
            return Math.Sqrt(lenths.Sum(l => (l * l)));
        }
        /// <summary>
        /// Vrátí úhel 2D
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        public static Angle2D GetAngle(double dx, double dy)
        {
            return new Angle2D(dx, dy);
        }
        /// <summary>
        /// Vrátí úhel 3D
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="dz"></param>
        /// <returns></returns>
        public static Angle3D GetAngle(double dx, double dy, double dz)
        {
            return new Angle3D(dx, dy, dz);
        }
        /// <summary>
        /// Určí cílový bod při zadání výchozího bodu, úhlu a vzdálenosti
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="angle"></param>
        /// <param name="length"></param>
        /// <param name="target"></param>
        public static void CalculateTarget(Point3D origin, Angle3D angle, Double length, out Point3D target)
        {
            Point3D point = angle.Point * length;
            target = origin + point;
        }
        /// <summary>
        /// Určí úhel a vzdálenost mezi dvěma danými body
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        /// <param name="angle"></param>
        /// <param name="length"></param>
        public static void CalculateAngleLength(Point3D origin, Point3D target, out Angle3D angle, out Double length)
        {
            Point3D delta = target - origin;
            angle = delta.Angle;
            length = delta.HypXYZ;
        }
        /// <summary>
        /// Určí cílový bod při zadání výchozího bodu, úhlu a vzdálenosti
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="angle"></param>
        /// <param name="length"></param>
        /// <param name="target"></param>
        public static void CalculateTarget(Point2D origin, Angle2D angle, Double length, out Point2D target)
        {
            Point2D point = angle.Point * length;
            target = origin + point;
        }
        /// <summary>
        /// Určí úhel a vzdálenost mezi dvěma danými body
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        /// <param name="angle"></param>
        /// <param name="length"></param>
        public static void CalculateAngleLength(Point2D origin, Point2D target, out Angle2D angle, out Double length)
        {
            Point2D delta = target - origin;
            angle = delta.Angle;
            length = delta.HypXY;
        }
        #region Extensions
        /// <summary>
        /// Vrací true, pokud this instance je null.
        /// Jedná se o Extension metodu, proto můžeme i na null instanci volat její metodu.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool IsNull(this object o) { return (o == null); }
        /// <summary>
        /// Vrací true, pokud this instance není null.
        /// Jedná se o Extension metodu, proto můžeme i na null instanci volat její metodu.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool IsNotNull(this object o) { return (o != null); }
        #endregion
    }
    #endregion
}
