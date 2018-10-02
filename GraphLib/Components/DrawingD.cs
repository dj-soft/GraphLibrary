using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Drawing;

namespace Asol.Tools.WorkScheduler.Components
{
    #region struct PointD
    /// <summary>
    /// Represents an ordered pair of decimal x- and y-coordinates that defines a point in a two-dimensional plane.
    /// </summary>
    public struct PointD
    {
        #region Constructors, properties
        /// <summary>
        /// Initializes a new instance of the PointD class with the specified coordinates.
        /// </summary>
        /// <param name="x">The horizontal position of the point.</param>
        /// <param name="y">The vertical position of the point.</param>
        public PointD(decimal x, decimal y)
        {
            this._X = x;
            this._Y = y;
        }
        /// <summary>
        /// Initializes a new instance of the PointD class with the specified point.
        /// </summary>
        /// <param name="point">Point, from which new instance will be initialized</param>
        public PointD(Point point)
        {
            this._X = point.X;
            this._Y = point.Y;
        }
        /// <summary>
        /// Initializes a new instance of the PointD class with the specified point.
        /// </summary>
        /// <param name="point">Point, from which new instance will be initialized</param>
        public PointD(PointF point)
        {
            this._X = (decimal)point.X;
            this._Y = (decimal)point.Y;
        }
        /// <summary>
        /// Initializes a new instance of the PointD class with the specified point.
        /// </summary>
        /// <param name="point">Point, from which new instance will be initialized</param>
        public PointD(PointD point)
        {
            this._X = point.X;
            this._Y = point.Y;
        }
        /// <summary>
        /// Converts this PointD to a human readable string.
        /// </summary>
        /// <returns>A string that represents this PointD.</returns>
        public override string ToString()
        {
            return "X=" + this.X.ToString() + "; Y=" + this.Y.ToString();
        }
        /// <summary>
        /// Gets or sets the x-coordinate of this PointD.
        /// </summary>
        public decimal X { get { return this._X; } set { this._X = value; } }
        /// <summary>
        /// Gets or sets the y-coordinate of this PointD.
        /// </summary>
        public decimal Y { get { return this._Y; } set { this._Y = value; } }
        private decimal _X;
        private decimal _Y;
        /// <summary>
        /// Gets a value indicating whether this PointD is empty.
        /// true if both PointD.X and PointD.Y are 0; otherwise, false.
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty { get { return (this.X == 0m && this.Y == 0m); } }
        /// <summary>
        /// Represents a new instance of the PointD class with member data left uninitialized.
        /// </summary>
        public static readonly PointD Empty = new PointD(0m, 0m);
        #endregion
        #region Methods
        /// <summary>
        /// Specifies whether this PointD contains the same coordinates as the specified System.Object.
        /// </summary>
        /// <param name="obj">The System.Object to test.</param>
        /// <returns>This method returns true if obj is a PointD and has the same coordinates as this PointD.</returns>
        public override bool Equals(object obj)
        {
            if (obj is PointD)
            {
                PointD other = (PointD)obj;
                return (this == other);
            }
            return false;
        }
        /// <summary>
        /// Returns a hash code for this PointD structure.
        /// </summary>
        /// <returns>An integer value that specifies a hash value for this PointD structure.</returns>
        public override int GetHashCode()
        {
            return this.X.GetHashCode() ^ this.Y.GetHashCode();
        }
        /// <summary>
        /// Translates a given PointD by the specified System.Drawing.Size.
        /// </summary>
        /// <param name="pt">The PointD to translate.</param>
        /// <param name="sz">The System.Drawing.Size that specifies the numbers to add to the coordinates of pt.</param>
        /// <returns>The translated PointD.</returns>
        public static PointD Add(PointD pt, Size sz)
        { return new PointD(pt.X + (decimal)sz.Width, pt.Y + (decimal)sz.Height); }
        /// <summary>
        /// Translates a given PointD by the specified System.Drawing.SizeF.
        /// </summary>
        /// <param name="pt">The PointD to translate.</param>
        /// <param name="sz">The System.Drawing.SizeF that specifies the numbers to add to the coordinates of pt.</param>
        /// <returns>The translated PointD.</returns>
        public static PointD Add(PointD pt, SizeF sz)
        { return new PointD(pt.X + (decimal)sz.Width, pt.Y + (decimal)sz.Height); }
        /// <summary>
        /// Translates a given PointD by the specified SizeD.
        /// </summary>
        /// <param name="pt">The PointD to translate.</param>
        /// <param name="sz">The SizeD that specifies the numbers to add to the coordinates of pt.</param>
        /// <returns>The translated PointD.</returns>
        public static PointD Add(PointD pt, SizeD sz)
        { return new PointD(pt.X + sz.Width, pt.Y + sz.Height); }
        /// <summary>
        /// Translates a PointD by the negative of a specified size.
        /// </summary>
        /// <param name="pt">The PointD to translate.</param>
        /// <param name="sz">The System.Drawing.Size that specifies the numbers to subtract from the coordinates of pt.</param>
        /// <returns>The translated PointD.</returns>
        public static PointD Subtract(PointD pt, Size sz)
        { return new PointD(pt.X - (decimal)sz.Width, pt.Y - (decimal)sz.Height); }
        /// <summary>
        /// Translates a PointD by the negative of a specified size.
        /// </summary>
        /// <param name="pt">The PointD to translate.</param>
        /// <param name="sz">The System.Drawing.SizeF that specifies the numbers to subtract from the coordinates of pt.</param>
        /// <returns>The translated PointD.</returns>
        public static PointD Subtract(PointD pt, SizeF sz)
        { return new PointD(pt.X - (decimal)sz.Width, pt.Y - (decimal)sz.Height); }
        /// <summary>
        /// Translates a PointD by the negative of a specified size.
        /// </summary>
        /// <param name="pt">The PointD to translate.</param>
        /// <param name="sz">The SizeD that specifies the numbers to subtract from the coordinates of pt.</param>
        /// <returns>The translated PointD.</returns>
        public static PointD Subtract(PointD pt, SizeD sz)
        { return new PointD(pt.X - sz.Width, pt.Y - sz.Height); }
        #endregion
        #region Operators
        /// <summary>
        /// Compares two PointD structures. The result specifies whether the values of the PointD.X and PointD.Y properties of the two PointD structures are equal.
        /// </summary>
        /// <param name="left">A PointD to compare.</param>
        /// <param name="right">A PointD to compare.</param>
        /// <returns>true if the PointD.X and PointD.Y values of the left and right PointD structures are equal; otherwise, false.</returns>
        public static bool operator ==(PointD left, PointD right)
        { return (left.X == right.X && left.Y == right.Y); }
        /// <summary>
        /// Determines whether the coordinates of the specified points are not equal.
        /// </summary>
        /// <param name="left">A PointD to compare.</param>
        /// <param name="right">A PointD to compare.</param>
        /// <returns>true to indicate the PointD.X and PointD.Y values of left and right are not equal; otherwise, false.</returns>
        public static bool operator !=(PointD left, PointD right)
        { return (left.X != right.X || left.Y != right.Y); }
        /// <summary>
        /// Return a new PointD as A + B (A.X + B.X, A.Y + B.Y)
        /// </summary>
        /// <param name="pa">A PointD</param>
        /// <param name="pb">B PointD</param>
        /// <returns></returns>
        public static PointD operator +(PointD pa, PointD pb)
        { return new PointD(pa.X + pb.X, pa.Y + pb.Y); }
        /// <summary>
        /// Translates a PointD by the of a given System.Drawing.Size.
        /// </summary>
        /// <param name="pt">A PointD</param>
        /// <param name="sz">A Size to add</param>
        /// <returns></returns>
        public static PointD operator +(PointD pt, Size sz)
        { return new PointD(pt.X + (decimal)sz.Width, pt.Y + (decimal)sz.Height); }
        /// <summary>
        /// Translates a PointD by the of a given System.Drawing.SizeF.
        /// </summary>
        /// <param name="pt">A PointD</param>
        /// <param name="sz">A Size to add</param>
        /// <returns></returns>
        public static PointD operator +(PointD pt, SizeF sz)
        { return new PointD(pt.X + (decimal)sz.Width, pt.Y + (decimal)sz.Height); }
        /// <summary>
        /// Translates a PointD by the of a given SizeD.
        /// </summary>
        /// <param name="pt">A PointD</param>
        /// <param name="sz">A Size to add</param>
        /// <returns></returns>
        public static PointD operator +(PointD pt, SizeD sz)
        { return new PointD(pt.X + sz.Width, pt.Y + sz.Height); }
        /// <summary>
        /// Translates a PointD by the of a given Vector.
        /// </summary>
        /// <param name="pt">A PointD</param>
        /// <param name="v">A Vector to add</param>
        /// <returns></returns>
        public static PointD operator +(PointD pt, Vector v)
        { PointD pb = v.TargetD; return new PointD(pt.X + pb.X, pt.Y + pb.Y); }
        /// <summary>
        /// Return a new PointD as A - B (A.X - B.X, A.Y - B.Y)
        /// </summary>
        /// <param name="pa">A PointD</param>
        /// <param name="pb">B PointD</param>
        /// <returns></returns>
        public static PointD operator -(PointD pa, PointD pb)
        { return new PointD(pa.X - pb.X, pa.Y - pb.Y); }
        /// <summary>
        /// Translates a PointD by the negative of a given System.Drawing.Size.
        /// </summary>
        /// <param name="pt">A PointD</param>
        /// <param name="sz">A Size to subtract</param>
        /// <returns></returns>
        public static PointD operator -(PointD pt, Size sz)
        { return new PointD(pt.X - (decimal)sz.Width, pt.Y - (decimal)sz.Height); }
        /// <summary>
        /// Translates a PointD by the negative of a given System.Drawing.SizeF.
        /// </summary>
        /// <param name="pt">A PointD</param>
        /// <param name="sz">A Size to subtract</param>
        /// <returns></returns>
        public static PointD operator -(PointD pt, SizeF sz)
        { return new PointD(pt.X - (decimal)sz.Width, pt.Y - (decimal)sz.Height); }
        /// <summary>
        /// Translates a PointD by the negative of a given SizeD.
        /// </summary>
        /// <param name="pt">A PointD</param>
        /// <param name="sz">A Size to subtract</param>
        /// <returns></returns>
        public static PointD operator -(PointD pt, SizeD sz)
        { return new PointD(pt.X - sz.Width, pt.Y - sz.Height); }
        /// <summary>
        /// Translates a PointD by the of a given Vector.
        /// </summary>
        /// <param name="pt">A PointD</param>
        /// <param name="v">A Vector to sub</param>
        /// <returns></returns>
        public static PointD operator -(PointD pt, Vector v)
        { PointD pb = v.TargetD; return new PointD(pt.X - pb.X, pt.Y - pb.Y); }
        #endregion
        #region Implicit & explicit convertors
        /// <summary>
        /// Converts the specified Point structure to a PointD structure.
        /// </summary>
        /// <param name="point">The Point structure to be converted</param>
        /// <returns>The PointD structure to which this operator converts.</returns>
        public static explicit operator PointD(Point point)
        { return new PointD(point.X, point.Y); }
        /// <summary>
        /// Converts the specified PointF structure to a PointD structure.
        /// </summary>
        /// <param name="point">The PointF structure to be converted</param>
        /// <returns>The PointD structure to which this operator converts.</returns>
        public static explicit operator PointD(PointF point)
        { return new PointD((decimal)point.X, (decimal)point.Y); }
        /// <summary>
        /// Converts the specified PointD structure to a (Rounded)Point structure.
        /// </summary>
        /// <param name="point">The PointD structure to be converted</param>
        /// <returns>The Point structure to which this operator converts.</returns>
        public static explicit operator Point(PointD point)
        { return new Point((int)Math.Round(point.X, 0), (int)Math.Round(point.Y, 0)); }
        /// <summary>
        /// Converts the specified PointD structure to a PointF structure.
        /// </summary>
        /// <param name="point">The PointD structure to be converted</param>
        /// <returns>The PointF structure to which this operator converts.</returns>
        public static explicit operator PointF(PointD point)
        { return new PointF((float)point.X, (float)point.Y); }
        /// <summary>
        /// Converts the specified SizeD structure to a PointD structure.
        /// </summary>
        /// <param name="size">The SizeD structure to be converted</param>
        /// <returns>The PointD structure to which this operator converts.</returns>
        public static explicit operator PointD(SizeD size)
        { return new PointD(size.Width, size.Height); }
        #endregion
    }
    #endregion
    #region struct SizeD
    /// <summary>
    /// Stores an ordered pair of decimal numbers, typically the width and height of a rectangle.
    /// </summary>
    public struct SizeD
    {
        #region Constructors, properties
        /// <summary>
        /// Initializes a new instance of the SizeD structure from the specified dimensions.
        /// </summary>
        /// <param name="width">The width component of the new SizeD structure.</param>
        /// <param name="height">The height component of the new SizeD structure.</param>
        public SizeD(decimal width, decimal height)
        {
            this._Width = width;
            this._Height = height;
        }
        /// <summary>
        /// Initializes a new instance of the SizeD structure from the specified PointD structure.
        /// </summary>
        /// <param name="pt">The PointD structure from which to initialize this SizeD structure.</param>
        public SizeD(PointD pt)
        {
            this._Width = pt.X;
            this._Height = pt.Y;
        }
        /// <summary>
        /// Initializes a new instance of the SizeD structure from the specified existing SizeD structure.
        /// </summary>
        /// <param name="size">The SizeD structure from which to create the new SizeD structure.</param>
        public SizeD(SizeD size)
        {
            this._Width = size.Width;
            this._Height = size.Height;
        }
        /// <summary>
        /// Gets or sets the horizontal component of this SizeD structure.
        /// </summary>
        public decimal Width { get { return this._Width; } set { this._Width = value; } }
        /// <summary>
        /// Gets or sets the vertical component of this SizeD structure.
        /// </summary>
        public decimal Height { get { return this._Height; } set { this._Height = value; } }
        private decimal _Width;
        private decimal _Height;
        /// <summary>
        /// Gets a value that indicates whether this SizeD structure has zero width and height.
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty { get { return (this.Width == 0m && this.Height == 0m); } }
        /// <summary>
        /// Gets a SizeD structure that has a SizeD.Height and SizeD.Width value of 0.
        /// </summary>
        public static readonly SizeD Empty = new SizeD(0m, 0m);
        /// <summary>
        /// Creates a human-readable string that represents this SizeD structure.
        /// </summary>
        /// <returns>A string that represents this SizeD structure.</returns>
        public override string ToString()
        {
            return "Width=" + this.Width.ToString() + "; Height=" + this.Height.ToString();
        }
        #endregion
        #region Methods
        /// <summary>
        /// Tests to see whether the specified object is a SizeD structure with the same dimensions as this SizeD structure.
        /// </summary>
        /// <param name="obj">The System.Object to test.</param>
        /// <returns>This method returns true if obj is a SizeD and has the same width and height as this SizeD; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is SizeD)
            {
                SizeD other = (SizeD)obj;
                return (this == other);
            }
            return false;
        }
        /// <summary>
        /// Returns a hash code for this SizeD structure.
        /// </summary>
        /// <returns>An integer value that specifies a hash value for this SizeD structure.</returns>
        public override int GetHashCode()
        {
            return this.Width.GetHashCode() ^ this.Height.GetHashCode();
        }
        /// <summary>
        /// Adds the width and height of one SizeD structure to the width and height of another SizeD structure.
        /// </summary>
        /// <param name="sz1">The first SizeD structure to add.</param>
        /// <param name="sz2">The second SizeD structure to add.</param>
        /// <returns>A SizeD structure that is the result of the addition operation.</returns>
        public static SizeD Add(SizeD sz1, SizeD sz2)
        { return sz1 + sz2; }
        /// <summary>
        /// Subtracts the width and height of one SizeD structure from the width and height of another SizeD structure.
        /// </summary>
        /// <param name="sz1">The SizeD structure on the left side of the subtraction operator.</param>
        /// <param name="sz2">The SizeD structure on the right side of the subtraction operator.</param>
        /// <returns>A SizeD structure that is a result of the subtraction operation.</returns>
        public static SizeD Subtract(SizeD sz1, SizeD sz2)
        { return sz1 - sz2; }
        #endregion
        #region Operators
        /// <summary>
        /// Tests whether two SizeD structures are equal.
        /// </summary>
        /// <param name="sz1">The SizeD structure on the left side of the equality operator.</param>
        /// <param name="sz2">The SizeD structure on the right of the equality operator.</param>
        /// <returns>This operator returns true if sz1 and sz2 have equal width and height; otherwise, false.</returns>
        public static bool operator ==(SizeD sz1, SizeD sz2)
        { return (sz1.Width == sz2.Width && sz1.Height == sz2.Height); }
        /// <summary>
        /// Tests whether two SizeD structures are different.
        /// </summary>
        /// <param name="sz1">The SizeD structure on the left side of the equality operator.</param>
        /// <param name="sz2">The SizeD structure on the right of the equality operator.</param>
        /// <returns>This operator returns true if sz1 and sz2 differ either in width or height; false if sz1 and sz2 are equal.</returns>
        public static bool operator !=(SizeD sz1, SizeD sz2)
        { return (sz1.Width != sz2.Width || sz1.Height != sz2.Height); }
        /// <summary>
        /// Adds the width and height of one SizeD structure to the width and height of another SizeD structure.
        /// </summary>
        /// <param name="sz1">The first SizeD structure to add.</param>
        /// <param name="sz2">The second SizeD structure to add.</param>
        /// <returns>A SizeD structure that is the result of the addition operation.</returns>
        public static SizeD operator +(SizeD sz1, SizeD sz2)
        { return new SizeD(sz1.Width + sz2.Width, sz1.Height + sz2.Height); }
        /// <summary>
        /// Subtracts the width and height of one SizeD structure from the width and height of another SizeD structure.
        /// </summary>
        /// <param name="sz1">The SizeD structure on the left side of the subtraction operator.</param>
        /// <param name="sz2">The SizeD structure on the right side of the subtraction operator.</param>
        /// <returns>A SizeD that is the result of the subtraction operation.</returns>
        public static SizeD operator -(SizeD sz1, SizeD sz2)
        { return new SizeD(sz1.Width - sz2.Width, sz1.Height - sz2.Height); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeD structure to multiply.</param>
        /// <param name="zoom">Coefficient.</param>
        /// <returns>A SizeD structure that is the result of the multiply operation.</returns>
        public static SizeD operator *(SizeD size, decimal zoom)
        { return new SizeD(size.Width * zoom, size.Height * zoom); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeD structure to multiply.</param>
        /// <param name="zoom">Coefficient.</param>
        /// <returns>A SizeD structure that is the result of the multiply operation.</returns>
        public static SizeD operator *(SizeD size, float zoom)
        { return new SizeD(size.Width * (decimal)zoom, size.Height * (decimal)zoom); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeD structure to multiply.</param>
        /// <param name="zoom">Coefficient.</param>
        /// <returns>A SizeD structure that is the result of the multiply operation.</returns>
        public static SizeD operator *(SizeD size, double zoom)
        { return new SizeD(size.Width * (decimal)zoom, size.Height * (decimal)zoom); }
        /// <summary>
        /// Změnší danou velikost daným poměrem.
        /// </summary>
        /// <param name="size">The SizeD structure to divide.</param>
        /// <param name="ratio">Ratio.</param>
        /// <returns>A SizeD structure that is the result of the divide operation.</returns>
        public static SizeD operator /(SizeD size, decimal ratio)
        { return new SizeD(size.Width / ratio, size.Height / ratio); }
        /// <summary>
        /// Změnší danou velikost daným poměrem.
        /// </summary>
        /// <param name="size">The SizeD structure to divide.</param>
        /// <param name="ratio">Ratio.</param>
        /// <returns>A SizeD structure that is the result of the divide operation.</returns>
        public static SizeD operator /(SizeD size, float ratio)
        { return new SizeD(size.Width / (decimal)ratio, size.Height / (decimal)ratio); }
        /// <summary>
        /// Změnší danou velikost daným poměrem.
        /// </summary>
        /// <param name="size">The SizeD structure to divide.</param>
        /// <param name="ratio">Ratio.</param>
        /// <returns>A SizeD structure that is the result of the divide operation.</returns>
        public static SizeD operator /(SizeD size, double ratio)
        { return new SizeD(size.Width / (decimal)ratio, size.Height / (decimal)ratio); }
        #endregion
        #region Explicit type convertors
        /// <summary>
        /// Converts the specified Size structure to a SizeD structure.
        /// </summary>
        /// <param name="size">The Size structure to be converted</param>
        /// <returns>The SizeD structure to which this operator converts.</returns>
        public static explicit operator SizeD(Size size)
        { return new SizeD(size.Width, size.Height); }
        /// <summary>
        /// Converts the specified SizeF structure to a SizeD structure.
        /// </summary>
        /// <param name="size">The SizeF structure to be converted</param>
        /// <returns>The SizeD structure to which this operator converts.</returns>
        public static explicit operator SizeD(SizeF size)
        { return new SizeD((decimal)size.Width, (decimal)size.Height); }
        /// <summary>
        /// Converts the specified SizeD structure to a PointD structure.
        /// </summary>
        /// <param name="size">The SizeD structure to be converted</param>
        /// <returns>The PointD structure to which this operator converts.</returns>
        public static explicit operator PointD(SizeD size)
        { return new PointD(size.Width, size.Height); }
        /// <summary>
        /// Converts the specified SizeD structure to a SizeF structure.
        /// </summary>
        /// <param name="size">The SizeF structure to be converted</param>
        /// <returns>The SizeD structure to which this operator converts.</returns>
        public static explicit operator SizeF(SizeD size)
        { return new SizeF((float)size.Width, (float)size.Height); }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public SizeF ToSizeF()
        {
            return new SizeF((float)this.Width, (float)this.Height);
        }
        #endregion
    }
    #endregion
    #region struct RectangleD
    /// <summary>
    /// Represents an coordinates of area (with decimal numbers).
    /// </summary>
    public struct RectangleD
    {
        #region Constructors, properties
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="source"></param>
        public RectangleD(RectangleD source)
        {
            this._X = source._X;
            this._Y = source._Y;
            this._Width = source._Width;
            this._Height = source._Height;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="source"></param>
        public RectangleD(RectangleF source)
        {
            this._X = (decimal)source.X;
            this._Y = (decimal)source.Y;
            this._Width = (decimal)source.Width;
            this._Height = (decimal)source.Height;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="source"></param>
        public RectangleD(Rectangle source)
        {
            this._X = (decimal)source.X;
            this._Y = (decimal)source.Y;
            this._Width = (decimal)source.Width;
            this._Height = (decimal)source.Height;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public RectangleD(decimal x, decimal y, decimal width, decimal height)
        {
            this._X = x;
            this._Y = y;
            this._Width = width;
            this._Height = height;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="location"></param>
        /// <param name="size"></param>
        public RectangleD(PointD location, SizeD size)
        {
            this._X = location.X;
            this._Y = location.Y;
            this._Width = size.Width;
            this._Height = size.Height;
        }
        /// <summary>
        /// Konstruktor statický
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static RectangleD FromPoints(PointD point1, PointD point2)
        {
            return FromLTRB(point1.X, point1.Y, point2.X, point2.Y);
        }
        /// <summary>
        /// Konstruktor statický
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static RectangleD FromPoints(Point point1, Point point2)
        {
            return FromLTRB(point1.X, point1.Y, point2.X, point2.Y);
        }
        /// <summary>
        /// Konstruktor statický
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static RectangleD FromPoints(PointF point1, PointF point2)
        {
            return FromLTRB((decimal)point1.X, (decimal)point1.Y, (decimal)point2.X, (decimal)point2.Y);
        }
        /// <summary>
        /// Konstruktor statický
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        /// <returns></returns>
        public static RectangleD FromLTRB(decimal left, decimal top, decimal right, decimal bottom)
        {
            decimal x = (left < right ? left : right);
            decimal y = (top < bottom ? top : bottom);
            decimal w = (left < right ? right - left : left - right);
            decimal h = (top < bottom ? bottom - top : top - bottom);
            return new RectangleD(x, y, w, h);
        }
        /// <summary>
        /// Prázdné souřadnice (0,0,0,0)
        /// </summary>
        public static readonly RectangleD Empty = new RectangleD();
        private decimal _X;
        private decimal _Y;
        private decimal _Width;
        private decimal _Height;
        /// <summary>
        /// Override HasCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ((this._X.GetHashCode() << 24) ^ (this._Y.GetHashCode() << 16) ^ (this._Width.GetHashCode() << 8) ^ (this._Height.GetHashCode()));
        }
        /// <summary>
        /// Override Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is RectangleD)) return false;
            RectangleD other = (RectangleD)obj;
            return (this == other);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "{X=" + this._X.ToString() + "; Y=" + this._Y.ToString() + "; Width=" + this._Width.ToString() + "; Height=" + this._Height.ToString() + "}";
        }
        /// <summary>
        /// Porovnání dvou instancí
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(RectangleD left, RectangleD right)
        {
            return (left._X == right._X && left._Y == right._Y && left._Width == right._Width && left._Height == right._Height);
        }
        /// <summary>
        /// Porovnání dvou instancí
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(RectangleD left, RectangleD right)
        {
            return (left._X != right._X || left._Y != right._Y || left._Width != right._Width || left._Height == right._Height);
        }
        /// <summary>
        /// Souřadnice X
        /// </summary>
        public decimal X { get { return this._X; } set { this._X = value; } }
        /// <summary>
        /// Souřadnice Y
        /// </summary>
        public decimal Y { get { return this._Y; } set { this._Y = value; } }
        /// <summary>
        /// Rozměr Šířka
        /// </summary>
        public decimal Width { get { return this._Width; } set { this._Width = value; } }
        /// <summary>
        /// Rozměr Výška
        /// </summary>
        public decimal Height { get { return this._Height; } set { this._Height = value; } }

        /// <summary>
        /// Souřadnice Left = X
        /// </summary>
        [Browsable(false)]
        public decimal Left { get { return this._X; } }
        /// <summary>
        /// Souřadnice Top = Y
        /// </summary>
        [Browsable(false)]
        public decimal Top { get { return this._Y; } }
        /// <summary>
        /// Souřadnice Right = X + Width
        /// </summary>
        [Browsable(false)]
        public decimal Right { get { return this._X + this._Width; } }
        /// <summary>
        /// Souřadnice Bottom = Y + Height
        /// </summary>
        [Browsable(false)]
        public decimal Bottom { get { return this._Y + this._Height; } }

        /// <summary>
        /// true pokud je souřadnice celá prázdná (0,0,0,0)
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty { get { return (this._X == 0m && this._Y == 0m && this._Width == 0m && this._Height == 0m); } }

        /// <summary>
        /// Souřadnice počátku
        /// </summary>
        [Browsable(false)]
        public PointD Location { get { return new PointD(this._X, this._Y); } set { this._X = value.X; this._Y = value.Y; } }
        /// <summary>
        /// Velikost prostoru
        /// </summary>
        [Browsable(false)]
        public SizeD Size { get { return new SizeD(this._Width, this._Height); } set { this._Width = value.Width; this._Height = value.Height; } }
        /// <summary>
        /// Střed prostoru
        /// </summary>
        [Browsable(false)]
        public PointD Center { get { return new PointD(this._X + this._Width / 2m, this._Y + this._Height / 2m); } }
        /// <summary>
        /// Pravý dolní bod
        /// </summary>
        [Browsable(false)]
        public PointD Last { get { return new PointD(this.Right, this.Bottom); } }
        #endregion
        #region Public methods
        /// <summary>
        /// Vrací true, pokud this prostor obsahuje daný bod, body na koncové souřadnici (Right, Bottom) se nepočítají
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Contains(decimal x, decimal y)
        {
            return this._Contains(x, y, false);
        }
        /// <summary>
        /// Vrací true, pokud this prostor obsahuje daný bod, body na koncové souřadnici (Right, Bottom) se nepočítají
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Contains(PointD point)
        {
            return this._Contains(point.X, point.Y, false);
        }
        /// <summary>
        /// Vrací true, pokud this prostor obsahuje jiný daný prostor, zcela
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public bool Contains(RectangleD rect)
        {
            return (this._Contains(rect._X, rect._Y, false) && this._Contains(rect.Right, rect.Bottom, true));
        }
        /// <summary>
        /// Vrací true, pokud this prostor obsahuje daný bod, body na koncové souřadnici (Right, Bottom) se počítají podle parametru includeEnd
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="includeEnd"></param>
        /// <returns></returns>
        private bool _Contains(decimal x, decimal y, bool includeEnd)
        {
            return (x >= this._X && (x < this.Right || (includeEnd && x == this.Right)) &&
                    y >= this._Y && (y < this.Bottom || (includeEnd && y == this.Bottom)));
        }
        /// <summary>
        /// Zvětší this prostor o danou velikost na každou stranu
        /// </summary>
        /// <param name="size"></param>
        public void Inflate(SizeD size)
        {
            this._X -= size.Width;
            this._Y -= size.Height;
            this._Width += (size.Width + size.Width);
            this._Height += (size.Height + size.Height);
        }
        /// <summary>
        /// Zvětší this prostor o danou velikost na každou stranu
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Inflate(decimal width, decimal height)
        {
            this._X -= width;
            this._Y -= height;
            this._Width += (width + width);
            this._Height += (height + height);
        }
        /// <summary>
        /// Vrátí daný prostor zvětšený o danou velikost na každou stranu
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static RectangleD Inflate(RectangleD rect, decimal x, decimal y)
        {
            return new RectangleD(rect.X - x, rect.Y - y, rect.Width + 2m * x, rect.Height + 2m * y);
        }
        /// <summary>
        /// Posunout this prostor o daný offset
        /// </summary>
        /// <param name="offset"></param>
        public void Move(PointD offset)
        {
            this._X += offset.X;
            this._Y += offset.Y;
        }
        /// <summary>
        /// Posunout this prostor o daný offset
        /// </summary>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        public void Move(decimal offsetX, decimal offsetY)
        {
            this._X += offsetX;
            this._Y += offsetY;
        }/// <summary>
        /// Ořízne this prostor daným jiným prostorem
        /// </summary>
        /// <param name="rect"></param>
        public void Intersect(RectangleD rect)
        {
            decimal x, y, r, b;
            if (!_Intersect(this, rect, out x, out y, out r, out b))
                this._Clear();
            else
                this._Set(x, y, r - x, b - y);
        }
        /// <summary>
        /// Vrátí průsečík dvou prostorů
        /// </summary>
        /// <param name="r1"></param>
        /// <param name="r2"></param>
        /// <returns></returns>
        public static RectangleD Intersect(RectangleD r1, RectangleD r2)
        {
            decimal x, y, r, b;
            if (!_Intersect(r1, r2, out x, out y, out r, out b))
                return Empty;
            else
                return new RectangleD(x, y, r - x, b - y);
        }
        public bool IntersectsWith(RectangleD rect)
        {
            decimal x, y, r, b;
            return _Intersect(this, rect, out x, out y, out r, out b);
        }
        public static RectangleD Union(RectangleD r1, RectangleD r2)
        {
            decimal x, y, r, b;
            if (!_Intersect(r1, r2, out x, out y, out r, out b))
                return Empty;
            else
                return new RectangleD(x, y, r - x, b - y);
        }
        public static Rectangle Ceiling(RectangleD value)
        {
            int x = (int)Math.Ceiling(value._X);
            int y = (int)Math.Ceiling(value._Y);
            int w = (int)Math.Ceiling(value._Width);
            int h = (int)Math.Ceiling(value._Height);
            return new Rectangle(x, y, w, h);
        }
        public static Rectangle Round(RectangleD value)
        {
            int x = (int)Math.Round(value._X, 0);
            int y = (int)Math.Round(value._Y, 0);
            int w = (int)Math.Round(value._Width, 0);
            int h = (int)Math.Round(value._Height, 0);
            return new Rectangle(x, y, w, h);
        }
        public static Rectangle Truncate(RectangleD value)
        {
            int x = (int)Math.Truncate(value._X);
            int y = (int)Math.Truncate(value._Y);
            int w = (int)Math.Truncate(value._Width);
            int h = (int)Math.Truncate(value._Height);
            return new Rectangle(x, y, w, h);
        }
        #endregion
        #region Operators
        public static RectangleD operator +(RectangleD r1, RectangleD r2)
        { return Union(r1, r2); }
        public static RectangleD operator +(RectangleD rectangle, SizeD size)
        { return new RectangleD(rectangle.Location, (rectangle.Size + size)); }
        public static RectangleD operator *(RectangleD r1, RectangleD r2)
        { return Intersect(r1, r2); }
        #endregion
        #region Implicit & explicit convertors
        public static implicit operator RectangleD(Rectangle rectangle)
        { return new RectangleD(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height); }
        public static implicit operator RectangleD(RectangleF rectangle)
        { return new RectangleD((decimal)rectangle.X, (decimal)rectangle.Y, (decimal)rectangle.Width, (decimal)rectangle.Height); }
        public static explicit operator RectangleF(RectangleD rectangle)
        { return new RectangleF((float)rectangle.X, (float)rectangle.Y, (float)rectangle.Width, (float)rectangle.Height); }
        public static explicit operator Rectangle(RectangleD rectangle)
        { return Round(rectangle); }
        #endregion
        #region Private methods

        private void _Clear()
        {
            this._X = 0m;
            this._Y = 0m;
            this._Width = 0m;
            this._Height = 0m;
        }

        private void _Set(decimal x, decimal y, decimal width, decimal height)
        {
            this._X = x;
            this._Y = y;
            this._Width = width;
            this._Height = height;
        }
        private static bool _Intersect(RectangleD r1, RectangleD r2, out decimal x, out decimal y, out decimal r, out decimal b)
        {
            x = (r1._X > r2._X ? r1._X : r2._X);
            y = (r1._Y > r2._Y ? r1._Y : r2._Y);
            r = (r1.Right < r2.Right ? r1.Right : r2.Right);
            b = (r1.Bottom < r2.Bottom ? r1.Bottom : r2.Bottom);
            if (r < x || b < y)
                return false;
            else
                return true;
        }
        private static bool _Union(RectangleD r1, RectangleD r2, out decimal x, out decimal y, out decimal r, out decimal b)
        {
            x = (r1._X < r2._X ? r1._X : r2._X);
            y = (r1._Y < r2._Y ? r1._Y : r2._Y);
            r = (r1.Right > r2.Right ? r1.Right : r2.Right);
            b = (r1.Bottom > r2.Bottom ? r1.Bottom : r2.Bottom);
            if (r < x || b < y)
                return false;
            else
                return true;
        }
        #endregion
    }
    #endregion
    #region struct Vector
    /// <summary>
    /// Struktura vektoru: obsahuje úhel a délku, lze ji převádět z/na PointF i PointD, lze ji sčítat i odečítat i násobit
    /// </summary>
    public struct Vector
    {
        #region Základní data
        /// <summary>
        /// Vytvoří vektor
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="length"></param>
        public Vector(double angle, double length)
        {
            this._Angle = 0d;
            this._Length = 0d;
            this._SetAngleLength(angle, length);
        }
        /// <summary>
        /// Vytvoří vektor z cílového bodu
        /// </summary>
        /// <param name="target"></param>
        public Vector(PointF target)
        {
            this._Angle = 0d;
            this._Length = 0d;
            this.TargetF = target;
        }
        /// <summary>
        /// Vytvoří vektor z cílového bodu
        /// </summary>
        /// <param name="target"></param>
        public Vector(PointD target)
        {
            this._Angle = 0d;
            this._Length = 0d;
            this.TargetD = target;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Angle=" + this.Angle.ToString("##0.00") + "; Length=" + this.Length.ToString("### ##0.00") + "; Target=" + this.TargetD.ToString();
        }
        /// <summary>
        /// Úhel ve stupních (0 až 360).
        /// Úhel 0° je do kladné hodnoty osy X, úhel vzrůstá podle hodinových ručiček takže hodnota 90° je na ose Y dolů, atd.
        /// To odpovídá tomu, že v PC grafice je osa Y kladná směrem dolů 
        /// (na rozdíl od matematiky, kde osa Y je kladná nahoru, a úhel se točí rovněž nahoru).
        /// </summary>
        public double Angle { get { return this._Angle; } set { this._SetAngleLength(value, this._Length); } } private double _Angle;
        /// <summary>
        /// Délka vektoru
        /// </summary>
        public double Length { get { return this._Length; } set { this._SetAngleLength(this._Angle, value); } } private double _Length;
        /// <summary>
        /// true pokud this je prázdný vektor (Length == 0d), na úhlu nezáleží.
        /// </summary>
        public bool IsEmpty { get { return (this.Length == 0d); } }
        /// <summary>
        /// Obsahuje prázdný vektor
        /// </summary>
        public static Vector Empty { get { return new Vector(); } }
        /// <summary>
        /// Maximální úhel ve stupních = 360d
        /// </summary>
        public static double ANGLE_MAX = 360d;
        /// <summary>
        /// Vloží úhel a délku vektoru. Zápornou délku obrátí do opačného úhlu.
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="length"></param>
        private void _SetAngleLength(double angle, double length)
        {
            if (length >= 0d)
            {   // Nezáporný vektor:
                this._Angle = _GetAngle(angle);
                this._Length = length;
            }
            else
            {   // Záporný vektor = vektor opačné délky opačným směrem:
                this._Angle = _GetAngle(angle + 180d);
                this._Length = -length;
            }
        }
        #endregion
        #region Převodník z/na PointF
        /// <summary>
        /// Cílový bod vektoru, pokud počátek je v bodě 0:0
        /// </summary>
        public PointD TargetD
        {
            get
            {
                double x = this.Length * Math.Cos(DegressToRadian(this._Angle));
                double y = this.Length * Math.Sin(DegressToRadian(this._Angle));
                return new PointD((decimal)x, (decimal)y);
            }
            set
            {
                double x = (double)value.X;
                double y = (double)value.Y;
                double l = Math.Sqrt(x * x + y * y);   // Pythagoras
                double a = 0d;
                // Řeším různé polohy úhlu v rámci 360°:
                if (x == 0d && y == 0d)
                    a = 0d;
                else if (x > 0d && y == 0d)
                    a = 0d;
                else if (x == 0d && y > 0d)
                    a = 90d;
                else if (x < 0d && y == 0d)
                    a = 180d;
                else if (x == 0d && y < 0d)
                    a = 270d;
                else if (y > 0d)
                    // Pro kladné Y platí jednoduché acos(x):
                    a = RadianToDegress(Math.Acos(x / l));
                else if (y < 0d)
                    // Pro záporné Y platí 360-acos(x):
                    a = ANGLE_MAX - RadianToDegress(Math.Acos(x / l));

                this._Angle = a;
                this._Length = l;
            }
        }
        /// <summary>
        /// Cílový bod vektoru, pokud počátek je v bodě 0:0
        /// </summary>
        public PointF TargetF
        {
            get { return (PointF)this.TargetD; }
            set { this.TargetD = (PointD)value; }
        }
        /// <summary>
        /// Převede úhel v radiánech (0 až 2* PI) na stupně (0 až 360°)
        /// </summary>
        /// <param name="angleRadian"></param>
        /// <returns></returns>
        public static double RadianToDegress(double angleRadian)
        {
            return angleRadian * 180d / Math.PI;
        }
        /// <summary>
        /// Převede úhel ve stupních (0 až 360°) na úhel v radiánech (0 až 2* PI)
        /// </summary>
        /// <param name="angleDegress"></param>
        /// <returns></returns>
        public static double DegressToRadian(double angleDegress)
        {
            return angleDegress * Math.PI / 180d;
        }
        /// <summary>
        /// Zarovná úhel do rozmezí 0-360°
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        private static double _GetAngle(double angle)
        {
            if (angle >= 0d && angle < ANGLE_MAX) return angle;
            if (angle > 0d) return angle % ANGLE_MAX;
            return ANGLE_MAX + (angle % ANGLE_MAX);
        }
        #endregion
        #region Operátory, HashCode a Equals
        /// <summary>
        /// Je rovno?
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Vector a, Vector b)
        {
            return (a._Angle == b._Angle && a._Length == b._Length);
        }
        /// <summary>
        /// Není rovno?
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(Vector a, Vector b)
        {
            return (a._Angle != b._Angle || a._Length != b._Length);
        }
        /// <summary>
        /// Součet vektorů
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.TargetD + b.TargetD);
        }
        /// <summary>
        /// Rozdíl vektorů
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vector operator -(Vector a, Vector b)
        {
            return new Vector(a.TargetD - b.TargetD);
        }
        /// <summary>
        /// Násobení Length vektoru daným koeficientem
        /// </summary>
        /// <param name="a"></param>
        /// <param name="coefficient"></param>
        /// <returns></returns>
        public static Vector operator *(Vector a, double coefficient)
        {
            return new Vector(a._Angle, coefficient * a._Length);
        }
        /// <summary>
        /// Override HashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (this._Angle.GetHashCode() << 16) | (this._Length.GetHashCode());
        }
        /// <summary>
        /// Override Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Vector)) return false;
            Vector other = (Vector)obj;
            return (this == other);
        }
        #endregion
    }
    #endregion
    #region Interval<TValue, TSize>
    public abstract class Interval<TValue, TSize>
        where TValue : IComparable
        where TSize : IComparable
    {
        public Interval(TValue begin, TValue end)
        {
            this.Begin = begin;
            this.End = end;
        }
        private Interval()
        { }
        public TValue Begin { get; private set; }
        public TValue End { get; private set; }
        public TSize Size { get { return GetSize(this.Begin, this.End); } }


        protected abstract TValue GetBegin(TSize size, TValue end);
        protected abstract TSize GetSize(TValue begin, TValue end);
        protected abstract TValue GetEnd(TValue begin, TSize size);
    }
    #endregion
}
