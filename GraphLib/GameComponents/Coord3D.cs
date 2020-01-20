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
    public class Point3D : Base3D
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
        public Angle3D Angle3D { get { return new Angle3D(_X, _Y, _Z); } }
        #endregion
        #region Sčítání, odčítání
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
        #endregion
    }
    #endregion
    #region class Point2D
    /// <summary>
    /// Souřadnice 2D bodu
    /// </summary>
    public class Point2D : Base3D
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
        #region Sčítání, odčítání
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
    #region class Polygon3D
    /// <summary>
    /// Víceúhelník
    /// </summary>
    public class Polygon3D : Base3D, IEnumerable<Point3D>
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
    #region class Vector3D
    /// <summary>
    /// Vektor v prostoru (dva body = prostorový úhel a délka)
    /// </summary>
    public class Vector3D : Base3D
    {
        #region Konstruktory a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="originPoint"></param>
        /// <param name="targetPoint"></param>
        public Vector3D(Point3D originPoint, Point3D targetPoint)
            : base()
        {
            _OriginPoint = originPoint;
            _TargetPoint = targetPoint;
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
            _OriginPoint = originPoint;
            _Angle = angle;
            _Length = length;
        }
        /// <summary>
        /// Souřadnice bodu 1 = výchozí bod vektoru
        /// </summary>
        public Point3D OriginPoint { get { return _OriginPoint; } set { this.ResetId(); _OriginPoint = value; _ResetAngleLength(); } }
        private Point3D _OriginPoint;
        private UInt64? _OriginPointId;
        /// <summary>
        /// Souřadnice bodu 2 = cílový bod vektoru
        /// </summary>
        public Point3D TargetPoint { get { _CheckTarget(); return _TargetPoint; } set { this.ResetId(); _TargetPoint = value; _ResetAngleLength(); } }
        private Point3D _TargetPoint;
        private UInt64? _TargetPointId;
        /// <summary>
        /// Úhel 3D z bodu <see cref="OriginPoint"/> do bodu <see cref="TargetPoint"/>
        /// </summary>
        public Angle3D Angle { get { _CheckAngleLength(); return _Angle; } set { this.ResetId(); _Angle = value; _ResetTarget(); } }
        private Angle3D _Angle;
        private UInt64? _AngleId;
        /// <summary>
        /// Vzdálenost z bodu <see cref="OriginPoint"/> do bodu <see cref="TargetPoint"/>
        /// </summary>
        public Double Length { get { _CheckAngleLength(); return _Length.Value; } set { this.ResetId(); _Length = value; _ResetTarget(); } }
        private Double? _Length;
        private Double? _LengthId;
        #endregion
        #region Automatické přepočty this.Points <=> Vector
        /// <summary>
        /// Nuluje cílový bod
        /// </summary>
        private void _ResetTarget()
        {
            _TargetPoint = null;
        }
        /// <summary>
        /// Zajistí, že cílový bod bude platný
        /// </summary>
        private void _CheckTarget()
        {
            if (_TargetPoint is null || IsChanged(_OriginPoint, _OriginPointId) || IsChanged(_Angle, _AngleId) || IsChanged(_Length, _LengthId))
            {
                Math3D.CalculateTarget(_OriginPoint, _Angle, _Length.Value, out _TargetPoint);
                _SaveId();
            }
        }
        /// <summary>
        /// Nuluje úhel a vzdálenost
        /// </summary>
        private void _ResetAngleLength()
        {
            _Angle = null;
            _Length = null;
        }
        /// <summary>
        /// Zajistí, že úhel a vzdálenost bude platný
        /// </summary>
        private void _CheckAngleLength()
        {
            if (_Angle is null || !_Length.HasValue || IsChanged(_OriginPoint, _OriginPointId) || IsChanged(_TargetPoint, _TargetPointId))
            {
                Math3D.CalculateAngleLength(_OriginPoint, _TargetPoint, out _Angle, out double length);
                _Length = length;
                _SaveId();
            }
        }
        /// <summary>
        /// Uloží si ID všech hodnot po jejich vypočítání
        /// </summary>
        private void _SaveId()
        {
            _OriginPointId = _OriginPoint.Id;
            _TargetPointId = _TargetPoint.Id;
            _AngleId = _Angle.Id;
            _LengthId = _Length;
        }
        #endregion
    }
    #endregion
    #region class Angle3D
    /// <summary>
    /// Směr vektoru v prostoru
    /// </summary>
    public class Angle3D : Base3D
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
            this._AngleH = angleH;
            this._AngleV = angleV;
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
        /// úhel narůstá ve směru k ose Z k jejím kladným hodnotám = proti směru hodinových ručiček
        /// </summary>
        public Angle2D AngleH { get { return _AngleH; } set { this.ResetId(); _AngleH = value; } }
        private Angle2D _AngleH;
        /// <summary>
        /// Úhel vertikální, ve svislé rovině, kde hodnota 0 reprezentuje vektor v rovině X-Z ve směru úhlu <see cref="AngleV"/>, 
        /// kde kladná hodnota představuje vektor mířící "vzhůru" ve směru kladné osy Y;
        /// kde hodnota 0.5d * <see cref="Math.PI"/> (=90°) reprezentuje svislý vektor směřující nahoru = kladná osa Y;
        /// kde hodnota 1.0d * <see cref="Math.PI"/> (=180°) reprezentuje vodorovný vektor v opačném směru než míří <see cref="AngleV"/>;
        /// kde hodnota 1.5d * <see cref="Math.PI"/> (=270°) reprezentuje svislý vektor směřující dolů = záporná osa Y.
        /// </summary>
        public Angle2D AngleV { get { return _AngleV; } set { this.ResetId(); _AngleV = value; } }
        private Angle2D _AngleV;
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
        #region Sčítání, odčítání
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
        #endregion
    }
    #endregion
    #region class Angle2D
    /// <summary>
    /// Jeden úhel (radiány, stupně)
    /// </summary>
    public class Angle2D : Base3D
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
        private const double Pi = 3.1415926535897932384626433832795028841971d;
        /// <summary>
        /// Hodnota 2d * <see cref="Pi"/> = 6.2831853071795862.......d; (na 40 míst)
        /// </summary>
        private const double R = 2d * Pi;
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
            return 360d * (rad % R) / R;
        }
        /// <summary>
        /// Ze zadaného čísla ve stupních (0-360) vrátí obloukovou míru (rad)
        /// </summary>
        /// <param name="degree"></param>
        /// <returns></returns>
        public static double ConvertDegreeToRad(double degree)
        {
            return (degree % 360d) * R / 360d;
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
                + ((d1 != 0 || d2 != 0) ? ":" + d1.ToString("00") + "'": "")
                + ((d2 != 0) ? ":" + d2.ToString("00") + "''" : "");
        }
        #endregion
        #region Sčítání, odčítání
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
    #region class Line3D, Triangle3D, Rectangle3D
    /// <summary>
    /// Čára mezi dvěma body
    /// </summary>
    public class Line3D : Base3D
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
    public class Triangle3D : Base3D
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
    public class Rectangle3D : Base3D
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
    #region class Base3D
    /// <summary>
    /// Bázová třída s identifikátorem
    /// </summary>
    public class Base3D
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Base3D()
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
        public static bool IsChanged(Base3D item, UInt64? id)
        {
            bool v1 = !(item is null);
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
            angle = delta.Angle3D;
            length = delta.HypXYZ;
        }
    }
    #endregion
}
