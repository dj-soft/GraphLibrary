using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.GameComponents
{
    /// <summary>
    /// Souřadnice 3D bodu
    /// </summary>
    public struct Point3D
    {
        public Point3D(double x, double y, double z)
        {
            _X = x;
            _Y = y;
            _Z = z;
        }
        public override string ToString()
        {
            return $"X:{_X}; Y:{_Y}; Z:{_Z}";
        }
        public double X { get { return _X; } }
        private double _X;
        public double Y { get { return _Y; } }
        private double _Y;
        public double Z { get { return _Z; } }
        private double _Z;
    }
    public struct Line3D
    {
        public Line3D(Point3D point1, Point3D point2)
        {
            _Point1 = point1;
            _Point2 = point2;
        }
        public Point3D Point1 { get { return _Point1; } }
        private Point3D _Point1;
        public Point3D Point2 { get { return _Point2; } }
        private Point3D _Point2;
    }

    public struct Triangle3D
    {
        public Triangle3D(Point3D point1, Point3D point2, Point3D point3)
        {
            _Point1 = point1;
            _Point2 = point2;
            _Point3 = point3;
        }
        public Point3D Point1 { get { return _Point1; } }
        private Point3D _Point1;
        public Point3D Point2 { get { return _Point2; } }
        private Point3D _Point2;
        public Point3D Point3 { get { return _Point3; } }
        private Point3D _Point3;
    }
    public struct Rectangle3D
    {
        public Rectangle3D(Point3D point1, Point3D point2, Point3D point3, Point3D point4)
        {
            _Point1 = point1;
            _Point2 = point2;
            _Point3 = point3;
            _Point4 = point4;
        }
        public Point3D Point1 { get { return _Point1; } }
        private Point3D _Point1;
        public Point3D Point2 { get { return _Point2; } }
        private Point3D _Point2;
        public Point3D Point3 { get { return _Point3; } }
        private Point3D _Point3;
        public Point3D Point4 { get { return _Point4; } }
        private Point3D _Point4;
    }
}
